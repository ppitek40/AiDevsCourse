using System.Runtime.CompilerServices;
using System.Text.Json;
using AiDevs.Core.Interfaces;
using AiDevs.Core.Models;
using AiDevs.Infrastructure.Services;

namespace AiDevs.Solutions.Task11;

/// <summary>
/// Solution for Task 11 - Sensor data validation and operator notes analysis
/// Reads sensor files, validates values against sensor_type, analyzes operator notes with LLM (cached),
/// and reports files with inaccuracies
/// </summary>
public class Task11Solution(
    ISensorAnomalies sensorAnomalies,
    IOperatorNotesClassifier operatorNotesClassifier,
    IAiDevsApiService aiDevsApiService) : ITaskSolution
{
    public int TaskId => 11;

    public async IAsyncEnumerable<StreamUpdate> ExecuteStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return StreamUpdate.Status("Starting sensor data validation task...");

        var sensorsPath = Path.Combine(AppContext.BaseDirectory,
            "../../../../AiDevs.Solutions/Task11/sensors");

        if (!Directory.Exists(sensorsPath))
        {
            yield return StreamUpdate.Complete(
                SolutionResult.Fail("Sensors directory not found"));
            yield break;
        }

        var sensorFiles = Directory.GetFiles(sensorsPath, "*.json")
            .OrderBy(f => f)
            .ToList();

        yield return StreamUpdate.Status($"Found {sensorFiles.Count} sensor files to process");

        yield return StreamUpdate.Status("Processing sensor files...");

        var readings = new List<SensorReadings>();
        foreach (var filePath in sensorFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);

            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            SensorFile? reading = null;
            try
            {
                reading = JsonSerializer.Deserialize<SensorFile>(json);
            }
            catch (Exception)
            {
                // ignored
            }

            if (reading == null)
            {
                yield return StreamUpdate.Status($"Failed to parse {fileName}");
                continue;
            }

            readings.Add(reading.ToSensorReadings(fileName));
        }
        yield return StreamUpdate.Status($"Parsed {readings.Count} files");

        foreach (var reading in readings)
        {
            reading.IsReadingAccurate = sensorAnomalies.IsSensorAnomaly(reading);
            if(reading.IsReadingAccurate)
                reading.IsOperatorNotesPositive = !await operatorNotesClassifier.IndicatesProblemAsync(reading.OperatorNotes, cancellationToken);
        }

        yield return StreamUpdate.Status("Assigned the rating of values and operator notes.");

        var wrongFiles = readings.Where(r => !r.IsReadingAccurate || !r.IsFileValid).Select(r => r.FileName).ToList();
        yield return StreamUpdate.Status($"Found {wrongFiles.Count} files with errors");
        yield return StreamUpdate.Status("Verifying answer with API...");

        var result = await aiDevsApiService.VerifyAsync(
            "evaluation",
            new { recheck = wrongFiles},
            cancellationToken);

        yield return StreamUpdate.Complete(result);
    }
}
