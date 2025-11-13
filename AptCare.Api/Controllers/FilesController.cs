using AptCare.Service.Services.Interfaces.IS3File;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : BaseApiController
    {
        private readonly IS3FileService _s3FileService;

        public FilesController(IS3FileService s3FileService)
        {
            _s3FileService = s3FileService;
        }
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File rỗng.");

            var key = await _s3FileService.UploadFileAsync(file, "pdf/");

            return Ok(new { Key = key });
        }

        [HttpGet("view/{**key}")]
        public async Task<IActionResult> View(string key)
        {
            key = Uri.UnescapeDataString(key);

            var (bytes, contentType, fileName) = await _s3FileService.GetFileAsync(key);
            
            contentType = "application/pdf";
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";

            return File(bytes, contentType);
        }
    }
}
