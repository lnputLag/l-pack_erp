using Client.Common;
using Client.Interfaces.Stock;

namespace Client.Interfaces.Production.MoldedContainer
{ 
    /// <summary>
    /// список простоев на литой таре для станков 
    /// 311 - Принтер BST и 321- Этикетер BST
    /// </summary>
    /// <author>greshnyh_ni</author>
    /// <version>1</version>
    /// <released>2024-07-30</released>3
    /// <changed>2024-07-30</changed>
    public class MoldedContainerRecyclingIdlesInterface
    {
        public MoldedContainerRecyclingIdlesInterface()
        {
            var recyclingMachineReportIdles = Central.WM.CheckAddTab<RecyclingMachineReportIdles>("RecyclingMachineReportIdles", "Простои ЛТ", true);

            Central.WM.SetActive("RecyclingMachineReportIdles");
        }
    }
}
