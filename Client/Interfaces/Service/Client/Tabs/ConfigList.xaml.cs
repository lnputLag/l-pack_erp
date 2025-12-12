using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Service
{
    /// <summary>
    /// список конфигов
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-02-21</released>
    /// <changed>2023-02-21</changed>
    public partial class ConfigList : UserControl
    {
        public ConfigList()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);
            Loaded += OnLoad;

            Init();
            SetDefaults();
        }

        /// <summary>
        /// форма с полями для фильтрации данных
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ID",
                        Path="HOST_USER_ID",
                        Doc="",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,
                        MaxWidth=250,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Изменение",
                        Path="ON_DATE",
                        Doc="",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,
                        MaxWidth=150,
                    },
                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=5,
                        MaxWidth=2000,
                    },
                };
                Grid.SetColumns(columns);
                Grid.PrimaryKey="HOST_USER_ID";
                Grid.SetSorting("HOST_USER_ID", ListSortDirection.Ascending);
                Grid.SearchText = Search;
                Grid.AutoUpdateInterval=60;
                Grid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                Grid.OnSelectItem = selectedItem =>
                {
                    UpdateActions();
                };

                //двойной клик на строке откроет форму редактирования
                Grid.OnDblClick = selectedItem =>
                {
                    Edit();
                };

                //данные грида
                Grid.OnLoadItems = LoadItems;
                Grid.OnFilterItems = FilterItems;

                 //контекстное меню
                Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "Edit",
                        new DataGridContextMenuItem()
                        {
                            Header="Изменить",
                            Action=()=>
                            {
                                Edit();
                            },
                        }
                    },
                    {
                        "Delete",
                        new DataGridContextMenuItem()
                        {
                            Header="Удалить",
                            Action=()=>
                            {
                                Delete();
                            },
                        }
                    },
                };

                Grid.Run();

                //фокус ввода           
                Grid.Focus();
            }

            //инициализация формы
            {
                Form = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="SEARCH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=Search,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                };

                Form.SetFields(fields);

                //после установки значений
                Form.AfterSet = (Dictionary<string, string> v) =>
                {
                    //фокус на кнопку обновления
                    RefreshButton.Focus();
                };
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
                ReceiverGroup = "Service",
                ReceiverName = "",
                SenderName = "ConfigList",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            Grid.Destruct();
        }


        private void OnLoad(object sender, RoutedEventArgs e)
        {
            var frameName=GetFrameName(); 
            Central.WM.SetActive(frameName);
        }

        public string GetFrameName()
        {
            var result="";
            result=$"configlist_";
            result=result.MakeSafeName();
            return result;
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("ConfigList") > -1)
            {
                switch (m.Action)
                {
                    case "Refresh":
                        Grid.LoadItems();

                        // выделение на новую строку
                        var hostName = m.Message.ToString();
                        Grid.SetSelectedItemId(hostName,"HOST_USER_ID");
                        break;
                }
            }
        }

        /// <summary>
        /// получение записей
        /// </summary>
        public async void LoadItems()
        {
            DisableControls();
            var today=DateTime.Now;

            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Service");
                q.Request.SetParam("Object", "Control");
                q.Request.SetParam("Action", "ListConfig");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts= Central.Parameters.RequestGridAttempts;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            Grid.UpdateItems(ds);
                        }
                    }
                }
            }

            EnableControls();
        }

        /// <summary>
        /// фильтрация записей (аккаунты)
        /// </summary>
        public void FilterItems()
        {
            UpdateActions();
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            Grid.ShowSplash();
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            Grid.HideSplash();
        }

        /// <summary>
        /// обновление методов работы с выбранной в гриде строкой
        /// </summary>
        /// <param name="selectedItem"></param>
        public void UpdateActions()
        {
        }


        /// <summary>
        /// редактирование записи
        /// </summary>
        public void Edit()
        {
            if(Grid.SelectedItem != null)
            {
                var hostUserId = Grid.SelectedItem.CheckGet("HOST_USER_ID").ToString();
                if (!hostUserId.IsNullOrEmpty())
                {
                    var i = new Config();
                    //i.Edit(hostUserId);
                }
            }
        }

        /// <summary>
        /// удаление записи
        /// </summary>
        public void Delete()
        {
            if(Grid.SelectedItem != null)
            {
                var hostUserId = Grid.SelectedItem.CheckGet("HOST_USER_ID").ToString();
                if (!hostUserId.IsNullOrEmpty())
                {
                    var i = new Config();
                    i.Delete(hostUserId);
                }
            }
        }

        /// <summary>
        /// обработка ввода с клавиатуры
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

                case Key.Down:
                    Grid.SetSelectToNextRow();
                    e.Handled = true;
                    break;

                case Key.Up:
                    Grid.SetSelectToPrevRow();
                    e.Handled = true;
                    break;

                case Key.Enter:
                    Edit();
                    e.Handled = true;
                    break;

                case Key.Delete:
                    Delete();
                    e.Handled = true;
                    break;

            }
        }

        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/service/agent/configs");
        }           

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            Create();
        }

      

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Delete();
        }
    }


}
