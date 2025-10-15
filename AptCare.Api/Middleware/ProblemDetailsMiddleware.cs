using AptCare.Service.Exceptions;
using AptCare.Service.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AptCare.Api.Middleware
{
    public class ProblemDetailsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ProblemDetailsMiddleware> _log;
        private readonly IWebHostEnvironment _env;
        private readonly bool _exposeDetailInProd;

        public ProblemDetailsMiddleware(RequestDelegate next,
            ILogger<ProblemDetailsMiddleware> log,
            IWebHostEnvironment env,
            IConfiguration config)
        {
            _next = next; _log = log; _env = env;
            // Bật/tắt mức detail ở Prod qua cấu hình
            _exposeDetailInProd = config.GetValue("Errors:ExposeDetail", false);
        }

        public async Task Invoke(HttpContext ctx)
        {
            try
            {
                await _next(ctx);
            }
            catch (AppValidationException vex)
            {
                await WriteProblem(ctx, vex.StatusCode,
                    title: "Validation/Business Error",
                    detail: vex.Message,
                    extras: vex.Payload);
                return;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Unhandled exception");

                // Cho mọi 500 đều có body. Ở Prod chỉ hiển thị message cơ bản (có thể bật chi tiết qua config).
                var detail = _env.IsDevelopment() || _exposeDetailInProd
                    ? ex.Message
                    : "Unexpected error occurred. Please contact support with the traceId.";
                await WriteProblem(ctx, 500, title: "Internal Server Error", detail: detail);
                return;
            }

            // Với các 4xx/5xx KHÔNG có body do controller tự đặt → thêm ProblemDetails tối thiểu
            if (!ctx.Response.HasStarted &&
                ctx.Response.ContentLength is null &&
                ctx.Response.StatusCode >= 400)
            {
                await WriteProblem(ctx, ctx.Response.StatusCode, "HTTP Error", null);
            }
        }

        private static async Task WriteProblem(HttpContext ctx, int statusCode, string title, string? detail, object? extras = null)
        {
            ctx.Response.StatusCode = statusCode;
            ctx.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = ctx.Request.Path
            };

            // kèm traceId để tra log
            problem.Extensions["traceId"] = ctx.TraceIdentifier;

            if (extras is not null)
                problem.Extensions["data"] = extras;

            await ctx.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }
}
