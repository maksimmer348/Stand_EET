using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace StandETT;

class CreatorAllDevicesAndLib
{
    //---

    #region События созданных устройств

    /// <summary>
    /// Событие проверки коннекта к порту
    /// </summary>
    public Action<BaseDevice, bool> PortConnecting;

    /// <summary>
    /// Событие проверки коннекта к устройству
    /// </summary>
    public Action<BaseDevice, string, DeviceCmd> DeviceReceiving;

    public Action<BaseDevice, string> DeviceError;

    #endregion

    #region Создание устройств и библиотек

    private MySerializer serializer = new();

    BaseLibCmd libCmd = BaseLibCmd.getInstance();

    private ConfigVips cfgVips = new ConfigVips();

    BaseDevice mainRelayVip = MainRelay.getInstance();

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
        mainRelayVip.AllDeviceError = new AllDeviceError();

        if (deserializeDevices == null || !deserializeDevices.Any())
        {
            BaseDevice voltMeter = new VoltMeter("GDM-78255A") { RowIndex = 1, ColumnIndex = 0 };
            voltMeter.SetConfigDevice(TypePort.SerialInput, "COM8", 115200, 1, 0, 8);
            temp.Add(voltMeter);

            BaseDevice thermoCurrentMeter = new ThermoCurrentMeter("GDM-78255A") { RowIndex = 1, ColumnIndex = 2 };
            thermoCurrentMeter.SetConfigDevice(TypePort.SerialInput, "COM7", 115200, 1, 0, 8);
            temp.Add(thermoCurrentMeter);


            BaseDevice supply = new Supply("PSW7-800-2.88") { RowIndex = 1, ColumnIndex = 1 };
            supply.SetConfigDevice(TypePort.SerialInput, "COM5", 115200, 1, 0, 8);
            temp.Add(supply);

            // //TODO вернуть 
            // BaseDevice smallLoad = new SmallLoad("SL") { RowIndex = 1, ColumnIndex = 3 };
            // smallLoad.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            // temp.Add(smallLoad);

            BaseDevice bigLoad = new BigLoad("AFG-72112") { RowIndex = 1, ColumnIndex = 4 };
            bigLoad.SetConfigDevice(TypePort.SerialInput, "COM6", 115200, 1, 0, 8);
            bigLoad.AllDeviceError = new AllDeviceError();
            temp.Add(bigLoad);

            // //TODO вернуть 
            // BaseDevice heat = new Heat("Heat") { RowIndex = 1, ColumnIndex = 5 };
            // heat.SetConfigDevice(TypePort.SerialInput, "COM80", 9600, 1, 0, 8);
            // temp.Add(heat);
            // heat.AllDeviceError = new AllDeviceError();

            BaseDevice relayMeter = new RelayMeter("MRS") { RowIndex = 1, ColumnIndex = 5 };
            relayMeter.SetConfigDevice(TypePort.SerialInput, "COM9", 9600, 1, 0, 8);
            relayMeter.AllDeviceError = new AllDeviceError();
            temp.Add(relayMeter);

            BaseDevice relay0 = new RelayVip(0, "0");
            relay0.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            relay0.Prefix = "AD";
            relay0.AllDeviceError = new AllDeviceError();
            temp.Add(relay0);

            BaseDevice relay1 = new RelayVip(1, "1");
            relay1.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            relay1.Prefix = "AE";
            relay1.AllDeviceError = new AllDeviceError();
            temp.Add(relay1);

            BaseDevice relay2 = new RelayVip(2, "2");
            relay2.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            relay2.Prefix = "AF";
            relay2.AllDeviceError = new AllDeviceError();
            temp.Add(relay2);

            BaseDevice relay3 = new RelayVip(3, "3");
            relay3.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            relay3.Prefix = "B0";
            relay3.AllDeviceError = new AllDeviceError();
            temp.Add(relay3);

            BaseDevice relay4 = new RelayVip(4, "C");
            relay4.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            relay4.Prefix = "B9";
            relay4.AllDeviceError = new AllDeviceError();
            temp.Add(relay4);

            BaseDevice relay5 = new RelayVip(5, "5");
            relay5.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            relay5.Prefix = "";
            relay5.AllDeviceError = new AllDeviceError();
            temp.Add(relay5);

            BaseDevice relay6 = new RelayVip(6, "6");
            relay6.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            relay6.Prefix = "";
            relay6.AllDeviceError = new AllDeviceError();
            temp.Add(relay6);

            BaseDevice relay7 = new RelayVip(7, "7");
            relay7.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            relay7.Prefix = "";
            relay7.AllDeviceError = new AllDeviceError();
            temp.Add(relay7);

            BaseDevice relay8 = new RelayVip(8, "8");
            relay8.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            relay8.Prefix = "";
            relay8.AllDeviceError = new AllDeviceError();
            temp.Add(relay8);

            BaseDevice relay9 = new RelayVip(9, "9");
            relay9.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            relay9.Prefix = "";
            relay9.AllDeviceError = new AllDeviceError();
            temp.Add(relay9);

            BaseDevice relay10 = new RelayVip(10, "A");
            relay10.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            relay10.Prefix = "";
            relay10.AllDeviceError = new AllDeviceError();
            temp.Add(relay10);

            BaseDevice relay11 = new RelayVip(11, "B");
            relay11.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);
            relay11.Prefix = "";
            relay11.AllDeviceError = new AllDeviceError();
            temp.Add(relay11);


            serializer.SerializeDevices(temp);
        }
        else
        {
            temp = deserializeDevices;
        }

        InvokeDevices(temp);
        return temp;
    }

    public List<Vip> SetVips(List<BaseDevice> relays)
    {
        List<Vip> temp = new List<Vip>();


        temp.Add(new Vip(0, (RelayVip)relays[0])
        {
            RowIndex = 0,
            ColumnIndex = 0,
        });
        temp.Add(new Vip(1, (RelayVip)relays[1])
        {
            RowIndex = 0,
            ColumnIndex = 1
        });
        temp.Add(new Vip(2, (RelayVip)relays[2])
        {
            RowIndex = 0,
            ColumnIndex = 2
        });
        temp.Add(new Vip(3, (RelayVip)relays[3])
        {
            RowIndex = 0,
            ColumnIndex = 3
        });
        temp.Add(new Vip(4, (RelayVip)relays[4])
        {
            RowIndex = 1,
            ColumnIndex = 0
        });
        temp.Add(new Vip(5, (RelayVip)relays[5])
        {
            RowIndex = 1,
            ColumnIndex = 1
        });
        temp.Add(new Vip(6, (RelayVip)relays[6])
        {
            RowIndex = 1,
            ColumnIndex = 2
        });
        temp.Add(new Vip(7, (RelayVip)relays[7])
        {
            RowIndex = 1,
            ColumnIndex = 3
        });
        temp.Add(new Vip(8, (RelayVip)relays[8])
        {
            RowIndex = 2,
            ColumnIndex = 0
        });
        temp.Add(new Vip(9, (RelayVip)relays[9])
        {
            RowIndex = 2,
            ColumnIndex = 1
        });
        temp.Add(new Vip(10, (RelayVip)relays[10])
        {
            RowIndex = 2,
            ColumnIndex = 2
        });
        temp.Add(new Vip(11, (RelayVip)relays[11])
        {
            RowIndex = 2,
            ColumnIndex = 3
        });
        return temp;
    }


    /// <summary>
    /// Установка библиотеки по умолчанию
    /// </summary>
    public Dictionary<DeviceIdentCmd, DeviceCmd> SetLib()
    {
        //десериализация библиотеки команд  
        Dictionary<DeviceIdentCmd, DeviceCmd> deserializeLib = serializer.DeserializeLib();
        libCmd.CreateTerminators();
        if (deserializeLib == null)
        {
            //--

            //статусы
            libCmd.AddCommand("Status", "AFG-72112", "*idn?", 200, "72192",
                TypeTerminator.LFCR, TypeTerminator.LF, type: TypeCmd.Text);
            libCmd.AddCommand("Status", "PSW7-800-2.88", "*idn?", 200, "880-2.88",
                TypeTerminator.LF, TypeTerminator.LF, type: TypeCmd.Text);
            libCmd.AddCommand("Status", "GDM-78255A", "*idn?", 200, "78255A",
                TypeTerminator.LFCR, TypeTerminator.LF, type: TypeCmd.Text);
            libCmd.AddCommand("Status", "MRS", "4980", 200, "FF", type: TypeCmd.Hex, isXor: true);

            libCmd.AddCommand("Status", "0", "1", 200, "01", type: TypeCmd.Hex, isXor: true);
            libCmd.AddCommand("Status", "0", "2", 200, "02", type: TypeCmd.Hex, isXor: true);
            libCmd.AddCommand("Status", "1", "3", 200, "03", type: TypeCmd.Hex, isXor: true);
            libCmd.AddCommand("Status", "2", "3", 200, "03", type: TypeCmd.Hex, isXor: true);
            //--

            //--
            libCmd.AddCommand("Get func", "GDM-78255A", "conf:stat:func?", 200, terminator: TypeTerminator.LFCR,
                receiveTerminator: TypeTerminator.LF);
            libCmd.AddCommand("Get volt meter", "GDM-78255A", "conf:stat:rang?", 200, terminator: TypeTerminator.LFCR,
                receiveTerminator: TypeTerminator.LF);
            libCmd.AddCommand("Set volt meter", "GDM-78255A", "conf:volt:dc ", 200, "78255A",
                TypeTerminator.LFCR, TypeTerminator.LF, type: TypeCmd.Text);

            libCmd.AddCommand("Get curr meter", "GDM-78255A", "conf:stat:rang?", 200, "78255A",
                TypeTerminator.LFCR, TypeTerminator.LF, type: TypeCmd.Text);
            libCmd.AddCommand("Set curr meter", "GDM-78255A", "conf:curr:dc ", 200, "78255A",
                TypeTerminator.LFCR, TypeTerminator.LF, type: TypeCmd.Text);

            libCmd.AddCommand("Get volt", "GDM-78255A", "val1?", 200, "78255A",
                TypeTerminator.LFCR, TypeTerminator.LF, type: TypeCmd.Text);
            //--


            deserializeLib = libCmd.DeviceCommands;
            SerializeLib();
        }

        return deserializeLib;
    }

    #endregion


    void InvokeDevices(List<BaseDevice> devices)
    {
        mainRelayVip.PortConnecting += Port_Connecting;
        mainRelayVip.DeviceError += Device_Error;
        
        foreach (var device in devices)
        {
            device.PortConnecting += Port_Connecting;
            device.DeviceReceiving += Device_Receiving;
            device.DeviceError += Device_Error;
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


    public void SerializeDevices(List<BaseDevice> devices)
    {
        serializer.SerializeDevices(devices.ToList());
    }

    public ObservableCollection<TypeVip> SetTypeVips()
    {
        var deserializeType = serializer.DeserializeTypeVips();

        if (deserializeType == null || !deserializeType.Any())
        {
            cfgVips.PrepareAddTypeVips();
            serializer.SerializeTypeVips(cfgVips.TypeVips.ToList());
        }
        else
        {
            cfgVips.TypeVips = new ObservableCollection<TypeVip>(deserializeType);
        }

        return cfgVips.TypeVips;
    }
}