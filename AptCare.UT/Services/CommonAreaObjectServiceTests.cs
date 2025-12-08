using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.CommonAreaObjectDtos;
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
    public class CommonAreaObjectServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _uow = new();
        private readonly Mock<IGenericRepository<CommonAreaObject>> _objectRepo = new();
        private readonly Mock<IGenericRepository<CommonArea>> _areaRepo = new();
        private readonly Mock<IGenericRepository<CommonAreaObjectType>> _typeRepo = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<CommonAreaObjectService>> _logger = new();
        private readonly Mock<IRedisCacheService> _cacheService = new();

        private readonly CommonAreaObjectService _service;

        public CommonAreaObjectServiceTests()
        {
            _uow.Setup(u => u.GetRepository<CommonAreaObject>()).Returns(_objectRepo.Object);
            _uow.Setup(u => u.GetRepository<CommonArea>()).Returns(_areaRepo.Object);
            _uow.Setup(u => u.GetRepository<CommonAreaObjectType>()).Returns(_typeRepo.Object);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            _cacheService.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.GetAsync<CommonAreaObjectDto>(It.IsAny<string>()))
                .ReturnsAsync((CommonAreaObjectDto)null);
            _cacheService.Setup(c => c.GetAsync<IPaginate<CommonAreaObjectDto>>(It.IsAny<string>()))
                .ReturnsAsync((IPaginate<CommonAreaObjectDto>)null);
            _cacheService.Setup(c => c.GetAsync<IEnumerable<CommonAreaObjectBasicDto>>(It.IsAny<string>()))
                .ReturnsAsync((IEnumerable<CommonAreaObjectBasicDto>)null);

            _service = new CommonAreaObjectService(_uow.Object, _logger.Object, _mapper.Object, _cacheService.Object);
        }

        #region CreateCommonAreaObjectAsync Tests

        [Fact]
        public async Task CreateCommonAreaObjectAsync_Success()
        {
            // Arrange
            var dto = new CommonAreaObjectCreateDto
            {
                Name = "Pool Pump",
                CommonAreaId = 1,
                CommonAreaObjectTypeId = 1
            };

            var area = new CommonArea { CommonAreaId = 1, Status = ActiveStatus.Active };
            var type = new CommonAreaObjectType { CommonAreaObjectTypeId = 1, Status = ActiveStatus.Active };

            _areaRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonArea, bool>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IOrderedQueryable<CommonArea>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IIncludableQueryable<CommonArea, object>>>()
            )).ReturnsAsync(area);

            _typeRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObjectType, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IOrderedQueryable<CommonAreaObjectType>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IIncludableQueryable<CommonAreaObjectType, object>>>()
            )).ReturnsAsync(type);

            _objectRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<CommonAreaObject, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IIncludableQueryable<CommonAreaObject, object>>>()
            )).ReturnsAsync(false);

            var obj = new CommonAreaObject { CommonAreaObjectId = 1 };
            _mapper.Setup(m => m.Map<CommonAreaObject>(dto)).Returns(obj);

            // Act
            var result = await _service.CreateCommonAreaObjectAsync(dto);

            // Assert
            Assert.Equal("Tạo đối tượng khu vực chung mới thành công", result);
            _objectRepo.Verify(r => r.InsertAsync(obj), Times.Once);
        }

        [Fact]
        public async Task CreateCommonAreaObjectAsync_Throws_WhenAreaNotFound()
        {
            // Arrange
            var dto = new CommonAreaObjectCreateDto { CommonAreaId = 999 };

            _areaRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonArea, bool>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IOrderedQueryable<CommonArea>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IIncludableQueryable<CommonArea, object>>>()
            )).ReturnsAsync((CommonArea)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.CreateCommonAreaObjectAsync(dto));
            Assert.Contains("Khu vực chung không tồn tại", ex.Message);
        }

        [Fact]
        public async Task CreateCommonAreaObjectAsync_Throws_WhenAreaInactive()
        {
            // Arrange
            var dto = new CommonAreaObjectCreateDto { CommonAreaId = 1 };
            var area = new CommonArea { CommonAreaId = 1, Status = ActiveStatus.Inactive };

            _areaRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonArea, bool>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IOrderedQueryable<CommonArea>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IIncludableQueryable<CommonArea, object>>>()
            )).ReturnsAsync(area);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.CreateCommonAreaObjectAsync(dto));
            Assert.Contains("đã ngưng hoạt động", ex.Message);
        }

        [Fact]
        public async Task CreateCommonAreaObjectAsync_Throws_WhenNameDuplicate()
        {
            // Arrange
            var dto = new CommonAreaObjectCreateDto
            {
                Name = "Pump",
                CommonAreaId = 1,
                CommonAreaObjectTypeId = 1
            };

            var area = new CommonArea { CommonAreaId = 1, Status = ActiveStatus.Active };
            var type = new CommonAreaObjectType { CommonAreaObjectTypeId = 1, Status = ActiveStatus.Active };

            _areaRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonArea, bool>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IOrderedQueryable<CommonArea>>>(),
                It.IsAny<Func<IQueryable<CommonArea>, IIncludableQueryable<CommonArea, object>>>()
            )).ReturnsAsync(area);

            _typeRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObjectType, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IOrderedQueryable<CommonAreaObjectType>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IIncludableQueryable<CommonAreaObjectType, object>>>()
            )).ReturnsAsync(type);

            _objectRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<CommonAreaObject, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IIncludableQueryable<CommonAreaObject, object>>>()
            )).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.CreateCommonAreaObjectAsync(dto));
            Assert.Contains("Tên đối tượng đã tồn tại", ex.Message);
        }

        #endregion

        #region ActivateCommonAreaObjectAsync Tests

        [Fact]
        public async Task ActivateCommonAreaObjectAsync_Success()
        {
            // Arrange
            var id = 1;
            var obj = new CommonAreaObject
            {
                CommonAreaObjectId = id,
                Status = ActiveStatus.Inactive,
                CommonArea = new CommonArea { Status = ActiveStatus.Active }
            };

            _objectRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObject, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IOrderedQueryable<CommonAreaObject>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IIncludableQueryable<CommonAreaObject, object>>>()
            )).ReturnsAsync(obj);

            // Act
            var result = await _service.ActivateCommonAreaObjectAsync(id);

            // Assert
            Assert.Equal("Kích hoạt đối tượng khu vực chung thành công", result);
            Assert.Equal(ActiveStatus.Active, obj.Status);
            _objectRepo.Verify(r => r.UpdateAsync(obj), Times.Once);
        }

        [Fact]
        public async Task ActivateCommonAreaObjectAsync_Throws_WhenAlreadyActive()
        {
            // Arrange
            var id = 1;
            var obj = new CommonAreaObject
            {
                CommonAreaObjectId = id,
                Status = ActiveStatus.Active,
                CommonArea = new CommonArea { Status = ActiveStatus.Active }
            };

            _objectRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObject, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IOrderedQueryable<CommonAreaObject>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IIncludableQueryable<CommonAreaObject, object>>>()
            )).ReturnsAsync(obj);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.ActivateCommonAreaObjectAsync(id));
            Assert.Contains("đã ở trạng thái hoạt động", ex.Message);
        }

        [Fact]
        public async Task ActivateCommonAreaObjectAsync_Throws_WhenAreaInactive()
        {
            // Arrange
            var id = 1;
            var obj = new CommonAreaObject
            {
                CommonAreaObjectId = id,
                Status = ActiveStatus.Inactive,
                CommonArea = new CommonArea { Status = ActiveStatus.Inactive }
            };

            _objectRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObject, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IOrderedQueryable<CommonAreaObject>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IIncludableQueryable<CommonAreaObject, object>>>()
            )).ReturnsAsync(obj);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.ActivateCommonAreaObjectAsync(id));
            Assert.Contains("khu vực chung đã ngưng hoạt động", ex.Message);
        }

        #endregion

        #region DeactivateCommonAreaObjectAsync Tests

        [Fact]
        public async Task DeactivateCommonAreaObjectAsync_Success_WithoutSchedule()
        {
            // Arrange
            var id = 1;
            var obj = new CommonAreaObject
            {
                CommonAreaObjectId = id,
                Status = ActiveStatus.Active,
                MaintenanceSchedule = null
            };

            _objectRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObject, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IOrderedQueryable<CommonAreaObject>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IIncludableQueryable<CommonAreaObject, object>>>()
            )).ReturnsAsync(obj);

            // Act
            var result = await _service.DeactivateCommonAreaObjectAsync(id);

            // Assert
            Assert.Equal("Vô hiệu hóa đối tượng khu vực chung thành công", result);
            Assert.Equal(ActiveStatus.Inactive, obj.Status);
        }

        [Fact]
        public async Task DeactivateCommonAreaObjectAsync_Success_WithActiveSchedule()
        {
            // Arrange
            var id = 1;
            var obj = new CommonAreaObject
            {
                CommonAreaObjectId = id,
                Status = ActiveStatus.Active,
                MaintenanceSchedule = new MaintenanceSchedule
                {
                    Status = ActiveStatus.Active
                }
            };

            _objectRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObject, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IOrderedQueryable<CommonAreaObject>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IIncludableQueryable<CommonAreaObject, object>>>()
            )).ReturnsAsync(obj);

            // Act
            var result = await _service.DeactivateCommonAreaObjectAsync(id);

            // Assert
            Assert.Equal(ActiveStatus.Inactive, obj.Status);
            Assert.Equal(ActiveStatus.Inactive, obj.MaintenanceSchedule.Status);
        }

        #endregion

        #region GetCommonAreaObjectsByCommonAreaAsync Tests

        [Fact]
        public async Task GetCommonAreaObjectsByCommonAreaAsync_Success()
        {
            // Arrange
            var areaId = 1;
            var objects = new List<CommonAreaObjectBasicDto>
            {
                new CommonAreaObjectBasicDto { CommonAreaObjectId = 1, Name = "Pump" }
            };

            _objectRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<CommonAreaObject, CommonAreaObjectBasicDto>>>(),
                It.IsAny<Expression<Func<CommonAreaObject, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IOrderedQueryable<CommonAreaObject>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IIncludableQueryable<CommonAreaObject, object>>>()
            )).ReturnsAsync(objects);

            // Act
            var result = await _service.GetCommonAreaObjectsByCommonAreaAsync(areaId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        #endregion

        #region GetCommonAreaObjectsByTypeAsync Tests

        [Fact]
        public async Task GetCommonAreaObjectsByTypeAsync_Success()
        {
            // Arrange
            var typeId = 1;

            _typeRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<CommonAreaObjectType, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IIncludableQueryable<CommonAreaObjectType, object>>>()
            )).ReturnsAsync(true);

            var objects = new List<CommonAreaObjectBasicDto>
            {
                new CommonAreaObjectBasicDto { CommonAreaObjectId = 1 }
            };

            _objectRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<CommonAreaObject, CommonAreaObjectBasicDto>>>(),
                It.IsAny<Expression<Func<CommonAreaObject, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IOrderedQueryable<CommonAreaObject>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IIncludableQueryable<CommonAreaObject, object>>>()
            )).ReturnsAsync(objects);

            // Act
            var result = await _service.GetCommonAreaObjectsByTypeAsync(typeId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetCommonAreaObjectsByTypeAsync_Throws_WhenTypeNotFound()
        {
            // Arrange
            _typeRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<CommonAreaObjectType, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IIncludableQueryable<CommonAreaObjectType, object>>>()
            )).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.GetCommonAreaObjectsByTypeAsync(999));
            Assert.Contains("Loại đối tượng khu vực chung không tồn tại", ex.Message);
        }

        #endregion
    }
}