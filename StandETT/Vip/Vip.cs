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

    private string channel2Revers;

    [JsonIgnore]
    public string Channel2Revers
    {
        get => channel2Revers;
        set => Set(ref channel2Revers, value);
    }

    private string channelARevers;

    [JsonIgnore]
    public string ChannelARevers
    {
        get => channelARevers;
        set => Set(ref channelARevers, value);
    }

    private string channel1Revers = "";

    [JsonIgnore]
    public string Channel1Revers
    {
        get => channel1Revers;
        set => Set(ref channel1Revers, value);
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

            ErrorStatusVip = " внутр. ошибка - ";

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

            if (ErrorVip.TemperatureIn)
            {
                if (extraError)
                {
                    ErrorStatusVip += "/";
                }

                ErrorStatusVip += "Tin!";
            }

            if (ErrorVip.TemperatureOut)
            {
                if (extraError)
                {
                    ErrorStatusVip += "/";
                }

                ErrorStatusVip += "Tout!";
            }
            if (!string.IsNullOrEmpty(Relay.ErrorStatus))
            {
                if (extraError)
                {
                    ErrorStatusVip += "/";
                }
                ErrorStatusVip += "Реле ≠>";
            }

            if (!ErrorVip.CheckIsUnselectError())
            {
                if (!string.IsNullOrEmpty(Name) && value != StatusDeviceTest.None)
                {
                    ErrorStatusVip = "Ok!";
                }

                if (!string.IsNullOrEmpty(Relay.ErrorStatus))
                {
                    ErrorStatusVip += "Реле ≠>";
                    // ErrorStatusVip = $"{Relay.ErrorStatus}";
                }
                else //TODO возможно веруть назад с добавлением увлоия на vip.StatusTest == StatusDeviceTest.Warning
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
            StatusDeviceTest.Warning => Brushes.DarkOrange,
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
        set => Set(ref statusChannelVipTest, value, nameof(StatusColorChannelV1), nameof(StatusColorChannelV2),
            nameof(StatusColorChannelA));
    }


    [JsonIgnore]
    public Brush StatusColorChannelV1
    {
        get
        {
            return StatusChannelVipTest switch
            {
                StatusChannelVipTest.ChannelV1Ok => Brushes.Green,
                StatusChannelVipTest.ChannelV2Ok => Brushes.Gold,
                StatusChannelVipTest.ChannelAOk => Brushes.Gold,
                StatusChannelVipTest.ChannelV1Error => Brushes.Red,
                _ => Brushes.DarkGray
            };
        }
    }

    [JsonIgnore]
    public Brush StatusColorChannelV2
    {
        get
        {
            return StatusChannelVipTest switch
            {
                StatusChannelVipTest.ChannelV1Ok => Brushes.Gold,
                StatusChannelVipTest.ChannelV2Ok => Brushes.Green,
                StatusChannelVipTest.ChannelAOk => Brushes.Gold,
                StatusChannelVipTest.ChannelV2Error => Brushes.Red,
                _ => Brushes.DarkGray
            };
        }
    }

    [JsonIgnore]
    public Brush StatusColorChannelA
    {
        get
        {
            return StatusChannelVipTest switch
            {
                StatusChannelVipTest.ChannelV1Ok => Brushes.Gold,
                StatusChannelVipTest.ChannelAOk => Brushes.Green,
                StatusChannelVipTest.ChannelV2Ok => Brushes.Gold,
                StatusChannelVipTest.ChannelAError => Brushes.Red,
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

    private decimal currentIn = 0;

    public decimal CurrentIn
    {
        get => currentIn;
        set => Set(ref currentIn, value);
    }

    private decimal temperatureIn;

    public decimal TemperatureIn
    {
        get => temperatureIn;
        set => Set(ref temperatureIn, value);
    }

    private decimal temperatureOut;

    public decimal TemperatureOut
    {
        get => temperatureOut;
        set => Set(ref temperatureOut, value);
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