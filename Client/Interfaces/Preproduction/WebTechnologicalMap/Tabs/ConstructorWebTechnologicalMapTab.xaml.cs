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
using System.Windows.Input;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Excel;
using Application = Microsoft.Office.Interop.Excel.Application;
using Style = System.Windows.Style;
using Action = System.Action;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс веб-техкарт (Конструкторы)
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class ConstructorWebTechnologicalMapTab : ControlBase
    {
        public ConstructorWebTechnologicalMapTab()
        {
            InitializeComponent();

            RoleName = "[erp]constructor_web_tech_map";
            ControlTitle = "Веб-техкарты (конструкторы)";
            DocumentationUrl = "/";
            TabName = "ConstructorWebTechMap";
            OnLoad = () =>
            {
                FormInit();
                LoadConstructors();
                GridInit();
            };

            OnUnload = () =>
            {
                Grid.Destruct();
            };

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverGroup == "WebTechnologicalMap")
                {
                    if (msg.ReceiverName == ControlName)
                    {
                        switch (msg.Action)
                        {
                            case "Refresh":
                                Grid.LoadItems();
                                break;
                            case "Rework":
                                Grid.LoadItems();
                                break;
                        }
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
                        Group = "grid_base",
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
                        Group = "grid_base",
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
                }

                Commander.SetCurrentGroup("special");
                {

                    Commander.Add(new CommandItem()
                    {
                        Name = "ShowTk",
                        Enabled = true,
                        Title = "Показать ТК",
                        Description = "Показать Excel файл техкарты",
                        ButtonUse = true,
                        ButtonName = "ShowFileTkButton",
                        MenuUse = true,
                        AccessLevel = Role.AccessMode.ReadOnly,
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
                    Commander.Add(new CommandItem()
                    {
                        Name = "show_pokupatel_contact",
                        Title = "Контакты покупателя",
                        Group = "chatoperation",
                        MenuUse = true,
                        Enabled = true,
                        Description = "Показать контакты для связи с покупателем",
                        Action = () =>
                        {
                            var pokupatelContact = new WebTechnologicalMapPokupatelContact();
                            pokupatelContact.ReceiverName = TabName;
                            pokupatelContact.IdTk = SelectedItem.CheckGet("ID_TK").ToInt();
                            pokupatelContact.IdPok = SelectedItem.CheckGet("ID_POK").ToInt();
                            pokupatelContact.Show();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "open_comments",
                        Enabled = true,
                        Title = "Открыть примечания",
                        MenuUse = true,
                        Action = () =>
                        {
                            OpenNotes();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "open_drawing",
                        MenuGroupHeader = "drawing",
                        MenuGroupHeaderName = "Чертеж",
                        Enabled = true,
                        Title = "Открыть чертеж",
                        Description = "Открыть JPG файл чертежа",
                        ButtonUse = false,
                        MenuUse = true,
                        Action = () =>
                        {
                            var row = Grid.SelectedItem;
                            Process.Start(new ProcessStartInfo(row.CheckGet("DRAWING_FILE")) { UseShellExecute = true });
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;

                            var row = Grid.SelectedItem;
                            if (File.Exists(row.CheckGet("DRAWING_FILE")))
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "attach_drawing",
                        MenuGroupHeader = "drawing",
                        MenuGroupHeaderName = "Чертеж",
                        Enabled = true,
                        Title = "Привязать чертеж",
                        Description = "Привязать JPG файл чертежа",
                        ButtonUse = false,
                        MenuUse = true,
                        Action = () =>
                        {
                            AttachDrawing();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;

                            var row = Grid.SelectedItem;
                            if (row.CheckGet("DRAWING_FILE").IsNullOrEmpty() || !File.Exists(row.CheckGet("DRAWING_FILE")))
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "delete_drawing",
                        MenuGroupHeader = "drawing",
                        MenuGroupHeaderName = "Чертеж",
                        Enabled = true,
                        Title = "Отвязать чертеж",
                        Description = "Отвязать JPG файл чертежа",
                        ButtonUse = false,
                        MenuUse = true,
                        Action = () =>
                        {
                            var dw = new DialogWindow("Вы действительно хотите отвязать чертеж от техкарты?", "Отвязать чертеж", "", DialogWindowButtons.NoYes);
                            if ((bool)dw.ShowDialog())
                            {
                                var row = Grid.SelectedItem;
                                UpdateDrawing(row.CheckGet("ID_TK").ToInt(), null);
                            }
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
                        Name = "folder_drawing",
                        MenuGroupHeader = "drawing",
                        MenuGroupHeaderName = "Чертеж",
                        Enabled = true,
                        Title = "Папка чертежа",
                        Description = "Открыть папку чертежа",
                        ButtonUse = false,
                        MenuUse = true,
                        Action = () =>
                        {
                            OpenDrawingFolder(Grid.SelectedItem.CheckGet("DRAWING_FILE"));
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "order_sample_drawing",
                        MenuGroupHeader = "drawing",
                        MenuGroupHeaderName = "Чертеж",
                        Enabled = true,
                        Title = "Заказать образец",
                        Description = "Заказать образец для чертежа",
                        ButtonUse = false,
                        MenuUse = true,
                        Action = () =>
                        {
                            var dw = new DialogWindow("Вы действительно хотите сделать заявку на образец?", "Заказ образца", "", DialogWindowButtons.NoYes);
                            if ((bool)dw.ShowDialog())
                            {
                                var row = Grid.SelectedItem;
                                string pok_name = row.CheckGet("NAME_POK");
                                if (pok_name.Length > 16)
                                {
                                    pok_name = row.CheckGet("NAME_POK").Substring(0, 16);
                                }
                                OrderSample(row.CheckGet("ID_TK").ToInt(), pok_name);
                            }

                        },
                        CheckEnabled = () =>
                        {
                            var result = false;

                            var row = Grid.SelectedItem;
                            if (!row.CheckGet("DRAWING_FILE").IsNullOrEmpty() && File.Exists(row.CheckGet("DRAWING_FILE")))
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "show_duplicate",
                        Title = "Показать похожие",
                        MenuUse = true,
                        Enabled = true,
                        MenuGroupHeader = "drawing",
                        MenuGroupHeaderName = "Чертеж",
                        Description = "Показать похожие техкарты",
                        Action = () =>
                        {
                            ShowDuplicate();
                        },
                    });
                }

                Commander.SetCurrentGroup("row");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "lvl1",
                        MenuGroupHeader = "difficulty_level",
                        MenuGroupHeaderName = "Указать уровень сложности",
                        Enabled = true,
                        Title = "1 - Низкий",
                        Description = "Указать низкий уровень сложности",
                        ButtonUse = false,
                        MenuUse = true,
                        Action = () =>
                        {
                            SetDifficultyLevel(1);
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "lvl2",
                        MenuGroupHeader = "difficulty_level",
                        MenuGroupHeaderName = "Указать уровень сложности",
                        Enabled = true,
                        Title = "2 - Средний",
                        Description = "Указать средний уровень сложности",
                        ButtonUse = false,
                        MenuUse = true,
                        Action = () =>
                        {
                            SetDifficultyLevel(2);
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "lvl3",
                        MenuGroupHeader = "difficulty_level",
                        MenuGroupHeaderName = "Указать уровень сложности",
                        Enabled = true,
                        Title = "3 - Высокий",
                        Description = "Указать высокий уровень сложности",
                        ButtonUse = false,
                        MenuUse = true,
                        Action = () =>
                        {
                            SetDifficultyLevel(3);
                        },
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "return_for_rework",
                        Enabled = true,
                        Title = "Вернуть на доработку",
                        MenuUse = true,
                        Action = () =>
                        {
                            var rework = new WebTechnologicalMapRework();
                            rework.TkId = Grid.SelectedItem.CheckGet("ID_TK").ToInt();
                            rework.ResiverName = ControlName;
                            rework.Show();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;

                            var row = Grid.SelectedItem;
                            if (row.CheckGet("STATUS_FOR_CONSTRUCTOR").ToInt().ContainsIn(2, 3))
                            {
                                result = true;
                            }
                            return result;
                        }
                    });

                }

                Commander.SetCurrentGroup("row2");
                { 
                    Commander.Add(new CommandItem()
                    {
                        Name = "pin",
                        Title = "Закрепить за собой",
                        Group = "operations",
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "PinButton",
                        Description = "Закрепить за собой техкарту",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var dw = new DialogWindow("Вы действительно хотите закрепить за собой техкарту?", "Закрепить техкарту", "", DialogWindowButtons.NoYes);
                            if ((bool)dw.ShowDialog())
                            {
                                TechMapChangeConstructorId(0);
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("STATUS_FOR_CONSTRUCTOR").ToInt().ContainsIn(1,2) && row.CheckGet("ID_CON").ToInt() != Central.User.EmployeeId.ToInt())
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "unpin",
                        Title = "Открепить от себя",
                        Group = "operations",
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "UnpinButton",
                        Description = "Открепить техкарту от себя",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var dw = new DialogWindow("Вы действительно хотите открепить от себя техкарту?", "Открепить техкарту", "", DialogWindowButtons.NoYes);
                            if ((bool)dw.ShowDialog())
                            {
                                TechMapChangeConstructorId(1);
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("ID_CON").ToInt() == Central.User.EmployeeId.ToInt())
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "confirm_design",
                        Enabled = true,
                        Group = "operations",
                        Title = "Отметить разработку чертежа",
                        Description = "Отметить разработку чертежа",
                        ButtonUse = true,
                        ButtonName = "ConfirmDrawingButton",
                        MenuUse = false,
                        Action = () =>
                        {
                            BindDrawing();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem; 
                            if (row.CheckGet("STATUS_FOR_CONSTRUCTOR").ToInt() == 2 
                                && !row.CheckGet("DRAWING_FILE").IsNullOrEmpty() 
                                && File.Exists(row.CheckGet("DRAWING_FILE")))
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

        /// <summary>
        /// Название вкладки
        /// </summary>
        public string TabName;

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// Выбранный ИД записи
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// Таймер заполнения поля фильтров
        /// </summary>
        public DispatcherTimer TemplateTimeoutTimer;

        /// <summary>
        /// Загрузка дизайнеров
        /// </summary>
        public async void LoadConstructors()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "ListConstructors");

            q.DoQuery();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {

                    var ds = ListDataSet.Create(result, "ITEMS");
                    var designers = new Dictionary<string, string>();
                    designers.Add("-1", "Все");
                    foreach (var item in ds.Items)
                    {
                        if (item["ID"].ToInt() != 0)
                        {
                            designers.CheckAdd(item["ID"].ToInt().ToString(), item.CheckGet("NAME"));

                        }
                    }
                    ConstructorSelectBox.Items = designers;
                    var empl_id = Central.User.EmployeeId;
                    if (ConstructorSelectBox.Items.ContainsKey(empl_id.ToString()))
                    {
                        ConstructorSelectBox.SetSelectedItemByKey(empl_id.ToString());
                    }
                    else
                    {
                        ConstructorSelectBox.SetSelectedItemByKey("-1");
                    }

                }
            }
            else
            {
                q.ProcessError();
            }
        }

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
                    Width2=2,
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
                    Header="Номер",
                    Path="NUM",
                    ColumnType=ColumnTypeRef.String,
                    Width2=6,
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
                                if( row.CheckGet("FLAG_PCULC").ToInt()==1)
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
                    Header="Артикул",
                    Path="ART",
                    ColumnType=ColumnTypeRef.String,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Площадка",
                    Path="PLACE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="Покупатель",
                    Path="NAME_POK",
                    ColumnType=ColumnTypeRef.String,
                    Width2=25,
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
                    Header="Автосборка",
                    Path="AUTOMATIC",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="Размер",
                    Path="TK_SIZE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.ForegroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("SAME_SAMPLE").ToInt()==1)

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
                    Header="Размер партии",
                    Path="ORDER_QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=25,
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
                    Header="Спецкартон",
                    Path="SPECIAL_RAW",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="Печать",
                    Path="PRINTING",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="Упаковка",
                    Path="ID_OTGR",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="Сообщения клиента",
                    Path="FLAG_CHAT_CLIENT",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=1,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Внутренние сообщения",
                    Path="FLAG_CHAT_INNER",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=1,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Чат с клиентом",
                    Path="NOT_READ_CLIENT",
                    Width2=3,
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
                    Width2=3,
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
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="Файлы наши",
                    Path="WEB_FILE_OUR",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="Сложность",
                    Path="DIFFICULTY_LEVEL",
                    ColumnType=ColumnTypeRef.String,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="Комментарий",
                    Path="COMMENTS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=25,
                    Options="valuestripbr",
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
                    Header="Иженер",
                    Path="ENG_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
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
                    Header="Примечание ОПП",
                    Path="OPP_COMMENT",
                    ColumnType=ColumnTypeRef.String,
                    Width2=25,
                },
                new DataGridHelperColumn
                {
                    Header="Ид конструктора",
                    Path="ID_CON",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=4,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header="Статус конструктора",
                    Path="STATUS_FOR_CONSTRUCTOR",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=4,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header="Статус ТК",
                    Path="TK_ORDER_STATUS",
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
                    Header="Файл чертежа",
                    Path="DRAWING_FILE",
                    ColumnType=ColumnTypeRef.String,
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
                    Header="ИД чата",
                    Path="CHAT_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible = false
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
                    Header="Флагналичия похожих",
                    Path="DUPLICATE",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=11,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="РЦ",
                    Path="FLAG_PCULC",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=3,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Похожие образцы",
                    Path="SAME_SAMPLE",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=3,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Создано менеджером",
                    Path="MANAGER_CREATOR_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Длина",
                    Path="L",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header="Ширина",
                    Path="B",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header="Высота",
                    Path="H",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header="Тип продукции",
                    Path="ID_PCLASS",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible = false
                },
            };
            Grid.SetColumns(columns);

            Grid.SetPrimaryKey("ID_TK");
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.ColumnWidthMin = 15;
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
                        currentStatus = row.CheckGet("STATUS_FOR_CONSTRUCTOR").ToInt();
                        switch (currentStatus)
                        {
                            // Новая
                            case 1:
                                color = HColor.Blue;
                                break;
                            // В работе
                            case 2:
                                color = HColor.White;
                                break;
                            // Готова
                            case 3:
                                color = HColor.Green;
                                break;
                                break;
                            default:
                                color = HColor.White;
                                break;
                        }

                        if (row.CheckGet("TK_ORDER_STATUS").ToInt()==6)
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
                {
                    StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        if( row.CheckGet("STATUS_FOR_CONSTRUCTOR").ToInt()==0)
                        {
                            color = HColor.BlueFg;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };

            Grid.ItemsAutoUpdate = true;
            Grid.AutoUpdateInterval = 300;

            Grid.OnLoadItems = LoadItems;
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

            var p = new Dictionary<string, string>();
            string showAll = (bool)ShowAllCheckBox.IsChecked ? "1" : "0";
            p.Add("SHOW_ALL", showAll);


            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "ListForConstructor");
            p.Add("CON_EMPL_ID", ConstructorSelectBox?.SelectedItem.Key.ToString());
            if (Form.GetValueByPath("DT_FROM").IsNullOrEmpty())
            {
                p.Add("DT_FROM", "");
            }
            else
            {
                p.Add("DT_FROM", Form.GetValueByPath("DT_FROM").ToDateTime().ToString("dd.MM.yyyy"));
            }
            if (Form.GetValueByPath("DT_TO").IsNullOrEmpty())
            {
                p.Add("DT_TO", "");
            }
            else
            {
                p.Add("DT_TO", Form.GetValueByPath("DT_TO").ToDateTime().AddDays(1).ToString("dd.MM.yyyy"));
            }
            p.Add("SEARCH_LENGTH", Form.GetValueByPath("SEARCH_LENGTH"));
            p.Add("SEARCH_WIDTH", Form.GetValueByPath("SEARCH_WIDTH"));
            p.Add("SEARCH_HEIGTH", Form.GetValueByPath("SEARCH_HEIGTH"));
            p.Add("SEARCH_TEXT", Form.GetValueByPath("SEARCH_TEXT"));
            q.Request.SetParams(p);

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
            Grid.HideSplash();
            Grid.Toolbar.IsEnabled = true;
        }

        #endregion


        #region "Функции обработки действий"

        /// <summary>
        /// Изменение ИД конструктора
        /// FLAG_DEL: 0 - установить себя, 1 - удалить себя
        /// </summary>
        private async void TechMapChangeConstructorId(int type)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "SetConstructor");
            q.Request.SetParam("ID_TK", Grid.SelectedItem.CheckGet("ID_TK").ToString());
            q.Request.SetParam("FLAG_DEL", type.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Grid.LoadItems();
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Установка уровня сложности
        /// </summary>
        private async void SetDifficultyLevel(int lvl)
        {
            int id = SelectedItem.CheckGet("ID_TK").ToInt();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "SetDrawingComplexity");

            q.Request.SetParam("ID_TK", id.ToString());
            q.Request.SetParam("DRAWING_COMPLEXITY", lvl.ToString());

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
                    else
                    {
                        var d2 = new DialogWindow("Ошибка обновления уровня сложности!", "Веб-техкарты", "", DialogWindowButtons.OK);
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

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


        /// <summary>
        /// Подтверждение выполнения чертежа
        /// </summary>
        private async void BindDrawing()
        {
            int id = SelectedItem.CheckGet("ID_TK").ToInt();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "UpdateDrawing");

            q.Request.SetParam("ID_TK", id.ToString());
            q.Request.SetParam("DRAWING", "2");

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
                    else
                    {
                        var d2 = new DialogWindow("Ошибка обновления статуса чертежа!", "Веб-техкарты", "", DialogWindowButtons.OK);
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        #endregion


        #region "Чертёж"

        /// <summary>
        /// Привязать чертёж
        /// </summary>
        private void AttachDrawing()
        {
            string directory = Central.GetStorageNetworkPathByCode("techcards_drawing");
            string selectedFile = "";
            string newFilePath = "";
            OpenFileDialog openFileDialog = new OpenFileDialog();
            bool resume = true;
            try
            {
                openFileDialog.Filter = "JPEG Files|*.jpg;*.jpeg";
                openFileDialog.Title = "Выберите JPEG файл";

                var directoryCust = Path.Combine(directory, Grid.SelectedItem.CheckGet("CUSTOMER_SHORT"));
                if (Directory.Exists(directoryCust))
                {
                    directory = directoryCust;
                }
                openFileDialog.InitialDirectory = directory;

                var fdResult = (bool)openFileDialog.ShowDialog();
                if (fdResult)
                {
                    selectedFile = openFileDialog.FileName;

                }
                newFilePath = selectedFile;
            }
            catch (Exception ex)
            {
                resume = false;
                var dw = new DialogWindow($"Ошибка сохранения файла. {ex.Message}", "Привязка файла чертежа", "", DialogWindowButtons.OK);
                dw.ShowDialog();
            }
            if (resume)
            {
                UpdateDrawing(Grid.SelectedItem.CheckGet("ID_TK").ToInt(), newFilePath);
            }
        }

        /// <summary>
        /// Обновление файла в БД
        /// </summary>
        private async void UpdateDrawing(int id, string fileName="")
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "SetDrawingFile");
            q.Request.SetParam("ID_TK", id.ToString());
            q.Request.SetParam("FILE_NAME", fileName);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Grid.LoadItems();
            }
            else 
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Открыть папку чертежа
        /// </summary>
        private void OpenDrawingFolder(string file)
        {
            if (File.Exists(file))
            {
                // Открываем папку и выделяем файл
                Process.Start("explorer.exe", $"/select,\"{file}\"");
            }
            else if (Directory.Exists(file))
            {
                // Если передан путь к папке, просто открываем её
                Process.Start("explorer.exe", $"\"{file}\"");
            }
            else
            {
                string directory = Central.GetStorageNetworkPathByCode("techcards_drawing");
                var directoryCust = Path.Combine(directory, Grid.SelectedItem.CheckGet("CUSTOMER_SHORT"));
                if (Directory.Exists(directoryCust))
                {
                    Process.Start("explorer.exe", $"\"{directoryCust}\"");
                }
                else
                {
                    Process.Start("explorer.exe", $"\"{directory}\"");
                }
            }
        }


        /// <summary>
        /// Заказать образец
        /// </summary>
        private async void OrderSample(int id, string pokupatel)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "AddSample");
            q.Request.SetParam("ID_TK", id.ToString());
            q.Request.SetParam("NAME_POK", pokupatel);

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
                    var dataSet = ListDataSet.Create(result, "ITEMS");
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        id = dataSet.Items.First().CheckGet("INSERT_ID").ToInt();

                        var dw = new DialogWindow($"Образец №{id} успешно заказан.", "Заказ образца", "", DialogWindowButtons.OK);
                        dw.ShowDialog();
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }
        #endregion

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
               

                fraimeFiles.ReturnTabName = TabName;
                fraimeFiles.Show();
            }
        }
        #endregion

        #region "Примечания"
        /// <summary>
        /// Открытие окна с примечаниями по веб-техкарте
        /// </summary>
        /// <param name="chatType">Тип чата: 0 - чат с клиентом, 1 - чат с коллегами</param>
        private void OpenNotes()
        {
            if (SelectedItem != null)
            {
                var noteFrame = new WebTechnologicalMapComments();
                noteFrame.TkId = SelectedItem.CheckGet("ID_TK").ToInt();
                noteFrame.ReceiverName = ControlName;
                noteFrame.ObjectType = 2;

                noteFrame.Show();
            }
        }
        #endregion

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

        /// <summary>
        /// Удаление техкарты
        /// </summary>
        /// <param name="id"></param>
        private async void DeleteTechCard(int id)
        {
            var dw = new DialogWindow("Вы действительно хотите удалить техкарту?", "Удаление", "", DialogWindowButtons.NoYes);
            if ((bool)dw.ShowDialog())
            {
                if (dw.ResultButton == DialogResultButton.Yes)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "MoldedContainer");
                    q.Request.SetParam("Action", "DeleteTechCard");
                    q.Request.SetParam("ID", id.ToString());

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                        if (result.ContainsKey("ITEMS"))
                        {
                            Grid.LoadItems();
                        }
                    }
                    else if (q.Answer.Error.Code == 145)
                    {
                        q.ProcessError();
                    }
                }
            }
        }

        /// <summary>
        /// Отправка ТК на подтверждение клиенту
        /// </summary>
        /// <param name="p"></param>
        public async void TkSendToConfirm()
        {
            Dictionary<string, string> p = new Dictionary<string, string>();

            p.CheckAdd("ID", Grid.SelectedItem.CheckGet("ID"));

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "SetConfirmStatus");
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
                    Grid.LoadItems();
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Открытие формы с похожими техкартами
        /// </summary>
        private void ShowDuplicate()
        {
            if (SelectedItem != null)
            {
                var fraimeFiles = new WebTechnologicalMapDuplicateForConstructor();

                fraimeFiles.L = Grid.SelectedItem.CheckGet("L").ToInt();
                fraimeFiles.B = Grid.SelectedItem.CheckGet("B").ToInt();
                fraimeFiles.H = Grid.SelectedItem.CheckGet("H").ToInt();
                fraimeFiles.IdPclass = Grid.SelectedItem.CheckGet("ID_PCLASS").ToInt();

                fraimeFiles.Show();
            }
        }


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

        private void ShowAllCheckBox_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Grid.LoadItems();
        }


        private void ConstructorSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
        }

        private void DtFrom_EditValueChanging(object sender, DevExpress.Xpf.Editors.EditValueChangingEventArgs e)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
        }

        private void DtTo_EditValueChanging(object sender, DevExpress.Xpf.Editors.EditValueChangingEventArgs e)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
        }
    }
}
