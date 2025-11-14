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
        Task<string> CreateIncomePaymentLinkAsync(int invoiceId);
        Task<TransactionDto> CreateIncomeCashAsync(TransactionIncomeCashDto dto);
        Task<IEnumerable<TransactionDto>> GetTransactionsByInvoiceIdAsync(int invoiceId);
        Task<TransactionDto> GetTransactionByIdAsync(int transactionId);
        Task<IPaginate<TransactionDto>> GetPaginateTransactionsAsync(TransactionFilterDto filterDto);
        Task<(decimal TotalIncome, decimal TotalExpense)> GetInvoiceSummaryAsync(int invoiceId);
    }
}