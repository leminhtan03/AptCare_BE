using AptCare.Service.Dtos.InvoiceDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface IInvoiceService
    {
        Task<string> CreateInternalInvoiceAsync(InvoiceInternalCreateDto dto);
        Task<string> CreateExternalInvoiceAsync(InvoiceExternalCreateDto dto);
        Task<IEnumerable<InvoiceDto>> GetInvoicesAsync(int repairRequestId);
    }
}
