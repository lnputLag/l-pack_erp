using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Образцы для чертежа
    /// </summary>
    /// <author>vlasov_ea</author>
    public partial class SampleDrawingList : ControlBase
    {
        /// <summary>
        /// Инициализация элемента интерфейса образцов для чертежа
        /// </summary>
        public SampleDrawingList()
        {
            ControlTitle = "Образцы для чертежа";
            InitializeComponent();
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/samples_new/designer_samples";

            SetDefaults();
            InitGrid();

            OnUnload = () =>
            {
                Grid.Destruct();
            };

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverName == ControlName)
                {
                    ProcessCommand(msg.Action, msg);
                }
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

            //Commander.Init(this);
        }

        /// <summary>
        /// данные для таблицы
        /// </summary>
        public ListDataSet DrawingSamplesDS { get; set; }
        /// <summary>
        /// Флаг необходимости обновить время завершения разработки чертежа
        /// </summary>
        private bool UpdateFinalTime;
        /// <summary>
        /// Список колонок грида
        /// </summary>
        private List<DataGridHelperColumn> Columns { get; set; }

        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// Обработка и выполнение команд
        /// </summary>
        /// <param name="command"></param>
        public void ProcessCommand(string command, ItemMessage m = null)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "refresh":
                        if (UpdateFinalTime)
                        {
                            FinishDrawing();
                        }
                        else
                        {
                            Grid.LoadItems();
                        }
                        break;
                    case "help":
                        Central.ShowHelp(DocumentationUrl);
                        break;
                    case "constructorconfirmationdrawing":
                        ConstructorConfirmationDrawing();
                        break;
                    case "startdrawing":
                        StartDrawing();
                        break;
                    case "enddrawing":
                        EndDrawing();
                        break;
                    case "showsimilar":
                        ShowSimilar();
                        break;
                    case "toexcel":
                        ExportToExcel();
                        break;
                    case "open":
                        SampleEdit();
                        break;
                }
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            FromDate.Text = DateTime.Now.AddDays(-7).ToString("dd.MM.yyyy");
            ToDate.Text = DateTime.Now.AddDays(7).ToString("dd.MM.yyyy");

            UpdateFinalTime = false;
            StartButton.IsEnabled = false;
            EndButton.IsEnabled = false;
        }

        public void InitGrid()
        {
            Columns = new List<DataGridHelperColumn>
            {
                // Номер строки результата запроса. Колонка нужна для первичной сортировки
                new DataGridHelperColumn()
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn()
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn()
                {
                    Header="Дата создания",
                    Path="DT_CREATED",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn()
                {
                    Header="Дата изготовления",
                    Path="DT_COMPLITED",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn()
                {
                    Header="Покупатель",
                    Path="NAME_POK",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=40,
                },
                new DataGridHelperColumn()
                {
                    Header="Образец",
                    Path="NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=40,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.ForegroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";
                                int duplicateFlag = row.CheckGet("DUPLICATE").ToInt();

                                if (duplicateFlag == 2)
                                {
                                    color = HColor.RedFG;
                                }
                                else if (duplicateFlag == 1)
                                {
                                    color = HColor.BlueFG;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        },
                    }
                },
                new DataGridHelperColumn()
                {
                    Header="Примечание",
                    Path="NAME_NOTE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn()
                {
                    Header="Комментарий",
                    Path="NOTE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn()
                {
                    Header="Картон",
                    Path="NAME_CARDBOARD",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn()
                {
                    Header="Размер партии",
                    Path="ORDER_QTY",
                    ColumnType=ColumnTypeRef.Double,
                    Format = "N0",
                    Width2=14,
                },
                new DataGridHelperColumn()
                {
                    Header="Размер развертки",
                    Path="BLANK_SIZE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=14,
                },
                new DataGridHelperColumn
                {
                    Header="Код FEFCO",
                    Path="FEFCO",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Клеить",
                    Path="GLUING_TEXT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=8,
                    FormatterRaw = (row) =>
                    {
                        var result = "";
                        int gluing = row.CheckGet("GLUING").ToInt();
                        switch (gluing)
                        {
                            case 1:
                                result = "склеить";
                                break;
                            case 2:
                                result = "не клеить";
                                break;
                        }
                        return result;
                    }
                },
                new DataGridHelperColumn()
                {
                    Header="Прикрепленные файлы",
                    Path="FILE_IS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    Width2=5,
                },
                new DataGridHelperColumn()
                {
                    Header="Файл чертежа",
                    Path="DESIGN_FILE_IS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    Width2=5,
                },
                new DataGridHelperColumn()
                {
                    Header="Дополнительный файл чертежа",
                    Path="DESIGN_FILE_OTHER_IS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    Width2=5,
                },
                new DataGridHelperColumn()
                {
                    Header="Номер",
                    Path="NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=25,
                },
                new DataGridHelperColumn
                {
                    Header="Сообщений от клиента",
                    Path="UNREAD_MSG",
                    ColumnType=ColumnTypeRef.String,
                    Width2=5,
                    FormatterRaw = (v) =>
                    {
                        var result = "";

                        var t=v.CheckGet("UNREAD_MSG").ToInt();
                        if (t > 0)
                        {
                            result = t.ToString();
                        }

                        return result;
                    },
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (!string.IsNullOrEmpty(row["CHAT_MSG"]))
                                {
                                    color = HColor.Yellow;
                                }
                                if (row["UNREAD_MSG"].ToInt() > 0)
                                {
                                    color = HColor.Red;
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
                new DataGridHelperColumn
                {
                    Header="Сообщений от коллег",
                    Path="UNREAD_MESSAGE_QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=5,
                    FormatterRaw = (v) =>
                    {
                        var result = "";

                        var t=v.CheckGet("UNREAD_MESSAGE_QTY").ToInt();
                        if (t > 0)
                        {
                            result = t.ToString();
                        }

                        return result;
                    },
                    Stylers=new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row=>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row["UNREAD_MESSAGE_QTY"].ToInt() > 0)
                                {
                                    color = HColor.Red;
                                }
                                else if (row["MESSAGE_QTY"].ToInt() > 0)
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
                new DataGridHelperColumn()
                {
                    Header="Конструктор",
                    Path="CONSTRUCTOR_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=25,
                },
                new DataGridHelperColumn()
                {
                    Header="Время начала",
                    Path="START_DESIGN_DTTM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Width2=20,
                    Format="dd.MM.yy HH:mm"
                },
                new DataGridHelperColumn()
                {
                    Header="Время окончания",
                    Path="END_DESIGN_DTTM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Width2=20,
                    Format="dd.MM.yy HH:mm"
                },
                new DataGridHelperColumn()
                {
                    Header="Менеджер",
                    Path="EMPL_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=25,
                },
                new DataGridHelperColumn()
                {
                    Header="Сложность",
                    Path="DRAWING_COMPLEXITY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=25,
                },
                new DataGridHelperColumn()
                {
                    Header="Канал продаж",
                    Path="TYPE_CUSTOMER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=50,
                },
                new DataGridHelperColumn()
                {
                    Header="Время принятия",
                    Path="CONSTRUCTOR_CONFIRMED_DTTM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Width2=25,
                    Format="dd.MM.yy HH:mm"
                },
                new DataGridHelperColumn()
                {
                    Header="Путь к файлу чертежа",
                    Path="DESIGN_FILE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="Путь к дополнительному файлу чертежа",
                    Path="DESIGN_FILE_OTHER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="Длина развертки",
                    Path="BLANK_LENGTH",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="Ширина развертки",
                    Path="BLANK_WIDTH",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="Класс изделия",
                    Path="PRODUCT_CLASS_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Код FEFCO",
                    Path="ID_FEFCO",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Признак склеивания",
                    Path="GLUING",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ID внутреннего чата",
                    Path="CHAT_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };

            Grid.SetColumns(Columns);

            // Раскраска строк
            Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                {
                    StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";
                        // Выполненые чертежы
                        if (row.CheckGet("DESIGN").ToInt() == 2)
                        {
                            color=HColor.Green;
                        }
                        //Принятые чертежи
                        else if (row.CheckGet("CONSTRUCTOR_CONFIRMATION").ToInt() == 0)
                        {
                            color=HColor.Blue;
                        }
                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };
            // используем ту сортировку, которая определена в запросе.
            // добавили колонку с номером строки результата запроса, по ней выполним сортировку
            Grid.SetPrimaryKey("ID");
            Grid.SetSorting("_ROWNNMBER", ListSortDirection.Ascending);
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;

            Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                { "Copy", new DataGridContextMenuItem(){
                    Header="Копировать текст в буфер обмена",
                    Action=()=>
                    {
                       Grid.CopyCellValue();
                    }
                }},
                { "Separator0", new DataGridContextMenuItem() {
                    Header="-",
                }},
                { "ConstructorConfirmation", new DataGridContextMenuItem(){
                    Header="Принять",
                    Action=()=>
                    {
                        ConstructorConfirmationDrawing();
                    }
                }},
                { "Start", new DataGridContextMenuItem(){
                    Header="Начать",
                    Action=()=>
                    {
                        StartDrawing();
                    }
                }},
                { "End", new DataGridContextMenuItem(){
                    Header="Завершить",
                    Action=()=>
                    {
                        EndDrawing();
                    }
                }},
                { "Separator1", new DataGridContextMenuItem() {
                    Header="-",
                }},
                {
                    "SelectDifficultyLevel",
                    new DataGridContextMenuItem()
                    {
                        Header="Уровень сложности",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                        },
                        Items=new Dictionary<string, DataGridContextMenuItem>()
                        {
                            { "LowLevel",
                                new DataGridContextMenuItem()
                                {
                                    Header ="Низкий",
                                    Action=() =>
                                    {
                                        SetDifficultyLevel(1);
                                    }
                                }
                            },
                            { "MediumLevel",
                                new DataGridContextMenuItem()
                                {
                                    Header ="Средний",
                                    Action=() =>
                                    {
                                        SetDifficultyLevel(2);
                                    }
                                }
                            },
                            { "HighLevel",
                                new DataGridContextMenuItem()
                                {
                                    Header ="Высокий",
                                    Action=() =>
                                    {
                                        SetDifficultyLevel(3);
                                    }
                                }
                            },
                        }
                    }
                },

                { "Separator2", new DataGridContextMenuItem() {
                    Header="-",
                }},
                { "OpenChat", new DataGridContextMenuItem(){
                    Header="Открыть чат",
                    Action=()=>
                    {
                        OpenChat(0);
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
                { "NotRequired", new DataGridContextMenuItem(){
                    Header="Чертеж не требуется",
                    Action=()=>
                    {
                        DesignNotRequired();
                    }
                }},
                { "Separator3", new DataGridContextMenuItem() {
                    Header="-",
                }},
                { "TechnologicalMap", new DataGridContextMenuItem(){
                    Header="Прикрепить ТК",
                    Action=()=>
                    {
                        AttachTechnologicalMap();
                    }
                }},
                { "OpenDrawing",
                    new DataGridContextMenuItem()
                    {
                        Header="Открыть чертёж",
                        Action=() =>
                        {
                        },
                        Items=new Dictionary<string, DataGridContextMenuItem>()
                        {
                            {
                                "OpenInMainFormat",
                                new DataGridContextMenuItem()
                                {
                                    Header ="В основном формате",
                                    Action=() =>
                                    {
                                        Central.OpenFile(Grid.SelectedItem.CheckGet("DESIGN_FILE"));
                                    }
                                }
                            },
                            {
                                "OpenInOtherFormat",
                                new DataGridContextMenuItem()
                                {
                                    Header ="В дополнительном формате",
                                    Action=() =>
                                    {
                                        Central.OpenFile(Grid.SelectedItem.CheckGet("DESIGN_FILE_OTHER"));
                                    }
                                }
                            },
                        }
                    }
                },
                { "OpenFolder", new DataGridContextMenuItem(){
                    Header="Открыть папку",
                    Action=()=>
                    {
                        OpenFolder();
                    }
                }},
                { "EditBlankSize", new DataGridContextMenuItem(){
                    Header="Размер развертки",
                    Action=()=>
                    {
                        EditBlankSize();
                    }
                }},
                { "CheckingSample", new DataGridContextMenuItem(){
                    Header="Образец для проверки",
                    Action=()=>
                    {
                        CheckingSample();
                    }
                }},
            };

            Grid.SearchText = SearchText;
            //данные грида
            Grid.OnLoadItems = LoadItems;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = selectedItem =>
            {
                UpdateActions(selectedItem);
            };

            Grid.Init();
        }

        /// <summary>
        /// Обработка строк
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        private ListDataSet ProcessItems(ListDataSet ds)
        {
            var _ds = ds;
            if (ds != null)
            {
                if (ds.Items.Count > 0)
                {
                    foreach (var row in _ds.Items)
                    {
                        var lz = row.CheckGet("BLANK_LENGTH").ToInt();
                        var bz = row.CheckGet("BLANK_WIDTH").ToInt();
                        string blankSize = "";
                        if (lz > 0 && bz > 0)
                        {
                            blankSize = $"{lz}х{bz}";
                        }
                        row.CheckAdd("BLANK_SIZE", blankSize);
                    }
                }
            }

            return _ds;
        }

        /// <summary>
        /// Загрузка данных из БД
        /// </summary>
        public async void LoadItems()
        {
            Grid.ShowSplash();
            GridToolbar.IsEnabled = false;

            bool resume = true;

            if (resume)
            {
                var f = FromDate.Text.ToDateTime();
                var t = ToDate.Text.ToDateTime();
                if (DateTime.Compare(f, t) > 0)
                {
                    var msg = "Дата начала должна быть меньше даты окончания.";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();
                    resume = false;
                }
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "SampleDrawing");
                q.Request.SetParam("Action", "List");

                q.Request.SetParam("FROM_DATE", FromDate.Text);
                q.Request.SetParam("TO_DATE", ToDate.Text);
                q.Request.SetParam("HAVE_COMPLETED", (bool)HaveCompletedCheckBox.IsChecked ? "1" : "0");

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
                        var ds = ListDataSet.Create(result, "DRAWING_SAMPLES");
                        DrawingSamplesDS = ProcessItems(ds);
                        Grid.UpdateItems(DrawingSamplesDS);
                    }
                }

                ShowButton.Style = (Style)ShowButton.TryFindResource("Button");
            }

            Grid.HideSplash();
            GridToolbar.IsEnabled = true;
        }

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem"></param>
        public void UpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;

            var row = Grid.SelectedItem;
            if (row != null)
            {
                if (row.CheckGet("DESIGN").ToInt() == 2)
                {
                    ConstructorConfirmationButton.IsEnabled = false;
                    StartButton.IsEnabled = false;
                    EndButton.IsEnabled = false;
                    Grid.Menu["ConstructorConfirmation"].Enabled = false;
                    Grid.Menu["Start"].Enabled = false;
                    Grid.Menu["End"].Enabled = false;
                }
                else if (row.CheckGet("CONSTRUCTOR_CONFIRMATION").ToInt() == 1)
                {
                    if (row.CheckGet("CONSTRUCTOR_EMPL_ID").ToInt() == Central.User.EmployeeId && !row.CheckGet("START_DESIGN_DTTM").IsNullOrEmpty())
                    {
                        ConstructorConfirmationButton.IsEnabled = false;
                        StartButton.IsEnabled = false;
                        EndButton.IsEnabled = true;
                        Grid.Menu["ConstructorConfirmation"].Enabled = false;
                        Grid.Menu["Start"].Enabled = false;
                        Grid.Menu["End"].Enabled = true;
                    }
                    else
                    {
                        ConstructorConfirmationButton.IsEnabled = false;
                        StartButton.IsEnabled = true;
                        EndButton.IsEnabled = false;
                        Grid.Menu["ConstructorConfirmation"].Enabled = false;
                        Grid.Menu["Start"].Enabled = true;
                        Grid.Menu["End"].Enabled = false;
                    }
                }
                else
                {
                    ConstructorConfirmationButton.IsEnabled = true;
                    StartButton.IsEnabled = false;
                    EndButton.IsEnabled = false;
                    Grid.Menu["ConstructorConfirmation"].Enabled = true;
                    Grid.Menu["Start"].Enabled = false;
                    Grid.Menu["End"].Enabled = false;
                }

                var isMainDrawing = !row.CheckGet("DESIGN_FILE").IsNullOrEmpty();
                var isOtherDrawing = !row.CheckGet("DESIGN_FILE_OTHER").IsNullOrEmpty();

                Grid.Menu["OpenDrawing"].Items["OpenInMainFormat"].Enabled = isMainDrawing;
                Grid.Menu["OpenDrawing"].Items["OpenInOtherFormat"].Enabled = isOtherDrawing;
                Grid.Menu["OpenDrawing"].Enabled = isMainDrawing || isOtherDrawing;
                Grid.Menu["OpenFolder"].Enabled = isMainDrawing || isOtherDrawing;

                int productClass = selectedItem.CheckGet("PRODUCT_CLASS_ID").ToInt();
                Grid.Menu["NotRequired"].Enabled = productClass != 10;
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

        /// <summary>
        /// Отображение образца
        /// </summary>
        private void SampleEdit()
        {
            int id = SelectedItem.CheckGet("ID").ToInt();

            var sampleForm = new Sample();
            sampleForm.ReceiverName = ControlName;
            sampleForm.Edit(id);
        }

        /// <summary>
        /// Открытие папки с файлом в проводнике
        /// </summary>
        private void OpenFolder()
        {
            if (Grid.SelectedItem != null)
            {
                var pathDesign = Grid.SelectedItem.CheckGet("DESIGN_FILE");
                System.Diagnostics.Process.Start("explorer.exe", $"/select, {pathDesign}");
            }
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
                chatFrame.ChatId = Grid.SelectedItem.CheckGet("CHAT_ID").ToInt();
                chatFrame.ObjectId = Grid.SelectedItem.CheckGet("ID").ToInt();
                chatFrame.ReceiverName = ControlName;
                chatFrame.Recipient = 23;
                chatFrame.RawMissingFlag = 0;
                chatFrame.Edit();
            }
        }

        /// <summary>
        /// Выбор уровня сложности
        /// </summary>
        private async void SetDifficultyLevel(int lvl)
        {
            int id = SelectedItem.CheckGet("ID").ToInt();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "AddLevel");

            q.Request.SetParam("ID_SMPL", id.ToString());
            q.Request.SetParam("DRAWING_COMPLEXITY", lvl.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                LoadItems();
            }
        }

        /// <summary>
        /// Вызывает окно редактирования размеров развертки
        /// </summary>
        private void EditBlankSize()
        {
            var row = Grid.SelectedItem;
            if (row.CheckGet("ID").ToInt() > 0)
            {
                var p = new Dictionary<string, string>()
                {
                    { "ID", row.CheckGet("ID") },
                    { "BLANK_LENGTH", row.CheckGet("BLANK_LENGTH") },
                    { "BLANK_WIDTH", row.CheckGet("BLANK_WIDTH") },
                    { "ID_FEFCO", row.CheckGet("ID_FEFCO").ToInt().ToString() },
                    { "GLUING", row.CheckGet("GLUING") },
                };
                var blankSize = new SampleEditBlankSize();
                blankSize.ReceiverName = ControlName;
                blankSize.Edit(p);
            }
        }

        /// <summary>
        /// Обработка принятия изготовления чертежа
        /// </summary>
        private async void ConstructorConfirmationDrawing()
        {
            int id = SelectedItem.CheckGet("ID").ToInt();

            if (DialogWindow.ShowDialog($"Принять заявку \"{SelectedItem.CheckGet("ID")}\"?"
                , "Изготовление чертежа"
                , ""
                , DialogWindowButtons.NoYes) != true) return;

            if (id > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "SampleDrawing");
                q.Request.SetParam("Action", "ConstructorConfirmation");

                q.Request.SetParam("ID_SMPL", id.ToString());
                q.Request.SetParam("CONSTRUCTOR_EMPL_ID", Central.User.EmployeeId.ToString());
                //q.Request.SetParam("START_DESIGN_DTTM", DateTime.Now.ToString());

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                }
                );

                if (q.Answer.Status == 0)
                {
                    var resultData = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (resultData != null)
                    {
                        if (resultData.ContainsKey("ITEMS"))
                        {
                            // если ответ не пустой, обновляем таблицу
                            Grid.LoadItems();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Обработка начала изготовления чертежа
        /// </summary>
        private async void StartDrawing()
        {
            int id = SelectedItem.CheckGet("ID").ToInt();

            if (id > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "SampleDrawing");
                q.Request.SetParam("Action", "Start");

                q.Request.SetParam("ID_SMPL", id.ToString());
                q.Request.SetParam("CONSTRUCTOR_EMPL_ID", Central.User.EmployeeId.ToString());
                q.Request.SetParam("START_DESIGN_DTTM", DateTime.Now.ToString());

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                }
                );

                if (q.Answer.Status == 0)
                {
                    var resultData = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (resultData != null)
                    {
                        if (resultData.ContainsKey("ITEMS"))
                        {
                            // если ответ не пустой, обновляем таблицу
                            Grid.LoadItems();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Обработка окончания изготовления чертежа
        /// </summary>
        private async void FinishDrawing()
        {
            UpdateFinalTime = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleDrawing");
            q.Request.SetParam("Action", "End");

            q.Request.SetParam("ID_SMPL", Grid.SelectedItem["ID"]);
            q.Request.SetParam("END_DESIGN_DTTM", DateTime.Now.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            }
            );

            if (q.Answer.Status == 0)
            {
                var resultData = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (resultData != null)
                {
                    if (resultData.ContainsKey("ITEMS"))
                    {
                        // если ответ не пустой, обновляем таблицу
                        Grid.LoadItems();
                    }
                }
            }
        }

        /// <summary>
        /// Проверки и внесение дополнительной информации перед завершением разработки чертежа
        /// </summary>
        private void EndDrawing()
        {
            if (Grid.SelectedItem != null)
            {
                if (Grid.SelectedItem.CheckGet("DESIGN_FILE_IS").ToBool() || Grid.SelectedItem.CheckGet("DESIGN_FILE_OTHER_IS").ToBool())
                {
                    UpdateFinalTime = true;
                    EditBlankSize();
                }
                else
                {
                    var dw = new DialogWindow("Прикрепите чертеж!", "Завершение разработки чертежа");
                    dw.ShowDialog();
                }
            }
        }

        /// <summary>
        /// Создание проверочного образца
        /// </summary>
        private async void CheckingSample()
        {
            var row = Grid.SelectedItem;
            if (row != null)
            {
                string msg = "";
                if (!string.IsNullOrEmpty(row.CheckGet("DESIGN_FILE")) || !string.IsNullOrEmpty(row.CheckGet("DESIGN_FILE_OTHER")))
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "Samples");
                    q.Request.SetParam("Action", "SaveCheckingSample");

                    q.Request.SetParam("ID", row.CheckGet("ID"));
                    q.Request.SetParam("DESIGN_FILE", row.CheckGet("DESIGN_FILE"));
                    q.Request.SetParam("DESIGN_FILE_OTHER", row.CheckGet("DESIGN_FILE_OTHER"));

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    }
                    );

                    if (q.Answer.Status == 0)
                    {
                        var resultData = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (resultData != null)
                        {
                            if (resultData.ContainsKey("ITEMS"))
                            {
                                msg = "Заявка на образец успешно создана";
                            }
                        }
                    }
                    else if (q.Answer.Error.Code == 145)
                    {
                        msg = q.Answer.Error.Message;
                    }
                }
                else
                {
                    msg = "Прикрепите чертеж";
                }

                var dw = new DialogWindow(msg, "Образец для проверки");
                dw.ShowDialog();

            }
        }

        /// <summary>
        /// Отмена разработки чертежа для образца. У образца поле Чертеж заполняется значением Не требуется
        /// </summary>
        private async void DesignNotRequired()
        {
            var row = Grid.SelectedItem;
            if (row != null)
            {
                // Подтверждаем отмену разработки чертежа
                var dw = new DialogWindow($"Отменить разработку чертежа для образца {row.CheckGet("NAME")}?", "Отмена разработки чертежа", "", DialogWindowButtons.YesNo);
                if ((bool)dw.ShowDialog())
                {
                    if (dw.ResultButton == DialogResultButton.Yes)
                    {
                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Preproduction");
                        q.Request.SetParam("Object", "SampleDrawing");
                        q.Request.SetParam("Action", "NotRequired");

                        q.Request.SetParam("ID", row.CheckGet("ID"));

                        q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                        q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                        await Task.Run(() =>
                        {
                            q.DoQuery();
                        });

                        if (q.Answer.Status == 0)
                        {
                            var resultData = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                            if (resultData != null)
                            {
                                if (resultData.ContainsKey("ITEMS"))
                                {
                                    Grid.LoadItems();
                                }
                            }
                        }
                    }
                }

            }
        }

        /// <summary>
        /// Открывает форму поиска и привязки ТК
        /// </summary>
        private void AttachTechnologicalMap()
        {
            UpdateFinalTime = false;

            var attachMapForm = new SampleAttachTechnologicalMap();
            attachMapForm.SampleId = Grid.SelectedItem.CheckGet("ID").ToInt();
            attachMapForm.ReceiverName = ControlName;
            attachMapForm.Show();
        }

        private void ShowSimilar()
        {
            var row = Grid.SelectedItem;
            if (row != null)
            {
                var p = new Dictionary<string, string>()
                {
                    { "LENGTH", row.CheckGet("SAMPLE_LENGTH") },
                    { "WIDTH", row.CheckGet("SAMPLE_WIDTH") },
                    { "HEIGHT", row.CheckGet("SAMPLE_HEIGHT") },
                    { "PRODUCT_CLASS_ID", row.CheckGet("PRODUCT_CLASS_ID") },
                    { "TASK_ID", row.CheckGet("ID") },
                    { "PRODUCT_NAME", $"{row.CheckGet("NAME_POK")} {row.CheckGet("NAME")}" }
                };

                var similarFrame = new RigCalculationTaskSimilar();
                similarFrame.TaskValues = p;
                similarFrame.ReceiverName = ControlName;
                similarFrame.Show();
            }
        }

            /// <summary>
            /// Экспорт в Excel
            /// </summary>
            private async void ExportToExcel()
        {
            var list = Grid.Items;

            var eg = new ExcelGrid();
            var columns = Columns;
            columns.RemoveAt(0);
            eg.SetColumnsFromGrid(columns);
            eg.Items = list;
            await Task.Run(() =>
            {
                eg.Make();
            });

        }

        /// <summary>
        /// Изменение цвета кнопки Показать на синий
        /// </summary>
        private void DateTextChanged()
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
        }

        private void ToDateTextChanged(object sender, TextChangedEventArgs e)
        {
            DateTextChanged();
        }

        private void FromDateTextChanged(object sender, TextChangedEventArgs e)
        {
            DateTextChanged();
        }

        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            var b = (Button)sender;
            if (b != null)
            {
                var t = b.Tag.ToString();
                ProcessCommand(t);
            }
        }

        private void HaveCompletedCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }
    }
}
