# AI DEVS Course Solutions

A scalable .NET 10 solution for implementing and executing AI DEVS course tasks via REST API.

## Project Structure

```
AiDevs/
├── AiDevs/                        # ASP.NET Core Web API
│   ├── Controllers/
│   │   └── SolutionsController.cs # Main API endpoint
│   ├── Models/
│   │   └── ExecuteSolutionRequest.cs
│   ├── Program.cs                 # App configuration & DI setup
│   └── appsettings.json          # Configuration (add your API key here)
│
├── AiDevs.Core/                   # Core domain interfaces & models
│   ├── Interfaces/
│   │   └── ITaskSolution.cs      # Interface all solutions implement
│   └── Models/
│       └── SolutionResult.cs     # Standard result format
│
├── AiDevs.Infrastructure/         # External services (OpenRouter)
│   ├── Services/
│   │   └── OpenRouterService.cs  # AI API integration
│   └── Models/
│       └── OpenRouterModels.cs   # DTOs for OpenRouter API
│
└── AiDevs.Solutions/              # Task implementations
    └── Task01/
        └── Task01Solution.cs      # Example task implementation
```

## Setup

### 1. Configure OpenRouter API Key

Edit `AiDevs/appsettings.json` or `AiDevs/appsettings.Development.json`:

```json
{
  "OpenRouter": {
    "ApiKey": "YOUR_ACTUAL_API_KEY"
  }
}
```

### 2. Restore and Build

```bash
dotnet restore
dotnet build
```

### 3. Run the API

```bash
cd AiDevs
dotnet run
```

The API will start at `https://localhost:5001` (and `http://localhost:5000`)

### 4. Access Swagger UI

Navigate to: `https://localhost:5001/swagger`

## API Usage

### Execute a Task Solution

```http
POST /api/solutions/{taskId}
Content-Type: application/json

{
  "input": "your task-specific input data"
}
```

**Example:**

```bash
curl -X POST https://localhost:5001/api/solutions/1 \
  -H "Content-Type: application/json" \
  -d '{"input": "Hello, AI!"}'
```

**Response:**

```json
{
  "success": true,
  "output": "AI-generated response",
  "error": null,
  "metadata": {
    "taskId": 1,
    "inputLength": 10
  }
}
```

### List Available Solutions

```http
GET /api/solutions
```

**Response:**

```json
{
  "tasks": [
    {
      "taskId": 1,
      "type": "Task01Solution"
    }
  ]
}
```

## Adding New Task Solutions

### Step 1: Create Task Folder

```bash
cd AiDevs.Solutions
mkdir Task02
```

### Step 2: Implement ITaskSolution

Create `AiDevs.Solutions/Task02/Task02Solution.cs`:

```csharp
using AiDevs.Core.Interfaces;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task02;

public class Task02Solution : ITaskSolution
{
    private readonly OpenRouterService _openRouterService;

    public int TaskId => 2;

    public Task02Solution(OpenRouterService openRouterService)
    {
        _openRouterService = openRouterService;
    }

    public async Task<SolutionResult> ExecuteAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Your task logic here
            var result = await _openRouterService.CompleteAsync(
                prompt: $"Task 2 logic: {input}",
                model: "openai/gpt-3.5-turbo",
                cancellationToken: cancellationToken
            );

            return SolutionResult.Ok(result);
        }
        catch (Exception ex)
        {
            return SolutionResult.Fail(ex.Message);
        }
    }
}
```

### Step 3: Register in Program.cs

Edit `AiDevs/Program.cs`:

```csharp
using AiDevs.Solutions.Task02; // Add this

// In the services section:
builder.Services.AddTransient<ITaskSolution, Task02Solution>();
```

### Step 4: Test

```bash
dotnet run
```

```bash
curl -X POST https://localhost:5001/api/solutions/2 \
  -H "Content-Type: application/json" \
  -d '{"input": "test"}'
```

## OpenRouter Service Usage

The `OpenRouterService` provides two methods:

### Simple Completion

```csharp
var response = await _openRouterService.CompleteAsync(
    prompt: "Your prompt",
    model: "openai/gpt-4",
    temperature: 0.7,
    maxTokens: 1000
);
```

### Chat with Message History

```csharp
var messages = new List<OpenRouterMessage>
{
    new() { Role = "system", Content = "You are a helpful assistant" },
    new() { Role = "user", Content = "Hello!" }
};

var response = await _openRouterService.ChatAsync(
    messages: messages,
    model: "openai/gpt-4"
);
```

## Development Tips

- Each task is **completely isolated** in its own folder
- All tasks implement the same `ITaskSolution` interface
- Use dependency injection to get `OpenRouterService` or other services
- Return structured `SolutionResult` for consistent API responses
- Task IDs should be unique (1-25+)
- Add logging via `ILogger<T>` if needed

## Future Enhancements

- [ ] Frontend application for navigating solutions
- [ ] Database for storing task results
- [ ] Authentication & authorization
- [ ] Rate limiting
- [ ] Caching for expensive AI calls
- [ ] Background job processing for long-running tasks

## License

This project is for educational purposes (AI DEVS course).
