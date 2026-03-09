# Task Solution Template

Use this template to quickly create new task solutions.

## Quick Start

1. **Create folder**: `AiDevs.Solutions/TaskXX/`
2. **Copy this template** to `TaskXXSolution.cs`
3. **Update namespace** and class name
4. **Implement logic** in `ExecuteAsync`
5. **Register** in `AiDevs/Program.cs`

## Template Code

```csharp
using AiDevs.Core.Interfaces;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.TaskXX; // Change XX to task number

/// <summary>
/// Solution for Task XX - [Brief description of what this task does]
/// </summary>
public class TaskXXSolution : ITaskSolution
{
    private readonly OpenRouterService _openRouterService;
    // Add other dependencies as needed

    public int TaskId => XX; // Change XX to task number

    public TaskXXSolution(OpenRouterService openRouterService)
    {
        _openRouterService = openRouterService;
    }

    public async Task<SolutionResult> ExecuteAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Implement your task logic here

            // Example: Call OpenRouter
            var response = await _openRouterService.CompleteAsync(
                prompt: $"Process this: {input}",
                model: "openai/gpt-3.5-turbo", // or gpt-4, claude, etc.
                temperature: 0.7,
                maxTokens: 1000,
                cancellationToken: cancellationToken
            );

            // Return success result
            return SolutionResult.Ok(response, new Dictionary<string, object>
            {
                { "taskId", TaskId },
                { "timestamp", DateTime.UtcNow }
            });
        }
        catch (Exception ex)
        {
            // Return error result
            return SolutionResult.Fail($"Task {TaskId} failed: {ex.Message}");
        }
    }
}
```

## Registration in Program.cs

Add this line in `AiDevs/Program.cs`:

```csharp
using AiDevs.Solutions.TaskXX; // Add at top

// In services registration:
builder.Services.AddTransient<ITaskSolution, TaskXXSolution>();
```

## Testing

```bash
# Build
dotnet build

# Run
cd AiDevs
dotnet run

# Test with curl
curl -X POST https://localhost:5001/api/solutions/XX \
  -H "Content-Type: application/json" \
  -d '{"input": "test input"}'
```

## Available OpenRouter Models

Common models you can use:

- `openai/gpt-3.5-turbo` - Fast and cheap
- `openai/gpt-4` - More capable
- `openai/gpt-4-turbo` - Larger context
- `anthropic/claude-3-opus` - Very capable
- `anthropic/claude-3-sonnet` - Balanced
- `google/gemini-pro` - Google's model

See more at: https://openrouter.ai/models

## Tips

### Working with Different Input Types

```csharp
// Parse JSON input
var data = JsonSerializer.Deserialize<MyInputModel>(input);

// Handle multiple formats
if (input.StartsWith("{"))
{
    // JSON input
}
else
{
    // Plain text input
}
```

### Building Complex Prompts

```csharp
var prompt = $@"
You are an expert in [domain].

Task: [task description]

Input: {input}

Instructions:
1. [Step 1]
2. [Step 2]

Output format: [expected format]
";
```

### Using Chat History

```csharp
var messages = new List<OpenRouterMessage>
{
    new() { Role = "system", Content = "You are a helpful assistant" },
    new() { Role = "user", Content = "First question" },
    new() { Role = "assistant", Content = "First answer" },
    new() { Role = "user", Content = input }
};

var response = await _openRouterService.ChatAsync(messages);
```

### Adding Custom Services

If you need additional services (HTTP client, database, etc.):

1. Create service in `AiDevs.Infrastructure/Services/`
2. Register in `Program.cs`
3. Inject via constructor

```csharp
public TaskXXSolution(
    OpenRouterService openRouterService,
    IMyCustomService customService)
{
    _openRouterService = openRouterService;
    _customService = customService;
}
```
