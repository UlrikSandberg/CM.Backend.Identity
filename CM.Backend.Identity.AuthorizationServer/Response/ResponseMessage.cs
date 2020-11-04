using System;
using IdentityModel.Client;
using IdentityServer4.Models;

namespace CM.Backend.Identity.AuthorizationServer.Response
{
    public class ResponseMessage
    {
		public bool IsSuccessful { get; private set; }
        public TokenResponse Token { get; private set; }
        public Exception Exception { get; private set; }
        public string Message { get; private set; }

        public ResponseMessage(bool isSuccessful, TokenResponse token = null, Exception exception = null, string message = null)
        {
            IsSuccessful = isSuccessful;
            Token = token;
            Exception = exception;
            Message = message;
        }

        public static ResponseMessage Unsuccessful(string message = null, TokenResponse token = null, Exception exception = null)
        {
            return new ResponseMessage(false, token , exception, message);
        }

        public static ResponseMessage Success()
        {
            return new ResponseMessage(true);
        }
    }
}
