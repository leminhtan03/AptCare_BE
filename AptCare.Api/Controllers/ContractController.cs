using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.ContractDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    public class ContractController : BaseApiController
    {
        private readonly IContractService _contractService;

        public ContractController(IContractService contractService)
        {
            _contractService = contractService;
        }

        /// <summary>
        /// Tạo hợp đồng cho yêu cầu sửa chữa với file PDF đính kèm
        /// </summary>
        /// <remarks>
        /// **Chức năng:**  
        /// - Tạo hợp đồng thuê ngoài cho yêu cầu sửa chữa.
        /// - Tự động upload file PDF hợp đồng lên AWS S3.
        /// - Lưu thông tin file vào bảng Media.
        /// 
        /// **Ràng buộc:**  
        /// - Yêu cầu sửa chữa phải tồn tại.
        /// - Báo cáo kiểm tra gần nhất phải có giải pháp là **Outsource** (Thuê ngoài).
        /// - Mã hợp đồng phải duy nhất.
        /// - File phải là định dạng PDF.
        /// 
        /// **Tham số:**  
        /// - `RepairRequestId`: ID yêu cầu sửa chữa (bắt buộc)
        /// - `ContractorName`: Tên nhà thầu (bắt buộc)
        /// - `ContractCode`: Mã hợp đồng (bắt buộc, duy nhất)
        /// - `StartDate`: Ngày bắt đầu hợp đồng (bắt buộc)
        /// - `EndDate`: Ngày kết thúc hợp đồng (tùy chọn)
        /// - `Amount`: Giá trị hợp đồng (tùy chọn)
        /// - `Description`: Mô tả hợp đồng (bắt buộc)
        /// - `ContractFile`: File PDF hợp đồng (bắt buộc)
        /// </remarks>
        /// <param name="dto">Thông tin hợp đồng và file đính kèm</param>
        /// <returns>Thông tin hợp đồng đã tạo</returns>
        /// <response code="200">Tạo hợp đồng thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ hoặc không đủ điều kiện tạo hợp đồng</response>
        /// <response code="404">Không tìm thấy yêu cầu sửa chữa</response>
        /// <response code="401">Không có quyền truy cập</response>
        /// <response code="500">Lỗi hệ thống</response>
        [HttpPost]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)},{nameof(AccountRole.Admin)}")]
        [ProducesResponseType(typeof(ContractDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateContract([FromForm] ContractCreateDto dto)
        {
            var result = await _contractService.CreateContractAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Kiểm tra xem yêu cầu sửa chữa có thể tạo hợp đồng không
        /// </summary>
        /// <remarks>
        /// **Chức năng:**  
        /// - Kiểm tra báo cáo kiểm tra gần nhất của yêu cầu sửa chữa.
        /// - Trả về `true` nếu báo cáo có giải pháp là **Outsource** và đã được phê duyệt.
        /// 
        /// **Use case:**  
        /// - Validate trước khi hiển thị form tạo hợp đồng.
        /// - Kiểm tra điều kiện nghiệp vụ.
        /// </remarks>
        /// <param name="repairRequestId">ID yêu cầu sửa chữa</param>
        /// <returns>True nếu có thể tạo hợp đồng, False nếu không</returns>
        /// <response code="200">Kiểm tra thành công</response>
        /// <response code="401">Không có quyền truy cập</response>
        [HttpGet("can-create/{repairRequestId}")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)},{nameof(AccountRole.Admin)}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CanCreateContract([FromRoute] int repairRequestId)
        {
            var result = await _contractService.CanCreateContractAsync(repairRequestId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết hợp đồng theo ID
        /// </summary>
        /// <remarks>
        /// **Chức năng:**  
        /// - Trả về thông tin đầy đủ của hợp đồng.
        /// - Bao gồm thông tin file PDF đính kèm.
        /// - Có thể download file qua endpoint `/api/files/view/{key}`.
        /// 
        /// **Kết quả:**  
        /// - `ContractFile`: Chứa `FilePath` (key trên S3) để download file.
        /// </remarks>
        /// <param name="id">ID hợp đồng</param>
        /// <returns>Thông tin chi tiết hợp đồng</returns>
        /// <response code="200">Lấy thông tin thành công</response>
        /// <response code="404">Không tìm thấy hợp đồng</response>
        /// <response code="401">Không có quyền truy cập</response>
        [HttpGet("{id}")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)},{nameof(AccountRole.Admin)},{nameof(AccountRole.Resident)}")]
        [ProducesResponseType(typeof(ContractDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetContractById([FromRoute] int id)
        {
            var result = await _contractService.GetContractByIdAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách hợp đồng theo yêu cầu sửa chữa
        /// </summary>
        /// <remarks>
        /// **Chức năng:**  
        /// - Lấy tất cả hợp đồng liên quan đến một yêu cầu sửa chữa.
        /// - Sắp xếp theo thời gian tạo giảm dần.
        /// 
        /// **Use case:**  
        /// - Hiển thị lịch sử hợp đồng của một yêu cầu sửa chữa.
        /// - Theo dõi các lần ký hợp đồng với nhà thầu.
        /// </remarks>
        /// <param name="repairRequestId">ID yêu cầu sửa chữa</param>
        /// <returns>Danh sách hợp đồng</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        /// <response code="401">Không có quyền truy cập</response>
        [HttpGet("by-repair-request/{repairRequestId}")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)},{nameof(AccountRole.Admin)},{nameof(AccountRole.Resident)}")]
        [ProducesResponseType(typeof(IEnumerable<ContractDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetContractsByRepairRequestId([FromRoute] int repairRequestId)
        {
            var result = await _contractService.GetContractsByRepairRequestIdAsync(repairRequestId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách hợp đồng có phân trang và tìm kiếm
        /// </summary>
        /// <remarks>
        /// **Chức năng:**  
        /// - Lấy danh sách hợp đồng với phân trang.
        /// - Hỗ trợ tìm kiếm và lọc theo trạng thái.
        /// 
        /// **Tham số lọc:**  
        /// - `page`: Số trang (mặc định = 1)
        /// - `size`: Số bản ghi trên trang (mặc định = 10)
        /// - `search`: Tìm kiếm theo mã hợp đồng, tên nhà thầu, mô tả
        /// - `filter`: Lọc theo trạng thái (Active, Inactive)
        /// - `sortBy`: Sắp xếp (id, id_desc, date, date_desc, code, code_desc)
        /// </remarks>
        /// <param name="dto">Tham số phân trang và lọc</param>
        /// <returns>Danh sách hợp đồng có phân trang</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        /// <response code="401">Không có quyền truy cập</response>
        [HttpGet("paginate")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)},{nameof(AccountRole.Admin)}")]
        [ProducesResponseType(typeof(IPaginate<ContractDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetPaginateContracts([FromQuery] PaginateDto dto)
        {
            var result = await _contractService.GetPaginateContractsAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật thông tin hợp đồng
        /// </summary>
        /// <remarks>
        /// **Chức năng:**  
        /// - Cập nhật thông tin hợp đồng.
        /// - Có thể thay thế file PDF hợp đồng.
        /// 
        /// **Ràng buộc:**  
        /// - Không thể cập nhật hợp đồng đã bị vô hiệu hóa.
        /// - File mới phải là định dạng PDF.
        /// 
        /// **Lưu ý:**  
        /// - Khi upload file mới, file cũ sẽ bị đánh dấu Inactive.
        /// - Tất cả tham số đều là optional.
        /// </remarks>
        /// <param name="id">ID hợp đồng</param>
        /// <param name="dto">Thông tin cập nhật</param>
        /// <returns>Thông báo cập nhật thành công</returns>
        /// <response code="200">Cập nhật thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ hoặc hợp đồng đã vô hiệu hóa</response>
        /// <response code="404">Không tìm thấy hợp đồng</response>
        /// <response code="401">Không có quyền truy cập</response>
        /// <response code="500">Lỗi hệ thống</response>
        [HttpPut("{id}")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)},{nameof(AccountRole.Admin)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateContract([FromRoute] int id, [FromForm] ContractUpdateDto dto)
        {
            var result = await _contractService.UpdateContractAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Vô hiệu hóa hợp đồng
        /// </summary>
        /// <remarks>
        /// **Chức năng:**  
        /// - Đánh dấu hợp đồng là Inactive.
        /// - Hợp đồng vẫn tồn tại trong hệ thống nhưng không hoạt động.
        /// 
        /// **Ràng buộc:**  
        /// - Không thể vô hiệu hóa hợp đồng đã bị vô hiệu hóa.
        /// 
        /// **Use case:**  
        /// - Hủy hợp đồng khi nhà thầu vi phạm.
        /// - Kết thúc hợp đồng sớm.
        /// </remarks>
        /// <param name="id">ID hợp đồng</param>
        /// <returns>Thông báo vô hiệu hóa thành công</returns>
        /// <response code="200">Vô hiệu hóa thành công</response>
        /// <response code="400">Hợp đồng đã bị vô hiệu hóa</response>
        /// <response code="404">Không tìm thấy hợp đồng</response>
        /// <response code="401">Không có quyền truy cập</response>
        [HttpPatch("{id}/inactivate")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)},{nameof(AccountRole.Admin)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> InactivateContract([FromRoute] int id)
        {
            var result = await _contractService.InactivateContractAsync(id);
            return Ok(result);
        }
    }
}