﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Media;
using Newtonsoft.Json;

namespace StandETT;

public class BaseDevice : Notify
{
    #region Поля

    #region --Индентификация устройства

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

    private string prefix;
    public string Prefix
    {
        get => prefix;
        set => Set(ref prefix, value);
    }

    private string nameCurrentCmd;

    /// <summary>
    /// Имя теукщей команды устройства может и не содержватся в библиотеке
    /// </summary>
    [JsonIgnore]
    public string NameCurrentCmd
    {
        get => nameCurrentCmd;
        set => Set(ref nameCurrentCmd, value);
    }

    /// <summary>
    /// Текущая команда устройства может из библиотеки
    /// </summary>
    private DeviceCmd selectCmd;

    [JsonIgnore]
    public DeviceCmd CurrentCmd
    {
        get => selectCmd;
        set => Set(ref selectCmd, value);
    }

    private string currentParameter;

    [JsonIgnore]
    public string CurrentParameter
    {
        get => currentParameter;
        set => Set(ref currentParameter, value);
    }

    #endregion

    //---

    #region --Статусы устройства

    [JsonIgnore] public AllDeviceError AllDeviceError = new AllDeviceError();

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


    [JsonIgnore] private OnOffStatus statusOnOff;

    /// <summary>
    /// Статус Output или Включения устройства
    /// </summary>
    [JsonIgnore]
    public OnOffStatus StatusOnOff
    {
        get => statusOnOff;
        set => Set(ref statusOnOff, value, nameof(OnOffColor));
    }

    /// <summary>
    /// Цвет статуса Output или Включения устройства
    /// </summary>
    [JsonIgnore]
    public object OnOffColor
    {
        get
        {
            return StatusOnOff switch
            {
                OnOffStatus.Off => Brushes.Red,
                OnOffStatus.On => Brushes.Green,
                _ => Brushes.DarkGray
            };
        }
    }

    private string errorStatus;

    /// <summary>
    /// Статус ошибки
    /// </summary>
    [JsonIgnore]
    public string ErrorStatus
    {
        get => errorStatus;
        set => Set(ref errorStatus, value);
    }

    #endregion

    //---

    #region --Конфиг устройства

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
    /// Текущая команда устройства может из библиотеки
    /// </summary>
    private bool portIsOpen;

    [JsonIgnore]
    public bool PortIsOpen
    {
        get => portIsOpen;
        set => Set(ref portIsOpen, value);
    }

    /// <summary>
    /// Тип сообщения
    /// </summary>
    [JsonIgnore]
    protected TypeCmd TypeReceive { get; set; }

    /// <summary>
    /// Класс библиотеки
    /// </summary>
    [JsonIgnore] public BaseLibCmd LibCmd = BaseLibCmd.getInstance();

    #endregion

    //---

    #region --События устройства

    /// <summary>
    /// Событие проверки коннекта к порту
    /// </summary>
    [JsonIgnore] public Action<BaseDevice, bool> PortConnecting;

    /// <summary>
    /// Событие приема данных с устройства
    /// </summary>
    [JsonIgnore] public Action<BaseDevice, string, DeviceCmd> DeviceReceiving;

    /// <summary>
    /// Событие приема данных с устройства
    /// </summary>
    [JsonIgnore] public Action<BaseDevice, string> DeviceError;

    // /// <summary>
    // /// Событие проверки коннекта к устройству
    // /// </summary>
    // [JsonIgnore] public Action<BaseDevice, bool, string> DeviceConnecting;

    // /// <summary>
    // /// Событие приема данных с устройства
    // /// </summary>
    // [JsonIgnore] public Action<BaseDevice, string> DeviceReceiving;

    #endregion

    //---

    #region --Вспомогательные поля

    //
    [JsonIgnore] Stopwatch stopwatch = new();
    //

    //
    //расположение в таблице окна пограммы
    public int RowIndex { get; set; }

    public int ColumnIndex { get; set; }


    //

    #endregion

    //---

    #endregion

    //----

    #region --Конструктор--ctor

    public BaseDevice(string name)
    {
        Name = name;
    }

    #endregion

    #region --Настройки --порта и --конфиги

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
    public virtual void SetConfigDevice(TypePort typePort, string portName, int baud, int stopBits, int parity,
        int dataBits,
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
    public virtual bool Open()
    {
        PortIsOpen = true;
        return port.Open();
    }

    /// <summary>
    /// Закрыть компорт устройства
    /// </summary>
    /// <returns></returns>
    public virtual void Close()
    {
        if (port != null)
        {
            port.Close();
        }
    }

    public virtual void DtrEnable()
    {
        if (port != null)
        {
            port.DtrEnable();
        }
    }


    public virtual void ClearBuff()
    {
        if (port != null)
        {
            port.DiscardInBuffer();
        }
    }

    /// <summary>
    /// Применение настроек, подключение событий и старт устройства
    /// </summary>
    public virtual void Start()
    {
        SetPort();
        PortIsOpen = port.Open();
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
        port.Dtr = true;
    }

    public void SetInvoke()
    {
        port.PortConnecting += Port_Connecting;
        port.Receiving += Device_Receiving;
        port.ErrorPort += Device_Error;
    }


    /// <summary>
    /// Получить конфиг данные порта устройства 
    /// </summary>
    /// <returns>Данные порта устройства</returns>
    /// <exception cref="DeviceException">Данные получить невзожноно</exception>
    public virtual ConfigDeviceParams GetConfigDevice()
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

    #endregion

    //----

    #region ---Обработка события

    /// <summary>
    /// Обработка события коннект выбраного компорта
    /// </summary>
    private void Port_Connecting(bool isConnect)
    {
        ErrorStatus = string.Empty;
        PortConnecting.Invoke(this, isConnect);
    }

    /// <summary>
    /// Обработка события прнятого сообщения из устройства
    /// </summary>
    private void Device_Receiving(byte[] data)
    {
        ErrorStatus = string.Empty;
        var receive = string.Empty;

        if (CurrentCmd.MessageType == TypeCmd.Text)
        {
            receive = Encoding.UTF8.GetString(data);
        }

        if (TypeReceive == TypeCmd.Hex)
        {
            foreach (var d in data)
            {
                receive += Convert.ToByte(d).ToString("x2");
            }
        }

        DeviceReceiving.Invoke(this, receive.ToLower(), CurrentCmd);
    }

    private void Device_Error(string e)
    {
        //ErrorStatus = $"Ошибка уcтройства {Name}, {e}";
        DeviceError?.Invoke(this, e);
    }

    #endregion

    //----

    #region отправка в устройство команд

    /// <summary>
    /// Отправка в устройство (есть в библиотеке команд) команд из устройства
    /// </summary>
    /// <param name="nameCommand">Имя команды (например Status)</param>
    /// <param name="parameter">Ответ от устройств из команды (Receive)</param>
    public virtual void WriteCmd(string nameCommand, string parameter = null)
    {
        NameCurrentCmd = nameCommand;
        CurrentParameter = parameter;

        CurrentCmd = GetLibItem(nameCommand, Name);

        //
        AllDeviceError.ErrorReceive = true;
        AllDeviceError.ErrorParam = !string.IsNullOrEmpty(parameter);
        AllDeviceError.ErrorTerminator = !string.IsNullOrEmpty(CurrentCmd.Terminator.ReceiveTerminator);
        AllDeviceError.ErrorLength = !string.IsNullOrEmpty(CurrentCmd.Length);
        AllDeviceError.ErrorTimeout = true;
        //

        if (CurrentCmd == null)
        {
            throw new Exception(
                $"Такое устройство - {IsDeviceType}/{Name} или команда - {nameCommand}, в библиотеке не найдены");
        }

        if (CurrentCmd.MessageType == TypeCmd.Hex)
        {
            TypeReceive = TypeCmd.Hex;
            if (CurrentCmd.IsXor)
            {
                port.TransmitCmdHexString(CurrentCmd.Transmit + parameter, CurrentCmd.Delay,
                    CurrentCmd.Terminator.ReceiveTerminator, true);
            }
            else
            {
                port.TransmitCmdHexString(CurrentCmd.Transmit + parameter, CurrentCmd.Delay,
                    CurrentCmd.Terminator.ReceiveTerminator);
            }
        }
        else
        {
            TypeReceive = TypeCmd.Text;
            port.TransmitCmdTextString(CurrentCmd.Transmit + parameter, CurrentCmd.Delay,
                CurrentCmd.Terminator.ReceiveTerminator);
        }

        //return CurrentCmd;
    }

    #endregion

    //----

    #region Вспомогтальные методы

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
            throw new Exception($"Exception: команда {cmd} или устройство не найдены {e.Message}");
        }
    }

    //TODO вернуть
    // protected ( KeyValuePair<DeviceIdentCmd, DeviceCmd> cmd, BaseDevice baseDevice) GetLibItemInReceive(
    //     string receiveCmd,
    //     string deviceName,
    //     List<BaseDevice> baseDevices)
    // {
    //     try
    //     {
    //         var deviceCmd = LibCmd.DeviceCommands
    //             .FirstOrDefault(x => x.Key.NameDevice == deviceName && x.Value.Receive == receiveCmd);
    //         var baceDevice = baseDevices.FirstOrDefault(x => x.Name == deviceName);
    //
    //         return (deviceCmd, baceDevice);
    //     }
    //     catch (Exception e)
    //     {
    //         throw new Exception($"Exception: Проблема с библиотекой команд {e.Message}");
    //     }
    // }

    #endregion
}