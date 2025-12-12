using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Вкладка создания производственного задания на картон для образцов
    /// </summary>
    /// <author>Рясной П.В.</author>
    public partial class SampleCardboardCreateTask : UserControl
    {
        public SampleCardboardCreateTask()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitForm();
            InitGrid();
            SetDefaults();
        }

        /// <summary>
        /// Минимальная длина задания
        /// </summary>
        int MinTaskLength;

        /// <summary>
        /// ID картона
        /// </summary>
        public int Idc;

        /// <summary>
        /// название картона
        /// </summary>
        public string CardboardName;

        /// <summary>
        /// Имя вкладки, которая становится активной после закрытия формы редактирования
        /// </summary>
        public string ReturnTabName { get; set; }

        public FormHelper Form { get; set; }

        /// <summary>
        /// Выбранная строка в списке асортимента картона для образцов
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// Остановка вспомогательных процессов при закрытии вкладки
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Preproduction",
                ReceiverName = "SampleCardboardTask",
                SenderName = "CreateTask",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
            //останавливаем таймеры грида
            Grid.Destruct();

            if (!ReturnTabName.IsNullOrEmpty())
            {
                Central.WM.SetActive(ReturnTabName, true);
                ReturnTabName = "";
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            MinTaskLength = 70;
            Idc = 0;
            TaskLength.Text = MinTaskLength.ToString();
            Status.Text = "";
        }

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
                    Path="LENGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TaskLength,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
            };
            Form.SetFields(fields);
        }

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Название",
                    Path="NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=80,
                    MaxWidth=300,
                },
                new DataGridHelperColumn
                {
                    Header="Номер ПЗ",
                    Path="PZ_NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=70,
                    MaxWidth=70,
                },
                 new DataGridHelperColumn
                {
                    Header="ИД ПЗ",
                    Path="ID_PZ",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=70,
                    MaxWidth=70,
                },
               new DataGridHelperColumn
                {
                    Header="Код товара",
                    Path="ID2",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Категория",
                    Path="IDK1",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Всего заданий",
                    Path="COUNT_PPZ",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Есть задание",
                    Path="PZ_IS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Ширина гофрополотна",
                    Path="WEB_WIDTH",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ПЗ в плане ГА",
                    Path="BQE_IS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Длина листа",
                    Path="L",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };

            Grid.SetColumns(columns);

            // Раскраска строк
            Grid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";
                        // Есть подходящие задания
                        if (!string.IsNullOrEmpty(row["ID_PZ"]))
                        {
                            color=HColor.Green;
                        }
                        // Уже создано задание на заготовки
                        if (row["PZ_IS"].ToInt() != 0)
                        {
                            color=HColor.Yellow;
                        }

                        if (!color.IsNullOrEmpty())
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
                {
                    DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        // Задание включено в план
                        if (row["BQE_IS"].ToInt() == 1)
                        {
                            color=HColor.BlueFG;
                        }

                        if (!color.IsNullOrEmpty())
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                }
            };

            Grid.Init();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };

            Grid.Run();

        }

        /// <summary>
        /// Обработчик сообщений
        /// </summary>
        /// <param name="m">сообщение</param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("Preproduction") > -1)
            {
                if (m.ReceiverName.IndexOf("SampleCardboardCreateTask") > -1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            GetData();
                            break;
                    }

                }
            }
        }

        private void UpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;
            UpdateTaskSheets();
        }

        /// <summary>
        /// Получение данных из БД
        /// </summary>
        private async void GetData()
        {
            Grid.ShowSplash();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleCardboards");
            q.Request.SetParam("Action", "PreformList");
            q.Request.SetParam("IDC", Idc.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            }
            );

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result.Count > 0)
                {
                    if (result.ContainsKey("PreformList"))
                    {
                        var PreformDS = result["PreformList"];
                        PreformDS?.Init();
                        Grid.UpdateItems(PreformDS);
                    }
                }
            }

            Grid.HideSplash();
        }

        /// <summary>
        /// Отображение вкладки для нового ПЗ на заготовки
        /// </summary>
        private void ShowTaskTab()
        {
            Central.WM.AddTab($"CreateTask_{Idc}", "Новое ПЗ на заготовки", true, "add", this);
        }

        /// <summary>
        /// Создание вкладки для нового ПЗ на заготовки. Основной метод
        /// </summary>
        /// <param name="Id"></param>
        public void Show(int id)
        {
            Idc = id;
            Cardboard.Text = CardboardName;
            GetData();
            ShowTaskTab();
        }

        /// <summary>
        /// Закрытие вкладки для нового ПЗ на заготовки
        /// </summary>
        public void Close()
        {
            Central.WM.RemoveTab($"CreateTask_{Idc}");

            Destroy();
        }

        /// <summary>
        /// Проверки перед сохранением данных
        /// </summary>
        private void Save()
        {
            var validationResult = Form.Validate();
            string error = "";

            if (!validationResult)
            {
                error = "Не все обязательные поля заполнены верно";
            }

            if (validationResult)
            {
                if (TaskLength.Text.ToInt() < MinTaskLength)
                {
                    error = $"Длина ПЗ должна быть больше или равна {MinTaskLength} м";
                    validationResult = false;
                }
            }

            if (validationResult)
            {
                if (SelectedItem["ID_PZ"].ToInt() == 0)
                {
                    error = "Для данной заготовки нет ПЗ";
                    validationResult = false;
                }
            }
            
            if (validationResult)
            {
                SaveData(SelectedItem);
            }
            else
            {
                Form.SetStatus(error, 1);
            }
        }

        /// <summary>
        /// сохранение данных
        /// </summary>
        /// <param name="p">содержимое выбранной в таблице строки</param>
        private async void SaveData(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleCardboards");
            q.Request.SetParam("Action", "CreateTask");
            q.Request.SetParam("LENGTH", TaskLength.Text);
            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            }
            );

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result.Count > 0)
                {
                    //Если ответ не пустой, отправляем сообщение Гриду о необходимости обновить данные
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "Preproduction",
                        ReceiverName = "SampleCardboardList",
                        SenderName = "SampleCardboardCreateTask",
                        Action = "TaskCreated",
                    });
                }
                Close();
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Обновляет количество листов, которое будет получено из заданной длины задания
        /// </summary>
        private void UpdateTaskSheets()
        {
            int s = 0;
            if (SelectedItem != null)
            {
                var t = TaskLength.Text.ToDouble();
                var l = SelectedItem.CheckGet("L").ToInt();
                if (l > 0)
                {
                    s = (int)Math.Ceiling(t * 1000 / l);
                }
            }
            TaskSheets.Text = $"(листов - {s})";

        }

        /// <summary>
        /// Обработчик нажатия на кнопку сохранения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку отмены
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TaskLength_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTaskSheets();
        }
    }
}
