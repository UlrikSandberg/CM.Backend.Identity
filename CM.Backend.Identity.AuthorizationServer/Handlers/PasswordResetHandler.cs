using System;
using System.Threading.Tasks;
using CM.Backend.Identity.AuthorizationServer.Response;
using CM.Backend.Identity.AuthorizationServer.Repositories;
using CM.Backend.Identity.AuthorizationServer.Repositories.Models;
using System.Collections.Generic;
using CM.Backend.Identity.AuthorizationServer.Configurations;
using CM.Backend.Identity.AuthorizationServer.Helpers;
using Microsoft.Extensions.Options;
using CM.Backend.Identity.AuthorizationServer.Repositories.Helpers;
using CM.Backend.Identity.AuthorizationServer.Security;
using CM.Backend.Identity.AuthorizationServer.SendGrid;
using CM.Backend.Identity.AuthorizationServer.SendGrid.DynamicEmailDataTemplates;
using Newtonsoft.Json;
using Serilog;

namespace CM.Backend.Identity.AuthorizationServer.Handlers
{
	public interface IPasswordResetHandler
	{
		Task<ResponseMessage> StartPasswordRecoveryProcedure(string email);
		Task<ResponseMessage> ValidatePasswordRecoveryToken(string token);
	}

	public class PasswordResetHandler : IPasswordResetHandler
	{
		private readonly IPasswordRecoveryRepository passwordRecoveryRepository;
		private readonly IUserRepository userRepository;
		private readonly IOptions<IdentityServerUrlSettings> _identityServerUrlConfig;
		private readonly IOptions<SendGridConfigs> _sendgridConfigs;
		private readonly IOptions<EmailAuthorityConfigurations> _emailAuthorityConfigs;

		public PasswordResetHandler(IPasswordRecoveryRepository passwordRecoveryRepository, IUserRepository userRepository, IOptions<IdentityServerUrlSettings> identityServerUrlConfig, IOptions<SendGridConfigs> sendgridConfigs, IOptions<EmailAuthorityConfigurations> emailAuthorityConfigs)
		{
			this.userRepository = userRepository;
			_identityServerUrlConfig = identityServerUrlConfig;
			_sendgridConfigs = sendgridConfigs;
			_emailAuthorityConfigs = emailAuthorityConfigs;
			this.passwordRecoveryRepository = passwordRecoveryRepository;
		}

		public async Task<ResponseMessage> StartPasswordRecoveryProcedure(string email)
		{
			//Find user for email.
			var user = await userRepository.FindUserByEmail(email);

			if (user == null)
			{
				////error 100: This means the specified email was not in the system. But by definition this information should not be available... To prevent scrapping with rainbow tables.
				return new ResponseMessage(false, null, null, "100"); //TODO : Vil vi faktisk gøre det sådan?
			}

			//Invalidate all tokens related to this user...
			await passwordRecoveryRepository.InvalidateAllRecoveriesForUser(user.Id);
			
			//Start password recovery procedure by generating a unique token id.
			var token = Guid.NewGuid();
			var recoveryId = Guid.NewGuid();

			var encryptedToken = Cryptography.Hash_sha256(token.ToString());

			//store passwordRecovery state...
			await passwordRecoveryRepository.Insert(new PasswordRecoveryModel
			{
				Id = recoveryId,
				UserId = user.Id,
				Email = email,
				RecoveryToken = encryptedToken,
				RecoveryRequestedAt = DateTime.UtcNow,
				IsActive = true
			});

			//Configure email personalization
			var personalization = new Personalization<PasswordResetTemplateData>();
			personalization.to = new List<To> {new To {email = email}};
			personalization.dynamic_template_data = new PasswordResetTemplateData
			{
				passwordRecoveryLink = $"{_identityServerUrlConfig.Value.IdentityServerUrl}/identity-api/resetpassword/changepassword/token/{token.ToString()}/"
			};
			
			//Send password recovery email
			await SendConfirmationEmail(email, _sendgridConfigs.Value.ResetPasswordTemplateId, personalization);
			
			return new ResponseMessage(true);
		}

		private async Task SendConfirmationEmail(string toEmail, string templateId, Personalization<PasswordResetTemplateData> personalized)
		{
			//Start SendGridClient
			var client = new SendGridClient(_sendgridConfigs.Value.API_Key);
            
			//Configure email
			var email = new SendGridEmailTemplate<PasswordResetTemplateData>(templateId);
           
			//Add personalization to email!
			email.AddPersonalization(personalized);

			//Email from parameters
			email.from = new From
			{
				email = _emailAuthorityConfigs.Value.Email,
				name = _emailAuthorityConfigs.Value.Name
			};

			var json = JsonConvert.SerializeObject(email, Formatting.None);

			var response = await client.SendTransactionalEmail(json);

			if (!response.IsSuccessStatusCode)
			{
				if (response.Content != null)
				{
					
					var errorMsg = await response.Content.ReadAsStringAsync();
					Log.Error("Failed to send transactional email with password reset for {ToEmail}, with {TemplateId} and {@Email}. Failed with {ResponseMSG}", toEmail, templateId, email, errorMsg);
					Console.WriteLine(errorMsg);
				}
				Log.Error("Failed to send transactional email with password reset for {ToEmail}, with {TemplateId} and {@Template}. Failed with {ResponseMSG}", toEmail, templateId, email, response.ReasonPhrase);
			}
		}

		public async Task<ResponseMessage> ValidatePasswordRecoveryToken(string token)
		{
			//Check if the provided token matches a record in the passwordRecoveryRepo and the record is not more than 1 day old and is still active.
			var encryptedToken = Cryptography.Hash_sha256(token);

			//Find passwordRecoveryState from repo
			var result = await passwordRecoveryRepository.GetRecoveryProcessFromToken(encryptedToken);

			if(result == null)
			{
				return new ResponseMessage(false, null, null, "Unauthorized token, this may be caused by a broken link. Try requesting another password reset.");
			}

			if(!result.IsActive)
			{
				return new ResponseMessage(false, null, null, "This password recovery link is no longer active, or has been used before. Try requesting another password reset.");
			}

			var expirationDate = result.RecoveryRequestedAt.AddDays(1);

			if(DateTime.UtcNow.CompareTo(expirationDate) > 0)
			{
				return new ResponseMessage(false, null, null, "This password recovery link has expired. Try requesting another password reset.");
			}

			return new ResponseMessage(true);
		}      
	}
}
