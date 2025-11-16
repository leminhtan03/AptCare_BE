using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.WorkSlotDtos;
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
    public class WorkSlotServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _uow = new();
        private readonly Mock<IGenericRepository<WorkSlot>> _workSlotRepo = new();
        private readonly Mock<IGenericRepository<User>> _userRepo = new();
        private readonly Mock<IGenericRepository<Slot>> _slotRepo = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<IUserContext> _userContext = new();
        private readonly Mock<ILogger<WorkSlotService>> _logger = new();

        private readonly WorkSlotService _service;

        public WorkSlotServiceTests()
        {
            _uow.Setup(u => u.GetRepository<WorkSlot>()).Returns(_workSlotRepo.Object);
            _uow.Setup(u => u.GetRepository<User>()).Returns(_userRepo.Object);
            _uow.Setup(u => u.GetRepository<Slot>()).Returns(_slotRepo.Object);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            _service = new WorkSlotService(_uow.Object, _logger.Object, _mapper.Object, _userContext.Object);
        }

        #region CreateWorkSlotsFromDateToDateAsync Tests

        [Fact]
        public async Task CreateWorkSlotsFromDateToDateAsync_Success_CreatesMultipleWorkSlots()
        {
            // Arrange
            var dto = new WorkSlotCreateFromDateToDateDto
            {
                TechnicianId = 1,
                FromDate = DateOnly.FromDateTime(DateTime.UtcNow),
                ToDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                SlotId = 1
            };

            _userRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync(true);

            var slot = new Slot { SlotId = 1, FromTime = new TimeSpan(8, 0, 0), ToTime = new TimeSpan(16, 0, 0) };
            _slotRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Slot, bool>>>(),
                It.IsAny<Func<IQueryable<Slot>, IOrderedQueryable<Slot>>>(),
                It.IsAny<Func<IQueryable<Slot>, IIncludableQueryable<Slot, object>>>()
            )).ReturnsAsync(slot);

            _workSlotRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<WorkSlot, bool>>>(),
                It.IsAny<Func<IQueryable<WorkSlot>, IIncludableQueryable<WorkSlot, object>>>()
            )).ReturnsAsync(false);

            // Act
            var result = await _service.CreateWorkSlotsFromDateToDateAsync(dto);

            // Assert
            Assert.Equal("Taọ lịch làm việc mới thành công.", result);
            _workSlotRepo.Verify(r => r.InsertRangeAsync(It.Is<IEnumerable<WorkSlot>>(ws => ws.Count() == 3)), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateWorkSlotsFromDateToDateAsync_Throws_WhenTechnicianNotExists()
        {
            // Arrange
            var dto = new WorkSlotCreateFromDateToDateDto { TechnicianId = 999 };

            _userRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateWorkSlotsFromDateToDateAsync(dto));
            Assert.Equal("Lỗi hệ thống: Kĩ thuật viên không tồn tại.", ex.Message);
        }

        [Fact]
        public async Task CreateWorkSlotsFromDateToDateAsync_Throws_WhenFromDateAfterToDate()
        {
            // Arrange
            var dto = new WorkSlotCreateFromDateToDateDto
            {
                TechnicianId = 1,
                FromDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
                ToDate = DateOnly.FromDateTime(DateTime.UtcNow)
            };

            _userRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateWorkSlotsFromDateToDateAsync(dto));
            Assert.Equal("Lỗi hệ thống: 'Từ ngày' phải nhỏ hơn hoặc bằng 'Đến ngày'", ex.Message);
        }

        [Fact]
        public async Task CreateWorkSlotsFromDateToDateAsync_Throws_WhenSlotNotExists()
        {
            // Arrange
            var dto = new WorkSlotCreateFromDateToDateDto
            {
                TechnicianId = 1,
                FromDate = DateOnly.FromDateTime(DateTime.UtcNow),
                ToDate = DateOnly.FromDateTime(DateTime.UtcNow),
                SlotId = 999
            };

            _userRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync(true);

            _slotRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Slot, bool>>>(),
                It.IsAny<Func<IQueryable<Slot>, IOrderedQueryable<Slot>>>(),
                It.IsAny<Func<IQueryable<Slot>, IIncludableQueryable<Slot, object>>>()
            )).ReturnsAsync((Slot)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateWorkSlotsFromDateToDateAsync(dto));
            Assert.Equal("Lỗi hệ thống: Slot không tồn tại.", ex.Message);
        }

        [Fact]
        public async Task CreateWorkSlotsFromDateToDateAsync_Throws_WhenWorkSlotAlreadyExists()
        {
            // Arrange
            var dto = new WorkSlotCreateFromDateToDateDto
            {
                TechnicianId = 1,
                FromDate = DateOnly.FromDateTime(DateTime.UtcNow),
                ToDate = DateOnly.FromDateTime(DateTime.UtcNow),
                SlotId = 1
            };

            _userRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync(true);

            var slot = new Slot { SlotId = 1 };
            _slotRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Slot, bool>>>(),
                It.IsAny<Func<IQueryable<Slot>, IOrderedQueryable<Slot>>>(),
                It.IsAny<Func<IQueryable<Slot>, IIncludableQueryable<Slot, object>>>()
            )).ReturnsAsync(slot);

            _workSlotRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<WorkSlot, bool>>>(),
                It.IsAny<Func<IQueryable<WorkSlot>, IIncludableQueryable<WorkSlot, object>>>()
            )).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CreateWorkSlotsFromDateToDateAsync(dto));
            Assert.Equal("Lỗi hệ thống: Lịch làm việc đã tồn tại.", ex.Message);
        }

        #endregion

        #region CheckInAsync Tests

        [Fact]
        public async Task CheckInAsync_Success_UpdatesStatusToWorking()
        {
            // Arrange
            var date = DateOnly.FromDateTime(DateTime.UtcNow);
            var slotId = 1;
            var userId = 1;

            _userContext.SetupGet(u => u.CurrentUserId).Returns(userId);

            var workSlot = new WorkSlot
            {
                WorkSlotId = 1,
                Date = date,
                SlotId = slotId,
                TechnicianId = userId,
                Status = WorkSlotStatus.NotStarted
            };

            _workSlotRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<WorkSlot, bool>>>(),
                It.IsAny<Func<IQueryable<WorkSlot>, IOrderedQueryable<WorkSlot>>>(),
                It.IsAny<Func<IQueryable<WorkSlot>, IIncludableQueryable<WorkSlot, object>>>()
            )).ReturnsAsync(workSlot);

            // Act
            var result = await _service.CheckInAsync(date, slotId);

            // Assert
            Assert.Equal("Điểm danh đầu giờ thành công.", result);
            Assert.Equal(WorkSlotStatus.Working, workSlot.Status);
            _workSlotRepo.Verify(r => r.UpdateAsync(workSlot), Times.Once);
        }

        [Fact]
        public async Task CheckInAsync_Throws_WhenWorkSlotNotFound()
        {
            // Arrange
            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            _workSlotRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<WorkSlot, bool>>>(),
                It.IsAny<Func<IQueryable<WorkSlot>, IOrderedQueryable<WorkSlot>>>(),
                It.IsAny<Func<IQueryable<WorkSlot>, IIncludableQueryable<WorkSlot, object>>>()
            )).ReturnsAsync((WorkSlot)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CheckInAsync(DateOnly.FromDateTime(DateTime.UtcNow), 1));
            Assert.Equal("Lịch làm việc không tồn tại.", ex.Message);
        }

        [Fact]
        public async Task CheckInAsync_Throws_WhenStatusNotNotStarted()
        {
            // Arrange
            var workSlot = new WorkSlot { Status = WorkSlotStatus.Working };
            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            _workSlotRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<WorkSlot, bool>>>(),
                It.IsAny<Func<IQueryable<WorkSlot>, IOrderedQueryable<WorkSlot>>>(),
                It.IsAny<Func<IQueryable<WorkSlot>, IIncludableQueryable<WorkSlot, object>>>()
            )).ReturnsAsync(workSlot);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.CheckInAsync(DateOnly.FromDateTime(DateTime.UtcNow), 1));
            Assert.Contains("Trạng thái lịch làm việc là", ex.Message);
        }

        #endregion

        #region CheckOutAsync Tests

        [Fact]
        public async Task CheckOutAsync_Success_UpdatesStatusToWorking()
        {
            // Arrange
            var date = DateOnly.FromDateTime(DateTime.UtcNow);
            var slotId = 1;
            var userId = 1;

            _userContext.SetupGet(u => u.CurrentUserId).Returns(userId);

            var workSlot = new WorkSlot
            {
                WorkSlotId = 1,
                Date = date,
                SlotId = slotId,
                TechnicianId = userId,
                Status = WorkSlotStatus.Working
            };

            _workSlotRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<WorkSlot, bool>>>(),
                It.IsAny<Func<IQueryable<WorkSlot>, IOrderedQueryable<WorkSlot>>>(),
                It.IsAny<Func<IQueryable<WorkSlot>, IIncludableQueryable<WorkSlot, object>>>()
            )).ReturnsAsync(workSlot);

            // Act
            var result = await _service.CheckOutAsync(date, slotId);

            // Assert
            Assert.Equal("Điểm danh cuối giờ thành công.", result);
            _workSlotRepo.Verify(r => r.UpdateAsync(workSlot), Times.Once);
        }

        #endregion

        #region DeleteWorkSlotAsync Tests

        [Fact]
        public async Task DeleteWorkSlotAsync_Success_DeletesWorkSlot()
        {
            // Arrange
            var id = 1;
            var workSlot = new WorkSlot { WorkSlotId = id };

            _workSlotRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<WorkSlot, bool>>>(),
                It.IsAny<Func<IQueryable<WorkSlot>, IOrderedQueryable<WorkSlot>>>(),
                It.IsAny<Func<IQueryable<WorkSlot>, IIncludableQueryable<WorkSlot, object>>>()
            )).ReturnsAsync(workSlot);

            // Act
            var result = await _service.DeleteWorkSlotAsync(id);

            // Assert
            Assert.Equal("Xóa lịch làm việc thành công.", result);
            _workSlotRepo.Verify(r => r.DeleteAsync(workSlot), Times.Once);
        }

        [Fact]
        public async Task DeleteWorkSlotAsync_Throws_WhenNotFound()
        {
            // Arrange
            _workSlotRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<WorkSlot, bool>>>(),
                It.IsAny<Func<IQueryable<WorkSlot>, IOrderedQueryable<WorkSlot>>>(),
                It.IsAny<Func<IQueryable<WorkSlot>, IIncludableQueryable<WorkSlot, object>>>()
            )).ReturnsAsync((WorkSlot)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() => _service.DeleteWorkSlotAsync(999));
            Assert.Equal("Lỗi hệ thống: lịch làm việc không tồn tại.", ex.Message);
        }

        #endregion
    }
}