using API.Configuration;
using API.Jobs;
using API.Jobs.Base;
using API.Services;
using API.Services.Interfaces;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.MySql;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace API.Extensions;

public static class HangfireExtensions
{
    private static readonly ILogger _logger = CreateLogger();

    public static IServiceCollection AddHangfireConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<HangfireConfig>(configuration.GetSection("Hangfire"));

        var connectionString = configuration.GetConnectionString("ConfiguracaoPadrao");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("String de conexão 'ConfiguracaoPadrao' não encontrada");
        }

        if (!connectionString.Contains("Allow User Variables", StringComparison.OrdinalIgnoreCase))
        {
            connectionString += connectionString.Contains("?") ? "&" : ";";
            connectionString += "Allow User Variables=true;Convert Zero Datetime=true;";
        }

        var hangfireConfig = configuration.GetSection("Hangfire").Get<HangfireConfig>()
            ?? new HangfireConfig();

        // ✅ Configurar Hangfire com retry automático
        services.AddHangfire(config =>
        {
            // ✅ RETRY AUTOMÁTICO NA CONFIGURAÇÃO
            var storage = CreateMySqlStorageWithRetry(connectionString);

            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseStorage(storage)
                .WithJobExpirationTimeout(TimeSpan.FromDays(7));
        });

        // ✅ Configuração do servidor com delay de inicialização
        services.AddHangfireServer(options =>
        {
            options.WorkerCount = hangfireConfig.WorkerCount;
            options.Queues = hangfireConfig.Queues.ToArray();
            options.ServerName = Environment.MachineName;
            options.HeartbeatInterval = TimeSpan.FromSeconds(30);
            options.ServerTimeout = TimeSpan.FromMinutes(5);
            options.SchedulePollingInterval = TimeSpan.FromSeconds(15);

            // ✅ DELAY IMPORTANTE: Aguardar antes de iniciar o servidor
            options.ServerCheckInterval = TimeSpan.FromSeconds(20);
        });

        // Registrar Jobs
        services.AddScoped<IntegrationJob>();
        services.AddScoped<EmailJob>();
        services.AddScoped<MaintenanceJob>();
        services.AddScoped<ReportJob>();
        services.AddScoped<IHangfireJobService, HangfireJobService>();

        return services;
    }

    // ✅ MÉTODO COM RETRY AUTOMÁTICO PARA CONEXÃO MYSQL
    private static MySqlStorage CreateMySqlStorageWithRetry(string connectionString, int maxRetries = 10)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("🔄 Tentativa {Attempt}/{MaxRetries} de conectar MySQL para Hangfire", attempt, maxRetries);

                // ✅ Testar conexão antes de criar o storage
                using (var testConnection = new MySqlConnection(connectionString))
                {
                    testConnection.Open();
                    _logger.LogInformation("✅ Conexão MySQL testada com sucesso");
                }

                // ✅ Criar storage com configurações otimizadas
                var storage = new MySqlStorage(connectionString, new MySqlStorageOptions
                {
                    QueuePollInterval = TimeSpan.FromSeconds(10),
                    JobExpirationCheckInterval = TimeSpan.FromHours(1),
                    CountersAggregateInterval = TimeSpan.FromMinutes(5),
                    PrepareSchemaIfNecessary = true,
                    DashboardJobListLimit = 50000,
                    TransactionTimeout = TimeSpan.FromMinutes(1),
                    TablesPrefix = "hangfire_",
                    TransactionIsolationLevel = (System.Transactions.IsolationLevel?)System.Data.IsolationLevel.ReadCommitted,
                    InvisibilityTimeout = TimeSpan.FromMinutes(30)
                });

                _logger.LogInformation("🚀 Hangfire MySQL Storage criado com sucesso");
                return storage;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "❌ Tentativa {Attempt}/{MaxRetries} falhou: {Error}", attempt, maxRetries, ex.Message);

                if (attempt == maxRetries)
                {
                    _logger.LogError("💥 Todas as tentativas de conexão MySQL falharam!");
                    throw;
                }

                // ✅ Aguardar antes da próxima tentativa
                var delay = TimeSpan.FromSeconds(3 * attempt); 
                _logger.LogInformation("⏳ Aguardando {Delay}s antes da próxima tentativa...", delay.TotalSeconds);
                Thread.Sleep(delay);
            }
        }

        throw new InvalidOperationException("Não foi possível criar MySQL Storage após múltiplas tentativas");
    }

    public static WebApplication UseHangfireConfiguration(
        this WebApplication app,
        IConfiguration configuration)
    {
        var hangfireConfig = configuration.GetSection("Hangfire").Get<HangfireConfig>()
            ?? new HangfireConfig();

        try
        {
            // ✅ Dashboard com configurações de segurança
            var dashboardOptions = new DashboardOptions
            {
                AppPath = "/",
                DashboardTitle = "ERP System - Jobs Dashboard",
                StatsPollingInterval = 2000,
                DisplayStorageConnectionString = false,
                Authorization = hangfireConfig.RequireAuth
                    ? new[] { new HangfireAuthorizationFilter(hangfireConfig) }
                    : new IDashboardAuthorizationFilter[0]
            };

            app.UseHangfireDashboard(hangfireConfig.DashboardPath, dashboardOptions);

            // ✅ Configurar jobs recorrentes com delay
            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10)); // Aguardar 10 segundos
                ConfigureRecurringJobs(configuration);
            });

            _logger.LogInformation("✅ Hangfire Dashboard configurado em {Path}", hangfireConfig.DashboardPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao configurar Hangfire Dashboard");
        }

        return app;
    }

    private static void ConfigureRecurringJobs(IConfiguration configuration)
    {
        try
        {
            _logger.LogInformation("🔄 Configurando jobs recorrentes...");

            var jobsConfig = configuration.GetSection("Jobs");

            RecurringJob.AddOrUpdate<IntegrationJob>(
                "integration-retry",
                job => job.ProcessFailedIntegrationsAsync(),
                jobsConfig["IntegrationRetrySchedule"] ?? "0 */30 * * * *",
                TimeZoneInfo.Local);

            RecurringJob.AddOrUpdate<EmailJob>(
                "email-processing",
                job => job.ProcessEmailQueueAsync(),
                jobsConfig["EmailProcessingSchedule"] ?? "0 */5 * * * *",
                TimeZoneInfo.Local);

            RecurringJob.AddOrUpdate<MaintenanceJob>(
                "maintenance",
                job => job.RunMaintenanceTasksAsync(),
                jobsConfig["MaintenanceSchedule"] ?? "0 0 2 * * *",
                TimeZoneInfo.Local);

            RecurringJob.AddOrUpdate<ReportJob>(
                "weekly-reports",
                job => job.GenerateWeeklyReportsAsync(),
                jobsConfig["ReportGenerationSchedule"] ?? "0 0 6 * * MON",
                TimeZoneInfo.Local);

            _logger.LogInformation("✅ Jobs recorrentes configurados com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao configurar recurring jobs");
        }
    }

    private static ILogger CreateLogger()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        return loggerFactory.CreateLogger("HangfireExtensions");
    }
}

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly HangfireConfig _config;

    public HangfireAuthorizationFilter(HangfireConfig config)
    {
        _config = config;
    }

    public bool Authorize(DashboardContext context)
    {
        if (!_config.RequireAuth)
            return true;

        var httpContext = context.GetHttpContext();

        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            if (httpContext.User.IsInRole("Admin"))
                return true;
        }

        return CheckBasicAuth(httpContext);
    }

    private bool CheckBasicAuth(HttpContext context)
    {
        try
        {
            string authHeader = context.Request.Headers["Authorization"].FirstOrDefault() ?? "";

            if (authHeader.StartsWith("Basic "))
            {
                var encodedUsernamePassword = authHeader.Substring("Basic ".Length).Trim();
                var decodedUsernamePassword = System.Text.Encoding.UTF8.GetString(
                    Convert.FromBase64String(encodedUsernamePassword));

                var parts = decodedUsernamePassword.Split(':');
                if (parts.Length == 2)
                {
                    var username = parts[0];
                    var password = parts[1];
                    return username == _config.Username && password == _config.Password;
                }
            }

            context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Hangfire Dashboard\"";
            context.Response.StatusCode = 401;
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }
}