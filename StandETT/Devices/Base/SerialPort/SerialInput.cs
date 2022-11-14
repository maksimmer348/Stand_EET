using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using RJCP.IO.Ports;
using SerialPortLib;


namespace StandETT;

public class SerialInput : ISerialLib
{
    protected SerialPortInput Port;
    
    public bool Dtr { get; set; }
    public string GetPortNum { get; set; }

    public int Delay { get; set; }
    
    // public long Ss { get => Port.Ss; }



    public event Action<bool> PortConnecting;
    public event Action<byte[]> Receiving;
    public event Action<string> ErrorPort;

    /// <summary>
    /// Адаптер значений для библиотеки 
    /// </summary>
    /// <param name="sBits">Stop bits (1-2)</param>
    /// <param name="par">Parity bits (0-2)</param>
    /// <param name="dBits">Data bits (5-8)</param>open
    /// <returns></returns>
    public (StopBits, Parity, DataBits) SetPortAdapter(int sBits, int par, int dBits)
    {
        StopBits stopBits = StopBits.One;
        Parity parity = Parity.None;
        DataBits dataBits = DataBits.Eight;

        stopBits = sBits switch
        {
            1 => StopBits.One,
            2 => StopBits.Two,
            _ => stopBits
        };

        parity = par switch
        {
            0 => Parity.None,
            1 => Parity.Odd,
            2 => Parity.Even,
            _ => parity
        };
        dataBits = dBits switch
        {
            5 => DataBits.Five,
            6 => DataBits.Six,
            7 => DataBits.Seven,
            8 => DataBits.Eight,
            _ => dataBits
        };
        return (stopBits, parity, dataBits);
    }

    public void SetPort(string pornName, int baud, int stopBits, int parity, int dataBits, bool dtr = false)
    {
        var adaptSettings = SetPortAdapter(stopBits, parity, dataBits);
        Port = new SerialPortInput(new NullLogger<SerialPortInput>());
        Port.ConnectionStatusChanged += OnPortConnectionStatusChanged;
        Port.MessageReceived += OnPortMessageReceived;
        Port.ErrorPort += PortOnErrorPort;
        Port.ExceptionPort += PortOnErrorPort;

        try
        {
            Port.SetPort(pornName, baud, adaptSettings.Item1, adaptSettings.Item2, adaptSettings.Item3);
            Port.DtrEnableSP = true;
            Port.DtrEnableSS = true;
        }
        catch (Exception e)
        {
            throw new Exception(
                $"SerialInput exception: Порт \"{GetPortNum}\" не конфигурирован, ошибка - {e.Message}");
        }

        GetPortNum = pornName;
    }

    private void PortOnErrorPort(Exception e)
    {
        ErrorPort?.Invoke(e.Message);
    }

    private void PortOnErrorPort(SerialError e)
    {
        if (e == SerialError.NoError)
        {
            ErrorPort?.Invoke("Indicates no error");
        }

        if (e == SerialError.RXOver)
        {
            ErrorPort?.Invoke("Driver buffer has reached 80% full");
        }

        if (e == SerialError.Overrun)
        {
            ErrorPort?.Invoke("Driver has detected an overflow");
        }

        if (e == SerialError.RXParity)
        {
            ErrorPort?.Invoke("Parity error detected");
        }

        if (e == SerialError.Frame)
        {
            ErrorPort?.Invoke("Frame error detected");
        }

        if (e == SerialError.TXFull)
        {
            ErrorPort?.Invoke("Transmit buffer is full");
        }
    }


    public bool Open()
    {
        try
        {
            if (!Port.IsConnected)
            {
                return Port.Connect();
            }

            if (Port.IsConnected)
            {
                return true;
            }
        }
        catch (Exception e)
        {
            throw new Exception(
                $"SerialInput exception: Порт \"{GetPortNum}\" не открыт, ошибка - {e.Message}");
        }

        return false;
    }

    public void Close()
    {
        try
        {
            Port.Disconnect();
        }
        catch (Exception e)
        {
            throw new Exception(
                $"SerialInput exception: Порт \"{GetPortNum}\" не закрыт, ошибка - {e.Message}");
        }
    }

    public void DtrEnable()
    {
        Port.DtrEnableSP = true;
        Port.DtrEnableSS = true;
    }

    public void SetReceiveLenght(int receiveLenght)
    {
        Port.ReceiveLenght = receiveLenght;
    }

    public void DiscardInBuffer()
    {
        Port.Flush();
        Port.DiscardOutBuffer();
        Port.DiscardInBuffer();
    }

    public void OnPortConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs args)
    {
        PortConnecting?.Invoke(args.Connected);
    }
    public void OnPortMessageReceived(object sender, MessageReceivedEventArgs args)
    {
        Receiving?.Invoke(args.Data);
    }

    public void TransmitCmdTextString(string cmd, int delay = 0, string terminator = null)
    {
        if (string.IsNullOrEmpty(cmd))
        {
            throw new Exception($"Команда - не должна быть пустой");
        }

        if (string.IsNullOrEmpty(terminator))
        {
            terminator = "\r\n";
        }

        var message = System.Text.Encoding.UTF8.GetBytes(cmd + terminator);
        try
        {
            Port.SendMessage(message);
        }
        catch (Exception e)
        {
            throw new Exception(
                $"SerialInput exception: Команда \"{message}\", в порт \"{GetPortNum}\" не отправлена, ошибка - {e.Message}");
        }
    }

    private Stopwatch s = new Stopwatch();
    public void TransmitCmdHexString(string cmd, int delay = 0, string terminator = null, bool isXor = false)
    {
       
        if (string.IsNullOrEmpty(cmd))
        {
            throw new Exception($"SerialInput exception: Команда - не должна быть пустой");
            return;
        }

        //s. входную строку команды в байтовый массив команды
        var cmdMsg = ISerialLib.StringToByteArray(cmd);

        //создаем список чтобы можно было легче приклеить xor сумму к массиву команды
        var t = new List<byte>(cmdMsg);

        if (isXor)
        {
            //массив команды складываем xor 
            var xorCalc = ISerialLib.XorCalcArr(cmdMsg);
            //приклеиваем 
            t.Add(xorCalc);
        }

        //преобразуме терминатор в строку
        if (terminator != null)
        {
            var term = ISerialLib.StringToByteArray(terminator);
            t.AddRange(term);
        }
        try
        {
           
            Port.SendMessage(t.ToArray());
        }
        catch (Exception e)
        {
            throw new Exception(
                $"Команда \"{cmdMsg}\", в порт \"{GetPortNum}\" не отправлена, ошибка - {e.Message}");
        }
    }
}