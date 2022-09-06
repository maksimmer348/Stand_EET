using System;

namespace StandETT;

public class LoopTester : Notify
{
    #region Время испытаний

    public void SetTimesTest(TimeSpan all, TimeSpan interval)
    {
        SetTestAllTime = all;
        SetTestIntervalTime = interval;
    }

    private DateTime testStartTime;

    /// <summary>
    /// Время начала теста
    /// </summary>
    public DateTime TestStartTime
    {
        get => testStartTime;
        set => Set(ref testStartTime, value);
    }

    private DateTime testEndTime;

    /// <summary>
    /// Время окончания теста
    /// </summary>
    public DateTime TestEndTime
    {
        get => testEndTime;
        set => Set(ref testEndTime, value);
    }

    private DateTime nextMeasurementIn;

    /// <summary>
    /// Время следующего замера
    /// </summary>
    public DateTime NextMeasurementIn
    {
        get => nextMeasurementIn;
        set => Set(ref nextMeasurementIn, value);
    }

    private TimeSpan setTestAllTime;

    /// <summary>
    /// Устанлвка сколько будет длится тест
    /// </summary>
    public TimeSpan SetTestAllTime
    {
        get => setTestAllTime;
        set => Set(ref setTestAllTime, value);
    }

    private TimeSpan setTestIntervalTime;

    /// <summary>
    /// Установка через какой интервал времени будет производится замер 
    /// </summary>
    public TimeSpan SetTestIntervalTime
    {
        get => setTestIntervalTime;
        set => Set(ref setTestIntervalTime, value);
    }

    private TimeSpan testLeftEndTime;

    /// <summary>
    /// Время коца теста = DateTime.Now + SetTestAllTime
    /// </summary>
    public TimeSpan TestLeftEndTime
    {
        get => testLeftEndTime;
        set => Set(ref testLeftEndTime, value);
    }

    #endregion
    
    
    public LoopTester(Stand1 stand1)
    {
        throw new System.NotImplementedException();
    }
}