using API.Extensions;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog 
ConfigureLogger();

// Usar Serilog como provider de logging
builder.Host.UseSerilog();

// Adicionar servi�os ao container
builder.Services.AddEndpointsApiExplorer();

// Configurar servi�os da aplica��o
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

// Configurar pipeline de requisi��es
app.ConfigureApplicationPipeline(app.Environment)
   .UsePrometheusMetrics();

try
{
    Log.Information("Iniciando aplica��o...");
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

// Configura��o do logger
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