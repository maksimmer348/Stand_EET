using System;
using System.Windows.Media;
using Newtonsoft.Json;

namespace StandETT;

public class Vip : BaseDevice
{
    public bool IsTested { get; set; }


    private StatusDeviceTest statusTest;

    public StatusDeviceTest StatusTest
    {
        get => statusTest;
        set => Set(ref statusTest, value, nameof(StatusColor));
    }

    public Brush StatusColor =>
        StatusTest switch
        {
            StatusDeviceTest.Error => Brushes.Red,
            StatusDeviceTest.Ok => Brushes.Green,
            _ => Brushes.DarkGray
        };


    /// <summary>
    /// Тип Випа - регулирует максимальные значения температуры напряжения и пр.
    /// </summary>
    public TypeVip Type { get; set; }


    //Текущие значения на Випе
    private double voltageOut1;

    public double VoltageOut1
    {
        get => voltageOut1;
        set => Set(ref voltageOut1, value);
    }


    public double VoltageOut2 { get; set; }
    public double CurrentIn { get; set; }
    public double Temperature { get; set; }
    public double VoltageIn { get; set; }


    public int Id { get; set; }

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
    

    public void SetUnityPort(ISerialLib unityPort)
    {
        port = unityPort;
    }

    //
    //расположение в таблице окна пограммы
    public int RowIndex { get; set; }
    public int ColumnIndex { get; set; }


    //
    public Vip(int i, string name, string addrRelay) : base(name)
    {
    }
}