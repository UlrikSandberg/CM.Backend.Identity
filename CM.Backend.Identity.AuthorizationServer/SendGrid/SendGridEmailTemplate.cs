using System.Collections.Generic;

namespace CM.Backend.Identity.AuthorizationServer.SendGrid
{
    public class SendGridEmailTemplate<T> where T : class
    {
        public SendGridEmailTemplate(string templateId)
        {
            template_id = templateId;
        }
        
        public List<Personalization<T>> personalizations { get; set; } = new List<Personalization<T>>();
        public From from { get; set; }
        public ReplyTo reply_to { get; set; }
        public string template_id { get; set; }


        public void AddPersonalization(Personalization<T> settings)
        {
            personalizations.Add(settings);
        }
    }
    
    public class To
    {
        public string email { get; set; }
        public string name { get; set; }
    }

    public class Personalization<T>
    {
        public List<To> to { get; set; }
        public T dynamic_template_data { get; set; }
    }

    public class From
    {
        public string email { get; set; }
        public string name { get; set; }
    }

    public class ReplyTo
    {
        public string email { get; set; }
        public string name { get; set; }
    }
}