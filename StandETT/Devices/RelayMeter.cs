using System;
using Newtonsoft.Json;

namespace StandETT;

public class RelayMeter : BaseDevice
{
    public RelayMeter(string name) : base(name)
    {
        IsDeviceType = $"Набор реле измерений";
    }
}