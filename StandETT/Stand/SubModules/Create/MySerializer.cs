using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace StandETT;

public class MySerializer
{
    /// <summary>
    /// Сериализуемая библиотека
    /// </summary>
    public Dictionary<DeviceIdentCmd, DeviceCmd> LibCmd;

    public List<KeyValuePair<DeviceIdentCmd, DeviceCmd>> SerializedLocations
    {
        get { return LibCmd.ToList(); }
        set { LibCmd = value.ToDictionary(x => x.Key, x => x.Value); }
    }

    public void SerializeLib()
    {
        try
        {
            string json = JsonConvert.SerializeObject(SerializedLocations, Formatting.Indented);

            File.WriteAllText(@"CommandLib.json", json.ToString());
        }
        catch (Exception e)
        {
            throw new Exception($"Сериализация библиотеки неудачна код ошибки {e}");
        }
    }

    public Dictionary<DeviceIdentCmd, DeviceCmd> DeserializeLib()
    {
        try
        {
            var json =
                JsonConvert.DeserializeObject<List<KeyValuePair<DeviceIdentCmd, DeviceCmd>>>(
                    File.ReadAllText(@"CommandLib.json"));

            Dictionary<DeviceIdentCmd, DeviceCmd> temp = new Dictionary<DeviceIdentCmd, DeviceCmd>();
            foreach (var cmd in json)
            {
                temp.Add(cmd.Key, cmd.Value);
            }

            return temp;
        }
        catch (Exception e)
        {
            return null;
        }
    }


    public void SerializeDevices(List<BaseDevice> devices)
    {
        try
        {
            var json = JsonConvert.SerializeObject(devices, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });
            File.WriteAllText(@"Devices.json", json.ToString());
        }
        catch (Exception e)
        {
            throw new Exception($"Сериализация списка устройтсв неудачна код ошибки {e}");
        }
    }

    public List<BaseDevice> DeserializeDevices()
    {
        try
        {
            var json =
                JsonConvert.DeserializeObject<List<BaseDevice>>(
                    File.ReadAllText(@"Devices.json"), new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto
                    });
            return json;
        }
        catch (Exception e)
        {
            return null;
        }
    }

    public void SerializeTypeVips(List<TypeVip> types)
    {
        try
        {
            var json = JsonConvert.SerializeObject(types, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });
            File.WriteAllText(@"TypesVips.json", json.ToString());
        }
        catch (Exception e)
        {
            throw new Exception($"Сериализация типов випов неудачна код ошибки {e}");
        }
    }

    public List<TypeVip> DeserializeTypeVips()
    {
        try
        {
            var json =
                JsonConvert.DeserializeObject<List<TypeVip>>(
                    File.ReadAllText(@"TypesVips.json"), new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto
                    });
            return json;
        }
        catch (Exception e)
        {
            return new List<TypeVip>();
        }
    }

    public void SerializeTime(TimeMachine time)
    {
        try
        {
            var json = JsonConvert.SerializeObject(time, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });
            File.WriteAllText(@"Times.json", json.ToString());
        }
        catch (Exception e)
        {
            throw new Exception($"Сериализация времени код ошибки {e}");
        }
    }

    public TimeMachine DeserializeTime()
    {
        try
        {
            var json =
                JsonConvert.DeserializeObject<TimeMachine>(
                    File.ReadAllText(@"Times.json"), new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto
                    });
            return json;
        }
        catch (Exception e)
        {
            return null;
        }
    }
}