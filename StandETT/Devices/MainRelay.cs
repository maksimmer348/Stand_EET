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
    [JsonIgnore] public ObservableCollection<RelayVip> Relays { get; set; } = new();

    /// <summary>
    /// Применение настроек, подключение событий и старт устройства
    /// </summary>
    public override void Start()
    {
        SetPort();
        PortIsOpen = port.Open();
        port.Dtr = Config.Dtr;

        foreach (var relay in Relays)
        {
            relay.PortIsOpen = PortIsOpen;
        }
    }

    /// <summary>
    /// Открыть компорт устройства
    /// </summary>
    /// <returns></returns>
    public override bool Open()
    {
        foreach (var relay in Relays)
        {
            relay.PortIsOpen = true;
        }

        return port.Open();
    }

    public static MainRelay GetInstance()
    {
        if (instance != null) return instance;
        lock (syncRoot)
        {
            instance ??= new MainRelay("MRV");
        }

        return instance;
    }
    public MainRelay(string name) : base(name)
    {
        PortConnecting += Port_Connecting;
        DeviceReceiving += Relay_Receive;
        DeviceError += Relay_Error;
    }

    private void Port_Connecting(BaseDevice device, bool isConnect)
    {
        ErrorStatus = string.Empty;
        foreach (var relay in Relays)
        {
            relay.InvokePortConnecting(relay, true);
        }
    }

    private void Relay_Error(BaseDevice device, string error)
    {
        if (error.Contains("Port not found") || error.Contains("Access Denied"))
        {
            foreach (var relay in Relays)
            {
                relay.InvokeDeviceError(relay, error);
            }
        }
        else
        {
            currentRelayVip.InvokeDeviceError(currentRelayVip, error);
        }
    }

    private void Relay_Receive(BaseDevice device, string receive, DeviceCmd cmd)
    {
        try
        {
            string prefix = null;

            prefix = receive.Contains("6f") ? receive.Substring(3, 1) : receive.Substring(2, 2);

            var currentRelayVipPrefix = Relays.FirstOrDefault(x => x.Prefix.ToLower() == prefix);
            if (currentRelayVipPrefix != null)
            {
                currentRelayVipPrefix.Device_Receiving(currentRelayVipPrefix, receive, cmd);
            }
            else
            {
                currentRelayVip.Device_Receiving(currentRelayVip, receive, cmd);
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

    public void SetReceiveLenght(int parse)
    {
        port.SetReceiveLenght(parse);
    }
}