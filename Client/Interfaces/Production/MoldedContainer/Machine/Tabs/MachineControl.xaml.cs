using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Stock;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using Prism.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static Client.Interfaces.Main.ScannerInputControl;

namespace Client.Interfaces.Production.MoldedContainer
{
    /// <summary>
    /// интерфейс оператора агрегата литой тары
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2024-07-16</released>
    /// <changed>2025-08-29</changed>
    public partial class MachineControl : ControlBase
    {
        public MachineControl()
        {
            InitializeComponent();

            ControlSection = "machine_control";
            RoleName = "[erp]molded_contnr_operator";
            ControlTitle = "Оператор ВФМ";
            DocumentationUrl = "/doc/l-pack-erp/production/molded_container/machine_control";

            PalletPrintButton.Focusable = false;
            PalletDeleteButton.Focusable = false;
            
            OnMessage = (ItemMessage m) =>
            {
                if(
                    m.ReceiverName == ControlName
                    || m.ReceiverGroup == ControlSection
                )
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (KeyEventArgs e) =>
            {
                //var code = Input.WordScanned;
                //Central.Dbg($"code={code}");
                //if(!code.IsNullOrEmpty())
                //{
                //    var id = code.ToInt();
                //    PalletPost(id);
                //}

                ScannerInput.InputProcess(Input, e);

                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };

            OnLoad = () =>
            {
                InitTaskForm();
                SetDefaults();
                PalletGridInit();
            };

            OnUnload = () =>
            {
                PalletGrid.Destruct();

                ScannerInput.Dispose();
            };

            OnFocusGot = () =>
            {
                PalletGrid.ItemsAutoUpdate = true;
                PalletGrid.Run();

                ScannerInput.Init(this, false, false);
                ScannerInput.SetScanningStatus(ScannerInputControl.ScannerInputStatusRef.Enabled);
            };

            OnFocusLost = () =>
            {
                PalletGrid.ItemsAutoUpdate = false;

                ScannerInput.SetScanningStatus(ScannerInputControl.ScannerInputStatusRef.Disabled);
            };

            OnNavigate = () =>
            {
            };

            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "help",
                        Enabled = true,
                        Title = "Справка",
                        Description = "Показать справочную информацию",
                        ButtonUse = true,
                        ButtonName = "HelpButton",
                        HotKey = "F1",
                        Action = () =>
                        {
                            Central.ShowHelp(DocumentationUrl);
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "debug_message",
                        Enabled = true,
                        ActionMessage = (ItemMessage message) =>
                        {
                            var m = message.Message;
                            DebugLogUpdate(m);
                        },
                    });
                }

                Commander.SetCurrentGroup("custom");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "machine_next",
                        Enabled = true,
                        Title = "",
                        Description = "",
                        ButtonUse = true,
                        Control = CorrugatedMachineButton,
                        Action = () =>
                        {
                            var m = MachineGetNext(CurrentMachineId);
                            var id = m.CheckGet("MACHINE_ID").ToInt();
                            if(id != 0)
                            {
                                MachineUpdate(id);
                            }
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "pallet_create-1",
                        Enabled = true,
                        Title = "",
                        Description = "",
                        ButtonUse = true,
                        Control = PalletCreate1Button,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var h = new PalletCreate();
                            //DB:PROD_MACHINE_REF.ID_ST
                            h.Values.CheckAdd("MACHINE_ID", "301");
                            h.Values.CheckAdd("ID2", Machine301Id2.ToString());
                            h.Edit();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "pallet_create-2",
                        Enabled = true,
                        Title = "",
                        Description = "",
                        ButtonUse = true,
                        Control = PalletCreate2Button,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var h = new PalletCreate();
                            //DB:PROD_MACHINE_REF.ID_ST
                            h.Values.CheckAdd("MACHINE_ID", "302");
                            h.Values.CheckAdd("ID2", Machine302Id2.ToString());
                            h.Edit();
                        },
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "pallet_create-3",
                        Enabled = true,
                        Title = "",
                        Description = "",
                        ButtonUse = true,
                        Control = PalletCreate1AButton,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var h = new PalletCreate();
                            //DB:PROD_MACHINE_REF.ID_ST
                            h.Values.CheckAdd("MACHINE_ID", "303");
                            h.Values.CheckAdd("ID2", Machine303Id2.ToString());
                            h.Edit();
                        },
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "pallet_create-4",
                        Enabled = true,
                        Title = "",
                        Description = "",
                        ButtonUse = true,
                        Control = PalletCreate1BButton,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var h = new PalletCreate();
                            //DB:PROD_MACHINE_REF.ID_ST
                            h.Values.CheckAdd("MACHINE_ID", "304");
                            h.Values.CheckAdd("ID2", Machine304Id2.ToString());
                            h.Edit();
                        },
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "pallet_created",
                        Enabled = true,
                        ActionMessage = (ItemMessage message) =>
                        {
                            if(message.ContextObject != null)
                            {
                                var row = (Dictionary<string, string>)message.ContextObject;
                                if(row != null)
                                {
                                    PalletOnCreate(row);

                                    var id_st = row.CheckGet("MACHINE_ID").ToInt();
                                    var palletId2 = row.CheckGet("GOODS_ID").ToInt();

                                    switch (id_st)
                                    {
                                        case 301:
                                            Machine301Id2 = palletId2;
                                            break;
                                        case 302:
                                            Machine302Id2 = palletId2;
                                            break;
                                        case 303:
                                            Machine303Id2 = palletId2;
                                            break;
                                        case 304:
                                            Machine304Id2 = palletId2;
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                        },
                    });
                }

                 Commander.SetCurrentGroup("pallet");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "pallet_refresh",
                        Enabled = true,
                        Title = "",
                        Description = "",
                        ButtonUse = true,
                        Control = PalletRefreshButton,
                        Action = () =>
                        {
                            PalletGrid.LoadItems();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "pallet_delete",
                        Enabled = true,
                        Title = "",
                        Description = "",
                        ButtonUse = true,
                        Control = PalletDeleteButton,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            if(PalletGrid.SelectedItem!=null)
                            {
                                PalletDeleteConfirm(PalletGrid.SelectedItem);
                            }                            
                        },
                        CheckEnabled= ()=>
                        {
                            PalletDeleteButton.IsEnabled = false;
                            var result = false;
                            var k = PalletGrid.GetPrimaryKey();
                            var row = PalletGrid.SelectedItem;
                            if(row != null)
                            {
                                if(row.CheckGet("PALLET_POST").ToInt().ToString() == "0")
                                {
                                    PalletDeleteButton.IsEnabled = true;
                                    result = true;
                                }
                            }
                            return result;
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "pallet_print",
                        Enabled = true,
                        Title = "",
                        Description = "",
                        ButtonUse = true,
                        Control = PalletPrintButton,
                        Action = () =>
                        {
                            ReceiptProcess(0, PalletGrid.SelectedItem);
                            PalletGrid.Focus();
                        },
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "pallet_arrivial",
                        Enabled = true,
                        Title = "",
                        Description = "Оприходовать паллету",
                        ButtonUse = true,
                        Control = PalletArrivialButton,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            if (PalletGrid.SelectedItem != null)
                            {
                                PalletArrivialConfirm(PalletGrid.SelectedItem);
                            }
                        },
                        CheckEnabled = () =>
                        {
                            PalletArrivialButton.IsEnabled = false;
                            var result = false;
                            var k = PalletGrid.GetPrimaryKey();
                            var row = PalletGrid.SelectedItem;
                            if (row != null)
                            {
                                if (row.CheckGet("PALLET_POST").ToInt().ToString() == "0")
                                {
                                    PalletArrivialButton.IsEnabled = true;
                                    result = true;
                                }
                            }
                            return result;
                        },
                    });
                }

                Commander.SetCurrentGroup("debug");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "burger_button",
                        Enabled = true,
                        Title = "",
                        Description = "",
                        ButtonUse = true,
                        Control = BurgerButton,
                        Action = () =>
                        {
                            BurgerMenu.IsOpen = true;
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "label_print",
                        Enabled = true,
                        Title = "",
                        Description = "",
                        ButtonUse = true,
                        Control = LabelPrintButton,
                        Action = () =>
                        {

                        },
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "settings_printer",
                        Enabled = true,
                        Title = "",
                        Description = "",
                        ButtonUse = true,
                        Control = PrinterSettingsButton,
                        Action = () =>
                        {
                            string url = "l-pack://l-pack_erp/service/printing";
                            Central.Navigator.ProcessURL(url);
                        },
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "label_print_debug",
                        Enabled = true,
                        Title = "",
                        Description = "",
                        ButtonUse = true,
                        Control = DebugLabelPrintButton,
                        Action = () =>
                        {
                            ReceiptProcess(0, PalletGrid.SelectedItem);
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "label_view_debug",
                        Enabled = true,
                        Title = "",
                        Description = "",
                        ButtonUse = true,
                        Control = DebugLabelViewButton,
                        Action = () =>
                        {
                            ReceiptProcess(1, PalletGrid.SelectedItem);
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "label_view2_debug",
                        Enabled = true,
                        Title = "",
                        Description = "",
                        ButtonUse = true,
                        Control = DebugLabelView2Button,
                        Action = () =>
                        {
                            ReceiptProcess(2, PalletGrid.SelectedItem);
                        },
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "pallet_post_debug",
                        Enabled = true,
                        Title = "",
                        Description = "",
                        ButtonUse = true,
                        Control = DebugPalletPostButton,
                        Action = () =>
                        {
                            var palletId = PalletGrid.SelectedItem.CheckGet("PALLET_ID").ToInt();
                            if(palletId != 0)
                            {
                                PalletPost(palletId);
                            }
                        },
                    });

                    
                    //debug main
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "info",
                            Enabled = true,
                            Title = "",
                            Description = "",
                            ButtonUse = true,
                            Control = InfoButton,
                            Action = () =>
                            {
                                var t = "Отладочная информация";
                                var m = Central.MakeInfoString();
                                var i = new ErrorTouch();
                                i.Show(t, m);
                            },
                        });

                        Commander.Add(new CommandItem()
                        {
                            Name = "log",
                            Enabled = true,
                            Title = "",
                            Description = "",
                            ButtonUse = true,
                            Control = LogButton,
                            Action = () =>
                            {
                                var h = new LogTouch();
                                h.SetLogText(InnerLog);
                                h.Show();
                            },
                        });

                        Commander.Add(new CommandItem()
                        {
                            Name = "restart",
                            Enabled = true,
                            Title = "",
                            Description = "",
                            ButtonUse = true,
                            Control = RestartButton,
                            Action = () =>
                            {
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "Main",
                                    ReceiverName = "MainWindow",
                                    SenderName = "Navigator",
                                    Action = "Restart",
                                    Message = "",
                                });
                            },
                        });

                        Commander.Add(new CommandItem()
                        {
                            Name = "exit",
                            Enabled = true,
                            Title = "",
                            Description = "",
                            ButtonUse = true,
                            Control = ExitButton,
                            Action = () =>
                            {
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "Main",
                                    ReceiverName = "MainWindow",
                                    SenderName = "Navigator",
                                    Action = "Exit",
                                    Message = "",
                                });
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "f11",
                            Enabled = true,
                            Title = "",
                            Description = "",
                            ButtonUse = true,
                            Control = F11Button,
                            Action = () =>
                            {
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "Main",
                                    ReceiverName = "MainWindow",
                                    SenderName = "Navigator",
                                    Action = "SetScreenMode",
                                    Message = "fullscreentoggle",
                                });
                            },
                        });
                    }

                    //debug additional
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "resize1",
                            Enabled = true,
                            Title = "",
                            Description = "",
                            ButtonUse = true,
                            Control = Resize1Button,
                            Action = () =>
                            {
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "All",
                                    SenderName = "Navigator",
                                    Action = "Resize",
                                    Message = "800x600",
                                });
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "resize2",
                            Enabled = true,
                            Title = "",
                            Description = "",
                            ButtonUse = true,
                            Control = Resize2Button,
                            Action = () =>
                            {
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "All",
                                    SenderName = "Navigator",
                                    Action = "Resize",
                                    Message = "1280x800",
                                });
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "resize3",
                            Enabled = true,
                            Title = "",
                            Description = "",
                            ButtonUse = true,
                            Control = Resize3Button,
                            Action = () =>
                            {
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "All",
                                    SenderName = "Navigator",
                                    Action = "Resize",
                                    Message = "1280x720",
                                });
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "post_splash_debug",
                            Enabled = true,
                            Title = "",
                            Description = "",
                            ButtonUse = true,
                            Control = DebugSplashPost,
                            Action = () =>
                            {
                                PalletPostComplete(PalletGrid.SelectedItem);
                            },
                        });

                        
                    }
                }

                Commander.Init(this);
            }

            InnerLog = "";
            CurrentPalletId = 0;
            CurrentMachineId = 0;
            CurrentProductionTask = 0;
            MachineList = new List<Dictionary<string, string>>();
            MachineIds = "63,64,69,70";
            ProductionTaskIds = "";

            //по умолчанию Id2=521436 (Л-ПАК Полуфабрикат)
            Machine301Id2 = 521436;
            Machine302Id2 = 521436;
            Machine303Id2 = 521436;
            Machine304Id2 = 521436;

            if (!Central.DebugMode)
            {
                DebugButton.Visibility= Visibility.Collapsed;
            }

            Init();
        }

        private string InnerLog { get; set; }
        private int CurrentPalletId { get; set; }
        private int CurrentMachineId { get; set; }
        private int CurrentProductionTask { get; set; }
        private List<Dictionary<string,string>> MachineList { get; set; }
        private string MachineIds { get; set; }
        private string ProductionTaskIds { get; set; }
        private Timeout PanelClockTimeout { get; set; }
        private Timeout PanelDebugTimeout { get; set; }
        private Timeout GetCountTimeout { get; set; }
        
        public int Machine301Id2 { get; set; }
        public int Machine302Id2 { get; set; }
        public int Machine303Id2 { get; set; }
        public int Machine304Id2 { get; set; }

        /// <summary>
        /// Флаг того, в данный момент происходит обработка отсканированного ярлыка
        /// </summary>
        private bool ScaningInProgress { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper MachineControlForm { get; set; }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            ScannerInput.OnBarcodeProcess = ScannerInputProcess;
        }

        private void ScannerInputProcess(string code)
        {
            if (!code.IsNullOrEmpty())
            {
                var id = code.ToInt();
                PalletPost(id);
            }
        }

        private void DebugLogUpdate(string t)
        {
            {
                var time = DateTime.Now.ToString("HH:mm:ss");
                var s = Log.Text;
                s = s.AddCR();
                s = s.Append($"{time} {t}");
                s = s.TruncateHead(500);
                Log.Text = s;
                Log.ScrollToEnd();
            }

            {
                var time = DateTime.Now.ToString("HH:mm:ss fff");
                var s = InnerLog;
                s = s.AddCR();
                s = s.Append($"{time} {t}");
                s = s.TruncateHead(3000);
                InnerLog = s;
            }
        }

        /// <summary>
        /// вывод информации в блок с лейблом
        /// </summary>
        /// <param name="t"></param>
        private void DebugLabelUpdate(string t)
        {
            PanelDebug.Text = t;
            if(PanelDebugTimeout != null)
            {
                PanelDebugTimeout.Restart();
            }            
        }

        private void Init()
        {
            LogMsg("Запуск");
            DebugLabelUpdate("Запуск");

            PanelClockUpdate();
            PanelClockTimeout = new Timeout(
                1,
                () =>
                {
                    PanelClockUpdate();
                },
                true
            );
            PanelClockTimeout.Run();

            PanelDebugTimeout = new Timeout(
                3,
                () =>
                {
                    PanelDebug.Text = "";
                },
                true
            );
            PanelDebugTimeout.Run();

            GetCountTimeout = new Timeout(
                5,
                () =>
                {
                   MachineGetCount();
                },
                true
            );
            GetCountTimeout.Run();

            MachineListLoad();
            MachineGetCount();
        }

        private void PalletOnCreate(Dictionary<string, string> row)
        {
            var palletId = row.CheckGet("PALLET_ID").ToInt();
            if(palletId != 0)
            {
                LogMsg($"Поддон создан: #{row.CheckGet("PALLET_NUMBER_CUSTOM")} [{row.CheckGet("PALLET_ID")}]");
                CurrentPalletId = palletId;
            }

            PalletGrid.LoadItems();

            if(CurrentPalletId != 0)
            {
                ReceiptProcess(0, row);                
            }
        }

        private async void MachineListLoad()
        {
            var complete = false;
            string error = "";
            MachineList = new List<Dictionary<string, string>>();

            var p=new Dictionary<string, string>();
            {
                p.CheckAdd("MACHINE_IDS", MachineIds);
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Machine");
            q.Request.SetParam("Action", "ListByIds");

            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if(q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if(result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if(ds.Items.Count > 0)
                    {
                        MachineList = ds.Items;
                        complete = true;
                    }
                    else
                    {
                        error = "Нет списка агрегатов";
                    }
                }
            }
            else
            {
                //q.ProcessError();
                error = q.GetError();
            }


            if(complete)
            {
                ProductionTaskIds = "";
                foreach(Dictionary<string,string> row in MachineList)
                {
                    var s = row.CheckGet("PRODUCTION_TASK2_ID");
                    if(!s.IsNullOrEmpty())
                    {
                        ProductionTaskIds = ProductionTaskIds.AddComma();
                        ProductionTaskIds = ProductionTaskIds.Append(s.ToInt().ToString());
                    }
                    PalletGrid.LoadItems();
                }

            }
            else
            {
                LogMsg($"Ошибка получения данных {error}");
            }
        }

        /// <summary>
        /// установить интерфейс на указанную машину
        /// </summary>
        /// <param name="id"></param>
        private void MachineUpdate(int id)
        {
            var m = MachineGetById(id);
            if(m.Count > 0)
            {
                CurrentMachineId = m.CheckGet("MACHINE_ID").ToInt();
                CurrentProductionTask = m.CheckGet("PRODUCTION_TASK2_ID").ToInt(); 

                //CorrugatedMachineButton.Content= m.CheckGet("MACHINE_NAME");

                if(CurrentProductionTask != 0 && CurrentMachineId != 0)
                {
                    PalletGrid.LoadItems();
                    if(CurrentPalletId != 0)
                    {
                        PalletGrid.SelectRowByKey(CurrentPalletId.ToString());
                    }
                }
                else
                {
                    LogMsg($"Ошибка получения данных");
                }                
            }
        }

        private Dictionary<string,string> MachineGetById(int id)
        {
            var result = new Dictionary<string,string>();
            foreach(Dictionary<string, string> row in MachineList)
            {
                if(row.CheckGet("MACHINE_ID").ToInt() == id)
                {
                    result = row;
                    break;
                }
            }
            return result;
        }

        private Dictionary<string, string> MachineGetFirst()
        {
            var result = new Dictionary<string, string>();
            var j = 0;
            foreach(Dictionary<string, string> row in MachineList)
            {
                j++;
                if(j == 1)
                {
                    result = row;
                    break;
                }
            }
            return result;
        }

        private Dictionary<string, string> MachineGetNext(int id)
        {
            var result = new Dictionary<string, string>();
            var selected = false;
            foreach(Dictionary<string, string> row in MachineList)
            {
                if(selected)
                {
                    result = row;
                    break;
                }

                if(row.CheckGet("MACHINE_ID").ToInt() == id)
                {
                    selected = true;                    
                }
            }

            if(selected && result.Count == 0)
            {
                result = MachineGetFirst();
            }
            return result;
        }

        private void PanelClockUpdate()
        {
            var time = DateTime.Now.ToString("HH:mm:ss");
            PanelClock.Text = time;
        }

        private void PalletGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Номер",
                    Path="PALLET_NUMBER_CUSTOM",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="GOODS_QUANTITY",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Дата создания",
                    Path="PALLET_CREATED",
                    Description="",
                    ColumnType=ColumnTypeRef.DateTime,
                    Format="dd.MM.yyyy HH:mm",
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="PRODUCTION_TASK2_ID",
                    Path="PRODUCTION_TASK2_ID",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="PALLET_POST",
                    Path="PALLET_POST",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="ПЗ",
                    Path="PRODUCTION_TASK_NUMBER",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Станок",
                    Path="MACHINE_NAME",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="GOODS_NAME",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="ИД паллеты",
                    Path="PALLET_ID",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=9,
                    Visible=true,
                },
            };
            PalletGrid.SetColumns(columns);
            PalletGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        if(row.CheckGet("PALLET_POST").ToInt() == 1)
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
            };
            
            PalletGrid.SetPrimaryKey("PALLET_ID");
            PalletGrid.SetSorting("PALLET_NUMBER", ListSortDirection.Ascending);
            PalletGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            PalletGrid.QueryLoadItems = new RequestData()
            {
                Module = "MoldedContainer",
                Object = "Pallet",
                Action = "List",
                AnswerSectionKey = "ITEMS",
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                    {
                        { "PRODUCTION_TASK_IDS2", ProductionTaskIds.ToString() },
                    };
                    DebugLabelUpdate($"Загрузка");
                },
                AfterUpdate = (RequestData rd, ListDataSet ds) =>
                {
                    PalletGrid.SelectRowLast();
                    DebugLabelUpdate($"OK({ds.Items.Count})");
                },
            };
            PalletGrid.Commands = Commander;
            PalletGrid.Init();
        }

        /// <summary>
        /// работа с ярлыком
        /// mode: 0=печать pdf, 1=просмотр pdf, 2=просмотр html
        /// </summary>
        private bool ReceiptProcess(int mode = 0, Dictionary<string, string> row=null)
        {
            bool result = false;

            if(row != null)
            {
                var palletId = row.CheckGet("PALLET_ID").ToInt();
                var report = new LabelReport2(true);
                var label = report.PrintingProfileLabel;

                LogMsg($"Печать: #{row.CheckGet("PALLET_NUMBER_CUSTOM")} [{palletId}]");
                DebugLabelUpdate($"Печать");

                if(palletId != 0)
                {
                    switch(mode)
                    {
                        //печать pdf
                        default:
                        case 0:
                        if(Central.DebugMode)
                        {
                            //view pdf
                            report.ShowLabelPdf(palletId.ToString(), 0);
                        }
                        else
                        {
                            //print
                            report.PrintLabel(palletId.ToString());
                        }
                        break;

                        //просмотр pdf
                        case 1:
                        report.ShowLabelPdf(palletId.ToString(), 0);
                        break;

                        //просмотр html
                        case 2:
                        report.ShowLabelHtml(palletId.ToString(), 0);
                        break;
                    }
                }
            }

            return result;
        }

        private bool PalletDeleteConfirm(Dictionary<string, string> row = null)
        {
            bool result = false;

            if(row != null)
            {
                var palletId = row.CheckGet("PALLET_ID").ToInt();
                if(palletId != 0)
                {
                    string msg = "";
                    msg = msg.Append($"#{row.CheckGet("PALLET_NUMBER_CUSTOM")}");
                    var d = new DialogTouch($"{msg}", "Удалить поддон?", "", DialogWindowButtons.NoYes);
                    d.OnComplete = (DialogResultButton resultButton) =>
                    {
                        if(resultButton == DialogResultButton.Yes)
                        {
                            PalletDelete(row);
                        }

                    };
                }
            }

            return result;
        }

        private bool PalletPostComplete(Dictionary<string, string> row = null)
        {
            bool result = false;

            if(row != null)
            {
                var palletId = row.CheckGet("PALLET_ID").ToInt();
                if(palletId != 0)
                {
                    string msg = "";
                    msg = msg.Append($"#{row.CheckGet("PALLET_NUMBER_CUSTOM")}");
                    var d = new DialogTouch($"{msg}", "Поддон проведен", "", DialogWindowButtons.OKAutohide);
                }
            }

            return result;
        }

        private async void PalletDelete(Dictionary<string, string> row = null)
        {
            var complete = false;
            string error = "";
            var palletId = row.CheckGet("PALLET_ID").ToInt();

            LogMsg($"Удаление: #{row.CheckGet("PALLET_NUMBER_CUSTOM")} [{palletId}]");
            DebugLabelUpdate($"Удаление");

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("PALLET_ID", palletId.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "Delete");

            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if(q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if(result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    var first = ds.GetFirstItem();
                    var id = first.CheckGet("PALLET_ID").ToInt();
                    complete = first.CheckGet("RESULT").ToBool();
                }
            }
            else
            {
                //q.ProcessError();
                error = q.GetError();
            }


            if(complete)
            {
                PalletGrid.LoadItems();
                DebugLabelUpdate($"ОК");
            }
            else
            {
                LogMsg($"Ошибка удаления {error}");
                DebugLabelUpdate($"Ошибка");
            }
        }

        public void SetSplash(bool inProgressFlag, string msg = "Загрузка")
        {
            ScaningInProgress = inProgressFlag;
            SplashControl.Visible = inProgressFlag;

            SplashControl.Message = msg;
        }

        private async void PalletPost(int palletId = 0)
        {
            SetSplash(true, "Проведение");

            var complete = false;
            string error = "";
            var pallet = new Dictionary<string, string>();

            LogMsg($"Проведение: [{palletId}]");
            DebugLabelUpdate($"Проведение");

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("PALLET_ID", palletId.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "Post");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if(q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if(result != null)
                {
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        var first = ds.GetFirstItem();
                        var id = first.CheckGet("PALLET_ID").ToInt();
                        complete = first.CheckGet("RESULT").ToBool();
                    }

                    {
                        var ds = ListDataSet.Create(result, "PALLET");
                        pallet = ds.GetFirstItem();
                    }
                }
            }
            else if (q.Answer.Status == 145)
            {
                string msg = q.Answer.Error.Message;
                var d = new DialogTouch($"{msg}", "Информация", "", DialogWindowButtons.OKAutohide);
                LogMsg($"Ошибка проведения: {msg}");
            }

            if (complete)
            {
                PalletGrid.LoadItems();
                DebugLabelUpdate($"ОК");
                PalletPostComplete(pallet);
            }

            SetSplash(false);
            ScannerInput.SetScanningStatus(ScannerInputStatusRef.Enabled);
        }

        /// <summary>
        /// получаем значение счетчиков для ВФМ1 и ВФМ2
        /// </summary>
        private async void MachineGetCount()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Machine");
            q.Request.SetParam("Action", "GetCountByIds");

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
                    if (ds.Items.Count > 0)
                    {
                        var counter_qty_305 = ds.Items.FirstOrDefault().CheckGet("COUNTER_QTY").ToInt().ToString();
                        var counter_qty_306 = ds.Items.Last().CheckGet("COUNTER_QTY").ToInt().ToString();

                        Bfm2.Text = $"По счетчику {counter_qty_305}";
                        Bfm1.Text = $"По счетчику {counter_qty_306}";
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Оприходование палеты без сканирования ярлыка
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private bool PalletArrivialConfirm(Dictionary<string, string> row = null)
        {
            bool result = false;

            if (row != null)
            {
                var palletId = row.CheckGet("PALLET_ID").ToInt();
                if (palletId != 0)
                {
                    string msg = "";
                    msg = msg.Append($"#{row.CheckGet("PALLET_NUMBER_CUSTOM")}");
                    var d = new DialogTouch($"{msg}", "Оприходовать поддон?", "", DialogWindowButtons.NoYes);
                    d.OnComplete = (DialogResultButton resultButton) =>
                    {
                        if (resultButton == DialogResultButton.Yes)
                        {
                            if (palletId != 0)
                            {
                                PalletPost(palletId);
                            }
                        }
                    };
                }
            }

            return result;
        }

        /// <summary>
        /// Инициализация формы текущего задания
        /// </summary>
        public void InitTaskForm()
        {
            MachineControlForm = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="CURRENT_BARCODE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    //Control=BarCodeTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            MachineControlForm.SetFields(fields);
        }
    }
}
