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
    /// Отчёт по браку (лишним метрам) на гофроагрегате
    /// </summary>
    /// <author>vlasov_ea</author> 
    public partial class CorrugatorMachineReportDefects : ControlBase
    {
        public CorrugatorMachineReportDefects()
        {
            InitializeComponent();

            ControlTitle = "Брак ГА";
            DocumentationUrl = "/doc/l-pack-erp/";
            RoleName = "[erp]machine_report";


            OnLoad = () =>
            {
                DefectGridInit();
            };

            OnUnload = () =>
            {
                GridDefects.Destruct();
            };

            OnFocusGot = () =>
            {
                GridDefects.ItemsAutoUpdate = true;
                GridDefects.Run();
            };

            OnFocusLost = () =>
            {
                GridDefects.ItemsAutoUpdate = false;
            };



            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            FormInit();
            SetDefaults();

            //IdleGridInit();
            //ProcessPermissions();

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
                        GridDefects.LoadItems();
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
                        DefectEdit(SelectedDefect);
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
                        DefectDelete(SelectedDefect);
                    },
                });

                Commander.Init(this);
            }
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        public Dictionary<string, string> SelectedDefect { get; set; }

        /// <summary>
        /// Инициализация грида
        /// </summary>
        public void DefectGridInit()
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
                    Header = "Время",
                    Path = "DTTM",
                    ColumnType = ColumnTypeRef.String,
                    Width = 120,
                },
                new DataGridHelperColumn
                {
                    Header = "ИД ПЗ",
                    Path = "ID_PZ",
                    ColumnType = ColumnTypeRef.Integer,
                    Width = 60,
                },
                new DataGridHelperColumn
                {
                    Header = "Номер",
                    Path = "NUM",
                    ColumnType = ColumnTypeRef.String,
                    Width = 80,
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
                    Header = "Причина",
                    Path = "REASON",
                    ColumnType = ColumnTypeRef.String,
                    Width = 350,
                },
                new DataGridHelperColumn
                {
                    Header = "Комментарий",
                    Path = "COMMENTS",
                    ColumnType = ColumnTypeRef.String,
                    Width = 400,
                    MaxWidth = 800,
                },
                new DataGridHelperColumn
                {
                    Header = "Тип",
                    Path = "TYPE",
                    ColumnType = ColumnTypeRef.String,
                    Width = 200,
                },
                new DataGridHelperColumn
                {
                    Header = "Плановая длина",
                    Path = "LENGTH_PLAN",
                    ColumnType = ColumnTypeRef.Integer,
                    Width = 60,
                },
                new DataGridHelperColumn
                {
                    Header = "Фактическая длина",
                    Path = "LENGTH_FACT",
                    ColumnType = ColumnTypeRef.Integer,
                    Width = 60,
                },
                new DataGridHelperColumn
                {
                    Header = "Добавлено, м",
                    Path = "LENGTH_EXTRA",
                    ColumnType = ColumnTypeRef.Integer,
                    Width = 60,
                },
                new DataGridHelperColumn
                {
                    Header = "Добавлено, м2",
                    Path = "SQUARE_EXTRA",
                    ColumnType = ColumnTypeRef.Integer,
                    Width = 60,
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

            //Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            //{
            //    { "Edit", new DataGridContextMenuItem(){
            //        Header="Изменить",
            //        Action=()=>
            //        {
            //            DefectEdit(SelectedDefect);
            //        }
            //    }},
            //    { "Delete", new DataGridContextMenuItem(){
            //        Header="Удалить",
            //        Action=()=>
            //        {
            //            DefectDelete(SelectedDefect);
            //        }
            //    }},
            //};

            GridDefects.SetColumns(columns);
            GridDefects.SearchText = SearchText;
            GridDefects.OnLoadItems = DefectGridLoadItems;
            GridDefects.OnDblClick = DefectEdit;
            GridDefects.OnSelectItem = item =>
            {
                SelectedDefect = item;
            };

            GridDefects.Commands = Commander;

            GridDefects.AutoUpdateInterval = 0;
            GridDefects.Init();
            GridDefects.Run();
            GridDefects.Focus();
        }

        /// <summary>
        /// Получение данных 
        /// </summary>
        public async void DefectGridLoadItems()
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
                q.Request.SetParam("Object", "Defect");
                q.Request.SetParam("Action", "List");

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
                        var ds = ListDataSet.Create(result, "ITEMS");
                        GridDefects.UpdateItems(ds);
                    }
                }
            }

            EnableControls();
        }

        public void DefectEdit(Dictionary<string, string> defect)
        {
            if (defect != null)
            {
                int id = defect.CheckGet("PRCR_ID").ToInt();
                string num = defect.CheckGet("NUM");

                var defectEditForm = new FormExtend()
                {
                    FrameName = "DefectEdit",
                    ID = "PRCR_ID",
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
                    var DefectReason = control as SelectBox;
                    DefectReason.Autocomplete = true;

                    FormHelper.ComboBoxInitHelper(control as SelectBox, "Production", "Defect", "ListReason", "ID", "REASON", null, true);
                };

                defectEditForm.OnAfterSave += (id, result) =>
                {
                    GridDefects.LoadItems();
                };

                defectEditForm.Show();
            }
        }

        public async void DefectDelete(Dictionary<string, string> defect)
        {
            var dw = new DialogWindow($"Вы действительно хотите удалить простой {defect.CheckGet("DTTM")}?", "Удаление простоя", "Подтверждение удаления простоя", DialogWindowButtons.NoYes);
            if (dw.ShowDialog() == true)
            {
                var p = new Dictionary<string, string>
                {
                    ["PRCR_ID"] = defect.CheckGet("PRCR_ID"),
                };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Defect");
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
                        GridDefects.LoadItems();
                    }
                }
            }
        }

        /// <summary>
        /// Деактивация контролов
        /// </summary>
        public void DisableControls()
        {
            GridDefects.ShowSplash();
            GridToolbar.IsEnabled = false;
        }

        /// <summary>
        /// Активация контролов
        /// </summary>
        public void EnableControls()
        {
            GridDefects.HideSplash();
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

            SelectedDefect = new Dictionary<string, string>();
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
        /// экспорт записей грида в Excel
        /// </summary>
        private async void ExportToExcel()
        {
            GridDefects.ItemsExportExcel();
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

        //private void ShowButton_Click(object sender, RoutedEventArgs e)
        //{
        //    ShowButton.Style = (Style)ShowButton.TryFindResource("Button");
        //    DefectGridLoadItems();
        //}

        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }
        private void DateTextChanged(object sender, RoutedEventArgs e)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
        }

        
    }
}
