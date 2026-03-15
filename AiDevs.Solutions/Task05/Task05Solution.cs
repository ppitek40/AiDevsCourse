using System.Runtime.CompilerServices;
using AiDevs.Core.Interfaces;
using AiDevs.Core.Models;
using AiDevs.Infrastructure.Models;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task05;

/// <summary>
/// Solution for Task 05 - Railway activation with self-documenting API
/// Uses LLM agent to discover and follow API workflow autonomously
/// </summary>
public class Task05Solution(
    IAgentSessionService agentSessionService) : ITaskSolution
{
    private const string RailwayCode = "X-01";

    public int TaskId => 5;

    public async IAsyncEnumerable<StreamUpdate> ExecuteStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return StreamUpdate.Status("Starting Task 05: Railway activation with AI agent...");

        var systemPrompt = $@"You are an AI agent tasked with activating railway route {RailwayCode} using a self-documenting API.

Your task:
1. Start by calling the Railway API with action='help' to get the complete API documentation
2. Carefully read and analyze the documentation to understand:
   - What actions are available
   - What parameters each action requires
   - The correct sequence of API calls needed to activate the railway
3. Follow the documented workflow step by step to activate railway {RailwayCode}
4. Handle any errors or requirements mentioned in API responses
5. Continue calling the API until you receive a flag in format {{FLG:...}}
6. When you get the flag, return it in your final response

IMPORTANT NOTES:
- The API handles 503 errors automatically with retries - don't worry about them
- Rate limits are monitored automatically - check the _rateLimitInfo in responses
- Read the API documentation carefully - it will tell you exactly what to do
- Follow the instructions in each API response
- If you get an error, read it carefully and adjust your approach
- The goal is to get a response containing {{FLG:...}} which means success

Return your final answer with the flag when you receive it.";

        var messages = new List<OpenRouterMessage>
        {
            new() { Role = "system", Content = systemPrompt },
            new() { Role = "user", Content = $"Activate railway {RailwayCode}. Start by getting the API documentation with action='help', then follow the documented steps." }
        };

        yield return StreamUpdate.Status("Starting agent session with railway API access...");

        string? finalResult = null;
        await foreach (var update in agentSessionService.ExecuteAgentSessionStreamAsync(
            messages,
            [typeof(RailwayApiFunction)],
            model: OpenRouterModel.Claude45Sonnet,
            temperature: 0,
            maxIterations: 30,
            cancellationToken: cancellationToken))
        {
            yield return update;

            if (update.IsComplete && update.FinalResult?.Success == true)
                finalResult = update.FinalResult.Output;
        }

        if (finalResult != null)
        {
            // Check if the result contains a flag
            if (finalResult.Contains("{FLG:"))
            {
                yield return StreamUpdate.Complete(SolutionResult.Ok(finalResult));
                yield break;
            }

            yield return StreamUpdate.Complete(SolutionResult.Ok($"Task completed. Result:\n{finalResult}"));
            yield break;
        }

        yield return StreamUpdate.Complete(SolutionResult.Fail("Failed to activate railway - agent did not complete successfully"));
    }
}
