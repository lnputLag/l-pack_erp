using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Интерфес для работы с приходом во вкладке "Приход оснастки" нижний грид.
    /// </summary>
    /// <author>volkov_as</author>
    public partial class IncomePositionFrame : ControlBase
    {
        public IncomePositionFrame()
        {
            PackingListPositionId = null;

            InitializeComponent();

            RoleName = "[erp]rig_movement";

            OnLoad = () =>
            {
                FormInit();
                InitGrid();
            };

            OnUnload = () =>
            {
                MainGridBox.Destruct();
            };

            OnFocusGot = () =>
            {
                MainGridBox.ItemsAutoUpdate = true;
            };

            OnFocusLost = () =>
            {
                MainGridBox.ItemsAutoUpdate = false;
            };
            
            FrameMode = 0;
            OnGetFrameTitle = () =>
            {
                var result = "";
                var id = PackingListPositionId.ToInt();

                if (id == 0)
                {
                    result = "Добавление позиции в приход";
                }
                else
                {
                    result = $"Изменение позиции накладной - {PackingListPositionId}";
                }

                return result;
            };

            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "save",
                        Enabled = true,
                        Title = "Сохранить",
                        ButtonUse = true,
                        Description = "Добавить позицию и закрыть окно",
                        ButtonName = "SaveButton",
                        AccessLevel = Common.Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            ChangeSave(0);
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "add",
                        Enabled = true,
                        Title = "Добавить",
                        ButtonUse = true,
                        Description = "Добавить позицию",
                        ButtonName = "AddButton",
                        AccessLevel = Common.Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            ChangeSave(1);
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "cancel",
                        Enabled = true,
                        Title = "Закрыть",
                        Description = "Закрыть окно без сохранения",
                        ButtonUse = true,
                        ButtonName = "CancelButton",
                        Action = CloseWindow
                    });
                }

                Commander.SetCurrentGridName("MainGridBox");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "main_grid_update_items",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "UpdateMainGrid",
                        MenuUse = true,
                        Action = () =>
                        {
                            MainGridBox.LoadItems();
                        },
                    });
                }

                Commander.Init(this);
            }
        }

        private string PackingListPositionId { get; set; }
        private string PackingListId { get; set; }
        private string SupplierId { get; set; }
        private string NoteId { get; set; }
        private string IdRequestionShtanzform { get; set; }
        private string RiotId { get; set; }
        private string ProductId { get; set; }
        private string ProductCategoryId { get; set; }
        public ListDataSet DataList { get; set; }
        public ListDataSet RecordList { get; set; }
        
        private string NamePosition { get; set; }

        private FormHelper Form { get; set; }

        private void FormInit()
        {
            Form = new FormHelper();
            var fileds = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="CENAPOKR",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control= PriceBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ },
                },
            };

            Form.SetFields(fileds);
        }

        private void InitGrid()
        {
            var column = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Наименование",
                    Path = "NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 45
                },
                new DataGridHelperColumn
                {
                    Header = "Ед.изм.",
                    Path = "NAME_IZM",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 8
                },
                new DataGridHelperColumn
                {
                    Header = "Дт получения",
                    Path = "RECEIPT_DTTM",
                    ColumnType = ColumnTypeRef.DateTime,
                    Width2 = 12,
                    Format = "dd.MM HH:mm"
                },
                new DataGridHelperColumn
                {
                    Header = "Статус",
                    Path = "STATUS_NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 15
                },
                new DataGridHelperColumn
                {
                    Header = "Статус",
                    Path = "STATUS",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 80,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "ИД",
                    Path = "ID2",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 15,
                    Visible = true
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
            MainGridBox.SetColumns(column);
            MainGridBox.OnLoadItems = MainGridLoadItems;
            MainGridBox.SearchText = FilterNameBox;
            MainGridBox.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            MainGridBox.SetPrimaryKey("ID2");
            
            MainGridBox.OnSelectItem = (Dictionary<string, string> selectedItem) =>
            {
                IdRequestionShtanzform = selectedItem.CheckGet("SHOR_ID");
                NoteId = selectedItem.CheckGet("CLOT_ID");
                ProductId = selectedItem.CheckGet("ID2");
                ProductCategoryId = selectedItem.CheckGet("IDK1");
                NamePosition = selectedItem.CheckGet("NAME");
                RiotId = selectedItem.CheckGet("RIOR_ID");
            };

            MainGridBox.Commands = Commander;

            MainGridBox.Init();
        }
        
        /// <summary>
        /// Загрузка выбранного прихода если есть Idp либо загрузка списка ПФ
        /// </summary>
        private async void MainGridLoadItems()
        {
            var q = new LPackClientQuery();

            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "IncomePosition");
            q.Request.SetParam("Action", "Create");
            q.Request.SetParam("NNAKL", PackingListId);
            q.Request.SetParam("ID_POST", SupplierId);
            q.Request.SetParam("IDP", PackingListPositionId);

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
                    DataList = ListDataSet.Create(result, "ITEMS");
                    MainGridBox.UpdateItems(DataList);
                }
            }

            if (PackingListPositionId != null)
            {
                IncomePositionPriceLoad();
            }
        }


        /// <summary>
        /// Загрузка цены позиции прихода
        /// </summary>
        private async void IncomePositionPriceLoad()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "IncomePosition");
            q.Request.SetParam("Action", "GetByPrice");
            q.Request.SetParam("IDP", PackingListPositionId);

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
                    RecordList = ListDataSet.Create(result, "ITEM");
                    
                    Form.SetValues(RecordList);
                }
            }
        }

        /// <summary>
        /// Функция вызывается и при добавлении и при редактировании позиции
        /// </summary>
        private async void ChangeSave(int flag)
        {
            var f = Form.GetValues();

            FormStatus.Text = "Сохранение";

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "IncomePosition");
            q.Request.SetParam("Action", "Save");
            q.Request.SetParam("IDP", PackingListPositionId);
            q.Request.SetParam("NNAKL", PackingListId);
            q.Request.SetParam("ID_POST", SupplierId);
            q.Request.SetParam("ID2", ProductId.ToInt().ToString());
            q.Request.SetParam("IDK1", ProductCategoryId);
            q.Request.SetParam("CENAPOKR", f.CheckGet("CENAPOKR"));
            q.Request.SetParam("CLOT_ID", NoteId.ToInt().ToString());
            q.Request.SetParam("SHOR_ID", IdRequestionShtanzform.ToInt().ToString());
            q.Request.SetParam("RIOR_ID", RiotId);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                FormStatus.Text = "Сохранено";

                if (flag == 1)
                {
                    var d = new DialogWindow($"Позиция - {NamePosition} с ценой {PriceBox.Text} руб. добавлена", "Добавление позиции");
                    d.ShowDialog();
                    
                    MainGridBox.LoadItems();
                    NamePosition = "";
                    PriceBox.Text = "";
                }
                else
                {
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverName = "IncomePositionGrid",
                        Action = "incoming_refresh",
                        Message = $"{PackingListPositionId}",
                    });
                    
                    Close();
                }
            }
        }

        private void CloseWindow()
        {
            Central.Msg.SendMessage(new ItemMessage()
            {
                ReceiverName = "IncomePositionGrid",
                Action = "incoming_refresh",
                Message = $"{PackingListPositionId}",
            });

            Close();
        }


        /// <summary>
        /// Открытие фрейма в режиме редактирование
        /// </summary>
        /// <param name="packingListId">ИД накладной</param>
        /// <param name="supplierId">ИД поставщика</param>
        /// <param name="packingListPositionId">Номер позиции в приходе</param>
        public void Edit(string packingListId, string supplierId, string packingListPositionId)
        {
            PackingListPositionId = packingListPositionId;
            PackingListId = packingListId;
            SupplierId = supplierId;
            Show();
        }


        /// <summary>
        /// Открытие фрейма в режиме создания
        /// </summary>
        /// <param name="packingListId">ИД накладной</param>
        /// <param name="supplierId">ИД поставщика</param>
        public void Create(string packingListId, string supplierId)
        {
            SupplierId = supplierId;
            PackingListId = packingListId;
            Show();
        }
    }
}