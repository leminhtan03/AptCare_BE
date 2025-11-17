using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace AptCare.Api.Filters
{
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var allParameters = context.ApiDescription.ParameterDescriptions;
            var hasFileParameter = allParameters.Any(p => ContainsFileType(p.Type));

            if (!hasFileParameter)
                return;

            var dtoParameter = allParameters.FirstOrDefault(p =>
                p.Source.Id == "Form" &&
                p.Type.IsClass &&
                p.Type != typeof(string) &&
                !p.Type.IsPrimitive);

            if (dtoParameter == null)
                return;

            var properties = new Dictionary<string, OpenApiSchema>();
            var modelProperties = dtoParameter.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in modelProperties)
            {
                var propertyName = prop.Name;

                if (IsFileType(prop.PropertyType))
                {
                    properties[propertyName] = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary",
                        Description = "Upload a single file"
                    };
                }
                else if (IsFileArrayType(prop.PropertyType))
                {
                    properties[propertyName] = new OpenApiSchema
                    {
                        Type = "array",
                        Items = new OpenApiSchema
                        {
                            Type = "string",
                            Format = "binary"
                        },
                        Description = "Upload multiple files"
                    };
                }
                else
                {
                    properties[propertyName] = GetSchemaForProperty(prop.PropertyType);
                }
            }
            if (properties.Any())
            {
                operation.Parameters?.Clear();

                operation.RequestBody = new OpenApiRequestBody
                {
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["multipart/form-data"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = properties
                            }
                        }
                    }
                };
            }
        }

        private bool ContainsFileType(Type type)
        {
            if (type == null) return false;

            if (IsFileType(type) || IsFileArrayType(type))
                return true;

            if (!type.IsClass || type == typeof(string) || type.IsPrimitive || type.IsEnum)
                return false;

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return properties.Any(p => IsFileType(p.PropertyType) || IsFileArrayType(p.PropertyType));
        }

        private bool IsFileType(Type type)
        {
            return type == typeof(IFormFile);
        }

        private bool IsFileArrayType(Type type)
        {
            return type == typeof(IFormFile[]) ||
                   type == typeof(List<IFormFile>) ||
                   type == typeof(IEnumerable<IFormFile>);
        }

        private OpenApiSchema GetSchemaForProperty(Type propertyType)
        {
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (underlyingType == typeof(string))
            {
                return new OpenApiSchema { Type = "string" };
            }
            else if (underlyingType == typeof(int))
            {
                return new OpenApiSchema { Type = "integer", Format = "int32" };
            }
            else if (underlyingType == typeof(long))
            {
                return new OpenApiSchema { Type = "integer", Format = "int64" };
            }
            else if (underlyingType == typeof(decimal))
            {
                return new OpenApiSchema { Type = "number", Format = "double" };
            }
            else if (underlyingType == typeof(double) || underlyingType == typeof(float))
            {
                return new OpenApiSchema { Type = "number", Format = "double" };
            }
            else if (underlyingType == typeof(bool))
            {
                return new OpenApiSchema { Type = "boolean" };
            }
            else if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset))
            {
                return new OpenApiSchema { Type = "string", Format = "date-time" };
            }
            else if (underlyingType == typeof(DateOnly))
            {
                return new OpenApiSchema { Type = "string", Format = "date" };
            }
            else if (underlyingType.IsEnum)
            {
                return new OpenApiSchema
                {
                    Type = "integer",
                    Format = "int32",
                    Enum = Enum.GetValues(underlyingType)
                        .Cast<int>()
                        .Select(v => (Microsoft.OpenApi.Any.IOpenApiAny)new Microsoft.OpenApi.Any.OpenApiInteger(v))
                        .ToList()
                };
            }
            else
            {
                return new OpenApiSchema { Type = "string" };
            }
        }
    }
}
