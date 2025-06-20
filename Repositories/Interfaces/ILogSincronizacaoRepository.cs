using API.Models.DTOs.Integration;
using API.Models.Entities;
using API.Models.Responses;
using API.Repositories.Base.Interfaces;

namespace API.Repositories.Interfaces
{
    public interface ILogSincronizacaoRepository : IBaseRepository<LogSincronizacao>
    {
        Task<LogSincronizacao> CreateLogAsync(LogSincronizacao log);
        Task<bool> UpdateLogStatusAsync(int logId, int status, string? response = null, string? errorMessage = null);
        Task<PagedResponse<SyncLogDto>> GetPagedLogsAsync(SyncLogFilterDto filter);
        Task<LogSincronizacao?> GetByCorrelationIdAsync(string correlationId);
        Task<IEnumerable<LogSincronizacao>> GetFailedLogsAsync(int limit = 50);
        Task<IEnumerable<LogSincronizacao>> GetByAtividadeAsync(string codAtiv);
        Task<IntegrationStatisticsDto> GetStatisticsAsync(int days = 7);
        Task<bool> CleanupOldLogsAsync(int olderThanDays = 30);
        Task<int> GetCountByStatusAsync(int status, DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<LogSincronizacao>> GetRecentByStatusAsync(int status, int limit = 10);
    }
}