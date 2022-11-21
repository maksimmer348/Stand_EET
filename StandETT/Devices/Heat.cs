using Newtonsoft.Json;

namespace StandETT;

public class Heat : BaseDevice
{
    public Heat(string name ) : base(name)
    {
        IsDeviceType = "Нагрев";
    }
}