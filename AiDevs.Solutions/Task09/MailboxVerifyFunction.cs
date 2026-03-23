using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task09;

[FunctionDefinition("mailbox_verify", "Verify extracted data from an emails.")]
public class MailboxVerifyFunction(IAiDevsApiService aiDevsApiService) : IFunctionHandler
{
    public Type ParametersType => typeof(MailboxVerifyParameters);

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not MailboxVerifyParameters p)
            return JsonSerializer.Serialize(new { error = "Invalid parameters type" });

        var result = await aiDevsApiService.VerifyAsync("mailbox", p, cancellationToken);
        return JsonSerializer.Serialize(result);
    }
}   

public class MailboxVerifyParameters
{
    [JsonPropertyName("password")]
    [Parameter("Password")]
    public string Password { get; set; } = "";

    [JsonPropertyName("date")]
    [Parameter("Date of the email")]
    public string Date { get; set; } = "";

    [JsonPropertyName("confirmation_code")]
    [Parameter("Confirmation code from the email")]
    public string ConfirmationCode { get; set; } = "";
}