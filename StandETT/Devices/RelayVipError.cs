namespace StandETT;

//TODO Уточнить про эти ошибки
public class RelayVipError
{
    public bool CurrentInErr { get; set; } = false;
    public bool VoltageOut1High { get; set; } = false;
    public bool VoltageOut1Low { get; set; } = false;
    public bool VoltageOut2High { get; set; } = false;
    public bool VoltageOut2Low { get; set; } = false;
    public bool TemperatureIn { get; set; } = false;
    public bool TemperatureOut { get; set; } = false;
    
    public bool CheckIsUnselectError(VipError e = VipError.All)
    {
        return CurrentInErr ||
               VoltageOut1High ||
               VoltageOut1Low ||
               VoltageOut2High ||
               VoltageOut2Low ||
               TemperatureIn ||
               TemperatureOut;
    }

    public void ResetAllError()
    {
        CurrentInErr = false;
        VoltageOut1High = false;
        VoltageOut1Low = false;
        VoltageOut2High = false;
        VoltageOut2Low = false;
        TemperatureIn = false;
        TemperatureOut = false;
    }
}

public enum VipError
{
    All = 0,
    CurrentInHigh,
    VoltageOut1High,
    VoltageOut1Low,
    VoltageOut2High,
    VoltageOut2Low,
    TemperatureIn,
    TemperatureOut
}