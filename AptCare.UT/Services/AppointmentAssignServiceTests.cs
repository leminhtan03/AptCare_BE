using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.UnitOfWork;
using AptCare.Repository.Repositories;
using AptCare.Service.Dtos.AppointmentAssignDtos;
using AptCare.Service.Dtos.AppointmentDtos;
using AptCare.Service.Dtos.NotificationDtos;
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
    public class AppointmentAssignServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _uow = new();
        private readonly Mock<IGenericRepository<Appointment>> _apptRepo = new();
        private readonly Mock<IGenericRepository<User>> _userRepo = new();
        private readonly Mock<IGenericRepository<AppointmentAssign>> _assignRepo = new();
        private readonly Mock<IGenericRepository<AppointmentTracking>> _trackingRepo = new();
        private readonly Mock<IGenericRepository<Technique>> _techRepo = new();
        private readonly Mock<IGenericRepository<RepairRequest>> _rrRepo = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<INotificationService> _notification = new();
        private readonly Mock<IUserContext> _userContext = new();
        private readonly Mock<ILogger<AppointmentAssignService>> _logger = new();

        private readonly AppointmentAssignService _service;

        public AppointmentAssignServiceTests()
        {
            // Wire repository resolvers
            _uow.Setup(u => u.GetRepository<Appointment>()).Returns(_apptRepo.Object);
            _uow.Setup(u => u.GetRepository<User>()).Returns(_userRepo.Object);
            _uow.Setup(u => u.GetRepository<AppointmentAssign>()).Returns(_assignRepo.Object);
            _uow.Setup(u => u.GetRepository<AppointmentTracking>()).Returns(_trackingRepo.Object);
            _uow.Setup(u => u.GetRepository<Technique>()).Returns(_techRepo.Object);
            _uow.Setup(u => u.GetRepository<RepairRequest>()).Returns(_rrRepo.Object);

            // Default transaction/commit behavior
            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _uow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            _service = new AppointmentAssignService(_uow.Object, _logger.Object, _mapper.Object, _notification.Object, _userContext.Object);
        }

        [Fact]
        public async Task AssignAppointmentAsync_Success_InsertsTrackingAndAssign()
        {
            // Arrange
            var appointmentId = 1;
            var start = DateTime.UtcNow.Date.AddHours(9);
            var end = start.AddHours(1);

            var appointment = new Appointment
            {
                AppointmentId = appointmentId,
                StartTime = start,
                EndTime = end,
                AppointmentTrackings = new List<AppointmentTracking>
                {
                    new AppointmentTracking { AppointmentId = appointmentId, Status = AppointmentStatus.Pending, UpdatedAt = DateTime.UtcNow.AddMinutes(-5) }
                }
            };

            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                    It.IsAny<Expression<Func<Appointment, bool>>>(),
                    It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                    It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
                )).ReturnsAsync(appointment);

            // Existing user is technician
            _userRepo.Setup(r => r.AnyAsync(
                    It.IsAny<Expression<Func<User, bool>>>(),
                    It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
                )).ReturnsAsync(true);

            // isExistingAppoAssign = false, isConflictAppoAssign = false
            _assignRepo.SetupSequence(r => r.AnyAsync(
                    It.IsAny<Expression<Func<AppointmentAssign, bool>>>(),
                    It.IsAny<Func<IQueryable<AppointmentAssign>, IIncludableQueryable<AppointmentAssign, object>>>()
                ))
                .ReturnsAsync(false)
                .ReturnsAsync(false);

            AppointmentAssign? insertedAssign = null;
            _assignRepo.Setup(r => r.InsertAsync(It.IsAny<AppointmentAssign>()))
                       .Callback<AppointmentAssign>(a => insertedAssign = a)
                       .Returns(Task.CompletedTask);

            AppointmentTracking? insertedTracking = null;
            _trackingRepo.Setup(r => r.InsertAsync(It.IsAny<AppointmentTracking>()))
                         .Callback<AppointmentTracking>(t => insertedTracking = t)
                         .Returns(Task.CompletedTask);

            _userContext.SetupGet(u => u.CurrentUserId).Returns(999);

            // Act
            var result = await _service.AssignAppointmentAsync(appointmentId, new[] { 123 });

            // Assert
            Assert.Equal("Phân công thành công.", result);
            Assert.NotNull(insertedAssign);
            Assert.Equal(123, insertedAssign!.TechnicianId);
            Assert.NotNull(insertedTracking);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
            _assignRepo.Verify(r => r.InsertAsync(It.IsAny<AppointmentAssign>()), Times.Once);
        }

        [Fact]
        public async Task AssignAppointmentAsync_Throws_WhenAppointmentNotFound()
        {
            // Arrange
            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                    It.IsAny<Expression<Func<Appointment, bool>>>(),
                    It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                    It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
                )).ReturnsAsync((Appointment?)null);

            // Act / Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.AssignAppointmentAsync(999, new[] { 1 }));
            Assert.Contains("Lịch hẹn không tồn tại", ex.Message);
        }

        [Fact]
        public async Task AssignAppointmentAsync_Throws_WhenTechnicianNotExists()
        {
            // Arrange
            var appointment = new Appointment
            {
                AppointmentId = 2,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddHours(1),
                AppointmentTrackings = new List<AppointmentTracking> { new AppointmentTracking { Status = AppointmentStatus.Pending, UpdatedAt = DateTime.UtcNow } }
            };

            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                    It.IsAny<Expression<Func<Appointment, bool>>>(),
                    It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                    It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
                )).ReturnsAsync(appointment);

            _userRepo.Setup(r => r.AnyAsync(
                    It.IsAny<Expression<Func<User, bool>>>(),
                    It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
                )).ReturnsAsync(false);

            // Act / Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.AssignAppointmentAsync(2, new[] { 777 }));
            Assert.Contains("Kĩ thuật viên có ID 777 không tồn tại", ex.Message);
        }

        [Fact]
        public async Task UpdateAppointmentAssignAsync_Success_WhenOwner()
        {
            // Arrange
            var assignId = 10;
            var currentUser = 55;
            _userContext.SetupGet(u => u.CurrentUserId).Returns(currentUser);

            var existing = new AppointmentAssign
            {
                AppointmentAssignId = assignId,
                TechnicianId = currentUser,
                Status = WorkOrderStatus.Pending
            };

            _assignRepo.Setup(r => r.SingleOrDefaultAsync(
                    It.IsAny<Expression<Func<AppointmentAssign, bool>>>(),
                    It.IsAny<Func<IQueryable<AppointmentAssign>, IOrderedQueryable<AppointmentAssign>>>(),
                    It.IsAny<Func<IQueryable<AppointmentAssign>, IIncludableQueryable<AppointmentAssign, object>>>()
                )).ReturnsAsync(existing);

            var dto = new AppointmentAssignUpdateDto
            {
                ActualStartTime = DateTime.UtcNow.AddHours(1),
                ActualEndTime = DateTime.UtcNow.AddHours(2),
                Status = WorkOrderStatus.Working
            };

            _mapper.Setup(m => m.Map(dto, existing)).Callback(() =>
            {
                existing.EstimatedStartTime = dto.ActualStartTime!.Value;
                existing.EstimatedEndTime = dto.ActualEndTime!.Value;
                existing.Status = dto.Status;
            });

            // Act
            var result = await _service.UpdateAppointmentAssignAsync(assignId, dto);

            // Assert
            Assert.Equal("Cập nhật lịch phân công thành công.", result);
            _assignRepo.Verify(r => r.UpdateAsync(existing), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
            Assert.Equal(WorkOrderStatus.Working, existing.Status);
        }

        [Fact]
        public async Task UpdateAppointmentAssignAsync_Throws_WhenNotFound()
        {
            // Arrange
            _assignRepo.Setup(r => r.SingleOrDefaultAsync(
                    It.IsAny<Expression<Func<AppointmentAssign, bool>>>(),
                    It.IsAny<Func<IQueryable<AppointmentAssign>, IOrderedQueryable<AppointmentAssign>>>(),
                    It.IsAny<Func<IQueryable<AppointmentAssign>, IIncludableQueryable<AppointmentAssign, object>>>()
                )).ReturnsAsync((AppointmentAssign?)null);

            // Act / Assert
            await Assert.ThrowsAsync<AppValidationException>(() => _service.UpdateAppointmentAssignAsync(999, new AppointmentAssignUpdateDto()));
        }

        [Fact]
        public async Task UpdateAppointmentAssignAsync_Throws_WhenNotOwner()
        {
            // Arrange
            var existing = new AppointmentAssign { AppointmentAssignId = 11, TechnicianId = 5 };          
            _assignRepo.Setup(r => r.SingleOrDefaultAsync(
                    It.IsAny<Expression<Func<AppointmentAssign, bool>>>(),
                    It.IsAny<Func<IQueryable<AppointmentAssign>, IOrderedQueryable<AppointmentAssign>>>(),
                    It.IsAny<Func<IQueryable<AppointmentAssign>, IIncludableQueryable<AppointmentAssign, object>>>()
                )).ReturnsAsync(existing);

            _userContext.SetupGet(u => u.CurrentUserId).Returns(99);

            // Act / Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.UpdateAppointmentAssignAsync(11, new AppointmentAssignUpdateDto()));
            Assert.Contains("Không thể cập nhật lịch phân công không phải của mình", ex.Message);
        }

        [Fact]
        public async Task SuggestTechniciansForAppointment_Throws_WhenAppointmentNotFound()
        {
            // Arrange
            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                    It.IsAny<Expression<Func<Appointment, bool>>>(),
                    It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                    It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
                )).ReturnsAsync((Appointment?)null);

            // Act / Assert
            await Assert.ThrowsAsync<AppValidationException>(() => _service.SuggestTechniciansForAppointment(123, null));
        }

        [Fact]
        public async Task SuggestTechniciansForAppointment_Returns_List_WhenSuccess()
        {
            // Arrange
            var appt = new Appointment
            {
                AppointmentId = 20,
                StartTime = DateTime.UtcNow.Date.AddHours(9),
                EndTime = DateTime.UtcNow.Date.AddHours(10),
                RepairRequest = new RepairRequest { IsEmergency = false, Issue = null }
            };

            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                    It.IsAny<Expression<Func<Appointment, bool>>>(),
                    It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                    It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
                )).ReturnsAsync(appt);

            // technique existence check (no techniqueId provided) - not required, but safe to return true if called
            _techRepo.Setup(r => r.AnyAsync(
                    It.IsAny<Expression<Func<Technique, bool>>>(),
                    It.IsAny<Func<IQueryable<Technique>, IIncludableQueryable<Technique, object>>>()
                )).ReturnsAsync(true);

            var suggested = new List<SuggestedTechnicianDto>
            {
                new SuggestedTechnicianDto
                {
                    UserId = 1,
                    FirstName = "T",
                    LastName = "L",
                    AssignCountThatDay = 0,
                    AssignCountThatMonth = 0,
                    AppointmentsThatDay = new List<AppointmentDto>()
                }
            };

            // Mock GetListAsync<TResult> on User repository
            _userRepo.Setup(r => r.GetListAsync(
                    It.IsAny<Expression<Func<User, SuggestedTechnicianDto>>>(),
                    It.IsAny<Expression<Func<User, bool>>>(),
                    It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
                    It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
                )).ReturnsAsync(suggested);

            // Act
            var result = await _service.SuggestTechniciansForAppointment(appt.AppointmentId, null);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result.First().UserId);
        }

        [Fact]
        public async Task ConfirmAssignmentAsync_ConfirmTrue_SendsNotifications()
        {
            // Arrange
            var apptId = 30;
            var appointment = new Appointment
            {
                AppointmentId = apptId,
                StartTime = DateTime.UtcNow.Date.AddHours(8),
                AppointmentTrackings = new List<AppointmentTracking>
                {
                    new AppointmentTracking { AppointmentId = apptId, Status = AppointmentStatus.Assigned, UpdatedAt = DateTime.UtcNow.AddMinutes(-5) }
                },
                AppointmentAssigns = new List<AppointmentAssign>
                {
                    new AppointmentAssign { AppointmentAssignId = 1, TechnicianId = 7 }
                }
            };

            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                    It.IsAny<Expression<Func<Appointment, bool>>>(),
                    It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                    It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
                )).ReturnsAsync(appointment);

            _trackingRepo.Setup(r => r.InsertAsync(It.IsAny<AppointmentTracking>())).Returns(Task.CompletedTask);
            _notification.Setup(n => n.SendNotificationForTechnicianInAppointment(apptId, It.IsAny<NotificationPushRequestDto>())).Returns(Task.CompletedTask);
            _notification.Setup(n => n.SendNotificationForResidentInRequest(apptId, It.IsAny<NotificationPushRequestDto>())).Returns(Task.CompletedTask);

            _userContext.SetupGet(u => u.CurrentUserId).Returns(777);

            // Act
            var result = await _service.ConfirmAssignmentAsync(apptId, true);

            // Assert
            Assert.Equal("Xác nhận phân công thành công.", result);
            _notification.Verify(n => n.SendNotificationForTechnicianInAppointment(apptId, It.IsAny<NotificationPushRequestDto>()), Times.Once);
            _notification.Verify(n => n.SendNotificationForResidentInRequest(apptId, It.IsAny<NotificationPushRequestDto>()), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task ConfirmAssignmentAsync_ConfirmFalse_DeletesAssigns()
        {
            // Arrange
            var apptId = 31;
            var assign = new AppointmentAssign { AppointmentAssignId = 2, TechnicianId = 101 };
            var appointment = new Appointment
            {
                AppointmentId = apptId,
                AppointmentTrackings = new List<AppointmentTracking>(),
                AppointmentAssigns = new List<AppointmentAssign> { assign }
            };

            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                    It.IsAny<Expression<Func<Appointment, bool>>>(),
                    It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                    It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
                )).ReturnsAsync(appointment);

            _trackingRepo.Setup(r => r.InsertAsync(It.IsAny<AppointmentTracking>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.ConfirmAssignmentAsync(apptId, false);

            // Assert
            Assert.Equal("Hủy phân công thành công.", result);
            _assignRepo.Verify(r => r.DeleteAsync(It.IsAny<AppointmentAssign>()), Times.Exactly(appointment.AppointmentAssigns.Count));
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CancleAssignmentAsync_Success_DeletesAssign()
        {
            // Arrange
            var dto = new CancleAssignDto { technicanId = 50, appointmentId = 60 };
            var assign = new AppointmentAssign { AppointmentAssignId = 5, TechnicianId = 50, AppointmentId = 60, Status = WorkOrderStatus.Pending };

            _assignRepo.Setup(r => r.SingleOrDefaultAsync(
                    It.IsAny<Expression<Func<AppointmentAssign, bool>>>(),
                    It.IsAny<Func<IQueryable<AppointmentAssign>, IOrderedQueryable<AppointmentAssign>>>(),
                    It.IsAny<Func<IQueryable<AppointmentAssign>, IIncludableQueryable<AppointmentAssign, object>>>()
                )).ReturnsAsync(assign);

            // Act
            var result = await _service.CancleAssignmentAsync(dto);

            // Assert
            Assert.Equal("Hủy phân công thành công.", result);
            _assignRepo.Verify(r => r.DeleteAsync(assign), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task CancleAssignmentAsync_Throws_WhenAssignInWorking()
        {
            // Arrange
            var dto = new CancleAssignDto { technicanId = 1, appointmentId = 2 };
            var assign = new AppointmentAssign { TechnicianId = 1, AppointmentId = 2, Status = WorkOrderStatus.Working };

            _assignRepo.Setup(r => r.SingleOrDefaultAsync(
                    It.IsAny<Expression<Func<AppointmentAssign, bool>>>(),
                    It.IsAny<Func<IQueryable<AppointmentAssign>, IOrderedQueryable<AppointmentAssign>>>(),
                    It.IsAny<Func<IQueryable<AppointmentAssign>, IIncludableQueryable<AppointmentAssign, object>>>()
                )).ReturnsAsync(assign);

            // Act / Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CancleAssignmentAsync(dto));
            Assert.Contains("Không thể hủy lịch phân công đang trong trạng thái Đang thực hiện hoặc Hoàn thành", ex.Message);
        }
    }
}