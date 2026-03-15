using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Core.Interfaces;
using AiDevs.Core.Models;
using AiDevs.Infrastructure.Models;
using AiDevs.Infrastructure.Services;
using AiDevs.Tools;

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
2. Power plant locations with their codes
3. Tools to query where suspects were seen and their access level
4. A tool to get the exact location of a city where power plants are located

Your task:
1. For each power plant, use get_exact_location_of_the_city to get its exact coordinates
2. For each suspect, get their location history using get_person_locations
3. Compare exact coordinates: check if any suspect's location matches a power plant location (approximate less than 0.001)
4. When you find someone who was at a power plant location, get their access level using get_access_level
5. Get the code of the power plant they were at
6. Return the answer only in this exact JSON format (do not include any additional text):
{
  ""name"": ""FirstName"",
  ""surname"": ""LastName"",
  ""accessLevel"": 3,
  ""powerPlant"": ""PWR1234PL""
}
Suspects:
" + suspectsJson + @"

Power Plants:
" + powerPlantsJson + @"
!!! ANSWER ONLY IN JSON FORMAT!!!";

        var messages = new List<OpenRouterMessage>
        {
            new() { Role = "system", Content = systemPrompt },
            new() { Role = "user", Content = "Find the suspect who visited a power plant. First get exact coordinates for each power plant using get_coordinates_of_the_city. Then check each person's locations and compare with exact power plant coordinates." }
        };

        yield return StreamUpdate.Status("Starting agent session...");

        // Execute agent session with function handlers
        string? answer = null;
        await foreach (var update in agentSessionService.ExecuteAgentSessionStreamAsync(
            messages,
            [typeof(GetPersonLocationsFunction), typeof(GetAccessLevelFunction), typeof(GetCoordinatesOfTheCityFunction)],
            model: OpenRouterModel.Claude45Sonnet,
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
            var answerObj = JsonSerializer.Deserialize<SuspectAnswer>(ResponseStripper.Strip(answer));
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
    [Description("First name of the suspect")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("surname")]
    [Description("Last name of the suspect")]
    public string Surname { get; set; } = string.Empty;

    [JsonPropertyName("accessLevel")]
    [Description("Access level of the suspect")]
    public int AccessLevel { get; set; }

    [JsonPropertyName("powerPlant")]
    [Description("Code of the power plant")]
    public string PowerPlant { get; set; } = string.Empty;
}