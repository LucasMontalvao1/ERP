using API.Middlewares;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;

namespace API.Extensions
{
    public static class ApplicationExtensions
    {
        /// <summary>
        /// Configura o pipeline de requisições HTTP
        /// </summary>
        public static WebApplication ConfigureApplicationPipeline(
            this WebApplication app,
            IWebHostEnvironment env)
        {
            // Configurações específicas de desenvolvimento
            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // Em produção, redirecionar erros para um controller de erro
                app.UseExceptionHandler("/Error");
                // HSTS padrão é 30 dias. Ajuste para cenários de produção.
                app.UseHsts();
            }

            // Middleware personalizado para tratamento global de exceções
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseHttpsRedirection();
            app.UseCors("AllowAll");

            app.UseRateLimiter();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            // Health Check endpoint DETALHADO
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";

                    var response = new
                    {
                        status = report.Status.ToString(),
                        totalDuration = report.TotalDuration.ToString(@"hh\:mm\:ss\.fff"),
                        entries = report.Entries.ToDictionary(
                            entry => entry.Key,
                            entry => new
                            {
                                status = entry.Value.Status.ToString(),
                                duration = entry.Value.Duration.ToString(@"hh\:mm\:ss\.fff"),
                                description = entry.Value.Description,
                                data = entry.Value.Data
                            }
                        )
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }));
                }
            });

            // Health Check simples (para Docker health check)
            app.MapHealthChecks("/health/ready");

            return app;
        }
    }
}
