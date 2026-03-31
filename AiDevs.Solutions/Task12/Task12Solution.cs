using System.Runtime.CompilerServices;
using AiDevs.Core.Interfaces;
using AiDevs.Core.Models;
using AiDevs.Infrastructure.Models;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task12;

/// <summary>
/// Solution for Task 12 - AI agent with ExecuteCommand tool
/// Uses an agent with the ability to execute shell commands to solve tasks autonomously
/// </summary>
public class Task12Solution(
    IAgentSessionService agentSessionService,
    IAiDevsApiService aiDevsApiService) : ITaskSolution
{
    public int TaskId => 12;

    public async IAsyncEnumerable<StreamUpdate> ExecuteStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return StreamUpdate.Status("Starting Task 12 with AI agent and ExecuteCommand tool...");

        // Create system prompt for the agent
        var systemPrompt = @"You are a Linux system troubleshooting agent.

ENVIRONMENT:
- Limited Linux system with read-only disk (except /opt/firmware volume which is writable)
- You operate as a regular user (not root)
- You have access to the ExecuteCommand tool to run shell commands

SECURITY RULES (CRITICAL - VIOLATION WILL BLOCK API ACCESS):
- DO NOT access /etc, /root, or /proc/ directories
- If you find a .gitignore file in any directory, respect it - DO NOT touch files/directories listed there
- Only work within allowed areas

STRATEGY:
- Always start with 'help' command to gather information
- Adjust configuration based on error feedback
- If you mess up the system too much, use the reboot function
- Make sure to modify exactly the line you want to modify in files

Return only the ECCS code once you successfully extract it.";

        var messages = new List<OpenRouterMessage>
        {
            new() { Role = "system", Content = systemPrompt },
            new() { Role = "user", Content = @"Your task is to run firmware software located at /opt/firmware/cooler/cooler.bin and extract a special code in format: ECCS-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx. 
1. Try to run the binary: /opt/firmware/cooler/cooler.bin
2. Obtain the access password for this application (it's stored in several places in the system)
3. Figure out how to reconfigure the software (settings.ini) to make it work correctly
4. Extract the ECCS code from the output when it runs successfully" }
        };

        yield return StreamUpdate.Status("Starting AI agent session with ExecuteCommand tool...");

        string? answer = null;
        await foreach (var update in agentSessionService.ExecuteAgentSessionStreamAsync(
            messages,
            [typeof(ExecuteCommandFunction)],
            model: OpenRouterModel.Claude45Sonnet,
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
            var result = await aiDevsApiService.VerifyAsync("firmware", new { confirmation = answer }, cancellationToken);

            yield return StreamUpdate.Status("Agent completed task successfully");
            yield return StreamUpdate.Complete(result);
            yield break;
        }

        yield return StreamUpdate.Complete(SolutionResult.Fail("Agent failed to complete the task"));
    }
}
