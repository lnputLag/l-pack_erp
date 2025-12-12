using System;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using System.Windows;
using Client.Interfaces.Service.Printing;
using static Client.Interfaces.Main.DataGridHelperColumn;
using Client.Interfaces.Sales;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Взаимодействие с приходами оснасток ( накладная, приход )
    /// </summary>
    /// <author>volkov_as</author>
    public partial class IncomeRigTab : ControlBase
    {
        public IncomeRigTab()
        {
            InitializeComponent();

            ControlSection = "income";
            RoleName = "[erp]rig_movement";
            ControlTitle = "Приход оснастки";
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/osnastka/prihod_osn";

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == "IncomePositionGrid")
                {
                    Commander.ProcessCommand(m.Action, m);
                }

                if (m.ReceiverName == "IncomeGrid")
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnLoad = () =>
            {
                IncomeGridInit();
                IncomePositionGridInit();
                FormInit();
            };

            OnUnload = () =>
            {
                IncomeGrid.Destruct();
                IncomePositionGrid.Destruct();
            };

            OnFocusGot = () =>
            {
                IncomeGrid.ItemsAutoUpdate = true;
                IncomePositionGrid.ItemsAutoUpdate = true;
            };

            OnFocusLost = () =>
            {
                IncomeGrid.ItemsAutoUpdate = false;
                IncomePositionGrid.ItemsAutoUpdate = false;
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
                }

                Commander.SetCurrentGridName("IncomeGrid");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "consignment_note_update",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "UpdateButton",
                        MenuUse = true,
                        ActionMessage = (ItemMessage message) =>
                        {
                            if(message != null)
                            {
                                var m = message.Message;
                                var id = m;
                                if(!id.IsNullOrEmpty())
                                {
                                    IncomeGrid.SetSelectedRowAfterUpdate(id);
                                }
                            }
                            IncomeGrid.LoadItems();
                        },
                        //Action = () =>
                        //{
                        //    IncomeGrid.LoadItems();
                        //    if (Commander.Message != null)
                        //    {
                        //        var m = Commander.Message.Message;

                        //        var id = m;

                        //        if (!id.IsNullOrEmpty())
                        //        { 
                        //            IncomeGrid.SelectRowByKey(id);
                        //        }
                        //    }
                        //},
                    });

                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "consignment_note_create",
                            Title = "Создать",
                            MenuUse = true,
                            Enabled = true,
                            ButtonUse = true,
                            ButtonName = "AddButtonFirst",
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var addNakl = new IncomeFrame();
                                addNakl.Create();
                            }
                        });

                        Commander.Add(new CommandItem()
                        {
                            Name = "consignment_note_edit",
                            Title = "Изменить",
                            MenuUse = true,
                            Enabled = true,
                            ButtonUse = true,
                            ButtonName = "EditButtonFirst",
                            HotKey = "Return|DoubleCLick",
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var editNakl = new IncomeFrame();
                                editNakl.Edit(PackingListId.ToInt());
                            }
                        });

                        Commander.Add(new CommandItem()
                        {
                            Name = "consignment_note_delete",
                            Title = "Удалить",
                            MenuUse = true,
                            Enabled = false,
                            ButtonUse = true,
                            ButtonName = "DeleteButtonFirst",
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            CheckEnabled = () => IncomePositionGrid.Items.Count == 0,
                            Action = () =>
                            {
                                IncomeDelete();
                            }
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "receipt_order_document",
                            Title = "Приходный ордер",
                            MenuUse = true,
                            Enabled = true,
                            AccessLevel = Role.AccessMode.ReadOnly,
                            CheckEnabled = () =>
                            {
                                var id = IncomeGrid.SelectedItem.CheckGet("NNAKL").ToInt();
                                
                                if (id != 0)
                                {
                                    return true;
                                }

                                return false;
                            },
                            Action = () =>
                            {
                                var id = IncomeGrid.SelectedItem.CheckGet("NNAKL").ToInt();
                                
                                ReceiptOrderDocument(id);
                            }
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "receipt_order_document_debug",
                            Title = "Приходный ордер (html)",
                            MenuUse = true,
                            Enabled = true,
                            AccessLevel = Role.AccessMode.ReadOnly,
                            CheckVisible = () =>
                            {
                                bool result = false;
                                if (Central.DebugMode)
                                {
                                    result = true;
                                }

                                return result;
                            },
                            CheckEnabled = () =>
                            {
                                bool result = false;
                                var id = IncomeGrid.SelectedItem.CheckGet("NNAKL").ToInt();
                                if (id != 0)
                                {
                                    if (Central.DebugMode)
                                    {
                                        result = true;
                                    }
                                }

                                return result;
                            },
                            Action = () =>
                            {
                                var id = IncomeGrid.SelectedItem.CheckGet("NNAKL").ToInt();

                                ReceiptOrderDocument(id, DocumentPrintManager.BaseDocumentFormat.HTML);
                            }
                        });
                    }
                }

                Commander.SetCurrentGridName("IncomePositionGrid");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "incoming_refresh",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "UpdateButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            IncomePositionGrid.LoadItems();
                        },
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "incoming_add",
                        Title = "Создать",
                        MenuUse = true,
                        Enabled = true,
                        ButtonUse = true,
                        ButtonName = "AddButtonSecond",
                        AccessLevel = Common.Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var addPrihod = new IncomePositionFrame();
                            addPrihod.Create( PackingListId, SupplierId);
                        }
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "incoming_edit",
                        Title = "Изменить",
                        MenuUse = true,
                        Enabled = false,
                        ButtonUse = true,
                        ButtonName = "EditButtonSecond",
                        AccessLevel = Common.Role.AccessMode.FullAccess,
                        CheckEnabled = () => PositionId.Length != 0 && PositionWriteOffId.Trim().Length == 0,
                        Action = () =>
                        {
                            var editPrihod = new IncomePositionFrame();
                            editPrihod.Edit(PackingListId, SupplierId, PositionId);
                        }
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "incoming_delete",
                        Title = "Удалить",
                        MenuUse = true,
                        Enabled = false,
                        ButtonUse = true,
                        ButtonName = "DeleteButtonSecond",
                        AccessLevel = Common.Role.AccessMode.FullAccess,
                        CheckEnabled = () => PositionId.Length != 0 && PositionWriteOffId.Trim().Length == 0,
                        Action = () =>
                        {
                            IncomePositionDelete();
                        }
                    });
                }

                Commander.Init(this);
            }
        }

        private string PackingListId { get; set; }
        private string SupplierId { get; set; }
        private string PositionId { get; set; } = "";
        private string PositionWriteOffId { get; set; } = "";
        private string ClisheOrderId { get; set; }
        private string ShtansFormOrderId { get; set; }
        private string RigOrderId { get; set; }

        public ListDataSet FirstGridList { get; set; }
        public ListDataSet SecondGridList { get; set; }
        public ListDataSet ListDataSetFilter { get; set; }

        public FormHelper Form { get; set; }

        public void FormInit()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="POSTAVSHIC_RIG",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=SelectProvider,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange=(FormHelperField f, string v) =>
                    {
                        IncomeGrid.LoadItems();
                    },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "Rig",
                        Object = "Supplier",
                        Action = "List",
                        AnswerSectionKey = "ITEMS",
                        OnComplete = (FormHelperField f, ListDataSet ds) =>
                        {
                            var row = new Dictionary<string, string>()
                            {
                                { "ID_POST", "0" },
                                { "NAME", "Все" },
                            };
                            ds.ItemsPrepend(row);
                            var list = ds.GetItemsList("ID_POST", "NAME");
                            var c = (SelectBox)f.Control;
                            if ( c != null)
                            {
                                c.Items = list;
                            }

                            SelectProvider.SetSelectedItemByKey("0");
                        }
                    }
                }
            };
            Form.SetFields(fields);
        }

        /// <summary>
        /// Инициализация первого грида
        /// </summary>
        public void IncomeGridInit()
        {
            var column = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="NNAKL",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 11,
                    Visible = true,
                    Exportable = true,
                },
                new DataGridHelperColumn
                {
                    Header="Дата прихода",
                    Path="DATA",
                    ColumnType = ColumnTypeRef.DateTime,
                    Width2 = 11,
                },
                new DataGridHelperColumn
                {
                    Header="№ накладной",
                    Path="NAME_NAKL",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 12,
                },
                new DataGridHelperColumn
                {
                    Header="№ СФ",
                    Path="NAMESF",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 12,
                    Description = "Счёт-фактура"
                },
                new DataGridHelperColumn
                {
                    Header="Поставщик",
                    Path="NAME_POST",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 19,
                },
                new DataGridHelperColumn
                {
                    Header="Стоимость, руб",
                    Path="SUM_CENAPOKR",
                    ColumnType = ColumnTypeRef.Double,
                    Width2 = 12,
                },
                new DataGridHelperColumn
                {
                    Header="Продавец",
                    Path="NAME_PROD",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 12,
                },
            };

            IncomeGrid.SetColumns(column);
            IncomeGrid.OnLoadItems = IncomeGridLoadItems;
            IncomeGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            IncomeGrid.SearchText = SearchText;
            IncomeGrid.SetPrimaryKey("NNAKL");
            IncomeGrid.Toolbar = FirstGridToolbar;
            
            IncomeGrid.OnFilterItems = () =>
            {
                if (IncomeGrid.Items.Count > 0)
                {
                    {
                        var v = Form.GetValues();

                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in IncomeGrid.Items)
                        {
                            if (v.CheckGet("POSTAVSHIC_RIG").ToInt() == 0)
                            {
                                items.Add(row);
                            } 
                            else if (v.CheckGet("POSTAVSHIC_RIG").ToInt() == row.CheckGet("ID_POST").ToInt())
                            {
                                items.Add(row);
                            }
                        }

                        IncomeGrid.Items = items;
                    }
                }
            };

            IncomeGrid.OnSelectItem = (Dictionary<string, string> selectedItem) =>
            {
                PackingListId = selectedItem.CheckGet("NNAKL");
                SupplierId = selectedItem.CheckGet("ID_POST");
                //IncomePositionGrid.LoadItems();
                IncomePositionGridLoadItems();
            };
            
            IncomeGrid.Commands = Commander;

            IncomeGrid.Init();
        }


        /// <summary>
        /// Инициализация второго грида
        /// </summary>
        public void IncomePositionGridInit()
        {
            var column = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="IDP",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 10,
                    Visible = true,
                    Exportable = true,
                },
                new DataGridHelperColumn
                {
                    Header="№",
                    Path="NUM_ORDER_RIG",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 4,
                    Description = "Порядковый номер в приходной накладной по оснастке"
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 51,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="ARTIKUL",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 14,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="KOL",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 11,
                },
                new DataGridHelperColumn
                {
                    Header="Цена, руб",
                    Path="CENAPOKR",
                    ColumnType = ColumnTypeRef.Double,
                    Width2 = 9,
                },
                new DataGridHelperColumn
                {
                    Header="Ед. изм",
                    Path="NAME_IZM",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "RIOR_ID",
                    Path = "RIOR_ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 15,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "SHOR_ID",
                    Path = "SHOR_ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 15,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "CLOT_ID",
                    Path = "CLOT_ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 15,
                    Visible = false
                }
            };

            IncomePositionGrid.SetColumns(column);
            IncomePositionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            IncomePositionGrid.SetPrimaryKey("IDP");
            IncomePositionGrid.AutoUpdateInterval = 0;
            IncomePositionGrid.ItemsAutoUpdate = false;
            //IncomePositionGrid.OnLoadItems = IncomePositionGridLoadItems;
            IncomePositionGrid.Toolbar = SecondGridToolbar;


            IncomePositionGrid.OnSelectItem = (Dictionary<string, string> selectedItem) =>
            {
                PositionId = selectedItem.CheckGet("IDP");
                PositionWriteOffId = selectedItem.CheckGet("IDP_R");
                ClisheOrderId = selectedItem.CheckGet("CLOT_ID");
                ShtansFormOrderId = selectedItem.CheckGet("SHOR_ID");
                RigOrderId = selectedItem.CheckGet("NUM_ORDER_RIG");
            };

            IncomePositionGrid.OnDblClick = (Dictionary<string, string> selectedItem) =>
            {
                var editPrihod = new IncomePositionFrame();
                editPrihod.Edit(PackingListId, SupplierId, PositionId);
            };

            IncomePositionGrid.Commands = Commander;

            IncomePositionGrid.Init();
        }

        /// <summary>
        /// Запрос для загрузки данных в первый грид
        /// </summary>
        public async void IncomeGridLoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "Income");
            q.Request.SetParam("Action", "List");

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() => { q.DoQuery(); });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                if (result != null)
                {
                    FirstGridList = ListDataSet.Create(result, "ITEMS");
                }
            }

            IncomeGrid.UpdateItems(FirstGridList);
        }

        /// <summary>
        /// Функция для формирования приходного ордера
        /// </summary>
        private async void ReceiptOrderDocument(int idIncome, DocumentPrintManager.BaseDocumentFormat documentFormat = DocumentPrintManager.BaseDocumentFormat.PDF)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "Income");
            q.Request.SetParam("Action", "GetReceiptOrder");
            q.Request.SetParam("NNAKL", idIncome.ToString());
            q.Request.SetParam("DOCUMENT_FORMAT_NAME", DocumentPrintManager.GetDocumentFormatName(documentFormat));

            await Task.Run(() => { q.DoQuery(); });

            if (q.Answer.Status == 0)
            {
                var printHelper = new PrintHelper();
                printHelper.PrintingProfile = PrintingSettings.DocumentPrinter.ProfileName;
                printHelper.Init();
                var printingResult = printHelper.ShowPreview(q.Answer.DownloadFilePath);
                printHelper.Dispose();
            }
        }

        /// <summary>
        /// Запрос для загрузки данных во второй грид ( зависит от выборной записи в первой гриде )
        /// </summary>
        public async void IncomePositionGridLoadItems()
        {
            if (!PackingListId.IsNullOrEmpty())
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Rig");
                q.Request.SetParam("Object", "IncomePosition");
                q.Request.SetParam("Action", "List");
                q.Request.SetParam("NNAKL", PackingListId);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() => { q.DoQuery(); });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                    if (result != null)
                    {
                        SecondGridList = ListDataSet.Create(result, "ITEMS");
                    }
                }

                IncomePositionGrid.UpdateItems(SecondGridList);
            }
        }

        /// <summary>
        /// Удаление прихода ( первый грид )
        /// </summary>
        private async void IncomeDelete()
        {
            var dialog = new DialogWindow($"Удалить приход №{PackingListId}?", "Удаление прихода", "",
                DialogWindowButtons.YesNo);
            dialog.ShowDialog();

            if (dialog.DialogResult == true)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Rig");
                q.Request.SetParam("Object", "Income");
                q.Request.SetParam("Action", "Delete");
                q.Request.SetParam("NNAKL", PackingListId);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() => { q.DoQuery(); });

                if (q.Answer.Status == 0)
                {
                    IncomeGridLoadItems();
                }
                else
                {
                    var msg = "Ошибка при удалении!";
                    var d = new DialogWindow(msg, "Проверка данных");
                    d.ShowDialog();
                }
            }
        }

        /// <summary>
        /// Удаление позиция из прихода ( второй грид )
        /// </summary>
        private async void IncomePositionDelete()
        {
            var dialog = new DialogWindow($"Удалить позицию №{PositionId} из прихода №{PackingListId}?",
                "Удаление позиции", "",
                DialogWindowButtons.YesNo);
            dialog.ShowDialog();

            if (dialog.DialogResult == true)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Rig");
                q.Request.SetParam("Object", "IncomePosition");
                q.Request.SetParam("Action", "Delete");
                q.Request.SetParam("NNAKL", PackingListId);
                q.Request.SetParam("IDP", PositionId);
                q.Request.SetParam("CLOT_ID", ClisheOrderId.ToInt().ToString());
                q.Request.SetParam("SHOR_ID", ShtansFormOrderId.ToInt().ToString());
                q.Request.SetParam("NUM_ORDER_RIG", RigOrderId);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() => { q.DoQuery(); });

                if (q.Answer.Status == 0)
                {
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverName = "IncomePositionGrid",
                        Action = "incoming_refresh",
                        Message = $"",
                    });
                }
                else
                {
                    var msg = "Ошибка при удалении!";
                    var d = new DialogWindow(msg, "Проверка данных");
                    d.ShowDialog();
                }
            }
        }
    }
}
