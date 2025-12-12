using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Форма редактирования краски для печати на литой таре
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class ColorOffset : ControlBase
    {
        public ColorOffset()
        {
            InitializeComponent();

            InitForm();
            SetDefaults();
        }

        /// <summary>
        /// Идентификатор редактируемой краски
        /// </summary>
        public int ColorId;
        /// <summary>
        /// Имя вкладки, которая вызвала открытие фрейма, и в которую возвращается фокус после закрытия фрейма
        /// </summary>
        public string ReceiverName { get; set; }
        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// начальный цвет фона текстового поля, для восстановления, когда цвет не может быть посчитан
        /// </summary>
        private Brush DefaultTextHexBrush;

        /// <summary>
        /// Функция перевода строки содержащей hex код цвета краски в цвет Brush
        /// <param name="hex_code">строка с hex числом</param>
        /// <return>Brush.цвет</return>
        /// </summary>
        private static Brush HexToBrush(string hex_code)
        {
            var hexString = (hex_code as string).Replace("#", "");

            var r = hexString.Substring(0, 2);
            var g = hexString.Substring(2, 2);
            var b = hexString.Substring(4, 2);

            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xff,
                byte.Parse(r, System.Globalization.NumberStyles.HexNumber),
                byte.Parse(g, System.Globalization.NumberStyles.HexNumber),
                byte.Parse(b, System.Globalization.NumberStyles.HexNumber)));
        }

        /// <summary>
        /// Обработка команд
        /// </summary>
        /// <param name="command"></param>
        private void ProcessCommand(string command)
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
        /// Инициализация формы
        /// </summary>
        private void InitForm()
        {
            Form = new FormHelper();
            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="PANTONE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control = Pantone,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="COLOR",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control = TextColor,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="HEX",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control = TextHex,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ARCHIVED_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control = CheckArchive,
                    ControlType = "CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="GUID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control = ColorGuid,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };
            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();
            ColorId = 0;
            DefaultTextHexBrush = TextHex.Background;
        }

        /// <summary>
        /// Получение данных для формы
        /// </summary>
        private async void GetData()
        {
            bool archiveAvailable = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PrintInk");
            q.Request.SetParam("Action", "Get");
            q.Request.SetParam("ID", ColorId.ToString());

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
                    var ds = ListDataSet.Create(result, "PRINT_INK");
                    if (ds.Items.Count > 0)
                    {
                        Form.SetValues(ds);
                        archiveAvailable = ds.Items[0].CheckGet("ARCHIVE_AVAILABLE").ToBool();
                    }
                }
            }
            else
            {
                Form.SetStatus("Не удалось загрузить данные", 1);
            }

            CheckArchive.IsEnabled = archiveAvailable;
            Show();
        }

        /// <summary>
        /// Вызов формы редактирования краски литой тары
        /// </summary>
        /// <param name="id"></param>
        public void Edit(int id = 0)
        {
            ColorId = id;
            ControlName = $"ColorOffset_{ColorId}";
            if (ColorId > 0)
            {
                GetData();
            }
            else
            {
                CheckArchive.IsEnabled = false;
                Show();
            }
        }

        /// <summary>
        /// Отображение формы редактирования оффсетной краски
        /// </summary>
        public void Show()
        {
            string title = $"Новая оффсетная краска";

            if (!Pantone.Text.IsNullOrEmpty())
            {
                title = $"Оффсетная краска {Pantone.Text}";
            }

            Central.WM.Show(ControlName, title, true, "add", this);
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        public void Close()
        {
            Central.WM.Close(ControlName);
            if (!ReceiverName.IsNullOrEmpty())
            {
                Central.WM.SetActive(ReceiverName);
                ReceiverName = "";
            }
        }

        /// <summary>
        /// Проверки перед сохранением
        /// </summary>
        public void Save()
        {
            if (Form.Validate())
            {
                var fld = Form.GetValues();

                fld.Add("ID", ColorId.ToString());

                SaveData(fld);
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
            q.Request.SetParam("Object", "PrintInk");
            q.Request.SetParam("Action", "Save");
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
                    if (result.ContainsKey("ITEM"))
                    {
                        //отправляем сообщение о необходимости обновить данные
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "PreproductionContainer",
                            ReceiverName = ReceiverName,
                            SenderName = ControlName,
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

        /// <summary>
        /// Обработка изменения кода цвета
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextHex_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextHex.Background = DefaultTextHexBrush;

            if (!string.IsNullOrEmpty(TextHex.Text))
            {
                string hex = TextHex.Text.TrimEnd(' ').TrimStart(' ');

                if (hex.Length == 6)
                {
                    if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out _))
                    {
                        TextHex.Background = HexToBrush(hex);
                    }
                }
            }
        }
    }
}
