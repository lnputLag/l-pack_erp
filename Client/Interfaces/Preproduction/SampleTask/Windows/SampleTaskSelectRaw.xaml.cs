using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Окно выбора сырья для задания на образцы
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleTaskSelectRaw : UserControl
    {
        public SampleTaskSelectRaw()
        {
            InitializeComponent();

            InitForm();
            InitGrid();
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
        /// Инициализация формы
        /// </summary>
        private void InitForm()
        {
            Form = new FormHelper();
            //список полей формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="IDC",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="PROFILE_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="ANY_CARTON_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                },
            };
            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

        /// <summary>
        /// Инициализация таблицы для списка доступного сырья
        /// </summary>
        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Сырье",
                    Path="CARDBOARD_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=80,
                    MaxWidth=200,
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
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();
            ReceiverName = "";
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
                SenderName = "SelectRaw",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        private async void LoadItems()
        {
            var v = Form.GetValues();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleCardboards");
            q.Request.SetParam("Action", "ListRack");

            q.Request.SetParam("IDC", v.CheckGet("IDC"));
            q.Request.SetParam("PROFILE_ID", v.CheckGet("PROFILE_ID"));
            q.Request.SetParam("ANY_CARTON_FLAG", v.CheckGet("ANY_CARTON_FLAG"));

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
                    var processedDS = ProcessItems(ds);
                    if (processedDS.Items.Count > 0)
                    {
                        RawGrid.UpdateItems(processedDS);
                    }
                    else
                    {
                        Form.SetStatus("Нет доступного сырья", 1);
                        SaveButton.IsEnabled = false;
                    }
                }

                Show();
            }
            else if (q.Answer.Error.Code == 145)
            {
                Form.SetStatus(q.Answer.Error.Message, 1);
            }
        }

        /// <summary>
        /// Обработка данных перед загрузкой в таблицу
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        private ListDataSet ProcessItems(ListDataSet ds)
        {
            var _ds = new ListDataSet();
            _ds.Init();

            if (ds.Items != null)
            {
                if (ds.Items.Count > 0)
                {
                    foreach (var item in ds.Items)
                    {
                        // добавляем только строки с ненулевыми остатками
                        if (item.CheckGet("QTY").ToInt() > 0)
                        {
                            _ds.Items.Add(item);
                        }
                    }
                }
            }

            return _ds;
        }

        /// <summary>
        /// Вызов окна изменения сырья
        /// </summary>
        public void Edit(Dictionary<string, string> values)
        {
            int rawId = values.CheckGet("IDC").ToInt();
            int profileId = values.CheckGet("PROFILE_ID").ToInt();

            if (rawId == 0 && profileId == 0)
            {
                var dw = new DialogWindow("Неверно заданы параметры", "Смена сырья");
                dw.ShowDialog();
            }
            else
            {
                Form.SetValues(values);
                LoadItems();
            }
        }

        /// <summary>
        /// Отображение окна
        /// </summary>
        public void Show()
        {
            string title = $"Смена сырья";

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
            Destroy();
        }

        /// <summary>
        /// Проверки перед сохранением
        /// </summary>
        public void Save()
        {
            var v = Form.GetValues();
            if (RawGrid.GridItems.Count > 0)
            {
                if (RawGrid.SelectedItem != null)
                {
                    var p = new Dictionary<string, string>()
                    {
                        { "ID", v.CheckGet("ID") },
                        { "RAW_ID", RawGrid.SelectedItem.CheckGet("ID") },
                    };
                    SaveData(p);
                }
                else
                {
                    Form.SetStatus("Выберите сырье", 1);
                }
            }
            else
            {
                Form.SetStatus("Нет доступного сырья", 1);
            }
        }

        /// <summary>
        /// Сохранение данных в БД
        /// </summary>
        /// <param name="p"></param>
        private async void SaveData(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleTask");
            q.Request.SetParam("Action", "UpdateRaw");
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
                    if (result.ContainsKey("ITEMS"))
                    {
                        //отправляем сообщение с данными и закрываем окно
                        Messenger.Default.Send(new ItemMessage()
                        {
                            ReceiverGroup = "PreproductionSample",
                            ReceiverName = ReceiverName,
                            SenderName = "SelectRaw",
                            Action = "Refresh",
                        });

                        Close();
                    }
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                Form.SetStatus(q.Answer.Error.Message, 1);
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
    }
}
