using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StandETT;

public static class ExtensionDevice
{
    public static T GetTypeDevice<T>(this ReadOnlyObservableCollection<BaseDevice> baseDevices) where T : BaseDevice
    {
        return (T)baseDevices.FirstOrDefault(x => x is T);
    }
    public static T GetTypeDevice<T>(this List<BaseDevice> baseDevices) where T : BaseDevice
    {
        return (T)baseDevices.FirstOrDefault(x => x is T);
    }
}