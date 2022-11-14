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
using System.Windows.Media;
using System.Windows.Threading;

namespace StandETT;

public class Stand1 : Notify
{
    #region Вспомогательное/Отладка

    Stopwatch s = new();

    #endregion


    #region Создание устройств

    /// <summary>
    /// Создание устройств стенда
    /// </summary>
    private CreatorAllDevicesAndLib creatorAllDevicesAndLib;

    //public ConfigVips ConfigVip { get; set; } = new ConfigVips();

    #endregion

    //--

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

    //--

    public TimeMachine timeMachine = TimeMachine.getInstance();

    //--
    MainRelay mainRelay = MainRelay.GetInstance();
    ConfigTypeVip cfgTypeVips = ConfigTypeVip.getInstance();

    private readonly ObservableCollection<Vip> vips = new();
    public readonly ReadOnlyObservableCollection<Vip> Vips;

    private ObservableCollection<RelayVip> allRelayVips = new();
    //--

    #endregion

    //---

    #region --Статусы стенда

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

    public event Action<string> timerError;

    #endregion

    //---

    #region Токены

    CancellationTokenSource ctsAllCancel = new();
    CancellationTokenSource ctsReceiveDevice = new();
    CancellationTokenSource ctsConnectDevice = new();

    #endregion

    //---

    #region --Конструктор --ctor

    private System.Timers.Timer timer;

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

        timeMachine = creatorAllDevicesAndLib.SetTime();
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
    public bool IsResetAll = false;

    bool stopMeasurement = false;

    //

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

        ctsAllCancel.Cancel();
        ctsConnectDevice.Cancel();
        ctsReceiveDevice.Cancel();

        TestRun = TypeOfTestRun.Stoped;

        //OpenActionWindow?.Invoke(true);

        PercentCurrentTest = 30;
        ProgressColor = Brushes.Green;

        await Task.Delay(TimeSpan.FromMilliseconds(200));

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
                ErrorMessage = $"Отключение выхода #{1} устройства {device.IsDeviceType}/{device.Name}...";
                resultOutput = await OutputDevice(device, 1, 1, t: t, on: false);
                if (!resultOutput.outputResult)
                {
                    ErrorOutput += $"выход #{1}/{device.ErrorStatus}\n";
                    ProgressResetColor = Brushes.Red;
                }

                ErrorMessage = $"Отключение выхода #{2} устройства {device.IsDeviceType}/{device.Name}...";
                resultOutput = await OutputDevice(device, 2, t: t, on: false);
                if (!resultOutput.outputResult)
                {
                    ErrorOutput += $"выход #{2}/{device.ErrorStatus}\n";
                    ProgressResetColor = Brushes.Red;
                }

                ErrorMessage = $"Отключение выхода #{3} устройства {device.IsDeviceType}/{device.Name}...";
                resultOutput = await OutputDevice(device, 3, t: t, on: false);
                TempChecks t4 = TempChecks.Start();
                if (!resultOutput.outputResult)
                {
                    ErrorOutput += $"выход #{3}/{device.ErrorStatus}\n";
                    ProgressResetColor = Brushes.Red;
                }

                ErrorMessage = $"Отключение выхода #{4} устройства {device.IsDeviceType}/{device.Name}...";
                resultOutput = await OutputDevice(device, 4, t: t, on: false);
                if (!resultOutput.outputResult)
                {
                    ErrorOutput += $"выход #{4}/{device.ErrorStatus}\n";
                    ProgressResetColor = Brushes.Red;
                }
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

                // relay.StatusTest = StatusDeviceTest.None;
                //(BaseDevice outputDevice, bool outputResult) resultOutput = (relay, false);

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

        if (t.IsOk)
        {
            PercentCurrentTest = 100;
            ErrorMessage = "Все выходы доступных/инициаизировных устройств были благополучно отключены";
        }
        else
        {
            PercentCurrentTest = 100;
            ProgressColor = Brushes.Red;

            ErrorMessage =
                "Осторожно! Следующие выходы устройств не были отключены, тк к устройствам нет доступа " +
                "или они неправильно индентифицированы";
            ErrorOutput += "Выходы остальных устройств отключены\n";
        }

        SetPriorityStatusStand(0, colorSubTest: Brushes.Red, clearAll: true);

        stopMeasurement = false;

        //
        CaptionAction = "Стенд остановлен";
        TestRun = TypeOfTestRun.Stop;
        PercentCurrentReset = 100;
        //

        IsResetAll = false;
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
    public async Task<bool> PrimaryCheckDevices(int countChecked = 3, int loopDelay = 3000)
    {
        //
        TestRun = TypeOfTestRun.PrimaryCheckDevices;
        ProgressColor = Brushes.Green;
        PercentCurrentTest = 30;
        SetPriorityStatusStand(0, clearAll: true);
        //

        var t = TempChecks.Start();


        //TODO убрать как появтся остальные
        currentDevice = devices.GetTypeDevice<Thermometer>();
        devices.Remove(currentDevice);
        currentDevice = devices.GetTypeDevice<Heat>();
        devices.Remove(currentDevice);
        //TODO убрать как появтся остальные


        await CheckConnectPorts(devices.ToList(), t: t);

        if (t.IsOk)
        {
            PercentCurrentTest = 70;
            t = TempChecks.Start();
            await WriteIdentCommands(devices.ToList(), "Status", countChecked: countChecked, t: t,
                loopDelay: loopDelay);

            if (t.IsOk)
            {
                //отключение инциализированых устройств если они вдруг оказались включены
                // currentDevice = devices.GetTypeDevice<Supply>();
                // await OutputDevice(currentDevice, t: t, forcedOff: true, on: false);
                // currentDevice = devices.GetTypeDevice<BigLoad>();
                // await OutputDevice(currentDevice, t: t, forcedOff: true, on: false);
                if (t.IsOk)
                {
                    //
                    SetPriorityStatusStand(1, $"запись команд в устройства, oк!", percentSubTest: 100,
                        colorSubTest: Brushes.Orange, clearAll: true);
                    TestRun = TypeOfTestRun.PrimaryCheckDevicesReady;
                    PercentCurrentTest = 100;
                    //
                    return true;
                }
            }
        }

        if (resetAll) return false;
        //
        PercentCurrentTest = 100;
        ProgressColor = Brushes.Red;
        TestRun = TypeOfTestRun.Error;
        SetPriorityStatusStand(1, $"запись команд в устройства, ошибка!", percentSubTest: 100,
            colorSubTest: Brushes.Red, clearAll: true);
        //
        throw new Exception("Одно или несколько устройств не ответили или ответили\nс ошибкой.\n");
    }

    private ObservableCollection<Vip> vipsTested = new();

    private ObservableCollection<Vip> vipsStopped = new();

    private ObservableCollection<RelayVip> relayVipsTested = new();

    //--випы--vips--relay--check--проверка--
    public async Task<bool> PrimaryCheckVips(int countChecked = 3, int loopDelay = 3000)
    {
        //
        TestRun = TypeOfTestRun.PrimaryCheckVips;
        ProgressColor = Brushes.Green;
        PercentCurrentTest = 30;
        SetPriorityStatusStand(0, clearAll: true);
        //

        TempChecks t = TempChecks.Start();

        //сбросы всех статусов перед проверкой
        foreach (var relayVip in allRelayVips)
        {
            if (relayVip.Name.Contains("SL")) continue;
            relayVip.StatusOnOff = OnOffStatus.None;
            relayVip.StatusTest = StatusDeviceTest.None;
            relayVip.ErrorStatus = null;
        }


        foreach (var vip in vips)
        {
            vip.StatusChannelVipTest = StatusChannelVipTest.None;
            vip.StatusSmallLoad = OnOffStatus.None;
            vip.ErrorVip = new RelayVipError();
            vip.StatusTest = StatusDeviceTest.None;
        }

        //предварительная настройка тестрировать ли вип => если у Випа есть имя то тестировать
        vipsTested.Clear();
        vipsTested = GetIsTestedVips();
        if (!vipsTested.Any())
        {
            TestRun = TypeOfTestRun.Stop;
            PercentCurrentTest = 0;
            throw new Exception("Отсутвуют инициализировнные Випы!");
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
                await WriteIdentCommand(relay, "Status", countChecked: countChecked, loopDelay: loopDelay, t: t);
                if (t.IsOk)
                {
                    await OutputDevice(relay, t: t, forcedOff: true, on: false);
                }
            }

            if (t.IsOk)
            {
                //
                SetPriorityStatusStand(1, $"запись команд в Випы, oк!", percentSubTest: 100,
                    colorSubTest: Brushes.Orange, clearAll: true);

                TestRun = TypeOfTestRun.PrimaryCheckDevicesReady;
                PercentCurrentTest = 100;
                TestRun = TypeOfTestRun.PrimaryCheckVipsReady;
                PercentCurrentTest = 100;
                //
                return true;
            }
        }

        if (!resetAll)
        {
            //
            SetPriorityStatusStand(1, $"запись команд в Випы, ошибка!", percentSubTest: 100,
                colorSubTest: Brushes.Red, clearAll: true);
            PercentCurrentTest = 100;
            ProgressColor = Brushes.Red;
            TestRun = TypeOfTestRun.Error;
            //
            throw new Exception("Одно или несколько устройств  не ответили или ответили\nс ошибкой.\n");
        }

        return false;
    }

    //--предварительные--наличие--AvailabilityCheckVip
    public async Task<bool> AvailabilityCheckVip(int countChecked = 3, int loopDelay = 1000)
    {
        if (!vipsTested.Any())
        {
            throw new Exception("Отсутвуют инициализировнные Випы!");
        }

        //
        TestRun = TypeOfTestRun.AvailabilityCheckVip;
        ProgressColor = Brushes.Green;
        PercentCurrentTest = 10;
        SetPriorityStatusStand(0, clearAll: true);
        //
        TempChecks tp = TempChecks.Start();
        currentDevice = devices.GetTypeDevice<Supply>();
        var getSupplyValues = GetParameterForDevice().SupplyValues;
        await OutputDevice(currentDevice, t: tp, on: false);

        //бп
        if (tp.IsOk)
            await SetCheckValueInDevice(currentDevice, "Set volt", getSupplyValues.VoltageAvailability,
                countChecked, loopDelay, tp, "Get volt");
        if (tp.IsOk)
            await SetCheckValueInDevice(currentDevice, "Set curr", getSupplyValues.CurrentAvailability,
                countChecked, loopDelay, tp, "Get curr");
        await OutputDevice(currentDevice, t: tp);

        foreach (var vipTested in vipsTested)
        {
            if (tp.IsOk) await TestVip(vipTested, TypeOfTestRun.AvailabilityCheckVip, tp);
        }

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

        if (tp.IsOk)
        {
            //
            SetPriorityStatusStand(1, $"Проверка проверки наличия (ЧТА?),Ок!", percentSubTest: 100,
                colorSubTest: Brushes.Violet, clearAll: true);
            TestRun = TypeOfTestRun.AvailabilityCheckVip;
            ProgressColor = Brushes.Green;
            PercentCurrentTest = 10;
            SetPriorityStatusStand(0, clearAll: true);
            //
            return true;
        }

        if (!resetAll && !stopMeasurement)
        {
            //
            SetPriorityStatusStand(1, $"Проверка наличия Випов, ошибка!", percentSubTest: 100,
                colorSubTest: Brushes.Red, clearAll: true);
            TestRun = TypeOfTestRun.Error;
            PercentCurrentTest = 100;
            ProgressColor = Brushes.Red;
            //
            throw new Exception("Одно или несколько устройств не ответили  или ответили\nс ошибкой.\n");
        }

        SetPriorityStatusStand(1, percentSubTest: 100, colorSubTest: Brushes.Red, clearAll: true);
        TestRun = TypeOfTestRun.Stop;
        return false;
    }

    //--zero--ноль--
    public async Task<bool> MeasurementZero(int countChecked = 3, int loopDelay = 1000)
    {
        //
        TestRun = TypeOfTestRun.MeasurementZero;
        ProgressColor = Brushes.Green;
        PercentCurrentTest = 10;
        SetPriorityStatusStand(0, clearAll: true);
        //

        bool isErrorRelayVip = false;
        TempChecks t = TempChecks.Start();

        // установки настройек приобров


        //
        PercentCurrentTest = 20;
        //

        //большой нагрузки
        if (t.IsOk)
        {
            currentDevice = devices.GetTypeDevice<BigLoad>();
            var getBigLoadValues = GetParameterForDevice().BigLoadValues;

            //TODO спросить нужно ли оставлять
            await OutputDevice(currentDevice, t: t, on: false);

            if (t.IsOk)
                await SetCheckValueInDevice(currentDevice, "Set freq", getBigLoadValues.Freq,
                    countChecked, loopDelay, t, "Get freq");
            if (t.IsOk)
                await SetCheckValueInDevice(currentDevice, "Set ampl", getBigLoadValues.Ampl,
                    countChecked, loopDelay, t, "Get ampl");
            if (t.IsOk)
                await SetCheckValueInDevice(currentDevice, "Set dco", getBigLoadValues.Dco,
                    countChecked, loopDelay, t, "Get dco");
            if (t.IsOk)
                await SetCheckValueInDevice(currentDevice, "Set squ", getBigLoadValues.Squ,
                    countChecked, loopDelay, t, "Get squ");
            if (t.IsOk)
                await OutputDevice(currentDevice, t: t);
        }

        // //TODO уточнить где должно быть малая нагрузка
        // //малой нагрузки
        // if (t.IsOk)
        // {
        //     currentDevice = devices.GetTypeDevice<SmallLoad>();
        //     await OutputDevice(currentDevice, t: t, on: false);
        // }

        //
        PercentCurrentTest = 40;
        //

        //бп
        if (t.IsOk)
        {
            currentDevice = devices.GetTypeDevice<Supply>();
            var getSupplyValues = GetParameterForDevice().SupplyValues;

            //TODO спросить нужно ли оставлять
            await OutputDevice(currentDevice, t: t, on: false);

            if (t.IsOk)
                await SetCheckValueInDevice(currentDevice, "Set volt", getSupplyValues.Voltage,
                    countChecked, loopDelay, t, "Get volt");
            if (t.IsOk)
                await SetCheckValueInDevice(currentDevice, "Set curr", getSupplyValues.Current,
                    countChecked, loopDelay, t, "Get curr");
            if (t.IsOk)
                await OutputDevice(currentDevice, t: t);
        }

        //TODO спросить нужно ли оставлять 
        // if (t.IsOk) 
        //     await SetCheckValueInDevice(currentDevice, null, getSupplyValues.Voltage,
        //         countChecked, loopDelay, t, true, "Get real volt");
        // if (t.IsOk)
        //     await SetCheckValueInDevice(currentDevice, null, getSupplyValues.Current,
        //         countChecked, loopDelay, t, true, "Get real curr");

        //
        PercentCurrentTest = 80;
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

            //  await report.CreateHeadersReport(new HeaderReport(1, vipsTested[0].Type));

            try
            {
                foreach (var vipTested in vipsTested)
                {
                    if (t.IsOk) await TestVip(vipTested, TypeOfTestRun.MeasurementZero, t);
                }

                if (vipsStopped.Any())
                {
                    //
                    SetPriorityStatusStand(1, $"Тест Випов, ошибка!", percentSubTest: 100,
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
            }
            catch (Exception e)
            {
                TestRun = TypeOfTestRun.Error;
                PercentCurrentTest = 100;
                ProgressColor = Brushes.Red;
                throw new Exception(e.Message);
            }
        }

        //TODO венуть
        if (t.IsOk)
        {
            TestRun = TypeOfTestRun.MeasurementZeroReady;
            PercentCurrentTest = 100;
            return true;
        }

        if (!resetAll && !stopMeasurement)
        {
            TestRun = TypeOfTestRun.Error;
            PercentCurrentTest = 100;
            ProgressColor = Brushes.Red;
            throw new Exception("Одно или несколько устройств не ответили или ответили\nс ошибкой.\\nn");
        }

        SetPriorityStatusStand(1, percentSubTest: 100,
            colorSubTest: Brushes.Red, clearAll: true);
        TestRun = TypeOfTestRun.Stop;
        return false;
    }


    //--prepare--pretest--prestart--heat
    public async Task<bool> PrepareMeasurementCycle(int countChecked = 3, int loopDelay = 1000, int timeHeatSec = 120)
    {
        //
        TestRun = TypeOfTestRun.WaitSupplyMeasurementZero;
        PercentCurrentTest = 50;
        ProgressColor = Brushes.Green;
        //
        TempChecks t = TempChecks.Start();

        //TODO вернуть
        // currentDevice = devices.GetTypeDevice<SmallLoad>();
        // if (t.IsOk) await WriteIdentCommand(currentDevice, "On 1", t: t);
        //TODO вернуть
        // currentDevice = devices.GetTypeDevice<Heat>();
        bool isHeatEnable = t.IsOk;
        //TODO вернуть
        // await OutputDevice(currentDevice, t: t);

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
                        //TODO вернуть
                        //if (t.IsOk) await OutputDevice(devices.GetTypeDevice<Heat>(), t: t, on: false);
                    }
                    else if (!isHeatEnable)
                    {
                        isHeatEnable = true;
                        //TODO вернуть
                        //if (t.IsOk) await OutputDevice(devices.GetTypeDevice<Heat>(), t: t);
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
            TestRun = TypeOfTestRun.WaitSupplyMeasurementZeroReady;
            PercentCurrentTest = 100;
            return true;
        }

        if (!resetAll)
        {
            await ResetAllTests();

            TestRun = TypeOfTestRun.Error;
            PercentCurrentTest = 100;
            ProgressColor = Brushes.Red;
            TestCurrentDevice = null;
            if (!string.IsNullOrEmpty(error))
            {
                throw new Exception(error + "\n");
            }

            throw new Exception("Одно или несколько устройств не ответили или ответили\nс ошибкой.\\nn");
        }

        return false;
    }

    private IntervalChecker oneIntervalCheckerCycle;
    private IntervalChecker intervalCheckerCycle;
    private IntervalChecker intervalCheckerStop;

    //--mesaure--cycle--cucle--start--loop
    public bool StartMeasurementCycle(int countChecked = 3,
        int loopDelay = 1000)
    {
        //
        TestRun = TypeOfTestRun.CyclicMeasurement;
        ProgressColor = Brushes.Green;
        PercentCurrentTest = 10;
        SetPriorityStatusStand(0, clearAll: true);
        //

        try
        {
            a = 0;
            b = 1;
            c = 1;

            if (timer != null)
            {
                timer.Stop();
                timer.Elapsed -= TimerOnElapsed;
            }


            timer = new System.Timers.Timer(1);
            try
            {
                var testIntervalTime = (float)vipsTested[0].Type.TestIntervalTime.TotalSeconds;
                var testAllTime = (float)vipsTested[0].Type.TestAllTime.TotalSeconds;
                
                
                intervalCheckerCycle = new(testIntervalTime);
                intervalCheckerCycle = new(testIntervalTime);
                intervalCheckerStop = new(testAllTime);
            }
            catch (Exception e)
            {
                throw new Exception("Отсутвуют инициализировнные Випы!");
            }
            
            timer.Elapsed += TimerOnElapsed;
            timer.Enabled = true;
            timer.Start();
        }
        
        catch (Exception e)
        {
            throw new Exception();
        }

        return false;
    }


    int a = 0;
    int b = 0;
    int c = 0;

    //private ReportCreator report = new ReportCreator();

    // --timer
    private async void TimerOnElapsed(object sender, ElapsedEventArgs e)
    {
        bool isMeasurement = false;

        TempChecks tp = TempChecks.Start();

        if (timer.Interval < 60000)
        {
            timer.Interval = 60000;
        }

        //
        SetPriorityStatusStand(2, $"Тик замера {a++}", clearAll: true);
        //

        // Debug.WriteLine($"Тик замера {a++}/{s.ElapsedMilliseconds} mc");


        if (intervalCheckerCycle.Check())
        {
            isMeasurement = true;

            //
            // Debug.WriteLine(
            //     $"Цикл замера начало {b}/{s.ElapsedMilliseconds} mc");

            foreach (var vip in vipsTested)
            {
                var dataMeasurement = await MeasurementTickAss(vip, t: tp);
                if (!tp.IsOk)
                {
                    intervalCheckerCycle.Reset();
                    intervalCheckerStop.Reset();
                    timer.Elapsed -= TimerOnElapsed;
                    timer.Stop();

                    timerError?.Invoke($"Вип - {vip.Name} не ответил или ответил с ошибкой.\n");
                    return;
                }
            }

            // Debug.WriteLine(
            //     $"Цикл замера конец {b}/{s.ElapsedMilliseconds} mc");

            b++;
        }

        if (intervalCheckerStop.Check())
        {
            //
            SetPriorityStatusStand(1, $"Последний тик замера", clearAll: true);
            //

            // Debug.WriteLine(
            //     $"Стоп замера {c++}/{s.ElapsedMilliseconds} mc");

            if (isMeasurement)
            {
                // Debug.WriteLine(
                //     $"Цикл замера стоп начало {c}/{s.ElapsedMilliseconds} mc");

                foreach (var vip in vipsTested)
                {
                    var dataMeasurement = await MeasurementTickAss(vip, t: tp);
                    if (!tp.IsOk)
                    {
                        intervalCheckerCycle.Reset();
                        intervalCheckerStop.Reset();
                        timer.Elapsed -= TimerOnElapsed;
                        timer.Stop();

                        timerError?.Invoke($"Вип - {vip.Name} не ответил или ответил с ошибкой.\n");
                        return;
                    }
                }

                //
                SetPriorityStatusStand(1, $"Циклические замеры заверщены успешно!", clearAll: true);
                //
                // Debug.WriteLine(
                //     $"Цикл замера стоп конец {c}/{s.ElapsedMilliseconds} mc");
            }

            intervalCheckerCycle.Reset();
            intervalCheckerStop.Reset();
            timer.Elapsed -= TimerOnElapsed;
            timer.Stop();

            await ResetAllTests();
        }
    }

    //--tick--test--ass
    async Task<(bool result, Vip vip)> MeasurementTickAss(Vip vip, int countChecked = 3, int loopDelay = 1000,
        TempChecks t = null)
    {
        //
        SetPriorityStatusStand(1, $"Цикл замера номер{b}", vip.Relay, percentSubTest: 0,
            currentCountCheckedSubTest: $"Попытка: {0}/{countChecked}", colorSubTest: Brushes.White);
        //
        // await Task.Delay(TimeSpan.FromMilliseconds(3000));
        TempChecks tp = TempChecks.Start();

        await SetTestChannelLoad(vip, 1, allDevices.ToList(), tp);

        return (false, new Vip(0, new RelayVip(0, null)));
    }

    private async Task<bool> SetTestChannelLoad(Vip vip, int i, List<BaseDevice> r, TempChecks tempChecks)
    {
        //
        SetPriorityStatusStand(2, $"переключение 1 канала измерений Вип - {vip.Name}", percentSubTest: 40,
            colorSubTest: Brushes.DodgerBlue, clearAll: true);
        //

        // if (device.Name.Contains("SL"))
        // {
        //     ErrorMessage = $"Отключение выхода #{1} устройства {device.IsDeviceType}/{device.Name}...";
        //     resultOutput = await OutputDevice(device, 1, 1, t: t, on: false);
        //     if (!resultOutput.outputResult)
        //     {
        //         ErrorOutput += $"выход #{1}/{device.ErrorStatus}\n";
        //         ProgressResetColor = Brushes.Red;
        //     }

        //var ss = r.FirstOrDefault(x => x.Prefix == 0);

        //currentDevice = devices.GetTypeDevice<SmallLoad>();

        TempChecks tp = TempChecks.Start();

        var ss = allDevices.FirstOrDefault(x => x.Name.Contains("SL") && x.Prefix == "4");
        //TODO вернуть!
        //var ss = allDevices.FirstOrDefault(x => x.Name.Contains("SL") && x.Prefix == vip.Id.ToString());
        if (ss != null)
        {
            vip.StatusSmallLoad = ss.StatusOnOff;
        }

        if (vip.Id == 0)
        {
            await OutputDevice(ss, 0, t: tp, on: true);
        }

        if (vip.Id == 1)
        {
            await OutputDevice(ss, 1, t: tp, on: true);
        }

        if (vip.Id == 2)
        {
            await OutputDevice(ss, 2, t: tp, on: true);
        }

        if (vip.Id == 3)
        {
            await OutputDevice(ss, 3, t: tp, on: true);
        }
        // if (vip.Id == 4 || vip.Id == 5 || vip.Id == 6 || vip.Id == 7)
        // {
        //     
        // }
        // if (vip.Id == 8 || vip.Id == 9 || vip.Id == 10 || vip.Id == 11)
        // {
        //     
        // }
        if (tp.IsOk)
        {
            SetPriorityStatusStand(2, $"Включение {vip.Id} канала малой нагрузки {ss?.Prefix} ,Ок!",
                percentSubTest: 100,
                colorSubTest: Brushes.DodgerBlue, clearAll: true);

            ss.StatusOnOff = OnOffStatus.On;

            tp?.Add(true);
            return true;
        }
        
        SetPriorityStatusStand(2, $"Включение {vip.Id} канала малой нагрузки {ss?.Prefix}, ошибка!",
            percentSubTest: 100,
            colorSubTest: Brushes.Red, clearAll: true);

        ss.StatusOnOff = OnOffStatus.Off;
        tp?.Add(false);
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
    async Task<bool> CheckConnectPort(BaseDevice device, int countChecked = 5, int loopDelay = 200, TempChecks t = null)
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
    async Task<bool> CheckConnectPorts(List<BaseDevice> devices, int countChecked = 5, int loopDelay = 200,
        TempChecks t = null)
    {
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
    /// <param name="device"></param>
    /// <param name="nameCmd"></param>
    /// <param name="paramSet"></param>
    /// <param name="paramGet"></param>
    /// <param name="countChecked"></param>
    /// <param name="loopDelay"></param>
    /// <param name="t"></param>
    /// <param name="token"></param>
    /// <param name="isReceiveVal"></param>
    /// <param name="toList"></param>
    /// <param name="tempChecks"></param>
    /// <param name="isLast"></param>
    private async Task<KeyValuePair<BaseDevice, string>> WriteIdentCommand(BaseDevice device,
        string nameCmd, string paramSet = null, string paramGet = null, int countChecked = 3, int loopDelay = 200,
        TempChecks t = null, CancellationToken token = default, bool isReceiveVal = false)
    {
        KeyValuePair<BaseDevice, string> deviceReceived = default;

        if (resetAll)
        {
            t?.Add(false);
            return deviceReceived;
        }

        CurrentWriteDevices = new() { device };
        //удаление прердыдущих ответов от устройств
        RemoveReceive(device);

        KeyValuePair<BaseDevice, bool> verifiedDevice;

        //если прибор от кторого приходят данные не содержится в библиотеке а при первом приеме данных так и будет
        if (!receiveInDevice.ContainsKey(device))
        {
            //добавляем туда данные и создаем список в ктороые будет записаны ответы от прибора
            receiveInDevice.Add(device, new List<string>());
        }

        try
        {
            for (int i = 1; i <= countChecked; i++)
            {
                //
                SetPriorityStatusStand(4, $"запись команды в устройство", device, percentSubTest: 0,
                    currentCountCheckedSubTest: $"Попытка: {i.ToString()}/{countChecked}",
                    colorSubTest: Brushes.Orange);
                //

                device.ErrorStatus = null;
                device.StatusTest = StatusDeviceTest.None;

                try
                {
                    if (i > 1)
                    {
                        ctsReceiveDevice = new CancellationTokenSource();
                        await Task.Delay(TimeSpan.FromMilliseconds(loopDelay), ctsReceiveDevice.Token);
                    }

                    if (!resetAll)
                    {
                        if (!device.PortIsOpen)
                        {
                            SetPriorityStatusStand(4, $"порт закрыт, переподключение...", clearAll: true);

                            if (!await CheckConnectPort(device, 3))
                            {
                                //
                                SetPriorityStatusStand(4, $"порт не открыт, ошибка!", device,
                                    colorSubTest: Brushes.Red);
                                //
                                t?.Add(false);
                                return deviceReceived;
                            }
                        }

                        await Task.Delay(TimeSpan.FromMilliseconds(100));

                        SetPriorityStatusStand(4, $"переподключение удачно, порт открыт");
                    }

                    //установка гет парамтра
                    device.CurrentParameterGet = paramGet;
                    device.IsReceive = isReceiveVal;

                    //запись команды в каждое устройсттво
                    device.WriteCmd(nameCmd, paramSet);

                    //
                    SetPriorityStatusStand(4, $"запись команды в устройство", device, percentSubTest: 50,
                        currentCountCheckedSubTest: $"Попытка: {i.ToString()}/{countChecked}",
                        colorSubTest: Brushes.Orange);
                    //


                    if (!string.IsNullOrEmpty(paramSet))
                    {
                        SetGdmReceive(device, paramSet);
                    }

                    if (!string.IsNullOrEmpty(paramGet) && string.IsNullOrEmpty(paramSet))
                    {
                        device.CurrentParameter = paramGet;
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(device.CurrentCmd?.Delay ?? 200),
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

                    await Task.Delay(TimeSpan.FromMilliseconds(100), ctsAllCancel.Token);

                    //если тест остановлены, немделнно выходим из метода с ошибкой
                    if (resetAll)
                    {
                        deviceReceived = GetReceiveLast(device);
                        t?.Add(false);
                        if (deviceReceived.Key == null)
                        {
                            return new KeyValuePair<BaseDevice, string>(device, "Stop tests");
                        }

                        return deviceReceived;
                    }
                }

                // Проверка устройств
                verifiedDevice = GetVerifiedDevice(device);

                if (verifiedDevice.Value)
                {
                    verifiedDevice.Key.StatusTest = StatusDeviceTest.Ok;

                    //
                    SetPriorityStatusStand(4, $"запись команды в устройство, ок!", device, percentSubTest: 100,
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
            SetPriorityStatusStand(4, $"запись команды в устройство, ошибка!", device, percentSubTest: 100,
                colorSubTest: Brushes.Red);
            //

            t?.Add(false);
            deviceReceived = GetReceiveLast(device);
            return deviceReceived;
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
            SetPriorityStatusStand(4, $"запись команды в устройство, ошибка!", device, percentSubTest: 100,
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
    /// <param name="devices"></param>
    /// <param name="cmd"></param>
    /// <param name="paramSet"></param>
    /// <param name="paramGet"></param>
    /// <param name="countChecked"></param>
    /// <param name="t"></param>
    /// <param name="loopDelay"></param>
    /// <param name="token"></param>
    /// <param name="isReceiveVal"></param>
    /// <param name="toList"></param>
    /// <param name="tempChecks"></param>
    private async Task<Dictionary<BaseDevice, List<string>>> WriteIdentCommands(List<BaseDevice> devices,
        string cmd = null, string paramSet = null, string paramGet = null, int countChecked = 3, TempChecks t = null,
        int loopDelay = 200, CancellationToken token = default, bool isReceiveVal = false)
    {
        var deviceReceived = new Dictionary<BaseDevice, List<string>>();

        if (resetAll)
        {
            t?.Add(false);
            return deviceReceived;
        }

        CurrentWriteDevices = devices;

        var verifiedDevices = new Dictionary<BaseDevice, bool>();
        var tempDevices = devices;
        var checkDevices = new List<KeyValuePair<BaseDevice, bool>>();

        var isRelay = false;
        //список задержек
        var delays = new List<int>(tempDevices.Count);

        //удаление прердыдущих ответов от устройств
        RemoveReceives(devices);

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

                        if (device is RelayVip)
                        {
                            isRelay = true;
                            await Task.Delay(TimeSpan.FromMilliseconds(delays.Max()), ctsReceiveDevice.Token);
                        }

                        if (!string.IsNullOrEmpty(paramSet))
                        {
                            SetGdmReceive(device, paramSet);
                        }

                        if (!string.IsNullOrEmpty(paramGet) && string.IsNullOrEmpty(paramSet))
                        {
                            device.CurrentParameter = paramGet;
                        }
                    }

                    if (!isRelay)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(delays.Max()), ctsReceiveDevice.Token);
                    }

                    if (t == null) return new Dictionary<BaseDevice, List<string>>();
                }

                catch (TaskCanceledException e) when (ctsReceiveDevice.IsCancellationRequested)
                {
                    ctsReceiveDevice = new CancellationTokenSource();
                    await Task.Delay(TimeSpan.FromMilliseconds(100), ctsAllCancel.Token);

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

                    devices.ForEach(x => x.StatusTest = StatusDeviceTest.Ok);

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
        string setCmd = null, string param = null, int countChecked = 3, int loopDelay = 200, TempChecks t = null,
        params string[] getCmds)
    {
        var deviceReceived = new Dictionary<BaseDevice, List<string>>();
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

            //
            SetPriorityStatusStand(3, percentSubTest: 70);
            //

            tp.Add(true);

            foreach (var getCmd in getCmds)
            {
                if (tp.IsOk)
                {
                    var receive = await WriteIdentCommand(device, getCmd, paramGet: param, countChecked: 2,
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

            if (!tp.IsOk)
            {
                continue;
            }

            device.StatusTest = StatusDeviceTest.Ok;
            //
            SetPriorityStatusStand(3, $"запись и проверка параметров, ок!", percentSubTest: 100, clearAll: true);
            //
            t?.Add(true);
            return deviceReceived;
        }

        device.StatusTest = StatusDeviceTest.Error;
        //
        SetPriorityStatusStand(3, $"запись и проверка параметров , ошибка!", percentSubTest: 100,
            colorSubTest: Brushes.Red, clearAll: true);
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


    private async Task<bool> SetTestChannelVip(Vip vip, int channel = 1, TempChecks t = null)
    {
        var relayMeter = devices.GetTypeDevice<RelayMeter>();
        var nameCmds = GetRelayChannelCmds(vip.Id);

        TempChecks tp = TempChecks.Start();

        if (channel == 1)
        {
            SetPriorityStatusStand(2, $"переключение 1 канала измерений Вип - {vip.Name}", percentSubTest: 40,
                colorSubTest: Brushes.DarkSalmon, clearAll: true);

            await WriteIdentCommand(relayMeter, nameCmds.cannel1, countChecked: 2,
                t: tp);
        }

        if (channel == 2)
        {
            SetPriorityStatusStand(2, $"переключение 2 канала измерений Вип - {vip.Name}", percentSubTest: 80,
                colorSubTest: Brushes.DarkSalmon, clearAll: true);

            await WriteIdentCommand(relayMeter, nameCmds.cannel2, countChecked: 2,
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
        }

        if (channel == 2)
        {
            vip.StatusChannelVipTest = StatusChannelVipTest.TwoError;
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

    private async Task<bool> GetErrorInVip(Vip vip, int countChecked = 3, TempChecks t = null, TempChecks te = null)
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
            var receiveDataVip = await WriteIdentCommand(vip.Relay, "Test", countChecked: 1, t: tp, isReceiveVal: true);

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

        else
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


    //--tv--test
    private async Task<bool> TestVip(Vip vip, TypeOfTestRun typeTest, TempChecks t)
    {
        //
        SetPriorityStatusStand(2, $"тест Випа", percentSubTest: 0, colorSubTest: Brushes.Violet, currentVipSubTest: vip,
            clearAll: true);
        //
        TempChecks tp = TempChecks.Start();
        var isError = false;


        decimal current = 0;

        decimal voltage1 = 0;
        decimal voltage2 = 0;

        decimal temperature = 0;

        //волтьтметра
        currentDevice = devices.GetTypeDevice<VoltMeter>();
        var getVoltValues = GetParameterForDevice().VoltValues;
        if (tp.IsOk)
            await SetCheckValueInDevice(currentDevice, "Set volt meter", getVoltValues.VoltMaxLimit,
                2, 1000, tp, "Get func", "Get volt meter");

        currentDevice = devices.GetTypeDevice<VoltCurrentMeter>();
        var getThermoCurrentValues = GetParameterForDevice().VoltCurrentValues;

        //волтьтамеперметра
        if (tp.IsOk)
        {
            await SetCheckValueInDevice(currentDevice, "Set volt meter", getThermoCurrentValues.VoltMaxLimit,
                2, 1000, tp, "Get func", "Get volt meter");
        }

        //TODO вернуть!
        // TempChecks tpe = TempChecks.Start();
        // if (tp.IsOk)
        //     await GetErrorInVip(vip, t: tp, te: tpe);
        // if (!tpe.IsOk && tp.IsOk)
        // {
        //     t.Add(true);
        //     return true;
        // }
        //TODO вернуть!

        if (tp.IsOk)
            await OutputDevice(vip.Relay, t: tp);

        if (tp.IsOk)
        {
            currentDevices.Clear();
            currentDevices.Add(devices.GetTypeDevice<VoltMeter>());
            if (vip.Type.PrepareMaxVoltageOut2 > 0)
            {
                currentDevices.Add(devices.GetTypeDevice<VoltCurrentMeter>());
            }


            if (tp.IsOk) await SetTestChannelVip(vip, 1, tp);

            //интервал для времени устаканивания напряжений 
            //TODO подбирать вручную
            try
            {
                if (typeTest == TypeOfTestRun.MeasurementZero)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(vip.Type.ZeroTestInterval), ctsAllCancel.Token);
                }

                if (typeTest == TypeOfTestRun.AvailabilityCheckVip)
                {
                    //TODO утьчниь про эти задержки
                    await Task.Delay(TimeSpan.FromMilliseconds(100), ctsAllCancel.Token);
                }
                else
                {
                    //TODO утьчниь про эти задержки
                    await Task.Delay(TimeSpan.FromMilliseconds(100), ctsAllCancel.Token);
                }
            }
            catch (TaskCanceledException e) when (ctsAllCancel.IsCancellationRequested)
            {
                t.Add(false);
                return false;
            }

            var receiveData1 = await WriteIdentCommands(currentDevices, "Get all value", t: tp, isReceiveVal: true);

            TempChecks tpv1 = TempChecks.Start();
            TempChecks tpv2 = TempChecks.Start();

            //
            SetPriorityStatusStand(2, $"Тест Випа: ", percentSubTest: 30, colorSubTest: Brushes.Violet,
                currentVipSubTest: vip, clearAll: true);
            //

            if (tp.IsOk)
            {
                if (typeTest is TypeOfTestRun.AvailabilityCheckVip or TypeOfTestRun.MeasurementZero)
                {
                    voltage1 = GetValueReceives(devices.GetTypeDevice<VoltMeter>(), receiveData1);
                    CheckValueInVip(vip, voltage1, TypeCheckVal.PrepareVoltage1, tpv1);

                    vip.VoltageOut1 = voltage1;

                    if (vip.Type.PrepareMaxVoltageOut2 > 0)
                    {
                        voltage2 = GetValueReceives(devices.GetTypeDevice<VoltCurrentMeter>(), receiveData1);
                        CheckValueInVip(vip, voltage2, TypeCheckVal.PrepareVoltage2, tpv2);

                        vip.VoltageOut2 = voltage2;
                    }
                }

                if (typeTest is TypeOfTestRun.CyclicMeasurement)
                {
                    voltage1 = GetValueReceives(devices.GetTypeDevice<VoltMeter>(), receiveData1);
                    CheckValueInVip(vip, voltage1, TypeCheckVal.Voltage1, tpv1);

                    vip.VoltageOut1 = voltage1;

                    if (vip.Type.PrepareMaxVoltageOut2 > 0)
                    {
                        voltage2 = GetValueReceives(devices.GetTypeDevice<VoltCurrentMeter>(), receiveData1);
                        CheckValueInVip(vip, voltage2, TypeCheckVal.Voltage2, tpv2);

                        vip.VoltageOut2 = voltage2;
                    }
                }
            }

            //волтьтамеперметра
            if (tp.IsOk)
            {
                currentDevice = devices.GetTypeDevice<VoltCurrentMeter>();
                await SetCheckValueInDevice(currentDevice, "Set curr meter", getThermoCurrentValues.CurrMaxLimit,
                    2, 1000, tp, "Get func", "Get curr meter");
            }

            if (tp.IsOk) await SetTestChannelVip(vip, 2, tp);

            currentDevice = devices.GetTypeDevice<VoltCurrentMeter>();
            var receiveData2 = await WriteIdentCommand(currentDevice, "Get all value", t: tp, isReceiveVal: true);


            TempChecks tpc = TempChecks.Start();

            if (tp.IsOk)
            {
                if (typeTest is TypeOfTestRun.MeasurementZero)
                {
                    current = GetValueReceive(devices.GetTypeDevice<VoltCurrentMeter>(), receiveData2);
                    CheckValueInVip(vip, current, TypeCheckVal.PrepareCurrent, tpc);

                    vip.CurrentIn = current;
                }

                if (typeTest is TypeOfTestRun.AvailabilityCheckVip)
                {
                    current = GetValueReceive(devices.GetTypeDevice<VoltCurrentMeter>(), receiveData2);
                    CheckValueInVip(vip, current, TypeCheckVal.AvailabilityCurrent, tpc);

                    vip.CurrentIn = current;
                }

                if (typeTest is TypeOfTestRun.CyclicMeasurement)
                {
                    current = GetValueReceive(devices.GetTypeDevice<VoltCurrentMeter>(), receiveData2);
                    CheckValueInVip(vip, current, TypeCheckVal.Current, tpc);

                    vip.CurrentIn = current;
                }
            }

            //TODO вернуть каогда появится термометр
            // currentDevice = devices.GetTypeDevice<Thermometer>();
            // var receiveDataT = await WriteIdentCommands(currentDevices, "Get all value", t: tp, isReceiveVal: true);
            //
            // TempChecks tpt = TempChecks.Start();
            //
            // if (tp.IsOk)
            // {
            //     temperature = GetValueReceives(devices.GetTypeDevice<VoltMeter>(), receiveDataT);
            //     CheckValueInVip(vip, temperature, TypeCheckVal.Temperature, tpt);
            // }
            //TODO вернуть каогда появится термометр


            if (!tpv1.IsOk || !tpv2.IsOk || !tpc.IsOk)
            {
                bool extraError = false;


                isError = true;
                if (tp.IsOk)
                    await OutputDevice(vip.Relay, t: tp, on: false);

                vip.StatusTest = StatusDeviceTest.Error;


                vip.ErrorStatusVip = "/";

                if (typeTest is TypeOfTestRun.AvailabilityCheckVip or TypeOfTestRun.MeasurementZero)
                {
                    if (!tpv1.IsOk)
                    {
                        if (voltage1 > vip.Type.PrepareMaxVoltageOut1)
                        {
                            var over = voltage1 - vip.Type.PrepareMaxVoltageOut1;
                            vip.ErrorStatusVip += $"U1вых.↑ на {over}В ";
                            extraError = true;
                        }

                        if (voltage1 < vip.Type.PrepareMaxVoltageOut1)
                        {
                            var over = vip.Type.PrepareMaxVoltageOut1 - voltage1;
                            vip.ErrorStatusVip += $"U1вых.↓ на {over}В ";
                            extraError = true;
                        }
                    }

                    if (!tpv2.IsOk)
                    {
                        if (voltage2 > vip.Type.PrepareMaxVoltageOut2)
                        {
                            if (extraError)
                            {
                                vip.ErrorStatusVip += "/";
                            }

                            var over = voltage2 - vip.Type.PrepareMaxVoltageOut2;
                            vip.ErrorStatusVip += $"U1вых.↑ на {over}В ";
                            extraError = true;
                        }

                        if (voltage2 < vip.Type.PrepareMaxVoltageOut2)
                        {
                            if (extraError)
                            {
                                vip.ErrorStatusVip += "/";
                            }

                            var over = vip.Type.PrepareMaxVoltageOut2 - voltage2;
                            vip.ErrorStatusVip += $"U1вых.↓ на {over}В ";
                            extraError = true;
                        }
                    }

                    if (!tpc.IsOk)
                    {
                        if (current > vip.Type.PrepareMaxCurrentIn)
                        {
                            if (extraError)
                            {
                                vip.ErrorStatusVip += "/";
                            }

                            var over = current - vip.Type.PrepareMaxCurrentIn;
                            vip.ErrorStatusVip += $" Iвх.↑ на {over}A ";
                            extraError = true;
                        }

                        if (current < vip.Type.PrepareMaxCurrentIn)
                        {
                            if (extraError)
                            {
                                vip.ErrorStatusVip += "/";
                            }

                            var over = vip.Type.PrepareMaxCurrentIn - current;
                            vip.ErrorStatusVip += $" Iвх.↓ на {over}A ";
                            extraError = true;
                        }
                    }
                }

                if (typeTest is TypeOfTestRun.CyclicMeasurement)
                {
                    vip.StatusTest = StatusDeviceTest.Error;

                    if (!tpv1.IsOk)
                    {
                        if (voltage1 > vip.Type.PrepareMaxVoltageOut1)
                        {
                            if (extraError)
                            {
                                vip.ErrorStatusVip += "/";
                            }

                            var over = voltage1 - vip.Type.PrepareMaxVoltageOut1;
                            vip.ErrorStatusVip = $"U1вых.↑ на {over}В ";
                            extraError = true;
                        }

                        if (voltage1 < vip.Type.PrepareMaxVoltageOut1)
                        {
                            if (extraError)
                            {
                                vip.ErrorStatusVip += "/";
                            }

                            var over = vip.Type.PrepareMaxVoltageOut1 - voltage1;
                            vip.ErrorStatusVip = $"U1вых.↓ на {over}В ";
                            extraError = true;
                        }
                    }

                    if (!tpv2.IsOk)
                    {
                        if (voltage2 > vip.Type.PrepareMaxVoltageOut2)
                        {
                            if (extraError)
                            {
                                vip.ErrorStatusVip += "/";
                            }

                            var over = voltage2 - vip.Type.PrepareMaxVoltageOut2;
                            vip.ErrorStatusVip = $"U1вых.↑ на {over}В ";
                            extraError = true;
                        }

                        if (voltage2 < vip.Type.PrepareMaxVoltageOut2)
                        {
                            if (extraError)
                            {
                                vip.ErrorStatusVip += "/";
                            }

                            var over = vip.Type.PrepareMaxVoltageOut2 - voltage2;
                            vip.ErrorStatusVip = $"U1вых.↓ на {over}В ";
                            extraError = true;
                        }
                    }

                    if (!tpc.IsOk)
                    {
                        if (current > vip.Type.PrepareMaxCurrentIn)
                        {
                            if (extraError)
                            {
                                vip.ErrorStatusVip += "/";
                            }

                            var over = current - vip.Type.PrepareMaxCurrentIn;
                            vip.ErrorStatusVip += $" Iвх.↑ на {over}A ";
                        }

                        if (current < vip.Type.PrepareMaxCurrentIn)
                        {
                            if (extraError)
                            {
                                vip.ErrorStatusVip += "/";
                            }

                            var over = vip.Type.PrepareMaxCurrentIn - current;
                            vip.ErrorStatusVip += $" Iвх.↓ на {over}A ";
                        }
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
            }

            vip.StatusChannelVipTest = StatusChannelVipTest.None;
            t.Add(true);
            return true;
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


        //TODO сделать тут
        // TestCurrentDevice = verifiedDevices.Keys.First();

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
        //Debug.WriteLine($"{device.Name}/{device.IsDeviceType}/ {s.ElapsedMilliseconds}");
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
                Debug.WriteLine(
                    $"Ошибка уcтройства {device.IsDeviceType}, команда {device.NameCurrentCmd}/неверный параметр, пришел \"{receive}\"/ожидался \"{device.CurrentParameter}\"\n");
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
            //TODO спросить у темы порядок дейтсвия исключений пока сотавить так
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
            //TODO спросить у темы порядок дейтсвия исключений пока сотавить так
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
            //TODO спросить у темы порядок дейтсвия исключений пока сотавить так
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
                //TODO спросить у темы порядок дейтсвия исключений пока сотавить так
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
            //TODO спросить у темы порядок дейтсвия исключений пока сотавить так
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
    /// <param name="externalDelay"></param>
    /// <param name="t"></param>
    /// <param name="forcedOff"></param>
    /// <param name="on">true - вкл, false - выкл</param>
    /// <param name="forcedAttempt"></param>
    /// <param name="values">Ответ который утройство должно отправить в ответ на запрос output</param>
    /// <returns>Результат включения/выключение</returns>
    private async Task<(BaseDevice outputDevice, bool outputResult)> OutputDevice(BaseDevice device, int channel = -1,
        int countChecked = 3, int externalDelay = 400, TempChecks t = null, bool forcedOff = false, bool on = true)
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
            SetOutputOnCmdName = "On";
            SetOutputOffCmdName = "Off";
        }
        else
        {
            //TODO проверить
            device.StatusOnOff = OnOffStatus.None;
            // 
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
                        await CheckConnectPort(device, 2, t: tp);
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
                                if (channel == 1)
                                {
                                    var receiveOutput = await WriteIdentCommand(r, "On1", countChecked: 1,
                                        loopDelay: externalDelay, t: tp);
                                }

                                if (channel == 2)
                                {
                                    var receiveOutput = await WriteIdentCommand(r, "On2", countChecked: 1,
                                        loopDelay: externalDelay, t: tp);
                                }

                                if (channel == 3)
                                {
                                    var receiveOutput = await WriteIdentCommand(r, "On3", countChecked: 1,
                                        loopDelay: externalDelay, t: tp);
                                }

                                if (channel == 4)
                                {
                                    var receiveOutput = await WriteIdentCommand(r, "On4", countChecked: 1,
                                        loopDelay: externalDelay, t: tp);
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
                                var receiveOutput = await WriteIdentCommand(r, SetOutputOnCmdName, countChecked: 1,
                                    loopDelay: externalDelay, t: tp);

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
                            }
                        }

                        #endregion

                        #region вкл внешнее утсройство

                        else
                        {
                            //опрос выхода устройства
                            var cmdResult = await WriteIdentCommand(device, getOutputCmdName, countChecked: 1, t: tp);

                            //если выход выкл 
                            if (cmdResult.Value == getParam.OutputOff && tp.IsOk)
                            {
                                //делаем выход вкл
                                await WriteIdentCommand(device, SetOutputOnCmdName);

                                //опрос выхода утсртойства
                                cmdResult = await WriteIdentCommand(device, getOutputCmdName, countChecked: 1,
                                    loopDelay: externalDelay, t: tp);
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
                                var receiveOutput = await WriteIdentCommand(r, "Off1", countChecked: 1,
                                    loopDelay: externalDelay, t: tp);
                            }

                            if (channel == 2)
                            {
                                var receiveOutput = await WriteIdentCommand(r, "Off2", countChecked: 1,
                                    loopDelay: externalDelay, t: tp);
                            }

                            if (channel == 3)
                            {
                                var receiveOutput = await WriteIdentCommand(r, "Off3", countChecked: 1,
                                    loopDelay: externalDelay, t: tp);
                            }

                            if (channel == 4)
                            {
                                var receiveOutput = await WriteIdentCommand(r, "Off4", countChecked: 1,
                                    loopDelay: externalDelay, t: tp);
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
                            var receiveOutput = await WriteIdentCommand(r, SetOutputOffCmdName, countChecked: 1,
                                loopDelay: externalDelay,
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
                        var cmdResult = await WriteIdentCommand(device, getOutputCmdName, countChecked: 1, t: tp);

                        //если выход вкл 
                        if (cmdResult.Value == getParam.OutputOn && tp.IsOk)
                        {
                            //делаем выход выкл
                            await WriteIdentCommand(device, SetOutputOffCmdName);

                            //опрос выхода устройства
                            cmdResult = await WriteIdentCommand(device, getOutputCmdName, countChecked: 1,
                                loopDelay: externalDelay, t: tp);
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

    public void SerializeTime()
    {
        creatorAllDevicesAndLib.SerializeTime(timeMachine);
    }

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
    Current,
    Voltage1,
    Voltage2,
    PrepareVoltage1,
    PrepareVoltage2,
    PrepareCurrent,
    AvailabilityCurrent,
    Temperature
}