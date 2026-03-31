using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AiDevs.Core.Interfaces;
using AiDevs.Core.Models;
using AiDevs.Infrastructure.Models;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task10;

/// <summary>
/// Solution for Task 10 - Drone navigation and image analysis
/// </summary>
public class Task10Solution(
    IAgentSessionService agentSessionService,
    HttpClient httpClient) : ITaskSolution
{
    public int TaskId => 10;

    public async IAsyncEnumerable<StreamUpdate> ExecuteStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return StreamUpdate.Status("Starting drone navigation task...");

        yield return StreamUpdate.Status("Fetching drone instructions from HTML...");
        var droneHtml = await httpClient.GetStringAsync("https://hub.ag3nts.org/dane/drone.html", cancellationToken);
        var strippedText = StripHtmlTags(droneHtml);

        var systemPrompt = $@"You are an operator of a combat drone in popular game. 
Your goal is to correctly send instructions to the drone to perform a specific task send by the user.
You have access to the documentation how to steer the drone and the tools to analyze the target and send instructions.
The documentation is complicated and contains many details. You don't need to use all the options.
If the set of instructions is not correct tool will return a detailed error message. You need to adjust your approach and try again.
If the tool will return {{FLG:...}} it means that the task is completed successfully.
{strippedText}";

        var messages = new List<OpenRouterMessage>
        {
            new() { Role = "system", Content = systemPrompt },
            new() { Role = "user", Content = "Steer the drone to target: PWR6132PL. And drop the bomb at the dam. ReadDroneImage function will return the position of the dam in the format rowxcol, for example 2x3." }
        };

        string? answer = null;

        yield return StreamUpdate.Status("Executing agent session with drone functions...");

        await foreach (var update in agentSessionService.ExecuteAgentSessionStreamAsync(
                           messages,
                           [typeof(ReadDroneImageFunction), typeof(SendDroneInstructionsFunction)],
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
            yield return StreamUpdate.Complete(
                SolutionResult.Fail("Failed to complete drone navigation task"));
            yield break;
        }


        yield return StreamUpdate.Complete(SolutionResult.Ok(answer));
    }

    private static string StripHtmlTags(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        // Remove script and style tags with their content
        html = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", string.Empty, RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<style[^>]*>[\s\S]*?</style>", string.Empty, RegexOptions.IgnoreCase);

        // Remove HTML tags
        html = Regex.Replace(html, @"<[^>]+>", string.Empty);

        // Decode HTML entities
        html = System.Net.WebUtility.HtmlDecode(html);

        // Clean up whitespace
        html = Regex.Replace(html, @"\s+", " ");
        html = html.Trim();

        return html;
    }
}
