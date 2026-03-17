using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task05;

[FunctionDefinition("call_railway_api", 
    "Call the railway API with a specific command. Command should be an JSON object as string. Start with action = help to get documentation.")]
public class RailwayApiFunction(IAiDevsApiService apiService) : IFunctionHandler
{
    private const int MaxRetries = 5;

    public Type ParametersType => typeof(RailwayApiParameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not RailwayApiParameters p)
            return JsonSerializer.Serialize(new { error = "Invalid parameters type" });

        var attempt = 0;
        
        var command = JsonSerializer.Deserialize<object>(p.Command);
        while (attempt < MaxRetries)
        {
            try
            {
                var response = await apiService.VerifyRawAsync("railway", command, cancellationToken);
                // Extract rate limit information
                var rateLimitRemaining = "";
                var rateLimitReset = "";
                if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remainingValues))
                    rateLimitRemaining = remainingValues.FirstOrDefault();
                if (response.Headers.TryGetValues("X-RateLimit-Reset", out var resetValues))
                    rateLimitReset = resetValues.FirstOrDefault();

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var dateTimeFromEpoch = DateTimeOffset.FromUnixTimeSeconds(long.Parse(rateLimitReset)).DateTime;
                    var timeRemaining = dateTimeFromEpoch - DateTime.UtcNow;
                    if (timeRemaining.TotalSeconds > 0)
                        await Task.Delay(TimeSpan.FromSeconds(timeRemaining.TotalSeconds), cancellationToken);
                    attempt++;
                    continue;
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
                        response = responseContent
                    });
                }

                return JsonSerializer.Serialize(new 
                { 
                    statusCode = 200,
                    response = responseContent
                });
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
    [JsonPropertyName("command")]
    [Parameter("the command to execute (e.g {\"command\": \"help\"})", required: true)]
    public string Command { get; set; } = string.Empty;
}
