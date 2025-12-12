using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Интерфейс выбора причины комплектации
    /// </summary>
    public partial class ComplectationReasonsEdit : UserControl
    {
        /// <summary>
        /// Конструктор интерфейса выбора причины комплектации
        /// </summary>
        public ComplectationReasonsEdit()
        {
            InitializeComponent();

            InitForm();
            SetDefaults();
        }

        public FormHelper FormHelper { get; set; }
        public Window Window { get; set; }

        private ListDataSet ReasonsRefDS { get; set; }

        /// <summary>
        /// Флаг того, что интерфейс отработал успешно
        /// </summary>
        public bool OkFlag { get; set; }

        public KeyValuePair<string, string> SelectedReason => ReasonsSelectBox.SelectedItem;
        public string ReasonMessage => ReasonMessageTextBox.Text;

        /// <summary>
        /// Причины комплектации для переработки
        /// </summary>
        public int ConvertingFlag { get; set; }

        /// <summary>
        /// Причины комплектации для СГП
        /// </summary>
        public int StockFlag { get; set; }

        /// <summary>
        /// Причины комплектации для ГА
        /// </summary>
        public int CorrugatorFlag { get; set; }

        /// <summary>
        /// Причины комплектации для ЛТ
        /// </summary>
        public int MoldedContainerFlag { get; set; }

        /// <summary>
        /// Старая причина комплектации, которую собираемся изменить
        /// </summary>
        public int OldReasonId { get; set; }

        /// <summary>
        /// Инициализация компонентов
        /// </summary>
        private void InitForm()
        {
            FormHelper = new FormHelper();
            //список колонок формы
            var fields = new List<FormHelperField>();
            FormHelper.SetFields(fields);

            ReasonMessageTextBox.Focus();
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            ReasonMessageTextBox.Text = "";
            OkFlag = false;

            ConvertingFlag = 0;
            StockFlag = 0;
            CorrugatorFlag = 0;
            MoldedContainerFlag = 0;
        }

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage
            {
                ReceiverGroup = "StockControl",
                ReceiverName = "",
                SenderName = "StockReasonsEdit",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// Загрузка данных по причинам комплектации
        /// </summary>
        private void LoadReasons()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Complectation");
            q.Request.SetParam("Object", "Reason");
            q.Request.SetParam("Action", "List");

            q.Request.SetParam("CONVERTING_FLAG", ConvertingFlag.ToString());
            q.Request.SetParam("STOCK_FLAG", StockFlag.ToString());
            q.Request.SetParam("CORRUGATOR_FLAG", CorrugatorFlag.ToString());
            q.Request.SetParam("MOLDED_CONTAINER_FLAG", MoldedContainerFlag.ToString());
            
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                if (result != null)
                {
                    // содержимое справочника  для выпадающего списка
                    ReasonsRefDS = ListDataSet.Create(result, "List");

                    ReasonsSelectBox.Items = ReasonsRefDS.GetItemsList("ID", "NAME");

                    if (OldReasonId > 0)
                    {
                        ReasonsSelectBox.SetSelectedItemByKey($"{OldReasonId}");
                    }
                }
            }
        }

        /// <summary>
        /// Показывает окно
        /// </summary>
        public void Show()
        {
            LoadReasons();

            var title = $"Ввод причины комплектации";

            Window = new Window
            {
                Title = title,
                Width = 550,
                Height = 100 + 40,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow,
                Content = new Frame
                {
                    Content = this,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                },
            };

            if (Window != null)
            {
                Window.Topmost = true;
                Window.ShowDialog();
            }
            ReasonsSelectBox.Focus();


        }

        /// <summary>
        /// Выбираем причину комплектации и закрываем окно
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (FormHelper.Validate())
            {
                if (!string.IsNullOrEmpty(ReasonsSelectBox.SelectedItem.Key))
                {
                    if (string.IsNullOrEmpty(ReasonMessageTextBox.Text) && ( ReasonsSelectBox.SelectedItem.Key == "100" || ReasonsSelectBox.SelectedItem.Key == "111"))
                    {
                        var msg = "Заполните поле Примечание.";
                        FormStatus.Text = msg;
                    }
                    else
                    {
                        FormStatus.Text = "";
                        OkFlag = true;
                        Close();
                    }
                }
                else
                {
                    var msg = "Не выбрана причина.";
                    FormStatus.Text = msg;
                }
            }
        }

        /// <summary>
        /// закрытие окна
        /// </summary>
        public void Close()
        {
            Window?.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
