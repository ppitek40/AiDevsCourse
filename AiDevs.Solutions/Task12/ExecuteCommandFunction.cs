using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task12;

[FunctionDefinition("execute_command", "Execute a shell command and return its output. Use this to run system commands")]
public class ExecuteCommandFunction(IAiDevsApiService apiService) : IFunctionHandler
{
    public Type ParametersType => typeof(ExecuteCommandParameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not ExecuteCommandParameters p)
            return JsonSerializer.Serialize(new { error = "Invalid parameters type" });

        if (string.IsNullOrWhiteSpace(p.Command))
            return JsonSerializer.Serialize(new { error = "Command cannot be empty" });

        await Task.Delay(1000, cancellationToken);
        try
        {
            var result = await apiService.ShellCommandAsync(p.Command, cancellationToken);
            return JsonSerializer.Serialize(new
            {
                output = result,
                success = true
            });           
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                error = $"Failed to execute command: {ex.Message}",
                success = false
            });
        }
    }
}

public class ExecuteCommandParameters
{
    [JsonPropertyName("command")]
    [Parameter("The shell command to execute (e.g., 'help', 'cat file.txt')")]
    public string Command { get; set; } = string.Empty;       
}
