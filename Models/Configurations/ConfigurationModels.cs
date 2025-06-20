namespace API.Models.Configurations
{
    /// <summary>
    /// Configurações do SalesForce baseadas no appsettings.json
    /// </summary>
    public class SalesForceConfig
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string LoginEndpoint { get; set; } = string.Empty;
        public string AtividadesEndpoint { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxRetries { get; set; } = 3;
        public int RetryDelaySeconds { get; set; } = 60;

        /// <summary>
        /// Tempo em minutos para manter o token em cache
        /// </summary>
        public int TokenCacheMinutes { get; set; } = 50;
    }

    /// <summary>
    /// Configurações de Email baseadas no appsettings.json
    /// </summary>
    public class EmailConfig
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Configurações de Webhook baseadas no appsettings.json
    /// </summary>
    public class WebhookConfig
    {
        public string SecretKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Configurações de Jobs baseadas no appsettings.json
    /// </summary>
    public class JobsConfig
    {
        public string IntegrationRetrySchedule { get; set; } = string.Empty;
        public string EmailProcessingSchedule { get; set; } = string.Empty;
        public string MaintenanceSchedule { get; set; } = string.Empty;
        public string ReportGenerationSchedule { get; set; } = string.Empty;
    }
}