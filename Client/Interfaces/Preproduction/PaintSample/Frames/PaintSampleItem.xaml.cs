using Client.Common;
using Client.Interfaces.Main;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Форма редактирования свойств выкраса
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class PaintSampleItem : ControlBase
    {
        public PaintSampleItem()
        {
            InitializeComponent();

            SetDefaults();
            InitForm();
        }

        /// <summary>
        /// Имя вкладки, откуда вызвана форма и куда передается ответ
        /// </summary>
        public string ReceiverName;
        /// <summary>
        /// Справочник цветов
        /// </summary>
        public ListDataSet ColorRefDS {  get; set; }
        /// <summary>
        /// Форма редактирования заявки на выкрасы
        /// </summary>
        FormHelper Form { get; set; }
        /// <summary>
        /// Тип запроса на выкрасы
        /// </summary>
        public int DemandType;

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            // По умолчанию заявка на действующие выкрасы
            DemandType = 0;
            var rawColorDict = new Dictionary<string, string>()
            {
                { "1", "Белый" },
                { "2", "Бурый" },
                { "3", "Крашенный" },
            };
            RawColor.Items = rawColorDict;

            var rawTypeDict = new Dictionary<string, string>()
            {
                { "0", "Макулатура" },
                { "1", "Целлюлоза" },
            };
            RawType.Items = rawTypeDict;

            ColorRefDS = new ListDataSet();
            ColorRefDS.Init();

            //Чекбокс учета цвета сырья доступен только по спецправам
            var role = Central.Navigator.GetRoleLevel("[erp]paint_sample");
            AnyRawColorCheckBox.IsEnabled = role == Role.AccessMode.Special;
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
                    Path="ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PAINT_ORDER",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="RAW_COLOR_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=RawColor,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="RAW_TYPE_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=RawType,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="COLOR_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PaintName,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PAINT_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Pantone,
                    ControlType="TextBox",
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
        /// Фильтрация содержимого выпадающего списка красок
        /// </summary>
        /// <param name="rawColor"></param>
        /// <returns></returns>
        private Dictionary<string, string> GetColorList(int rawColor)
        {
            // Если отмечен чекбокс, то не фильтруем
            bool anyRawColor = (bool)AnyRawColorCheckBox.IsChecked;
            var items = new Dictionary<string, string>();

            foreach (var item in ColorRefDS.Items)
            {
                bool include = anyRawColor;

                if (!anyRawColor)
                {
                    int cardboardType = item.CheckGet("CARDBOARD_TYPE").ToInt();
                    if ((cardboardType == 0) || (cardboardType == rawColor))
                    {
                        include = true;
                    }
                }

                if (include)
                {
                    items.Add(item["ID"], item["NAME"]);
                }
            }

            return items;
        }

        /// <summary>
        /// Вызов формы редактирования выкраса
        /// </summary>
        /// <param name="data"></param>
        public void Edit(Dictionary<string, string> data)
        {
            DemandType = data.CheckGet("DEMAND_TYPE").ToInt();
            if (DemandType == 1)
            {
                PaintLabel.Visibility = Visibility.Collapsed;
                PaintTextBlock.Visibility = Visibility.Collapsed;
            }
            else
            {
                PantoneLabel.Visibility = Visibility.Collapsed;
                PantoneTextBlock.Visibility = Visibility.Collapsed;
            }
            
            int psiId = data.CheckGet("ID").ToInt();
            var rawColor = data.CheckGet("RAW_COLOR").ToInt();
                
            PaintName.Items = GetColorList(rawColor);

            Form.SetValues(data);

            ControlName = $"PaintSampleItem_{psiId}";
            ControlTitle = $"Выкрас {psiId}";
            if (psiId == 0)
            {
                ControlTitle = "Новый выкрас";
            }

            var jj = Form.GetValues();


            Show();
        }

        /// <summary>
        /// Настройка содержимого полей и списков
        /// </summary>
        private void FilterFields()
        {
            int rawColor = RawColor.SelectedItem.Key.ToInt();
            
            switch (rawColor)
            {
                case 1:
                    RawType.SetSelectedItemByKey("1");
                    RawType.IsReadOnly = true;
                    PaintName.Items = GetColorList(1);
                    break;
                case 2:
                    PaintName.Items = GetColorList(2);
                    RawType.IsReadOnly = false;
                    break;
                case 3:
                    RawType.SetSelectedItemByKey("0");
                    RawType.IsReadOnly = true;
                    PaintName.Items = GetColorList(2);
                    break;
            }
        }

        /// <summary>
        /// Отображение вкладки для редактирования выкраса
        /// </summary>
        public void Show()
        {
            Central.WM.Show(ControlName, ControlTitle, true, "add", this);
        }

        /// <summary>
        /// Сохранение данных выкраса в заявке
        /// </summary>
        public void Save()
        {
            bool resume = true;

            if (Form.Validate())
            {
                var values = Form.GetValues();

                // Проверяем заполненность полей
                if (resume)
                {
                    switch (DemandType)
                    {
                        case 0:
                        case 2:
                            string paintId = values.CheckGet("COLOR_ID");
                            if (paintId.IsNullOrEmpty())
                            {
                                resume = false;
                                Form.SetStatus("Необходимо выбрать цвет выкраса", 1);
                            }
                            break;
                        case 1:
                            if (Pantone.Text.IsNullOrEmpty())
                            {
                                resume = false;
                                Form.SetStatus("Необходимо указать пантон выкраса", 1);
                            }
                            break;
                    }
                }

                if (resume)
                {
                    values.Add("RAW_COLOR", RawColor.SelectedItem.Value);
                    values.Add("RAW_TYPE", RawType.SelectedItem.Value);
                    string paintName = "";
                    if (DemandType == 1)
                    {
                        if (Pantone.Text.ToLower().StartsWith("pantone"))
                        {
                            paintName = Pantone.Text;
                        }
                        else
                        {
                            paintName = $"Pantone {Pantone.Text}";
                        }
                    }
                    else
                    {
                        paintName = PaintName.SelectedItem.Value;
                    }
                    values.CheckAdd("PAINT_NAME", paintName);

                    // Отправляем сообщение с данными формы обратно
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverGroup = "Preproduction",
                        ReceiverName = ReceiverName,
                        SenderName = ControlName,
                        Action = "SavePaintSample",
                        ContextObject = values,
                    });

                    Close();
                }
            }
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        public void Close()
        {
            Central.WM.RemoveTab(ControlName);
            Central.WM.SetActive(ReceiverName);
            ReceiverName = "";
        }

        /// <summary>
        /// Обработка нажатия на кнопку
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
        /// Обработка выбора цвета картона, на который наносится краска
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void RawColor_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FilterFields();
        }

        /// <summary>
        /// Обработка флажка фильтрации красок без учета цвета картона
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnyRawColorCheckBox_Click(object sender, RoutedEventArgs e)
        {
            FilterFields();
        }
    }
}
