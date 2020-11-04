using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace CM.Backend.Identity.AuthorizationServer.Middleware.CorrelationMiddleware
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        
        public async Task InvokeAsync(HttpContext httpContext)
        {
            httpContext.Request.Headers.TryGetValue("x-correlation-id", out var correlationIds);

            var corrId = correlationIds.FirstOrDefault() ?? httpContext.TraceIdentifier;
            
            CallContext.SetData("correlationId", corrId);
            httpContext.Response.OnStarting(state =>
            {
                var ctx = (HttpContext) state;
                ctx.Response.Headers.Add("x-correlation-id", corrId);
                return Task.FromResult(0);
                
            }, httpContext);
            
            using (LogContext.PushProperty("CorrelationId", corrId))
            {
                await _next(httpContext);
            }
        }
    }
}