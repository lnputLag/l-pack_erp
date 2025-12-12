using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperator
{
    /// <summary>
    /// перечень брака
    /// </summary>
    /// <author>zelenskiy_sv</author>
    public partial class Defects : UserControl
    {
        public Defects()
        {
            FrameName = "Defects";

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();

            DefectsGridInit();

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
        /// идентификатор записи, с которой работает форма
        /// (primary key записи таблицы)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// идентификатор записи причины простоя
        /// </summary>
        public int SelectedReasonID { get; set; }

        /// <summary>
        /// идентификатор записи описания причины простоя
        /// </summary>
        public int SelectedReasonDetailID { get; set; }

        /// <summary>
        /// Выбранная запись в IdleGrid
        /// </summary>
        public Dictionary<string, string> SelectedIdleItem { get; set; }
        public Dictionary<string, string> SelectedDefectReason { get; private set; }
        public Dictionary<string, string> SelectedDefectItem { get; private set; }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                //new FormHelperField()
                //{
                //    Path="REASON",
                //    FieldType=FormHelperField.FieldTypeRef.String,
                //    Control=DefectReasonText,
                //    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                //        //{ FormHelperField.FieldFilterRef.Required, null },
                //    },
                //},
            };

            Form.SetFields(fields);

            //после установки значений
            Form.AfterSet = (Dictionary<string, string> v) =>
            {
                //фокус на ввод причины простоя
                DefectsGrid.Focus();
            };
        }

        /// <summary>
        /// инициализация грида (причины простоев)
        /// </summary>
        public void DefectsGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                     new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=25,
                    },
                     new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="PRCI_ID",
                        Doc="ИД",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=60,
                    },
                     new DataGridHelperColumn
                    {
                        Header="ИД ПЗ",
                        Path="ID_PZ",
                        Doc="ИД ПЗ",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=70,
                    },
                     new DataGridHelperColumn
                    {
                        Header="Время",
                        Path="DTTM",
                        Doc="Время",
                        ColumnType=ColumnTypeRef.String,
                        Width=120,
                    },
                     new DataGridHelperColumn
                    {
                        Header="№ ПЗ",
                        Path="NUM",
                        Doc="№ ПЗ",
                        ColumnType=ColumnTypeRef.String,
                        Width=80,
                    },
                     new DataGridHelperColumn
                    {
                        Header="Длина",
                        Path="LENGTH",
                        Doc="Длина",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=50,
                    },
                     new DataGridHelperColumn
                    {
                        Header="Добавлено",
                        Path="QTY",
                        Doc="Добавлено",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=70,
                    },
                     new DataGridHelperColumn
                    {
                       Header = " ",
                       Path = "_",
                       ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                       MinWidth = 5,
                       MaxWidth = 2000,
                    },
                };
                DefectsGrid.SetColumns(columns);

                DefectsGrid.UseRowHeader = false;
                DefectsGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                DefectsGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {
                        SelectedDefectItem = selectedItem;
                        //DefectReasonsGrid.LoadItems();
                    }
                };

                //данные грида
                DefectsGrid.OnLoadItems = DefectsGridLoadItems;

                DefectsGrid.OnDblClick = DefectEdit;

                DefectsGrid.LayoutTransform = new ScaleTransform(1.5, 1.5);

                DefectsGrid.Run();
            }
        }

        /// <summary>
        /// загрузка грида
        /// </summary>
        public async void DefectsGridLoadItems()
        {
            DefectsDisableControls();

            var ds = await ListDefects();
            DefectsGrid.UpdateItems(ds);

            DefectsEnableControls();
        }

        /// <summary>
        /// Список причин брака
        /// </summary>
        public static async Task<ListDataSet> ListDefects()
        {
            var ds = new ListDataSet();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Defect");
            q.Request.SetParam("Action", "ListUncommented");
            q.Request.SetParam("ID_ST", CorrugatorMachineOperator.CurrentMachineId.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            _ = await Task.Run(() =>
            {
                q.DoQuery();
                return q;
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    ds = ListDataSet.Create(result, "ITEMS");
                }
            }

            return ds;
        }

        public void DefectEdit(Dictionary<string, string> defect)
        {
            if (defect != null)
            {
                int id = defect.CheckGet("PRCI_ID").ToInt();
                string num = defect.CheckGet("NUM");

                var defectEditForm = new FormExtend()
                {
                    FrameName = "DefectEdit",
                    ID = "PRCI_ID",
                    Id = id,
                    Title = $"Брак по заданию {num}",

                    QueryGet = new FormExtend.RequestData()
                    {
                        Module = "Production",
                        Object = "Defect",
                        Action = "Get"
                    },

                    QuerySave = new FormExtend.RequestData()
                    {
                        Module = "Production",
                        Object = "Defect",
                        Action = "Save"
                    },

                    Fields = new List<FormHelperField>()
                    {
                        new FormHelperField()
                        {
                            Path="PCRR_ID",
                            FieldType=FormHelperField.FieldTypeRef.Integer,
                            Description = "Причина:",
                            ControlType = "SelectBox",
                            Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                                { FormHelperField.FieldFilterRef.Required, null },
                            },
                            Width = 500,
                            
                        },
                        new FormHelperField()
                        {
                            Path="COMMENTS",
                            FieldType=FormHelperField.FieldTypeRef.String,
                            Description = "Комментарии:",
                            ControlType="TextBox",
                            Width = 500,
                        },
                    }
                };

                defectEditForm["PCRR_ID"].OnAfterCreate += (control) =>
                {
                    var DefectReason = control as SelectBox;
                    DefectReason.ListBoxMinHeight = 900;
                    DefectReason.Autocomplete = true;
                    DefectReason.CompareMode = SelectBox.CompareModeRef.Contains;

                    FormHelper.ComboBoxInitHelper(control as SelectBox, "Production", "Defect", "ListReason", "ID", "REASON", null, true);
                };

                defectEditForm.OnAfterSave += (id, result) =>
                {
                    DefectsGrid.LoadItems();
                };

                defectEditForm.Show();
            }
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();
        }

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "CorrugatorMachineOperator",
                ReceiverName = "",
                SenderName = "Defects",
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
            if (m.ReceiverGroup.IndexOf("Production") > -1)
            {
                // обновление данных
                if (m.ReceiverName.IndexOf("Defects") > -1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            DefectsGrid.LoadItems();

                            var id = m.Message.ToInt();
                            DefectsGrid.SetSelectedItemId(id, "ID");

                            break;
                    }
                }
            }
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
            }
        }

        /// <summary>
        /// редактирвоание записи
        /// </summary>
        /// <param name="id"></param>
        public void Edit()
        {
            //Id = id;
            Show();
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

            var frameName = GetFrameName();

            Central.WM.Show(frameName, "Причины брака", true, "add", this);
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            var frameName = GetFrameName();
            Central.WM.Close(frameName);
        }

        /// <summary>
        /// формирует уникальный идентификатор фрейма
        /// </summary>
        /// <returns></returns>
        public string GetFrameName()
        {
            string result = "";
            result = $"{FrameName}_{Id}";
            return result;
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DefectsDisableControls()
        {
            EditButton.IsEnabled = false;
            DefectsGrid.IsEnabled = false;
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void DefectsEnableControls()
        {
            EditButton.IsEnabled = DefectsGrid.Items?.Count > 0;
            DefectsGrid.IsEnabled = true;
        }

        private void CloseButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void EditButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DefectEdit(SelectedDefectItem);
        }
    }
}
