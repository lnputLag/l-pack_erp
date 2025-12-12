using Client.Common;
using Client.Interfaces.Main;
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

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Логика взаимодействия для CuttingStampItemUsage.xaml
    /// </summary>
    public partial class CuttingStampItemUsage : ControlBase
    {
        public CuttingStampItemUsage()
        {
            InitializeComponent();

            OnLoad = () =>
            {
                InitGrid();
            };

            OnUnload = () =>
            {
                Grid.Destruct();
            };

            OnFocusGot = () =>
            {
                Grid.ItemsAutoUpdate = true;
                Grid.Run();
            };

            OnFocusLost = () =>
            {
                Grid.ItemsAutoUpdate = false;
            };
        }
        /// <summary>
        /// Идентификатор полумуфты
        /// </summary>
        public int StampItemId;
        /// <summary>
        /// Имя вкладки, откуда вызвана форма и куда передается ответ
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// Обработка и выполнение команд
        /// </summary>
        /// <param name="command"></param>
        public void ProcessCommand(string command, ItemMessage m = null)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "close":
                        Close();
                        break;
                    case "toexcel":
                        Grid.ItemsExportExcel();
                        break;
                }
            }
        }

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
                    Width2=2,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="SKU_CODE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Номер ПЗ",
                    Path="TASK_NUMBER",
                    ColumnType=ColumnTypeRef.String,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="Линия",
                    Path="LINE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="Время окончания ПЗ",
                    Path="END_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=3,
                    Totals=(List<Dictionary<string,string>> rows) =>
                    {
                        int result=0;
                        if(rows != null)
                        {
                            foreach(Dictionary<string,string> row in rows)
                            {
                                result += row.CheckGet("QTY").ToInt();
                            }
                        }

                        return result;
                    },
                },
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("_ROWNUMBER");
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            Grid.AutoUpdateInterval = 0;
            Grid.OnLoadItems = LoadItems;
            Grid.Init();
        }

        private async void LoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "CuttingStamp");
            q.Request.SetParam("Action", "ListUsage");
            q.Request.SetParam("ID", StampItemId.ToString());

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
                    var ds = ListDataSet.Create(result, "STAMP_USAGE");
                    Grid.UpdateItems(ds);
                }
            }
        }

        /// <summary>
        /// Отображение вкладки
        /// </summary>
        public void ShowTab()
        {
            ControlName = $"CuttingStampItemUsage{StampItemId}";
            ControlTitle = $"Предыдущие задания {StampItemId}";

            Central.WM.Show(ControlName, ControlTitle, true, "add", this);
        }

        /// <summary>
        /// Закрытие вкладки с формой
        /// </summary>
        public void Close()
        {
            Central.WM.Close(ControlName);
            Central.WM.SetActive(ReceiverName);
        }

        /// <summary>
        /// Обработчик нажатия на кнопку
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
