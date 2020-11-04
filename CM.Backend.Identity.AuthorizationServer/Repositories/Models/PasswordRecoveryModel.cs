using System;
namespace CM.Backend.Identity.AuthorizationServer.Repositories.Models
{
    public class PasswordRecoveryModel
    {
		public Guid Id { get; set; }

		public Guid UserId { get; set; }

		public string Email { get; set; }

		public string RecoveryToken { get; set; }

		public DateTime RecoveryRequestedAt { get; set; }

		public bool IsActive { get; set; }
    }
}
