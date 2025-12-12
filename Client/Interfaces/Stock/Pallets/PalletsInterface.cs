using Client.Common;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Интерфейс операций с поддонами
    /// </summary>
    public class PalletInterface
    {
        public PalletInterface()
        {
            Central.WM.AddTab("Pallets", "Поддоны");

            // поддоны
            var palletList = new PalletList();
            Central.WM.AddTab("Pallets_List", "Поддоны", false, "Pallets", palletList, "bottom");

            // приход поддонов
            var palletReceipt = new PalletReceiptList();
            Central.WM.AddTab("Pallets_Receipt", "Приход поддонов", false, "Pallets", palletReceipt, "bottom");

            //Возвратные поддоны
            var palletReturnable = new PalletReturnableList();
            Central.WM.AddTab("Pallets_Returnable", "Возвратные поддоны", false, "Pallets", palletReturnable, "bottom");

            //расход поддонов
            var palletExpenditure = new PalletExpenditureList();
            Central.WM.AddTab("Pallets_Expenditure", "Расход поддонов", false, "Pallets", palletExpenditure, "bottom");

            // баланс поддонов
            var palletBalance = new PalletBalanceList();
            Central.WM.AddTab("Pallets_Balance", "Баланс поддонов", false, "Pallets", palletBalance, "bottom");

            //ставим фокус на вкладку по умолчанию
            Central.WM.SetActive("Pallets_List");

            //если навигация не отработана, отрабатываем
            if (Central.Navigator.Address.Processed != true)
            {
                //переключим вкладку, если работает навигация            
                if (Central.Navigator.Address.AddressInner.Count > 0)
                {
                    
                    //  Если мы пришли через навигатор, смотрим на первый элемент в списке 
                    //  внутренних частей адреса. В нем должно быть имя таба, который нужно открыть
                    //   http://192.168.3.237/developer/l-pack-erp/client/infra/navigation

                    /*
                        l-pack://l-pack_erp/stock/pallets

                        l-pack://l-pack_erp/stock/pallets/returnable
                        l-pack://l-pack_erp/stock/pallets/receipt
                        l-pack://l-pack_erp/stock/pallets/in
                        l-pack://l-pack_erp/stock/pallets/expenditure
                        l-pack://l-pack_erp/stock/pallets/out
                        l-pack://l-pack_erp/stock/pallets/balance
                     */
                     
                    if (!string.IsNullOrEmpty(Central.Navigator.Address.AddressInner[0]))
                    {
                        var tab = Central.Navigator.Address.AddressInner[0];
                        switch (tab)
                        {
                            case "returnable":
                                Central.WM.SetActive("Pallets_Returnable");
                                palletReturnable.ProcessNavigation();
                                break;

                            case "receipt":
                            case "in":
                                Central.WM.SetActive("Pallets_Receipt");
                                palletReceipt.ProcessNavigation();
                                break;

                            case "expenditure":
                            case "out":
                                Central.WM.SetActive("Pallets_Expenditure");
                                palletExpenditure.ProcessNavigation();
                                break;

                            case "balance":
                                Central.WM.SetActive("Pallets_Balance");
                                palletBalance.ProcessNavigation();
                                break;

                        }
                    }
                }
            }
        }
    }
}
