namespace API.Configuration
{
    public class CacheConfiguration
    {
        public int DefaultExpirationMinutes { get; set; } = 30;
        public int SqlQueryCacheMinutes { get; set; } = 60;
        public int UserSessionCacheMinutes { get; set; } = 120;
        public bool EnableCache { get; set; } = true;
    }

    public class RedisConfiguration
    {
        public string InstanceName { get; set; } = "API_MONTALVAO";
        public int Database { get; set; } = 0;
        public int ConnectTimeout { get; set; } = 5000;
        public int SyncTimeout { get; set; } = 5000;
        public int KeepAlive { get; set; } = 60;
        public int ConnectRetry { get; set; } = 3;
    }

    public static class CacheKeys
    {
        public const string SQL_QUERY_PREFIX = "sql:";
        public const string USER_SESSION_PREFIX = "user:";
        public const string API_RESPONSE_PREFIX = "api:";

        public static string SqlQuery(string query) => $"{SQL_QUERY_PREFIX}{query.GetHashCode()}";
        public static string UserSession(string userId) => $"{USER_SESSION_PREFIX}{userId}";
        public static string ApiResponse(string endpoint, params object[] parameters)
            => $"{API_RESPONSE_PREFIX}{endpoint}:{string.Join(":", parameters)}";
    }
}