using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.AccessoryDto;
using AptCare.Service.Services.Interfaces;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    public class AccessoryController : BaseApiController
    {
        private readonly IAccessoryService _accessoryService;

        public AccessoryController(IAccessoryService accessoryService)
        {
            _accessoryService = accessoryService;
        }

        /// <summary>
        /// Tạo phụ kiện mới.
        /// </summary>
        /// <remarks>
        /// Chỉ các role: Manager hoặc TechnicianLead.  
        /// Body: `AccessoryCreateDto` (Name, Descrption, Price, Quantity)
        /// </remarks>
        /// <response code="201">Tạo thành công, trả về thông điệp.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền (Unauthorized).</response>
        /// <response code="403">Bị cấm (Forbidden).</response>
        [HttpPost]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)}, {nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> CreateAccessory([FromBody] AccessoryCreateDto dto)
        {
            var result = await _accessoryService.CreateAccessoryAsync(dto);
            return Created(string.Empty, result);
        }

        /// <summary>
        /// Cập nhật phụ kiện.
        /// </summary>
        /// <remarks>
        /// Chỉ các role: Manager hoặc TechnicianLead.  
        /// Body: `AccessoryUpdateDto` (Name, Descrption, Price, Quantity, Status)
        /// </remarks>
        /// <param name="id">ID phụ kiện cần cập nhật.</param>
        /// <response code="200">Cập nhật thành công, trả về thông điệp.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền.</response>
        /// <response code="403">Bị cấm.</response>
        /// <response code="404">Không tìm thấy phụ kiện.</response>
        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)}, {nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> UpdateAccessory(int id, [FromBody] AccessoryUpdateDto dto)
        {
            var result = await _accessoryService.UpdateAccessoryAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Xóa phụ kiện (hoặc mark soft-delete tuỳ implement).
        /// </summary>
        /// <remarks>
        /// Chỉ các role: Manager hoặc TechnicianLead.
        /// </remarks>
        /// <param name="id">ID phụ kiện cần xóa.</param>
        /// <response code="200">Xóa thành công, trả về thông điệp.</response>
        /// <response code="401">Không có quyền.</response>
        /// <response code="403">Bị cấm.</response>
        /// <response code="404">Không tìm thấy phụ kiện.</response>
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
        /// Lấy chi tiết một phụ kiện theo ID.
        /// </summary>
        /// <remarks>
        /// Role: TechnicianLead, Manager, Technician.
        /// </remarks>
        /// <param name="id">ID phụ kiện.</param>
        /// <response code="200">Trả về thông tin phụ kiện.</response>
        /// <response code="401">Không có quyền.</response>
        /// <response code="404">Không tìm thấy phụ kiện.</response>
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
        /// Lấy danh sách phụ kiện có phân trang, tìm kiếm và lọc.
        /// </summary>
        /// <remarks>
        /// Role: TechnicianLead, Manager, Technician.  
        ///
        /// Query parameters (được đóng gói trong `PaginateDto` hoặc truyền trực tiếp):
        /// - page (int, optional): số trang, bắt đầu từ 1. Mặc định 1.  
        /// - size (int, optional): số bản ghi mỗi trang. Mặc định 10.  
        /// - search (string, optional): tìm kiếm theo `Name` và `Descrption` (không phân biệt hoa thường).  
        /// - filter (string, optional): lọc theo trạng thái, giá trị hợp lệ:
        ///     - "active"  => chỉ trả về phụ kiện có `Status = Active`  
        ///     - "inactive" => chỉ trả về phụ kiện có `Status = Inactive`  
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
        /// Lấy danh sách phụ kiện (chỉ trạng thái Active) để hiển thị dropdown hoặc sử dụng nội bộ.
        /// </summary>
        /// <remarks>
        /// Role: TechnicianLead, Manager, Technician.   
        /// Trả về danh sách sắp xếp theo `Name`.
        /// </remarks>
        /// <response code="200">Trả về `IEnumerable<AccessoryDto>`.</response>
        /// <response code="401">Không có quyền.</response>
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
    }
}