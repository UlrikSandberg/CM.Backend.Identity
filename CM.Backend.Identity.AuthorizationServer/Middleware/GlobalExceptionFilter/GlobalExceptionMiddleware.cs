using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using ILogger = Serilog.ILogger;

namespace CM.Backend.Identity.AuthorizationServer.Middleware.GlobalExceptionFilter
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly IHostingEnvironment _hostingEnvironment;
 
        public GlobalExceptionMiddleware(RequestDelegate next, ILogger logger, IHostingEnvironment hostingEnvironment)
        {
            _next = next;
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
        }
        
        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "Fatal exception occured");
                await HandleExceptionAsync(httpContext, ex);
            }
        }
 
        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            
            if (_hostingEnvironment.IsProduction())
                return context.Response.WriteAsync($"An error occured. TraceId: {context.TraceIdentifier}");
            
            var details = new ErrorDetails
            {
                StatusCode = context.Response.StatusCode,
                Message = "Internal Server Error: " + exception.Message
            };
            
            return context.Response.WriteAsync(JsonConvert.SerializeObject(details));
        }
    }

}
