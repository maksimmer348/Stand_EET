using System;
using System.Windows.Media;
using Newtonsoft.Json;

namespace StandETT;

public class Vip : Notify
{
    #region --Индентификация Випа

    //
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
        set => Set(ref name, value, nameof(StatusColor));
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
        set => Set(ref statusTest, value, nameof(StatusColor));
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

    #endregion

    #region --Устройства випа

    /// <summary>
    /// Релейный модуль Випа
    /// </summary>
    public RelayVip Relay { get; set; }

    #endregion

    #region Значения Випа

    //Текущие значения на Випе
    private decimal voltageOut1;

    public decimal VoltageOut1
    {
        get => voltageOut1;
        set => Set(ref voltageOut1, value);
    }

    public decimal VoltageOut2 { get; set; }
    public decimal CurrentIn { get; set; }
    
   
    private decimal temperature;
    public decimal Temperature    
    {
        get => temperature;
        set => Set(ref temperature, value);
    }

    
    public decimal VoltageIn { get; set; }
    
    #endregion

    public Vip(int id, RelayVip relayVip)
    {
        Id = id;
        IsDeviceType = $"Вип {id}";
        Relay = relayVip;
        Relay.Id = id;
    }
}