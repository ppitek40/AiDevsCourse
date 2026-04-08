using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task17;

[FunctionDefinition("generate_and_send_configs",
    "Generates unlock codes for each configuration entry, sends the complete configuration to the API, " +
    "runs the turbine check, and calls done to finalize. Returns the flag on success. " +
    "configs parameter must be a JSON array string where each entry has: " +
    "startDate (YYYY-MM-DD), startHour (HH:MM), pitchAngle (number), turbineMode ('production'|'idle'), windMs (number).")]
public class GenerateAndSendConfigsFunction(IAiDevsApiService apiService) : IFunctionHandler
{
    public Type ParametersType => typeof(GenerateAndSendConfigsParameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not GenerateAndSendConfigsParameters p)
            return JsonSerializer.Serialize(new { error = "Invalid parameters type" });

        List<TurbineConfigEntry>? configs;
        try
        {
            configs = JsonSerializer.Deserialize<List<TurbineConfigEntry>>(p.Configs);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Invalid configs JSON: {ex.Message}" });
        }

        if (configs == null || configs.Count == 0)
            return JsonSerializer.Serialize(new { error = "No configs provided" });
                                                 
        if (configs.Count > 15)
            return JsonSerializer.Serialize(new { error = "Too many configs provided" });

        // Step 1: Queue all unlock code generators in parallel
        var generatorTasks = configs
            .Select(c => SendCommand(new
            {
                action = "unlockCodeGenerator",
                startDate = c.StartDate,
                startHour = c.StartHour,
                windMs = c.WindMs,
                pitchAngle = c.PitchAngle
            }, cancellationToken))
            .ToList();

        await Task.WhenAll(generatorTasks);

        // Step 2: Poll getResult to collect all unlock codes, match by startDate+startHour
        var unlockCodesBySlot = new Dictionary<string, string>();
        var maxPolls = configs.Count * 8;

        while (unlockCodesBySlot.Count < configs.Count && maxPolls-- > 0)
        {
            await Task.Delay(200, cancellationToken);
            var result = await SendCommand(new { action = "getResult" }, cancellationToken);
            var json = TryParseJson(result);
            if (json == null) continue;

            if (!json.Value.TryGetProperty("sourceFunction", out var sf) ||
                sf.GetString() != "unlockCodeGenerator")
                continue;

            string? unlockCode = json.Value.TryGetProperty("unlockCode", out var uc)
                ? uc.GetString()
                : null;

            if (unlockCode == null) continue;

            // Match via signedParams (API echoes back the input params there)
            if (json.Value.TryGetProperty("signedParams", out var signed) &&
                signed.TryGetProperty("startDate", out var sd) &&
                signed.TryGetProperty("startHour", out var sh))
            {
                // startHour may be "HH:MM:SS" — normalize to "HH:MM"
                var startHour = sh.GetString() ?? "";
                if (startHour.Length > 5) startHour = startHour[..5];
                var key = $"{sd.GetString()}_{startHour}";
                unlockCodesBySlot[key] = unlockCode;
            }
            else
            {
                // Fallback: assign to the first unmatched slot
                var unmatched = configs.FirstOrDefault(c =>
                    !unlockCodesBySlot.ContainsKey($"{c.StartDate}_{c.StartHour}"));
                if (unmatched != null)
                    unlockCodesBySlot[$"{unmatched.StartDate}_{unmatched.StartHour}"] = unlockCode;
            }
        }

        // Step 3: Build final config array with unlock codes
        var finalConfigs = configs.Select(c => new
        {
            startDate = c.StartDate,
            startHour = c.StartHour,
            pitchAngle = c.PitchAngle,
            turbineMode = c.TurbineMode,
            unlockCode = unlockCodesBySlot.GetValueOrDefault($"{c.StartDate}_{c.StartHour}", "")
        }).ToList();

        // Step 4: Send full configuration
        var configResult = await SendCommand(new { action = "config", configs = finalConfigs }, cancellationToken);

        // Step 6: Finalize
        var doneResult = await SendCommand(new { action = "done" }, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            codesCollected = unlockCodesBySlot.Count,
            configsCount = configs.Count,
            config = configResult,
            done = doneResult
        });
    }

    private async Task<string> SendCommand(object command, CancellationToken cancellationToken)
    {
        var result = await apiService.VerifyAsync("windpower", command, cancellationToken);
        return result.Output ?? result.Error ?? "{}";
    }

    private static JsonElement? TryParseJson(string json)
    {
        try { return JsonDocument.Parse(json).RootElement; }
        catch { return null; }
    }
}

public class GenerateAndSendConfigsParameters
{
    [JsonPropertyName("configs")]
    [Parameter(
        "JSON array of configuration entries. Each object must have: " +
        "startDate (string, YYYY-MM-DD), startHour (string, HH:MM), " +
        "pitchAngle (number, degrees), turbineMode (string, 'production' or 'idle'), " +
        "windMs (number, wind speed m/s at that time slot). " +
        "Example: [{\"startDate\":\"2024-01-15\",\"startHour\":\"10:00\",\"pitchAngle\":45,\"turbineMode\":\"idle\",\"windMs\":25}]",
        required: true)]
    public string Configs { get; set; } = string.Empty;
}

public class TurbineConfigEntry
{
    [JsonPropertyName("startDate")]
    public string StartDate { get; set; } = string.Empty;

    [JsonPropertyName("startHour")]
    public string StartHour { get; set; } = string.Empty;

    [JsonPropertyName("pitchAngle")]
    public double PitchAngle { get; set; }

    [JsonPropertyName("turbineMode")]
    public string TurbineMode { get; set; } = string.Empty;

    [JsonPropertyName("windMs")]
    public double WindMs { get; set; }
}
