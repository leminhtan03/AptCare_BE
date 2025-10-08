
using AptCare.Api.Extensions;
using AptCare.Api.MapperProfile;
using AptCare.Repository;
using AptCare.Repository.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using OfficeOpenXml;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace AptCare.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddConfiguration(builder.Configuration)
                .AddService()
                .AddAuthenticationConfig(builder.Configuration)
                .AddCustomController()
                .AddSwaggerConfig();
            builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddAutoMapper(typeof(AutoMapperProfiles));

            builder.Services.AddRouting(options => options.LowercaseUrls = true);

            builder.Services.AddHttpContextAccessor();

            //CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:5174") // Your frontend URL
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials(); // Important for cookies/auth
                });
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();

                var maxRetries = 5;
                var retryCount = 0;

                while (retryCount < maxRetries)
                {
                    try
                    {
                        logger.LogInformation("Đang thử áp dụng migrations... Lần thử: {RetryCount}", retryCount + 1);
                        var dbContext = services.GetRequiredService<AptCareSystemDBContext>();

                        await dbContext.Database.CanConnectAsync();

                        await dbContext.Database.MigrateAsync();

                        logger.LogInformation("Áp dụng migrations thành công!");
                        break;
                    }
                    catch (Exception ex)
                    {
                        retryCount++;
                        logger.LogWarning(ex, "Lần thử {RetryCount}/{MaxRetries} áp dụng migrations thất bại. Đang thử lại sau 5 giây...", retryCount, maxRetries);

                        if (retryCount >= maxRetries)
                        {
                            logger.LogError(ex, "Không thể áp dụng migrations sau {MaxRetries} lần thử.", maxRetries);
                            throw;
                        }

                        await Task.Delay(5000);
                    }
                }
            }
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    // Hiển thị UI của Swagger
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AptCareSystem API v1");
                });
            }


            app.UseHttpsRedirection();
            app.UseCors("AllowFrontend");
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
