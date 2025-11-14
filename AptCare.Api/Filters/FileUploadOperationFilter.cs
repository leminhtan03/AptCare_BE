using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AptCare.Api.Filters
{
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var fileParameters = context.ApiDescription.ParameterDescriptions
                .Where(p => p.Type == typeof(IFormFile) ||
                           p.Type == typeof(IEnumerable<IFormFile>) ||
                           p.Type == typeof(List<IFormFile>) ||
                           p.Type == typeof(IFormFile[]))
                .ToList();

            if (!fileParameters.Any())
                return;
            operation.Parameters?.Clear();
            operation.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = fileParameters.ToDictionary(
                                p => p.Name,
                                p => new OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "binary",
                                    Description = p.Type == typeof(IFormFile)
                                        ? "Upload a single file"
                                        : "Upload multiple files"
                                }
                            ),
                            Required = fileParameters
                                .Where(p => p.IsRequired)
                                .Select(p => p.Name)
                                .ToHashSet()
                        }
                    }
                }
            };
        }
    }
}
