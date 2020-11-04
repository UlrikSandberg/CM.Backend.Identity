using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;
using CM.Backend.Identity.AuthorizationServer.Handlers;
using CM.Backend.Identity.AuthorizationServer.Helpers;
using CM.Backend.Identity.AuthorizationServer.Repositories;
using CM.Backend.Identity.AuthorizationServer.Repositories.Helpers;
using CM.Backend.Identity.AuthorizationServer.RequestModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Serilog;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CM.Backend.Identity.AuthorizationServer.Controllers
{
	[Route("identity-api/v1/users")]
    public class UserController : BaseController
    {
	    public UserController(IClientStore clientStore, IUserHandler userHandler, IPasswordResetHandler passwordResetHandler) : base(clientStore, userHandler, passwordResetHandler)
	    {
	    }

		[HttpPost]
		[ServiceFilter(typeof(ClientValidationFilter))]
		[Route("")]
		public async Task<IActionResult> CreateUser([FromBody]CreateUserRequestModel createUserRequestModel)
		{
			//Validate the user
			var response = await UserHandler.CreateUser(createUserRequestModel.Id, createUserRequestModel.Email.ToLower(), createUserRequestModel.Password);
            
			if(!response.IsSuccessful)
			{
				Console.WriteLine(response.Message);
				Log.Logger.Error(nameof(CreateUser) + " failed for {Id}, {Email} with {@Response}", createUserRequestModel.Id, createUserRequestModel.Email, response);
				return StatusCode(403, response.Message);
			}

			return new ObjectResult(response.Token);
		}

	    [HttpPut]
	    [Authorize(AuthenticationSchemes = "Bearer")] //<-- DO NOT CHANGE *** This Authentication scheme may not be changed, the default startup settings to allow swagger does not at the same time allow default [Authorize] authorization and thus [Authorize(AuthenticationSchemes = "Bearer")] is required
	    [Route("{userId}/updateEmail")]
	    public async Task<IActionResult> UpdateEmail(Guid userId, string email, string password)
	    {
			var response = await UserHandler.UpdateEmail(userId, email.ToLower(), password);
		    
			if (!response.IsSuccessful)
		    {
			    Log.Logger.Error(nameof(UpdateEmail) + " failed for {Id}, {Email} with {@Response}", userId, email, response);
			    return StatusCode(403, response.Message); 
		    }

		    return StatusCode(201);
	    }
      
	    [HttpPut]
	    [Authorize(AuthenticationSchemes = "Bearer")] //<-- DO NOT CHANGE *** This Authentication scheme may not be changed, the default startup settings to allow swagger does not at the same time allow default [Authorize] authorization and thus [Authorize(AuthenticationSchemes = "Bearer")] is required
	    [Route("currentUser/updatePassword")]
	    public async Task<IActionResult> UpdatePassword(string currentPassword, string newPassword)
	    {
		    //First read token from auth headers
		    StringValues value = Request.Headers["Authorization"];

		    if (value.Count < 1)
		    {
			    return StatusCode(401);
		    }
		    
		    //Value is not empty extract token
		    var accessToken = value.ToString().Substring(7);
		    
		    //Decrypt token
		    var handler = new JwtSecurityTokenHandler();
		    var decodedToken = handler.ReadJwtToken(accessToken) as JwtSecurityToken;
		    
		    Guid userId = Guid.Empty;

		    if (decodedToken != null)
		    {
			    try
			    {
				    userId = Guid.Parse(decodedToken.Subject);
			    }
			    catch (Exception ex)
			    {
				    Log.Fatal(ex, "Exception occured while decoding userId from bearer token");
				    Console.WriteLine(ex.Message);
			    }
		    }

		    if (userId.Equals(Guid.Empty))
		    {
			    return StatusCode(401);
		    }
		    
			var response = await UserHandler.UpdatePassword(userId, currentPassword, newPassword);

			if (!response.IsSuccessful)
			{
				Log.Error(nameof(UpdatePassword) + " failed for {userId} with {@Response}", userId, response);
				return StatusCode(400, response.Message);
			}

			return StatusCode(201);
	    } 
    }
}
