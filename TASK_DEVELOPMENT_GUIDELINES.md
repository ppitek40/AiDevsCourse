# Task Development Guidelines

This document outlines the architecture and patterns for implementing new tasks in the AiDevs solution.

## Core Architecture

### 1. Task Solution Interface

All tasks **must** implement `ITaskSolution` interface:

```csharp
public interface ITaskSolution
{
    int TaskId { get; }
    IAsyncEnumerable<StreamUpdate> ExecuteStreamAsync(CancellationToken cancellationToken = default);
}
```

**Key requirements:**
- `TaskId`: Unique identifier (1-25)
- `ExecuteStreamAsync`: Returns streaming progress updates for real-time UI feedback
- Use `[EnumeratorCancellation]` attribute on the cancellation token parameter

### 2. Folder Structure

Each task should be organized in its own folder under `AiDevs.Solutions/`:

```
AiDevs.Solutions/
└── TaskXX/
    ├── TaskXXSolution.cs      # Main solution implementation
    ├── [DataModels].cs         # Task-specific data models
    ├── [Functions].cs          # Function calling handlers (if needed)
    ├── [Helpers].cs            # Helper classes/utilities
    └── [data files]            # Input/output JSON, CSV, etc.
```

**Example from Task01:**
- `Task01Solution.cs` - Main solution
- `Person.cs` - Data model
- `CsvParser.cs` - Helper utility
- `people.csv`, `result.json` - Data files

**Example from Task02:**
- `Task02Solution.cs` - Main solution
- `GetPersonLocationsFunction.cs` - Function handler
- `GetAccessLevelFunction.cs` - Function handler
- `findhim_locations.json` - Data file

## Dependency Injection

Use **constructor injection** for all services:

```csharp
public class Task01Solution(
    IOpenRouterService openRouterService,
    IAiDevsApiService aiDevsApiService) : ITaskSolution
```

Common services:
- `IOpenRouterService` - Direct LLM interactions
- `IAgentSessionService` - Function calling / agent workflows
- `IAiDevsApiService` - Verify answers, get data from AiDevs API

## Streaming Updates Pattern

### StreamUpdate Types

Use static factory methods from `StreamUpdate` class:

```csharp
// Status message
yield return StreamUpdate.Status("Loading data...");

// LLM token streaming (for real-time token display)
yield return new StreamUpdate
{
    Type = StreamUpdateType.LLMToken,
    Content = token
};

// Completion with result
yield return StreamUpdate.Complete(SolutionResult.Ok("output"));
yield return StreamUpdate.Complete(SolutionResult.Fail("error"));
```

### StreamUpdateType Enum

- `Status` - Progress messages
- `LLMToken` - Individual LLM output tokens
- `ToolCall` - Function/tool being invoked
- `ToolResult` - Function/tool result
- `Complete` - Final result

### Best Practices for Streaming

1. **Start with status**: Always begin with a status update describing the first step
2. **Granular updates**: Provide updates at each major step
3. **Batch processing**: Report progress for batch operations
4. **LLM token streaming**: Stream individual tokens when using LLM directly
5. **Final result**: Always end with `StreamUpdate.Complete()`

**Example pattern:**
```csharp
yield return StreamUpdate.Status("Reading CSV file...");
// ... do work ...

yield return StreamUpdate.Status($"Loaded {count} items");

yield return StreamUpdate.Status("Processing with LLM...");
await foreach (var token in llmStream)
{
    yield return new StreamUpdate { Type = StreamUpdateType.LLMToken, Content = token };
}

yield return StreamUpdate.Status("Verifying answer...");
var result = await VerifyAsync(...);

yield return StreamUpdate.Complete(result);
```

## Two LLM Interaction Patterns

### Pattern 1: Direct LLM Streaming (Task01)

Use for **simple** LLM interactions without function calling:

```csharp
await foreach (var token in openRouterService.StreamChatAsync(
    messages,
    model: OpenRouterModel.Gpt4o,
    temperature: 0.3,
    cancellationToken: cancellationToken))
{
    fullResponse.Append(token);
    yield return new StreamUpdate
    {
        Type = StreamUpdateType.LLMToken,
        Content = token
    };
}
```

**When to use:**
- Simple prompt-response interactions
- Batch classification/tagging
- Text generation without tools

### Pattern 2: Agent Session with Function Calling (Task02)

Use for **complex** workflows requiring function/tool calling:

```csharp
await foreach (var update in agentSessionService.ExecuteAgentSessionStreamAsync(
    messages,
    [typeof(GetPersonLocationsFunction), typeof(GetAccessLevelFunction)],
    model: OpenRouterModel.Claude35Sonnet,
    temperature: 0,
    maxIterations: 20,
    cancellationToken: cancellationToken))
{
    yield return update;

    if (update.IsComplete && update.FinalResult?.Success == true)
        answer = update.FinalResult.Output;
}
```

**When to use:**
- Multi-step reasoning requiring external data
- Iterative problem-solving
- Tasks requiring API calls based on LLM decisions

## Function Calling Implementation

### 1. Define Function Handler

Implement `IFunctionHandler` interface:

```csharp
[FunctionDefinition("function_name", "Description of what this function does")]
public class MyFunction(IAiDevsApiService apiService) : IFunctionHandler
{
    public Type ParametersType => typeof(MyFunctionParameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not MyFunctionParameters p)
            return "Invalid parameters type";

        var result = await apiService.SomeMethod(p.Field1, p.Field2, cancellationToken);
        return JsonSerializer.Serialize(result);
    }
}
```

### 2. Define Parameters Class

```csharp
public class MyFunctionParameters
{
    [JsonPropertyName("field1")]
    [Parameter("Description of field1")]
    public string Field1 { get; set; } = string.Empty;

    [JsonPropertyName("field2")]
    [Parameter("Description of field2", required: false)]
    public int Field2 { get; set; }
}
```

**Key points:**
- Use `[FunctionDefinition]` attribute on class
- Use `[Parameter]` attribute on each property
- Always use `[JsonPropertyName]` for consistent serialization
- Return JSON-serialized strings from `ExecuteAsync`
- Handle invalid parameters gracefully

### 3. Register Functions

Pass function types to `ExecuteAgentSessionStreamAsync`:

```csharp
await foreach (var update in agentSessionService.ExecuteAgentSessionStreamAsync(
    messages,
    [typeof(GetPersonLocationsFunction), typeof(GetAccessLevelFunction)],
    ...))
```

## Data Models

### Task-Specific Models

Create models in the task folder:

```csharp
namespace AiDevs.Solutions.Task01;

public class Person
{
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    // ... other properties

    // Business logic methods
    public bool MeetsTransportCriteria()
    {
        return Gender == "M" && BirthYear >= 1986 && BirthYear <= 2006;
    }
}
```

### Answer Models

For API verification, create answer DTOs:

```csharp
public class SuspectAnswer
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("surname")]
    public string Surname { get; set; } = string.Empty;

    // ... other fields matching API requirements
}
```

## File Handling

### Reading Files

Use relative paths from `AppContext.BaseDirectory`:

```csharp
var csvPath = Path.Combine(AppContext.BaseDirectory,
    "../../../../AiDevs.Solutions/Task01/people.csv");
var data = await File.ReadAllTextAsync(csvPath, cancellationToken);
```

### Writing Results

Save intermediate results when needed:

```csharp
var resultPath = Path.Combine(AppContext.BaseDirectory,
    "../../../../AiDevs.Solutions/Task01/result.json");
await File.WriteAllTextAsync(resultPath,
    JsonSerializer.Serialize(data), cancellationToken);
```

## API Verification

Always verify answers with the AiDevs API:

```csharp
var result = await aiDevsApiService.VerifyAsync(
    "task_key",      // Task identifier for API
    answerObject,    // Answer data (will be serialized to JSON)
    cancellationToken);

yield return StreamUpdate.Complete(result);
```

**Optional: Add metadata:**
```csharp
yield return StreamUpdate.Complete(result.AddMetadata(new Dictionary<string, object>
{
    { "totalPeople", people.Count },
    { "filteredCount", filtered.Count }
}));
```

## LLM Prompt Design

### System Prompts

- Be specific about expected output format
- Provide examples when needed
- Request **JSON-only** responses when parsing results

**Example from Task02:**
```csharp
var systemPrompt = @"You are a detective tasked with finding a suspect.

You have access to:
1. A list of suspects
2. Tools to query data

Your task:
1. Check each suspect using get_person_locations
2. Compare with known locations
3. Return answer in this exact JSON format:
{
  ""name"": ""FirstName"",
  ""surname"": ""LastName""
}

Data:
" + dataJson;
```

### Response Parsing

Clean LLM responses before JSON parsing:

```csharp
var cleanResponse = fullResponse.ToString().Trim();
if (cleanResponse.StartsWith("```json"))
    cleanResponse = cleanResponse.Substring(7);
if (cleanResponse.StartsWith("```"))
    cleanResponse = cleanResponse.Substring(3);
if (cleanResponse.EndsWith("```"))
    cleanResponse = cleanResponse.Substring(0, cleanResponse.Length - 3);
cleanResponse = cleanResponse.Trim();

var result = JsonSerializer.Deserialize<MyType>(cleanResponse);
```

## Model Selection

Choose appropriate models based on task requirements:

- **GPT-4o**: Fast, good for classification/tagging (Task01)
- **Claude 3.5 Sonnet**: Better reasoning, function calling (Task02)
- **Temperature**:
  - `0` or `0.3` for deterministic tasks
  - Higher for creative tasks

## Error Handling

### Graceful Failures

Always return meaningful errors:

```csharp
if (answer == null)
{
    yield return StreamUpdate.Complete(
        SolutionResult.Fail("Failed to find suspect"));
    yield break;
}
```

### Parameter Validation

Validate function parameters:

```csharp
if (parameters is not MyFunctionParameters p)
    return "Invalid parameters type";

if (string.IsNullOrEmpty(p.RequiredField))
    return "RequiredField is missing";
```

## Testing Strategy

1. **Intermediate results**: Save intermediate data to JSON files for debugging
2. **Batch processing**: Process in smaller batches during development
3. **Status updates**: Add granular status updates to track progress
4. **Logging**: Use status updates as implicit logging

## Checklist for New Tasks

- [ ] Create `TaskXX` folder under `AiDevs.Solutions`
- [ ] Implement `ITaskSolution` interface
- [ ] Add constructor with required services (DI)
- [ ] Implement `ExecuteStreamAsync` with streaming updates
- [ ] Add status updates at each major step
- [ ] Create task-specific data models if needed
- [ ] Implement function handlers if using agent session
- [ ] Use appropriate LLM interaction pattern
- [ ] Parse and clean LLM responses properly
- [ ] Verify answer with `aiDevsApiService.VerifyAsync`
- [ ] Return `StreamUpdate.Complete()` with final result
- [ ] Test with sample data
- [ ] Save intermediate results for debugging

## Common Pitfalls to Avoid

1. **Missing stream updates**: Always provide status updates for user feedback
2. **Forgetting final complete**: Every execution path must end with `StreamUpdate.Complete()`
3. **Not cleaning LLM output**: Always strip markdown code blocks before parsing JSON
4. **Hardcoded paths**: Use `AppContext.BaseDirectory` for relative paths
5. **Missing cancellation support**: Always pass `cancellationToken` to async methods
6. **Incorrect JSON property names**: Use `[JsonPropertyName]` to match API requirements
7. **No error handling**: Handle null results and invalid parameters
8. **Wrong model selection**: Choose models appropriate for task complexity

## Example Implementations

Refer to:
- **Task01**: Simple LLM streaming with batch processing
- **Task02**: Agent session with function calling and multi-step reasoning
