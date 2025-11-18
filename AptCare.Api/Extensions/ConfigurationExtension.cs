using AptCare.Repository;
using AptCare.Repository.Cloudinary;
using AptCare.Repository.FCM;
using AptCare.Repository.Repositories;
using AptCare.Service.Dtos.S3AWSDtos;
using Microsoft.EntityFrameworkCore;
using PayOS;

namespace AptCare.Api.Extensions
{
    public static class ConfigurationExtension
    {
        public static IServiceCollection AddConfiguration(this IServiceCollection service, IConfiguration configuration)
        {
            service.AddDbContext<AptCareSystemDBContext>(options =>
                 options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));


            service.Configure<CloudinarySettings>(configuration.GetSection("CloudinarySettings"));
            service.Configure<MailSettings>(configuration.GetSection("MailSettings"));
            service.Configure<FCMSettings>(configuration.GetSection("FCMSettings"));

            // PayOS Configuration version 2.0.1
            var payOSClientId = Environment.GetEnvironmentVariable("PAYOS_CLIENT_ID")
                ?? throw new Exception("PAYOS_CLIENT_ID missing");
            var payOSApiKey = Environment.GetEnvironmentVariable("PAYOS_API_KEY")
                ?? throw new Exception("PAYOS_API_KEY missing");
            var payOSChecksumKey = Environment.GetEnvironmentVariable("PAYOS_CHECKSUM_KEY")
                ?? throw new Exception("PAYOS_CHECKSUM_KEY missing");

            service.AddSingleton(new PayOSClient(payOSClientId, payOSApiKey, payOSChecksumKey));

            service.Configure<S3Options>(configuration.GetSection("AWS"));

            return service;
        }
    }
}
