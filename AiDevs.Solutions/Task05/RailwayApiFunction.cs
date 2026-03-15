using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using Microsoft.Extensions.Configuration;

namespace AiDevs.Solutions.Task05;

[FunctionDefinition("call_railway_api", 
    "Call the railway API with a specific action and optional parameters. ")]
public class RailwayApiFunction(HttpClient httpClient, IConfiguration configuration) : IFunctionHandler
{
    private readonly string _apiKey = configuration["AiDevs:ApiKey"]
        ?? throw new InvalidOperationException("AiDevs API key not configured");
    private const string BaseUrl = "https://hub.ag3nts.org";
    private const int MaxRetries = 5;

    public Type ParametersType => typeof(RailwayApiParameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not RailwayApiParameters p)
            return JsonSerializer.Serialize(new { error = "Invalid parameters type" });

        var attempt = 0;
        
        while (attempt < MaxRetries)
        {
            try
            {
                var payload = new Dictionary<string, object>
                {
                    { "apikey", _apiKey },
                    { "action", p.Action }
                };

                // Add optional parameters
                if (!string.IsNullOrEmpty(p.Code))
                    payload["code"] = p.Code;
                
                if (!string.IsNullOrEmpty(p.Route))
                    payload["route"] = p.Route;
                
                if (!string.IsNullOrEmpty(p.Status))
                    payload["status"] = p.Status;

                // Add any additional parameters from the raw JSON
                if (!string.IsNullOrEmpty(p.AdditionalParameters))
                {
                    try
                    {
                        var additionalParams = JsonSerializer.Deserialize<Dictionary<string, object>>(p.AdditionalParameters);
                        if (additionalParams != null)
                        {
                            foreach (var kvp in additionalParams)
                            {
                                payload[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                    catch
                    {
                        // Ignore JSON parsing errors for additional parameters
                    }
                }

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync($"{BaseUrl}/api/railway", content, cancellationToken);
                
                // Extract rate limit information
                var rateLimitInfo = "";
                if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remainingValues))
                {
                    rateLimitInfo += $" [Rate limit remaining: {remainingValues.FirstOrDefault()}]";
                }
                if (response.Headers.TryGetValues("X-RateLimit-Reset", out var resetValues))
                {
                    rateLimitInfo += $" [Reset at: {resetValues.FirstOrDefault()}]";
                }

                // Handle 503 Service Unavailable (simulated overload)
                if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    attempt++;
                    var waitTime = (int)Math.Pow(2, attempt);
                    
                    if (attempt >= MaxRetries)
                    {
                        return JsonSerializer.Serialize(new 
                        { 
                            error = $"Service unavailable after {MaxRetries} retries. The server is overloaded. Wait and try again later.",
                            statusCode = 503,
                            attempts = attempt
                        });
                    }
                    
                    // Wait before retry
                    await Task.Delay(TimeSpan.FromSeconds(waitTime), cancellationToken);
                    continue;
                }

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Serialize(new 
                    { 
                        error = $"API returned error status {response.StatusCode}",
                        statusCode = (int)response.StatusCode,
                        response = responseContent,
                        rateLimitInfo
                    });
                }

                // Try to parse as JSON and add rate limit info
                try
                {
                    var jsonResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    if (jsonResponse != null && !string.IsNullOrEmpty(rateLimitInfo))
                    {
                        jsonResponse["_rateLimitInfo"] = rateLimitInfo.Trim();
                        return JsonSerializer.Serialize(jsonResponse);
                    }
                }
                catch
                {
                    // If not JSON, return as-is
                }

                return responseContent + (string.IsNullOrEmpty(rateLimitInfo) ? "" : $"\n{rateLimitInfo}");
            }
            catch (Exception ex)
            {
                attempt++;
                if (attempt >= MaxRetries)
                {
                    return JsonSerializer.Serialize(new 
                    { 
                        error = $"Exception after {MaxRetries} attempts: {ex.Message}",
                        attempts = attempt
                    });
                }
                
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }

        return JsonSerializer.Serialize(new { error = "Maximum retries exceeded" });
    }
}

public class RailwayApiParameters
{
    [JsonPropertyName("action")]
    [Parameter("The action to perform (e.g., 'help', 'activate', 'status', etc.). Start with 'help' to get documentation.", required: true)]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    [Parameter("Railway code (e.g., 'X-01') - required for certain actions like activation", required: false)]
    public string? Code { get; set; }

    [JsonPropertyName("route")]
    [Parameter("Route identifier - may be required for some actions", required: false)]
    public string? Route { get; set; }

    [JsonPropertyName("status")]
    [Parameter("Status parameter - may be required for some actions", required: false)]
    public string? Status { get; set; }

    [JsonPropertyName("additionalParameters")]
    [Parameter("JSON string with any additional parameters the API might require based on the documentation", required: false)]
    public string? AdditionalParameters { get; set; }
}
