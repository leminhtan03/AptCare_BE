using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces.IS3File
{
    public interface IS3FileService
    {
        Task<string> UploadFileAsync(IFormFile file, string? prefix = null);
        Task<(byte[] FileBytes, string ContentType, string FileName)> GetFileAsync(string key);
    }
}
