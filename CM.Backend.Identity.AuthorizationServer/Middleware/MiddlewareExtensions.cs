using CM.Backend.API.Middleware.ServiceInfo;
using CM.Backend.Identity.AuthorizationServer.Middleware.CorrelationMiddleware;
using CM.Backend.Identity.AuthorizationServer.Middleware.GlobalExceptionFilter;
using CM.Backend.Identity.AuthorizationServer.Middleware.IPMiddleware;
using CM.Backend.Identity.AuthorizationServer.Middleware.RequestResponseMiddleware;
using Microsoft.AspNetCore.Builder;

namespace CM.Backend.API.Middleware
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseDefaultCMLoggingMiddlewares(this IApplicationBuilder builder)
        {
            return builder
                .UseMiddleware<ServiceInfoLoggingMiddleware>()
                .UseMiddleware<GlobalExceptionMiddleware>()
                .UseMiddleware<CorrelationIdMiddleware>()
                .UseMiddleware<RequestResponseLoggingMiddleware>()
                .UseMiddleware<IPLoggingMiddleware>();
        }
    }
}