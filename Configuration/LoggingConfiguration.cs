using Serilog;
using Serilog.Events;

namespace API.Configuration;

public static class LoggingConfiguration
{
    public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
    {
        var environment = builder.Environment.EnvironmentName;
        var applicationName = builder.Configuration.GetValue<string>("ApplicationName") ?? "API ERP";

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .ConfigureMinimumLevels()
            .EnrichWithContext(applicationName, environment)
            .ConfigureWriteTo()
            .CreateLogger();

        builder.Host.UseSerilog();
        return builder;
    }

    private static LoggerConfiguration ConfigureMinimumLevels(this LoggerConfiguration config)
    {
        return config
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Hangfire", LogEventLevel.Information)
            .MinimumLevel.Override("RabbitMQ.Client", LogEventLevel.Warning);
    }

    private static LoggerConfiguration EnrichWithContext(this LoggerConfiguration config, string applicationName, string environment)
    {
        return config
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", applicationName)
            .Enrich.WithProperty("Environment", environment)
            .Enrich.WithProperty("MachineName", Environment.MachineName)
            .Enrich.WithProperty("ProcessId", Environment.ProcessId);
    }

    private static LoggerConfiguration ConfigureWriteTo(this LoggerConfiguration config)
    {
        return config
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File("Logs/Api/api-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                fileSizeLimitBytes: 50_000_000,
                rollOnFileSizeLimit: true,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext} {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File("Logs/Erros/errors-.log",
                restrictedToMinimumLevel: LogEventLevel.Error,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 60,
                fileSizeLimitBytes: 20_000_000,
                rollOnFileSizeLimit: true,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {Level:u3} {SourceContext} {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File("Logs/Integracao/integration-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 15,
                fileSizeLimitBytes: 30_000_000,
                rollOnFileSizeLimit: true,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {Level:u3} {SourceContext} {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File("Logs/jobs/jobs-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 15,
                fileSizeLimitBytes: 30_000_000,
                rollOnFileSizeLimit: true,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {Level:u3} {Message:lj} {Properties:j}{NewLine}{Exception}");
    }
}