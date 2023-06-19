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
    public bool ErrorThread { get; set; } = false;

    public void ResetAllError()
    {
        ErrorPort = false;
        ErrorDevice = false;
        ErrorTerminator = false;
        ErrorReceive = false;
        ErrorParam = false;
        ErrorLength = false;
        ErrorTimeout = false;
        ErrorThread = false;
    }
    
    public bool CheckIsUnselectError(DeviceErrors e = DeviceErrors.All)
    {
        return e switch
        {
            DeviceErrors.ErrorPort => ErrorPort,
            DeviceErrors.ErrorDevice => ErrorDevice,
            DeviceErrors.ErrorTerminator => ErrorTerminator,
            DeviceErrors.ErrorReceive => ErrorReceive,
            DeviceErrors.ErrorParam => ErrorParam,
            DeviceErrors.ErrorLength => ErrorLength,
            DeviceErrors.ErrorTimeout => ErrorTimeout,
            DeviceErrors.ErrorThread => ErrorThread,
            _ => ErrorPort || ErrorDevice || ErrorTerminator || ErrorReceive || ErrorParam || ErrorLength ||
                 ErrorTimeout
        };
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
    ErrorThread
}