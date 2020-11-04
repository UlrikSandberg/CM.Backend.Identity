using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CM.Backend.API.Middleware
{
    public static class SwaggerExtensions
    {
        public static void ApplySecurityDefinitionToMethods(this SwaggerGenOptions genContext)
        {
            genContext.OperationFilter<AuthorizeCheckOperationFilter>();
        }
    }
    
    public class AuthorizeCheckOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            var attributes = context.MethodInfo.CustomAttributes;
            var controllerAttributes = context.MethodInfo.DeclaringType.CustomAttributes;
            
            var authorizeAttribute = attributes.Where(x =>
                x.AttributeType.ToString().Equals(typeof(AuthorizeAttribute).ToString()));

            var controllerAuthorizeAttribute = controllerAttributes.Where(x =>
                x.AttributeType.ToString().Equals(typeof(AuthorizeAttribute).ToString()));

            var hasAuthorize = authorizeAttribute.Any();
            var controllerHasAuthorize = controllerAuthorizeAttribute.Any();
            
            if (hasAuthorize || controllerHasAuthorize)
            {
                operation.Responses.Add("401", new Response { Description = "Unauthorized" });
                operation.Responses.Add("403", new Response { Description = "Forbidden" });

                operation.Security = new List<IDictionary<string, IEnumerable<string>>>
                {
                    new Dictionary<string, IEnumerable<string>> {{"oauth2", new[] {"Backend.API"}}}
                };
            }
        }
    }
}