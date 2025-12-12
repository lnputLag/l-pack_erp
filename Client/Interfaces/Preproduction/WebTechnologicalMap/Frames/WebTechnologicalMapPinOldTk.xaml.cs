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
using Client.Assets.HighLighters;
using System.Windows;
using Excel = Microsoft.Office.Interop.Excel;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using NPOI.SS.Util;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Форма привязывания старой техкарты 
    /// Страница веб-техкарты.
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class WebTechnologicalMapPinOldTk : ControlBase
    {
        public WebTechnologicalMapPinOldTk()
        {

            InitializeComponent();
            FrameMode = 2;
            FrameName = "WebTechnologicalMapPinOldTk";
            OnGetFrameTitle = () =>
            {
                return "Привязать техкарту";
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
                    Name = "save",
                    Group = "main",
                    Enabled = true,
                    Title = "Сохранить",
                    Description = "Привзать выбранную техкарту",
                    ButtonUse = true,
                    ButtonName = "SaveButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Save();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        var path = row.CheckGet("PATHTK");

                        var folder_work = row.CheckGet("PATH_WORK");
                        var folder_archive = row.CheckGet("PATH_ARCHIVE");

                        if (row.CheckGet("ID_TK").ToInt() > 0)
                        {
                            if (File.Exists(Path.Combine(folder_work, path)))
                            {
                                result = true;
                            }
                            else if(File.Exists(Path.Combine(folder_archive, path)))
                            {
                                result = true;
                            }
                        }
                        return result;
                    }
                });
                            
                Commander.Add(new CommandItem()
                {
                    Name = "show_tk",
                    Group = "main",
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
                    Group = "main",
                    Enabled = true,
                    Title = "Закрыть",
                    Description = "Закрыть форму",
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
            OnLoad = () =>
            {
                GridInit();
            };
        }

        public int TkId { get; set; }
        public string PathTk { get; set; }

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
                    Path="CUSTOMER_SHORT",
                    ColumnType=ColumnTypeRef.String,
                    Width2=26,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="ART",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="Размер",
                    Path="SIZE_PRODUCT",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Статус клише",
                    Path="STATUS",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Статус клише",
                    Path="STATUS_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("STATUS").ToInt()==0)
                                {
                                    color = HColor.Green;
                                }
                                else if(row.CheckGet("STATUS").ToInt().ContainsIn(1,10))
                                {
                                    color = HColor.Red;
                                }
                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        },
                        {
                            StylerTypeRef.ForegroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = HColor.BlackFG;

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Имя файла",
                    Path="PATHTK",
                    ColumnType=ColumnTypeRef.String,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Путь к рабочим",
                    Path="PATH_WORK",
                    ColumnType=ColumnTypeRef.String,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Путь к архиву",
                    Path="PATH_ARCHIVE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="В архиве",
                    Path="ARCHIVE",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=7,
                    Visible=false,
                },
            };
            Grid.SetColumns(columns);

            Grid.SetPrimaryKey("ID_TK");
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.Toolbar = GridToolbar;
            Grid.Commands = Commander;
            Grid.SearchText = GridSearch;
            Grid.OnLoadItems = LoadItems;

            Grid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                {
                    DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=System.Windows.DependencyProperty.UnsetValue;
                        var color = "";
                        if (row.CheckGet("ARCHIVE").ToInt() == 1)
                        {
                            color = HColor.Olive;
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
        /// Загрузка данных в таблицу
        /// </summary>
        public async void LoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "ListForPinOldTk");
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
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "UpdateTkOld");
            q.Request.SetParam("ID_TK", TkId.ToString());
            q.Request.SetParam("ID_TK_OLD", Grid.SelectedItem.CheckGet("ID_TK").ToString());
            q.Request.SetParam("NOTE", NoteTextBox.Text);

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    Excel.Application excelApp = null;
                    Excel.Workbook workbook = null;
                    Excel.Worksheet worksheet = null;
                    try
                    {
                        excelApp = new Excel.Application();
                        workbook = excelApp.Workbooks.Open(PathTk);
                        worksheet = workbook.Worksheets[1];

                        worksheet.Range["A36"].Value2 = Grid.SelectedItem.CheckGet("ART").ToString() + " " + NoteTextBox.Text;
                        workbook.Save();
                        workbook.Close(true);
                        var dw = new DialogWindow("Старая техкарта успешно привязана", "Привязка старой техкарты", "", DialogWindowButtons.OK);
                        dw.ShowDialog();
                    }
                    catch(Exception e)
                    {
                        var dw = new DialogWindow($"Ошибка обновления файла: {e.Message}", "Привязка старой техкарты", "", DialogWindowButtons.OK);
                        dw.ShowDialog();
                    }
                    finally
                    {
                        if (worksheet != null)
                            System.Runtime.InteropServices.Marshal.ReleaseComObject(worksheet);

                        if (workbook != null)
                            System.Runtime.InteropServices.Marshal.ReleaseComObject(workbook);

                        if (excelApp != null)
                        {
                            excelApp.Quit();
                            System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
                        }

                    }
                }
            }
            else
            {
                q.ProcessError();
            }
            Close();
        }
        public void Close()
        {
            var frameName = GetFrameName();
            Central.WM.Close(frameName);
        }

    }
}
