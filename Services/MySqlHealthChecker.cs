using MySqlConnector;
using Serilog;

namespace API.Services;

public static class MySqlHealthChecker
{
    /// <summary>
    /// Aguarda o MySQL estar disponível antes de prosseguir
    /// </summary>
    /// <param name="connectionString">String de conexão do MySQL</param>
    /// <param name="maxAttempts">Número máximo de tentativas</param>
    /// <param name="delaySeconds">Delay entre tentativas em segundos</param>
    /// <returns>Task</returns>
    /// <exception cref="InvalidOperationException">Quando MySQL não fica disponível após todas as tentativas</exception>
    public static async Task WaitForAvailabilityAsync(string connectionString, int maxAttempts = 30, int delaySeconds = 2)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            Log.Warning("⚠️ Connection string do MySQL está vazia");
            return;
        }

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

    /// <summary>
    /// Verifica se o MySQL está disponível
    /// </summary>
    /// <param name="connectionString">String de conexão</param>
    /// <returns>True se disponível, False caso contrário</returns>
    public static async Task<bool> IsAvailableAsync(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return false;

        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            await connection.CloseAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}