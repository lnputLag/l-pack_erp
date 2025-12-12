using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Окно выбора или создания пакета передачи оснастки
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class RigTransferPackages : ControlBase
    {
        public RigTransferPackages()
        {
            InitializeComponent();
            TransferDS = new ListDataSet();
            TransferDS.Init();

            OnLoad = () =>
            {
                InitGrid();
                FormStatus.Text = "";
            };

            OnUnload = () =>
            {
                Grid.Destruct();
            };
        }

        /// <summary>
        /// Тип передаваемой оснастки: 1 - штанцформы, 2 - клише
        /// </summary>
        public int RigType;
        /// <summary>
        /// Идентификатор оснастки, готовой к передаче
        /// </summary>
        public int RigId;
        /// <summary>
        /// Идентификатор производственной площадки: 1 - Липецк, 2 - Кашира
        /// </summary>
        public int FactoryId;
        /// <summary>
        /// Статус передачи: 12, 13 - возврат клиенту, 17, 18 - на другую площадку
        /// </summary>
        public int Status;
        /// <summary>
        /// Имя вкладки, окуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;
        /// <summary>
        /// Данные для списка пакетов передачи
        /// </summary>
        private ListDataSet TransferDS { get; set; }

        /// <summary>
        /// Структура окна
        /// </summary>
        private Window Window { get; set; }

        /// <summary>
        /// Обработка команд
        /// </summary>
        /// <param name="command"></param>
        private void ProcessCommand(string command)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "insert":
                        int id = Grid.SelectedItem.CheckGet("ID").ToInt();
                        Save(id);
                        break;
                    case "create":
                        Save(0);
                        break;
                    case "close":
                        Close();
                        break;
                }
            }
        }

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        public void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn()
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn()
                {
                    Header="Наименование",
                    Path="OWNER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=25,
                },
                new DataGridHelperColumn()
                {
                    Header="Дата статуса",
                    Path="STATUS_DT",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=12,
                },
                new DataGridHelperColumn()
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=25,
                },
                new DataGridHelperColumn()
                {
                    Header="Статус",
                    Path="STATUS_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("ID");
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.AutoUpdateInterval = 0;
            Grid.OnLoadItems = LoadGridItems;
            Grid.OnDblClick = selectedItem =>
            {
                int id = selectedItem.CheckGet("ID").ToInt();
                Save(id);
            };

            Grid.Init();

        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        private void LoadGridItems()
        {
            if (TransferDS.Items != null)
            {
                Grid.UpdateItems(TransferDS);
            }
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        private async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "RigTransfer");
            q.Request.SetParam("Action", "ListPackage");
            q.Request.SetParam("RIG_TYPE", RigType.ToString());
            q.Request.SetParam("FACTORY_ID", FactoryId.ToString());

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
                    var ds = ListDataSet.Create(result, "TRANSFER");
                    if (ds.Items.Count > 0)
                    {
                        //В зависимости от назначения передачи оставляем только нужные передачи
                        foreach (var item in ds.Items)
                        {
                            int statusId = item.CheckGet("STATUS_ID").ToInt();
                            if (Status.ContainsIn(12, 13) && statusId.ContainsIn(12, 13))
                            {
                                TransferDS.Items.Add(item);
                            }
                            else if (Status.ContainsIn(17, 18) && statusId.ContainsIn(17, 18))
                            {
                                TransferDS.Items.Add(item);
                            }
                        }

                        Show();
                    }
                    else
                    {
                        Save(0);
                    }
                }
            }
        }

        public void Bind()
        {
            GetData();
        }

        /// <summary>
        /// Показ окна
        /// </summary>
        public void Show()
        {
            int w = (int)Width;
            int h = (int)Height;
            string title = $"Выбор пакета передачи";

            Window = new Window
            {
                Title = title,
                Width = w + 17,
                Height = h + 40,
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
        }

        /// <summary>
        /// Закрытие окна
        /// </summary>
        public void Close()
        {
            var window = this.Window;
            if (window != null)
            {
                window.Close();
            }
        }

        /// <summary>
        /// Сохранение пакета передачи
        /// </summary>
        /// <param name="id"></param>
        public async void Save(int id)
        {
            var d = new Dictionary<string, string>()
            {
                { "STATUS", Status.ToString() },
                { "RIG_TYPE", RigType.ToString() },
                { "ID", id.ToString() },
                { "RIG_ID", RigId.ToString() },
            };

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "RigTransfer");
            q.Request.SetParam("Action", "BindPackage");
            q.Request.SetParams(d);

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
                    var ds = ListDataSet.Create(result, "ITEM");
                    if (ds.Items.Count > 0)
                    {
                        var packageId = ds.Items[0].CheckGet("ID").ToInt().ToString();
                        d.CheckAdd("ID", packageId);
                    }
                    //отправляем сообщение с данными полей окна
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverGroup = "Preproduction/Rig",
                        ReceiverName = ReceiverName,
                        SenderName = ControlName,
                        Action = "TransferBinded",
                        ContextObject = d,
                    });
                    Close();
                }
            }
            else if (q.Answer.Error.Code == 145)
            {

            }
        }

        /// <summary>
        /// Обработка нажатия на кнопку
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            var b = (Button)sender;
            if (b != null)
            {
                var t = b.Tag.ToString();
                ProcessCommand(t);
            }
        }
    }
}
