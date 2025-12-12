using Client.Common;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс "Картон для образцов"
    /// </summary>
    /// <author>Рясной П.В.</author>
    public class SampleCardboardInterface
    {
        public SampleCardboardInterface()
        {
            Central.WM.AddTab("SampleCardboardMain", "Картон для образцов");

            string sampleCardboardName = "SampleCardboardList";
            var sampleCardboardList = Central.WM.CheckAddTab<SampleCardboardList>(sampleCardboardName, "Заготовки для образцов", false, "SampleCardboardMain", "bottom");
            sampleCardboardList.TabName = sampleCardboardName;

            string sampleCardboardTaskName = "SampleCardboardTaskList";
            var sampleCardboardTaskList = Central.WM.CheckAddTab<SampleCardboardTaskList>(sampleCardboardTaskName, "ПЗ на заготовки", false, "SampleCardboardMain", "bottom");
            sampleCardboardTaskList.TabName = sampleCardboardTaskName;

            string sampleCardboardStockName = "SampleCardboardStockList";
            var sampleCardboardStockList = Central.WM.CheckAddTab<SampleCardboardStockList>(sampleCardboardStockName, "Получение заготовок", false, "SampleCardboardMain", "bottom");
            sampleCardboardStockList.TabName = sampleCardboardStockName;

            // Добавляем вкладку Отчет по композициям пользователю со спецправами
            var mode = Central.Navigator.GetRoleLevel("[erp]sample_cardboard");
            if (mode == Role.AccessMode.Special)
            {
                string sampleCardboardPlanListName = "SampleCardboardPlanList";
                var sampleCardboardCompositionList = Central.WM.CheckAddTab<PreproductionConfirmOrderCompositionList>(sampleCardboardPlanListName, "Планирование сырья", false, "SampleCardboardMain", "bottom");
                sampleCardboardCompositionList.ControlName = sampleCardboardPlanListName;
            }

            //ставим фокус на вкладку по умолчанию
            Central.WM.SetActive(sampleCardboardName);

            if (Central.Navigator.Address.Processed != true)
            {
                //переключим вкладку, если работает навигация
                if (!string.IsNullOrEmpty(Central.Navigator.Address.AddressInner[0]))
                {
                    var tab = Central.Navigator.Address.AddressInner[0];
                    switch (tab)
                    {
                        case "list":
                            Central.WM.SetActive(sampleCardboardName);
                            sampleCardboardList.ProcessNavigation();
                            break;
                        case "task":
                            Central.WM.SetActive(sampleCardboardTaskName);
                            sampleCardboardTaskList.ProcessNavigation();
                            break;
                        case "stock":
                            Central.WM.SetActive(sampleCardboardStockName);
                            sampleCardboardStockList.ProcessNavigation();
                            break;
                    }
                }
            }
        }
    }
}
