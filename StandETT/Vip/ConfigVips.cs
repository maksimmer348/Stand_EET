using System;
using System.Collections.ObjectModel;

namespace StandETT;

public class ConfigVips : Notify
{
    public ObservableCollection<Vip> Vips { get; set; } = new ObservableCollection<Vip>();
   
    private ObservableCollection<TypeVip> typeVips = new();
    public ObservableCollection<TypeVip> TypeVips

{
        get => typeVips;
        set => Set(ref typeVips, value);
    }
    #region Типы Випов


    /// <summary>
    /// Предваритльное добавление типов Випов
    /// </summary>
    public void PrepareAddTypeVips()
    {
        var typeVip70 = new TypeVip
        {
            Type = "Vip70",
            MaxTemperature = 70,
            //максимаьные значения во время испытаниий они означают ошибку
            MaxVoltageIn = 220,
            MaxVoltageOut1 = 40,
            MaxVoltageOut2 = 45,
            MaxCurrentIn = 2.5,
            //максимальные значения во время предпотготовки испытания (PrepareMaxVoltageOut1 и PrepareMaxVoltageOut2
            //берутся из MaxVoltageOut1 и MaxVoltageOut2 соотвественно)
            PrepareMaxCurrentIn = 0.5,
            //процент погрешности измерения
            PercentAccuracyCurrent = 10,
            PercentAccuracyVoltages = 5,
        };
        //настройки для приборов они зависят от типа Випа
        typeVip70.SetDeviceParameters(new DeviceParameters()
        {
            BigLoadValues = new BigLoadValues("300", "4", "2", "40", "1", "0"),
            HeatValues = new HeatValues("1", "0"),
            SupplyValues = new SupplyValues("2", "1", "1", "0"),
            ThermoCurrentValues = new ThermoCurrentMeterValues("100","1", "0"),
            VoltValues = new VoltMeterValues("100", "1", "0")
        });
        typeVip70.BaseDeviceValues.Add(typeVip70.GetDeviceParameters().BigLoadValues);
        typeVip70.BaseDeviceValues.Add(typeVip70.GetDeviceParameters().HeatValues);
        typeVip70.BaseDeviceValues.Add(typeVip70.GetDeviceParameters().SupplyValues);
        typeVip70.BaseDeviceValues.Add(typeVip70.GetDeviceParameters().ThermoCurrentValues);
        var typeVip71 = new TypeVip
        {
            Type = "Vip71",
            //максимаьные значения во время испытаниий они означают ошибку
            MaxTemperature = 90,
            MaxVoltageIn = 120,
            MaxVoltageOut1 = 20,
            MaxVoltageOut2 = 25,
            MaxCurrentIn = 5,
            //максимальные значения во время предпотготовки испытания (PrepareMaxVoltageOut1 и PrepareMaxVoltageOut2
            //берутся из MaxVoltageOut1 и MaxVoltageOut2 соотвественно)
            PrepareMaxCurrentIn = 0.5,
            //процент погрешности измерения
            PercentAccuracyCurrent = 10,
            PercentAccuracyVoltages = 5,
        };
        //настройки для приборов они зависят от типа Випа
        typeVip71.SetDeviceParameters(new DeviceParameters()
        {
            BigLoadValues = new BigLoadValues("200", "3.3", "1.65", "20", "1", "0"),
            HeatValues = new HeatValues("1", "0"),
            SupplyValues = new SupplyValues("3", "2", "1", "0"),
            ThermoCurrentValues = new ThermoCurrentMeterValues("10","1", "0"),
            VoltValues = new VoltMeterValues("100", "1", "0")
        });
        typeVip71.BaseDeviceValues.Add(typeVip70.GetDeviceParameters().BigLoadValues);
        typeVip71.BaseDeviceValues.Add(typeVip70.GetDeviceParameters().HeatValues);
        typeVip71.BaseDeviceValues.Add(typeVip70.GetDeviceParameters().SupplyValues);
        typeVip71.BaseDeviceValues.Add(typeVip70.GetDeviceParameters().ThermoCurrentValues);
        typeVip71.BaseDeviceValues.Add(typeVip70.GetDeviceParameters().VoltValues);
        AddTypeVips(typeVip70);
        AddTypeVips(typeVip71);
    }

    /// <summary>
    /// Тип випа от него зависит его предварительные и рабочие макс значения  
    /// </summary>
    /// <param name="type">Не удалось добавить новый тип випа</param>
    public void AddTypeVips(TypeVip type)
    {
        try
        {
            TypeVips.Add(type);
            Console.WriteLine($"Создан тип Випа {type.Type}, максимальная тепмпература {type.MaxTemperature}, " +
                              $"максимальнный предварительный ток 1 {type.PrepareMaxVoltageOut1}, " +
                              $"максимальнный предварительный ток 2 {type.PrepareMaxVoltageOut2}");
            //уведомить
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

    //public void ChangedTypeVips(int indextypeVip, TypeVip newTypeVips)
    //{
    //    try
    //    {
    //        //Console.WriteLine($"До изменения типа Випа {TypeVips[indextypeVip].PrepareMaxVoltageOut1}, {TypeVips[indextypeVip].PrepareMaxVoltageOut2}");
    //        Console.WriteLine(
    //            $"До изменения типа Випа {TypeVips[indextypeVip].MaxVoltageOut1}, {TypeVips[indextypeVip].MaxVoltageOut2}");
    //        TypeVips[indextypeVip] = newTypeVips;
    //        //Console.WriteLine($"После изменения тип Випа {TypeVips[indextypeVip].PrepareMaxVoltageOut1}, {TypeVips[indextypeVip].PrepareMaxVoltageOut2}");
    //        Console.WriteLine(
    //            $"После изменения тип Випа {TypeVips[indextypeVip].MaxVoltageOut1}, {TypeVips[indextypeVip].MaxVoltageOut2}");
    //    }
    //    catch (Exception e)
    //    {
    //        throw new Exception($"Не изменен тип Випа {TypeVips[indextypeVip]}, ошибка{e}");
    //    }
    //}

    #endregion

    #region Добавление и удаление Випов

    /// <summary>
    /// Доабавить новый Вип
    /// </summary>
    /// <param name="name">Имя Випа (Берется из текстбокса)</param>
    /// <param name="indexTypeVip">Тип Випа (берется из списка который будет привязан к индексу сомбобокса)</param>
    [ObsoleteAttribute("Метод больше не используется")]
    public void AddVip(string name, int indexTypeVip, int id)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            // // проверка на недопуст символы 
            // if (!mainValidator.ValidateInvalidSymbols(name))
            // {
            //     //TODO уточнить где кидать исключение здесь или в классе MainValidator
            //     //TODO сделать чтобы исключение выбрасывалось при потере контекста в текстбоксе
            //     throw new Exception($"Название добавляемого Випа - {name}, содержит недопустимые символы");
            // }
            //
            // // проверка на повторяющиеся имена Випов 
            // if (!mainValidator.ValidateCollisionName(name, Vips))
            // {
            //     //TODO уточнить где кидать исключение здесь или в классе MainValidator
            //     //TODO сделать чтобы исключение выбрасывалось при потере контекста в текстбоксе
            //     throw new Exception($"Название добавляемого Випа - {name}, уже есть в списке");
            // }

            var vip = new Vip(1, new RelayVip(id,name))
            {
                Name = name,
                Type = TypeVips[indexTypeVip],
                StatusTest = StatusDeviceTest.None
            };
            Vips.Add(vip);
            Console.WriteLine("Вип имя: " + vip.Name + " был добалвен");
            //уведомить
        }
    }

    //TODO должно срабоать при удалении текста из текстбокса 
    /// <summary>
    /// Удаление Випа
    /// </summary>
    /// <param name="indexVip">Индекс Випа (берется из списка который будет привязан к индексу сомбобокса)</param>
    [ObsoleteAttribute("Метод больше не используется")]
    public void RemoveVip(Vip vip)
    {
        try
        {
            Vips.Remove(vip);
            Console.WriteLine("Вип : " + vip.Name + " был удален");
            //уведомить
        }
        catch (Exception e)
        {
            throw new Exception("Вип c индексом: " + vip.Name + "не был был удален");
        }
    }

    #endregion
}