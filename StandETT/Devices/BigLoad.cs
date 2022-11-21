using Newtonsoft.Json;

namespace StandETT;

public class BigLoad : BaseDevice
{
    public BigLoad(string name) : base(name)
    {
        IsDeviceType = "Большая нагрузка";
    }
}