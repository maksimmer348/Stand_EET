using Newtonsoft.Json;

namespace StandETT;

public class Supply : BaseDevice
{
    [JsonIgnore] public double Voltage { get; set; }
    [JsonIgnore] public double Current { get; set; }

    /// <summary>
    /// Статус выхода устройства
    /// </summary>
    [JsonIgnore]
    public bool Output { get; set; }

    public Supply(string name) : base(name)
    {
        IsDeviceType = "Блок питания";
    }
}