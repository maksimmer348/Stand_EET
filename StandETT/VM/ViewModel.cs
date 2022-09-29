using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using StandETT.SubCore;

namespace StandETT;

public class ViewModel : Notify
{
    //---

    #region --Модель

    //--

    #region --Стенд

    private Stand1 stand = new();

    private BaseLibCmd libCmd = BaseLibCmd.getInstance();

    private ConfigTypeVip cfgTypeVips = ConfigTypeVip.getInstance();

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


    /// <summary>
    /// Список Випов
    /// </summary>
    public ObservableCollection<TypeVip> TypeVips => cfgTypeVips.TypeVips;

    #endregion

    //---

    #endregion

    //---

    #region --Конструктор --ctor

    public ViewModel()
    {
        stand.PropertyChanged += StandTestOnPropertyChanged;

        #region --Команды --cmds

        #region Общие

        StartTestDevicesCmd = new ActionCommand(OnStartTestDevicesCmdExecuted, CanStartTestDevicesCmdExecuted);
        DeviceConfigCmd = new ActionCommand(OnDeviceConfigCmdExecuted, CanDeviceConfigCmdExecuted);
        CancelAllTestCmd = new ActionCommand(OnCancelAllTestCmdExecuted, CanCancelAllTestCmdExecuted);
        NextCmd = new ActionCommand(OnNextCmdExecuted, CanNextCmdExecuted);
        CloseActionWindowCmd = new ActionCommand(OnCloseActionWindowCmdExecuted, CanCloseActionWindowCmdExecuted);

        #endregion

        #region Настройка устройств

        SaveSettingsCmd = new ActionCommand(OnSaveSettingsCmdExecuted, CanSaveSettingsCmdExecuted);
        AddCmdFromDeviceCmd = new ActionCommand(OnAddCmdFromDeviceCmdExecuted, CanAddCmdFromDeviceCmdExecuted);
        RemoveCmdFromDeviceCmd = new ActionCommand(OnRemoveCmdFromDeviceCmdExecuted, CanRemoveCmdFromDeviceCmdExecuted);

        #endregion

        #region Настройка типов Випов

        SaveTypeVipSettingsCmd = new ActionCommand(OnSaveTypeVipSettingsCmdExecuted, CanSaveTypeVipSettingsCmdExecuted);

        RemoveTypeVipSettingsCmd =
            new ActionCommand(OnRemoveTypeVipSettingsCmdExecuted, CanRemoveTypeVipSettingsCmdExecuted);

        #endregion

        #endregion

        stand.OpenActionWindow += OpenActionWindow;

        //TODO убрать
        AllBtnsEnable();
        AllTabsEnable();
        SelectTab = 0;
        //TODO убрать

        // CreateReportCmd = new ActionCommand(OnCreateReportCmdExecuted, CanCreateReportCmdExecuted);
    }

    private void OpenActionWindow(bool obj)
    {
        if (obj)
        {
            aw = new ActionWindow()
            {
                DataContext = this
            };
            aw.Show();
            aw.Closed += AwOnClosed;
            WindowDisabled = false;
        }
    }

    private ActionWindow aw;

    //Именно посредством него View получает уведомления, что во VM что-то изменилось и требуется обновить данные.
    private void StandTestOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(e.PropertyName);

        if (e.PropertyName == nameof(TestRun))
        {
            TestRun = stand.TestRun;
        }
    }

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
                var rnd = Random.Shared.Next(1000, 10000);
                Vips[0].Name = rnd.ToString();
                rnd = Random.Shared.Next(1000, 10000);
                Vips[1].Name = rnd.ToString();
                rnd = Random.Shared.Next(1000, 10000);
                Vips[2].Name = rnd.ToString();
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

    /// <summary>
    /// Выключить все кнопки 
    /// </summary>
    void AllBtnsEnable()
    {
        NextBtnEnabled = true;
        DeviceConfigBtnEnabled = true;
        StartTestDevicesBtnEnabled = true;
        CancelAllTestBtnEnabled = true;
    }

    /// <summary>
    /// Выключить все кнопки 
    /// </summary>
    void AllBtnsDisable()
    {
        NextBtnEnabled = false;
        DeviceConfigBtnEnabled = false;
        StartTestDevicesBtnEnabled = false;
        CancelAllTestBtnEnabled = false;
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
        // return TestRun == TypeOfTestRun.PrimaryCheckDevicesReady ||
        //        TestRun == TypeOfTestRun.PrimaryCheckVipsReady;
        return true;
    }

    //--reset--allreset
    /// <summary>
    /// Команда ОТМЕНИТЬ испытания
    /// </summary>
    public ICommand CancelAllTestCmd { get; }

    async Task OnCancelAllTestCmdExecuted(object p)
    {
        await stand.ResetAllTests();
    }

    private void AwOnClosed(object sender, EventArgs e)
    {
        WindowDisabled = true;
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
                await stand.PrimaryCheckDevices(Convert.ToInt32(CountChecked), Convert.ToInt32(AllTimeChecked));
            }
            catch (Exception e)
            {
                string caption = "Ошибка предварительной проверки устройств";

                var result = MessageBox.Show(e.Message + "Перейти в настройки устройств?", caption,
                    MessageBoxButton.YesNo, MessageBoxImage.Error);

                if (result == MessageBoxResult.No)
                {
                    await stand.ResetAllTests();
                    SelectTab = 0;
                }

                if (result == MessageBoxResult.Yes)
                {
                    await stand.ResetAllTests();
                    SelectTab = 3;
                }
            }
        }

        else if (SelectTab == 1)
        {
            try
            {
                await stand.PrimaryCheckVips(Convert.ToInt32(CountChecked), Convert.ToInt32(AllTimeChecked));
            }
            catch (Exception e)
            {
                const string caption = "Ошибка предварительной проверки реле Випов";
                
                var result = MessageBox.Show(e.Message + "Перейти в настройки устройств?", caption,
                    MessageBoxButton.YesNo);

                if (result == MessageBoxResult.No)
                {
                    await stand.ResetAllTests();
                    SelectTab = 0;
                }
                if (result == MessageBoxResult.Yes)
                {
                    await stand.ResetAllTests();
                    SelectTab = 3;
                }
            }
        }

        else if (SelectTab == 2)
        {
            try
            {
                bool mesZero = await stand.MeasurementZero();

                // if (mesZero)
                // {
                //     var heat = await standTest.WaitForTestMode();
                //     if (heat)
                //     {
                //         await standTest.CyclicMeasurement();
                //     }
                // }
            }
            catch (Exception e) when (e.Message.Contains("Ошибка настройки парамтеров"))
            {
                const string caption = "Ошибка настройки парамтеров";
                var result = MessageBox.Show(e.Message + "Перейти в настройки парамтеров?", caption,
                    MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    SelectTab = 4;
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
        return true;
    }

    /// <summary>
    /// Команда открыть ФАЙЛ КОНФИГУРАЦИИ/НАСТРОЙКУ внешних устройств
    /// </summary>
    public ICommand DeviceConfigCmd { get; }

    Task OnDeviceConfigCmdExecuted(object p)
    {
        //обработчик команды
        //TODO отправить отсюда в настройки
        return Task.CompletedTask;
    }

    bool CanDeviceConfigCmdExecuted(object p)
    {
        // return TestRun switch
        // {
        //     TypeOfTestRun.PrimaryCheckDevices => false,
        //     TypeOfTestRun.PrimaryCheckVips => false,
        //     TypeOfTestRun.DeviceOperation => false,
        //     TypeOfTestRun.MeasurementZero => false,
        //     TypeOfTestRun.Stoped => false,
        //     _ => true
        // };

        return true;
    }


    public ICommand CloseActionWindowCmd { get; }

    Task OnCloseActionWindowCmdExecuted(object p)
    {
        aw.Close();
        return Task.CompletedTask;
    }

    bool CanCloseActionWindowCmdExecuted(object p)
    {
        return true;
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
            AllDevices[index].Prefix = Prefix;
            AllDevices[index].Config.PortName = PortName;
            AllDevices[index].Config.Baud = Baud;
            AllDevices[index].Config.StopBits = StopBits;
            AllDevices[index].Config.Parity = Parity;
            AllDevices[index].Config.DataBits = DataBits;
            AllDevices[index].Config.Dtr = Dtr;


            NameDevice = selectDevice.Name;
            Prefix = selectDevice.Prefix;
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


            stand.timeMachine.CountChecked = CountChecked;
            stand.timeMachine.AllTimeChecked = AllTimeChecked;

            stand.SerializeDevice();
            stand.SerializeTime();
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

            libCmd.AddCommand(NameCmdLib, SelectDevice.Name, TransmitCmdLib, DelayCmdLib,
                ReceiveCmdLib, IsTransmitParam,
                terminator: SelectTerminatorTransmit.Type, receiveTerminator: SelectTerminatorReceive.Type,
                type: TypeMessageCmdLib, isXor: IsXor, length: LengthCmdLib);

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

    /// <summary>
    /// Команда ДОБАВИТЬ тип випа
    /// </summary>
    public ICommand SaveTypeVipSettingsCmd { get; }

    Task OnSaveTypeVipSettingsCmdExecuted(object p)
    {
        var typeConfig = new TypeVip();

        typeConfig.Type = TypeVipNameSettings;
        typeConfig.PrepareMaxCurrentIn = Convert.ToDouble(PrepareMaxCurrentIn);
        typeConfig.MaxCurrentIn = Convert.ToDouble(MaxCurrentIn);
        typeConfig.PercentAccuracyCurrent = Convert.ToDouble(PercentAccuracyCurrent);
        typeConfig.MaxVoltageOut1 = Convert.ToDouble(MaxVoltageOut1);
        typeConfig.MaxVoltageOut2 = Convert.ToDouble(MaxVoltageOut2);
        typeConfig.PercentAccuracyVoltages = Convert.ToDouble(PercentAccuracyVoltages);
        typeConfig.VoltageOut2Using = voltageOuе2Using;
        typeConfig.SetDeviceParameters(new DeviceParameters()
        {
            BigLoadValues = new BigLoadValues(FreqLoad, AmplLoad, DcoLoad, SquLoad, OutputOnLoad, OutputOffLoad),
            HeatValues = new HeatValues(OutputOnHeat, OutputOffHeat),
            SupplyValues = new SupplyValues(VoltageSupply, CurrentSupply, OutputOnSupply, OutputOffSupply),
            ThermoCurrentValues =
                new ThermoCurrentMeterValues(CurrentMeterCurrentMax, TermocoupleType, OutputOnThermoCurrent,
                    OutputOffThermoCurrent),
            VoltValues = new VoltMeterValues(VoltMeterVoltMax, OutputOnVoltMeter, OutputOffVoltmeter)
        });

        stand.AddTypeVips(typeConfig);

        selectedTypeVips.Source = SelectTypeVipSettings?.Type;
        OnPropertyChanged(nameof(SelectedTypeVips));

        stand.SerializeTypeVips();
        CurrentTypeVipSettings = cfgTypeVips.TypeVips.IndexOf(typeConfig);
        return Task.CompletedTask;
    }

    bool CanSaveTypeVipSettingsCmdExecuted(object p)
    {
        return true;
    }

    /// <summary>
    /// Команда УДАЛИТЬ тип випа
    /// </summary>
    public ICommand RemoveTypeVipSettingsCmd { get; }

    Task OnRemoveTypeVipSettingsCmdExecuted(object p)
    {
        var index = cfgTypeVips.TypeVips.IndexOf(SelectTypeVipSettings);

        stand.RemoveTypeVips(SelectTypeVipSettings);
        //AllTypeVips = standTest.ConfigVip.TypeVips;
        stand.SerializeTypeVips();

        if (index > 0)
        {
            CurrentTypeVipSettings = index - 1;
        }
        else
        {
            TypeVipNameSettings = null;
            EnableTypeVipName = true;
            PrepareMaxCurrentIn = null;
            MaxCurrentIn = null;
            PercentAccuracyCurrent = null;
            MaxVoltageOut1 = null;
            MaxVoltageOut2 = null;
            PercentAccuracyVoltages = null;
            voltageOuе2Using = false;

            FreqLoad = null;
            AmplLoad = null;
            DcoLoad = null;
            SquLoad = null;

            OutputOnLoad = null;
            OutputOffLoad = null;

            OutputOnHeat = null;
            OutputOffHeat = null;

            VoltageSupply = null;
            CurrentSupply = null;
            OutputOnSupply = null;
            OutputOffSupply = null;

            CurrentMeterCurrentMax = null;
            TermocoupleType = null;
            OutputOnThermoCurrent = null;
            OutputOffThermoCurrent = null;


            VoltMeterVoltMax = null;
            OutputOffVoltmeter = null;
            OutputOnVoltMeter = null;

            CurrentTypeVipSettings = 0;
        }

        return Task.CompletedTask;
    }

    bool CanRemoveTypeVipSettingsCmdExecuted(object p)
    {
        return true;
    }

    #endregion

    #endregion

    //---

    #region --Поля

    //--

    #region Поля --Общие

    //--

    #region Управление окнами

    private bool windowDisabled = true;

    public bool WindowDisabled
    {
        get => windowDisabled;
        set => Set(ref windowDisabled, value);
    }

    private string captionAction;

    /// <summary>
    ///
    /// </summary>
    public string CaptionAction
    {
        get => captionAction;
        set => Set(ref captionAction, value);
    }

    public string ErrorMessage => stand.ErrorMessage;

    public string ErrorOutput => stand.ErrorOutput;

    #endregion

    //--

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

    //--

    #region --Статусы стенда общие

    /// <summary>
    /// Уведомляет сколько процентов текущего теста прошло
    /// </summary>
    public double PercentCurrentTest => stand.PercentCurrentTest;

    /// <summary>
    /// Уведомляет сколько процентов текущего теста прошло
    /// </summary>
    public double PercentCurrentReset => stand.PercentCurrentReset;

    
    /// <summary>
    /// Уведомляет текстом какое устройство проходит тест
    /// </summary>
    public string TestCurrentDevice
    {
        get
        {
            if (stand.TestCurrentDevice != null)
            {
                if (stand.TestCurrentDevice is RelayVip)
                {
                    return "Устройство: " + stand.TestCurrentDevice.IsDeviceType;
                }

                return "Устройство: " + stand.TestCurrentDevice.IsDeviceType + " " + stand.TestCurrentDevice.Name;
            }

            return null;
        }
    }

    private string textCurrentTest;

    /// <summary>
    /// Уведомляет текстом этап тестов
    /// </summary>
    public string TextCurrentTest
    {
        get => textCurrentTest;
        set => Set(ref textCurrentTest, value);
    }

    public string SubTestText => stand.SubTestText;
    public string CurrentCountChecked => stand.CurrentCountChecked;

    private string countTimes = "3";

    public string CountChecked
    {
        get => countTimes;
        set => Set(ref countTimes, value);
    }

    private string allTimeChecked = "3000";

    public string AllTimeChecked
    {
        get => allTimeChecked;
        set => Set(ref allTimeChecked, value);
    }

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

    private TypeOfTestRun testRun = TypeOfTestRun.Stop;

    /// <summary>
    /// Уведомляет о просодимом тесте прееключает вкладки
    /// </summary>
    public TypeOfTestRun TestRun
    {
        get => testRun;

        //TODO Вернуть убранно чтобы разлоичить вкладки
        // set { testRun = value; }
        set
        {
            if (!Set(ref testRun, value)) return;
            //-

            if (stand.TestRun == TypeOfTestRun.Stop)
            {
                TextCurrentTest = "Стенд остановлен";

                // //
                // AllTabsDisable();
                // AllBtnsEnable();
                // //
                // CancelAllTestBtnEnabled = false;
                // PrimaryCheckDevicesTab = true;
            }

            else if (stand.TestRun == TypeOfTestRun.Stoped)
            {
                TextCurrentTest = "Тесты прерваны, отключение устройств... ";

                // //
                // AllTabsDisable();
                // AllBtnsDisable();
                // //
                // PrimaryCheckDevicesTab = true;
            }

            //    else if (stand.TestRun == TypeOfTestRun.None)
            //    {
            //        TextCurrentTest = "";

            //        //
            //        AllTabsEnable();
            //        AllBtnsEnable();
            //        //

            //        CancelAllTestBtnEnabled = false;
            //    }

            //    //-

            //    else if (stand.TestRun == TypeOfTestRun.CheckPorts)
            //    {
            //        TextCurrentTest = " Проверка портов";

            //        //
            //        AllTabsDisable();
            //        AllBtnsDisable();
            //        //

            //        CancelAllTestBtnEnabled = true;
            //    }

            //    else if (stand.TestRun == TypeOfTestRun.CheckPortsReady)
            //    {
            //        TextCurrentTest = "Проверка портов ОК";

            //        //
            //        AllTabsEnable();
            //        AllBtnsEnable();
            //        //

            //        CancelAllTestBtnEnabled = false;
            //    }

            //    //-

            //    else if (stand.TestRun == TypeOfTestRun.WriteDevicesCmd)
            //    {
            //        TextCurrentTest = " Отправка на устройства";

            //        //
            //        AllTabsDisable();
            //        AllBtnsDisable();
            //        //

            //        CancelAllTestBtnEnabled = true;
            //    }

            //    else if (stand.TestRun == TypeOfTestRun.WriteDevicesCmdReady)
            //    {
            //        TextCurrentTest = "Отправка на устройства ОК";

            //        //
            //        AllTabsEnable();
            //        AllBtnsEnable();
            //        //

            //        CancelAllTestBtnEnabled = false;
            //    }

            //    //-

            else if (stand.TestRun == TypeOfTestRun.PrimaryCheckDevices)
            {
                TextCurrentTest = $"Предпроверка устройств: ";

                // //
                // AllTabsDisable();
                // AllBtnsDisable();
                // //
                //
                // CancelAllTestBtnEnabled = true;
                // PrimaryCheckDevicesTab = true;
            }

            //    else if (stand.TestRun == TypeOfTestRun.PrimaryCheckDevicesReady)
            //    {
            //        TextCurrentTest = " Предпроверка устройств ОК";
            //        TextCountTimes = "Всего попыток:";

            //        //
            //        AllTabsDisable();
            //        AllBtnsEnable();
            //        //

            //        CancelAllTestBtnEnabled = false;
            //        PrimaryCheckDevicesTab = true;
            //        PrimaryCheckVipsTab = true;
            //    }

            //    //-

            else if (stand.TestRun == TypeOfTestRun.PrimaryCheckVips)
            {
                TextCurrentTest = "Предпроверка Випов: ";
                // //
                // AllTabsDisable();
                // AllBtnsDisable();
                // //
                //
                // CancelAllTestBtnEnabled = true;
                // PrimaryCheckVipsTab = true;
            }

            //    else if (stand.TestRun == TypeOfTestRun.PrimaryCheckVipsReady)
            //    {
            //        TextCurrentTest = " Предпроверка Випов Ок";
            //        TextCountTimes = "Всего попыток:";

            //        //
            //        AllTabsDisable();
            //        AllBtnsEnable();
            //        //
            //        CancelAllTestBtnEnabled = false;
            //        PrimaryCheckDevicesTab = true;
            //        CheckVipsTab = true;
            //    }

            //    //-

            //    else if (stand.TestRun == TypeOfTestRun.DeviceOperation)
            //    {
            //        TextCurrentTest = $" Включение устройства";
            //        TextCountTimes = "Попытка включения:";
            //        AllTabsDisable();
            //        CheckVipsTab = true;
            //    }

            //    else if (stand.TestRun == TypeOfTestRun.DeviceOperationReady)
            //    {
            //        TextCurrentTest = " Включение устройства Ок";
            //        TextCountTimes = "Всего попыток:";
            //        AllTabsEnable();
            //    }

            //    //-

            else if (stand.TestRun == TypeOfTestRun.MeasurementZero)
            {
                TextCurrentTest = " Нулевой замер";
                // AllTabsDisable();
                // CheckVipsTab = true;
            }

            //    else if (stand.TestRun == TypeOfTestRun.MeasurementZeroReady)
            //    {
            //        TextCurrentTest = " Нулевой замер ОК";
            //        AllTabsEnable();
            //    }

            //    //-

            //    else if (stand.TestRun == TypeOfTestRun.WaitSupplyMeasurementZero)
            //    {
            //        TextCurrentTest = " Ожидание источника питания";
            //        AllTabsDisable();
            //        CheckVipsTab = true;
            //    }

            //    else if (stand.TestRun == TypeOfTestRun.WaitSupplyMeasurementZeroReady)
            //    {
            //        TextCurrentTest = " Ожидание источника питания ОК";
            //        AllTabsEnable();
            //    }

            //    //-

            //    else if (stand.TestRun == TypeOfTestRun.WaitHeatPlate)
            //    {
            //        TextCurrentTest = " Нагрев основания";
            //        AllTabsDisable();
            //        CheckVipsTab = true;
            //    }

            //    else if (stand.TestRun == TypeOfTestRun.WaitHeatPlateReady)
            //    {
            //        TextCurrentTest = " Нагрев основания ОК";
            //        AllTabsEnable();
            //    }

            //    //-

            //    else if (stand.TestRun == TypeOfTestRun.CyclicMeasurement)
            //    {
            //        TextCurrentTest = " Циклический замер";
            //        Al_lTabsDisable();
            //        CheckVipsTab = true;
            //    }

            //    else if (stand.TestRun == TypeOfTestRun.CycleWait)
            //    {
            //        TextCurrentTest = " Ожидание замер";
            //        AllTabsDisable();
            //        CheckVipsTab = true;
            //    }

            //    else if (stand.TestRun == TypeOfTestRun.CyclicMeasurementReady)
            //    {
            //        TextCurrentTest = " Циклический замеы закончены";
            //        AllTabsDisable();
            //        CheckVipsTab = true;
            //    }

            //-

            else if (stand.TestRun == TypeOfTestRun.Error)
            {
                TextCurrentTest = "Ошибка стенда";
                stand.CurrentCountChecked = string.Empty;
                stand.SubTestText = string.Empty;
                AllTabsEnable();
            }
        }
    }

    #endregion

    //--

    #region Управление --кнопками

    private bool deviceConfigEnabled = true;

    public bool DeviceConfigBtnEnabled
    {
        get => deviceConfigEnabled;
        set => Set(ref deviceConfigEnabled, value);
    }

    private bool startTestDevicesEnabled = true;

    public bool StartTestDevicesBtnEnabled
    {
        get => startTestDevicesEnabled;
        set => Set(ref startTestDevicesEnabled, value);
    }

    private bool cancelAllTestEnabled = false;

    public bool CancelAllTestBtnEnabled
    {
        get => cancelAllTestEnabled;
        set => Set(ref cancelAllTestEnabled, value);
    }

    private bool nextEnabled = true;

    public bool NextBtnEnabled
    {
        get => nextEnabled;
        set => Set(ref nextEnabled, value);
    }

    #endregion

    //--

    #endregion

    #region Поля --Подключение Устройств --0 tab

    #endregion

    #region Поля --Подключение Випов --1 tab

    private TypeVip selectTypeVip;

    /// <summary>
    /// Выбор типа Випа
    /// </summary>
    public TypeVip SelectTypeVip
    {
        get { return selectTypeVip; }

        set
        {
            if (!Set(ref selectTypeVip, value)) return;

            try
            {
                stand.SetTypeVips(SelectTypeVip);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
    }

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
            Prefix = selectDevice.Prefix;

            IsPrefix = selectDevice is RelayVip;

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


                CountChecked = stand.timeMachine.CountChecked;
                AllTimeChecked = stand.timeMachine.AllTimeChecked;
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

    private string prefix;

    public string Prefix
    {
        get => prefix;
        set { Set(ref prefix, value); }
    }

    private bool isPrefix;

    public bool IsPrefix
    {
        get => isPrefix;
        set { Set(ref isPrefix, value); }
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

    private bool isTransmitParam;

    public bool IsTransmitParam
    {
        get => isTransmitParam;
        set => Set(ref isTransmitParam, value);
    }

    private bool isReceiveParam;

    public bool IsReceiveParam
    {
        get => isReceiveParam;
        set => Set(ref isReceiveParam, value);
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

    private int lengthCmdLib;

    public int LengthCmdLib
    {
        get => lengthCmdLib;
        set => Set(ref lengthCmdLib, value);
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
            LengthCmdLib = Convert.ToInt32(SelectedCmdLib.Value.Length);

            //
            if (selectedCmdLib.Key.NameCmd.ToLower().Contains("set"))
            {
                IsTransmitParam = true;
                IsReceiveParam = false;
            }
            else if (selectedCmdLib.Key.NameCmd.ToLower().Contains("get"))
            {
                IsTransmitParam = false;
                IsReceiveParam = true;
            }
            else
            {
                IsTransmitParam = false;
                IsReceiveParam = false;
            }

            //
            XorIsHex = TypeMessageCmdLib == TypeCmd.Hex;
            //

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

    public Brush ProgressColor => stand.ProgressColor;

    public Brush ProgressResetColor => stand.ProgressResetColor;
    
    #endregion

    //--

    #endregion

    #region Поля --Настройки Типа Випов --3 tab

    private TypeVip selectTypeVipSettings;

    /// <summary>
    /// Выбор типа в в выпадающем списке
    /// </summary>
    public TypeVip SelectTypeVipSettings
    {
        get { return selectTypeVipSettings; }

        set
        {
            if (!Set(ref selectTypeVipSettings, value)) return;

            if (selectTypeVipSettings == null)
            {
                return;
            }

            try
            {
                TypeVipNameSettings = selectTypeVipSettings.Type;
                EnableTypeVipName = selectTypeVipSettings.EnableTypeVipName;
                PrepareMaxCurrentIn = selectTypeVipSettings.PrepareMaxCurrentIn.ToString(CultureInfo.InvariantCulture);
                MaxCurrentIn = selectTypeVipSettings.MaxCurrentIn.ToString(CultureInfo.InvariantCulture);
                PercentAccuracyCurrent =
                    selectTypeVipSettings.PercentAccuracyCurrent.ToString(CultureInfo.InvariantCulture);
                MaxVoltageOut1 = selectTypeVipSettings.PrepareMaxVoltageOut1.ToString(CultureInfo.InvariantCulture);
                MaxVoltageOut2 = selectTypeVipSettings.MaxVoltageOut2.ToString(CultureInfo.InvariantCulture);
                PercentAccuracyVoltages =
                    selectTypeVipSettings.PercentAccuracyVoltages.ToString(CultureInfo.InvariantCulture);

                FreqLoad = selectTypeVipSettings.GetDeviceParameters().BigLoadValues.Freq;
                AmplLoad = selectTypeVipSettings.GetDeviceParameters().BigLoadValues.Ampl;
                DcoLoad = selectTypeVipSettings.GetDeviceParameters().BigLoadValues.Dco;
                SquLoad = selectTypeVipSettings.GetDeviceParameters().BigLoadValues.Squ;
                OutputOnLoad = selectTypeVipSettings.GetDeviceParameters().BigLoadValues.OutputOn;
                OutputOffLoad = selectTypeVipSettings.GetDeviceParameters().BigLoadValues.OutputOff;

                OutputOnHeat = selectTypeVipSettings.GetDeviceParameters().HeatValues.OutputOn;
                OutputOffHeat = selectTypeVipSettings.GetDeviceParameters().HeatValues.OutputOff;

                VoltageSupply = selectTypeVipSettings.GetDeviceParameters().SupplyValues.Voltage;
                CurrentSupply = selectTypeVipSettings.GetDeviceParameters().SupplyValues.Current;
                OutputOnSupply = selectTypeVipSettings.GetDeviceParameters().SupplyValues.OutputOn;
                OutputOffSupply = selectTypeVipSettings.GetDeviceParameters().SupplyValues.OutputOff;

                CurrentMeterCurrentMax = selectTypeVipSettings.GetDeviceParameters().ThermoCurrentValues.CurrMaxLimit;
                TermocoupleType = selectTypeVipSettings.GetDeviceParameters().ThermoCurrentValues.TermocoupleType;
                OutputOnThermoCurrent = selectTypeVipSettings.GetDeviceParameters().ThermoCurrentValues.OutputOn;
                OutputOffThermoCurrent = selectTypeVipSettings.GetDeviceParameters().ThermoCurrentValues.OutputOff;

                VoltMeterVoltMax = selectTypeVipSettings.GetDeviceParameters().VoltValues.VoltMaxLimit;
                OutputOnVoltMeter = selectTypeVipSettings.GetDeviceParameters().VoltValues.OutputOn;
                OutputOffVoltmeter = selectTypeVipSettings.GetDeviceParameters().VoltValues.OutputOff;


                //обновление типа випа выбранного
                selectedTypeVips.Source = SelectTypeVipSettings?.Type;
                OnPropertyChanged(nameof(SelectedTypeVips));
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
    }

    public string TypeVipName { get; set; }
    private readonly CollectionViewSource selectedTypeVips = new();

    /// <summary>
    /// Для показа/обновление типа випов
    /// </summary>
    public ICollectionView? SelectedTypeVips => selectedTypeVips?.View;

    private string typeVipNameSettings;

    public string TypeVipNameSettings
    {
        get => typeVipNameSettings;
        set => Set(ref typeVipNameSettings, value);
    }

    private bool enableTypeVipName;

    public bool EnableTypeVipName
    {
        get => enableTypeVipName;
        set => Set(ref enableTypeVipName, value);
    }

    private string repareMaxCurrentIn;

    public string PrepareMaxCurrentIn
    {
        get => repareMaxCurrentIn;
        set => Set(ref repareMaxCurrentIn, value);
    }

    private string maxCurrentIn;

    public string MaxCurrentIn
    {
        get => maxCurrentIn;
        set => Set(ref maxCurrentIn, value);
    }

    private string percentAccuracyCurrent;

    public string PercentAccuracyCurrent
    {
        get => percentAccuracyCurrent;
        set => Set(ref percentAccuracyCurrent, value);
    }

    private string maxVoltageOut1;

    public string MaxVoltageOut1
    {
        get => maxVoltageOut1;
        set => Set(ref maxVoltageOut1, value);
    }

    private string maxVoltageOut2;

    public string MaxVoltageOut2
    {
        get => maxVoltageOut2;
        set
        {
            if (string.IsNullOrWhiteSpace(maxVoltageOut2))
            {
                voltageOuе2Using = false;
                Set(ref maxVoltageOut2, value);
            }
            else
            {
                voltageOuе2Using = true;
                Set(ref maxVoltageOut2, value);
            }
        }
    }

    private bool voltageOuе2Using;
    private string percentAccuracyVoltages;

    public string PercentAccuracyVoltages
    {
        get => percentAccuracyVoltages;
        set => Set(ref percentAccuracyVoltages, value);
    }

    private int currentTypeVipSettings;

    public int CurrentTypeVipSettings
    {
        get => currentTypeVipSettings;
        set => Set(ref currentTypeVipSettings, value);
    }

    private string freqLoad;

    /// <summary>
    /// 
    /// </summary>
    public string FreqLoad
    {
        get => freqLoad;
        set => Set(ref freqLoad, value);
    }

    private string amplLoad;

    /// <summary>
    /// 
    /// </summary>
    public string AmplLoad
    {
        get => amplLoad;
        set => Set(ref amplLoad, value);
    }

    private string dcoLoad;

    /// <summary>
    /// 
    /// </summary>
    public string DcoLoad
    {
        get => dcoLoad;
        set => Set(ref dcoLoad, value);
    }

    private string squLoad;

    /// <summary>
    /// 
    /// </summary>
    public string SquLoad
    {
        get => squLoad;
        set => Set(ref squLoad, value);
    }

    private string outputOnLoad;

    /// <summary>
    /// 
    /// </summary>
    public string OutputOnLoad
    {
        get => outputOnLoad;
        set => Set(ref outputOnLoad, value);
    }

    private string outputOffLoad;

    /// <summary>
    /// 
    /// </summary>
    public string OutputOffLoad
    {
        get => outputOffLoad;
        set => Set(ref outputOffLoad, value);
    }

    private string outputOnHeat;

    /// <summary>
    /// 
    /// </summary>
    public string OutputOnHeat
    {
        get => outputOnHeat;
        set => Set(ref outputOnHeat, value);
    }

    private string outputOffHeat;

    /// <summary>
    /// 
    /// </summary>
    public string OutputOffHeat
    {
        get => outputOffHeat;
        set => Set(ref outputOffHeat, value);
    }

    private string voltageSupply;

    /// <summary>
    /// 
    /// </summary>
    public string VoltageSupply
    {
        get => voltageSupply;
        set => Set(ref voltageSupply, value);
    }

    private string currentSupply;

    /// <summary>
    /// 
    /// </summary>
    public string CurrentSupply
    {
        get => currentSupply;
        set => Set(ref currentSupply, value);
    }

    private string outputOnSupply;

    /// <summary>
    /// 
    /// </summary>
    public string OutputOnSupply
    {
        get => outputOnSupply;
        set => Set(ref outputOnSupply, value);
    }

    private string outputOffSupply;

    /// <summary>
    /// 
    /// </summary>
    public string OutputOffSupply
    {
        get => outputOffSupply;
        set => Set(ref outputOffSupply, value);
    }

    private string outputOnThermo;

    /// <summary>
    /// 
    /// </summary>
    public string OutputOnThermoCurrent
    {
        get => outputOnThermo;
        set => Set(ref outputOnThermo, value);
    }

    private string outputOffThermo;

    /// <summary>
    /// 
    /// </summary>
    public string OutputOffThermoCurrent
    {
        get => outputOffThermo;
        set => Set(ref outputOffThermo, value);
    }

    private string voltMeterVoltMax;

    /// <summary>
    /// 
    /// </summary>
    public string VoltMeterVoltMax
    {
        get => voltMeterVoltMax;
        set => Set(ref voltMeterVoltMax, value);
    }

    private string currentMeterCurrentMax;

    /// <summary>
    /// 
    /// </summary>
    public string CurrentMeterCurrentMax
    {
        get => currentMeterCurrentMax;
        set => Set(ref currentMeterCurrentMax, value);
    }


    private string termocoupleType;

    /// <summary>
    /// 
    /// </summary>
    public string TermocoupleType
    {
        get => termocoupleType;
        set => Set(ref termocoupleType, value);
    }

    private string outputOnVoltmerer;

    /// <summary>
    /// 
    /// </summary>
    public string OutputOnVoltMeter
    {
        get => outputOnVoltmerer;
        set => Set(ref outputOnVoltmerer, value);
    }

    private string outputOffVoltmeter;

    /// <summary>
    /// 
    /// </summary>
    public string OutputOffVoltmeter
    {
        get => outputOffVoltmeter;
        set => Set(ref outputOffVoltmeter, value);
    }

    #endregion

    #endregion

    //---
}