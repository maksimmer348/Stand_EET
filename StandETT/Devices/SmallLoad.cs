using System;
using System.Diagnostics;
using Newtonsoft.Json;

namespace StandETT;

public class SmallLoad : BaseDevice
{
    public SmallLoad(int id, string name) : base(name)
    {
        IsDeviceType = $"Малая нагрузка-{id}";
    }
}