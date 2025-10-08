using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum.TokenEnum;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.AuthenDto;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Implements
{
    public class TokenService : BaseService<TokenService>, ITokenService
    {
        private readonly IConfiguration _configuration;
        public TokenService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<TokenService> logger, IMapper mapper, IConfiguration configuration) : base(unitOfWork, logger, mapper)
        {
            _configuration = configuration;
        }

        public async Task<TokenResponseDto> GenerateTokensAsync(User user, string deviceId)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var secretKey = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("UserId", user.UserId.ToString()),
                new Claim(ClaimTypes.Role, nameof(user.Account.Role))
            }),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature)
            };
            var accessToken = jwtTokenHandler.CreateToken(tokenDescriptor);
            var accessTokenString = jwtTokenHandler.WriteToken(accessToken);

            var refreshToken = new AccountToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                AccountId = user.UserId,
                DeviceInfo = deviceId,
                TokenType = TokenType.RefreshToken,
                Status = TokenStatus.Active
            };
            await _unitOfWork.GetRepository<AccountToken>().InsertAsync(refreshToken);
            await _unitOfWork.CommitAsync();

            return new TokenResponseDto
            {
                AccessToken = accessTokenString,
                RefreshToken = refreshToken.Token
            };
        }

        public async Task<TokenResponseDto> RefreshTokensAsync(string refreshToken)
        {
            var existingToken = await _unitOfWork.GetRepository<AccountToken>()
                .SingleOrDefaultAsync(predicate: urt => urt.Token == refreshToken);
            if (existingToken == null)
            {
                throw new SecurityTokenException("Refresh token không tồn tại.");
            }
            if (existingToken.Status == TokenStatus.Revoked)
            {
                throw new SecurityTokenException("Refresh token đã bị thu hồi.");
            }
            if (existingToken.ExpiresAt < DateTime.UtcNow)
            {
                throw new SecurityTokenException("Refresh token đã hết hạn.");
            }
            existingToken.Status = TokenStatus.Expired;
            _unitOfWork.GetRepository<AccountToken>().UpdateAsync(existingToken);
            await _unitOfWork.CommitAsync();
            var user = await _unitOfWork.GetRepository<User>()
                .SingleOrDefaultAsync(predicate: u => u.UserId == existingToken.AccountId, include: ex => ex.Include(ex1 => ex1.Account));
            if (user == null)
                throw new SecurityTokenException("Người dùng liên quan đến token này không còn tồn tại.");
            return await GenerateTokensAsync(user, existingToken.DeviceInfo);
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            var storedToken = await _unitOfWork.GetRepository<AccountToken>()
                .SingleOrDefaultAsync(predicate: urt => urt.Token == refreshToken);
            if (storedToken != null && !(storedToken.Status == TokenStatus.Revoked))
            {
                storedToken.Status = TokenStatus.Revoked;
                _unitOfWork.GetRepository<AccountToken>().UpdateAsync(storedToken);
                await _unitOfWork.CommitAsync();
            }
        }
    }

}
