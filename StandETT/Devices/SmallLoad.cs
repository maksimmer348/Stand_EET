using Newtonsoft.Json;

namespace StandETT;

public class SmallLoad : BaseDevice
{
    /// <summary>
    /// Статус выхода устройства
    /// </summary>
    [JsonIgnore]
    public bool Output { get; set; }
    public SmallLoad(string name) : base(name)
    {
        IsDeviceType = "Малая нагрузка";
    }
}