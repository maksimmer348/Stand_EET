using Newtonsoft.Json;

namespace StandETT;

public class  VoltMeter : BaseDevice
{
    [JsonIgnore]
    public double VoltageOut1 { get; set; }

    public VoltMeter(string name) : base(name)
    {
        IsDeviceType = "Вольтметр";
    }
    
}

public enum MeterMode
{
    Voltage,
    Current
}