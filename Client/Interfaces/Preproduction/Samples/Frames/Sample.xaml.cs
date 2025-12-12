using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Shipments;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Preproduction
{
    //FIXME: замечания по дизайну формы

    /// <summary>
    /// Редактирование образца
    /// </summary>
    /// <author>Рясной П.В.</author>
    public partial class Sample : UserControl
    {
        public Sample()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitForm();
            SetDefaults();
        }

        /// <summary>
        /// ID образца
        /// </summary>
        int SampleId;

        /// <summary>
        /// Статус редактируемого образца
        /// </summary>
        int Status;

        /// <summary>
        /// Признак, что образец получен из ЛК клиента. Информация о клиенте недоступна для редактирования
        /// </summary>
        bool FromWeb;

        /// <summary>
        /// Список профилей картона, будем использовать при фильтрации типов картона
        /// </summary>
        ListDataSet ProfilesDS { get; set; }

        /// <summary>
        /// Список марок картона, будем использовать при фильтрации типов картона
        /// </summary>
        ListDataSet CartonMarksDS { get; set; }

        /// <summary>
        /// список картона
        /// </summary>
        ListDataSet CardboardDS { get; set; }

        /// <summary>
        /// Форма редактирования образца
        /// </summary>
        FormHelper SampleForm { get; set; }

        /// <summary>
        /// Имя вкладки, куда происходит возврат при закрытии этой вкладки
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// Имя этой вкладки
        /// </summary>
        public string TabName;

        /// <summary>
        /// Режим работы технолога. Для остальных есть ограничения
        /// </summary>
        private bool TechnologMode;
        /// <summary>
        /// Режим редактирования образца, зависит от роли пользователя. 1 - технолог, 2 - конструктор, 3 - менеджер, 4 - лаборатория, по умолчанию 3
        /// </summary>
        private int EditMode;

        /// <summary>
        /// Признак, что образец принимается в работу
        /// </summary>
        public int Confirmation;

        /// <summary>
        /// При редактировании старые значения картона и сырья. Если они меняются и образец уже в очереди заданий, надо удалить его из очереди
        /// </summary>
        private int OldCardboardId;
        private int OldRawId;

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            TabName = "";
            Status = 0;
            Confirmation = 0;
            FromWeb = false;
            OldCardboardId = 0;
            OldRawId = 0;

            SampleForm.SetDefaults();
            // типы доставки. по умолчанию - из липецкого офиса
            DeliveryType.Items = DeliveryTypes.Items;
            DeliveryType.SelectedItem = DeliveryTypes.Items.GetEntry("1");
            // необходимость в чертеже. по умолчанию - не нужен
            NeedDesign.Items = SampleDesignTypes.Items;
            NeedDesign.SelectedItem = SampleDesignTypes.Items.GetEntry("0");
            // типы упаковки образца
            PackingType.Items = PackingTypes.Items;
            PackingType.SelectedItem = PackingTypes.Items.GetEntry("1");


            var gluingItems = new Dictionary<string, string>() {
                { "0", " " },
                { "1", "склеить" },
                { "2", "не клеить" },
                { "3", "сшить" },
                { "4", "склеить и сшить" },
            };
            GluingType.Items = gluingItems;
            GluingType.SetSelectedItemByKey("0");

            CreatedDate.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            EditQtyLimit.Text = "5";
            PlanCompletedQty.Text = "";
            // По умолчанию скрываем марку картона
            HideMarkCheckBox.IsChecked = true;

            // Находим режим редактирования образца по роли пользователя
            EditMode = 3;

            if (Central.User.Roles.ContainsKey("[erp]sample"))
            {
                EditMode = 1;
            }
            else if (Central.User.Roles.ContainsKey("[erp]sample_drawing"))
            {
                EditMode = 2;
                ResponsibleLabel.Content = "Ответственный:  ";
            }
            else if (Central.User.Roles.ContainsKey("[erp]sample_laboratory"))
            {
                EditMode = 4;
                ResponsibleLabel.Content = "Ответственный:  ";
            }
        }

        /// <summary>
        /// Остановка вспомогательных процессов при закрытии вкладки
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "PreproductionSample",
                ReceiverName = "",
                SenderName = "Sample",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            // Делаем активной вкладку, откуда было вызвано редактирование
            if (!ReceiverName.IsNullOrEmpty())
            {
                Central.WM.SetActive(ReceiverName, true);
                ReceiverName = "";
            }

        }

        /// <summary>
        /// Обработка сообщений
        /// </summary>
        /// <param name="obj"></param>
        private void ProcessMessages(ItemMessage obj)
        {
            if (obj.ReceiverGroup.IndexOf("PreproductionSample") > -1)
            {
                if (obj.ReceiverName.IndexOf(TabName) > -1)
                {
                    if (obj.Action == "CardboardSelected")
                    {
                        if (obj.ContextObject != null)
                        {
                            var v = (Dictionary<string, string>)obj.ContextObject;
                            SetSampleCardboard(v);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Инициализация формы
        /// </summary>
        private void InitForm()
        {
            SampleForm = new FormHelper();
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
                    Path="CUSTOMER_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CustomerName,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="INNER_ORDER",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=InnerOrderCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DT_CREATED",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CreatedDate,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Format="dd.MM.yyyy HH:mm",
                },
                new FormHelperField()
                {
                    Path="NUM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Num,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_CLASS_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductClass,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="FEFCO",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Fefco,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="L",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=SLength,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="B",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=SWidth,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="H",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=SHeight,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PROFILE_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Profile,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="MARK_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Mark,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="COLOR_OUTER_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ColorOuter,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="IDC",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Cardboard,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="BLANK_IDC",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=SampleCardboardId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="BLANK_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=SampleCardboard,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ANY_CARTON_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=AnyMarkCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TECHNOLOG_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Technolog,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Note,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=EditQty,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="LIMIT_QTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=EditQtyLimit,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CONFIRMATION_REASON",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ConfirmationReason,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DT_COMPLITED",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CompletedDate,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="RAW_MISSING_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=RawMissingCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="REVISION_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=RevisionCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DESIGN",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=NeedDesign,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DELIVERY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=DeliveryType,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="GLUING",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=GluingType,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="MANAGER_EMPL_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ManagerName,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=EditNote,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PACKING_TYPE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PackingType,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="HIDE_MARK",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=HideMarkCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="HIDE_NOTE",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=HideNoteCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };
            SampleForm.SetFields(fields);
            SampleForm.StatusControl = FormStatus;
        }

        /// <summary>
        /// Запуск редактирования образца
        /// </summary>
        /// <param name="id">ИД образца</param>
        public void Edit(int id)
        {
            SampleId = id;
            // для менеджеров прячем флаг внутреннего заказа
            if (EditMode == 3)
            {
                InnerOrderCheckBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                InnerOrderCheckBox.Visibility = Visibility.Visible;
            }

            var mode = Central.Navigator.GetRoleLevel("[erp]sample");
            TechnologMode = (mode == Role.AccessMode.FullAccess || mode == Role.AccessMode.Special);
            
            GetData();
        }

        /// <summary>
        /// Блокируем для изменений поля формы
        /// </summary>
        private void SetReadonly()
        {
            // Для менеджеров при создании образца спрячем поля Картон для образцов и Дата выполнения
            if (!TechnologMode && (SampleId == 0))
            {
                RawMissingBlock.Visibility = Visibility.Collapsed;
                CompleteDateLabel.Visibility = Visibility.Collapsed;
                CompleteDateBlock.Visibility = Visibility.Collapsed;
                PlanCompleteBlock.Visibility = Visibility.Collapsed;
            }

            if (!TechnologMode && (SampleId > 0))
            {
                RawMissingCheckBox.IsEnabled = false;
                CompletedDate.IsEnabled = false;
            }

            // Образец создан в ЛК клиента
            if (FromWeb)
            {
                CustomerName.IsReadOnly = true;
                Num.IsReadOnly = true;
            }

            // Образец в статусе Новый или Приемка может редактироваться
            bool acceptance = Status == SampleStates.Acceptance || Status == SampleStates.New;

            // Все поля блока, кроме статуса В работе, сделаны только для чтения
            // Кроме того, у кого нет прав на редактирование образца, не могут редактировать параметры образца
            if (!acceptance && ((Status != SampleStates.InWork) || (EditMode != 1)))
            {
                foreach (var f in SampleRawBlock.Children.OfType<Border>())
                {
                    var a = f.Child.DependencyObjectType.Name;
                    if (a == "SelectBox")
                    {
                        SelectBox s = f.Child as SelectBox;
                        s.IsReadOnly = true;
                    }
                    else if (a == "CheckBox")
                    {
                        CheckBox s = f.Child as CheckBox;
                        s.IsEnabled = false;
                    }
                    else if (a == "TextBox")
                    {
                        TextBox s = f.Child as TextBox;
                        s.IsReadOnly = true;
                    }
                }

                CompletedDate.IsEnabled = false;
                SLength.IsReadOnly = true;
                SWidth.IsReadOnly = true;
                SHeight.IsReadOnly = true;
                EditQty.IsReadOnly = true;
                Profile.IsReadOnly = true;
                Mark.IsReadOnly = true;
                ColorOuter.IsReadOnly = true;
            }

            // Отклоненные, отгруженные и утилизированные образцы нельзя изменять
            if ((Status == SampleStates.Rejected) || (Status == SampleStates.Shipped) || (Status == SampleStates.Utilized))
            {
                SampleCardboardButton.IsEnabled = false;
                SaveButton.IsEnabled = false;
                DeliveryType.IsReadOnly = true;
                ManagerName.IsReadOnly = true;
                EditNote.IsReadOnly = true;
            }
        }

        /// <summary>
        /// Получение данных и справочников для образца
        /// </summary>
        private async void GetData()
        {
            string workGroupId = "0";
            // Для конструкторов и лаборантов отправляем код рабочей группы
            if (EditMode == 2)
            {
                workGroupId = "23";
            }
            else if (EditMode == 4)
            {
                workGroupId = "15";
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "Get");
            q.Request.SetParam("ID", SampleId.ToString());
            q.Request.SetParam("WORKGROUP_ID", workGroupId);

            await Task.Run(() =>
            {
                q.DoQuery();
            }
            );

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    // профиль картона
                    ProfilesDS = ListDataSet.Create(result,"Profiles");
                    Profile.Items = ProfilesDS.GetItemsList("ID", "NAME");

                    // марка картона
                    CartonMarksDS = ListDataSet.Create(result, "SrcMark");
                    // Спиоск заполняется при смене профиля

                    // цвет картона
                    var ColorOuterDS = ListDataSet.Create(result, "ColorOuter");
                    ColorOuter.Items = ColorOuterDS.GetItemsList("ID", "OUTER_NAME");

                    // Покупатель
                    var CustomerDS = ListDataSet.Create(result, "Customers");
                    CustomerName.Items = CustomerDS.GetItemsList("ID", "NAME");

                    // вид изделия
                    var ProdClassDS = ListDataSet.Create(result, "ProductClasses");
                    ProductClass.Items = ProdClassDS.GetItemsList("ID", "NAME");

                    // картон и картон для образцов
                    CardboardDS = ListDataSet.Create(result, "Cardboards");

                    // FEFCO
                    var fefcoDS = ListDataSet.Create(result, "FEFCO");
                    Fefco.Items = fefcoDS.GetItemsList("ID", "NAME");

                    // менеджер
                    string managerKey = "MANAGERS";
                    if (result.ContainsKey("USER_GROUP"))
                    {
                        managerKey = "USER_GROUP";
                    }

                    var managersDS = ListDataSet.Create(result, managerKey);
                    var list = new Dictionary<string, string>();

                    foreach (var item in managersDS.Items)
                    {
                        list.CheckAdd(item["ID"].ToInt().ToString(), item["FIO"]);
                    }

                    ManagerName.Items = list;

                    var values = new Dictionary<string, string>();
                    if (SampleId > 0)
                    {
                        var ds = ListDataSet.Create(result, "SampleRec");
                        if (ds.Items.Count > 0)
                        {
                            values = ds.Items.First();
                        }
                        // Поле Внутренний заказчик доступно только при создании образца
                        InnerOrderCheckBox.IsEnabled = false;

                        //При просмотре образца конструкторами заблокируем кнопку сохранения
                        if ((EditMode == 2) && (values.CheckGet("INNER_ORDER").ToInt() == 0))
                        {
                            SaveButton.IsEnabled = false;
                        }
                    }
                    else
                    {
                        // Если образец создаёт менеджер, очистим дату изготовления
                        if (EditMode == 3)
                        {
                            CompletedDate.Text = "";
                        }
                        // Передзаполненные поля для конструктора
                        else if (EditMode == 2)
                        {
                            values.CheckAdd("CUSTOMER_ID", "4202");
                            values.CheckAdd("INNER_ORDER", "1");
                            values.CheckAdd("DT_CREATED", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                            values.CheckAdd("PRODUCT_CLASS_ID", "10");
                            values.CheckAdd("ANY_CARTON_FLAG", "1");
                            values.CheckAdd("NAME", "Для конструктора");
                            values.CheckAdd("QTY", "1");
                            values.CheckAdd("DESIGN", "2");
                            values.CheckAdd("DELIVERY", "3");
                            values.CheckAdd("PACKING_TYPE", "0");
                        }
                        else if (EditMode == 4)
                        {
                            values.CheckAdd("CUSTOMER_ID", "4202");
                            values.CheckAdd("INNER_ORDER", "1");
                            values.CheckAdd("DT_CREATED", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                            values.CheckAdd("NAME", "Для лаборатории");
                            values.CheckAdd("QTY", "1");
                            values.CheckAdd("DELIVERY", "3");
                            values.CheckAdd("PACKING_TYPE", "0");
                        }

                        // Если активный пользователь есть в списке менеджеров, выберем его при создании образца
                        string emplId = Central.User.EmployeeId.ToString();
                        if (list.ContainsKey(emplId))
                        {
                            values.CheckAdd("MANAGER_EMPL_ID", emplId);
                        }
                    }
                    values.Add("ID", SampleId.ToString());

                    Status = values.CheckGet("STATUS").ToInt();
                    FromWeb = values.CheckGet("WEB").ToBool();
                    OldCardboardId = values.CheckGet("IDC").ToInt();
                    OldRawId = values.CheckGet("BLANK_IDC").ToInt();

                    if (values.CheckGet("LIMIT_ID").ToInt() > 1)
                    {
                        // Добавляем обоснование подтверждения менеджера
                        var confReasonId = values.CheckGet("CONFIRMATION_REASON").ToInt();
                        string confReason = SampleConfirmationReasons.Items.CheckGet(confReasonId.ToString());
                        string note = values.CheckGet("CONFIRMATION_NOTE");
                        if (!string.IsNullOrEmpty(note))
                        {
                            confReason = $"{confReason}: {note}";
                        }
                        values.CheckAdd("CONFIRMATION_REASON", $"{values.CheckGet("LIMIT_REASON")} / {confReason}");
                    }

                    if (Status == SampleStates.New || Status == SampleStates.Acceptance)
                    {
                        if (string.IsNullOrEmpty(values.CheckGet("DT_COMPLITED")))
                        {
                            // Для технологов предлагаем дату изготовления по умолчанию
                            if (EditMode == 1)
                            {
                                values.CheckAdd("DT_COMPLITED", DateTime.Now.AddDays(3).ToString("dd.MM.yyyy"));
                            }
                            else if (EditMode == 2 || EditMode == 4)
                            {
                                values.CheckAdd("DT_COMPLITED", DateTime.Now.ToString("dd.MM.yyyy"));
                            }
                        }

                        // Для новых образцов принудительно ставим необходимость чертежа, если есть приложенные файлы
                        if (values.CheckGet("DESIGN").ToInt() == 0)
                        {
                            if (values.CheckGet("INNER_ORDER").ToInt() == 0)
                            {
                                bool attachment = values.CheckGet("FILE_IS").ToBool();
                                if (attachment || (values.CheckGet("PRODUCT_CLASS_ID").ToInt() == 10))
                                {
                                    values.CheckAdd("DESIGN", "1");
                                }
                            }
                        }
                    }

                    SampleForm.SetValues(values);
                }
                Show();
            }
            else
            {
                var dw = new DialogWindow("Произошла ошибка! Откройте образец повторно", "Редактирование образца");
                dw.ShowDialog();
            }
        }

        /// <summary>
        /// Фильтрация списка марок
        /// </summary>
        private void UpdateMarkItems()
        {
            int profileId = Profile.SelectedItem.Key.ToInt();
            var markItems = new Dictionary<string, string>();
            bool exists = false;
            int currentMark = Mark.SelectedItem.Key.ToInt();

            if (profileId > 0)
            {
                // Находим количество слоев в профиле
                int qtyLayers = 0;
                foreach (var p in ProfilesDS.Items)
                {
                    if (p.CheckGet("ID").ToInt() == profileId)
                    {
                        qtyLayers = p.CheckGet("QTY_LAYERS").ToInt();
                    }
                }

                if (qtyLayers > 0)
                {
                    foreach (var m in CartonMarksDS.Items)
                    {
                        if (m.CheckGet("QTY_LAYERS").ToInt() == qtyLayers)
                        {
                            markItems.Add(m.CheckGet("ID"), m.CheckGet("NAME"));
                        }
                    }
                    if (markItems.ContainsKey(currentMark.ToString()))
                    {
                        exists = true;
                    }
                }
            }
            Mark.Items = markItems;
            if (!exists && (markItems.Count > 0))
            {
                Mark.SelectedItem = markItems.First();
            }
        }

        /// <summary>
        /// Фильтрация списка картона
        /// </summary>
        private void UpdateCardboardItems()
        {
            int profileId = Profile.SelectedItem.Key.ToInt();
            int markId = Mark.SelectedItem.Key.ToInt();
            int colorId = ColorOuter.SelectedItem.Key.ToInt();

            var cardboardItems = new Dictionary<string, string>();

            if (CardboardDS.Items.Count > 0)
            {
                if ((profileId > 0) && (markId > 0) && (colorId > 0))
                {
                    foreach (Dictionary<string, string> row in CardboardDS.Items)
                    {
                        if ((row.CheckGet("PROFILE_ID").ToInt() == profileId) 
                            && (row.CheckGet("MARK_ID").ToInt() == markId)
                            && (row.CheckGet("OUTER_COLOR_ID").ToInt() == colorId))
                        {
                            cardboardItems.Add(row.CheckGet("ID"), row.CheckGet("NAME"));
                        }
                    }
                }
            }

            Cardboard.Items = cardboardItems;
        }

        /// <summary>
        /// Открытие вкладки выбора картона заготовок для образца
        /// </summary>
        private void SelectSampleCardboard()
        {
            int profileId = Profile.SelectedItem.Key.ToInt();
            int markId = Mark.SelectedItem.Key.ToInt();
            int colorId = ColorOuter.SelectedItem.Key.ToInt();

            if ((profileId > 0) && (markId > 0) && (colorId > 0))
            {
                var selectCardboard = new SampleSelectCardboard();

                var p = new Dictionary<string, string>()
                {
                    { "PROFILE",  profileId.ToString() },
                    { "MARK",  markId.ToString() },
                    { "COLOR", colorId.ToString() },
                    { "CARDBOARD", Cardboard.SelectedItem.Key },
                };
                selectCardboard.ReceiverName = TabName;
                selectCardboard.Edit(p);
            }
            else
            {
                FormStatus.Text = "Сначала выберите профиль, марку и цвет";
            }
        }

        /// <summary>
        /// Установка картона заготовк для образца
        /// </summary>
        /// <param name="cardboard">картон заготовок</param>
        private void SetSampleCardboard(Dictionary<string, string> cardboard)
        {
            int cardboardId = cardboard.CheckGet("ID").ToInt();
            if (cardboardId > 0)
            {
                SampleCardboardId.Text = cardboardId.ToString();
                SampleCardboard.Text = cardboard.CheckGet("CARDBOARD_NAME");
                // Проверим листы в наличии. Если листов нет, поставим флаг ожидания сырья
                RawMissingCheckBox.IsChecked = false;
                if (cardboard.CheckGet("QTY").ToInt() == 0)
                {
                    RawMissingCheckBox.IsChecked = true;
                }
            }
            else
            {
                FormStatus.Text = "Ошибка выбора картона для образцов";
            }
        }

        /// <summary>
        /// Получение планового количества образцов на выбранную дату
        /// </summary>
        private async void GetSampleInWorkQty()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "GetQtyOnDate");
            q.Request.SetParam("DATE", CompletedDate.Text);


            await Task.Run(() =>
            {
                q.DoQuery();
            }
            );

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "DATE_QTY");
                    if (ds.Items.Count > 0)
                    {
                        var rec = ds.Items[0];
                        PlanCompletedQty.Text = $"{rec.CheckGet("CNT").ToInt()}/{rec.CheckGet("QTY").ToInt()}";
                    }
                }
            }

        }

        /// <summary>
        /// Отображение вкладки редактирования образца
        /// </summary>
        private void Show()
        {
            // Для технологов поле Покупатель не обязательно. Они могут создавать заявки для нужд предприятия
            string reqChar = TechnologMode ? " " : "*";
            CustomerLabel.Content = $"Покупатель: {reqChar}";

            string title = $"Образец {SampleId}";
            TabName = $"Sample_{SampleId}";
            Central.WM.AddTab(TabName, title, true, "add", this);
            SetReadonly();
        }

        /// <summary>
        /// Закрытие вкладки редактирования образца
        /// </summary>
        private void Close()
        {
            Central.WM.RemoveTab(TabName);
            Destroy();
        }

        /// <summary>
        /// Проверки перед сохранением
        /// </summary>
        private void Save()
        {
            var p = SampleForm.GetValues();
            bool resume = true;
            string msg = "";

            if (resume && (p.CheckGet("MANAGER_EMPL_ID").ToInt() == 0))
            {
                // Для внутренних заказов менеджера можно не указывать
                if (!(bool)InnerOrderCheckBox.IsChecked)
                {
                    resume = false;
                    msg = "Укажите менеджера";
                }
            }

            // Технологи могут создавать заявки для нужд предприятия. В этом случае покупатель не указывается
            if (resume && !TechnologMode)
            {
                if (p.CheckGet("CUSTOMER_ID").ToInt() == 0)
                {
                    resume = false;
                    msg = "Не выбран покупатель";
                }
            }

            if (resume && (p.CheckGet("PRODUCT_CLASS_ID").ToInt() == 0))
            {
                resume = false;
                msg = "Не выбран вид изделия";
            }

            if (resume && ((p.CheckGet("L").ToInt() == 0) || (p.CheckGet("B").ToInt() == 0)))
            {
                resume = false;
                msg = "Не задан размер изделия";
            }

            if (resume && (p.CheckGet("PROFILE_ID").ToInt() == 0))
            {
                resume = false;
                msg = "Не задан профиль картона";
            }

            if (resume && (p.CheckGet("MARK_ID").ToInt() == 0))
            {
                resume = false;
                msg = "Не задана марка картона";
            }

            if (resume && (p.CheckGet("COLOR_OUTER_ID").ToInt() == 0))
            {
                resume = false;
                msg = "Не задан цвет картона";
            }

            if (resume && !(bool)AnyMarkCheckBox.IsChecked && (p.CheckGet("IDC").ToInt() == 0))
            {
                resume = false;
                msg = "Выберите картон или поставьте флаг Без обязательной марки";
            }

            // Для технологов делаем проверку на картон для образцов, для остальных - пропускаем
            if (resume && TechnologMode)
            {
                if (!(bool)RawMissingCheckBox.IsChecked && (p.CheckGet("BLANK_IDC").ToInt() == 0))
                {
                    resume = false;
                    msg = "Надо выбрать картон для образцов или поставьте флаг Ожидание сырья";
                }
            }

            int sampleQty = p.CheckGet("QTY").ToInt();
            if (resume && (sampleQty == 0))
            {
                resume = false;
                msg = "Не задано количество образцов";
            }

            if (resume)
            {
                p.CheckAdd("STATUS", Status.ToString());
                p.CheckAdd("CONFIRMATION", Confirmation.ToString());
            }

            if (resume)
            {
                // При создании образца если выбран класс изделия ИСВ, а необходимость чертежа не проставили, сохраним принудительно необходимость чертежа
                if (SampleId == 0)
                {
                    if ((p.CheckGet("PRODUCT_CLASS_ID").ToInt() == 10) && p.CheckGet("DESIGN").ToInt() == 0)
                    {
                        p.CheckAdd("DESIGN", "1");
                    }
                }
            }

            // Проверяем, изменился ли картон или сырье. Проверка только для образцов в работе
            // Добавляем флаг удаления из очереди заданий, по умолчанию не удаляем
            p.CheckAdd("REMOVE_TASK", "0");
            if (resume)
            {
                if (Status == 1)
                {
                    var newCardboardId = p.CheckGet("IDC").ToInt();
                    var newRawId = p.CheckGet("BLANK_IDC").ToInt();
                    if ((newCardboardId != OldCardboardId) || (newRawId != OldRawId))
                    {
                        p["REMOVE_TASK"] = "1";
                    }
                }
            }

            if (resume)
            {
                p.CheckAdd("EDIT_MODE", EditMode.ToString());
                
                SaveData(p);
            }
            else
            {
                FormStatus.Text = msg;
            }
        }

        /// <summary>
        /// Сохраняет данные в БД
        /// </summary>
        /// <param name="p">Поля формы и их значения</param>
        private async void SaveData(Dictionary<string,string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "Save");
            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            }
            );

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    if (SampleCardboardId.Text.ToInt() != OldCardboardId)
                    {
                        SendMessage();
                    }

                    if (result.ContainsKey("ITEMS"))
                    {
                        //отправляем гриду сообщение о необходимости обновления
                        Messenger.Default.Send(new ItemMessage()
                        {
                            ReceiverGroup = "PreproductionSample",
                            ReceiverName = ReceiverName,
                            SenderName = "Sample",
                            Action = "Refresh",
                        });
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "PreproductionSample",
                            ReceiverName = ReceiverName,
                            SenderName = "Sample",
                            Action = "Refresh",
                        });
                    }
                }
                Close();
            }
            else
            {
                FormStatus.Text = "Ошибка! Повторите операцию";
            }
        }

        private async void SendMessage()
        {

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "SendMailForChangeCardboard");
            q.Request.SetParam("ID", SampleId.ToString());
            q.Request.SetParam("CARDBOARD_NAME", SampleCardboard.Text.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            }
            );

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {

                }
            }
            else
            {
                FormStatus.Text = q.Answer.Error.ToString();
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

        private void Profile_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UpdateMarkItems();
            UpdateCardboardItems();
        }

        private void Mark_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UpdateCardboardItems();
        }

        private void ColorOuter_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UpdateCardboardItems();
        }

        private void SampleCardboardButton_Click(object sender, RoutedEventArgs e)
        {
            SelectSampleCardboard();
        }

        private void CompletedDate_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Status == SampleStates.New || Status == SampleStates.InWork || Status == SampleStates.Acceptance)
            {
                var dt = CompletedDate.Text.ToDateTime();
                var today = DateTime.Now.Date;
                if (DateTime.Compare(dt, DateTime.Now.Date) >= 0)
                {
                    FormStatus.Text = "";
                    GetSampleInWorkQty();
                }
                else
                {
                    FormStatus.Text = $"Дата изготовления должна быть не раньше {today:dd.MM.yyyy}";
                    PlanCompletedQty.Text = $"0/0";
                }
            }
        }

        private void InnerOrder_Click(object sender, RoutedEventArgs e)
        {
            if (CustomerName.Items != null)
            {
                if (CustomerName.Items.ContainsKey("4202"))
                {
                    CustomerName.SetSelectedItemByKey("4202");
                }
            }
        }
    }
}
