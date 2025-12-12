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
    /// Список покупателей. Общий интерфейс
    /// </summary>
    public partial class BuyerList : UserControl
    {
        public BuyerList(string receiverGroup, string receiverName, string parentFrame)
        {
            FrameName = "BuyerList";
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            SetDefaults();
            BuyerGridInit();

            _ReceiverGroup = receiverGroup;
            _ReceiverName = receiverName;
            _ParentFrame = parentFrame;
        }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        private FormHelper Form { get; set; }

        /// <summary>
        /// Основной датасет с данными по покупателям
        /// </summary>
        private ListDataSet BuyerGridDataSet { get; set; }

        /// <summary>
        /// Группа получателей для отправки выбранного покупателя по шине сообщений
        /// </summary>
        private string _ReceiverGroup { get; set; }

        /// <summary>
        /// Имя объекта получателя для отправки выбранного покупателя по шине сообщений
        /// </summary>
        private string _ReceiverName { get; set; }

        /// <summary>
        /// Наименование таба, который вызвал этот таб
        /// </summary>
        private string _ParentFrame { get; set; }

        /// <summary>
        /// 1 -- Покупатель;
        /// 2 -- Грузополучатель.
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void BuyerGridInit()
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
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование контрагента",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование покупателя",
                        Path="BUYER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Адрес",
                        Path="BUYER_ADRES",
                        ColumnType=ColumnTypeRef.String,
                        Width2=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИНН",
                        Path="INN",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                    },
                };
                BuyerGrid.SetColumns(columns);

                BuyerGrid.SetPrimaryKey("ID");
                BuyerGrid.SearchText = SearchText;
                BuyerGrid.Toolbar = GridToolbar;
                BuyerGrid.AutoUpdateInterval = 0;

                BuyerGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

                //данные грида
                BuyerGrid.OnLoadItems = BuyerGridLoadItems;

                BuyerGrid.OnSelectItem = selectedItem =>
                {
                    if (SearchText != null)
                    {
                        SearchText.Focus();
                    }
                };

                BuyerGrid.Init();
            }

            if (SearchText != null)
            {
                SearchText.Focus();
            }
        }

        public void SetDefaults()
        {
            BuyerGridDataSet = new ListDataSet();
            Type = 1;

            if (Form != null)
            {
                Form.SetDefaults();
            }
        }

        /// <summary>
        /// Получение данных по покупателям
        /// </summary>
        public async void BuyerGridLoadItems()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "Sale");
            q.Request.SetParam("Action", "ListBuyer");
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
                    BuyerGridDataSet = ListDataSet.Create(result, "ITEMS");
                    BuyerGrid.UpdateItems(BuyerGridDataSet);
                }
            }
            else
            {
                q.ProcessError();
            }

            if (SearchText != null)
            {
                SearchText.Focus();
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

            if (SearchText != null)
            {
                SearchText.Focus();
            }

            Central.WM.Show(FrameName, $"Список потребителей", true, "add", this);
        }

        public void Save()
        {
            if (BuyerGrid != null && BuyerGrid.SelectedItem != null && BuyerGrid.SelectedItem.Count > 0)
            {
                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = _ReceiverGroup,
                    ReceiverName = _ReceiverName,
                    SenderName = "BuyerList",
                    Action = "SelectItem",
                    Message = Type.ToString(),
                    ContextObject = BuyerGrid.SelectedItem,
                });

                Central.WM.SetActive(_ParentFrame, true);
                Close();
            }
        }

        public void Refresh()
        {
            SetDefaults();
            BuyerGridLoadItems();
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.SetActive(_ParentFrame, true);
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
                SenderName = "BuyerList",
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
            Refresh();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {

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
