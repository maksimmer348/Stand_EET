using Newtonsoft.Json;

namespace StandETT;

public class VoltCurrentMeter : BaseDevice
{
    public VoltCurrentMeter(string name ) : base(name)
    {
        IsDeviceType = "Вольтметр/Амперметр";
    }
}