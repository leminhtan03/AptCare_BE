using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Exceptions
{
    public class AppValidationException : Exception
    {
        public int StatusCode { get; }
        public object? Payload { get; }
        public AppValidationException(string message, int statusCode = 400, object? payload = null)
            : base(message) { StatusCode = statusCode; Payload = payload; }
    }
}
