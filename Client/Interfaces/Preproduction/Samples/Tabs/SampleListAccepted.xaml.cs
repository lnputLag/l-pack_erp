using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Вкладка приема образцов
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleListAccepted : UserControl
    {
        public SampleListAccepted()
        {
            InitializeComponent();
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/samples_new/samples1/priem_samples";

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitGrid();
            ProcessPermissions();

            UIUtil.ProcessPermissions("[erp]sample", this);
        }

        /// <summary>
        /// Право на выполнение специальных действий
        /// </summary>
        public bool MasterRights;

        #region Common

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// Имя вкладки
        /// </summary>
        public string TabName;
        /// <summary>
        /// Ссылка на страницу документации
        /// </summary>
        private string DocumentationUrl;

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            //флаг активности текстового ввода
            //когда курсор стоит в поле ввода (например, поиск)
            //мы запрещаем обрабатывать такие клавиши, как Del, Ins etc
            bool inputActive = false;
            if (SearchText.IsFocused)
            {
                inputActive = true;
            }

            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.F5:
                    Grid.LoadItems();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// отображение статьи в справочной системе
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp(DocumentationUrl);
        }

        /// <summary>
        /// деструктор интерфейса
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

        public void ProcessNavigation()
        {

        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Если пользователь имеет спецправа, включаем режим мастера
            var mode = Central.Navigator.GetRoleLevel("[erp]sample");
            switch (mode)
            {
                case Role.AccessMode.Special:
                    MasterRights = true;
                    break;

                default:
                    MasterRights = false;
                    break;
            }
        }

        #endregion

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

                        case "SaveNote":
                            SaveNote((Dictionary<string, string>)obj.ContextObject);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>()
            {
                new DataGridHelperColumn()
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    Doc="Номер по порядку в списке",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=50,
                },
               new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header="Веб-заявка",
                    Path="WEB_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    MinWidth=30,
                    MaxWidth=30,
                },
                new DataGridHelperColumn
                {
                    Header="Дата создания",
                    Path="DT_CREATED",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=90,
                },
                new DataGridHelperColumn
                {
                    Header="Дата изготовления",
                    Path="DT_COMPLETED",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Тип изготовления",
                    Path="PRODUCTION_TYPE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Покупатель",
                    Path="CUSTOMER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=200,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.ForegroundColor,
                            row=>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                // Заявки от внутреннего заказчика ТД Л-ПАК
                                if (row["CUSTOMER_ID"].ToInt() == 4202 && row["INNER_ORDER"].ToInt() == 1)
                                {
                                    color = HColor.MagentaFG;
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
                new DataGridHelperColumn
                {
                    Header="Образец",
                    Path="SAMPLE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=280,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="SAMPLE_COMMENT",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=200,
                },
                new DataGridHelperColumn
                {
                    Header="Комментарий",
                    Path="SAMPLE_NOTE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=200,
                },
                new DataGridHelperColumn
                {
                    Header="Картон",
                    Path="CARDBOARD_NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=150,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.ForegroundColor,
                            row=>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row["ANY_CARTON_FLAG"].ToInt() == 1)
                                {
                                    color = HColor.BlueFG;
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
                new DataGridHelperColumn
                {
                    Header="Картон для образца",
                    Path="SAMPLE_CARDBOARD",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=250,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="QTY_INFO",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=60,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row=>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row["CONFIRMATION"].ToInt() == 0)
                                {
                                    if (!string.IsNullOrEmpty(row["LIMIT_QTY"]))
                                    {
                                        // Ограничение по количеству, требуется подтверждение
                                        if (row["QTY"].ToInt() > row["LIMIT_QTY"].ToInt())
                                        {
                                            color = HColor.Red;
                                        }
                                    }
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
                new DataGridHelperColumn
                {
                    Header="Ограничение",
                    Path="LIMIT_REASON",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Обоснование",
                    Path="CONFIRMATION_REASON",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Файлы",
                    Path="FILE_IS",
                    ColumnType=ColumnTypeRef.Boolean,
                    MinWidth=40,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Сообщений от клиента",
                    Path="UNREAD_MSG",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=40,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row=>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row["UNREAD_MSG"].ToInt() > 0)
                                {
                                    color = HColor.Red;
                                }
                                else if (row["CHAT_MSG"].ToInt() > 0)
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
                new DataGridHelperColumn
                {
                    Header="Сообщений от коллег",
                    Path="UNREAD_MESSAGE_QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=40,
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
                new DataGridHelperColumn
                {
                    Header="Доставка",
                    Path="DELIVERY",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Менеджер",
                    Path="MANAGER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание технолога",
                    Path="TECHNOLOG_NOTE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Подтверждение",
                    Path="CONFIRMATION",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Без марки картона",
                    Path="ANY_CARTON_FLAG",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Разрешенное количество",
                    Path="LIMIT_QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Тип изготовления",
                    Path="PRODUCTION_TYPE_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Дублирование изделий",
                    Path="DUPLICATED",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Образец от внутреннего заказчика",
                    Path="INNER_ORDER",
                    ColumnType=ColumnTypeRef.Integer,
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

                        if (row["INNER_ORDER"].ToInt() == 1)
                        {
                                // Подтверждение не требуется
                                color = HColor.Green;
                        }
                        else if (row["CONFIRMATION"].ToInt() == 2)
                        {
                            // Количество подтверждено
                            color = HColor.Green;
                        }
                        else
                        {
                            if ((row["QTY"].ToInt() > row["LIMIT_QTY"].ToInt()) || (row["DUPLICATED"].ToInt() == 1))
                            {
                                // Есть ограничения по количеству, нужно подтверждение
                                color = HColor.Yellow;
                            }
                            else
                            {
                                // Подтверждение не требуется
                                color = HColor.Green;
                            }
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                }
            };

            Grid.Init();

            // контекстное меню
            Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                {
                    "OpenAttachments",
                    new DataGridContextMenuItem()
                    {
                        Header="Прикрепленные файлы",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            OpenAttachments();
                        }
                    }
                },
                { "ShowDrawing", new DataGridContextMenuItem(){
                    Header="Показать схему",
                    Action=()=>
                    {
                        ShowDrawing();
                    }
                }},
                {
                    "OpenChat",
                    new DataGridContextMenuItem()
                    {
                        Header="Открыть чат по образцу",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            OpenChat(0);
                        }
                    }
                },
                { "OpenInnerChat",
                    new DataGridContextMenuItem()
                    {
                        Header="Открыть внутренний чат",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            OpenChat(1);
                        }
                    }
                },
                { "EditNote", new DataGridContextMenuItem(){
                    Header="Изменить примечание",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        GetNote();
                    }
                }},
            };

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };

            //данные грида
            Grid.OnLoadItems = LoadItems;
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

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "ListAccepted");

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

                    bool emptyGrid = sampleDS.Items.Count == 0;
                    AcceptButton.IsEnabled = !emptyGrid;
                    RejectButton.IsEnabled = !emptyGrid;
                    AttachTechCardButton.IsEnabled = !emptyGrid;
                }
            }

            GridToolbar.IsEnabled = true;
            Grid.HideSplash();
        }

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem">выбранная запись</param>
        private void UpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;

            bool allowAccept = true;
            if (SelectedItem.CheckGet("INNER_ORDER").ToInt() == 0)
            {
                if (SelectedItem.CheckGet("CONFIRMATION").ToInt() == 1)
                {
                    if (SelectedItem.CheckGet("QTY").ToInt() > SelectedItem.CheckGet("LIMIT_QTY").ToInt())
                    {
                        allowAccept = false;
                    }
                    if (SelectedItem.CheckGet("DUPLICATED").ToInt() == 1)
                    {
                        allowAccept = false;
                    }
                }
            }
            AcceptButton.IsEnabled = allowAccept;
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
                chatFrame.RawMissingFlag = SelectedItem.CheckGet("RAW_MISSING_FLAG").ToInt();
                chatFrame.ObjectId = SelectedItem.CheckGet("ID").ToInt();
                chatFrame.ReceiverName = TabName;
                chatFrame.Recipient = 6;
                chatFrame.UnReadCheckBox.IsChecked = true;
                chatFrame.Edit();
            }
        }

        /// <summary>
        /// Открывает форму редактирования образца
        /// </summary>
        /// <param name="sampleId"></param>
        private void Edit(int sampleId)
        {
            var sampleForm = new Sample();
            sampleForm.ReceiverName = TabName;
            // При редактировании код подтверждения не меняем
            sampleForm.Confirmation = SelectedItem["CONFIRMATION"].ToInt();
            sampleForm.Edit(sampleId);
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
        /// Получение схемы развертки образца
        /// </summary>
        public async void ShowDrawing()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "GetScheme");
            q.Request.SetParam("ID", SelectedItem.CheckGet("ID"));

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Central.OpenFile(q.Answer.DownloadFilePath);
            }
            else if (q.Answer.Error.Code == 145)
            {
                var d = new DialogWindow($"{q.Answer.Error.Message}", "Схема образца");
                d.ShowDialog();
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null)
            {
                int sampleId = SelectedItem.CheckGet("ID").ToInt();
                if (sampleId > 0)
                {
                    if (SelectedItem.CheckGet("PRODUCTION_TYPE_ID").ToInt() == 0)
                    {
                        var sampleForm = new Sample();
                        sampleForm.ReceiverName = TabName;

                        // Для образцов от внутреннего заказчика подтверждение не требуется
                        if (SelectedItem.CheckGet("INNER_ORDER").ToInt() == 1)
                        {
                            sampleForm.Confirmation = 2;
                        }
                        // Принятые менеджерами образцы
                        else if (SelectedItem.CheckGet("CONFIRMATION").ToInt() == 2)
                        {
                            sampleForm.Confirmation = 2;
                        }
                        else
                        {
                            bool duplicated = SelectedItem.CheckGet("DUPLICATED").ToBool();
                            bool limitation = SelectedItem.CheckGet("QTY").ToInt() > SelectedItem.CheckGet("LIMIT_QTY").ToInt();

                            if (limitation || duplicated)
                            {
                                sampleForm.Confirmation = 1;
                            }
                            else
                            {
                                // Если нет ограничений, отправляем в работу
                                sampleForm.Confirmation = 2;
                            }
                        }

                        sampleForm.Edit(sampleId);
                    }
                    else
                    {
                        // Образцы с линии сразу в работу
                        UpdateStatus(1);
                    }
                }
            }
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
        /// Вызов вкладки редактирования примечания
        /// </summary>
        /// <param name="sampleId"></param>
        public void GetNote()
        {
            int sampleId = SelectedItem.CheckGet("ID").ToInt();
            if (sampleId > 0)
            {
                var sampleNote = new SampleNote();
                sampleNote.ReceiverName = TabName;
                var p = new Dictionary<string, string>()
                {
                    { "ID", sampleId.ToString() },
                    { "NOTE", SelectedItem.CheckGet("TECHNOLOG_NOTE") },
                };
                sampleNote.Edit(p);
            };
        }

        /// <summary>
        /// Сохранение примечания
        /// </summary>
        /// <param name="p"></param>
        public async void SaveNote(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "SaveStorekeeperNote");
            q.Request.SetParam("ID", p.CheckGet("ID"));
            q.Request.SetParam("STOREKEEPER_NOTE", p.CheckGet("NOTE"));

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    if (result.ContainsKey("Items"))
                    {
                        Grid.LoadItems();
                    }
                }
            }
        }

        private void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            var dw = new DialogWindow("Вы точно хотите отклонить образец?", "Отклонить образец", "", DialogWindowButtons.NoYes);
            if ((bool)dw.ShowDialog())
            {
                UpdateStatus(2);
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void AttachTechCard_Click(object sender, RoutedEventArgs e)
        {
            var attachMapForm = new SampleAttachTechnologicalMap();
            attachMapForm.SampleId = SelectedItem.CheckGet("ID").ToInt();
            attachMapForm.ReceiverName = TabName;
            attachMapForm.Show();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null)
            {
                if (SelectedItem.ContainsKey("ID"))
                {
                    Edit(SelectedItem["ID"].ToInt());
                }
            }
        }

        private void PrintMenuButton_Click(object sender, RoutedEventArgs e)
        {
            PrintMenu.IsOpen = true;
        }

        private void WorkloadReport_Click(object sender, RoutedEventArgs e)
        {
            var workloadReport = new SampleWorkloadReport();
            workloadReport.Make();
        }
    }
}
