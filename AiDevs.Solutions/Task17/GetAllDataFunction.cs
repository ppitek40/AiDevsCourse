using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task17;

[FunctionDefinition("get_all_data", "Initializes the wind turbine service window and fetches all required data: weather forecast, power plant energy requirements, and turbine documentation. Returns all data needed to build a configuration.")]
public class GetAllDataFunction(IAiDevsApiService apiService) : IFunctionHandler
{
    public Type ParametersType => typeof(GetAllDataParameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        // Step 1: Start the service window
        var startResult = await SendCommand(new { action = "start" }, cancellationToken);

        // Step 2: Queue async data requests and fetch documentation (returned directly) in parallel
        var weatherTask = SendCommand(new { action = "get", param = "weather" }, cancellationToken);
        var powerplantTask = SendCommand(new { action = "get", param = "powerplantcheck" }, cancellationToken);
        var documentationTask = SendCommand(new { action = "get", param = "documentation" }, cancellationToken);
        var turbineCheckTask = SendCommand(new { action = "get", param = "turbinecheck" }, cancellationToken);


        await Task.WhenAll(weatherTask, powerplantTask, documentationTask, turbineCheckTask);

        await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
        // Step 3: Poll getResult until both async responses arrive
        string? weatherData = null;
        string? powerplantData = null;
        string? turbineCheckData = null;
        var maxPolls = 40;

        while ((weatherData == null || powerplantData == null || turbineCheckData == null) && maxPolls-- > 0)
        {
            await Task.Delay(500, cancellationToken);
            var result = await SendCommand(new { action = "getResult" }, cancellationToken);
            var json = TryParseJson(result);
            if (json == null) continue;

            if (json.Value.TryGetProperty("sourceFunction", out var sf))
            {
                var sourceFn = sf.GetString();
                if (sourceFn == "weather" && weatherData == null) weatherData = FilterWeather(result);
                else if (sourceFn == "powerplantcheck" && powerplantData == null) powerplantData = result;
                else if (sourceFn == "turbinecheck" && turbineCheckData == null) turbineCheckData = result;
            }
        }

        return JsonSerializer.Serialize(new
        {
            start = startResult,
            weather = weatherData ?? "not received",
            powerplantcheck = powerplantData ?? "not received",
            turbinecheck = turbineCheckData ?? "not received",
            documentation = documentationTask.Result
        });
    }

    private async Task<string> SendCommand(object command, CancellationToken cancellationToken)
    {
        var result = await apiService.VerifyAsync("windpower", command, cancellationToken);
        return result.Output ?? result.Error ?? "{}";
    }

    private static string FilterWeather(string rawJson)
    {
        var response = JsonSerializer.Deserialize<WeatherResponse>(rawJson);
        if (response == null) return rawJson;

        response.Forecast = response.Forecast.Where(s => s.WindMs > 4.0).ToList();

        return JsonSerializer.Serialize(response);
    }

    private static JsonElement? TryParseJson(string json)
    {
        try { return JsonDocument.Parse(json).RootElement; }
        catch { return null; }
    }
}

public class GetAllDataParameters
{
    // No parameters required
}

public class WeatherResponse
{
    [JsonPropertyName("code")] public int Code { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("sourceFunction")] public string? SourceFunction { get; set; }
    [JsonPropertyName("intervalHours")] public int IntervalHours { get; set; }
    [JsonPropertyName("forecastDays")] public int ForecastDays { get; set; }
    [JsonPropertyName("unit")] public JsonElement? Unit { get; set; }
    [JsonPropertyName("forecast")] public List<WeatherSlot> Forecast { get; set; } = [];
}

public class WeatherSlot
{
    [JsonPropertyName("timestamp")] public string? Timestamp { get; set; }
    [JsonPropertyName("windMs")] public double WindMs { get; set; }
    [JsonPropertyName("precipitationMm")] public double PrecipitationMm { get; set; }
    [JsonPropertyName("temperatureC")] public double TemperatureC { get; set; }
}
