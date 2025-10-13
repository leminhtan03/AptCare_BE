using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.AuthenDto
{
    public class ChangePasswordComfirmDto
    {
        public string Email { get; set; }
        public string Otp { get; set; }
    }
}
