using Microsoft.Extensions.Http;
using System.Net.Http.Json;

namespace Worker.Services;

public class HeartbeatService : BackgroundService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<HeartbeatService> _logger;
    private readonly string _queueServerUrl;
    private readonly string _workerId;

    public HeartbeatService(
        IHttpClientFactory httpFactory,
        ILogger<HeartbeatService> logger,
        IConfiguration config)
    {
        _httpFactory = httpFactory;
        _logger = logger;
        _queueServerUrl = config["QUEUE_SERVER_URL"] ?? "http://localhost:5000";
        _workerId = config["WORKER_ID"] ?? "worker-1";
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(3000, ct);
        await RegisterWithQueueServer();

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var client = _httpFactory.CreateClient();
                await client.PostAsync(
                    $"{_queueServerUrl}/api/jobs/workers/heartbeat/{_workerId}",
                    null, ct);

                _logger.LogInformation("Heartbeat sent from {WorkerId}", _workerId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Heartbeat failed: {Message}", ex.Message);
            }

            await Task.Delay(5000, ct);
        }
    }

    private async Task RegisterWithQueueServer()
    {
        for (int i = 0; i < 5; i++)
        {
            try
            {
                var client = _httpFactory.CreateClient();
                var workerUrl = Environment.GetEnvironmentVariable("WORKER_URL")
     ?? "http://localhost:5001";

                var workerInfo = new
                {
                    Id = _workerId,
                    Url = workerUrl
                };

                var response = await client.PostAsJsonAsync(
                    $"{_queueServerUrl}/api/jobs/workers/register",
                    workerInfo);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Worker {Id} registered successfully!", _workerId);
                    return;
                }
            }
            catch
            {
                _logger.LogWarning("Registration attempt {i} failed, retrying...", i + 1);
                await Task.Delay(2000);
            }
        }
    }
}