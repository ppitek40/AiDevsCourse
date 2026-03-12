using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task03;

[FunctionDefinition("redirect_package", "Redirect a package to a new destination using a security code. This function returns a confirmation code that should be returned to the operator.")]
public class RedirectPackageFunction(IAiDevsApiService aiDevsApiService) : IFunctionHandler
{
    public Type ParametersType => typeof(RedirectPackageParameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not RedirectPackageParameters p)
            return JsonSerializer.Serialize(new { error = "Invalid parameters type" });

        if (string.IsNullOrEmpty(p.Code))
            return JsonSerializer.Serialize(new { error = "Security code is required" });

        var content = await aiDevsApiService.RedirectPackageAsync(p.PackageId, p.Destination, p.Code, cancellationToken);
        return content;
    }
}

public class RedirectPackageParameters
{
    [JsonPropertyName("package_id")]
    [Parameter("The package ID to redirect (e.g., PKG12345678)")]
    public string PackageId { get; set; } = string.Empty;

    [JsonPropertyName("destination")]
    [Parameter("The destination code (e.g., PWR6132PL)")]
    public string Destination { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    [Parameter("The security code provided by the operator")]
    public string Code { get; set; } = string.Empty;
}
