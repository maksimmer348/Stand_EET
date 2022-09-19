using System;
using System.Linq;
using System.Text;
//using System.Threading;
using GodSharp.SerialPort;

namespace StandETT;

public class SerialGod : ISerialLib
{
    private GodSerialPort port;
    public bool Dtr { get; set; }
    public string GetPortNum { get; set; }

    public int Delay { get; set; }
    public void DiscardInBuffer()
    {
        port.DiscardInBuffer();
    }

    public Action<bool> PortConnecting { get; set; }
    public Action<byte[]> Receiving { get; set; }
    public Action<string> ErrorPort { get; set; }

    

    public void SetPort(string pornName, int baud, int stopBits, int parity, int dataBits, bool dtr = false)
    {
        try
        {
            port = new GodSerialPort(pornName, baud, parity, dataBits, stopBits);
            port.DtrEnable = true;
            GetPortNum = pornName;
        }
        catch (Exception e)
        {
            throw new Exception(
                $"SerialGod exception: Порт \"{GetPortNum}\" не конфигурирован, ошибка - {e.Message}");
        }
    }

    
  

    public bool Connect()
    {
        try
        {
            var isConnect = port.Open();
            PortConnecting?.Invoke(isConnect);

            return isConnect;
        }
        catch (Exception e)
        {
            throw new Exception($"SerialGod exception: Порт \"{GetPortNum}\" не отвечает, ошибка - {e.Message}");
        }
    }

    public bool Open()
    {
        return port.Open();
    }

    public void Close()
    {
        port.Close();
    }

    public void Disconnect()
    {
        try
        {
            port.Close();
        }
        catch (Exception e)
        {
            throw new Exception($"SerialGod exception: Порт \"{GetPortNum}\" не отвечает, ошибка - {e.Message}");
        }
    }
    
    // "0A""0D"
    private string[] trashStr = { "FE", "FC", "F8", "FF", "F0", "E0", " ", "C0" };

    /// <summary>
    /// Отправка и прием сообщений для Psp 405
    /// </summary>
    /// <param name="startOfString">Начало строки</param>
    /// <param name="endOfString">Конец строки</param>
    /// <param name="countReads">Общее количетво попыток считать данные</param>
    /// <param name="innerCount">Колво внутренних попыток сиать данные (по умолчанию 10)</param>
    /// <returns>Ответ от прибора</returns>
    /// <exception cref="Exception"></exception>
    public string ReadWritePsp(string startOfString, string endOfString = "", int countReads = 3,int innerCount = 10)
    {
        int innerNullCount = innerCount;
        int innerErrorCount = innerCount;

        for (int i = 0; i < countReads; i++)
        {
            var s = port.ReadString();
            //если не прочиатлась строка 
            while (s == null)
            {
                //пытаемся еще раз прочиать
                s = port.ReadString();
                
                
                //Thread.Sleep(10);
                
                
                //если innerCount раз не прочиатлась то вызов исключения
                innerNullCount--;

                if (innerNullCount == 0)
                {
                    throw new Exception($"SerialGod exception: Ответ от устройства - null, удачных попыток {innerCount}");
                }
            }

            //удаляем мусори и пробелы из строки
            foreach (var str in trashStr)
            {
                s = s.Replace(str, "");
            }

            //если строка содержит входной символ
            while (s.Contains(startOfString))
            {
                //то мы прибавляем строку из буффера компорта
                s += port.ReadString();
                s = s.Replace(" ", "");
                //TODO переделать (0D == \r, 0A == \n)
                //проверяемем есть ли в строке сиволы окончания
                if (string.Equals(s.Substring(s.Length - endOfString.Length), endOfString,
                        StringComparison.CurrentCultureIgnoreCase))
                {
                    //проверка на дублирование ответа если дублирован убираем 2 половину
                    if (s.Substring(0, s.Length / 2) == s.Substring(s.Length / 2, s.Length / 2))
                    {
                        s = s.Substring(0, s.Length / 2);
                    }

                    //возвращаем строку
                    var ss = ISerialLib.StringToByteArray(s);
                    Receiving.Invoke(ss);
                    return s;
                }

                innerErrorCount--;
                //тк мы в while если количевто попыток прочиать строку превысит 10 попыток то выходим из цикла
                //и выбрасываем исключение
                if (innerErrorCount <= 0)
                {
                    throw new Exception($"SerialGod exception: Слишком много неудачых попыток, ответ от устройства - notNull - {innerCount}");
                }

                //Thread.Sleep(10);
            }

            if (!s.Contains(startOfString))
            {
                port.DiscardInBuffer();
                port.DiscardOutBuffer();
                //Thread.Sleep(20);
                TransmitCmdTextString(startOfString);
            }
        }
        throw new Exception($"SerialGod exception: Ответа нет, неудачых попыток {countReads}");
    }
    
    public void TransmitCmdTextString(string cmd, int delay = 0, string terminator = null)
    {
        if (string.IsNullOrEmpty(cmd))
        {
            throw new Exception($"SerialGod exception: При ручном вводе, checkCmd- не должны быть пустыми");
        }

        if (string.IsNullOrEmpty(terminator))
        {
            terminator = "0A0D";
        }

        Delay = delay;
        try
        {
            port.WriteHexString(cmd + terminator);
            //Thread.Sleep(30);
            // ReadWritePsp(start, end);
        }
        catch (Exception e)
        {
            throw new Exception("SerialGod exception: " + e.Message);
        }
    }
    
    
    public void TransmitCmdHexString(string cmd, int delay = 0, string terminator = null, bool isXor = false)
    {
        TransmitCmdTextString(ISerialLib.GetStringHexInText(cmd), delay, ISerialLib.GetStringHexInText(terminator));
    }

    public void DtrEnable()
    {
        port.DtrEnable = true;
    }
}