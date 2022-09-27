using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StandETT;

public class ConfigTypeVip : Notify
{
    private static object syncRoot = new();
    private static ConfigTypeVip instance;

    public static ConfigTypeVip getInstance()
    {
        if (instance == null)
        {
            lock (syncRoot)
            {
                if (instance == null)
                    instance = new ConfigTypeVip();
            }
        }
        return instance;
    }

    public ObservableCollection<TypeVip> TypeVips = new();

    #region Типы Випов
    
    /// <summary>
    /// Тип випа от него зависит его предварительные и рабочие макс значения  
    /// </summary>
    /// <param name="type">Не удалось добавить новый тип випа</param>
    public void AddTypeVips(TypeVip type)
    {
        try
        {
            TypeVips.Add(type);
        }
        catch (Exception e)
        {
            throw new Exception($"Не создан тип Випа {type.Type}, ошибка{e}");
        }
    }

    public void RemoveTypeVips(TypeVip tv)
    {
        try
        {
            TypeVips.Remove(tv);
            //уведомить
        }
        catch (Exception e)
        {
            throw new Exception($"Не удален тип Випа {tv.Type}, ошибка{e}");
        }
    }

    #endregion

    // #region Добавление и удаление Випов
    //
    // /// <summary>
    // /// Доабавить новый Вип
    // /// </summary>
    // /// <param name="name">Имя Випа (Берется из текстбокса)</param>
    // /// <param name="indexTypeVip">Тип Випа (берется из списка который будет привязан к индексу сомбобокса)</param>
    // [ObsoleteAttribute("Метод больше не используется")]
    // public void AddVip(string name, int indexTypeVip, int id)
    // {
    //     if (!string.IsNullOrWhiteSpace(name))
    //     {
    //         // // проверка на недопуст символы 
    //         // if (!mainValidator.ValidateInvalidSymbols(name))
    //         // {
    //         //     //TODO уточнить где кидать исключение здесь или в классе MainValidator
    //         //     //TODO сделать чтобы исключение выбрасывалось при потере контекста в текстбоксе
    //         //     throw new Exception($"Название добавляемого Випа - {name}, содержит недопустимые символы");
    //         // }
    //         //
    //         // // проверка на повторяющиеся имена Випов 
    //         // if (!mainValidator.ValidateCollisionName(name, Vips))
    //         // {
    //         //     //TODO уточнить где кидать исключение здесь или в классе MainValidator
    //         //     //TODO сделать чтобы исключение выбрасывалось при потере контекста в текстбоксе
    //         //     throw new Exception($"Название добавляемого Випа - {name}, уже есть в списке");
    //         // }
    //
    //         var vip = new Vip(1, new RelayVip(id, name))
    //         {
    //             Name = name,
    //             Type = TypeVips[indexTypeVip],
    //             StatusTest = StatusDeviceTest.None
    //         };
    //         //Vips.Add(vip);
    //         Console.WriteLine("Вип имя: " + vip.Name + " был добалвен");
    //         //уведомить
    //     }
    // }
    //
    // //TODO должно срабоать при удалении текста из текстбокса 
    // /// <summary>
    // /// Удаление Випа
    // /// </summary>
    // /// <param name="indexVip">Индекс Випа (берется из списка который будет привязан к индексу сомбобокса)</param>
    // [ObsoleteAttribute("Метод больше не используется")]
    // public void RemoveVip(Vip vip)
    // {
    //     try
    //     {
    //         //Vips.Remove(vip);
    //         Console.WriteLine("Вип : " + vip.Name + " был удален");
    //         //уведомить
    //     }
    //     catch (Exception e)
    //     {
    //         throw new Exception("Вип c индексом: " + vip.Name + "не был был удален");
    //     }
    // }
    //
    // #endregion
}