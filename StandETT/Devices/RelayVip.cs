﻿using System;
using System.Diagnostics;
using System.Threading;

namespace StandETT;

using System.Windows.Media;
using Newtonsoft.Json;

public class RelayVip : BaseDevice
{
    public int Id { get; set; }
    //public string Id { get; set; }

    public CancellationTokenSource CtsRelayReceive = new();

    MainRelay MainRelay = MainRelay.GetInstance();

    public override void SetConfigDevice(TypePort typePort, string portName, int baud, int stopBits, int parity,
        int dataBits,
        bool dtr = true)
    {
        MainRelay.Config.TypePort = typePort;
        MainRelay.Config.PortName = $"{portName}";
        MainRelay.Config.Baud = baud;
        MainRelay.Config.StopBits = stopBits;
        MainRelay.Config.Parity = parity;
        MainRelay.Config.DataBits = dataBits;
        MainRelay.Config.Dtr = dtr;
    }

    public override void Close()
    {
        MainRelay.Close();
    }

    public override void DtrEnable()
    {
        MainRelay.DtrEnable();
    }

    public override void ClearBuff()
    {
        MainRelay.ClearBuff();
    }

    public override void Start()
    {
        MainRelay.Start();
    }

    /// <summary>
    /// Получить конфиг данные порта устройства 
    /// </summary>
    /// <returns>Данные порта устройства</returns>
    /// <exception cref="DeviceException">Данные получить невзожноно</exception>
    public override ConfigDeviceParams GetConfigDevice()
    {
        try
        {
            return MainRelay.Config;
        }
        catch (Exception e)
        {
            throw new Exception("Файл конфига отсутствует");
        }
    }

    public override void WriteCmd(string nameCommand, string parameter = null)
    {
        CurrentCmd = GetLibItem(nameCommand, Name);


        if (CurrentCmd == null)
        {
            throw new Exception(
                $"Такое устройство - {IsDeviceType}/{Name} или команда - {nameCommand}, в библиотеке не найдены");
        }

        Name = Name;
        NameCurrentCmd = nameCommand;
        if (NameCurrentCmd == "On")
        {
            StatusOnOff = OnOffStatus.Switching;
        }

        CurrentParameter = parameter;
        SetErrors();

        MainRelay.WriteCmdRelay(this, CurrentCmd, parameter);
    }

    // private void Device_Error(BaseDevice device, string error)
    // {
    //     ErrorStatus = $"Ошибка уcтройства {Name}, {error}";
    //     DeviceError?.Invoke(this, error);
    // }
    //
    // private void Device_Receiving(BaseDevice device, string receive, DeviceCmd cmd)
    // {
    //     DeviceReceiving.Invoke(this, receive.ToLower(), cmd);
    // }


    /// <summary>
    /// Статус выхода устройства
    /// </summary>
    [JsonIgnore]
    public bool Output { get; set; }

    /// <summary>
    /// Вид ошибки Випа
    /// </summary>
    [JsonIgnore]
    public RelayVipError ErrorVip { get; set; }

    private OnOffStatus statusOnOff;

    public OnOffStatus StatusOnOff
    {
        get => statusOnOff;
        set => Set(ref statusOnOff, value, nameof(OnOffColor));
    }

    public object OnOffColor
    {
        get
        {
            return StatusOnOff switch
            {
                
                OnOffStatus.Off => Brushes.Red,
                OnOffStatus.On => Brushes.Green,
                OnOffStatus.Switching => Brushes.BlueViolet,
                _ => Brushes.DarkGray
            };
        }
    }

    public bool IsTested { get; set; }


    public RelayVip(int id, string name) : base(name)
    {
        IsDeviceType = $"Реле ВИПА-{id}";
        DeviceReceiving += Relay_Receiving;
    }

    private void Relay_Receiving(BaseDevice arg1, string arg2, DeviceCmd arg3)
    {
        if (NameCurrentCmd == "On")
        {
            StatusOnOff = OnOffStatus.None;
            CtsRelayReceive.Cancel();
        }
    }
}