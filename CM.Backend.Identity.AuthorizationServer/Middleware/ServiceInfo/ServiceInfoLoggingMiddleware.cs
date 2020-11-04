using System;
using System.Threading.Tasks;
using CM.Backend.Identity.AuthorizationServer.Middleware.ServiceInfo;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Serilog.Context;
using Serilog.Core.Enrichers;
using Serilog.Enrichers;
using Serilog.Enrichers.AzureWebApps;

namespace CM.Backend.API.Middleware.ServiceInfo
{
    public class ServiceInfoLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IOptions<ServiceInfoConfiguration> _serviceInfo;

        private const string ASPNETCORE_ENVIRONMENT = "ASPNETCORE_ENVIRONMENT";

        public ServiceInfoLoggingMiddleware(RequestDelegate next, IOptions<ServiceInfoConfiguration> serviceInfo)
        {
            _next = next;
            _serviceInfo = serviceInfo;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            using (LogContext.Push(
                new AzureWebAppsNameEnricher(),
                new PropertyEnricher("ServiceInfo", _serviceInfo.Value, destructureObjects: true),
                new PropertyEnricher("Environment", Environment.GetEnvironmentVariable(ASPNETCORE_ENVIRONMENT)),
                new MachineNameEnricher()))
            {
                await _next(httpContext);
            }
        }
    }
}