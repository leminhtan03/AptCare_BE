using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.MaintenanceTaskDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    public class MaintenanceTaskController : BaseApiController
    {
        private readonly IMaintenanceTaskService _maintenanceTaskService;

        public MaintenanceTaskController(IMaintenanceTaskService maintenanceTaskService)
        {
            _maintenanceTaskService = maintenanceTaskService;
        }        

        /// <summary>
        /// Lấy danh sách nhiệm vụ bảo trì theo loại đối tượng.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> tất cả người dùng đã đăng nhập.<br/>
        /// Dùng khi cần hiển thị các nhiệm vụ bảo trì thuộc một loại đối tượng cụ thể (đang hoạt động).
        /// </remarks>
        /// <param name="commonAreaObjectTypeId">ID của loại đối tượng khu vực chung.</param>
        /// <returns>
        /// <b>MaintenanceTaskBasicDto[]:</b>
        /// <ul>
        ///   <li><b>MaintenanceTaskId</b>: ID nhiệm vụ bảo trì.</li>
        ///   <li><b>CommonAreaObjectTypeId</b>: ID loại đối tượng.</li>
        ///   <li><b>TaskName</b>: Tên nhiệm vụ.</li>
        ///   <li><b>TaskDescription</b>: Mô tả nhiệm vụ.</li>
        ///   <li><b>DisplayOrder</b>: Thứ tự hiển thị.</li>
        ///   <li><b>EstimatedDurationMinutes</b>: Thời gian dự kiến (phút).</li>
        ///   <li><b>Status</b>: Trạng thái nhiệm vụ.</li>
        /// </ul>
        /// </returns>
        /// <response code="200">Trả về danh sách nhiệm vụ theo loại đối tượng.</response>
        /// <response code="404">Không tìm thấy loại đối tượng khu vực chung.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet("by-type/{commonAreaObjectTypeId}")]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<MaintenanceTaskBasicDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetMaintenanceTasksByType(int commonAreaObjectTypeId)
        {
            var result = await _maintenanceTaskService.GetMaintenanceTasksByTypeAsync(commonAreaObjectTypeId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một nhiệm vụ bảo trì theo ID.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> tất cả người dùng đã đăng nhập.
        /// </remarks>
        /// <param name="id">ID của nhiệm vụ bảo trì cần lấy thông tin.</param>
        /// <returns>
        /// <b>MaintenanceTaskDto:</b>
        /// <ul>
        ///   <li><b>MaintenanceTaskId</b>: ID nhiệm vụ bảo trì.</li>
        ///   <li><b>CommonAreaObjectTypeId</b>: ID loại đối tượng khu vực chung.</li>
        ///   <li><b>TaskName</b>: Tên nhiệm vụ.</li>
        ///   <li><b>TaskDescription</b>: Mô tả nhiệm vụ.</li>
        ///   <li><b>RequiredTools</b>: Công cụ yêu cầu.</li>
        ///   <li><b>DisplayOrder</b>: Thứ tự hiển thị.</li>
        ///   <li><b>EstimatedDurationMinutes</b>: Thời gian dự kiến (phút).</li>
        ///   <li><b>Status</b>: Trạng thái (Active/Inactive).</li>
        ///   <li><b>CommonAreaObjectType</b>: Thông tin loại đối tượng liên kết.</li>
        /// </ul>
        /// </returns>
        /// <response code="200">Trả về thông tin nhiệm vụ bảo trì.</response>
        /// <response code="404">Không tìm thấy nhiệm vụ bảo trì.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(MaintenanceTaskDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetMaintenanceTaskById(int id)
        {
            var result = await _maintenanceTaskService.GetMaintenanceTaskByIdAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Tạo mới một nhiệm vụ bảo trì.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> Manager<br/>
        /// <b>Dữ liệu yêu cầu (<c>MaintenanceTaskCreateDto</c>):</b>
        /// <ul>
        ///   <li><b>CommonAreaObjectTypeId</b>: ID loại đối tượng khu vực chung (bắt buộc).</li>
        ///   <li><b>TaskName</b>: Tên nhiệm vụ (bắt buộc, tối đa 256 ký tự, không trùng lặp trong cùng loại đối tượng).</li>
        ///   <li><b>TaskDescription</b>: Mô tả nhiệm vụ (tối đa 1000 ký tự, tùy chọn).</li>
        ///   <li><b>RequiredTools</b>: Công cụ yêu cầu (tối đa 500 ký tự, tùy chọn).</li>
        ///   <li><b>DisplayOrder</b>: Thứ tự hiển thị (>= 0).</li>
        ///   <li><b>EstimatedDurationMinutes</b>: Thời gian dự kiến tính bằng phút (> 0, bắt buộc).</li>
        /// </ul>
        /// Trạng thái sẽ tự động được đặt là Active khi tạo mới.
        /// </remarks>
        /// <param name="dto">
        /// <b>MaintenanceTaskCreateDto:</b>
        /// <ul>
        ///   <li><b>CommonAreaObjectTypeId</b>: ID loại đối tượng.</li>
        ///   <li><b>TaskName</b>: Tên nhiệm vụ.</li>
        ///   <li><b>TaskDescription</b>: Mô tả nhiệm vụ.</li>
        ///   <li><b>RequiredTools</b>: Công cụ yêu cầu.</li>
        ///   <li><b>DisplayOrder</b>: Thứ tự hiển thị.</li>
        ///   <li><b>EstimatedDurationMinutes</b>: Thời gian dự kiến (phút).</li>
        /// </ul>
        /// </param>
        /// <returns>Thông báo tạo nhiệm vụ bảo trì thành công.</returns>
        /// <response code="201">Nhiệm vụ bảo trì được tạo thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="404">Không tìm thấy loại đối tượng khu vực chung.</response>
        /// <response code="409">Tên nhiệm vụ đã tồn tại cho loại đối tượng này.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpPost]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> CreateMaintenanceTask(MaintenanceTaskCreateDto dto)
        {
            var result = await _maintenanceTaskService.CreateMaintenanceTaskAsync(dto);
            return Created(string.Empty, result);
        }

        /// <summary>
        /// Cập nhật thông tin nhiệm vụ bảo trì theo ID.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> Manager<br/>
        /// <b>Dữ liệu cập nhật (<c>MaintenanceTaskUpdateDto</c>):</b>
        /// <ul>
        ///   <li><b>CommonAreaObjectTypeId</b>: ID loại đối tượng khu vực chung (bắt buộc).</li>
        ///   <li><b>TaskName</b>: Tên nhiệm vụ (bắt buộc, tối đa 256 ký tự).</li>
        ///   <li><b>TaskDescription</b>: Mô tả nhiệm vụ (tối đa 1000 ký tự, tùy chọn).</li>
        ///   <li><b>RequiredTools</b>: Công cụ yêu cầu (tối đa 500 ký tự, tùy chọn).</li>
        ///   <li><b>DisplayOrder</b>: Thứ tự hiển thị (>= 0).</li>
        ///   <li><b>EstimatedDurationMinutes</b>: Thời gian dự kiến (phút, > 0).</li>
        ///   <li><b>Status</b>: Trạng thái nhiệm vụ (Active/Inactive).</li>
        /// </ul>
        /// </remarks>
        /// <param name="id">ID của nhiệm vụ bảo trì cần cập nhật.</param>
        /// <param name="dto">
        /// <b>MaintenanceTaskUpdateDto:</b>
        /// <ul>
        ///   <li><b>CommonAreaObjectTypeId</b>: ID loại đối tượng.</li>
        ///   <li><b>TaskName</b>: Tên nhiệm vụ.</li>
        ///   <li><b>TaskDescription</b>: Mô tả nhiệm vụ.</li>
        ///   <li><b>RequiredTools</b>: Công cụ yêu cầu.</li>
        ///   <li><b>DisplayOrder</b>: Thứ tự hiển thị.</li>
        ///   <li><b>EstimatedDurationMinutes</b>: Thời gian dự kiến (phút).</li>
        ///   <li><b>Status</b>: Trạng thái.</li>
        /// </ul>
        /// </param>
        /// <returns>Thông báo cập nhật thành công.</returns>
        /// <response code="200">Cập nhật nhiệm vụ bảo trì thành công.</response>
        /// <response code="404">Không tìm thấy nhiệm vụ bảo trì hoặc loại đối tượng.</response>
        /// <response code="409">Tên nhiệm vụ đã tồn tại cho loại đối tượng này.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpPut("{id}")]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> UpdateMaintenanceTask(int id, MaintenanceTaskUpdateDto dto)
        {
            var result = await _maintenanceTaskService.UpdateMaintenanceTaskAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Xóa một nhiệm vụ bảo trì theo ID.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> Manager<br/>
        /// <b>Lưu ý:</b> Không thể xóa nếu nhiệm vụ bảo trì đang có yêu cầu sửa chữa liên kết.
        /// </remarks>
        /// <param name="id">ID của nhiệm vụ bảo trì cần xóa.</param>
        /// <returns>Thông báo xóa thành công.</returns>
        /// <response code="200">Nhiệm vụ bảo trì được xóa thành công.</response>
        /// <response code="404">Không tìm thấy nhiệm vụ bảo trì.</response>
        /// <response code="400">Không thể xóa do có dữ liệu liên kết.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpDelete("{id}")]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> DeleteMaintenanceTask(int id)
        {
            var result = await _maintenanceTaskService.DeleteMaintenanceTaskAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Kích hoạt nhiệm vụ bảo trì.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> Manager<br/>
        /// Chuyển trạng thái của nhiệm vụ bảo trì từ Inactive sang Active.<br/>
        /// <b>Lưu ý:</b> Không thể kích hoạt nếu loại đối tượng cha đã bị vô hiệu hóa.
        /// </remarks>
        /// <param name="id">ID của nhiệm vụ bảo trì cần kích hoạt.</param>
        /// <returns>Thông báo kích hoạt thành công.</returns>
        /// <response code="200">Kích hoạt nhiệm vụ bảo trì thành công.</response>
        /// <response code="404">Không tìm thấy nhiệm vụ bảo trì.</response>
        /// <response code="400">Nhiệm vụ đã ở trạng thái hoạt động hoặc loại đối tượng cha đã bị vô hiệu hóa.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpPatch("{id}/activate")]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> ActivateMaintenanceTask(int id)
        {
            var result = await _maintenanceTaskService.ActivateMaintenanceTaskAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Vô hiệu hóa nhiệm vụ bảo trì.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> Manager<br/>
        /// Chuyển trạng thái của nhiệm vụ bảo trì từ Active sang Inactive.
        /// </remarks>
        /// <param name="id">ID của nhiệm vụ bảo trì cần vô hiệu hóa.</param>
        /// <returns>Thông báo vô hiệu hóa thành công.</returns>
        /// <response code="200">Vô hiệu hóa nhiệm vụ bảo trì thành công.</response>
        /// <response code="404">Không tìm thấy nhiệm vụ bảo trì.</response>
        /// <response code="400">Nhiệm vụ đã ở trạng thái ngưng hoạt động.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpPatch("{id}/deactivate")]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> DeactivateMaintenanceTask(int id)
        {
            var result = await _maintenanceTaskService.DeactivateMaintenanceTaskAsync(id);
            return Ok(result);
        }
    }
}