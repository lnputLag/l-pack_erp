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
    /// История изменения веб-техкарты
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class WebTechnologicalMapHistory : ControlBase
    {
        public WebTechnologicalMapHistory()
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
                    Path="ID_TK",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible = false,
                },
                new DataGridHelperColumn
                {
                    Header="Дата изменения",
                    Path="AUDIT_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=14,
                    Format="dd.MM.yyyy HH:mm:ss",
                },
                new DataGridHelperColumn
                {
                    Header="Автор",
                    Path="AUDIT_USER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                },
                new DataGridHelperColumn
                {
                    Header="Площадка",
                    Path="FACTORY",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("FACTORY_F").ToInt() == 1 )
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
                    Header="Номер",
                    Path="NUM",
                    ColumnType=ColumnTypeRef.String,
                    Width2=29,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("NUM_F").ToInt() == 1 )
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
                    Header="Статус",
                    Path="STATUS",
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
                    Width2=9,
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
                    Header="Чертеж",
                    Path="DRAWING",
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

                                if( row.CheckGet("DRAWING_F").ToInt() == 1 )
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
                    Header="Размеры",
                    Path="PRODUCT_SIZE",
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

                                if( row.CheckGet("PRODUCT_SIZE_F").ToInt() == 1 )
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
                    Header="Размеры заготовки",
                    Path="BLANK_SIZE",
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

                                if( row.CheckGet("BLANK_SIZE_F").ToInt() == 1 )
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
                    Header="Вид изделия",
                    Path="TYPE_PRODUCT",
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

                                if( row.CheckGet("TYPE_PRODUCT_F").ToInt() == 1 )
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
                    Header="Картон",
                    Path="DESCRIPTION",
                    ColumnType=ColumnTypeRef.String,
                    Width2=7,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("DESCRIPTION_F").ToInt() == 1 )
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
                    Header="Тип картона",
                    Path="CARTON",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("CARTON_F").ToInt() == 1 )
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
                    Header="Марка для артикула",
                    Path="MARKA",
                    ColumnType=ColumnTypeRef.String,
                    Width2=4,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("MARKA_F").ToInt() == 1 )
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
                    Header="Спецкартон",
                    Path="SPECIAL_RAW",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("SPECIAL_RAW_F").ToInt() == 1 )
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
                    Header="Печать",
                    Path="PRINTING",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("PRINTING_F").ToInt() == 1 )
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
                    Header="Цвета",
                    Path="COLORS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                    Options="valuestripbr",
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("COLORS_F").ToInt() == 1 )
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
                    Header="Упаковка",
                    Path="ID_OTGR",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("ID_OTGR_F").ToInt() == 1 )
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
                    Header="Автосборка",
                    Path="AUTOMATIC",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("AUTOMATIC_F").ToInt() == 1 )
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
                    Header="Тип рилевки",
                    Path="TYPE_CREASE",
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

                                if( row.CheckGet("TYPE_CREASE_F").ToInt() == 1 )
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
                    Header="Согласована с клиентом",
                    Path="AGREED_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("AGREED_FLAG_F").ToInt() == 1 )
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
                    Header="Подписанный файл техкарты",
                    Path="FILE_SIGNED_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("FILE_SIGNED_FLAG_F").ToInt() == 1 )
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
                    Header="Канал продаж",
                    Path="TYPE_CUSTOMER",
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

                                if( row.CheckGet("TYPE_CUSTOMER_F").ToInt() == 1 )
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
                    Path="ID_TK_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Флаг изменения",
                    Path="AUDIT_DTTM_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="AUDIT_USER_NAME_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="FACTORY_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="NUM_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="STATUS_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="DESIGN_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="DRAWING_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="PRODUCT_SIZE_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="BLANK_SIZE_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="TYPE_PRODUCT_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="DESCRIPTION_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="CARTON_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="MARKA_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="SPECIAL_RAW_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="PRINTING_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="COLORS_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="ID_OTGR_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="AUTOMATIC_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="TYPE_CREASE_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="AGREED_FLAG_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="FILE_SIGNED_FLAG_F",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Path="TYPE_CUSTOMER_F",
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
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "GetHistory");

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
                    var newDs = new ListDataSet();
                    if (ds != null && ds.Items.Count>0)
                    {
                        newDs.Cols = ds.Cols;

                        var lastRow = new Dictionary<string, string>();
                        lastRow = ds.Items[0];
                        newDs.Items.Add(lastRow);
                        for (int i = 1; i < ds.Items.Count; i++)
                        {
                            var save = false;
                            var t = ds.Items[i].Keys;
                            foreach (var key in ds.Items[i].Keys)
                            {
                                if (key != "ID_TK" && key != "AUDIT_DTTM" && key != "AUDIT_USER_NAME" && key != "_ROWNUMBER")
                                {
                                    if (lastRow.CheckGet(key) != ds.Items[i].CheckGet(key))
                                    {
                                        save = true;
                                        break;
                                    }
                                }
                            }
                            if (save)
                            {
                                newDs.Items.Add(ds.Items[i]);
                            }

                            lastRow = ds.Items[i];
                        }

                        lastRow = newDs.Items[0];
                        foreach (var item in newDs.Items)
                        {
                            if (item.CheckGet("FACTORY") != lastRow.CheckGet("FACTORY"))
                            {
                                item.CheckAdd("FACTORY_F", "1");
                            }
                            else
                            {
                                item.CheckAdd("FACTORY_F", "0");
                            }
                            if (item.CheckGet("NUM") != lastRow.CheckGet("NUM"))
                            {
                                item.CheckAdd("NUM_F", "1");
                            }
                            else
                            {
                                item.CheckAdd("NUM_F", "0");
                            }
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
                                item.CheckAdd("DESIGN_F", "1");
                            }
                            else
                            {
                                item.CheckAdd("DESIGN_F", "0");
                            }
                            if (item.CheckGet("DRAWING") != lastRow.CheckGet("DRAWING"))
                            {
                                item.CheckAdd("DRAWING_F", "1");
                            }
                            else
                            {
                                item.CheckAdd("DRAWING_F", "0");
                            }
                            if (item.CheckGet("PRODUCT_SIZE") != lastRow.CheckGet("PRODUCT_SIZE"))
                            {
                                item.CheckAdd("PRODUCT_SIZE_F", "1");
                            }
                            else
                            {
                                item.CheckAdd("PRODUCT_SIZE_F", "0");
                            }
                            if (item.CheckGet("BLANK_SIZE") != lastRow.CheckGet("BLANK_SIZE"))
                            {
                                item.CheckAdd("BLANK_SIZE_F", "1");
                            }
                            else
                            {
                                item.CheckAdd("BLANK_SIZE_F", "0");
                            }
                            if (item.CheckGet("TYPE_PRODUCT") != lastRow.CheckGet("TYPE_PRODUCT"))
                            {
                                item.CheckAdd("TYPE_PRODUCT_F", "1");
                            }
                            else
                            {
                                item.CheckAdd("TYPE_PRODUCT_F", "0");
                            }
                            if (item.CheckGet("DESCRIPTION") != lastRow.CheckGet("DESCRIPTION"))
                            {
                                item.CheckAdd("DESCRIPTION_F", "1");
                            }
                            else
                            {
                                item.CheckAdd("DESCRIPTION_F", "0");
                            }
                            if (item.CheckGet("CARTON") != lastRow.CheckGet("CARTON"))
                            {
                                item.CheckAdd("CARTON_F", "1");
                            }
                            else
                            {
                                item.CheckAdd("CARTON_F", "0");
                            }
                            if (item.CheckGet("MARKA") != lastRow.CheckGet("MARKA"))
                            {
                                item.CheckAdd("MARKA_F", "1");
                            }
                            else
                            {
                                item.CheckAdd("MARKA_F", "0");
                            }
                            if (item.CheckGet("SPECIAL_RAW") != lastRow.CheckGet("SPECIAL_RAW"))
                            {
                                item.CheckAdd("SPECIAL_RAW_F", "1");
                            }
                            else
                            {
                                item.CheckAdd("SPECIAL_RAW_F", "0");
                            }
                            if (item.CheckGet("PRINTING") != lastRow.CheckGet("PRINTING"))
                            {
                                item.CheckAdd("PRINTING_F", "1");
                            }
                            else
                            {
                                item.CheckAdd("PRINTING_F", "0");
                            }
                            if (item.CheckGet("COLORS") != lastRow.CheckGet("COLORS"))
                            {
                                item.CheckAdd("COLORS_F", "1");
                            }
                            else
                            {
                                item.CheckAdd("COLORS_F", "0");
                            }
                            if (item.CheckGet("ID_OTGR") != lastRow.CheckGet("ID_OTGR"))
                            {
                                item.CheckAdd("ID_OTGR_F", "1");
                            }
                            else
                            {
                                item.CheckAdd("ID_OTGR_F", "0");
                            }
                            if (item.CheckGet("AUTOMATIC") != lastRow.CheckGet("AUTOMATIC"))
                            {
                                item.CheckAdd("AUTOMATIC_F", "1");
                            }
                            else
                            {
                                item.CheckAdd("AUTOMATIC_F", "0");
                            }
                            if (item.CheckGet("TYPE_CREASE") != lastRow.CheckGet("TYPE_CREASE"))
                            {
                                item.CheckAdd("TYPE_CREASE_F", "1");
                            }
                            else
                            {
                                item.CheckAdd("TYPE_CREASE_F", "0");
                            }
                            if (item.CheckGet("AGREED_FLAG") != lastRow.CheckGet("AGREED_FLAG"))
                            {
                                item.CheckAdd("AGREED_FLAG_F", "1");
                            }
                            else
                            {
                                item.CheckAdd("AGREED_FLAG_F", "0");
                            }
                            if (item.CheckGet("FILE_SIGNED_FLAG") != lastRow.CheckGet("FILE_SIGNED_FLAG"))
                            {
                                item.CheckAdd("FILE_SIGNED_FLAG_F", "1");
                            }
                            else
                            {
                                item.CheckAdd("FILE_SIGNED_FLAG_F", "0");
                            }
                            if (item.CheckGet("TYPE_CUSTOMER") != lastRow.CheckGet("TYPE_CUSTOMER"))
                            {
                                item.CheckAdd("TYPE_CUSTOMER_F", "1");
                            }
                            else
                            {
                                item.CheckAdd("TYPE_CUSTOMER_F", "0");
                            }


                            lastRow = new Dictionary<string, string>(item);
                        }
                    }
                    
                    HistoryGrid.UpdateItems(newDs);
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
