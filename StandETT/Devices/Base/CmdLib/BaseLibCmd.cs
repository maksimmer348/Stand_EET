using System;
using System.Collections.Generic;
using System.Linq;


namespace StandETT;

public class BaseLibCmd
{
    private static BaseLibCmd instance;
    
    public List<Terminator> Terminators = new();


    public string Name { get; private set; }


    private static object syncRoot = new();

    public static BaseLibCmd getInstance()
    {
        if (instance == null)
        {
            lock (syncRoot)
            {
                if (instance == null)
                    instance = new BaseLibCmd();
            }
        }

        return instance;
    }

    public Dictionary<DeviceIdentCmd, DeviceCmd> DeviceCommands { get; set; } =
        new();


    public void CreateTerminators()
    {
        Terminators.Add(new Terminator("None", TypeTerminator.None, null, TypeCmd.Text));

        Terminators.Add(new Terminator("CR Text(\\r)", TypeTerminator.CR, "\r", TypeCmd.Text));
        Terminators.Add(new Terminator("LF Text(\\n)", TypeTerminator.LF, "\n", TypeCmd.Text));
        Terminators.Add(new Terminator("CRLF Text(\\r\\n)", TypeTerminator.CRLF, "\r\n", TypeCmd.Text));
        Terminators.Add(new Terminator("LFCR Text(\\n\\r)", TypeTerminator.LFCR, "\n\r", TypeCmd.Text));

        //
        Terminators.Add(new Terminator("None", TypeTerminator.None, null, TypeCmd.Hex));

        Terminators.Add(new Terminator("CR Hex(\\r)", TypeTerminator.CR, "0D", TypeCmd.Hex));
        Terminators.Add(new Terminator("LF Hex(\\n)", TypeTerminator.LF, "0A", TypeCmd.Hex));
        Terminators.Add(new Terminator("CRLF Hex(\\r\\n)", TypeTerminator.CRLF, "0D0A", TypeCmd.Hex));
        Terminators.Add(new Terminator("LFCR Hex(\\n\\r)", TypeTerminator.LFCR, "0A0D", TypeCmd.Hex));
    }

    /// <summary>
    /// Добавление команды в общую билиотеку команд
    /// </summary>
    /// <param name="nameCmd">Имя команды</param>
    /// <param name="nameDevice">Прибор для которого эта команда предназначена</param>
    /// <param name="transmit">Команда котороую нужно передать в прибор</param>
    /// <param name="delay">Задержка между передачей команды и приемом ответа</param>
    /// <param name="receive">Ответ от прибора на команду</param>
    /// <param name="isParam"></param>
    /// <param name="terminator">Терминатор отправляемой строки</param>
    /// <param name="receiveTerminator">Терминатор принимаемой строки</param>
    /// <param name="type">Тип ответа (по умолчанию текстовый)</param>
    /// <param name="isXor"></param>
    /// <param name="length"></param>
    /// <param name="lengthCmdLib"></param>
    public void AddCommand(string nameCmd, string nameDevice, string transmit,
        int delay, string receive = null, bool isParam = false, TypeTerminator terminator = TypeTerminator.None,
        TypeTerminator receiveTerminator = TypeTerminator.None, TypeCmd type = TypeCmd.Text,
        bool isXor = false, int length = 0)
    {
        var tTx = Terminators.First(x => x.Type == terminator && x.TypeEncod == type);
        var tRx = Terminators.First(x => x.Type == receiveTerminator && x.TypeEncod == type);
        string lenghtStr = null;
        if (length > 0)
        {
            lenghtStr = length.ToString();
        }
        
        try
        {
            var tempIdentCmd = new DeviceIdentCmd
            {
                NameCmd = nameCmd,
                NameDevice = nameDevice
            };
            var tempCmd = new DeviceCmd
            {
                Transmit = transmit,
                Terminator = tTx,
                Receive = receive,
                IsParam = isParam,
                ReceiveTerminator = tRx,
                MessageType = type,
                Delay = delay,
                IsXor = isXor,
                Length = lenghtStr
            };

            DeviceCommands.Add(tempIdentCmd, tempCmd);
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

    /// <summary>
    /// Удаление команды устройства
    /// </summary>
    /// <param name="nameCommand">Имя удаляемой команды</param>
    /// <param name="nameDevice">Имя устройства команду которого удаляют</param>
    public void DeleteCommand(DeviceIdentCmd cmd)
    {
        try
        {
            DeviceCommands.Remove(cmd);
        }
        catch (Exception e)
        {
            throw new Exception("Команда не была удалена ошибка -" + e.Message);
        }
    }

    //TODO нужно ли переписвать
    /// <summary>
    ///  Изменить значение команды по ключу
    /// </summary>
    /// <param name="nameCommandOld">Название изменяемой команды</param>
    /// <param name="nameDeviceOld">Название прибора для кторого будет изменена команда</param>
    /// <param name="transmitNew">Новое значение передваемой команды (если пусто исользуется старая команда)</param>
    /// <param name="terminatorNew">Новое значение терминатора отправляемой строки</param>
    /// <param name="receiveNew">Новое значение принримаемой команды (если пусто исользуется старая команда)</param>
    /// <param name="receiveTerminatorNew">Новое значение терминатора принимаемой строки</param>
    /// <param name="delayNew">Новое значение задержки (если 0 исользуется старая задержка)</param
    /// <param name="typeNew">Новое значение типа сообщения (если пусто исользуется старый тип)</param>
    public void ChangeCommand(string nameCommandOld, string nameDeviceOld, string transmitNew = null,
        TypeTerminator terminatorNew = TypeTerminator.None,
        TypeTerminator receiveTerminatorNew = TypeTerminator.None,
        string receiveNew = null, int delayNew = 0, TypeCmd typeNew = TypeCmd.Text)
    {
        try
        {
            var select = DeviceCommands
                .Where(x => x.Key.NameCmd == nameCommandOld)
                .FirstOrDefault(x => x.Key.NameDevice == nameDeviceOld).Key;

            if (DeviceCommands.ContainsKey(select))
            {
                var tTx = Terminators.First(x => x.Type == terminatorNew && x.TypeEncod == typeNew);
                var tRx = Terminators.First(x => x.Type == receiveTerminatorNew && x.TypeEncod == typeNew);

                var tempCmd = new DeviceCmd();
                tempCmd.Transmit = transmitNew;
                tempCmd.Terminator = tTx;
                tempCmd.Receive = receiveNew;
                tempCmd.ReceiveTerminator = tRx;
                tempCmd.Delay = delayNew;
                tempCmd.MessageType = typeNew;
                tempCmd.Receive = receiveNew;

                if (string.IsNullOrWhiteSpace(transmitNew))
                {
                    tempCmd.Transmit = DeviceCommands[select].Transmit;
                }

                if (terminatorNew == TypeTerminator.None)
                {
                    tempCmd.Terminator = DeviceCommands[select].Terminator;
                }

                if (string.IsNullOrWhiteSpace(receiveNew))
                {
                    tempCmd.Receive = DeviceCommands[select].Receive;
                }

                if (receiveTerminatorNew == TypeTerminator.None)
                {
                    tempCmd.ReceiveTerminator = DeviceCommands[select].ReceiveTerminator;
                }

                if (delayNew == 0)
                {
                    tempCmd.Delay = DeviceCommands[select].Delay;
                }

                if (typeNew == TypeCmd.Text)
                {
                    tempCmd.MessageType = DeviceCommands[select].MessageType;
                }

                DeviceCommands[select] = tempCmd;
            }
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }
}

public class Terminator
{
    /// <summary>
    /// Имя
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Имя
    /// </summary>
    public TypeTerminator Type { get; set; }

    /// <summary>
    /// Тип
    /// </summary>
    public string ReceiveTerminator { get; set; }

    /// <summary>
    /// Тип
    /// </summary>
    public TypeCmd TypeEncod { get; set; }


    public Terminator(string name, TypeTerminator type, string receiveTerminator, TypeCmd typeEncod)
    {
        Name = name;
        Type = type;
        ReceiveTerminator = receiveTerminator;
        TypeEncod = typeEncod;
    }
}

public enum TypeTerminator
{
    None,

    /// <summary>
    /// \n
    /// </summary>
    LF,

    /// <summary>
    ///  \r
    /// </summary>
    CR,

    /// <summary>
    ///  \n\r
    /// </summary>
    LFCR,

    /// <summary>
    ///  \r\n
    /// </summary>
    CRLF
}