using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Interfaces.Main.DataGridHelperColumn;


namespace Client.Interfaces.Orders.MoldedContainer
{
    /// <summary>
    /// Фрейм выбора товара для позиции заявки литой тары
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class MoldedContainerProductSelect : ControlBase
    {
        public MoldedContainerProductSelect()
        {
            InitializeComponent();
            InitGrid();
            FormInit();

            OnUnload = () =>
            {
                Grid.Destruct();
            };

            Commander.SetCurrentGroup("main");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "save",
                    Group = "main_form",
                    Enabled = true,
                    Title = "Выбрать",
                    Description = "Выбор изделия для позиции спецификации",
                    ButtonUse = true,
                    ButtonName = "SaveButton",
                    MenuUse = false,
                    HotKey = "Return|DoubleCLick",
                    Action = () =>
                    {
                        Save();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "close",
                    Group = "main_form",
                    Enabled = true,
                    Title = "Отмена",
                    Description = "Закрыть форму без сохранения",
                    ButtonUse = true,
                    ButtonName = "CancelButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Close();
                    },
                });
            }
            Commander.Init(this);
        }
        /// <summary>
        /// Имя вкладки, окуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;
        /// <summary>
        /// ID заявки, для которой выбирается изделие в редактируемую позицию
        /// </summary>
        public int OrderId;
        
        public FormHelper Form { get; set; }

        private void FormInit()
        {
            Form = new FormHelper();
            var field = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path = "SHOW_ALL",
                    FieldType = FormHelperField.FieldTypeRef.Boolean,
                    Control = ShowAllRow,
                    ControlType = "CheckBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> { },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        Grid.UpdateItems();
                    },
                },
            };
            Form.SetFields(field);
            Form.SetDefaults();
        }

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="SKU_CODE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="PRODUCTS_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="PRODUCT_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("PRODUCT_ID");
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            Grid.SearchText = SearchText;
            Grid.Toolbar = Toolbar;
            Grid.Commands = Commander;

            Grid.OnLoadItems = LoadItems;
            Grid.AutoUpdateInterval = 0;
            Grid.OnSelectItem = (selectItem) =>
            {
                FormStatus.Text = "";
            };

            Grid.OnFilterItems = () =>
            {
                if (Grid.Items.Count > 0)
                {
                    {
                        var showAll = false;
                        var v = Form.GetValues();

                        if (v.CheckGet("SHOW_ALL").ToBool())
                        {
                            showAll = true;
                        }

                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in Grid.Items)
                        {
                            if (showAll)
                            {
                                items.Add(row);
                            }
                            else
                            {
                                if (row.CheckGet("CATEGORY_ID").ToInt() == 16)
                                {
                                    items.Add(row);
                                }
                            }
                        }
                        
                        Grid.Items = items;
                    }
                }
            };
            
            Grid.Init();
        }

        /// <summary>
        /// Загрузка содержимого таблицы
        /// </summary>
        private async void LoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Orders");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "ListProductSelect");
            q.Request.SetParam("ORDER_ID", OrderId.ToString());

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
                    var ds = ListDataSet.Create(result, "PRODUCTS");
                    Grid.UpdateItems(ds);
                }
            }

        }

        /// <summary>
        /// Сохранение выбора изделия
        /// </summary>
        public void Save()
        {
            if (Grid.Items != null)
            {
                if (Grid.SelectedItem != null)
                {
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverGroup = "Orders",
                        ReceiverName = ReceiverName,
                        SenderName = ControlName,
                        Action = "ProductSelect",
                        ContextObject = Grid.SelectedItem,
                    });
                    Close();
                }
                else
                {
                    FormStatus.Text = "Выберите изделие в таблице";
                }
            }
            else
            {
                FormStatus.Text = "Нет изделий для выбора";
            }
        }

        /// <summary>
        /// Отображение вкладки
        /// </summary>
        public void Show()
        {
            string title = "Выбор изделия";
            Central.WM.Show(ControlName, title, true, "add", this);
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        public void Close()
        {
            Central.WM.Close(ControlName);
            if (!ReceiverName.IsNullOrEmpty())
            {
                Central.WM.SetActive(ReceiverName);
                ReceiverName = "";
            }
        }
    }
}
