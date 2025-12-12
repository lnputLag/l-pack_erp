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
    /// Форма отображения товаров и их схем производства
    /// </summary>
    /// /// <author>lavrenteva_ma</author>
    public partial class ProductionSchemeTab : ControlBase
    {
        public ProductionSchemeTab()
        {
            InitializeComponent();

            ControlSection = "production_scheme";
            RoleName = "[erp]production_scheme";
            ControlTitle = "Схемы производства";
            DocumentationUrl = "/doc/l-pack-erp-new/products_materials/typeschema";

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    switch (m.Action)
                    {
                        case "ReturnId2":
                            GoodsGrid.SelectedItem.CheckAdd("ID2", m.Message);
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
                FormInit();
                GoodsGridInit();
                ProductionSchemeGridInit();
            };

            OnUnload = () =>
            {
                GoodsGrid.Destruct();
                ProductionSchemeGrid.Destruct();
            };

            Commander.SetCurrentGridName("ProductionSchemeGrid");
            {
                Commander.SetCurrentGroup("item");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "create",
                        Group = "crud",
                        Enabled = true,
                        Title = "Добавить",
                        Description = "Добавить схему производства",
                        ButtonUse = true,
                        ButtonName = "CreateProductionSchemeButton",
                        MenuUse = true,
                        AccessLevel = Common.Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var i = new ProductionSchemeForm();
                            i.Idtls = 0;
                            i.Idtscheme = 0;
                            i.IsCreated = true;
                            i.Show();

                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "update",
                        Group = "crud",
                        Enabled = true,
                        Title = "Изменить",
                        Description = "Изменить схему производства",
                        ButtonUse = true,
                        ButtonName = "EditProductionSchemeButton",
                        MenuUse = true,
                        HotKey = "DoubleCLick",
                        AccessLevel = Common.Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var i = new ProductionSchemeForm();
                            i.Values = ProductionSchemeGrid.SelectedItem;
                            i.Idtls = ProductionSchemeGrid.SelectedItem.CheckGet("ID_TLS").ToInt();
                            i.Idtscheme = ProductionSchemeGrid.SelectedItem.CheckGet("IDTSCHEME").ToInt();
                            i.IsCreated = false;
                            i.Show();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var k = ProductionSchemeGrid.GetPrimaryKey();
                            var row = ProductionSchemeGrid.SelectedItem;
                            var is_del = ProductionSchemeGrid.SelectedItem.CheckGet("MARKED_TO_DELETE").ToInt();
                            if (row.CheckGet(k).ToInt() != 0 && is_del!=1)
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
                        Description = "Удалить схему производства",
                        ButtonUse = true,
                        ButtonName = "DeleteProductionSchemeButton",
                        MenuUse = true,
                        AccessLevel = Common.Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            DisableControls();
                            ProductionSchemeDelete();
                            EnableControls();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var is_del = ProductionSchemeGrid.SelectedItem.CheckGet("MARKED_TO_DELETE").ToInt();
                            var row = ProductionSchemeGrid.SelectedItem.Count;
                            if (row != 0 
                                && is_del != 1
                                && ProductionSchemeGrid.SelectedItem.CheckGet("DEFS").ToInt() != 1)
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "defs",
                        Group = "crud",
                        Enabled = true,
                        Title = "Установить признак \"Главн.схема\"",
                        Description = "Установить признак \"Главн.схема\"",
                        MenuUse = true,
                        AccessLevel = Common.Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            DisableControls();
                            ProductionSchemeSetDefs();
                            EnableControls();
                        },
                        CheckEnabled = () =>
                        {
                            return ProductionSchemeGrid.SelectedItem.CheckGet("DEFS").ToInt() == 0;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "secondary",
                        Group = "crud",
                        Enabled = true,
                        Title = "Убрать признак \"Главн.схема\"",
                        Description = "Убрать признак \"Главн.схема\"",
                        MenuUse = true,
                        AccessLevel = Common.Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            DisableControls();
                            ProductionSchemeResetDefs();
                            EnableControls();
                        },
                        CheckEnabled = () =>
                        {
                            return ProductionSchemeGrid.SelectedItem.CheckGet("DEFS").ToInt() != 0;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "create_alt",
                        Group = "crud",
                        Enabled = true,
                        Title = "Создать альт. схему для добивочной заготовки",
                        Description = "Создать альтернативную схему для добивочной заготовки",
                        ButtonUse = true,
                        ButtonName = "CreateAlternativeProductionSchemeButton",
                        MenuUse = true,
                        AccessLevel = Common.Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            ProductionSchemeCreateAlt();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var defs = ProductionSchemeGrid.SelectedItem.CheckGet("DEFS").ToInt();
                            var is_del = ProductionSchemeGrid.SelectedItem.CheckGet("MARKED_TO_DELETE").ToInt();
                            var idtscheme = ProductionSchemeGrid.SelectedItem.CheckGet("IDTSCHEME").ToInt();
                            var row = ProductionSchemeGrid.SelectedItem;
                            if (row != null && defs==0 && idtscheme == 42 && is_del != 1)
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "Refresh",
                        Group = "refresh",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить таблицу схем производства",
                        MenuUse = true,
                        Action = () =>
                        {
                            GoodsGridLoadItems();
                        }
                    });
                }

            }

            Commander.Init(this);
        }
        public FormHelper Form { get; set; }

        private void FormInit()
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
        private void GoodsGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="ИД изделия",
                    Path="ID2",
                    Width2=10,
                    ColumnType=ColumnTypeRef.Integer,
                    
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
                    Width2=18,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="NAME",
                    Doc="Наименование товара",
                    ColumnType=ColumnTypeRef.String,
                    Width2=48,
                },
            };
            GoodsGrid.SetColumns(columns);
            GoodsGrid.SetPrimaryKey("ID2");
            GoodsGrid.SetSorting("NAME", ListSortDirection.Ascending);
            GoodsGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

            GoodsGrid.Toolbar = GoodsGridToolbar;
            GoodsGrid.ItemsAutoUpdate = false;
            GoodsGrid.Commands = Commander;

            
            GoodsGrid.OnSelectItem = (row) =>
            {
                ProductionSchemeGridLoadItems();
            };
            GoodsGrid.Init();
        }
        private void ProductionSchemeGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="IDTSTREE",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                    Visible=false
                },
                //new DataGridHelperColumn
                //{
                //    Header="defs",
                //    Path="DEFS",
                //    ColumnType=ColumnTypeRef.Integer,
                //    Width2=8,
                //    Visible=false
                //},
                new DataGridHelperColumn
                {
                    Header="del",
                    Path="MARKED_TO_DELETE",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                    Visible=false
                },
                //new DataGridHelperColumn
                //{
                //    Header="second",
                //    Path="SECONDARY_FLAG",
                //    ColumnType=ColumnTypeRef.Integer,
                //    Width2=8,
                //    Visible=false
                //},
                new DataGridHelperColumn
                {
                    Header="Схема производства",
                    Path="NAME_SCHEMA",
                    ColumnType=ColumnTypeRef.String,
                    Width2=28,
                    Visible=true,
                    DxEnableColumnSorting=false,
                },
                new DataGridHelperColumn
                {
                    Header="Станок",
                    Path="ST_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                    Visible=true,
                    DxEnableColumnSorting=false,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="ARTIKUL",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                    Visible=true,
                    DxEnableColumnSorting=false,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=36,
                    Visible=true,
                    DxEnableColumnSorting=false,
                },
                new DataGridHelperColumn
                {
                    Header="Кол-во изд-й из заг-ки",
                    Path="QTY",
                    ColumnType=ColumnTypeRef.Double,
                    Width2=17,
                    Visible=true,
                    DxEnableColumnSorting=false,
                },
                new DataGridHelperColumn
                {
                    Header="Кол-во после передела",
                    Path="QTY_NORM",
                    ColumnType=ColumnTypeRef.Double,
                    Width2=18,
                    Visible=true,
                    DxEnableColumnSorting=false,
                },
                new DataGridHelperColumn
                {
                    Header="Человек",
                    Path="PEOPLE",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                    Visible=true,
                    DxEnableColumnSorting=false,
                },
                new DataGridHelperColumn
                {
                    Header="Скорость",
                    Path="SPEED",
                    ColumnType=ColumnTypeRef.Double,
                    Width2=8,
                    Visible=true,
                    DxEnableColumnSorting=false,
                },
                new DataGridHelperColumn
                {
                    Header="Статистическая скорость",
                    Path="STATISTICAL_SPEED_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=19,
                    Visible=true,
                    DxEnableColumnSorting=false,
                },
                new DataGridHelperColumn
                {
                    Header="Главн. схема",
                    Path="DEFS",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=11,
                    Visible=true,
                    DxEnableColumnSorting=false,
                },
                new DataGridHelperColumn
                {
                    Header="Второстеп. схема",
                    Path="SECONDARY_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=13,
                    Visible=true,
                    DxEnableColumnSorting=false,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=36,
                    Visible=true,
                    DxEnableColumnSorting=false,
                },
                new DataGridHelperColumn
                {
                    Header="Пользователь",
                    Path="USERNAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=28,
                    Visible=true,
                    DxEnableColumnSorting=false,
                },
                new DataGridHelperColumn
                {
                    Header="id_tls",
                    Path="ID_TLS",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="idtscheme",
                    Path="IDTSCHEME",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="idtstree_p",
                    Path="idtstree_p",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="key_c",
                    Path="KEY_C",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="key_p",
                    Path="KEY_PARENT",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="Есть незавершенные ПЗ",
                    Path="NR_NOT_EXIST",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                    Visible=false
                },
            };
            ProductionSchemeGrid.SetColumns(columns);
            ProductionSchemeGrid.SetPrimaryKey("KEY_C");
            ProductionSchemeGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ProductionSchemeGrid.Toolbar = ProductionSchemeToolbar;
            ProductionSchemeGrid.ItemsAutoUpdate = false;
            ProductionSchemeGrid.Commands = Commander;
            ProductionSchemeGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        var defs = row.CheckGet("DEFS").ToInt();
                        var del = row.CheckGet("MARKED_TO_DELETE").ToInt();
                        var second = row.CheckGet("SECONDARY_FLAG").ToInt();
                        if (defs == 1)
                        {
                            color = HColor.Green;
                        }
                        if (del == 1)
                        {
                            color = HColor.Red;
                        }
                        if (second == 1)
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
            };
            ProductionSchemeGrid.Init();
        }

        private async void ProductionSchemeGridLoadItems()
        {
            bool resume = true;

            if (resume)
            {
                int goodsId2 = GoodsGrid.SelectedItem.CheckGet("ID2").ToInt();
                int goodsIdk1 = GoodsGrid.SelectedItem.CheckGet("IDK1").ToInt();
                if (goodsId2 > 0)
                {
                    var p = new Dictionary<string, string>();
                    {
                        p.CheckAdd("ID2", goodsId2.ToString());
                        p.CheckAdd("IDK1", goodsIdk1.ToString());
                    }
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sources/ProductionScheme");
                    q.Request.SetParam("Object", "ProductionScheme");
                    q.Request.SetParam("Action", "ListTree");
                    q.Request.SetParams(p);

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    q.DoQuery();
                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            {
                                var ds = ListDataSet.Create(result, "ITEMS");
                                if (ds.Items.Count > 0)
                                {
                                    string last = ds.Items[0].CheckGet("KEY_C");
                                    var cnt = 1;
                                    var space = "|    ";
                                    foreach (var item in ds.Items)
                                    {
                                        item["NAME_SCHEMA"] = "|-⯈" + item["NAME_SCHEMA"];

                                        if (item["KEY_PARENT"] == last)
                                        {

                                            item["NAME_SCHEMA"] = string.Concat(Enumerable.Repeat(space, cnt)) + item["NAME_SCHEMA"];
                                            cnt++;
                                        }
                                        else
                                        {
                                            cnt = 1;
                                        }

                                        last = item["KEY_C"];

                                    }
                                }
                                ProductionSchemeGrid.UpdateItems(ds);

                            }
                        }
                    }
                }

            }
        }
        private async void GoodsGridLoadItems()
        {
            bool resume = true;

            string goodsSearch = "%" + GoodsGridSearch.Text + "%";
            if (goodsSearch.Length != null && goodsSearch.Length >= 5)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("TEXT", goodsSearch.ToString());
                }
                if (resume)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sources/ProductionScheme");
                    q.Request.SetParam("Object", "Goods");
                    q.Request.SetParam("Action", "ListForProductionScheme");
                    q.Request.SetParams(p);
                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            {
                                var ds = ListDataSet.Create(result, "ITEMS");

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

        private void ProductionSchemeDelete()
        {
            bool resume = false;
            int id_tls = ProductionSchemeGrid.SelectedItem.CheckGet("ID_TLS").ToInt();

            if (ProductionSchemeGrid.SelectedItem.CheckGet("NR_NOT_EXIST").ToBool())
            {
                DialogWindow.ShowDialog($"Схема используется в незавершенном ПЗ", "Невозможно удалить схему", "", DialogWindowButtons.OK);
                return;
            }

            var dw = new DialogWindow("Вы действительно хотите удалить схему производства? \r\nИД схемы: " + id_tls, "Удаление схемы производства", "", DialogWindowButtons.NoYes);
            if ((bool)dw.ShowDialog())
            {
                resume = dw.ResultButton == DialogResultButton.Yes;
            }

            if (resume)
            {
                var id_tk = ProductionSchemeGrid.SelectedItem.CheckGet("ID_TK").ToInt();
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("I_ID_TLS_OLD", id_tls.ToString());
                    p.CheckAdd("I_ID_TK", id_tk.ToString());
                    p.CheckAdd("I_ID_TLS_NEW", "");
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sources/ProductionScheme");
                q.Request.SetParam("Object", "ProductionScheme");
                q.Request.SetParam("Action", "Delete");
                q.Request.SetParams(p);
                Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    ProductionSchemeGrid.DeleteItemsByKey("ID_TLS", id_tls.ToString());


                }
                else if (q.Answer.Error.Code == 145)
                {
                    q.ProcessError();
                }
            }
        }
        private async void ProductionSchemeCreateAlt()
        {
            bool resume = false;
            string idScheme = ProductionSchemeGrid.SelectedItem.CheckGet("ID_TLS").ToString();

            var dw = new DialogWindow("Вы действительно хотите создать альтернативные схемы для добивочной заготовки? \r\nИД схемы: " + idScheme, "Создание схемы производства", "", DialogWindowButtons.NoYes);
            if ((bool)dw.ShowDialog())
            {
                resume = dw.ResultButton == DialogResultButton.Yes;
            }

            if (resume)
            {

                var id_tls = ProductionSchemeGrid.SelectedItem.CheckGet("ID_TLS").ToInt();
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("I_ID_TLS", id_tls.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sources/ProductionScheme");
                q.Request.SetParam("Object", "ProductionScheme");
                q.Request.SetParam("Action", "SaveAlternative");
                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    ProductionSchemeGridLoadItems();
                }
                else if (q.Answer.Error.Code == 145)
                {
                    q.ProcessError();
                }
            }
        }
        private void GoodsGridSearchButtonClick(object sender, RoutedEventArgs e)
        {
            GoodsGridLoadItems();
        }
        private void GoodsGridSearchTextChanged(object sender, TextChangedEventArgs e)
        {
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
        private void DisableControls()
        {
            ProductionSchemeToolbar.IsEnabled = false;
            ProductionSchemeGrid.ShowSplash();
            ProductionSchemeGrid.IsEnabled = false;
        }
        private void EnableControls()
        {
            ProductionSchemeToolbar.IsEnabled = true;
            ProductionSchemeGrid.HideSplash();
            ProductionSchemeGrid.IsEnabled = true;
        }
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp-new/products_materials/typeschema");
        }
        private void ProductionSchemeSetDefs()
        {
            if (ProductionSchemeGrid.SelectedItem == null
                || DialogWindow.ShowDialog($"Установить признак \"Главн.схема\" \"{ProductionSchemeGrid.SelectedItem.CheckGet("NAME_SCHEMA")}\"?", "Схема", "", DialogWindowButtons.NoYes) != true) return;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sources/ProductionScheme");
            q.Request.SetParam("Object", "ProductionScheme");
            q.Request.SetParam("Action", "SetDefs");
            q.Request.SetParam("ID2", GoodsGrid.SelectedItem.CheckGet("ID2"));
            q.Request.SetParam("IDK1", GoodsGrid.SelectedItem.CheckGet("IDK1"));
            q.Request.SetParam("ID_TLS", ProductionSchemeGrid.SelectedItem.CheckGet("ID_TLS"));
            q.DoQuery();
            if (q.Answer.Status == 0)
            {
                ProductionSchemeGridLoadItems();
            }
            else
            {
                q.ProcessError();
            }
        }
        private void ProductionSchemeResetDefs()
        {
            if (ProductionSchemeGrid.SelectedItem == null
                || DialogWindow.ShowDialog($"Убрать признак \"Главн.схема\" \"{ProductionSchemeGrid.SelectedItem.CheckGet("NAME_SCHEMA")}\"?", "Схема", "", DialogWindowButtons.NoYes) != true) return;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sources/ProductionScheme");
            q.Request.SetParam("Object", "ProductionScheme");
            q.Request.SetParam("Action", "ResetDefs");
            q.Request.SetParam("ID_TLS", ProductionSchemeGrid.SelectedItem.CheckGet("ID_TLS"));
            q.DoQuery();
            if (q.Answer.Status == 0)
            {
                ProductionSchemeGridLoadItems();
            }
            else
            {
                q.ProcessError();
            }
        }
    }
}
