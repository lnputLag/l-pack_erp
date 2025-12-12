using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Common;

namespace Client.Interfaces.Production.EnergyResources
{
    public class EnergyResourcesInterface
    {
        public EnergyResourcesInterface()
        {
            Central.WM.AddTab("EnergyResources", "Отчеты по энергоресурсам");

            var electricityReportInterface = Central.WM.CheckAddTab<ElectricityReport>("EnergyResources_ElectricityReport", "Отчет по электричеству", false, "EnergyResources", "bottom");

            var waterReportInterface = Central.WM.CheckAddTab<WaterReport>("EnergyResources_WaterReport", "Отчет по воде", false, "EnergyResources", "bottom");

            var gazReportInterface = Central.WM.CheckAddTab<GazReport>("EnergyResources_GazReport", "Отчет по газу", false, "EnergyResources", "bottom");

            var sewageReportInterface = Central.WM.CheckAddTab<SewageReport>("EnergyResources_SewageReport", "Отчет по канализации", false, "EnergyResources", "bottom");

            var steamReportInterface = Central.WM.CheckAddTab<SteamReport>("EnergyResources_SteamReport", "Отчет по пару", false, "EnergyResources", "bottom");

            var sewageYokcbReportInterface = Central.WM.CheckAddTab<SewageYokcbReport>("EnergyResources_SewageYokcbReport", "Отчет по УОКСВ", false, "EnergyResources", "bottom");

            //ставим фокус на вкладку по умолчанию
            Central.WM.SetActive("EnergyResources_ElectricityReport");
        }
    }
}
