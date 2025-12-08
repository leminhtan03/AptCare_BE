using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.InspectionReporDtos;
using AptCare.Service.Dtos.RepairRequestTaskDtos;
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
    public class RepairRequestTaskServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _uow = new();
        private readonly Mock<IGenericRepository<RepairRequestTask>> _taskRepo = new();
        private readonly Mock<IGenericRepository<RepairRequest>> _requestRepo = new();
        private readonly Mock<IRedisCacheService> _cacheService = new();
        private readonly Mock<IUserContext> _userContext = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<RepairRequestTaskService>> _logger = new();

        private readonly RepairRequestTaskService _service;

        public RepairRequestTaskServiceTests()
        {
            _uow.Setup(u => u.GetRepository<RepairRequestTask>()).Returns(_taskRepo.Object);
            _uow.Setup(u => u.GetRepository<RepairRequest>()).Returns(_requestRepo.Object);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            _cacheService.Setup(c => c.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.GetAsync<RepairRequestTaskDto>(It.IsAny<string>()))
                .ReturnsAsync((RepairRequestTaskDto)null);
            _cacheService.Setup(c => c.GetAsync<IEnumerable<RepairRequestTaskDto>>(It.IsAny<string>()))
                .ReturnsAsync((IEnumerable<RepairRequestTaskDto>)null);

            _service = new RepairRequestTaskService(
                _uow.Object,
                _logger.Object,
                _mapper.Object,
                _cacheService.Object,
                _userContext.Object
            );
        }

        #region UpdateRepairRequestTaskStatusAsync Tests

        [Fact]
        public async Task UpdateRepairRequestTaskStatusAsync_Success()
        {
            // Arrange
            var userId = 1;
            var taskId = 1;
            var dto = new RepairRequestTaskStatusUpdateDto
            {
                Status = TaskCompletionStatus.Completed,
                TechnicianNote = "Task completed successfully",
                InspectionResult = "All good"
            };

            var task = new RepairRequestTask
            {
                RepairRequestTaskId = taskId,
                RepairRequestId = 10,
                Status = TaskCompletionStatus.Pending,
                TaskName = "Fix leak"
            };

            _userContext.Setup(u => u.CurrentUserId).Returns(userId);

            _taskRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequestTask, bool>>>(),
                It.IsAny<Func<IQueryable<RepairRequestTask>, IOrderedQueryable<RepairRequestTask>>>(),
                It.IsAny<Func<IQueryable<RepairRequestTask>, IIncludableQueryable<RepairRequestTask, object>>>()
            )).ReturnsAsync(task);

            _mapper.Setup(m => m.Map(dto, task));

            // Act
            var result = await _service.UpdateRepairRequestTaskStatusAsync(taskId, dto);

            // Assert
            Assert.Equal("Cập nhật trạng thái nhiệm vụ sửa chữa thành công", result);
            Assert.Equal(userId, task.CompletedByUserId);
            _taskRepo.Verify(r => r.UpdateAsync(task), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
            _cacheService.Verify(c => c.RemoveAsync($"repair_request_task:{taskId}"), Times.Once);
        }

        [Fact]
        public async Task UpdateRepairRequestTaskStatusAsync_Throws_WhenNotFound()
        {
            // Arrange
            var dto = new RepairRequestTaskStatusUpdateDto { Status = TaskCompletionStatus.Completed };

            _taskRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequestTask, bool>>>(),
                It.IsAny<Func<IQueryable<RepairRequestTask>, IOrderedQueryable<RepairRequestTask>>>(),
                It.IsAny<Func<IQueryable<RepairRequestTask>, IIncludableQueryable<RepairRequestTask, object>>>()
            )).ReturnsAsync((RepairRequestTask)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.UpdateRepairRequestTaskStatusAsync(999, dto));
            Assert.Contains("Nhiệm vụ sửa chữa không tồn tại", ex.Message);
        }

        #endregion

        #region GetRepairRequestTaskByIdAsync Tests

        [Fact]
        public async Task GetRepairRequestTaskByIdAsync_Success()
        {
            // Arrange
            var taskId = 1;
            var taskDto = new RepairRequestTaskDto
            {
                RepairRequestTaskId = taskId,
                TaskName = "Fix leak"
            };

            _taskRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequestTask, RepairRequestTaskDto>>>(),
                It.IsAny<Expression<Func<RepairRequestTask, bool>>>(),
                It.IsAny<Func<IQueryable<RepairRequestTask>, IOrderedQueryable<RepairRequestTask>>>(),
                It.IsAny<Func<IQueryable<RepairRequestTask>, IIncludableQueryable<RepairRequestTask, object>>>()
            )).ReturnsAsync(taskDto);

            // Act
            var result = await _service.GetRepairRequestTaskByIdAsync(taskId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(taskId, result.RepairRequestTaskId);
            _cacheService.Verify(c => c.SetAsync($"repair_request_task:{taskId}", taskDto, TimeSpan.FromMinutes(30)), Times.Once);
        }

        [Fact]
        public async Task GetRepairRequestTaskByIdAsync_Throws_WhenNotFound()
        {
            // Arrange
            _taskRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequestTask, RepairRequestTaskDto>>>(),
                It.IsAny<Expression<Func<RepairRequestTask, bool>>>(),
                It.IsAny<Func<IQueryable<RepairRequestTask>, IOrderedQueryable<RepairRequestTask>>>(),
                It.IsAny<Func<IQueryable<RepairRequestTask>, IIncludableQueryable<RepairRequestTask, object>>>()
            )).ReturnsAsync((RepairRequestTaskDto)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.GetRepairRequestTaskByIdAsync(999));
            Assert.Contains("Nhiệm vụ sửa chữa không tồn tại", ex.Message);
        }

        #endregion

        #region GetRepairRequestTasksByRepairRequestIdAsync Tests

        [Fact]
        public async Task GetRepairRequestTasksByRepairRequestIdAsync_Success()
        {
            // Arrange
            var requestId = 1;
            var tasks = new List<RepairRequestTaskDto>
            {
                new RepairRequestTaskDto { RepairRequestTaskId = 1, TaskName = "Task 1" },
                new RepairRequestTaskDto { RepairRequestTaskId = 2, TaskName = "Task 2" }
            };

            _requestRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(true);

            _taskRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<RepairRequestTask, RepairRequestTaskDto>>>(),
                It.IsAny<Expression<Func<RepairRequestTask, bool>>>(),
                It.IsAny<Func<IQueryable<RepairRequestTask>, IOrderedQueryable<RepairRequestTask>>>(),
                It.IsAny<Func<IQueryable<RepairRequestTask>, IIncludableQueryable<RepairRequestTask, object>>>()
            )).ReturnsAsync(tasks);

            // Act
            var result = await _service.GetRepairRequestTasksByRepairRequestIdAsync(requestId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetRepairRequestTasksByRepairRequestIdAsync_Throws_WhenRepairRequestNotFound()
        {
            // Arrange
            _requestRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.GetRepairRequestTasksByRepairRequestIdAsync(999));
            Assert.Contains("Yêu cầu sửa chữa không tồn tại", ex.Message);
        }

        #endregion

        #region UpdateRepairRequestTasksStatusAsync Tests

        [Fact]
        public async Task UpdateRepairRequestTasksStatusAsync_Success()
        {
            // Arrange
            var userId = 1;
            var requestId = 1;
            var tasks = new List<RepairRequestTask>
            {
                new RepairRequestTask
                {
                    RepairRequestTaskId = 1,
                    TaskName = "Task 1",
                    Status = TaskCompletionStatus.Pending
                },
                new RepairRequestTask
                {
                    RepairRequestTaskId = 2,
                    TaskName = "Task 2",
                    Status = TaskCompletionStatus.Pending
                }
            };

            var updateDtos = new List<RequestTaskStatusUpdateDto>
            {
                new RequestTaskStatusUpdateDto
                {
                    RepairRequestTaskId = 1,
                    Status = TaskCompletionStatus.Completed,
                    TechnicianNote = "Done",
                    InspectionResult = "Good"
                },
                new RequestTaskStatusUpdateDto
                {
                    RepairRequestTaskId = 2,
                    Status = TaskCompletionStatus.Completed,
                    TechnicianNote = "Done",
                    InspectionResult = "Good"
                }
            };

            var repairRequest = new RepairRequest
            {
                RepairRequestId = requestId,
                RepairRequestTasks = tasks
            };

            _userContext.Setup(u => u.CurrentUserId).Returns(userId);

            _requestRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(repairRequest);

            // Act
            var result = await _service.UpdateRepairRequestTasksStatusAsync(requestId, updateDtos);

            // Assert
            Assert.Equal("Cập nhật trạng thái nhiệm vụ thành công.", result);
            Assert.All(tasks, t =>
            {
                Assert.Equal(TaskCompletionStatus.Completed, t.Status);
                Assert.Equal(userId, t.CompletedByUserId);
                Assert.NotNull(t.CompletedAt);
            });
            _taskRepo.Verify(r => r.UpdateAsync(It.IsAny<RepairRequestTask>()), Times.Exactly(2));
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateRepairRequestTasksStatusAsync_Throws_WhenRepairRequestNotFound()
        {
            // Arrange
            var updateDtos = new List<RequestTaskStatusUpdateDto>();

            _requestRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync((RepairRequest)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.UpdateRepairRequestTasksStatusAsync(999, updateDtos));
            Assert.Contains("Không tìm thấy yêu cầu sửa chữa", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateRepairRequestTasksStatusAsync_Throws_WhenNoTasksProvided()
        {
            // Arrange
            var requestId = 1;
            var repairRequest = new RepairRequest
            {
                RepairRequestId = requestId,
                RepairRequestTasks = new List<RepairRequestTask>
                {
                    new RepairRequestTask { RepairRequestTaskId = 1 }
                }
            };

            _requestRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(repairRequest);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.UpdateRepairRequestTasksStatusAsync(requestId, null));
            Assert.Contains("Chưa có công việc nào được cập nhật", ex.Message);
        }

        [Fact]
        public async Task UpdateRepairRequestTasksStatusAsync_Throws_WhenRepairRequestHasNoTasks()
        {
            // Arrange
            var requestId = 1;
            var updateDtos = new List<RequestTaskStatusUpdateDto>
            {
                new RequestTaskStatusUpdateDto { RepairRequestTaskId = 1, Status = TaskCompletionStatus.Completed }
            };

            var repairRequest = new RepairRequest
            {
                RepairRequestId = requestId,
                RepairRequestTasks = new List<RepairRequestTask>() // Empty
            };

            _requestRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(repairRequest);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.UpdateRepairRequestTasksStatusAsync(requestId, updateDtos));
            Assert.Contains("không có nhiệm vụ nào", ex.Message);
        }

        [Fact]
        public async Task UpdateRepairRequestTasksStatusAsync_Throws_WhenMissingTasks()
        {
            // Arrange
            var requestId = 1;
            var tasks = new List<RepairRequestTask>
            {
                new RepairRequestTask { RepairRequestTaskId = 1, TaskName = "Task 1" },
                new RepairRequestTask { RepairRequestTaskId = 2, TaskName = "Task 2" }
            };

            var updateDtos = new List<RequestTaskStatusUpdateDto>
            {
                new RequestTaskStatusUpdateDto
                {
                    RepairRequestTaskId = 1,
                    Status = TaskCompletionStatus.Completed
                }
                // Missing task 2
            };

            var repairRequest = new RepairRequest
            {
                RepairRequestId = requestId,
                RepairRequestTasks = tasks
            };

            _requestRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(repairRequest);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.UpdateRepairRequestTasksStatusAsync(requestId, updateDtos));
            Assert.Contains("Chưa cập nhật đủ tất cả nhiệm vụ", ex.Message);
            Assert.Contains("Task 2", ex.Message);
        }

        [Fact]
        public async Task UpdateRepairRequestTasksStatusAsync_Throws_WhenInvalidTaskIds()
        {
            // Arrange
            var requestId = 1;
            var tasks = new List<RepairRequestTask>
            {
                new RepairRequestTask { RepairRequestTaskId = 1, TaskName = "Task 1" }
            };

            var updateDtos = new List<RequestTaskStatusUpdateDto>
            {
                new RequestTaskStatusUpdateDto
                {
                    RepairRequestTaskId = 1,
                    Status = TaskCompletionStatus.Completed
                },
                new RequestTaskStatusUpdateDto
                {
                    RepairRequestTaskId = 999, // Invalid ID
                    Status = TaskCompletionStatus.Completed
                }
            };

            var repairRequest = new RepairRequest
            {
                RepairRequestId = requestId,
                RepairRequestTasks = tasks
            };

            _requestRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(repairRequest);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.UpdateRepairRequestTasksStatusAsync(requestId, updateDtos));
            Assert.Contains("không thuộc yêu cầu sửa chữa này", ex.Message);
            Assert.Contains("999", ex.Message);
        }

        [Fact]
        public async Task UpdateRepairRequestTasksStatusAsync_Throws_WhenTasksNotCompleted()
        {
            // Arrange
            var requestId = 1;
            var tasks = new List<RepairRequestTask>
            {
                new RepairRequestTask { RepairRequestTaskId = 1, TaskName = "Task 1" },
                new RepairRequestTask { RepairRequestTaskId = 2, TaskName = "Task 2" }
            };

            var updateDtos = new List<RequestTaskStatusUpdateDto>
            {
                new RequestTaskStatusUpdateDto
                {
                    RepairRequestTaskId = 1,
                    Status = TaskCompletionStatus.Completed
                },
                new RequestTaskStatusUpdateDto
                {
                    RepairRequestTaskId = 2,
                    Status = TaskCompletionStatus.Pending // Not completed
                }
            };

            var repairRequest = new RepairRequest
            {
                RepairRequestId = requestId,
                RepairRequestTasks = tasks
            };

            _requestRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(repairRequest);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.UpdateRepairRequestTasksStatusAsync(requestId, updateDtos));
            Assert.Contains("Tất cả nhiệm vụ phải được hoàn thành", ex.Message);
            Assert.Contains("Task 2", ex.Message);
        }

        #endregion
    }
}