using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using CM.Backend.Identity.AuthorizationServer.Configurations;
using IdentityModel;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace CM.Backend.Identity.AuthorizationServer.Helpers
{
    public class ProfileService : IProfileService
    {
        private readonly IOptions<ClientIdSecretConfigurations> _clientIdSecretConfigs;
        private readonly IClaimIssuer _claimIssuer;

        public ProfileService(IClaimIssuer claimIssuer, IOptions<ClientIdSecretConfigurations> clientIdSecretConfigs)
        {
            _clientIdSecretConfigs = clientIdSecretConfigs;
            _claimIssuer = claimIssuer;
        }

        public Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            //If the users claims is stored in a seperate data store or should be fetch from API do logic here...
            if (context.IssuedClaims == null)
            {
                context.IssuedClaims = new List<Claim>();
            }
            context.IssuedClaims.Add(_claimIssuer.GetUserAccessClaim());

            //If the client asking is CMAdmin client issue additional access claims
            if (context.Client.ClientId.Equals(_clientIdSecretConfigs.Value.SwaggerClientId))
            {
                context.IssuedClaims.Add(new Claim(JwtClaimTypes.Name, context.Client.ClientName));
                context.IssuedClaims.Add(_claimIssuer.GetAdminAccessClaim());
            }
            
            return Task.CompletedTask;
        }

        public Task IsActiveAsync(IsActiveContext context)
        {
            //If the users activity state is stored in a seperate data store or should be fetch from API do logic here...
            
            //We might look in data store to figure out if this user is eligible to receive tokens... 
            //For now just issue IsActive = true; Not worth noting in the beginning.
            context.IsActive = true;
            
            return Task.CompletedTask;
        }
    }  
}