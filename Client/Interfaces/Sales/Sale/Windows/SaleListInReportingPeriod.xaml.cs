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
    /// Список накладных в отчётном периоде
    /// </summary>
    public partial class SaleListInReportingPeriod : ControlBase
    {
        public SaleListInReportingPeriod()
        {
            ControlTitle = "Накладные в отчётном периоде";
            FrameName = "SaleListInReportingPeriod";
            InitializeComponent();

            OnLoad = () =>
            {
                Messenger.Default.Register<ItemMessage>(this, _ProcessMessages);
                Central.Msg.Register(ProcessMessages);

                Init();
                SetDefaults();
                GridInit();
            };

            OnUnload = () =>
            {
                Messenger.Default.Unregister<ItemMessage>(this);
                Central.Msg.UnRegister(ProcessMessages);
            };

            OnFocusGot = () =>
            {
                Grid.RunAutoUpdateTimer();
            };

            OnFocusLost = () =>
            {
                Grid.StopAutoUpdateTimer();
            };
        }

        /// <summary>
        /// Наименование таба, который вызвал этот таб
        /// </summary>
        public string ParentFrame { get; set; }

        public int FactoryId { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        private FormHelper Form { get; set; }

        /// <summary>
        /// Датасет с данными для грида продукции на остатках
        /// </summary>
        private ListDataSet GridDataSet { get; set; }

        /// <summary>
        /// Данные по позиции, которую хотим переместить в другую накладную
        /// </summary>
        private Dictionary<string, string> MovingPositionData = new Dictionary<string, string>();

        /// <summary>
        /// инициализация компонентов формы
        /// </summary>
        public void Init()
        {
            Form = new FormHelper();

            //список колонок формы
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
                new FormHelperField()
                {
                    Path = "INVOICE_DATE",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = InvoiceDate,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
            };
            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();
            Form.SetValueByPath("INVOICE_DATE", DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy"));
            GridDataSet = new ListDataSet();
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

            FrameName = $"{FrameName}";
            Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
            windowParametrs.Add("no_resize", "1");
            windowParametrs.Add("center_screen", "1");
            this.MinHeight = 600;
            this.MinWidth = 900;
            Central.WM.Show(FrameName, "Накладные в отчётном периоде", true, "main", this, "top", windowParametrs);
        }

        /// <summary>
        /// Инициализация грида продукции на остатках
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
                        Path="NSTHET",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата создания",
                        Path="DATA",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy",
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ СФ",
                        Path="NAME_SF",
                        ColumnType=ColumnTypeRef.String,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата СФ",
                        Path="DATASTH",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy",
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ Счёта",
                        Path="NAME_TOVCHEK",
                        ColumnType=ColumnTypeRef.String,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата счёта",
                        Path="DATAOPRSTH",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy",
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ ТН",
                        Path="NAME_PRIH",
                        ColumnType=ColumnTypeRef.String,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ ТТН",
                        Path="NAME_STH",
                        ColumnType=ColumnTypeRef.String,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Покупатель",
                        Path="BUYER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=28,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Продавец",
                        Path="SELLER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Договор",
                        Path="CONTRACT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид покупателя",
                        Path="BUYER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=1,
                        Hidden=true,
                        Visible=false,
                    },
                };
                Grid.SetColumns(columns);
                Grid.PrimaryKey = "NSTHET";
                Grid.AutoUpdateInterval = 0;
                Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                Grid.SearchText = SearchText;
                Grid.OnLoadItems = GridLoadItems;

                Grid.Init();
                Grid.Run();
            }
        }

        public void GridLoadItems()
        {
            DisableControls();

            var p = new Dictionary<string, string>();
            p.Add("INVOICE_DATE", Form.GetValueByPath("INVOICE_DATE"));
            p.Add("FACTORY_ID", $"{this.FactoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "Sale");
            q.Request.SetParam("Action", "ListInReportingPeriod");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    GridDataSet = ListDataSet.Create(result, "ITEMS");
                    Grid.UpdateItems(GridDataSet);
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();
        }

        public void Save()
        {
            if (Grid != null && Grid.SelectedItem != null && Grid.SelectedItem.Count > 0)
            {
                MovingPositionData.Add("NEW_INVOICE_ID", Grid.SelectedItem.CheckGet("NSTHET"));
                MovingPositionData.Add("NEW_CUSTOMER_ID", Grid.SelectedItem.CheckGet("BUYER_ID"));
                MovingPositionData.Add("NAME_SF", Grid.SelectedItem.CheckGet("NAME_SF"));
                MovingPositionData.Add("NAME_STH", Grid.SelectedItem.CheckGet("NAME_STH"));
                MovingPositionData.Add("BUYER_NAME", Grid.SelectedItem.CheckGet("BUYER_NAME"));

                // Отправляем сообщение вкладке "Позиции накладной расхода"
                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = "Preproduction",
                    ReceiverName = "ConsumptionList",
                    SenderName = "SaleListInReportingPeriod",
                    Action = "MoveToOtherDocument",
                    Message = "",
                    ContextObject = MovingPositionData,
                }
                );

                Close();
            }
        }

        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
            SaveButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
        }

        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
            SaveButton.IsEnabled = true;
            CancelButton.IsEnabled = true;
        }

        /// <summary>
        /// обработка сообщений
        /// </summary>
        /// <param name="message"></param>
        public void ProcessMessages(ItemMessage message)
        {
            if (message != null)
            {
                if (message.SenderName == "WindowManager")
                {
                    switch (message.Action)
                    {
                        case "FocusGot":
                            break;

                        case "FocusLost":
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void _ProcessMessages(ItemMessage m)
        {

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
                ReceiverGroup = "Sales",
                ReceiverName = "",
                SenderName = "SaleListInReportingPeriod",
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
            //FIXME Документация 
            Central.ShowHelp("/doc/l-pack-erp/sales/sale_list/");
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
