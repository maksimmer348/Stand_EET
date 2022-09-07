using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace StandETT;

public class ViewModel : Notify
{
    //---

    #region --Модель

    //--

    #region --Стенд

    private Stand1 stand = new();

    private BaseLibCmd libCmd = BaseLibCmd.getInstance();

    #endregion

    //--

    #region --Устройства

    /// <summary>
    /// Общий список устройств
    /// </summary>
    public ReadOnlyObservableCollection<BaseDevice> AllDevices => stand.AllDevices;

    /// <summary>
    /// Внешние устройства
    /// </summary>
    public ReadOnlyObservableCollection<BaseDevice> Devices => stand.Devices;

    /// <summary>
    /// Список Випов
    /// </summary>
    public ReadOnlyObservableCollection<Vip> Vips => stand.Vips;

    #endregion

    //---

    #endregion

    //---

    #region --Конструктор --ctor

    public ViewModel()
    {
        stand.PropertyChanged += StandTestOnPropertyChanged;

        #region --Команды --cmd

        #region Общие

        StartTestDevicesCmd = new ActionCommand(OnStartTestDevicesCmdExecuted, CanStartTestDevicesCmdExecuted);
        OpenSettingsDevicesCmd = new ActionCommand(OnOpenSettingsDevicesCmdExecuted, CanOpenSettingsDevicesCmdExecuted);
        CancelAllTestCmd = new ActionCommand(OnCancelAllTestCmdExecuted, CanCancelAllTestCmdExecuted);
        NextCmd = new ActionCommand(OnNextCmdExecuted, CanNextCmdExecuted);

        #endregion

        #region Настройка устройств

        SaveSettingsCmd = new ActionCommand(OnSaveSettingsCmdExecuted, CanSaveSettingsCmdExecuted);
        AddCmdFromDeviceCmd = new ActionCommand(OnAddCmdFromDeviceCmdExecuted, CanAddCmdFromDeviceCmdExecuted);
        RemoveCmdFromDeviceCmd = new ActionCommand(OnRemoveCmdFromDeviceCmdExecuted, CanRemoveCmdFromDeviceCmdExecuted);

        #endregion

        #endregion


        // CreateReportCmd = new ActionCommand(OnCreateReportCmdExecuted, CanCreateReportCmdExecuted);
    }

    //Именно посредством него View получает уведомления, что во VM что-то изменилось и требуется обновить данные.
    private void StandTestOnPropertyChanged(object? sender, PropertyChangedEventArgs e) =>
        OnPropertyChanged(e.PropertyName);

    private double selectTab;

    /// <summary>
    /// Какая сейчас выбрана вкладка
    /// </summary>
    public double SelectTab
    {
        get => selectTab;

        set
        {
            Set(ref selectTab, value);

            if (selectTab == 4 || selectTab == 1)
            {
                //AllTypeVips = standTest.ConfigVip.TypeVips;
            }

            if (selectTab == 1)
            {
                //for (var index = 0; index < AllPrepareVips.Count; index++)
                //{
                //    if (index < 4)
                //    {
                //        var VARIABLE = AllPrepareVips[index];
                //        var rnd = Random.Shared.Next(1000, 10000);
                //        VARIABLE.number = rnd.ToString();
                //    }
                //}

                //SelectTypeVipIndex = 0;
            }
        }
    }

    #endregion

    //---

    #region Методы

    #region Общие

    /// <summary>
    /// Выключить все вкладки (PrimaryCheckDevicesTab, PrimaryCheckVipsTab, CheckVipsTab, SettingsTab)
    /// </summary>
    void AllTabsDisable()
    {
        PrimaryCheckDevicesTab = false;
        PrimaryCheckVipsTab = false;
        CheckVipsTab = false;
        SettingsTab = false;
    }

    /// <summary>
    /// Выключить все вкладки (PrimaryCheckDevicesTab, PrimaryCheckVipsTab, CheckVipsTab, SettingsTab)
    /// </summary>
    void AllTabsEnable()
    {
        PrimaryCheckDevicesTab = true;
        PrimaryCheckVipsTab = true;
        CheckVipsTab = true;
        SettingsTab = true;
    }

    #endregion

    #endregion

    //---

    #region --Команды

    //--

    #region Команды --Общие

    /// <summary>
    /// Команда ПРОДОЛЖИТЬ/ДАЛЕЕ
    /// </summary>
    public ICommand NextCmd { get; }

    async Task OnNextCmdExecuted(object p)
    {
        if (TestRun == TypeOfTestRun.PrimaryCheckDevicesReady)
        {
            SelectTab = 1;
        }
        else if (TestRun == TypeOfTestRun.PrimaryCheckVipsReady)
        {
            SelectTab = 2;
        }
    }

    bool CanNextCmdExecuted(object p)
    {
        return TestRun == TypeOfTestRun.PrimaryCheckDevicesReady ||
               TestRun == TypeOfTestRun.PrimaryCheckVipsReady;
    }

    /// <summary>
    /// Команда ОТМЕНИТЬ испытания
    /// </summary>
    public ICommand CancelAllTestCmd { get; }

    async Task OnCancelAllTestCmdExecuted(object p)
    {
        //TODO добавить canellded
        await stand.ResetCurrentTest();
        SelectTab = 0;
    }

    bool CanCancelAllTestCmdExecuted(object p)
    {
        return true;
    }

    /// <summary>
    /// Команда ЗАПУСТИТЬ исптания
    /// </summary>
    public ICommand StartTestDevicesCmd { get; }

    async Task OnStartTestDevicesCmdExecuted(object p)
    {
        if (SelectTab == 0)
        {
            try
            {
                await stand.PrimaryCheckDevices(3);
            }
            catch (Exception e)
            {
                const string caption = "Ошибка предварительной проверки устройств";
                var result = MessageBox.Show(e.Message + " Перейти в настройки?", caption, MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    SelectTab = 3;
                }
            }
        }

        else if (SelectTab == 1)
        {
            try
            {
                await stand.PrimaryCheckVips();
            }
            catch (Exception e)
            {
                const string caption = "Ошибка предварительной проверки плат Випов";
                var result = MessageBox.Show(e.Message + " Перейти в настройки?", caption, MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    SelectTab = 3;
                }
            }
        }

        else if (SelectTab == 2)
        {
            try
            {
                var mesZero = await stand.MeasurementZero();
                if (mesZero)
                {
                    // var heat = await standTest.WaitForTestMode();
                    // if (heat)
                    // {
                    //     await standTest.CyclicMeasurement();
                    // }
                }
            }
            catch (Exception e)
            {
                const string caption = "Ошибка 0 замера";
                var result = MessageBox.Show(e.Message + " Перейти в настройки?", caption, MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    SelectTab = 3;
                }
            }
        }
    }

    bool CanStartTestDevicesCmdExecuted(object p)
    {
        return TestRun != TypeOfTestRun.PrimaryCheckDevices || TestRun != TypeOfTestRun.PrimaryCheckVips ||
               TestRun != TypeOfTestRun.MeasurementZero;
    }

    /// <summary>
    /// Команда открыть ФАЙЛ КОНФИГУРАЦИИ/НАСТРОЙКУ внешних устройств
    /// </summary>
    public ICommand OpenSettingsDevicesCmd { get; }

    Task OnOpenSettingsDevicesCmdExecuted(object p)
    {
        //обработчик команды
        //TODO отправить отсюда в настройки
        return Task.CompletedTask;
    }

    bool CanOpenSettingsDevicesCmdExecuted(object p)
    {
        return TestRun switch
        {
            TypeOfTestRun.PrimaryCheckDevices => false,
            TypeOfTestRun.PrimaryCheckVips => false,
            TypeOfTestRun.DeviceOperation => false,
            TypeOfTestRun.MeasurementZero => false,
            _ => true
        };
    }

    #endregion

    //--

    #region Команды --Подключение устройств --0 tab

    #endregion

    //--

    #region Команды --Подключение Випов --1 tab

    #endregion

    //--

    #region Команды --Настройки устройств --2 tab

    /// <summary>
    /// Команда СОХРАНИТЬ выбранное внешнее устройство
    /// </summary>
    public ICommand SaveSettingsCmd { get; }

    Task OnSaveSettingsCmdExecuted(object p)
    {
        try
        {
            var index = AllDevices.IndexOf(SelectDevice);
            AllDevices[index].Name = NameDevice;
            AllDevices[index].Config.PortName = PortName;
            AllDevices[index].Config.Baud = Baud;
            AllDevices[index].Config.StopBits = StopBits;
            AllDevices[index].Config.Parity = Parity;
            AllDevices[index].Config.DataBits = DataBits;
            AllDevices[index].Config.Dtr = Dtr;


            NameDevice = selectDevice.Name;
            PortName = selectDevice.GetConfigDevice().PortName;
            Parity = selectDevice.GetConfigDevice().Baud;
            StopBits = selectDevice.GetConfigDevice().StopBits;
            Parity = selectDevice.GetConfigDevice().Parity;
            DataBits = selectDevice.GetConfigDevice().DataBits;
            Dtr = selectDevice.GetConfigDevice().Dtr;

            AllDevices[index].SetConfigDevice(TypePort.SerialInput, PortName, Baud, StopBits, Parity,
                DataBits, Dtr);

            selectedDeviceCmd.Source =
                SelectDevice?.LibCmd.DeviceCommands.Where(x =>
                    x.Key.NameDevice == selectDevice.Name);
            OnPropertyChanged(nameof(SelectedDeviceCmd));

            stand.SerializeDevice();
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message);
        }

        return Task.CompletedTask;
    }

    bool CanSaveSettingsCmdExecuted(object p)
    {
        return true;
    }


    /// <summary>
    /// Команда добавить команду к устройству
    /// </summary>
    public ICommand AddCmdFromDeviceCmd { get; }

    Task OnAddCmdFromDeviceCmdExecuted(object p)
    {
        try
        {
            //получение текущего индекса
            var index = IndexSelectCmd;

            libCmd.AddCommand(NameCmdLib, SelectDevice.Name, TransmitCmdLib, ReceiveCmdLib, DelayCmdLib,
                SelectTerminatorTransmit.Type,
                SelectTerminatorReceive.Type, TypeMessageCmdLib, IsXor);

            //обновление датагрида
            selectedDeviceCmd.Source =
                SelectDevice?.LibCmd.DeviceCommands.Where(x =>
                    x.Key.NameDevice == selectDevice.Name);
            OnPropertyChanged(nameof(SelectedDeviceCmd));


            IndexSelectCmd = index + 1;

            stand.SerializeLib();
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message);
        }

        return Task.CompletedTask;
    }

    bool CanAddCmdFromDeviceCmdExecuted(object p)
    {
        return true;
    }


    /// <summary>
    /// Команда Удалить команду устройства
    /// </summary>
    public ICommand RemoveCmdFromDeviceCmd { get; }

    Task OnRemoveCmdFromDeviceCmdExecuted(object p)
    {
        try
        {
            var index = IndexSelectCmd;

            libCmd.DeleteCommand(SelectedCmdLib.Key);
            selectedDeviceCmd.Source =
                SelectDevice?.LibCmd.DeviceCommands.Where(x =>
                    x.Key.NameDevice == selectDevice.Name);


            OnPropertyChanged(nameof(SelectedDeviceCmd));

            if (index > 0)
            {
                IndexSelectCmd = index - 1;
            }

            stand.SerializeLib();
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message);
        }


        return Task.CompletedTask;
    }


    bool CanRemoveCmdFromDeviceCmdExecuted(object p)
    {
        return true;
    }

    #endregion

    //--

    #region Команды --Настройки Типа Випов --3 tab

    #endregion

    #endregion

    //---

    #region --Поля

    #region Поля --Общие

    #region Управление --вкладками

    private bool primaryCheckDevicesTab;

    /// <summary>
    /// Включатель вкладки подключения устройств 0
    /// </summary>
    public bool PrimaryCheckDevicesTab
    {
        get => primaryCheckDevicesTab;
        set => Set(ref primaryCheckDevicesTab, value);
    }

    private bool primaryCheckVipsTab;

    /// <summary>
    /// Включатель вкладки предварительной проверки випов 1
    /// </summary>
    public bool PrimaryCheckVipsTab
    {
        get => primaryCheckVipsTab;
        set => Set(ref primaryCheckVipsTab, value);
    }

    private bool checkVipsTab;

    /// <summary>
    /// Включатель влкадки проверки випов 2
    /// </summary>
    public bool CheckVipsTab
    {
        get => checkVipsTab;
        set => Set(ref checkVipsTab, value);
    }

    private bool settingsTab;

    /// <summary>
    /// Включатель влкадки  настроек 3
    /// </summary>
    public bool SettingsTab
    {
        get => settingsTab;
        set => Set(ref settingsTab, value);
    }

    #endregion

    #region --Статусы стенда общие

    /// <summary>
    /// Уведомляет сколько процентов текущего теста прошло
    /// </summary>
    public double PercentCurrentTest => stand.PercentCurrentTest;

    /// <summary>
    /// Уведомляет текстом какое устройство проходит тест
    /// </summary>
    public string CurrentTestDevice => stand.TestCurrentDevice.Name;

    private string textCurrentTest;

    /// <summary>
    /// Уведомляет текстом этап тестов
    /// </summary>
    public string TextCurrentTest
    {
        get => textCurrentTest;
        set => Set(ref textCurrentTest, value);
    }


    private string textCountTimes;

    public string TextCountTimes
    {
        get => textCountTimes;
        set => Set(ref textCountTimes, value);
    }

    public double CountTimes => stand.CountTimes;

    //

    private double selectTypeVipIndex;

    /// <summary>
    /// Уведомляет какая вкладка сейчас открыта
    /// </summary>
    public double SelectTypeVipIndex
    {
        get => selectTypeVipIndex;
        set => Set(ref selectTypeVipIndex, value);
    }

    /// <summary>
    /// Уведомляет о просодимом тесте прееключает вкладки
    /// </summary>
    public TypeOfTestRun TestRun
    {
        get
        {
            //-

            if (stand.TestRun == TypeOfTestRun.Stop)
            {
                TextCurrentTest = "Стенд остановлен";

                AllTabsDisable();
                PrimaryCheckDevicesTab = true;
            }

            else if (stand.TestRun == TypeOfTestRun.None)
            {
                TextCurrentTest = "";
                AllTabsEnable();
            }

            //-

            else if (stand.TestRun == TypeOfTestRun.PrimaryCheckDevices)
            {
                TextCurrentTest = " Предпроверка устройств";
                TextCountTimes = "Попытка предпроверки:";
                AllTabsDisable();
                PrimaryCheckDevicesTab = true;
            }

            else if (stand.TestRun == TypeOfTestRun.PrimaryCheckDevicesReady)
            {
                TextCurrentTest = " Предпроверка устройств ОК";
                TextCountTimes = "Всего попыток:";
                PrimaryCheckDevicesTab = true;
                PrimaryCheckVipsTab = true;
            }

            //-

            else if (stand.TestRun == TypeOfTestRun.PrimaryCheckVips)
            {
                TextCurrentTest = " Предпроверка Випов";
                TextCountTimes = "Попытка предпроверки Випов:";
                AllTabsDisable();
                PrimaryCheckVipsTab = true;
            }

            else if (stand.TestRun == TypeOfTestRun.PrimaryCheckVipsReady)
            {
                TextCurrentTest = " Предпроверка Випов Ок";
                TextCountTimes = "Всего попыток:";
                PrimaryCheckDevicesTab = true;
                CheckVipsTab = true;
            }

            //-

            else if (stand.TestRun == TypeOfTestRun.DeviceOperation)
            {
                TextCurrentTest = $" Включение устройства";
                TextCountTimes = "Попытка включения:";
                AllTabsDisable();
                CheckVipsTab = true;
            }

            else if (stand.TestRun == TypeOfTestRun.DeviceOperationReady)
            {
                TextCurrentTest = " Включение устройства Ок";
                TextCountTimes = "Всего попыток:";
                AllTabsEnable();
            }

            //-

            else if (stand.TestRun == TypeOfTestRun.MeasurementZero)
            {
                TextCurrentTest = " Нулевой замер";
                AllTabsDisable();
                CheckVipsTab = true;
            }

            else if (stand.TestRun == TypeOfTestRun.MeasurementZeroReady)
            {
                TextCurrentTest = " Нулевой замер ОК";
                AllTabsEnable();
            }

            //-

            else if (stand.TestRun == TypeOfTestRun.WaitSupplyMeasurementZero)
            {
                TextCurrentTest = " Ожидание источника питания";
                AllTabsDisable();
                CheckVipsTab = true;
            }

            else if (stand.TestRun == TypeOfTestRun.WaitSupplyMeasurementZeroReady)
            {
                TextCurrentTest = " Ожидание источника питания ОК";
                AllTabsEnable();
            }

            //-

            else if (stand.TestRun == TypeOfTestRun.WaitHeatPlate)
            {
                TextCurrentTest = " Нагрев основания";
                AllTabsDisable();
                CheckVipsTab = true;
            }

            else if (stand.TestRun == TypeOfTestRun.WaitHeatPlateReady)
            {
                TextCurrentTest = " Нагрев основания ОК";
                AllTabsEnable();
            }

            //-

            else if (stand.TestRun == TypeOfTestRun.CyclicMeasurement)
            {
                TextCurrentTest = " Циклический замер";
                AllTabsDisable();
                CheckVipsTab = true;
            }

            else if (stand.TestRun == TypeOfTestRun.CycleWait)
            {
                TextCurrentTest = " Ожидание замер";
                AllTabsDisable();
                CheckVipsTab = true;
            }

            else if (stand.TestRun == TypeOfTestRun.CyclicMeasurementReady)
            {
                TextCurrentTest = " Циклический замеы закончены";
                AllTabsDisable();
                CheckVipsTab = true;
            }

            //-

            else if (stand.TestRun == TypeOfTestRun.Error)
            {
                TextCurrentTest = " Ошибка!";
            }

            return stand.TestRun;
        }
    }

    #endregion

    #endregion

    #region Поля --Подключение Устройств --0 tab

    #endregion

    #region Поля --Подключение Випов --1 tab

    #endregion

    #region Поля --Настройки устройств --2 tab

    #region --Выбор и --настройки прибора

    private readonly CollectionViewSource selectedDeviceCmd = new();

    /// <summary>
    /// Для показа/обновление команд выбранного устройства
    /// </summary>
    public ICollectionView? SelectedDeviceCmd => selectedDeviceCmd?.View;

    private BaseDevice selectDevice;

    /// <summary>
    /// Выбор устройства в в выпадающем списке
    /// </summary>
    public BaseDevice SelectDevice
    {
        get { return selectDevice; }
        set
        {
            if (!Set(ref selectDevice, value)) return;

            NameDevice = selectDevice.Name;

            try
            {
                PortName = selectDevice.GetConfigDevice().PortName;
                Baud = selectDevice.GetConfigDevice().Baud;
                StopBits = selectDevice.GetConfigDevice().StopBits;
                Parity = selectDevice.GetConfigDevice().Parity;
                DataBits = selectDevice.GetConfigDevice().DataBits;
                Dtr = selectDevice.GetConfigDevice().Dtr;


                //обновление команд выбранного устройства
                selectedDeviceCmd.Source = value?.LibCmd.DeviceCommands.Where(x =>
                    x.Key.NameDevice == selectDevice.Name);
                OnPropertyChanged(nameof(SelectedDeviceCmd));
                OnPropertyChanged(nameof(IndexTerminatorReceive));
                OnPropertyChanged(nameof(IndexTerminatorTransmit));
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
    }

    private string nameDevice;

    /// <summary>
    /// Имя устройства в текстбоксе
    /// </summary>
    public string NameDevice
    {
        get => nameDevice;
        set { Set(ref nameDevice, value); }
    }


    private string portName;

    /// <summary>
    /// Имя порта в текстбоксе
    /// </summary>
    public string PortName
    {
        get => portName;
        set => Set(ref portName, value);
    }

    private int baud;

    /// <summary>
    /// Baud rate порта в текстбоксе 
    /// </summary>
    public int Baud
    {
        get => baud;
        set => Set(ref baud, value);
    }

    private int stopBits;

    /// <summary>
    /// Стоповые биты порта в текстбоксе
    /// </summary>
    public int StopBits
    {
        get => stopBits;
        set => Set(ref stopBits, value);
    }

    private int parity;

    /// <summary>
    /// Parity bits порта в тектсбоксе
    /// </summary>
    public int Parity
    {
        get => parity;
        set => Set(ref parity, value);
    }

    private int dataBits;

    /// <summary>
    /// Бит данных в команде в текстбоксе
    /// </summary>
    public int DataBits
    {
        get => dataBits;
        set => Set(ref dataBits, value);
    }

    private bool dtr;

    /// <summary>
    /// DTR порта в чекбоксе
    /// </summary>
    public bool Dtr
    {
        get => dtr;
        set => Set(ref dtr, value);
    }

    #endregion

    #region --Выбор и --настройки --библиотеки выбранного приора

    public List<Terminator> Terminators => libCmd.Terminators;

    private string nameCmdLib;

    /// <summary>
    /// Имя команды из библиотеки
    /// </summary>
    public string NameCmdLib
    {
        get => nameCmdLib;
        set => Set(ref nameCmdLib, value);
    }

    private string transmitCmdLib;

    /// <summary>
    /// Отправляемое сообщение для устройства из библиотеки
    /// </summary>
    public string TransmitCmdLib
    {
        get => transmitCmdLib;
        set => Set(ref transmitCmdLib, value);
    }

    private Terminator selectTerminatorTransmit;

    public Terminator SelectTerminatorTransmit
    {
        get => selectTerminatorTransmit;
        set => Set(ref selectTerminatorTransmit, value);
    }


    private int indexTerminatorTransmit;

    public int IndexTerminatorTransmit
    {
        get
        {
            try
            {
                var item = Terminators.FirstOrDefault(x =>
                    x.Type == SelectTerminatorTransmit.Type && x.TypeEncod == SelectTerminatorTransmit.TypeEncod);
                indexTerminatorTransmit = Terminators.IndexOf(item);
            }
            catch (Exception e)
            {
                indexTerminatorTransmit = 0;
            }

            return indexTerminatorTransmit;
        }
        set => Set(ref indexTerminatorTransmit, value);
    }

    private bool isXor;

    /// <summary>
    /// 
    /// </summary>
    public bool IsXor
    {
        get => isXor;
        set { Set(ref isXor, value); }
    }

    private bool xorIsHex;

    /// <summary>
    /// Xor можно высчитывать только если тип сообщения hex
    /// </summary>
    public bool XorIsHex
    {
        get => xorIsHex;
        set => Set(ref xorIsHex, value);
    }

    private string receiveCmdLib;

    /// <summary>
    /// Принимаемое сообщение из устройства из библиотеки 
    /// </summary>
    public string ReceiveCmdLib
    {
        get => receiveCmdLib;
        set => Set(ref receiveCmdLib, value);
    }

    private Terminator selectTerminatorReceive;

    public Terminator SelectTerminatorReceive
    {
        get => selectTerminatorReceive;
        set => Set(ref selectTerminatorReceive, value);
    }

    private int indexTerminatorReceive;

    public int IndexTerminatorReceive
    {
        get
        {
            try
            {
                var item = Terminators.FirstOrDefault(x =>
                    x.Type == SelectTerminatorReceive.Type && x.TypeEncod == SelectTerminatorReceive.TypeEncod);
                indexTerminatorReceive = Terminators.IndexOf(item);
            }
            catch (Exception e)
            {
                indexTerminatorTransmit = 0;
            }

            return indexTerminatorReceive;
        }
        set => Set(ref indexTerminatorReceive, value);
    }


    private TypeCmd typeMessageCmdLib;

    /// <summary>
    /// Тип отправялемемой и принимаемой команды из библиотеки
    /// </summary>
    public TypeCmd TypeMessageCmdLib
    {
        get => typeMessageCmdLib;
        set => Set(ref typeMessageCmdLib, value);
    }

    private int delayCmdLib;

    /// <summary>
    /// ЗАдержка на после отправки команды до ее приема из библиотеки
    /// </summary>
    public int DelayCmdLib
    {
        get => delayCmdLib;
        set => Set(ref delayCmdLib, value);
    }

    private KeyValuePair<DeviceIdentCmd, DeviceCmd> selectedCmdLib;

    /// <summary>
    /// Выбранный итем из библиотеки
    /// </summary>
    public KeyValuePair<DeviceIdentCmd, DeviceCmd> SelectedCmdLib
    {
        get => selectedCmdLib;
        set
        {
            selectedCmdLib = value;
            NameCmdLib = SelectedCmdLib.Key.NameCmd;
            TransmitCmdLib = SelectedCmdLib.Value.Transmit;
            SelectTerminatorTransmit = SelectedCmdLib.Value.Terminator;
            IsXor = SelectedCmdLib.Value.IsXor;
            ReceiveCmdLib = SelectedCmdLib.Value.Receive;
            SelectTerminatorReceive = SelectedCmdLib.Value.ReceiveTerminator;
            TypeMessageCmdLib = SelectedCmdLib.Value.MessageType;
            DelayCmdLib = SelectedCmdLib.Value.Delay;

            if (TypeMessageCmdLib == TypeCmd.Hex)
            {
                XorIsHex = true;
            }

            if (TypeMessageCmdLib == TypeCmd.Text)
            {
                XorIsHex = false;
            }

            OnPropertyChanged(nameof(SelectedCmdLib));
            OnPropertyChanged(nameof(IndexTerminatorTransmit));
            OnPropertyChanged(nameof(IndexTerminatorReceive));
        }
    }


    private int indexSelectCmd;

    public int IndexSelectCmd
    {
        get => indexSelectCmd;
        set => Set(ref indexSelectCmd, value);
    }

    #endregion

    //--

    #endregion

    #region Поля --Настройки Типа Випов --3 tab

    #endregion

    #endregion

    //---
}