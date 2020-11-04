using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CM.Backend.Identity.AuthorizationServer.Handlers;
using CM.Backend.Identity.AuthorizationServer.Helpers;
using CM.Backend.Identity.AuthorizationServer.Repositories;
using IdentityServer4.Stores;
using Serilog;

namespace CM.Backend.Identity.AuthorizationServer.Controllers
{
    [Route("identity-api/v1/Validation")]
    public class ValidateEnabledClientController : BaseController
    {
        public ValidateEnabledClientController(IClientStore clientStore, IUserHandler userHandler, IPasswordResetHandler passwordResetHandler) : base(clientStore, userHandler, passwordResetHandler)
        {
        }

        [HttpGet]
        [ServiceFilter(typeof(ClientValidationFilter))]
        [Route("")]
        public async Task<IActionResult> ValidateEnabledClientAsync([FromHeader(Name = "X-Client-ID")]string clientId, [FromHeader(Name = "X-Client-Secret")]string clientSecret)
        {
            return Ok();
        }
    }
}