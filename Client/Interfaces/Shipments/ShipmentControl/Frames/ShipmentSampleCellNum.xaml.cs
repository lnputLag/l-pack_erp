using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Окно ввода номера ячейки хранения образцов на СГП
    /// </summary>
    public partial class ShipmentSampleCellNum : UserControl
    {
        public ShipmentSampleCellNum()
        {
            InitializeComponent();

            InitForm();
            SetDefaults();
        }

        /// <summary>
        /// ID образца
        /// </summary>
        int IdSample { get; set; }

        /// <summary>
        /// Форма редактирования поддонов
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Структура окна
        /// </summary>
        private Window Window { get; set; }

        /// <summary>
        /// Имя вкладки, окуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// Деструктор компонентов. Завершает вспомогательные процессы
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "ShipmentControl",
                ReceiverName = "ShipmentSamples",
                SenderName = "ShipmentSamplesCellNum",
                Action = "Closed",
            });
        }

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
                    Path="QTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CellNum,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.IsNotZero, null },
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
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            FormStatus.Text = "";
            CellNum.Text = "";
            IdSample = 0;

            ReceiverName = "ShipmentSamples";
        }

        /// <summary>
        /// Запуск редактирования номера ячейки
        /// </summary>
        public void Edit(int idSample = 0)
        {
            if (idSample > 0)
            {
                IdSample = idSample;
                GetData();
            }
        }

        /// <summary>
        /// Получение данных из БД для формы редактирования
        /// </summary>
        private async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "GetRecord");

            q.Request.SetParam("ID", IdSample.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var SampleDS = ListDataSet.Create(result, "SampleRec");
                    var cellNumValue = 0;
                    if (SampleDS.Items.Count > 0)
                    {
                        var record = SampleDS.Items.First();
                        cellNumValue = record.CheckGet("CELL_NUM").ToInt();
                    }

                    if (cellNumValue > 0)
                    {
                        CellNum.Text = cellNumValue.ToString();
                    }

                    Show();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Показ окна
        /// </summary>
        public void Show()
        {
            int w = (int)Width;
            int h = (int)Height;
            string title = $"Изменение ячейки";

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
            CellNum.Focus();

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
        /// Сохранение
        /// </summary>
        private void Save()
        {
            if (Form.Validate())
            {
                SaveData();
            }
        }

        /// <summary>
        /// Запись данных в БД
        /// </summary>
        private async void SaveData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "SaveCellNum");

            q.Request.SetParam("ID", IdSample.ToString());
            q.Request.SetParam("CELL_NUM", CellNum.Text);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    //если ответ не пустой, отправляем сообщение Гриду о необходимости обновить данные
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "ShipmentControl",
                        ReceiverName = "ShipmentSamples",
                        SenderName = "ShipmentSamplesCellNum",
                        Action = "Refresh",
                    });
                    Close();
                }
            }
            else
            {
                q.ProcessError();
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
