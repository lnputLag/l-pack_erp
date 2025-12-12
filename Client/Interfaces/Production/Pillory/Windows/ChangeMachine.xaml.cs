using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Utils.About;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
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

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Окно смены станка для производственного задания
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class ChangeMachine : ControlBase
    {
        public ChangeMachine()
        {
            ControlTitle = "Выбор станка";
            RoleName = "[erp]pillory";
            DocumentationUrl = "/doc/l-pack-erp/";
            InitializeComponent();

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (KeyEventArgs e) =>
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
                Commander.Init(this);

                FormInit();
                MachineGridInit();
                SetDefaults();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                MachineGrid?.Destruct();
            };

            OnFocusGot = () =>
            {
                MachineGrid.Run();
            };

            OnFocusLost = () =>
            {
            };

            {
                Commander.Add(new CommandItem()
                {
                    Name = "save",
                    Group = "main",
                    Enabled = true,
                    Title = "",
                    Description = "Сохранить",
                    ButtonUse = true,
                    ButtonControl = SaveButton,
                    ButtonName = "SaveButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        Save();
                    },
                    CheckEnabled = () =>
                    {
                        bool resume = false;
                        if (MachineGrid != null && MachineGrid.Items != null && MachineGrid.Items.Count > 1)
                        {
                            if (MachineGrid.SelectedItem != null && MachineGrid.SelectedItem.Count > 0)
                            {
                                if (MachineGrid.SelectedItem.CheckGet("SCHEME_ID").ToInt() != MachineGrid.SelectedItem.CheckGet("CURRENT_SCHEME_ID").ToInt())
                                {
                                    resume = true;
                                }
                            }
                        }

                        return resume;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "refresh",
                    Group = "main",
                    Enabled = true,
                    Title = "Обновить",
                    Description = "Обновить данные",
                    ButtonUse = true,
                    ButtonControl = RefreshButton,
                    ButtonName = "RefreshButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Refresh();
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
                Commander.Add(new CommandItem()
                {
                    Name = "cancel",
                    Group = "main",
                    Enabled = true,
                    Title = "",
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
            }
        }

        /// <summary>
        /// Наименование таба, который вызвал этот таб
        /// </summary>
        public string ParentFrame { get; set; }

        /// <summary>
        /// Наименование продукции по ПЗ
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// Ид площадки
        /// </summary>
        public int FactoryId { get; set; }

        /// <summary>
        /// idorderdates
        /// Ид позиции заявки
        /// </summary>
        public int OrderPositionId { get; set; }

        /// <summary>
        /// proiz_zad.id_pz
        /// Ид ПЗ ГА
        /// </summary>
        public int BlankProductionTaskId { get; set; }

        /// <summary>
        /// tovar.id2
        /// ИД заготовки
        /// </summary>
        public int BlankProductId { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        private FormHelper Form { get; set; }

        private ListDataSet MachineGridDataSet { get; set; }

        public void Refresh()
        {
            MachineGrid.LoadItems();
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

            FrameName = $"{FrameName}";
            Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
            windowParametrs.Add("no_resize", "1");
            windowParametrs.Add("center_screen", "1");
            this.MinHeight = 650;
            this.MinWidth = 750;
            Central.WM.Show(FrameName, this.ControlTitle, true, "main", this, "top", windowParametrs);
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        private void Close()
        {
            if (!string.IsNullOrEmpty(ParentFrame))
            {
                Central.WM.SetActive(ParentFrame, true);
            }

            Central.WM.Close(FrameName);

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
        }

        /// <summary>
        /// инициализация компонентов формы
        /// </summary>
        private void FormInit()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="PRODUCT_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductNameTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };
            Form.SetFields(fields);
        }

        private void SetDefaults()
        {
            bool resume = true;
            string msg = "";

            // Проверяем, что передали параметры для работы интерфейса
            if (resume)
            {
                if (!(OrderPositionId != 0 || (BlankProductionTaskId != 0 && BlankProductId != 0)))
                {
                    resume = false;
                    msg = "Нет данных по заявке или производственному заданию на ГА. Пожалуйста, сообщите о проблеме.";
                }
            }

            // Проверяем, что в рабочей очереди станков переработки нет заданий по той позиции заявки
            if (resume)
            {
                int productionTaskCount = GetWorkQueueCountByOrderPosition();
                if (productionTaskCount > 0)
                {
                    resume = false;
                    msg = "Задание в рабочей очереди станка. Невозможно переместить.";

                }
                else if (productionTaskCount < 0)
                {
                    resume = false;
                    msg = "Ошибка получения данных по рабочей очереди. Пожалуйста, повторите операцию.";
                }
            }

            if (resume)
            {
                Form.SetValueByPath("PRODUCT_NAME", ProductName);
                Refresh();
            }
            else
            {
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();

                Close();
            }
        }

        private int GetWorkQueueCountByOrderPosition()
        {
            int workQueueCount = -1;

            var p = new Dictionary<string, string>();
            p.Add("ORDER_POSITION_ID", $"{OrderPositionId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production/Pillory");
            q.Request.SetParam("Object", "ProductionTask");
            q.Request.SetParam("Action", "GetWorkQueueCountByOrderPosition");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        workQueueCount = ds.Items[0].CheckGet("CNT").ToInt();
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            return workQueueCount;
        }

        private void MachineGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Description = "Идентификатор схемы производства",
                        Path="SCHEME_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Схема",
                        Description = "Наименование схемы производства",
                        Path="SCHEME_TYPE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=21,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Станок",
                        Description = "Последний станок в схеме производства",
                        Path="MACHINE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Человек",
                        Description = "Количество персонала необходимое для производства продукции",
                        Path="PEOPLE_QUANTITY",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=5,
                        Format="N0",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Главная",
                        Description = "Признак главной схемы производства для этого изделия",
                        Path="DEFAULT_SCHEME_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Второстепенная",
                        Description = "Признак второстепенной схемы производства для этого изделия",
                        Path="SECONDARY_SCHEME_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Стат. скорость",
                        Description = "Признак статистической скорости",
                        Path="STATISTICAL_SPEED_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=7,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Ид типа схемы",
                        Description = "Идентификатор типа схемы производства",
                        Path="SCHEME_TYPE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид станка",
                        Description = "Идентификатор последнего станка в схеме производства",
                        Path="MACHINE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид продукции",
                        Description = "Идентификатор продукции",
                        Path="PRODUCT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид позиции заявки",
                        Description = "Идентификатор позиции заявки",
                        Path="ORDER_POSITION_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид текущей схемы",
                        Description = "Идентификатор текущей схемы производства",
                        Path="CURRENT_SCHEME_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид текущего ПЗ",
                        Description = "Идентификатор текущего производственного задания на переработку",
                        Path="CURRENT_PRODUCTION_TASK_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Список станков",
                        Description = "Список идентификаторов станков переработки, учавствующих в этой схеме производства",
                        Path="MACHINE_ID_LIST",
                        ColumnType=ColumnTypeRef.String,
                        Width2=5,
                        Hidden= !Central.DebugMode,
                    }
                };
                MachineGrid.SetColumns(columns);
                MachineGrid.SetPrimaryKey("SCHEME_ID");
                MachineGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                MachineGrid.AutoUpdateInterval = 0;
                MachineGrid.Toolbar = MachineGridToolbar;
                MachineGrid.SearchText = SearchText;
                MachineGrid.OnLoadItems = MachineGridLoadItems;
                MachineGrid.UseProgressSplashAuto = false;
                MachineGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    // определение цветов фона строк
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            // Выбрана таже схемы, что сейчас используется
                            if (row.CheckGet("SCHEME_ID").ToInt() == row.CheckGet("CURRENT_SCHEME_ID").ToInt())
                            {
                                color = HColor.Gray;
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };
                MachineGrid.Commands = this.Commander;
                MachineGrid.Init();
            }
        }

        private async void MachineGridLoadItems()
        {
            EnableSplash();

            var p = new Dictionary<string, string>();
            p.Add("ORDER_POSITION_ID", $"{OrderPositionId}");
            p.Add("PRODUCTION_TASK_ID", $"{BlankProductionTaskId}");
            p.Add("PRODUCT_ID", $"{BlankProductId}");
            p.Add("FACTORY_ID", $"{this.FactoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production/Pillory");
            q.Request.SetParam("Object", "Machine");
            q.Request.SetParam("Action", "ListForChangeByOrderPositionId");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            MachineGridDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    MachineGridDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            else
            {
                q.ProcessError();
            }
            MachineGrid.UpdateItems(MachineGridDataSet);

            DisableSplash();
        }

        private void Save()
        {
            string machineIdOldList = MachineGridDataSet.Items.FirstOrDefault(x => x.CheckGet("SCHEME_ID").ToInt() == x.CheckGet("CURRENT_SCHEME_ID").ToInt())?.CheckGet("MACHINE_ID_LIST");
            string machineIdNewList = MachineGrid.SelectedItem.CheckGet("MACHINE_ID_LIST");

            var p = new Dictionary<string, string>();
            p.Add("PRODUCTION_TASK_ID", MachineGrid.SelectedItem.CheckGet("CURRENT_PRODUCTION_TASK_ID"));
            p.Add("PRODUCT_ID", MachineGrid.SelectedItem.CheckGet("PRODUCT_ID"));
            p.Add("SCHEME_ID", MachineGrid.SelectedItem.CheckGet("SCHEME_ID"));

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production/Pillory");
            q.Request.SetParam("Object", "ProductionTask");
            q.Request.SetParam("Action", "ChangeMachine");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                bool succesfullFLag = false;

                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        if (ds.Items[0].CheckGet("SCHEME_ID").ToInt() > 0)
                        {
                            succesfullFLag = true;
                        }
                    }
                }

                if (succesfullFLag)
                {
                    {
                        ChangeMachineDataStruct changeMachineDataStruct = new ChangeMachineDataStruct();
                        if (!string.IsNullOrEmpty(machineIdNewList))
                        {
                            changeMachineDataStruct.MachineIdNewList = machineIdNewList.Split(';').ToList();
                        }
                        if (!string.IsNullOrEmpty(machineIdOldList))
                        {
                            changeMachineDataStruct.MachineIdOldList = machineIdOldList.Split(';').ToList();
                        }
                        changeMachineDataStruct.OrderPositionId = this.OrderPositionId;
                        changeMachineDataStruct.BlankProductId = this.BlankProductId;
                        changeMachineDataStruct.BlankProductionTaskId = this.BlankProductionTaskId;

                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "Pillory",
                            ReceiverName = ParentFrame,
                            SenderName =this.FrameName ,
                            Action = "change_machine_refresh",
                            ContextObject = changeMachineDataStruct
                        });
                    }

                    string msg = "Успешное перемещение задания на другой станок.";
                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                    d.ShowDialog();

                    Close();
                }
                else
                {
                    string msg = "При перемещении задания на другой станок произошла ошибка. Пожалуйста, сообщие о проблеме.";
                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        private void EnableSplash()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SplashControl.Visible = true;
            });
        }

        private void DisableSplash()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SplashControl.Visible = false;
            });
        }
    }

    public class ChangeMachineDataStruct
    {
        /// <summary>
        /// idorderdates
        /// Ид позиции заявки
        /// </summary>
        public int OrderPositionId { get; set; }

        /// <summary>
        /// proiz_zad.id_pz
        /// Ид ПЗ ГА
        /// </summary>
        public int BlankProductionTaskId { get; set; }

        /// <summary>
        /// tovar.id2
        /// ИД заготовки
        /// </summary>
        public int BlankProductId { get; set; }

        /// <summary>
        /// Список идентификаторов станков, которые использовались в текущей схеме проихводства
        /// </summary>
        public List<string> MachineIdOldList { get; set; }

        /// <summary>
        /// Список идентификаторов станков, которые используются в новой выбранной схеме проихводства
        /// </summary>
        public List<string> MachineIdNewList { get; set; }
    }
}
