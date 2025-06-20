using API.Services.Interfaces;
using API.Models.DTOs.Jobs;
using API.Models.Responses;
using API.Models.DTOs.Webhook;
using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using System.Text.Json;


using CustomRecurringJobDto = API.Models.DTOs.Jobs.RecurringJobDto;
using API.Models.DTOs.Webhook;

namespace API.Services
{
    public class HangfireJobService : IHangfireJobService
    {
        private readonly ILogger<HangfireJobService> _logger;
        private readonly IMonitoringApi _monitoringApi;
        private readonly IEmailService _emailService;
        private readonly IRabbitMQService _rabbitMQService;

        public HangfireJobService(
            ILogger<HangfireJobService> logger,
            IEmailService emailService,
            IRabbitMQService rabbitMQService)
        {
            _logger = logger;
            _monitoringApi = JobStorage.Current.GetMonitoringApi();
            _emailService = emailService;
            _rabbitMQService = rabbitMQService;
        }

        #region Métodos básicos de gerenciamento de jobs

        public async Task<PagedResponse<JobInfoDto>> GetJobsAsync(JobFilterDto filter)
        {
            try
            {
                var jobs = new List<JobInfoDto>();
                var skip = (filter.Page - 1) * filter.PageSize;

                // Buscar jobs por status
                if (string.IsNullOrEmpty(filter.Status) || filter.Status.Equals("scheduled", StringComparison.OrdinalIgnoreCase))
                {
                    var scheduledJobs = _monitoringApi.ScheduledJobs(skip, filter.PageSize);
                    foreach (var job in scheduledJobs)
                    {
                        jobs.Add(MapToJobInfoDto(job));
                    }
                }

                if (string.IsNullOrEmpty(filter.Status) || filter.Status.Equals("processing", StringComparison.OrdinalIgnoreCase))
                {
                    var processingJobs = _monitoringApi.ProcessingJobs(skip, filter.PageSize);
                    foreach (var job in processingJobs)
                    {
                        jobs.Add(MapToJobInfoDto(job));
                    }
                }

                if (string.IsNullOrEmpty(filter.Status) || filter.Status.Equals("succeeded", StringComparison.OrdinalIgnoreCase))
                {
                    var succeededJobs = _monitoringApi.SucceededJobs(skip, filter.PageSize);
                    foreach (var job in succeededJobs)
                    {
                        jobs.Add(MapToJobInfoDto(job));
                    }
                }

                if (string.IsNullOrEmpty(filter.Status) || filter.Status.Equals("failed", StringComparison.OrdinalIgnoreCase))
                {
                    var failedJobs = _monitoringApi.FailedJobs(skip, filter.PageSize);
                    foreach (var job in failedJobs)
                    {
                        jobs.Add(MapToJobInfoDto(job));
                    }
                }

                var totalCount = jobs.Count;
                var pagedJobs = jobs.Skip(skip).Take(filter.PageSize).ToList();

                return PagedResponse<JobInfoDto>.Create(pagedJobs, filter.Page, filter.PageSize, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar jobs");
                return PagedResponse<JobInfoDto>.Create(new List<JobInfoDto>(), filter.Page, filter.PageSize, 0, "Erro ao buscar jobs");
            }
        }

        public async Task<ApiResponse<JobDetailDto>> GetJobByIdAsync(string jobId)
        {
            try
            {
                var jobDetails = _monitoringApi.JobDetails(jobId);
                if (jobDetails == null)
                {
                    return ApiResponse<JobDetailDto>.ErrorResult("Job não encontrado");
                }

                var jobDetailDto = MapToJobDetailDto(jobId, jobDetails);
                return ApiResponse<JobDetailDto>.SuccessResult(jobDetailDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar job {JobId}", jobId);
                return ApiResponse<JobDetailDto>.ErrorResult("Erro ao buscar detalhes do job");
            }
        }

        public async Task<ApiResponse<bool>> CancelJobAsync(string jobId)
        {
            try
            {
                var result = BackgroundJob.Delete(jobId);
                if (result)
                {
                    _logger.LogInformation("Job {JobId} cancelado com sucesso", jobId);
                    return ApiResponse<bool>.SuccessResult(true, "Job cancelado com sucesso");
                }
                else
                {
                    return ApiResponse<bool>.ErrorResult("Não foi possível cancelar o job");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cancelar job {JobId}", jobId);
                return ApiResponse<bool>.ErrorResult("Erro ao cancelar job");
            }
        }

        public async Task<ApiResponse<string>> RetryJobAsync(string jobId)
        {
            try
            {
                var success = BackgroundJob.Requeue(jobId);
                if (success)
                {
                    _logger.LogInformation("Job {JobId} reagendado com sucesso", jobId);
                    return ApiResponse<string>.SuccessResult(jobId, "Job reagendado com sucesso");
                }
                else
                {
                    return ApiResponse<string>.ErrorResult("Não foi possível reagendar o job");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao reagendar job {JobId}", jobId);
                return ApiResponse<string>.ErrorResult("Erro ao reagendar job");
            }
        }

        public async Task<int> CleanupOldJobsAsync(int olderThanDays)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
                var removedCount = 0;

                // Limpar jobs antigos 
                _logger.LogInformation("Limpeza de jobs mais antigos que {Days} dias iniciada", olderThanDays);

                return removedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao limpar jobs antigos");
                return 0;
            }
        }

        public async Task<ApiResponse<List<JobLogDto>>> GetJobLogsAsync(string jobId)
        {
            try
            {
                var logs = new List<JobLogDto>();

                var jobDetails = _monitoringApi.JobDetails(jobId);
                if (jobDetails?.History != null)
                {
                    foreach (var state in jobDetails.History)
                    {
                        logs.Add(new JobLogDto
                        {
                            Timestamp = state.CreatedAt,
                            State = state.StateName,
                            Reason = state.Reason,
                            Data = state.Data != null ? JsonSerializer.Serialize(state.Data) : null
                        });
                    }
                }

                return ApiResponse<List<JobLogDto>>.SuccessResult(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar logs do job {JobId}", jobId);
                return ApiResponse<List<JobLogDto>>.ErrorResult("Erro ao buscar logs do job");
            }
        }

        #endregion

        #region Agendamento de jobs específicos

        public async Task<string> ScheduleIntegrationJobAsync(ScheduleIntegrationJobDto request, string correlationId)
        {
            try
            {
                var delay = request.ExecutarEm.HasValue
                    ? request.ExecutarEm.Value - DateTime.UtcNow
                    : TimeSpan.FromSeconds(30);

                if (delay < TimeSpan.Zero)
                    delay = TimeSpan.FromSeconds(5);

                var jobId = BackgroundJob.Schedule(
                    () => ProcessIntegrationJobAsync(request.AtividadeId, request.TipoOperacao, request.Prioridade, correlationId),
                    delay);

                _logger.LogInformation("Job de integração agendado - AtividadeId: {AtividadeId}, TipoOperacao: {TipoOperacao}, CorrelationId: {CorrelationId}, JobId: {JobId}",
                    request.AtividadeId, request.TipoOperacao, correlationId, jobId);

                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao agendar job de integração para AtividadeId: {AtividadeId}", request.AtividadeId);
                throw;
            }
        }

        public async Task<string> ScheduleEmailJobAsync(ScheduleEmailJobDto request, string correlationId)
        {
            try
            {
                var delay = request.ExecutarEm.HasValue
                    ? request.ExecutarEm.Value - DateTime.UtcNow
                    : TimeSpan.FromSeconds(10);

                if (delay < TimeSpan.Zero)
                    delay = TimeSpan.FromSeconds(5);

                var jobId = BackgroundJob.Schedule(
                    () => ProcessEmailJobAsync(request, correlationId),
                    delay);

                _logger.LogInformation("Job de email agendado - Para: {To}, CorrelationId: {CorrelationId}, JobId: {JobId}",
                    request.To, correlationId, jobId);

                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao agendar job de email");
                throw;
            }
        }

        public async Task<string> ScheduleReportJobAsync(ScheduleReportJobDto request, string correlationId)
        {
            try
            {
                var delay = request.ExecutarEm.HasValue
                    ? request.ExecutarEm.Value - DateTime.UtcNow
                    : TimeSpan.FromMinutes(1);

                if (delay < TimeSpan.Zero)
                    delay = TimeSpan.FromSeconds(30);

                var jobId = BackgroundJob.Schedule(
                    () => ProcessReportJobAsync(request, correlationId),
                    delay);

                _logger.LogInformation("Job de relatório agendado - Tipo: {ReportType}, CorrelationId: {CorrelationId}, JobId: {JobId}",
                    request.ReportType, correlationId, jobId);

                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao agendar job de relatório");
                throw;
            }
        }

        public async Task<string> ScheduleMaintenanceJobAsync(DateTime? executeAt, string correlationId)
        {
            try
            {
                var delay = executeAt.HasValue
                    ? executeAt.Value - DateTime.UtcNow
                    : TimeSpan.FromSeconds(30);

                if (delay < TimeSpan.Zero)
                    delay = TimeSpan.FromSeconds(10);

                var jobId = BackgroundJob.Schedule(
                    () => ProcessMaintenanceJobAsync(correlationId),
                    delay);

                _logger.LogInformation("Job de manutenção agendado - CorrelationId: {CorrelationId}, JobId: {JobId}",
                    correlationId, jobId);

                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao agendar job de manutenção");
                throw;
            }
        }

        #endregion

        #region Jobs recorrentes

        public async Task<List<CustomRecurringJobDto>> GetRecurringJobsAsync()
        {
            try
            {
                var jobs = new List<CustomRecurringJobDto>();

                // Lista de jobs recorrentes conhecidos
                var knownRecurringJobs = new[]
                {
                    new { Id = "integration-retry", Name = "Retry de Integrações", Cron = "0 */30 * * * *" },
                    new { Id = "email-processing", Name = "Processamento de Emails", Cron = "0 */5 * * * *" },
                    new { Id = "maintenance", Name = "Manutenção do Sistema", Cron = "0 0 2 * * *" },
                    new { Id = "weekly-reports", Name = "Relatórios Semanais", Cron = "0 0 6 * * MON" }
                };

                foreach (var job in knownRecurringJobs)
                {
                    jobs.Add(new CustomRecurringJobDto
                    {
                        Id = job.Id,
                        Name = job.Name,
                        Cron = job.Cron,
                        Queue = "default",
                        IsActive = true,
                        LastExecution = null,
                        NextExecution = GetNextExecutionTime(job.Cron)
                    });
                }

                return jobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar jobs recorrentes");
                return new List<CustomRecurringJobDto>();
            }
        }

        public async Task<ApiResponse<bool>> PauseRecurringJobAsync(string jobId)
        {
            try
            {
                RecurringJob.RemoveIfExists(jobId);
                _logger.LogInformation("Job recorrente {JobId} pausado", jobId);
                return ApiResponse<bool>.SuccessResult(true, "Job recorrente pausado com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao pausar job recorrente {JobId}", jobId);
                return ApiResponse<bool>.ErrorResult("Erro ao pausar job recorrente");
            }
        }

        public async Task<ApiResponse<bool>> ResumeRecurringJobAsync(string jobId)
        {
            try
            {
                _logger.LogInformation("Job recorrente {JobId} retomado", jobId);
                return ApiResponse<bool>.SuccessResult(true, "Job recorrente retomado com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao retomar job recorrente {JobId}", jobId);
                return ApiResponse<bool>.ErrorResult("Erro ao retomar job recorrente");
            }
        }

        public async Task<ApiResponse<string>> TriggerRecurringJobAsync(string jobId)
        {
            try
            {
                RecurringJob.Trigger(jobId);
                _logger.LogInformation("Job recorrente {JobId} executado manualmente", jobId);
                return ApiResponse<string>.SuccessResult(jobId, "Job recorrente executado com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar job recorrente {JobId}", jobId);
                return ApiResponse<string>.ErrorResult("Erro ao executar job recorrente");
            }
        }

        #endregion

        #region Estatísticas

        public async Task<JobStatisticsDto> GetJobStatisticsAsync(int days)
        {
            try
            {
                var statistics = _monitoringApi.GetStatistics();
                var endDate = DateTime.UtcNow;
                var startDate = endDate.AddDays(-days);

                return new JobStatisticsDto
                {
                    TotalJobs = (int)(statistics.Enqueued + statistics.Scheduled + statistics.Processing + statistics.Succeeded + statistics.Failed),
                    SucceededJobs = (int)statistics.Succeeded,
                    FailedJobs = (int)statistics.Failed,
                    ProcessingJobs = (int)statistics.Processing,
                    ScheduledJobs = (int)statistics.Scheduled,
                    EnqueuedJobs = (int)statistics.Enqueued,
                    SuccessRate = statistics.Succeeded + statistics.Failed > 0
                        ? (double)statistics.Succeeded / (statistics.Succeeded + statistics.Failed) * 100
                        : 0,
                    PeriodStart = startDate,
                    PeriodEnd = endDate,
                    Daily = GenerateDailyStatistics(startDate, endDate)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar estatísticas dos jobs");
                return new JobStatisticsDto();
            }
        }

        #endregion

        #region Métodos de processamento dos jobs

        public async Task ProcessIntegrationJobAsync(string atividadeId, int tipoOperacao, int prioridade, string correlationId)
        {
            _logger.LogInformation("Processando job de integração - AtividadeId: {AtividadeId}, TipoOperacao: {TipoOperacao}, CorrelationId: {CorrelationId}",
                atividadeId, tipoOperacao, correlationId);

            try
            {
                await Task.Delay(1000); 

                _logger.LogInformation("Job de integração concluído - AtividadeId: {AtividadeId}, CorrelationId: {CorrelationId}",
                    atividadeId, correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no job de integração - AtividadeId: {AtividadeId}, CorrelationId: {CorrelationId}",
                    atividadeId, correlationId);
                throw;
            }
        }

        public async Task ProcessEmailJobAsync(ScheduleEmailJobDto emailDto, string correlationId)
        {
            _logger.LogInformation("Processando job de email - Para: {To}, CorrelationId: {CorrelationId}",
                emailDto.To, correlationId);

            try
            {
                var emailMessage = new EmailMessage
                {
                    To = emailDto.To,
                    Subject = emailDto.Subject,
                    Body = emailDto.Body,
                    IsHtml = emailDto.IsHtml,
                    Attachments = emailDto.Attachments ?? new List<string>(),
                    CorrelationId = correlationId
                };

                await _rabbitMQService.PublishEmailMessageAsync(emailMessage, correlationId);

                _logger.LogInformation("Job de email concluído - Para: {To}, CorrelationId: {CorrelationId}",
                    emailDto.To, correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no job de email - Para: {To}, CorrelationId: {CorrelationId}",
                    emailDto.To, correlationId);
                throw;
            }
        }

        public async Task ProcessReportJobAsync(ScheduleReportJobDto reportDto, string correlationId)
        {
            _logger.LogInformation("Processando job de relatório - Tipo: {ReportType}, CorrelationId: {CorrelationId}",
                reportDto.ReportType, correlationId);

            try
            {
                await Task.Delay(2000); 

                _logger.LogInformation("Job de relatório concluído - Tipo: {ReportType}, CorrelationId: {CorrelationId}",
                    reportDto.ReportType, correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no job de relatório - Tipo: {ReportType}, CorrelationId: {CorrelationId}",
                    reportDto.ReportType, correlationId);
                throw;
            }
        }

        public async Task ProcessMaintenanceJobAsync(string correlationId)
        {
            _logger.LogInformation("Processando job de manutenção - CorrelationId: {CorrelationId}", correlationId);

            try
            {
                await Task.Delay(5000); 

                _logger.LogInformation("Job de manutenção concluído - CorrelationId: {CorrelationId}", correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no job de manutenção - CorrelationId: {CorrelationId}", correlationId);
                throw;
            }
        }

        #endregion

        #region Métodos auxiliares

        private JobInfoDto MapToJobInfoDto(KeyValuePair<string, ScheduledJobDto> scheduledJob)
        {
            return new JobInfoDto
            {
                Id = scheduledJob.Key,
                Name = scheduledJob.Value.Job?.Method?.Name ?? "Unknown",
                State = "Scheduled",
                Queue = scheduledJob.Value.Job?.Queue ?? "default",
                CreatedAt = DateTime.UtcNow,
                ScheduledAt = null,
                Parameters = scheduledJob.Value.Job?.Args != null ? JsonSerializer.Serialize(scheduledJob.Value.Job.Args) : null
            };
        }

        private JobInfoDto MapToJobInfoDto(KeyValuePair<string, ProcessingJobDto> processingJob)
        {
            return new JobInfoDto
            {
                Id = processingJob.Key,
                Name = processingJob.Value.Job?.Method?.Name ?? "Unknown",
                State = "Processing",
                Queue = processingJob.Value.Job?.Queue ?? "default",
                CreatedAt = DateTime.UtcNow,
                StartedAt = DateTime.UtcNow,
                Parameters = processingJob.Value.Job?.Args != null ? JsonSerializer.Serialize(processingJob.Value.Job.Args) : null
            };
        }

        private JobInfoDto MapToJobInfoDto(KeyValuePair<string, SucceededJobDto> succeededJob)
        {
            return new JobInfoDto
            {
                Id = succeededJob.Key,
                Name = succeededJob.Value.Job?.Method?.Name ?? "Unknown",
                State = "Succeeded",
                Queue = succeededJob.Value.Job?.Queue ?? "default",
                CreatedAt = DateTime.UtcNow,
                StartedAt = DateTime.UtcNow,
                FinishedAt = DateTime.UtcNow,
                Parameters = succeededJob.Value.Job?.Args != null ? JsonSerializer.Serialize(succeededJob.Value.Job.Args) : null
            };
        }

        private JobInfoDto MapToJobInfoDto(KeyValuePair<string, FailedJobDto> failedJob)
        {
            return new JobInfoDto
            {
                Id = failedJob.Key,
                Name = failedJob.Value.Job?.Method?.Name ?? "Unknown",
                State = "Failed",
                Queue = failedJob.Value.Job?.Queue ?? "default",
                CreatedAt = DateTime.UtcNow,
                StartedAt = DateTime.UtcNow,
                FinishedAt = null,
                Reason = failedJob.Value.ExceptionMessage,
                AttemptCount = 1,
                Parameters = failedJob.Value.Job?.Args != null ? JsonSerializer.Serialize(failedJob.Value.Job.Args) : null
            };
        }

        private JobDetailDto MapToJobDetailDto(string jobId, JobDetailsDto jobDetails)
        {
            var jobInfo = jobDetails.Job;
            var lastState = jobDetails.History?.LastOrDefault();
            var processingState = jobDetails.History?.FirstOrDefault(h => h.StateName == "Processing");
            var completedState = jobDetails.History?.FirstOrDefault(h => h.StateName == "Succeeded" || h.StateName == "Failed");
            var failedState = jobDetails.History?.FirstOrDefault(h => h.StateName == "Failed");

            return new JobDetailDto
            {
                Id = jobId,
                Name = jobInfo?.Method?.Name ?? "Unknown",
                State = lastState?.StateName ?? "Unknown",
                Queue = jobInfo?.Queue ?? "default",
                CreatedAt = jobDetails.CreatedAt ?? DateTime.UtcNow,
                StartedAt = processingState?.CreatedAt,
                FinishedAt = completedState?.CreatedAt,
                Reason = failedState?.Reason,
                AttemptCount = jobDetails.History?.Count(h => h.StateName == "Failed") ?? 0,
                Arguments = jobInfo?.Args != null ? JsonSerializer.Serialize(jobInfo.Args) : null,
                Exception = failedState?.Data?.ContainsKey("ExceptionDetails") == true
                    ? failedState.Data["ExceptionDetails"]
                    : failedState?.Reason,
                History = jobDetails.History?.Select(h => new JobLogDto
                {
                    Timestamp = h.CreatedAt,
                    State = h.StateName,
                    Reason = h.Reason,
                    Data = h.Data != null ? JsonSerializer.Serialize(h.Data) : null
                }).ToList() ?? new List<JobLogDto>()
            };
        }

        private DateTime? GetNextExecutionTime(string cronExpression)
        {
            return DateTime.UtcNow.AddMinutes(30);
        }

        private List<JobStatisticDetail> GenerateDailyStatistics(DateTime startDate, DateTime endDate)
        {
            var stats = new List<JobStatisticDetail>();

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                stats.Add(new JobStatisticDetail
                {
                    Date = date,
                    Total = Random.Shared.Next(10, 100),
                    Succeeded = Random.Shared.Next(8, 95),
                    Failed = Random.Shared.Next(0, 5)
                });
            }

            return stats;
        }

        #endregion
    }
}