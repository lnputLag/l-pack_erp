using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
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

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Логика взаимодействия для RawMaterialGroupListOrder.xaml
    /// </summary>
    public partial class RawMaterialGroupListOrder : UserControl
    {
        public RawMaterialGroupListOrder(string parentFrame)
        {
            FrameName = "RawMaterialGroupListOrder";

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            GridInit();
            SetDefaults();

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
        public FormHelper Form { get; set; }

        /// <summary>
        /// Выбранная запись в гриде
        /// </summary>
        public Dictionary<string, string> GridSelectedItem { get; set; }

        /// <summary>
        /// Наименование таба, который вызвал этот таб
        /// </summary>
        private string _ParentFrame { get; set; }

        /// <summary>
        /// Датасет с данными для заполнения грида
        /// </summary>
        public ListDataSet GridDataSet { get; set; }

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
                        Header="Заявка",
                        Path="ORDER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Внешний номер заявки",
                        Path="ORDER_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=67,
                        MaxWidth=68,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Покупатель",
                        Path="BUYER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=115,
                        MaxWidth=210,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Изделие",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=115,
                        MaxWidth=320,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Path="PRODUCT_CODE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=115,
                        MaxWidth=115,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Штук по заявке",
                        Path="QUANTITY_IN_ORDER",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=48,
                        MaxWidth=65,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Картон",
                        Path="CARDBOARD_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=85,
                        MaxWidth=135,
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

                Grid.SetSorting("ORDER_ID", System.ComponentModel.ListSortDirection.Descending);
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
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {

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
            HeaderLabelBorder.Visibility = Visibility.Collapsed;

            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 1;
            var dt = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");
            FrameName = $"{FrameName}_{dt}";
            Central.WM.Show(FrameName, $"Список заявок", true, "add", this);

            SetData();
        }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show(string labelText)
        {
            HeaderLabel.Content = labelText;

            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 1;
            var dt = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");
            FrameName = $"{FrameName}_{dt}";
            Central.WM.Show(FrameName, $"Список заявок", true, "add", this);

            SetData();
        }

        /// <summary>
        /// Заполнение грида данными
        /// </summary>
        public void SetData()
        {
            Grid.UpdateItems(GridDataSet);
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
                ReceiverGroup = "Preproduction",
                ReceiverName = "",
                SenderName = "RawMaterialGroupListOrder",
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

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
