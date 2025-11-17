using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Enum.Apartment;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.ApproveReportDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Implements;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace AptCare.UT.Services
{
    public class ReportApprovalServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _uow = new();
        private readonly Mock<IGenericRepository<ReportApproval>> _approvalRepo = new();
        private readonly Mock<IGenericRepository<RepairReport>> _repairReportRepo = new();
        private readonly Mock<IGenericRepository<InspectionReport>> _inspectionReportRepo = new();
        private readonly Mock<IGenericRepository<User>> _userRepo = new();
        private readonly Mock<IUserContext> _userContext = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<ReportApprovalService>> _logger = new();

        private readonly ReportApprovalService _service;

        public ReportApprovalServiceTests()
        {
            _uow.Setup(u => u.GetRepository<ReportApproval>()).Returns(_approvalRepo.Object);
            _uow.Setup(u => u.GetRepository<RepairReport>()).Returns(_repairReportRepo.Object);
            _uow.Setup(u => u.GetRepository<InspectionReport>()).Returns(_inspectionReportRepo.Object);
            _uow.Setup(u => u.GetRepository<User>()).Returns(_userRepo.Object);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            _service = new ReportApprovalService(
                _uow.Object,
                _userContext.Object,
                _logger.Object,
                _mapper.Object
            );
        }

        #region CreateApproveReportAsync Tests

        [Fact]
        public async Task CreateApproveReportAsync_Success_CreatesRepairReportApproval()
        {
            // Arrange
            var currentUserId = 1;
            var techLeadId = 2;
            var dto = new ApproveReportCreateDto
            {
                ReportId = 1,
                ReportType = "RepairReport",
                Status = ReportStatus.Pending,
                Comment = "Waiting for approval"
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(currentUserId);
            _userContext.SetupGet(u => u.Role).Returns(AccountRole.Technician.ToString());

            // Mock finding TechnicianLead
            _userRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, int>>>(),
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync(techLeadId);

            // Check repair report exists
            _repairReportRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<RepairReport, bool>>>(),
                It.IsAny<Func<IQueryable<RepairReport>, IIncludableQueryable<RepairReport, object>>>()
            )).ReturnsAsync(true);

            ReportApproval insertedApproval = null;
            _approvalRepo.Setup(r => r.InsertAsync(It.IsAny<ReportApproval>()))
                .Callback<ReportApproval>(a => insertedApproval = a)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateApproveReportAsync(dto);

            // Assert
            Assert.True(result);
            Assert.NotNull(insertedApproval);
            Assert.Equal(dto.ReportId, insertedApproval.RepairReportId);
            Assert.Equal(techLeadId, insertedApproval.UserId);
            Assert.Equal(AccountRole.TechnicianLead, insertedApproval.Role);
            Assert.Equal(ReportStatus.Pending, insertedApproval.Status);
            Assert.Equal(dto.Comment ?? "Chờ phê duyệt", insertedApproval.Comment);
            _approvalRepo.Verify(r => r.InsertAsync(It.IsAny<ReportApproval>()), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateApproveReportAsync_Success_CreatesInspectionReportApproval()
        {
            // Arrange
            var currentUserId = 1;
            var techLeadId = 2;
            var dto = new ApproveReportCreateDto
            {
                ReportId = 1,
                ReportType = "InspectionReport",
                Status = ReportStatus.Pending,
                Comment = "Waiting for inspection approval"
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(currentUserId);
            _userContext.SetupGet(u => u.Role).Returns(AccountRole.Technician.ToString());

            _userRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, int>>>(),
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync(techLeadId);

            ReportApproval insertedApproval = null;
            _approvalRepo.Setup(r => r.InsertAsync(It.IsAny<ReportApproval>()))
                .Callback<ReportApproval>(a => insertedApproval = a)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateApproveReportAsync(dto);

            // Assert
            Assert.True(result);
            Assert.NotNull(insertedApproval);
            Assert.Equal(dto.ReportId, insertedApproval.InspectionReportId);
            Assert.Equal(techLeadId, insertedApproval.UserId);
            Assert.Equal(AccountRole.TechnicianLead, insertedApproval.Role);
        }

        [Fact]
        public async Task CreateApproveReportAsync_Throws_WhenStatusNotPending()
        {
            // Arrange
            var dto = new ApproveReportCreateDto
            {
                ReportId = 1,
                ReportType = "RepairReport",
                Status = ReportStatus.Approved, // Not Pending
                Comment = "Test"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.CreateApproveReportAsync(dto));
            Assert.Contains("Chỉ có thể tạo approval với status Pending", ex.Message);
        }

        [Fact]
        public async Task CreateApproveReportAsync_Throws_WhenInvalidReportType()
        {
            // Arrange
            var dto = new ApproveReportCreateDto
            {
                ReportId = 1,
                ReportType = "InvalidType",
                Status = ReportStatus.Pending
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);
            _userContext.SetupGet(u => u.Role).Returns(AccountRole.Technician.ToString());

            _userRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, int>>>(),
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync(2);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.CreateApproveReportAsync(dto));
            Assert.Contains("Loại báo cáo không hợp lệ", ex.Message);
        }

        [Fact]
        public async Task CreateApproveReportAsync_Throws_WhenRepairReportNotFound()
        {
            // Arrange
            var dto = new ApproveReportCreateDto
            {
                ReportId = 999,
                ReportType = "RepairReport",
                Status = ReportStatus.Pending
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);
            _userContext.SetupGet(u => u.Role).Returns(AccountRole.Technician.ToString());

            _userRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, int>>>(),
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync(2);

            _repairReportRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<RepairReport, bool>>>(),
                It.IsAny<Func<IQueryable<RepairReport>, IIncludableQueryable<RepairReport, object>>>()
            )).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.CreateApproveReportAsync(dto));
            Assert.Contains("Không tìm thấy báo cáo sửa chữa", ex.Message);
        }

        #endregion

        #region ApproveReportAsync Tests

        [Fact]
        public async Task ApproveReportAsync_Success_ApprovesRepairReport()
        {
            // Arrange
            var userId = 2; // TechnicianLead
            var dto = new ApproveReportCreateDto
            {
                ReportId = 1,
                ReportType = "RepairReport",
                Status = ReportStatus.Approved,
                Comment = "Approved",
                EscalateToHigherLevel = false
            };

            var pendingApproval = new ReportApproval
            {
                ReportApprovalId = 1,
                RepairReportId = 1,
                UserId = userId,
                Role = AccountRole.TechnicianLead,
                Status = ReportStatus.Pending,
                Comment = "Waiting"
            };

            var repairReport = new RepairReport
            {
                RepairReportId = 1,
                Status = ReportStatus.Pending,
                ReportApprovals = new List<ReportApproval> { pendingApproval },
                Appointment = new Appointment
                {
                    RepairRequest = new RepairRequest(),
                    AppointmentAssigns = new List<AppointmentAssign>()
                }
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(userId);
            _userContext.SetupGet(u => u.Role).Returns(AccountRole.TechnicianLead.ToString());

            _repairReportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairReport, bool>>>(),
                It.IsAny<Func<IQueryable<RepairReport>, IOrderedQueryable<RepairReport>>>(),
                It.IsAny<Func<IQueryable<RepairReport>, IIncludableQueryable<RepairReport, object>>>()
            )).ReturnsAsync(repairReport);

            // Act
            var result = await _service.ApproveReportAsync(dto);

            // Assert
            Assert.True(result);
            Assert.Equal(ReportStatus.Approved, pendingApproval.Status);
            Assert.Equal(dto.Comment, pendingApproval.Comment);
            Assert.Equal(ReportStatus.Approved, repairReport.Status);
            _approvalRepo.Verify(r => r.UpdateAsync(pendingApproval), Times.Once);
            _repairReportRepo.Verify(r => r.UpdateAsync(repairReport), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task ApproveReportAsync_Success_RejectsRepairReport()
        {
            // Arrange
            var userId = 2;
            var dto = new ApproveReportCreateDto
            {
                ReportId = 1,
                ReportType = "RepairReport",
                Status = ReportStatus.Rejected,
                Comment = "Rejected due to errors",
                EscalateToHigherLevel = false
            };

            var pendingApproval = new ReportApproval
            {
                ReportApprovalId = 1,
                RepairReportId = 1,
                UserId = userId,
                Role = AccountRole.TechnicianLead,
                Status = ReportStatus.Pending
            };

            var repairReport = new RepairReport
            {
                RepairReportId = 1,
                Status = ReportStatus.Pending,
                ReportApprovals = new List<ReportApproval> { pendingApproval },
                Appointment = new Appointment
                {
                    RepairRequest = new RepairRequest(),
                    AppointmentAssigns = new List<AppointmentAssign>()
                }
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(userId);
            _userContext.SetupGet(u => u.Role).Returns(AccountRole.TechnicianLead.ToString());

            _repairReportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairReport, bool>>>(),
                It.IsAny<Func<IQueryable<RepairReport>, IOrderedQueryable<RepairReport>>>(),
                It.IsAny<Func<IQueryable<RepairReport>, IIncludableQueryable<RepairReport, object>>>()
            )).ReturnsAsync(repairReport);

            // Act
            var result = await _service.ApproveReportAsync(dto);

            // Assert
            Assert.True(result);
            Assert.Equal(ReportStatus.Rejected, pendingApproval.Status);
            Assert.Equal(ReportStatus.Rejected, repairReport.Status);
        }

        [Fact]
        public async Task ApproveReportAsync_Success_EscalatesToManager()
        {
            // Arrange
            var userId = 2; // TechnicianLead
            var managerId = 3;
            var dto = new ApproveReportCreateDto
            {
                ReportId = 1,
                ReportType = "RepairReport",
                Status = ReportStatus.Approved,
                Comment = "Escalating to Manager",
                EscalateToHigherLevel = true
            };

            var pendingApproval = new ReportApproval
            {
                ReportApprovalId = 1,
                RepairReportId = 1,
                UserId = userId,
                Role = AccountRole.TechnicianLead,
                Status = ReportStatus.Pending
            };

            var repairReport = new RepairReport
            {
                RepairReportId = 1,
                Status = ReportStatus.Pending,
                ReportApprovals = new List<ReportApproval> { pendingApproval },
                Appointment = new Appointment
                {
                    RepairRequest = new RepairRequest(),
                    AppointmentAssigns = new List<AppointmentAssign>()
                }
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(userId);
            _userContext.SetupGet(u => u.Role).Returns(AccountRole.TechnicianLead.ToString());

            _repairReportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairReport, bool>>>(),
                It.IsAny<Func<IQueryable<RepairReport>, IOrderedQueryable<RepairReport>>>(),
                It.IsAny<Func<IQueryable<RepairReport>, IIncludableQueryable<RepairReport, object>>>()
            )).ReturnsAsync(repairReport);

            // Mock finding Manager
            _userRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, int>>>(),
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync(managerId);

            ReportApproval newApproval = null;
            _approvalRepo.Setup(r => r.InsertAsync(It.IsAny<ReportApproval>()))
                .Callback<ReportApproval>(a => newApproval = a)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.ApproveReportAsync(dto);

            // Assert
            Assert.True(result);
            Assert.Equal(ReportStatus.Approved, pendingApproval.Status); // Current approval marked as approved
            Assert.Equal(ReportStatus.Pending, repairReport.Status); // Report still pending
            Assert.NotNull(newApproval);
            Assert.Equal(managerId, newApproval.UserId);
            Assert.Equal(AccountRole.Manager, newApproval.Role);
            Assert.Equal(ReportStatus.Pending, newApproval.Status);
            _approvalRepo.Verify(r => r.UpdateAsync(pendingApproval), Times.Once);
            _approvalRepo.Verify(r => r.InsertAsync(It.IsAny<ReportApproval>()), Times.Once);
        }

        [Fact]
        public async Task ApproveReportAsync_Throws_WhenRepairReportNotFound()
        {
            // Arrange
            var dto = new ApproveReportCreateDto
            {
                ReportId = 999,
                ReportType = "RepairReport",
                Status = ReportStatus.Approved
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);
            _userContext.SetupGet(u => u.Role).Returns(AccountRole.TechnicianLead.ToString());

            _repairReportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairReport, bool>>>(),
                It.IsAny<Func<IQueryable<RepairReport>, IOrderedQueryable<RepairReport>>>(),
                It.IsAny<Func<IQueryable<RepairReport>, IIncludableQueryable<RepairReport, object>>>()
            )).ReturnsAsync((RepairReport)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.ApproveReportAsync(dto));
            Assert.Contains("Không tìm thấy báo cáo sửa chữa", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task ApproveReportAsync_Throws_WhenNoPendingApprovalFound()
        {
            // Arrange
            var userId = 2;
            var dto = new ApproveReportCreateDto
            {
                ReportId = 1,
                ReportType = "RepairReport",
                Status = ReportStatus.Approved
            };

            var repairReport = new RepairReport
            {
                RepairReportId = 1,
                Status = ReportStatus.Pending,
                ReportApprovals = new List<ReportApproval>(), // No pending approval for this user
                Appointment = new Appointment
                {
                    RepairRequest = new RepairRequest(),
                    AppointmentAssigns = new List<AppointmentAssign>()
                }
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(userId);
            _userContext.SetupGet(u => u.Role).Returns(AccountRole.TechnicianLead.ToString());

            _repairReportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairReport, bool>>>(),
                It.IsAny<Func<IQueryable<RepairReport>, IOrderedQueryable<RepairReport>>>(),
                It.IsAny<Func<IQueryable<RepairReport>, IIncludableQueryable<RepairReport, object>>>()
            )).ReturnsAsync(repairReport);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.ApproveReportAsync(dto));
            Assert.Contains("Không tìm thấy approval pending của bạn", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task ApproveReportAsync_Success_ApprovesInspectionReport()
        {
            // Arrange
            var userId = 2;
            var dto = new ApproveReportCreateDto
            {
                ReportId = 1,
                ReportType = "InspectionReport",
                Status = ReportStatus.Approved,
                Comment = "Inspection approved",
                EscalateToHigherLevel = false
            };

            var pendingApproval = new ReportApproval
            {
                ReportApprovalId = 1,
                InspectionReportId = 1,
                UserId = userId,
                Role = AccountRole.TechnicianLead,
                Status = ReportStatus.Pending
            };

            var inspectionReport = new InspectionReport
            {
                InspectionReportId = 1,
                Status = ReportStatus.Pending,
                ReportApprovals = new List<ReportApproval> { pendingApproval },
                Appointment = new Appointment
                {
                    RepairRequest = new RepairRequest()
                }
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(userId);
            _userContext.SetupGet(u => u.Role).Returns(AccountRole.TechnicianLead.ToString());

            _inspectionReportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<InspectionReport, bool>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IOrderedQueryable<InspectionReport>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IIncludableQueryable<InspectionReport, object>>>()
            )).ReturnsAsync(inspectionReport);

            // Act
            var result = await _service.ApproveReportAsync(dto);

            // Assert
            Assert.True(result);
            Assert.Equal(ReportStatus.Approved, pendingApproval.Status);
            Assert.Equal(ReportStatus.Approved, inspectionReport.Status);
            _approvalRepo.Verify(r => r.UpdateAsync(pendingApproval), Times.Once);
            _inspectionReportRepo.Verify(r => r.UpdateAsync(inspectionReport), Times.Once);
        }

        #endregion
    }
}