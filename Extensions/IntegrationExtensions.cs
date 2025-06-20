using API.Models.Configurations;
using API.Repositories;
using API.Repositories.Interfaces;
using API.Services;
using API.Services.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace API.Extensions
{
    public static class IntegrationExtensions
    {
        /// <summary>
        /// Adiciona todos os serviços de integração ao container de DI
        /// </summary>
        public static IServiceCollection AddIntegrationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Configurar objetos de configuração
            services.Configure<SalesForceConfig>(configuration.GetSection("SalesForce"));
            services.Configure<RabbitMQConfig>(configuration.GetSection("RabbitMQ"));
            services.Configure<EmailConfig>(configuration.GetSection("Email"));
            services.Configure<WebhookConfig>(configuration.GetSection("Webhook"));
            services.Configure<JobsConfig>(configuration.GetSection("Jobs"));

            // Registrar repositórios de integração
            services.AddScoped<IAtividadeRepository, AtividadeRepository>();
            services.AddScoped<IConfiguracaoIntegracaoRepository, ConfiguracaoIntegracaoRepository>();
            services.AddScoped<ILogSincronizacaoRepository, LogSincronizacaoRepository>();
            services.AddScoped<IFilaProcessamentoRepository, FilaProcessamentoRepository>();

            // Registrar serviços de integração
            services.AddScoped<IAtividadeService, AtividadeService>();
            services.AddScoped<IIntegrationService, IntegrationService>();
            services.AddScoped<IEmailService, EmailService>();

            // Registrar SalesForce service como Scoped com HttpClient
            services.AddHttpClient<ISalesForceService, SalesForceService>(client =>
            {
                var salesForceConfig = configuration.GetSection("SalesForce").Get<SalesForceConfig>();
                if (salesForceConfig != null)
                {
                    client.BaseAddress = new Uri(salesForceConfig.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(salesForceConfig.TimeoutSeconds);
                }
            });

            // Registrar RabbitMQ service como Singleton
            services.AddSingleton<IRabbitMQService, RabbitMQService>();

            return services;
        }

        /// <summary>
        /// Adiciona validação de configurações de integração
        /// </summary>
        public static IServiceCollection AddIntegrationValidation(this IServiceCollection services, IConfiguration configuration)
        {
            // Validar configuração do SalesForce
            var salesForceConfig = configuration.GetSection("SalesForce").Get<SalesForceConfig>();
            if (salesForceConfig == null)
            {
                throw new InvalidOperationException("Configuração do SalesForce não encontrada no appsettings.json");
            }

            if (string.IsNullOrEmpty(salesForceConfig.BaseUrl))
            {
                throw new InvalidOperationException("BaseUrl do SalesForce não configurada");
            }

            if (string.IsNullOrEmpty(salesForceConfig.Login))
            {
                throw new InvalidOperationException("Username do SalesForce não configurado");
            }

            // Validar configuração do RabbitMQ
            var rabbitMQConfig = configuration.GetSection("RabbitMQ").Get<RabbitMQConfig>();
            if (rabbitMQConfig == null)
            {
                throw new InvalidOperationException("Configuração do RabbitMQ não encontrada no appsettings.json");
            }

            if (string.IsNullOrEmpty(rabbitMQConfig.HostName))
            {
                throw new InvalidOperationException("HostName do RabbitMQ não configurado");
            }

            // Validar configuração de Email
            var emailConfig = configuration.GetSection("Email").Get<EmailConfig>();
            if (emailConfig == null)
            {
                throw new InvalidOperationException("Configuração de Email não encontrada no appsettings.json");
            }

            if (string.IsNullOrEmpty(emailConfig.SmtpServer))
            {
                throw new InvalidOperationException("SmtpServer não configurado");
            }

            return services;
        }

        /// <summary>
        /// Configura health checks para integração
        /// </summary>
        public static IServiceCollection AddIntegrationHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            var healthChecks = services.AddHealthChecks();

            // Health check customizado para SalesForce
            var salesForceConfig = configuration.GetSection("SalesForce").Get<SalesForceConfig>();
            if (salesForceConfig != null)
            {
                healthChecks.AddCheck<SalesForceHealthCheck>("salesforce");
            }

            // Health check customizado para RabbitMQ
            var rabbitMQConfig = configuration.GetSection("RabbitMQ").Get<RabbitMQConfig>();
            if (rabbitMQConfig != null)
            {
                healthChecks.AddCheck<RabbitMQHealthCheck>("rabbitmq");
            }

            // Health check customizado para Email
            var emailConfig = configuration.GetSection("Email").Get<EmailConfig>();
            if (emailConfig != null)
            {
                healthChecks.AddCheck<EmailHealthCheck>("email");
            }

            return services;
        }

        /// <summary>
        /// Adiciona logging específico para integração
        /// </summary>
        public static IServiceCollection AddIntegrationLogging(this IServiceCollection services)
        {
            services.AddLogging(builder =>
            {
                builder.AddFilter("API.Services.SalesForceService", LogLevel.Debug);
                builder.AddFilter("API.Services.IntegrationService", LogLevel.Information);
                builder.AddFilter("API.Services.RabbitMQService", LogLevel.Information);
                builder.AddFilter("API.Services.EmailService", LogLevel.Information);
            });

            return services;
        }

        /// <summary>
        /// Configura cache específico para integração
        /// </summary>
        public static IServiceCollection AddIntegrationCache(this IServiceCollection services)
        {
            // Cache específico para tokens de autenticação
            services.AddMemoryCache(options =>
            {
                options.SizeLimit = 1000;
                options.TrackStatistics = true;
            });

            return services;
        }

        /// <summary>
        /// Adiciona middleware específico para integração
        /// </summary>
        public static WebApplication UseIntegrationMiddleware(this WebApplication app)
        {
            // Middleware para logging de correlação de integrações
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/api/atividades") ||
                    context.Request.Path.StartsWithSegments("/api/integration") ||
                    context.Request.Path.StartsWithSegments("/api/salesforce") ||
                    context.Request.Path.StartsWithSegments("/api/webhook"))
                {
                    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                                      ?? Guid.NewGuid().ToString();

                    context.Items["CorrelationId"] = correlationId;
                    context.Response.Headers.Add("X-Correlation-ID", correlationId);
                }

                await next();
            });

            return app;
        }

        /// <summary>
        /// Inicializa dados padrão de integração
        /// </summary>
        public static async Task<WebApplication> SeedIntegrationDataAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                var configuracaoRepo = scope.ServiceProvider.GetRequiredService<IConfiguracaoIntegracaoRepository>();

                // Verificar se já existe configuração padrão
                var defaultConfig = await configuracaoRepo.GetDefaultConfigAsync();
                if (defaultConfig == null)
                {
                    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                    var salesForceConfig = configuration.GetSection("SalesForce").Get<SalesForceConfig>();

                    if (salesForceConfig != null)
                    {
                        var novaConfig = new Models.Entities.ConfiguracaoIntegracao
                        {
                            Nome = "SalesForce - Configuração Padrão",
                            Descricao = "Configuração padrão criada automaticamente",
                            UrlApi = salesForceConfig.BaseUrl,
                            Login = salesForceConfig.Login,
                            SenhaCriptografada = salesForceConfig.Password, // Em produção, criptografar
                            VersaoApi = "v1",
                            EndpointLogin = salesForceConfig.LoginEndpoint,
                            EndpointPrincipal = salesForceConfig.AtividadesEndpoint,
                            Ativo = true,
                            TimeoutSegundos = salesForceConfig.TimeoutSeconds,
                            MaxTentativas = salesForceConfig.MaxRetries,
                            ConfiguracaoPadrao = true,
                            DataCriacao = DateTime.UtcNow
                        };

                        await configuracaoRepo.CreateAsync(novaConfig);
                        logger.LogInformation("Configuração padrão de integração criada automaticamente");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao inicializar dados de integração");
            }

            return app;
        }

        /// <summary>
        /// Valida conectividade das integrações na inicialização
        /// </summary>
        public static async Task<WebApplication> ValidateIntegrationsAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                // Validar RabbitMQ
                var rabbitMQService = scope.ServiceProvider.GetRequiredService<IRabbitMQService>();
                var isRabbitConnected = await rabbitMQService.IsConnectedAsync();
                logger.LogInformation("RabbitMQ conectado: {Connected}", isRabbitConnected);

                // Validar Email
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var isEmailValid = await emailService.ValidateEmailConfigurationAsync();
                logger.LogInformation("Configuração de email válida: {Valid}", isEmailValid);

                try
                {
                    var salesForceService = scope.ServiceProvider.GetRequiredService<ISalesForceService>();
                    var healthCheck = await salesForceService.CheckHealthAsync();
                    logger.LogInformation("SalesForce saudável: {Healthy}", healthCheck.IsHealthy);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Não foi possível validar SalesForce na inicialização (pode estar indisponível)");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao validar integrações");
            }

            return app;
        }
    }

    // Health Checks customizados
    public class SalesForceHealthCheck : IHealthCheck
    {
        private readonly ISalesForceService _salesForceService;

        public SalesForceHealthCheck(ISalesForceService salesForceService)
        {
            _salesForceService = salesForceService;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _salesForceService.CheckHealthAsync();
                return result.IsHealthy
                    ? HealthCheckResult.Healthy("SalesForce está acessível")
                    : HealthCheckResult.Unhealthy("SalesForce não está acessível");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Erro ao verificar SalesForce", ex);
            }
        }
    }

    public class RabbitMQHealthCheck : IHealthCheck
    {
        private readonly IRabbitMQService _rabbitMQService;

        public RabbitMQHealthCheck(IRabbitMQService rabbitMQService)
        {
            _rabbitMQService = rabbitMQService;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var isConnected = await _rabbitMQService.IsConnectedAsync();
                return isConnected
                    ? HealthCheckResult.Healthy("RabbitMQ está conectado")
                    : HealthCheckResult.Unhealthy("RabbitMQ não está conectado");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Erro ao verificar RabbitMQ", ex);
            }
        }
    }

    public class EmailHealthCheck : IHealthCheck
    {
        private readonly IEmailService _emailService;

        public EmailHealthCheck(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var isValid = await _emailService.ValidateEmailConfigurationAsync();
                return isValid
                    ? HealthCheckResult.Healthy("Configuração de email válida")
                    : HealthCheckResult.Unhealthy("Configuração de email inválida");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Erro ao verificar configuração de email", ex);
            }
        }
    }
}