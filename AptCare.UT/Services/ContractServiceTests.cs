using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.ContractDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Implements;
using AptCare.Service.Services.Interfaces;
using AptCare.Service.Services.Interfaces.IS3File;
using AutoMapper;
using Microsoft.AspNetCore.Http;
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
    public class ContractServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _uow = new();
        private readonly Mock<IGenericRepository<Contract>> _contractRepo = new();
        private readonly Mock<IGenericRepository<RepairRequest>> _repairRequestRepo = new();
        private readonly Mock<IGenericRepository<InspectionReport>> _inspectionReportRepo = new();
        private readonly Mock<IGenericRepository<Media>> _mediaRepo = new();
        private readonly Mock<IS3FileService> _s3FileService = new();
        private readonly Mock<IUserContext> _userContext = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<ContractService>> _logger = new();
        private readonly Mock<IRedisCacheService> _cacheService = new();

        private readonly ContractService _service;

        public ContractServiceTests()
        {
            _uow.Setup(u => u.GetRepository<Contract>()).Returns(_contractRepo.Object);
            _uow.Setup(u => u.GetRepository<RepairRequest>()).Returns(_repairRequestRepo.Object);
            _uow.Setup(u => u.GetRepository<InspectionReport>()).Returns(_inspectionReportRepo.Object);
            _uow.Setup(u => u.GetRepository<Media>()).Returns(_mediaRepo.Object);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            _cacheService.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>())).Returns(Task.CompletedTask);
            _cacheService.Setup(c => c.GetAsync<ContractDto>(It.IsAny<string>()))
                .ReturnsAsync((ContractDto)null);

            _cacheService.Setup(c => c.GetAsync<IEnumerable<ContractDto>>(It.IsAny<string>()))
                .ReturnsAsync((IEnumerable<ContractDto>)null);

            _cacheService.Setup(c => c.GetAsync<IPaginate<ContractDto>>(It.IsAny<string>()))
                .ReturnsAsync((IPaginate<ContractDto>)null);

            _service = new ContractService(
                _uow.Object,
                _logger.Object,
                _mapper.Object,
                _s3FileService.Object,
                _userContext.Object,
                _cacheService.Object
            );
        }

        #region CreateContractAsync Tests

        [Fact]
        public async Task CreateContractAsync_Success_CreatesContract()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("contract.pdf");
            mockFile.Setup(f => f.ContentType).Returns("application/pdf");
            mockFile.Setup(f => f.Length).Returns(1024);

            var dto = new ContractCreateDto
            {
                RepairRequestId = 1,
                ContractorName = "ABC Contractor",
                ContractCode = "CT-001",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(3),
                Amount = 50000000,
                Description = "Major repair contract",
                ContractFile = mockFile.Object
            };

            var repairRequest = new RepairRequest
            {
                RepairRequestId = 1,
                Appointments = new List<Appointment>
                {
                    new Appointment
                    {
                        InspectionReports = new List<InspectionReport>
                        {
                            new InspectionReport
                            {
                                SolutionType = SolutionType.Outsource,
                                Status = ReportStatus.Approved,
                                CreatedAt = DateTime.Now
                            }
                        }
                    }
                }
            };

            _repairRequestRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(repairRequest);

            _inspectionReportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<InspectionReport, bool>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IOrderedQueryable<InspectionReport>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IIncludableQueryable<InspectionReport, object>>>()
            )).ReturnsAsync(repairRequest.Appointments.First().InspectionReports.First());

            _contractRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Contract, bool>>>(),
                It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()
            )).ReturnsAsync(false);

            _s3FileService.Setup(s => s.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
                .ReturnsAsync("s3://bucket/contract.pdf");

            Contract insertedContract = null;
            //_contractRepo.Setup(r => r.InsertAsync(It.IsAny<Contract>()))
            //    .Callback<Contract>(c =>
            //    {
            //        c.ContractId = 1;
            //        insertedContract = c;
            //    })
            //    .Returns(Task.CompletedTask);

            var contractDto = new ContractDto
            {
                ContractId = 1,
                ContractCode = dto.ContractCode
            };

            _mapper.Setup(m => m.Map<ContractDto>(It.IsAny<Contract>())).Returns(contractDto);
            _mapper.Setup(m => m.Map<MediaDto>(It.IsAny<Media>())).Returns(new MediaDto());

            // Act
            var result = await _service.CreateContractAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(insertedContract);
            Assert.Equal(dto.ContractorName, insertedContract.ContractorName);
            Assert.Equal(dto.ContractCode, insertedContract.ContractCode);
            Assert.Equal(ActiveStatus.Active, insertedContract.Status);
            _contractRepo.Verify(r => r.InsertAsync(It.IsAny<Contract>()), Times.Once);
            _mediaRepo.Verify(r => r.InsertAsync(It.IsAny<Media>()), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateContractAsync_Throws_WhenRepairRequestNotFound()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("contract.pdf");
            mockFile.Setup(f => f.ContentType).Returns("application/pdf");
            mockFile.Setup(f => f.Length).Returns(1024);

            var dto = new ContractCreateDto
            {
                RepairRequestId = 999,
                ContractCode = "CT-001",
                ContractFile = mockFile.Object
            };

            _repairRequestRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync((RepairRequest)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.CreateContractAsync(dto));
            Assert.Contains("không tồn tại", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateContractAsync_Throws_WhenCannotCreateContract()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("contract.pdf");
            mockFile.Setup(f => f.ContentType).Returns("application/pdf");
            mockFile.Setup(f => f.Length).Returns(1024);

            var dto = new ContractCreateDto
            {
                RepairRequestId = 1,
                ContractCode = "CT-001",
                ContractFile = mockFile.Object
            };

            var repairRequest = new RepairRequest
            {
                RepairRequestId = 1,
                Appointments = new List<Appointment>()
            };

            _repairRequestRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(repairRequest);

            _inspectionReportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<InspectionReport, bool>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IOrderedQueryable<InspectionReport>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IIncludableQueryable<InspectionReport, object>>>()
            )).ReturnsAsync((InspectionReport)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.CreateContractAsync(dto));
            Assert.Contains("Không thể tạo hợp đồng", ex.Message);
        }

        [Fact]
        public async Task CreateContractAsync_Throws_WhenContractCodeExists()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("contract.pdf");
            mockFile.Setup(f => f.ContentType).Returns("application/pdf");
            mockFile.Setup(f => f.Length).Returns(1024);

            var dto = new ContractCreateDto
            {
                RepairRequestId = 1,
                ContractCode = "CT-001",
                ContractFile = mockFile.Object
            };

            var repairRequest = new RepairRequest { RepairRequestId = 1, Appointments = new List<Appointment>() };

            _repairRequestRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(repairRequest);

            var inspectionReport = new InspectionReport
            {
                SolutionType = SolutionType.Outsource,
                Status = ReportStatus.Approved
            };

            _inspectionReportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<InspectionReport, bool>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IOrderedQueryable<InspectionReport>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IIncludableQueryable<InspectionReport, object>>>()
            )).ReturnsAsync(inspectionReport);

            _contractRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Contract, bool>>>(),
                It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()
            )).ReturnsAsync(true); // Code already exists

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.CreateContractAsync(dto));
            Assert.Contains("đã tồn tại", ex.Message);
        }

        [Fact]
        public async Task CreateContractAsync_Throws_WhenFileNotPdf()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("contract.docx");
            mockFile.Setup(f => f.ContentType).Returns("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
            mockFile.Setup(f => f.Length).Returns(1024);

            var dto = new ContractCreateDto
            {
                RepairRequestId = 1,
                ContractCode = "CT-001",
                ContractFile = mockFile.Object
            };

            var repairRequest = new RepairRequest { RepairRequestId = 1, Appointments = new List<Appointment>() };

            _repairRequestRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RepairRequest, bool>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IOrderedQueryable<RepairRequest>>>(),
                It.IsAny<Func<IQueryable<RepairRequest>, IIncludableQueryable<RepairRequest, object>>>()
            )).ReturnsAsync(repairRequest);

            var inspectionReport = new InspectionReport
            {
                SolutionType = SolutionType.Outsource,
                Status = ReportStatus.Approved
            };

            _inspectionReportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<InspectionReport, bool>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IOrderedQueryable<InspectionReport>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IIncludableQueryable<InspectionReport, object>>>()
            )).ReturnsAsync(inspectionReport);

            _contractRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Contract, bool>>>(),
                It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()
            )).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.CreateContractAsync(dto));
            Assert.Contains("PDF", ex.Message);
        }

        #endregion

        #region CanCreateContractAsync Tests

        [Fact]
        public async Task CanCreateContractAsync_ReturnsTrue_WhenOutsourceSolutionApproved()
        {
            // Arrange
            var repairRequestId = 1;
            var inspectionReport = new InspectionReport
            {
                SolutionType = SolutionType.Outsource,
                Status = ReportStatus.Approved,
                CreatedAt = DateTime.Now,
                Appointment = new Appointment()
            };

            _inspectionReportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<InspectionReport, bool>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IOrderedQueryable<InspectionReport>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IIncludableQueryable<InspectionReport, object>>>()
            )).ReturnsAsync(inspectionReport);

            // Act
            var result = await _service.CanCreateContractAsync(repairRequestId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CanCreateContractAsync_ReturnsFalse_WhenNoApprovedInspectionReport()
        {
            // Arrange
            var repairRequestId = 1;

            _inspectionReportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<InspectionReport, bool>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IOrderedQueryable<InspectionReport>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IIncludableQueryable<InspectionReport, object>>>()
            )).ReturnsAsync((InspectionReport)null);

            // Act
            var result = await _service.CanCreateContractAsync(repairRequestId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanCreateContractAsync_ReturnsFalse_WhenSolutionNotOutsource()
        {
            // Arrange
            var repairRequestId = 1;
            var inspectionReport = new InspectionReport
            {
                SolutionType = SolutionType.Replacement, // Not Outsource
                Status = ReportStatus.Approved,
                Appointment = new Appointment()
            };

            _inspectionReportRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<InspectionReport, bool>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IOrderedQueryable<InspectionReport>>>(),
                It.IsAny<Func<IQueryable<InspectionReport>, IIncludableQueryable<InspectionReport, object>>>()
            )).ReturnsAsync(inspectionReport);

            // Act
            var result = await _service.CanCreateContractAsync(repairRequestId);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetContractByIdAsync Tests

        [Fact]
        public async Task GetContractByIdAsync_Success_ReturnsContract()
        {
            // Arrange
            var contractId = 1;
            var contract = new Contract
            {
                ContractId = contractId,
                ContractCode = "CT-001",
                ContractorName = "ABC Contractor",
                RepairRequest = new RepairRequest()
            };

            var contractDto = new ContractDto
            {
                ContractId = contractId,
                ContractCode = "CT-001"
            };

            _contractRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Contract, bool>>>(),
                It.IsAny<Func<IQueryable<Contract>, IOrderedQueryable<Contract>>>(),
                It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()
            )).ReturnsAsync(contract);

            _mapper.Setup(m => m.Map<ContractDto>(contract)).Returns(contractDto);
            _mapper.Setup(m => m.Map<MediaDto>(It.IsAny<Media>())).Returns(new MediaDto());

            _mediaRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Media, bool>>>(),
                It.IsAny<Func<IQueryable<Media>, IOrderedQueryable<Media>>>(),
                It.IsAny<Func<IQueryable<Media>, IIncludableQueryable<Media, object>>>()
            )).ReturnsAsync(new Media());

            // Act
            var result = await _service.GetContractByIdAsync(contractId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(contractId, result.ContractId);
        }

        [Fact]
        public async Task GetContractByIdAsync_Throws_WhenNotFound()
        {
            // Arrange
            var contractId = 999;

            _contractRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Contract, bool>>>(),
                It.IsAny<Func<IQueryable<Contract>, IOrderedQueryable<Contract>>>(),
                It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()
            )).ReturnsAsync((Contract)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.GetContractByIdAsync(contractId));
            Assert.Contains("không tồn tại", ex.Message);
        }

        #endregion

        #region UpdateContractAsync Tests

        [Fact]
        public async Task UpdateContractAsync_Success_UpdatesContract()
        {
            // Arrange
            var contractId = 1;
            var dto = new ContractUpdateDto
            {
                ContractorName = "Updated Contractor",
                Description = "Updated description",
                Amount = 60000000
            };

            var contract = new Contract
            {
                ContractId = contractId,
                ContractorName = "Old Contractor",
                Status = ActiveStatus.Active,
                RepairRequestId = 1
            };

            _contractRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Contract, bool>>>(),
                It.IsAny<Func<IQueryable<Contract>, IOrderedQueryable<Contract>>>(),
                It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()
            )).ReturnsAsync(contract);

            // Act
            var result = await _service.UpdateContractAsync(contractId, dto);

            // Assert
            Assert.Contains("thành công", result);
            Assert.Equal(dto.ContractorName, contract.ContractorName);
            Assert.Equal(dto.Amount, contract.Amount);
            _contractRepo.Verify(r => r.UpdateAsync(contract), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateContractAsync_Throws_WhenContractInactive()
        {
            // Arrange
            var contractId = 1;
            var dto = new ContractUpdateDto
            {
                ContractorName = "Updated"
            };

            var contract = new Contract
            {
                ContractId = contractId,
                Status = ActiveStatus.Inactive // Inactive
            };

            _contractRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Contract, bool>>>(),
                It.IsAny<Func<IQueryable<Contract>, IOrderedQueryable<Contract>>>(),
                It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()
            )).ReturnsAsync(contract);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.UpdateContractAsync(contractId, dto));
            Assert.Contains("vô hiệu hóa", ex.Message);
        }

        #endregion

        #region InactivateContractAsync Tests

        [Fact]
        public async Task InactivateContractAsync_Success_InactivatesContract()
        {
            // Arrange
            var contractId = 1;
            var contract = new Contract
            {
                ContractId = contractId,
                Status = ActiveStatus.Active
            };

            _contractRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Contract, bool>>>(),
                It.IsAny<Func<IQueryable<Contract>, IOrderedQueryable<Contract>>>(),
                It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()
            )).ReturnsAsync(contract);

            // Act
            var result = await _service.InactivateContractAsync(contractId);

            // Assert
            Assert.Contains("thành công", result);
            Assert.Equal(ActiveStatus.Inactive, contract.Status);
            _contractRepo.Verify(r => r.UpdateAsync(contract), Times.Once);
        }

        [Fact]
        public async Task InactivateContractAsync_Throws_WhenAlreadyInactive()
        {
            // Arrange
            var contractId = 1;
            var contract = new Contract
            {
                ContractId = contractId,
                Status = ActiveStatus.Inactive
            };

            _contractRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Contract, bool>>>(),
                It.IsAny<Func<IQueryable<Contract>, IOrderedQueryable<Contract>>>(),
                It.IsAny<Func<IQueryable<Contract>, IIncludableQueryable<Contract, object>>>()
            )).ReturnsAsync(contract);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.InactivateContractAsync(contractId));
            Assert.Contains("đã bị vô hiệu hóa", ex.Message);
        }

        #endregion
    }
}