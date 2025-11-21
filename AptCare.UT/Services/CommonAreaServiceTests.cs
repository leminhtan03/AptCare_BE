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
    public class CommonAreaServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _uow = new();
        private readonly Mock<IGenericRepository<CommonArea>> _commonAreaRepo = new();
        private readonly Mock<IGenericRepository<Floor>> _floorRepo = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<CommonAreaService>> _logger = new();
        private readonly Mock<IRedisCacheService> _cacheService = new();

        private readonly CommonAreaService _service;

        public CommonAreaServiceTests()
        {
            _uow.Setup(u => u.GetRepository<CommonArea>()).Returns(_commonAreaRepo.Object);
            _uow.Setup(u => u.GetRepository<Floor>()).Returns(_floorRepo.Object);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            _cacheService.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.GetAsync<CommonAreaDto>(It.IsAny<string>()))
                .ReturnsAsync((CommonAreaDto)null);

            _cacheService.Setup(c => c.GetAsync<IEnumerable<CommonAreaDto>>(It.IsAny<string>()))
                .ReturnsAsync((IEnumerable<CommonAreaDto>)null);

            _cacheService.Setup(c => c.GetAsync<IPaginate<CommonAreaDto>>(It.IsAny<string>()))
                .ReturnsAsync((IPaginate<CommonAreaDto>)null);

            _service = new CommonAreaService(_uow.Object, _logger.Object, _mapper.Object, _cacheService.Object);
        }

        #region CreateCommonAreaAsync Tests

        [Fact]
        public async Task CreateCommonAreaAsync_Success_WithFloor()
        {
            // Arrange
            var dto = new CommonAreaCreateDto
            {
                FloorId = 1,
                AreaCode = "LOBBY-01",
                Name = "Main Lobby",
                Location = "Ground Floor"
            };

            var floor = new Floor { FloorId = 1, Status = ActiveStatus.Active };
            var commonArea = new CommonArea { CommonAreaId = 1, AreaCode = "LOBBY-01" };

            _floorRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IOrderedQueryable<Floor>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>()
            )).ReturnsAsync(floor);

            _commonAreaRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<CommonArea, bool>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IIncludableQueryable<CommonArea, object>>>()
            )).ReturnsAsync(false);

            _mapper.Setup(m => m.Map<CommonArea>(dto)).Returns(commonArea);

            // Act
            var result = await _service.CreateCommonAreaAsync(dto);

            // Assert
            Assert.Equal("Tạo khu vực chung mới thành công", result);
            _commonAreaRepo.Verify(r => r.InsertAsync(commonArea), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateCommonAreaAsync_Success_WithoutFloor()
        {
            // Arrange
            var dto = new CommonAreaCreateDto
            {
                FloorId = null,
                AreaCode = "PARKING-B1",
                Name = "Parking Basement"
            };

            var commonArea = new CommonArea { CommonAreaId = 1, AreaCode = "PARKING-B1" };

            _commonAreaRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<CommonArea, bool>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IIncludableQueryable<CommonArea, object>>>()
            )).ReturnsAsync(false);

            _mapper.Setup(m => m.Map<CommonArea>(dto)).Returns(commonArea);

            // Act
            var result = await _service.CreateCommonAreaAsync(dto);

            // Assert
            Assert.Equal("Tạo khu vực chung mới thành công", result);
            _floorRepo.Verify(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IOrderedQueryable<Floor>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>()
            ), Times.Never);
        }

        [Fact]
        public async Task CreateCommonAreaAsync_Throws_WhenFloorNotExists()
        {
            // Arrange
            var dto = new CommonAreaCreateDto { FloorId = 999, AreaCode = "AREA-01" };

            _floorRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IOrderedQueryable<Floor>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>()
            )).ReturnsAsync((Floor?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateCommonAreaAsync(dto));
            Assert.Equal("Lỗi hệ thống: Tầng không tồn tại.", ex.Message);
        }

        [Fact]
        public async Task CreateCommonAreaAsync_Throws_WhenFloorInactive()
        {
            // Arrange
            var dto = new CommonAreaCreateDto { FloorId = 1, AreaCode = "AREA-01" };
            var floor = new Floor { FloorId = 1, Status = ActiveStatus.Inactive };

            _floorRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IOrderedQueryable<Floor>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>()
            )).ReturnsAsync(floor);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateCommonAreaAsync(dto));
            Assert.Equal("Lỗi hệ thống: Tầng đã ngưng hoạt động.", ex.Message);
        }

        [Fact]
        public async Task CreateCommonAreaAsync_Throws_WhenAreaCodeExists()
        {
            // Arrange
            var dto = new CommonAreaCreateDto { AreaCode = "LOBBY-01" };

            _commonAreaRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<CommonArea, bool>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IIncludableQueryable<CommonArea, object>>>()
            )).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateCommonAreaAsync(dto));
            Assert.Equal("Lỗi hệ thống: Mã khu vực chung đã tồn tại.", ex.Message);
        }

        #endregion

        #region UpdateCommonAreaAsync Tests

        [Fact]
        public async Task UpdateCommonAreaAsync_Success_UpdatesCommonArea()
        {
            // Arrange
            var id = 1;
            var dto = new CommonAreaUpdateDto
            {
                FloorId = 1,
                AreaCode = "LOBBY-02",
                Name = "Updated Lobby"
            };

            var commonArea = new CommonArea { CommonAreaId = id, AreaCode = "LOBBY-01" };
            var floor = new Floor { FloorId = 1, Status = ActiveStatus.Active };

            _commonAreaRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonArea, bool>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IOrderedQueryable<CommonArea>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IIncludableQueryable<CommonArea, object>>>()
            )).ReturnsAsync(commonArea);

            _floorRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IOrderedQueryable<Floor>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>()
            )).ReturnsAsync(floor);

            _commonAreaRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<CommonArea, bool>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IIncludableQueryable<CommonArea, object>>>()
            )).ReturnsAsync(false);

            _mapper.Setup(m => m.Map(dto, commonArea));

            // Act
            var result = await _service.UpdateCommonAreaAsync(id, dto);

            // Assert
            Assert.Equal("Cập nhật khu vực chung thành công", result);
            _commonAreaRepo.Verify(r => r.UpdateAsync(commonArea), Times.Once);
        }

        [Fact]
        public async Task UpdateCommonAreaAsync_Throws_WhenNotFound()
        {
            // Arrange
            var dto = new CommonAreaUpdateDto { AreaCode = "AREA-01" };

            _commonAreaRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonArea, bool>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IOrderedQueryable<CommonArea>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IIncludableQueryable<CommonArea, object>>>()
            )).ReturnsAsync((CommonArea?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.UpdateCommonAreaAsync(999, dto));
            Assert.Equal("Lỗi hệ thống: Khu vực chung không tồn tại.", ex.Message);
        }

        #endregion

        #region DeleteCommonAreaAsync Tests

        [Fact]
        public async Task DeleteCommonAreaAsync_Success_DeletesCommonArea()
        {
            // Arrange
            var id = 1;
            var commonArea = new CommonArea { CommonAreaId = id, AreaCode = "LOBBY-01" };

            _commonAreaRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonArea, bool>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IOrderedQueryable<CommonArea>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IIncludableQueryable<CommonArea, object>>>()
            )).ReturnsAsync(commonArea);

            // Act
            var result = await _service.DeleteCommonAreaAsync(id);

            // Assert
            Assert.Equal("Xóa khu vực chung thành công", result);
            _commonAreaRepo.Verify(r => r.DeleteAsync(commonArea), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteCommonAreaAsync_Throws_WhenNotFound()
        {
            // Arrange
            _commonAreaRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonArea, bool>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IOrderedQueryable<CommonArea>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IIncludableQueryable<CommonArea, object>>>()
            )).ReturnsAsync((CommonArea?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.DeleteCommonAreaAsync(999));
            Assert.Equal("Lỗi hệ thống: Khu vực chung không tồn tại.", ex.Message);
        }

        #endregion

        #region GetCommonAreaByIdAsync Tests

        [Fact]
        public async Task GetCommonAreaByIdAsync_Success_ReturnsCommonArea()
        {
            // Arrange
            var id = 1;
            var dto = new CommonAreaDto
            {
                CommonAreaId = id,
                AreaCode = "LOBBY-01",
                Name = "Main Lobby"
            };

            _commonAreaRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonArea, CommonAreaDto>>>(),
                It.IsAny<Expression<Func<CommonArea, bool>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IOrderedQueryable<CommonArea>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IIncludableQueryable<CommonArea, object>>>()
            )).ReturnsAsync(dto);

            // Act
            var result = await _service.GetCommonAreaByIdAsync(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result.CommonAreaId);
        }

        [Fact]
        public async Task GetCommonAreaByIdAsync_Throws_WhenNotFound()
        {
            // Arrange
            _commonAreaRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonArea, CommonAreaDto>>>(),
                It.IsAny<Expression<Func<CommonArea, bool>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IOrderedQueryable<CommonArea>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IIncludableQueryable<CommonArea, object>>>()
            )).ReturnsAsync((CommonAreaDto?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.GetCommonAreaByIdAsync(999));
            Assert.Equal("Khu vực chung không tồn tại.", ex.Message);
        }

        #endregion

        #region GetCommonAreasAsync Tests

        [Fact]
        public async Task GetCommonAreasAsync_Success_ReturnsActiveAreas()
        {
            // Arrange
            var areas = new List<CommonAreaDto>
            {
                new CommonAreaDto { CommonAreaId = 1, AreaCode = "LOBBY-01" },
                new CommonAreaDto { CommonAreaId = 2, AreaCode = "GYM-01" }
            };

            _commonAreaRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<CommonArea, CommonAreaDto>>>(),
                It.IsAny<Expression<Func<CommonArea, bool>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IOrderedQueryable<CommonArea>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IIncludableQueryable<CommonArea, object>>>()
            )).ReturnsAsync(areas);

            // Act
            var result = await _service.GetCommonAreasAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        #endregion

        #region GetPaginateCommonAreaAsync Tests

        [Fact]
        public async Task GetPaginateCommonAreaAsync_Success_ReturnsPaginatedResult()
        {
            // Arrange
            var dto = new PaginateDto { page = 1, size = 10 };
            var pagedResult = new Paginate<CommonAreaDto>
            {
                Items = new List<CommonAreaDto>
                {
                    new CommonAreaDto { CommonAreaId = 1, AreaCode = "LOBBY-01" }
                },
                Page = 1,
                Size = 10,
                Total = 1,
                TotalPages = 1
            };

            _commonAreaRepo.Setup(r => r.GetPagingListAsync(
                It.IsAny<Expression<Func<CommonArea, CommonAreaDto>>>(),
                It.IsAny<Expression<Func<CommonArea, bool>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IOrderedQueryable<CommonArea>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IIncludableQueryable<CommonArea, object>>>(),
                1,
                10
            )).ReturnsAsync(pagedResult);

            // Act
            var result = await _service.GetPaginateCommonAreaAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
        }

        #endregion
    }
}