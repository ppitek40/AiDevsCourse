using System.Runtime.CompilerServices;
using AiDevs.Core.Interfaces;
using AiDevs.Core.Models;
using AiDevs.Infrastructure.Models;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task08;

/// <summary>
/// Solution for Task 08
/// </summary>
public class Task08Solution(
    IAgentSessionService agentSessionService) : ITaskSolution
{
    public int TaskId => 8;

    public async IAsyncEnumerable<StreamUpdate> ExecuteStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return StreamUpdate.Status("Starting Task 08...");
        var systemPrompt = @"You are a log analysis orchestrator for a power plant failure investigation.

## Your Goal
Produce a condensed log summary (≤1500 tokens) containing ONLY events relevant to failure analysis:
power supply, cooling systems, water pumps, software components, and other plant subsystems.

## Workflow

### Phase 1 — Research 
- Use tools to read log.
- Provide a prompt to the sub-agent to extract relevant events.

### Phase 2 — Condense & Format
Produce a condensed log where:
- Each line = exactly one event (no multi-event lines)
- Format per line: `[YYYY-MM-DD HH:MM] [LEVEL] [COMPONENT_ID] short description`
- You MAY paraphrase and abbreviate — preserve: timestamp, severity level, component ID
- Total output must fit within 1500 tokens

### Phase 3 — Submit & Iterate
1. Send condensed logs 
2. Read the feedback from Centrala carefully — technicians will indicate:
   - Missing events
   - Unclear or insufficiently described components
3. Revise your condensed log based on feedback
4. Resubmit — repeat until technicians confirm completeness and you receive a `{FLG:...}` flag

## Rules
- NEVER skip Phase 1 — always read the log file completely
- On feedback: do targeted fixes. If data is missing, read the log file again.
- Stop and report the flag when `{FLG:...}` is received";

        var messages = new List<OpenRouterMessage>
        {
            new() { Role = "system", Content = systemPrompt },
            new() { Role = "user", Content = "Start task execution." }
        };

        yield return StreamUpdate.Status("Starting agent session...");

        string? finalResult = null;
        await foreach (var update in agentSessionService.ExecuteAgentSessionStreamAsync(
            messages,
            [typeof(LogReaderFunction), typeof(SendLogsFunction), typeof(TokenCounterFunction)],
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
            yield return StreamUpdate.Complete(SolutionResult.Ok($"Task completed. Result:\n{finalResult}"));
            yield break;
        }

        yield return StreamUpdate.Complete(SolutionResult.Fail("Failed to complete task - agent did not complete successfully"));
    }
}
