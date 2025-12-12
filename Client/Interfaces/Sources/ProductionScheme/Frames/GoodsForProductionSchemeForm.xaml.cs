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

namespace Client.Interfaces.Sources
{
    /// <summary>
    /// Форма выбора товара для создания или редактирования схемы производства
    /// Страница схем производств.
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class GoodsForProductionSchemeForm : ControlBase
    {
        public GoodsForProductionSchemeForm()
        {

            InitializeComponent();
            FrameName = "goodsforproductionschemeform_";
            OnGetFrameTitle = () =>
            {
                return "Изделие из схемы";
            };
            //ControlName = "goodsforproductionschemeform_";
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
                        var id2 = GoodsGrid.SelectedItem.CheckGet("ID2").ToInt();
                        if (id2 != 0)
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
                goodsGridInit();
                GoodsGridSearch.Focus();
            };
        }

        public int Id2 { get; set; }
        public int Idk1 { get; set; }
        public int Idtk { get; set; }
        public int Topnode { get; set; }
        public string Artikul { get; set; }
        public string Name { get; set; }
        public DispatcherTimer TemplateTimeoutTimer;

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
                    Control=GoodsGridSearch,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
            };
            Form.SetFields(fields);
        }
        private void goodsGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="id2",
                    Path="ID2",
                    Width2=6,
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="idk1",
                    Path="IDK1",
                    Width2=6,
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="ARTIKUL",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="NAME",
                    Doc="Наименование товара",
                    ColumnType=ColumnTypeRef.String,
                    Width2=48,
                },
                new DataGridHelperColumn
                {
                    Header="id_tk",
                    Path="ID_TK",
                    Width2=6,
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false
                },
            };
            GoodsGrid.SetColumns(columns);
            GoodsGrid.SetPrimaryKey("ID2");
            GoodsGrid.SetSorting("NAME", ListSortDirection.Ascending);
            GoodsGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            GoodsGrid.Toolbar = GoodsGridToolbar;
            GoodsGrid.ItemsAutoUpdate = false;
            GoodsGrid.Commands = Commander;
            GoodsGrid.OnSelectItem = (selectItem) =>
            {
                Id2 = GoodsGrid.SelectedItem.CheckGet("ID2").ToInt();
                Idk1 = GoodsGrid.SelectedItem.CheckGet("IDK1").ToInt(); 
                Idtk = GoodsGrid.SelectedItem.CheckGet("ID_TK").ToInt();
                Name = GoodsGrid.SelectedItem.CheckGet("NAME").ToString();
                Artikul = GoodsGrid.SelectedItem.CheckGet("ARTIKUL").ToString();
            };
            GoodsGrid.OnLoadItems = goodsGridLoadItems;
            GoodsGrid.Init();
        }

        private async void goodsGridLoadItems()
        {
            var text = "%" + GoodsGridSearch.Text + "%";
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("TOPNODE", Topnode.ToString());
                p.CheckAdd("TEXT", text.ToString());
            }
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sources/ProductionScheme");
            q.Request.SetParam("Object", "Goods");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        GoodsGrid.UpdateItems(ds);
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
            m.CheckAdd("ID2", Id2.ToString());
            m.CheckAdd("IDK1", Idk1.ToString());
            m.CheckAdd("NAME", Name.ToString());
            m.CheckAdd("ARTIKUL", Artikul.ToString());
            m.CheckAdd("ID_TK", Idtk.ToString());
            Central.Msg.SendMessage(new ItemMessage()
            {
                ReceiverName = "ProductionSchemeForm",
                SenderName = ControlName,
                Action = "ReturnGoods",
                ContextObject = m,
            });
            Close();
        }
        public void Close()
        {
            Central.WM.Close(FrameName);
        }

        private void RunTemplateTimeoutTimer()
        {
            if (TemplateTimeoutTimer == null)
            {
                TemplateTimeoutTimer = new DispatcherTimer
                {
                    Interval = new TimeSpan(0, 0, 2)
                };

                {
                    var row = new Dictionary<string, string>();
                    row.CheckAdd("TIMEOUT", "2000");
                    row.CheckAdd("DESCRIPTION", "");
                    Central.Stat.TimerAdd("AssortmentList_RunTemplateTimeoutTimer", row);
                }

                TemplateTimeoutTimer.Tick += (s, e) =>
                {
                    GoodsGrid.LoadItems();
                    StopTemplateTimeoutTimer();
                };
            }

            if (TemplateTimeoutTimer.IsEnabled)
            {
                TemplateTimeoutTimer.Stop();
            }
            TemplateTimeoutTimer.Start();
        }
        private void StopTemplateTimeoutTimer()
        {
            if (TemplateTimeoutTimer != null)
            {
                if (TemplateTimeoutTimer.IsEnabled)
                {
                    TemplateTimeoutTimer.Stop();
                }
            }
        }

        private void goodsRefreshButtonClick(object sender, RoutedEventArgs e)
        {
            goodsGridLoadItems();
        }

        private void GoodsGridSearchTextChanged(object sender, RoutedEventArgs e)
        {
            RunTemplateTimeoutTimer();
        }
    }
}
