using API.Constants;
using API.Models.DTOs.Jobs;
using API.Models.Responses;
using API.Services.Interfaces;
using Hangfire;
using Hangfire.Storage.Monitoring;
using Hangfire.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


using CustomRecurringJobDto = API.Models.DTOs.Jobs.RecurringJobDto;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces(ApiConstants.ContentTypes.Json)]
public class JobsController : ControllerBase
{
    private readonly IHangfireJobService _jobService;
    private readonly ILogger<JobsController> _logger;

    public JobsController(
        IHangfireJobService jobService,
        ILogger<JobsController> logger)
    {
        _jobService = jobService;
        _logger = logger;
    }

    /// <summary>
    /// Listar todos os jobs ativos
    /// </summary>
    /// <param name="page">Página</param>
    /// <param name="pageSize">Tamanho da página</param>
    /// <param name="status">Filtro por status</param>
    /// <returns>Lista de jobs</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<JobInfoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJobs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        var filter = new JobFilterDto
        {
            Page = page,
            PageSize = pageSize,
            Status = status
        };

        var result = await _jobService.GetJobsAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Buscar job por ID
    /// </summary>
    /// <param name="jobId">ID do job</param>
    /// <returns>Detalhes do job</returns>
    [HttpGet("{jobId}")]
    [ProducesResponseType(typeof(ApiResponse<JobDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobById(string jobId)
    {
        var result = await _jobService.GetJobByIdAsync(jobId);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Agendar job de integração para atividade específica
    /// </summary>
    /// <param name="request">Dados para agendamento</param>
    /// <returns>ID do job agendado</returns>
    [HttpPost("schedule/integration")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ScheduleIntegrationJob([FromBody] ScheduleIntegrationJobDto request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponse<object>.ErrorResult(string.Join("; ", errors)));
        }

        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Agendando job de integração para atividade {AtividadeId}. CorrelationId: {CorrelationId}",
            request.AtividadeId, correlationId);

        var jobId = await _jobService.ScheduleIntegrationJobAsync(request, correlationId);

        return CreatedAtAction(
            nameof(GetJobById),
            new { jobId },
            ApiResponse<string>.SuccessResult(jobId, "Job agendado com sucesso"));
    }

    /// <summary>
    /// Agendar job de email
    /// </summary>
    /// <param name="request">Dados do email</param>
    /// <returns>ID do job agendado</returns>
    [HttpPost("schedule/email")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ScheduleEmailJob([FromBody] ScheduleEmailJobDto request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponse<object>.ErrorResult(string.Join("; ", errors)));
        }

        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Agendando job de email. CorrelationId: {CorrelationId}", correlationId);

        var jobId = await _jobService.ScheduleEmailJobAsync(request, correlationId);

        return CreatedAtAction(
            nameof(GetJobById),
            new { jobId },
            ApiResponse<string>.SuccessResult(jobId, "Job de email agendado com sucesso"));
    }

    /// <summary>
    /// Agendar job de relatório
    /// </summary>
    /// <param name="request">Configuração do relatório</param>
    /// <returns>ID do job agendado</returns>
    [HttpPost("schedule/report")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ScheduleReportJob([FromBody] ScheduleReportJobDto request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponse<object>.ErrorResult(string.Join("; ", errors)));
        }

        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Agendando job de relatório {ReportType}. CorrelationId: {CorrelationId}",
            request.ReportType, correlationId);

        var jobId = await _jobService.ScheduleReportJobAsync(request, correlationId);

        return CreatedAtAction(
            nameof(GetJobById),
            new { jobId },
            ApiResponse<string>.SuccessResult(jobId, "Job de relatório agendado com sucesso"));
    }

    /// <summary>
    /// Cancelar job
    /// </summary>
    /// <param name="jobId">ID do job</param>
    /// <returns>Confirmação do cancelamento</returns>
    [HttpDelete("{jobId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelJob(string jobId)
    {
        _logger.LogInformation("Cancelando job {JobId}", jobId);

        var result = await _jobService.CancelJobAsync(jobId);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Reexecutar job falhado
    /// </summary>
    /// <param name="jobId">ID do job</param>
    /// <returns>ID do novo job</returns>
    [HttpPost("{jobId}/retry")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RetryJob(string jobId)
    {
        _logger.LogInformation("Reagendando job {JobId}", jobId);

        var result = await _jobService.RetryJobAsync(jobId);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return CreatedAtAction(
            nameof(GetJobById),
            new { jobId = result.Data },
            result);
    }

    /// <summary>
    /// Obter estatísticas dos jobs
    /// </summary>
    /// <param name="days">Número de dias para análise</param>
    /// <returns>Estatísticas dos jobs</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ApiResponse<JobStatisticsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJobStatistics([FromQuery] int days = 7)
    {
        var statistics = await _jobService.GetJobStatisticsAsync(days);

        return Ok(ApiResponse<JobStatisticsDto>.SuccessResult(
            statistics,
            "Estatísticas obtidas com sucesso"));
    }

    /// <summary>
    /// Listar jobs recorrentes
    /// </summary>
    /// <returns>Lista de jobs recorrentes</returns>
    [HttpGet("recurring")]
    [ProducesResponseType(typeof(ApiResponse<List<CustomRecurringJobDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecurringJobs()
    {
        var recurringJobs = await _jobService.GetRecurringJobsAsync();

        return Ok(ApiResponse<List<CustomRecurringJobDto>>.SuccessResult(
            recurringJobs,
            "Jobs recorrentes obtidos com sucesso"));
    }

    /// <summary>
    /// Pausar job recorrente
    /// </summary>
    /// <param name="jobId">ID do job recorrente</param>
    /// <returns>Confirmação</returns>
    [HttpPost("recurring/{jobId}/pause")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PauseRecurringJob(string jobId)
    {
        _logger.LogInformation("Pausando job recorrente {JobId}", jobId);

        var result = await _jobService.PauseRecurringJobAsync(jobId);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Retomar job recorrente
    /// </summary>
    /// <param name="jobId">ID do job recorrente</param>
    /// <returns>Confirmação</returns>
    [HttpPost("recurring/{jobId}/resume")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResumeRecurringJob(string jobId)
    {
        _logger.LogInformation("Retomando job recorrente {JobId}", jobId);

        var result = await _jobService.ResumeRecurringJobAsync(jobId);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Executar job recorrente imediatamente
    /// </summary>
    /// <param name="jobId">ID do job recorrente</param>
    /// <returns>ID do job executado</returns>
    [HttpPost("recurring/{jobId}/trigger")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TriggerRecurringJob(string jobId)
    {
        _logger.LogInformation("Executando job recorrente {JobId} imediatamente", jobId);

        var result = await _jobService.TriggerRecurringJobAsync(jobId);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return CreatedAtAction(
            nameof(GetJobById),
            new { jobId = result.Data },
            result);
    }

    /// <summary>
    /// Limpar jobs antigos
    /// </summary>
    /// <param name="olderThanDays">Jobs mais antigos que X dias</param>
    /// <returns>Número de jobs removidos</returns>
    [HttpPost("cleanup")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CleanupOldJobs([FromQuery] int olderThanDays = 30)
    {
        _logger.LogInformation("Limpando jobs mais antigos que {Days} dias", olderThanDays);

        var removedCount = await _jobService.CleanupOldJobsAsync(olderThanDays);

        return Ok(ApiResponse<int>.SuccessResult(
            removedCount,
            $"{removedCount} jobs removidos com sucesso"));
    }

    /// <summary>
    /// Obter logs de um job específico
    /// </summary>
    /// <param name="jobId">ID do job</param>
    /// <returns>Logs do job</returns>
    [HttpGet("{jobId}/logs")]
    [ProducesResponseType(typeof(ApiResponse<List<JobLogDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobLogs(string jobId)
    {
        var result = await _jobService.GetJobLogsAsync(jobId);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Agendar manutenção manual
    /// </summary>
    /// <param name="executeAt">Data/hora para execução (opcional - se não informado, executa imediatamente)</param>
    /// <returns>ID do job agendado</returns>
    [HttpPost("schedule/maintenance")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status201Created)]
    public async Task<IActionResult> ScheduleMaintenanceJob([FromQuery] DateTime? executeAt = null)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Agendando job de manutenção manual. CorrelationId: {CorrelationId}", correlationId);

        var jobId = await _jobService.ScheduleMaintenanceJobAsync(executeAt, correlationId);

        return CreatedAtAction(
            nameof(GetJobById),
            new { jobId },
            ApiResponse<string>.SuccessResult(jobId, "Job de manutenção agendado com sucesso"));
    }
}