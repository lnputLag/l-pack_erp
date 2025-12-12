using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Отчёт по списаниям на гофроагрегате
    /// </summary>
    public partial class CorrugatorMachineReportWriteOff : ControlBase
    {
        public CorrugatorMachineReportWriteOff()
        {
            InitializeComponent();


            ControlTitle = "Простои";
            DocumentationUrl = "/doc/l-pack-erp/";
            RoleName = "[erp]machine_report";

            OnLoad = () =>
            {
                WriteOffGridInit();
            };

            OnUnload = () =>
            {
                Grid.Destruct();
            };

            OnFocusGot = () =>
            {
                Grid.ItemsAutoUpdate = true;
                Grid.Run();
            };

            OnFocusLost = () =>
            {
                Grid.ItemsAutoUpdate = false;
            };

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            FormInit();
            SetDefaults();



            {
                Commander.SetCurrentGridName("GridDefects");

                Commander.Add(new CommandItem()
                {
                    Name = "refresh",
                    Group = "grid_base",
                    Enabled = true,
                    Title = "Обновить",
                    Description = "Обновить данные",
                    ButtonUse = true,
                    //AccessLevel = Role.AccessMode.ReadOnly,
                    ButtonName = "ShowButton",
                    MenuUse = true,
                    Action = () =>
                    {
                        ShowButton.Style = (Style)ShowButton.TryFindResource("Button");
                        ShowButton_Click();
                    },
                });

                Commander.Add(new CommandItem()
                {
                    Name = "excel",
                    Group = "grid_base",
                    Enabled = true,
                    Title = "В Excel",
                    Description = "В Excel",
                    ButtonUse = true,
                    ButtonName = "ExcelButton",
                    MenuUse = true,
                    Action = () =>
                    {
                        Grid.ItemsExportExcel();

                    },
                });

                Commander.Init(this);
            }
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Выбранная запись в гриде списаний
        /// </summary>
        public Dictionary<string, string> WriteOffSelectedItem { get; set; }

        /// <summary>
        /// Инициализация грида
        /// </summary>
        public void WriteOffGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Номер ПЗ",
                    Path = "PRODUCTION_TASK_NUMBER",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 65,
                    MaxWidth = 75,
                },
                new DataGridHelperColumn
                {
                    Header = "ИД ПЗ",
                    Path = "PRODUCTION_TASK_ID",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 40,
                    MaxWidth = 55,
                },
                new DataGridHelperColumn
                {
                    Header = "ГА",
                    Path = "CUTOFF_ALLOCATION",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 65,
                    MaxWidth = 75,
                },
                new DataGridHelperColumn
                {
                    Header = "Дата начала ПЗ",
                    Path = "PRODUCTION_TASK_START_DTTM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format = "dd.MM.yyyy HH:mm:ss",
                    MinWidth = 70,
                    MaxWidth = 110,
                },
                new DataGridHelperColumn
                {
                    Header = "Дата окончания ПЗ",
                    Path = "PRODUCTION_TASK_END_DTTM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format = "dd.MM.yyyy HH:mm:ss",
                    MinWidth = 70,
                    MaxWidth = 110,
                },
                new DataGridHelperColumn
                {
                    Header = "По заданию, шт.",
                    Path = "INCOMING_QUANTITY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 30,
                    MaxWidth = 110,
                },
                new DataGridHelperColumn
                {
                    Header = "В приходе, шт.",
                    Path = "PRODUCTION_QUANTITY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 30,
                    MaxWidth = 100,
                },
                new DataGridHelperColumn
                {
                    Header = "Списано, шт.",
                    Path = "CONSUMPTION_QUANTITY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 30,
                    MaxWidth = 90,
                },
                new DataGridHelperColumn
                {
                    Header = "По заявке, шт.",
                    Path = "ORDER_QUANTITY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 30,
                    MaxWidth = 100,
                },
                new DataGridHelperColumn
                {
                    Header = "Дата последнего списания",
                    Path = "LAST_CONSUMPTION_DATETIME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format = "dd.MM.yyyy HH:mm:ss",
                    MinWidth = 70,
                    MaxWidth = 170,
                },
                new DataGridHelperColumn
                {
                    Header = "Брак, %.",
                    Path = "REJECT",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    MinWidth = 30,
                    MaxWidth = 70,
                    Format = "N2",
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    //перенесена на другой день
                                    if (row.CheckGet("REJECT").ToDouble() > 5)
                                    {
                                        color = HColor.Red;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                },
                new DataGridHelperColumn
                {
                    Header = "Продукция",
                    Path = "PRODUCT_NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 65,
                    MaxWidth = 320,
                },
                

                new DataGridHelperColumn
                {
                    Header = "",
                    Name="_",
                    Path = "_",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 5,
                    MaxWidth = 1200,
                },
            };


            Grid.SetColumns(columns);
            Grid.SetSorting("PRODUCTION_TASK_ID", ListSortDirection.Descending);
            Grid.SearchText = SearchText;
            Grid.Menu = new Dictionary<string, DataGridContextMenuItem>();
            Grid.OnLoadItems = WriteOffLoadItems;
            //Grid.OnDblClick = WriteOffEdit;
            Grid.OnSelectItem = item =>
            {
                WriteOffSelectedItem = item;
            };

            Grid.Commands = Commander;

            Grid.AutoUpdateInterval = 0;
            Grid.Init();
            Grid.Run();
            Grid.Focus();
        }

        /// <summary>
        /// Получение данных по списаниям для заполнения грида
        /// </summary>
        public async void WriteOffLoadItems()
        {
            DisableControls();

            var resume = true;

            var f = Form.GetValueByPath("FROM_DATE").ToDateTime();
            var t = Form.GetValueByPath("TO_DATE").ToDateTime();

            if (DateTime.Compare(f, t) > 0)
            {
                const string msg = "Дата начала должна быть меньше даты окончания.";
                var d = new DialogWindow($"{msg}", "Проверка данных");
                d.ShowDialog();
                resume = false;
            }

            if (resume)
            {
                var p = new Dictionary<string, string>
                {
                    ["ID_ST"] = Machines.SelectedItem.Key.ToInt().ToString(),
                    ["FROM_DATE"] = Form.GetValueByPath("FROM_DATE"),
                    ["TO_DATE"] = Form.GetValueByPath("TO_DATE"),
                };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Report");
                q.Request.SetParam("Action", "ListWriteOff");

                q.Request.SetParams(p);

                q.Request.Timeout = 30000;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() => {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");

                        if (ds != null)
                        {
                            if (ds.Items.Count > 0)
                            {
                                foreach (var item in ds.Items)
                                {
                                    int orderQuantity = item.CheckGet("ORDER_QUANTITY").ToInt();
                                    int consumptionQuantity = item.CheckGet("CONSUMPTION_QUANTITY").ToInt();

                                    if (orderQuantity > 0)
                                    {
                                        double onePercent = (double)orderQuantity / 100.0;

                                        double consumptionPercent = (double)consumptionQuantity / onePercent;

                                        item.CheckAdd("REJECT", consumptionPercent.ToString());
                                    }
                                }
                            }
                        }

                        Grid.UpdateItems(ds);
                    }
                }
            }

            EnableControls();
        }


        public void WriteOffEdit(Dictionary<string, string> writeOff)
        {
            if (writeOff != null)
            {
                int id = writeOff.CheckGet("IDIDLES").ToInt();

                var idleEditForm = new FormExtend()
                {
                    FrameName = "WriteOffEdit",
                    ID = "IDIDLES",
                    Id = id,
                    Title = $"Списание {id}",

                    QueryGet = new FormExtend.RequestData()
                    {
                        Module = "Production",
                        Object = "Report",
                        Action = "GetWriteOff"
                    },

                    QuerySave = new FormExtend.RequestData()
                    {
                        Module = "Production",
                        Object = "Report",
                        Action = "SaveWriteOff"
                    },

                    Fields = new List<FormHelperField>()
                    {
                        new FormHelperField()
                        {
                            Path="REASON_ID",
                            FieldType=FormHelperField.FieldTypeRef.Integer,
                            Description = "Причина:",
                            ControlType = "SelectBox",
                            Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                                { FormHelperField.FieldFilterRef.Required, null },
                            },
                            Width = 400,
                        },
                        new FormHelperField()
                        {
                            Path="ID_IDLE_DETAILS",
                            FieldType=FormHelperField.FieldTypeRef.Integer,
                            Description = "Тип:",
                            ControlType = "SelectBox",
                            Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                                { FormHelperField.FieldFilterRef.Required, null },
                            },
                            Width = 400,
                        },
                        new FormHelperField()
                        {
                            Path="REASON",
                            FieldType=FormHelperField.FieldTypeRef.String,
                            Description = "Описание:",
                            ControlType="TextBox",
                            Width = 400,
                        },
                    }
                };

                idleEditForm["REASON_ID"].OnAfterCreate += (control) =>
                {
                    var WriteOffReason = control as SelectBox;
                    WriteOffReason.Autocomplete = true;

                    FormHelper.ComboBoxInitHelper(control as SelectBox, "Production", "CorrugatorMachineOperator", "WriteOffReasonList", "ID", "NAME", null, true);
                };

                idleEditForm["ID_IDLE_DETAILS"].OnAfterCreate += (control) =>
                {
                    var WriteOffReason = control as SelectBox;
                    WriteOffReason.Autocomplete = true;

                    FormHelper.ComboBoxInitHelper(control as SelectBox, "Production", "CorrugatorMachineOperator", "WriteOffReasonDetailList", "ID_IDLE_DETAILS", "DESCRIPTION", null, true);
                };

                idleEditForm.OnAfterSave += (id, result) =>
                {
                    Grid.LoadItems();
                };

                idleEditForm.Show();
            }
        }

        /// <summary>
        /// Деактивация контролов
        /// </summary>
        public void DisableControls()
        {
            Grid.ShowSplash();
            GridToolbar.IsEnabled = false;
        }

        /// <summary>
        /// Активация контролов
        /// </summary>
        public void EnableControls()
        {
            Grid.HideSplash();
            GridToolbar.IsEnabled = true;
        }

        /// <summary>
        /// Управление доступом
        /// </summary>
        /// <param name="roleCode"></param>
        private void ProcessPermissions(string roleCode = "")
        {

        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetValueByPath("FROM_DATE", DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy"));
            Form.SetValueByPath("TO_DATE", DateTime.Now.ToString("dd.MM.yyyy"));

            var list = new Dictionary<string, string>();
            list.Add("0", "Все");
            list.Add("2", "ГА-1");
            list.Add("21", "ГА-2");
            list.Add("22", "ГА-3");
            list.Add("23", "ГА-1 КШ");
            Machines.Items = list;
            Machines.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");

            WriteOffSelectedItem = new Dictionary<string, string>();
            ShowButton.Style = (Style)ShowButton.TryFindResource("Button");
        }

        /// <summary>
        /// Инициализация формы
        /// </summary>
        public void FormInit()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path = "FROM_DATE",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = FromDate,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path = "TO_DATE",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = ToDate,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = GridToolbar;
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {

        }

        /// <summary>
        /// Обработчик выбора станка
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void Types_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
        }

        /// <summary>
        /// Документация
        /// </summary>
        private void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/production/machine_report/report_write_off");
        }

        private void ShowButton_Click()
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("Button");
            WriteOffLoadItems();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void DateTextChanged(object sender, RoutedEventArgs e)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
        }

        private void ButtonEdit_Click(object sender, RoutedEventArgs e)
        {
            WriteOffEdit(WriteOffSelectedItem);
        }
    }
}
