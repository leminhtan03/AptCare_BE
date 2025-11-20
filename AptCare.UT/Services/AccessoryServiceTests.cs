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
using AptCare.Service.Dtos.AccessoryDto;
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
    public class AccessoryServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _uow = new();
        private readonly Mock<IGenericRepository<Accessory>> _accessoryRepo = new();
        private readonly Mock<IGenericRepository<Media>> _mediaRepo = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<AccessoryService>> _logger = new();
        private readonly Mock<ICloudinaryService> _cloudinary = new();
        private readonly Mock<IRedisCacheService> _cacheService = new();

        private readonly AccessoryService _service;

        public AccessoryServiceTests()
        {
            _uow.Setup(u => u.GetRepository<Accessory>()).Returns(_accessoryRepo.Object);
            _uow.Setup(u => u.GetRepository<Media>()).Returns(_mediaRepo.Object);
            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _uow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            _cacheService.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.GetAsync<AccessoryDto>(It.IsAny<string>()))
                .ReturnsAsync((AccessoryDto)null);

            _cacheService.Setup(c => c.GetAsync<IEnumerable<AccessoryDto>>(It.IsAny<string>()))
                .ReturnsAsync((IEnumerable<AccessoryDto>)null);

            _cacheService.Setup(c => c.GetAsync<IPaginate<AccessoryDto>>(It.IsAny<string>()))
                .ReturnsAsync((IPaginate<AccessoryDto>)null);

            _service = new AccessoryService(_uow.Object, _logger.Object, _cloudinary.Object, _cacheService.Object, _mapper.Object);
        }

        #region CreateAccessoryAsync Tests

        [Fact]
        public async Task CreateAccessoryAsync_Success_WithoutImages()
        {
            // Arrange
            var dto = new AccessoryCreateDto
            {
                Name = "Test Accessory",
                Price = 100,
                Quantity = 10,
                Descrption = "Test Description"
            };

            var accessory = new Accessory { AccessoryId = 1, Name = dto.Name };

            _accessoryRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>()
            )).ReturnsAsync(false);

            _mapper.Setup(m => m.Map<Accessory>(dto)).Returns(accessory);

            // Act
            var result = await _service.CreateAccessoryAsync(dto);

            // Assert
            Assert.Equal("Tạo phụ kiện thành công.", result);
            _accessoryRepo.Verify(r => r.InsertAsync(accessory), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAccessoryAsync_Success_WithImages()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(1024);
            fileMock.Setup(f => f.FileName).Returns("test.jpg");
            fileMock.Setup(f => f.ContentType).Returns("image/jpeg");

            var dto = new AccessoryCreateDto
            {
                Name = "Test Accessory",
                Price = 100,
                Quantity = 10,
                Images = new List<IFormFile> { fileMock.Object }
            };

            var accessory = new Accessory { AccessoryId = 1, Name = dto.Name };

            _accessoryRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>()
            )).ReturnsAsync(false);

            _mapper.Setup(m => m.Map<Accessory>(dto)).Returns(accessory);
            _cloudinary.Setup(c => c.UploadImageAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync("http://cloudinary.com/image.jpg");

            // Act
            var result = await _service.CreateAccessoryAsync(dto);

            // Assert
            Assert.Equal("Tạo phụ kiện thành công.", result);
            _mediaRepo.Verify(r => r.InsertAsync(It.IsAny<Media>()), Times.Once);
            _cloudinary.Verify(c => c.UploadImageAsync(fileMock.Object), Times.Once);
        }

        [Fact]
        public async Task CreateAccessoryAsync_Throws_WhenDuplicateName()
        {
            // Arrange
            var dto = new AccessoryCreateDto { Name = "Duplicate" };

            _accessoryRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>()
            )).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateAccessoryAsync(dto));
            Assert.Equal("Lỗi hệ thống: Phụ kiện đã tồn tại.", ex.Message);
        }

        [Fact]
        public async Task CreateAccessoryAsync_Throws_WhenInvalidFile()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(0); // Invalid file

            var dto = new AccessoryCreateDto
            {
                Name = "Test",
                Images = new List<IFormFile> { fileMock.Object }
            };

            var accessory = new Accessory { AccessoryId = 1 };

            _accessoryRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>()
            )).ReturnsAsync(false);

            _mapper.Setup(m => m.Map<Accessory>(dto)).Returns(accessory);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateAccessoryAsync(dto));
            Assert.Equal("Lỗi hệ thống: File không hợp lệ.", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAccessoryAsync_Throws_WhenUploadFails()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(1024);
            fileMock.Setup(f => f.FileName).Returns("test.jpg");

            var dto = new AccessoryCreateDto
            {
                Name = "Test",
                Images = new List<IFormFile> { fileMock.Object }
            };

            var accessory = new Accessory { AccessoryId = 1 };

            _accessoryRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>()
            )).ReturnsAsync(false);

            _mapper.Setup(m => m.Map<Accessory>(dto)).Returns(accessory);
            _cloudinary.Setup(c => c.UploadImageAsync(It.IsAny<IFormFile>())).ReturnsAsync(string.Empty);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateAccessoryAsync(dto));
            Assert.Equal("Lỗi hệ thống: Có lỗi xảy ra khi gửi file.", ex.Message);
        }

        #endregion

        #region UpdateAccessoryAsync Tests

        [Fact]
        public async Task UpdateAccessoryAsync_Success_BasicUpdate()
        {
            // Arrange
            var id = 1;
            var dto = new AccessoryUpdateDto
            {
                Name = "Updated Name",
                Price = 200,
                Quantity = 20
            };

            var existing = new Accessory { AccessoryId = id, Name = "Old Name" };

            _accessoryRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IOrderedQueryable<Accessory>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>()
            )).ReturnsAsync(existing);

            _accessoryRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>()
            )).ReturnsAsync(false);

            _mapper.Setup(m => m.Map(dto, existing));

            // Act
            var result = await _service.UpdateAccessoryAsync(id, dto);

            // Assert
            Assert.Equal("Cập nhật phụ kiện thành công.", result);
            _accessoryRepo.Verify(r => r.UpdateAsync(existing), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAccessoryAsync_Success_WithNewImages()
        {
            // Arrange
            var id = 1;
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(1024);
            fileMock.Setup(f => f.FileName).Returns("new.jpg");
            fileMock.Setup(f => f.ContentType).Returns("image/jpeg");

            var dto = new AccessoryUpdateDto
            {
                Name = "Updated",
                NewImages = new List<IFormFile> { fileMock.Object }
            };

            var existing = new Accessory { AccessoryId = id, Name = "Old" };

            _accessoryRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IOrderedQueryable<Accessory>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>()
            )).ReturnsAsync(existing);

            _accessoryRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>()
            )).ReturnsAsync(false);

            _cloudinary.Setup(c => c.UploadImageAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync("http://cloudinary.com/new.jpg");

            // Act
            var result = await _service.UpdateAccessoryAsync(id, dto);

            // Assert
            Assert.Equal("Cập nhật phụ kiện thành công.", result);
            _mediaRepo.Verify(r => r.InsertAsync(It.IsAny<Media>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAccessoryAsync_Success_WithRemoveMediaIds()
        {
            // Arrange
            var id = 1;
            var dto = new AccessoryUpdateDto
            {
                Name = "Updated",
                RemoveMediaIds = new List<int> { 100 }
            };

            var existing = new Accessory { AccessoryId = id, Name = "Old" };
            var media = new Media { MediaId = 100 };

            _accessoryRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IOrderedQueryable<Accessory>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>()
            )).ReturnsAsync(existing);

            _accessoryRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>()
            )).ReturnsAsync(false);

            _mediaRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Media, bool>>>(),
                It.IsAny<Func<IQueryable<Media>, IOrderedQueryable<Media>>>(),
                It.IsAny<Func<IQueryable<Media>, IIncludableQueryable<Media, object>>>()
            )).ReturnsAsync(media);

            // Act
            var result = await _service.UpdateAccessoryAsync(id, dto);

            // Assert
            Assert.Equal("Cập nhật phụ kiện thành công.", result);
            _mediaRepo.Verify(r => r.DeleteAsync(media), Times.Once);
        }

        [Fact]
        public async Task UpdateAccessoryAsync_Throws_WhenNotFound()
        {
            // Arrange
            var dto = new AccessoryUpdateDto { Name = "Test" };

            _accessoryRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IOrderedQueryable<Accessory>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>()
            )).ReturnsAsync((Accessory?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.UpdateAccessoryAsync(999, dto));
            Assert.Equal("Lỗi hệ thống: Phụ kiện không tồn tại.", ex.Message);
        }

        [Fact]
        public async Task UpdateAccessoryAsync_Throws_WhenDuplicateName()
        {
            // Arrange
            var id = 1;
            var dto = new AccessoryUpdateDto { Name = "Duplicate" };
            var existing = new Accessory { AccessoryId = id, Name = "Old" };

            _accessoryRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IOrderedQueryable<Accessory>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>()
            )).ReturnsAsync(existing);

            _accessoryRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>()
            )).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.UpdateAccessoryAsync(id, dto));
            Assert.Equal("Lỗi hệ thống: Tên phụ kiện đã tồn tại.", ex.Message);
        }

        [Fact]
        public async Task UpdateAccessoryAsync_Throws_WhenMediaNotFound()
        {
            // Arrange
            var id = 1;
            var dto = new AccessoryUpdateDto
            {
                Name = "Updated",
                RemoveMediaIds = new List<int> { 999 }
            };

            var existing = new Accessory { AccessoryId = id, Name = "Old" };

            _accessoryRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IOrderedQueryable<Accessory>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>()
            )).ReturnsAsync(existing);

            _accessoryRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>()
            )).ReturnsAsync(false);

            _mediaRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Media, bool>>>(),
                It.IsAny<Func<IQueryable<Media>, IOrderedQueryable<Media>>>(),
                It.IsAny<Func<IQueryable<Media>, IIncludableQueryable<Media, object>>>()
            )).ReturnsAsync((Media?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.UpdateAccessoryAsync(id, dto));
            Assert.Equal("Lỗi hệ thống: Không tìm thấy media.", ex.Message);
        }

        #endregion

        #region DeleteAccessoryAsync Tests

        [Fact]
        public async Task DeleteAccessoryAsync_Success()
        {
            // Arrange
            var id = 1;
            var accessory = new Accessory { AccessoryId = id, Name = "Test" };

            _accessoryRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IOrderedQueryable<Accessory>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>()
            )).ReturnsAsync(accessory);

            // Act
            var result = await _service.DeleteAccessoryAsync(id);

            // Assert
            Assert.Equal("Xóa phụ kiện thành công.", result);
            _accessoryRepo.Verify(r => r.DeleteAsync(accessory), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAccessoryAsync_Throws_WhenNotFound()
        {
            // Arrange
            _accessoryRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IOrderedQueryable<Accessory>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>()
            )).ReturnsAsync((Accessory?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.DeleteAccessoryAsync(999));
            Assert.Equal("Lỗi hệ thống: Phụ kiện không tồn tại.", ex.Message);
        }

        #endregion

        #region GetAccessoryByIdAsync Tests

        [Fact]
        public async Task GetAccessoryByIdAsync_Success_ReturnsWithMedia()
        {
            // Arrange
            var id = 1;
            var dto = new AccessoryDto { AccessoryId = id, Name = "Test" };
            var medias = new List<MediaDto>
            {
                new MediaDto { MediaId = 1, FilePath = "http://test.com/1.jpg" }
            };

            _mapper.Setup(m => m.ConfigurationProvider).Returns(new MapperConfiguration(cfg => { }).CreateMapper().ConfigurationProvider);

            _accessoryRepo.Setup(r => r.ProjectToSingleOrDefaultAsync<AccessoryDto>(
                It.IsAny<IConfigurationProvider>(),
                null,
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>()
            )).ReturnsAsync(dto);

            _mediaRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Media, MediaDto>>>(),
                It.IsAny<Expression<Func<Media, bool>>>(),
                It.IsAny<Func<IQueryable<Media>, IOrderedQueryable<Media>>>(),
                It.IsAny<Func<IQueryable<Media>, IIncludableQueryable<Media, object>>>()
            )).ReturnsAsync(medias);

            // Act
            var result = await _service.GetAccessoryByIdAsync(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result.AccessoryId);
            Assert.NotNull(result.Images);
            Assert.Single(result.Images);
        }

        [Fact]
        public async Task GetAccessoryByIdAsync_Throws_WhenNotFound()
        {
            // Arrange
            _mapper.Setup(m => m.ConfigurationProvider).Returns(new MapperConfiguration(cfg => { }).CreateMapper().ConfigurationProvider);

            _accessoryRepo.Setup(r => r.ProjectToSingleOrDefaultAsync<AccessoryDto>(
                It.IsAny<IConfigurationProvider>(),
                null,
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>()
            )).ReturnsAsync((AccessoryDto?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.GetAccessoryByIdAsync(999));
            Assert.Equal("Phụ kiện không tồn tại.", ex.Message);
        }

        #endregion

        #region GetPaginateAccessoryAsync Tests

        [Fact]
        public async Task GetPaginateAccessoryAsync_Success_ReturnsPagedResult()
        {
            // Arrange
            var dto = new PaginateDto { page = 1, size = 10 };
            var items = new List<AccessoryDto>
            {
                new AccessoryDto { AccessoryId = 1, Name = "Test1" },
                new AccessoryDto { AccessoryId = 2, Name = "Test2" }
            };

            var pagedResult = new Paginate<AccessoryDto>
            {
                Items = items,
                Page = 1,
                Size = 10,
                Total = 2,
                TotalPages = 1
            };

            _mapper.Setup(m => m.ConfigurationProvider).Returns(new MapperConfiguration(cfg => { }).CreateMapper().ConfigurationProvider);

            _accessoryRepo.Setup(r => r.ProjectToPagingListAsync<AccessoryDto>(
                It.IsAny<IConfigurationProvider>(),
                null,
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IOrderedQueryable<Accessory>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>(),
                1,
                10
            )).ReturnsAsync(pagedResult);

            _mediaRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Media, MediaDto>>>(),
                It.IsAny<Expression<Func<Media, bool>>>(),
                It.IsAny<Func<IQueryable<Media>, IOrderedQueryable<Media>>>(),
                It.IsAny<Func<IQueryable<Media>, IIncludableQueryable<Media, object>>>()
            )).ReturnsAsync(new List<MediaDto>());

            // Act
            var result = await _service.GetPaginateAccessoryAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(1, result.Page);
        }

        [Fact]
        public async Task GetPaginateAccessoryAsync_Success_WithSearchFilter()
        {
            // Arrange
            var dto = new PaginateDto
            {
                page = 1,
                size = 10,
                search = "test",
                filter = "active"
            };

            var pagedResult = new Paginate<AccessoryDto>
            {
                Items = new List<AccessoryDto>(),
                Page = 1,
                Size = 10,
                Total = 0,
                TotalPages = 0
            };

            _mapper.Setup(m => m.ConfigurationProvider).Returns(new MapperConfiguration(cfg => { }).CreateMapper().ConfigurationProvider);

            _accessoryRepo.Setup(r => r.ProjectToPagingListAsync<AccessoryDto>(
                It.IsAny<IConfigurationProvider>(),
                null,
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IOrderedQueryable<Accessory>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>(),
                1,
                10
            )).ReturnsAsync(pagedResult);

            // Act
            var result = await _service.GetPaginateAccessoryAsync(dto);

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region GetAccessoriesAsync Tests

        [Fact]
        public async Task GetAccessoriesAsync_Success_ReturnsActiveAccessories()
        {
            // Arrange
            var accessories = new List<AccessoryDto>
            {
                new AccessoryDto { AccessoryId = 1, Name = "Active1" },
                new AccessoryDto { AccessoryId = 2, Name = "Active2" }
            };

            _mapper.Setup(m => m.ConfigurationProvider).Returns(new MapperConfiguration(cfg => { }).CreateMapper().ConfigurationProvider);

            _accessoryRepo.Setup(r => r.ProjectToListAsync<AccessoryDto>(
                It.IsAny<IConfigurationProvider>(),
                null,
                It.IsAny<Expression<Func<Accessory, bool>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IOrderedQueryable<Accessory>>>(),
                It.IsAny<Func<IQueryable<Accessory>, IIncludableQueryable<Accessory, object>>>()
            )).ReturnsAsync(accessories);

            _mediaRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Media, MediaDto>>>(),
                It.IsAny<Expression<Func<Media, bool>>>(),
                It.IsAny<Func<IQueryable<Media>, IOrderedQueryable<Media>>>(),
                It.IsAny<Func<IQueryable<Media>, IIncludableQueryable<Media, object>>>()
            )).ReturnsAsync(new List<MediaDto>());

            // Act
            var result = await _service.GetAccessoriesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        #endregion
    }
}