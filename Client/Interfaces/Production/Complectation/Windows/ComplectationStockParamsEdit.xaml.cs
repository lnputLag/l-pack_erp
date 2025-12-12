using System;
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
    /// Окно выбора заявки, по которой создаются новые поддоны про комплектации СГП
    /// </summary>
    public partial class ComplectationStockParamsEdit : UserControl
    {
        public ComplectationStockParamsEdit()
        {
            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitializeComponent();

            InitForm();
            SetDefaults();
        }

        /// <summary>
        /// Ид продукции (tovar.id2)
        /// </summary>
        public string ProductId { get; set; }

        public int FactoryId { get; set; }

        public Window Window { get; set; }

        /// <summary>
        /// Флаг того, что работа с этим окном успешно завершена
        /// </summary>
        public bool OkFlag { get; set; }

        /// <summary>
        /// Выбранная заявка
        /// </summary>
        public Dictionary<string, string> SelectedValue { get; private set; }

        /// <summary>
        /// Датасет с данными по заявкам на эту продукцию
        /// </summary>
        private ListDataSet ItemsRefDS { get; set; }

        public FormHelper FormHelper { get; set; }

        /// <summary>
        /// Список заявок, к которым привязаны списываемые поддоны
        /// </summary>
        public List<string> OrderList { get; set; }

        public void InitForm()
        {
            FormHelper = new FormHelper();

            var fields = new List<FormHelperField>
                {
                    new FormHelperField
                    {
                        Path = "ORDER",
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = OrderSelectBox,
                        ControlType = "SelectBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                };

            FormHelper.SetFields(fields);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m">Сообщение</param>
        private void ProcessMessages(ItemMessage m)
        {

        }

        /// <summary>
        /// закрытие окна
        /// </summary>
        public void Close()
        {
            Window?.Close();
        }

        /// <summary>
        /// Показывает окно
        /// </summary>
        public void Show()
        {
            LoadItems();

            // если заявок нет или она одна выбор очевиден
            if (ItemsRefDS.Items.Count == 0)
            {
                SelectedValue = null;
                OkFlag = true;
            }
            else if (ItemsRefDS.Items.Count == 1)
            {
                SelectedValue = ItemsRefDS.Items[0];
                OkFlag = true;
            }
            else
            {
                var title = $"Выберите заявку";

                Window = new Window
                {
                    Title = title,
                    Width = 450,
                    Height = 65 + 40,
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
                OrderSelectBox.Focus();

                // Предвыбираем значение для выпадающего списка заявок
                SetSelectedItemByOrderList();

                if (Window != null)
                {
                    Window.Topmost = true;
                    Window.ShowDialog();
                }
            }
        }

        public void SetSelectedItemByOrderList()
        {
            // Если среди выбранных поддонов есть те, которые привязаны к заявкам
            if (OrderList != null && OrderList.Count > 0)
            {
                // получаем список заявок, к которым привязаны поддоны, среди тех, что мы получили в запросе
                List<Dictionary<string, string>> orders = new List<Dictionary<string, string>>();
                foreach (var idorderdates in OrderList)
                {
                    Dictionary<string, string> order = ItemsRefDS.Items.FirstOrDefault(x => x.CheckGet("IDORDERDATES").ToInt() == idorderdates.ToInt());
                    orders.Add(order);
                }

                // Если в выпадающем списке есть заявки, которые соответсвтуют заявкам, к которым привязаны поддоны
                // То выбираем самую раннюю из них
                if (orders != null && orders.Count > 0)
                {
                    orders = orders.OrderBy(x => x.CheckGet("ORDER_DTTM")).ToList();
                    var selectBoxItem = OrderSelectBox.Items.FirstOrDefault(x => x.Key.ToInt() == orders.First().CheckGet("IDORDERDATES").ToInt());
                    OrderSelectBox.SetSelectedItem(selectBoxItem);
                }
                // Если в выпадающем списке нет заявок, которые соответствовали бы выбранным поддонам, 
                else
                {
                    //то выбираем самую раннюю заявку среди всех заявко по этой продукции
                    OrderSelectBox.SetSelectedItem(OrderSelectBox.Items.First());
                }
            }
            // Если ни один из выбранных поддонов не привязан к заявке, то выбираем самую раннюю заявку среди всех заявко по этой продукции
            else
            {
                OrderSelectBox.SetSelectedItem(OrderSelectBox.Items.First());
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            OkFlag = false;
            SelectedValue = new Dictionary<string, string>();
            OrderList = new List<string>();
        }

        /// <summary>
        /// Получаем список заявок на эту продукцию и заполняем выпадающий список
        /// </summary>
        private void LoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Complectation");
            q.Request.SetParam("Object", "Operation");
            q.Request.SetParam("Action", "ListApplication");
            q.Request.SetParam("PRODUCT_ID", ProductId);
            q.Request.SetParam("FACTORY_ID", $"{FactoryId}");
            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                if (result != null)
                {
                    // содержимое справочника для выпадающего списка
                    ItemsRefDS = ListDataSet.Create(result, "List");
                    OrderSelectBox.Items = ItemsRefDS.GetItemsList("IDORDERDATES", "ORDER_NAME");
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage
            {
                ReceiverGroup = "StockControl",
                ReceiverName = "",
                SenderName = "StockComplectationStockParamsEdit",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// Запоминаем текущую выбранную запись в выпадаюем списке и скрываем окно
        /// </summary>
        public void Save()
        {
            if (FormHelper != null)
            {
                if (FormHelper.Validate()) 
                {
                    if (OrderSelectBox.Items.Count > 0)
                    {
                        if (OrderSelectBox.SelectedItem.Key.ToInt() > 0)
                        {
                            Dictionary<string, string> dictionary = new Dictionary<string, string>();
                            dictionary = ItemsRefDS.Items.FirstOrDefault(x => x.CheckGet("IDORDERDATES").ToInt() == OrderSelectBox.SelectedItem.Key.ToInt());

                            if (dictionary.Count > 0)
                            {
                                SelectedValue = dictionary;
                            }

                            OkFlag = true;
                            Close();
                        }
                    }
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
