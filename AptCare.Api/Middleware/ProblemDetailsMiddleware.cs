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

        public ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> log, IWebHostEnvironment env, IConfiguration config)
        {
            _next = next; _log = log; _env = env;
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
                var detail = ex.Message;
                var errorExtras = new Dictionary<string, object>
                {
                    ["message"] = ex.Message,
                    ["type"] = ex.GetType().Name
                };
                if (_env.IsDevelopment() || _exposeDetailInProd)
                {
                    errorExtras["stackTrace"] = ex.StackTrace ?? "No stack trace available";
                    if (ex.InnerException != null)
                    {
                        errorExtras["innerException"] = new
                        {
                            message = ex.InnerException.Message,
                            type = ex.InnerException.GetType().Name,
                            stackTrace = ex.InnerException.StackTrace
                        };
                    }
                }
                await WriteProblem(ctx, 500,
                    title: "Internal Server Error",
                    detail: detail,
                    extras: errorExtras);
                return;
            }

            if (!ctx.Response.HasStarted && ctx.Response.ContentLength is null && ctx.Response.StatusCode >= 400)
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
            problem.Extensions["traceId"] = ctx.TraceIdentifier;

            if (extras is not null)
                problem.Extensions["data"] = extras;

            await ctx.Response.WriteAsync(JsonSerializer.Serialize(problem, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
    }
}
