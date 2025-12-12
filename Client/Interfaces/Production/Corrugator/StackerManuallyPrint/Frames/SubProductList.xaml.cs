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

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Форма заказа перестила на выход гофроагрегата.
    /// Отображает список перестилов, которые можно заказать.
    /// </summary>
    /// <author>sviridov_ae</author>
    public partial class SubProductList : UserControl
    {
        /// <summary>
        /// Обязательные к заполнению переменные:
        /// MachineId.
        /// Не обязательные к заполнению переменные:
        /// ParentFrame.
        /// ParentContainer
        /// </summary>
        public SubProductList()
        {
            FrameName = "TovarSubList";

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            FormInit();
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
        /// Основной датасет с данными для грида перестилов
        /// </summary>
        public ListDataSet GridDataSet { get; set; }

        /// <summary>
        /// Выбранная запись в гриде
        /// </summary>
        public Dictionary<string, string> GridSelectedItem { get; set; }

        /// <summary>
        /// Наименование таба, который вызвал этот таб
        /// </summary>
        public string ParentFrame { get; set; }
        
        public string ParentContainer { get; set; }

        /// <summary>
        /// Идентификатор станка
        /// </summary>
        public int MachineId { get; set; }

        public void FormInit()
        {
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

        /// <summary>
        /// инициализация грида перестилов
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
                        Path="PRODUCT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=50,
                        MaxWidth=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=205,
                        MaxWidth=285,
                    },
                    new DataGridHelperColumn
                    {
                        Header="В буфере, шт",
                        Path="QUANTITY_IN_BUFFER",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=70,
                        MaxWidth=90,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Заказов на перемещение",
                        Path="COUNT_TASK_TO_MOVE",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=60,
                        MaxWidth=160,
                    },
                    new DataGridHelperColumn
                    {
                        Header="По умолчанию, шт",
                        Path="KOL_PAK",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=100,
                        MaxWidth=120,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Категория продукции",
                        Path="PRODUCT_CATEGORY_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=100,
                        MaxWidth=120,
                        Hidden=true,
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
                //Grid.OnLoadItems = GridLoadItems;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                Grid.OnSelectItem = selectedItem =>
                {
                    GridSelectedItem = selectedItem;
                };

                Grid.Init();
                Grid.Run();
            }
        }

        /// <summary>
        /// Получение данных по перестилам
        /// </summary>
        public async void GridLoadItems()
        {
            if (MachineId > 0)
            {
                DisableControls();

                var p = new Dictionary<string, string>();
                p.Add("CORRUGATOR_MACHINE_ID", MachineId.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ManuallyPrint");
                q.Request.SetParam("Action", "ListProductSub");
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
            else
            {
                var msg = $"Не найден станок для заказа перестила.";
                var d = new DialogWindow($"{msg}", "Заказ перестила", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
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

        /// <summary>
        /// Заказ парастила на станок (заявка на перемещение погрузчиком перестила на выбранный станок)
        /// </summary>
        public void OrderTovarSub()
        {
            if (GridSelectedItem != null && GridSelectedItem.Count > 0)
            {
                if (MachineId > 0)
                {
                    string corrugatorMachineName = "";
                    switch (MachineId)
                    {
                        case 2:
                            corrugatorMachineName = "БХС1";
                            break;

                        case 21:
                            corrugatorMachineName = "БХС2";
                            break;

                        case 22:
                            corrugatorMachineName = "ФОСБЕР";
                            break;

                        case 23:
                            corrugatorMachineName = "JS";
                            break;

                        default:
                            break;
                    }

                    var message = $"Заказать перестил {GridSelectedItem.CheckGet("NAME")} для станка {corrugatorMachineName}?";
                    if (DialogWindow.ShowDialog(message, "Заказ перестила", "", DialogWindowButtons.YesNo) == true)
                    {
                        DisableControls();

                        var p = new Dictionary<string, string>();
                        p.Add("CORRUGATOR_MACHINE_ID", MachineId.ToString());
                        p.Add("PRODUCT_ID", GridSelectedItem.CheckGet("PRODUCT_ID"));
                        p.Add("PRODUCT_CATEGORY_ID", GridSelectedItem.CheckGet("PRODUCT_CATEGORY_ID"));

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Production");
                        q.Request.SetParam("Object", "ManuallyPrint");
                        q.Request.SetParam("Action", "SaveTovarSubOrder");
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
                                    int productId = dataSet.Items.First().CheckGet("PRODUCT_ID").ToInt();
                                    if (productId > 0)
                                    {
                                        succesfullFlag = true;
                                    }
                                }
                            }

                            if (succesfullFlag)
                            {
                                var msg = $"Успешное создание заказа перестила.{Environment.NewLine}Перестил : {GridSelectedItem.CheckGet("NAME")}.";
                                var d = new DialogWindow($"{msg}", "Заказ перестила", "", DialogWindowButtons.OK);
                                d.ShowDialog();

                                GridLoadItems();
                            }
                            else
                            {
                                var msg = $"Ошибка создания заказа перестила.";
                                var d = new DialogWindow($"{msg}", "Заказ перестила", "", DialogWindowButtons.OK);
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
                else
                {
                    var msg = $"Не найден станок для заказа перестила.";
                    var d = new DialogWindow($"{msg}", "Заказ перестила", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                var msg = $"Не выбран перестил для заказа.";
                var d = new DialogWindow($"{msg}", "Заказ перестила", "", DialogWindowButtons.OK);
                d.ShowDialog();
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

            var dt = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");
            FrameName = $"{FrameName}_{dt}";
            
            if (Central.WM.TabItems.ContainsKey(ParentContainer))
            {
                Central.WM.Show(FrameName, $"Заказ перестила", true, ParentContainer, this);
            }
            else
            {
                Central.WM.Show(FrameName, $"Заказ перестила", true, "main", this);
            }

            GridLoadItems();
        }

        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            Grid.ShowSplash();
        }

        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            Grid.HideSplash();
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            if (!string.IsNullOrEmpty(ParentFrame))
            {
                Central.WM.SetActive(ParentFrame, true);
            }

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
                ReceiverGroup = "Production",
                ReceiverName = "",
                SenderName = "TovarSubList",
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
            Central.ShowHelp("/doc/l-pack-erp/");
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {

        }

        private void ResreshButton_Click(object sender, RoutedEventArgs e)
        {
            GridLoadItems();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void OrderTovarSubButton_Click(object sender, RoutedEventArgs e)
        {
            OrderTovarSub();
        }
    }
}
