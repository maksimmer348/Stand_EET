using System;
using System.Diagnostics;
using System.Threading;

namespace StandETT;

using System.Windows.Media;
using Newtonsoft.Json;

public class RelayVip : BaseDevice
{
    public int Id { get; set; }

    [JsonIgnore]
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
        if (!string.IsNullOrEmpty(nameCommand))
        {
            CurrentCmd = GetLibItem(nameCommand, Name);
        }
        else if (!string.IsNullOrEmpty(nameExternalCmd))
        {
            CurrentCmd = GetLibItem(nameExternalCmd, Name);
        }
        
        if (CurrentCmd == null)
        {
            throw new Exception(
                $"Такое устройство - {IsDeviceType}/{Name} или команда - {nameCommand}, в библиотеке не найдены");
        }

        if (!string.IsNullOrEmpty(CurrentCmd.Length))
        {
            MainRelay.SetReceiveLenght(int.Parse(CurrentCmd.Length)); 
        }
        else
        {
            MainRelay.SetReceiveLenght(0); 
        }
        
        Name = Name;
        NameCurrentCmd = nameCommand;
        
        if (NameCurrentCmd != null && NameCurrentCmd.Contains("On"))
        {
            StatusOnOff = OnOffStatus.Switching;
        }

        CurrentParameter = parameter;
        SetErrors();

        MainRelay.WriteCmdRelay(this, CurrentCmd, parameter);
    }
    
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
        if (name.Contains("SL"))
        {
            IsDeviceType = $"Малая нагрузка-{id}";
        }
        else
        {
            IsDeviceType = $"Реле ВИПА-{id}";
        }
        DeviceReceiving += Relay_Receiving;
    }

    //private Stopwatch s = new Stopwatch();
    private void Relay_Receiving(BaseDevice arg1, string arg2, DeviceCmd arg3)
    {
        if (NameCurrentCmd.Contains("On"))
        {
            StatusOnOff = OnOffStatus.None;
            CtsRelayReceive.Cancel();
        }
    }
}