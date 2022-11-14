using System;
using System.Diagnostics;
using Newtonsoft.Json;

namespace StandETT;

public class SmallLoad : BaseDevice
{
    /// <summary>
    /// Статус выхода устройства
    /// </summary>
    [JsonIgnore]
    public bool Output { get; set; }

    // public event Action<BaseDevice, string, DeviceCmd> LoadReceiving;

    public SmallLoad(int id, string name) : base(name)
    {
        IsDeviceType = $"Малая нагрузка-{id}";
        //DeviceReceiving += Relay_Receiving;
        // DeviceReceiving += Load_Receive;
    }

    private string temReceive;

    // public override void WriteCmd(string nameCommand, string parameter = null)
    // {
    //     //temReceive = null;
    //     base.WriteCmd(nameCommand, parameter);
    // }

    private int i = 0;

    // public void Load_Receive(BaseDevice device, string receive, DeviceCmd cmd)
    // {
    //     Debug.WriteLine(i);
    //     i++;
    //
    //     if (NameCurrentCmd == "Off")
    //     {
    //         temReceive += receive;
    //
    //         if (temReceive.Length / 2 == int.Parse(cmd.Length))
    //         {
    //             Debug.WriteLine(temReceive);
    //
    //             LoadReceiving?.Invoke(device, temReceive, cmd);
    //         }
    //     }
    //
    //     if (NameCurrentCmd == "On")
    //     {
    //         LoadReceiving?.Invoke(device, receive, cmd);
    //     }
    //
    //     else
    //     {
    //         LoadReceiving?.Invoke(device, receive, cmd);
    //     }
    // }
}