using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CM.Backend.Identity.AuthorizationServer.Helpers;
using IdentityServer4.Events;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Test;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Serilog;
using LoginInputModel = CM.Backend.Identity.AuthorizationServer.ViewModels.LoginInputModel;
using LoginViewModel = CM.Backend.Identity.AuthorizationServer.ViewModels.LoginViewModel;

namespace CM.Backend.Identity.AuthorizationServer.Controllers
{
    [Route("account")]
    public class AccountController : Controller
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly IEventService _events;
        private readonly IOptions<CMAdminConfiguration> _cmAdminConfig;
        private readonly IOptions<IdentityServerUrlSettings> _identityConfig;
        private readonly TestUserStore _users;

        public AccountController(
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IEventService events, 
            IOptions<CMAdminConfiguration> cmAdminConfig,
            IOptions<IdentityServerUrlSettings> identityConfig,
            TestUserStore users = null)
        {
            // if the TestUserStore is not in DI, then we'll just use the global users collection
            // this is where you would plug in your own custom identity management library (e.g. ASP.NET Identity)
            _users = users ?? new TestUserStore(Config.GetSystemUsers(_cmAdminConfig.Value.Username, _cmAdminConfig.Value.Password));

            _interaction = interaction;
            _clientStore = clientStore;
            _events = events;
            _cmAdminConfig = cmAdminConfig;
            _identityConfig = identityConfig;
        }

        /// <summary>
        /// Entry point for login page.
        /// </summary>
        /// <returns>The login.</returns>
        /// <param name="returnUrl">Return URL.</param>
        [HttpGet]
        [Route("login")]
        public async Task<IActionResult> Login(string returnUrl)
        {
            var validUrl = await ValidateRedirectUrl(returnUrl);

            if (!validUrl)
            {
                Log.Logger.Error("Tried to fetch CM-Admin login view with error --> bad return url: " + returnUrl);
                return Redirect($"{_identityConfig.Value.IdentityServerUrl}/Home/Error?errorId=1");
            }
            
           ViewBag.ReturnUrl = returnUrl;
            var vm = new LoginViewModel
            {
                Error = "",
                ReturnUrl = returnUrl,
                IsSuccessfull = true
            };
            return View("~/Views/LoginView/LoginView.cshtml", vm);
        }

        /// <summary>
        /// Accept the login form from the view
        /// </summary>
        /// <param name="loginInputModel"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginInputModel loginInputModel)
        {
            if (_users.ValidateCredentials(loginInputModel.Email, loginInputModel.Password))
            {
                var user = _users.FindByUsername(loginInputModel.Email);
                await _events.RaiseAsync(new UserLoginSuccessEvent(user.Username, user.SubjectId, user.Username));

                AuthenticationProperties props = new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
                };
                
                await HttpContext.SignInAsync(user.SubjectId, user.Username, props, user.Claims.ToArray());
                
                return Redirect(loginInputModel.ReturnUrl);

            }
            
            Log.Logger.Error(HttpContext.Connection.RemoteIpAddress.ToString() + " tried to login with username " + loginInputModel.Email + " and password " + loginInputModel.Password + ". <-- Login attempt was rejected due to invalid credentials");
            
            //Show user error; example wrong login credentials
            ViewBag.ReturnUrl = loginInputModel.ReturnUrl;
            var vm = new LoginViewModel{Error = "Invalid credentials", ReturnUrl = loginInputModel.ReturnUrl, IsSuccessfull = false };
            return View("~/Views/LoginView/LoginView.cshtml", vm);
        }

        private async Task<bool> ValidateRedirectUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }

            if (!url.Contains('&'))
            {
                return false;
            }

            var blocks = url.Split('&');

            foreach (var block in blocks)
            {
                if (block.Contains("client_id"))
                {
                    var clientId = block.Replace("client_id=", "");
                    var enabledClient = await _clientStore.FindEnabledClientByIdAsync(clientId);
                    if (enabledClient == null)
                    {
                        return false;
                    }

                    return true;
                }
            }

            return false;
        }
    }
}