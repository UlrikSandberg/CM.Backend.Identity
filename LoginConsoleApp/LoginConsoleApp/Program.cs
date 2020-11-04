using System;
using IdentityModel.Client;

namespace LoginConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // discover endpoints from metadata
            var disco = DiscoveryClient.GetAsync("http://localhost:5000").Result;
            if (disco.IsError)
            {
                Console.WriteLine(disco.Error);
                return;
            }

            // request token
            var tokenClient = new TokenClient(disco.TokenEndpoint, "client", "secret");
            var tokenResponse = tokenClient.RequestResourceOwnerPasswordAsync("lars", "ulrik", "scope.fullaccess").Result;

            if (tokenResponse.IsError)
            {
                Console.WriteLine(tokenResponse.Error);
                return;
            }

            Console.WriteLine(tokenResponse.Json);
            Console.WriteLine("\n\n");
        }
    }
}