using Client.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Нвстройка параметров отчета по выполненным образцам
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleCompletedReportSettings : UserControl
    {
        public SampleCompletedReportSettings()
        {
            InitializeComponent();

            InitForm();
            SetDefaults();
        }

        /// <summary>
        /// Форма редактирования поддонов
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Структура окна
        /// </summary>
        private Window Window { get; set; }

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
                    Path="DT_FROM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=FromDate,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="SHIFT_FROM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ShiftFrom,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="DT_TO",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ToDate,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="SHIFT_TO",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ShiftTo,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
            };
            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();

            var hourDict = new Dictionary<string, string>()
            {
                { "8", "8" },
                { "20", "20" },
            };
            ShiftFrom.SetItems(hourDict);
            ShiftTo.SetItems(hourDict);

            var morning = DateTime.Now.Date.AddHours(8);
            var evening = DateTime.Now.Date.AddHours(20);
            FromDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
            ToDate.Text = DateTime.Now.ToString("dd.MM.yyyy");

            if (DateTime.Compare(DateTime.Now, morning) < 0)
            {
                FromDate.Text = DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy");
                ToDate.Text = DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy");
                ShiftFrom.SetSelectedItemByKey("8");
                ShiftTo.SetSelectedItemByKey("20");
            }
            else if (DateTime.Compare(DateTime.Now, evening) < 0)
            {
                FromDate.Text = DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy");
                ShiftFrom.SetSelectedItemByKey("20");
                ShiftTo.SetSelectedItemByKey("8");
            }
            else
            {
                ShiftFrom.SetSelectedItemByKey("8");
                ShiftTo.SetSelectedItemByKey("20");
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
        }

        /// <summary>
        /// Проверки перед сохранением
        /// </summary>
        private void Save()
        {
            var p = Form.GetValues();

            int startHour = p["SHIFT_FROM"].ToInt();
            DateTime dttmFrom = p["DT_FROM"].ToDateTime().AddHours(startHour);
            int finnishHour = p["SHIFT_TO"].ToInt();
            DateTime dttmTo = p["DT_TO"].ToDateTime().AddHours(finnishHour);

            if (DateTime.Compare(dttmFrom, dttmTo) >= 0)
            {
                Form.SetStatus("Начало отчета должно быть раньше", 1);
            }
            else if (DateTime.Compare(dttmTo, DateTime.Now) > 0)
            {
                Form.SetStatus("Выбранная смена еще не закончилась", 1);
            }
            else
            {
                GetReport(p);
            }
        }

        /// <summary>
        /// Получение файла отчета
        /// </summary>
        /// <param name="p">Данные формы</param>
        private async void GetReport(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "CompleteReport");
            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Close();
                Central.OpenFile(q.Answer.DownloadFilePath);
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
