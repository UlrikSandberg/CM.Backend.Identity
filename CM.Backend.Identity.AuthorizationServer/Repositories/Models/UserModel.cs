using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace CM.Backend.Identity.AuthorizationServer
{
    public class UserModel
	{
		[BsonId]
		public Guid Id { get; set; }
		
        public string Email { get; set; }

        public string Password { get; set; }

		public bool IsActive { get; set; }

		public List<string> Claims { get; set; }

    }
}