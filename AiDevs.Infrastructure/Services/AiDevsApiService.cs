using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Core.Models;
using Microsoft.Extensions.Configuration;

namespace AiDevs.Infrastructure.Services;

public class AiDevsApiService(HttpClient httpClient, IConfiguration configuration) : IAiDevsApiService
{
    private readonly string _apiKey = configuration["AiDevs:ApiKey"]
        ?? throw new InvalidOperationException("AiDevs API key not configured");
    private const string BaseUrl = "https://hub.ag3nts.org";

    public async Task<List<Coordinate>> GetLocationAsync(string name, string surname, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            apikey = _apiKey,
            name,
            surname
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync($"{BaseUrl}/api/location", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var locationResponse = JsonSerializer.Deserialize<List<Coordinate>>(responseJson);

        return locationResponse ?? [];
    }

    public async Task<int> GetAccessLevelAsync(string name, string surname, int birthYear, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            apikey = _apiKey,
            name,
            surname,
            birthYear
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync($"{BaseUrl}/api/accesslevel", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var accessResponse = JsonSerializer.Deserialize<AccessLevelResponse>(responseJson);

        return accessResponse?.AccessLevel ?? 0;
    }

    public async Task<SolutionResult> VerifyAsync(string task, object answer, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            apikey = _apiKey,
            task,
            answer
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await httpClient.PostAsync($"{BaseUrl}/verify", content, cancellationToken);
            var result = await response.Content.ReadAsStringAsync(cancellationToken);
            return !response.IsSuccessStatusCode 
                ? SolutionResult.Fail($"HTTP {response.StatusCode}: {result}") 
                : SolutionResult.Ok(result);
        }
        catch (Exception e)
        {
            return SolutionResult.Fail(e.Message);
        }
    }

    public async Task<StatsResponse> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var formData = new MultipartFormDataContent
        {
            { new StringContent(_apiKey), "key" }
        };

        var response = await httpClient.PostAsync($"{BaseUrl}/stats", formData, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var statsResponse = JsonSerializer.Deserialize<StatsResponse>(responseJson);

        return statsResponse;
    }

    public async Task<string> CheckPackageAsync(string packageId, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            apikey = _apiKey,
            action = "check",
            packageid = packageId
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync($"{BaseUrl}/api/packages", content, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        return responseContent;
    }

    public async Task<string> RedirectPackageAsync(string packageId, string destination, string code, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            apikey = _apiKey,
            action = "redirect",
            packageid = packageId,
            destination,
            code
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync($"{BaseUrl}/api/packages", content, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        return responseContent;
    }

    private class AccessLevelResponse
    {
        [JsonPropertyName("name")]
        public required string Name { get; init; }
        [JsonPropertyName("surname")]
        public required string Surname { get; init; }
        [JsonPropertyName("accessLevel")]
        public required int AccessLevel { get; init; }
    }

    public class StatsResponse
    {
        [JsonPropertyName("days")]
        public required List<string> Days { get; init; }
    }
}
