using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Debug
{
    /// <summary>
    /// список запросов
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2022-08-10</released>
    /// <changed>2022-08-10</changed>
    public partial class QueryList : UserControl
    {
        public QueryList()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            QueryGridInit();
            Init();

            SetDefaults();
        }

        /// <summary>
        /// данные из выбранной в гриде строки
        /// </summary>
        Dictionary<string, string> SelectedQueryItem { get; set; }
        
        /// <summary>
        /// данные из выбранной в гриде строки
        /// </summary>
        Dictionary<string, string> SelectedRoleItem { get; set; }

        /// <summary>
        /// форма с полями для фильтрации данных
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Debug",
                ReceiverName = "",
                SenderName = "QueryList",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            QueryGrid.Destruct();
        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {
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
                    RefreshAccountsButton.Focus();
                };

            }

        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();
        }

       

        /// <summary>
        /// инициализация компонентов (аккаунты)
        /// </summary>
        public void QueryGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=37,
                        MaxWidth=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Module",
                        Path="MODULE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Object",
                        Path="OBJECT",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Action",
                        Path="ACTION",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Старт",
                        Path="DATE_START",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=120,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Завершение",
                        Path="DATE_FINISH",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=120,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Таймаут",
                        Path="TIMEOUT",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Время",
                        Path="TIME",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Общее время",
                        Path="TIME_TOTAL",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="В работе",
                        Path="IN_PROGRESS",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Сервер",
                        Path="SERVER_IP",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Попыток",
                        Path="ATTEMPTS",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Код",
                        Path="CODE",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отчет",
                        Path="REPORT",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Журнал",
                        Path="LOG",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,
                    },
                   
                };
                QueryGrid.SetColumns(columns);

            }

            // Раскраска строк
            QueryGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
            };

            QueryGrid.SetSorting("DATE_START", ListSortDirection.Descending);
            QueryGrid.SearchText = Search;
            QueryGrid.AutoUpdateInterval=1;
            QueryGrid.Init();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            QueryGrid.OnSelectItem = selectedItem =>
            {
                QueryGridUpdateActions(selectedItem);
            };

            //двойной клик на строке откроет форму редактирования
            QueryGrid.OnDblClick = selectedItem =>
            {
            };

            //данные грида
            QueryGrid.OnLoadItems = QueryGridLoadItems;
            //QueryGrid.OnFilterItems = QueryGridFilterItems;

            //контекстное меню
            QueryGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
            };

            QueryGrid.Run();

            //фокус ввода           
            QueryGrid.Focus();
        }

        /// <summary>
        /// получение записей (аккаунты)
        /// </summary>
        public async void QueryGridLoadItems()
        {
            //QueryGridDisableControls();

            bool resume = true;

            if (resume)
            {

                var list=new List<Dictionary<string,string>>();
                if(Central.Parameters.UseRequestLog)
                {
                    if(Central.Queries != null)
                    {
                        var items = new Dictionary<string, LPackClientQuery>(Central.Queries);
                        foreach(KeyValuePair<string, LPackClientQuery> item in items)
                        {
                            var q = item.Value;
                            var row = q.GetDict();
                            list.Add(row);
                        }
                    }
                }
                

                var ds=ListDataSet.Create(list);
                QueryGrid.UpdateItems(ds);
                //QueryGrid.SetSelectToFirstRow();
            }

            //QueryGridEnableControls();
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void QueryGridDisableControls()
        {
            QueryGridToolbar.IsEnabled = false;
            QueryGrid.ShowSplash();
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void QueryGridEnableControls()
        {
            QueryGridToolbar.IsEnabled = true;
            QueryGrid.HideSplash();
        }

        /// <summary>
        /// фильтрация записей (аккаунты)
        /// </summary>
        public void QueryGridFilterItems()
        {
            QueryGridUpdateActions(null);

            if (QueryGrid.GridItems != null)
            {
                if (QueryGrid.GridItems.Count > 0)
                {
                    //фильтрация строк
                }
            }
        }

        /// <summary>
        /// обновление методов работы с выбранной в гриде строкой
        /// </summary>
        public void QueryGridUpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedQueryItem = selectedItem;
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
        }

        /// <summary>
        /// обработка ввода с клавиатуры (роли)
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.F5:
                    QueryGrid.LoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Home:
                    QueryGrid.SetSelectToFirstRow();
                    e.Handled = true;
                    break;

                case Key.End:
                    QueryGrid.SetSelectToLastRow();
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
            Central.ShowHelp("/doc/l-pack-erp/service/accounts/accounts");
        }

        private void RefreshAccounts()
        {
            QueryGrid.LoadItems();
        }
        
        private void ShowAllAccounts()
        {
            QueryGrid.UpdateItems();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void RefreshAccountsButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshAccounts();
        }

        private void ShowAll_Click(object sender, RoutedEventArgs e)
        {
            ShowAllAccounts();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if(Central.Queries!=null)
            {
                Central.Queries.Clear();
            }
        }
    }
}
