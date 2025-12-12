using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Accounts;
using Client.Interfaces.Main;
using DevExpress.Xpf.Core;
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
using static Client.Common.Role;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.MoldedContainer
{
    /// <summary>
    /// Отчёт по простоям на литой таре
    /// </summary>
    /// <author>greshnyh_ni</author>   
    public partial class RecyclingMachineReportIdles : UserControl
    {
        public RecyclingMachineReportIdles()
        {
            InitializeComponent();

            FormInit();
            SetDefaults();
            IdleGridInit();

            Grid.SelectItemMode = 2;

            ProcessPermissions();
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
                        Header="*",
                        Path="_SELECTED",
                        ColumnType=ColumnTypeRef.Boolean,
                        Editable=true,
                        OnClickAction = (row, el) =>
                        {
                            if (Machines.SelectedItem.Key.ToInt() == 0)
                            {
                                if(el is CheckBox box)
                                {
                                    box.IsChecked = false;
                                }

                            }
                            return null;
                        },

                    },
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
                    Width=183,
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
                    Header = "Окончание",
                    Path = "END_DTTM",
                    ColumnType = ColumnTypeRef.String,
                    Width = 130,
                },
                new DataGridHelperColumn
                {
                    Header = "Время простоя (ЧЧ:ММ:СС)",
                    Path = "DT",
                    ColumnType = ColumnTypeRef.String,
                    Width = 104,
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
                    Header = "Время простоя (часы)",
                    Path = "TIME_IDLES_HOUR",
                    ColumnType = ColumnTypeRef.String,
                    Width = 120,
                   Totals = (List<Dictionary<string,string>> rows) =>
                        {
                            // расcчитываем общее время простоев
                            double totalTime = 0;
                            if (rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    double stime= row["TIME_IDLES_HOUR"].ToDouble();
                                    totalTime += stime;
                                }
                            }
                            return $"{totalTime}";
                        },
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
                    Header = "Тип",
                    Path = "NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width = 110,
                },
                new DataGridHelperColumn
                {
                    Header = "Причина",
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

            Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                { "Edit", new DataGridContextMenuItem(){
                    Header="Изменить",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        IdleEdit(IdleSelectedItem);
                    }
                }},
                { "Delete", new DataGridContextMenuItem(){
                    Header="Удалить",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        IdleDelete(IdleSelectedItem);
                    }
                }},
            };

            Grid.PrimaryKey = "_ROWNUMBER";
            Grid.SetColumns(columns);

            Grid.SearchText = SearchText;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = item =>
            {
                IdleSelectedItem = item;
            };

            // расcчитываем общее время простоев
            //TimeSpan totalTime = new TimeSpan();
            //foreach (var row in ds.Items)
            //{
            //    string stime = row.CheckGet("DT");
            //    var span = TimeSpan.Parse(stime);
            //    totalTime += span;
            //}
            //IdlesInfo.Text = $"Время простоев {totalTime}";

            //данные грида
            Grid.OnLoadItems = IdleGridLoadItems;
            // Grid.OnDblClick = IdleEdit;

            Grid.AutoUpdateInterval = 0;
            Grid.SetSorting("FROMDT");

            Grid.Init();
            Grid.Run();
            //фокус ввода       
            Grid.Focus();
        }

        /// <summary>
        /// Получение данных по простоям для заполнения грида
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
                if (Machines.SelectedItem.Key.ToInt() != 0)
                    st = Machines.SelectedItem.Key.ToInt().ToString();

                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_ST", st);
                    p.CheckAdd("FROM_DATE", Form.GetValueByPath("FROM_DATE"));
                    p.CheckAdd("TO_DATE", Form.GetValueByPath("TO_DATE"));
                    p.CheckAdd("IDLES", Idles.SelectedItem.Key.ToInt().ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "MoldedContainer");
                q.Request.SetParam("Object", "Recycling");
                q.Request.SetParam("Action", "DowntimeList");

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
                        SelectAllButton.IsChecked = false;
                        var ds = ListDataSet.Create(result, "ITEMS");
                        Grid.UpdateItems(ds);

                        if (!ReadOnly && ds.Items.Count > 0)
                        {
                            EditButton.IsEnabled = true;
                            DeleteButton.IsEnabled = true;
                            SelectAllButton.IsEnabled = true;
                        }
                        else
                        {
                            EditButton.IsEnabled = false;
                            DeleteButton.IsEnabled = false;
                            SelectAllButton.IsEnabled = false;
                        }

                        // расcчитываем общее время простоев
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
            string role = "";
            // Проверяем уровень доступа
            role = "[erp]molded_contnr_idles";

            var mode = Central.Navigator.GetRoleLevel(role);
            var userAccessMode = mode;

            switch (mode)
            {
                case Common.Role.AccessMode.Special:
                    {
                        AddButton.IsEnabled = false;
                        EditButton.IsEnabled = true;
                        DeleteButton.IsEnabled = false;
                        ReadOnly = false;
                    }
                    break;

                case Common.Role.AccessMode.FullAccess:
                    {
                        AddButton.IsEnabled = true;
                        EditButton.IsEnabled = true;
                        DeleteButton.IsEnabled = true;
                        ReadOnly = false;
                    }
                    break;

                case Common.Role.AccessMode.ReadOnly:
                    {
                        AddButton.IsEnabled = false;
                        EditButton.IsEnabled = false;
                        DeleteButton.IsEnabled = false;
                        ReadOnly = true;
                    }
                    break;
            }

            if (Grid != null && Grid.Menu != null && Grid.Menu.Count > 0)
            {
                foreach (var manuItem in Grid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            //Form.SetValueByPath("FROM_DATE", DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy"));
            //Form.SetValueByPath("TO_DATE", DateTime.Now.ToString("dd.MM.yyyy"));

            // получаем список станков из базы
            /*
                 var list = new Dictionary<string, string>();
                 list.Add("0", "Все");
                 list.Add("311", "Принтер BST[СPH-3430]-1");
                 list.Add("321", "Этикетир. BST[TBH-2438]-1");
                 list.Add("312", "Принтер AAEI [301 P]");
                 list.Add("322", "Этикетир AAEI [301 L]");
                 list.Add("331", "Упаковка ЛТ.");

                 list.Add("301", "ВФМ BST-2 A");
                 list.Add("302", "ВФМ BST-2 B");
                 list.Add("303", "ВФМ BST-1 A");
                 list.Add("304", "ВФМ BST-1 B");

                 Machines.Items = list;
                 Machines.SetSelectedItemByKey("0");
                 //Machines.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
            */

            var list = new Dictionary<string, string>();
            list = new Dictionary<string, string>();
            list.Add("0", "Все");
            list.Add("1", "Менее 10 мин.");
            list.Add("2", "Более 10 мин.");

            Idles.Items = list;
            Idles.SetSelectedItemByKey("0");
            //Idles.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");

            IdleSelectedItem = new Dictionary<string, string>();
            ShowButton.Style = (Style)ShowButton.TryFindResource("Button");

            var start = DateTime.Now;

            int hour = start.Hour;

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
                    Path="MACHINE_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Default="0",
                    Control=Machines,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange=(FormHelperField f, string v)=>
                    {
                        //EmployeeGrid.UpdateItems();
                    },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "MoldedContainer",
                        Object = "Recycling",
                        Action = "StanokList",
                        AnswerSectionKey="ITEMS",
                        OnComplete = (FormHelperField f,ListDataSet ds) =>
                        {
                            //var row = new Dictionary<string, string>()
                            //{
                            //    {"ID", "0" },
                            //    {"NAME_CAR", "Все" },
                            //};
                            //ds.ItemsPrepend(row);
                            var list=ds.GetItemsList("MACHINE_ID","MACHINE_NAME");
                            var c=(SelectBox)f.Control;
                            if(c != null)
                            {
                                c.Items=list;
                                Machines.SetSelectedItemByKey("0.0");
                            }
                        },
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
            if (m.ReceiverGroup.IndexOf("MoldedContainer") > -1)
            {
                if (m.ReceiverName.IndexOf("RecyclingMachineReportIdles") > -1)
                {
                    switch (m.Action)
                    {
                        case "RefreshRecyclingIdlesList":
                            {
                                bool SelectedIdlesItem = false;

                                foreach (var item in Grid.Items)
                                {
                                    if (item["_SELECTED"].ToInt() == 1)
                                    {
                                        SelectedIdlesItem = true;
                                    }
                                }

                                if (SelectedIdlesItem)
                                {
                                    var v = (Dictionary<string, string>)m.ContextObject;
                                    if (v.ContainsKey("DOTY_ID") && v.ContainsKey("DORE_ID"))
                                    {
                                        var par = new Dictionary<string, string>();
                                        {
                                            par.Add("DOTY_ID", v["DOTY_ID"]);
                                            par.Add("DORE_ID", v["DORE_ID"]);
                                            par.Add("NOTE", v["NOTE"]);
                                        }
                                        UpdateCheckIdles(par);
                                    }
                                }
                                else
                                {
                                    IdleGridLoadItems();
                                }
                            }
                            break;
                    }
                }
            }
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
            IdleGridLoadItems();
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
        /// фильтр длительности простоев
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void Idles_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
            Grid.UpdateItems();
        }


        /// <summary>
        /// обновляем информацию по отмеченным простоям
        /// </summary>
        /// <param name="p"></param>
        private void UpdateCheckIdles(Dictionary<string, string> p)
        {
            foreach (var item in Grid.Items)
            {
                if (item["_SELECTED"].ToInt() == 1)
                {
                    {
                        p.CheckAdd("DNTM_ID", item["IDIDLES"].ToString());
                    }
                    SaveData(p);
                }
            }
            IdleGridLoadItems();
        }

        /// <summary>
        /// Запись данных в БД
        /// </summary>
        /// <param name="p"></param>
        private void SaveData(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();

            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "IdlesSave");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;
            q.DoQuery();
        }


        /// <summary>
        /// отметить все записи
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectAllButton_Click_1(object sender, RoutedEventArgs e)
        {
            var selected = (bool)SelectAllButton.IsChecked;

            if (Grid.Items != null)
            {
                if (Grid.Items.Count > 0)
                {
                    foreach (Dictionary<string, string> row in Grid.Items)
                    {
                        row.CheckAdd("_SELECTED", selected ? "1" : "0");
                    }

                    Grid.UpdateItems();
                    //    var selectedItems = Grid.GetSelectedItems();
                }
            }
        }


        /// <summary>
        ///  отчет по простоям
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReportButton_Click(object sender, RoutedEventArgs e)
        {
            Report();
        }

        /// <summary>
        /// формируем отчет по простоям
        /// </summary>
        private async void Report()
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

                q.Request.SetParam("Module", "MoldedContainer");
                q.Request.SetParam("Object", "Recycling");
                q.Request.SetParam("Action", "DownTimeContainerReport");
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
        /// экспорт записей грида в Excel
        /// </summary>
        private async void ExportToExcel()
        {
            DisableControls();

            if (Grid.Items != null)
            {
                if (Grid.Items.Count > 0)
                {
                    var eg = new ExcelGrid();
                    var cols = Grid.Columns;
                    eg.SetColumnsFromGrid(cols);
                    eg.Items = Grid.Items;
                    await Task.Run(() =>
                    {
                        eg.Make();
                    });
                }
            }

            EnableControls();
        }


        /// <summary>
        ///  нажали кнопку "добавить простой"
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

        /// <summary>
        ///  редактируем простой
        /// </summary>
        /// <param name="idle"></param>
        public void IdleEdit(Dictionary<string, string> idle)
        {
            if (idle != null)
            {
                var idleRecord = new RecyclingIdleRecord(idle);
                idleRecord.ReceiverName = "RecyclingMachineReportIdles";
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
                    ["DNTM_ID"] = idle.CheckGet("IDIDLES"),
                };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "MoldedContainer");
                q.Request.SetParam("Object", "Recycling");
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





    }
}
