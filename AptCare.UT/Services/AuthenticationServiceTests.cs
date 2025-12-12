using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Enum.OTPEnum;
using AptCare.Repository.Enum.TokenEnum;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.Account;
using AptCare.Service.Dtos.AuthenDto;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Implements;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

namespace AptCare.UT.Services
{
    public class AuthenticationServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _mockUnitOfWork;
        private readonly Mock<IPasswordHasher<Account>> _mockPasswordHasher;
        private readonly Mock<IOtpService> _mockOtpService;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly Mock<IMailSenderService> _mockMailSenderService;
        private readonly Mock<IUserContext> _mockUserContext;
        private readonly Mock<ILogger<AuthenticationService>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly AuthenticationService _authenticationService;
        private readonly Mock<IGenericRepository<Account>> _mockAccountRepo;
        private readonly Mock<IGenericRepository<User>> _mockUserRepo;
        private readonly Mock<IGenericRepository<AccountToken>> _mockAccountTokenRepo;

        public AuthenticationServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork<AptCareSystemDBContext>>();
            _mockPasswordHasher = new Mock<IPasswordHasher<Account>>();
            _mockOtpService = new Mock<IOtpService>();
            _mockTokenService = new Mock<ITokenService>();
            _mockMailSenderService = new Mock<IMailSenderService>();
            _mockUserContext = new Mock<IUserContext>();
            _mockLogger = new Mock<ILogger<AuthenticationService>>();
            _mockMapper = new Mock<IMapper>();

            _mockAccountRepo = new Mock<IGenericRepository<Account>>();
            _mockUserRepo = new Mock<IGenericRepository<User>>();
            _mockAccountTokenRepo = new Mock<IGenericRepository<AccountToken>>();

            _mockUnitOfWork.Setup(u => u.GetRepository<Account>()).Returns(_mockAccountRepo.Object);
            _mockUnitOfWork.Setup(u => u.GetRepository<User>()).Returns(_mockUserRepo.Object);
            _mockUnitOfWork.Setup(u => u.GetRepository<AccountToken>()).Returns(_mockAccountTokenRepo.Object);

            _authenticationService = new AuthenticationService(
                _mockUnitOfWork.Object,
                (Service.Services.Interfaces.RabbitMQ.IRabbitMQService)_mockMailSenderService.Object,
                _mockPasswordHasher.Object,
                _mockOtpService.Object,
                _mockUserContext.Object,
                _mockTokenService.Object,
                _mockLogger.Object,
                _mockMapper.Object
            );
        }

        #region RegisterAsync Tests

        [Fact]
        public async Task RegisterAsync_WithValidData_ReturnsSuccessResponse()
        {
            // Arrange
            var registerDto = new RegisterRequestDto
            {
                Email = "newuser@test.com",
                Password = "Password123!@#"
            };

            var user = new User
            {
                UserId = 1,
                Email = registerDto.Email,
                FirstName = "Test",
                LastName = "User"
            };

            _mockAccountRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Account, bool>>>(),
                null, null
            )).ReturnsAsync((Account)null);

            _mockUserRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                null, null
            )).ReturnsAsync(user);

            _mockPasswordHasher.Setup(p => p.HashPassword(It.IsAny<Account>(), It.IsAny<string>()))
                .Returns("hashed_password");

            _mockOtpService.Setup(o => o.CreateOtpAsync(
                It.IsAny<int>(),
                OTPType.EmailVerification,
                It.IsAny<TimeSpan>(),
                6
            )).ReturnsAsync("123456");

            _mockMailSenderService.Setup(m => m.SendEmailWithTemplateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>()
            )).Returns(Task.CompletedTask);

            // Act
            var result = await _authenticationService.RegisterAsync(registerDto);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.OtpSent);
            Assert.Equal(user.UserId, result.AccountId);
            _mockAccountRepo.Verify(r => r.InsertAsync(It.IsAny<Account>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_WithExistingEmail_ThrowsException()
        {
            // Arrange
            var registerDto = new RegisterRequestDto
            {
                Email = "existing@test.com",
                Password = "Password123!@#"
            };

            _mockAccountRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Account, bool>>>(),
                null, null
            )).ReturnsAsync(new Account { AccountId = 1 });

            // Act & Assert
            await Assert.ThrowsAsync<AppValidationException>(() =>
                _authenticationService.RegisterAsync(registerDto)
            );
        }

        [Fact]
        public async Task RegisterAsync_WithEmailNotInSystem_ThrowsException()
        {
            // Arrange
            var registerDto = new RegisterRequestDto
            {
                Email = "notinsystem@test.com",
                Password = "Password123!@#"
            };

            _mockAccountRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Account, bool>>>(),
                null, null
            )).ReturnsAsync((Account)null);

            _mockUserRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                null, null
            )).ReturnsAsync((User)null);

            // Act & Assert
            await Assert.ThrowsAsync<AppValidationException>(() =>
                _authenticationService.RegisterAsync(registerDto)
            );
        }

        #endregion

        #region LoginAsync Tests

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsTokenResponse()
        {
            // Arrange
            var loginDto = new LoginRequestDto
            {
                UsernameOrEmail = "user@test.com",
                Password = "Password123!@#",
                DeviceInfo = "TestDevice"
            };

            var account = new Account
            {
                AccountId = 1,
                Username = loginDto.UsernameOrEmail,
                PasswordHash = "hashed_password",
                EmailConfirmed = true,
                MustChangePassword = false,
                User = new User { UserId = 1, Email = loginDto.UsernameOrEmail }
            };

            var tokenResponse = new TokenResponseDto
            {
                AccessToken = "access_token",
                RefreshToken = "refresh_token"
            };

            _mockAccountRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Account, bool>>>(),
                It.IsAny<Func<IQueryable<Account>, IOrderedQueryable<Account>>>(),
                It.IsAny<Func<IQueryable<Account>, IIncludableQueryable<Account, object>>>()
            )).ReturnsAsync(account);

            _mockPasswordHasher.Setup(p => p.VerifyHashedPassword(
                It.IsAny<Account>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).Returns(PasswordVerificationResult.Success);

            _mockUserRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                null,
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync(account.User);

            _mockTokenService.Setup(t => t.GenerateTokensAsync(
                It.IsAny<User>(),
                It.IsAny<string>()
            )).ReturnsAsync(tokenResponse);

            // Act
            var result = await _authenticationService.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(tokenResponse.AccessToken, result.AccessToken);
            Assert.Equal(tokenResponse.RefreshToken, result.RefreshToken);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidCredentials_ThrowsException()
        {
            // Arrange
            var loginDto = new LoginRequestDto
            {
                UsernameOrEmail = "user@test.com",
                Password = "WrongPassword",
                DeviceInfo = "TestDevice"
            };

            var account = new Account
            {
                AccountId = 1,
                Username = loginDto.UsernameOrEmail,
                PasswordHash = "hashed_password",
                EmailConfirmed = true,
                User = new User()
            };

            _mockAccountRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Account, bool>>>(),
                null,
                It.IsAny<Func<IQueryable<Account>, IIncludableQueryable<Account, object>>>()
            )).ReturnsAsync(account);

            _mockPasswordHasher.Setup(p => p.VerifyHashedPassword(
                It.IsAny<Account>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).Returns(PasswordVerificationResult.Failed);

            // Act & Assert
            await Assert.ThrowsAsync<AppValidationException>(() =>
                _authenticationService.LoginAsync(loginDto)
            );
        }

        [Fact]
        public async Task LoginAsync_WithUnverifiedEmail_ThrowsException()
        {
            // Arrange
            var loginDto = new LoginRequestDto
            {
                UsernameOrEmail = "user@test.com",
                Password = "Password123!@#",
                DeviceInfo = "TestDevice"
            };

            var account = new Account
            {
                AccountId = 1,
                Username = loginDto.UsernameOrEmail,
                PasswordHash = "hashed_password",
                EmailConfirmed = false,
                User = new User()
            };

            _mockAccountRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Account, bool>>>(),
                null,
                It.IsAny<Func<IQueryable<Account>, IIncludableQueryable<Account, object>>>()
            )).ReturnsAsync(account);

            _mockPasswordHasher.Setup(p => p.VerifyHashedPassword(
                It.IsAny<Account>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).Returns(PasswordVerificationResult.Success);

            // Act & Assert
            await Assert.ThrowsAsync<AppValidationException>(() =>
                _authenticationService.LoginAsync(loginDto)
            );
        }

        [Fact]
        public async Task LoginAsync_WithMustChangePassword_ThrowsPasswordChangeRequiredException()
        {
            // Arrange
            var loginDto = new LoginRequestDto
            {
                UsernameOrEmail = "user@test.com",
                Password = "Password123!@#",
                DeviceInfo = "TestDevice"
            };

            var account = new Account
            {
                AccountId = 1,
                Username = loginDto.UsernameOrEmail,
                PasswordHash = "hashed_password",
                EmailConfirmed = true,
                MustChangePassword = true,
                User = new User()
            };

            _mockAccountRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Account, bool>>>(),
                null,
                It.IsAny<Func<IQueryable<Account>, IIncludableQueryable<Account, object>>>()
            )).ReturnsAsync(account);

            _mockPasswordHasher.Setup(p => p.VerifyHashedPassword(
                It.IsAny<Account>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).Returns(PasswordVerificationResult.Success);

            // Act & Assert
            await Assert.ThrowsAsync<PasswordChangeRequiredException>(() =>
                _authenticationService.LoginAsync(loginDto)
            );
        }

        #endregion

        #region VerifyEmailAsync Tests

        [Fact]
        public async Task VerifyEmailAsync_WithValidOtp_ReturnsTrue()
        {
            // Arrange
            var accountId = 1;
            var otp = "123456";
            var account = new Account
            {
                AccountId = accountId,
                EmailConfirmed = false,
                User = new User()
            };

            _mockOtpService.Setup(o => o.VerifyOtpAsync(
                accountId,
                otp,
                OTPType.EmailVerification
            )).ReturnsAsync(true);

            _mockAccountRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Account, bool>>>(),
                null,
                It.IsAny<Func<IQueryable<Account>, IIncludableQueryable<Account, object>>>()
            )).ReturnsAsync(account);

            // Act
            var result = await _authenticationService.VerifyEmailAsync(accountId, otp);

            // Assert
            Assert.True(result);
            _mockAccountRepo.Verify(r => r.UpdateAsync(It.IsAny<Account>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task VerifyEmailAsync_WithInvalidOtp_ReturnsFalse()
        {
            // Arrange
            var accountId = 1;
            var otp = "wrong_otp";

            _mockOtpService.Setup(o => o.VerifyOtpAsync(
                accountId,
                otp,
                OTPType.EmailVerification
            )).ReturnsAsync(false);

            // Act
            var result = await _authenticationService.VerifyEmailAsync(accountId, otp);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region PasswordResetRequestAsync Tests

        [Fact]
        public async Task PasswordResetRequestAsync_WithValidEmail_SendsOtp()
        {
            // Arrange
            var dto = new PasswordResetRequestDto
            {
                Email = "user@test.com"
            };

            var user = new User
            {
                UserId = 1,
                Email = dto.Email,
                FirstName = "Test",
                LastName = "User",
                Account = new Account { AccountId = 1 }
            };

            _mockUserRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                null,
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync(user);

            _mockOtpService.Setup(o => o.CreateOtpAsync(
                It.IsAny<int>(),
                OTPType.PasswordReset,
                It.IsAny<TimeSpan>(),
                6
            )).ReturnsAsync("123456");

            _mockMailSenderService.Setup(m => m.SendEmailWithTemplateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>()
            )).Returns(Task.CompletedTask);

            // Act
            var result = await _authenticationService.PasswordResetRequestAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Account.AccountId, result.AccountId);
            _mockMailSenderService.Verify(m => m.SendEmailWithTemplateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>()
            ), Times.Once);
        }

        [Fact]
        public async Task PasswordResetRequestAsync_WithInvalidEmail_ThrowsException()
        {
            // Arrange
            var dto = new PasswordResetRequestDto
            {
                Email = "nonexistent@test.com"
            };

            _mockUserRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                null,
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync((User)null);

            // Act & Assert
            await Assert.ThrowsAsync<AppValidationException>(() =>
                _authenticationService.PasswordResetRequestAsync(dto)
            );
        }

        #endregion

        #region RegisterFCMTokenAsync Tests

        [Fact]
        public async Task RegisterFCMTokenAsync_WithNewToken_InsertsToken()
        {
            // Arrange
            var userId = 1;
            var dto = new FCMRequestDto
            {
                FcmToken = "new_fcm_token",
                DeviceInfo = "TestDevice"
            };

            _mockUserContext.Setup(u => u.CurrentUserId).Returns(userId);
            _mockAccountTokenRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<AccountToken, bool>>>(),
                null
            )).ReturnsAsync(false);

            // Act
            var result = await _authenticationService.RegisterFCMTokenAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Thành công", result);
            _mockAccountTokenRepo.Verify(r => r.InsertAsync(It.IsAny<AccountToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task RegisterFCMTokenAsync_WithExistingToken_DoesNotInsert()
        {
            // Arrange
            var userId = 1;
            var dto = new FCMRequestDto
            {
                FcmToken = "existing_token",
                DeviceInfo = "TestDevice"
            };

            _mockUserContext.Setup(u => u.CurrentUserId).Returns(userId);
            _mockAccountTokenRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<AccountToken, bool>>>(),
                null
            )).ReturnsAsync(true);

            // Act
            var result = await _authenticationService.RegisterFCMTokenAsync(dto);

            // Assert
            Assert.NotNull(result);
            _mockAccountTokenRepo.Verify(r => r.InsertAsync(It.IsAny<AccountToken>()), Times.Never);
        }

        #endregion

        #region RefreshFCMTokenAsync Tests

        [Fact]
        public async Task RefreshFCMTokenAsync_WithValidOldToken_UpdatesToken()
        {
            // Arrange
            var userId = 1;
            var dto = new FCMRequestDto
            {
                FcmToken = "new_fcm_token",
                DeviceInfo = "TestDevice"
            };

            var oldToken = new AccountToken
            {
                TokenId = 1,
                AccountId = userId,
                Token = "old_token",
                DeviceInfo = dto.DeviceInfo,
                Status = TokenStatus.Active
            };

            _mockUserContext.Setup(u => u.CurrentUserId).Returns(userId);
            _mockAccountTokenRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<AccountToken, bool>>>(),
                null, null
            )).ReturnsAsync(oldToken);

            // Act
            var result = await _authenticationService.RefreshFCMTokenAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Thành công", result);
            _mockAccountTokenRepo.Verify(r => r.UpdateAsync(It.IsAny<AccountToken>()), Times.Once);
            _mockAccountTokenRepo.Verify(r => r.InsertAsync(It.IsAny<AccountToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task RefreshFCMTokenAsync_WithNoOldToken_ThrowsException()
        {
            // Arrange
            var userId = 1;
            var dto = new FCMRequestDto
            {
                FcmToken = "new_fcm_token",
                DeviceInfo = "TestDevice"
            };

            _mockUserContext.Setup(u => u.CurrentUserId).Returns(userId);
            _mockAccountTokenRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<AccountToken, bool>>>(),
                null, null
            )).ReturnsAsync((AccountToken)null);

            // Act & Assert
            await Assert.ThrowsAsync<AppValidationException>(() =>
                _authenticationService.RefreshFCMTokenAsync(dto)
            );
        }

        #endregion
    }
}