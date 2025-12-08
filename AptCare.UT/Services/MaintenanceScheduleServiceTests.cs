using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.MaintenanceScheduleDtos;
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
    public class MaintenanceScheduleServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _uow = new();
        private readonly Mock<IGenericRepository<MaintenanceSchedule>> _scheduleRepo = new();
        private readonly Mock<IGenericRepository<CommonAreaObject>> _objectRepo = new();
        private readonly Mock<IGenericRepository<Technique>> _techniqueRepo = new();
        private readonly Mock<IGenericRepository<MaintenanceTrackingHistory>> _historyRepo = new();
        private readonly Mock<IUserContext> _userContext = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<MaintenanceScheduleService>> _logger = new();

        private readonly MaintenanceScheduleService _service;

        public MaintenanceScheduleServiceTests()
        {
            _uow.Setup(u => u.GetRepository<MaintenanceSchedule>()).Returns(_scheduleRepo.Object);
            _uow.Setup(u => u.GetRepository<CommonAreaObject>()).Returns(_objectRepo.Object);
            _uow.Setup(u => u.GetRepository<Technique>()).Returns(_techniqueRepo.Object);
            _uow.Setup(u => u.GetRepository<MaintenanceTrackingHistory>()).Returns(_historyRepo.Object);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            _service = new MaintenanceScheduleService(
                _uow.Object,
                _logger.Object,
                _mapper.Object,
                _userContext.Object
            );
        }

        #region CreateMaintenanceScheduleAsync Tests

        [Fact]
        public async Task CreateMaintenanceScheduleAsync_Success()
        {
            // Arrange
            var dto = new MaintenanceScheduleCreateDto
            {
                CommonAreaObjectId = 1,
                FrequencyInDays = 30,
                RequiredTechnicians = 2,
                RequiredTechniqueId = 1,
                NextScheduledDate = DateOnly.FromDateTime(DateTime.Now.AddDays(7)),
                Description = "Monthly maintenance"
            };

            var commonAreaObject = new CommonAreaObject
            {
                CommonAreaObjectId = 1,
                Status = ActiveStatus.Active,
                MaintenanceSchedule = null,
                CommonAreaObjectType = new CommonAreaObjectType
                {
                    CommonAreaObjectTypeId = 1,
                    MaintenanceTasks = new List<MaintenanceTask>
                    {
                        new MaintenanceTask { MaintenanceTaskId = 1, EstimatedDurationMinutes = 60 },
                        new MaintenanceTask { MaintenanceTaskId = 2, EstimatedDurationMinutes = 30 }
                    }
                }
            };

            var schedule = new MaintenanceSchedule
            {
                MaintenanceScheduleId = 1,
                CommonAreaObjectId = dto.CommonAreaObjectId,
                FrequencyInDays = dto.FrequencyInDays
            };

            _objectRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObject, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IOrderedQueryable<CommonAreaObject>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IIncludableQueryable<CommonAreaObject, object>>>()
            )).ReturnsAsync(commonAreaObject);

            _techniqueRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Technique, bool>>>(),
                It.IsAny<Func<IQueryable<Technique>, IIncludableQueryable<Technique, object>>>()
            )).ReturnsAsync(true);

            _mapper.Setup(m => m.Map<MaintenanceSchedule>(dto)).Returns(schedule);

            // Act
            var result = await _service.CreateMaintenanceScheduleAsync(dto);

            // Assert
            Assert.Equal("Tạo lịch bảo trì thành công", result);
            Assert.Equal(1.5, schedule.EstimatedDuration); // (60 + 30) / 60 = 1.5 hours
            Assert.Equal(ActiveStatus.Active, schedule.Status);
            _scheduleRepo.Verify(r => r.InsertAsync(It.IsAny<MaintenanceSchedule>()), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateMaintenanceScheduleAsync_Throws_WhenObjectNotFound()
        {
            // Arrange
            var dto = new MaintenanceScheduleCreateDto { CommonAreaObjectId = 999 };

            _objectRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObject, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IOrderedQueryable<CommonAreaObject>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IIncludableQueryable<CommonAreaObject, object>>>()
            )).ReturnsAsync((CommonAreaObject)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateMaintenanceScheduleAsync(dto));
            Assert.Contains("Đối tượng khu vực chung không tồn tại", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateMaintenanceScheduleAsync_Throws_WhenObjectInactive()
        {
            // Arrange
            var dto = new MaintenanceScheduleCreateDto { CommonAreaObjectId = 1 };
            var commonAreaObject = new CommonAreaObject
            {
                CommonAreaObjectId = 1,
                Status = ActiveStatus.Inactive,
                CommonAreaObjectType = new CommonAreaObjectType()
            };

            _objectRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObject, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IOrderedQueryable<CommonAreaObject>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IIncludableQueryable<CommonAreaObject, object>>>()
            )).ReturnsAsync(commonAreaObject);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateMaintenanceScheduleAsync(dto));
            Assert.Contains("đã ngưng hoạt động", ex.Message);
        }

        [Fact]
        public async Task CreateMaintenanceScheduleAsync_Throws_WhenScheduleAlreadyExists()
        {
            // Arrange
            var dto = new MaintenanceScheduleCreateDto { CommonAreaObjectId = 1 };
            var commonAreaObject = new CommonAreaObject
            {
                CommonAreaObjectId = 1,
                Status = ActiveStatus.Active,
                MaintenanceSchedule = new MaintenanceSchedule { MaintenanceScheduleId = 1 },
                CommonAreaObjectType = new CommonAreaObjectType()
            };

            _objectRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObject, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IOrderedQueryable<CommonAreaObject>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IIncludableQueryable<CommonAreaObject, object>>>()
            )).ReturnsAsync(commonAreaObject);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateMaintenanceScheduleAsync(dto));
            Assert.Contains("đã có lịch bảo trì", ex.Message);
        }

        [Fact]
        public async Task CreateMaintenanceScheduleAsync_Throws_WhenTechniqueNotFound()
        {
            // Arrange
            var dto = new MaintenanceScheduleCreateDto
            {
                CommonAreaObjectId = 1,
                RequiredTechniqueId = 999
            };

            var commonAreaObject = new CommonAreaObject
            {
                CommonAreaObjectId = 1,
                Status = ActiveStatus.Active,
                MaintenanceSchedule = null,
                CommonAreaObjectType = new CommonAreaObjectType()
            };

            _objectRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObject, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IOrderedQueryable<CommonAreaObject>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IIncludableQueryable<CommonAreaObject, object>>>()
            )).ReturnsAsync(commonAreaObject);

            _techniqueRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Technique, bool>>>(),
                It.IsAny<Func<IQueryable<Technique>, IIncludableQueryable<Technique, object>>>()
            )).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateMaintenanceScheduleAsync(dto));
            Assert.Contains("Kỹ thuật yêu cầu không tồn tại", ex.Message);
        }

        [Fact]
        public async Task CreateMaintenanceScheduleAsync_Throws_WhenFrequencyInvalid()
        {
            // Arrange
            var dto = new MaintenanceScheduleCreateDto
            {
                CommonAreaObjectId = 1,
                FrequencyInDays = 0 // Invalid
            };

            var commonAreaObject = new CommonAreaObject
            {
                CommonAreaObjectId = 1,
                Status = ActiveStatus.Active,
                MaintenanceSchedule = null,
                CommonAreaObjectType = new CommonAreaObjectType()
            };

            _objectRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObject, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IOrderedQueryable<CommonAreaObject>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IIncludableQueryable<CommonAreaObject, object>>>()
            )).ReturnsAsync(commonAreaObject);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateMaintenanceScheduleAsync(dto));
            Assert.Contains("Chu kỳ bảo trì phải lớn hơn 0", ex.Message);
        }

        [Fact]
        public async Task CreateMaintenanceScheduleAsync_Throws_WhenRequiredTechniciansInvalid()
        {
            // Arrange
            var dto = new MaintenanceScheduleCreateDto
            {
                CommonAreaObjectId = 1,
                FrequencyInDays = 30,
                RequiredTechnicians = -1 // Invalid
            };

            var commonAreaObject = new CommonAreaObject
            {
                CommonAreaObjectId = 1,
                Status = ActiveStatus.Active,
                MaintenanceSchedule = null,
                CommonAreaObjectType = new CommonAreaObjectType()
            };

            _objectRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObject, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IOrderedQueryable<CommonAreaObject>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IIncludableQueryable<CommonAreaObject, object>>>()
            )).ReturnsAsync(commonAreaObject);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateMaintenanceScheduleAsync(dto));
            Assert.Contains("Số lượng kỹ thuật viên yêu cầu phải lớn hơn 0", ex.Message);
        }

        [Fact]
        public async Task CreateMaintenanceScheduleAsync_Throws_WhenNoMaintenanceTasks()
        {
            // Arrange
            var dto = new MaintenanceScheduleCreateDto
            {
                CommonAreaObjectId = 1,
                FrequencyInDays = 30,
                RequiredTechnicians = 2
            };

            var commonAreaObject = new CommonAreaObject
            {
                CommonAreaObjectId = 1,
                Status = ActiveStatus.Active,
                MaintenanceSchedule = null,
                CommonAreaObjectType = new CommonAreaObjectType
                {
                    MaintenanceTasks = new List<MaintenanceTask>() // Empty
                }
            };

            _objectRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObject, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IOrderedQueryable<CommonAreaObject>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IIncludableQueryable<CommonAreaObject, object>>>()
            )).ReturnsAsync(commonAreaObject);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateMaintenanceScheduleAsync(dto));
            Assert.Contains("Chưa có công việc bảo trì", ex.Message);
        }

        [Fact]
        public async Task CreateMaintenanceScheduleAsync_Throws_WhenNextDateInPast()
        {
            // Arrange
            var dto = new MaintenanceScheduleCreateDto
            {
                CommonAreaObjectId = 1,
                FrequencyInDays = 30,
                RequiredTechnicians = 2,
                NextScheduledDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1)) // Past date
            };

            var commonAreaObject = new CommonAreaObject
            {
                CommonAreaObjectId = 1,
                Status = ActiveStatus.Active,
                MaintenanceSchedule = null,
                CommonAreaObjectType = new CommonAreaObjectType
                {
                    MaintenanceTasks = new List<MaintenanceTask>
                    {
                        new MaintenanceTask { EstimatedDurationMinutes = 60 }
                    }
                }
            };

            _objectRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObject, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IOrderedQueryable<CommonAreaObject>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IIncludableQueryable<CommonAreaObject, object>>>()
            )).ReturnsAsync(commonAreaObject);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateMaintenanceScheduleAsync(dto));
            Assert.Contains("không được trong quá khứ", ex.Message);
        }

        #endregion

        #region UpdateMaintenanceScheduleAsync Tests

        [Fact]
        public async Task UpdateMaintenanceScheduleAsync_Success_UpdatesMultipleFields()
        {
            // Arrange
            var userId = 1;
            var scheduleId = 1;
            var dto = new MaintenanceScheduleUpdateDto
            {
                Description = "Updated description",
                FrequencyInDays = 60,
                RequiredTechnicians = 3
            };

            var schedule = new MaintenanceSchedule
            {
                MaintenanceScheduleId = scheduleId,
                Description = "Old description",
                FrequencyInDays = 30,
                RequiredTechnicians = 2
            };

            _userContext.Setup(u => u.CurrentUserId).Returns(userId);

            _scheduleRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<MaintenanceSchedule, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceSchedule>, IOrderedQueryable<MaintenanceSchedule>>>(),
                It.IsAny<Func<IQueryable<MaintenanceSchedule>, IIncludableQueryable<MaintenanceSchedule, object>>>()
            )).ReturnsAsync(schedule);

            // Act
            var result = await _service.UpdateMaintenanceScheduleAsync(scheduleId, dto);

            // Assert
            Assert.Equal("Cập nhật lịch bảo trì thành công", result);
            Assert.Equal("Updated description", schedule.Description);
            Assert.Equal(60, schedule.FrequencyInDays);
            Assert.Equal(3, schedule.RequiredTechnicians);
            _historyRepo.Verify(r => r.InsertAsync(It.IsAny<MaintenanceTrackingHistory>()), Times.Exactly(3));
        }

        [Fact]
        public async Task UpdateMaintenanceScheduleAsync_Success_NoChanges()
        {
            // Arrange
            var scheduleId = 1;
            var dto = new MaintenanceScheduleUpdateDto(); // No changes

            var schedule = new MaintenanceSchedule
            {
                MaintenanceScheduleId = scheduleId,
                Description = "Current description",
                FrequencyInDays = 30
            };

            _scheduleRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<MaintenanceSchedule, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceSchedule>, IOrderedQueryable<MaintenanceSchedule>>>(),
                It.IsAny<Func<IQueryable<MaintenanceSchedule>, IIncludableQueryable<MaintenanceSchedule, object>>>()
            )).ReturnsAsync(schedule);

            // Act
            var result = await _service.UpdateMaintenanceScheduleAsync(scheduleId, dto);

            // Assert
            Assert.Equal("Cập nhật lịch bảo trì thành công", result);
            _historyRepo.Verify(r => r.InsertAsync(It.IsAny<MaintenanceTrackingHistory>()), Times.Never);
        }

        [Fact]
        public async Task UpdateMaintenanceScheduleAsync_Throws_WhenNotFound()
        {
            // Arrange
            var dto = new MaintenanceScheduleUpdateDto();

            _scheduleRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<MaintenanceSchedule, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceSchedule>, IOrderedQueryable<MaintenanceSchedule>>>(),
                It.IsAny<Func<IQueryable<MaintenanceSchedule>, IIncludableQueryable<MaintenanceSchedule, object>>>()
            )).ReturnsAsync((MaintenanceSchedule)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.UpdateMaintenanceScheduleAsync(999, dto));
            Assert.Contains("Lịch bảo trì không tồn tại", ex.Message);
        }

        #endregion

        #region DeleteMaintenanceScheduleAsync Tests

        [Fact]
        public async Task DeleteMaintenanceScheduleAsync_Success()
        {
            // Arrange
            var id = 1;
            var schedule = new MaintenanceSchedule { MaintenanceScheduleId = id };

            _scheduleRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<MaintenanceSchedule, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceSchedule>, IOrderedQueryable<MaintenanceSchedule>>>(),
                It.IsAny<Func<IQueryable<MaintenanceSchedule>, IIncludableQueryable<MaintenanceSchedule, object>>>()
            )).ReturnsAsync(schedule);

            // Act
            var result = await _service.DeleteMaintenanceScheduleAsync(id);

            // Assert
            Assert.Equal("Xóa lịch bảo trì thành công", result);
            _scheduleRepo.Verify(r => r.DeleteAsync(schedule), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteMaintenanceScheduleAsync_Throws_WhenNotFound()
        {
            // Arrange
            _scheduleRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<MaintenanceSchedule, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceSchedule>, IOrderedQueryable<MaintenanceSchedule>>>(),
                It.IsAny<Func<IQueryable<MaintenanceSchedule>, IIncludableQueryable<MaintenanceSchedule, object>>>()
            )).ReturnsAsync((MaintenanceSchedule)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.DeleteMaintenanceScheduleAsync(999));
            Assert.Contains("Lịch bảo trì không tồn tại", ex.Message);
        }

        #endregion

        #region ActivateMaintenanceScheduleAsync Tests

        [Fact]
        public async Task ActivateMaintenanceScheduleAsync_Success()
        {
            // Arrange
            var id = 1;
            var schedule = new MaintenanceSchedule
            {
                MaintenanceScheduleId = id,
                Status = ActiveStatus.Inactive,
                CommonAreaObject = new CommonAreaObject { Status = ActiveStatus.Active }
            };

            _scheduleRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<MaintenanceSchedule, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceSchedule>, IOrderedQueryable<MaintenanceSchedule>>>(),
                It.IsAny<Func<IQueryable<MaintenanceSchedule>, IIncludableQueryable<MaintenanceSchedule, object>>>()
            )).ReturnsAsync(schedule);

            // Act
            var result = await _service.ActivateMaintenanceScheduleAsync(id);

            // Assert
            Assert.Equal("Kích hoạt lịch bảo trì thành công", result);
            Assert.Equal(ActiveStatus.Active, schedule.Status);
            _scheduleRepo.Verify(r => r.UpdateAsync(schedule), Times.Once);
        }

        [Fact]
        public async Task ActivateMaintenanceScheduleAsync_Throws_WhenAlreadyActive()
        {
            // Arrange
            var id = 1;
            var schedule = new MaintenanceSchedule
            {
                MaintenanceScheduleId = id,
                Status = ActiveStatus.Active,
                CommonAreaObject = new CommonAreaObject()
            };

            _scheduleRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<MaintenanceSchedule, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceSchedule>, IOrderedQueryable<MaintenanceSchedule>>>(),
                It.IsAny<Func<IQueryable<MaintenanceSchedule>, IIncludableQueryable<MaintenanceSchedule, object>>>()
            )).ReturnsAsync(schedule);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.ActivateMaintenanceScheduleAsync(id));
            Assert.Contains("đã ở trạng thái hoạt động", ex.Message);
        }

        [Fact]
        public async Task ActivateMaintenanceScheduleAsync_Throws_WhenObjectInactive()
        {
            // Arrange
            var id = 1;
            var schedule = new MaintenanceSchedule
            {
                MaintenanceScheduleId = id,
                Status = ActiveStatus.Inactive,
                CommonAreaObject = new CommonAreaObject { Status = ActiveStatus.Inactive }
            };

            _scheduleRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<MaintenanceSchedule, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceSchedule>, IOrderedQueryable<MaintenanceSchedule>>>(),
                It.IsAny<Func<IQueryable<MaintenanceSchedule>, IIncludableQueryable<MaintenanceSchedule, object>>>()
            )).ReturnsAsync(schedule);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.ActivateMaintenanceScheduleAsync(id));
            Assert.Contains("đối tượng khu vực chung đã ngưng hoạt động", ex.Message);
        }

        #endregion

        #region GetMaintenanceScheduleByIdAsync Tests

        [Fact]
        public async Task GetMaintenanceScheduleByIdAsync_Success()
        {
            // Arrange
            var id = 1;
            var dto = new MaintenanceScheduleDto
            {
                MaintenanceScheduleId = id,
                FrequencyInDays = 30
            };

            _scheduleRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<MaintenanceSchedule, MaintenanceScheduleDto>>>(),
                It.IsAny<Expression<Func<MaintenanceSchedule, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceSchedule>, IOrderedQueryable<MaintenanceSchedule>>>(),
                It.IsAny<Func<IQueryable<MaintenanceSchedule>, IIncludableQueryable<MaintenanceSchedule, object>>>()
            )).ReturnsAsync(dto);

            // Act
            var result = await _service.GetMaintenanceScheduleByIdAsync(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result.MaintenanceScheduleId);
        }

        [Fact]
        public async Task GetMaintenanceScheduleByIdAsync_Throws_WhenNotFound()
        {
            // Arrange
            _scheduleRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<MaintenanceSchedule, MaintenanceScheduleDto>>>(),
                It.IsAny<Expression<Func<MaintenanceSchedule, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceSchedule>, IOrderedQueryable<MaintenanceSchedule>>>(),
                It.IsAny<Func<IQueryable<MaintenanceSchedule>, IIncludableQueryable<MaintenanceSchedule, object>>>()
            )).ReturnsAsync((MaintenanceScheduleDto)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.GetMaintenanceScheduleByIdAsync(999));
            Assert.Contains("Lịch bảo trì không tồn tại", ex.Message);
        }

        #endregion

        #region GetTrackingHistoryAsync Tests

        [Fact]
        public async Task GetTrackingHistoryAsync_Success()
        {
            // Arrange
            var scheduleId = 1;
            var histories = new List<MaintenanceTrackingHistoryDto>
            {
                new MaintenanceTrackingHistoryDto { MaintenanceTrackingHistoryId = 1 },
                new MaintenanceTrackingHistoryDto { MaintenanceTrackingHistoryId = 2 }
            };

            _scheduleRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<MaintenanceSchedule, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceSchedule>, IIncludableQueryable<MaintenanceSchedule, object>>>()
            )).ReturnsAsync(true);

            _historyRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<MaintenanceTrackingHistory, MaintenanceTrackingHistoryDto>>>(),
                It.IsAny<Expression<Func<MaintenanceTrackingHistory, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTrackingHistory>, IOrderedQueryable<MaintenanceTrackingHistory>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTrackingHistory>, IIncludableQueryable<MaintenanceTrackingHistory, object>>>()
            )).ReturnsAsync(histories);

            // Act
            var result = await _service.GetTrackingHistoryAsync(scheduleId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetTrackingHistoryAsync_Throws_WhenScheduleNotFound()
        {
            // Arrange
            _scheduleRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<MaintenanceSchedule, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceSchedule>, IIncludableQueryable<MaintenanceSchedule, object>>>()
            )).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.GetTrackingHistoryAsync(999));
            Assert.Contains("Lịch bảo trì không tồn tại", ex.Message);
        }

        #endregion
    }
}