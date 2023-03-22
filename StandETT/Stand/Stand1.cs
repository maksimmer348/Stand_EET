using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace StandETT;

public class Stand1 : Notify
{
    #region --Вспомогательное/Отладка

    Stopwatch s0 = new();
    Stopwatch s = new();
    Stopwatch s1 = new();

    Stopwatch sTests = new();

    #endregion

    //---

    #region Создание устройств

    /// <summary>
    /// Создание устройств стенда
    /// </summary>
    private CreatorAllDevicesAndLib creatorAllDevicesAndLib;

    #endregion

    //---

    #region Библиотека стенда

    BaseLibCmd libCmd = BaseLibCmd.getInstance();

    #endregion


    //---

    #region --Устройства стенда

    //--

    private readonly ObservableCollection<BaseDevice> allDevices = new();
    public readonly ReadOnlyObservableCollection<BaseDevice> AllDevices;

    //--

    private readonly ObservableCollection<BaseDevice> devices = new();
    public readonly ReadOnlyObservableCollection<BaseDevice> Devices;
    private BaseDevice currentDevice;
    private List<BaseDevice> currentDevices = new();

    //
    private ObservableCollection<Vip> vipsTested = new();

    private ObservableCollection<Vip> vipsStopped = new();

    private ObservableCollection<RelayVip> relayVipsTested = new();

    //--


    MainRelay mainRelay = MainRelay.GetInstance();
    ConfigTypeVip cfgTypeVips = ConfigTypeVip.getInstance();

    #region --Тестируемые Випы стенда

    private readonly ObservableCollection<Vip> vips;
    public readonly ReadOnlyObservableCollection<Vip> Vips;
    private ObservableCollection<RelayVip> allRelayVips = new();

    #endregion

    #region --Настройки таймера

    private IntervalChecker firstIntervalMeasurementCycle;
    private IntervalChecker intervalMeasurementCycle;
    private IntervalChecker lastIntervalMeasurementStop;

    #endregion


    //--

    #endregion

    //---

    #region --Статусы стенда

    private string reportNum;

    public string ReportNum
    {
        get => reportNum;
        set => Set(ref reportNum, value);
    }


    private Brush progressColor;

    public Brush ProgressColor
    {
        get => progressColor;
        set => Set(ref progressColor, value);
    }

    private Brush progressSubColor;

    public Brush ProgressSubColor
    {
        get => progressSubColor;
        set => Set(ref progressSubColor, value);
    }

    private Brush progressResetColor;

    public Brush ProgressResetColor
    {
        get => progressResetColor;
        set => Set(ref progressResetColor, value);
    }

    public TypeOfTestRun runTest;

    /// <summary>
    /// Какой сейчас тест идет
    /// </summary>
    public TypeOfTestRun TestRun
    {
        get => runTest;
        set => Set(ref runTest, value);
    }

    public string subRunText;

    /// <summary>
    /// Какой сейчас sub тест идет
    /// </summary>
    public string SubTestText
    {
        get => subRunText;
        set => Set(ref subRunText, value);
    }


    public BaseDevice testCurrentDevice;

    /// <summary>
    /// Чей сейчас тест идет
    /// </summary>
    public BaseDevice TestCurrentDevice
    {
        get => testCurrentDevice;
        set => Set(ref testCurrentDevice, value);
    }

    public Vip testCurrentVip;

    public Vip TestCurrentVip
    {
        get => testCurrentVip;
        set => Set(ref testCurrentVip, value);
    }

    private double percentCurrentTest;

    /// <summary>
    /// На соклько процентов выполнен текущий тест
    /// </summary>
    public double PercentCurrentTest
    {
        get => percentCurrentTest;
        set => Set(ref percentCurrentTest, value);
    }

    private double percentCurrentSubTest;

    public double PercentCurrentSubTest
    {
        get => percentCurrentSubTest;
        set => Set(ref percentCurrentSubTest, value);
    }

    private double percentCurrentReset;

    public double PercentCurrentReset
    {
        get => percentCurrentReset;
        set => Set(ref percentCurrentReset, value);
    }

    private string currentCountChecked;

    public string CurrentCountChecked
    {
        get => currentCountChecked;
        set => Set(ref currentCountChecked, value);
    }

    #endregion

    //---

    #region --События устройств

    /// <summary>
    /// Событие проверки коннекта к устройству
    /// </summary>
    public event Action<BaseDevice, bool> CheckPort;

    /// <summary>
    /// Событие проверки коннекта к устройству
    /// </summary>
    public event Action<BaseDevice, bool, string> CheckDevice;

    /// <summary>
    /// Событие приема данных с устройства
    /// </summary>
    public event Action<BaseDevice, string> Receive;

    public event Action<bool> OpenActionWindow;
    public event Action<bool> OpenErrorVipWindow;

    #endregion

    #region События таймера

    public event Action<string> timerErrorMeasurement;
    public Action<string> timerErrorDevice;
    public event Action<string> timerOk;

    #endregion

    //---

    #region --Токены

    CancellationTokenSource ctsAllCancel = new();
    CancellationTokenSource ctsReceiveDevice = new();
    CancellationTokenSource ctsConnectDevice = new();

    #endregion

    //---

    #region --Время

    private System.Timers.Timer timerTest;

    private string timeTestStart;

    public string TimeTestStart
    {
        get => timeTestStart;
        set => Set(ref timeTestStart, value);
    }

    private string timeTestNext;

    public string TimeTestNext
    {
        get => timeTestNext;
        set => Set(ref timeTestNext, value);
    }

    private string timeTestStop;

    public string TimeTestStop
    {
        get => timeTestStop;
        set => Set(ref timeTestStop, value);
    }

    #endregion

    #region --Конструктор --ctor

    public Stand1()
    {
        creatorAllDevicesAndLib = new();

        //-
        libCmd.DeviceCommands = creatorAllDevicesAndLib.SetLib();
        //-
        cfgTypeVips.TypeVips = creatorAllDevicesAndLib.SetTypeVips();
        //-

        var createAllDevices = creatorAllDevicesAndLib.SetDevices();
        allDevices = new(createAllDevices);
        AllDevices = new(allDevices);
        //-
        devices = new(allDevices.Where(d => d is not MainRelay && d is not RelayVip || d.Name.Contains("SL")));
        Devices = new(devices);
        //-
        foreach (var device in createAllDevices)
        {
            if (device is RelayVip r)
            {
                allRelayVips.Add(r);
            }
        }

        vips = new(creatorAllDevicesAndLib.SetVips(allRelayVips.ToList()));
        Vips = new(vips);

        // timeMachine = creatorAllDevicesAndLib.SetTime();
        //-
        creatorAllDevicesAndLib.PortConnecting += Port_Connecting;
        creatorAllDevicesAndLib.DeviceReceiving += Device_Receiving;
        creatorAllDevicesAndLib.DeviceError += Device_Error;
        //-
        SetStatusStand(0, testRun: TypeOfTestRun.Stop);
        //-
    }

    #endregion

    //---

    #region Установка статусов стенда и устройств

    /// <summary>
    /// Установка статуса стенда 
    /// </summary>
    /// <param name="percent">Процент выполнения задачи (default = 0)</param>
    /// <param name="testRun">Текущая задача (default = Stop)</param>
    /// <param name="device">Текущий прибор (default = null)</param>
    public void SetStatusStand(double percent = 0, Brush pColor = null, TypeOfTestRun testRun = TypeOfTestRun.None,
        BaseDevice device = null,
        int currentCheck = 0)
    {
        CurrentCountChecked =
            currentCheck == 0 ? null : $"Попытка: {currentCheck.ToString()}-я из {currentCheck}";
        ProgressColor = pColor ?? Brushes.Green;
        if (percent > 0)
        {
            PercentCurrentTest = percent;
        }

        if (testRun != TypeOfTestRun.None)
        {
            TestRun = testRun;
        }

        TestCurrentDevice = device;
    }

    private int prevPrioritySubTest = 5;

    /// <summary>
    /// Установка приоритетов статусов стенда
    /// </summary>
    /// <param name="currentPrioritySubTest">Приоритет уведомления</param>
    /// <param name="currentSubTest">Текущий субтест (default = null)</param>
    /// <param name="currentDeviceSubTest">Проверяемое устройство (default = null)</param>
    /// <param name="percentSubTest">Процент текущего субтеста (default = -1)</param>
    /// <param name="currentCountCheckedSubTest">Количемтво попыток текущего субтеста (default = null)</param>
    /// <param name="colorSubTest">Цвет прогресс бара текущего субтеста (default = default)</param>
    /// <param name="currentVipSubTest"></param>
    /// <param name="clearAll">Исполдьзовалть ли значения по умолчанию</param>
    private void SetPriorityStatusStand(int currentPrioritySubTest, string currentSubTest = null,
        BaseDevice currentDeviceSubTest = null, float percentSubTest = -1,
        string currentCountCheckedSubTest = null, SolidColorBrush colorSubTest = default, Vip currentVipSubTest = null,
        bool clearAll = false)
    {
        if (currentPrioritySubTest <= prevPrioritySubTest)
        {
            if (clearAll)
            {
                if (currentPrioritySubTest == 0)
                {
                    prevPrioritySubTest = 5;
                }

                SubTestText = currentSubTest;
                TestCurrentDevice = currentDeviceSubTest;
                TestCurrentVip = currentVipSubTest;
                PercentCurrentSubTest = percentSubTest < 0 ? 0 : percentSubTest;
                CurrentCountChecked = currentCountCheckedSubTest;
                ProgressSubColor = colorSubTest;
                clearAll = false;
                return;
            }

            prevPrioritySubTest = currentPrioritySubTest;

            if (!string.IsNullOrEmpty(currentSubTest))
            {
                SubTestText = currentSubTest;
            }

            if (currentDeviceSubTest != null)
            {
                TestCurrentDevice = currentDeviceSubTest;
            }

            if (currentVipSubTest != null)
            {
                TestCurrentVip = currentVipSubTest;
            }

            if (percentSubTest > -1)
            {
                PercentCurrentSubTest = percentSubTest;
            }

            if (colorSubTest != default)
            {
                ProgressSubColor = colorSubTest;
            }

            if (!string.IsNullOrEmpty(currentCountCheckedSubTest))
            {
                CurrentCountChecked = currentCountCheckedSubTest;
            }
        }
    }

    #endregion

    //---

    #region --Проверки--Тесты

    bool resetAll = false;
    bool stopMeasurement = false;
    public bool IsResetAll = false;

    private string captionAction;

    public string CaptionAction
    {
        get => captionAction;
        set => Set(ref captionAction, value);
    }

    private string errorOutput;

    public string ErrorOutput
    {
        get => errorOutput;
        set => Set(ref errorOutput, value);
    }

    private string errorMessage;

    public string ErrorMessage
    {
        get => errorMessage;
        set => Set(ref errorMessage, value);
    }

    private double percentStopedTest;

    /// <summary>
    /// На соклько процентов выполнен текущий тест
    /// </summary>
    public double PercentStopedTest
    {
        get => percentStopedTest;
        set => Set(ref percentStopedTest, value);
    }

    //

    private string captionVipAction;

    public string CaptionVipAction
    {
        get => captionVipAction;
        set => Set(ref captionVipAction, value);
    }

    private string errorVipOutput;

    public string ErrorVipOutput
    {
        get => errorVipOutput;
        set => Set(ref errorVipOutput, value);
    }

    private string errorVipMessage;

    public string ErrorVipMessage
    {
        get => errorVipMessage;
        set => Set(ref errorVipMessage, value);
    }

    //

    //--stop--resetall--allreset
    //Остановка всех тестов
    public async Task ResetAllTests()
    {
        IsResetAll = true;
        resetAll = true;

        ResetMeasurementCycle();

        ctsAllCancel?.Cancel();
        ctsConnectDevice?.Cancel();
        ctsReceiveDevice?.Cancel();

        if (testVipPlay)
        {
            if (TestCurrentVip != null && report != null)
            {
                await report.CreateErrorReport(TestCurrentVip);
            }
        }

        TestRun = TypeOfTestRun.Stopped;

        //OpenActionWindow?.Invoke(true);

        PercentCurrentTest = 30;
        ProgressColor = Brushes.Green;

        await Task.Delay(TimeSpan.FromMilliseconds(1000));

        ctsAllCancel = new CancellationTokenSource();
        ctsConnectDevice = new CancellationTokenSource();
        ctsReceiveDevice = new CancellationTokenSource();

        resetAll = false;

        TempChecks t = TempChecks.Start();
        PercentCurrentReset = 0;
        ProgressResetColor = Brushes.Green;
        ErrorMessage = null;
        ErrorOutput = null;

        var percent = 100;

        stopMeasurement = true;

        foreach (var device in devices)
        {
            (BaseDevice outputDevice, bool outputResult) resultOutput = (device, false);

            if (device.Name.Contains("SL"))
            {
                //--
                ErrorMessage = $"Отключение выхода #{1} устройства {device.IsDeviceType}/{device.Name}...";
                resultOutput = await OutputDevice(device, 1, 1, t: t, on: false);
                if (!resultOutput.outputResult)
                {
                    ErrorOutput += $"Выход #{1}/{device.ErrorStatus}\n";
                    ProgressResetColor = Brushes.Red;
                    continue;
                }

                //--

                ErrorMessage = $"Отключение выхода #{2} устройства {device.IsDeviceType}/{device.Name}...";
                resultOutput = await OutputDevice(device, 2, t: t, on: false);
                if (!resultOutput.outputResult)
                {
                    ErrorOutput += $"Выход #{2}/{device.ErrorStatus}\n";
                    ProgressResetColor = Brushes.Red;
                    continue;
                }

                //--

                ErrorMessage = $"Отключение выхода #{3} устройства {device.IsDeviceType}/{device.Name}...";
                resultOutput = await OutputDevice(device, 3, t: t, on: false);
                TempChecks t4 = TempChecks.Start();
                if (!resultOutput.outputResult)
                {
                    ErrorOutput += $"Выход #{3}/{device.ErrorStatus}\n";
                    ProgressResetColor = Brushes.Red;
                    continue;
                }

                //--

                ErrorMessage = $"Отключение выхода #{4} устройства {device.IsDeviceType}/{device.Name}...";
                resultOutput = await OutputDevice(device, 4, t: t, on: false);
                if (!resultOutput.outputResult)
                {
                    ErrorOutput += $"Выход #{4}/{device.ErrorStatus}\n";
                    ProgressResetColor = Brushes.Red;
                    continue;
                }

                //--
            }
            else
            {
                resultOutput = await OutputDevice(device, t: t, on: false);
                ErrorMessage = $"Отключение выхода устройства {device.IsDeviceType}/{device.Name}...";

                if (!resultOutput.outputResult)
                {
                    ErrorOutput += $"{device.ErrorStatus}\n";
                    ProgressResetColor = Brushes.Red;
                }
            }

            PercentCurrentReset += Math.Round((1 / (float)devices.Count) * percent);
        }

        PercentCurrentTest = 30;

        if (relayVipsTested.Any())
        {
            percent = 50;
            mainRelay.Relays = relayVipsTested;

            foreach (var relay in relayVipsTested)
            {
                ErrorMessage = $"Отключение выхода реле Випа {relay.Name}...";

                (BaseDevice outputDevice, bool outputResult) resultOutput = await OutputDevice(relay, t: t, on: false);
                if (!resultOutput.outputResult)
                {
                    ErrorOutput += $"{relay.ErrorStatus}\n";
                    ProgressResetColor = Brushes.Red;
                }

                PercentCurrentReset += Math.Round((1 / (float)relayVipsTested.Count) * percent);
            }
        }

        PercentCurrentTest = 70;

        switch (t.IsOk)
        {
            case true:
                PercentCurrentTest = 100;
                ErrorMessage = "Все выходы доступных/инициаизировных устройств были благополучно отключены";

                //
                SetPriorityStatusStand(1, percentSubTest: 100, colorSubTest: Brushes.Green, clearAll: true);
                TestRun = TypeOfTestRun.Stop;
                PercentCurrentReset = 100;
                //
                break;

            case false:
                PercentCurrentTest = 100;
                ProgressColor = Brushes.Red;

                ErrorMessage =
                    "Осторожно! Следующие выходы устройств не были отключены, тк к устройствам нет доступа " +
                    "или они неправильно индентифицированы";
                ErrorOutput += "Выходы остальных устройств отключены\n";

                //
                SetPriorityStatusStand(1, percentSubTest: 100, colorSubTest: Brushes.Red, clearAll: true);
                CaptionAction = "Стенд остановлен, с ошибкой!";
                TestRun = TypeOfTestRun.Stop;
                PercentCurrentReset = 100;
                //
                break;
        }

        // await ResetAllVips();

        stopMeasurement = false;
        IsResetAll = false;
    }

    private void ResetMeasurementCycle()
    {
        //
        SetPriorityStatusStand(1, $"Сброс таймера испытаний", percentSubTest: 0,
            colorSubTest: Brushes.Green, clearAll: true);
        //
        firstIntervalMeasurementCycle?.Stop();
        intervalMeasurementCycle?.Stop();
        lastIntervalMeasurementStop?.Stop();

        if (timerTest != null)
        {
            timerTest.Elapsed -= TimerOnElapsed;
            timerTest.Stop();
        }

        //
        SetPriorityStatusStand(1, $"Сброс таймера испытаний, ок!", percentSubTest: 100,
            colorSubTest: Brushes.Green, clearAll: true);
        //
    }

    /// <summary>
    /// Установка типа випов
    /// </summary>
    /// <param name="selectTypeVip">ВЫбранный тип Випа</param>
    public void SetTypeVips(TypeVip selectTypeVip)
    {
        foreach (var vip in vips)
        {
            vip.Type = selectTypeVip;
        }
    }

    //--приборы--проверка--check--devices--primary
    public async Task<bool> PrimaryCheckDevices(int innerCountCheck = 3, int innerDelay = 3000)
    {
        var supplyLoads = new List<BaseDevice>();
        //
        TestRun = TypeOfTestRun.PrimaryCheckDevices;
        ProgressColor = Brushes.Green;
        PercentCurrentTest = 30;
        SetPriorityStatusStand(0, clearAll: true);
        //

        var t = TempChecks.Start();

        //TODO убрать когда эти приборы появятся в стенде
        currentDevice = devices.GetTypeDevice<Thermometer>();
        devices.Remove(currentDevice);
        currentDevice = devices.GetTypeDevice<Heat>();
        devices.Remove(currentDevice);
        //TODO убрать когда эти приборы появятся в стенде

        await CheckConnectPorts(devices.ToList(), t: t);

        //вычленяем нагрузки из общего списка приборов тк они висят на одной шине и
        //обычные методы проверки порта и команд не подходят
        supplyLoads = new(devices.Where(d => d.Name.Contains("SL")));
        //проверка порта первой нагрузки тк они все висят на одном порту
        await CheckConnectPort(supplyLoads[0], t: t);

        if (t.IsOk)
        {
            PercentCurrentTest = 70;
            t = TempChecks.Start();
            //проверка внешних приборов
            await WriteIdentCommands(devices.ToList(), "Status",
                countChecked: innerCountCheck, loopDelay: innerDelay, t: t);
            //проверка нагрузок
            foreach (var sl in supplyLoads)
            {
                await WriteIdentCommand(sl, "Status",
                    countChecked: innerCountCheck, loopDelay: innerDelay, t: t);
            }

            if (t.IsOk)
            {
                //
                SetPriorityStatusStand(1, $"запись команд в устройства, oк!", percentSubTest: 100,
                    colorSubTest: Brushes.Green, clearAll: true);
                TestRun = TypeOfTestRun.PrimaryCheckDevicesReady;
                PercentCurrentTest = 100;
                //
                return true;
            }
        }

        if (resetAll) return false;
        //
        SetPriorityStatusStand(1, $"запись команд в устройства, ошибка!", percentSubTest: 100,
            colorSubTest: Brushes.Red, clearAll: true);
        PercentCurrentTest = 100;
        ProgressColor = Brushes.Red;
        TestRun = TypeOfTestRun.Error;
        //
        throw new Exception("Одно или несколько устройств не ответили или ответили\nс ошибкой.\n");
    }

    //--випы--vips--relay--check--проверка--
    public async Task<bool> PrimaryCheckVips(int innerCountCheck = 3, int innerDelay = 3000)
    {
        s.Restart();
        //
        TestRun = TypeOfTestRun.PrimaryCheckVips;
        ProgressColor = Brushes.Green;
        PercentCurrentTest = 30;
        SetPriorityStatusStand(0, clearAll: true);
        //

        //предварительное создание класса отчета
        report = new ReportCreator();
        //проверки полей ввода имени файла отчета
        if (string.IsNullOrEmpty(ReportNum))
        {
            throw new Exception("Введите корректный номер отчета");
        }

        if (report.CheckHeadersReport(new HeaderReport(ReportNum, vips[0].Type)))
        {
            throw new Exception("Отчет с таким номером уже сущетвует");
        }

        TempChecks t = TempChecks.Start();
        await ResetAllVips(innerCountCheck, innerDelay);
        //предварительная настройка тестрировать ли вип => если у Випа есть имя то тестировать
        vipsTested = GetIsTestedVips();

        //проверки полей ввода випов
        if (!vipsTested.Any())
        {
            TestRun = TypeOfTestRun.Stop;
            PercentCurrentTest = 0;
            throw new Exception("Отсутвуют номера Випов!");
        }

        if (vipsTested.GroupBy(x => x.Name).Any(g => g.Count() > 1))
        {
            TestRun = TypeOfTestRun.Stop;
            PercentCurrentTest = 0;
            throw new Exception("Номера Випов не должны дублироватся!");
        }

        relayVipsTested.Clear();
        foreach (var vip in vipsTested)
        {
            relayVipsTested.Add(vip.Relay);
        }

        mainRelay.Relays = relayVipsTested;

        await CheckConnectPort(mainRelay, t: t);

        if (t.IsOk)
        {
            PercentCurrentTest = 70;
            t = TempChecks.Start();

            foreach (var relay in relayVipsTested)
            {
                Debug.WriteLine("Relay - " + relay.Name);
                await WriteIdentCommand(relay, "Status", countChecked: innerCountCheck, loopDelay: innerDelay, t: t);

                //TODO удалить после отладки
                // if (t.IsOk)
                // {
                //     await OutputDevice(relay, t: t, forcedOff: true, on: false);
                // }
                //
                // if (t.IsOk)
                // {
                //     await OutputDevice(relay, t: t);
                // }
                //TODO удалить после отладки
            }
            
            if (t.IsOk)
            {
                //
                SetPriorityStatusStand(1, $"запись команд в Випы, oк!", percentSubTest: 100,
                    colorSubTest: Brushes.Green, clearAll: true);

                TestRun = TypeOfTestRun.PrimaryCheckVipsReady;
                PercentCurrentTest = 100;
                //
                return true;
            }
        }

        if (resetAll) return false;
        //
        SetPriorityStatusStand(1, $"запись команд в Випы, ошибка!", percentSubTest: 100,
            colorSubTest: Brushes.Red, clearAll: true);
        PercentCurrentTest = 100;
        ProgressColor = Brushes.Red;
        TestRun = TypeOfTestRun.Error;
        //
        throw new Exception("Одно или несколько устройств  не ответили или ответили\nс ошибкой.\n");
    }

    //--предварительные--наличие--AvailabilityCheckVip
    public async Task<bool> AvailabilityCheckVip(int innerCountCheck = 3, int innerDelay = 200)
    {
        TimeTestStart = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        
        if (!vipsTested.Any())
        {
            throw new Exception("Отсутвуют инициализировнные Випы!");
        }
        //задаем репортер тут тк будет исолпьзоватся ерроррепортер
        try
        {
            await report.CreateHeadersReport(new HeaderReport(ReportNum, vips[0].Type));
        }
        catch (Exception e)
        {
            throw new Exception($"Ошибка {e.Message} - Файл отчета не был создан!");
        }
        
        //
        TestRun = TypeOfTestRun.AvailabilityCheckVip;
        ProgressColor = Brushes.Green;
        PercentCurrentTest = 10;
        SetPriorityStatusStand(0, clearAll: true);
        //

        TempChecks t = TempChecks.Start();

        //бп
        currentDevice = devices.GetTypeDevice<Supply>();
        var getSupplyValues = GetParameterForDevice().SupplyValues;
        await OutputDevice(currentDevice, t: t, on: false);

        if (t.IsOk)
            await SetCheckValueInDevice(currentDevice, "Set volt", getSupplyValues.VoltageAvailability,
                innerCountCheck, innerDelay, t, "Get volt");
        if (t.IsOk)
            await SetCheckValueInDevice(currentDevice, "Set curr", getSupplyValues.CurrentAvailability,
                innerCountCheck, innerDelay, t, "Get curr");

        await OutputDevice(currentDevice, t: t);

        //TODO уточнить остать тут или пернести вовне метода имзерений тк в отличчии от вольтамперметра пока что волтметр не меняет настройки
        //волтьтметра
        currentDevice = devices.GetTypeDevice<VoltMeter>();
        var getVoltValues = GetParameterForDevice().VoltValues;
        if (t.IsOk)
            await SetCheckValueInDevice(currentDevice, "Set volt meter", getVoltValues.VoltMaxLimit,
                innerCountCheck, innerDelay, t, "Get func", "Get volt meter");

        // s0.Restart();

        //цикл замеров
        foreach (var vipTested in vipsTested)
        {
            if (t.IsOk) await TestVip(vipTested, TypeOfTestRun.AvailabilityCheckVip, t: t);
        }

        testVipPlay = false;

        if (resetAll) return false;

        if (vipsStopped.Any())
        {
            //
            SetPriorityStatusStand(1, $"Проверка наличия Випов, ошибка!", percentSubTest: 100,
                colorSubTest: Brushes.Red, clearAll: true);
            TestRun = TypeOfTestRun.Error;
            PercentCurrentTest = 100;
            ProgressColor = Brushes.Red;
            string vipsStoppedErrors = null;
            //
            foreach (var vip in vipsStopped)
            {
                vipsStoppedErrors += $" Вип - {vip.Name}:\n {vip.ErrorStatusVip}\n";
            }

            throw new Exception($" Следующие випы не функционируют:\n{vipsStoppedErrors}");
        }

        if (t.IsOk)
        {
            //
            SetPriorityStatusStand(1, $"Проверка наличия ,Ок!", percentSubTest: 100,
                colorSubTest: Brushes.Violet, clearAll: true);
            TestRun = TypeOfTestRun.AvailabilityCheckVip;
            ProgressColor = Brushes.Green;
            PercentCurrentTest = 10;
            SetPriorityStatusStand(0, clearAll: true);
            //
            return true;
        }

        if (resetAll || stopMeasurement) return false;
        //
        SetPriorityStatusStand(1, $"Проверка наличия Випов, ошибка!", percentSubTest: 100,
            colorSubTest: Brushes.Red, clearAll: true);
        TestRun = TypeOfTestRun.Error;
        PercentCurrentTest = 100;
        ProgressColor = Brushes.Red;
        //
        throw new Exception("Одно или несколько устройств не ответили  или ответили\nс ошибкой.\n");
    }


    //--zero--ноль--
    public async Task<bool> MeasurementZero(int innerDelay = 3, int innerCountCheck = 1000)
    {
        //
        TestRun = TypeOfTestRun.MeasurementZero;
        ProgressColor = Brushes.Green;
        PercentCurrentTest = 10;
        SetPriorityStatusStand(0, clearAll: true);
        //

        //
        PercentCurrentTest = 20;
        //

        TempChecks t = TempChecks.Start();

        //отключение приборов 

        currentDevice = devices.GetTypeDevice<Supply>();
        //выкл бп
        await OutputDevice(currentDevice, t: t, on: false);

        currentDevice = devices.GetTypeDevice<BigLoad>();
        //выкл нагрузку
        await OutputDevice(currentDevice, t: t, on: false);

        //выкл випы
        foreach (var vip in vipsTested)
        {
            if (t.IsOk)
            {
                await OutputDevice(vip.Relay, t: t, forcedOff: true, on: false);
            }
        }

        // установки настройек приобров

        //большой нагрузки
        if (t.IsOk)
        {
            currentDevice = devices.GetTypeDevice<BigLoad>();
            var getBigLoadValues = GetParameterForDevice().BigLoadValues;

            if (t.IsOk)
                await SetCheckValueInDevice(currentDevice, "Set freq", getBigLoadValues.Freq,
                    innerDelay, innerCountCheck, t, "Get freq");
            if (t.IsOk)
                await SetCheckValueInDevice(currentDevice, "Set ampl", getBigLoadValues.Ampl,
                    innerDelay, innerCountCheck, t, "Get ampl");
            if (t.IsOk)
                await SetCheckValueInDevice(currentDevice, "Set dco", getBigLoadValues.Dco,
                    innerDelay, innerCountCheck, t, "Get dco");
            if (t.IsOk)
                await SetCheckValueInDevice(currentDevice, "Set squ", getBigLoadValues.Squ,
                    innerDelay, innerCountCheck, t, "Get squ");

            //вкл нагрузку
            if (t.IsOk)
                await OutputDevice(currentDevice, t: t);
        }

        //
        PercentCurrentTest = 40;
        //

        //бп
        if (t.IsOk)
        {
            currentDevice = devices.GetTypeDevice<Supply>();
            var getSupplyValues = GetParameterForDevice().SupplyValues;

            if (t.IsOk)
                await SetCheckValueInDevice(currentDevice, "Set volt", getSupplyValues.Voltage,
                    innerDelay, innerCountCheck, t, "Get volt");
            if (t.IsOk)
                await SetCheckValueInDevice(currentDevice, "Set curr", getSupplyValues.Current,
                    innerDelay, innerCountCheck, t, "Get curr");

            //вкл бп
            if (t.IsOk)
                await OutputDevice(currentDevice, t: t);
        }

        //
        PercentCurrentTest = 60;
        //

        if (t.IsOk)
        {
            if (!vipsTested.Any())
            {
                TestRun = TypeOfTestRun.Error;
                PercentCurrentTest = 100;
                ProgressColor = Brushes.Red;
                throw new Exception("Отсутвуют инициализировнные реле випов!");
            }

            //TODO уточнить добавлять ли сюда тк он конфигурится в методе выше AvailabilityCheckVip
            //волтьтметра
            // currentDevice = devices.GetTypeDevice<VoltMeter>();
            // var getVoltValues = GetParameterForDevice().VoltValues;
            // if (tp.IsOk)
            //     await SetCheckValueInDevice(currentDevice, "Set volt meter", getVoltValues.VoltMaxLimit,
            //         innerCountCheck, innerDelay, tp, "Get func", "Get volt meter");

            try
            {
                // s1.Restart();
                foreach (var vipTested in vipsTested)
                {
                    if (t.IsOk) await TestVip(vipTested, TypeOfTestRun.MeasurementZero, t: t);
                    //Debug.WriteLine(s1.ElapsedMilliseconds + $"ms/Zero/{vipTested.Name}");
                }

                testVipPlay = false;
                //Debug.WriteLine(s1.ElapsedMilliseconds + $"ms/Zero");

                if (vipsStopped.Any())
                {
                    //
                    SetPriorityStatusStand(1, $"Тест Випов, ошибка!", percentSubTest: 100,
                        colorSubTest: Brushes.Red, clearAll: true);
                    //

                    //
                    TestRun = TypeOfTestRun.Error;
                    PercentCurrentTest = 100;
                    ProgressColor = Brushes.Red;
                    string vipsStoppedErrors = null;
                    //

                    foreach (var vip in vipsStopped)
                    {
                        vipsStoppedErrors += $" Вип - {vip.Name}:\n {vip.ErrorStatusVip}\n";
                    }

                    throw new Exception($" Следующие випы не функционируют:\n{vipsStoppedErrors}");
                }
            }
            catch (Exception e)
            {
                TestRun = TypeOfTestRun.Error;
                PercentCurrentTest = 100;
                ProgressColor = Brushes.Red;
                throw new Exception(e.Message);
            }
        }

        if (t.IsOk)
        {
            //
            SetPriorityStatusStand(1, $"0 Замер, Ок!", percentSubTest: 100,
                colorSubTest: Brushes.Green, clearAll: true);
            TestRun = TypeOfTestRun.MeasurementZeroReady;
            PercentCurrentTest = 100;
            //
            return true;
        }

        if (resetAll || stopMeasurement) return false;
        //
        SetPriorityStatusStand(1, $"0 Замер, ошибка!", percentSubTest: 100,
            colorSubTest: Brushes.Red, clearAll: true);
        TestRun = TypeOfTestRun.Error;
        PercentCurrentTest = 100;
        ProgressColor = Brushes.Red;
        //
        throw new Exception("Одно или несколько устройств не ответили или ответили\nс ошибкой.\n");
    }


    //--prepare--pretest--prestart--heat
    public async Task<bool> PrepareMeasurementCycle(int countChecked = 3, int loopDelay = 1000, int timeHeatSec = 120)
    {
        //
        TestRun = TypeOfTestRun.WaitHeatPlate;
        PercentCurrentTest = 50;
        ProgressColor = Brushes.Green;
        //

        TempChecks t = TempChecks.Start();

        currentDevice = devices.GetTypeDevice<Heat>();
        bool isHeatEnable = t.IsOk;
        await OutputDevice(currentDevice, t: t);

        if (t.IsOk)
        {
            currentDevice = devices.GetTypeDevice<VoltCurrentMeter>();
            var getThermoCurrentValues = GetParameterForDevice().VoltCurrentValues;
            await WriteIdentCommand(currentDevice, "Set temp meter");
            await SetCheckValueInDevice(currentDevice, "Set tco type", getThermoCurrentValues.TermocoupleType,
                countChecked, loopDelay, t, "Get tco type");
        }

        //цикл нагревание пластины 

        var timeHeat = timeHeatSec / 10;
        float extPercent = 0;
        (bool isChecked, Threshold threshold) isChecked = (false, Threshold.Low);
        string error = null;

        if (t.IsOk)
        {
            for (int i = 1; i <= timeHeat; i++)
            {
                try
                {
                    var percent = ((1) * 100 / timeHeat);
                    if (percent > 100)
                    {
                        percent = 100;
                    }

                    extPercent += percent;

                    SetPriorityStatusStand(3, $"Нагрев основания", currentDevice, percentSubTest: extPercent,
                        currentCountCheckedSubTest: $"Цикл: {i.ToString()}/{timeHeat}",
                        colorSubTest: Brushes.RosyBrown);

                    var receiveData = await WriteIdentCommand(currentDevice, "Get all value", t: t);
                    var temperature = GetValueReceive(currentDevice, receiveData);
                    isChecked = CheckValueInVip(vipsTested[0], temperature, TypeCheckVal.Temperature);

                    //
                    if (isChecked.threshold == Threshold.High && isHeatEnable)
                    {
                        isHeatEnable = false;
                        if (t.IsOk) await OutputDevice(devices.GetTypeDevice<Heat>(), t: t, on: false);
                    }
                    else if (!isHeatEnable)
                    {
                        isHeatEnable = true;
                        if (t.IsOk) await OutputDevice(devices.GetTypeDevice<Heat>(), t: t);
                    }
                    //

                    if (isChecked.isChecked)
                    {
                        TestRun = TypeOfTestRun.WaitSupplyMeasurementZeroReady;
                        PercentCurrentTest = 100;
                        ProgressColor = Brushes.Green;
                        //
                        SetPriorityStatusStand(3, $"Нагрев основания, ок!", currentDevice, percentSubTest: 100,
                            colorSubTest: Brushes.RosyBrown, clearAll: true);
                        //
                        t?.Add(true);
                        break;
                    }


                    //проверка нагрева каждые 10 сек
                    await Task.Delay(TimeSpan.FromMilliseconds(10000), ctsAllCancel.Token);
                }
                catch (TaskCanceledException e) when (ctsAllCancel.IsCancellationRequested)
                {
                    SetPriorityStatusStand(3, $"Нагрев основания, ошибка!", currentDevice, percentSubTest: 100,
                        colorSubTest: Brushes.Red, clearAll: true);
                    ctsAllCancel = new CancellationTokenSource();
                    t?.Add(false);
                    return false;
                }
            }
        }

        if (!isChecked.isChecked)
        {
            TestRun = TypeOfTestRun.Error;
            PercentCurrentTest = 100;
            ProgressColor = Brushes.Red;

            if (isChecked.threshold == Threshold.Low)
            {
                //
                error = $"Нагрев основания, ошибка - низкая температура!";
                SetPriorityStatusStand(3, error, currentDevice,
                    percentSubTest: 100,
                    colorSubTest: Brushes.RosyBrown, clearAll: true);
                //
            }
            else
            {
                //
                error = $"Нагрев основания, ошибка!";
                SetPriorityStatusStand(3, error, currentDevice,
                    percentSubTest: 100,
                    colorSubTest: Brushes.RosyBrown, clearAll: true);
                //
            }

            t?.Add(false);
        }

        //TODO венуть
        if (t.IsOk)
        {
            //
            SetPriorityStatusStand(3, $"Нагрев основания, ок!", currentDevice, percentSubTest: 100,
                colorSubTest: Brushes.RosyBrown, clearAll: true);
            //
            TestRun = TypeOfTestRun.WaitHeatPlateReady;
            PercentCurrentTest = 100;

            return true;
        }

        if (resetAll) return false;
        //
        SetPriorityStatusStand(1, $"Нагрев основания, ошибка!", percentSubTest: 100,
            colorSubTest: Brushes.Red, clearAll: true);
        TestRun = TypeOfTestRun.Error;
        PercentCurrentTest = 100;
        ProgressColor = Brushes.Red;
        //
        throw new Exception("Одно или несколько устройств не ответили или ответили\nс ошибкой.\n");
    }

    //--load--enable--нагрузка
    public async Task<bool> EnableLoads(int countChecked = 3, int loopDelay = 1000)
    {
        TestRun = TypeOfTestRun.SmallLoadOutput;
        ProgressColor = Brushes.Green;
        PercentCurrentTest = 0;
        SetPriorityStatusStand(0, clearAll: true);
        //
        SetPriorityStatusStand(2, $"Включение малой нагрузки", currentDevice, percentSubTest: 0,
            colorSubTest: Brushes.OrangeRed, clearAll: true);
        //

        TempChecks t = TempChecks.Start();

        foreach (var vip in vipsTested)
        {
            if (t.IsOk) await OutputLoadInChannel(vip, t);
        }

        if (t.IsOk)
        {
            //
            SetPriorityStatusStand(2, $"включение малой нагрузки,Ок!", currentDevice, percentSubTest: 100,
                colorSubTest: Brushes.Green, clearAll: true);
            TestRun = TypeOfTestRun.SmallLoadOutputReady;
            PercentCurrentTest = 100;
            //
            return true;
        }

        if (resetAll || stopMeasurement) return false;
        //
        SetPriorityStatusStand(1, $"включение малой нагрузки, ошибка!", percentSubTest: 100,
            colorSubTest: Brushes.Red, clearAll: true);
        TestRun = TypeOfTestRun.Error;
        PercentCurrentTest = 100;
        ProgressColor = Brushes.Red;
        //
        throw new Exception("Одно или несколько устройств не ответили или ответили\nс ошибкой.\n");
    }

    //--mesaure--cycle--cucle--start--loop
    public bool StartMeasurementCycle()
    {
        //
        TestRun = TypeOfTestRun.CyclicMeasurement;
        ProgressColor = Brushes.Green;
        PercentCurrentTest = 10;
        SetPriorityStatusStand(0, clearAll: true);
        //

        try
        {
            countCheckCycle = 1;
            countMeasurementCycle = 1;

            //TODO уточнить надо ли выносить в текстбоксы
            //тик таймера 120000 милисекунд = 120 секунд = 2 минуты
            int tickInterval = 10000;

            //первый замер 1800 секунд = 30 минут
            int firstInterval = 30;
            //последующие замеры 7200 секунд = 2 часа
            int intervalMeasurement = 60;
            //последний замер/заверщение замеров 28800 секунд = 8 часов
            int lastMeasurement = 240;

            if (timerTest != null)
            {
                timerTest.Stop();
                timerTest.Elapsed -= TimerOnElapsed;
            }

            timerTest = new System.Timers.Timer(tickInterval);

            firstIntervalMeasurementCycle = new(firstInterval);
            intervalMeasurementCycle = new(intervalMeasurement);
            lastIntervalMeasurementStop = new(lastMeasurement);

            //Debug.Write("============================\n");

            //
            SetPriorityStatusStand(1, $"Циклические замеры Випов начало", percentSubTest: 0,
                colorSubTest: Brushes.Azure, clearAll: true);
            //

            TimeTestStart = DateTime.Now.ToString("HH:mm:ss");
            sTests.Restart();
            timerTest.Elapsed += TimerOnElapsed;
            timerTest.Enabled = true;
            timerTest.Start();
        }

        catch (Exception e)
        {
            throw new Exception();
        }

        return false;
    }

    int countCheckCycle = 0;
    int countMeasurementCycle = 0;
    private ReportCreator report;

    private bool isMeasurementOne;

    // --timer
    private async void TimerOnElapsed(object sender, ElapsedEventArgs e)
    {
        try
        {
            PercentCurrentTest += 5;

            if (vipsTested.Any())
            {
                TempChecks t = TempChecks.Start();
                float extPercent = 0;

                if (firstIntervalMeasurementCycle.Check())
                {
                    // Debug.WriteLine(sTests.ElapsedMilliseconds + $" mc/FirstCyclicMeasurement/{countMeasurementCycle}");

                    foreach (var vip in vipsTested)
                    {
                        //
                        var percent = ((1) * 100 / vipsTested.Count);
                        if (percent > 95)
                        {
                            percent = 100;
                        }

                        extPercent += percent;
                        SetPriorityStatusStand(1, $"цикл замера, Вип {vip.Name}", percentSubTest: extPercent,
                            colorSubTest: Brushes.Azure);
                        //

                        if (t.IsOk) await MeasurementTick(vip, TypeOfTestRun.CyclicMeasurement, t: t);
                    }

                    // Debug.WriteLine(sTests.ElapsedMilliseconds + $" mc/FirstCyclicMeasurement END/{countMeasurementCycle}");

                    countMeasurementCycle++;

                    firstIntervalMeasurementCycle.Stop();
                }

                else if (lastIntervalMeasurementStop.Check())
                {
                    PercentCurrentTest += 10;

                    // Debug.WriteLine(
                    //     sTests.Elapsed.ToString("HH:mm:ss") + $" mc/LastCyclicMeasurement/{countCheckCycle}");

                    foreach (var vip in vipsTested)
                    {
                        //
                        var percent = ((1) * 100 / vipsTested.Count);
                        if (percent > 95)
                        {
                            percent = 100;
                        }

                        extPercent += percent;
                        SetPriorityStatusStand(1, $"последний цикл замера, Вип {vip.Name}", percentSubTest: extPercent,
                            colorSubTest: Brushes.Azure);
                        //

                        if (t.IsOk) await MeasurementTick(vip, TypeOfTestRun.CyclicMeasurement, t: t);
                    }

                    //
                    SetPriorityStatusStand(1, $"Циклические замеры Випов завершение!", percentSubTest: 100,
                        colorSubTest: Brushes.Green);
                    //

                    //Debug.WriteLine(sTests.ElapsedMilliseconds + $" mc/LastCyclicMeasurement END/{countCheckCycle}");

                    countMeasurementCycle++;

                    ResetMeasurementCycle();

                    if (vipsStopped.Any())
                    {
                        //
                        SetPriorityStatusStand(1, $"Циклические замеры Випов, ошибка!", percentSubTest: 100,
                            colorSubTest: Brushes.Red, clearAll: true);
                        TestRun = TypeOfTestRun.Error;
                        PercentCurrentTest = 100;
                        ProgressColor = Brushes.Red;
                        string vipsStoppedErrors = null;
                        //

                        foreach (var vip in vipsStopped)
                        {
                            vipsStoppedErrors += $" Вип - {vip.Name}:\n {vip.ErrorStatusVip}\n";
                        }

                        timerErrorMeasurement?.Invoke($" Следующие випы не функционируют:\n{vipsStoppedErrors}");
                        return;
                    }

                    string vipsTestedNames = null;
                    foreach (var vip in vipsTested)
                    {
                        vipsTestedNames += $" Вип - {vip.Name}:\n {vip.ErrorStatusVip}\n";
                    }

                    timerOk?.Invoke($"Испытания завершены все Випы в норме:\n{vipsTestedNames}");
                }

                else if (intervalMeasurementCycle.Check())
                {
                    // Debug.WriteLine(sTests.ElapsedMilliseconds + $" mc/CyclicMeasurement/{countMeasurementCycle}");

                    foreach (var vip in vipsTested)
                    {
                        //
                        var percent = ((1) * 100 / vipsTested.Count);
                        if (percent > 95)
                        {
                            percent = 100;
                        }

                        extPercent += percent;
                        SetPriorityStatusStand(1, $"цикл замера, Вип {vip.Name}", percentSubTest: extPercent,
                            colorSubTest: Brushes.Azure);
                        //
                        if (t.IsOk) await MeasurementTick(vip, TypeOfTestRun.CyclicMeasurement, t: t);
                    }

                    // Debug.WriteLine(sTests.ElapsedMilliseconds + $" mc/CyclicMeasurement END/{countMeasurementCycle}");

                    countMeasurementCycle++;
                }
                else
                {
                    // Debug.WriteLine(sTests.ElapsedMilliseconds + $" mc/CycleCheck/{countCheckCycle}");

                    foreach (var vip in vipsTested)
                    {
                        //
                        var percent = ((1) * 100 / vipsTested.Count);
                        if (percent > 95)
                        {
                            percent = 100;
                        }

                        extPercent += percent;
                        SetPriorityStatusStand(1, $"цикл проверки работоспособности стенда, Вип {vip.Name}",
                            percentSubTest: extPercent,
                            colorSubTest: Brushes.NavajoWhite);
                        //

                        if (t.IsOk) await MeasurementTick(vip, TypeOfTestRun.CycleCheck, t: t);
                    }

                    // Debug.WriteLine(sTests.ElapsedMilliseconds + $" mc/CycleCheck END/{countCheckCycle}");

                    countCheckCycle++;
                }

                if (resetAll || stopMeasurement) return;

                if (!t.IsOk)
                {
                    //
                    SetPriorityStatusStand(1, $"Циклические замеры Випов, ошибка!", percentSubTest: 100,
                        colorSubTest: Brushes.Red, clearAll: true);
                    TestRun = TypeOfTestRun.Error;
                    PercentCurrentTest = 100;
                    ProgressColor = Brushes.Red;
                    //
                    timerErrorDevice?.Invoke(
                        "Одно или несколько устройств не ответили или ответили\nс ошибкой.\n");
                }
            }

            if (vipsTested.Any()) return;

            //
            SetPriorityStatusStand(1, $"Циклические замеры Випов, ошибка!", percentSubTest: 100,
                colorSubTest: Brushes.Red, clearAll: true);
            TestRun = TypeOfTestRun.Error;
            PercentCurrentTest = 100;
            ProgressColor = Brushes.Red;
            //

            ResetMeasurementCycle();

            if (vipsStopped.Any())
            {
                string vipsStoppedErrors = null;
                foreach (var vip in vipsStopped)
                {
                    vipsStoppedErrors += $" Вип - {vip.Name}:\n {vip.ErrorStatusVip}\n";
                }

                timerErrorMeasurement?.Invoke(
                    $"У вас закончились рабочие Випы, проверять нечего!\n Следующие випы не функционируют:\n{vipsStoppedErrors}");
                return;
            }

            timerErrorMeasurement?.Invoke($"У вас закончились рабочие Випы, проверять нечего!");
        }
        catch (Exception ex)
        {
            //
            SetPriorityStatusStand(1, $"Циклические замеры Випов, ошибка!", percentSubTest: 100,
                colorSubTest: Brushes.Red, clearAll: true);
            TestRun = TypeOfTestRun.Error;
            PercentCurrentTest = 100;
            ProgressColor = Brushes.Red;
            //
            timerErrorDevice?.Invoke(ex.Message);
        }
    }

    //--tick--test--ass
    async Task<(bool result, Vip vip)> MeasurementTick(Vip vip, TypeOfTestRun typeTest, int countChecked = 3,
        int loopDelay = 1000, TempChecks t = null)
    {
        try
        {
            await TestVip(vip, typeTest, t: t);
            testVipPlay = false;
            return (t?.IsOk ?? true, vip);
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

    private async Task<bool> OutputLoadInChannel(Vip vip, TempChecks t, bool on = true)
    {
        TempChecks tp = TempChecks.Start();

        //TODO вернуть когда появятся все нагрузки или исправится номера текущих
        // var ss = devices.FirstOrDefault(x => x.Name.Contains("SL") && x.Prefix == (vip.Id + 1).ToString());

        //TODO удалить когда появятся все нагрузки или исправится номера текущих
        var supplyLoads = allRelayVips.Where(x => x.Name.Contains("SL")).ToList();
        //TODO удалить когда появятся все нагрузки или исправится номера текущих

        //
        SetPriorityStatusStand(2, $"переключение 1 канала измерений Вип - {vip.Name}", percentSubTest: 40,
            colorSubTest: Brushes.DodgerBlue, clearAll: true);
        //
        if (on)
        {
            vip.StatusSmallLoad = OnOffStatus.Switching;
        }

        RelayVip sl = null;

        if (vip.Id is 0 or 1 or 2 or 3)
        {
            sl = supplyLoads.FirstOrDefault(x => x.Prefix == "1");
            await OutputDevice(sl, vip.Id + 1, t: tp, on: on);
        }
        else if (vip.Id is 4 or 5 or 6 or 7)
        {
            sl = supplyLoads.FirstOrDefault(x => x.Prefix == "2");
            await OutputDevice(sl, vip.Id - 3, t: tp, on: on);
        }
        else if (vip.Id is 8 or 9 or 10 or 11)
        {
            sl = supplyLoads.FirstOrDefault(x => x.Prefix == "3");
            await OutputDevice(sl, vip.Id - 7, t: tp, on: on);
        }

        if (tp.IsOk)
        {
            if (on)
            {
                //
                SetPriorityStatusStand(2, $"Включение {vip.Id} канала малой нагрузки {sl?.Prefix} ,Ок!",
                    percentSubTest: 100,
                    colorSubTest: Brushes.DodgerBlue, clearAll: true);
                //
                vip.StatusSmallLoad = OnOffStatus.On;
                if (sl != null) sl.StatusOnOff = OnOffStatus.On;
            }
            else
            {
                //
                SetPriorityStatusStand(2, $"Отключение {vip.Id} канала малой нагрузки {sl?.Prefix} ,Ок!",
                    percentSubTest: 100,
                    colorSubTest: Brushes.DodgerBlue, clearAll: true);
                //
                vip.StatusSmallLoad = OnOffStatus.Off;
                if (sl != null) sl.StatusOnOff = OnOffStatus.Off;
            }

            t?.Add(true);
            return true;
        }

        //
        SetPriorityStatusStand(2, $"Вкл/выкл {vip.Id} канала малой нагрузки {sl?.Prefix}, ошибка!",
            percentSubTest: 100, colorSubTest: Brushes.Red, clearAll: true);
        //

        vip.StatusSmallLoad = OnOffStatus.None;
        if (sl != null) sl.StatusOnOff = OnOffStatus.None;
        t?.Add(false);
        return false;
    }

    private ObservableCollection<Vip> GetIsTestedVips()
    {
        foreach (var vip in vips)
        {
            if (!string.IsNullOrWhiteSpace(vip.Name) && vip.StatusTest != StatusDeviceTest.Error)
            {
                vip.IsTested = true;
            }
            else
            {
                vip.IsTested = false;
            }
        }

        var testedVips = vips.Where(x => x.IsTested);
        return new ObservableCollection<Vip>(testedVips);
    }

    private ObservableCollection<Vip> GetStoppedVips()
    {
        var stoppedVips = vips.Where(x => x.StatusTest == StatusDeviceTest.Error);

        return new ObservableCollection<Vip>(stoppedVips);
    }

    #endregion

    //---

    #region --Запросы и обработа ответов/ошибок из устройств

    //--

    #region --Запрос в --порт устройства

    private List<BaseDevice> currentConnectDevices = new();

    //--check--port--conn--
    /// <summary>
    /// Проверка на физическое существование порта (одичночного)
    /// </summary>
    /// <param name="device">Устройство</param>
    /// <param name="countChecked"></param>
    /// <param name="loopDelay"></param>
    /// <param name="t"></param>
    /// <param name="externalDelay">Общая задержка проверки (default = 100)</param>
    /// <param name="countChecks">Колво проверок если не работает</param>
    /// <returns name="errorDevice"></returns>
    async Task<bool> CheckConnectPort(BaseDevice device, int countChecked = 3, int loopDelay = 200, TempChecks t = null)
    {
        if (resetAll)
        {
            t?.Add(false);
            return false;
        }

        currentConnectDevices = new() { device };

        try
        {
            for (int i = 1; i <= countChecked; i++)
            {
                SetPriorityStatusStand(5, $"проверка порта - {device.GetConfigDevice().PortName}", device, 0,
                    $"Попытка: {i.ToString()}/{countChecked}", Brushes.RoyalBlue);
                if (i > 1)
                {
                    try
                    {
                        if (loopDelay > 0)
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(loopDelay), ctsConnectDevice.Token);
                        }
                    }
                    catch (TaskCanceledException e)
                    {
                        ctsConnectDevice = new CancellationTokenSource();

                        //если тесты остановлены, немделнно выходим из метода с ошибкой
                        if (resetAll)
                        {
                            t?.Add(false);
                            return false;
                        }
                    }
                }

                //
                SetPriorityStatusStand(5, percentSubTest: 50,
                    currentCountCheckedSubTest: $"Попытка: {i.ToString()}/{countChecked}");
                //

                device.ErrorStatus = null;
                device.StatusTest = StatusDeviceTest.None;

                device.Close();
                await Task.Delay(TimeSpan.FromMilliseconds(80), ctsAllCancel.Token);
                device.Start();
                await Task.Delay(TimeSpan.FromMilliseconds(20), ctsAllCancel.Token);
                device.DtrEnable();
                await Task.Delay(TimeSpan.FromMilliseconds(80), ctsAllCancel.Token);

                var verifiedDevice = GetVerifiedPort(device);

                if (!verifiedDevice.Value)
                {
                    continue;
                }

                //
                SetPriorityStatusStand(5, $"проверка порта - {device.GetConfigDevice().PortName}, ок!",
                    percentSubTest: 100, clearAll: true);
                //

                t?.Add(true);
                return true;
            }

            device.Close();
            await Task.Delay(TimeSpan.FromMilliseconds(80), ctsAllCancel.Token);

            //
            SetPriorityStatusStand(5, $"проверка порта - {device.GetConfigDevice().PortName}, ошибка!",
                percentSubTest: 100, colorSubTest: Brushes.Red, clearAll: true);
            //

            device.StatusTest = StatusDeviceTest.Error;

            t?.Add(false);
            return false;
        }
        catch (TaskCanceledException e) when (ctsAllCancel.IsCancellationRequested)
        {
            ctsAllCancel = new CancellationTokenSource();
            t?.Add(false);
            return false;
        }

        catch (Exception e)
        {
            //
            SetPriorityStatusStand(5, $"проверка порта - {device.GetConfigDevice().PortName}, ошибка!",
                percentSubTest: 100, colorSubTest: Brushes.Red, clearAll: true);
            //

            device.StatusTest = StatusDeviceTest.Error;

            if (resetAll)
            {
                t?.Add(false);
                return false;
            }

            t?.Add(false);
            throw new Exception(e.Message);
        }
    }

    //--checks--ports--conns--
    /// <summary>
    /// Проверка на физическое существование портов (несколько)
    /// </summary>
    /// <param name="devices"></param>
    /// <param name="countChecked">Колво проверок если не работает</param>
    /// <param name="loopDelay"></param>
    /// <param name="t"></param>
    /// <param name="tempChecks"></param>
    /// <param name="device">Устройство</param>
    /// <param name="externalDelay">Общая задержка проверки (default = 100)</param>
    /// <returns name="errorDevice"></returns>
    async Task<bool> CheckConnectPorts(List<BaseDevice> devices, int countChecked = 3, int loopDelay = 200,
        TempChecks t = null)
    {
        devices.RemoveAll(i => i.Name.Contains("SL"));

        if (resetAll)
        {
            t?.Add(false);
            return false;
        }

        currentConnectDevices = devices;
        var verifiedDevices = new Dictionary<BaseDevice, bool>();

        //
        SetPriorityStatusStand(5, $"проверка портов", null,
            percentSubTest: 100, currentCountCheckedSubTest: null, colorSubTest: Brushes.RoyalBlue);
        //

        try
        {
            for (int i = 1; i <= countChecked; i++)
            {
                //
                SetPriorityStatusStand(5, $"проверка портов", null,
                    percentSubTest: 100, currentCountCheckedSubTest: $"Попытка: {i.ToString()}/{countChecked}",
                    colorSubTest: Brushes.RoyalBlue);
                //

                try
                {
                    if (i > 1)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(loopDelay), ctsConnectDevice.Token);
                    }

                    foreach (var device in devices)
                    {
                        device.ErrorStatus = null;
                        device.StatusTest = StatusDeviceTest.None;
                        device.Close();
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(80), ctsAllCancel.Token);

                    float extPercent = 0;

                    foreach (var device in devices)
                    {
                        device.Start();
                        await Task.Delay(TimeSpan.FromMilliseconds(20), ctsAllCancel.Token);
                        device.DtrEnable();

                        var percent = ((1 / (float)devices.Count) * 100);
                        if (percent > 100)
                        {
                            percent = 100;
                        }

                        extPercent += percent;
                        //
                        SetPriorityStatusStand(5, $"проверка порта - {device.GetConfigDevice().PortName}", device);
                        //
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(80), ctsConnectDevice.Token);
                }

                catch (TaskCanceledException e)
                {
                    ctsConnectDevice = new CancellationTokenSource();
                    //если тесты остановлены, немделнно выходим из метода с ошибкой
                    if (resetAll)
                    {
                        t?.Add(false);
                        return false;
                    }
                }

                verifiedDevices = GetVerifiedPorts(devices);

                var checkDevices = verifiedDevices.Where(x => x.Value);

                foreach (var checkDevice in checkDevices)
                {
                    checkDevice.Key.StatusTest = StatusDeviceTest.None;
                    t?.Add(true);
                    verifiedDevices.Remove(checkDevice.Key);
                }

                if (verifiedDevices.Any())
                {
                    foreach (var verifiedDevice in verifiedDevices)
                    {
                        TestCurrentDevice = verifiedDevices.Keys.First();
                        TestCurrentDevice.StatusTest = StatusDeviceTest.Error;
                    }

                    continue;
                }

                //
                SetPriorityStatusStand(5, $"проверка портов, ок!", percentSubTest: 100,
                    clearAll: true);
                //

                return true;
            }

            foreach (var verifiedDevice in verifiedDevices)
            {
                verifiedDevice.Key.StatusTest = StatusDeviceTest.Error;
                t?.Add(false);
                verifiedDevice.Key.Close();
            }

            await Task.Delay(TimeSpan.FromMilliseconds(80), ctsAllCancel.Token);

            //
            SetPriorityStatusStand(5, $"проверка портов, ошибка!", percentSubTest: 100, colorSubTest: Brushes.Red);
            //

            return false;
        }
        catch (TaskCanceledException e) when (ctsAllCancel.IsCancellationRequested)
        {
            ctsConnectDevice = new CancellationTokenSource();
            t?.Add(false);
            return false;
        }
        catch (Exception e)
        {
            //
            SetPriorityStatusStand(5, $"проверка портов, ошибка!", percentSubTest: 100, colorSubTest: Brushes.Red);
            //

            if (resetAll)
            {
                t?.Add(false);
                return false;
            }

            t?.Add(false);
            throw new Exception(e.Message);
        }
    }

    #endregion

    //--

    #region --Запрос в --устройство

    private List<BaseDevice> CurrentWriteDevices = new();

    //--write--cmd--
    /// <summary>
    /// Запись одинакоывой команды с проверкой
    /// </summary>
    /// <param name="deviceCheck"></param>
    /// <param name="nameCmd"></param>
    /// <param name="paramSet"></param>
    /// <param name="paramGet"></param>
    /// <param name="countChecked"></param>
    /// <param name="loopDelay"></param>
    /// <param name="t"></param>
    /// <param name="isReceiveVal"></param>
    /// <param name="toList"></param>
    /// <param name="tempChecks"></param>
    /// <param name="isLast"></param>
    private async Task<KeyValuePair<BaseDevice, string>> WriteIdentCommand(BaseDevice deviceCheck, string nameCmd,
        string paramSet = null, string paramGet = null, int countChecked = 3, int loopDelay = 600, TempChecks t = null,
        bool isReceiveVal = false)
    {
        KeyValuePair<BaseDevice, string> deviceReceived = default;

        if (resetAll)
        {
            t?.Add(false);
            return deviceReceived;
        }

        CurrentWriteDevices = new() { deviceCheck };
        //удаление прердыдущих ответов от устройств
        RemoveReceive(deviceCheck);

        KeyValuePair<BaseDevice, bool> verifiedDevice;

        //если прибор от кторого приходят данные не содержится в библиотеке а при первом приеме данных так и будет
        if (!receiveInDevice.ContainsKey(deviceCheck))
        {
            //добавляем туда данные и создаем список в ктороые будет записаны ответы от прибора
            receiveInDevice.Add(deviceCheck, new List<string>());
        }

        try
        {
            for (int i = 1; i <= countChecked; i++)
            {
                //
                SetPriorityStatusStand(4, $"запись команды в устройство", deviceCheck, percentSubTest: 0,
                    currentCountCheckedSubTest: $"Попытка: {i.ToString()}/{countChecked}",
                    colorSubTest: Brushes.Orange);
                //

                deviceCheck.ErrorStatus = null;
                deviceCheck.StatusTest = StatusDeviceTest.None;

                try
                {
                    if (i > 1)
                    {
                        //добавить кансел и проверить как будет работаьть
                        ctsReceiveDevice?.Cancel();
                        //где создаются проставить дебаги чтобы понять какой вызвает task canncelled
                        ctsReceiveDevice = new CancellationTokenSource();
                        // Debug.WriteLine("ctsReceiveDevice");
                        await Task.Delay(TimeSpan.FromMilliseconds(loopDelay), ctsReceiveDevice.Token);
                    }

                    if (!resetAll && !deviceCheck.PortIsOpen)
                    {
                        if (deviceCheck is not RelayVip || !mainRelay.PortIsOpen)
                        {
                            SetPriorityStatusStand(4, $"порт закрыт, переподключение...", clearAll: true);

                            if (!await CheckConnectPort(deviceCheck, 3))
                            {
                                //
                                SetPriorityStatusStand(4, $"порт не открыт, ошибка!", deviceCheck,
                                    colorSubTest: Brushes.Red);
                                //
                                t?.Add(false);
                                return deviceReceived;
                            }

                            SetPriorityStatusStand(4, $"переподключение удачно, порт открыт");
                            await Task.Delay(TimeSpan.FromMilliseconds(50));
                        }
                    }

                    //установка гет парамтра
                    deviceCheck.CurrentParameterGet = paramGet;
                    deviceCheck.IsReceive = isReceiveVal;

                    //запись команды в каждое устройсттво
                    deviceCheck.WriteCmd(nameCmd, paramSet);

                    //
                    SetPriorityStatusStand(4, $"запись команды в устройство", deviceCheck, percentSubTest: 50,
                        currentCountCheckedSubTest: $"Попытка: {i.ToString()}/{countChecked}",
                        colorSubTest: Brushes.Orange);
                    //

                    if (!string.IsNullOrEmpty(paramSet))
                    {
                        SetGdmReceive(deviceCheck, paramSet);
                    }

                    if (!string.IsNullOrEmpty(paramGet) && string.IsNullOrEmpty(paramSet))
                    {
                        deviceCheck.CurrentParameter = paramGet;
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(deviceCheck.CurrentCmd?.Delay ?? 200),
                        ctsReceiveDevice.Token);

                    if (t == null)
                    {
                        SetPriorityStatusStand(4, $"запись команды в устройство, ок!");
                        return new KeyValuePair<BaseDevice, string>();
                    }
                }

                catch (TaskCanceledException e) when (ctsReceiveDevice.IsCancellationRequested)
                {
                    ctsReceiveDevice = new CancellationTokenSource();

                    await Task.Delay(TimeSpan.FromMilliseconds(50), ctsAllCancel.Token); //TODO 100ms

                    //если тест остановлены, немделнно выходим из метода с ошибкой
                    if (resetAll)
                    {
                        deviceReceived = GetReceiveLast(deviceCheck);
                        t?.Add(false);
                        if (deviceReceived.Key == null)
                        {
                            return new KeyValuePair<BaseDevice, string>(deviceCheck, "Stop tests");
                        }

                        return deviceReceived;
                    }
                }

                // Проверка устройств
                verifiedDevice = GetVerifiedDevice(deviceCheck);

                if (verifiedDevice.Value)
                {
                    verifiedDevice.Key.StatusTest = StatusDeviceTest.Ok;

                    //
                    SetPriorityStatusStand(4, $"запись команды в устройство, ок!", deviceCheck, percentSubTest: 100,
                        colorSubTest: Brushes.Orange);
                    //

                    t?.Add(true);
                    deviceReceived = GetReceiveLast(verifiedDevice.Key);
                    return deviceReceived;
                }

                verifiedDevice.Key.StatusTest = StatusDeviceTest.Error;

                TempChecks tp = TempChecks.Start();
                if (verifiedDevice.Key.AllDeviceError.ErrorDevice ||
                    verifiedDevice.Key.AllDeviceError.ErrorPort)
                {
                    await CheckConnectPort(verifiedDevice.Key, 1, t: tp);
                }
                else if (verifiedDevice.Key.AllDeviceError.ErrorTimeout)
                {
                    verifiedDevice.Key.ErrorStatus =
                        $"Ошибка уcтройства \"{verifiedDevice.Key.IsDeviceType}\"/нет ответа";

                    tp.Add(true);
                }
                else if (verifiedDevice.Key.AllDeviceError.ErrorTerminator ||
                         verifiedDevice.Key.AllDeviceError.ErrorReceive ||
                         verifiedDevice.Key.AllDeviceError.ErrorParam ||
                         verifiedDevice.Key.AllDeviceError.ErrorLength)
                {
                    if (loopDelay > 1000)
                    {
                        loopDelay = 1000;
                    }

                    tp.Add(true);
                }
            }

            //
            SetPriorityStatusStand(4, $"запись команды в устройство, ошибка!", deviceCheck, percentSubTest: 100,
                colorSubTest: Brushes.Red);
            //

            t?.Add(false);
            deviceReceived = GetReceiveLast(deviceCheck);
            return deviceReceived;
        }
        catch (TaskCanceledException e) when (ctsAllCancel.IsCancellationRequested)
        {
            ctsConnectDevice = new CancellationTokenSource();
            t?.Add(false);
            return deviceReceived;
        }

        // //добавить catch без when тобы понять 
        // catch (TaskCanceledException e)
        // {
        //     Debug.WriteLine($"ctsAllCancel: {ctsAllCancel.IsCancellationRequested}");
        //     Debug.WriteLine($"ctsConnectDevice: {ctsConnectDevice.IsCancellationRequested}");
        //     Debug.WriteLine($"ctsReceiveDevice: {ctsReceiveDevice.IsCancellationRequested}");
        //     throw;
        // }
        catch (Exception e)
        {
            // //
            // Debug.WriteLine(e);
            // //
            SetPriorityStatusStand(4, $"запись команды в устройство, ошибка!", deviceCheck, percentSubTest: 100,
                colorSubTest: Brushes.Red);
            //

            if (resetAll)
            {
                t?.Add(false);
                return deviceReceived;
            }

            throw new Exception(e.Message);
        }
    }

    //--writes--cmds--cms-
    /// <summary>
    /// Запись одинаковых команд с проверкой
    /// </summary>
    /// <param name="deviceChecks"></param>
    /// <param name="cmd"></param>
    /// <param name="paramSet"></param>
    /// <param name="paramGet"></param>
    /// <param name="countChecked"></param>
    /// <param name="loopDelay"></param>
    /// <param name="t"></param>
    /// <param name="isReceiveVal"></param>
    /// <param name="toList"></param>
    /// <param name="tempChecks"></param>
    private async Task<Dictionary<BaseDevice, List<string>>> WriteIdentCommands(List<BaseDevice> deviceChecks,
        string cmd = null, string paramSet = null, string paramGet = null, int countChecked = 3, int loopDelay = 600,
        TempChecks t = null, bool isReceiveVal = false)
    {
        deviceChecks.RemoveAll(i => i.Name.Contains("SL"));
        var deviceReceived = new Dictionary<BaseDevice, List<string>>();

        if (resetAll)
        {
            t?.Add(false);
            return deviceReceived;
        }

        CurrentWriteDevices = deviceChecks;

        var verifiedDevices = new Dictionary<BaseDevice, bool>();
        var tempDevices = deviceChecks;
        var checkDevices = new List<KeyValuePair<BaseDevice, bool>>();

        var isRelay = false;
        //список задержек
        var delays = new List<int>(tempDevices.Count);

        //удаление прердыдущих ответов от устройств
        RemoveReceives(deviceChecks);

        float extPercent = 0;
        try
        {
            for (int i = 1; i <= countChecked; i++)
            {
                try
                {
                    //
                    SetPriorityStatusStand(4, $"запись команд в устройства", percentSubTest: 0,
                        currentCountCheckedSubTest: $"Попытка: {i.ToString()}/{countChecked}",
                        colorSubTest: Brushes.Orange);
                    //

                    if (i > 1)
                    {
                        //
                        var percent = (float)Math.Round((1 / (float)tempDevices.Count) * 100 / i);
                        if (percent > 100)
                        {
                            percent = 100;
                        }

                        extPercent += percent;
                        //

                        SetPriorityStatusStand(4, currentDeviceSubTest: tempDevices[0], percentSubTest: extPercent);
                        //ctsReceiveDevice.Cancel();
                        ctsReceiveDevice = new CancellationTokenSource();
                        await Task.Delay(TimeSpan.FromMilliseconds(loopDelay), ctsReceiveDevice.Token);
                    }

                    // запись команды в каждое устройсттво
                    foreach (var device in tempDevices)
                    {
                        //
                        TestCurrentDevice = device;
                        //

                        //если прибор от кторого приходят данные не содержится в библиотеке а при первом приеме данных так и будет
                        if (!receiveInDevice.ContainsKey(device))
                        {
                            //добавляем туда данные и создаем список в ктороые будет записаны ответы от прибора
                            receiveInDevice.Add(device, new List<string>());
                        }

                        if (!resetAll)
                        {
                            if (!device.PortIsOpen)
                            {
                                //
                                SetPriorityStatusStand(4, $"порт закрыт, переподключение...", clearAll: true);

                                if (!await CheckConnectPort(device, 3))
                                {
                                    //
                                    SetPriorityStatusStand(4, $"порт не открыт, ошибка!", device,
                                        colorSubTest: Brushes.Red, clearAll: true);
                                    //
                                    t?.Add(false);
                                    return deviceReceived;
                                }

                                //
                                SetPriorityStatusStand(4, $"переподключение удачно, порт открыт", clearAll: true);
                                //
                                await Task.Delay(TimeSpan.FromMilliseconds(50), ctsAllCancel.Token); //TODO 100ms
                            }
                        }

                        device.CurrentParameterGet = paramGet;
                        device.IsReceive = isReceiveVal;

                        if (string.IsNullOrEmpty(cmd))
                        {
                            device.WriteCmd(device.NameCurrentCmd, device.CurrentParameter);
                        }
                        else
                        {
                            device.WriteCmd(cmd, paramSet);
                        }

                        //
                        SetPriorityStatusStand(4, $"запись команды в устройство", device,
                            currentCountCheckedSubTest: $"Попытка: {i.ToString()}/{countChecked}",
                            colorSubTest: Brushes.Orange);
                        //

                        delays.Add(device.CurrentCmd?.Delay ?? 200);

                        if (!string.IsNullOrEmpty(paramSet))
                        {
                            SetGdmReceive(device, paramSet);
                        }

                        if (!string.IsNullOrEmpty(paramGet) && string.IsNullOrEmpty(paramSet))
                        {
                            device.CurrentParameter = paramGet;
                        }
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(delays.Max()), ctsReceiveDevice.Token);

                    if (t == null) return new Dictionary<BaseDevice, List<string>>();
                }

                catch (TaskCanceledException e) when (ctsReceiveDevice.IsCancellationRequested)
                {
                    ctsReceiveDevice = new CancellationTokenSource();
                    await Task.Delay(TimeSpan.FromMilliseconds(50), ctsAllCancel.Token); //TODO 100ms

                    //если тесты остановлены, немделнно выходим из метода с ошибкой
                    if (resetAll)
                    {
                        t?.Add(false);
                        deviceReceived = GetReceives();
                        if (!deviceReceived.Keys.Any())
                        {
                            deviceReceived.Add(TestCurrentDevice, new List<string>() { "Stop test" });
                        }

                        return deviceReceived;
                    }
                }

                //Проверка устройств
                verifiedDevices = GetVerifiedDevices(tempDevices);

                checkDevices = verifiedDevices.Where(x => x.Value).ToList();

                foreach (var checkDevice in checkDevices)
                {
                    //
                    SetPriorityStatusStand(4, $"...попытки записи команды в устройство", checkDevice.Key);
                    //
                    checkDevice.Key.StatusTest = StatusDeviceTest.Ok;

                    t?.Add(true);
                    verifiedDevices.Remove(checkDevice.Key);
                }

                if (verifiedDevices.Any())
                {
                    TempChecks tp = TempChecks.Start();
                    foreach (var verifiedDevice in verifiedDevices)
                    {
                        verifiedDevice.Key.StatusTest = StatusDeviceTest.Error;

                        if (verifiedDevice.Key.AllDeviceError.ErrorDevice ||
                            verifiedDevice.Key.AllDeviceError.ErrorPort)
                        {
                            await CheckConnectPort(verifiedDevice.Key, 1, t: tp);
                        }
                        else if (verifiedDevice.Key.AllDeviceError.ErrorTimeout)
                        {
                            verifiedDevice.Key.ErrorStatus =
                                $"Ошибка уcтройства \"{verifiedDevice.Key.IsDeviceType}\"/нет ответа";
                        }
                        else if (verifiedDevice.Key.AllDeviceError.ErrorTerminator ||
                                 verifiedDevice.Key.AllDeviceError.ErrorReceive ||
                                 verifiedDevice.Key.AllDeviceError.ErrorParam ||
                                 verifiedDevice.Key.AllDeviceError.ErrorLength ||
                                 verifiedDevice.Key.AllDeviceError.ErrorTimeout)
                        {
                            if (loopDelay > 1000)
                            {
                                loopDelay = 1000;
                            }
                        }
                    }

                    tempDevices = verifiedDevices.Keys.ToList();
                }
                else
                {
                    //
                    SetPriorityStatusStand(4, $"запись команд в устройства, oк!", percentSubTest: 100,
                        colorSubTest: Brushes.Orange, clearAll: true);
                    //

                    deviceChecks.ForEach(x => x.StatusTest = StatusDeviceTest.Ok);

                    t?.Add(true);
                    deviceReceived = GetReceives();
                    return deviceReceived;
                }
            }
        }
        catch (TaskCanceledException e) when (ctsAllCancel.IsCancellationRequested)
        {
            ctsConnectDevice = new CancellationTokenSource();
            t?.Add(false);
            return deviceReceived;
        }
        catch (Exception e)
        {
            //
            SetPriorityStatusStand(4, $"запись команд в устройства, ошибка!", percentSubTest: 100,
                colorSubTest: Brushes.Red, clearAll: true);
            //

            if (resetAll)
            {
                t?.Add(false);
                return deviceReceived;
            }

            throw new Exception(e.Message);
        }

        //
        SetPriorityStatusStand(4, $"запись команд в устройства, ошибка!", percentSubTest: 100,
            colorSubTest: Brushes.Red, clearAll: true);
        //

        t?.Add(false);
        deviceReceived = GetReceives();
        return deviceReceived;
    }

    //-

    #endregion

    //--

    #region Обработка

    #region Обработка --ошибок

    /// <summary>
    /// Получение порта с ошбиками 
    /// </summary>
    /// <param name="device"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private KeyValuePair<BaseDevice, bool> GetVerifiedPort(BaseDevice device)
    {
        try
        {
            var errors = device.AllDeviceError;

            if (errors.ErrorDevice)
            {
                return new KeyValuePair<BaseDevice, bool>(device, false);
            }

            if (errors.ErrorPort)
            {
                return new KeyValuePair<BaseDevice, bool>(device, false);
            }
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }

        var keyValuePair = new KeyValuePair<BaseDevice, bool>(device, true);

        return keyValuePair;
    }

    /// <summary>
    /// Получение портов с ошбиками 
    /// </summary>
    /// <param name="devices"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private Dictionary<BaseDevice, bool> GetVerifiedPorts(List<BaseDevice> devices)
    {
        var verifiedPorts = new Dictionary<BaseDevice, bool>();
        try
        {
            foreach (var device in devices)
            {
                var item = GetVerifiedPort(device);
                verifiedPorts.Add(item.Key, item.Value);
            }
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }

        return verifiedPorts;
    }

    /// <summary>
    /// Получение устройства с ошибками 
    /// </summary>
    /// <param name="checkDevices">Временный список устройств</param>
    /// <returns>ошибка</returns>
    private KeyValuePair<BaseDevice, bool> GetVerifiedDevice(BaseDevice device)
    {
        KeyValuePair<BaseDevice, bool> keyValuePair = new KeyValuePair<BaseDevice, bool>();
        try
        {
            var errors = device.AllDeviceError;

            keyValuePair = GetVerifiedPort(device);

            if (!keyValuePair.Value)
            {
                return keyValuePair;
            }

            if (errors.ErrorReceive)
            {
                return new KeyValuePair<BaseDevice, bool>(device, false);
            }

            if (errors.ErrorTerminator)
            {
                return new KeyValuePair<BaseDevice, bool>(device, false);
            }

            if (errors.ErrorParam)
            {
                return new KeyValuePair<BaseDevice, bool>(device, false);
            }

            if (errors.ErrorLength)
            {
                return new KeyValuePair<BaseDevice, bool>(device, false);
            }

            if (errors.ErrorTimeout)
            {
                return new KeyValuePair<BaseDevice, bool>(device, false);
            }
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }

        keyValuePair = new KeyValuePair<BaseDevice, bool>(device, true);
        return keyValuePair;
    }

    /// <summary>
    /// Получение устройства с ошибками 
    /// </summary>
    /// <param name="devices"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private Dictionary<BaseDevice, bool> GetVerifiedDevices(List<BaseDevice> devices)
    {
        var verifiedDevices = new Dictionary<BaseDevice, bool>();
        try
        {
            foreach (var device in devices)
            {
                var item = GetVerifiedDevice(device);
                verifiedDevices.Add(item.Key, item.Value);
            }
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }

        return verifiedDevices;
    }

    //--params--chechkparams--value--
    private async Task<Dictionary<BaseDevice, List<string>>> SetCheckValueInDevice(BaseDevice device,
        string setCmd = null, string param = null, int countChecked = 3, int loopDelay = 600, TempChecks t = null,
        params string[] getCmds)
    {
        var deviceReceived = new Dictionary<BaseDevice, List<string>>();

        s1.Restart();

        for (int i = 1; i <= countChecked; i++)
        {
            //
            SetPriorityStatusStand(3, $"запись и проверка параметров", device, percentSubTest: 30,
                currentCountCheckedSubTest: $"Попытка: {i.ToString()}/{countChecked}", colorSubTest: Brushes.Teal);
            //

            TempChecks tp = TempChecks.Start();

            if (!string.IsNullOrEmpty(setCmd))
            {
                await WriteIdentCommand(device, setCmd, paramSet: param);
            }


            // Debug.WriteLine($"Установка настроек прибора/{s1.ElapsedMilliseconds} mc/{device.IsDeviceType}");


            //
            SetPriorityStatusStand(3, percentSubTest: 70);
            //

            tp.Add(true);

            foreach (var getCmd in getCmds)
            {
                if (tp.IsOk)
                {
                    var receive = await WriteIdentCommand(device, getCmd, paramGet: param, countChecked: countChecked,
                        loopDelay: loopDelay, t: tp);

                    if (stopMeasurement)
                    {
                        device.StatusTest = StatusDeviceTest.Error;
                        //
                        SetPriorityStatusStand(3, $"запись и проверка параметров , ошибка!", percentSubTest: 100,
                            colorSubTest: Brushes.Red, clearAll: true);
                        //

                        t?.Add(false);
                        return deviceReceived;
                    }

                    if (tp.IsOk)
                    {
                        if (receive.Key != null && !deviceReceived.ContainsKey(receive.Key))
                        {
                            //добавляем туда данные и создаем список в ктороые будет записаны ответы от прибора
                            deviceReceived.Add(device, new List<string>());
                        }

                        if (receive.Key != null) deviceReceived[receive.Key].Add(receive.Value);
                    }
                }
            }

            //
            // Debug.WriteLine($"Установка настроек проверка/{s1.ElapsedMilliseconds} mc/раз - {i}/{device.IsDeviceType}");
            //
            if (!tp.IsOk)
            {
                continue;
            }

            device.StatusTest = StatusDeviceTest.Ok;
            //
            SetPriorityStatusStand(3, $"запись и проверка параметров, ок!", percentSubTest: 100, clearAll: true);
            //

            //
            // Debug.WriteLine($"Установка настроек прибора, Ок/{s1.ElapsedMilliseconds} mc/{device.IsDeviceType}");


            t?.Add(true);
            return deviceReceived;
        }

        device.StatusTest = StatusDeviceTest.Error;
        //
        SetPriorityStatusStand(3, $"запись и проверка параметров , ошибка!", percentSubTest: 100,
            colorSubTest: Brushes.Red, clearAll: true);
        //

        //
        //Debug.WriteLine($"Установка настроек прибора, Ок/{s1.ElapsedMilliseconds} mc/{device.IsDeviceType}");
        //

        t?.Add(false);
        return deviceReceived;
    }

    private (string cannel1, string cannel2) GetRelayChannelCmds(int idVip)
    {
        (string, string ) cmdNames = idVip switch
        {
            0 => ("On 01", "On 11"),
            1 => ("On 02", "On 12"),
            2 => ("On 03", "On 13"),
            3 => ("On 04", "On 14"),
            4 => ("On 05", "On 15"),
            5 => ("On 06", "On 16"),
            6 => ("On 07", "On 17"),
            7 => ("On 08", "On 18"),
            8 => ("On 09", "On 19"),
            9 => ("On 0A", "On 1A"),
            10 => ("On 0B", "On 1B"),
            11 => ("On 0C", "On 1C"),
            _ => (null, null)
        };
        return cmdNames;
    }

    private (string cannel1, string cannel2) GetLoadChannelCmds(int idVip)
    {
        (string, string ) cmdNames = idVip switch
        {
            0 => ("On 01", "On 11"),
            1 => ("On 02", "On 12"),
            2 => ("On 03", "On 13"),
            3 => ("On 04", "On 14"),
            4 => ("On 05", "On 15"),
            5 => ("On 06", "On 16"),
            6 => ("On 07", "On 17"),
            7 => ("On 08", "On 18"),
            8 => ("On 09", "On 19"),
            9 => ("On 0A", "On 1A"),
            10 => ("On 0B", "On 1B"),
            11 => ("On 0C", "On 1C"),
            _ => (null, null)
        };
        return cmdNames;
    }


    private async Task<bool> SetTestChannelVip(Vip vip, int channel = 1, int innerCountCheck = 3, int innerDelay = 200,
        TempChecks t = null)
    {
        s.Restart();
        var relayMeter = devices.GetTypeDevice<RelayMeter>();
        var nameCmds = GetRelayChannelCmds(vip.Id);

        TempChecks tp = TempChecks.Start();

        if (channel == 1)
        {
            SetPriorityStatusStand(2, $"переключение 1 канала измерений Вип - {vip.Name}", percentSubTest: 40,
                colorSubTest: Brushes.DarkSalmon, clearAll: true);

            await WriteIdentCommand(relayMeter, nameCmds.cannel1, countChecked: innerCountCheck, loopDelay: innerDelay,
                t: tp);
        }

        if (channel == 2)
        {
            SetPriorityStatusStand(2, $"переключение 2 канала измерений Вип - {vip.Name}", percentSubTest: 80,
                colorSubTest: Brushes.DarkSalmon, clearAll: true);

            await WriteIdentCommand(relayMeter, nameCmds.cannel2, countChecked: innerCountCheck, loopDelay: innerDelay,
                t: tp);
        }

        if (tp.IsOk)
        {
            if (channel == 1)
            {
                vip.StatusChannelVipTest = StatusChannelVipTest.One;
            }

            if (channel == 2)
            {
                vip.StatusChannelVipTest = StatusChannelVipTest.Two;
            }

            SetPriorityStatusStand(2, $"переключение канала {channel} измерений Вип - {vip.Name}, ок!",
                percentSubTest: 100,
                colorSubTest: Brushes.DarkSalmon, clearAll: true);

            relayMeter.StatusTest = StatusDeviceTest.Ok;

            t?.Add(true);
            return true;
        }

        if (channel == 1)
        {
            vip.StatusChannelVipTest = StatusChannelVipTest.OneError;
            Debug.WriteLine($"releMeter {vip.Id}/1ch " + devices?.FirstOrDefault(r => r is RelayMeter)?.ErrorStatus);
        }

        ;


        if (channel == 2)
        {
            vip.StatusChannelVipTest = StatusChannelVipTest.TwoError;
            Debug.WriteLine($"releMeter {vip.Id}/2ch " + devices?.FirstOrDefault(r => r is RelayMeter)?.ErrorStatus);
        }

        SetPriorityStatusStand(2, $"переключение канала {channel} измерений Вип - {vip.Name}, ошибка!",
            percentSubTest: 100,
            colorSubTest: Brushes.Red, clearAll: true);

        relayMeter.StatusTest = StatusDeviceTest.Error;
        t?.Add(false);
        return false;
    }

    private decimal GetValueReceives(BaseDevice device, Dictionary<BaseDevice, List<string>> receiveData)
    {
        var val = decimal.Parse(
            CastToNormalValues(receiveData.FirstOrDefault(x => x.Key.IsDeviceType == device.IsDeviceType).Value
                .Last()));
        return val;
    }

    private decimal GetValueReceive(BaseDevice device, KeyValuePair<BaseDevice, string> receiveData)
    {
        var val = decimal.Parse(CastToNormalValues(receiveData.Value));
        return val;
    }

    private async Task<bool> GetErrorInVip(Vip vip, int countChecked = 3, int innerCountCheck = 1, int delay = 200,
        TempChecks t = null, TempChecks te = null)
    {
        TempChecks tp = TempChecks.Start();
        //var receiveDataVip = await WriteIdentCommand(vip.Relay, "Status", t: tp);

        bool availableRelay = true;

        for (int i = 1; i <= countChecked; i++)
        {
            //
            SetPriorityStatusStand(2, $"предварительная проверка на ошибку", percentSubTest: 50,
                currentCountCheckedSubTest: $"Попытка: {i.ToString()}/{countChecked}", colorSubTest: Brushes.Teal,
                currentVipSubTest: vip, clearAll: true);
            // 
            var receiveDataVip = await WriteIdentCommand(vip.Relay, "Test", countChecked: innerCountCheck, t: tp,
                loopDelay: delay,
                isReceiveVal: true);

            if (tp.IsOk)
            {
                try
                {
                    var errorStatus = receiveDataVip.Value.Substring(4, 2);
                    //vip.ErrorVip.ResetAllError();

                    if (errorStatus.Contains("01"))
                    {
                        // выс I вх
                        vip.ErrorVip.CurrentInHigh = true;
                    }

                    else if (errorStatus.Contains("02"))
                    {
                        // низк U1
                        vip.ErrorVip.VoltageOut1Low = true;
                    }

                    else if (errorStatus.Contains("04"))
                    {
                        // выс U1
                        vip.ErrorVip.VoltageOut1High = true;
                    }

                    else if (errorStatus.Contains("08"))
                    {
                        // низк U2
                        vip.ErrorVip.VoltageOut2Low = true;
                    }

                    else if (errorStatus.Contains("16"))
                    {
                        // выс U2
                        vip.ErrorVip.VoltageOut2High = true;
                    }

                    else if (errorStatus.Contains("15"))
                    {
                        // выс I вх, выс U1, выс U2
                        vip.ErrorVip.CurrentInHigh = true;
                        vip.ErrorVip.VoltageOut1High = true;
                        vip.ErrorVip.VoltageOut2High = true;
                    }

                    else if (errorStatus.Contains("14"))
                    {
                        // выс U1, выс U2
                        vip.ErrorVip.VoltageOut1High = true;
                        vip.ErrorVip.VoltageOut2High = true;
                    }

                    else if (errorStatus.Contains("05"))
                    {
                        // выс I вх, выс U1
                        vip.ErrorVip.CurrentInHigh = true;
                        vip.ErrorVip.VoltageOut1High = true;
                    }

                    else if (errorStatus.Contains("11"))
                    {
                        // выс I вх, выс U2
                        vip.ErrorVip.CurrentInHigh = true;
                        vip.ErrorVip.VoltageOut2High = true;
                    }

                    else if (errorStatus.Contains("0B"))
                    {
                        // выс I вх, низк U1, низк U2
                        vip.ErrorVip.CurrentInHigh = true;
                        vip.ErrorVip.VoltageOut1Low = true;
                        vip.ErrorVip.VoltageOut2Low = true;
                    }

                    else if (errorStatus.Contains("10"))
                    {
                        // низк U1, низк U2
                        vip.ErrorVip.VoltageOut1Low = true;
                        vip.ErrorVip.VoltageOut2Low = true;
                    }

                    else if (errorStatus.Contains("03"))
                    {
                        // выс I вх, низк U1
                        vip.ErrorVip.CurrentInHigh = true;
                        vip.ErrorVip.VoltageOut1Low = true;
                    }

                    else if (errorStatus.Contains("09"))
                    {
                        // выс I вх, низк U2
                        vip.ErrorVip.CurrentInHigh = true;
                        vip.ErrorVip.VoltageOut2Low = true;
                    }
                    else if (!vip.ErrorVip.CheckIsUnselectError())
                    {
                        vip.StatusTest = StatusDeviceTest.Ok;
                        //
                        SetPriorityStatusStand(2, $"ошибок не обнаружено!", percentSubTest: 100);
                        // 
                        t?.Add(true);
                        te?.Add(true);
                        return true;
                    }

                    break;
                }
                catch (ArgumentOutOfRangeException e)
                {
                    if (countChecked == i)
                    {
                        availableRelay = false;
                    }
                }
            }
        }

        if (availableRelay)
        {
            //
            SetPriorityStatusStand(2, $"Вип - {vip.Name} не отвчает", percentSubTest: 100,
                colorSubTest: Brushes.Red);
            // 
        }

        if (!availableRelay)
        {
            //
            SetPriorityStatusStand(2, $"обнаружена ошибка/ки!", percentSubTest: 100,
                colorSubTest: Brushes.Red);
            // 
        }

        vip.StatusTest = StatusDeviceTest.Error;
        vipsTested = GetIsTestedVips();
        vipsStopped = GetStoppedVips();

        t?.Add(tp.IsOk);
        te?.Add(!vip.ErrorVip.CheckIsUnselectError());
        return false;
    }

    private bool testVipPlay = false;

    //--tv--test
    private async Task<bool> TestVip(Vip vip, TypeOfTestRun typeTest, int innerCountCheck = 3, int innerDelay = 200,
        TempChecks t = null)
    {
        testVipPlay = true;
        s.Restart();
        //
        SetPriorityStatusStand(2, $"тест Випа", percentSubTest: 0, colorSubTest: Brushes.Violet, currentVipSubTest: vip,
            clearAll: true);
        //
        TempChecks tp = TempChecks.Start();

        var isError = false;


        decimal voltage1 = vip.VoltageOut1;
        decimal voltage2 = vip.VoltageOut2;
        decimal current = vip.CurrentIn;
        decimal temperature = vip.Temperature;

        //TODO уточнить нужны ли вообще и здесь или где
        //проверка приборов входящих в стенд при цикл. испытаниях
        // if (typeTest is TypeOfTestRun.CyclicMeasurement or TypeOfTestRun.CycleCheck)
        // {
        //     currentDevice = devices.GetTypeDevice<Supply>();
        //     await WriteIdentCommand(currentDevice, "Status", t: tp);
        //     
        //     currentDevice = devices.GetTypeDevice<BigLoad>();
        //     await WriteIdentCommand(currentDevice, "Status", t: tp);
        //     
        //     //TODO вернуть когда появятся все нагрузки или исправится номера текущих
        //     //var ss = allDevices.FirstOrDefault(x => x.Name.Contains("SL") && x.Prefix == vip.Id.ToString());
        //     //TODO удалить когда появятся все нагрузки или исправится номера текущих
        //     var ss = allRelayVips.FirstOrDefault(x => x.Name.Contains("SL") && x.Prefix == "4");
        //
        //     await WriteIdentCommand(ss, "Status", t: tp);
        // }

        //TODO уточнить остать тут или пернести вовне метода имзерений тк в отличчии от вольтамперметра пока что волтметр не меняет настройки
        // //волтьтметра
        // currentDevice = devices.GetTypeDevice<VoltMeter>();
        // var getVoltValues = GetParameterForDevice().VoltValues;
        // if (tp.IsOk)
        //     await SetCheckValueInDevice(currentDevice, "Set volt meter", getVoltValues.VoltMaxLimit,
        //         innerCountCheck, innerDelay, tp, "Get func", "Get volt meter");

        var getThermoCurrentValues = GetParameterForDevice().VoltCurrentValues;
        if (vip.Type.PrepareMaxVoltageOut2 > 0)
        {
            //волтьтамеперметра
            currentDevice = devices.GetTypeDevice<VoltCurrentMeter>();
            if (tp.IsOk)
                await SetCheckValueInDevice(currentDevice, "Set volt meter", getThermoCurrentValues.VoltMaxLimit,
                    innerCountCheck, innerDelay, tp, "Get func", "Get volt meter");
        }

        // //
        // Debug.WriteLine($"Установка приборов/{s.ElapsedMilliseconds} mc /{typeTest}/Вип - {vip.Id}");
        // s.Restart();
        // //

        //TODO уточнить до или после Проверка на внутренние ошибки плат Випов и в каких случаях (AvailabilityCheckVip/MeasurementZero etc)
        //Включение реле Випа (если уже не включен)
        if (typeTest is TypeOfTestRun.AvailabilityCheckVip or TypeOfTestRun.MeasurementZero)
        {
            if (tp.IsOk) await OutputDevice(vip.Relay, t: tp);
        }

        // //
        // Debug.WriteLine($"Включение випа/{s.ElapsedMilliseconds} mc /{typeTest}/Вип - {vip.Id}");
        // s.Restart();
        // //

        //TODO вернуть!
        //TODO уточнить до или после Включение реле Випа и в каких случаях (AvailabilityCheckVip/MeasurementZero etc)
        // //Проверка на внутренние ошибки плат Випов
        // TempChecks tpe = TempChecks.Start();
        // //алгоритм проверки текущего випа на внутренние ошибки
        // if (tp.IsOk)
        //   await GetErrorInVip(vip, t: tp, te: tpe);
        //
        // if (!tpe.IsOk && tp.IsOk)
        // {
        //     try
        //     {
        //         await report.CreateReport(vip, true);
        //         await report.CreateErrorReport(vip);
        //     }
        //     catch (Exception e)
        //     {
        //         //
        //         SetPriorityStatusStand(2, $"Запись репорта сбоя, ошибка!", percentSubTest: 0, colorSubTest: Brushes.Red,
        //             currentVipSubTest: vip,
        //             clearAll: true);
        //         //
        //         throw new Exception($"Ошибка записи репорта сбоя - {e.Message}!");
        //     }
        //
        //     t?.Add(true);
        //     return true;
        // }
        //TODO вернуть!


        // //
        // Debug.WriteLine($"Проверка реле випа/{s.ElapsedMilliseconds} mc /{typeTest}/Вип - {vip.Id}");
        // s.Restart();
        // //
        //
        if (tp.IsOk)
        {
            //переключение канала измерений Випа на 1
            if (tp.IsOk) await SetTestChannelVip(vip, 1, t: tp);

            // //
            // Debug.WriteLine($"Установка канала 1/{s.ElapsedMilliseconds} mc /{typeTest}/Вип - {vip.Id}");
            // s.Restart();
            //

            //TODO подбирать вручную
            //TODO уточнить нужно ли выводить на тексбокс
            //интервал для времени устаканивания напряжений 

            var delay = typeTest switch
            {
                //TODO уточнить про эти значение задержки 
                TypeOfTestRun.AvailabilityCheckVip => 1000,
                TypeOfTestRun.MeasurementZero => vip.Type.ZeroTestInterval,
                TypeOfTestRun.CyclicMeasurement or TypeOfTestRun.CycleCheck => 1000,
                _ => 1000
            };

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(delay), ctsAllCancel.Token);
            }

            catch (TaskCanceledException e) when (ctsAllCancel.IsCancellationRequested)
            {
                t?.Add(false);
                return false;
            }

            //очистка если были ДО и добавление в список приборов для измерения показаний
            currentDevices.Clear();
            currentDevices.Add(devices.GetTypeDevice<VoltMeter>());

            if (vip.Type.PrepareMaxVoltageOut2 > 0)
            {
                currentDevices.Add(devices.GetTypeDevice<VoltCurrentMeter>());
            }

            //измерение показаний 1 канала с двух подканалов
            var receivesData1 = await WriteIdentCommands(currentDevices, "Get all value", t: tp, isReceiveVal: true);

            //
            SetPriorityStatusStand(2, $"Тест Випа: ", percentSubTest: 30, colorSubTest: Brushes.Violet,
                currentVipSubTest: vip, clearAll: true);
            //

            //задание чекера для 1 и 2 подканала напряжений
            TempChecks tpv1 = TempChecks.Start();
            TempChecks tpv2 = TempChecks.Start();

            TypeCheckVal typeVolt1 = TypeCheckVal.None;
            TypeCheckVal typeVolt2 = TypeCheckVal.None;

            if (tp.IsOk)
            {
                //TODO уточнить правильно ли сделано (в каком случае каки дожны быть значения и вывод их в тектбоксы)

                //проверка напряжений для режима проверки наличия и 0 замера
                //1 канала на на сосответвие, приведение их в удобочитаемый вид и запись значений в соотв Вип

                switch (typeTest)
                {
                    case TypeOfTestRun.AvailabilityCheckVip or TypeOfTestRun.MeasurementZero:
                        typeVolt1 = TypeCheckVal.PrepareVoltage1;
                        typeVolt2 = TypeCheckVal.PrepareVoltage2;
                        break;
                    case TypeOfTestRun.CyclicMeasurement or TypeOfTestRun.CycleCheck:
                        typeVolt1 = TypeCheckVal.Voltage1;
                        typeVolt2 = TypeCheckVal.Voltage2;
                        break;
                }

                //проверка замеров напряжения 1 подканал 
                voltage1 = GetValueReceives(devices.GetTypeDevice<VoltMeter>(), receivesData1);
                CheckValueInVip(vip, voltage1, typeVolt1, tpv1);
                vip.VoltageOut1 = voltage1;

                //проверка замеров напряжения 2 подканал 
                if (vip.Type.PrepareMaxVoltageOut2 > 0)
                {
                    voltage2 = GetValueReceives(devices.GetTypeDevice<VoltCurrentMeter>(), receivesData1);
                    CheckValueInVip(vip, voltage2, typeVolt2, tpv2);
                    vip.VoltageOut2 = voltage2;
                }
            }

            // //
            // Debug.WriteLine($"Замер напряжений/{s.ElapsedMilliseconds - 500} mc /{typeTest}/Вип - {vip.Id}");
            // s.Restart();
            // //

            //переключение волтьтамеперметра в режим амперметра
            currentDevice = devices.GetTypeDevice<VoltCurrentMeter>();
            if (tp.IsOk)
                await SetCheckValueInDevice(currentDevice, "Set curr meter", getThermoCurrentValues.CurrMaxLimit,
                    innerCountCheck, innerDelay, tp, "Get func", "Get curr meter");

            //TODO уточнить нужа ли зажерка аналигично - интервалу для времени устаканивания напряжений 

            //переключение на 2 канал для измерения тока
            if (tp.IsOk) await SetTestChannelVip(vip, 2, t: tp);

            // //
            // Debug.WriteLine($"Переключение на 2 канал /{s.ElapsedMilliseconds} mc /{typeTest}/Вип - {vip.Id}");
            // s.Restart();
            // //

            //измерение тока 2 канала с 1 подканала 
            currentDevice = devices.GetTypeDevice<VoltCurrentMeter>();
            var receiveData2 = await WriteIdentCommand(currentDevice, "Get all value", t: tp, isReceiveVal: true);

            //задание чекера для 1 подканала тока
            TempChecks tpc = TempChecks.Start();
            TypeCheckVal typeCurr = TypeCheckVal.None;

            if (tp.IsOk)
            {
                //TODO уточнить правильно ли сделано (в каком случае каки дожны быть значения и вывод их в тектбоксы)

                //2 канала на на сосответвие, приведение их в удобочитаемый вид и запись значений в соотв Вип
                current = GetValueReceive(devices.GetTypeDevice<VoltCurrentMeter>(), receiveData2);

                //проверка тока для режима проверки наличия
                if (typeTest is TypeOfTestRun.AvailabilityCheckVip)
                {
                    typeCurr = TypeCheckVal.AvailabilityCurrent;
                }

                //проверка тока для режима 0 замера
                if (typeTest is TypeOfTestRun.MeasurementZero)
                {
                    typeCurr = TypeCheckVal.PrepareCurrent;
                }

                //проверка напряжений для режима цикла испытаний
                if (typeTest is TypeOfTestRun.CyclicMeasurement or TypeOfTestRun.CycleCheck)
                {
                    typeCurr = TypeCheckVal.Current;
                }

                CheckValueInVip(vip, current, typeCurr, tpc);
                vip.CurrentIn = current;
            }

            // //
            // Debug.WriteLine($"Замер тока/{s.ElapsedMilliseconds} mc /{typeTest}/Вип - {vip.Id}");
            // s.Restart();
            // //

            //TODO вернуть каогда появится термометр
            // currentDevice = devices.GetTypeDevice<Thermometer>();
            // var receiveDataT = await WriteIdentCommand(currentDevice, "Get all value", t: tp, isReceiveVal: true);
            //задание чекера для температуры
            TempChecks tpt = TempChecks.Start();
            // if (tp.IsOk)
            // {
            //     temperature = GetValueReceive(devices.GetTypeDevice<VoltMeter>(), receiveDataT);
            //     CheckValueInVip(vip, temperature, TypeCheckVal.Temperature, tpt);
            // }
            //TODO вернуть каогда появится термометр

            bool extraError = true;
            isError = false;

            //если какойто из чекеров false
            if (!tpv1.IsOk || !tpv2.IsOk || !tpc.IsOk || !tpt.IsOk)
            {
                await OutputDevice(vip.Relay, t: tp, on: false);

                //для добавления косой черты в строку сообщения
                extraError = false;
                isError = true;
                vip.StatusTest = StatusDeviceTest.Error;
                vip.ErrorStatusVip = null;

                decimal typeVipVoltage1 = 0;
                decimal typeVipVoltage2 = 0;
                decimal typeVipcurrent = 0;

                if (typeTest is TypeOfTestRun.AvailabilityCheckVip or TypeOfTestRun.MeasurementZero)
                {
                    typeVipVoltage1 = vip.Type.PrepareMaxVoltageOut1;
                    typeVipVoltage2 = vip.Type.PrepareMaxVoltageOut2;

                    if (typeTest is TypeOfTestRun.AvailabilityCheckVip)
                    {
                        typeVipcurrent = vip.Type.AvailabilityMaxCurrentIn;
                    }
                    else
                    {
                        typeVipcurrent = vip.Type.AvailabilityMaxCurrentIn;
                    }
                }

                if (typeTest is TypeOfTestRun.CyclicMeasurement or TypeOfTestRun.CycleCheck)
                {
                    await OutputLoadInChannel(vip, t, false);

                    typeVipVoltage1 = vip.Type.MaxVoltageOut1;
                    typeVipVoltage2 = vip.Type.MaxVoltageOut2;
                    typeVipcurrent = vip.Type.MaxCurrentIn;
                }

                if (!tpv1.IsOk)
                {
                    if (voltage1 > typeVipVoltage1)
                    {
                        vip.ErrorVip.VoltageOut1High = true;
                        var over = voltage1 - typeVipVoltage1;
                        vip.ErrorStatusVip = $"U1вых.↑ на {over}В ";
                        extraError = true;
                    }

                    if (voltage1 < typeVipVoltage1)
                    {
                        vip.ErrorVip.VoltageOut1Low = true;
                        var over = typeVipVoltage1 - voltage1;
                        vip.ErrorStatusVip = $"U1вых.↓ на {over}В ";
                        extraError = true;
                    }
                }

                if (!tpv2.IsOk)
                {
                    if (voltage2 > typeVipVoltage2)
                    {
                        if (extraError)
                        {
                            vip.ErrorStatusVip += "/";
                        }

                        vip.ErrorVip.VoltageOut2High = true;
                        var over = voltage2 - typeVipVoltage2;
                        vip.ErrorStatusVip += $"U1вых.↑ на {over}В ";
                        extraError = true;
                    }

                    if (voltage2 < typeVipVoltage2)
                    {
                        if (extraError)
                        {
                            vip.ErrorStatusVip += "/";
                        }

                        vip.ErrorVip.VoltageOut2Low = true;
                        var over = typeVipVoltage2;
                        vip.ErrorStatusVip += $"U1вых.↓ на {over}В ";
                        extraError = true;
                    }
                }

                if (!tpc.IsOk)
                {
                    if (current > typeVipcurrent)
                    {
                        if (extraError)
                        {
                            vip.ErrorStatusVip += "/";
                        }

                        vip.ErrorVip.CurrentInHigh = true;
                        var over = current - typeVipcurrent;
                        vip.ErrorStatusVip += $" Iвх.↑ на {over}A ";
                        extraError = true;
                    }

                    if (current < typeVipcurrent)
                    {
                        if (extraError)
                        {
                            vip.ErrorStatusVip += "/";
                        }

                        vip.ErrorVip.CurrentInHigh = true;
                        var over = typeVipcurrent - current;
                        vip.ErrorStatusVip += $" Iвх.↓ на {over}A ";
                        extraError = true;
                    }
                }

                if (!tpt.IsOk)
                {
                    if (temperature > vip.Type.MaxTemperature)
                    {
                        if (extraError)
                        {
                            vip.ErrorStatusVip += "/";
                        }

                        vip.ErrorVip.TemperatureHigh = true;
                        var over = temperature - vip.Type.MaxTemperature;
                        vip.ErrorStatusVip += $"T↑ на {over}℃";
                        extraError = true;
                    }

                    if (temperature < vip.Type.MaxTemperature)
                    {
                        if (extraError)
                        {
                            vip.ErrorStatusVip += "/";
                        }

                        vip.ErrorVip.TemperatureHigh = true;
                        var over = vip.Type.MaxTemperature - temperature;
                        vip.ErrorStatusVip += $"T↓ на {over}℃";
                        extraError = true;
                    }
                }
            }
        }

        vipsTested = GetIsTestedVips();
        vipsStopped = GetStoppedVips();

        if (tp.IsOk)
        {
            if (!isError)
            {
                vip.StatusTest = StatusDeviceTest.Ok;
                vip.ErrorStatusVip = "Ok";
                if (typeTest is TypeOfTestRun.MeasurementZero or TypeOfTestRun.CyclicMeasurement)
                {
                    //
                    SetPriorityStatusStand(2, $"Запись репорта", percentSubTest: 0, colorSubTest: Brushes.Sienna,
                        currentVipSubTest: vip,
                        clearAll: true);
                    //
                    try
                    {
                        await report.CreateReport(vip);
                        vip.Channel1AddrNum++;
                        vip.Channel2AddrNum++;
                    }
                    catch (Exception e)
                    {
                        //
                        SetPriorityStatusStand(2, $"Запись репорта, ошибка!", percentSubTest: 100,
                            colorSubTest: Brushes.Red, currentVipSubTest: vip, clearAll: true);
                        //
                        throw new Exception($"Ошибка записи репорта - {e.Message}!");
                    }

                    //
                    SetPriorityStatusStand(2, $"Запись репорта, Ок!", percentSubTest: 0, colorSubTest: Brushes.Sienna,
                        currentVipSubTest: vip, clearAll: true);
                    //
                }
            }
            else
            {
                //
                SetPriorityStatusStand(2, $"Запись репорта сбоя", percentSubTest: 0, colorSubTest: Brushes.Sienna,
                    currentVipSubTest: vip, clearAll: true);
                //

                try
                {
                    if (typeTest is TypeOfTestRun.MeasurementZero or TypeOfTestRun.CyclicMeasurement
                        or TypeOfTestRun.CycleCheck)
                    {
                        await report.CreateReport(vip, true);
                    }

                    await report.CreateErrorReport(vip);
                }
                catch (Exception e)
                {
                    //
                    SetPriorityStatusStand(2, $"Запись репорта сбоя, ошибка!", percentSubTest: 0,
                        colorSubTest: Brushes.Red,
                        currentVipSubTest: vip,
                        clearAll: true);
                    //
                    throw new Exception($"Ошибка записи репорта сбоя - {e.Message}!");
                }

                //
                SetPriorityStatusStand(2, $"Запись репорта сбоя, Ок!", percentSubTest: 0, colorSubTest: Brushes.Sienna,
                    currentVipSubTest: vip,
                    clearAll: true);
                //
            }

            vip.StatusChannelVipTest = StatusChannelVipTest.None;

            t.Add(true);
            return true;
        }

        try
        {
            if (typeTest is TypeOfTestRun.MeasurementZero or TypeOfTestRun.CyclicMeasurement
                or TypeOfTestRun.CycleCheck)
            {
                await report.CreateReport(vip, true);
            }

            await report.CreateErrorReport(vip);
        }
        catch (Exception e)
        {
            //
            SetPriorityStatusStand(2, $"Запись репорта сбоя, ошибка!", percentSubTest: 0, colorSubTest: Brushes.Red,
                currentVipSubTest: vip,
                clearAll: true);
            //
            throw new Exception($"Ошибка записи репорта сбоя - {e.Message}!");
        }

        vip.StatusChannelVipTest = StatusChannelVipTest.None;
        vip.VoltageOut1 = 0;
        vip.VoltageOut2 = 0;
        vip.CurrentIn = 0;
        t.Add(false);
        return false;
    }

    private (bool isChecked, Threshold threshold) CheckValueInVip(Vip vip, decimal receiveVal,
        TypeCheckVal typeCheckVal,
        TempChecks t = null)
    {
        decimal vipMaxVal = 0;
        decimal vipPercentVal = 0;

        //

        if (typeCheckVal == TypeCheckVal.Temperature)
        {
            vipMaxVal = vip.Type.MaxTemperature;
            vipPercentVal = vip.Type.PercentAccuracyTemperature;
        }

        //

        if (typeCheckVal == TypeCheckVal.Voltage1)
        {
            vipMaxVal = vip.Type.MaxVoltageOut1;
            vipPercentVal = vip.Type.PercentAccuracyVoltages;
        }

        if (typeCheckVal == TypeCheckVal.Voltage2)
        {
            vipMaxVal = vip.Type.MaxVoltageOut2;
            vipPercentVal = vip.Type.PercentAccuracyVoltages;
        }

        if (typeCheckVal == TypeCheckVal.PrepareVoltage1)
        {
            vipMaxVal = vip.Type.PrepareMaxVoltageOut1;
            vipPercentVal = vip.Type.PercentAccuracyVoltages;
        }

        if (typeCheckVal == TypeCheckVal.PrepareVoltage2)
        {
            vipMaxVal = vip.Type.PrepareMaxVoltageOut2;
            vipPercentVal = vip.Type.PercentAccuracyVoltages;
        }

        //

        if (typeCheckVal == TypeCheckVal.Current)
        {
            vipMaxVal = vip.Type.MaxCurrentIn;
            vipPercentVal = vip.Type.PercentAccuracyCurrent;
        }

        if (typeCheckVal == TypeCheckVal.PrepareCurrent)
        {
            vipMaxVal = vip.Type.PrepareMaxCurrentIn;
            vipPercentVal = vip.Type.PercentAccuracyCurrent;
        }

        if (typeCheckVal == TypeCheckVal.AvailabilityCurrent)
        {
            vipMaxVal = vip.Type.AvailabilityMaxCurrentIn;
            vipPercentVal = vip.Type.PercentAccuracyCurrent;
        }

        //

        //нахождение процента погрешности значений Випа
        var valPercentError = (vipMaxVal / 100) * vipPercentVal;

        decimal valMin = vipMaxVal - valPercentError;
        decimal valMax = vipMaxVal + valPercentError;

        // входит ли receiveValue  в диапазон от numMin до numMax

        var isChecked = false;
        var threshold = Threshold.Normal;

        if (receiveVal >= valMin && receiveVal <= valMax)
        {
            threshold = Threshold.Normal;
            isChecked = true;
        }
        else if (receiveVal < valMin)
        {
            threshold = Threshold.Low;
        }
        else if (receiveVal > valMax)
        {
            threshold = Threshold.High;
        }

        t?.Add(isChecked);
        return (isChecked, threshold);
    }

    #endregion

    #region Обработка --событий/--ответов с приборов

    ///Прием ответа от устройства
    Dictionary<BaseDevice, bool> connectDevice = new();

    private void Port_Connecting(BaseDevice device, bool isConnect)
    {
        if (isConnect)
        {
            device.AllDeviceError.ErrorPort = false;
            device.AllDeviceError.ErrorDevice = false;

            //если прибор от кторого приходят данные не содержится в библиотеке а при первом приеме данных так и будет
            if (!connectDevice.ContainsKey(device))
            {
                //добавляем туда данные и создаем список в ктороые будет записаны ответы от прибора
                connectDevice.Add(device, true);
            }
        }

        //далее в библиотеку текущего приобра пишем данные
        connectDevice[device] = true;

        var baseConnects = currentConnectDevices.Except(connectDevice.Keys).ToList();

        if (!baseConnects.Any())
        {
            ctsConnectDevice.Cancel();
        }
    }

    private void Device_Error(BaseDevice device, string err)
    {
        if (err.Contains("Device Error"))
        {
            device.ErrorStatus = $"Ошибка уcтройства \"{device.IsDeviceType}\"/сбой устройства";
            device.AllDeviceError.ErrorDevice = true;
        }

        if (err.Contains("Port not found"))
        {
            device.ErrorStatus =
                $"Ошибка уcтройства \"{device.IsDeviceType}\"/сбой порта {device.GetConfigDevice().PortName}";
            device.AllDeviceError.ErrorPort = true;
        }

        if (err.Contains("Access Denied"))
        {
            device.ErrorStatus =
                $"Ошибка уcтройства \"{device.IsDeviceType}\"/порт {device.GetConfigDevice().PortName} заблокирован другим потоком";
            device.AllDeviceError.ErrorPort = true;
        }
    }

    ///Прием ответа от устройства
    Dictionary<BaseDevice, List<string>> receiveInDevice = new();


    private void Device_Receiving(BaseDevice device, string receive, DeviceCmd cmd)
    {
        Debug.WriteLine($"{device.Name}/{device.IsDeviceType}/{receive}/{s.ElapsedMilliseconds}");
        //сброс ошибки таймаута - ответ пришел
        device.AllDeviceError.ErrorTimeout = false;

        //если в команду из билиотекие влючен ответ RX
        if (!string.IsNullOrEmpty(cmd.Receive))
        {
            if (!receive.Contains(cmd.Receive.ToLower()))
            {
                device.AllDeviceError.ErrorReceive = true;
                device.ErrorStatus =
                    $"Ошибка уcтройства {device.IsDeviceType}, команда {device.NameCurrentCmd}/неверный ответ";
            }

            if (receive.Contains(cmd.Receive.ToLower()))
            {
                device.AllDeviceError.ErrorReceive = false;
            }
        }
        else
        {
            device.AllDeviceError.ErrorReceive = false;
        }

        //если в команду из билиотекие влючен терминатор RX
        if (!string.IsNullOrEmpty(cmd.ReceiveTerminator.ReceiveTerminator))
        {
            var terminatorLenght = cmd.ReceiveTerminator.ReceiveTerminator.Length;
            var terminator = receive.Substring(receive.Length - terminatorLenght, terminatorLenght);

            var temp = receive;
            receive = temp.Replace(terminator, "");

            if (terminator != cmd.ReceiveTerminator.ReceiveTerminator || receive.Contains('\r') ||
                receive.Contains('\n'))
            {
                device.AllDeviceError.ErrorTerminator = true;
                device.ErrorStatus =
                    $"Ошибка уcтройства {device.IsDeviceType}, команда {device.NameCurrentCmd}/неверный терминатор";
            }
            else if (terminator == cmd.ReceiveTerminator.ReceiveTerminator)
            {
                device.AllDeviceError.ErrorTerminator = false;
            }
        }
        else
        {
            device.AllDeviceError.ErrorTerminator = false;
        }

        //если в команду включена длина сообщения
        if (!string.IsNullOrEmpty(cmd.Length))
        {
            var receiveLenght = Convert.ToInt32(cmd.Length);

            if (device.CurrentCmd.MessageType == TypeCmd.Hex)
            {
                if (receive.Length / 2 != receiveLenght)
                {
                    device.AllDeviceError.ErrorLength = true;
                    device.ErrorStatus =
                        $"Ошибка уcтройства {device.IsDeviceType}, команда {device.NameCurrentCmd}/неверная длина сообщения";
                }

                if (receive.Length / 2 == receiveLenght)
                {
                    device.AllDeviceError.ErrorLength = false;
                }
            }
            else
            {
                if (receive.Length != receiveLenght)
                {
                    device.AllDeviceError.ErrorLength = true;
                    device.ErrorStatus =
                        $"Ошибка уcтройства {device.IsDeviceType}, команда {device.NameCurrentCmd}/неверная длина сообщения";
                }

                if (receive.Length == receiveLenght)
                {
                    device.AllDeviceError.ErrorLength = false;
                }
            }
        }
        else
        {
            device.AllDeviceError.ErrorLength = false;
        }

        //если из устройство нужно получить каойто параметр
        if (!string.IsNullOrEmpty(device.CurrentParameter))
        {
            receive = CastToNormalValues(receive);

            var isGdmMatch = GetGdmReceive(device, receive);

            if (isGdmMatch.isGdm && isGdmMatch.match)
            {
                device.AllDeviceError.ErrorParam = false;
            }
            else if (!isGdmMatch.isGdm)
            {
                if (device.CurrentParameter.Contains(receive ?? null))
                {
                    device.AllDeviceError.ErrorParam = false;
                }
                else if (decimal.TryParse(receive, out decimal num1))
                {
                    if (decimal.TryParse(device.CurrentParameter, out decimal num2))
                    {
                        if (num1 == num2)
                        {
                            device.AllDeviceError.ErrorParam = false;
                        }
                    }
                }
                else
                {
                    device.ErrorStatus =
                        $"Ошибка уcтройства {device.IsDeviceType}, команда {device.NameCurrentCmd}/неверный параметр, пришел \"{receive}\"/ожидался \"{device.CurrentParameter}\"\n";
                    device.AllDeviceError.ErrorParam = true;
                }
            }
            else
            {
                device.ErrorStatus =
                    $"Ошибка уcтройства {device.IsDeviceType}, команда {device.NameCurrentCmd}/неверный параметр, пришел \"{receive}\"/ожидался \"{device.CurrentParameter}\"\n";
                device.AllDeviceError.ErrorParam = true;
                // Debug.WriteLine(
                //     $"Ошибка уcтройства {device.IsDeviceType}, команда {device.NameCurrentCmd}/неверный параметр, пришел \"{receive}\"/ожидался \"{device.CurrentParameter}\"\n");
            }
        }
        else
        {
            device.AllDeviceError.ErrorParam = false;
        }

        //далее в библиотеку текущего приобра пишем данные
        receiveInDevice[device].Add(receive);

        var baseDevices = CurrentWriteDevices.Except(receiveInDevice.Keys).ToList();

        //
        TestCurrentDevice = device;
        //

        if (!baseDevices.Any())
        {
            ctsReceiveDevice.Cancel();
        }
    }

    private void SetGdmReceive(BaseDevice device, string param)
    {
        if (device.Name.ToLower().Contains("gdm"))
        {
            if (device.NameCurrentCmd.Contains("Set curr"))
            {
                SetParameterGdmDevice(device, ModeGdm.Current, param);
            }

            if (device.NameCurrentCmd.Contains("Set volt"))
            {
                SetParameterGdmDevice(device, ModeGdm.Voltage, param);
            }

            if (device.NameCurrentCmd.Contains("Set term"))
            {
                SetParameterGdmDevice(device, ModeGdm.Themperature);
            }
        }
    }

    private (bool isGdm, bool match) GetGdmReceive(BaseDevice device, string param)
    {
        try
        {
            if (device.Name.ToLower().Contains("gdm"))
            {
                if (device.NameCurrentCmd.Contains("Get func"))
                {
                    if (device is VoltCurrentMeter)
                    {
                        return (true, param.Contains(vips[0].Type.Parameters.VoltCurrentValues.ReturnFuncGDM));
                    }

                    if (device is VoltMeter)
                    {
                        return (true, param.Contains(vips[0].Type.Parameters.VoltValues.ReturnFuncGDM));
                    }
                }
                else if (device.NameCurrentCmd.Contains("Get curr"))
                {
                    return (true, param.Contains(vips[0].Type.Parameters.VoltCurrentValues.ReturnCurrGDM));
                }
                else if (device.NameCurrentCmd.Contains("Get volt"))
                {
                    if (device is VoltCurrentMeter)
                    {
                        return (true, param.Contains(vips[0].Type.Parameters.VoltCurrentValues.ReturnVoltGDM));
                    }

                    if (device is VoltMeter)
                    {
                        return (true, param.Contains(vips[0].Type.Parameters.VoltValues.ReturnVoltGDM));
                    }
                }
            }
        }
        catch (Exception e)
        {
            return (true, false);
        }

        return (false, true);
    }

    #endregion

    #region Обработка/--получение --ответов

    private Dictionary<BaseDevice, List<string>> GetReceives()
    {
        if (receiveInDevice.Any())
        {
            return receiveInDevice;
        }

        return new Dictionary<BaseDevice, List<string>>();
    }

    private Dictionary<BaseDevice, string> GetReceivesLast(List<BaseDevice> devices)
    {
        try
        {
            var results = GetReceives();
            var lastResults = new Dictionary<BaseDevice, string>();

            foreach (var result in results)
            {
                lastResults.Add(result.Key, result.Value.Last());
            }

            return lastResults;
        }
        catch (Exception e)
        {
            return new Dictionary<BaseDevice, string>();
        }
    }

    //-
    private KeyValuePair<BaseDevice, List<string>> GetReceive(BaseDevice device)
    {
        try
        {
            var result =
                receiveInDevice.FirstOrDefault(x => x.Key.Name == device.Name &&
                                                    x.Key.IsDeviceType == device.IsDeviceType);
            return result;
        }
        catch (Exception e)
        {
            return new KeyValuePair<BaseDevice, List<string>>();
            throw new Exception($"Невозмонжо получить ответ от устройства - {device.IsDeviceType}");
        }
    }

    private KeyValuePair<BaseDevice, string> GetReceiveLast(BaseDevice device)
    {
        try
        {
            var last = GetReceive(device).Value.Last();
            var keyValuePairs = new KeyValuePair<BaseDevice, string>(device, last);
            return keyValuePairs;
        }
        catch (Exception e)
        {
            return new KeyValuePair<BaseDevice, string>(device, null);
        }
    }

    //-
    private void RemoveReceives()
    {
        try
        {
            receiveInDevice = new Dictionary<BaseDevice, List<string>>();
        }
        catch (Exception e)
        {
            return;
            throw new Exception("Невозмонжо получить ответ от устройств, поэтому нельзя удалить");
        }
    }

    private void RemoveReceives(List<BaseDevice> devices)
    {
        foreach (var device in devices)
        {
            try
            {
                if (receiveInDevice.ContainsKey(device))
                {
                    //receiveInDevice.Remove(device);
                    receiveInDevice[device] = new List<string>();
                }
            }
            catch (Exception e)
            {
                continue;
                //throw new Exception(
                //     $"Невозмонжо получить ответ от устройства - {device.IsDeviceType}, поэтому нельзя удалить");
            }
        }
    }

    private void RemoveReceive(BaseDevice device)
    {
        try
        {
            receiveInDevice.Remove(device);
        }
        catch (Exception e)
        {
            return;
            //throw new Exception("Невозмонжо получить ответ от данного устройства, поэтому нельзя удалить");
        }
    }


    //--output--on--off--вкл--выкл
    /// <summary>
    /// Включение/выключение устройства
    /// </summary>
    /// <param name="device">Устройство</param>
    /// <param name="channel"></param>
    /// <param name="countChecked"></param>
    /// <param name="innerCountCheck"></param>
    /// <param name="delay"></param>
    /// <param name="t"></param>
    /// <param name="forcedOff"></param>
    /// <param name="on">true - вкл, false - выкл</param>
    /// <param name="forcedAttempt"></param>
    /// <param name="values">Ответ который утройство должно отправить в ответ на запрос output</param>
    /// <returns>Результат включения/выключение</returns>
    private async Task<(BaseDevice outputDevice, bool outputResult)> OutputDevice(BaseDevice device, int channel = -1,
        int countChecked = 3, int innerCountCheck = 1, int delay = 200, TempChecks t = null,
        bool forcedOff = false, bool on = true)
    {
        #region настройка

        string getOutputCmdName = "Get output";
        string SetOutputOnCmdName = "Set output on";
        string SetOutputOffCmdName = "Set output off";

        var getParam = new BaseDeviceValues("1", "0");

        if (device is BigLoad)
        {
            getParam = GetParameterForDevice().BigLoadValues;
        }
        else if (device is Supply)
        {
            getParam = GetParameterForDevice().SupplyValues;
        }
        else if (device is Heat)
        {
            getParam = GetParameterForDevice().HeatValues;
        }
        else if (device is SmallLoad)
        {
            SetOutputOnCmdName = "On";
            SetOutputOffCmdName = "Off";
        }
        else if (device is RelayVip)
        {
            //TODO поменять на соответвующие значения из реле Test когда появятся команды от Влада
            getParam = new BaseDeviceValues("99", "99");
            getOutputCmdName = "Test";
            SetOutputOnCmdName = "On";
            SetOutputOffCmdName = "Off";
        }
        else
        {
            device.StatusOnOff = OnOffStatus.None;

            if (string.IsNullOrEmpty(device.ErrorStatus))
            {
                if (device.StatusTest != StatusDeviceTest.Ok)
                {
                    device.StatusTest = StatusDeviceTest.None;
                }
            }
            else
            {
                device.StatusTest = StatusDeviceTest.Error;
            }


            t?.Add(true);
            return (device, true);
        }

        #endregion

        for (int i = 1; i <= countChecked; i++)
        {
            TempChecks tp = TempChecks.Start();

            if (device.IsNotCmd)
            {
                device.ErrorStatus = $"Остутсвует команда {device.NameCurrentCmd} для устройства {device.Name}!";
                device.StatusOnOff = OnOffStatus.None;
                device.StatusTest = StatusDeviceTest.Error;

                t?.Add(false);
                return (device, false);
            }

            if (tp.IsOk)
            {
                if (device.AllDeviceError.ErrorPort)
                {
                    if (i == 1)
                    {
                        await CheckConnectPort(device, innerCountCheck, t: tp);
                    }
                    else
                    {
                        if (i != countChecked) continue;
                        //
                        SetPriorityStatusStand(3, $"к порту устройства нет доступа, ошибка!", device,
                            percentSubTest: 100,
                            colorSubTest: Brushes.Red, clearAll: true);
                        //

                        device.StatusOnOff = OnOffStatus.None;
                        device.StatusTest = StatusDeviceTest.Error;

                        t?.Add(false);
                        return (device, false);
                    }
                }

                if (!stopMeasurement)
                {
                    #region вкл

                    if (on)
                    {
                        //
                        SetPriorityStatusStand(3, $"включение выхода устройства", device, percentSubTest: 50,
                            currentCountCheckedSubTest: $"Попытка: {i.ToString()}/{countChecked}",
                            colorSubTest: Brushes.BlueViolet);
                        //

                        #region вкл реле випа

                        if (device is RelayVip r)
                        {
                            if (device.Name.Contains("SL"))
                            {
                                switch (channel)
                                {
                                    case 1:
                                    {
                                        var receiveOutput = await WriteIdentCommand(r, "On1",
                                            countChecked: innerCountCheck,
                                            loopDelay: delay, t: tp);
                                        break;
                                    }
                                    case 2:
                                    {
                                        var receiveOutput = await WriteIdentCommand(r, "On2",
                                            countChecked: innerCountCheck,
                                            loopDelay: delay, t: tp);
                                        break;
                                    }
                                    case 3:
                                    {
                                        var receiveOutput = await WriteIdentCommand(r, "On3",
                                            countChecked: innerCountCheck,
                                            loopDelay: delay, t: tp);
                                        break;
                                    }
                                    case 4:
                                    {
                                        var receiveOutput = await WriteIdentCommand(r, "On4",
                                            countChecked: innerCountCheck,
                                            loopDelay: delay, t: tp);
                                        break;
                                    }
                                }

                                if (tp.IsOk)
                                {
                                    //
                                    SetPriorityStatusStand(3, $"#{channel} выход устройства включен", device,
                                        percentSubTest: 100,
                                        colorSubTest: Brushes.BlueViolet, clearAll: true);
                                    //
                                    r.StatusOnOff = OnOffStatus.On;
                                    r.StatusTest = StatusDeviceTest.Ok;

                                    t?.Add(true);
                                    return (device, true);
                                }
                            }
                            else
                            {
                                //TODO добавить когда появтся команды от Влада
                                // //опрос выхода устройства
                                // var cmdResult = await WriteIdentCommand(device, getOutputCmdName, countChecked: innerCountCheck, t: tp);
                                //
                                // //если выход выкл 
                                // if (cmdResult.Value == getParam.OutputOff && tp.IsOk)
                                // {
                                //     //делаем выход вкл
                                //     await WriteIdentCommand(device, SetOutputOnCmdName);
                                //
                                //     //опрос выхода утсртойства
                                //     cmdResult = await WriteIdentCommand(device, getOutputCmdName, countChecked: innerCountCheck,
                                //         loopDelay: externalDelay, t: tp);
                                // }

                                var cmdResult =
                                    await WriteIdentCommand(device, getOutputCmdName, countChecked: innerCountCheck,
                                        t: tp);

                                if (tp.IsOk && r.StatusOnOff == OnOffStatus.Off || r.StatusOnOff == OnOffStatus.None)
                                {
                                    var receiveOutput = await WriteIdentCommand(r, SetOutputOnCmdName,
                                        countChecked: innerCountCheck,
                                        loopDelay: delay, t: tp);
                                }

                                if (tp.IsOk)
                                {
                                    //
                                    SetPriorityStatusStand(3, $"выход устройства включен", device, percentSubTest: 100,
                                        colorSubTest: Brushes.BlueViolet, clearAll: true);
                                    //
                                    r.StatusOnOff = OnOffStatus.On;
                                    r.StatusTest = StatusDeviceTest.Ok;

                                    t?.Add(true);
                                    return (device, true);
                                }

                                //TODO добавить когда появтся команды от Влада
                                // //если выход вкл
                                // if (cmdResult.Value == getParam.OutputOn && tp.IsOk)
                                // {
                                //     //
                                //     SetPriorityStatusStand(3, $"выход устройства включен", device, percentSubTest: 100,
                                //         colorSubTest: Brushes.BlueViolet, clearAll: true);
                                //     //
                                //
                                //     device.StatusOnOff = OnOffStatus.On;
                                //     device.StatusTest = StatusDeviceTest.Ok;
                                //     t?.Add(true);
                                //     return (device, true);
                                // }
                                //
                                // if (cmdResult.Value != getParam.OutputOn)
                                // {
                                //     if (cmdResult.Value == null)
                                //     {
                                //         device.AllDeviceError.ErrorTimeout = true;
                                //         device.ErrorStatus = $"Ошибка реле Випа \"{device.IsDeviceType}\"/нет ответа";
                                //     }
                                //     else
                                //     {
                                //         device.AllDeviceError.ErrorParam = true;
                                //         device.ErrorStatus =
                                //             $"Ошибка уcтройства {device.IsDeviceType}, команда {device.NameCurrentCmd}/неверный параметр, пришел \"{cmdResult.Value}\"/ожидался \"{getParam.OutputOn}\"\n";
                                //     }
                                // }
                                //
                                // if (cmdResult.Value == "Stop tests")
                                // {
                                //     return (device, false);
                                // }
                            }
                        }

                        #endregion

                        #region вкл внешнее утсройство

                        else
                        {
                            //опрос выхода устройства
                            var cmdResult = await WriteIdentCommand(device, getOutputCmdName,
                                countChecked: innerCountCheck, t: tp);

                            //если выход выкл 
                            if (cmdResult.Value == getParam.OutputOff && tp.IsOk)
                            {
                                //делаем выход вкл
                                await WriteIdentCommand(device, SetOutputOnCmdName);

                                //опрос выхода утсртойства
                                cmdResult = await WriteIdentCommand(device, getOutputCmdName,
                                    countChecked: innerCountCheck,
                                    loopDelay: delay, t: tp);
                            }

                            //если выход вкл
                            if (cmdResult.Value == getParam.OutputOn && tp.IsOk)
                            {
                                //
                                SetPriorityStatusStand(3, $"выход устройства включен", device, percentSubTest: 100,
                                    colorSubTest: Brushes.BlueViolet, clearAll: true);
                                //

                                device.StatusOnOff = OnOffStatus.On;
                                device.StatusTest = StatusDeviceTest.Ok;
                                t?.Add(true);
                                return (device, true);
                            }


                            if (cmdResult.Value != getParam.OutputOn)
                            {
                                if (cmdResult.Value == null)
                                {
                                    device.AllDeviceError.ErrorTimeout = true;
                                    device.ErrorStatus = $"Ошибка уcтройства \"{device.IsDeviceType}\"/нет ответа";
                                }
                                else
                                {
                                    device.AllDeviceError.ErrorParam = true;
                                    device.ErrorStatus =
                                        $"Ошибка уcтройства {device.IsDeviceType}, команда {device.NameCurrentCmd}/неверный параметр, пришел \"{cmdResult.Value}\"/ожидался \"{getParam.OutputOn}\"\n";
                                }
                            }

                            if (cmdResult.Value == "Stop tests")
                            {
                                return (device, false);
                            }
                        }

                        #endregion

                        if (i != countChecked) continue;
                        //
                        SetPriorityStatusStand(3, $"выход устройства не включен, ошибка", device, percentSubTest: 100,
                            colorSubTest: Brushes.Red, clearAll: true);
                        //
                    }

                    #endregion
                }

                #region выкл

                if (!on)
                {
                    if (!device.Name.Contains("SL"))
                        if (device.AllDeviceError.CheckIsUnselectError(DeviceErrors.ErrorDevice) && i == 1)
                        {
                            if (forcedOff)
                            {
                                i = countChecked;
                            }
                            else
                            {
                                t?.Add(true);
                                return (device, true);
                            }
                        }

                    //
                    SetPriorityStatusStand(3, $"выключение выхода устройства", device, percentSubTest: 50,
                        currentCountCheckedSubTest: $"Попытка: {i.ToString()}/{countChecked}",
                        colorSubTest: Brushes.BlueViolet);
                    //

                    #region выкл реле випа

                    if (device is RelayVip r)
                    {
                        try
                        {
                            //ожидание цикла включения реле випа перед выключением
                            if (r.StatusOnOff == OnOffStatus.Switching)
                            {
                                await Task.Delay(TimeSpan.FromMilliseconds(5500), r.CtsRelayReceive.Token);
                                await Task.Delay(TimeSpan.FromMilliseconds(100));
                            }
                        }
                        catch (TaskCanceledException e) when (r.CtsRelayReceive.IsCancellationRequested)
                        {
                            r.CtsRelayReceive = new();
                        }

                        if (device.Name.Contains("SL"))
                        {
                            if (channel == 1)
                            {
                                var receiveOutput = await WriteIdentCommand(r, "Off1", countChecked: innerCountCheck,
                                    loopDelay: delay, t: tp);
                            }

                            if (channel == 2)
                            {
                                var receiveOutput = await WriteIdentCommand(r, "Off2", countChecked: innerCountCheck,
                                    loopDelay: delay, t: tp);
                            }

                            if (channel == 3)
                            {
                                var receiveOutput = await WriteIdentCommand(r, "Off3", countChecked: innerCountCheck,
                                    loopDelay: delay, t: tp);
                            }

                            if (channel == 4)
                            {
                                var receiveOutput = await WriteIdentCommand(r, "Off4", countChecked: innerCountCheck,
                                    loopDelay: delay, t: tp);
                            }

                            if (tp.IsOk)
                            {
                                //
                                SetPriorityStatusStand(3, $"#{channel} выход устройства выключен", device,
                                    percentSubTest: 100,
                                    colorSubTest: Brushes.BlueViolet, clearAll: true);
                                //
                                r.StatusOnOff = OnOffStatus.Off;
                                r.StatusTest = StatusDeviceTest.Ok;

                                t?.Add(true);
                                return (device, true);
                            }
                        }
                        else
                        {
                            s.Restart();
                            var receiveOutput = await WriteIdentCommand(r, SetOutputOffCmdName,
                                countChecked: innerCountCheck,
                                loopDelay: delay,
                                t: tp);

                            if (tp.IsOk)
                            {
                                //
                                SetPriorityStatusStand(3, $"#{channel} выход устройства выключен", device,
                                    percentSubTest: 100,
                                    colorSubTest: Brushes.BlueViolet, clearAll: true);
                                //
                                r.StatusOnOff = OnOffStatus.Off;
                                r.StatusTest = StatusDeviceTest.Ok;

                                t?.Add(true);
                                return (device, true);
                            }
                        }
                    }

                    #endregion

                    #region выкл внешнее утсройство

                    //если выключается не реле випа
                    else
                    {
                        //опрос выхода устройства
                        var cmdResult = await WriteIdentCommand(device, getOutputCmdName, countChecked: innerCountCheck,
                            t: tp);

                        //если выход вкл 
                        if (cmdResult.Value == getParam.OutputOn && tp.IsOk)
                        {
                            //делаем выход выкл
                            await WriteIdentCommand(device, SetOutputOffCmdName);

                            //опрос выхода устройства
                            cmdResult = await WriteIdentCommand(device, getOutputCmdName, countChecked: innerCountCheck,
                                loopDelay: delay, t: tp);
                        }

                        if (cmdResult.Value == getParam.OutputOff && tp.IsOk)
                        {
                            //
                            SetPriorityStatusStand(3, $"выход устройства выключен", device, percentSubTest: 100,
                                colorSubTest: Brushes.BlueViolet, clearAll: true);
                            //

                            device.StatusOnOff = OnOffStatus.Off;
                            device.StatusTest = StatusDeviceTest.Ok;
                            t?.Add(true);
                            return (device, true);
                        }

                        if (cmdResult.Value != getParam.OutputOn)
                        {
                            if (cmdResult.Value == null)
                            {
                                device.AllDeviceError.ErrorTimeout = true;
                                device.ErrorStatus = $"Ошибка уcтройства \"{device.IsDeviceType}\"/нет ответа";
                            }
                            else
                            {
                                device.AllDeviceError.ErrorParam = true;
                                device.ErrorStatus =
                                    $"Ошибка уcтройства {device.IsDeviceType}, команда {device.NameCurrentCmd}/неверный параметр, пришел \"{cmdResult.Value}\"/ожидался \"{getParam.OutputOn}\"\n";
                            }
                        }

                        if (cmdResult.Value == "Stop tests")
                        {
                            return (device, false);
                        }
                    }

                    //
                    SetPriorityStatusStand(3, $"устройство не выключено, ошибка", device, percentSubTest: 100,
                        colorSubTest: Brushes.Red, clearAll: true);
                    //

                    #endregion
                }

                #endregion
            }
        }

        device.StatusOnOff = OnOffStatus.None;
        device.StatusTest = StatusDeviceTest.Error;

        t?.Add(false);
        return (device, false);
    }

    #endregion

    #endregion

//--

    #endregion

//---

    #region Вспомогательные методы

//--

    #region Общие

    async Task<bool> ResetAllVips(int countChecked = 3, int loopDelay = 600, TempChecks t = null)
    {
        //сбросы всех статусов перед проверкой
        foreach (var relayVip in allRelayVips)
        {
            if (relayVip.Name.Contains("SL")) continue;

            if (t != null)
            {
                await WriteIdentCommand(relayVip, "Status", countChecked: countChecked, loopDelay: loopDelay, t: t);
                await OutputDevice(relayVip, t: t, forcedOff: true, on: false);
            }

            relayVip.AllDeviceError = new AllDeviceError();
            relayVip.StatusOnOff = OnOffStatus.None;
            relayVip.StatusTest = StatusDeviceTest.None;
            relayVip.ErrorStatus = null;
        }

        foreach (var vip in vips)
        {
            vip.ErrorVip = new RelayVipError();
            vip.StatusChannelVipTest = StatusChannelVipTest.None;
            vip.StatusSmallLoad = OnOffStatus.None;
            vip.StatusTest = StatusDeviceTest.None;
            vip.Channel1AddrNum = 6;
            vip.Channel2AddrNum = 14;
        }

        vipsTested?.Clear();

        if (t == null) return true;
        if (resetAll) return false;

        return t.IsOk;
        // ? true
        // : throw new Exception("Одно или несколько устройств  не ответили или ответили\nс ошибкой.\n");
    }

    public void SerializeDevice()
    {
        creatorAllDevicesAndLib.SerializeDevices(allDevices.ToList());
    }

    public void SerializeLib()
    {
        creatorAllDevicesAndLib.SerializeLib();
    }

    public void SerializeTypeVips()
    {
        creatorAllDevicesAndLib.SerializeTypeVip(cfgTypeVips.TypeVips.ToList());
    }

    // public void SerializeTime()
    // {
    //     creatorAllDevicesAndLib.SerializeTime(timeMachine);
    // }

    public void AddTypeVips(TypeVip typeConfig)
    {
        cfgTypeVips.AddTypeVips(typeConfig);
    }

    public void RemoveTypeVips(TypeVip selectTypeVipSettings)
    {
        cfgTypeVips.RemoveTypeVips(selectTypeVipSettings);
    }

    #endregion

//--

    #region Проверка и работа с парамтерами

    /// <summary>
    /// Получение параметров приборов из типа Випа
    /// </summary>
    /// <returns>DeviceParameters</returns>
    DeviceParameters GetParameterForDevice()
    {
        return vips[0].Type.GetDeviceParameters();
    }

    /// <summary>
    /// Установка параметров приборов в тип Випа
    /// </summary>
    /// <param name="baseDevice"></param>
    /// <param name="mode"></param>
    /// <param name="param"></param>
    /// <param name="bdv">Вид прибора в ктороый будут установлены значения</param>
    void SetParameterGdmDevice(BaseDevice device, ModeGdm mode = ModeGdm.None, string param = null)
    {
        if (device is VoltCurrentMeter)
        {
            if (mode == ModeGdm.Current)
            {
                vips[0].Type.Parameters.VoltCurrentValues.Mode = mode;
                vips[0].Type.Parameters.VoltCurrentValues.CurrMaxLimit = param;
                vips[0].Type.Parameters.VoltCurrentValues.SetFuncGDM();
            }
            else if (mode == ModeGdm.Voltage)
            {
                vips[0].Type.Parameters.VoltCurrentValues.Mode = mode;
                vips[0].Type.Parameters.VoltCurrentValues.VoltMaxLimit = param;
                vips[0].Type.Parameters.VoltCurrentValues.SetFuncGDM();
            }
        }

        else
        {
            if (mode == ModeGdm.Current)
            {
                vips[0].Type.Parameters.VoltCurrentValues.Mode = mode;
                vips[0].Type.Parameters.VoltCurrentValues.CurrMaxLimit = param;
                vips[0].Type.Parameters.VoltCurrentValues.SetFuncGDM();
            }
            else if (mode == ModeGdm.Voltage)
            {
                vips[0].Type.Parameters.VoltValues.Mode = mode;
                vips[0].Type.Parameters.VoltValues.VoltMaxLimit = param;
                vips[0].Type.Parameters.VoltCurrentValues.SetFuncGDM();
            }
            else if (mode == ModeGdm.Themperature)
            {
                vips[0].Type.Parameters.VoltCurrentValues.Mode = mode;
                vips[0].Type.Parameters.VoltCurrentValues.CurrMaxLimit = param;
                vips[0].Type.Parameters.VoltCurrentValues.SetFuncGDM();
            }
        }
    }

    /// <summary>
    /// Преобразовние строк вида "SQU +2.00000000E+02,+4.000E+00,+2.00E+00" в стандартные строки вида 200, 4, 20
    /// </summary>
    /// <param name="str">Строка которая будет преобразована</param>
    /// <returns></returns>
    public string CastToNormalValues(string str)
    {
        if (str != null)
        {
            decimal myDecimalValue = 0;
            int myIntlValue = 0;

            if (str.Contains("E+") || str.Contains("e+") || str.Contains("e") || str.Contains("E-") ||
                str.Contains("e-") || str[0] == '+' ||
                str[0] == '-')
            {
                if (decimal.TryParse(str, out myDecimalValue))
                {
                    decimal x1 = Math.Floor(myDecimalValue);
                    decimal x2 = myDecimalValue - Math.Floor(x1);

                    // if (x2 == 0)
                    // {
                    //     return (T)Convert.ChangeType(x1, typeof(T));
                    // }
                    if (x2 == 0)
                    {
                        return x1.ToString(CultureInfo.InvariantCulture);
                    }
                }
                else
                {
                    myDecimalValue = Decimal.Parse(str, NumberStyles.Float);
                    decimal x1 = Math.Floor(myDecimalValue);
                    decimal x2 = myDecimalValue - Math.Floor(x1);
                    if (x2 == 0)
                    {
                        return x1.ToString(CultureInfo.InvariantCulture);
                    }
                }


                return myDecimalValue.ToString(CultureInfo.InvariantCulture);
            }
        }

        return str;
    }

    #endregion

    #endregion

//--
}

internal enum Threshold
{
    Normal,
    High,
    Low
}

internal enum TypeCheckVal
{
    None = 0,
    Current,
    Voltage1,
    Voltage2,
    PrepareVoltage1,
    PrepareVoltage2,
    PrepareCurrent,
    AvailabilityCurrent,
    Temperature
}