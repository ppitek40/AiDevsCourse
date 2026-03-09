using AiDevs.Core.Interfaces;
using AiDevs.Models;
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
    /// Execute a specific task solution
    /// </summary>
    /// <param name="taskId">Task ID (1-25)</param>
    /// <param name="request">Input data for the task</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Solution result</returns>
    [HttpPost("{taskId}")]
    public async Task<IActionResult> ExecuteSolution(
        int taskId,
        [FromBody] ExecuteSolutionRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing solution for Task {TaskId}", taskId);

        var solution = _solutions.FirstOrDefault(s => s.TaskId == taskId);
        if (solution == null)
        {
            _logger.LogWarning("Solution for Task {TaskId} not found", taskId);
            return NotFound(new { error = $"Solution for Task {taskId} not found" });
        }

        try
        {
            var result = await solution.ExecuteAsync(request.Input, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Task {TaskId} completed successfully", taskId);
                return Ok(result);
            }
            else
            {
                _logger.LogError("Task {TaskId} failed: {Error}", taskId, result.Error);
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while executing Task {TaskId}", taskId);
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
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
