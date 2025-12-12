using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// добавление записи в
    /// </summary>
    public partial class SetWeight : UserControl
    {
        public SetWeight()
        {
            FrameName = "SetWeight";

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();
            SetDefaults();
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// идентификатор из справочника источника отходов
        /// </summary>
        public int InwsId { get; set; }

        /// <summary>
        /// регулярное выражение для проверки ввода
        /// </summary>
        public static Regex onlyNumbers = new Regex("[^0-9,]+");

        /// <summary>
        /// источник
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        // инициализация компонентов
        /// </summary>
        public void Init()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="GROSS_WEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=GrossWeight,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="TARE_WEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TareWeight,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
            };

            Form.SetFields(fields);
            Form.StatusControl = FormStatus;


            //после установки значений
            Form.AfterSet = (Dictionary<string, string> v) =>
            {
                //фокус на поле ввода логина
                TareWeight.Focus();
                TareWeight.SelectAll();
            };
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();
        }

        /// <summary>
        /// Закрытие окна
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Production",
                ReceiverName = "",
                SenderName = "SetWeight",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
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
        /// создание записи
        /// </summary>
        /// <param name="inws_id"></param>
        /// <param name="source"></param>
        /// <param name="gross"></param>
        public void Set(int inws_id, string source, double gross)
        {
            InwsId = inws_id;
            Source = source;
            GrossWeight.Text = gross.ToString();
            TareWeight.Text = "0";
            GetData();
        }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            Central.WM.Show($"SetWeight", $"Источник {Source}", true, "add", this);
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.Close($"SetWeight");

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
        }

        /// <summary>
        /// получение данных с сервера
        /// </summary>
        public void GetData()
        {
            Show();
        }

        /// <summary>
        /// подготовка данных
        /// </summary>
        public void Save()
        {
            bool resume = true;
            string error = "";

            //стандартная валидация данных средствами формы
            if (resume)
            {
                var validationResult = Form.Validate();

                if (GrossWeight.Text.ToDouble() > (double)1000)
                {
                    var msg = "Вес брутто должен быть меньше тонны.";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();
                    resume = false;
                }

                if (GrossWeight.Text.ToDouble() < TareWeight.Text.ToDouble())
                {
                    var msg = "Вес брутто должен быть больше веса тары.";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();
                    resume = false;
                }

                if (!validationResult)
                {
                    resume = false;
                }
            }

            var v = Form.GetValues();

            //отправка данных
            if (resume)
            {
                // добавление параметров для сохраниения веса
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("INWS_ID", InwsId.ToString());
                }

                v.AddRange(p);

                SaveData(v);
            }
            else
            {
                Form.SetStatus(error, 1);
            }
        }

        /// <summary>
        /// отпаравка данных на сервер
        /// </summary>
        /// <param name="p"></param>
        public async void SaveData(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "IndustrialWaste");
            q.Request.SetParam("Action", "SetIndustrialWaste");

            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                //отправляем сообщение гриду Press о необходимости обновить данные
                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = "Production",
                    ReceiverName = "UnitWeight",
                    SenderName = "SetWeight",
                    Action = "Refresh",
                });
                Close();
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// обработчик нажатия на кнопку отмена
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// обработчик нажатия на кнопку сохранить
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Save();
        }

        /// <summary>
        /// проверка ввода
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = onlyNumbers.IsMatch(e.Text);
        }
    }
}
