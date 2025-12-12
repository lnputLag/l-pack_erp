using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Интерфейс выбора причины списания
    /// </summary>
    public partial class ComplectationWriteOffReasonsEdit : UserControl
    {
        /// <summary>
        /// Конструктор интерфейса выбора причины списания
        /// </summary>
        public ComplectationWriteOffReasonsEdit()
        {
            InitializeComponent();

            InitForm();
            SetDefaults();
        }

        public FormHelper FormHelper { get; set; }
        public Window Window { get; set; }

        /// <summary>
        /// Причины списания в комплектации для переработки
        /// </summary>
        public int ConvertingFlag { get; set; }

        /// <summary>
        /// Причины списания в комплектации для СГП
        /// </summary>
        public int StockFlag { get; set; }

        /// <summary>
        /// Причины списания в комплектации для ГА
        /// </summary>
        public int CorrugatorFlag { get; set; }

        /// <summary>
        /// Причины списания в комплектации для ЛТ
        /// </summary>
        public int MoldedContainerFlag { get; set; }

        /// <summary>
        /// Датаест причин списания в комплектации
        /// </summary>
        private ListDataSet ReasonsRefDS { get; set; }

        /// <summary>
        /// Флаг того, что интерфейс отработал успешно
        /// </summary>
        public bool OkFlag { get; set; }

        /// <summary>
        /// Выбранная причина списания в комплектации
        /// </summary>
        public KeyValuePair<string, string> SelectedReason => ReasonsSelectBox.SelectedItem;

        /// <summary>
        /// Старая причина списания, которую собираемся изменить
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
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            OkFlag = false;
        }

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage
            {
                ReceiverGroup = "StockControl",
                ReceiverName = "",
                SenderName = "StockWriteoffReasonsEdit",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// Получаем список причин списания
        /// </summary>
        private async void LoadReasons()
        {
            var p = new Dictionary<string, string>();
            {
                p.Add("CONVERTING_FLAG", ConvertingFlag.ToString());
                p.Add("STOCK_FLAG", StockFlag.ToString());
                p.Add("CORRUGATOR_FLAG", CorrugatorFlag.ToString());
                p.Add("MOLDED_CONTAINER_FLAG", MoldedContainerFlag.ToString());   
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Complectation");
            q.Request.SetParam("Object", "Reason");
            q.Request.SetParam("Action", "ListWriteOffByPlace");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                if (result != null)
                {
                    // содержимое справочника  для выпадающего списка
                    ReasonsRefDS = ListDataSet.Create(result, "ITEMS");

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

            var title = $"Ввод причины списания в брак";

            Window = new Window
            {
                Title = title,
                Width = 550,
                Height = 70 + 40,
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
        /// Выбираем причину и закрываем окно
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (FormHelper.Validate())
            {
                if (!string.IsNullOrEmpty(ReasonsSelectBox.SelectedItem.Key))
                {
                    OkFlag = true;
                    Close();
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
