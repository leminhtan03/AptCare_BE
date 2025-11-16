using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.TransactionEnum;
using AptCare.Repository.Paginate;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.PayOSDto;
using AptCare.Service.Dtos.TransactionDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Implements;
using AptCare.Service.Services.Interfaces;
using AptCare.Service.Services.Interfaces.IS3File;
using AptCare.Service.Services.PayOSService;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace AptCare.UT.Services
{
    public class TransactionServiceTests
    {
        private readonly Mock<IUnitOfWork<AptCareSystemDBContext>> _uow = new();
        private readonly Mock<IGenericRepository<Transaction>> _transactionRepo = new();
        private readonly Mock<IGenericRepository<Invoice>> _invoiceRepo = new();
        private readonly Mock<IGenericRepository<Media>> _mediaRepo = new();
        private readonly Mock<IGenericRepository<User>> _userRepo = new();
        private readonly Mock<IUserContext> _userContext = new();
        private readonly Mock<IS3FileService> _s3FileService = new();
        private readonly Mock<IPayOSClient> _payOSClient = new();
        private readonly Mock<IOptions<PayOSOptions>> _payOSOptions = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<TransactionService>> _logger = new();

        private readonly TransactionService _service;

        public TransactionServiceTests()
        {
            var options = new PayOSOptions
            {
                BaseUrl = "https://test.payos.vn",
                ClientId = "test-client",
                ApiKey = "test-key",
                ChecksumKey = "test-checksum",
                ReturnUrl = "https://test.com/return"
            };

            _payOSOptions.Setup(o => o.Value).Returns(options);

            _uow.Setup(u => u.GetRepository<Transaction>()).Returns(_transactionRepo.Object);
            _uow.Setup(u => u.GetRepository<Invoice>()).Returns(_invoiceRepo.Object);
            _uow.Setup(u => u.GetRepository<Media>()).Returns(_mediaRepo.Object);
            _uow.Setup(u => u.GetRepository<User>()).Returns(_userRepo.Object);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            _service = new TransactionService(
                _uow.Object,
                _logger.Object,
                _mapper.Object,
                _userContext.Object,
                _s3FileService.Object,
                _payOSClient.Object,
                _payOSOptions.Object
            );
        }

        #region CreateExpenseDepositAsync Tests

        [Fact]
        public async Task CreateExpenseDepositAsync_Success_CreatesDepositTransaction()
        {
            // Arrange
            var userId = 1;
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("invoice.pdf");
            mockFile.Setup(f => f.ContentType).Returns("application/pdf");
            mockFile.Setup(f => f.Length).Returns(1024);

            var dto = new TransactionExpenseDepositDto
            {
                InvoiceId = 1,
                Amount = 5000000,
                Note = "Deposit 50%",
                ContractorInvoiceFile = mockFile.Object
            };

            var invoice = new Invoice
            {
                InvoiceId = 1,
                Type = InvoiceType.ExternalContractor,
                TotalAmount = 10000000,
                Status = InvoiceStatus.Draft,
                RepairRequest = new RepairRequest()
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(userId);

            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync(invoice);

            _transactionRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Transaction, bool>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IIncludableQueryable<Transaction, object>>>()
            )).ReturnsAsync(new List<Transaction>());

            _s3FileService.Setup(s => s.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
                .ReturnsAsync("s3://bucket/invoice.pdf");

            Transaction insertedTransaction = null;
            _transactionRepo.Setup(r => r.InsertAsync(It.IsAny<Transaction>()))
                .Callback<Transaction>(t =>
                {
                    t.TransactionId = 1;
                    insertedTransaction = t;
                })
                .Returns(Task.CompletedTask);

            _userRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, string>>>(),
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync("John Doe");

            var transactionDto = new TransactionDto
            {
                TransactionId = 1,
                Amount = dto.Amount,
                Direction = TransactionDirection.Expense
            };

            _mapper.Setup(m => m.Map<Transaction>(dto)).Returns(new Transaction
            {
                InvoiceId = dto.InvoiceId,
                Amount = dto.Amount,
                Direction = TransactionDirection.Expense,
                TransactionType = TransactionType.Payment,
                Status = TransactionStatus.Success,
                Provider = PaymentProvider.UnKnow
            });

            _mapper.Setup(m => m.Map<TransactionDto>(It.IsAny<Transaction>())).Returns(transactionDto);
            _mapper.Setup(m => m.Map<MediaDto>(It.IsAny<Media>())).Returns(new MediaDto());

            // Act
            var result = await _service.CreateExpenseDepositAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(insertedTransaction);
            Assert.Equal(userId, insertedTransaction.UserId);
            Assert.Contains("Đặt cọc lần 1", insertedTransaction.Description);
            Assert.Equal(InvoiceStatus.PartiallyPaid, invoice.Status);
            _transactionRepo.Verify(r => r.InsertAsync(It.IsAny<Transaction>()), Times.Once);
            _mediaRepo.Verify(r => r.InsertAsync(It.IsAny<Media>()), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateExpenseDepositAsync_Throws_WhenInvoiceNotFound()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("invoice.pdf");
            mockFile.Setup(f => f.ContentType).Returns("application/pdf");
            mockFile.Setup(f => f.Length).Returns(1024);

            var dto = new TransactionExpenseDepositDto
            {
                InvoiceId = 999,
                Amount = 1000,
                ContractorInvoiceFile = mockFile.Object
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync((Invoice)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.CreateExpenseDepositAsync(dto));
            Assert.Contains("Không tìm thấy hóa đơn", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateExpenseDepositAsync_Throws_WhenNotExternalContractor()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("invoice.pdf");
            mockFile.Setup(f => f.ContentType).Returns("application/pdf");
            mockFile.Setup(f => f.Length).Returns(1024);

            var dto = new TransactionExpenseDepositDto
            {
                InvoiceId = 1,
                Amount = 1000,
                ContractorInvoiceFile = mockFile.Object
            };

            var invoice = new Invoice
            {
                InvoiceId = 1,
                Type = InvoiceType.InternalRepair, // Not ExternalContractor
                TotalAmount = 10000
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync(invoice);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.CreateExpenseDepositAsync(dto));
            Assert.Contains("nhà thầu", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateExpenseDepositAsync_Throws_WhenAmountExceedsTotal()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("invoice.pdf");
            mockFile.Setup(f => f.ContentType).Returns("application/pdf");
            mockFile.Setup(f => f.Length).Returns(1024);

            var dto = new TransactionExpenseDepositDto
            {
                InvoiceId = 1,
                Amount = 6000000,
                ContractorInvoiceFile = mockFile.Object
            };

            var invoice = new Invoice
            {
                InvoiceId = 1,
                Type = InvoiceType.ExternalContractor,
                TotalAmount = 10000000,
                Status = InvoiceStatus.Draft
            };

            var existingTransactions = new List<Transaction>
            {
                new Transaction { Amount = 5000000, Status = TransactionStatus.Success }
            };

            _userContext.SetupGet(u => u.CurrentUserId).Returns(1);

            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync(invoice);

            _transactionRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Transaction, bool>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IIncludableQueryable<Transaction, object>>>()
            )).ReturnsAsync(existingTransactions);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.CreateExpenseDepositAsync(dto));
            Assert.Contains("vượt quá giá trị hóa đơn", ex.Message);
        }

        #endregion

        #region CreateIncomePaymentLinkAsync Tests

        [Fact]
        public async Task CreateIncomePaymentLinkAsync_Success_ReturnsPaymentLink()
        {
            // Arrange
            var invoiceId = 1;
            var invoice = new Invoice
            {
                InvoiceId = invoiceId,
                IsChargeable = true,
                TotalAmount = 5000000,
                Status = InvoiceStatus.Draft,
                RepairRequest = new RepairRequest
                {
                    UserId = 1,
                    RequestTrackings = new List<RequestTracking>
                    {
                        new RequestTracking
                        {
                            Status = RequestStatus.Completed,
                            UpdatedAt = DateTime.Now
                        }
                    }
                }
            };

            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync(invoice);

            _transactionRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Transaction, bool>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IIncludableQueryable<Transaction, object>>>()
            )).ReturnsAsync(new List<Transaction>());

            _payOSClient.Setup(p => p.CreatePaymentLinkAsync(
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).ReturnsAsync(("https://payos.com/pay/123", "link-id-123"));

            // Act
            var result = await _service.CreateIncomePaymentLinkAsync(invoiceId);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("https://payos.com", result);
            Assert.Equal(InvoiceStatus.AwaitingPayment, invoice.Status);
            _transactionRepo.Verify(r => r.InsertAsync(It.IsAny<Transaction>()), Times.Once);
            _payOSClient.Verify(p => p.CreatePaymentLinkAsync(
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ), Times.Once);
        }

        [Fact]
        public async Task CreateIncomePaymentLinkAsync_Throws_WhenNotChargeable()
        {
            // Arrange
            var invoiceId = 1;
            var invoice = new Invoice
            {
                InvoiceId = invoiceId,
                IsChargeable = false, // Not chargeable
                TotalAmount = 5000000
            };

            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync(invoice);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.CreateIncomePaymentLinkAsync(invoiceId));
            Assert.Contains("không thu phí", ex.Message);
        }

        #endregion

        #region GetTransactionByIdAsync Tests

        [Fact]
        public async Task GetTransactionByIdAsync_Success_ReturnsTransaction()
        {
            // Arrange
            var transactionId = 1;
            var transaction = new Transaction
            {
                TransactionId = transactionId,
                Amount = 1000000,
                User = new User
                {
                    UserId = 1,
                    FirstName = "John",
                    LastName = "Doe"
                }
            };

            var transactionDto = new TransactionDto
            {
                TransactionId = transactionId,
                Amount = 1000000
            };

            _transactionRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Transaction, bool>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IIncludableQueryable<Transaction, object>>>()
            )).ReturnsAsync(transaction);

            _mapper.Setup(m => m.Map<TransactionDto>(transaction)).Returns(transactionDto);
            _mapper.Setup(m => m.Map<MediaDto>(It.IsAny<Media>())).Returns(new MediaDto());

            _mediaRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Media, bool>>>(),
                It.IsAny<Func<IQueryable<Media>, IOrderedQueryable<Media>>>(),
                It.IsAny<Func<IQueryable<Media>, IIncludableQueryable<Media, object>>>()
            )).ReturnsAsync((Media)null);

            // Act
            var result = await _service.GetTransactionByIdAsync(transactionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(transactionId, result.TransactionId);
            Assert.Equal("John Doe", result.UserFullName);
        }

        [Fact]
        public async Task GetTransactionByIdAsync_Throws_WhenNotFound()
        {
            // Arrange
            var transactionId = 999;

            _transactionRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Transaction, bool>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IIncludableQueryable<Transaction, object>>>()
            )).ReturnsAsync((Transaction)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
                _service.GetTransactionByIdAsync(transactionId));
            Assert.Contains("Không tìm thấy giao dịch", ex.Message);
        }

        #endregion

        #region GetInvoiceSummaryAsync Tests

        [Fact]
        public async Task GetInvoiceSummaryAsync_Success_ReturnsSummary()
        {
            // Arrange
            var invoiceId = 1;
            var incomeTransactions = new List<Transaction>
            {
                new Transaction { Amount = 3000000, Status = TransactionStatus.Success },
                new Transaction { Amount = 2000000, Status = TransactionStatus.Success }
            };

            var expenseTransactions = new List<Transaction>
            {
                new Transaction { Amount = 1000000, Status = TransactionStatus.Success }
            };

            _transactionRepo.SetupSequence(r => r.GetListAsync(
                It.IsAny<Expression<Func<Transaction, bool>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IIncludableQueryable<Transaction, object>>>()
            ))
            .ReturnsAsync(incomeTransactions)
            .ReturnsAsync(expenseTransactions);

            // Act
            var (totalIncome, totalExpense) = await _service.GetInvoiceSummaryAsync(invoiceId);

            // Assert
            Assert.Equal(5000000, totalIncome);
            Assert.Equal(1000000, totalExpense);
        }

        #endregion
    }
}