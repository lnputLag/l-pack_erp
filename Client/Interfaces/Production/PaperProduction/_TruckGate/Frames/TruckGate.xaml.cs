using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Office.Interop.Excel;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.PaperProduction
{
    /// <summary>
    /// Interaction logic for TruckGate.xaml
    /// </summary>
    public partial class TruckGate : UserControl
    {
        public TruckGate(int id)
        {
            InitializeComponent();

            Id = id;
            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            SetDefaults();

            Init();
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// идентификатор ячейки, с которой работает форма
        /// (primary key записи таблицы)
        /// </summary>
        private int Id { get; set; }

        /// <summary>
        /// ингициалищация компонентов формы
        /// </summary>
        public void SetDefaults()
        {
            Form = new FormHelper();
            Form.SetDefaults();
        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {
            //список колонок формы
            //Form.StatusControl = Status;

        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {

                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 1;

            var frameName = GetFrameName();
            Central.WM.Show(frameName, "Ворота " + Id, true, "add", this);

            //HistoryGrid.LoadItems();
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            var frameName = GetFrameName();
            Central.WM.Close(frameName);

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "wms",
                ReceiverName = "",
                SenderName = GetFrameName(),
                Action = "Closed",
            });


            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// формирует уникальный идентификатор фрейма
        /// </summary>
        /// <returns></returns>
        public string GetFrameName()
        {
            string result = "";

            result = $"{FrameName}_{Id}";

            return result;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OpenBarrier1Button_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<string, string> p = new Dictionary<string, string>();
            p.CheckAdd("ID", "1");
            p.CheckAdd("CMD", "1");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Devices");
            q.Request.SetParam("Object", "Barrier");
            q.Request.SetParam("Action", "Insert");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {

            }
        }
    }
}
