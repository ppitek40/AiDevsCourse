using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task03;

[FunctionDefinition("check_package", "Check the status and location of a package by its ID")]
public class CheckPackageFunction(IAiDevsApiService aiDevsApiService) : IFunctionHandler
{
    public Type ParametersType => typeof(CheckPackageParameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not CheckPackageParameters p)
            return JsonSerializer.Serialize(new { error = "Invalid parameters type" });

        var content = await aiDevsApiService.CheckPackageAsync(p.PackageId, cancellationToken);
        return content;
    }
}

public class CheckPackageParameters
{
    [JsonPropertyName("package_id")]
    [Parameter("The package ID to check (e.g., PKG12345678)")]
    public string PackageId { get; set; } = string.Empty;
}
