namespace AiDevs.Solutions.Task11;

public interface ISensorAnomalies
{
    bool IsSensorAnomaly(SensorReadings reading);
}

public class SensorAnomalies
    : ISensorAnomalies
{
    private List<SensorAcceptedValues> sensorAcceptedValues =
    [
        new() {Type = SensorType.Humidity, MinValue = 40.0f, MaxValue = 80.0f },
        new() {Type = SensorType.Voltage, MinValue = 229.0f, MaxValue = 231.0f },
        new() {Type = SensorType.Water, MinValue = 5.0f, MaxValue = 15.0f },
        new() {Type = SensorType.Pressure, MinValue = 60.0f, MaxValue = 160.0f },
        new() {Type = SensorType.Temperature, MinValue = 553.0f, MaxValue = 873.0f },
    ];
    public bool IsSensorAnomaly(SensorReadings reading)
    {
        foreach (var acceptedValue in sensorAcceptedValues)
        {
            var value = reading.Values.First(v => v.Type == acceptedValue.Type);
            if (reading.SensorTypes.Contains(acceptedValue.Type))
            {
                if (IsAccurate(acceptedValue, value))
                    continue;
                return false;
            }

            if (value.Value == 0)
                continue;
            return false;
        }

        return true;
    }

    private bool IsAccurate(SensorAcceptedValues acceptedValues, SensorValue value)
    {
        return value.Value >= acceptedValues.MinValue && value.Value <= acceptedValues.MaxValue;
    }
}

public class SensorAcceptedValues
{
    public SensorType Type { get; set; }
    public float MinValue { get; set; }
    public float MaxValue { get; set; }
}