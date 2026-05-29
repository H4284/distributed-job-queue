using Microsoft.Extensions.Hosting;    
using Microsoft.Extensions.Logging;
using QueueServer.Models;
using System.Diagnostics;

namespace QueueServer.Services
{
    public class DispatcherService : BackgroundService
    {
        private readonly JobQueueService _jobQueue;
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<DispatcherService> _logger;
        private int _autoWorkerCount = 0;

        public DispatcherService(
            JobQueueService jobQueue,
            IHttpClientFactory httpFactory,
            ILogger<DispatcherService> logger)
        {
            _jobQueue = jobQueue;
            _httpFactory = httpFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.LogInformation("Dispatcher started.");

            while (!ct.IsCancellationRequested)
            {
                try
                {
                  
                    await HandleDeadWorkers();
                    await AutoScale();
                    await DispatchJobs();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Dispatcher error");
                }

                await Task.Delay(1000, ct); 
            }
        }

        private async Task HandleDeadWorkers()
        {
            var deadWorkers = _jobQueue.GetDeadWorkers();
            foreach (var worker in deadWorkers)
            {
                _logger.LogWarning("Worker {Id} is dead! Reassigning jobs...", worker.Id);

           
                var stuck = _jobQueue.GetAllJobs()
                    .Where(j => j.AssignedWorker == worker.Id && j.Status == "running")
                    .ToList();

                foreach (var job in stuck)
                {
                    job.Status = "pending";
                    job.AssignedWorker = null;
                    _jobQueue.Enqueue(job); 
                    _logger.LogInformation("Job {Id} re-queued.", job.Id);
                }

                _jobQueue.RemoveWorker(worker.Id);
            }
        }

        private async Task DispatchJobs()
        {
            while (_jobQueue.HasPending())
            {
                var worker = _jobQueue.GetLeastLoadedWorker();
                if (worker == null) break; 

                var job = _jobQueue.Dequeue();
                if (job == null) break;

                job.Status = "running";
                job.AssignedWorker = worker.Id;
                worker.ActiveJobs++;

                _logger.LogInformation(
                         "Dispatching job {JobId} (priority: {Priority}) to {WorkerId} (load: {Load})",
                            job.Id, job.Priority, worker.Id, worker.ActiveJobs);

                _ = SendJobToWorker(worker, job);
            }
        }

        private async Task SendJobToWorker(WorkerInfo worker, Job job)
        {
            try
            {
                var client = _httpFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                await client.PostAsJsonAsync($"{worker.Url}/execute", job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send job {Id} to {Worker}", job.Id, worker.Id);
                job.Status = "pending";
                job.AssignedWorker = null;
                worker.ActiveJobs = Math.Max(0, worker.ActiveJobs - 1);
                _jobQueue.Enqueue(job);
            }
        }

        private async Task AutoScale()
        {
            int pending = _jobQueue.PendingCount;
            int workerCount = _jobQueue.GetWorkers().Count(w => w.IsAlive);

            _logger.LogInformation("AutoScale: pending={Pending} autoWorkers={Auto}",
                pending, _autoWorkerCount);

            if (pending > 5 && _autoWorkerCount < 3)
            {
                _autoWorkerCount++;
                string workerName = $"worker-auto-{_autoWorkerCount}";
                _logger.LogInformation("Auto-scaling: starting {Name}", workerName);

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = $"run -d --name {workerName} " +
                                    $"--network distributed-job-queue_default " +
                                    $"-e WORKER_ID={workerName} " +
                                    $"-e WORKER_URL=http://{workerName}:5001 " +
                                    $"-e QUEUE_SERVER_URL=http://queue-server:5000 " +
                                    $"-e PYTHON_CMD=python3 " +
                                    $"-e ASPNETCORE_URLS=http://0.0.0.0:5001 " +
                                    $"distributed-job-queue-worker-1:latest",
                        UseShellExecute = false,
                         RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                process.Start();
                await process.WaitForExitAsync();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                _logger.LogInformation("Docker run output: {Output} {Error}", output, error);

                _logger.LogInformation("Docker output: {Output}", output);
                if (!string.IsNullOrEmpty(error))
                    _logger.LogError("Docker error: {Error}", error);
            }

         
            if (pending == 0 && _autoWorkerCount > 0)
            {
                await Task.Delay(30000);
                string workerName = $"worker-auto-{_autoWorkerCount}";
                _logger.LogInformation("Auto-scaling down: removing {Name}", workerName);

                Process.Start("docker", $"rm -f {workerName}");
                _autoWorkerCount--;
            }
        }
    }
}
