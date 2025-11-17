using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum.OTPEnum;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Services.Implements;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

namespace AptCare.UT.Services
{
    public class OtpServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _mockUnitOfWork;
        private readonly Mock<ILogger<OtpService>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly OtpService _otpService;
        private readonly Mock<IGenericRepository<AccountOTPHistory>> _mockOtpHistoryRepo;

        public OtpServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork<AptCareSystemDBContext>>();
            _mockLogger = new Mock<ILogger<OtpService>>();
            _mockMapper = new Mock<IMapper>();

            _mockOtpHistoryRepo = new Mock<IGenericRepository<AccountOTPHistory>>();

            _mockUnitOfWork.Setup(u => u.GetRepository<AccountOTPHistory>()).Returns(_mockOtpHistoryRepo.Object);

            _otpService = new OtpService(
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mockMapper.Object
            );
        }

        #region CreateOtpAsync Tests

        [Fact]
        public async Task CreateOtpAsync_WithValidData_CreatesOtp()
        {
            // Arrange
            var accountId = 1;
            var otpType = OTPType.EmailVerification;
            var ttl = TimeSpan.FromMinutes(5);
            var digits = 6;

            _mockOtpHistoryRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<AccountOTPHistory, bool>>>(),
                null, null
            )).ReturnsAsync(new List<AccountOTPHistory>());

            // Act
            var result = await _otpService.CreateOtpAsync(accountId, otpType, ttl, digits);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(digits, result.Length);
            Assert.All(result, c => Assert.True(char.IsDigit(c)));
            _mockOtpHistoryRepo.Verify(r => r.InsertAsync(It.IsAny<AccountOTPHistory>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateOtpAsync_WithExistingActiveOtp_RevokesOldOtp()
        {
            // Arrange
            var accountId = 1;
            var otpType = OTPType.EmailVerification;
            var existingOtps = new List<AccountOTPHistory>
            {
                new AccountOTPHistory
                {
                    AccountId = accountId,
                    OTPType = otpType,
                    Status = OTPStatus.Active,
                    ExpiresAt = DateTime.Now.AddMinutes(5)
                }
            };

            _mockOtpHistoryRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<AccountOTPHistory, bool>>>(),
                null, null
            )).ReturnsAsync(existingOtps);

            // Act
            var result = await _otpService.CreateOtpAsync(accountId, otpType);

            // Assert
            Assert.NotNull(result);
            _mockOtpHistoryRepo.Verify(r => r.UpdateAsync(It.IsAny<AccountOTPHistory>()), Times.Once);
            _mockOtpHistoryRepo.Verify(r => r.InsertAsync(It.IsAny<AccountOTPHistory>()), Times.Once);
        }

        [Fact]
        public async Task CreateOtpAsync_WithCustomDigits_CreatesCorrectLength()
        {
            // Arrange
            var accountId = 1;
            var otpType = OTPType.PasswordReset;
            var digits = 8;

            _mockOtpHistoryRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<AccountOTPHistory, bool>>>(),
                null, null
            )).ReturnsAsync(new List<AccountOTPHistory>());

            // Act
            var result = await _otpService.CreateOtpAsync(accountId, otpType, digits: digits);

            // Assert
            Assert.Equal(digits, result.Length);
        }

        #endregion

        #region VerifyOtpAsync Tests

        [Fact]
        public async Task VerifyOtpAsync_WithValidOtp_ReturnsTrue()
        {
            // Arrange
            var accountId = 1;
            var otpCode = "123456";
            var otpType = OTPType.EmailVerification;

            // Hash the OTP code for comparison
            var hashedOtp = ComputeHash(otpCode);

            var otpHistory = new AccountOTPHistory
            {
                AccountId = accountId,
                OTPCode = hashedOtp,
                OTPType = otpType,
                Status = OTPStatus.Active,
                ExpiresAt = DateTime.Now.AddMinutes(5),
                CreatedAt = DateTime.Now
            };

            _mockOtpHistoryRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<AccountOTPHistory, bool>>>(),
                null, null
            )).ReturnsAsync(new List<AccountOTPHistory> { otpHistory });

            // Act
            var result = await _otpService.VerifyOtpAsync(accountId, otpCode, otpType);

            // Assert
            Assert.True(result);
            _mockOtpHistoryRepo.Verify(r => r.UpdateAsync(It.Is<AccountOTPHistory>(
                o => o.Status == OTPStatus.Verified && o.VerifiedAt != null
            )), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task VerifyOtpAsync_WithInvalidOtp_ReturnsFalse()
        {
            // Arrange
            var accountId = 1;
            var otpCode = "123456";
            var wrongOtp = "654321";
            var otpType = OTPType.EmailVerification;

            var hashedOtp = ComputeHash(wrongOtp);

            var otpHistory = new AccountOTPHistory
            {
                AccountId = accountId,
                OTPCode = hashedOtp,
                OTPType = otpType,
                Status = OTPStatus.Active,
                ExpiresAt = DateTime.Now.AddMinutes(5)
            };

            _mockOtpHistoryRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<AccountOTPHistory, bool>>>(),
                null, null
            )).ReturnsAsync(new List<AccountOTPHistory> { otpHistory });

            // Act
            var result = await _otpService.VerifyOtpAsync(accountId, otpCode, otpType);

            // Assert
            Assert.False(result);
            _mockOtpHistoryRepo.Verify(r => r.UpdateAsync(It.IsAny<AccountOTPHistory>()), Times.Never);
        }

        [Fact]
        public async Task VerifyOtpAsync_WithExpiredOtp_ReturnsFalse()
        {
            // Arrange
            var accountId = 1;
            var otpCode = "123456";
            var otpType = OTPType.EmailVerification;

            _mockOtpHistoryRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<AccountOTPHistory, bool>>>(),
                null, null
            )).ReturnsAsync(new List<AccountOTPHistory>());

            // Act
            var result = await _otpService.VerifyOtpAsync(accountId, otpCode, otpType);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task VerifyOtpAsync_WithNoActiveOtp_ReturnsFalse()
        {
            // Arrange
            var accountId = 1;
            var otpCode = "123456";
            var otpType = OTPType.EmailVerification;

            _mockOtpHistoryRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<AccountOTPHistory, bool>>>(),
                null, null
            )).ReturnsAsync(new List<AccountOTPHistory>());

            // Act
            var result = await _otpService.VerifyOtpAsync(accountId, otpCode, otpType);

            // Assert
            Assert.False(result);
            _mockOtpHistoryRepo.Verify(r => r.UpdateAsync(It.IsAny<AccountOTPHistory>()), Times.Never);
        }

        #endregion

        #region Helper Methods

        private static string ComputeHash(string input)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes);
        }

        #endregion
    }
}