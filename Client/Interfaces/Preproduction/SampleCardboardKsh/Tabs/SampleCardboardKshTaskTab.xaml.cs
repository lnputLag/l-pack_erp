using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using static DevExpress.Data.Filtering.Helpers.SubExprHelper.ThreadHoppingFiltering;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Список производственных заданий на картон для образцов
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleCardboardKshTaskTab : ControlBase
    {
        public SampleCardboardKshTaskTab()
        {
            InitializeComponent();

            ControlTitle = "ПЗГА на заготовки";
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/samples_new/carton_samples";
            RoleName = "[erp]sample_cardboard_ksh";

            FactoryId = 2;
            SetDefaults();

            OnLoad = () =>
            {
                GridInit();
            };

            OnUnload = () =>
            {
                Grid.Destruct();
            };

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverName == ControlName)
                {
                    ProcessMessage(msg);
                }
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

        public int FactoryId;

        /// <summary>
        /// Обработка и выполнение команд
        /// </summary>
        /// <param name="command"></param>
        public void ProcessMessage(ItemMessage m)
        {
            string command = m.Action;
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "refresh":
                        Grid.LoadItems();
                        break;
                }
            }
        }

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        private void GridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Ид ПЗ",
                    Path = "ID_PZ",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header = "Дата",
                    Path = "DATA",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format="dd.MM.yyyy",
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header = "Номер ПЗ",
                    Path = "PZ_NUM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header = "Выполнено",
                    Path = "POSTING",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Boolean,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header = "В плане",
                    Path = "WORK",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Boolean,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество",
                    Path = "KOL",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header = "Время начала",
                    Path = "DTBEGIN",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format="dd.MM.yyyy HH:mm",
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header = "Время окончания",
                    Path = "DTEND",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format="dd.MM.yyyy HH:mm",
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header = "Образец",
                    Path = "NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=25,
                },
                new DataGridHelperColumn
                {
                    Header = "Номер картона",
                    Path = "NUM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header = "Картон",
                    Path = "NAME_CARTON",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=25,
                },
                new DataGridHelperColumn
                {
                    Header = "Сотрудник",
                    Path = "FIO1",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header = "В плане есть такой же картон",
                    Path = "IN_PLAN",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "В очереди",
                    Path = "PZ_LINE",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "Повторы",
                    Path = "SAME_QTY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "ИД прихода",
                    Path = "IDP",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden = true,
                },
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("ID_PZ");
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.Toolbar = GridToolbar;
            Grid.Commands = Commander;
            Grid.OnLoadItems = LoadItems;

            // Раскраска строк
            Grid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";
                        // Выполнены
                        if (row.CheckGet("POSTING").ToInt() == 1)
                        {
                            if (row.CheckGet("IDP").ToInt() > 0)
                            {
                                color=HColor.Green;
                            }
                            else
                            {
                                color=HColor.Yellow;
                            }
                        }
                        // В очереди на ГА
                        else if (row.CheckGet("PZ_LINE").ToInt() == 1)
                        {
                            color=HColor.Yellow;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }
                        return result;
                    }
                },
                {
                    DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";
                        // Подсвечиваем только невыполненные задания
                        if (row.CheckGet("POSTING").ToInt() == 0)
                        {
                            // Задание на образцы не в очереди, но в очереди ГА есть задания с таким же картоном
                            // Надо сообщить, чтобы задание на образец включили в план
                            if ((row.CheckGet("PZ_LINE").ToInt() == 0) && (row.CheckGet("IN_PLAN").ToInt() == 1))
                            {
                                color=HColor.BlueFG;
                            }
                            // Есть дубли задания с одинаковым картоном
                            if (row.CheckGet("SAME_QTY").ToInt() > 1)
                            {
                                color=HColor.MagentaFG;
                            }
                        }
                        else
                        {
                            // Неотсканированные завершенные задания
                            if (row.CheckGet("IDP").ToInt() == 0)
                            {
                                color=HColor.OliveFG;
                            }
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }
                        return result;
                    }
                }
            };

            Grid.Init();
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            FromDate.Text = DateTime.Now.AddDays(-3).ToString("dd.MM.yyyy");
            ToDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
        }

        public async void LoadItems()
        {
            GridToolbar.IsEnabled = false;
            Grid.ShowSplash();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleCardboards");
            q.Request.SetParam("Action", "TaskList");
            q.Request.SetParam("DATE_FROM", FromDate.Text);
            q.Request.SetParam("DATE_TO", ToDate.Text);
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
                    var ds = ListDataSet.Create(result, "TaskList");
                    Grid.UpdateItems(ds);
                }
                RefreshButton.Style = (Style)RefreshButton.TryFindResource("Button");
            }

            GridToolbar.IsEnabled = true;
            Grid.HideSplash();
        }

        private void DateTextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshButton.Style = (Style)RefreshButton.TryFindResource("FButtonPrimary");
        }
    }
}
