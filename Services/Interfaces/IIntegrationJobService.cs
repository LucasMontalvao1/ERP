namespace API.Services.Interfaces
{
    public interface IIntegrationJobService
    {
        Task ProcessActivityIntegrationAsync(string codAtiv, string operationType);
        Task ProcessActivityIntegrationByIdAsync(int activityId, string operationType);
        Task ProcessBatchSyncAsync(List<string> codAtivList, string operationType);
        Task ProcessBatchSyncByIdsAsync(List<int> activityIds, string operationType);
    }
}