using System.Reflection;
using API.Services.Cache;
using API.Configuration;
using API.Services.Cache.Interfaces;

namespace API.SQL
{
    public class SqlLoader
    {
        private static readonly Dictionary<string, string> _sqlQueries = new();
        private readonly ILogger<SqlLoader> _logger;
        private readonly ICacheService? _cacheService;

        public SqlLoader(ILogger<SqlLoader> logger, ICacheService? cacheService = null)
        {
            _logger = logger;
            _cacheService = cacheService;
        }

        /// <summary>
        /// Carrega um arquivo SQL do assembly atual com suporte a cache.
        /// </summary>
        /// <param name="path">Caminho relativo do arquivo SQL.</param>
        /// <returns>Conteúdo do arquivo SQL.</returns>
        public async Task<string> LoadSqlAsync(string path)
        {
            // 1. Tentar buscar do cache Redis primeiro
            if (_cacheService != null)
            {
                string cacheKey = CacheKeys.SqlQuery(path);
                var cachedSql = await _cacheService.GetAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedSql))
                {
                    _logger.LogDebug("SQL encontrado no cache Redis: {Path}", path);
                    return cachedSql;
                }
            }

            // 2. Tentar buscar do cache em memória
            if (_sqlQueries.TryGetValue(path, out string? sql))
            {
                _logger.LogDebug("SQL encontrado no cache em memória: {Path}", path);

                // Salvar no Redis para próximas consultas (outras instâncias)
                if (_cacheService != null && !string.IsNullOrEmpty(sql))
                {
                    await _cacheService.SetAsync(CacheKeys.SqlQuery(path), sql, TimeSpan.FromHours(1));
                }

                return sql;
            }

            _logger.LogWarning("SQL não encontrado em cache: {Path}", path);

            // 3. Buscar no assembly (código original)
            var assembly = Assembly.GetExecutingAssembly();
            var resources = assembly.GetManifestResourceNames();
            _logger.LogInformation("Recursos disponíveis: {Resources}", string.Join(", ", resources));

            string normalizedPath = path.Replace("/", ".");
            string fullResourceName = $"{assembly.GetName().Name}.SQL.{normalizedPath}";
            string? resourceName = null;

            // Estratégia 1: nome exato
            if (resources.Contains(fullResourceName))
            {
                resourceName = fullResourceName;
                _logger.LogInformation("Recurso encontrado pelo nome exato: {Name}", resourceName);
            }

            // Estratégia 2: substring
            if (resourceName == null)
            {
                var possible = resources.Where(r => r.Contains(normalizedPath)).ToList();
                if (possible.Any())
                {
                    resourceName = possible.First();
                    _logger.LogInformation("Recurso encontrado por substring: {Name}", resourceName);
                }
            }

            // Estratégia 3: termina com nome do arquivo
            if (resourceName == null)
            {
                string fileName = Path.GetFileName(path);
                var possible = resources.Where(r => r.EndsWith(fileName, StringComparison.OrdinalIgnoreCase)).ToList();
                if (possible.Any())
                {
                    resourceName = possible.First();
                    _logger.LogInformation("Recurso encontrado pelo nome do arquivo: {Name}", resourceName);
                }
            }

            if (resourceName != null)
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    sql = await reader.ReadToEndAsync();

                    // Salvar em ambos os caches
                    _sqlQueries[path] = sql;

                    if (_cacheService != null)
                    {
                        await _cacheService.SetAsync(CacheKeys.SqlQuery(path), sql, TimeSpan.FromHours(1));
                        _logger.LogDebug("SQL salvo no cache Redis: {Path}", path);
                    }

                    return sql;
                }
            }

            // Tentativa final: ler do disco
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string fullPath = Path.Combine(basePath, "SQL", path);
                if (File.Exists(fullPath))
                {
                    _logger.LogInformation("Arquivo encontrado no sistema de arquivos: {Path}", fullPath);
                    sql = await File.ReadAllTextAsync(fullPath);

                    // Salvar em ambos os caches
                    _sqlQueries[path] = sql;

                    if (_cacheService != null)
                    {
                        await _cacheService.SetAsync(CacheKeys.SqlQuery(path), sql, TimeSpan.FromHours(1));
                    }

                    return sql;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao tentar ler arquivo do disco: {Path}", path);
            }

            throw new FileNotFoundException($"Arquivo SQL '{path}' não encontrado. Recursos disponíveis: {string.Join(", ", resources)}", path);
        }

        /// <summary>
        /// Limpa o cache de SQL (útil para desenvolvimento)
        /// </summary>
        public async Task ClearCacheAsync()
        {
            _sqlQueries.Clear();

            if (_cacheService != null)
            {
                await _cacheService.RemoveByPatternAsync("sql:*");
                _logger.LogInformation("Cache SQL limpo");
            }
        }

        /// <summary>
        /// Registra os arquivos SQL embutidos no assembly como recursos.
        /// </summary>
        public static void RegisterSqlFiles(IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var allResources = assembly.GetManifestResourceNames();
            Console.WriteLine($"Todos os recursos encontrados: {string.Join(", ", allResources)}");

            var sqlResources = allResources
                .Where(name => name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Console.WriteLine($"Recursos SQL encontrados: {string.Join(", ", sqlResources)}");

            if (sqlResources.Count == 0)
            {
                Console.WriteLine("AVISO: Nenhum arquivo SQL encontrado como recurso embutido!");
                return;
            }

            foreach (var resourceName in sqlResources)
            {
                try
                {
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream == null)
                    {
                        Console.WriteLine($"Não foi possível ler o recurso: {resourceName}");
                        continue;
                    }

                    using var reader = new StreamReader(stream);
                    var sqlContent = reader.ReadToEnd();
                    var path = ConvertResourceNameToPath(resourceName);

                    Console.WriteLine($"Registrando SQL: {resourceName} como {path}");
                    _sqlQueries[path] = sqlContent;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao processar recurso {resourceName}: {ex.Message}");
                }
            }

            Console.WriteLine($"Total de SQLs registrados: {_sqlQueries.Count}");
            Console.WriteLine($"SQLs disponíveis: {string.Join(", ", _sqlQueries.Keys)}");
        }

        /// <summary>
        /// Converte um nome de recurso embutido em um caminho relativo.
        /// </summary>
        private static string ConvertResourceNameToPath(string resourceName)
        {
            try
            {
                var parts = resourceName.Split('.');
                int sqlIndex = Array.IndexOf(parts, "SQL");

                if (sqlIndex >= 0 && sqlIndex < parts.Length - 1)
                {
                    var pathParts = parts.Skip(sqlIndex + 1).Take(parts.Length - sqlIndex - 2);
                    return string.Join('/', pathParts) + "/" + parts[^2] + ".sql";
                }

                int dotSqlDotIndex = resourceName.IndexOf(".SQL.");
                if (dotSqlDotIndex >= 0)
                {
                    var pathWithDots = resourceName.Substring(dotSqlDotIndex + 5);
                    if (pathWithDots.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                        pathWithDots = pathWithDots[..^4];

                    return pathWithDots.Replace('.', '/') + ".sql";
                }

                string fallback = resourceName.Replace('.', '/');
                if (fallback.EndsWith("/sql", StringComparison.OrdinalIgnoreCase))
                    fallback = fallback[..^4] + ".sql";

                return fallback;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao converter nome de recurso: {ex.Message}");
                return resourceName;
            }
        }
    }
}