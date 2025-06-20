using API.Models.DTOs.Jobs;
using API.Models.Responses;

namespace API.Services.Interfaces;

public interface IHangfireJobService
{
    // Métodos básicos de gerenciamento de jobs
    Task<PagedResponse<JobInfoDto>> GetJobsAsync(JobFilterDto filter);
    Task<ApiResponse<JobDetailDto>> GetJobByIdAsync(string jobId);
    Task<ApiResponse<bool>> CancelJobAsync(string jobId);
    Task<ApiResponse<string>> RetryJobAsync(string jobId);
    Task<int> CleanupOldJobsAsync(int olderThanDays);
    Task<ApiResponse<List<JobLogDto>>> GetJobLogsAsync(string jobId);

    // Agendamento de jobs específicos
    Task<string> ScheduleIntegrationJobAsync(ScheduleIntegrationJobDto request, string correlationId);
    Task<string> ScheduleEmailJobAsync(ScheduleEmailJobDto request, string correlationId);
    Task<string> ScheduleReportJobAsync(ScheduleReportJobDto request, string correlationId);
    Task<string> ScheduleMaintenanceJobAsync(DateTime? executeAt, string correlationId);

    // Jobs recorrentes
    Task<List<RecurringJobDto>> GetRecurringJobsAsync();
    Task<ApiResponse<bool>> PauseRecurringJobAsync(string jobId);
    Task<ApiResponse<bool>> ResumeRecurringJobAsync(string jobId);
    Task<ApiResponse<string>> TriggerRecurringJobAsync(string jobId);

    // Estatísticas
    Task<JobStatisticsDto> GetJobStatisticsAsync(int days);
}