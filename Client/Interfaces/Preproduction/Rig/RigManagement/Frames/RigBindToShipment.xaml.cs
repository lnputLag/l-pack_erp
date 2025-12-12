using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using DevExpress.XtraReports.Native;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Вкладка привязки оснастки к отгрузке
    /// </summary>
    public partial class RigBindToShipment : ControlBase
    {
        public RigBindToShipment()
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
        public FormHelper Form { get; set; }
        /// <summary>
        /// ИД заказчика/клиента
        /// </summary>
        public int CustomerId;
        /// <summary>
        /// Список ID образцов в виде строки
        /// </summary>
        public string IdList;
        /// <summary>
        /// Объект, который привязывается к оснастке
        /// </summary>
        public string ObjectName;
        /// <summary>
        /// Идентификатор производственной площадки: 1 - Липецк, 2 - Кашира
        /// </summary>
        public int FactoryId;
        /// <summary>
        /// Тип заявки на отгрузку: 1 - клиенту, 2 - на другую площадку
        /// </summary>
        public int TypeOrder;

        /// <summary>
        /// Инициализация формы
        /// </summary>
        private void FormInit()
        {
            Form = new FormHelper();
            var field = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path = "SHOW_ALL",
                    FieldType = FormHelperField.FieldTypeRef.Boolean,
                    Control = AllShipments,
                    ControlType = "CheckBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> { },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        Grid.LoadItems();
                    },
                },
            };
            Form.SetFields(field);

            FactoryId = 1;
            Form.SetDefaults();
        }

        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>()
            {
                new DataGridHelperColumn()
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    Doc="Номер по порядку в списке",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Дата отгрузки",
                    Path="SHIPMENT_DATE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Покупатель",
                    Path="CUSTOMER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=50,
                },
                new DataGridHelperColumn
                {
                    Header="Водитель",
                    Path="DRIVER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=50,
                },
                new DataGridHelperColumn
                {
                    Header="Номер заявки",
                    Path="NUMBER_ORDER",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="ИД отгрузки",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=20,
                },
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("ID");
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            Grid.AutoUpdateInterval = 0;
            Grid.SearchText = SearchText;
            Grid.Toolbar = MainToolbar;
            Grid.Commands = Commander;

            Grid.OnLoadItems = LoadItems;
            Grid.OnDblClick = selectedItem =>
            {
                Save();
            };
            Grid.Init();
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        private async void LoadItems()
        {
            // Если отмечен чекбокс Все отгрузки, то вместо ИД покупателя отправляем 0
            string customerId = (bool)AllShipments.IsChecked ? "0" : CustomerId.ToString();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "RigTransfer");
            q.Request.SetParam("Action", "ListAvaliableShipment");
            q.Request.SetParam("CUSTOMER_ID", customerId);
            q.Request.SetParam("FACTORY_ID", FactoryId.ToString());
            q.Request.SetParam("TYPE_ORDER", TypeOrder.ToString());

            q.Request.Timeout = Central.Parameters.RequestTimeoutMax;
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
                    var ds = ListDataSet.Create(result, "SHIPMENTS");
                    Grid.UpdateItems(ds);
                }
            }
        }

        /// <summary>
        /// Вызов метода привязки отгрузки к образцам
        /// </summary>
        /// <param name="list">список Id образцов в виде строки с разделителем запятая</param>
        public void Bind(string list)
        {
            if (!string.IsNullOrEmpty(list))
            {
                IdList = list;
                if (CustomerId == 0)
                {
                    AllShipments.IsChecked = true;
                    AllShipments.IsEnabled = false;
                }
                //Grid.LoadItems();
                Show();
            }
        }

        /// <summary>
        /// Отображение вкладки
        /// </summary>
        public void Show()
        {
            string title = $"Отгрузки для привязки";
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
                Central.WM.SetActive(ReceiverName, true);
                ReceiverName = "";
            }
        }


        public async void Save()
        {
            if (Grid.SelectedItem != null)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Rig");
                q.Request.SetParam("Object", ObjectName);
                q.Request.SetParam("Action", "BindShipment");
                q.Request.SetParam("SHIPMENT_ID", Grid.SelectedItem.CheckGet("ID"));
                q.Request.SetParam("ID_LIST", IdList);

                q.Request.Timeout = Central.Parameters.RequestTimeoutMax;
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
                        if(result.ContainsKey("ITEM"))
                        {
                            //отправляем сообщение о закрытии окна
                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverGroup = "Preproduction/Rig",
                                ReceiverName = ReceiverName,
                                SenderName = ControlName,
                                Action = "Refresh",
                            });
                            Close();
                        }
                    }
                }
                else if (q.Answer.Error.Code == 145)
                {
                    FormStatus.Text = q.Answer.Error.Message;
                }

            }
        }

    }
}

