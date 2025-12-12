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
using System.Windows.Media;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс для работы с цветами
    /// </summary>
    /// <author>eletskikh_ya</author>
    public partial class ColorList : UserControl
    {
        /// <summary>
        /// Конструктор класса
        /// </summary>
        public ColorList()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/colours";
            ColorGridInit();
            CompositionGridInit();
            TechnologicalMapGridInit();

            SetDefaults();
        }

        /// <summary>
        /// данные из выбранной в гриде строки
        /// </summary>
        public Dictionary<string, string> SelectedItem { get; set; }
        /// <summary>
        /// данные из выбранной в гриде тех карты строки
        /// </summary>
        public Dictionary<string, string> SelectedTechnicalMapItem { get; set; }

        /// <summary>
        /// Выбранный цвет
        /// </summary>
        private string SelectedColor { get; set; }

        /// <summary>
        /// Названия типов картона, используются при обработке строк
        /// </summary>
        private Dictionary<string, string> CardboardTypeName { get; set; }

        /// <summary>
        /// Имя вкладки, определяется в файле интерфейса
        /// </summary>
        public string TabName;
        /// <summary>
        /// Ссылка на страницу документации
        /// </summary>
        private string DocumentationUrl;
        /// <summary>
        /// Право доступа пользователя к интерфейсу
        /// </summary>
        private Role.AccessMode UserAccessMode { get; set; }

        /// <summary>
        /// Установка значений по умолчанию и создание предзаполненных справочников 
        /// </summary>
        private void SetDefaults()
        {
            var list = new Dictionary<string, string>();
            list.Add("0", "Все");
            list.Add("1", "В работе");
            list.Add("2", "В архиве");
            list.Add("3", "В дизайне");

            UsingType.SetItems(list);
            UsingType.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");

            CardboardTypeName = new Dictionary<string, string>()
            {
                { "0", "универсальный" },
                { "1", "по белому" },
                { "2", "по бурому" },
            };
            CardboardType.Items = new Dictionary<string, string>() { { "-1", "Все" } };
            CardboardType.Items.AddRange(CardboardTypeName);
            CardboardType.SetSelectedItemByKey("-1");

            UserAccessMode = Central.Navigator.GetRoleLevel("[erp]color");
        }

        /// <summary>
        /// Функция перевода строки содержащей hex код цвета краски в цвет Brush
        /// <param name="hex_code">строка с hex числом</param>
        /// <return>Brush.цвет</return>
        /// </summary>
        private Brush HexToBrush(string hex_code)
        {
            SolidColorBrush result = null;
            var hexString = (hex_code as string).Replace("#", "");

            if (hexString.Length == 6)
            {
                var r = hexString.Substring(0, 2);
                var g = hexString.Substring(2, 2);
                var b = hexString.Substring(4, 2);

                result = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xff,
                   byte.Parse(r, System.Globalization.NumberStyles.HexNumber),
                   byte.Parse(g, System.Globalization.NumberStyles.HexNumber),
                   byte.Parse(b, System.Globalization.NumberStyles.HexNumber)));
            }

            return result;
        }

        /// <summary>
        /// Инициализация таблицы цветов
        /// </summary>
        private void ColorGridInit()
        {
            var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="COLOR_ID",
                        Doc="Идентификатор цвета",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=40,
                        MaxWidth = 40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="NAME",
                        Doc="Наименование цвета",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=110,
                        MaxWidth = 110,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Цвет",
                        Path="NAME2",
                        Doc="Тестовое обозначение цвета",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        MaxWidth=200,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Цвет сокращенный",
                        Path="SHORT_NAME",
                        Doc="Сокращенное наименование цвета",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        MaxWidth = 150,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Код цвета",
                        Path="HEX",
                        Doc="Цифровое обозначение цвета",
                        Options="hexcolor",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=80,
                        MaxWidth = 100,
                        Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = row["HEX"];

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result = HexToBrush(color);
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Архив",
                        Path="ARCHIVE_FLAG",
                        Doc="",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=50,
                        MaxWidth = 50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Редкий цвет",
                        Path="RARE_FLAG",
                        Doc="",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=50,
                        MaxWidth = 50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Новый цвет",
                        Path="NEW_FLAG",
                        Doc="Новый разработанный цвет или возвращенный из архива",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=50,
                        MaxWidth = 50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Цвет картона",
                        Path="_CARDBOARD_TYPE_NAME",
                        Doc="Цвет картона",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=120,
                        MaxWidth = 120,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Код цвета Pantone",
                        Path="PANTONE",
                        Doc="Цвет по Pantone",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        MaxWidth = 120,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Цена евро",
                        Path="PRICE",
                        Doc="Цена",
                        ColumnType=ColumnTypeRef.Double,
                        MinWidth=80,
                        MaxWidth = 80,
                    },
                    new DataGridHelperColumn
                    {
                       Header = " ",
                       Path = "_",
                       ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                       MinWidth = 5,
                       MaxWidth = 2000,
                    },
                };

            // Раскраска строк
            GridColor.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                // Цвета фона строк
                {
                    DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        var currentStatus = row.CheckGet("ARCHIVE_FLAG").ToBool();
                        if (currentStatus == true)
                        {
                            color = HColor.OliveFG;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };

            GridColor.SelectItemMode = 0; 
            GridColor.SetColumns(columns);
            GridColor.PrimaryKey = "COLOR_ID";
            GridColor.SetSorting("NAME", ListSortDirection.Ascending);
            GridColor.SearchText = Search;
            GridColor.Init();

            GridColor.OnDblClick = selectedItem =>
            {
                if (UserAccessMode == Role.AccessMode.FullAccess)
                {
                    Edit();
                }
            };

            GridColor.OnSelectItem = selectedItem =>
            {
                UpdateActions(selectedItem);
            };

            //данные грида
            GridColor.OnLoadItems = ColorGridLoadItems;
            GridColor.OnFilterItems = ColorGridFilterItems;

            GridColor.Run();

            //фокус ввода           
            GridColor.Focus();

        }

        /// <summary>
        /// Функция окрашивания ячейки в зависимости от того совпадает ли значение в ней с именем выбранной краски
        /// <param name="row">строка для проверки</param>
        /// <param name="columnName">имя столбца для проверки</param>
        /// <param name="defaultResult">цвет по умолчанию</param>
        /// <returns> HColor.Yellow если совпадает </returns>
        /// </summary>
        private object ColorForLogicalMapCellStyle(Dictionary<string, string> row, string columnName, object defaultResult)
        {
            var result = defaultResult;

            if (SelectedColor != string.Empty)
                if (row[columnName] == SelectedColor)
                {
                    result = HColor.Yellow;
                }

            return result;
        }

        /// <summary>
        /// Инициализация грида технической карты
        /// </summary>
        private void TechnologicalMapGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Потребитель",
                    Path="CUSTOMER",
                    Doc="Наименование потребителя",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=140,
                    MaxWidth=350,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="ART",
                    Doc="Артикул",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=120,
                    MaxWidth=350,
                },
                new DataGridHelperColumn
                {
                    Header="Размер",
                    Path="SIZE_PROD",
                    Doc="Размер",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=76,
                    MaxWidth=250,
                },
                new DataGridHelperColumn
                {
                    Header="Цвет 1",
                    Path="COL1",
                    Doc="Используемые цвета",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=96,
                    MaxWidth=250,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        // Если выбранный цвет совпадает с цветом в тех.карте то мы его подсвечиваем желтым
                        { 
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                return ColorForLogicalMapCellStyle(row, "COL1", DependencyProperty.UnsetValue);
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Цвет 2",
                    Path="COL2",
                    Doc="Используемые цвета",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=96,
                    MaxWidth=250,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        // Если выбранный цвет совпадает с цветом в тех.карте то мы его подсвечиваем желтым
                        { 
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                return ColorForLogicalMapCellStyle(row, "COL2", DependencyProperty.UnsetValue);
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Цвет 3",
                    Path="COL3",
                    Doc="Используемые цвета",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=96,
                    MaxWidth=250,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        // Если выбранный цвет совпадает с цветом в тех.карте то мы его подсвечиваем желтым
                        { 
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                return ColorForLogicalMapCellStyle(row, "COL3", DependencyProperty.UnsetValue);
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Цвет 4",
                    Path="COL4",
                    Doc="Используемые цвета",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=96,
                    MaxWidth=250,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        // Если выбранный цвет совпадает с цветом в тех.карте то мы его подсвечиваем желтым
                        { 
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                return ColorForLogicalMapCellStyle(row, "COL4", DependencyProperty.UnsetValue);
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Цвет 5",
                    Path="COL5",
                    Doc="Используемые цвета",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=96,
                    MaxWidth=250,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        // Если выбранный цвет совпадает с цветом в тех.карте то мы его подсвечиваем желтым
                        { 
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                return ColorForLogicalMapCellStyle(row, "COL5", DependencyProperty.UnsetValue);
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Внутренний цвет 1",
                    Path="COLOR_IN_1",
                    Doc="Используемые цвета",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=96,
                    MaxWidth=250,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        // Если выбранный цвет совпадает с цветом в тех.карте то мы его подсвечиваем желтым
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                return ColorForLogicalMapCellStyle(row, "COLOR_IN_1", DependencyProperty.UnsetValue);
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Внутренний цвет 2",
                    Path="COLOR_IN_2",
                    Doc="Используемые цвета",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=96,
                    MaxWidth=250,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        // Если выбранный цвет совпадает с цветом в тех.карте то мы его подсвечиваем желтым
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                return ColorForLogicalMapCellStyle(row, "COLOR_IN_2", DependencyProperty.UnsetValue);
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Дата последней отгрузки",
                    Path="DATA",
                    Doc="Дата последней отгрузки",
                    ColumnType=ColumnTypeRef.String,
                    Format = "dd.MM.yyyy",
                    MinWidth=120,
                    MaxWidth=350,
                }
            };

            // цветовая маркировка строк
            GridTechnologicalMap.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                // Цвета фона строк
                {
                    DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        var currentStatus = row.CheckGet("ARCHIVE").ToBool();
                        if (currentStatus == true)
                        {
                            color = HColor.OliveFG;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };

            GridTechnologicalMap.SetColumns(columns);

            GridTechnologicalMap.SetSorting("CUSTOMER", ListSortDirection.Ascending);
            GridTechnologicalMap.SearchText = SearchByReference;
            GridTechnologicalMap.Init();

            GridTechnologicalMap.OnFilterItems = TechnicalMapGridFilterItems;

            ShowTMButton.IsEnabled = false;
            GridTechnologicalMap.SelectItemMode = 0; 

            GridTechnologicalMap.OnSelectItem = selectedItem =>
            {
                SelectedTechnicalMapItem = selectedItem;

                ShowTMButton.IsEnabled = false;

                if (SelectedTechnicalMapItem != null)
                {
                    string fullPathTk = SelectedTechnicalMapItem["PATHTK"];
                    if (!string.IsNullOrEmpty(fullPathTk))
                    {
                        if (System.IO.File.Exists(fullPathTk))
                        {
                            ShowTMButton.IsEnabled = true;
                        }
                    }
                }
            };

            GridTechnologicalMap.Run();
        }

        /// <summary>
        /// Фильтр для грида технологической карты, в зависимости от выбора selectbox принимается решение показывать или не показывать запись
        /// </summary>
        private bool TechnicalMapGridCheck(Dictionary<string, string> ds)
        {
            bool result = false;

            int index = UsingType.GetValue().ToInt();
            int archive = ds.CheckGet("ARCHIVE").ToInt();
            bool art = string.IsNullOrEmpty(ds.CheckGet("ART"));

            if (
                index == 0
                || (
                        index == 1
                        && archive == 0
                        && !art
                    )
                || (
                        index == 2
                        && archive == 1
                        && !art
                    )
                || (
                        index == 3
                        && archive == 0
                        && art
                    )
                )
            {
                result = true;
            }
            else
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Функция фильтрация данных в технологической карте
        /// </summary>
        public void TechnicalMapGridFilterItems()
        {
            if (GridTechnologicalMap.GridItems != null)
            {
                if (GridTechnologicalMap.GridItems.Count > 0)
                {
                    var list = new List<Dictionary<string, string>>();
                    foreach (var item in GridTechnologicalMap.GridItems)
                    {
                        if (TechnicalMapGridCheck(item))
                        {
                            list.Add(item);
                        }
                    }

                    GridTechnologicalMap.GridItems = list;
                }
            }
        }


        /// <summary>
        /// Инициализация грида компонентов краски
        /// </summary>
        private void CompositionGridInit()
        {
            var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Рецептура",
                        Path="NAME",
                        Doc="Наименование цвета",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        MaxWidth=200,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Описание",
                        Path="DESCRIPTION",
                        Doc="Тестовое обозначение цвета",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        MaxWidth=200,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Доля",
                        Path="RATIO",
                        Doc="Доля компонента в краске (максимальное значение 1,0)",
                        ColumnType=ColumnTypeRef.Double,
                        Format = "F5",
                        MinWidth=60,
                        MaxWidth=200,
                    },
                };

            GridComposition.SetColumns(columns);
            GridComposition.SetSorting("NAME", ListSortDirection.Ascending);
            GridComposition.Init();
            GridComposition.Run();
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            // Управление автообновлением
            if (m.SenderName == "WindowManager" && m.ReceiverName == TabName)
            {
                switch (m.Action)
                {
                    case "FocusGot":
                        GridColor.ItemsAutoUpdate = true;
                        GridColor.LoadItems();
                        break;

                    case "FocusLost":
                        GridColor.ItemsAutoUpdate = false;
                        break;
                }
            }
            
            if (m.ReceiverGroup.IndexOf("colorMain") > -1)
            {
                if (m.ReceiverName.IndexOf(TabName) > -1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            GridColor.LoadItems();

                            var id = m.Message.ToInt();
                            GridColor.SetSelectedItemId(id, "COLOR_ID");

                            break;
                    }
                }
            }
        }

        /// <summary>
        /// обновление методов работы с выбранной в гриде строкой
        /// </summary>
        /// <param name="selectedItem"></param>
        public void UpdateActions(Dictionary<string, string> selectedItem)
        {
            // запретить удаление краски, после загрузки всех гридов будет проверка на возможность удаления    
            DeleteButton.IsEnabled = false; 

            SelectedItem = selectedItem;

            if (SelectedItem != null)
            {
                SelectedColor = SelectedItem.CheckGet("NAME");
            }

            PaintComponentGridLoad();
            TechnologicalMapGridLoad();
        }

        /// <summary>
        /// Загрузка данными грида технологической карты
        /// </summary>
        public async void TechnologicalMapGridLoad()
        {
            if (SelectedItem != null)
            {
                string color = SelectedItem.CheckGet("NAME").ToString();

                if (!string.IsNullOrEmpty(color))
                {
                    var p = new Dictionary<string, string>();
                    {
                        p.CheckAdd("COLOR_NAME", color.ToString());
                    }

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "TechnologicalMap");
                    q.Request.SetParam("Action", "ListByColor");
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
                            var ds = ListDataSet.Create(result, "ITEMS");
                            GridTechnologicalMap.UpdateItems(ds);
                        }

                        TechnologicalMapGridAfterLoad();
                    }
                }
            }
        }

        /// <summary>
        /// проверка на возможность удаления краски
        /// вариант первый, пользователь имеет полные права на удаление краски прописанный в интерфейсе ролей
        /// второй вариант, это неиспользуемая краска, возможно, созданная по ошибке не иvеющая ссылок из технологической карты 
        /// и с незаполненной таблицей состава
        /// </summary>
        private void TechnologicalMapGridAfterLoad()
        {
            ProcessPermissions();
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void  ProcessPermissions(string roleCode="")
        {
            bool canDelete = false;

            if (UserAccessMode == Role.AccessMode.Special)
            {
                // Пользователю со спецправами разрешаем всё, даже удалять
                canDelete = true;
            }
            else if (UserAccessMode == Role.AccessMode.FullAccess)
            {
                // пользователю с полный доступом разрешаем изменять, удалять можно только краски без дополнительных данных
                if (GridTechnologicalMap.Items == null || GridTechnologicalMap.Items.Count == 0)
                {
                    if (GridComposition.Items == null || GridComposition.Items.Count == 0)
                    {
                        canDelete = true;
                    }
                }
            }
            else if (UserAccessMode == Role.AccessMode.ReadOnly)
            {
                // пользователю только для чтения блокируем кнопки добавления и удаления, остальные доступны
                AddButton.IsEnabled = false;
                EditButton.IsEnabled = false;
            }

            DeleteButton.IsEnabled = canDelete;
        }

        /// <summary>
        /// удаление краски 
        /// </summary>
        private void Delete()
        {
            bool resume = true;

            if (resume)
            {
                if (SelectedItem == null)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                string msg = "";
                msg += $"Удалить выбранную краску?";
                msg += $"\n{SelectedColor}";
                var d = new DialogWindow($"{msg}", "Удаление краски", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() != true)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var COLOR_ID = SelectedItem.CheckGet("COLOR_ID");

                var p = new Dictionary<string, string>();
                {
                    p.Add("COLOR_ID", COLOR_ID);
                }

                var q = new LPackClientQuery();

                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "Color");
                q.Request.SetParam("Action", "Delete");
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    ColorGridLoadItems();
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Загрузка компонентов краски
        /// </summary>
        public async void PaintComponentGridLoad()
        {
            int colorId = 0;
            if (SelectedItem != null)
            {
                colorId = SelectedItem.CheckGet("COLOR_ID").ToInt();

                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("COLOR_ID", colorId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "Color");
                q.Request.SetParam("Action", "ListPaintComponent");
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
                            GridComposition.UpdateItems(ds);
                        }

                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// получение записей для GridColor
        /// </summary>
        public async void ColorGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Color");
            q.Request.SetParam("Action", "List");
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
                        ColorGridProcessItems(ds);
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Обработка данных перед отображением в таблице 
        /// </summary>
        /// <param name="ds">датасет</param>
        private void ColorGridProcessItems(ListDataSet ds)
        {
            if (ds.Items.Count > 0)
            {
                foreach (var item in ds.Items)
                {
                    var result = "";

                    var cardboardType = item.CheckGet("CARDBOARD_TYPE").ToInt().ToString();
                    if (CardboardTypeName.ContainsKey(cardboardType))
                    {
                        result = CardboardTypeName[cardboardType];
                    }
                    item.CheckAdd("_CARDBOARD_TYPE_NAME", result);
                }
            }

            GridColor.UpdateItems(ds);
        }

        private void ColorGridFilterItems()
        {
            if (GridColor.GridItems != null)
            {
                if (GridColor.GridItems.Count > 0)
                {
                    int cardboardType = CardboardType.SelectedItem.Key.ToInt();
                    if (cardboardType >= 0)
                    {
                        var list = new List<Dictionary<string, string>>();
                        foreach (var item in GridColor.GridItems)
                        {
                            bool includeByType = true;

                            if (item.CheckGet("CARDBOARD_TYPE").ToInt() != cardboardType)
                            {
                                includeByType = false;
                            }

                            if (includeByType)
                            {
                                list.Add(item);
                            }
                        }

                        GridColor.GridItems = list;
                    }
                }
            }
        }

        /// <summary>
        /// Вызов страницы справки
        /// </summary>
        private void ShowHelp()
        {
            Central.ShowHelp(DocumentationUrl);
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
                    ColorGridLoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Home:
                    GridColor.SetSelectToFirstRow();
                    e.Handled = true;
                    break;

                case Key.End:
                    GridColor.SetSelectToLastRow();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Освобождение ресурсов
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "colorMain",
                ReceiverName = "",
                SenderName = TabName,
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            GridColor.Destruct();
            GridTechnologicalMap.Destruct();
            GridComposition.Destruct();
        }

        /// <summary>
        /// Открытие грида в Excel
        /// К сожалению отображения цвета не удалось, так как данный способ не позволяет отображать RGB цвета, по крайней мере просто
        /// </summary>
        private async void Print()
        {
            var eg = new ExcelGrid();
            eg.SetColumnsFromGrid(GridColor.Columns);

            eg.Columns[4].Options = "hexcolor";
            eg.Items = GridColor.GridItems;

            await Task.Run(() =>
            {
                eg.Make();
            });
        }

        /// <summary>
        /// редактирование записи или создание новой
        /// <para> newColor - true добавление, false редактирование </para>
        /// </summary>
        public void Edit(bool newColor=false)
        {
            if(newColor)
            {
                var i = new Color();
                i.ReceiverName = TabName;
                i.Edit(0);
            }
            else
            if (SelectedItem != null)
            {
                var id = SelectedItem.CheckGet("COLOR_ID").ToInt();

                var i = new Color();
                i.ReceiverName = TabName;
                i.Edit(id);
            }
        }

        public void ShowTechnologicalMap()
        {
            ShowTMButton.IsEnabled = false;

            if (SelectedTechnicalMapItem != null)
            {
                string FullPathTechnicalMap = SelectedTechnicalMapItem["PATHTK"];
                if (!string.IsNullOrEmpty(FullPathTechnicalMap))
                {
                    if (System.IO.File.Exists(FullPathTechnicalMap))
                    {
                        Central.OpenFile(FullPathTechnicalMap);
                    }
                    else
                    {
                        var dw = new DialogWindow("Файл техкарты был удален или перемещен", "Показать техкарту");
                        dw.ShowDialog();
                    }
                }
            }

            ShowTMButton.IsEnabled = true;
        }

        /// <summary>
        /// Показ техкарты из Excel файла
        /// </summary>
        private void ShowTM_Click(object sender, RoutedEventArgs e)
        {
            ShowTechnologicalMap();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            Edit();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            ColorGridLoadItems();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            Print();
        }
       
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Edit(true);
        }

        private void TypeComponent_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GridTechnologicalMap.UpdateItems();
        }

        private void DelButton_Click(object sender, RoutedEventArgs e)
        {
            Delete();
        }

        private void CardboardType_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GridColor.UpdateItems();
        }
    }
}
