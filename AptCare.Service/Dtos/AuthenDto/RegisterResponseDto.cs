using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.AuthenDto
{
    public class RegisterResponseDto
    {
        public int AccountId { get; set; }
        public bool OtpSent { get; set; }
        public string Message { get; set; }
    }
}
