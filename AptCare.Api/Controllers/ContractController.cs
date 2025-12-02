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
        /// Tạo hợp đồng cho yêu cầu sửa chữa với file PDF đính kèm.
        /// </summary>
        /// <remarks>
        /// <b>Chức năng:</b>
        /// <ul>
        ///   <li>Tạo hợp đồng thuê ngoài cho yêu cầu sửa chữa.</li>
        ///   <li>Tự động upload file PDF hợp đồng lên AWS S3.</li>
        ///   <li>Lưu thông tin file vào bảng Media.</li>
        /// </ul>
        /// <b>Ràng buộc:</b>
        /// <ul>
        ///   <li>Yêu cầu sửa chữa phải tồn tại.</li>
        ///   <li>Báo cáo kiểm tra gần nhất phải có giải pháp là <b>Outsource</b> (Thuê ngoài).</li>
        ///   <li>Mã hợp đồng phải duy nhất.</li>
        ///   <li>File phải là định dạng PDF.</li>
        /// </ul>
        /// <b>Tham số (<c>ContractCreateDto</c>):</b>
        /// <ul>
        ///   <li><b>RepairRequestId</b>: ID yêu cầu sửa chữa (bắt buộc).</li>
        ///   <li><b>ContractorName</b>: Tên nhà thầu (bắt buộc).</li>
        ///   <li><b>ContractCode</b>: Mã hợp đồng (bắt buộc, duy nhất).</li>
        ///   <li><b>StartDate</b>: Ngày bắt đầu hợp đồng (bắt buộc).</li>
        ///   <li><b>EndDate</b>: Ngày kết thúc hợp đồng (tùy chọn).</li>
        ///   <li><b>Amount</b>: Giá trị hợp đồng (tùy chọn).</li>
        ///   <li><b>Description</b>: Mô tả hợp đồng (bắt buộc).</li>
        ///   <li><b>ContractFile</b>: File PDF hợp đồng (bắt buộc).</li>
        /// </ul>
        /// </remarks>
        /// <param name="dto">
        /// <b>ContractCreateDto:</b>
        /// <ul>
        ///   <li><b>RepairRequestId</b>: ID yêu cầu sửa chữa liên kết.</li>
        ///   <li><b>ContractorName</b>: Tên nhà thầu.</li>
        ///   <li><b>ContractCode</b>: Mã hợp đồng.</li>
        ///   <li><b>StartDate</b>: Ngày bắt đầu hợp đồng.</li>
        ///   <li><b>EndDate</b>: Ngày kết thúc hợp đồng.</li>
        ///   <li><b>Amount</b>: Giá trị hợp đồng.</li>
        ///   <li><b>Description</b>: Mô tả hợp đồng.</li>
        ///   <li><b>ContractFile</b>: File PDF hợp đồng.</li>
        /// </ul>
        /// </param>
        /// <returns>
        /// <b>ContractDto:</b>
        /// <ul>
        ///   <li><b>ContractId</b>: ID hợp đồng.</li>
        ///   <li><b>RepairRequestId</b>: ID yêu cầu sửa chữa liên kết.</li>
        ///   <li><b>ContractorName</b>: Tên nhà thầu.</li>
        ///   <li><b>ContractCode</b>: Mã hợp đồng.</li>
        ///   <li><b>StartDate</b>: Ngày bắt đầu hợp đồng.</li>
        ///   <li><b>EndDate</b>: Ngày kết thúc hợp đồng.</li>
        ///   <li><b>Amount</b>: Giá trị hợp đồng.</li>
        ///   <li><b>Description</b>: Mô tả hợp đồng.</li>
        ///   <li><b>Status</b>: Trạng thái hợp đồng (Active/Inactive).</li>
        ///   <li><b>CreatedAt</b>: Thời gian tạo hợp đồng.</li>
        ///   <li><b>ContractFile</b>: Thông tin file PDF hợp đồng (MediaDto).</li>
        /// </ul>
        /// </returns>
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
        /// Kiểm tra xem yêu cầu sửa chữa có thể tạo hợp đồng không.
        /// </summary>
        /// <remarks>
        /// <b>Chức năng:</b>
        /// <ul>
        ///   <li>Kiểm tra báo cáo kiểm tra gần nhất của yêu cầu sửa chữa.</li>
        ///   <li>Trả về <b>true</b> nếu báo cáo có giải pháp là <b>Outsource</b> và đã được phê duyệt.</li>
        /// </ul>
        /// <b>Use case:</b>
        /// <ul>
        ///   <li>Validate trước khi hiển thị form tạo hợp đồng.</li>
        ///   <li>Kiểm tra điều kiện nghiệp vụ.</li>
        /// </ul>
        /// </remarks>
        /// <param name="repairRequestId">ID yêu cầu sửa chữa.</param>
        /// <returns>True nếu có thể tạo hợp đồng, False nếu không.</returns>
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
        /// Lấy thông tin chi tiết hợp đồng theo ID.
        /// </summary>
        /// <remarks>
        /// <b>Chức năng:</b>
        /// <ul>
        ///   <li>Trả về thông tin đầy đủ của hợp đồng.</li>
        ///   <li>Bao gồm thông tin file PDF đính kèm.</li>
        ///   <li>Có thể download file qua endpoint <c>/api/files/view/{key}</c>.</li>
        /// </ul>
        /// <b>Kết quả (<c>ContractDto</c>):</b>
        /// <ul>
        ///   <li><b>ContractFile</b>: Chứa <b>FilePath</b> (key trên S3) để download file.</li>
        /// </ul>
        /// </remarks>
        /// <param name="id">ID hợp đồng.</param>
        /// <returns>
        /// <b>ContractDto:</b>
        /// <ul>
        ///   <li><b>ContractId</b>: ID hợp đồng.</li>
        ///   <li><b>RepairRequestId</b>: ID yêu cầu sửa chữa liên kết.</li>
        ///   <li><b>ContractorName</b>: Tên nhà thầu.</li>
        ///   <li><b>ContractCode</b>: Mã hợp đồng.</li>
        ///   <li><b>StartDate</b>: Ngày bắt đầu hợp đồng.</li>
        ///   <li><b>EndDate</b>: Ngày kết thúc hợp đồng.</li>
        ///   <li><b>Amount</b>: Giá trị hợp đồng.</li>
        ///   <li><b>Description</b>: Mô tả hợp đồng.</li>
        ///   <li><b>Status</b>: Trạng thái hợp đồng (Active/Inactive).</li>
        ///   <li><b>CreatedAt</b>: Thời gian tạo hợp đồng.</li>
        ///   <li><b>ContractFile</b>: Thông tin file PDF hợp đồng (MediaDto).</li>
        /// </ul>
        /// </returns>
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
        /// Lấy danh sách hợp đồng theo yêu cầu sửa chữa.
        /// </summary>
        /// <remarks>
        /// <b>Chức năng:</b>
        /// <ul>
        ///   <li>Lấy tất cả hợp đồng liên quan đến một yêu cầu sửa chữa.</li>
        ///   <li>Sắp xếp theo thời gian tạo giảm dần.</li>
        /// </ul>
        /// <b>Use case:</b>
        /// <ul>
        ///   <li>Hiển thị lịch sử hợp đồng của một yêu cầu sửa chữa.</li>
        ///   <li>Theo dõi các lần ký hợp đồng với nhà thầu.</li>
        /// </ul>
        /// </remarks>
        /// <param name="repairRequestId">ID yêu cầu sửa chữa.</param>
        /// <returns>
        /// <b>ContractDto[]:</b>
        /// <ul>
        ///   <li><b>ContractId</b>: ID hợp đồng.</li>
        ///   <li><b>RepairRequestId</b>: ID yêu cầu sửa chữa liên kết.</li>
        ///   <li><b>ContractorName</b>: Tên nhà thầu.</li>
        ///   <li><b>ContractCode</b>: Mã hợp đồng.</li>
        ///   <li><b>StartDate</b>: Ngày bắt đầu hợp đồng.</li>
        ///   <li><b>EndDate</b>: Ngày kết thúc hợp đồng.</li>
        ///   <li><b>Amount</b>: Giá trị hợp đồng.</li>
        ///   <li><b>Description</b>: Mô tả hợp đồng.</li>
        ///   <li><b>Status</b>: Trạng thái hợp đồng (Active/Inactive).</li>
        ///   <li><b>CreatedAt</b>: Thời gian tạo hợp đồng.</li>
        ///   <li><b>ContractFile</b>: Thông tin file PDF hợp đồng (MediaDto).</li>
        /// </ul>
        /// </returns>
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
        /// Lấy danh sách hợp đồng có phân trang và tìm kiếm.
        /// </summary>
        /// <remarks>
        /// <b>Chức năng:</b>
        /// <ul>
        ///   <li>Lấy danh sách hợp đồng với phân trang.</li>
        ///   <li>Hỗ trợ tìm kiếm và lọc theo trạng thái.</li>
        /// </ul>
        /// <b>Tham số lọc (<c>PaginateDto</c>):</b>
        /// <ul>
        ///   <li><b>page</b>: Số trang (mặc định = 1).</li>
        ///   <li><b>size</b>: Số bản ghi trên trang (mặc định = 10).</li>
        ///   <li><b>search</b>: Tìm kiếm theo mã hợp đồng, tên nhà thầu, mô tả.</li>
        ///   <li><b>filter</b>: Lọc theo trạng thái (Active, Inactive).</li>
        ///   <li><b>sortBy</b>: Sắp xếp (id, id_desc, date, date_desc, code, code_desc).</li>
        /// </ul>
        /// </remarks>
        /// <param name="dto">
        /// <b>PaginateDto:</b>
        /// <ul>
        ///   <li><b>page</b>: Số trang hiện tại.</li>
        ///   <li><b>size</b>: Số bản ghi mỗi trang.</li>
        ///   <li><b>search</b>: Từ khóa tìm kiếm.</li>
        ///   <li><b>filter</b>: Lọc theo trạng thái.</li>
        ///   <li><b>sortBy</b>: Tiêu chí sắp xếp.</li>
        /// </ul>
        /// </param>
        /// <returns>
        /// <b>IPaginate&lt;ContractDto&gt;:</b>
        /// <ul>
        ///   <li><b>ContractId</b>: ID hợp đồng.</li>
        ///   <li><b>RepairRequestId</b>: ID yêu cầu sửa chữa liên kết.</li>
        ///   <li><b>ContractorName</b>: Tên nhà thầu.</li>
        ///   <li><b>ContractCode</b>: Mã hợp đồng.</li>
        ///   <li><b>StartDate</b>: Ngày bắt đầu hợp đồng.</li>
        ///   <li><b>EndDate</b>: Ngày kết thúc hợp đồng.</li>
        ///   <li><b>Amount</b>: Giá trị hợp đồng.</li>
        ///   <li><b>Description</b>: Mô tả hợp đồng.</li>
        ///   <li><b>Status</b>: Trạng thái hợp đồng (Active/Inactive).</li>
        ///   <li><b>CreatedAt</b>: Thời gian tạo hợp đồng.</li>
        ///   <li><b>ContractFile</b>: Thông tin file PDF hợp đồng (MediaDto).</li>
        /// </ul>
        /// </returns>
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
        /// Cập nhật thông tin hợp đồng.
        /// </summary>
        /// <remarks>
        /// <b>Chức năng:</b>
        /// <ul>
        ///   <li>Cập nhật thông tin hợp đồng.</li>
        ///   <li>Có thể thay thế file PDF hợp đồng.</li>
        /// </ul>
        /// <b>Ràng buộc:</b>
        /// <ul>
        ///   <li>Không thể cập nhật hợp đồng đã bị vô hiệu hóa.</li>
        ///   <li>File mới phải là định dạng PDF.</li>
        /// </ul>
        /// <b>Lưu ý:</b>
        /// <ul>
        ///   <li>Khi upload file mới, file cũ sẽ bị đánh dấu Inactive.</li>
        ///   <li>Tất cả tham số đều là optional.</li>
        /// </ul>
        /// <b>Tham số (<c>ContractUpdateDto</c>):</b>
        /// <ul>
        ///   <li><b>ContractorName</b>: Tên nhà thầu (tùy chọn).</li>
        ///   <li><b>StartDate</b>: Ngày bắt đầu hợp đồng (tùy chọn).</li>
        ///   <li><b>EndDate</b>: Ngày kết thúc hợp đồng (tùy chọn).</li>
        ///   <li><b>Amount</b>: Giá trị hợp đồng (tùy chọn).</li>
        ///   <li><b>Description</b>: Mô tả hợp đồng (tùy chọn).</li>
        ///   <li><b>ContractFile</b>: File PDF hợp đồng mới (tùy chọn).</li>
        /// </ul>
        /// </remarks>
        /// <param name="id">ID hợp đồng.</param>
        /// <param name="dto">
        /// <b>ContractUpdateDto:</b>
        /// <ul>
        ///   <li><b>ContractorName</b>: Tên nhà thầu.</li>
        ///   <li><b>StartDate</b>: Ngày bắt đầu hợp đồng.</li>
        ///   <li><b>EndDate</b>: Ngày kết thúc hợp đồng.</li>
        ///   <li><b>Amount</b>: Giá trị hợp đồng.</li>
        ///   <li><b>Description</b>: Mô tả hợp đồng.</li>
        ///   <li><b>ContractFile</b>: File PDF hợp đồng mới.</li>
        /// </ul>
        /// </param>
        /// <returns>Thông báo cập nhật thành công.</returns>
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
        /// Vô hiệu hóa hợp đồng.
        /// </summary>
        /// <remarks>
        /// <b>Chức năng:</b>
        /// <ul>
        ///   <li>Đánh dấu hợp đồng là Inactive.</li>
        ///   <li>Hợp đồng vẫn tồn tại trong hệ thống nhưng không hoạt động.</li>
        /// </ul>
        /// <b>Ràng buộc:</b>
        /// <ul>
        ///   <li>Không thể vô hiệu hóa hợp đồng đã bị vô hiệu hóa.</li>
        /// </ul>
        /// <b>Use case:</b>
        /// <ul>
        ///   <li>Hủy hợp đồng khi nhà thầu vi phạm.</li>
        ///   <li>Kết thúc hợp đồng sớm.</li>
        /// </ul>
        /// </remarks>
        /// <param name="id">ID hợp đồng.</param>
        /// <returns>Thông báo vô hiệu hóa thành công.</returns>
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