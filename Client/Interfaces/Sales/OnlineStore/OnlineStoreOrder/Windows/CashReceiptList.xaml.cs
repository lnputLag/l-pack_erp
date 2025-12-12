using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Sales
{
    /// <summary>
    /// Логика взаимодействия для CashReceiptList.xaml
    /// </summary>
    public partial class CashReceiptList : UserControl
    {
        public CashReceiptList(int customerId, int orderId)
        {
            FrameName = "CashReceiptList";
            CustomerId = customerId;
            OrderId = orderId;

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            SetDefaults();
            GridInit();
        }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Основной датасет с данными по платёжным поручения
        /// </summary>
        public ListDataSet GridDataSet { get; set; }

        /// <summary>
        /// Выбранная запись в гриде
        /// </summary>
        public Dictionary<string, string> GridSelectedItem { get; set; }

        /// <summary>
        /// Иденификатор покупателя, по которому получаем список платёжных поручений
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Иденификатор заказа интернет магазина, к которому будем привязывать операцию(платёжное поручение) 
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Флаг успешной привязуки платёжного поручения к заказу
        /// </summary>
        public bool SuccesfullBindFlag { get; set; }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void GridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер",
                        Path="RECEIPT_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата",
                        Path="RECEIPT_DTTM",
                        ColumnType=ColumnTypeRef.String,
                        Width=62,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Сумма",
                        Path="RECEIPT_SUMM",
                        ColumnType=ColumnTypeRef.Double,
                        Width=65,
                    },
                    new DataGridHelperColumn
                    {
                        Header="По заявкам",
                        Path="SUM_PRICE_VAT_DELIVERY",
                        ColumnType=ColumnTypeRef.Double,
                        Width=65,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // К данному платёжному поручению привязан заказ интернет-магазина, но сумма в платёжном поручении больше, чем стоимоть заказа интернет-магазина
                                    if( !string.IsNullOrEmpty(row.CheckGet("IDO")) && row.CheckGet("SUM_PRICE_VAT_DELIVERY").ToDouble() < row.CheckGet("RECEIPT_SUMM").ToDouble() )
                                    {
                                        color = HColor.Blue;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Комментарий",
                        Path="RECEIPT_COMMENT",
                        ColumnType=ColumnTypeRef.String,
                        Width=300,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Договор",
                        Path="CONTRACT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width=110,
                    },

                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=2,
                        MaxWidth=2000,
                    },
                };
                Grid.SetColumns(columns);

                Grid.SearchText = SearchText;
                Grid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                Grid.OnSelectItem = selectedItem =>
                {
                    GridSelectedItem = selectedItem;
                };

                //данные грида
                Grid.OnLoadItems = GridLoadItems;
                Grid.Run();

                //инициализация формы
                {
                    Form = new FormHelper();

                    //колонки формы
                    var fields = new List<FormHelperField>()
                    {
                        new FormHelperField()
                        {
                            Path="SEARCH",
                            FieldType=FormHelperField.FieldTypeRef.String,
                            Control=SearchText,
                            ControlType="TextBox",
                            Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            }
                        },
                    };

                    Form.SetFields(fields);
                }
            }
        }

        public async void GridLoadItems()
        {
            DisableControls();

            var p = new Dictionary<string, string>();
            p.Add("CUSTOMER_ID", CustomerId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "OnlineStoreOrder");
            q.Request.SetParam("Action", "ListCashReceipt");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var dataSet = ListDataSet.Create(result, "ITEMS");
                    GridDataSet = dataSet;

                    Grid.UpdateItems(GridDataSet);
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();
        }

        public void SetDefaults()
        {
            GridDataSet = new ListDataSet();
            GridSelectedItem = new Dictionary<string, string>();

            if (Form != null)
            {
                Form.SetDefaults();
            }
        }

        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
        }

        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
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
            Central.WM.FrameMode = 2;

            var dt = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");
            FrameName = $"{FrameName}_{dt}";

            Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
            windowParametrs.Add("no_resize", "1");
            windowParametrs.Add("center_screen", "1");
            Central.WM.Show(FrameName, $"Список платёжных поручений", true, "add", this, "top", windowParametrs);
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.Close(FrameName);

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
                ReceiverGroup = "Sales",
                ReceiverName = "",
                SenderName = "CashReceiptList",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }


        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            //FIXME: Нужно сделать документацию
            Central.ShowHelp("/doc/l-pack-erp-new/application/online_shop");
        }

        public void Save()
        {
            if (GridSelectedItem != null && GridSelectedItem.Count > 0)
            {
                if (GridSelectedItem.CheckGet("CONTRACT_TYPE").ToInt() != 5)
                {
                    var message = $"Внимание!{Environment.NewLine}Выбранный платёж не привязан к договору интернет-магазина!{Environment.NewLine}" +
                        $"Вы уверены, что хотите привязать этот платёж к заказу интернет-магазина?";

                    if (DialogWindow.ShowDialog(message, "Заказы интернет-магазина", "", DialogWindowButtons.NoYes) != true)
                    {
                        return;
                    }
                }

                DisableControls();

                var p = new Dictionary<string, string>();
                p.Add("OPERATION_ID", GridSelectedItem.CheckGet("ID"));
                p.Add("ORDER_ID", OrderId.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "OnlineStoreOrder");
                q.Request.SetParam("Action", "BindCashReceipt");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    bool succesfullFlag = false;

                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var dataSet = ListDataSet.Create(result, "ITEMS");
                        if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                        {
                            int orderId = dataSet.Items.First().CheckGet("ORDER_ID").ToInt();

                            if (orderId > 0)
                            {
                                succesfullFlag = true;
                            }
                        }
                    }

                    if (succesfullFlag)
                    {
                        string msg = $"Успешная привязка операции платёжного поручения к заказу интернет магазина.";
                        var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                        d.ShowDialog();

                        SuccesfullBindFlag = true;
                        Close();
                    }
                    else
                    {
                        string msg = $"Ошибка привязки операции платёжного поручения к заказу интернет магазина.";
                        var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }

                EnableControls();
            }
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {

        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }
    }
}
