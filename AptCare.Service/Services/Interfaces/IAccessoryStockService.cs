
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.AccessoryDto;
namespace AptCare.Service.Services.Interfaces
{
    public interface IAccessoryStockService
    {
        Task<string> CreateStockInRequestAsync(StockInAccessoryDto dto);
        Task<bool> ApproveStockInRequestAsync(int stockTransactionId, bool isApprove, string? note = null);
        Task<int> CreateStockOutRequestAsync(int accessoryId, int quantity, int? repairRequestId, int? invoiceId, string note);
        Task<bool> ApproveStockOutRequestAsync(int stockTransactionId);
        Task<bool> ConfirmStockInAsync(ConfirmStockInDto dto);
        Task<IPaginate<AccessoryStockTransactionDto>> GetPaginateStockTransactionsAsync(StockTransactionFilterDto filter);
        Task<AccessoryStockTransactionDto> GetStockTransactionByIdAsync(int stockTransactionId);
        // ... các method khác ...
    }
}