using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.TransactionDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    public class TransactionController : BaseApiController
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        /// <summary>
        /// Tạo link PayOS để thu tiền từ cư dân
        /// </summary>
        /// <remarks>
        /// **Role:** Technician, Manager, Admin  
        /// 
        /// **Chức năng:**  
        /// - Tạo link thanh toán PayOS cho cư dân.
        /// - Tự động tạo Transaction với Status = Pending.
        /// - Cập nhật Invoice Status = AwaitingPayment.
        /// - Cư dân quét QR hoặc click link để thanh toán.
        /// 
        /// **Ràng buộc:**  
        /// - Chỉ dành cho hóa đơn IsChargeable = true.
        /// - RepairRequest phải đã Completed hoặc AcceptancePendingVerify.
        /// - Hóa đơn chưa thanh toán đủ.
        /// 
        /// **Use case:**  
        /// - Technician tạo QR cho cư dân thanh toán sau khi sửa chữa xong.
        /// - Manager tạo link thanh toán cho hóa đơn nhà thầu (pass-through).
        /// </remarks>
        /// <param name="invoiceId">ID hóa đơn cần tạo link thanh toán</param>
        /// <response code="200">Trả về link PayOS cho cư dân thanh toán</response>
        /// <response code="400">Hóa đơn không thu phí hoặc công việc chưa hoàn tất</response>
        /// <response code="404">Không tìm thấy hóa đơn</response>
        /// <response code="401">Không có quyền truy cập</response>
        /// <response code="500">Lỗi PayOS hoặc hệ thống</response>
        [HttpPost("income/payment-link/{invoiceId}")]
        [Authorize(Roles = $"{nameof(AccountRole.Technician)},{nameof(AccountRole.Manager)},{nameof(AccountRole.Admin)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateIncomePaymentLink([FromRoute] int invoiceId)
        {
            var checkoutUrl = await _transactionService.CreateIncomePaymentLinkAsync(invoiceId);
            return Ok(new { CheckoutUrl = checkoutUrl });
        }

        /// <summary>
        /// Tạo giao dịch THU tiền mặt từ cư dân
        /// </summary>
        /// <remarks>
        /// **Role:** Technician, Manager, Admin  
        /// 
        /// **Chức năng:**  
        /// - Ghi nhận thu tiền mặt từ cư dân.
        /// - Upload file biên lai (PDF/Image - optional).
        /// - Tự động cập nhật trạng thái Invoice (PartiallyPaid/Paid).
        /// - Có thể thu nhiều lần cho cùng 1 hóa đơn.
        /// 
        /// **Ràng buộc:**  
        /// - Chỉ dành cho hóa đơn IsChargeable = true.
        /// - RepairRequest phải đã Completed hoặc AcceptancePendingVerify.
        /// - Tổng số tiền thu không vượt quá giá trị hóa đơn.
        /// 
        /// **Use case:**  
        /// - Technician thu tiền mặt tại chỗ sau khi sửa chữa xong.
        /// - Cư dân trả tiền theo từng đợt.
        /// </remarks>
        /// <param name="dto">Thông tin thu tiền bao gồm InvoiceId, Amount, Note, ReceiptFile (optional)</param>
        /// <response code="200">Tạo giao dịch thu tiền thành công</response>
        /// <response code="400">Vượt quá giá trị hóa đơn hoặc công việc chưa hoàn tất</response>
        /// <response code="404">Không tìm thấy hóa đơn</response>
        /// <response code="401">Không có quyền truy cập</response>
        /// <response code="500">Lỗi hệ thống</response>
        [HttpPost("cash-income")]
        [Authorize(Roles = $"{nameof(AccountRole.Technician)},{nameof(AccountRole.Manager)},{nameof(AccountRole.Admin)}")]
        [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateIncomeCash([FromForm] TransactionIncomeCashDto dto)
        {
            var result = await _transactionService.CreateIncomeCashAsync(dto);
            return Ok(result);
        }


        /// <summary>
        /// Lấy danh sách giao dịch theo Invoice
        /// </summary>
        /// <remarks>
        /// **Role:**  Manager, Admin, TechnicianLead  
        /// 
        /// **Chức năng:**  
        /// - Lấy toàn bộ lịch sử giao dịch của 1 hóa đơn.
        /// - Bao gồm cả thu và chi, file đính kèm.
        /// - Sắp xếp theo thời gian tạo giảm dần.
        /// 
        /// **Use case:**  
        /// - Xem lịch sử thanh toán của 1 hóa đơn cụ thể.
        /// - Kiểm tra đã thu chi đến đâu.
        /// </remarks>
        /// <param name="invoiceId">ID hóa đơn cần xem lịch sử</param>
        /// <response code="200">Danh sách giao dịch của hóa đơn</response>
        /// <response code="401">Không có quyền truy cập</response>
        /// <response code="500">Lỗi hệ thống</response>
        [HttpGet("by-invoice/{invoiceId}")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)},{nameof(AccountRole.Admin)},{nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(IEnumerable<TransactionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTransactionsByInvoiceId([FromRoute] int invoiceId)
        {
            var result = await _transactionService.GetTransactionsByInvoiceIdAsync(invoiceId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết 1 giao dịch
        /// </summary>
        /// <remarks>
        /// **Role:**  Manager, Admin, TechnicianLead  
        /// 
        /// **Chức năng:**  
        /// - Xem thông tin chi tiết 1 giao dịch.
        /// - Bao gồm file đính kèm, thông tin người tạo.
        /// 
        /// **Use case:**  
        /// - Xem chi tiết giao dịch để kiểm tra.
        /// - Download file hóa đơn/biên lai đính kèm.
        /// </remarks>
        /// <param name="transactionId">ID giao dịch cần xem chi tiết</param>
        /// <response code="200">Thông tin chi tiết giao dịch</response>
        /// <response code="404">Không tìm thấy giao dịch</response>
        /// <response code="401">Không có quyền truy cập</response>
        /// <response code="500">Lỗi hệ thống</response>
        [HttpGet("{transactionId}")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)},{nameof(AccountRole.Admin)},{nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTransactionById([FromRoute] int transactionId)
        {
            var result = await _transactionService.GetTransactionByIdAsync(transactionId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách giao dịch có phân trang và lọc
        /// </summary>
        /// <remarks>
        /// <b>Role:</b> Manager, Admin, TechnicianLead  
        /// 
        /// <b>Chức năng:</b>  
        /// - Lấy danh sách giao dịch với phân trang.
        /// - Hỗ trợ tìm kiếm và lọc đa dạng theo nhiều trường.
        /// 
        /// <b>Tham số phân trang:</b>
        /// - <c>page</c>: Số trang (mặc định = 1)
        /// - <c>size</c>: Số bản ghi trên trang (mặc định = 10)
        /// - <c>sortBy</c>: Sắp xếp (id, date, amount, ...)
        /// - <c>search</c>: Tìm kiếm theo mô tả giao dịch
        /// 
        /// <b>Tham số lọc nâng cao:</b>
        /// - <c>InvoiceId</c>: Lọc theo hóa đơn cụ thể
        /// - <c>TransactionType</c>: Lọc theo loại giao dịch (Payment, Refund, ...)
        /// - <c>Status</c>: Lọc theo trạng thái giao dịch (Pending, Success, Failed)
        /// - <c>Provider</c>: Lọc theo nhà cung cấp (PayOS, UnKnow, ...)
        /// - <c>Direction</c>: Lọc theo hướng giao dịch (Income, Expense)
        /// - <c>FromDate</c>, <c>ToDate</c>: Lọc theo khoảng thời gian tạo giao dịch
        /// 
        /// <b>Use case:</b>  
        /// - Báo cáo tài chính hệ thống.
        /// - Kiểm tra giao dịch theo nhiều tiêu chí.
        /// </remarks>
        /// <param name="filterDto">
        /// Tham số phân trang và lọc, bao gồm:
        /// <list type="bullet">
        /// <item><description><c>page</c>, <c>size</c>, <c>sortBy</c>, <c>search</c></description></item>
        /// <item><description><c>InvoiceId</c>, <c>TransactionType</c>, <c>Status</c>, <c>Provider</c>, <c>Direction</c></description></item>
        /// <item><description><c>FromDate</c>, <c>ToDate</c></description></item>
        /// </list>
        /// </param>
        /// <response code="200">Danh sách giao dịch có phân trang</response>
        /// <response code="401">Không có quyền truy cập</response>
        /// <response code="500">Lỗi hệ thống</response>
        [HttpGet("paginate")]
        //[Authorize(Roles = $"{nameof(AccountRole.Manager)},{nameof(AccountRole.Admin)},{nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(IPaginate<TransactionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPaginateTransactions([FromQuery] TransactionFilterDto filterDto)
        {
            var result = await _transactionService.GetPaginateTransactionsAsync(filterDto);
            return Ok(result);
        }


    }
}
