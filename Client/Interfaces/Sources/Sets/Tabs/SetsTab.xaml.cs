using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Accounts;
using Client.Interfaces.Main;
using DevExpress.DirectX.StandardInterop.Direct2D;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;
using ControlBase = Client.Interfaces.Main.ControlBase;

namespace Client.Interfaces.Sources
{
    /// <summary>
    /// Форма отображения товаров и комплектов
    /// </summary>
    /// /// <author>lavrenteva_ma</author>
    public partial class SetsTab : ControlBase
    {
        public SetsTab()
        {
            InitializeComponent();

            ControlSection = "sets";
            RoleName = "[erp]sets";
            ControlTitle = "Комплекты";
            DocumentationUrl = "/doc/l-pack-erp-new/products_materials/set";

            AddedId = "";

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    switch (m.Action)
                    {
                        case "ReturnNscheme":
                            AddedId = m.Message;
                            goodsGridLoadItems();
                            break;
                        default:
                            break;
                    }
                        
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

            OnLoad = () =>
            {
                formInit();
                goodsGridInit();
                setsGridInit();
            };

            OnUnload = () =>
            {
                GoodsGrid.Destruct();
                SetsGrid.Destruct();
            };

            Commander.SetCurrentGridName("SetsGrid");
            {
                Commander.SetCurrentGroup("item");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "Refresh",
                        Group = "refresh",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить таблицу комплектов",
                        MenuUse = true,
                        Action = () =>
                        {
                            AddedId = "";
                            goodsGridLoadItems();
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "create",
                        Group = "crud",
                        Enabled = true,
                        Title = "Добавить главное изделие",
                        Description = "Добавить главное изделие",
                        ButtonUse = true,
                        ButtonName = "CreateSetButton",
                        MenuUse = true,
                        AccessLevel = Common.Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var i = new GoodsForm();
                            i.Nscheme = 0;
                            i.IdS = 0;
                            i.IsPrimary = 1;
                            i.Id2 = 0;
                            i.Count = 1;
                            i.Show();

                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "create_element",
                        Group = "crud",
                        Enabled = true,
                        Title = "Добавить комплектующие",
                        Description = "Добавить комплектующие",
                        ButtonUse = true,
                        ButtonName = "CreateSetElementButton",
                        MenuUse = true,
                        AccessLevel = Common.Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            int nscheme = SetsGrid.SelectedItem.CheckGet("NSHEMA").ToInt();
                            var i = new GoodsForm();
                            i.Nscheme = nscheme;
                            i.IdS = 0;
                            i.Id2 = 0;
                            i.IsPrimary = 0;
                            i.Count = 1;
                            i.Show();

                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = SetsGrid.SelectedItem;
                            if (row.CheckGet("PARENT").ToInt() == 1)
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "update",
                        Group = "crud",
                        Enabled = true,
                        Title = "Изменить",
                        Description = "Изменить комплект",
                        ButtonUse = true,
                        ButtonName = "EditSetButton",
                        MenuUse = true,
                        HotKey = "DoubleCLick",
                        AccessLevel = Common.Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            
                            int isParent = SetsGrid.SelectedItem.CheckGet("PARENT").ToInt();
                            int ids = SetsGrid.SelectedItem.CheckGet("IDS").ToInt();
                            int nscheme = SetsGrid.SelectedItem.CheckGet("NSHEMA").ToInt();
                            int id2 = SetsGrid.SelectedItem.CheckGet("ID2").ToInt();
                            int count = SetsGrid.SelectedItem.CheckGet("KOL").ToInt();
                            var i = new GoodsForm();
                            i.Nscheme = nscheme;
                            i.IdS = ids;
                            i.IsPrimary = isParent;
                            i.Id2 = id2;
                            i.Count = count;
                            i.Show();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var k = SetsGrid.GetPrimaryKey();
                            var row = SetsGrid.SelectedItem;
                            if (row.CheckGet(k).ToInt() != 0)
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "delete",
                        Group = "crud",
                        Enabled = true,
                        Title = "Удалить",
                        Description = "Удалить комплект",
                        ButtonUse = true,
                        ButtonName = "DeleteSetButton",
                        MenuUse = true,
                        AccessLevel = Common.Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            int isParent = SetsGrid.SelectedItem.CheckGet("PARENT").ToInt();
                            int ids = SetsGrid.SelectedItem.CheckGet("IDS").ToInt();
                            int nscheme = SetsGrid.SelectedItem.CheckGet("NSHEMA").ToInt();
                            if (isParent == 0)
                            {
                                SchemeDelete(ids, nscheme);
                            }
                            else
                            {
                                SetDelete(nscheme);
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var k = SetsGrid.GetPrimaryKey();
                            var row = SetsGrid.SelectedItem;
                            if (row.CheckGet(k).ToInt() != 0)
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "help",
                        Enabled = true,
                        Title = "Справка",
                        Description = "Показать справочную информацию",
                        ButtonUse = true,
                        ButtonName = "HelpButton",
                        HotKey = "F1",
                        Action = () =>
                        {
                            Central.ShowHelp(DocumentationUrl);
                        },
                    });
                }

            }

            Commander.Init(this);
        }
        public FormHelper Form { get; set; }
        private string AddedId {  get; set; }

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
                new FormHelperField()
                {
                    Path="SHOW_ALL",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ShowAll,
                    ControlType="CheckBox",
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
                    Header="ИД изделия",
                    Path="ID",
                    Width2=10,
                    ColumnType=ColumnTypeRef.Integer,
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
                    Header = "Количество комплектов",
                    Path="COUNT",
                    Doc="Количество включающих схем",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=18,
                },
            };
            GoodsGrid.SetColumns(columns);
            GoodsGrid.SetPrimaryKey("ID");
            GoodsGrid.SetSorting("NAME", ListSortDirection.Ascending);
            GoodsGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            
            GoodsGrid.Toolbar = GoodsGridToolbar;
            GoodsGrid.ItemsAutoUpdate = false;
            GoodsGrid.Commands = Commander;

            GoodsGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        var count = row.CheckGet("COUNT").ToInt();
                        if (count > 1)
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
                {
                    DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var foreColor = "";

                        var name = row.CheckGet("NAME").ToString();
                        if (name!="" && name[0] == '*')
                        {
                            foreColor = HColor.Olive;
                        }

                        if (!string.IsNullOrEmpty(foreColor))
                        {
                            result=foreColor.ToBrush();
                        }

                        return result;
                    }
                }
            };
            GoodsGrid.OnSelectItem = (row) =>
            {
               setsGridLoadItems();
            };
            GoodsGrid.OnFilterItems = () =>
            {
                if (GoodsGrid.Items.Count > 0)
                {
                    var showAll = false;
                    var v = Form.GetValues();
                    if (v.CheckGet("SHOW_ALL").ToBool())
                    {
                        showAll = true;
                    }
                    var items = new List<Dictionary<string, string>>();
                    foreach (Dictionary<string, string> row in GoodsGrid.Items)
                    {
                        var name = row.CheckGet("NAME").ToString();
                        var isArchive = false;
                        if (name[0] == '*')
                        {
                            isArchive = true;
                        }
                        if (
                            showAll
                            || !isArchive
                        )
                        {
                            items.Add(row);
                        }

                    }
                    GoodsGrid.Items = items;

                }
            };
            GoodsGrid.Init();
        }
        private void setsGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Номер",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="Id2",
                    Path="ID2",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="IDS",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="ИД комплекта",
                    Path="NSHEMA",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="PARENT",
                    Path="PARENT",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="ARTIKUL",
                    ColumnType=ColumnTypeRef.String,
                    Width2=21,
                    DxEnableColumnSorting=false,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="NAME",
                    Doc="Наименование товара",
                    ColumnType=ColumnTypeRef.String,
                    Width2=41,
                    DxEnableColumnSorting=false,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество",
                    Path="KOL",
                    Doc="Количество включающих схем",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=11,
                    DxEnableColumnSorting=false,
                },
            };
            SetsGrid.SetColumns(columns);
            SetsGrid.SetPrimaryKey("_ROWNUMBER");
            SetsGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            SetsGrid.Toolbar = SetsToolbar;
            SetsGrid.ItemsAutoUpdate = false;
            SetsGrid.Commands = Commander;
            SetsGrid.Init();
        }

        private async void setsGridLoadItems()
        {
            bool resume = true;

            if (resume)
            {
                int goodsId = GoodsGrid.SelectedItem.CheckGet("ID").ToInt();
                if (goodsId > 0)
                {
                    var p = new Dictionary<string, string>();
                    {
                        p.CheckAdd("ID", goodsId.ToString());
                    }
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sources/Sets");
                    q.Request.SetParam("Object", "ProductionScheme");
                    q.Request.SetParam("Action", "ListById");
                    q.Request.SetParams(p);

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
                            {
                                var ds = ListDataSet.Create(result, "ITEMS");

                                var modeList = Acl.GetAccessModeList();
                                ds.Items = ListDataSet.AddColumnToList(ds.Items, "DATA_ACCESS_MODE", "_MODE_NAME", modeList);
                                foreach (var item in ds.Items)
                                {
                                    if (item["PARENT"].ToInt() == 1)
                                    {
                                        item["ARTIKUL"] = "|-⯈" + item["ARTIKUL"];

                                    }
                                    else
                                    {
                                        item["ARTIKUL"] = "|    |-⯈" + item["ARTIKUL"];
                                    }
                                }
                                SetsGrid.UpdateItems(ds);

                            }
                        }
                    }
                }

            }
        }
        private async void goodsGridLoadItems()
        {
            bool resume = true;
            
            string goodsSearch = "%" + GoodsGridSearch.Text + "%";
            if (goodsSearch.Length != null && goodsSearch.Length >= 5 || AddedId != "")
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("TEXT", goodsSearch.ToString());
                    p.CheckAdd("ADDED_ID", AddedId);
                }
                if (resume)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sources/Sets");
                    q.Request.SetParam("Object", "Goods");
                    q.Request.SetParam("Action", "List");
                    q.Request.SetParams(p);
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
                            {
                                var ds = ListDataSet.Create(result, "ITEMS");

                                var modeList = Acl.GetAccessModeList();
                                ds.Items = ListDataSet.AddColumnToList(ds.Items, "DATA_ACCESS_MODE", "_MODE_NAME", modeList);

                                GoodsGrid.UpdateItems(ds);
                                GoodsGridSearchButton.Style = (Style)GoodsGridSearch.TryFindResource("Button");

                            }
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }
            else
            {
                var dw = new DialogWindow("Для загрузки данных введите часть наименования или артикула (как минимум 3 символа).", "Список товаров");
                dw.ShowDialog();
            }
        }

        private async void SetDelete(int nscheme)
        {
            // Подтверждение на удаление
            bool resume = false;
            string name = SetsGrid.SelectedItem.CheckGet("NAME").ToString();
            string artikul = SetsGrid.SelectedItem.CheckGet("ARTIKUL").ToString();

            var dw = new DialogWindow("Вы действительно хотите удалить комплект? \r\nАртикул: " + artikul + "\r\nНаиименование: " + name, "Удаление комплекта", "", DialogWindowButtons.NoYes);
            if ((bool)dw.ShowDialog())
            {
                resume = dw.ResultButton == DialogResultButton.Yes;
            }
            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sources/Sets");
                q.Request.SetParam("Object", "ProductionScheme");
                q.Request.SetParam("Action", "DeleteMaster");
                q.Request.SetParam("NSHEMA", nscheme.ToString());

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    goodsGridLoadItems();
                    setsGridLoadItems();
                }
                else if (q.Answer.Error.Code == 145)
                {
                    q.ProcessError();
                }
            }
        }
        private async void SchemeDelete(int ids, int nscheme)
        {
            // Подтверждение на удаление
            bool resume = false;
            string name = SetsGrid.SelectedItem.CheckGet("NAME").ToString();
            string artikul = SetsGrid.SelectedItem.CheckGet("ARTIKUL").ToString();

            var dw = new DialogWindow("Вы действительно хотите удалить элемент схемы? \r\nАртикул: " + artikul + "\r\nНаименование: " + name, "Удаление элемеента схемы", "", DialogWindowButtons.NoYes);
            if ((bool)dw.ShowDialog())
            {
                resume = dw.ResultButton == DialogResultButton.Yes;
            }
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("IDS", ids.ToString());
                p.CheckAdd("NSCHEME", nscheme.ToString());
            }
            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sources/Sets");
                q.Request.SetParam("Object", "ProductionScheme");
                q.Request.SetParam("Action", "DeleteSlave");
                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    goodsGridLoadItems();
                    setsGridLoadItems();
                }
                else if (q.Answer.Error.Code == 145)
                {
                    q.ProcessError();
                }
            }
        }

        private void goodsGridSearchButtonClick(object sender, RoutedEventArgs e)
        {
            goodsGridLoadItems();
        }
        private void showArchiveClick(object sender, RoutedEventArgs e)
        {
            GoodsGrid.UpdateItems();
        }
        private void GoodsGridSearchTextChanged(object sender, RoutedEventArgs e)
        {
            AddedId = "";
            if (GoodsGridSearch.Text.Length >= 3)
            {
                GoodsGridSearchButton.Style = (Style)GoodsGridSearch.TryFindResource("FButtonPrimary");
            }
            else
            {
                GoodsGridSearchButton.Style = (Style)GoodsGridSearch.TryFindResource("Button");
                
            }
        }
        private void HelpButtonClick(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp-new/products_materials/set");
        }
    }
}
