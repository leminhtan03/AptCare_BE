using AptCare.Repository.Entities;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.AccessoryDto;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccessoryController : BaseApiController
    {
        private readonly IAccessoryService _accessoryService;
        private readonly IAccessoryStockService _stockService;

        public AccessoryController(IAccessoryService accessoryService, IAccessoryStockService stockService)
        {
            _accessoryService = accessoryService;
            _stockService = stockService;
        }


        /// <summary>
        /// Cập nhật thông tin linh kiện.
        /// </summary>
        /// <remarks>
        /// Chỉ dành cho các vai trò: Manager hoặc TechnicianLead.  
        /// Body: `AccessoryUpdateDto` (Name, Description, Price, Quantity, Status)
        /// </remarks>
        /// <param name="id">ID của linh kiện cần cập nhật.</param>
        /// <response code="200">Cập nhật thành công, trả về thông điệp.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Bị từ chối truy cập.</response>
        /// <response code="404">Không tìm thấy linh kiện.</response>
        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)}, {nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> UpdateAccessory(int id, [FromForm] AccessoryUpdateDto dto)
        {
            var result = await _accessoryService.UpdateAccessoryAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Xóa linh kiện hoặc đánh dấu đã xóa (soft-delete).
        /// </summary>
        /// <remarks>
        /// Chỉ dành cho các vai trò: Manager hoặc TechnicianLead.
        /// </remarks>
        /// <param name="id">ID của linh kiện cần xóa.</param>
        /// <response code="200">Xóa thành công, trả về thông điệp.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Bị từ chối truy cập.</response>
        /// <response code="404">Không tìm thấy linh kiện.</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)}, {nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> DeleteAccessory(int id)
        {
            var result = await _accessoryService.DeleteAccessoryAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một linh kiện theo ID.
        /// </summary>
        /// <remarks>
        /// Dành cho các vai trò: TechnicianLead, Manager, Technician.
        /// </remarks>
        /// <param name="id">ID của linh kiện.</param>
        /// <response code="200">Trả về thông tin linh kiện.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="404">Không tìm thấy linh kiện.</response>
        [HttpGet("{id:int}")]
        [Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}, {nameof(AccountRole.Manager)}, {nameof(AccountRole.Technician)}")]
        [ProducesResponseType(typeof(AccessoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AccessoryDto>> GetAccessoryById(int id)
        {
            var result = await _accessoryService.GetAccessoryByIdAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách linh kiện có phân trang, tìm kiếm và lọc.
        /// </summary> 
        /// <remarks>
        /// Role: TechnicianLead, Manager, Technician.  
        ///
        /// Query parameters (được đóng gói trong `PaginateDto` hoặc truyền trực tiếp):
        /// - page (int, optional): số trang, bắt đầu từ 1. Mặc định 1.  
        /// - size (int, optional): số bản ghi mỗi trang. Mặc định 10.  
        /// - search (string, optional): tìm kiếm theo `Name` và `Descrption` (không phân biệt hoa thường).  
        /// - filter (string, optional): lọc theo trạng thái, giá trị hợp lệ:
        ///     - "active"  => chỉ trả về linh kiện có `Status = Active`  
        ///     - "inactive" => chỉ trả về linh kiện có `Status = Inactive`  
        ///     - empty/null => không lọc theo trạng thái  
        /// - sortBy (string, optional): sắp xếp, giá trị hợp lệ:
        ///     - "name"        => theo `Name` tăng dần  
        ///     - "name_desc"   => theo `Name` giảm dần  
        ///     - "price"       => theo `Price` tăng dần  
        ///     - "price_desc"  => theo `Price` giảm dần  
        ///     - empty/null    => mặc định sắp xếp theo `AccessoryId` giảm dần
        ///
        /// Ví dụ: GET /accessory/paginate?page=1&size=20&search=đèn&filter=active&sortBy=price_desc
        /// </remarks>
        /// <param name="dto">Đối tượng phân trang chứa `page`, `size`, `search`, `filter`, `sortBy`.</param>
        /// <response code="200">Trả về kết quả phân trang `IPaginate<AccessoryDto>`.</response>
        /// <response code="400">Tham số truy vấn không hợp lệ.</response>
        /// <response code="401">Không có quyền.</response>
        [HttpGet("paginate")]
        [Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}, {nameof(AccountRole.Manager)}, {nameof(AccountRole.Technician)}")]
        [ProducesResponseType(typeof(IPaginate<AccessoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IPaginate<AccessoryDto>>> GetPaginateAccessory([FromQuery] PaginateDto dto)
        {
            var result = await _accessoryService.GetPaginateAccessoryAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách linh kiện đang hoạt động để hiển thị hoặc sử dụng nội bộ.
        /// </summary>
        /// <remarks>
        /// Dành cho các vai trò: TechnicianLead, Manager, Technician.   
        /// Trả về danh sách sắp xếp theo tên.
        /// </remarks>
        /// <response code="200">Trả về danh sách linh kiện.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet("list")]
        [Authorize(Roles = $"{nameof(AccountRole.TechnicianLead)}, {nameof(AccountRole.Manager)}, {nameof(AccountRole.Technician)}")]
        [ProducesResponseType(typeof(IEnumerable<AccessoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<AccessoryDto>>> GetAccessories()
        {
            var result = await _accessoryService.GetAccessoriesAsync();
            return Ok(result);
        }

        // --- AccessoryStock (StockIn/StockOut) APIs tích hợp tại đây ---

        /// <summary>
        /// Tạo yêu cầu nhập kho vật tư (tạo mới hoặc nhập thêm).
        /// </summary>
        /// <remarks>
        /// <b>Chức năng:</b> Tạo yêu cầu nhập kho vật tư. Nếu vật tư chưa tồn tại sẽ tự động tạo mới.<br/>
        /// <b>Yêu cầu quyền:</b> Manager, TechnicianLead.<br/>
        /// <b>Thuộc tính <see cref="StockInAccessoryDto"/>:</b>
        /// <ul>
        ///   <li><b>AccessoryId</b> (int?, optional): ID vật tư đã có. Nếu null hoặc 0 sẽ tạo mới vật tư mới.</li> 
        ///   <li><b>Name</b> (string, required nếu tạo mới): Tên vật tư mới.</li>
        ///   <li><b>Description</b> (string, optional): Mô tả vật tư.</li>
        ///   <li><b>UnitPrice</b> (decimal?, optional): Đơn giá vật tư.</li>
        ///   <li><b>Quantity</b> (int, required): Số lượng nhập kho.</li>
        ///   <li><b>Note</b> (string, optional): Ghi chú cho yêu cầu nhập kho.</li>
        /// </ul>
        /// <b>Kết quả:</b> Trả về thông báo tạo yêu cầu nhập kho thành công.
        /// </remarks>
        [HttpPost("stock-in")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)}, {nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateStockInRequest([FromForm] StockInAccessoryDto dto)
        {
            var result = await _stockService.CreateStockInRequestAsync(dto);
            return Created(string.Empty, result);
        }

        /// <summary>
        /// Phê duyệt hoặc từ chối yêu cầu nhập kho.
        /// </summary>
        /// <remarks>
        /// <b>Chức năng:</b> Phê duyệt hoặc từ chối một yêu cầu nhập kho vật tư.<br/>
        /// <b>Yêu cầu quyền:</b> Manager, TechnicianLead.<br/>
        /// <b>Tham số:</b>
        /// <ul>
        ///   <li><b>stockTransactionId</b> (int): ID giao dịch nhập kho.</li>
        ///   <li><b>isApprove</b> (bool): true = phê duyệt, false = từ chối.</li>
        ///   <li><b>note</b> (string, optional): Ghi chú khi phê duyệt/từ chối.</li>
        /// </ul>
        /// <b>Kết quả:</b> Trả về true nếu thao tác thành công.
        /// </remarks>
        [HttpPatch("stock-in/approve/{stockTransactionId:int}")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)}, {nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> ApproveStockInRequest(int stockTransactionId, [FromQuery] bool isApprove, [FromQuery] string? note)
        {
            var result = await _stockService.ApproveStockInRequestAsync(stockTransactionId, isApprove, note);
            return Ok(result);
        }

        /// <summary>
        /// Xác nhận hoặc từ chối nhập kho (kèm file xác thực nếu có).
        /// </summary>
        /// <remarks>
        /// <b>Chức năng:</b> Xác nhận hoặc từ chối nhập kho thực tế, có thể đính kèm file xác thực (PDF hoặc ảnh).<br/>
        /// <b>Yêu cầu quyền:</b> Manager, TechnicianLead.<br/>
        /// <b>Thuộc tính <see cref="ConfirmStockInDto"/>:</b>
        /// <ul>
        ///   <li><b>StockTransactionId</b> (int): ID giao dịch nhập kho cần xử lý.</li>
        ///   <li><b>IsConfirm</b> (bool): true = xác nhận nhập kho, false = từ chối.</li>
        ///   <li><b>VerificationFile</b> (IFormFile, optional): File xác thực (PDF hoặc ảnh).</li>
        ///   <li><b>Note</b> (string, optional): Ghi chú xác nhận/từ chối.</li>
        /// </ul>
        /// <b>Logic xử lý:</b>
        /// <ul>
        ///   <li><b>Confirm (IsConfirm = true):</b>
        ///     <ul>
        ///       <li>Nếu không gắn invoice → Cộng kho ngay (hàng dự trữ)</li>
        ///       <li>Nếu gắn invoice còn active → Cộng kho để sẵn sàng sử dụng</li>
        ///       <li>Nếu gắn invoice đã cancelled → Cộng kho (trở thành dự trữ)</li>
        ///       <li>Đánh dấu transaction tài chính = Success</li>
        ///     </ul>
        ///   </li>
        ///   <li><b>Reject (IsConfirm = false):</b>
        ///     <ul>
        ///       <li>Không cộng kho</li>
        ///       <li>Hoàn trả budget (đã trừ lúc approve)</li>
        ///       <li>Đánh dấu transaction tài chính = Fail</li>
        ///       <li>Cảnh báo nếu gắn với invoice còn active</li>
        ///     </ul>
        ///   </li>
        /// </ul>
        /// <b>Kết quả:</b> Trả về true nếu xử lý thành công.
        /// </remarks>
        [HttpPost("stock-in/confirm")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)}, {nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConfirmOrRejectStockIn([FromForm] ConfirmStockInDto dto)
        {
            var result = await _stockService.ConfirmStockInAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách giao dịch nhập/xuất kho (phân trang, tìm kiếm, lọc).
        /// </summary>
        /// <remarks>
        /// <b>Chức năng:</b> Lấy danh sách các giao dịch nhập/xuất kho với phân trang, tìm kiếm theo tên vật tư hoặc ghi chú, lọc theo loại giao dịch, trạng thái, ngày tạo.<br/>
        /// <b>Thuộc tính <see cref="StockTransactionFilterDto"/>:</b>
        /// <ul>
        ///   <li><b>search</b> (string, optional): Tìm kiếm theo tên vật tư hoặc ghi chú.</li>
        ///   <li><b>Type</b> (StockTransactionType?, optional): Lọc theo loại giao dịch (Import/Export). Giá trị hợp lệ: <code>Import</code>, <code>Export</code>, để trống = tất cả.</li>
        ///   <li><b>Status</b> (StockTransactionStatus?, optional): Lọc theo trạng thái giao dịch. Giá trị hợp lệ: <code>Pending</code>, <code>Approved</code>, <code>Rejected</code>, <code>Completed</code>, để trống = tất cả.</li>
        ///   <li><b>FromDate</b> (DateOnly?, optional): Lọc từ ngày tạo.</li>
        ///   <li><b>ToDate</b> (DateOnly?, optional): Lọc đến ngày tạo.</li>
        ///   <li><b>page</b> (int, optional): Số trang.</li>
        ///   <li><b>size</b> (int, optional): Số bản ghi mỗi trang.</li>
        ///   <li><b>sortBy</b> (string, optional): Sắp xếp theo trường. Giá trị hợp lệ:
        ///     <ul>
        ///         <li><code>createdAt</code>: theo ngày tạo tăng dần</li>
        ///         <li><code>createdAt_desc</code>: theo ngày tạo giảm dần (mặc định)</li>
        ///         <li><code>name</code>: theo tên vật tư tăng dần</li>
        ///         <li><code>name_desc</code>: theo tên vật tư giảm dần</li>
        ///         <li><code>quantity</code>: theo số lượng tăng dần</li>
        ///         <li><code>quantity_desc</code>: theo số lượng giảm dần</li>
        ///     </ul>
        ///   </li>
        ///   <li><b>filter</b> (string, optional): Lọc nhanh theo trạng thái. Giá trị hợp lệ:
        ///     <ul>
        ///         <li><code>pending</code>: chỉ giao dịch chờ duyệt</li>
        ///         <li><code>approved</code>: chỉ giao dịch đã duyệt</li>
        ///         <li><code>rejected</code>: chỉ giao dịch bị từ chối</li>
        ///         <li><code>completed</code>: chỉ giao dịch đã hoàn thành</li>
        ///         <li><code>import</code>: chỉ giao dịch nhập kho</li>
        ///         <li><code>export</code>: chỉ giao dịch xuất kho</li>
        ///         <li>để trống/null: không lọc nhanh</li>
        ///     </ul>
        ///     Có thể kết hợp đồng thời với <b>Type</b> để lọc cả loại giao dịch và trạng thái.
        ///   </li>
        /// </ul>
        /// <b>Kết quả:</b> Trả về danh sách phân trang các giao dịch nhập/xuất kho.
        /// </remarks>
        [HttpGet("stock-transaction/paginate")]
        [Authorize]
        [ProducesResponseType(typeof(IPaginate<AccessoryStockTransactionDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaginateStockTransactions([FromQuery] StockTransactionFilterDto filter)
        {
            var result = await _stockService.GetPaginateStockTransactionsAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết giao dịch nhập/xuất kho theo ID (bao gồm media).
        /// </summary>
        /// <remarks>
        /// <b>Chức năng:</b> Lấy chi tiết một giao dịch nhập/xuất kho, bao gồm thông tin vật tư, trạng thái, các file xác thực (media) nếu có.<br/>
        /// <b>Tham số:</b>
        /// <ul>
        ///   <li><b>stockTransactionId</b> (int): ID giao dịch nhập/xuất kho.</li>
        /// </ul>
        /// <b>Kết quả:</b> Trả về đối tượng AccessoryStockTransactionDto gồm thông tin giao dịch và danh sách media.
        /// </remarks>
        [HttpGet("stock-transactions/{stockTransactionId:int}")]
        [Authorize]
        [ProducesResponseType(typeof(AccessoryStockTransactionDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStockTransactionById(int stockTransactionId)
        {
            var result = await _stockService.GetStockTransactionByIdAsync(stockTransactionId);
            return Ok(result);
        }
        /// <summary>
        /// Tạo yêu cầu xuất kho vật tư.
        /// </summary>
        /// <remarks>
        /// <b>Chức năng:</b> Tạo yêu cầu xuất kho vật tư từ kho.<br/>
        /// <b>Yêu cầu quyền:</b> Manager, TechnicianLead.<br/>
        /// <b>Thuộc tính <see cref="StockOutAccessoryDto"/>:</b>
        /// <ul>
        ///   <li><b>Quantity</b> (int, required): Số lượng cần xuất kho.</li>
        ///   <li><b>Note</b> (string, optional): Ghi chú cho yêu cầu xuất kho.</li>
        /// </ul>
        /// <b>Kết quả:</b> Trả về thông báo tạo yêu cầu xuất kho thành công.
        /// </remarks>
        /// <param name="accessoryId">ID của vật tư cần xuất kho.</param>
        /// <param name="dto">Thông tin yêu cầu xuất kho.</param>
        /// <response code="201">Tạo yêu cầu xuất kho thành công.</response>
        /// <response code="400">Dữ liệu không hợp lệ.</response>
        /// <response code="404">Vật tư không tồn tại.</response>
        [HttpPost("stock-out/{accessoryId:int}")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)}, {nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateStockOutRequest(int accessoryId, [FromBody] StockOutAccessoryDto dto)
        {
            var result = await _stockService.CreateStockOutRequestAsync(accessoryId, dto);
            return Created(string.Empty, result);
        }

        /// <summary>
        /// Phê duyệt hoặc từ chối yêu cầu xuất kho.
        /// </summary>
        /// <remarks>
        /// <b>Chức năng:</b> Phê duyệt hoặc từ chối một yêu cầu xuất kho vật tư.<br/>
        /// <b>Yêu cầu quyền:</b> Manager, TechnicianLead.<br/>
        /// <b>Tham số:</b>
        /// <ul>
        ///   <li><b>stockTransactionId</b> (int): ID giao dịch xuất kho.</li>
        ///   <li><b>isApprove</b> (bool): true = phê duyệt, false = từ chối.</li>
        ///   <li><b>note</b> (string, optional): Ghi chú khi phê duyệt/từ chối.</li>
        /// </ul>
        /// <b>Logic xử lý khi phê duyệt:</b>
        /// <ul>
        ///   <li>Kiểm tra số lượng vật tư trong kho có đủ không.</li>
        ///   <li>Trừ số lượng vật tư khỏi kho.</li>
        ///   <li>Chuyển trạng thái sang Approved.</li>
        /// </ul>
        /// <b>Kết quả:</b> Trả về true nếu thao tác thành công.
        /// </remarks>
        /// <param name="stockTransactionId">ID giao dịch xuất kho.</param>
        /// <param name="isApprove">true = phê duyệt, false = từ chối.</param>
        /// <param name="note">Ghi chú (tùy chọn).</param>
        /// <response code="200">Phê duyệt/từ chối thành công.</response>
        /// <response code="400">Yêu cầu không hợp lệ hoặc không đủ số lượng.</response>
        [HttpPatch("stock-out/approve/{stockTransactionId:int}")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)}, {nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ApproveStockOutRequest(int stockTransactionId, [FromQuery] bool isApprove, [FromQuery] string? note)
        {
            var result = await _stockService.ApproveStockOutRequestAsync(stockTransactionId, isApprove, note);
            return Ok(result);
        }

        /// <summary>
        /// Xác nhận hoặc từ chối xuất kho (kèm file xác thực nếu có).
        /// </summary>
        /// <remarks>
        /// <b>Chức năng:</b> Xác nhận hoặc từ chối xuất kho thực tế, có thể đính kèm file xác thực (PDF hoặc ảnh).<br/>
        /// <b>Yêu cầu quyền:</b> Manager, TechnicianLead.<br/>
        /// <b>Thuộc tính <see cref="ConfirmStockOutDto"/>:</b>
        /// <ul>
        ///   <li><b>StockTransactionId</b> (int): ID giao dịch xuất kho cần xử lý.</li>
        ///   <li><b>IsConfirm</b> (bool): true = xác nhận xuất kho, false = từ chối.</li>
        ///   <li><b>VerificationFile</b> (IFormFile, optional): File xác thực (PDF hoặc ảnh).</li>
        ///   <li><b>Note</b> (string, optional): Ghi chú xác nhận/từ chối.</li>
        /// </ul>
        /// <b>Logic xử lý:</b>
        /// <ul>
        ///   <li><b>Confirm (IsConfirm = true):</b>
        ///     <ul>
        ///       <li>Đánh dấu giao dịch = Completed</li>
        ///       <li>Lưu file xác thực (nếu có)</li>
        ///     </ul>
        ///   </li>
        ///   <li><b>Reject (IsConfirm = false):</b>
        ///     <ul>
        ///       <li>Hoàn trả số lượng vật tư vào kho (đã trừ lúc approve)</li>
        ///       <li>Đánh dấu giao dịch = Rejected</li>
        ///     </ul>
        ///   </li>
        /// </ul>
        /// <b>Kết quả:</b> Trả về true nếu xử lý thành công.
        /// </remarks>
        /// <param name="dto">Thông tin xác nhận xuất kho.</param>
        /// <response code="200">Xác nhận/từ chối thành công.</response>
        /// <response code="400">Yêu cầu không hợp lệ.</response>
        [HttpPost("stock-out/confirm")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)}, {nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConfirmOrRejectStockOut([FromForm] ConfirmStockOutDto dto)
        {
            var result = await _stockService.ConfirmStockOutAsync(dto);
            return Ok(result);
        }
    }
}