using System.Text.Json.Serialization;
using AiDevs.Core.Models;
using AiDevs.Infrastructure.Models;
using AiDevs.Infrastructure.Services;
using AiDevs.Solutions.Task14;
using Microsoft.AspNetCore.Mvc;

namespace AiDevs.Controllers;

[ApiController]
[Route("api/task14")]
public class Task14Controller(
    IAgentSessionService agentSessionService,
    IProxyEventAggregator eventAggregator,
    ILogger<Task14Controller> logger) : ControllerBase
{
    /// <summary>
    /// Endpoint 1 - TODO: describe purpose
    /// </summary>
    [HttpPost("get-cities-for-item")]
    public async Task<IActionResult> GetCitiesForItem([FromBody]ApiParams parameters, CancellationToken cancellationToken)
    {
        eventAggregator.Publish(StreamUpdate.Status("ActionOne received request"));

        var systemPrompt = "You are a helpful assistant that helps find user a cities where specific item can be found." +
            "You have access to the tools which helps you with this." +
            "One tool will help you with searching for the item in the database." +
            "The other tool will help locate the city where item can be found." +
            "If search tool will return more than one result, ask for clarifications and only when you are sure which item it is. Locate it and return to the user." +
            "If something will fail or will be not clear, describe the issue, but you cant exceed 500 characters.";

        var messages = new List<OpenRouterMessage>
        {
            new() { Role = "system", Content = systemPrompt },
            new() { Role = "user", Content = parameters.Params.ItemName }
        };

        string? result = null;
        try
        {
            await foreach (var update in agentSessionService.ExecuteAgentSessionStreamAsync(
                messages,
                [typeof(GetCitiesForItemCode), typeof(SearchItemsFunction)],
                model: OpenRouterModel.Claude45Sonnet,
                temperature: 0,
                maxIterations: 10,
                cancellationToken: cancellationToken))
            {
                if (update.IsComplete && update.FinalResult?.Success == true)
                    result = update.FinalResult.Output;

                if (update.Type != StreamUpdateType.LLMToken && update.Type != StreamUpdateType.Complete)
                    eventAggregator.Publish(update);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in Task14 ActionOne");
            eventAggregator.Publish(StreamUpdate.Complete(SolutionResult.Fail(ex.Message)));
            return Ok(new { error = ex.Message });
        }

        logger.LogInformation("Task14 ActionOne completed: {Result}", result);
        return Ok(new { result });
    }

    /// <summary>
    /// Endpoint 2 - TODO: describe purpose
    /// </summary>
    [HttpPost("action-two")]
    public async Task<IActionResult> ActionTwo(CancellationToken cancellationToken)
    {
        eventAggregator.Publish(StreamUpdate.Status("ActionTwo received request"));

        var systemPrompt = "TODO: define system prompt for action two";

        var messages = new List<OpenRouterMessage>
        {
            new() { Role = "system", Content = systemPrompt },
            new() { Role = "user", Content = "TODO: define user message" }
        };

        string? result = null;
        try
        {
            await foreach (var update in agentSessionService.ExecuteAgentSessionStreamAsync(
                messages,
                [typeof(GetCitiesForItemCode), typeof(SearchItemsFunction)],
                model: OpenRouterModel.Claude45Sonnet,
                temperature: 0,
                maxIterations: 10,
                cancellationToken: cancellationToken))
            {
                if (update.IsComplete && update.FinalResult?.Success == true)
                    result = update.FinalResult.Output;

                if (update.Type != StreamUpdateType.LLMToken && update.Type != StreamUpdateType.Complete)
                    eventAggregator.Publish(update);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in Task14 ActionTwo");
            eventAggregator.Publish(StreamUpdate.Complete(SolutionResult.Fail(ex.Message)));
            return Ok(new { error = ex.Message });
        }

        logger.LogInformation("Task14 ActionTwo completed: {Result}", result);
        return Ok(new { result });
    }
}

public class ApiParams
{
    [JsonPropertyName("params")]
    public ParamsRequest Params { get; set; }
}

public class ParamsRequest
{
    [JsonPropertyName("item_name")]
    public string ItemName { get; set; }
}
