using Client.Common;

namespace Client.Interfaces.Orders.MoldedContainer
{
    /// <summary>
    /// Интерфейс для управления "Заявки на ЛТ"
    /// </summary>
    public class MoldedContainerOrderInterface
    {
        public MoldedContainerOrderInterface()
        {
            Central.WM.AddTab<MoldedContainerOrderTab>("MoldedContainerOrdersMain", true);
            Central.WM.SetActive("MoldedContainerOrderTab");
        }
    }
}
