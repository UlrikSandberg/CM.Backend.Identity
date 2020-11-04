using System;
using System.Diagnostics;
using System.Text;
using CM.Backend.Identity.AuthorizationServer.Handlers;
using CM.Backend.Identity.AuthorizationServer.Repositories;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Serilog;
using Serilog.Core;

namespace CM.Backend.Identity.AuthorizationServer.Controllers
{
    public class BaseController : Controller
    {
        protected IUserHandler UserHandler;
        protected IPasswordResetHandler PasswordResetHandler;
        protected readonly IClientStore ClientStore;

        public BaseController(IClientStore clientStore, IUserHandler userHandler, IPasswordResetHandler passwordResetHandler)
        {
            UserHandler = userHandler;
            PasswordResetHandler = passwordResetHandler;
            ClientStore = clientStore;
        }
    }
}