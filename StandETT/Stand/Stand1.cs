using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using StandETT.SubCore;

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

    private readonly ObservableCollection<BaseDevice> allDevices = new();
    public readonly ReadOnlyObservableCollection<BaseDevice> AllDevices;

    private readonly ObservableCollection<BaseDevice> devices = new();
    public readonly ReadOnlyObservableCollection<BaseDevice> Devices;

    private BaseDevice currentDevice;
    private readonly List<BaseDevice> currentDevices = new();

    BaseDevice mainRelay = MainRelay.getInstance();

    ConfigTypeVip cfgTypeVips = ConfigTypeVip.getInstance();

    public TimeMachine timeMachine = TimeMachine.getInstance();


    private readonly ObservableCollection<Vip> vips = new();
    public readonly ReadOnlyObservableCollection<Vip> Vips;
    private ObservableCollection<BaseDevice> allRelayVips = new();

    #endregion

    //---

    #region --Статусы стенда

    private Brush progressColor;

    public Brush ProgressColor
    {
        get => progressColor;
        set => Set(ref progressColor, value);
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
        allRelayVips = new(createAllDevices.Where(x => x is RelayVip));
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
        CurrentCountChecked = currentCheck == 0 ? string.Empty : $"Попытка: {currentCheck.ToString()}-я";
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

    private double percentStopedTest;

    /// <summary>
    /// На соклько процентов выполнен текущий тест
    /// </summary>
    public double PercentStopedTest
    {
        get => percentStopedTest;
        set => Set(ref percentStopedTest, value);
    }


    //--stop--reset--allreset
    //Остановка всех тестов
    public async Task ResetAllTests()
    {
        resetAll = true;
        ctsAllCancel.Cancel();
        ctsConnectDevice.Cancel();
        ctsReceiveDevice.Cancel();

        TestRun = TypeOfTestRun.Stoped;

        await Task.Delay(TimeSpan.FromMilliseconds(200));

        ctsAllCancel = new CancellationTokenSource();
        ctsConnectDevice = new CancellationTokenSource();
        ctsReceiveDevice = new CancellationTokenSource();
        resetAll = false;

        TempChecks t = TempChecks.Start();

        PercentCurrentTest = 0;
        foreach (var device in devices)
        {
            var resultOutput = await OutputDevice(device, t: t, on: false);

            if (!resultOutput.outputResult)
            {
                ErrorOutput +=
                    $"{resultOutput.outputDevice.IsDeviceType} {device.Name} не был выключен ошибка {device.ErrorStatus}";
            }
            else
            {
                ErrorOutput +=
                    $"{resultOutput.outputDevice.IsDeviceType} {device.Name} был выключен ";

                PercentCurrentTest = ((1 / (float)allDevices.Count) * 100);
            }
        }

        //
        TestRun = TypeOfTestRun.Stop;
        PercentCurrentTest = 0;
        CurrentCountChecked = string.Empty;
        ProgressColor = Brushes.Green;
        TestCurrentDevice = null; //new BaseDevice("");
        //
        // if (ErrorOutput != null)
        // {
        //     throw new ResetErrorException($"{errorOutput}");
        // }
    }

    //--приборы--проверка--check--
    public async Task<bool> PrimaryCheckDevices(int countChecked = 3, int loopDelay = 3000)
    {
        //
        TestRun = TypeOfTestRun.PrimaryCheckDevices;
        //

        TempChecks t = TempChecks.Start();
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
                TestCurrentDevice = null; //new BaseDevice("");
                //
                return true;
            }
        }

        //
        TestRun = TypeOfTestRun.Error;
        PercentCurrentTest = 100;
        ProgressColor = Brushes.Red;
        TestCurrentDevice = null; //new BaseDevice("");
        if (!resetAll)
        {
            throw new Exception("Одно или несколько устройств не ответили или ответили\nс ошибкой. ");
        }

        //
        return false;
    }

    private List<Vip> vipsTested = new();

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

        TempChecks t = TempChecks.Start();
        //предварительная настройка тестрировать ли вип => если у Випа есть имя то тестировать
        vipsTested = GetIsTestedVips();

        await CheckConnectPort(mainRelay, t: t);

        if (t.IsOk)
        {
            t = TempChecks.Start();
            foreach (var vip in vipsTested)
            {
                await WriteIdentCommand(vip.Relay, "Status", countChecked: countChecked, loopDelay: loopDelay, t: t);
            }

            if (t.IsOk)
            {
                TestRun = TypeOfTestRun.PrimaryCheckVipsReady;
                PercentCurrentTest = 100;
                return true;
            }
        }

        //
        TestRun = TypeOfTestRun.Error;
        PercentCurrentTest = 100;
        ProgressColor = Brushes.Red;
        TestCurrentDevice = null; //new BaseDevice("");
        //
        if (!resetAll)
        {
            throw new Exception("Одно или несколько випов не ответили или ответили\nс ошибкой. ");
        }

        return false;
    }

    Stopwatch s = new Stopwatch();

    private bool externalCount = false;

    private async Task<Dictionary<BaseDevice, List<string>>> SetCheckValueInDevice(BaseDevice device, string setCmd,
        string param, int countChecked = 3,
        int loopDelay = 200, TempChecks t = null,
        params string[] getCmds)
    {
        var deviceReceived = new Dictionary<BaseDevice, List<string>>();
        for (int i = 1; i <= countChecked; i++)
        {
            externalCount = true;
            CurrentCountChecked = $"Попытка: {i.ToString()}-я";
            t = TempChecks.Start();
            await WriteIdentCommand(device, setCmd, paramSet: param);
            t.Add(true);

            foreach (var getCmd in getCmds)
            {
                if (t.IsOk)
                {
                    var receive = await WriteIdentCommand(device, getCmd, paramGet: param, countChecked: 1, t: t);

                    if (!deviceReceived.ContainsKey(receive.Key))
                    {
                        //добавляем туда данные и создаем список в ктороые будет записаны ответы от прибора
                        deviceReceived.Add(device, new List<string>());
                    }

                    deviceReceived[receive.Key].Add(receive.Value);
                }
            }

            if (!t.IsOk)
            {
                continue;
            }

            externalCount = false;
            t.Add(true);
            return deviceReceived;
        }

        externalCount = false;
        t.Add(false);

        return deviceReceived;
    }


    //--zero--ноль--
    public async Task<bool> MeasurementZero(int countChecked = 3, int loopDelay = 1000)
    {
        TestRun = TypeOfTestRun.MeasurementZero;
        currentDevice = devices.GetTypeDevice<BigLoad>();
        var getParamLoad = GetParameterForDevice().BigLoadValues;
        var getParamVoltMeter = GetParameterForDevice().VoltValues;
        var getParamThermoCurrentMeter = GetParameterForDevice().ThermoCurrentValues;

        currentDevice = devices.GetTypeDevice<ThermoCurrentMeter>();
        TempChecks t = TempChecks.Start();
        s.Start();

        // await SetCheckValueInDevice(currentDevice, "Set curr meter", getParamThermoCurrentMeter.CurrMaxLimit,
        //     countChecked, loopDelay, t, "Get func", "Get curr meter");

        currentDevice = devices.GetTypeDevice<Supply>();
        await OutputDevice(currentDevice);
        //
        // currentDevice = devices.GetTypeDevice<BigLoad>();
        // await OutputDevice(currentDevice);
        //
        // await Task.Delay(TimeSpan.FromMilliseconds(3000));
        //
        // currentDevice = devices.GetTypeDevice<Supply>();
        // await OutputDevice(currentDevice, on: false);
        //
        // currentDevice = devices.GetTypeDevice<BigLoad>();
        // await OutputDevice(currentDevice, on: false);


        // if (t.IsOk)
        // {
        //     await SetCheckValueInDevice(currentDevice, "Set temp meter", null, countChecked,
        //         loopDelay, t, "Get func");
        //     await SetCheckValueInDevice(currentDevice, "Set tco type", getParamThermoCurrentMeter.TermocoupleType,
        //         countChecked, loopDelay, t, "Get tco type");
        // }

        if (t.IsOk)
            // if (t.IsOk) await WriteIdentCommand(currentDevice, "Set freq", getParamLoad.Freq, t: t);
            // if (t.IsOk) await WriteIdentCommand(currentDevice, "Set ampl", getParamLoad.Ampl, t: t);
            // if (t.IsOk) await WriteIdentCommand(currentDevice, "Set dco", getParamLoad.Dco, t: t);
            // if (t.IsOk) await WriteIdentCommand(currentDevice, "Set squ", getParamLoad.Squ, t: t);

            //
            // await WriteIdentCommand(currentDevice, "Set temp meter");
            // await WriteIdentCommand(currentDevice, "Get func", param: getParamThermoCurrentMeter.ReturnFuncGDM, t: t);
            // //

            currentDevice = devices.GetTypeDevice<VoltMeter>();
        // await WriteIdentCommand(currentDevice, "Set volt meter", param: getParamVoltMeter.VoltMaxLimit);
        // await WriteIdentCommand(currentDevice, "Get func", param: getParamVoltMeter.VoltMaxLimit, t: t);


        // currentDevice = devices.GetTypeDevice<ThermoCurrentMeter>();
        // if (t.IsOk) await WriteIdentCommand(currentDevice, "Set curr meter", getParamVoltMeter.ReturnVoltGDM, t: t);
        // if (t.IsOk) await WriteIdentCommand(currentDevice, "Set volt meter", getParamVoltMeter.ReturnVoltGDM, t: t);
        //
        // await OutputDevice(currentDevice, getParamLoad, t, false);
        //

        //
        // if (t.IsOk) await OutputDevice(currentDevice, getParamLoad, t, true);

        if (t.IsOk)
        {
            TestRun = TypeOfTestRun.MeasurementZeroReady;
            PercentCurrentTest = 100;
            return true;
        }

        //
        TestRun = TypeOfTestRun.Error;
        PercentCurrentTest = 100;
        ProgressColor = Brushes.Red;
        TestCurrentDevice = null; //new BaseDevice("");
        //
        if (!resetAll)
        {
            throw new Exception("Одно или несколько устройств или випов не ответили или ответили с ошибкой.\n");
        }

        return false;
    }


    private List<Vip> GetIsTestedVips()
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
        return testedVips.ToList();
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
        SubTestText = $"проверка порта - {device.GetConfigDevice().PortName}";
        if (!externalStatus)
        {
            PercentCurrentTest = 0;
            TestRun = TypeOfTestRun.CheckPorts;
            ProgressColor = Brushes.RoyalBlue;
        }
        //

        try
        {
            for (int i = 1; i <= countChecked; i++)
            {
                //
                CurrentCountChecked = $"Попытка: {i.ToString()}-я";
                //

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

                PercentCurrentTest = 50;

                device.StatusTest = StatusDeviceTest.None;
                device.ErrorStatus = string.Empty;

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
                SubTestText = $"проверка порта - {device.GetConfigDevice().PortName}, ок!";
                if (!externalStatus)
                {
                    PercentCurrentTest = 100;
                    TestRun = TypeOfTestRun.CheckPortsReady;
                    CurrentCountChecked = string.Empty;
                    TestCurrentDevice = new("");
                }
                //

                t?.Add(true);
                return true;
            }

            device.Close();
            await Task.Delay(TimeSpan.FromMilliseconds(80), ctsAllCancel.Token);

            //
            SubTestText = $"проверка порта - {device.GetConfigDevice().PortName}, ошибка!";
            if (!externalStatus)
            {
                PercentCurrentTest = 100;
                TestRun = TypeOfTestRun.Error;
                ProgressColor = Brushes.Red;
                TestCurrentDevice = new("");
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
            SubTestText = $"проверка порта - {device.GetConfigDevice().PortName}, ошибка!";
            if (!externalStatus)
            {
                PercentCurrentTest = 100;
                TestRun = TypeOfTestRun.Error;
                ProgressColor = Brushes.Red;
                TestCurrentDevice = new("");
            }
            //

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

        SubTestText = $"проверка портов";
        if (!externalStatus)
        {
            //
            TestRun = TypeOfTestRun.CheckPorts;
            ProgressColor = Brushes.RoyalBlue;
            //
        }


        try
        {
            for (int i = 1; i <= countChecked; i++)
            {
                //
                CurrentCountChecked = $"Попытка: {i.ToString()}-я";
                if (!externalStatus)
                {
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
                        device.Start();
                        await Task.Delay(TimeSpan.FromMilliseconds(20), ctsAllCancel.Token);
                        device.DtrEnable();

                        //
                        SubTestText = $"проверка порта - {device.GetConfigDevice().PortName}";
                        if (!externalStatus)
                        {
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
                    continue;
                }

                //
                SubTestText = $"проверка портов, ок!";
                if (!externalStatus)
                {
                    TestRun = TypeOfTestRun.CheckPortsReady;
                    CurrentCountChecked = string.Empty;
                    TestCurrentDevice = null; //new("");
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

            SubTestText = $"проверка портов, ошибка!";
            if (!externalStatus)
            {
                //
                PercentCurrentTest = 100;
                TestRun = TypeOfTestRun.Error;
                ProgressColor = Brushes.Red;
                TestCurrentDevice = null; //new("");
                //
            }

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
            SubTestText = $"проверка портов, ошибка!";
            if (!externalStatus)
            {
                //
                PercentCurrentTest = 100;
                TestRun = TypeOfTestRun.Error;
                ProgressColor = Brushes.Red;
                TestCurrentDevice = null; //new("");
                //
            }

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
    private async Task<KeyValuePair<BaseDevice, string>> WriteIdentCommand(BaseDevice device,
        string nameCmd, string paramSet = null, string paramGet = null, int countChecked = 3, int loopDelay = 200,
        TempChecks t = null, CancellationToken token = default, bool externalStatus = false)
    {
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
        SubTestText = $"запись команды в устройство";
        if (!externalStatus)
        {
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
                //
                CurrentCountChecked = $"Попытка: {i.ToString()}-я";
                //
                try
                {
                    if (i > 1)
                    {
                        ctsReceiveDevice = new CancellationTokenSource();
                        await Task.Delay(TimeSpan.FromMilliseconds(loopDelay), ctsReceiveDevice.Token);
                    }

                    device.StatusTest = StatusDeviceTest.None;
                    device.ErrorStatus = string.Empty;

                    if (!device.PortIsOpen)
                    {
                        SubTestText = $"запись команды в устройство, ошибка - порт закрыт, переподключение...";
                        if (!await CheckConnectPort(device, 3, externalStatus: true))
                        {
                            t?.Add(false);
                            return deviceReceived;
                        }
                    }

                    //установка гет парамтра
                    device.CurrentParameterGet = paramGet;

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
                        SubTestText = $"запись команды в устройство, ок!";
                        return new KeyValuePair<BaseDevice, string>();
                    }
                }

                catch (TaskCanceledException e)
                {
                    ctsReceiveDevice = new CancellationTokenSource();

                    await Task.Delay(TimeSpan.FromMilliseconds(100), ctsAllCancel.Token);

                    //если тест остановлены, немделнно выходим из метода с ошибкой
                    if (resetAll)
                    {
                        deviceReceived = GetReceiveLast(device);
                        t?.Add(false);
                        return deviceReceived;
                    }
                }

                // Проверка устройств
                verifiedDevice = GetVerifiedDevice(device);

                if (verifiedDevice.Value)
                {
                    verifiedDevice.Key.StatusTest = StatusDeviceTest.Ok;

                    //
                    SubTestText = $"запись команды в устройство, ок!";
                    if (!externalStatus)
                    {
                        PercentCurrentTest = 100;
                        TestRun = TypeOfTestRun.WriteDevicesCmdReady;
                        CurrentCountChecked = string.Empty;
                        TestCurrentDevice = null; //new("");
                    }
                    //

                    t?.Add(true);
                    deviceReceived = GetReceiveLast(verifiedDevice.Key);
                    return deviceReceived;
                }
                else
                {
                    verifiedDevice.Key.StatusTest = StatusDeviceTest.Error;

                    TempChecks tp = TempChecks.Start();
                    if (verifiedDevice.Key.AllDeviceError.ErrorDevice ||
                        verifiedDevice.Key.AllDeviceError.ErrorPort)
                    {
                        await CheckConnectPort(verifiedDevice.Key, 1, t: tp);
                    }
                    else if (verifiedDevice.Key.AllDeviceError.ErrorTerminator ||
                             verifiedDevice.Key.AllDeviceError.ErrorReceive ||
                             verifiedDevice.Key.AllDeviceError.ErrorParam ||
                             verifiedDevice.Key.AllDeviceError.ErrorLength ||
                             verifiedDevice.Key.AllDeviceError.ErrorTimeout)
                    {
                        if (verifiedDevice.Key.AllDeviceError.ErrorTimeout)
                        {
                            verifiedDevice.Key.ErrorStatus =
                                $"Ошибка уcтройства \"{verifiedDevice.Key.IsDeviceType}\"/нет ответа";
                        }

                        tp.Add(true);
                    }

                    if (tp.IsOk)
                    {
                        continue;
                    }
                }
            }

            //
            SubTestText = $"запись команды в устройство, ошибка!";
            if (!externalStatus)
            {
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
            SubTestText = $"запись команды в устройство, ошибка!";
            if (!externalStatus)
            {
                PercentCurrentTest = 100;
                TestRun = TypeOfTestRun.Error;
                ProgressColor = Brushes.Red;
                TestCurrentDevice = new("");
            }
            //

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
    private async Task<Dictionary<BaseDevice, List<string>>> WriteIdentCommands(List<BaseDevice> devices, string cmd,
        string paramSet = null, string paramGet = null, int countChecked = 3, TempChecks t = null, int loopDelay = 200,
        CancellationToken token = default, bool externalStatus = false)
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
        SubTestText = $"запись команд в устройства";
        if (!externalStatus)
        {
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
                    SubTestText = $"запись команд в устройства";
                    if (countChecked > 1)
                    {
                        CurrentCountChecked = $"Попытка: {i.ToString()}-я";
                    }

                    if (i > 1)
                    {
                        //
                        if (!externalStatus)
                        {
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

                        if (!device.PortIsOpen)
                        {
                            if (!await CheckConnectPort(device, 3))
                            {
                                t?.Add(false);
                                return deviceReceived;
                            }
                        }

                        device.CurrentParameterGet = paramGet;

                        device.WriteCmd(cmd, paramSet);

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

                catch (TaskCanceledException e)
                {
                    ctsReceiveDevice = new CancellationTokenSource();
                    await Task.Delay(TimeSpan.FromMilliseconds(100), ctsAllCancel.Token);

                    //если тесты остановлены, немделнно выходим из метода с ошибкой
                    if (resetAll)
                    {
                        t?.Add(false);
                        deviceReceived = GetReceives();
                        return deviceReceived;
                    }
                }

                // Проверка устройств
                verifiedDevices = GetVerifiedDevices(tempDevices);

                checkDevices = verifiedDevices.Where(x => x.Value).ToList();

                foreach (var checkDevice in checkDevices)
                {
                    PercentCurrentTest += ((1 / (float)devices.Count) * 100);

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
                        else if (verifiedDevice.Key.AllDeviceError.ErrorTerminator ||
                                 verifiedDevice.Key.AllDeviceError.ErrorReceive ||
                                 verifiedDevice.Key.AllDeviceError.ErrorParam ||
                                 verifiedDevice.Key.AllDeviceError.ErrorLength ||
                                 verifiedDevice.Key.AllDeviceError.ErrorTimeout)
                        {
                            if (verifiedDevice.Key.AllDeviceError.ErrorTimeout)
                            {
                                verifiedDevice.Key.ErrorStatus =
                                    $"Ошибка уcтройства \"{verifiedDevice.Key.IsDeviceType}\"/нет ответа";
                            }
                        }
                        // //TODO сделать чтобы вылетоало на послденем устройстве
                        // if (i == countChecked)
                        // {
                        //     if (verifiedDevices.Last().Key.IsDeviceType == verifiedDevice.Key.IsDeviceType)
                        //     {
                        //         var errorStatuses = string.Empty;
                        //         foreach (var device in verifiedDevices)
                        //         {
                        //             errorStatuses += device.Key.ErrorStatus + "\n";
                        //         }
                        //
                        //         throw new ResetTestException($"{errorStatuses}");
                        //     }
                        // }
                    }

                    if (tp.IsOk)
                    {
                        tempDevices = verifiedDevices.Keys.ToList();
                    }
                }
                else
                {
                    devices.ForEach(x => x.StatusTest = StatusDeviceTest.Ok);

                    //
                    PercentCurrentTest = 100;
                    TestRun = TypeOfTestRun.WriteDevicesCmdReady;
                    TestCurrentDevice = new("");
                    CurrentCountChecked = string.Empty;
                    ProgressColor = Brushes.Green;
                    //

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
            PercentCurrentTest = 100;
            TestRun = TypeOfTestRun.Error;
            ProgressColor = Brushes.Red;
            TestCurrentDevice = new($"");
            //
            throw new Exception(e.Message);
        }

        //
        TestRun = TypeOfTestRun.Error;
        PercentCurrentTest = 100;
        ProgressColor = Brushes.Red;
        CurrentCountChecked = string.Empty;
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

        //
        TestCurrentDevice = device;
        ProgressColor = Brushes.RoyalBlue;
        //

        if (!baseConnects.Any())
        {
            ctsConnectDevice.Cancel();
        }
    }

    private void Device_Error(BaseDevice device, string err)
    {
        Debug.WriteLine($"Ошибка уcтройства {device}/{err}");

        if (err.Contains("Device Error"))
        {
            device.ErrorStatus = $"Ошибка уcтройства \"{device.IsDeviceType}\"/сбой устройства";
            device.AllDeviceError.ErrorDevice = true;

            if (device is MainRelay)
            {
                foreach (var vip in vipsTested)
                {
                    vip.Relay.ErrorStatus = $"Ошибка уcтройства \"{device.Name}\"/сбой устройства";
                    vip.Relay.StatusTest = StatusDeviceTest.Error;
                }
            }
        }

        if (err.Contains("Port not found"))
        {
            device.ErrorStatus =
                $"Ошибка уcтройства \"{device.IsDeviceType}\"/сбой порта {device.GetConfigDevice().PortName}";
            device.AllDeviceError.ErrorPort = true;


            if (device is MainRelay)
            {
                foreach (var vip in vipsTested)
                {
                    vip.Relay.ErrorStatus =
                        $"Ошибка уcтройства \"{device.Name}\"/сбой порта {device.GetConfigDevice().PortName}";
                    vip.Relay.StatusTest = StatusDeviceTest.Error;
                }
            }
        }
    }

    ///Прием ответа от устройства
    Dictionary<BaseDevice, List<string>> receiveInDevice = new();

    private void Device_Receiving(BaseDevice device, string receive, DeviceCmd cmd)
    {
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
            else if (!isGdmMatch.isGdm && receive == device.CurrentParameter)
            {
                device.AllDeviceError.ErrorParam = false;
            }
            else
            {
                device.ErrorStatus =
                    $"Ошибка уcтройства {device.IsDeviceType}, команда {device.NameCurrentCmd}/неверный параметр, пришел \"{receive}\"/ожидался \"{device.CurrentParameter}\"";
                device.AllDeviceError.ErrorParam = true;
            }
        }
        else
        {
            device.AllDeviceError.ErrorReceive = false;
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
        if (device.Name.ToLower().Contains("gdm"))
        {
            if (device.NameCurrentCmd.Contains("Get func"))
            {
                return (true, param.Contains(vips[0].Type.Parameters.ThermoCurrentValues.ReturnFuncGDM));
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

        //TODO спросить у темы порядок дейтсвия исключений пока сотавить так
        return new Dictionary<BaseDevice, List<string>>();

        throw new Exception($"Нет ответов от данных устройств");
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
            //TODO спросить у темы порядок дейтсвия исключений пока сотавить так
            return new Dictionary<BaseDevice, string>();
            throw new Exception(e.Message);
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
            throw new Exception($"Невозмонжо получить ответ от устройства - {device.IsDeviceType}");
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


    //--ouptup--on--off--вкл--выкл
    /// <summary>
    /// Включение/выключение устройства
    /// </summary>
    /// <param name="device">Устройство</param>
    /// <param name="values">Ответ который утройство должно отправить в ответ на запрос output</param>
    /// <param name="on">true - вкл, false - выкл</param>
    /// <returns>Результат включения/выключение</returns>
    private async Task<(BaseDevice outputDevice, bool outputResult)> OutputDevice(BaseDevice device,
        int countChecked = 3, TempChecks t = null,
        bool on = true)
    {
        var getParam = new BaseDeviceValues("1", "0");

        if (device is BigLoad)
        {
            getParam = GetParameterForDevice().BigLoadValues;
        }
        else if (device is Supply)
        {
            getParam = GetParameterForDevice().SupplyValues;
        }
        // else if (device is Heat)
        // {
        //     getParam = GetParameterForDevice().HeatValues;
        // }
        else
        {
            device.StatusOnOff = on ? OnOffStatus.On : OnOffStatus.Off;
            t?.Add(true);
            return (device, true);
        }

        for (int i = 0; i < countChecked; i++)
        {
            if (on)
            {
                TempChecks tp = TempChecks.Start();
                var cmdResult = await WriteIdentCommand(device, "Get output", countChecked: 1, t: tp);
                //если выход вкл 
                if (cmdResult.Value == getParam.OutputOff && tp.IsOk)
                {
                    //делаем выход выкл
                    await WriteIdentCommand(device, "Set output on");
                    cmdResult = await WriteIdentCommand(device, "Get output", countChecked: 1, t: tp);
                }

                if (cmdResult.Value == getParam.OutputOn && tp.IsOk)
                {
                    device.StatusOnOff = OnOffStatus.On;
                    t?.Add(true);
                    return (device, true);
                }

                if (!tp.IsOk)
                {
                    t?.Add(false);
                    return (device, false);
                }
            }
            else
            {
                TempChecks tp = TempChecks.Start();
                var cmdResult = await WriteIdentCommand(device, "Get output", countChecked: 1, t: tp);
                //если выход вкл 
                if (cmdResult.Value == getParam.OutputOn && tp.IsOk)
                {
                    //делаем выход выкл
                    await WriteIdentCommand(device, "Set output off");
                    cmdResult = await WriteIdentCommand(device, "Get output", countChecked: 1, t: tp);
                }

                if (cmdResult.Value == getParam.OutputOff && tp.IsOk)
                {
                    device.StatusOnOff = OnOffStatus.Off;
                    t?.Add(true);
                    return (device, true);
                }

                if (!tp.IsOk)
                {
                    t?.Add(false);
                    return (device, false);
                }
            }
        }


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
        // if (device is BigLoad)
        // {
        //     return (T)param.BigLoadValues;
        // }
        //
        // if (device is Heat)
        // {
        //     return (T)param.HeatValues;
        // }
        //
        // if (device is MainRelay)
        // {
        // }
        //
        // if (device is RelayMeter)
        // {
        // }
        //
        // if (device is RelayVip)
        // {
        // }
        //
        // if (device is SmallLoad)
        // {
        // }
        //
        // if (device is Supply)
        // {
        // }
        //
        // if (device is ThermoCurrentMeter)
        // {
        // }
        //
        // if (device is VoltMeter)
        // {
        // }
    }

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

            if (str.Contains("E+") || str.Contains("e+") || str.Contains("E-") || str.Contains("e-"))
            {
                myDecimalValue = Decimal.Parse(str, System.Globalization.NumberStyles.Float);
                return myDecimalValue.ToString(CultureInfo.InvariantCulture);
            }
        }

        return str;
    }

    (bool matches, string receive) CheckedDeviceOnParameter(BaseDevice device, string receiveInLib,
        string parameter)
    {
        bool matches = false;

        //данные из листа приема от устройств
        var receive = CastToNormalValues(receiveInDevice[device].Last());
        var receiveStr = receive.Last();
        if (receiveInLib != null && !string.IsNullOrEmpty(receiveInLib))
        {
            //проверка листа приема на содержание в ответе от прибора параметра команды -
            //берется из Recieve библиотеки
            matches = receive.Contains(receiveInLib);
        }
        else
        {
            //очистка листа приема от устроойств
            receiveInDevice[device].Clear();
            //проверка листа приема на содержание в ответе от прибора параметра команды
            matches = receive.Contains(parameter);
        }

        return (matches, receive);
    }

    #endregion

    #endregion

    //--
}

public class ResetErrorException : Exception
{
    public ResetErrorException(string s)
    {
    }
}