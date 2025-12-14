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
        /// - Biên lai nội bộ hỗ trợ 2 loại vật tư:
        ///   1. **AvailableAccessories**: Vật tư có sẵn trong kho
        ///   2. **AccessoriesToPurchase**: Vật tư cần mua từ bên ngoài
        /// 
        /// **Quy trình:**
        /// 1. Tạo invoice chính với vật tư có sẵn và dịch vụ
        /// 2. Tự động tạo invoice phụ (AccessoryPurchase) cho vật tư cần mua
        /// 3. Khi Manager/TechLead approve InspectionReport:
        ///    - Trừ quantity vật tư có sẵn từ kho
        ///    - Trừ budget cho việc mua vật tư
        ///    - Tạo transaction ghi nhận chi tiêu
        /// 
        /// **Lưu ý:**
        /// - Vật tư trong `AvailableAccessories` phải đủ số lượng trong kho
        /// - Vật tư trong `AccessoriesToPurchase` phải cung cấp giá mua dự kiến
        /// - Chỉ tạo invoice phụ khi có `AccessoriesToPurchase`
        /// </remarks>
        /// <param name="dto">Thông tin tạo biên lai nội bộ.</param>
        /// <response code="200">Tạo biên lai thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="404">Yêu cầu sửa chữa hoặc vật tư không tồn tại.</response>
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
        /// - Giá và tên dịch vụ, vật tư được nhập thủ công.  
        /// - Không ảnh hưởng tới kho vật tư nội bộ.  
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
        [Authorize(Roles = nameof(AccountRole.Manager) + "," + nameof(AccountRole.Technician))]
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
        /// - Bao gồm thông tin vật tư và dịch vụ của từng biên lai.
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

        /// <summary>
        /// Xác nhận đã thanh toán cho nhà thầu bên ngoài (dành cho Manager).
        /// </summary>
        /// <remarks>
        /// **Role:**Manager  
        /// 
        /// - Xác nhận đã thanh toán thực tế cho nhà thầu sau khi hoàn thành công việc.
        /// - Cập nhật Transaction từ Pending → Success.
        /// - Không trừ budget (đã trừ lúc approve InspectionReport).
        /// 
        /// **Lưu ý:**
        /// - Chỉ dành cho invoice thuê ngoài (IsChargeable = false).
        /// - Transaction phải ở trạng thái Pending.
        /// - Cần upload chứng từ thanh toán (hóa đơn từ nhà thầu).
        /// </remarks>
        [HttpPost("confirm-external-payment")]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> ConfirmExternalContractorPayment([FromForm] ExternalContractorPaymentConfirmDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _invoiceService.ConfirmExternalContractorPaymentAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Hủy invoice và hoàn trả vật tư/budget (dành cho Manager/TechLead).
        /// </summary>
        /// <remarks>
        /// **Role:** Manager, TechnicianLead  
        /// 
        /// **Chức năng:**
        /// - Hủy invoice Draft hoặc Approved
        /// - Tự động hoàn trả vật tư đã xuất về kho
        /// - Hoàn trả budget đã chi cho vật tư cần mua
        /// - Cập nhật trạng thái phiếu xuất/nhập kho thành Cancelled
        /// 
        /// **Ràng buộc:**
        /// - Không thể hủy invoice đã thanh toán (Paid)
        /// - Không thể hủy invoice đang chờ thanh toán từ resident (AwaitingPayment)
        /// 
        /// **Logic hoàn trả:**
        /// - **Phiếu xuất (FromStock):** Cộng lại số lượng vào kho
        /// - **Phiếu nhập (ToBePurchased):**
        ///   - Nếu Completed: Cộng lại số lượng vào kho
        ///   - Nếu Pending/Approved: Chỉ cancel phiếu, hoàn trả budget
        /// </remarks>
        /// <param name="invoiceId">ID invoice cần hủy</param>
        /// <param name="dto">Lý do hủy invoice</param>
        /// <response code="200">Hủy invoice thành công</response>
        /// <response code="400">Invoice không hợp lệ hoặc không thể hủy</response>
        /// <response code="404">Không tìm thấy invoice</response>
        [HttpPost("cancel/{invoiceId:int}")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)}, {nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelInvoice(int invoiceId, [FromBody] CancelInvoiceDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _invoiceService.CancelInvoiceAsync(invoiceId, dto.Reason);
            return Ok(result);
        }
    }

}
