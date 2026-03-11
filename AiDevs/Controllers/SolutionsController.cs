using AiDevs.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AiDevs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SolutionsController : ControllerBase
{
    private readonly IEnumerable<ITaskSolution> _solutions;
    private readonly ILogger<SolutionsController> _logger;

    public SolutionsController(
        IEnumerable<ITaskSolution> solutions,
        ILogger<SolutionsController> logger)
    {
        _solutions = solutions;
        _logger = logger;
    }

    /// <summary>
    /// Execute a specific task solution with streaming
    /// </summary>
    /// <param name="taskId">Task ID (1-25)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Server-Sent Events stream</returns>
    [HttpPost("{taskId}")]
    public async Task ExecuteSolutionStream(
        int taskId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing solution for Task {TaskId} with streaming", taskId);

        var solution = _solutions.FirstOrDefault(s => s.TaskId == taskId);
        if (solution == null)
        {
            _logger.LogWarning("Solution for Task {TaskId} not found", taskId);
            Response.StatusCode = 404;
            await Response.WriteAsync($"{{\"error\": \"Solution for Task {taskId} not found\"}}", cancellationToken);
            return;
        }

        // Set headers for Server-Sent Events
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        try
        {
            await foreach (var update in solution.ExecuteStreamAsync(cancellationToken))
            {
                var json = System.Text.Json.JsonSerializer.Serialize(update);
                await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            _logger.LogInformation("Task {TaskId} streaming completed", taskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while streaming Task {TaskId}", taskId);
            var errorUpdate = new { type = "error", error = ex.Message };
            var json = System.Text.Json.JsonSerializer.Serialize(errorUpdate);
            await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Get list of all available task solutions
    /// </summary>
    /// <returns>List of task IDs</returns>
    [HttpGet]
    public IActionResult GetAvailableSolutions()
    {
        var taskIds = _solutions.Select(s => new
        {
            taskId = s.TaskId,
            type = s.GetType().Name
        }).OrderBy(x => x.taskId);

        return Ok(new { tasks = taskIds });
    }
}
