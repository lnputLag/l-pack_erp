using Client.Assets.HighLighters;
using Client.Common;
using Client.Common.Extensions;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Shipments;
using Client.Interfaces.Sources;
using DevExpress.Data.Filtering.Helpers;
using DevExpress.DirectX.StandardInterop.Direct2D;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using DevExpress.Xpf.Core.Internal;
using DevExpress.Xpo.DB;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Office.Interop.Excel;
using Newtonsoft.Json;
using NPOI.OpenXmlFormats.Shared;
using NPOI.OpenXmlFormats.Spreadsheet;
using NPOI.POIFS.Crypt.Dsig;
using NPOI.SS.Formula.Functions;
using NPOI.Util;
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
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Xml.Linq;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Форма создания и редактирования заявки на техкарту
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class WebTechnologicalMapForm : ControlBase
    {
        public WebTechnologicalMapForm()
        {
            DocumentationUrl = "/";

            InitializeComponent();

            FormInit();
            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    

                }
            };
            FrameMode = 1;
            OnGetFrameTitle = () =>
            {
                var result = "";

                if (IsCreate == 1)
                {
                    result = $"Заяввка на техкарту";
                }
                else
                {
                    result = $"Заяввка на техкарту #{IdTk}";
                }
                return result;
            };


            Commander.SetCurrentGroup("main_form");
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
                        SaveOrUpdate();
                        
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

            }
            Commander.Init(this);
            OnLoad = () =>
            {
                SetDefaults();
            };

        }

        #region "Переменные"
        public FormHelper Form { get; set; }
        ListDataSet formDS { get; set; }
        ListDataSet PokupatelContactDS { get; set; }
        public int IdTk { get; set; }
        public int IsCreate { get; set; }
        public int FlagUpdateOrder { get; set; }
        public string ReciverName { get; set; }
        #endregion

        #region "Загрузка справочников"
        /// <summary>
        /// Загрузка покупателей, типа продукции и профиля
        /// </summary>
        public async void SetDefaults()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "LoadRef");

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {

                    var pokupatel = ListDataSet.Create(result, "POKUPATEL");
                    var productClass = ListDataSet.Create(result, "TYPE_PRODUCT");
                    var profil = ListDataSet.Create(result, "PROFIL");
                    var pok = new Dictionary<string, string>();
                    var prodClass = new Dictionary<string, string>();
                    var prof = new Dictionary<string, string>();
                    var fact = new Dictionary<string, string>();

                    foreach (var item in pokupatel.Items)
                    {
                        if (item["ID"].ToInt() != 0)
                        {
                            pok.CheckAdd(item["ID"].ToInt().ToString(), item.CheckGet("NAME"));

                        }
                    }
                    PokupatelSelectBox.Items = pok;

                    foreach (var item in productClass.Items)
                    {
                        if (item["ID"].ToInt() != 0)
                        {
                            prodClass.CheckAdd(item["ID"].ToInt().ToString(), item.CheckGet("NAME"));

                        }
                    }
                    TypeProductSelectBox.Items = prodClass;

                    foreach (var item in profil.Items)
                    {
                        if (item["ID"].ToInt() != 0)
                        {
                            prof.CheckAdd(item["ID"].ToInt().ToString(), item.CheckGet("NAME"));

                        }
                    }
                    ProfilSelectBox.Items = prof;

                    fact.CheckAdd("1", "Липецк");
                    fact.CheckAdd("2", "Кашира");
                    FactIdSelectBox.Items = fact;

                    Open();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Загрузка картона
        /// </summary>
        public async void SetCarton()
        {
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_POK", PokupatelSelectBox.SelectedItem.Key.ToString());
                p.CheckAdd("ID_PROF", ProfilSelectBox.SelectedItem.Key.ToString());
            }
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "LoadCartonRef");
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

                    var cartonList = ListDataSet.Create(result, "CARTON");
                    var carton = new Dictionary<string, string>();

                    foreach (var item in cartonList.Items)
                    {
                        if (item["ID"].ToInt() != 0)
                        {
                            carton.CheckAdd(item["ID"].ToInt().ToString(), item.CheckGet("DESCRIPTION"));

                        }
                    }
                    if(formDS !=null && formDS.Items?.Count > 0)
                    {
                        carton.CheckAdd(formDS.Items.First().CheckGet("COMPOSITION").ToInt().ToString(), formDS.Items.First().CheckGet("DESCRIPTION"));
                        CartonSelectBox.Items = carton;
                        CartonSelectBox.SetSelectedItemByKey(formDS.Items.First().CheckGet("COMPOSITION").ToInt().ToString());
                    }
                    else
                    {
                        CartonSelectBox.Items = carton;
                        CartonSelectBox.SetSelectedItemFirst();
                    }

                }
            }
            else
            {
                q.ProcessError();
            }
        }

        

        /// <summary>
        /// Установка доступности полей
        /// </summary>
        public async void SetEnable()
        {
            Number.IsReadOnly = true;
            PokupatelSelectBox.IsReadOnly = true;
            TypeProductSelectBox.IsReadOnly = true;
            FactIdSelectBox.IsReadOnly = true;
            Length.IsReadOnly = true;
            Width.IsReadOnly = true;
            Height.IsReadOnly = true;
            OrderQty.IsReadOnly = true;
            ProfilSelectBox.IsReadOnly = true;
            CartonSelectBox.IsReadOnly = true;
            Color1.IsReadOnly = true;
            Color2.IsReadOnly = true;
            Color3.IsReadOnly = true;
            Color4.IsReadOnly = true;
            Color5.IsReadOnly = true;
            PrintFlag.IsEnabled = false;
            PackingFlag.IsEnabled = false;
            AutoAssemblyFlag.IsEnabled = false;
            NoteTextBox.IsReadOnly = true;
            LimitHeight.IsReadOnly = true;
        }

        /// <summary>
        /// Загрузка цветов
        /// </summary>
        public async void SetColors()
        {
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("IDC", CartonSelectBox.SelectedItem.Key.ToString());
            }
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "LoadColorRef");
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

                    var colorList = ListDataSet.Create(result, "COLOR");
                    var color = new Dictionary<string, string>();

                    foreach (var item in colorList.Items)
                    {
                        if (item["ID"].ToInt() != 0)
                        {
                            color.CheckAdd(item["ID"].ToInt().ToString(), item.CheckGet("NAME"));
                        }
                    }
                    Color1.Items = color;
                    Color2.Items = color;
                    Color3.Items = color;
                    Color4.Items = color;
                    Color5.Items = color;
                    Color1.GridDataSet = colorList;
                    Color2.GridDataSet = colorList;
                    Color3.GridDataSet = colorList;
                    Color4.GridDataSet = colorList;
                    Color5.GridDataSet = colorList;


                    if (formDS != null && formDS.Items?.Count > 0)
                    {
                        var name = formDS?.Items?.First().CheckGet("COLOR1");
                        if (!name.IsNullOrEmpty())
                        {
                            foreach (var c in colorList.Items)
                            {
                                if (name == c.CheckGet("NAME"))
                                {
                                    var d = new KeyValuePair<string, string>(c.CheckGet("ID"), c.CheckGet("NAME"));
                                    Color1.SetSelectedItem(d);
                                    break;
                                }
                            }
                            name = formDS?.Items?.First().CheckGet("COLOR2");
                            if (!name.IsNullOrEmpty())
                            {
                                foreach (var c in colorList.Items)
                                {
                                    if (name == c.CheckGet("NAME"))
                                    {
                                        var d = new KeyValuePair<string, string>(c.CheckGet("ID"), c.CheckGet("NAME"));
                                        Color2.SetSelectedItem(d);
                                        break;
                                    }
                                }
                                name = formDS?.Items?.First().CheckGet("COLOR3");
                                if (!name.IsNullOrEmpty())
                                {
                                    foreach (var c in colorList.Items)
                                    {
                                        if (name == c.CheckGet("NAME"))
                                        {
                                            var d = new KeyValuePair<string, string>(c.CheckGet("ID"), c.CheckGet("NAME"));
                                            Color3.SetSelectedItem(d);
                                            break;
                                        }
                                    }
                                    name = formDS?.Items?.First().CheckGet("COLOR4");
                                    if (!name.IsNullOrEmpty())
                                    {
                                        foreach (var c in colorList.Items)
                                        {
                                            if (name == c.CheckGet("NAME"))
                                            {
                                                var d = new KeyValuePair<string, string>(c.CheckGet("ID"), c.CheckGet("NAME"));
                                                Color4.SetSelectedItem(d);
                                                break;
                                            }
                                        }
                                        name = formDS?.Items?.First().CheckGet("COLOR5");
                                        if (!name.IsNullOrEmpty())
                                        {
                                            foreach (var c in colorList.Items)
                                            {
                                                if (name == c.CheckGet("NAME"))
                                                {
                                                    var d = new KeyValuePair<string, string>(c.CheckGet("ID"), c.CheckGet("NAME"));
                                                    Color5.SetSelectedItem(d);
                                                    break;
                                                }
                                            }

                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (FlagUpdateOrder == 0)
                    {
                        Color1.IsReadOnly = true;
                        Color2.IsReadOnly = true;
                        Color3.IsReadOnly = true;
                        Color4.IsReadOnly = true;
                        Color5.IsReadOnly = true;
                    }
                    else
                    {
                        Color1.IsEnabled = true;
                        Color2.IsEnabled = true;
                        Color3.IsEnabled = true;
                        Color4.IsEnabled = true;
                        Color5.IsEnabled = true;
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Удаление цветов
        /// </summary>
        public async void DelColors()
        {
            var color = new Dictionary<string, string>();
            color.CheckAdd("-1", "");
            Color1.Items = color;
            Color2.Items = color;
            Color3.Items = color;
            Color4.Items = color;
            Color5.Items = color;
            Color1.SetSelectedItemByKey("-1");
            Color2.SetSelectedItemByKey("-1");
            Color3.SetSelectedItemByKey("-1");
            Color4.SetSelectedItemByKey("-1");
            Color5.SetSelectedItemByKey("-1");

            Color1.IsEnabled = false;
            Color2.IsEnabled = false;
            Color3.IsEnabled = false;
            Color4.IsEnabled = false;
            Color5.IsEnabled = false;

        }
        #endregion

        public void FormInit()
        {
            Form = new FormHelper();

            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="NUM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Number,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="POKUPATEL",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PokupatelSelectBox,
                    ControlType="SelectBox",
                    OnChange = (FormHelperField field, string value) =>
                    {
                        if (PokupatelSelectBox.SelectedItem.Key.ToInt() > 0
                            && ProfilSelectBox.SelectedItem.Key.ToInt() > 0)
                        {
                            SetCarton();
                        }
                    },
                    Validate = (f, v) =>
                    {
                        if (PokupatelSelectBox.SelectedItem.Key.ToInt() <= 0)
                        {
                            f.ValidateResult = false;
                            f.ValidateProcessed = true;
                            f.ValidateMessage = "Заполните поле покупателя";
                        }
                    },
                },
                new FormHelperField()
                {
                    Path="FACT_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=FactIdSelectBox,
                    ControlType="SelectBox",
                    Validate = (f, v) =>
                    {
                        if (FactIdSelectBox.SelectedItem.Key.ToInt() <= 0)
                        {
                            f.ValidateResult = false;
                            f.ValidateProcessed = true;
                            f.ValidateMessage = "Выберите площадку";
                        }
                    },
                },
                new FormHelperField()
                {
                    Path="TYPE_PRODUCT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TypeProductSelectBox,
                    ControlType="SelectBox",
                    OnChange = (FormHelperField field, string value) =>
                    {
                        if (TypeProductSelectBox.SelectedItem.Key.ToInt().ContainsIn(1,14,15,121,221,7))
                        {
                            Height.Text = "";
                            Height.IsEnabled = false;
                        }
                        else
                        {
                            Height.IsEnabled = true;
                        }

                        if (TypeProductSelectBox.SelectedItem.Key.ToInt().ContainsIn(107,2,114))
                        {
                            AutoAssemblyFlag.IsEnabled = true;
                        }
                        else
                        {
                            AutoAssemblyFlag.IsChecked = false;
                            AutoAssemblyFlag.IsEnabled = false;
                        }
                        if (!TypeProductSelectBox.SelectedItem.Key.ToInt().ContainsIn(10,11))
                        {
                            OrderQty.Text = "";
                            OrderQty.IsEnabled = false;
                        }
                        else
                        {
                            OrderQty.IsEnabled = true;
                        }
                    },
                    Validate = (f, v) =>
                    {
                        if (TypeProductSelectBox.SelectedItem.Key.ToInt() <= 0)
                        {
                            f.ValidateResult = false;
                            f.ValidateProcessed = true;
                            f.ValidateMessage = "Заполните поле тип продукции";
                        }
                    },
                },
                new FormHelperField()
                {
                    Path="LENGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Length,
                    ControlType="TextBox",
                    Validate = (f, v) =>
                    {
                        if (Length.Text.ToInt() <= 0)
                        {
                            f.ValidateResult = false;
                            f.ValidateProcessed = true;
                            f.ValidateMessage = "Заполните поле длина";
                        }
                    },
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 4 },
                    },
                },
                new FormHelperField()
                {
                    Path="WIDTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Width,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 4 },
                    },
                    Validate = (f, v) =>
                    {
                        if (Width.Text.ToInt() <= 0)
                        {
                            f.ValidateResult = false;
                            f.ValidateProcessed = true;
                            f.ValidateMessage = "Заполните поле ширина";
                        }
                    },
                },
                new FormHelperField()
                {
                    Path="HEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Height,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 4 },
                    },
                    Validate = (f, v) =>
                    {
                        if (Height.Text.ToInt()<=0
                            && !TypeProductSelectBox.SelectedItem.Key.ToInt().ContainsIn(1, 14, 15, 121, 221, 7))
                        {
                            f.ValidateResult = false;
                            f.ValidateProcessed = true;
                            f.ValidateMessage = "Заполните поле высота";
                        }
                    },
                },
                new FormHelperField()
                {
                    Path="ORDER_QTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=OrderQty,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Validate = (f, v) =>
                    {
                        if (TypeProductSelectBox.SelectedItem.Key.ToInt().ContainsIn(10,11) && OrderQty.Text.ToInt()<=0)
                        {
                            f.ValidateResult = false;
                            f.ValidateProcessed = true;
                            f.ValidateMessage = "Заполните количество в партии";
                        }
                    },
                    
                },
                new FormHelperField()
                {
                    Path="PROFIL",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProfilSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        if (PokupatelSelectBox.SelectedItem.Key.ToInt() > 0 && ProfilSelectBox.SelectedItem.Key.ToInt()>0)
                        {
                            SetCarton();
                        }
                    },
                    Validate = (f, v) =>
                    {
                        if (ProfilSelectBox.SelectedItem.Key.ToInt() <= 0)
                        {
                            f.ValidateResult = false;
                            f.ValidateProcessed = true;
                            f.ValidateMessage = "Заполните поле профиль";
                        }
                    },
                },
                new FormHelperField()
                {
                    Path="COMPOSITION",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CartonSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Validate = (f, v) =>
                    {
                        if (CartonSelectBox.SelectedItem.Key.ToInt() <= 0)
                        {
                            f.ValidateResult = false;
                            f.ValidateProcessed = true;
                            f.ValidateMessage = "Заполните поле композиция";
                        }
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        if (PrintFlag.IsChecked == true)
                        {
                            SetColors();
                        }
                        else
                        {
                            DelColors();
                        }
                    }
                },
                new FormHelperField()
                {
                    Path="COLOR1",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Color1,
                    Enabled =false,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        var tt=Color1;
                    },
                },
                new FormHelperField()
                {
                    Path="COLOR2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Color2,
                    Enabled =false,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Validate = (f, v) =>
                    {
                        if (Color2.SelectedItem.Key.ToInt() > 0
                            && Color1.SelectedItem.Key.ToInt() <= 0)
                        {
                            f.ValidateResult = false;
                            f.ValidateProcessed = true;
                            f.ValidateMessage = "Заполните предыдущие цвета";
                        }
                    },
                },
                new FormHelperField()
                {
                    Path="COLOR3",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Color3,
                    Enabled =false,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Validate = (f, v) =>
                    {
                        if (Color3.SelectedItem.Key.ToInt() > 0
                            && (    Color1.SelectedItem.Key.ToInt() <= 0
                                ||  Color2.SelectedItem.Key.ToInt() <= 0))
                        {
                            f.ValidateResult = false;
                            f.ValidateProcessed = true;
                            f.ValidateMessage = "Заполните предыдущие цвета";
                        }
                    },
                },
                new FormHelperField()
                {
                    Path="COLOR4",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Color4,
                    Enabled =false,
                    ControlType="SelectBox",
                    Validate = (f, v) =>
                    {
                        if (Color4.SelectedItem.Key.ToInt() > 0
                            && (    Color1.SelectedItem.Key.ToInt() <= 0
                                ||  Color2.SelectedItem.Key.ToInt() <= 0
                                ||  Color3.SelectedItem.Key.ToInt() <= 0))
                        {
                            f.ValidateResult = false;
                            f.ValidateProcessed = true;
                            f.ValidateMessage = "Заполните предыдущие цвета";
                        }
                    },
                },
                new FormHelperField()
                {
                    Path="COLOR5",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Color5,
                    Enabled =false,
                    ControlType="SelectBox",
                    Validate = (f, v) =>
                    {
                        if (Color4.SelectedItem.Key.ToInt() > 0
                            && (    Color1.SelectedItem.Key.ToInt() <= 0
                                ||  Color2.SelectedItem.Key.ToInt() <= 0
                                ||  Color3.SelectedItem.Key.ToInt() <= 0
                                ||  Color4.SelectedItem.Key.ToInt() <= 0))
                        {
                            f.ValidateResult = false;
                            f.ValidateProcessed = true;
                            f.ValidateMessage = "Заполните предыдущие цвета";
                        }
                    },
                },
                new FormHelperField()
                {
                    Path="PRINT_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=PrintFlag,
                    ControlType="CheckBox",
                    OnChange = (FormHelperField field, string value) =>
                    {
                        if (PrintFlag.IsChecked == true && CartonSelectBox.SelectedItem.Key.ToInt()>0)
                        {
                            SetColors();
                        }
                        else
                        {
                            DelColors();
                        }
                    }
                },
                new FormHelperField()
                {
                    Path="PACKING_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=PackingFlag,
                    ControlType="CheckBox",
                    OnChange = (FormHelperField field, string value) =>
                    {
                        if (PackingFlag.IsChecked == true)
                        {
                            LimitHeight.IsEnabled = true;
                        }
                        else
                        {
                            LimitHeight.IsEnabled = false;
                            LimitHeight.Text="";
                        }
                    }
                },
                new FormHelperField()
                {
                    Path="AUTO_ASSEMBLY_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=AutoAssemblyFlag,
                    ControlType="CheckBox",
                },
                new FormHelperField()
                {
                    Path="LIMIT_HEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=LimitHeight,
                    Enabled =false,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 4 },
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                    Validate = (f, v) =>
                    {
                        if (LimitHeight.Text.ToInt()>2400)
                        {
                            f.ValidateResult = false;
                            f.ValidateProcessed = true;
                            f.ValidateMessage = "Максимальное значение предельной высоты 2400";
                        }
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=NoteTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                
            };
            Form.ToolbarControl = null;
            Form.SetFields(fields);
            
            var columns = new List<DataGridHelperColumn>()
            {
                new DataGridHelperColumn()
                {
                    Header="#",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=50,
                    Visible = false,
                },
                new DataGridHelperColumn()
                {
                    Header="Название",
                    Path="NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=163,
                },
                new DataGridHelperColumn
                {
                    Header="Код цвета",
                    Path="HEX",
                    ColumnType=ColumnTypeRef.String,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Цвет",
                    Path="_COLOR",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = row.CheckGet("HEX");

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = HexToBrush(color);
                                }

                                return result;
                            }
                        },
                    },
                },
            };
            Color1.GridColumns = columns;
            Color1.SelectedItemValue = "NAME";
            Color1.GridPrimaryKey = "ID";

            Color2.GridColumns = columns;
            Color2.SelectedItemValue = "NAME";
            Color2.GridPrimaryKey = "ID";

            Color3.GridColumns = columns;
            Color3.SelectedItemValue = "NAME";
            Color3.GridPrimaryKey = "ID";

            Color4.GridColumns = columns;
            Color4.SelectedItemValue = "NAME";
            Color4.GridPrimaryKey = "ID";

            Color5.GridColumns = columns;
            Color5.SelectedItemValue = "NAME";
            Color5.GridPrimaryKey = "ID";
        }

        public void Open()
        {
            if (IdTk > 0)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_TK", IdTk.ToString());
                }
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "WebTechnologicalMap");
                q.Request.SetParam("Action", "GetTkOrder");
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        formDS = ds;
                        
                        var pok = new Dictionary<string, string>();
                        pok.CheckAdd(ds.GetFirstItem().CheckGet("POKUPATEL").ToInt().ToString(), ds.GetFirstItem().CheckGet("POKUPATEL_NAME"));
                        PokupatelSelectBox.Items = pok;

                        Form.SetValues(ds);
                        PokupatelSelectBox.IsEnabled = false;

                        if (FlagUpdateOrder == 0)
                        {
                            SetEnable();
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
                PokupatelSelectBox.SetSelectedItemFirst();
                TypeProductSelectBox.SetSelectedItemFirst();
                ProfilSelectBox.SetSelectedItemFirst();
            }
            
        }
        public async void SaveOrUpdate()
        {
            var validationResult = Form.Validate();
            if (validationResult)
            {


                bool resume = true;
                var p = Form.GetValues();

                // Сохранение данных по заявке
                if(FlagUpdateOrder==1)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "WebTechnologicalMap");
                    q.Request.SetParam("Action", "AddOrUpdateOrder");
                    q.Request.SetParams(p);
                    q.Request.SetParam("ID_TK", IdTk.ToString());
                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var ds = ListDataSet.Create(result, "ITEM");
                            if (ds.Items[0].CheckGet("ID_TK").ToInt() > 0)
                            {
                                IdTk = ds.Items[0].CheckGet("ID_TK").ToInt();

                            }

                        }
                    }
                    else
                    {
                        q.ProcessError();
                        resume = false;
                    }
                }

                if (resume)
                {
                    var dw = new DialogWindow("Заявка успешно сохранена", "Заявка на ТК", "", DialogWindowButtons.OKCancel);
                    dw.ShowDialog();
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverName = ReciverName,
                        SenderName = ControlName,
                        Action = "AddOrUpdateOrder",
                        Message = IdTk.ToString(),
                    });
                    this.Close();
                }
            }
        }


        /// <summary>
        /// Функция перевода строки содержащей hex код цвета краски в цвет Brush
        /// <param name="hex_code">строка с hex числом</param>
        /// <return>Brush.цвет</return>
        /// </summary>
        private Brush HexToBrush(string hex_code)
        {
            SolidColorBrush result = null;
            var hexString = (hex_code as string).Replace("#", "");

            if (hexString.Length == 6)
            {
                var r = hexString.Substring(0, 2);
                var g = hexString.Substring(2, 2);
                var b = hexString.Substring(4, 2);

                result = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xff,
                   byte.Parse(r, System.Globalization.NumberStyles.HexNumber),
                   byte.Parse(g, System.Globalization.NumberStyles.HexNumber),
                   byte.Parse(b, System.Globalization.NumberStyles.HexNumber)));
            }

            return result;
        }
        private void HelpButtonClick(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }
        public void ShowHelp()
        {
            Central.ShowHelp("/");
        }


    }
}