using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Интерфейс выбора водителя транспортного средства среди списка ожидаемых водителей
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class DriverListKshExpected : ControlBase
    {
        public DriverListKshExpected()
        {
            ControlTitle = "Список ожидаемых водителей";
            RoleName = "[erp]shipment_control_ksh";
            DocumentationUrl = "/doc/l-pack-erp/shipments/control/listing";
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
                FormInit();
                SetDefaults();
                DriverGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                DriverGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                DriverGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
            };

            {
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
                    Name = "export_to_excel",
                    Group = "main",
                    Enabled = true,
                    Title = "В Excel",
                    Description = "Выгрузить данные в Excel файл",
                    ButtonUse = true,
                    ButtonName = "ExportToExcelButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        DriverGrid.ItemsExportExcel();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "cancel",
                    Group = "main",
                    Enabled = true,
                    Title = "Отмена",
                    Description = "Закрыть без изменений",
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

            Commander.SetCurrentGridName("DriverGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "choice_driver",
                    Title = "Выбрать",
                    Group = "driver_grid_main",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = ChoiceDriverButton,
                    ButtonName = "ChoiceDriverButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        ChoiceDriver();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (DriverGrid != null && DriverGrid.SelectedItem != null && DriverGrid.SelectedItem.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(DriverGrid.SelectedItem.CheckGet("ID")))
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "edit_driver",
                    Title = "Изменить",
                    Group = "driver_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = EditDriverButton,
                    ButtonName = "EditDriverButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        EditDriver();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (DriverGrid != null && DriverGrid.SelectedItem != null && DriverGrid.SelectedItem.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(DriverGrid.SelectedItem.CheckGet("ID")))
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
            }

            Commander.Init(this);
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        private FormHelper Form { get; set; }

        /// <summary>
        /// Датасет с данными грида списка всех водителей
        /// </summary>
        private ListDataSet DriverGridDataSet { get; set; }

        public delegate void ChoiceDriverDelegate(Dictionary<string, string> driverItem);

        public ChoiceDriverDelegate OnChoiceDriver;

        public delegate void CloseDelegate();

        public CloseDelegate OnClose;

        public string ParentFrame { get; set; }

        public int FactoryId = 2;

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        private void FormInit()
        {
            //инициализация формы
            {
                Form = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="SEARCH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=SearchText,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                };

                Form.SetFields(fields);
            }
        }

        private void SetDefaults()
        {
            DriverGridDataSet = new ListDataSet();

            Form.SetDefaults();

            {
                var list = new Dictionary<string, string>();
                list.Add("-1", "Все типы");
                list.Add("-2", "СГП");
                list.Add("0", "Изделия");
                list.Add("2", "Бумага");
                list.Add("5", "СОХ");
                list.Add("7", "Литая тара");
                list.Add("9", "Макулатура");
                ShipmentTypeSelectBox.Items = list;
                ShipmentTypeSelectBox.SelectedItem = list.FirstOrDefault((x) => x.Key == "-1");
            }
        }

        private void DriverGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Description = "Идентификатор водителя",
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Водитель",
                        Description = "ФИО водителя",
                        Path="DRIVERNAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=32,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Автомобиль",
                        Description = "Данные транспортного средства",
                        Path="CAR",
                        ColumnType=ColumnTypeRef.String,
                        Width2=23,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Телефон",
                        Description = "Номер телефона",
                        Path="NOTE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата отгрузки",
                        Description = "Дата и время отгрузки",
                        Path="SHIPMENTDATETIME",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=9,
                        Format="dd.MM HH:mm",
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                (Dictionary<string, string> row) =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    //перенесена на другой день
                                    if (row.CheckGet("UNSHIPPED").ToInt() == 1)
                                    {
                                         color = HColor.Orange;
                                    }

                                    //опоздавшая
                                    if (row.CheckGet("LATE").ToInt() == 1)
                                    {
                                        color = HColor.Yellow;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Покупатель",
                        Description = "Наименование покупателя",
                        Path="BUYER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=32,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вид продукции",
                        Description = "Вид отгружаемой продукции",
                        Path="PRODUCTIONTYPE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид отгрузки",
                        Description = "Идентификатор отгрузки",
                        Path="SHIPMENTID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Ид вида продукции",
                        Description = "Идентификатор вид отгружаемой продукции",
                        Path="PRODUCTIONTYPEID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Перенесённая",
                        Description = "Признак перенесённой отгрузки",
                        Path="UNSHIPPED",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=5,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Опоздавшая",
                        Description = "Признак опоздавшей отгрузки",
                        Path="LATE",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=5,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Марка ТС",
                        Description = "Марка транспортного средства",
                        Path="CARMARK",
                        ColumnType=ColumnTypeRef.String,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер ТС",
                        Description = "Номер транспортного средства",
                        Path="CARNUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Тип отгрузки",
                        Description = "Тип отгрузки по упаковке",
                        Path="SHIPMENTTYPE",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                };
                DriverGrid.SetColumns(columns);
                DriverGrid.SearchText = SearchText;
                DriverGrid.OnLoadItems = DriverGridLoadItems;
                DriverGrid.OnFilterItems = () =>
                {
                    if (DriverGrid.Items != null && DriverGrid.Items.Count > 0)
                    {
                        // Фильтрация по типу отгрузки
                        if (ShipmentTypeSelectBox.SelectedItem.Key != null)
                        {
                            var key = ShipmentTypeSelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все
                                case -1:
                                    items = DriverGrid.Items;
                                    break;

                                // СГП
                                case -2:
                                    items.AddRange(DriverGrid.Items.Where(x => x.CheckGet("PRODUCTIONTYPEID").ToInt() == 0 
                                        || x.CheckGet("PRODUCTIONTYPEID").ToInt() == 1
                                        || x.CheckGet("PRODUCTIONTYPEID").ToInt() == 2
                                        || x.CheckGet("PRODUCTIONTYPEID").ToInt() == 5
                                        || x.CheckGet("PRODUCTIONTYPEID").ToInt() == 9));
                                    break;

                                // Изделия
                                case 0:
                                    items.AddRange(DriverGrid.Items.Where(x => x.CheckGet("PRODUCTIONTYPEID").ToInt() == 0 
                                        || x.CheckGet("PRODUCTIONTYPEID").ToInt() == 1));
                                    break;

                                default:
                                    items.AddRange(DriverGrid.Items.Where(x => x.CheckGet("PRODUCTIONTYPEID").ToInt() == key));
                                    break;
                            }

                            DriverGrid.Items = items;
                        }

                    }
                };
                DriverGrid.SetPrimaryKey("ID");
                DriverGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                DriverGrid.AutoUpdateInterval = 0;
                DriverGrid.ItemsAutoUpdate = false;
                DriverGrid.Toolbar = GridToolbar;
                DriverGrid.Commands = Commander;
                DriverGrid.UseProgressSplashAuto = false;
                DriverGrid.Init();
            }
        }

        private async void DriverGridLoadItems()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SplashControl.Visible = true;
            });

            var p = new Dictionary<string, string>();
            p.Add("ProductionTypeId", $"{ShipmentTypeSelectBox.SelectedItem.Key.ToInt()}");
            p.Add("FACTORY_ID", $"{FactoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "TransportDriver");
            q.Request.SetParam("Action", "ListExpected");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            DriverGridDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    DriverGridDataSet = ListDataSet.Create(result, "Drivers");
                    if (DriverGridDataSet != null && DriverGridDataSet.Items != null && DriverGridDataSet.Items.Count > 0)
                    {
                        foreach (var item in DriverGridDataSet.Items)
                        {
                            item["NOTE"] = DataFormatter.CellPhone(item["NOTE"]);
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
            DriverGrid.UpdateItems(DriverGridDataSet);

            Application.Current.Dispatcher.Invoke(() =>
            {
                SplashControl.Visible = false;
            });
        }

        public void Refresh()
        {
            DriverGrid.LoadItems();
        }

        // FIXME сделать два режима работы фрейма,
        // как самостоятельный фрейм
        // и в комбинации со списокм ожидаемых водителей
        // доработать для этого функции открытия

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
            FrameName = $"{FrameName}";

            Central.WM.Show(FrameName, this.ControlTitle, true, "add", this);
        }

        public void Close()
        {
            if (!string.IsNullOrEmpty(ParentFrame))
            {
                Central.WM.SetActive(ParentFrame, true);
            }

            OnClose?.Invoke();

            Central.WM.Close(FrameName);

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
        }

        private void EditDriver()
        {
            if (DriverGrid.SelectedItem != null && DriverGrid.SelectedItem.CheckGet("ID").ToInt() > 0)
            {
                var h = new Driver();
                h.ReturnTabName = this.FrameName;
                h.OnSave = (Dictionary<string, string> driverData) =>
                {
                    Refresh();
                };
                h.Edit(DriverGrid.SelectedItem.CheckGet("ID").ToInt());
            }
            else
            {
                string msg = $"Не выбран водитель для редактирования.";
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private void ChoiceDriver()
        {
            if (DriverGrid.SelectedItem != null && DriverGrid.SelectedItem.CheckGet("ID").ToInt() > 0)
            {
                OnChoiceDriver?.Invoke(DriverGrid.SelectedItem);
                Close();
            }
            else
            {
                string msg = $"Не выбран водитель для отметки.";
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private void Types_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DriverGrid.UpdateItems();
        }
    }
}
