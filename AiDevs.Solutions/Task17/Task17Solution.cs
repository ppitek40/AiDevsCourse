using System.Runtime.CompilerServices;
using AiDevs.Core.Interfaces;
using AiDevs.Core.Models;
using AiDevs.Infrastructure.Models;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task17;

/// <summary>
/// Solution for Task 17 - Wind turbine scheduling agent
/// </summary>
public class Task17Solution(
    IAgentSessionService agentSessionService) : ITaskSolution
{
    public int TaskId => 17;

    public async IAsyncEnumerable<StreamUpdate> ExecuteStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return StreamUpdate.Status("Starting Task 17...");

        var systemPrompt = """
            You are a wind turbine scheduling agent. Your ONLY job is to call exactly 2 tools in sequence:

            ## STEP 1 — Call get_all_data (no arguments)
            This returns the weather forecast, turbine specifications, and power plant energy requirements.

            ## STEP 2 — Analyze the data and call generate_and_send_configs
            Using the data from Step 1, determine the correct configuration:
            - STORM slots (wind speed > turbine max tolerance): set turbineMode = "idle" and pitchAngle to the no-resistance value from turbine specs.
            - PRODUCTION slots (best wind for generating the required energy): set turbineMode = "production" and pitchAngle to the optimal production value from turbine specs.

            Configure only STORM and PRODUCTION slots. dont configure other slots.
            Pass slot configurations as the `configs` JSON array string.
            Each entry MUST include windMs (the actual wind speed at that slot — needed internally to generate unlock codes).

            The tool handles unlock code generation, turbine check, and submission automatically.
            The done response contains the flag — return it as your final answer.
            
            DO NOT EXPLAIN YOUR DECISIONS OR REASONS FOR CHOOSING A CONFIGURATION. ONLY CALL THE TOOLS.
            """;

        var userPrompt = "Start by calling get_all_data, then analyze the results and call generate_and_send_configs with the complete configuration.";

        var messages = new List<OpenRouterMessage>
        {
            new() { Role = "system", Content = systemPrompt },
            new() { Role = "user", Content = userPrompt }
        };

        string? answer = null;

        yield return StreamUpdate.Status("Running agent session...");

        await foreach (var update in agentSessionService.ExecuteAgentSessionStreamAsync(
            messages,
            [typeof(GetAllDataFunction), typeof(GenerateAndSendConfigsFunction)],
            model: OpenRouterModel.Claude45Sonnet,
            temperature: 0,
            maxIterations: 10,
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
