using System.Text.Json;

namespace API.Services;

public static class StartupLoggingService
{
    /// <summary>
    /// Registra informações de inicialização da aplicação
    /// </summary>
    public static WebApplication LogStartupInformation(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        LogEnvironmentInfo(app, logger);
        LogConfigurationInfo(app, logger);
        LogSystemInfo(logger);
        LogHangfireInfo(app, logger);

        return app;
    }

    private static void LogEnvironmentInfo(WebApplication app, ILogger logger)
    {
        var environment = app.Environment.EnvironmentName;
        var urls = app.Configuration.GetValue<string>("ASPNETCORE_URLS") ?? "http://localhost:5000";

        logger.LogInformation("🌐 Ambiente: {Environment}", environment);
        logger.LogInformation("🔗 URLs: {Urls}", urls);
        logger.LogInformation("📊 Swagger disponível em: /swagger");
        logger.LogInformation("🔧 Hangfire Dashboard: /hangfire");
        logger.LogInformation("💚 Health Check: /health");
        logger.LogInformation("📈 Métricas: /metrics");
    }

    private static void LogConfigurationInfo(WebApplication app, ILogger logger)
    {
        var redisConnectionString = app.Configuration.GetConnectionString("Redis");
        var hangfireConnectionString = app.Configuration.GetConnectionString("ConfiguracaoPadrao");
        var rabbitMQHost = app.Configuration.GetSection("RabbitMQ:HostName").Value;

        logger.LogInformation("🔴 Redis: {RedisStatus}",
            !string.IsNullOrEmpty(redisConnectionString) ? "Habilitado" : "Desabilitado");
        logger.LogInformation("🚀 Hangfire: {HangfireStatus}",
            !string.IsNullOrEmpty(hangfireConnectionString) ? "Habilitado" : "Desabilitado");
        logger.LogInformation("🐰 RabbitMQ: {RabbitMQStatus}",
            !string.IsNullOrEmpty(rabbitMQHost) ? $"Configurado ({rabbitMQHost})" : "Não configurado");
    }

    private static void LogSystemInfo(ILogger logger)
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();
        logger.LogInformation("💾 Memória inicial: {Memory:N0} MB",
            process.WorkingSet64 / 1024 / 1024);
        logger.LogInformation("🏗️ Framework: {Framework}",
            System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
        logger.LogInformation("💻 SO: {OS}",
            System.Runtime.InteropServices.RuntimeInformation.OSDescription);
    }

    private static void LogHangfireInfo(WebApplication app, ILogger logger)
    {
        var hangfireConfig = app.Configuration.GetSection("Hangfire");
        if (hangfireConfig.Exists())
        {
            logger.LogInformation("⚙️ Workers Hangfire: {WorkerCount}",
                hangfireConfig.GetValue<int>("WorkerCount", Environment.ProcessorCount));
            logger.LogInformation("📋 Filas Hangfire: {Queues}",
                string.Join(", ", hangfireConfig.GetSection("Queues").Get<string[]>() ?? new[] { "default" }));
        }
    }

    /// <summary>
    /// Registra estatísticas dos serviços da aplicação
    /// </summary>
    public static async Task<WebApplication> LogServiceStatisticsAsync(this WebApplication app)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("🔧 === ESTATÍSTICAS DOS SERVIÇOS ===");

            await LogSqlLoaderStatistics(scope, logger);
            await LogIntegrationStatistics(scope, logger);
            await LogHangfireStatistics(scope, logger);
            await LogRepositoryStatistics(scope, logger);

            logger.LogInformation("🔧 === FIM DAS ESTATÍSTICAS ===");
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogWarning(ex, "Erro ao obter estatísticas dos serviços");
        }

        return app;
    }

    private static async Task LogSqlLoaderStatistics(IServiceScope scope, ILogger logger)
    {
        try
        {
            var sqlLoader = scope.ServiceProvider.GetRequiredService<API.SQL.SqlLoader>();
            var sqlStats = sqlLoader.GetStatistics();
            logger.LogInformation("📁 SQL Queries carregadas: {SqlStats}",
                JsonSerializer.Serialize(sqlStats));
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "SQL Loader não disponível");
        }
    }

    private static async Task LogIntegrationStatistics(IServiceScope scope, ILogger logger)
    {
        try
        {
            var integrationService = scope.ServiceProvider.GetService<API.Services.Interfaces.IIntegrationService>();
            logger.LogInformation("🔗 Serviço de Integração: {Status}",
                integrationService != null ? "Disponível" : "Não registrado");

            var salesForceService = scope.ServiceProvider.GetService<API.Services.Interfaces.ISalesForceService>();
            logger.LogInformation("🏢 SalesForce Service: {Status}",
                salesForceService != null ? "Disponível" : "Não registrado");

            var emailService = scope.ServiceProvider.GetService<API.Services.Interfaces.IEmailService>();
            logger.LogInformation("📧 Email Service: {Status}",
                emailService != null ? "Disponível" : "Não registrado");

            var rabbitMQService = scope.ServiceProvider.GetService<API.Services.Interfaces.IRabbitMQService>();
            logger.LogInformation("🐰 RabbitMQ Service: {Status}",
                rabbitMQService != null ? "Disponível" : "Não registrado");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Erro ao verificar serviços de integração");
        }
    }

    private static async Task LogHangfireStatistics(IServiceScope scope, ILogger logger)
    {
        try
        {
            var hangfireJobService = scope.ServiceProvider.GetService<API.Services.Interfaces.IHangfireJobService>();
            logger.LogInformation("🚀 Hangfire Job Service: {Status}",
                hangfireJobService != null ? "Disponível" : "Não registrado");

            if (hangfireJobService != null)
            {
                var stats = await hangfireJobService.GetJobStatisticsAsync(1);
                logger.LogInformation("📊 Jobs - Total: {Total}, Sucesso: {Success}, Falha: {Failed}",
                    stats.TotalJobs, stats.SucceededJobs, stats.FailedJobs);
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Erro ao obter estatísticas do Hangfire");
        }
    }

    private static async Task LogRepositoryStatistics(IServiceScope scope, ILogger logger)
    {
        try
        {
            var atividadeRepo = scope.ServiceProvider.GetService<API.Repositories.Interfaces.IAtividadeRepository>();
            logger.LogInformation("🗄️ Repositório de Atividades: {Status}",
                atividadeRepo != null ? "Disponível" : "Não registrado");

            var configRepo = scope.ServiceProvider.GetService<API.Repositories.Interfaces.IConfiguracaoIntegracaoRepository>();
            logger.LogInformation("⚙️ Repositório de Configuração: {Status}",
                configRepo != null ? "Disponível" : "Não registrado");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Erro ao verificar repositórios");
        }
    }
}