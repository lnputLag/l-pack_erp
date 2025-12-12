using Client.Common;

namespace Client.Interfaces.Service.Printing
{
    /// <summary>
    /// профили печати
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-10-24</released>
    /// <changed>2023-10-24</changed>
    public class PrintingInterface
    {
        public PrintingInterface()
        {
            Central.WM.AddTab("printing", "Печать");
            Central.WM.AddTab<PrintingSettingsList>("printing");
            
            Central.WM.SetActive("PrintingSettingsList");
        }
    }
}
