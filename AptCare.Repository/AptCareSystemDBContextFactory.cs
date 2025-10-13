using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AptCare.Repository
{
    public class AptCareSystemDBContextFactory : IDesignTimeDbContextFactory<AptCareSystemDBContext>
    {
        public AptCareSystemDBContext CreateDbContext(string[] args)
        {
            var env = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            var solutionRoot = GetSolutionRoot();
            var apiPath = Path.Combine(solutionRoot, "AptCare.Api");

            TryLoadDotEnv(Path.Combine(apiPath, ".env"));

            var configuration = new ConfigurationBuilder()
                .SetBasePath(apiPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
                connectionString = "Host=localhost;Database=AptCareSystemDB;Username=postgres;Password=12345";

            var optionsBuilder = new DbContextOptionsBuilder<AptCareSystemDBContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new AptCareSystemDBContext(optionsBuilder.Options);
        }

        private static string GetSolutionRoot()
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null && !dir.GetFiles("*.sln").Any())
                dir = dir.Parent;
            return dir?.FullName ?? Directory.GetCurrentDirectory();
        }

        private static void TryLoadDotEnv(string envPath)
        {
            if (!File.Exists(envPath)) return;

            foreach (var raw in File.ReadAllLines(envPath))
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

                var idx = line.IndexOf('=');
                if (idx <= 0) continue;

                var key = line[..idx].Trim();
                var value = line[(idx + 1)..].Trim();

                // bỏ ngoặc kép nếu có
                if (value.Length >= 2 && value.StartsWith("\"") && value.EndsWith("\""))
                    value = value[1..^1];

                System.Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
