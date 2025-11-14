using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.TransactionDtos;

namespace AptCare.Service.Services.Interfaces
{
    public interface ITransactionService
    {
        Task<TransactionDto> CreateExpenseDepositAsync(TransactionExpenseDepositDto dto);
        Task<TransactionDto> CreateExpenseFinalAsync(TransactionExpenseFinalDto dto);
        Task<TransactionDto> CreateExpenseInternalAsync(TransactionExpenseInternalDto dto);

        // ========== INCOME (THU) ==========

        /// <summary>
        /// Tạo link PayOS để thu tiền từ cư dân
        /// Chỉ dùng cho IsChargeable = true
        /// </summary>
        Task<string> CreateIncomePaymentLinkAsync(int invoiceId);

        /// <summary>
        /// Tạo giao dịch THU tiền mặt từ cư dân
        /// Upload file biên lai (optional)
        /// </summary>
        Task<TransactionDto> CreateIncomeCashAsync(TransactionIncomeCashDto dto);

        // ========== QUERY ==========

        /// <summary>
        /// Lấy danh sách giao dịch theo Invoice
        /// </summary>
        Task<IEnumerable<TransactionDto>> GetTransactionsByInvoiceIdAsync(int invoiceId);

        /// <summary>
        /// Lấy chi tiết 1 giao dịch
        /// </summary>
        Task<TransactionDto> GetTransactionByIdAsync(int transactionId);

        /// <summary>
        /// Lấy danh sách giao dịch có phân trang và lọc
        /// </summary>
        Task<IPaginate<TransactionDto>> GetPaginateTransactionsAsync(TransactionFilterDto filterDto);

        /// <summary>
        /// Lấy tổng thu chi theo Invoice
        /// </summary>
        Task<(decimal TotalIncome, decimal TotalExpense)> GetInvoiceSummaryAsync(int invoiceId);
    }
}