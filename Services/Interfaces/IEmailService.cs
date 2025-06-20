using API.Jobs.Base;
using API.Models.DTOs.Webhook;
using API.Models.Responses;

namespace API.Services.Interfaces
{
    public interface IEmailService
    {
        Task<ApiResponse<bool>> SendEmailAsync(EmailMessage emailMessage);
        Task<ApiResponse<bool>> SendReportEmailAsync(string reportType, List<string> recipients, string correlationId);
        Task<ApiResponse<bool>> SendDataIntegrityReportAsync(List<string> issues);
        Task<ApiResponse<bool>> SendIntegrationNotificationAsync(string type, object data, string correlationId);
        Task<bool> ValidateEmailConfigurationAsync();
        Task<ApiResponse<object>> GetEmailStatisticsAsync(int days = 7);
    }
}