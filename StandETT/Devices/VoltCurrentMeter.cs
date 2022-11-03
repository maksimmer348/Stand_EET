using Newtonsoft.Json;

namespace StandETT;

public class VoltCurrentMeter : BaseDevice
{

    [JsonIgnore]
    public double Current { get; set; }

    [JsonIgnore]
    public double VoltageOut2 { get; set; }
    
    /// <summary>
    /// Режим измерения нпряжение/ток
    /// </summary>
    [JsonIgnore]
    public MeterMode Mode { get; set; }

    public VoltCurrentMeter(string name ) : base(name)
    {
        IsDeviceType = "Вольтметр/Амперметр";
    }
}