using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ORUApi.Data
{
    public class ORUDbContextFactory : IDesignTimeDbContextFactory<ORUDbContext>
    {
        public ORUDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(
                config.GetConnectionString("DefaultConnection")!);
            dataSourceBuilder.EnableDynamicJson();

            var options = new DbContextOptionsBuilder<ORUDbContext>()
                .UseNpgsql(dataSourceBuilder.Build())
                .Options;

            return new ORUDbContext(options);
        }
    }
}
