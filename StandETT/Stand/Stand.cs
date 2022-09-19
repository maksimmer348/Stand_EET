﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace StandETT;

public class Stand1 : Notify
{
    #region --Создание устройств

    /// <summary>
    /// Создание устройств стенда
    /// </summary>
    private CreatorAllDevicesAndLib creatorAllDevicesAndLib;

    #endregion

    //--

    #region Библиотека стенда

    public BaseLibCmd LibCmd = BaseLibCmd.getInstance();

    #endregion

    //---

    #region --Устройства стенда

    private readonly ObservableCollection<BaseDevice> allDevices = new();
    public readonly ReadOnlyObservableCollection<BaseDevice> AllDevices;

    private readonly ObservableCollection<BaseDevice> devices = new();
    public readonly ReadOnlyObservableCollection<BaseDevice> Devices;

    BaseDevice mainRelay = MainRelay.getInstance();

    private readonly ObservableCollection<Vip> vips = new();
    public readonly ReadOnlyObservableCollection<Vip> Vips;
    public ObservableCollection<TypeVip> AllTypeVips { get; set; }

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

    //TODO расконмменить нужный токен
    // CancellationTokenSource ctsPortDevice = new();
    // CancellationTokenSource ctsCheckDevice = new();
    //

    // CancellationTokenSource ctsCmdDevice = new();
    // CancellationTokenSource ctsReceiveRelayVips = new();
    // CancellationTokenSource ctsReceiveRelayMeters = new();

    //TODO изменить на нужный токен
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
        AllTypeVips = creatorAllDevicesAndLib.SetTypeVips();
        //-
        LibCmd.DeviceCommands = creatorAllDevicesAndLib.SetLib();
        //-
        creatorAllDevicesAndLib.PortConnecting += Port_Connecting;
        creatorAllDevicesAndLib.DeviceReceiving += Device_Receiving;
        creatorAllDevicesAndLib.DeviceError += Device_Error;
        //-
        SetStatusStand(0, testRun: TypeOfTestRun.Stop);
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

    #region --Запросы и обработа ответов из устройств

    #region --Порт

    private List<BaseDevice> cancelConnects = new();

    //--checks--ports--conns--
    /// <summary>
    /// Проверка на физическое существование порта (несокльких)
    /// </summary>
    /// <param name="devices"></param>
    /// <param name="countChecked">Колво проверок если не работает</param>
    /// <param name="tempChecks"></param>
    /// <param name="device">Устройство</param>
    /// <param name="externalDelay">Общая задержка проверки (default = 100)</param>
    /// <returns name="errorDevice"></returns>
    async Task<bool> CheckConnectPorts(List<BaseDevice> devices, int countChecked = 5, int loopDelay = 300,
        TempChecks t = null)
    {
        cancelConnects = devices;
        var verifiedDevices = new Dictionary<BaseDevice, bool>();
        try
        {
            for (int i = 1; i <= countChecked; i++)
            {
                //
                TestRun = TypeOfTestRun.CheckPorts;
                PercentCurrentTest = 0;
                CurrentCountChecked = $"Попытка: {i.ToString()}-я";
                ProgressColor = Brushes.Green;
                //
                try
                {
                    if (i > 1)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(loopDelay), ctsConnectDevice.Token);
                    }

                    foreach (var device in devices)
                    {
                        device.Close();
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(80));

                    foreach (var device in devices)
                    {
                        device.Start();
                        await Task.Delay(TimeSpan.FromMilliseconds(20));
                        device.DtrEnable();

                        //
                        TestCurrentDevice = device;
                        PercentCurrentTest += ((1 / (float)devices.Count) * 100);
                        //
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(80), ctsConnectDevice.Token);
                }

                catch (Exception e) when (ctsConnectDevice.IsCancellationRequested)
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
                    //
                    checkDevice.Key.StatusTest = StatusDeviceTest.None;
                    //
                    t?.Add(true);
                    verifiedDevices.Remove(checkDevice.Key);
                }

                if (verifiedDevices.Any())
                {
                    continue;
                }

                //
                PercentCurrentTest = 100;
                ProgressColor = Brushes.Green;
                TestRun = TypeOfTestRun.CheckPortsReady;
                CurrentCountChecked = string.Empty;
                TestCurrentDevice = new("");
                //

                return true;
            }

            foreach (var verifiedDevice in verifiedDevices)
            {
                t?.Add(false);
                //
                verifiedDevice.Key.StatusTest = StatusDeviceTest.Error;
                //
                verifiedDevice.Key.Close();
            }

            await Task.Delay(TimeSpan.FromMilliseconds(80));

            //
            TestRun = TypeOfTestRun.Error;
            PercentCurrentTest = 100;
            CurrentCountChecked = string.Empty;
            ProgressColor = Brushes.Red;
            //

            return false;
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }


    //--check--port--conn--
    /// <summary>
    /// Проверка на физическое существование порта (одичночного)
    /// </summary>
    /// <param name="device">Устройство</param>
    /// <param name="externalDelay">Общая задержка проверки (default = 100)</param>
    /// <param name="countChecks">Колво проверок если не работает</param>
    /// <returns name="errorDevice"></returns>
    async Task<bool> CheckConnectPort(BaseDevice device, int countChecked = 5, int loopDelay = 300, TempChecks t = null)
    {
        cancelConnects = new() { device };
        try
        {
            for (int i = 1; i <= countChecked; i++)
            {
                //
                TestRun = TypeOfTestRun.CheckPorts;
                PercentCurrentTest = 0;
                TestCurrentDevice = device;
                CurrentCountChecked = $"Попытка: {i.ToString()}-я";
                ProgressColor = Brushes.Green;
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
                    catch (Exception e)
                    {
                        ctsConnectDevice = new CancellationTokenSource();
                    }
                }

                device.Close();
                await Task.Delay(TimeSpan.FromMilliseconds(80));
                device.Start();
                await Task.Delay(TimeSpan.FromMilliseconds(20));
                device.DtrEnable();
                await Task.Delay(TimeSpan.FromMilliseconds(80));

                //
                PercentCurrentTest = 50;
                //

                var verifiedDevice = GetVerifiedPort(device);

                if (!verifiedDevice.Value)
                {
                    continue;
                }

                //
                TestRun = TypeOfTestRun.CheckPortsReady;
                PercentCurrentTest = 100;
                ProgressColor = Brushes.Green;
                CurrentCountChecked = string.Empty;
                TestCurrentDevice = new("");
                //

                device.StatusTest = StatusDeviceTest.None;
                t?.Add(true);
                return true;
            }

            device.Close();
            await Task.Delay(TimeSpan.FromMilliseconds(80));

            //
            TestRun = TypeOfTestRun.Error;
            PercentCurrentTest = 100;
            ProgressColor = Brushes.Red;
            //

            device.StatusTest = StatusDeviceTest.Error;
            t?.Add(false);
            return false;
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

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

    /// <summary>
    /// Получение нерабочего устройства
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

    #endregion


    #region Команды

    private List<BaseDevice> cancelDevices = new();

    //--Writes--
    /// <summary>
    /// Запись одинаковых команд с проверкой
    /// </summary>
    /// <param name="toList"></param>
    /// <param name="cmd"></param>
    /// <param name="param"></param>
    /// <param name="countChecked"></param>
    /// <param name="tempChecks"></param>
    /// <param name="loopDelay"></param>
    private async Task<Dictionary<BaseDevice, List<string>>> WriteIdentCommands(List<BaseDevice> devices, string cmd,
        string param = null, int countChecked = 3, TempChecks t = null, int loopDelay = 0,
        CancellationToken token = default)
    {
        cancelDevices = devices;
        var deviceReceived = new Dictionary<BaseDevice, List<string>>();

        var verifiedDevices = new Dictionary<BaseDevice, bool>();
        var tempDevices = devices;
        var checkDevices = new List<KeyValuePair<BaseDevice, bool>>();

        var isRelay = false;
        //список задержек
        var delays = new List<int>(tempDevices.Count);

        //удаление прердыдущих ответов от устройств
        RemoveReceives(devices);

        try
        {
            for (int i = 1; i <= countChecked; i++)
            {
                try
                {
                    //
                    PercentCurrentTest = 0;
                    TestRun = TypeOfTestRun.WriteDevicesCmd;
                    CurrentCountChecked = $"Попытка: {i.ToString()}-я";
                    ProgressColor = Brushes.Blue;
                    //

                    if (i > 1)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(loopDelay), ctsReceiveDevice.Token);
                    }

                    // запись команды в каждое устройсттво
                    foreach (var device in tempDevices)
                    {
                        //если прибор от кторого приходят данные не содержится в библиотеке а при первом приеме данных так и будет
                        if (!receiveInDevice.ContainsKey(device))
                        {
                            //добавляем туда данные и создаем список в ктороые будет записаны ответы от прибора
                            receiveInDevice.Add(device, new List<string>());
                        }

                        device.WriteCmd(cmd, param);

                        delays.Add(device.CurrentCmd?.Delay ?? 200);

                        //
                        TestCurrentDevice = device;
                        PercentCurrentTest += ((1 / (float)devices.Count) * 50);
                        //
                        if (device is RelayVip)
                        {
                            isRelay = true;
                            await Task.Delay(TimeSpan.FromMilliseconds(delays.Max()), ctsReceiveDevice.Token);
                        }
                    }

                    if (!isRelay)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(delays.Max()), ctsReceiveDevice.Token);
                    }
                }

                catch (Exception e) when (ctsReceiveDevice.IsCancellationRequested)
                {
                    ctsReceiveDevice = new CancellationTokenSource();
                    await Task.Delay(TimeSpan.FromMilliseconds(100));

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
                    checkDevice.Key.StatusTest = StatusDeviceTest.Ok;
                    t?.Add(true);
                    verifiedDevices.Remove(checkDevice.Key);
                }

                if (verifiedDevices.Any())
                {
                    TempChecks tp = TempChecks.Start();
                    foreach (var verifiedDevice in verifiedDevices)
                    {
                        //
                        verifiedDevice.Key.StatusTest = StatusDeviceTest.Error;
                        //

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
                            else if (verifiedDevice.Key.AllDeviceError.ErrorReceive)
                            {
                                verifiedDevice.Key.ErrorStatus =
                                    $"Ошибка уcтройства \"{verifiedDevice.Key.IsDeviceType}\"/неверный ответ";
                            }

                            tp.Add(true);
                        }
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
        catch (Exception e)
        {
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

    //--Write--
    /// <summary>
    /// Запись одинаковых команд с проверкой по очереди
    /// </summary>
    /// <param name="device"></param>
    /// <param name="cmd"></param>
    /// <param name="param"></param>
    /// <param name="countChecked"></param>
    /// <param name="loopDelay"></param>
    /// <param name="t"></param>
    /// <param name="token"></param>
    /// <param name="toList"></param>
    /// <param name="tempChecks"></param>
    private async Task<KeyValuePair<BaseDevice, string>> WriteIdentCommand(BaseDevice device,
        string cmd,
        string param = null, int countChecked = 3, int loopDelay = 0, TempChecks t = null,
        CancellationToken token = default)
    {
        cancelDevices = new() { device };
        RemoveReceive(device);
        KeyValuePair<BaseDevice, bool> verifiedDevice = default;
        KeyValuePair<BaseDevice, string> deviceReceived;

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
                PercentCurrentTest = 0;
                TestRun = TypeOfTestRun.WriteDevicesCmd;
                CurrentCountChecked = $"Попытка: {i.ToString()}-я";
                ProgressColor = Brushes.Blue;
                //

                try
                {
                    if (i > 1)
                    {
                        //TODO изменить на нужный токен
                        await Task.Delay(TimeSpan.FromMilliseconds(loopDelay), ctsReceiveDevice.Token);
                    }

                    // запись команды в каждое устройсттво

                    //
                    device.StatusTest = StatusDeviceTest.None;
                    //

                    device.WriteCmd(cmd, param);

                    //
                    TestCurrentDevice = device;
                    PercentCurrentTest = 50;
                    //

                    //TODO изменить на нужный токен
                    await Task.Delay(TimeSpan.FromMilliseconds(device.CurrentCmd?.Delay ?? 200),
                        ctsReceiveDevice.Token);
                }

                catch (Exception e) when (ctsReceiveDevice.IsCancellationRequested)
                {
                    ctsReceiveDevice = new CancellationTokenSource();

                    await Task.Delay(TimeSpan.FromMilliseconds(100));

                    //если тесты остановлены, немделнно выходим из метода с ошибкой
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
                    t?.Add(true);

                    //
                    verifiedDevice.Key.StatusTest = StatusDeviceTest.Ok;
                    PercentCurrentTest = 100;
                    TestRun = TypeOfTestRun.WriteDevicesCmdReady;
                    CurrentCountChecked = string.Empty;
                    TestCurrentDevice = new("");
                    //
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

                        else if (verifiedDevice.Key.AllDeviceError.ErrorReceive)
                        {
                            verifiedDevice.Key.ErrorStatus =
                                $"Ошибка уcтройства \"{verifiedDevice.Key.IsDeviceType}\"/неверный ответ";
                        }

                        tp.Add(true);
                    }

                    if (tp.IsOk)
                    {
                        continue;
                    }
                }
            }

            deviceReceived = GetReceiveLast(device);

            t?.Add(false);
            return deviceReceived;
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

    #region Работа с библиотекой ответов

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
            //receiveInDevice[device] = new List<string>();
        }
        catch (Exception e)
        {
            //TODO спросить у темы порядок дейтсвия исключений пока сотавить так
            return;
            //throw new Exception("Невозмонжо получить ответ от данного устройства, поэтому нельзя удалить");
        }
    }

    #endregion

    //-

    #endregion

    //--

    #endregion


    //---

    #region --Проверки--Тесты

    private bool resetAll = false;

    //Остановка всех тестов
    public async Task ResetAllTests()
    {
        ctsAllCancel.Cancel();
        ctsConnectDevice.Cancel();
        ctsReceiveDevice.Cancel();
        resetAll = true;

        TestRun = TypeOfTestRun.Stoped;

        await Task.Delay(TimeSpan.FromMilliseconds(200));

        //
        TestRun = TypeOfTestRun.Stop;
        PercentCurrentTest = 0;
        CurrentCountChecked = string.Empty;
        ProgressColor = Brushes.Green;
        //

        ctsAllCancel = new CancellationTokenSource();
        ctsConnectDevice = new CancellationTokenSource();
        ctsReceiveDevice = new CancellationTokenSource();
        resetAll = false;
    }

    //--приборы
    public async Task<bool> PrimaryCheckDevices(int countChecked = 3, int loopDelay = 3000)
    {
        //
        TestRun = TypeOfTestRun.PrimaryCheckDevices;
        ProgressColor = Brushes.Green;
        //


        TempChecks t = TempChecks.Start();
        await CheckConnectPorts(devices.ToList(), t: t);
        if (t.IsOk)
        {
            t = TempChecks.Start();
            var re = await WriteIdentCommands(devices.ToList(), "Status", countChecked: countChecked, t: t,
                loopDelay: loopDelay);
            //await WriteIdentCommand(devices[0], "Status", countChecked: 3, loopDelay: 3000, t: t);
            if (t.IsOk)
            {
                TestRun = TypeOfTestRun.PrimaryCheckDevicesReady;
                PercentCurrentTest = 100;
                TestCurrentDevice = new BaseDevice("");
                //
                return true;
            }
        }

        //
        TestRun = TypeOfTestRun.Error;
        PercentCurrentTest = 100;
        ProgressColor = Brushes.Red;
        TestCurrentDevice = new BaseDevice("");
        //

        return false;
    }

    //--випы
    public async Task PrimaryCheckVips(int countChecked = 3, int loopDelay = 3000)
    {
        //установка тест первичный платок випов 
        TestRun = TypeOfTestRun.PrimaryCheckVips;

        TempChecks t = TempChecks.Start();
        var relaysTested = GetIsTestedVips();
        ((MainRelay)mainRelay).Relays = relaysTested.Where(x => x is RelayVip).ToList();
        await CheckConnectPort(mainRelay, t: t);

        if (t.IsOk)
        {
            t = TempChecks.Start();
            //предварительная настройка тестрировать ли вип => если у Випа есть имя то тестировать
            // var re = await WriteIdentCommands(relaysTested, "Status", countChecked: countChecked, t: t,
            //     loopDelay: loopDelay);
            foreach (var relay in relaysTested)
            {
                PercentCurrentTest += ((1 / (float)devices.Count) * 100);
                //
                await WriteIdentCommand(relay, "Status", countChecked: 3, loopDelay: 3000, t: t);
            }
        }
    }

    private List<BaseDevice> GetIsTestedVips()
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

        var testedRelays = allRelayVips.Where(x => ((RelayVip)x).IsTested).ToList();
        return testedRelays;
    }

    #endregion

    //---

    #region Обработка событий с приборов

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

        var baseConnects = cancelConnects.Except(connectDevice.Keys).ToList();
        
        //
        TestCurrentDevice = device;
        PercentCurrentTest += ((1 / (float)cancelConnects.Count) * 100);
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
        }

        if (err.Contains("Port not found"))
        {
            device.ErrorStatus =
                $"Ошибка уcтройства \"{device.IsDeviceType}\"/сбой порта {device.GetConfigDevice().PortName}";
            device.AllDeviceError.ErrorPort = true;
        }
    }

    ///Прием ответа от устройства
    Dictionary<BaseDevice, List<string>> receiveInDevice = new();

    private void Device_Receiving(BaseDevice device, string receive, DeviceCmd cmd)
    {
        // //если прибор от кторого приходят данные не содержится в библиотеке а при первом приеме данных так и будет
        // if (!receiveInDevice.ContainsKey(device))
        // {
        //     //добавляем туда данные и создаем список в ктороые будет записаны ответы от прибора
        //     receiveInDevice.Add(device, new List<string>());
        // }

        //далее в библиотеку текущего приобра пишем данные

        receiveInDevice[device].Add(receive);
        device.AllDeviceError.ErrorTimeout = false;

        //если в команду из билиотекие влючен ответ RX
        if (!string.IsNullOrEmpty(cmd.Receive))
        {
            if (!receive.Contains(cmd.Receive.ToLower()))
            {
                device.AllDeviceError.ErrorReceive = true;
                device.ErrorStatus = $"Ошибка уcтройства {device.IsDeviceType}/неверный ответ";
            }

            if (receive.Contains(cmd.Receive.ToLower()))
            {
                device.AllDeviceError.ErrorReceive = false;
            }
        }

        //если в команду из билиотекие влючен терминатор RX
        if (!string.IsNullOrEmpty(cmd.Terminator.ReceiveTerminator))
        {
            var terminatorLenght = cmd.Terminator.ReceiveTerminator.Length;
            var terminator = receive.Substring(receive.Length - terminatorLenght, terminatorLenght);
            if (terminator != cmd.Terminator.ReceiveTerminator)
            {
                device.AllDeviceError.ErrorTerminator = true;
                device.ErrorStatus = $"Ошибка уcтройства {device.IsDeviceType}/неверный терминатор";
            }

            if (terminator == cmd.Terminator.ReceiveTerminator)
            {
                device.AllDeviceError.ErrorTerminator = false;
            }
        }

        if (!string.IsNullOrEmpty(cmd.Length))
        {
            var receiveLenght = Convert.ToInt32(cmd.Length);
            if (receive.Length != receiveLenght)
            {
                device.AllDeviceError.ErrorLength = true;
                device.ErrorStatus = $"Ошибка уcтройства {device.IsDeviceType}/неверная длина сообщения";
            }

            if (receive.Length == receiveLenght)
            {
                device.AllDeviceError.ErrorLength = false;
            }
        }
        //TODO добавить проверку параметаров
        // //если из устройство нужно получить каойто параметр
        // if (!string.IsNullOrEmpty(device.CurrentParameter))
        // {
        //     CastToNormalValues(receive);
        //
        //     var resultGdm = CheckGdm(device, cmd.Transmit, device.CurrentParameter, cmd.Receive?.ToLower());
        //
        //     if (!resultGdm.isGDM && !resultGdm.result)
        //     {
        //         device.AllDeviceError.ErrorParam = true;
        //         device.ErrorStatus =
        //             $"Ошибка уcтройства {device.Name}, неверный параметр {resultGdm.receive}, ожидаемый {device.CurrentParameter}";
        //     }
        // else
        // {
        //     CheckedsDeviceOnParanmeter(device, receiveInLib, matches, parameter, tempChecks);
        //     if (!receive.Contains(cmd.Receive))
        //     {
        //         device.AllDeviceError.ErrorParam = true;
        //         device.ErrorStatus = $"Ошибка уcтройства {device.Name}, неверный параметр";
        //     }
        // }

        // receiveInDevice[device].Last() = receive;
        // }

        // var receivingDelayReset = receiveInDevice.Keys.ToList().Except(deviceIdentCmd);
        //
        // if (receivingDelayReset.Any())
        // {
        //     ctsCheckDevice.Cancel();
        // }

        var baseDevices = cancelDevices.Except(receiveInDevice.Keys).ToList();

        //
        TestCurrentDevice = device;
        PercentCurrentTest += ((1 / (float)receiveInDevice.Count) * 100);
        ProgressColor = Brushes.RoyalBlue;
        //
        
        if (!baseDevices.Any())
        {
            ctsReceiveDevice.Cancel();
        }
    }

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
    /// <param name="bdv">Вид прибора в ктороый будут установлены значения</param>
    void SetParameterThermoCurrent(ModeGdm mode = ModeGdm.None)
    {
        foreach (var vip in vips)
        {
            vip.Type.Parameters.ThermoCurrentValues.Mode = mode;
        }
    }

    /// <summary>
    /// Установка параметров приборов в тип Випа
    /// </summary>
    /// <param name="bdv">Вид прибора в ктороый будут установлены значения</param>
    void SetParameterVolt(ModeGdm mode = ModeGdm.None)
    {
        foreach (var vip in vips)
        {
            vip.Type.Parameters.VoltValues.Mode = mode;
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


    /// <summary>
    /// Проверка GDM устройств (см. документацию)
    /// </summary>
    /// <param name="device">Проверяемо утсройство</param>
    /// <param name="cmd">Трансимт команды в кторой содержится нужна последовательность</param>
    /// <param name="param"></param>
    /// <param name="receiveInLib">Шаблонный ответ из бибилиотеки</param>
    /// <param name="matches"></param>
    /// <param name="tempChecks">Результат проверка листа приема на соответвие параметру из Типа Випа</param>
    /// <returns>Item1 - соответвует ли device GDM устройству
    /// Item2 - Прошла ли проверка устройства согласно алгоритму</returns>
    (bool isGDM, bool result, string receive) CheckGdm(BaseDevice device, string cmd, string param,
        string receiveInLib)
    {
        (bool result, string receive) matchesGdm = (false, null);
        //установка параметров для проверки GDM термо/вольтметра см. документацию GDM-8255A
        if (device is ThermoCurrentMeter t)
        {
            if (t.Name.ToLower().Contains("gdm"))
            {
                if (cmd.Contains("Get term"))
                {
                    SetParameterThermoCurrent(ModeGdm.Themperature);

                    matchesGdm = CheckedDeviceOnParameter(device, receiveInLib,
                        GetParameterForDevice().ThermoCurrentValues.ReturnFuncGDM);

                    return (true, matchesGdm.result, matchesGdm.receive);
                }

                if (cmd.Contains("Get curr"))
                {
                    SetParameterThermoCurrent(ModeGdm.Current);

                    matchesGdm = CheckedDeviceOnParameter(device, receiveInLib,
                        GetParameterForDevice().ThermoCurrentValues.ReturnCurrGDM);

                    return (true, matchesGdm.result, matchesGdm.receive);
                }

                if (cmd.Contains("Get func"))
                {
                    matchesGdm = CheckedDeviceOnParameter(device, receiveInLib,
                        GetParameterForDevice().ThermoCurrentValues.ReturnFuncGDM);
                    return (true, matchesGdm.result, matchesGdm.receive);
                }
            }
        }

        if (device is VoltMeter v)
        {
            if (v.Name.ToLower().Contains("gdm"))
            {
                if (cmd.Contains("Get volt"))
                {
                    SetParameterVolt(ModeGdm.Voltage);

                    matchesGdm = CheckedDeviceOnParameter(device, receiveInLib,
                        GetParameterForDevice().VoltValues.ReturnVoltGDM);

                    return (true, matchesGdm.result, matchesGdm.receive);
                }

                if (cmd.Contains("Get func"))
                {
                    matchesGdm = CheckedDeviceOnParameter(device, receiveInLib,
                        GetParameterForDevice().VoltValues.ReturnFuncGDM);

                    return (true, matchesGdm.result, matchesGdm.receive);
                }
            }
        }

        return (false, false, null);
    }

    #endregion

    #endregion

    //--
}

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

// class VipsCreator
//{

//    public void SetPrepareVips()
//    {
//        VipsPrepareStand = new();
//        VipsPrepareStand.Add(new Vip(1)
//        {
//            RowIndex = 0,
//            ColumnIndex = 0,
//        });
//        VipsPrepareStand.Add(new Vip(2)
//        {
//            RowIndex = 0,
//            ColumnIndex = 1
//        });
//        VipsPrepareStand.Add(new Vip(3)
//        {
//            RowIndex = 0,
//            ColumnIndex = 2
//        });
//        VipsPrepareStand.Add(new Vip(4)
//        {
//            RowIndex = 0,
//            ColumnIndex = 3
//        });
//        VipsPrepareStand.Add(new Vip(5)
//        {
//            RowIndex = 1,
//            ColumnIndex = 0
//        });
//        VipsPrepareStand.Add(new Vip(6)
//        {
//            RowIndex = 1,
//            ColumnIndex = 1
//        });
//        VipsPrepareStand.Add(new Vip(7)
//        {
//            RowIndex = 1,
//            ColumnIndex = 2
//        });
//        VipsPrepareStand.Add(new Vip(8)
//        {
//            RowIndex = 1,
//            ColumnIndex = 3
//        });
//        VipsPrepareStand.Add(new Vip(9)
//        {
//            RowIndex = 2,
//            ColumnIndex = 0
//        });
//        VipsPrepareStand.Add(new Vip(10)
//        {
//            RowIndex = 2,
//            ColumnIndex = 1
//        });
//        VipsPrepareStand.Add(new Vip(11)
//        {
//            RowIndex = 2,
//            ColumnIndex = 2
//        });
//        VipsPrepareStand.Add(new Vip(12)
//        {
//            RowIndex = 2,
//            ColumnIndex = 3
//        });

//        //Добавляем предустановленные типы випов

//        //TODO Serialize comment
//        //ConfigVip.PrepareAddTypeVips();
//        //TODO Serialize comment

//        TypeVips = ConfigVip.TypeVips;
//    }
//}