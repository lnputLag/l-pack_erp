using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Service.Printing;
using DevExpress.Data.Filtering.Helpers;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Excel;
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
using Application = Microsoft.Office.Interop.Excel.Application;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс веб-техкарт (Дизайнеры)
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class DesignerWebTechnologicalMapTab : ControlBase
    {
        public DesignerWebTechnologicalMapTab()
        {
            InitializeComponent();

            RoleName = "[erp]designer_web_tech_map";
            ControlTitle = "Веб-техкарты (дизайнеры)";
            DocumentationUrl = "/";
            TabName = "DesignerWebTechMap";


            OnLoad = () =>
            {
                FormInit();
                LoadDesigners();
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
                            ShowButton.Style = (System.Windows.Style)ShowButton.TryFindResource("Button");
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
                }


                Commander.SetCurrentGroup("custom");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "show_tk",
                        Enabled = true,
                        Title = "Показать ТК",
                        Description = "Показать Excel файл техкарты",
                        ButtonUse = true,
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
                        Name = "show_duplicate",
                        Title = "Показать похожие техкарты",
                        MenuUse = true,
                        Enabled = true,
                        Description = "Показать похожие техкарты",
                        Action = () =>
                        {
                            ShowDuplicate();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("DUPLICATE").ToInt().ContainsIn(1, 2, 3))
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "open_comments",
                        Enabled = true,
                        Title = "Открыть примечания по заявке",
                        MenuUse = true,
                        Action = () =>
                        {
                            OpenNotes();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "hidden_false",
                        MenuGroupHeader = "hidden",
                        MenuGroupHeaderName = "Видимость",
                        Enabled = true,
                        Title = "Показать",
                        Description = "Отображать техкарту",
                        ButtonUse = false,
                        MenuUse = true,
                        Action = () =>
                        {
                            SetHiddenFlag(0);
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("DESIGNER_HIDDEN_FLAG").ToInt() == 1)
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "hidden_true",
                        MenuGroupHeader = "hidden",
                        MenuGroupHeaderName = "Видимость",
                        Enabled = true,
                        Title = "Скрыть",
                        Description = "Скрыть техкарту",
                        ButtonUse = false,
                        MenuUse = true,
                        Action = () =>
                        {
                            SetHiddenFlag(1);
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("DESIGNER_HIDDEN_FLAG").ToInt() == 0)
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
                }

                Commander.SetCurrentGroup("cliche");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "cliche_true",
                        MenuGroupHeader = "cliche",
                        MenuGroupHeaderName = "Клише клиента",
                        Enabled = true,
                        Title = "Да",
                        Description = "Установить признак того, что клише клиента",
                        ButtonUse = false,
                        MenuUse = true,
                        Action = () =>
                        {
                            SetClicheFlag(1);
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("CLICHE_CUSTOMER_FLAG").ToInt() == 0
                                && row.CheckGet("ID_DES").ToInt() == Central.User.EmployeeId.ToInt())
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "cliche_false",
                        MenuGroupHeader = "cliche",
                        MenuGroupHeaderName = "Клише клиента",
                        Enabled = true,
                        Title = "Нет",
                        Description = "Убрать признак того, что клише клиента",
                        ButtonUse = false,
                        MenuUse = true,
                        Action = () =>
                        {
                            SetClicheFlag(0);
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("CLICHE_CUSTOMER_FLAG").ToInt() == 1
                                && row.CheckGet("ID_DES").ToInt() == Central.User.EmployeeId.ToInt())
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "overlay_color_true",
                        MenuGroupHeader = "overlay_color",
                        MenuGroupHeaderName = "Наложение цветов",
                        Enabled = true,
                        Title = "Есть",
                        Description = "Установить признак того, что есть наложение цветов",
                        ButtonUse = false,
                        MenuUse = true,
                        Action = () =>
                        {
                            SetOverlayColorFlag(1);
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("overlay_color_flag").ToInt() == 0
                                && row.CheckGet("ID_DES").ToInt() == Central.User.EmployeeId.ToInt())
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "overlay_color_false",
                        MenuGroupHeader = "overlay_color",
                        MenuGroupHeaderName = "Наложение цветов",
                        Enabled = true,
                        Title = "Нет",
                        Description = "Убрать признак того, что есть наложение цветов",
                        ButtonUse = false,
                        MenuUse = true,
                        Action = () =>
                        {
                            SetOverlayColorFlag(0);
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("overlay_color_flag").ToInt() == 1
                                && row.CheckGet("ID_DES").ToInt() == Central.User.EmployeeId.ToInt())
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "scorer_print_true",
                        MenuGroupHeader = "scorer_print",
                        MenuGroupHeaderName = "Печать по рилевке",
                        Enabled = true,
                        Title = "Да",
                        Description = "Установить признак печать по рилевке",
                        ButtonUse = false,
                        MenuUse = true,
                        Action = () =>
                        {
                            SetScorerPrintFlag(1);
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("SCORER_PRINT_FLAG").ToInt() == 0
                                && row.CheckGet("ID_DES").ToInt() == Central.User.EmployeeId.ToInt()
                                && row.CheckGet("ID_PCLASS").ToInt().ContainsIn(107, 108, 109, 114, 115, 116, 113, 110, 111, 112, 121, 218, 219, 220, 215, 17, 105, 106, 2, 3, 4, 214, 122, 217))
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "scorer_print_false",
                        MenuGroupHeader = "scorer_print",
                        MenuGroupHeaderName = "Печать по рилевке",
                        Enabled = true,
                        Title = "Нет",
                        Description = "Убрать признак печать по рилевке",
                        ButtonUse = false,
                        MenuUse = true,
                        Action = () =>
                        {
                            SetScorerPrintFlag(0);
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("SCORER_PRINT_FLAG").ToInt() == 1
                                && row.CheckGet("ID_DES").ToInt() == Central.User.EmployeeId.ToInt())
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                }

                Commander.SetCurrentGroup("old_tk");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "add_old_tk",
                        Title = "Привязать старую техкарту",
                        MenuUse = true,
                        Enabled = true,
                        Description = "Привязать старую техкарту",
                        Action = () =>
                        {
                            ShowPinOldTk();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            var path = row.CheckGet("PATHTK");

                            var folder_new = row.CheckGet("PATH_NEW");
                            var folder_work = row.CheckGet("PATH_WORK");
                            var folder_archive = row.CheckGet("PATH_ARCHIVE");

                            if (row.CheckGet("ID_TK").ToInt() > 0)
                            {
                                if (File.Exists(Path.Combine(folder_work, path)))
                                {
                                    PathTk = Path.Combine(folder_work, path);
                                    result = true;
                                }
                                else if (File.Exists(Path.Combine(folder_archive, path)))
                                {
                                    PathTk = Path.Combine(folder_archive, path);
                                    result = true;
                                }
                                else if (File.Exists(Path.Combine(folder_new, path)))
                                {
                                    PathTk = Path.Combine(folder_new, path);
                                    result = true;
                                }
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "del_old_tk",
                        Title = "Отвязать старую техкарту",
                        MenuUse = true,
                        Enabled = true,
                        Description = "Отвязать старую техкарту",
                        Action = () =>
                        {
                            UnpinOldTk();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;

                            if (!row.CheckGet("ART_OLD").IsNullOrEmpty())
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                }


                Commander.SetCurrentGroup("custom2");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "lvl1",
                        MenuGroupHeader = "difficulty_level",
                        MenuGroupHeaderName = "Установить уровень сложности",
                        Enabled = true,
                        Title = "1 - Низкий",
                        Description = "Установить низкий уровень сложности",
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
                        MenuGroupHeaderName = "Установить уровень сложности",
                        Enabled = true,
                        Title = "2 - Средний",
                        Description = "Установить средний уровень сложности",
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
                        MenuGroupHeaderName = "Установить уровень сложности",
                        Enabled = true,
                        Title = "3 - Высокий",
                        Description = "Установить высокий уровень сложности",
                        ButtonUse = false,
                        MenuUse = true,
                        Action = () =>
                        {
                            SetDifficultyLevel(3);
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "open_design_comments",
                        Enabled = true,
                        Title = "Открыть примечания дизайнера",
                        MenuUse = true,
                        Action = () =>
                        {
                            OpenDesignNotes();
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
                            if (row.CheckGet("STATUS_FOR_DESIGNER").ToInt().ContainsIn(2, 3))
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                }

                Commander.SetCurrentGroup("operations");
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
                            TechMapChangeDesignerId(0);
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("STATUS_FOR_DESIGNER").ToInt().ContainsIn(1, 2) && row.CheckGet("ID_DES").ToInt() != Central.User.EmployeeId.ToInt())
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
                            TechMapChangeDesignerId(1);
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("ID_DES").ToInt() == Central.User.EmployeeId.ToInt())
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
                        Title = "Подтвердить",
                        Description = "Подтвердить выполнение дизайна",
                        ButtonUse = true,
                        ButtonName = "ConfirmDesignButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            var dw = new DialogWindow("Вы действительно хотите отметить разработку дизайна?", "Разработка дизайна техкарты", "", DialogWindowButtons.NoYes);
                            if ((bool)dw.ShowDialog())
                            {
                                var dw2 = new DialogWindow("Вы проверили печать по рилевкам?", "Разработка дизайна техкарты", "", DialogWindowButtons.NoYes);
                                if ((bool)dw2.ShowDialog())
                                {
                                    BindDesign();
                                }
                            }

                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("STATUS_FOR_DESIGNER").ToInt() == 2
                                && row.CheckGet("ID_DES").ToInt() == Central.User.EmployeeId.ToInt())
                            {
                                result = true;
                            }
                            return result;
                        },
                    });
                    //Видимость


                    // Клише клиента


                    // Наложение цветов


                    // Печать по рилевке

                }


                Commander.Init(this);
            }
        }

        /// <summary>
        /// Название вкладки
        /// </summary>
        public string TabName;
        public string PathTk { get; set; }

        /// <summary>
        /// Таймер заполнения поля фильтров
        /// </summary>
        public DispatcherTimer TemplateTimeoutTimer;

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// Выбранный ИД записи
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Загрузка дизайнеров
        /// </summary>
        public async void LoadDesigners()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "ListDesigners");

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
                        if (item["ID_DES"].ToInt() != 0)
                        {
                            designers.CheckAdd(item["ID_DES"].ToInt().ToString(), item.CheckGet("DES_NAME"));

                        }
                    }
                    DesignerSelectBox.Items = designers;
                    var empl_id = Central.User.EmployeeId;
                    if (DesignerSelectBox.Items.ContainsKey(empl_id.ToString()))
                    {
                        DesignerSelectBox.SetSelectedItemByKey(empl_id.ToString());
                    }
                    else
                    {
                        DesignerSelectBox.SetSelectedItemByKey("-1");
                    }

                }
            }
            else
            {
                q.ProcessError();
            }
            Grid.HideSplash();
            Grid.Toolbar.IsEnabled = true;
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
                    DxEnableColumnSorting = false,
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
                    Header="Покупатель",
                    Path="NAME_POK",
                    ColumnType=ColumnTypeRef.String,
                    Width2=18,
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
                    Width2=11,
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
                    Header="Цвет 1",
                    Path="COLOR1",
                    ColumnType=ColumnTypeRef.String,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Цвет 2",
                    Path="COLOR2",
                    ColumnType=ColumnTypeRef.String,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Цвет 3",
                    Path="COLOR3",
                    ColumnType=ColumnTypeRef.String,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Цвет 4",
                    Path="COLOR4",
                    ColumnType=ColumnTypeRef.String,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Цвет 5",
                    Path="COLOR5",
                    ColumnType=ColumnTypeRef.String,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Клише клиента",
                    Path="CLICHE_CUSTOMER_FLAG",
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
                    Width2=2,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Внутренние сообщения",
                    Path="FLAG_CHAT_INNER",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=2,
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
                    Header="Комментарий клиента",
                    Path="COMMENTS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=25,
                    Options="valuestripbr",
                },
                new DataGridHelperColumn
                {
                    Header="Примечание дизайнера",
                    Path="DESIGN_NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание ОПП",
                    Path="OPP_NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=25,
                },
                new DataGridHelperColumn
                {
                    Header="Старая ТК",
                    Path="ART_OLD",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Наложение цвета",
                    Path="OVERLAY_COLOR_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="Печать по рилёвке",
                    Path="SCORER_PRINT_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="Дизайнер",
                    Path="DES_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header="Конструктор",
                    Path="CON_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header="Иженер",
                    Path="ENG_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header= "Менеджер РЦ",
                    Path="PR_CAL_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header="Менеджер ОРК",
                    Path="MAN_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header="Менеджер ОП",
                    Path="SALES_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header="Площадка",
                    Path="PLACE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Ид дизайнера",
                    Path="ID_DES",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=4,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="STATUS_FOR_DESIGNER",
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
                    Header="Скрыта?",
                    Path="DESIGNER_HIDDEN_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=5,
                    Visible = false,
                },
                new DataGridHelperColumn
                {
                    Header="Статус ТК",
                    Path="TK_ORDER_STATUS",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                    Visible = false,
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
                    Header="Создано менеджером",
                    Path="MANAGER_CREATOR_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="ИД вида изделия",
                    Path="ID_PCLASS",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=11,
                    Visible = false,
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
                        currentStatus = row.CheckGet("STATUS_FOR_DESIGNER").ToInt();
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
                            default:
                                color = HColor.White;
                                break;
                        }

                        if (row.CheckGet("DESIGNER_HIDDEN_FLAG").ToBool())
                        {
                                color = HColor.Yellow;
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

                        if( row.CheckGet("STATUS_FOR_DESIGNER").ToInt()==0)
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

            Grid.OnLoadItems = LoadItems;
            Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    SelectedItem = selectedItem;
                }

            };

            Grid.ItemsAutoUpdate = true;
            Grid.AutoUpdateInterval = 120;

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
            q.Request.SetParam("Action", "ListForDesigners");
            p.Add("DES_EMPL_ID", DesignerSelectBox?.SelectedItem.Key.ToString());
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
        /// Изменение ИД дизайнера
        /// FLAG_DEL: 0 - установить себя, 1 - удалить себя
        /// </summary>
        private async void TechMapChangeDesignerId(int type)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "SetDesigner");
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
            q.Request.SetParam("Action", "SetDesignComplexity");

            q.Request.SetParam("ID_TK", id.ToString());
            q.Request.SetParam("DESIGN_COMPLEXITY", lvl.ToString());

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
        /// Подтверждение выполнения дизайна
        /// </summary>
        private async void BindDesign()
        {
            int id = SelectedItem.CheckGet("ID_TK").ToInt();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "UpdateDesign");

            q.Request.SetParam("ID_TK", id.ToString());
            q.Request.SetParam("DESIGN", "2");

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
                        var d2 = new DialogWindow("Ошибка обновления статуса дизайна!", "Веб-техкарты", "", DialogWindowButtons.OK);
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

        /// <summary>
        /// Открытие формы привязывания старой техкарты
        /// </summary>
        private void ShowPinOldTk()
        {
            if (SelectedItem != null)
            {
                var fraime= new WebTechnologicalMapPinOldTk();
                fraime.TkId = SelectedItem.CheckGet("ID_TK").ToInt();
                fraime.PathTk = PathTk;

                fraime.Show();
                Grid.LoadItems();
            }
        }

        /// <summary>
        /// Отвязать старую ТК
        /// </summary>
        private async void UnpinOldTk()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "UpdateTkOld");
            q.Request.SetParam("ID_TK", Grid.SelectedItem.CheckGet("ID_TK").ToString());
            q.Request.SetParam("ID_TK_OLD", "");
            q.Request.SetParam("NOTE", "");

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    Application excelApp = null;
                    Workbook workbook = null;
                    Worksheet worksheet = null;
                    try
                    {
                        excelApp = new Application();
                        workbook = excelApp.Workbooks.Open(PathTk);
                        worksheet = workbook.Worksheets[1];

                        worksheet.Range["A36"].Value2 = "";
                        workbook.Save();
                        workbook.Close(true);
                        var dw = new DialogWindow("Старая техкарта успешно отвязана", "Отвязать старую техкарту", "", DialogWindowButtons.OK);
                        dw.ShowDialog();
                    }
                    catch (Exception e)
                    {
                        var dw = new DialogWindow($"Ошибка обновления файла: {e.Message}", "Отвязать старую техкарту", "", DialogWindowButtons.OK);
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
                Grid.LoadItems();
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Открытие вкладки с похожими техкартами
        /// </summary>
        private void ShowDuplicate()
        {
            if (SelectedItem != null)
            {
                var fraimeFiles = new WebTechnologicalMapDuplicate();
                fraimeFiles.TkId = SelectedItem.CheckGet("ID_TK").ToInt();

                fraimeFiles.Show();
            }
        }

        #region "Примечания"
        /// <summary>
        /// Открытие окна с примечаниями по веб-техкарте
        /// </summary>
        private void OpenNotes()
        {
            if (SelectedItem != null)
            {
                var noteFrame = new WebTechnologicalMapComments();
                noteFrame.TkId = SelectedItem.CheckGet("ID_TK").ToInt();
                noteFrame.ReceiverName = ControlName;
                noteFrame.ObjectType = 1;
                noteFrame.Show();
            }
        }

        /// <summary>
        /// Открытие окна с примечаниями дизайнера по веб-техкарте
        /// </summary>
        private void OpenDesignNotes()
        {
            if (SelectedItem != null)
            {
                var noteFrame = new WebTechnologicalMapDesignerComment();
                noteFrame.TkId = SelectedItem.CheckGet("ID_TK").ToInt();
                noteFrame.ReceiverName = ControlName;

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

        #region "Действия над ТК"

        /// <summary>
        /// Установить флаг видимости ТК
        /// </summary>
        private async void SetHiddenFlag(int hidden_flag)
        {
            int id = Grid.SelectedItem.CheckGet("ID_TK").ToInt();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "SetHiddenFlag");

            q.Request.SetParam("ID_TK", id.ToString());
            q.Request.SetParam("DESIGN_HIDDEN", hidden_flag.ToString());

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
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Установить флаг клише клиента
        /// </summary>
        private async void SetClicheFlag(int flag)
        {
            int id = Grid.SelectedItem.CheckGet("ID_TK").ToInt();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "SetClicheCustomerFlag");

            q.Request.SetParam("ID_TK", id.ToString());
            q.Request.SetParam("FLAG", flag.ToString());

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
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Установить флаг наложения цветов
        /// </summary>
        private async void SetOverlayColorFlag(int flag)
        {
            int id = Grid.SelectedItem.CheckGet("ID_TK").ToInt();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "SetOverlayColorFlag");

            q.Request.SetParam("ID_TK", id.ToString());
            q.Request.SetParam("FLAG", flag.ToString());

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
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Установить флаг печать по рилевке
        /// </summary>
        private async void SetScorerPrintFlag(int flag)
        {
            int id = Grid.SelectedItem.CheckGet("ID_TK").ToInt();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "SetScorerPrintFlag");

            q.Request.SetParam("ID_TK", id.ToString());
            q.Request.SetParam("FLAG", flag.ToString());

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
            else
            {
                q.ProcessError();
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
                MaskedTextBox.Dispatcher.BeginInvoke(new System.Action(() =>
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

        private void DesignerSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void DtFrom_EditValueChanging(object sender, DevExpress.Xpf.Editors.EditValueChangingEventArgs e)
        {
            ShowButton.Style = (System.Windows.Style)ShowButton.TryFindResource("FButtonPrimary");
        }

        private void DtTo_EditValueChanging(object sender, DevExpress.Xpf.Editors.EditValueChangingEventArgs e)
        {
            ShowButton.Style = (System.Windows.Style)ShowButton.TryFindResource("FButtonPrimary");
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
            if (TemplateTimeoutTimer == null || TemplateTimeoutTimer?.IsEnabled == false)
            {
                if (ShowAllCheckBox.IsChecked == true)
                {
                    if (!SizeLength.Text.IsNullOrEmpty() || !SizeWidth.Text.IsNullOrEmpty() || !SizeHeigth.Text.IsNullOrEmpty() || (!SearchTextBox.Text.IsNullOrEmpty() && SearchTextBox.Text.Length > 2))
                    {
                        RunTemplateTimeoutTimer();
                    }
                }
                else
                {
                    RunTemplateTimeoutTimer();
                }
            }
        }

        private void TextBoxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(TemplateTimeoutTimer == null || TemplateTimeoutTimer?.IsEnabled == false)
            {
                if (ShowAllCheckBox.IsChecked == true)
                {
                    if(!SizeLength.Text.IsNullOrEmpty() || !SizeWidth.Text.IsNullOrEmpty() || !SizeHeigth.Text.IsNullOrEmpty() || (!SearchTextBox.Text.IsNullOrEmpty() && SearchTextBox.Text.Length > 2))
                    {
                        RunTemplateTimeoutTimer();
                    }
                }
                else
                {
                    RunTemplateTimeoutTimer();
                }
            }
        }
    }
}
