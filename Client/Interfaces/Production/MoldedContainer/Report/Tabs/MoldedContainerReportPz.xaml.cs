using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.MoldedContainer
{
    /// <summary>
    /// Отчет по ПЗ
    /// </summary>
    /// <author>greshnyh_ni</author>
    public partial class MoldedContainerReportPz : UserControl
    {
        public MoldedContainerReportPz()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            FormInit();
            SetDefault();
            GridInit();

            ProcessPermissions();
        }

        public string RoleName = "[erp]molded_contnr_productn_repo";

        /// <summary>
        /// форма с полями для фильтрации данных
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        // инициализация компонентов формы
        /// </summary>
        public void FormInit()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="FROM_DATE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=FromDate,
                    Default=DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy"),
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TO_DATE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ToDate,
                    Default=DateTime.Now.ToString("dd.MM.yyyy"),
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SEARCH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Search,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            Form.SetFields(fields);
            Form.SetDefaults();
        }

        /// <summary>
        // инициализация компонентов таблицы
        /// </summary>
        public void GridInit()
        {
            //инициализация грида
            {
                //колонки грида

                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД ПЗ",
                        Path="TASK_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth =10,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер",
                        Path="TASK_NUMBER",
                        Description="",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth =12,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Path="TASK_STATUS_TITLE",
                        Description="",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth =12,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата начала",
                        Path="TASK_START",
                        ColumnType=ColumnTypeRef.DateTime,
                        MinWidth =14,
                        Width2=14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата завершения",
                        Path="TASK_COMPLETED",
                        ColumnType=ColumnTypeRef.DateTime,
                        MinWidth =14,
                        Width2=14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Станок",
                        Path="NAME_MACHINE",
                        Description="",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth =20,
                        Width2=20,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Path="GOODS_CODE",
                        Description="",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth =12,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Изделие",
                        Path="GOODS_NAME",
                        Description="",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth =45,
                        Width2=45,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество, шт",
                        Path="TASK_QUANTITY",
                        Description="",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth =14,
                        Width2=14,
                        Totals =(List<Dictionary<string,string>> rows) =>
                        {
                            int result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    result += row.CheckGet("TASK_QUANTITY").ToInt();
                                }
                            }
                            return string.Format("{0:### ### ###}", result);
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Оприходовано, шт",
                        Path="PRIHOD_QTY",
                        Description="",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth =14,
                        Width2=14,
                        Totals =(List<Dictionary<string,string>> rows) =>
                        {
                            int result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    result += row.CheckGet("PRIHOD_QTY").ToInt();
                                }
                            }
                            return string.Format("{0:### ### ###}", result);
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Списано, шт",
                        Path="RASHOD_QTY",
                        Description="",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth =14,
                        Width2=14,
                        Totals =(List<Dictionary<string,string>> rows) =>
                        {
                            int result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    result += row.CheckGet("RASHOD_QTY").ToInt();
                                }
                            }
                            return string.Format("{0:### ### ###}", result);
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отгрузка",
                        Path="ORDER_TITLE",
                        Description="",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth =30,
                        Width2=30,
                    },
                };
                Grid.SetColumns(columns);
                Grid.SetPrimaryKey("TASK_ID");
                //Grid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
                Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                Grid.AutoUpdateInterval = 0;
                Grid.SearchText = Search;
                Grid.Toolbar = GridToolbar;
                Grid.OnLoadItems = GridLoadItems;
                Grid.Init();
            }

            //инициализация формы
            {
                Form = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>();

                Form.SetFields(fields);
            }
        }

        /// <summary>
        /// получение данных по потреблению электричества
        /// </summary>
        public async void GridLoadItems()
        {
            GridDisableControls();

            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("FROM_DT", FromDate.Text);
                    p.Add("TO_DT", ToDate.Text);
                    p.Add("MACHINE_ID", Machines.SelectedItem.Key);
                    
                }
                
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "MoldedContainer");
                q.Request.SetParam("Object", "Report");
                q.Request.SetParam("Action", "TaskList");
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

                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            Grid.UpdateItems(ds);
                        }                        
                    }
                }
            }
            ShowButton.Style = (Style)ShowButton.TryFindResource("Button");
            GridEnableControls();
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void GridDisableControls()
        {
            GridToolbar.IsEnabled = false;
            Grid.ShowSplash();
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void GridEnableControls()
        {
            GridToolbar.IsEnabled = true;
            Grid.HideSplash();
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
        /// Обработчик нажатия на кнопку обновления формы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefault()
        {
            {
                var list = new Dictionary<string, string>();
                list.Add("1", "Все");
                list.Add("301", "ВФМ BST[ЕС9600]-2 A");
                list.Add("302", "ВФМ BST[ЕС9600]-2 B");
                list.Add("303", "ВФМ BST[ЕС9600]-1 A");
                list.Add("304", "ВФМ BST[ЕС9600]-1 B");
                list.Add("311", "Принтер BST[СPH-3430]-1");
                list.Add("321", "Этикетир. BST[TBH-2438]-1");
                list.Add("312", "Принтер AAEI [301 P]");
                list.Add("322", "Этикетир AAEI [301 L]");

                Machines.Items = list;
                Machines.SetSelectedItemByKey("1");
            }
            Form.SetDefaults();
        }

        /// <summary>
        /// Обработчик изменения параметра фильтрафции
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void Types_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
            Grid.UpdateItems();
        }

        /// <summary>
        /// Обработчик изменения начальной даты отчета
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void ToDateTextChanged(object sender, TextChangedEventArgs args)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
            Grid.UpdateItems();
        }

        /// <summary>
        /// Обработчик изменения конечной даты отчета
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void FromDateTextChanged(object sender, TextChangedEventArgs args)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
            Grid.UpdateItems();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку экспорта в Excel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {
            Grid.ItemsExportExcel();
        }

        /// <summary>
        /// Экспорт данных из таблицы в Excel
        /// </summary>
        public async void ExportToExcel(List<DataGridHelperColumn> columns, List<Dictionary<string, string>> items)
        {
            var eg = new ExcelGrid();
            eg.SetColumnsFromGrid(columns);
            eg.Items = items;
            await Task.Run(() =>
            {
                eg.Make();
            });

        }

        /// <summary>
        /// Обработчик нажатия на кнопку справки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/production/MoldedContainer_report");
        }

        /// <summary>
        /// Закрытие окна
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "MoldedContainer",
                ReceiverName = "",
                SenderName = "MoldedContainerReport",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            Grid.Destruct();
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
                    Grid.LoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

               
            }
        }

        private void Machines_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
            Grid.UpdateItems();
        }
    }
}
