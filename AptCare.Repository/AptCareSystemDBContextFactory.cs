using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AptCare.Repository
{
    public class AptCareSystemDBContextFactory : IDesignTimeDbContextFactory<AptCareSystemDBContext>
    {
        public AptCareSystemDBContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                 .AddJsonFile("appsettings.Development.json", optional: true)
                 .AddEnvironmentVariables()
                 .Build();

            var optionsBuilder = new DbContextOptionsBuilder<AptCareSystemDBContext>();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = "Host=localhost;Database=AptCareSystemDB;Username=postgres;Password=12345";
            }

            optionsBuilder.UseNpgsql(connectionString);

            return new AptCareSystemDBContext(optionsBuilder.Options);
        }
    }
}
