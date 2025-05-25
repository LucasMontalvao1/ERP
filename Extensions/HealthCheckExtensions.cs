using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace API.Extensions
{
    public static class HealthCheckExtensions
    {
        public static IServiceCollection AddHealthChecksConfiguration(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddHealthChecks()
                .AddMySql(configuration.GetConnectionString("ConfiguracaoPadrao")!)
                .AddRedis(configuration.GetConnectionString("Redis")!)
                .AddCheck("api", () => HealthCheckResult.Healthy("API esta funcionando (onlineeee)"));

            return services;
        }
    }
}