using Microsoft.AspNetCore.Http;

namespace AptCare.Service.Services.Interfaces
{
    public interface ICloudinaryService
    {
        Task<string> UploadImageAsync(IFormFile file);
        Task<List<string>> UploadMultipleImagesAsync(List<IFormFile> files);
    }
}
