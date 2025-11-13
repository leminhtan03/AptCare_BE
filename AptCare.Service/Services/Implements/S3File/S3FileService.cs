using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using AptCare.Service.Dtos.S3AWSDtos;
using AptCare.Service.Services.Interfaces.IS3File;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AptCare.Service.Services.Implements.S3File
{
    public class S3FileService : IS3FileService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly S3Options _options;

        public S3FileService(IOptions<S3Options> options)
        {
            _options = options.Value;

            var credentials = new BasicAWSCredentials(_options.AccessKey, _options.SecretKey);
            _s3Client = new AmazonS3Client(credentials, RegionEndpoint.GetBySystemName(_options.Region));
        }

        public async Task<string> UploadFileAsync(IFormFile file, string? prefix = null)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File rỗng.");

            var safeFileName = Path.GetFileName(file.FileName);
            var key = $"{prefix}{Guid.NewGuid()}_{safeFileName}".Replace("//", "/");

            using var stream = file.OpenReadStream();

            var putRequest = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                InputStream = stream,
                ContentType = file.ContentType
            };

            await _s3Client.PutObjectAsync(putRequest);

            return key;
        }

        public async Task<(byte[] FileBytes, string ContentType, string FileName)> GetFileAsync(string key)
        {
            var response = await _s3Client.GetObjectAsync(_options.BucketName, key);

            await using var responseStream = response.ResponseStream;
            using var ms = new MemoryStream();
            await responseStream.CopyToAsync(ms);

            var bytes = ms.ToArray();
            var contentType = response.Headers.ContentType ?? "application/octet-stream";
            var fileName = Path.GetFileName(key);

            return (bytes, contentType, fileName);
        }
    }
}
