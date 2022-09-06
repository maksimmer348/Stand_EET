using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StandETT;

public class Checker : Notify
{
}

public class TempChecksDevices
{
    private readonly List<BaseDevice> baseDevicesIsOk = new();
    private readonly List<BaseDevice> baseDevices;

    public void Add(BaseDevice value)
    {
        baseDevicesIsOk.Add(value);
    }

    public bool IsOk => !baseDevicesIsOk.Except(baseDevices).Any();

    public static TempChecksDevices Start(List<BaseDevice> baseDevices) => new TempChecksDevices(baseDevices);

    /// <summary>
    /// Проверка списка устройств
    /// </summary>
    /// <param name="baseDevices">Проверяемый список</param>
    public TempChecksDevices(List<BaseDevice> baseDevices)
    {
        this.baseDevices = baseDevices;
    }
}