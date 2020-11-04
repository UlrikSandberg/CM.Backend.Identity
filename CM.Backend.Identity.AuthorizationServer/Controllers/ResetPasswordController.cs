using Microsoft.AspNetCore.Mvc;
using CM.Backend.Identity.AuthorizationServer.RequestModels;
using System.Threading.Tasks;
using CM.Backend.Identity.AuthorizationServer.Response;
using IdentityServer4.Stores;
using CM.Backend.Identity.AuthorizationServer.Repositories;
using CM.Backend.Identity.AuthorizationServer.Handlers;
using CM.Backend.Identity.AuthorizationServer.Helpers;
using Serilog;

namespace CM.Backend.Identity.AuthorizationServer.Controllers
{
	[Route("identity-api/resetpassword")]
	public class ResetPasswordController : BaseController
	{
		public ResetPasswordController(IClientStore clientStore, IUserHandler userHandler, IPasswordResetHandler passwordResetHandler) : base(clientStore, userHandler, passwordResetHandler)
		{
		}

		[HttpGet]
		[Route("changepassword/token/{token}")]
		public async Task<ActionResult> ResetPasswordPage(string token)
		{
			//Check if the id is in the database and is valid...
			var response = await PasswordResetHandler.ValidatePasswordRecoveryToken(token);

			if (response.IsSuccessful)
			{
				ViewBag.Token = token;
				return View();
			}
			else
			{
				//Display page with error message!
				Log.Logger.Error(nameof(PasswordResetHandler.ValidatePasswordRecoveryToken) + " returned {@Response} in context of {Token}" , response, token);
				return View("ErrorResetPassword", response);
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[Route("")]
        public async Task<IActionResult> ChangePassword(ResetPasswordRequestModel model)
        {
			var result = await UserHandler.UpdatePassword(model.Token, model.Password, model.ConfirmPassword);

			if(!result.IsSuccessful) 
			{
				Log.Logger.Error(nameof(ChangePassword) + " Failed with input {Token}, and {@Result}", model.Token, result);
				return new JsonResult(new ResetPasswordResponse
				{
					Succes = result.IsSuccessful,
					ErrorMessage = result.Message
				});
			}

			return new JsonResult(new ResetPasswordResponse { Succes = true });
        }
      
        [HttpPost]
        [ServiceFilter(typeof(ClientValidationFilter))]
        [Route("requestpasswordreset")]
        public async Task<IActionResult> RequestPasswordReset(string email)
        {
            var response = await PasswordResetHandler.StartPasswordRecoveryProcedure(email.ToLower());

            if(!response.IsSuccessful)
            {
	            Log.Logger.Error(nameof(RequestPasswordReset) + " failed for {Email}, with response", email, response);
                return StatusCode(400, response.Message);
            }

            return StatusCode(200);
        }
	}
}