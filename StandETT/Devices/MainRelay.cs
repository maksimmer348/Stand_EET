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
    private string[] signalInterferences = { "00", };

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
        var open = port.Open();
        
        foreach (var relay in Relays)
        {
            relay.PortIsOpen = open;
        }
        
        return open;
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
        if (error.Contains("Port not found") || error.Contains("Access Denied") || error.Contains("Frame error detected"))
        {
            foreach (var relay in Relays)
            {
                relay.InvokeDeviceError(relay, error);
            }
        }
        else
        {
            if (currentRelayVip != null)
            {
                currentRelayVip.InvokeDeviceError(currentRelayVip, error);
            }
            else
            {
                foreach (var relay in Relays)
                {
                    relay.InvokeDeviceError(relay, error);
                }
            }
        }
    }

    private void Relay_Receive(BaseDevice device, string receive, DeviceCmd cmd)
    {
        try
        {
            // Debug.WriteLine($"ROW receive - {receive}");
            //clear receive
            receive = signalInterferences.Aggregate(receive, (r, s) => r.TrimStart(s).TrimEnd(s));
            //init prefix
            var prefix = receive.Substring(2, 2);

            var currentRelayVipPrefix = Relays.FirstOrDefault(x => x.Prefix.ToLower() == prefix);

            if (currentRelayVipPrefix != null)
            {
                // Debug.WriteLine(
                    // $"CLEAR receive - {receive}/cmd - {currentRelayVipPrefix.NameCurrentCmd}/name prefix - {currentRelayVipPrefix.Name}");
                currentRelayVipPrefix.Device_Receiving(currentRelayVipPrefix, receive, cmd);
            }
            else
            {
                // Debug.WriteLine(
                    // $"CLEAR receive - {receive}/cmd - {currentRelayVip.NameCurrentCmd}/name NO prefix - {currentRelayVip.Name}");
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