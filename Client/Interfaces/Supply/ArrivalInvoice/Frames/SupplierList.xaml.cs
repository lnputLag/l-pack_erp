using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace Client.Interfaces.Supply
{ 
    /// <summary>
    /// Логика взаимодействия для SupplierList.xaml
    /// </summary>
    public partial class SupplierList : UserControl
    {
        public SupplierList()
        {
            FrameName = "SupplierList";
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            SetDefaults();
            SupplierGridInit();
        }

        public delegate void OnSaveDelegate(Dictionary<string, string> selectedItem);
        public OnSaveDelegate OnSave;

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
        /// Основной датасет с данными по поставщикам
        /// </summary>
        private ListDataSet SupplierGridDataSet { get; set; }

        /// <summary>
        /// Наименование таба, который вызвал этот таб
        /// </summary>
        public string ParentFrame { get; set; }

        public void SetDefaults()
        {
            SupplierGridDataSet = new ListDataSet();

            if (Form != null)
            {
                Form.SetDefaults();
            }
        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void SupplierGridInit()
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
                        Header="Наименование поставщика",
                        Path="SUPPLIER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИНН",
                        Path="INN",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                    },
                };
                SupplierGrid.SetColumns(columns);

                SupplierGrid.SetPrimaryKey("ID");
                SupplierGrid.SearchText = SearchText;
                SupplierGrid.Toolbar = GridToolbar;
                SupplierGrid.AutoUpdateInterval = 0;

                SupplierGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

                //данные грида
                SupplierGrid.OnLoadItems = SupplierGridLoadItems;

                SupplierGrid.OnSelectItem = selectedItem =>
                {
                    if (SearchText != null)
                    {
                        SearchText.Focus();
                    }
                };

                SupplierGrid.Init();
            }

            if (SearchText != null)
            {
                SearchText.Focus();
            }
        }

        /// <summary>
        /// Получение данных по покупателям
        /// </summary>
        public async void SupplierGridLoadItems()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Supply");
            q.Request.SetParam("Object", "ArrivalInvoice");
            q.Request.SetParam("Action", "ListSupplier");
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
                    SupplierGridDataSet = ListDataSet.Create(result, "ITEMS");
                    SupplierGrid.UpdateItems(SupplierGridDataSet);
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

            Central.WM.Show(FrameName, $"Список поставщиков", true, "add", this);
        }

        public void Refresh()
        {
            SupplierGrid.LoadItems();
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.SetActive(ParentFrame, true);
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
                ReceiverGroup = "Supply",
                ReceiverName = "",
                SenderName = "SupplierList",
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

        public void Save()
        {
            if (SupplierGrid != null && SupplierGrid.SelectedItem != null && SupplierGrid.SelectedItem.Count > 0)
            {
                OnSave?.Invoke(SupplierGrid.SelectedItem);

                Close();
            }
        }

        private void ResreshButton_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }
    }
}
