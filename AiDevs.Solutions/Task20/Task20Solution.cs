using System.Runtime.CompilerServices;
using AiDevs.Core.Interfaces;
using AiDevs.Core.Models;
using AiDevs.Infrastructure.Models;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task20;

/// <summary>
/// Solution for Task 20 - Agent session
/// </summary>
public class Task20Solution(
    IAgentSessionService agentSessionService) : ITaskSolution
{
    public int TaskId => 20;

    public async IAsyncEnumerable<StreamUpdate> ExecuteStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return StreamUpdate.Status("Starting Task 20...");

        var systemPrompt = """
            TODO: Add task-specific system prompt here.
            """;

        var userPrompt = """
            TODO: Add task-specific user prompt here.
            """;

        var messages = new List<OpenRouterMessage>
        {
            new() { Role = "system", Content = systemPrompt },
            new() { Role = "user", Content = userPrompt }
        };

        string? answer = null;

        await foreach (var update in agentSessionService.ExecuteAgentSessionStreamAsync(
            messages,
            [typeof(SendCommandTask20Function)],
            model: OpenRouterModel.Claude45Sonnet,
            temperature: 0,
            maxIterations: 30,
            cancellationToken: cancellationToken))
        {
            yield return update;

            if (update.IsComplete && update.FinalResult?.Success == true)
                answer = update.FinalResult.Output;
        }

        if (answer == null)
        {
            yield return StreamUpdate.Complete(SolutionResult.Fail("Agent did not produce an answer"));
            yield break;
        }

        yield return StreamUpdate.Complete(SolutionResult.Ok(answer));
    }
}
