using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace StandETT;

class DeviceAndLibCreator
{
    //---

    #region Создание устройств и библиотек

    private MySerializer serializer = new();

    private readonly Stand1 stand1;

    BaseLibCmd libCmd = BaseLibCmd.getInstance();

    public DeviceAndLibCreator(Stand1 stand1)
    {
        this.stand1 = stand1;
    }

    /// <summary>
    /// Установка приборов по умолчанию
    /// </summary>
    public List<BaseDevice> SetDevices()
    {
        List<BaseDevice> temp = new List<BaseDevice>();
        if (serializer.DeserializeDevices() == null || !serializer.DeserializeDevices().Any())
        {
            BaseDevice voltMeter = new VoltMeter("GDM-78255A") { RowIndex = 1, ColumnIndex = 0 };
            voltMeter.SetConfigDevice(TypePort.SerialInput, "COM8", 115200, 1, 0, 8);
            temp.Add(voltMeter);

            //
            BaseDevice thermoCurrentMeter = new ThermoCurrentMeter("GDM-78255A") { RowIndex = 1, ColumnIndex = 2 };
            thermoCurrentMeter.SetConfigDevice(TypePort.SerialInput, "COM7", 115200, 1, 0, 8);
            temp.Add(thermoCurrentMeter);

            BaseDevice supply = new Supply("PSW7-800-2.88") { RowIndex = 1, ColumnIndex = 1 };
            supply.SetConfigDevice(TypePort.SerialInput, "COM5", 115200, 1, 0, 8);
            temp.Add(supply);

            // TODO вернуть 
            // BaseDevice smallLoad = new SmallLoad("SMLL LOAD-87") { RowIndex = 1, ColumnIndex = 3 };
            // smallLoad.SetConfigDevice(TypePort.SerialInput, "COM60", 2400, 1, 0, 8);
            // temp.Add(smallLoad);

            BaseDevice bigLoad = new BigLoad("AFG-72112") { RowIndex = 1, ColumnIndex = 4 };
            bigLoad.SetConfigDevice(TypePort.SerialInput, "COM6", 115200, 1, 0, 8);
            temp.Add(bigLoad);

            //TODO вернуть 
            // BaseDevice heat = new Heat("Heat") { RowIndex = 1, ColumnIndex = 5 };
            // heat.SetConfigDevice(TypePort.SerialInput, "COM80", 9600, 1, 0, 8);
            // temp.Add(heat);

            BaseDevice relayMeter = new RelayMeter("MRS") { RowIndex = 1, ColumnIndex = 5 };
            relayMeter.SetConfigDevice(TypePort.SerialInput, "COM9", 9600, 1, 0, 8);
            temp.Add(relayMeter);

            BaseDevice mainRelayVip = new MainRelay("VRS");
            mainRelayVip.SetConfigDevice(TypePort.SerialInput, "COM3", 9600, 1, 0, 8);

            serializer.SerializeDevices(temp);
        }
        else
        {
            temp = serializer.DeserializeDevices();
        }

        InvokeDevices(temp);
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
            libCmd.AddCommand("Status", "AFG-72112", "*idn?", "72192", 200, TypeTerminator.LFCR, TypeTerminator.LF,
                type: TypeCmd.Text);
            libCmd.AddCommand("Status", "PSW7-800-2.88", "*idn?", "880-2.88", 200, TypeTerminator.LFCR,
                TypeTerminator.LF, type: TypeCmd.Text);
            libCmd.AddCommand("Status", "GDM-78255A", "*idn?", "78255A", 200, TypeTerminator.LFCR, TypeTerminator.LF,
                type: TypeCmd.Text);
            libCmd.AddCommand("Status", "MRS", "4980", "FF", 200, type: TypeCmd.Hex, isXor: true);
            deserializeLib = libCmd.DeviceCommands;
            SerializeLib();
        }

        return deserializeLib;
    }

    #endregion


    void InvokeDevices(List<BaseDevice> devices)
    {
        foreach (var device in devices)
        {
            device.PortConnecting += OnConnectPort;
            device.DeviceConnecting += OnConnectDevice;
            device.DeviceReceiving += OnReceive;
        }
    }

    private void OnConnectDevice(BaseDevice device, bool connect, string receive)
    {
        stand1.CheckDevice?.Invoke(device, connect, receive);
    }

    private void OnConnectPort(BaseDevice device, bool connect)
    {
        stand1.CheckPort?.Invoke(device, connect);
    }

    private void OnReceive(BaseDevice device, string receive)
    {
        stand1.Receive?.Invoke(device, receive);
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
}