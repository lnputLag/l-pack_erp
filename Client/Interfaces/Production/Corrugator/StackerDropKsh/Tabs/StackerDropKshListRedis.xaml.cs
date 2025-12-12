using Client.Assets.HighLighters;
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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.Corrugator
{
    /// <summary>
    /// Список съёмов стекера из Redis
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class StackerDropKshListRedis : ControlBase
    {
        public StackerDropKshListRedis()
        {
            ControlTitle = "Список съёмов Redis";
            RoleName = "[erp]debug";
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
                StackerDropGridInit();
                CompletedTaskGridInit();
                BlockedTaskGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                StackerDropGrid.Destruct();
                CompletedTaskGrid.Destruct();
                BlockedTaskGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                StackerDropGrid.ItemsAutoUpdate = true;
                StackerDropGrid.Run();

                CompletedTaskGrid.ItemsAutoUpdate = true;
                CompletedTaskGrid.Run();

                BlockedTaskGrid.ItemsAutoUpdate = true;
                BlockedTaskGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                StackerDropGrid.ItemsAutoUpdate = false;
                CompletedTaskGrid.ItemsAutoUpdate = false;
                BlockedTaskGrid.ItemsAutoUpdate = false;
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
                        StackerDropGrid.ItemsExportExcel();
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
        /// Датасет с данными грида сёъмов стекера
        /// </summary>
        private ListDataSet StackerDropGridDataSet { get; set; }

        private ListDataSet CompletedTaskGridDataSet { get; set; }

        private ListDataSet BlockedTaskGridDataSet { get; set; }

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
                        Path="STACKER_DROP_SEARCH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=StackerDropGridSearchText,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="COMPLETED_TASK_SEARCH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=CompletedTaskGridSearchText,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="BLOCKED_TASK_SEARCH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=BlockedTaskGridSearchText,
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
            StackerDropGridDataSet = new ListDataSet();
            CompletedTaskGridDataSet = new ListDataSet();
            BlockedTaskGridDataSet = new ListDataSet();

            Form.SetDefaults();
        }

        private void StackerDropGridInit()
        {
            //инициализация грида
            {
                string dataUpColor = HColor.Blue;
                string dataDnColor = HColor.Yellow;

                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Description = "_ROWNUMBER",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                    },

                    new DataGridHelperColumn
                    {
                        Header="▲Дата съёма",
                        Description = "dataUp_currDateTime Дата съёма верхнего стекера",
                        Path="dataUp_currDateTime",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=14,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataUpColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▲Id",
                        Description = "dataUp_id Ид съёма верхнего стекера",
                        Path="dataUp_id",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataUpColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▲Ид ПЗ",
                        Description = "dataUp_productionTaskId Ид производственного задания верхнего стекера",
                        Path="dataUp_productionTaskId",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataUpColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▲Ид продукции",
                        Description = "dataUp_customerId Ид продукции верхнего стекера",
                        Path="dataUp_customerId",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataUpColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▲В ручье",
                        Description = "dataUp_sheetsPerPart Количество заготовок в ручье верхнего стекера",
                        Path="dataUp_sheetsPerPart",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataUpColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▲Ручьёв",
                        Description = "dataUp_parts Количество ручьёв для верхнего стекера",
                        Path="dataUp_parts",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataUpColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▲Съём",
                        Description = "dataUp_dropped_sheets Количество в съёме верхнего стекера",
                        Path="dataUp_dropped_sheets",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataUpColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▲По заданию",
                        Description = "dataUp_sheets Количество по заданию верхнего стекера",
                        Path="dataUp_sheets",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataUpColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▲Last",
                        Description = "dataUp_lastStack Признак последнего съёма верхнего стекера",
                        Path="dataUp_lastStack",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataUpColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },

                    new DataGridHelperColumn
                    {
                        Header="▼Дата съёма",
                        Description = "[dataDn_currDateTime] Дата съёма нижнего стекера",
                        Path="dataDn_currDateTime",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=14,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataDnColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▼Id",
                        Description = "[dataDn_id] Ид съёма нижнего стекера",
                        Path="dataDn_id",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataDnColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▼Ид ПЗ",
                        Description = "[dataDn_productionTaskId] Ид производственного задания нижнего стекера",
                        Path="dataDn_productionTaskId",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataDnColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▼Ид продукции",
                        Description = "[dataDn_customerId] Ид продукции нижнего стекера",
                        Path="dataDn_customerId",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataDnColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▼В ручье",
                        Description = "[dataDn_sheetsPerPart] Количество заготовок в ручье нижнего стекера",
                        Path="dataDn_sheetsPerPart",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataDnColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▼Ручьёв",
                        Description = "[dataDn_parts] Количество ручьёв для нижнего стекера",
                        Path="dataDn_parts",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataDnColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▼Съём",
                        Description = "[dataDn_dropped_sheets] Количество заготовок в съёме нижнего стекера",
                        Path="dataDn_dropped_sheets",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataDnColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▼По заданию",
                        Description = "[dataDn_sheets] Количество заготовок по заданию нижнего стекера",
                        Path="dataDn_sheets",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataDnColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▼Last",
                        Description = "[dataDn_lastStack] Признак последнего съёма нижнего стекера",
                        Path="dataDn_lastStack",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataDnColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },

                    new DataGridHelperColumn
                    {
                        Header="▲Рилёвки",
                        Description = "[dataUp_scoring] Рилёвки верхнего стекера",
                        Path="dataUp_scoring",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataUpColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▲Длина",
                        Description = "[dataUp_boardLength] Длина заготовки верхнего стекера",
                        Path="dataUp_boardLength",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataUpColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▲Ширина",
                        Description = "[dataUp_boardWidth] Ширина заготовки верхнего стекера",
                        Path="dataUp_boardWidth",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataUpColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▲Высота",
                        Description = "[dataUp_boardHeight] Высоат заготовки верхнего стекера",
                        Path="dataUp_boardHeight",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataUpColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▲Артикул",
                        Description = "[dataUp_customerName] Артикул продукции верхнего стекера",
                        Path="dataUp_customerName",
                        ColumnType=ColumnTypeRef.String,
                        Width2=14,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataUpColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▲Печать",
                        Description = "[dataUp_needPrint] Признак того, что нужна печать для верхнего стекера",
                        Path="dataUp_needPrint",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataUpColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▲Поворот",
                        Description = "[dataUp_needTurnRound] Признак того, что нужне поворот для верхнего стекера",
                        Path="dataUp_needTurnRound",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataUpColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▲LineId",
                        Description = "[dataUp_lineId] верхнего стекера",
                        Path="dataUp_lineId",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataUpColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▲№ стекера",
                        Description = "[dataUp_stackerNo] Номер ГА верхнего стекера",
                        Path="dataUp_stackerNo",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataUpColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▲Стекер",
                        Description = "[dataUp_layer] Этаж верхнего стекера",
                        Path="dataUp_layer",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataUpColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▲ScheduleId",
                        Description = "[dataUp_scheduleId] верхнего стекера",
                        Path="dataUp_scheduleId",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataUpColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▲Ид задания",
                        Description = "[dataUp_salesOrderId] Ид задания верхнего стекера",
                        Path="dataUp_salesOrderId",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataUpColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▲Примечание",
                        Description = "[dataUp_remark] Примечание верхнего стекера",
                        Path="dataUp_remark",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataUpColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },

                    new DataGridHelperColumn
                    {
                        Header="▼Рилёвки",
                        Description = "[dataDn_scoring] Рилёвки нижнего стекера",
                        Path="dataDn_scoring",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataDnColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▼Длина",
                        Description = "[dataDn_boardLength] Длина заготовки нижнего стекера",
                        Path="dataDn_boardLength",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataDnColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▼Ширина",
                        Description = "[dataDn_boardWidth] Ширина заготовки нижнего стекера",
                        Path="dataDn_boardWidth",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataDnColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▼Высота",
                        Description = "[dataDn_boardHeight] Высота заготовки нижнего стекера",
                        Path="dataDn_boardHeight",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataDnColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▼Артикул",
                        Description = "[dataDn_customerName] Артикул продукции нижнего стекера",
                        Path="dataDn_customerName",
                        ColumnType=ColumnTypeRef.String,
                        Width2=14,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataDnColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▼Печать",
                        Description = "[dataDn_needPrint] Признак того, что нужна печать для нижнего стекера",
                        Path="dataDn_needPrint",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataDnColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▼Поворот",
                        Description = "[dataDn_needTurnRound] Признак того, что нужен поворот для нижнего стекера",
                        Path="dataDn_needTurnRound",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataDnColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▼LineId",
                        Description = "[dataDn_lineId] нижнего стекера",
                        Path="dataDn_lineId",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataDnColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▼№ стекера",
                        Description = "[dataDn_stackerNo] Номер ГА нижнего стекера",
                        Path="dataDn_stackerNo",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataDnColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▼Стекер",
                        Description = "[dataDn_layer] Этаж нижнего стекера",
                        Path="dataDn_layer",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataDnColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▼ScheduleId",
                        Description = "[dataDn_scheduleId] нижнего стекера",
                        Path="dataDn_scheduleId",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataDnColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▼Ид задания",
                        Description = "[dataDn_salesOrderId] Ид задания нижнего стекера",
                        Path="dataDn_salesOrderId",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataDnColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="▼Примечание",
                        Description = "[dataDn_remark] Примечание нижнего стекера",
                        Path="dataDn_remark",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=dataDnColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },

                    new DataGridHelperColumn
                    {
                        Header="resultCode",
                        Description = "resultCode",
                        Path="resultCode",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="resultMessage",
                        Description = "resultMessage",
                        Path="resultMessage",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                    },
                };
                StackerDropGrid.SetColumns(columns);
                StackerDropGrid.SearchText = StackerDropGridSearchText;
                StackerDropGrid.OnLoadItems = StackerDropGridLoadItems;
                StackerDropGrid.SetPrimaryKey("_ROWNUMBER");
                StackerDropGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                StackerDropGrid.AutoUpdateInterval = 5 * 60;
                StackerDropGrid.Toolbar = StackerDropGridToolbar;
                StackerDropGrid.Commands = Commander;
                StackerDropGrid.UseProgressSplashAuto = false;
                StackerDropGrid.Init();
            }
        }

        private async void StackerDropGridLoadItems()
        {
            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "StackerDropKsh");
                q.Request.SetParam("Action", "ListRedis");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                StackerDropGridDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        StackerDropGridDataSet = ListDataSet.Create(result, "DROP");
                    }
                }
                else
                {
                    q.ProcessError();
                }
                StackerDropGrid.UpdateItems(StackerDropGridDataSet);
            }
        }

        private void CompletedTaskGridInit()
        {
            //инициализация грида
            {
                string calculatedColemnColor = HColor.Gray;

                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Description = "_ROWNUMBER",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата",
                        Description = "[Date] Дата вставки записи",
                        Path="Date",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=14,
                        Format="dd.MM.yyyy HH:mm:ss",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид ПЗ",
                        Description = "[productionTaskId] Ид производственного задания",
                        Path="productionTaskId",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид продукции",
                        Description = "[productId] Ид продукции",
                        Path="productId",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Всего*",
                        Description = "[AllQtyByOuts] Всего выпущено в одном ручье",
                        Path="AllQtyByOuts",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=calculatedColemnColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Хороших*",
                        Description = "[GoodQtyByOuts] Хороших выпущено в одном ручье",
                        Path="GoodQtyByOuts",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=calculatedColemnColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Брак*",
                        Description = "[BadQtyByOuts] Брака выпущено в одном ручье",
                        Path="BadQtyByOuts",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=calculatedColemnColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ручьёв*",
                        Description = "[OutsInDb] Фактическое количество ручьёв",
                        Path="OutsInDb",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;

                                    {
                                        result=calculatedColemnColor.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Всего 1",
                        Description = "[AllQty] Всего выпущено в одном ручье",
                        Path="AllQty",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Хороших 1",
                        Description = "[GoodQty] Хороших выпущено в одном ручье",
                        Path="GoodQty",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Брак 1",
                        Description = "[BadQty] Брака выпущено в одном ручье",
                        Path="BadQty",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ручьёв по заданию",
                        Description = "[OutsInFile] Количество ручьёв по заданию",
                        Path="OutsInFile",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Тандем",
                        Description = "[IsTandem] Признак того, что задание тандемное",
                        Path="IsTandem",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид задания",
                        Description = "[salesOrderId] Ид задания",
                        Path="salesOrderId",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                    },
                };
                CompletedTaskGrid.SetColumns(columns);
                CompletedTaskGrid.SearchText = CompletedTaskGridSearchText;
                CompletedTaskGrid.Toolbar = CompletedTaskGridToolbar;
                CompletedTaskGrid.OnLoadItems = CompletedTaskGridLoadItems;
                CompletedTaskGrid.SetPrimaryKey("_ROWNUMBER");
                CompletedTaskGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                CompletedTaskGrid.AutoUpdateInterval = 5 * 60;
                CompletedTaskGrid.Commands = Commander;
                CompletedTaskGrid.UseProgressSplashAuto = false;
                CompletedTaskGrid.Init();
            }
        }

        private async void CompletedTaskGridLoadItems()
        {
            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "StackerDropKsh");
                q.Request.SetParam("Action", "ListCompletedTaskRedis");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                CompletedTaskGridDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        CompletedTaskGridDataSet = ListDataSet.Create(result, "COMPLETED_TASK");
                    }
                }
                else
                {
                    q.ProcessError();
                }
                CompletedTaskGrid.UpdateItems(CompletedTaskGridDataSet);
            }
        }

        private void BlockedTaskGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Description = "_ROWNUMBER",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Дата",
                        Description = "[insert_date] Дата встарвки записи",
                        Path="insert_date",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=14,
                        Format="dd.MM.yyyy HH:mm:ss",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид ПЗ",
                        Description = "[productionTaskId] Ид производственного задания",
                        Path="productionTaskId",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид продукции",
                        Description = "[productId] Ид продукции",
                        Path="productId",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид задания",
                        Description = "[salesOrderId] Ид задания",
                        Path="salesOrderId",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                    },
                };
                BlockedTaskGrid.SetColumns(columns);
                BlockedTaskGrid.SearchText = BlockedTaskGridSearchText;
                BlockedTaskGrid.Toolbar = BlockedTaskGridToolbar;
                BlockedTaskGrid.OnLoadItems = BlockedTaskGridLoadItems;
                BlockedTaskGrid.SetPrimaryKey("_ROWNUMBER");
                BlockedTaskGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                BlockedTaskGrid.AutoUpdateInterval = 5 * 60;
                BlockedTaskGrid.Commands = Commander;
                BlockedTaskGrid.UseProgressSplashAuto = false;
                BlockedTaskGrid.Init();
            }
        }

        private async void BlockedTaskGridLoadItems()
        {
            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "StackerDropKsh");
                q.Request.SetParam("Action", "ListBlockedTaskRedis");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                BlockedTaskGridDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        BlockedTaskGridDataSet = ListDataSet.Create(result, "BLOCKED_TASK");
                    }
                }
                else
                {
                    q.ProcessError();
                }
                BlockedTaskGrid.UpdateItems(BlockedTaskGridDataSet);
            }
        }

        public void Refresh()
        {
            StackerDropGrid.LoadItems();
            CompletedTaskGrid.LoadItems();
        }
    }
}
