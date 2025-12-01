using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Paginate;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.AppointmentDtos;
using AptCare.Service.Dtos.ApproveReportDtos;
using AptCare.Service.Dtos.InspectionReporDtos;
using AptCare.Service.Dtos.InvoiceDtos;
using AptCare.Service.Dtos.RepairRequestDtos;
using AptCare.Service.Dtos.ReportDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Implements;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
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
    public class InspectionReportServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _uow = new();
        private readonly Mock<IGenericRepository<InspectionReport>> _inspectionReportRepo = new();
        private readonly Mock<IGenericRepository<Appointment>> _appointmentRepo = new();
        private readonly Mock<IGenericRepository<RepairRequest>> _repairRequestRepo = new();
        private readonly Mock<IGenericRepository<Media>> _mediaRepo = new();
        private readonly Mock<IGenericRepository<ReportApproval>> _reportApprovalRepo = new();
        private readonly Mock<IGenericRepository<Invoice>> _invoiceRepo = new();
        private readonly Mock<IUserContext> _userContext = new();
        private readonly Mock<ICloudinaryService> _cloudinaryService = new();
        private readonly Mock<IRepairRequestService> _repairRequestService = new();
        private readonly Mock<IAppointmentService> _appointmentService = new();
        private readonly Mock<IReportApprovalService> _reportApprovalService = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<InspectionReporService>> _logger = new();

        private readonly InspectionReporService _service;

        public InspectionReportServiceTests()
        {
            _uow.Setup(u => u.GetRepository<InspectionReport>()).Returns(_inspectionReportRepo.Object);
            _uow.Setup(u => u.GetRepository<Appointment>()).Returns(_appointmentRepo.Object);
            _uow.Setup(u => u.GetRepository<RepairRequest>()).Returns(_repairRequestRepo.Object);
            _uow.Setup(u => u.GetRepository<Media>()).Returns(_mediaRepo.Object);
            _uow.Setup(u => u.GetRepository<ReportApproval>()).Returns(_reportApprovalRepo.Object);
            _uow.Setup(u => u.GetRepository<Invoice>()).Returns(_invoiceRepo.Object);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            _service = new InspectionReporService(
                _uow.Object,
                _logger.Object,
                _mapper.Object,
                _userContext.Object,
                _cloudinaryService.Object,
                _repairRequestService.Object,
                _appointmentService.Object,
                _reportApprovalService.Object
            );
        }

        #region CreateInspectionReportAsync Tests

        [Fact]
        public async Task CreateInspectionReportAsync_Success_CreatesReportWithImages()
        {
            // Arrange
            var userId = 1;
            var appointmentId = 1;
            var mockFile1 = new Mock<IFormFile>();
            mockFile1.Setup(f => f.FileName).Returns("image1.jpg");
            mockFile1.Setup(f => f.ContentType).Returns("image/jpeg");
            mockFile1.Setup(f => f.Length).Returns(1024);

            var mockFile2 = new Mock<IFormFile>();
            mockFile2.Setup(f => f.FileName).Returns("image2.jpg");
            mockFile2.Setup(f => f.ContentType).Returns("image/jpeg");
            mockFile2.Setup(f => f.Length).Returns(1024);

            var dto = new CreateInspectionReporDto
            {
                AppointmentId = appointmentId,
                Description = "Test inspection",
                Solution = "Test solution",
                FaultOwner = FaultType.BuildingFault,
                SolutionType = SolutionType.Replacement,
                Files = new List<IFormFile> { mockFile1.Object, mockFile2.Object }
            };

            var appointment = new Appointment
            {
                AppointmentId = appointmentId,
                AppointmentTrackings = new List<AppointmentTracking>()
            };

            var repairRequest = new RepairRequest
            {
                RepairRequestId = 1,
                Appointments = new List<Appointment> { appointment }
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(userId);
            _userContext.SetupGet(u => u.Role).Returns(AccountRole.Technician.ToString());

            // Mock appointment exists
            _appointmentRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync(appointment);

            // Mock repair request exists
            _repairRequestRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(repairRequest);

            // Mock no existing report with pending approval
            _inspectionReportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<InspectionReport, bool>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IOrderedQueryable<InspectionReport>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IIncludableQueryable<InspectionReport, object>>>()
            )).ReturnsAsync((InspectionReport)null);

            // Mock cloudinary upload
            _cloudinaryService.SetupSequence(c => c.UploadImageAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync("https://cloudinary.com/image1.jpg")
                .ReturnsAsync("https://cloudinary.com/image2.jpg");

            // Mock insert report
            InspectionReport insertedReport = null;
            _inspectionReportRepo.Setup(r => r.InsertAsync(It.IsAny<InspectionReport>()))
                .Callback<InspectionReport>(ir =>
                {
                    ir.InspectionReportId = 1;
                    insertedReport = ir;
                })
                .Returns(Task.CompletedTask);

            // Mock appointment service
            _appointmentService.Setup(s => s.ToogleAppoimnentStatus(
                It.IsAny<int>(),
                It.IsAny<string>(),
                AppointmentStatus.AwaitingIRApproval
            )).ReturnsAsync(true);

            // Mock report approval service
            _reportApprovalService.Setup(s => s.CreateApproveReportAsync(It.IsAny<ApproveReportCreateDto>()))
                .ReturnsAsync(true);

            // Mock mapper
            var reportDto = new InspectionReportDto { InspectionReportId = 1 };
            _mapper.Setup(m => m.Map<InspectionReport>(dto)).Returns(new InspectionReport
            {
                AppointmentId = dto.AppointmentId,
                Description = dto.Description,
                Solution = dto.Solution,
                FaultOwner = dto.FaultOwner,
                SolutionType = dto.SolutionType
            });
            _mapper.Setup(m => m.Map<InspectionReportDto>(It.IsAny<InspectionReport>())).Returns(reportDto);

            // Act
            var result = await _service.CreateInspectionReportAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(insertedReport);
            Assert.Equal(appointmentId, insertedReport.AppointmentId);
            Assert.Equal(userId, insertedReport.UserId);
            Assert.Equal(dto.Description, insertedReport.Description);
            Assert.Equal(dto.Solution, insertedReport.Solution);
            Assert.Equal(ReportStatus.Pending, insertedReport.Status);
            _inspectionReportRepo.Verify(r => r.InsertAsync(It.IsAny<InspectionReport>()), Times.Once);
            _mediaRepo.Verify(r => r.InsertRangeAsync(It.Is<IEnumerable<Media>>(m => m.Count() == 2)), Times.Once);
            _appointmentService.Verify(s => s.ToogleAppoimnentStatus(appointmentId, It.IsAny<string>(), AppointmentStatus.AwaitingIRApproval), Times.Once);
            _reportApprovalService.Verify(s => s.CreateApproveReportAsync(It.Is<ApproveReportCreateDto>(
                a => a.ReportId == 1 && a.ReportType == "InspectionReport" && a.Status == ReportStatus.Pending
            )), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Exactly(2)); // Once for report, once for media
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateInspectionReportAsync_Throws_WhenAppointmentNotFound()
        {
            // Arrange
            var dto = new CreateInspectionReporDto
            {
                AppointmentId = 999,
                Description = "Test"
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            _appointmentRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync((Appointment)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.CreateInspectionReportAsync(dto));
            Assert.Contains("không tồn tại", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateInspectionReportAsync_Throws_WhenRepairRequestNotFound()
        {
            // Arrange
            var dto = new CreateInspectionReporDto
            {
                AppointmentId = 1,
                Description = "Test"
            };

            var appointment = new Appointment
            {
                AppointmentId = 1,
                AppointmentTrackings = new List<AppointmentTracking>()
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            _appointmentRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync(appointment);

            _repairRequestRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync((RepairRequest)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.CreateInspectionReportAsync(dto));
            Assert.Contains("Không tìm thấy yêu cầu sửa chữa", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateInspectionReportAsync_Throws_WhenReportWithPendingApprovalExists()
        {
            // Arrange
            var dto = new CreateInspectionReporDto
            {
                AppointmentId = 1,
                Description = "Test"
            };

            var appointment = new Appointment
            {
                AppointmentId = 1,
                AppointmentTrackings = new List<AppointmentTracking>()
            };

            var repairRequest = new RepairRequest
            {
                Appointments = new List<Appointment> { appointment }
            };

            var existingReport = new InspectionReport
            {
                InspectionReportId = 1,
                AppointmentId = 1,
                ReportApprovals = new List<ReportApproval>
                {
                    new ReportApproval { Status = ReportStatus.Pending }
                }
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            _appointmentRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync(appointment);

            _repairRequestRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(repairRequest);

            _inspectionReportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<InspectionReport, bool>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IOrderedQueryable<InspectionReport>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IIncludableQueryable<InspectionReport, object>>>()
            )).ReturnsAsync(existingReport);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.CreateInspectionReportAsync(dto));
            Assert.Contains("đang chờ phê duyệt", ex.Message);
        }

        [Fact]
        public async Task CreateInspectionReportAsync_Throws_WhenFileUploadFails()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("image.jpg");
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
            mockFile.Setup(f => f.Length).Returns(1024);

            var dto = new CreateInspectionReporDto
            {
                AppointmentId = 1,
                Description = "Test",
                Files = new List<IFormFile> { mockFile.Object }
            };

            var appointment = new Appointment { AppointmentId = 1, AppointmentTrackings = new List<AppointmentTracking>() };
            var repairRequest = new RepairRequest { Appointments = new List<Appointment> { appointment } };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            _appointmentRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync(appointment);

            _repairRequestRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(repairRequest);

            _inspectionReportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<InspectionReport, bool>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IOrderedQueryable<InspectionReport>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IIncludableQueryable<InspectionReport, object>>>()
            )).ReturnsAsync((InspectionReport)null);

            // Mock upload failure
            _cloudinaryService.Setup(c => c.UploadImageAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync((string)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.CreateInspectionReportAsync(dto));
            Assert.Contains("Có lỗi xảy ra khi gửi file", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        #endregion

        #region GetInspectionReportByIdAsync Tests

        [Fact]
        public async Task GetInspectionReportByIdAsync_Success_ReturnsReport()
        {
            // Arrange
            var reportId = 1;
            var report = new InspectionReport
            {
                InspectionReportId = reportId,
                Description = "Test report",
                Appointment = new Appointment { RepairRequestId = 1},
                CreatedAt = DateTime.Now
            };

            var reportDto = new InspectionReportDto
            {
                InspectionReportId = reportId,
                Description = "Test report"
            };

            _inspectionReportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<InspectionReport, bool>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IOrderedQueryable<InspectionReport>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IIncludableQueryable<InspectionReport, object>>>()
            )).ReturnsAsync(report);

            _mapper.Setup(m => m.Map<InspectionReportDto>(report)).Returns(reportDto);
            _mapper.Setup(m => m.Map<MediaDto>(It.IsAny<Media>())).Returns(new MediaDto());
            _mapper.Setup(m => m.Map<InspectionReportDetailDto>(It.IsAny<InspectionReport>())).Returns(new InspectionReportDetailDto { InspectionReportId = report.InspectionReportId});

            _mediaRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Media, MediaDto>>>(),
                It.IsAny<Expression<Func<Media, bool>>>(),
                It.IsAny<Func<IQueryable<Media>, IOrderedQueryable<Media>>>(),
                It.IsAny<Func<IQueryable<Media>, IIncludableQueryable<Media, object>>>()
            )).ReturnsAsync(new List<MediaDto>());

            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync(new Invoice());

            // Act
            var result = await _service.GetInspectionReportByIdAsync(reportId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(reportId, result.InspectionReportId);
        }

        [Fact]
        public async Task GetInspectionReportByIdAsync_ReturnsEmptyDto_WhenReportNotFound()
        {
            // Arrange
            var reportId = 999;

            _inspectionReportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<InspectionReport, bool>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IOrderedQueryable<InspectionReport>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IIncludableQueryable<InspectionReport, object>>>()
            )).ReturnsAsync((InspectionReport)null);

            // Act
            var result = await _service.GetInspectionReportByIdAsync(reportId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.InspectionReportId); // Empty DTO
        }

        #endregion

        #region GetPaginateInspectionReportsAsync Tests

        [Fact]
        public async Task GetPaginateInspectionReportsAsync_Success_ReturnsPaginatedResults()
        {
            // Arrange
            var userId = 1;
            var filterDto = new InspectionReportFilterDto
            {
                page = 1,
                size = 10,
                search = "test",
                filter = "Pending"
            };

            var reportDetail = new InspectionReportDetailDto
            {
                InspectionReportId = 1,
                Appointment = new AppointmentDto
                {
                    RepairRequest = new RepairRequestBasicDto
                    {
                        RepairRequestId = 1
                    }
                }
            };

            var reports = new List<InspectionReportDetailDto> { reportDetail };

            var paginate = new Paginate<InspectionReportDetailDto>
            {
                Items = reports,
                Page = 1,
                Size = 10,
                Total = 1
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(userId);

            // Fix: Match the actual method signature with selector first
            _inspectionReportRepo.Setup(r => r.GetPagingListAsync(
                It.IsAny<Expression<Func<InspectionReport, InspectionReportDetailDto>>>(),
                It.IsAny<Expression<Func<InspectionReport, bool>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IOrderedQueryable<InspectionReport>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IIncludableQueryable<InspectionReport, object>>>(),
                It.IsAny<int>(),
                It.IsAny<int>()
            )).ReturnsAsync(paginate);

            _mediaRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Media, MediaDto>>>(),
                It.IsAny<Expression<Func<Media, bool>>>(),
                It.IsAny<Func<IQueryable<Media>, IOrderedQueryable<Media>>>(),
                It.IsAny<Func<IQueryable<Media>, IIncludableQueryable<Media, object>>>()
            )).ReturnsAsync(new List<MediaDto>());

            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, InvoiceDto>>>(),
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync((InvoiceDto)null);

            // Act
            var result = await _service.GetPaginateInspectionReportsAsync(filterDto);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(1, result.Total);
        }

        #endregion

        #region UpdateInspectionReportAsync Tests

        [Fact]
        public async Task UpdateInspectionReportAsync_Success_UpdatesReport()
        {
            // Arrange
            var reportId = 1;
            var dto = new UpdateInspectionReporDto
            {
                Description = "Updated description",
                Solution = "Updated solution"
            };

            var existingReport = new InspectionReport
            {
                InspectionReportId = reportId,
                Description = "Old description",
                Solution = "Old solution"
            };

            _inspectionReportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<InspectionReport, bool>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IOrderedQueryable<InspectionReport>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IIncludableQueryable<InspectionReport, object>>>()
            )).ReturnsAsync(existingReport);

            _mapper.Setup(m => m.Map(dto, existingReport)).Returns(existingReport);

            // Act
            var result = await _service.UpdateInspectionReportAsync(reportId, dto);

            // Assert
            Assert.Contains("thành công", result);
            _inspectionReportRepo.Verify(r => r.UpdateAsync(existingReport), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        #endregion
    }
}