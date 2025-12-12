using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperatorKsh
{
    /// <summary>
    /// перечень брака
    /// </summary>
    /// <author>volkov_as</author>
    public partial class Defects : ControlBase
    {
        public Defects()
        {
            InitializeComponent();

            FrameMode = 1;
            OnGetFrameTitle = () => "Причины брака";
            
            OnLoad = () =>
            {
                Messenger.Default.Register<ItemMessage>(this, ProcessMessages);
                DefectsGridInit();
            };

            OnUnload = () =>
            {
                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = "CorrugatorMachineOperator",
                    ReceiverName = "",
                    SenderName = "Defects",
                    Action = "Closed",
                });
                
                Messenger.Default.Unregister<ItemMessage>(this);
            };

            OnKeyPressed = args =>
            {
                switch (args.Key)
                {
                    case Key.Escape:
                        Close();
                        args.Handled = true;
                        break;
                }
            };
        }

        private Dictionary<string, string> SelectedDefectItem { get; set; }
        
        /// <summary>
        /// инициализация грида (причины простоев)
        /// </summary>
        private void DefectsGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                     new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=25,
                    },
                     new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="PRCI_ID",
                        Doc="ИД",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=60,
                    },
                     new DataGridHelperColumn
                    {
                        Header="ИД ПЗ",
                        Path="ID_PZ",
                        Doc="ИД ПЗ",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=70,
                    },
                     new DataGridHelperColumn
                    {
                        Header="Время",
                        Path="DTTM",
                        Doc="Время",
                        ColumnType=ColumnTypeRef.String,
                        Width=120,
                    },
                     new DataGridHelperColumn
                    {
                        Header="№ ПЗ",
                        Path="NUM",
                        Doc="№ ПЗ",
                        ColumnType=ColumnTypeRef.String,
                        Width=80,
                    },
                     new DataGridHelperColumn
                    {
                        Header="Длина",
                        Path="LENGTH",
                        Doc="Длина",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=50,
                    },
                     new DataGridHelperColumn
                    {
                        Header="Добавлено",
                        Path="QTY",
                        Doc="Добавлено",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=70,
                    },
                     new DataGridHelperColumn
                    {
                       Header = " ",
                       Path = "_",
                       ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                       MinWidth = 5,
                       MaxWidth = 2000,
                    },
                };
                DefectsGrid.SetColumns(columns);

                DefectsGrid.UseRowHeader = false;
                DefectsGrid.Init();
                
                DefectsGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {
                        SelectedDefectItem = selectedItem;
                    }
                };

                //данные грида
                DefectsGrid.OnLoadItems = DefectsGridLoadItems;

                DefectsGrid.OnDblClick = DefectEdit;

                DefectsGrid.Run();
            }
        }

        /// <summary>
        /// загрузка грида
        /// </summary>
        private async void DefectsGridLoadItems()
        {
            DefectsDisableControls();

            var ds = await ListDefects();
            DefectsGrid.UpdateItems(ds);

            DefectsEnableControls();
        }

        /// <summary>
        /// Список причин брака
        /// </summary>
        public static async Task<ListDataSet> ListDefects()
        {
            var ds = new ListDataSet();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Defect");
            q.Request.SetParam("Action", "ListUncommented");
            q.Request.SetParam("ID_ST", CorrugatorMachineOperator.SelectedMachineId.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            _ = await Task.Run(() =>
            {
                q.DoQuery();
                return q;
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    ds = ListDataSet.Create(result, "ITEMS");
                }
            }

            return ds;
        }

        private void DefectEdit(Dictionary<string, string> defect)
        {
            if (defect != null)
            {
                int id = defect.CheckGet("PRCI_ID").ToInt();
                string num = defect.CheckGet("NUM");

                var defectEditForm = new FormExtend()
                {
                    FrameName = "DefectEdit",
                    ID = "PRCI_ID",
                    Id = id,
                    Title = $"Брак по заданию {num}",

                    QueryGet = new FormExtend.RequestData()
                    {
                        Module = "Production",
                        Object = "Defect",
                        Action = "Get"
                    },

                    QuerySave = new FormExtend.RequestData()
                    {
                        Module = "Production",
                        Object = "Defect",
                        Action = "Save"
                    },

                    Fields = new List<FormHelperField>()
                    {
                        new FormHelperField()
                        {
                            Path="PCRR_ID",
                            FieldType=FormHelperField.FieldTypeRef.Integer,
                            Description = "Причина:",
                            ControlType = "SelectBox",
                            Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                                { FormHelperField.FieldFilterRef.Required, null },
                            },
                            Width = 400
                        },
                        new FormHelperField()
                        {
                            Path="COMMENTS",
                            FieldType=FormHelperField.FieldTypeRef.String,
                            Description = "Комментарии:",
                            ControlType="TextBox",
                            Width = 400,
                        },
                    }
                };

                defectEditForm["PCRR_ID"].OnAfterCreate += (control) =>
                {
                    if (control is SelectBox defectReason) defectReason.Autocomplete = true;

                    FormHelper.ComboBoxInitHelper(control as SelectBox, "Production", "Defect", "ListReason", "ID", "REASON", null, true);
                };

                defectEditForm.OnAfterSave += (id, result) =>
                {
                    DefectsGrid.LoadItems();
                };

                defectEditForm.Show();
            }
        }
        
        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("Production") > -1)
            {
                // обновление данных
                if (m.ReceiverName.IndexOf("Defects") > -1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            DefectsGrid.LoadItems();

                            var id = m.Message.ToInt();
                            DefectsGrid.SetSelectedItemId(id, "ID");

                            break;
                    }
                }
            }
        }
        
        /// <summary>
        /// редактирвоание записи
        /// </summary>
        /// <param name="id"></param>
        public void Edit()
        {
            Show();
        }
        
        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        private void DefectsDisableControls()
        {
            EditButton.IsEnabled = false;
            DefectsGrid.IsEnabled = false;
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        private void DefectsEnableControls()
        {
            EditButton.IsEnabled = DefectsGrid.Items?.Count > 0;
            DefectsGrid.IsEnabled = true;
        }

        private void CloseButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void EditButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DefectEdit(SelectedDefectItem);
        }
    }
}
