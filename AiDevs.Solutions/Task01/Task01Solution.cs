using AiDevs.Core.Interfaces;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task01;

/// <summary>
/// Example solution for Task 01
/// This is a template showing how to implement a task solution
/// </summary>
public class Task01Solution : ITaskSolution
{
    private readonly IOpenRouterService _openRouterService;

    public int TaskId => 1;

    public Task01Solution(IOpenRouterService openRouterService)
    {
        _openRouterService = openRouterService;
    }

    public async Task<SolutionResult> ExecuteAsync(string input, CancellationToken cancellationToken = default)
    {
        try
        {
            // Example: Use OpenRouter to process the input
            var prompt = $"Process this input: {input}";
            var response = await _openRouterService.CompleteAsync(
                prompt,
                model: "openai/gpt-3.5-turbo",
                temperature: 0.7,
                cancellationToken: cancellationToken
            );

            return SolutionResult.Ok(response, new Dictionary<string, object>
            {
                { "taskId", TaskId },
                { "inputLength", input.Length }
            });
        }
        catch (Exception ex)
        {
            return SolutionResult.Fail($"Task 01 failed: {ex.Message}");
        }
    }
}
