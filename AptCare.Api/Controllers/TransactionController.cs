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

        // ===================== EXPENSE (CHI) =====================

        /// <summary>
        /// Tạo giao dịch CHI đặt cọc cho nhà thầu (ExternalContractor)
        /// </summary>
        /// <remarks>
        /// **Role:** Manager, Admin  
        /// 
        /// **Chức năng:**  
        /// - Tạo giao dịch chi tiền đặt cọc cho nhà thầu.
        /// - Upload file hóa đơn nhà thầu (PDF - bắt buộc).
        /// - Tự động cập nhật trạng thái Invoice (PartiallyPaid/PaidToContractor).
        /// - Có thể đặt cọc nhiều lần cho cùng 1 hóa đơn.
        /// 
        /// **Ràng buộc:**  
        /// - Chỉ dành cho hóa đơn nhà thầu (InvoiceType.ExternalContractor).
        /// - Tổng số tiền chi không vượt quá giá trị hóa đơn.
        /// - File PDF hóa đơn là bắt buộc.
        /// 
        /// **Use case:**  
        /// - Manager đặt cọc 30% khi ký hợp đồng với nhà thầu.
        /// - Đặt cọc thêm khi nhà thầu hoàn thành giai đoạn nào đó.
        /// </remarks>
        /// <param name="dto">Thông tin đặt cọc bao gồm InvoiceId, Amount, Note, ContractorInvoiceFile</param>
        /// <response code="200">Tạo giao dịch đặt cọc thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ hoặc vượt quá giá trị hóa đơn</response>
        /// <response code="404">Không tìm thấy hóa đơn</response>
        /// <response code="401">Không có quyền truy cập</response>
        /// <response code="500">Lỗi hệ thống</response>
        [HttpPost("expense/deposit")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)},{nameof(AccountRole.Admin)}")]
        [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateExpenseDeposit([FromForm] TransactionExpenseDepositDto dto)
        {
            var result = await _transactionService.CreateExpenseDepositAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Tạo giao dịch CHI phần còn lại cho nhà thầu (ExternalContractor)
        /// </summary>
        /// <remarks>
        /// **Role:**  Manager, Admin  
        /// 
        /// **Chức năng:**  
        /// - Thanh toán phần còn lại cho nhà thầu sau khi hoàn thành công việc.
        /// - Upload file hóa đơn nhà thầu (PDF - bắt buộc).
        /// - Tự động cập nhật trạng thái Invoice = PaidToContractor.
        /// - Chỉ được thực hiện 1 lần cho mỗi hóa đơn.
        /// 
        /// **Ràng buộc:**  
        /// - Chỉ dành cho hóa đơn nhà thầu (InvoiceType.ExternalContractor).
        /// - RepairRequest phải đã Completed.
        /// - Chưa có giao dịch "thanh toán phần còn lại" nào trước đó.
        /// - File PDF hóa đơn là bắt buộc.
        /// 
        /// **Use case:**  
        /// - Manager thanh toán 70% còn lại sau khi nhà thầu hoàn thành công việc.
        /// </remarks>
        /// <param name="dto">Thông tin thanh toán bao gồm InvoiceId, Note, ContractorInvoiceFile</param>
        /// <response code="200">Tạo giao dịch thanh toán cuối thành công</response>
        /// <response code="400">Công việc chưa hoàn thành hoặc đã thanh toán trước đó</response>
        /// <response code="404">Không tìm thấy hóa đơn</response>
        /// <response code="401">Không có quyền truy cập</response>
        /// <response code="500">Lỗi hệ thống</response>
        [HttpPost("expense/final")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)},{nameof(AccountRole.Admin)}")]
        [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateExpenseFinal([FromForm] TransactionExpenseFinalDto dto)
        {
            var result = await _transactionService.CreateExpenseFinalAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Tạo giao dịch CHI nội bộ (InternalRepair + IsChargeable = false)
        /// </summary>
        /// <remarks>
        /// **Role:**  Manager, Admin  
        /// 
        /// **Chức năng:**  
        /// - Ghi nhận chi phí xuất kho nội bộ (phụ kiện + dịch vụ).
        /// - Upload file chứng từ (PDF/Image - optional).
        /// - Tự động cập nhật trạng thái Invoice = Paid.
        /// - Ghi nhận chi phí không thu từ cư dân.
        /// 
        /// **Ràng buộc:**  
        /// - Chỉ dành cho hóa đơn nội bộ (InvoiceType.InternalRepair).
        /// - IsChargeable = false (không thu phí từ cư dân).
        /// - Chỉ được tạo 1 lần cho mỗi hóa đơn.
        /// - File chứng từ là optional (phiếu xuất kho, biên bản kiểm kê).
        /// 
        /// **Use case:**  
        /// - Kỹ thuật viên sửa chữa bằng phụ kiện từ kho, không tính phí cư dân.
        /// - Manager ghi nhận chi phí vật tư tiêu hao.
        /// </remarks>
        /// <param name="dto">Thông tin chi phí bao gồm InvoiceId, Note, ProofFile (optional)</param>
        /// <response code="200">Tạo giao dịch chi phí nội bộ thành công</response>
        /// <response code="400">Hóa đơn yêu cầu thu phí hoặc đã có giao dịch trước đó</response>
        /// <response code="404">Không tìm thấy hóa đơn</response>
        /// <response code="401">Không có quyền truy cập</response>
        /// <response code="500">Lỗi hệ thống</response>
        [HttpPost("expense/internal")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)},{nameof(AccountRole.Admin)}")]
        [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateExpenseInternal([FromForm] TransactionExpenseInternalDto dto)
        {
            var result = await _transactionService.CreateExpenseInternalAsync(dto);
            return Ok(result);
        }

        // ===================== INCOME (THU) =====================

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
        [HttpPost("income/cash")]
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

        // ===================== QUERY =====================

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
        /// **Role:** Manager, Admin, TechnicianLead  
        /// 
        /// **Chức năng:**  
        /// - Lấy danh sách giao dịch với phân trang.
        /// - Hỗ trợ tìm kiếm và lọc đa dạng.
        /// 
        /// **Tham số lọc:**  
        /// - `page`: Số trang (mặc định = 1)
        /// - `size`: Số bản ghi trên trang (mặc định = 10)
        /// - `search`: Tìm kiếm theo mô tả giao dịch
        /// - `InvoiceId`: Lọc theo hóa đơn cụ thể
        /// - `UserId`: Lọc theo người tạo giao dịch
        /// - `Direction`: Lọc theo hướng (Income/Expense)
        /// - `Status`: Lọc theo trạng thái (Pending/Success/Failed)
        /// - `Provider`: Lọc theo nhà cung cấp (PayOS/UnKnow)
        /// - `FromDate`, `ToDate`: Lọc theo khoảng thời gian
        /// - `sortBy`: Sắp xếp (id, date, amount, ...)
        /// 
        /// **Use case:**  
        /// - Báo cáo tài chính hệ thống.
        /// - Kiểm tra giao dịch theo khoảng thời gian.
        /// </remarks>
        /// <param name="filterDto">Tham số phân trang và lọc</param>
        /// <response code="200">Danh sách giao dịch có phân trang</response>
        /// <response code="401">Không có quyền truy cập</response>
        /// <response code="500">Lỗi hệ thống</response>
        [HttpGet("paginate")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)},{nameof(AccountRole.Admin)},{nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(IPaginate<TransactionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPaginateTransactions([FromQuery] TransactionFilterDto filterDto)
        {
            var result = await _transactionService.GetPaginateTransactionsAsync(filterDto);
            return Ok(result);
        }

        /// <summary>
        /// Lấy tổng thu chi theo Invoice
        /// </summary>
        /// <remarks>
        /// **Role:** Manager, Admin, TechnicianLead  
        /// 
        /// **Chức năng:**  
        /// - Tính tổng số tiền đã thu và đã chi cho 1 hóa đơn.
        /// - Chỉ tính các giao dịch thành công (Success).
        /// 
        /// **Use case:**  
        /// - Dashboard hiển thị tình hình thu chi.
        /// - Kiểm tra còn bao nhiêu tiền chưa thu/chưa chi.
        /// </remarks>
        /// <param name="invoiceId">ID hóa đơn cần tính tổng</param>
        /// <response code="200">Tổng thu và tổng chi của hóa đơn</response>
        /// <response code="401">Không có quyền truy cập</response>
        /// <response code="500">Lỗi hệ thống</response>
        [HttpGet("summary/{invoiceId}")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)},{nameof(AccountRole.Admin)},{nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetInvoiceSummary([FromRoute] int invoiceId)
        {
            var (totalIncome, totalExpense) = await _transactionService.GetInvoiceSummaryAsync(invoiceId);
            return Ok(new
            {
                InvoiceId = invoiceId,
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                NetIncome = totalIncome - totalExpense
            });
        }
    }
}
