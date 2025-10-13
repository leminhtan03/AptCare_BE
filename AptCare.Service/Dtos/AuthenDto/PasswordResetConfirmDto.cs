using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.AuthenDto
{
    public class PasswordResetConfirmDto
    {
        public int AccountId { get; set; }
        public string ResetToken { get; set; }
        public string NewPassword { get; set; }
    }
}
