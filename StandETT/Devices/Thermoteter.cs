using Newtonsoft.Json;

namespace StandETT;

public class Thermometer : BaseDevice
{
    [JsonIgnore]
    public double Temperature { get; set; }


    public Thermometer(string name) : base(name)
    {
        IsDeviceType = "Термометр";
    }
}