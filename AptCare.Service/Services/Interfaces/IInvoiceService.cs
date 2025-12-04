using AptCare.Service.Dtos.InvoiceDtos;

namespace AptCare.Service.Services.Interfaces
{
    public interface IInvoiceService
    {
        Task<string> CreateInternalInvoiceAsync(InvoiceInternalCreateDto dto);
        Task<string> CreateExternalInvoiceAsync(InvoiceExternalCreateDto dto);
        Task<IEnumerable<InvoiceDto>> GetInvoicesAsync(int repairRequestId);
    }
}
