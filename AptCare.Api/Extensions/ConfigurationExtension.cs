using AptCare.Repository;
using AptCare.Repository.Cloudinary;
using AptCare.Repository.FCM;
using AptCare.Repository.Repositories;
using AptCare.Service.Dtos.TransactionDtos;
using Microsoft.EntityFrameworkCore;


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
            service.Configure<PayOSOptions>(options =>
            {
                options.BaseUrl = Environment.GetEnvironmentVariable("PAYOS_BASE_URL") ?? "https://api.payos.vn";
                options.ClientId = Environment.GetEnvironmentVariable("PAYOS_CLIENT_ID") ?? throw new Exception("PAYOS_CLIENT_ID missing");
                options.ApiKey = Environment.GetEnvironmentVariable("PAYOS_API_KEY") ?? throw new Exception("PAYOS_API_KEY missing");
                options.ChecksumKey = Environment.GetEnvironmentVariable("PAYOS_CHECKSUM_KEY") ?? throw new Exception("PAYOS_CHECKSUM_KEY missing");
                options.ReturnUrl = Environment.GetEnvironmentVariable("PAYOS_RETURN_URL") ?? "https://aptcare.vn/payment/return";
            });

            return service;
        }
    }
}
