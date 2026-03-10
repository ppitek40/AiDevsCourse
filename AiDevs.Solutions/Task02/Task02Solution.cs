using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Core.Interfaces;
using AiDevs.Core.Models;
using AiDevs.Infrastructure.Models;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task02;

public class Task02Solution : ITaskSolution
{
    private readonly IAgentSessionService _agentSessionService;
    private readonly IAiDevsApiService _aiDevsApiService;

    public Task02Solution(IAgentSessionService agentSessionService, IAiDevsApiService aiDevsApiService)
    {
        _agentSessionService = agentSessionService;
        _aiDevsApiService = aiDevsApiService;
    }

    public int TaskId => 2;

    public async Task<SolutionResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Load suspects from Task01
        var suspectsJson = await File.ReadAllTextAsync("../AiDevs.Solutions/Task01/result.json", cancellationToken);

        // Load power plants
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

        // Execute agent session with function handlers
        var answer = await _agentSessionService.ExecuteAgentSessionAsync(
            messages,
            [typeof(GetPersonLocationsFunction), typeof(GetAccessLevelFunction)],
            model: OpenRouterModel.Claude35Sonnet,
            temperature: 0,
            maxIterations: 20,
            cancellationToken: cancellationToken
        );

        // Submit to verify
        var answerObj = JsonSerializer.Deserialize<SuspectAnswer>(answer);
        if (answerObj != null)
        {
            var verifyResponse = await _aiDevsApiService.VerifyAsync("findhim", answerObj, cancellationToken);
            return new SolutionResult
            {
                Success = true,
                Output = JsonSerializer.Serialize(verifyResponse)
            };
        }

        return new SolutionResult
        {
            Success = false,
            Error = "Failed to find suspect"
        };
    }
}

public class Person
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Surname")]
    public string Surname { get; set; } = string.Empty;

    [JsonPropertyName("BirthYear")]
    public int BirthYear { get; set; }

    [JsonPropertyName("BirthPlace")]
    public string? BirthPlace { get; set; }
}

public class PowerPlantsData
{
    [JsonPropertyName("power_plants")]
    public Dictionary<string, PowerPlant>? PowerPlants { get; set; }
}

public class PowerPlant
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

public class PowerPlantLocation
{
    public string City { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lon { get; set; }
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