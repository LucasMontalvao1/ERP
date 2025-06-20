using API.Models.Entities;
using API.Repositories.Base.Interfaces;

namespace API.Repositories.Interfaces
{
    public interface IFilaProcessamentoRepository : IBaseRepository<FilaProcessamento>
    {
        Task<IEnumerable<FilaProcessamento>> GetPendingItemsAsync(string nomeFila, int limit = 50);
        Task<IEnumerable<FilaProcessamento>> GetByPriorityAsync(string nomeFila, int limit = 50);
        Task<bool> UpdateStatusAsync(int id, int status, string? erro = null);
        Task<bool> IncrementAttemptsAsync(int id, DateTime? nextProcessing = null);
        Task<IEnumerable<FilaProcessamento>> GetFailedItemsAsync(string nomeFila, int limit = 50);
        Task<bool> RequeueItemAsync(int id);
        Task<int> GetQueueCountAsync(string nomeFila, int? status = null);
        Task<bool> CleanupProcessedItemsAsync(int olderThanDays = 7);
        Task<Dictionary<string, int>> GetQueueStatisticsAsync();
        Task<bool> CancelItemAsync(int id, string motivo);
    }
}