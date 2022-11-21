using Newtonsoft.Json;

namespace StandETT;

public class  VoltMeter : BaseDevice
{
    public VoltMeter(string name) : base(name)
    {
        IsDeviceType = "Вольтметр";
    }
}