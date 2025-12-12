using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
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


namespace Client.Interfaces.Service
{
    /// <summary>
    /// Автомобили, которым разрешен доступ для проезда через шлагбаум на СГП
    /// </summary>
    /// <author>eletskikh_ya</author>
    /// <changed>Грешных Н.И.</changed> 
    public partial class CarList : UserControl
    {
        public CarList()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            TabName = "CarList";

            LoadUserGroup();
            InitGrid();

            ProcessPermissions();
        }


        #region Common

        public string RoleName = "[erp]access_transport";

        /// <summary>
        /// Имя вкладки
        /// </summary>
        public string TabName;

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// Список групп, в которые входит пользователь
        /// </summary>
        public List<string> UserGroups { get; set; }

        /// <summary>
        /// право пользователя изменять запись
        /// </summary>
        bool UserChange;

        #endregion


        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="obj">сообщение</param>
        private void ProcessMessages(ItemMessage obj)
        {
            if (obj.ReceiverGroup.IndexOf("Service") > -1)
            {
                if (obj.ReceiverName.IndexOf(TabName) > -1)
                {
                    switch (obj.Action)
                    {
                        case "Refresh":
                            GridCar.LoadItems();
                            break;
                    }
                }
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
        }

        /// <summary>
        /// инициализация таблицы со списком машин
        /// </summary>
        public void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn()
                {
                    Header="Ид",
                    Path="CHCA_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=48,
                    Width2=6,
                },
                new DataGridHelperColumn()
                {
                    Header="Номер машины",
                    Path="NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=180,
                    MaxWidth=250,
                    Width2=20,
                },
                new DataGridHelperColumn()
                {
                    Header="Дата начала",
                    Path="FROM_DT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn()
                {
                    Header="Дата окончания",
                    Path="TO_DT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    
                    Width2=10,
                },
                new DataGridHelperColumn()
                {
                    Header="Примечание",
                    Path="DESCRIPTION",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=80,
                    MaxWidth=150,
                    Width2=32,
                },
                new DataGridHelperColumn()
                {
                    Header="Дата создания",
                    Path="CREATED_DTTM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn()
                {
                    Header="Сотрудник",
                    Path="NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=80,
                    MaxWidth=80,
                    Width2=32,
                },
                new DataGridHelperColumn()
                {
                    Header="",
                    Path="",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Hidden=true,
                },
            };
         
            GridCar.SetColumns(columns);
            
            GridCar.PrimaryKey = "CHCA_ID";
            GridCar.SetSorting("NUM", System.ComponentModel.ListSortDirection.Ascending);
            GridCar.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            GridCar.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };

            //двойной клик на строке откроет форму редактирования
            GridCar.OnDblClick = selectedItem =>
            {
                if (SelectedItem != null && UserChange)
                {
                    if (Central.Navigator.GetRoleLevel(this.RoleName) >= Role.AccessMode.FullAccess)
                    {
                        int сarPermentId = SelectedItem.CheckGet("CHCA_ID").ToInt();
                        Edit(сarPermentId);
                    }
                }
            };
            
            GridCar.Label = "Car";
            GridCar.Init();
            GridCar.AutoUpdateInterval = 60;

            //данные грида
            GridCar.OnLoadItems = LoadItems;
//            GridCar.OnFilterItems = GridFilter;

            GridCar.Run();

            DeleteButton.IsEnabled = false;

            //фокус ввода           
            GridCar.Focus();
        }

        /// <summary>
        /// Загрузка данных из БД в таблицу списка машин
        /// </summary>
        public async void LoadItems()
        {
            GridToolbar.IsEnabled = false;
            GridCar.ShowSplash();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Transport");
            q.Request.SetParam("Object", "Access");
            q.Request.SetParam("Action", "ListCars");
            if (CheckShowAll.IsChecked == true)
                q.Request.SetParam("ALL_CAR", "1");
            else
                q.Request.SetParam("ALL_CAR", "0");

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

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
                    // GridCar.UpdateItems(ListDataSet.Create(result, "ITEMS"));

                    var ds = ListDataSet.Create(result, "ITEMS");
                    GridCar.UpdateItems(ds);

                }
            }

            GridToolbar.IsEnabled = true;
            GridCar.HideSplash();
        }


        /// <summary>
        /// обновление методов работы с выбранной записью в таблице машин
        /// </summary>
        /// <param name="selectedItem">выбранная запись</param>
        public void UpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;
        }

        /// <summary>
        /// Открывает фрейм изменения данных по машине
        /// </summary>
        /// <param name="patternId"></param>
        private void Edit(int сarPermentId)
        {
            var сarPermentForm = new TemporaryCarPass();
            сarPermentForm.ReceiverName = TabName;
            сarPermentForm.Edit(сarPermentId);
        }

        /// <summary>
        /// Удаление доступа
        /// </summary>
        private async void Delete()
        {
            int сarPermentId = SelectedItem.CheckGet("CHCA_ID").ToInt();
            if (сarPermentId > 0)
            {
                var dw = new DialogWindow("Вы действительно хотите удалить машину?", "Удаление машины", "Подтверждение удаления машины от клиента", DialogWindowButtons.NoYes);
                if (dw.ShowDialog() == true)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Transport");
                    q.Request.SetParam("Object", "Access");
                    q.Request.SetParam("Action", "DeleteCars");
                    q.Request.SetParam("CHCA_ID", сarPermentId.ToString());

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            if (result.Count > 0)
                            {
                                // вернулся не пустой ответ, обновим таблицу
                                GridCar.LoadItems();
                            }
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }
        }


        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            GridCar.LoadItems();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Edit(0);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null)
            {
                Edit(SelectedItem.CheckGet("CHCA_ID").ToInt());
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null)
            {
                Delete();
            }
        }

        /// <summary>
        /// Получение списка групп, в которые входит пользователь
        /// </summary>
        private async void LoadUserGroup()
        {
            UserGroups = new List<string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Accounts");
            q.Request.SetParam("Object", "Group");
            q.Request.SetParam("Action", "GroupListByUser");
            q.Request.SetParam("ID", Central.User.EmployeeId.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

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
                    var employeeGroups = ListDataSet.Create(result, "ITEMS");
                    if (employeeGroups.Items.Count > 0)
                    {
                        foreach (var item in employeeGroups.Items)
                        {
                            if (item.CheckGet("WOGR_ID").ToInt() != 1)
                            {
                                string groupCode = item.CheckGet("CODE");
                                if (!string.IsNullOrEmpty(groupCode))
                                {
                                    UserGroups.Add(groupCode);
                                }
                            }
                        }
                    }
                }

                // включение разрешений на действия
                UserChange = UserGroups.Contains("logist") || UserGroups.Contains("programmer");
                /*
                    CreateButton.IsEnabled = UserChange;
                    EditButton.IsEnabled = UserChange;
                    DeleteButton.IsEnabled = UserChange;
              */
              // для отладки  
              UserChange = true;
            }
        }

        private void CheckShowAll_Checked(object sender, RoutedEventArgs e)
        {
            GridCar.LoadItems();
        }

    }
}
