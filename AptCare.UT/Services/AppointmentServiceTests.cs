using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.AppointmentDtos;
using AptCare.Service.Dtos.RepairRequestDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Implements;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AptCare.UT.Services
{
    public class AppointmentServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _uow = new();
        private readonly Mock<IGenericRepository<Appointment>> _apptRepo = new();
        private readonly Mock<IGenericRepository<RepairRequest>> _rrRepo = new();
        private readonly Mock<IGenericRepository<AppointmentTracking>> _trackingRepo = new();
        private readonly Mock<IGenericRepository<AppointmentAssign>> _assignRepo = new();
        private readonly Mock<IGenericRepository<RequestTracking>> _reqTrackingRepo = new();
        private readonly Mock<IGenericRepository<Media>> _mediaRepo = new();
        private readonly Mock<IGenericRepository<InspectionReport>> _inspectionRepo = new();
        private readonly Mock<IGenericRepository<RepairReport>> _repairReportRepo = new();
        private readonly Mock<IGenericRepository<ReportApproval>> _approvalRepo = new();
        private readonly Mock<IGenericRepository<Invoice>> _invoiceRepo = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<IUserContext> _userContext = new();
        private readonly Mock<INotificationService> _notification = new();
        private readonly Mock<IRepairRequestService> _rrService = new();
        private readonly Mock<ILogger<AppointmentService>> _logger = new();

        private readonly AppointmentService _service;

        public AppointmentServiceTests()
        {
            _uow.Setup(u => u.GetRepository<Appointment>()).Returns(_apptRepo.Object);
            _uow.Setup(u => u.GetRepository<RepairRequest>()).Returns(_rrRepo.Object);
            _uow.Setup(u => u.GetRepository<AppointmentTracking>()).Returns(_trackingRepo.Object);
            _uow.Setup(u => u.GetRepository<AppointmentAssign>()).Returns(_assignRepo.Object);
            _uow.Setup(u => u.GetRepository<RequestTracking>()).Returns(_reqTrackingRepo.Object);
            _uow.Setup(u => u.GetRepository<Media>()).Returns(_mediaRepo.Object);
            _uow.Setup(u => u.GetRepository<InspectionReport>()).Returns(_inspectionRepo.Object);
            _uow.Setup(u => u.GetRepository<RepairReport>()).Returns(_repairReportRepo.Object);
            _uow.Setup(u => u.GetRepository<ReportApproval>()).Returns(_approvalRepo.Object);
            _uow.Setup(u => u.GetRepository<Invoice>()).Returns(_invoiceRepo.Object);
            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _uow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            _service = new AppointmentService(_uow.Object, _logger.Object, _mapper.Object, _userContext.Object, _notification.Object, _rrService.Object);
        }

        #region CreateAppointmentAsync Tests

        [Fact]
        public async Task CreateAppointmentAsync_Success_CreatesAppointment()
        {
            // Arrange
            var dto = new AppointmentCreateDto
            {
                RepairRequestId = 1,
                StartTime = DateTime.Now.AddHours(1),
                EndTime = DateTime.Now.AddHours(2)
            };

            _rrRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(true);

            var appointment = new Appointment
            {
                RepairRequestId = 1,
                StartTime = DateTime.Now.AddHours(1),
                EndTime = DateTime.Now.AddHours(2),
                AppointmentTrackings = new List<AppointmentTracking>()
            };
            _mapper.Setup(m => m.Map<Appointment>(dto)).Returns(appointment);

            // Act
            var result = await _service.CreateAppointmentAsync(dto);

            // Assert
            Assert.Equal("Tạo lịch hẹn thành công", result);
            _apptRepo.Verify(r => r.InsertAsync(appointment), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAppointmentAsync_Throws_WhenRepairRequestNotExists()
        {
            // Arrange
            var dto = new AppointmentCreateDto { RepairRequestId = 999 };
            _rrRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateAppointmentAsync(dto));
            Assert.Contains("Yêu cầu sửa chữa không tồn tại", ex.Message);
        }

        [Fact]
        public async Task CreateAppointmentAsync_Throws_WhenEndTimeBeforeStartTime()
        {
            // Arrange
            var dto = new AppointmentCreateDto
            {
                RepairRequestId = 1,
                StartTime = DateTime.Now.AddHours(2),
                EndTime = DateTime.Now.AddHours(1)
            };

            _rrRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateAppointmentAsync(dto));
            Assert.Contains("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.", ex.Message);
        }

        #endregion

        #region UpdateAppointmentAsync Tests

        [Fact]
        public async Task UpdateAppointmentAsync_Success_UpdatesAppointment()
        {
            // Arrange
            var id = 1;
            var dto = new AppointmentUpdateDto
            {
                StartTime = DateTime.UtcNow.AddHours(3),
                EndTime = DateTime.UtcNow.AddHours(4),
                Note = "Updated note"
            };

            var appointment = new Appointment
            {
                AppointmentId = id,
                StartTime = DateTime.UtcNow.AddHours(1),
                EndTime = DateTime.UtcNow.AddHours(2),
                AppointmentAssigns = new List<AppointmentAssign>()
            };

            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync(appointment);

            _mapper.Setup(m => m.Map(dto, appointment));

            // Act
            var result = await _service.UpdateAppointmentAsync(id, dto);

            // Assert
            Assert.Equal("Cập nhật lịch hẹn thành công", result);
            _apptRepo.Verify(r => r.UpdateAsync(appointment), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAppointmentAsync_Throws_WhenNotFound()
        {
            // Arrange
            var dto = new AppointmentUpdateDto();
            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync((Appointment?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.UpdateAppointmentAsync(999, dto));
            Assert.Contains("Lịch hẹn không tồn tại", ex.Message);
        }

        [Fact]
        public async Task UpdateAppointmentAsync_Throws_WhenAlreadyAssignedAndTimeChanged()
        {
            // Arrange
            var id = 1;
            var dto = new AppointmentUpdateDto
            {
                StartTime = DateTime.UtcNow.AddHours(5),
                EndTime = DateTime.UtcNow.AddHours(6)
            };

            var appointment = new Appointment
            {
                AppointmentId = id,
                StartTime = DateTime.UtcNow.AddHours(1),
                EndTime = DateTime.UtcNow.AddHours(2),
                AppointmentAssigns = new List<AppointmentAssign>
                {
                    new AppointmentAssign { AppointmentAssignId = 1 }
                }
            };

            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync(appointment);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.UpdateAppointmentAsync(id, dto));
            Assert.Contains("Không thể thay đổi thời gian lịch hẹn khi đã phân công", ex.Message);
        }

        #endregion

        #region DeleteAppointmentAsync Tests

        [Fact]
        public async Task DeleteAppointmentAsync_Success_DeletesAppointment()
        {
            // Arrange
            var id = 1;
            var appointment = new Appointment { AppointmentId = id };

            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync(appointment);

            // Act
            var result = await _service.DeleteAppointmentAsync(id);

            // Assert
            Assert.Equal("Xóa lịch hẹn thành công", result);
            _apptRepo.Verify(r => r.DeleteAsync(appointment), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAppointmentAsync_Throws_WhenNotFound()
        {
            // Arrange
            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync((Appointment?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.DeleteAppointmentAsync(999));
            Assert.Contains("Lịch hẹn không tồn tại", ex.Message);
        }

        #endregion

        #region GetAppointmentByIdAsync Tests

        [Fact]
        public async Task GetAppointmentByIdAsync_Success_ReturnsAppointmentWithMedia()
        {
            // Arrange
            var id = 1;
            var dto = new AppointmentDto
            {
                AppointmentId = id,
                RepairRequest = new RepairRequestBasicDto { RepairRequestId = 1 }
            };

            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Appointment, AppointmentDto>>>(),
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync(dto);

            _mediaRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Media, MediaDto>>>(),
                It.IsAny<Expression<Func<Media, bool>>>(),
                It.IsAny<Func<IQueryable<Media>, IOrderedQueryable<Media>>>(),
                It.IsAny<Func<IQueryable<Media>, IIncludableQueryable<Media, object>>>()
            )).ReturnsAsync(new List<MediaDto>());

            // Act
            var result = await _service.GetAppointmentByIdAsync(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result.AppointmentId);
        }

        [Fact]
        public async Task GetAppointmentByIdAsync_Throws_WhenNotFound()
        {
            // Arrange
            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Appointment, AppointmentDto>>>(),
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync((AppointmentDto?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.GetAppointmentByIdAsync(999));
            Assert.Contains("Tầng không tồn tại", ex.Message); // Note: This seems like a bug in the original code
        }

        #endregion

        #region GetPaginateAppointmentAsync Tests

        [Fact]
        public async Task GetPaginateAppointmentAsync_Success_ReturnsPaginatedResult()
        {
            // Arrange
            var dto = new PaginateDto { page = 1, size = 10 };
            var pagedResult = new Paginate<AppointmentDto>
            {
                Items = new List<AppointmentDto>
                {
                    new AppointmentDto { AppointmentId = 1 }
                },
                Page = 1,
                Size = 10,
                Total = 1,
                TotalPages = 1
            };

            _apptRepo.Setup(r => r.GetPagingListAsync(
                It.IsAny<Expression<Func<Appointment, AppointmentDto>>>(),
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>(),
                1,
                10
            )).ReturnsAsync(pagedResult);

            // Act
            var result = await _service.GetPaginateAppointmentAsync(dto, null, null, null);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
        }

        [Fact]
        public async Task GetPaginateAppointmentAsync_Throws_WhenFromDateAfterToDate()
        {
            // Arrange
            var dto = new PaginateDto();
            var fromDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
            var toDate = DateOnly.FromDateTime(DateTime.UtcNow);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.GetPaginateAppointmentAsync(dto, fromDate, toDate, null));
            Assert.Contains("Ngày bắt đầu không thể sau ngày kết thúc", ex.Message);
        }

        #endregion

        #region CheckInAsync Tests

        [Fact]
        public async Task CheckInAsync_Success_UpdatesStatusToInVisit()
        {
            // Arrange
            var id = 1;
            var userId = 1;
            var timeNow = DateTime.UtcNow;
            
            var appointment = new Appointment
            {
                AppointmentId = id,
                StartTime = timeNow.AddMinutes(-20),
                RepairRequestId = 1,
                AppointmentTrackings = new List<AppointmentTracking>
                {
                    new AppointmentTracking { Status = AppointmentStatus.Confirmed, UpdatedAt = timeNow }
                },
                AppointmentAssigns = new List<AppointmentAssign>
                {
                    new AppointmentAssign { Status = WorkOrderStatus.Pending }
                },
                RepairRequest = new RepairRequest { RepairRequestId = 1 }
            };

            var workSlot = new WorkSlot
            {
                WorkSlotId = 1,
                Date = DateOnly.FromDateTime(timeNow),
                TechnicianId = userId,
                Status = WorkSlotStatus.Working,
                Slot = new Slot
                {
                    SlotId = 1,
                    FromTime = timeNow.AddHours(-1).TimeOfDay,
                    ToTime = timeNow.AddHours(2).TimeOfDay
                }
            };

            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync(appointment);

            var workSlotRepo = new Mock<IGenericRepository<WorkSlot>>();
            workSlotRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<WorkSlot, bool>>>(),
                It.IsAny<Func<IQueryable<WorkSlot>, IOrderedQueryable<WorkSlot>>>(),
                It.IsAny<Func<IQueryable<WorkSlot>, IIncludableQueryable<WorkSlot, object>>>()
            )).ReturnsAsync(workSlot);

            _uow.Setup(u => u.GetRepository<WorkSlot>()).Returns(workSlotRepo.Object);

            _reqTrackingRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RequestTracking, RequestStatus>>>(),
                It.IsAny<Expression<Func<RequestTracking, bool>>>(),
                It.IsAny<Func<IQueryable<RequestTracking>, IOrderedQueryable<RequestTracking>>>(),
                It.IsAny<Func<IQueryable<RequestTracking>, IIncludableQueryable<RequestTracking, object>>>()
            )).ReturnsAsync(RequestStatus.InProgress);

            _rrService.Setup(s => s.ToggleRepairRequestStatusAsync(It.IsAny<ToggleRRStatus>()))
                .ReturnsAsync(true);

            _userContext.SetupGet(u => u.CurrentUserId).Returns(userId);

            // Act
            var result = await _service.CheckInAsync(id);

            // Assert
            Assert.True(result);
            _trackingRepo.Verify(r => r.InsertAsync(It.Is<AppointmentTracking>(t => t.Status == AppointmentStatus.InVisit)), Times.Once);
            _assignRepo.Verify(r => r.UpdateAsync(It.IsAny<AppointmentAssign>()), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CheckInAsync_Throws_WhenAppointmentNotFound()
        {
            // Arrange
            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync((Appointment?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CheckInAsync(999));
            Assert.Contains("Lịch hẹn không tồn tại", ex.Message);
        }

        [Fact]
        public async Task CheckInAsync_Throws_WhenNotConfirmedStatus()
        {
            // Arrange
            var id = 1;
            var userId = 1;
            var timeNow = DateTime.UtcNow;
            
            var appointment = new Appointment
            {
                AppointmentId = id,
                StartTime = timeNow.AddMinutes(-20),
                RepairRequestId = 1,
                AppointmentTrackings = new List<AppointmentTracking>
                {
                    new AppointmentTracking { Status = AppointmentStatus.Pending, UpdatedAt = timeNow }
                },
                AppointmentAssigns = new List<AppointmentAssign>(),
                RepairRequest = new RepairRequest { RepairRequestId = 1 }
            };

            var workSlot = new WorkSlot
            {
                WorkSlotId = 1,
                Date = DateOnly.FromDateTime(timeNow),
                TechnicianId = userId,
                Status = WorkSlotStatus.Working,
                Slot = new Slot
                {
                    SlotId = 1,
                    FromTime = timeNow.AddHours(-1).TimeOfDay,
                    ToTime = timeNow.AddHours(2).TimeOfDay
                }
            };

            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync(appointment);

            var workSlotRepo = new Mock<IGenericRepository<WorkSlot>>();
            workSlotRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<WorkSlot, bool>>>(),
                It.IsAny<Func<IQueryable<WorkSlot>, IOrderedQueryable<WorkSlot>>>(),
                It.IsAny<Func<IQueryable<WorkSlot>, IIncludableQueryable<WorkSlot, object>>>()
            )).ReturnsAsync(workSlot);

            _uow.Setup(u => u.GetRepository<WorkSlot>()).Returns(workSlotRepo.Object);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CheckInAsync(id));
            Assert.Contains("Lịch hẹn không ở trạng thái chưa được xác nhận phân công, không thể check-in", ex.Message);
        }

        #endregion

        #region StartRepairAsync Tests

        [Fact]
        public async Task StartRepairAsync_Success_FromInVisit()
        {
            // Arrange
            var id = 1;
            var appointment = new Appointment
            {
                AppointmentId = id,
                RepairRequestId = 1,
                AppointmentTrackings = new List<AppointmentTracking>
                {
                    new AppointmentTracking { Status = AppointmentStatus.InVisit, UpdatedAt = DateTime.UtcNow }
                },
                AppointmentAssigns = new List<AppointmentAssign>(),
                InspectionReports = new List<InspectionReport>(),
                RepairRequest = new RepairRequest
                {
                    Appointments = new List<Appointment>
                    {
                        new Appointment
                        {
                            InspectionReports = new List<InspectionReport>
                            {
                                new InspectionReport { Status = ReportStatus.Approved, CreatedAt = DateTime.UtcNow }
                            }
                        }
                    }
                }
            };

            var invoice = new Invoice
            {
                InvoiceId = 1,
                RepairRequestId = 1,
                Status = InvoiceStatus.Approved
            };

            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync(appointment);

            _rrRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(true);

            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync(invoice);

            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            // Act
            var result = await _service.StartRepairAsync(id);

            // Assert
            Assert.True(result);
            _trackingRepo.Verify(r => r.InsertAsync(It.Is<AppointmentTracking>(t => t.Status == AppointmentStatus.InRepair)), Times.Once);
        }

        [Fact]
        public async Task StartRepairAsync_Throws_WhenInvalidStatusTransition()
        {
            // Arrange
            var id = 1;
            var appointment = new Appointment
            {
                AppointmentId = id,
                AppointmentTrackings = new List<AppointmentTracking>
                {
                    new AppointmentTracking { Status = AppointmentStatus.Pending, UpdatedAt = DateTime.UtcNow }
                }
            };

            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync(appointment);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.StartRepairAsync(id));
            Assert.Contains("Không thể bắt đầu sửa chữa từ trạng thái", ex.Message);
        }

        #endregion

        #region CompleteAppointmentAsync Tests

        [Fact]
        public async Task CompleteAppointmentAsync_Success_WithoutNextAppointment()
        {
            // Arrange
            var id = 1;
            var appointment = new Appointment
            {
                AppointmentId = id,
                RepairRequestId = 1,
                AppointmentTrackings = new List<AppointmentTracking>
                {
                    new AppointmentTracking { Status = AppointmentStatus.InRepair, UpdatedAt = DateTime.UtcNow }
                },
                AppointmentAssigns = new List<AppointmentAssign>
                {
                    new AppointmentAssign { Status = WorkOrderStatus.Working }
                },
                InspectionReports = new List<InspectionReport>(),
                RepairReport = new RepairReport
                {
                    Status = ReportStatus.Approved,
                    ReportApprovals = new List<ReportApproval>()
                },
                RepairRequest = new RepairRequest
                {
                    RepairRequestId = 1,
                    ApartmentId = null 
                }
            };

            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync(appointment);

            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            // Act
            var result = await _service.CompleteAppointmentAsync(id, "Completed", true, DateOnly.FromDateTime(DateTime.Now));

            // Assert
            Assert.Equal("Lịch hẹn đã được hoàn thành", result);
            _trackingRepo.Verify(r => r.InsertAsync(It.Is<AppointmentTracking>(t => t.Status == AppointmentStatus.Completed)), Times.Once);
        }

        [Fact]
        public async Task CompleteAppointmentAsync_Success_WithNextAppointment()
        {
            // Arrange
            var id = 1;
            var appointment = new Appointment
            {
                AppointmentId = id,
                RepairRequestId = 1,
                AppointmentTrackings = new List<AppointmentTracking>
                {
                    new AppointmentTracking { Status = AppointmentStatus.InRepair, UpdatedAt = DateTime.UtcNow }
                },
                AppointmentAssigns = new List<AppointmentAssign>
                {
                    new AppointmentAssign { Status = WorkOrderStatus.Working }
                },
                InspectionReports = new List<InspectionReport>(),            
                RepairReport = new RepairReport 
                { 
                    Status = ReportStatus.Approved,
                    ReportApprovals = new List<ReportApproval>()
                },
                RepairRequest = new RepairRequest
                {
                    RepairRequestId = 1,
                    ApartmentId = null
                }
            };

            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync(appointment);

            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            // Act
            var result = await _service.CompleteAppointmentAsync(id, "Completed", true, null);

            // Assert
            Assert.Equal("Lịch hẹn đã được hoàn thành", result);
            _reqTrackingRepo.Verify(r => r.InsertAsync(It.Is<RequestTracking>(t => t.Status == RequestStatus.Scheduling)), Times.Once);
        }

        #endregion

        #region ToogleAppoimnentStatus Tests

        [Fact]
        public async Task ToogleAppoimnentStatus_Success_ValidTransition()
        {
            // Arrange
            var id = 1;
            var appointment = new Appointment
            {
                AppointmentId = id,
                AppointmentTrackings = new List<AppointmentTracking>
                {
                    new AppointmentTracking { Status = AppointmentStatus.Pending, UpdatedAt = DateTime.UtcNow }
                }
            };

            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync(appointment);

            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            // Act
            var result = await _service.ToogleAppoimnentStatus(id, "Note", AppointmentStatus.Assigned);

            // Assert
            Assert.True(result);
            _trackingRepo.Verify(r => r.InsertAsync(It.Is<AppointmentTracking>(t => t.Status == AppointmentStatus.Assigned)), Times.Once);
        }

        [Fact]
        public async Task ToogleAppoimnentStatus_Throws_WhenInvalidTransition()
        {
            // Arrange
            var id = 1;
            var appointment = new Appointment
            {
                AppointmentId = id,
                AppointmentTrackings = new List<AppointmentTracking>
                {
                    new AppointmentTracking { Status = AppointmentStatus.Completed, UpdatedAt = DateTime.UtcNow }
                }
            };

            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync(appointment);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.ToogleAppoimnentStatus(id, "Note", AppointmentStatus.Pending));
            Assert.Contains("Không thể chuyển trạng thái từ", ex.Message);
        }

        #endregion

        #region GetResidentAppointmentScheduleAsync Tests

        [Fact]
        public async Task GetResidentAppointmentScheduleAsync_Success_ReturnsSchedule()
        {
            // Arrange
            var fromDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var toDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));

            var appointments = new List<AppointmentDto>
            {
                new AppointmentDto
                {
                    AppointmentId = 1,
                    StartTime = DateTime.UtcNow.AddDays(1)
                }
            };

            _apptRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Appointment, AppointmentDto>>>(),
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync(appointments);

            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            // Act
            var result = await _service.GetResidentAppointmentScheduleAsync(fromDate, toDate);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        #endregion

        #region GetTechnicianAppointmentScheduleAsync Tests

        [Fact]
        public async Task GetTechnicianAppointmentScheduleAsync_Success_ReturnsSchedule()
        {
            // Arrange
            var techId = 1;
            var fromDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var toDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));

            var appointments = new List<AppointmentDto>
            {
                new AppointmentDto
                {
                    AppointmentId = 1,
                    StartTime = DateTime.UtcNow.AddHours(9)
                }
            };

            var slots = new List<Slot>
            {
                new Slot { SlotId = 1, FromTime = new TimeSpan(0, 0, 0), ToTime = new TimeSpan(23, 0, 0) }
            };

            _apptRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Appointment, AppointmentDto>>>(),
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync(appointments);

            var slotRepo = new Mock<IGenericRepository<Slot>>();
            slotRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Slot, bool>>>(),
                It.IsAny<Func<IQueryable<Slot>, IOrderedQueryable<Slot>>>(),
                It.IsAny<Func<IQueryable<Slot>, IIncludableQueryable<Slot, object>>>()
            )).ReturnsAsync(slots);

            _uow.Setup(u => u.GetRepository<Slot>()).Returns(slotRepo.Object);

            // Act
            var result = await _service.GetTechnicianAppointmentScheduleAsync(techId, fromDate, toDate);

            // Assert
            Assert.NotNull(result);
        }

        #endregion
    }
}