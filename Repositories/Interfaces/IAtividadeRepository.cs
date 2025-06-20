using API.Models.DTOs.Atividade;
using API.Models.Entities;
using API.Models.Responses;
using API.Repositories.Base.Interfaces;

namespace API.Repositories.Interfaces
{
    public interface IAtividadeRepository : IBaseRepository<Atividade>
    {
        Task<Atividade?> GetByCodAtivAsync(string codAtiv);
        Task<bool> ExistsByCodAtivAsync(string codAtiv);
        Task<PagedResponse<AtividadeListDto>> GetPagedAsync(AtividadeFilterDto filter);
        Task<IEnumerable<Atividade>> GetPendingSyncAsync(int limit = 50);
        Task<IEnumerable<Atividade>> GetFailedSyncAsync(int limit = 50);
        Task<bool> UpdateSyncStatusAsync(string codAtiv, int status, string? externalId = null, string? errorMessage = null);
        Task<int> GetCountByStatusAsync(int status);
        Task<IEnumerable<Atividade>> SearchAsync(string searchTerm, int limit = 10);
        Task<bool> BulkUpdateSyncStatusAsync(List<string> codAtivs, int status);
        Task<IEnumerable<Atividade>> GetByRamoAsync(string ramo);
        Task<bool> SoftDeleteAsync(string codAtiv, int userId);
    }
}