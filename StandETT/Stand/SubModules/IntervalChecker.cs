using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace StandETT;

public class IntervalChecker
{
    private Stopwatch watch;
    
    private long last;
    
    private bool autoReset;
    public double Interval { get; set; }
    public int Count { get; private set; }
    public float IntervalRange { get; set; } = 1;

    public double Elapsed { get; private set; }

    //TODO autoReset = false
    public IntervalChecker(double interval, bool autoReset = false)
    {
        watch = new Stopwatch();
        Interval = interval;
        this.autoReset = autoReset;
        Start();
    }

    public bool Check()
    {
        Elapsed += TimeConvert.TicksToSeconds(watch.ElapsedTicks - last);
        last = watch.ElapsedTicks;
        
        var interval = Interval * (Count + 1);
        var intervalMin = interval - IntervalRange;
        var intervalMax = interval + IntervalRange;
        // if (Elapsed >= Interval * (Count + 1))
        if (Elapsed >= intervalMin ||Elapsed >= intervalMax)
        {
            Count += 1;
            if (autoReset) Reset();
            return true;
        }

        return false;
    }

    public void Reset()
    {
        last = watch.ElapsedTicks;
        Elapsed = 0;
        Count = 0;
    }

    public void Stop()
    {
        watch.Stop();
        Elapsed = 0;
        Count = 0;
    }

    public void Restart()
    {
        watch.Restart();
        Reset();
    }

    public void Restart(float newInterval)
    {
        Interval = newInterval;
        Restart();
    }

    public void Start()
    {
        watch.Start();
        Reset();
    }
}

public static class TimeConvert
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double TicksToSeconds(double ticks) => ticks / Stopwatch.Frequency;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double TicksToMs(double ticks) => (ticks / Stopwatch.Frequency) * 1000;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SecondsToMs(float seconds) => seconds * 1000;
}