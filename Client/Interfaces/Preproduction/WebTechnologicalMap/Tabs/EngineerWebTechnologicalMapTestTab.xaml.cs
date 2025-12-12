using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Service.Printing;
using DevExpress.Data.Filtering.Helpers;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using DevExpress.Xpf.Bars;
using System.Globalization;
using DevExpress.Xpf.Core.ReflectionExtensions.Attributes;
using DevExpress.Xpf.Grid;
using DevExpress.XtraReports.UI;
using Microsoft.Win32;
using Newtonsoft.Json;
using NPOI.POIFS.Crypt.Dsig;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using static Client.Interfaces.Main.DataGridHelperColumn;
using Application = Microsoft.Office.Interop.Excel.Application;
using Microsoft.Office.Interop.Excel;
using Microsoft.Office.Core;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс веб-техкарт (Инженеры)
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class EngineerWebTechnologicalMapTestTab : ControlBase, INotifyPropertyChanged
    {
        public EngineerWebTechnologicalMapTestTab()
        {
            InitializeComponent();

            RoleName = "[erp]engineer_web_tech_map";
            ControlTitle = "Веб-техкарты (Тест)";
            DocumentationUrl = "/";

            Orders = new ObservableCollection<TkOrder>();

            OnLoad = () =>
            {
                FormInit();
                SetDefaults();
                InitCommander();
                InitializeTimer();
            };

            OnUnload = () =>
            {
                StopTimer();
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
                                LoadItems();
                                break;
                            case "Rework":
                                LoadItems();
                                break;
                            case "UpdateStatusDesignOrDrawing":
                                if (msg.Message.ToInt() == 1)
                                {
                                    SetDesign(1);
                                }
                                else if (msg.Message.ToInt() == 2)
                                {
                                    SetDrawing(1);
                                }
                                LoadItems();
                                break;
                        }
                    }
                }
            };

            DataContext = this;
        }

        #region "Переменные"

        public ObservableCollection<TkOrder> _orders;

        public ObservableCollection<TkOrder> Orders
        {
            get => _orders;
            set
            {
                _orders = value;
                OnPropertyChanged(nameof(Orders));
            }
        }

        private Timer refreshTimer;

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Название вкладки
        /// </summary>
        public string TabName;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool flagUpdate { get; set; }

        #endregion

        #region "Таймер"

        private void InitializeTimer()
        {
            refreshTimer = new Timer();
            refreshTimer.Interval = 180000; 
            refreshTimer.Tick += RefreshTimer_Tick;
            refreshTimer.Start();
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            LoadItems();
        }

        private void StopTimer()
        {
            refreshTimer?.Stop();
        }

        private void StartTimer()
        {
            refreshTimer?.Start();
        }
        #endregion

        #region "Загрузка справочников"
        public void SetDefaults()
        {
            LoadRefs();
            LoadItems();
        }

        /// <summary>
        /// Загрузка инженеров
        /// </summary>
        public async void LoadRefs()
        {
            flagUpdate = false;
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
            flagUpdate = true;
        }

        #endregion


        #region "Форма"
        /// <summary>
        /// Инициализация Commander для контекстного меню и команд
        /// </summary>
        private void InitCommander()
        {
            Commander.SetCurrentGridName("gridControl");

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
                        LoadItems();
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
                        ExportToExcel();
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
                        var SelectedIds = gridControl.SelectedItems;
                        if (SelectedIds != null && SelectedIds.Count > 0)
                        {
                            var dw = new DialogWindow("Вы действительно хотите принять заявку на техкарту?", "Принятие заявки на техкарту", "", DialogWindowButtons.NoYes);
                            if ((bool)dw.ShowDialog())
                            {
                                foreach (TkOrder row in SelectedIds)
                                {
                                    TechMapChangeOrderStatus(row.IdTk, 1, 0, 1, 0);
                                }
                            }
                        }
                        else
                        {
                            var dw = new DialogWindow("Вы действительно хотите принять заявку на техкарту?", "Принятие заявки на техкарту", "", DialogWindowButtons.NoYes);
                            if ((bool)dw.ShowDialog())
                            {

                                var row = GetSelectRow();
                                TechMapChangeOrderStatus(row.IdTk, 1, 0, 1, 0);
                            }
                        }
                            
                    },
                    CheckEnabled = () =>
                    {
                        var result = true;
                        var SelectedIds = gridControl.SelectedItems;
                        if (SelectedIds != null && SelectedIds.Count > 0)
                        {
                            foreach (TkOrder row in SelectedIds)
                            {
                                if (row != null && !row.Status.ContainsIn(0, 5))
                                {
                                    result = false;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            var row = GetSelectRow();
                            if (row != null && !row.Status.ContainsIn(0, 5))
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
                        var SelectedIds = gridControl.SelectedItems;
                        if (SelectedIds != null && SelectedIds.Count > 0)
                        {
                            var dw = new DialogWindow("Вы действительно хотите отменить заявки на техкарты?", "Отмена заявок на техкарты", "", DialogWindowButtons.NoYes);
                            if ((bool)dw.ShowDialog())
                            {
                                foreach (TkOrder row in SelectedIds)
                                {
                                    TechMapChangeOrderStatus(row.IdTk, 2, 0, 0, 1);
                                }
                            }
                        }
                        else
                        {
                            var dw = new DialogWindow("Вы действительно хотите отменить заявку на техкарту?", "Отмена заявки на техкарту", "", DialogWindowButtons.NoYes);
                            if ((bool)dw.ShowDialog())
                            {
                                var row = GetSelectRow();
                                TechMapChangeOrderStatus(row.IdTk, 2, 0, 0, 1);
                            }
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = true;
                        var SelectedIds = gridControl.SelectedItems;
                        if (SelectedIds != null && SelectedIds.Count > 0)
                        {
                            foreach (TkOrder row in SelectedIds)
                            {
                                if (row != null && !row.Status.ContainsIn(0, 1, 3, 5, 6, 7))
                                {
                                    result = false;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            var row = GetSelectRow();
                            if (row != null && !row.Status.ContainsIn(0, 1, 3, 5, 6, 7))
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
                            var row = GetSelectRow();
                            TechMapChangeOrderStatus(row.IdTk, 3, 1, 0, 0);

                            var path = row.Pathtk;

                            var folder_new = row.PathtkNew;

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
                        var row = GetSelectRow();

                        if (row != null)
                        {
                            var path = row.Pathtk;
                            var folder_new = row.PathtkNew;

                            if (path != null && File.Exists(Path.Combine(folder_new, path)))
                            {
                                file_exist = true;
                            }

                            if (row.Status == 1
                                && row.DesignStatus.ToInt().ContainsIn(0, 2)
                                && row.DrawingStatus.ToInt().ContainsIn(0, 2)
                                && file_exist)
                            {
                                result = true;
                            }
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
                            var row = GetSelectRow();
                            TechMapChangeOrderStatus(row.IdTk, 1, 0, 0, 0);
                            RejectAgreedTk();

                            if (row != null && !string.IsNullOrEmpty(row.FileSigned))
                            {
                                var file_name = row.FileSigned;
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
                        var row = GetSelectRow();

                        if (row != null && row.Status==3)
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
                            var row = GetSelectRow();
                            TechMapChangeOrderStatus(row.IdTk, 4, 0, 0, 0);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = GetSelectRow();

                        if (row != null && row.Status == 7 && !row.Art.IsNullOrEmpty())
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
                            var row = GetSelectRow();
                            TechMapChangeOrderStatus(row.IdTk, 3, 0, 0, 0);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = GetSelectRow();

                        if (row != null && row.Status == 4)
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
                        var row = GetSelectRow();
                        if (row != null)
                        {
                            var rework = new WebTechnologicalMapRework();
                            rework.TkId = row.IdTk;
                            rework.ResiverName = ControlName;
                            rework.Show();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;

                        var row = GetSelectRow();
                        if (row != null && row.Status.ContainsIn(1, 3, 7))
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
                        var row = GetSelectRow();
                        if (row != null && row.HiddenFlag == 1)
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
                        var row = GetSelectRow();
                        if (row != null && row.HiddenFlag == 0)
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
                        var row = GetSelectRow();
                        if (row != null && row.Status == 6)
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
                        var row = GetSelectRow();
                        if (row != null)
                        {
                            var noteFrame = new WebTechnologicalMapAddNote();
                            noteFrame.ReceiverName = ControlName;
                            noteFrame.Type = 1;
                            noteFrame.TkId = row.IdTk;
                            noteFrame.Show();
                        }
                    },
                    CheckMenuVisible = () =>
                    {
                        var result = false;
                        var row = GetSelectRow();
                        if (row != null && row.DesignStatus.GetValueOrDefault().ContainsIn(0, 2)
                        &&  row.Status == 1)
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
                    CheckMenuVisible = () =>
                    {
                        var result = false;
                        var row = GetSelectRow();
                        if (row != null && row.DesignStatus.ToInt()==1 && row.Status==1)
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
                    CheckMenuVisible = () =>
                    {
                        var result = false;
                        var row = GetSelectRow();
                        if (row != null && row.DesignStatus == 2 && row.Status==1)
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
                        var row = GetSelectRow();
                        if (row != null)
                        {
                            var noteFrame = new WebTechnologicalMapAddNote();
                            noteFrame.ReceiverName = ControlName;
                            noteFrame.Type = 2;
                            noteFrame.TkId = row.IdTk;
                            noteFrame.Show();
                        }
                    },
                    CheckMenuVisible = () =>
                    {
                        var result = false;
                        var row = GetSelectRow();
                        if (row != null && row.DrawingStatus.GetValueOrDefault().ContainsIn(0, 2)
                            && row.Status == 1)
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
                    CheckMenuVisible = () =>
                    {
                        var result = false;
                        var row = GetSelectRow();
                        if (row != null && row.DrawingStatus == 1 && row.Status==1)
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
                    CheckMenuVisible = () =>
                    {
                        var result = false;
                        var row = GetSelectRow();
                        if (row != null && row.DrawingStatus == 2 && row.Status==1)
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
                        var row = GetSelectRow();
                        if (row != null && row.IdTk > 0
                            && row.Status == 1)
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
                        var row = GetSelectRow();
                        if (row != null && row.Duplicate.ToInt().ContainsIn(1, 2, 3))
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
                        var row = GetSelectRow();
                        if (row != null && row.Status != 4)
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
                        var row = GetSelectRow();
                        if (row != null && row.Status != 4)
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
                        var row = GetSelectRow();
                        var pokupatelContact = new WebTechnologicalMapPokupatelContact();
                        pokupatelContact.ReceiverName = TabName;
                        pokupatelContact.IdTk = row.IdTk;
                        pokupatelContact.IdPok = row.IdPok;
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
                    Title = "Показать ТК",
                    Description = "Показать Excel файл техкарты",
                    ButtonUse = true,
                    Group = "file",
                    ButtonName = "ShowFileTkButton",
                    MenuUse = true,
                    Action = () =>
                    {
                        var row = GetSelectRow();
                        if (row != null)
                        {
                            var path = row.Pathtk;

                            var folder_new = row.PathtkNew;
                            var folder_work = row.PathtkWork;
                            var folder_archive = row.PathtkArchive;

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



                            if (File.Exists(Path.Combine(folder_work, path)))
                            {
                                Process.Start(new ProcessStartInfo(Path.Combine(folder_work, path)) { UseShellExecute = true });
                            }
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;

                        var row = GetSelectRow();
                        if (row != null)
                        {
                            var path = row.Pathtk;

                            if (path != null)
                            {
                                var folder_new = row.PathtkNew;
                                var folder_work = row.PathtkWork;
                                var folder_archive = row.PathtkArchive;
                                if(File.Exists(Path.Combine(folder_new, path))
                                || File.Exists(Path.Combine(folder_work, path))
                                || File.Exists(Path.Combine(folder_archive, path)))
                                {
                                    result = true;
                                }
                            }
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
                        var row = GetSelectRow();
                        if (row != null)
                        {
                            Process.Start(new ProcessStartInfo(row.DrawingFile) { UseShellExecute = true });
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;

                        var row = GetSelectRow();
                        if (row != null && File.Exists(row.DrawingFile))
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
                        var row = GetSelectRow();
                        if (row != null)
                        {
                            var path = row.FileSigned;

                            string storagePath = Central.GetStorageNetworkPathByCode("techcard_signed");

                            var filePath = Path.Combine(storagePath, path);

                            if (File.Exists(filePath))
                            {
                                Process.Start(new ProcessStartInfo(filePath));
                            }
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;

                        var row = GetSelectRow();
                        if (row != null)
                        {
                            var path = row.FileSigned;
                            string storagePath = Central.GetStorageNetworkPathByCode("techcard_signed");

                            if (path != null)
                            {
                                var filePath = Path.Combine(storagePath, path);

                                if (File.Exists(filePath))
                                {
                                    result = true;
                                }
                            }
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
                        var row = GetSelectRow();
                        if (row != null)
                        {
                            var historyFrame = new WebTechnologicalMapHistory();
                            historyFrame.ReceiverName = TabName;
                            historyFrame.IdTk = row.IdTk;
                            historyFrame.Show();
                        }
                    },
                });
            }

            Commander.Init(this);

            tableView.ShowGridMenu += TableView_ShowGridMenu;

            gridControl.MouseDoubleClick += GridControl_MouseDoubleClick;
        }
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
        /// Загрузка данных в таблицу
        /// </summary>

        public async void LoadItems()
        {
            if (flagUpdate)
            {
                gridControl.ShowLoadingPanel = true;

                MaskedTextBox_Parsed();
                var p = new Dictionary<string, string>();

                string showAll = (bool)ShowAllCheckBox.IsChecked ? "1" : "0";
                p.Add("SHOW_ALL", showAll);
                p.Add("ENG_EMPL_ID", EngineerSelectBox?.SelectedItem.Key?.ToString());
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
                q.Request.SetParam("Action", "ListForEngineerTest");
                q.Request.SetParams(p);

                q.Request.Timeout = 120000;


                await Task.Run(() => q.DoQuery());


                if (q.Answer.Status == 0)
                {
                    var orders = JsonConvert.DeserializeObject<List<TkOrder>>(q.Answer.Data);

                    if (orders != null)
                    {
                        Orders.Clear();

                        foreach (var order in orders)
                        {
                            Orders.Add(order);
                        }
                        ShowButton.Style = (System.Windows.Style)ShowButton.TryFindResource("Button");
                    }
                }
                else
                {
                    q.ProcessError();
                }

                gridControl.ShowLoadingPanel = false;
            }

        }




        /// <summary>
        /// Обработчик для создания контекстного меню
        /// </summary>
        private void TableView_ShowGridMenu(object sender, DevExpress.Xpf.Grid.GridMenuEventArgs e)
        {
            if (e.MenuType == DevExpress.Xpf.Grid.GridMenuType.RowCell)
            {
                e.Customizations.Clear();

                Commander.UpdateActions();
                
                var contextMenu = Commander.RenderMenu("gridControl");

                foreach (var item in contextMenu)
                {
                    var menuItem = item.Value;

                    if (menuItem.Header == "-")
                    {
                        e.Customizations.Add(new DevExpress.Xpf.Bars.BarItemSeparator());
                    }
                    else
                    {
                        if (menuItem.GroupHeader!=null && menuItem.GroupHeaderName != "")
                        {
                            var barButtonHeaderItem = new DevExpress.Xpf.Bars.BarSubItem()
                            {
                                Content = menuItem.GroupHeaderName,
                            };
                            var specificHeaderItem = e.Customizations
                                .Where(item => item is BarSubItem headerItem && headerItem.Content == menuItem.GroupHeaderName)
                                .FirstOrDefault() as BarSubItem;
                            if (specificHeaderItem!=null)
                            {
                                var b = new BarButtonItem()
                                {
                                    Content = menuItem.Header,
                                    IsEnabled = menuItem.Enabled,
                                    Hint = menuItem.ToolTip
                                };
                                b.ItemClick += (s, args) =>
                                {
                                    menuItem.Action?.Invoke();
                                };
                                specificHeaderItem.ItemLinks.Add(b);
                                
                            }
                            else
                            {
                                var b = new BarButtonItem()
                                {
                                    Content = menuItem.Header,
                                    IsEnabled = menuItem.Enabled,
                                    Hint = menuItem.ToolTip
                                };
                                b.ItemClick += (s, args) =>
                                {
                                    menuItem.Action?.Invoke();
                                };
                                barButtonHeaderItem.ItemLinks.Add(b);

                                e.Customizations.Add(barButtonHeaderItem);
                            }

                        }
                        else
                        {
                            var barButtonItem = new DevExpress.Xpf.Bars.BarButtonItem()
                            {
                                Content = menuItem.Header,
                                IsEnabled = menuItem.Enabled,
                                Hint = menuItem.ToolTip
                            };

                            barButtonItem.ItemClick += (s, args) =>
                            {
                                menuItem.Action?.Invoke();
                            };

                            e.Customizations.Add(barButtonItem);
                        }
                            
                    }
                }
            }
        }

        /// <summary>
        /// Обработчик двойного клика по строке грида
        /// </summary>
        private void GridControl_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Commander.ProcessDoubleClick("gridControl");
        }
        
        private TkOrder GetSelectRow()
        {
            if (gridControl.CurrentItem != null && gridControl.CurrentItem is TkOrder tkOrder)
            {
                return tkOrder;
            }

            return null;
        }

        private TkOrder GetRowById(int idTk)
        {
            return Orders.FirstOrDefault(order => order.IdTk == idTk);
        }


        /// <summary>
        /// Экспорт данных грида в Excel
        /// </summary>
        private void ExportToExcel()
        {
            try
            {
                var filePath = Central.GetTempFilePathWithExtension("xlsx");

                tableView.ExportToXlsx(filePath);

                if (File.Exists(filePath))
                {
                    System.Diagnostics.Process.Start(filePath);
                }
            }
            catch (Exception ex)
            {
                var dialog = new DialogWindow("Ошибка при экспорте в Excel", "Excel");
                dialog.ShowDialog();
            }
        }

        #endregion



        #region "Функции"

        /// <summary>
        /// Печать информации по техкарте
        /// </summary>
        private async void TechcardPrint(int chatType = 0)
        {
            var row = GetSelectRow();
            if (row != null)
            {
                int id = row.IdTk;

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

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                LoadItems();
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
            var row = GetSelectRow();
            if (row == null) return;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "SetAgreed");
            q.Request.SetParam("ID_TK", row.IdTk.ToString());
            q.Request.SetParam("AGREED_FLAG", "0");
            q.Request.SetParam("FLAG_UPD_AGREED_DTTM", "0");
            q.Request.SetParam("FLAG_UPD_UNSIGNED_FLAG", "1");
            q.Request.SetParam("OLD_STATUS", row.Status.ToString());

            q.DoQuery();

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
        public async void UpdateFileSigned()
        {
            var row = GetSelectRow();
            if (row == null) return;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "UpdateFileSigned");
            q.Request.SetParam("ID_TK", row.IdTk.ToString());
            q.Request.SetParam("FILE_SIGNED", "");

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
        /// Вернуть техкарту в работу
        /// flag: 0 - отменить ожидание, 1 - установить ожидание
        /// </summary>
        private async void SetlWaiting(int flag)
        {
            var row = GetSelectRow();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "SetWaiting");

            q.Request.SetParam("ID_TK", row.IdTk.ToString());
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
                        LoadItems();
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Установить флаг видимости ТК
        /// </summary>
        private async void SetHiddenFlag(int hidden_flag)
        {
            var row = GetSelectRow();
            if (row == null) return;

            int id = row.IdTk;

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

                LoadItems();
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
            var row = GetSelectRow();
            if (row == null) return;

            int id = row.IdTk;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "UpdateDesign");

            q.Request.SetParam("ID_TK", id.ToString());
            q.Request.SetParam("DESIGN", status.ToString());

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEM");
                    if (ds.Items[0].CheckGet("SUCCESS").ToInt() == 1)
                    {
                        LoadItems();
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
            var row = GetSelectRow();
            if (row == null) return;

            int id = row.IdTk;

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
                        LoadItems();
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
            var row = GetSelectRow();
            if (row == null) return;

            int id = row.IdTk;

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
                        LoadItems();
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
            var row = GetSelectRow();
            if (row != null)
            {
                var frame = new WebTechnologicalMapPinTk();
                frame.TkId = row.IdTk;
                frame.ReceiverName = ControlName;
                frame.Show();
                LoadItems();
            }
        }

        /// <summary>
        /// Открытие формы с похожими техкартами
        /// </summary>
        private void ShowDuplicate()
        {
            var row = GetSelectRow();
            if (row != null)
            {
                var fraimeFiles = new WebTechnologicalMapDuplicate();
                fraimeFiles.TkId = row.IdTk;
                fraimeFiles.Show();
            }
        }

        /// <summary>
        /// Открытие формы изменения менеджера техкарты
        /// </summary>
        private void ChangeNameUser()
        {
            var row = GetSelectRow();
            if (row != null)
            {
                var frame = new WebTechnologicalMapChangeManager();
                frame.TkId = row.IdTk;
                frame.ReciverName = ControlName;
                frame.Show();
                LoadItems();
            }
        }

        /// <summary>
        /// Открытие вкладки с чатом по веб-техкарте
        /// </summary>
        /// <param name="chatType">Тип чата: 0 - чат с клиентом, 1 - чат с коллегами</param>
        private void OpenChat(int chatType = 0)
        {
            var row = GetSelectRow();
            if (row != null)
            {
                var chatFrame = new WebTechnologicalMapChat();
                chatFrame.ObjectId = row.IdTk;
                chatFrame.ReceiverName = ControlName;
                chatFrame.ChatObject = "WebTechMap";
                chatFrame.ChatType = chatType;
                if (chatType == 1)
                {
                    chatFrame.ChatId = row.ChatId ?? 0;
                }
                else
                {
                    chatFrame.ChatId = row.IdTk;
                }
                chatFrame.Edit();
            }
        }

        /// <summary>
        /// Открытие вкладку с приложенными файлами
        /// </summary>
        private void OpenAttachments()
        {
            var row = GetSelectRow();
            if (row != null)
            {
                var fraimeFiles = new WebTechnologicalMapFiles();
                fraimeFiles.TkId = row.IdTk;
                fraimeFiles.TechCardName = row.Num;
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
            int id_tk = GetSelectRow().IdTk;

            if (!string.IsNullOrEmpty(fullPathTk))
            {
                Microsoft.Office.Interop.Excel.Application excelApplication;
                Microsoft.Office.Interop.Excel.Workbook excelWorkbook;

                excelApplication = new Microsoft.Office.Interop.Excel.Application();
                excelApplication.ScreenUpdating = false;
                excelApplication.DisplayAlerts = false;
                excelWorkbook = excelApplication.Workbooks.Open(fullPathTk);

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
                            excelWorkbook.ExportAsFixedFormat(Microsoft.Office.Interop.Excel.XlFixedFormatType.xlTypePDF, fileFullLocalPathPdf);
                        }
                        else
                        {
                            System.IO.Directory.CreateDirectory($"{path}{folder}");
                            if (System.IO.Directory.Exists($"{path}{folder}"))
                            {
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

                            string msg = "PDF файл успешно создан";
                            var d = new DialogWindow($"{msg}", "Создание ПДФ", "", DialogWindowButtons.OK);
                            d.ShowDialog();

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
            LoadItems();
        }

        private void UpdateGridItems(System.Windows.DependencyObject d, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            LoadItems();
        }

        #endregion

        private void EngineerSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LoadItems();
        }

        private void DtTo_EditValueChanging(object sender, DevExpress.Xpf.Editors.EditValueChangingEventArgs e)
        {
            ShowButton.Style = (System.Windows.Style)ShowButton.TryFindResource("FButtonPrimary");
        }

        private void DtFrom_EditValueChanging(object sender, DevExpress.Xpf.Editors.EditValueChangingEventArgs e)
        {
            ShowButton.Style = (System.Windows.Style)ShowButton.TryFindResource("FButtonPrimary");
        }

        /// <summary>
        /// Обработчик изменения выделенной строки в гриде
        /// Обновляет состояние команд (кнопок контекстного меню) при клике на элемент
        /// </summary>
        private void GridControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            // Обновляем состояние всех команд
            Commander.UpdateActions();
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
                if ((e.Key >= Key.D0 && e.Key <= Key.D9) ||
                (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9))
                {
                    ShowButton.Style = (System.Windows.Style)ShowButton.TryFindResource("FButtonPrimary");
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

        private void TextBoxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ShowButton.Style = (System.Windows.Style)ShowButton.TryFindResource("FButtonPrimary");
        }

        #endregion

        private void tableView_CustomCellAppearance(object sender, CustomCellAppearanceEventArgs e)
        {
            if (e.ConditionalValue != null && e.Property != null)
            {
                var prop = e.Property;
                bool isBackgroundProp =
                    ReferenceEquals(prop, System.Windows.Controls.Control.BackgroundProperty) ||
                    ReferenceEquals(prop, System.Windows.Controls.Border.BackgroundProperty) ||
                    ReferenceEquals(prop, System.Windows.Controls.Panel.BackgroundProperty) ||
                    string.Equals(prop.Name, "Background", StringComparison.Ordinal);

                if (isBackgroundProp)
                {
                    e.Result = e.ConditionalValue;
                    e.Handled = true;
                }
            }
        }
    }

    public class CustomDateTimeConverter : JsonConverter<DateTime?>
    {
        private const string DateFormat = "dd.MM.yyyy";
        private const string DateTimeFormat = "dd.MM.yyyy HH:mm:ss";

        public override DateTime? ReadJson(JsonReader reader, Type objectType, DateTime? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
                return null;

            var dateString = reader.Value.ToString();
        
            if (DateTime.TryParseExact(dateString, DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                return result;
            
            if (DateTime.TryParseExact(dateString, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return result;
            
            return null;
        }

        public override void WriteJson(JsonWriter writer, DateTime? value, JsonSerializer serializer)
        {
            if (value.HasValue)
                writer.WriteValue(value.Value.ToString(DateTimeFormat));
            else
                writer.WriteNull();
        }
    }


    public class TkOrder
    {
        [JsonProperty("id_tk")]
        public int IdTk { get; set; }

        [JsonProperty("dttm")]
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime? Dttm { get; set; }

        [JsonProperty("num")]
        public string Num { get; set; }

        [JsonProperty("factory")]
        public string Factory { get; set; }

        [JsonProperty("flag_pculc")]
        public int? FlagPculc { get; set; }
        public bool FlagPculcBool => FlagPculc == 1;

        [JsonProperty("name_pok")]
        public string NamePok { get; set; }

        [JsonProperty("name_pclass")]
        public string NamePclass { get; set; }

        [JsonProperty("tk_size")]
        public string TkSize { get; set; }

        [JsonProperty("note")]
        public string Note { get; set; }

        [JsonProperty("cardboard")]
        public string Cardboard { get; set; }

        [JsonProperty("special_raw")]
        public int? SpecialRaw { get; set; }
        public bool SpecialRawBool => SpecialRaw == 1;

        [JsonProperty("printing")]
        public int? Printing { get; set; }
        public bool PrintingBool => Printing == 1;

        [JsonProperty("id_otgr")]
        public int? IdOtgr { get; set; }
        public bool IdOtgrBool => IdOtgr == 1;

        [JsonProperty("automatic")]
        public int? Automatic { get; set; }
        public bool AutomaticBool => Automatic == 1;

        [JsonProperty("design_status")]
        public int? DesignStatus { get; set; }

        [JsonProperty("drawing_status")]
        public int? DrawingStatus { get; set; }

        [JsonProperty("design")]
        public string Design { get; set; }

        [JsonProperty("drawing")]
        public string Drawing { get; set; }

        [JsonProperty("flag_chat_client")]
        public int? FlagChatClient { get; set; }

        [JsonProperty("flag_chat_inner")]
        public int? FlagChatInner { get; set; }

        [JsonProperty("web_file_client")]
        public int? WebFileClient { get; set; }
        public bool WebFileClientBool => WebFileClient == 1;

        [JsonProperty("web_file_our")]
        public int? WebFileOur { get; set; }
        public bool WebFileOurBool => WebFileOur == 1;

        [JsonProperty("agreed_flag")]
        public int? AgreedFlag { get; set; }
        public bool AgreedFlagBool => AgreedFlag == 1;

        [JsonProperty("file_signed_flag")]
        public int? FileSignedFlag { get; set; }
        public bool FileSignedFlagBool => FileSignedFlag == 1;

        [JsonProperty("eng_name")]
        public string EngName { get; set; }

        [JsonProperty("pr_cal_name")]
        public string PrCalName { get; set; }

        [JsonProperty("man_name")]
        public string ManName { get; set; }

        [JsonProperty("sales_name")]
        public string SalesName { get; set; }

        [JsonProperty("type_customer")]
        public string TypeCustomer { get; set; }

        [JsonProperty("head_confirm_flag")]
        public int? HeadConfirmFlag { get; set; }
        public bool HeadConfirmFlagBool => HeadConfirmFlag == 1;

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("order_by_1")]
        public int OrderBy1 { get; set; }

        [JsonProperty("order_by_2")]
        public int OrderBy2 { get; set; }

        [JsonProperty("order_by_3")]
        public int OrderBy3 { get; set; }

        [JsonProperty("hidden_flag")]
        public int? HiddenFlag { get; set; }

        [JsonProperty("duplicate")]
        public int? Duplicate { get; set; }

        [JsonProperty("scorer_print")]
        public int? ScorerPrint { get; set; }

        [JsonProperty("manager_creator_flag")]
        public int? ManagerCreatorFlag { get; set; }

        [JsonProperty("chat_id")]
        public int? ChatId { get; set; }

        [JsonProperty("tk_order_status")]
        public string TkOrderStatus { get; set; }

        [JsonProperty("id_pok")]
        public int IdPok { get; set; }

        [JsonProperty("acceptance_dttm")]
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime? AcceptanceDttm { get; set; }

        [JsonProperty("working_dttm")]
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime? WorkingDttm { get; set; }

        [JsonProperty("sending_for_approval_dttm")]
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime? SendingForApprovalDttm { get; set; }

        [JsonProperty("dttm_todo_des")]
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime? DttmTodoDes { get; set; }

        [JsonProperty("dttm_accep_des")]
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime? DttmAccepDes { get; set; }

        [JsonProperty("dttm_done_des")]
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime? DttmDoneDes { get; set; }

        [JsonProperty("dttm_todo_cons")]
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime? DttmTodoCons { get; set; }

        [JsonProperty("dttm_accep_cons")]
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime? DttmAccepCons { get; set; }

        [JsonProperty("dttm_done_cons")]
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime? DttmDoneCons { get; set; }

        [JsonProperty("dttm_waiting_asnwer")]
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime? DttmWaitingAsnwer { get; set; }

        [JsonProperty("product_name")]
        public string ProductName { get; set; }

        [JsonProperty("customer_carton")]
        public string CustomerCarton { get; set; }

        [JsonProperty("colors")]
        public string Colors { get; set; }

        [JsonProperty("design_complexity")]
        public string DesignComplexity { get; set; }

        [JsonProperty("drawing_complexity")]
        public string DrawingComplexity { get; set; }

        [JsonProperty("comments")]
        public string Comments { get; set; }

        [JsonProperty("des_name")]
        public string DesName { get; set; }

        [JsonProperty("con_name")]
        public string ConName { get; set; }

        [JsonProperty("designer_note")]
        public string DesignerNote { get; set; }

        [JsonProperty("drawer_note")]
        public string DrawerNote { get; set; }

        [JsonProperty("drawing_file")]
        public string? DrawingFile { get; set; }

        [JsonProperty("rework_cnt")]
        public int? ReworkCnt { get; set; }

        [JsonProperty("rework_reason")]
        public string ReworkReason { get; set; }

        [JsonProperty("not_read_client")]
        public int? NotReadClient { get; set; }

        [JsonProperty("not_read_inner")]
        public int? NotReadInner { get; set; }

        [JsonProperty("pathtk")]
        public string Pathtk { get; set; }

        [JsonProperty("file_signed")]
        public string FileSigned { get; set; }

        [JsonProperty("pathtk_work")]
        public string PathtkWork { get; set; }

        [JsonProperty("pathtk_new")]
        public string PathtkNew { get; set; }

        [JsonProperty("pathtk_archive")]
        public string PathtkArchive { get; set; }

        [JsonProperty("art")]
        public string Art { get; set; }
    }

    /// <summary>
    /// Конвертер для преобразования int (0/1) в bool для чекбоксов
    /// </summary>
    public class IntToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return false;
            
            if (value is int intValue)
            {
                return intValue == 1;
            }
            
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? 1 : 0;
            }
            
            return 0;
        }
    }

}