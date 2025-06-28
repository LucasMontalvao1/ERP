using API.Extensions;
using API.Services;
using Serilog;

namespace API.Configuration;

public static class ApplicationConfiguration
{
    public static async Task<WebApplication> ConfigureApplicationAsync(this WebApplication app)
    {
        Log.Information("🔧 Configurando pipeline da aplicação...");

        // 🔧 PIPELINE DE CONFIGURAÇÃO
        app.ConfigureApplicationPipeline(app.Environment);

        // 🔗 MIDDLEWARE DE INTEGRAÇÃO
        app.UseIntegrationMiddleware();

        // 📊 MÉTRICAS
        app.UsePrometheusMetrics();

        // 🚀 HANGFIRE COM TRATAMENTO DE ERRO
        app.ConfigureHangfireDashboard();

        // 📊 INICIALIZAÇÃO DE DADOS E VALIDAÇÕES
        await app.InitializeApplicationDataAsync();

        Log.Information("✅ Pipeline da aplicação configurado");
        return app;
    }

    public static async Task<WebApplication> StartApplicationAsync(this WebApplication app)
    {
        // 🎯 INFORMAÇÕES DE INICIALIZAÇÃO
        app.LogStartupInformation();

        // 📊 ESTATÍSTICAS DOS SERVIÇOS
        await app.LogServiceStatisticsAsync();

        Log.Information("🚀 Aplicação iniciada com sucesso em {Environment}!", app.Environment.EnvironmentName);

        app.Run();
        return app;
    }

    private static WebApplication ConfigureHangfireDashboard(this WebApplication app)
    {
        try
        {
            app.UseHangfireConfiguration(app.Configuration);
            Log.Information("✅ Hangfire Dashboard configurado com sucesso");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "⚠️ Erro ao configurar Hangfire Dashboard - continuando sem Dashboard");
        }

        return app;
    }

    private static async Task<WebApplication> InitializeApplicationDataAsync(this WebApplication app)
    {
        await app.SeedIntegrationDataAsync();
        await app.ValidateIntegrationsAsync();
        return app;
    }
}