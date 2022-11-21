using Newtonsoft.Json;

namespace StandETT;

public class Supply : BaseDevice
{
    public Supply(string name) : base(name)
    {
        IsDeviceType = "Блок питания";
    }
}