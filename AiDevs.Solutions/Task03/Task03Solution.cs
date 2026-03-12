using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using AiDevs.Core.Interfaces;
using AiDevs.Core.Models;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task03;

public class Task03Solution(
    IAiDevsApiService aiDevsApiService,
    IProxyEventAggregator eventAggregator) : ITaskSolution
{
    public int TaskId => 3;

    public async IAsyncEnumerable<StreamUpdate> ExecuteStreamAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return StreamUpdate.Status("Preparing to submit proxy endpoint...");

        // You need to update this URL with your actual public endpoint
        // After running the app locally, expose it with ngrok or similar tool
        // Example: ngrok http https://localhost:5001
        const string publicUrl = "https://ansto-85-221-137-135.a.free.pinggy.link";
        var sessionId = Guid.NewGuid().ToString();

        yield return StreamUpdate.Status($"Submitting endpoint: {publicUrl}");
        yield return StreamUpdate.Status($"Session ID: {sessionId}");

        var answer = new ProxyAnswer
        {
            Url = publicUrl,
            SessionId = sessionId
        };

        yield return StreamUpdate.Status("Waiting for proxy requests...");

        // Stream proxy events while waiting for verification
        var verifyTask = await aiDevsApiService.VerifyAsync("proxy", answer, cancellationToken);

        yield return StreamUpdate.Complete(verifyTask);

        var time = DateTime.UtcNow;
        while (time > DateTime.UtcNow.AddSeconds(-20))
        {
            await foreach (var proxyEvent in eventAggregator.Subscribe(sessionId).WithCancellation(new CancellationTokenSource(TimeSpan.FromSeconds(20)).Token))
                yield return proxyEvent;
        }

        eventAggregator.CompleteSession(sessionId);

        yield return StreamUpdate.Status("Proxy session completed.");
    }
}

public class ProxyAnswer
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("sessionID")]
    public string SessionId { get; set; } = string.Empty;
}
