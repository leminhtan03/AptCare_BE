using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Service.Dtos.InvoiceDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    public class InvoiceController : BaseApiController
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        /// <summary>
        /// Tạo biên lai sửa chữa nội bộ (dành cho kỹ thuật viên).
        /// </summary>
        /// <remarks>
        /// **Role:** 🧑‍🔧 Technician  
        /// 
        /// - Biên lai nội bộ lấy phụ kiện từ kho trong hệ thống.  
        /// - Giảm số lượng phụ kiện trong kho tương ứng.  
        /// - Chỉ áp dụng cho các yêu cầu sửa chữa nội bộ.  
        /// 
        /// **Lưu ý:**  
        /// - Mỗi phụ kiện phải tồn tại và còn đủ số lượng trong kho.  
        /// - Nếu không, hệ thống sẽ trả về lỗi 400 hoặc 404.
        /// </remarks>
        /// <param name="dto">Thông tin tạo biên lai nội bộ.</param>
        /// <response code="200">Tạo biên lai thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="404">Yêu cầu sửa chữa hoặc phụ kiện không tồn tại.</response>
        /// <response code="500">Lỗi hệ thống.</response>
        [HttpPost("internal")]
        [Authorize(Roles = nameof(AccountRole.Technician))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateInternalInvoice([FromBody] InvoiceInternalCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var message = await _invoiceService.CreateInternalInvoiceAsync(dto);
            return Ok(message);
        }

        /// <summary>
        /// Tạo biên lai sửa chữa bên thứ ba (dành cho quản lý).
        /// </summary>
        /// <remarks>
        /// **Role:** 🧑‍💼 Manager  
        /// 
        /// - Dùng cho trường hợp thuê bên ngoài thực hiện sửa chữa.  
        /// - Giá và tên dịch vụ, phụ kiện được nhập thủ công.  
        /// - Không ảnh hưởng tới kho phụ kiện nội bộ.  
        /// 
        /// **Lưu ý:**  
        /// - Cần có `RepairRequestId` hợp lệ.  
        /// </remarks>
        /// <param name="dto">Thông tin tạo biên lai bên thứ ba.</param>
        /// <response code="200">Tạo biên lai thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="404">Yêu cầu sửa chữa không tồn tại.</response>
        /// <response code="500">Lỗi hệ thống.</response>
        [HttpPost("external")]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateExternalInvoice([FromBody] InvoiceExternalCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var message = await _invoiceService.CreateExternalInvoiceAsync(dto);
            return Ok(message);
        }

        /// <summary>
        /// Lấy danh sách các biên lai theo yêu cầu sửa chữa.
        /// </summary>
        /// <remarks>
        /// **Role:** 🧑‍🔧 Technician / 🧑‍💼 Manager / 👨‍🏭 TechnicianLead  
        /// 
        /// - Trả về toàn bộ biên lai thuộc về một yêu cầu sửa chữa.  
        /// - Bao gồm thông tin phụ kiện và dịch vụ của từng biên lai.
        /// 
        /// **Lưu ý:**  
        /// - Nếu `RepairRequestId` không tồn tại, hệ thống trả về 404.  
        /// - Chỉ lấy những biên lai có `Status = Active`.
        /// </remarks>
        /// <param name="repairRequestId">ID của yêu cầu sửa chữa cần xem biên lai.</param>
        /// <response code="200">Danh sách biên lai tương ứng.</response>
        /// <response code="404">Không tìm thấy yêu cầu sửa chữa.</response>
        /// <response code="500">Lỗi hệ thống.</response>
        [HttpGet("{repairRequestId:int}")]
        [Authorize(Roles = $"{nameof(AccountRole.Technician)}, {nameof(AccountRole.TechnicianLead)}, {nameof(AccountRole.Manager)},{nameof(AccountRole.Resident)}")]
        [ProducesResponseType(typeof(IEnumerable<InvoiceDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetInvoicesByRepairRequestId(int repairRequestId)
        {
            var invoices = await _invoiceService.GetInvoicesAsync(repairRequestId);
            return Ok(invoices);
        }


    }

}
