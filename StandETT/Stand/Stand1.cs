using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace StandETT;

public class Stand1 : Notify
{
    #region оздание устройств

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

    private double percentCurrentTest;

    /// <summary>
    /// На соклько процентов выполнен текущий тест
    /// </summary>
    public double PercentCurrentTest
    {
        get => percentCurrentTest;
        set => Set(ref percentCurrentTest, value);
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
    public Action<BaseDevice, bool> CheckPort;

    /// <summary>
    /// Событие проверки коннекта к устройству
    /// </summary>
    public Action<BaseDevice, bool, string> CheckDevice;

    /// <summary>
    /// Событие приема данных с устройства
    /// </summary>
    public Action<BaseDevice, string> Receive;

    /// <summary>
    /// Событие приема данных с устройства
    /// </summary>
    public Action<bool> OpenActionWindow;

    #endregion

    //---

    #region Токены

    CancellationTokenSource ctsAllCancel = new();
    CancellationTokenSource ctsReceiveDevice = new();
    CancellationTokenSource ctsConnectDevice = new();

    #endregion

    //---

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
        devices = new(allDevices.Where(d => d is not MainRelay && d is not RelayVip));
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
            currentCheck == 0 ? string.Empty : $"Попытка: {currentCheck.ToString()}-я из {currentCheck}";
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


    /// <summary>
    /// Установка статусов устройств
    /// </summary>
    /// <param name="device">Устройство</param>
    /// <param name="test">Статус устройства (default = None)</param>
    void SetStatusDevices(List<BaseDevice> devices, StatusDeviceTest test = StatusDeviceTest.None)
    {
        foreach (var device in devices)
        {
            SetStatusDevice(device, test);
        }
    }

    /// <summary>
    /// Установка статуса устройства
    /// </summary>
    /// <param name="device">Устройство</param>
    /// <param name="test">Статус устройства (default = None)</param>
    void SetStatusDevice(BaseDevice device, StatusDeviceTest test = StatusDeviceTest.None)
    {
        device.StatusTest = test;
    }

    /// <summary>
    /// Установка статуса включения випа
    /// </summary>
    /// <param name="vip">Вип</param>
    /// <param name="status">Статус включения (default = Off)</param>
    void SetStatusEnabledCurrentVip(Vip vip, OnOffStatus status = OnOffStatus.Off)
    {
        vip.StatusOnOff = status;
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

    #endregion


    //---

    #region --Проверки--Тесты

    private bool resetAll = false;

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

    private string captionAction;

    public string CaptionAction
    {
        get => captionAction;
        set => Set(ref captionAction, value);
    }

    //--stop--resetall--allreset
    //Остановка всех тестов
    public async Task ResetAllTests()
    {
        resetAll = true;
        ctsAllCancel.Cancel();
        ctsConnectDevice.Cancel();
        ctsReceiveDevice.Cancel();
        TestRun = TypeOfTestRun.Stoped;
        OpenActionWindow?.Invoke(true);

        await Task.Delay(TimeSpan.FromMilliseconds(200));

        ctsAllCancel = new CancellationTokenSource();
        ctsConnectDevice = new CancellationTokenSource();
        ctsReceiveDevice = new CancellationTokenSource();

        resetAll = false;

        TempChecks t = TempChecks.Start();
        PercentCurrentReset = 0;
        ProgressResetColor = Brushes.Green;
        ErrorMessage = string.Empty;
        ErrorOutput = string.Empty;

        var percent = 100;

        if (relayVipsTested.Any())
        {
            percent = 50;
            mainRelay.Relays = relayVipsTested;

            foreach (var relay in relayVipsTested)
            {
                ErrorMessage = $"Отключение выхода реле Випа {relay.Name}...";
                relay.StatusTest = StatusDeviceTest.None;
                var resultOutput = await OutputDevice(relay, t: t, on: false);
                if (!resultOutput.outputResult)
                {
                    ErrorOutput += $"{relay.ErrorStatus}\n";
                    ProgressResetColor = Brushes.Red;
                }

                PercentCurrentReset += Math.Round((1 / (float)relayVipsTested.Count) * percent);
            }
        }

        foreach (var device in devices)
        {
            ErrorMessage = $"Отключение выхода устройства {device.IsDeviceType}/{device.Name}...";
            device.StatusTest = StatusDeviceTest.None;
            var resultOutput = await OutputDevice(device, 1, t: t, on: false);
            if (!resultOutput.outputResult)
            {
                ErrorOutput += $"{device.ErrorStatus}\n";
                ProgressResetColor = Brushes.Red;
            }

            PercentCurrentReset += Math.Round((1 / (float)devices.Count) * percent);
        }

        if (t.IsOk)
        {
            ProgressResetColor = Brushes.Green;
            ErrorMessage = "Все выходы устройств были благополучно отключены";
        }
        else
        {
            ErrorMessage =
                "Осторожно! Следующие выходы устройств не были отключены, тк к устройствам нет доступа " +
                "или они неправильно индентифицированы";
            ErrorOutput += "Выходы остальных устройств отключены\n";
        }

        //
        CaptionAction = "Стенд остановлен";
        TestRun = TypeOfTestRun.Stop;
        PercentCurrentReset = 100;
        CurrentCountChecked = string.Empty;
        TestCurrentDevice = null;
        SubTestText = string.Empty;
        //
    }

    //--приборы--проверка--check--devices--primary
    public async Task<bool> PrimaryCheckDevices(int countChecked = 3, int loopDelay = 3000)
    {
        //
        TestRun = TypeOfTestRun.PrimaryCheckDevices;
        //
        var t = TempChecks.Start();
        await CheckConnectPorts(devices.ToList(), t: t);

        if (t.IsOk)
        {
            t = TempChecks.Start();
            await WriteIdentCommands(devices.ToList(), "Status", countChecked: countChecked, t: t,
                loopDelay: loopDelay);
            if (t.IsOk)
            {
                //
                TestRun = TypeOfTestRun.PrimaryCheckDevicesReady;
                PercentCurrentTest = 100;
                TestCurrentDevice = null;
                //
                return true;
            }
        }

        if (!resetAll)
        {
            TestRun = TypeOfTestRun.Error;
            PercentCurrentTest = 100;
            ProgressColor = Brushes.Red;
            TestCurrentDevice = null;
            throw new Exception("Одно или несколько устройств не ответили или ответили\nс ошибкой. ");
        }

        //
        return false;
    }

    private ObservableCollection<Vip> vipsTested = new();
    private ObservableCollection<RelayVip> relayVipsTested = new();

    //--випы--vips--relay--check--проверка--
    public async Task<bool> PrimaryCheckVips(int countChecked = 3, int loopDelay = 3000)
    {
        //
        TestRun = TypeOfTestRun.PrimaryCheckVips;
        //

        //сбросы всех статусов перед проверкой
        foreach (var relayVip in allRelayVips)
        {
            relayVip.StatusTest = StatusDeviceTest.None;
            relayVip.ErrorStatus = string.Empty;
        }
        //

        //предварительная настройка тестрировать ли вип => если у Випа есть имя то тестировать
        vipsTested = GetIsTestedVips();
        relayVipsTested.Clear();
        foreach (var vip in vipsTested)
        {
            relayVipsTested.Add(vip.Relay);
        }

        mainRelay.Relays = relayVipsTested;

        TempChecks t = TempChecks.Start();

        await CheckConnectPort(mainRelay, t: t);

        if (t.IsOk)
        {
            t = TempChecks.Start();

            foreach (var relay in relayVipsTested)
            {
                await WriteIdentCommand(relay, "Status", countChecked: countChecked, loopDelay: loopDelay, t: t);
            }

            if (t.IsOk)
            {
                TestRun = TypeOfTestRun.PrimaryCheckVipsReady;
                PercentCurrentTest = 100;
                return true;
            }
        }

        if (!resetAll)
        {
            TestRun = TypeOfTestRun.Error;
            PercentCurrentTest = 100;
            ProgressColor = Brushes.Red;
            TestCurrentDevice = null;
            throw new Exception("Одно или несколько устройств не ответили или ответили\nс ошибкой. ");
        }

        return false;
    }

    Stopwatch s = new Stopwatch();
    //private bool externalCount = false;

    //--params--chechkparams--value--
    private async Task<Dictionary<BaseDevice, List<string>>> SetCheckValueInDevice(BaseDevice device,
        string setCmd = null,
        string param = null, int countChecked = 3,
        int loopDelay = 200, TempChecks t = null, bool externalStatus = false,
        params string[] getCmds)
    {
        var deviceReceived = new Dictionary<BaseDevice, List<string>>();
        for (int i = 1; i <= countChecked; i++)
        {
            CurrentCountChecked = $"Попытка: {i.ToString()}/{countChecked}";
            SubTestText = $"запись и проверка параметров";
            TempChecks tp = TempChecks.Start();

            if (!string.IsNullOrEmpty(setCmd))
            {
                await WriteIdentCommand(device, setCmd, externalStatus: true, paramSet: param);
            }

            tp.Add(true);

            foreach (var getCmd in getCmds)
            {
                if (tp.IsOk)
                {
                    var receive = await WriteIdentCommand(device, getCmd, paramGet: param, countChecked: 2,
                        externalStatus: true, loopDelay: loopDelay, t: tp);

                    //await Task.Delay(TimeSpan.FromMilliseconds(100));

                    if (receive.Key != null && !deviceReceived.ContainsKey(receive.Key))
                    {
                        //добавляем туда данные и создаем список в ктороые будет записаны ответы от прибора
                        deviceReceived.Add(device, new List<string>());
                    }

                    if (receive.Key != null) deviceReceived[receive.Key].Add(receive.Value);
                }
            }

            if (!tp.IsOk)
            {
                continue;
            }

            device.StatusTest = StatusDeviceTest.Ok;
            SubTestText = $"запись и проверка параметров , ок!";
            t?.Add(true);
            return deviceReceived;
        }

        device.StatusTest = StatusDeviceTest.Error;
        SubTestText = $"запись и проверка параметров , ошибка!";
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
            _ => (string.Empty, string.Empty)
        };
        return cmdNames;
    }

    private async Task<bool> SetTestChannelVip(Vip vip, int channel = 1, TempChecks t = null,
        bool externalStatus = false)
    {
        var relayMeter = devices.GetTypeDevice<RelayMeter>();
        var nameCmds = GetRelayChannelCmds(vip.Id);

        TempChecks tp = TempChecks.Start();

        if (channel == 1)
        {
            if (!externalStatus)
            {
                SubTestText = $"переключение 1 канала измерений Вип - {vip.Name}";
                PercentCurrentTest = 0;
                ProgressColor = Brushes.SeaGreen;
            }

            await WriteIdentCommand(relayMeter, nameCmds.cannel1, countChecked: 2, externalStatus: true,
                t: tp);

            if (!externalStatus)
            {
                PercentCurrentTest = 50;
            }
        }

        if (channel == 2)
        {
            if (!externalStatus)
            {
                SubTestText = $"переключение 2 канала измерений Вип - {vip.Name}";
                PercentCurrentTest = 0;
                ProgressColor = Brushes.SeaGreen;
            }

            await WriteIdentCommand(relayMeter, nameCmds.cannel2, countChecked: 2, externalStatus: true,
                t: tp);

            if (!externalStatus)
            {
                PercentCurrentTest = 100;
            }
        }


        if (tp.IsOk)
        {
            if (!externalStatus)
            {
                //relayMeter.StatusTest = StatusDeviceTest.Ok;
                SubTestText = $"переключение канала {channel} измерений Вип - {vip.Name}, ок!";
                PercentCurrentTest = 0;
                ProgressColor = Brushes.SeaGreen;
            }

            relayMeter.StatusTest = StatusDeviceTest.Ok;

            t?.Add(true);
            return true;
        }

        if (!externalStatus)
        {
            //relayMeter.StatusTest = StatusDeviceTest.Error;
            SubTestText = $"переключение канала {channel} измерений Вип - {vip.Name}, ошибка!";
        }

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

    private async Task<bool> ZeroTestVip(Vip vip, TempChecks t)
    {
        TempChecks tp = TempChecks.Start();

        //

        decimal voltage1 = 0;
        decimal voltage2 = 0;
        decimal current = 0;

        currentDevices.Clear();
        currentDevices.Add(devices.GetTypeDevice<VoltMeter>());
        currentDevices.Add(devices.GetTypeDevice<ThermoCurrentMeter>());


        if (vip.Type.PrepareMaxVoltageOut1 > 0)
        {
            var receiveData = await WriteIdentCommands(currentDevices, "Get all value", t: tp, isReceiveVal: true);

            if (tp.IsOk)
            {
                current = GetValueReceives(devices.GetTypeDevice<ThermoCurrentMeter>(), receiveData);
                CheckValueInVip(vip, current, TypeCheckVal.PrepareCurrent, tp);
                if (!tp.IsOk)
                {
                    throw new Exception(
                        $"Вип - {vip.Id}, ошибка 0 замера! Текущий ток 1 канала - {current}А," +
                        $" а должно быть {vip.Type.PrepareMaxCurrentIn}А/\n");
                }

                voltage1 = GetValueReceives(devices.GetTypeDevice<VoltMeter>(), receiveData);
                CheckValueInVip(vip, voltage1, TypeCheckVal.PrepareVoltage1, tp);
                if (!tp.IsOk)
                {
                    throw new Exception(
                        $"Вип - {vip.Id}, ошибка 0 замера! Текущее напряжение 1 канала - {voltage1}В, " +
                        $"а должно быть {vip.Type.MaxVoltageOut1}В/\n");
                }
            }
        }

        if (vip.Type.PrepareMaxVoltageOut2 > 0)
        {
            await SetTestChannelVip(vip, 2, tp);
            var receiveData = await WriteIdentCommands(currentDevices, "Get all value", t: tp, isReceiveVal: true);
            if (tp.IsOk)
            {
                voltage2 = GetValueReceives(devices.GetTypeDevice<VoltMeter>(), receiveData);
                CheckValueInVip(vip, voltage2, TypeCheckVal.PrepareVoltage2, tp);
                if (!tp.IsOk)
                {
                    throw new Exception(
                        $"Вип - {vip.Id}, ошибка 0 замера! Текущее напряжение 2 канала - {voltage1}В, " +
                        $"а должно быть {vip.Type.MaxVoltageOut2}В/\n");
                }
            }
        }

        //
        if (tp.IsOk)
        {
            t.Add(true);
            return true;
        }

        t.Add(false);
        return false;
    }

    private (bool isChecked, Threshold threshold) CheckValueInVip(Vip vip, decimal receiveVal,
        TypeCheckVal typeCheckVal,
        TempChecks t = null)
    {
        decimal vipMaxVal = 0;
        decimal vipPercentVal = 0;

        if (typeCheckVal == TypeCheckVal.Temperature)
        {
            vipMaxVal = vip.Type.MaxTemperature;
            vipPercentVal = vip.Type.PercentAccuracyTemperature;
        }

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

        if (typeCheckVal == TypeCheckVal.Current)
        {
            vipMaxVal = vip.Type.MaxCurrentIn;
            vipPercentVal = vip.Type.PercentAccuracyCurrent;
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

        if (typeCheckVal == TypeCheckVal.PrepareCurrent)
        {
            vipMaxVal = vip.Type.PrepareMaxCurrentIn;
            vipPercentVal = vip.Type.PercentAccuracyCurrent;
        }

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

    //--zero--ноль--
    public async Task<bool> MeasurementZero(int countChecked = 3, int loopDelay = 1000)
    {
        //
        TestRun = TypeOfTestRun.MeasurementZero;
        //

        bool isErrorRelayVip = false;
        TempChecks t = TempChecks.Start();

        // установки настройек приобров
        // волтьтметра

        currentDevice = devices.GetTypeDevice<VoltMeter>();
        var getVoltValues = GetParameterForDevice().VoltValues;
        if (t.IsOk)
            await SetCheckValueInDevice(currentDevice, "Set volt meter", getVoltValues.VoltMaxLimit,
                countChecked, loopDelay, t, true, "Get func", "Get volt meter");

        //амперметра
        if (t.IsOk) currentDevice = devices.GetTypeDevice<ThermoCurrentMeter>();
        var getThermoCurrentValues = GetParameterForDevice().ThermoCurrentValues;
        if (t.IsOk)
            await SetCheckValueInDevice(currentDevice, "Set curr meter", getThermoCurrentValues.CurrMaxLimit,
                countChecked, loopDelay, t, true, "Get func", "Get curr meter");

        //большой нагрузки
        if (t.IsOk) currentDevice = devices.GetTypeDevice<BigLoad>();
        var getBigLoadValues = GetParameterForDevice().BigLoadValues;
        await OutputDevice(currentDevice, t: t, on: false);

        if (t.IsOk)
            await SetCheckValueInDevice(currentDevice, "Set freq", getBigLoadValues.Freq,
                countChecked, loopDelay, t, true, "Get freq");
        if (t.IsOk)
            await SetCheckValueInDevice(currentDevice, "Set ampl", getBigLoadValues.Ampl,
                countChecked, loopDelay, t, true, "Get ampl");
        if (t.IsOk)
            await SetCheckValueInDevice(currentDevice, "Set dco", getBigLoadValues.Dco,
                countChecked, loopDelay, t, true, "Get dco");
        if (t.IsOk)
            await SetCheckValueInDevice(currentDevice, "Set squ", getBigLoadValues.Squ,
                countChecked, loopDelay, t, true, "Get squ");
        if (t.IsOk)
            await OutputDevice(currentDevice, t: t);

        //бп
        if (t.IsOk)
            currentDevice = devices.GetTypeDevice<Supply>();
        var getSupplyValues = GetParameterForDevice().SupplyValues;

        if (t.IsOk)
            await OutputDevice(currentDevice, t: t, on: false);
        if (t.IsOk)
            await SetCheckValueInDevice(currentDevice, "Set volt", getSupplyValues.Voltage,
                countChecked, loopDelay, t, true, "Get volt");
        if (t.IsOk)
            await SetCheckValueInDevice(currentDevice, "Set curr", getSupplyValues.Current,
                countChecked, loopDelay, t, true, "Get curr");
        if (t.IsOk)
            await OutputDevice(currentDevice, t: t);

        //TODO уточнить отсавлть ли
        // if (t.IsOk) 
        //     await SetCheckValueInDevice(currentDevice, null, getSupplyValues.Voltage,
        //         countChecked, loopDelay, t, true, "Get real volt");
        // if (t.IsOk)
        //     await SetCheckValueInDevice(currentDevice, null, getSupplyValues.Current,
        //         countChecked, loopDelay, t, true, "Get real curr");

        foreach (var vipTested in vipsTested)
        {
            try
            {
                if (t.IsOk) await SetTestChannelVip(vipTested, 1, t);
                if (t.IsOk) await OutputDevice(vipTested.Relay, t: t);
                if (t.IsOk) await ZeroTestVip(vipTested, t);
            }
            catch (Exception e)
            {
                await OutputDevice(devices.GetTypeDevice<BigLoad>(), t: t, on: false);
                await OutputDevice(devices.GetTypeDevice<Supply>(), t: t, on: false);

                TestRun = TypeOfTestRun.Error;
                PercentCurrentTest = 100;
                ProgressColor = Brushes.Red;
                TestCurrentDevice = null;
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

        if (!resetAll)
        {
            await OutputDevice(devices.GetTypeDevice<BigLoad>(), t: t, on: false);
            await OutputDevice(devices.GetTypeDevice<Supply>(), t: t, on: false);

            TestRun = TypeOfTestRun.Error;
            PercentCurrentTest = 100;
            ProgressColor = Brushes.Red;
            TestCurrentDevice = null;
            throw new Exception("Одно или несколько устройств не ответили или ответили с ошибкой.\n");
        }

        return false;
    }

    //--prepare--pretest--prestart--
    public async Task<bool> PrepareMeasurementCycle(int countChecked = 3, int loopDelay = 1000, int timeHeatSec = 120)
    {
        //
        TestRun = TypeOfTestRun.WaitSupplyMeasurementZero;
        //
        TempChecks t = TempChecks.Start();

        // currentDevice = devices.GetTypeDevice<SmallLoad>();
        // if (t.IsOk) await WriteIdentCommand(currentDevice, "On 1", t: t);

        // currentDevice = devices.GetTypeDevice<Heat>();
        bool isHeatEnable = true;
        // await OutputDevice(currentDevice, t: t);

        if (t.IsOk)
        {
            currentDevice = devices.GetTypeDevice<ThermoCurrentMeter>();
            var getThermoCurrentValues = GetParameterForDevice().ThermoCurrentValues;
            await WriteIdentCommand(currentDevice, "Set temp meter");
            await SetCheckValueInDevice(currentDevice, "Set tco type", getThermoCurrentValues.TermocoupleType,
                countChecked, loopDelay, t, true, "Get tco type");
        }


        //цикл нагревание пластины 

        var timeHeat = timeHeatSec / 10;
        if (t.IsOk)
        {
            for (int i = 0; i < timeHeat; i++)
            {
                try
                {
                    var receiveData = await WriteIdentCommand(currentDevice, "Get all value", t: t);
                    var temperature = GetValueReceive(currentDevice, receiveData);
                    var isChecked = CheckValueInVip(vipsTested[0], temperature, TypeCheckVal.Temperature);

                    //
                    if (isChecked.threshold == Threshold.High && isHeatEnable)
                    {
                        isHeatEnable = false;
                        //if (t.IsOk) await OutputDevice(devices.GetTypeDevice<Heat>(), t: t, on: false);
                    }
                    else if (!isHeatEnable)
                    {
                        isHeatEnable = true;
                        //if (t.IsOk) await OutputDevice(devices.GetTypeDevice<Heat>(), t: t);
                    }
                    //

                    if (isChecked.isChecked)
                    {
                        //TODO Предупрждение все ок, нагрелось за отведенное время!
                        t?.Add(true);
                        break;
                    }

                    //проверка нагрева каждые 10 сек
                    await Task.Delay(TimeSpan.FromMilliseconds(10000), ctsAllCancel.Token);
                }
                catch (TaskCanceledException e) when (ctsAllCancel.IsCancellationRequested)
                {
                    ctsAllCancel = new CancellationTokenSource();
                    t?.Add(false);
                    return false;
                }
            }
        }

        //TODO венуть
        if (t.IsOk)
        {
            TestRun = TypeOfTestRun.WaitSupplyMeasurementZeroReady;
            PercentCurrentTest = 100;
            return true;
        }

        if (!resetAll)
        {
            await OutputDevice(devices.GetTypeDevice<BigLoad>(), t: t, on: false);
            await OutputDevice(devices.GetTypeDevice<Supply>(), t: t, on: false);

            TestRun = TypeOfTestRun.Error;
            PercentCurrentTest = 100;
            ProgressColor = Brushes.Red;
            TestCurrentDevice = null;
            throw new Exception("Одно или несколько устройств не ответили или ответили с ошибкой.\n");
        }

        return false;
    }

    //--mesaure--cycle--test--start--
    public async Task<bool> MeasurementCycle(int allTimeCycle, int betweenTimeCycle, int countChecked = 3,
        int loopDelay = 1000)
    {
        while (true)
        {
            //TODO тесты...

            //ожидание след измерения
            await Task.Delay(TimeSpan.FromMilliseconds(betweenTimeCycle), ctsAllCancel.Token);
        }

        await Task.Delay(TimeSpan.FromMilliseconds(allTimeCycle), ctsAllCancel.Token);
        return false;
    }


    private ObservableCollection<Vip> GetIsTestedVips()
    {
        foreach (var vip in vips)
        {
            vip.StatusTest = StatusDeviceTest.None;
            vip.StatusOnOff = OnOffStatus.Off;
            if (!string.IsNullOrWhiteSpace(vip.Name))
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
    /// <param name="externalDelay">Общая задержка проверки (default = 100)</param>
    /// <param name="countChecks">Колво проверок если не работает</param>
    /// <returns name="errorDevice"></returns>
    async Task<bool> CheckConnectPort(BaseDevice device, int countChecked = 5, int loopDelay = 200, TempChecks t = null,
        bool externalStatus = false)
    {
        if (resetAll)
        {
            t?.Add(false);
            return false;
        }

        currentConnectDevices = new() { device };
        
        //
        if (!externalStatus)
        {
            SubTestText = $"проверка порта - {device.GetConfigDevice().PortName}";
            PercentCurrentTest = 0;
            TestRun = TypeOfTestRun.CheckPorts;
            ProgressColor = Brushes.RoyalBlue;
        }
        //

        try
        {
            for (int i = 1; i <= countChecked; i++)
            {
                if (countChecked < 1)
                {
                    CurrentCountChecked = $"Попытка: {i.ToString()}/{countChecked}";
                }

                if (!externalStatus)
                {
                    CurrentCountChecked = $"Попытка: {i.ToString()}/{countChecked}";
                }

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
                if (!externalStatus)
                {
                    PercentCurrentTest = 50;
                    //device.StatusTest = StatusDeviceTest.None;
                    device.ErrorStatus = string.Empty;
                }

                //
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
                if (!externalStatus)
                {
                    SubTestText = $"проверка порта - {device.GetConfigDevice().PortName}, ок!";
                    PercentCurrentTest = 100;
                    TestRun = TypeOfTestRun.CheckPortsReady;
                    CurrentCountChecked = string.Empty;
                    TestCurrentDevice = null;
                }
                //

                t?.Add(true);
                return true;
            }

            device.Close();
            await Task.Delay(TimeSpan.FromMilliseconds(80), ctsAllCancel.Token);

            //
            if (!externalStatus)
            {
                //device.StatusTest = StatusDeviceTest.Error;
                SubTestText = $"проверка порта - {device.GetConfigDevice().PortName}, ошибка!";
                PercentCurrentTest = 100;
                TestRun = TypeOfTestRun.Error;
                ProgressColor = Brushes.Red;
                TestCurrentDevice = null;
            }

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
            if (!externalStatus)
            {
                //device.StatusTest = StatusDeviceTest.Error;
                SubTestText = $"проверка порта - {device.GetConfigDevice().PortName}, ошибка!";
                PercentCurrentTest = 100;
                TestRun = TypeOfTestRun.Error;
                ProgressColor = Brushes.Red;
                TestCurrentDevice = null;
            }

            device.StatusTest = StatusDeviceTest.Error;
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

    //--checks--ports--conns--
    /// <summary>
    /// Проверка на физическое существование портов (несколько)
    /// </summary>
    /// <param name="devices"></param>
    /// <param name="countChecked">Колво проверок если не работает</param>
    /// <param name="tempChecks"></param>
    /// <param name="device">Устройство</param>
    /// <param name="externalDelay">Общая задержка проверки (default = 100)</param>
    /// <returns name="errorDevice"></returns>
    async Task<bool> CheckConnectPorts(List<BaseDevice> devices, int countChecked = 5, int loopDelay = 200,
        TempChecks t = null, bool externalStatus = false)
    {
        if (resetAll)
        {
            t?.Add(false);
            return false;
        }

        currentConnectDevices = devices;
        var verifiedDevices = new Dictionary<BaseDevice, bool>();

        //
        if (!externalStatus)
        {
            SubTestText = $"проверка портов";
            TestRun = TypeOfTestRun.CheckPorts;
            ProgressColor = Brushes.RoyalBlue;
        }
        //

        try
        {
            for (int i = 1; i <= countChecked; i++)
            {
                //
                if (!externalStatus)
                {
                    CurrentCountChecked = $"Попытка: {i.ToString()}/{countChecked}";

                    PercentCurrentTest = 0;
                }
                //

                try
                {
                    if (i > 1)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(loopDelay), ctsConnectDevice.Token);
                    }

                    foreach (var device in devices)
                    {
                        device.StatusTest = StatusDeviceTest.None;
                        device.ErrorStatus = string.Empty;
                        device.Close();
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(80), ctsAllCancel.Token);

                    foreach (var device in devices)
                    {
                        TestCurrentDevice = device;

                        device.Start();
                        await Task.Delay(TimeSpan.FromMilliseconds(20), ctsAllCancel.Token);
                        device.DtrEnable();

                        //
                        if (!externalStatus)
                        {
                            SubTestText = $"проверка порта - {device.GetConfigDevice().PortName}";
                            var precent = ((1 / (float)devices.Count) * 100);
                            if (precent > 100)
                            {
                                precent = 100;
                            }

                            PercentCurrentTest += precent;
                        }
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
                    TestCurrentDevice.StatusTest = StatusDeviceTest.Error;
                    TestCurrentDevice = verifiedDevices.Keys.First();
                    continue;
                }


                //
                if (!externalStatus)
                {
                    SubTestText = $"проверка портов, ок!";
                    TestRun = TypeOfTestRun.CheckPortsReady;
                    CurrentCountChecked = string.Empty;
                    TestCurrentDevice = null; //null;
                }

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
            if (!externalStatus)
            {
                SubTestText = $"проверка портов, ошибка!";
                PercentCurrentTest = 100;
                TestRun = TypeOfTestRun.Error;
                ProgressColor = Brushes.Red;
                TestCurrentDevice = null; //null;
            }

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
            if (!externalStatus)
            {
                SubTestText = $"проверка портов, ошибка!";
                PercentCurrentTest = 100;
                TestRun = TypeOfTestRun.Error;
                ProgressColor = Brushes.Red;
                TestCurrentDevice = null; //null;
            }
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
    /// <param name="externalStatus"></param>
    /// <param name="toList"></param>
    /// <param name="tempChecks"></param>
    /// <param name="isLast"></param>
    /// <param name="isReceiveVal"></param>
    private async Task<KeyValuePair<BaseDevice, string>> WriteIdentCommand(BaseDevice device,
        string nameCmd, string paramSet = null, string paramGet = null, int countChecked = 3, int loopDelay = 200,
        TempChecks t = null, CancellationToken token = default, bool externalStatus = false, bool isReceiveVal = false)
    {
        //s.Restart();
        KeyValuePair<BaseDevice, string> deviceReceived = default;

        if (resetAll)
        {
            t?.Add(false);
            return deviceReceived;
        }

        CurrentWriteDevices = new() { device };
        RemoveReceive(device);

        KeyValuePair<BaseDevice, bool> verifiedDevice;

        //если прибор от кторого приходят данные не содержится в библиотеке а при первом приеме данных так и будет
        if (!receiveInDevice.ContainsKey(device))
        {
            //добавляем туда данные и создаем список в ктороые будет записаны ответы от прибора
            receiveInDevice.Add(device, new List<string>());
        }

        //
        if (!externalStatus)
        {
            SubTestText = $"запись команды в устройство";
            PercentCurrentTest = 0;
            TestRun = TypeOfTestRun.WriteDevicesCmd;
            ProgressColor = Brushes.Green;
            TestCurrentDevice = device;
        }
        //

        try
        {
            for (int i = 1; i <= countChecked; i++)
            {
                if (countChecked < 1)
                {
                    CurrentCountChecked = $"Попытка: {i.ToString()}/{countChecked}";
                }

                if (!externalStatus)
                {
                    CurrentCountChecked = $"Попытка: {i.ToString()}/{countChecked}";
                    //device.StatusTest = StatusDeviceTest.None;
                    device.ErrorStatus = string.Empty;
                }

                device.StatusTest = StatusDeviceTest.None;
                try
                {
                    if (i > 1)
                    {
                        ctsReceiveDevice = new CancellationTokenSource();
                        await Task.Delay(TimeSpan.FromMilliseconds(loopDelay), ctsReceiveDevice.Token);
                    }

                    if (!device.PortIsOpen)
                    {
                        //
                        if (!externalStatus)
                        {
                            SubTestText = $"порт закрыт, переподключение...";
                        }
                        //

                        if (!await CheckConnectPort(device, 3, externalStatus: true))
                        {
                            //
                            if (!externalStatus)
                            {
                                SubTestText = $"порт не открыт, ошибка!";
                                PercentCurrentTest = 100;
                                TestCurrentDevice = device;
                                TestRun = TypeOfTestRun.Error;
                                ProgressColor = Brushes.Red;
                            }
                            //

                            t?.Add(false);
                            return deviceReceived;
                        }

                        //
                        if (!externalStatus)
                        {
                            SubTestText = $"переподключение удачно, порт открыт";
                        }
                        //
                    }

                    //установка гет парамтра
                    device.CurrentParameterGet = paramGet;
                    device.IsReceive = isReceiveVal;

                    //запись команды в каждое устройсттво
                    device.WriteCmd(nameCmd, paramSet);

                    //
                    if (!externalStatus)
                    {
                        PercentCurrentTest = 50;
                    }
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
                        if (!externalStatus)
                        {
                            SubTestText = $"запись команды в устройство, ок!";
                        }

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
                    if (!externalStatus)
                    {
                        //verifiedDevice.Key.StatusTest = StatusDeviceTest.Ok;
                    }

                    verifiedDevice.Key.StatusTest = StatusDeviceTest.Ok;
                    //
                    if (!externalStatus)
                    {
                        SubTestText = $"запись команды в устройство, ок!";
                        PercentCurrentTest = 100;
                        TestRun = TypeOfTestRun.WriteDevicesCmdReady;
                        CurrentCountChecked = string.Empty;
                        TestCurrentDevice = null; //null;
                    }
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

            if (!externalStatus)
            {
                SubTestText = $"запись команды в устройство, ошибка!";
                PercentCurrentTest = 100;
                TestCurrentDevice = device;
                TestRun = TypeOfTestRun.Error;
                ProgressColor = Brushes.Red;
            }
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
            if (!externalStatus)
            {
                SubTestText = $"запись команды в устройство, ошибка!";
                PercentCurrentTest = 100;
                TestRun = TypeOfTestRun.Error;
                ProgressColor = Brushes.Red;
                TestCurrentDevice = null;
            }
            //

            if (resetAll)
            {
                t?.Add(false);
                return deviceReceived;
            }

            throw new Exception(e.Message);
        }
    }

    //--writes--cmds--cms--
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
    /// <param name="externalStatus"></param>
    /// <param name="toList"></param>
    /// <param name="tempChecks"></param>
    /// <param name="isReceiveVal"></param>
    private async Task<Dictionary<BaseDevice, List<string>>> WriteIdentCommands(List<BaseDevice> devices,
        string cmd = null, string paramSet = null, string paramGet = null, int countChecked = 3, TempChecks t = null,
        int loopDelay = 200, CancellationToken token = default, bool externalStatus = false, bool isReceiveVal = false)
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

        //
        if (!externalStatus)
        {
            SubTestText = $"запись команд в устройства";
            PercentCurrentTest = 0;
            TestRun = TypeOfTestRun.WriteDevicesCmd;
            ProgressColor = Brushes.Green;
        }
        //

        try
        {
            for (int i = 1; i <= countChecked; i++)
            {
                try
                {
                    //
                    if (!externalStatus)
                    {
                        SubTestText = $"запись команд в устройства";
                    }

                    if (countChecked > 1)
                    {
                        CurrentCountChecked = $"Попытка: {i.ToString()}/{countChecked}";
                    }
                    //

                    if (i > 1)
                    {
                        //
                        if (!externalStatus)
                        {
                            TestCurrentDevice = tempDevices[0];
                            PercentCurrentTest += Math.Round((1 / (float)devices.Count) * 100 / i);
                        }
                        //

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
                                if (!externalStatus)
                                {
                                    SubTestText = $"ошибка - порт закрыт, переподключение...";
                                }

                                if (!await CheckConnectPort(device, 3, externalStatus: true))
                                {
                                    //
                                    if (!externalStatus)
                                    {
                                        SubTestText = $"порт не открыт, ошибка!";
                                        PercentCurrentTest = 100;
                                        TestCurrentDevice = device;
                                        TestRun = TypeOfTestRun.Error;
                                        ProgressColor = Brushes.Red;
                                    }

                                    //
                                    t?.Add(false);
                                    return deviceReceived;
                                }

                                //
                                if (!externalStatus)
                                {
                                    SubTestText = $"переподключение удачно, порт открыт";
                                }
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
                    PercentCurrentTest += ((1 / (float)devices.Count) * 100);

                    checkDevice.Key.StatusTest = StatusDeviceTest.Ok;
                    // if (!externalStatus)
                    // {
                    //  checkDevice.Key.StatusTest = StatusDeviceTest.Ok;
                    // }

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
                    if (!externalStatus)
                    {
                        //devices.ForEach(x => x.StatusTest = StatusDeviceTest.Ok);
                        SubTestText = $"запись команд в устройства, oк!";
                        PercentCurrentTest = 100;
                        TestRun = TypeOfTestRun.WriteDevicesCmdReady;
                        TestCurrentDevice = null;
                        CurrentCountChecked = string.Empty;
                        ProgressColor = Brushes.Green;
                    }

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
            if (!externalStatus)
            {
                SubTestText = $"запись команд в устройства, ошибка!";
                PercentCurrentTest = 100;
                TestRun = TypeOfTestRun.Error;
                ProgressColor = Brushes.Red;
                TestCurrentDevice = null;
            }

            if (resetAll)
            {
                t?.Add(false);
                return deviceReceived;
            }

            throw new Exception(e.Message);
        }

        if (!externalStatus)
        {
            SubTestText = $"запись команд в устройства, ошибка!";
            TestRun = TypeOfTestRun.Error;
            PercentCurrentTest = 100;
            ProgressColor = Brushes.Red;
            CurrentCountChecked = string.Empty;
        }

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


        //
        ProgressColor = Brushes.RoyalBlue;
        //

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
                $"Ошибка уcтройства \"{device.IsDeviceType}\"/порт заблокирован {device.GetConfigDevice().PortName}";
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
                if (device.CurrentParameter.Contains(receive ?? string.Empty))
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
            if (device.NameCurrentCmd.Contains("Set term"))
            {
                SetParameterGdmDevice(ModeGdm.Themperature);
            }

            else if (device.NameCurrentCmd.Contains("Set curr"))
            {
                SetParameterGdmDevice(ModeGdm.Current, param);
            }

            else if (device.NameCurrentCmd.Contains("Set volt"))
            {
                SetParameterGdmDevice(ModeGdm.Voltage, param);
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
                    if (device is ThermoCurrentMeter)
                    {
                        return (true, param.Contains(vips[0].Type.Parameters.ThermoCurrentValues.ReturnFuncGDM));
                    }

                    if (device is VoltMeter)
                    {
                        return (true, param.Contains(vips[0].Type.Parameters.VoltValues.ReturnFuncGDM));
                    }
                }
                else if (device.NameCurrentCmd.Contains("Get curr"))
                {
                    return (true, param.Contains(vips[0].Type.Parameters.ThermoCurrentValues.ReturnCurrGDM));
                }
                else if (device.NameCurrentCmd.Contains("Get volt"))
                {
                    return (true, param.Contains(vips[0].Type.Parameters.VoltValues.ReturnVoltGDM));
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
    /// <param name="countChecked"></param>
    /// <param name="externalDelay"></param>
    /// <param name="t"></param>
    /// <param name="on">true - вкл, false - выкл</param>
    /// <param name="externalStatus"></param>
    /// <param name="values">Ответ который утройство должно отправить в ответ на запрос output</param>
    /// <returns>Результат включения/выключение</returns>
    private async Task<(BaseDevice outputDevice, bool outputResult)> OutputDevice(BaseDevice device,
        int countChecked = 3, int externalDelay = 200, TempChecks t = null, bool on = true, bool externalStatus = false)
    {
        string getOutputCmdName = "Get output";
        string SetOutputOnCmdName = "Set output on";
        string SetOutputOffCmdName = "Set output off";

        //
        if (!externalStatus)
        {
            PercentCurrentTest = 0;
            ProgressColor = Brushes.Green;
        }
        //

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
        else if (device is RelayVip)
        {
            SetOutputOnCmdName = "On";
            SetOutputOffCmdName = "Off";
        }
        else
        {
            //
            if (!externalStatus)
            {
                // device.StatusTest = string.IsNullOrEmpty(device.ErrorStatus)
                //     ? StatusDeviceTest.Ok
                //     : StatusDeviceTest.Error;
                // device.StatusOnOff = on ? OnOffStatus.On : OnOffStatus.Off;
                device.StatusOnOff = OnOffStatus.None;
            }

            //
            device.StatusTest = string.IsNullOrEmpty(device.ErrorStatus)
                ? StatusDeviceTest.Ok
                : StatusDeviceTest.Error;


            t?.Add(true);
            return (device, true);
        }

        for (int i = 1; i <= countChecked; i++)
        {
            //
            CurrentCountChecked = $"Попытка: {i.ToString()}/{countChecked}";
            //

            if (on)
            {
                //
                if (!externalStatus)
                {
                    SubTestText = $"включение выхода устройства";
                    TestCurrentDevice = device;
                }
                //

                TempChecks tp = TempChecks.Start();

                if (device is RelayVip r)
                {
                    if (device.AllDeviceError.ErrorPort)
                    {
                        await CheckConnectPort(device, 1);
                    }

                    if (!string.IsNullOrEmpty(device.ErrorStatus))
                    {
                        if (!device.ErrorStatus.Contains("неверна") || !device.ErrorStatus.Contains("нет ответа"))
                        {
                            device.StatusTest = StatusDeviceTest.Error;
                            t?.Add(false);
                            return (device, false);
                        }
                    }

                    await WriteIdentCommand(r, SetOutputOnCmdName, countChecked: 2, loopDelay: externalDelay, t: tp,
                        externalStatus: true);

                    if (tp.IsOk)
                    {
                        //
                        if (!externalStatus)
                        {
                            //r.StatusTest = StatusDeviceTest.Ok;
                            r.StatusOnOff = OnOffStatus.On;
                        }

                        //
                        r.StatusTest = StatusDeviceTest.Ok;

                        t?.Add(true);
                        return (device, true);
                    }
                }

                //если не реле випа
                else
                {
                    if (device.AllDeviceError.ErrorPort)
                    {
                        await CheckConnectPort(device, 1);
                    }

                    if (!string.IsNullOrEmpty(device.ErrorStatus))
                    {
                        if (!device.ErrorStatus.Contains("неверна") || !device.ErrorStatus.Contains("нет ответа"))
                        {
                            device.StatusTest = StatusDeviceTest.Error;
                            t?.Add(false);
                            return (device, false);
                        }
                    }

                    var cmdResult = await WriteIdentCommand(device, getOutputCmdName, countChecked: 2, t: tp,
                        externalStatus: true);
                    //если выход вкл 
                    if (cmdResult.Value == getParam.OutputOff && tp.IsOk)
                    {
                        //делаем выход вкл
                        await WriteIdentCommand(device, SetOutputOnCmdName, externalStatus: true);
                        cmdResult = await WriteIdentCommand(device, getOutputCmdName, loopDelay: externalDelay,
                            countChecked: 2, t: tp,
                            externalStatus: true);
                    }

                    if (cmdResult.Value == getParam.OutputOn && tp.IsOk)
                    {
                        //
                        if (!externalStatus)
                        {
                            SubTestText = $"выход устройства включен";
                            PercentCurrentTest = 100;
                            TestCurrentDevice = device;
                            device.StatusOnOff = OnOffStatus.On;
                            //device.StatusTest = StatusDeviceTest.Ok;
                        }

                        //
                        device.StatusTest = StatusDeviceTest.Ok;
                        t?.Add(true);
                        return (device, true);
                    }

                    if (cmdResult.Value != getParam.OutputOn)
                    {
                        device.AllDeviceError.ErrorParam = true;
                        device.ErrorStatus =
                            $"Внимание! Произошла ошибка при выключении выхода уcтройства {device.IsDeviceType}!";
                        //
                        if (!externalStatus)
                        {
                            SubTestText = $"устройство не включено, ошибка";
                            PercentCurrentTest = 100;
                            TestCurrentDevice = device;
                            TestRun = TypeOfTestRun.Error;
                            ProgressColor = Brushes.Red;
                            //device.StatusTest = StatusDeviceTest.Error;
                            device.StatusOnOff = OnOffStatus.None;
                        }

                        device.StatusTest = StatusDeviceTest.Error;
                        //
                        if (i == countChecked)
                        {
                            t?.Add(false);
                            return (device, false);
                        }

                        continue;
                    }

                    if (cmdResult.Value == "Stop tests")
                    {
                        return (device, false);
                    }
                }

                device.StatusTest = StatusDeviceTest.Error;

                if (!tp.IsOk)
                {
                    //
                    if (!externalStatus)
                    {
                        SubTestText = $"выход устройства не включен, ошибка";
                        PercentCurrentTest = 100;
                        TestCurrentDevice = device;
                        TestRun = TypeOfTestRun.Error;
                        ProgressColor = Brushes.Red;
                        device.StatusOnOff = OnOffStatus.None;
                    }
                    //

                    if (i == countChecked)
                    {
                        t?.Add(false);
                        return (device, false);
                    }
                }
            }
            //если не реле випа надо выкл
            else
            {
                //
                if (!externalStatus)
                {
                    SubTestText = $"выключение устройства";
                    TestCurrentDevice = device;
                }
                //


                TempChecks tp = TempChecks.Start();
                //если выключается ерле випа
                if (device is RelayVip r)
                {
                    if (device.AllDeviceError.ErrorPort)
                    {
                        await CheckConnectPort(device, 1);
                    }

                    if (!string.IsNullOrEmpty(device.ErrorStatus))
                    {
                        if (!device.ErrorStatus.Contains("неверна") || !device.ErrorStatus.Contains("нет ответа"))
                        {
                            device.StatusTest = StatusDeviceTest.Error;
                            t?.Add(false);
                            return (device, false);
                        }
                    }

                    try
                    {
                        if (r.StatusOnOff == OnOffStatus.Switching)
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(5500), r.CtsRelayReceive.Token);
                        }
                    }
                    catch (TaskCanceledException e) when (r.CtsRelayReceive.IsCancellationRequested)
                    {
                        r.CtsRelayReceive = new();
                    }

                    await WriteIdentCommand(r, SetOutputOffCmdName, countChecked: 2, loopDelay: externalDelay, t: tp,
                        externalStatus: true);

                    if (tp.IsOk)
                    {
                        if (!externalStatus)
                        {
                            //r.StatusTest = StatusDeviceTest.Ok;
                            r.StatusOnOff = OnOffStatus.Off;
                        }

                        r.StatusTest = StatusDeviceTest.Ok;
                        t?.Add(true);
                        return (device, true);
                    }
                }
                //если выключается не реле випа
                else
                {
                    if (device.AllDeviceError.ErrorPort)
                    {
                        await CheckConnectPort(device, 1);
                    }

                    if
                        (!string.IsNullOrEmpty(device.ErrorStatus))
                    {
                        if (!device.ErrorStatus.Contains("неверна") || !device.ErrorStatus.Contains("нет ответа"))
                        {
                            device.StatusTest = StatusDeviceTest.Error;
                            t?.Add(false);
                            return (device, false);
                        }
                    }

                    var cmdResult = await WriteIdentCommand(device, getOutputCmdName, countChecked: 2, t: tp,
                        externalStatus: true);
                    //если выход вкл 
                    if (cmdResult.Value == getParam.OutputOn && tp.IsOk)
                    {
                        //делаем выход выкл
                        await WriteIdentCommand(device, SetOutputOffCmdName, externalStatus: true);
                        cmdResult = await WriteIdentCommand(device, getOutputCmdName, countChecked: 2,
                            loopDelay: externalDelay, t: tp,
                            externalStatus: true);
                    }

                    if (cmdResult.Value == getParam.OutputOff && tp.IsOk)
                    {
                        //
                        if (!externalStatus)
                        {
                            SubTestText = $"устройство выключено";
                            PercentCurrentTest = 100;
                            TestCurrentDevice = device;
                        }


                        //
                        if (!externalStatus)
                        {
                            device.StatusOnOff = OnOffStatus.Off;
                            //device.StatusTest = StatusDeviceTest.Ok;
                        }

                        device.StatusTest = StatusDeviceTest.Ok;
                        t?.Add(true);
                        return (device, true);
                    }

                    if (cmdResult.Value != getParam.OutputOn)
                    {
                        device.AllDeviceError.ErrorParam = true;
                        device.ErrorStatus =
                            $"Внимание! Произошла ошибка при выключении выхода уcтройства {device.IsDeviceType}!";

                        //
                        if (!externalStatus)
                        {
                            SubTestText = $"устройство не включено, ошибка";
                            PercentCurrentTest = 100;
                            TestCurrentDevice = device;
                            TestRun = TypeOfTestRun.Error;
                            ProgressColor = Brushes.Red;
                            //device.StatusTest = StatusDeviceTest.Error;
                            device.StatusOnOff = OnOffStatus.None;
                        }

                        device.StatusTest = StatusDeviceTest.Error;
                        //
                        if (i == countChecked)
                        {
                            t?.Add(false);
                            return (device, false);
                        }

                        continue;
                    }

                    if (cmdResult.Value == "Stop tests")
                    {
                        return (device, false);
                    }
                }

                if (!tp.IsOk)
                {
                    //
                    if (!externalStatus)
                    {
                        SubTestText = $"устройство не включено, ошибка";
                        PercentCurrentTest = 100;
                        TestCurrentDevice = device;
                        TestRun = TypeOfTestRun.Error;
                        ProgressColor = Brushes.Red;
                        device.StatusTest = StatusDeviceTest.Error;
                        device.StatusOnOff = OnOffStatus.None;
                    }

                    // device.StatusTest = StatusDeviceTest.Error;
                    //
                    if (i == countChecked)
                    {
                        t?.Add(false);
                        return (device, false);
                    }
                }
            }
        }


        //
        if (!externalStatus)
        {
            device.StatusOnOff = OnOffStatus.On;
            //device.StatusTest = StatusDeviceTest.Ok;
        }

        device.StatusTest = StatusDeviceTest.Ok;
        //
        t?.Add(true);
        return (device, true);
    }


    //--ouptup--on--off--вкл--выкл
    /// <summary>
    /// Включение/выключение устройств
    /// </summary>
    /// <param name="device">Устройство</param>
    /// <param name="values">Ответ который утройство должно отправить в ответ на запрос output</param>
    /// <param name="on">true - вкл, false - выкл</param>
    /// <returns>Результат включения/выключение</returns>
    // private async Task<(BaseDevice outputDevice, bool outputResult)> OutputDevices(List<BaseDevice> devices,
    //     TempChecks t = null,
    //     bool on = true)
    // {
    //     var getParam = new BaseDeviceValues("1", "0");
    //
    //     if (device is BigLoad)
    //     {
    //         getParam = GetParameterForDevice().BigLoadValues;
    //     }
    //     else if (device is Supply)
    //     {
    //         getParam = GetParameterForDevice().SupplyValues;
    //     }
    //     // else if (device is Heat)
    //     // {
    //     //     getParam = GetParameterForDevice().HeatValues;
    //     // }
    //
    //     else
    //     {
    //         device.StatusOnOff = on ? OnOffStatus.On : OnOffStatus.Off;
    //         t?.Add(true);
    //         return (device, true);
    //     }
    //
    //     if (on)
    //     {
    //         TempChecks tp = TempChecks.Start();
    //         var cmdResult = await WriteIdentCommand(device, "Get output", t: tp);
    //         //если выход вкл 
    //         if (cmdResult.Value == getParam.OutputOff && tp.IsOk)
    //         {
    //             //делаем выход выкл
    //             await WriteIdentCommand(device, "Set output on");
    //             cmdResult = await WriteIdentCommand(device, "Get output", t: tp);
    //         }
    //
    //         if (cmdResult.Value == getParam.OutputOn && tp.IsOk)
    //         {
    //             device.StatusOnOff = OnOffStatus.On;
    //             t?.Add(true);
    //             return (device, true);
    //         }
    //
    //         if (!tp.IsOk)
    //         {
    //             t?.Add(false);
    //             return (device, false);
    //         }
    //     }
    //     else
    //     {
    //         TempChecks tp = TempChecks.Start();
    //         var cmdResult = await WriteIdentCommand(device, "Get output", t: tp);
    //         //если выход вкл 
    //         if (cmdResult.Value == getParam.OutputOn && tp.IsOk)
    //         {
    //             //делаем выход выкл
    //             await WriteIdentCommand(device, "Set output off");
    //             cmdResult = await WriteIdentCommand(device, "Get output", t: tp);
    //         }
    //
    //         if (cmdResult.Value == getParam.OutputOff && tp.IsOk)
    //         {
    //             device.StatusOnOff = OnOffStatus.Off;
    //             t?.Add(true);
    //             return (device, true);
    //         }
    //
    //         if (!tp.IsOk)
    //         {
    //             t?.Add(false);
    //             return (device, false);
    //         }
    //     }
    //
    //     t?.Add(true);
    //     return (device, true);
    // }

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

    // /// <summary>
    // /// Получение параметров приборов из типа Випа
    // /// </summary>
    // /// <returns>DeviceParameters</returns>
    // T GetParameterForDevice<T>(BaseDevice device) where T : DeviceParameters
    // {
    //     if (device is Supply)
    //     {
    //        return GetParameterForDevice().SupplyValues as T;
    //     }
    //     else if (device is Heat)
    //     {
    //         return GetParameterForDevice().HeatValues as T;
    //     }
    //
    //     return null;
    // }

    /// <summary>
    /// Установка параметров приборов в тип Випа
    /// </summary>
    /// <param name="bdv">Вид прибора в ктороый будут установлены значения</param>
    void SetParameterGdmDevice(ModeGdm mode = ModeGdm.None, string param = null)
    {
        switch (mode)
        {
            case ModeGdm.Themperature:
                vips[0].Type.Parameters.ThermoCurrentValues.Mode = mode;
                vips[0].Type.Parameters.ThermoCurrentValues.CurrMaxLimit = param;
                vips[0].Type.Parameters.ThermoCurrentValues.SetFuncGDM();
                break;
            case ModeGdm.Current:
                vips[0].Type.Parameters.ThermoCurrentValues.Mode = mode;
                vips[0].Type.Parameters.ThermoCurrentValues.CurrMaxLimit = param;
                vips[0].Type.Parameters.ThermoCurrentValues.SetFuncGDM();
                break;
            case ModeGdm.Voltage:
                vips[0].Type.Parameters.VoltValues.Mode = mode;
                vips[0].Type.Parameters.VoltValues.VoltMaxLimit = param;
                vips[0].Type.Parameters.ThermoCurrentValues.SetFuncGDM();
                break;
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
    Temperature
}

public class ResetErrorException : Exception
{
    public ResetErrorException(string s)
    {
    }
}