using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using static Client.Common.LPackClientRequest;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Заявки на предоставление дизайна и чертежей
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class TkFileRequestTab : ControlBase
    {
        public TkFileRequestTab()
        {
            InitializeComponent();

            RoleName = "[erp]tk_file_request";
            ControlTitle = "Заявки на предоставление файлов";
            DocumentationUrl = "/";
            TabName = "TkFileRequestTab";

            OnLoad = () =>
            {
                GridInit();
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

            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "Refresh",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить",
                        ButtonUse = true,
                        ButtonName = "RefreshButton",
                        MenuUse = false,
                        Action = () =>
                        {
                            Grid.LoadItems();
                        },
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "ShowFile",
                        Enabled = true,
                        Title = "Показать файл",
                        Description = "Показать файл",
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "ShowFileButton",
                        ButtonControl = ShowFileButton,
                        AccessLevel = Common.Role.AccessMode.ReadOnly,
                        Action = () =>
                        {
                            ShowFile();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (!row.CheckGet("FILE_NAME").IsNullOrEmpty() && row.CheckGet("STATUS").ToInt() == 1)
                            {
                                if (Central.Navigator.GetRoleLevel(this.RoleName) >= Role.AccessMode.FullAccess || Central.User.EmployeeId == row.CheckGet("MANAGER_EMPL_ID").ToInt())
                                {
                                    result = true;
                                }
                            }

                            return result;
                        },
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "PinFile",
                        Enabled = true,
                        Title = "Прикрепить файл",
                        Description = "Прикрепить файл",
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "ConfirmButton",
                        AccessLevel = Common.Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            PinFile();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("STATUS").ToInt() == 0)
                            {
                                result = true;
                            }

                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "UnpinFile",
                        Enabled = true,
                        Title = "Удалить файл",
                        Description = "Удалить файл",
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "UnpinFileButton",
                        AccessLevel = Common.Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            UnpinFile();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (!row.CheckGet("FILE_NAME").IsNullOrEmpty() && row.CheckGet("STATUS").ToInt() == 1)
                            {
                                result = true;
                            }

                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "Reject",
                        Enabled = true,
                        Title = "Отклонить",
                        Description = "Отклонить заявку на получение файла",
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "CancelButton",
                        AccessLevel = Common.Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            ChangeStatus(2);
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("STATUS").ToInt() == 0)
                            {
                                result = true;
                            }

                            return result;
                        }
                    });
                }
                Commander.Init(this);
            }
        }

        /// <summary>
        /// Название вкладки
        /// </summary>
        public string TabName;

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        private void GridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Ид заявки",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible = false,
                },
                new DataGridHelperColumn
                {
                    Header="Ид ТК",
                    Path="ID_TK",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header="Номер",
                    Path="NUM",
                    ColumnType=ColumnTypeRef.String,
                    Width2=29,
                },
                new DataGridHelperColumn
                {
                    Header="Дата поступления",
                    Path="CREATED_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=14,
                    Format="dd.MM.yyyy",
                },

                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Факт",
                    Path="STATUS_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="ИД инициатора",
                    Path="MANAGER_EMPL_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=11,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Тип запроса",
                    Path="TYPE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=25,
                },
                new DataGridHelperColumn
                {
                    Header="Комментарий",
                    Path="NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                },
                new DataGridHelperColumn
                {
                    Header="Направление",
                    Path="RCPT_TYPE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                },
                new DataGridHelperColumn
                {
                    Header="Ответственный",
                    Path="RESP_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="Инициатор",
                    Path="MANAGER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="ИД ответственного",
                    Path="RESP_EMPL_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=11,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Имя файла",
                    Path="FILE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                    Visible=false,
                },

            };
            Grid.SetColumns(columns);

            Grid.SetPrimaryKey("ID");
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.Commands = Commander;

            Grid.OnLoadItems = LoadItems;

            Grid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                // Цвета фона строк
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=System.Windows.DependencyProperty.UnsetValue;
                        var color = "";
                        int currentStatus = row.CheckGet("STATUS").ToInt();
                        switch (currentStatus)
                        {
                            // Готова
                            case 1:
                                color = HColor.Green;
                                break;
                            // Отклонена
                            case 2:
                                color = HColor.Red;
                                break;
                            default:
                                color = HColor.White;
                                break;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                }
            };

            Grid.UseProgressSplashAuto = false;
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
            q.Request.SetParam("Action", "ListTkFileRequest");

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
        }

        /// <summary>
        /// Привязка файла к заявке
        /// </summary>
        public async void PinFile()
        {
            bool resume = true;
            var fileName = "";

            var fd = new OpenFileDialog();
            fd.Filter = "Файл (*.*)|*.*";
            fd.FilterIndex = 0;
            fd.InitialDirectory = "\\\\file-server-4\\Техкарты";

            var fdResult = (bool)fd.ShowDialog();
            if (fdResult)
            {
                fileName = fd.FileName;
            }
            else
            {
                resume = false;
            }

            if (resume && !fileName.IsNullOrEmpty())
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "WebTechnologicalMap");
                q.Request.SetParam("Action", "SaveIntellectualProperty");
                q.Request.SetParam("ID", Grid.SelectedItem.CheckGet("ID").ToString());
                q.Request.SetParam("DEL_FLAG", "0");
                q.Request.Type = RequestTypeRef.MultipartForm;
                q.Request.UploadFilePath = fileName;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        if (!result.ContainsKey("ITEMS"))
                        {
                            var dw = new DialogWindow($"Файл успешно сохранен", "Сохранение файла", "", DialogWindowButtons.OK);
                            dw.ShowDialog();
                        }

                        ChangeStatus(1);
                        Grid.LoadItems();
                    }
                }
                else if (q.Answer.Error.Code == 145)
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Удаление файла из заявки
        /// </summary>
        public async void UnpinFile()
        {
            bool resume = true;
            var fileName = Grid.SelectedItem.CheckGet("FILE_NAME");
            if (!fileName.IsNullOrEmpty())
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "WebTechnologicalMap");
                q.Request.SetParam("Action", "SaveIntellectualProperty");
                q.Request.SetParam("ID", Grid.SelectedItem.CheckGet("ID").ToString());
                q.Request.SetParam("DEL_FLAG", "1");

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        if (result.ContainsKey("ITEMS"))
                        {
                            var dw = new DialogWindow($"Файл успешно удален", "Удаление файла", "", DialogWindowButtons.OK);
                            dw.ShowDialog();

                            ChangeStatus(0);
                        }
                    }
                }
                else if (q.Answer.Error.Code == 145)
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Открытие файла
        /// </summary>
        public async void ShowFile()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "GetIntellectualPropertyFile");
            q.Request.SetParam("ID", Grid.SelectedItem.CheckGet("ID").ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Central.OpenFile(q.Answer.DownloadFilePath);
            }
            else if (q.Answer.Error.Code == 145)
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Изменение статуса заявки
        /// </summary>
        public async void ChangeStatus(int status)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "UpdateStatusTkFileRequest");
            q.Request.SetParam("ID", Grid.SelectedItem.CheckGet("ID").ToString());
            q.Request.SetParam("STATUS", status.ToString());
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
                    if (ds.Items[0].CheckGet("SUCCESS").ToInt() == 1)
                    {
                        Grid.LoadItems();
                    }
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                q.ProcessError();
            }

        }
    }
}
