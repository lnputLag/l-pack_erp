using Client.Common;

namespace Client.Interfaces.Test
{
    public class UiLabInterface
    {
        public UiLabInterface()
        {
            Central.WM.AddTab("ui_lab", "Лаборатория интерфейсов");
            

            var form0Test = new Form0Test();
            Central.WM.AddTab("TestUI_Form0Test", "Form", false, "ui_lab", form0Test, "bottom");

            //Central.WM.AddTab<TestGrid4>("ui_lab");
            Central.WM.AddTab<Client.Interfaces.Accounts.DepartmentTab4>("ui_lab");
            Central.WM.AddTab<Client.Interfaces.Accounts.AccountTab4>("ui_lab");
            Central.WM.AddTab<Client.Interfaces.Accounts.EmailTab4>("ui_lab");

            Central.WM.SetActive("AccountTab4");
        }
    }
}
