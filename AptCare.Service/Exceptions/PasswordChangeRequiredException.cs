using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Exceptions
{
    public sealed class PasswordChangeRequiredException : Exception
    {
        public int AccountId { get; }
        public PasswordChangeRequiredException(int accountId, string message = "PASSWORD_CHANGE_REQUIRED", int statusCode = StatusCodes.Status400BadRequest)
            : base(message) => AccountId = accountId;
    }
}
