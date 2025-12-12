using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
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

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Список образцов, заказанных конструкторами для тестового изготовления
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleConstructionList : UserControl
    {
        public SampleConstructionList()
        {
            InitializeComponent();
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/samples_new/designer_samples";

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            SetDefaults();
            InitGrid();

            ProcessPermissions();
        }

        #region Common

        public string RoleName = "[erp]sample_drawing";

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        public string DocumentationUrl;

        /// <summary>
        /// Название вкладки
        /// </summary>
        public string TabName;

        /// <summary>
        /// обработка ввода с клавиатуры (роли)
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.F5:
                    Grid.LoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Home:
                    Grid.SetSelectToFirstRow();
                    e.Handled = true;
                    break;

                case Key.End:
                    Grid.SetSelectToLastRow();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "PreproductionSample",
                ReceiverName = "",
                SenderName = TabName,
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            Grid.Destruct();
        }

        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp(DocumentationUrl);
        }

        public void ProcessNavigation()
        {

        }

        #endregion

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
                { "3", "Переданные" },
                { "4", "Полученные" },
            };
            SampleStatus.Items = statusList;
            SampleStatus.SelectedItem = statusList.FirstOrDefault((x) => x.Key == "1");
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="obj">сообщение</param>
        private void ProcessMessages(ItemMessage obj)
        {
            if (obj.ReceiverGroup.IndexOf("PreproductionSample") > -1)
            {
                if (obj.ReceiverName.IndexOf(TabName) > -1)
                {
                    switch (obj.Action)
                    {
                        case "Refresh":
                            Grid.LoadItems();
                            break;

                    }
                }
            }
        }
        
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
                },
                new DataGridHelperColumn
                {
                    Header = "Ид",
                    Path = "ID",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 40,
                    MaxWidth = 40,
                },
                new DataGridHelperColumn
                {
                    Header="Дата заявки",
                    Path="CREATED_DTTM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth=60,
                    MaxWidth=120,
                    Format="dd.MM.yyyy HH:mm:ss",
                },
                new DataGridHelperColumn
                {
                    Header="Дата изготовления",
                    Path="COMPLETED_DTTM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth=60,
                    MaxWidth=120,
                    Format="dd.MM.yyyy HH:mm:ss",
                },
                new DataGridHelperColumn
                {
                    Header = "Исходный образец",
                    Path = "ORIGINAL_DATA",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 60,
                    MaxWidth = 80,
                },
                new DataGridHelperColumn
                {
                    Header="Образец",
                    Path="SAMPLE_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=300,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Картон",
                    Path="CARDBOARD_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=250,
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=70,
                },
                new DataGridHelperColumn
                {
                    Header="Чертеж",
                    Path="DESIGN_FILE_IS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=30,
                    MaxWidth=30,
                },
                new DataGridHelperColumn
                {
                    Header="Чертеж в другом формате",
                    Path="DESIGN_FILE_OTHER_IS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=30,
                    MaxWidth=30,
                },
                new DataGridHelperColumn
                {
                    Header="Конструктор",
                    Path="CONSTRUCTOR_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=30,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Ответственный",
                    Path="RESPONSIBLE_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=30,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header=" ",
                    Path="_",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=5,
                    MaxWidth=1500,
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
                    Header="ID внутреннего чата",
                    Path="CHAT_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            Grid.SetColumns(columns);
            Grid.SearchText = SearchText;

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
                        int currentStatus = row["STATUS_ID"].ToInt();

                        if ((currentStatus == SampleStates.Shipped) || (currentStatus == SampleStates.Received))
                        {
                            color = HColor.Green;
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
                { "SetWork", new DataGridContextMenuItem(){
                    Header="Отправить в работу",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        UpdateStatus(SampleStates.InWork);
                    }
                }},
                { "SetReceived", new DataGridContextMenuItem(){
                    Header="Отметить получение",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        UpdateStatus(SampleStates.Shipped);
                    }
                }},
                { "Files", new DataGridContextMenuItem(){
                    Header="Прикреплённные файлы",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        OpenAttachments();
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
                { "AdvancedValues",
                    new DataGridContextMenuItem()
                    {
                        Header="Дополнительные параметры",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            ShowAdvancedParams();
                        }
                    }
                },
            };

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };

            Grid.Init();

            //данные грида
            Grid.OnLoadItems = LoadItems;
            Grid.OnFilterItems = FilterItems;
            Grid.Run();

            //фокус ввода           
            Grid.Focus();
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        private async void LoadItems()
        {
            GridToolbar.IsEnabled = false;
            Grid.ShowSplash();
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
                q.Request.SetParam("Action", "ListForConstructor");
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
                    }
                }

                GridToolbar.IsEnabled = true;
                Grid.HideSplash();
            }
        }

        private void FilterItems()
        {
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
                                    // Изготовленные
                                    case 2:
                                        if (statusId == 3)
                                        {
                                            includeByStatus = true;
                                        }
                                        break;
                                    // Переданные
                                    case 3:
                                        if (statusId == 7)
                                        {
                                            includeByStatus = true;
                                        }
                                        break;
                                    // Полученные и отгруженные
                                    case 4:
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
            }
        }

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem"></param>
        public void UpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;

            int status = SelectedItem.CheckGet("STATUS_ID").ToInt();
            Grid.Menu["SetWork"].Enabled = false;
            if (status == 0)
            {
                if (SelectedItem.CheckGet("DESIGN_FILE_IS").ToInt() == 1)
                {
                    Grid.Menu["SetWork"].Enabled = true;
                }
            }    
            Grid.Menu["SetReceived"].Enabled = status.ContainsIn(3, 4, 7);

            ProcessPermissions();
        }

        /// <summary>
        /// Обновление статуса образца
        /// </summary>
        /// <param name="newStatus"></param>
        private async void UpdateStatus(int newStatus)
        {
            if (SelectedItem != null)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "Samples");
                q.Request.SetParam("Action", "UpdateStatus");
                q.Request.SetParam("SAMPLE_ID", SelectedItem.CheckGet("ID"));
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
        /// Открытие вкладки с приложенными файлами
        /// </summary>
        private void OpenAttachments()
        {
            if (SelectedItem != null)
            {
                var sampleFiles = new SampleFiles();
                sampleFiles.SampleId = SelectedItem.CheckGet("ID").ToInt();
                sampleFiles.ReturnTabName = TabName;
                sampleFiles.Show();
            }
        }

        /// <summary>
        /// Открытие вкладки с чатом по образцу
        /// </summary>
        private void OpenChat(int chatType = 0)
        {
            if (SelectedItem != null)
            {
                var chatFrame = new SampleChat();
                chatFrame.ChatType = chatType;
                chatFrame.ChatId = SelectedItem.CheckGet("CHAT_ID").ToInt();
                chatFrame.RawMissingFlag = 0;
                chatFrame.ObjectId = SelectedItem.CheckGet("ID").ToInt();
                chatFrame.ReceiverName = TabName;
                chatFrame.Recipient = 23;
                chatFrame.Edit();
            }
        }

        /// <summary>
        /// Открывает форму редактирования образца
        /// </summary>
        /// <param name="id"></param>
        private void SampleEdit(int id)
        {
            var sampleForm = new Sample();
            sampleForm.ReceiverName = TabName;
            sampleForm.Edit(id);
        }

        /// <summary>
        /// Открывает вкладку с дополнительными параметрами образца
        /// </summary>
        private void ShowAdvancedParams()
        {
            if (SelectedItem != null)
            {
                var blankSize = new SampleEditBlankSize();
                blankSize.ReceiverName = TabName;
                blankSize.Edit(SelectedItem);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            SampleEdit(0);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null)
            {
                SampleEdit(SelectedItem.CheckGet("ID").ToInt());
            }
        }

        private void AllSamplesCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void SampleStatus_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null)
            {
                // Меняем статус на отклоненный
                UpdateStatus(SampleStates.Rejected);
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }
    }
}
