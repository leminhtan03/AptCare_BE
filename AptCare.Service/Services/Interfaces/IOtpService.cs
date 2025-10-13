using AptCare.Repository.Enum.OTPEnum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface IOtpService
    {
        Task<string> CreateOtpAsync(int accountId, OTPType type, TimeSpan? ttl = null, int digits = 6);
        Task<bool> VerifyOtpAsync(int accountId, string otpCode, OTPType type);
    }
}
