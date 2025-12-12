using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static Client.Interfaces.Main.DataGridHelperColumn;
using Client.Common;
using System;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperator
{
    /// <summary>
    /// Запланированные ремонты
    /// </summary>
    /// <author>vlasov_ea</author>   
    public partial class ScheduledRepairs : UserControl
    {
        public ScheduledRepairs()
        {
            InitializeComponent();

            Init();
        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {
            Id = 0;
            FrameName = "ScheduledRepairs";

            UnitLoadItems();
            GridScheduledRepairsInit();
            GridAllRepairsInit();
        }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }
        /// <summary>
        /// идентификатор записи, с которой работает форма
        /// (primary key записи таблицы)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Список узлов станка
        /// </summary>
        private List<string> UnitsList { get; set; }

        /// <summary>
        /// инициализация грида запланированных ремонтов
        /// </summary>
        public void GridScheduledRepairsInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                     new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=25,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Задания на ремонт",
                        Path="DESCRIPTION",
                        ColumnType=ColumnTypeRef.String,
                        Width=800,
                        MaxWidth=1000,
                    },
                };
                GridScheduledRepairs.SetColumns(columns);

                GridScheduledRepairs.UseRowHeader = false;
                GridScheduledRepairs.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                GridScheduledRepairs.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {
                        //NoteTextBlock.Text = selectedItem.CheckGet("DESCRIPTION");
                    }
                };

                //данные грида
                GridScheduledRepairs.OnLoadItems = GridScheduledRepairsLoadItems;

                GridScheduledRepairs.Run();

                //фокус ввода           
                GridScheduledRepairs.Focus();
            }
        }

        /// <summary>
        /// Загрузка запланированных ремонтов 
        /// </summary>
        public async void GridScheduledRepairsLoadItems()
        {
            GridScheduledRepairs.ShowSplash();
            bool resume = false;

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_ST", CorrugatorMachineOperator.CurrentMachineId.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatorRepairs");
            q.Request.SetParam("Action", "List");
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
                    var ds = ListDataSet.Create(result, "REPAIRS");
                    GridScheduledRepairs.UpdateItems(ds);
                }
            }

            GridScheduledRepairs.HideSplash();
        }

        /// <summary>
        /// инициализация грида всех ремонтов
        /// </summary>
        public void GridAllRepairsInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                     new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=25,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Задания на ремонт",
                        Path="DESCRIPTION",
                        ColumnType=ColumnTypeRef.String,
                        Width=800,
                        MaxWidth=1000,
                    },
                };
                GridAllRepairs.SetColumns(columns);

                GridAllRepairs.UseRowHeader = false;
                GridAllRepairs.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                GridAllRepairs.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {
                        //NoteTextBlock.Text = selectedItem.CheckGet("DESCRIPTION");
                    }
                };

                //данные грида
                GridAllRepairs.OnLoadItems = GridAllRepairsLoadItems;

                GridAllRepairs.Run();

                //фокус ввода           
                GridAllRepairs.Focus();
            }
        }

        /// <summary>
        /// Загрузка запланированных ремонтов 
        /// </summary>
        public async void GridAllRepairsLoadItems()
        {
            GridAllRepairs.ShowSplash();
            bool resume = false;

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_ST", CorrugatorMachineOperator.CurrentMachineId.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatorRepairs");
            q.Request.SetParam("Action", "ListAll");
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
                    var ds = ListDataSet.Create(result, "REPAIRS");
                    GridAllRepairs.UpdateItems(ds);
                }
            }

            GridAllRepairs.HideSplash();
        }

        /// <summary>
        /// Получение списка узлов станка
        /// </summary>
        public async void UnitLoadItems()
        {
            UnitsList = new List<string>();

            var ds = await Logbook.GetUnits();
            var items = ds?.Items;
            foreach (var item in items)
            {
                var unit = item?.CheckGet("NAME_UNIT");
                UnitsList.Add(unit);
            }
            Units.ItemsSource = UnitsList;

            if (UnitsList.Count > 0)
            {
                Units.SelectedIndex = 0;
            }
        }

        public void CreateRepair()
        {

            //RepairsGrid.LoadItems();
        }

        /// <summary>
        /// формирует уникальный идентификатор фрейма
        /// </summary>
        public string GetFrameName()
        {
            string result = "";
            result = $"{FrameName}_{Id}";
            return result;
        }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            Central.WM.FrameMode = 1;

            var frameName = GetFrameName();

            Central.WM.Show(frameName, "Запланированные ремонты", true, "add", this);
        }

        public void Close()
        {
            var frameName = GetFrameName();
            Central.WM.Close(frameName);
            Destroy();
        }

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "tape_counter",
                ReceiverName = "",
                SenderName = "ScheduledRepairs",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        private void CloseButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            CreateRepair();
        }
    }
}
