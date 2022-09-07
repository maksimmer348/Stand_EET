using System;
using System.Linq;
using System.Text;

namespace StandETT;

public interface ISerialLib
{
    /// <summary>
    /// DTR прибора
    /// </summary>
    public bool Dtr { get; set; }

    /// <summary>
    /// Получение номера порта
    /// </summary>
    public string GetPortNum { get; set; }

    /// <summary>
    /// Задержка на выполнение команды/ожидание ответа от устройства
    /// </summary>
    public int Delay { get; set; }

    /// <summary>
    /// Открыть компорт
    /// </summary>
    /// <returns></returns>
    public bool Open();

    /// <summary>
    /// Закрыть компорт
    /// </summary>
    /// <returns></returns>
    public void Close();

    /// <summary>
    /// Событие конекта к порту 
    /// </summary>
    Action<bool> PortConnecting { get; set; }

    /// <summary>
    /// Событие ответа устройства
    /// </summary>
    Action<byte[]> Receiving { get; set; }
    
    /// <summary>
    /// Настройка порта 
    /// </summary>
    /// <param name="pornName">Имя (например COM32)</param>
    /// <param name="baud">Baud rate (например 2400)</param>
    /// <param name="stopBits">Stop bits (например 1)</param>
    /// <param name="parity">Parity bits (например 0)</param>
    /// <param name="dataBits">Data bits (напрмиер 8)</param>
    /// <param name="dtr">Dtr - по умолчанию false (напрмие true)</param>
    public void SetPort(string pornName, int baud, int stopBits, int parity, int dataBits, bool dtr = false);


    /// <summary>
    /// Отправка в устройство и прием команд из устройства в виде текстовой строки
    /// </summary>
    /// <param name="cmd">Команда</param>
    /// <param name="delay">Задержка между запросом и ответом</param>
    /// <param name="terminator">Окончание строки команды - по умолчанию \n\r или 0D0A </param>
    public void TransmitCmdTextString(string cmd, int delay = 0, string terminator = null);

    /// <summary>
    /// Отправка в устройство и прием команд из устройства в виде хекс строки
    /// </summary>
    /// <param name="cmd">Команда</param>
    /// <param name="delay">Задержка между запросом и ответом</param>
    /// <param name="start">Начало строки для библиотеки SerialGod </param>
    /// <param name="end">Конец строки для библиотеки SerialGod</param>
    /// <param name="terminator">Окончание строки команды - по умолчанию \n\r или 0D0A </param>
    /// <param name="b"></param>
    public void TransmitCmdHexString(string cmd, int delay = 0, string terminator = null, bool isXor = false);

    /// <summary>
    /// Преборазование строки в массив байт
    /// </summary>
    /// <param name="hex">byte[]</param>
    /// <returns></returns>
    public static byte[] StringToByteArray(string hex)
    {
        return Enumerable.Range(0, hex.Length)
            .Where(x => x % 2 == 0)
            .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
            .ToArray();
    }

    /// <summary>
    /// Преборазование текстовой строки в хексовую строку
    /// </summary>
    /// <param name="s">string Text</param>
    /// <returns></returns>
    public static string GetStringTextInHex(string s)
    {
        if (!string.IsNullOrEmpty(s))
        {
            byte[] bytes = new byte[s.Length / 2];
            for (int i = 0; i < s.Length; i += 2)
            {
                var ff = bytes[i / 2];
                bytes[i / 2] = Convert.ToByte(s.Substring(i, 2), 16);
            }

            return Encoding.ASCII.GetString(bytes);
        }

        return "";
    }

    /// <summary>
    /// Преборазование хексовой строки в текстовую строку
    /// </summary>
    /// <param name="s">string Hex</param>
    /// <returns></returns>
    public static string GetStringHexInText(string s)
    {
        if (!string.IsNullOrEmpty(s))
        {
            string hex = "";
            foreach (var ss in s)
            {
                hex += Convert.ToByte(ss).ToString("x2");
            }

            return hex;
        }

        return "";
    }

    /// <summary>
    /// Xor калькулятор для хекс строки 
    /// </summary>
    /// <param name="bArr"></param>
    /// <returns></returns>
    public static byte XorCalcArr(byte[] bArr)
    {
        var xor = 0;

        foreach (var b in bArr)
        {
            xor ^= b;
        }

        return (byte)xor;
    }
}