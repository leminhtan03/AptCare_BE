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
        public async Task<ActionResult> GetPaginateApartment([FromQuery] PaginateDto dto, int? floorId)
        {
            var result = await _apartmentService.GetPaginateApartmentAsync(dto, floorId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách căn hộ theo ID tầng.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** tất cả người dùng đã đăng nhập.  
        ///  
        /// Dùng khi cần hiển thị các căn hộ thuộc một tầng cụ thể.
        /// </remarks>
        /// <param name="floorId">ID của tầng cần lấy danh sách căn hộ.</param>
        /// <returns>Danh sách căn hộ thuộc tầng đó.</returns>
        /// <response code="200">Trả về danh sách căn hộ theo tầng.</response>
        /// <response code="404">Không tìm thấy tầng.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet("by-floor/{floorId}")]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<ApartmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetApartmentsByFloor(int floorId)
        {
            var result = await _apartmentService.GetApartmentsByFloorAsync(floorId);
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

        ///// <summary>
        ///// Tạo mới một căn hộ.
        ///// </summary>
        ///// <remarks>
        ///// **Chỉ role:** Manager  
        /////  
        ///// Dữ liệu yêu cầu gồm: số căn, mô tả, ID tầng.
        ///// </remarks>
        ///// <param name="dto">Thông tin căn hộ cần tạo.</param>
        ///// <returns>Thông báo tạo căn hộ thành công.</returns>
        ///// <response code="200">Căn hộ được tạo thành công.</response>
        ///// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        ///// <response code="401">Không có quyền truy cập.</response>
        ///// <response code="403">Không đủ quyền truy cập.</response>
        //[HttpPost]
        //[Authorize(Roles = nameof(AccountRole.Manager))]
        //[ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        //public async Task<ActionResult> CreateApartment(ApartmentCreateDto dto)
        //{
        //    var result = await _apartmentService.CreateApartmentAsync(dto);
        //    return Created(string.Empty, result);
        //}

        ///// <summary>
        ///// Cập nhật thông tin căn hộ theo ID.
        ///// </summary>
        ///// <remarks>
        ///// **Chỉ role:** Manager  
        /////  
        ///// Cập nhật thông tin như: mô tả, trạng thái, số căn, ID tầng.
        ///// </remarks>
        ///// <param name="id">ID của căn hộ cần cập nhật.</param>
        ///// <param name="dto">Thông tin căn hộ cập nhật.</param>
        ///// <returns>Thông báo cập nhật thành công.</returns>
        ///// <response code="200">Cập nhật căn hộ thành công.</response>
        ///// <response code="404">Không tìm thấy căn hộ.</response>
        ///// <response code="401">Không có quyền truy cập.</response>
        ///// <response code="403">Không đủ quyền truy cập.</response>
        //[HttpPut("{id}")]
        //[Authorize(Roles = nameof(AccountRole.Manager))]
        //[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        //public async Task<ActionResult> UpdateApartment(int id, ApartmentUpdateDto dto)
        //{
        //    var result = await _apartmentService.UpdateApartmentAsync(id, dto);
        //    return Ok(result);
        //}

        ///// <summary>
        ///// Xóa một căn hộ theo ID.
        ///// </summary>
        ///// <remarks>
        ///// **Chỉ role:** Manager  
        /////  
        ///// Xóa căn hộ sẽ không ảnh hưởng đến các dữ liệu khác ngoài quan hệ trực tiếp (nếu có).
        ///// </remarks>
        ///// <param name="id">ID của căn hộ cần xóa.</param>
        ///// <returns>Thông báo xóa thành công.</returns>
        ///// <response code="200">Căn hộ được xóa thành công.</response>
        ///// <response code="404">Không tìm thấy căn hộ.</response>
        ///// <response code="401">Không có quyền truy cập.</response>
        ///// <response code="403">Không đủ quyền truy cập.</response>
        //[HttpDelete("{id:int}")]
        //[Authorize(Roles = nameof(AccountRole.Manager))]
        //[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        //public async Task<ActionResult> DeleteApartment(int id)
        //{
        //    var result = await _apartmentService.DeleteApartmentAsync(id);
        //    return Ok(result);
        //}

        /// <summary>
        /// Cập nhật thông tin căn hộ và quản lý thành viên (thêm/sửa/xóa).
        /// </summary>
        /// <remarks>
        /// <para>Endpoint này cho phép cập nhật toàn diện thông tin căn hộ và quản lý danh sách cư dân:</para>
        /// 
        /// <para><strong>Chức năng quản lý thành viên:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Thêm mới:</strong> User mới trong danh sách Residents sẽ được thêm vào căn hộ</description></item>
        /// <item><description><strong>Cập nhật:</strong> User đã có trong căn hộ sẽ cập nhật Role/Relationship</description></item>
        /// <item><description><strong>Xóa:</strong> User không còn trong danh sách sẽ bị xóa khỏi căn hộ (soft delete)</description></item>
        /// </list>
        /// 
        /// <para><strong>Validation Rules:</strong></para>
        /// <list type="number">
        /// <item><description><strong>Owner Constraint:</strong> Căn hộ PHẢI có đúng 1 Owner (không nhiều hơn, không ít hơn)</description></item>
        /// <item><description><strong>Limit Constraint:</strong> Số lượng cư dân không được vượt quá Limit của căn hộ</description></item>
        /// <item><description><strong>Owner Protection:</strong> Không cho phép xóa Owner trực tiếp (phải chuyển quyền trước)</description></item>
        /// <item><description><strong>User Validation:</strong> Tất cả UserId phải tồn tại và active</description></item>
        /// </list>
        /// 
        /// <para><strong>Ví dụ request - Cập nhật members:</strong></para>

        /// <para><strong>Error cases:</strong></para>
        /// <list type="bullet">
        /// <item><description>❌ Không có Owner trong danh sách → "Căn hộ phải có ít nhất một chủ sở hữu"</description></item>
        /// <item><description>❌ Có 2 Owners → "Căn hộ chỉ có thể có một chủ sở hữu"</description></item>
        /// <item><description>❌ Vượt quá Limit → "Số lượng cư dân vượt quá giới hạn"</description></item>
        /// <item><description>❌ Xóa Owner → "Không thể xóa chủ sở hữu. Vui lòng chuyển quyền trước"</description></item>
        /// </remarks>
        [HttpPut("with-resident/{aptId}")]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(ApartmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> UpdateApartmentWithResidentData(int aptId, UpdateApartmentWithResidentDataDto dto)
        {
            var result = await _apartmentService.UpadteUserDataForAptAsync(aptId, dto);
            return Ok(result);
        }
    }
}
