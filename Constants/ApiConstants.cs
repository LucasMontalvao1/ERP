namespace API.Constants
{
    /// <summary>
    /// Classe para armazenar constantes da API
    /// </summary>
    public static class ApiConstants
    {
        /// <summary>
        /// Versão atual da API
        /// </summary>
        public const string ApiVersion = "v1";

        /// <summary>
        /// Nome da API
        /// </summary>
        public const string ApiName = "API MONTALVAO";

        /// <summary>
        /// Nome da política CORS padrão
        /// </summary>
        public const string DefaultCorsPolicyName = "AllowAll";

        /// <summary>
        /// Diretório base para logs
        /// </summary>
        public const string LogsDirectory = "Logs";

        /// <summary>
        /// Formato do arquivo de log
        /// </summary>
        public const string LogFileFormat = "app-.log";

        /// <summary>
        /// Nome da instância Redis
        /// </summary>
        public const string RedisInstanceName = "API_MONTALVAO";

        /// <summary>
        /// Prefixos para cache
        /// </summary>
        public static class CachePrefixes
        {
            public const string SqlQuery = "sql:";
            public const string UserSession = "user:";
            public const string ApiResponse = "api:";
            public const string Authentication = "auth:";
            public const string Permission = "perm:";
        }

        /// <summary>
        /// Políticas de Rate Limiting
        /// </summary>
        public static class RateLimitPolicies
        {
            public const string Default = "ApiPolicy";
            public const string Authentication = "AuthPolicy";
            public const string Public = "PublicPolicy";
        }

        /// <summary>
        /// Roles de usuário
        /// </summary>
        public static class UserRoles
        {
            public const string Admin = "Admin";
            public const string User = "User";
            public const string Manager = "Manager";
            public const string Viewer = "Viewer";
        }

        /// <summary>
        /// Claims personalizados
        /// </summary>
        public static class CustomClaims
        {
            public const string UserId = "user_id";
            public const string CompanyId = "company_id";
            public const string Permissions = "permissions";
            public const string Role = "role";
            public const string Email = "email";
        }

        /// <summary>
        /// Mensagens de erro padrão
        /// </summary>
        public static class ErrorMessages
        {
            public const string Unauthorized = "Usuário não autorizado";
            public const string NotFound = "Recurso não encontrado";
            public const string ValidationError = "Erro de validação";
            public const string InternalError = "Erro interno do servidor";
            public const string RateLimitExceeded = "Limite de requisições excedido. Tente novamente em alguns segundos.";
            public const string InvalidCredentials = "Credenciais inválidas";
            public const string ExpiredToken = "Token expirado";
            public const string InvalidToken = "Token inválido";
            public const string AccessDenied = "Acesso negado";
            public const string ResourceExists = "Recurso já existe";
            public const string InvalidOperation = "Operação inválida";
            public const string DatabaseError = "Erro de banco de dados";
            public const string CacheError = "Erro no sistema de cache";
        }

        /// <summary>
        /// Mensagens de sucesso padrão
        /// </summary>
        public static class SuccessMessages
        {
            public const string Created = "Recurso criado com sucesso";
            public const string Updated = "Recurso atualizado com sucesso";
            public const string Deleted = "Recurso removido com sucesso";
            public const string Retrieved = "Dados recuperados com sucesso";
            public const string LoginSuccess = "Login realizado com sucesso";
            public const string LogoutSuccess = "Logout realizado com sucesso";
        }

        /// <summary>
        /// Configurações padrão
        /// </summary>
        public static class Defaults
        {
            public const int PageSize = 20;
            public const int MaxPageSize = 100;
            public const int MinPageSize = 5;
            public const int CacheExpirationMinutes = 30;
            public const int JwtExpirationMinutes = 360;
            public const int RefreshTokenExpirationDays = 30;
            public const int MaxLoginAttempts = 5;
            public const int LockoutMinutes = 15;
        }

        /// <summary>
        /// Headers HTTP personalizados
        /// </summary>
        public static class CustomHeaders
        {
            public const string ApiVersion = "X-API-Version";
            public const string RequestId = "X-Request-ID";
            public const string Correlation = "X-Correlation-ID";
            public const string ExecutionTime = "X-Execution-Time";
            public const string RateLimit = "X-RateLimit-Remaining";
            public const string TotalCount = "X-Total-Count";
        }

        /// <summary>
        /// Tipos de conteúdo
        /// </summary>
        public static class ContentTypes
        {
            public const string Json = "application/json";
            public const string Xml = "application/xml";
            public const string Pdf = "application/pdf";
            public const string Excel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            public const string Csv = "text/csv";
        }

        /// <summary>
        /// Códigos de status HTTP mais usados
        /// </summary>
        public static class StatusCodes
        {
            public const int Ok = 200;
            public const int Created = 201;
            public const int NoContent = 204;
            public const int BadRequest = 400;
            public const int Unauthorized = 401;
            public const int Forbidden = 403;
            public const int NotFound = 404;
            public const int Conflict = 409;
            public const int UnprocessableEntity = 422;
            public const int TooManyRequests = 429;
            public const int InternalServerError = 500;
        }

        /// <summary>
        /// Configurações de validação
        /// </summary>
        public static class Validation
        {
            public const int MinPasswordLength = 6;
            public const int MaxPasswordLength = 100;
            public const int MinUsernameLength = 3;
            public const int MaxUsernameLength = 50;
            public const int MaxEmailLength = 254;
            public const int MaxNameLength = 100;
            public const int MaxDescriptionLength = 500;
        }

        /// <summary>
        /// Padrões Regex comuns
        /// </summary>
        public static class RegexPatterns
        {
            public const string Email = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            public const string Phone = @"^\(\d{2}\)\s\d{4,5}-\d{4}$";
            public const string Cpf = @"^\d{3}\.\d{3}\.\d{3}-\d{2}$";
            public const string Cnpj = @"^\d{2}\.\d{3}\.\d{3}/\d{4}-\d{2}$";
            public const string OnlyNumbers = @"^\d+$";
            public const string OnlyLetters = @"^[a-zA-ZÀ-ÿ\s]+$";
        }

        /// <summary>
        /// Configurações de cache específicas
        /// </summary>
        public static class CacheSettings
        {
            public const int ShortCacheMinutes = 5;
            public const int MediumCacheMinutes = 30;
            public const int LongCacheMinutes = 120;
            public const int SqlCacheHours = 1;
            public const int UserSessionHours = 8;
            public const int StaticDataHours = 24;
        }

        /// <summary>
        /// Eventos de auditoria
        /// </summary>
        public static class AuditEvents
        {
            public const string UserLogin = "USER_LOGIN";
            public const string UserLogout = "USER_LOGOUT";
            public const string UserCreated = "USER_CREATED";
            public const string UserUpdated = "USER_UPDATED";
            public const string UserDeleted = "USER_DELETED";
            public const string PasswordChanged = "PASSWORD_CHANGED";
            public const string PermissionGranted = "PERMISSION_GRANTED";
            public const string PermissionRevoked = "PERMISSION_REVOKED";
            public const string DataExported = "DATA_EXPORTED";
            public const string SystemConfigChanged = "SYSTEM_CONFIG_CHANGED";
        }
    }
}