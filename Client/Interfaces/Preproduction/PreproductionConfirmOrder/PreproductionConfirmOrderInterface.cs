using Client.Common;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// "Подтверждение заявок"
    /// Интерфейс для работы с заявками
    /// </summary>
    /// <author>sviridov_ae</author>
    public class PreproductionConfirmOrderInterface
    {
        public PreproductionConfirmOrderInterface()
        {
            Central.WM.AddTab("_PreproductionConfirmOrder", "Подтверждение заявок");

            Central.WM.CheckAddTab<PreproductionConfirmOrderList>("PreproductionConfirmOrderList", "Список заявок", false, "_PreproductionConfirmOrder", "bottom");
            Central.WM.CheckAddTab<PreproductionConfirmOrderCompositionList>("PreproductionConfirmOrderCompositionList", "Отчёт по композициям", false, "_PreproductionConfirmOrder", "bottom");

            Central.WM.SetActive("PreproductionConfirmOrderList");
        }
    }
}
