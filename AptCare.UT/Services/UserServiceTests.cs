using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Enum.Apartment;
using AptCare.Repository.Paginate;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.Account;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Dtos.UserDtos;
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
    public class UserServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _mockUnitOfWork;
        private readonly Mock<IPasswordHasher<Account>> _mockPasswordHasher;
        private readonly Mock<IMailSenderService> _mockMailSenderService;
        private readonly Mock<ILogger<UserService>> _mockLogger;
        private readonly Mock<ICloudinaryService> _mockCloudinaryService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly UserService _userService;
        private readonly Mock<IGenericRepository<User>> _mockUserRepo;
        private readonly Mock<IGenericRepository<Account>> _mockAccountRepo;
        private readonly Mock<IGenericRepository<UserApartment>> _mockUserApartmentRepo;
        private readonly Mock<IGenericRepository<Apartment>> _mockApartmentRepo;
        private readonly Mock<IGenericRepository<Technique>> _mockTechniqueRepo;
        private readonly Mock<IGenericRepository<TechnicianTechnique>> _mockTechnicianTechniqueRepo;
        private readonly Mock<IGenericRepository<Media>> _mockMediaRepo;
        private readonly Mock<IRedisCacheService> _cacheService = new();

        public UserServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork<AptCareSystemDBContext>>();
            _mockPasswordHasher = new Mock<IPasswordHasher<Account>>();
            _mockMailSenderService = new Mock<IMailSenderService>();
            _mockLogger = new Mock<ILogger<UserService>>();
            _mockCloudinaryService = new Mock<ICloudinaryService>();
            _mockMapper = new Mock<IMapper>();

            _mockUserRepo = new Mock<IGenericRepository<User>>();
            _mockAccountRepo = new Mock<IGenericRepository<Account>>();
            _mockUserApartmentRepo = new Mock<IGenericRepository<UserApartment>>();
            _mockApartmentRepo = new Mock<IGenericRepository<Apartment>>();
            _mockTechniqueRepo = new Mock<IGenericRepository<Technique>>();
            _mockTechnicianTechniqueRepo = new Mock<IGenericRepository<TechnicianTechnique>>();
            _mockMediaRepo = new Mock<IGenericRepository<Media>>();

            _mockUnitOfWork.Setup(u => u.GetRepository<User>()).Returns(_mockUserRepo.Object);
            _mockUnitOfWork.Setup(u => u.GetRepository<Account>()).Returns(_mockAccountRepo.Object);
            _mockUnitOfWork.Setup(u => u.GetRepository<UserApartment>()).Returns(_mockUserApartmentRepo.Object);
            _mockUnitOfWork.Setup(u => u.GetRepository<Apartment>()).Returns(_mockApartmentRepo.Object);
            _mockUnitOfWork.Setup(u => u.GetRepository<Technique>()).Returns(_mockTechniqueRepo.Object);
            _mockUnitOfWork.Setup(u => u.GetRepository<TechnicianTechnique>()).Returns(_mockTechnicianTechniqueRepo.Object);
            _mockUnitOfWork.Setup(u => u.GetRepository<Media>()).Returns(_mockMediaRepo.Object);

            _cacheService.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.GetAsync<UserDto>(It.IsAny<string>()))
                .ReturnsAsync((UserDto)null);

            _cacheService.Setup(c => c.GetAsync<IPaginate<UserGetAllDto>>(It.IsAny<string>()))
                .ReturnsAsync((IPaginate<UserGetAllDto>)null);

            _userService = new UserService(
                _mockUnitOfWork.Object,
                _mockPasswordHasher.Object,
                _mockMailSenderService.Object,
                _mockLogger.Object,
                _mockCloudinaryService.Object,
                _cacheService.Object,
                _mockMapper.Object
            );
        }

        #region CreateUserAsync Tests

        [Fact]
        public async Task CreateUserAsync_WithValidResidentData_ReturnsSuccess()
        {
            // Arrange
            var createUserDto = new CreateUserDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@test.com",
                PhoneNumber = "0123456789",
                Role = AccountRole.Resident,
                CreateAccount = true,
                Apartments = new List<ApartmentForUserCreateDto>
                {
                    new ApartmentForUserCreateDto
                    {
                        ApartmentId = 1,
                        RoleInApartment = RoleInApartmentType.Owner
                    }
                }
            };

            var user = new User { UserId = 1, Email = createUserDto.Email };
            var apartment = new Apartment
            {
                ApartmentId = 1,
                Room = "101",
                Limit = 5,
                Status = ApartmentStatus.Active,
                UserApartments = new List<UserApartment>()
            };

            var apartments = new List<Apartment>();
            apartments.Add(apartment);

            _mockUserRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<User, bool>>>(), null))
                .ReturnsAsync(false);
            _mockMapper.Setup(m => m.Map<User>(createUserDto)).Returns(user);
            _mockApartmentRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Apartment, bool>>>(),
                null, 
                It.IsAny<Func<IQueryable<Apartment>, IIncludableQueryable<Apartment, object>>>()
            )).ReturnsAsync(apartments);

            _mockPasswordHasher.Setup(p => p.HashPassword(It.IsAny<Account>(), It.IsAny<string>()))
                .Returns("hashed_password");

            // Act
            var result = await _userService.CreateUserAsync(createUserDto);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.AccountCreated);
            _mockUserRepo.Verify(r => r.InsertAsync(It.IsAny<User>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task CreateUserAsync_WithDuplicateEmail_ThrowsException()
        {
            // Arrange
            var createUserDto = new CreateUserDto
            {
                Email = "duplicate@test.com",
                Role = AccountRole.Resident
            };

            _mockUserRepo.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                _userService.CreateUserAsync(createUserDto)
            );
        }

        [Fact]
        public async Task CreateUserAsync_ResidentWithoutApartments_ThrowsException()
        {
            // Arrange
            var createUserDto = new CreateUserDto
            {
                Email = "resident@test.com",
                Role = AccountRole.Resident,
                Apartments = null
            };

            _mockUserRepo.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                _userService.CreateUserAsync(createUserDto)
            );
        }

        [Fact]
        public async Task CreateUserAsync_TechnicianWithoutTechniques_ThrowsException()
        {
            // Arrange
            var createUserDto = new CreateUserDto
            {
                Email = "tech@test.com",
                Role = AccountRole.Technician,
                TechniqueIds = null
            };

            _mockUserRepo.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>(), null))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                _userService.CreateUserAsync(createUserDto)
            );
        }

        #endregion

        #region GetUserByIdAsync Tests

        [Fact]
        public async Task GetUserByIdAsync_WithValidId_ReturnsUser()
        {
            // Arrange
            var userId = 1;
            var userDto = new UserDto
            {
                UserId = userId,
                Email = "test@test.com",
                FirstName = "Test",
                LastName = "User"
            };

            _mockUserRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, UserDto>>>(),
                It.IsAny<Expression<Func<User, bool>>>(),
                null,
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync(userDto);

            _mockMediaRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Media, bool>>>(),
                null, null
            )).ReturnsAsync((Media)null);

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.UserId);
        }

        [Fact]
        public async Task GetUserByIdAsync_WithInvalidId_ThrowsException()
        {
            // Arrange
            var userId = 999;

            _mockUserRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, UserDto>>>(),
                It.IsAny<Expression<Func<User, bool>>>(),
                null,
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync((UserDto)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _userService.GetUserByIdAsync(userId)
            );
        }

        #endregion

        #region UpdateUserAsync Tests

        [Fact]
        public async Task UpdateUserAsync_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var userId = 1;
            var updateDto = new UpdateUserDto
            {
                FirstName = "Updated",
                LastName = "Name"
            };

            var existingUser = new User
            {
                UserId = userId,
                Email = "test@test.com",
                FirstName = "Old",
                LastName = "Name",
                Account = new Account { AccountId = userId }
            };

            _mockUserRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                null,
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync(existingUser);

            // Act
            var result = await _userService.UpdateUserAsync(userId, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Cập nhật người dùng thành công", result);
            _mockUserRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task UpdateUserAsync_WithInvalidId_ThrowsException()
        {
            // Arrange
            var userId = 999;
            var updateDto = new UpdateUserDto();

            _mockUserRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                null,
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync((User)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _userService.UpdateUserAsync(userId, updateDto)
            );
        }

        #endregion

        #region InactivateUserAsync Tests

        [Fact]
        public async Task InactivateUserAsync_WithValidUser_ReturnsSuccess()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                UserId = userId,
                FirstName = "Test",
                LastName = "User",
                Status = ActiveStatus.Active,
                Account = new Account { AccountId = userId, Role = AccountRole.Resident },
                UserApartments = new List<UserApartment>
                {
                    new UserApartment
                    {
                        RoleInApartment = RoleInApartmentType.Member,
                        Status = ActiveStatus.Active
                    }
                }
            };

            _mockUserRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                null,
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync(user);

            // Act
            var result = await _userService.InactivateUserAsync(userId, new InactivateUserDto());

            // Assert
            Assert.NotNull(result);
            Assert.Contains("vô hiệu hóa", result);
            _mockUserRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task InactivateUserAsync_WithOwnerRole_ThrowsException()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                UserId = userId,
                FirstName = "Test",
                LastName = "Owner",
                Status = ActiveStatus.Active,
                Account = new Account { AccountId = userId, Role = AccountRole.Resident },
                UserApartments = new List<UserApartment>
                {
                    new UserApartment
                    {
                        RoleInApartment = RoleInApartmentType.Owner,
                        Status = ActiveStatus.Active,
                        Apartment = new Apartment { Room = "101", Floor = new Floor { FloorNumber = 1 } }
                    }
                }
            };

            _mockUserRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                null,
                It.IsAny<Func<IQueryable<User>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<User, object>>>()
            )).ReturnsAsync(user);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _userService.InactivateUserAsync(userId, new InactivateUserDto())
            );
            Assert.Contains("Owner", exception.Message);
        }

        [Fact]
        public async Task InactivateUserAsync_WithAdminRole_ThrowsException()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                UserId = userId,
                Status = ActiveStatus.Active,
                Account = new Account { AccountId = userId, Role = AccountRole.Admin }
            };

            _mockUserRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                null,
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync(user);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                _userService.InactivateUserAsync(userId, new InactivateUserDto())
            );
        }

        #endregion
    }
}