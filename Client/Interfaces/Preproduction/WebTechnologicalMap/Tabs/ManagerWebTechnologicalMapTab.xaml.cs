using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Service.Printing;
using DevExpress.Data.Filtering.Helpers;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;
using Excel = Microsoft.Office.Interop.Excel;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Excel;
using Application = Microsoft.Office.Interop.Excel.Application;
using Style = System.Windows.Style;
using Action = System.Action;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс веб-техкарт (Менеджеры)
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class ManagerWebTechnologicalMapTab : ControlBase
    {
        public ManagerWebTechnologicalMapTab()
        {
            InitializeComponent();

            RoleName = "[erp]manager_web_tech_map";
            ControlTitle = "Веб-техкарты (менеджеры)";
            DocumentationUrl = "/";
            TabName = "ManagerWebTechMap";


            OnLoad = () =>
            {
                FormInit(); 
                SetDefaults();
                LoadManagers();
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
                    switch (msg.Action)
                    {
                        case "AddOrUpdateOrder":
                            Grid.LoadItems();
                            if (!msg.Message.IsNullOrEmpty())
                            {
                                if (msg.Message.ToInt() > 0)
                                {
                                    IdTk = msg.Message;
                                }
                            }
                            break;
                        case "Refresh":
                            Grid.LoadItems();
                            break;
                    }
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


            {
                Commander.SetCurrentGridName("Grid");
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "Show",
                        Group = "main",
                        Enabled = true,
                        Title = "Показать",
                        Description = "Показать",
                        ButtonUse = true,
                        ButtonName = "ShowButton",
                        MenuUse = false,
                        AccessLevel = Role.AccessMode.ReadOnly,
                        Action = () =>
                        {
                            Grid.LoadItems();
                            ShowButton.Style = (Style)ShowButton.TryFindResource("Button");
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "Print",
                        Group = "main",
                        Enabled = true,
                        Title = "Печать",
                        Description = "Распечатать информацию по техкарте",
                        ButtonUse = true,
                        ButtonName = "PrintButton",
                        MenuUse = false,
                        AccessLevel = Role.AccessMode.ReadOnly,
                        Action = () =>
                        {
                            TechcardPrint();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "help",
                        Enabled = true,
                        Group = "main",
                        Title = "Справка",
                        Description = "Показать справочную информацию",
                        ButtonUse = true,
                        ButtonName = "HelpButton",
                        HotKey = "F1",
                        AccessLevel = Role.AccessMode.ReadOnly,
                        Action = () =>
                        {
                            Central.ShowHelp(DocumentationUrl);
                        },
                    }); 
                    Commander.Add(new CommandItem()
                    {
                        Name = "export_to_excel",
                        Group = "grid_excel",
                        Enabled = true,
                        Title = "В Excel",
                        Description = "Выгрузить данные в Excel файл",
                        ButtonUse = true,
                        ButtonName = "ExportToExcelButton",
                        Action = () =>
                        {
                            Grid.ItemsExportExcel();
                        },
                    });
                }

                Commander.SetCurrentGroup("crud");
                {

                    Commander.Add(new CommandItem()
                    {
                        Name = "create",
                        Title = "Добавить",
                        Enabled = true,
                        MenuUse = true,
                        HotKey = "Insert",
                        ButtonUse = true,
                        ButtonName = "AddButton",
                        Description = "Добавить новую заявку на ТК",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var webTk = new WebTechnologicalMapForm();
                            webTk.ReciverName = this.ControlName;
                            webTk.IdTk = 0;
                            webTk.FlagUpdateOrder = 1;
                            webTk.Show();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "edit",
                        Title = "Изменить",
                        Enabled = true,
                        MenuUse = true,
                        HotKey = "Return|DoubleCLick",
                        ButtonUse = true,
                        ButtonName = "EditButton",
                        Description = "Изменить существующую заявку на ТК",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var webTk = new WebTechnologicalMapForm();
                            webTk.ReciverName = this.ControlName;
                            webTk.IdTk = Grid.SelectedItem.CheckGet("ID_TK").ToInt();
                            webTk.FlagUpdateOrder = 0;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("ID_TK").ToInt() > 0
                                && row.CheckGet("STATUS").ToInt() == 0
                                && row.CheckGet("MANAGER_CREATOR_FLAG").ToInt() == 1)
                            {
                                webTk.FlagUpdateOrder = 1;
                            }
                            webTk.Show();
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "delete",
                        Title = "Удалить",
                        Enabled = true,
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "DeleteButton",
                        Description = "Удалить выбранную техкарту",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var dw = new DialogWindow("Вы действительно хотите удалить заявку на техкарту?", "Удаление заявки на ТК", "", DialogWindowButtons.NoYes);
                            if ((bool)dw.ShowDialog())
                            {
                                Delete();
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("ID_TK").ToInt() > 0
                                && row.CheckGet("STATUS").ToInt() == 0
                                && row.CheckGet("MANAGER_CREATOR_FLAG").ToInt() == 1)
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                }


                Commander.SetCurrentGroup("chat");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "open_chat_with_client",
                        Enabled = true,
                        Title = "Открыть чат с клиентом",
                        Group = "chatoperation",
                        MenuUse = true,
                        ButtonName = "",
                        ButtonUse = false,
                        Action = () =>
                        {
                            OpenChat(0);
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "open_inner_chat",
                        Enabled = true,
                        Title = "Открыть внутренний чат",
                        Group = "chatoperation",
                        MenuUse = true,
                        ButtonName = "",
                        ButtonUse = false,
                        Action = () =>
                        {
                            OpenChat(1);
                        },
                    });
                }

                Commander.SetCurrentGroup("operations");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "approve",
                        Title = "Согласовать",
                        Enabled = true,
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "ApproveButton",
                        Description = "Согласовать техкарту с клиентом",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            AgreedTk();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("ID_TK").ToInt() > 0
                                && row.CheckGet("STATUS").ToInt() == 3
                                && row.CheckGet("MANAGER_CREATOR_FLAG").ToInt() == 1)
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "pokupatel_contact",
                        Title = "Контакты покупателя",
                        Enabled = true,
                        MenuUse = true,
                        ButtonUse = false,
                        Description = "Показать список контактов для связи с покупателем",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var contacts = new WebTechnologicalMapPokupatelContactList();
                            contacts.ReceiverName = this.ControlName;
                            contacts.IdPok = Grid.SelectedItem.CheckGet("ID_POK").ToInt();
                            contacts.Show();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("ID_POK").ToInt() > 0)
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                }


                Commander.SetCurrentGroup("file");
                {

                    Commander.Add(new CommandItem()
                    {
                        Name = "show_tk",
                        Enabled = true,
                        Title = "Показать ТК",
                        Description = "Показать Excel файл техкарты",
                        ButtonUse = true,
                        Group = "file",
                        ButtonName = "ShowFileTkButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            var row = Grid.SelectedItem;
                            var path = row.CheckGet("PATHTK");

                            var folder_new = row.CheckGet("PATH_NEW");
                            var folder_work = row.CheckGet("PATH_WORK");
                            var folder_archive = row.CheckGet("PATH_ARCHIVE");


                            if (File.Exists(Path.Combine(folder_work, path)))
                            {
                                Application excelApp = null;
                                Workbook workbook = null;
                                try
                                {
                                    excelApp = new Application();
                                    excelApp.Visible = true;
                                    excelApp.AutomationSecurity = MsoAutomationSecurity.msoAutomationSecurityForceDisable;

                                    workbook = excelApp.Workbooks.Open(Path.Combine(folder_work, path));
                                }
                                catch (Exception ex)
                                {
                                    workbook?.Close(false);
                                    excelApp?.Quit();
                                }
                            }
                            else if (File.Exists(Path.Combine(folder_new, path)))
                            {
                                Application excelApp = null;
                                Workbook workbook = null;
                                try
                                {
                                    excelApp = new Application();
                                    excelApp.Visible = true;
                                    excelApp.AutomationSecurity = MsoAutomationSecurity.msoAutomationSecurityForceDisable;

                                    workbook = excelApp.Workbooks.Open(Path.Combine(folder_new, path));
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Ошибка: {ex.Message}");
                                    workbook?.Close(false);
                                    excelApp?.Quit();
                                }
                            }
                            else if (File.Exists(Path.Combine(folder_archive, path)))
                            {
                                Application excelApp = null;
                                Workbook workbook = null;
                                try
                                {
                                    excelApp = new Application();
                                    excelApp.Visible = true;
                                    excelApp.AutomationSecurity = MsoAutomationSecurity.msoAutomationSecurityForceDisable;

                                    workbook = excelApp.Workbooks.Open(Path.Combine(folder_archive, path));
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Ошибка: {ex.Message}");
                                    workbook?.Close(false);
                                    excelApp?.Quit();
                                }
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;

                            var row = Grid.SelectedItem;
                            var path = row.CheckGet("PATHTK");

                            var folder_new = row.CheckGet("PATH_NEW");
                            var folder_work = row.CheckGet("PATH_WORK");
                            var folder_archive = row.CheckGet("PATH_ARCHIVE");

                            if (File.Exists(Path.Combine(folder_work, path))
                                || File.Exists(Path.Combine(folder_new, path))
                                || File.Exists(Path.Combine(folder_archive, path)))
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "attachment_files",
                        Title = "Прикрепленные файлы",
                        MenuUse = true,
                        Enabled = true,
                        Description = "Открыть прикрепленные к техкарте файлы",
                        Action = () =>
                        {
                            OpenAttachments();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "create_tk_request_file",
                        Title = "Запросить файл",
                        MenuUse = true,
                        Enabled = true,
                        Description = "Создать запрос на получение файла чертежа, дизайна или другого.",
                        Action = () =>
                        {
                            CreateTkFileRequest();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "open_drawing",
                        Enabled = true,
                        Title = "Открыть чертеж",
                        Description = "Открыть JPG файл чертежа",
                        ButtonUse = false,
                        MenuUse = true,
                        Action = () =>
                        {
                            ShowDrawing();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;

                            var row = Grid.SelectedItem;
                            if (!row.CheckGet("DRAWING_FILE").IsNullOrEmpty())
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "show_tk_signed",
                        Enabled = true,
                        Title = "Подписанная ТК",
                        Description = "Показать подписанный файл техкарты",
                        ButtonUse = false,
                        Group = "file",
                        MenuUse = true,
                        Action = () =>
                        {
                            var row = Grid.SelectedItem;
                            var path = row.CheckGet("FILE_SIGNED");

                            string storagePath = Central.GetStorageNetworkPathByCode("techcard_signed"); ;

                            var filePath = Path.Combine(storagePath, path);
                            var extension = Path.GetExtension(filePath).ToLower();

                            if (File.Exists(filePath))
                            {
                                Process.Start(new ProcessStartInfo(filePath));
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;

                            var row = Grid.SelectedItem;
                            var path = row.CheckGet("FILE_SIGNED");
                            string storagePath = Central.GetStorageNetworkPathByCode("techcard_signed"); ;

                            var filePath = Path.Combine(storagePath, path);

                            if (File.Exists(filePath))
                            {
                                result = true;
                            }
                            return result;
                        }
                    });

                }

                Commander.SetCurrentGroup("history");
                {

                    Commander.Add(new CommandItem()
                    {
                        Name = "history",
                        Enabled = true,
                        Title = "История изменений",
                        Description = "Показать историю изменений заявки на ТК",
                        ButtonUse = false,
                        Group = "history",
                        MenuUse = true,
                        Action = () =>
                        {
                            var historyFrame = new WebTechnologicalMapHistory();
                            historyFrame.ReceiverName = TabName;
                            historyFrame.IdTk = SelectedItem.CheckGet("ID_TK").ToInt();
                            historyFrame.Show();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("ID_TK").ToInt() > 0)
                            {
                                result = true;
                            }
                            return result;
                        },
                    });
                }

                Commander.Init(this);
            }
        }

        #region "Переменные"
        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }
        /// <summary>
        /// Название вкладки
        /// </summary>
        public string TabName;

        private string IdTk { get; set; }

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// Таймер заполнения поля фильтров
        /// </summary>
        public DispatcherTimer TemplateTimeoutTimer;

        #endregion



        #region "Загрузка справочников"

        public void SetDefaults()
        {
            LoadStatus();
        }

        /// <summary>
        /// Загрузка менеджеров
        /// </summary>
        public async void LoadManagers()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "ListManagers");

            q.DoQuery();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {

                    var ds = ListDataSet.Create(result, "ITEMS");
                    var managers = new Dictionary<string, string>();
                    managers.Add("-1", "Все");
                    foreach (var item in ds.Items)
                    {
                        if (item["ID"].ToInt() != 0)
                        {
                            managers.CheckAdd(item["ID"].ToInt().ToString(), item.CheckGet("NAME"));

                        }
                    }
                    ManagerSelectBox.Items = managers;
                    var empl_id = Central.User.EmployeeId;
                    if (ManagerSelectBox.Items.ContainsKey(empl_id.ToString()))
                    {
                        ManagerSelectBox.SetSelectedItemByKey(empl_id.ToString());
                    }
                    else
                    {
                        ManagerSelectBox.SetSelectedItemByKey("-1");
                    }

                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Загрузка статусов
        /// </summary>
        public async void LoadStatus()
        {
            var status = new Dictionary<string, string>()
            {
                {"-1", "Все"},
                {"0", "Новая"},
                {"5", "Из РЦ"},
                {"1", "В работе"},
                {"6", "Ожидание ответа"},
                {"3", "Согласование"},
                {"7", "Согласована"},
                {"4", "Готова"},
                {"2", "Отменена"},

            };
            StatusSelectBox.Items = status;
            StatusSelectBox.SetSelectedItemByKey("-1");

        }
        #endregion

        #region "Форма"
        public void FormInit()
        {
            //инициализация формы
            {
                Form = new FormHelper();

                //список колонок формы
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path = "DT_FROM",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = DtFrom,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "DT_TO",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = DtTo,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path="SEARCH_LENGTH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=SizeLength,
                        ControlType="TextBox",
                    },
                    new FormHelperField()
                    {
                        Path="SEARCH_WIDTH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=SizeWidth,
                        ControlType="TextBox",
                    },
                    new FormHelperField()
                    {
                        Path="SEARCH_HEIGTH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=SizeHeigth,
                        ControlType="TextBox",
                    },
                    new FormHelperField()
                    {
                        Path="SEARCH_TEXT",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=SearchTextBox,
                        ControlType="TextBox",
                    },
                };

                Form.SetFields(fields);
            }
        }
        #endregion



        #region "Таблица"
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
                    Visible = false,
                },
                new DataGridHelperColumn
                {
                    Header="Дата создания",
                    Path="DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=8,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="Дата принятия",
                    Path="ACCEPTANCE_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=8,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="Дата отправки на согласование",
                    Path="SENDING_FOR_APPROVAL_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=8,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="Номер",
                    Path="NUM",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header="РЦ",
                    Path="FLAG_PCULC",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="Покупатель",
                    Path="NAME_POK",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.FontWeight,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";
                                var fontWeight= new FontWeight();
                                fontWeight=FontWeights.Normal;
                                if( row.CheckGet("MANAGER_CREATOR_FLAG").ToBool())
                                {
                                    fontWeight=FontWeights.Bold;
                                }

                                return fontWeight;
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Вид изделия",
                    Path="NAME_PCLASS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="ART",
                    ColumnType=ColumnTypeRef.String,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Размер",
                    Path="TK_SIZE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.ForegroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("STATUS").ToInt().ContainsIn(0,1,5,6)
                                    && row.CheckGet("DUPLICATE").ToInt().ContainsIn(1,2,3))

                                {
                                    color = HColor.RedFG;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        },
                        {
                            StylerTypeRef.FontWeight,
                            row =>
                            {
                                var fontWeight= new FontWeight();
                                fontWeight=FontWeights.Normal;
                                if( row.CheckGet("STATUS").ToInt()==0
                                    && row.CheckGet("DUPLICATE").ToInt()>=2)
                                {
                                    fontWeight=FontWeights.Bold;
                                }

                                return fontWeight;
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="PRODUCT_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
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
                    Header="Марка для артикула",
                    Path="CUSTOMER_CARTON",
                    ColumnType=ColumnTypeRef.String,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Спецкартон",
                    Path="SPECIAL_RAW",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Печать",
                    Path="PRINTING",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Цвета",
                    Path="COLORS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=5,
                    Options="valuestripbr",
                },
                new DataGridHelperColumn
                {
                    Header="Упаковка",
                    Path="ID_OTGR",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Автосборка",
                    Path="AUTOMATIC",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Создание заявки менеджером",
                    Path="MANAGER_CREATOR_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Количество похожих",
                    Path="DUPLICATE",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=11,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Дизайн",
                    Path="DESIGN",
                    ColumnType=ColumnTypeRef.String,
                    Width2=4,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("SCORER_PRINT_FLAG").ToInt()==1
                                    && row.CheckGet("DESIGN")!="Разрабатывается"
                                    && row.CheckGet("DESIGN")!="Выполнен")
                                {
                                    color = HColor.Red;
                                }
                                else if( row.CheckGet("DESIGN")=="Разрабатывается")
                                {
                                    color = HColor.Yellow;
                                }
                                else if( row.CheckGet("DESIGN")=="Выполнен")
                                {
                                    color = HColor.Green;
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
                                var color = "";

                                if( (row.CheckGet("DESIGN")=="Разрабатывается"
                                    || row.CheckGet("DESIGN")=="Выполнен")
                                    && row.CheckGet("SCORER_PRINT").ToInt()==2)
                                {
                                    color = HColor.RedFG;
                                }

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
                    Header="Чертеж",
                    Path="DRAWING",
                    ColumnType=ColumnTypeRef.String,
                    Width2=4,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("DRAWING")=="Разрабатывается")
                                {
                                    color = HColor.Yellow;
                                }
                                if( row.CheckGet("DRAWING")=="Выполнен")
                                {
                                    color = HColor.Green;
                                }

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
                    Header="ИД чата",
                    Path="CHAT_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header="Сообщения клиента",
                    Path="FLAG_CHAT_CLIENT",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Внутренние сообщения",
                    Path="FLAG_CHAT_INNER",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Чат с клиентом",
                    Path="NOT_READ_CLIENT",
                    Width2=4,
                    ColumnType=ColumnTypeRef.Double,
                    Format = "N0",
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("FLAG_CHAT_CLIENT").ToInt() > 0 )
                                {
                                    color = HColor.Yellow;
                                }
                                if( row.CheckGet("NOT_READ_CLIENT").ToInt() > 0 )
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
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Чат с коллегами",
                    Path="NOT_READ_INNER",
                    Width2=4,
                    ColumnType=ColumnTypeRef.Double,
                    Format = "N0",
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("FLAG_CHAT_INNER").ToInt() > 0 )
                                {
                                    color = HColor.Yellow;
                                }
                                if( row.CheckGet("NOT_READ_INNER").ToInt() > 0 )
                                {
                                    color = HColor.Orange;
                                }

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
                    Header="Файлы клиента",
                    Path="WEB_FILE_CLIENT",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Файлы наши",
                    Path="WEB_FILE_OUR",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Согласовано клиентом",
                    Path="AGREED_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("FILE_SIGNED_FLAG").ToBool())
                                {
                                    color = HColor.Green;
                                }

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
                    Header="Подписанная ТК",
                    Path="FILE_SIGNED_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=11,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("FILE_SIGNED_FLAG").ToBool())
                                {
                                    color = HColor.Green;
                                }

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
                    Header="Подписанная ТК",
                    Path="FILE_SIGNED",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=11,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header="Комментарий клиента",
                    Path="COMMENTS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                    Options="valuestripbr",
                },
                new DataGridHelperColumn
                {
                    Header="Инженер",
                    Path="ENG_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                },
                new DataGridHelperColumn
                {
                    Header="ИД инженера",
                    Path="ENG_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=11,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="Дизайнер",
                    Path="DES_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                },
                new DataGridHelperColumn
                {
                    Header="ИД дизайнера",
                    Path="DES_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=11,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="Конструктор",
                    Path="CON_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                },
                new DataGridHelperColumn
                {
                    Header="ИД конструктора",
                    Path="CON_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=11,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="Примечание для дизайнера",
                    Path="DESIGNER_NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание для конструктора",
                    Path="DRAWER_NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                },
                new DataGridHelperColumn
                {
                    Header="Менеджер РЦ",
                    Path="PR_CAL_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                },
                new DataGridHelperColumn
                {
                    Header="Менеджер ОРК",
                    Path="MAN_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                },
                new DataGridHelperColumn
                {
                    Header="Менеджер ОП",
                    Path="SALES_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                },

                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=4,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header="ИД покупателя",
                    Path="ID_POK",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=25,
                    Visible = false,
                },
                new DataGridHelperColumn
                {
                    Header="Имя покупателя(короткое)",
                    Path="CUSTOMER_SHORT",
                    ColumnType=ColumnTypeRef.String,
                    Width2=25,
                    Visible = false,
                },
                new DataGridHelperColumn
                {
                    Header="Путь (в работе)",
                    Path="PATH_WORK",
                    ColumnType=ColumnTypeRef.String,
                    Width2=25,
                    Visible = false,
                },
                new DataGridHelperColumn
                {
                    Header="Путь (новые)",
                    Path="PATH_NEW",
                    ColumnType=ColumnTypeRef.String,
                    Width2=25,
                    Visible = false,
                },
                new DataGridHelperColumn
                {
                    Header="Путь (архивные)",
                    Path="PATH_ARCHIVE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=25,
                    Visible = false,
                },
                new DataGridHelperColumn
                {
                    Header="Файл чертежа",
                    Path="DRAWING_FILE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=25,
                    Visible = false,
                },
                new DataGridHelperColumn
                {
                    Header="Печать по рилёвке",
                    Path="SCORER_PRINT_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=2,
                    Visible = false,
                },
            };
            Grid.SetColumns(columns);

            Grid.SetPrimaryKey("ID_TK");
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.SearchText = GridSearch;
            Grid.Toolbar = GridToolbar;
            Grid.Commands = Commander;
            // Раскраска строк
            Grid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                // Цвета фона строк
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=System.Windows.DependencyProperty.UnsetValue;
                        var color = "";
                        int currentStatus = 0;
                        currentStatus = row.CheckGet("STATUS").ToInt();
                        switch (currentStatus)
                        {
                            // Новая
                            case 0:
                            case 5:
                                color = HColor.Blue;
                                break;
                            // В работе
                            case 1:
                            case 3:
                            case 7:
                                color = HColor.White;
                                break;
                            // Готова
                            case 4:
                                color = HColor.Green;
                                break;
                            case 6:
                                color = HColor.Orange;
                                break;
                            case 2:
                                color = HColor.Red;
                                break;
                            // Отменена
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
                },
                {
                    DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        if (row.CheckGet("STATUS").ToInt()==5)
                        {
                            color = HColor.BlueDark;
                        }
                        if (row.CheckGet("STATUS").ToInt().ContainsIn(3,7))
                        {
                            if (row.CheckGet("ART").IsNullOrEmpty())
                            {
                                color = HColor.BlueDark;
                            }
                            else
                            {
                                color = HColor.GreenFG;
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

            Grid.ItemsAutoUpdate = true;
            Grid.AutoUpdateInterval = 180;

            Grid.OnLoadItems = LoadItems;
            Grid.OnFilterItems = FilterItems;
            Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    SelectedItem = selectedItem;
                }

            };

            Grid.Init();
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        public async void LoadItems()
        {
            MaskedTextBox_Parsed();
            Grid.Toolbar.IsEnabled = false;
            Grid.ShowSplash();
            string showAll = (bool)ShowAllCheckBox.IsChecked ? "1" : "0";

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "ListForManager");
            q.Request.SetParam("SHOW_ALL", showAll);
            q.Request.SetParam("EMPL_ID", ManagerSelectBox.SelectedItem.Key);
            if (Form.GetValueByPath("DT_FROM").IsNullOrEmpty())
            {
                q.Request.SetParam("DT_FROM", "");
            }
            else
            {
                q.Request.SetParam("DT_FROM", Form.GetValueByPath("DT_FROM").ToDateTime().ToString("dd.MM.yyyy"));
            }
            if (Form.GetValueByPath("DT_TO").IsNullOrEmpty())
            {
                q.Request.SetParam("DT_TO", "");
            }
            else
            {
                q.Request.SetParam("DT_TO", Form.GetValueByPath("DT_TO").ToDateTime().AddDays(1).ToString("dd.MM.yyyy"));
            }
            q.Request.SetParam("SEARCH_LENGTH", Form.GetValueByPath("SEARCH_LENGTH"));
            q.Request.SetParam("SEARCH_WIDTH", Form.GetValueByPath("SEARCH_WIDTH"));
            q.Request.SetParam("SEARCH_HEIGTH", Form.GetValueByPath("SEARCH_HEIGTH"));
            q.Request.SetParam("SEARCH_TEXT", Form.GetValueByPath("SEARCH_TEXT"));

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
                if (!IdTk.IsNullOrEmpty())
                {
                    Grid.SelectRowByKey(IdTk);
                    IdTk = "";
                }
            }
            else
            {
                q.ProcessError();
            }
            Grid.HideSplash();
            Grid.Toolbar.IsEnabled = true;
        }

        /// <summary>
        /// Фильтрация записей таблицы 
        /// </summary>
        public void FilterItems()
        {
            if (Grid?.Items != null)
            {
                if (Grid.Items.Count > 0)
                {
                    int status = StatusSelectBox.SelectedItem.Key.ToInt();

                    var list = new List<Dictionary<string, string>>();
                    foreach (var item in Grid.Items)
                    {
                        bool includeByVisible = true;
                        if (status != -1)
                        {
                            if (item.CheckGet("STATUS").ToInt() != status)
                            {
                                includeByVisible = false;
                            }
                        }

                        if (includeByVisible)
                        {
                            list.Add(item);
                        }
                    }
                    Grid.Items = list;
                    Grid.SelectRowFirst();

                }
            }
        }

        #endregion

        #region "Функции"
        /// <summary>
        /// Удаление заявки на ТК
        /// </summary>
        public async void Delete()
        {
            Grid.Toolbar.IsEnabled = false;
            Grid.ShowSplash();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "DeleteTkOrder");
            q.Request.SetParam("ID_TK", Grid.SelectedItem.CheckGet("ID_TK"));

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    Grid.LoadItems();
                }
            }
            else
            {
                q.ProcessError();
            }
            Grid.HideSplash();
            Grid.Toolbar.IsEnabled = true;
        }

        /// <summary>
        /// Согласование заявки на ТК
        /// </summary>
        public async void AgreedTk()
        {
            Grid.Toolbar.IsEnabled = false;
            Grid.ShowSplash();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "SetAgreed");
            q.Request.SetParam("ID_TK", Grid.SelectedItem.CheckGet("ID_TK"));
            q.Request.SetParam("AGREED_FLAG", "1");
            q.Request.SetParam("FLAG_UPD_AGREED_DTTM", "1");
            q.Request.SetParam("FLAG_UPD_UNSIGNED_FLAG", "0");

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    Grid.LoadItems();
                }
            }
            else
            {
                q.ProcessError();
            }
            Grid.HideSplash();
            Grid.Toolbar.IsEnabled = true;
        }
        #endregion


        /// <summary>
        /// Печать информации по техкарте
        /// </summary>
        private async void TechcardPrint(int chatType = 0)
        {
            if (SelectedItem != null)
            {
                int id = SelectedItem.CheckGet("ID_TK").ToInt();

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "WebTechnologicalMap");
                q.Request.SetParam("Action", "InformationDocument");
                q.Request.SetParam("ID_TK", id.ToString());

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    if (q.Answer.Type == LPackClientAnswer.AnswerTypeRef.File)
                    {
                        Central.OpenFile(q.Answer.DownloadFilePath);

                    }
                    else
                    {
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        #region "Приложенные файлы"
        /// <summary>
        /// Открытие вкладку с приложенными файлами
        /// </summary>
        private void OpenAttachments()
        {
            if (SelectedItem != null)
            {
                var fraimeFiles = new WebTechnologicalMapFiles();
                fraimeFiles.TkId = SelectedItem.CheckGet("ID_TK").ToInt();
                fraimeFiles.TechCardName = SelectedItem.CheckGet("NUM").ToString();
                fraimeFiles.FromManager = 1;

                fraimeFiles.ReturnTabName = TabName;
                fraimeFiles.Show();
            }
        }
        #endregion

        private void CreateTkFileRequest()
        {
            if (SelectedItem != null)
            {
                var fraimeFiles = new WebTechnologicalMapRequestFileForm();
                fraimeFiles.IdTk = SelectedItem.CheckGet("ID_TK").ToInt();
                fraimeFiles.IdDes = SelectedItem.CheckGet("DES_ID").ToInt();
                fraimeFiles.IdCon = SelectedItem.CheckGet("CON_ID").ToInt();

                fraimeFiles.Show();
            }
        }

        /// <summary>
        /// Открытие файла
        /// </summary>
        public async void ShowDrawing()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "GetDrawingFile");
            q.Request.SetParam("DRAWING_FILE", Grid.SelectedItem.CheckGet("DRAWING_FILE").ToString());
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
        #region "Чат"

        /// <summary>
        /// Открытие вкладки с чатом по веб-техкарте
        /// </summary>
        /// <param name="chatType">Тип чата: 0 - чат с клиентом, 1 - чат с коллегами</param>
        private void OpenChat(int chatType = 0)
        {
            if (SelectedItem != null)
            {
                var chatFrame = new WebTechnologicalMapChat();
                chatFrame.ObjectId = SelectedItem.CheckGet("ID_TK").ToInt();
                chatFrame.ReceiverName = ControlName;
                chatFrame.ChatObject = "WebTechMap";
                chatFrame.ChatType = chatType;
                if (chatType == 1)
                {
                    chatFrame.ChatId = SelectedItem.CheckGet("CHAT_ID").ToInt();
                }
                else
                {
                    chatFrame.ChatId = SelectedItem.CheckGet("ID_TK").ToInt();
                }
                chatFrame.Edit();
            }
        }

        #endregion



        #region Функции парсинга размеров 

        private void MaskedTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Back || e.Key == Key.Tab ||
                (e.Key >= Key.D0 && e.Key <= Key.D9) ||
                (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) ||
                e.Key == Key.X || e.Key == Key.Oem4
                || e.Key == Key.Left || e.Key == Key.Right)
            {
                if (e.Key == Key.Tab || e.Key == Key.X || e.Key == Key.Oem4)
                {
                    int caretIndex = MaskedTextBox.CaretIndex;
                    var text = MaskedTextBox.Text;
                    var firstX = text.IndexOf('x');
                    var secondX = text.IndexOf('x', firstX + 1);
                    if (caretIndex < firstX)
                    {
                        caretIndex = firstX + 3;
                    }
                    else if (caretIndex < secondX)
                    {
                        caretIndex = secondX + 3;
                    }
                    MaskedTextBox.CaretIndex = caretIndex;
                    e.Handled = true;

                }
            }
            else
            {
                e.Handled = true;
            }
        }

        private void MaskedTextBox_Parsed()
        {
            var text = MaskedTextBox.Text;
            string[] str = text.Split(new[] { "x" }, StringSplitOptions.RemoveEmptyEntries);
            List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();
            if (str.Count() != 0)
            {
                SizeLength.Text = str[0].Trim();
                SizeWidth.Text = str[1].Trim();
                SizeHeigth.Text = str[2].Trim();

            }
            else
            {
                SizeLength.Text = "";
                SizeWidth.Text = "";
                SizeHeigth.Text = "";
            }
        }
        private void MaskedTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (MaskedTextBox.Text == "")
            {
                MaskedTextBox.Text = "  x  x  ";
                MaskedTextBox.Dispatcher.BeginInvoke(new Action(() =>
                {
                    MaskedTextBox.CaretIndex = 0;
                }), System.Windows.Threading.DispatcherPriority.Input);

                e.Handled = true;
            }
        }


        #endregion

        private void ShowAllCheckBox_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void ManagerSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void DtTo_EditValueChanging(object sender, DevExpress.Xpf.Editors.EditValueChangingEventArgs e)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
        }

        private void DtFrom_EditValueChanging(object sender, DevExpress.Xpf.Editors.EditValueChangingEventArgs e)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
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

        private void MaskedTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if ((!SizeLength.Text.IsNullOrEmpty() || !SizeWidth.Text.IsNullOrEmpty() || !SizeHeigth.Text.IsNullOrEmpty() || (!SearchTextBox.Text.IsNullOrEmpty() && SearchTextBox.Text.Length > 2))
                && (TemplateTimeoutTimer == null || TemplateTimeoutTimer?.IsEnabled == false))
            {
                RunTemplateTimeoutTimer();
            }
        }

        private void TextBoxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ((!SizeLength.Text.IsNullOrEmpty() || !SizeWidth.Text.IsNullOrEmpty() || !SizeHeigth.Text.IsNullOrEmpty() || (!SearchTextBox.Text.IsNullOrEmpty() && SearchTextBox.Text.Length > 2))
                && (TemplateTimeoutTimer == null || TemplateTimeoutTimer?.IsEnabled == false))
            {
                RunTemplateTimeoutTimer();
            }
        }
    }
}
