using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Интерфейс изменение времени отгрузки
    /// </summary>
    public partial class ShipmentDateChange : ControlBase
    {
        public ShipmentDateChange()
        {
            ControlTitle = "Перенос времени отгрузки";
            DocumentationUrl = "/doc/l-pack-erp/shipments/control/listing/reason_of_date_change";
            RoleName = "[erp]shipment_control";
            TimeGridInitialized = false;
            InitializeComponent();

            OnMessage = (ItemMessage message) => {
                DebugLog($"message=[{message.Message}]");
            };

            Messenger.Default.Register<ItemMessage>(this, _ProcessMessages);

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                FormInit();
                SetDefaults();
                ReasonLoadItems();
                TimeGridInit();
                ProcessPermissions();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                TimeGrid.Destruct();

                Messenger.Default.Unregister<ItemMessage>(this);
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
            };
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        private ListDataSet TimeGridDataSet { get; set; }

        private bool TimeGridInitialized { get; set; }

        public int ShipmentId { get; set; }

        public int ShipmentType { get; set; }

        public int FactoryId = 0;

        public void FormInit()
        {
            //инициализация формы
            {
                Form = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="SHIPMENT_DATE",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=ShipmentDate,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "REASON_ID",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = ReasonId,
                        ControlType = "SelectBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "NOTE",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = Note,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {

                        },
                    },
                };

                Form.SetFields(fields);
            }
        }

        public void TimeGridInit()
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
                        Width2=4,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата",
                        Path="DT",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy",
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Время",
                        Path="TIME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество отгрузок",
                        Path="COUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Максимальное количество отгрузок",
                        Path="LIMIT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                };
                TimeGrid.SetColumns(columns);
                TimeGrid.OnLoadItems = TimeGridLoadItems;
                TimeGrid.SetPrimaryKey("_ROWNUMBER");
                TimeGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                TimeGrid.AutoUpdateInterval = 0;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                TimeGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.CheckGet("COUNT").ToInt() == selectedItem.CheckGet("LIMIT").ToInt())
                    {
                        SaveButton.IsEnabled = false;
                    }
                    else
                    {
                        SaveButton.IsEnabled = true;
                    }

                    ProcessPermissions();
                };

                TimeGrid.UseProgressSplashAuto = false;
                TimeGrid.Init();
                TimeGrid.Run();

                TimeGridInitialized = true;
            }
        }

        public async void TimeGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            p.Add("SHIPMENTID", ShipmentId.ToString());
            p.Add("SHIPMENTTYPE", ShipmentType.ToString());
            p.Add("SHIPMENTDATE", Form.GetValueByPath("SHIPMENT_DATE"));
            p.Add("FACTORY_ID", $"{FactoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Shipment");
            q.Request.SetParam("Action", "ListTimes");

            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            TimeGridDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    TimeGridDataSet = ListDataSet.Create(result, "Items");
                }
            }
            TimeGrid.UpdateItems(TimeGridDataSet);
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            TimeGridDataSet = new ListDataSet();
            Form.SetValueByPath("SHIPMENT_DATE", DateTime.Now.ToString("dd.MM.yyyy"));
        }

        public async void ReasonLoadItems()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Shipment");
            q.Request.SetParam("Action", "ListReasonOfDateChange");
            q.Request.SetParams(p);
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
                    var dataSet = ListDataSet.Create(result, "ITEMS");
                    ReasonId.SetItems(dataSet, "ID", "NAME");
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel(this.RoleName);
            var userAccessMode = mode;
            switch (mode)
            {
                case Role.AccessMode.Special:
                    break;

                case Role.AccessMode.FullAccess:
                    break;

                case Role.AccessMode.ReadOnly:
                default:
                    break;
            }

            List<Button> buttons = UIUtil.GetVisualChilds<Button>(this.Content as DependencyObject);
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    var buttonTagList = UIUtil.GetTagList(button);
                    var accessMode = Acl.FindTagAccessMode(buttonTagList);
                    if (accessMode > userAccessMode)
                    {
                        button.IsEnabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 2;
            var frameName = $"{ControlName}_{ShipmentId}";
            this.MinHeight = 675;
            this.MinWidth = 575;
            Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
            windowParametrs.Add("no_resize", "1");
            windowParametrs.Add("center_screen", "1");
            Central.WM.Show(frameName, $"Перенос времени отгрузки № {ShipmentId}", true, "main", this, "top", windowParametrs);
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            var frameName = $"{ControlName}_{ShipmentId}";
            Central.WM.Close(frameName);
        }

        public void SetFormFilters()
        {
            // Если выбрали причину -- Иное, то требуем обязательно заполнить комментарий
            if (Form.GetValueByPath("REASON_ID").ToInt() == 3)
            {
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "NOTE"), FormHelperField.FieldFilterRef.Required);
            }
            else
            {
                Form.RemoveFilter("NOTE", FormHelperField.FieldFilterRef.Required);
            }
        }

        public void ShowHelp()
        {
            Central.ShowHelp(DocumentationUrl);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void _ProcessMessages(ItemMessage m)
        {

        }

        public void Save()
        {
            if (Form.Validate())
            {
                if (TimeGrid != null && TimeGrid.SelectedItem != null && TimeGrid.SelectedItem.Count > 0)
                {
                    if (TimeGrid.SelectedItem.CheckGet("COUNT").ToInt() == TimeGrid.SelectedItem.CheckGet("LIMIT").ToInt())
                    {
                        var d = new DialogWindow($"Достигнуто максимальное количество отгрузок на выбранное время", "Перенос отгрузки", "", DialogWindowButtons.OK);
                        d.ShowDialog();

                        return;
                    }

                    if (string.IsNullOrEmpty(TimeGrid.SelectedItem.CheckGet("DT")) 
                        || string.IsNullOrEmpty(TimeGrid.SelectedItem.CheckGet("TIME")))
                    {
                        var d = new DialogWindow($"Не указана дата или время отгрузки", "Перенос отгрузки", "", DialogWindowButtons.OK);
                        d.ShowDialog();

                        return;
                    }

                    {
                        var p = new Dictionary<string, string>();
                        p.Add("SHIPMENT_ID", ShipmentId.ToString());
                        p.Add("REASON_ID", Form.GetValueByPath("REASON_ID"));
                        p.Add("NOTE", Form.GetValueByPath("NOTE"));
                        p.Add("SHIPMENT_DATE", TimeGrid.SelectedItem.CheckGet("DT"));
                        p.Add("SHIPMENT_TIME", TimeGrid.SelectedItem.CheckGet("TIME"));

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Shipments");
                        q.Request.SetParam("Object", "Shipment");
                        q.Request.SetParam("Action", "EditDate");
                        q.Request.SetParams(p);

                        q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                        q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                        q.DoQuery();

                        if (q.Answer.Status == 0)
                        {
                            bool succesfullFlag = false;

                            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                            if (result != null)
                            {
                                var dataSet = ListDataSet.Create(result, "ITEMS");
                                if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                                {
                                    if (dataSet.Items.First().CheckGet("SHIPMENT_ID").ToInt() > 0)
                                    {
                                        succesfullFlag = true;
                                    }
                                }

                            }

                            if (succesfullFlag)
                            {
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "ShipmentControl",
                                    SenderName = "ShipmentDateChange",
                                    ReceiverName = "ShipmentsList",
                                    Action = "Refresh",
                                    Message = "",
                                });

                                Close();
                            }
                            else
                            {
                                var d = new DialogWindow($"При изменении даты отгрузки произошла ошибка. Пожалуйста, сообщите о проблеме.", "Перенос отгрузки", "", DialogWindowButtons.OK);
                                d.ShowDialog();
                            }
                        }
                        else
                        {
                            q.ProcessError();
                        }
                    }
                }
                else
                {
                    var d = new DialogWindow($"Не выбрано новое время отгрузки", "Перенос отгрузки", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                var d = new DialogWindow($"Не все поля заполнены верно", "Перенос отгрузки", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }
        private void ReasonId_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SetFormFilters();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void ShipmentDate_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TimeGrid != null && TimeGridInitialized)
            {
                TimeGridLoadItems();
            }
        }
    }
}
