using Newtonsoft.Json;

namespace StandETT;

public class ThermoCurrentMeter : BaseDevice
{
    [JsonIgnore]
    public double Temperature { get; set; }
    
    [JsonIgnore]
    public double Current { get; set; }

    /// <summary>
    /// Режим измерения канал - 1 или 2
    /// </summary>
    [JsonIgnore]
    public MeterMode Mode { get; set; }

    public ThermoCurrentMeter(string name ) : base(name)
    {
        IsDeviceType = "Термометр/Амперметр";
    }
}