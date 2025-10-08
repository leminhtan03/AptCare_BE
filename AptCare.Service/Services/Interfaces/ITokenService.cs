
using AptCare.Repository.Entities;
using AptCare.Service.Dtos.AuthenDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface ITokenService
    {
        Task<TokenResponseDto> GenerateTokensAsync(User user, string deviceId);
        Task<TokenResponseDto> RefreshTokensAsync(string refreshToken);
        Task RevokeRefreshTokenAsync(string refreshToken);
    }
}
