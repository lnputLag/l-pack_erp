using Client.Common;
using Client.Interfaces.Stock;

namespace Client.Interfaces.Production.MoldedContainer
{ 
    /// <summary>
    /// производственные задания на литую тару для станков 
    /// 311 - Принтер BST и 321- Этикетер BST
    /// </summary>
    /// <author>greshnyh_ni</author>
    /// <version>1</version>
    /// <released>2024-07-30</released>3
    /// <changed>2024-07-30</changed>
    public class MoldedContainerRecyclingInterface
    {
        public MoldedContainerRecyclingInterface()
        {
            var recyclingTab2 = Central.WM.CheckAddTab<RecyclingTab2>("RecyclingTab2", "Переработка ЛТ", true);

            Central.WM.SetActive("RecyclingTab2");
        }
    }
}
