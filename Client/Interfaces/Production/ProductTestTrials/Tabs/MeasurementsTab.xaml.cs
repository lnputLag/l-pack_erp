using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.ProductTestTrials
{
    /// <summary>
    /// Логика взаимодействия для Measurements.xaml
    /// </summary>
    public partial class MeasurementsTab : ControlBase
    {
        public MeasurementsTab()
        {
            RoleName = "[erp]production_testing_trial";
            ControlTitle = "Измерения";

            InitializeComponent();

            SetDefaultValues();

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == "GridFirst")
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnLoad = () =>
            {
                GridInit();
                GridSecondInit();
                GridThreeInit();
                FormInit();
            };

            OnUnload = () =>
            {
                GridFirst.Destruct();
                GridSecond.Destruct();
                GridThree.Destruct();
            };

            OnFocusGot = () =>
            {
                GridFirst.ItemsAutoUpdate = true;
                GridSecond.ItemsAutoUpdate = true;
                GridThree.ItemsAutoUpdate = true;
            };

            OnFocusLost = () =>
            {
                GridFirst.ItemsAutoUpdate = false;
                GridSecond.ItemsAutoUpdate = false;
                GridThree.ItemsAutoUpdate = false;
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
                        AccessLevel = Role.AccessMode.ReadOnly,
                        HotKey = "F1",
                        Action = () => { Central.ShowHelp(DocumentationUrl); },
                    });
                }

                Commander.SetCurrentGridName("GridFirst");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "grid_first_refresh",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "RefreshGridFirst",
                        AccessLevel = Role.AccessMode.ReadOnly,
                        MenuUse = true,
                        Action = GridFirst.LoadItems
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "grid_first_export_to_excel",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "В Excel",
                        Description = "Экспорт данных в excel",
                        ButtonUse = true,
                        ButtonName = "ExportToExcel",
                        AccessLevel = Role.AccessMode.ReadOnly,
                        MenuUse = true,
                        Action = GridFirst.ItemsExportExcel
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "grid_first_show_tech_map",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Показать ТК",
                        Description = "Показать тех.карту",
                        ButtonUse = true,
                        ButtonName = "ShowTechMap",
                        AccessLevel = Role.AccessMode.ReadOnly,
                        MenuUse = true,
                        Action = ShowTechnologyMap
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "grid_first_edit_note",
                        Group = "grid_tools",
                        Enabled = true,
                        Title = "Изменить",
                        Description = "Изменить запись",
                        ButtonUse = true,
                        ButtonName = "EditButton",
                        AccessLevel = Role.AccessMode.FullAccess,
                        MenuUse = true,
                        Action = () =>
                        {
                            var testEdit = new MeasurementsEditFrame();
                            testEdit.Edit(IdTest);
                        }
                    });
                    
                    Commander.Add(new CommandItem()
                    {
                        Name = "grid_first_delete",
                        Group = "grid_tools",
                        Enabled = true,
                        Title = "Удалить",
                        Description = "Удалить запись",
                        ButtonUse = true,
                        ButtonName = "DeleteButton",
                        AccessLevel = Role.AccessMode.FullAccess,
                        MenuUse = true,
                        Action = DeleteMeasurementsShowWindow
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "grid_first_upload_new_measurements",
                        Group = "grid_tools",
                        Enabled = true,
                        Title = "Загрузить",
                        Description = "Загрузить новые измерения",
                        ButtonUse = true,
                        ButtonName = "UploadButton",
                        AccessLevel = Role.AccessMode.FullAccess,
                        MenuUse = true,
                        Action = async () =>
                        {
                            HourCorrection = 0;
                            await ParseNewMeasurements();
                        }
                    });
                    
                    Commander.Add(new CommandItem()
                    {
                        Name = "grid_first_upload_again_measurements",
                        Group = "grid_tools",
                        Enabled = true,
                        Title = "Повторная загрузка",
                        Description = "Повторная загрузка/проверка данных",
                        ButtonUse = true,
                        ButtonName = "UploadAgainButton",
                        AccessLevel = Role.AccessMode.FullAccess,
                        MenuUse = true,
                        Action = async () =>
                        {
                            var dialoge = new DialogWindow("Вы хотите загрузить данные повторно?", "Загрузка данных", " ",
                                DialogWindowButtons.YesNo);
                            dialoge.ShowDialog();
                            
                            if (dialoge.DialogResult == true)
                            {
                                HourCorrection = AdjustingValues.SelectedItem.Key.ToInt();
                                await ParseNewMeasurements();
                            }
                        }
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "get_report_btn",
                        Group = "report",
                        Enabled = true,
                        Title = "Получить отчет",
                        ButtonUse = true,
                        ButtonName = "GetReport",
                        AccessLevel = Role.AccessMode.Special,
                        Action = () =>
                        {
                            BlockForSendler.Visibility = Visibility.Visible;
                            GetReport.Visibility = Visibility.Collapsed;

                            ReportFromDate.Text = DateTime.Now.AddMonths(-1).ToString("dd.MM.yyyy");

                            ReportToDate.Text = DateTime.Now.ToString("dd.MM.yyyy");

                        }
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "cancel_send_report_btn",
                        Group = "report",
                        Enabled = true,
                        Title = "Отмена отправки",
                        ButtonUse = true,
                        ButtonName = "CancelSendReport",
                        AccessLevel = Role.AccessMode.Special,
                        Action = () =>
                        {
                            GetReport.Visibility = Visibility.Visible;
                            BlockForSendler.Visibility = Visibility.Collapsed;
                        }
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "send_report_btn",
                        Group = "report",
                        Enabled = true,
                        Title = "Отправить отчет",
                        ButtonUse = true,
                        ButtonName = "SendReport",
                        AccessLevel = Role.AccessMode.Special,
                        Action = SendReportToEmail
                    });
                }

                Commander.Init(this);

            }
        }
        
        private FormHelper Form { get; set; }
        private string Id2 { get; set; }
        private string IdPz { get; set; }
        private int IdTest { get; set; }
        private int HourCorrection { get; set; }
        private int FactId { get; set; } = 1;

        private void FormInit()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="DATE_FROM",
                    FieldType=FormHelperField.FieldTypeRef.DateTime,
                    Default = DateTime.Now.AddMonths(-1).ToString("dd.MM.yyyy"),
                    Control= FromDate,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        RefreshGridFirst.Style = (Style)RefreshGridFirst.TryFindResource("FButtonPrimary");
                    }
                },
                new FormHelperField()
                {
                    Path="DATE_TO",
                    FieldType=FormHelperField.FieldTypeRef.DateTime,
                    Default = DateTime.Now.ToString("dd.MM.yyyy"),
                    Control= ToDate,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        RefreshGridFirst.Style = (Style)RefreshGridFirst.TryFindResource("FButtonPrimary");
                    }
                },
                new FormHelperField()
                {
                    Path = "CLICHE",
                    FieldType = FormHelperField.FieldTypeRef.Boolean,
                    Control = Clishe,
                    ControlType = "CheckBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{},
                    OnChange = (FormHelperField field, string value)=>{
                        GridFirst.UpdateItems();
                    },
                },
                new FormHelperField()
                {
                    Path = "VST_BELOW_LOWER",
                    FieldType = FormHelperField.FieldTypeRef.Boolean,
                    Control = VstBelowLower,
                    ControlType = "CheckBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{},
                    OnChange = (FormHelperField field, string value)=>{
                        GridFirst.UpdateItems();
                    },
                },
                new FormHelperField()
                {
                    Path = "SHTANZ_FORMA",
                    FieldType = FormHelperField.FieldTypeRef.Boolean,
                    Control = ShtanzForma,
                    ControlType = "CheckBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{},
                    OnChange = (FormHelperField field, string value) =>
                    {
                        GridFirst.UpdateItems();
                    }
                },
                new FormHelperField()
                {
                    Path = "LESS10",
                    FieldType = FormHelperField.FieldTypeRef.Boolean,
                    Control = Less10,
                    ControlType = "CheckBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{},
                    OnChange = (FormHelperField field, string value) =>
                    {
                        GridFirst.UpdateItems();
                    }
                },
                new FormHelperField()
                {
                    Path = "MORE10",
                    FieldType = FormHelperField.FieldTypeRef.Boolean,
                    Control = More10,
                    ControlType = "CheckBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{},
                    OnChange = (FormHelperField field, string value) =>
                    {
                        GridFirst.UpdateItems();
                    }
                },
                new FormHelperField
                {
                    Path = "ADJUSTING_VALUES",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = AdjustingValues,
                    ControlType = "SelectBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{},
                }
                
            };
            Form.SetFields(fields);
            Form.SetDefaults();
        }
        
        private void GridInit()
        {
            var column = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Дт. испытания",
                    Path="TEST_DATE",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format = "dd.MM.yyyy HH:mm:ss",
                    Width2=15,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="ART",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Group="Изделие",
                    Width2=15,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="Размеры",
                    Path="SIZE_PRODUCT",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Group="Изделие",
                    Width2=12,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="Профиль",
                    Path="PROFIL_NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Group="Изделие",
                    Width2=4,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="Марка",
                    Path="NAME_MARKA",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Group="Изделие",
                    Width2=16,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="Композиция",
                    Path="NAME_CARTON",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Group="Изделие",
                    Width2=13,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="Клише",
                    Path="CLICHE_FLAG",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Boolean,
                    Group="Изделие",
                    Width2=4,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="Штанцформа",
                    Path="SHTANZ_FLAG",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Boolean,
                    Group="Изделие",
                    Width2=4,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="Целевое ВСН, H",
                    Path="BCT_PLAN",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Group="Изделие",
                    Width2=8,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="ВСТ, H",
                    Path="BCT",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Group="Изделие",
                    Width2=8,
                    DxEnableColumnSorting = false,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if ((row.CheckGet("COUNT_TEST").ToDouble() >= 2) && (row.CheckGet("BCT").ToDouble() != 0) && (row.CheckGet("BCT").ToDouble() < row.CheckGet("BCT_MIN").ToDouble()))
                                {
                                    color = HColor.Red;
                                }
                                else if ((row.CheckGet("COUNT_TEST").ToDouble() >= 2) &&
                                         (row.CheckGet("BCT").ToDouble() != 0) &&
                                         (((row.CheckGet("BCT").ToDouble() / row.CheckGet("BCT_AVG").ToDouble()) - 1) * 100 <= -10))
                                {
                                    color = HColor.Orange;
                                }


                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        },
                    }
                },
                new DataGridHelperColumn
                {
                    Header="ВСТ 24, H",
                    Path="BCT_24",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Group="Изделие",
                    Width2=8,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="Прочность, кг",
                    Path="STRENGTH",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Group="Изделие",
                    Width2=8,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="Целевое ЕСТ, кН",
                    Path="ECT_PLAN",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Group="Изделие",
                    Width2=8,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="ЕСТ, кН",
                    Path="ECT",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Group="Изделие",
                    Width2=8,
                    DxEnableColumnSorting = false,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if ((row.CheckGet("ECT").ToDouble() != 0) &&
                                    (row.CheckGet("ECT_PLAN").ToDouble() != 0) &&
                                    (row.CheckGet("ECT_DIF").ToDouble() >= 20))
                                {
                                    color = HColor.GreenFG;
                                }
                                else if ((row.CheckGet("ECT").ToDouble() != 0) &&
                                         (row.CheckGet("ECT_PLAN").ToDouble() != 0) &&
                                         (row.CheckGet("ECT_DIF").ToDouble() >= 10))
                                {
                                    color = HColor.Yellow;
                                }
                                else if ((row.CheckGet("ECT").ToDouble() != 0) &&
                                         (row.CheckGet("ECT_PLAN").ToDouble() != 0) &&
                                         (row.CheckGet("ECT_DIF").ToDouble() <= -10))
                                {
                                    color = HColor.Red;
                                }
                                else if ((row.CheckGet("ECT").ToDouble() != 0) &&
                                         (row.CheckGet("ECT_PLAN").ToDouble() != 0) &&
                                         (row.CheckGet("ECT_DIF").ToDouble() > -10) &&
                                         (row.CheckGet("ECT_DIF").ToDouble() < -3))
                                {
                                    color = HColor.Orange;
                                }



                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        },
                    }
                },
                new DataGridHelperColumn
                {
                    Header="ЕСТ 24, кН",
                    Path="ECT_24",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Group="Изделие",
                    Width2=8,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="ЕСТ камера, кН",
                    Path="ECT_CHAMBER",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Group="Изделие",
                    Width2=8,
                    Visible = false,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="Влажность, %",
                    Path="HUMIDITY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Group="Изделие",
                    Width2=8,
                    Visible = false,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="Толщина, мм",
                    Path="THICKNESS",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Group="Изделие",
                    Width2=8,
                    DxEnableColumnSorting = false,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";
                                
                                if (Math.Abs(row.CheckGet("PROC_THICKNESS").ToInt()) >= 10 && row.CheckGet("THICKNESS").ToInt() != 0)
                                {
                                    color = HColor.Red;
                                }


                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        },
                    }
                },
                new DataGridHelperColumn
                {
                    Header="% потерь толщины",
                    Path="PROC_THICKNESS",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Group = "Изделие",
                    Width2=8,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="№ Пз",
                    Path="PZ_GA_NUM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Group="Заготовка",
                    Width2=8,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="ЕСТ, кН",
                    Path="ECT_GA",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Group="Заготовка",
                    Width2=8,
                    DxEnableColumnSorting = false,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if ((row.CheckGet("ECT_GA").ToDouble() != 0) &&
                                    (row.CheckGet("ECT_PLAN").ToDouble() != 0) &&
                                    (row.CheckGet("ECT_GA_DIF").ToDouble() >= 20))
                                {
                                    color = HColor.Yellow;
                                }
                                else if ((row.CheckGet("ECT_GA").ToDouble() != 0) &&
                                         (row.CheckGet("ECT_PLAN").ToDouble() != 0) &&
                                         (row.CheckGet("ECT_GA_DIF").ToDouble() >= 10))
                                {
                                    color = HColor.Yellow;
                                }
                                else if ((row.CheckGet("ECT_GA").ToDouble() != 0) &&
                                         (row.CheckGet("ECT_PLAN").ToDouble() != 0) &&
                                         (row.CheckGet("ECT_GA_DIF").ToDouble() <= -10))
                                {
                                    color = HColor.Red;
                                }
                                else if ((row.CheckGet("ECT_GA").ToDouble() != 0) &&
                                         (row.CheckGet("ECT_PLAN").ToDouble() != 0) &&
                                         (row.CheckGet("ECT_GA_DIF").ToDouble() > -10) &&
                                         (row.CheckGet("ECT_GA_DIF").ToDouble() < -3))
                                {
                                    color = HColor.Orange;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        },
                    }
                },
                new DataGridHelperColumn
                {
                    Header="ЕСТ 24, кН",
                    Path="ECT_GA_24",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Group="Заготовка",
                    Width2=8,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="ЕСТ камера, кН",
                    Path="ECT_GA_CHAMBER",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Group="Заготовка",
                    Width2=8,
                    Visible = false,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="Влажность, %",
                    Path="HUMIDITY_GA",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Group="Заготовка",
                    Width2=8,
                    Visible = false,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="Толщина план., мм",
                    Path="THICKNES_C",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Group="Заготовка",
                    Width2=8,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="Толщина, мм",
                    Path="THICKNESS_GA",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Group="Заготовка",
                    Width2=8,
                    DxEnableColumnSorting = false,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (Math.Abs(row.CheckGet("PROC_THICKNESS_GA").ToInt()) >= 10 && row.CheckGet("THICKNESS_GA").ToInt() != 0)
                                {
                                    color = HColor.Red;
                                }


                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        },
                    }
                },
                new DataGridHelperColumn
                {
                    Header="% потерь толщины",
                    Path="PROC_THICKNESS_GA",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Group = "Заготовка",
                    Width2=8,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="% потерь на переработке",
                    Path="ECT_PRC",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Width2=8,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="NOTE",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=8,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="Дата ВСТ",
                    Path="BCT_DTTM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format = "dd.MM.yyyy HH:mm:ss",
                    Group="Даты",
                    Width2=8,
                    Visible = false,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header = "Лаборант 1",
                    Path = "TEST_OPERATOR",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Group = "Даты",
                    Width2 = 8,
                    Visible = false,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="Дата ЕСТ изделия",
                    Path="ECT_DTTM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format = "dd.MM.yyyy HH:mm:ss",
                    Group="Даты",
                    Width2=8,
                    Visible = false,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header = "Лаборант 2",
                    Path = "TEST_OPERATOR_ECT",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Group = "Даты",
                    Width2 = 8,
                    Visible = false,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="Дата ЕСТ заготовки",
                    Path="ECT_GA_DTTM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format = "dd.MM.yyyy HH:mm:ss",
                    Group="Даты",
                    Width2=8,
                    Visible = false,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="Дельта времени ЕСТ, мин",
                    Path="ECT_DTTM_DELTA",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=8,
                    Group="Даты",
                    Visible = false,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="ИД Испытания",
                    Path="ID",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=0,
                    Group="Идентификаторы",
                    Visible = false,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="ИД Продукции",
                    Path="ID2",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2= 0,
                    Group="Идентификаторы",
                    Visible = false,
                    DxEnableColumnSorting = false,
                },
                new DataGridHelperColumn
                {
                    Header="ИД ПЗ на ГА",
                    Path="ID_PZ_GA",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=0,
                    Group="Идентификаторы",
                    Visible = false,
                    DxEnableColumnSorting = false,
                },
            };
            GridFirst.SetColumns(column);
            GridFirst.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            GridFirst.OnLoadItems = LoadItems;
            GridFirst.Toolbar = GridToolbarOne;
            GridFirst.SearchText = SearchText;
            GridFirst.SetPrimaryKey("ID");
            GridFirst.EnableFiltering = true;

            GridFirst.OnSelectItem = (Dictionary<string, string> selectedItem) =>
            {
                Id2 = selectedItem.CheckGet("ID2");
                IdPz = selectedItem.CheckGet("ID_PZ_GA");
                IdTest = selectedItem.CheckGet("ID").ToInt();
                LoadItemSecond();
                LoadItemThree();
            };

            GridFirst.OnFilterItems = () =>
            {
                if (GridFirst.Items.Count > 0)
                {
                    {
                        var v = Form.GetValues();
                        bool showAll = v.CheckGet("VST_BELOW_LOWER").ToBool() || v.CheckGet("CLICHE").ToBool() ||
                                       v.CheckGet("SHTANZ_FORMA").ToBool() || v.CheckGet("LESS10").ToBool() ||
                                       v.CheckGet("MORE10").ToBool();

                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in GridFirst.Items)
                        {

                            if (!showAll)
                            {
                                items.Add(row);
                            }
                            else
                            {
                                var exclude = true;


                                if (v.CheckGet("VST_BELOW_LOWER").ToInt() == 1)
                                {
                                    if ((row.CheckGet("COUNT_TEST").ToDouble() >= 2) &&
                                        (row.CheckGet("BCT").ToInt() != 0) &&
                                         string.IsNullOrEmpty(row.CheckGet("BCT")) != true &&
                                         (row.CheckGet("BCT").ToDouble() < row.CheckGet("BCT_MIN").ToDouble()))
                                    { }
                                    else
                                        exclude = false;
                                }

                                if (!exclude)
                                    continue;

                                if (v.CheckGet("CLICHE").ToInt() == 1)
                                {
                                    if (row.CheckGet("CLICHE_FLAG").ToInt() == 1)
                                    { }
                                    else
                                        exclude = false;
                                }

                                if (!exclude)
                                    continue;

                                if (v.CheckGet("SHTANZ_FORMA").ToInt() == 1)
                                {
                                    if (row.CheckGet("SHTANZ_FLAG").ToInt() == 1)
                                    { }
                                    else
                                        exclude = false;
                                }

                                if (!exclude)
                                    continue;
                                
                                if (v.CheckGet("LESS10").ToInt() == 1)
                                {
                                    if (((string.IsNullOrEmpty(row.CheckGet("ECT_GA")) != true) &&
                                         (string.IsNullOrEmpty(row.CheckGet("ECT_PLAN")) != true) &&
                                         (row.CheckGet("ECT_GA_DIF").ToDouble() < -10)) 
                                        ||
                                        ((string.IsNullOrEmpty(row.CheckGet("ECT")) != true) &&
                                         (string.IsNullOrEmpty(row.CheckGet("ECT_PLAN")) != true) &&
                                         (row.CheckGet("ECT_DIF").ToDouble() < -10))
                                        )
                                    { }
                                    else
                                        exclude = false;
                                }

                                if (!exclude)
                                    continue;

                                if (v.CheckGet("MORE10").ToInt() == 1)
                                {
                                    if (((string.IsNullOrEmpty(row.CheckGet("ECT_GA")) != true) &&
                                         (string.IsNullOrEmpty(row.CheckGet("ECT_PLAN")) != true) &&
                                         (row.CheckGet("ECT_GA_DIF").ToDouble() > 10))
                                        ||
                                        ((string.IsNullOrEmpty(row.CheckGet("ECT")) != true) &&
                                         (string.IsNullOrEmpty(row.CheckGet("ECT_PLAN")) != true) &&
                                         (row.CheckGet("ECT_DIF").ToDouble() > 10))
                                       )
                                    { }
                                    else
                                        exclude = false;
                                }

                                if (!exclude)
                                    continue;

                                items.Add(row);
                            }

                        }

                        GridFirst.Items = items;
                    }
                }
            };

            GridFirst.Commands = Commander;

            GridFirst.Init();
        }
        
        private void ShowHiddenColumnsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            GridFirst.GridColumnIsVisible(1);
        }
        
        private void ShowHiddenColumnsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            GridFirst.GridColumnIsVisible(0);
        }

        private void GridSecondInit()
        {
            var column = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="ГА",
                    Path="ST_GA_NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=4
                },
                new DataGridHelperColumn
                {
                    Header="Линия",
                    Path="ST_NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=4
                },
                new DataGridHelperColumn
                {
                    Header="Время до переработки, ч",
                    Path="TIME_STOCK",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Width2=4
                },
            };
            GridSecond.SetColumns(column);
            GridSecond.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            GridSecond.OnLoadItems = LoadItemSecond;
            GridSecond.Init();
        }

        private void GridThreeInit()
        {
            var column = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Дт накладной",
                    Path="DATA",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format = "dd.MM.yyyy",
                    Width2=4
                },
                new DataGridHelperColumn
                {
                    Header="Раскат",
                    Path="LAYER",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=4
                },
                new DataGridHelperColumn
                {
                    Header="Рулон",
                    Path="NUM_ROLL",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=4
                },
                new DataGridHelperColumn
                {
                    Header="Длина задания",
                    Path="LEN",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Width2=4
                },
                new DataGridHelperColumn
                {
                    Header="Длина рулона",
                    Path="ROLL_LENGTH",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Width2=4
                },
                new DataGridHelperColumn
                {
                    Header="Факт",
                    Path="NAME_PAPER_FACT",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=4
                },
                new DataGridHelperColumn
                {
                    Header="План",
                    Path="NAME_PAPER_PLAN",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=4
                },
                new DataGridHelperColumn
                {
                    Header="RCT, kH/m",
                    Path="RCT",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Width2=4
                },
                new DataGridHelperColumn
                {
                    Header="SCT, kH/m",
                    Path="SCT",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Width2=4
                },

            };

            GridThree.SetColumns(column);
            GridThree.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            GridThree.OnLoadItems = LoadItemThree;
            GridThree.Init();
        }

        /// <summary>
        /// Отправка статуса о необходимости отчета
        /// </summary>
        /// <returns></returns>
        private async void SendReportToEmail()
        {
            var p = new Dictionary<string, string>
            {
                { "REPORT_START_DATE", $"{ReportFromDate.Text} 08:00" },
                { "REPORT_END_DATE", $"{ReportToDate.Text} 08:00" },
                { "FACT_ID", FactId.ToString() }
            };

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductTestTrial");
            q.Request.SetParam("Object", "Measurements");
            q.Request.SetParam("Action", "SetValueForSend");
            q.Request.SetParams(p);

            await Task.Run(() => q.DoQuery());

            if (q.Answer.Status == 0)
            {
                var dialog = new DialogWindow("Запрос обработан. На почту отчет придет в течение 1-2 минут", "Отчет");

                dialog.ShowDialog();

                GetReport.Visibility = Visibility.Visible;
                BlockForSendler.Visibility = Visibility.Collapsed;
            }
            else
            {
                var dialog = new DialogWindow("Ошибка при отправке статуса на формирование отчета", "Отчет");

                dialog.ShowDialog();
            }
        }

        /// <summary>
        /// Загрузка данных GridInit
        /// </summary>
        private async void LoadItems()
        {
            GridToolbarOne.IsEnabled = false;
            GridToolbarSecond.IsEnabled = false;

            bool resume = true;

            {
                var f = FromDate.Text.ToDateTime();
                var t = ToDate.Text.ToDateTime();
                if (DateTime.Compare(f, t) > 0)
                {
                    var msg = "Дата начала периода не может быть больше даты окончания периода";
                    var d = new DialogWindow(msg, "Проверка данных");
                    d.ShowDialog();
                    resume = false;
                    GridToolbarOne.IsEnabled = true;
                    GridToolbarSecond.IsEnabled = true;
                }
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "ProductTestTrial");
                q.Request.SetParam("Object", "Measurements");
                q.Request.SetParam("Action", "List");
                q.Request.SetParam("DATE_FROM", FromDate.Text);
                q.Request.SetParam("DATE_TO", ToDate.Text);

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
                        var item = ListDataSet.Create(result, "TEST_SELECT");
                        GridFirst.UpdateItems(item);

                        RefreshGridFirst.Style = (System.Windows.Style)RefreshGridFirst.TryFindResource("Button");
                    }
                    else
                    {
                        q.ProcessError();
                    }

                    GridToolbarOne.IsEnabled = true;
                    GridToolbarSecond.IsEnabled = true;
                }
            }
        }


        /// <summary>
        /// Загрузка данных GridSecondInit
        /// </summary>
        private async void LoadItemSecond()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductTestTrial");
            q.Request.SetParam("Object", "Measurements");
            q.Request.SetParam("Action", "ListGA");
            q.Request.SetParam("ID2", Id2);
            q.Request.SetParam("ID_PZ", IdPz);

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
                    var item = ListDataSet.Create(result, "PZ_SELECT");
                    GridSecond.UpdateItems(item);
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Загрузка данных GridThreeInit
        /// </summary>
        private async void LoadItemThree()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductTestTrial");
            q.Request.SetParam("Object", "Measurements");
            q.Request.SetParam("Action", "ListRolls");
            q.Request.SetParam("ID_PZ", IdPz);

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
                    var item = ListDataSet.Create(result, "ROLLS_SELECT");
                    GridThree.UpdateItems(item);
                }
                else
                {
                    q.ProcessError();
                }
            }
        }


        /// <summary>
        /// Показать тех.карту
        /// </summary>
        private void ShowTechnologyMap()
        {
            if (GridFirst.Items.Count > 0)
            {
                if (GridFirst.SelectedItem != null)
                {
                    var path = GridFirst.SelectedItem.CheckGet("PATHTK");
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (File.Exists(path))
                        {
                            Central.OpenFile(path);
                        }
                    }
                }
            }
        }

        private async Task ParseNewMeasurements()
        {
            UploadButton.IsEnabled = false;
            UploadButton.Content = "Парсинг BCT...";

            var date = DateTime.Now;

            string[] arrayMonth =
            {
                "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь",
                "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь"
            };

            DateTime now = DateTime.Now;

            // Получаем маскимальную дату для 1 площадки

            var _maxDate = await GetMaxDate();

            if (_maxDate == null)
            {
                new DialogWindow("Ошибка во время получения последней даты", "Загрузка").ShowDialog();
                UploadButton.Content = "Загрузить";
                UploadButton.IsEnabled = true;
                return;
            }

            if (!_maxDate.CheckGet("MAX_DATE").IsNullOrEmpty())
            {
                date = _maxDate.CheckGet("MAX_DATE").ToDateTime();
            }

            var totalLoadedBct = 0;
            var totalLoadedEct = 0;

            try
            {
                // Обработка BCT файлов
                UploadButton.Content = "Загрузка BCT...";

                string folder = $@"\\192.168.21.60\unipap\{now.Year}\{now.Month:D2} {arrayMonth[now.Month - 1]}";
                string[] files = await Task.Run(() => Directory.GetFiles(folder, "ГОСТ*.xml"));

                foreach (var file in files)
                {
                    if (date.AddHours(HourCorrection) < File.GetLastWriteTime(file))
                    {
                        var result = await LoadFileBct(file);
                        totalLoadedBct += result;
                    }
                }

                // Обработка ECT файлов
                UploadButton.Content = "Парсинг ECT...";

                folder = $@"\\192.168.21.59\Испытательнaя машина\Результаты (csv)\ИТС 8111-1,0\{now.Year}\{now.Month:D2} {arrayMonth[now.Month - 1]}";
                string[] filesWithFolderEct = await Task.Run(() => Directory.GetFiles(folder, "ГОСТ*.xml"));

                UploadButton.Content = "Загрузка ECT...";

                foreach (var file in filesWithFolderEct)
                {
                    if (date.AddHours(HourCorrection) < File.GetLastWriteTime(file))
                    {
                        var result = await LoadFileEct(file);
                        totalLoadedEct += result;
                    }
                }
            }
            catch (Exception ex)
            {
                new DialogWindow($"Ошибка при загрузке файлов: {ex.Message}", "Ошибка").ShowDialog();
            }
            finally
            {
                UploadButton.Content = "Загрузить";
                UploadButton.IsEnabled = true;
            }

            if (totalLoadedBct != 0 || totalLoadedEct != 0)
            {
                new DialogWindow($"Загружено ЕСТ = {totalLoadedEct} BCT = {totalLoadedBct}", "Загрузка").ShowDialog();
            }

            GridFirst.LoadItems();
        }


        private async Task<int> LoadFileBct(string file)
        {
            try
            {
                var fl = await Task.Run(() => File.ReadAllLines(file));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "ProductTestTrial");
                q.Request.SetParam("Object", "Measurements");
                q.Request.SetParam("Action", "LoadBct");
                q.Request.SetParam("FILE", JsonConvert.SerializeObject(fl[1]));
                q.Request.SetParam("LAST_WRITE", File.GetLastWriteTime(file).ToString("dd.MM.yyyy HH:mm:ss"));
                q.Request.Timeout = 120000;

                q.DoQuery();

                return q.Answer.Status == 0 ? 1 : 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private async Task<int> LoadFileEct(string file)
        {
            try
            {
                var fl = await Task.Run(() => File.ReadAllLines(file));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "ProductTestTrial");
                q.Request.SetParam("Object", "Measurements");
                q.Request.SetParam("Action", "LoadEct");
                q.Request.SetParam("FILE", JsonConvert.SerializeObject(fl[1]));
                q.Request.SetParam("LAST_WRITE", File.GetLastWriteTime(file).ToString("dd.MM.yyyy HH:mm:ss"));
                q.Request.Timeout = 120000;

                q.DoQuery();

                return q.Answer.Status == 0 ? 1 : 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private async Task<Dictionary<string, string>> GetMaxDate()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductTestTrial");
            q.Request.SetParam("Object", "Measurements");
            q.Request.SetParam("Action", "GetMaxDate");

            await Task.Run(() => q.DoQuery());

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(q.Answer.Data);

                return result;
            }

            return null;
        }

        private void DeleteMeasurementsShowWindow()
        {
            var id = GridFirst.SelectedItem.CheckGet("ID").ToInt();
            var dialog = new DialogWindow($"Вы уверены что хотите удалить тестирование - {id}?", "Удаление", "", DialogWindowButtons.YesNo);
            dialog.ShowDialog();

            if (dialog.DialogResult != true)
            {
                return;
            }

            DeleteMeasurements(id.ToString());
        }

        private void DeleteMeasurements(string id)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductTestTrial");
            q.Request.SetParam("Object", "Measurements");
            q.Request.SetParam("Action", "Delete");
            q.Request.SetParam("ID", id);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                GridFirst.LoadItems();
            }
            else
            {
                q.ProcessError();
            }
        }

        private void SetDefaultValues()
        {
            var list = new Dictionary<string, string>()
            {
                {"0", "Обычный режим"},
                {"-3", "3 часа"},
                {"-6", "6 часов"},
                {"-9", "9 часов"},
                {"-12", "12 часов"},
            };
            
            AdjustingValues.Items = list;
            AdjustingValues.SetSelectedItemByKey("0");
            HourCorrection = AdjustingValues.SelectedItem.Value.ToInt();

            var factoryTypeItems = new Dictionary<string, string>
            {
                { "1", "Л-ПАК ЛИПЕЦК" },
                { "2", "Л-ПАК КАШИРА" }
            };
            FactoryType.Items = factoryTypeItems;
            FactoryType.SetSelectedItemByKey("1");
        }

        /// <summary>
        /// Функция для корректировки тестовых значений
        /// (Повторный прогон данных)
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void AdjustingValues_OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var s = (SelectBox)d;

            HourCorrection = s.SelectedItem.Key.ToInt();
        }

        private void FactoryType_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var s = (SelectBox)d;

            FactId = s.SelectedItem.Key.ToInt();
        }
    }
}
