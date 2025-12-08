using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.ReportDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Implements;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace AptCare.UT.Services
{
    public class ReportServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _uow = new();
        private readonly Mock<IGenericRepository<Report>> _reportRepo = new();
        private readonly Mock<IGenericRepository<CommonAreaObject>> _objectRepo = new();
        private readonly Mock<IGenericRepository<Media>> _mediaRepo = new();
        private readonly Mock<IUserContext> _userContext = new();
        private readonly Mock<ICloudinaryService> _cloudinaryService = new();
        private readonly Mock<IRedisCacheService> _cacheService = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<ReportService>> _logger = new();

        private readonly ReportService _service;

        public ReportServiceTests()
        {
            _uow.Setup(u => u.GetRepository<Report>()).Returns(_reportRepo.Object);
            _uow.Setup(u => u.GetRepository<CommonAreaObject>()).Returns(_objectRepo.Object);
            _uow.Setup(u => u.GetRepository<Media>()).Returns(_mediaRepo.Object);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            _cacheService.Setup(c => c.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.GetAsync<ReportDto>(It.IsAny<string>()))
                .ReturnsAsync((ReportDto)null);
            _cacheService.Setup(c => c.GetAsync<Paginate<ReportDto>>(It.IsAny<string>()))
                .ReturnsAsync((Paginate<ReportDto>)null);

            _service = new ReportService(
                _uow.Object,
                _logger.Object,
                _mapper.Object,
                _userContext.Object,
                _cloudinaryService.Object,
                _cacheService.Object
            );
        }

        #region CreateReportAsync Tests

        [Fact]
        public async Task CreateReportAsync_Success_WithoutFiles()
        {
            // Arrange
            var userId = 1;
            var dto = new ReportCreateDto
            {
                CommonAreaObjectId = 1,
                Title = "Broken elevator",
                Description = "Elevator not working",
                Files = null
            };

            var commonAreaObject = new CommonAreaObject
            {
                CommonAreaObjectId = 1,
                Status = ActiveStatus.Active,
                CommonArea = new CommonArea { Status = ActiveStatus.Active }
            };

            var report = new Report
            {
                ReportId = 1,
                CommonAreaObjectId = dto.CommonAreaObjectId,
                Title = dto.Title,
                UserId = userId
            };

            var reportDto = new ReportDto
            {
                ReportId = 1,
                Title = dto.Title,
                Medias = new List<MediaDto>()
            };

            _userContext.Setup(u => u.CurrentUserId).Returns(userId);

            _objectRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObject, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IOrderedQueryable<CommonAreaObject>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IIncludableQueryable<CommonAreaObject, object>>>()
            )).ReturnsAsync(commonAreaObject);

            _mapper.Setup(m => m.Map<Report>(dto)).Returns(report);
            _mapper.Setup(m => m.Map<ReportDto>(It.IsAny<Report>())).Returns(reportDto);

            _reportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Report, bool>>>(),
                It.IsAny<Func<IQueryable<Report>, IOrderedQueryable<Report>>>(),
                It.IsAny<Func<IQueryable<Report>, IIncludableQueryable<Report, object>>>()
            )).ReturnsAsync(report);

            _mediaRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Media, MediaDto>>>(),
                It.IsAny<Expression<Func<Media, bool>>>(),
                It.IsAny<Func<IQueryable<Media>, IOrderedQueryable<Media>>>(),
                It.IsAny<Func<IQueryable<Media>, IIncludableQueryable<Media, object>>>()
            )).ReturnsAsync(new List<MediaDto>());

            // Act
            var result = await _service.CreateReportAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.ReportId);
            _reportRepo.Verify(r => r.InsertAsync(It.Is<Report>(rpt => rpt.UserId == userId)), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateReportAsync_Success_WithFiles()
        {
            // Arrange
            var userId = 1;
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");

            var dto = new ReportCreateDto
            {
                CommonAreaObjectId = 1,
                Title = "Broken elevator",
                Description = "Elevator not working",
                Files = new List<IFormFile> { mockFile.Object }
            };

            var commonAreaObject = new CommonAreaObject
            {
                CommonAreaObjectId = 1,
                Status = ActiveStatus.Active,
                CommonArea = new CommonArea { Status = ActiveStatus.Active }
            };

            var report = new Report { ReportId = 1, UserId = userId };

            _userContext.Setup(u => u.CurrentUserId).Returns(userId);

            _objectRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObject, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IOrderedQueryable<CommonAreaObject>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IIncludableQueryable<CommonAreaObject, object>>>()
            )).ReturnsAsync(commonAreaObject);

            _mapper.Setup(m => m.Map<Report>(dto)).Returns(report);
            _cloudinaryService.Setup(c => c.UploadImageAsync(mockFile.Object))
                .ReturnsAsync("https://cloudinary.com/test.jpg");

            _reportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Report, bool>>>(),
                It.IsAny<Func<IQueryable<Report>, IOrderedQueryable<Report>>>(),
                It.IsAny<Func<IQueryable<Report>, IIncludableQueryable<Report, object>>>()
            )).ReturnsAsync(report);

            _mapper.Setup(m => m.Map<ReportDto>(It.IsAny<Report>()))
                .Returns(new ReportDto { ReportId = 1, Medias = new List<MediaDto>() });

            _mediaRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Media, MediaDto>>>(),
                It.IsAny<Expression<Func<Media, bool>>>(),
                It.IsAny<Func<IQueryable<Media>, IOrderedQueryable<Media>>>(),
                It.IsAny<Func<IQueryable<Media>, IIncludableQueryable<Media, object>>>()
            )).ReturnsAsync(new List<MediaDto>());

            // Act
            var result = await _service.CreateReportAsync(dto);

            // Assert
            Assert.NotNull(result);
            _cloudinaryService.Verify(c => c.UploadImageAsync(mockFile.Object), Times.Once);
            _mediaRepo.Verify(r => r.InsertAsync(It.IsAny<Media>()), Times.Once);
        }

        [Fact]
        public async Task CreateReportAsync_Throws_WhenObjectNotFound()
        {
            // Arrange
            var dto = new ReportCreateDto { CommonAreaObjectId = 999 };

            _userContext.Setup(u => u.CurrentUserId).Returns(1);

            _objectRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObject, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IOrderedQueryable<CommonAreaObject>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IIncludableQueryable<CommonAreaObject, object>>>()
            )).ReturnsAsync((CommonAreaObject)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateReportAsync(dto));
            Assert.Contains("Đối tượng khu vực chung không tồn tại", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateReportAsync_Throws_WhenObjectInactive()
        {
            // Arrange
            var dto = new ReportCreateDto { CommonAreaObjectId = 1 };

            _userContext.Setup(u => u.CurrentUserId).Returns(1);

            var commonAreaObject = new CommonAreaObject
            {
                CommonAreaObjectId = 1,
                Status = ActiveStatus.Inactive,
                CommonArea = new CommonArea()
            };

            _objectRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObject, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IOrderedQueryable<CommonAreaObject>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IIncludableQueryable<CommonAreaObject, object>>>()
            )).ReturnsAsync(commonAreaObject);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateReportAsync(dto));
            Assert.Contains("đã ngưng hoạt động", ex.Message);
        }

        #endregion

        #region UpdateReportAsync Tests

        [Fact]
        public async Task UpdateReportAsync_Success()
        {
            // Arrange
            var userId = 1;
            var reportId = 1;
            var dto = new ReportUpdateDto
            {
                CommonAreaObjectId = 1,
                Title = "Updated title",
                Description = "Updated description"
            };

            var report = new Report
            {
                ReportId = reportId,
                UserId = userId,
                Title = "Old title"
            };

            var commonAreaObject = new CommonAreaObject
            {
                CommonAreaObjectId = 1,
                Status = ActiveStatus.Active,
                CommonArea = new CommonArea { Status = ActiveStatus.Active }
            };

            _userContext.Setup(u => u.CurrentUserId).Returns(userId);

            _reportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Report, bool>>>(),
                It.IsAny<Func<IQueryable<Report>, IOrderedQueryable<Report>>>(),
                It.IsAny<Func<IQueryable<Report>, IIncludableQueryable<Report, object>>>()
            )).ReturnsAsync(report);

            _objectRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObject, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IOrderedQueryable<CommonAreaObject>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IIncludableQueryable<CommonAreaObject, object>>>()
            )).ReturnsAsync(commonAreaObject);

            _mapper.Setup(m => m.Map(dto, report));

            // Act
            var result = await _service.UpdateReportAsync(reportId, dto);

            // Assert
            Assert.Equal("Cập nhật báo cáo thành công.", result);
            _reportRepo.Verify(r => r.UpdateAsync(report), Times.Once);
        }

        [Fact]
        public async Task UpdateReportAsync_Throws_WhenNotOwner()
        {
            // Arrange
            var userId = 1;
            var reportId = 1;
            var dto = new ReportUpdateDto { CommonAreaObjectId = 1 };

            var report = new Report
            {
                ReportId = reportId,
                UserId = 999 // Different user
            };

            _userContext.Setup(u => u.CurrentUserId).Returns(userId);

            _reportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Report, bool>>>(),
                It.IsAny<Func<IQueryable<Report>, IOrderedQueryable<Report>>>(),
                It.IsAny<Func<IQueryable<Report>, IIncludableQueryable<Report, object>>>()
            )).ReturnsAsync(report);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.UpdateReportAsync(reportId, dto));
            Assert.Contains("không có quyền cập nhật", ex.Message);
        }

        #endregion

        #region DeleteReportAsync Tests

        [Fact]
        public async Task DeleteReportAsync_Success()
        {
            // Arrange
            var userId = 1;
            var reportId = 1;
            var report = new Report { ReportId = reportId, UserId = userId };

            _userContext.Setup(u => u.CurrentUserId).Returns(userId);

            _reportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Report, bool>>>(),
                It.IsAny<Func<IQueryable<Report>, IOrderedQueryable<Report>>>(),
                It.IsAny<Func<IQueryable<Report>, IIncludableQueryable<Report, object>>>()
            )).ReturnsAsync(report);

            // Act
            var result = await _service.DeleteReportAsync(reportId);

            // Assert
            Assert.Equal("Xóa báo cáo thành công.", result);
            _reportRepo.Verify(r => r.DeleteAsync(report), Times.Once);
        }

        [Fact]
        public async Task DeleteReportAsync_Throws_WhenNotOwner()
        {
            // Arrange
            var userId = 1;
            var reportId = 1;
            var report = new Report { ReportId = reportId, UserId = 999 };

            _userContext.Setup(u => u.CurrentUserId).Returns(userId);

            _reportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Report, bool>>>(),
                It.IsAny<Func<IQueryable<Report>, IOrderedQueryable<Report>>>(),
                It.IsAny<Func<IQueryable<Report>, IIncludableQueryable<Report, object>>>()
            )).ReturnsAsync(report);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.DeleteReportAsync(reportId));
            Assert.Contains("không có quyền xóa", ex.Message);
        }

        #endregion

        #region ActivateReportAsync Tests

        [Fact]
        public async Task ActivateReportAsync_Success()
        {
            // Arrange
            var reportId = 1;
            var report = new Report { ReportId = reportId, Status = ActiveStatus.Inactive };

            _reportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Report, bool>>>(),
                It.IsAny<Func<IQueryable<Report>, IOrderedQueryable<Report>>>(),
                It.IsAny<Func<IQueryable<Report>, IIncludableQueryable<Report, object>>>()
            )).ReturnsAsync(report);

            // Act
            var result = await _service.ActivateReportAsync(reportId);

            // Assert
            Assert.Equal("Kích hoạt báo cáo thành công.", result);
            Assert.Equal(ActiveStatus.Active, report.Status);
            _reportRepo.Verify(r => r.UpdateAsync(report), Times.Once);
        }

        [Fact]
        public async Task ActivateReportAsync_Throws_WhenAlreadyActive()
        {
            // Arrange
            var reportId = 1;
            var report = new Report { ReportId = reportId, Status = ActiveStatus.Active };

            _reportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Report, bool>>>(),
                It.IsAny<Func<IQueryable<Report>, IOrderedQueryable<Report>>>(),
                It.IsAny<Func<IQueryable<Report>, IIncludableQueryable<Report, object>>>()
            )).ReturnsAsync(report);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.ActivateReportAsync(reportId));
            Assert.Contains("đã ở trạng thái hoạt động", ex.Message);
        }

        #endregion
    }
}