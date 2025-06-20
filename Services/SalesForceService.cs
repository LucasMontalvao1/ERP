using API.Models.Configurations;
using API.Models.DTOs.SalesForce;
using API.Models.Responses;
using API.Services.Cache.Interfaces;
using API.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace API.Services
{
    public class SalesForceService : ISalesForceService
    {
        private readonly HttpClient _httpClient;
        private readonly SalesForceConfig _config;
        private readonly ICacheService _cacheService;
        private readonly ILogger<SalesForceService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public SalesForceService(
            HttpClient httpClient,
            IOptions<SalesForceConfig> config,
            ICacheService cacheService,
            ILogger<SalesForceService> logger)
        {
            _httpClient = httpClient;
            _config = config.Value;
            _cacheService = cacheService;
            _logger = logger;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            ConfigureHttpClient();
        }

        private void ConfigureHttpClient()
        {
            _httpClient.BaseAddress = new Uri(_config.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ERP-Montalvao/1.0");
        }

        public async Task<SalesForceServiceResult<SalesForceAuthDto>> AuthenticateAsync()
        {
            Console.WriteLine("=== SALESFORCE SERVICE - AUTHENTICATE ASYNC (API CUSTOMIZADA) ===");

            try
            {
                _logger.LogInformation("Iniciando autenticação com SalesForce");

                // Logs das configurações
                Console.WriteLine("=== CONFIGURAÇÕES ===");
                Console.WriteLine($"BaseUrl: {_config.BaseUrl}");
                Console.WriteLine($"LoginEndpoint: {_config.LoginEndpoint}");
                Console.WriteLine($"Login: {_config.Login}");
                Console.WriteLine($"Password: {(!string.IsNullOrEmpty(_config.Password) ? "***CONFIGURADO***" : "***VAZIO***")}");
                Console.WriteLine($"TimeoutSeconds: {_config.TimeoutSeconds}");
                Console.WriteLine($"MaxRetries: {_config.MaxRetries}");
                Console.WriteLine($"RetryDelaySeconds: {_config.RetryDelaySeconds}");
                Console.WriteLine($"TokenCacheMinutes: {_config.TokenCacheMinutes}");

                // Verificar cache primeiro
                var cacheKey = "salesforce:auth_token";
                var cachedToken = await _cacheService.GetAsync<SalesForceAuthDto>(cacheKey);
                if (cachedToken != null && cachedToken.ExpiresAt > DateTime.UtcNow.AddMinutes(5))
                {
                    Console.WriteLine("=== TOKEN VÁLIDO EM CACHE ===");
                    Console.WriteLine($"Token expira em: {cachedToken.ExpiresAt}");
                    _logger.LogDebug("Token válido encontrado em cache");
                    return SalesForceServiceResult<SalesForceAuthDto>.CreateSuccess(cachedToken, "Token obtido do cache");
                }

                // Construir URL completa
                var loginUrl = $"{_config.BaseUrl.TrimEnd('/')}{_config.LoginEndpoint}";
                Console.WriteLine($"=== URL DE AUTENTICAÇÃO ===");
                Console.WriteLine($"Login URL: {loginUrl}");

                // Preparar dados do POST 
                var requestData = new
                {
                    login = _config.Login,
                    password = _config.Password
                };

                Console.WriteLine("=== DADOS DA REQUISIÇÃO ===");
                Console.WriteLine($"login: {requestData.login}");
                Console.WriteLine($"password: ***OCULTO***");

                // Serializar para JSON
                var jsonContent = JsonSerializer.Serialize(requestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                Console.WriteLine("=== HEADERS DA REQUISIÇÃO ===");
                Console.WriteLine($"Content-Type: {content.Headers.ContentType}");
                Console.WriteLine($"Request Body JSON: {jsonContent.Replace(_config.Password, "***PASSWORD***")}");

                Console.WriteLine("=== ENVIANDO REQUISIÇÃO ===");
                Console.WriteLine($"Timestamp: {DateTime.UtcNow:HH:mm:ss.fff}");

                var startTime = DateTime.UtcNow;
                var response = await _httpClient.PostAsync(loginUrl, content);
                var responseTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

                Console.WriteLine("=== RESPOSTA RECEBIDA ===");
                Console.WriteLine($"Status Code: {response.StatusCode} ({(int)response.StatusCode})");
                Console.WriteLine($"Status Description: {response.ReasonPhrase}");
                Console.WriteLine($"Response Time: {responseTime}ms");
                Console.WriteLine($"Timestamp: {DateTime.UtcNow:HH:mm:ss.fff}");

                Console.WriteLine("=== HEADERS DA RESPOSTA ===");
                foreach (var header in response.Headers)
                {
                    Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
                }

                if (response.Content?.Headers != null)
                {
                    Console.WriteLine("=== CONTENT HEADERS ===");
                    foreach (var header in response.Content.Headers)
                    {
                        Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
                    }
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"=== CORPO DA RESPOSTA ===");
                Console.WriteLine($"Response Length: {responseContent?.Length ?? 0} characters");
                Console.WriteLine($"Response Content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("=== PARSING RESPOSTA DE SUCESSO ===");

                    var customResponse = JsonSerializer.Deserialize<CustomSalesForceAuthResponse>(responseContent, _jsonOptions);

                    Console.WriteLine($"Success: {customResponse?.Success}");
                    Console.WriteLine($"Data Criacao: {customResponse?.Data_Criacao}");
                    Console.WriteLine($"Data Expiracao: {customResponse?.Data_Expiracao}");
                    Console.WriteLine($"Token Access: {customResponse?.Token_De_Acesso?.Substring(0, Math.Min(50, customResponse.Token_De_Acesso.Length))}...");
                    Console.WriteLine($"Resposta: {customResponse?.Resposta}");

                    if (customResponse?.Success == true && !string.IsNullOrEmpty(customResponse.Token_De_Acesso))
                    {
                        DateTime expiresAt = DateTime.UtcNow.AddMinutes(_config.TokenCacheMinutes); // Default baseado na config
                        if (DateTime.TryParse(customResponse.Data_Expiracao, out var parsedExpiration))
                        {
                            expiresAt = parsedExpiration;
                            Console.WriteLine($"Data de expiração parseada: {expiresAt}");
                        }

                        var authDto = new SalesForceAuthDto
                        {
                            AccessToken = customResponse.Token_De_Acesso,
                            InstanceUrl = _config.BaseUrl,
                            ExpiresAt = expiresAt,
                            TokenType = "Bearer",
                            ExtraData = new Dictionary<string, object>
                            {
                                ["data_criacao"] = customResponse.Data_Criacao ?? "",
                                ["resposta"] = customResponse.Resposta ?? ""
                            }
                        };

                        var cacheExpiration = TimeSpan.FromMinutes(_config.TokenCacheMinutes - 5); // 5 min antes de expirar
                        await _cacheService.SetAsync(cacheKey, authDto, cacheExpiration);
                        Console.WriteLine($"Token armazenado no cache por {cacheExpiration.TotalMinutes} minutos");

                        _logger.LogInformation("Autenticação com SalesForce realizada com sucesso. Tempo: {ResponseTime}ms", responseTime);

                        Console.WriteLine("=== RETORNANDO SUCESSO ===");
                        return SalesForceServiceResult<SalesForceAuthDto>.CreateSuccess(authDto, "Autenticação realizada com sucesso");
                    }
                    else
                    {
                        Console.WriteLine("=== RESPOSTA SEM SUCESSO ===");
                        var errorMsg = $"API retornou success=false: {customResponse?.Resposta}";
                        _logger.LogWarning("Falha na autenticação com SalesForce: {ErrorMsg}", errorMsg);
                        return SalesForceServiceResult<SalesForceAuthDto>.CreateFailure(errorMsg);
                    }
                }
                else
                {
                    Console.WriteLine("=== RESPOSTA COM ERRO HTTP ===");

                    if (!string.IsNullOrEmpty(responseContent))
                    {
                        try
                        {
                            var errorResponse = JsonSerializer.Deserialize<dynamic>(responseContent);
                            Console.WriteLine($"Erro da API: {errorResponse}");
                        }
                        catch (Exception parseEx)
                        {
                            Console.WriteLine($"Erro ao parsear resposta de erro: {parseEx.Message}");
                        }
                    }

                    _logger.LogWarning("Falha na autenticação com SalesForce. Status: {Status}, Response: {Response}",
                        response.StatusCode, responseContent);

                    return SalesForceServiceResult<SalesForceAuthDto>.CreateFailure($"Falha na autenticação: {response.StatusCode}");
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine("=== EXCEÇÃO HTTP ===");
                Console.WriteLine($"HttpRequestException: {httpEx.Message}");
                Console.WriteLine($"Inner Exception: {httpEx.InnerException?.Message}");
                Console.WriteLine($"Stack Trace: {httpEx.StackTrace}");

                _logger.LogError(httpEx, "Erro HTTP durante autenticação SalesForce");
                return SalesForceServiceResult<SalesForceAuthDto>.CreateFailure($"Erro de conexão: {httpEx.Message}");
            }
            catch (TaskCanceledException timeoutEx)
            {
                Console.WriteLine("=== TIMEOUT ===");
                Console.WriteLine($"TaskCanceledException: {timeoutEx.Message}");
                Console.WriteLine($"Is Timeout: {timeoutEx.CancellationToken.IsCancellationRequested}");

                _logger.LogError(timeoutEx, "Timeout durante autenticação SalesForce");
                return SalesForceServiceResult<SalesForceAuthDto>.CreateFailure("Timeout na autenticação");
            }
            catch (Exception ex)
            {
                Console.WriteLine("=== EXCEÇÃO GERAL ===");
                Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"Exception Message: {ex.Message}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                _logger.LogError(ex, "Erro durante autenticação SalesForce");
                return SalesForceServiceResult<SalesForceAuthDto>.CreateFailure($"Erro interno: {ex.Message}");
            }
        }

        public async Task<SalesForceHealthDto> CheckHealthAsync()
        {
            var healthDto = new SalesForceHealthDto
            {
                CheckedAt = DateTime.UtcNow
            };

            try
            {
                var startTime = DateTime.UtcNow;

                var authResult = await AuthenticateAsync();
                healthDto.ResponseTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

                if (authResult.Success)
                {
                    healthDto.IsHealthy = true;
                    healthDto.Status = "Healthy";
                    healthDto.Version = "v1";
                }
                else
                {
                    healthDto.IsHealthy = false;
                    healthDto.Status = "Unhealthy";
                    healthDto.Issues.Add($"Authentication failed: {authResult.Message}");
                }
            }
            catch (Exception ex)
            {
                healthDto.IsHealthy = false;
                healthDto.Status = "Unhealthy";
                healthDto.Issues.Add($"Health check failed: {ex.Message}");
                _logger.LogError(ex, "Erro ao verificar saúde do SalesForce");
            }

            return healthDto;
        }

        public async Task<string> GetBaseUrlAsync()
        {
            return _config.BaseUrl;
        }

        public async Task ClearAuthCacheAsync()
        {
            await _cacheService.RemoveAsync("salesforce:auth_token");
            _logger.LogInformation("Cache de autenticação SalesForce limpo");
        }

        public async Task<bool> ValidateTokenAsync()
        {
            try
            {
                var cachedToken = await _cacheService.GetAsync<SalesForceAuthDto>("salesforce:auth_token");
                return cachedToken != null && cachedToken.ExpiresAt > DateTime.UtcNow.AddMinutes(5);
            }
            catch
            {
                return false;
            }
        }

        public async Task<ApiResponse<SalesForceSyncResult>> SendAtividadeAsync(Models.DTOs.Atividade.AtividadeSyncDto atividade, string correlationId)
        {
            try
            {
                _logger.LogInformation("Enviando atividade {CodAtiv} para SalesForce. CorrelationId: {CorrelationId}",
                    atividade.CodAtiv, correlationId);

                var authResult = await EnsureAuthenticatedAsync();
                if (!authResult.Success)
                {
                    return ApiResponse<SalesForceSyncResult>.ErrorResult($"Falha na autenticação: {authResult.Message}");
                }

                var salesForceData = new SalesForceAtividadeDto
                {
                    CodAtiv = atividade.CodAtiv,
                    Ramo = atividade.Ramo,
                    PercDesc = atividade.PercDesc,
                    CalculaSt = atividade.CalculaSt,
                    DataCriacao = atividade.DataCriacao,
                    DataAtualizacao = atividade.DataAtualizacao,
                    CorrelationId = correlationId
                };

                var result = await ExecuteRequestAsync(_config.AtividadesEndpoint, HttpMethod.Post, salesForceData, correlationId);

                return ApiResponse<SalesForceSyncResult>.SuccessResult(new SalesForceSyncResult
                {
                    Success = result.Data?.Success ?? false,
                    ExternalId = result.Data?.Data?.ToString(),
                    Message = result.Data?.Message ?? "Enviado com sucesso",
                    ResponseTime = 0,
                    HttpStatusCode = result.Data?.StatusCode ?? 200,
                    ProcessedAt = DateTime.UtcNow,
                    CorrelationId = correlationId,
                    ResponseData = result.Data?.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar atividade {CodAtiv} para SalesForce", atividade.CodAtiv);
                return ApiResponse<SalesForceSyncResult>.ErrorResult("Erro interno ao enviar atividade");
            }
        }

        /// <summary>
        /// Criar nova atividade na API externa
        /// </summary>
        public async Task<SalesForceResult<AtividadeIntegrationResponseDto>> CreateAtividadeAsync(
            Models.DTOs.Atividade.AtividadeSyncDto atividade, string correlationId)
        {
            try
            {
                _logger.LogInformation("🆕 CRIANDO nova atividade {CodAtiv} na API externa. CorrelationId: {CorrelationId}",
                    atividade.CodAtiv, correlationId);

                var authResult = await EnsureAuthenticatedAsync();
                if (!authResult.Success)
                {
                    _logger.LogError("❌ Falha na autenticação: {AuthError}", authResult.Message);
                    return SalesForceResult<AtividadeIntegrationResponseDto>.CreateError($"Falha na autenticação: {authResult.Message}");
                }

                var requestData = new[]
                {
            new ExternalApiAtividadeRequest
            {
                CodAtiv = atividade.CodAtiv,
                PercDesc = atividade.PercDesc,
                Hash = Guid.NewGuid().ToString("N")[..8], 
                Ramo = atividade.Ramo,
                CalculaSt = atividade.CalculaSt  
            }
        };

                _logger.LogInformation("=== DADOS PREPARADOS PARA CRIAÇÃO ===");
                _logger.LogInformation("- CodAtiv: {CodAtiv}", atividade.CodAtiv);
                _logger.LogInformation("- Ramo: {Ramo}", atividade.Ramo);
                _logger.LogInformation("- PercDesc: {PercDesc}", atividade.PercDesc);
                _logger.LogInformation("- CalculaSt: {CalculaSt}", atividade.CalculaSt);

                var startTime = DateTime.UtcNow;
                var result = await ExecuteAtividadesRequestAsync(_config.AtividadesEndpoint, HttpMethod.Post, requestData, correlationId);
                var responseTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

                if (!result.Success)
                {
                    _logger.LogError("❌ Falha na requisição de criação: {Error}", result.Message);
                    return SalesForceResult<AtividadeIntegrationResponseDto>.CreateError(result.Message);
                }

                var externalResponse = ProcessExternalApiResponse(result.Data, correlationId);

                if (externalResponse.Success && externalResponse.Data != null)
                {
                    var hasSuccess = externalResponse.Data.Success.Any();

                    if (hasSuccess)
                    {
                        var firstSuccess = externalResponse.Data.Success.First();
                        var integrationResponse = new AtividadeIntegrationResponseDto
                        {
                            ExternalId = firstSuccess.Chave.CodAtiv,
                            Success = true,
                            Message = "Atividade criada com sucesso",
                            ProcessedAt = DateTime.UtcNow,
                            ResponseTime = responseTime
                        };

                        _logger.LogInformation("✅ Atividade {CodAtiv} criada com sucesso. ExternalId: {ExternalId}",
                            atividade.CodAtiv, integrationResponse.ExternalId);

                        return SalesForceResult<AtividadeIntegrationResponseDto>.CreateSuccess(integrationResponse);
                    }
                    else
                    {
                        var errors = string.Join("; ", externalResponse.Data.Errors.Select(e => e.Message));
                        _logger.LogError("❌ API externa retornou erros: {Errors}", errors);
                        return SalesForceResult<AtividadeIntegrationResponseDto>.CreateError($"Erros da API: {errors}");
                    }
                }
                else
                {
                    _logger.LogError("❌ Falha no processamento da resposta: {Error}", externalResponse.Message);
                    return SalesForceResult<AtividadeIntegrationResponseDto>.CreateError(externalResponse.Message ?? "Erro no processamento");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Exceção ao criar atividade {CodAtiv}", atividade.CodAtiv);
                return SalesForceResult<AtividadeIntegrationResponseDto>.CreateError($"Erro interno: {ex.Message}");
            }
        }

        /// <summary>
        /// Atualizar atividade existente na API externa
        /// </summary>
        public async Task<SalesForceResult<AtividadeIntegrationResponseDto>> UpdateAtividadeAsync(
            Models.DTOs.Atividade.AtividadeSyncDto atividade, string correlationId)
        {
            try
            {
                _logger.LogInformation("🔄 ATUALIZANDO atividade {CodAtiv} na API externa. CorrelationId: {CorrelationId}",
                    atividade.CodAtiv, correlationId);

                var authResult = await EnsureAuthenticatedAsync();
                if (!authResult.Success)
                {
                    _logger.LogError("❌ Falha na autenticação: {AuthError}", authResult.Message);
                    return SalesForceResult<AtividadeIntegrationResponseDto>.CreateError($"Falha na autenticação: {authResult.Message}");
                }

                var requestData = new[]
                {
            new ExternalApiAtividadeRequest
            {
                CodAtiv = atividade.CodAtiv,
                PercDesc = atividade.PercDesc,
                Hash = Guid.NewGuid().ToString("N")[..8], 
                Ramo = atividade.Ramo,
                CalculaSt = atividade.CalculaSt
            }
        };

                _logger.LogInformation("=== DADOS PREPARADOS PARA ATUALIZAÇÃO ===");
                _logger.LogInformation("- CodAtiv: {CodAtiv}", atividade.CodAtiv);
                _logger.LogInformation("- Ramo: {Ramo}", atividade.Ramo);
                _logger.LogInformation("- PercDesc: {PercDesc}", atividade.PercDesc);
                _logger.LogInformation("- CalculaSt: {CalculaSt}", atividade.CalculaSt);

                var startTime = DateTime.UtcNow;
                var result = await ExecuteAtividadesRequestAsync(_config.AtividadesEndpoint, HttpMethod.Post, requestData, correlationId);
                var responseTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

                if (!result.Success)
                {
                    _logger.LogError("❌ Falha na requisição de atualização: {Error}", result.Message);
                    return SalesForceResult<AtividadeIntegrationResponseDto>.CreateError(result.Message);
                }

                var externalResponse = ProcessExternalApiResponse(result.Data, correlationId);

                if (externalResponse.Success && externalResponse.Data != null)
                {
                    var hasSuccess = externalResponse.Data.Success.Any();

                    if (hasSuccess)
                    {
                        var firstSuccess = externalResponse.Data.Success.First();
                        var integrationResponse = new AtividadeIntegrationResponseDto
                        {
                            ExternalId = firstSuccess.Chave.CodAtiv,
                            Success = true,
                            Message = "Atividade atualizada com sucesso",
                            ProcessedAt = DateTime.UtcNow,
                            ResponseTime = responseTime
                        };

                        _logger.LogInformation("✅ Atividade {CodAtiv} atualizada com sucesso. ExternalId: {ExternalId}",
                            atividade.CodAtiv, integrationResponse.ExternalId);

                        return SalesForceResult<AtividadeIntegrationResponseDto>.CreateSuccess(integrationResponse);
                    }
                    else
                    {
                        var errors = string.Join("; ", externalResponse.Data.Errors.Select(e => e.Message));
                        _logger.LogError("❌ API externa retornou erros: {Errors}", errors);
                        return SalesForceResult<AtividadeIntegrationResponseDto>.CreateError($"Erros da API: {errors}");
                    }
                }
                else
                {
                    _logger.LogError("❌ Falha no processamento da resposta: {Error}", externalResponse.Message);
                    return SalesForceResult<AtividadeIntegrationResponseDto>.CreateError(externalResponse.Message ?? "Erro no processamento");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Exceção ao atualizar atividade {CodAtiv}", atividade.CodAtiv);
                return SalesForceResult<AtividadeIntegrationResponseDto>.CreateError($"Erro interno: {ex.Message}");
            }
        }

        public async Task<ApiResponse<SalesForceSyncResult>> DeleteAtividadeAsync(string codAtiv, string correlationId)
        {
            try
            {
                _logger.LogInformation("Deletando atividade {CodAtiv} no SalesForce. CorrelationId: {CorrelationId}",
                    codAtiv, correlationId);

                var authResult = await EnsureAuthenticatedAsync();
                if (!authResult.Success)
                {
                    return ApiResponse<SalesForceSyncResult>.ErrorResult($"Falha na autenticação: {authResult.Message}");
                }

                var endpoint = $"{_config.AtividadesEndpoint}/{codAtiv}";
                var result = await ExecuteRequestAsync(endpoint, HttpMethod.Delete, null, correlationId);

                return ApiResponse<SalesForceSyncResult>.SuccessResult(new SalesForceSyncResult
                {
                    Success = result.Data?.Success ?? false,
                    Message = result.Data?.Message ?? "Deletado com sucesso",
                    ResponseTime = 0,
                    HttpStatusCode = result.Data?.StatusCode ?? 200,
                    ProcessedAt = DateTime.UtcNow,
                    CorrelationId = correlationId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar atividade {CodAtiv} no SalesForce", codAtiv);
                return ApiResponse<SalesForceSyncResult>.ErrorResult("Erro interno ao deletar atividade");
            }
        }

        public async Task<ApiResponse<List<SalesForceSyncResult>>> SendBatchAsync(List<Models.DTOs.Atividade.AtividadeSyncDto> atividades, string correlationId)
        {
            var results = new List<SalesForceSyncResult>();

            _logger.LogInformation("Enviando lote de {Count} atividades para SalesForce. CorrelationId: {CorrelationId}",
                atividades.Count, correlationId);

            foreach (var atividade in atividades)
            {
                try
                {
                    var result = await SendAtividadeAsync(atividade, correlationId);
                    if (result.Data != null)
                    {
                        results.Add(result.Data);
                    }

                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar atividade {CodAtiv} no lote", atividade.CodAtiv);
                    results.Add(new SalesForceSyncResult
                    {
                        Success = false,
                        Message = $"Erro interno: {ex.Message}",
                        CorrelationId = correlationId,
                        ProcessedAt = DateTime.UtcNow
                    });
                }
            }

            return ApiResponse<List<SalesForceSyncResult>>.SuccessResult(results);
        }

        public async Task<ApiResponse<SalesForceApiResponse>> ExecuteRequestAsync(string endpoint, HttpMethod method, object? data = null, string? correlationId = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            _logger.LogInformation("=== INICIANDO REQUISIÇÃO PARA API EXTERNA ===");
            _logger.LogInformation("Endpoint: {Endpoint}", endpoint);
            _logger.LogInformation("Método HTTP: {Method}", method.Method);
            _logger.LogInformation("CorrelationId: {CorrelationId}", correlationId ?? "N/A");
            _logger.LogInformation("Timestamp: {Timestamp}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            try
            {
                var fullUrl = $"{_config.BaseUrl.TrimEnd('/')}{endpoint}";
                _logger.LogInformation("=== CONFIGURAÇÕES DA REQUISIÇÃO ===");
                _logger.LogInformation("URL Completa: {FullUrl}", fullUrl);
                _logger.LogInformation("BaseUrl Config: {BaseUrl}", _config.BaseUrl);
                _logger.LogInformation("Timeout: {Timeout}s", _config.TimeoutSeconds);

                var request = new HttpRequestMessage(method, fullUrl);

                _logger.LogInformation("=== PREPARANDO HEADERS ===");

                if (!string.IsNullOrEmpty(correlationId))
                {
                    request.Headers.Add("X-Correlation-ID", correlationId);
                    _logger.LogInformation("Header adicionado: X-Correlation-ID = {CorrelationId}", correlationId);
                }

                _logger.LogInformation("=== VERIFICANDO AUTENTICAÇÃO ===");
                var cachedToken = await _cacheService.GetAsync<SalesForceAuthDto>("salesforce:auth_token");

                if (cachedToken != null)
                {
                    _logger.LogInformation("Token encontrado no cache:");
                    _logger.LogInformation("- Token válido até: {TokenExpiry}", cachedToken.ExpiresAt);
                    _logger.LogInformation("- Tempo restante: {TimeRemaining} minutos",
                                          (cachedToken.ExpiresAt - DateTime.UtcNow).TotalMinutes);
                    _logger.LogInformation("- Preview do token: {TokenPreview}...",
                                          cachedToken.AccessToken.Substring(0, Math.Min(20, cachedToken.AccessToken.Length)));

                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cachedToken.AccessToken);
                    _logger.LogInformation("✅ Header Authorization adicionado");
                }
                else
                {
                    _logger.LogWarning("⚠️ NENHUM TOKEN ENCONTRADO NO CACHE!");
                    _logger.LogWarning("A requisição pode falhar por falta de autenticação");
                }

                string? requestBody = null;
                if (data != null && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
                {
                    _logger.LogInformation("=== PREPARANDO BODY DA REQUISIÇÃO ===");
                    _logger.LogInformation("Dados recebidos para serialização: {DataType}", data.GetType().Name);

                    requestBody = JsonSerializer.Serialize(data, _jsonOptions);
                    request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                    _logger.LogInformation("Body serializado:");
                    _logger.LogInformation("- Content-Type: application/json");
                    _logger.LogInformation("- Encoding: UTF-8");
                    _logger.LogInformation("- Tamanho: {Size} characters", requestBody.Length);
                    _logger.LogInformation("- Conteúdo: {RequestBody}", requestBody);
                }
                else
                {
                    _logger.LogInformation("=== SEM BODY ===");
                    _logger.LogInformation("Método {Method} sem dados para envio", method.Method);
                }

                _logger.LogInformation("=== HEADERS FINAIS DA REQUISIÇÃO ===");
                foreach (var header in request.Headers)
                {
                    var headerValue = header.Key == "Authorization"
                        ? $"Bearer {header.Value.First().Substring(0, Math.Min(20, header.Value.First().Length))}..."
                        : string.Join(", ", header.Value);
                    _logger.LogInformation("- {HeaderName}: {HeaderValue}", header.Key, headerValue);
                }

                if (request.Content?.Headers != null)
                {
                    _logger.LogInformation("=== CONTENT HEADERS ===");
                    foreach (var header in request.Content.Headers)
                    {
                        _logger.LogInformation("- {HeaderName}: {HeaderValue}", header.Key, string.Join(", ", header.Value));
                    }
                }

                _logger.LogInformation("=== ENVIANDO REQUISIÇÃO HTTP ===");
                _logger.LogInformation("Timestamp de envio: {SendTimestamp}", DateTime.UtcNow.ToString("HH:mm:ss.fff"));

                var response = await _httpClient.SendAsync(request);

                stopwatch.Stop();
                var responseTime = (int)stopwatch.ElapsedMilliseconds;

                _logger.LogInformation("=== RESPOSTA RECEBIDA ===");
                _logger.LogInformation("Status Code: {StatusCode} ({StatusCodeNumber})", response.StatusCode, (int)response.StatusCode);
                _logger.LogInformation("Reason Phrase: {ReasonPhrase}", response.ReasonPhrase);
                _logger.LogInformation("Tempo de resposta: {ResponseTime}ms", responseTime);
                _logger.LogInformation("Timestamp de recebimento: {ReceiveTimestamp}", DateTime.UtcNow.ToString("HH:mm:ss.fff"));

                _logger.LogInformation("=== HEADERS DA RESPOSTA ===");
                foreach (var header in response.Headers)
                {
                    _logger.LogInformation("- {HeaderName}: {HeaderValue}", header.Key, string.Join(", ", header.Value));
                }

                if (response.Content?.Headers != null)
                {
                    _logger.LogInformation("=== CONTENT HEADERS DA RESPOSTA ===");
                    foreach (var header in response.Content.Headers)
                    {
                        _logger.LogInformation("- {HeaderName}: {HeaderValue}", header.Key, string.Join(", ", header.Value));
                    }
                }

                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("=== CONTEÚDO DA RESPOSTA ===");
                _logger.LogInformation("Tamanho da resposta: {ResponseSize} characters", responseContent?.Length ?? 0);
                _logger.LogInformation("Conteúdo bruto: {ResponseContent}", responseContent);

                var apiResponse = new SalesForceApiResponse
                {
                    Success = response.IsSuccessStatusCode,
                    StatusCode = (int)response.StatusCode,
                    Message = response.IsSuccessStatusCode ? "Requisição executada com sucesso" : $"Falha na requisição: {response.StatusCode}",
                    Timestamp = DateTime.UtcNow
                };

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("=== PROCESSANDO RESPOSTA DE SUCESSO ===");

                    if (!string.IsNullOrEmpty(responseContent))
                    {
                        try
                        {
                            var parsedResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                            apiResponse.Data = parsedResponse;

                            _logger.LogInformation("✅ Resposta deserializada com sucesso");
                            _logger.LogInformation("Tipo da resposta: {ResponseType}", parsedResponse.ValueKind);

                            if (parsedResponse.ValueKind == JsonValueKind.Object)
                            {
                                if (parsedResponse.TryGetProperty("success", out var successProp))
                                {
                                    var successValue = successProp.ValueKind == JsonValueKind.Array && successProp.GetArrayLength() > 0;
                                    _logger.LogInformation("Propriedade 'success' encontrada: {SuccessArray}", successValue);

                                    apiResponse.Success = successValue;
                                }
                            }
                        }
                        catch (JsonException jsonEx)
                        {
                            _logger.LogWarning("⚠️ Falha ao deserializar JSON: {JsonError}", jsonEx.Message);
                            _logger.LogWarning("Usando conteúdo como string simples");
                            apiResponse.Data = responseContent;
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Resposta vazia (sem conteúdo)");
                    }
                }
                else
                {
                    _logger.LogError("=== PROCESSANDO RESPOSTA DE ERRO ===");
                    _logger.LogError("Status HTTP: {StatusCode}", response.StatusCode);
                    _logger.LogError("Reason: {ReasonPhrase}", response.ReasonPhrase);

                    var errorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";

                    if (!string.IsNullOrEmpty(responseContent))
                    {
                        _logger.LogError("Detalhes do erro: {ErrorContent}", responseContent);
                        errorMessage += $" - {responseContent}";

                        try
                        {
                            var errorJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
                            if (errorJson.TryGetProperty("message", out var messageProp))
                            {
                                apiResponse.Errors.Add(messageProp.GetString() ?? "Erro desconhecido");
                            }
                            if (errorJson.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var error in errorsProp.EnumerateArray())
                                {
                                    apiResponse.Errors.Add(error.GetString() ?? "Erro desconhecido");
                                }
                            }
                        }
                        catch (JsonException)
                        {
                            _logger.LogWarning("Resposta de erro não é JSON válido");
                            apiResponse.Errors.Add(responseContent);
                        }
                    }

                    apiResponse.Errors.Add(errorMessage);
                }

                _logger.LogInformation("=== RESULTADO FINAL ===");
                _logger.LogInformation("Success: {Success}", apiResponse.Success);
                _logger.LogInformation("Status Code: {StatusCode}", apiResponse.StatusCode);
                _logger.LogInformation("Message: {Message}", apiResponse.Message);
                _logger.LogInformation("Timestamp: {Timestamp}", apiResponse.Timestamp);
                _logger.LogInformation("Errors Count: {ErrorsCount}", apiResponse.Errors.Count);

                if (apiResponse.Errors.Any())
                {
                    _logger.LogInformation("Errors:");
                    for (int i = 0; i < apiResponse.Errors.Count; i++)
                    {
                        _logger.LogInformation("  {Index}. {Error}", i + 1, apiResponse.Errors[i]);
                    }
                }

                _logger.LogInformation("=== FIM DA REQUISIÇÃO PARA API EXTERNA ===");

                return ApiResponse<SalesForceApiResponse>.SuccessResult(apiResponse);
            }
            catch (HttpRequestException httpEx)
            {
                stopwatch.Stop();

                _logger.LogError("💥 EXCEÇÃO HTTP:");
                _logger.LogError("- Tipo: HttpRequestException");
                _logger.LogError("- Mensagem: {Message}", httpEx.Message);
                _logger.LogError("- Inner Exception: {InnerException}", httpEx.InnerException?.Message);
                _logger.LogError("- Tempo até exceção: {ElapsedTime}ms", stopwatch.ElapsedMilliseconds);
                _logger.LogError("- CorrelationId: {CorrelationId}", correlationId);

                return ApiResponse<SalesForceApiResponse>.ErrorResult($"Erro de conexão HTTP: {httpEx.Message}");
            }
            catch (TaskCanceledException timeoutEx)
            {
                stopwatch.Stop();

                _logger.LogError("⏰ TIMEOUT:");
                _logger.LogError("- Tipo: TaskCanceledException");
                _logger.LogError("- Mensagem: {Message}", timeoutEx.Message);
                _logger.LogError("- É Timeout: {IsTimeout}", timeoutEx.CancellationToken.IsCancellationRequested);
                _logger.LogError("- Tempo até timeout: {ElapsedTime}ms", stopwatch.ElapsedMilliseconds);
                _logger.LogError("- Timeout configurado: {ConfiguredTimeout}s", _config.TimeoutSeconds);
                _logger.LogError("- CorrelationId: {CorrelationId}", correlationId);

                return ApiResponse<SalesForceApiResponse>.ErrorResult($"Timeout na requisição ({_config.TimeoutSeconds}s)");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError("💥 EXCEÇÃO GERAL:");
                _logger.LogError("- Tipo: {ExceptionType}", ex.GetType().Name);
                _logger.LogError("- Mensagem: {Message}", ex.Message);
                _logger.LogError("- Inner Exception: {InnerException}", ex.InnerException?.Message);
                _logger.LogError("- Stack Trace: {StackTrace}", ex.StackTrace);
                _logger.LogError("- Tempo até exceção: {ElapsedTime}ms", stopwatch.ElapsedMilliseconds);
                _logger.LogError("- CorrelationId: {CorrelationId}", correlationId);

                return ApiResponse<SalesForceApiResponse>.ErrorResult($"Erro interno na requisição: {ex.Message}");
            }
        }

        

        /// <summary>
        /// Enviar dados de teste para a API externa no formato correto
        /// </summary>
        public async Task<ApiResponse<ExternalApiAtividadeResponse>> SendTestDataAsync(
    SalesForceTestDataDto testData, string correlationId)
        {
            _logger.LogInformation("=== INICIANDO ENVIO DE DADOS DE TESTE ===");
            _logger.LogInformation("CorrelationId: {CorrelationId}", correlationId);

            try
            {
                var authResult = await EnsureAuthenticatedAsync();
                if (!authResult.Success)
                {
                    _logger.LogError("❌ Falha na autenticação: {AuthError}", authResult.Message);
                    return ApiResponse<ExternalApiAtividadeResponse>.ErrorResult($"Falha na autenticação: {authResult.Message}");
                }

                var testCodAtiv = $"TEST_{DateTime.UtcNow:yyyyMMddHHmmss}";
                var testHash = Guid.NewGuid().ToString("N")[..8];

                var requestData = new[]
                {
            new ExternalApiAtividadeRequest
            {
                CodAtiv = testCodAtiv,
                PercDesc = testData.PercDesc,
                Hash = testHash,
                Ramo = testData.Ramo,
                CalculaSt = testData.CalculaSt
            }
        };

                _logger.LogInformation("=== DADOS PREPARADOS ===");
                _logger.LogInformation("- CodAtiv: {CodAtiv}", testCodAtiv);
                _logger.LogInformation("- Ramo: {Ramo}", testData.Ramo);
                _logger.LogInformation("- PercDesc: {PercDesc}", testData.PercDesc);
                _logger.LogInformation("- Hash: {Hash}", testHash);

                var endpoint = _config.AtividadesEndpoint; 
                _logger.LogInformation("Endpoint: {Endpoint}", endpoint);

                var result = await ExecuteAtividadesRequestAsync(endpoint, HttpMethod.Post, requestData, correlationId);

                if (!result.Success)
                {
                    _logger.LogError("❌ Falha na requisição: {Error}", result.Message);
                    return ApiResponse<ExternalApiAtividadeResponse>.ErrorResult(result.Message);
                }

                return ProcessExternalApiResponse(result.Data, correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 EXCEÇÃO durante envio de dados de teste");
                return ApiResponse<ExternalApiAtividadeResponse>.ErrorResult($"Erro interno: {ex.Message}");
            }
        }

        /// <summary>
        /// Executa requisição específica para endpoint de Atividades
        /// </summary>
        private async Task<ApiResponse<SalesForceApiResponse>> ExecuteAtividadesRequestAsync(
            string endpoint, HttpMethod method, object? data = null, string? correlationId = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            _logger.LogInformation("=== EXECUTANDO REQUISIÇÃO DE ATIVIDADES ===");
            _logger.LogInformation("Endpoint: {Endpoint}", endpoint);
            _logger.LogInformation("Method: {Method}", method.Method);
            _logger.LogInformation("CorrelationId: {CorrelationId}", correlationId ?? "N/A");

            try
            {
                var fullUrl = $"{_config.BaseUrl.TrimEnd('/')}{endpoint}";
                _logger.LogInformation("URL Completa: {FullUrl}", fullUrl);

                var request = new HttpRequestMessage(method, fullUrl);

                if (!string.IsNullOrEmpty(correlationId))
                {
                    request.Headers.Add("X-Correlation-ID", correlationId);
                }

                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

                var cachedToken = await _cacheService.GetAsync<SalesForceAuthDto>("salesforce:auth_token");
                if (cachedToken != null)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cachedToken.AccessToken);
                    _logger.LogInformation("✅ Authorization header adicionado");
                    _logger.LogInformation("Token preview: {TokenPreview}...",
                        cachedToken.AccessToken.Substring(0, Math.Min(20, cachedToken.AccessToken.Length)));
                }

                if (data != null && (method == HttpMethod.Post || method == HttpMethod.Put))
                {
                    var jsonData = JsonSerializer.Serialize(data, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = true
                    });

                    request.Content = new StringContent(jsonData, Encoding.UTF8, "application/json-patch+json");

                    _logger.LogInformation("=== BODY DA REQUISIÇÃO ===");
                    _logger.LogInformation("Content-Type: application/json-patch+json");
                    _logger.LogInformation("Body JSON: {JsonBody}", jsonData);
                }

                _logger.LogInformation("=== ENVIANDO REQUISIÇÃO ===");
                var response = await _httpClient.SendAsync(request);

                stopwatch.Stop();
                var responseTime = (int)stopwatch.ElapsedMilliseconds;

                _logger.LogInformation("=== RESPOSTA RECEBIDA ===");
                _logger.LogInformation("Status Code: {StatusCode}", response.StatusCode);
                _logger.LogInformation("Tempo: {ResponseTime}ms", responseTime);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Response Body: {ResponseBody}", responseContent);

                var apiResponse = new SalesForceApiResponse
                {
                    Success = response.IsSuccessStatusCode,
                    StatusCode = (int)response.StatusCode,
                    Message = response.IsSuccessStatusCode ? "Requisição executada com sucesso" : $"Falha: {response.StatusCode}",
                    Timestamp = DateTime.UtcNow
                };

                if (response.IsSuccessStatusCode && !string.IsNullOrEmpty(responseContent))
                {
                    try
                    {
                        var parsedResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        apiResponse.Data = parsedResponse;
                        _logger.LogInformation("✅ Resposta deserializada com sucesso");
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogWarning("⚠️ Falha ao deserializar JSON: {JsonError}", jsonEx.Message);
                        apiResponse.Data = responseContent;
                    }
                }
                else if (!response.IsSuccessStatusCode)
                {
                    apiResponse.Errors.Add($"HTTP {response.StatusCode}: {responseContent}");
                }

                return ApiResponse<SalesForceApiResponse>.SuccessResult(apiResponse);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "💥 EXCEÇÃO durante requisição de atividades");
                return ApiResponse<SalesForceApiResponse>.ErrorResult($"Erro interno: {ex.Message}");
            }
        }

        private ApiResponse<ExternalApiAtividadeResponse> ProcessExternalApiResponse(
    SalesForceApiResponse apiResponse, string correlationId)
        {
            _logger.LogInformation("=== PROCESSANDO RESPOSTA DA API EXTERNA ===");

            var externalResponse = new ExternalApiAtividadeResponse();

            if (apiResponse?.Data != null && apiResponse.Data is JsonElement jsonElement)
            {
                _logger.LogInformation("Tipo da resposta: {ValueKind}", jsonElement.ValueKind);

                if (jsonElement.ValueKind == JsonValueKind.Object)
                {
                    _logger.LogInformation("Resposta é um objeto");

                    if (jsonElement.TryGetProperty("success", out var successProp) &&
                        successProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var successItem in successProp.EnumerateArray())
                        {
                            if (successItem.TryGetProperty("chave", out var chaveProp) &&
                                chaveProp.TryGetProperty("codativ", out var codativProp))
                            {
                                var codativ = codativProp.GetString();
                                _logger.LogInformation("✅ CodAtiv processado: {CodAtiv}", codativ);

                                externalResponse.Success.Add(new ExternalApiSuccess
                                {
                                    Chave = new ExternalApiChave { CodAtiv = codativ ?? "" }
                                });
                            }
                        }
                    }

                    if (jsonElement.TryGetProperty("errors", out var errorsProp) &&
                        errorsProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var error in errorsProp.EnumerateArray())
                        {
                            var errorMsg = error.GetString() ?? "Erro desconhecido";
                            _logger.LogError("❌ Erro da API: {Error}", errorMsg);
                            externalResponse.Errors.Add(new ExternalApiError { Message = errorMsg });
                        }
                    }
                }

                else if (jsonElement.ValueKind == JsonValueKind.Array)
                {
                    _logger.LogInformation("Resposta é um array com {Length} elementos", jsonElement.GetArrayLength());

                    foreach (var item in jsonElement.EnumerateArray())
                    {
                        if (item.TryGetProperty("success", out var successProp) &&
                            successProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var successItem in successProp.EnumerateArray())
                            {
                                if (successItem.TryGetProperty("chave", out var chaveProp) &&
                                    chaveProp.TryGetProperty("codativ", out var codativProp))
                                {
                                    var codativ = codativProp.GetString();
                                    _logger.LogInformation("✅ CodAtiv processado: {CodAtiv}", codativ);

                                    externalResponse.Success.Add(new ExternalApiSuccess
                                    {
                                        Chave = new ExternalApiChave { CodAtiv = codativ ?? "" }
                                    });
                                }
                            }
                        }

                        if (item.TryGetProperty("errors", out var errorsProp) &&
                            errorsProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var error in errorsProp.EnumerateArray())
                            {
                                var errorMsg = error.GetString() ?? "Erro desconhecido";
                                _logger.LogError("❌ Erro da API: {Error}", errorMsg);
                                externalResponse.Errors.Add(new ExternalApiError { Message = errorMsg });
                            }
                        }
                    }
                }
            }

            var hasSuccess = externalResponse.Success.Any();
            var hasErrors = externalResponse.Errors.Any();

            _logger.LogInformation("=== RESULTADO FINAL ===");
            _logger.LogInformation("✅ Sucessos: {SuccessCount}", externalResponse.Success.Count);
            _logger.LogInformation("❌ Erros: {ErrorCount}", externalResponse.Errors.Count);

            if (hasSuccess && !hasErrors)
            {
                return ApiResponse<ExternalApiAtividadeResponse>.SuccessResult(
                    externalResponse,
                    "Dados processados com sucesso"
                );
            }
            else if (hasErrors)
            {
                var errorMsg = string.Join("; ", externalResponse.Errors.Select(e => e.Message));
                return ApiResponse<ExternalApiAtividadeResponse>.ErrorResult($"Erros da API: {errorMsg}");
            }
            else
            {
                return ApiResponse<ExternalApiAtividadeResponse>.ErrorResult("Resposta vazia da API");
            }
        }
        private async Task<SalesForceServiceResult<SalesForceAuthDto>> EnsureAuthenticatedAsync()
        {
            if (!await ValidateTokenAsync())
            {
                return await AuthenticateAsync();
            }

            var cachedToken = await _cacheService.GetAsync<SalesForceAuthDto>("salesforce:auth_token");
            return SalesForceServiceResult<SalesForceAuthDto>.CreateSuccess(cachedToken!, "Token válido do cache");
        }
    }
}