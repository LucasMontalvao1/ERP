using API.Middlewares;

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

            app.MapHealthChecks("/health");

            return app;
        }
    }
}
