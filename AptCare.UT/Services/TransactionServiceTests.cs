using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.TransactionEnum;
using AptCare.Repository.Paginate;
using AptCare.Repository.Repositories;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.TransactionDtos;
using AptCare.Service.Services.Implements;
using AptCare.Service.Services.Interfaces;
using AptCare.Service.Services.Interfaces.IS3File;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PayOS;
using System.Linq.Expressions;
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
        private readonly Mock<IGenericRepository<Budget>> _budgetRepo = new();
        private readonly Mock<IUserContext> _userContext = new();
        private readonly Mock<IS3FileService> _s3FileService = new();
        private readonly Mock<ICloudinaryService> _cloudinaryService = new();
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
                ChecksumKey = "test-checksum"
            };

            _payOSOptions.Setup(o => o.Value).Returns(options);

            // ✅ Setup all repositories
            _uow.Setup(u => u.GetRepository<Transaction>()).Returns(_transactionRepo.Object);
            _uow.Setup(u => u.GetRepository<Invoice>()).Returns(_invoiceRepo.Object);
            _uow.Setup(u => u.GetRepository<Media>()).Returns(_mediaRepo.Object);
            _uow.Setup(u => u.GetRepository<User>()).Returns(_userRepo.Object);
            _uow.Setup(u => u.GetRepository<Budget>()).Returns(_budgetRepo.Object);
            
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _uow.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            // ✅ Đúng constructor
            _service = new TransactionService(
                _uow.Object,
                _logger.Object,
                _mapper.Object,
                _userContext.Object,
                _s3FileService.Object,
                _cloudinaryService.Object, // ✅ Không cast từ PayOSClient
                _payOSOptions.Object
            );
        }

        #region CreateIncomePaymentLinkAsync Tests

        [Fact]
        public async Task CreateIncomePaymentLinkAsync_Throws_WhenInvoiceNotFound()
        {
            // Arrange
            var invoiceId = 999;

            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync((Invoice)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateIncomePaymentLinkAsync(invoiceId));
            Assert.Contains("Không tìm thấy hóa đơn", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateIncomePaymentLinkAsync_Throws_WhenNotChargeable()
        {
            // Arrange
            var invoiceId = 1;
            var invoice = new Invoice
            {
                InvoiceId = invoiceId,
                IsChargeable = false,
                TotalAmount = 5000000
            };

            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync(invoice);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateIncomePaymentLinkAsync(invoiceId));
            Assert.Contains("không thu phí", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateIncomePaymentLinkAsync_Throws_WhenInvoiceCancelled()
        {
            // Arrange
            var invoiceId = 1;
            var invoice = new Invoice
            {
                InvoiceId = invoiceId,
                IsChargeable = true,
                Status = InvoiceStatus.Cancelled,
                TotalAmount = 5000000
            };

            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync(invoice);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateIncomePaymentLinkAsync(invoiceId));
            Assert.Contains("đã hủy", ex.Message);
        }

        [Fact]
        public async Task CreateIncomePaymentLinkAsync_Throws_WhenAmountZero()
        {
            // Arrange
            var invoiceId = 1;
            var invoice = new Invoice
            {
                InvoiceId = invoiceId,
                IsChargeable = true,
                TotalAmount = 0,
                Status = InvoiceStatus.Draft,
                RepairRequest = new RepairRequest()
            };

            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync(invoice);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateIncomePaymentLinkAsync(invoiceId));
            Assert.Contains("không có giá trị", ex.Message);
        }

        [Fact]
        public async Task CreateIncomePaymentLinkAsync_Throws_WhenAmountTooLow()
        {
            // Arrange
            var invoiceId = 1;
            var invoice = new Invoice
            {
                InvoiceId = invoiceId,
                IsChargeable = true,
                TotalAmount = 1500,
                Status = InvoiceStatus.Draft,
                RepairRequest = new RepairRequest()
            };

            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync(invoice);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateIncomePaymentLinkAsync(invoiceId));
            Assert.Contains("tối thiểu là 2,000", ex.Message);
        }

        [Fact]
        public async Task CreateIncomePaymentLinkAsync_Throws_WhenRepairNotCompleted()
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
                            Status = RequestStatus.InProgress,
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

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateIncomePaymentLinkAsync(invoiceId));
            Assert.Contains("chưa hoàn tất", ex.Message);
        }

        [Fact]
        public async Task CreateIncomePaymentLinkAsync_ReturnsExisting_WhenTransactionPending()
        {
            // Arrange
            var invoiceId = 1;
            var existingUrl = "https://payos.com/existing/123";
            
            var invoice = new Invoice
            {
                InvoiceId = invoiceId,
                IsChargeable = true,
                TotalAmount = 5000000,
                Status = InvoiceStatus.AwaitingPayment,
                InvoiceServices = new List<Repository.Entities.InvoiceService>(),
                InvoiceAccessories = new List<InvoiceAccessory>(),
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

            var existingTransaction = new Transaction
            {
                TransactionId = 1,
                InvoiceId = invoiceId,
                CheckoutUrl = existingUrl,
                Status = TransactionStatus.Pending,
                Provider = PaymentProvider.PayOS
            };

            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync(invoice);

            _transactionRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Transaction, bool>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IIncludableQueryable<Transaction, object>>>()
            )).ReturnsAsync(existingTransaction);

            // Act
            var result = await _service.CreateIncomePaymentLinkAsync(invoiceId);

            // Assert
            Assert.Equal(existingUrl, result);
            _transactionRepo.Verify(r => r.InsertAsync(It.IsAny<Transaction>()), Times.Never);
        }

        // ⚠️ Note: Test tạo PayOS link mới sẽ FAIL vì PayOSClient được tạo trong constructor
        // Để test được, cần refactor service để inject PayOSClient qua DI

        #endregion

        #region CreateIncomeCashAsync Tests

        [Fact]
        public async Task CreateIncomeCashAsync_Success_WithoutReceipt()
        {
            // Arrange
            var userId = 1;
            var dto = new TransactionIncomeCashDto
            {
                InvoiceId = 1,
                Note = "Thanh toán tiền mặt"
            };

            var invoice = new Invoice
            {
                InvoiceId = 1,
                IsChargeable = true,
                TotalAmount = 5000000,
                Status = InvoiceStatus.AwaitingPayment,
                RepairRequest = new RepairRequest
                {
                    UserId = userId,
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

            var budget = new Budget
            {
                BudgetId = 1,
                Amount = 10000000
            };

            var transaction = new Transaction
            {
                TransactionId = 1,
                InvoiceId = dto.InvoiceId,
                Amount = invoice.TotalAmount,
                UserId = userId,
                TransactionType = TransactionType.Cash,
                Status = TransactionStatus.Success,
                Provider = PaymentProvider.UnKnow,
                Direction = TransactionDirection.Income
            };

            _userContext.Setup(u => u.CurrentUserId).Returns(userId);

            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync(invoice);

            _budgetRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Budget, bool>>>(),
                It.IsAny<Func<IQueryable<Budget>, IOrderedQueryable<Budget>>>(),
                It.IsAny<Func<IQueryable<Budget>, IIncludableQueryable<Budget, object>>>()
            )).ReturnsAsync(budget);

            _mapper.Setup(m => m.Map<Transaction>(dto)).Returns(transaction);
            _mapper.Setup(m => m.Map<TransactionDto>(transaction))
                .Returns(new TransactionDto 
                { 
                    TransactionId = 1, 
                    Amount = 5000000 
                });

            _transactionRepo.Setup(r => r.InsertAsync(It.IsAny<Transaction>()))
                .Returns(Task.CompletedTask);

            _userRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, string>>>(),
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync("John Doe");

            // Act
            var result = await _service.CreateIncomeCashAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(InvoiceStatus.Paid, invoice.Status);
            Assert.Equal(15000000, budget.Amount); // 10M + 5M
            _transactionRepo.Verify(r => r.InsertAsync(It.IsAny<Transaction>()), Times.Once);
            _budgetRepo.Verify(r => r.UpdateAsync(budget), Times.Once);
            _uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateIncomeCashAsync_Success_WithPdfReceipt()
        {
            // Arrange
            var userId = 1;
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("receipt.pdf");
            mockFile.Setup(f => f.ContentType).Returns("application/pdf");
            mockFile.Setup(f => f.Length).Returns(1024);

            var dto = new TransactionIncomeCashDto
            {
                InvoiceId = 1,
                Note = "Thanh toán có biên lai",
                ReceiptFile = mockFile.Object
            };

            var invoice = new Invoice
            {
                InvoiceId = 1,
                IsChargeable = true,
                TotalAmount = 5000000,
                Status = InvoiceStatus.AwaitingPayment,
                RepairRequest = new RepairRequest
                {
                    UserId = userId,
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

            var budget = new Budget { BudgetId = 1, Amount = 10000000 };
            var transaction = new Transaction
            {
                TransactionId = 1,
                InvoiceId = dto.InvoiceId,
                Amount = invoice.TotalAmount,
                UserId = userId
            };

            _userContext.Setup(u => u.CurrentUserId).Returns(userId);
            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync(invoice);

            _budgetRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Budget, bool>>>(),
                It.IsAny<Func<IQueryable<Budget>, IOrderedQueryable<Budget>>>(),
                It.IsAny<Func<IQueryable<Budget>, IIncludableQueryable<Budget, object>>>()
            )).ReturnsAsync(budget);

            _mapper.Setup(m => m.Map<Transaction>(dto)).Returns(transaction);
            _mapper.Setup(m => m.Map<TransactionDto>(transaction))
                .Returns(new TransactionDto { TransactionId = 1 });
            _mapper.Setup(m => m.Map<MediaDto>(It.IsAny<Media>()))
                .Returns(new MediaDto());

            _s3FileService.Setup(s => s.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
                .ReturnsAsync("s3://bucket/receipt.pdf");

            _mediaRepo.Setup(r => r.InsertAsync(It.IsAny<Media>()))
                .Returns(Task.CompletedTask);

            _userRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<User, string>>>(),
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>>()
            )).ReturnsAsync("John Doe");

            // Act
            var result = await _service.CreateIncomeCashAsync(dto);

            // Assert
            Assert.NotNull(result);
            _s3FileService.Verify(s => s.UploadFileAsync(mockFile.Object, It.IsAny<string>()), Times.Once);
            _mediaRepo.Verify(r => r.InsertAsync(It.IsAny<Media>()), Times.Once);
        }

        [Fact]
        public async Task CreateIncomeCashAsync_Throws_WhenInvoiceNotChargeable()
        {
            // Arrange
            var dto = new TransactionIncomeCashDto { InvoiceId = 1 };
            var invoice = new Invoice
            {
                InvoiceId = 1,
                IsChargeable = false
            };

            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync(invoice);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateIncomeCashAsync(dto));
            Assert.Contains("không thu phí", ex.Message);
            _uow.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateIncomeCashAsync_Throws_WhenBudgetNotFound()
        {
            // Arrange
            var userId = 1;
            var dto = new TransactionIncomeCashDto { InvoiceId = 1 };
            var invoice = new Invoice
            {
                InvoiceId = 1,
                IsChargeable = true,
                TotalAmount = 5000000,
                Status = InvoiceStatus.AwaitingPayment,
                RepairRequest = new RepairRequest
                {
                    UserId = userId,
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

            var transaction = new Transaction
            {
                TransactionId = 1,
                InvoiceId = dto.InvoiceId,
                Amount = invoice.TotalAmount,
                UserId = userId
            };

            _userContext.Setup(u => u.CurrentUserId).Returns(userId);
            _invoiceRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IOrderedQueryable<Invoice>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IIncludableQueryable<Invoice, object>>>()
            )).ReturnsAsync(invoice);

            _mapper.Setup(m => m.Map<Transaction>(dto)).Returns(transaction);

            _budgetRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Budget, bool>>>(),
                It.IsAny<Func<IQueryable<Budget>, IOrderedQueryable<Budget>>>(),
                It.IsAny<Func<IQueryable<Budget>, IIncludableQueryable<Budget, object>>>()
            )).ReturnsAsync((Budget)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateIncomeCashAsync(dto));
            Assert.Contains("Không tìm thấy ngân sách", ex.Message);
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
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.GetTransactionByIdAsync(transactionId));
            Assert.Contains("Không tìm thấy giao dịch", ex.Message);
        }

        #endregion

        #region GetTransactionsByInvoiceIdAsync Tests

        [Fact]
        public async Task GetTransactionsByInvoiceIdAsync_Success_ReturnsTransactions()
        {
            // Arrange
            var invoiceId = 1;
            var transactions = new List<Transaction>
            {
                new Transaction
                {
                    TransactionId = 1,
                    InvoiceId = invoiceId,
                    Amount = 5000000,
                    User = new User { FirstName = "John", LastName = "Doe" }
                },
                new Transaction
                {
                    TransactionId = 2,
                    InvoiceId = invoiceId,
                    Amount = 3000000,
                    User = new User { FirstName = "Jane", LastName = "Smith" }
                }
            };

            _transactionRepo.Setup(r => r.GetListAsync(
                It.IsAny<Expression<Func<Transaction, bool>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>>>(),
                It.IsAny<Func<IQueryable<Transaction>, IIncludableQueryable<Transaction, object>>>()
            )).ReturnsAsync(transactions);

            _mapper.Setup(m => m.Map<TransactionDto>(It.IsAny<Transaction>()))
                .Returns((Transaction t) => new TransactionDto
                {
                    TransactionId = t.TransactionId,
                    Amount = t.Amount
                });

            _mediaRepo.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Media, bool>>>(),
                It.IsAny<Func<IQueryable<Media>, IOrderedQueryable<Media>>>(),
                It.IsAny<Func<IQueryable<Media>, IIncludableQueryable<Media, object>>>()
            )).ReturnsAsync((Media)null);

            // Act
            var result = await _service.GetTransactionsByInvoiceIdAsync(invoiceId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, t => t.UserFullName == "John Doe");
            Assert.Contains(result, t => t.UserFullName == "Jane Smith");
        }

        #endregion
    }
}