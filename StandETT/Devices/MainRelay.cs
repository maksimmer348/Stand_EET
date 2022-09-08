using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace StandETT;

public class MainRelay : BaseDevice
{
    private List<BaseDevice> relays;

    public ISerialLib GetPort()
    {
        if (port != null)
        {
            return port;
        }
        else
        {
            throw new Exception($"Порт на устройстве {Name} еще нe был инициализирован");
        }
    }

    public MainRelay(string name) : base(name)
    {
        //TODO вохмонжо вернуть
        //relays = relaysVips.ToList();
        //ReceiveRelay += ReceiveRelayMessage;
    }

    // public void TransmitCmdInLibRelay(BaseDevice device, string cmd)
    // {
    //     var selectCmd = GetLibItem(cmd, device.Name);
    //
    //     if (selectCmd == null)
    //     {
    //         throw new Exception(
    //             $"BaseDevice exception: Такое устройство - {IsDeviceType}/{Name} или команда - \"Status\" в библиотеке не найдены");
    //     }
    //     
    //     if (selectCmd.MessageType == TypeCmd.Hex)
    //     {
    //         TypeReceive = TypeCmd.Hex;
    //         if (selectCmd.IsXor)
    //         {
    //             port.TransmitCmdHexString(selectCmd.Transmit, selectCmd.Delay,
    //                 selectCmd.Terminator.ReceiveTerminator, true);
    //         }
    //         else
    //         {
    //             port.TransmitCmdHexString(selectCmd.Transmit, selectCmd.Delay,
    //                 selectCmd.Terminator.ReceiveTerminator);
    //         }
    //     }
    //     else
    //     {
    //         TypeReceive = TypeCmd.Text;
    //         port.TransmitCmdTextString(selectCmd.Transmit, selectCmd.Delay, selectCmd.Terminator.ReceiveTerminator);
    //     }
    // }
    //
    // private void ReceiveRelayMessage(string receive)
    // {
    //     (KeyValuePair<DeviceIdentCmd, DeviceCmd> cmd, BaseDevice baseDevice) cmdInLib = 
    //         (new KeyValuePair<DeviceIdentCmd, DeviceCmd>(), null);
    //     
    //     try
    //     {
    //         var addrVip = receive.Substring(2, 2); //TODO если строка неправильной длины поробовать еще раз
    //         var cmdVip = receive.Substring(4, 2); //TODO если строка неправильной длины поробовать еще раз
    //
    //         cmdInLib = RelayLibEncode(cmdVip, addrVip);
    //         
    //         if (cmdInLib.cmd.Value.Receive == cmdVip)
    //         {
    //             if (cmdInLib.cmd.Key.NameCmd == "On")
    //             {
    //                 DeviceConnecting?.Invoke(cmdInLib.baseDevice, true, "Is on");
    //                 return;
    //             }
    //             DeviceConnecting?.Invoke(cmdInLib.baseDevice, true, receive);
    //         }
    //     }
    //     catch (ArgumentOutOfRangeException e)
    //     {
    //         DeviceConnecting?.Invoke(cmdInLib.baseDevice, false, receive);
    //         return;
    //     }
    //     catch (Exception e)
    //     {
    //         DeviceConnecting?.Invoke(cmdInLib.baseDevice, false, receive);
    //         return;
    //     }
    // }
    //
    // private (KeyValuePair<DeviceIdentCmd, DeviceCmd> cmd, BaseDevice device) RelayLibEncode(string cmdVip,
    //     string vipName)
    // {
    //     (KeyValuePair<DeviceIdentCmd, DeviceCmd> cmd, BaseDevice baseDevice) cmdInLib = 
    //     (new KeyValuePair<DeviceIdentCmd, DeviceCmd>(), null);
    //     switch (vipName)
    //     {
    //         case "AD":
    //         {
    //             cmdInLib = GetLibItemInReceive(cmdVip, "1", relays);
    //             break;
    //         }
    //         case "AE":
    //         {
    //             cmdInLib = GetLibItemInReceive(cmdVip, "2", relays);
    //             break;
    //         }
    //         case "AF":
    //         {
    //             cmdInLib = GetLibItemInReceive(cmdVip, "3", relays);
    //             break;
    //         }
    //         case "B0":
    //         {
    //             cmdInLib = GetLibItemInReceive(cmdVip, "4", relays);
    //             break;
    //         }
    //         case "B9":
    //         {
    //             cmdInLib = GetLibItemInReceive(cmdVip, "5", relays);
    //             break;
    //         }
    //     }
    //
    //     return cmdInLib;
    // }

    // public void EnabledRelay(BaseDevice device)
    // {
    //     var selectCmd = GetLibItem("Status", device.Name);
    //     TransmitCmdInLib();
    // }

    // public string RelayTransmitCmdInLib(RelayVip vipRelay, string cmd)
    // {
    //     var selectCmd = GetLibItem(cmd, vipRelay.Name);
    //     
    // }
}