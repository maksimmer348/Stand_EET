using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;

namespace StandETT;

public class MainRelay : BaseDevice
{
    private static MainRelay instance;
    private static object syncRoot = new();
    private RelayVip currentRelayVip;

    public static MainRelay getInstance()
    {
        if (instance == null)
        {
            lock (syncRoot)
            {
                if (instance == null)
                    instance = new MainRelay("MRV");
            }
        }

        return instance;
    }

    [JsonIgnore] public ObservableCollection<RelayVip> Relays = new();

    public MainRelay(string name) : base(name)
    {
        PortConnecting += Port_Connecting;
        DeviceReceiving += Receive_Relay;
        DeviceError += Relay_Error;
    }

    private void Port_Connecting(BaseDevice device, bool isConnect)
    {
        ErrorStatus = string.Empty;
        foreach (var relay in Relays)
        {
            relay.ErrorStatus = string.Empty;
            relay.PortIsOpen = true;
        }
    }

    private void Relay_Error(BaseDevice device, string error)
    {
        ErrorStatus = string.Empty;
        foreach (var relay in Relays)
        {
            relay.DeviceError.Invoke(this, error);
            // vip.ErrorStatus =
            //     $"Ошибка уcтройства \"{device.Name}\"/сбой порта {device.GetConfigDevice().PortName}";
            // vip.StatusTest = StatusDeviceTest.Error;
        }
    }

    private void Receive_Relay(BaseDevice device, string receive, DeviceCmd cmd)
    {
        ErrorStatus = string.Empty;
        
        try
        {
            var prefix = receive.Substring(2, 2);
            currentRelayVip.AllDeviceError.ErrorTimeout = false;
            var currentRelayVipPrefix = Relays.FirstOrDefault(x => x.Prefix.ToLower() == prefix);
            if (currentRelayVipPrefix != null)
            {
                currentRelayVipPrefix.DeviceReceiving?.Invoke(currentRelayVipPrefix, receive, cmd);
            }
            else
            {
                currentRelayVip.DeviceReceiving?.Invoke(currentRelayVip, receive, cmd);
            }
        }
        catch (ArgumentOutOfRangeException e)
        {
            currentRelayVip.AllDeviceError.ErrorTimeout = true;
        }
    }

    /// <summary>
    /// Отправка в устройство (есть в библиотеке команд) команд из устройства
    /// </summary>
    /// <param name="nameCommand">Имя команды (например Status)</param>
    /// <param name="parameter">Ответ от устройств из команды (Receive)</param>
    public void WriteCmdRelay(RelayVip relayVip, DeviceCmd cmd, string parameter = null)
    {
        relayVip.ErrorStatus = string.Empty;

        currentRelayVip = relayVip;
        CurrentCmd = cmd;
        if (CurrentCmd.MessageType == TypeCmd.Hex)
        {
            TypeReceive = TypeCmd.Hex;
            if (CurrentCmd.IsXor)
            {
                port.TransmitCmdHexString(CurrentCmd.Transmit + parameter, CurrentCmd.Delay,
                    CurrentCmd.Terminator.ReceiveTerminator, true);
            }
            else
            {
                port.TransmitCmdHexString(CurrentCmd.Transmit + parameter, CurrentCmd.Delay,
                    CurrentCmd.Terminator.ReceiveTerminator);
            }
        }
        else
        {
            TypeReceive = TypeCmd.Text;
            port.TransmitCmdTextString(CurrentCmd.Transmit + parameter, CurrentCmd.Delay,
                CurrentCmd.Terminator.ReceiveTerminator);
        }
    }
}