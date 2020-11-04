using IdentityModel.Client;

namespace CM.Backend.Identity.AuthorizationServer.ResponseModels
{
    public class CreateUserResponseModel
    {
        public TokenResponse TokenResponse { get; set; }
    }
}