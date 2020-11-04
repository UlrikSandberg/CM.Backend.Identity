using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using CM.Backend.API.Middleware;
using CM.Backend.Identity.AuthorizationServer.Configurations;
using CM.Backend.Identity.AuthorizationServer.Repositories;
using CM.Backend.Identity.AuthorizationServer.Repositories.Helpers;
using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using CM.Backend.Identity.AuthorizationServer.Handlers;
using CM.Backend.Identity.AuthorizationServer.Helpers;
using CM.Backend.Identity.AuthorizationServer.Middleware;
using CM.Backend.Identity.AuthorizationServer.Middleware.ServiceInfo;
using IdentityServer4.Stores;
using Serilog;
using Serilog.Core;
using Swashbuckle.AspNetCore.Swagger;

namespace CM.Backend.Identity.AuthorizationServer
{
    public class Startup
    {
        private readonly IHostingEnvironment _environment;

        public Startup(IConfiguration configuration, IHostingEnvironment environment)
        {
            _environment = environment;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<StorageConfigurationSettings>(Configuration.GetSection("UserPersistence"));
            services.Configure<IdentityServerUrlSettings>(Configuration.GetSection("IdentityServer"));
            services.Configure<SendGridConfigs>(Configuration.GetSection("SendGridSettings"));
            services.Configure<ServiceInfoConfiguration>
                (Configuration.GetSection(nameof(ServiceInfoConfiguration)));
            services.Configure<IPStackConfiguration>(Configuration.GetSection(nameof(IPStackConfiguration)));
            
            services.Configure<CMAdminConfiguration>(Configuration.GetSection(nameof(CMAdminConfiguration)));
            services.Configure<InstrumentationConfiguration>(
                Configuration.GetSection(nameof(InstrumentationConfiguration)));
            services.Configure<EmailAuthorityConfigurations>(
                Configuration.GetSection(nameof(EmailAuthorityConfigurations)));
            services.Configure<ClientIdSecretConfigurations>(
                Configuration.GetSection(nameof(ClientIdSecretConfigurations)));
            services.Configure<BackendServerConfiguration>(
                Configuration.GetSection(nameof(BackendServerConfiguration)));
            
            //Add framework services...
            services.AddAuthentication(x =>
                {
                    x.DefaultScheme = IdentityServerAuthenticationDefaults.AuthenticationScheme;
                })
                .AddIdentityServerAuthentication(x =>
                {
                    x.Authority = Configuration["IdentityServer:IdentityServerUrl"];
                    x.RequireHttpsMetadata = _environment.IsProduction();
                    x.ApiName = "Identity.API";
                });
            services.AddAuthorization();
            
			services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "Champagne Moments Identity", Version = "v1", Description = "Champagne Moments API for use with prior agreement" });
            });
            
            //Bind to configuration section in order to inject configurations into in memory clients.
            var clientIdSecretConfig = Configuration.GetSection(nameof(ClientIdSecretConfigurations))
                .Get<ClientIdSecretConfigurations>();
            var backendServerConfig = Configuration.GetSection(nameof(BackendServerConfiguration))
                .Get<BackendServerConfiguration>();

            var adminConfig = Configuration.GetSection(nameof(CMAdminConfiguration)).Get<CMAdminConfiguration>();

            if (_environment.IsDevelopment())
            {
                services
                    .AddIdentityServer()
                        .AddTestUsers(Config.GetSystemUsers(adminConfig.Username, adminConfig.Password))
                        .AddInMemoryIdentityResources(Config.GetIdentityResources())
                        .AddInMemoryApiResources(Config.GetAPIResources())
                        .AddInMemoryClients(Config.GetClients(clientIdSecretConfig, backendServerConfig))
                        .AddDeveloperSigningCredential(false);   
            }
            else
            {
                var thumbprint = Configuration["IdentityServer:CertificateThumbprint"];
                services
                    .AddIdentityServer()
                    .AddTestUsers(Config.GetSystemUsers(adminConfig.Username, adminConfig.Password))
                    .AddInMemoryIdentityResources(Config.GetIdentityResources())
                    .AddInMemoryApiResources(Config.GetAPIResources())
                    .AddInMemoryClients(Config.GetClients(clientIdSecretConfig, backendServerConfig))
                    .AddValidationKey(GetCertificate(thumbprint))
                    .AddSigningCredential(GetCertificate(thumbprint));
            }
            
            var cors = new DefaultCorsPolicyService(new NullLogger<DefaultCorsPolicyService>())
            {
                AllowedOrigins = { "http://localhost:49990","http://localhost:49991", "Http://localhost:49992", "https://cm-qa-identity.azurewebsites.net" , "https://api-cm-staging.azurewebsites.net", "https://internal-cm-staging.azurewebsites.net" }
                //TODO : These cors aren't really a problem. But i guess they are not ready for production
            };
            
            //Cross-origin settings
            services.AddSingleton<ICorsPolicyService>(cors);

            //Add transient make the dependency be injected as a new instance for every dependency through out the request
            services.AddTransient<IResourceOwnerPasswordValidator, PasswordValidator>();
            services.AddTransient<IProfileService, ProfileService>();
            services.AddTransient<IPersistedGrantStore, PersistedGrantStore>();
            
            //Add scoped means that the same dependency instance is scoped for the entirety of the request.
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IGrantRepository, GrantRepository>();
            services.AddScoped<IUserHandler, UserHandler>();
            services.AddScoped<IPasswordResetHandler, PasswordResetHandler>();
            services.AddScoped<IPasswordRecoveryRepository, PasswordRecoveryRepository>();
            services.AddScoped<IClaimIssuer, ClaimIssuer>();
            services.AddScoped<ClientValidationFilter>();

            var loggerConfig = LoggingInstrumentation.LoggingConfiguration.GetConfiguration(Configuration.GetSection(nameof(InstrumentationConfiguration)).Get<InstrumentationConfiguration>());            
            services.AddSingleton<ILogger>(loggerConfig.CreateLogger());

            services.AddMvc();
            services.AddCors();
            services.AddOptions();
            
            /*Structuremap isn't handling IEnumerable<Client> or other type registrations. I created my own in memory client store and the it worked fine (bar the scopes, but that is the same problem).

            This is probably due to the reason that StructureMap uses IEnumerable<T> for registering multiple concrete types against a single interface.

            I advise either

            1: wrapping the IEnumerable<Client> in another type
            2: use factory functions in the DI to construct your InMemoryXYZ types
            3: or more idomatically - make people implement their own in memory client stores and scope stores etc...*/

            /*var container = new Container(new WebRegistry());
            container.Configure(config => config.Populate(services));
            container.AssertConfigurationIsValid();
            
            return container.GetInstance<IServiceProvider>();*/

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseDefaultCMLoggingMiddlewares();
            
            if (env.IsDevelopment() || env.IsEnvironment("QA"))
            {
                app.UseDeveloperExceptionPage();
            }
			app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Champagne Moments Identity API v1");
            });

            app.UseStaticFiles();
            app.UseIdentityServer();
            app.UseCors(policy => { });
            app.UseMvcWithDefaultRoute();
        }

        private X509Certificate2 GetCertificate(string certificateThumbprint)
        {
            var certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            certStore.Open(OpenFlags.ReadOnly);

            var certCollection = certStore.Certificates.Find(
                X509FindType.FindByThumbprint, certificateThumbprint, false);
            var signingCertificate = certCollection[0];

            certStore.Close();

            return signingCertificate;
        }
    }
}
