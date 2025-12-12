using Client.Common;
using Client.Interfaces.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Production
{
    public class PaperMakingSlitter1Interface
    {
        public PaperMakingSlitter1Interface()
        {
            Central.WM.AddTab("PaperMakingSlitter1", "Управление ПРС на БДМ1");
            var pmSlitter1 = Central.WM.CheckAddTab<PmSlitter1List>("PmSlitter1List", "Создание рулонов", false, "PaperMakingSlitter1", "top");
            var pmSlitter1Task = Central.WM.CheckAddTab<PaperMakingSlitter1Task>("PaperMakingSlitter1Task", "Отчет задания/рулоны", false, "PaperMakingSlitter1", "top");
            //ставим фокус на вкладку по умолчанию
            Central.WM.SetActive("PmSlitter1List");
        }
    }
}
