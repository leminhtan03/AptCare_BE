using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.TokenEnum;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Dtos.NotificationDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Implements;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

namespace AptCare.UT.Services
{
    public class NotificationServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _mockUnitOfWork;
        private readonly Mock<ILogger<NotificationService>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IFCMService> _mockFCMService;
        private readonly Mock<IUserContext> _mockUserContext;
        private readonly NotificationService _notificationService;
        private readonly Mock<IGenericRepository<Notification>> _mockNotificationRepo;
        private readonly Mock<IGenericRepository<AccountToken>> _mockAccountTokenRepo;
        private readonly Mock<IGenericRepository<User>> _mockUserRepo;
        private readonly Mock<IGenericRepository<Appointment>> _mockAppointmentRepo;
        private readonly Mock<IGenericRepository<RepairRequest>> _mockRepairRequestRepo;
        private readonly Mock<IGenericRepository<AppointmentAssign>> _mockAppointmentAssignRepo;
        private readonly Mock<IGenericRepository<Media>> _mockMediaRepo;

        public NotificationServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork<AptCareSystemDBContext>>();
            _mockLogger = new Mock<ILogger<NotificationService>>();
            _mockMapper = new Mock<IMapper>();
            _mockFCMService = new Mock<IFCMService>();
            _mockUserContext = new Mock<IUserContext>();

            _mockNotificationRepo = new Mock<IGenericRepository<Notification>>();
            _mockAccountTokenRepo = new Mock<IGenericRepository<AccountToken>>();
            _mockUserRepo = new Mock<IGenericRepository<User>>();
            _mockAppointmentRepo = new Mock<IGenericRepository<Appointment>>();
            _mockRepairRequestRepo = new Mock<IGenericRepository<RepairRequest>>();
            _mockAppointmentAssignRepo = new Mock<IGenericRepository<AppointmentAssign>>();
            _mockMediaRepo = new Mock<IGenericRepository<Media>>();

            _mockUnitOfWork.Setup(u => u.GetRepository<Notification>()).Returns(_mockNotificationRepo.Object);
            _mockUnitOfWork.Setup(u => u.GetRepository<AccountToken>()).Returns(_mockAccountTokenRepo.Object);
            _mockUnitOfWork.Setup(u => u.GetRepository<User>()).Returns(_mockUserRepo.Object);
            _mockUnitOfWork.Setup(u => u.GetRepository<Appointment>()).Returns(_mockAppointmentRepo.Object);
            _mockUnitOfWork.Setup(u => u.GetRepository<RepairRequest>()).Returns(_mockRepairRequestRepo.Object);
            _mockUnitOfWork.Setup(u => u.GetRepository<AppointmentAssign>()).Returns(_mockAppointmentAssignRepo.Object);
            _mockUnitOfWork.Setup(u => u.GetRepository<Media>()).Returns(_mockMediaRepo.Object);

            _notificationService = new NotificationService(
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mockMapper.Object,
                _mockFCMService.Object,
                _mockUserContext.Object
            );
        }

        #region BroadcastNotificationAsync Tests

        [Fact]
        public async Task BroadcastNotificationAsync_WithGeneralType_SendsToAllUsers()
        {
            // Arrange
            var dto = new NotificationCreateDto
            {
                Title = "Test Notification",
                Description = "Test Description",
                Type = NotificationType.General
            };

            var userIds = new List<int> { 1, 2, 3 };
            _mockUserRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<User, int>>>(),
                It.IsAny<Expression<Func<User, bool>>>(),
                null, null
            )).ReturnsAsync(userIds);

            var pushDto = new NotificationPushRequestDto
            {
                Title = dto.Title,
                Description = dto.Description,
                Type = dto.Type,
                UserIds = userIds // ✅ Phải có UserIds
            };

            // ✅ Mock mapping từ NotificationCreateDto -> NotificationPushRequestDto
            _mockMapper.Setup(m => m.Map<NotificationPushRequestDto>(dto))
                .Returns(pushDto);

            _mockMapper.Setup(m => m.Map<Notification>(It.IsAny<NotificationPushRequestDto>()))
                .Returns((NotificationPushRequestDto source) => new Notification
                {
                    Title = source.Title,
                    Description = source.Description,
                    Type = source.Type,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                    // ReceiverId sẽ được set trong service loop
                });

            _mockAccountTokenRepo.Setup(r => r.GetListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<AccountToken, string>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<AccountToken, bool>>>(),
                null, null
            )).ReturnsAsync(new List<string> { "token1", "token2" });

            _mockFCMService.Setup(f => f.PushMulticastAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).ReturnsAsync(true);

            // Act
            var result = await _notificationService.BroadcastNotificationAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Gửi thông báo thành công", result);
            _mockNotificationRepo.Verify(r => r.InsertRangeAsync(It.IsAny<IEnumerable<Notification>>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task BroadcastNotificationAsync_WithInternalType_SendsToInternalUsers()
        {
            // Arrange
            var dto = new NotificationCreateDto
            {
                Title = "Internal Notification",
                Description = "Internal Description",
                Type = NotificationType.Internal
            };

            var userIds = new List<int> { 1, 2 };
            _mockUserRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<User, int>>>(),
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync(userIds);

            var pushDto = new NotificationPushRequestDto
            {
                Title = dto.Title,
                Description = dto.Description,
                Type = dto.Type,
                UserIds = userIds // ✅ Phải có UserIds
            };

            _mockMapper.Setup(m => m.Map<NotificationPushRequestDto>(dto))
                .Returns(pushDto);

            _mockMapper.Setup(m => m.Map<Notification>(It.IsAny<NotificationPushRequestDto>()))
               .Returns((NotificationPushRequestDto source) => new Notification
               {
                   Title = source.Title,
                   Description = source.Description,
                   Type = source.Type,
                   IsRead = false,
                   CreatedAt = DateTime.Now
                   // ReceiverId sẽ được set trong service loop
               });

            _mockAccountTokenRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<AccountToken, string>>>(),
                It.IsAny<Expression<Func<AccountToken, bool>>>(),
                null, null
            )).ReturnsAsync(new List<string>());

            _mockFCMService.Setup(f => f.PushMulticastAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).ReturnsAsync(true);

            // Act
            var result = await _notificationService.BroadcastNotificationAsync(dto);

            // Assert
            Assert.NotNull(result);
            _mockNotificationRepo.Verify(r => r.InsertRangeAsync(It.IsAny<IEnumerable<Notification>>()), Times.Once);
        }

        #endregion

        #region MarkAsReadAsync Tests

        [Fact]
        public async Task MarkAsReadAsync_WithValidIds_MarksNotificationsAsRead()
        {
            // Arrange
            var userId = 1;
            var notificationIds = new List<int> { 1, 2, 3 };
            var notifications = notificationIds.Select(id => new Notification
            {
                NotificationId = id,
                ReceiverId = userId,
                IsRead = false
            }).ToList();

            _mockUserContext.Setup(u => u.CurrentUserId).Returns(userId);
            _mockNotificationRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Notification, bool>>>(),
                It.IsAny<Func<IQueryable<Notification>, IOrderedQueryable<Notification>>>(),
                It.IsAny<Func<IQueryable<Notification>, IIncludableQueryable<Notification, object>>>()
            )).ReturnsAsync(notifications);

            // Act
            var result = await _notificationService.MarkAsReadAsync(notificationIds);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Thành công", result);
            _mockNotificationRepo.Verify(r => r.UpdateRange(It.IsAny<IEnumerable<Notification>>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task MarkAsReadAsync_WithOtherUserNotification_ThrowsException()
        {
            // Arrange
            var currentUserId = 1;
            var otherUserId = 2;
            var notificationIds = new List<int> { 1 };
            var notifications = new List<Notification>
            {
                new Notification
                {
                    NotificationId = 1,
                    ReceiverId = otherUserId,
                    IsRead = false
                }
            };

            _mockUserContext.Setup(u => u.CurrentUserId).Returns(currentUserId);
            _mockNotificationRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Notification, bool>>>(),
                It.IsAny<Func<IQueryable<Notification>, IOrderedQueryable<Notification>>>(),
                It.IsAny<Func<IQueryable<Notification>, IIncludableQueryable<Notification, object>>>()
            )).ReturnsAsync(notifications);

            // Act & Assert
            await Assert.ThrowsAsync<AppValidationException>(() =>
                _notificationService.MarkAsReadAsync(notificationIds)
            );
        }

        #endregion

        #region GetMyUnreadCountAsync Tests

        [Fact]
        public async Task GetMyUnreadCountAsync_ReturnsCorrectCount()
        {
            // Arrange
            var userId = 1;
            var unreadIds = new List<int> { 1, 2, 3, 4, 5 };

            _mockUserContext.Setup(u => u.CurrentUserId).Returns(userId);
            _mockNotificationRepo.Setup(r => r.GetListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Notification, int>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Notification, bool>>>(),
                null, null
            )).ReturnsAsync(unreadIds);

            // Act
            var result = await _notificationService.GetMyUnreadCountAsync();

            // Assert
            Assert.Equal(5, result);
        }

        [Fact]
        public async Task GetMyUnreadCountAsync_WithNoUnread_ReturnsZero()
        {
            // Arrange
            var userId = 1;

            _mockUserContext.Setup(u => u.CurrentUserId).Returns(userId);
            _mockNotificationRepo.Setup(r => r.GetListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Notification, int>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Notification, bool>>>(),
                null, null
            )).ReturnsAsync(new List<int>());

            // Act
            var result = await _notificationService.GetMyUnreadCountAsync();

            // Assert
            Assert.Equal(0, result);
        }

        #endregion

        #region SendAndPushNotificationAsync Tests

        [Fact]
        public async Task SendAndPushNotificationAsync_WithValidData_SendsNotification()
        {
            // Arrange
            var dto = new NotificationPushRequestDto
            {
                Title = "Test",
                Description = "Test Description",
                Type = NotificationType.Individual,
                UserIds = new List<int> { 1, 2 }
            };

            _mockMapper.Setup(m => m.Map<Notification>(dto))
                .Returns(new Notification());

            _mockAccountTokenRepo.Setup(r => r.GetListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<AccountToken, string>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<AccountToken, bool>>>(),
                null, null
            )).ReturnsAsync(new List<string> { "token1" });

            _mockFCMService.Setup(f => f.PushMulticastAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).ReturnsAsync(true);

            // Act
            await _notificationService.SendAndPushNotificationAsync(dto);

            // Assert
            _mockNotificationRepo.Verify(r => r.InsertRangeAsync(It.IsAny<IEnumerable<Notification>>()), Times.Once);
            _mockFCMService.Verify(f => f.PushMulticastAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ), Times.Once);
        }

        #endregion
    }
}