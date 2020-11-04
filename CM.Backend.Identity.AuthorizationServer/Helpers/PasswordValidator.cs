using System;
using System.Security.Claims;
using System.Threading.Tasks;
using CM.Backend.Identity.AuthorizationServer.Repositories;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Validation;

namespace CM.Backend.Identity.AuthorizationServer.Helpers
{
    public class PasswordValidator : IResourceOwnerPasswordValidator
    {
        private readonly IUserHandler _userHandler;
        
        public PasswordValidator(IUserHandler userHandler)
        {
            _userHandler = userHandler;
        }
        
        public Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            try
            {       
                //get user model from db by email
                var user = _userHandler.FindUserByEmail(context.UserName.ToLower()).Result;
                
                if (user != null)
                {
                    //check if password match - this should be hashed
                    if (BCrypt.Net.BCrypt.Verify(context.Password, user.Password))
                    {
                        //set the result
                        context.Result = new GrantValidationResult(
							subject: user.Id.ToString(),
                            authenticationMethod: "custom");

                        return Task.CompletedTask;
                    }

                    context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Incorrect password");
                    return Task.CompletedTask;
                }

                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "User does not exist.");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                context.Result =
                    new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Invalid username or password");
            }
            
            return Task.CompletedTask;
        }
    }
}