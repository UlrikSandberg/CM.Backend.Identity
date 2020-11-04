using System.Threading.Tasks;
using CM.Backend.Identity.AuthorizationServer.ViewModels;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace CM.Backend.Identity.AuthorizationServer.Controllers
{
    [Route("Home")]
    public class HomeController : Controller
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IHostingEnvironment _environment;

        public HomeController(IIdentityServerInteractionService interaction, IHostingEnvironment environment)
        {
            _interaction = interaction;
            _environment = environment;
        }


        /// <summary>
        /// Shows the error page
        /// </summary>
        [HttpGet]
        [Route("Error")]
        public async Task<IActionResult> Error(string errorId)
        {
            var vm = new ErrorViewModel();

            // retrieve error details from identityserver
            var message = await _interaction.GetErrorContextAsync(errorId);
            if (message != null)
            {
                vm.Error = message;

                /*if (!_environment.IsDevelopment())
                {
                    // only show in development
                    message.ErrorDescription = null;
                }*/
            }

            return View("~/Views/ErrorView/ErrorView.cshtml", vm);
        }
    }
}