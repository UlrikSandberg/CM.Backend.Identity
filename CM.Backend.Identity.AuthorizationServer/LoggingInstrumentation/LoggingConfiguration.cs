using System;
using CM.Backend.Identity.AuthorizationServer.Helpers;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace CM.Backend.Identity.AuthorizationServer.LoggingInstrumentation
{
    public class LoggingConfiguration
    {
        public static Serilog.LoggerConfiguration GetConfiguration(InstrumentationConfiguration config)
        {
            return new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)
                .Enrich.WithEnvironment("ASPNETCORE_ENVIRONMENT")
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .WriteTo.Console()
                .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(config.ElasticsearchUrl))
                {
                    AutoRegisterTemplate = true,
                    ModifyConnectionSettings = x => x.BasicAuthentication(config.Username, config.Password)
                })
                .WriteTo.ApplicationInsightsEvents(TelemetryConfiguration.Active);
        }
    }
}