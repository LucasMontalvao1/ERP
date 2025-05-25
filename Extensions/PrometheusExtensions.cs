using Prometheus;

namespace API.Extensions
{
    public static class PrometheusExtensions
    {
        public static IServiceCollection AddPrometheusMetrics(this IServiceCollection services)
        {
            // Métricas customizadas
            services.AddSingleton(Metrics.CreateCounter(
                "api_requests_total",
                "Total number of API requests",
                new[] { "method", "endpoint", "status_code" }));

            services.AddSingleton(Metrics.CreateHistogram(
                "api_request_duration_seconds",
                "Duration of API requests in seconds",
                new[] { "method", "endpoint" }));

            services.AddSingleton(Metrics.CreateGauge(
                "api_active_connections",
                "Number of active connections"));

            services.AddSingleton(Metrics.CreateCounter(
                "api_cache_hits_total",
                "Total number of cache hits",
                new[] { "cache_type" }));

            services.AddSingleton(Metrics.CreateCounter(
                "api_cache_misses_total",
                "Total number of cache misses",
                new[] { "cache_type" }));

            services.AddSingleton(Metrics.CreateGauge(
                "api_database_connections",
                "Number of active database connections"));

            return services;
        }

        public static WebApplication UsePrometheusMetrics(this WebApplication app)
        {
            // Middleware para métricas HTTP
            app.UseHttpMetrics();

            // Endpoint /metrics para Prometheus
            app.MapMetrics();

            return app;
        }
    }
}