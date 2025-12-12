using Client.Common;
using Client.Interfaces.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Production
{
    public class PaperMakingSlitter2Interface
    {
        public PaperMakingSlitter2Interface()
        {
            Central.WM.AddTab("PaperMakingSlitter2", "Управление ПРС на БДМ2");
            var pmSlitter2 = Central.WM.CheckAddTab<PmSlitter2List>("PmSlitter2List", "Создание рулонов", false, "PaperMakingSlitter2", "top");
            var pmSlitter2Task = Central.WM.CheckAddTab<PaperMakingSlitter2Task>("PaperMakingSlitter2Task", "Отчет задания/рулоны", false, "PaperMakingSlitter2", "top");
            //ставим фокус на вкладку по умолчанию
            Central.WM.SetActive("PmSlitter2List");

            //            Central.WM.AddTab<PaperMakingSlitter2Prs>("PmSlitter2");
            //            Central.WM.AddTab<PaperMakingSlitter2Task>("PmSlitter2");
            //            Central.WM.SetActive("PaperMakingSlitter2Prs");
        }
    }
}
