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

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Форма отображения похожих техкарт 
    /// Страница веб-техкарты.
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class WebTechnologicalMapDuplicate : ControlBase
    {
        public WebTechnologicalMapDuplicate()
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
                    Name = "ShowTk",
                    Enabled = true,
                    Title = "Показать ТК",
                    Description = "Показать Excel файл техкарты",
                    ButtonUse = true,
                    ButtonName = "ShowFileTkButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        var row = Grid.SelectedItem;
                        var path = row.CheckGet("PATHTK");

                        var folder_work = row.CheckGet("PATH_WORK");
                        var folder_archive = row.CheckGet("PATH_ARCHIVE");

                        if (File.Exists(Path.Combine(folder_work, path)))
                        {
                            Process.Start(new ProcessStartInfo(Path.Combine(folder_work, path)) { UseShellExecute = true });
                        }
                        else if (File.Exists(Path.Combine(folder_archive, path)))
                        {
                            Process.Start(new ProcessStartInfo(Path.Combine(folder_archive, path)) { UseShellExecute = true });
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;

                        var row = Grid.SelectedItem;
                        var path = row.CheckGet("PATHTK");

                        var folder_work = row.CheckGet("PATH_WORK");
                        var folder_archive = row.CheckGet("PATH_ARCHIVE");

                        if (File.Exists(Path.Combine(folder_work, path))
                            || File.Exists(Path.Combine(folder_archive, path)))
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
            }
            Commander.Init(this);
            OnLoad = () =>
            {
                GridInit();
            };
        }

        public int TkId { get; set; }

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
                    Path="ID_TK",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header="Покупатель",
                    Path="NAME_POK",
                    ColumnType=ColumnTypeRef.String,
                    Width2=26,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="ART",
                    ColumnType=ColumnTypeRef.String,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=32,
                },
                new DataGridHelperColumn
                {
                    Header="Вид изделия",
                    Path="NAME_PCLASS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Картон",
                    Path="CARDBOARD",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                },
                new DataGridHelperColumn
                {
                    Header="Упаковка",
                    Path="ID_OTGR",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header="Дата отгрузки",
                    Path="DTSHIP",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=8,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="Файл",
                    Path="PATHTK",
                    ColumnType=ColumnTypeRef.String,
                    Visible = false,
                },
                new DataGridHelperColumn
                {
                    Header="Путь к рабочей",
                    Path="PATH_WORK",
                    ColumnType=ColumnTypeRef.String,
                    Visible = false,
                },
                new DataGridHelperColumn
                {
                    Header="Путь к архивной",
                    Path="PATH_ARCHIVE",
                    ColumnType=ColumnTypeRef.String,
                    Visible = false,
                },
            };
            Grid.SetColumns(columns);

            Grid.SetPrimaryKey("ID_TK");
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
            q.Request.SetParam("Action", "ListDuplicate");
            q.Request.SetParam("ID_TK", TkId.ToString());

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

        public async void Save()
        {
            Close();
        }
        public void Close()
        {
            var frameName = GetFrameName();
            Central.WM.Close(frameName);
        }

    }
}
