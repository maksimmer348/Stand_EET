using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StandETT;

public static class ExtensionDevice
{
    public static T GetTypeDevice<T>(this ObservableCollection<BaseDevice> baseDevices) where T : BaseDevice
    {
        return (T)baseDevices.FirstOrDefault(x => x is T);
    }
    public static T GetTypeDevice<T>(this List<BaseDevice> baseDevices) where T : BaseDevice
    {
        return (T)baseDevices.FirstOrDefault(x => x is T);
    }
}

public static class ExtensionRelay
{
    public static T GetTypeDevice<T>(this ObservableCollection<RelayVip> baseDevices) where T : RelayVip
    {
        return (T)baseDevices.FirstOrDefault(x => x is T);
    }
    public static T GetTypeDevice<T>(this List<RelayVip> baseDevices) where T : RelayVip
    {
        return (T)baseDevices.FirstOrDefault(x => x is T);
    }
}


public static class ExtensionParameters
{
    public static T GetTypeDevice<T>(this BaseDeviceValues parameters) where T : BaseDeviceValues
    {
        return (T)parameters;
    }
}