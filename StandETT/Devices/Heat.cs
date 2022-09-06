using Newtonsoft.Json;

namespace StandETT;

public class Heat : BaseDevice
{
    
    /// <summary>
    /// Статус выхода устройства
    /// </summary>
    [JsonIgnore]
    public bool Output { get; set; }

    public Heat(string name ) : base(name)
    {
        IsDeviceType = "Нагрев";
    }
}