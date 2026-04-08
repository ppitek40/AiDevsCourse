using System.Runtime.CompilerServices;
using AiDevs.Core.Interfaces;
using AiDevs.Core.Models;
using AiDevs.Infrastructure.Models;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task18;

/// <summary>
/// Solution for Task 18 - Agent session with SendCommandTask18Function
/// </summary>
public class Task18Solution(
    IAgentSessionService agentSessionService) : ITaskSolution
{
    public int TaskId => 18;

    public async IAsyncEnumerable<StreamUpdate> ExecuteStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return StreamUpdate.Status("Starting Task 18...");

        var systemPrompt = """
            You are a tactical field commander coordinating an evacuation mission in the ruined city of Domatow.
            Your objective: locate a wounded partisan hiding in one of the tallest buildings and evacuate them by helicopter.

            ## Mission Context
            Intercepted radio signal from the survivor:
            "I survived. Bombs destroyed the city. Soldiers were here, searching for resources, took the oil. Now it's empty.
            I have a weapon, I am wounded. I hid in one of the tallest blocks. I have no food. Help."

            ## Resources
            - Up to 4 transporters (vehicles that move only on streets)
            - Up to 8 scouts (infantry that can move anywhere)
            - 300 action points total for the entire operation
            - 11x11 grid map with terrain markings

            ## Action Costs
            - Create scout: 5 points
            - Create transporter: 5 points base + 5 points per carried scout
            - Move scout: 7 points per tile
            - Move transporter: 1 point per tile
            - Inspect tile: 1 point
            - Disembark scouts from transporter: 0 points

            ## Step-by-Step Execution Plan
            0. Before starting restart the game using {"action": "reset"}
            1. Call {"action": "help"} to learn all available actions and their exact syntax
            2. Call {"action": "getMap"} to study the full terrain layout BEFORE deploying any units
            3. Analyze the map: identify streets (transporter routes), tall buildings (survivor's hiding spot), and optimal paths
            4. Plan unit deployment to minimize action point usage
            5. Create transporters and load scouts onto them — use transporters to reach key areas cheaply (1 pt/tile)
            6. Disembark scouts where foot movement is needed (building interiors)
            7. Use {"action": "inspect"} on each tile to search for the survivor — focus on TALLEST buildings first
            8. Regularly call {"action": "getLogs"} to analyze inspection results and track findings
            9. The moment a scout finds the partisan, IMMEDIATELY call {"action": "callHelicopter"} to trigger evacuation
            10. Completing the evacuation successfully will cause HQ to return the flag

            ## Important Rules
            - Always start with help, then getMap — never deploy units without studying the terrain first
            - The survivor is in one of the TALLEST buildings — do not waste points inspecting short structures
            - Track remaining action points carefully — total budget is 300
            - Use transporters to move units as close to the target as possible. Move by foot only when needed.
            - Do not rush the solution, take action one by one, monitoring the remaining action points.
            - After each inspect, use getLogs to check results before moving on
            - When you receive a flag in the format {FLG:...}, return it as your final answer
            """;

        var userPrompt = """
            Begin the evacuation mission. Follow this sequence:
            1. Call send_command with {"action": "help"} to discover all available actions and their syntax
            2. Call send_command with {"action": "getMap"} to analyze the city terrain
            3. Plan the most efficient route to the tallest buildings
            4. Deploy units and search for the survivor
            5. Call helicopter as soon as the survivor is found
            """;

        var messages = new List<OpenRouterMessage>
        {
            new() { Role = "system", Content = systemPrompt },
            new() { Role = "user", Content = userPrompt }
        };

        string? answer = null;

        yield return StreamUpdate.Status("Running agent session...");

        await foreach (var update in agentSessionService.ExecuteAgentSessionStreamAsync(
            messages,
            [typeof(SendCommandTask18Function)],
            model: OpenRouterModel.Claude45Sonnet,
            temperature: 0,
            maxIterations: 50,
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
