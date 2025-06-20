using API.Models.DTOs.Integration;
using API.Models.Entities;
using API.Models.Responses;
using API.Repositories.Base.Interfaces;

namespace API.Repositories.Interfaces
{
    public interface IConfiguracaoIntegracaoRepository : IBaseRepository<ConfiguracaoIntegracao>
    {
        Task<ConfiguracaoIntegracao?> GetDefaultConfigAsync();
        Task<ConfiguracaoIntegracao?> GetByNameAsync(string nome);
        Task<IEnumerable<ConfiguracaoIntegracao>> GetActiveConfigsAsync();
        Task<bool> SetAsDefaultAsync(int id);
        Task<bool> TestConnectionAsync(int id);
        Task<bool> UpdateTokenAsync(int id, string token, DateTime expiration);
        Task<bool> UpdateLastConnectionAsync(int id);
        Task<PagedResponse<IntegrationConfigDto>> GetPagedConfigsAsync(int page, int pageSize, bool? activeOnly = null);
    }
}