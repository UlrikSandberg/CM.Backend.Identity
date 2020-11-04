using System.Net.Http;
using System.Threading.Tasks;

namespace CM.Backend.Identity.AuthorizationServer.SendGrid
{
    public class SendGridClient
    {
        private HttpClient client;

        private const string baseurl = "https://api.sendgrid.com/v3/mail/send"; //TODO : Hardcoded values, though doesn't matter since this never changes?
        
        public SendGridClient(string apiKey)
        {
            client = new HttpClient();
            
            client.DefaultRequestHeaders.Add("authorization", "Bearer " + apiKey);
           
            client.DefaultRequestHeaders
                .Accept
                .Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        }

        public async Task<HttpResponseMessage> SendTransactionalEmail(string json)
        {
            StringContent content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            return await client.PostAsync(baseurl, content);
        }
    }
}