using Client.Assets.HighLighters;
using Client.Common;
using Client.Common.Lib;
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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Client.Common.FormHelperField;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Отчёт по простоям на БДМ
    /// </summary>
    /// <author>greshnyh_ni</author>   
    public partial class PaperMachineReportIdles : ControlBase
    {
        public PaperMachineReportIdles()
        {
            InitializeComponent();

            OnLoad = () =>
            {
                IdleGridInit();
                FormInit();
                SetDefaults();

                ProcessPermissions();
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

            OnNavigate = () =>
            {

            };



            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Выбранная запись в гриде списаний
        /// </summary>
        public Dictionary<string, string> IdleSelectedItem { get; set; }

        private bool ReadOnly { get; set; }

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
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="IDIDLES",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header="Станок",
                    Path="MACHINE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header = "Смена",
                    Path = "WOTE_ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 5,
                },
                new DataGridHelperColumn
                {
                    Header = "Начало",
                    Path = "FROMDT",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 14,
                },
                new DataGridHelperColumn
                {
                    Header = "Окончание",
                    Path = "END_DTTM",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 14,
                },
                new DataGridHelperColumn
                {
                    Header = "Время простоя (д:ч:м:с)",
                    Path = "DT",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 10,
                   Totals = (List<Dictionary<string,string>> rows) =>
                        {
                            // расcчитываем общее время простоев
                            TimeSpan totalTime = new TimeSpan();
                            if (rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    string stime= row["DT"];
                                    var span = TimeSpan.Parse(stime);
                                    totalTime += span;
                                }
                            }
                            return $"{totalTime}";
                        },
                },
                new DataGridHelperColumn
                {
                    Header = "Тип",
                    Path = "NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 15,
                },
                new DataGridHelperColumn
                {
                    Header = "Причина",
                    Path = "DESCRIPTION",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 20,
                },
                new DataGridHelperColumn
                {
                    Header = "Описание",
                    Path = "REASON",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 42,
                },
                new DataGridHelperColumn
                {
                    Header="Граммаж",
                    Path="ROLL_RO",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Скорость",
                    Path="BDM_SPEED_IDLES",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
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
            
            /*
            Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                { "Edit", new DataGridContextMenuItem(){
                    Header="Изменить",
                    Action=()=>
                    {
                        IdleEdit(IdleSelectedItem);
                    }
                }},
                { "Delete", new DataGridContextMenuItem(){
                    Header="Удалить",
                    Action=()=>
                    {
                        IdleDelete(IdleSelectedItem);
                    }
                }},
            };
            */

            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("IDIDLES");
            Grid.SetSorting("FROMDT", ListSortDirection.Ascending);
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

            Grid.SearchText = SearchText;
            Grid.OnLoadItems = IdleGridLoadItems;
           // Grid.OnDblClick = IdleEdit;

            Grid.OnFilterItems = () =>
            {
                if (Grid.Items.Count > 0)
                {
                    {
                        var showAll = false;
                        var v = Form.GetValues();
                        var idleReasonId = v.CheckGet("NAME").ToInt();

                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in Grid.Items)
                        {
                            var includeRowByDepartment = false;

                            if (idleReasonId != 0)
                            {
                                if (row.CheckGet("IDREASON").ToInt() == idleReasonId)
                                {
                                    includeRowByDepartment = true;
                                }
                            }
                            else
                            {
                                includeRowByDepartment = true;
                            }

                            if (includeRowByDepartment
                            )
                            {
                                items.Add(row);
                            }
                        }
                        Grid.Items = items;

                        //// расcчитываем общее время простоев
                        //TimeSpan totalTime = new TimeSpan();
                        //foreach (var row in Grid.Items)
                        //{
                        //    string stime = row.CheckGet("DT");
                        //    if (!stime.IsNullOrEmpty())
                        //    {
                        //        var span = TimeSpan.Parse(stime);
                        //        totalTime += span;
                        //    }
                        //}
                        //IdlesInfo.Text = $"Время простоев {totalTime}";
                        ////IdleGridLoadItems();
                    }
                }
            };

            Grid.OnSelectItem = item =>
            {
                IdleSelectedItem = item;
            };

            Grid.Init();
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
                var st = "";
                if (Machines.SelectedItem.Key.ToInt().ToString() != "0")
                {
                    st = Machines.SelectedItem.Key.ToInt().ToString();
                }

                var p = new Dictionary<string, string>
                {
                    ["ID_ST"] = st,
                    ["FROM_DT"] = Form.GetValueByPath("FROM_DATE"),
                    ["TO_DT"] = Form.GetValueByPath("TO_DATE"),
                };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "Monitoring");
                q.Request.SetParam("Action", "ListIdlesByIdSt");

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
                        Grid.UpdateItems(ds);

                        if (!ReadOnly && ds.Items.Count > 0)
                        {
                            EditButton.IsEnabled = true;
                            DeleteButton.IsEnabled = true;
                        }
                        else
                        {
                            EditButton.IsEnabled = false;
                            DeleteButton.IsEnabled = false;
                        }

                        //// расcчитываем общее время простоев
                        //TimeSpan totalTime = new TimeSpan();
                        //foreach (var row in ds.Items)
                        //{
                        //    string stime = row.CheckGet("DT");
                        //    var span = TimeSpan.Parse(stime);
                        //    totalTime += span;
                        //}
                        //IdlesInfo.Text = $"Время простоев {totalTime}";
                    }
                }
            }

            EnableControls();
        }

        public void IdleEdit(Dictionary<string, string> idle)
        {
            if (idle != null)
            {
                var idleRecord = new IdlesLogRecord(Machines.SelectedItem.Key.ToInt(), idle as Dictionary<string, string>);
                idleRecord.ReceiverName = "PaperMachineReportIdles";
                idleRecord.Edit();
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
                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "Monitoring");
                q.Request.SetParam("Action", "IdlesDelete");

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
        /// Управление доступом
        /// </summary>
        /// <param name="roleCode"></param>
        private void ProcessPermissions(string roleCode = "")
        {

            ReadOnly = true;

            if (Machines.SelectedItem.Key.ToInt() == 0)
            {
                AddButton.IsEnabled = false;
                EditButton.IsEnabled = false;
                DeleteButton.IsEnabled = false;
            }
            else
            {
                string role = "";
                // Проверяем уровень доступа
                if (Machines.SelectedItem.Key.ToInt().ToString() == "716")
                    role = "[erp]bdm1_downtime";
                else if (Machines.SelectedItem.Key.ToInt().ToString() == "1716")
                    role = "[erp]bdm2_downtime";

                var mode = Central.Navigator.GetRoleLevel(role);
                var userAccessMode = mode;

                switch (mode)
                {
                    case Role.AccessMode.Special:
                        {
                            AddButton.IsEnabled = false;
                            EditButton.IsEnabled = true;
                            DeleteButton.IsEnabled = false;
                            ReadOnly = false;
                        }

                        break;

                    case Role.AccessMode.FullAccess:
                        {
                            AddButton.IsEnabled = true;
                            EditButton.IsEnabled = true;
                            DeleteButton.IsEnabled = true;
                            ReadOnly = false;
                        }
                        break;

                    case Role.AccessMode.ReadOnly:
                        {
                            AddButton.IsEnabled = false;
                            EditButton.IsEnabled = false;
                            DeleteButton.IsEnabled = false;
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            var list = new Dictionary<string, string>();
            list.Add("0", "Все");
            list.Add("716", "БДМ");
            list.Add("1716", "БДМ-2");
            Machines.Items = list;
            Machines.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");

            IdleSelectedItem = new Dictionary<string, string>();
            ShowButton.Style = (Style)ShowButton.TryFindResource("Button");

            var start = DateTime.Now;

            int hour = start.Hour;

            //if (hour > 12)
            if (hour > 20)
            {
                start = new DateTime(start.Year, start.Month, start.Day, 20, 0, 0);
            }
            else
            {
                start = new DateTime(start.Year, start.Month, start.Day, 8, 0, 0);
            }

            FromDate.EditValue = start;
            ToDate.EditValue = start.AddHours(12);
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

                new FormHelperField()
                {
                    Path="NAME",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Default="0",
                    Control=Name,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange=(FormHelperField f, string v)=>
                    {
                        Grid.UpdateItems();
                    },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "ProductionPm",
                        Object = "Monitoring",
                        Action = "IdleGroup",
                        AnswerSectionKey="ITEMS",
                        OnComplete = (FormHelperField f,ListDataSet ds) =>
                        {
                            var row = new Dictionary<string, string>()
                            {
                                {"ID", "0" },
                                {"NAME", "Все" },
                            };
                            ds.ItemsPrepend(row);
                            var list=ds.GetItemsList("ID","NAME");
                            var c=(SelectBox)f.Control;

                            if(c != null)
                            {
                                c.Items=list;
                            }
                            Name.SetSelectedItemByKey("0");
                        },
                    },
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = GridToolbar;
            double nScale = 1.5;
            Grid.LayoutTransform = new ScaleTransform(nScale, nScale);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("PaperMachine") > -1)
            {
                if (m.ReceiverName.IndexOf("PaperMachineReportIdles") > -1)
                {
                    switch (m.Action)
                    {
                        case "RefreshPaperMachineIdlesList":
                            IdleGridLoadItems();

                            break;
                    }
                }
            }
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
                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "Monitoring");
                q.Request.SetParam("Action", "IdlesReport");
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
            ProcessPermissions();
        }

        /// <summary>
        /// Документация
        /// </summary>
        private void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/production/machine_report/report_write_off");
        }

        private void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("Button");
            IdleGridLoadItems();
        }

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

        private void ButtonEdit_Click(object sender, RoutedEventArgs e)
        {
            IdleEdit(IdleSelectedItem);
        }
        private void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            IdleDelete(IdleSelectedItem);
        }

        private void LeftShiftButton_Click(object sender, RoutedEventArgs e)
        {
            if (FromDate.EditValue is DateTime start)
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

        /// <summary>
        ///  добавляем вручную запись о сбое
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("", "");
            }

            IdleEdit(p);
        }



        /////
    }
}
