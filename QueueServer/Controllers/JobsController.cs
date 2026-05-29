using Microsoft.AspNetCore.Mvc;
using QueueServer.Models;
using QueueServer.Services;

namespace QueueServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly JobQueueService _queue;

        public JobsController(JobQueueService queue)
        {
            _queue = queue;
        }

     
        [HttpPost("submit")]
        public IActionResult Submit([FromBody] Job job)
        {
            job.Id = Guid.NewGuid().ToString();
            job.Status = "pending";
            job.CreatedAt = DateTime.UtcNow;
            _queue.Enqueue(job);
            return Ok(job);
        }

       
        [HttpGet("list")]
        public IActionResult List() => Ok(_queue.GetAllJobs());


        [HttpPost("workers/register")]
        public IActionResult Register([FromBody] WorkerInfo worker)
        {
            _queue.RegisterWorker(worker);
            return Ok();
        }

      
        [HttpPost("workers/heartbeat/{workerId}")]
        public IActionResult Heartbeat(string workerId)
        {
            _queue.UpdateHeartbeat(workerId);
            return Ok();
        }


        [HttpPost("workers/complete")]
        public IActionResult Complete([FromBody] JobResult result)
        {
            var job = _queue.GetJobById(result.JobId);
            if (job == null) return NotFound();

            job.Status = result.Success ? "done" : "failed";
            job.Result = result.Output;

 
            var worker = _queue.GetWorkers()
                .FirstOrDefault(w => w.Id == result.WorkerId);
            if (worker != null)
                worker.ActiveJobs = Math.Max(0, worker.ActiveJobs - 1);

            return Ok();
        }

        [HttpGet("workers")]
        public IActionResult GetWorkers() => Ok(_queue.GetWorkers());
    }


    public class JobResult
    {
        public string JobId { get; set; } = "";
        public string WorkerId { get; set; } = "";
        public bool Success { get; set; }
        public string Output { get; set; } = "";
    }
}
