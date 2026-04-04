using Microsoft.AspNetCore.Mvc;
using AiDevs.Infrastructure.Models;
using AiDevs.Infrastructure.FunctionCalling;
using System.Reflection;
using System.Text.Json;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Controllers;

[ApiController]
[Route("api/custom-llm")]
public class CustomLLMController(ILogger<CustomLLMController> logger, IAgentSessionService agentSessionService) : ControllerBase
{
 /// <summary>
    /// Execute a custom LLM call with specified model, tools, and parameters
    /// </summary>
    [HttpPost]
    public async Task CustomLlmCall(
        [FromBody] CustomLlmRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Executing custom LLM call with model {Model}", request.Model);

        // Set headers for Server-Sent Events
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        var messages = new List<OpenRouterMessage>();
        if (!string.IsNullOrEmpty(request.SystemMessage))
            messages.Add(new OpenRouterMessage { Role = "system", Content = request.SystemMessage });

        messages.Add(new OpenRouterMessage { Role = "user", Content = request.UserMessage });

        await foreach (var update in agentSessionService.ExecuteAgentSessionStreamAsync(
            messages,
            request.ToolTypes.Select(GetToolType).Where(t => t != null).Select(t => t!).ToList(),
            request.Model,
            request.Temperature,
            request.Iterations,
            null,
            cancellationToken: cancellationToken))
        {
            var json = JsonSerializer.Serialize(update);
            await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
    [HttpGet("models")]
    public IActionResult GetModels()
    {
        var models = Enum.GetValues<OpenRouterModel>()
            .Select(model => new
            {
                id = (int)model, 
                name = model.ToString()
            })
            .ToArray();

        return Ok(models);
    }

    [HttpGet("tools")]
    public IActionResult GetTools()
    {
        var functionHandlerType = typeof(IFunctionHandler);
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        var tools = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => functionHandlerType.IsAssignableFrom(type) &&
                          !type.IsInterface &&
                          !type.IsAbstract &&
                          type.GetCustomAttribute<FunctionDefinitionAttribute>() != null)
            .Select(type =>
            {
                var functionAttr = type.GetCustomAttribute<FunctionDefinitionAttribute>()!;

                return new
                {
                    name = functionAttr.Name
                };
            })
            .ToArray();

        return Ok(tools);
    }

    private Type? GetToolType(string toolName)
    {
        var functionHandlerType = typeof(IFunctionHandler);
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        return assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => functionHandlerType.IsAssignableFrom(type) &&
                          !type.IsInterface &&
                          !type.IsAbstract &&
                          type.GetCustomAttribute<FunctionDefinitionAttribute>()?.Name == toolName)
            .FirstOrDefault();
    }
}