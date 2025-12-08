using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.MaintenanceTaskDtos;
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
    public class MaintenanceTaskServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _uow = new();
        private readonly Mock<IGenericRepository<MaintenanceTask>> _taskRepo = new();
        private readonly Mock<IGenericRepository<CommonAreaObjectType>> _typeRepo = new();
        private readonly Mock<IGenericRepository<CommonAreaObject>> _objectRepo = new();
        private readonly Mock<IGenericRepository<MaintenanceSchedule>> _scheduleRepo = new();
        private readonly Mock<IRedisCacheService> _cacheService = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<MaintenanceTaskService>> _logger = new();

        private readonly MaintenanceTaskService _service;

        public MaintenanceTaskServiceTests()
        {
            _uow.Setup(u => u.GetRepository<MaintenanceTask>()).Returns(_taskRepo.Object);
            _uow.Setup(u => u.GetRepository<CommonAreaObjectType>()).Returns(_typeRepo.Object);
            _uow.Setup(u => u.GetRepository<CommonAreaObject>()).Returns(_objectRepo.Object);
            _uow.Setup(u => u.GetRepository<MaintenanceSchedule>()).Returns(_scheduleRepo.Object);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            _cacheService.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.GetAsync<MaintenanceTaskDto>(It.IsAny<string>()))
                .ReturnsAsync((MaintenanceTaskDto)null);
            _cacheService.Setup(c => c.GetAsync<IEnumerable<MaintenanceTaskBasicDto>>(It.IsAny<string>()))
                .ReturnsAsync((IEnumerable<MaintenanceTaskBasicDto>)null);

            _service = new MaintenanceTaskService(
                _uow.Object,
                _logger.Object,
                _mapper.Object,
                _cacheService.Object
            );
        }

        #region CreateMaintenanceTaskAsync Tests

        [Fact]
        public async Task CreateMaintenanceTaskAsync_Success()
        {
            // Arrange
            var dto = new MaintenanceTaskCreateDto
            {
                CommonAreaObjectTypeId = 1,
                TaskName = "Clean pool",
                EstimatedDurationMinutes = 60,
                DisplayOrder = 1
            };

            var objectType = new CommonAreaObjectType
            {
                CommonAreaObjectTypeId = 1,
                Status = ActiveStatus.Active
            };

            var task = new MaintenanceTask
            {
                MaintenanceTaskId = 1,
                TaskName = dto.TaskName
            };

            _typeRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObjectType, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IOrderedQueryable<CommonAreaObjectType>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IIncludableQueryable<CommonAreaObjectType, object>>>()
            )).ReturnsAsync(objectType);

            _taskRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<MaintenanceTask, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTask>, IIncludableQueryable<MaintenanceTask, object>>>()
            )).ReturnsAsync(false);

            _mapper.Setup(m => m.Map<MaintenanceTask>(dto)).Returns(task);

            _taskRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<MaintenanceTask, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTask>, IOrderedQueryable<MaintenanceTask>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTask>, IIncludableQueryable<MaintenanceTask, object>>>()
            )).ReturnsAsync(new List<MaintenanceTask>());

            _objectRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<CommonAreaObject, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IOrderedQueryable<CommonAreaObject>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IIncludableQueryable<CommonAreaObject, object>>>()
            )).ReturnsAsync(new List<CommonAreaObject>());

            // Act
            var result = await _service.CreateMaintenanceTaskAsync(dto);

            // Assert
            Assert.Equal("Tạo nhiệm vụ bảo trì mới thành công", result);
            Assert.Equal(ActiveStatus.Active, task.Status);
            _taskRepo.Verify(r => r.InsertAsync(task), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateMaintenanceTaskAsync_Throws_WhenTypeNotFound()
        {
            // Arrange
            var dto = new MaintenanceTaskCreateDto { CommonAreaObjectTypeId = 999 };

            _typeRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObjectType, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IOrderedQueryable<CommonAreaObjectType>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IIncludableQueryable<CommonAreaObjectType, object>>>()
            )).ReturnsAsync((CommonAreaObjectType)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.CreateMaintenanceTaskAsync(dto));
            Assert.Contains("Loại đối tượng khu vực chung không tồn tại", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateMaintenanceTaskAsync_Throws_WhenTypeInactive()
        {
            // Arrange
            var dto = new MaintenanceTaskCreateDto { CommonAreaObjectTypeId = 1 };
            var objectType = new CommonAreaObjectType
            {
                CommonAreaObjectTypeId = 1,
                Status = ActiveStatus.Inactive
            };

            _typeRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObjectType, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IOrderedQueryable<CommonAreaObjectType>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IIncludableQueryable<CommonAreaObjectType, object>>>()
            )).ReturnsAsync(objectType);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.CreateMaintenanceTaskAsync(dto));
            Assert.Contains("đã ngưng hoạt động", ex.Message);
        }

        [Fact]
        public async Task CreateMaintenanceTaskAsync_Throws_WhenDuplicateTaskName()
        {
            // Arrange
            var dto = new MaintenanceTaskCreateDto
            {
                CommonAreaObjectTypeId = 1,
                TaskName = "Clean Pool"
            };

            var objectType = new CommonAreaObjectType
            {
                CommonAreaObjectTypeId = 1,
                Status = ActiveStatus.Active
            };

            _typeRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObjectType, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IOrderedQueryable<CommonAreaObjectType>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IIncludableQueryable<CommonAreaObjectType, object>>>()
            )).ReturnsAsync(objectType);

            _taskRepo.SetupSequence(r => r.AnyAsync(
                It.IsAny<Expression<Func<MaintenanceTask, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTask>, IIncludableQueryable<MaintenanceTask, object>>>()
            ))
            .ReturnsAsync(true);  // Duplicate name

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.CreateMaintenanceTaskAsync(dto));
            Assert.Contains("Tên nhiệm vụ đã tồn tại", ex.Message);
        }

        [Fact]
        public async Task CreateMaintenanceTaskAsync_Throws_WhenDuplicateDisplayOrder()
        {
            // Arrange
            var dto = new MaintenanceTaskCreateDto
            {
                CommonAreaObjectTypeId = 1,
                TaskName = "New Task",
                DisplayOrder = 1
            };

            var objectType = new CommonAreaObjectType
            {
                CommonAreaObjectTypeId = 1,
                Status = ActiveStatus.Active
            };

            _typeRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObjectType, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IOrderedQueryable<CommonAreaObjectType>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IIncludableQueryable<CommonAreaObjectType, object>>>()
            )).ReturnsAsync(objectType);

            _taskRepo.SetupSequence(r => r.AnyAsync(
                It.IsAny<Expression<Func<MaintenanceTask, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTask>, IIncludableQueryable<MaintenanceTask, object>>>()
            ))
            .ReturnsAsync(false)  // Name check passes
            .ReturnsAsync(true);  // DisplayOrder duplicate

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.CreateMaintenanceTaskAsync(dto));
            Assert.Contains("Thứ tự hiển thị đã tồn tại", ex.Message);
        }

        #endregion

        #region UpdateMaintenanceTaskAsync Tests

        [Fact]
        public async Task UpdateMaintenanceTaskAsync_Success()
        {
            // Arrange
            var id = 1;
            var dto = new MaintenanceTaskUpdateDto
            {
                CommonAreaObjectTypeId = 1,
                TaskName = "Updated Task",
                EstimatedDurationMinutes = 90,
                DisplayOrder = 2
            };

            var task = new MaintenanceTask
            {
                MaintenanceTaskId = id,
                TaskName = "Old Task",
                CommonAreaObjectTypeId = 1
            };

            var objectType = new CommonAreaObjectType
            {
                CommonAreaObjectTypeId = 1,
                Status = ActiveStatus.Active
            };

            _taskRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<MaintenanceTask, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTask>, IOrderedQueryable<MaintenanceTask>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTask>, IIncludableQueryable<MaintenanceTask, object>>>()
            )).ReturnsAsync(task);

            _typeRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<CommonAreaObjectType, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IOrderedQueryable<CommonAreaObjectType>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IIncludableQueryable<CommonAreaObjectType, object>>>()
            )).ReturnsAsync(objectType);

            _taskRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<MaintenanceTask, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTask>, IIncludableQueryable<MaintenanceTask, object>>>()
            )).ReturnsAsync(false);

            _mapper.Setup(m => m.Map(dto, task));

            _taskRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<MaintenanceTask, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTask>, IOrderedQueryable<MaintenanceTask>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTask>, IIncludableQueryable<MaintenanceTask, object>>>()
            )).ReturnsAsync(new List<MaintenanceTask>());

            _objectRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<CommonAreaObject, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IOrderedQueryable<CommonAreaObject>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IIncludableQueryable<CommonAreaObject, object>>>()
            )).ReturnsAsync(new List<CommonAreaObject>());

            // Act
            var result = await _service.UpdateMaintenanceTaskAsync(id, dto);

            // Assert
            Assert.Equal("Cập nhật nhiệm vụ bảo trì thành công", result);
            _taskRepo.Verify(r => r.UpdateAsync(task), Times.Once);
        }

        [Fact]
        public async Task UpdateMaintenanceTaskAsync_Throws_WhenNotFound()
        {
            // Arrange
            var dto = new MaintenanceTaskUpdateDto();

            _taskRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<MaintenanceTask, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTask>, IOrderedQueryable<MaintenanceTask>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTask>, IIncludableQueryable<MaintenanceTask, object>>>()
            )).ReturnsAsync((MaintenanceTask)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.UpdateMaintenanceTaskAsync(999, dto));
            Assert.Contains("Nhiệm vụ bảo trì không tồn tại", ex.Message);
        }

        #endregion

        #region DeleteMaintenanceTaskAsync Tests

        [Fact]
        public async Task DeleteMaintenanceTaskAsync_Success()
        {
            // Arrange
            var id = 1;
            var task = new MaintenanceTask
            {
                MaintenanceTaskId = id,
                CommonAreaObjectTypeId = 1,
                RepairRequestTasks = new List<RepairRequestTask>()
            };

            _taskRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<MaintenanceTask, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTask>, IOrderedQueryable<MaintenanceTask>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTask>, IIncludableQueryable<MaintenanceTask, object>>>()
            )).ReturnsAsync(task);

            _taskRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<MaintenanceTask, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTask>, IOrderedQueryable<MaintenanceTask>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTask>, IIncludableQueryable<MaintenanceTask, object>>>()
            )).ReturnsAsync(new List<MaintenanceTask>());

            _objectRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<CommonAreaObject, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IOrderedQueryable<CommonAreaObject>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IIncludableQueryable<CommonAreaObject, object>>>()
            )).ReturnsAsync(new List<CommonAreaObject>());

            // Act
            var result = await _service.DeleteMaintenanceTaskAsync(id);

            // Assert
            Assert.Equal("Xóa nhiệm vụ bảo trì thành công", result);
            _taskRepo.Verify(r => r.DeleteAsync(task), Times.Once);
        }

        [Fact]
        public async Task DeleteMaintenanceTaskAsync_Throws_WhenHasRepairRequests()
        {
            // Arrange
            var id = 1;
            var task = new MaintenanceTask
            {
                MaintenanceTaskId = id,
                RepairRequestTasks = new List<RepairRequestTask>
                {
                    new RepairRequestTask { RepairRequestTaskId = 1 }
                }
            };

            _taskRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<MaintenanceTask, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTask>, IOrderedQueryable<MaintenanceTask>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTask>, IIncludableQueryable<MaintenanceTask, object>>>()
            )).ReturnsAsync(task);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.DeleteMaintenanceTaskAsync(id));
            Assert.Contains("đang có yêu cầu sửa chữa liên kết", ex.Message);
        }

        #endregion

        #region ActivateMaintenanceTaskAsync Tests

        [Fact]
        public async Task ActivateMaintenanceTaskAsync_Success()
        {
            // Arrange
            var id = 1;
            var task = new MaintenanceTask
            {
                MaintenanceTaskId = id,
                Status = ActiveStatus.Inactive,
                CommonAreaObjectTypeId = 1,
                CommonAreaObjectType = new CommonAreaObjectType { Status = ActiveStatus.Active }
            };

            _taskRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<MaintenanceTask, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTask>, IOrderedQueryable<MaintenanceTask>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTask>, IIncludableQueryable<MaintenanceTask, object>>>()
            )).ReturnsAsync(task);

            _taskRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<MaintenanceTask, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTask>, IOrderedQueryable<MaintenanceTask>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTask>, IIncludableQueryable<MaintenanceTask, object>>>()
            )).ReturnsAsync(new List<MaintenanceTask>());

            _objectRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<CommonAreaObject, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IOrderedQueryable<CommonAreaObject>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObject>, IIncludableQueryable<CommonAreaObject, object>>>()
            )).ReturnsAsync(new List<CommonAreaObject>());

            // Act
            var result = await _service.ActivateMaintenanceTaskAsync(id);

            // Assert
            Assert.Equal("Kích hoạt nhiệm vụ bảo trì thành công", result);
            Assert.Equal(ActiveStatus.Active, task.Status);
        }

        [Fact]
        public async Task ActivateMaintenanceTaskAsync_Throws_WhenAlreadyActive()
        {
            // Arrange
            var id = 1;
            var task = new MaintenanceTask
            {
                MaintenanceTaskId = id,
                Status = ActiveStatus.Active,
                CommonAreaObjectType = new CommonAreaObjectType()
            };

            _taskRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<MaintenanceTask, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTask>, IOrderedQueryable<MaintenanceTask>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTask>, IIncludableQueryable<MaintenanceTask, object>>>()
            )).ReturnsAsync(task);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.ActivateMaintenanceTaskAsync(id));
            Assert.Contains("đã ở trạng thái hoạt động", ex.Message);
        }

        #endregion

        #region GetMaintenanceTasksByTypeAsync Tests

        [Fact]
        public async Task GetMaintenanceTasksByTypeAsync_Success()
        {
            // Arrange
            var typeId = 1;
            var tasks = new List<MaintenanceTaskBasicDto>
            {
                new MaintenanceTaskBasicDto { MaintenanceTaskId = 1, TaskName = "Task 1" },
                new MaintenanceTaskBasicDto { MaintenanceTaskId = 2, TaskName = "Task 2" }
            };

            _typeRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<CommonAreaObjectType, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IIncludableQueryable<CommonAreaObjectType, object>>>()
            )).ReturnsAsync(true);

            _taskRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<MaintenanceTask, MaintenanceTaskBasicDto>>>(),
                It.IsAny<Expression<Func<MaintenanceTask, bool>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTask>, IOrderedQueryable<MaintenanceTask>>>(),
                It.IsAny<Func<IQueryable<MaintenanceTask>, IIncludableQueryable<MaintenanceTask, object>>>()
            )).ReturnsAsync(tasks);

            // Act
            var result = await _service.GetMaintenanceTasksByTypeAsync(typeId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetMaintenanceTasksByTypeAsync_Throws_WhenTypeNotFound()
        {
            // Arrange
            _typeRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<CommonAreaObjectType, bool>>>(),
                It.IsAny<Func<IQueryable<CommonAreaObjectType>, IIncludableQueryable<CommonAreaObjectType, object>>>()
            )).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.GetMaintenanceTasksByTypeAsync(999));
            Assert.Contains("Loại đối tượng khu vực chung không tồn tại", ex.Message);
        }

        #endregion
    }
}