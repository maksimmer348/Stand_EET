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

    public string Length { get; set; }

    public bool IsParam { get; set; }

    protected bool Equals(DeviceCmd other)
    {
        return Transmit == other.Transmit && Equals(Terminator, other.Terminator) && Receive == other.Receive &&
               Equals(ReceiveTerminator, other.ReceiveTerminator) && MessageType == other.MessageType &&
               Delay == other.Delay && IsXor == other.IsXor && Length == other.Length && IsParam == other.IsParam;
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
        var hashCode = new HashCode();
        hashCode.Add(Transmit);
        hashCode.Add(Terminator);
        hashCode.Add(Receive);
        hashCode.Add(ReceiveTerminator);
        hashCode.Add((int)MessageType);
        hashCode.Add(Delay);
        hashCode.Add(IsXor);
        hashCode.Add(Length);
        hashCode.Add(IsParam);
        return hashCode.ToHashCode();
    }
    
}