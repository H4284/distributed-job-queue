using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;
using Worker.Models;
using Worker.Services;

namespace Worker.Controllers;

[ApiController]
[Route("[controller]")]
public class ExecuteController : ControllerBase
{
    private readonly JobProcessorService _processor;
    private readonly ILogger<ExecuteController> _logger;

    public ExecuteController(
        JobProcessorService processor,
        ILogger<ExecuteController> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    [HttpPost]
    public IActionResult Execute([FromBody] Job job)
    {
        _logger.LogInformation("Received job {Id}", job.Id);
        _ = Task.Run(() => _processor.ProcessJob(job));
        return Ok(new { message = "Job accepted" });
    }

    [HttpGet("/health")]
    public IActionResult Health() => Ok(new { status = "alive" });
}