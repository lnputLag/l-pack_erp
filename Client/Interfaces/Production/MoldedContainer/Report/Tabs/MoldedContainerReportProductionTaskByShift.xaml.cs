using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using SixLabors.ImageSharp.Formats;
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

namespace Client.Interfaces.Production.MoldedContainer.Report.Tabs
{
    /// <summary>
    /// Отчёт по производственным заданиям на литую тару в разрезе смен
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class MoldedContainerReportProductionTaskByShift : ControlBase
    {
        public MoldedContainerReportProductionTaskByShift()
        {
            InitializeComponent();

            ControlSection = "report_order";
            ControlTitle = "Отчет по сменам";
            DocumentationUrl = "";

            OnLoad = () =>
            {
                FormInit();
                SetDefaults();
                GridInit();
            };

            OnUnload = () =>
            {
                Grid.Destruct();
            };

            OnFocusGot = () =>
            {
                Grid.ItemsAutoUpdate = true;
            };

            OnFocusLost = () =>
            {
                Grid.ItemsAutoUpdate = false;
            };

            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "help",
                        Enabled = true,
                        Title = "Справка",
                        Description = "Показать справочную информацию",
                        ButtonUse = true,
                        ButtonName = "HelpButton",
                        HotKey = "F1",
                        Action = () => { Central.ShowHelp(DocumentationUrl); },
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "show",
                        Enabled = true,
                        Title = "Показать",
                        Description = "Показать отчет",
                        ButtonUse = true,
                        MenuEnabled = true,
                        ButtonName = "ShowButton",
                        Action = () => 
                        {
                            Grid.LoadItems();
                            ShowButton.Style = (System.Windows.Style)ShowButton.TryFindResource("Button");
                        }
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "export_to_excel",
                        Enabled = true,
                        Title = "Экспорт в Excel",
                        Description = "Экспортировать отчет в Excel",
                        ButtonUse = true,
                        MenuEnabled = true,
                        ButtonName = "ExcelButton",
                        Action = () =>
                        {
                            Grid.ItemsExportExcel();
                        }
                    });
                }
            }

            Commander.Init(this);
        }

        private FormHelper Form { get; set; }
        private ListDataSet GridItems { get; set; }

        public void FormInit()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="FROM_DATE",
                    FieldType=FormHelperField.FieldTypeRef.DateTime,
                    Default = DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy"),
                    Control= FromDate,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        ShowButton.Style = (System.Windows.Style)ShowButton.TryFindResource("FButtonPrimary");
                    }
                },
                new FormHelperField()
                {
                    Path="FROM_TIME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control= FromTimeSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        ShowButton.Style = (System.Windows.Style)ShowButton.TryFindResource("FButtonPrimary");
                    }
                },
                new FormHelperField()
                {
                    Path="TO_DATE",
                    FieldType=FormHelperField.FieldTypeRef.DateTime,
                    Default = DateTime.Now.ToString("dd.MM.yyyy"),
                    Control= ToDate,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        ShowButton.Style = (System.Windows.Style)ShowButton.TryFindResource("FButtonPrimary");
                    }
                },
                new FormHelperField()
                {
                    Path="TO_TIME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control= ToTimeSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        ShowButton.Style = (System.Windows.Style)ShowButton.TryFindResource("FButtonPrimary");
                    }
                },
            };
            Form.SetFields(fields);
            Form.SetDefaults();
        }

        public void SetDefaults()
        {
            Form.SetDefaults();

            {
                var list = new Dictionary<string, string>();
                list.Add("08", "08:00");
                list.Add("20", "20:00");

                FromTimeSelectBox.Items = list;
                FromTimeSelectBox.SetSelectedItemByKey("08");
            }

            {
                var list = new Dictionary<string, string>();
                list.Add("08", "08:00");
                list.Add("20", "20:00");

                ToTimeSelectBox.Items = list;
                ToTimeSelectBox.SetSelectedItemByKey("20");
            }

            {
                var list = new Dictionary<string, string>();
                list.Add("-1", "Все станки");
                list.Add("311", "Принтер BST[СPH-3430]-1");
                list.Add("321", "Этикетир. BST[TBH-2438]-1");
                list.Add("312", "Принтер AAEI [301 P]");
                list.Add("322", "Этикетир AAEI [301 L]");
                list.Add("303", "Пресс BST[ЕС9600]-1 A");
                list.Add("304", "Пресс BST[ЕС9600]-1 B");
                list.Add("301", "Пресс BST[ЕС9600]-2 A");
                list.Add("302", "Пресс BST[ЕС9600]-2 B");
                
                MachineSelectBox.Items = list;
                MachineSelectBox.SetSelectedItemByKey("-1");
            }

            {
                var list = new Dictionary<string, string>();
                list.Add("-1", "Все бригады");
                list.Add("1", "Бр-1");
                list.Add("2", "Бр-2");
                list.Add("3", "Бр-3");
                list.Add("4", "Бр-4");

                ShiftSelectBox.Items = list;
                ShiftSelectBox.SetSelectedItemByKey("-1");
            }
        }

        private void GridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    Description="Порядковый номер записи",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="ИД ПЗ",
                    Path="PRODUCTION_TASK_ID",
                    Description="Идентификатор производственного задания",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Номер",
                    Path="PRODUCTION_TASK_NUBMER",
                    Description="Номер производственного задания",
                    ColumnType=ColumnTypeRef.String,
                    Width2=9,
                },
                new DataGridHelperColumn
                {
                    Header="Смена",
                    Path="SHIFT_FULL_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=13,
                },
                new DataGridHelperColumn
                {
                    Header="Станок",
                    Path="MACHINE_NAME",
                    Description="Станок выполнения производственного задания",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="PRODUCT_CODE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Изделие",
                    Path="PRODUCT_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=42,
                },
                new DataGridHelperColumn
                {
                    Header="Оприходовано, шт",
                    Path="INCOMING_QUANTITY_BY_SHIFT",
                    Description="Количество оприходованной продукции по этому заданию за смену",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=14,
                    Totals = (List<Dictionary<string,string>> rows) =>
                    {
                        int result = 0;

                        if (rows != null)
                        {
                            foreach(Dictionary<string,string> row in rows)
                            {
                                result += row["INCOMING_QUANTITY_BY_SHIFT"].ToInt();
                            }
                        }

                        return $"{result}";
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Списано, шт",
                    Path="CONSUMPTION_QUANTITY_BY_SHIFT",
                    Description="Количество списанной продукции по этому заданию за смену",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=14,
                    Totals = (List<Dictionary<string,string>> rows) =>
                    {
                        int result = 0;

                        if (rows != null)
                        {
                            foreach(Dictionary<string,string> row in rows)
                            {
                                result += row["CONSUMPTION_QUANTITY_BY_SHIFT"].ToInt();
                            }
                        }

                        return $"{result}";
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Заявка",
                    Path="ORDER_NAME",
                    Description="Заявка, по которой производится эта продукция",
                    ColumnType=ColumnTypeRef.String,
                    Width2=32,
                },
                new DataGridHelperColumn
                {
                    Header="Бригада",
                    Path="SHIFT_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=7,
                },

                new DataGridHelperColumn
                {
                    Header="№ смены",
                    Path="SHIFT_NUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Ид станка",
                    Path="MACHINE_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Ид категории продукции",
                    Path="PRODUCT_KATEGORY_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                    Visible=false,
                },
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("_ROWNUMBER");
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.SearchText = Search;
            Grid.OnLoadItems = LoadItems;
            Grid.OnFilterItems = () =>
            {
                if (Grid.Items != null && Grid.Items.Count > 0)
                {
                    // Фильтрация по станку
                    // -1 -- Все станки
                    if (MachineSelectBox.SelectedItem.Key != null)
                    {
                        var key = MachineSelectBox.SelectedItem.Key.ToInt();
                        var items = new List<Dictionary<string, string>>();

                        switch (key)
                        {
                            // Все станки
                            case -1:
                                items = Grid.Items;
                                break;

                            default:
                                items.AddRange(Grid.Items.Where(x => x.CheckGet("MACHINE_ID").ToInt() == key));
                                break;
                        }

                        Grid.Items = items;
                    }

                    // Фильтрация по бригаде
                    // -1 -- Все бригады
                    if (ShiftSelectBox.SelectedItem.Key != null)
                    {
                        var key = ShiftSelectBox.SelectedItem.Key.ToInt();
                        var items = new List<Dictionary<string, string>>();

                        switch (key)
                        {
                            // Все бригады
                            case -1:
                                items = Grid.Items;
                                break;

                            default:
                                items.AddRange(Grid.Items.Where(x => x.CheckGet("SHIFT_NUMBER").ToInt() == key));
                                break;
                        }

                        Grid.Items = items;
                    }
                }
            };

            Grid.Commands = Commander;
            Grid.Init();
        }

        private async void LoadItems()
        {
            if (Form.Validate())
            {
                var v = Form.GetValues();

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "MoldedContainer");
                q.Request.SetParam("Object", "Report");
                q.Request.SetParam("Action", "ReportListTaskByShift");

                q.Request.SetParam("FROM_DTTM", $"{v.CheckGet("FROM_DATE")} {v.CheckGet("FROM_TIME")}:00:00");
                q.Request.SetParam("TO_DTTM", $"{v.CheckGet("TO_DATE")} {v.CheckGet("TO_TIME")}:00:00");

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                GridItems = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        GridItems = ListDataSet.Create(result, "ITEMS");
                    }
                }
                Grid.UpdateItems(GridItems);
            }
        }

        private void MachineSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void ProductTypeSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void ShiftSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }
    }
}
