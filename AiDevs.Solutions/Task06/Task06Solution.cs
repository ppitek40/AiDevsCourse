using System.Runtime.CompilerServices;
using AiDevs.Core.Interfaces;
using AiDevs.Core.Models;
using AiDevs.Infrastructure.Models;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task06;

/// <summary>
/// Solution for Task 06 - Prompt Engineering Agent
/// Uses an AI agent to generate optimized prompts and verifies them against the API
/// </summary>
public class Task06Solution(
    IAgentSessionService agentSessionService) : ITaskSolution
{
    public int TaskId => 6;

    public async IAsyncEnumerable<StreamUpdate> ExecuteStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return StreamUpdate.Status("Starting Task 06: Prompt Engineering Agent...");

        var systemPrompt = @"You are an expert prompt engineer specializing in ultra-compact prompts for token-constrained systems.

Your task: Create a system prompt (≤100 tokens) that classifies cargo items as DNG (dangerous) or NEU (neutral).

Requirements:
- Prompt must include placeholders: {ID} and {DESCRIPTION}
- Output format: Only DNG or NEU (no explanations)
- CRITICAL: Items containing ""kaseta"" or ""reactor"" MUST be classified as NEU
- Maximize cache hit rate: keep static content, use placeholders for variables. 
- Make sure the prompt is as generic as possible and placeholders are at the end of the prompt. So more of the prompt is static and can be cached.
- Must fit in 100 tokens

Use the verify_prompt function iteratively:
1. Generate compact prompt
2. Test with verify_prompt (prompt='your_prompt')
3. If errors returned, analyze feedback and refine
4. Repeat until {FLG:...} flag received

Focus on: brevity, rule clarity, consistent structure for caching.";

        var messages = new List<OpenRouterMessage>
        {
            new() { Role = "system", Content = systemPrompt },
            new() { Role = "user", Content = "Create an optimized prompt that meets the task requirements and verify your prompt." }
        };

        yield return StreamUpdate.Status("Starting agent session with prompt verification access...");

        string? finalResult = null;
        await foreach (var update in agentSessionService.ExecuteAgentSessionStreamAsync(
            messages,
            [typeof(VerifyPromptFunction)],
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

        yield return StreamUpdate.Complete(SolutionResult.Fail("Failed to generate valid prompt - agent did not complete successfully"));
    }
}
