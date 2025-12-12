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
using NPOI.POIFS.Crypt.Dsig;
using Org.BouncyCastle.Crypto;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;
using ControlBase = Client.Interfaces.Main.ControlBase;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Форма отображения комплектов техкарт
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class TechnologicalMapSetsList : ControlBase
    {
        public TechnologicalMapSetsList()
        {
            InitializeComponent();

            RoleName = "[erp]technological_map_sets";
            ControlTitle = "Комплекты техкарт";
            DocumentationUrl = "/doc/l-pack-erp-new/products_materials/set";


            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    switch (m.Action)
                    {
                        case "SetCreated":
                            TkSetsGridLoadItems();
                            var id_set = m.Message.ToInt();
                            TkSetsGrid.SelectRowByKey(id_set.ToString());
                            //ElementsGridLoadItems();
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
                LoadRef();
                TkSetsGridInit();
                ElementsGridInit();
            };

            OnUnload = () =>
            {
                TkSetsGrid.Destruct();
                ElementsGrid.Destruct();
            };

            Commander.SetCurrentGridName("TkSetsGrid");
            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "show",
                        Group = "main",
                        Title = "Показать",
                        Enabled = true,
                        Description = "Показать",
                        ButtonUse = true,
                        ButtonName = "ShowButton",
                        MenuUse = false,
                        Action = () =>
                        {
                            TkSetsGridLoadItems();
                            ShowButton.Style = (Style)ShowButton.TryFindResource("Button");
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "create",
                        Group = "main",
                        Title = "Создать",
                        Description = "Создать комплект",
                        Enabled = true,
                        MenuUse = true,
                        HotKey = "Insert",
                        ButtonUse = true,
                        ButtonName = "CreateButton",
                        Action = () =>
                        {
                            var i = new TechnologicalMapSet();
                            i.IsUpdateCount = false;
                            i.ReciverName = ControlName;
                            i.IsCreate = 1;
                            i.CustId = -1;
                            i.Show();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "edit",
                        Group = "main",
                        Title = "Изменить",
                        MenuUse = true,
                        Enabled = true,
                        HotKey = "Return|DoubleCLick",
                        Description = "Изменить комплект",
                        ButtonUse = true,
                        ButtonName = "EditButton",
                        Action = () =>
                        {
                            bool change_count_flag = false;
                            foreach (var item in ElementsGrid.Items)
                            {
                                if (item.CheckGet("ARTIKUL") != "")
                                {
                                    change_count_flag = true;
                                    break;
                                }
                            }

                            var id_set = TkSetsGrid.SelectedItem.CheckGet("ID_SET").ToInt();
                            var path = TkSetsGrid.SelectedItem.CheckGet("PATH_SET");
                            if (id_set > 0)
                            {
                                var i = new TechnologicalMapSet();
                                i.IsUpdateCount = change_count_flag;
                                i.ReciverName = ControlName;
                                i.IdSet = id_set;
                                i.PathSet = path;
                                i.IsCreate = 0;
                                i.Open(id_set);
                                i.Show();
                            }
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "delete",
                        Group = "main",
                        Enabled = true,
                        Title = "Удалить",
                        MenuUse = true,
                        Description = "Удалить комплект",
                        ButtonUse = true,
                        ButtonName = "DeleteButton",
                        Action = () =>
                        {
                            var id_set = TkSetsGrid.SelectedItem.CheckGet("ID_SET").ToInt();

                            if (id_set > 0)
                            {
                                var i = new TechnologicalMapSet();
                                i.ReciverName = ControlName;
                                i.IdSet = id_set;
                                i.PathSet = TkSetsGrid.SelectedItem.CheckGet("PATH_SET");
                                i.IsCreate = 0;
                                i.Open(id_set);
                                i.DeleteSet(id_set);
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = true;
                            foreach (var item in ElementsGrid.Items)
                            {
                                if (item.CheckGet("ARTIKUL") != "")
                                {
                                    result = false;
                                }
                            }
                            return result;
                        }
                    });
                }
                Commander.SetCurrentGroup("excel");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "show_file",
                        Group = "excel",
                        Enabled = true,
                        Title = "Показать ТК",
                        Description = "Показать файл техкарты",
                        ButtonUse = true,
                        MenuUse = true,
                        ButtonName = "ShowFileTkButton",
                        Action = () =>
                        {
                            OpenExcelFile();
                        },
                        CheckEnabled = () =>
                        {
                            var name = TkSetsGrid.SelectedItem.CheckGet("PATH_SET");
                            var pathNew = Path.Combine(TkSetsGrid.SelectedItem.CheckGet("PATH_NEW"), name);
                            var pathWork = Path.Combine(TkSetsGrid.SelectedItem.CheckGet("PATH_WORK"), name);
                            var pathArchive = Path.Combine(TkSetsGrid.SelectedItem.CheckGet("PATH_ARCHIVE"), name);
                            if (System.IO.File.Exists(pathNew) || System.IO.File.Exists(pathWork) || System.IO.File.Exists(pathArchive))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        },
                    });
                }

                Commander.SetCurrentGroup("move");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "move_to_work",
                        Group = "move",
                        Enabled = true,
                        Title = "В рабочую",
                        Description = "Переместить в рабочую",
                        ButtonUse = true,
                        MenuUse = true,
                        ButtonName = "MoveToWork",
                        Action = () =>
                        {
                            MoveToWorkAction();
                        },
                        CheckEnabled = () =>
                        {
                            var name = TkSetsGrid.SelectedItem.CheckGet("PATH_SET");
                            var pathNew = Path.Combine(TkSetsGrid.SelectedItem.CheckGet("PATH_NEW"), name);
                            var pathArchive = Path.Combine(TkSetsGrid.SelectedItem.CheckGet("PATH_ARCHIVE"), name);
                            if (System.IO.File.Exists(pathNew) || System.IO.File.Exists(pathArchive))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "move_to_archive",
                        Group = "move",
                        Enabled = true,
                        Title = "В архив",
                        Description = "Переместить в архив",
                        ButtonUse = true,
                        ButtonName = "MoveToArchive",
                        MenuUse = true,
                        Action = () =>
                        {
                            MoveToArchiveAction();
                        },
                        CheckEnabled = () =>
                        {
                            var name = TkSetsGrid.SelectedItem.CheckGet("PATH_SET");
                            var pathWork = Path.Combine(TkSetsGrid.SelectedItem.CheckGet("PATH_WORK"), name);
                            if (System.IO.File.Exists(pathWork))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        },
                    });
                }
            }

            Commander.Init(this);
        }

        #region "Переменные"
        /// <summary>
        /// Форма
        /// </summary>
        public FormHelper Form { get; set; }


        /// <summary>
        /// ИД  комплекта
        /// </summary>
        public int IdSet { get; set; }

        /// <summary>
        /// ИД покупателя
        /// </summary>
        public int CustId { get; set; }
        #endregion

        private void FormInit()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="SEARCH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=GridSearch,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
            };
            Form.SetFields(fields);
        }
        private void TkSetsGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="ИД комплекта",
                    Path="ID_SET",
                    Width2=12,
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
                    Header="Размер",
                    Path="SIZE_TK",
                    Width2=12,
                    ColumnType=ColumnTypeRef.String,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="NAME",
                    Doc="Наименование комплекта",
                    ColumnType=ColumnTypeRef.String,
                    Width2=60,
                },
                new DataGridHelperColumn
                {
                    Header="Покупатель",
                    Path="BUYER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=25,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Дата создания",
                    Path="CREATED_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=16,
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
                    Header="Номер",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="Файл",
                    Path="PATH_SET",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="Рабочая папка",
                    Path="PATH_WORK",
                    ColumnType=ColumnTypeRef.String,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="Папка для новых файлов",
                    Path="PATH_NEW",
                    ColumnType=ColumnTypeRef.String,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="Папка для архивных файлов",
                    Path="PATH_ARCHIVE",
                    ColumnType=ColumnTypeRef.String,
                    Visible=false
                },
            };
            TkSetsGrid.SetColumns(columns);
            TkSetsGrid.SetPrimaryKey("_ROWNUMBER");
            TkSetsGrid.SetSorting("SIZE", ListSortDirection.Ascending);
            TkSetsGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

            TkSetsGrid.SearchText = GridSearch;
            TkSetsGrid.Toolbar = TkSetsGridToolbar;
            TkSetsGrid.ItemsAutoUpdate = false;
            TkSetsGrid.Commands = Commander;

            TkSetsGrid.OnSelectItem = (row) =>
            {
                IdSet = row.CheckGet("ID_SET").ToInt();
                ElementsGridLoadItems();
            };
            TkSetsGrid.Init();
        }
        private void ElementsGridInit()
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
                    Header="ИД комплекта",
                    Path="ID_SET",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=14,
                },
                new DataGridHelperColumn
                {
                    Header="ИД ТК",
                    Path="ID_TK",
                    ColumnType=ColumnTypeRef.String,
                    Width2=13,
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
                    Header="Размер",
                    Path="SIZE_TK",
                    Width2=12,
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false,
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
                    Header="Покупатель",
                    Path="BUYER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=25,
                },
                new DataGridHelperColumn
                {
                    Header="Кол-во",
                    Path="CNT",
                    ColumnType=ColumnTypeRef.Double,
                    Format = "N0",
                    Width2=11,
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
                    Header="PARENT",
                    Path="PARENT",
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false
                },
            };
            ElementsGrid.SetColumns(columns);
            ElementsGrid.SetPrimaryKey("_ROWNUMBER");
            ElementsGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ElementsGrid.ItemsAutoUpdate = false;
            ElementsGrid.EnableSortingGrid = false;
            ElementsGrid.Commands = Commander;
            ElementsGrid.Init();
        }

        /// <summary>
        /// Загрузка справочника покупателей
        /// </summary>
        public async void LoadRef()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "TechnologicalMapSets");
            q.Request.SetParam("Action", "ListBuyers");
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
                    var ds = ListDataSet.Create(result, "CUSTOMER");
                    var buyerList = new Dictionary<string, string>()
                    {
                        { "-1", "Все" },
                    };
                    foreach (var item in ds.Items)
                    {
                        buyerList.CheckAdd(item["ID"].ToInt().ToString(), item.CheckGet("NAME"));
                    }
                    BuyerName.Items = buyerList;
                    BuyerName.SetSelectedItemByKey("-1");
                    TkSetsGridLoadItems();
                }
            }
        }
        private async void TkSetsGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("CUST_ID", BuyerName.SelectedItem.Key.ToString());
            }
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "TechnologicalMapSets");
            q.Request.SetParam("Action", "ListSets");
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
                    if (result.ContainsKey("ITEMS"))
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        TkSetsGrid.UpdateItems(ds);

                    }
                }
            }
        }
        private async void ElementsGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_SET", IdSet.ToString());
            }
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "TechnologicalMapSets");
            q.Request.SetParam("Action", "ListElements");
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

                        foreach (var item in ds.Items)
                        {
                            if (item["PARENT"].ToInt() == 1)
                            {
                                item["ID_TK"] = "|-⯈" + item["ID_TK"].ToInt().ToString();

                            }
                            else
                            {
                                item["ID_TK"] = "|    |-⯈" + item["ID_TK"].ToInt().ToString();
                            }
                        }
                        ElementsGrid.UpdateItems(ds);

                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }


        private void OpenExcelFile()
        {
            var file_name = TkSetsGrid.SelectedItem.CheckGet("PATH_SET");
            var path_work = TkSetsGrid.SelectedItem.CheckGet("PATH_WORK");
            var path_new = TkSetsGrid.SelectedItem.CheckGet("PATH_NEW");
            var path_archive = TkSetsGrid.SelectedItem.CheckGet("PATH_ARCHIVE");

            var path = Path.Combine(path_work, file_name);
            if (System.IO.File.Exists(path))
            {
                Central.OpenFile(path);
                return;
            }
            path = Path.Combine(path_new, file_name);
            if (System.IO.File.Exists(path))
            {
                Central.OpenFile(path);
                return;
            }
            path = Path.Combine(path_archive, file_name);
            if (System.IO.File.Exists(path))
            {
                Central.OpenFile(path);
                return;
            }
            else
            {
                var msg = "Excel файл тех карты не найден";
                var d = new DialogWindow($"{msg}", "ТК", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }
        private void MoveToWorkAction()
        {
            var name = TkSetsGrid.SelectedItem.CheckGet("PATH_SET");
            var pathNew = Path.Combine(TkSetsGrid.SelectedItem.CheckGet("PATH_NEW"), name);
            var pathWork = Path.Combine(TkSetsGrid.SelectedItem.CheckGet("PATH_WORK"), name);
            var pathArchive = Path.Combine(TkSetsGrid.SelectedItem.CheckGet("PATH_ARCHIVE"), name);

            if (File.Exists(pathNew))
            {
                if (File.Exists(pathWork))
                {
                    File.Delete(pathWork);
                }

                File.Move(pathNew, pathWork);
            }
            else if (File.Exists(pathArchive))
            {
                if (File.Exists(pathWork))
                {
                    File.Delete(pathWork);
                }

                File.Move(pathArchive, pathWork);
                ChangeArchiveFlag(0);
            }
            else
            {
                var msg = "Исходный файл не найден!";
                var d = new DialogWindow($"{msg}", "ТК", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private void MoveToArchiveAction()
        {
            var name = TkSetsGrid.SelectedItem.CheckGet("PATH_SET");
            var pathNew = Path.Combine(TkSetsGrid.SelectedItem.CheckGet("PATH_NEW"), name);
            var pathWork = Path.Combine(TkSetsGrid.SelectedItem.CheckGet("PATH_WORK"), name);
            var pathArchive = Path.Combine(TkSetsGrid.SelectedItem.CheckGet("PATH_ARCHIVE"), name);

            if (File.Exists(pathWork))
            {
                if (File.Exists(pathArchive))
                {
                    File.Delete(pathArchive);
                }

                File.Move(pathWork, pathArchive);
                ChangeArchiveFlag(1);
            }
            else
            {
                var msg = "Исходный файл не найден!";
                var d = new DialogWindow($"{msg}", "ТК", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private async void ChangeArchiveFlag (int flag)
        {
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_SET", TkSetsGrid.SelectedItem.CheckGet("ID_SET"));
                p.CheckAdd("FLAG", flag.ToString());
            }
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "TechnologicalMapSets");
            q.Request.SetParam("Action", "ChangeArchiveFlag");
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
                }
            }
        }


        private void UpdateGridItems(System.Windows.DependencyObject d, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");

        }
        private void HelpButtonClick(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp-new/products_materials/set");
        }


        //private async void SetDelete(int nscheme)
        //{
        //    // Подтверждение на удаление
        //    bool resume = false;
        //    string name = SetsGrid.SelectedItem.CheckGet("NAME").ToString();
        //    string artikul = SetsGrid.SelectedItem.CheckGet("ARTIKUL").ToString();

        //    var dw = new DialogWindow("Вы действительно хотите удалить комплект? \r\nАртикул: " + artikul + "\r\nНаиименование: " + name, "Удаление комплекта", "", DialogWindowButtons.NoYes);
        //    if ((bool)dw.ShowDialog())
        //    {
        //        resume = dw.ResultButton == DialogResultButton.Yes;
        //    }
        //    if (resume)
        //    {
        //        var q = new LPackClientQuery();
        //        q.Request.SetParam("Module", "Sources/Sets");
        //        q.Request.SetParam("Object", "ProductionScheme");
        //        q.Request.SetParam("Action", "DeleteMaster");
        //        q.Request.SetParam("NSHEMA", nscheme.ToString());

        //        await Task.Run(() =>
        //        {
        //            q.DoQuery();
        //        });

        //        if (q.Answer.Status == 0)
        //        {
        //            goodsGridLoadItems();
        //            setsGridLoadItems();
        //        }
        //        else if (q.Answer.Error.Code == 145)
        //        {
        //            q.ProcessError();
        //        }
        //    }
        //}
        //private async void SchemeDelete(int ids, int nscheme)
        //{
        //    // Подтверждение на удаление
        //    bool resume = false;
        //    string name = SetsGrid.SelectedItem.CheckGet("NAME").ToString();
        //    string artikul = SetsGrid.SelectedItem.CheckGet("ARTIKUL").ToString();

        //    var dw = new DialogWindow("Вы действительно хотите удалить элемент схемы? \r\nАртикул: " + artikul + "\r\nНаименование: " + name, "Удаление элемеента схемы", "", DialogWindowButtons.NoYes);
        //    if ((bool)dw.ShowDialog())
        //    {
        //        resume = dw.ResultButton == DialogResultButton.Yes;
        //    }
        //    var p = new Dictionary<string, string>();
        //    {
        //        p.CheckAdd("IDS", ids.ToString());
        //        p.CheckAdd("NSCHEME", nscheme.ToString());
        //    }
        //    if (resume)
        //    {
        //        var q = new LPackClientQuery();
        //        q.Request.SetParam("Module", "Sources/Sets");
        //        q.Request.SetParam("Object", "ProductionScheme");
        //        q.Request.SetParam("Action", "DeleteSlave");
        //        q.Request.SetParams(p);

        //        await Task.Run(() =>
        //        {
        //            q.DoQuery();
        //        });

        //        if (q.Answer.Status == 0)
        //        {
        //            goodsGridLoadItems();
        //            setsGridLoadItems();
        //        }
        //        else if (q.Answer.Error.Code == 145)
        //        {
        //            q.ProcessError();
        //        }
        //    }
        //}

        //private void goodsGridSearchButtonClick(object sender, RoutedEventArgs e)
        //{
        //    goodsGridLoadItems();
        //}
        //private void showArchiveClick(object sender, RoutedEventArgs e)
        //{
        //    GoodsGrid.UpdateItems();
        //}
        //private void GoodsGridSearchTextChanged(object sender, RoutedEventArgs e)
        //{
        //    AddedId = "";
        //    if (GoodsGridSearch.Text.Length >= 3)
        //    {
        //        GoodsGridSearchButton.Style = (Style)GoodsGridSearch.TryFindResource("FButtonPrimary");
        //    }
        //    else
        //    {
        //        GoodsGridSearchButton.Style = (Style)GoodsGridSearch.TryFindResource("Button");

        //    }
        //}

    }
}
