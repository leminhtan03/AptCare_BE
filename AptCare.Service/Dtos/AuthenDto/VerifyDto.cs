using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.AuthenDto
{
    public class VerifyDto
    {
        public int AccountId { get; set; }
        public string Otp { get; set; }
    }
    public class VerifyAndLoginDto : VerifyDto
    {
        public string DeviceId { get; set; }
    }
}
