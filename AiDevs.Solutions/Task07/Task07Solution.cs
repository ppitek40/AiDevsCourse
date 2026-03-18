using System.Runtime.CompilerServices;
using AiDevs.Core.Interfaces;
using AiDevs.Core.Models;
using AiDevs.Infrastructure.Models;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task07;

/// <summary>
/// Solution for Task 07
/// Uses an AI agent with electricity diagram and rotation functions
/// </summary>
public class Task07Solution(
    IAgentSessionService agentSessionService) : ITaskSolution
{
    public int TaskId => 7;

    public async IAsyncEnumerable<StreamUpdate> ExecuteStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return StreamUpdate.Status("Starting Task 07...");

        var systemPrompt = """
You are an AI agent tasked with solving a 3x3 electrical grid puzzle. Your goal is to connect three power plants (PWR6132PL, PWR1593PL, PWR7264PL) to the emergency power source (located at the bottom-left) by rotating cable tiles on the board.

## Puzzle Mechanics
- The board is a 3x3 grid where each cell contains an electrical cable connector piece
- You can ONLY rotate tiles 90 degrees clockwise
- Each rotation costs one API call
- When the board matches the target configuration, you'll receive a flag: {FLG:...}

## Your Task
1. Use GetElectricityDiagramFunction to fetch the current board state (PNG image) and target configuration
2. Analyze both images to determine which tiles need rotation
3. Calculate how many 90-degree clockwise rotations each tile needs (0, 1, 2, or 3)
4. Use RotateFunction to rotate each tile that differs from the target - send one API call per rotation
5. Verify the result and continue rotating if needed
6. Once complete, you'll receive the flag {FLG:...}

## Important Notes
- Each rotation is 90 degrees CLOCKWISE only
- You must send separate API calls for each individual rotation (if a tile needs 2 rotations, send 2 separate calls)
- Compare the current state carefully with the target state before making moves
- The puzzle is solved when all cables form a complete path from the emergency power source to all three power plants
- If you need to restart, you can use the reset function

## Strategy
- Start by fetching and analyzing both diagrams
- Identify the position of each tile (row 0-2, col 0-2)
- Determine the optimal number of rotations needed for each tile
- Execute rotations systematically
- Verify completion

Begin by calling GetElectricityDiagramFunction with reset=true to see the current and target board states.
""";

        var messages = new List<OpenRouterMessage>
        {
            new() { Role = "system", Content = systemPrompt },
            new() { Role = "user", Content = "Start task execution." }
        };

        yield return StreamUpdate.Status("Starting agent session...");

        string? finalResult = null;
        await foreach (var update in agentSessionService.ExecuteAgentSessionStreamAsync(
            messages,
            [typeof(GetElectricityDiagramFunction), typeof(RotateFunction)],
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
