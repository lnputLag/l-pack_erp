using Client.Common;
using Client.Interfaces.Main;
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
    /// Форма редактирования базовых параметров образца от клиента
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class PatternOrder : ControlBase
    {
        /// <summary>
        /// Инициализация
        /// </summary>
        public PatternOrder()
        {
            InitializeComponent();

            InitForm();
            InitGrid();
            SetDefaults();
        }

        /// <summary>
        /// ID образца от клиента
        /// </summary>
        int PatternId;
        /// <summary>
        /// Форма редактирования
        /// </summary>
        public FormHelper PatternForm { get; set; }
        /// <summary>
        /// Список выбранных целей анализа образца от клиента
        /// </summary>
        List<int> InitPurposeIds;
        /// <summary>
        /// Имя вкладки, которая становится активной после закрытия фрейма
        /// </summary>
        public string ReceiverName;
        /// <summary>
        /// Данные для таблицы целей
        /// </summary>
        private ListDataSet PurposeDS { get; set; }

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
            PatternForm = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="ID_POK",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Pokupatel,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                    },
                },
                new FormHelperField()
                {
                    Path="CLIENT_CUSTOMER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ClientCustomer,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="MANUFACTURER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Manufacturer,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CONTACT_PERSON",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ContactPerson,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="EMPL_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Manager,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=PatternName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="COMPETITOR_PRICE",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=CompetitorPrice,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="COMPETITOR_SALES",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CompetitorSales,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 6 },
                    },
                },
                new FormHelperField()
                {
                    Path="COMPETITOR_ID_MARKA",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=SourceMarka,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="INTERNAL_CRACK_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=InternalCrack,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            PatternForm.SetFields(fields);
            PatternForm.StatusControl = FormStatus;
        }

        /// <summary>
        /// Инициализация списка целей
        /// </summary>
        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="ORDER_NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=30,
                },
                new DataGridHelperColumn
                {
                    Header="Цель",
                    Path="NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=200,
                },
                new DataGridHelperColumn
                {
                    Header="Ответственный",
                    Path="GROPUP_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=130,
                },
                new DataGridHelperColumn
                {
                    Header="Есть",
                    Path="CHECKING",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    Editable=true,
                    Width2=40,

                },
                new DataGridHelperColumn
                {
                    Header="ИД цели",
                    Path="PTPU_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("ORDER_NUM");
            Grid.SetSorting("ORDER_NUM", ListSortDirection.Ascending);
            Grid.AutoUpdateInterval = 0;
            Grid.OnLoadItems = LoadItems;
            Grid.Init();

        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            PatternId = 0;
            InitPurposeIds = new List<int>();

            PatternForm.SetDefaults();
            PurposeDS = new ListDataSet();
            PurposeDS.Init();
        }

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
        /// Получение данных для формы
        /// </summary>
        private async void GetData()
        {
            var values = new Dictionary<string, string>();
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PatternOrder");
            q.Request.SetParam("Action", "Get");
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
                    // содержимое таблицы с целями
                    PurposeDS = ListDataSet.Create(result, "UsedPurpose");

                    foreach (var row in PurposeDS.Items)
                    {
                        if (row.CheckGet("CHECKING").ToInt() == 1)
                        {
                            InitPurposeIds.Add(row["PTPU_ID"].ToInt());
                        }
                    }

                    // содержимое выпадающего списка Покупатель
                    var customersDS = ListDataSet.Create(result, "Customers");
                    Pokupatel.Items = customersDS.GetItemsList("ID", "NAME");

                    // содержимое выпадающего списка Менеджер
                    var ManagersDS = ListDataSet.Create(result, "Managers");
                    Manager.Items = ManagersDS.GetItemsList("ID", "FIO");

                    // содержимое списка марок картона
                    var sourceMarkaDS = ListDataSet.Create(result, "SrcMarka");
                    SourceMarka.Items = sourceMarkaDS.GetItemsList("ID", "NAME_MARKA");

                    // значения основных полей
                    if (PatternId > 0)
                    {
                        var patternsDS = ListDataSet.Create(result, "Pattern");
                        if (patternsDS.Items.Count > 0)
                        {
                            values = patternsDS.Items[0];
                        }
                    }
                    else
                    {
                        // Если активный пользователь есть в списке менеджеров, выберем его при создании образца
                        string emplId = Central.User.EmployeeId.ToString();
                        if (Manager.Items.ContainsKey(emplId))
                        {
                            values.CheckAdd("EMPL_ID", emplId);
                        }

                    }
                    PatternForm.SetValues(values);
                    Grid.LoadItems();
                    Show();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        private void LoadItems()
        {
            if (PurposeDS.Items != null)
            {
                Grid.UpdateItems(PurposeDS);
            }
        }

        /// <summary>
        /// Основной метод для создания вкладки редактирования образца от клиента
        /// </summary>
        /// <param name="Id">ID образца от клиента</param>
        public void Edit(int Id)
        {
            PatternId = Id;
            ControlName = $"Pattern_{PatternId}";

            GetData();
        }

        /// <summary>
        /// Отображение вкладки с формой редактирования
        /// </summary>
        private void Show()
        {
            string title = $"Образец от клиента {PatternId}";
            if (PatternId == 0)
            {
                title = "Новый образец";
            }
            Central.WM.Show(ControlName, title, true, "add", this);
        }

        /// <summary>
        /// Сохранение данных
        /// </summary>
        private async void Save()
        {
            bool resume = true;

            if (string.IsNullOrEmpty(PatternName.Text) || (Manager.SelectedItem.Key == null))
            {
                resume = false;
                PatternForm.SetStatus("Заполнены не все обязательные поля", 1);
            }

            // выбранные цели
            List<int> purposeIds = new List<int>();
            if (resume)
            {
                var rows = Grid.GetItems();

                foreach (var row in rows)
                {
                    if ((row["CHECKING"].ToInt() == 1) && !InitPurposeIds.Contains(row["PTPU_ID"].ToInt()))
                    {
                        purposeIds.Add(row["PTPU_ID"].ToInt());
                    }
                }

                if ((purposeIds.Count == 0) && (InitPurposeIds.Count == 0))
                {
                    resume = false;
                    PatternForm.SetStatus("Не выбрано ни одной цели", 1);
                }
            }

            if (resume)
            {
                var p = PatternForm.GetValues();
                p.Add("ID", PatternId.ToString());
                p.Add("PTPU_LIST", String.Join(",", purposeIds));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "PatternOrder");
                q.Request.SetParam("Action", "Save");
                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        //Если ответ не пустой, отправляем сообщение Гриду о необходимости обновить данные
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "Preproduction",
                            ReceiverName = ReceiverName,
                            SenderName = "PatternRecord",
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
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        private void Close()
        {
            Central.WM.RemoveTab(ControlName);
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
    }
}
