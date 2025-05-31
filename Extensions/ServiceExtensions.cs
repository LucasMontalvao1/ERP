using API.Filters;
using API.Infra.Data;
using API.SQL;
using API.Services.Cache;
using API.Configuration;
using API.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Text;
using System.Threading.RateLimiting;
using API.Services.Cache.Interfaces;

namespace API.Extensions
{
    public static class ServiceExtensions
    {
        /// <summary>
        /// Adiciona a configuração de controladores e filtros
        /// </summary>
        public static IServiceCollection AddControllersConfiguration(this IServiceCollection services)
        {
            services.AddControllers(options =>
            {
                // options.Filters.Add<ApiExceptionFilter>();
            })
            .AddJsonOptions(options =>
            {
                // Configurações adicionais do JSON, se necessário
                options.JsonSerializerOptions.WriteIndented = true;
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            });
            // Registrar filtros de exceção como serviços para injeção de dependência
            services.AddScoped<ApiExceptionFilter>();

            return services;
        }

        /// <summary>
        /// Adiciona a configuração do Swagger com suporte JWT
        /// </summary>
        public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(ApiConstants.ApiVersion, new OpenApiInfo
                {
                    Title = ApiConstants.ApiName,
                    Version = ApiConstants.ApiVersion
                });

                // Configuração para autenticação JWT no Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = @"Digite seu token no campo abaixo. Exemplo: 'DlHPFGcvUqHQSw+BNFQfASg=='",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                // Incluir comentários XML se houver
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });

            return services;
        }

        /// <summary>
        /// Adiciona configurações do AutoMapper
        /// </summary>
        public static IServiceCollection AddMappingConfiguration(this IServiceCollection services)
        {
            // services.AddAutoMapper(typeof(Program));
            return services;
        }

        /// <summary>
        /// Adiciona configurações de validação com FluentValidation
        /// </summary>
        public static IServiceCollection AddValidationConfiguration(this IServiceCollection services)
        {
            // services.AddFluentValidationAutoValidation();
            // services.AddFluentValidationClientsideAdapters();
            // services.AddValidatorsFromAssemblyContaining<Program>();

            return services;
        }

        /// <summary>
        /// Adiciona configurações SQL
        /// </summary>
        public static IServiceCollection AddSqlConfiguration(this IServiceCollection services)
        {
            services.AddScoped<SqlLoader>();

            // Registrar os arquivos SQL
            SqlLoader.RegisterSqlFiles(services);

            // Configuração do Database Service
            services.AddScoped<IDatabaseService, MySqlDatabaseService>();

            return services;
        }

        /// <summary>
        /// Adiciona configuração do Redis com cache
        /// </summary>
        public static IServiceCollection AddRedisConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            // Configurar objetos tipados
            services.Configure<CacheConfiguration>(configuration.GetSection("Cache"));
            services.Configure<RedisConfiguration>(configuration.GetSection("Redis"));

            var connectionString = configuration.GetConnectionString("Redis");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Redis connection string não configurada no appsettings.json");
            }

            // Verificar se o cache está habilitado
            var cacheConfig = configuration.GetSection("Cache").Get<CacheConfiguration>();
            if (cacheConfig?.EnableCache != true)
            {
                // Se cache desabilitado, registrar implementação vazia
                services.AddSingleton<ICacheService, NullCacheService>();
                return services;
            }

            // Configurar Redis Connection
            var redisConfig = configuration.GetSection("Redis").Get<RedisConfiguration>() ?? new RedisConfiguration();

            services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<IConnectionMultiplexer>>();

                try
                {
                    var configurationOptions = ConfigurationOptions.Parse(connectionString);
                    configurationOptions.ConnectTimeout = redisConfig.ConnectTimeout;
                    configurationOptions.SyncTimeout = redisConfig.SyncTimeout;
                    configurationOptions.KeepAlive = redisConfig.KeepAlive;
                    configurationOptions.ConnectRetry = redisConfig.ConnectRetry;
                    configurationOptions.AbortOnConnectFail = false;

                    var connection = ConnectionMultiplexer.Connect(configurationOptions);

                    logger.LogInformation("Redis conectado com sucesso: {Endpoints}",
                        string.Join(", ", connection.GetEndPoints().Select(e => e.ToString())));

                    return connection;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Erro ao conectar com Redis. Usando cache em memória como fallback");
                    throw;
                }
            });

            // Registrar serviços de cache
            services.AddSingleton<ICacheService, RedisCacheService>();

            // Cache distribuído do ASP.NET Core
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = connectionString;
                options.InstanceName = redisConfig.InstanceName;
            });

            return services;
        }

        /// <summary>
        /// Adiciona repositórios ao container de serviços
        /// </summary>
        public static IServiceCollection AddRepositoryConfiguration(this IServiceCollection services)
        {
            // services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            // services.AddScoped<IProdutoRepository, ProdutoRepository>();

            return services;
        }

        /// <summary>
        /// Adiciona serviços de negócio ao container de serviços
        /// </summary>
        public static IServiceCollection AddServiceConfiguration(this IServiceCollection services)
        {
            // services.AddScoped<IUsuarioService, UsuarioService>();
            // services.AddScoped<IProdutoService, ProdutoService>();

            return services;
        }

        /// <summary>
        /// Adiciona configuração de CORS
        /// </summary>
        public static IServiceCollection AddCorsConfiguration(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(ApiConstants.DefaultCorsPolicyName,
                    builder => builder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
            });

            return services;
        }


        /// <summary>
        /// Implementação de cache nulo para quando cache está desabilitado
        /// </summary>
        private class NullCacheService : ICacheService
        {
            public Task<T?> GetAsync<T>(string key) where T : class => Task.FromResult<T?>(null);
            public Task<string?> GetAsync(string key) => Task.FromResult<string?>(null);
            public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class => Task.CompletedTask;
            public Task SetAsync(string key, string value, TimeSpan? expiration = null) => Task.CompletedTask;
            public Task RemoveAsync(string key) => Task.CompletedTask;
            public Task RemoveByPatternAsync(string pattern) => Task.CompletedTask;
            public Task<bool> ExistsAsync(string key) => Task.FromResult(false);
            public Task<long> IncrementAsync(string key, long value = 1) => Task.FromResult(0L);
            public Task<long> DecrementAsync(string key, long value = 1) => Task.FromResult(0L);
        }
    }
}