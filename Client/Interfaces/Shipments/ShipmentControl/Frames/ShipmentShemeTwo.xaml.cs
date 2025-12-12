using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Linq;
using System.Printing;
using System;
using System.Globalization;
using System.Windows.Media.Imaging;
using DevExpress.Xpf.Core.Native;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Логика взаимодействия для ShipmentShemeTwo.xaml
    /// </summary>
    public partial class ShipmentShemeTwo : UserControl
    {
        public ShipmentShemeTwo()
        {
            ShipmentId = 0;
            ReturnTabName = "";
            UpdateCtl = false;

            CheckedRow = new Dictionary<string, string>();
            ArticNumDictionnary = new Dictionary<string, int>();

            offsetHeigth = 20;

            CarForPlaced = 1;

            GlobalLoadOrder = 1;

            CollorDictionary = new Dictionary<string, string>();
            CollorDictionary.Add("-1", "#ffff99"); // (б/уп)песочный
            CollorDictionary.Add("0", "#d6fcd5");  // (c/уп)салатовый
            CollorDictionary.Add("1", "#b5f2ea");  // Аквамарин
            CollorDictionary.Add("2", "#fed6bc");  // Персиковый
            CollorDictionary.Add("3", "#3eb489");  // Мятный
            CollorDictionary.Add("4", "#fffadd");  // Светло-жёлтый
            CollorDictionary.Add("5", "#e7ecff");  // Лиловый
            CollorDictionary.Add("6", "#fadadd");  // Бледно-розовый
            CollorDictionary.Add("7", "#dad871");  // Вердепешевый
            CollorDictionary.Add("8", "#b39f7a");  // Кофе с молоком
            CollorDictionary.Add("9", "#def7fe");  // Голубой 
            CollorDictionary.Add("10", "#ffbd88"); // Макароны и сыр
            CollorDictionary.Add("11", "#acb78e"); // Болотный
            CollorDictionary.Add("12", "#ffcf48"); // Восход солнца
            CollorDictionary.Add("13", "#a18594"); // Пастельно-фиолетовый
            CollorDictionary.Add("14", "#ff9baa"); // Лососевый Крайола
            CollorDictionary.Add("15", "#c6df90"); // Очень светлый желто-зеленый
            CollorDictionary.Add("16", "#efa94a"); // Пастельно-желтый
            CollorDictionary.Add("17", "#afdafc"); // Синий-синий иней
            CollorDictionary.Add("18", "#e4717a"); // Карамельно-розовый
            CollorDictionary.Add("19", "#f6fff8"); // Лаймовый
            CollorDictionary.Add("20", "#e6d690"); // Светлая слоновая кость
            CollorDictionary.Add("99", "#eeeeee"); // (транспорт)серый

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            TruckScene = new Scene();
            //объекты в верстке -> в структуру
            TruckScene.ModelContainer = PalletsModelVisual3D;
            TruckScene.Camera = myCamera;
            TruckScene.Light1 = L1;
            TruckScene.Light2 = L2;
            TruckScene.ContainerGroupNumberPallet = NumberTextBoxContainer;
            TruckScene.UIPointContainer = PointContainerUIElement3D;

            if (Central.DebugMode != true)
            {
                InitButton.Visibility = Visibility.Hidden;
                RenderButton.Visibility = Visibility.Hidden;
            }

            TrailerScene = new Scene();
            TrailerScene.ModelContainer = TrailerPalletsModelVisual3D;
            TrailerScene.Camera = TrailerCamera;
            TrailerScene.Light1 = TrailerL1;
            TrailerScene.Light2 = TrailerL2;
            TrailerScene.ContainerGroupNumberPallet = TrailerNumberTextBoxContainer;
            TrailerScene.UIPointContainer = TrailerPointContainerUIElement3D;

            OKButton.IsEnabled = false;
            TruckRadioButton.IsChecked = true;

            PalletDemonstrationScene = new PalletScene();
            PalletDemonstrationScene.ModelContainer = PalletDemonstrationModelVisual3D;
            PalletDemonstrationScene.Camera = PalletDemonstrationCamera;
            PalletDemonstrationScene.Light1 = PalletDemonstrationL1;
            PalletDemonstrationScene.Light2 = PalletDemonstrationL2;
            PalletDemonstrationScene.ContainerGroupNumberPallet = PalletDemonstrationNumberTextBoxContainer;


            InitGrid();
            PositionGridInit();

            Form.SetValueByPath("STEP_OF_MOVEMENT", "5");

            //ContainerUIElement3D uiPointContainer = PointContainerUIElement3D;

            //ModelUIElement3D testModelUIElement3D = new ModelUIElement3D();
            //GeometryModel3D geometryModel3D = new GeometryModel3D();

            //var textPoint = new Box();
            //textPoint.Left = 0;
            //textPoint.Top = 0;
            //textPoint.Length = 500;
            //textPoint.Width = 500;
            //textPoint.Height = 500;
            //textPoint.CollorInner = "#ffff00";
            //textPoint.ColumnIndexPlace = 1;
            //textPoint.RowIndexPlace = 1;
            //textPoint.PointRotationOption = 0;

            //MeshGeometry3D mesh = textPoint.CreateMesh();
            //var color = textPoint.CollorOuter;
            //var brush = color.ToBrush();
            //var material = new DiffuseMaterial(brush);

            //geometryModel3D.Geometry = mesh;
            //geometryModel3D.Material = material;

            //testModelUIElement3D.Model = geometryModel3D;
            //testModelUIElement3D.MouseDown += ModelUIElement3D_MouseDown_6;

            //uiPointContainer.Children.Add(testModelUIElement3D);

            ProcessPermissions();
        }

        //private void ModelUIElement3D_MouseDown_6(object sender, MouseButtonEventArgs e)
        //{

        //}

        public string RoleName = "[erp]manually_loading_scheme";

        public string FrameName { get; set; }

        /// <summary>
        /// Основной ДатаСет интерфейса
        /// </summary>
        public ListDataSet Ds;
        /// <summary>
        /// ИД отгрузки
        /// </summary>
        public int ShipmentId { get; set; }
        public string ReturnTabName { get; set; }
        /// <summary>
        /// Полученное сообщение с настройками просмотра сцены
        /// </summary>
        public Dictionary<string, string> rowMessage { get; set; }

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        public Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// фура + прицеп
        /// </summary>
        public Dictionary<string, string> Cars { get; set; }

        public bool UpdateCtl { get; set; }

        /// <summary>
        /// Сцена фуры
        /// </summary>
        public Scene TruckScene { get; set; }

        /// <summary>
        /// Сцена пицепа
        /// </summary>
        public Scene TrailerScene { get; set; }

        /// <summary>
        /// Сцена предпросмотра модели поддона
        /// </summary>
        public PalletScene PalletDemonstrationScene { get; set; }

        /// <summary>
        /// Номер транспорта для размещения
        /// 1 - Фургон;
        /// 2 - Прицеп
        /// </summary>
        public int CarForPlaced { get; set; }

        /// <summary>
        /// Текущее максимальное значение поля LOAD_ORDER;
        /// Используется для определения того, какие позиции сейчас можно размещать;
        /// </summary>
        public int GlobalLoadOrder { get; set; }

        /// <summary>
        /// Запись, у которой в данный момент активен флаг размещения 
        /// </summary>
        public Dictionary<string, string> CheckedRow { get; set; }

        /// <summary>
        /// Статус схемы погрузки
        /// </summary>
        public int BootOrderStatus { get; set; }

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
        /// Деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "ShipmentControl",
                ReceiverName = "",
                SenderName = "ShipmentShemeTwo",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            Grid.Destruct();
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            var row = Ds.GetFirstItem();

            if (row.CheckGet("TRAILER_FLAG") == "1")
            {
                TrailerRadioButton.IsEnabled = true;
                TrailerRadioButtonLable.IsEnabled = true;
            }
            else
            {
                TrailerRadioButton.IsEnabled = false;
                TrailerRadioButtonLable.IsEnabled = false;
            }

            SceneSetDefaultParams();
        }

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        public void InitGrid()
        {
            //список колонок грида
            var columns = new List<DataGridHelperColumn>()
            {
                new DataGridHelperColumn()
                {
                    Header=$"№{Environment.NewLine}Номер позиции",
                    Path="RN",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=40,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("PLACED") == "1")
                                {
                                    if (row.CheckGet("OBVAZ") == "б/уп")
                                    {
                                        color = CollorDictionary.CheckGet("-1");
                                    }
                                    else
                                    {
                                        color = GetColor(row.CheckGet("RN").ToInt().ToString());
                                    }
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        },
                    },
                },
                new DataGridHelperColumn()
                {
                    Header=$"##{Environment.NewLine}Порядок загрузки",
                    Path="LOAD_ORDER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=40,
                },
                new DataGridHelperColumn()
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=50,
                    Hidden = CheckHidenForUser(),
                },
                new DataGridHelperColumn()
                {
                    Header="ID позиции отгрузки",
                    Path="POSITION_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=50,
                    Hidden = CheckHidenForUser(),
                },
                new DataGridHelperColumn()
                {
                    Header="ИД отгрузки",
                    Path="IDTS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=50,
                    Hidden = CheckHidenForUser(),
                },
                new DataGridHelperColumn()
                {
                    Header="ИД заявки",
                    Path="NSTHET",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=50,
                    Hidden = CheckHidenForUser(),
                },
                new DataGridHelperColumn()
                {
                    Header="Артикул",
                    Path="ARTIKUL",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width=120,
                },
                new DataGridHelperColumn()
                {
                    Header="Наименование",
                    Path="NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width=290,
                },
                new DataGridHelperColumn()
                {
                    Header="Упаковка",
                    Path="OBVAZ",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width=50,
                },
                new DataGridHelperColumn()
                {
                    Header="Длина",
                    Path="LT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=70,
                    Hidden = CheckHidenForUser(),
                },
                new DataGridHelperColumn()
                {
                    Header="Ширина",
                    Path="BT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=70,
                    Hidden = CheckHidenForUser(),
                },
                new DataGridHelperColumn()
                {
                    Header="Высота",
                    Path="HT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=70,
                    Hidden = CheckHidenForUser(),
                },
                new DataGridHelperColumn()
                {
                    Header="Транс. пакет",
                    Path="SIZE_TK",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width=100,
                },
                new DataGridHelperColumn()
                {
                    Header=$"Габариты{Environment.NewLine}Фактические размеры поддона + припуск",
                    Path="SIZE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width=100,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                // Не полный поддон
                                if( row.CheckGet("INCOMPLET_PALLET").ToInt() == 1 )
                                {
                                    color = HColor.Yellow;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        },
                    },
                },
                new DataGridHelperColumn()
                {
                    Header="шт. в заявке",
                    Path="QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=100,
                    Hidden = CheckHidenForUser(),
                },
                new DataGridHelperColumn()
                {
                    Header="шт. грузится",
                    Path="KOL_PAK",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=105,
                    Hidden = CheckHidenForUser(),
                },
                new DataGridHelperColumn()
                {
                    Header="кол. поддонов",
                    Path="KOL_POD",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=70,
                    Hidden = CheckHidenForUser(),
                },
                new DataGridHelperColumn()
                {
                    Header="№ поддона в позиции",
                    Path="NUMBERROW",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=70,
                    Hidden = CheckHidenForUser(),
                },
                new DataGridHelperColumn()
                {
                    Header="№ при размещении",
                    Path="INDEXNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=130,
                    Hidden = CheckHidenForUser(),
                },
                new DataGridHelperColumn()
                {
                    Header="Left",
                    Path="LEFT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=70,
                    Hidden = CheckHidenForUser(),
                },
                new DataGridHelperColumn()
                {
                    Header="Top",
                    Path="TOP",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=70,
                    Hidden = CheckHidenForUser(),
                },
                new DataGridHelperColumn()
                {
                    Header="Повёрнут",
                    Path="ROTATED",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    Width=70,
                    Hidden = CheckHidenForUser(),
                },
                 new DataGridHelperColumn()
                {
                    Header="Транспорт",
                    Path="CAR",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width=70,
                    Hidden = CheckHidenForUser(),
                },
                new DataGridHelperColumn()
                {
                    Header="Ярус",
                    Path="HEIGHTINDEXPLACE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=55,
                    Hidden = CheckHidenForUser(),
                },
                new DataGridHelperColumn()
                {
                    Header="Не полный поддон",
                    Path="INCOMPLET_PALLET",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=55,
                    Hidden = CheckHidenForUser(),
                },
                new DataGridHelperColumn()
                {
                    Header="Размещён в сохранённой схеме",
                    Path="PLACED_BY_SAVED_SCHEME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=55,
                    Hidden = CheckHidenForUser(),
                },
                new DataGridHelperColumn()
                {
                    Header=" ",
                    Path="_",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=5,
                    MaxWidth=900,
                }
            };

            Grid.SetColumns(columns);

            Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                // определение цветов фона строк
                {
                    StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        // Если поддон небыл размещён в сохранённой схеме, то выделяем строку красным цветом
                        if (row.CheckGet("PLACED_BY_SAVED_SCHEME").ToInt() == 0
                            && (FlagBDSheme || FlagDemoSheme))
                        {
                            color = HColor.Red;
                        }

                        //редактируемая строка выделяется синим
                        if(row == CheckedRow)
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

            Grid.SetSorting("RN", ListSortDirection.Ascending);

            //автообновление грида
            Grid.AutoUpdateInterval = 0;
            Grid.Init();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = (Dictionary<string, string> selectedItem) =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);

                    DemonstrationPallet(selectedItem);
                }
            };

            //данные грида
            Grid.OnLoadItems = LoadItems;
            Grid.PrimaryKey = "_ROWNUMBER";

            Grid.OnDblClick = selectedItem =>
            {
                RowDoubleClick(selectedItem);
            };
        }

        public bool CheckHidenForUser()
        {
            bool result = true;

            //if (Central.User.Roles.Count > 0)
            //{
            //    foreach (var item in Central.User.Roles)
            //    {
            //        if (item.Value.Code.Contains("[f]admin"))
            //        {
            //            //result = false;
            //        }
            //    }
            //}

            return result;
        }

        public FormHelper Form { get; set; }
        public void PositionGridInit()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                //new FormHelperField()
                //{
                //    Path="CARCHECKBOX",
                //    FieldType=FormHelperField.FieldTypeRef.Boolean,
                //    Control=CarCheckBox,
                //    ControlType="CheckBox",
                //    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                //    },
                //},
                new FormHelperField()
                {
                    Path="TRUCKRB",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=TruckRadioButton,
                    ControlType="",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TRAILERRB",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=TrailerRadioButton,
                    ControlType="",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="LEFT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=LeftCoordinateTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TOP",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TopCoordinateTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STEP_OF_MOVEMENT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=StepOfMovementCoordinateTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ROTATED",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=Rotated,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                //new FormHelperField()
                //{
                //    Path="CAR",
                //    FieldType=FormHelperField.FieldTypeRef.String,
                //    Control=Car,
                //    ControlType="SelectBox",
                //    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                //    },
                //},

            };

            Form.SetFields(fields);

            PositionGrid.Visibility = Visibility.Hidden;
        }



        /// <summary>
        /// обновление записей
        /// </summary>
        public void LoadItems()
        {
            Grid.ShowSplash();

            GetCoordinates();

            Grid.HideSplash();
        }

        /// <summary>
        /// ИД таблицы LoadingScheme(LOSC_ID)
        /// </summary>
        public int LoadingShemeId { get; set; }

        /// <summary>
        /// Отступ сверху при размещении россыпью
        /// </summary>
        public int offsetHeigth { get; set; }

        /// <summary>
        /// Получение данных
        /// </summary>
        public async void GetCoordinates()
        {
            DisableControls();

            var p = new Dictionary<string, string>();
            p.CheckAdd("ID_TS", ShipmentId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "LoadingTwo");
            q.Request.SetParam("Action", "GetCoordinates");

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
                    if (result["ITEMS"].Rows.Count > 0)
                    {
                        Ds = ListDataSet.Create(result, "ITEMS");

                        foreach (Dictionary<string, string> row in Ds.Items)
                        {
                            row.CheckAdd("INDEXNUMBER", "");
                            row.CheckAdd("PLACED", "");
                            row.CheckAdd("PLACED_BY_SAVED_SCHEME", "");

                            if (row.CheckGet("OBVAZ") == "б/уп")
                            {
                                row.CheckAdd("KOL_PAK", row.CheckGet("PLACEDSTACKSCOUNT"));
                            }
                        }
                        Grid.UpdateItems(Ds);

                        var lsi = ListDataSet.Create(result, "LOSC_ID");
                        LoadingShemeId = lsi.Items.First().CheckGet("LOSC_ID").ToInt();

                        InitScene();

                        SetDefaults();

                        LoadPositions(result);

                        //Если есть не размещённая россыпь, то для таких объектов не будет заданы размеры кубика, в этом случае высчитываем размеры кубика из объёма россыпи
                        {
                            foreach (var item in Ds.Items)
                            {
                                if (item.CheckGet("OBVAZ") == "б/уп")
                                {
                                    if (item.CheckGet("PLACED").ToInt() == 0)
                                    {
                                        item.CheckAdd("HT", (TruckScene.Car.Height - offsetHeigth).ToString());
                                        item.CheckAdd("BT", TruckScene.Car.Width.ToString());

                                        long vol = item.CheckGet("VOLUM").ToInt();
                                        long qty = item.CheckGet("QTY").ToInt();

                                        long volum = vol * qty;

                                        int bt = TruckScene.Car.Width;
                                        int ht = TruckScene.Car.Height - offsetHeigth;

                                        var lt = volum / (bt * ht);

                                        item.CheckAdd("LT", lt.ToString());

                                        item.CheckAdd("SIZE", $"{item.CheckGet("LT")}x{item.CheckGet("BT")}x{item.CheckGet("HT")}");
                                    }
                                }
                            }

                            Grid.UpdateItems(Ds);
                        }

                        CheckGlobalLoadOrder();

                        Show();

                        GetBootOrderStatus();
                        UpdateActions();
                    }
                }
                else if (q.Answer.Type == LPackClientAnswer.AnswerTypeRef.File)
                {
                    Central.OpenFile(q.Answer.DownloadFilePath);
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Получение данных по расположению поддонов
        /// </summary>
        /// <param name="result"></param>
        public void LoadPositions(Dictionary<string, ListDataSet> result)
        {
            if (result["POSITIONS"].Rows.Count > 0)
            {
                var Positions = ListDataSet.Create(result, "POSITIONS");
                foreach (var positionItem in Positions.Items)
                {
                    if (positionItem.CheckGet("PRODUCT_TYPE") == "1")
                    {
                        var row = Ds.Items.FirstOrDefault(
                            x => x.CheckGet("POSITION_ID") == positionItem.CheckGet("IDORDERDATES")
                            && x.CheckGet("HT") == positionItem.CheckGet("HEIGHT")
                            && x.CheckGet("PLACED").ToInt() == 0);

                        if (row != null)
                        {
                            IndexNumber += 1;
                            row.CheckAdd("INDEXNUMBER", IndexNumber.ToString());
                            row.CheckAdd("LEFT", positionItem.CheckGet("OFFSET_LENGTH"));
                            row.CheckAdd("TOP", positionItem.CheckGet("OFFSET_WIDTH"));
                            row.CheckAdd("PLACED", "1");
                            row.CheckAdd("PLACED_BY_SAVED_SCHEME", "1");
                            row.CheckAdd("LOSC_ID", positionItem.CheckGet("LOSC_ID"));
                            row.CheckAdd("OnHigh", positionItem.CheckGet("OFFSET_HEIGHT"));
                            row.CheckAdd("_NUMBER", positionItem.CheckGet("_NUMBER"));

                            if (positionItem.CheckGet("LENGTH").ToInt() == row.CheckGet("LT").ToInt() && positionItem.CheckGet("WIDTH").ToInt() == row.CheckGet("BT").ToInt())
                            {
                                row.CheckAdd("ROTATED", "0");
                            }
                            else
                            {
                                row.CheckAdd("ROTATED", "1");
                            }

                            if (positionItem.CheckGet("OFFSET_HEIGHT") == "0")
                            {
                                row.CheckAdd("HEIGHTINDEXPLACE", "1");
                            }
                            else
                            {
                                var list = Positions.Items.Where(x => x.CheckGet("OFFSET_LENGTH") == positionItem.CheckGet("OFFSET_LENGTH") && x.CheckGet("OFFSET_WIDTH") == positionItem.CheckGet("OFFSET_WIDTH"));

                                int hip = 1;

                                foreach (var li in list)
                                {
                                    if (li.CheckGet("OFFSET_HEIGHT").ToInt() < positionItem.CheckGet("OFFSET_HEIGHT").ToInt())
                                    {
                                        hip += 1;
                                    }
                                }

                                row.CheckAdd("HEIGHTINDEXPLACE", hip.ToString());
                            }

                            if (positionItem.CheckGet("TRAILER_FLAG") == "1")
                            {
                                row.CheckAdd("CAR", "2");
                            }
                            else
                            {
                                row.CheckAdd("CAR", "1");
                            }

                            AddPallet(row);
                        }
                    }
                    else if (positionItem.CheckGet("PRODUCT_TYPE") == "2")
                    {
                        var row = Ds.Items.FirstOrDefault(x => x.CheckGet("POSITION_ID") == positionItem.CheckGet("IDORDERDATES"));

                        if (row != null)
                        {
                            IndexNumber += 1;
                            row.CheckAdd("INDEXNUMBER", IndexNumber.ToString());
                            row.CheckAdd("LEFT", positionItem.CheckGet("OFFSET_LENGTH"));
                            row.CheckAdd("TOP", positionItem.CheckGet("OFFSET_WIDTH"));
                            row.CheckAdd("PLACED", "1");
                            row.CheckAdd("PLACED_BY_SAVED_SCHEME", "1");
                            row.CheckAdd("ROTATED", "0");
                            row.CheckAdd("LOSC_ID", positionItem.CheckGet("LOSC_ID"));
                            row.CheckAdd("OnHigh", positionItem.CheckGet("OFFSET_HEIGHT"));
                            row.CheckAdd("HT", positionItem.CheckGet("HEIGHT"));
                            row.CheckAdd("LT", positionItem.CheckGet("LENGTH"));
                            row.CheckAdd("BT", positionItem.CheckGet("WIDTH"));
                            row.CheckAdd("SIZE", $"{row.CheckGet("LT")}x{row.CheckGet("BT")}x{row.CheckGet("HT")}");
                            row.CheckAdd("_NUMBER", positionItem.CheckGet("_NUMBER"));
                            row.CheckAdd("HEIGHTINDEXPLACE", "1");

                            if (positionItem.CheckGet("TRAILER_FLAG") == "1")
                            {
                                row.CheckAdd("CAR", "2");
                            }
                            else
                            {
                                row.CheckAdd("CAR", "1");
                            }
                           
                            AddPallet(row);
                        }
                    }
                }

                FlagBDSheme = true;

                Grid.UpdateItems(Ds);
            }
        }

        /// <summary>
        /// Попытка получение данных из таблицы Loading_Scheme_Items по текущей отгрузке
        /// </summary>
        public async void TryGetTableLoadingShemeItems()
        {
            DisableControls();
            
            var p = new Dictionary<string, string>();
            p.CheckAdd("LOSC_ID", LoadingShemeId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "LoadingTwo");
            q.Request.SetParam("Action", "GetLoadingShemeItem");

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
                    var TableLoadingShemeItems = ListDataSet.Create(result, "ITEMS");
                    if (TableLoadingShemeItems.Items.Count > 0)
                    {
                        TableLoadingShemeItems.Items = TableLoadingShemeItems.Items.OrderBy(x => x.CheckGet("OFFSET_LENGTH").ToInt()).ThenBy(x => x.CheckGet("OFFSET_WIDTH").ToInt()).ToList();
                        foreach (var positionItem in TableLoadingShemeItems.Items)
                        {
                            if (positionItem.CheckGet("PRODUCT_TYPE") == "1")
                            {
                                var row = Ds.Items.FirstOrDefault(
                                    x => x.CheckGet("POSITION_ID") == positionItem.CheckGet("IDORDERDATES")
                                    && x.CheckGet("HT") == positionItem.CheckGet("HEIGHT")
                                    && x.CheckGet("PLACED").ToInt() == 0);

                                if (row != null)
                                {
                                    IndexNumber += 1;
                                    row.CheckAdd("INDEXNUMBER", IndexNumber.ToString());
                                    row.CheckAdd("LEFT", positionItem.CheckGet("OFFSET_LENGTH"));
                                    row.CheckAdd("TOP", positionItem.CheckGet("OFFSET_WIDTH"));
                                    row.CheckAdd("PLACED", "1");
                                    row.CheckAdd("LOSC_ID", positionItem.CheckGet("LOSC_ID"));
                                    row.CheckAdd("OnHigh", positionItem.CheckGet("OFFSET_HEIGHT"));

                                    if (positionItem.CheckGet("LENGTH").ToInt() == row.CheckGet("LT").ToInt() && positionItem.CheckGet("WIDTH").ToInt() == row.CheckGet("BT").ToInt())
                                    {
                                        row.CheckAdd("ROTATED", "0");
                                    }
                                    else
                                    {
                                        row.CheckAdd("ROTATED", "1");
                                    }

                                    if (positionItem.CheckGet("OFFSET_HEIGHT") == "0")
                                    {
                                        row.CheckAdd("HEIGHTINDEXPLACE", "1");
                                    }
                                    else
                                    {
                                        var list = TableLoadingShemeItems.Items.Where(x => x.CheckGet("OFFSET_LENGTH") == positionItem.CheckGet("OFFSET_LENGTH") && x.CheckGet("OFFSET_WIDTH") == positionItem.CheckGet("OFFSET_WIDTH"));

                                        int hip = 1;

                                        foreach (var li in list)
                                        {
                                            if (li.CheckGet("OFFSET_HEIGHT").ToInt() < positionItem.CheckGet("OFFSET_HEIGHT").ToInt())
                                            {
                                                hip += 1;
                                            }
                                        }

                                        row.CheckAdd("HEIGHTINDEXPLACE", hip.ToString());
                                    }

                                    if (positionItem.CheckGet("TRAILER_FLAG") == "1")
                                    {
                                        row.CheckAdd("CAR", "2");
                                    }
                                    else
                                    {
                                        row.CheckAdd("CAR", "1");
                                    }

                                    AddPallet(row);
                                }
                            }
                            else if (positionItem.CheckGet("PRODUCT_TYPE") == "2")
                            {
                                var row = Ds.Items.FirstOrDefault(x => x.CheckGet("POSITION_ID") == positionItem.CheckGet("IDORDERDATES"));

                                if (row != null)
                                {
                                    IndexNumber += 1;
                                    row.CheckAdd("INDEXNUMBER", IndexNumber.ToString());
                                    row.CheckAdd("LEFT", positionItem.CheckGet("OFFSET_LENGTH"));
                                    row.CheckAdd("TOP", positionItem.CheckGet("OFFSET_WIDTH"));
                                    row.CheckAdd("PLACED", "1");
                                    row.CheckAdd("LOSC_ID", positionItem.CheckGet("LOSC_ID"));
                                    row.CheckAdd("OnHigh", positionItem.CheckGet("OFFSET_HEIGHT"));
                                    row.CheckAdd("ROTATED", "0");
                                    row.CheckAdd("HEIGHTINDEXPLACE", "1");
                                    row.CheckAdd("HT", positionItem.CheckGet("HEIGHT"));
                                    row.CheckAdd("LT", positionItem.CheckGet("LENGTH"));
                                    row.CheckAdd("BT", positionItem.CheckGet("WIDTH"));
                                    row.CheckAdd("SIZE", $"{row.CheckGet("LT")}x{row.CheckGet("BT")}x{row.CheckGet("HT")}");

                                    if (positionItem.CheckGet("TRAILER_FLAG") == "1")
                                    {
                                        row.CheckAdd("CAR", "2");
                                    }
                                    else
                                    {
                                        row.CheckAdd("CAR", "1");
                                    }

                                    AddPallet(row);

                                }
                            }
                        }

                        FlagBDSheme = true;

                        Grid.UpdateItems(Ds);

                        CheckGlobalLoadOrder();
                    }
                    else
                    {
                        string msg = "Для данной отгрузки нет сохранённой схемы погрузки";
                        var d = new DialogWindow($"{msg}", "Загрузка схемы погрузки", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            UpdateActions();
        }

        /// <summary>
        /// Удаление последнего размещённого поддона
        /// </summary>
        public void DeleteLastPallet()
        {
            var scene = new Scene();
            if (SelectedItem.CheckGet("CAR") == "2")
            {
                scene = TrailerScene;
            }
            else
            {
                scene = TruckScene;
            }
            var listElement = scene.ModelList.FirstOrDefault(x => x.Value.ListIndex == IndexNumber);

            foreach (var item in scene.ModelList)
            {
                if (item.Value.Left == listElement.Value.Left && item.Value.Top == listElement.Value.Top && item.Value.Height == listElement.Value.OnHigh && item.Value.HasHead == 1)
                {
                    item.Value.HasHead = 0;
                }
            }

            ClearBorders(listElement);
            scene.ModelList.Remove(listElement.Key);
            scene.ModelListNumberPallet.Remove(listElement.Key);

            scene.PalletQuantity -= 1;

            foreach (var item in Ds.Items)
            {
                if (item == SelectedItem)
                {
                    item.CheckAdd("LEFT", "");
                    item.CheckAdd("TOP", "");
                    item.CheckAdd("CAR", "");
                    item.CheckAdd("INDEXNUMBER", "");
                    item.CheckAdd("ROTATED", "");
                    item.CheckAdd("PLACED", "");
                    item.CheckAdd("HEIGHTINDEXPLACE", "");
                }
            }

            Grid.UpdateItems(Ds);

            if (IndexNumber > 0)
            {
                IndexNumber -= 1;
            }

            scene.Update();
            scene.Render();
        }

        public void DeleteLastPalletAuto()
        {
            int indexNumber = IndexNumber;

            KeyValuePair<string, Box> listElement = new KeyValuePair<string, Box>();

            bool existElement = false;

            var scene = new Scene();

            if (Ds.Items.First().CheckGet("TRAILER_FLAG") == "1")
            {
                if (TrailerScene.ModelList.Count > 0)
                {
                    if (TrailerScene.ModelList.FirstOrDefault(x => x.Value.ListIndex == IndexNumber).Value != null)
                    {
                        listElement = TrailerScene.ModelList.FirstOrDefault(x => x.Value.ListIndex == IndexNumber);

                        existElement = true;

                        scene = TrailerScene;
                    }
                }
            }

            if (existElement == false)
            {
                if (TruckScene.ModelList.Count > 0)
                {
                    if (TruckScene.ModelList.FirstOrDefault(x => x.Value.ListIndex == IndexNumber).Value != null)
                    {
                        listElement = TruckScene.ModelList.FirstOrDefault(x => x.Value.ListIndex == IndexNumber);

                        existElement = true;

                        scene = TruckScene;
                    }
                }
            }

            if (existElement)
            {
                foreach (var item in scene.ModelList)
                {
                    if (item.Value.Left == listElement.Value.Left && item.Value.Top == listElement.Value.Top && item.Value.Height == listElement.Value.OnHigh && item.Value.HasHead == 1)
                    {
                        item.Value.HasHead = 0;
                    }
                }

                ClearBorders(listElement);
                scene.ModelList.Remove(listElement.Key);
                scene.ModelListNumberPallet.Remove(listElement.Key);

                scene.PalletQuantity -= 1;

                foreach (var item in Ds.Items)
                {
                    if (item.CheckGet("INDEXNUMBER") == indexNumber.ToString())
                    {
                        item.CheckAdd("LEFT", "");
                        item.CheckAdd("TOP", "");
                        item.CheckAdd("CAR", "");
                        item.CheckAdd("INDEXNUMBER", "");
                        item.CheckAdd("ROTATED", "");
                        item.CheckAdd("PLACED", "");
                        item.CheckAdd("HEIGHTINDEXPLACE", "");
                    }
                }

                Grid.UpdateItems(Ds);

                if (IndexNumber > 0)
                {
                    IndexNumber -= 1;
                }

                CheckGlobalLoadOrder();

                scene.Update();
                scene.Render();
            }
        }

        /// <summary>
        /// Отображение 3Д модели выбранного поддона
        /// </summary>
        /// <param name="selectedItem"></param>
        public void DemonstrationPallet(Dictionary<string, string> selectedItem)
        {
            KeyValuePair<string, Box> model = new KeyValuePair<string, Box>();
            for (int i = PalletDemonstrationScene.ModelList.Count - 1; i >= 0; i--)
            {
                model = PalletDemonstrationScene.ModelList.ElementAt(i);
                if (model.Value.NumberProduct > 0)
                {
                    foreach (var border in model.Value.Boxes)
                    {
                        var b = PalletDemonstrationScene.ModelList.FirstOrDefault(x => x.Value == border);
                        PalletDemonstrationScene.ModelList.Remove(b.Key);
                    }

                    PalletDemonstrationScene.ModelList.Remove(model.Key);
                    PalletDemonstrationScene.ModelListNumberPallet.Clear();
                }
            }

            var box = new Box();
            box.PositionItemData = selectedItem;
            box.Left = 0;
            box.Top = 0;
            box.Width = selectedItem.CheckGet("BT").ToInt();
            box.Length = selectedItem.CheckGet("LT").ToInt();
            box.Height = selectedItem.CheckGet("HT").ToInt();
            box.BoxRotated = selectedItem.CheckGet("ROTATED").ToInt();
            box.NumberProduct = selectedItem.CheckGet("RN").ToInt();
            box.ListIndex = 1;

            if (selectedItem.CheckGet("OBVAZ") == "б/уп")
            {
                box.CollorInner = CollorDictionary.CheckGet("-1");
            }
            else
            {
                if (box.NumberProduct > 0)
                {
                    box.CollorInner = GetColor(box.NumberProduct.ToString());
                }
                else
                {
                    box.CollorInner = CollorDictionary.CheckGet("0");
                }
            }

            box.Init();

            PalletDemonstrationScene.AddModel(box);

            box.AddBorders(box.Width, box.Height, box.Length, box.Left, box.Top, box, box.OnHigh);
            foreach (var borderBox in box.Boxes)
            {
                PalletDemonstrationScene.AddModel(borderBox);
            }

            PalletDemonstrationScene.Update();
            PalletDemonstrationScene.Render();
        }

        public void DisableControls()
        {
            ShipmentsGridToolbar.IsEnabled = false;
            GridToolbar.IsEnabled = false;
        }

        public void UpdateActions()
        {
            // Если в данный момент мы размещаем какой-то поддон
            if (CheckedRow != null && CheckedRow.Count > 0)
            {
                GridToolbar.IsEnabled = true;

                ShipmentsGridToolbar.IsEnabled = false;

                DeletePalleteButton.IsEnabled = false;
                ClearButton.IsEnabled = false;
                TestShemeButton.IsEnabled = false;
            }
            else
            {
                GridToolbar.IsEnabled = true;

                ShipmentsGridToolbar.IsEnabled = true;
                GetShemeButton.IsEnabled = true;
                DemoShemeOne.IsEnabled = true;

                // Если есть размещённые поддоны
                if (Ds.Items.Count(x => x.CheckGet("PLACED").ToInt() > 0) > 0)
                {
                    DeletePalleteButton.IsEnabled = true;
                    ClearButton.IsEnabled = true;
                    TestShemeButton.IsEnabled = true;

                    SaveShemeButton.IsEnabled = true;
                }
                else
                {
                    DeletePalleteButton.IsEnabled = false;
                    ClearButton.IsEnabled = false;
                    TestShemeButton.IsEnabled = false;

                    SaveShemeButton.IsEnabled = false;
                }

                OKButton.IsEnabled = false;
            }

            ProcessPermissions();
        }

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem"></param>
        public void UpdateActions(Dictionary<string, string> selectedItem)
        {
            if (selectedItem.Count > 0)
            {
                SelectedItem = selectedItem;
                if (selectedItem.CheckGet("PLACED") == "1")
                {
                    PlaceButton.IsEnabled = false;
                }
                else
                {
                    if (selectedItem.CheckGet("LOAD_ORDER").ToInt() == GlobalLoadOrder)
                    {
                        PlaceButton.IsEnabled = true;
                        FormStatus.Text = "";
                    }
                    else
                    {
                        PlaceButton.IsEnabled = true;
                        FormStatus.Text = $"Внимание!{Environment.NewLine}Не все позиции из предыдущей точки выгрузки размещены.";
                    }

                }
            }

            ProcessPermissions();
        }

        /// <summary>
        /// Вызов справки
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/shipments/control/listing/porjadok-zagruzki");
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("ShipmentControl") > -1)
            {
                if (m.ReceiverName.IndexOf("ShipmentShemeTwo") > -1)
                {
                    switch (m.Action)
                    {
                        case "Save":
                            rowMessage = (Dictionary<string, string>)m.ContextObject;
                            UpdateScene(rowMessage);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Обновление параметров просмотра сцены (угол обзора, освещённость)
        /// </summary>
        /// <param name="rowMessage"></param>
        public void UpdateScene(Dictionary<string, string> rowMessage)
        {
            TruckScene.Angle1 = rowMessage.CheckGet("ANGLE1").ToInt();
            TruckScene.Angle2 = rowMessage.CheckGet("ANGLE2").ToInt();
            TruckScene.Distance = rowMessage.CheckGet("DISTANCE").ToInt();
            TruckScene.Light1Intensity = rowMessage.CheckGet("L1").ToInt();
            TruckScene.Light2Intensity = rowMessage.CheckGet("L2").ToInt();

            TruckScene.Update();
            TruckScene.Render();

            TrailerScene.Angle1 = rowMessage.CheckGet("ANGLE1").ToInt();
            TrailerScene.Angle2 = rowMessage.CheckGet("ANGLE2").ToInt();
            TrailerScene.Distance = rowMessage.CheckGet("DISTANCE").ToInt();
            TrailerScene.Light1Intensity = rowMessage.CheckGet("L1").ToInt();
            TrailerScene.Light2Intensity = rowMessage.CheckGet("L2").ToInt();

            TrailerScene.Update();
            TrailerScene.Render();
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessKeyboard(object sender, System.Windows.Input.KeyEventArgs e)
        {
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
            }

            TruckScene.ProcessKeyboard(e);
            TrailerScene.ProcessKeyboard(e);
            PalletDemonstrationScene.ProcessKeyboard(e);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Show()
        {
            string title = $"#{ShipmentId}";
            if (ShipmentId == 0)
            {
                title = "Отгрузка";
            }
            this.FrameName = $"shipmenthistory_{ShipmentId}";
            Central.WM.AddTab(FrameName, title, true, "add", this);

        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        public void Close()
        {
            Central.WM.RemoveTab($"shipmenthistory_{ShipmentId}");
            if (ReturnTabName == "add")
            {
                Central.WM.SetLayer("add");
                ReturnTabName = "";
            }

            Destroy();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку отмены
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Номер поддона при размещении
        /// </summary>
        public int IndexNumber { get; set; }

        /// <summary>
        /// Получение максимального значения indexNumber,
        /// Нахождение наибольшего порядкового номера среди размещённых поддонов,
        /// (indexNumber = MaxindexNumber)
        /// </summary>
        public void CheckMaxIndexNumber()
        {
            IndexNumber = 0;
            int kolIndexes = 0;

            foreach (var item in Ds.Items)
            {
                if (item.CheckGet("INDEXNUMBER").ToInt() > IndexNumber)
                {
                    IndexNumber = item.CheckGet("INDEXNUMBER").ToInt();
                }

                kolIndexes = kolIndexes + item.CheckGet("INDEXNUMBER").ToInt();

            }

            if (kolIndexes == 0)
            {
                IndexNumber = 0;
            }
        }

        /// <summary>
        /// Флаг того, что схема построена с помощью старого алгоритма
        /// Если true, то ручное размещение запрещено
        /// </summary>
        public bool FlagDemoSheme { get; set; }

        /// <summary>
        /// Флаг того, что схема построена с помощью данных из БД
        /// Если true, то ручное размещение запрещено
        /// </summary>
        public bool FlagBDSheme { get; set; }

        public Dictionary<string, int> ArticNumDictionnary { get; set; }

        public int CheckArticulNumber(string articul)
        {
            int num;

            if (ArticNumDictionnary.ContainsKey(articul))
            {
                num = ArticNumDictionnary[articul];
            }
            else
            {
                int n = 0;
                foreach (var item in ArticNumDictionnary)
                {
                    if (item.Value > n)
                    {
                        n = item.Value;
                    }
                }

                num = n + 1;

                ArticNumDictionnary.Add(articul, num);
            }

            return num;
        }

        public void RowDoubleClick(Dictionary<string, string> selectedItem)
        {
            if (selectedItem.CheckGet("PLACED") == "1" || PlaceButton.IsEnabled == false)
            {

            }
            else
            {
                Place();
            }
        }

        public void Place()
        {
            if (CheckedRow != null && CheckedRow.Count > 0)
            {
                CancelPlace();
            }

            LeftCoordinateTextBox.Clear();
            TopCoordinateTextBox.Clear();
            Rotated.IsChecked = false;

            foreach (var item in Ds.Items)
            {
                if (item.CheckGet("PLACED") != "1")
                {
                    item.CheckAdd("LEFT", "");
                    item.CheckAdd("TOP", "");
                    item.CheckAdd("CAR", "");
                    item.CheckAdd("INDEXNUMBER", "");
                    item.CheckAdd("ROTATED", "");
                    item.CheckAdd("PLACED", "");
                    item.CheckAdd("HEIGHTINDEXPLACE", "");
                }
            }

            CheckMaxIndexNumber();

            if (TruckScene.ModelList.FirstOrDefault(x => x.Value.ListIndex == IndexNumber + 1).Key != null)
            {
                var list = TruckScene.ModelList.FirstOrDefault(x => x.Value.ListIndex == IndexNumber + 1);

                ClearBorders(list);
                TruckScene.ModelList.Remove(list.Key);
                TruckScene.ModelListNumberPallet.Remove(list.Key);

                TruckScene.Update();
                TruckScene.Render();
            }

            if (TrailerScene.ModelList.FirstOrDefault(x => x.Value.ListIndex == IndexNumber + 1).Key != null)
            {
                var list = TrailerScene.ModelList.FirstOrDefault(x => x.Value.ListIndex == IndexNumber + 1);

                ClearBorders(list);
                TrailerScene.ModelList.Remove(list.Key);
                TrailerScene.ModelListNumberPallet.Remove(list.Key);

                TrailerScene.Update();
                TrailerScene.Render();
            }

            foreach (Dictionary<string, string> row in Ds.Items)
            {
                if (row == SelectedItem && row.CheckGet("PLACED") != "1")
                {
                    IndexNumber += 1;

                    var Artikul = row.CheckGet("ARTIKUL").ToString();
                    if (Artikul != null)
                    {
                        PositionGrid.Visibility = Visibility.Visible;

                        CheckedRow = row;

                        string c = CarForPlaced.ToString();

                        SetUIPoints(c);
                    }

                }
            }
            Grid.UpdateItems(Ds);

            UpdateActions();
        }

        private void PlaceButton_Click(object sender, RoutedEventArgs e)
        {
            Place();
        }

        //FIXME будет работать, только есть существует поддон с Top == 0
        public void SetUIPoints(string carid)
        {
            Scene currentScene = new Scene();
            if (carid == "1")
            {
                currentScene = TruckScene;
            }
            else if (carid == "2")
            {
                currentScene = TrailerScene;
            }

            currentScene.UIPointContainer.Children.Clear();
            currentScene.UIPointBoxList.Clear();

            // Список размещённых поддонов
            List<KeyValuePair<string, Box>> PlacedPallets = new List<KeyValuePair<string, Box>>();
            //получаем список всех расположенных поддонов
            foreach (var item in currentScene.ModelList)
            {
                if (item.Value.ListIndex > 0)
                {
                    PlacedPallets.Add(item);
                }
            }

            // Если нет ни одного размещённого поддона
            if (PlacedPallets.Count == 0)
            {
                int palletWidth = CheckedRow.CheckGet("BT").ToInt();
                int palletLength = CheckedRow.CheckGet("LT").ToInt();

                // Если поддон можно паставить в транспорт
                if (currentScene.Car.Width >= palletWidth && currentScene.Car.Length >= palletLength
                || currentScene.Car.Length >= palletWidth && currentScene.Car.Width >= palletLength)
                {
                    ModelUIElement3D modelUIElement3D = new ModelUIElement3D();
                    GeometryModel3D geometryModel3D = new GeometryModel3D();

                    var point = new Box();
                    point.Left = 0;
                    point.Top = 0;
                    point.Length = 250;
                    point.Width = 250;
                    point.Height = 250;
                    point.CollorInner = "#ff0000";

                    if (CheckedRow.CheckGet("OBVAZ") == "б/уп")
                    {
                        point.PointRotationOption = 0;
                    }
                    else
                    {
                        if (currentScene.Car.Width >= palletWidth && currentScene.Car.Length >= palletLength
                            && currentScene.Car.Length >= palletWidth && currentScene.Car.Width >= palletLength)
                        {

                            point.PointRotationOption = 2;
                        }
                        else
                        {
                            if (currentScene.Car.Length >= palletWidth && currentScene.Car.Width >= palletLength)
                            {

                                point.PointRotationOption = 1;
                            }
                            else
                            {

                                point.PointRotationOption = 0;
                            }
                        }
                    }

                    MeshGeometry3D mesh = point.CreateMesh();
                    var color = point.CollorOuter;
                    var brush = color.ToBrush();
                    var material = new DiffuseMaterial(brush);

                    geometryModel3D.Geometry = mesh;
                    geometryModel3D.Material = material;

                    modelUIElement3D.Model = geometryModel3D;
                    modelUIElement3D.MouseDown += UIPoint_MouseDown;

                    point.BoxModelUIElement3D = modelUIElement3D;
                    currentScene.UIPointBoxList.Add(point);
                    currentScene.UIPointContainer.Children.Add(modelUIElement3D);
                }
            }
            // Если есть размещённые поддоны
            else
            {
                // Поддон, с которого начинается вычисление свободных пространств. Самый правый поддон, прижатый к крайней верхней точке транспорта
                KeyValuePair<string, Box> startPallet = PlacedPallets.Where(x => x.Value.Top == 0).OrderByDescending(x => x.Value.Left + x.Value.Length).First();
                if (startPallet.Value != null)
                {
                    // Список свободных пространств
                    List<Box> Places = new List<Box>();

                    // Нижняя точка пространства
                    int width = 0;
                    var widthAdjustmentPallets = PlacedPallets.Where(x => x.Value.Top > startPallet.Value.Top && x.Value.Top < (startPallet.Value.Top + startPallet.Value.Width) && x.Value.Left >= (startPallet.Value.Left + startPallet.Value.Length)).ToList();
                    if (widthAdjustmentPallets != null && widthAdjustmentPallets.Count > 0)
                    {
                        width = widthAdjustmentPallets.Min(x => x.Value.Top) - startPallet.Value.Top;
                    }
                    else
                    {
                        width = startPallet.Value.Width;
                    }

                    // Первое свободное пространство
                    Box startPlace = new Box();
                    startPlace.Left = startPallet.Value.Left + startPallet.Value.Length;
                    startPlace.Length = currentScene.Car.Length - startPlace.Left;
                    startPlace.Top = startPallet.Value.Top;
                    startPlace.Width = width;
                    Places.Add(startPlace);

                    // Пространство, с которым сейчас работаем
                    Box currentPlace = startPlace;

                    List<KeyValuePair<string, Box>> nextPalletList = new List<KeyValuePair<string, Box>>();
                    KeyValuePair<string, Box> nextPallet = new KeyValuePair<string, Box>();

                    for (int i = 0; i <= Grid.Items.Count; i++)
                    {
                        nextPalletList = new List<KeyValuePair<string, Box>>();
                        nextPallet = new KeyValuePair<string, Box>();

                        // Если нижняя точка текущего свободного прастранства это нижняя грань транспорта, то прекращаем работу цикла
                        if ((currentPlace.Top + currentPlace.Width) == currentScene.Car.Width)
                        {
                            break;
                        }
                        else
                        {
                            {
                                //// Ищем поддон, верхняя точка размещения которого граничит с нижней точкой текущего свободного пространства
                                //nextPalletList = PlacedPallets.Where(x => x.Value.Top == (currentPlace.Top + currentPlace.Width)).ToList();
                                //// Если такой поддон есть, то работаем с ним
                                //if (nextPalletList.Count > 0)
                                //{
                                //    // Ищем самый верхний и самый правый поддон, верхняя точка размещения которого граничит с нижней точкой текущего свободного пространства
                                //    nextPallet = nextPalletList.OrderBy(x => x.Value.Top).ThenByDescending(x => x.Value.Left + x.Value.Length).First();

                                //    // Если крайняя правая точка найденного поддона граничит с левой точкой начала размещения текущего свободного пространства,
                                //    // то продлеваем текущее свободное пространство по ширине на ширину найденного поддона
                                //    if (nextPallet.Value.Left + nextPallet.Value.Length == currentPlace.Left)
                                //    {
                                //        // Нижняя точка пространства
                                //        width = 0;
                                //        widthAdjustmentPallets = PlacedPallets.Where(x => x.Value.Top > nextPallet.Value.Top && x.Value.Top < (nextPallet.Value.Top + nextPallet.Value.Width) && x.Value.Left >= (nextPallet.Value.Left + nextPallet.Value.Length)).ToList();
                                //        if (widthAdjustmentPallets != null && widthAdjustmentPallets.Count > 0)
                                //        {
                                //            width = widthAdjustmentPallets.Min(x => x.Value.Top) - nextPallet.Value.Top;
                                //        }
                                //        else
                                //        {
                                //            width = nextPallet.Value.Width;
                                //        }

                                //        currentPlace.Width = currentPlace.Width + width;
                                //    }
                                //    // Если крайняя правая точка найденного поддона не граничит с левой точкой начала размещения текущего свободного пространства,
                                //    // то создаём новое свободное пространство от найденного поддона и выбираем его текущим свободным пространством
                                //    else
                                //    {
                                //        // Нижняя точка пространства
                                //        width = 0;
                                //        widthAdjustmentPallets = PlacedPallets.Where(x => x.Value.Top > nextPallet.Value.Top && x.Value.Top < (nextPallet.Value.Top + nextPallet.Value.Width) && x.Value.Left >= (nextPallet.Value.Left + nextPallet.Value.Length)).ToList();
                                //        if (widthAdjustmentPallets != null && widthAdjustmentPallets.Count > 0)
                                //        {
                                //            width = widthAdjustmentPallets.Min(x => x.Value.Top) - nextPallet.Value.Top;
                                //        }
                                //        else
                                //        {
                                //            width = nextPallet.Value.Width;
                                //        }

                                //        Box nextPlace = new Box();
                                //        nextPlace.Left = nextPallet.Value.Left + nextPallet.Value.Length;
                                //        nextPlace.Length = currentScene.Car.Length - nextPlace.Left;
                                //        nextPlace.Top = nextPallet.Value.Top;
                                //        nextPlace.Width = width;
                                //        Places.Add(nextPlace);

                                //        currentPlace = nextPlace;
                                //    }
                                //}
                                //// Если такого поддона нет, то пытаемся определить новое свободное пространство (либо от поддона, либо от левого края транспорта)
                                //else
                                //{
                                //    // Ищем поддон, который пересекает нижнюю точку текущего свободного пространства
                                //    nextPalletList = PlacedPallets.Where(x => x.Value.Top < (currentPlace.Top + currentPlace.Width) && (x.Value.Top + x.Value.Width) > (currentPlace.Top + currentPlace.Width)).ToList();
                                //    // Если такой поддон есть, то создаём новое свободное пространство от найденного поддона и выбираем его текущим свободным пространством
                                //    if (nextPalletList.Count > 0)
                                //    {
                                //        //nextPallet = nextPalletList.OrderBy(x => x.Value.Top).ThenByDescending(x => x.Value.Left + x.Value.Length).First();
                                //        nextPallet = nextPalletList.OrderByDescending(x => x.Value.Left + x.Value.Length).First();

                                //        Box nextPlace = new Box();
                                //        nextPlace.Left = nextPallet.Value.Left + nextPallet.Value.Length;
                                //        nextPlace.Length = currentScene.Car.Length - nextPlace.Left;
                                //        nextPlace.Top = currentPlace.Top + currentPlace.Width;

                                //        // Нижняя точка пространства
                                //        width = 0;
                                //        widthAdjustmentPallets = PlacedPallets.Where(x => x.Value.Top > nextPlace.Top && x.Value.Top < (nextPallet.Value.Top + nextPallet.Value.Width) && x.Value.Left >= (nextPallet.Value.Left + nextPallet.Value.Length)).ToList();
                                //        if (widthAdjustmentPallets != null && widthAdjustmentPallets.Count > 0)
                                //        {
                                //            width = widthAdjustmentPallets.Min(x => x.Value.Top) - nextPlace.Top;
                                //        }
                                //        else
                                //        {
                                //            width = nextPallet.Value.Top + nextPallet.Value.Width - nextPlace.Top;
                                //        }
                                //        nextPlace.Width = width;

                                //        Places.Add(nextPlace);

                                //        currentPlace = nextPlace;
                                //    }
                                //    // Если такого поддона нет, то создаём новое свободное пространство от левого края транспорта
                                //    else
                                //    {
                                //        // Ищем поддоны, которые начинаются ниже нижней точки текущего свободного пространства
                                //        nextPalletList = PlacedPallets.Where(x => x.Value.Top > (currentPlace.Top + currentPlace.Width)).ToList();
                                //        // Если ниже есть ещё поддоны, то ширина нового пространства до нижних поддонов
                                //        if (nextPalletList.Count > 0)
                                //        {
                                //            nextPallet = nextPalletList.OrderBy(x => x.Value.Top).First();

                                //            Box nextPlace = new Box();
                                //            nextPlace.Left = 0;
                                //            nextPlace.Length = currentScene.Car.Length;
                                //            nextPlace.Top = currentPlace.Top + currentPlace.Width;
                                //            nextPlace.Width = nextPallet.Value.Top - nextPlace.Top;
                                //            Places.Add(nextPlace);

                                //            currentPlace = nextPlace;
                                //        }
                                //        // Если ниже нет поддонов, то ширина нового пространства до конца фуры и выходим из цикла
                                //        else
                                //        {
                                //            Box nextPlace = new Box();
                                //            nextPlace.Left = 0;
                                //            nextPlace.Length = currentScene.Car.Length;
                                //            nextPlace.Top = currentPlace.Top + currentPlace.Width;
                                //            nextPlace.Width = currentScene.Car.Width - nextPlace.Top;
                                //            Places.Add(nextPlace);

                                //            currentPlace = nextPlace;

                                //            break;
                                //        }
                                //    }
                                //}
                            }

                            // Новый вариант
                            {
                                // Ищем поддон, который пересекает нижнюю точку текущего свободного пространства
                                nextPalletList = PlacedPallets.Where(x => x.Value.Top <= (currentPlace.Top + currentPlace.Width) && (x.Value.Top + x.Value.Width) > (currentPlace.Top + currentPlace.Width)).ToList();
                                // Если такой поддон есть, то создаём новое свободное пространство от найденного поддона и выбираем его текущим свободным пространством
                                if (nextPalletList.Count > 0)
                                {
                                    // Получаем самый правый поддон из списка
                                    nextPallet = nextPalletList.OrderByDescending(x => x.Value.Left + x.Value.Length).First();

                                    // Если это поддон, верхняя точка размещения которого граничит с нижней точкой текущего свободного пространства
                                    if (nextPallet.Value.Top == (currentPlace.Top + currentPlace.Width))
                                    {
                                        // Если крайняя правая точка найденного поддона граничит с левой точкой начала размещения текущего свободного пространства,
                                        // то продлеваем текущее свободное пространство по ширине на ширину найденного поддона
                                        if (nextPallet.Value.Left + nextPallet.Value.Length == currentPlace.Left)
                                        {
                                            // Нижняя точка пространства
                                            width = 0;
                                            widthAdjustmentPallets = PlacedPallets.Where(x => x.Value.Top > nextPallet.Value.Top && x.Value.Top < (nextPallet.Value.Top + nextPallet.Value.Width) && x.Value.Left >= (nextPallet.Value.Left + nextPallet.Value.Length)).ToList();
                                            if (widthAdjustmentPallets != null && widthAdjustmentPallets.Count > 0)
                                            {
                                                width = widthAdjustmentPallets.Min(x => x.Value.Top) - nextPallet.Value.Top;
                                            }
                                            else
                                            {
                                                width = nextPallet.Value.Width;
                                            }

                                            currentPlace.Width = currentPlace.Width + width;
                                        }
                                        // Если крайняя правая точка найденного поддона не граничит с левой точкой начала размещения текущего свободного пространства,
                                        // то создаём новое свободное пространство от найденного поддона и выбираем его текущим свободным пространством
                                        else
                                        {
                                            // Нижняя точка пространства
                                            width = 0;
                                            widthAdjustmentPallets = PlacedPallets.Where(x => x.Value.Top > nextPallet.Value.Top && x.Value.Top < (nextPallet.Value.Top + nextPallet.Value.Width) && x.Value.Left >= (nextPallet.Value.Left + nextPallet.Value.Length)).ToList();
                                            if (widthAdjustmentPallets != null && widthAdjustmentPallets.Count > 0)
                                            {
                                                width = widthAdjustmentPallets.Min(x => x.Value.Top) - nextPallet.Value.Top;
                                            }
                                            else
                                            {
                                                width = nextPallet.Value.Width;
                                            }

                                            Box nextPlace = new Box();
                                            nextPlace.Left = nextPallet.Value.Left + nextPallet.Value.Length;
                                            nextPlace.Length = currentScene.Car.Length - nextPlace.Left;
                                            nextPlace.Top = nextPallet.Value.Top;
                                            nextPlace.Width = width;
                                            Places.Add(nextPlace);

                                            currentPlace = nextPlace;
                                        }
                                    }
                                    else
                                    {
                                        Box nextPlace = new Box();
                                        nextPlace.Left = nextPallet.Value.Left + nextPallet.Value.Length;
                                        nextPlace.Length = currentScene.Car.Length - nextPlace.Left;
                                        nextPlace.Top = currentPlace.Top + currentPlace.Width;

                                        // Нижняя точка пространства
                                        width = 0;
                                        widthAdjustmentPallets = PlacedPallets.Where(x => x.Value.Top > nextPlace.Top && x.Value.Top < (nextPallet.Value.Top + nextPallet.Value.Width) && x.Value.Left >= (nextPallet.Value.Left + nextPallet.Value.Length)).ToList();
                                        if (widthAdjustmentPallets != null && widthAdjustmentPallets.Count > 0)
                                        {
                                            width = widthAdjustmentPallets.Min(x => x.Value.Top) - nextPlace.Top;
                                        }
                                        else
                                        {
                                            width = nextPallet.Value.Top + nextPallet.Value.Width - nextPlace.Top;
                                        }
                                        nextPlace.Width = width;

                                        Places.Add(nextPlace);

                                        currentPlace = nextPlace;
                                    }
                                }
                                // Если такого поддона нет, то создаём новое свободное пространство от левого края транспорта
                                else
                                {
                                    // Ищем поддоны, которые начинаются ниже нижней точки текущего свободного пространства
                                    nextPalletList = PlacedPallets.Where(x => x.Value.Top > (currentPlace.Top + currentPlace.Width)).ToList();
                                    // Если ниже есть ещё поддоны, то ширина нового пространства до нижних поддонов
                                    if (nextPalletList.Count > 0)
                                    {
                                        nextPallet = nextPalletList.OrderBy(x => x.Value.Top).First();

                                        Box nextPlace = new Box();
                                        nextPlace.Left = 0;
                                        nextPlace.Length = currentScene.Car.Length;
                                        nextPlace.Top = currentPlace.Top + currentPlace.Width;
                                        nextPlace.Width = nextPallet.Value.Top - nextPlace.Top;
                                        Places.Add(nextPlace);

                                        currentPlace = nextPlace;
                                    }
                                    // Если ниже нет поддонов, то ширина нового пространства до конца фуры и выходим из цикла
                                    else
                                    {
                                        Box nextPlace = new Box();
                                        nextPlace.Left = 0;
                                        nextPlace.Length = currentScene.Car.Length;
                                        nextPlace.Top = currentPlace.Top + currentPlace.Width;
                                        nextPlace.Width = currentScene.Car.Width - nextPlace.Top;
                                        Places.Add(nextPlace);

                                        currentPlace = nextPlace;

                                        break;
                                    }
                                }
                            }
                        }
                    }

                    // Если есть свободные пространства
                    if (Places.Count > 0)
                    {
                        int palletWidth = CheckedRow.CheckGet("BT").ToInt();
                        int palletLength = CheckedRow.CheckGet("LT").ToInt();

                        //List<Box> nearestPlaces = new List<Box>();
                        //Box nearestPlace = new Box();

                        // Старый вариант
                        {
                            //for (int i = 0; i < Places.Count; i++)
                            //{
                            //    nearestPlaces = new List<Box>();
                            //    nearestPlace = new Box();

                            //    var place = Places[i];
                            //    if (place != null)
                            //    {
                            //        // Если пространство не может вместить в себя текущий поддон, то объединяем это пространство с другим
                            //        if (!(place.Width >= palletWidth && place.Length >= palletLength
                            //            || place.Length >= palletWidth && place.Width >= palletLength))
                            //        {
                            //            nearestPlaces = Places.Where(x => (x.Top == place.Top + place.Width || x.Top + x.Width == place.Top) && x.Left >= place.Left).ToList();
                            //            if (nearestPlaces.Count > 0)
                            //            {
                            //                nearestPlace = nearestPlaces.First();
                            //            }
                            //            else
                            //            {
                            //                nearestPlaces = Places.Where(x => x.Top == place.Top + place.Width || x.Top + x.Width == place.Top).ToList();
                            //                if (nearestPlaces.Count > 0)
                            //                {
                            //                    nearestPlace = nearestPlaces.OrderBy(x => Math.Abs(x.Left - place.Left)).First();
                            //                }
                            //                else
                            //                {
                            //                    nearestPlace = null;
                            //                }
                            //            }

                            //            if (nearestPlace != null)
                            //            {
                            //                nearestPlace.Width = nearestPlace.Width + place.Width;
                            //                // Если найденное свободное пространство ниже текущего, то меняяем его верхнюю точку начала
                            //                if (nearestPlace.Top == place.Top + place.Width)
                            //                {
                            //                    nearestPlace.Top = place.Top;
                            //                }

                            //                // Если левая точка текущего свободного пространства находится правее левой точки найденного свободного пространства,
                            //                // то двигаем найденное свободное пространство до левой точки текущего
                            //                if (place.Left > nearestPlace.Left)
                            //                {
                            //                    nearestPlace.Length = nearestPlace.Length - (place.Left - nearestPlace.Left);
                            //                    nearestPlace.Left = place.Left;
                            //                }
                            //            }

                            //            Places.Remove(place);
                            //            i--;
                            //        }
                            //    }
                            //}
                        }

                        // Новый вариант
                        // Объединяем пространства, которые не могут вместить в себя размещаемый поддон
                        {
                            //Places = Places.OrderBy(x => x.Top).ToList();
                            //for (int i = 0; i < Places.Count; i++)
                            //{
                            //    nearestPlace = new Box();

                            //    var place = Places[i];
                            //    if (place != null)
                            //    {
                            //        // Если пространство не может вместить в себя текущий поддон, то объединяем это пространство с другим
                            //        if (!((place.Width >= palletWidth && place.Length >= palletLength)
                            //            || (place.Length >= palletWidth && place.Width >= palletLength)))
                            //        {
                            //            // Находим свободное пространство под этим пространством
                            //            nearestPlace = Places.FirstOrDefault(x => x.Top == place.Top + place.Width);
                            //            if (nearestPlace == null)
                            //            {
                            //                // Находим свободное пространство над этим пространством
                            //                nearestPlace = Places.FirstOrDefault(x => x.Top + x.Width == place.Top);
                            //            }

                            //            if (nearestPlace != null)
                            //            {
                            //                nearestPlace.Width = nearestPlace.Width + place.Width;
                            //                // Если найденное свободное пространство ниже текущего, то меняяем его верхнюю точку начала
                            //                if (nearestPlace.Top == place.Top + place.Width)
                            //                {
                            //                    nearestPlace.Top = place.Top;
                            //                }

                            //                // Если левая точка текущего свободного пространства находится правее левой точки найденного свободного пространства,
                            //                // то двигаем найденное свободное пространство до левой точки текущего
                            //                if (place.Left > nearestPlace.Left)
                            //                {
                            //                    nearestPlace.Length = nearestPlace.Length - (place.Left - nearestPlace.Left);
                            //                    nearestPlace.Left = place.Left;
                            //                }
                            //            }

                            //            Places.Remove(place);
                            //            i--;
                            //        }
                            //    }
                            //}

                            //nearestPlace = new Box();
                        }

                        // Новый вариант2
                        // Объединяем пространства, которые не могут вместить в себя размещаемый поддон
                        {
                            //Places = Places.OrderBy(x => x.Top).ToList();

                            //// Сохраняем список пространств, которые могут вместить в себя размещаемый поддон
                            //var independentPlaces = new List<Box>();
                            //for (int i = 0; i < Places.Count; i++)
                            //{
                            //    var place = Places[i];
                            //    if (place != null)
                            //    {
                            //        if ((place.Width >= palletWidth && place.Length >= palletLength)
                            //            || (place.Length >= palletWidth && place.Width >= palletLength))
                            //        {
                            //            independentPlaces.Add(new Box() { Left = place.Left, Length = place.Length, Top = place.Top, Width = place.Width });
                            //        }
                            //    }
                            //}

                            //// Объединяем пространства, которые не могут вместить в себя размещаемый поддон
                            //for (int i = 0; i < Places.Count; i++)
                            //{
                            //    nearestPlace = new Box();

                            //    var place = Places[i];
                            //    if (place != null)
                            //    {
                            //        // Если пространство не может вместить в себя текущий поддон, то объединяем это пространство с другим
                            //        if (!((place.Width >= palletWidth && place.Length >= palletLength)
                            //            || (place.Length >= palletWidth && place.Width >= palletLength)))
                            //        {
                            //            // Находим свободное пространство под этим пространством
                            //            nearestPlace = Places.FirstOrDefault(x => x.Top == place.Top + place.Width);
                            //            if (nearestPlace == null)
                            //            {
                            //                // Находим свободное пространство над этим пространством
                            //                nearestPlace = Places.FirstOrDefault(x => x.Top + x.Width == place.Top);
                            //            }

                            //            if (nearestPlace != null)
                            //            {
                            //                nearestPlace.Width = nearestPlace.Width + place.Width;
                            //                // Если найденное свободное пространство ниже текущего, то меняяем его верхнюю точку начала
                            //                if (nearestPlace.Top == place.Top + place.Width)
                            //                {
                            //                    nearestPlace.Top = place.Top;
                            //                }

                            //                // Если левая точка текущего свободного пространства находится правее левой точки найденного свободного пространства,
                            //                // то двигаем найденное свободное пространство до левой точки текущего
                            //                if (place.Left > nearestPlace.Left)
                            //                {
                            //                    nearestPlace.Length = nearestPlace.Length - (place.Left - nearestPlace.Left);
                            //                    nearestPlace.Left = place.Left;
                            //                }
                            //            }

                            //            Places.Remove(place);
                            //            i--;
                            //        }
                            //    }
                            //}

                            //nearestPlace = new Box();

                            //// Удаляем пространства, которые всё ещё не могут вместить в себя поддон
                            //for (int i = 0; i < Places.Count; i++)
                            //{
                            //    var place = Places[i];
                            //    if (place != null)
                            //    {
                            //        // Если пространство не может вместить в себя текущий поддон, то удаляем это пространство из списка свободных продаж
                            //        if (!((place.Width >= palletWidth && place.Length >= palletLength)
                            //            || (place.Length >= palletWidth && place.Width >= palletLength)))
                            //        {
                            //            Places.Remove(place);
                            //            i--;
                            //        }
                            //    }
                            //}

                            //// Добавляем пространства, которые могут вместить в себя размещаемый поддон,
                            //// к списку пространств, полученных после объединения пространств, которые не могут вместить в себя размещаемый поддон
                            //if (independentPlaces != null && independentPlaces.Count > 0)
                            //{
                            //    foreach (var independentPlace in independentPlaces)
                            //    {
                            //        Places.Add(independentPlace);
                            //    }
                            //}
                        }

                        // Новый вариант3
                        {
                            // временный список для пространств, которые смогу вмместить поддон
                            var tempPlaces = new List<Box>();

                            // Сортируем пространства сверху вниз
                            Places = Places.OrderBy(x => x.Top).ToList();

                            Box place = null;
                            // Последовательно проходим по всем пространствам
                            for (int i = 0; i < Places.Count; i++)
                            {
                                place = Places[i];
                                if (place != null)
                                {
                                    Box nearPlace = null;
                                    // Последовательно проходим по всем пространствам, ниже текущего, включая текущее
                                    for (int j = i; j < Places.Count; j++)
                                    {
                                        // Объединяем с нижним пространством
                                        if (j != i)
                                        {
                                            nearPlace = Places[j];
                                            if (nearPlace != null)
                                            {
                                                place.Width = place.Width + nearPlace.Width;
                                                if (nearPlace.Left > place.Left)
                                                {
                                                    place.Length = place.Length - (nearPlace.Left - place.Left);
                                                    place.Left = nearPlace.Left;
                                                    
                                                }
                                            }
                                        }

                                        // Если может вместить поддон, то добавляем во временный список
                                        if ((place.Width >= palletWidth && place.Length >= palletLength)
                                            || (place.Length >= palletWidth && place.Width >= palletLength))
                                        {
                                            tempPlaces.Add(place.Copy());
                                        }

                                        nearPlace = null;
                                    }

                                    nearPlace = null;
                                }

                                place = null;
                            }

                            place = null;

                            // заполняем список пространств только теми пространствами, которые смогли вместить поддон
                            Places = tempPlaces;
                        }

                        // Проходим по полученным пространствам, способным вместить в себя поддон
                        // В каждом пространстве создаём точку стыковки
                        foreach (var place in Places)
                        {
                            ModelUIElement3D modelUIElement3D = new ModelUIElement3D();
                            GeometryModel3D geometryModel3D = new GeometryModel3D();

                            var point = new Box();
                            point.Left = place.Left;
                            point.Top = place.Top;
                            point.Length = 250;
                            point.Width = 250;
                            point.Height = 250;
                            point.CollorInner = "#ff0000";

                            if (CheckedRow.CheckGet("OBVAZ") == "б/уп")
                            {
                                point.PointRotationOption = 0;
                            }
                            else
                            {
                                if (place.Width >= palletWidth && place.Length >= palletLength
                                    && place.Length >= palletWidth && place.Width >= palletLength)
                                {
                                    point.PointRotationOption = 2;
                                }
                                else
                                {
                                    if (place.Length >= palletWidth && place.Width >= palletLength)
                                    {
                                        point.PointRotationOption = 1;
                                    }
                                    else
                                    {
                                        point.PointRotationOption = 0;

                                        //Дополнительно проверяем, можно ли разместить поддон вертикально, если задействовать пространство, которое находится под текущим пространством
                                        if (CheckNearestPlaceForPalletLength(place.Left, place.Top, place.Length, place.Width, palletLength, palletWidth, Places, 0))
                                        {
                                            point.PointRotationOption = 2;
                                        }
                                    }
                                }
                            }

                            MeshGeometry3D mesh = point.CreateMesh();
                            var color = point.CollorOuter;
                            var brush = color.ToBrush();
                            var material = new DiffuseMaterial(brush);

                            geometryModel3D.Geometry = mesh;
                            geometryModel3D.Material = material;

                            modelUIElement3D.Model = geometryModel3D;
                            modelUIElement3D.MouseDown += UIPoint_MouseDown;
                            modelUIElement3D.MouseEnter += ModelUIElement3D_MouseEnter;
                            modelUIElement3D.MouseLeave += ModelUIElement3D_MouseLeave;

                            point.BoxModelUIElement3D = modelUIElement3D;
                            currentScene.UIPointBoxList.Add(point);
                            currentScene.UIPointContainer.Children.Add(modelUIElement3D);
                        }
                    }

                    // Создаём верхние точки размещения
                    // Проходим по всем размещённым поддонам
                    foreach (var placedPallet in PlacedPallets)
                    {
                        // Если порядок выгрузки текущего размещённого поддона совпадает с порядков мыгрузки размещаемого
                        // и на текущем размещённом поддоне не стоит другой поддон
                        // и высота текущем размещённого поддона + высота размещаемого поддона меньше высоты транспорта
                        // и длина и ширина текущем размещённого поддона меньше или равна длине и ширине размещаемого поддона
                        if (
                            //placedPallet.Value.PositionItemData.CheckGet("LOAD_ORDER").ToInt() == CheckedRow.CheckGet("LOAD_ORDER").ToInt()
                            //&& 
                            placedPallet.Value.HasHead == 0
                            && (placedPallet.Value.OnHigh + placedPallet.Value.Height + CheckedRow.CheckGet("HT").ToInt()) <= currentScene.Car.Height
                            && ( 
                                (
                                    placedPallet.Value.Length >= CheckedRow.CheckGet("LT").ToInt()
                                    && placedPallet.Value.Width >= CheckedRow.CheckGet("BT").ToInt()
                                )
                                ||
                                (
                                    placedPallet.Value.Width >= CheckedRow.CheckGet("LT").ToInt()
                                    && placedPallet.Value.Length >= CheckedRow.CheckGet("BT").ToInt()
                                )
                            ))
                        {
                            ModelUIElement3D modelUIElement3D = new ModelUIElement3D();
                            GeometryModel3D geometryModel3D = new GeometryModel3D();

                            var point = new Box();
                            point.Left = placedPallet.Value.Left;
                            point.Top = placedPallet.Value.Top;
                            point.OnHigh = placedPallet.Value.OnHigh + placedPallet.Value.Height;
                            point.Length = 250;
                            point.Width = 250;
                            point.Height = 250;
                            point.CollorInner = "#ff0000";

                            if (CheckedRow.CheckGet("OBVAZ") == "б/уп")
                            {
                                point.PointRotationOption = 0;
                            }
                            {
                                if (
                                     placedPallet.Value.Length >= CheckedRow.CheckGet("LT").ToInt()
                                     && placedPallet.Value.Width >= CheckedRow.CheckGet("BT").ToInt()
                                     && placedPallet.Value.Width >= CheckedRow.CheckGet("LT").ToInt()
                                     && placedPallet.Value.Length >= CheckedRow.CheckGet("BT").ToInt()
                                    )
                                {
                                    point.PointRotationOption = 2;
                                }
                                else
                                {
                                    if (placedPallet.Value.Width >= CheckedRow.CheckGet("LT").ToInt() && placedPallet.Value.Length >= CheckedRow.CheckGet("BT").ToInt())
                                    {
                                        point.PointRotationOption = 1;
                                    }
                                    else
                                    {
                                        point.PointRotationOption = 0;
                                    }
                                }
                            }

                            MeshGeometry3D mesh = point.CreateMesh(point.OnHigh);
                            var color = point.CollorOuter;
                            var brush = color.ToBrush();
                            var material = new DiffuseMaterial(brush);

                            geometryModel3D.Geometry = mesh;
                            geometryModel3D.Material = material;

                            modelUIElement3D.Model = geometryModel3D;
                            modelUIElement3D.MouseDown += UIPoint_MouseDown;

                            point.BoxModelUIElement3D = modelUIElement3D;
                            currentScene.UIPointBoxList.Add(point);
                            currentScene.UIPointContainer.Children.Add(modelUIElement3D);
                        }
                    }
                }
            }
        }

        private void ModelUIElement3D_MouseLeave(object sender, MouseEventArgs e)
        {
            ModelUIElement3D modelUIElement3D = sender as ModelUIElement3D;
            Box point = TruckScene.UIPointBoxList.FirstOrDefault(x => x.BoxModelUIElement3D == modelUIElement3D);
            if (point == null)
            {
                if (TrailerScene != null)
                {
                    point = TrailerScene.UIPointBoxList.FirstOrDefault(x => x.BoxModelUIElement3D == modelUIElement3D);
                }
            }

            ((GeometryModel3D)modelUIElement3D.Model).Material = new DiffuseMaterial($"{point.CollorOuter}".ToBrush());
        }

        private void ModelUIElement3D_MouseEnter(object sender, MouseEventArgs e)
        {
            ((GeometryModel3D)((ModelUIElement3D)sender).Model).Material = new DiffuseMaterial("#300bfe".ToBrush());
        }

        /// <summary>
        /// Рекурсивная функция. Првоеряет, можем ли мы разместить поддон вертикально, если к текущему пространству добавить порстранства под ним.
        /// </summary>
        /// <param name="placeLeft"></param>
        /// <param name="placeTop"></param>
        /// <param name="placeLength"></param>
        /// <param name="placeWidth"></param>
        /// <param name="palletLength"></param>
        /// <param name="palletWidth"></param>
        /// <param name="Places"></param>
        /// <returns></returns>
        public bool CheckNearestPlaceForPalletLength(int placeLeft, int placeTop, int placeLength, int placeWidth, int palletLength, int palletWidth, List<Box> Places, int iterationNumber)
        {
            if (iterationNumber >= 100)
            {
                return false;
            }
            else
            {
                // Находим пространство, которое находится прямо под текущим свободным пространством и левая точка которого не правее левой точки текущего пространства
                // и правая точка которого не левее, чем правая точка размещаемого поддона
                Box nearestPlace = Places.FirstOrDefault(x => x.Top == placeWidth + placeTop && x.Left <= placeLeft && x.Left + x.Length >= placeLeft + palletWidth);
                if (nearestPlace != null)
                {
                    placeWidth = placeWidth + nearestPlace.Width;
                    if (placeWidth >= palletLength)
                    {
                        return true;
                    }
                    else
                    {
                        return CheckNearestPlaceForPalletLength(placeLeft, placeTop, placeLength, placeWidth, palletLength, palletWidth, Places, iterationNumber + 1);
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        private void UIPoint_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ModelUIElement3D modelUIElement3D = sender as ModelUIElement3D;
            Box point = new Box();

            point = TruckScene.UIPointBoxList.FirstOrDefault(x => x.BoxModelUIElement3D == modelUIElement3D);
            if (point == null)
            {
                if (TrailerScene != null)
                {
                    point = TrailerScene.UIPointBoxList.FirstOrDefault(x => x.BoxModelUIElement3D == modelUIElement3D);
                }
            }

            if (point != null)
            {
                OKButton.Style = (Style)OKButton.TryFindResource("FButtonPrimary");

                LeftCoordinateTextBox.Text = point.Left.ToString();
                TopCoordinateTextBox.Text = point.Top.ToString();

                CheckPalletParametrs(0, 0, point.OnHigh, point.HeightIndexPlace, point.PointRotationOption);
            }


            //GeometryModel3D geometryModel3D = modelUIElement3D.Model as GeometryModel3D;
            //MeshGeometry3D mesh = geometryModel3D.Geometry as MeshGeometry3D;
            //Point3DCollection positions = mesh.Positions as Point3DCollection;

            //LeftCoordinateTextBox.Text = $"{positions.Min(x => x.X)}";
            //TopCoordinateTextBox.Text = $"{positions.Min(x => x.Z)}";

            //CheckPalletParametrs(PointRigth.ColumnIndexPlace, PointRigth.RowIndexPlace, PointRigth.OnHigh, PointRigth.HeightIndexPlace, PointRigth.PointRotationOption);
        }

        /// <summary>
        /// старт работы интерфейса
        /// </summary>
        public void Init()
        {
            Grid.LoadItems();
        }

        /// <summary>
        /// рендер сцены
        /// </summary>
        public void InitScene()
        {
            TruckScene.ModelList.Clear();
            TruckScene.ModelListNumberPallet.Clear();
            TruckScene.PalletQuantity = -1;

            TrailerScene.ModelList.Clear();
            TrailerScene.ModelListNumberPallet.Clear();
            TrailerScene.PalletQuantity = -1;

            PalletDemonstrationScene.ModelList.Clear();
            PalletDemonstrationScene.ModelListNumberPallet.Clear();
            PalletDemonstrationScene.PalletQuantity = -1;

            CarSetParams();

            TruckScene.Init();
            TruckScene.Update();
            TruckScene.Render();

            if (Ds.Items.FirstOrDefault(x => x["TRAILER_FLAG"] == "1") != null)
            {
                TrailerBorder.Visibility = Visibility.Visible;

                TrailerScene.Init();
                TrailerScene.Update();
                TrailerScene.Render();

                TrailerLable.Visibility = Visibility.Visible;
            }
            else
            {
                TrailerBorder.Visibility = Visibility.Collapsed;
                TrailerLable.Visibility = Visibility.Collapsed;
            }

            PalletDemonstrationScene.Init();
            PalletDemonstrationScene.Update();
            PalletDemonstrationScene.Render();

            //разрешаем работу пульта управления
            UpdateCtl = true;
            Radius = 20697;
        }

        /// <summary>
        /// загрузка параметров машин из датасета
        /// </summary>
        public void CarSetParams()
        {
            if (Ds != null)
            {
                if (Ds.Items.Count > 0)
                {
                    Dictionary<string, string> first = new Dictionary<string, string>();
                    Dictionary<string, string> firstTrail = new Dictionary<string, string>();

                    TruckScene.Car.CollorInner = CollorDictionary["99"];

                    if (Ds.Items.FirstOrDefault(x => x["CAR_L"] != null && x["CAR_L"] != "" && x["CAR_B"] != null && x["CAR_B"] != "" && x["CAR_H"] != null && x["CAR_H"] != "") != null)
                    {
                        first = Ds.Items.First(x => x["CAR_L"] != null && x["CAR_L"] != "" && x["CAR_B"] != null && x["CAR_B"] != "" && x["CAR_H"] != null && x["CAR_H"] != "");
                        TruckScene.Car.Width = first.CheckGet("CAR_B").ToInt();
                        TruckScene.Car.Length = first.CheckGet("CAR_L").ToInt();
                        TruckScene.Car.Height = first.CheckGet("CAR_H").ToInt();

                        CarLable.Content = $"{Ds.Items.First().CheckGet("CAR_FULNAME")}" + Environment.NewLine
                            + $"{TruckScene.Car.Length}x{TruckScene.Car.Width}x{TruckScene.Car.Height}";

                        var color = "#000000";
                        var brush = color.ToBrush();
                        CarLable.Foreground = brush;
                    }
                    else
                    {
                        // Самые повторяющиеся параметры фуры
                        TruckScene.Car.Width = 2450;
                        TruckScene.Car.Length = 13600;
                        TruckScene.Car.Height = 2400;

                        //MessageBox.Show("Заданы параметры фуры по умолчанию");

                        CarLable.Content = $"{Ds.Items.First().CheckGet("CAR_FULNAME")}" + Environment.NewLine
                            + $"{TruckScene.Car.Length}x{TruckScene.Car.Width}x{TruckScene.Car.Height}";

                        //Добавить выделение текста серым цветом
                        var color = "#999999";
                        var brush = color.ToBrush();
                        CarLable.Foreground = brush;

                        CarLable.ToolTip = "Заданы параметры фургона по умолчанию";

                    }


                    if (Ds.Items.FirstOrDefault(x => x["TRAILER_FLAG"] == "1") != null)
                    {
                        TrailerScene.Car.CollorInner = CollorDictionary["99"];

                        if (Ds.Items.FirstOrDefault(x => x["TRAILER_L"] != null && x["TRAILER_L"] != "" && x["TRAILER_B"] != null && x["TRAILER_B"] != "" && x["TRAILER_H"] != null && x["TRAILER_H"] != "") != null)
                        {
                            firstTrail = Ds.Items.FirstOrDefault(x => x["TRAILER_L"] != null && x["TRAILER_L"] != "" && x["TRAILER_B"] != null && x["TRAILER_B"] != "" && x["TRAILER_H"] != null && x["TRAILER_H"] != "");

                            TrailerScene.Car.Width = firstTrail.CheckGet("TRAILER_B").ToInt();
                            TrailerScene.Car.Length = firstTrail.CheckGet("TRAILER_L").ToInt();
                            TrailerScene.Car.Height = firstTrail.CheckGet("TRAILER_H").ToInt();

                            TrailerLable.Content = $"{Ds.Items.First().CheckGet("TRAILER_NUMBER")}" + Environment.NewLine
                                + $"{TrailerScene.Car.Length}x{TrailerScene.Car.Width}x{TrailerScene.Car.Height}";

                            var color = "#000000";
                            var brush = color.ToBrush();
                            TrailerLable.Foreground = brush;
                        }
                        else
                        {
                            //Самые повторяющиеся параметры прицепа
                            TrailerScene.Car.Width = 2450;
                            TrailerScene.Car.Length = 8000;
                            TrailerScene.Car.Height = 3000;

                            //MessageBox.Show("Заданы параметры прицепа по умолчанию");

                            TrailerLable.Content = $"{Ds.Items.First().CheckGet("TRAILER_NUMBER")}" + Environment.NewLine
                                + $"{TrailerScene.Car.Length}x{TrailerScene.Car.Width}x{TrailerScene.Car.Height}";

                            //Добавить выделение текста серым цветом
                            var color = "#999999";
                            var brush = color.ToBrush();
                            TrailerLable.Foreground = brush;

                            TrailerLable.ToolTip = "Заданы параметры прицепа по умолчанию";
                        }

                    }
                }
            }

            {
                PalletDemonstrationScene.Car = new Box { Width = 2450, Height = 1, Length = 2500, CollorInner = CollorDictionary["99"] };
                PalletDemonstrationLabel.Content = $"{PalletDemonstrationScene.Car.Length}x{PalletDemonstrationScene.Car.Width}";
                var color = "#000000";
                var brush = color.ToBrush();
                PalletDemonstrationLabel.Foreground = brush;
            }
        }

        public void SceneSetDefaultParams()
        {
        }

        private void RenderButton_Click(object sender, RoutedEventArgs e)
        {
            if (TruckScene != null)
            {
                TruckScene.Update();
                TruckScene.Render();
            }

            if (TrailerScene != null)
            {
                TrailerScene.Update();
                TrailerScene.Render();
            }
        }

        private void InitButton_Click(object sender, RoutedEventArgs e)
        {
            InitScene();
        }

        public double Radius { get; set; }

        public void CheckGlobalLoadOrder()
        {
            int globalLoadOrder = 0;
            foreach (var item in Ds.Items)
            {
                if (item.CheckGet("LOAD_ORDER").ToInt() > globalLoadOrder)
                {
                    globalLoadOrder = item.CheckGet("LOAD_ORDER").ToInt();
                }
            }
            globalLoadOrder = globalLoadOrder + 1;

            foreach (var item in Ds.Items)
            {
                if (item.CheckGet("PLACED").ToInt() != 1 && item.CheckGet("LOAD_ORDER").ToInt() < globalLoadOrder)
                {
                    globalLoadOrder = item.CheckGet("LOAD_ORDER").ToInt();
                }
            }

            GlobalLoadOrder = globalLoadOrder;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            OKButton.Style = (Style)OKButton.TryFindResource("Button");

            CheckedRow.CheckAdd("PLACED", "1");

            CheckedRow.CheckAdd("INDEXNUMBER", IndexNumber.ToString());

            var scene = new Scene();
            if (CheckedRow.CheckGet("CAR") == "2")
            {
                scene = TrailerScene;
            }
            else
            {
                scene = TruckScene;
            }

            foreach (var item in scene.ModelList)
            {
                if (item.Value.Left == CheckedRow.CheckGet("LEFT").ToInt() && item.Value.Top == CheckedRow.CheckGet("TOP").ToInt() && item.Value.Height == CheckedRow.CheckGet("OnHigh").ToInt() && item.Value.HasHead == 0)
                {
                    item.Value.HasHead = 1;
                }
            }

            CheckGlobalLoadOrder();

            CheckedRow = null;

            LeftCoordinateTextBox.Text = "";
            TopCoordinateTextBox.Text = "";
            Rotated.IsChecked = false;

            TruckScene.UIPointContainer.Children.Clear();
            TruckScene.UIPointBoxList.Clear();
            if (TrailerScene != null)
            {
                TrailerScene.UIPointContainer.Children.Clear();
                TrailerScene.UIPointBoxList.Clear();
            }

            Grid.UpdateItems(Ds);

            PositionGrid.Visibility = Visibility.Hidden;

            UpdateActions();
        }

        private void Rotated_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in Ds.Items)
            {
                if (item == CheckedRow)
                {
                    var v = Form.GetValues();
                    item.CheckAdd("ROTATED", v.CheckGet("ROTATED"));

                    Grid.UpdateItems(Ds);

                    //var scene = new Scene();
                    //if ()
                    //{

                    //}
                    //if (item.CheckGet("PLACED") != "1")
                    //{
                    //    string c = CarForPlaced.ToString();
                    //    SetUIPoints(c);
                    //}

                    //if (item.CheckGet("LEFT") != null && item.CheckGet("LEFT") != "" && item.CheckGet("TOP") != null && item.CheckGet("TOP") != "")
                    if (!string.IsNullOrEmpty(item.CheckGet("LEFT")) && !string.IsNullOrEmpty(item.CheckGet("TOP")))
                    {
                        AddPallet(item);
                    }
                }
            }
        }

        /// <summary>
        /// Передача параметров по расположению объекта при нажатии на поинт
        /// </summary>
        /// <param name="ColumnIndexPlace"></param>
        /// <param name="RowIndexPlace"></param>
        /// <param name="OnHigh"></param>
        /// <param name="HeightIndexPlace"></param>
        public void CheckPalletParametrs(int ColumnIndexPlace, int RowIndexPlace, int OnHigh, int HeightIndexPlace, int pointRotationOption)
        {
            foreach (var item in Ds.Items)
            {
                if (item == CheckedRow)
                {
                    var v = Form.GetValues();
                    item.CheckAdd("LEFT", v.CheckGet("LEFT"));
                    item.CheckAdd("TOP", v.CheckGet("TOP"));

                    switch (pointRotationOption)
                    {
                        case 0:
                            Form.SetValueByPath("ROTATED", "0");
                            item.CheckAdd("ROTATED", "0");
                            Rotated.IsEnabled = false;
                            break;

                        case 1:
                            Form.SetValueByPath("ROTATED", "1");
                            item.CheckAdd("ROTATED", "1");
                            Rotated.IsEnabled = false;
                            break;

                        case 2:
                            item.CheckAdd("ROTATED", v.CheckGet("ROTATED"));
                            Rotated.IsEnabled = true;
                            break;

                        default:
                            break;
                    }

                    //item.CheckAdd("ROTATED", v.CheckGet("ROTATED"));
                    //item.CheckAdd("CAR", v.CheckGet("CAR"));
                    item.CheckAdd("ColumnIndexPlace", ColumnIndexPlace.ToString());
                    item.CheckAdd("RowIndexPlace", RowIndexPlace.ToString());
                    item.CheckAdd("OnHigh", OnHigh.ToString());
                    item.CheckAdd("HEIGHTINDEXPLACE", HeightIndexPlace.ToString());

                    //item.CheckAdd("CAR", Car.SelectedItem.Key);

                    //item.CheckAdd("CAR", (CarCheckBox.IsChecked.ToInt() + 1).ToString());
                    item.CheckAdd("CAR", CarForPlaced.ToString());

                    var scene = new Scene();
                    if (item.CheckGet("CAR") == "1")
                    {
                        scene = TruckScene;
                    }
                    else if (item.CheckGet("CAR") == "2")
                    {
                        scene = TrailerScene;
                    }

                    if (item.CheckGet("OBVAZ") == "б/уп")
                    {
                        bool edited = false;

                        if (item.CheckGet("HT").ToInt() != scene.Car.Height - offsetHeigth)
                        {
                            item.CheckAdd("HT", (scene.Car.Height - offsetHeigth).ToString());

                            edited = true;
                        }

                        if (item.CheckGet("BT").ToInt() != scene.Car.Width)
                        {
                            item.CheckAdd("BT", scene.Car.Width.ToString());

                            edited = true;
                        }

                        if (edited)
                        {
                            long vol = item.CheckGet("VOLUM").ToInt();
                            long qty = item.CheckGet("QTY").ToInt();

                            long volum = vol * qty;

                            int bt = item.CheckGet("BT").ToInt();
                            int ht = item.CheckGet("HT").ToInt();

                            var lt = volum / (bt * ht);

                            item.CheckAdd("LT", lt.ToString());
                        }

                        item.CheckAdd("SIZE", $"{item.CheckGet("LT")}x{item.CheckGet("BT")}x{item.CheckGet("HT")}");
                    }

                    Grid.UpdateItems(Ds);

                    //if (item.CheckGet("LEFT") != null && item.CheckGet("LEFT") != "" && item.CheckGet("TOP") != null && item.CheckGet("TOP") != "")
                    if(!string.IsNullOrEmpty(item.CheckGet("LEFT")) && !string.IsNullOrEmpty(item.CheckGet("TOP")))
                    {
                        AddPallet(item);
                    }
                }
            }
        }

        /// <summary>
        /// Список цветов для расскраски отгружаемых объектов
        /// -1 -- (б/уп)песочный;
        /// 0 -- (c/уп)салатовый;
        /// 1 -- аквамарин;
        /// 2 -- персиковый;
        /// 3 -- лиловый;
        /// 4 -- лаймовый;
        /// 5 -- светло-жёлтый;
        /// 6 -- голубой;
        /// 99 -- (транспорт)серый; 
        /// </summary>
        public Dictionary<string, string> CollorDictionary { get; set; }

        /// <summary>
        /// Генерация объекта и его обрамления,
        /// добавление объектов в ModelList для рендера 
        /// </summary>
        /// <param name="item"></param>
        public void AddPallet(Dictionary<string, string> item)
        {
            var scene = new Scene();

            if (item.CheckGet("CAR") == "1")
            {
                scene = TruckScene;
            }
            else if (item.CheckGet("CAR") == "2")
            {
                scene = TrailerScene;
            }
            else
            {
                scene = TruckScene;
            }

            var b = new Box();
            b.PositionItemData = item;
            b.Height = item.CheckGet("HT").ToInt();
            b.Left = item.CheckGet("LEFT").ToInt();
            b.Top = item.CheckGet("TOP").ToInt();
            b.BoxRotated = item.CheckGet("ROTATED").ToInt();
            b.NumberProduct = item.CheckGet("RN").ToInt();
            b.PlacedCar = item.CheckGet("CAR");
            b.ListIndex = IndexNumber;
            b.ColumnIndexPlace = item.CheckGet("ColumnIndexPlace").ToInt();
            b.RowIndexPlace = item.CheckGet("RowIndexPlace").ToInt();

            if (item.CheckGet("ROTATED").ToInt() == 1)
            {
                b.Length = item.CheckGet("BT").ToInt();
                b.Width = item.CheckGet("LT").ToInt();
            }
            else
            {
                b.Width = item.CheckGet("BT").ToInt();
                b.Length = item.CheckGet("LT").ToInt();
            }

            if (item.CheckGet("OnHigh") != "0")
            {
                b.OnHigh = item.CheckGet("OnHigh").ToInt();
                b.HeightIndexPlace = item.CheckGet("HEIGHTINDEXPLACE").ToInt();

                //foreach (var item in scene.ModelList)
                //{
                //    if (item.Value.Left == Item.CheckGet("LEFT").ToInt() && item.Value.Top == Item.CheckGet("TOP").ToInt() && item.Value.Height == Item.CheckGet("OnHigh").ToInt() && item.Value.HasHead == 0)
                //    {
                //        item.Value.HasHead = 1;
                //    }
                //}
            }

            //if (Item.CheckGet("_NUMBER").ToInt() == 0)
            //{
            //    b.NumberProduct = CheckArticulNumber(Item.CheckGet("POSITION_ID"));
            //}
            //else
            //{
            //    b.NumberProduct = Item.CheckGet("_NUMBER").ToInt();
            //}

            if (b.NumberProduct > 0)
            {
                if (item.CheckGet("OBVAZ") == "б/уп")
                {
                    b.CollorInner = CollorDictionary.CheckGet("-1");
                }
                else
                {
                    b.CollorInner = GetColor(b.NumberProduct.ToString());
                }
            }
            else
            {
                if (item.CheckGet("OBVAZ") == "б/уп")
                {
                    b.CollorInner = CollorDictionary.CheckGet("-1");
                }
                else
                {
                    b.CollorInner = CollorDictionary.CheckGet("0");
                }
            }

            b.Init();

            int flag = 0;
            foreach (var model in scene.ModelList)
            {
                if (model.Value.ListIndex == b.ListIndex)
                {
                    flag = 1;
                    ClearBorders(model);
                    scene.ModelList[model.Key] = b;

                    b.AddBorders(b.Width, b.Height, b.Length, b.Left, b.Top, b, b.OnHigh);

                    foreach (var bord in b.Boxes)
                    {
                        scene.AddModel(bord);
                    }

                    scene.ModelListNumberPallet[model.Key] = new NumberPallet(b);

                    break;
                }
            }

            if (flag == 0)
            {
                scene.AddModel(b);

                b.AddBorders(b.Width, b.Height, b.Length, b.Left, b.Top, b, b.OnHigh);

                foreach (var borderBox in b.Boxes)
                {
                    scene.AddModel(borderBox);
                }
            }
            scene.Update();
            scene.Render();
        }

        public string GetColor(string _number)
        {
            string result = CollorDictionary["0"];

            decimal colCollors = CollorDictionary.Count() - 3;

            List<string> positions_id = new List<string>();
            int nums = 0;
            foreach (var item in Ds.Items)
            {
                if (!positions_id.Contains(item.CheckGet("POSITION_ID")))
                {
                    nums += 1;
                    positions_id.Add(item.CheckGet("POSITION_ID"));
                }
            }

            decimal colProduction = nums; //ItemsList.Count();

            int maxKoef = (int)Math.Ceiling(colProduction / colCollors);

            if (_number.ToInt() == 99 || _number.ToInt() == 0 || _number.ToInt() == -1)
            {
                result = CollorDictionary[_number];
            }
            else
            {
                for (int j = 0; j < maxKoef; j++)
                {
                    for (int i = 1; i < colCollors + 1; i++)
                    {
                        if (i + colCollors * j == _number.ToInt())
                        {
                            result = CollorDictionary[i.ToString()];
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Удаление обрамления с объекта
        /// </summary>
        /// <param name="item"></param>
        public void ClearBorders(KeyValuePair<string, Box> item)
        {
            var scene = new Scene();
            if (item.Value.PlacedCar == "1")
            {
                scene = TruckScene;
            }
            else if (item.Value.PlacedCar == "2")
            {
                scene = TrailerScene;
            }
            else
            {
                scene = TruckScene;
            }

            foreach (var bord in item.Value.Boxes)
            {
                var b = scene.ModelList.FirstOrDefault(x => x.Value == bord);
                scene.ModelList.Remove(b.Key);
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var i = new ShipmentAttributes();

            i.SetValues(TruckScene.Angle1.ToString(), TruckScene.Angle2.ToString(), TruckScene.Distance.ToString(), TruckScene.Light1Intensity.ToString(), TruckScene.Light2Intensity.ToString());
            i.Show();
        }

        private void SaveShemeButton_Click(object sender, RoutedEventArgs e)
        {
            if (TestSheme(false))
            {
                string msg = $"Внимание!{Environment.NewLine}" +
                    $"Сохраняемое размещение будет использовано при генерации файла схемы погрузки.{Environment.NewLine}" +
                    $"Продолжить?";
                var d = new DialogWindow($"{msg}", "Сохранение порядка загрузки", "", DialogWindowButtons.YesNo);
                if (d.ShowDialog() == true)
                {
                    SaveLoadingSchemeData();
                }
            }
            else
            {
                string msg = "Не все поддоны расположены корректно. Пожалуйста, измените расположение выделенных поддонов.";
                var d = new DialogWindow($"{msg}", "Сохранение порядка загрузки", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Сохранение данных по размещению поддонов текущей схемы погрузки в таблицу Loading_Scheme_Item
        /// Меняем статус схемы погрузки на boot_order_status = 3
        /// </summary>
        public async void SaveLoadingSchemeData()
        {
            int operationResult = 1;

            //Удаление имеющихся данных
            {
                var p = new Dictionary<string, string>();
                p.CheckAdd("LOSC_ID", LoadingShemeId.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "LoadingTwo");
                q.Request.SetParam("Action", "DeleteLoadingSchemeItem");

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
                        var resultList = ListDataSet.Create(result, "ITEMS");

                        if (resultList.GetFirstItemValueByKey("RESULT").ToInt() != 1)
                        {
                            operationResult = 0;
                        }
                    }

                    if (operationResult == 1)
                    {

                    }
                    else if (operationResult == 0)
                    {
                        string msg = "В процесе сохранения произошла ошибка, попробуйте снова";
                        var d = new DialogWindow($"{msg}", "Сохранение порядка загрузки", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
            }

            //Сохранение данных
            if (operationResult == 1)
            {
                var p = new List<Dictionary<string, string>>();
                foreach (var item in Ds.Items)
                {
                    if (item.CheckGet("PLACED") == "1")
                    {
                        var row = new Dictionary<string, string>();
                        row["OFFSET_LENGTH"] = item.CheckGet("LEFT");
                        row["OFFSET_WIDTH"] = item.CheckGet("TOP");

                        if (item.CheckGet("ROTATED") == "1")
                        {
                            row["LENGTH"] = item.CheckGet("BT");
                            row["WIDTH"] = item.CheckGet("LT");
                        }
                        else
                        {
                            row["LENGTH"] = item.CheckGet("LT");
                            row["WIDTH"] = item.CheckGet("BT");
                        }

                        row["HEIGHT"] = item.CheckGet("HT");


                        row["OFFSET_HEIGHT"] = item.CheckGet("OnHigh");

                        row["LOSC_ID"] = LoadingShemeId.ToString();
                        row["IDORDERDATES"] = item.CheckGet("POSITION_ID");

                        if (item.CheckGet("OBVAZ") == "б/уп")
                        {
                            row["PRODUCT_TYPE"] = "2";
                        }
                        else
                        {
                            row["PRODUCT_TYPE"] = "1";
                        }

                        if (item.CheckGet("CAR") == "2")
                        {
                            row["TRAILER_FLAG"] = "1";
                        }
                        else
                        {
                            row["TRAILER_FLAG"] = "0";
                        }

                        p.Add(row);
                    }

                }

                if (p.Count > 0)
                {
                    foreach (var item in p)
                    {

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Shipments");
                        q.Request.SetParam("Object", "LoadingTwo");
                        q.Request.SetParam("Action", "CreateLoadingSchemeItem");

                        q.Request.SetParams(item);

                        await Task.Run(() =>
                        {
                            q.DoQuery();
                        });

                        if (q.Answer.Status == 0)
                        {
                            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                            if (result != null)
                            {
                                var resultList = ListDataSet.Create(result, "ITEMS");

                                if (resultList.GetFirstItemValueByKey("RESULT").ToInt() != 1)
                                {
                                    operationResult = 0;
                                }
                            }
                        }
                    }

                    if (operationResult == 1)
                    {

                    }
                    else if (operationResult == 0)
                    {
                        string msg = "В процесе сохранения произошла ошибка, попробуйте снова";
                        var d = new DialogWindow($"{msg}", "Сохранение порядка загрузки", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
            }

            // Меняем статус схемы погрузки
            if (operationResult == 1)
            {
                var p = new Dictionary<string, string>();
                p.CheckAdd("LOSC_ID", LoadingShemeId.ToString());
                p.CheckAdd("BOOT_ORDER_STATUS", "3");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "LoadingTwo");
                q.Request.SetParam("Action", "UpdateBootOrderStatus");

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
                        var resultList = ListDataSet.Create(result, "ITEMS");

                        if (resultList.GetFirstItemValueByKey("RESULT").ToInt() != 1)
                        {
                            operationResult = 0;
                        }
                    }

                    if (operationResult == 1)
                    {
                        string msg = "Параметры порядка загрузки сохранены";
                        var d = new DialogWindow($"{msg}", "Сохранение порядка загрузки", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    else if (operationResult == 0)
                    {
                        string msg = "Ошибка изменения статуса схемы погрузки, попробуйте снова";
                        var d = new DialogWindow($"{msg}", "Сохранение порядка загрузки", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
            }

            GetBootOrderStatus();
        }

        /// <summary>
        /// Удаление данных по размещению поддонов текущей схемы погрузки в таблице Loading_Scheme_Item
        /// Меняем статус схемы погрузки на boot_order_status = 1
        /// </summary>
        public async void DeleteManuallyScheme()
        {
            int operationResult = 1;

            //Удаление имеющихся данных
            {
                var p = new Dictionary<string, string>();
                p.CheckAdd("LOSC_ID", LoadingShemeId.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "LoadingTwo");
                q.Request.SetParam("Action", "DeleteLoadingSchemeItem");

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
                        var resultList = ListDataSet.Create(result, "ITEMS");

                        if (resultList.GetFirstItemValueByKey("RESULT").ToInt() != 1)
                        {
                            operationResult = 0;
                        }
                    }

                    if (operationResult == 1)
                    {

                    }
                    else if (operationResult == 0)
                    {
                        string msg = "В процесе удаления произошла ошибка, попробуйте снова";
                        var d = new DialogWindow($"{msg}", "Удаление порядка загрузки", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
            }

            // Меняем статус схемы погрузки
            if (operationResult == 1)
            {
                var p = new Dictionary<string, string>();
                p.CheckAdd("LOSC_ID", LoadingShemeId.ToString());
                p.CheckAdd("BOOT_ORDER_STATUS", "0");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "LoadingTwo");
                q.Request.SetParam("Action", "UpdateBootOrderStatus");

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
                        var resultList = ListDataSet.Create(result, "ITEMS");

                        if (resultList.GetFirstItemValueByKey("RESULT").ToInt() != 1)
                        {
                            operationResult = 0;
                        }
                    }

                    if (operationResult == 1)
                    {
                        string msg = "Успешное удаление ручной схемы погрузки";
                        var d = new DialogWindow($"{msg}", "Удаление порядка загрузки", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    else if (operationResult == 0)
                    {
                        string msg = "Ошибка изменения статуса схемы погрузки, попробуйте снова";
                        var d = new DialogWindow($"{msg}", "Удаление порядка загрузки", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
            }

            GetBootOrderStatus();
        }

        public async void GetBootOrderStatus()
        {
            var p = new Dictionary<string, string>();
            p.CheckAdd("TRANSPORT_ID", ShipmentId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "LoadingTwo");
            q.Request.SetParam("Action", "GetBootOrderStatus");

            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            string bootOrderStatusName = "";
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        bootOrderStatusName = ds.Items[0].CheckGet("BOOT_ORDER_STATUS_NAME");
                        BootOrderStatus = ds.Items[0].CheckGet("BOOT_ORDER_STATUS").ToInt();
                    }
                }
            }
            BootOrderStatusNameLabel.Content = bootOrderStatusName;

            if (BootOrderStatus == 3)
            {
                DeleteManuallySchemeButton.IsEnabled = true;
            }
            else
            {
                DeleteManuallySchemeButton.IsEnabled = false;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            //this.Close();

            if (!string.IsNullOrEmpty(ReturnTabName))
            {
                Central.WM.SetActive(ReturnTabName, true);
            }

            Central.WM.Close(FrameName);
        }

        /// <summary>
        /// Датасет даных по размещению поддонов из старого алгорима погрузки
        /// </summary>
        public ListDataSet DemoPalletPositions { get; set; }

        /// <summary>
        /// Датасет даных по размещению россыпи из старого алгорима погрузки
        /// </summary>
        public ListDataSet DemoStackPositions { get; set; }


        /// <summary>
        /// Получение координат размещения после работы старого алгоритма
        /// </summary>
        private void DemoShemeOneRun()
        {
            GetDemoShemeOne(1);
        }

        /// <summary>
        /// Отправление запроса на выполнение старого алгоритма погрузки
        /// </summary>
        /// <param name="demo">1 -- Координаты. 0 -- Картинка схемы.</param>
        public async void GetDemoShemeOne(int demo)
        {
            DisableControls();

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID", ShipmentId.ToString());
                p.CheckAdd("DEMO", demo.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "LoadingTwo");
            q.Request.SetParam("Action", "GetOldMapTwo");

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
                    if (result["PALLETS"].Rows.Count > 0)
                    {
                        DemoPalletPositions = ListDataSet.Create(result, "PALLETS");
                        DemoPalletPositions.Items.OrderBy(x => x["LEFT"]).ThenBy(x => x["TOP"]);
                        var list = GetOnHighFromDemoSheme();
                        DemoPalletPositions = list;

                        foreach (var positionItem in DemoPalletPositions.Items)
                        {
                            if (positionItem.CheckGet("PLACED").ToInt() > 0)
                            {
                                var row = Ds.Items.FirstOrDefault(
                                    x => x.CheckGet("ARTIKUL") == positionItem.CheckGet("ARTIKUL")
                                    && x.CheckGet("HT") == positionItem.CheckGet("HEIGHT")
                                    && x.CheckGet("PLACED").ToInt() == 0);

                                if (row != null)
                                {
                                    IndexNumber += 1;
                                    row.CheckAdd("INDEXNUMBER", IndexNumber.ToString());
                                    row.CheckAdd("HEIGHTINDEXPLACE", positionItem.CheckGet("HEIGHTINDEXPLACE"));
                                    row.CheckAdd("OnHigh", positionItem.CheckGet("OnHigh"));
                                    row.CheckAdd("CAR", positionItem.CheckGet("CAR"));
                                    row.CheckAdd("PLACED", "1");
                                    row.CheckAdd("LEFT", positionItem.CheckGet("LEFT"));
                                    row.CheckAdd("TOP", positionItem.CheckGet("TOP"));

                                    if (positionItem.CheckGet("ROTATED") == "True")
                                    {
                                        row.CheckAdd("ROTATED", "1");
                                    }
                                    else
                                    {
                                        row.CheckAdd("ROTATED", "0");
                                    }

                                    AddPallet(row);
                                }
                            }
                        }
                    }

                    if (result["STACKS"].Rows.Count > 0)
                    {
                        DemoStackPositions = ListDataSet.Create(result, "STACKS");
                        DemoStackPositions.Items.OrderBy(x => x["LEFT"]).ThenBy(x => x["TOP"]);

                        foreach (var positionItem in DemoStackPositions.Items)
                        {
                            if (positionItem.CheckGet("PLACED").ToInt() > 0)
                            {
                                var row = Ds.Items.FirstOrDefault(x => x.CheckGet("ARTIKUL") == positionItem.CheckGet("ARTIKUL"));
                                if (row != null)
                                {
                                    IndexNumber += 1;
                                    row.CheckAdd("INDEXNUMBER", IndexNumber.ToString());
                                    row.CheckAdd("LEFT", positionItem.CheckGet("LEFT"));
                                    row.CheckAdd("TOP", positionItem.CheckGet("TOP"));
                                    row.CheckAdd("PLACED", "1");

                                    if (!string.IsNullOrEmpty(positionItem.CheckGet("HEIGHT")) && !string.IsNullOrEmpty(positionItem.CheckGet("LENGTH")) && !string.IsNullOrEmpty(positionItem.CheckGet("WIDTH")))
                                    {
                                        row.CheckAdd("HT", positionItem.CheckGet("HEIGHT"));
                                        row.CheckAdd("LT", positionItem.CheckGet("LENGTH"));
                                        row.CheckAdd("BT", positionItem.CheckGet("WIDTH"));
                                    }

                                    row.CheckAdd("SIZE", $"{row.CheckGet("LT")}x{row.CheckGet("BT")}x{row.CheckGet("HT")}");

                                    AddPallet(row);
                                }
                            }
                        }
                    }

                    FlagDemoSheme = true;
                    Grid.UpdateItems(Ds);

                    CheckGlobalLoadOrder();
                }
                else if (q.Answer.Type == LPackClientAnswer.AnswerTypeRef.File)
                {
                    Central.OpenFile(q.Answer.DownloadFilePath);
                }
            }
            else
            {
                q.ProcessError();
            }

            UpdateActions();
        }

        /// <summary>
        /// Определение высоты начала расположения объекта по ярусу
        /// </summary>
        /// <returns></returns>
        public ListDataSet GetOnHighFromDemoSheme()
        {
            ListDataSet list = new ListDataSet();
            foreach (var item in DemoPalletPositions.Items)
            {
                if (item.CheckGet("HEIGHTINDEXPLACE").ToInt() <= 1)
                {
                    item.CheckAdd("OnHigh", "0");
                    list.Items.Add(item);
                }
                else
                {
                    int h = item.CheckGet("HEIGHTINDEXPLACE").ToInt();

                    var li = DemoPalletPositions.Items.FirstOrDefault(x => x.CheckGet("HEIGHTINDEXPLACE").ToInt() == h - 1 && x.CheckGet("LEFT") == item.CheckGet("LEFT") && x.CheckGet("TOP") == item.CheckGet("TOP"));

                    item.CheckAdd("OnHigh", (li.CheckGet("HEIGHT").ToInt() + li.CheckGet("OnHigh").ToInt()).ToString());

                    list.Items.Add(item);
                }
            }

            return list;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            bool resume = false;

            string msg = "Сбросить размещение поддонов?";
            var d = new DialogWindow($"{msg}", "Сброс размещения", "", DialogWindowButtons.NoYes);
            if (d.ShowDialog() == true)
            {
                resume = true;
            }

            if (resume == true)
            {
                ClearScene();
            }

            UpdateActions();
        }

        /// <summary>
        /// Очистка всех сцен и грида
        /// </summary>
        public void ClearScene()
        {
            FlagDemoSheme = false;
            FlagBDSheme = false;

            foreach (var item in Ds.Items)
            {
                item.CheckAdd("LEFT", "");
                item.CheckAdd("TOP", "");
                item.CheckAdd("CAR", "");
                item.CheckAdd("INDEXNUMBER", "");
                item.CheckAdd("ROTATED", "");
                item.CheckAdd("PLACED", "");
                item.CheckAdd("HEIGHTINDEXPLACE", "");
                item.CheckAdd("_NUMBER", "");
            }
            Grid.UpdateItems(Ds);

            IndexNumber = 0;
            GlobalLoadOrder = 1;
            ArticNumDictionnary.Clear();

            TruckScene.ModelList.Clear();
            TruckScene.ModelListNumberPallet.Clear();
            TruckScene.PalletQuantity = -1;
            TruckScene.Init();
            TruckScene.Update();
            TruckScene.Render();

            if (TrailerScene.Initialized)
            {
                TrailerScene.ModelList.Clear();
                TrailerScene.ModelListNumberPallet.Clear();
                TrailerScene.PalletQuantity = -1;
                TrailerScene.Init();
                TrailerScene.Update();
                TrailerScene.Render();
            }

            PalletDemonstrationScene.ModelList.Clear();
            PalletDemonstrationScene.ModelListNumberPallet.Clear();
            PalletDemonstrationScene.PalletQuantity = -1;
            PalletDemonstrationScene.Init();
            PalletDemonstrationScene.Update();
            PalletDemonstrationScene.Render();

        }

        private void DeletePalleteButton_Click(object sender, RoutedEventArgs e)
        {
            //DeleteLastPallet();

            DeleteLastPalletAuto();

            UpdateActions(SelectedItem);
            UpdateActions();
        }

        private void DemoShemeOne_Click(object sender, RoutedEventArgs e)
        {
            ClearScene();
            DemoShemeOneRun();
        }

        private void GetShemeButton_Click(object sender, RoutedEventArgs e)
        {
            ClearScene();
            TryGetTableLoadingShemeItems();
        }

        public void ClearNonPlacedObjects()
        {
            foreach (var item in Ds.Items)
            {
                if (item.CheckGet("PLACED") != "1")
                {
                    item.CheckAdd("LEFT", "");
                    item.CheckAdd("TOP", "");
                    item.CheckAdd("CAR", "");
                    item.CheckAdd("INDEXNUMBER", "");
                    item.CheckAdd("ROTATED", "");
                    item.CheckAdd("PLACED", "");
                    item.CheckAdd("HEIGHTINDEXPLACE", "");
                }
            }

            if (TruckScene.ModelList.FirstOrDefault(x => x.Value.ListIndex == IndexNumber + 1).Key != null)
            {
                var list = TruckScene.ModelList.FirstOrDefault(x => x.Value.ListIndex == IndexNumber + 1);

                ClearBorders(list);
                TruckScene.ModelList.Remove(list.Key);
                TruckScene.ModelListNumberPallet.Remove(list.Key);

                TruckScene.Update();
                TruckScene.Render();
            }

            if (TrailerScene.ModelList.FirstOrDefault(x => x.Value.ListIndex == IndexNumber + 1).Key != null)
            {
                var list = TrailerScene.ModelList.FirstOrDefault(x => x.Value.ListIndex == IndexNumber + 1);

                ClearBorders(list);
                TrailerScene.ModelList.Remove(list.Key);
                TrailerScene.ModelListNumberPallet.Remove(list.Key);

                TrailerScene.Update();
                TrailerScene.Render();
            }
        }

        /// <summary>
        /// Запускает тестирование размещённых в транспорте объектов
        /// Если размещённые кубики пересекают границу транспорта или других кубиков, то вернёт false
        /// </summary>
        public bool TestSheme(bool showResultMessage = true)
        {
            bool result = true;
            
            // Проверка фургона
            {
                List<KeyValuePair<string, Box>> placedBoxes = new List<KeyValuePair<string, Box>>();
                // Получаем все размещённые кубики в фургоне
                foreach (var model in TruckScene.ModelList)
                {
                    if (model.Value.ListIndex > 0)
                    {
                        model.Value.Valid = true;
                        placedBoxes.Add(model);
                    }
                }

                Central.Parameters.GlobalDebugOutput = false;
                string msg = "";
                msg = $"{msg}{Environment.NewLine}{placedBoxes.Count}";

                Rect currentRect = new Rect();
                Rect otherRect = new Rect();
                Rect resultRect = new Rect();
                msg = $"{msg}{Environment.NewLine}Начинаем цикл";
                for (int i = 0; i < placedBoxes.Count - 1; i++)
                {
                    msg = $"{msg}{Environment.NewLine}i={i}";
                    var curentPlacedBox = placedBoxes[i];
                    msg = $"{msg}{Environment.NewLine}curentPlacedBox.Value.Left={curentPlacedBox.Value.Left}";
                    msg = $"{msg}{Environment.NewLine}curentPlacedBox.Value.Length={curentPlacedBox.Value.Length}";
                    msg = $"{msg}{Environment.NewLine}curentPlacedBox.Value.Top={curentPlacedBox.Value.Top}";
                    msg = $"{msg}{Environment.NewLine}curentPlacedBox.Value.Width={curentPlacedBox.Value.Width}";

                    msg = $"{msg}{Environment.NewLine} Проверяем, что выбранный кубик не выходит за границы фургона";
                    // Проверяем, что выбранный кубик не выходит за границы фургона
                    if (
                        curentPlacedBox.Value.Left < TruckScene.Car.Left
                        || curentPlacedBox.Value.Top < TruckScene.Car.Top
                        || (curentPlacedBox.Value.Left + curentPlacedBox.Value.Length) > (TruckScene.Car.Left + TruckScene.Car.Length)
                        || (curentPlacedBox.Value.Top + curentPlacedBox.Value.Width) > (TruckScene.Car.Top + TruckScene.Car.Width)
                        || (curentPlacedBox.Value.OnHigh + curentPlacedBox.Value.Height) > (TruckScene.Car.Height)
                        )
                    {
                        curentPlacedBox.Value.Valid = false;
                        result = false;
                        msg = $"{msg}{Environment.NewLine}кубик выходит за границы фургона";
                    }
                    msg = $"{msg}{Environment.NewLine}Valid={curentPlacedBox.Value.Valid}";

                    currentRect.Location = new Point(curentPlacedBox.Value.Left, curentPlacedBox.Value.Top);
                    currentRect.Size = new Size(curentPlacedBox.Value.Length, curentPlacedBox.Value.Width);

                    msg = $"{msg}{Environment.NewLine}Проверяем, что выбранный кубик не пересекает границы других кубиков";
                    // Проверяем, что выбранный кубик не пересекает границы других кубиков
                    for (int j = i + 1 ; j < placedBoxes.Count; j++)
                    {
                        msg = $"{msg}{Environment.NewLine}j={j}";

                        var otherPlacedBox = placedBoxes[j];
                        msg = $"{msg}{Environment.NewLine}otherPlacedBox.Value.Left={otherPlacedBox.Value.Left}";
                        msg = $"{msg}{Environment.NewLine}otherPlacedBox.Value.Length={otherPlacedBox.Value.Length}";
                        msg = $"{msg}{Environment.NewLine}otherPlacedBox.Value.Top={otherPlacedBox.Value.Top}";
                        msg = $"{msg}{Environment.NewLine}otherPlacedBox.Value.Width={otherPlacedBox.Value.Width}";

                        otherRect.Location = new Point(otherPlacedBox.Value.Left, otherPlacedBox.Value.Top);
                        otherRect.Size = new Size(otherPlacedBox.Value.Length, otherPlacedBox.Value.Width);

                        resultRect = Rect.Intersect(currentRect, otherRect);

                        if (resultRect.IsEmpty || (resultRect.Size.Width * resultRect.Size.Height) == 0 || 
                            (curentPlacedBox.Value.Left == otherPlacedBox.Value.Left
                            && curentPlacedBox.Value.Top == otherPlacedBox.Value.Top
                            && curentPlacedBox.Value.OnHigh != otherPlacedBox.Value.OnHigh)
                            )
                        {

                        }
                        else
                        {
                            curentPlacedBox.Value.Valid = false;
                            result = false;
                            msg = $"{msg}{Environment.NewLine}кубик выходит за границы других кубиков";
                        }

                        msg = $"{msg}{Environment.NewLine}Valid={curentPlacedBox.Value.Valid}";
                        msg = $"{msg}{Environment.NewLine}";
                    }

                    msg = $"{msg}{Environment.NewLine}";
                    msg = $"{msg}{Environment.NewLine}";
                }

                Central.Dbg(msg);

                TruckScene.Render();
            }

            // Проверка прицепа
            {
                if (TrailerScene != null && TrailerScene.Car != null)
                {
                    List<KeyValuePair<string, Box>> placedBoxes = new List<KeyValuePair<string, Box>>();
                    // Получаем все размещённые кубики в прицепа
                    foreach (var model in TrailerScene.ModelList)
                    {
                        if (model.Value.ListIndex > 0)
                        {
                            model.Value.Valid = true;
                            placedBoxes.Add(model);
                        }
                    }

                    Central.Parameters.GlobalDebugOutput = false;
                    string msg = "";
                    msg = $"{msg}{Environment.NewLine}{placedBoxes.Count}";

                    Rect currentRect = new Rect();
                    Rect otherRect = new Rect();
                    Rect resultRect = new Rect();
                    msg = $"{msg}{Environment.NewLine}Начинаем цикл";
                    for (int i = 0; i < placedBoxes.Count - 1; i++)
                    {
                        msg = $"{msg}{Environment.NewLine}i={i}";
                        var curentPlacedBox = placedBoxes[i];
                        msg = $"{msg}{Environment.NewLine}curentPlacedBox.Value.Left={curentPlacedBox.Value.Left}";
                        msg = $"{msg}{Environment.NewLine}curentPlacedBox.Value.Length={curentPlacedBox.Value.Length}";
                        msg = $"{msg}{Environment.NewLine}curentPlacedBox.Value.Top={curentPlacedBox.Value.Top}";
                        msg = $"{msg}{Environment.NewLine}curentPlacedBox.Value.Width={curentPlacedBox.Value.Width}";

                        msg = $"{msg}{Environment.NewLine} Проверяем, что выбранный кубик не выходит за границы прицепа";
                        // Проверяем, что выбранный кубик не выходит за границы прицепа
                        if (
                            curentPlacedBox.Value.Left < TrailerScene.Car.Left
                            || curentPlacedBox.Value.Top < TrailerScene.Car.Top
                            || (curentPlacedBox.Value.Left + curentPlacedBox.Value.Length) > (TrailerScene.Car.Left + TrailerScene.Car.Length)
                            || (curentPlacedBox.Value.Top + curentPlacedBox.Value.Width) > (TrailerScene.Car.Top + TrailerScene.Car.Width)
                            || (curentPlacedBox.Value.OnHigh + curentPlacedBox.Value.Height) > (TrailerScene.Car.Height)
                            )
                        {
                            curentPlacedBox.Value.Valid = false;
                            result = false;
                            msg = $"{msg}{Environment.NewLine}кубик выходит за границы прицепа";
                        }
                        msg = $"{msg}{Environment.NewLine}Valid={curentPlacedBox.Value.Valid}";

                        currentRect.Location = new Point(curentPlacedBox.Value.Left, curentPlacedBox.Value.Top);
                        currentRect.Size = new Size(curentPlacedBox.Value.Length, curentPlacedBox.Value.Width);

                        msg = $"{msg}{Environment.NewLine}Проверяем, что выбранный кубик не пересекает границы других кубиков";
                        // Проверяем, что выбранный кубик не пересекает границы других кубиков
                        for (int j = i + 1; j < placedBoxes.Count; j++)
                        {
                            msg = $"{msg}{Environment.NewLine}j={j}";

                            var otherPlacedBox = placedBoxes[j];
                            msg = $"{msg}{Environment.NewLine}otherPlacedBox.Value.Left={otherPlacedBox.Value.Left}";
                            msg = $"{msg}{Environment.NewLine}otherPlacedBox.Value.Length={otherPlacedBox.Value.Length}";
                            msg = $"{msg}{Environment.NewLine}otherPlacedBox.Value.Top={otherPlacedBox.Value.Top}";
                            msg = $"{msg}{Environment.NewLine}otherPlacedBox.Value.Width={otherPlacedBox.Value.Width}";

                            otherRect.Location = new Point(otherPlacedBox.Value.Left, otherPlacedBox.Value.Top);
                            otherRect.Size = new Size(otherPlacedBox.Value.Length, otherPlacedBox.Value.Width);

                            resultRect = Rect.Intersect(currentRect, otherRect);

                            if (resultRect.IsEmpty || (resultRect.Size.Width * resultRect.Size.Height) == 0 ||
                                (curentPlacedBox.Value.Left == otherPlacedBox.Value.Left
                                && curentPlacedBox.Value.Top == otherPlacedBox.Value.Top
                                && curentPlacedBox.Value.OnHigh != otherPlacedBox.Value.OnHigh)
                                )
                            {

                            }
                            else
                            {
                                curentPlacedBox.Value.Valid = false;
                                result = false;
                                msg = $"{msg}{Environment.NewLine}кубик выходит за границы других кубиков";
                            }

                            msg = $"{msg}{Environment.NewLine}Valid={curentPlacedBox.Value.Valid}";
                            msg = $"{msg}{Environment.NewLine}";
                        }

                        msg = $"{msg}{Environment.NewLine}";
                        msg = $"{msg}{Environment.NewLine}";
                    }

                    Central.Dbg(msg);

                    TrailerScene.Render();
                }
            }

            if (showResultMessage)
            {
                if (result)
                {
                    string msg = $"Проверка пройдена успешно.{Environment.NewLine}Все поддоны расположены корректно.";
                    var d = new DialogWindow($"{msg}", "Проверка порядка загрузки", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    string msg = $"Проверка не пройдена!{Environment.NewLine}Не все поддоны расположены корректно.{Environment.NewLine}Измените расположение выделенных поддонов.";
                    var d = new DialogWindow($"{msg}", "Проверка порядка загрузки", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }

            return result;
        }

        private void Left_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(LeftCoordinateTextBox.Text) && !string.IsNullOrEmpty(TopCoordinateTextBox.Text))
            {
                OKButton.IsEnabled = true;
            }
            else
            {
                OKButton.IsEnabled = false;
            }
        }

        private void Top_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(LeftCoordinateTextBox.Text) && !string.IsNullOrEmpty(TopCoordinateTextBox.Text))
            {
                OKButton.IsEnabled = true;
            }
            else
            {
                OKButton.IsEnabled = false;
            }
        }

        public void CancelPlace()
        {
            OKButton.Style = (Style)OKButton.TryFindResource("Button");

            CheckedRow = null;

            LeftCoordinateTextBox.Text = "";
            TopCoordinateTextBox.Text = "";
            Rotated.IsChecked = false;

            TruckScene.UIPointContainer.Children.Clear();
            TruckScene.UIPointBoxList.Clear();
            if (TrailerScene != null)
            {
                TrailerScene.UIPointContainer.Children.Clear();
                TrailerScene.UIPointBoxList.Clear();
            }

            foreach (var item in Ds.Items)
            {
                if (item.CheckGet("PLACED") != "1")
                {
                    item.CheckAdd("LEFT", "");
                    item.CheckAdd("TOP", "");
                    item.CheckAdd("CAR", "");
                    item.CheckAdd("INDEXNUMBER", "");
                    item.CheckAdd("ROTATED", "");
                    item.CheckAdd("PLACED", "");
                    item.CheckAdd("HEIGHTINDEXPLACE", "");
                }
            }

            Grid.UpdateItems(Ds);

            PositionGrid.Visibility = Visibility.Hidden;

            CheckMaxIndexNumber();

            if (TruckScene.ModelList.FirstOrDefault(x => x.Value.ListIndex == IndexNumber + 1).Key != null)
            {
                var list = TruckScene.ModelList.FirstOrDefault(x => x.Value.ListIndex == IndexNumber + 1);

                ClearBorders(list);
                TruckScene.ModelList.Remove(list.Key);
                TruckScene.ModelListNumberPallet.Remove(list.Key);

                TruckScene.Update();
                TruckScene.Render();
            }

            if (TrailerScene.ModelList.FirstOrDefault(x => x.Value.ListIndex == IndexNumber + 1).Key != null)
            {
                var list = TrailerScene.ModelList.FirstOrDefault(x => x.Value.ListIndex == IndexNumber + 1);

                ClearBorders(list);
                TrailerScene.ModelList.Remove(list.Key);
                TrailerScene.ModelListNumberPallet.Remove(list.Key);

                TrailerScene.Update();
                TrailerScene.Render();
            }

            FormStatus.Text = "";

            UpdateActions();
        }

        private void CancelPlaceButton_Click(object sender, RoutedEventArgs e)
        {
            CancelPlace();
        }

        /// <summary>
        /// Обработчик нажатия на чекбокс для расположения в прицепе
        /// Пересчёт поинтов для выбранного транспорта,
        /// Удаление не расположенных объектов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CarCheckBox_Click(object sender, RoutedEventArgs e)
        {
            //string c = (CarCheckBox.IsChecked.ToInt() + 1).ToString();
            string c = CarForPlaced.ToString();

            foreach (var item in Ds.Items)
            {
                if (item.CheckGet("PLACED") != "1")
                {
                    item.CheckAdd("LEFT", "");
                    item.CheckAdd("TOP", "");
                    item.CheckAdd("CAR", "");
                    item.CheckAdd("INDEXNUMBER", "");
                    item.CheckAdd("ROTATED", "");
                    item.CheckAdd("PLACED", "");
                    item.CheckAdd("HEIGHTINDEXPLACE", "");
                }
            }

            CheckMaxIndexNumber();

            if (TruckScene.ModelList.FirstOrDefault(x => x.Value.ListIndex == IndexNumber + 1).Key != null)
            {
                var list = TruckScene.ModelList.FirstOrDefault(x => x.Value.ListIndex == IndexNumber + 1);

                ClearBorders(list);
                TruckScene.ModelList.Remove(list.Key);
                TruckScene.ModelListNumberPallet.Remove(list.Key);

                TruckScene.Update();
                TruckScene.Render();
            }

            if (TrailerScene.ModelList.FirstOrDefault(x => x.Value.ListIndex == IndexNumber + 1).Key != null)
            {
                var list = TrailerScene.ModelList.FirstOrDefault(x => x.Value.ListIndex == IndexNumber + 1);

                ClearBorders(list);
                TrailerScene.ModelList.Remove(list.Key);
                TrailerScene.ModelListNumberPallet.Remove(list.Key);

                TrailerScene.Update();
                TrailerScene.Render();
            }

            IndexNumber += 1;

            Grid.UpdateItems(Ds);

            SetUIPoints(c);
        }

        public void TransportRadioButtonClick()
        {
            string c = CarForPlaced.ToString();

            foreach (var item in Ds.Items)
            {
                if (item.CheckGet("PLACED") != "1")
                {
                    item.CheckAdd("LEFT", "");
                    item.CheckAdd("TOP", "");
                    item.CheckAdd("CAR", "");
                    item.CheckAdd("INDEXNUMBER", "");
                    item.CheckAdd("ROTATED", "");
                    item.CheckAdd("PLACED", "");
                    item.CheckAdd("HEIGHTINDEXPLACE", "");
                }
            }

            CheckMaxIndexNumber();

            if (TruckScene.ModelList.FirstOrDefault(x => x.Value.ListIndex == IndexNumber + 1).Key != null)
            {
                var list = TruckScene.ModelList.FirstOrDefault(x => x.Value.ListIndex == IndexNumber + 1);

                ClearBorders(list);
                TruckScene.ModelList.Remove(list.Key);
                TruckScene.ModelListNumberPallet.Remove(list.Key);

                TruckScene.Update();
                TruckScene.Render();
            }

            if (TrailerScene.ModelList.FirstOrDefault(x => x.Value.ListIndex == IndexNumber + 1).Key != null)
            {
                var list = TrailerScene.ModelList.FirstOrDefault(x => x.Value.ListIndex == IndexNumber + 1);

                ClearBorders(list);
                TrailerScene.ModelList.Remove(list.Key);
                TrailerScene.ModelListNumberPallet.Remove(list.Key);

                TrailerScene.Update();
                TrailerScene.Render();
            }

            IndexNumber += 1;

            Grid.UpdateItems(Ds);

            SetUIPoints(c);
        }

        private void TruckRadioButton_Click(object sender, RoutedEventArgs e)
        {
            TrailerRadioButton.IsChecked = false;
            var checkedTruckRB = (bool)TruckRadioButton.IsChecked;
            var checkedTrailerRB = (bool)TrailerRadioButton.IsChecked;

            if (checkedTruckRB)
            {
                CarForPlaced = 1;
                TransportRadioButtonClick();
            }
        }

        private void TrailerRadioButton_Click(object sender, RoutedEventArgs e)
        {
            TruckRadioButton.IsChecked = false;
            var checkedTruckRB = (bool)TruckRadioButton.IsChecked;
            var checkedTrailerRB = (bool)TrailerRadioButton.IsChecked;

            if (checkedTrailerRB)
            {
                CarForPlaced = 2;
                TransportRadioButtonClick();
            }
        }

        private void TestShemeButton_Click(object sender, RoutedEventArgs e)
        {
            TestSheme();
        }

        private void RigthButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in Ds.Items)
            {
                if (item == CheckedRow)
                {
                    int leftValue = Form.GetValueByPath("LEFT").ToInt();

                    int carWidth = 0;
                    int carLength = 0;
                    if (item.CheckGet("CAR") == "1")
                    {
                        carWidth = item.CheckGet("CAR_B").ToInt();
                        carLength = item.CheckGet("CAR_L").ToInt();
                    }
                    else if (item.CheckGet("CAR") == "2")
                    {
                        carWidth = item.CheckGet("TRAILER_B").ToInt();
                        carLength = item.CheckGet("TRAILER_L").ToInt();
                    }

                    int palletWidth = 0;
                    int palletLength = 0;
                    if (item.CheckGet("ROTATED") == "0")
                    {
                        palletWidth = item.CheckGet("BT").ToInt();
                        palletLength = item.CheckGet("LT").ToInt();

                    }
                    else if (item.CheckGet("ROTATED") == "1")
                    {
                        palletWidth = item.CheckGet("LT").ToInt();
                        palletLength = item.CheckGet("BT").ToInt();
                    }

                    if (leftValue + palletLength < carLength)
                    {
                        if (leftValue + palletLength + Form.GetValueByPath("STEP_OF_MOVEMENT").ToInt() <= carLength)
                        {
                            leftValue = leftValue + Form.GetValueByPath("STEP_OF_MOVEMENT").ToInt();
                        }
                        else
                        {
                            leftValue = carLength - palletLength;
                        }

                        string left = leftValue.ToString();
                        item.CheckAdd("LEFT", left);
                        Grid.UpdateItems(Ds);
                        LeftCoordinateTextBox.Text = left;

                        if (!string.IsNullOrEmpty(item.CheckGet("LEFT")) && !string.IsNullOrEmpty(item.CheckGet("TOP")))
                        {
                            AddPallet(item);
                        }
                    }
                }
            }
        }

        private void LeftButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in Ds.Items)
            {
                if (item == CheckedRow)
                {
                    int leftValue = Form.GetValueByPath("LEFT").ToInt();
                    if (leftValue > 0)
                    {
                        if (leftValue - Form.GetValueByPath("STEP_OF_MOVEMENT").ToInt() >= 0)
                        {
                            leftValue = leftValue - Form.GetValueByPath("STEP_OF_MOVEMENT").ToInt();
                        }
                        else
                        {
                            leftValue = 0;
                        }

                        string left = leftValue.ToString();
                        item.CheckAdd("LEFT", left);
                        Grid.UpdateItems(Ds);
                        LeftCoordinateTextBox.Text = left;

                        if (!string.IsNullOrEmpty(item.CheckGet("LEFT")) && !string.IsNullOrEmpty(item.CheckGet("TOP")))
                        {
                            AddPallet(item);
                        }
                    }
                }
            }
        }

        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in Ds.Items)
            {
                if (item == CheckedRow)
                {
                    int topValue = Form.GetValueByPath("TOP").ToInt();

                    int carWidth = 0;
                    int carLength = 0;
                    if (item.CheckGet("CAR") == "1")
                    {
                        carWidth = item.CheckGet("CAR_B").ToInt();
                        carLength = item.CheckGet("CAR_L").ToInt();
                    }
                    else if (item.CheckGet("CAR") == "2")
                    {
                        carWidth = item.CheckGet("TRAILER_B").ToInt();
                        carLength = item.CheckGet("TRAILER_L").ToInt();
                    }

                    int palletWidth = 0;
                    int palletLength = 0;
                    if (item.CheckGet("ROTATED") == "0")
                    {
                        palletWidth = item.CheckGet("BT").ToInt();
                        palletLength = item.CheckGet("LT").ToInt();

                    }
                    else if (item.CheckGet("ROTATED") == "1")
                    {
                        palletWidth = item.CheckGet("LT").ToInt();
                        palletLength = item.CheckGet("BT").ToInt();
                    }

                    if (topValue + palletWidth < carWidth)
                    {
                        if (topValue + palletWidth + Form.GetValueByPath("STEP_OF_MOVEMENT").ToInt() <= carWidth)
                        {
                            topValue = topValue + Form.GetValueByPath("STEP_OF_MOVEMENT").ToInt();
                        }
                        else
                        {
                            topValue = carWidth - palletWidth;
                        }

                        string top = topValue.ToString();
                        item.CheckAdd("TOP", top);
                        Grid.UpdateItems(Ds);
                        TopCoordinateTextBox.Text = top;

                        if (!string.IsNullOrEmpty(item.CheckGet("LEFT")) && !string.IsNullOrEmpty(item.CheckGet("TOP")))
                        {
                            AddPallet(item);
                        }
                    }
                }
            }
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in Ds.Items)
            {
                if (item == CheckedRow)
                {
                    int topValue = Form.GetValueByPath("TOP").ToInt();
                    if (topValue > 0)
                    {
                        if (topValue - Form.GetValueByPath("STEP_OF_MOVEMENT").ToInt() >= 0)
                        {
                            topValue = topValue - Form.GetValueByPath("STEP_OF_MOVEMENT").ToInt();
                        }
                        else
                        {
                            topValue = 0;
                        }

                        string top = topValue.ToString();
                        item.CheckAdd("TOP", top);
                        Grid.UpdateItems(Ds);
                        TopCoordinateTextBox.Text = top;

                        if (!string.IsNullOrEmpty(item.CheckGet("LEFT")) && !string.IsNullOrEmpty(item.CheckGet("TOP")))
                        {
                            AddPallet(item);
                        }
                    }
                }
            }
        }

        public void SetPresetCameraPosition(Scene currentScene, int presetNumber)
        {
            if (currentScene != null)
            {
                currentScene.PresetCameraNumber = presetNumber;
                currentScene.Init();
                currentScene.Update();
                currentScene.Render();
            }
        }

        public async void GetSavedSchemeImage()
        {
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID", $"{ShipmentId}");
                p.CheckAdd("DEMO", "0");
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Loading");
            q.Request.SetParam("Action", "GetMap");

            q.Request.Timeout = 15000;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.Request.SetParams(p);

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

        private void CameraFirstPositionButton_Click(object sender, RoutedEventArgs e)
        {
            // Просмотр спереди
            SetPresetCameraPosition(TruckScene, 1);
        }

        private void CameraThirdPositionButton_Click(object sender, RoutedEventArgs e)
        {
            // Просмотр сзади
            SetPresetCameraPosition(TruckScene, 3);
        }

        private void CameraSecondPositionButton_Click(object sender, RoutedEventArgs e)
        {
            // Просмотр сверху
            SetPresetCameraPosition(TruckScene, 2);
        }

        private void DeleteManuallySchemeButton_Click(object sender, RoutedEventArgs e)
        {
            string msg = $"Внимание! Вы удаляете ручное размещение.{Environment.NewLine}" +
                $"При генерации файла схемы погрузки будет использован алгоритм автоматического размещения.{Environment.NewLine}" +
                $"Продолжить?";
            var d = new DialogWindow($"{msg}", "Удаление порядка загрузки", "", DialogWindowButtons.YesNo);
            if (d.ShowDialog() == true)
            {
                DeleteManuallyScheme();
            }
        }

        private void CameraFourthPositionButton_Click(object sender, RoutedEventArgs e)
        {
            // Просмотр сбоку
            SetPresetCameraPosition(TruckScene, 4);
        }

        private void GetSavedSchemeImageButton_Click(object sender, RoutedEventArgs e)
        {
            GetSavedSchemeImage();
        }
    }


    /// <summary>
    /// универсальный объект
    /// </summary>
    public class Box
    {
        /*
                                  |
            points ->    mesh ->  |  model 
                      material->  |
                                  |

            points = CreatePoints
            mesh = CreateMesh
            model=CreateModel
         
         */

        public Box()
        {
            Width = 0;
            Length = 0;
            Height = 0;
            Left = 0;
            Top = 0;
            Initialized = false;

            OnHigh = 0;
            HasHead = 0;

            HeightIndexPlace = 1;

            Boxes = new List<Box>();

            PointRotationOption = 0;
            BoxRotated = 0;
            Valid = true;

            // Красный
            CollorError = "#ff0000";

            PositionItemData = new Dictionary<string, string>();
        }

        public Box Copy()
        {
            Box box = new Box();

            box.Width = Width;
            box.Length = Length;
            box.Height = Height;
            box.Left = Left;
            box.Top = Top;
            box.Initialized = Initialized;
            box.CollorInner = CollorInner;
            box.CarFlag = CarFlag;
            box.OnHigh = OnHigh;
            box.HasHead = HasHead;
            box.ListIndex = ListIndex;
            box.NumberProduct = NumberProduct;
            box.RowIndexPlace = RowIndexPlace;
            box.ColumnIndexPlace = ColumnIndexPlace;
            box.HeightIndexPlace = HeightIndexPlace;
            box.PlacedCar = PlacedCar;
            box.PointRotationOption = PointRotationOption;
            box.BoxRotated = BoxRotated;
            box.Valid = Valid;
            box.Boxes = new List<Box>();
            foreach (var borderBox in Boxes)
            {
                box.Boxes.Add(borderBox.Copy());
            }

            //box.BoxModelUIElement3D = BoxModelUIElement3D;
            //box.PositionItemData = PositionItemData;

            return box;
        }

        /// <summary>
        /// ширина, мм
        /// Фактическая ширина кубика. 
        /// Если поддон повёрнут, то равна длине поддона;
        /// если не повёрнут, то равна ширине поддона.
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// длина, мм
        /// Фактическая длина кубика.
        /// Если поддон повёрнут, то равна ширине поддона;
        /// если не повёрнут, то равна длине поддона.
        /// </summary>
        public int Length { get; set; }
        /// <summary>
        /// высота, мм
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// координата x
        /// </summary>
        public int Left { get; set; }

        /// <summary>
        /// координата y
        /// </summary>
        public int Top { get; set; }

        /// <summary>
        /// все параметры проинициализированы, объект готов к работе
        /// </summary>
        public bool Initialized { get; set; }

        /// <summary>
        /// Внешний цвет объекта
        /// </summary>
        public string CollorOuter
        {
            get
            {
                if (Valid)
                {
                    return CollorInner;
                }
                else
                {
                    return CollorError;
                }
            }
        }

        /// <summary>
        /// Цвет бокса, если Valid = true.
        /// Цвет поддона в зависимости от его очерёдности.
        /// </summary>
        public string CollorInner { get; set; }

        /// <summary>
        /// Цвет бокса, если Valid = false.
        /// Цвет ошибки, который указывает, что этот поддон пересекает границу другого поддона или транспорта.
        /// </summary>
        public string CollorError { get; }

        /// <summary>
        /// Если true, значит фура
        /// В зависимости от состояния флага выбирается mesh объекта
        /// (true - вывернут наизнанку, false - обычный)
        /// </summary>
        public bool CarFlag { get; set; }

        /// <summary>
        /// расположение дна объекта по высоте
        /// если больше 0, то объект находится не на полу
        /// используется для вызова CreateModel(H)
        /// </summary>
        public int OnHigh { get; set; }

        /// <summary>
        /// Флаг того, что на поддоне уже размещён ещё один поддон
        /// используетс дляопределения верхних точек стыковки
        /// если 1, значит на нём уже стоит другой поддон и новый разместить нельзя
        /// </summary>
        public int HasHead { get; set; }

        /// <summary>
        /// номер поддона при размещении
        /// </summary>
        public int ListIndex { get; set; }

        /// <summary>
        /// номер продукции для отображения на крышке бокса
        /// </summary>
        public int NumberProduct { get; set; }

        /// <summary>
        /// набор боксов, которые являются оконтовкой этого элемента
        /// </summary>
        public List<Box> Boxes { get; set; }

        /// <summary>
        /// Условный номер ряда, в котором расположен поддон
        /// </summary>
        public int RowIndexPlace { get; set; }

        /// <summary>
        /// Условный номер колонки, в котором расположен поддон
        /// </summary>
        public int ColumnIndexPlace { get; set; }

        /// <summary>
        /// Условный номер яруса, в котором расположен поддон
        /// </summary>
        public int HeightIndexPlace { get; set; }

        /// <summary>
        /// Место размещения
        /// "1" - Фура, "2" - Прицеп
        /// </summary>
        public string PlacedCar { get; set; }

        /// <summary>
        /// Варианты поворота размещаемого поддона в выбранной точке.
        /// 0 -- Rotated = 0 (Горизонтальное размещение. Блокируем чекбокс поворота поддона);
        /// 1 -- Rotated = 1 (Вертикальное размещение. Блокируем чекбокс поворота поддона);
        /// 2 -- Rotated = Form.GetValueByPath("Rotated") (Можем разместить поддон и горизонтально и вертикально. Даём пользователю выбрать через чекбокс поворота поддона).
        /// </summary>
        public int PointRotationOption { get; set; }

        /// <summary>
        /// Флаг того, что поддон повёрнут
        /// </summary>
        public int BoxRotated { get; set; }

        /// <summary>
        /// Флаг того, что поддон не пересекает границы других поддонов и транспорта
        /// </summary>
        public bool Valid { get; set; }

        /// <summary>
        /// Объект ModelUIElement3D на сцене, который соответсвтует этому Box
        /// </summary>
        public ModelUIElement3D BoxModelUIElement3D { get; set; }

        /// <summary>
        /// Данные стоки грида, по которой был создан этот Box
        /// </summary>
        public Dictionary<string, string> PositionItemData { get; set; }

        public void Init()
        {
            if (
                Width > 0
                && Length > 0
                && Height > 0
            )
            {
                Initialized = true;
            }
        }

        /// <summary>
        /// создание коллекции точек
        /// </summary>
        /// <returns></returns>
        public Point3DCollection CreatePoints()
        {
            var points = new Point3DCollection();

            /*
               центр -- слева вверху
             */

            //1
            {
                var p = new Point3D();
                p.X = Left;
                p.Y = 0;
                p.Z = Top + Width;
                points.Add(p);
            }

            //2
            {
                var p = new Point3D();
                p.X = Left;
                p.Y = 0;
                p.Z = Top;
                points.Add(p);
            }

            //3
            {
                var p = new Point3D();
                p.X = Left + Length;
                p.Y = 0;
                p.Z = Top;
                points.Add(p);
            }

            //4
            {
                var p = new Point3D();
                p.X = Left + Length;
                p.Y = 0;
                p.Z = Top + Width;
                points.Add(p);
            }

            //5
            {
                var p = new Point3D();
                p.X = Left;
                p.Y = Height;
                p.Z = Top + Width;
                points.Add(p);
            }

            //6
            {
                var p = new Point3D();
                p.X = Left;
                p.Y = Height;
                p.Z = Top;
                points.Add(p);
            }

            //7
            {
                var p = new Point3D();
                p.X = Left + Length;
                p.Y = Height;
                p.Z = Top;
                points.Add(p);
            }

            //8
            {
                var p = new Point3D();
                p.X = Left + Length;
                p.Y = Height;
                p.Z = Top + Width;
                points.Add(p);
            }

            return points;
        }

        /// <summary>
        /// создание коллекции точек на высоте H
        /// </summary>
        /// <param name="H">Высота нижней точки</param>
        /// <returns></returns>
        public Point3DCollection CreatePoints(int H)
        {
            var points = new Point3DCollection();

            /*
               центр -- слева вверху
             */

            //1
            {
                var p = new Point3D();
                p.X = Left;
                p.Y = H;
                p.Z = Top + Width;
                points.Add(p);
            }

            //2
            {
                var p = new Point3D();
                p.X = Left;
                p.Y = H;
                p.Z = Top;
                points.Add(p);
            }

            //3
            {
                var p = new Point3D();
                p.X = Left + Length;
                p.Y = H;
                p.Z = Top;
                points.Add(p);
            }

            //4
            {
                var p = new Point3D();
                p.X = Left + Length;
                p.Y = H;
                p.Z = Top + Width;
                points.Add(p);
            }

            //5
            {
                var p = new Point3D();
                p.X = Left;
                p.Y = H + Height;
                p.Z = Top + Width;
                points.Add(p);
            }

            //6
            {
                var p = new Point3D();
                p.X = Left;
                p.Y = H + Height;
                p.Z = Top;
                points.Add(p);
            }

            //7
            {
                var p = new Point3D();
                p.X = Left + Length;
                p.Y = H + Height;
                p.Z = Top;
                points.Add(p);
            }

            //8
            {
                var p = new Point3D();
                p.X = Left + Length;
                p.Y = H + Height;
                p.Z = Top + Width;
                points.Add(p);
            }

            return points;
        }


        /// <summary>
        /// создание меша
        /// (вывернутый наизнанку)
        /// </summary>
        /// <returns></returns>
        public MeshGeometry3D CreateMeshInverted()
        {
            var points = CreatePoints();

            var mesh = new MeshGeometry3D();
            mesh.Positions = points;

            var c = new Int32Collection();

            //Задняя стенка
            c.Add(0);
            c.Add(4);
            c.Add(3);

            c.Add(3);
            c.Add(4);
            c.Add(7);

            //Левая стенка
            c.Add(0);
            c.Add(1);
            c.Add(4);

            c.Add(4);
            c.Add(1);
            c.Add(5);

            //Нижняя стенка
            c.Add(0);
            c.Add(3);
            c.Add(1);

            c.Add(3);
            c.Add(2);
            c.Add(1);

            //Правая стенка
            c.Add(3);
            c.Add(6);
            c.Add(2);

            c.Add(3);
            c.Add(7);
            c.Add(6);

            //Передняя стенка
            c.Add(1);
            c.Add(2);
            c.Add(5);

            c.Add(6);
            c.Add(5);
            c.Add(2);

            //Верхняя стенка
            c.Add(4);
            c.Add(5);
            c.Add(7);

            c.Add(7);
            c.Add(5);
            c.Add(6);

            mesh.TriangleIndices = c;

            return mesh;
        }

        /// <summary>
        /// создание меша
        /// </summary>
        /// <returns></returns>
        public MeshGeometry3D CreateMesh()
        {
            var points = CreatePoints();

            var mesh = new MeshGeometry3D();
            mesh.Positions = points;

            var c = new Int32Collection();

            //Задняя стенка
            c.Add(0);
            c.Add(3);
            c.Add(4);

            c.Add(3);
            c.Add(7);
            c.Add(4);

            //Левая стенка
            c.Add(0);
            c.Add(4);
            c.Add(1);

            c.Add(4);
            c.Add(5);
            c.Add(1);

            //Нижняя стенка
            c.Add(0);
            c.Add(1);
            c.Add(3);

            c.Add(3);
            c.Add(1);
            c.Add(2);

            //Правая стенка
            c.Add(3);
            c.Add(2);
            c.Add(6);

            c.Add(3);
            c.Add(6);
            c.Add(7);

            //Передняя стенка
            c.Add(1);
            c.Add(5);
            c.Add(2);

            c.Add(6);
            c.Add(2);
            c.Add(5);

            //Верхняя стенка
            c.Add(4);
            c.Add(7);
            c.Add(5);

            c.Add(7);
            c.Add(6);
            c.Add(5);

            mesh.TriangleIndices = c;

            return mesh;
        }

        /// <summary>
        /// создание меша для куба на высоте H
        /// </summary>
        /// <param name="H">Высота нижней точки куба</param>
        /// <returns></returns>
        public MeshGeometry3D CreateMesh(int H)
        {
            var points = CreatePoints(H);

            var mesh = new MeshGeometry3D();
            mesh.Positions = points;

            var c = new Int32Collection();

            //Задняя стенка
            c.Add(0);
            c.Add(3);
            c.Add(4);

            c.Add(3);
            c.Add(7);
            c.Add(4);

            //Левая стенка
            c.Add(0);
            c.Add(4);
            c.Add(1);

            c.Add(4);
            c.Add(5);
            c.Add(1);

            //Нижняя стенка
            c.Add(0);
            c.Add(1);
            c.Add(3);

            c.Add(3);
            c.Add(1);
            c.Add(2);

            //Правая стенка
            c.Add(3);
            c.Add(2);
            c.Add(6);

            c.Add(3);
            c.Add(6);
            c.Add(7);

            //Передняя стенка
            c.Add(1);
            c.Add(5);
            c.Add(2);

            c.Add(6);
            c.Add(2);
            c.Add(5);

            //Верхняя стенка
            c.Add(4);
            c.Add(7);
            c.Add(5);

            c.Add(7);
            c.Add(6);
            c.Add(5);

            mesh.TriangleIndices = c;

            return mesh;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Width"></param>
        /// <param name="Height"></param>
        /// <param name="Length"></param>
        /// <param name="Left"></param>
        /// <param name="Top"></param>
        /// <param name="b"></param>
        /// <param name="OnHigh"></param>
        /// <param name="presetCameraPosition">1 -- взгляд спереди; 2 -- взгляд сверху; 3 -- взгляд сзади</param>
        public void AddCarBorders(int Width, int Height, int Length, int Left, int Top, Box b, int OnHigh, int presetCameraPosition = 1)
        {
            int borderSize = 10;

            // Вертикальные грани
            {
                //1
                {
                    Box Borders = new Box();
                    Borders.Width = borderSize;
                    Borders.Height = Height;
                    Borders.Length = borderSize;
                    Borders.Left = Left - (borderSize / 2);
                    Borders.Top = Top - (borderSize / 2);
                    Borders.OnHigh = OnHigh;
                    Borders.CollorInner = "#000000";
                    Borders.Init();
                    b.Boxes.Add(Borders);
                }
                //2
                {
                    if (presetCameraPosition == 1)
                    {
                        Box Borders = new Box();
                        Borders.Width = borderSize;
                        Borders.Height = Height;
                        Borders.Length = borderSize;
                        Borders.Left = Left + Length - (borderSize / 2);
                        Borders.Top = Top - (borderSize / 2);
                        Borders.OnHigh = OnHigh;
                        Borders.CollorInner = "#000000";
                        Borders.Init();
                        b.Boxes.Add(Borders);
                    }
                }
                //3
                {
                    if (presetCameraPosition == 3)
                    {
                        Box Borders = new Box();
                        Borders.Width = borderSize;
                        Borders.Height = Height;
                        Borders.Length = borderSize;
                        Borders.Left = Left + Length - (borderSize / 2);
                        Borders.Top = Top + Width - (borderSize / 2);
                        Borders.OnHigh = OnHigh;
                        Borders.CollorInner = "#000000";
                        Borders.Init();
                        b.Boxes.Add(Borders);
                    }
                }
                //4
                {
                    Box Borders = new Box();
                    Borders.Width = borderSize;
                    Borders.Height = Height;
                    Borders.Length = borderSize;
                    Borders.Left = Left - (borderSize / 2);
                    Borders.Top = Top + Width - (borderSize / 2);
                    Borders.OnHigh = OnHigh;
                    Borders.CollorInner = "#000000";
                    Borders.Init();
                    b.Boxes.Add(Borders);
                }
            }

            // Грани, уходящие вглубь
            {
                //5
                {
                    Box Borders = new Box();
                    Borders.Width = Width;
                    Borders.Height = borderSize;
                    Borders.Length = borderSize;
                    Borders.Left = Left - (borderSize / 2);
                    Borders.Top = Top - (borderSize / 2);
                    Borders.OnHigh = OnHigh;
                    Borders.CollorInner = "#000000";
                    Borders.Init();
                    b.Boxes.Add(Borders);
                }
                //6
                {
                    Box Borders = new Box();
                    Borders.Width = Width;
                    Borders.Height = borderSize;
                    Borders.Length = borderSize;
                    Borders.Left = Left + Length - (borderSize / 2);
                    Borders.Top = Top - (borderSize / 2);
                    Borders.OnHigh = OnHigh;
                    Borders.CollorInner = "#000000";
                    Borders.Init();
                    b.Boxes.Add(Borders);
                }
                //7
                {
                    Box Borders = new Box();
                    Borders.Width = Width;
                    Borders.Height = borderSize;
                    Borders.Length = borderSize;
                    Borders.Left = Left - (borderSize / 2);
                    Borders.Top = Top - (borderSize / 2);
                    Borders.CollorInner = "#000000";
                    Borders.OnHigh = Height + OnHigh;
                    Borders.Init();
                    b.Boxes.Add(Borders);
                }
                //8
                {
                    if (presetCameraPosition == 2)
                    {
                        Box Borders = new Box();
                        Borders.Width = Width;
                        Borders.Height = borderSize;
                        Borders.Length = borderSize;
                        Borders.Left = Left + Length - (borderSize / 2);
                        Borders.Top = Top - (borderSize / 2);
                        Borders.CollorInner = "#000000";
                        Borders.OnHigh = Height + OnHigh;
                        Borders.Init();
                        b.Boxes.Add(Borders);
                    }
                }
            }

            //Горизонтальные грани
            {
                //9
                {
                    Box Borders = new Box();
                    Borders.Width = borderSize;
                    Borders.Height = borderSize;
                    Borders.Length = Length;
                    Borders.Left = Left - (borderSize / 2);
                    Borders.Top = Top - (borderSize / 2);
                    Borders.OnHigh = OnHigh;
                    Borders.CollorInner = "#000000";
                    Borders.Init();
                    b.Boxes.Add(Borders);
                }
                //10
                {
                    Box Borders = new Box();
                    Borders.Width = borderSize;
                    Borders.Height = borderSize;
                    Borders.Length = Length;
                    Borders.Left = Left - (borderSize / 2);
                    Borders.Top = Top + Width - (borderSize / 2);
                    Borders.OnHigh = OnHigh;
                    Borders.CollorInner = "#000000";
                    Borders.Init();
                    b.Boxes.Add(Borders);
                }
                //11
                {
                    if (presetCameraPosition == 1)
                    {
                        Box Borders = new Box();
                        Borders.Width = borderSize;
                        Borders.Height = borderSize;
                        Borders.Length = Length;
                        Borders.Left = Left - (borderSize / 2);
                        Borders.Top = Top - (borderSize / 2);
                        Borders.CollorInner = "#000000";
                        Borders.OnHigh = Height + OnHigh;
                        Borders.Init();
                        b.Boxes.Add(Borders);
                    }
                }
                //12
                {
                    if (presetCameraPosition == 3)
                    {
                        Box Borders = new Box();
                        Borders.Width = borderSize;
                        Borders.Height = borderSize;
                        Borders.Length = Length;
                        Borders.Left = Left - (borderSize / 2);
                        Borders.Top = Top + Width - (borderSize / 2);
                        Borders.CollorInner = "#000000";
                        Borders.OnHigh = Height + OnHigh;
                        Borders.Init();
                        b.Boxes.Add(Borders);
                    }
                }
            }
        }

        /// <summary>
        /// Добавление обрамления объекту
        /// </summary>
        /// <param name="Width"></param>
        /// <param name="Height"></param>
        /// <param name="Length"></param>
        /// <param name="Left"></param>
        /// <param name="Top"></param>
        /// <param name="b"></param>
        /// <param name="OnHigh"></param>
        public void AddBorders(int Width, int Height, int Length, int Left, int Top, Box b, int OnHigh)
        {
            int borderSize = 10;

            //1
            {
                Box Borders = new Box();
                Borders.Width = borderSize;
                Borders.Height = Height;
                Borders.Length = borderSize;
                Borders.Left = Left - (borderSize / 2);
                Borders.Top = Top - (borderSize / 2);
                Borders.OnHigh = OnHigh;
                Borders.CollorInner = "#000000";
                Borders.Init();
                b.Boxes.Add(Borders);
            }
            //2
            {
                Box Borders = new Box();
                Borders.Width = borderSize;
                Borders.Height = Height;
                Borders.Length = borderSize;
                Borders.Left = Left + Length - (borderSize / 2);
                Borders.Top = Top - (borderSize / 2);
                Borders.OnHigh = OnHigh;
                Borders.CollorInner = "#000000";
                Borders.Init();
                b.Boxes.Add(Borders);
            }
            //3
            {
                Box Borders = new Box();
                Borders.Width = borderSize;
                Borders.Height = Height;
                Borders.Length = borderSize;
                Borders.Left = Left + Length - (borderSize / 2);
                Borders.Top = Top + Width - (borderSize / 2);
                Borders.OnHigh = OnHigh;
                Borders.CollorInner = "#000000";
                Borders.Init();
                b.Boxes.Add(Borders);
            }
            //4
            {
                Box Borders = new Box();
                Borders.Width = borderSize;
                Borders.Height = Height;
                Borders.Length = borderSize;
                Borders.Left = Left - (borderSize / 2);
                Borders.Top = Top + Width - (borderSize / 2);
                Borders.OnHigh = OnHigh;
                Borders.CollorInner = "#000000";
                Borders.Init();
                b.Boxes.Add(Borders);
            }

            //5
            {
                Box Borders = new Box();
                Borders.Width = Width;
                Borders.Height = borderSize;
                Borders.Length = borderSize;
                Borders.Left = Left - (borderSize / 2);
                Borders.Top = Top - (borderSize / 2);
                Borders.OnHigh = OnHigh;
                Borders.CollorInner = "#000000";
                Borders.Init();
                b.Boxes.Add(Borders);
            }
            //6
            {
                Box Borders = new Box();
                Borders.Width = Width;
                Borders.Height = borderSize;
                Borders.Length = borderSize;
                Borders.Left = Left + Length - (borderSize / 2);
                Borders.Top = Top - (borderSize / 2);
                Borders.OnHigh = OnHigh;
                Borders.CollorInner = "#000000";
                Borders.Init();
                b.Boxes.Add(Borders);
            }
            //7
            {
                Box Borders = new Box();
                Borders.Width = Width;
                Borders.Height = borderSize;
                Borders.Length = borderSize;
                Borders.Left = Left - (borderSize / 2);
                Borders.Top = Top - (borderSize / 2);
                Borders.OnHigh = Height + OnHigh;
                Borders.CollorInner = "#000000";
                Borders.Init();
                b.Boxes.Add(Borders);
            }
            //8
            {
                Box Borders = new Box();
                Borders.Width = Width;
                Borders.Height = borderSize;
                Borders.Length = borderSize;
                Borders.Left = Left + Length - (borderSize / 2);
                Borders.Top = Top - (borderSize / 2);
                Borders.OnHigh = Height + OnHigh;
                Borders.CollorInner = "#000000";
                Borders.Init();
                b.Boxes.Add(Borders);
            }

            //9
            {
                Box Borders = new Box();
                Borders.Width = borderSize;
                Borders.Height = borderSize;
                Borders.Length = Length;
                Borders.Left = Left - (borderSize / 2);
                Borders.Top = Top - (borderSize / 2);
                Borders.OnHigh = OnHigh;
                Borders.CollorInner = "#000000";
                Borders.Init();
                b.Boxes.Add(Borders);
            }
            //10
            {
                Box Borders = new Box();
                Borders.Width = borderSize;
                Borders.Height = borderSize;
                Borders.Length = Length;
                Borders.Left = Left - (borderSize / 2);
                Borders.Top = Top + Width - (borderSize / 2);
                Borders.OnHigh = OnHigh;
                Borders.CollorInner = "#000000";
                Borders.Init();
                b.Boxes.Add(Borders);
            }
            //11
            {
                Box Borders = new Box();
                Borders.Width = borderSize;
                Borders.Height = borderSize;
                Borders.Length = Length;
                Borders.Left = Left - (borderSize / 2);
                Borders.Top = Top - (borderSize / 2);
                Borders.OnHigh = Height + OnHigh;
                Borders.CollorInner = "#000000";
                Borders.Init();
                b.Boxes.Add(Borders);
            }
            //12
            {
                Box Borders = new Box();
                Borders.Width = borderSize;
                Borders.Height = borderSize;
                Borders.Length = Length;
                Borders.Left = Left - (borderSize / 2);
                Borders.Top = Top + Width - (borderSize / 2);
                Borders.OnHigh = Height + OnHigh;
                Borders.CollorInner = "#000000";
                Borders.Init();
                b.Boxes.Add(Borders);
            }
        }

        /// <summary>
        /// создание модели
        /// </summary>
        /// <returns></returns>
        public GeometryModel3D CreateModel()
        {
            GeometryModel3D result = null;

            if (Initialized)
            {
                MeshGeometry3D mesh;
                if (CarFlag)
                {
                    mesh = CreateMeshInverted();
                }
                else
                {
                    mesh = CreateMesh();
                }

                var color = CollorOuter;
                var brush = color.ToBrush();
                var material = new DiffuseMaterial(brush);
                result = new GeometryModel3D(mesh, material);
            }

            return result;
        }

        /// <summary>
        /// создание модели на высоте H
        /// </summary>
        /// /// <param name="H">Высота нижней точки куба</param> 
        /// <returns></returns>
        public GeometryModel3D CreateModel(int H)
        {
            GeometryModel3D result = null;

            if (Initialized)
            {
                MeshGeometry3D mesh;
                if (CarFlag == true)
                {
                    mesh = CreateMeshInverted();
                }
                else
                {
                    mesh = CreateMesh(H);
                }

                var color = CollorOuter;
                var brush = color.ToBrush();
                var material = new DiffuseMaterial(brush);
                result = new GeometryModel3D(mesh, material);
            }

            return result;
        }
    }

    public class PalletBox : Box
    {

    }

    /// <summary>
    /// Класс объектов, представляющих нумерацию на верхней грани бокса
    /// </summary>
    public class NumberPallet
    {
        /// <summary>
        /// Плоскость с текстблоком для нумерации боксов 
        /// </summary>
        /// <param name="box"></param>
        public NumberPallet(Box box)
        {
            NumberPalletModel = new Viewport2DVisual3D();
            Number = box.NumberProduct;
            Left = box.Left;
            Top = box.Top;
            Length = box.Length;
            Width = box.Width;
            Height = box.Height;
            OnHigh = box.OnHigh;
        }

        Viewport2DVisual3D NumberPalletModel { get; set; }
        /// <summary>
        /// Номер, который отображается на плоскости
        /// </summary>
        public int Number { get; set; }
        /// <summary>
        /// Х координата плоскости с номером
        /// </summary>
        public int Left { get; set; }
        /// <summary>
        /// У координата плоскости сномером
        /// </summary>
        public int Top { get; set; }
        /// <summary>
        /// Длина плоскости с номером
        /// </summary>
        public int Length { get; set; }
        /// <summary>
        /// Ширина плоскости с номером
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// ?Высота плоскости с номером?
        /// ?Высота объекта, но котором располагается плоскость?
        /// </summary>
        public int Height { get; set; }
        /// <summary>
        /// Высота расположения плосости с номером
        /// </summary>
        public int OnHigh { get; set; }

        /// <summary>
        /// Создание меша полскости с номером
        /// </summary>
        /// <returns></returns>
        public MeshGeometry3D CreateMesh()
        {
            var points = CreatePoints();

            var mesh = new MeshGeometry3D();
            mesh.Positions = points;

            var c = new Int32Collection();

            //Верхняя стенка
            c.Add(0);
            c.Add(3);
            c.Add(1);

            c.Add(3);
            c.Add(2);
            c.Add(1);

            mesh.TriangleIndices = c;

            var texturePoints = CreateTexturePoints();

            mesh.TextureCoordinates = texturePoints;

            return mesh;
        }

        /// <summary>
        /// Определение точек и их порядка для расположения номера на плоскости
        /// </summary>
        /// <returns></returns>
        public PointCollection CreateTexturePoints()
        {
            var texturePoints = new PointCollection();

            {
                var t = new Point();
                t.X = Left;
                t.Y = Top + Width;
                texturePoints.Add(t);
            }

            {
                var t = new Point();
                t.X = Left;
                t.Y = Top;
                texturePoints.Add(t);
            }

            {
                var t = new Point();
                t.X = Left + Length;
                t.Y = Top;
                texturePoints.Add(t);
            }

            {
                var t = new Point();
                t.X = Left + Length;
                t.Y = Top + Width;
                texturePoints.Add(t);
            }

            return texturePoints;
        }

        /// <summary>
        /// Создание точек для расположения плоскости в пространстве
        /// </summary>
        /// <returns></returns>
        public Point3DCollection CreatePoints()
        {
            var points = new Point3DCollection();

            //5
            {
                var p = new Point3D();
                p.X = Left;
                p.Y = Height + OnHigh + 5;
                p.Z = Top + Width;
                points.Add(p);
            }

            //6
            {
                var p = new Point3D();
                p.X = Left;
                p.Y = Height + OnHigh + 5;
                p.Z = Top;
                points.Add(p);
            }

            //7
            {
                var p = new Point3D();
                p.X = Left + Length;
                p.Y = Height + OnHigh + 5;
                p.Z = Top;
                points.Add(p);
            }

            //8
            {
                var p = new Point3D();
                p.X = Left + Length;
                p.Y = Height + OnHigh + 5;
                p.Z = Top + Width;
                points.Add(p);
            }

            return points;
        }

        /// <summary>
        /// Создание TextBlock, который будет отображать номер на плоскости
        /// </summary>
        /// <param name="Number"></param>
        /// <returns></returns>
        public TextBlock CreateTextBlock(int Number)
        {
            var textblock = new TextBlock();
            textblock.Text = $"{Number}";

            //не работает
            textblock.FontSize = 0.8;
            textblock.TextAlignment = TextAlignment.Center;

            //textblock.MouseUp += TextBlockNumberClick; //Не работает

            return textblock;
        }

        /// <summary>
        /// Создание плоскости с номером в пространстве
        /// </summary>
        /// <returns></returns>
        public Viewport2DVisual3D CreateModel()
        {
            NumberPalletModel.Geometry = CreateMesh();

            var material = new DiffuseMaterial();

            var tb = CreateTextBlock(this.Number);
            var vb = new VisualBrush();
            vb.Visual = tb;
            material.Brush = vb;

            NumberPalletModel.Material = material;

            return NumberPalletModel;
        }
    }

    public class PalletScene : Scene
    {
        public PalletScene()
        {
            Car = new Box();
            ModelGroup = new Model3DGroup();
            ModelList = new Dictionary<string, Box>();
            ModelContainer = null;
            CarGeometry = null;
            Camera = null;
            CameraPosition = new Position();
            CameraTargetPosition = new Position();
            Light1 = null;
            Light2 = null;
            Initialized = false;
            Light1Intensity = 127;
            Light2Intensity = 127;
            Angle1 = 0;
            Angle2 = 0;
            Distance = 0;
            ModelListNumberPallet = new Dictionary<string, NumberPallet>();
            ContainerGroupNumberPallet = new ContainerUIElement3D();
            PalletQuantity = 0;
        }

        /// <summary>
        /// Инициализация сцены, расположение камеры, света, осей и транспорта
        /// </summary>
        public void Init()
        {
            if (
                ModelContainer != null
                && Camera != null
            )
            {
                //отрисовка авто
                {
                    Car.CarFlag = true;
                    Car.Init();
                    AddModel(Car);

                    Car.AddBorders(Car.Width, Car.Height, Car.Length, Car.Left, Car.Top, Car, Car.OnHigh);
                    foreach (var item in Car.Boxes)
                    {
                        AddModel(item);
                    }
                }

                //камера
                {
                    //положение
                    CameraPosition.X = -4235;
                    CameraPosition.Y = 7341;
                    CameraPosition.Z = 10737;

                    CameraTargetPosition.X = -1700;
                    CameraTargetPosition.Y = 3900;
                    CameraTargetPosition.Z = 5300;
                }

                //свет
                {
                    Light1Intensity = 64;
                    Light2Intensity = 220;
                }

                {
                    Angle1 = 335;
                    Angle2 = 35;
                    Distance = 6000;
                }

                //оси
                if (Central.DebugMode == true)
                {

                    {
                        YLine = new Box();
                        YLine.Width = 10;
                        YLine.Height = 5000000;
                        YLine.Length = 10;
                        YLine.Left = 0;
                        YLine.Top = 0;

                        YLine.CollorInner = "#008800";

                        YLine.Init();

                        this.AddModel(YLine);

                    }

                    {
                        XLine = new Box();
                        XLine.Width = 10;
                        XLine.Height = 10;
                        XLine.Length = 5000000;
                        XLine.Left = 0;
                        XLine.Top = 0;

                        XLine.CollorInner = "#0000ff";

                        XLine.Init();

                        this.AddModel(XLine);
                    }

                    {
                        ZLine = new Box();
                        ZLine.Width = 5000000;
                        ZLine.Height = 10;
                        ZLine.Length = 10;
                        ZLine.Left = 0;
                        ZLine.Top = 0;

                        ZLine.CollorInner = "#ff0000";

                        ZLine.Init();

                        this.AddModel(ZLine);
                    }
                }

                Initialized = true;
            }
        }

        /// <summary>
        /// Обработчик нажатия горячих клавиш для управления камерой
        /// </summary>
        /// <param name="e"></param>
        public void ProcessKeyboard(System.Windows.Input.KeyEventArgs e)
        {

            bool shift = Keyboard.IsKeyDown(Key.LeftCtrl) ||
                                Keyboard.IsKeyDown(Key.RightCtrl);
            bool processed = false;
            if (Central.DebugMode == true)
            {
                switch (e.Key)
                {

                    case Key.Q:
                        Init();
                        processed = true;
                        break;

                    case Key.D1:
                        Light1Intensity++;
                        processed = true;
                        break;

                    case Key.D2:
                        Light1Intensity--;
                        processed = true;
                        break;

                    case Key.D3:
                        Light2Intensity++;
                        processed = true;
                        break;

                    case Key.D4:
                        Light2Intensity--;
                        processed = true;
                        break;


                    case Key.NumPad4:
                        if (shift)
                        {
                            CameraPosition.X = CameraPosition.X + 100;
                        }
                        else
                        {
                            CameraTargetPosition.X = CameraTargetPosition.X + 100;
                        }
                        processed = true;
                        break;

                    case Key.NumPad1:
                        if (shift)
                        {
                            CameraPosition.X = CameraPosition.X - 100;
                        }
                        else
                        {
                            CameraTargetPosition.X = CameraTargetPosition.X - 100;
                        }
                        processed = true;
                        break;


                    case Key.NumPad5:
                        if (shift)
                        {
                            CameraPosition.Z = CameraPosition.Z + 100;
                        }
                        else
                        {
                            CameraTargetPosition.Z = CameraTargetPosition.Z + 100;
                        }
                        processed = true;
                        break;

                    case Key.NumPad2:
                        if (shift)
                        {
                            CameraPosition.Z = CameraPosition.Z - 100;
                        }
                        else
                        {
                            CameraTargetPosition.Z = CameraTargetPosition.Z - 100;
                        }
                        processed = true;
                        break;

                    case Key.NumPad6:
                        if (shift)
                        {
                            CameraPosition.Y = CameraPosition.Y + 100;
                        }
                        else
                        {
                            CameraTargetPosition.Y = CameraTargetPosition.Y + 100;
                        }
                        processed = true;
                        break;

                    case Key.NumPad3:
                        if (shift)
                        {
                            CameraPosition.Y = CameraPosition.Y - 100;
                        }
                        else
                        {
                            CameraTargetPosition.Y = CameraTargetPosition.Y - 100;
                        }
                        processed = true;
                        break;
                }
            }

            if (processed)
            {
                if (!shift)
                {
                    Update();
                }

                Render();
            }
        }
    }

    /// <summary>
    /// сцена отрисовки автомобиля
    /// </summary>
    public class Scene
    {
        public Scene()
        {
            Car = new Box();
            ModelGroup = new Model3DGroup();
            ModelList = new Dictionary<string, Box>();
            ModelContainer = null;
            CarGeometry = null;
            Camera = null;
            CameraPosition = new Position();
            CameraTargetPosition = new Position();
            Light1 = null;
            Light2 = null;
            PalletQuantity = 0;
            Initialized = false;
            Light1Intensity = 127;
            Light2Intensity = 127;
            Angle1 = 0;
            Angle2 = 0;
            Distance = 0;
            PresetCameraNumber = 1;

            ModelListNumberPallet = new Dictionary<string, NumberPallet>();
            ContainerGroupNumberPallet = new ContainerUIElement3D();
            UIPointContainer = new ContainerUIElement3D();
            UIPointBoxList = new List<Box>();
        }

        /// <summary>
        /// автомобиль
        /// </summary>
        public Box Car { get; set; }

        /// <summary>
        /// все параметры проинициализированы, объект готов к работе
        /// </summary>
        public bool Initialized { get; set; }

        public int PalletQuantity { get; set; }
        /// <summary>
        /// Лист боксов для рендера на сцене
        /// </summary>
        public Dictionary<string, Box> ModelList { get; set; }
        /// <summary>
        /// Набор сценерированных 3D моделей объектов для добавления их в контейнер сцены
        /// </summary>
        public Model3DGroup ModelGroup { get; set; }
        /// <summary>
        /// Контейнер 3D моделей объектов сцены
        /// </summary>
        public ModelVisual3D ModelContainer { get; set; }

        public MeshGeometry3D CarGeometry { get; set; }
        /// <summary>
        /// Ортогональная камера сцены
        /// </summary>
        public OrthographicCamera Camera { get; set; }

        public Position CameraPosition { get; set; }

        public Position CameraTargetPosition { get; set; }
        /// <summary>
        /// Освецение направленное
        /// </summary>
        public int Light1Intensity { get; set; }
        /// <summary>
        /// Освещение рассеянное
        /// </summary>
        public int Light2Intensity { get; set; }
        public DirectionalLight Light1 { get; set; }
        public AmbientLight Light2 { get; set; }

        /// <summary>
        /// Горизонтальный угол обзора сцена
        /// </summary>
        public int Angle1 { get; set; }
        /// <summary>
        /// Вертикальный угол обзора сцены
        /// </summary>
        public int Angle2 { get; set; }
        /// <summary>
        /// Расстояние камеры до сцены
        /// </summary>
        public int Distance { get; set; }

        /// <summary>
        /// Лист моделей объектов с интерактивными элементами (плоскость с TextBlock)
        /// </summary>
        public Dictionary<string, NumberPallet> ModelListNumberPallet { get; set; }
        /// <summary>
        /// Контейнер 3D объектов сцены с интерактивными элементами
        /// </summary>
        public ContainerUIElement3D ContainerGroupNumberPallet { get; set; }

        /// <summary>
        /// Контейнер точек стыковки (3D объектов сцены с интерактивными элементами) размещаемых поддонов
        /// </summary>
        public ContainerUIElement3D UIPointContainer { get; set; }

        /// <summary>
        /// Список объектов класса Box, по которым были созданы точки стыковки ModelUIElement3D для ContainerUIElement3D
        /// </summary>
        public List<Box> UIPointBoxList { get; set; }

        /// <summary>
        /// 1 -- взгляд спереди; 2 -- взгляд сверху; 3 -- взгляд сзади; 4 -- взгляд сбоку
        /// </summary>
        public int PresetCameraNumber { get; set; }

        /// <summary>
        /// Инициализация сцены, расположение камеры, света, осей и транспорта
        /// </summary>
        public void Init()
        {
            if (
                ModelContainer != null
                && Camera != null
            )
            {
                // Добавление транспорта
                {
                    InitCar();
                }

                // Установка камеры
                {
                    InitCamera();
                }

                //оси
                if (Central.DebugMode == true)
                {

                    {
                        YLine = new Box();
                        YLine.Width = 10;
                        YLine.Height = 5000000;
                        YLine.Length = 10;
                        YLine.Left = 0;
                        YLine.Top = 0;

                        YLine.CollorInner = "#008800";

                        YLine.Init();

                        this.AddModel(YLine);

                    }

                    {
                        XLine = new Box();
                        XLine.Width = 10;
                        XLine.Height = 10;
                        XLine.Length = 5000000;
                        XLine.Left = 0;
                        XLine.Top = 0;

                        XLine.CollorInner = "#0000ff";

                        XLine.Init();

                        this.AddModel(XLine);
                    }

                    {
                        ZLine = new Box();
                        ZLine.Width = 5000000;
                        ZLine.Height = 10;
                        ZLine.Length = 10;
                        ZLine.Left = 0;
                        ZLine.Top = 0;

                        ZLine.CollorInner = "#ff0000";

                        ZLine.Init();

                        this.AddModel(ZLine);
                    }
                }

                Initialized = true;
            }
        }

        public void InitCar()
        {
            Car.CarFlag = true;
            Car.Init();

            if (!ModelList.ContainsValue(Car))
            {
                AddModel(Car);
            }

            foreach (var board in Car.Boxes)
            {
                var b = ModelList.FirstOrDefault(x => x.Value == board);
                if (!string.IsNullOrEmpty(b.Key))
                {
                    ModelList.Remove(b.Key);
                }
            }
            Car.Boxes.Clear();

            Car.AddCarBorders(Car.Width, Car.Height, Car.Length, Car.Left, Car.Top, Car, Car.OnHigh, PresetCameraNumber);
            foreach (var item in Car.Boxes)
            {
                AddModel(item);
            }
        }

        public void InitCamera()
        {
            // Свет
            {
                Light1Intensity = 64;
                Light2Intensity = 220;
            }

            // Положение
            {
                CameraPosition.X = Car.Length;
                CameraPosition.Y = 3000;
                CameraPosition.Z = Car.Width;
            }

            switch (PresetCameraNumber)
            {
                // Просмотр спереди
                case 1:
                    Angle1 = 335;
                    Angle2 = 35;
                    Distance = 6000;
                    CameraTargetPosition.X = 600;
                    CameraTargetPosition.Y = 5000;
                    CameraTargetPosition.Z = 10000;

                    if (Car != null && Car.Length > 13600)
                    {
                        Angle1 = 345;
                        Angle2 = 35;
                        Distance = 6000;

                        //currentScene.CameraTargetPosition.X = 600;
                        //currentScene.CameraTargetPosition.Y = 5000;
                        //currentScene.CameraTargetPosition.Z = 10000;
                    }
                    break;

                // Просмотр сверху
                case 2:
                    Angle1 = 305;
                    Angle2 = 90;
                    Distance = 6000;
                    CameraTargetPosition.X = 1300;
                    CameraTargetPosition.Y = 4000;
                    CameraTargetPosition.Z = 8100;
                    break;

                // Просмотр сзади
                case 3:
                    Angle1 = 445;
                    Angle2 = 35;
                    Distance = 6000;
                    CameraTargetPosition.X = 6300;
                    CameraTargetPosition.Y = 5000;
                    CameraTargetPosition.Z = 6400;
                    break;

                // Просмотр сбоку
                case 4:
                    Angle1 = 395;
                    Angle2 = 90;
                    Distance = 6000;
                    CameraTargetPosition.X = 3000;
                    CameraTargetPosition.Y = 2600;
                    CameraTargetPosition.Z = 8100;
                    break;

                default:
                    Angle1 = 335;
                    Angle2 = 35;
                    Distance = 6000;
                    CameraTargetPosition.X = 600;
                    CameraTargetPosition.Y = 5000;
                    CameraTargetPosition.Z = 10000;

                    if (Car != null && Car.Length > 13600)
                    {
                        Angle1 = 345;
                        Angle2 = 35;
                        Distance = 6000;

                        //currentScene.CameraTargetPosition.X = 600;
                        //currentScene.CameraTargetPosition.Y = 5000;
                        //currentScene.CameraTargetPosition.Z = 10000;
                    }
                    break;
            }
        }

        /// <summary>
        /// Изменение положения камеры в зависимости от угла просмотра сцены
        /// </summary>
        public void Update()
        {
            if (Initialized)
            {
                UpdateCamera();
            }
        }

        /// <summary>
        /// Пересчёт положения камеры
        /// </summary>
        public void UpdateCamera()
        {
            {
                //deg to rad
                var a = (Math.PI / 180) * Angle1;
                CameraPosition.Z = (int)(Distance * Math.Cos(a)) + CameraTargetPosition.Z;
                CameraPosition.X = (int)(Distance * Math.Sin(a)) + CameraTargetPosition.X;
            }

            {
                //deg to rad
                var a = (Math.PI / 180) * Angle2;
                CameraPosition.Y = (int)(Distance * Math.Sin(a)) + CameraTargetPosition.Y;
            }

            if (Central.DebugMode == true)
            {
                CameraTarget = new Box();
                CameraTarget.Width = 250;
                CameraTarget.Height = 250;
                CameraTarget.Length = 250;
                CameraTarget.Left = CameraTargetPosition.Z;
                CameraTarget.Top = -CameraTargetPosition.X;

                CameraTarget.OnHigh = CameraTargetPosition.Y;

                CameraTarget.CollorInner = "#ff0000";

                CameraTarget.Init();

                if (ModelList.ContainsKey(CameraTargetIndex.ToString()))
                {
                    ModelList[CameraTargetIndex.ToString()] = CameraTarget;
                }
                else
                {
                    AddModel(CameraTarget);
                    CameraTargetIndex = PalletQuantity;
                }
            }
        }

        /// <summary>
        /// Ость У
        /// </summary>
        public Box YLine { get; set; }
        /// <summary>
        /// Ость Х
        /// </summary>
        public Box XLine { get; set; }
        /// <summary>
        /// Ось Z
        /// </summary>
        public Box ZLine { get; set; }
        /// <summary>
        /// Точка, на которую смотрит камера
        /// </summary>
        public Box CameraTarget { get; set; }
        /// <summary>
        /// Индекс точки в рамках ModelList, на которую смотрит камера
        /// </summary>
        public int CameraTargetIndex { get; set; }

        /// <summary>
        /// Начало отрисовки объектов и сцены
        /// </summary>
        public void Render()
        {
            if (Initialized)
            {
                PrintInfo();

                //camera
                {
                    //положение камеры
                    var position = new Point3D();
                    position.X = CameraPosition.X; //0; // 
                    position.Y = CameraPosition.Y; //10000; //
                    position.Z = CameraPosition.Z; //500; // 
                    Camera.Position = position;

                    // Почему вычитаем?

                    //положение таргета камеры
                    Point3D cameraTarget = new Point3D();
                    cameraTarget.X =  CameraTargetPosition.X - CameraPosition.X; //0; //
                    cameraTarget.Y = CameraTargetPosition.Y - CameraPosition.Y; //-1000; // 
                    cameraTarget.Z = CameraTargetPosition.Z - CameraPosition.Z; //500; 
                    Camera.LookDirection = (Vector3D)cameraTarget;
                }

                //light
                {
                    Light1.Color = (Color.FromRgb((byte)Light1Intensity, (byte)Light1Intensity, (byte)Light1Intensity));
                    Light2.Color = (Color.FromRgb((byte)Light2Intensity, (byte)Light2Intensity, (byte)Light2Intensity));
                }

                //объекты
                RenderObjects();

                //плоскости с номерами
                RenderNumberPalletObjects();

            }
        }

        /// <summary>
        /// Вывод в консоль данных по камере
        /// </summary>
        public void PrintInfo()
        {
            var s = "";
            s = $"l1=[{Light1Intensity}] l2=[{Light2Intensity}]";
            Central.Dbg(s);

            s = $"TARGET X=[{CameraTargetPosition.X}] Y=[{CameraTargetPosition.Y}] Z=[{CameraTargetPosition.Z}]";
            Central.Dbg(s);

            s = $"   CAM X=[{CameraPosition.X}] Y=[{CameraPosition.Y}] Z=[{CameraPosition.Z}]";
            Central.Dbg(s);

            s = $"   ANGLE 1=[{Angle1}] 2=[{Angle2}] Distance=[{Distance}]";
            Central.Dbg(s);
        }

        /// <summary>
        /// Обработчик нажатия горячих клавиш для управления камерой
        /// </summary>
        /// <param name="e"></param>
        public void ProcessKeyboard(System.Windows.Input.KeyEventArgs e)
        {

            bool shift = Keyboard.IsKeyDown(Key.LeftCtrl) ||
                                Keyboard.IsKeyDown(Key.RightCtrl);
            bool processed = false;
            if (Central.DebugMode == true)
            {
                switch (e.Key)
                {
                    case Key.R:
                        processed = true;
                        break;

                    case Key.Q:
                        Init();
                        processed = true;
                        break;

                    case Key.D1:
                        Light1Intensity++;
                        processed = true;
                        break;

                    case Key.D2:
                        Light1Intensity--;
                        processed = true;
                        break;

                    case Key.D3:
                        Light2Intensity++;
                        processed = true;
                        break;

                    case Key.D4:
                        Light2Intensity--;
                        processed = true;
                        break;


                    case Key.NumPad4:
                        if (shift)
                        {
                            CameraPosition.X = CameraPosition.X + 100;
                        }
                        else
                        {
                            CameraTargetPosition.X = CameraTargetPosition.X + 100;
                        }
                        processed = true;
                        break;

                    case Key.NumPad1:
                        if (shift)
                        {
                            CameraPosition.X = CameraPosition.X - 100;
                        }
                        else
                        {
                            CameraTargetPosition.X = CameraTargetPosition.X - 100;
                        }
                        processed = true;
                        break;


                    case Key.NumPad5:
                        if (shift)
                        {
                            CameraPosition.Z = CameraPosition.Z + 100;
                        }
                        else
                        {
                            CameraTargetPosition.Z = CameraTargetPosition.Z + 100;
                        }
                        processed = true;
                        break;

                    case Key.NumPad2:
                        if (shift)
                        {
                            CameraPosition.Z = CameraPosition.Z - 100;
                        }
                        else
                        {
                            CameraTargetPosition.Z = CameraTargetPosition.Z - 100;
                        }
                        processed = true;
                        break;

                    case Key.NumPad6:
                        if (shift)
                        {
                            CameraPosition.Y = CameraPosition.Y + 100;
                        }
                        else
                        {
                            CameraTargetPosition.Y = CameraTargetPosition.Y + 100;
                        }
                        processed = true;
                        break;

                    case Key.NumPad3:
                        if (shift)
                        {
                            CameraPosition.Y = CameraPosition.Y - 100;
                        }
                        else
                        {
                            CameraTargetPosition.Y = CameraTargetPosition.Y - 100;
                        }
                        processed = true;
                        break;
                }
            }

            if (processed)
            {
                Update();
                Render();
            }
        }


        /// <summary>
        /// Добавление модели в список моделей для рендера (ModelList)
        /// </summary>
        public void AddModel(Box box)
        {
            if (!ModelList.ContainsKey(PalletQuantity.ToString()))
            {
                ModelList.Add(PalletQuantity.ToString(), box);

                AddNumberPalletModel(box);

                PalletQuantity++;
            }
            else
            {
                ModelList[PalletQuantity.ToString()] = box;

                AddNumberPalletModel(box);

                PalletQuantity++;
            }
        }

        /// <summary>
        /// Добавление плоскости с номером в список моделей для рендере (PalletModelList)
        /// </summary>
        /// <param name="box"></param>
        public void AddNumberPalletModel(Box box)
        {
            if (box.ListIndex > 0)
            {
                NumberPallet numberPallet = new NumberPallet(box);
                ModelListNumberPallet.Add(PalletQuantity.ToString(), numberPallet);
            }
        }

        /// <summary>
        /// Создание 3D объектов из списка моделей для рендера(ModelList),
        /// Добавление объектов на сцену
        /// </summary>
        public void RenderObjects()
        {
            //очищаем группу
            ModelGroup.Children.Clear();

            //добавляем в группу все поддоны из коллекции
            if (ModelList.Count > 0)
            {
                int i = 0;
                foreach (var item in ModelList)
                {
                    //if (item.Key == "435")
                    //{

                    //}
                    GeometryModel3D model;
                    if (item.Value.OnHigh != 0)
                    {
                        model = item.Value.CreateModel((int)item.Value.OnHigh);
                    }
                    else
                    {
                        model = item.Value.CreateModel();
                    }

                    if (model == null)
                    {
                        i += 1;
                    }
                    else
                    {
                        ModelGroup.Children.Add(model);
                    }
                }

                if (i != 0)
                {
                    MessageBox.Show("Ошибка добавления модели");
                }
            }

            //передаем коллекцию в контейнер
            ModelContainer.Content = ModelGroup;
        }

        /// <summary>
        /// Создание 3D объектов с интерактивными элементами из списка моделей для рендера(PalletModelList),
        /// Добавление объектов на сцену
        /// </summary>
        public void RenderNumberPalletObjects()
        {
            ContainerGroupNumberPallet.Children.Clear();

            if (ModelListNumberPallet.Count > 0)
            {
                foreach (var item in ModelListNumberPallet)
                {
                    Viewport2DVisual3D NumberPalletModel;
                    NumberPalletModel = item.Value.CreateModel();

                    ContainerGroupNumberPallet.Children.Add(NumberPalletModel);
                }
            }
        }
    }

    /// <summary>
    /// ?X,Y,Z позиции объектов?
    /// </summary>
    public class Position
    {
        public Position()
        {
            X = 0;
            Y = 0;
            Z = 0;
        }

        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
    }
}

