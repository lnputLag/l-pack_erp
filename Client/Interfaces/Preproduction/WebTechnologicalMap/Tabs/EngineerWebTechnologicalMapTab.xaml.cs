using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Service.Printing;
using DevExpress.Data.Filtering.Helpers;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using DevExpress.XtraReports.UI;
using Microsoft.Win32;
using NCalc;
using Newtonsoft.Json;
using NPOI.POIFS.Crypt.Dsig;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;
using Excel = Microsoft.Office.Interop.Excel;
using System.Windows.Threading;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Excel;
using Application = Microsoft.Office.Interop.Excel.Application;
using Style = System.Windows.Style;
using Action = System.Action;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс веб-техкарт (Инженеры)
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class EngineerWebTechnologicalMapTab : ControlBase
    {
        public EngineerWebTechnologicalMapTab()
        {
            InitializeComponent();

            RoleName = "[erp]engineer_web_tech_map";
            ControlTitle = "Веб-техкарты (инженеры)";
            DocumentationUrl = "/";

            OnLoad = () =>
            {
                FormInit();
                SetDefaults();
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
                            case "UpdateStatusDesignOrDrawing":
                                if (msg.Message.ToInt()==1)
                                {
                                    SetDesign(1);
                                }
                                else if (msg.Message.ToInt() == 2)
                                {
                                    SetDrawing(1);
                                }
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

                Commander.SetCurrentGroup("operations");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "accept",
                        Title = "Принять",
                        ButtonUse = true,
                        ButtonName = "AcceptButton",
                        Description = "Принять заявку на техкарту",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            if (SelectedIds != null && SelectedIds.Count > 0)
                            {
                                var dw = new DialogWindow("Вы действительно хотите принять заявку на техкарту?", "Принятие заявки на техкарту", "", DialogWindowButtons.NoYes);
                                if ((bool)dw.ShowDialog())
                                {
                                    foreach (var id in SelectedIds)
                                    {
                                        var tmp_row = Grid.Items.FirstOrDefault(x => x.CheckGet("ID_TK") == id.ToString());

                                        TechMapChangeOrderStatus(tmp_row.CheckGet("ID_TK").ToInt(), 1, 0, 1, 0);

                                        Grid.LoadItems();
                                    }
                                }
                            }
                            else
                            {
                                var dw = new DialogWindow("Вы действительно хотите принять заявку на техкарту?", "Принятие заявки на техкарту", "", DialogWindowButtons.NoYes);
                                if ((bool)dw.ShowDialog())
                                {
                                    TechMapChangeOrderStatus(Grid.SelectedItem.CheckGet("ID_TK").ToInt(), 1, 0, 1, 0);

                                    Grid.LoadItems();
                                }
                            }
                            
                        },
                        CheckEnabled = () =>
                        {
                            var result = true;
                            var row = Grid.SelectedItem;
                            if (SelectedIds!=null && SelectedIds.Count > 0)
                            {
                                foreach(var id in SelectedIds)
                                {
                                    var tmp_row = Grid.Items.FirstOrDefault(x => x.CheckGet("ID_TK") == id.ToString());

                                    if (tmp_row!=null && !tmp_row.CheckGet("STATUS").ToInt().ContainsIn(0, 5))
                                    {
                                        result = false;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                if (!row.CheckGet("STATUS").ToInt().ContainsIn(0, 5))
                                {
                                    result = false;
                                }
                            }
                            
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "reject",
                        Title = "Отменить",
                        ButtonUse = true,
                        ButtonName = "RejectButton",
                        Description = "Отменить заявку на техкарту",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            if (SelectedIds != null && SelectedIds.Count > 0)
                            {
                                var dw = new DialogWindow("Вы действительно хотите отменить заявки на техкарты?", "Отмена заявок на техкарты", "", DialogWindowButtons.NoYes);
                                if ((bool)dw.ShowDialog())
                                {
                                    foreach (var id in SelectedIds)
                                    {
                                        var tmp_row = Grid.Items.FirstOrDefault(x => x.CheckGet("ID_TK") == id.ToString());

                                        TechMapChangeOrderStatus(tmp_row.CheckGet("ID_TK").ToInt(), 2, 0, 0, 1);

                                        Grid.LoadItems();
                                    }
                                }
                            }
                            else
                            {
                                var dw = new DialogWindow("Вы действительно хотите отменить заявку на техкарту?", "Отмена заявки на техкарту", "", DialogWindowButtons.NoYes);
                                if ((bool)dw.ShowDialog())
                                {
                                    TechMapChangeOrderStatus(Grid.SelectedItem.CheckGet("ID_TK").ToInt(), 2, 0, 0, 1);

                                    Grid.LoadItems();
                                }
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = true;
                            var row = Grid.SelectedItem;
                            if (SelectedIds != null && SelectedIds.Count > 0)
                            {
                                foreach (var id in SelectedIds)
                                {
                                    var tmp_row = Grid.Items.FirstOrDefault(x => x.CheckGet("ID_TK") == id.ToString());

                                    if (tmp_row != null && !tmp_row.CheckGet("STATUS").ToInt().ContainsIn(0, 1, 3, 5, 6, 7))
                                    {
                                        result = false;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                if (!row.CheckGet("STATUS").ToInt().ContainsIn(0, 1, 3, 5, 6, 7))
                                {
                                    result = false;
                                }
                            }
                            return result;
                        }
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "accept_with_client",
                        Title = "Согласовать с клиентом",
                        MenuUse = true,
                        ButtonUse = false,
                        Description = "Согласовать техкарту с клиентом",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var dw = new DialogWindow("Вы действительно хотите согласовать техкарту с клиентом?", "Согласование техкарты с клиентом", "", DialogWindowButtons.NoYes);
                            if ((bool)dw.ShowDialog())
                            {
                                TechMapChangeOrderStatus(Grid.SelectedItem.CheckGet("ID_TK").ToInt(), 3, 1, 0, 0);

                                var row = Grid.SelectedItem;
                                var path = row.CheckGet("PATHTK");

                                var folder_new = row.CheckGet("PATHTK_NEW");

                                if (File.Exists(Path.Combine(folder_new, path)))
                                {
                                    ConvertExcelToPdf(Path.Combine(folder_new, path));
                                }
                                
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var file_exist = false;

                            var row = Grid.SelectedItem;
                            var path = row.CheckGet("PATHTK");

                            var folder_new = row.CheckGet("PATHTK_NEW");
                            if (path!=null && File.Exists(Path.Combine(folder_new, path)))
                            {
                                file_exist = true;
                            }

                            if (row.CheckGet("STATUS").ToInt() == 1
                                && row.CheckGet("DESIGN_STATUS").ToInt().ContainsIn(0, 2)
                                && row.CheckGet("DRAWING_STATUS").ToInt().ContainsIn(0, 2)
                                && file_exist)
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "reject_with_client",
                        Title = "Отменить согласование с клиентом",
                        MenuUse = true,
                        ButtonUse = false,
                        Description = "Согласовать техкарту с клиентом",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var dw = new DialogWindow("Вы действительно хотите отменить согласование техкарты с клиентом?", "Отмена согласования техкарты с клиентом", "", DialogWindowButtons.NoYes);
                            if ((bool)dw.ShowDialog())
                            {
                                TechMapChangeOrderStatus(Grid.SelectedItem.CheckGet("ID_TK").ToInt(), 1, 0, 0, 0);
                                RejectAgreedTk();
                                if (!Grid.SelectedItem.CheckGet("FILE_SIGNED").IsNullOrEmpty())
                                {
                                    var file_name = Grid.SelectedItem.CheckGet("FILE_SIGNED");
                                    UpdateFileSigned();

                                    string storagePath = Central.GetStorageNetworkPathByCode("techcard_signed");
                                    string filePath = Path.Combine(storagePath, file_name);

                                    if (File.Exists(filePath))
                                    {
                                        var dw2 = new DialogWindow("Удалить файл подписанной техкарты?", "Удаление подписанной техкарты", "", DialogWindowButtons.NoYes);
                                        if ((bool)dw2.ShowDialog())
                                        {
                                            File.Delete(filePath);
                                        }
                                    }
                                }
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;

                            if (row.CheckGet("STATUS").ToInt().ContainsIn(3))
                            {
                                result = true;
                            }
                            return result;
                        }
                    });


                    Commander.Add(new CommandItem()
                    {
                        Name = "publish",
                        Title = "Опубликовать техкарту",
                        MenuUse = true,
                        ButtonUse = false,
                        Description = "Опубликовать техкарту",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var dw = new DialogWindow("Вы действительно хотите опубликовать техкарту?", "Публикация техкарты", "", DialogWindowButtons.NoYes);
                            if ((bool)dw.ShowDialog())
                            {
                                TechMapChangeOrderStatus(Grid.SelectedItem.CheckGet("ID_TK").ToInt(), 4, 0, 0, 0);
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;

                            if (row.CheckGet("STATUS").ToInt() == 7
                                && !row.CheckGet("ART").IsNullOrEmpty())
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "unpublish",
                        Title = "Отменить публикацию",
                        MenuUse = true,
                        ButtonUse = false,
                        Description = "Отменить публикацию техкарты",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var dw = new DialogWindow("Вы действительно хотите отменить публикацию техкарты?", "Отмена публикации техкарты", "", DialogWindowButtons.NoYes);
                            if ((bool)dw.ShowDialog())
                            {
                                TechMapChangeOrderStatus(Grid.SelectedItem.CheckGet("ID_TK").ToInt(), 3, 0, 0, 0);
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;

                            if (row.CheckGet("STATUS").ToInt() == 4)
                            {
                                result = true;
                            }
                            return result;
                        }
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
                            if (row.CheckGet("STATUS").ToInt().ContainsIn(1, 3, 7))
                            {
                                result = true;
                            }
                            return result;
                        }
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
                            if (row.CheckGet("HIDDEN_FLAG").ToInt() == 1)
                            {
                                result = true;
                            }
                            return result;
                        },
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
                            if (row.CheckGet("HIDDEN_FLAG").ToInt() == 0)
                            {
                                result = true;
                            }
                            return result;
                        }
                    });


                    Commander.Add(new CommandItem()
                    {
                        Name = "cancel_design",
                        Enabled = true,
                        Title = "Вернуть в работу",
                        Description = "Вернуть в работу техкарту с ожиданием ответа",
                        ButtonUse = false,
                        MenuUse = true,
                        Action = () =>
                        {
                            SetlWaiting(0);
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("STATUS").ToInt() == 6)
                            {
                                result = true;
                            }
                            return result;
                        },
                    });
                }

                Commander.SetCurrentGroup("operations2");
                {

                    Commander.Add(new CommandItem()
                    {
                        Name = "send_to_designer",
                        Enabled = true,
                        Title = "Отправить дизайнеру",
                        Description = "Отправить техкарту дизайнеру",
                        ButtonUse = false,
                        MenuUse = true,
                        Action = () =>
                        {
                            var noteFrame = new WebTechnologicalMapAddNote();
                            noteFrame.ReceiverName = ControlName;
                            noteFrame.Type = 1;
                            noteFrame.TkId = Grid.SelectedItem.CheckGet("ID_TK").ToInt();
                            noteFrame.Show();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("DESIGN_STATUS").ToInt().ContainsIn(0, 2)
                                && row.CheckGet("STATUS").ToInt() == 1)
                            {
                                result = true;
                            }
                            return result;
                        },
                        CheckMenuVisible = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("DESIGN_STATUS").ToInt().ContainsIn(0, 1)
                                && !row.CheckGet("STATUS").ToInt().ContainsIn(0,5))
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "send_from_designer",
                        Enabled = true,
                        Title = "Вернуть от дизайнера",
                        Description = "Вернуть техкарту от дизайнера",
                        ButtonUse = false,
                        MenuUse = true,
                        Action = () =>
                        {
                            SetDesign(0);
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("DESIGN_STATUS").ToInt() == 1
                                && row.CheckGet("STATUS").ToInt() == 1)
                            {
                                result = true;
                            }
                            return result;
                        },
                        CheckMenuVisible = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("DESIGN_STATUS").ToInt().ContainsIn(0, 1)
                                && !row.CheckGet("STATUS").ToInt().ContainsIn(0, 5))
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "cancel_design",
                        Enabled = true,
                        Title = "Отменить выполнение дизайна",
                        Description = "Отменить выполнение дизайна техкарты",
                        ButtonUse = false,
                        MenuUse = true,
                        Action = () =>
                        {
                            SetDesign(0);
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("STATUS").ToInt() == 1)
                            {
                                result = true;
                            }
                            return result;
                        },
                        CheckMenuVisible = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("DESIGN_STATUS").ToInt() == 2
                                && !row.CheckGet("STATUS").ToInt().ContainsIn(0, 5))
                            {
                                result = true;
                            }
                            return result;
                        }
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "send_to_constructor",
                        Enabled = true,
                        Title = "Отправить конструктору",
                        Description = "Отправить техкарту конструктору",
                        ButtonUse = false,
                        MenuUse = true,
                        Action = () =>
                        {
                            var noteFrame = new WebTechnologicalMapAddNote();
                            noteFrame.ReceiverName = ControlName;
                            noteFrame.Type = 2;
                            noteFrame.TkId = Grid.SelectedItem.CheckGet("ID_TK").ToInt();
                            noteFrame.Show();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("DRAWING_STATUS").ToInt().ContainsIn(0, 2)
                                && row.CheckGet("STATUS").ToInt() == 1)
                            {
                                result = true;
                            }
                            return result;
                        },
                        CheckMenuVisible = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("DRAWING_STATUS").ToInt().ContainsIn(0, 1)
                                && !row.CheckGet("STATUS").ToInt().ContainsIn(0, 5))
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "send_from_constructor",
                        Enabled = true,
                        Title = "Вернуть от конструктора",
                        Description = "Вернуть техкарту от конструктора",
                        ButtonUse = false,
                        MenuUse = true,
                        Action = () =>
                        {
                            SetDrawing(0);
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("DRAWING_STATUS").ToInt() == 1
                                && row.CheckGet("STATUS").ToInt() == 1)
                            {
                                result = true;
                            }
                            return result;
                        },
                        CheckMenuVisible = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("DRAWING_STATUS").ToInt().ContainsIn(0, 1)
                                && !row.CheckGet("STATUS").ToInt().ContainsIn(0, 5))
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "cancel_drawing",
                        Enabled = true,
                        Title = "Отменить выполнение чертежа",
                        Description = "Отменить выполнение чертежа техкарты",
                        ButtonUse = false,
                        MenuUse = true,
                        Action = () =>
                        {
                            SetDrawing(0);
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("STATUS").ToInt() == 1)
                            {
                                result = true;
                            }
                            return result;
                        },
                        CheckMenuVisible = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("DRAWING_STATUS").ToInt() == 2
                                && !row.CheckGet("STATUS").ToInt().ContainsIn(0, 5))
                            {
                                result = true;
                            }
                            return result;
                        }
                    });

                }

                Commander.SetCurrentGroup("operations3");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "pin_tk",
                        Title = "Привязать техкарту",
                        MenuUse = true,
                        Enabled = true,
                        Description = "Привязать файл техкарты",
                        Action = () =>
                        {
                            ShowPinTk();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("ID_TK").ToInt() > 0
                                && row.CheckGet("STATUS").ToInt() == 1)
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
                        Description = "Показать похожие техкарты",
                        Action = () =>
                        {
                            ShowDuplicate();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("DUPLICATE").ToInt().ContainsIn(1,2,3))
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "change_user",
                        Title = "Изменить инженера",
                        MenuUse = true,
                        Enabled = true,
                        Description = "Изменить инженера для техкарты",
                        Action = () =>
                        {
                            ChangeNameUser();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("STATUS").ToInt() != 4)
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "change_fact_id_1",
                        Title = "Изменить площадку",
                        MenuUse = true,
                        Enabled = true,
                        Description = "Изменить площадку для техкарты",
                        Action = () =>
                        {
                            ChangeFactId();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("STATUS").ToInt() != 4)
                            {
                                result = true;
                            }
                            return result;
                        }
                    }); 
                    Commander.Add(new CommandItem()
                    {
                        Name = "show_pokupatel_contact",
                        Title = "Контакты покупателя",
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
                }

                Commander.SetCurrentGroup("file");
                {
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
                        Name = "show_tk",
                        Enabled = true,
                        Title = "Показать техкарту",
                        Description = "Показать Excel файл техкарты",
                        ButtonUse = true,
                        Group = "file",
                        ButtonName = "ShowFileTkButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            var row = Grid.SelectedItem;
                            var path = row.CheckGet("PATHTK");

                            var folder_new = row.CheckGet("PATHTK_NEW");
                            var folder_work = row.CheckGet("PATHTK_WORK");
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
                                    Console.WriteLine($"Ошибка: {ex.Message}");
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

                            var folder_new = row.CheckGet("PATHTK_NEW");
                            var folder_work = row.CheckGet("PATHTK_WORK");
                            var folder_archive = row.CheckGet("PATHTK_ARCHIVE");

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
                        Name = "open_drawing",
                        Enabled = true,
                        Title = "Показать чертеж",
                        Description = "Показать JPG файл чертежа",
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
                        Name = "show_tk_signed",
                        Enabled = true,
                        Title = "Подписанная техкарта",
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
                        Name = "show_history",
                        Enabled = true,
                        Title = "Показать историю",
                        Description = "Открыть историю по техкарте",
                        ButtonUse = false,
                        MenuUse = true,
                        Action = () =>
                        {
                            var historyFrame = new WebTechnologicalMapHistory();
                            historyFrame.ReceiverName = TabName;
                            historyFrame.IdTk = SelectedItem.CheckGet("ID_TK").ToInt();
                            historyFrame.Show();
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
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }


        /// <summary>
        /// Название вкладки
        /// </summary>
        public string TabName;

        /// <summary>
        /// Датасет таблицы
        /// </summary>
        public ListDataSet GridDS { get; set; }

        /// <summary>
        /// Список выбранных строк
        /// </summary>
        private List<int> SelectedIds { get; set; }

        /// <summary>
        /// Таймер заполнения поля фильтров
        /// </summary>
        public DispatcherTimer TemplateTimeoutTimer;
        #endregion



        #region "Загрузка справочников"
        public void SetDefaults()
        {
            SelectedIds = new List<int>();
            LoadRefs();
        }

        /// <summary>
        /// Загрузка инженеров
        /// </summary>
        public async void LoadRefs()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "LoadRefForListEngineers");

            q.DoQuery();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {

                    var ds_pok = ListDataSet.Create(result, "POKUPATEL");
                    var pok = new Dictionary<string, string>();
                    pok.CheckAdd("-1", "Все");
                    foreach (var item in ds_pok.Items)
                    {
                        if (item["ID"].ToInt() != 0)
                        {
                            pok.CheckAdd(item["ID"].ToInt().ToString(), item.CheckGet("NAME"));

                        }
                    }
                    PokupatelSelectBox.Items = pok;
                    PokupatelSelectBox.SetSelectedItemByKey("-1");

                    var ds_eng = ListDataSet.Create(result, "LIST_ENGINEERS");
                    var eng = new Dictionary<string, string>();
                    eng.CheckAdd("-1", "Все");
                    foreach (var item in ds_eng.Items)
                    {
                        if (item["ID"].ToInt() != 0)
                        {
                            eng.CheckAdd(item["ID"].ToInt().ToString(), item.CheckGet("NAME"));

                        }
                    }
                    EngineerSelectBox.Items = eng;
                    var empl_id = Central.User.EmployeeId;
                    if (EngineerSelectBox.Items.ContainsKey(empl_id.ToString()))
                    {
                        EngineerSelectBox.SetSelectedItemByKey(empl_id.ToString());
                    }
                    else
                    {
                        EngineerSelectBox.SetSelectedItemByKey("57");
                    }

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
            }
            else
            {
                q.ProcessError();
            }
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
                        OnChange=(FormHelperField f, string v)=>
                        {
                            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
                        },
                    },
                    new FormHelperField()
                    {
                        Path="SEARCH_WIDTH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=SizeWidth,
                        ControlType="TextBox",
                        OnChange=(FormHelperField f, string v)=>
                        {
                            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
                        },
                    },
                    new FormHelperField()
                    {
                        Path="SEARCH_HEIGTH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=SizeHeigth,
                        ControlType="TextBox",
                        OnChange=(FormHelperField f, string v)=>
                        {
                            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
                        },
                    },
                    new FormHelperField()
                    {
                        Path="SEARCH_TEXT",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=SearchTextBox,
                        ControlType="TextBox",
                        OnChange=(FormHelperField f, string v)=>
                        {
                            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
                        },
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
                    Header="*",
                    Path="_SELECTED",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 3,
                    Exportable = false,
                    Editable = true,
                    OnClickAction = (row, el) =>
                    {
                        int id = row.CheckGet("ID_TK").ToInt();
                        if (id>0)
                        {
                            bool selected = row.CheckGet("_SELECTED").ToBool();
                            if (selected)
                            {
                                if (SelectedIds.Contains(id))
                                {
                                    SelectedIds.Remove(id);
                                }
                            }
                            else
                            {
                                SelectedIds.Add(id);
                            }
                            Commander.UpdateActions();
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                },
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

                },
                new DataGridHelperColumn
                {
                    Header="Площадка",
                    Path="FACTORY",
                    ColumnType=ColumnTypeRef.String,
                    Width2=3,

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
                    Width2=24,

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
                                    && row.CheckGet("DUPLICATE").ToInt().ContainsIn(1,2))

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

                    Options="valuestripbr",
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
                    Header="Цвета",
                    Path="COLORS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=4,
                    Options="valuestripbr",

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
                    Header="Автосборка",
                    Path="AUTOMATIC",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=3,

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

                                if( row.CheckGet("SCORER_PRINT").ToInt()==1
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
                    Header="Согласовано клиентом",
                    Path="AGREED_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=3,

                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("AGREED_FLAG").ToBool())
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
                    Width2=3,

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
                    Header="Сложность дизайнера",
                    Path="DESIGN_COMPLEXITY",
                    ColumnType=ColumnTypeRef.String,
                    Width2=2,

                },
                new DataGridHelperColumn
                {
                    Header="Сложность конструктора",
                    Path="DRAWING_COMPLEXITY",
                    ColumnType=ColumnTypeRef.String,
                    Width2=2,

                },
                new DataGridHelperColumn
                {
                    Header="Комментарий клиента",
                    Path="COMMENTS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,

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
                    Header="Дизайнер",
                    Path="DES_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,

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
                    Header="Примечание для дизайнера",
                    Path="DESIGNER_NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,

                },
                new DataGridHelperColumn
                {
                    Header="Примечание для конструктора",
                    Path="DRAWER_NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=6,

                },
                new DataGridHelperColumn
                {
                    Header= "Менеджер РЦ",
                    Path="PR_CAL_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,

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
                    Header="Канал продаж",
                    Path="TYPE_CUSTOMER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=11,

                },
                new DataGridHelperColumn
                {
                    Header="Дт/вр принятия",
                    Path="ACCEPTANCE_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=10,

                    Format="dd.MM.yyyy HH:mm:ss",
                },
                new DataGridHelperColumn
                {
                    Header="Дт/вр разработки (плановое)",
                    Path="WORKING_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=10,

                    Format="dd.MM.yyyy HH:mm:ss",
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("WORKING_DTTM").ToDateTime()>DateTime.Now)
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
                    Header="Дт/вр отправки на согласование",
                    Path="SENDING_FOR_APPROVAL_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=10,

                    Format="dd.MM.yyyy HH:mm:ss",
                },
                new DataGridHelperColumn
                {
                    Header="Доработок",
                    Path="REWORK_CNT",
                    Width2=4,

                    ColumnType=ColumnTypeRef.Double,
                    Format = "N0",
                },
                new DataGridHelperColumn
                {
                    Header="Причина доработки",
                    Path="REWORK_REASON",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=11,

                },
                new DataGridHelperColumn
                {
                    Header="Согласование руководителя",
                    Path="HEAD_CONFIRM_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=11,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header="Дт/вр отправки дизайнеру",
                    Path="DTTM_TODO_DES",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=10,

                    Format="dd.MM.yyyy HH:mm:ss",
                },
                new DataGridHelperColumn
                {
                    Header="Дт/вр принятия дизайнером",
                    Path="DTTM_ACCEP_DES",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=10,

                    Format="dd.MM.yyyy HH:mm:ss",
                },
                new DataGridHelperColumn
                {
                    Header="Дт/вр завершения дизайнером",
                    Path="DTTM_DONE_DES",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=10,

                    Format="dd.MM.yyyy HH:mm:ss",
                },
                new DataGridHelperColumn
                {
                    Header="Дт/вр отправки конструктору",
                    Path="DTTM_TODO_CONS",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=10,

                    Format="dd.MM.yyyy HH:mm:ss",
                },
                new DataGridHelperColumn
                {
                    Header="Дт/вр принятия конструктором",
                    Path="DTTM_ACCEP_CONS",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=10,

                    Format="dd.MM.yyyy HH:mm:ss",
                },
                new DataGridHelperColumn
                {
                    Header="Дт/вр завершения конструктором",
                    Path="DTTM_DONE_CONS",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=10,
                    Format="dd.MM.yyyy HH:mm:ss",
                },
                new DataGridHelperColumn
                {
                    Header="Дт/вр ожидания ответа",
                    Path="DTTM_WAITING_ASNWER",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=10,
                    Format="dd.MM.yyyy HH:mm:ss",
                },
                new DataGridHelperColumn
                {
                    Header="ИД Статуса",
                    Path="STATUS",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=4,
                    Visible = false,
                    Exportable = false,
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="TK_ORDER_STATUS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header="Файл ТК",
                    Path="PATHTK",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                    Visible=false,
                    Exportable = false,
                },
                new DataGridHelperColumn
                {
                    Header="Путь к рабочей",
                    Path="PATHTK_WORK",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                    Visible=false,
                    Exportable = false,
                },
                new DataGridHelperColumn
                {
                    Header="Путь к новой",
                    Path="PATHTK_NEW",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                    Visible=false,
                    Exportable = false,
                },
                new DataGridHelperColumn
                {
                    Header="Путь к архивной",
                    Path="PATHTK_ARCHIVE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                    Visible=false,
                    Exportable = false,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="ART",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                    Visible=false,
                    Exportable = false,
                },
                new DataGridHelperColumn
                {
                    Header="Скрытая",
                    Path="HIDDEN_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=11,
                    Visible=false,
                    Exportable = false,
                },
                new DataGridHelperColumn
                {
                    Header="Количество похожих",
                    Path="DUPLICATE",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=11,
                    Visible=false,
                    Exportable = false,
                },
                new DataGridHelperColumn
                {
                    Header="Файл чертежа",
                    Path="DRAWING_FILE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                    Visible=false,
                    Exportable = false,
                },
                new DataGridHelperColumn
                {
                    Header="Подписанная ТК",
                    Path="FILE_SIGNED",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                    Visible=false,
                    Exportable = false,
                },
                new DataGridHelperColumn
                {
                    Header="Статус дизайна",
                    Path="DESIGN_STATUS",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=11,
                    Visible=false,
                    Exportable = false,
                },
                new DataGridHelperColumn
                {
                    Header="Статус чертежа",
                    Path="DRAWING_STATUS",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=11,
                    Visible=false,
                    Exportable = false,
                },
                new DataGridHelperColumn
                {
                    Header="Чат",
                    Path="CHAT_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=11,
                    Visible=false,
                    Exportable = false,
                },
                new DataGridHelperColumn
                {
                    Header="Печать по рилевке",
                    Path="SCORER_PRINT",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=4,
                    Visible=false,
                    Exportable = false,
                },
                new DataGridHelperColumn
                {
                    Header="Создано менеджером",
                    Path="MANAGER_CREATOR_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                    Visible=false,
                    Exportable = false,
                },
                new DataGridHelperColumn
                {
                    Header="ИД покупателя",
                    Path="ID_POK",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=11,
                    Visible=false,
                    Exportable = false,
                },
            };
            Grid.SetColumns(columns);

            Grid.SetPrimaryKey("ID_TK");
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.ColumnWidthMin = 15;
            Grid.SearchText = GridSearch;
            Grid.Toolbar = GridToolbar;
            Grid.Commands = Commander;
            Grid.EnableSortingGrid = false;

            Grid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
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
                            // В работе/Согласование/Согласована
                            case 1:
                            case 3:
                            case 7:
                                color = HColor.White;
                                break;
                            // Отменена
                            case 2:
                                color = HColor.Red;
                                break;
                            // Опубликована
                            case 4:
                                color = HColor.Green;
                                break;
                            // Ожидание ответа
                            case 6:
                                color = HColor.Orange;
                                break;
                            default:
                                color = HColor.White;
                                break;
                        }

                        if (row.CheckGet("HIDDEN_FLAG").ToInt() == 1
                            && row.CheckGet("STATUS").ToInt().ContainsIn(0,1,5,3,7)
                        )
                        {
                                color = HColor.Yellow;
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
            Grid.AutoUpdateInterval = 120;
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
            var p = new Dictionary<string, string>();

            MaskedTextBox_Parsed();
            string showAll = (bool)ShowAllCheckBox.IsChecked ? "1" : "0";
            p.Add("SHOW_ALL", showAll);
            p.Add("ENG_EMPL_ID", EngineerSelectBox?.SelectedItem.Key.ToString());
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

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "ListForEngineer");
            q.Request.SetParams(p);


            q.DoQuery();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {

                    var ds = ListDataSet.Create(result, "ITEMS");

                    GridDS = ds;

                    foreach (var item in GridDS.Items)
                    {
                        if (SelectedIds != null && SelectedIds.Count > 0)
                        {
                            string numbersString = string.Join(",", SelectedIds);
                            if (numbersString.Contains(item.CheckGet("ID_TK")))
                            {
                                item.CheckAdd("_SELECTED", "1");
                            }
                            else
                            {
                                item.CheckAdd("_SELECTED", "0");
                            }
                        }
                        else
                        {
                            item.CheckAdd("_SELECTED", "0");
                        }
                    }
                    
                    Grid.UpdateItems(GridDS);
                    ShowButton.Style = (Style)ShowButton.TryFindResource("Button");
                }
            }
            else
            {
                q.ProcessError();
            }
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
                    int pokupatel = PokupatelSelectBox.SelectedItem.Key.ToInt();

                    var list = new List<Dictionary<string, string>>();
                    foreach (var item in Grid.Items)
                    {
                        bool includeByVisible = true;
                        if (status!=-1)
                        {
                            if (item.CheckGet("STATUS").ToInt() != status)
                            {
                                includeByVisible = false;
                            }
                        }
                        if (pokupatel != -1)
                        {
                            if (item.CheckGet("ID_POK").ToInt() != pokupatel)
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
        /// Изменение статуса заявки
        /// STATUS: 1 - в работе, 2 - отменена, 3 - на согласовании
        /// FLAG_UPD_SENDING_FOR_APPROVAL_DTTM: флаг обновления дт/вр отправления на согласование
        /// FLAG_UPD_ACCEPTANCE_DTTM: флаг обновления дт/вр принятия
        /// FLAG_UPD_CANCELED_DTTM: флаг обновления дт/вр отмены заявки
        /// </summary>
        private async void TechMapChangeOrderStatus(int id_tk, int status, int flag_upd_sending_for_approval_dttm, int flag_upd_acceptance_dttm, int flag_upd_canceled_dttm)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "UpdateStatus");
            q.Request.SetParam("ID_TK", id_tk.ToString());
            q.Request.SetParam("STATUS", status.ToString());
            q.Request.SetParam("FLAG_UPD_SENDING_FOR_APPROVAL_DTTM", flag_upd_sending_for_approval_dttm.ToString());
            q.Request.SetParam("FLAG_UPD_ACCEPTANCE_DTTM", flag_upd_acceptance_dttm.ToString());
            q.Request.SetParam("FLAG_UPD_CANCELED_DTTM", flag_upd_canceled_dttm.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Отмена согласования заявки на ТК
        /// </summary>
        public async void RejectAgreedTk()
        {
            Grid.Toolbar.IsEnabled = false;
            Grid.ShowSplash();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "SetAgreed");
            q.Request.SetParam("ID_TK", Grid.SelectedItem.CheckGet("ID_TK"));
            q.Request.SetParam("AGREED_FLAG", "0");
            q.Request.SetParam("FLAG_UPD_AGREED_DTTM", "0");
            q.Request.SetParam("FLAG_UPD_UNSIGNED_FLAG", "1");
            q.Request.SetParam("OLD_STATUS", Grid.SelectedItem.CheckGet("STATUS"));

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
        /// Отмена согласования заявки на ТК
        /// </summary>
        public async void UpdateFileSigned()
        {
            Grid.Toolbar.IsEnabled = false;
            Grid.ShowSplash();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "UpdateFileSigned");
            q.Request.SetParam("ID_TK", Grid.SelectedItem.CheckGet("ID_TK"));
            q.Request.SetParam("FILE_SIGNED", "");

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
            q.Request.SetParam("HIDDEN", hidden_flag.ToString());

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
        /// Вернуть техкарту в работу
        /// flag: 0 - отменить ожидание, 1 - установить ожидание
        /// </summary>
        private async void SetlWaiting(int flag)
        {
            int id = Grid.SelectedItem.CheckGet("ID_TK").ToInt();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "SetWaiting");

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
        /// Установить статус разработки дизайна
        /// </summary>
        private async void SetDesign(int status)
        {
            int id = Grid.SelectedItem.CheckGet("ID_TK").ToInt();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "UpdateDesign");

            q.Request.SetParam("ID_TK", id.ToString());
            q.Request.SetParam("DESIGN", status.ToString());

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
        /// Установить статус разработки чертежа
        /// </summary>
        private async void SetDrawing(int status)
        {
            int id = Grid.SelectedItem.CheckGet("ID_TK").ToInt();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "UpdateDrawing");

            q.Request.SetParam("ID_TK", id.ToString());
            q.Request.SetParam("DRAWING", status.ToString());

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
        /// Функция изменения площадки для ТК
        /// </summary>
        private async void ChangeFactId()
        {
            int id = Grid.SelectedItem.CheckGet("ID_TK").ToInt();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "UpdateFactId");

            q.Request.SetParam("ID_TK", id.ToString());

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
        /// Открытие формы привязывания старой техкарты
        /// </summary>
        private void ShowPinTk()
        {
            if (SelectedItem != null)
            {
                var frame = new WebTechnologicalMapPinTk();
                frame.TkId = Grid.SelectedItem.CheckGet("ID_TK").ToInt();
                frame.ReceiverName = ControlName;
                frame.Show();
                Grid.LoadItems();
            }
        }

        /// <summary>
        /// Открытие формы с похожими техкартами
        /// </summary>
        private void ShowDuplicate()
        {
            if (SelectedItem != null)
            {
                var fraimeFiles = new WebTechnologicalMapDuplicate();
                fraimeFiles.TkId = Grid.SelectedItem.CheckGet("ID_TK").ToInt();

                fraimeFiles.Show();
            }
        }

        /// <summary>
        /// Открытие формы изменения менеджера техкарты
        /// </summary>
        private void ChangeNameUser()
        {
            if (SelectedItem != null)
            {
                var frame = new WebTechnologicalMapChangeManager();
                frame.TkId = Grid.SelectedItem.CheckGet("ID_TK").ToInt();
                frame.ReciverName = ControlName;
                frame.Show();
                Grid.LoadItems();
            }
        }

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

        public void ConvertExcelToPdf(string fullPathTk)
        {
            // Локальный путь для сохранения нового пдф файла
            string fileFullLocalPathPdf = $"";
            // Серверный путь для сохранения нового пдф файла
            string fileFullGlobalPathPdf = $"";
            int id_tk = Grid.SelectedItem.CheckGet("ID_TK").ToInt();

            if (!string.IsNullOrEmpty(fullPathTk))
            {
                // Create COM Objects
                Microsoft.Office.Interop.Excel.Application excelApplication;
                Microsoft.Office.Interop.Excel.Workbook excelWorkbook;

                // Create new instance of Excel
                excelApplication = new Microsoft.Office.Interop.Excel.Application();

                // Make the process invisible to the user
                excelApplication.ScreenUpdating = false;

                // Make the process silent
                excelApplication.DisplayAlerts = false;

                // Open the workbook that you wish to export to PDF
                excelWorkbook = excelApplication.Workbooks.Open(fullPathTk);

                // If the workbook failed to open, stop, clean up, and bail out
                if (excelWorkbook == null)
                {
                    excelApplication.Quit();

                    excelApplication = null;
                    excelWorkbook = null;
                }
                else
                {
                    var exportSuccessful = true;

                    try
                    {
                        string path = "C:\\temp\\erp\\storage\\net\\techcards\\";
                        string folder = "_PDF";
                        string fileName = $"{id_tk}.pdf";

                        fileFullLocalPathPdf = $"{path}{folder}\\{fileName}";

                        if (System.IO.Directory.Exists($"{path}{folder}"))
                        {
                            // Call Excel's native export function (valid in Office 2007 and Office 2010, AFAIK)
                            excelWorkbook.ExportAsFixedFormat(Microsoft.Office.Interop.Excel.XlFixedFormatType.xlTypePDF, fileFullLocalPathPdf);
                        }
                        else
                        {
                            System.IO.Directory.CreateDirectory($"{path}{folder}");
                            if (System.IO.Directory.Exists($"{path}{folder}"))
                            {
                                // Call Excel's native export function (valid in Office 2007 and Office 2010, AFAIK)
                                excelWorkbook.ExportAsFixedFormat(Microsoft.Office.Interop.Excel.XlFixedFormatType.xlTypePDF, fileFullLocalPathPdf);
                            }
                            else
                            {
                                string msg = $"Не удалось подготовить папку для PDF файла";
                                var d = new DialogWindow($"{msg}", "ТК решётки в сборе", "", DialogWindowButtons.OK);
                                d.ShowDialog();
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        // Mark the export as failed for the return value...
                        exportSuccessful = false;

                        string msg = $"Не удалось преобразовать эксель файл в PDF. {ex.Message}";
                        var d = new DialogWindow($"{msg}", "ТК решётки в сборе", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    finally
                    {
                        excelWorkbook.Close();
                        excelApplication.Quit();

                        excelApplication = null;
                        excelWorkbook = null;

                        if (System.IO.File.Exists(fileFullLocalPathPdf))
                        {
                            string path = Central.GetStorageNetworkPathByCode("techcard_pdf");
                            string folder = $"{(int)(id_tk / 10000)}";
                            string fileName = $"{id_tk}.pdf";

                            fileFullGlobalPathPdf = System.IO.Path.Combine(path, folder, fileName);

                            string fileFolderGlobalPathPdf = System.IO.Path.Combine(path, folder);
                            System.IO.Directory.CreateDirectory(fileFolderGlobalPathPdf);
                            System.IO.File.Copy(fileFullLocalPathPdf, fileFullGlobalPathPdf, true);

                            //var q = new LPackClientQuery();
                            //q.Request.SetParam("Module", "Preproduction");
                            //q.Request.SetParam("Object", "TechnologicalMap");
                            //q.Request.SetParam("Action", "AddAttachmentPdf");
                            //q.Request.SetParam("ID_TK", id_tk.ToString());
                            //q.Request.SetParam("ORIG_NAME", fileName);

                            //q.DoQuery();

                            //if (q.Answer.Status == 0)
                            //{
                            //    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                            //    if (result != null)
                            //    {
                            //        var ds = ListDataSet.Create(result, "ITEMS");
                            //        var newFileName = ds.Items[0].CheckGet("FILE_NAME");
                            //        var folderAttachment = Central.GetStorageNetworkPathByCode("techcard_attachment");
                            //        var fileFullGlobalPathAttachment = System.IO.Path.Combine(folderAttachment, newFileName);
                            //        System.IO.File.Copy(fileFullLocalPathPdf, fileFullGlobalPathAttachment, true);

                                    string msg = "PDF файл успешно создан";
                                    var d = new DialogWindow($"{msg}", "Создание ПДФ", "", DialogWindowButtons.OK);
                                    d.ShowDialog();
                            //    }
                            //}
                            //else
                            //{
                            //    string msg = "Не удалось прикрепить PDF файл к комлекту техкарт.";
                            //    var d = new DialogWindow($"{msg}", "ТК решётки в сборе", "", DialogWindowButtons.OK);
                            //    d.ShowDialog();
                            //}

                        }
                        else
                        {
                            string msg = $"Не удалось преобразовать эксель файл в PDF.";
                            var d = new DialogWindow($"{msg}", "ТК решётки в сборе", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                }
            }

        }

        #endregion

        #region "Обработка событий"
        private void ShowAllCheckBox_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void UpdateGridItems(System.Windows.DependencyObject d, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();

        }

        #endregion

        private void EngineerSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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

    }
}