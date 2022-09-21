using System;
using System.Collections.ObjectModel;
using System.Globalization;
using Newtonsoft.Json;

namespace StandETT;

public class TypeVip : Notify
{
    private static TypeVip instance;
    private static object syncRoot = new();

    public static TypeVip getInstance()
    {
        if (instance == null)
        {
            lock (syncRoot)
            {
                if (instance == null)
                    instance = new TypeVip();
            }
        }
        return instance;
    }

    private string type;

    /// <summary>
    /// Тип Випа
    /// </summary>
    public string Type
    {
        get => type;
        set => Set(ref type, value);
    }

    #region Значения для Випов

    //максимальные значения во время цикла испытаниий 1...n, они означают ошибку
    public double MaxTemperature { get; set; }

    public double MaxVoltageIn { get; set; }

    private double maxVoltageOut1;

    public double MaxVoltageOut1
    {
        get => maxVoltageOut1;
        set
        {
            maxVoltageOut1 = value;
            PrepareMaxVoltageOut1 = value;
        }
    }

    private double maxVoltageOut2;

    public double MaxVoltageOut2
    {
        get => maxVoltageOut2;
        set
        {
            maxVoltageOut2 = value;
            PrepareMaxVoltageOut2 = value;
        }
    }

    public double MaxCurrentIn { get; set; }


    //максимальные значения во время замера 0
    public double PrepareMaxCurrentIn { get; set; }
    public double PrepareMaxVoltageOut1 { get; set; }
    public double PrepareMaxVoltageOut2 { get; set; }

    private bool enableTypeVipName = true;

    public bool EnableTypeVipName
    {
        get => enableTypeVipName;
        set => Set(ref enableTypeVipName, value);
    }

    public double PercentAccuracyVoltages { get; set; }

    public double PercentAccuracyCurrent { get; set; }
    public bool VoltageOut2Using { get; set; }

    #endregion

    //

    #region Значения для приброров

    public ObservableCollection<BaseDeviceValues> BaseDeviceValues = new ObservableCollection<BaseDeviceValues>();

    public DeviceParameters Parameters;

    public void SetDeviceParameters(DeviceParameters dp)
    {
        Parameters = dp;
    }

    public DeviceParameters GetDeviceParameters()
    {
        try
        {
            return Parameters;
        }
        catch (Exception e)
        {
            throw new Exception("VipException: Параметры випа не заданы");
        }
    }

    #endregion
}

public class DeviceParameters
{
    public BigLoadValues BigLoadValues { get; set; }
    public HeatValues HeatValues { get; set; }
    public SupplyValues SupplyValues { get; set; }
    public ThermoCurrentMeterValues ThermoCurrentValues { get; set; }
    public VoltMeterValues VoltValues { get; set; }
}


public class VoltMeterValues : BaseDeviceValues
{
    [JsonIgnore] public string ReturnFuncGDM { get; set; }
    [JsonIgnore] public string ReturnVoltGDM { get; set; }

    private ModeGdm mode;

    [JsonIgnore]
    public ModeGdm Mode
    {
        get { return mode; }
        set
        {
            SetFuncVoltageGDM();
            mode = value;
        }
    }

    //

    private string voltMaxLimit;

    public string VoltMaxLimit
    {
        get { return voltMaxLimit; }
        set
        {
            SetFuncVoltageGDM();
            voltMaxLimit = value;
        }
    }

    public VoltMeterValues(string voltMaxLimit, string outputOn, string outputOff) : base(outputOn, outputOff)
    {
        VoltMaxLimit = voltMaxLimit;
        SetFuncVoltageGDM();
    }

    //
    void SetFuncVoltageGDM()
    {
        if (VoltMaxLimit == null)
        {
            return;
        }
        else if (double.Parse(VoltMaxLimit) == 0.1)
        {
            ReturnVoltGDM = "1";
        }
        else if (int.Parse(VoltMaxLimit) == 1)
        {
            ReturnVoltGDM = "2";
        }
        else if (int.Parse(VoltMaxLimit) == 10)
        {
            ReturnVoltGDM = "3";
        }
        else if (int.Parse(VoltMaxLimit) == 100)
        {
            ReturnVoltGDM = "4";
        }
        else if (int.Parse(VoltMaxLimit) == 1000)
        {
            ReturnVoltGDM = "5";
        }

        if (Mode == ModeGdm.Voltage)
        {
            ReturnFuncGDM = "1";
        }
    }
}

public class ThermoCurrentMeterValues : BaseDeviceValues
{
    [JsonIgnore] public string ReturnFuncGDM { get; set; }
    [JsonIgnore] public string ReturnCurrGDM { get; set; }

    private ModeGdm mode;

    [JsonIgnore]
    public ModeGdm Mode
    {
        get { return mode; }
        set
        {
            SetFuncVoltageGDM();
            mode = value;
        }
    }

    //

    private string currMaxLimit;

    public string CurrMaxLimit
    {
        get { return currMaxLimit; }
        set
        {
            SetFuncVoltageGDM();
            currMaxLimit = value;
        }
    }

    //

    public ThermoCurrentMeterValues(string currMaxLimit, string outputOn, string outputOff) : base(outputOn, outputOff)
    {
        CurrMaxLimit = currMaxLimit;
        SetFuncVoltageGDM();
    }

    void SetFuncVoltageGDM()
    {
        if (CurrMaxLimit == null)
        {
            return;
        }
        else if (double.Parse(CurrMaxLimit) == 0.010)
        {
            ReturnCurrGDM = "1";
        }
        else if (int.Parse(CurrMaxLimit) == 0.100)
        {
            ReturnCurrGDM = "2";
        }
        else if (int.Parse(CurrMaxLimit) == 3)
        {
            ReturnCurrGDM = "3";
        }

        if (Mode == ModeGdm.Voltage)
        {
            ReturnFuncGDM = "1";
        }

        if (Mode == ModeGdm.Current)
        {
            ReturnFuncGDM = "3";

            ReturnCurrGDM = "1";
        }

        if (Mode == ModeGdm.Themperature)
        {
            ReturnFuncGDM = "9";
        }
    }
}

public enum ModeGdm
{
    None,
    Voltage,
    Themperature,
    Current,
}

public class BaseDeviceValues
{
    public string OutputOn { get; set; }
    public string OutputOff { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="outputOn">Включить выход</param>
    /// <param name="outputOff">Выключить выход</param>
    public BaseDeviceValues(string outputOn, string outputOff)
    {
        OutputOn = outputOn;
        OutputOff = outputOff;
    }
}

public class BigLoadValues : BaseDeviceValues
{
    public string Freq { get; set; }
    public string Ampl { get; set; }
    public string Dco { get; set; }
    public string Squ { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="freq">Частота</param>
    /// <param name="ampl">Амплитуда</param>
    /// <param name="dco">Л=DCO</param>
    /// <param name="squ">SQU</param>
    /// <param name="outputOn">Вкл</param>
    /// <param name="outputOff">Выкл</param>
    public BigLoadValues(string freq, string ampl, string dco, string squ, string outputOn, string outputOff) : base(
        outputOn, outputOff)
    {
        Freq = freq;
        Ampl = ampl;
        Dco = dco;
        Squ = squ;
    }
}

public class HeatValues : BaseDeviceValues
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="outputOn">Вкл</param>
    /// <param name="outputOff">Выкл</param>
    public HeatValues(string outputOn, string outputOff) : base(outputOn, outputOff)
    {
        //тут ничего не будет не отвелкайся
    }
}

public class SupplyValues : BaseDeviceValues
{
    public string Voltage { get; set; }
    public string Current { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="voltage">Напряжение</param>
    /// <param name="current">Ток</param>
    public SupplyValues(string voltage, string current, string outputOn, string outputOff) : base(outputOn, outputOff)
    {
        Voltage = voltage;
        Current = current;
    }
}