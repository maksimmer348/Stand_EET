using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Media;
using Newtonsoft.Json;

namespace StandETT;

public class BaseDevice : Notify
{
    private string isDeviceType;

    /// <summary>
    /// Тип устройства
    /// </summary>
    public string IsDeviceType
    {
        get => isDeviceType;
        set => Set(ref isDeviceType, value);
    }

    private string name;

    /// <summary>
    /// Имя устройства
    /// </summary>
    public string Name
    {
        get => name;
        set => Set(ref name, value);
    }

    /// <summary>
    /// Статус подключения порта устройства
    /// </summary>
    [JsonIgnore]
    public bool IsConnect { get; set; }

    [JsonIgnore] private StatusDeviceTest statusTest;

    /// <summary>
    /// Текущий статус устройства
    /// </summary>
    [JsonIgnore]
    public StatusDeviceTest StatusTest
    {
        get => statusTest;
        set => Set(ref statusTest, value, nameof(StatusColor));
    }

    /// <summary>
    /// Цвет статуса устройства
    /// </summary>
    [JsonIgnore]
    public Brush StatusColor =>
        StatusTest switch
        {
            StatusDeviceTest.Error => Brushes.Red,
            StatusDeviceTest.Ok => Brushes.Green,
            _ => Brushes.DarkGray
        };

    /// <summary>
    /// Класс конфига
    /// </summary>
    public ConfigDeviceParams Config { get; set; } = new ConfigDeviceParams();

    /// <summary>
    /// Компорт прибора
    /// </summary>
    [JsonIgnore]
    protected ISerialLib port { get; set; }

    /// <summary>
    /// Класс библиотеки
    /// </summary>
    [JsonIgnore] public BaseLibCmd LibCmd = BaseLibCmd.getInstance();

    /// <summary>
    /// Класс библиотеки
    /// </summary>
    [JsonIgnore]
    protected TypeCmd TypeReceive { get; set; }

    /// <summary>
    /// Задержка команды
    /// </summary>
    [JsonIgnore]
    public int CmdDelay { get; set; }

    /// <summary>
    /// Событие проверки коннекта к порту
    /// </summary>
    [JsonIgnore] public Action<BaseDevice, bool> ConnectPort;

    /// <summary>
    /// Событие проверки коннекта к устройству
    /// </summary>
    [JsonIgnore] public Action<BaseDevice, bool, string> ConnectDevice;

    /// <summary>
    /// Событие приема данных с устройства
    /// </summary>
    [JsonIgnore] public Action<BaseDevice, string> Receive;

    /// <summary>
    /// Событие приема данных с устройства
    /// </summary>
    [JsonIgnore] public Action<string> ReceiveRelay;

    Stopwatch stopwatch = new();

    //
    //расположение в таблице окна пограммы
    public int RowIndex { get; set; }

    public int ColumnIndex { get; set; }
    //

    public BaseDevice(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Конфигурация коморта утройства
    /// </summary>
    /// <param name="typePort">Тип исопльзуемой библиотеки com port</param>
    /// <param name="portName">омер компорта</param>
    /// <param name="baud">Бауд рейт компорта</param>
    /// <param name="stopBits">Стоповые биты компорта</param>
    /// <param name="parity">Parity bits</param>
    /// <param name="dataBits">Data bits count</param>
    /// <param name="dtr"></param>
    /// <returns></returns>
    public void SetConfigDevice(TypePort typePort, string portName, int baud, int stopBits, int parity, int dataBits,
        bool dtr = true)
    {
        Config.TypePort = typePort;
        Config.PortName = $"{portName}";
        Config.Baud = baud;
        Config.StopBits = stopBits;
        Config.Parity = parity;
        Config.DataBits = dataBits;
        Config.Dtr = dtr;
    }

    /// <summary>
    /// Открыть компорт устройства
    /// </summary>
    /// <returns></returns>
    public bool Open()
    {
        return port.Open();
    }

    /// <summary>
    /// Закрыть компорт устройства
    /// </summary>
    /// <returns></returns>
    public void Close()
    {
        if (port != null)
        {
            port.Close();
        }
    }

    /// <summary>
    /// Применение настроек, подключение событий и старт устройства
    /// </summary>
    public void Start()
    {
        SetPort();
        port.Open();
        port.Dtr = Config.Dtr;
    }

    public void SetPort()
    {
        if (Config.TypePort == TypePort.GodSerial)
        {
            port = new SerialGod();
        }

        if (Config.TypePort == TypePort.SerialInput)
        {
            port = new SerialInput();
        }

        SetInvoke();
        port.SetPort(Config.PortName, Config.Baud, Config.StopBits, Config.Parity, Config.DataBits);
    }

    public void SetInvoke()
    {
        port.ConnectionStatusChanged += ConnectionStatusChanged;
        port.MessageReceived += MessageReceived;
    }

    /// <summary>
    /// Получить конфиг данные порта устройства 
    /// </summary>
    /// <returns>Данные порта устройства</returns>
    /// <exception cref="DeviceException">Данные получить невзожноно</exception>
    public ConfigDeviceParams GetConfigDevice()
    {
        try
        {
            return Config;
        }
        catch (Exception e)
        {
            throw new Exception("Файл конфига отсутствует");
        }
    }

    /// <summary>
    /// Проверка устройства на ответ на статусную команду
    /// </summary>
    /// <param name="checkCmd">Команда проверки не из библиотеки (если пусто будет исользована команда "Status" и прибор из библиотеки )</param>
    /// <param name="delay">Задержка на проверку (если 0 будет исользована из библиотеки)</param>
    /// <param name="terminator">Терминатор строки</param>
    /// <returns>Успешна ли попытка коннекта</returns>
    /// <exception cref="Exception">Такого устройства, нет в библиотеке команд</exception>
    public void CheckedConnectDevice(string checkCmd = "", int delay = 0, string terminator = "")
    {
        //если строка команды пустая
        if (string.IsNullOrWhiteSpace(checkCmd))
        {
            //используем команду статус которя возмет текущий прибор и введет в него команду статус
            TransmitCmdInLib("Status");
        }
        else
        {
            //используем ручной ввод
            port.TransmitCmdTextString(cmd: checkCmd, delay: delay, terminator: terminator);
        }
    }

    /// <summary>
    /// Отправка в устройство и прием СТАНДАРТНЫХ (есть в библиотеке команд) команд из устройства
    /// </summary>
    /// <param name="nameCommand">Имя команды (например Status)</param>
    /// <param name="parameter">Ответ от устройств из команды (Receive)</param>
    public DeviceCmd TransmitCmdInLib(string nameCommand, string parameter = null)
    {
        var selectCmd = GetLibItem(nameCommand, Name);

        if (selectCmd == null)
        {
            throw new Exception(
                $"Такое устройство - {IsDeviceType}/{Name} или команда - {nameCommand}, в библиотеке не найдены");
        }

        CmdDelay = selectCmd.Delay;

        if (selectCmd.MessageType == TypeCmd.Hex)
        {
            TypeReceive = TypeCmd.Hex;
            if (selectCmd.IsXor)
            {
                port.TransmitCmdHexString(selectCmd.Transmit + parameter, selectCmd.Delay,
                    selectCmd.Terminator.ReceiveTerminator, true);
            }
            else
            {
                port.TransmitCmdHexString(selectCmd.Transmit + parameter, selectCmd.Delay,
                    selectCmd.Terminator.ReceiveTerminator);
            }
        }
        else
        {
            TypeReceive = TypeCmd.Text;
            port.TransmitCmdTextString(selectCmd.Transmit + parameter, selectCmd.Delay,
                selectCmd.Terminator.ReceiveTerminator);
        }

        return selectCmd;
    }

    /// <summary>
    /// Выбор команды из библиотеки основываясь на ее имени и имени прибора
    /// </summary>
    /// <param name="cmd">Имя команды</param>
    /// <param name="deviceName">Имя прибора</param>
    /// <returns>Команда из библиотеки</returns>
    public DeviceCmd GetLibItem(string cmd, string deviceName)
    {
        try
        {
            return LibCmd.DeviceCommands
                .FirstOrDefault(x => x.Key.NameDevice == deviceName && x.Key.NameCmd == cmd).Value;
        }
        catch (Exception e)
        {
            throw new Exception($"Exception: Проблема с библиотекой команд {e.Message}");
        }
    }

    protected ( KeyValuePair<DeviceIdentCmd, DeviceCmd> cmd, BaseDevice baseDevice) GetLibItemInReceive(
        string receiveCmd,
        string deviceName,
        List<BaseDevice> baseDevices)
    {
        try
        {
            var deviceCmd = LibCmd.DeviceCommands
                .FirstOrDefault(x => x.Key.NameDevice == deviceName && x.Value.Receive == receiveCmd);
            var baceDevice = baseDevices.FirstOrDefault(x => x.Name == deviceName);

            return (deviceCmd, baceDevice);
        }
        catch (Exception e)
        {
            throw new Exception($"Exception: Проблема с библиотекой команд {e.Message}");
        }
    }

    /// <summary>
    /// Обработка события коннект выбраного компорта
    /// </summary>
    private void ConnectionStatusChanged(bool isConnect)
    {
        IsConnect = isConnect;
        ConnectPort.Invoke(this, isConnect);
    }

    /// <summary>
    /// Обработка события прнятого сообщения из устройства
    /// </summary>
    private void MessageReceived(byte[] data)
    {
        var receive = "";

        if (Name == "MRS")
        {
            if (TypeReceive == TypeCmd.Text)
            {
                receive = Encoding.UTF8.GetString(data);
                ReceiveRelay.Invoke(receive);
                return;
            }

            if (TypeReceive == TypeCmd.Hex)
            {
                foreach (var d in data)
                {
                    receive += Convert.ToByte(d).ToString("x2");
                }

                ConnectDevice.Invoke(this, true, receive);
                return;
            }

            ConnectDevice.Invoke(this, false, receive);
            return;
        }

        var selectCmd = GetLibItem("Status", Name);

        if (TypeReceive == TypeCmd.Text)
        {
            receive = Encoding.UTF8.GetString(data);

            if (receive.Contains(selectCmd.Receive))
            {
                ConnectDevice.Invoke(this, true, receive);
                return;
            }

            Receive.Invoke(this, receive);
            return;
        }

        if (TypeReceive == TypeCmd.Hex)
        {
            foreach (var d in data)
            {
                receive += Convert.ToByte(d).ToString("x2");
            }

            if (receive.Contains(selectCmd.Receive.ToLower()))
            {
                ConnectDevice.Invoke(this, true, receive);
                return;
            }

            Receive.Invoke(this, ISerialLib.GetStringHexInText(receive));
            return;
        }
    }
}