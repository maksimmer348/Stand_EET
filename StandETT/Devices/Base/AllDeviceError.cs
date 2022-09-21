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
}