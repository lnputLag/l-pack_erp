using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Xpf.Core;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// журнал оператора БДМ
    /// </summary>
    /// <author>greshnyh_ni</author>
    public partial class LogbookPaperMachine : UserControl
    {
        public LogbookPaperMachine(int currentMachineId)
        {
            InitializeComponent();
            MachineId = currentMachineId;
            MachineIdCur = currentMachineId;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();
            SetDefaults();
        }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// выбранная запись в гриде проблем
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// Список id и имён всех служб 
        /// </summary>
        private List<(int, string)> DepartmentIdName { get; set; }

        /// <summary>
        /// Список флагов для узлов станка
        /// </summary>
        private Dictionary<string, string> UnitFlags { get; set; }

        /// <summary>
        /// идентификатор записи, с которой работает форма
        /// (primary key записи таблицы)
        /// </summary>
        public int Id { get; set; }
        private int MachineId { get; set; }
        private static int MachineIdCur { get; set; }
        

        /// <summary>
        // инициализация компонентов
        /// </summary>
        public void Init()
        {
            Id = 0;
            FrameName = "LogbookPaperMachine";

            DepartmentIdName = new List<(int, string)>();
            UnitFlags = new Dictionary<string, string>();

            LogbookDepLoadItems();
            LogbookDesicionLoadItems();

            GridUnitInit();
            GridProblemInit();
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            FromDate.Text = DateTime.Now.AddYears(-3).ToString("dd.MM.yyyy");
            ToDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
        }

        /// <summary>
        /// инициализация грида
        /// </summary>
        public void GridProblemInit()
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
                        Header="Номер смены",
                        Path="SM_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Начало смены",
                        Path="SM_DTTM",
                        ColumnType=ColumnTypeRef.String,
                        Width=115,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Создание записи",
                        Path="LOG_DTTM",
                        ColumnType=ColumnTypeRef.String,
                        Width=115,
                    },
                   new DataGridHelperColumn
                    {
                        Header="Узел",
                        Path="NAME_UNIT",
                        ColumnType=ColumnTypeRef.String,
                        Width=150,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ответственная служба",
                        Path="DEP_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width=100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Проблема",
                        Path="PROBLEM",
                        ColumnType=ColumnTypeRef.String,
                        Width=350,
                        MaxWidth=800,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Решение",
                        Path="DECISION",
                        ColumnType=ColumnTypeRef.String,
                        Width=350,
                        MaxWidth=800,
                    },
                };
                GridProblem.SetColumns(columns);

                GridProblem.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    { "Create", new DataGridContextMenuItem(){
                        Header="Добавить",
                        Action=()=>
                        {
                            CreateRecord();
                        }
                    }},
                    { "Edit", new DataGridContextMenuItem(){
                        Header="Изменить",
                        Action=()=>
                        {
                            EditRecord(SelectedItem);
                        }
                    }},
                    { "Delete", new DataGridContextMenuItem(){
                        Header="Удалить",
                        Action=()=>
                        {
                            DeleteRecord();
                        }
                    }},

                };

                GridProblem.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
                GridProblem.SearchText = SearchText;
                GridProblem.UseRowHeader = false;
                GridProblem.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                GridProblem.OnSelectItem = selectedItem =>
                {
                    SelectedItem = selectedItem;
                };

                GridProblem.OnDblClick = EditRecord;

                //данные грида
                GridProblem.OnLoadItems = GridProblemLoadItems;
                GridProblem.OnFilterItems = GridProblemFilterByUnit;

                GridProblem.Run();

                //фокус ввода           
                GridProblem.Focus();
            }
        }

        /// <summary>
        /// инициализация грида
        /// </summary>
        public void GridUnitInit()
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
                        Header = "*",
                        Path="_SELECTED",
                        ColumnType=ColumnTypeRef.Boolean,
                        Editable = true,
                        Width=25,
                        OnClickAction = (row, el) =>
                        {
                            if (row["_SELECTED"].ToInt() == 1)
                            {
                                var rowNameUnit = row.CheckGet("NAME_UNIT");
                                if (UnitFlags.ContainsKey(rowNameUnit))
                                {
                                    UnitFlags[rowNameUnit] = "true";
                                }
                            }
                            else
                            {
                                var rowNameUnit = row.CheckGet("NAME_UNIT");
                                if (UnitFlags.ContainsKey(rowNameUnit))
                                {
                                    UnitFlags[rowNameUnit] = "false";
                                }
                            }
                            GridProblemLoadItems();
                            return null;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Узел",
                        Path="NAME_UNIT",
                        ColumnType=ColumnTypeRef.String,
                        Width=160,
                    },
                };
                GridUnit.SetColumns(columns);

                GridUnit.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
                GridUnit.UseRowHeader = false;
                GridUnit.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                GridUnit.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {
                        //UpdateActions(selectedItem);
                    }
                };

                //данные грида
                GridUnit.OnLoadItems = GridUnitLoadItems;

                GridUnit.Run();

                //фокус ввода           
                GridUnit.Focus();
            }
        }

        /// <summary>
        /// получение записей журнала оператора ГА
        /// </summary>
        public async void GridProblemLoadItems()
        {
            DisableControls();

            var p = new Dictionary<string, string>();
            p.CheckAdd("ID_ST", MachineId.ToString());
            p.CheckAdd("STLD_ID", GetIdByNameDepartment((Department.SelectedItem as string)).ToString());
            p.CheckAdd("FROM_DATE", FromDate.Text);
            // добавляем день, чтобы отображались записи за весь текущий день
            p.CheckAdd("TO_DATE", ToDate.Text.ToDateTime().AddDays(1).ToString("dd.MM.yyyy"));

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatorLog");
            q.Request.SetParam("Action", "List");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestGridAttempts;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "LOGBOOK");
                    GridProblem.UpdateItems(ds);
                }
            }

            EnableControls();
            CheckGridItemsCount();
        }

        /// <summary>
        /// получение списка узлов станка
        /// </summary>
        public async void GridUnitLoadItems()
        {
            DisableControls();

            var ds = await GetUnits();
            foreach (var item in ds.Items)
            {
                item["_SELECTED"] = "1";
                UnitFlags[item["NAME_UNIT"]] = "true";
            }

            GridUnit.UpdateItems(ds);

            GridProblemLoadItems();

            EnableControls();
        }


        /// <summary>
        /// получение списка узлов станка
        /// </summary>
        public async static Task<ListDataSet> GetUnits()
        {
            var resultLDS = new ListDataSet();

            var p = new Dictionary<string, string>();
            p.CheckAdd("ID_ST", MachineIdCur.ToString());
            

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatorLog");
            q.Request.SetParam("Action", "ListUnit");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestGridAttempts;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    resultLDS = ListDataSet.Create(result, "UNITS");
                }
            }

            return resultLDS;
        }

        /// <summary>
        /// Получение списка служб для журнала оператора ГА
        /// </summary>
        public async void LogbookDepLoadItems()
        {
            DisableControls();

            DepartmentIdName = new List<(int, string)>();
            var departments = new List<string>();

            var ds = await GetDepartments();
            var items = ds?.Items;
            foreach (var item in items)
            {
                var stldId = item?.CheckGet("STLD_ID").ToInt();
                var department = item?.CheckGet("NAME");
                DepartmentIdName.Add((stldId ?? 0, department));
                departments.Add(department);
            }
            Department.ItemsSource = departments;

            if (departments.Count > 0)
            {
                Department.SelectedIndex = 0;
            }

            EnableControls();
        }

        /// <summary>
        /// Получение списка служб для журнала оператора ГА
        /// </summary>
        public async static Task<ListDataSet> GetDepartments()
        {
            var resultLDS = new ListDataSet();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatorLog");
            q.Request.SetParam("Action", "ListDepartment");

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestGridAttempts;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    resultLDS = ListDataSet.Create(result, "DEPARTMENTS");
                }
            }

            return resultLDS;
        }

        /// <summary>
        /// Получение указаний от технологов
        /// </summary>
        public async void LogbookDesicionLoadItems()
        {
            DisableControls();

            var p = new Dictionary<string, string>();
            p.CheckAdd("ID_ST", MachineId.ToString());
            // есть указание от технологов
            p.CheckAdd("IS_DIRECTION", "1");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatorLog");
            q.Request.SetParam("Action", "GetDecision");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestGridAttempts;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "DECISION");

                    Desicion.Text = ds.Items[0]["DECISION"];
                }
            }

            EnableControls();
        }

        /// <summary>
        /// Проверка записей на соответствие выделенным узлам
        /// </summary>
        private void GridProblemFilterByUnit()
        {
            if (GridProblem.GridItems != null)
            {
                if (GridProblem.GridItems.Count > 0)
                {
                    var items = new List<Dictionary<string, string>>();
                    foreach (var item in GridProblem.GridItems)
                    {
                        var itemNameUnit = item.CheckGet("NAME_UNIT");
                        var unitFlag = UnitFlags.CheckGet(itemNameUnit);

                        if (unitFlag.ToBool())
                        {
                            items.Add(item);
                        }
                    }
                    GridProblem.GridItems = items;
                }
            }
        }

        public async void DeleteProblem()
        {
            DisableControls();

            var p = new Dictionary<string, string>();
            p.CheckAdd("ID_LOGBOOK", SelectedItem.CheckGet("ID_LOGBOOK"));

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatorLog");
            q.Request.SetParam("Action", "Delete");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestGridAttempts;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var ds = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (ds != null)
                {
                    if (ds.ContainsKey("ITEMS"))
                    {
                        // если ответ не пустой, обновляем таблицу
                        GridProblemLoadItems();
                    }
                }
            }

            EnableControls();
        }

        private int GetIdByNameDepartment(string name)
        {
            var result = 0;
            foreach (var departmentIdName in DepartmentIdName)
            {
                if (departmentIdName.Item2 == name)
                {
                    result = departmentIdName.Item1;
                }
            }
            return result;
        }

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Production",
                ReceiverName = "",
                SenderName = "LogbookPaperMachine",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;
                case Key.Enter:
                    EditRecord(SelectedItem);
                    e.Handled = true;
                    break;
            }
        }


        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            Central.WM.FrameMode = 1;

            var frameName = GetFrameName();

            Central.WM.Show(frameName, "Журнал оператора БДМ", true, "add", this);

        }

        public void Close()
        {
            var frameName = GetFrameName();
            Central.WM.Close(frameName);
            Destroy();
        }

        public void CreateRecord()
        {
            var logbookRecord = new LogbookRecordPaperMachine(MachineId);
            logbookRecord.OnClose += GridProblemLoadItems;
            logbookRecord.Show();
        }

        public void EditRecord(object selectedItem)
        {
            var logbookRecord = new LogbookRecordPaperMachine(MachineId, selectedItem as Dictionary<string, string>);
            logbookRecord.OnClose += GridProblemLoadItems;
            logbookRecord.Show();
        }

        public void DeleteRecord()
        {
            if (SelectedItem != null)
            {
                var d = new DialogWindow($"Вы действительно хотите удалить запись о проблеме \"{SelectedItem.CheckGet("PROBLEM")}\"", "Удаление записи", "", DialogWindowButtons.YesNo);
                d.ShowDialog();
                if (d.DialogResult == true)
                {
                    DeleteProblem();
                }
            }
        }

        /// <summary>
        /// формирует уникальный идентификатор фрейма
        /// </summary>
        /// <returns></returns>
        public string GetFrameName()
        {
            string result = "";
            result = $"{FrameName}_{Id}";
            return result;
        }

        public void CheckGridItemsCount()
        {
            if (GridProblem.GridItems != null)
            {
                if (GridProblem.GridItems.Count > 0)
                {
                    ChengeButton.IsEnabled = true;
                    DeleteButton.IsEnabled = true;
                }
                else
                {
                    ChengeButton.IsEnabled = false;
                    DeleteButton.IsEnabled = false;
                }
            }
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
        }

        private void CloseButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }
        private void UpdateButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            GridProblemLoadItems();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            CreateRecord();
        }

        private void ChangeButton_Click(object sender, RoutedEventArgs e)
        {
            EditRecord(SelectedItem);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteRecord();
        }

        private void SelectAllCheckBoxOnClick(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                var c = (System.Windows.Controls.CheckBox)sender;
                var flag = (bool)c.IsChecked;

                if (GridUnit?.Items?.Count > 0)
                {
                    foreach (var item in GridUnit.Items)
                    {
                        var itemNameUnit = item.CheckGet("NAME_UNIT");
                        if (flag)
                        {
                            item.CheckAdd("_SELECTED", "1");
                            UnitFlags.CheckAdd(itemNameUnit, "true");
                        }
                        else
                        {
                            item.CheckAdd("_SELECTED", "0");
                            UnitFlags.CheckAdd(itemNameUnit, "false");
                        }
                    }

                    GridUnit.UpdateItems();
                    GridProblemLoadItems();
                }
            }
        }

        private void Department_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GridProblemLoadItems();
        }

        private void DateTextChanged(object sender, TextChangedEventArgs args)
        {
            GridProblemLoadItems();
        }
    }
}
