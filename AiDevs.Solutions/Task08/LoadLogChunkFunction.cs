using System.Text.Json;
using System.Text.Json.Serialization;
using AiDevs.Infrastructure.FunctionCalling;

namespace AiDevs.Solutions.Task08;

[FunctionDefinition("load_log_chunk", "Load a chunk of logs from the API")]
public class LoadLogChunkFunction : IFunctionHandler
{
    public Type ParametersType => typeof(LoadLogChunkParameters);
    private const int MaxChunkSize = 400;

    public async Task<string> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not LoadLogChunkParameters p)
            return JsonSerializer.Serialize(new { error = "Invalid parameters type" });

        var resultPath = Path.Combine(AppContext.BaseDirectory, "../../../../AiDevs.Solutions/Task08/failure.log");
        var lines = await File.ReadAllLinesAsync(resultPath, cancellationToken);
        lines = lines.Where(l => l.Contains("[CRIT]") || l.Contains("[ERRO]")).ToArray();
        lines = lines.DistinctBy(l => l.Split("] [")[1]).ToArray();
        var chunk = string.Join("\n", lines.Skip((p.ChunkNumber -1) * MaxChunkSize).Take(MaxChunkSize));
        return JsonSerializer.Serialize(new
        {
            NumberOfChunks = (int)Math.Ceiling((double)lines.Length / MaxChunkSize),
            Chunk = chunk
        });
    }    
}

public class LoadLogChunkParameters
{
    [JsonPropertyName("chunk_number")]
    [Parameter("The chunk number to read. Starting from 1", required: true)]
    public int ChunkNumber { get; set; }
}