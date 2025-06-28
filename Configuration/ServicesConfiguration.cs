using API.Extensions;
using API.Services;
using Serilog;

namespace API.Configuration;

public static class ServicesConfiguration
{
    public static async Task<WebApplicationBuilder> ConfigureServicesAsync(this WebApplicationBuilder builder)
    {
        Log.Information("🔧 Iniciando configuração de serviços...");

        // 📋 CONFIGURAÇÕES BÁSICAS
        builder.Services.ConfigureBasicServices();

        // 🔐 AUTENTICAÇÃO E AUTORIZAÇÃO
        builder.Services.ConfigureAuthentication(builder.Configuration);

        // 🗄️ BANCO DE DADOS E CACHE
        builder.Services.ConfigureDatabaseAndCache(builder.Configuration);

        // 🔗 INTEGRAÇÃO
        builder.Services.ConfigureIntegrationServices(builder.Configuration);

        // 🚀 HANGFIRE (COM VERIFICAÇÃO)
        await builder.Services.ConfigureHangfireWithHealthCheckAsync(builder.Configuration);

        // 🌐 CORS E RATE LIMITING
        builder.Services.ConfigureWebServices();

        // 📊 MAPEAMENTO E VALIDAÇÃO
        builder.Services.ConfigureMappingAndValidation();

        // 📈 REPOSITÓRIOS E SERVIÇOS DE NEGÓCIO
        builder.Services.ConfigureBusinessServices();

        // 🔍 HEALTH CHECKS E MÉTRICAS
        builder.Services.ConfigureMonitoring(builder.Configuration);

        Log.Information("✅ Configuração de serviços concluída");
        return builder;
    }

    private static IServiceCollection ConfigureBasicServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddControllersConfiguration();
        services.AddSwaggerConfiguration();
        return services;
    }

    private static IServiceCollection ConfigureAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthenticationConfiguration(configuration);
        services.AddAuthorizationPolicies();
        return services;
    }

    private static IServiceCollection ConfigureDatabaseAndCache(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSqlConfiguration();
        services.AddRedisConfiguration(configuration);
        return services;
    }

    private static IServiceCollection ConfigureIntegrationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIntegrationServices(configuration);
        services.AddIntegrationValidation(configuration);
        services.AddIntegrationHealthChecks(configuration);
        services.AddIntegrationLogging();
        services.AddIntegrationCache();
        return services;
    }

    private static async Task<IServiceCollection> ConfigureHangfireWithHealthCheckAsync(this IServiceCollection services, IConfiguration configuration)
    {
        try
        {
            Log.Information("🔄 Verificando disponibilidade do MySQL para Hangfire...");

            var connectionString = configuration.GetConnectionString("ConfiguracaoPadrao");
            if (!string.IsNullOrEmpty(connectionString))
            {
                await MySqlHealthChecker.WaitForAvailabilityAsync(connectionString);
                Log.Information("✅ MySQL verificado e disponível");
            }

            services.AddHangfireConfiguration(configuration);
            Log.Information("✅ Hangfire configurado com sucesso");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "⚠️ Erro na configuração do Hangfire - continuando sem Hangfire");
        }

        return services;
    }

    private static IServiceCollection ConfigureWebServices(this IServiceCollection services)
    {
        services.AddCorsConfiguration();
        services.AddRateLimitingConfiguration();
        return services;
    }

    private static IServiceCollection ConfigureMappingAndValidation(this IServiceCollection services)
    {
        services.AddMappingConfiguration();
        services.AddValidationConfiguration();
        return services;
    }

    private static IServiceCollection ConfigureBusinessServices(this IServiceCollection services)
    {
        services.AddRepositoryConfiguration();
        services.AddServiceConfiguration();
        return services;
    }

    private static IServiceCollection ConfigureMonitoring(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecksConfiguration(configuration);
        services.AddPrometheusMetrics();
        return services;
    }
}