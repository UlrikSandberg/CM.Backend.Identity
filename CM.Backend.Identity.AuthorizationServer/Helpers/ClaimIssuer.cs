using System.Collections.Generic;
using System.Security.Claims;

namespace CM.Backend.Identity.AuthorizationServer.Helpers
{
    public interface IClaimIssuer
    {
        Claim GetAdminAccessClaim();
        Claim GetUserAccessClaim();
    }
    
    public class ClaimIssuer : IClaimIssuer
    {
        private const string CMAdminAccessClaim = "CMAdminAccessClaim";
        private const string CMUserAccessClaim = "CMUserAccessClaim";

        private const string CMAdminUser = "CMAdmin";
        private const string CMUser = "CMUser";
        
        public Claim GetAdminAccessClaim()
        {
            return new Claim(CMAdminAccessClaim, CMAdminUser);
        }

        public Claim GetUserAccessClaim()
        {
            return new Claim(CMUserAccessClaim, CMUser);
        }
    }
}