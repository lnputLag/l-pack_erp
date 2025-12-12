using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using DevExpress.DirectX.StandardInterop.Direct2D;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using SharpVectors.Dom;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Форма выбора товара для создания или редактирования схемы производства
    /// Страница схем производств.
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class TkForTkSetForm : ControlBase
    {
        public TkForTkSetForm()
        {

            InitializeComponent();
            OnGetFrameTitle = () =>
            {
                return "Выбор техкарты";
            };
            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }

            };
            Commander.SetCurrentGridName("main");
            Commander.SetCurrentGroup("item");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "save",
                    Group = "main_form",
                    Enabled = false,
                    Title = "Изменить",
                    Description = "Изменить",
                    ButtonUse = true,
                    ButtonName = "SaveButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Save();

                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var idTk = TkGrid.SelectedItem.CheckGet("ID_TK").ToInt();
                        if (idTk != 0)
                        {
                            result = true;
                        }
                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "close",
                    Group = "main_form",
                    Enabled = true,
                    Title = "Отмена",
                    Description = "Закрыть форму без сохранения",
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

                formInit();
                TkGridInit();
                TkGridSearch.Focus();
            };
        }

        public string ReciverName { get; set; }
        public string IdPclass { get; set; }
        public int CustId { get; set; }

        public FormHelper Form { get; set; }

        public void Show()
        {
            this.MinHeight = 150;
            this.MinWidth = 400;
            ControlTitle = "Изделие из схемы";
            Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
            windowParametrs.Add("no_resize", "1");
            windowParametrs.Add("center_screen", "1");
            Central.WM.FrameMode = 2;
            Central.WM.Show(FrameName, ControlTitle, true, "add", this, "top", windowParametrs);
        }
        private void formInit()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="SEARCH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TkGridSearch,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
            };
            Form.SetFields(fields);
        }
        private void TkGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="ИД ТК",
                    Path="ID_TK",
                    Width2=8,
                    ColumnType=ColumnTypeRef.Integer,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=50,
                },
                new DataGridHelperColumn
                {
                    Header="Дата создания",
                    Path="CREATED_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=14,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="Создатель",
                    Path="CREATOR",
                    ColumnType=ColumnTypeRef.String,
                    Width2=14,
                },
                new DataGridHelperColumn
                {
                    Header="Тип",
                    Path="ID_PCLASS",
                    Width2=6,
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="Покупатель",
                    Path="CUSTOMER",
                    ColumnType=ColumnTypeRef.String,
                    Width2=48,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="Файл ТК",
                    Path="PATHTK",
                    ColumnType=ColumnTypeRef.String,
                    Width2=48,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="ИД покупателя",
                    Path="CUST_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=48,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="Имя покупателя",
                    Path="CUSTOMER_SHORT",
                    ColumnType=ColumnTypeRef.String,
                    Width2=48,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=48,
                    Visible=false
                },
            };
            TkGrid.SetColumns(columns);
            TkGrid.SetPrimaryKey("ID_TK");
            TkGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            TkGrid.Toolbar = TkGridToolbar;
            TkGrid.SearchText = TkGridSearch;
            TkGrid.Commands = Commander;
            TkGrid.OnLoadItems = TkGridLoadItems;
            TkGrid.ItemsAutoUpdate = false;
            TkGrid.Init();
        }

        private async void TkGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_PCLASS", IdPclass.ToString());
                p.CheckAdd("CUST_ID", CustId.ToString());
            }
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "TechnologicalMapSets");
            q.Request.SetParam("Action", "ListTkByIdPclass");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        TkGrid.UpdateItems(ds);
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

        }
        public void Save()
        {
            Dictionary<string, string> m = new Dictionary<string, string>();
            m.CheckAdd("ID_PCLASS", TkGrid.SelectedItem.CheckGet("ID_PCLASS"));
            m.CheckAdd("NAME", TkGrid.SelectedItem.CheckGet("NAME"));
            m.CheckAdd("ARTIKUL", TkGrid.SelectedItem.CheckGet("ARTIKUL"));
            m.CheckAdd("PATHTK", TkGrid.SelectedItem.CheckGet("PATHTK"));
            m.CheckAdd("ID_TK", TkGrid.SelectedItem.CheckGet("ID_TK"));
            m.CheckAdd("CUST_ID", TkGrid.SelectedItem.CheckGet("CUST_ID"));
            m.CheckAdd("CUSTOMER_SHORT", TkGrid.SelectedItem.CheckGet("CUSTOMER_SHORT"));
            m.CheckAdd("QTY", TkGrid.SelectedItem.CheckGet("QTY"));

            Central.Msg.SendMessage(new ItemMessage()
            {
                ReceiverName = ReciverName,
                SenderName = ControlName,
                Action = "ReturnTk",
                ContextObject = m,
            });
            Close();
        }
        public void Close()
        {
            Central.WM.Close(FrameName);
        }

    }
}
