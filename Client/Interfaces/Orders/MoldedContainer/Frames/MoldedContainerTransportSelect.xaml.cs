using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Orders.MoldedContainer
{
    /// <summary>
    /// Форма выбора водителя отгрузки
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class MoldedContainerTransportSelect : ControlBase
    {
        public MoldedContainerTransportSelect()
        {
            InitializeComponent();
            InitGrid();

            OnUnload = () =>
            {
                Grid.Destruct();
            };

            Commander.SetCurrentGroup("main");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "save",
                    Group = "main_form",
                    Enabled = true,
                    Title = "Выбрать",
                    Description = "Выбор водителя с транспортом для привязки к отгрузке",
                    ButtonUse = true,
                    ButtonName = "SaveButton",
                    MenuUse = false,
                    HotKey = "Return|DoubleCLick",
                    Action = () =>
                    {
                        Save();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "close",
                    Group = "main_form",
                    Enabled = true,
                    Title = "Отмена",
                    Description = "Закрыть форму без сохранения",
                    ButtonUse = true,
                    ButtonName = "CancelButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Close();
                    },
                });
            }
            Commander.Init(this);
        }
        /// <summary>
        /// Имя вкладки, окуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;

        private int SelectedDriver;

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Водитель",
                    Path="DRIVERNAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=50
                },
                new DataGridHelperColumn
                {
                    Header="Марка",
                    Path="CARMARK",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=15
                },
                new DataGridHelperColumn
                {
                    Header="Номер",
                    Path="CARNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=30
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=10,
                },
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("ID");
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            Grid.SearchText = SearchText;
            Grid.Toolbar = Toolbar;
            Grid.Commands = Commander;

            Grid.OnLoadItems = LoadItems;
            Grid.AutoUpdateInterval = 0;
            Grid.OnSelectItem = (selectItem) =>
            {
                FormStatus.Text = "";
            };
            Grid.Init();
        }

        /// <summary>
        /// Загрузка содержимого таблицы
        /// </summary>
        private async void LoadItems()
        {
            //Grid.ShowSplash();
            DisableControls();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Orders");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "ListDriver");

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
                    var ds = ListDataSet.Create(result, "DRIVERS");
                    Grid.UpdateItems(ds);

                    if (SelectedDriver > 0)
                    {
                        Grid.SelectRowByKey(SelectedDriver.ToString());
                    }
                }
            }

            //Grid.HideSplash();
            EnableControls();
        }

        /// <summary>
        /// Сохранение выбора изделия
        /// </summary>
        public void Save()
        {
            if (Grid.Items != null)
            {
                if (Grid.SelectedItem != null)
                {
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverGroup = "Orders",
                        ReceiverName = ReceiverName,
                        SenderName = ControlName,
                        Action = "TransportSelect",
                        ContextObject = Grid.SelectedItem,
                    });
                    Close();
                }
                else
                {
                    FormStatus.Text = "Выберите водителя в таблице";
                }
            }
            else
            {
                FormStatus.Text = "Нет водителей для выбора";
            }
        }

        /// <summary>
        /// Отображение вкладки
        /// </summary>
        public void Show(int selectedId=0)
        {
            SelectedDriver = selectedId;
            string title = "Выбор водителя";
            Central.WM.Show(ControlName, title, true, "add", this);
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        public void Close()
        {
            Central.WM.Close(ControlName);
            if (!ReceiverName.IsNullOrEmpty())
            {
                Central.WM.SetActive(ReceiverName);
                ReceiverName = "";
            }
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            Grid.Toolbar.IsEnabled = true;
            Grid.HideSplash();
            Grid.IsEnabled = true;
            SplashControl.Visible = false;
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            Grid.Toolbar.IsEnabled = false;
            Grid.ShowSplash();
            Grid.IsEnabled = false;
            SplashControl.Visible = true;
        }
    }
}
