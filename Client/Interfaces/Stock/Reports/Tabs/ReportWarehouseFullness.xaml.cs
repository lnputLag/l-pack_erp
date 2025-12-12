using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
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

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Отчет загрузки складской зоны
    /// </summary>
    public partial class ReportWarehouseFullness : ControlBase
    {
        public ReportWarehouseFullness()
        {
            InitializeComponent();
            ControlTitle = "Загрузка складской зоны";
            RoleName = "[erp]warehouse_report";
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
                FormInit();
                SetDefaults();
                GridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                Grid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                Grid.ItemsAutoUpdate = true;
                Grid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                Grid.ItemsAutoUpdate = false;
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
                    ButtonControl = HelpButton,
                    ButtonName = "HelpButton",
                    HotKey = "F1",
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
            }

            Commander.SetCurrentGridName("Grid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "export_to_excel",
                    Title = "В Excel",
                    Description = "Экспортировать в Excel",
                    Group = "grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = ExportToExcelButton,
                    ButtonName = "ExportToExcelButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Grid.ItemsExportExcel();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (Grid != null && Grid.Items != null && Grid.Items.Count > 0)
                        {
                            result = true;
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
        public FormHelper Form { get; set; }

        /// <summary>
        /// Датасет с данными грида приходных накладных
        /// </summary>
        private ListDataSet GridDataSet { get; set; }

        public int FactoryId = 1;

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
                    new FormHelperField()
                    {
                        Path="ZONE",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=ZoneSelectBox,
                        ControlType="SelectBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                };

                Form.SetFields(fields);
            }
        }


        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            GridDataSet = new ListDataSet();

            FormHelper.ComboBoxInitHelper(ZoneSelectBox, "Warehouse", "Zone", "ListByFactory", "WMZO_ID", "ZONE_FULL_NAME", new Dictionary<string, string>() { { "FACTORY_ID", $"{FactoryId}"} }, true);
            ZoneSelectBox.SetSelectedItemByKey("1");
        }

        private void GridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид ряда",
                        Description = "Идентификатор ряда",
                        Path="WMRO_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ряд",
                        Description = "Наименование ряда",
                        Path="ROW_NUM",
                        ColumnType=ColumnTypeRef.String,
                        Width2=6,
                        TotalsType=TotalsTypeRef.Count,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Свободно",
                        Description = "Количество свободных ячеек",
                        Path="FREE_CNT",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=14,
                        Format="N0",
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Занято",
                        Description = "Количество занятых ячеек",
                        Path="OCCUPIED_CNT",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=14,
                        Format="N0",
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Частично занято",
                        Description = "Количество частично занятых ячеек",
                        Path="PARTIALLY_OCCUPIED_CNT",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=14,
                        Format="N0",
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Заблокировано",
                        Description = "Количество заблокированных ячеек",
                        Path="BLOKED_CNT",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=14,
                        Format="N0",
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Забронированно",
                        Description = "Количество забронированных ячеек",
                        Path="BOOKED_CNT",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=14,
                        Format="N0",
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Недоступно",
                        Description = "Количество недоступных ячеек",
                        Path="UNAVAILABLE_CNT",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=14,
                        Format="N0",
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Всего",
                        Description = "Общее количество ячеек в ряду",
                        Path="ALL_CNT",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=14,
                        Format="N0",
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Продукция",
                        Description = "Признак того, что в ряду установлена продукция",
                        Path="ITEM_IS",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=9,
                        Totals = (List<Dictionary<string,string>> rows) =>
                        {
                            int result = 0;
                            if (rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    result += row.CheckGet("ITEM_IS").ToInt();
                                }
                            }
                            return result;
                        },
                    },
                };
                Grid.SetColumns(columns);
                Grid.SearchText = SearchText;
                Grid.OnLoadItems = GridLoadItems;
                Grid.SetPrimaryKey("WMRO_ID");
                Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                Grid.AutoUpdateInterval = 5 * 60;
                Grid.Toolbar = GridToolbar;
                Grid.Commands = Commander;
                Grid.UseProgressSplashAuto = false;
                Grid.Init();
            }
        }

        private async void GridLoadItems()
        {
            bool resume = false;

            int zone = Form.GetValueByPath("ZONE").ToInt();

            if (zone > 0)
            {
                resume = true;
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                p.Add("WMZO_ID", $"{zone}");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Row");
                q.Request.SetParam("Action", "ListByZoneRack");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                GridDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        GridDataSet = ListDataSet.Create(result, "ITEMS");
                    }
                }
                else
                {
                    q.ProcessError();
                }
                Grid.UpdateItems(GridDataSet);
            }
        }

        public void Refresh()
        {
            Grid.LoadItems();
        }

        private void ZoneSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Refresh();
        }
    }
}
