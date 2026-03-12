using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Core.Interfaces;
using AiDevs.Core.Models;
using AiDevs.Infrastructure.Models;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task02;

public class Task02Solution(IAgentSessionService agentSessionService, IAiDevsApiService aiDevsApiService)
    : ITaskSolution
{
    public int TaskId => 2;

    public async IAsyncEnumerable<StreamUpdate> ExecuteStreamAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Load suspects from Task01
        yield return StreamUpdate.Status("Loading suspects data...");

        var suspectsJson = await File.ReadAllTextAsync("../AiDevs.Solutions/Task01/result.json", cancellationToken);

        // Load power plants
        yield return StreamUpdate.Status("Loading power plants data...");

        var powerPlantsJson = await File.ReadAllTextAsync("../AiDevs.Solutions/Task02/findhim_locations.json", cancellationToken);

        // Create prompt for LLM
        var systemPrompt = @"You are a detective tasked with finding a suspect who visited a power plant.

You have access to:
1. A list of suspects with their personal details
2. Power plant locations with their coordinates and codes
3. Tools to query where suspects were seen and their access levels

Your task:
1. For each suspect, get their location history using get_person_locations
2. Compare the locations with power plant coordinates (find very close matches - within ~0.01 degrees)
3. When you find someone who was near a power plant, get their access level using get_access_level
4. Return the answer only in this exact JSON format (do not include any additional text):
{
  ""name"": ""FirstName"",
  ""surname"": ""LastName"",
  ""accessLevel"": 3,
  ""powerPlant"": ""PWR1234PL""
}

Suspects:
" + suspectsJson + @"

Power Plants (with coordinates):
" + powerPlantsJson;

        var messages = new List<OpenRouterMessage>
        {
            new() { Role = "system", Content = systemPrompt },
            new() { Role = "user", Content = "Find the suspect who visited a power plant. Check each person's locations and match them with power plant coordinates." }
        };

        yield return StreamUpdate.Status("Starting agent session...");

        // Execute agent session with function handlers
        string? answer = null;
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

        if (answer != null)
        {
            yield return StreamUpdate.Status("Verifying answer...");

            // Submit to verify
            var answerObj = JsonSerializer.Deserialize<SuspectAnswer>(answer);
            if (answerObj != null)
            {
                var verifyResponse = await aiDevsApiService.VerifyAsync("findhim", answerObj, cancellationToken);
                yield return StreamUpdate.Complete(verifyResponse);
                yield break;
            }
        }

        yield return StreamUpdate.Complete(SolutionResult.Fail("Failed to find suspect"));
    }
}

public class SuspectAnswer
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("surname")]
    public string Surname { get; set; } = string.Empty;

    [JsonPropertyName("accessLevel")]
    public int AccessLevel { get; set; }

    [JsonPropertyName("powerPlant")]
    public string PowerPlant { get; set; } = string.Empty;
}