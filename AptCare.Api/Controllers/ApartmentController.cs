using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    public class ApartmentController : BaseApiController
    {
        private readonly IApartmentService _apartmentService;

        public ApartmentController(IApartmentService apartmentService)
        {
            _apartmentService = apartmentService;
        }

        /// <summary>
        /// Lấy danh sách căn hộ có phân trang, tìm kiếm và sắp xếp.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** tất cả người dùng đã đăng nhập.    
        ///  
        /// **Tham số phân trang (PaginateDto):**
        /// - <b>page</b>: Số trang hiện tại (bắt đầu từ 1).  
        /// - <b>size</b>: Số bản ghi mỗi trang.  
        /// - <b>search</b>: Từ khóa tìm kiếm (theo số căn, mô tả).  
        /// - <b>filter</b>: Lọc theo trạng thái căn hộ.  
        /// - <b>sortBy</b>: Tiêu chí sắp xếp kết quả:
        ///   - <b>number</b>: Sắp xếp theo số căn tăng dần.  
        ///   - <b>number_desc</b>: Sắp xếp theo số căn giảm dần.  
        /// </remarks>
        /// <param name="dto">Thông tin phân trang, tìm kiếm, sắp xếp và lọc.</param>
        /// <returns>Danh sách căn hộ kèm thông tin phân trang.</returns>
        /// <response code="200">Trả về danh sách căn hộ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(IPaginate<ApartmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetPaginateApartment([FromQuery] PaginateDto dto)
        {
            var result = await _apartmentService.GetPaginateApartmentAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một căn hộ theo ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** tất cả người dùng đã đăng nhập.  
        /// </remarks>
        /// <param name="id">ID của căn hộ cần lấy thông tin.</param>
        /// <returns>Thông tin chi tiết của căn hộ.</returns>
        /// <response code="200">Trả về thông tin căn hộ.</response>
        /// <response code="404">Không tìm thấy căn hộ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ApartmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetApartmentById(int id)
        {
            var result = await _apartmentService.GetApartmentByIdAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Tạo mới một căn hộ.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager  
        ///  
        /// Dữ liệu yêu cầu gồm: số căn, mô tả, ID tầng.
        /// </remarks>
        /// <param name="dto">Thông tin căn hộ cần tạo.</param>
        /// <returns>Thông báo tạo căn hộ thành công.</returns>
        /// <response code="200">Căn hộ được tạo thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpPost]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> CreateApartment(ApartmentCreateDto dto)
        {
            var result = await _apartmentService.CreateApartmentAsync(dto);
            return Created(string.Empty, result);
        }

        /// <summary>
        /// Cập nhật thông tin căn hộ theo ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager  
        ///  
        /// Cập nhật thông tin như: mô tả, trạng thái, số căn, ID tầng.
        /// </remarks>
        /// <param name="id">ID của căn hộ cần cập nhật.</param>
        /// <param name="dto">Thông tin căn hộ cập nhật.</param>
        /// <returns>Thông báo cập nhật thành công.</returns>
        /// <response code="200">Cập nhật căn hộ thành công.</response>
        /// <response code="404">Không tìm thấy căn hộ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpPut("{id}")]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> UpdateApartment(int id, ApartmentUpdateDto dto)
        {
            var result = await _apartmentService.UpdateApartmentAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Xóa một căn hộ theo ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager  
        ///  
        /// Xóa căn hộ sẽ không ảnh hưởng đến các dữ liệu khác ngoài quan hệ trực tiếp (nếu có).
        /// </remarks>
        /// <param name="id">ID của căn hộ cần xóa.</param>
        /// <returns>Thông báo xóa thành công.</returns>
        /// <response code="200">Căn hộ được xóa thành công.</response>
        /// <response code="404">Không tìm thấy căn hộ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> DeleteApartment(int id)
        {
            var result = await _apartmentService.DeleteApartmentAsync(id);
            return Ok(result);
        }
    }
}
