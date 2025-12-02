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
        /// Tạo mới một linh kiện.
        /// </summary>
        /// <remarks>
        /// Chỉ dành cho các vai trò: Manager hoặc TechnicianLead.  
        /// Body: `AccessoryCreateDto` (Name, Description, Price, Quantity)
        /// </remarks>
        /// <response code="201">Tạo thành công, trả về thông điệp.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Bị từ chối truy cập.</response>
        [HttpPost]
        [Authorize(Roles = $"{nameof(AccountRole.Manager)}, {nameof(AccountRole.TechnicianLead)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> CreateAccessory([FromForm] AccessoryCreateDto dto)
        {
            var result = await _accessoryService.CreateAccessoryAsync(dto);
            return Created(string.Empty, result);
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
    }
}