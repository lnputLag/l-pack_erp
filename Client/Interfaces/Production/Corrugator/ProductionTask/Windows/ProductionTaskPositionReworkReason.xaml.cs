using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Выбор причины и комментарий к перевыгону для задагий созданныхвручную
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class ProductionTaskPositionReworkReason : ControlBase
    {
        public ProductionTaskPositionReworkReason()
        {
            InitializeComponent();

            InitForm();
            ReworkReasonDS = new ListDataSet();
            ReworkReasonDS.Init();
        }

        /// <summary>
        /// Форма редактирования задания
        /// </summary>
        FormHelper Form { get; set; }
        /// <summary>
        /// Окно редактирования примечания
        /// </summary>
        public Window Window { get; set; }
        /// <summary>
        /// Название окна получателя сообщения
        /// </summary>
        public string ReceiverName;
        /// <summary>
        /// Комментарий из задания на перевыгон для автозаполнения причины перевыгона
        /// </summary>
        public string ReworkTaskComment;
        /// <summary>
        /// Содержимое выпадающего списка причин перевыгона
        /// </summary>
        private ListDataSet ReworkReasonDS { get; set; }

        public Dictionary<string, string> ReworkParams { get; set; }

        /// <summary>
        /// Инициализация формы
        /// </summary>
        private void InitForm()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="PRIMARY_ID_PZ",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PreviousTaskNum,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="REWORK_REASON_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ReworkReason,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="COMMENTS",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Comments,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

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
                    case "save":
                        Save();
                        break;
                    case "close":
                        Close();
                        break;
                }
            }
        }

        /// <summary>
        /// Получение данных и справочников
        /// </summary>
        private async void GetData()
        {
            string taskNum = ReworkParams.CheckGet("TASK_NUM").Substring(5, 5);

            if (!string.IsNullOrEmpty(taskNum))
            {

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ProductionTask");
                q.Request.SetParam("Action", "GetPrimaryTask");
                q.Request.SetParam("POSITION_ID", ReworkParams.CheckGet("POSITION_ID"));
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
                        var ds = ListDataSet.Create(result, "TASK");
                        if ((ds.Items != null) && (ds.Items.Count > 0))
                        {
                            // Исключаем текущее задание, если оно есть, ставим выбор на задании с заданным номером, если нашли
                            int currentTaskId = ReworkParams.CheckGet("TASK_ID").ToInt();
                            var selectItems = new Dictionary<string, string>();
                            int taskNumId = 0;
                            foreach(var item in ds.Items)
                            {
                                int primaryId = item.CheckGet("PRIMARY_ID_PZ").ToInt();
                                if (primaryId != currentTaskId)
                                {
                                    string primaryNum = item.CheckGet("PRIMARY_NUM");
                                    selectItems.Add(primaryId.ToString(), primaryNum);

                                    var a = primaryNum.Substring(0, 5);
                                    if (primaryNum.Substring(0, 5) == taskNum)
                                    {
                                        taskNumId = primaryId;
                                    }
                                }
                            }

                            if (selectItems.Count > 0)
                            {
                                PreviousTaskNum.Items = selectItems;
                                if (taskNumId > 0)
                                {
                                    PreviousTaskNum.SetSelectedItemByKey(taskNumId.ToString());
                                }

                                ReworkReasonDS = ListDataSet.Create(result, "REASONS");
                                ReworkReason.Items = ReworkReasonDS.GetItemsList("ID", "REASON");

                                string taskNote = ReworkParams.CheckGet("NOTE");
                                foreach (var item in ReworkReason.Items)
                                {
                                    if (taskNote.StartsWith(item.Value))
                                    {
                                        ReworkReason.SetSelectedItem(item);
                                        break;
                                    }
                                }

                                Show();

                            }
                            else
                            {
                                var errDw = new DialogWindow("Не удалось найти доступное предыдущее ПЗ", "Отметка перевыгона");
                                errDw.ShowDialog();
                            }

                        }
                        else
                        {
                            var errDw = new DialogWindow("Не удалось найти предыдущее ПЗ", "Отметка перевыгона");
                            errDw.ShowDialog();
                        }
                    }
                    else
                    {
                        var errDw = new DialogWindow("Не удалось получить данные для предыдущего ПЗ", "Отметка перевыгона");
                        errDw.ShowDialog();
                    }
                }
                else
                {
                    var errDw = new DialogWindow("Не удалось получить данные по предыдущему ПЗ", "Отметка перевыгона");
                    errDw.ShowDialog();
                }
            }
            else
            {
                var errDw = new DialogWindow("Ошибка распознавания номера ПЗ", "Отметка перевыгона");
                errDw.ShowDialog();
            }
        }

        /// <summary>
        /// Редактирование данных для перевыгона
        /// </summary>
        /// <param name="values"></param>
        public void Edit(Dictionary<string, string> values)
        {
            ReworkParams = values;
            GetData();
        }

        /// <summary>
        /// Показывает окно
        /// </summary>
        public void Show()
        {
            string title = $"Отметка перевыгона";

            Window = new Window
            {
                Title = title,
                Width = this.Width + 24,
                Height = this.Height + 40,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow,
            };
            Window.Content = new Frame
            {
                Content = this,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            if (Window != null)
            {
                Window.Topmost = true;
                Window.ShowDialog();
            }
        }

        /// <summary>
        /// Закрытие окна
        /// </summary>
        public void Close()
        {
            var window = this.Window;
            if (window != null)
            {
                window.Close();
            }

            if (!ReceiverName.IsNullOrEmpty())
            {
                Central.WM.SetActive(ReceiverName, true);
                ReceiverName = "";
            }
        }

        /// <summary>
        /// Проверки перед сохранением
        /// </summary>
        public void Save()
        {
            var p = Form.GetValues();
            bool resume = true;

            if (p.CheckGet("PRIMARY_ID_PZ").ToInt() == 0)
            {
                resume = false;
                Form.SetStatus("Выберите задание для перевыгона", 1);
            }

            if (ReworkReason.SelectedItem.Key.ToInt() == 0)
            {
                resume = false;
                Form.SetStatus("Выберите причину перевыгона", 1);
            }

            if (resume)
            {
                SaveData(p);
            }
        }

        /// <summary>
        /// Сохранение данных в базе
        /// </summary>
        /// <param name="values"></param>
        private async void SaveData(Dictionary<string, string> values)
        {
            values.Add("TASK_ID", ReworkParams["TASK_ID"]);
            values.Add("POSITION_ID", ReworkParams["POSITION_ID"]);
            values.Add("QUANTITY", ReworkParams["QUANTITY"]);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Position");
            q.Request.SetParam("Action", "SetReworkFlag");
            q.Request.SetParams(values);
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
                    //отправляем сообщение о закрытии окна
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "ProductionTask",
                        ReceiverName = ReceiverName,
                        SenderName = "ProductionTaskPositionReworkReason",
                        Action = "Refresh",
                    });

                    Close();
                }
            }

        }

        /// <summary>
        /// Обработчик нажатия на кнопку
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            var b = (Button)sender;
            if (b != null)
            {
                var t = b.Tag.ToString();
                ProcessCommand(t);
            }
        }

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
                        k = ReworkReasonDS.Items[i + 1]["ID"].ToInt();
                        ReworkReason.SetSelectedItemByKey(k.ToString());
                    }
                    break;
                }
                i++;
            }
        }
    }
}
