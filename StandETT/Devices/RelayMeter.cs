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

public enum SetTestChannel
{
    ChannelV1,
    ChannelV2,
    ChannelA
} 