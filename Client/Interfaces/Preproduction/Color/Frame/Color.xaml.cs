using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Stock;
using GalaSoft.MvvmLight.Messaging;
using Gu.Wpf.DataGrid2D;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Диалог редактирования - добавления цвета
    /// </summary>
    /// <author>eletskikh_ya</author>
    public partial class Color : UserControl
    {
        /// <summary>
        /// Конструктор класса
        /// </summary>
        public Color()
        {
            InitializeComponent();

            defaultTextHexBrush = TextHex.Background;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            PaintComponentGridInit();
            InitForm();
            ProcessPermissions();
            SetDefaults();
        }

        /// <summary>
        /// начальный цвет фона текстового поля, для восстановления, когда цвет не может быть посчитан
        /// </summary>
        private Brush defaultTextHexBrush;

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// Имя вкладки, которая вызвала открытие фрейма, и в которую возвращается фокус после закрытия фрейма
        /// </summary>
        public string ReceiverName { get; set; }

        /// <summary>
        /// идентификатор записи, с которой работает форма
        /// (primary key записи таблицы)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Признак, что данные в форме изменились и надо передать сообщение гриду об обновлении данных
        /// </summary>
        private bool DataChanged;

        /// <summary>
        /// Право на выполнение специальных действий
        /// </summary>
        public bool MasterRights;

        /// <summary>
        /// Признак, что редактируем архивную краску
        /// </summary>
        public bool ArchivedColor;

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }


        public Dictionary<string, string> selectedComponent { get; set; }


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
        /// Освобождение ресурсов
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "colorMain",
                ReceiverName = "",
                SenderName = FrameName,
                Action = "Closed",
            });

            GridComposition.Destruct();

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
            Central.WM.SetActive(ReceiverName);
            ReceiverName = "";
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        public void ProcessPermissions(string roleCode = "")
        {
            // Если пользователь имеет спецправа, включаем режим мастера
            var mode = Central.Navigator.GetRoleLevel("[erp]color");
            switch (mode)
            {
                case Role.AccessMode.Special:
                    MasterRights = true;
                    break;

                default:
                    MasterRights = false;
                    break;
            }
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();
            Id = 0;
            DataChanged = false;
        }

        /// <summary>
        /// Инициализация грида компонентов краски
        /// </summary>
        public void PaintComponentGridInit()
        {
            var list = new Dictionary<string, string>();
            list.Add("0", "универсальный");
            list.Add("1", "по белому");
            list.Add("2", "по бурому");

            TypeComponent.SetItems(list);

            var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Рецептура",
                        Path="NAME",
                        Doc="Наименование цвета",
                        ColumnType=ColumnTypeRef.String,
                        Width=140,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Описание",
                        Path="DESCRIPTION",
                        Doc="Тестовое обозначение цвета",
                        ColumnType=ColumnTypeRef.String,
                        Width=160,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Доля",
                        Path="RATIO",
                        Doc="Сокращенное наименование цвета",
                        ColumnType=ColumnTypeRef.Double,
                        Format = "0.00000",
                        Width=80,
                    },
                    new DataGridHelperColumn
                    {
                       Header = " ",
                       Path = "_",
                       ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                       MinWidth = 5,
                       MaxWidth = 800,
                    },
                };

            GridComposition.SetColumns(columns);

            GridComposition.SetSorting("NAME", ListSortDirection.Ascending);
            GridComposition.AutoUpdateInterval = 0;
            GridComposition.Init();

            GridComposition.Run();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            GridComposition.OnSelectItem = selectedItem =>
            {
                GridCompositionUpdateActions(selectedItem);
            };
        }

        private void InitForm()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control = Name,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="NAME2",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control = TextColor,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SHORT_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control = TextColorShort,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PANTONE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control = TextColorPanton,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CARDBOARD_TYPE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TypeComponent,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ARCHIVE_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control = CheckArchive,
                    ControlType = "CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="RARE_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control = CheckRare,
                    ControlType = "CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NEW_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control = CheckNew,
                    ControlType = "CheckBox",
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
            };

            Form.SetFields(fields);

            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = Status;
        }

        private void GridCompositionUpdateActions(Dictionary<string, string> selectedItem)
        {
            selectedComponent = selectedItem;
            if(selectedItem != null)
            {
                DeleteButton.IsEnabled = true;
            }
            else DeleteButton.IsEnabled = false;

            AddButton.IsEnabled = !CheckComposition();
        }


        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            // Обновить компоненты краски при добавлении
            if (m.ReceiverGroup.IndexOf("colorMain") > -1)
            {
                if (m.ReceiverName.IndexOf(FrameName) > -1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            DataChanged = true;
                            PaintComponentGridLoadItems();
                            break;
                        case "AddComponent":
                            DataChanged = true;
                            Dictionary<string, string> item = (Dictionary<string, string>)m.ContextObject;
                            if(item!= null)
                            {
                                AddComponentToGrid(item);
                            }
                            Form.SetStatus("");
                            break;
                    }
                }
            }

        }

        /// <summary>
        /// Функция редактирования
        /// </summary>
        /// <param name="_Id">Идентификатор цвет, если 0 то это создание нового цвета</param>
        public void Edit(int _Id=0)
        {
            Id = _Id;
            FrameName = $"Color_{Id}";
            GetData();
        }

        /// <summary>
        /// Загрузка грида компонентов краски
        /// </summary>
        public async void PaintComponentGridLoadItems()
        {
            DeleteButton.IsEnabled = false;
            AddButton.IsEnabled = true;

            if (Id > -1)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("COLOR_ID", Id.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "Color");
                q.Request.SetParam("Action", "ListPaintComponent");
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
                        var ds = ListDataSet.Create(result, "ITEMS");
                        GridComposition.UpdateItems(ds);

                        AddButton.IsEnabled = !CheckComposition();
                    }
                }
            }
        }

        /// <summary>
        /// получение данных с сервера
        /// </summary>
        public async void GetData()
        {
            DisableControls();

            CheckArchive.IsEnabled = false;
            CheckNew.IsChecked = true;

            if (Id > 0)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("COLOR_ID", Id.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "Color");
                q.Request.SetParam("Action", "Get");
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
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            Form.SetValues(ds);

                            if (ds.Items != null)
                            {
                                if (ds.Items.Count > 0)
                                {
                                    var rec = ds.Items[0];

                                    // Если есть техкарты с выбранной краской не в архиве, то блокируем возможность отправить краску в архив
                                    ArchivedColor = (bool)CheckArchive.IsChecked;
                                    int inUse = rec.CheckGet("IN_USE").ToInt();
                                    if (!(bool)CheckArchive.IsChecked && inUse > 0)
                                    {
                                        CheckArchive.IsEnabled = false;
                                    }
                                    else
                                    {
                                        CheckArchive.IsEnabled = true;
                                    }

                                    // Снимаем блокировку с новой краски, если есть отметка и у пользователя есть спецправа
                                    int newFlag = rec.CheckGet("NEW_FLAG").ToInt();
                                    if ((newFlag == 1) && MasterRights)
                                    {
                                        CheckNew.IsEnabled = true;
                                    }
                                }
                            }
                        }

                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            Show();

            PaintComponentGridLoadItems();

            EnableControls();
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
        }


        /// <summary>
        /// Показ формы
        /// </summary>
        public void Show()
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 1;

            var groupName = Name.Text;

            if (Id == 0)
            {
                Central.WM.Show(FrameName, "Новая краска", true, "add", this);
            }
            else
            {
                Central.WM.Show(FrameName, $"Краска {groupName}", true, "add", this);
            }

        }

        /// <summary>
        /// Проверка корректности состава краски
        /// </summary>
        /// <returns></returns>
        private bool CheckComposition()
        {
            bool result = false;

            // Для литой тары используются готовые краски, состав не проверяем
            if (TypeComponent.SelectedItem.Key.ToInt() == 3)
            {
                result = true;
            }

            if (!result)
            {
                if (GridComposition.Items != null)
                {
                    if (GridComposition.Items.Count > 0)
                    {
                        // Суммируем доли всех компонентов, должна быть 1
                        // Используем тип decimal чтобы исключить ошибку округления
                        decimal sumRation = 0;
                        foreach (var item in GridComposition.Items)
                        {
                            sumRation += (decimal)item.CheckGet("RATIO").ToDouble();
                        }

                        if (sumRation >= 1M)
                        {
                            result = true;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// сохранение\обновление данных
        /// </summary>
        public void Save()
        {
            bool resume = true;
            string error = "";

            //стандартная валидация данных средствами формы
            if (resume)
            {
                var validationResult = Form.Validate();
                if (!validationResult)
                {
                    resume = false;
                }
            }

            // Сохранять и изменять можно данные для краски с полностью заполненной формулой (составом)
            if (resume)
            {
                if (!CheckComposition())
                {
                    error = "Перед сохранением необходимо заполнить состав краски";
                    resume = false;
                }
            }

            var v = Form.GetValues();

            // Для новой краски ставим флаг
            //if (Id == 0)

            if (ArchivedColor && (v.CheckGet("ARCHIVE_FLAG").ToInt() == 0))
            {
                v.CheckAdd("NEW_FLAG", "1");
            }



            //отправка данных
            if (resume)
            {
                SaveData(v);
            }
            else
            {
                Form.SetStatus(error, 1);
            }
        }

        /// <summary>
        /// Добавление выбранного компонента в таблицу. Выполняется только для новых красок
        /// </summary>
        /// <param name="item">данные по компоненту</param>
        private void AddComponentToGrid(Dictionary<string, string> item)
        {
            List<Dictionary<string, string>> items = new List<Dictionary<string, string>>();
            if (GridComposition.Items != null)
            {
                items = GridComposition.Items;
            }

            if (item.Count > 0)
            {
                items.Add(item);
                GridComposition.Items = items;
            }

            AddButton.IsEnabled = !CheckComposition();
        }

        /// <summary>
        /// Удалить текущий компонент краски
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private async void DeleteComponent()
        {
            if (selectedComponent != null)
            {
                string componentName = selectedComponent.CheckGet("NAME");
                var dw = new DialogWindow($"Вы действительно хотите удалить компонент {componentName}?", "Удаление компонента", "Подтверждение удаления компонента", DialogWindowButtons.NoYes);
                if (dw.ShowDialog() == true)
                {
                    int componentId = selectedComponent.CheckGet("PACM_ID").ToInt();

                    // Для сохраненных компонентов выполняем удаление
                    if (componentId > 0)
                    {
                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Preproduction");
                        q.Request.SetParam("Object", "Color");
                        q.Request.SetParam("Action", "DeletePaintComponent");
                        q.Request.SetParam("ID", componentId.ToString());

                        await Task.Run(() =>
                        {
                            q.DoQuery();
                        });

                        if (q.Answer.Status == 0)
                        {
                            DataChanged = true;
                            PaintComponentGridLoadItems();
                        }
                    }
                    // Для несохраненных компонентов выполняем удаление записи из таблицы
                    else
                    {
                        foreach (var row in GridComposition.Items)
                        {
                            if (row.CheckGet("NAME") == componentName)
                            {
                                GridComposition.Items.Remove(row);
                                GridComposition.UpdateItems();
                                break;
                            }
                        }
                    }

                    AddButton.IsEnabled = !CheckComposition();
                }
            }
            
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            if (DataChanged)
            {
                //отправляем сообщение гриду о необходимости обновить данные
                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = "colorMain",
                    ReceiverName = ReceiverName,
                    SenderName = FrameName,
                    Action = "Refresh",
                });
            }

            Central.WM.Close(FrameName);
            Destroy();
        }

        /// <summary>
        /// отпаравка данных на сервер
        /// </summary>
        public async void SaveData(Dictionary<string, string> p)
        {
            DisableControls();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Color");
            q.Request.SetParam("Action", "Save");

            q.Request.SetParams(p);

            if (Id > 0) // Update
            {
                q.Request.SetParam("COLOR_ID", Id.ToString());
            }

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        var id = ds.GetFirstItemValueByKey("COLOR_ID").ToInt();
                        if (id != 0)
                        {
                            // Необходимо проверить несохраненные компоненты
                            if (GridComposition.Items != null)
                            {
                                if (GridComposition.Items.Count > 0)
                                {
                                    foreach (var item in GridComposition.Items)
                                    {
                                        if (item.ContainsKey("Id"))
                                        {
                                            if (item["Id"] == "0")
                                            {
                                                Dictionary<string, string> v = new Dictionary<string, string>();
                                                // необходимо сохранить компоненту
                                                v.Add("ID", id.ToString());
                                                v.Add("COMPONENT_ID", item["PACO_ID"]);
                                                v.Add("RATIO", item["RATIO"]);

                                                q = new LPackClientQuery();
                                                q.Request.SetParam("Module", "Preproduction");
                                                q.Request.SetParam("Object", "Color");
                                                q.Request.SetParam("Action", "AddPaintComponent");

                                                q.Request.SetParams(v);

                                                await Task.Run(() =>
                                                {
                                                    q.DoQuery();
                                                });

                                                if (q.Answer.Status == 0)
                                                {

                                                }

                                            }
                                        }
                                    }
                                }
                            }
                            DataChanged = true;
                            Close();
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void TextHex_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextHex.Background = defaultTextHexBrush;

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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Name.Focus();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddComponent();
        }

        private void AddComponent()
        {
            var newComponent = new AddColorComponent(GridComposition.Items);
            newComponent.ReceiverName = FrameName;
            newComponent.Edit(Id);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteComponent();
        }
    }
}
