using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Core.Interfaces;
using AiDevs.Core.Models;
using AiDevs.Infrastructure.Models;
using AiDevs.Infrastructure.Services;
using AiDevs.Tools;
using Microsoft.AspNetCore.Mvc;

namespace AiDevs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SolutionsController(
    IEnumerable<ITaskSolution> solutions,
    ILogger<SolutionsController> logger,
    IAiDevsApiService aiDevsApiService)
    : ControllerBase
{
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
        logger.LogInformation("Executing solution for Task {TaskId} with streaming", taskId);

        var solution = solutions.FirstOrDefault(s => s.TaskId == taskId);
        if (solution == null)
        {
            logger.LogWarning("Solution for Task {TaskId} not found", taskId);
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
                var json = JsonSerializer.Serialize(update);
                await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            logger.LogInformation("Task {TaskId} streaming completed", taskId);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception while streaming Task {TaskId}", taskId);
            var err = StreamUpdate.Complete(SolutionResult.Fail(ex.Message));
            var json = JsonSerializer.Serialize(err);
            await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Get list of all available task solutions
    /// </summary>
    /// <returns>List of task IDs with their status</returns>
    [HttpGet]
    public async Task<IActionResult> GetAvailableSolutions()
    {
        const int totalTasks = 25;
        var startDate = new DateTime(2026, 3, 9); // March 9, 2026
        var today = DateTime.Today;

        // Fetch completed tasks from the API
        var stats = await aiDevsApiService.GetStatsAsync();
        var completedTaskIds = new HashSet<int>(stats.Days.Select(int.Parse));
        var workingDaysList = DateService.GetNWorkingDaysFrom(startDate, totalTasks);

        var tasks = new List<object>();

        for (var i = 0; i < totalTasks; i++)
        {
            if (completedTaskIds.Contains(i + 1))
            {
                tasks.Add(new { taskId = i + 1, status = "Completed" });
                continue;
            }

            // Determine status
            if (workingDaysList[i] > today)
            {
                tasks.Add(new { taskId = i + 1, status = "NotPublished" });
                continue;
            }

            tasks.Add(new { taskId = i + 1, status = "NotCompleted" });
        }

        return Ok(new { tasks });
    }

   
}

public class CustomLlmRequest
{
    [JsonPropertyName("model")]
    public OpenRouterModel Model { get; set; } = OpenRouterModel.Gpt4o;

    [JsonPropertyName("toolTypes")]
    public List<string> ToolTypes { get; set; } = [];

    [JsonPropertyName("systemMessage")]
    public string? SystemMessage { get; set; }

    [JsonPropertyName("userMessage")]
    public string UserMessage { get; set; } = string.Empty;

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;

    [JsonPropertyName("iterations")]
    public int Iterations { get; set; } = 1;
}