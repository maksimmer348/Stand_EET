using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace StandETT;

public class ViewModel : Notify, IDataErrorInfo, INotifyDataErrorInfo
{
    //---

    #region --Модель

    //--

    #region --Стенд

    public Stand1 stand = new();

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

        stand.timerErrorMeasurement += TimerErrorMeasurement;
        stand.timerErrorDevice += TimerErrorDevice;
        stand.timerOk += TimerOkMeasurement;

        #endregion

        #endregion

        //TODO убрать когда допишу функцинал отключения влкадок режимами прогверки 
        AllBtnsEnable();
        AllTabsEnable();
        SelectTab = 0;
        //TODO убрать когда допишу функцинал отключения влкадок режимами прогверки 
    }

    //Именно посредством него View получает уведомления, что во VM что-то изменилось и требуется обновить данные.
    private void StandTestOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(e.PropertyName);

        if (e.PropertyName == nameof(TestRun))
        {
            TestRun = stand.TestRun;
        }

        if (e.PropertyName == nameof(RelaySwitch))
        {
            RelaySwitch = stand.RelaySwitch;
        }

        if (e.PropertyName == nameof(TemperatureCurrentIn))
        {
            TemperatureCurrentIn = stand.TemperatureCurrentIn;
        }

        if (e.PropertyName == nameof(TemperatureCurrentOut))
        {
            TemperatureCurrentOut = stand.TemperatureCurrentOut;
        }
    }

    private double selectTab;

    //--selecttab--tab
    /// <summary>
    /// Какая сейчас выбрана вкладка
    /// </summary>
    public double SelectTab
    {
        get
        {
            NextBtnEnabled = true;

            if (selectTab == 0)
            {
            }
            else if (selectTab == 1)
            {
            }
            else if (selectTab == 2)
            {
            }
            else if (selectTab == 3)
            {
            }
            else if (selectTab == 4)
            {
            }
            else if (selectTab == 5)
            {
                BackButtonText = "Назад";
                NextBtnEnabled = false;
            }

            return selectTab;
        }
        set => Set(ref selectTab, value);
    }

    public double goToSelectTab;

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
        DeviceConfigBtnEnabled = true;
        StartTestDevicesBtnEnabled = true;
        CloseActionWindowBtnEnabled = true;
        CancelAllTestBtnEnabled = true;
    }

    /// <summary>
    /// Выключить все кнопки 
    /// </summary>
    void AllBtnsDisable()
    {
        DeviceConfigBtnEnabled = false;
        StartTestDevicesBtnEnabled = false;
        CloseActionWindowBtnEnabled = false;
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
        return true;
    }

    //--reset--allreset
    /// <summary>
    /// Команда ОТМЕНИТЬ испытания
    /// </summary>
    public ICommand CancelAllTestCmd { get; }

    async Task OnCancelAllTestCmdExecuted(object p)
    {
        CancelAllTestBtnEnabled = false;

        await stand.ResetAllTests();

        CancelAllTestBtnEnabled = true;
    }

    private void AwOnClosed(object sender, EventArgs e)
    {
        WindowDisabled = true;
    }

    private void VewOnClosed(object sender, EventArgs e)
    {
        WindowVipDisabled = true;
    }

    bool CanCancelAllTestCmdExecuted(object p)
    {
        return true;
    }

    private void TimerErrorMeasurement(string message)
    {
        Application.Current.Dispatcher.BeginInvoke(new Action(async () =>
        {
            await stand.ResetAllTests();

            const string caption = "Тесты завершены с ошибкой замеров!";
            MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
        }));
    }

    private void TimerErrorDevice(string message)
    {
        Application.Current.Dispatcher.BeginInvoke(new Action(async () =>
        {
            await stand.ResetAllTests();

            const string caption = "Тесты завершены с ошибкой устройств!";
            var result = MessageBox.Show(message + "Перейти в настройки устройств?", caption, MessageBoxButton.OK,
                MessageBoxImage.Error);
        }));
    }

    private void TimerOkMeasurement(string message)
    {
        Application.Current.Dispatcher.BeginInvoke(new Action(async () =>
        {
            await stand.ResetAllTests();

            const string caption = "Тесты завершены без ошибок!";
            MessageBox.Show(message, caption, MessageBoxButton.OK);
        }));
    }

    /// <summary>
    /// Команда ЗАПУСТИТЬ исптания
    /// </summary>
    public ICommand StartTestDevicesCmd { get; }

    async Task OnStartTestDevicesCmdExecuted(object p)
    {
        bool available = false;

        if (SelectTab == 0)
        {
            try
            {
                //--device
                await stand.PrimaryCheckDevices(Convert.ToInt32(CountChecked), Convert.ToInt32(AllTimeChecked));
            }
            catch (Exception e)
            {
                string caption = "Ошибка предварительной проверки устройств";
                var result = MessageBox.Show(e.Message + "Перейти в настройки устройств?", caption,
                    MessageBoxButton.YesNo, MessageBoxImage.Error);

                await stand.ResetAllTests(true);
                
                if (result == MessageBoxResult.No)
                {
                    goToSelectTab = 0;
                }

                if (result == MessageBoxResult.Yes)
                {
                    goToSelectTab = 3;
                }
            }
        }
        else if (SelectTab == 1)
        {
            try
            {
                //--vips
                await stand.PrimaryCheckVips(Convert.ToInt32(CountChecked), Convert.ToInt32(AllTimeChecked));
            }
            //Exception отсуствутют или дублируются номера Випов
            catch (Exception e) when (e.Message.ToLower().Contains("номера") || e.Message.ToLower().Contains("тип"))
            {
                const string caption = "Ошибка предварительной проверки реле Випов";
                MessageBox.Show(e.Message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            //Exception создания отчета
            catch (Exception e) when (e.Message.ToLower().Contains("отчет"))
            {
                const string caption = "Ошибка создания отчета";
                MessageBox.Show(e.Message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            //Exception общий сбой пердварительной проверки
            catch (Exception e)
            {
                const string caption = "Ошибка предварительной проверки реле Випов";
                var result = MessageBox.Show(e.Message + "Перейти в настройки устройств?", caption,
                    MessageBoxButton.YesNo, MessageBoxImage.Error);

                await stand.ResetAllTests(true);
                
                if (result == MessageBoxResult.No)
                {
                    goToSelectTab = 1;
                }

                if (result == MessageBoxResult.Yes)
                {
                    goToSelectTab = 3;
                }
            }
        }
        else if (SelectTab == 2)
        {
            try
            {
                //TODO удалить после отладки
                await stand.AvailabilityCheckVip();
                await stand.MeasurementZero();
                await stand.PrepareMeasurementCycle();
                stand.StartMeasurementCycle();
                return;
                //TODO удалить после отладки

                //--available
                available = await stand.AvailabilityCheckVip();
                if (available)
                {
                    //--zero
                    bool mesZero = await stand.MeasurementZero();

                    if (mesZero)
                    {
                        var prepare = await stand.PrepareMeasurementCycle();

                        if (prepare)
                        {
                            //--mesaure--cycle
                            stand.StartMeasurementCycle();
                        }
                    }
                }
            }
            //неверные значения преварительной проверки Випов
            catch (Exception e) when (e.Message.Contains("Предварительные"))
            {
                const string caption = "Ошибка предварительной проверки Випов";
                var errorStr = e.Message.Replace("/", "\n ");
                var result = MessageBox.Show(errorStr + "Перейти в настройки типов Випов?", caption,
                    MessageBoxButton.YesNo, MessageBoxImage.Error);

                if (!stand.IsResetAll)
                {
                    await stand.ResetAllTests();
                }

                goToSelectTab = result switch
                {
                    MessageBoxResult.Yes => 4,
                    MessageBoxResult.No => errorStr.Contains("Реле Випа") ? 1 : 0,
                    _ => goToSelectTab
                };
            }
            //ошибка на этапе 0 замера / НКУ
            catch (Exception e) when (e.Message.Contains("НКУ"))
            {
                const string caption = "Ошибка НКУ Випов";
                var errorStr = e.Message.Replace("/", "\n ");
                var result = MessageBox.Show(errorStr + " Перейти в настройки?", caption, MessageBoxButton.YesNo,
                    MessageBoxImage.Error);

                if (!stand.IsResetAll)
                {
                    await stand.ResetAllTests();
                }

                goToSelectTab = result switch
                {
                    MessageBoxResult.Yes => 3,
                    MessageBoxResult.No => 1, //errorStr.Contains("Реле Випа") ? 1 : 0;
                    _ => goToSelectTab
                };
            }
            //неверные значения преварительной проверки температуры
            catch (Exception e) when (e.Message.Contains("температура"))
            {
                const string caption = "Ошибка температуры";
                var errorStr = e.Message.Replace("/", "\n ");
                var result = MessageBox.Show(errorStr + "Проверте модуль температуры \n" + " Перейти в настройки?",
                    caption,
                    MessageBoxButton.YesNo, MessageBoxImage.Error);

                if (!stand.IsResetAll)
                {
                    await stand.ResetAllTests(true);
                }

                if (result == MessageBoxResult.Yes)
                {
                    goToSelectTab = 4;
                }

                if (result == MessageBoxResult.No)
                {
                    goToSelectTab = 1;
                }
            }
            //отстувуют инициализированые випы тк все они отсеялись во время преварительной проверки Випов
            catch (Exception e) when (e.Message.Contains("Отсутвуют инициализировнные"))
            {
                string caption = "Инициализируйте Випы!";
                var result = MessageBox.Show(e.Message, caption, MessageBoxButton.OK, MessageBoxImage.Information);

                if (result is MessageBoxResult.OK)
                {
                    if (!stand.IsResetAll)
                    {
                        await stand.ResetAllTests();
                    }

                    goToSelectTab = 1;
                }
            }
            catch (Exception e)
            {
                const string caption = "Ошибка стенда";
                var errorStr = e.Message.Replace("/", "\n ");
                var result = MessageBox.Show(errorStr + " Перейти в настройки?", caption, MessageBoxButton.YesNo,
                    MessageBoxImage.Error);

                if (result == MessageBoxResult.No)
                {
                    if (!stand.IsResetAll)
                    {
                        await stand.ResetAllTests();
                    }

                    goToSelectTab = errorStr.Contains("Реле Випа") ? 1 : 0;
                }

                if (result == MessageBoxResult.Yes)
                {
                    if (!stand.IsResetAll)
                    {
                        await stand.ResetAllTests();
                    }

                    goToSelectTab = 3;
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
        // //обработчик команды
        // if (SelectTab == 0)
        // {
        //     SelectTab = 3;
        //     // BackButtonText = "Назад";
        // }
        // else if (SelectTab == 3)
        // {
        //     SelectTab = 0;
        //     // BackButtonText = "Откыть конфиг";
        // }
        // //
        // else if (SelectTab == 1)
        // {
        //     SelectTab = 4;
        //     // BackButtonText = "Назад";
        // }
        // else if (SelectTab == 4)
        // {
        //     SelectTab = 1;
        //     // BackButtonText = "Откыть конфиг";
        // }

        if (stand.TestRun == TypeOfTestRun.Stop)
        {
            SelectTab = 0;
        }

        return Task.CompletedTask;
    }

    bool CanDeviceConfigCmdExecuted(object p)
    {
        return true;
    }

    bool StopAll { get; set; }

    /// <summary>
    /// Команда закрыть окно
    /// </summary>
    public ICommand CloseActionWindowCmd { get; }

    Task OnCloseActionWindowCmdExecuted(object p)
    {
        StopAll = true;
        SelectTab = goToSelectTab;
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
            AllDevices[index].Config.Baud = Convert.ToInt32(Baud);
            AllDevices[index].Config.StopBits = Convert.ToInt32(StopBit);
            AllDevices[index].Config.Parity = Parity;
            AllDevices[index].Config.DataBits = Convert.ToInt32(DataBit);
            AllDevices[index].Config.Dtr = Dtr;

            NameDevice = selectDevice.Name;
            Prefix = selectDevice.Prefix;

            if (AllDevices[index] is not RelayVip)
            {
                PortName = selectDevice.GetConfigDevice().PortName;
                Baud = selectDevice.GetConfigDevice().Baud.ToString();
                StopBit = selectDevice.GetConfigDevice().StopBits.ToString();
                Parity = selectDevice.GetConfigDevice().Parity;
                DataBit = selectDevice.GetConfigDevice().DataBits.ToString();
                Dtr = selectDevice.GetConfigDevice().Dtr;
            }

            AllDevices[index].SetConfigDevice(TypePort.SerialInput, PortName, Convert.ToInt32(Baud),
                Convert.ToInt32(StopBit), Parity,
                Convert.ToInt32(DataBit), Dtr);

            selectedDeviceCmd.Source =
                SelectDevice?.LibCmd.DeviceCommands.Where(x =>
                    x.Key.NameDevice == selectDevice.Name);
            OnPropertyChanged(nameof(SelectedDeviceCmd));

            stand.SerializeDevice();


            //TODO добавит когда повяться время
            // stand.timeMachine.CountChecked = CountChecked;
            // stand.timeMachine.AllTimeChecked = AllTimeChecked;
            //stand.SerializeTime();
            //TODO добавит когда повяться время
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message);
        }

        IsSaveDeviceMessage = "Изменения сохранены";
        return Task.CompletedTask;
    }


    bool CanSaveSettingsCmdExecuted(object p)
    {
        return Error == null; //|| !Error.Contains("устройств");
    }

    /// <summary>
    /// Команда добавить команду к устройству
    /// </summary>
    public ICommand AddCmdFromDeviceCmd { get; }

    private TypeCmd typeCmd = TypeCmd.None;

    Task OnAddCmdFromDeviceCmdExecuted(object p)
    {
        try
        {
            //получение текущего индекса
            var index = IndexSelectCmd;

            ConvertTypeCmdToEnum(TypeMessageCmdLib);

            libCmd.AddCommand(NameCmdLib, SelectDevice.Name, TransmitCmdLib, Convert.ToInt32(DelayCmdLib),
                ReceiveCmdLib, IsTransmitParam,
                terminator: SelectTerminatorTransmit.Type, receiveTerminator: SelectTerminatorReceive.Type,
                type: typeCmd, isXor: IsXor, length: LengthCmdLib);

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

        RemoveCmdFromDeviceBtnEnabled = true;

        return Task.CompletedTask;
    }

    private void ConvertTypeCmdToEnum(string typeCmd)
    {
        this.typeCmd = typeCmd switch
        {
            "None" => TypeCmd.None,
            "Text" => TypeCmd.Text,
            "Hex" => TypeCmd.Hex,
            _ => this.typeCmd
        };
    }

    private void ConvertTypeCmdToString(TypeCmd typeCmd)
    {
        if (typeCmd == TypeCmd.None)
        {
            TypeMessageCmdLib = "None";
        }

        if (typeCmd == TypeCmd.Text)
        {
            TypeMessageCmdLib = "Text";
        }

        if (typeCmd == TypeCmd.Hex)
        {
            TypeMessageCmdLib = "Hex";
        }
    }

    bool CanAddCmdFromDeviceCmdExecuted(object p)
    {
        return Error == null; //|| !Error.Contains("команд");
        // return true;
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

    //-

    #region Команды --Настройки Типа Випов --3 tab

    /// <summary>
    /// Команда ДОБАВИТЬ тип випа
    /// </summary>
    public ICommand SaveTypeVipSettingsCmd { get; }

    Task OnSaveTypeVipSettingsCmdExecuted(object p)
    {
        var typeConfig = new TypeVip();

        typeConfig.Name = TypeVipNameSettings;


        typeConfig.PrepareMaxCurrentIn = ConvertValToVip(PrepareMaxCurrentIn);

        typeConfig.AvailabilityMaxCurrentIn = ConvertValToVip(AvailabilityMaxCurrentIn);

        typeConfig.MaxCurrentIn = ConvertValToVip(MaxCurrentIn);
        typeConfig.PercentAccuracyCurrent = ConvertValToVip(PercentAccuracyCurrent);
        typeConfig.MaxVoltageOut1 = ConvertValToVip(MaxVoltageOut1);
        typeConfig.MaxVoltageOut2 = ConvertValToVip(MaxVoltageOut2);
        typeConfig.PercentAccuracyVoltages = ConvertValToVip(PercentAccuracyVoltages);
        typeConfig.PercentAccuracyTemperature = ConvertValToVip(PercentAccuracyTemperature);


        typeConfig.MaxTemperatureIn = ConvertValToVip(TemperatureIn);
        typeConfig.MaxTemperatureOut = ConvertValToVip(TemperatureOut);


        typeConfig.ZeroTestInterval = ZeroTestInterval;

        typeConfig.TestFirstIntervalTime = TestFirstIntervalTime;
        typeConfig.TestIntervalTime = TestIntervalTime;
        typeConfig.TestAllTime = TestAllTime;

        typeConfig.VoltageOut2Using = voltageOuе2Using;

        // typeConfig.SetTestAllTime = voltageOuе2Using;
        // typeConfig.VoltageOut2Using = voltageOuе2Using;
        // typeConfig.VoltageOut2Using = voltageOuе2Using;


        typeConfig.SetDeviceParameters(new DeviceParameters()
        {
            BigLoadValues = new BigLoadValues(FreqLoad, AmplLoad, DcoLoad, SquLoad, OutputOnLoad, OutputOffLoad),

            HeatValues = new HeatValues(OutputOnHeat, OutputOffHeat),

            SupplyValues = new SupplyValues(VoltageSupply, CurrentSupply, VoltageAvailabilitySupply,
                CurrentAvailabilitySupply, OutputOnSupply, OutputOffSupply),

            VoltCurrentValues = new VoltCurrentMeterValues(CurrentMeterCurrentMax, VoltCurrentMeterVoltMax,
                TermocoupleType, ShuntResistance, OutputOnThermoCurrent, OutputOffThermoCurrent),

            VoltValues = new VoltMeterValues(VoltMeterVoltMax, OutputOnVoltMeter, OutputOffVoltmeter)
        });

        stand.AddTypeVips(typeConfig);

        selectedTypeVips.Source = SelectTypeVipSettings?.Name;
        OnPropertyChanged(nameof(SelectedTypeVips));

        stand.SerializeTypeVips();
        CurrentTypeVipSettings = cfgTypeVips.TypeVips.IndexOf(typeConfig);

        return Task.CompletedTask;
    }

    bool CanSaveTypeVipSettingsCmdExecuted(object p)
    {
        return Error == null ||
               !Error.Contains("параметров") && !Error.Contains("Випа") && !Error.Contains("документации");
    }

    private decimal ConvertValToVip(string s)
    {
        return string.IsNullOrWhiteSpace(s) ? 0m : Convert.ToDecimal(s.Replace(',', '.'));
    }


    /// <summary>
    /// Команда УДАЛИТЬ тип випа
    /// </summary>
    public ICommand RemoveTypeVipSettingsCmd { get; }

    Task OnRemoveTypeVipSettingsCmdExecuted(object p)
    {
        var index = cfgTypeVips.TypeVips.IndexOf(SelectTypeVipSettings);

        stand.RemoveTypeVips(SelectTypeVipSettings);

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
            AvailabilityMaxCurrentIn = null;

            MaxCurrentIn = null;
            PercentAccuracyCurrent = null;
            MaxVoltageOut1 = null;
            MaxVoltageOut2 = null;
            PercentAccuracyVoltages = null;
            PercentAccuracyTemperature = null;

            TemperatureIn = null;
            TemperatureOut = null;

            ZeroTestInterval = 0;

            TestFirstIntervalTime = TimeSpan.Zero;
            TestIntervalTime = TimeSpan.Zero;
            TestAllTime = TimeSpan.Zero;

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

            CurrentAvailabilitySupply = null;
            VoltageAvailabilitySupply = null;

            OutputOnSupply = null;
            OutputOffSupply = null;

            CurrentMeterCurrentMax = null;
            TermocoupleType = null;
            OutputOnThermoCurrent = null;
            OutputOffThermoCurrent = null;
            ShuntResistance = null;

            VoltMeterVoltMax = null;
            VoltCurrentMeterVoltMax = null;

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

    private bool windowVipDisabled = true;

    public bool WindowVipDisabled
    {
        get => windowVipDisabled;
        set => Set(ref windowVipDisabled, value);
    }

    private string backButtonText = "Открыть\nконфиг";

    public string BackButtonText
    {
        get => backButtonText;
        set => Set(ref backButtonText, value);
    }


    /// <summary>
    ///
    /// </summary>
    public string CaptionAction => stand.CaptionAction;

    /// <summary>
    /// Текст ошибки
    /// </summary>
    public string ErrorMessage => stand.ErrorMessage;

    /// <summary>
    /// Текст ошибки выхода устрокйства
    /// </summary>
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
    /// Включатель влкадки настроек 3
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
    /// Текст сколько процентов текущего теста прошло
    /// </summary>
    public double PercentCurrentTest => stand.PercentCurrentTest;

    /// <summary>
    /// Текст сколько процентов текущего сабтеста прошло
    /// </summary>
    public double PercentCurrentSubTest => stand.PercentCurrentSubTest;

    /// <summary>
    /// Текст сколько процентов текущего теста прошло
    /// </summary>
    public double PercentCurrentReset => stand.PercentCurrentReset;

    /// <summary>
    /// Текст текстом какое устройство проходит тест
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

                // if (stand.TestCurrentDevice == null)
                //     return string.Empty;
                return "Устройство: " + stand.TestCurrentDevice.IsDeviceType + " " + stand.TestCurrentDevice.Name;
            }
            else
            {
                if (stand.TestCurrentVip != null)
                {
                    return "Вип: " + stand.TestCurrentVip.IsDeviceType + " " + stand.TestCurrentVip.Name;
                }
            }

            return null;
        }
    }

    private string textCurrentTest;

    /// <summary>
    /// Текст этап тестов
    /// </summary>
    public string TextCurrentTest
    {
        get => textCurrentTest;
        set => Set(ref textCurrentTest, value);
    }

    /// <summary>
    /// Текст субтест
    /// </summary>
    public string SubTestText => stand.SubTestText;

    /// <summary>
    /// Текст текущее количество проверок
    /// </summary>
    public string CurrentCountChecked => stand.CurrentCountChecked;

    private string countTimes = "3";

    /// <summary>
    /// Текст количество перепроверок
    /// </summary>
    public string CountChecked
    {
        get => countTimes;
        set => Set(ref countTimes, value);
    }

    private string allTimeChecked = "3000";

    /// <summary>
    /// Текстбокс времени проверки 
    /// </summary>
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

    private bool relaySwitch;

    public bool RelaySwitch
    {
        get => relaySwitch;
        set
        {
            if (!Set(ref relaySwitch, value)) return;
            {
                if (stand.RelaySwitch)
                {
                    CancelAllTestBtnEnabled = false;
                }
                else
                {
                    CancelAllTestBtnEnabled = true;
                }
            }
        }
    }

    //--run--tr--testrun--
    private TypeOfTestRun testRun = TypeOfTestRun.Stop;

    /// <summary>
    /// Уведомляет о проводимом тесте прееключает вкладки, выключает кнопки
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
                BackButtonText = "Назад";

                //AllTabsDisable();
                PrimaryCheckDevicesTab = true;

                AllBtnsEnable();
                CancelAllTestBtnEnabled = false;
            }

            else if (stand.TestRun == TypeOfTestRun.Stopped)
            {
                TextCurrentTest = "Тесты прерваны, отключение устройств... ";

                StopAll = false;
                SelectTab = 5;


                // AllTabsDisable();
                // PrimaryCheckDevicesTab = true;

                AllBtnsDisable();
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
                TextCurrentTest = "Нулевой замер: ";
                // AllTabsDisable();
                // CheckVipsTab = true;
            }

            else if (stand.TestRun == TypeOfTestRun.MeasurementZeroReady)
            {
                TextCurrentTest = " Нулевой замер ОК";
                //AllTabsEnable();
            }

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

            else if (stand.TestRun == TypeOfTestRun.CyclicMeasurement)
            {
                TextCurrentTest = " Циклические замеры: ";
                // AllTabsDisable();
                // CheckVipsTab = true;
            }
            else if (stand.TestRun == TypeOfTestRun.CyclicMeasurementReady)
            {
                TextCurrentTest = " Циклическиe замеры закончены";
                // AllTabsDisable();
                // CheckVipsTab = true;
            }

            //    else if (stand.TestRun == TypeOfTestRun.CycleWait)
            //    {
            //        TextCurrentTest = " Ожидание замер";
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


    private bool closeActionWindowBtnEnabled;

    public bool CloseActionWindowBtnEnabled
    {
        get => closeActionWindowBtnEnabled;
        set => Set(ref closeActionWindowBtnEnabled, value);
    }


    private bool nextEnabled = true;

    public bool NextBtnEnabled
    {
        get => nextEnabled;
        set => Set(ref nextEnabled, value);
    }


    private bool removeCmdFromDeviceBtnEnabled;

    public bool RemoveCmdFromDeviceBtnEnabled
    {
        get => removeCmdFromDeviceBtnEnabled;
        set => Set(ref removeCmdFromDeviceBtnEnabled, value);
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

            IsPrefix = selectDevice is RelayVip;
            NameDevice = selectDevice.Name;
            Prefix = selectDevice.Prefix;

            IsSaveDeviceMessage = "";

            try
            {
                PortName = selectDevice.GetConfigDevice().PortName.Replace("COM", "");
                Baud = selectDevice.GetConfigDevice().Baud.ToString();
                StopBit = selectDevice.GetConfigDevice().StopBits.ToString();
                Parity = selectDevice.GetConfigDevice().Parity;
                DataBit = selectDevice.GetConfigDevice().DataBits.ToString();
                Dtr = selectDevice.GetConfigDevice().Dtr;

                var cmds = value?.LibCmd.DeviceCommands.Where(x =>
                    x.Key.NameDevice == selectDevice.Name);
                //обновление команд выбранного устройства
                selectedDeviceCmd.Source = cmds;

                if (cmds == null || !cmds.ToList().Any())
                {
                    RemoveCmdFromDeviceBtnEnabled = false;
                }
                else
                {
                    RemoveCmdFromDeviceBtnEnabled = true;
                }


                OnPropertyChanged(nameof(SelectedDeviceCmd));
                OnPropertyChanged(nameof(IndexTerminatorReceive));
                OnPropertyChanged(nameof(IndexTerminatorTransmit));

                // CountChecked = stand.timeMachine.CountChecked;
                // AllTimeChecked = stand.timeMachine.AllTimeChecked;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
    }

    public string nameDevice;

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

    private string baud;

    /// <summary>
    /// Baud rate порта в текстбоксе 
    /// </summary>
    public string Baud
    {
        get => baud;
        set => Set(ref baud, value);
    }

    private string stopBit;

    /// <summary>
    /// Стоповые биты порта в текстбоксе
    /// </summary>
    public string StopBit
    {
        get => stopBit;
        set => Set(ref stopBit, value);
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

    private string dataBit;

    /// <summary>
    /// Бит данных в команде в текстбоксе
    /// </summary>
    public string DataBit
    {
        get => dataBit;
        set => Set(ref dataBit, value);
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

    private string isSaveDevice;

    /// <summary>
    ///  Уведомление о сохранениии настроек
    /// </summary>
    public string IsSaveDeviceMessage
    {
        get => isSaveDevice;
        set => Set(ref isSaveDevice, value);
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
                if (SelectTerminatorTransmit != null)
                {
                    var item = Terminators.First(x =>
                        x.Type == SelectTerminatorTransmit.Type && x.TypeEncod == SelectTerminatorTransmit.TypeEncod);
                    indexTerminatorTransmit = Terminators.IndexOf(item);
                }
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
                if (SelectTerminatorReceive != null)
                {
                    var item = Terminators.FirstOrDefault(x =>
                        x.Type == SelectTerminatorReceive.Type && x.TypeEncod == SelectTerminatorReceive.TypeEncod);
                    indexTerminatorReceive = Terminators.IndexOf(item);
                }
            }
            catch (Exception e)
            {
                indexTerminatorTransmit = 0;
            }

            return indexTerminatorReceive;
        }
        set => Set(ref indexTerminatorReceive, value);
    }

    private string typeMessageCmdLib = "None";

    /// <summary>
    /// Тип отправялемемой и принимаемой команды из библиотеки
    /// </summary>
    public string TypeMessageCmdLib
    {
        get => typeMessageCmdLib;
        set => Set(ref typeMessageCmdLib, value);
    }

    private string delayCmdLib;

    /// <summary>
    /// ЗАдержка на после отправки команды до ее приема из библиотеки
    /// </summary>
    public string DelayCmdLib
    {
        get => delayCmdLib;
        set => Set(ref delayCmdLib, value);
    }

    private string lengthCmdLib;

    public string LengthCmdLib
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

            ConvertTypeCmdToString(SelectedCmdLib.Value.MessageType);

            DelayCmdLib = SelectedCmdLib.Value.Delay.ToString();
            LengthCmdLib = SelectedCmdLib.Value.Length;

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
            XorIsHex = TypeMessageCmdLib == "Hex";
            //

            OnPropertyChanged(nameof(SelectedCmdLib));
            OnPropertyChanged(nameof(IndexTerminatorTransmit));
            OnPropertyChanged(nameof(IndexTerminatorReceive));
        }
    }

    private int indexSelectCmd = 1;

    public int IndexSelectCmd
    {
        get => indexSelectCmd;
        set => Set(ref indexSelectCmd, value);
    }

    public Brush ProgressColor => stand.ProgressColor;
    public Brush ProgressSubColor => stand.ProgressSubColor;
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
                TypeVipNameSettings = selectTypeVipSettings.Name;
                EnableTypeVipName = selectTypeVipSettings.EnableTypeVipName;

                PrepareMaxCurrentIn = selectTypeVipSettings.PrepareMaxCurrentIn.ToString(CultureInfo.InvariantCulture);
                AvailabilityMaxCurrentIn =
                    selectTypeVipSettings.AvailabilityMaxCurrentIn.ToString(CultureInfo.InvariantCulture);
                MaxCurrentIn = selectTypeVipSettings.MaxCurrentIn.ToString(CultureInfo.InvariantCulture);

                PercentAccuracyCurrent =
                    selectTypeVipSettings.PercentAccuracyCurrent.ToString(CultureInfo.InvariantCulture);
                MaxVoltageOut1 = selectTypeVipSettings.PrepareMaxVoltageOut1.ToString(CultureInfo.InvariantCulture);
                MaxVoltageOut2 = selectTypeVipSettings.MaxVoltageOut2.ToString(CultureInfo.InvariantCulture);
                PercentAccuracyVoltages =
                    selectTypeVipSettings.PercentAccuracyVoltages.ToString(CultureInfo.InvariantCulture);
                PercentAccuracyTemperature =
                    selectTypeVipSettings.PercentAccuracyTemperature.ToString(CultureInfo.InvariantCulture);
                TemperatureIn =
                    selectTypeVipSettings.MaxTemperatureIn.ToString(CultureInfo.InvariantCulture);
                TemperatureOut =
                    selectTypeVipSettings.MaxTemperatureOut.ToString(CultureInfo.InvariantCulture);

                ZeroTestInterval = selectTypeVipSettings.ZeroTestInterval;

                TestFirstIntervalTime = selectTypeVipSettings.TestFirstIntervalTime;
                TestIntervalTime = selectTypeVipSettings.TestIntervalTime;
                TestAllTime = selectTypeVipSettings.TestAllTime;

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

                VoltageAvailabilitySupply =
                    selectTypeVipSettings.GetDeviceParameters().SupplyValues.VoltageAvailability;
                CurrentAvailabilitySupply =
                    selectTypeVipSettings.GetDeviceParameters().SupplyValues.CurrentAvailability;

                OutputOnSupply = selectTypeVipSettings.GetDeviceParameters().SupplyValues.OutputOn;
                OutputOffSupply = selectTypeVipSettings.GetDeviceParameters().SupplyValues.OutputOff;

                CurrentMeterCurrentMax = selectTypeVipSettings.GetDeviceParameters().VoltCurrentValues.CurrMaxLimit;
                TermocoupleType = selectTypeVipSettings.GetDeviceParameters().VoltCurrentValues.TermocoupleType;
                OutputOnThermoCurrent = selectTypeVipSettings.GetDeviceParameters().VoltCurrentValues.OutputOn;
                OutputOffThermoCurrent = selectTypeVipSettings.GetDeviceParameters().VoltCurrentValues.OutputOff;
                ShuntResistance = selectTypeVipSettings.GetDeviceParameters().VoltCurrentValues.ShuntResistance;
                VoltCurrentMeterVoltMax = selectTypeVipSettings.GetDeviceParameters().VoltCurrentValues.VoltMaxLimit;

                VoltMeterVoltMax = selectTypeVipSettings.GetDeviceParameters().VoltValues.VoltMaxLimit;
                OutputOnVoltMeter = selectTypeVipSettings.GetDeviceParameters().VoltValues.OutputOn;
                OutputOffVoltmeter = selectTypeVipSettings.GetDeviceParameters().VoltValues.OutputOff;

                //обновление типа випа выбранного
                selectedTypeVips.Source = SelectTypeVipSettings?.Name;
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
    private string reportNum;

    public string ReportNum
    {
        get => reportNum;
        set
        {
            Set(ref reportNum, value);
            stand.ReportNum = value;
        }
    }

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

    private string prepareMaxCurrentIn;

    public string PrepareMaxCurrentIn
    {
        get => prepareMaxCurrentIn;
        set => Set(ref prepareMaxCurrentIn, value);
    }

    private string availabilityMaxCurrentIn;

    public string AvailabilityMaxCurrentIn
    {
        get => availabilityMaxCurrentIn;
        set => Set(ref availabilityMaxCurrentIn, value);
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

    private string percentAccuracyTemperature;

    public string PercentAccuracyTemperature
    {
        get => percentAccuracyTemperature;
        set => Set(ref percentAccuracyTemperature, value);
    }


    private string temperatureIn;

    public string TemperatureIn
    {
        get => temperatureIn;
        set => Set(ref temperatureIn, value);
    }

    private string temperatureOut;

    public string TemperatureOut
    {
        get => temperatureOut;
        set => Set(ref temperatureOut, value);
    }


    private string temperatureCurrentIn = "0";

    public string TemperatureCurrentIn
    {
        get => temperatureCurrentIn;
        set => Set(ref temperatureCurrentIn, value);
    }

    private string temperatureCurrentOut = "0";

    public string TemperatureCurrentOut
    {
        get => temperatureCurrentOut;
        set => Set(ref temperatureCurrentOut, value);
    }

    private double zeroTestInterval;

    public double ZeroTestInterval
    {
        get => zeroTestInterval;
        set => Set(ref zeroTestInterval, value);
    }

    private TimeSpan testFirstIntervalTime;

    public TimeSpan TestFirstIntervalTime
    {
        get => testFirstIntervalTime;
        set => Set(ref testFirstIntervalTime, value);
    }

    private TimeSpan testIntervalTime;

    public TimeSpan TestIntervalTime
    {
        get => testIntervalTime;
        set => Set(ref testIntervalTime, value);
    }

    private TimeSpan testAllTime;

    public TimeSpan TestAllTime
    {
        get => testAllTime;
        set => Set(ref testAllTime, value);
    }

    private int currentTypeVipSettings;

    public int CurrentTypeVipSettings
    {
        get => currentTypeVipSettings;
        set => Set(ref currentTypeVipSettings, value);
    }

    private int selectIndexDevice = 0;

    public int SelectIndexDevice
    {
        get => selectIndexDevice;
        set => Set(ref selectIndexDevice, value);
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

    private string voltageAvailabilitySupply;

    /// <summary>
    /// 
    /// </summary>
    public string VoltageAvailabilitySupply
    {
        get => voltageAvailabilitySupply;
        set => Set(ref voltageAvailabilitySupply, value);
    }

    private string currentAvailabilitySupply;

    /// <summary>
    /// 
    /// </summary>
    public string CurrentAvailabilitySupply
    {
        get => currentAvailabilitySupply;
        set => Set(ref currentAvailabilitySupply, value);
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

    private string voltCurrentMeterVoltMax;

    /// <summary>
    /// 
    /// </summary>
    public string VoltCurrentMeterVoltMax
    {
        get => voltCurrentMeterVoltMax;
        set => Set(ref voltCurrentMeterVoltMax, value);
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

    private string shuntResistance;

    /// <summary>
    /// 
    /// </summary>
    public string ShuntResistance
    {
        get => shuntResistance;
        set => Set(ref shuntResistance, value);
    }

    public string TimeTestStart => stand.TimeTestStart;
    public string TimeObservableTestNext => stand.TimeObservableTestNext;
    public string TimeControlTestNext => stand.TimeControlTestNext;
    public string TimeTestStop => stand.TimeTestStop;

    #endregion

    #region Валидация полей

    #region IDataErrorInfo

    // private List<int> comports = new List<int>();

    private List<int> bauds = new List<int>()
    {
        2400,
        4800,
        9600,
        19200,
        28800,
        38400,
        57600,
        76800,
        115200
    };

    private List<int> stopBits = new List<int>() { 0, 1, 2, 3 };

    private List<int> dataBits = new List<int>() { 5, 6, 7, 8 };


    //--Error--ошибка--
    public string this[string columnName]
    {
        get
        {
            string error = null;
            var strValue = GetType().GetProperty(columnName)?.GetValue(this)?.ToString();

            switch (columnName)
            {
                case nameof(NameDevice):
                    if (string.IsNullOrWhiteSpace(NameDevice))
                        error = "Введите имя устройства";
                    break;
                case nameof(Prefix):
                    if (string.IsNullOrWhiteSpace(Prefix) && IsPrefix)
                        error = "Введите префикс устройства";
                    break;
                case nameof(PortName):
                    if (string.IsNullOrWhiteSpace(PortName))
                        error = "Введите comport устройства";
                    //TODO возмонж сделать проверку на совпадение com ports !IsStingNumericNoMatch(PortName, comports) ||
                    else if (!IsStingNumericMaxMin(PortName, 1, 100))
                        error = "Введите корректный номер com port устройства от 1 до 100";
                    break;
                case nameof(Baud):
                    if (!IsStingNumericMatch(Baud, bauds))
                        error = "Введите один из стандартных baud rate устройства";
                    break;
                case nameof(StopBit):
                    if (!IsStingNumericMatch(StopBit, stopBits))
                        error = "Введите один из стандартных stop bit устройства";
                    break;
                case nameof(DataBit):
                    if (!IsStingNumericMatch(DataBit, dataBits))
                        error = "Введите один из стандартных data bit устройства";
                    break;
                case nameof(AllTimeChecked):
                    if (string.IsNullOrWhiteSpace(AllTimeChecked))
                        error = "Введите время проверки устройства";
                    if (!IsStingNumericMaxMin(AllTimeChecked, 200, 10000))
                        error = "Введите корректное время проверки устройства от 200 мс до 10000 мс";
                    break;
                case nameof(CountChecked):
                    if (string.IsNullOrWhiteSpace(CountChecked))
                        error = "Введите количество проверок устройства";
                    if (!IsStingNumericMaxMin(CountChecked, 1, 5))
                        error = "Введите корректное количество проверок устройства от 1 до 5";
                    break;
                //
                case nameof(NameCmdLib):
                    if (string.IsNullOrWhiteSpace(NameCmdLib))
                        error = "Введите имя команды";
                    break;
                case nameof(TransmitCmdLib):
                    if (string.IsNullOrWhiteSpace(TransmitCmdLib))
                        error = "Введите команду";
                    break;
                case nameof(TypeMessageCmdLib):
                    if (!IsTypeMessageCmdLib(TypeMessageCmdLib))
                        error = "Введите тип команды Hex/Text";
                    break;
                case nameof(DelayCmdLib):
                    if (string.IsNullOrWhiteSpace(DelayCmdLib))
                        error = "Введите корректное время задержки команды от 200 мс до 10000 мс";
                    else if (!IsStingNumericMaxMin(DelayCmdLib, 200, 10000))
                        error = "Введите корректное время задержки команды от 200 мс до 10000 мс";
                    break;
                case nameof(LengthCmdLib):
                    if (!string.IsNullOrWhiteSpace(LengthCmdLib) && !IsStingNumericMaxMin(LengthCmdLib, 1, 100))
                        error = "Введите корректую длину команды от 1 до 100";
                    break;
                //
                case nameof(TypeVipNameSettings):
                    if (string.IsNullOrWhiteSpace(strValue))
                        error = $"Поле имя типа Випа не должно быть пустым";

                    break;
                case nameof(MaxVoltageOut1):
                case nameof(MaxVoltageOut2):
                    if (string.IsNullOrWhiteSpace(MaxVoltageOut1) && string.IsNullOrWhiteSpace(MaxVoltageOut2))
                        error = $"Оба канала Uвых. типа Випа не могут быть пустыми";
                    break;
                case nameof(AvailabilityMaxCurrentIn):
                case nameof(PrepareMaxCurrentIn):
                case nameof(MaxCurrentIn):
                case nameof(TemperatureIn):
                case nameof(TemperatureOut):
                case nameof(ZeroTestInterval):
                case nameof(PercentAccuracyCurrent):
                case nameof(PercentAccuracyVoltages):
                case nameof(PercentAccuracyTemperature):
                    if (string.IsNullOrWhiteSpace(strValue))
                        error = $"Поле {columnName} типа Випа не должно быть пустым";
                    break;
                case nameof(TestFirstIntervalTime):
                case nameof(TestIntervalTime):
                case nameof(TestAllTime):
                    if (string.IsNullOrWhiteSpace(strValue))
                        error = $"Поле {columnName} типа Випа не должно быть пустым";
                    if (!TimeSpan.TryParseExact(strValue, "hh\\:mm\\:ss", null, out _))
                        error = $"Поле {columnName} типа Випа должно иметь вид 00:00:00";
                    break;
                case nameof(OutputOnLoad):
                case nameof(OutputOffLoad):
                case nameof(OutputOnSupply):
                case nameof(OutputOffSupply):
                    if (string.IsNullOrWhiteSpace(strValue))
                        error = $"Поле {columnName} параметров не должно быть пустым";
                    break;
                case nameof(FreqLoad):
                case nameof(AmplLoad):
                case nameof(DcoLoad):
                case nameof(SquLoad):
                case nameof(VoltageSupply):
                case nameof(CurrentSupply):
                case nameof(VoltageAvailabilitySupply):
                case nameof(CurrentAvailabilitySupply):
                case nameof(VoltCurrentMeterVoltMax):
                case nameof(CurrentMeterCurrentMax):
                case nameof(ShuntResistance):
                case nameof(VoltMeterVoltMax):
                    if (string.IsNullOrWhiteSpace(strValue))
                        error = $"Поле {columnName} параметров не должно быть пустым";
                    if (!IsStingDecimal(strValue))
                        error = $"В поле {columnName} введите цисловое значение согласно документации";
                    break;
                case nameof(TermocoupleType):
                    if (string.IsNullOrWhiteSpace(strValue))
                        error = $"Поле {columnName} параметров не должно быть пустым";
                    break;
            }

            return error;
        }
    }


    public string Error =>
        //
        this[nameof(TypeVipNameSettings)] ??
        this[nameof(AvailabilityMaxCurrentIn)] ??
        this[nameof(PrepareMaxCurrentIn)] ??
        this[nameof(MaxCurrentIn)] ??
        this[nameof(MaxVoltageOut1)] ??
        this[nameof(MaxVoltageOut2)] ??
        this[nameof(TemperatureIn)] ??
        this[nameof(TemperatureOut)] ??
        this[nameof(ZeroTestInterval)] ??
        this[nameof(TestFirstIntervalTime)] ??
        this[nameof(TestIntervalTime)] ??
        this[nameof(TestAllTime)] ??
        this[nameof(PercentAccuracyCurrent)] ??
        this[nameof(PercentAccuracyCurrent)] ??
        this[nameof(PercentAccuracyCurrent)] ??
        this[nameof(PercentAccuracyVoltages)] ??
        this[nameof(PercentAccuracyVoltages)] ??
        this[nameof(PercentAccuracyTemperature)] ??
        this[nameof(FreqLoad)] ??
        this[nameof(AmplLoad)] ??
        this[nameof(DcoLoad)] ??
        this[nameof(SquLoad)] ??
        this[nameof(OutputOnLoad)] ??
        this[nameof(OutputOffLoad)] ??
        this[nameof(VoltageSupply)] ??
        this[nameof(CurrentSupply)] ??
        this[nameof(VoltageAvailabilitySupply)] ??
        this[nameof(CurrentAvailabilitySupply)] ??
        this[nameof(OutputOnSupply)] ??
        this[nameof(OutputOffSupply)] ??
        this[nameof(VoltCurrentMeterVoltMax)] ??
        this[nameof(CurrentMeterCurrentMax)] ??
        this[nameof(TermocoupleType)] ??
        this[nameof(ShuntResistance)] ??
        this[nameof(VoltMeterVoltMax)] ??
        //
        this[nameof(NameDevice)] ??
        this[nameof(PortName)] ??
        this[nameof(Baud)] ??
        this[nameof(StopBit)] ??
        this[nameof(DataBit)] ??
        //
        this[nameof(AllTimeChecked)] ??
        this[nameof(CountChecked)] ??
        //
        this[nameof(NameCmdLib)] ??
        this[nameof(TransmitCmdLib)] ??
        this[nameof(ReceiveCmdLib)] ??
        this[nameof(TypeMessageCmdLib)] ??
        this[nameof(DelayCmdLib)] ??
        this[nameof(LengthCmdLib)] ??
        this[nameof(Prefix)];

    public bool prefixValidate = false;

    /// <summary>
    /// Имя устройства в текстбоксе
    /// </summary>
    public bool PrefixValidate
    {
        get => prefixValidate;
        set { Set(ref prefixValidate, value); }
    }


    private bool IsStingInt(string str)
    {
        if (!int.TryParse(str, out var num)) return false;
        return num > 0;
    }

    private bool IsStingDecimal(string str)
    {
        if (!decimal.TryParse(str, out var num)) return false;
        return num > 0;
    }

    private bool IsStingNumericMaxMin(string str, int minVal, int maxVal)
    {
        if (!int.TryParse(str, out var num)) return false;
        return num >= minVal && num <= maxVal;
    }

    /// <summary>
    /// Если такое число уже существует в списке выведет фелс
    /// </summary>
    private bool IsStingNumericNoMatch(string str, List<int> matches)
    {
        if (!int.TryParse(str, out var num)) return false;
        return !matches.Contains(num);
    }

    /// <summary>
    /// Если такое число не существует в списке выведет фелс
    /// </summary>
    private bool IsStingNumericMatch(string str, List<int> matches)
    {
        if (!int.TryParse(str, out var num)) return false;
        return matches.Contains(num);
    }

    private bool IsNumericContains(int num, List<int> matches)
    {
        var s = matches.Contains(num);
        return s;
    }

    private bool IsTypeMessageCmdLib(string typeCmd)
    {
        return typeCmd is "Hex" or "Text";
    }

    #endregion

    #endregion

    #endregion

    //---
    public IEnumerable GetErrors(string propertyName)
    {
        throw new NotImplementedException();
    }

    public bool HasErrors { get; }
    public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
}

public class ValidationErrorsConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var errors = values[0] as ReadOnlyCollection<ValidationError>;
        var errorType = parameter as string;

        if (errors != null && errors.Any(e => e.ErrorContent.ToString() == errorType))
        {
            return true;
        }

        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}