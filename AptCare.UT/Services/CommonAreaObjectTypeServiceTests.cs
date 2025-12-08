using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.CommonAreaObjectTypeDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Implements;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace AptCare.UT.Services
{
    public class CommonAreaObjectTypeServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _uow = new();
        private readonly Mock<IGenericRepository<CommonAreaObjectType>> _typeRepo = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<CommonAreaObjectTypeService>> _logger = new();
        private readonly Mock<IRedisCacheService> _cacheService = new();

        private readonly CommonAreaObjectTypeService _service;

        public CommonAreaObjectTypeServiceTests()
        {
            _uow.Setup(u => u.GetRepository<CommonAreaObjectType>()).Returns(_typeRepo.Object);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            _cacheService.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.GetAsync<CommonAreaObjectTypeDto>(It.IsAny<string>()))
                .ReturnsAsync((CommonAreaObjectTypeDto)null);
            _cacheService.Setup(c => c.GetAsync<Paginate<CommonAreaObjectTypeDto>>(It.IsAny<string>()))
                .ReturnsAsync((Paginate<CommonAreaObjectTypeDto>)null);
            _cacheService.Setup(c => c.GetAsync<IEnumerable<CommonAreaObjectTypeDto>>(It.IsAny<string>()))
                .ReturnsAsync((IEnumerable<CommonAreaObjectTypeDto>)null);

            _service = new CommonAreaObjectTypeService(_uow.Object, _logger.Object, _mapper.Object, _cacheService.Object);
        }

        #region CreateCommonAreaObjectTypeAsync Tests

        [Fact]
        public async Task CreateCommonAreaObjectTypeAsync_Success()
        {
            // Arrange
            var dto = new CommonAreaObjectTypeCreateDto
            {
                TypeName = "Pump",
                Description = "Water pumps"
            };

            _typeRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<CommonAreaObjectType, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IIncludableQueryable<CommonAreaObjectType, object>>>()
            )).ReturnsAsync(false);

            var type = new CommonAreaObjectType { CommonAreaObjectTypeId = 1, TypeName = dto.TypeName };
            _mapper.Setup(m => m.Map<CommonAreaObjectType>(dto)).Returns(type);

            // Act
            var result = await _service.CreateCommonAreaObjectTypeAsync(dto);

            // Assert
            Assert.Equal("Tạo loại đối tượng khu vực chung mới thành công", result);
            Assert.Equal(ActiveStatus.Active, type.Status);
            _typeRepo.Verify(r => r.InsertAsync(type), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateCommonAreaObjectTypeAsync_Throws_WhenDuplicate()
        {
            // Arrange
            var dto = new CommonAreaObjectTypeCreateDto { TypeName = "Pump" };

            _typeRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<CommonAreaObjectType, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IIncludableQueryable<CommonAreaObjectType, object>>>()
            )).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.CreateCommonAreaObjectTypeAsync(dto));
            Assert.Contains("Tên loại đối tượng đã tồn tại", ex.Message);
        }

        #endregion

        #region UpdateCommonAreaObjectTypeAsync Tests

        [Fact]
        public async Task UpdateCommonAreaObjectTypeAsync_Success()
        {
            // Arrange
            var id = 1;
            var dto = new CommonAreaObjectTypeUpdateDto
            {
                TypeName = "Updated Pump",
                Description = "Updated"
            };

            var type = new CommonAreaObjectType
            {
                CommonAreaObjectTypeId = id,
                TypeName = "Pump"
            };

            _typeRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObjectType, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IOrderedQueryable<CommonAreaObjectType>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IIncludableQueryable<CommonAreaObjectType, object>>>()
            )).ReturnsAsync(type);

            _typeRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<CommonAreaObjectType, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IIncludableQueryable<CommonAreaObjectType, object>>>()
            )).ReturnsAsync(false);

            _mapper.Setup(m => m.Map(dto, type));

            // Act
            var result = await _service.UpdateCommonAreaObjectTypeAsync(id, dto);

            // Assert
            Assert.Equal("Cập nhật loại đối tượng khu vực chung thành công", result);
            _typeRepo.Verify(r => r.UpdateAsync(type), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateCommonAreaObjectTypeAsync_Throws_WhenNotFound()
        {
            // Arrange
            var dto = new CommonAreaObjectTypeUpdateDto { TypeName = "Test" };

            _typeRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObjectType, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IOrderedQueryable<CommonAreaObjectType>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IIncludableQueryable<CommonAreaObjectType, object>>>()
            )).ReturnsAsync((CommonAreaObjectType)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.UpdateCommonAreaObjectTypeAsync(999, dto));
            Assert.Contains("Loại đối tượng khu vực chung không tồn tại", ex.Message);
        }

        #endregion

        #region DeleteCommonAreaObjectTypeAsync Tests

        [Fact]
        public async Task DeleteCommonAreaObjectTypeAsync_Success()
        {
            // Arrange
            var id = 1;
            var type = new CommonAreaObjectType
            {
                CommonAreaObjectTypeId = id,
                CommonAreaObjects = new List<CommonAreaObject>(),
                MaintenanceTasks = new List<MaintenanceTask>()
            };

            _typeRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObjectType, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IOrderedQueryable<CommonAreaObjectType>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IIncludableQueryable<CommonAreaObjectType, object>>>()
            )).ReturnsAsync(type);

            // Act
            var result = await _service.DeleteCommonAreaObjectTypeAsync(id);

            // Assert
            Assert.Equal("Xóa loại đối tượng khu vực chung thành công", result);
            _typeRepo.Verify(r => r.DeleteAsync(type), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteCommonAreaObjectTypeAsync_Throws_WhenHasObjects()
        {
            // Arrange
            var id = 1;
            var type = new CommonAreaObjectType
            {
                CommonAreaObjectTypeId = id,
                CommonAreaObjects = new List<CommonAreaObject>
                {
                    new CommonAreaObject { CommonAreaObjectId = 1 }
                },
                MaintenanceTasks = new List<MaintenanceTask>()
            };

            _typeRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObjectType, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IOrderedQueryable<CommonAreaObjectType>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IIncludableQueryable<CommonAreaObjectType, object>>>()
            )).ReturnsAsync(type);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.DeleteCommonAreaObjectTypeAsync(id));
            Assert.Contains("đang có đối tượng liên kết", ex.Message);
        }

        [Fact]
        public async Task DeleteCommonAreaObjectTypeAsync_Throws_WhenHasMaintenanceTasks()
        {
            // Arrange
            var id = 1;
            var type = new CommonAreaObjectType
            {
                CommonAreaObjectTypeId = id,
                CommonAreaObjects = new List<CommonAreaObject>(),
                MaintenanceTasks = new List<MaintenanceTask>
                {
                    new MaintenanceTask { MaintenanceTaskId = 1 }
                }
            };

            _typeRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObjectType, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IOrderedQueryable<CommonAreaObjectType>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IIncludableQueryable<CommonAreaObjectType, object>>>()
            )).ReturnsAsync(type);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.DeleteCommonAreaObjectTypeAsync(id));
            Assert.Contains("nhiệm vụ bảo trì liên kết", ex.Message);
        }

        #endregion

        #region ActivateCommonAreaObjectTypeAsync Tests

        [Fact]
        public async Task ActivateCommonAreaObjectTypeAsync_Success()
        {
            // Arrange
            var id = 1;
            var type = new CommonAreaObjectType
            {
                CommonAreaObjectTypeId = id,
                Status = ActiveStatus.Inactive
            };

            _typeRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObjectType, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IOrderedQueryable<CommonAreaObjectType>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IIncludableQueryable<CommonAreaObjectType, object>>>()
            )).ReturnsAsync(type);

            // Act
            var result = await _service.ActivateCommonAreaObjectTypeAsync(id);

            // Assert
            Assert.Equal("Kích hoạt loại đối tượng khu vực chung thành công", result);
            Assert.Equal(ActiveStatus.Active, type.Status);
            _typeRepo.Verify(r => r.UpdateAsync(type), Times.Once);
        }

        [Fact]
        public async Task ActivateCommonAreaObjectTypeAsync_Throws_WhenAlreadyActive()
        {
            // Arrange
            var id = 1;
            var type = new CommonAreaObjectType
            {
                CommonAreaObjectTypeId = id,
                Status = ActiveStatus.Active
            };

            _typeRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObjectType, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IOrderedQueryable<CommonAreaObjectType>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IIncludableQueryable<CommonAreaObjectType, object>>>()
            )).ReturnsAsync(type);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.ActivateCommonAreaObjectTypeAsync(id));
            Assert.Contains("đã ở trạng thái hoạt động", ex.Message);
        }

        #endregion

        #region DeactivateCommonAreaObjectTypeAsync Tests

        [Fact]
        public async Task DeactivateCommonAreaObjectTypeAsync_Success()
        {
            // Arrange
            var id = 1;
            var type = new CommonAreaObjectType
            {
                CommonAreaObjectTypeId = id,
                Status = ActiveStatus.Active
            };

            _typeRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObjectType, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IOrderedQueryable<CommonAreaObjectType>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IIncludableQueryable<CommonAreaObjectType, object>>>()
            )).ReturnsAsync(type);

            // Act
            var result = await _service.DeactivateCommonAreaObjectTypeAsync(id);

            // Assert
            Assert.Equal("Vô hiệu hóa loại đối tượng khu vực chung thành công", result);
            Assert.Equal(ActiveStatus.Inactive, type.Status);
            _typeRepo.Verify(r => r.UpdateAsync(type), Times.Once);
        }

        #endregion

        #region GetCommonAreaObjectTypeByIdAsync Tests

        [Fact]
        public async Task GetCommonAreaObjectTypeByIdAsync_Success()
        {
            // Arrange
            var id = 1;
            var dto = new CommonAreaObjectTypeDto { CommonAreaObjectTypeId = id, TypeName = "Pump" };

            _typeRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObjectType, CommonAreaObjectTypeDto>>>(),
                It.IsAny<Expression<Func<CommonAreaObjectType, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IOrderedQueryable<CommonAreaObjectType>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IIncludableQueryable<CommonAreaObjectType, object>>>()
            )).ReturnsAsync(dto);

            // Act
            var result = await _service.GetCommonAreaObjectTypeByIdAsync(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result.CommonAreaObjectTypeId);
        }

        [Fact]
        public async Task GetCommonAreaObjectTypeByIdAsync_Throws_WhenNotFound()
        {
            // Arrange
            _typeRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObjectType, CommonAreaObjectTypeDto>>>(),
                It.IsAny<Expression<Func<CommonAreaObjectType, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IOrderedQueryable<CommonAreaObjectType>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IIncludableQueryable<CommonAreaObjectType, object>>>()
            )).ReturnsAsync((CommonAreaObjectTypeDto)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.GetCommonAreaObjectTypeByIdAsync(999));
            Assert.Contains("Loại đối tượng khu vực chung không tồn tại", ex.Message);
        }

        #endregion

        #region GetCommonAreaObjectTypesAsync Tests

        [Fact]
        public async Task GetCommonAreaObjectTypesAsync_Success()
        {
            // Arrange
            var types = new List<CommonAreaObjectTypeDto>
            {
                new CommonAreaObjectTypeDto { CommonAreaObjectTypeId = 1, TypeName = "Pump" },
                new CommonAreaObjectTypeDto { CommonAreaObjectTypeId = 2, TypeName = "Filter" }
            };

            _typeRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<CommonAreaObjectType, CommonAreaObjectTypeDto>>>(),
                It.IsAny<Expression<Func<CommonAreaObjectType, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IOrderedQueryable<CommonAreaObjectType>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IIncludableQueryable<CommonAreaObjectType, object>>>()
            )).ReturnsAsync(types);

            // Act
            var result = await _service.GetCommonAreaObjectTypesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        #endregion

        #region GetPaginateCommonAreaObjectTypeAsync Tests

        [Fact]
        public async Task GetPaginateCommonAreaObjectTypeAsync_Success()
        {
            // Arrange
            var dto = new PaginateDto { page = 1, size = 10 };
            var types = new Paginate<CommonAreaObjectTypeDto>
            {
                Items = new List<CommonAreaObjectTypeDto>
                {
                    new CommonAreaObjectTypeDto { CommonAreaObjectTypeId = 1 }
                },
                Page = 1,
                Size = 10,
                Total = 1
            };

            _typeRepo.Setup(r => r.GetPagingListAsync(
                It.IsAny<Expression<Func<CommonAreaObjectType, CommonAreaObjectTypeDto>>>(),
                It.IsAny<Expression<Func<CommonAreaObjectType, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IOrderedQueryable<CommonAreaObjectType>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IIncludableQueryable<CommonAreaObjectType, object>>>(),
                1,
                10
            )).ReturnsAsync(types);

            // Act
            var result = await _service.GetPaginateCommonAreaObjectTypeAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
        }

        #endregion
    }
}