using System;
using System.Threading.Tasks;
using CM.Backend.Identity.AuthorizationServer.Handlers;
using CM.Backend.Identity.AuthorizationServer.Helpers;
using CM.Backend.Identity.AuthorizationServer.Repositories;
using CM.Backend.Identity.AuthorizationServer.RequestModels;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace CM.Backend.Identity.AuthorizationServer.Controllers
{
    [Route("identity-api/v1/migration")]
    public class MigrationController : BaseController
    {
        public MigrationController(IClientStore clientStore, IUserHandler userHandler, IPasswordResetHandler passwordResetHandler) : base(clientStore, userHandler, passwordResetHandler)
        {
        }
        
        
        [HttpPost]
        [ServiceFilter(typeof(ClientValidationFilter))]
        [Route("users")]
        public async Task<IActionResult> CreateUser([FromBody]CreateUserRequestModel createUserRequestModel)
        {
            //Validate the user
            var response = await UserHandler.MigrateUser(createUserRequestModel.Id, createUserRequestModel.Email.ToLower(), createUserRequestModel.Password);
            
            if(!response.IsSuccessful)
            {
                Console.WriteLine(response.Message);
                Log.Logger.Error(nameof(CreateUser) + " failed for {Id}, {Email} with {@Response}", createUserRequestModel.Id, createUserRequestModel.Email, response);
                return StatusCode(403, response.Message);
            }

            return new ObjectResult(response.Token);
        }
    }
}