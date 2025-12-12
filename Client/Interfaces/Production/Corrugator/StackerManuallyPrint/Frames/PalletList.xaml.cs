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

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Форма заказа пустых поддонов на выход гофроагрегата.
    /// Отображает список поддонов, которые можно заказать.
    /// </summary>
    /// <author>sviridov_ae</author>
    public partial class PalletList : UserControl
    {
        /// <summary>
        /// Обязательные к заполнению переменные:
        /// MachineId.
        /// Не обязательные к заполнению переменные:
        /// ParentFrame;
        /// PalletTypeIdList.
        /// ParentContainer
        /// </summary>
        public PalletList()
        {
            FrameName = "PalletList";
            PalletTypeIdList = new List<int>();

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

        public string ParentContainer { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Основной датасет с данными для грида поддонов
        /// </summary>
        public ListDataSet GridDataSet { get; set; }

        /// <summary>
        /// Выбранная запись в гриде
        /// </summary>
        public Dictionary<string, string> GridSelectedItem { get; set; }

        /// <summary>
        /// Наименование таба, который вызвал этот таб.
        /// Используется для возврата к родительскому табу при закрытии этого.
        /// </summary>
        public string ParentFrame { get; set; }

        /// <summary>
        /// Идентификатор станка
        /// </summary>
        public int MachineId { get; set; }

        /// <summary>
        /// Список Ид типа поддона (pallet.id_pal) для выбранного производственного задания.
        /// Нужен для подсветки типов поддонов по умолчанию в списке поддонов для заказа.
        /// </summary>
        public List<int> PalletTypeIdList { get; set; }

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
        /// инициализация грида поддонов
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
                        MinWidth=50,
                        MaxWidth=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Поддон",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,
                        MaxWidth=160,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Свободно, шт",
                        Path="FREE_PALLET_QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=95,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Место хранения",
                        Path="PALLET_LOCATION",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=75,
                        MaxWidth=107,
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

                Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    {
                        StylerTypeRef.BackgroundColor,
                        (Dictionary<string, string> row) =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            // зелёный -- поддон по умолчанию для выбранного производсвтенного задания
                            if (row.CheckGet("ID").ToInt() > 0)
                            {
                                if (PalletTypeIdList.Contains(row.CheckGet("ID").ToInt()))
                                {
                                    color = HColor.Green;
                                }
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };

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
        /// Получение данных по поддонам
        /// </summary>
        public async void GridLoadItems()
        {
            DisableControls();

            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "ManuallyPrint");
            q.Request.SetParam("Action", "ListPalletType");
            q.Request.SetParams(p);

            q.Request.Timeout = 30000;
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
                Central.WM.Show(FrameName, $"Список поддонов", true, ParentContainer, this);
            }
            else
            {
                Central.WM.Show(FrameName, $"Список поддонов", true, "main", this);
            }

            GridLoadItems();
        }

        /// <summary>
        /// Заказать поддон
        /// </summary>
        public void OrderPallet()
        {
            if (GridSelectedItem != null && GridSelectedItem.Count > 0 && MachineId > 0)
            {
                int palletQuantity = 0;
                var i = new ComplectationCMQuantity(1);
                i.Show("Количество поддонов");
                if (i.OkFlag)
                {
                    palletQuantity = i.QtyInt;
                }

                if (palletQuantity > 0)
                {
                    DisableControls();

                    var p = new Dictionary<string, string>();
                    p.Add("PALLET_ID", GridSelectedItem.CheckGet("ID"));
                    p.Add("PALLET_QUANTITY", palletQuantity.ToString());
                    p.Add("CORRUGATOR_MACHINE_ID", MachineId.ToString());

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Production");
                    q.Request.SetParam("Object", "ManuallyPrint");
                    q.Request.SetParam("Action", "SavePalletOrder");
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
                                int palletId = dataSet.Items.First().CheckGet("PALLET_ID").ToInt();
                                if (palletId > 0)
                                {
                                    succesfullFlag = true;
                                }
                            }
                        }

                        if (succesfullFlag)
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

                                default:
                                    break;
                            }

                            var msg = $"Успешное создание заказа поддонов.{Environment.NewLine}Поддон : {GridSelectedItem.CheckGet("NAME")}.{Environment.NewLine}Количество : {palletQuantity}.{Environment.NewLine}Станок : {corrugatorMachineName}.";
                            var d = new DialogWindow($"{msg}", "Заказ поддонов", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                        else
                        {
                            var msg = $"Ошибка создания заказа поддонов.";
                            var d = new DialogWindow($"{msg}", "Заказ поддонов", "", DialogWindowButtons.OK);
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
                var msg = $"Не выбран поддон для заказа.";
                var d = new DialogWindow($"{msg}", "Заказ поддонов", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
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
                SenderName = "PalletList",
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
            Central.ShowHelp("/doc/l-pack-erp-new/gofroproduction/label_printing/task_list/order_empty_pallet");
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

        private void OrderPalletButton_Click(object sender, RoutedEventArgs e)
        {
            OrderPallet();
        }
    }
}
