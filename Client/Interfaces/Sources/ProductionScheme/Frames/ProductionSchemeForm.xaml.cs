using Client.Assets.HighLighters;
using Client.Common;
using Client.Common.Extensions;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using DevExpress.DirectX.StandardInterop.Direct2D;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using DevExpress.Xpf.Core.Internal;
using DevExpress.Xpo.DB;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Crypto;
using SharpVectors.Dom;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Sources
{
    /// <summary>
    /// Форма создания и редактирования схемы производства
    /// Страница схемы производства.
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class ProductionSchemeForm : ControlBase
    {
        public ProductionSchemeForm()
        {
            DocumentationUrl = "/doc/l-pack-erp-new/products_materials/typeschema";

            InitializeComponent();
            FormInit();
            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    switch (m.Action)
                    {
                        case "ReturnGoods":
                            var n = (Dictionary<string, string>)m.ContextObject;
                            Form.SetValues(n);
                            Task.Delay(100);
                            ProductionSchemeUpdateGrid();
                            break;
                        default:
                            break;
                    }

                    Commander.ProcessCommand(m.Action, m);
                }
            };
            FrameMode = 1;
            OnGetFrameTitle = () =>
            {
                var result = "";

                if (Idtls == 0)
                {
                    result = $"Добавление схемы производства";
                }
                else   
                {
                    result = $"Изменение схемы производства #{Idtls}";
                }
                return result;
            };
            Commander.SetCurrentGridName("ProductionSchemeGrid");
            Commander.SetCurrentGroup("item");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "save",
                    Group = "main_form",
                    Enabled = true,
                    Title = "Сохранить",
                    Description = "Сохранить",
                    ButtonUse = true,
                    ButtonName = "SaveButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Save();

                    },
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
                Commander.Add(new CommandItem()
                {
                    Name = "update",
                    Group = "crud",
                    Title = "Изменить",
                    Enabled = true,
                    Description = "Добавить товар",
                    ButtonUse = true,
                    ButtonName = "GoodsSelectButton",
                    MenuUse = true,
                    HotKey = "DoubleCLick",
                    Action = () =>
                    {
                        var i = new GoodsForProductionSchemeForm();
                        i.Topnode = ProductionSchemeGrid.SelectedItem.CheckGet("TOPNODE").ToInt();
                        i.Show();
                        i.Focus();
                    },
                    CheckEnabled = () =>
                    { 
                        var result = false;
                        var item = ProductionSchemeGrid.SelectedItem.CheckGet("IDTSTREE");
                        if (DS != null && DS.Items != null)
                        {

                            var count = DS.Items.Count;
                            if (item == DS.Items[0].CheckGet("IDTSTREE") || item == DS.Items[count - 1].CheckGet("IDTSTREE"))
                            {
                                result = true;
                            }
                        }
                        return result;

                    }
                });
            }
            Commander.Init(this);
            OnLoad = () =>
            {
                GetDataTypeScheme();
                Form.SetDefaults();
                if (IsCreated)
                {
                    TypeScheme.SelectedItem = TypeScheme.Items.First();
                }
                else
                {
                    TypeScheme.SelectedItem = TypeScheme.Items.FirstOrDefault((x) => x.Key == Idtscheme.ToString());

                    TypeScheme.IsReadOnly = true;

                }
                ProductionSchemeGridInit();


            };

        }

        public Dictionary<string, string> Values { get; set; }
        public FormHelper Form { get; set; }
        public int Idtls { get; set; }
        public int Idtscheme { get; set; }
        public bool IsCreated { get; set; }
        public DispatcherTimer TemplateTimeoutTimer;
        private ListDataSet DS { get; set; }

        public void FormInit()
        {
            Form = new FormHelper();

            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="ID_TLS",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="IDTSCHEME",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TYPESCHEME",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TypeScheme,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QTY_NORM",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=QtyNorm,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PEOPLE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=People,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SPEED",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=Speed,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DEFS",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=PrimaryScheme,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SECONDARY_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=SecondaryScheme,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=SchemeNoteTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="ID2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="IDK1",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=GoodsName,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ARTIKUL",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ID_TK",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;

            Form.AfterSet = (Dictionary<string, string> v) =>
            {
                QtyNorm.Focus();
            };
        }
        private void ProductionSchemeGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Станок",
                    Path="ST_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=14,
                    Visible=true,
                    DxEnableColumnSorting=false,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="ARTIKUL",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                    Visible=true
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
                    Width2=30,
                    Visible=true,
                    DxEnableColumnSorting=false,
                },
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="IDTSTREE",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="parent",
                    Path="IDTSTREE_P",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="del",
                    Path="MARKED_TO_DELETE",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="Схема производства",
                    Path="NAME_SCHEMA",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                    Visible=false
                },
                
                new DataGridHelperColumn
                {
                    Header="Статистическая скорость",
                    Path="STATISTICAL_SPEED_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=8,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="Пользователь",
                    Path="USERNAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="Логин",
                    Path="USERLOGIN",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                    Visible=false
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
                    Header="id2",
                    Path="ID2",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="idk1",
                    Path="IDK1",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="stanok",
                    Path="ID_ST",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="id_tk",
                    Path="ID_TK",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="topnode",
                    Path="TOPNODE",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="qty_st",
                    Path="QTY_ST",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                    Visible=false
                },
            };
            
            ProductionSchemeGrid.SetColumns(columns);
            ProductionSchemeGrid.SetPrimaryKey("IDTSTREE");
            ProductionSchemeGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ProductionSchemeGrid.Toolbar = ProductionSchemeGridToolbar;
            ProductionSchemeGrid.ItemsAutoUpdate = false;
            ProductionSchemeGrid.Commands = Commander;
            ProductionSchemeGrid.OnSelectItem = (selectItem) =>
            {
                if (selectItem.CheckGet("TOPNODE") == "1")
                {
                    QtyNorm.Text="1";
                    QtyNorm.IsEnabled = false;
                }
                else
                {
                    QtyNorm.IsEnabled = true;
                }
                UpdateProductionSchemePanel(selectItem);
            };
            ProductionSchemeGrid.OnLoadItems = ProductionSchemeGridLoadItems;
            ProductionSchemeGrid.Init();
        }
        private async void ProductionSchemeGridLoadItems()
        {
            GetData();
        }
        private async void GetData()
        {
            DisableControls();
            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_TLS", Idtls.ToString());
                    p.CheckAdd("IDTSCHEME", Idtscheme.ToString());
                }
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sources/ProductionScheme");
                q.Request.SetParam("Object", "ProductionScheme");
                q.Request.SetParam("Action", "GetTreeById");
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var username = Central.User.Name;
                    var userlogin = Central.User.Login;
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        DS = ListDataSet.Create(result, "ITEMS");
                        string str = "|-⯈";
                        var nameTypeScheme = TypeScheme.SelectedItem.Value;
                        foreach (var item in DS.Items)
                        {
                            item["ST_NAME"] = str + item["ST_NAME"];
                            str = "|   " + str;
                            item.CheckAdd("NAME_SCHEMA", nameTypeScheme);
                            if (IsCreated)
                            {
                                item.CheckAdd("USERNAME", username);
                                item.CheckAdd("USERLOGIN", userlogin);
                            }
                        }
                        if (IsCreated)
                        {
                            PrimaryScheme.IsEnabled = false;
                            SecondaryScheme.IsEnabled = false;
                            DS.Items[0].CheckAdd("TOPNODE", "1");
                            DS.Items[0].CheckAdd("QTY", "1");
                            DS.Items[0].CheckAdd("QTY_NORM", "1");
                        }
                        ProductionSchemeGrid.UpdateItems(DS);
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            EnableControls();
        }
        private void GetDataTypeScheme()
        {
            DisableControls();
            bool resume = true;

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sources/ProductionScheme");
                q.Request.SetParam("Object", "ProductionScheme");
                q.Request.SetParam("Action", "ListType");

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        {
                            var ds = ListDataSet.Create(result, "TYPESCHEME");
                            TypeScheme.SetItems(ds, "ID", "NAME");
                        }
                        Show();
                    }
                }
                else
                {
                    q.ProcessError();
                }

            }
            EnableControls();
        }
        private async void ProductionSchemePanelSetItems()
        {
            Values = ProductionSchemeGrid.SelectedItem;
            Form.SetValues(Values);
        }
        private void UpdateProductionSchemePanel(Dictionary<string, string> item)
        {
            Values = item;
            Form.SetDefaults();
            Form.SetValues(item);
        }
        private void TypeScheme_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (IsCreated)
            {
                Idtscheme = TypeScheme.SelectedItem.Key.ToInt();
                GetData();
            }
        }
        private void ProductionSchemeUpdateGrid()
        {
            var v = Form.GetValues();
            int id = ProductionSchemeGrid.SelectedItem.CheckGet("IDTSTREE").ToInt();
            if (id == ProductionSchemeGrid.Items[ProductionSchemeGrid.Items.Count - 1].CheckGet("IDTSTREE").ToInt())
            {

                int id_tk = v.CheckGet("ID_TK").ToInt();
                foreach (var item in DS.Items)
                {
                    item.CheckAdd("ID_TK", id_tk.ToString());
                }
            }
            var qty = 1.0;
            foreach (var item in DS.Items)
            {
                item.CheckAdd("DEFS", PrimaryScheme.IsChecked.ToString());
                item.CheckAdd("SECONDARY_FLAG", SecondaryScheme.IsChecked.ToString());

                if (item.CheckGet("IDTSTREE").ToInt() == id)
                {
                    item.CheckAdd("QTY_NORM", QtyNorm.Text);
                    item.CheckAdd("PEOPLE", People.Text);
                    item.CheckAdd("SPEED", Speed.Text);
                    item.CheckAdd("NOTE", SchemeNoteTextBox.Text);
                    item.CheckAdd("ID2", v.CheckGet("ID2").ToString());
                    item.CheckAdd("IDK1", v.CheckGet("IDK1").ToString());
                    item.CheckAdd("NAME", v.CheckGet("NAME").ToString());
                    item.CheckAdd("ARTIKUL", v.CheckGet("ARTIKUL").ToString());
                }
                if (item.CheckGet("QTY_NORM").ToDouble() == 0)
                {
                    item.CheckAdd("QTY_NORM", "1");
                }
                if (item.CheckGet("QTY_ST").ToInt() > 0)
                {
                    if(ProductionSchemeGrid.SelectedItem == item)
                    {
                        item.CheckAdd("QTY", QtyNorm.Text);
                    }
                    qty = 1.0;
                }
                else
                {
                    qty *= item.CheckGet("QTY_NORM").ToDouble();
                    item.CheckAdd("QTY", qty.ToString());
                }
            }
            ProductionSchemeGrid.UpdateItems(DS);
        }
        private void Save()
        {
            if (DS.Items.Count <= 1)
            {
                var dw = new DialogWindow("Выберите схему производства.", "Сохранение схемы производства");
                dw.ShowDialog();
            }
            else
            {
                var resume = true;
                var countPrimary = 0;
                var countSecond = 0;
                var i = 0;
                foreach (var item in DS.Items)
                {
                    if ( (i == 0 || i == DS.Items.Count-1) && item["ID2"].ToInt() == 0 )
                    {
                        resume = false;
                        var dw = new DialogWindow("Схема производства является неполной. Добавьте недостающее изделие.", "Сохранение схемы производства");
                        dw.ShowDialog();
                        break;
                    }
                    if (item["IDTSTREE_P"].ToInt() == 0 && item != DS.Items[0])
                    {
                        resume = false;
                        var dw = new DialogWindow("Схема производства является неполной. Добавьте недостающее изделие.", "Сохранение схемы производства");
                        dw.ShowDialog();
                        break;
                    }
                    if (item["SECONDARY_FLAG"].ToInt() == 1)
                    {
                        countSecond++;
                    }
                    if (item["DEFS"].ToInt() == 1)
                    {
                        countPrimary++;
                    }
                    i++;
                }
                if (countPrimary> 0 && (countSecond!= 0 || countPrimary != DS.Items.Count))
                {
                    resume = false;
                    var dw = new DialogWindow("Признак \"Главная схема\" должен стоять в каждой строке или отсутствовать.", "Сохранение схемы производства");
                    dw.ShowDialog();
                }
                else if(countSecond > 0 && (countPrimary != 0 || countSecond != DS.Items.Count))
                {
                    resume = false;
                    var dw = new DialogWindow("Признак \"Второстепенная схема\" должен стоять в каждой строке или отсутствовать.", "Сохранение схемы производства");
                    dw.ShowDialog();
                }

                if (resume)
                {
                    var action = "update";
                    if (Idtls == 0)
                    {
                        action = "save";
                    }
                    var success = true;

                    string param = JsonConvert.SerializeObject(DS.Items);
                    var p = new Dictionary<string, string>();
                    {

                        p.CheckAdd("ACTION", action);
                        p.CheckAdd("ITEMS", param);
                    }
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sources/ProductionScheme");
                    q.Request.SetParam("Object", "ProductionScheme");
                    q.Request.SetParam("Action", "SaveOrUpdate");
                    q.Request.SetParams(p);

                    q.DoQuery();

                    if (q.Answer.Status != 0)
                    {
                        success = false;
                    }
                    if (success)
                    {
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverName = "ProductionSchemeTab",
                            SenderName = ControlName,
                            Action = "ReturnId2",
                            Message = DS.Items[0].CheckGet("ID2").ToString(),
                        });
                        Close();
                    }


                }

            }
        }
        private void QtyNorm_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            RunTemplateTimeoutTimer();
        }
        private void Speed_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            RunTemplateTimeoutTimer();
        }
        private void People_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            RunTemplateTimeoutTimer();
        }
        private void SchemeNoteTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            RunTemplateTimeoutTimer();
        }
        private void PrimaryScheme_Click(object sender, RoutedEventArgs e)
        {
            var primary = PrimaryScheme.IsChecked;
            var second = SecondaryScheme.IsChecked;
            if (primary == true && second == true)
            {
                SecondaryScheme.IsChecked = false;
            }

            RunTemplateTimeoutTimer();
        }
        private void SecondaryScheme_Click(object sender, RoutedEventArgs e)
        {
            var primary = PrimaryScheme.IsChecked;
            var second = SecondaryScheme.IsChecked;
            if (second == true && primary == true)
            {
                PrimaryScheme.IsChecked = false;
            }
            RunTemplateTimeoutTimer();
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
                    row.CheckAdd("TIMEOUT", "300");
                    row.CheckAdd("DESCRIPTION", "");
                    Central.Stat.TimerAdd("AssortmentList_RunTemplateTimeoutTimer", row);
                }
                TemplateTimeoutTimer.Tick += (s, e) =>
                {
                    ProductionSchemeUpdateGrid();
                    StopTemplateTimeoutTimer();
                };
            }

            if (TemplateTimeoutTimer.IsEnabled)
            {
                TemplateTimeoutTimer.Stop();
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
        private void DisableControls()
        {
            FormToolbar.IsEnabled = false;
        }
        private void EnableControls()
        {
            FormToolbar.IsEnabled = true;
        }

        private void HelpButtonClick(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp-new/products_materials/typeschema");
        }
    }
}
