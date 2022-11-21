using Newtonsoft.Json;

namespace StandETT;

public class Thermometer : BaseDevice
{
    public Thermometer(string name) : base(name)
    {
        IsDeviceType = "Термометр";
    }
}