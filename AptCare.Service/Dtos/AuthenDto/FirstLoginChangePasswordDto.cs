using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.AuthenDto
{
    public sealed class FirstLoginChangePasswordDto
    {
        public int AccountId { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string DeviceInfo { get; set; }
    }
}
