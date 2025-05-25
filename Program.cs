using API.Extensions;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog 
ConfigureLogger();

// Usar Serilog como provider de logging
builder.Host.UseSerilog();

// Adicionar serviços ao container
builder.Services.AddEndpointsApiExplorer();

// Configurar serviços da aplicação
builder.Services
    .AddControllersConfiguration()  
    .AddSwaggerConfiguration()
    .AddMappingConfiguration()
    .AddValidationConfiguration()
    .AddSqlConfiguration()
    .AddRedisConfiguration(builder.Configuration)
    .AddRateLimitingConfiguration()
    .AddHealthChecksConfiguration(builder.Configuration)
    .AddPrometheusMetrics()
    .AddRepositoryConfiguration()
    .AddServiceConfiguration()
    .AddCorsConfiguration()
    .AddAuthenticationConfiguration(builder.Configuration);

// Construir o aplicativo
var app = builder.Build();

// Configurar pipeline de requisições
app.ConfigureApplicationPipeline(app.Environment)
   .UsePrometheusMetrics();

try
{
    Log.Information("Iniciando aplicação...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Erro fatal ao iniciar o aplicativo");
}
finally
{
    Log.CloseAndFlush();
}

// Configuração do logger
void ConfigureLogger()
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "API ERP")
        .WriteTo.Console()
        .WriteTo.File("Logs/myapp-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 25,
            fileSizeLimitBytes: 10_000_000,
            rollOnFileSizeLimit: true)
        .CreateLogger();
}