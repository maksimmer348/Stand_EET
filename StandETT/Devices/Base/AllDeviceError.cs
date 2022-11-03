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


    public bool CheckIsUnselectError(DeviceErrors e = DeviceErrors.All)
    {
        return e switch
        {
            DeviceErrors.ErrorPort => ErrorDevice || ErrorTerminator || ErrorReceive || ErrorParam || ErrorLength ||
                                      ErrorTimeout,
            DeviceErrors.ErrorDevice => ErrorPort || ErrorTerminator || ErrorReceive || ErrorParam || ErrorLength ||
                                        ErrorTimeout,
            DeviceErrors.ErrorTerminator => ErrorPort || ErrorDevice || ErrorReceive || ErrorParam || ErrorLength ||
                                            ErrorTimeout,
            DeviceErrors.ErrorReceive => ErrorPort || ErrorDevice || ErrorTerminator || ErrorParam || ErrorLength ||
                                         ErrorTimeout,
            DeviceErrors.ErrorParam => ErrorPort || ErrorDevice || ErrorTerminator || ErrorReceive || ErrorLength ||
                                       ErrorTimeout,
            DeviceErrors.ErrorLength => ErrorPort || ErrorDevice || ErrorTerminator || ErrorReceive || ErrorParam ||
                                        ErrorTimeout,
            DeviceErrors.ErrorTimeout => ErrorPort || ErrorDevice || ErrorTerminator || ErrorReceive || ErrorParam ||
                                         ErrorLength,
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
}