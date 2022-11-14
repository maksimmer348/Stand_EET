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

    private string specifications = "ЯКЛЮ.436638.001 ТУ";

    /// <summary>
    /// Тип Випа
    /// </summary>
    public string Specifications
    {
        get => specifications;
        set => Set(ref specifications, value);
    }

    #region Значения для Випов

    //максимальные значения во время цикла испытаниий 1...n, они означают ошибку
    public decimal MaxTemperature { get; set; }
    public decimal PercentAccuracyTemperature { get; set; }

    //

    //напряжение вх
    public decimal MaxVoltageIn { get; set; }

    //проценты погрещностей
    public decimal PercentAccuracyVoltages { get; set; }


    //1 канал 
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

    //2 канал
    public bool VoltageOut2Using { get; set; }

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

    //ток вх
    public decimal MaxCurrentIn { get; set; }
    public decimal PercentAccuracyCurrent { get; set; }

    //максимальные значения во время замера 0
    public decimal PrepareMaxCurrentIn { get; set; }
    
    public decimal AvailabilityMaxCurrentIn { get; set; }
    
    public decimal PrepareMaxVoltageOut1 { get; set; }
    public decimal PrepareMaxVoltageOut2 { get; set; }

    //
    private bool enableTypeVipName = true;

    public bool EnableTypeVipName
    {
        get => enableTypeVipName;
        set => Set(ref enableTypeVipName, value);
    }

    public double ZeroTestInterval { get; set; }
    public TimeSpan TestIntervalTime { get; set; }
    public TimeSpan TestAllTime { get; set; }
    
 

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
    public VoltCurrentMeterValues VoltCurrentValues { get; set; }
    public VoltMeterValues VoltValues { get; set; }
    
     public BaseDeviceValues SmallLoadValues { get; set; }
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

public class VoltCurrentMeterValues : BaseDeviceValues
{
    [JsonIgnore] public string ReturnFuncGDM { get; set; }
    [JsonIgnore] public string ReturnCurrGDM { get; set; }
    
    [JsonIgnore] public string ReturnVoltGDM { get; set; }

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
        set
        {
            SetFuncGDM();
            currMaxLimit = value;
        }
    }

    private string voltMaxLimit;

    public string VoltMaxLimit
    {
        get { return voltMaxLimit; }
        set
        {
            SetFuncGDM();
            voltMaxLimit = value;
        }
    }
    

    private string termocoupleType;

    public string TermocoupleType
    {
        get { return termocoupleType; }
        set { termocoupleType = value; }
    }
    //

    public VoltCurrentMeterValues(string currMaxLimit, string voltMaxLimit, string termocoupleType, string outputOn,
        string outputOff) : base(outputOn, outputOff)
    {
        CurrMaxLimit = currMaxLimit;
        VoltMaxLimit = voltMaxLimit;
        TermocoupleType = termocoupleType;
        SetFuncGDM();
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
            if (decimal.Parse(VoltMaxLimit) == 0.1M)
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

    public string VoltageAvailability { get; set; }

    public string CurrentAvailability { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="voltage">Напряжение</param>
    /// <param name="current">Ток</param>
    public SupplyValues(string voltage, string current, string voltageAvailability, string currentAvailability,
        string outputOn, string outputOff) : base(outputOn, outputOff)
    {
        Voltage = voltage;
        Current = current;
        VoltageAvailability = voltageAvailability;
        CurrentAvailability = currentAvailability;
    }
}