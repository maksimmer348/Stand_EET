using System;
using System.Windows.Media;
using Newtonsoft.Json;

namespace StandETT;

public class Vip : Notify
{
    #region --Индентификация Випа
    
    //расположение в таблице окна пограммы
    public int RowIndex { get; set; }

    public int ColumnIndex { get; set; }

    //
    public string IsDeviceType { get; set; }
    public int Id { get; set; }

    [JsonIgnore] private string name;

    [JsonIgnore]
    public string Name
    {
        get => name;
        set => Set(ref name, value);
    }

    /// <summary>
    /// Тип Випа - регулирует максимальные значения температуры напряжения и пр.
    /// </summary>
    public TypeVip Type = TypeVip.getInstance();

    #endregion

    #region --Статусы випа

    private bool isTested;

    [JsonIgnore]
    public bool IsTested
    {
        get => isTested;
        set
        {
            Set(ref isTested, value);
            Relay.IsTested = value;
        }
    }

    private string errorStatusRelay;

    [JsonIgnore]
    public string ErrorStatusRelay
    {
        get => Relay.ErrorStatus;
        set => Set(ref errorStatusRelay, value);
    }

    private string errorStatusVip;

    public string ErrorStatusVip
    {
        get => errorStatusVip;
        set => Set(ref errorStatusVip, value);
    }

    [JsonIgnore] private StatusDeviceTest statusTest;

    [JsonIgnore]
    public StatusDeviceTest StatusTest
    {
        get => statusTest;
        set
        {
            Set(ref statusTest, value, nameof(StatusColor));

            bool extraError = false;

            if (ErrorVip.CurrentInHigh)
            {
                ErrorStatusVip += "Iвх.↑";
                extraError = true;
            }

            if (ErrorVip.VoltageOut1High)
            {
                if (extraError)
                {
                    ErrorStatusVip += "/";
                }

                ErrorStatusVip += "U1вых.↑";
                extraError = true;
            }

            if (ErrorVip.VoltageOut1Low)
            {
                if (extraError)
                {
                    ErrorStatusVip += "/";
                }

                ErrorStatusVip += "U1вых.↓";
                extraError = true;
            }

            if (ErrorVip.VoltageOut2High)
            {
                if (extraError)
                {
                    ErrorStatusVip += "/";
                }

                ErrorStatusVip += "U2вых.↑";
                extraError = true;
            }

            if (ErrorVip.VoltageOut2Low)
            {
                if (extraError)
                {
                    ErrorStatusVip += "/";
                }

                ErrorStatusVip += "U2вых.↓";
            }

            if (ErrorVip.TemperatureHigh)
            {
                if (extraError)
                {
                    ErrorStatusVip += "/";
                }

                ErrorStatusVip += "T↑";
            }

            if (!ErrorVip.CheckIsUnselectError())
            {
                if (!string.IsNullOrEmpty(Name) && value != StatusDeviceTest.None)
                {
                    ErrorStatusVip = "Ok!";
                }

                if (!string.IsNullOrEmpty(Relay.ErrorStatus))
                {
                    ErrorStatusVip = $"{Relay.ErrorStatus}";
                }
                else
                {
                    ErrorStatusVip = null;
                }
            }
        }
    }


    [JsonIgnore]
    public Brush StatusColor =>
        StatusTest switch
        {
            StatusDeviceTest.Error => Brushes.Red,
            StatusDeviceTest.Ok => Brushes.Green,
            _ => Brushes.DarkGray
        };

    [JsonIgnore] private OnOffStatus statusOnOff;

    /// <summary>
    /// Статус Output или Включения устройства
    /// </summary>
    [JsonIgnore]
    public OnOffStatus StatusOnOff
    {
        get => statusOnOff;
        set => Set(ref statusOnOff, value, nameof(OnOffColor));
    }

    /// <summary>
    /// Цвет статуса Output или Включения устройства
    /// </summary>
    [JsonIgnore]
    public object OnOffColor
    {
        get
        {
            return StatusOnOff switch
            {
                OnOffStatus.Off => Brushes.Red,
                OnOffStatus.On => Brushes.Green,
                _ => Brushes.DarkGray
            };
        }
    }


    [JsonIgnore] private StatusChannelVipTest statusChannelVipTest;

    /// <summary>
    /// Статус Output или Включения устройства
    /// </summary>
    [JsonIgnore]
    public StatusChannelVipTest StatusChannelVipTest
    {
        get => statusChannelVipTest;
        set => Set(ref statusChannelVipTest, value, nameof(StatusColorChannel1), nameof(StatusColorChannel2));
    }


    [JsonIgnore]
    public Brush StatusColorChannel1
    {
        get
        {
            return StatusChannelVipTest switch
            {
                StatusChannelVipTest.One => Brushes.Green,
                StatusChannelVipTest.Two => Brushes.Gold,
                StatusChannelVipTest.OneError => Brushes.Red,
                _ => Brushes.DarkGray
            };
        }
    }

    [JsonIgnore]
    public Brush StatusColorChannel2
    {
        get
        {
            return StatusChannelVipTest switch
            {
                StatusChannelVipTest.One => Brushes.Gold,
                StatusChannelVipTest.Two => Brushes.Green,
                StatusChannelVipTest.TwoError => Brushes.Red,
                _ => Brushes.DarkGray
            };
        }
    }

    [JsonIgnore] private OnOffStatus statusSmallLoad;

    /// <summary>
    /// Статус Output или Включения устройства
    /// </summary>
    [JsonIgnore]
    public OnOffStatus StatusSmallLoad
    {
        get => statusSmallLoad;
        set => Set(ref statusSmallLoad, value, nameof(StatusColorSmallLoad));
    }

    public object StatusColorSmallLoad
    {
        get
        {
            return StatusSmallLoad switch
            {
                OnOffStatus.On => Brushes.Green,
                OnOffStatus.Off => Brushes.Red,
                OnOffStatus.Switching => Brushes.BlueViolet,
                _ => Brushes.DarkGray
            };
        }
    }

    /// <summary>
    /// Вид ошибки Випа
    /// </summary>
    [JsonIgnore]
    public RelayVipError ErrorVip { get; set; }

    #endregion

    #region --Устройства випа

    /// <summary>
    /// Релейный модуль Випа
    /// </summary>
    public RelayVip Relay { get; set; }

    #endregion

    #region --Значения Випа

    //Текущие значения на Випе
    private decimal voltageOut1;

    public decimal VoltageOut1
    {
        get => voltageOut1;
        set => Set(ref voltageOut1, value);
    }

    private decimal voltageOut2;

    public decimal VoltageOut2
    {
        get => voltageOut2;
        set => Set(ref voltageOut2, value);
    }

    private decimal currentIn;

    public decimal CurrentIn
    {
        get => currentIn;
        set => Set(ref currentIn, value);
    }

    private decimal temperature;

    public decimal Temperature
    {
        get => temperature;
        set => Set(ref temperature, value);
    }


    public decimal VoltageIn { get; set; }

    #endregion

    #region --Адрес для репортера

    public int Channel1AddrNum { get; set; }
    public int Channel2AddrNum { get; set; }

    #endregion

    public Vip(int id, RelayVip relayVip)
    {
        Id = id;
        IsDeviceType = $"Вип {id}";
        Relay = relayVip;
        Relay.Id = id;
    }
}