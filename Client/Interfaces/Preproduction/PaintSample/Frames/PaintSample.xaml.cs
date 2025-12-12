using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Форма редактирования заявки на выкрасы
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class PaintSample : ControlBase
    {
        public PaintSample()
        {
            InitializeComponent();

            InitForm();
            InitPaintGrid();
            SetDefaults();
            ProcessPermition();

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverName == ControlName)
                {
                    ProcessMessage(msg);
                }
            };
        }
        /// <summary>
        /// Имя вкладки, откуда вызвана форма и куда передается ответ
        /// </summary>
        public string ReceiverName;
        /// <summary>
        /// Идентификатор заявки на выкрасы
        /// </summary>
        public int PaintSampleId;
        /// <summary>
        /// Данные для списка выкрасов в данной заявке
        /// </summary>
        private ListDataSet PaintSampleDS {  get; set; }
        /// <summary>
        /// Список идентификаторов удаленных записей
        /// </summary>
        private List<int> DeletedIds { get; set; }
        /// <summary>
        /// Данные справочника цветов
        /// </summary>
        private ListDataSet ColorRefDS { get; set; }
        /// <summary>
        /// Статус заявки. Регулирует доступность компонентов
        /// </summary>
        private int Status;
        /// <summary>
        /// Уровень доступа пользователя к функциям интерфейса
        /// </summary>
        Role.AccessMode RoleLevel { get; set; }
        /// <summary>
        /// Форма редактирования заявки на выкрасы
        /// </summary>
        FormHelper Form { get; set; }

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
                    case "create":
                        EditPaintItems(0);
                        break;
                    case "edit":
                        EditPaintItems(1);
                        break;
                    case "delete":
                        DeletePaintItem();
                        break;
                }
            }
        }

        /// <summary>
        /// Обработка прав доступа пользователя
        /// </summary>
        private void ProcessPermition()
        {
            RoleLevel = Central.Navigator.GetRoleLevel("[erp]paint_sample");
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            PaintSampleDS = new ListDataSet();
            PaintSampleDS.Init();
            DeletedIds = new List<int>();

            var demandTypeDict = new Dictionary<string, string>
            {
                { "0", "Действующие пантоны" },
                { "1", "Разработка пантонов" },
                { "2", "Подбор цвета по образцу" },
            };
            DemandType.Items = demandTypeDict;
            DemandType.SetSelectedItemByKey("0");

            Form.SetDefaults();
        }

        /// <summary>
        /// Обработка и выполнение команд
        /// </summary>
        /// <param name="command"></param>
        public void ProcessMessage(ItemMessage msg)
        {
            string command = msg.Action;
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "savepaintsample":
                        if (msg.ContextObject != null)
                        {
                            var v = (Dictionary<string, string>)msg.ContextObject;
                            SaveItem(v);
                        }
                        else
                        {
                            Form.SetStatus("Ошибка получения данных выкраса", 1);
                        }
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
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="DEMAND_TYPE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=DemandType,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
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
                    Path="MANAGER_ID",
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
                    Control=Note,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };
            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

        /// <summary>
        /// Инициализация таблицы с выкрасами
        /// </summary>
        private void InitPaintGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="N",
                    Path="PAINT_ORDER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Цвет бумаги",
                    Path="RAW_COLOR",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Тип бумаги",
                    Path="RAW_TYPE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Краска",
                    Path="PAINT_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
            };
            PaintGrid.SetColumns(columns);
            PaintGrid.SetPrimaryKey("PAINT_ORDER");
            PaintGrid.SetSorting("PAINT_ORDER", ListSortDirection.Ascending);
            PaintGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            PaintGrid.Commands = Commander;
            PaintGrid.AutoUpdateInterval = 0;

            PaintGrid.OnLoadItems = LoadItems;
            PaintGrid.OnDblClick = selectedItem =>
            {
                if (RoleLevel == Role.AccessMode.FullAccess)
                {
                    EditPaintItems(1);
                }
            };

            PaintGrid.Init();

        }

        /// <summary>
        /// Загрузка данных в таблицу из датасета
        /// </summary>
        private void LoadItems()
        {
            if (PaintSampleDS.Items != null)
            {
                if (PaintSampleDS.Items.Count > 0)
                {
                    PaintGrid.UpdateItems(PaintSampleDS);
                }
                else
                {
                    PaintGrid.ClearItems();
                }
            }
        }

        /// <summary>
        /// Получение данных для заполнения формы
        /// </summary>
        private async void GetData()
        {
            Status = 0;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PaintSample");
            q.Request.SetParam("Action", "Get");
            q.Request.SetParam("ID", PaintSampleId.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    // Справочник покупателей
                    var customerDS = ListDataSet.Create(result, "CUSTOMERS");
                    CustomerName.Items = customerDS.GetItemsList("ID", "NAME");

                    // Справочник менеджеров
                    var managerDS = ListDataSet.Create(result, "MANAGERS");
                    ManagerName.Items = managerDS.GetItemsList("ID", "FIO");

                    // Справочник цветов
                    ColorRefDS = ListDataSet.Create(result, "COLORS");

                    if (PaintSampleId > 0)
                    {
                        var paintSampleDS = ListDataSet.Create(result, "PAINT_SAMPLE");
                        if (paintSampleDS.Items != null)
                        {
                            if (paintSampleDS.Items.Count > 0)
                            {
                                var rec = paintSampleDS.Items[0];
                                Status = rec.CheckGet("STATUS").ToInt();
                            }
                        }

                        PaintSampleDS = ListDataSet.Create(result, "PAINT_SMP_ITEMS");
                        //Добавляем поле с номером
                        foreach (var item in PaintSampleDS.Items)
                        {
                            var n = item.CheckGet("_ROWNUMBER");
                            item.CheckAdd("PAINT_ORDER", n);
                            item.CheckAdd("CHANGED", "0");
                        }

                        Form.SetValues(paintSampleDS);
                        PaintGrid.LoadItems();
                    }
                    else
                    {
                        // Если активный пользователь есть в списке менеджеров, выберем его при создании образца
                        string emplId = Central.User.EmployeeId.ToString();
                        if (ManagerName.Items.ContainsKey(emplId))
                        {
                            ManagerName.SetSelectedItemByKey(emplId);
                        }
                    }

                    SetFieldsAvailable();
                    Show();
                }
            }
        }

        /// <summary>
        /// Вызов формы редактирования заявки на выкрас
        /// </summary>
        /// <param name="id"></param>
        public void Edit(int id=0)
        {
            PaintSampleId = id;
            ControlName = $"PaintSample_{id}";
            ControlTitle = $"Заявка на выкрасы {id}";
            if (id == 0)
            {
                ControlTitle = "Новая заявка на выкрасы";
            }

            GetData();
        }

        /// <summary>
        /// Вызов формы редактирования выкраса
        /// </summary>
        /// <param name="oper">0 - создание, 1 - изменение</param>
        private void EditPaintItems(int oper = 0)
        {
            var itemFrame = new PaintSampleItem();
            itemFrame.ReceiverName = ControlName;
            itemFrame.ColorRefDS = ColorRefDS;

            var v = new Dictionary<string, string>();

            if (oper == 1)
            {
                v = PaintGrid.SelectedItem;
            }
            else
            {
                v.CheckAdd("ID", "0");
                v.CheckAdd("PAINT_ORDER", (PaintSampleDS.Items.Count + 1).ToString());
                // По умолчанию заполняем Бурый и мукулатура
                v.CheckAdd("RAW_COLOR_ID", "2");
                v.CheckAdd("RAW_TYPE_ID", "0");
            }
            v.CheckAdd("DEMAND_TYPE", DemandType.SelectedItem.Key);
            itemFrame.Edit(v);
        }

        /// <summary>
        /// Удаление выкраса из таблицы
        /// </summary>
        private void DeletePaintItem()
        {
            var item = PaintGrid.SelectedItem;
            int deletedId = item.CheckGet("ID").ToInt();
            if (deletedId > 0)
            {
                DeletedIds.Add(deletedId);
            }
            int paintOrder = item.CheckGet("PAINT_ORDER").ToInt();
            foreach (var row in PaintSampleDS.Items)
            {
                if (row.CheckGet("PAINT_ORDER").ToInt() == paintOrder)
                {
                    PaintSampleDS.Items.Remove(row);
                    break;
                }
            }

            // Пересчитываем номера строк
            int i = 1;
            foreach (var row in PaintSampleDS.Items)
            {
                row["PAINT_ORDER"] = i.ToString();
                i++;
            }

            PaintGrid.LoadItems();
        }

        /// <summary>
        /// Настройка доступности полей
        /// </summary>
        private void SetFieldsAvailable()
        {
            DemandType.IsEnabled = false;
            if (Status == 0 && PaintGrid.Items.Count == 0)
            {
                DemandType.IsEnabled = true;
            }
            // Редактировать можно только новые заявки. Если заявка выполнена или отклонена, изменять нельзя
            if (Status > 2)
            {
                ManagerName.IsReadOnly = true;
                CustomerName.IsReadOnly = true;
                CreateButton.IsEnabled = false;
                EditButton.IsEnabled = false;
                DeleteButton.IsEnabled = false;
                Note.IsReadOnly = true;
            }
            // Заявку со статусом В работе можно редактировать только при наличии спецправ
            else if (Status == 2)
            {
                ManagerName.IsReadOnly = true;
                CustomerName.IsReadOnly = true;

                bool specialRole = RoleLevel == Role.AccessMode.Special;
                CreateButton.IsEnabled = specialRole;
                EditButton.IsEnabled = specialRole;
                DeleteButton.IsEnabled = specialRole;
            }
        }

        /// <summary>
        /// Отображение вкладки с формой редактирования
        /// </summary>
        public void Show()
        {
            Central.WM.Show(ControlName, ControlTitle, true, "add", this);
        }

        /// <summary>
        /// Подготовка к сохранению
        /// </summary>
        public void Save()
        {
            bool resume = true;
            var v = Form.GetValues();

            // Должна быть хотя бы одна строка в таблице выкрасов
            if (PaintGrid.Items.Count == 0)
            {
                Form.SetStatus("Должна быть хотя бы одна краска", 1);
                resume = false;
            }

            if (resume)
            {
                v.Add("ID", PaintSampleId.ToString());
                //Добавим имя менеджера и заказчика для записи в сообщении
                v.Add("CUSTOMER_NAME", CustomerName.SelectedItem.Value);
                v.Add("MANAGER_NAME", ManagerName.SelectedItem.Value);

                var paintItems = JsonConvert.SerializeObject(PaintSampleDS.Items);
                v.Add("PAINT_ITEMS", paintItems);

                var deletedIds = JsonConvert.SerializeObject(DeletedIds);
                v.Add("DELETED_IDS", deletedIds);

                SaveData(v);
            }
        }

        /// <summary>
        /// Запись данных в БД
        /// </summary>
        /// <param name="data"></param>
        private async void SaveData(Dictionary<string, string> data)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PaintSample");
            q.Request.SetParam("Action", "Save");
            q.Request.SetParams(data);

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
                        //Если ответ не пустой, отправляем сообщение Гриду о необходимости обновить данные
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "Preproduction",
                            ReceiverName = ReceiverName,
                            SenderName = ControlName,
                            Action = "Refresh",
                        });

                        Close();
                    }
                }
            }
        }

        /// <summary>
        /// Сохранение информации о выкрасе
        /// </summary>
        /// <param name="item"></param>
        private void SaveItem(Dictionary<string, string> item)
        {
            int paintOrder = item.CheckGet("PAINT_ORDER").ToInt();
            item.CheckAdd("CHANGED", "1");

            if (paintOrder > PaintSampleDS.Items.Count)
            {
                PaintSampleDS.Items.Add(item);
            }
            else
            {
                //Перебираем все записи, когда найдем, обновляем все поля
                foreach (var ps in PaintSampleDS.Items)
                {
                    if (ps.CheckGet("PAINT_ORDER").ToInt() == paintOrder)
                    {
                        foreach (var k in item.Keys)
                        {
                            if (ps.ContainsKey(k))
                            {
                                ps[k] = item[k];
                            }
                        }
                    }
                }
            }

            PaintGrid.LoadItems();
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
