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
    public decimal MaxTemperature { get; set; }
    public decimal PercentAccuracyTemperature { get; set; }
    public decimal MaxVoltageIn { get; set; }

    private decimal maxVoltageOut1;

    public decimal MaxVoltageOut1
    {
        get => maxVoltageOut1;
        set
        {
            maxVoltageOut1 = value;
            PrepareMaxVoltageOut1 = value;
        }
    }

    private decimal maxVoltageOut2;

    public decimal MaxVoltageOut2
    {
        get => maxVoltageOut2;
        set
        {
            maxVoltageOut2 = value;
            PrepareMaxVoltageOut2 = value;
        }
    }

    public decimal MaxCurrentIn { get; set; }


    //максимальные значения во время замера 0
    public decimal PrepareMaxCurrentIn { get; set; }
    public decimal PrepareMaxVoltageOut1 { get; set; }
    public decimal PrepareMaxVoltageOut2 { get; set; }

    private bool enableTypeVipName = true;

    public bool EnableTypeVipName
    {
        get => enableTypeVipName;
        set => Set(ref enableTypeVipName, value);
    }

    public decimal PercentAccuracyVoltages { get; set; }

    public decimal PercentAccuracyCurrent { get; set; }
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
    //  public BaseDeviceValues SmallLoadValues { get; set; }
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
        else if (decimal.Parse(VoltMaxLimit) == 0.1M)
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
        set { mode = value; }
    }

    //

    private string currMaxLimit;

    public string CurrMaxLimit
    {
        get { return currMaxLimit; }
        set { currMaxLimit = value; }
    }


    private string termocoupleType;

    public string TermocoupleType
    {
        get { return termocoupleType; }
        set { termocoupleType = value; }
    }
    //

    public ThermoCurrentMeterValues(string currMaxLimit, string termocoupleType, string outputOn,
        string outputOff) : base(outputOn, outputOff)
    {
        CurrMaxLimit = currMaxLimit;
        TermocoupleType = termocoupleType;
    }

    public void SetFuncGDM()
    {
        if (Mode == ModeGdm.Current)
        {
            ReturnFuncGDM = "5";

            if (decimal.Parse(CurrMaxLimit) == 0.010M)
            {
                ReturnCurrGDM = "1";
            }
            else if (decimal.Parse(CurrMaxLimit) == 0.100M)
            {
                ReturnCurrGDM = "2";
            }
            else if (decimal.Parse(CurrMaxLimit) == 1)
            {
                ReturnCurrGDM = "3";
            }
            else if (decimal.Parse(CurrMaxLimit) == 10)
            {
                ReturnFuncGDM = "3";
                ReturnCurrGDM = "1";
            }
        }

        else if (Mode == ModeGdm.Voltage)
        {
            ReturnFuncGDM = "1";
            ReturnFuncGDM = string.Empty;
        }

        else if (Mode == ModeGdm.Themperature)
        {
            ReturnFuncGDM = "9";
            ReturnFuncGDM = string.Empty;
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