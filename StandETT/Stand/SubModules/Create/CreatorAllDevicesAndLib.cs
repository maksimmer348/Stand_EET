using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace StandETT;

class CreatorAllDevicesAndLib
{
    //---

    #region События созданных устройств

    /// <summary>
    /// Событие проверки коннекта к порту
    /// </summary>
    public event Action<BaseDevice, bool> PortConnecting;

    /// <summary>
    /// Событие проверки коннекта к устройству
    /// </summary>
    public event Action<BaseDevice, string, DeviceCmd> DeviceReceiving;

    public event Action<BaseDevice, string> DeviceError;

    #endregion

    #region Создание устройств и библиотек

    private MySerializer serializer = new();

    BaseLibCmd libCmd = BaseLibCmd.getInstance();

    ConfigTypeVip cfgTypeVips = ConfigTypeVip.getInstance();

    MainRelay mainRelayVip = MainRelay.GetInstance();

    TimeMachine timeMachine = TimeMachine.getInstance();

    public CreatorAllDevicesAndLib()
    {
    }

    /// <summary>
    /// Установка приборов по умолчанию
    /// </summary>
    public List<BaseDevice> SetDevices()
    {
        List<BaseDevice> temp = new List<BaseDevice>();

        var deserializeDevices = serializer.DeserializeDevices();

        mainRelayVip.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);


        if (deserializeDevices == null || !deserializeDevices.Any())
        {
            BaseDevice supply = new Supply("PSW7-800-2.88") { RowIndex = 0, ColumnIndex = 3 };
            supply.SetConfigDevice(TypePort.SerialInput, "COM11", 115200, 1, 0, 8);
            temp.Add(supply);

            BaseDevice voltMeter = new VoltMeter("GDM-78255A")
                { RowIndex = 0, ColumnIndex = 0, IsHiddenOutputOnOff = Visibility.Collapsed };
            voltMeter.SetConfigDevice(TypePort.SerialInput, "COM8", 115200, 1, 0, 8);
            temp.Add(voltMeter);

            BaseDevice voltCurrentMeter = new VoltCurrentMeter("GDM-78255A")
                { RowIndex = 0, ColumnIndex = 1, IsHiddenOutputOnOff = Visibility.Collapsed };
            voltCurrentMeter.SetConfigDevice(TypePort.SerialInput, "COM7", 115200, 1, 0, 8);
            temp.Add(voltCurrentMeter);

            BaseDevice thermometer = new Thermometer("???") { RowIndex = 0, ColumnIndex = 2 };
            thermometer.SetConfigDevice(TypePort.SerialInput, "COM88", 115200, 1, 0, 8);
            temp.Add(thermometer);


            BaseDevice bigLoad = new BigLoad("AFG-72112") { RowIndex = 0, ColumnIndex = 4 };
            bigLoad.SetConfigDevice(TypePort.SerialInput, "COM6", 115200, 1, 0, 8);

            temp.Add(bigLoad);

            BaseDevice heat = new Heat("???") { RowIndex = 1, ColumnIndex = 0 };
            heat.SetConfigDevice(TypePort.SerialInput, "COM99", 9600, 1, 0, 8);
            temp.Add(heat);


            BaseDevice relayMeter = new RelayMeter("MRS")
                { RowIndex = 1, ColumnIndex = 1, IsHiddenOutputOnOff = Visibility.Collapsed };
            relayMeter.SetConfigDevice(TypePort.SerialInput, "COM9", 9600, 1, 0, 8);

            temp.Add(relayMeter);

            BaseDevice relay0 = new RelayVip(0, "0");
            relay0.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            relay0.Prefix = "AD";

            temp.Add(relay0);

            BaseDevice relay1 = new RelayVip(1, "1");
            relay1.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            relay1.Prefix = "AE";

            temp.Add(relay1);

            BaseDevice relay2 = new RelayVip(2, "2");
            relay2.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            relay2.Prefix = "AF";

            temp.Add(relay2);

            BaseDevice relay3 = new RelayVip(3, "3");
            relay3.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            relay3.Prefix = "B0";

            temp.Add(relay3);

            BaseDevice relay4 = new RelayVip(4, "C");
            relay4.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            relay4.Prefix = "B9";

            temp.Add(relay4);

            BaseDevice relay5 = new RelayVip(5, "5");
            relay5.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            relay5.Prefix = "";

            temp.Add(relay5);

            BaseDevice relay6 = new RelayVip(6, "6");
            relay6.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            relay6.Prefix = "";

            temp.Add(relay6);

            BaseDevice relay7 = new RelayVip(7, "7");
            relay7.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            relay7.Prefix = "";

            temp.Add(relay7);

            BaseDevice relay8 = new RelayVip(8, "8");
            relay8.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            relay8.Prefix = "";

            temp.Add(relay8);

            BaseDevice relay9 = new RelayVip(9, "9");
            relay9.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            relay9.Prefix = "";

            temp.Add(relay9);

            BaseDevice relay10 = new RelayVip(10, "A");
            relay10.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            relay10.Prefix = "";

            temp.Add(relay10);

            BaseDevice relay11 = new RelayVip(11, "B");
            relay11.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            relay11.Prefix = "";

            temp.Add(relay11);

            BaseDevice smallLoad1 = new RelayVip(1, "SL-1")
                { RowIndex = 1, ColumnIndex = 2, IsHiddenOutputOnOff = Visibility.Collapsed };
            smallLoad1.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            smallLoad1.Prefix = "1";

            temp.Add(smallLoad1);

            BaseDevice smallLoad2 = new RelayVip(2, "SL-2")
                { RowIndex = 1, ColumnIndex = 3, IsHiddenOutputOnOff = Visibility.Collapsed };
            smallLoad2.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            smallLoad2.Prefix = "2";

            temp.Add(smallLoad2);

            BaseDevice smallLoad0 = new RelayVip(3, "SL-3")
            {
                RowIndex = 1, ColumnIndex = 4,
                IsHiddenOutputOnOff = Visibility.Collapsed
            };
            smallLoad0.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            smallLoad0.Prefix = "3";

            temp.Add(smallLoad0);

            //TODO вернуть

            serializer.SerializeDevices(temp);
        }
        else
        {
            temp = deserializeDevices;
        }

        InvokeDevices(temp);
        return temp;
    }

    public List<Vip> SetVips(List<RelayVip> relays)
    {
        List<Vip> temp = new List<Vip>();

        temp.Add(new Vip(0, relays[0])
        {
            RowIndex = 0,
            ColumnIndex = 0,
        });
        temp.Add(new Vip(1, relays[1])
        {
            RowIndex = 0,
            ColumnIndex = 1
        });
        temp.Add(new Vip(2, relays[2])
        {
            RowIndex = 0,
            ColumnIndex = 2
        });
        temp.Add(new Vip(3, relays[3])
        {
            RowIndex = 0,
            ColumnIndex = 3
        });
        temp.Add(new Vip(4, relays[4])
        {
            RowIndex = 1,
            ColumnIndex = 0
        });
        temp.Add(new Vip(5, relays[5])
        {
            RowIndex = 1,
            ColumnIndex = 1
        });
        temp.Add(new Vip(6, relays[6])
        {
            RowIndex = 1,
            ColumnIndex = 2
        });
        temp.Add(new Vip(7, relays[7])
        {
            RowIndex = 1,
            ColumnIndex = 3
        });
        temp.Add(new Vip(8, relays[8])
        {
            RowIndex = 2,
            ColumnIndex = 0
        });
        temp.Add(new Vip(9, relays[9])
        {
            RowIndex = 2,
            ColumnIndex = 1
        });
        temp.Add(new Vip(10, relays[10])
        {
            RowIndex = 2,
            ColumnIndex = 2
        });
        temp.Add(new Vip(11, relays[11])
        {
            RowIndex = 2,
            ColumnIndex = 3
        });


        mainRelayVip.Relays = new(relays);
        return temp;
    }


    /// <summary>
    /// Установка библиотеки по умолчанию
    /// </summary>
    public Dictionary<DeviceIdentCmd, DeviceCmd> SetLib()
    {
        //десериализация библиотеки команд  
        Dictionary<DeviceIdentCmd, DeviceCmd> deserializeLib = serializer.DeserializeLib();
        //для ввода терминаторов строки
        libCmd.CreateTerminators();

        //если нечего десеризоывать создается стандратная 
        if (deserializeLib == null || !deserializeLib.Any())
        {
            //--

            //статусы устройств
            libCmd.AddCommand("Status", "GDM-78255A", "*idn?", 200, "78255A",
                terminator: TypeTerminator.CRLF, receiveTerminator: TypeTerminator.LFCR, type: TypeCmd.Text);
            libCmd.AddCommand("Status", "AFG-72112", "*idn?", 200, "72112",
                terminator: TypeTerminator.CRLF, receiveTerminator: TypeTerminator.CRLF, type: TypeCmd.Text);
            libCmd.AddCommand("Status", "PSW7-800-2.88", "*idn?", 200, "800-2.88",
                terminator: TypeTerminator.CRLF, receiveTerminator: TypeTerminator.LF, type: TypeCmd.Text);
            libCmd.AddCommand("Status", "MRS", "4980", 200, "FF", type: TypeCmd.Hex, isXor: true);
            //статусы випы
            libCmd.AddCommand("Status", "0", "4E502E", 200, "AD4B", type: TypeCmd.Hex, isXor: true, length: "4");
            libCmd.AddCommand("Status", "1", "4E512E", 200, "AE4B", type: TypeCmd.Hex, isXor: true, length: "4");
            libCmd.AddCommand("Status", "2", "4E522E", 200, "AF4B", type: TypeCmd.Hex, isXor: true, length: "4");
            libCmd.AddCommand("Status", "3", "4E532E", 200, "B04B", type: TypeCmd.Hex, isXor: true, length: "4");
            libCmd.AddCommand("Status", "C", "4E5C2E", 200, "B94B", type: TypeCmd.Hex, isXor: true, length: "4");

            //--

            //--
            //команды устройств
            //вольтметр и термомтер и амперметр 78255A

            //set
            libCmd.AddCommand("Set volt meter", "GDM-78255A", "conf:volt:dc ", 200,
                terminator: TypeTerminator.LFCR, receiveTerminator: TypeTerminator.LFCR, type: TypeCmd.Text);
            libCmd.AddCommand("Set curr meter", "GDM-78255A", "conf:curr:dc ", 200,
                terminator: TypeTerminator.LFCR, receiveTerminator: TypeTerminator.LFCR, type: TypeCmd.Text);
            libCmd.AddCommand("Set temp meter", "GDM-78255A", "conf:temp", 200,
                terminator: TypeTerminator.LFCR, receiveTerminator: TypeTerminator.LFCR, type: TypeCmd.Text);
            libCmd.AddCommand("Set tco type", "GDM-78255A", "sens:temp:tco:type ", 200,
                terminator: TypeTerminator.LFCR, receiveTerminator: TypeTerminator.LFCR, type: TypeCmd.Text);


            //get
            libCmd.AddCommand("Get func", "GDM-78255A", "conf:stat:func?", 200, terminator: TypeTerminator.LFCR,
                receiveTerminator: TypeTerminator.LFCR);
            libCmd.AddCommand("Get curr meter", "GDM-78255A", "conf:stat:rang?", 200,
                terminator: TypeTerminator.LFCR, receiveTerminator: TypeTerminator.LFCR, type: TypeCmd.Text);
            libCmd.AddCommand("Get volt meter", "GDM-78255A", "conf:stat:rang?", 200, terminator: TypeTerminator.LFCR,
                receiveTerminator: TypeTerminator.LFCR);
            libCmd.AddCommand("Get all value", "GDM-78255A", "val1?", 200,
                terminator: TypeTerminator.LFCR, receiveTerminator: TypeTerminator.LFCR, type: TypeCmd.Text);
            libCmd.AddCommand("Get tco type", "GDM-78255A", "sens:temp:tco:type?", 200,
                terminator: TypeTerminator.LFCR, receiveTerminator: TypeTerminator.LFCR, type: TypeCmd.Text);


            //блок питания
            //set
            libCmd.AddCommand("Set volt", "PSW7-800-2.88", "SOUR:VOLT:LEV:IMM:AMPL ", 200,
                terminator: TypeTerminator.LFCR, receiveTerminator: TypeTerminator.LF, type: TypeCmd.Text);
            libCmd.AddCommand("Set curr", "PSW7-800-2.88", "SOUR:CURR:LEV:IMM:AMPL ", 200,
                terminator: TypeTerminator.LFCR, receiveTerminator: TypeTerminator.LF, type: TypeCmd.Text);
            libCmd.AddCommand("Set output on", "PSW7-800-2.88", "OUTPut 1", 200,
                terminator: TypeTerminator.LFCR, receiveTerminator: TypeTerminator.LF, type: TypeCmd.Text);
            libCmd.AddCommand("Set output off", "PSW7-800-2.88", "OUTPut 0", 200,
                terminator: TypeTerminator.LFCR, receiveTerminator: TypeTerminator.LF, type: TypeCmd.Text);
            //get
            libCmd.AddCommand("Get volt", "PSW7-800-2.88", "SOUR:VOLT:LEV:IMM:AMPL?", 200,
                terminator: TypeTerminator.LFCR, receiveTerminator: TypeTerminator.LF, type: TypeCmd.Text);
            libCmd.AddCommand("Get curr", "PSW7-800-2.88", "SOUR:CURR:LEV:IMM:AMPL?", 200,
                terminator: TypeTerminator.LFCR, receiveTerminator: TypeTerminator.LF, type: TypeCmd.Text);
            libCmd.AddCommand("Get output", "PSW7-800-2.88", "OUTPut?", 200,
                terminator: TypeTerminator.LFCR, receiveTerminator: TypeTerminator.LF, type: TypeCmd.Text);


            //большая нагрузка/генератор
            //set
            libCmd.AddCommand("Set freq", "AFG-72112", "SOUR1:FREQ ", 200,
                terminator: TypeTerminator.CRLF, receiveTerminator: TypeTerminator.CRLF, type: TypeCmd.Text);
            libCmd.AddCommand("Set dco", "AFG-72112", "SOUR1:DCO ", 200,
                terminator: TypeTerminator.CRLF, receiveTerminator: TypeTerminator.CRLF, type: TypeCmd.Text);
            libCmd.AddCommand("Set ampl", "AFG-72112", "SOUR1:AMPL ", 200,
                terminator: TypeTerminator.CRLF, receiveTerminator: TypeTerminator.CRLF, type: TypeCmd.Text);
            libCmd.AddCommand("Set squ", "AFG-72112", "SOUR1:SQU:DCYC ", 200,
                terminator: TypeTerminator.CRLF, receiveTerminator: TypeTerminator.CRLF, type: TypeCmd.Text);
            libCmd.AddCommand("Set output off", "AFG-72112", "OUTP OFF", 200,
                terminator: TypeTerminator.CRLF, receiveTerminator: TypeTerminator.CRLF, type: TypeCmd.Text);
            libCmd.AddCommand("Set output on", "AFG-72112", "OUTP ON", 200,
                terminator: TypeTerminator.CRLF, receiveTerminator: TypeTerminator.CRLF, type: TypeCmd.Text);
            //get
            libCmd.AddCommand("Get freq", "AFG-72112", "SOUR1:FREQ?", 200,
                terminator: TypeTerminator.CRLF, receiveTerminator: TypeTerminator.CRLF, type: TypeCmd.Text);
            libCmd.AddCommand("Get squ", "AFG-72112", "SOUR1:SQU:DCYC?", 200,
                terminator: TypeTerminator.CRLF, receiveTerminator: TypeTerminator.CRLF, type: TypeCmd.Text);
            libCmd.AddCommand("Get ampl", "AFG-72112", "SOUR1:AMPL?", 200,
                terminator: TypeTerminator.CRLF, receiveTerminator: TypeTerminator.CRLF, type: TypeCmd.Text);
            libCmd.AddCommand("Get dco", "AFG-72112", "SOUR1:DCO?", 200,
                terminator: TypeTerminator.CRLF, receiveTerminator: TypeTerminator.CRLF, type: TypeCmd.Text);
            libCmd.AddCommand("Get output", "AFG-72112", "OUTPut?", 200,
                terminator: TypeTerminator.CRLF, receiveTerminator: TypeTerminator.CRLF, type: TypeCmd.Text);

            #region Relay

            //реле измерителей
            //вкл
            //1канал
            libCmd.AddCommand("On 01", "MRS", "4901", 400, "01", type: TypeCmd.Hex, isXor: true, length: "3");
            libCmd.AddCommand("On 02", "MRS", "4902", 400, "02", type: TypeCmd.Hex, isXor: true, length: "3");
            libCmd.AddCommand("On 03", "MRS", "4903", 400, "03", type: TypeCmd.Hex, isXor: true, length: "3");
            libCmd.AddCommand("On 04", "MRS", "4904", 400, "04", type: TypeCmd.Hex, isXor: true, length: "3");
            libCmd.AddCommand("On 05", "MRS", "4905", 400, "05", type: TypeCmd.Hex, isXor: true, length: "3");
            libCmd.AddCommand("On 06", "MRS", "4906", 400, "06", type: TypeCmd.Hex, isXor: true, length: "3");
            libCmd.AddCommand("On 07", "MRS", "4907", 400, "07", type: TypeCmd.Hex, isXor: true, length: "3");
            libCmd.AddCommand("On 08", "MRS", "4908", 400, "08", type: TypeCmd.Hex, isXor: true, length: "3");
            libCmd.AddCommand("On 09", "MRS", "4909", 400, "09", type: TypeCmd.Hex, isXor: true, length: "3");
            libCmd.AddCommand("On 0A", "MRS", "49AC", 400, "0A", type: TypeCmd.Hex, isXor: true, length: "3");
            libCmd.AddCommand("On 0B", "MRS", "490B", 400, "0B", type: TypeCmd.Hex, isXor: true, length: "3");
            libCmd.AddCommand("On 0C", "MRS", "490C", 400, "0C", type: TypeCmd.Hex, isXor: true, length: "3");

            //2 канал
            libCmd.AddCommand("On 11", "MRS", "4911", 400, "11", type: TypeCmd.Hex, isXor: true, length: "3");
            libCmd.AddCommand("On 12", "MRS", "4912", 400, "12", type: TypeCmd.Hex, isXor: true, length: "3");
            libCmd.AddCommand("On 13", "MRS", "4913", 400, "13", type: TypeCmd.Hex, isXor: true, length: "3");
            libCmd.AddCommand("On 14", "MRS", "4914", 400, "14", type: TypeCmd.Hex, isXor: true, length: "3");
            libCmd.AddCommand("On 15", "MRS", "4915", 400, "15", type: TypeCmd.Hex, isXor: true, length: "3");
            libCmd.AddCommand("On 16", "MRS", "4916", 400, "16", type: TypeCmd.Hex, isXor: true, length: "3");
            libCmd.AddCommand("On 17", "MRS", "4917", 400, "17", type: TypeCmd.Hex, isXor: true, length: "3");
            libCmd.AddCommand("On 18", "MRS", "4918", 400, "18", type: TypeCmd.Hex, isXor: true, length: "3");
            libCmd.AddCommand("On 19", "MRS", "4919", 400, "19", type: TypeCmd.Hex, isXor: true, length: "3");
            libCmd.AddCommand("On 1A", "MRS", "491A", 400, "1A", type: TypeCmd.Hex, isXor: true, length: "3");
            libCmd.AddCommand("On 1B", "MRS", "491B", 400, "1B", type: TypeCmd.Hex, isXor: true, length: "3");
            libCmd.AddCommand("On 1C", "MRS", "491C", 400, "1C", type: TypeCmd.Hex, isXor: true, length: "3");
            //реле ви1ов
            //вкл
            libCmd.AddCommand("On", "0", "4E5061", 5500, "AD1", type: TypeCmd.Hex, isXor: true, length: "4");
            libCmd.AddCommand("On", "1", "4E5161", 5500, "AE1", type: TypeCmd.Hex, isXor: true, length: "4");
            libCmd.AddCommand("On", "2", "4E5261", 5500, "AF1", type: TypeCmd.Hex, isXor: true, length: "4");
            libCmd.AddCommand("On", "3", "4E5361", 5500, "B01", type: TypeCmd.Hex, isXor: true, length: "4");
            libCmd.AddCommand("On", "C", "4E5C61", 5500, "B91", type: TypeCmd.Hex, isXor: true, length: "4");
            //выкл
            libCmd.AddCommand("Off", "0", "4E50B8", 200, "AD99", type: TypeCmd.Hex, isXor: true, length: "4");
            libCmd.AddCommand("Off", "1", "4E51B8", 200, "AE99", type: TypeCmd.Hex, isXor: true, length: "4");
            libCmd.AddCommand("Off", "2", "4E52B8", 200, "AF99", type: TypeCmd.Hex, isXor: true, length: "4");
            libCmd.AddCommand("Off", "3", "4E53B8", 200, "B099", type: TypeCmd.Hex, isXor: true, length: "4");
            libCmd.AddCommand("Off", "C", "4E5CB8", 200, "B999", type: TypeCmd.Hex, isXor: true, length: "4");
            //

            #endregion

            //--

            deserializeLib = libCmd.DeviceCommands;
            SerializeLib();
        }

        return deserializeLib;
    }


    public ObservableCollection<TypeVip> SetTypeVips()
    {
        //десериализация 
        ObservableCollection<TypeVip> deserializeTypeVips = new(serializer.DeserializeTypeVips());
        if (deserializeTypeVips == null || !deserializeTypeVips.Any())
        {
            //-

            var typeVip70 = new TypeVip
            {
                Name = "Vip70",
                MaxTemperature = 70,
                //максимаьные значения во время испытаниий они означают ошибку
                MaxVoltageIn = 220,
                MaxVoltageOut1 = 40,
                MaxVoltageOut2 = 45,
                MaxCurrentIn = 2.5M,
                //максимальные значения во время предпотготовки испытания (PrepareMaxVoltageOut1 и PrepareMaxVoltageOut2
                //берутся из MaxVoltageOut1 и MaxVoltageOut2 соотвественно)
                PrepareMaxCurrentIn = 0.5M,
                //процент погрешности измерения
                PercentAccuracyCurrent = 10,
                PercentAccuracyVoltages = 5,
            };

            //настройки для приборов они зависят от типа Випа
            typeVip70.SetDeviceParameters(new DeviceParameters()
            {
                BigLoadValues = new BigLoadValues("300", "4", "2", "40", "1", "0"),
                HeatValues = new HeatValues("1", "0"),
                SupplyValues = new SupplyValues("2", "1", "3", "0.5", "1", "0"),
                VoltCurrentValues = new VoltCurrentMeterValues("100", "100", "k", "1", "0"),
                VoltValues = new VoltMeterValues("100", "1", "0")
            });
            typeVip70.BaseDeviceValues.Add(typeVip70.GetDeviceParameters().BigLoadValues);
            typeVip70.BaseDeviceValues.Add(typeVip70.GetDeviceParameters().HeatValues);
            typeVip70.BaseDeviceValues.Add(typeVip70.GetDeviceParameters().SupplyValues);
            typeVip70.BaseDeviceValues.Add(typeVip70.GetDeviceParameters().VoltCurrentValues);
            typeVip70.BaseDeviceValues.Add(typeVip70.GetDeviceParameters().VoltValues);

            //-

            var typeVip71 = new TypeVip
            {
                Name = "Vip71",
                //максимаьные значения во время испытаниий они означают ошибку
                MaxTemperature = 90,
                MaxVoltageIn = 120,
                MaxVoltageOut1 = 20,
                MaxVoltageOut2 = 25,
                MaxCurrentIn = 5,
                //максимальные значения во время предпотготовки испытания (PrepareMaxVoltageOut1 и PrepareMaxVoltageOut2
                //берутся из MaxVoltageOut1 и MaxVoltageOut2 соотвественно)
                PrepareMaxCurrentIn = 0.5M,
                //процент погрешности измерения
                PercentAccuracyCurrent = 10,
                PercentAccuracyVoltages = 5,
            };
            //настройки для приборов они зависят от типа Випа
            typeVip71.SetDeviceParameters(new DeviceParameters()
            {
                BigLoadValues = new BigLoadValues("200", "3.3", "1.65", "20", "1", "0"),
                HeatValues = new HeatValues("1", "0"),
                SupplyValues = new SupplyValues("3", "2", "1", "4", "0.5", "0"),
                VoltCurrentValues = new VoltCurrentMeterValues("10", "100", "k", "1", "0"),
                VoltValues = new VoltMeterValues("100", "1", "0")
            });
            typeVip71.BaseDeviceValues.Add(typeVip70.GetDeviceParameters().BigLoadValues);
            typeVip71.BaseDeviceValues.Add(typeVip70.GetDeviceParameters().HeatValues);
            typeVip71.BaseDeviceValues.Add(typeVip70.GetDeviceParameters().SupplyValues);
            typeVip71.BaseDeviceValues.Add(typeVip70.GetDeviceParameters().VoltCurrentValues);
            typeVip71.BaseDeviceValues.Add(typeVip70.GetDeviceParameters().VoltValues);

            deserializeTypeVips?.Add(typeVip70);
            deserializeTypeVips?.Add(typeVip71);

            SerializeTypeVip(cfgTypeVips.TypeVips.ToList());
        }

        return deserializeTypeVips;
    }

    public TimeMachine SetTime()
    {
        var t = serializer.DeserializeTime();
        if (t == null)
        {
            t = new TimeMachine();
            t.CountChecked = "3";
            t.AllTimeChecked = "3000";
            SerializeTime(t);
        }

        return t;
    }

    #endregion


    void InvokeDevices(List<BaseDevice> devices)
    {
        mainRelayVip.PortConnecting += Port_Connecting;
        mainRelayVip.DeviceError += Device_Error;

        foreach (var device in devices)
        {
            device.PortConnecting += Port_Connecting;
            device.DeviceError += Device_Error;
            device.DeviceReceiving += Device_Receiving;
        }
    }


    /// <summary>
    /// Обработка события прнятого сообщения из устройства
    /// </summary>
    private void Device_Receiving(BaseDevice device, string receive, DeviceCmd cmd)
    {
        DeviceReceiving?.Invoke(device, receive, cmd);
    }

    /// <summary>
    /// Обработка события коннект выбраного компорта
    /// </summary>
    private void Port_Connecting(BaseDevice device, bool isConnect)
    {
        PortConnecting.Invoke(device, isConnect);
    }

    private void Device_Error(BaseDevice device, string err)
    {
        DeviceError.Invoke(device, err);
    }


    public void SerializeLib()
    {
        serializer.LibCmd = libCmd.DeviceCommands;
        serializer.SerializeLib();
    }

    public void SerializeTypeVip(List<TypeVip> typeVips)
    {
        serializer.SerializeTypeVips(typeVips);
    }


    public void SerializeDevices(List<BaseDevice> devices)
    {
        serializer.SerializeDevices(devices);
    }

    public void SerializeTime(TimeMachine timeMachine)
    {
        serializer.SerializeTime(timeMachine);
    }
}