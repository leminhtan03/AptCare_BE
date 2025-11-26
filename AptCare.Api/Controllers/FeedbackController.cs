using AptCare.Service.Dtos.FeedbackDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AptCare.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;

        public FeedbackController(IFeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        /// <summary>
        /// Tạo feedback mới hoặc reply feedback
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateFeedback([FromBody] CreateFeedbackRequest request)
        {
            try
            {
                var result = await _feedbackService.CreateFeedbackAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy toàn bộ chuỗi feedback của một repair request
        /// </summary>
        [HttpGet("repair-request/{repairRequestId}")]
        public async Task<IActionResult> GetFeedbackThread(int repairRequestId)
        {
            try
            {
                var result = await _feedbackService.GetFeedbackThreadAsync(repairRequestId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy chi tiết một feedback theo ID
        /// </summary>
        [HttpGet("{feedbackId}")]
        public async Task<IActionResult> GetFeedbackById(int feedbackId)
        {
            try
            {
                var result = await _feedbackService.GetFeedbackByIdAsync(feedbackId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Xóa feedback và tất cả replies của nó
        /// </summary>
        [HttpDelete("{feedbackId}")]
        public async Task<IActionResult> DeleteFeedback(int feedbackId)
        {
            try
            {
                var result = await _feedbackService.DeleteFeedbackAsync(feedbackId);
                return Ok(new { success = result, message = "Xóa feedback thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}