using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.SlotDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Implements;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace AptCare.UT.Services
{
    public class SlotServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _uow = new();
        private readonly Mock<IGenericRepository<Slot>> _slotRepo = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<SlotService>> _logger = new();
        private readonly Mock<IRedisCacheService> _cacheService = new();

        private readonly SlotService _service;

        public SlotServiceTests()
        {
            _uow.Setup(u => u.GetRepository<Slot>()).Returns(_slotRepo.Object);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            _cacheService.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.GetAsync<SlotDto>(It.IsAny<string>()))
                .ReturnsAsync((SlotDto)null);

            _cacheService.Setup(c => c.GetAsync<IEnumerable<SlotDto>>(It.IsAny<string>()))
                .ReturnsAsync((IEnumerable<SlotDto>)null);

            _cacheService.Setup(c => c.GetAsync<List<SlotDto>>(It.IsAny<string>()))
                .ReturnsAsync((List<SlotDto>)null);

            _service = new SlotService(_uow.Object, _logger.Object, _mapper.Object, _cacheService.Object);
        }

        #region CreateSlotAsync Tests

        [Fact]
        public async Task CreateSlotAsync_Success_CreatesSlot()
        {
            // Arrange
            var dto = new SlotCreateDto
            {
                FromTime = new TimeSpan(8, 0, 0),
                ToTime = new TimeSpan(12, 0, 0)
            };

            _slotRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Slot, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Slot>, IIncludableQueryable<Slot, object>>>()
            )).ReturnsAsync(false);

            var slot = new Slot { SlotId = 1, FromTime = dto.FromTime, ToTime = dto.ToTime };
            _mapper.Setup(m => m.Map<Slot>(dto)).Returns(slot);

            // Act
            var result = await _service.CreateSlotAsync(dto);

            // Assert
            Assert.Equal("Tạo slot mới thành công", result);
            _slotRepo.Verify(r => r.InsertAsync(slot), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateSlotAsync_Throws_WhenSlotAlreadyExists()
        {
            // Arrange
            var dto = new SlotCreateDto
            {
                FromTime = new TimeSpan(8, 0, 0),
                ToTime = new TimeSpan(12, 0, 0)
            };

            _slotRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Slot, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Slot>, IIncludableQueryable<Slot, object>>>()
            )).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateSlotAsync(dto));
            Assert.Equal("Lỗi hệ thống: Đã tồn tại slot có khoảng thời gian này.", ex.Message);
        }

        [Fact]
        public async Task CreateSlotAsync_Throws_WhenFromTimeAfterToTime()
        {
            // Arrange
            var dto = new SlotCreateDto
            {
                FromTime = new TimeSpan(12, 0, 0),
                ToTime = new TimeSpan(8, 0, 0)
            };

            _slotRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Slot, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Slot>, IIncludableQueryable<Slot, object>>>()
            )).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateSlotAsync(dto));
            Assert.Equal("Lỗi hệ thống: Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.", ex.Message);
        }

        #endregion

        #region UpdateSlotAsync Tests

        [Fact]
        public async Task UpdateSlotAsync_Success_UpdatesSlot()
        {
            // Arrange
            var id = 1;
            var dto = new SlotUpdateDto
            {
                FromTime = new TimeSpan(9, 0, 0),
                ToTime = new TimeSpan(13, 0, 0)
            };

            var slot = new Slot { SlotId = id, FromTime = new TimeSpan(8, 0, 0), ToTime = new TimeSpan(12, 0, 0) };

            _slotRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Slot, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Slot>, System.Linq.IOrderedQueryable<Slot>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Slot>, IIncludableQueryable<Slot, object>>>()
            )).ReturnsAsync(slot);

            _slotRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Slot, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Slot>, IIncludableQueryable<Slot, object>>>()
            )).ReturnsAsync(false);

            _mapper.Setup(m => m.Map(dto, slot));

            // Act
            var result = await _service.UpdateSlotAsync(id, dto);

            // Assert
            Assert.Equal("Cập nhật slot thành công", result);
            _slotRepo.Verify(r => r.UpdateAsync(slot), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateSlotAsync_Throws_WhenNotFound()
        {
            // Arrange
            var dto = new SlotUpdateDto();

            _slotRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Slot, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Slot>, System.Linq.IOrderedQueryable<Slot>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Slot>, IIncludableQueryable<Slot, object>>>()
            )).ReturnsAsync((Slot)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.UpdateSlotAsync(999, dto));
            Assert.Equal("Lỗi hệ thống: Slot không tồn tại.", ex.Message);
        }

        #endregion

        #region DeleteSlotAsync Tests

        [Fact]
        public async Task DeleteSlotAsync_Success_DeletesSlot()
        {
            // Arrange
            var id = 1;
            var slot = new Slot { SlotId = id };

            _slotRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Slot, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Slot>, System.Linq.IOrderedQueryable<Slot>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Slot>, IIncludableQueryable<Slot, object>>>()
            )).ReturnsAsync(slot);

            // Act
            var result = await _service.DeleteSlotAsync(id);

            // Assert
            Assert.Equal("Xóa slot thành công", result);
            _slotRepo.Verify(r => r.DeleteAsync(slot), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteSlotAsync_Throws_WhenNotFound()
        {
            // Arrange
            _slotRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Slot, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Slot>, System.Linq.IOrderedQueryable<Slot>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Slot>, IIncludableQueryable<Slot, object>>>()
            )).ReturnsAsync((Slot)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.DeleteSlotAsync(999));
            Assert.Equal("Lỗi hệ thống: Slot không tồn tại.", ex.Message);
        }

        #endregion

        #region GetSlotByIdAsync Tests

        [Fact]
        public async Task GetSlotByIdAsync_Success_ReturnsSlot()
        {
            // Arrange
            var id = 1;
            var dto = new SlotDto { SlotId = id, FromTime = new TimeSpan(8, 0, 0), ToTime = new TimeSpan(12, 0, 0) };

            _slotRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Slot, SlotDto>>>(),
                It.IsAny<Expression<Func<Slot, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Slot>, System.Linq.IOrderedQueryable<Slot>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Slot>, IIncludableQueryable<Slot, object>>>()
            )).ReturnsAsync(dto);

            // Act
            var result = await _service.GetSlotByIdAsync(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result.SlotId);
        }

        [Fact]
        public async Task GetSlotByIdAsync_Throws_WhenNotFound()
        {
            // Arrange
            _slotRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Slot, SlotDto>>>(),
                It.IsAny<Expression<Func<Slot, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Slot>, System.Linq.IOrderedQueryable<Slot>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Slot>, IIncludableQueryable<Slot, object>>>()
            )).ReturnsAsync((SlotDto)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.GetSlotByIdAsync(999));
            Assert.Equal("Slot không tồn tại", ex.Message);
        }

        #endregion

        #region GetSlotsAsync Tests

        [Fact]
        public async Task GetSlotsAsync_Success_ReturnsAllSlots()
        {
            // Arrange
            var slots = new List<SlotDto>
            {
                new SlotDto { SlotId = 1, FromTime = new TimeSpan(8, 0, 0), ToTime = new TimeSpan(12, 0, 0) },
                new SlotDto { SlotId = 2, FromTime = new TimeSpan(13, 0, 0), ToTime = new TimeSpan(17, 0, 0) }
            };

            _slotRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Slot, SlotDto>>>(),
                It.IsAny<Expression<Func<Slot, bool>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Slot>, System.Linq.IOrderedQueryable<Slot>>>(),
                It.IsAny<Func<System.Linq.IQueryable<Slot>, IIncludableQueryable<Slot, object>>>()
            )).ReturnsAsync(slots);

            // Act
            var result = await _service.GetSlotsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        #endregion
    }
}