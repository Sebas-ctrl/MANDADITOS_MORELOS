using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MANDADITOS_MORELOS
{
    public static class DbContextConfigurator
    {
        public static void ConfigureDbContext<TContext>(IServiceCollection services, IConfiguration configuration)
            where TContext : DbContext
        {
            services.AddDbContext<TContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("connection_to_mysql");
                options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)));
            });
        }
    }
}