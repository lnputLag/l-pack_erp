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
    /// Логика взаимодействия для CuttingStampBind.xaml
    /// </summary>
    public partial class CuttingStampBind : ControlBase
    {
        public CuttingStampBind()
        {
            InitializeComponent();
            InitGrid();

            OnUnload = () =>
            {
                Grid.Destruct();
            };

        }

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
                    case "save":
                        Save();
                        break;
                    case "close":
                        Close();
                        break;
                }
            }
        }

        /// <summary>
        /// Список производственных площадок для выпадающего списка
        /// </summary>
        public Dictionary<string, string> FactoryItems { get; set; }
        /// <summary>
        /// ID техкарты, к которой привязывается штанцформа
        /// </summary>
        public int TechcardId { get; set; }
        /// <summary>
        /// Размеры изделия техкарты, к которой привязывается штанцформа
        /// </summary>
        public string TechcardSize { get; set; }
        /// <summary>
        /// Имя вкладки, которая вызвала открытие фрейма, и в которую возвращается фокус после закрытия фрейма
        /// </summary>
        public string ReceiverName { get; set; }

        public string TabName;

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        private void InitGrid()
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
                    Path="STAMP_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn()
                {
                    Header="Станок",
                    Path="STAMP_MACHINE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn()
                {
                    Header="Универсальная",
                    Path="HOLE_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=5,
                },
                new DataGridHelperColumn()
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn()
                {
                    Header="Площадка",
                    Path="FACTORY",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("ID");
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.AutoUpdateInterval = 0;
            Grid.SearchText = GridSearch;
            Grid.Toolbar = Toolbar;

            Grid.OnLoadItems = LoadGridItems;
            Grid.OnFilterItems = FilterGridItems;
            Grid.OnDblClick = selectedItem =>
            {
                Save();
            };

            Grid.Init();
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        private async void LoadGridItems()
        {
            string techcardSize = TechcardSize;
            bool allSize = (bool)AllSizeCheckBox.IsChecked;
            if (allSize)
            {
                techcardSize = "";
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "CuttingStamp");
            q.Request.SetParam("Action", "ListTkBind");
            q.Request.SetParam("TECHCARD_ID", TechcardId.ToString());
            q.Request.SetParam("TECHCARD_SIZE", techcardSize);

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
                    //Список станков
                    var machineDS = ListDataSet.Create(result, "MACHINE_LIST");
                    var machineItems = new Dictionary<string, string> { { "0", "Все" } };
                    foreach (var item in machineDS.Items)
                    {
                        machineItems.Add(item["ID"], item["NAME"]);
                    }
                    Machine.Items = machineItems;
                    Machine.SetSelectedItemByKey("0");
                    
                    var ds = ListDataSet.Create(result, "STAMP_LIST");
                    Grid.UpdateItems(ds);
                }
            }
        }

        /// <summary>
        /// Фмльтрация строк таблицы
        /// </summary>
        private void FilterGridItems()
        {
            if (Grid.Items != null)
            {
                if (Grid.Items.Count > 0)
                {
                    bool doFilteringByMachine = false;
                    int machineId = Machine.SelectedItem.Key.ToInt();
                    if (machineId > 0)
                    {
                        doFilteringByMachine = true;
                    }

                    if (doFilteringByMachine)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in Grid.Items)
                        {
                            bool includeByMachine = true;
                            if (doFilteringByMachine)
                            {
                                includeByMachine = false;
                                if (row.CheckGet("MACHINE_ID").ToInt() == machineId)
                                {
                                    includeByMachine = true;
                                }
                            }

                            if (includeByMachine)
                            {
                                items.Add(row);
                            }
                        }

                        Grid.Items = items;
                    }
                }
            }
        }

        /// <summary>
        /// Отображение вкладки со списком штанцформ
        /// </summary>
        /// <param name="values"></param>
        public void Show(Dictionary<string, string> values)
        {
            string skuCode = values.CheckGet("SKU_CODE");
            TechcardId = values.CheckGet("ID").ToInt();
            if (skuCode.IsNullOrEmpty())
            {
                skuCode = TechcardId.ToString();
            }
            else
            {
                skuCode = skuCode.Substring(0, 7);
            }

            string title = $"Привязка штанцформы к {skuCode}";
            TechcardSize = values.CheckGet("TK_SIZE");
            TabName = $"{ControlName}_{TechcardId}";

            LoadGridItems();

            Central.WM.Show(TabName, title, true, "add", this);
        }

        /// <summary>
        /// Закрытие формы
        /// </summary>
        public void Close()
        {
            Central.WM.Close(TabName);
            if (!ReceiverName.IsNullOrEmpty())
            {
                Central.WM.SetActive(ReceiverName);
                ReceiverName = "";
            }
        }

        /// <summary>
        /// Сохранение привязки
        /// </summary>
        public async void Save()
        {
            bool resume = true;
            int stampId = Grid.SelectedItem.CheckGet("ID").ToInt();

            if (stampId == 0)
            {
                resume = false;
                var dw = new DialogWindow("Не выбрана штанцформа", "Привязка штанцформы");
                dw.ShowDialog();
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Rig");
                q.Request.SetParam("Object", "CuttingStamp");
                q.Request.SetParam("Action", "SaveTkBinding");
                q.Request.SetParam("TECHCARD_ID", TechcardId.ToString());
                q.Request.SetParam("STAMP_ID", stampId.ToString());

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
                        if (result.ContainsKey("ITEM"))
                        {
                            //отправляем сообщение с данными полей окна
                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverGroup = "Preproduction/Rig",
                                ReceiverName = ReceiverName,
                                SenderName = ControlName,
                                Action = "RefreshStamp",
                            });
                            Close();
                        }
                    }
                }
                else if (q.Answer.Error.Code == 145)
                {
                    FormStatus.Text = q.Answer.Error.Message;
                }
            }
        }

        private void AllSizeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            LoadGridItems();
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

        private void Machine_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }
    }
}
