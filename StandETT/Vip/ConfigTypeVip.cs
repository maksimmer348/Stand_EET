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
            throw new Exception($"Не создан тип Випа {type.Name}, ошибка{e}");
        }
    }

    public void RemoveTypeVips(TypeVip tv)
    {
        try
        {
            TypeVips.Remove(tv);
        }
        catch (Exception e)
        {
            throw new Exception($"Не удален тип Випа {tv.Name}, ошибка{e}");
        }
    }

    #endregion
}