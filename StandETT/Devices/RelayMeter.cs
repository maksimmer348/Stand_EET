using System;
using Newtonsoft.Json;

namespace StandETT;

public class RelayMeter : BaseDevice
{
    public int Id { get; set; }
    /// <summary>
    /// Статус выхода устройства
    /// </summary>
    [JsonIgnore]
    public bool Output1 { get; set; }
    /// <summary>
    /// Статус выхода устройства
    /// </summary>
    [JsonIgnore]
    public bool Output2 { get; set; }
    public RelayMeter(string name) : base(name)
    {
        IsDeviceType = $"Набор реле измерений";
    }
}