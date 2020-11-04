using System.ComponentModel.DataAnnotations;

namespace CM.Backend.Identity.AuthorizationServer.ViewModels
{
    public class LoginInputModel
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
        public string ReturnUrl { get; set; }
    }
}