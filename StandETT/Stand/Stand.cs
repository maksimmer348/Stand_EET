using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StandETT;

public class Stand1 : Notify
{
    #region --Модули проверок и испытаний стенда

    /// <summary>
    /// Создание устройств стенда
    /// </summary>
    private DeviceAndLibCreator deviceAndLibCreator;

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

    private readonly ObservableCollection<Vip> vips = new();
    public readonly ReadOnlyObservableCollection<Vip> Vips;

    #endregion

    //---

    #region --Статусы стенда

    public TypeOfTestRun testRun;

    /// <summary>
    /// Какой сейчас тест идет
    /// </summary>
    public TypeOfTestRun TestRun
    {
        get => testRun;
        set => Set(ref testRun, value);
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

    private double countTimes;

    /// <summary>
    /// Попытко поклдючения
    /// </summary>
    public double CountTimes
    {
        get => countTimes;
        set => Set(ref countTimes, value);
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

    CancellationTokenSource ctsPortDevice = new();
    CancellationTokenSource ctsCheckDevice = new();

    CancellationTokenSource ctsReceiveDevice = new();
    CancellationTokenSource ctsCmdDevice = new();
    CancellationTokenSource ctsReceiveRelayVips = new();
    CancellationTokenSource ctsReceiveRelayMeters = new();

    #endregion

    //---

    #region --Конструктор --ctor

    public Stand1()
    {
        deviceAndLibCreator = new DeviceAndLibCreator(this);

        allDevices = new ObservableCollection<BaseDevice>(deviceAndLibCreator.SetDevices());
        AllDevices = new ReadOnlyObservableCollection<BaseDevice>(allDevices);

        devices = new ObservableCollection<BaseDevice>(allDevices.Where(d => d is not MainRelay));
        Devices = new ReadOnlyObservableCollection<BaseDevice>(devices);

        LibCmd.DeviceCommands = deviceAndLibCreator.SetLib();

        CheckPort += OnCheckConnectPort;
        CheckDevice += OnCheckDevice;
        Receive += OnReceive;

        SetStatusStand();
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
    public void SetStatusStand(int percent = 0, TypeOfTestRun testRun = TypeOfTestRun.Stop, BaseDevice device = null)
    {
        PercentCurrentTest = percent;
        TestRun = testRun;
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
        //vip.StatusOnOff = status;
    }

    #endregion

    #region Запросы и обработа ответов из устройств

    #region Порт

    //--Событие порта

    /// <summary>
    /// Временное хранилище проверенных (работоспособных) приборов
    /// </summary>
    private List<BaseDevice> verifiedDevices = new();

    /// <summary>
    /// Событие поверки порта на коннект 
    /// </summary>/// <param name="baseDevice"></param>
    /// <param name="connect"></param>
    void OnCheckConnectPort(BaseDevice device, bool connect)
    {
        //если есть коннект 
        if (connect)
        {
            //добавляем в проверенные приборы
            verifiedDevices.Add(device);
            //сброс токена порта
            ctsPortDevice.Cancel();
        }
    }

    //--Событие порта

    /// <summary>
    /// Проверка на физические существования портов (нескольких)
    /// </summary>
    /// <param name="checkDevices">Временный списко устройств</param>
    /// <param name="delay">Общая задержка проверки (default = 100)</param>
    /// <returns name="errorDevices">Приборы с ошибкой</returns>
    async Task<List<BaseDevice>> CheckConnectPorts(List<BaseDevice> checkDevices,
        int delay = 100)
    {
        var errorDevice = new List<BaseDevice>();
        try
        {
            //пытаемся вытащить из списка проверямых приборов маинреле
            var mainRelay = checkDevices.GetTypeDevice<MainRelay>();
            //если удачно
            if (checkDevices.GetTypeDevice<MainRelay>() != null)
            {
                //используя одиночную проверку порта проверяем маинреле
                var error = await CheckConnectPort(mainRelay, delay);
                if (!error)
                {
                    //если проверка успешна вернем пустой список тк сбойных приборов нет
                    return new List<BaseDevice>();
                }

                //если проверка не успешна после задержки в этом списке будет маин реле не прошедшее проверку
                errorDevice = checkDevices;
                return errorDevice;
            }

            foreach (var device in checkDevices)
            {
                device.Close();
            }

            await Task.Delay(TimeSpan.FromMilliseconds(delay));

            foreach (var device in checkDevices)
            {
                device.Start();
            }

            await Task.Delay(TimeSpan.FromMilliseconds(delay));

            //после задержки в этом списке будут устройства не прошедшие проверку
            var errorDevices = GetErrorDevices(checkDevices);
            return errorDevices;
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

    /// <summary>
    /// Проверка на физическое существование порта (одичночного)
    /// </summary>
    /// <param name="tempCheckDevice">Устройство</param>
    /// <param name="externalDelay">Общая задержка проверки (default = 100)</param>
    /// <returns name="errorDevice">Ошибка проверки</returns>
    async Task<bool> CheckConnectPort(BaseDevice tempCheckDevice,
        int externalDelay = 0)
    {
        try
        {
            tempCheckDevice.Close();
            await Task.Delay(TimeSpan.FromMilliseconds(externalDelay));
            tempCheckDevice.Start();
            await Task.Delay(TimeSpan.FromMilliseconds(externalDelay));
            //получим нерабочее устройство
            var errorDevice = GetErrorDevice(tempCheckDevice);
            //
            return errorDevice;
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

    /// <summary>
    /// Получение списка нерабочих устройств
    /// </summary>
    /// <param name="devices">Временный список устройств</param>
    /// <returns></returns>
    private List<BaseDevice> GetErrorDevices(List<BaseDevice> devices)
    {
        try
        {
            //если проверенных устройств вообще нет
            if (!verifiedDevices.Any())
            {
                //вернем список не ответивших приборов целиком
                return devices.ToList();
            }

            //сравниваем входящий список проверяемых приборов со списком сформировванным из ответивших приборов, разницу
            //кладем в список сбойных устройств
            var errorDevices = devices.Except(verifiedDevices).ToList();

            //очистка списка рабочих устройств
            verifiedDevices.Clear();

            //возвращаем список приборов не прошедших проверку
            return errorDevices;
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

    /// <summary>
    /// Получение нерабочего устройства
    /// </summary>
    /// <param name="checkDevices">Временный список устройств</param>
    /// <returns></returns>
    private bool GetErrorDevice(BaseDevice device)
    {
        try
        {
            var errorDevice = verifiedDevices.Contains(device);
            verifiedDevices.Clear();
            return !errorDevice;
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

    #endregion

    //--

    #region --Проверка на команду

    //--Событие команды коннект

    /// <summary>
    /// Событие проверки устройства на коннект
    /// </summary>
    /// <param name="baseDevice"></param>
    /// <param name="connect"></param>
    private void OnCheckDevice(BaseDevice device, bool connect, string receive = null)
    {
        //для вьюмодели

        //если коннект есть добавляем в список годных, текущее устройство
        if (connect)
        {
            verifiedDevices.Add(device);
        }

        if (device is not Vip)
        {
            //если список проверяемых устройств будет равен списку всех утсройст этого типа
            var isCheck = Devices.Except(verifiedDevices).ToList();

            //сбрасываем задержку тк все приборы ответили
            if (!isCheck.Any())
            {
                ctsCheckDevice.Cancel();
            }
        }
    }

    //--Событие команды коннект

    /// <summary>
    /// Проверка устройств пингуются ли они 
    /// </summary>
    /// <param name="devices">Временный списко устройств</param>
    /// <param name="token">Сброс вермени ожидания если прибор ответил раньше</param>
    /// <param name="externalDelay">Общая задержка проверки (default = 0, если 0 исопльзуется маисмаьная задержка устройств)</param>
    /// <param name="cmd">Если команда пустая исопльзуется (default "Status")</param>
    /// <returns></returns>
    async Task<List<BaseDevice>> CheckConnectDevices(List<BaseDevice> devices,
        int externalDelay = 0, string cmd = null, CancellationToken token = default)
    {
        DeviceCmd dataInLib = null;
        //сброс временного списка дефетктивынх приборов
        List<BaseDevice> errorDevices = new List<BaseDevice>();
        //список задержек
        List<int> delays = new List<int>(devices.Count);
        try
        {
            foreach (var device in devices)
            {
                if (string.IsNullOrEmpty(cmd))
                {
                    var result = await CheckConnectDevice(device, cmd: "Status", token: token);
                    delays.Add(result.cmd.Delay);
                }
                else
                {
                    device.TransmitCmdInLib(cmd);
                }
            }

            if (externalDelay == 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(delays.Max()), token);
            }

            if (externalDelay > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(externalDelay), token);
            }

            //после задержки в этом списке будут устройства не прошедшие проверку
            errorDevices = GetErrorDevices(devices);

            return errorDevices;
        }
        //елси задлаче была прервана заранее полняем следующий код
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            token = new();
            return errorDevices;
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

    private async Task WriteCommands(BaseDevice device, string cmd, CancellationToken token)
    {
        
    }

    /// <summary>
    /// Проверка устройсва пингуются ли оно 
    /// </summary>
    /// <param name="tempCheckDevices">Устройство</param>
    /// <param name="token">Сброс вермени ожидания если прибор ответил раньше</param>
    /// <param name="externalDelay">Общая задержка проверки (default = 0, если 0 то не используется внутренняя зареджка из библиотеки)</param>
    /// <param name="cmd">Входная команда (default = "Status", из библиотеки устройства)</param>
    /// <returns></returns>
    async Task<(BaseDevice device, DeviceCmd cmd)> CheckConnectDevice(BaseDevice device,
        int externalDelay = 0, string cmd = null, CancellationToken token = default)
    {
        (bool result, DeviceCmd cmd) isWrite = (false, null);
        try
        {
            if (string.IsNullOrEmpty(cmd))
            {
                isWrite = await WriteCommandLib(device, "Status", 0, token: token);
            }
            else
            {
                isWrite = await WriteCommandLib(device, cmd, 0, token: token);
            }

            //если отправка в прибор без исключения, то получаем команду и заждержку из библиотеки (device) 
            if (isWrite.result)
            {
                if (externalDelay > 0)
                {
                    if (isWrite.cmd.Delay == 0)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(externalDelay), token);
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(isWrite.cmd.Delay), token);
                    }
                }
            }

            return (device, isWrite.cmd);
        }
        //елси задлаче была прервана заранее полняем следующий код
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            token = new();
            return (device, isWrite.cmd);
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }
    

    /// <summary>
    /// Заспись в устроство простой команды
    /// </summary>
    /// <param name="device">Устройство</param>
    /// <param name="cmd">Команда</param>
    /// <param name="externalDelay">Внешняя задержка (default = 0)</param>
    /// <param name="parameter"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private async Task<(bool result, DeviceCmd cmd)> WriteCommandLib(BaseDevice device, string cmd,
        int externalDelay = 0, string parameter = null, CancellationToken token = default)
    {
        DeviceCmd dataInLib = null;
        try
        {
            //Отпрвляем имя команды и параметр в устройство (device) и получаем из метода команду библиотеки
            dataInLib = device.TransmitCmdInLib(cmd, parameter);

            for (int i = 0; i < 2; i++)
            {
                if (externalDelay == 0 && dataInLib != null)
                {
                    if (dataInLib.Delay == 0)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(100), token);
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(dataInLib.Delay), token);
                    }
                }

                if (externalDelay > 0)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(externalDelay), token);
                }

                return (true, dataInLib);
            }

            return (false, dataInLib);
        }
        //елси задлаче была прервана заранее полняем следующий код
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            token = new();
            return (true, dataInLib);
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

    /// <summary>
    ///  Отправка команды и соответвие ответа заданному параметру
    /// </summary>
    /// <param name="device">Проверяемый прибор</param>
    /// <param name="cmd">Стандартная команда из библиотеки отправляемая в прибор</param>
    /// <param name="tempChecks">Список правильных ответов (если прибор ответил верно tempChecks = true)</param>
    /// <param name="parameter">Параметр команды из типа Випа</param>
    /// <param name="externalDelay">Внешняя задержка использование ее отключает токен</param>
    /// <param name="token">Сброс вермени ожидания если прибор ответил раньше</param>
    /// <exception cref="StandException"></exception>
    private async Task<(bool result, string receive)> WriteReadCommand(BaseDevice device, string cmd,
        string parameter = null, int externalDelay = 0, TempChecks tempChecks = null, CancellationToken token = default)
    {
        DeviceCmd dataInLib = null;

        bool matches = false;
        try
        {
            //получаем команду и заждержку из библиотеки (device) 
            dataInLib = device.TransmitCmdInLib(cmd);

            await WriteCommandLib(device, cmd, dataInLib.Delay, parameter, token);

            var isGdmCheck = CheckGdm(device, cmd, dataInLib.Receive, matches, tempChecks);

            if (isGdmCheck.isGDM || isGdmCheck.result)
            {
                var resultGdm = (isGdmCheck.result, isGdmCheck.cmd);
                return (resultGdm.result, resultGdm.cmd.Receive);
            }

            var result = CheckedDeviceOnParameter(device, dataInLib.Receive, matches, parameter, tempChecks);
            return (result.matches, result.receive);
        }
        //елси задлаче была прервана заранее полняем следующий код
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            token = new CancellationToken();

            var isGdmCheck = CheckGdm(device, cmd, dataInLib.Receive, matches, tempChecks);

            if (isGdmCheck.isGDM || isGdmCheck.result)
            {
                var resultGdm = (isGdmCheck.result, isGdmCheck.cmd.Receive);
                return (resultGdm.result, resultGdm.Receive);
            }

            var result = CheckedDeviceOnParameter(device, dataInLib.Receive, matches, parameter, tempChecks);
            return (result.matches, result.receive);
        }

        catch (Exception e)
        {
            //если вылеатет какоето исключение записываем в лист провеки false
            tempChecks?.Add(false);
            throw new Exception($"Ошибка \"{e.Message})\" при проверке данных с устройства");
        }
    }

    #endregion


    /// <summary>
    /// Проверка GDM устройств (см. документацию)
    /// </summary>
    /// <param name="device">Проверяемо утсройство</param>
    /// <param name="cmd">Команда в кторой содержится нужна последовательность</param>
    /// <param name="receiveInLib">Шаблонный ответ из бибилиотеки</param>
    /// <param name="matches"></param>
    /// <param name="tempChecks">Результат проверка листа приема на соответвие параметру из Типа Випа</param>
    /// <returns>Item1 - соответвует ли device GDM устройству
    /// Item2 - Прошла ли проверка устройства согласно алгоритму</returns>
    (bool isGDM, bool result, DeviceCmd cmd) CheckGdm(BaseDevice device, string cmd,
        string receiveInLib, bool matches, TempChecks tempChecks)
    {
        (bool result, string receive) matchesGdm = (false, null);
        // //установка параметров для проверки GDM термо/вольтметра см. документацию GDM-8255A
        // if (device is ThermoCurrentMeter t)
        // {
        //     if (t.Name.ToLower().Contains("gdm"))
        //     {
        //         if (cmd.Contains("Get term"))
        //         {
        //             SetParameterThermoCurrent(ModeGdm.Themperature);
        //             matchesGdm = CheckedDeviceOnParameter(device, receiveInLib, matches,
        //                 GetParameterForDevice().ThermoCurrentValues.ReturnFuncGDM, tempChecks);
        //
        //             return (true, matchesGdm.result, CastToNormalValues(matchesGdm.receive));
        //         }
        //
        //         if (cmd.Contains("Get curr"))
        //         {
        //             SetParameterThermoCurrent(ModeGdm.Current);
        //             matchesGdm = CheckedDeviceOnParameter(device, receiveInLib, matches,
        //                 GetParameterForDevice().ThermoCurrentValues.ReturnCurrGDM, tempChecks);
        //
        //             return (true, matchesGdm.result, CastToNormalValues(matchesGdm.receive));
        //         }
        //
        //         if (cmd.Contains("Get func"))
        //         {
        //             matchesGdm = CheckedDeviceOnParameter(device, receiveInLib, matches,
        //                 GetParameterForDevice().ThermoCurrentValues.ReturnFuncGDM, tempChecks);
        //
        //             return (true, matchesGdm.result, CastToNormalValues(matchesGdm.receive));
        //         }
        //     }
        // }
        //
        // if (device is VoltMeter v)
        // {
        //     if (v.Name.ToLower().Contains("gdm"))
        //     {
        //         if (cmd.Contains("Get volt"))
        //         {
        //             SetParameterVolt(ModeGdm.Voltage);
        //
        //             matchesGdm = CheckedDeviceOnParameter(device, receiveInLib, matches,
        //                 GetParameterForDevice().VoltValues.ReturnVoltGDM, tempChecks);
        //
        //             return (true, matchesGdm.result, CastToNormalValues(matchesGdm.receive));
        //         }
        //
        //         if (cmd.Contains("Get func"))
        //         {
        //             matchesGdm = CheckedDeviceOnParameter(device, receiveInLib, matches,
        //                 GetParameterForDevice().VoltValues.ReturnFuncGDM, tempChecks);
        //
        //             return (true, matchesGdm.result, CastToNormalValues(matchesGdm.receive));
        //         }
        //     }
        // }

        return (false, false, null);
    }


    (bool matches, string receive) CheckedDeviceOnParameter(BaseDevice device, string receiveLib, bool matches,
        string parameter, TempChecks tempChecks)
    {
        receiveLib = null;
        matches = false;
        try
        {
            //данные из листа приема от устройств
            var receive = receiveDevices[device];
            var receiveStr = receive.Last();

            if (receiveLib != null && !string.IsNullOrWhiteSpace(receiveLib))
            {
                //проверка листа приема на содержание в ответе от прибора параметра команды -
                //берется из Recieve библиотеки

                matches = CastToNormalValues(receive.Last()).Contains(receiveLib);
            }
            else
            {
                //проверка листа приема на содержание в ответе от прибора параметра команды
                matches = CastToNormalValues(receive.Last()).Contains(parameter);
            }

            //очистка листа приема от устроойств
            receiveDevices[device].Clear();
            //добавление результата проверки в список проверки
            tempChecks?.Add(matches);
            return (matches, CastToNormalValues(receiveStr));
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

    /// <summary>
    /// Преобразовние строк вида "SQU +2.00000000E+02,+4.000E+00,+2.00E+00" в стандартные строки вида 200, 4, 20
    /// </summary>
    /// <param name="str">Строка которая будет преобразована</param>
    /// <returns></returns>
    public string CastToNormalValues(string str)
    {
        try
        {
            if (str != null)
            {
                decimal myDecimalValue = 0;

                if (str.Contains("E+"))
                {
                    myDecimalValue = Decimal.Parse(str, NumberStyles.Float);
                    return myDecimalValue.ToString(CultureInfo.InvariantCulture);
                }
            }

            return str;
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

    /// <summary>
    /// Бибилиотека принимаемых от устройств данных key - Устройство с данными, value - список данных
    /// </summary>
    private Dictionary<BaseDevice, List<string>> receiveDevices = new();

    private void OnReceive(BaseDevice device, string receive)
    {
        
    }

    #endregion

    //---

    #region Проверки

    public async Task<bool> PrimaryCheckDevices(int countChecked)
    {
        try
        {
            SetStatusStand();
            SetStatusStand(testRun: TypeOfTestRun.PrimaryCheckDevices);

            var checkDevices = devices.ToList();

            for (int i = 1; i < countChecked; i++)
            {
                //сброс статусов устройств
                SetStatusDevices(checkDevices);

                var errorDevices = await CheckConnectPorts(checkDevices);
                if (errorDevices.Any())
                {
                    SetStatusDevices(errorDevices, StatusDeviceTest.Error);
                    await Task.Delay(TimeSpan.FromMilliseconds(3000));
                    SetStatusStand(20);
                }
                else
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(5000));
                    SetStatusStand(20);
                    errorDevices = await CheckConnectDevices(checkDevices, token: ctsCheckDevice.Token);
                    SetStatusDevices(errorDevices, StatusDeviceTest.Error);
                    if (!errorDevices.Any())
                    {
                        SetStatusDevices(checkDevices, StatusDeviceTest.Ok);
                    }

                    SetStatusStand(100, TypeOfTestRun.PrimaryCheckDevices);
                    return true;
                }
            }

            SetStatusStand(testRun: TypeOfTestRun.Error);
            return false;
        }
        catch (Exception e)
        {
            throw new Exception($"{e.Message}");
        }
    }

    public async Task PrimaryCheckVips()
    {
        throw new System.NotImplementedException();
    }

    #endregion


    public async Task<bool> MeasurementZero()
    {
        throw new System.NotImplementedException();
    }

    //---

    #region Обработка событий с приборов

    #endregion

    //---
    public async Task ResetCurrentTest()
    {
        throw new NotImplementedException();
    }


    public void SerializeDevice()
    {
        deviceAndLibCreator.SerializeDevices(allDevices.ToList());
    }

    public void SerializeLib()
    {
        deviceAndLibCreator.SerializeLib();
    }
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