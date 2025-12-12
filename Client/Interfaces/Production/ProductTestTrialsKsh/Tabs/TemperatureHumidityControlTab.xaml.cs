using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Common;
using Client.Interfaces.Main;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.ProductTestTrialsKsh
{
    /// <summary>
    /// Логика взаимодействия для TemperatureHumidityControl.xaml
    /// </summary>
    public partial class TemperatureHumidityControlTab : ControlBase
    {
        public TemperatureHumidityControlTab()
        {
            InitializeComponent();

            ControlTitle = "Контроль температуры и влажности";

            RoleName = "[erp]prod_testing_trial_ksh";

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == "ControlGrid")
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };


            OnLoad = () =>
            {
                ControlGridInit();
            };

            OnUnload = () =>
            {
                ControlGrid.Destruct();
            };

            OnFocusGot = () =>
            {
                ControlGrid.ItemsAutoUpdate = true;
            };

            OnFocusLost = () =>
            {
                ControlGrid.ItemsAutoUpdate = false;
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
                        AccessLevel = Role.AccessMode.ReadOnly,
                        ButtonName = "HelpButton",
                        HotKey = "F1",
                        Action = () =>
                        {
                            Central.ShowHelp(DocumentationUrl);
                        },
                    });
                }
                Commander.SetCurrentGridName("ControlGrid");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "control_grid_update",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить таблицу",
                        ButtonUse = true,
                        ButtonName = "RefresheButton",
                        AccessLevel = Role.AccessMode.ReadOnly,
                        MenuUse = true,
                        Action = () =>
                        {
                            ControlGrid.LoadItems();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "control_grid_edit_note",
                        Group = "grid_base_note",
                        Enabled = true,
                        Title = "Изменить",
                        Description = "Изменить данные",
                        AccessLevel = Role.AccessMode.FullAccess,
                        ButtonUse = true,
                        ButtonName = "EditButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            var editNote = new ControlEditFrame();
                            editNote.Edit(ControlGrid.SelectedItem.CheckGet("TECO_ID").ToInt());
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "control_grid_add_note",
                        Group = "grid_base_note",
                        Enabled = true,
                        Title = "Добавить",
                        Description = "Добавить данные",
                        AccessLevel = Role.AccessMode.FullAccess,
                        ButtonUse = true,
                        ButtonName = "AddButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            var editNote = new ControlEditFrame();
                            editNote.Create();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "control_grid_delete_note",
                        Group = "grid_base_note",
                        Enabled = true,
                        Title = "Удалить",
                        Description = "Удалить запись",
                        AccessLevel = Role.AccessMode.FullAccess,
                        ButtonUse = true,
                        ButtonName = "DeleteButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            Delete();
                        },
                    });

                }
                Commander.Init(this);
            }
        }

        public void ControlGridInit()
        {
            var column = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Дата создания",
                    Path="CREATED_DTTM",
                    ColumnType = ColumnTypeRef.DateTime,
                    Format = "dd.MM.yyyy HH:mm:ss",
                    Width2 = 15,
                },
                new DataGridHelperColumn
                {
                    Header="Ид контроля",
                    Path="TECO_ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 11,
                },
                new DataGridHelperColumn
                {
                    Header="Влажность, %",
                    Path="HUMIDITY",
                    ColumnType = ColumnTypeRef.Double,
                    Width2 = 11,
                },
                new DataGridHelperColumn
                {
                    Header="Температура, C",
                    Path="TEMPERATURE",
                    ColumnType = ColumnTypeRef.Double,
                    Width2 = 12,
                },
                new DataGridHelperColumn
                {
                    Header="ФИО исполнителя",
                    Path="EMPLOYEE",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 15,
                },
                
            };
            ControlGrid.SetColumns(column);

            ControlGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ControlGrid.SearchText = SearchText;
            ControlGrid.SetPrimaryKey("TECO_ID");
            ControlGrid.Toolbar = ControlGridToolbar;
            ControlGrid.QueryLoadItems = new RequestData()
            {
                Module = "ProductTestTrialKsh",
                Object = "Control",
                Action = "List",
                AnswerSectionKey = "CONTROL_LIST"
            };

            ControlGrid.OnDblClick = (Dictionary<string, string> selectedItem) =>
            {
                var editNote = new ControlEditFrame();
                editNote.Edit(selectedItem.CheckGet("TECO_ID").ToInt());
            };

            ControlGrid.Commands = Commander;

            ControlGrid.Init();


        }

        /// <summary>
        /// Функция для удаления замера
        /// </summary>
        private void Delete()
        {
            var id = ControlGrid.SelectedItem.CheckGet("TECO_ID").ToInt();
            var dialog = new DialogWindow($"Вы уверены что хотите удалить замер - {id}?", "Удаление", "", DialogWindowButtons.YesNo);
            dialog.ShowDialog();

            if (dialog.DialogResult != true)
            {
                return;
            }

            DeleteControl(id.ToString());
        }


        /// <summary>
        /// Запрос выполняющий при удаления замера Delete()
        /// </summary>
        /// <param name="tecoId">ИД замера</param>
        private async void DeleteControl(string tecoId)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductTestTrialKsh");
            q.Request.SetParam("Object", "Control");
            q.Request.SetParam("Action", "RecordDelete");
            q.Request.SetParam("TECO_ID", tecoId);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                ControlGrid.LoadItems();
            }
        }


    }
}
