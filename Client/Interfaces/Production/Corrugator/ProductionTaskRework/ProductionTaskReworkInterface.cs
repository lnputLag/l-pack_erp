using Client.Common;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Интерфейс Раскрой по ПЗ
    /// </summary>
    /// <author>Рясной П.В.</author>
    class ProductionTaskCMReworkInterface
    {
        /// <summary>
        /// Интерфейс Раскрой по ПЗ
        /// </summary>
        public ProductionTaskCMReworkInterface(int factoryId)
        {
            string mainTabTitle = "Раскрой по ПЗ";
            if (factoryId == 2)
            {
                mainTabTitle = $"{mainTabTitle} КШ";
            }
            string mainTabName = $"ProductionTaskRework{factoryId}";

            Central.WM.AddTab(mainTabName, mainTabTitle);

            var reworkFromTask = new ProductionTaskReworkFromTask();
            reworkFromTask.TabName = $"ProductionTaskRework_ListFromTask{factoryId}";
            Central.WM.AddTab(reworkFromTask.TabName, "Перераскрой", false, mainTabName, reworkFromTask, "bottom");
            reworkFromTask.FactoryId = factoryId;
            reworkFromTask.Initialize();

            var reworkDuplicate = new ProductionTaskReworkListCompleted();
            reworkDuplicate.TabName = $"ProductionTaskRework_ListDuplicate{factoryId}";
            Central.WM.AddTab(reworkDuplicate.TabName, "Дублирование", false, mainTabName, reworkDuplicate, "bottom");
            reworkDuplicate.FactoryId = factoryId;
            reworkDuplicate.Init();

            //ставим фокус на вкладку по умолчанию
            Central.WM.SetActive(reworkFromTask.TabName);

            //если навигация не отработана, отрабатываем
            if (Central.Navigator.Address.Processed != true)
            {
                //переключим вкладку, если работает навигация            
                if (Central.Navigator.Address.AddressInner.Count > 0)
                {
                    /*
                        Если мы пришли через навигатор, смотрим на первый элемент в списке 
                        внутренних частей адреса. В нем должно быть имя таба, который нужно открыть
                        http://192.168.3.237/developer/l-pack-erp/client/infra/navigation
                     */
                    if (!string.IsNullOrEmpty(Central.Navigator.Address.AddressInner[0]))
                    {
                        var tab = Central.Navigator.Address.AddressInner[0];
                        switch (tab)
                        {
                            case "from":
                                Central.WM.SetActive(reworkFromTask.TabName);
                                break;

                            case "clone":
                                Central.WM.SetActive(reworkDuplicate.TabName);
                                break;

                        }
                    }
                }

            }
        }
    }
}
