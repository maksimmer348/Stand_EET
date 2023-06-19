using System;
using System.Diagnostics;
using System.Text;
using System.Windows;
using Newtonsoft.Json;

namespace StandETT;

public class Thermometer : BaseDevice
{
    public event Action<BaseDevice, string, DeviceCmd> TermodatReceiving;

    public Thermometer(string name) : base(name)
    {
        IsDeviceType = "Термометр";
        IsThermomether = Visibility.Visible;
        
    }
    
    protected override void Device_Receiving(byte[] data)
    {
        var receive = Encoding.UTF8.GetString(data);
        var res = intParse(receive);
        // Debug.WriteLine($"Termodat receive - {res}");

        TermodatReceiving?.Invoke(this, res.ToString(), CurrentCmd);
    }
    
    private int intParse(string HexResponse)
    {
        HexResponse = HexResponse.Substring(7, 4);
        return Convert.ToInt32(HexResponse, 16);
    }

    public override void WriteCmd(string nameCommand, string numOrRegister = null)
    {
        NameCurrentCmd = nameCommand;
        CurrentParameter = numOrRegister;

        CurrentCmd = GetLibItem(nameCommand, Name);
        SetErrors();
        IsNotCmd = false;
        if (CurrentCmd == null)
        {
            IsNotCmd = true;
            throw new Exception(
                $"Такое устройство - {IsDeviceType}/{Name} \nили команда - {nameCommand}, в библиотеке не найдены!");
        }

        if (!string.IsNullOrEmpty(CurrentCmd.Length))
        {
            port.SetReceiveLenght(int.Parse(CurrentCmd.Length));
        }
        else
        {
            port.SetReceiveLenght(0);
        }

        var cmd = Convert.ToInt32(CurrentCmd.Transmit, 16);
        var stepOrProg = Convert.ToInt32(numOrRegister);
        string cmdPacket = GenerateCmdPacket(1, 1, cmd, 1);
        port.TransmitCmdString(cmdPacket);
    }

    private string GenerateCmdPacket(int deviceAddr, long deviceChannel, long cmd,
        long numberOfRegistersToRead) //Modbas system decode
    {
        switch (deviceChannel)
        {
            case 1:
                break;
            case 2:
                cmd += 0x0400;
                break;
            case 3:
                cmd += 0x0800;
                break;
            case 4:
                cmd += 0x0c00;
                break;
        }

        long value = 1;
        value = numberOfRegistersToRead;
        var lrc = 0;
        int sum1, sum2, sum3, sum4, sum5, sum6;
        var packet = ":";

        packet = packet + deviceAddr.ToString("X2") + "03" + cmd.ToString("X4") + value.ToString("X4");
        string sub1 = packet.Substring(1, 2);
        sum1 = Convert.ToInt16(sub1, 16);
        string sub2 = packet.Substring(3, 2);
        sum2 = Convert.ToInt16(sub2, 16);

        string sub3 = packet.Substring(5, 2);
        sum3 = Convert.ToInt16(sub3, 16);
        string sub4 = packet.Substring(7, 2);
        sum4 = Convert.ToInt16(sub4, 16);

        string sub5 = packet.Substring(9, 2);
        sum5 = Convert.ToInt16(sub5, 16);
        string sub6 = packet.Substring(11, 2);
        sum6 = Convert.ToInt16(sub6, 16);
        
        lrc = sum1 + sum2 + sum3 + sum4 + sum5 + sum6;
        lrc = ~lrc; // NOT
        lrc = lrc + 1;

        string Output = packet + lrc.ToString("X2").Substring(6, 2) + "\r\n";
        return Output;
    }
}