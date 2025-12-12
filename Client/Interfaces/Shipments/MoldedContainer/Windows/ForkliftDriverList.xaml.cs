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

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// 
    /// </summary>
    public partial class ForkliftDriverList : ControlBase
    {
        public ForkliftDriverList()
        {
            ControlTitle = "Выбор погрузчика";
            FrameName = "ForkliftDriverList";
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
            };

            OnFocusLost = () =>
            {
            };
        }

        /// <summary>
        /// Наименование таба, который вызвал этот таб
        /// </summary>
        public string ParentFrame { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Датасет с данными для грида продукции на остатках
        /// </summary>
        public ListDataSet GridDataSet { get; set; }

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
                    Control=DriverSearchBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
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
            GridDataSet = new ListDataSet();
        }

        /// <summary>
        /// Инициализация грида
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
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Водитель",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=30,
                    },
                };
                DriverGrid.SetColumns(columns);
                DriverGrid.SetPrimaryKey("ID");
                DriverGrid.AutoUpdateInterval = 0;
                DriverGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                DriverGrid.SearchText = DriverSearchBox;
                DriverGrid.OnLoadItems = GridLoadItems;
                
                DriverGrid.Init();
            }
        }

        public void GridLoadItems()
        {
            DisableControls();

            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "ListForkliftDriver");
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
                    DriverGrid.UpdateItems(GridDataSet);
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();
        }

        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
            DriverGridToolBar.IsEnabled = false;
        }

        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
            DriverGridToolBar.IsEnabled = true;
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

        public void Save()
        {
            if (DriverGrid.SelectedItem != null && DriverGrid.SelectedItem.Count > 0)
            {
                // Отправляем сообщение
                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverName = ParentFrame,
                    SenderName = "ForkliftDriverList",
                    Action = "ChoiceForkliftDriver",
                    Message = DriverGrid.SelectedItem.CheckGet("ID"),
                    ContextObject = new KeyValuePair<string, string>(DriverGrid.SelectedItem.CheckGet("ID"), DriverGrid.SelectedItem.CheckGet("NAME")),
                }
                );

                Close();
            }
            else
            {
                string msg = "Не выбран погрузчик";
                var d = new DialogWindow($"{msg}", "Выбор погрузчика", "", DialogWindowButtons.OK);
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
            Central.WM.FrameMode = 2;

            FrameName = $"{FrameName}";
            Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
            windowParametrs.Add("no_resize", "1");
            windowParametrs.Add("center_screen", "1");
            this.MinHeight = 350;
            this.MinWidth = 300;
            Central.WM.Show(FrameName, "Выбор погрузчика", true, "main", this, "top", windowParametrs);
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
                ReceiverGroup = "Shipment",
                ReceiverName = "",
                SenderName = "ForkliftDriverList",
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
            Central.ShowHelp("/doc/l-pack-erp/");
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
