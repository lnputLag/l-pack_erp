using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Sales;
using Client.Interfaces.Service.Printing;
using DevExpress.Utils.Design;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static DevExpress.XtraPrinting.Native.ExportOptionsPropertiesNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// История изменения техкарты
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class MoldedContainerTechCardHistory : ControlBase
    {
        public MoldedContainerTechCardHistory()
        {
            InitializeComponent();
            OnLoad = () =>
            {
                GridInit();
            };

            OnUnload = () =>
            {
                HistoryGrid.Destruct();
            };

            OnFocusGot = () =>
            {
                HistoryGrid.ItemsAutoUpdate = true;
                HistoryGrid.Run();
            };

            OnFocusLost = () =>
            {
                HistoryGrid.ItemsAutoUpdate = false;
            };

            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "Refresh",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить историю",
                        ButtonUse = true,
                        ButtonName = "RefreshButton",
                        MenuUse = false,
                        Action = () =>
                        {
                            HistoryGrid.LoadItems();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "Back",
                        Enabled = true,
                        Title = "Назад",
                        ButtonUse = true,
                        ButtonName = "BackButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            Close();
                        },
                    });
                }
                Commander.Init(this);
            }
        }

        /// <summary>
        /// ИД техкарты
        /// </summary>
        public int IdTk;

        /// <summary>
        /// Имя вкладки, окуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;

        public ListDataSet GridDS;

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        private void GridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Дата изменения",
                    Path="AUDIT_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=15,
                    Format="dd.MM.yyyy HH:mm:ss",
                },
                new DataGridHelperColumn
                {
                    Header="Автор",
                    Path="AUDIT_USER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("STATUS_F").ToInt() == 1 )
                                {
                                    color = HColor.Green;
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
                    Header="Дизайн",
                    Path="DESIGN",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("DESIGN_F").ToInt() == 1 )
                                {
                                    color = HColor.Green;
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
                    Header="Артикул",
                    Path="SKU",
                    ColumnType=ColumnTypeRef.String,
                    Width2=9,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("SKU_F").ToInt() == 1 )
                                {
                                    color = HColor.Green;
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
                    Header="Наименование",
                    Path="NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("NAME_F").ToInt() == 1 )
                                {
                                    color = HColor.Green;
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
                    Path="BUYER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=25,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("BUYER_NAME_F").ToInt() == 1 )
                                {
                                    color = HColor.Green;
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
                    Header="Тип контейнера",
                    Path="CONTAINER_TYPE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("CONTAINER_TYPE_F").ToInt() == 1 )
                                {
                                    color = HColor.Green;
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
                    Header="Схема производства",
                    Path="PRODUCTION_SCHEME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("PRODUCTION_SCHEME_F").ToInt() == 1 )
                                {
                                    color = HColor.Green;
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
                    Header="Цвет",
                    Path="CONTAINER_COLOR",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("CONTAINER_COLOR_F").ToInt() == 1 )
                                {
                                    color = HColor.Green;
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
                    Header="Размеры упаковки",
                    Path="PALLET_SIZE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=14,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("PALLET_SIZE_F").ToInt() == 1 )
                                {
                                    color = HColor.Green;
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
                    Header="Файл ТК",
                    Path="HAS_TK_FILE",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=8,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("HAS_TK_FILE_F").ToInt() == 1 )
                                {
                                    color = HColor.Green;
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
                    Header="Файл дизайна",
                    Path="HAS_PRINT_FILE",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=8,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("HAS_PRINT_FILE_F").ToInt() == 1 )
                                {
                                    color = HColor.Green;
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
                    Header="Этикетка",
                    Path="HAS_STICKER",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=8,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("HAS_STICKER_F").ToInt() == 1 )
                                {
                                    color = HColor.Green;
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
                    Header="Кол-во доработок",
                    Path="REWORK_CNT",
                    ColumnType=ColumnTypeRef.Double,
                    Format = "N0",
                    Width2=10,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("REWORK_CNT_F").ToInt() == 1 )
                                {
                                    color = HColor.Green;
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
                    Header="Инженер",
                    Path="CREATOR",
                    ColumnType=ColumnTypeRef.String,
                    Width2=14,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("CREATOR_F").ToInt() == 1 )
                                {
                                    color = HColor.Green;
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
                    Header="Дизайнер",
                    Path="DESIGNER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=14,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("DESIGNER_NAME_F").ToInt() == 1 )
                                {
                                    color = HColor.Green;
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
                    Path="STATUS_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Флаг изменения",
                    Path="DESIGN_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="SKU_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="NAME_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="BUYER_NAME_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="CONTAINER_TYPE_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="PRODUCTION_SCHEME_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="CONTAINER_COLOR_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="PALLET_SIZE_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="HAS_PRINT_FILE_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="HAS_TK_FILE_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="HAS_STICKER_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="REWORK_CNT_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="CREATOR_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="DESIGNER_NAME_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
            };
            HistoryGrid.SetColumns(columns);

            HistoryGrid.SetSorting("AUDIT_DTTM", ListSortDirection.Ascending);
            HistoryGrid.SetPrimaryKey("_ROWNUMBER");
            HistoryGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            HistoryGrid.Commands = Commander;
            
            HistoryGrid.OnLoadItems = LoadItems;

            HistoryGrid.UseProgressSplashAuto = false;
            HistoryGrid.Init();
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        public async void LoadItems()
        {

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "GetHistoryTechCard");

            q.Request.SetParam("ID", IdTk.ToString());

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

                    var lastRow = new Dictionary<string, string>();
                    lastRow = ds.Items[0];
                    foreach (var item in  ds.Items)
                    {
                        if (item.CheckGet("STATUS") != lastRow.CheckGet("STATUS"))
                        {
                            item.CheckAdd("STATUS_F", "1");
                        }
                        else
                        {
                            item.CheckAdd("STATUS_F", "0");
                        }
                        if (item.CheckGet("DESIGN") != lastRow.CheckGet("DESIGN"))
                        {
                            item.Add("DESIGN_F", "1");
                        }
                        else
                        {
                            item.Add("DESIGN_F", "0");
                        }
                        if (item.CheckGet("SKU") != lastRow.CheckGet("SKU"))
                        {
                            item.Add("SKU_F", "1");
                        }
                        else
                        {
                            item.Add("SKU_F", "0");
                        }
                        if (item.CheckGet("NAME") != lastRow.CheckGet("NAME"))
                        {
                            item.Add("NAME_F", "1");
                        }
                        else
                        {
                            item.Add("NAME_F", "0");
                        }
                        if (item.CheckGet("BUYER_NAME") != lastRow.CheckGet("BUYER_NAME"))
                        {
                            item.Add("BUYER_NAME_F", "1");
                        }
                        else
                        {
                            item.Add("BUYER_NAME_F", "0");
                        }
                        if (item.CheckGet("CONTAINER_TYPE") != lastRow.CheckGet("CONTAINER_TYPE"))
                        {
                            item.Add("CONTAINER_TYPE_F", "1");
                        }
                        else
                        {
                            item.Add("CONTAINER_TYPE_F", "0");
                        }
                        if (item.CheckGet("PRODUCTION_SCHEME") != lastRow.CheckGet("PRODUCTION_SCHEME"))
                        {
                            item.Add("PRODUCTION_SCHEME_F", "1");
                        }
                        else
                        {
                            item.Add("PRODUCTION_SCHEME_F", "0");
                        }
                        if (item.CheckGet("CONTAINER_COLOR") != lastRow.CheckGet("CONTAINER_COLOR"))
                        {
                            item.Add("CONTAINER_COLOR_F", "1");
                        }
                        else
                        {
                            item.Add("CONTAINER_COLOR_F", "0");
                        }
                        if (item.CheckGet("PALLET_SIZE") != lastRow.CheckGet("PALLET_SIZE"))
                        {
                            item.Add("PALLET_SIZE_F", "1");
                        }
                        else
                        {
                            item.Add("PALLET_SIZE_F", "0");
                        }
                        if (item.CheckGet("HAS_PRINT_FILE") != lastRow.CheckGet("HAS_PRINT_FILE"))
                        {
                            item.Add("HAS_PRINT_FILE_F", "1");
                        }
                        else
                        {
                            item.Add("HAS_PRINT_FILE_F", "0");
                        }
                        if (item.CheckGet("HAS_TK_FILE") != lastRow.CheckGet("HAS_TK_FILE"))
                        {
                            item.Add("HAS_TK_FILE_F", "1");
                        }
                        else
                        {
                            item.Add("HAS_TK_FILE_F", "0");
                        }
                        if (item.CheckGet("HAS_STICKER") != lastRow.CheckGet("HAS_STICKER"))
                        {
                            item.Add("HAS_STICKER_F", "1");
                        }
                        else
                        {
                            item.Add("HAS_STICKER_F", "0");
                        }
                        if (item.CheckGet("REWORK_CNT") != lastRow.CheckGet("REWORK_CNT"))
                        {
                            item.Add("REWORK_CNT_F", "1");
                        }
                        else
                        {
                            item.Add("REWORK_CNT_F", "0");
                        }
                        if (item.CheckGet("CREATOR") != lastRow.CheckGet("CREATOR"))
                        {
                            item.Add("CREATOR_F", "1");
                        }
                        else
                        {
                            item.Add("CREATOR_F", "0");
                        }
                        if (item.CheckGet("DESIGNER_NAME") != lastRow.CheckGet("DESIGNER_NAME"))
                        {
                            item.Add("DESIGNER_NAME_F", "1");
                        }
                        else
                            {
                                item.Add("DESIGNER_NAME_F", "0");
                            }


                        lastRow = new Dictionary<string, string>(item);
                    }
                    HistoryGrid.UpdateItems(ds);
                }
            }
        }

        public void Show()
        {
            FrameName = $"{FrameName}_{IdTk}";

            Central.WM.FrameMode = 1;
            Central.WM.Show(FrameName, $"История изменений ТК {IdTk}", true, "add", this);
        }

        public void Close()
        {
            Central.WM.SetActive(ReceiverName);
            Central.WM.Close(FrameName);
        }
    }
}
