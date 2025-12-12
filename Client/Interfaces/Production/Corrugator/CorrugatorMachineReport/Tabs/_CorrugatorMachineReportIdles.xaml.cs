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
    /// Отчёт по простоям на гофроагрегате
    /// </summary>
    /// <author>vlasov_ea</author>   
    public partial class CorrugatorMachineReportIdles : ControlBase
    {
        public CorrugatorMachineReportIdles()
        {
            InitializeComponent();

            ControlTitle = "Простои";
            DocumentationUrl = "/doc/l-pack-erp/";
            RoleName = "[erp]machine_report";

            OnLoad = () =>
            {
                IdleGridInit();
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
                    Name = "edit",
                    Group = "grid_base",
                    Enabled = true,
                    Title = "Изменить",
                    Description = "Изменить",
                    ButtonUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    ButtonName = "EditButton",
                    MenuUse = true,
                    Action = () =>
                    {
                        IdleEdit(IdleSelectedItem);
                    },
                });

                Commander.Add(new CommandItem()
                {
                    Name = "delete",
                    Group = "grid_base",
                    Enabled = true,
                    Title = "Удалить",
                    Description = "Удалить",
                    ButtonUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    ButtonName = "DeleteButton",
                    MenuUse = true,
                    Action = () =>
                    {
                        IdleDelete(IdleSelectedItem);

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
                        ExportToExcel();

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
        public Dictionary<string, string> IdleSelectedItem { get; set; }

        /// <summary>
        /// Инициализация грида
        /// </summary>
        public void IdleGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width=40,
                },
                new DataGridHelperColumn
                {
                    Header="Станок",
                    Path="MACHINE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width=40,
                },
                new DataGridHelperColumn
                {
                    Header = "Начало",
                    Path = "FROMDT",
                    ColumnType = ColumnTypeRef.String,
                    Width = 130,
                },
                new DataGridHelperColumn
                {
                    Header = "Время простоя",
                    Path = "DT",
                    ColumnType = ColumnTypeRef.String,
                    Width = 65,
                },
                new DataGridHelperColumn
                {
                    Header = "Смена",
                    Path = "WOTE_ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width = 65,
                },
                new DataGridHelperColumn
                {
                    Header = "Причина",
                    Path = "NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width = 110,
                },
                new DataGridHelperColumn
                {
                    Header = "Тип",
                    Path = "DESCRIPTION",
                    ColumnType = ColumnTypeRef.String,
                    Width = 400,
                },
                new DataGridHelperColumn
                {
                    Header = "Описание",
                    Path = "REASON",
                    ColumnType = ColumnTypeRef.String,
                    Width = 600,
                    MaxWidth = 1000,
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
            Grid.SearchText = SearchText;
            Grid.OnLoadItems = IdleGridLoadItems;

            Grid.OnFilterItems = () =>
             {
                 if (Grid.Items != null)
                 {
                     if (Grid.Items.Count > 0)
                     {
                         //FIXME: нужно заюзать интерфейс FormHelper
                         if (Reason.SelectedItem.Key != null)
                         {
                             var IdleId = Reason.SelectedItem.Key.ToInt();
                             var items = new List<Dictionary<string, string>>();

                             if (IdleId == 0)
                             {
                                 items = Grid.Items;
                             }
                             else
                             {
                                 items.AddRange(Grid.Items.Where(row => row.CheckGet("IDREASON").ToInt() == IdleId));
                             }

                             Grid.Items = items;
                         }
                     }
                 }
             };

            Grid.OnDblClick = IdleEdit;
            Grid.OnSelectItem = item =>
            {
                IdleSelectedItem = item;
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
        public async void IdleGridLoadItems()
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
                q.Request.SetParam("Object", "Idle");
                q.Request.SetParam("Action", "List");

                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
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
                        Grid.UpdateItems(ds);
                    }
                }
            }

            EnableControls();
        }

        public void IdleEdit(Dictionary<string, string> idle)
        {
            if (idle != null)
            {
                int id = idle.CheckGet("IDIDLES").ToInt();

                var idleEditForm = new FormExtend()
                {
                    FrameName = "IdleEdit",
                    ID = "IDIDLES",
                    Id = id,
                    Title = $"Простой {id}",

                    QueryGet = new FormExtend.RequestData()
                    {
                        Module = "Production",
                        Object = "Idle",
                        Action = "Get"
                    },

                    QuerySave = new FormExtend.RequestData()
                    {
                        Module = "Production",
                        Object = "Idle",
                        Action = "Save"
                    },

                    Fields = new List<FormHelperField>()
                    {
                        new FormHelperField()
                        {
                            Path="IDREASON",
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
                            Path="ID_REASON_DETAIL",
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

                idleEditForm["IDREASON"].Validate += (f, v) =>
                {
                    if ((f.Control as SelectBox).ValueTextBox.Text.IsNullOrEmpty())
                    {
                        string errorMessage = "Укажите причину простоя";
                        
                        f.ValidateResult = false;
                        f.ValidateProcessed = true;
                        f.ValidateMessage = errorMessage;

                        var d = new DialogWindow(errorMessage, "Ошибка ввода данных");
                        d.ShowDialog();
                    }
                };

                idleEditForm["ID_REASON_DETAIL"].Validate += (f, v) =>
                {
                    if ((f.Control as SelectBox).ValueTextBox.Text.IsNullOrEmpty())
                    {
                        string errorMessage = "Укажите тип простоя";

                        f.ValidateResult = false;
                        f.ValidateProcessed = true;
                        f.ValidateMessage = errorMessage;

                        var d = new DialogWindow(errorMessage, "Ошибка ввода данных");
                        d.ShowDialog();
                    }
                };


                idleEditForm["IDREASON"].OnAfterCreate += (control) =>
                {
                    var IdleReason = control as SelectBox;
                    IdleReason.Autocomplete = true;

                    (control as SelectBox).SelectedItemChanged += (d, e) =>
                    {
                        FormHelper.ComboBoxInitHelper(idleEditForm["ID_REASON_DETAIL"].Control as SelectBox, "Production", "Idle", "ReasonDetailList", "ID_REASON_DETAIL", "DESCRIPTION", new Dictionary<string, string>() { { "ID_REASON_DETAIL", (d as SelectBox).SelectedItem.Key } }, true, true);
                    };

                    FormHelper.ComboBoxInitHelper(control as SelectBox, "Production", "Idle", "ReasonList", "ID", "NAME", null, true);
                }; 

                idleEditForm.OnAfterSave += (id, result) =>
                {
                    Grid.LoadItems();
                };

                idleEditForm.Show();
            }
        }

        public async void IdleDelete(Dictionary<string, string> idle)
        {
            var dw = new DialogWindow($"Вы действительно хотите удалить простой {idle.CheckGet("FROMDT")}?", "Удаление простоя", "Подтверждение удаления простоя", DialogWindowButtons.NoYes);
            if (dw.ShowDialog() == true)
            {
                var p = new Dictionary<string, string>
                {
                    ["IDIDLES"] = idle.CheckGet("IDIDLES"),
                };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Idle");
                q.Request.SetParam("Action", "Delete");

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
                        Grid.LoadItems();
                    }
                }
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
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            //Form.SetValueByPath("FROM_DATE", DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy"));
            //Form.SetValueByPath("TO_DATE", DateTime.Now.ToString("dd.MM.yyyy"));

            var list = new Dictionary<string, string>();
            list.Add("0", "Все");
            list.Add("2", "ГА-1");
            list.Add("21", "ГА-2");
            list.Add("22", "ГА-3");
            Machines.Items = list;
            Machines.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");

            IdleSelectedItem = new Dictionary<string, string>();
            ShowButton.Style = (Style)ShowButton.TryFindResource("Button");

            var start = DateTime.Now;

            int hour = start.Hour;

            if (hour > 12)
            {
                start = new DateTime(start.Year, start.Month, start.Day, 20, 0, 0);
            }
            else
            {
                start = new DateTime(start.Year, start.Month, start.Day, 8, 0, 0);
            }

            FromDate.EditValue = start;
            ToDate.EditValue = start.AddHours(12);

            LoadIdles();

            //FormHelper.ComboBoxInitHelper(Reason, "Production", "Idle", "ReasonList", "ID", "NAME", null, true);
        }


        /// <summary>
        /// Загрузка справочников
        /// </summary>
        private async void LoadIdles()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Idle");
            q.Request.SetParam("Action", "ReasonList");

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
                    var machineDS = ListDataSet.Create(result, "ITEMS");
                    var machineItems = new Dictionary<string, string>();
                    machineItems.Add("0", "Все");
                    foreach (var row in machineDS.Items)
                    {
                        int machineGroup = row.CheckGet("ID").ToInt();
                        machineItems.Add(machineGroup.ToString(), row.CheckGet("NAME"));
                    }

                    Reason.Items = machineItems;
                    Reason.SetSelectedItemByKey("0");
                }
            }
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
        /// экспорт записей грида в Excel
        /// </summary>
        private async void ExportToExcel()
        {
            DisableControls();

            if (Form.Validate())
            {
                var listString = JsonConvert.SerializeObject(Grid.Items);

                var p = new Dictionary<string, string>()
                {
                    { "DATA_LIST", listString },
                    { "FROM", FromDate.Text },
                    { "TO", ToDate.Text },
                    { "ID_ST", Machines.SelectedItem.Key }
                };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Report");
                q.Request.SetParam("Action", "DownTimeReport");
                q.Request.SetParams(p);

                q.Request.Timeout = 25000;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    Central.OpenFile(q.Answer.DownloadFilePath);
                }
                else
                {
                    q.ProcessError();
                }
            }
            EnableControls();
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
            IdleGridLoadItems();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }
        private void DateTextChanged(object sender, RoutedEventArgs e)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
        }

        private void LeftShiftButton_Click(object sender, RoutedEventArgs e)
        {
            if(FromDate.EditValue is DateTime start)
            {
                start = start.AddHours(-12);
                int hour = start.Hour;

                if (hour > 12)
                {
                    start = new DateTime(start.Year, start.Month, start.Day, 20, 0, 0);
                }
                else
                {
                    start = new DateTime(start.Year, start.Month, start.Day, 8, 0, 0);
                }

                FromDate.EditValue = start;
                ToDate.EditValue = start.AddHours(12);

                IdleGridLoadItems();
            }
        }

        private void RightShiftButton_Click(object sender, RoutedEventArgs e)
        {
            if (FromDate.EditValue is DateTime start)
            {
                start = start.AddHours(12);
                int hour = start.Hour;

                if (hour > 12)
                {
                    start = new DateTime(start.Year, start.Month, start.Day, 20, 0, 0);
                }
                else
                {
                    start = new DateTime(start.Year, start.Month, start.Day, 8, 0, 0);
                }

                FromDate.EditValue = start;
                ToDate.EditValue = start.AddHours(12);

                IdleGridLoadItems();
            }

        }

        private void Reason_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }
    }
}
