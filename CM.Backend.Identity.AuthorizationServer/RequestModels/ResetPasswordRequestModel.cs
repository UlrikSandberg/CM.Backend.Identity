using System;
namespace CM.Backend.Identity.AuthorizationServer.RequestModels
{
    public class ResetPasswordRequestModel
    {
		public string Token { get; set; }
		public string Password { get; set; }
		public string ConfirmPassword { get; set; }
	   
    }
}
