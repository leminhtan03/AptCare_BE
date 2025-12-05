
using AptCare.Api.Extensions;
using AptCare.Api.MapperProfile;
using AptCare.Api.Middleware;
using AptCare.Repository;
using AptCare.Repository.Repositories;
using AptCare.Repository.Seeds;
using AptCare.Service.Hub;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

namespace AptCare.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            Env.Load();
            builder.Configuration.AddEnvironmentVariables();
            builder.Services.AddConfiguration(builder.Configuration)
                .AddService()
                .AddAuthenticationConfig(builder.Configuration)
                .AddCustomController()
                .AddSwaggerConfig();

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddAutoMapper(typeof(AutoMapperProfiles));

            builder.Services.AddRouting(options => options.LowercaseUrls = true);

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddSignalR();

            var allowed = new[]
            {
                "http://localhost:5173",
                "http://192.168.100.141",
                "http://aptcare.click",
                "http://www.aptcare.click",
                "https://aptcare.click",
                "https://www.aptcare.click",
                "https://fe-capstone-project-apt-care.vercel.app"
            };

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins(allowed)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials(); // cần cho cookie/JWT + SignalR
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

                        using (var migrateScope = app.Services.CreateScope())
                        {
                            var dbContext = migrateScope.ServiceProvider.GetRequiredService<AptCareSystemDBContext>();
                            await dbContext.Database.MigrateAsync();
                        }

                        logger.LogInformation("Áp dụng migrations thành công!");

                        using (var seedScope = app.Services.CreateScope())
                        {
                            await Seed.Initialize(seedScope.ServiceProvider);
                        }
                        break;
                    }
                    catch (Exception ex)
                    {
                        retryCount++;
                        logger.LogWarning(ex, "Lần thử {RetryCount}/{MaxRetries} áp dụng migrations thất bại. Đang thử lại sau 5 giây...", retryCount, maxRetries);

                        if (retryCount >= maxRetries)
                        {
                            logger.LogError(ex, "Không thể áp dụng migrations sau {MaxRetries} lần thử.", maxRetries);
                            throw new Exception(ex.Message);
                        }

                        await Task.Delay(5000);
                    }
                }
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
            app.UseMiddleware<ProblemDetailsMiddleware>();


            //if (app.Environment.IsDevelopment())
            //{
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                // Hiển thị UI của Swagger
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "AptCareSystem API v1");
            });
            //}

            app.UseHttpsRedirection();
            app.UseCors("AllowFrontend");

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHub<ChatHub>("/chatHub");
            app.MapControllers();
            app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
            app.MapGet("/health", () => Results.Ok(new { status = "Healthy" })).AllowAnonymous();
            app.Run();
        }
    }
}
