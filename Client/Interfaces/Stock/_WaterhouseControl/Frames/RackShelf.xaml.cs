using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Client.Common.FormHelperField;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Редактирование полки стеллажа
    /// </summary>
    public partial class RackShelf : ControlBase
    {
        public RackShelf()
        {
            ControlTitle = "Редактирование полки";
            DocumentationUrl = "/doc/l-pack-erp/";
            RoleName = "[erp]warehouse_control";
            InitializeComponent();

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (System.Windows.Input.KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                Init();
                SetDefaults();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
            };

            {
                Commander.Add(new CommandItem()
                {
                    Name = "save",
                    Group = "main",
                    Enabled = true,
                    Title = "Сохранить",
                    Description = "Сохранить данные",
                    ButtonUse = true,
                    ButtonControl = SaveButton,
                    ButtonName = "SaveButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        Save();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "cancel",
                    Group = "main",
                    Enabled = true,
                    Title = "Отмена",
                    Description = "Отмена",
                    ButtonUse = true,
                    ButtonControl = CancelButton,
                    ButtonName = "CancelButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Close();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "help",
                    Group = "main",
                    Enabled = true,
                    Title = "Справка",
                    Description = "Показать справочную информацию",
                    ButtonUse = true,
                    ButtonName = "HelpButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    HotKey = "F1",
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
            }

            Commander.Init(this);
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Идентификатор полки
        /// </summary>
        public int RackShelfId { get; set; }

        /// <summary>
        /// Датасет с данными по всем складам WMS
        /// </summary>
        public ListDataSet WarehouseDataSet { get; set; }

        /// <summary>
        /// Разграничитель для наименования полки
        /// </summary>
        private string Delimiter { get; set; }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="RACK_SHELF_NUM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=RackShelfNumTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 32 },
                    },
                },
                new FormHelperField()
                {
                    Path="WMWA_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=WarehouseSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="WMRO_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=RowSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="WMRS_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=RackSectionSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="WMLE_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=LevelSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="WRST_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=RackShelfTypeSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="WMRH_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                    Control=null,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = Status;
        }

        public void SetDefaults()
        {
            WarehouseDataSet = new ListDataSet();

            Form.SetDefaults();

            ListWarehouse();
            ListRackShelfType();

            if (RackShelfId > 0)
            {
                GetData();
            }
        }

        /// <summary>
        /// Получаем список всех складов WMS для заполнения выпадающего списка складов
        /// </summary>
        public void ListWarehouse()
        {
            var p = new Dictionary<string, string>();
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Warehouse");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    WarehouseDataSet = ListDataSet.Create(result, "ITEMS");
                    WarehouseSelectBox.SetItems(WarehouseDataSet, FieldTypeRef.Integer, "WMWA_ID", "WAREHOUSE");
                    WarehouseSelectBox.SetSelectedItemByKey("1");
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Получаем список всех типов полок для заполнения выпадающего списка
        /// </summary>
        public void ListRackShelfType()
        {
            FormHelper.ComboBoxInitHelper(RackShelfTypeSelectBox, "Warehouse", "RackShelfType", "List", "WRST_ID", "SHELF_TYPE", null, true, true);
        }

        /// <summary>
        /// получение данных с сервера
        /// </summary>
        public async void GetData()
        {
            DisableControls();

            if (RackShelfId > 0)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("WMRH_ID", RackShelfId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "RackShelf");
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
                        var ds = ListDataSet.Create(result, "ITEMS");
                        Form.SetValues(ds);
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            EnableControls();
        }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 1;

            this.FrameName = $"{FrameName}_{RackShelfId}";
            if (RackShelfId == 0)
            {
                Central.WM.Show(FrameName, "Новая полка", true, "add", this);
            }
            else
            {
                Central.WM.Show(FrameName, $"Полка №{RackShelfId}", true, "add", this);
            }
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.Close(FrameName);
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
        }

        /// <summary>
        /// Очищаем наполнение селектбокса
        /// </summary>
        /// <param name="selectBox"></param>
        private void ClearSelectBox(SelectBox selectBox)
        {
            selectBox.DropDownListBox.Items.Clear();
            selectBox.DropDownListBox.SelectedItem = null;
            selectBox.ValueTextBox.Text = "";
            selectBox.Items = new Dictionary<string, string>();
            selectBox.SelectedItem = new KeyValuePair<string, string>();
        }

        /// <summary>
        /// Обновление данных при выборе склада
        /// </summary>
        public void SelectWarehouse()
        {
            ClearSelectBox(RowSelectBox);
            ClearSelectBox(RackSectionSelectBox);
            ClearSelectBox(LevelSelectBox);

            Delimiter = WarehouseDataSet.Items.FirstOrDefault(x => x.CheckGet("WMWA_ID").ToInt() == WarehouseSelectBox.SelectedItem.Key.ToInt()).CheckGet("DELIMITER");

            FormHelper.ComboBoxInitHelper(RowSelectBox, "Warehouse", "Row", "ListByWarehouse", "WMRO_ID", "ROW_NUM", new Dictionary<string, string>() { { "WMWA_ID", WarehouseSelectBox.SelectedItem.Key } }, true, true);
            FormHelper.ComboBoxInitHelper(RackSectionSelectBox, "Warehouse", "RackSection", "ListByWarehouse", "WMRS_ID", "RACK_SECTION_NUM", new Dictionary<string, string>() { { "WMWA_ID", WarehouseSelectBox.SelectedItem.Key } }, true, true);
            FormHelper.ComboBoxInitHelper(LevelSelectBox, "Warehouse", "Level", "ListByWarehouse", "WMLE_ID", "LEVEL_NUM", new Dictionary<string, string>() { { "WMWA_ID", WarehouseSelectBox.SelectedItem.Key } }, true, true);            
        }

        /// <summary>
        /// отпаравка данных на сервер
        /// </summary>
        public void SaveData(Dictionary<string, string> p)
        {
            DisableControls();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "RackShelf");
            q.Request.SetParam("Action", "Save");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    var id = ds.GetFirstItemValueByKey("ID").ToInt();
                    if (id != 0)
                    {
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "WarehouseControl",
                            ReceiverName = "WaterhouseRackShelf",
                            SenderName = this.FrameName,
                            Action = "Refresh",
                            Message = $"{id}",
                        });

                        Close();
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();
        }

        /// <summary>
        /// Сохраняем данные по хранилищу
        /// </summary>
        private void Save()
        {
            bool resume = true;
            Dictionary<string, string> formData = Form.GetValues();

            //стандартная валидация данных средствами формы
            if (resume)
            {
                resume = Form.Validate();
                if (!resume)
                {
                    Form.SetStatus("Не все данные заполнены верно", 1);
                }
            }

            if (resume)
            {
                resume = CheckUniqueRackShelf(formData);
                if (resume)
                {
                    //отправка данных
                    SaveData(formData);
                }
                else
                {
                    Form.SetStatus("Полка с заданными параметрами уже существует", 1);
                }
            }
        }

        /// <summary>
        /// Проверяем уникальность полки
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private bool CheckUniqueRackShelf(Dictionary<string, string> param)
        {
            bool result = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "RackShelf");
            q.Request.SetParam("Action", "CheckUnique");

            q.Request.SetParams(param);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var resultData = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (resultData != null)
                {
                    var ds = ListDataSet.Create(resultData, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        if (ds.Items.First().CheckGet("CNT").ToInt() == 0)
                        {
                            result = true;
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            return result;
        }

        /// <summary>
        /// Формирование наименования хранилища
        /// </summary>
        private void GenerateName()
        {
            var storageNum = "";
            if (!string.IsNullOrEmpty(RowSelectBox.SelectedItem.Value))
            {
                storageNum = RowSelectBox.SelectedItem.Value;
                if (!string.IsNullOrEmpty(RackSectionSelectBox.SelectedItem.Value))
                {
                    storageNum += Delimiter + RackSectionSelectBox.SelectedItem.Value;
                    if (!string.IsNullOrEmpty(LevelSelectBox.SelectedItem.Value))
                    {
                        storageNum += Delimiter + LevelSelectBox.SelectedItem.Value;
                    }
                }
            }

            RackShelfNumTextBox.Text = storageNum;
        }

        public void ShowHelp()
        {
            Central.ShowHelp(DocumentationUrl);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void WarehouseSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SelectWarehouse();
        }
        private void GenerateNameFromSelectBox(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GenerateName();
        }
    }
}
