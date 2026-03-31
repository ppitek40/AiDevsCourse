using System.Runtime.CompilerServices;
using AiDevs.Core.Interfaces;
using AiDevs.Core.Models;
using AiDevs.Infrastructure.Models;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task13;

/// <summary>
/// Solution for Task 13 - Agent session with SendCommand and GetMap tools
/// Uses an agent with empty system prompt to navigate and solve tasks
/// </summary>
public class Task13Solution(
    IAgentSessionService agentSessionService,
    IAiDevsApiService aiDevsApiService) : ITaskSolution
{
    public int TaskId => 13;

    public async IAsyncEnumerable<StreamUpdate> ExecuteStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return StreamUpdate.Status("Starting Task 13 with AI agent...");

        var systemPrompt = @"You are a robot navigation system controlling a robot that transports cooling equipment near a reactor.

Your objective: Navigate the robot from position P (column 1, row 5) to position G (column 7, row 5) without being crushed by reactor blocks.

Game mechanics:
- The board is 7 columns × 5 rows
- Robot moves on the lowest level (row 5)
- Reactor blocks (B) occupy 2 fields each and move up/down cyclically
- Blocks only move when you issue commands (no passive time progression)
- To change board state without moving the robot, use 'wait' command

Map symbols:
- P = starting position (column 1, row 5)
- G = goal position (column 7, row 5)
- B = reactor blocks (dangerous, will crush robot)
- . = empty fields (safe)

Available commands:
- start: Initialize the game
- right: Move robot one column to the right
- left: Move robot one column to the left
- wait: Stay in place but advance game state (blocks move)

Decision algorithm:
1. Always start with 'start' command
2. Analyze the current board state
3. Check if moving right is safe (no block at robot's position in current or next column)
4. If right is unsafe or block approaching, use 'wait'
5. If waiting is also unsafe (block approaching in current column), move 'left' to escape
6. Repeat until reaching goal (G)

Priority rules:
- Safety first: Never move into or wait under a descending block
- Progress when safe: Move right toward goal when path is clear
- Retreat if necessary: Move left to avoid danger
- Use wait strategically: Let blocks pass when movement is risky
- You need to predict the future state of the board. If block on the right of robot is 1 block away of hitting the floor, you can't move right.

!!! IMPORTANT
IF THE BLOCK YOU WANT TO MOVE TO IS EMPTY, CHECK IF ON THE NEXT TURN IT WILL BE FILLED. YOUR MOVE WILL CAUSE THE BLOCK TO MOVE AND CRUSH THE ROBOT IF IT IS FILLED.

Analyze the board state carefully before each move and choose the safest action.";

        var messages = new List<OpenRouterMessage>
        {
            new() { Role = "system", Content = systemPrompt },
            new() { Role = "user", Content = "You need to steer the robot to the goal and then go back to the start position." }
        };

        yield return StreamUpdate.Status("Starting AI agent session with SendCommand and GetMap tools...");

        string? answer = null;
        await foreach (var update in agentSessionService.ExecuteAgentSessionStreamAsync(
            messages,
            [typeof(SendCommandFunction)],
            model: OpenRouterModel.Gemini31FlashLitePreview,
            temperature: 0,
            maxIterations: 30,
            cancellationToken: cancellationToken))
        {
            yield return update;

            if (update.IsComplete && update.FinalResult?.Success == true)
                answer = update.FinalResult.Output;
        }

        if (answer != null)
        {
            var result = await aiDevsApiService.VerifyAsync("agent", new { answer }, cancellationToken);

            yield return StreamUpdate.Status("Agent completed task successfully");
            yield return StreamUpdate.Complete(result);
            yield break;
        }

        yield return StreamUpdate.Complete(SolutionResult.Fail("Agent failed to complete the task"));
    }
}
