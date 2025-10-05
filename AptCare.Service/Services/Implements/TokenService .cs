//using AutoMapper;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;
//using Microsoft.IdentityModel.Tokens;
//using SportGo.Repository;
//using SportGo.Repository.Entities;
//using SportGo.Repository.UnitOfWork;
//using SportGo.Service.DTOs.UserDtos.Authen;
//using SportGo.Service.Services.Interfaces;
//using System;
//using System.Collections.Generic;
//using System.IdentityModel.Tokens.Jwt;
//using System.Linq;
//using System.Security.Claims;
//using System.Security.Cryptography;
//using System.Text;
//using System.Threading.Tasks;

//namespace AptCare.Service.Services.Implements
//{
//    public class TokenService : BaseService<TokenService>, ITokenService
//    {
//        private readonly IConfiguration _configuration;
//        public TokenService(IUnitOfWork<SportGoDbContext> unitOfWork, ILogger<TokenService> logger, IMapper mapper, IConfiguration configuration) : base(unitOfWork, logger, mapper)
//        {
//            _configuration = configuration;
//        }

//        public async Task<TokenResponseDto> GenerateTokensAsync(User user, string deviceId, string deviceName)
//        {
//            var jwtTokenHandler = new JwtSecurityTokenHandler();
//            var secretKey = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);

//            var tokenDescriptor = new SecurityTokenDescriptor
//            {
//                Subject = new ClaimsIdentity(new[]
//                {
//                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
//                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
//                new Claim("UserId", user.UserId.ToString()),
//                new Claim(ClaimTypes.Role, user.Role)
//            }),
//                Expires = DateTime.UtcNow.AddMinutes(15),
//                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature)
//            };
//            var accessToken = jwtTokenHandler.CreateToken(tokenDescriptor);
//            var accessTokenString = jwtTokenHandler.WriteToken(accessToken);

//            var refreshToken = new UserRefreshToken
//            {
//                UserId = user.UserId,
//                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)), // Tạo chuỗi ngẫu nhiên an toàn
//                JwtId = accessToken.Id,
//                IsUsed = false,
//                IsRevoked = false,
//                CreatedAt = DateTime.UtcNow,
//                ExpiresAt = DateTime.UtcNow.AddMonths(6), // Thời gian sống dài
//                DeviceId = deviceId,
//                DeviceName = deviceName
//            };

//            // 3. Lưu Refresh Token vào CSDL
//            await _unitOfWork.GetRepository<UserRefreshToken>().InsertAsync(refreshToken);
//            await _unitOfWork.CommitAsync();

//            return new TokenResponseDto
//            {
//                AccessToken = accessTokenString,
//                RefreshToken = refreshToken.Token
//            };
//        }

//        public async Task<TokenResponseDto> RefreshTokensAsync(string refreshToken)
//        {
//            var existingToken = await _unitOfWork.GetRepository<UserRefreshToken>()
//                .SingleOrDefaultAsync(predicate: urt => urt.Token == refreshToken);
//            if (existingToken == null)
//            {
//                throw new SecurityTokenException("Refresh token không tồn tại.");
//            }
//            if (existingToken.IsRevoked)
//            {
//                throw new SecurityTokenException("Refresh token đã bị thu hồi.");
//            }
//            if (existingToken.IsUsed)
//            {
//                throw new SecurityTokenException("Refresh token đã được sử dụng.");
//            }
//            if (existingToken.ExpiresAt < DateTime.UtcNow)
//            {
//                throw new SecurityTokenException("Refresh token đã hết hạn.");
//            }
//            existingToken.IsUsed = true;
//            _unitOfWork.GetRepository<UserRefreshToken>().UpdateAsync(existingToken);
//            await _unitOfWork.CommitAsync();
//            var user = await _unitOfWork.GetRepository<User>()
//                .SingleOrDefaultAsync(predicate: u => u.UserId == existingToken.UserId);
//            if (user == null)
//                throw new SecurityTokenException("Người dùng liên quan đến token này không còn tồn tại.");
//            return await GenerateTokensAsync(user, existingToken.DeviceId, existingToken.DeviceName);
//        }

//        public async Task RevokeRefreshTokenAsync(string refreshToken)
//        {
//            var storedToken = await _unitOfWork.GetRepository<UserRefreshToken>()
//                .SingleOrDefaultAsync(predicate: urt => urt.Token == refreshToken);
//            if (storedToken != null && !storedToken.IsRevoked)
//            {
//                storedToken.IsRevoked = true;
//                _unitOfWork.GetRepository<UserRefreshToken>().UpdateAsync(storedToken);
//                await _unitOfWork.CommitAsync();
//            }
//        }
//    }

//}
