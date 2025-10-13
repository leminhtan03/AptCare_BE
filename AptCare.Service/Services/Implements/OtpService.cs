using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum.OTPEnum;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Implements
{
    public class OtpService : BaseService<OtpService>, IOtpService
    {
        public OtpService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<OtpService> logger, IMapper mapper) : base(unitOfWork, logger, mapper)
        {
        }

        public async Task<string> CreateOtpAsync(int accountId, OTPType type, TimeSpan? ttl = null, int digits = 6)
        {
            var lifetime = ttl ?? TimeSpan.FromMinutes(5);

            // 2) Hủy các OTP còn hiệu lực trước đó (tránh spam/đè)
            var repo = _unitOfWork.GetRepository<AccountOTPHistory>();
            var actives = await repo.GetListAsync(predicate: p =>
                p.AccountId == accountId &&
                p.OTPType == type &&
                p.Status == OTPStatus.Active &&
                p.ExpiresAt > DateTime.UtcNow);

            foreach (var it in actives)
            {
                it.Status = OTPStatus.Revoked;
                repo.UpdateAsync(it);
            }

            var otp = GenerateNumericOtp(digits);
            var entity = new AccountOTPHistory
            {
                AccountId = accountId,
                OTPCode = HashString(otp),
                OTPType = type,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(lifetime),
                Status = OTPStatus.Active
            };
            await repo.InsertAsync(entity);
            await _unitOfWork.CommitAsync();

            return otp;
        }

        public async Task<bool> VerifyOtpAsync(int accountId, string otpCode, OTPType type)
        {
            var repo = _unitOfWork.GetRepository<AccountOTPHistory>();
            var now = DateTime.UtcNow;

            // Lấy OTP mới nhất còn active
            var otp = (await repo.GetListAsync(predicate: p =>
                        p.AccountId == accountId &&
                        p.OTPType == type &&
                        p.Status == OTPStatus.Active &&
                        p.ExpiresAt > now))
                      .OrderByDescending(x => x.CreatedAt)
                      .FirstOrDefault();

            if (otp == null) return false;

            if (!SlowEquals(otp.OTPCode, HashString(otpCode))) return false;

            // Đúng → đánh dấu Verified & Active => Consumed (1 lần dùng)
            otp.VerifiedAt = now;
            otp.Status = OTPStatus.Verified; // hoặc Consumed tuỳ enum bạn định nghĩa
            repo.UpdateAsync(otp);
            await _unitOfWork.CommitAsync();
            return true;
        }

        private static string GenerateNumericOtp(int digits)
        {
            var bytes = RandomNumberGenerator.GetBytes(digits);
            var sb = new StringBuilder(digits);
            foreach (var b in bytes) sb.Append((b % 10).ToString());
            return sb.ToString();
        }
        private static string HashString(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes);
        }
        private static bool SlowEquals(string a, string b)
        {
            if (a?.Length != b?.Length) return false;
            var result = 0;
            for (int i = 0; i < a.Length; i++) result |= a[i] ^ b[i];
            return result == 0;
        }
    }
}
