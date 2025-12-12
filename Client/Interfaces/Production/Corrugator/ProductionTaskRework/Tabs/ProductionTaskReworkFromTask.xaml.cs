using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Раскрой по ПЗ. Создание нового задания с позицией из выполненного задания
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class ProductionTaskReworkFromTask : UserControl
    {
        /// <summary>
        /// Создание нового задания с позицией из выполненного задания
        /// </summary>
        public ProductionTaskReworkFromTask()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Formats = new Dictionary<string, string>();
            ReworkReasonDS = new ListDataSet();
            ReworkReasonDS.Init();

            FactoryId = 1;
            MainMode = true;

            ProcessPermissions();
        }

        public string TabName;

        public string RoleName = "[erp]production_task_cm_rework";

        /// <summary>
        /// процессор формы
        /// </summary>
        private FormHelper Form { get; set; }

        /// <summary>
        /// данные из выбранной в гриде заданий строки
        /// </summary>
        Dictionary<string, string> SelectedTaskItem { get; set; }

        /// <summary>
        /// данные из выбранной в гриде позиций строки
        /// </summary>
        Dictionary<string, string> SelectedPositionItem { get; set; }

        /// <summary>
        /// форматы для раскроя
        /// массив вида:
        /// FORMAT2100 1
        /// при изменении значения чекбокса в блоке выбора форматов ставится значение в
        /// соотв. элемент массива, при отправке запроса на раскрой данные 
        /// по форматам берутся из этого массива
        /// </summary>
        private Dictionary<string, string> Formats { get; set; }

        /// <summary>
        /// Производственное задание, полученное после раскроя
        /// </summary>
        private Dictionary<string, string> ReworkTask { get; set; }
        /// <summary>
        /// Содержимое выпадающего списка причин раскроя
        /// </summary>
        private ListDataSet ReworkReasonDS { get; set; }

        private Dictionary<string, string> ReworkPosition { get; set; }

        /// <summary>
        /// Словарь с отгруженными заданиями
        /// </summary>
        Dictionary<string, string> ShippedTask { get; set; }

        /// <summary>
        /// Ид исходного производственного задания
        /// </summary>
        private int PrimaryIdPz;

        private int ReworkBlankQty;

        /// <summary>
        /// Если форма открывается для выбора позиции перевыпуска, то содержит имя вкладки вызова
        /// </summary>
        public string BackTabName;
        /// <summary>
        /// Режим работы основной вкладки. false - дочерняя вкладка другого интерфейса 
        /// </summary>
        public bool MainMode;
        /// <summary>
        /// Идентификатор производственной площадки, на которой выполняется ПЗГА
        /// </summary>
        public int FactoryId;

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
        /// обработчик сообщений
        /// </summary>
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
                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Escape:
                    CloseTab();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о фрейма
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "ProductionTask",
                ReceiverName = "",
                SenderName = "TaskRework",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        public void Initialize()
        {
            LoadRef();

            InitTaskGrid();
            InitPosisionGrid();
            InitForm();
            SetDefaults();
        }

        /// <summary>
        /// загрузка вспомогательных данных для построения интерфейса
        /// </summary>
        public async void LoadRef()
        {
            GridToolbar.IsEnabled = false;
            PositionGrid.ShowSplash();
            bool resume = true;

            // Загрузка списка форматов
            if (resume)
            {
                var p = new Dictionary<string, string>()
                {
                    { "FACTORY_ID", FactoryId.ToString() }
                };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Cutter");
                q.Request.SetParam("Action", "GetSources");
                q.Request.SetParam("FACTORY_ID", FactoryId.ToString());

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "FORMATS");
                        if (ds.Items.Count > 0)
                        {
                            var list = new Dictionary<string, string>();
                            list.AddRange<string, string>(ds.GetItemsList("PAWI_ID", "WIDTH"));

                            if (list.Count > 0)
                            {
                                Formats = new Dictionary<string, string>();
                                FormatContainer.Children.Clear();

                                foreach (KeyValuePair<string, string> i in list)
                                {
                                    var k = i.Value.ToString();

                                    //Проверяем форматы. В Кашире нет 2700 и 2800
                                    if ((k.ToInt() > 2500) && (FactoryId == 2))
                                    {
                                        continue;
                                    }
                                    var v = "1";

                                    Formats.CheckAdd(k, v);

                                    var checkBox = new CheckBox();
                                    {
                                        checkBox.Name = $"Format_{k}";
                                        checkBox.Content = $"{i.Value}";
                                        checkBox.AddHandler(
                                            CheckBox.ClickEvent,
                                            new RoutedEventHandler((o, e) =>
                                            {
                                                if (o != null)
                                                {
                                                    var el = (CheckBox)o;
                                                    if (el != null)
                                                    {
                                                        var k = el.Name;
                                                        k = k.Replace("Format_", "");
                                                        var v = el.IsChecked.ToInt().ToString();
                                                        Formats.CheckAdd(k, v);
                                                    }
                                                }
                                            })
                                        );
                                        checkBox.Style = (Style)FormatContainer.TryFindResource("CheckBoxTopPanel");
                                        if (v.ToInt() == 1)
                                        {
                                            checkBox.IsChecked = true;
                                        }
                                        else
                                        {
                                            checkBox.IsChecked = false;
                                        }
                                        FormatContainer.Children.Add(checkBox);
                                    }

                                }
                            }
                        }

                    }
                }
            }

            // Загрузка причин повторного выпуска
            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ProductionTask");
                q.Request.SetParam("Action", "ReworkReasonRef");

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        ReworkReasonDS = ListDataSet.Create(result, "ITEMS");
                        ReworkReason.Items = ReworkReasonDS.GetItemsList("ID", "REASON");
                    }
                }
            }

            GridToolbar.IsEnabled = true;
            PositionGrid.HideSplash();
        }

        public void InitTaskGrid()
        {
            var columns = new List<DataGridHelperColumn>()
            {
                new DataGridHelperColumn()
                {
                    Header="Время Начала",
                    Path="TASK_START",
                    ColumnType=ColumnTypeRef.DateTime,
                    Format="dd.MM.yyyy HH:mm",
                    MinWidth=100,
                    MaxWidth=100,
                },
                new DataGridHelperColumn()
                {
                    Header="ИД ПЗ",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=60,
                    MaxWidth=60,
                },
                new DataGridHelperColumn()
                {
                    Header="Номер ПЗ",
                    Path="NUM",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=75,
                    MaxWidth=90,
                },
                new DataGridHelperColumn
                {
                    Header = "Артикул",
                    Path = "ARTICLE",
                    ColumnType = ColumnTypeRef.String,
                    MinWidth=150,
                    MaxWidth=300,
                },
                new DataGridHelperColumn()
                {
                    Header="ГА",
                    Path="MACHINE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=75,
                    MaxWidth=90,
                },
                new DataGridHelperColumn()
                {
                    Header="Длина, м",
                    Path="LEN",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=75,
                },
                new DataGridHelperColumn()
                {
                    Header="Профиль",
                    Path="PROFIL_NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=40,
                    MaxWidth=70,
                },
                new DataGridHelperColumn()
                {
                    Header="Формат",
                    Path="WIDTH",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=65,
                },
                new DataGridHelperColumn()
                {
                    Header=" ",
                    Path="_",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=5,
                    MaxWidth=1500,
                },
                new DataGridHelperColumn()
                {
                    Header="Выполнено",
                    Path="POSTING",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="Отгружено",
                    Path="AT_SHIPMENT",
                    ColumnType=ColumnTypeRef.String,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="Артикулы",
                    Path="ARTICLE",
                    ColumnType=ColumnTypeRef.String,
                    Hidden=true,
                },
            };
            TaskGrid.SetColumns(columns);

            TaskGrid.SetSorting("ROWNNMBER", ListSortDirection.Ascending);
            TaskGrid.SearchText = SearchText;
            TaskGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                // Цвета фона строк
                {
                    StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";
                        if (row["POSTING"].ToInt() == 0)
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
            };
            TaskGrid.Init();

            //данные грида
            TaskGrid.OnLoadItems = TaskLoadItems;
            TaskGrid.OnFilterItems = FilterTaskItems;
            TaskGrid.Run();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            TaskGrid.OnSelectItem = (Dictionary<string, string> selectedItem) =>
            {
                if (selectedItem.Count > 0)
                {
                    TaskGridUpdateActions(selectedItem);
                }
            };
        }

        public void InitPosisionGrid()
        {
            var columns = new List<DataGridHelperColumn>()
            {
                new DataGridHelperColumn()
                {
                    Header="Стекер",
                    Path="STACKER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=60,
                },
                new DataGridHelperColumn()
                {
                    Header="Картон",
                    Path="CARDBOARD_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=80,
                    MaxWidth=200,
                },
                new DataGridHelperColumn()
                {
                    Header="Заявка",
                    Path="POSITION_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="Дата и время отгрузки",
                    Path="SHIPMENT_DATE_TIME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format="dd.MM.yyyy HH:mm",
                    MinWidth=100,
                    MaxWidth=100,
                },
                new DataGridHelperColumn()
                {
                    Header="Изделие",
                    Path="GOODS_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="Изделие",
                    Path="GOODS_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=120,
                    MaxWidth=300,
                },
                new DataGridHelperColumn()
                {
                    Header="Заготовка",
                    Path="BLANK_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="Заготовка",
                    Path="BLANK_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=120,
                    MaxWidth=300,
                },
                new DataGridHelperColumn
                {
                    Header="В задании",
                    Doc="Количество изделий в задании",
                    Path="TASK_QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="В заявке",
                    Doc="Количество изделий в заявке",
                    Path="ORDER_QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="ИД картона",
                    Path="CARDBOARD_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header=" ",
                    Path="_",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=5,
                    MaxWidth=1500,
                },
                new DataGridHelperColumn
                {
                    Header="Доступность отгрузки",
                    Path="AVAILABLE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            PositionGrid.SetColumns(columns);

            PositionGrid.SetSorting("STACKER", ListSortDirection.Ascending);
            // Раскраска строк
            PositionGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                {
                    DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";
                        // Отгрузка недоступна, перевыгон делать нельзя
                        if (row.CheckGet("AVAILABLE").ToInt() == 0)
                        {
                            color=HColor.GrayFG;
                        }
                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };

            PositionGrid.Init();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            PositionGrid.OnSelectItem = (Dictionary<string, string> selectedItem) =>
            {
                if (selectedItem.Count > 0)
                {
                    PositionGridUpdateActions(selectedItem);
                }
            };
        }

        /// <summary>
        /// Инициализация полей формы
        /// </summary>
        public void InitForm()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="REWORK_REASON",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ReworkReason,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Comments,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CARDBOARD_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CardboardName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CARDBOARD_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CardboardId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QID_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Quality,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CUTTING_WIDTH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CuttingWidth,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TRIM_PERCENTAGE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TrimPercentage,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CUTTING_LENGTH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CuttingLength,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                //стекер 1                
                new FormHelperField()
                {
                    Path="STACKER1_BLANK_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker1BlankId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER1_GOOD_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker1GoodId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER1_BLANK",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Stacker1Blank,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER1_SHIPMENT_DATE_TIME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Stacker1Shipment,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER1_CREASE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Stacker1Crease,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER1_CREASE_NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Stacker1CreaseNote,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER1_LENGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker1Length,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER1_WIDTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker1Width,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER1_QUANTITY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker1Quantity,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER1_PRODUCTS_FROM_BLANK",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker1ProductsFromBlank,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER1_PALLET",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker1StacksInPallet,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER1_QUANTITY_CALCULATED",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker1QuantityCalculated,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER1_THREADS",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker1Threads,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER1_POSITION_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker1PositionId,
                    Default="0",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },


                //стекер 2                
                new FormHelperField()
                {
                    Path="STACKER2_BLANK_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker2BlankId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER2_GOOD_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker2GoodId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER2_BLANK",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Stacker2Blank,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER2_SHIPMENT_DATE_TIME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Stacker2Shipment,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER2_CREASE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Stacker2Crease,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER2_CREASE_NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Stacker2CreaseNote,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="STACKER2_LENGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker2Length,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER2_WIDTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker2Width,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER2_QUANTITY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker2Quantity,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER2_PRODUCTS_FROM_BLANK",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker2ProductsFromBlank,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER2_PALLET",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker2StacksInPallet,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER2_QUANTITY_CALCULATED",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker2QuantityCalculated,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER2_THREADS",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker2Threads,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER2_POSITION_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker2PositionId,
                    Default="0",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                // состав картона
                new FormHelperField()
                {
                    Path="LAYER_1_RAWGROUP",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Layer1Paper,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="LAYER_2_RAWGROUP",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Layer2Paper,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="LAYER_3_RAWGROUP",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Layer3Paper,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="LAYER_4_RAWGROUP",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Layer4Paper,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="LAYER_5_RAWGROUP",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Layer5Paper,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };
            Form.SetFields(fields);
        }

        /// <summary>
        /// Заполнение значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            Form.SetDefaults();
            ErrorMessage.Text = "";
            MakeCutButton.IsEnabled = false;
            SaveTaskButton.IsEnabled = false;
            Stacker1QuantityCalculated.IsReadOnly = true;
            Stacker2QuantityCalculated.IsReadOnly = true;
            ReworkTask = new Dictionary<string, string>();
            ReworkPosition = new Dictionary<string, string>();
            ShippedTask = new Dictionary<string, string>();
            PrimaryIdPz = 0;
            ReworkBlankQty = 0;
        }

        /// <summary>
        /// установка цвета бордера контрола
        /// type:
        ///     0 -- нормальное сосотояние, серый цвет
        ///     1 -- ошибка, акцент внимания, красный цвет
        ///     2 -- акцент внимания, синий цвет
        /// </summary>
        private void SetControlBorder(Control control, int type = 0)
        {
            var color = "#ffcccccc";

            switch (type)
            {
                //red
                case 1:
                    color = "#FFFF0000";
                    break;

                //blue
                case 2:
                    color = "#FF0055FF";
                    break;

                default:
                    color = "#FFCCCCCC";
                    break;
            }

            if (control != null)
            {
                var bc = new BrushConverter();
                var brush = (Brush)bc.ConvertFrom(color);
                control.BorderBrush = brush;
            }
        }

        /// <summary>
        /// Загрузка данных в таблицу заданий
        /// </summary>
        public async void TaskLoadItems()
        {
            GridToolbar.IsEnabled = false;
            TaskGrid.ShowSplash();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "ProductionTask");
            q.Request.SetParam("Action", "ListCompleteShort");
            // количество дней, за которые нам нужен список выполненных заданий
            q.Request.SetParam("DAYS", "14");
            q.Request.SetParam("FACTORY_ID", FactoryId.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

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
                    var processedDs = ProcessItems(ds);
                    TaskGrid.UpdateItems(processedDs);
                }
            }

            GridToolbar.IsEnabled = true;
            TaskGrid.HideSplash();
        }

        /// <summary>
        /// Обработка данных перед загрузкой в таблицу
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        private ListDataSet ProcessItems(ListDataSet ds)
        {
            ListDataSet _ds = new ListDataSet();
            _ds.Init();

            if (ds.Items != null)
            {
                if (ds.Items.Count > 0)
                {
                    foreach (var row in ds.Items)
                    {
                        // Осталвляем только те записи, где в поле AT_SHIPMENT присутствует A
                        // Если только S, то запоминаем номера заданий для последующего информирования
                        string atShipment = row.CheckGet("AT_SHIPMENT");
                        if (atShipment == "S" || atShipment == "S,S")
                        {
                            string num = row.CheckGet("NUM").Substring(0, 4);
                            ShippedTask.CheckAdd(num, row.CheckGet("NUM"));
                        }
                        else
                        {
                            _ds.Items.Add(row);
                        }
                    }

                }
            }

            return _ds;
        }

        /// <summary>
        /// Фильтрация строк таблицы заданий
        /// </summary>
        private void FilterTaskItems()
        {
            if (TaskGrid.GridItems != null)
            {
                // Номер для поиска берем из строки поиска
                string num = SearchText.Text;
                if (TaskGrid.GridItems.Count > 0)
                {
                    var items = new List<Dictionary<string, string>>();

                    foreach (Dictionary<string, string> row in TaskGrid.GridItems)
                    {
                        bool includeNum = true;
                        bool includeArticle = true;

                        if (num.Length > 1)
                        {
                            // Оставляем только строки с найденным номером задания
                            includeNum = false;
                            if (row.CheckGet("NUM").Contains(num))
                            {
                                includeNum = true;
                            }
                        }

                        if (num.Length > 2)
                        {
                            includeArticle = false;
                            if (row.CheckGet("ARTICLE").Contains(num))
                            {
                                includeArticle = true;
                            }
                        }

                        if (includeNum || includeArticle)
                        {
                            items.Add(row);
                        }
                    }

                    TaskGrid.GridItems = items;
                }
                else
                {
                    ErrorMessage.Text = "";
                    if (num.Length == 4)
                    {
                        // Если нашли в словаре отгруженных заданий номер из строки поиска, выводим сообщение
                        if (ShippedTask.ContainsKey(num))
                        {
                            ErrorMessage.Text = $"Задание {ShippedTask[num]} полностью отгружено. Перевыгон невозможен";
                        }
                    }

                    // В таблице заданий ничего нет, очищаем зависимые объекты
                    PositionGrid.ClearItems();
                    ClearPosition(1);
                    ClearPosition(2);
                }
            }
        }

        /// <summary>
        /// обновление методов работы с выбранной в таблице заданий строкой
        /// </summary>
        /// <param name="selectedItem"></param>
        private void TaskGridUpdateActions(Dictionary<string, string> selectedItem)
        {
            if (selectedItem != null)
            {
                ErrorMessage.Text = "";
                if (SelectedTaskItem != selectedItem)
                {
                    ClearPosition(1);
                    ClearPosition(2);

                    SelectedTaskItem = selectedItem;
                    PositionLoadItems();
                }
            }
        }

        /// <summary>
        /// обновление методов работы с выбранной в таблице заготовок строкой
        /// </summary>
        /// <param name="selectedItem"></param>
        private void PositionGridUpdateActions(Dictionary<string, string> selectedItem)
        {
            if (selectedItem != null)
            {
                if (SelectedPositionItem != selectedItem)
                {
                    SelectedPositionItem = selectedItem;

                    // Выбираем позицию только если для неё доступна отгрузка
                    bool available = selectedItem.CheckGet("AVAILABLE").ToBool();

                    if (available)
                    {
                        ErrorMessage.Text = "";
                        SetPosition(selectedItem);
                        if (!MainMode)
                        {
                            MakeCutButton.IsEnabled = true;
                        }
                    }
                    else
                    {
                        ClearPosition(1);
                        ClearPosition(2);
                        ErrorMessage.Text = $"Отгрузка завершена, повторный раскрой невозможен";
                    }

                    ProcessPermissions();
                }
            }
        }

        /// <summary>
        /// Загрузка данных в таблицу позиций
        /// </summary>
        public async void PositionLoadItems()
        {
            int taskId = SelectedTaskItem.CheckGet("ID").ToInt();
            if (taskId > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Position");
                q.Request.SetParam("Action", "ListTaskCopy");
                q.Request.SetParam("ID", SelectedTaskItem["ID"]);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        if (result.ContainsKey("ITEMS"))
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            PositionGrid.UpdateItems(ds, false);
                        }

                        // Заполняем поля с названием бумаги в слоях
                        if (result.ContainsKey("RAW_GROUPS"))
                        {
                            var layerDS = ListDataSet.Create(result, "RAW_GROUPS");
                            var v = new Dictionary<string, string>();
                            for (int i=1; i<6; i++)
                            {
                                v.Add($"LAYER_{i}_RAWGROUP", "");
                            }
                            foreach(var item in layerDS.Items)
                            {
                                var l = item.CheckGet("LAYER").ToInt();
                                if (l > 0)
                                {
                                    v[$"LAYER_{l}_RAWGROUP"] = item.CheckGet("RAW_NAME");
                                }
                            }
                            Form.SetValues(v);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Получение с сервера результатов раскроя для создания производственного задания
        /// </summary>
        private async void GetReworkTask()
        {
            TaskToolBar.IsEnabled = false;

            string positionId = ReworkPosition["ID"];
            int blankQty = 0;
            string formatsString = "";
            bool resume = true;
            string msg = "";
            TextBox control = null;

            if (Stacker1PositionId.Text == ReworkPosition["ID"])
            {
                blankQty = Stacker1QuantityCalculated.Text.ToInt();
                control = Stacker1QuantityCalculated;
            }
            else if (Stacker2PositionId.Text == ReworkPosition["ID"])
            {
                blankQty = Stacker2QuantityCalculated.Text.ToInt();
                control = Stacker2QuantityCalculated;
            }
            else
            {
                msg = "Не удалось определить задание";
                resume = false;
            }

            if (resume)
            {
                if (blankQty == 0)
                {
                    msg = "Не указано количество заготовок";
                    resume = false;
                }
            }

            if (resume)
            {
                var len = Math.Ceiling(ReworkPosition["LENGTH"].ToDouble() * blankQty / 1000);
                if (len < 100)
                {
                    msg = "Слишком короткое задание. Увеличьте количество заготовок";
                    resume = false;
                }
            }

            if (!string.IsNullOrEmpty(msg))
            {
                SetControlBorder(control, 1);
                ErrorMessage.Text = msg;
                resume = false;
            }
            else
            {
                ErrorMessage.Text = "";
            }

            // Если заготовок больше, чем в задании, требуется подтверждение оператора
            if (resume)
            {
                int requiredBlanks = ReworkPosition["BLANK_QTY"].ToInt();
                if (blankQty > requiredBlanks)
                {
                    var dw = new DialogWindow($"По заданию требуется {requiredBlanks} заготовок. Вы уверены, что надо изготовить больше?", "Раскрой по ПЗ", "", DialogWindowButtons.NoYes);
                    if ((bool)dw.ShowDialog())
                    {
                        if (dw.ResultButton == DialogResultButton.No)
                        {
                            resume = false;
                        }
                    }
                }
            }

            if (resume)
            {
                if (Formats.Count > 0)
                {
                    formatsString = JsonConvert.SerializeObject(Formats);
                }
                else
                {
                    resume = false;
                }
            }

            if (resume)
            {
                // Количество изделий. Вычислим из требуемого количества заготовок
                var prodFomBlank = ReworkPosition.CheckGet("PRODUCTS_FROM_BLANK").ToDouble();
                if (prodFomBlank == 0)
                {
                    prodFomBlank = 1.0;
                }
                var positionQty = (blankQty * prodFomBlank).ToInt();

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Position");
                q.Request.SetParam("Action", "ReworkCutting");
                q.Request.SetParam("TASK_ID", SelectedTaskItem["ID"]);
                q.Request.SetParam("POSITION_ID", positionId);
                q.Request.SetParam("POSITION_QTY", positionQty.ToString());
                q.Request.SetParam("CARDBOARD_ID", SelectedPositionItem["CARDBOARD_ID"]);
                q.Request.SetParam("FORMATS", formatsString);
                q.Request.SetParam("WITHLONG", CuttingWithLong.IsChecked.ToInt().ToString());
                q.Request.SetParam("FACTORY_ID", FactoryId.ToString());

                q.Request.Timeout = 60000;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        ClearPosition(1);
                        ClearPosition(2);

                        var task = ListDataSet.Create(result, "TASK");
                        if (task.Items.Count > 0)
                        {
                            ReworkTask = task.Items[0];
                            ReworkTask["LENGTH"] = Math.Ceiling((decimal)ReworkTask["LENGTH"].ToDouble()).ToString();
                            string taskNum = SelectedTaskItem.CheckGet("NUM");
                            if (taskNum.Length > 5)
                            {
                                ReworkTask["NUM"] = taskNum.Substring(0, 5);
                            }

                            // Заполняем поля данными из сгенерированного задания
                            var v = new Dictionary<string, string>();
                            v.CheckAdd("CARDBOARD_NAME", ReworkTask.CheckGet("CARDBOARD_NAME"));
                            v.CheckAdd("CARDBOARD_ID", ReworkTask.CheckGet("IDC"));
                            v.CheckAdd("QID_NAME", ReworkTask.CheckGet("QID"));
                            v.CheckAdd("CUTTING_WIDTH", ReworkTask.CheckGet("FORMAT"));
                            v.CheckAdd("CUTTING_LENGTH", ReworkTask.CheckGet("LENGTH"));
                            v.CheckAdd("TRIM_PERCENTAGE", ReworkTask.CheckGet("TRIM_PERCENT"));

                            if (!string.IsNullOrEmpty(ReworkTask.CheckGet("ID_ORDERDATES1")))
                            {
                                v.CheckAdd($"STACKER1_POSITION_ID", ReworkTask.CheckGet("ID_ORDERDATES1"));
                                v.CheckAdd($"STACKER1_BLANK_ID", ReworkTask.CheckGet("BLANK_ID1"));
                                v.CheckAdd($"STACKER1_GOOD_ID", ReworkTask.CheckGet("GOODS_ID1"));
                                v.CheckAdd($"STACKER1_BLANK", ReworkTask.CheckGet("NAME_ZAG1"));
                                v.CheckAdd($"STACKER1_CREASE", ReworkTask.CheckGet("CREASE1"));
                                v.CheckAdd($"STACKER1_LENGTH", ReworkTask.CheckGet("LENGTH1"));
                                v.CheckAdd($"STACKER1_WIDTH", ReworkTask.CheckGet("WIDTH1"));
                                v.CheckAdd($"STACKER1_THREADS", ReworkTask.CheckGet("THREAD1"));
                                v.CheckAdd($"STACKER1_QUANTITY_CALCULATED", ReworkTask.CheckGet("QTY1"));

                                if (ReworkTask.CheckGet("BLANK_ID1") == ReworkPosition.CheckGet("BLANK_ID"))
                                {
                                    v.CheckAdd($"STACKER1_SHIPMENT_DATE_TIME", ReworkPosition.CheckGet("SHIPMENT_DATE_TIME"));
                                    v.CheckAdd($"STACKER1_CREASE_NOTE", ReworkPosition.CheckGet("CREASE_NOTE"));
                                    v.CheckAdd($"STACKER1_QUANTITY", ReworkPosition.CheckGet("BLANK_QTY"));
                                    v.CheckAdd($"STACKER1_PRODUCTS_FROM_BLANK", ReworkPosition.CheckGet("PRODUCTS_FROM_BLANK"));
                                    v.CheckAdd($"STACKER1_PALLET", ReworkPosition.CheckGet("PRODUCTS_IN_PALLET"));
                                    Stacker1QuantityCalculated.IsReadOnly = false;
                                }
                            }

                            if (!string.IsNullOrEmpty(ReworkTask.CheckGet("ID_ORDERDATES2")))
                            {
                                v.CheckAdd($"STACKER2_POSITION_ID", ReworkTask.CheckGet("ID_ORDERDATES2"));
                                v.CheckAdd($"STACKER2_BLANK_ID", ReworkTask.CheckGet("BLANK_ID2"));
                                v.CheckAdd($"STACKER2_GOOD_ID", ReworkTask.CheckGet("GOODS_ID2"));
                                v.CheckAdd($"STACKER2_BLANK", ReworkTask.CheckGet("NAME_ZAG2"));
                                v.CheckAdd($"STACKER2_CREASE", ReworkTask.CheckGet("CREASE2"));
                                v.CheckAdd($"STACKER2_LENGTH", ReworkTask.CheckGet("LENGTH2"));
                                v.CheckAdd($"STACKER2_WIDTH", ReworkTask.CheckGet("WIDTH2"));
                                v.CheckAdd($"STACKER2_THREADS", ReworkTask.CheckGet("THREAD2"));
                                v.CheckAdd($"STACKER2_QUANTITY_CALCULATED", ReworkTask.CheckGet("QTY2"));

                                if (ReworkTask.CheckGet("BLANK_ID2") == ReworkPosition.CheckGet("BLANK_ID"))
                                {
                                    v.CheckAdd($"STACKER2_SHIPMENT_DATE_TIME", ReworkPosition.CheckGet("SHIPMENT_DATE_TIME"));
                                    v.CheckAdd($"STACKER2_CREASE_NOTE", ReworkPosition.CheckGet("CREASE_NOTE"));
                                    v.CheckAdd($"STACKER2_QUANTITY", ReworkPosition.CheckGet("BLANK_QTY"));
                                    v.CheckAdd($"STACKER2_PRODUCTS_FROM_BLANK", ReworkPosition.CheckGet("PRODUCTS_FROM_BLANK"));
                                    v.CheckAdd($"STACKER2_PALLET", ReworkPosition.CheckGet("PRODUCTS_IN_PALLET"));
                                    Stacker2QuantityCalculated.IsReadOnly = false;
                                }
                            }

                            // сырье по слоям
                            v.CheckAdd("LAYER_1_RAWGROUP", ReworkTask.CheckGet("LAYER_1_RAWGROUP"));
                            v.CheckAdd("LAYER_2_RAWGROUP", ReworkTask.CheckGet("LAYER_2_RAWGROUP"));
                            v.CheckAdd("LAYER_3_RAWGROUP", ReworkTask.CheckGet("LAYER_3_RAWGROUP"));
                            v.CheckAdd("LAYER_4_RAWGROUP", ReworkTask.CheckGet("LAYER_4_RAWGROUP"));
                            v.CheckAdd("LAYER_5_RAWGROUP", ReworkTask.CheckGet("LAYER_5_RAWGROUP"));

                            Form.SetValues(v);

                            // схема раскроя
                            var p = new Dictionary<string, string>();
                            p.Add("FORMAT", ReworkTask.CheckGet("FORMAT"));
                            p.Add("STACKER1_WIDTH", ReworkTask.CheckGet("WIDTH1"));
                            p.Add("STACKER1_THREADS", ReworkTask.CheckGet("THREAD1"));
                            p.Add("STACKER1_ITEM_ID", ReworkTask.CheckGet("BLANK_ID1"));
                            p.Add("STACKER1_CREASE_SYMMETRIC", "");
                            p.Add("STACKER1_TITLE", "1");
                            p.Add("STACKER2_WIDTH", ReworkTask.CheckGet("WIDTH2"));
                            p.Add("STACKER2_THREADS", ReworkTask.CheckGet("THREAD2"));
                            p.Add("STACKER2_ITEM_ID", ReworkTask.CheckGet("BLANK_ID2"));
                            p.Add("STACKER2_CREASE_SYMMETRIC", "");
                            p.Add("STACKER2_TITLE", "2");

                            p.Add("TRIM_SIZE", "");
                            p.Add("TRIM", "");
                            p.Add("TRIM_RATED", ReworkTask.CheckGet("TRIM_PERCENT"));
                            p.Add("TRIM_SIZE_RATED", ReworkTask.CheckGet("TRIM"));
                            p.Add("MAP_STATUS", "true");

                            GetMap(p);
                            SaveTaskButton.IsEnabled = true;
                            if (ReworkTask.CheckGet("ID_ORDERDATES1") == positionId)
                            {
                                ReworkBlankQty = ReworkTask.CheckGet("QTY1").ToInt();
                            }
                            else if (ReworkTask.CheckGet("ID_ORDERDATES2") == positionId)
                            {
                                ReworkBlankQty = ReworkTask.CheckGet("QTY2").ToInt();
                            }

                            ProcessPermissions();
                        }
                    }
                }
                else if (q.Answer.Status == 145)
                {
                    var dw = new DialogWindow(q.Answer.Error.Message, "Раскрой по ПЗ");
                    dw.ShowDialog();

                }
                else
                {
                    q.ProcessError();
                }
            }
            TaskToolBar.IsEnabled = true;
        }

        /// <summary>
        /// загрузка схемы раскроя с сервера
        /// </summary>
        private async void GetMap(Dictionary<string, string> p)
        {
            bool resume = true;

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Cutter");
                q.Request.SetParam("Action", "CutterGetMap");

                q.Request.SetParams(p);
                q.Request.RequiredAnswerType = LPackClientAnswer.AnswerTypeRef.Stream;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                SchemeImage.Source = null;

                if (q.Answer.Status == 0)
                {
                    if (q.Answer.DataStream != null)
                    {
                        try
                        {
                            BitmapImage image = new BitmapImage();
                            image.BeginInit();
                            image.StreamSource = q.Answer.DataStream;
                            image.EndInit();
                            SchemeImage.Source = image;
                        }
                        catch (Exception e)
                        {

                        }
                    }
                }
            }
        }

        /// <summary>
        /// Очистка полей, относящихся к заданному стекеру
        /// </summary>
        /// <param name="x">Номер стекера</param>
        private void ClearPosition(int x)
        {
            if (x.ContainsIn(1,2))
            {
                var v = new Dictionary<string, string>();
                v.CheckAdd($"CARDBOARD_NAME", "");
                v.CheckAdd($"QID_NAME", "");
                v.CheckAdd($"CUTTING_WIDTH", "");
                v.CheckAdd($"CUTTING_LENGTH", "");
                v.CheckAdd($"TRIM_PERCENTAGE", "");
                v.CheckAdd($"STACKER{x}_POSITION_ID", "");
                v.CheckAdd($"STACKER{x}_BLANK_ID", "");
                v.CheckAdd($"STACKER{x}_GOOD_ID", "");
                v.CheckAdd($"STACKER{x}_BLANK", "");
                v.CheckAdd($"STACKER{x}_SHIPMENT_DATE_TIME", "");
                v.CheckAdd($"STACKER{x}_CREASE", "");
                v.CheckAdd($"STACKER{x}_LENGTH", "");
                v.CheckAdd($"STACKER{x}_WIDTH", "");
                v.CheckAdd($"STACKER{x}_CREASE_NOTE", "");
                v.CheckAdd($"STACKER{x}_QUANTITY", "");
                v.CheckAdd($"STACKER{x}_QUANTITY_CALCULATED", "");
                v.CheckAdd($"STACKER{x}_THREADS", "");
                v.CheckAdd($"STACKER{x}_PRODUCTS_FROM_BLANK", "");
                v.CheckAdd($"STACKER{x}_PALLET", "");
                Form.SetValues(v);
                SchemeImage.Source = null;
                SaveTaskButton.IsEnabled = false;
                ReworkTask.Clear();
                Stacker1QuantityCalculated.IsReadOnly = true;
                SetControlBorder(Stacker1QuantityCalculated, 0);
                Stacker2QuantityCalculated.IsReadOnly = true;
                SetControlBorder(Stacker2QuantityCalculated, 0);
            }
        }

        /// <summary>
        /// Заполнение полей выбранной позиции для повторного раскроя
        /// </summary>
        /// <param name="selectedPosition"></param>
        private void SetPosition(Dictionary<string,string> selectedPosition)
        {
            if (selectedPosition != null)
            {
                // номер стекера в задании
                var stacker = selectedPosition.CheckGet("STACKER").ToInt();
                var x = stacker == 3 ? 2 : stacker;
                ClearPosition(x == 1 ? 2 : 1);

                ReworkPosition = selectedPosition;
                PrimaryIdPz = SelectedTaskItem.CheckGet("ID").ToInt();
                var v = new Dictionary<string, string>();
                // Картон
                v.CheckAdd("CARDBOARD_NAME", ReworkPosition.CheckGet("CARDBOARD_NAME"));
                v.CheckAdd("CARDBOARD_ID", ReworkPosition.CheckGet("CARDBOARD_ID"));

                var blank = $"{ReworkPosition.CheckGet("BLANK_NAME")} {ReworkPosition.CheckGet("BLANK_CODE")}";

                v.CheckAdd($"STACKER{x}_POSITION_ID", ReworkPosition.CheckGet("ID"));
                v.CheckAdd($"STACKER{x}_BLANK_ID", ReworkPosition.CheckGet("BLANK_ID"));
                v.CheckAdd($"STACKER{x}_GOOD_ID", ReworkPosition.CheckGet("GOODS_ID"));
                v.CheckAdd($"STACKER{x}_BLANK", blank);
                v.CheckAdd($"STACKER{x}_SHIPMENT_DATE_TIME", ReworkPosition.CheckGet("SHIPMENT_DATE_TIME"));
                v.CheckAdd($"STACKER{x}_CREASE", ReworkPosition.CheckGet("CREASE_TYPE"));
                v.CheckAdd($"STACKER{x}_LENGTH", ReworkPosition.CheckGet("LENGTH"));
                v.CheckAdd($"STACKER{x}_WIDTH", ReworkPosition.CheckGet("WIDTH"));
                v.CheckAdd($"STACKER{x}_PALLET", ReworkPosition.CheckGet("PRODUCTS_IN_PALLET"));

                var blankQty = ReworkPosition.CheckGet("TASK_QTY").ToInt();
                var prodFomBlank = ReworkPosition.CheckGet("PRODUCTS_FROM_BLANK").ToDouble();
                v.CheckAdd($"STACKER{x}_PRODUCTS_FROM_BLANK", prodFomBlank.ToString());
                ReworkPosition.CheckAdd("BLANK_QTY", blankQty.ToString());
                v.CheckAdd($"STACKER{x}_QUANTITY", blankQty.ToString());
                v.CheckAdd($"STACKER{x}_QUANTITY_CALCULATED", "");

                {
                    /*
                        Тип рилёвки для заготовки,
                        1 = СТРОГО 3 точки(в простонародье папа-мама), 
                        2 = СТРОГО плоская рилёвка, 
                        3 = НЕВАЖНО, т.е. будем смотреть по ситуации по конкретному заданию, 
                        4 - папа-папа (3 точки, смещение = 1)
                        */

                    var c1 = "";
                    {
                        switch (ReworkPosition.CheckGet("CREASE_TYPE").ToInt())
                        {
                            case 1:
                                c1 = "п/м";
                                break;

                            case 2:
                                c1 = "пл";
                                break;

                            case 4:
                                c1 = "п/п";
                                break;
                        }
                    }
                    ReworkPosition.CheckAdd("CREASE_NOTE", c1);
                    v.CheckAdd($"STACKER{x}_CREASE_NOTE", c1);
                }

                Form.SetValues(v);
                if (ReworkReason.SelectedItem.Key == null)
                {
                    SetControlBorder(ReworkReason, 2);
                }

                // Разблокируем поле ввода количества
                if (x == 1)
                {
                    Stacker1QuantityCalculated.IsReadOnly = false;
                    SetControlBorder(Stacker1QuantityCalculated, 2);
                }
                else
                {
                    Stacker2QuantityCalculated.IsReadOnly = false;
                    SetControlBorder(Stacker2QuantityCalculated, 2);
                }
            }
        }

        /// <summary>
        /// Сохранение созданного производственноо задания
        /// </summary>
        public async void Save()
        {
            bool resume = true;

            if (ReworkReason.SelectedItem.Key.IsNullOrEmpty())
            {
                SetControlBorder(ReworkReason, 1);
                ErrorMessage.Text = "Заполните причину повторного выполнения задания";
                resume = false;
            }

            if (resume && (ReworkTask.Count > 0))
            {
                // Блокируем кнопку сохранения
                SaveTaskButton.IsEnabled = false;

                //Копируем в примечание к заданию причину перевыгона
                string reworkReasonNote = ReworkReason.SelectedItem.Value;
                if (!Comments.Text.IsNullOrEmpty())
                {
                    reworkReasonNote = $"{reworkReasonNote} ({Comments.Text})";
                }
                string taskNote = ReworkTask.CheckGet("NOTE");
                if (taskNote.IsNullOrEmpty())
                {
                    taskNote = reworkReasonNote;
                }
                else
                {
                    taskNote = $"{taskNote}. {reworkReasonNote}";
                }
                ReworkTask.CheckAdd("NOTE", taskNote);

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ProductionTask");
                q.Request.SetParam("Action", "SaveRework");
                q.Request.Timeout = 120000;

                // Полученное производственное задание
                q.Request.SetParams(ReworkTask);
                q.Request.SetParam("REWORK_REASON", ReworkReason.SelectedItem.Key);
                q.Request.SetParam("REWORK_COMMENT", Comments.Text);
                q.Request.SetParam("PRIMARY_ID_PZ", PrimaryIdPz.ToString());
                q.Request.SetParam("PRIMARY_ID2", ReworkPosition["BLANK_ID"]);
                q.Request.SetParam("QTY", ReworkBlankQty.ToString());


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
                        if (ds.Items.Count > 0)
                        {
                            ClearPosition(1);
                            ClearPosition(2);
                            
                            TaskGrid.UpdateItems();

                            var taskNum = ds.Items[0]["NUM"];
                            var dw = new DialogWindow($"Создано новое задание {taskNum}", "Раскрой по ПЗ");
                            dw.ShowDialog();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Отправляем выбранную позицию в форму ручного раскроя
        /// </summary>
        public void SelectForManual()
        {
            bool resume = true;

            if (ReworkReason.SelectedItem.Key.IsNullOrEmpty())
            {
                SetControlBorder(ReworkReason, 1);
                ErrorMessage.Text = "Заполните причину повторного выполнения задания";
                resume = false;
            }

            if (resume)
            {
                var position = ReworkPosition;
                position.CheckAdd("STACKER_ID", ReworkPosition.CheckGet("STACKER"));
                position.CheckAdd("POSITION_ID", ReworkPosition.CheckGet("ID"));
                position.CheckAdd("REWORK_REASON", ReworkReason.SelectedItem.Key);
                position.CheckAdd("REWORK_COMMENT", Comments.Text);
                position.CheckAdd("PRIMARY_ID_PZ", PrimaryIdPz.ToString());
                position.CheckAdd("PRIMARY_ID2", ReworkPosition["BLANK_ID"]);
                position.CheckAdd("PRODUCTS_IN_APPLICATION", ReworkPosition["TASK_QTY"]);

                // Вычисляем признак полный штамп на роторной линии
                bool FullStamp = ReworkPosition.CheckGet("FULL_STAMP").ToBool();
                bool StampingFormRt = ReworkPosition.CheckGet("STAMPING_FORM_RT").ToBool();
                bool FullStampRt = FullStamp && StampingFormRt;
                position.CheckAdd("FULL_STAMP_RT", FullStampRt ? "1" : "0");

                string taskNum = SelectedTaskItem.CheckGet("NUM");
                if (taskNum.Length > 5)
                {
                    taskNum = taskNum.Substring(0, 5);
                }
                position.CheckAdd("NUM", taskNum);

                //Считаем рилевки
                int creaseCount = 0;
                string creaseList = "";
                int w = ReworkPosition.CheckGet("WIDTH").ToInt();
                int creaseSymmetric = ReworkPosition.CheckGet("CREASE_SYMMETRIC").ToInt();
                if (creaseSymmetric > 0)
                {
                    creaseCount = 2;
                    int middle = w - creaseSymmetric * 2;
                    creaseList = $"{creaseSymmetric}-{middle}-{creaseSymmetric}";
                }
                else
                {
                    for (var i = 1; i <= 24; i++)
                    {
                        int crease = ReworkPosition.CheckGet($"CREASE{i}").ToInt();
                        if (crease > 0)
                        {
                            creaseList = $"{creaseList}{crease}-";
                            creaseCount++;
                            w -= crease;
                        }
                    }

                    if (creaseCount > 0)
                    {
                        creaseList = $"{creaseList}{w}";
                    }
                }

                position.CheckAdd("CREASE_COUNT", creaseCount.ToString());
                position.CheckAdd("CREASE_LIST", creaseList);

                //отправляем сообщение о выборе заготовки
                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = "ProductionTask",
                    ReceiverName = "CuttingManualView",
                    SenderName = "TaskRework",
                    Action = "SelectedBlank",
                    ContextObject = position,
                });
                CloseTab();
            }
        }

        public void ShowTab()
        {
            string title = $"Перевыпуск";
            Central.WM.AddTab($"select_task_rework", title, true, "add", this);

            MainMode = false;

            // Если открываем как дочернюю вкладку, прячем ненужные элементы
            TaskParamsPannel.Visibility = Visibility.Collapsed;
            PositionsPannel.Visibility = Visibility.Collapsed;
            TaskMapPannel.Visibility = Visibility.Collapsed;
            SourcePannel.Visibility = Visibility.Collapsed;
            FormatContainer.Visibility = Visibility.Collapsed;

            SaveTaskButton.Visibility = Visibility.Collapsed;
            CloseButton.Visibility = Visibility.Visible;
            MakeCutButton.Content = "Выбрать";

            Initialize();
        }

        public void CloseTab()
        {
            Central.WM.RemoveTab($"select_task_rework");
            Destroy();
            GoBack();
        }
        
        /// <summary>
        /// Возврат в предыдущий интерфейс
        /// </summary>
        public void GoBack()
        {
            if (!string.IsNullOrEmpty(BackTabName))
            {
                Central.WM.SetActive(BackTabName, true);
                BackTabName = "";
            }
        }

        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/production/creating_tasks/task_rework");
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            TaskGrid.LoadItems();
        }

        /// <summary>
        /// Обработка нажатия на кнопку Раскроить (или Выбрать в дочернем режиме)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MakeCutButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainMode)
            {
                ErrorMessage.Text = "";
                int positionId = SelectedPositionItem.CheckGet("ID").ToInt();
                int cardboardId = SelectedPositionItem.CheckGet("CARDBOARD_ID").ToInt();

                if ((positionId > 0) && (cardboardId > 0))
                {
                    GetReworkTask();
                }
                else
                {
                    ErrorMessage.Text = "Ошибка данных в позиции для раскроя";
                }
            }
            else
            {
                SelectForManual();
            }
        }

        /// <summary>
        /// Корректировка выбранной причины дублирования задания. Названия групп причин выбирать нельзя
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void ReworkReason_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            int k = ReworkReason.SelectedItem.Key.ToInt();
            int i = 0;
            foreach (var item in ReworkReasonDS.Items)
            {
                if (item["ID"].ToInt() == k)
                {
                    if (item["SELECTED_FLAG"].ToInt() == 0)
                    {
                        // Находим ID следующего элемента
                        k = ReworkReasonDS.Items[i+1]["ID"].ToInt();
                        ReworkReason.SetSelectedItemByKey(k.ToString());
                    }
                    break;
                }
                i++;
            }
            ErrorMessage.Text = "";
            SetControlBorder(ReworkReason, 0);
        }

        /// <summary>
        /// Сохранение производственного задания
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveTaskButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void Stacker1QuantityCalculated_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Stacker1QuantityCalculated.Text.IsNullOrEmpty())
            {
                MakeCutButton.IsEnabled = false;
                SetControlBorder(Stacker1QuantityCalculated, 2);
            }
            else
            {
                MakeCutButton.IsEnabled = true;
                SetControlBorder(Stacker1QuantityCalculated, 0);

                ProcessPermissions();
            }
        }

        private void Stacker2QuantityCalculated_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Stacker2QuantityCalculated.Text.IsNullOrEmpty())
            {
                MakeCutButton.IsEnabled = false;
                SetControlBorder(Stacker2QuantityCalculated, 2);
            }
            else
            {
                MakeCutButton.IsEnabled = true;
                SetControlBorder(Stacker2QuantityCalculated, 0);

                ProcessPermissions();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseTab();
        }

        /// <summary>
        /// Ставит флажки во все чекбоксы форматов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormatResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (FormatContainer.Children.Count > 0)
            {
                foreach (CheckBox c in FormatContainer.Children)
                {
                    c.IsChecked = true;
                }
            }
        }
    }
}
