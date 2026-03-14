using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace UserService.API.Infra
{
    public class InternalServerError : ObjectResult
    {
        public InternalServerError(string message)
            : this(new { Message = message })
        {
            
        }

        public InternalServerError(object value) 
            : base(value)
        {
            StatusCode = (int)HttpStatusCode.InternalServerError;
        }
    }
}
