using System.Text.Json.Serialization;

namespace AiDevs.Solutions.Task11;

public class SensorFile
{
    [JsonPropertyName("sensor_type")]
    public string SensorTypeStr { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("temperature_K")]
    public float TemperatureK { get; set; }

    [JsonPropertyName("pressure_bar")]
    public float PressureBar { get; set; }

    [JsonPropertyName("water_level_meters")]
    public float WaterLevelMeters { get; set; }

    [JsonPropertyName("voltage_supply_v")]
    public float VoltageSupplyV { get; set; }

    [JsonPropertyName("humidity_percent")]
    public float HumidityPercent { get; set; }

    [JsonPropertyName("operator_notes")]
    public string OperatorNotes { get; set; } = string.Empty;

    public SensorReadings ToSensorReadings(string fileName)
    {
        var reading = new SensorReadings
        {
            FileName = fileName,
            OperatorNotes = OperatorNotes,
            SensorTypes = SensorTypeStr.Split("/").Select(s => Enum.Parse<SensorType>(s, true)).ToArray()
        };

        reading.Values.AddRange(new List<SensorValue> {
        new() { Type = SensorType.Temperature, Value = TemperatureK },
        new() { Type = SensorType.Pressure, Value = PressureBar },
        new() { Type = SensorType.Water, Value = WaterLevelMeters },
        new() { Type = SensorType.Voltage, Value = VoltageSupplyV },
        new() { Type = SensorType.Humidity, Value = HumidityPercent },});

        return reading;
    }
}

public class SensorReadings
{
    public string FileName {get; set;}
    public SensorType[] SensorTypes { get; set; } = [];
    public List<SensorValue> Values { get; set; } = [];
    public string OperatorNotes {get; set;}
    public bool IsReadingAccurate { get; set; } = false;
    public bool IsOperatorNotesPositive { get; set; } = true;
    public bool IsFileValid => IsReadingAccurate == IsOperatorNotesPositive;
}

public class SensorValue
{
    public SensorType Type { get; set; }
    public float Value { get; set; }
}

public enum SensorType
{
    Temperature,
    Pressure,
    Water,
    Voltage,
    Humidity
}
