using API.Models.DTOs.Atividade;
using API.Models.Responses;

namespace API.Services.Interfaces
{
    public interface IAtividadeService
    {
        Task<ApiResponse<AtividadeResponseDto>> CreateAsync(CreateAtividadeDto dto, int userId);
        Task<ApiResponse<AtividadeResponseDto>> UpdateAsync(string codAtiv, UpdateAtividadeDto dto, int userId);
        Task<ApiResponse<bool>> DeleteAsync(string codAtiv, int userId);
        Task<ApiResponse<AtividadeResponseDto>> GetByCodAtivAsync(string codAtiv);
        Task<PagedResponse<AtividadeListDto>> GetPagedAsync(AtividadeFilterDto filter);
        Task<ApiResponse<IEnumerable<AtividadeListDto>>> SearchAsync(string searchTerm, int limit = 10);
        Task<ApiResponse<BatchSyncResponseDto>> SyncBatchAsync(List<string> codAtivs, string correlationId);
        Task<ApiResponse<bool>> ForceSyncAsync(string codAtiv, string correlationId);
        Task<ApiResponse<object>> GetSyncStatisticsAsync();
    }
}