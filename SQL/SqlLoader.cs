using System.Collections.Concurrent;
using System.Reflection;

namespace API.SQL
{
    public class SqlLoader
    {
        private static readonly ConcurrentDictionary<string, string> _sqlQueries = new();
        private readonly ILogger<SqlLoader> _logger;

        public SqlLoader(ILogger<SqlLoader> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Registra todos os arquivos SQL do projeto
        /// </summary>
        public static void RegisterSqlFiles(IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var sqlResources = assembly.GetManifestResourceNames()
                .Where(name => name.EndsWith(".sql"))
                .ToList();

            foreach (var resourceName in sqlResources)
            {
                try
                {
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        using var reader = new StreamReader(stream);
                        var content = reader.ReadToEnd();

                        // Extrair categoria e nome do arquivo do recurso
                        var parts = resourceName.Split('.');
                        if (parts.Length >= 3)
                        {
                            var category = parts[^3]; // Antepenúltimo elemento
                            var fileName = parts[^2];  // Penúltimo elemento (sem .sql)

                            RegisterSqlFile(category, fileName, content);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao carregar recurso SQL {resourceName}: {ex.Message}");
                }
            }

            // Se não encontrou recursos, carregar dos arquivos físicos
            if (_sqlQueries.IsEmpty)
            {
                LoadSqlFromFiles();
            }
        }

        /// <summary>
        /// Carrega arquivos SQL do sistema de arquivos
        /// </summary>
        private static void LoadSqlFromFiles()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var sqlDirectory = Path.Combine(baseDirectory, "SQL");

            if (!Directory.Exists(sqlDirectory))
            {
                Console.WriteLine($"Diretório SQL não encontrado: {sqlDirectory}");
                return;
            }

            // Buscar todas as pastas dentro do diretório SQL
            var categoryDirectories = Directory.GetDirectories(sqlDirectory);

            foreach (var categoryPath in categoryDirectories)
            {
                var category = Path.GetFileName(categoryPath);
                LoadCategorySqlFiles(category, categoryPath);
            }

            Console.WriteLine($"Carregadas {_sqlQueries.Count} queries SQL de {categoryDirectories.Length} categorias");
        }

        /// <summary>
        /// Carrega arquivos SQL de uma categoria específica
        /// </summary>
        private static void LoadCategorySqlFiles(string category, string categoryPath)
        {
            var sqlFiles = Directory.GetFiles(categoryPath, "*.sql", SearchOption.AllDirectories);

            foreach (var filePath in sqlFiles)
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    var content = File.ReadAllText(filePath);
                    RegisterSqlFile(category, fileName, content);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao carregar arquivo SQL {filePath}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Registra o conteúdo de um arquivo SQL individual
        /// </summary>
        private static void RegisterSqlFile(string category, string fileName, string content)
        {
            var key = $"{category}.{fileName}";

            // Limpar o conteúdo (remover comentários extras, espaços em branco)
            var cleanContent = CleanSqlContent(content);

            if (_sqlQueries.TryAdd(key, cleanContent))
            {
                Console.WriteLine($"Registrada query: {key}");
            }
            else
            {
                Console.WriteLine($"Query duplicada ignorada: {key}");
            }
        }

        /// <summary>
        /// Limpa o conteúdo SQL removendo comentários desnecessários e formatando
        /// </summary>
        private static string CleanSqlContent(string content)
        {
            var lines = content.Split('\n');
            var cleanLines = new List<string>();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Ignorar linhas vazias e comentários que começam com --
                if (!string.IsNullOrWhiteSpace(trimmedLine) && !trimmedLine.StartsWith("--"))
                {
                    cleanLines.Add(line);
                }
            }

            return string.Join("\n", cleanLines).Trim();
        }

        /// <summary>
        /// Obtém uma query SQL específica
        /// </summary>
        public string GetQuery(string category, string queryName)
        {
            var key = $"{category}.{queryName}";

            if (_sqlQueries.TryGetValue(key, out var query))
            {
                return query;
            }

            _logger.LogWarning("Query SQL não encontrada: {Category}.{QueryName}", category, queryName);
            throw new InvalidOperationException($"Query SQL não encontrada: {key}");
        }

        /// <summary>
        /// Verifica se uma query existe
        /// </summary>
        public bool HasQuery(string category, string queryName)
        {
            var key = $"{category}.{queryName}";
            return _sqlQueries.ContainsKey(key);
        }

        /// <summary>
        /// Lista todas as queries disponíveis
        /// </summary>
        public Dictionary<string, List<string>> GetAvailableQueries()
        {
            var result = new Dictionary<string, List<string>>();

            foreach (var key in _sqlQueries.Keys)
            {
                var parts = key.Split('.');
                if (parts.Length >= 2)
                {
                    var category = parts[0];
                    var queryName = string.Join(".", parts.Skip(1));

                    if (!result.ContainsKey(category))
                    {
                        result[category] = new List<string>();
                    }

                    result[category].Add(queryName);
                }
            }

            return result;
        }

        /// <summary>
        /// Recarrega todas as queries SQL
        /// </summary>
        public void ReloadQueries()
        {
            _sqlQueries.Clear();
            LoadSqlFromFiles();
            _logger.LogInformation("Queries SQL recarregadas. Total: {Count}", _sqlQueries.Count);
        }

        /// <summary>
        /// Obtém estatísticas das queries carregadas
        /// </summary>
        public object GetStatistics()
        {
            var categoriesStats = _sqlQueries.Keys
                .Select(key => key.Split('.')[0])
                .GroupBy(category => category)
                .ToDictionary(
                    group => group.Key,
                    group => group.Count()
                );

            return new
            {
                TotalQueries = _sqlQueries.Count,
                Categories = categoriesStats.Count,
                QueriesByCategory = categoriesStats,
                LoadedAt = DateTime.UtcNow,
                AvailableQueries = GetAvailableQueries()
            };
        }

        /// <summary>
        /// Obtém query com substituição de parâmetros de template
        /// </summary>
        public string GetQueryWithTemplate(string category, string queryName, Dictionary<string, string>? templateParams = null)
        {
            var query = GetQuery(category, queryName);

            if (templateParams != null)
            {
                foreach (var param in templateParams)
                {
                    query = query.Replace($"{{{{${param.Key}}}}}", param.Value);
                }
            }

            return query;
        }

        /// <summary>
        /// Valida se uma query está sintaticamente correta (básico)
        /// </summary>
        public bool ValidateQuery(string query)
        {
            try
            {
                // Validações básicas
                if (string.IsNullOrWhiteSpace(query))
                    return false;

                // Verificar se não contém comandos perigosos
                var dangerousCommands = new[] { "DROP", "TRUNCATE", "DELETE FROM", "ALTER", "CREATE USER", "GRANT" };
                var upperQuery = query.ToUpperInvariant();

                foreach (var cmd in dangerousCommands)
                {
                    if (upperQuery.Contains(cmd))
                    {
                        _logger.LogWarning("Query contém comando potencialmente perigoso: {Command}", cmd);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar query SQL");
                return false;
            }
        }

        /// <summary>
        /// Obtém o conteúdo bruto de uma query (útil para debug)
        /// </summary>
        public string GetRawQuery(string category, string queryName)
        {
            var key = $"{category}.{queryName}";
            return _sqlQueries.TryGetValue(key, out var query) ? query : string.Empty;
        }

        /// <summary>
        /// Lista todas as queries de uma categoria específica
        /// </summary>
        public List<string> GetQueriesByCategory(string category)
        {
            return _sqlQueries.Keys
                .Where(key => key.StartsWith($"{category}."))
                .Select(key => key.Substring(category.Length + 1))
                .ToList();
        }
    }
}