using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Stock;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.MoldedContainer
{
    /// <summary>
    /// производственные задания на литую тару для станков 
    /// 311 - Принтер BST и 321- Этикетер BST
    /// </summary>
    /// <author>greshnyh_ni</author>
    /// <version>1</version>
    /// <released>2024-07-30</released>
    /// <changed>2024-08-02</changed>
    public partial class RecyclingTab : ControlBase
    {
    
        public FormHelper Form { get; set; }
        /// <summary>
        /// датасет, содержащий данные
        /// </summary>
        public ListDataSet ProductionTasksDS { get; set; }

        /// <summary>
        /// выбранная в гриде ПЗ запись
        /// </summary>
        public int SelectedItemId { get; set; }
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// текущий в работе Id_pz
        /// </summary>
        public int RecyclingGridIdPz { get; set; }

        /// <summary>
        /// текущий в работе Prot_Id
        /// </summary>
        public int RecyclingGridProtId { get; set; }


        /// <summary>
        /// Флаг того, что запрос при сканировании ярлыка ещё работает
        /// </summary>
        public bool QueryInProgress { get; set; }

        /// <summary>
        /// true - у текущего задания есть не оприходованные паллеты 
        /// </summary>
        public bool PalletNotArrivialFlag { get; set; }

        /// <summary>
        /// true - паллета уже проведена 
        /// </summary>
        public bool PalletArrivialFlag { get; set; }

        /// <summary>
        /// Список групп, в которые входит пользователь
        /// </summary>
        private List<string> UserGroups { get; set; }

        public RecyclingTab()
        {
            InitializeComponent();

            ControlSection = "recycling_control";
            RoleName = "[erp]developer";
            ControlTitle = "Переработка ЛТ";
            DocumentationUrl = "/doc/l-pack-erp/production/molded_container/recycling_control";

            Form = null;
            FormInit();
            SetDefaults();
            RecyclingGridInit();
            PalletPrihodGridInit();
            PalletRashodGridInit();

            OnMessage = (ItemMessage m) =>
            {
                if (
                   m.ReceiverName == ControlName
                   || m.ReceiverGroup == ControlSection
               )
                {
                    Commander.ProcessCommand(m.Action, m);
                    //ProcessMessage(m);
                }

            };


            OnKeyPressed = (KeyEventArgs e) =>
            {
                if(!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }

                if (!Input.WordScanned.IsNullOrEmpty())
                {
                    var s = $"{Input.WordScanned}";
                 
                    var id = Input.WordScanned.ToInt();
                    if (id != 0)
                    {
                        PalletPost(id);
                        Refresh();
                    }
                }

            };

            OnLoad =()=>
            {
            };

            OnUnload=()=>
            {
                RecyclingGrid.Destruct();
                PalletPrihodGrid.Destruct();
                PalletRashodGrid.Destruct(); 
            };

            OnFocusGot=()=>
            {
                RecyclingGrid.ItemsAutoUpdate = true;
                PalletPrihodGrid.ItemsAutoUpdate = true;
                PalletRashodGrid.ItemsAutoUpdate = true;

                RecyclingGrid.Run();
                PalletPrihodGrid.Run();
                PalletRashodGrid.Run();
            };

            OnFocusLost=()=>
            {
                RecyclingGrid.ItemsAutoUpdate = false;
                PalletPrihodGrid.ItemsAutoUpdate = false;
                PalletRashodGrid.ItemsAutoUpdate = false;
            };

            OnNavigate = () =>
            {
            };

            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "refresh",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonControl = ProductionTaskGridRefreshButton,
                        ButtonName = "ProductionTaskGridRefreshButton",
                        Action = () =>
                        {
                            Refresh();
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
                        Action = () =>
                        {
                            Central.ShowHelp(DocumentationUrl);
                        },
                    });
                }

                Commander.SetCurrentGridName("RecyclingGrid");
                {
                    //Commander.SetCurrentGroup("common");
                    //{
                    //}

                    //Commander.SetCurrentGroup("item");
                    //{
                        Commander.Add(new CommandItem()
                        {
                            Name = "production_task_start",
                            Title = "Начать ПЗ",
                            Description = "",
                            Group = "converting",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "ProductionTaskGridStartButton",
                            HotKey = "",
                          //  AccessLevel = Common.Role.AccessMode.AllowAll,
                            Action = () =>
                            {
                                ProductionTaskCounterStart();
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;

                                if ((RecyclingGrid.Items.Count() != 0)
                                && (RecyclingGrid.SelectedItem.CheckGet("TASK_STATUS_ID").ToInt() <= 3))
                                {
                                    result = true;
                                }
                                return result;

                            },
                        });
                    //}

                    //Commander.SetCurrentGroup("item");
                    //{
                        Commander.Add(new CommandItem()
                        {
                            Name = "production_task_end",
                            Title = "Закончить ПЗ",
                            Description = "",
                            Group = "converting",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "ProductionTaskGridEndButton",
                            HotKey = "",
                          //  AccessLevel = Common.Role.AccessMode.AllowAll,
                            Action = () =>
                            {
                                ProductionTaskCounterStart();
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;

                                if ((RecyclingGrid.Items.Count() != 0)
                                && (RecyclingGrid.SelectedItem.CheckGet("TASK_STATUS_ID").ToInt() == 4))
                                {
                                    result = true;
                                }
                                return result;

                            },
                        });
                    //}
                    
                    //Commander.SetCurrentGroup("item");
                    //{
                        Commander.Add(new CommandItem()
                        {
                            Name = "show_tech_card",
                            Title = "Открыть ТК",
                            Description = "",
                            Group = "converting",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "ShowExcelButton",
                          //  AccessLevel = Common.Role.AccessMode.AllowAll,
                            HotKey = "",
                            Action = () =>
                            {
                                var filePath = RecyclingGrid.SelectedItem.CheckGet("TK_FILE_PATH");
                                Central.OpenFile(filePath);
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var filePath = RecyclingGrid.SelectedItem.CheckGet("TK_FILE_PATH");
                                if ((RecyclingGrid.SelectedItem.CheckGet("TASK_ID").ToInt() != 0) &&
                                    (!filePath.IsNullOrEmpty()))
                                {
                                    if (File.Exists(filePath))
                                    {
                                        result = true;
                                    }
                                }

                                if ((RecyclingGrid.Items.Count() != 0)
                                && (RecyclingGrid.SelectedItem.CheckGet("TASK_ID").ToInt() != 0))
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });
                    //}

                    //{
                    Commander.Add(new CommandItem()
                    {
                        Name = "show_all_task",
                        Title = "Все ПЗ",
                        Description = "",
                        Group = "converting",
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "ShowAllPz",
                        HotKey = "",
                        Action = () =>
                        {
                            ShowAllProdictionTask();
                        },
                        CheckEnabled = () =>
                        {
                            var result = true;
                            return result;
                        },
                    });
                    //}

                }

                Commander.SetCurrentGridName("PalletPrihodGrid");
                {
                    //Commander.SetCurrentGroup("common");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "pallet_created",
                            Enabled = true,
                            Title = "Создать паллету",
                            Description = "Создать паллету",
                            Group = "pallet",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "PalletCreateButton",
                            Action = () =>
                            {
                                FormCreatePallet();
                            },
                            CheckEnabled = () =>
                            {
                                /// если есть активное задание для станка (смотрим prod_contenr_prod.id_st), тогда true
                                var result = false;
                                {
                                    result = GurrentTask();
                                }
                                return result;
                            },
                        });

                        Commander.Add(new CommandItem()
                        {
                            Name = "pallet_refresh",
                            Enabled = true,
                            ActionMessage = (ItemMessage message) =>
                            {
                                if (message.ContextObject != null)
                                {
                                    var row = (Dictionary<string, string>)message.ContextObject;
                                    if (row != null)
                                    {
                                        //PalletOnCreate(row);
                                        PalletPrihodGrid.LoadItems();
                                    }
                                }
                            },
                        });

                        Commander.Add(new CommandItem()
                        {
                            Name = "pallet_delete_button",
                            Enabled = true,
                            Title = "Удалить паллету",
                            Description = "удалить паллету",
                            Group = "pallet",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "PalletDeleteButton",
                            Action = () =>
                            {
                                DeletePallet();
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;

                                if ((PalletPrihodGrid.Items.Count() != 0) 
                                && (PalletPrihodGrid.SelectedItem.CheckGet("PALLET_POST").ToInt() == 0))
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });

                        Commander.Add(new CommandItem()
                        {
                            Name = "pallet_print_button",
                            Enabled = true,
                            Title = "Печать ярлыка",
                            Description = "Печать ярлыка",
                            Group = "pallet",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "PalletPrintButton",
                            Action = () =>
                            {
                                PrintPallet(); 
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;

                                if (PalletPrihodGrid.Items.Count() != 0)
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });
                    }
                }

                Commander.Init(this);
            }
        }

        /// <summary>
        /// Обработка общих сообщений
        /// </summary>
        /// <param name="action"></param>
        /// <param name="obj"></param>
        private void ProcessMessage(ItemMessage obj = null)
        {
            string action = obj.Action;
            switch (action)
            {
                case "Refresh":
                    RecyclingGrid.LoadItems();
                    break;
            }
        }


        /// <summary>
        // инициализация компонентов формы
        /// </summary>
        public void FormInit()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="SHOW_ALL_PALLET_RASHOD",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ShowAllPalletRashod,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },

                new FormHelperField()
                {
                    Path="SHOW_ALL_PALLET_PRIHOD",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ShowAllPalletPrihod,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },

            };

            Form.SetFields(fields);
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            {
                var list = new Dictionary<string, string>();
                list.Add("311", "Принтер BST[СPH-3430]-1");
                list.Add("321", "Этикетир. BST[TBH-2438]-1");
              
                Machines.Items = list;
                Machines.SelectedItem = list.FirstOrDefault((x) => x.Key == "311");
            }

            ShowAllPalletRashod.IsChecked = false;
            ShowAllPalletPrihod.IsChecked = false;
            PalletCreateButton.IsEnabled = false;
            PalletDeleteButton.IsEnabled = false;
            PalletPrintButton.IsEnabled = false;

        }

        /// <summary>
        /// список ПЗ для переработки на ЛТ
        /// </summary>
        public void RecyclingGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="TASK_ID",
                    Description="(prot_id)",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Номер",
                    Path="TASK_NUMBER",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Станок",
                    Path="MACHINE_ID",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                    Visible = false,
                },
                new DataGridHelperColumn
                {
                    Header="Изделие",
                    Path="GOODS_NAME",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=42,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="GOODS_CODE",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Схема производства",
                    Path="PRODUCTION_SCHEME_NAME",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="TASK_STATUS_TITLE",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="TASK_STATUS_ID",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=2,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header="Количество, шт",
                    Path="TASK_QUANTITY",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Произведено, шт",
                    Path="LABEL_QTY",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Оприходовано, шт",
                    Path="PRIHOD_QTY",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Списано, шт",
                    Path="RASHOD_QTY",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Заявка",
                    Path="ORDER_TITLE",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="ORDER_NOTE_GENERAL",
                    Description="примечание ОПП и складу",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                 new DataGridHelperColumn
                {
                    Header="ИДПЗ",
                    Path="TASK_ID2",
                    Description="(proiz_zad)",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="ИД позиции заявки",
                    Path="ORDER_POSITION_ID",
                    Description="(idorderdates)",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="ИД изделия",
                    Path="GOODS_ID",
                    Description="(id2)",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Количество на паллете",
                    Path="PER_PALLET_QTY",
                    Description="(tc.per_pallet_qty)",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                    Hidden=true,
                },
            };

            RecyclingGrid.SetColumns(columns);
            RecyclingGrid.SetPrimaryKey("TASK_ID");
           // RecyclingGrid.SetSorting("TASK_ID", ListSortDirection.Ascending);
            RecyclingGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            RecyclingGrid.Toolbar = ProductionTaskGridToolbar;
           
            RecyclingGrid.AutoUpdateInterval = 0;
            RecyclingGrid.OnLoadItems = RecyclingLoadItems;
            RecyclingGrid.Commands = Commander;

            RecyclingGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    {
                        StylerTypeRef.BackgroundColor,
                        (Dictionary<string, string> row) =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            // Зеленый -- в работе
                            if(row.ContainsKey("TASK_STATUS_ID"))
                            {
                                if (row.CheckGet("TASK_STATUS_ID").ToInt() == 4)
                                {
                                    color = HColor.Green;
                                }
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };

            RecyclingGrid.Init();
        }

        /// <summary>
        /// начало/завершение выполнения ПЗ
        /// </summary>
        private async void ProductionTaskCounterStart()
        {
            bool resume = true;
            var id = RecyclingGrid.SelectedItem.CheckGet("TASK_ID").ToInt();
            var status = RecyclingGrid.SelectedItem.CheckGet("TASK_STATUS_ID").ToInt();
            var num = RecyclingGrid.SelectedItem.CheckGet("TASK_NUMBER").ToString();

            if (status == 4)
            {
                ProductionTaskInfo();
                if (PalletNotArrivialFlag)
                {
                    resume = false;
                }
            }

            if (id > 0)
            {
                var mes = status == 4 ? "закончить" : "начать";

                if (resume)
                {
                    var dw = new DialogWindow($"Вы действительно хотите {mes} производственное задание №{num}?", "Работа с ПЗ", "", DialogWindowButtons.NoYes);
                    if (dw.ShowDialog() == true)
                    {
                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "MoldedContainer");
                        q.Request.SetParam("Object", "Recycling");
                        q.Request.SetParam("Action", "Save");

                        if (status < 4)
                        {
                            q.Request.SetParam("TASK_ID", id.ToString());
                        }
                        else
                        {
                            q.Request.SetParam("TASK_ID", "");
                        }

                        q.Request.SetParam("PRODUCTION_MACHINE_ID", RecyclingGrid.SelectedItem.CheckGet("MACHINE_ID").ToInt().ToString());

                        await Task.Run(() =>
                        {
                            q.DoQuery();
                        });

                        if (q.Answer.Status == 0)
                        {
                            RecyclingGrid.LoadItems();
                        }
                        else
                        {
                            q.ProcessError();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// выбираем станок
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void Machines_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ShowAllPalletRashod.IsChecked = false;
            ShowAllPalletPrihod.IsChecked = false;
            PalletCreateButton.IsEnabled = false;
            PalletDeleteButton.IsEnabled = false;
            PalletPrintButton.IsEnabled = false;

            GurrentTask();
            RecyclingGrid.LoadItems();
            PalletPrihodGrid.LoadItems();
            PalletRashodGrid.LoadItems();
        }


        /// <summary>
        /// список паллет для выбранного станка
        /// </summary>
        public void PalletPrihodGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="№ паллета",
                    Path="PALLET_NUMBER_CUSTOM",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=9,
                },
                new DataGridHelperColumn
                {
                    Header="Количество, шт",
                    Path="GOODS_QUANTITY",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Дата создания",
                    Path="PALLET_CREATED",
                    ColumnType=ColumnTypeRef.DateTime,
                    Format="dd.MM.yyyy HH:mm:ss",
                    Description="",
                    Width2=14,
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="PRODUCTION_TASK_ID",
                    Description="prot_id",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Оприходован",
                    Path="PALLET_POST",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header="ПЗ",
                    Path="PRODUCTION_TASK2_ID",
                    Description="id_pz",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                    Hidden = true
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="GOODS_NAME",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=28,
                },
                new DataGridHelperColumn
                {
                    Header="ИД паллета",
                    Path="PALLET_ID",
                    Description="(id_poddon)",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=9,
                },

            };

            PalletPrihodGrid.SetColumns(columns);
            PalletPrihodGrid.SetPrimaryKey("PALLET_ID");
          //  PalletPrihodGrid.SetSorting("PALLET_NUMBER_CUSTOM", ListSortDirection.Ascending);
            PalletPrihodGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            PalletPrihodGrid.OnLoadItems = PalletPrihodGridLoadItems;

            PalletPrihodGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    {
                        StylerTypeRef.BackgroundColor,
                        (Dictionary<string, string> row) =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            // Зеленый -- оприходован
                            if(row.ContainsKey("PALLET_POST"))
                            {
                                if (row.CheckGet("PALLET_POST").ToInt() == 1)
                                {
                                    color = HColor.Green;
                                }
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
            };

            PalletPrihodGrid.Commands = Commander;
            PalletPrihodGrid.Init();
        }


        /// <summary>
        /// список списанных паллет
        /// </summary>
        public void PalletRashodGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="№ паллеты",
                    Path="PALLET_NUMBER_CUSTOM",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=9,
                },
                new DataGridHelperColumn
                {
                    Header="Количество, шт",
                    Path="GOODS_QUANTITY",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Дата списания",
                    Path="PALLET_RASHOD",
                    ColumnType=ColumnTypeRef.DateTime,
                    Format="dd.MM.yyyy HH:mm:ss",
                    Description="",
                    Width2=14,
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="PRODUCTION_TASK_ID",
                    Description="prot_id",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Станок",
                    Path="NAME",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=28,
                },
                new DataGridHelperColumn
                {
                    Header="ИД паллета",
                    Path="PALLET_ID",
                    Description="(id_poddon)",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=9,
                },
            };

            PalletRashodGrid.SetColumns(columns);
            PalletRashodGrid.SetPrimaryKey("PALLET_ID");
            //  PalletPrihodGrid.SetSorting("PALLET_NUMBER_CUSTOM", ListSortDirection.Ascending);
            PalletRashodGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            PalletRashodGrid.OnLoadItems = PalletRashodGridLoadItems;

            PalletRashodGrid.Commands = Commander;
            PalletRashodGrid.Init();
        }


        /// <summary>
        /// вызов формы для создания паллеты по текущему ПЗ
        /// </summary>
        private void FormCreatePallet()
        {
            var h = new RecyclingPalletCreate();

            // получаем активное текущие Id_pz
            GurrentTask();

            h.Values.CheckAdd("TASK_ID", RecyclingGridProtId.ToInt().ToString());
            h.ReceiverName = ControlName;
            h.Edit();

            Refresh();
            //RecyclingGrid.LoadItems();
            //PalletPrihodGrid.LoadItems();
        }

        public void PrintPallet()
        {
            LabelReport2 report = new LabelReport2(true);
            report.PrintLabel(PalletPrihodGrid.SelectedItem.CheckGet("PALLET_ID").ToInt().ToString());
            // report.ShowLabelHtml(PalletPrihodGrid.SelectedItem.CheckGet("PALLET_ID"));
        }

        /// <summary>
        /// Установка настроек для принтера
        /// </summary>
        public void SetPrintSettings()
        {
            var report = new LabelReport2(true);
            report.SetPrintingProfile();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }

        /// <summary>
        /// оприходование паллеты для текущего ПЗ
        /// </summary>
        private void ArrivialPallet(Dictionary<string, string> p)
        {
            var palleta = p.CheckGet("PALLET_ID").ToString();
            var palletaName = p.CheckGet("PALLET_NUMBER_CUSTOM").ToString();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "PalletArrivial");
            q.Request.SetParams(p);

            q.Request.Timeout = 5000;
            q.Request.Attempts = 1;

            //await Task.Run(() =>
            //{
                q.DoQuery();
            //});

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var dataSet = ListDataSet.Create(result, "ITEMS");
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        if (dataSet.Items.First().CheckGet("ID").ToInt() == 0)
                        {
                            // паллет оприходован успешно
                            string msg = $"Паллета [{palletaName}] оприходована успешно!{Environment.NewLine}.";
                            int status = 2;
                            var d = new StackerScanedLableInfo($"{msg}", status);
                            d.WindowMaxSizeFlag = true;
                            d.ShowAndAutoClose(2);
                        }
                    }
                }
            }
            else if (q.Answer.Status == 145)
            {
                string msg = q.Answer.Error.Message;
                int status = 1;
                var d = new StackerScanedLableInfo(msg, status);
                d.WindowMaxSizeFlag = true;
                d.ShowAndAutoClose(2);
            }
        }

        /// <summary>
        /// списание паллеты с заготовками
        /// </summary>
        private void СonsumptionPallet(Dictionary<string, string> p)
        {
            var palleta = p.CheckGet("PALLET_ID").ToString();
            var palletaName = p.CheckGet("PALLET_NUMBER_CUSTOM").ToString();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "PalletСonsumption");
            q.Request.SetParams(p);

            q.Request.Timeout = 5000;
            q.Request.Attempts = 1;

            //await Task.Run(() =>
            //{
            q.DoQuery();
            //});

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var dataSet = ListDataSet.Create(result, "ITEMS");
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        if (dataSet.Items.First().CheckGet("ID").ToInt() == 0)
                        {
                            // паллета списана успешно
                            string msg = $"Паллета [{palletaName}] списана успешно!{Environment.NewLine}.";
                            int status = 2;
                            var d = new StackerScanedLableInfo($"{msg}", status);
                            d.WindowMaxSizeFlag = true;
                            d.ShowAndAutoClose(2);
                        }
                    }
                }
            }
            else if (q.Answer.Status == 145)
            {
                string msg = q.Answer.Error.Message;
                int status = 1;
                var d = new StackerScanedLableInfo(msg, status);
                d.WindowMaxSizeFlag = true;
                d.ShowAndAutoClose(2);
            }

//            BarCodeTextBox.IsReadOnly = false;
            QueryInProgress = false;
        }

        /// <summary>
        /// информация о паллете (списывать или оприходовать)
        /// </summary>
        private void InfoPallet(string str)
        {
            var p = new Dictionary<string, string>();
            {
                p.Add("ID_PODDON", str);
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "PalletInfo");
            q.Request.SetParams(p);

            q.Request.Timeout = 5000;
            q.Request.Attempts = 1;

            //await Task.Run(() =>
            //{
            q.DoQuery();
            //});

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var dataSet = ListDataSet.Create(result, "ITEMS");
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        // для какого станка была создана паллета
                        var idSt = dataSet.Items.First().CheckGet("ID_ST").ToInt();
                        // название паллеты
                        var palletName = dataSet.Items.First().CheckGet("NAME_PALLET").ToString();

                        if ((idSt == 301) || (idSt == 302) || (idSt == 303) || (idSt == 304))
                        {
                            // получаем текущие Id_pz и на каком станке производят изделия
                            GurrentTask();
                            if ((RecyclingGridIdPz > 0) && (Machines.SelectedItem.Key.ToInt() > 0))
                            {
                                var p2 = new Dictionary<string, string>();
                                {
                                    p2.Add("PALLET_ID", str);
                                    p2.Add("ID_PZ", RecyclingGridIdPz.ToString());
                                    p2.Add("ID_ST", Machines.SelectedItem.Key.ToString());
                                    p2.Add("PALLET_NUMBER_CUSTOM", palletName);
                                }

                                // списываем паллету с заготовкой
                                СonsumptionPallet(p2);
                            }

                        }
                        else if ((idSt == 311) || (idSt == 321) || (idSt == 322))
                        {
                            var p2 = new Dictionary<string, string>();
                            {
                                p2.Add("PALLET_ID", str);
                                p2.Add("PALLET_NUMBER_CUSTOM", palletName);
                            }

                            // оприходуем паллету
                            ArrivialPallet(p2);
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Обработчик ввода
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;

            switch (e.Key)
            {
                case Key.F1:
                    //ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Down:
                case Key.Enter:
                    break;
            }
        }
        
        /// <summary>
        /// Загрузка данных в список ПЗ для ЛТ
        /// </summary>
        private async void RecyclingLoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "List");
            q.Request.SetParam("MACHINE_ID", Machines.SelectedItem.Key.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                // Очищаем все зависимые таблицы
                PalletPrihodGrid.Items.Clear();
                PalletPrihodGrid.ClearItems();

                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    RecyclingGrid.UpdateItems(ds);
                }
            }
        }

        /// <summary>
        /// загрузка данных грида списка созданных паллет для станка
        /// </summary>
        private async void PalletPrihodGridLoadItems()
        {
            Central.Parameters.GlobalDebugOutput = true;
            Profiler profiler = new Profiler();
    //        Central.Dbg($"[!] [1] [{DateTime.Now}] [{profiler.GetDelta()}] GET PalletPrihodGrid DATA");

            var p = new Dictionary<string, string>();
            {
              p.Add("ID_ST", Machines.SelectedItem.Key.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "PalletList");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestGridAttempts;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            var dataSet = new ListDataSet();
            int i = 0;

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    dataSet = ListDataSet.Create(result, "ITEMS");
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        //                      Central.Dbg($"[!] [2] [{DateTime.Now}] [{profiler.GetDelta()}] GET PalletPrihodGrid DATA");
                        i = dataSet.Items.Count(x => x.CheckGet("PALLET_ID").ToInt() > 0);
                        PalletPrihodGrid.UpdateItems(dataSet);

                    }
                }
            }
            else
            {
                q.ProcessError();
            }

//            Central.Dbg($"[!] [3] [{DateTime.Now}] [{profiler.GetDelta()}] [{i}] GET PalletPrihodGrid DATA");
      

            PalletPrihodGrid.UpdateItems(dataSet);
        }

        /// <summary>
        /// загрузка данных грида списка списанных паллет
        /// </summary>
        private async void PalletRashodGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            {
                p.Add("ID_ST", Machines.SelectedItem.Key.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "PalletСonsumptionList");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestGridAttempts;

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
                    PalletRashodGrid.UpdateItems(ds);
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// удаляем созданную, но еще не оприходованную паллету с готовой продукцией
        /// </summary>
        private async void DeletePallet()
        {
            var id = PalletPrihodGrid.SelectedItem.CheckGet("PALLET_ID").ToInt();
            if (id > 0)
            {
                var dw = new DialogWindow("Вы действительно хотите удалить паллету?", "Удаление паллеты", "Подтверждение удаления.", DialogWindowButtons.NoYes);
                if (dw.ShowDialog() == true)
                {
                    var p = new Dictionary<string, string>();
                    {
                        p.Add("PALLET_ID", id.ToString());
                    }

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "MoldedContainer");
                    q.Request.SetParam("Object", "Recycling");
                    q.Request.SetParam("Action", "DeletePallet");
                    q.Request.SetParams(p);

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestGridAttempts;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if (q.Answer.Status == 0)
                    {
                        PalletPrihodGrid.LoadItems();
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }
        }


        /// <summary>
        /// информация по активному заданию (наличие не оприходованных паллет)
        /// </summary>
        private void ProductionTaskInfo()
        {
            PalletNotArrivialFlag = false;

            var p = new Dictionary<string, string>();
            {
                p.Add("ID_PZ", RecyclingGrid.SelectedItem.CheckGet("TASK_ID2").ToInt().ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "TaskInfo");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestGridAttempts;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");

                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        // var idPz = ds.Items.First().CheckGet("ID_PZ").ToInt();
                        var t = "Внимание информация!";
                        var m = $"Для текущего задания {RecyclingGrid.SelectedItem.CheckGet("TASK_ID").ToInt()} \nесть не оприходованные паллеты.";
                        var i = new ErrorTouch();
                        i.Show(t, m);
                        PalletNotArrivialFlag = true;
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        ////разбираем штрих
        /// </summary>
        private void PalletPost(int strih)
        {
            {
                    var str = strih.ToString();
                    InfoPallet(str);
            }
        }


        /// <summary>
        /// получаем наличие начатого задания для выбранного станка
        /// </summary>
        private bool GurrentTask()
        {
            var res = false;
            RecyclingGridIdPz = 0;
            RecyclingGridProtId = 0;
            var p = new Dictionary<string, string>();
            {
                p.Add("MACHINE_ID", Machines.SelectedItem.Key.ToString());
            }
            
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "GetCurrentTask");
         
            q.Request.SetParams(p);

            q.Request.Timeout = 5000;
            q.Request.Attempts = 1;

//            await Task.Run(() =>
//            {
            q.DoQuery();
//            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var dataSet = ListDataSet.Create(result, "ITEMS");
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        if (dataSet.Items.First().CheckGet("PRODUCTION_TASK_ID").ToInt() != 0)
                        {
                            RecyclingGridProtId = dataSet.Items.First().CheckGet("PRODUCTION_TASK_ID").ToInt();
                            RecyclingGridIdPz = dataSet.Items.First().CheckGet("PRODUCTION_TASK2_ID").ToInt();
                            res = true;
                        }
                    }
                }
            }

            return res;
        }


        /// <summary>
        /// обновляем все гриды
        /// </summary>
        private void Refresh()
        {
            RecyclingGrid.LoadItems();
            PalletPrihodGrid.LoadItems();
            PalletRashodGrid.LoadItems();
        }

  
        /// <summary>
        /// все паллеты списанные
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowAllPalletRashod_Click(object sender, RoutedEventArgs e)
        {
            var showAll = false;
            var v = Form.GetValues();

            if (v.CheckGet("SHOW_ALL_PALLET_RASHOD").ToBool())
            {
                showAll = true;
            }

            if (showAll)
            {
                PalletRashodGrid.OnLoadItems = PalletRashodGridLoadAllItems;
            }
            else
            {
                PalletRashodGrid.OnLoadItems = PalletRashodGridLoadItems;
            }

            PalletRashodGrid.LoadItems();
        }

        /// <summary>
        ////все паллеты с готовой продукцией
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowAllPalletPrihod_Click(object sender, RoutedEventArgs e)
        {
            var showAll = false;
            var v = Form.GetValues();

            if (v.CheckGet("SHOW_ALL_PALLET_PRIHOD").ToBool())
            {
                showAll = true;
            }

            if (showAll)
            {
                PalletPrihodGrid.OnLoadItems = PalletPrihodGridLoadAllItems;
            }
            else
            {
               PalletPrihodGrid.OnLoadItems = PalletPrihodGridLoadItems;
            }

            PalletPrihodGrid.LoadItems();
        }


        /// <summary>
        /// загрузка данных грида списка созданных паллет для станка (все паллеты)
        /// </summary>
        private async void PalletPrihodGridLoadAllItems()
        {
            var p = new Dictionary<string, string>();
            {
                p.Add("ID_ST", Machines.SelectedItem.Key.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "PalletAllList");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestGridAttempts;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            var dataSet = new ListDataSet();
            int i = 0;

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    dataSet = ListDataSet.Create(result, "ITEMS");
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        PalletPrihodGrid.UpdateItems(dataSet);
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// загрузка данных грида списка списанных паллет с заготовками для станка (все паллеты)
        /// </summary>
        private async void PalletRashodGridLoadAllItems()
        {
            var p = new Dictionary<string, string>();
            {
                p.Add("ID_ST", Machines.SelectedItem.Key.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "PalletСonsumptionAllList");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestGridAttempts;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            var dataSet = new ListDataSet();
            int i = 0;

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    dataSet = ListDataSet.Create(result, "ITEMS");
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        PalletRashodGrid.UpdateItems(dataSet);
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        ////отображаем список ранее выполненых ПЗ для текущего станка
        /// </summary>
        private void ShowAllProdictionTask()
        {

            var h = new RecyclingPtoductionTaskAll();
            h.IdSt = Machines.SelectedItem.Key.ToString();
            h.Edit();
        }



    }
}
