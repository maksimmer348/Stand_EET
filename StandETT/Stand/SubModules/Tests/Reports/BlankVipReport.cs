namespace StandETT;

public struct BlankVipReport
{
    public int VipId { get; set; }
    public object VipNum { get; set; }
    public decimal VoltageOut1 { get; set; }
    public decimal VoltageOut2 { get; set; }
    public decimal CurrentIn { get; set; }
    public decimal VoltageIn { get; set; }
    public decimal Temperature { get; set; }
   
}