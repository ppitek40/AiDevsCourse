using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task07;

[FunctionDefinition("rotate", "Rotate a cell by sending rotate command to the API")]
public class RotateFunction(IAiDevsApiService apiService) : IFunctionHandler
{
    public Type ParametersType => typeof(RotateParameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not RotateParameters p)
            return JsonSerializer.Serialize(new { error = "Invalid parameters type" });

        var command = new { rotate = p.CellName };
        var result = await apiService.VerifyAsync("electricity", command, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            response = result.Output
        });
    }
}

public class RotateParameters
{
    [JsonPropertyName("cell_name")]
    [Parameter("The name of the cell to rotate (e.g., '2x3') Cell are numbered from 1 to 3", required: true)]
    public string CellName { get; set; } = string.Empty;
}
