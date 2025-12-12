using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Economics.MoldedContainer
{
    /// <summary>
    /// Список договоров и спецификаций для литой тары
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class MoldedContainerSpecificationTab : ControlBase
    {
        /// <summary>
        /// Список договоров и спецификаций для литой тары
        /// </summary>
        public MoldedContainerSpecificationTab()
        {
            ControlTitle = "Спецификации ЛТ";
            DocumentationUrl = "/doc/l-pack-erp/economics/molded_container_specification";
            RoleName = "[erp]molded_contnr_specification";

            InitializeComponent();

            InitBuyerGrid();
            InitContractGrid();
            InitSpecificationGrid();
            InitPositionGrid();

            OnLoad = () =>
            {

            };

            OnUnload = () =>
            {
                BuyerGrid.Destruct();
                ContractGrid.Destruct();
                SpecificationGrid.Destruct();
                PositionGrid.Destruct();
            };


            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverName == ControlName)
                {
                    ProcessMessage(msg.Action, msg);
                }
            };

            Commander.SetCurrentGroup("main");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "refresh",
                    Group = "main",
                    Enabled = true,
                    Title = "Обновить",
                    Description = "Обновить данные",
                    ButtonUse = true,
                    ButtonName = "RefreshButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    MenuUse = true,
                    Action = () =>
                    {
                        BuyerGrid.LoadItems();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "help",
                    Group = "main",
                    Enabled = true,
                    Title = "Справка",
                    Description = "Показать справочную информацию",
                    ButtonUse = true,
                    ButtonName = "HelpButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    HotKey = "F1",
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
            }

            {
                Commander.SetCurrentGridName("ContractGrid");
                //Commander.SetCurrentGroup("contract");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "create_contract",
                        Title = "Добавить",
                        Group = "contract",
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "CreateContractButton",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            int сontractorId = BuyerGrid.SelectedItem.CheckGet("CONTRACTOR_ID").ToInt();

                            var contractFrame = new MoldedContainerContract();
                            contractFrame.ReceiverName = ControlName;
                            contractFrame.ContractorId = сontractorId;
                            contractFrame.Edit();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            // Проверяем, что выбран покупатель, чтобы привязать к нему новый договор
                            var row = BuyerGrid.SelectedItem;
                            if (row != null)
                            {
                                if (row.CheckGet("ID").ToInt() != 0)
                                {
                                    result = true;
                                }
                            }
                            return result;
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "edit_contract",
                        Title = "Изменить",
                        Group = "contract",
                        MenuUse = true,
                        HotKey = "Return|DoubleCLick",
                        ButtonUse = true,
                        ButtonName = "EditContractButton",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var k = ContractGrid.GetPrimaryKey();
                            var id = ContractGrid.SelectedItem.CheckGet(k).ToInt();
                            if (id != 0)
                            {
                                var contractFrame = new MoldedContainerContract();
                                contractFrame.ReceiverName = ControlName;
                                contractFrame.Edit(id);
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var k = ContractGrid.GetPrimaryKey();
                            var row = ContractGrid.SelectedItem;
                            if (row != null)
                            {
                                if (row.CheckGet("ID").ToInt() != 0)
                                {
                                    result = true;
                                }
                            }
                            return result;
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "delete_contract",
                        Title = "Удалить",
                        Group = "contract",
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "DeleteContractButton",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var k = ContractGrid.GetPrimaryKey();
                            var id = ContractGrid.SelectedItem.CheckGet(k).ToInt();
                            if (id != 0)
                            {
                                DeleteContract(id);
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var k = ContractGrid.GetPrimaryKey();
                            var row = ContractGrid.SelectedItem;
                            if (row != null)
                            {
                                if (row.CheckGet("ID").ToInt() != 0)
                                {
                                    result = true;
                                }
                            }
                            return result;
                        },
                    });
                }
                {
                    Commander.SetCurrentGridName("SpecificationGrid");
                    //Commander.SetCurrentGroup("specification");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "create_specification",
                            Title = "Добавить",
                            Group = "specification",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "CreateSpecificationButton",
                            AccessLevel = Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                int сontractId = ContractGrid.SelectedItem.CheckGet("ID").ToInt();

                                if (сontractId > 0)
                                {
                                    var specificationFrame = new MoldedContainerSpecification();
                                    specificationFrame.ReceiverName = ControlName;
                                    specificationFrame.ContractId = сontractId;
                                    specificationFrame.Edit();
                                }
                                else
                                {
                                    var dw = new DialogWindow("Не выбран договор", "Создать спецификацию");
                                    dw.ShowDialog();
                                }
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                // Проверяем, что выбран договор, чтобы привязать к нему новую спецификацию
                                var row = ContractGrid.SelectedItem;
                                if (row != null)
                                {
                                    if (row.CheckGet("ID").ToInt() != 0)
                                    {
                                        result = true;
                                    }
                                }
                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "edit_specification",
                            Title = "Изменить",
                            Group = "specification",
                            MenuUse = true,
                            HotKey = "Return|DoubleCLick",
                            ButtonUse = true,
                            ButtonName = "EditSpecificationButton",
                            AccessLevel = Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var k = SpecificationGrid.GetPrimaryKey();
                                var id = SpecificationGrid.SelectedItem.CheckGet(k).ToInt();
                                if (id != 0)
                                {
                                    var specificationFrame = new MoldedContainerSpecification();
                                    specificationFrame.ReceiverName = ControlName;
                                    specificationFrame.Edit(id);
                                }
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = SpecificationGrid.GetPrimaryKey();
                                var row = SpecificationGrid.SelectedItem;
                                if (row != null)
                                {
                                    if (row.CheckGet("ID").ToInt() != 0)
                                    {
                                        result = true;
                                    }
                                }
                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "delete_specification",
                            Title = "Удалить",
                            Group = "specification",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "DeleteSpecificationButton",
                            AccessLevel = Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var k = SpecificationGrid.GetPrimaryKey();
                                var id = SpecificationGrid.SelectedItem.CheckGet(k).ToInt();
                                if (id != 0)
                                {
                                    DeleteSpecification(id);
                                }
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = SpecificationGrid.GetPrimaryKey();
                                var row = SpecificationGrid.SelectedItem;
                                if (row != null)
                                {
                                    if (row.CheckGet("ID").ToInt() != 0)
                                    {
                                        result = true;
                                    }
                                }
                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "print_specification",
                            Title = "Печать",
                            Group = "specification",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "PrintSpecificationButton",
                            AccessLevel = Role.AccessMode.ReadOnly,
                            Action = () =>
                            {
                                var k = SpecificationGrid.GetPrimaryKey();
                                var id = SpecificationGrid.SelectedItem.CheckGet(k).ToInt();
                                if (id != 0)
                                {
                                    PrintSpecification(id);
                                }
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = SpecificationGrid.GetPrimaryKey();
                                var row = SpecificationGrid.SelectedItem;
                                if (row != null)
                                {
                                    if (row.CheckGet("ID").ToInt() != 0)
                                    {
                                        result = true;
                                    }
                                }
                                return result;
                            },
                        });
                    }
                }
                {
                    Commander.SetCurrentGridName("PositionGrid");
                    //Commander.SetCurrentGroup("position");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "create_position",
                            Title = "Добавить",
                            Group = "position",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "CreatePositionButton",
                            AccessLevel = Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                int specificationId = SpecificationGrid.SelectedItem.CheckGet("ID").ToInt();

                                if (specificationId > 0)
                                {
                                    var positionFrame = new MoldedContainerPricePosition();
                                    positionFrame.ReceiverName = ControlName;
                                    positionFrame.SpecificationId = specificationId;
                                    positionFrame.Vat = SpecificationGrid.SelectedItem.CheckGet("VAT").ToDouble();
                                    positionFrame.Edit();
                                }
                                else
                                {
                                    var dw = new DialogWindow("Не выбрана спецификация", "Создать позицию спецификации");
                                    dw.ShowDialog();
                                }
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                // Проверяем, что выбрана спецификация, чтобы привязать к ней новую позицию
                                var row = SpecificationGrid.SelectedItem;
                                if (row != null)
                                {
                                    if (row.CheckGet("ID").ToInt() != 0)
                                    {
                                        result = true;
                                    }
                                }
                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "edit_position",
                            Title = "Изменить",
                            Group = "position",
                            MenuUse = true,
                            HotKey = "Return|DoubleCLick",
                            ButtonUse = true,
                            ButtonName = "EditPositionButton",
                            AccessLevel = Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                // По спецификации находим покупателя для изделий
                                int specificationId = SpecificationGrid.SelectedItem.CheckGet("ID").ToInt();
                                var k = PositionGrid.GetPrimaryKey();
                                var id = PositionGrid.SelectedItem.CheckGet(k).ToInt();
                                if (id != 0)
                                {
                                    var positionFrame = new MoldedContainerPricePosition();
                                    positionFrame.ReceiverName = ControlName;
                                    positionFrame.SpecificationId = specificationId;
                                    positionFrame.Vat = SpecificationGrid.SelectedItem.CheckGet("VAT").ToDouble();
                                    positionFrame.Edit(id);
                                }
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = PositionGrid.GetPrimaryKey();
                                var row = PositionGrid.SelectedItem;
                                if (row != null)
                                {
                                    if (row.CheckGet("ID").ToInt() != 0)
                                    {
                                        result = true;
                                    }
                                }
                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "delete_position",
                            Title = "Удалить",
                            Group = "position",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "DeletePositionButton",
                            AccessLevel = Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var k = PositionGrid.GetPrimaryKey();
                                var id = PositionGrid.SelectedItem.CheckGet(k).ToInt();
                                if (id != 0)
                                {
                                    DeletePosition(id);
                                }
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = PositionGrid.GetPrimaryKey();
                                var row = PositionGrid.SelectedItem;
                                if (row != null)
                                {
                                    if (row.CheckGet("ID").ToInt() != 0)
                                    {
                                        result = true;
                                    }
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
        /// Инициализация таблицы покупателей
        /// </summary>
        private void InitBuyerGrid()
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
                    Header="Покупатель",
                    Path="CUSTOMER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                },
            };
            BuyerGrid.SetColumns(columns);
            BuyerGrid.SetPrimaryKey("_ROWNUMBER");
            BuyerGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            BuyerGrid.SearchText = BuyerSearchText;
            BuyerGrid.Toolbar = BuyerToolbar;
            BuyerGrid.Commands = Commander;

            BuyerGrid.AutoUpdateInterval = 0;

            BuyerGrid.OnLoadItems = BuyerLoadItems;

            BuyerGrid.OnSelectItem = (selectItem) =>
            {
                UpdateBuyerAction(selectItem);
            };


            BuyerGrid.Init();
        }

        /// <summary>
        /// Инициализация таблицы договоров
        /// </summary>
        private void InitContractGrid()
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
                    Header="№ договора",
                    Path="CONTRACT_NUMBER",
                    ColumnType=ColumnTypeRef.String,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Дата договора",
                    Path="START_DATE",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=8,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="Продавец",
                    Path="SELLER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header="Валюта",
                    Path="CURRENCY_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="Условия оплаты",
                    Path="PAYMENT",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header="Файл",
                    Path="CONTRACT_FILE",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="Оригинал",
                    Path="CONTRACT_ORIGINAL",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="Копия",
                    Path="CONTRACT_FAX",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="Комментарий",
                    Path="COMMENTS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Должность руководителя",
                    Path="CHAIRMAN_POST",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="ФИО руководителя",
                    Path="CHAIRMAN_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                },
            };
            ContractGrid.SetColumns(columns);
            ContractGrid.SetPrimaryKey("ID");
            ContractGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            ContractGrid.AutoUpdateInterval = 0;
            ContractGrid.Toolbar = ContractToolbar;
            ContractGrid.Commands = Commander;

            ContractGrid.OnLoadItems = ContractLoadItems;
            ContractGrid.OnSelectItem = (selectItem) =>
            {
                UpdateContractAction(selectItem);
            };

            ContractGrid.Init();
        }

        /// <summary>
        /// Инициализация списка спецификаций
        /// </summary>
        private void InitSpecificationGrid()
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
                    Header="Номер",
                    Path="NUM",
                    ColumnType=ColumnTypeRef.String,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Дата начала",
                    Path="START_DATE",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=8,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="Дата окончания",
                    Path="END_DATE",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=8,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="Направление",
                    Path="CITY",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Оригинал получен",
                    Path="RETURNED_SIGNED_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Комментарий",
                    Path="COMMENTS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=3,
                },
            };
            SpecificationGrid.SetColumns(columns);
            SpecificationGrid.SetPrimaryKey("ID");
            SpecificationGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            SpecificationGrid.Toolbar = SpecificationToolbar;
            SpecificationGrid.AutoUpdateInterval = 0;
            SpecificationGrid.Commands = Commander;

            SpecificationGrid.OnLoadItems = SpecificationLoadItems;
            SpecificationGrid.OnFilterItems = SpecificationFilterItems;
            SpecificationGrid.OnSelectItem = (selectItem) =>
            {
                UpdateSpecificationAction(selectItem);
            };

            SpecificationGrid.Init();
        }

        /// <summary>
        /// Инициализация таблицы позиций спецификации
        /// </summary>
        private void InitPositionGrid()
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
                    Header="Артикул",
                    Path="SKU_CODE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="PRODUCT_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Цена без НДС",
                    Path="PRICE_VAT_EXCLUDED",
                    ColumnType=ColumnTypeRef.Double,
                    Width2=8,
                    Format="N2",
                },
                new DataGridHelperColumn
                {
                    Header="Цена с НДС",
                    Path="_PRICE",
                    ColumnType=ColumnTypeRef.Double,
                    Width2=8,
                    Format="N2",
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                },
            };
            PositionGrid.SetColumns(columns);
            PositionGrid.SetPrimaryKey("ID");
            PositionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            PositionGrid.Toolbar = PositionToolbar;
            PositionGrid.AutoUpdateInterval = 0;
            PositionGrid.Commands = Commander;

            PositionGrid.OnLoadItems = PositionLoadItems;
            PositionGrid.OnSelectItem = (selectItem) =>
            {

            };

            PositionGrid.Init();
        }

        /// <summary>
        /// Обработка общих сообщений
        /// </summary>
        /// <param name="action"></param>
        /// <param name="obj"></param>
        private void ProcessMessage(string action, ItemMessage obj=null)
        {
            switch (action)
            {
                case "RefreshContracts":
                    ContractGrid.LoadItems();
                    break;
                case "RefreshSpecifications":
                    SpecificationGrid.LoadItems();
                    break;
                case "RefreshPosition":
                    PositionGrid.LoadItems();
                    break;
            }
        }

        /// <summary>
        /// Загрузка данных в таблицу покупателей
        /// </summary>
        private async void BuyerLoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Economics");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "ListBuyer");

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                // Очищаем все зависимые таблицы
                ContractGrid.Items.Clear();
                ContractGrid.ClearItems();
                SpecificationGrid.Items.Clear();
                SpecificationGrid.ClearItems();
                PositionGrid.Items.Clear();
                PositionGrid.ClearItems();
                
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "BUYERS");
                    BuyerGrid.UpdateItems(ds);
                }
            }
        }

        /// <summary>
        /// Действия при выборе в таблице покупателей
        /// </summary>
        /// <param name="item"></param>
        private void UpdateBuyerAction(Dictionary<string, string> item)
        {
            ContractGrid.ClearItems();
            ContractGrid.Items.Clear();
            ContractGrid.SelectedItem.Clear();

            ContractGrid.LoadItems();
        }

        /// <summary>
        /// Загрузка данных в таблицу договоров
        /// </summary>
        private async void ContractLoadItems()
        {
            var contractorId = 0;

            if (BuyerGrid.SelectedItem != null)
            {
                contractorId = BuyerGrid.SelectedItem.CheckGet("CONTRACTOR_ID").ToInt();
            }

            if (contractorId > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Economics");
                q.Request.SetParam("Object", "MoldedContainer");
                q.Request.SetParam("Action", "ListContract");
                q.Request.SetParam("CONTRACTOR_ID", contractorId.ToString());

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    // Очищаем зависимые таблицы
                    SpecificationGrid.Items.Clear();
                    SpecificationGrid.ClearItems();
                    SpecificationGrid.SelectedItem.Clear();
                    PositionGrid.Items.Clear();
                    PositionGrid.ClearItems();
                    PositionGrid.SelectedItem.Clear();

                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "CONTRACTS");
                        ContractGrid.UpdateItems(ds);
                        if (ContractGrid.SelectedItem == null)
                        {
                            ContractGrid.SelectRowFirst();
                        }
                    }
                }

            }
            else
            {
                ContractGrid.ClearItems();
            }
        }

        /// <summary>
        /// Действия при выборе в таблице покупателей
        /// </summary>
        /// <param name="item"></param>
        private void UpdateContractAction(Dictionary<string, string> item)
        {
            SpecificationGrid.ClearItems();
            SpecificationGrid.Items.Clear();
            SpecificationGrid.SelectedItem.Clear();

            SpecificationGrid.LoadItems();
        }

        /// <summary>
        /// Удаление договора
        /// </summary>
        /// <param name="contractId"></param>
        private async void DeleteContract(int contractId)
        {
            // Подтверждение на удаление
            bool resume = false;

            var dw = new DialogWindow("Вы действительно хотите удвлить договор?", "Удаление договора", "", DialogWindowButtons.NoYes);
            if ((bool)dw.ShowDialog())
            {
                resume = dw.ResultButton == DialogResultButton.Yes;
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Economics");
                q.Request.SetParam("Object", "MoldedContainer");
                q.Request.SetParam("Action", "DeleteContract");
                q.Request.SetParam("CONTRACT_ID", contractId.ToString());

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
                            ContractGrid.LoadItems();
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
        /// Загрузка данных в таблицу спецификаций
        /// </summary>
        private async void SpecificationLoadItems()
        {
            var contractId = 0;

            if (BuyerGrid.SelectedItem != null)
            {
                contractId = ContractGrid.SelectedItem.CheckGet("ID").ToInt();
            }

            if (contractId > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Economics");
                q.Request.SetParam("Object", "MoldedContainer");
                q.Request.SetParam("Action", "ListSpecification");
                q.Request.SetParam("CONTRACT_ID", contractId.ToString());

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    // Очищаем зависимые таблицы
                    PositionGrid.Items.Clear();
                    PositionGrid.ClearItems();
                    PositionGrid.SelectedItem.Clear();

                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "SPECIFICATIONS");
                        SpecificationGrid.UpdateItems(ds);

                        if (SpecificationGrid.SelectedItem == null)
                        {
                            SpecificationGrid.SelectRowFirst();
                        }
                    }
                }
            }
            else
            {
                SpecificationGrid.ClearItems();
            }
        }

        /// <summary>
        /// Фильтрация записей таблицы спецификаций
        /// </summary>
        public void SpecificationFilterItems()
        {
            if (SpecificationGrid.Items != null)
            {
                if (SpecificationGrid.Items.Count > 0)
                {
                    bool showInvisible = (bool)ShowInvisible.IsChecked;

                    var list = new List<Dictionary<string, string>>();
                    foreach (var item in SpecificationGrid.Items)
                    {
                        bool includeByVisible = true;
                        if (!showInvisible)
                        {
                            if (item.CheckGet("INVISIBLE_FLAG").ToInt() == 1)
                            {
                                includeByVisible = false;
                            }
                        }

                        if (includeByVisible)
                        {
                            list.Add(item);
                        }
                    }
                    SpecificationGrid.Items = list;
                    SpecificationGrid.SelectRowFirst();

                }
            }
        }

        /// <summary>
        /// Действия при выборе в таблице покупателей
        /// </summary>
        /// <param name="item"></param>
        private void UpdateSpecificationAction(Dictionary<string, string> item)
        {
            PositionGrid.ClearItems();
            PositionGrid.Items.Clear();
            PositionGrid.SelectedItem.Clear();
            
            PositionGrid.LoadItems();
        }

        /// <summary>
        /// Удаление спецификации
        /// </summary>
        /// <param name="id"></param>
        private async void DeleteSpecification(int id)
        {
            bool resume = false;
            string warningMsg = "";
            
            // Если у спецификации есть позиции, просим пользователя подтвердить удаление
            if (PositionGrid.Items.Count > 0)
            {
                warningMsg = "Спецификация не пустая. Вы действительно хотите удалить спецификацию и ее содержимое?";
            }
            else
            {
                warningMsg = "Вы действительно хотите удалить спецификацию?";
            }

            var dw = new DialogWindow(warningMsg, "Удаление спецификации", "", DialogWindowButtons.NoYes);
            if ((bool)dw.ShowDialog())
            {
                resume = dw.ResultButton == DialogResultButton.Yes;
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Economics");
                q.Request.SetParam("Object", "MoldedContainer");
                q.Request.SetParam("Action", "DeleteSpecification");
                q.Request.SetParam("SPECIFICATION_ID", id.ToString());

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
                            SpecificationGrid.LoadItems();
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
        /// Обработка данных для таблицы позиций спецификации
        /// </summary>
        /// <param name="ds">Обработанный датасет</param>
        /// <returns></returns>
        private ListDataSet ProcessPositionItems(ListDataSet ds)
        {
            ListDataSet _ds = ds;
            double taxCoef = 1;
            if (SpecificationGrid.SelectedItem != null)
            {
                taxCoef = SpecificationGrid.SelectedItem.CheckGet("VAT").ToDouble();
            }

            if (ds.Items != null)
            {
                if (ds.Items.Count > 0)
                {
                    foreach (var item in _ds.Items)
                    {
                        var priceVatExcluded = item.CheckGet("PRICE_VAT_EXCLUDED").ToDouble();
                        item.CheckAdd("_PRICE", (priceVatExcluded * taxCoef).ToString());
                    }
                }
            }

            return _ds;
        }

        /// <summary>
        /// Загрузка данных позиций спецификации
        /// </summary>
        private async void PositionLoadItems()
        {
            int specificationId = 0;

            if (SpecificationGrid.SelectedItem != null)
            {
                specificationId = SpecificationGrid.SelectedItem.CheckGet("ID").ToInt();
            }

            if (specificationId > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Economics");
                q.Request.SetParam("Object", "MoldedContainer");
                q.Request.SetParam("Action", "ListSpecDetail");
                q.Request.SetParam("SPECIFICATION_ID", specificationId.ToString());

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "POSITIONS");
                        var processedDS = ProcessPositionItems(ds);
                        PositionGrid.UpdateItems(processedDS);

                        if (PositionGrid.SelectedItem == null)
                        {
                            PositionGrid.SelectRowFirst();
                        }
                    }
                }
            }
            else
            {
                PositionGrid.ClearItems();
            }
        }

        /// <summary>
        /// Удаление позиции спецификации
        /// </summary>
        /// <param name="id"></param>
        private async void DeletePosition(int id)
        {
            // Спрашиваем подтверждение на удаление
            var dw = new DialogWindow("Вы действительно хотите удалить позицию спецификации", "Удаление позиции", "", DialogWindowButtons.NoYes);
            if ((bool)dw.ShowDialog())
            {
                if (dw.ResultButton == DialogResultButton.Yes)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Economics");
                    q.Request.SetParam("Object", "MoldedContainer");
                    q.Request.SetParam("Action", "DeletePricePosition");
                    q.Request.SetParam("POSITION_ID", id.ToString());

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
                                PositionGrid.LoadItems();
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

        private async void PrintSpecification(int id)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Economics");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "GetSpecificationDocument");
            q.Request.SetParam("SPECIFICATION_ID", id.ToString());

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
            else if (q.Answer.Error.Code == 145)
            {
                q.ProcessError();
            }
        }

        private void ShowInvisible_Click(object sender, RoutedEventArgs e)
        {
            SpecificationGrid.UpdateItems();
        }
    }
}
