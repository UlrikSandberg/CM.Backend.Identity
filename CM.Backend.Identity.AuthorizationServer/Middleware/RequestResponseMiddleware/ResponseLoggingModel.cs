namespace CM.Backend.Identity.AuthorizationServer.Middleware.RequestResponseMiddleware
{
    public class ResponseLoggingModel
    {
        public int StatusCode { get; set; }
        public string ReasonPhrase { get; set; }
        public string Date { get; set; }
        public string Server { get; set; }
        public string ContentType { get; set; }
    }
}