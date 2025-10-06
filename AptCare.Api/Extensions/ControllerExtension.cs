using System.Text.Json.Serialization;

namespace AptCare.Api.Extensions
{
    public static class ControllerExtension
    {
        public static IServiceCollection AddCustomController(this IServiceCollection services)
        {
            services.AddControllers()
                    .AddJsonOptions(options =>
                    {
                        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
                        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    });

            return services;
        }        
    }
}
