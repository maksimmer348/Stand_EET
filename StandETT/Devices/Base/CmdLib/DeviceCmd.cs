using System;
using Newtonsoft.Json;

namespace StandETT;

/// <summary>
/// Стандартная команда, ответ и задержка
/// </summary>
public class DeviceCmd
{
    /// <summary>
    /// Команда в устройство
    /// </summary>
    public string Transmit { get; set; }

    /// <summary>
    /// Окончание строки
    /// </summary>
    public Terminator Terminator { get; set; }

    /// <summary>
    /// Ответ от устройства
    /// </summary>
    public string Receive { get; set; }
    /// <summary>
    /// Очончание ответа от устройства
    /// </summary>
    public Terminator ReceiveTerminator { get; set; }
    /// <summary>
    /// Тип команды  и ответа от устройства (hex/text) 
    /// </summary>
    public TypeCmd MessageType { get; set; }

    /// <summary>
    /// Задержка между передачей команды и приемом ответа 
    /// </summary>
    public int Delay { get; set; }
    /// <summary>
    ///  Производитль ли над командой xor операцию
    /// </summary>
    public bool IsXor { get; set; }
    

    protected bool Equals(DeviceCmd other)
    {
        return Transmit == other.Transmit && Terminator == other.Terminator && Receive == 
            other.Receive && ReceiveTerminator == other.ReceiveTerminator && MessageType == 
            other.MessageType && Delay == other.Delay && IsXor == other.IsXor;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((DeviceCmd)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Transmit, Terminator, Receive, ReceiveTerminator, (int)MessageType, Delay, IsXor);
    }
}