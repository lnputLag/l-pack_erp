using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Service
{
    public class BarcodeGeneratorInterface
    {
        public BarcodeGeneratorInterface()
        {
            var barcodeGeneratorTab = Central.WM.CheckAddTab<BarcodeGeneratorTab>("BarcodeGeneratorTab", "Генератор штрих-кода", true);
            Central.WM.SetActive("BarcodeGeneratorTab");
        }
    }
}

