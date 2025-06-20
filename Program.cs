using API.Extensions;
using Serilog;
using Serilog.Events;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

try
{
    // 📋 CONFIGURAÇÃO DE LOGGING (SERILOG)
    ConfigureLogger();
    builder.Host.UseSerilog();

    // 📋 CONFIGURAÇÕES BÁSICAS
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddControllersConfiguration();
    builder.Services.AddSwaggerConfiguration();

    // 🔐 AUTENTICAÇÃO E AUTORIZAÇÃO
    builder.Services.AddAuthenticationConfiguration(builder.Configuration);
    builder.Services.AddAuthorizationPolicies();

    // 🗄️ BANCO DE DADOS E CACHE
    builder.Services.AddSqlConfiguration();
    builder.Services.AddRedisConfiguration(builder.Configuration);

    // 🔗 INTEGRAÇÃO (ATUALIZADO)
    builder.Services.AddIntegrationServices(builder.Configuration);
    builder.Services.AddIntegrationValidation(builder.Configuration);
    builder.Services.AddIntegrationHealthChecks(builder.Configuration);
    builder.Services.AddIntegrationLogging();
    builder.Services.AddIntegrationCache();

    // 🚀 HANGFIRE COM VERIFICAÇÃO DE SAÚDE DO MYSQL
    try
    {
        Log.Information("🔄 Verificando disponibilidade do MySQL antes de configurar Hangfire...");

        // ✅ Aguardar MySQL estar disponível
        var connectionString = builder.Configuration.GetConnectionString("ConfiguracaoPadrao");
        if (!string.IsNullOrEmpty(connectionString))
        {
            await WaitForMySqlAvailabilityAsync(connectionString);
            Log.Information("✅ MySQL verificado e disponível");
        }

        // ✅ Configurar Hangfire após verificação
        builder.Services.AddHangfireConfiguration(builder.Configuration);
        Log.Information("✅ Hangfire configurado com sucesso");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "⚠️ Erro na configuração do Hangfire - continuando sem Hangfire");
        // A aplicação continua sem Hangfire se houver problema
    }

    // 🌐 CORS E RATE LIMITING
    builder.Services.AddCorsConfiguration();
    builder.Services.AddRateLimitingConfiguration();

    // 📊 MAPEAMENTO E VALIDAÇÃO
    builder.Services.AddMappingConfiguration();
    builder.Services.AddValidationConfiguration();

    // 📈 REPOSITÓRIOS E SERVIÇOS
    builder.Services.AddRepositoryConfiguration();
    builder.Services.AddServiceConfiguration();

    // 🔍 HEALTH CHECKS E MÉTRICAS
    builder.Services.AddHealthChecksConfiguration(builder.Configuration);
    builder.Services.AddPrometheusMetrics();

    // Construir a aplicação
    var app = builder.Build();

    // 🔧 PIPELINE DE CONFIGURAÇÃO
    app.ConfigureApplicationPipeline(app.Environment);

    // 🔗 MIDDLEWARE DE INTEGRAÇÃO
    app.UseIntegrationMiddleware();

    // 📊 MÉTRICAS
    app.UsePrometheusMetrics();

    // 🚀 HANGFIRE COM TRATAMENTO DE ERRO
    try
    {
        app.UseHangfireConfiguration(builder.Configuration);
        Log.Information("✅ Hangfire Dashboard configurado com sucesso");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "⚠️ Erro ao configurar Hangfire Dashboard - continuando sem Dashboard");
    }

    // 📊 INICIALIZAÇÃO DE DADOS E VALIDAÇÕES
    await app.SeedIntegrationDataAsync();
    await app.ValidateIntegrationsAsync();

    // 🎯 INFORMAÇÕES DE INICIALIZAÇÃO
    LogStartupInformation(app);

    // 📊 ESTATÍSTICAS DOS SERVIÇOS
    LogServiceStatisticsAsync(app);

    Log.Information("🚀 Aplicação iniciada com sucesso em {Environment}!", app.Environment.EnvironmentName);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Erro crítico na inicialização da aplicação");
    throw;
}
finally
{
    Log.Information("🛑 Encerrando aplicação...");
    await Log.CloseAndFlushAsync();
}

// ✅ FUNÇÃO HELPER PARA AGUARDAR MYSQL ESTAR DISPONÍVEL
static async Task WaitForMySqlAvailabilityAsync(string connectionString, int maxAttempts = 30, int delaySeconds = 2)
{
    for (int i = 0; i < maxAttempts; i++)
    {
        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            await connection.CloseAsync();

            Log.Information("✅ MySQL disponível na tentativa {Attempt}", i + 1);
            return;
        }
        catch (Exception ex)
        {
            Log.Debug("🔄 Tentativa {Attempt}/{MaxAttempts} - MySQL não disponível: {Error}",
                i + 1, maxAttempts, ex.Message);

            if (i < maxAttempts - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        }
    }

    throw new InvalidOperationException($"MySQL não ficou disponível após {maxAttempts} tentativas");
}

// 📋 CONFIGURAÇÃO DO SERILOG
void ConfigureLogger()
{
    var environment = builder.Environment.EnvironmentName;
    var applicationName = builder.Configuration.GetValue<string>("ApplicationName") ?? "API ERP";

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .MinimumLevel.Override("Hangfire", LogEventLevel.Information)
        .MinimumLevel.Override("RabbitMQ.Client", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", applicationName)
        .Enrich.WithProperty("Environment", environment)
        .Enrich.WithProperty("MachineName", Environment.MachineName)
        .Enrich.WithProperty("ProcessId", Environment.ProcessId)
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.File("Logs/Api/api-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            fileSizeLimitBytes: 50_000_000,
            rollOnFileSizeLimit: true,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext} {Message:lj} {Properties:j}{NewLine}{Exception}")
        // Log de erros em arquivo separado
        .WriteTo.File("Logs/Erros/errors-.log",
            restrictedToMinimumLevel: LogEventLevel.Error,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 60,
            fileSizeLimitBytes: 20_000_000,
            rollOnFileSizeLimit: true,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {Level:u3} {SourceContext} {Message:lj} {Properties:j}{NewLine}{Exception}")
        // Log específico para integrações
        .WriteTo.File("Logs/Integracao/integration-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 15,
            fileSizeLimitBytes: 30_000_000,
            rollOnFileSizeLimit: true,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {Level:u3} {SourceContext} {Message:lj} {Properties:j}{NewLine}{Exception}")
        // Log específico para Jobs/Hangfire
        .WriteTo.File("Logs/jobs/jobs-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 15,
            fileSizeLimitBytes: 30_000_000,
            rollOnFileSizeLimit: true,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {Level:u3} {Message:lj} {Properties:j}{NewLine}{Exception}")
        .CreateLogger();
}

// 🎯 LOG DE INFORMAÇÕES DE INICIALIZAÇÃO
void LogStartupInformation(WebApplication app)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var environment = app.Environment.EnvironmentName;
    var urls = app.Configuration.GetValue<string>("ASPNETCORE_URLS") ?? "http://localhost:5000";

    logger.LogInformation("🌐 Ambiente: {Environment}", environment);
    logger.LogInformation("🔗 URLs: {Urls}", urls);
    logger.LogInformation("📊 Swagger disponível em: /swagger");
    logger.LogInformation("🔧 Hangfire Dashboard: /hangfire");
    logger.LogInformation("💚 Health Check: /health");
    logger.LogInformation("📈 Métricas: /metrics");

    // Informações de configuração
    var redisConnectionString = app.Configuration.GetConnectionString("Redis");
    var hangfireConnectionString = app.Configuration.GetConnectionString("ConfiguracaoPadrao");
    var rabbitMQHost = app.Configuration.GetSection("RabbitMQ:HostName").Value;

    logger.LogInformation("🔴 Redis: {RedisStatus}",
        !string.IsNullOrEmpty(redisConnectionString) ? "Habilitado" : "Desabilitado");
    logger.LogInformation("🚀 Hangfire: {HangfireStatus}",
        !string.IsNullOrEmpty(hangfireConnectionString) ? "Habilitado" : "Desabilitado");
    logger.LogInformation("🐰 RabbitMQ: {RabbitMQStatus}",
        !string.IsNullOrEmpty(rabbitMQHost) ? $"Configurado ({rabbitMQHost})" : "Não configurado");

    // Informações de sistema
    var process = System.Diagnostics.Process.GetCurrentProcess();
    logger.LogInformation("💾 Memória inicial: {Memory:N0} MB",
        process.WorkingSet64 / 1024 / 1024);
    logger.LogInformation("🏗️ Framework: {Framework}",
        System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
    logger.LogInformation("💻 SO: {OS}",
        System.Runtime.InteropServices.RuntimeInformation.OSDescription);

    // Informações de configuração do Hangfire
    var hangfireConfig = app.Configuration.GetSection("Hangfire");
    if (hangfireConfig.Exists())
    {
        logger.LogInformation("⚙️ Workers Hangfire: {WorkerCount}",
            hangfireConfig.GetValue<int>("WorkerCount", Environment.ProcessorCount));
        logger.LogInformation("📋 Filas Hangfire: {Queues}",
            string.Join(", ", hangfireConfig.GetSection("Queues").Get<string[]>() ?? new[] { "default" }));
    }
}

// 📊 LOG DE ESTATÍSTICAS DOS SERVIÇOS
async Task LogServiceStatisticsAsync(WebApplication app)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("🔧 === ESTATÍSTICAS DOS SERVIÇOS ===");

        // Estatísticas do SQL Loader (se disponível)
        try
        {
            var sqlLoader = scope.ServiceProvider.GetRequiredService<API.SQL.SqlLoader>();
            var sqlStats = sqlLoader.GetStatistics();
            logger.LogInformation("📁 SQL Queries carregadas: {SqlStats}",
                System.Text.Json.JsonSerializer.Serialize(sqlStats));
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "SQL Loader não disponível");
        }

        // Estatísticas de Integração
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

        // Estatísticas do Hangfire
        try
        {
            var hangfireJobService = scope.ServiceProvider.GetService<API.Services.Interfaces.IHangfireJobService>();
            logger.LogInformation("🚀 Hangfire Job Service: {Status}",
                hangfireJobService != null ? "Disponível" : "Não registrado");

            if (hangfireJobService != null)
            {
                // Tentar obter estatísticas básicas
                var stats = await hangfireJobService.GetJobStatisticsAsync(1);
                logger.LogInformation("📊 Jobs - Total: {Total}, Sucesso: {Success}, Falha: {Failed}",
                    stats.TotalJobs, stats.SucceededJobs, stats.FailedJobs);
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Erro ao obter estatísticas do Hangfire");
        }

        // Estatísticas de repositórios
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

        logger.LogInformation("🔧 === FIM DAS ESTATÍSTICAS ===");
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Erro ao obter estatísticas dos serviços");
    }
}