namespace StandETT;

//TODO Уточнить про эти ошибки
public class RelayVipError
{
    public bool CurrentInHigh { get; set; } = false;

    public bool VoltageOut1High { get; set; } = false;
    public bool VoltageOut1Low { get; set; } = false;

    public bool VoltageOut2High { get; set; } = false;
    public bool VoltageOut2Low { get; set; } = false;
    public bool TemperatureHigh { get; set; }

    public bool CheckIsUnselectError(VipError e = VipError.All)
    {
        return CurrentInHigh ||
               VoltageOut1High ||
               VoltageOut1Low ||
               VoltageOut2High ||
               VoltageOut2Low;
    }

    public void ResetAllError()
    {
        CurrentInHigh = false;
        VoltageOut1High = false;
        VoltageOut1Low = false;
        VoltageOut2High = false;
        VoltageOut2Low = false;
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
}