
using AptCare.Api.MapperProfile;
using AptCare.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace AptCare.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "AptCareSystem API", Version = "v1" });
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Vui lòng nhập Access Token với tiền tố 'Bearer ' vào ô bên dưới.\n\nVí dụ: 'Bearer 12345abcdef'",
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });
            builder.Services.AddDbContext<AptCareSystemDBContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddAutoMapper(typeof(AutoMapperProfiles));

            builder.Services.AddHttpContextAccessor();

            // Cấu hình xác thực JWT (Mã của bạn đã đúng)
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false; // Chỉ nên là false trong môi trường dev
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
                };
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
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SportGo API v1");
                });
            }


            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
