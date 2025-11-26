using System.Linq.Expressions;
using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.RepairRequestDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Implements;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using AptCare.Service.Dtos.AppointmentAssignDtos;
using AptCare.Service.Services.Interfaces.RabbitMQ;

namespace AptCare.UT.Services
{
    public class RepairRequestServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _uow = new();
        private readonly Mock<IGenericRepository<RepairRequest>> _rrRepo = new();
        private readonly Mock<IGenericRepository<Apartment>> _aptRepo = new();
        private readonly Mock<IGenericRepository<UserApartment>> _userAptRepo = new();
        private readonly Mock<IGenericRepository<Issue>> _issueRepo = new();
        private readonly Mock<IGenericRepository<Slot>> _slotRepo = new();
        private readonly Mock<IGenericRepository<RequestTracking>> _trackingRepo = new();
        private readonly Mock<IGenericRepository<Media>> _mediaRepo = new();
        private readonly Mock<IGenericRepository<Appointment>> _apptRepo = new();
        private readonly Mock<IGenericRepository<AppointmentTracking>> _apptTrackingRepo = new();
        private readonly Mock<IGenericRepository<AppointmentAssign>> _assignRepo = new();
        private readonly Mock<IGenericRepository<User>> _userRepo = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<IUserContext> _userContext = new();
        private readonly Mock<ICloudinaryService> _cloudinary = new();
        private readonly Mock<IAppointmentAssignService> _appointmentAssignService = new();
        private readonly Mock<INotificationService> _notification = new();
        private readonly Mock<ILogger<RepairRequestService>> _logger = new();
        private readonly Mock<IRabbitMQService> _rabbitMQService = new();

        private readonly RepairRequestService _service;

        public RepairRequestServiceTests()
        {
            _uow.Setup(u => u.GetRepository<RepairRequest>()).Returns(_rrRepo.Object);
            _uow.Setup(u => u.GetRepository<Apartment>()).Returns(_aptRepo.Object);
            _uow.Setup(u => u.GetRepository<UserApartment>()).Returns(_userAptRepo.Object);
            _uow.Setup(u => u.GetRepository<Issue>()).Returns(_issueRepo.Object);
            _uow.Setup(u => u.GetRepository<Slot>()).Returns(_slotRepo.Object);
            _uow.Setup(u => u.GetRepository<RequestTracking>()).Returns(_trackingRepo.Object);
            _uow.Setup(u => u.GetRepository<Media>()).Returns(_mediaRepo.Object);
            _uow.Setup(u => u.GetRepository<Appointment>()).Returns(_apptRepo.Object);
            _uow.Setup(u => u.GetRepository<AppointmentTracking>()).Returns(_apptTrackingRepo.Object);
            _uow.Setup(u => u.GetRepository<AppointmentAssign>()).Returns(_assignRepo.Object);
            _uow.Setup(u => u.GetRepository<User>()).Returns(_userRepo.Object);
            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _uow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            _service = new RepairRequestService(
                _uow.Object,
                _logger.Object,
                _mapper.Object,
                _userContext.Object,
                _cloudinary.Object,
                _appointmentAssignService.Object,
                _notification.Object,
                _rabbitMQService.Object);
        }

        #region CreateNormalRepairRequestAsync Tests

        [Fact]
        public async Task CreateNormalRepairRequestAsync_Success_CreatesRequestWithAppointment()
        {
            // Arrange
            var currentUserId = 1;
            var dto = new RepairRequestNormalCreateDto
            {
                ApartmentId = 1,
                IssueId = 1,
                PreferredAppointment = DateTime.UtcNow.AddDays(1).Date.AddHours(9),
                Description = "Test repair",
                Note = "Test note"
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(currentUserId);
            _userContext.SetupGet(u => u.IsResident).Returns(true);

            _aptRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Apartment, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Apartment>, IIncludableQueryable<Apartment, object>>>()
            )).ReturnsAsync(true);

            _userAptRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<UserApartment, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<UserApartment>, IIncludableQueryable<UserApartment, object>>>()
            )).ReturnsAsync(true);

            var issue = new Issue { IssueId = 1, IsEmergency = false, EstimatedDuration = 2, RequiredTechnician = 1 };
            _issueRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Issue, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Issue>, System.Linq.IOrderedQueryable<Issue>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Issue>, IIncludableQueryable<Issue, object>>>()
            )).ReturnsAsync(issue);

            _slotRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Slot, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Slot>, IIncludableQueryable<Slot, object>>>()
            )).ReturnsAsync(true);

            var request = new RepairRequest { RepairRequestId = 1 };
            _mapper.Setup(m => m.Map<RepairRequest>(dto)).Returns(request);

            _userRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, int>>>(),
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<User>, System.Linq.IOrderedQueryable<User>>>(),
                It.IsAny<Func<System.Linq.IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync(100);

            _appointmentAssignService.Setup(s => s.SuggestTechniciansForAppointment(It.IsAny<int>(), It.IsAny<int?>()))
                .ReturnsAsync(new List<SuggestedTechnicianDto>());

            _notification.Setup(n => n.SendAndPushNotificationAsync(It.IsAny<Service.Dtos.NotificationDtos.NotificationPushRequestDto>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateNormalRepairRequestAsync(dto);

            // Assert
            Assert.Equal("Tạo yêu cầu sửa chữa thành công", result);
            _rrRepo.Verify(r => r.InsertAsync(It.IsAny<RepairRequest>()), Times.Once);
            _apptRepo.Verify(r => r.InsertAsync(It.IsAny<Appointment>()), Times.Once);
            _trackingRepo.Verify(r => r.InsertAsync(It.IsAny<RequestTracking>()), Times.AtLeastOnce);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateNormalRepairRequestAsync_Throws_WhenApartmentNotExists()
        {
            // Arrange
            var dto = new RepairRequestNormalCreateDto { ApartmentId = 999 };
            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            _aptRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Apartment, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Apartment>, IIncludableQueryable<Apartment, object>>>()
            )).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateNormalRepairRequestAsync(dto));
            Assert.Equal("Lỗi hệ thống: Căn hộ không tồn tại.", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateNormalRepairRequestAsync_Throws_WhenResidentNotInApartment()
        {
            // Arrange
            var dto = new RepairRequestNormalCreateDto { ApartmentId = 1 };
            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);
            _userContext.SetupGet(u => u.IsResident).Returns(true);

            _aptRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Apartment, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Apartment>, IIncludableQueryable<Apartment, object>>>()
            )).ReturnsAsync(true);

            _userAptRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<UserApartment, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<UserApartment>, IIncludableQueryable<UserApartment, object>>>()
            )).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateNormalRepairRequestAsync(dto));
            Assert.Equal("Lỗi hệ thống: Người dùng không thuộc căn hộ này.", ex.Message);
        }

        [Fact]
        public async Task CreateNormalRepairRequestAsync_Throws_WhenIssueIsEmergency()
        {
            // Arrange
            var dto = new RepairRequestNormalCreateDto { ApartmentId = 1, IssueId = 1, PreferredAppointment = DateTime.UtcNow.AddDays(1) };
            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);
            _userContext.SetupGet(u => u.IsResident).Returns(false);

            _aptRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Apartment, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Apartment>, IIncludableQueryable<Apartment, object>>>()
            )).ReturnsAsync(true);

            var emergencyIssue = new Issue { IssueId = 1, IsEmergency = true };
            _issueRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Issue, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Issue>, System.Linq.IOrderedQueryable<Issue>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Issue>, IIncludableQueryable<Issue, object>>>()
            )).ReturnsAsync(emergencyIssue);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateNormalRepairRequestAsync(dto));
            Assert.Equal("Lỗi hệ thống: Đây là Vấn đề khẩn cấp.", ex.Message);
        }

        [Fact]
        public async Task CreateNormalRepairRequestAsync_Throws_WhenTimeSlotNotSuitable()
        {
            // Arrange
            var dto = new RepairRequestNormalCreateDto
            {
                ApartmentId = 1,
                IssueId = 1,
                PreferredAppointment = DateTime.UtcNow.AddDays(1).Date.AddHours(9)
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);
            _userContext.SetupGet(u => u.IsResident).Returns(false);

            _aptRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Apartment, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Apartment>, IIncludableQueryable<Apartment, object>>>()
            )).ReturnsAsync(true);

            var issue = new Issue { IssueId = 1, IsEmergency = false, EstimatedDuration = 2 };
            _issueRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Issue, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Issue>, System.Linq.IOrderedQueryable<Issue>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Issue>, IIncludableQueryable<Issue, object>>>()
            )).ReturnsAsync(issue);

            _slotRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Slot, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Slot>, IIncludableQueryable<Slot, object>>>()
            )).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateNormalRepairRequestAsync(dto));
            Assert.Equal("Lỗi hệ thống: Thời gian sửa chữa không nằm trong ca làm việc của kỹ thuật viên, vui lòng chọn thời gian khác.", ex.Message);
        }

        #endregion

        #region CreateEmergencyRepairRequestAsync Tests

        [Fact]
        public async Task CreateEmergencyRepairRequestAsync_Success_CreatesEmergencyRequest()
        {
            // Arrange
            var dto = new RepairRequestEmergencyCreateDto
            {
                ApartmentId = 1,
                IssueId = 1,
                Description = "Emergency issue"
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);
            _userContext.SetupGet(u => u.IsResident).Returns(false);

            _aptRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Apartment, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Apartment>, IIncludableQueryable<Apartment, object>>>()
            )).ReturnsAsync(true);

            var emergencyIssue = new Issue { IssueId = 1, IsEmergency = true, EstimatedDuration = 1, RequiredTechnician = 2 };
            _issueRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Issue, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Issue>, System.Linq.IOrderedQueryable<Issue>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Issue>, IIncludableQueryable<Issue, object>>>()
            )).ReturnsAsync(emergencyIssue);

            var request = new RepairRequest { RepairRequestId = 1 };
            _mapper.Setup(m => m.Map<RepairRequest>(dto)).Returns(request);

            _appointmentAssignService.Setup(s => s.SuggestTechniciansForAppointment(It.IsAny<int>(), It.IsAny<int?>()))
                .ReturnsAsync(new List<SuggestedTechnicianDto>
                {
                    new SuggestedTechnicianDto { UserId = 1 },
                    new SuggestedTechnicianDto { UserId = 2 }
                });

            _userRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<User, int>>>(),
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<User>, System.Linq.IOrderedQueryable<User>>>(),
                It.IsAny<Func<System.Linq.IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync(new List<int> { 100, 101 });

            _notification.Setup(n => n.SendAndPushNotificationAsync(It.IsAny<Service.Dtos.NotificationDtos.NotificationPushRequestDto>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateEmergencyRepairRequestAsync(dto);

            // Assert
            Assert.Equal("Tạo yêu cầu sửa chữa khẩn cấp thành công", result);
            _rrRepo.Verify(r => r.InsertAsync(It.IsAny<RepairRequest>()), Times.Once);
            _apptRepo.Verify(r => r.InsertAsync(It.IsAny<Appointment>()), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateEmergencyRepairRequestAsync_Throws_WhenIssueNotEmergency()
        {
            // Arrange
            var dto = new RepairRequestEmergencyCreateDto { ApartmentId = 1, IssueId = 1 };
            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);
            _userContext.SetupGet(u => u.IsResident).Returns(false);

            _aptRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Apartment, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Apartment>, IIncludableQueryable<Apartment, object>>>()
            )).ReturnsAsync(true);

            var normalIssue = new Issue { IssueId = 1, IsEmergency = false };
            _issueRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Issue, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Issue>, System.Linq.IOrderedQueryable<Issue>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Issue>, IIncludableQueryable<Issue, object>>>()
            )).ReturnsAsync(normalIssue);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateEmergencyRepairRequestAsync(dto));
            Assert.Equal("Lỗi hệ thống: Đây không phải là Vấn đề khẩn cấp.", ex.Message);
        }

        #endregion

        #region ToggleRepairRequestStatusAsync Tests

        [Fact]
        public async Task ToggleRepairRequestStatusAsync_Success_UpdatesStatus()
        {
            // Arrange
            var dto = new ToggleRRStatus
            {
                RepairRequestId = 1,
                NewStatus = RequestStatus.Approved,
                Note = "Approved"
            };

            var request = new RepairRequest
            {
                RepairRequestId = 1,
                RequestTrackings = new List<RequestTracking>
                {
                    new RequestTracking { Status = RequestStatus.Pending, UpdatedAt = DateTime.UtcNow }
                },
                Appointments = new List<Appointment>(),
                Apartment = new Apartment
                {
                    UserApartments = new List<UserApartment>
                    {
                        new UserApartment { UserId = 1, Status = ActiveStatus.Active }
                    }
                }
            };

            _rrRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, System.Linq.IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(request);

            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            _notification.Setup(n => n.SendAndPushNotificationAsync(It.IsAny<Service.Dtos.NotificationDtos.NotificationPushRequestDto>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.ToggleRepairRequestStatusAsync(dto);

            // Assert
            Assert.True(result);
            _trackingRepo.Verify(r => r.InsertAsync(It.Is<RequestTracking>(t => t.Status == RequestStatus.Approved)), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ToggleRepairRequestStatusAsync_Throws_WhenRequestNotFound()
        {
            // Arrange
            var dto = new ToggleRRStatus
            {
                RepairRequestId = 999,
                NewStatus = RequestStatus.Approved
            };

            _rrRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, System.Linq.IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync((RepairRequest)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.ToggleRepairRequestStatusAsync(dto));
            Assert.Equal("Lỗi hệ thống: Yêu cầu sửa chữa không tồn tại.", ex.Message);
        }

        [Fact]
        public async Task ToggleRepairRequestStatusAsync_Throws_WhenInvalidTransition()
        {
            // Arrange
            var dto = new ToggleRRStatus
            {
                RepairRequestId = 1,
                NewStatus = RequestStatus.Completed
            };

            var request = new RepairRequest
            {
                RepairRequestId = 1,
                RequestTrackings = new List<RequestTracking>
                {
                    new RequestTracking { Status = RequestStatus.Pending, UpdatedAt = DateTime.UtcNow }
                },
                Appointments = new List<Appointment>()
            };

            _rrRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, System.Linq.IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(request);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.ToggleRepairRequestStatusAsync(dto));
            Assert.Contains("Chuyển trạng thái không hợp lệ", ex.Message);
        }

        #endregion

        #region ApprovalRepairRequestAsync Tests

        [Fact]
        public async Task ApprovalRepairRequestAsync_Success_ApprovesRequest()
        {
            // Arrange
            var dto = new ToggleRRStatus
            {
                RepairRequestId = 1,
                NewStatus = RequestStatus.Approved,
                Note = "Approved by manager"
            };

            var request = new RepairRequest
            {
                RepairRequestId = 1,
                RequestTrackings = new List<RequestTracking>
                {
                    new RequestTracking { Status = RequestStatus.Pending, UpdatedAt = DateTime.UtcNow }
                },
                Appointments = new List<Appointment>(),
                Apartment = new Apartment
                {
                    UserApartments = new List<UserApartment>
                    {
                        new UserApartment { UserId = 1, Status = ActiveStatus.Active }
                    }
                }
            };

            _rrRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, System.Linq.IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(request);

            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            _notification.Setup(n => n.SendAndPushNotificationAsync(It.IsAny<Service.Dtos.NotificationDtos.NotificationPushRequestDto>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.ApprovalRepairRequestAsync(dto);

            // Assert
            Assert.Equal("Cập nhật thành công", result);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task ApprovalRepairRequestAsync_Throws_WhenInvalidStatus()
        {
            // Arrange
            var dto = new ToggleRRStatus
            {
                RepairRequestId = 1,
                NewStatus = RequestStatus.InProgress // Invalid status for approval
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.ApprovalRepairRequestAsync(dto));
            Assert.Contains("Trạng thái mới phải là Approved hoặc Cancelled", ex.Message);
        }

        #endregion
    }
}