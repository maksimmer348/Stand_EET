using Newtonsoft.Json;

namespace StandETT;

public class  VoltMeter : BaseDevice
{
    [JsonIgnore]
    public double VoltageOut1 { get; set; }
    [JsonIgnore]
    public double VoltageOut2 { get; set; }
    public VoltMeter(string name) : base(name)
    {
        IsDeviceType = "Вольтметр";
    }


}

public enum MeterMode
{
    VoltageOut1,
    VoltageOut2
}