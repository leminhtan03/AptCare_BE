using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.AccessoryDto;
namespace AptCare.Service.Services.Interfaces
{
    public interface IAccessoryStockService
    {
        Task<string> CreateStockInRequestAsync(StockInAccessoryDto dto);
        Task<bool> ApproveStockInRequestAsync(int stockTransactionId, bool isApprove, string? note = null);
        Task<bool> ConfirmStockInAsync(ConfirmStockInDto dto);

        Task<IPaginate<AccessoryStockTransactionDto>> GetPaginateStockTransactionsAsync(StockTransactionFilterDto filter);
        Task<AccessoryStockTransactionDto> GetStockTransactionByIdAsync(int stockTransactionId);

        Task<string> CreateStockOutRequestAsync(int id, StockOutAccessoryDto dto);
        Task<bool> ApproveStockOutRequestAsync(int stockTransactionId, bool isApprove, string? note);

        Task RevertStockForCancelledInvoiceAsync(int invoiceId);
        Task<List<string>> EnsureStockForInvoiceAsync(int invoiceId);
    }
}