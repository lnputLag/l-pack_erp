using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Shipments
{
    public class DriverListKshInterface
    {
        public DriverListKshInterface()
        {
            Central.WM.AddTab(FrameName, "Регистрация водителя", true, "add");

            DriverListKshExpectedView = Central.WM.CheckAddTab<DriverListKshExpected>("DriverListKshExpected", "Ожидаемые водители", false, FrameName);
            DriverListKshExpectedView.OnChoiceDriver = ChiseDriver;
            DriverListKshExpectedView.OnClose = Close;

            DriverListKshAllView = Central.WM.CheckAddTab<DriverListKshAll>("DriverListKshAll", "Все водители", false, FrameName);
            DriverListKshAllView.OnChoiceDriver = ChiseDriver;
            DriverListKshAllView.OnClose = Close;

            Central.WM.SetActive("DriverListKshExpected");
        }

        private DriverListKshExpected DriverListKshExpectedView;

        private DriverListKshAll DriverListKshAllView;

        private string FrameName = "DriverListKsh";

        public int FactoryId = 2;

        public string RoleName = "[erp]shipment_control_ksh";

        public string ParentFrame { get; set; }

        public delegate void ChoiceDriverDelegate(Dictionary<string, string> driverItem);

        public ChoiceDriverDelegate OnChoiceDriver;

        private void ChiseDriver(Dictionary<string, string> driverItem)
        {
            OnChoiceDriver?.Invoke(driverItem);

            Close();
        }

        private void Close()
        {
            if (DriverListKshExpectedView != null)
            {
                Central.WM.Close(DriverListKshExpectedView.FrameName);
                DriverListKshExpectedView = null;
            }

            if (DriverListKshAllView != null)
            {
                Central.WM.Close(DriverListKshAllView.FrameName);
                DriverListKshAllView = null;
            }

            Central.WM.Close(FrameName);
        }

        public void SetValues()
        {
            DriverListKshExpectedView.ParentFrame = ParentFrame;
            DriverListKshExpectedView.FactoryId = FactoryId;
            DriverListKshExpectedView.RoleName = RoleName;

            DriverListKshAllView.ParentFrame = ParentFrame;
            DriverListKshAllView.RoleName = RoleName;
        }
    }
}
