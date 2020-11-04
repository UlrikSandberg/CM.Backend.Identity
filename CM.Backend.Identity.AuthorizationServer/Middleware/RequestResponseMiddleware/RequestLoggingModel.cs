namespace CM.Backend.Identity.AuthorizationServer.Middleware.RequestResponseMiddleware
{
    public class RequestLoggingModel
    {
        public string Method { get; set; }
        public string RequestURI { get; set; }
        public string Protocol { get; set; }
        public string Query { get; set; }
        public string Scheme { get; set; }
        public string ContentType { get; set; }
        public string UserAgent { get; set; }
        public string ClientBuildId { get; set; }
    }
}