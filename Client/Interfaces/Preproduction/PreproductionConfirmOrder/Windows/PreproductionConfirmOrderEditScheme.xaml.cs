using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Окно редактирования схемы производства для выбранной позиции выбранной заявки
    /// </summary>
    public partial class PreproductionConfirmOrderEditScheme : ControlBase
    {
        /// <summary>
        /// Конструктор окна редактирования схемы производства для выбранной позиции выбранной заявки.
        /// Обящательные к заполнению поля:
        /// ProductName;
        /// PositionId;
        /// CurrentSchemeName.
        /// </summary>
        public PreproductionConfirmOrderEditScheme()
        {
            ControlTitle = "Выбор схемы производства";
            InitializeComponent();

            OnMessage = (ItemMessage message) => {
                DebugLog($"message=[{message.Message}]");
            };

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                FormInit();
                InitGrid();
                SetDefaults();
                LoadWorkloadData();
                FillNewSchemeSelectBox();
                GetLastUsedScheme();
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
        }

        /// <summary>
        /// Наименование выбранной продукции
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// Идентификатор позиции заявки (orderdates.idorderdates)
        /// </summary>
        public int PositionId { get; set; }

        /// <summary>
        /// Наименование выбранной схемы производства
        /// </summary>
        public string CurrentSchemeName { get; set; }

        /// <summary>
        /// Идентификатор выбранной схемы производства
        /// </summary>
        public int CurrentSchemeId { get; set; }

        /// <summary>
        /// Наименование последней использованной для выбранной продукции схемы производства
        /// </summary>
        public string LastUsedSchemeName { get; set; }

        /// <summary>
        /// Идентификатор последней использованной для выбранной продукции схемы производства
        /// </summary>
        public int LastUsedSchemeId { get; set; }

        /// <summary>
        /// Дата по умолчанию
        /// </summary>
        public string ShipmentDt { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Датасет с данными по загруженности станков
        /// </summary>
        public ListDataSet WorkloadDataSet { get; set; }

        /// <summary>
        /// Инициализация формы
        /// </summary>
        public void FormInit()
        {
            //инициализация формы
            {
                Form = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path = "PRODUCT_NAME",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = ProductNameTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "CURRENT_SCHEME",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = CurrentSchemeTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path="NEW_SCHEME",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=NewSchemeSelectBox,
                        ControlType="SelectBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                };

                Form.SetFields(fields);
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            WorkloadDataSet = new ListDataSet();

            Form.SetValueByPath("PRODUCT_NAME", ProductName);
            Form.SetValueByPath("CURRENT_SCHEME", CurrentSchemeName);
        }

        /// <summary>
        /// Наполняем выпадающий список схем производства схемами, доступными для заданной позиции заявки
        /// </summary>
        public async void FillNewSchemeSelectBox()
        {
            var p = new Dictionary<string, string>();
            p.Add("POSITION_ID", PositionId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "ConfirmOrder");
            q.Request.SetParam("Action", "ListScheme");
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
                    NewSchemeSelectBox.SetItems(ds, "ID", "NAME");

                    if (NewSchemeSelectBox.Items != null && NewSchemeSelectBox.Items.Count > 0)
                    {
                        var item = NewSchemeSelectBox.Items.FirstOrDefault(x => x.Value == CurrentSchemeName).Key;
                        if (item != null && !string.IsNullOrEmpty(item))
                        {
                            NewSchemeSelectBox.SetSelectedItemByKey(item);
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Получаем последнюю использованную схему производства для выбранной продукии
        /// </summary>
        public async void GetLastUsedScheme()
        {
            var p = new Dictionary<string, string>();
            p.Add("POSITION_ID", PositionId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "ConfirmOrder");
            q.Request.SetParam("Action", "GetLastUsedScheme");
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
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        LastUsedSchemeId = ds.Items.First().CheckGet("SCHEME_ID").ToInt();
                        LastUsedSchemeName = ds.Items.First().CheckGet("SCHEME_NAME");
                    }
                }
            }

            if (LastUsedSchemeId > 0)
            {
                LastSchemeTextBox.Text = LastUsedSchemeName;
                LastSchemeTextBoxLabelBorder.Visibility = Visibility.Visible;
                LastSchemeTextBoxBorder.Visibility = Visibility.Visible;
                LastSchemeWorkloadGridLabelBorder.Visibility = Visibility.Visible;
                LastSchemeWorkloadGridBorder.Visibility = Visibility.Visible;

                //this.Height = 330;
                //this.Width = 800;
            }
            else
            {
                //this.Height = 260;
                //this.Width = 800;
            }
        }

        /// <summary>
        /// Инициализация всех гридов формы
        /// </summary>
        public void InitGrid()
        {
            // CurrentSchemeWorkloadGrid
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="SCHEME_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=30,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Схема",
                        Path="SCHEME_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width=80,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Станок",
                        Path="MACHINE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Принято, ч.",
                        Path="WORKLOAD",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Позиция",
                        Path="POSITION_WORKLOAD",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width=70,
                    },

                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=2,
                        MaxWidth=2000,
                    },
                };
                CurrentSchemeWorkloadGrid.SetColumns(columns);
                CurrentSchemeWorkloadGrid.PrimaryKey = "SCHEME_ID";
                CurrentSchemeWorkloadGrid.UseSorting = false;
                //CurrentSchemeWorkloadGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

                CurrentSchemeWorkloadGrid.Init();
            }

            // NewSchemeWorkloadGrid
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="SCHEME_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=30,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Схема",
                        Path="SCHEME_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width=80,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Станок",
                        Path="MACHINE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Принято, ч.",
                        Path="WORKLOAD",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Позиция",
                        Path="POSITION_WORKLOAD",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width=70,
                    },

                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=2,
                        MaxWidth=2000,
                    },
                };
                NewSchemeWorkloadGrid.SetColumns(columns);
                NewSchemeWorkloadGrid.PrimaryKey = "SCHEME_ID";
                NewSchemeWorkloadGrid.UseSorting = false;
                //NewSchemeSelectBox.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

                NewSchemeWorkloadGrid.Init();
            }

            // LastSchemeWorkloadGrid
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="SCHEME_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=30,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Схема",
                        Path="SCHEME_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width=80,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Станок",
                        Path="MACHINE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Принято, ч.",
                        Path="WORKLOAD",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Позиция",
                        Path="POSITION_WORKLOAD",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width=70,
                    },

                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=2,
                        MaxWidth=2000,
                    },
                };
                LastSchemeWorkloadGrid.SetColumns(columns);
                LastSchemeWorkloadGrid.PrimaryKey = "SCHEME_ID";
                LastSchemeWorkloadGrid.UseSorting = false;
                //LastSchemeWorkloadGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

                LastSchemeWorkloadGrid.Init();
            }
        }

        /// <summary>
        /// Получаем данные по загруженности станков
        /// </summary>
        public void LoadWorkloadData()
        {
            DisableControls();

            var p = new Dictionary<string, string>();
            p.Add("POSITION_ID", PositionId.ToString());
            p.Add("DT", ShipmentDt);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "ConfirmOrder");
            q.Request.SetParam("Action", "GetMachineWorkloadByOrderPosition");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;
            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    WorkloadDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }

            EnableControls();
        }

        /// <summary>
        /// Заполняем гриды загруженности станков в зависимости от данных по схемам производства
        /// </summary>
        public void SetWorkLoadData()
        {
            if (WorkloadDataSet != null && WorkloadDataSet.Items != null && WorkloadDataSet.Items.Count > 0)
            {
                if (CurrentSchemeId > 0)
                {
                    ListDataSet currentSchemeWorkloadDataSet = new ListDataSet();
                    currentSchemeWorkloadDataSet.Items = WorkloadDataSet.Items.Where(x => x.CheckGet("SCHEME_ID").ToInt() == CurrentSchemeId).ToList();
                    CurrentSchemeWorkloadGrid.UpdateItems(currentSchemeWorkloadDataSet);
                }

                if (NewSchemeSelectBox.SelectedItem.Key != null && NewSchemeSelectBox.SelectedItem.Key.ToInt() > 0)
                {
                    ListDataSet newSchemeWorkloadDataSet = new ListDataSet();
                    newSchemeWorkloadDataSet.Items = WorkloadDataSet.Items.Where(x => x.CheckGet("SCHEME_ID").ToInt() == NewSchemeSelectBox.SelectedItem.Key.ToInt()).ToList();
                    NewSchemeWorkloadGrid.UpdateItems(newSchemeWorkloadDataSet);
                }

                if (LastUsedSchemeId > 0)
                {
                    ListDataSet lastSchemeWorkloadDataSet = new ListDataSet();
                    lastSchemeWorkloadDataSet.Items = WorkloadDataSet.Items.Where(x => x.CheckGet("SCHEME_ID").ToInt() == LastUsedSchemeId).ToList();
                    LastSchemeWorkloadGrid.UpdateItems(lastSchemeWorkloadDataSet);
                }
            }
        }

        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
        }

        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
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
            Central.WM.FrameMode = 2;
            var frameName = $"{ControlName}_{PositionId}";
            Central.WM.Show(frameName, $"Схема производства для {PositionId}", true, "main", this);
        }

        public void ProcessCommand(string command)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "help":
                        {
                            Central.ShowHelp("/doc/l-pack-erp/preproduction/preproduction_confirm_order/");
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Сохранение новой схемы производства для выбранной позиции заявки
        /// </summary>
        public async void Save()
        {
            var selectedScheme = NewSchemeSelectBox.SelectedItem;

            // Если изменили схему производства
            if (selectedScheme.Value != Form.GetValueByPath("CURRENT_SCHEME"))
            {
                DisableControls();

                var p = new Dictionary<string, string>();
                p.Add("POSITION_ID", PositionId.ToString());
                p.Add("ID_TLS", selectedScheme.Key);

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "ConfirmOrder");
                q.Request.SetParam("Action", "UpdateScheme");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;
                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    bool succesfullFlag = false;

                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(ds.Items.First().CheckGet("POSITION_ID")))
                            {
                                succesfullFlag = true;

                                // Отправляем сообщение табу список заявок на гофропроизводство для подтверждения о необходимости обновить грид
                                {
                                    Central.Msg.SendMessage(new ItemMessage()
                                    {
                                        ReceiverGroup = "Preproduction",
                                        ReceiverName = "PreproductionConfirmOrderList",
                                        SenderName = "PreproductionConfirmOrder",
                                        Action = "Refresh",
                                    });
                                }

                                Close();
                            }
                        }
                    }

                    if (!succesfullFlag)
                    {
                        string msg = $"При изменении схемы произошла ошибка. Пожалуйста, сообщите об ошибке.";
                        var d = new DialogWindow($"{msg}", $"Схема производства для {PositionId}", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }

                EnableControls();
            }
            else
            {
                Close();
            }
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            var frameName = $"{ControlName}_{PositionId}";
            Central.WM.Close(frameName);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessCommand("help");
        }

        private void NewSchemeSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SetWorkLoadData();
        }

        private void LastSchemeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetWorkLoadData();
        }
    }
}
