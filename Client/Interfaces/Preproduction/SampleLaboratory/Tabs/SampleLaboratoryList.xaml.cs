using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс образцов для лаборатории
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleLaboratoryList : ControlBase
    {
        public SampleLaboratoryList()
        {
            InitializeComponent();
            ControlTitle = "Образцы для лаборатории";
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/samples_new/laboratory_samples";
            RoleName = "[erp]sample_laboratory";

            OnLoad = () =>
            {
                SetDefaults();
                InitGrid();

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

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    switch (e.Key)
                    {
                        case Key.F1:
                            ProcessCommand("help");
                            e.Handled = true;
                            break;
                    }
                }

                if (!e.Handled)
                {
                    Grid.ProcessKeyboardEvents(e);
                }
            };

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverGroup == "PreproductionSample")
                {
                    if (msg.ReceiverName == ControlName)
                    {
                        ProcessCommand(msg.Action,msg);
                    }
                }
            };
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

            //UIUtil.SetFrameworkElementEnabledByTagAccessMode(this.Content as DependencyObject, Acl.AccessMode.ReadOnly);

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
        /// Обработка и выполнение команд
        /// </summary>
        /// <param name="command"></param>
        public void ProcessCommand(string command, ItemMessage m=null)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "refresh":
                        Grid.LoadItems();
                        if(m!=null)
                        {
                            var id=m.Message.ToString();
                            if(!id.IsNullOrEmpty())
                            {
                                Grid.SelectRowByKey(id);
                            }
                        }
                        break;

                    case "update":
                        Grid.UpdateItems();
                        break;

                    case "help":
                        Central.ShowHelp(DocumentationUrl);
                        break;

                    case "add":
                        SampleEdit(0);
                        break;

                    case "edit":
                        SampleEdit(Grid.SelectedItem.CheckGet("ID").ToInt());
                        break;

                    case "delete":
                        // Вместо удаления ставим у образца статус Отменен
                        UpdateStatus(SampleStates.Rejected);
                        break;

                    case "techmap":
                        AttachTechCard();
                        break;

                }
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            FromDate.Text = DateTime.Now.AddDays(-7).ToString("dd.MM.yyyy");
            ToDate.Text = DateTime.Now.AddDays(7).ToString("dd.MM.yyyy");

            // Список статусов для фильтра
            var statusList = new Dictionary<string, string>()
            {
                { "-1", "Все" },
                { "1", "В работе" },
                { "2", "Изготовленные" },
                { "3", "Полученные" },
            };
            SampleStatus.Items = statusList;
            SampleStatus.SetSelectedItemByKey("1");
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
                    Header = "№",
                    Path = "_ROWNUMBER",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 40,
                    MaxWidth = 40,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header = "Ид",
                    Path = "ID",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 40,
                    MaxWidth = 40,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header="Дата заявки",
                    Path="CREATED_DTTM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth=60,
                    MaxWidth=120,
                    Format="dd.MM.yyyy HH:mm",
                    Width2=14,
                },
                new DataGridHelperColumn
                {
                    Header="Дата изготовления",
                    Path="COMPLETED_DTTM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth=60,
                    MaxWidth=120,
                    Format="dd.MM.yyyy HH:mm",
                    Width2=14,
                },
                new DataGridHelperColumn
                {
                    Header="Образец",
                    Path="SAMPLE_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=300,
                    Width2=36,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=40,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header="Картон",
                    Path="CARDBOARD_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=250,
                    Width2=26,
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=70,
                    Width2=9,
                },
                new DataGridHelperColumn
                {
                    Header="Сообщений от коллег",
                    Path="UNREAD_MESSAGE_QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=40,
                    Stylers=new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row=>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("UNREAD_MESSAGE_QTY").ToInt() > 0)
                                {
                                    color = HColor.Red;
                                }
                                else if (row.CheckGet("MESSAGE_QTY").ToInt() > 0)
                                {
                                    color = HColor.YellowOrange;
                                }
                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header="Ответственный",
                    Path="RESPONSIBLE_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=30,
                    MaxWidth=150,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Код Статуса",
                    Path="STATUS_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Код ответственного",
                    Path="RESPONSIBLE_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Сотрудник лаборатории",
                    Path="IS_LABORANT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ID чата с коллегами",
                    Path="CHAT_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };

            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("ID");
            Grid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;

            // Раскраска строк
            Grid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                // Цвета фона строк
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        // Образцы получены
                        if (row["STATUS_ID"].ToInt() == SampleStates.Shipped)
                        {
                            color = HColor.Green;
                        }
                        // Образцы от клиентов, по которым ведется переписка
                        else if (row["IS_LABORANT"].ToInt() == 0)
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
                // Цвета шрифта строк
                {
                    DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        // Образцы изготовлены или переданы
                        if (row["STATUS_ID"].ToInt().ContainsIn(3, 7))
                        {
                            color = HColor.GreenFG;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },

            };

            Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                { "SetReceived", new DataGridContextMenuItem(){
                    Header="Отметить получение",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        UpdateStatus(SampleStates.Shipped);
                    }
                }},
                { "OpenInnerChat",
                    new DataGridContextMenuItem()
                    {
                        Header="Открыть внутренний чат",
                        Action=() =>
                        {
                            OpenChat(1);
                        }
                    }
                },
                { "Files", new DataGridContextMenuItem(){
                    Header="Прикреплённные файлы",
                    Action=()=>
                    {
                        OpenAttachments();
                    }
                }},
                { "AttachMap", new DataGridContextMenuItem(){
                    Header="Прикрепить ТК",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        AttachTechCard();
                    }
                }},
            };

            Grid.SearchText = SearchText;
            Grid.OnLoadItems = LoadItems;
            Grid.OnFilterItems = FilterItems;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };

            Grid.Init();
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        private async void LoadItems()
        {
            GridToolbar.IsEnabled = false;

            bool resume = true;

            int emplId = Central.User.EmployeeId;
            if ((bool)AllEmployeeCheckBox.IsChecked)
            {
                emplId = 0;
            }

            if (resume)
            {
                var f = FromDate.Text.ToDateTime();
                var t = ToDate.Text.ToDateTime();
                if (DateTime.Compare(f, t) > 0)
                {
                    var msg = "Дата начала не должна быть больше даты окончания.";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();
                    resume = false;
                }
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "Samples");
                q.Request.SetParam("Action", "ListLaboratory");
                q.Request.SetParam("FROM_DATE", FromDate.Text);
                q.Request.SetParam("TO_DATE", ToDate.Text);
                q.Request.SetParam("EMPLOYEE_ID", emplId.ToString());

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
                        var sampleDS = ListDataSet.Create(result, "SAMPLES");
                        Grid.UpdateItems(sampleDS);
                        Grid.CellHeaderWidthProcess();
                        RefreshButton.Style = (Style)RefreshButton.TryFindResource("Button");
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }


            GridToolbar.IsEnabled = true;
        }

        /// <summary>
        /// Фильрация строк
        /// </summary>
        private void FilterItems()
        {
            EditButton.IsEnabled = false;
            DeleteButton.IsEnabled = false;
            AttachTKButton.IsEnabled = false;

            if (Grid.GridItems != null)
            {
                if (Grid.GridItems.Count > 0)
                {
                    bool doFilteringByStatus = false;
                    int status = -1;
                    if (SampleStatus.SelectedItem.Key != null)
                    {
                        status = SampleStatus.SelectedItem.Key.ToInt();
                        if (status >= 0)
                        {
                            doFilteringByStatus = true;
                        }
                    }

                    if (doFilteringByStatus)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in Grid.GridItems)
                        {
                            bool includeByStatus = true;

                            if (doFilteringByStatus)
                            {
                                int statusId = row.CheckGet("STATUS_ID").ToInt();
                                includeByStatus = false;

                                switch (status)
                                {
                                    // В работе, включая новые
                                    case 1:
                                        if (statusId == 0 || statusId == 1)
                                        {
                                            includeByStatus = true;
                                        }
                                        break;
                                    // Изготовленные и Переданные
                                    case 2:
                                        if (statusId == 3 || statusId == 7)
                                        {
                                            includeByStatus = true;
                                        }
                                        break;
                                    // Полученные и отгруженные
                                    case 3:
                                        if (statusId == 4 || statusId == 5)
                                        {
                                            includeByStatus = true;
                                        }
                                        break;
                                }
                            }

                            if (includeByStatus)
                            {
                                items.Add(row);
                            }
                        }
                        Grid.GridItems = items;
                    }
                }

                // Если после фильтрации в таблице не осталось ни одной строки, заблокируем некоторые кнопки
                if (Grid.GridItems.Count > 0)
                {
                    EditButton.IsEnabled = true;
                    DeleteButton.IsEnabled = true;
                    AttachTKButton.IsEnabled = true;

                    ProcessPermissions();
                }
            }

        }

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem"></param>
        public void UpdateActions(Dictionary<string, string> selectedItem)
        {
            int status = Grid.SelectedItem.CheckGet("STATUS_ID").ToInt();
            Grid.Menu["SetReceived"].Enabled = status.ContainsIn(3, 4, 7);
        }

        /// <summary>
        /// Обновление статуса образца
        /// </summary>
        /// <param name="newStatus"></param>
        private async void UpdateStatus(int newStatus)
        {
            if (Grid.SelectedItem != null)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "Samples");
                q.Request.SetParam("Action", "UpdateStatus");
                q.Request.SetParam("SAMPLE_ID", Grid.SelectedItem.CheckGet("ID"));
                q.Request.SetParam("STATUS", newStatus.ToString());

                await Task.Run(() =>
                {
                    q.DoQuery();
                }
                );

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        if (result.Count > 0)
                        {
                            Grid.LoadItems();
                        }
                    }
                }

            }
        }

        /// <summary>
        /// Открывает форму редактирования образца
        /// </summary>
        /// <param name="id"></param>
        private void SampleEdit(int id)
        {
            var sampleForm = new Sample();
            sampleForm.ReceiverName = ControlName;
            sampleForm.Edit(id);
        }

        private void AttachTechCard()
        {
            var attachMapForm = new SampleAttachTechnologicalMap();
            attachMapForm.SampleId = Grid.SelectedItem.CheckGet("ID").ToInt();
            attachMapForm.ReceiverName = ControlName;
            attachMapForm.Show();
        }

        /// <summary>
        /// Открытие вкладки с чатом по образцу
        /// </summary>
        private void OpenChat(int chatType = 0)
        {
            if (Grid.SelectedItem != null)
            {
                var chatFrame = new SampleChat();
                chatFrame.ChatType = chatType;
                chatFrame.ChatObject = "Sample";
                chatFrame.ChatId = Grid.SelectedItem.CheckGet("CHAT_ID").ToInt();
                chatFrame.ObjectId = Grid.SelectedItem.CheckGet("ID").ToInt();
                chatFrame.ReceiverName = ControlName;
                chatFrame.Edit();
            }
        }

        /// <summary>
        /// Открытие вкладки с приложенными файлами
        /// </summary>
        private void OpenAttachments()
        {
            if (Grid.SelectedItem != null)
            {
                var sampleFiles = new SampleFiles();
                sampleFiles.SampleId = Grid.SelectedItem.CheckGet("ID").ToInt();
                sampleFiles.ReturnTabName = ControlName;
                sampleFiles.Show();
            }
        }

        private void Date_TextChanged(object sender, TextChangedEventArgs e)
        {
            //Изменение цвета кнопки Показать на синий
            RefreshButton.Style = (Style)RefreshButton.TryFindResource("FButtonPrimary");
        }

        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            var b = (Button)sender;
            if (b != null)
            {
                var buttonTagList = UIUtil.GetTagList(b);
                foreach (var tag in buttonTagList)
                {
                    ProcessCommand(tag);
                }
            }
        }

        private void SampleStatus_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (Grid.Initialized)
            {
                ProcessCommand("update");
            }
        }

        private void AllEmployeeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            ProcessCommand("refresh");
        }
    }
}
