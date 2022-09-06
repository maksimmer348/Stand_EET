using Newtonsoft.Json;

namespace StandETT;
    public class DeviceIdentCmd
    {
        //public TypeDevice TypeDevice { get; set; }
        
        /// <summary>
        /// Имя устройства
        /// </summary>
        public string NameDevice { get; set; }
        /// <summary>
        /// Имя команды
        /// </summary>
        public string NameCmd { get; set; }
    }
