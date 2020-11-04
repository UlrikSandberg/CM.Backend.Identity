namespace CM.Backend.Identity.AuthorizationServer.ViewModels
{
    public class LoginViewModel
    {
        public string Error { get; set; }
        public string ReturnUrl { get; set; }
        public bool IsSuccessfull { get; set; }
    }
}