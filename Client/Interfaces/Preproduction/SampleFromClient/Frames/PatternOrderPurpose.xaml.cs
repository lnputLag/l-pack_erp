using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Форма редактирования данных анализа образца от клиента
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class PatternOrderPurpose : ControlBase
    {
        public PatternOrderPurpose()
        {
            InitializeComponent();
            InitForm();
            SetDefaults();
        }

        /// <summary>
        /// ID образца от клиента
        /// </summary>
        int PatternId;
        /// <summary>
        /// Датасет профилей картона
        /// </summary>
        ListDataSet ProfilesDS { get; set; }
        /// <summary>
        /// Датасет марок картона
        /// </summary>
        ListDataSet SourceMarksDS { get; set; }
        /// <summary>
        /// Форма редактирования данных анализа образца от клиента
        /// </summary>
        FormHelper Form { get; set; }
        /// <summary>
        /// Имя вкладки, которая становится активной после закрытия фрейма
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// Дополнительный обработчик нажатий на клавиши
        /// </summary>
        private void ProcessKeyboard2()
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
        /// Обработка и выполнение команд
        /// </summary>
        /// <param name="command"></param>
        public void ProcessCommand(string command)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "close":
                        Close();
                        break;
                    case "save":
                        Save();
                        break;
                }
            }
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
                    Path="ID_PROF",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProfileName,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CORRUGATED_COEFFICIENT",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=CorrugatedCoefficient,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CORRUGATED_COEFFICIENT_2",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=CorrugatedCoefficient2,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="EDGE_CRASH_TEST",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=EdgeCrashTest,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ID_MARKA",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PurposeMarka,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ID_OUTER",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ColorOuter,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="LENGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PatternLength,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="WIDTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PatternWidth,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="HEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PatternHeight,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="BLANK_LENGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=BlankLength,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="BLANK_WIDTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=BlankWidth,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="BURSTING_STRENGTH_TEST",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=BurstingStrengthTest,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DELAMINATION_RESISTANCE",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=DelaminationResistance,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="RAW_GROUP_1",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=RawGroup1,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="RAW_GROUP_2",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=RawGroup2,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="RAW_GROUP_3",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=RawGroup3,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="RAW_GROUP_4",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=RawGroup4,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="RAW_GROUP_5",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=RawGroup5,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PARA_ID_1",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PaperRaw1,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PARA_ID_2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PaperRaw2,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PARA_ID_3",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PaperRaw3,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PARA_ID_4",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PaperRaw4,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PARA_ID_5",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PaperRaw5,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PACL_ID_1",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ColorRaw1,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PACL_ID_2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ColorRaw2,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PACL_ID_3",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ColorRaw3,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PACL_ID_4",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ColorRaw4,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PACL_ID_5",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ColorRaw5,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ID_CLR_1",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Color1,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ID_CLR_2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Color2,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ID_CLR_3",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Color3,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ID_CLR_4",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Color4,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ID_CLR_5",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Color5,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="WEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=PatternWeight,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="THICKNESS",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=Thickness,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="COMMENTS",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Comment,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

        /// <summary>
        /// Заполнение значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            PatternId = 0;

            Form.SetDefaults();
        }

        /// <summary>
        /// Основной метод вызова формы редактирования
        /// </summary>
        /// <param name="Id"></param>
        public void Edit(int Id)
        {
            PatternId = Id;

            ControlName = $"PatternPurpose_{PatternId}";
            GetData();
            Show();
        }

        /// <summary>
        /// Получение данных из БД
        /// </summary>
        private async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PatternOrder");
            q.Request.SetParam("Action", "GetPurpose");
            q.Request.SetParam("ID", PatternId.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                if (result != null)
                {
                    // содержимое выпадающего списка профиль картона
                    ProfilesDS = ListDataSet.Create(result, "Profiles");
                    ProfileName.Items = ProfilesDS.GetItemsList("ID", "NAME");

                    // содержимое выпадающего списка марка картона
                    SourceMarksDS = ListDataSet.Create(result, "SrcMarks");
                    PurposeMarka.Items = SourceMarksDS.GetItemsList("ID", "NAME");

                    // содержимое выпадающего списка цвет картона
                    var colorOuterDS = ListDataSet.Create(result, "ColorOuter");
                    ColorOuter.Items = colorOuterDS.GetItemsList("ID", "OUTER_NAME");

                    // содержимое выпадающих списков типа сырья каждого слоя
                    var paperRawsDS = ListDataSet.Create(result, "ParerRaws");
                    var paperRawItems = paperRawsDS.GetItemsList("ID", "SHORT_NAME");
                    PaperRaw1.Items = paperRawItems;
                    PaperRaw2.Items = paperRawItems;
                    PaperRaw3.Items = paperRawItems;
                    PaperRaw4.Items = paperRawItems;
                    PaperRaw5.Items = paperRawItems;

                    // содержимое выпадающих списков цвета сырья каждого слоя
                    var parerColorsDS = ListDataSet.Create(result, "ParerColors");
                    var paperColorItems = parerColorsDS.GetItemsList("ID", "COLOR");
                    ColorRaw1.Items = paperColorItems;
                    ColorRaw2.Items = paperColorItems;
                    ColorRaw3.Items = paperColorItems;
                    ColorRaw4.Items = paperColorItems;
                    ColorRaw5.Items = paperColorItems;

                    // содержимое выпадающих списков использующихся цветов
                    var colorsDS = ListDataSet.Create(result, "Colors");
                    var colorItems = colorsDS.GetItemsList("ID", "NAME");
                    Color1.Items = colorItems;
                    Color2.Items = colorItems;
                    Color3.Items = colorItems;
                    Color4.Items = colorItems;
                    Color5.Items = colorItems;

                    // данные по образцу клиента
                    var orderPurposeDS = ListDataSet.Create(result, "OrderPurpose");
                    Form.SetValues(orderPurposeDS);

                    FilterMarksItems(ProfileName.SelectedItem.Key.ToInt());
                }
            }
        }

        /// <summary>
        /// Фильтр содержимого выпадающего списка марок картона в зависимости от выбранного профиля картона
        /// </summary>
        /// <param name="selectedItem">выбранная строка</param>
        /// <returns></returns>
        private bool FilterMarksItems(int profileId)
        {
            bool result = false;
            if (SourceMarksDS.Items.Count > 0)
            {
                int qtyLayers = 0;
                foreach (var profileItem in ProfilesDS.Items)
                {
                    if (profileItem.ContainsKey("ID"))
                    {
                        if (profileItem["ID"].ToInt() == profileId)
                        {
                            if (profileItem.ContainsKey("QTY_LAYERS"))
                            {
                                qtyLayers = profileItem["QTY_LAYERS"].ToInt();
                                result = true;
                            }
                            break;
                        }
                    }
                }

                CorrugatedCoefficient2.IsEnabled = qtyLayers == 5;

                // если нашли значения количества слоёв у выбранного профиля,
                // в выпадающем списке марок оставим только те, у которых столько же слоёв
                if (result)
                {
                    var markItems = new Dictionary<string, string>();
                    foreach (var item in SourceMarksDS.Items)
                    {
                        if (item.ContainsKey("QTY_LAYERS"))
                        {
                            if (item["QTY_LAYERS"].ToInt() == qtyLayers)
                            {
                                markItems.Add(item["ID"], item["NAME"]);
                            }
                        }
                    }
                    PurposeMarka.Items = markItems;
                }
            }

            return result;
        }

        /// <summary>
        /// Создание вкладки с формой редактирования
        /// </summary>
        private void Show()
        {
            string title = $"Образец от клиента {PatternId}";
            Central.WM.Show(ControlName, title, true, "add", this);
        }

        /// <summary>
        /// Сохранение данных с формы редактирования
        /// </summary>
        private async void Save()
        {
            var p = Form.GetValues();
            p.Add("ID", PatternId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PatternOrder");
            q.Request.SetParam("Action", "SavePurpose");
            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(q.Answer.Data);
                if (result != null)
                {
                    //отправляем сообщение Гриду о необходимости обновить данные
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverGroup = "Preproduction",
                        ReceiverName = ReceiverName,
                        SenderName = "PatternPurpose",
                        Action = "Refresh",
                    });
                }
            }
            else
            {
                q.ProcessError();
            }

            Close();
        }

        /// <summary>
        /// Закрытие вкладки с формой редактирования
        /// </summary>
        private void Close()
        {
            //Central.WM.RemoveTab(ControlName);
            Central.WM.Close(ControlName);
            Central.WM.SetActive(ReceiverName);
            ReceiverName = "";
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

        private void ProfileName_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (ProfileName.SelectedItem.Key != null)
            {
                FilterMarksItems(ProfileName.SelectedItem.Key.ToInt());
            }
        }
    }
}
