using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using AiDevs.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace AiDevs.Solutions.Task06;

[FunctionDefinition("verify_prompt", "Verify a prompt to categorize items")]
public class VerifyPromptFunction(IAiDevsApiService apiService, IConfiguration configuration) : IFunctionHandler
{
    private const int MaxRetries = 5;

    public Type ParametersType => typeof(VerifyPromptParameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not VerifyPromptParameters p)
            return JsonSerializer.Serialize(new { error = "Invalid parameters type" });

        var resetResponse = await apiService.VerifyAsync("categorize", new { prompt = "reset" }, cancellationToken);
        if (!resetResponse.Success)
            return JsonSerializer.Serialize(new { error = $"Failed to reset API: {resetResponse.Error}" });

        // Fetch CSV from URL
        var csvUrl = $"https://hub.ag3nts.org/data/{configuration["AiDevs:ApiKey"]}/categorize.csv";
        var records = new List<CsvRecord>();

        using var httpClient = new HttpClient();
        var csvContent = await httpClient.GetStringAsync(csvUrl, cancellationToken);

        foreach (var line in csvContent.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("code,description")) continue; // Skip header and empty lines
            var parts = line.Split(',', 2);
            if (parts.Length == 2)
            {
                records.Add(new CsvRecord { Id = parts[0], Description = parts[1].Trim('"') });
            }
        }

        records = records.OrderBy(r => r.Order).ToList();
        var results = new List<object>();

        int[] chars = [10, 4, 9, 2, 1, 3, 7, 5, 8, 6];
        // Process each record
        for (int i = 0; i < records.Count; i++)
        {
            var record = records[chars[i]-1];
            var promptWithData = p.Prompt
                .Replace("{ID}", record.Id)
                .Replace("{DESCRIPTION}", record.Description);

            var command = new { prompt = promptWithData };

            var response = await apiService.VerifyRawAsync("categorize", command, cancellationToken);

            results.Add(new
            {
                id = record.Id,
                statusCode = (int)response.StatusCode,
                response = await response.Content.ReadAsStringAsync(cancellationToken)
            });
        }

        return JsonSerializer.Serialize(new { results });
    }
}

public class VerifyPromptParameters
{
    [JsonPropertyName("prompt")]
    [Parameter("The prompt to verify with {ID} and {DESCRIPTION} tokens to be replaced", required: true)]
    public string Prompt { get; set; } = string.Empty;
}

public class CsvRecord
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? Order { get; set; }
}
