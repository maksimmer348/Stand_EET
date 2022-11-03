using System.Collections.Generic;

using StandETT;
public class TempChecks
{
    private List<bool> list = new();

    public void Add(bool value)
    {
        list.Add(value);
    }

    public bool IsOk => list.TrueForAll(e => e);

    public static TempChecks Start() => new TempChecks();

    protected TempChecks()
    {
    }
}