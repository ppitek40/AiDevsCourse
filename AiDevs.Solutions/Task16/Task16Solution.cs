using System.Runtime.CompilerServices;
using AiDevs.Core.Interfaces;
using AiDevs.Core.Models;
using AiDevs.Infrastructure.Models;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task16;

/// <summary>
/// Solution for Task 16 - Agent session with UseApiFunction
/// </summary>
public class Task16Solution(
    IAgentSessionService agentSessionService,
    IAiDevsApiService aiDevsApiService) : ITaskSolution
{
    public int TaskId => 16;

    public async IAsyncEnumerable<StreamUpdate> ExecuteStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return StreamUpdate.Status("Starting Task 16...");

        var systemPrompt = """
            You are an investigative agent with access to an API.
            Use the use_api function to query available endpoints and gather information needed to solve the task.
            
            You also have access to a website https://oko.ag3nts.org/
            It is the operator panel of the system where you get all needed information.
            USE IT ONLY READ-ONLY. For changes use the API.
            Changing anything in the panel will result in a ban and failing the task.
            You can check the page by playwright mcp tools, if the login screen appear, user will provide credentials directly to the form.
            
            When you have found the answer, submit it using the use_api function with the appropriate endpoint.
            The task will be completed when the tool return the {FLG:...} flag.
            
            Read carefully the Tool responses, it will guide you if something would be wrong.
            """;

        var userPrompt = "Start the investigation. Use the API to do these three things:" +
            "1. Change the classification of the Skolwin city incident from Pojazdy i ludzie to Zwierzęta" +
            "2. On the task list find task related to Skolwin city and change its status to Done in the text write that there were seen some animals (e.g beavers)" +
            "3. Create new report about the incident in different city (e.g Komarowo) write that there were seen some people." +
            "4. If you finish all the tasks send the action = done";

        var messages = new List<OpenRouterMessage>
        {
            new() { Role = "system", Content = systemPrompt },
            new() { Role = "user", Content = userPrompt }
        };

        string? answer = null;

        yield return StreamUpdate.Status("Running agent session...");

        await foreach (var update in agentSessionService.ExecuteAgentSessionStreamAsync(
            messages,
            [typeof(UseApiFunction)],
            model: OpenRouterModel.Claude45Sonnet,
            temperature: 0,
            maxIterations: 40,
            mcpClient: McpClientService.CreatePlaywright(),
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

        var result = await aiDevsApiService.VerifyAsync("task16", answer, cancellationToken);
        yield return StreamUpdate.Complete(result);
    }
}
