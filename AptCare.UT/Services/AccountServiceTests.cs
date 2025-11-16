using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Enum.Apartment;
using AptCare.Repository.Paginate;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.Account;
using AptCare.Service.Dtos.UserDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Implements;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AptCare.UT.Services
{
    public class AccountServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _uow = new();
        private readonly Mock<IGenericRepository<Account>> _accountRepo = new();
        private readonly Mock<IGenericRepository<User>> _userRepo = new();
        private readonly Mock<IGenericRepository<Media>> _mediaRepo = new();
        private readonly Mock<IPasswordHasher<Account>> _passwordHasher = new();
        private readonly Mock<IMailSenderService> _mailSender = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<AccountService>> _logger = new();

        private readonly AccountService _service;

        public AccountServiceTests()
        {
            _uow.Setup(u => u.GetRepository<Account>()).Returns(_accountRepo.Object);
            _uow.Setup(u => u.GetRepository<User>()).Returns(_userRepo.Object);
            _uow.Setup(u => u.GetRepository<Media>()).Returns(_mediaRepo.Object);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            _service = new AccountService(
                _uow.Object,
                _mailSender.Object,
                _passwordHasher.Object,
                _logger.Object,
                _mapper.Object
            );
        }

        #region CreateAccountForUserAsync Tests

        [Fact]
        public async Task CreateAccountForUserAsync_Success_CreatesAccountForResident()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                UserId = userId,
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                Status = ActiveStatus.Active,
                Account = null,
                UserApartments = new List<UserApartment>
                {
                    new UserApartment { Status = ActiveStatus.Active }
                },
                TechnicianTechniques = new List<TechnicianTechnique>()
            };

            _userRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync(user);

            _passwordHasher.Setup(p => p.HashPassword(It.IsAny<Account>(), It.IsAny<string>()))
                .Returns("hashed_password");

            _mailSender.Setup(m => m.SendEmailWithTemplateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>()
            )).Returns(Task.CompletedTask);

            Account insertedAccount = null;
            _accountRepo.Setup(r => r.InsertAsync(It.IsAny<Account>()))
                .Callback<Account>(a => insertedAccount = a)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAccountForUserAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Resident", result);
            Assert.NotNull(insertedAccount);
            Assert.Equal(AccountRole.Resident, insertedAccount.Role);
            Assert.Equal(user.Email, insertedAccount.Username);
            Assert.True(insertedAccount.MustChangePassword);
            _accountRepo.Verify(r => r.InsertAsync(It.IsAny<Account>()), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
            _mailSender.Verify(m => m.SendEmailWithTemplateAsync(
                user.Email,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>()
            ), Times.Once);
        }

        [Fact]
        public async Task CreateAccountForUserAsync_Throws_WhenUserNotExists()
        {
            // Arrange
            _userRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync((User)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateAccountForUserAsync(999));
            Assert.Contains("không tồn tại hoặc đã bị vô hiệu hóa", ex.Message);
        }

        [Fact]
        public async Task CreateAccountForUserAsync_Throws_WhenUserAlreadyHasAccount()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                Status = ActiveStatus.Active,
                Account = new Account
                {
                    AccountId = 1,
                    Username = "test@example.com",
                    Role = AccountRole.Resident
                }
            };

            _userRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync(user);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateAccountForUserAsync(1));
            Assert.Contains("đã có tài khoản", ex.Message);
        }

        [Fact]
        public async Task CreateAccountForUserAsync_Throws_WhenUserInactive()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                Status = ActiveStatus.Inactive,
                Account = null
            };

            _userRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync(user);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateAccountForUserAsync(1));
            Assert.Contains("Không thể tạo tài khoản cho user ID", ex.Message);
        }

        [Fact]
        public async Task CreateAccountForUserAsync_Throws_WhenUserHasNoApartments()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Email = "test@example.com",
                Status = ActiveStatus.Active,
                Account = null,
                UserApartments = new List<UserApartment>(),
                TechnicianTechniques = new List<TechnicianTechnique>()
            };

            _userRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync(user);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateAccountForUserAsync(1));
            Assert.Contains("Không thể tạo tài khoản", ex.Message);
        }

        #endregion

        #region GetSystemUserPageAsync Tests

        [Fact]
        public async Task GetSystemUserPageAsync_Success_ReturnsFilteredUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User
                {
                    UserId = 1,
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john@example.com",
                    Status = ActiveStatus.Active,
                    Account = new Account { Role = AccountRole.Resident }
                }
            };

            var userDtos = new List<UserDto>
            {
                new UserDto { UserId = 1, Email = "john@example.com" }
            };

            var paginate = new Paginate<UserDto>
            {
                Items = userDtos,
                Page = 1,
                Size = 10,
                Total = 1
            };

            _userRepo.Setup(r => r.GetPagingListAsync(
                It.IsAny<Expression<Func<User, UserDto>>>(),
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>(),
                It.IsAny<int>(),
                It.IsAny<int>()
            )).ReturnsAsync(paginate);

            _mediaRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Media, bool>>>(),
                It.IsAny<Func<IQueryable<Media>, IOrderedQueryable<Media>>>(),
                It.IsAny<Func<IQueryable<Media>, IIncludableQueryable<Media, object>>>()
            )).ReturnsAsync(new List<Media>());

            // Act
            var result = await _service.GetSystemUserPageAsync("john", "Resident", "Active", 1, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(1, result.Total);
        }

        [Fact]
        public async Task GetSystemUserPageAsync_ReturnsEmpty_WhenInvalidRole()
        {
            // Arrange & Act
            var result = await _service.GetSystemUserPageAsync("", "InvalidRole", "", 1, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.Total);
        }

        [Fact]
        public async Task GetSystemUserPageAsync_ReturnsEmpty_WhenInvalidStatus()
        {
            // Arrange & Act
            var result = await _service.GetSystemUserPageAsync("", "", "InvalidStatus", 1, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.Total);
        }

        #endregion

        #region TogleAccontStatus Tests

        [Fact]
        public async Task TogleAccontStatus_Success_LocksAccount()
        {
            // Arrange
            var account = new Account
            {
                AccountId = 1,
                LockoutEnabled = false
            };

            _accountRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Account, bool>>>(),
                It.IsAny<Func<IQueryable<Account>, IOrderedQueryable<Account>>>(),
                It.IsAny<Func<IQueryable<Account>, IIncludableQueryable<Account, object>>>()
            )).ReturnsAsync(account);

            // Act
            var result = await _service.TogleAccontStatus(1);

            // Assert
            Assert.Contains("Đã khóa", result);
            Assert.True(account.LockoutEnabled);
            _accountRepo.Verify(r => r.UpdateAsync(account), Times.Once);
        }

        [Fact]
        public async Task TogleAccontStatus_Success_UnlocksAccount()
        {
            // Arrange
            var account = new Account
            {
                AccountId = 1,
                LockoutEnabled = true
            };

            _accountRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Account, bool>>>(),
                It.IsAny<Func<IQueryable<Account>, IOrderedQueryable<Account>>>(),
                It.IsAny<Func<IQueryable<Account>, IIncludableQueryable<Account, object>>>()
            )).ReturnsAsync(account);

            // Act
            var result = await _service.TogleAccontStatus(1);

            // Assert
            Assert.Contains("Đã mở khóa", result);
            Assert.False(account.LockoutEnabled);
            _accountRepo.Verify(r => r.UpdateAsync(account), Times.Once);
        }

        [Fact]
        public async Task TogleAccontStatus_Throws_WhenAccountNotExists()
        {
            // Arrange
            _accountRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Account, bool>>>(),
                It.IsAny<Func<IQueryable<Account>, IOrderedQueryable<Account>>>(),
                It.IsAny<Func<IQueryable<Account>, IIncludableQueryable<Account, object>>>()
            )).ReturnsAsync((Account)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.TogleAccontStatus(999));
            Assert.Contains("không tồn tại", ex.Message);
        }

        #endregion
    }
}