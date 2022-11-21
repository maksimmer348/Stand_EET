using System;

namespace StandETT;


public class TimeMachine : Notify
{
    private static object syncRoot = new();
    private static TimeMachine instance;
    
    public static TimeMachine getInstance()
    {
        if (instance == null)
        {
            lock (syncRoot)
            {
                if (instance == null)
                    instance = new TimeMachine();
            }
        }
        return instance;
    }
    
    private string countTimes = "3";

    public string CountChecked
    {
        get => countTimes;
        set => Set(ref countTimes, value);
    }

    private string allTimeChecked = "3000";

    public string AllTimeChecked
    {
        get => allTimeChecked;
        set => Set(ref allTimeChecked, value);
    }
}