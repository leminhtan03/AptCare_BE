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
        private readonly Mock<IGenericRepository<Invoice>> _invoiceRepo = new();
        private readonly Mock<IGenericRepository<Budget>> _budgetRepo = new();
        private readonly Mock<IGenericRepository<Transaction>> _transactionRepo = new();
        private readonly Mock<IGenericRepository<Accessory>> _accessoryRepo = new();

        private readonly Mock<IUserContext> _userContext = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<ReportApprovalService>> _logger = new();
        private readonly Mock<IRedisCacheService> _cacheService = new();

        private readonly ReportApprovalService _service;

        public ReportApprovalServiceTests()
        {
            _uow.Setup(u => u.GetRepository<ReportApproval>()).Returns(_approvalRepo.Object);
            _uow.Setup(u => u.GetRepository<RepairReport>()).Returns(_repairReportRepo.Object);
            _uow.Setup(u => u.GetRepository<InspectionReport>()).Returns(_inspectionReportRepo.Object);
            _uow.Setup(u => u.GetRepository<User>()).Returns(_userRepo.Object);
            _uow.Setup(u => u.GetRepository<Invoice>()).Returns(_invoiceRepo.Object);
            _uow.Setup(u => u.GetRepository<Budget>()).Returns(_budgetRepo.Object);
            _uow.Setup(u => u.GetRepository<Transaction>()).Returns(_transactionRepo.Object);
            _uow.Setup(u => u.GetRepository<Accessory>()).Returns(_accessoryRepo.Object);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            _service = new ReportApprovalService(
                _uow.Object,
                _userContext.Object,
                _logger.Object,
                _mapper.Object,
                _cacheService.Object
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

        public async Task ApproveReportAsync_Success_ApprovesInspectionReport_WithPurchaseInvoice()
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

            var now = DateTime.Now;
            var inspectionReport = new InspectionReport
            {
                InspectionReportId = 1,
                Status = ReportStatus.Pending,
                SolutionType = SolutionType.Replacement, // Internal repair
                CreatedAt = now,
                ReportApprovals = new List<ReportApproval> { pendingApproval },
                Appointment = new Appointment
                {
                    AppointmentId = 1,
                    RepairRequestId = 10,
                    RepairRequest = new RepairRequest { RepairRequestId = 10 }
                }
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(userId);
            _userContext.SetupGet(u => u.Role).Returns(AccountRole.TechnicianLead.ToString());

            _inspectionReportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<InspectionReport, bool>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IOrderedQueryable<InspectionReport>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IIncludableQueryable<InspectionReport, object>>>()
            )).ReturnsAsync(inspectionReport);

            // ✅ Mock main invoice (InternalRepair)
            var mainInvoice = new Invoice
            {
                InvoiceId = 100,
                RepairRequestId = 10,
                Type = InvoiceType.InternalRepair,
                Status = InvoiceStatus.Draft,
                CreatedAt = now.AddMinutes(-10),
                TotalAmount = 500000,
                IsChargeable = false, // Dùng budget
                InvoiceAccessories = new List<InvoiceAccessory>
        {
            new InvoiceAccessory
            {
                InvoiceAccessoryId = 1,
                AccessoryId = 1,
                Name = "Ống nước",
                Quantity = 5,
                Price = 50000
            }
        },
                InvoiceServices = new List<Repository.Entities.InvoiceService>()
            };

            // ✅ Mock purchase invoice (AccessoryPurchase)
            var purchaseInvoice = new Invoice
            {
                InvoiceId = 101,
                RepairRequestId = 10,
                Type = InvoiceType.AccessoryPurchase,
                Status = InvoiceStatus.Draft,
                CreatedAt = now.AddMinutes(-5),
                TotalAmount = 1000000,
                IsChargeable = false,
                InvoiceAccessories = new List<InvoiceAccessory>
        {
            new InvoiceAccessory
            {
                InvoiceAccessoryId = 2,
                AccessoryId = 2,
                Name = "Van điều áp",
                Quantity = 2,
                Price = 500000
            }
        },
                InvoiceServices = new List<Repository.Entities.InvoiceService>()
            };

            // ✅ Setup invoice queries
            _invoiceRepo.SetupSequence(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            ))
            .ReturnsAsync(mainInvoice)      // Lần 1: Main invoice
            .ReturnsAsync(purchaseInvoice); // Lần 2: Purchase invoice

            // ✅ Mock Accessory (cho main invoice)
            var accessory = new Accessory
            {
                AccessoryId = 1,
                Name = "Ống nước",
                Quantity = 100, // Đủ số lượng
                Status = ActiveStatus.Active
            };

            _accessoryRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IOrderedQueryable<Accessory>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>()
            )).ReturnsAsync(accessory);

            // ✅ Mock Budget
            var budget = new Budget
            {
                BudgetId = 1,
                Amount = 5000000
            };

            _budgetRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Budget, bool>>>(),
                It.IsAny<Func<IQueryable<Budget>, IOrderedQueryable<Budget>>>(),
                It.IsAny<Func<IQueryable<Budget>, IIncludableQueryable<Budget, object>>>()
            )).ReturnsAsync(budget);

            // ✅ Mock Transaction insert
            _transactionRepo.Setup(r => r.InsertAsync(It.IsAny<Transaction>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.ApproveReportAsync(dto);

            // Assert
            Assert.True(result);
            Assert.Equal(ReportStatus.Approved, pendingApproval.Status);
            Assert.Equal(ReportStatus.Approved, inspectionReport.Status);

            // ✅ Verify budget được trừ
            Assert.Equal(5000000 - 1000000, budget.Amount); // 5M - 1M = 4M
            _budgetRepo.Verify(r => r.UpdateAsync(It.Is<Budget>(b =>
                b.Amount == 4000000
            )), Times.Once);

            // ✅ Verify transaction được tạo
            _transactionRepo.Verify(r => r.InsertAsync(It.Is<Transaction>(t =>
                t.Amount == purchaseInvoice.TotalAmount &&
                t.Status == TransactionStatus.Success &&
                t.Direction == TransactionDirection.Expense &&
                t.Provider == PaymentProvider.Budget
            )), Times.Once);

            // ✅ Verify invoices được update
            _invoiceRepo.Verify(r => r.UpdateAsync(It.Is<Invoice>(i =>
                i.InvoiceId == mainInvoice.InvoiceId &&
                i.Status == InvoiceStatus.Approved
            )), Times.Once);

            _invoiceRepo.Verify(r => r.UpdateAsync(It.Is<Invoice>(i =>
                i.InvoiceId == purchaseInvoice.InvoiceId &&
                i.Status == InvoiceStatus.Approved
            )), Times.Once);

            // ✅ Verify accessory quantity được trừ
            Assert.Equal(100 - 5, accessory.Quantity); // 100 - 5 = 95
            _accessoryRepo.Verify(r => r.UpdateAsync(It.Is<Accessory>(a =>
                a.Quantity == 95
            )), Times.Once);
        }

        [Fact]
        public async Task ApproveReportAsync_Throws_WhenBudgetInsufficient()
        {
            // Arrange
            var userId = 2;
            var dto = new ApproveReportCreateDto
            {
                ReportId = 1,
                ReportType = "InspectionReport",
                Status = ReportStatus.Approved,
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

            var now = DateTime.Now;
            var inspectionReport = new InspectionReport
            {
                InspectionReportId = 1,
                Status = ReportStatus.Pending,
                SolutionType = SolutionType.Replacement,
                CreatedAt = now,
                ReportApprovals = new List<ReportApproval> { pendingApproval },
                Appointment = new Appointment
                {
                    RepairRequestId = 10,
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

            var mainInvoice = new Invoice
            {
                InvoiceId = 100,
                Type = InvoiceType.InternalRepair,
                Status = InvoiceStatus.Draft,
                CreatedAt = now.AddMinutes(-10),
                IsChargeable = false,
                InvoiceAccessories = new List<InvoiceAccessory>()
            };

            var purchaseInvoice = new Invoice
            {
                InvoiceId = 101,
                Type = InvoiceType.AccessoryPurchase,
                Status = InvoiceStatus.Draft,
                CreatedAt = now.AddMinutes(-5),
                TotalAmount = 10000000, // 10M
                InvoiceAccessories = new List<InvoiceAccessory>()
            };

            _invoiceRepo.SetupSequence(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            ))
            .ReturnsAsync(mainInvoice)
            .ReturnsAsync(purchaseInvoice);

            // ✅ Budget không đủ
            var budget = new Budget
            {
                BudgetId = 1,
                Amount = 5000000 // Chỉ 5M, cần 10M
            };

            _budgetRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Budget, bool>>>(),
                It.IsAny<Func<IQueryable<Budget>, IOrderedQueryable<Budget>>>(),
                It.IsAny<Func<IQueryable<Budget>, IIncludableQueryable<Budget, object>>>()
            )).ReturnsAsync(budget);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.ApproveReportAsync(dto));

            Assert.Contains("Ngân sách không đủ", ex.Message);

            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task ApproveReportAsync_Success_ApprovesInspectionReport_WithMixedAccessories()
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

            var now = DateTime.Now;
            var inspectionReport = new InspectionReport
            {
                InspectionReportId = 1,
                Status = ReportStatus.Pending,
                SolutionType = SolutionType.Replacement,
                CreatedAt = now,
                ReportApprovals = new List<ReportApproval> { pendingApproval },
                Appointment = new Appointment
                {
                    AppointmentId = 1,
                    RepairRequestId = 10,
                    RepairRequest = new RepairRequest { RepairRequestId = 10 }
                }
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(userId);
            _userContext.SetupGet(u => u.Role).Returns(AccountRole.TechnicianLead.ToString());

            _inspectionReportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<InspectionReport, bool>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IOrderedQueryable<InspectionReport>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IIncludableQueryable<InspectionReport, object>>>()
            )).ReturnsAsync(inspectionReport);

            // ✅ Mock invoice với cả FromStock và ToBePurchased
            var mainInvoice = new Invoice
            {
                InvoiceId = 100,
                RepairRequestId = 10,
                Type = InvoiceType.InternalRepair,
                Status = InvoiceStatus.Draft,
                CreatedAt = now.AddMinutes(-1),
                TotalAmount = 1500000,
                IsChargeable = false,
                InvoiceAccessories = new List<InvoiceAccessory>
                {
                    // Vật tư từ kho
                    new InvoiceAccessory
                    {
                        InvoiceAccessoryId = 1,
                        AccessoryId = 1,
                        Name = "Ống nước",
                        Quantity = 5,
                        Price = 50000,
                        SourceType = InvoiceAccessorySourceType.FromStock
                    },
                    // Vật tư cần mua
                    new InvoiceAccessory
                    {
                        InvoiceAccessoryId = 2,
                        AccessoryId = 2,
                        Name = "Van điều áp",
                        Quantity = 2,
                        Price = 500000,
                        SourceType = InvoiceAccessorySourceType.ToBePurchased
                    }
                },
                InvoiceServices = new List<Repository.Entities.InvoiceService>()
            };

            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync(mainInvoice);

            // ✅ Mock accessory từ kho (có sẵn)
            var stockAccessory = new Accessory
            {
                AccessoryId = 1,
                Name = "Ống nước",
                Quantity = 100,
                Status = ActiveStatus.Active
            };

            // ✅ Mock accessory cần mua (mới tạo, Darft)
            var newAccessory = new Accessory
            {
                AccessoryId = 2,
                Name = "Van điều áp",
                Quantity = 0,
                Status = ActiveStatus.Darft
            };

            _accessoryRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IOrderedQueryable<Accessory>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>()
            )).ReturnsAsync((
                Expression<Func<Accessory, bool>> predicate,
                Func<IQueryable<Accessory>, IOrderedQueryable<Accessory>> orderBy,
                Func<IQueryable<Accessory>, IIncludableQueryable<Accessory, object>> include) =>
            {
                var func = predicate.Compile();
                if (func(stockAccessory)) return stockAccessory;
                if (func(newAccessory)) return newAccessory;
                return null;
            });

            // ✅ Mock Budget
            var budget = new Budget
            {
                BudgetId = 1,
                Amount = 5000000
            };

            _budgetRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Budget, bool>>>(),
                It.IsAny<Func<IQueryable<Budget>, IOrderedQueryable<Budget>>>(),
                It.IsAny<Func<IQueryable<Budget>, IIncludableQueryable<Budget, object>>>()
            )).ReturnsAsync(budget);

            var stockTxRepo = new Mock<IGenericRepository<AccessoryStockTransaction>>();
            _uow.Setup(u => u.GetRepository<AccessoryStockTransaction>()).Returns(stockTxRepo.Object);

            _transactionRepo.Setup(r => r.InsertAsync(It.IsAny<Transaction>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.ApproveReportAsync(dto);

            // Assert
            Assert.True(result);
            Assert.Equal(ReportStatus.Approved, inspectionReport.Status);
            Assert.Equal(InvoiceStatus.Approved, mainInvoice.Status);

            // ✅ Verify tạo 1 phiếu xuất (FromStock)
            stockTxRepo.Verify(r => r.InsertAsync(It.Is<AccessoryStockTransaction>(st =>
                st.Type == StockTransactionType.Export &&
                st.AccessoryId == 1 &&
                st.Quantity == 5
            )), Times.Once);

            // ✅ Verify tạo 1 phiếu nhập (ToBePurchased)
            stockTxRepo.Verify(r => r.InsertAsync(It.Is<AccessoryStockTransaction>(st =>
                st.Type == StockTransactionType.Import &&
                st.AccessoryId == 2 &&
                st.Quantity == 2
            )), Times.Once);

            // ✅ Verify trừ kho cho vật tư FromStock
            Assert.Equal(95, stockAccessory.Quantity); // 100 - 5

            // ✅ Verify kích hoạt vật tư mới
            Assert.Equal(ActiveStatus.Active, newAccessory.Status);

            // ✅ Verify trừ budget
            Assert.Equal(4000000, budget.Amount); // 5M - 1M

            // ✅ Verify tạo transaction
            _transactionRepo.Verify(r => r.InsertAsync(It.Is<Transaction>(t =>
                t.Amount == 1000000 &&
                t.Direction == TransactionDirection.Expense
            )), Times.Once);
        }
        #endregion
    }
}