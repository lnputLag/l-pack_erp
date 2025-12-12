using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using System.IO;
using static Client.Interfaces.Main.DataGridHelperColumn;
using Client.Assets.HighLighters;
using System.Windows;
using Excel = Microsoft.Office.Interop.Excel;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using NPOI.SS.Util;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Форма привязывания техкарты 
    /// Страница веб-техкарты.
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class WebTechnologicalMapPinTk : ControlBase
    {
        public WebTechnologicalMapPinTk()
        {

            InitializeComponent();
            FrameMode = 2;
            FrameName = "WebTechnologicalMapPinTk";
            OnGetFrameTitle = () =>
            {
                return "Привязать техкарту";
            };
            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }

            };
            Commander.SetCurrentGroup("item");
            {

                Commander.Add(new CommandItem()
                {
                    Name = "save",
                    Group = "main",
                    Enabled = true,
                    Title = "Сохранить",
                    Description = "Привзать выбранную техкарту",
                    ButtonUse = true,
                    ButtonName = "SaveButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        var dw = new DialogWindow("Вы действительно хотите привязать техкарту?", "Привязывание техкарты", "", DialogWindowButtons.NoYes);
                        if ((bool)dw.ShowDialog())
                        {
                            Save();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;

                        if (row.CheckGet("ID_TK").ToInt() > 0)
                        {
                            result = true;
                        }
                        return result;
                    }
                });
                            
                Commander.Add(new CommandItem()
                {
                    Name = "close",
                    Group = "main",
                    Enabled = true,
                    Title = "Закрыть",
                    Description = "Закрыть форму",
                    ButtonUse = true,
                    ButtonName = "CancelButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Close();
                    },
                });
            }
            Commander.Init(this);
            OnLoad = () =>
            {
                GridInit();
            };
        }


        public int TkId { get; set; }
        public string ReceiverName { get; set; }

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        private void GridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Покупатель",
                    Path="NAME_POK",
                    ColumnType=ColumnTypeRef.String,
                    Width2=26,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="ART",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="Вид изделия",
                    Path="NAME_PCLASS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                },
                new DataGridHelperColumn
                {
                    Header="Картон",
                    Path="CARDBOARD",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                },
                new DataGridHelperColumn
                {
                    Header="Спецкартон",
                    Path="SPECIAL_RAW",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="ID_TK",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header="ID_PROF_TK_NEW",
                    Path="ID_PROF_TK_NEW",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="ID_PROF_TK_OLD",
                    Path="ID_PROF_TK_OLD",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="ID_MARKA_TK_NEW",
                    Path="ID_MARKA_TK_NEW",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="ID_MARKA_TK_OLD",
                    Path="ID_MARKA_TK_OLD",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="ID_OTGR_TK_NEW",
                    Path="ID_OTGR_TK_NEW",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="ID_OTGR_TK_OLD",
                    Path="ID_OTGR_TK_OLD",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="PRINTING_TK_NEW",
                    Path="PRINTING_TK_NEW",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="PRINTING_TK_OLD",
                    Path="PRINTING_TK_OLD",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="ID_PCLASS_TK_NEW",
                    Path="ID_PCLASS_TK_NEW",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="ID_PCLASS_TK_OLD",
                    Path="ID_PCLASS_TK_OLD",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="L_TK_NEW",
                    Path="L_TK_NEW",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="L_TK_OLD",
                    Path="L_TK_OLD",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="B_TK_NEW",
                    Path="B_TK_NEW",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="B_TK_OLD",
                    Path="B_TK_OLD",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="H_TK_NEW",
                    Path="H_TK_NEW",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="H_TK_OLD",
                    Path="H_TK_OLD",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="ID_OUTER_TK_NEW",
                    Path="ID_OUTER_TK_NEW",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="ID_OUTER_TK_OLD",
                    Path="ID_OUTER_TK_OLD",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="IDC_TK_NEW",
                    Path="IDC_TK_NEW",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="IDC_TK_OLD",
                    Path="IDC_TK_OLD",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="COL1_TK_NEW",
                    Path="COL1_TK_NEW",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="COL1_TK_OLD",
                    Path="COL1_TK_OLD",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="COL2_TK_NEW",
                    Path="COL2_TK_NEW",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="COL2_TK_OLD",
                    Path="COL2_TK_OLD",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="COL3_TK_NEW",
                    Path="COL3_TK_NEW",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="COL3_TK_OLD",
                    Path="COL3_TK_OLD",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="COL4_TK_NEW",
                    Path="COL4_TK_NEW",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="COL4_TK_OLD",
                    Path="COL4_TK_OLD",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="COL5_TK_NEW",
                    Path="COL5_TK_NEW",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="COL5_TK_OLD",
                    Path="COL5_TK_OLD",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="CUSTOMER_CARTON_TK_NEW",
                    Path="CUSTOMER_CARTON_TK_NEW",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="CUSTOMER_CARTON_TK_OLD",
                    Path="CUSTOMER_CARTON_TK_OLD",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="CUSTOMER_CARTON_TK_NEW",
                    Path="SPECIAL_RAW_TK_NEW",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="SPECIAL_RAW_TK_OLD",
                    Path="SPECIAL_RAW_TK_OLD",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="COLOR_IN_1_TK_NEW",
                    Path="COLOR_IN_1_TK_NEW",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="COLOR_IN_1_TK_OLD",
                    Path="COLOR_IN_1_TK_OLD",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="COLOR_IN_2_TK_NEW",
                    Path="COLOR_IN_2_TK_NEW",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="COLOR_IN_2_TK_OLD",
                    Path="COLOR_IN_2_TK_OLD",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false,
                },
            };
            Grid.SetColumns(columns);

            Grid.SetPrimaryKey("ID_TK");
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.Toolbar = GridToolbar;
            Grid.Commands = Commander;
            Grid.SearchText = GridSearch;
            Grid.OnLoadItems = LoadItems;


            Grid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=System.Windows.DependencyProperty.UnsetValue;
                        var color = "";
                        if(
                            row.CheckGet("SPECIAL_RAW_TK_NEW") != row.CheckGet("SPECIAL_RAW_TK_OLD")
                            && row.CheckGet("ID_PROF_TK_NEW") != row.CheckGet("ID_PROF_TK_OLD")
                            && row.CheckGet("ID_MARKA_TK_NEW") != row.CheckGet("ID_MARKA_TK_OLD")
                            && row.CheckGet("ID_OTGR_TK_NEW") != row.CheckGet("ID_OTGR_TK_OLD")
                            && row.CheckGet("PRINTING_TK_NEW") != row.CheckGet("PRINTING_TK_OLD")
                            && row.CheckGet("ID_PCLASS_TK_NEW") != row.CheckGet("ID_PCLASS_TK_OLD")
                            && row.CheckGet("ID_OUTER_TK_NEW") != row.CheckGet("ID_OUTER_TK_OLD")
                            && row.CheckGet("IDC_TK_NEW") != row.CheckGet("IDC_TK_OLD")
                            && row.CheckGet("L_TK_NEW") != row.CheckGet("L_TK_OLD")
                            && row.CheckGet("B_TK_NEW") != row.CheckGet("B_TK_OLD")
                            && row.CheckGet("H_TK_NEW") != row.CheckGet("H_TK_OLD")
                            && row.CheckGet("COL1_TK_NEW") != row.CheckGet("COL1_TK_OLD")
                            && row.CheckGet("COL2_TK_NEW") != row.CheckGet("COL2_TK_OLD")
                            && row.CheckGet("COL3_TK_NEW") != row.CheckGet("COL3_TK_OLD")
                            && row.CheckGet("COL4_TK_NEW") != row.CheckGet("COL4_TK_OLD")
                            && row.CheckGet("COL5_TK_NEW") != row.CheckGet("COL5_TK_OLD")
                            && row.CheckGet("CUSTOMER_CARTON_TK_NEW") != row.CheckGet("CUSTOMER_CARTON_TK_OLD")

                        )
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

            Grid.Init();
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        public async void LoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "ListForPinTk");
            q.Request.SetParam("ID_TK", TkId.ToString());

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
                    Grid.UpdateItems(ds);
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        public async void Save()
        {
            var row = Grid.SelectedItem;
            var reason = "";
            if(row.CheckGet("SPECIAL_RAW_TK_NEW") != row.CheckGet("SPECIAL_RAW_TK_OLD"))
            {
                reason = reason + ", спецкартону";
            }
            if (row.CheckGet("ID_PROF_TK_NEW") != row.CheckGet("ID_PROF_TK_OLD"))
            {
                reason = reason + ", профилю";
            }
            if (row.CheckGet("ID_MARKA_TK_NEW") != row.CheckGet("ID_MARKA_TK_OLD"))
            {
                reason = reason + ", марке";
            }
            if (row.CheckGet("ID_OTGR_TK_NEW") != row.CheckGet("ID_OTGR_TK_OLD"))
            {
                reason = reason + ", упаковке";
            }
            if (row.CheckGet("PRINTING_TK_NEW") != row.CheckGet("PRINTING_TK_OLD"))
            {
                reason = reason + ", печати";
            }
            if (row.CheckGet("ID_PCLASS_TK_NEW") != row.CheckGet("ID_PCLASS_TK_OLD"))
            {
                reason = reason + ", виду изделия";
            }
            if (row.CheckGet("ID_OUTER_TK_NEW") != row.CheckGet("ID_OUTER_TK_OLD"))
            {
                reason = reason + ", цвету";
            }
            if (row.CheckGet("IDC_TK_NEW") != row.CheckGet("IDC_TK_OLD"))
            {
                reason = reason + ", типу картона";
            }
            if (row.CheckGet("L_TK_NEW") != row.CheckGet("L_TK_OLD")
                || row.CheckGet("B_TK_NEW") != row.CheckGet("B_TK_OLD")
                || row.CheckGet("H_TK_NEW") != row.CheckGet("H_TK_OLD"))
            {
                reason = reason + ", размеру";
            }

            if (row.CheckGet("COL1_TK_NEW") != row.CheckGet("COL1_TK_OLD")
                || row.CheckGet("COL2_TK_NEW") != row.CheckGet("COL2_TK_OLD")
                || row.CheckGet("COL3_TK_NEW") != row.CheckGet("COL3_TK_OLD")
                || row.CheckGet("COL4_TK_NEW") != row.CheckGet("COL4_TK_OLD")
                || row.CheckGet("COL5_TK_NEW") != row.CheckGet("COL5_TK_OLD")
                || row.CheckGet("COLOR_IN_1_TK_NEW") != row.CheckGet("COLOR_IN_1_TK_OLD")
                || row.CheckGet("COLOR_IN_2_TK_NEW") != row.CheckGet("COLOR_IN_2_TK_OLD"))
            {
                reason = reason + ", пантонам";
            }
            if (row.CheckGet("CUSTOMER_CARTON_TK_NEW") != row.CheckGet("CUSTOMER_CARTON_TK_OLD"))
            {
                reason = reason + ", марке артикула";
            }

            if (reason.Length > 2)
            {
                reason = reason.Substring(2);
            }

            bool resume = false;
            if (reason == "")
            {
                resume = true;
            }
            else
            {
                var dw = new DialogWindow($"Техкарта не совпадает по {reason}. Вы действительно хотите привязать данную техкарту?", "Привязывание техкарты", "", DialogWindowButtons.NoYes);
                if ((bool)dw.ShowDialog())
                {
                    resume = true;
                }
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "WebTechnologicalMap");
                q.Request.SetParam("Action", "AddNewTk");
                q.Request.SetParam("ID_TK", TkId.ToString());
                q.Request.SetParam("ID_TK_NEW", Grid.SelectedItem.CheckGet("ID_TK").ToString());

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
                        if (ds.Items[0].CheckGet("SUCCESS").ToInt() == 1)
                        {
                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverGroup = "WebTechnologicalMap",
                                ReceiverName = ReceiverName,
                                SenderName = ControlName,
                                Action = "Refresh",
                            });
                            Close();
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }
        public void Close()
        {
            var frameName = GetFrameName();
            Central.WM.Close(frameName);
        }

    }
}
