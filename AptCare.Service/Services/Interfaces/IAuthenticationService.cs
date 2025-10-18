using AptCare.Repository.Entities;
using AptCare.Service.Dtos.Account;
using AptCare.Service.Dtos.AuthenDto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface IAuthenticationService
    {
        Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto dto);
        Task ResendEmailVerificationOtpAsync(int accountId);
        Task<bool> VerifyEmailAsync(int accountId, string otp);
        Task<TokenResponseDto> VerifyEmailAndLoginAsync(int accountId, string otp, string deviceId);
        Task<TokenResponseDto> LoginAsync(LoginRequestDto dto);
        Task<ResetPasswordResponseDto> PasswordResetRequestAsync(PasswordResetRequestDto dto);
        Task<string> PasswordResetVerifyOtpAsync(PasswordResetVerifyOtpDto dto);
        Task PasswordResetConfirmAsync(PasswordResetConfirmDto dto);
        Task<GetOwnProfileDto> GetOwnProfile();
        Task<TokenResponseDto> FirstLoginChangePasswordAsync(FirstLoginChangePasswordDto dto);
        Task<TokenResponseDto> RefreshTokensAsync(RefreshRequestDto dto);
    }
}
