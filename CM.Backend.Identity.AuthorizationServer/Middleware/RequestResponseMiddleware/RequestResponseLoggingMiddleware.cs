using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using CM.Backend.Identity.AuthorizationServer.Middleware.ServiceInfo;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Serilog;

namespace CM.Backend.Identity.AuthorizationServer.Middleware.RequestResponseMiddleware
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly IOptions<ServiceInfoConfiguration> _serviceInfo;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger logger, IOptions<ServiceInfoConfiguration> serviceInfo)
        {
            _next = next;
            _logger = logger;
            _serviceInfo = serviceInfo;
        }
        
        public async Task Invoke(HttpContext context)
        {
            context.Response.OnStarting(state =>
            {
                var ctx = (HttpContext) state;
                ctx.Response.Headers.Add("x-build-id", _serviceInfo.Value.BuildId);
                return Task.FromResult(0);
                
            }, context);
            
            var sw = new Stopwatch();
            sw.Start();
            await _next.Invoke(context);
            sw.Stop();

            LogRequest(context, sw.ElapsedMilliseconds);
        }
        
        private void LogRequest(HttpContext context, long elapsed)
        {
            try
            {
                var request = new RequestLoggingModel
                {
                    Method = context.Request.Method,
                    ContentType = context.Request.ContentType,
                    Protocol = context.Request.Protocol,
                    Query = context.Request.QueryString.ToString(),
                    RequestURI = context.Request.Path,
                    Scheme = context.Request.Scheme,
                    UserAgent = context.Request.Headers["user-agent"],
                    ClientBuildId = context.Request.Headers["X-Client-BuildId"]
                };
                var response = new ResponseLoggingModel
                {
                    StatusCode = context.Response.StatusCode,
                    ReasonPhrase = ((HttpStatusCode) context.Response.StatusCode).ToString(),
                    Date = context.Response.Headers[HttpResponseHeader.Date.ToString()],
                    Server = context.Response.Headers[HttpResponseHeader.Server.ToString()],
                    ContentType = context.Response.ContentType
                };

                _logger.Information("{@Request} received and {@Response} generated in {ElapsedMillis}", request,
                    response, elapsed);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Could not log request/response to elastic");
            }
        }
    }
}