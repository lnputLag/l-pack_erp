using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Client.Interfaces.Production.PaperProduction.DeviceConfig.Device;

namespace Client.Interfaces.Production.PaperProduction
{
    /// <summary>
    /// Описание конфигурации проездов со шлагбаумами
    /// <author>eletskikh_ya</author>
    /// </summary>
    public class DeviceConfig
    {
        /// <summary>
        /// Отдельное устройство
        /// </summary>
        public class Device
        {
            public enum TypeDevice
            {
                Unknow = 0, Barrier = 1, Camera = 2, Panel = 3, Laurent = 4, Scales = 5
            };

            /// <summary>
            /// Тип устройства
            /// </summary>
            public TypeDevice Type { get; set; }
            /// <summary>
            /// ID устройства
            /// </summary>
            public int ID { get; set; }

            /// <summary>
            /// Наименование устройства
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// Перевод типа в текстовое представление
            /// </summary>
            public string TypeName
            {
                get
                {
                    string typeName = "не известно";

                    switch (this.Type)
                    {
                        case TypeDevice.Barrier:
                            typeName = "Шлагбаум";
                        break;
                        case TypeDevice.Camera:
                            typeName = "Камера";
                            break;
                        case TypeDevice.Panel:
                            typeName = "Табло";
                            break;
                        case TypeDevice.Laurent:
                            typeName = "Датчик положения";
                            break;
                        case TypeDevice.Scales:
                            typeName = "Весы";
                        break;
                    }

                    return typeName;
                }
            }

            /// <summary>
            /// Получение позиции для грида
            /// </summary>
            public Dictionary<string,string> Item
            {
                get
                {
                    return new Dictionary<string, string>() { { "ID", ID.ToString() }, { "NAME", Name }, {"TYPE", ((int)Type).ToString() }, { "TYPENAME", TypeName } };
                }
            }
        }

        /// <summary>
        /// Конфигурация ворот
        /// </summary>
        public class Gate
        {
            /// <summary>
            /// ID вороот
            /// </summary>
            public int ID { get; set; }
            /// <summary>
            /// Наименование проезда
            /// </summary>
            public string Name { get; set; }

            public Dictionary<string, string> Item
            {
                get
                {
                    return new Dictionary<string, string>() { { "ID", ID.ToString() }, { "NAME", Name } };
                }
            }

            /// <summary>
            /// Устройства находящиеся в данном проезде
            /// </summary>
            public List<Device> devices { get; set; }

        }

        /// <summary>
        /// Список проездов
        /// </summary>
        public List<Gate> gates { get; set; }

        /// <summary>
        /// Описание конфигурации всех проездов
        /// </summary>
        public DeviceConfig()
        {
            // bdm 1
            var gate = new Gate() { ID = 1, Name = "БДМ 1" };
            gate.devices = new List<Device>();
            gate.devices.Add(new Device() { Type = TypeDevice.Barrier, ID = 1, Name = "Въезд на ЛПАК" });
            gate.devices.Add(new Device() { Type = TypeDevice.Barrier, ID = 2, Name = "Выезд с ЛПАК" });
            gate.devices.Add(new Device() { Type = TypeDevice.Panel, ID = 1, Name = "Табло въезд" });
            gate.devices.Add(new Device() { Type = TypeDevice.Panel, ID = 2, Name = "Табло выезд" });
            gate.devices.Add(new Device() { Type = TypeDevice.Camera, ID = 1, Name = "Камера въезд" });
            gate.devices.Add(new Device() { Type = TypeDevice.Camera, ID = 2, Name = "Камера выезд" });
            gate.devices.Add(new Device() { Type = TypeDevice.Scales, ID = 1, Name = "Весы" });
            gate.devices.Add(new Device() { Type = TypeDevice.Laurent, ID = 1, Name = "Датчик положения" });

            // bdm 2
            var gate2 = new Gate() { ID = 2, Name = "БДМ 2" };
            gate2.devices = new List<Device>();
            gate2.devices.Add(new Device() { Type = TypeDevice.Barrier, ID = 3, Name = "Въезд на ЛПАК" });
            gate2.devices.Add(new Device() { Type = TypeDevice.Barrier, ID = 4, Name = "Выезд с ЛПАК" });
            gate2.devices.Add(new Device() { Type = TypeDevice.Panel, ID = 3, Name = "Табло въезд" });
            gate2.devices.Add(new Device() { Type = TypeDevice.Panel, ID = 4, Name = "Табло выезд" });
            gate2.devices.Add(new Device() { Type = TypeDevice.Camera, ID = 3, Name = "Камера въезд" });
            gate2.devices.Add(new Device() { Type = TypeDevice.Camera, ID = 4, Name = "Камера выезд" });
            gate2.devices.Add(new Device() { Type = TypeDevice.Scales, ID = 2, Name = "Весы" });
            gate2.devices.Add(new Device() { Type = TypeDevice.Laurent, ID = 2, Name = "Датчик положения" });

            // sgp
            var gate3 = new Gate() { ID = 3, Name = "СГП" };

            gate3.devices = new List<Device>();
            gate3.devices.Add(new Device() { Type = TypeDevice.Barrier, ID = 5, Name = "Вход" });
            gate3.devices.Add(new Device() { Type = TypeDevice.Camera, ID = 5, Name = "Камера" });

            gates = new List<Gate>() { gate, gate2, gate3 };
        }

        /// <summary>
        /// получение списка всех проездов
        /// </summary>
        /// <returns></returns>
        public ListDataSet GetGatesList()
        {
            List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();

            foreach (var gate in gates)
            {
                var item = gate.Item.ToDictionary(entry => entry.Key,
                                       entry => entry.Value);
                result.Add(item);
            }

            return ListDataSet.Create(result);
        }

        /// <summary>
        /// Получение всех устройств проезда
        /// </summary>
        /// <param name="id">проезд</param>
        /// <returns></returns>
        public ListDataSet GetGateDevices(int id)
        {
            List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();

            foreach (var gate in gates)
            {
                if (gate.ID == id)
                {
                    foreach (var device in gate.devices)
                    {
                        var item = device.Item.ToDictionary(entry => entry.Key,
                                               entry => entry.Value);

                        item.Add("LOCATION", gate.Name);
                        result.Add(item);
                    }
                }
            }

            return ListDataSet.Create(result);
        }
        /// <summary>
        /// Получение списка всех устройств по типу
        /// </summary>
        /// <param name="type">тип устройства</param>
        /// <returns></returns>
        public ListDataSet GetDevices(TypeDevice type)
        {
            List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();

            foreach (var gate in gates)
            {
                foreach(var device in gate.devices)
                {
                    if(device.Type == type)
                    {
                        var item = device.Item.ToDictionary(entry => entry.Key,
                                               entry => entry.Value);

                        item.Add("LOCATION", gate.Name);
                        result.Add(item);
                    }
                }
            }

            return ListDataSet.Create(result);
        }
    }
}
