using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
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

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Окно выбора и утверждения даты производства для заявки на гофропроизводство
    /// </summary>
    /// <author>sviridov_ae</author>
    public partial class PreproductionConfirmOrder : ControlBase
    {
        /// <summary>
        /// Обязательные к заполнению переменные:
        /// OrderId;
        /// OrderType;
        /// OrderStatus;
        /// DefaultDate.
        /// </summary>
        public PreproductionConfirmOrder()
        {
            ControlTitle = "Утверждение даты для заявки";
            InitializeComponent();

            OnMessage = (ItemMessage message) => {
                DebugLog($"message=[{message.Message}]");
            };

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                FormInit();
                SetDefaults();
                GridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
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

        /// <summary>
        /// Ид площадки
        /// </summary>
        public int FactoryId { get; set; }

        /// <summary>
        /// Ид заявки на гофропроизводство.
        /// naklrashodz.nsthet
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Флаг того, что интерфейс полностью рабочий, то есть можно принимать заявку, а не просто смотреть загруженность
        /// </summary>
        public bool Editable { get; set; }

        /// <summary>
        /// Дата производства по умолчанию. Приходит из вне.
        /// </summary>
        public string DefaultDate { get; set; }

        /// <summary>
        /// Тип продукции в заявке 
        /// (1 - гофрапродукция, 3 - гофроизделия интернет магазина)
        /// </summary>
        public int OrderType { get; set; }

        /// <summary>
        /// Статус заявки
        /// (1 - веб зявки(в обработке), 3 - отредактирована клиентом(в обработке))
        /// </summary>
        public int OrderStatus { get; set; }

        /// <summary>
        /// Основной датасет с данными для грида
        /// </summary>
        public ListDataSet GridDataSet { get; set; }

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
                        Path = "CONFIRMED_DATE",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Format = "dd.MM.yyyy",
                        Control = DtFrom,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                };

                Form.SetFields(fields);
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            if (Editable)
            {
                ChangeDateCheckBox.IsChecked = false;
                DtFrom.IsEnabled = false;
                Form.SetValueByPath("CONFIRMED_DATE", DefaultDate);

                ChangeDateCheckBox.Visibility = Visibility.Visible;
                ChangeDateLabel.Visibility = Visibility.Visible;
                SaveButton.Visibility = Visibility.Visible;
            }
            else
            {
                ChangeDateCheckBox.IsChecked = true;
                DtFrom.IsEnabled = true;
                Form.SetValueByPath("CONFIRMED_DATE", DefaultDate);

                ChangeDateCheckBox.Visibility = Visibility.Collapsed;
                ChangeDateLabel.Visibility = Visibility.Collapsed;
                SaveButton.Visibility = Visibility.Collapsed;
            }
        }

        public void ProcessCommand(string command)
        {
            command=command.ClearCommand();
            if(!command.IsNullOrEmpty())
            {
                switch(command)
                {
                    case "help":
                    {
                        Central.ShowHelp("/doc/l-pack-erp/preproduction/preproduction_confirm_order/preproduction_confirm_order");
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Инициализация грида
        /// </summary>
        public void GridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Path="ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 4,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Рабочий центр",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 16,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"%{Environment.NewLine}(Принято + Простои + Заявка) / MAX",
                        Path="WORKLOAD_PERCENTAGE",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"КПД{Environment.NewLine}Для агрегированных записей среднее значение",
                        Path="EFFICIENCY",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="MAX",
                        Path="CAPACITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Простои",
                        Path="DOWNTIME",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Загрузка{Environment.NewLine}Загрузка в м2 для ГА / шт для переработки по принятым заявкам",
                        Path="BASE_QUANTITY",
                        ColumnType=ColumnTypeRef.Double,
                        Formatter = (string value) => { return value.ToDouble().ToString("#,###,###,###"); },
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Принято{Environment.NewLine}Загрузка в часах по принятым заявкам / КПД",
                        Path="WORKLOAD",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Не оплачено{Environment.NewLine}Загрузка в часах по принятым не оплаченным заявкам / КПД",
                        Path="UNPAID_WORKLOAD",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Заявка",
                        Path="ORDER_WORKLOAD",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2 = 8,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.FontWeight,
                                row =>
                                {
                                    var fontWeight= new FontWeight();
                                    fontWeight=FontWeights.Normal;

                                    if(row.CheckGet("ORDER_WORKLOAD").ToDouble() > 0)
                                    {
                                        fontWeight=FontWeights.Bold;
                                    }

                                    return fontWeight;
                                }
                            },
                        },
                    },

                    new DataGridHelperColumn
                    {
                        Header="Тип строки",
                        Path="ROW_TYPE",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 4,
                        Hidden=true,
                    },

                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=2,
                        MaxWidth=2000,
                    },
                };
                Grid.SetColumns(columns);
                Grid.OnLoadItems = GridLoadItems;
                Grid.PrimaryKey = "ROWNUMBER";
                Grid.SetSorting("ROWNUMBER", ListSortDirection.Ascending);
                Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

                //Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                Grid.ItemsAutoUpdate = true;

                // раскраска строк
                Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    {
                        StylerTypeRef.BackgroundColor,
                        (Dictionary<string, string> row) =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            // Серый -- Тип строки = 1 -- Это агрегированная строка с данными по группе станков
                            if (row.CheckGet("ROW_TYPE").ToInt() == 1)
                            {
                                color = HColor.Blue;
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };

                // контекстное меню
                Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "ShowMachineList",
                        new DataGridContextMenuItem()
                        {
                            Header="Показать список станков",
                            Action=()=>
                            {
                                ShowMachineList();
                            }
                        }
                    },
                    {
                        "ShowBaseWorkload",
                        new DataGridContextMenuItem()
                        {
                            Header="Загруженность в часах",
                            Action=()=>
                            {
                                ShowBaseWorkload();
                            }
                        }
                    },
                };

                Grid.Init();
                Grid.Focus();
                Grid.Run();
            }
        }

        public async void GridLoadItems()
        {
            DisableControls();
            SplashControl.Visible = true;

            var p = new Dictionary<string, string>();
            p.Add("ORDER_ID", OrderId.ToString());
            p.Add("DTTM", Form.GetValueByPath("CONFIRMED_DATE"));

            if (FactoryId > 0)
            {
                p.Add("FACTORY_ID", FactoryId.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "ConfirmOrder");
            q.Request.SetParam("Action", "GetMachineWorkload");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            GridDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    GridDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            else
            {
                q.ProcessError();
            }
            Grid.UpdateItems(GridDataSet);

            SplashControl.Visible = false;
            EnableControls();
        }

        /// <summary>
        /// Показать список станков для выбранной строки
        /// </summary>
        public void ShowMachineList()
        {
            string msg = $"Список станков строки {Grid.SelectedItem.CheckGet("NAME")}:{Environment.NewLine}{Grid.SelectedItem.CheckGet("MACHINE_LIST")}";
            var d = new DialogWindow($"{msg}", "Утверждение даты для заявки", "", DialogWindowButtons.OK);
            d.ShowDialog();
        }

        /// <summary>
        /// Показать загрузку станка в часах по принятым заявкам
        /// </summary>
        public void ShowBaseWorkload()
        {
            string msg = $"Рабочий центр: {Grid.SelectedItem.CheckGet("NAME")}{Environment.NewLine}" +
                $"Загруженность по принятым заявкам: {Grid.SelectedItem.CheckGet("BASE_WORKLOAD")}{Environment.NewLine}" +
                $"Загруженность по принятым не оплаченым заявкам: {Grid.SelectedItem.CheckGet("BASE_UNPAID_WORKLOAD")}";
            var d = new DialogWindow($"{msg}", "Утверждение даты для заявки", "", DialogWindowButtons.OK);
            d.ShowDialog();
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
            this.MinHeight = 680;
            this.MinWidth = 900;

            if (OrderId > 0)
            {
                var frameName = $"{ControlName}_{OrderId}";
                if (Editable)
                {
                    Central.WM.Show(frameName, $"Утверждение даты заявки {OrderId}", true, "main", this);
                }
                else
                {
                    Central.WM.Show(frameName, $"Загруженность станков заявки {OrderId}", true, "main", this);
                }
            }
            else
            {
                var frameName = $"{ControlName}";
                Central.WM.Show(frameName, $"Загруженность станков", true, "main", this);
            }
        }

        public void Save()
        {
            if (Form.Validate())
            {
                if (OrderId > 0 && Editable)
                {
                    DisableControls();

                    var p = new Dictionary<string, string>();
                    p.Add("ORDER_STATUS_ID", OrderStatus.ToString());
                    p.Add("ORDER_ID", OrderId.ToString());
                    p.Add("ORDER_TYPE", OrderType.ToString());
                    p.Add("CONFIRM_DTTM", Form.GetValueByPath("CONFIRMED_DATE"));
                    p.Add("NEW_DTTM_FLAG", ChangeDateCheckBox.IsChecked.ToInt().ToString());

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "ConfirmOrder");
                    q.Request.SetParam("Action", "Confirm");
                    q.Request.SetParams(p);
                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;
                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var dataSet = ListDataSet.Create(result, "ITEMS");
                            if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                            {
                                if (dataSet.Items.First().CheckGet("ORDER_ID").ToInt() > 0)
                                {
                                    // Отправляем сообщение табу список заявок на гофропроизводство для подтверждения о необходимости обновить грид
                                    {
                                        Central.Msg.SendMessage(new ItemMessage()
                                        {
                                            ReceiverGroup = "Preproduction",
                                            ReceiverName = "PreproductionConfirmOrderList",
                                            SenderName = "PreproductionConfirmOrder",
                                            //Message = dataSet.Items.First().CheckGet("ORDER_ID"),
                                            Action = "refresh",
                                        });
                                    }

                                    Close();
                                }
                            }
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }

                    EnableControls();
                }
                else
                {
                    Close();
                }
            }
        }

        public void ChangeDateCheckBoxClick()
        {
            DisableControls();

            if ((bool)ChangeDateCheckBox.IsChecked)
            {
                DtFrom.IsEnabled = true;
            }
            else
            {
                DtFrom.IsEnabled = false;
                Form.SetValueByPath("CONFIRMED_DATE", DefaultDate);
            }

            EnableControls();
        }

        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            Toolbar.IsEnabled = false;
        }

        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            Toolbar.IsEnabled = true;
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            if (OrderId > 0)
            {
                var frameName = $"{ControlName}_{OrderId}";
                Central.WM.Close(frameName);
            }
            else
            {
                var frameName = $"{ControlName}";
                Central.WM.Close(frameName);
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessCommand("help");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ChangeDateCheckBox_Click(object sender, RoutedEventArgs e)
        {
            ChangeDateCheckBoxClick();
        }

        private void ConfirmedDateTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Grid != null)
            {
                Grid.LoadItems();
            }
        }
    }
}
