using System.Runtime.CompilerServices;
using AiDevs.Core.Interfaces;
using AiDevs.Core.Models;
using AiDevs.Infrastructure.Models;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task15;

/// <summary>
/// Solution for Task 15 - [Brief description of what this task does]
/// </summary>
public class Task15Solution(
    IAgentSessionService agentSessionService,
    IAiDevsApiService aiDevsApiService) : ITaskSolution
{
    public int TaskId => 15;

    public async IAsyncEnumerable<StreamUpdate> ExecuteStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return StreamUpdate.Status("Starting Task 15...");

        var systemPrompt = """
            You are a strategic route-planning agent. Your mission is to guide a messenger to the city of Skolwin on a 10x10 map.

            ## Resources
            - Food: 10 portions (consumed every move — faster travel = less food consumed per move, slower = more)
            - Fuel: 10 units (consumed every move by vehicles — faster vehicle = more fuel per move; walking uses no fuel)
            - You can exit any vehicle at any time and continue on foot

            ## Initial steps (mandatory, in order)
            1. Use `toolsearch` with a query about maps to find the map tool, then retrieve the map
            2. Use `toolsearch` with a query about vehicles to find available vehicles and their fuel/food consumption rates
            3. Analyze the map: identify your start position, the location of Skolwin, and all obstacles (rivers, trees, rocks, etc.)

            ## Planning
            - You have 10 food + 10 fuel total. Every move consumes food. Vehicle moves also consume fuel.
            - Faster vehicle → more fuel per move, less food per move
            - Slower / on foot → no fuel cost, more food per move
            - Find a path that reaches Skolwin without running out of either resource
            - Prefer the shortest passable path; adjust vehicle use to stay within budget

            ## Execution
            - Move step by step using the appropriate movement tool
            - After each move confirm your current position and remaining resources
            - Reaching the destination field will yield a flag — that flag is your final answer

            ## Tool usage
            - You have ONE tool available: call it with tool name `toolsearch` to discover other tools
            - Change the tool name to call any discovered tool
            - All tools accept a `query` parameter and return JSON with the top 3 matching results
            - Use natural language or keywords in your queries
                                                                                  
            ##IMPORTANT!
            - Remember! YOU CAN WALK OVER WATER!
                                                                                              
            ## Final answer
            When you figure out the optimal route, use your second tool Submit, to submit your route to the server.
            When you reach Skolwin and receive a flag, output ONLY the flag value as your final answer.
            """;

        var userPrompt = "Start the mission. First, discover the map and available vehicles, then plan and execute the optimal route to Skolwin.";

        var messages = new List<OpenRouterMessage>
        {
            new() { Role = "system", Content = systemPrompt },
            new() { Role = "user", Content = userPrompt }
        };

        string? answer = null;

        yield return StreamUpdate.Status("Running agent session...");

        await foreach (var update in agentSessionService.ExecuteAgentSessionStreamAsync(
            messages,
            [typeof(UseToolFunction), typeof(SubmitFunction)],
            model: OpenRouterModel.Claude45Sonnet,
            temperature: 0,
            maxIterations: 20,
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
