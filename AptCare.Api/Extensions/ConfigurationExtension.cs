using AptCare.Repository;
using AptCare.Repository.Cloudinary;
using AptCare.Repository.Repositories;
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

            return service;
        }
    }
}
