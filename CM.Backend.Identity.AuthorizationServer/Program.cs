using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CM.Backend.Identity.AuthorizationServer.Helpers;
using CM.Backend.Identity.AuthorizationServer.LoggingInstrumentation;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace CM.Backend.Identity.AuthorizationServer
{
    public class Program
    {
        public static IHostingEnvironment HostingEnvironment { get; set; }
        public static IConfiguration Configuration { get; set; }
        
        public static void Main(string[] args)
        {
            //Build config to get config files
            var config = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();
            
            var instrumentationConfig = new InstrumentationConfiguration();
            
            //Map instrumentationConfiguration section into config file
            config.GetSection(nameof(InstrumentationConfiguration)).Bind(instrumentationConfig);
            
            //Initiate config with configSection
            Log.Logger = LoggingConfiguration.GetConfiguration(instrumentationConfig).CreateLogger();
            
            try
            {
                Log.Information("Starting API host");

                CreateWebHostBuilder(args).Build().Run();
            }
            catch (Exception e)
            {
                Log.Fatal(e, "API host could not start or terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseApplicationInsights()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    HostingEnvironment = hostingContext.HostingEnvironment;
                    Configuration = config.Build();
                })
                .UseStartup<Startup>();
    }
}
