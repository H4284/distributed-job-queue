using System.Net.Http.Json;
using Worker.Models;

namespace Worker.Services;

public class JobProcessorService : BackgroundService
{
    private readonly CodeExecutor _executor;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<JobProcessorService> _logger;
    private readonly string _queueServerUrl;
    private readonly string _workerId;

    public JobProcessorService(
        CodeExecutor executor,
        IHttpClientFactory httpFactory,
        ILogger<JobProcessorService> logger,
        IConfiguration config)
    {
        _executor = executor;
        _httpFactory = httpFactory;
        _logger = logger;
        _queueServerUrl = config["QUEUE_SERVER_URL"] ?? "http://localhost:5000";
        _workerId = config["WORKER_ID"] ?? "worker-1";
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Worker {Id} ready and waiting for jobs...", _workerId);
        await Task.Delay(Timeout.Infinite, ct);
    }

    public async Task ProcessJob(Job job)
    {
        _logger.LogInformation("Processing job {Id} ({Language})", job.Id, job.Language);

        (bool success, string output) result = await _executor.ExecuteAsync(job.Code, job.Language);

        _logger.LogInformation("Job {Id} finished. Success: {Success}", job.Id, result.success);

        await ReportResult(job.Id, result.success, result.output);
    }

    private async Task ReportResult(string jobId, bool success, string output)
    {
        try
        {
            var client = _httpFactory.CreateClient();
            await client.PostAsJsonAsync(
                $"{_queueServerUrl}/api/jobs/workers/complete",
                new
                {
                    JobId = jobId,
                    WorkerId = _workerId,
                    Success = success,
                    Output = output
                });
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to report result for job {Id}: {Error}", jobId, ex.Message);
        }
    }
}