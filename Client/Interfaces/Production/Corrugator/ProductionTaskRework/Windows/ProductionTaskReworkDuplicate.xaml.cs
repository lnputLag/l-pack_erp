using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Окно создания дублирующего задания на ГА
    /// </summary>
    public partial class ProductionTaskReworkDuplicate : UserControl
    {
        public ProductionTaskReworkDuplicate()
        {
            InitializeComponent();

            TaskId = 0;
            ReturnTabName = "";

            ReworkReasonDS = new ListDataSet();
            ReworkReasonDS.Init();
            Threads = new Dictionary<string, string>();
            Raws = new List<Dictionary<string, string>>();

            InitForm();
        }

        /// <summary>
        /// ID ПЗ, которое надо продублировать
        /// </summary>
        public int TaskId;

        /// <summary>
        /// Форма создания дублирующего задания
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Таб для возврата
        /// Если определен, фокус будет возвращен этому табу
        /// </summary>
        public string ReturnTabName { get; set; }

        /// <summary>
        /// Структура окна
        /// </summary>
        private Window Window { get; set; }

        /// <summary>
        /// Данные для списка причин создания повторного задания
        /// </summary>
        private ListDataSet ReworkReasonDS { get; set; }
        /// <summary>
        /// Идентификатор производственной площадки, на которой выполняется ПЗГА
        /// </summary>
        public int FactoryId;

        /// <summary>
        /// Данные исходного задания
        /// </summary>
        public Dictionary<string, string> PreviousTask { get; set; }
        private Dictionary<string, string> Threads { get; set; }
        private List<Dictionary<string, string>> Raws { get; set; }

        /// <summary>
        /// Инициализация формы редактирования
        /// </summary>
        private void InitForm()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="LENGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TaskLength,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.IsNotZero, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PCRR_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ReworkReason,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
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
            };
            Form.SetFields(fields);

            //после окончания стандартной валидации
            Form.OnValidate = (bool valid, string message) =>
            {
                if (valid)
                {
                    //SaveButton.IsEnabled=true;
                    FormStatus.Text = "";
                }
                else
                {
                    //SaveButton.IsEnabled=false;
                    FormStatus.Text = "Не все поля заполнены верно";
                }
            };
        }

        /// <summary>
        /// Деструктор компонентов. Завершает вспомогательные процессы
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "ProductionTask",
                ReceiverName = "RecuttingDuplicate",
                SenderName = "Duplicate",
                Action = "Closed",
            });

            GoBack();
        }

        /// <summary>
        /// Возврат на фрейм, откуда был вызван данный фрейм
        /// </summary>
        public void GoBack()
        {
            if (!string.IsNullOrEmpty(ReturnTabName))
            {
                Central.WM.SetActive(ReturnTabName, true);
                ReturnTabName = "";
            }
        }

        /// <summary>
        /// Изменение параметров для создания нового задания
        /// </summary>
        public void Edit()
        {
            if (TaskId > 0)
            {
                GetData();
                Show();
            }
        }

        /// <summary>
        /// Отображение окна
        /// </summary>
        private void Show()
        {
            int w = (int)Width;
            int h = (int)Height;
            string title = "Дублировать задание";

            Window = new Window
            {
                Title = title,
                Width = w + 17,
                Height = h + 40,
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
        /// Получение данных для полей окна
        /// </summary>
        private async void GetData()
        {
            // список причин повторного создания ПЗ
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

            // данные по позициям ПЗ и расходу бумаги
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Position");
                q.Request.SetParam("Action", "ListTaskCopy");
                q.Request.SetParam("ID", TaskId.ToString());

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var treadsDS = ListDataSet.Create(result, "ITEMS");
                        FillThreadData(treadsDS.Items);

                        var rawDS = ListDataSet.Create(result, "RAW_GROUPS");
                        Raws = rawDS.Items;
                    }
                }
            }
        }

        /// <summary>
        /// Закрытие окна
        /// </summary>
        public void Close()
        {
            if (Window != null)
            {
                Window.Close();
            }

            Destroy();
        }

        /// <summary>
        /// Заполнение данных позиций на стекерах
        /// </summary>
        /// <param name="p"></param>
        private void FillThreadData(List<Dictionary<string, string>> p)
        {
            if (p.Count == 2)
            {
                int i = 1;
                foreach (var item in p)
                {
                    Threads.CheckAdd($"ID_ORDERDATES{i}", item.CheckGet("ID"));
                    Threads.CheckAdd($"BLANK_ID{i}", item.CheckGet("BLANK_ID"));
                    Threads.CheckAdd($"GOODS_ID{i}", item.CheckGet("GOODS_ID"));
                    Threads.CheckAdd($"QTY{i}", item.CheckGet("TASK_QTY"));
                    Threads.CheckAdd($"THREAD{i}", item.CheckGet("THREADS"));
                    Threads.CheckAdd($"O{i}", item.CheckGet("TRIAL"));
                    Threads.CheckAdd($"CUTOFF_ALLOCATION{i}", item.CheckGet("STACKER"));

                    i++;
                }
            }
            else
            {
                var item = p[0];
                int i1, i2;
                if (item.CheckGet("STACKER").ToInt() == 1)
                {
                    i1 = 1;
                    i2 = 2;
                }
                else
                {
                    i1 = 2;
                    i2 = 1;
                }

                // заполненный стекер
                Threads.CheckAdd($"ID_ORDERDATES{i1}", item.CheckGet("ID"));
                Threads.CheckAdd($"BLANK_ID{i1}", item.CheckGet("BLANK_ID"));
                Threads.CheckAdd($"GOODS_ID{i1}", item.CheckGet("GOODS_ID"));
                Threads.CheckAdd($"QTY{i1}", item.CheckGet("TASK_QTY"));
                Threads.CheckAdd($"THREAD{i1}", item.CheckGet("THREADS"));
                Threads.CheckAdd($"O{i1}", item.CheckGet("TRIAL"));
                Threads.CheckAdd($"CUTOFF_ALLOCATION{i1}", item.CheckGet("STACKER"));

                // пустой стекер
                int emptyStacker = item.CheckGet("STACKER").ToInt() == 1 ? 2 : 1;
                Threads.CheckAdd($"ID_ORDERDATES{i2}", "0");
                Threads.CheckAdd($"BLANK_ID{i2}", "0");
                Threads.CheckAdd($"GOODS_ID{i2}", "0");
                Threads.CheckAdd($"QTY{i2}", "0");
                Threads.CheckAdd($"THREAD{i2}", "0");
                Threads.CheckAdd($"O{i2}", "0");
                Threads.CheckAdd($"CUTOFF_ALLOCATION{i2}", emptyStacker.ToString());
            }
        }

        /// <summary>
        /// Проверки перед сохранением
        /// </summary>
        /// <returns></returns>
        private bool CheckData()
        {
            string report = "";
            bool resume = true;

            if (resume)
            {
                // Для коротких заданий требуем подтверждения
                if (TaskLength.Text.ToInt() < 100)
                {
                    var dw = new DialogWindow("Длина задания меньше 100м. Продолжить?", "Раскрой по ПЗ", "", DialogWindowButtons.YesNo);
                    dw.Topmost = true;
                    if ((bool)dw.ShowDialog())
                    {
                        if (dw.ResultButton == DialogResultButton.No)
                        {
                            resume = false;
                        }
                    }
                    else
                    {
                        resume = false;
                    }
                }
            }

            if (resume)
            {
                if (TaskLength.Text.ToInt() > PreviousTask["LEN"].ToInt())
                {
                    var dw = new DialogWindow("Длина задания больше длины исходного задания. Продолжить?", "Раскрой по ПЗ", "", DialogWindowButtons.YesNo);
                    dw.Topmost = true;
                    if ((bool)dw.ShowDialog())
                    {
                        if (dw.ResultButton == DialogResultButton.No)
                        {
                            resume = false;
                        }
                    }
                    else
                    {
                        resume = false;
                    }
                }
            }

            if (resume)
            {
                if (ReworkReason.SelectedItem.Key.IsNullOrEmpty())
                {
                    resume = false;
                    report = "Заполните причину повторного выполнения задания";
                }
            }

            if (!string.IsNullOrEmpty(report))
            {
                FormStatus.Text = report;
            }

            return resume;
        }

        /// <summary>
        /// Сохранение данных
        /// </summary>
        private void Save()
        {
            if (CheckData())
            {
                double k = TaskLength.Text.ToDouble() / PreviousTask["LEN"].ToDouble();

                // Уменьшим в k раз количество на каждом стекере
                double qty1 = Threads.CheckGet("QTY1").ToDouble();
                if (qty1 > 0)
                {
                    var newQty = Math.Ceiling(qty1 * k).ToInt();
                    Threads["QTY1"] = newQty.ToString();
                }
                double qty2 = Threads.CheckGet("QTY2").ToDouble();
                if (qty2 > 0)
                {
                    var newQty = Math.Ceiling(qty2 * k).ToInt();
                    Threads["QTY2"] = newQty.ToString();
                }

                // Уменьшим в k раз массу расхода бумаги на каждом слое
                foreach (var item in Raws)
                {
                    var w = Math.Ceiling(item["WEIGHT"].ToDouble() * k).ToInt();
                    item["WEIGHT"] = w.ToString();
                }

                var p = new Dictionary<string, string>()
                {
                    { "PRIMARY_ID_PZ", PreviousTask.CheckGet("ID") },
                    { "TRIM_PERCENT", PreviousTask.CheckGet("TRIM") },
                    { "LENGTH", TaskLength.Text },
                    { "NUM", PreviousTask.CheckGet("NUM").Substring(0,5) },
                    { "NOTE", "" },
                    { "FORMAT", PreviousTask.CheckGet("WIDTH") },
                    { "ID_PROF", PreviousTask.CheckGet("ID_PROF") },
                    { "FIXED_WEIGHT_FLAG", PreviousTask.CheckGet("FIXED_WEIGHT_FLAG") },
                    { "REWORK_REASON", ReworkReason.SelectedItem.Key },
                    { "REWORK_COMMENT", Comments.Text },
                    { "QTY", TaskLength.Text },
                    { "PRIMARY_ID2", "0" },
                    { "FACTORY_ID", FactoryId.ToString() },
                };

                SaveData(p);
            }
        }

        /// <summary>
        /// Сохранение данных в БД
        /// </summary>
        /// <param name="p"></param>
        private async void SaveData(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "ProductionTask");
            q.Request.SetParam("Action", "SaveRework");
            q.Request.SetParams(p);
            q.Request.SetParams(Threads);

            var raws = new Dictionary<string, string>();
            foreach (var row in Raws)
            {
                int i = row["LAYER"].ToInt();
                raws.Add($"ID_RAW_GROUP{i}", row["ID_RAW_GROUP"]);
                raws.Add($"WEIGHT{i}", row["WEIGHT"]);
                raws.Add($"GLUED_FLAG{i}", row["GLUED_FLAG"]);
            }
            q.Request.SetParams(raws);

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
                        Close();
                        var taskNum = ds.Items[0]["NUM"];
                        var dw = new DialogWindow($"Создано новое задание {taskNum}", "Раскрой по ПЗ");
                        dw.ShowDialog();
                    }
                }
            }

        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
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
