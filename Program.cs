using API.Extensions;
using API.Configuration;

var builder = WebApplication.CreateBuilder(args);

try
{
    // CONFIGURAÇÃO DE LOGGING
    builder.ConfigureLogging();

    // CONFIGURAÇÃO DE SERVIÇOS
    await builder.ConfigureServicesAsync();

    //  CONSTRUIR A APLICAÇÃO
    var app = builder.Build();

    // CONFIGURAR PIPELINE
    await app.ConfigureApplicationAsync();

    // INICIAR APLICAÇÃO
    await app.StartApplicationAsync();
}
catch (Exception ex)
{
    Serilog.Log.Fatal(ex, "Erro crítico na inicialização da aplicação");
    throw;
}
finally
{
    Serilog.Log.Information("Encerrando aplicação...");
    await Serilog.Log.CloseAndFlushAsync();
}