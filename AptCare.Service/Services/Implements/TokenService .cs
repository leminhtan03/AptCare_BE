using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum.TokenEnum;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.AuthenDto;
using AptCare.Service.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using AptCare.Service.Exceptions;

namespace AptCare.Service.Services.Implements
{
    public class TokenService : BaseService<TokenService>, ITokenService
    {
        private readonly IConfiguration _configuration;

        private const bool HASH_REFRESH_TOKENS = false;

        public TokenService(
            IUnitOfWork<AptCareSystemDBContext> unitOfWork,
            ILogger<TokenService> logger,
            IMapper mapper,
            IConfiguration configuration
        ) : base(unitOfWork, logger, mapper)
        {
            _configuration = configuration;
        }

        #region Helpers

        private static string GenerateSecureToken(int bytes = 64)
            => Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes));

        private static string HashToken(string token)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(hash);
        }

        private SymmetricSecurityKey GetJwtKey()
        {
            var key = _configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(key))
                throw new AppValidationException("Missing Jwt:Key configuration.");
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        }

        private string GetIssuerOrDefault() => _configuration["Jwt:Issuer"] ?? string.Empty;
        private string GetAudienceOrDefault() => _configuration["Jwt:Audience"] ?? string.Empty;

        #endregion

        #region Access / Refresh

        public async Task<TokenResponseDto> GenerateTokensAsync(User user, string deviceId)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var jwtHandler = new JwtSecurityTokenHandler();
            var signingKey = GetJwtKey();

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("UserId", user.UserId.ToString()),
                new Claim(ClaimTypes.Role, user.Account?.Role.ToString() ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddMinutes(120),                 // AccessToken TTL
                Issuer = GetIssuerOrDefault(),                            // optional
                Audience = GetAudienceOrDefault(),                        // optional
                SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256Signature)
            };

            var accessToken = jwtHandler.CreateToken(tokenDescriptor);
            var accessTokenString = jwtHandler.WriteToken(accessToken);
            var refreshPlain = GenerateSecureToken(64);
            var tokenToStore = HASH_REFRESH_TOKENS ? HashToken(refreshPlain) : refreshPlain;
            var refreshEntity = new AccountToken
            {
                Token = tokenToStore,
                ExpiresAt = DateTime.Now.AddDays(7),
                CreatedAt = DateTime.Now,
                AccountId = user.UserId,
                DeviceInfo = deviceId,
                TokenType = TokenType.RefreshToken,
                Status = TokenStatus.Active
            };
            await _unitOfWork.GetRepository<AccountToken>().InsertAsync(refreshEntity);
            await _unitOfWork.CommitAsync();
            return new TokenResponseDto
            {
                AccessToken = accessTokenString,
                RefreshToken = refreshPlain
            };
        }

        public async Task<TokenResponseDto> RefreshTokensAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new SecurityTokenException("Thiếu refresh token.");

            var repo = _unitOfWork.GetRepository<AccountToken>();
            var lookup = HASH_REFRESH_TOKENS ? HashToken(refreshToken) : refreshToken;

            var existingToken = await repo.SingleOrDefaultAsync(predicate: t =>
                t.Token == lookup &&
                t.TokenType == TokenType.RefreshToken);

            if (existingToken == null)
                throw new SecurityTokenException("Refresh token không tồn tại.");
            if (existingToken.Status == TokenStatus.Revoked)
                throw new SecurityTokenException("Refresh token đã bị thu hồi.");
            if (existingToken.ExpiresAt < DateTime.Now)
                throw new SecurityTokenException("Refresh token đã hết hạn.");

            existingToken.Status = TokenStatus.Expired;
            repo.UpdateAsync(existingToken);
            await _unitOfWork.CommitAsync();

            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                predicate: u => u.UserId == existingToken.AccountId,
                include: q => q.Include(x => x.Account));

            if (user == null)
                throw new SecurityTokenException("Người dùng liên quan đến token này không còn tồn tại.");

            return await GenerateTokensAsync(user, existingToken.DeviceInfo);
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken)) return;

            var repo = _unitOfWork.GetRepository<AccountToken>();
            var lookup = HASH_REFRESH_TOKENS ? HashToken(refreshToken) : refreshToken;

            var storedToken = await repo.SingleOrDefaultAsync(predicate: t =>
                t.Token == lookup &&
                t.TokenType == TokenType.RefreshToken);

            if (storedToken != null && storedToken.Status != TokenStatus.Revoked)
            {
                storedToken.Status = TokenStatus.Revoked;
                repo.UpdateAsync(storedToken);
                await _unitOfWork.CommitAsync();
            }
        }
        #endregion

        #region Password Reset (OTP → PasswordResetToken → Confirm)

        /// <summary>
        /// Tạo PasswordResetToken (one-time, TTL ngắn) cho accountId.
        /// Lưu HASH trong DB. Revoke token reset còn hiệu lực trước đó.
        /// </summary>
        public async Task<string> CreatePasswordResetTokenAsync(int accountId, TimeSpan? ttl = null)
        {
            var repo = _unitOfWork.GetRepository<AccountToken>();
            var oldTokens = await repo.GetListAsync(predicate: t =>
                t.AccountId == accountId &&
                t.TokenType == TokenType.PasswordResetToken &&
                t.Status == TokenStatus.Active &&
                t.ExpiresAt > DateTime.Now);

            foreach (var t in oldTokens)
            {
                t.Status = TokenStatus.Revoked;
                repo.UpdateAsync(t);
            }

            var lifetime = ttl ?? TimeSpan.FromMinutes(10);
            var plain = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var entity = new AccountToken
            {
                Token = HashToken(plain), // LƯU HASH
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.Add(lifetime),
                Status = TokenStatus.Active,
                TokenType = TokenType.PasswordResetToken,
                DeviceInfo = "ForRested",
                AccountId = accountId
            };
            await repo.InsertAsync(entity);
            await _unitOfWork.CommitAsync();
            return plain;
        }

        /// <summary>
        /// Kiểm tra reset token còn hiệu lực (không consume).
        /// </summary>
        public async Task<bool> VerifyPasswordResetTokenAsync(int accountId, string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;

            var repo = _unitOfWork.GetRepository<AccountToken>();
            var hashed = HashToken(token);

            var rec = await repo.SingleOrDefaultAsync(predicate: t =>
                t.AccountId == accountId &&
                t.TokenType == TokenType.PasswordResetToken &&
                t.Token == hashed &&
                t.Status == TokenStatus.Active);

            if (rec == null) return false;
            if (rec.ExpiresAt <= DateTime.Now) return false;
            return true;
        }

        /// <summary>
        /// Consume reset token (one-time) nếu hợp lệ & còn hạn.
        /// </summary>
        public async Task<bool> ConsumePasswordResetTokenAsync(int accountId, string resetToken)
        {
            if (string.IsNullOrWhiteSpace(resetToken)) return false;

            var repo = _unitOfWork.GetRepository<AccountToken>();
            var hashed = HashToken(resetToken);

            var rec = await repo.SingleOrDefaultAsync(predicate: t =>
                t.AccountId == accountId &&
                t.TokenType == TokenType.PasswordResetToken &&
                t.Status == TokenStatus.Active &&
                t.ExpiresAt > DateTime.Now &&
                t.Token == hashed);

            if (rec == null) return false;

            rec.Status = TokenStatus.Consumed;
            repo.UpdateAsync(rec);
            await _unitOfWork.CommitAsync();
            return true;
        }

        /// <summary>
        /// Thu hồi toàn bộ RefreshToken của account (buộc đăng nhập lại sau khi đổi mật khẩu).
        /// </summary>
        public async Task RevokeAllRefreshTokensAsync(int accountId)
        {
            var repo = _unitOfWork.GetRepository<AccountToken>();
            var list = await repo.GetListAsync(predicate: t =>
                t.AccountId == accountId &&
                t.TokenType == TokenType.RefreshToken &&
                (t.Status == TokenStatus.Active || t.ExpiresAt > DateTime.Now));

            foreach (var t in list)
            {
                t.Status = TokenStatus.Revoked;
                repo.UpdateAsync(t);
            }
            await _unitOfWork.CommitAsync();
        }

        #endregion
    }
}
