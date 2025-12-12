using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
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
    /// Данные по съёмам для текущего стекера за последние сутки
    /// </summary>
    public partial class StackerDropList : UserControl
    {
        public StackerDropList()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            FrameName = "StackerDropList";

            InitForm();
            SetDefaults();
            InitGrid();
            
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }


        public string FrameName { get; set; }

        /// <summary>
        /// Датасет с данными по съёмам на этом стекере
        /// </summary>
        public ListDataSet StackerDropDataSet { get; set; }

        public void InitForm()
        {
            //инициализация формы
            {
                Form = new FormHelper();

                //список колонок формы
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path = "DT_FROM",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = FromDate,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "DT_TO",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = ToDate,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                };

                Form.SetFields(fields);
                Form.ToolbarControl = StackerDropListToolbar;
            }
        }

        public void InitGrid()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="PCSD_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата съёма",
                        Path="DTTM",
                        ColumnType=ColumnTypeRef.String,
                        Width = 120,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид стекера",
                        Path="PRCA_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД ПЗ",
                        Path="ID_PZ",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД продукции",
                        Path="ID2",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер съёма",
                        Path="DROP_CNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 55,
                    },
                    
                    new DataGridHelperColumn
                    {
                        Header="Cтоп в съёме",
                        Doc="Количество стоп в съёме",
                        Path="DROP_STACK_COUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=110,
                    },
                    new DataGridHelperColumn
                    {
                        Header="По умолчанию в стопе съёма",
                        Path="DEFAULT_DROP_STACK_QTY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=130,
                    },
                    new DataGridHelperColumn
                    {
                        Header="В стопе съёма",
                        Path="DROP_STACK_QTY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=130,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = HColor.Yellow;

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="В съёме",
                        Path="DROP_ITEM_QTY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 130,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = HColor.Yellow;

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Съём",
                        Path="DROP_STACKING",
                        ColumnType=ColumnTypeRef.String,
                        Width = 130,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Стоп по текущей укладке",
                        Path="STACKING_QTY_REAM",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 130,
                    },
                    new DataGridHelperColumn
                    {
                        Header="В стопе по умолчанию",
                        Path="STACKING_QTY_IN_REAM",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=130,
                    },
                    new DataGridHelperColumn
                    {
                        Header="В стопе по текущей укладке",
                        Path="STACKING_FACT_QTY_IN_REAM",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width=130,
                    },
                    new DataGridHelperColumn
                    {
                        Header="На поддоне по текущей укладке",
                        Path="STACKING_QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=130,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Текущая укладка",
                        Path="STACKING",
                        ColumnType=ColumnTypeRef.String,
                        Width = 130,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Стоп из съёма для одного поддона по текущей укладке",
                        Path="DROP_STACK_COUNT_ON_PALLET",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=130,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Поддонов из стоп съёма",
                        Path="DROP_PALLET_COUNT",
                        ColumnType=ColumnTypeRef.Double,
                        Width=130,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Поддон из съёма",
                        Path="STACKING_ON_PALLET_BY_DROP",
                        ColumnType=ColumnTypeRef.String,
                        Width = 130,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Дата обработки",
                        Path="PROCESSED_DTTM",
                        ColumnType=ColumnTypeRef.String,
                        Width = 120,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ручной сброс",
                        Path="DROP_MANUALLY_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width=85,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Обработано",
                        Path="PROCESSED_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width=85,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Валидные данные",
                        Path="VERIFIED_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Станок",
                        Path="ID_ST",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид станка",
                        Path="ID_ST",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Стекер",
                        Path="STACKER_NUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=55,
                    },
                    
                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=2,
                        MaxWidth=2000,
                    },
                };
                StackerDropListGrid.SetColumns(columns);
                StackerDropListGrid.SearchText = SearchText;

                StackerDropListGrid.OnLoadItems = GetStackerData;

                StackerDropListGrid.OnFilterItems = () =>
                {
                    if (StackerDropListGrid.GridItems != null && StackerDropListGrid.GridItems.Count > 0)
                    {
                        if (CorrugatorMachineSelectBox.SelectedItem.Key != null)
                        {
                            var items = new List<Dictionary<string, string>>();
                            switch (CorrugatorMachineSelectBox.SelectedItem.Key)
                            {
                                case "-1":
                                    items = StackerDropListGrid.GridItems;
                                    break;

                                default:
                                    items.AddRange(StackerDropListGrid.GridItems.Where(row => row.CheckGet("ID_ST").ToInt() == CorrugatorMachineSelectBox.SelectedItem.Key.ToInt()));
                                    break;
                            }

                            StackerDropListGrid.GridItems = items;
                        }

                        if (StackerSelectBox.SelectedItem.Key != null)
                        {
                            var items = new List<Dictionary<string, string>>();
                            switch (StackerSelectBox.SelectedItem.Key)
                            {
                                case "-1":
                                    items = StackerDropListGrid.GridItems;
                                    break;

                                default:
                                    items.AddRange(StackerDropListGrid.GridItems.Where(row => row.CheckGet("STACKER_NUMBER").ToInt() == StackerSelectBox.SelectedItem.Key.ToInt()));
                                    break;
                            }

                            StackerDropListGrid.GridItems = items;
                        }
                    }
                };

                StackerDropListGrid.Init();
                StackerDropListGrid.Run();
            }
        }

        /// <summary>
        /// Получение данных из новой таблицы prod_corrgtr_stacker_data
        /// </summary>
        public void GetStackerData()
        {
            StackerDropListGrid.ShowSplash();
            StackerDropListToolbar.IsEnabled = false;

            bool resume = true;
            var f = Form.GetValueByPath("DT_FROM").ToDateTime();
            var t = Form.GetValueByPath("DT_TO").ToDateTime();
            if (DateTime.Compare(f, t) > 0)
            {
                const string msg = "Дата начала должна быть меньше даты окончания.";
                var d = new DialogWindow($"{msg}", "Проверка данных");
                d.ShowDialog();
                resume = false;
            }

            if (resume)
            {
                StackerDropListGrid?.ClearItems();

                var p = new Dictionary<string, string>();
                p.Add("DT_FROM", Form.GetValueByPath("DT_FROM"));
                p.Add("DT_TO", Form.GetValueByPath("DT_TO"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Stacker");
                q.Request.SetParam("Action", "ListStackerData");
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            StackerDropDataSet = ds;
                            StackerDropListGrid.UpdateItems(StackerDropDataSet);
                        }
                    }
                }
            }

            StackerDropListToolbar.IsEnabled = true;
            StackerDropListGrid.HideSplash();
        }

        public void SetDefaults()
        {
            Form.SetValueByPath("DT_FROM", DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy"));
            Form.SetValueByPath("DT_TO", DateTime.Now.ToString("dd.MM.yyyy"));

            StackerDropDataSet = new ListDataSet();

            {
                var list = new Dictionary<string, string>();
                list.Add("-1", "Все ГА");
                list.Add("2", "ГА1");
                list.Add("21", "ГА2");
                list.Add("22", "ГА3");
                list.Add("23", "ГА1 КШ");
                CorrugatorMachineSelectBox.Items = list;
                CorrugatorMachineSelectBox.SelectedItem = list.FirstOrDefault((x) => x.Key == "-1");
            }

            {
                var list = new Dictionary<string, string>();
                list.Add("-1", "Все стекеры");
                list.Add("1", "1 (нижний)");
                list.Add("2", "2 (верхний)");
                StackerSelectBox.Items = list;
                StackerSelectBox.SelectedItem = list.FirstOrDefault((x) => x.Key == "-1");
            }
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.Close(FrameName);

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
        }

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "StackerCM",
                ReceiverName = "",
                SenderName = "StackerDropList",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;

            switch (e.Key)
            {
                case Key.F5:
                    GetStackerData();
                    e.Handled = true;
                    break;
            }

            return;
        }


        private void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/production/stacker_cm/stacker_drop_list");
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            GetStackerData();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void CorrugatorMachineSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            StackerDropListGrid.UpdateItems();
        }

        private void StackerSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            StackerDropListGrid.UpdateItems();
        }
    }
}