using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;

namespace CM.Backend.Identity.AuthorizationServer.Helpers
{
    public class ClientValidationFilter : ActionFilterAttribute
    {
        private readonly IClientStore _clientStore;

        public ClientValidationFilter(IClientStore clientStore)
        {
            _clientStore = clientStore;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var authorized = await Authorize(context);

            if (!authorized)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            await base.OnActionExecutionAsync(context, next);
        }

        private async Task<bool> Authorize(ActionContext context)
        {
            var clientId = context.HttpContext.Request.Headers.FirstOrDefault(x =>
                x.Key.Equals("X-Client-Id", StringComparison.OrdinalIgnoreCase));
            var clientSecret = context.HttpContext.Request.Headers.FirstOrDefault(x =>
                x.Key.Equals("X-Client-Secret", StringComparison.OrdinalIgnoreCase));
              
            if (!clientId.Value.Any() && !clientSecret.Value.Any())
            {
                return false;
            }
            
            var client = await _clientStore.FindEnabledClientByIdAsync(clientId.Value.First());

            if (client != null && client.ClientSecrets.Any(x => x.Value == clientSecret.Value.First().Sha256()))
                return true;

            Log.Logger.Error("Failed to find enabled client --> Requested authorization with {ClientId}", clientId);
            return false;
        }
    }

}