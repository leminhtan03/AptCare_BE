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
using AptCare.Service.Dtos.ApproveReportDtos;
using AptCare.Service.Dtos.RepairReportDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Implements;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AptCare.UT.Services
{
    public class RepairReportServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _uow = new();
        private readonly Mock<IGenericRepository<RepairReport>> _reportRepo = new();
        private readonly Mock<IGenericRepository<Appointment>> _apptRepo = new();
        private readonly Mock<IGenericRepository<Media>> _mediaRepo = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<IUserContext> _userContext = new();
        private readonly Mock<ICloudinaryService> _cloudinary = new();
        private readonly Mock<IReportApprovalService> _approvalService = new();
        private readonly Mock<IAppointmentService> _appointmentService = new();
        private readonly Mock<ILogger<RepairReportService>> _logger = new();

        private readonly RepairReportService _service;

        public RepairReportServiceTests()
        {
            _uow.Setup(u => u.GetRepository<RepairReport>()).Returns(_reportRepo.Object);
            _uow.Setup(u => u.GetRepository<Appointment>()).Returns(_apptRepo.Object);
            _uow.Setup(u => u.GetRepository<Media>()).Returns(_mediaRepo.Object);
            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _uow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            _service = new RepairReportService(
                _uow.Object,
                _logger.Object,
                _mapper.Object,
                _userContext.Object,
                _cloudinary.Object,
                _approvalService.Object,
                _appointmentService.Object);
        }

        #region CreateRepairReportAsync Tests

        [Fact]
        public async Task CreateRepairReportAsync_Success_CreatesReport()
        {
            // Arrange
            var dto = new CreateRepairReportDto
            {
                AppointmentId = 1,
                WorkDescription = "Repair completed",
                Note = "All good"
            };

            var appointment = new Appointment
            {
                AppointmentId = 1,
                AppointmentTrackings = new List<AppointmentTracking>
                {
                    new AppointmentTracking { Status = AppointmentStatus.InRepair, UpdatedAt = DateTime.UtcNow }
                },
                RepairRequest = new RepairRequest(),
                RepairReport = null
            };

            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Appointment>, System.Linq.IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync(appointment);

            var report = new RepairReport { RepairReportId = 1 };
            _mapper.Setup(m => m.Map<RepairReport>(dto)).Returns(report);
            _mapper.Setup(m => m.Map<RepairReportDto>(report)).Returns(new RepairReportDto { RepairReportId = 1 });

            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            _approvalService.Setup(s => s.CreateApproveReportAsync(It.IsAny<ApproveReportCreateDto>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.CreateRepairReportAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.RepairReportId);
            _reportRepo.Verify(r => r.InsertAsync(It.IsAny<RepairReport>()), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateRepairReportAsync_Throws_WhenAppointmentNotFound()
        {
            // Arrange
            var dto = new CreateRepairReportDto { AppointmentId = 999 };

            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Appointment>, System.Linq.IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync((Appointment)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateRepairReportAsync(dto));
            Assert.Equal("Cuộc hẹn không tồn tại.", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateRepairReportAsync_Throws_WhenAppointmentNotStarted()
        {
            // Arrange
            var dto = new CreateRepairReportDto { AppointmentId = 1 };

            var appointment = new Appointment
            {
                AppointmentId = 1,
                AppointmentTrackings = new List<AppointmentTracking>
                {
                    new AppointmentTracking { Status = AppointmentStatus.Pending, UpdatedAt = DateTime.UtcNow }
                }
            };

            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Appointment>, System.Linq.IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync(appointment);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateRepairReportAsync(dto));
            Assert.Equal("Cuộc hẹn chưa bắt đầu hoặc đang chờ phân công.", ex.Message);
        }

        [Fact]
        public async Task CreateRepairReportAsync_Throws_WhenReportAlreadyExists()
        {
            // Arrange
            var dto = new CreateRepairReportDto { AppointmentId = 1 };

            var appointment = new Appointment
            {
                AppointmentId = 1,
                AppointmentTrackings = new List<AppointmentTracking>
                {
                    new AppointmentTracking { Status = AppointmentStatus.InRepair, UpdatedAt = DateTime.UtcNow }
                },
                RepairReport = new RepairReport { RepairReportId = 1 }
            };

            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Appointment>, System.Linq.IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync(appointment);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateRepairReportAsync(dto));
            Assert.Equal("Cuộc hẹn này đã có báo cáo sửa chữa.", ex.Message);
        }

        [Fact]
        public async Task CreateRepairReportAsync_Success_WithFiles()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(1024);
            fileMock.Setup(f => f.FileName).Returns("report.jpg");
            fileMock.Setup(f => f.ContentType).Returns("image/jpeg");

            var dto = new CreateRepairReportDto
            {
                AppointmentId = 1,
                WorkDescription = "Test",
                Files = new List<IFormFile> { fileMock.Object }
            };

            var appointment = new Appointment
            {
                AppointmentId = 1,
                AppointmentTrackings = new List<AppointmentTracking>
                {
                    new AppointmentTracking { Status = AppointmentStatus.InRepair, UpdatedAt = DateTime.UtcNow }
                },
                RepairRequest = new RepairRequest()
            };

            _apptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Appointment, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Appointment>, System.Linq.IOrderedQueryable<Appointment>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Appointment>, IIncludableQueryable<Appointment, object>>>()
            )).ReturnsAsync(appointment);

            var report = new RepairReport { RepairReportId = 1 };
            _mapper.Setup(m => m.Map<RepairReport>(dto)).Returns(report);
            _mapper.Setup(m => m.Map<RepairReportDto>(report)).Returns(new RepairReportDto { RepairReportId = 1 });

            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            _cloudinary.Setup(c => c.UploadImageAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync("http://cloudinary.com/report.jpg");

            _approvalService.Setup(s => s.CreateApproveReportAsync(It.IsAny<ApproveReportCreateDto>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.CreateRepairReportAsync(dto);

            // Assert
            Assert.NotNull(result);
            _mediaRepo.Verify(r => r.InsertAsync(It.IsAny<Media>()), Times.Once);
        }

        #endregion

        #region GetRepairReportByIdAsync Tests

        [Fact]
        public async Task GetRepairReportByIdAsync_Success_ReturnsReport()
        {
            // Arrange
            var id = 1;
            var report = new RepairReport
            {
                RepairReportId = id,
                Description = "Test Report"
            };

            _reportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairReport, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairReport>, System.Linq.IOrderedQueryable<RepairReport>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairReport>, IIncludableQueryable<RepairReport, object>>>()
            )).ReturnsAsync(report);

            _mapper.Setup(m => m.Map<RepairReportDto>(report))
                .Returns(new RepairReportDto { RepairReportId = id });

            _mediaRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Media, MediaDto>>>(),
                It.IsAny<Expression<Func<Media, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Media>, System.Linq.IOrderedQueryable<Media>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Media>, IIncludableQueryable<Media, object>>>()
            )).ReturnsAsync(new List<MediaDto>());

            // Act
            var result = await _service.GetRepairReportByIdAsync(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result.RepairReportId);
        }

        [Fact]
        public async Task GetRepairReportByIdAsync_Throws_WhenNotFound()
        {
            // Arrange
            _reportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairReport, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairReport>, System.Linq.IOrderedQueryable<RepairReport>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairReport>, IIncludableQueryable<RepairReport, object>>>()
            )).ReturnsAsync((RepairReport)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.GetRepairReportByIdAsync(999));
            Assert.Equal("Báo cáo sửa chữa không tồn tại.", ex.Message);
        }

        #endregion

        #region UpdateRepairReportAsync Tests

        [Fact]
        public async Task UpdateRepairReportAsync_Success_UpdatesReport()
        {
            // Arrange
            var id = 1;
            var dto = new UpdateRepairReportDto
            {
                WorkDescription = "Updated description"
            };

            var report = new RepairReport
            {
                RepairReportId = id,
                Status = ReportStatus.Pending
            };

            _reportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairReport, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairReport>, System.Linq.IOrderedQueryable<RepairReport>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairReport>, IIncludableQueryable<RepairReport, object>>>()
            )).ReturnsAsync(report);

            _mapper.Setup(m => m.Map(dto, report));

            // Act
            var result = await _service.UpdateRepairReportAsync(id, dto);

            // Assert
            Assert.Equal("Cập nhật báo cáo sửa chữa thành công.", result);
            _reportRepo.Verify(r => r.UpdateAsync(report), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateRepairReportAsync_Throws_WhenNotFound()
        {
            // Arrange
            var dto = new UpdateRepairReportDto();

            _reportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairReport, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairReport>, System.Linq.IOrderedQueryable<RepairReport>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairReport>, IIncludableQueryable<RepairReport, object>>>()
            )).ReturnsAsync((RepairReport)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.UpdateRepairReportAsync(999, dto));
            Assert.Equal("Báo cáo sửa chữa không tồn tại.", ex.Message);
        }

        [Fact]
        public async Task UpdateRepairReportAsync_Throws_WhenAlreadyApproved()
        {
            // Arrange
            var id = 1;
            var dto = new UpdateRepairReportDto();

            var report = new RepairReport
            {
                RepairReportId = id,
                Status = ReportStatus.Approved
            };

            _reportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairReport, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairReport>, System.Linq.IOrderedQueryable<RepairReport>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairReport>, IIncludableQueryable<RepairReport, object>>>()
            )).ReturnsAsync(report);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.UpdateRepairReportAsync(id, dto));
            Assert.Equal("Không thể cập nhật báo cáo đã được phê duyệt.", ex.Message);
        }

        #endregion

        #region GetRepairReportByAppointmentIdAsync Tests

        [Fact]
        public async Task GetRepairReportByAppointmentIdAsync_Success_ReturnsReport()
        {
            // Arrange
            var appointmentId = 1;
            var report = new RepairReport
            {
                RepairReportId = 1,
                AppointmentId = appointmentId
            };

            _reportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairReport, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairReport>, System.Linq.IOrderedQueryable<RepairReport>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairReport>, IIncludableQueryable<RepairReport, object>>>()
            )).ReturnsAsync(report);

            _mapper.Setup(m => m.Map<RepairReportDto>(report))
                .Returns(new RepairReportDto { RepairReportId = 1, AppointmentId = appointmentId });

            _mediaRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Media, MediaDto>>>(),
                It.IsAny<Expression<Func<Media, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Media>, System.Linq.IOrderedQueryable<Media>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Media>, IIncludableQueryable<Media, object>>>()
            )).ReturnsAsync(new List<MediaDto>());

            // Act
            var result = await _service.GetRepairReportByAppointmentIdAsync(appointmentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(appointmentId, result.AppointmentId);
        }

        [Fact]
        public async Task GetRepairReportByAppointmentIdAsync_Throws_WhenNotFound()
        {
            // Arrange
            _reportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairReport, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairReport>, System.Linq.IOrderedQueryable<RepairReport>>>(),
                It.IsAny<Func<System.Linq.IQueryable<RepairReport>, IIncludableQueryable<RepairReport, object>>>()
            )).ReturnsAsync((RepairReport)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.GetRepairReportByAppointmentIdAsync(999));
            Assert.Equal("Không tìm thấy báo cáo sửa chữa cho cuộc hẹn này.", ex.Message);
        }

        #endregion
    }
}