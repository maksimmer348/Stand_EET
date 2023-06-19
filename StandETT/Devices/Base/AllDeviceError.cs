namespace StandETT;

public class AllDeviceError
{
    public bool ErrorPort { get; set; } = false;
    public bool ErrorDevice { get; set; } = false;
    public bool ErrorTerminator { get; set; } = false;
    public bool ErrorReceive { get; set; } = false;
    public bool ErrorParam { get; set; } = false;
    public bool ErrorLength { get; set; } = false;
    public bool ErrorTimeout { get; set; } = false;

    public void ResetAllError()
    {
        ErrorPort = false;
        ErrorDevice = false;
        ErrorTerminator = false;
        ErrorReceive = false;
        ErrorParam = false;
        ErrorLength = false;
        ErrorTimeout = false;
    }
    
    public bool CheckIsUnselectError(DeviceErrors e = DeviceErrors.All)
    {
        if (e == DeviceErrors.ErrorPort)
            return ErrorDevice || ErrorTerminator || ErrorReceive || ErrorParam || ErrorLength || ErrorTimeout;
        if (e == DeviceErrors.ErrorDevice)
            return ErrorPort || ErrorTerminator || ErrorReceive || ErrorParam || ErrorLength || ErrorTimeout;
        if (e == DeviceErrors.ErrorTerminator)
            return ErrorPort || ErrorDevice || ErrorReceive || ErrorParam || ErrorLength || ErrorTimeout;
        if (e == DeviceErrors.ErrorReceive)
            return ErrorPort || ErrorDevice || ErrorTerminator || ErrorParam || ErrorLength || ErrorTimeout;
        if (e == DeviceErrors.ErrorParam)
            return ErrorPort || ErrorDevice || ErrorTerminator || ErrorReceive || ErrorLength || ErrorTimeout;
        if (e == DeviceErrors.ErrorLength)
            return ErrorPort || ErrorDevice || ErrorTerminator || ErrorReceive || ErrorParam || ErrorTimeout;
        if (e == DeviceErrors.ErrorTimeout)
            return ErrorPort || ErrorDevice || ErrorTerminator || ErrorReceive || ErrorParam || ErrorLength;
        return ErrorPort || ErrorDevice || ErrorTerminator || ErrorReceive || ErrorParam || ErrorLength ||
               ErrorTimeout;
    }
}

public enum DeviceErrors
{
    All = 0,
    ErrorPort,
    ErrorDevice,
    ErrorTerminator,
    ErrorReceive,
    ErrorParam,
    ErrorLength,
    ErrorTimeout,
}