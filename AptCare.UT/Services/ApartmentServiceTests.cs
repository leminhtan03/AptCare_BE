using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.Apartment;
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
using Xunit;

namespace AptCare.UT.Services
{
    public class ApartmentServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _uow = new();
        private readonly Mock<IGenericRepository<Apartment>> _aptRepo = new();
        private readonly Mock<IGenericRepository<Floor>> _floorRepo = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<ApartmentService>> _logger = new();
        private readonly Mock<IRedisCacheService> _cacheService = new(); // ✅ Thêm Redis cache mock

        private readonly ApartmentService _service;

        public ApartmentServiceTests()
        {
            _uow.Setup(u => u.GetRepository<Apartment>()).Returns(_aptRepo.Object);
            _uow.Setup(u => u.GetRepository<Floor>()).Returns(_floorRepo.Object);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            // ✅ Setup cache service mocks
            _cacheService.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.GetAsync<ApartmentDto>(It.IsAny<string>()))
                    .ReturnsAsync((ApartmentDto)null);

            _cacheService.Setup(c => c.GetAsync<IEnumerable<ApartmentBasicDto>>(It.IsAny<string>()))
                .ReturnsAsync((IEnumerable<ApartmentBasicDto>)null);

            _cacheService.Setup(c => c.GetAsync<Paginate<ApartmentDto>>(It.IsAny<string>()))
                .ReturnsAsync((Paginate<ApartmentDto>)null);

            // ✅ Inject IRedisCacheService vào constructor
            _service = new ApartmentService(_uow.Object, _logger.Object, _mapper.Object, _cacheService.Object);
        }

        #region CreateApartmentAsync Tests

        [Fact]
        public async Task CreateApartmentAsync_Success_CreatesApartment()
        {
            // Arrange
            var dto = new ApartmentCreateDto
            {
                Room = "A101",
                FloorId = 1,
                Area = 70,
                Limit = 4,
                Description = "Test Apartment"
            };

            var apartment = new Apartment
            {
                ApartmentId = 1,
                Room = "A101",
                FloorId = 1,
                Status = ApartmentStatus.Active
            };

            var floor = new Floor { FloorId = 1, Status = ActiveStatus.Active };

            _floorRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IOrderedQueryable<Floor>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>()
            )).ReturnsAsync(floor);

            _aptRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Apartment, bool>>>(),
                It.IsAny<Func<IQueryable<Apartment>, IIncludableQueryable<Apartment, object>>>()
            )).ReturnsAsync(false);

            _mapper.Setup(m => m.Map<Apartment>(dto)).Returns(apartment);

            // Act
            var result = await _service.CreateApartmentAsync(dto);

            // Assert
            Assert.Equal("Tạo căn hộ mới thành công", result);
            _aptRepo.Verify(r => r.InsertAsync(apartment), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
            _cacheService.Verify(c => c.RemoveByPrefixAsync("apartment"), Times.Once); // ✅ Verify cache clear
        }

        [Fact]
        public async Task CreateApartmentAsync_Throws_WhenFloorNotExists()
        {
            // Arrange
            var dto = new ApartmentCreateDto { FloorId = 999, Room = "A101" };

            _floorRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IOrderedQueryable<Floor>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>()
            )).ReturnsAsync((Floor?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateApartmentAsync(dto));
            Assert.Equal("Tầng không tồn tại.", ex.Message);
        }

        [Fact]
        public async Task CreateApartmentAsync_Throws_WhenFloorInactive()
        {
            // Arrange
            var dto = new ApartmentCreateDto { FloorId = 1, Room = "A101" };
            var floor = new Floor { FloorId = 1, Status = ActiveStatus.Inactive };

            _floorRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IOrderedQueryable<Floor>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>()
            )).ReturnsAsync(floor);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateApartmentAsync(dto));
            Assert.Equal("Tầng đã ngưng hoạt động.", ex.Message);
        }

        [Fact]
        public async Task CreateApartmentAsync_Throws_WhenRoomExists()
        {
            // Arrange
            var dto = new ApartmentCreateDto { FloorId = 1, Room = "A101" };
            var floor = new Floor { FloorId = 1, Status = ActiveStatus.Active };

            _floorRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IOrderedQueryable<Floor>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>()
            )).ReturnsAsync(floor);

            _aptRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Apartment, bool>>>(),
                It.IsAny<Func<IQueryable<Apartment>, IIncludableQueryable<Apartment, object>>>()
            )).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateApartmentAsync(dto));
            Assert.Equal("Số phòng đã tồn tại.", ex.Message);
        }

        #endregion

        #region UpdateApartmentAsync Tests

        [Fact]
        public async Task UpdateApartmentAsync_Success_UpdatesApartment()
        {
            // Arrange
            var id = 1;
            var dto = new ApartmentUpdateDto
            {
                Room = "A102",
                FloorId = 1,
                Area = 80,
                Limit = 5
            };

            var apartment = new Apartment
            {
                ApartmentId = id,
                Room = "A101",
                FloorId = 1
            };

            var floor = new Floor { FloorId = 1, Status = ActiveStatus.Active };

            _aptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Apartment, bool>>>(),
                It.IsAny<Func<IQueryable<Apartment>, IOrderedQueryable<Apartment>>>(),
                It.IsAny<Func<IQueryable<Apartment>, IIncludableQueryable<Apartment, object>>>()
            )).ReturnsAsync(apartment);

            _floorRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IOrderedQueryable<Floor>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>()
            )).ReturnsAsync(floor);

            _aptRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Apartment, bool>>>(),
                It.IsAny<Func<IQueryable<Apartment>, IIncludableQueryable<Apartment, object>>>()
            )).ReturnsAsync(false);

            _mapper.Setup(m => m.Map(dto, apartment));

            // Act
            var result = await _service.UpdateApartmentAsync(id, dto);

            // Assert
            Assert.Equal("Cập nhật căn hộ thành công", result);
            _aptRepo.Verify(r => r.UpdateAsync(apartment), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateApartmentAsync_Throws_WhenNotFound()
        {
            // Arrange
            var dto = new ApartmentUpdateDto { Room = "A101" };

            _aptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Apartment, bool>>>(),
                It.IsAny<Func<IQueryable<Apartment>, IOrderedQueryable<Apartment>>>(),
                It.IsAny<Func<IQueryable<Apartment>, IIncludableQueryable<Apartment, object>>>()
            )).ReturnsAsync((Apartment?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.UpdateApartmentAsync(999, dto));
            Assert.Equal("Căn hộ không tồn tại.", ex.Message);
        }

        [Fact]
        public async Task UpdateApartmentAsync_Throws_WhenRoomExistsForOther()
        {
            // Arrange
            var id = 1;
            var dto = new ApartmentUpdateDto { Room = "A102", FloorId = 1 };
            var apartment = new Apartment { ApartmentId = id, Room = "A101", FloorId = 1 };
            var floor = new Floor { FloorId = 1, Status = ActiveStatus.Active };

            _aptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Apartment, bool>>>(),
                It.IsAny<Func<IQueryable<Apartment>, IOrderedQueryable<Apartment>>>(),
                It.IsAny<Func<IQueryable<Apartment>, IIncludableQueryable<Apartment, object>>>()
            )).ReturnsAsync(apartment);

            _floorRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IOrderedQueryable<Floor>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>()
            )).ReturnsAsync(floor);

            _aptRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Apartment, bool>>>(),
                It.IsAny<Func<IQueryable<Apartment>, IIncludableQueryable<Apartment, object>>>()
            )).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.UpdateApartmentAsync(id, dto));
            Assert.Equal("Số phòng đã tồn tại.", ex.Message);
        }

        #endregion

        #region DeleteApartmentAsync Tests

        [Fact]
        public async Task DeleteApartmentAsync_Success_DeletesApartment()
        {
            // Arrange
            var id = 1;
            var apartment = new Apartment { ApartmentId = id, Room = "A101" };

            _aptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Apartment, bool>>>(),
                It.IsAny<Func<IQueryable<Apartment>, IOrderedQueryable<Apartment>>>(),
                It.IsAny<Func<IQueryable<Apartment>, IIncludableQueryable<Apartment, object>>>()
            )).ReturnsAsync(apartment);

            // Act
            var result = await _service.DeleteApartmentAsync(id);

            // Assert
            Assert.Equal("Xóa căn hộ thành công", result);
            _aptRepo.Verify(r => r.DeleteAsync(apartment), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
            _cacheService.Verify(c => c.RemoveByPrefixAsync("apartment"), Times.Once); // ✅ Verify cache clear
        }

        [Fact]
        public async Task DeleteApartmentAsync_Throws_WhenNotFound()
        {
            // Arrange
            _aptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Apartment, bool>>>(),
                It.IsAny<Func<IQueryable<Apartment>, IOrderedQueryable<Apartment>>>(),
                It.IsAny<Func<IQueryable<Apartment>, IIncludableQueryable<Apartment, object>>>()
            )).ReturnsAsync((Apartment?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.DeleteApartmentAsync(999));
            Assert.Equal("Căn hộ không tồn tại.", ex.Message);
        }

        #endregion

        #region GetApartmentByIdAsync Tests

        [Fact]
        public async Task GetApartmentByIdAsync_Success_ReturnsApartment()
        {
            // Arrange
            var id = 1;
            var dto = new ApartmentDto
            {
                ApartmentId = id,
                Room = "A101",
                Area = 70
            };

            _aptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Apartment, ApartmentDto>>>(),
                It.IsAny<Expression<Func<Apartment, bool>>>(),
                It.IsAny<Func<IQueryable<Apartment>, IOrderedQueryable<Apartment>>>(),
                It.IsAny<Func<IQueryable<Apartment>, IIncludableQueryable<Apartment, object>>>()
            )).ReturnsAsync(dto);

            // Act
            var result = await _service.GetApartmentByIdAsync(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result.ApartmentId);
            Assert.Equal("A101", result.Room);
        }

        [Fact]
        public async Task GetApartmentByIdAsync_Throws_WhenNotFound()
        {
            // Arrange
            _aptRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Apartment, ApartmentDto>>>(),
                It.IsAny<Expression<Func<Apartment, bool>>>(),
                It.IsAny<Func<IQueryable<Apartment>, IOrderedQueryable<Apartment>>>(),
                It.IsAny<Func<IQueryable<Apartment>, IIncludableQueryable<Apartment, object>>>()
            )).ReturnsAsync((ApartmentDto?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.GetApartmentByIdAsync(999));
            Assert.Equal("Căn hộ không tồn tại.", ex.Message);
        }

        #endregion

        #region GetApartmentsAsync Tests

        [Fact]
        public async Task GetApartmentsAsync_Success_ReturnsActiveApartments()
        {
            // Arrange
            var floorId = 1;
            var apartments = new List<ApartmentBasicDto>
            {
                new ApartmentBasicDto { ApartmentId = 1, Room = "A101", FloorId = floorId },
                new ApartmentBasicDto { ApartmentId = 2, Room = "A102", FloorId = floorId }
            };

            _aptRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Apartment, ApartmentBasicDto>>>(),
                It.IsAny<Expression<Func<Apartment, bool>>>(),
                It.IsAny<Func<IQueryable<Apartment>, IOrderedQueryable<Apartment>>>(),
                It.IsAny<Func<IQueryable<Apartment>, IIncludableQueryable<Apartment, object>>>()
            )).ReturnsAsync(apartments);

            // Act
            var result = await _service.GetApartmentsByFloorAsync(floorId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        #endregion

        #region GetPaginateApartmentAsync Tests

        [Fact]
        public async Task GetPaginateApartmentAsync_Success_ReturnsPaginatedResult()
        {
            // Arrange
            var floorId = 1;
            var dto = new PaginateDto { page = 1, size = 10 };
            var pagedResult = new Paginate<ApartmentDto>
            {
                Items = new List<ApartmentDto>
                {
                    new ApartmentDto { ApartmentId = 1, Room = "A101", FloorId = floorId  }
                },
                Page = 1,
                Size = 10,
                Total = 1,
                TotalPages = 1
            };

            _floorRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Floor, bool>>>(),
                It.IsAny<Func<IQueryable<Floor>, IIncludableQueryable<Floor, object>>>()
            )).ReturnsAsync(true);

            _aptRepo.Setup(r => r.GetPagingListAsync(
                It.IsAny<Expression<Func<Apartment, ApartmentDto>>>(),
                It.IsAny<Expression<Func<Apartment, bool>>>(),
                It.IsAny<Func<IQueryable<Apartment>, IOrderedQueryable<Apartment>>>(),
                It.IsAny<Func<IQueryable<Apartment>, IIncludableQueryable<Apartment, object>>>(),
                1,
                10
            )).ReturnsAsync(pagedResult);

            // Act
            var result = await _service.GetPaginateApartmentAsync(dto, floorId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
        }

        #endregion
    }
}