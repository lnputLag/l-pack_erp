using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Окно выбора заявки для позиции на стекере ГА
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class ProductionTaskSelectApplication : UserControl
    {
        public ProductionTaskSelectApplication()
        {
            InitializeComponent();
            SetDefaults();
            InitGrid();
        }

        /// <summary>
        /// Данные по активным заявкам для изделия на стекере
        /// </summary>
        public ListDataSet ApplicationDS { get; set; }

        /// <summary>
        /// Номер стекера, на котором выбирается заявка
        /// </summary>
        public int StackerId;
        /// <summary>
        /// ИД заявки, выбранной на стекере
        /// </summary>
        public int CurrentPositionId;
        /// <summary>
        /// Окно редактирования примечания
        /// </summary>
        public Window Window { get; set; }
        /// <summary>
        /// Название окна получателя сообщения
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            ApplicationDS = new ListDataSet();
            ApplicationDS.Init();
            StackerId = 0;
            CurrentPositionId = 0;
            SaveButton.IsEnabled = false;
        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "ProductionTask",
                ReceiverName = "",
                SenderName = "SelectApplication",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            if (!ReceiverName.IsNullOrEmpty())
            {
                Central.WM.SetActive(ReceiverName, true);
                ReceiverName = "";
            }
        }

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>()
            {
                new DataGridHelperColumn()
                {
                    Header="ИД заявки",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=60,
                },
                new DataGridHelperColumn()
                {
                    Header="Товар",
                    Path="GOODS_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=190,
                },
                new DataGridHelperColumn()
                {
                    Header="В заявке",
                    Doc="Количество изделий в заявке, шт",
                    Path="IN_APPLICATION_QUANTITY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=60,
                },
                new DataGridHelperColumn()
                {
                    Header="В ПЗ",
                    Doc="Количество заготовок в ПЗ, всего, шт",
                    Path="BLANK_QUANTITY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=60,
                },
                new DataGridHelperColumn()
                {
                    Header="Отгрузка",
                    Path="SHIPMENT_DATE",
                    Format="dd.MM HH:mm",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth=70,
                },
                new DataGridHelperColumn()
                {
                    Header="ПЗ",
                    Path="PRODUCTION_DATE",
                    Format="dd.MM HH:mm",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth=70,
                },
                new DataGridHelperColumn()
                {
                    Header="Заявка",
                    Path="NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Hidden=true,
                },
            };
            Grid.SetColumns(columns);
            Grid.AutoUpdateInterval = 0;
            Grid.Init();
        }

        public void Edit(Dictionary<string,string> p)
        {
            if (ApplicationDS.Items.Count > 0)
            {
                Grid.UpdateItems(ApplicationDS);
                SaveButton.IsEnabled = true;

                StackerId = p.CheckGet("STACKER_ID").ToInt();
                CurrentPositionId = p.CheckGet("POSITION_ID").ToInt();
                /*
                if (CurrentPositionId > 0)
                {
                    foreach(var row in Grid.Items)
                    {
                        if (row.CheckGet("ID").ToInt() == CurrentPositionId)
                        {
                            Grid.SetSelectedItemId(CurrentPositionId);
                        }
                    }
                }
                */
            }

            Show();
        }

        /// <summary>
        /// Показывает окно
        /// </summary>
        private void Show()
        {
            string title = $"Заявки на изделие";

            Window = new Window
            {
                Title = title,
                Width = this.Width + 24,
                Height = this.Height + 40,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow,
            };
            Window.Content = new Frame
            {
                Content = this,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            if (Window != null)
            {
                Window.Topmost = true;
                Window.ShowDialog();
            }
            Grid.Focus();

        }

        public void Close()
        {
            var window = this.Window;
            if (window != null)
            {
                window.Close();
            }
            Destroy();
        }

        public void Save()
        {
            if (Grid.SelectedItem != null)
            {
                var p = Grid.SelectedItem;
                p.Add("STACKER_ID", StackerId.ToString());

                //отправляем сообщение с выбранной заявкой
                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = "ProductionTask",
                    ReceiverName = ReceiverName,
                    SenderName = "SelectApplication",
                    Action = "SelectedApplication",
                    ContextObject = p,
                });
                Close();
            }
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
