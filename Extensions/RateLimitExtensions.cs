using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace API.Extensions
{
    public static class RateLimitExtensions
    {
        public static IServiceCollection AddRateLimitingConfiguration(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                // Rate limiting global
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                    httpContext => RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.User?.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1)
                        }));

                // Política específica para API
                options.AddFixedWindowLimiter("ApiPolicy", opts =>
                {
                    opts.PermitLimit = 50;
                    opts.Window = TimeSpan.FromMinutes(1);
                    opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opts.QueueLimit = 10;
                });

                // Política para autenticação (mais restritiva)
                options.AddFixedWindowLimiter("AuthPolicy", opts =>
                {
                    opts.PermitLimit = 5;
                    opts.Window = TimeSpan.FromMinutes(1);
                    opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opts.QueueLimit = 2;
                });

                // Resposta quando limite é excedido
                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = 429;
                    context.HttpContext.Response.ContentType = "application/json";

                    var response = new
                    {
                        error = "Rate limit exceeded",
                        message = "Muitas requisições. Tente novamente em alguns segundos.",
                        retryAfter = "60s"
                    };

                    await context.HttpContext.Response.WriteAsync(
                        System.Text.Json.JsonSerializer.Serialize(response), token);
                };
            });

            return services;
        }
    }
}