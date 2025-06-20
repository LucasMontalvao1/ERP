using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace API.Jobs.Base
{
    public abstract class BaseJob
    {
        protected readonly ILogger _logger;

        protected BaseJob(ILogger logger)
        {
            _logger = logger;
        }

        public async Task ExecuteWithErrorHandling(Func<Task> action, string jobName)
        {
            var jobId = GetJobId();

            try
            {
                _logger.LogInformation("Iniciando job {JobName}. JobId: {JobId}", jobName, jobId);

                var startTime = DateTime.UtcNow;
                await action();
                var duration = DateTime.UtcNow - startTime;

                _logger.LogInformation("Job {JobName} concluído com sucesso. Duração: {Duration}ms",
                    jobName, duration.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante execução do job {JobName}. JobId: {JobId}",
                    jobName, jobId);
                throw;
            }
        }

        protected string GetJobId() => Guid.NewGuid().ToString();
    }
}
