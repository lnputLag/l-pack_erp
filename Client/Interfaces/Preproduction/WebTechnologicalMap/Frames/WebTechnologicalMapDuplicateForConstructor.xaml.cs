using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using System.IO;
using static Client.Interfaces.Main.DataGridHelperColumn;
using System.Windows.Threading;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Форма отображения похожих техкарт для конструктора
    /// Страница веб-техкарты.
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class WebTechnologicalMapDuplicateForConstructor : ControlBase
    {
        public WebTechnologicalMapDuplicateForConstructor()
        {

            InitializeComponent();
            FrameMode = 2;
            FrameName = "WebTechnologicalMapDuplicate";
            OnGetFrameTitle = () =>
            {
                return "Похожие техкарты";
            };
            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }

            };
            Commander.SetCurrentGroup("item");
            {

                Commander.Add(new CommandItem()
                {
                    Name = "ShowDrawing",
                    Enabled = true,
                    Title = "Чертеж",
                    Description = "Показать файл чертежа",
                    ButtonUse = true,
                    ButtonName = "ShowDrawingButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        var row = Grid.SelectedItem;
                        var path = row.CheckGet("DRAWING_FILE_PATH");

                        if (File.Exists(path))
                        {
                            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;

                        var row = Grid.SelectedItem;
                        var path = row.CheckGet("DRAWING_FILE_PATH");


                        if (File.Exists(path))
                        {
                            result = true;
                        }
                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "close",
                    Group = "main_form",
                    Enabled = true,
                    Title = "Закрыть",
                    Description = "Закрыть форму",
                    ButtonUse = true,
                    ButtonName = "CloseButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Close();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "refresh",
                    Group = "main_form",
                    Enabled = true,
                    Title = "Обновить",
                    Description = "Обновить",
                    ButtonUse = true,
                    ButtonName = "UpdateButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Grid.UpdateItems();
                    },
                });
            }
            Commander.Init(this);
            OnLoad = () =>
            {
                DeviationTextBox.Text = "5";
                GridInit();
            };
        }

        public int TkId { get; set; }
        public int L { get; set; }
        public int B { get; set; }
        public int H { get; set; }
        public int IdPclass { get; set; }


        /// <summary>
        /// Таймер заполнения поля фильтров
        /// </summary>
        public DispatcherTimer TemplateTimeoutTimer;

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        private void GridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="UNION_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header="Дата создания",
                    Path="CREATED_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=8,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="Тип",
                    Path="TYPE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=26,
                },
                new DataGridHelperColumn
                {
                    Header="Название",
                    Path="PRODUCT_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=32,
                },
                new DataGridHelperColumn
                {
                    Header="Развертка",
                    Path="PROJECTION",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="FEFCO",
                    Path="FEFCO",
                    ColumnType=ColumnTypeRef.String,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header="Файл чертежа",
                    Path="DRAWING_FILE_PATH",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                    Visible=false,
                },
            };
            Grid.SetColumns(columns);

            Grid.SetPrimaryKey("UNION_ID");
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.Toolbar = GridToolbar;
            Grid.Commands = Commander;
            Grid.OnLoadItems = LoadItems;
            Grid.Init();
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        public async void LoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "ListDuplicateDrawing");
            q.Request.SetParam("L", L.ToString());
            q.Request.SetParam("B", B.ToString());
            q.Request.SetParam("H", H.ToString());
            q.Request.SetParam("ID_PCLASS", IdPclass.ToString());
            q.Request.SetParam("DEV", DeviationTextBox.Text);

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    Grid.UpdateItems(ds);
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        public void Close()
        {
            var frameName = GetFrameName();
            Central.WM.Close(frameName);
        }

        private void DeviationTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            RunTemplateTimeoutTimer();
        }

        /// <summary>
        /// Запуск таймера заполнения фильтров
        /// </summary>
        private void RunTemplateTimeoutTimer()
        {
            if (TemplateTimeoutTimer == null)
            {
                TemplateTimeoutTimer = new DispatcherTimer
                {
                    Interval = new TimeSpan(0, 0, 2)
                };

                {
                    var row = new Dictionary<string, string>();
                    row.CheckAdd("TIMEOUT", "2000");
                    row.CheckAdd("DESCRIPTION", "");
                    Central.Stat.TimerAdd("TkOrderList_RunTemplateTimeoutTimer", row);
                }

                TemplateTimeoutTimer.Tick += (s, e) =>
                {
                    Grid.LoadItems();
                    StopTemplateTimeoutTimer();
                };
            }

            if (TemplateTimeoutTimer.IsEnabled)
            {
                TemplateTimeoutTimer.Stop();
            }
            TemplateTimeoutTimer.Start();
        }
        private void StopTemplateTimeoutTimer()
        {
            if (TemplateTimeoutTimer != null)
            {
                if (TemplateTimeoutTimer.IsEnabled)
                {
                    TemplateTimeoutTimer.Stop();
                }
            }
        }
    }
}
