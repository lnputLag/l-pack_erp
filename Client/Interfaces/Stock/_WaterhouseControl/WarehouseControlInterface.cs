using System;
using Client.Common;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Интерфейс Управление складом
    /// </summary>
    /// <author>eletskikh_ya</author>
    class WarehouseControlInterface
    {
        public WarehouseControlInterface()
        {
            //главная вкладка 
            Central.WM.AddTab("WarehouseControl", "Управление складом");

            Central.WM.CheckAddTab<WaterhouseState>("WaterhouseState", "Состояние склада", false, "WarehouseControl");

            Central.WM.CheckAddTab<WaterhouseRackShelf>("WaterhouseRackShelf", "Пролёт", false, "WarehouseControl");

            Central.WM.CheckAddTab<WarehouseListArrival>("WarehouseListArrival", "Поступление складских единиц", false, "WarehouseControl");

            Central.WM.CheckAddTab<WarehouseItemAccounting>("WarehouseItemAccounting", "Учет складских единиц", false, "WarehouseControl");

            Central.WM.CheckAddTab<WarehouseListConsumption>("WarehouseListConsumption", "Списание складских единиц", false, "WarehouseControl");

            Central.WM.CheckAddTab<WarehouseTask>("WarehouseTask", "Задачи", false, "WarehouseControl");

            Central.WM.CheckAddTab<WarehouseOperation>("WarehouseOperation", "Операции", false, "WarehouseControl");

            Central.WM.CheckAddTab<WarehouseGeographicalMap>("WarehouseGeographicalMap", "Карта склада", false, "WarehouseControl");

            Central.WM.SetActive("WaterhouseState");
        }
    }
}
