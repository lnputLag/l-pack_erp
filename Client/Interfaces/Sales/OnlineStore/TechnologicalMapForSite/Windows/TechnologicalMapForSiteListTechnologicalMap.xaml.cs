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
    /// Список существующих тех карт, по которым можно создать позиции для выгрузки на сайт
    /// </summary>
    public partial class TechnologicalMapForSiteListTechnologicalMap : UserControl
    {
        public TechnologicalMapForSiteListTechnologicalMap()
        {
            FrameName = "TechnologicalMapForSiteListTechnologicalMap";
            OkFlag = false;

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();
            SetDefaults();
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
        /// Основной датасет с данными по тех картам
        /// </summary>
        public ListDataSet GridDataSet { get; set; }

        /// <summary>
        /// Выбранная в гриде запись
        /// </summary>
        public Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// Флаг того, что работа с интерфейсом успешно завершена
        /// </summary>
        public bool OkFlag { get; set; }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        MaxWidth=200,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Path="CODE",
                        ColumnType=ColumnTypeRef.String,
                        Width = 115,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Марка",
                        Path="BRAND",
                        ColumnType=ColumnTypeRef.String,
                        Width=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Профиль",
                        Path="PROFILE",
                        ColumnType=ColumnTypeRef.String,
                        Width=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Длина",
                        Path="LENGTH",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ширина",
                        Path="WIDTH",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Высота",
                        Path="HEIGTH",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Цвет",
                        Path="COLOR",
                        ColumnType=ColumnTypeRef.String,
                        Width=70,
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
                    UpdateActions(selectedItem);
                };

                //данные грида
                Grid.OnLoadItems = LoadItems;
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

        public async void LoadItems()
        {
            SetDefaults();
            DisableControls();

            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "TechnologicalMapForSite");
            q.Request.SetParam("Action", "ListTechnologicalMap");
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

        public void Save()
        {
            if (SelectedItem != null && SelectedItem.Count > 0)
            {
                OkFlag = true;
                Close();
            }
            else
            {
                // msg
            }
        }

        public void UpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;
        }

        public void UpdateButtons()
        {
            if (SelectedItem != null && SelectedItem.Count > 0)
            {
                SaveButton.IsEnabled = true;
            }
            else
            {
                SaveButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            if (Grid != null && Grid.Items != null)
            {
                Grid.Items.Clear();
            }

            if (Form != null)
            {
                Form.SetDefaults();
            }

            SelectedItem = new Dictionary<string, string>();
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

            var dt = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");

            FrameName = $"{FrameName}_new_{dt}";

            Central.WM.Show(FrameName, "Техкарты", true, "add", this);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {

        }

        public void DisableControls()
        {
            Grid.IsEnabled = false;
            HeaderToolBar.IsEnabled = false;
            GridToolbar.IsEnabled = false;
        }

        public void EnableControls()
        {
            Grid.IsEnabled = true;
            HeaderToolBar.IsEnabled = true;
            GridToolbar.IsEnabled = true;

            UpdateButtons();
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
                SenderName = "TechnologicalMapForSiteListTechnologicalMap",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            Grid.Destruct();
        }

        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            //FIXME: Нужно сделать документацию
            Central.ShowHelp("/doc/l-pack-erp-new/application/online_shop/online_shop_tk");
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void ResreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadItems();
        }
    }
}
