using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;
using CM.Backend.Identity.AuthorizationServer.Configurations;
using CM.Backend.Identity.AuthorizationServer.Response;
using CM.Backend.Identity.AuthorizationServer.Helpers;
using EmailValidation;
using IdentityModel.Client;
using Microsoft.Extensions.Options;
using TokenResponse = IdentityModel.Client.TokenResponse;
using CM.Backend.Identity.AuthorizationServer.Security;
using CM.Backend.Identity.AuthorizationServer.Repositories.Models;
using Serilog;

namespace CM.Backend.Identity.AuthorizationServer.Repositories
{
    public interface IUserHandler
	{
		Task<ResponseMessage> CreateUser(Guid id, string email, string password);
		Task<ResponseMessage> MigrateUser(Guid id, string email, string password);
		Task<ResponseMessage> UpdateEmail(Guid userId, string email, string password);
		Task<ResponseMessage> UpdatePassword(Guid userId, string currentPassword, string newPassword);
		Task<ResponseMessage> UpdatePassword(string token, string newPassword, string confirmNewPassword);
		Task<UserModel> FindUserByEmail(string email);
	}

	public class UserHandler : IUserHandler
    {
		private readonly IUserRepository userStore;
	    private readonly IOptions<IdentityServerUrlSettings> _config;
	    private readonly IOptions<ClientIdSecretConfigurations> _clientIdSecretConfigs;
	    private const string forbiddenChars = " \"";
	    private const string forbiddenUsernameChars = " !#$%&'()*+,./:;<=>?@[\\]^`{|}~\"";
		private readonly IPasswordRecoveryRepository _passwordRecoveryRepository;

		public UserHandler(IUserRepository userRepository, IPasswordRecoveryRepository passwordRecoveryRepository, IOptions<IdentityServerUrlSettings> config, IOptions<ClientIdSecretConfigurations> clientIdSecretConfigs)
		{
			_passwordRecoveryRepository = passwordRecoveryRepository;
			userStore = userRepository;
			_config = config;
			_clientIdSecretConfigs = clientIdSecretConfigs;
		}

		public async Task<ResponseMessage> CreateUser(Guid id, string email, string password)
		{     
            //Validate if the email or password is a valid type.
			if(!ValidateEmail(email).IsSuccessful)
			{
				return new ResponseMessage(false, null, null , "Must enter valid email address");
			}

			if (password.Length < 6)
			{
				return new ResponseMessage(false, null, null, "Password must be atleast 6 characters long");
			}
			
			if(!ValidatePassword(password).IsSuccessful)
			{
				return new ResponseMessage(false, null, null , "Special characters are not allowed in the password");
			}

			//Validate that the email has not been used yet
			var result = await FindUserByEmail(email);

			if(result != null)
			{
				return new ResponseMessage(false, null, null, "An account already exists with this email");
			}
                     
			//Both email and password is valid, plus there exist no account matching the email -> Create that account!

			//Hash the password and insert the new user into the UserStore
			var hashPassword = BCrypt.Net.BCrypt.HashPassword(password);
            
            //Remember to save email as lower
			await userStore.Insert(new UserModel
			{
				Id = id,
				Email = email.ToLower(),
				Password = hashPassword,
				IsActive = true,
				Claims = new List<string>()
			});

			//User has been successfully created grant init tokens         
			var initTokens = await GrantInitTokens(email, password);
			
			if (initTokens != null)
			{
				if (!initTokens.IsError)
				{
					return new ResponseMessage(true, initTokens, null, "Succesful");	
				}
			}
			//TODO : Maybe an overkill, seing as they might just be able to log-in the next time? Although they don't have access to app without. Maybe we could try a couple of times?
			Log.Logger.Fatal("User create but the server was not able to issue access tokens. The invocation was rolled back for {UserId}, {Email} failed with {@InitTokens}", id, email, initTokens);
			var rollbackResponse = await RollBack(id);
			
			return new ResponseMessage(false, null, null, "User created but the server encountered problems issuing access tokens, the invocation was rolled back");

		}

	    public async Task<ResponseMessage> MigrateUser(Guid id, string email, string password)
	    {
		    //Validate that the email has not been used yet
		    var result = await FindUserByEmail(email);

		    if(result != null)
		    {
			    return new ResponseMessage(false, null, null, "An account already exists with this email");
		    }
		    //Remember to save email as lower
		    await userStore.Insert(new UserModel
		    {
			    Id = id,
			    Email = email.ToLower(),
			    Password = password,
			    IsActive = true,
			    Claims = new List<string>()
		    });
		    
		    return new ResponseMessage(true);
	    }

	    public async Task<ResponseMessage> UpdateEmail(Guid userId, string email, string password)
	    {
		    var validation = EmailValidator.Validate(email);
		    
			if (!EmailValidator.Validate(email))
		    {
				return new ResponseMessage(false, null, null, "Must enter valid email address");
		    }
		    
			var result = await userStore.FindUserByEmail(email);

		    if (result != null)
		    {
			    return new ResponseMessage(false, null, null, "Email already taken");
		    }
		    
		    //Check if the password match
		    var user = await userStore.FindUserById(userId);
		    if (user == null)
		    {
			    return new ResponseMessage(false, null, null, "Unauthorized userId");
		    }

		    if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
		    {
			    return new ResponseMessage(false, null, null, "Incorrect password");
		    }
		    
		    await userStore.UpdateEmail(userId, email);
		    return new ResponseMessage(true);
		    
	    }

	    public async Task<ResponseMessage> UpdatePassword(Guid userId, string currentPassword, string newPassword)
	    {
		    if (!ValidatePassword(newPassword).IsSuccessful)
		    {
			    return new ResponseMessage(false, null, null, "Password may not contain spaces nor \"");
		    }
		    
		    //Firstly check if the currentPassword matches
		    var user = await userStore.FindUserById(userId);
		    
		    
		    //Check if currentPassword matches the one in the store
		    if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.Password))
		    {
			    return new ResponseMessage(false, null, null, "Password is incorrect");
		    }
		    
		    //Insert new password;
		    await userStore.UpdatePassword(userId, BCrypt.Net.BCrypt.HashPassword(newPassword));
            
			return new ResponseMessage(true);         
	    }

		public async Task<ResponseMessage> UpdatePassword(string token, string newPassword, string confirmNewPassword)
        {
			var tokenResponse = await _passwordRecoveryRepository.GetRecoveryProcessFromToken(Cryptography.Hash_sha256(token));

			var validateResponse = ValidateTokenResponse(tokenResponse);

			if(!validateResponse.IsSuccessful)
			{
				return validateResponse;
			}
            
			if(!newPassword.Equals(confirmNewPassword))
			{
				return new ResponseMessage(false, null, null, "New Password and Confirm New Password must match");
			}

			if (confirmNewPassword.Length < 6)
            {
                return new ResponseMessage(false, null, null, "Password must be atleast 6 characters long");
            }

			if (!ValidatePassword(confirmNewPassword).IsSuccessful)
            {
                return new ResponseMessage(false, null, null, "Special characters are not allowed in the password");
            }

			//Password match all criteria, hash password and update password for user
			await userStore.UpdatePassword(tokenResponse.UserId, BCrypt.Net.BCrypt.HashPassword(confirmNewPassword));

			//Recovery Procedure completed invalidate the recovery procedure process state.
			await _passwordRecoveryRepository.InvalidateRecoveryProcess(tokenResponse.Id);

			return new ResponseMessage(true);

        }      

	    public async Task<UserModel> FindUserByEmail(string email)
	    {
		    var result = await userStore.FindUserByEmail(email);

		    if (result == null)
		    {
			    return null;
		    }

		    return result;
	    }

	    private ResponseMessage ValidateEmail(string email)
		{
			return new ResponseMessage(EmailValidator.Validate(email));
		}

        private ResponseMessage ValidatePassword(string password)
		{
			foreach(char c in password)
			{
				if(forbiddenChars.Contains(c))
				{
					return new ResponseMessage(false);
				}
			}
			return new ResponseMessage(true);
		}

	    private async Task<TokenResponse> GrantInitTokens(string email, string password, int allowedFailAttempts = 3)
	    {
		    var disco = await DiscoveryClient.GetAsync(_config.Value.IdentityServerUrl);

		    if(disco.IsError)
		    {
			    Console.WriteLine(disco.Error);
			    return null;
		    }

		    var tokenClient = new TokenClient(disco.TokenEndpoint, _clientIdSecretConfigs.Value.AppClientId, _clientIdSecretConfigs.Value.AppClientSecret); 
		    
		    TokenResponse tokenResponse = null;
		    var grantAttempts = 0;
		    var grantSuccessful = false;

		    while (!grantSuccessful)
		    {
			    tokenResponse = await tokenClient.RequestResourceOwnerPasswordAsync(email, password);
			    if (!tokenResponse.IsError)
			    {
				    grantSuccessful = true;
			    }
			    else
			    {
				    grantAttempts++;
				    await Task.Delay(100);
				    if (grantAttempts >= allowedFailAttempts)
				    {
					    break;
				    }
			    }
		    }
		    
		    return tokenResponse;
	    }

	    private async Task<ResponseMessage> RollBack(Guid id)
	    {
		    var result = await userStore.DeleteById(id);

		    return new ResponseMessage(result);
	    }
      
		private ResponseMessage ValidateTokenResponse(PasswordRecoveryModel passwordRecoveryModel)
		{
			if (passwordRecoveryModel == null)
            {
                return new ResponseMessage(false, null, null, "Unauthorized token, this may be caused by a broken link. Try requesting another password reset.");
            }

			if (!passwordRecoveryModel.IsActive)
            {
                return new ResponseMessage(false, null, null, "This password recovery link is no longer active, or has been used before. Try requesting another password reset.");
            }

			var expirationDate = passwordRecoveryModel.RecoveryRequestedAt.AddDays(1);

            if (DateTime.UtcNow.CompareTo(expirationDate) > 0)
            {
                return new ResponseMessage(false, null, null, "This password recovery link has expired. Try requesting another password reset.");
            }

            return new ResponseMessage(true);         
		}
	}
}
