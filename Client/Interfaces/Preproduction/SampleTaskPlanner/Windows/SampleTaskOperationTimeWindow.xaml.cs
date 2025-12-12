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

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Окно определения оценочного времени изготовления образца
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleTaskOperationTimeWindow : UserControl
    {
        public SampleTaskOperationTimeWindow()
        {
            InitializeComponent();
            InitForm();
            InitGrid();
            InitRawGrid();
            SetDefaults();
        }

        /// <summary>
        /// форма
        /// </summary>
        public FormHelper Form { get; set; }
        /// <summary>
        /// Окно редактирования примечания
        /// </summary>
        public Window Window { get; set; }
        /// <summary>
        /// Название окна получателя сообщения
        /// </summary>
        public string ReceiverName;
        /// <summary>
        /// Количество образцов
        /// </summary>
        private int Qty;
        /// <summary>
        /// Группа изделия: 1 - 4-клапанная коробка, 2 - ИСВ, 3 - обечайка, 4 - решетки, 5 - лист
        /// </summary>
        private int ProductGroupId;
        /// <summary>
        /// Время на изготовление образца в секундах. Используется при оценке времени с помощью выбора операций
        /// </summary>
        private decimal ProductTime;
        /// <summary>
        /// ИД картона для образцов
        /// </summary>
        private int CardboardId;

        /// <summary>
        /// инициализация формы
        /// </summary>
        private void InitForm()
        {
            Form = new FormHelper();
            //список полей формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="ID_SMPL",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="MACHINE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="MINUTES",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=EstimateTime,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="RAW_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="BLANK_QTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ReserveQty,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
            };
            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
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
                    Header="#",
                    Path="NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=30,
                },
                new DataGridHelperColumn
                {
                    Header="Операция",
                    Path="OPERATION",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=100,
                    MaxWidth=200,
                },
                new DataGridHelperColumn
                {
                    Header="Есть",
                    Path="CHECKING",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=50,
                    MaxWidth=50,
                    Editable=true,
                    OnClickAction = (row,el) =>
                    {
                        var c=(CheckBox)el;
                        SetTime(row["NUM"], (bool)c.IsChecked);
                        return null;
                    },
                },
            };
            Grid.SetColumns(columns);
            Grid.SetSorting("NUM", ListSortDirection.Ascending);
            Grid.UseSorting = false;
            Grid.AutoUpdateInterval = 0;
            Grid.Init();

            Grid.Run();
        }

        private void InitRawGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Длина",
                    Path="LENGTH",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header="Ширина",
                    Path="WIDTH",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header="Листов в наличии",
                    Path="QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header="Листов в резерве",
                    Path="RESERVE_QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header="Место хранения",
                    Path="RACK_PLACE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=30,
                    MaxWidth=50,
                },
            };
            RawGrid.SetColumns(columns);
            RawGrid.AutoUpdateInterval = 0;
            RawGrid.Init();

        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            Form.SetDefaults();
            ReceiverName = "";

            var list = new List<Dictionary<string, string>>();
            foreach (var item in SampleTaskPlannerElements.SampleOperation)
            {
                var el = new Dictionary<string, string>()
                {
                    { "NUM", item.Key },
                    { "OPERATION", item.Value },
                    { "CHECKING", "0" },
                };
                list.Add(el);
            }
            var ds = ListDataSet.Create(list);
            Grid.UpdateItems(ds);
        }

        /// <summary>
        /// Расчет вемени при выборе операции
        /// </summary>
        /// <param name="oper">Код операции</param>
        /// <param name="add">Операция отмечена</param>
        public void SetTime(string oper, bool add)
        {
            int time = SampleTaskPlannerElements.GetOperationTime(ProductGroupId, oper.ToInt());
            // Время упаковки относится ко всей заявке на образцы. Разделим на количество
            if ((oper.ToInt() == 7) && ((bool)SampleTime.IsChecked))
            {
                time /= Qty;
            }

            if (add)
            {
                ProductTime += time;
            }
            else
            {
                ProductTime -= time;
            }
            ShowTime();
        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "PreproductionSample",
                ReceiverName = "",
                SenderName = "EstimateOperations",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;

                case Key.Enter:
                    Save();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// редактирование
        /// </summary>
        public void Edit(Dictionary<string, string> values)
        {
            Qty = values.CheckGet("QTY").ToInt();
            var pClassId = values.CheckGet("PRODUCT_CLASS_ID").ToInt();
            ProductGroupId = SampleTaskPlannerElements.SampleProductGroup(pClassId);
            ProductTime = ((decimal)values.CheckGet("ESTIMATE").ToInt()) * 60 / Qty;
            values.CheckAdd("MINUTES", ProductTime.ToString());

            CardboardId = values.CheckGet("CARDBOARD_ID").ToInt();
            if (CardboardId > 0)
            {
                GetData();
            }

            Form.SetValues(values);
            Show();
        }

        private async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleCardboards");
            q.Request.SetParam("Action", "ListRack");

            q.Request.SetParam("IDC", CardboardId.ToString());

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
                    var ds = ListDataSet.Create(result, "SampleCardboard");
                    RawGrid.UpdateItems(ds);
                    var v = Form.GetValues();
                    var rawId = v.CheckGet("RAW_ID").ToInt();
                    if (rawId > 0)
                    {
                        RawGrid.SetSelectedItemId(rawId);
                    }
                }
            }
        }

        /// <summary>
        /// Показывает окно
        /// </summary>
        private void Show()
        {
            string title = $"Оценка времени изготовления";

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
            EstimateTime.Focus();

        }

        /// <summary>
        /// Пересчитывает и заносит результат в поле времени в зависимости от состояния полей формы
        /// </summary>
        private void ShowTime()
        {
            var qty = 1;
            if ((bool)OrderTime.IsChecked)
            {
                qty = Qty;
            }
            EstimateTime.Text = Math.Ceiling(ProductTime * qty / 60).ToString();
        }

        public void Save()
        {
            var v = Form.GetValues();
            var estimate = v["MINUTES"].ToInt();
            bool resume = true;
            string errorMsg = "";

            // Если отмечен пункт Время на один образец, то время умножаем на количество
            if ((bool)SampleTime.IsChecked)
            {
                estimate *= Qty;
            }

            // Передадим выбранную строку в таблице сырья
            v.CheckAdd("RAW_ID", RawGrid.SelectedItem.CheckGet("ID"));

            // Проверим, что время изготовления не превышает продолжительность смены. Иначе выводим сообщение об ошибке
            if (estimate < 720)
            {
                v.CheckAdd("ESTIMATE", estimate.ToString());
            }
            else
            {
                errorMsg = "Слишком большое время";
                resume = false;
            }

            // Количество листов должно быть заполнено
            if (string.IsNullOrEmpty(ReserveQty.Text))
            {
                errorMsg = "Нет количества листов сырья";
                resume = false;
            }

            if (resume)
            {
                SaveData(v);
            }
            else
            {
                Form.SetStatus(errorMsg, 1);
            }
        }

        private async void SaveData(Dictionary<string, string> vls)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleTask");
            q.Request.SetParam("Action", "Append");
            q.Request.SetParams(vls);

            q.Request.Timeout = Central.Parameters.RequestTimeoutMax;
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
                    if (result.ContainsKey("ITEMS"))
                    {
                        //отправляем сообщение с данными и закрываем окно
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "PreproductionSample",
                            ReceiverName = ReceiverName,
                            SenderName = "EstimateOperations",
                            Action = "Refresh",
                        });
                        Close();
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        public void Close()
        {
            var window = this.Window;
            if (window != null)
            {
                window.Close();
            }
            Destroy();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CalcSampleTime(object sender, RoutedEventArgs e)
        {
            ShowTime();
        }
    }
}
