using System;
using System.Collections.Generic;
using System.Security.Claims;
using CM.Backend.Identity.AuthorizationServer.Configurations;
using CM.Backend.Identity.AuthorizationServer.Helpers;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Test;
using Microsoft.Extensions.Options;

namespace CM.Backend.Identity.AuthorizationServer
{
    public class Config
    {
        private const string CMUser = "CMUser";
        private const string CMAdmin = "CMAdmin";
        
        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new IdentityResource[]
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile()
            };
        }

        public static IEnumerable<ApiResource> GetAPIResources()
        {
            return new[]
            {
                new ApiResource("Backend.API", "Full access to entire backend"),
                new ApiResource("Identity.API", "Full acces to entire Identity.API") 
            };
        }

        public static IEnumerable<Client> GetClients(ClientIdSecretConfigurations clientIdSecretConfig, BackendServerConfiguration backendServerConfiguration)
        {
            return new[]
            {
                new Client //<-- CMApp client issues authentication token through the ResourceOwnerPassword flow
                {
                    ClientId = clientIdSecretConfig.AppClientId,
                    AllowedGrantTypes = GrantTypes.ResourceOwnerPasswordAndClientCredentials,
                    
                    AccessTokenLifetime = 3600 * 24 * 30, //30 Days
                    AbsoluteRefreshTokenLifetime = 3600 * 24 * 30, //30 Days
                    
                    AllowOfflineAccess = true, //<-- Enables refresh tokens for this client.
                    
                    ClientSecrets =
                    {
                        new Secret
                        {
                            Type = IdentityServerConstants.SecretTypes.SharedSecret,
                            Value = clientIdSecretConfig.AppClientSecret.Sha256()
                        }
                    },
                    AllowedScopes = { "Backend.API", "Identity.API" }, //<-- Which scopes are this client allowed to request. In this case CMApp will only ever be able to request access to our Backend.API
                    
                    //TODO: Put CORS origins in config, as this differs between environments
                    AllowedCorsOrigins = { "https://api.qa.champagnemoments.eu", "https://api-qa.champagnemoments.eu", "https://idp-qa.champagnemoments.eu", "http://localhost:49991", "http://localhost:49992" }
                },
                
                new Client //<-- Swagger UI client, accessed through the implicit flow. issues a return callback to swagger upon login with authenticated token
                {
                    ClientId = clientIdSecretConfig.SwaggerClientId,
                    ClientName = "Swagger UI",
                    AllowedGrantTypes = GrantTypes.Implicit,
                    AllowAccessTokensViaBrowser = true,
                    RequireConsent = false,
                    AllowRememberConsent = false,
                    RedirectUris =
                    {
                        $"{backendServerConfiguration.BackendServerUrl}/swagger/oauth2-redirect.html",
                        $"{backendServerConfiguration.BackendServerUrl}/swagger/o2c.html"
                    },
                    AllowedScopes = { "Backend.API" },
                    AllowedCorsOrigins = { backendServerConfiguration.BackendServerUrl, "https://api-qa.champagnemoments.eu", "https://idp-qa.champagnemoments.eu", "http://localhost:49991", "http://localhost:49992" }
                }
            };
        }

        public static List<TestUser> GetSystemUsers(string username, string password)
        {
            IClaimIssuer claimIssuer = new ClaimIssuer();
            
            return new List<TestUser>
            {
                new TestUser
                {
                    SubjectId = "1", Username = username, Password = password //<-- CM-Admin User! 
                }
            };
        }
    }
}