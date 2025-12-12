using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using DevExpress.DirectX.StandardInterop.Direct2D;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using DevExpress.Xpf.Core.Internal;
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
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Sources
{
    /// <summary>
    /// Форма создания и редактирования комплектов
    /// Страница комплектов.
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class GoodsForm : ControlBase
    {
        public GoodsForm()
        {
            DocumentationUrl = "/doc/l-pack-erp-new/products_materials/set";

            InitializeComponent();

            formInit();
            goodsGridInit();

            FrameMode = 1;
            IsFirstLoad = true;
            OnGetFrameTitle = () =>
            {
                var result = "";

                var nscheme = Nscheme.ToInt();
                var ids = IdS.ToInt();
                var is_primary = IsPrimary.ToInt();
                if (is_primary == 1 && nscheme == 0)
                {
                    result = $"Добавление главного изделия";
                }
                else if (is_primary == 1 && nscheme != 0)
                {
                    result = $"Изменение главного изделия #{nscheme}";
                }
                else if (ids == 0 && is_primary == 0)
                {
                    result = $"Добавление комплектующего";
                }
                else if (ids != 0 && is_primary == 0)
                {
                    result = $"Изменение комплектующего #{ids}";
                }

                PrimaryKeyValue = nscheme.ToString();
                return result;
            };
            FrameName = "GoodsForm";
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
                    Title = "Сохранить",
                    Description = "Сохранить",
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
                        var k = GoodsGrid.GetPrimaryKey();
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
            

            Commander.Init(this);

            OnLoad = () =>
            {
                Form.SetDefaults();
                GoodsGridSearch.Focus();
            };

        }
        
        public int Nscheme { get; set; }
        public int IdS { get; set; }
        public int Id2 { get; set; }
        public int Count { get; set; }
        public int IsPrimary { get; set; }
        private bool Resume { get; set; }
        private string Error { get; set; }  
        public FormHelper Form { get; set; }
        private bool IsFirstLoad { get; set; }

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
                    Path="KOL",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ActualValue = "12",
                    Control=GoodsCountTextBox,
                    ControlType="TextBox",
                    Default="1",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 4 },
                    },
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
            };
            GoodsGrid.SetColumns(columns);
            GoodsGrid.SetPrimaryKey("ID2");
            GoodsGrid.SetSorting("NAME", ListSortDirection.Ascending);
            GoodsGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            GoodsGrid.Toolbar = GoodsGridToolbar;
            GoodsGrid.ItemsAutoUpdate = false;
            GoodsGrid.Commands = Commander;
            GoodsGrid.OnLoadItems = goodsGridLoadItems;
            GoodsGrid.Init();
        }

        private async void goodsGridLoadItems()
        {
            if (IsFirstLoad)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID2", Id2.ToString());
                }
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sources/Sets");
                q.Request.SetParam("Object", "Goods");
                q.Request.SetParam("Action", "GetById2");
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
                IsFirstLoad = false;
                GoodsCountTextBox.Text = Count.ToString();
            }
            else
            {
                string goods_search = "%" + GoodsGridSearch.Text + "%";
                if (goods_search.Length != null && goods_search.Length >= 5)
                {
                    var p = new Dictionary<string, string>();
                    {
                        p.CheckAdd("TEXT", goods_search.ToString());
                        p.CheckAdd("ID2", Id2.ToString());
                        p.CheckAdd("NSCHEME", Nscheme.ToString());
                    }
                    if (IsPrimary == 1)
                    {
                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Sources/Sets");
                        q.Request.SetParam("Object", "Goods");
                        q.Request.SetParam("Action", "ListMaster");
                        q.Request.SetParams(p);

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
                                    GoodsGrid.UpdateItems(ds);
                                    GoodsRefreshButton.Style = (Style)GoodsGridSearch.TryFindResource("Button");

                                }
                            }
                        }
                        else
                        {
                            q.ProcessError();
                        }
                    }
                    else
                    {
                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Sources/Sets");
                        q.Request.SetParam("Object", "Goods");
                        q.Request.SetParam("Action", "ListSlave");
                        q.Request.SetParams(p);

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
            
        }

        private void SchemeListCreate()
        {
            var p = new Dictionary<string, string>();
            var v = Form.GetValues();

            int kol = v.CheckGet("KOL").ToInt();
            int id2 = GoodsGrid.SelectedItem.CheckGet("ID2").ToInt();
            int idk1 = GoodsGrid.SelectedItem.CheckGet("IDK1").ToInt();
            p.CheckAdd("KOL", kol.ToString());
            p.CheckAdd("ID2", id2.ToString());
            p.CheckAdd("IDK1", idk1.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sources/Sets");
            q.Request.SetParam("Object", "Goods");
            q.Request.SetParam("Action", "SaveMaster");
            q.Request.SetParams(p);

            Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Id2 = id2;
            }
            else
            {
                Resume = false;
                var dw = new DialogWindow(q.Answer.Error.ToString(), "Ошибка сохранения");
                dw.ShowDialog();
                q.ProcessError();
            }

        }
        private void SchemeListUpdate()
        {
            var p = new Dictionary<string, string>();

            var v = Form.GetValues();

            int kol = v.CheckGet("KOL").ToInt();
            int id2 = GoodsGrid.SelectedItem.CheckGet("ID2").ToInt();
            int idk1 = GoodsGrid.SelectedItem.CheckGet("IDK1").ToInt();
            p.CheckAdd("NSCHEME", Nscheme.ToString());
            p.CheckAdd("KOL", kol.ToString());
            p.CheckAdd("ID2", id2.ToString());
            p.CheckAdd("IDK1", idk1.ToString());
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sources/Sets");
            q.Request.SetParam("Object", "Goods");
            q.Request.SetParam("Action", "UpdateMaster");
            q.Request.SetParams(p);

            Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Id2 = id2;
            }
            else
            {
                Resume = false;
                var dw = new DialogWindow("Не удалось обновить главную схему", "Ошибка обновления");
                dw.ShowDialog();
                q.ProcessError();
            }
        }
        private void SchemeCreate()
        {
            var p = new Dictionary<string, string>();
            var v = Form.GetValues();

            int kol = v.CheckGet("KOL").ToInt();
            int id2 = GoodsGrid.SelectedItem.CheckGet("ID2").ToInt();
            int idk1 = GoodsGrid.SelectedItem.CheckGet("IDK1").ToInt();
            p.CheckAdd("NSCHEME", Nscheme.ToString());
            p.CheckAdd("KOL", kol.ToString());
            p.CheckAdd("ID2", id2.ToString());
            p.CheckAdd("IDK1", idk1.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sources/Sets");
            q.Request.SetParam("Object", "Goods");
            q.Request.SetParam("Action", "SaveSlave");
            q.Request.SetParams(p);

            Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Id2 = id2;
            }
            else
            {
                Resume = false;
                var dw = new DialogWindow("Не удалось добавить комплектующее", "Ошибка сохранения");
                dw.ShowDialog();
                q.ProcessError();
            }
        }
        private void SchemeUpdate()
        {
            var p = new Dictionary<string, string>();

            var v = Form.GetValues();

            int kol = v.CheckGet("KOL").ToInt();
            int id2 = GoodsGrid.SelectedItem.CheckGet("ID2").ToInt();
            int idk1 = GoodsGrid.SelectedItem.CheckGet("IDK1").ToInt();
            p.CheckAdd("IDS", IdS.ToString());
            p.CheckAdd("KOL", kol.ToString());
            p.CheckAdd("ID2", id2.ToString());
            p.CheckAdd("IDK1", idk1.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sources/Sets");
            q.Request.SetParam("Object", "Goods");
            q.Request.SetParam("Action", "UpdateSlave");
            q.Request.SetParams(p);

            Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Id2 = id2; 
            }
            else
            {
                Resume = false;
                var dw = new DialogWindow("Не удалось обновить комплектующее", "Ошибка обновления");
                dw.ShowDialog();
                q.ProcessError();
            }
        }
        private bool SchemeIsExist()
        {
            bool exist = false;
            var p = new Dictionary<string, string>();

            int id2 = GoodsGrid.SelectedItem.CheckGet("ID2").ToInt();
            int idk1 = GoodsGrid.SelectedItem.CheckGet("IDK1").ToInt();
            p.CheckAdd("NSCHEME", Nscheme.ToString());
            p.CheckAdd("ID2", id2.ToString());
            p.CheckAdd("IDK1", idk1.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sources/Sets");
            q.Request.SetParam("Object", "ProductionScheme");
            q.Request.SetParam("Action", "IsExist");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null) {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    int cnt = ds.GetFirstItem().CheckGet("CNT").ToInt();
                    if (cnt != 0 )
                    {
                        exist = true;
                    }
                }
                
            }
            return exist;
        }

        public void Save()
        {
            if (Form.Validate())
            {
                var v = Form.GetValues();
                int kol = v.CheckGet("KOL").ToInt();

                bool resume = true;
                string errorMsg = "";
                if (kol == 0)
                {
                    resume = false;
                    errorMsg = "Количество не может быть нулевым";
                }

                if (resume)
                {
                    SaveData();
                }
                else
                {
                    Form.SetStatus(errorMsg, 1);
                }
            }
        }

        private async void SaveData()
        {
            Resume = true;
            //Создание главного
            if (Nscheme == 0)
            {
                SchemeListCreate();
            }
            else
            {
                //Редактирование комплектующего
                if (IdS != 0)
                {
                    if (!SchemeIsExist())
                    {
                        SchemeUpdate();
                    }
                    else
                    {
                        Resume = false;
                        var dw = new DialogWindow("Комплектующее уже существует. Обновите количество.", "Ошибка сохранения");
                        dw.ShowDialog();
                    }
                }
                else
                {
                    //Редактирование главного
                    if (IsPrimary == 1)
                    {
                        SchemeListUpdate();
                    }
                    //Создание комплектующего
                    else
                    {
                        if (!SchemeIsExist())
                        {
                            SchemeCreate();
                        }
                        else
                        {
                            Resume = false;
                            var dw = new DialogWindow("Комплектующее уже существует. Обновите количество.", "Ошибка сохранения");
                            dw.ShowDialog();
                        }
                        
                    }
                }
            }
            if (Resume)
            {
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverName = "SetsTab",
                    SenderName = ControlName,
                    Action = "ReturnNscheme",
                    Message = Id2.ToString(),
                });
                Close();
            }

        }
        
        private void goodsRefreshButtonClick(object sender, RoutedEventArgs e)
        {
            goodsGridLoadItems();
        }

        private void GoodsGridSearchTextChanged(object sender, RoutedEventArgs e)
        {
            if (GoodsGridSearch.Text.Length >= 3)
            {
                GoodsRefreshButton.Style = (Style)GoodsGridSearch.TryFindResource("FButtonPrimary");
            }
            else
            {
                GoodsRefreshButton.Style = (Style)GoodsGridSearch.TryFindResource("Button");

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
