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
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Implements;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AptCare.UT.Services
{
    public class FloorServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _uow = new();
        private readonly Mock<IGenericRepository<Floor>> _floorRepo = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<FloorService>> _logger = new();

        private readonly FloorService _service;

        public FloorServiceTests()
        {
            _uow.Setup(u => u.GetRepository<Floor>()).Returns(_floorRepo.Object);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            _service = new FloorService(_uow.Object, _logger.Object, _mapper.Object);
        }

        #region CreateFloorAsync Tests

        [Fact]
        public async Task CreateFloorAsync_Success_CreatesFloor()
        {
            // Arrange
            var dto = new FloorCreateDto
            {
                FloorNumber = 5,
                Description = "Test Floor"
            };

            var floor = new Floor
            {
                FloorId = 1,
                FloorNumber = 5,
                Description = "Test Floor",
                Status = ActiveStatus.Active
            };

            _floorRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>()
            )).ReturnsAsync(false);

            _mapper.Setup(m => m.Map<Floor>(dto)).Returns(floor);

            // Act
            var result = await _service.CreateFloorAsync(dto);

            // Assert
            Assert.Equal("Tạo tầng mới thành công", result);
            _floorRepo.Verify(r => r.InsertAsync(floor), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateFloorAsync_Throws_WhenFloorNumberExists()
        {
            // Arrange
            var dto = new FloorCreateDto { FloorNumber = 5 };

            _floorRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>()
            )).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateFloorAsync(dto));
            Assert.Equal("Lỗi hệ thống: Số tầng đã tồn tại.", ex.Message);
        }

        #endregion

        #region UpdateFloorAsync Tests

        [Fact]
        public async Task UpdateFloorAsync_Success_UpdatesFloor()
        {
            // Arrange
            var id = 1;
            var dto = new FloorUpdateDto
            {
                FloorNumber = 6,
                Description = "Updated Floor"
            };

            var floor = new Floor
            {
                FloorId = id,
                FloorNumber = 5,
                Description = "Old Floor"
            };

            _floorRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IOrderedQueryable<Floor>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>()
            )).ReturnsAsync(floor);

            _floorRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>()
            )).ReturnsAsync(false);

            _mapper.Setup(m => m.Map(dto, floor));

            // Act
            var result = await _service.UpdateFloorAsync(id, dto);

            // Assert
            Assert.Equal("Cập nhật tầng thành công", result);
            _floorRepo.Verify(r => r.UpdateAsync(floor), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateFloorAsync_Throws_WhenNotFound()
        {
            // Arrange
            var dto = new FloorUpdateDto { FloorNumber = 6 };

            _floorRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IOrderedQueryable<Floor>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>()
            )).ReturnsAsync((Floor?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.UpdateFloorAsync(999, dto));
            Assert.Equal("Lỗi hệ thống: Tầng không tồn tại.", ex.Message);
        }

        [Fact]
        public async Task UpdateFloorAsync_Throws_WhenFloorNumberExistsForOther()
        {
            // Arrange
            var id = 1;
            var dto = new FloorUpdateDto { FloorNumber = 6 };
            var floor = new Floor { FloorId = id, FloorNumber = 5 };

            _floorRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IOrderedQueryable<Floor>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>()
            )).ReturnsAsync(floor);

            _floorRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>()
            )).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.UpdateFloorAsync(id, dto));
            Assert.Equal("Lỗi hệ thống: Số tầng đã tồn tại.", ex.Message);
        }

        #endregion

        #region DeleteFloorAsync Tests

        [Fact]
        public async Task DeleteFloorAsync_Success_DeletesFloor()
        {
            // Arrange
            var id = 1;
            var floor = new Floor { FloorId = id, FloorNumber = 5 };

            _floorRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IOrderedQueryable<Floor>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>()
            )).ReturnsAsync(floor);

            // Act
            var result = await _service.DeleteFloorAsync(id);

            // Assert
            Assert.Equal("Xóa tầng thành công", result);
            _floorRepo.Verify(r => r.DeleteAsync(floor), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteFloorAsync_Throws_WhenNotFound()
        {
            // Arrange
            _floorRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IOrderedQueryable<Floor>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>()
            )).ReturnsAsync((Floor?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.DeleteFloorAsync(999));
            Assert.Equal("Lỗi hệ thống: Tầng không tồn tại.", ex.Message);
        }

        #endregion

        #region GetFloorByIdAsync Tests

        [Fact]
        public async Task GetFloorByIdAsync_Success_ReturnsFloor()
        {
            // Arrange
            var id = 1;
            var dto = new FloorDto
            {
                FloorId = id,
                FloorNumber = 5,
                Description = "Test Floor"
            };

            _floorRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Floor, FloorDto>>>(),
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IOrderedQueryable<Floor>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>()
            )).ReturnsAsync(dto);

            // Act
            var result = await _service.GetFloorByIdAsync(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result.FloorId);
            Assert.Equal(5, result.FloorNumber);
        }

        [Fact]
        public async Task GetFloorByIdAsync_Throws_WhenNotFound()
        {
            // Arrange
            _floorRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Floor, FloorDto>>>(),
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IOrderedQueryable<Floor>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>()
            )).ReturnsAsync((FloorDto?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.GetFloorByIdAsync(999));
            Assert.Equal("Tầng không tồn tại", ex.Message);
        }

        #endregion

        #region GetFloorsAsync Tests

        [Fact]
        public async Task GetFloorsAsync_Success_ReturnsActiveFloors()
        {
            // Arrange
            var floors = new List<FloorBasicDto>
            {
                new FloorBasicDto { FloorId = 1, FloorNumber = 1 },
                new FloorBasicDto { FloorId = 2, FloorNumber = 2 }
            };

            _floorRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Floor, FloorBasicDto>>>(),
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IOrderedQueryable<Floor>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>()
            )).ReturnsAsync(floors);

            // Act
            var result = await _service.GetFloorsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetFloorsAsync_Success_ReturnsEmptyWhenNoFloors()
        {
            // Arrange
            _floorRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Floor, FloorBasicDto>>>(),
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IOrderedQueryable<Floor>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>()
            )).ReturnsAsync(new List<FloorBasicDto>());

            // Act
            var result = await _service.GetFloorsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region GetPaginateFloorAsync Tests

        [Fact]
        public async Task GetPaginateFloorAsync_Success_ReturnsPaginatedResult()
        {
            // Arrange
            var dto = new PaginateDto
            {
                page = 1,
                size = 10,
                search = "",
                filter = ""
            };

            var pagedResult = new Paginate<GetAllFloorsDto>
            {
                Items = new List<GetAllFloorsDto>
                {
                    new GetAllFloorsDto { FloorId = 1, FloorNumber = 1 },
                    new GetAllFloorsDto { FloorId = 2, FloorNumber = 2 }
                },
                Page = 1,
                Size = 10,
                Total = 2,
                TotalPages = 1
            };

            _floorRepo.Setup(r => r.ProjectToPagingListAsync<GetAllFloorsDto>(
                It.IsAny<IConfigurationProvider>(),
                null,
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IOrderedQueryable<Floor>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>(),
                1,
                10
            )).ReturnsAsync(pagedResult);

            // Act
            var result = await _service.GetPaginateFloorAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(1, result.Page);
            Assert.Equal(2, result.Total);
        }

        [Fact]
        public async Task GetPaginateFloorAsync_Success_WithSearchFilter()
        {
            // Arrange
            var dto = new PaginateDto
            {
                page = 1,
                size = 10,
                search = "test",
                filter = "active"
            };

            var pagedResult = new Paginate<GetAllFloorsDto>
            {
                Items = new List<GetAllFloorsDto>(),
                Page = 1,
                Size = 10,
                Total = 0,
                TotalPages = 0
            };

            _floorRepo.Setup(r => r.ProjectToPagingListAsync<GetAllFloorsDto>(
                It.IsAny<IConfigurationProvider>(),
                null,
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IOrderedQueryable<Floor>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>(),
                1,
                10
            )).ReturnsAsync(pagedResult);

            // Act
            var result = await _service.GetPaginateFloorAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task GetPaginateFloorAsync_Success_DefaultPaging()
        {
            // Arrange
            var dto = new PaginateDto
            {
                page = 1,  // Should default to 1
                size = 10   // Should default to 10
            };

            var pagedResult = new Paginate<GetAllFloorsDto>
            {
                Items = new List<GetAllFloorsDto> { new GetAllFloorsDto {FloorId = 1 } },
                Page = 1,
                Size = 10,
                Total = 0,
                TotalPages = 0
            };

            _floorRepo.Setup(r => r.ProjectToPagingListAsync<GetAllFloorsDto>(
                It.IsAny<IConfigurationProvider>(),
                null,
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IOrderedQueryable<Floor>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>(),
                1,
                10
            )).ReturnsAsync(pagedResult);

            // Act
            var result = await _service.GetPaginateFloorAsync(dto);

            // Assert
            Assert.NotNull(result);
            _floorRepo.Verify(r => r.ProjectToPagingListAsync<GetAllFloorsDto>(
                It.IsAny<IConfigurationProvider>(),
                null,
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IOrderedQueryable<Floor>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>(),
                1,
                10
            ), Times.Once);
        }

        #endregion
    }
}