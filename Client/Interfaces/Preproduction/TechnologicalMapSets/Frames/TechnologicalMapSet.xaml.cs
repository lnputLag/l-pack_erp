using Client.Assets.HighLighters;
using Client.Common;
using Client.Common.Extensions;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Sources;
using DevExpress.Data.Filtering.Helpers;
using DevExpress.DirectX.StandardInterop.Direct2D;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using DevExpress.Xpf.Core.Internal;
using DevExpress.Xpo.DB;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Office.Interop.Excel;
using Newtonsoft.Json;
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
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Форма создания и редактирования комплекта техкарт
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class TechnologicalMapSet : ControlBase
    {
        public TechnologicalMapSet()
        {
            DocumentationUrl = "/";

            InitializeComponent();
            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    switch (m.Action)
                    {
                        case "ReturnTk":
                            if (m.ContextObject != null)
                            {
                                var obj = (Dictionary<string, string>)m.ContextObject;
                                switch (Tag)
                                {
                                    case "Box":
                                        IdBox = obj.CheckGet("ID_TK").ToInt();
                                        BoxName.Text = obj.CheckGet("NAME");
                                        Form.SetValueByPath("QTY_BOX", "1");
                                        CustId = obj.CheckGet("CUST_ID").ToInt();
                                        CustomerShort = obj.CheckGet("CUSTOMER_SHORT");
                                        QtyBox.Text = "1";
                                        break;
                                    case "Partition":

                                        IdPartition1 = obj.CheckGet("ID_TK").ToInt();
                                        PartitionName.Text = obj.CheckGet("NAME");
                                        if (obj.CheckGet("ID_PCLASS").ToInt().ContainsIn(100, 229))
                                        {
                                            SetSecondPartition(IdPartition1);
                                        }
                                        else
                                        {
                                            QtyPartition1.Text = "1";
                                            IdPartition2 = 0;
                                            Partition2Name.Text = "";
                                            Form.SetValueByPath("QTY_PARTITION2", "0");
                                            QtyPartition2.Text = "";
                                        }
                                        break;
                                    case "Gasket":
                                        IdGasket = obj.CheckGet("ID_TK").ToInt();
                                        GasketName.Text = obj.CheckGet("NAME");
                                        QtyGasket.Text = "1";
                                        break;
                                    case "Liner":
                                        IdLiner = obj.CheckGet("ID_TK").ToInt();
                                        LinerName.Text = obj.CheckGet("NAME");
                                        Form.SetValueByPath("QTY_LINER", "1");
                                        QtyLiner.Text = "1";
                                        break;
                                    case "Sheet":
                                        IdSheet = obj.CheckGet("ID_TK").ToInt();
                                        SheetName.Text = obj.CheckGet("NAME");
                                        Form.SetValueByPath("QTY_SHEET", "1");
                                        QtySheet.Text = "1";
                                        break;
                                }
                                SetButtons();
                            }

                            break;
                        default:
                            break;
                    }

                }
            };
            FrameMode = 1;
            OnGetFrameTitle = () =>
            {
                var result = "";

                if (IsCreate == 1)
                {
                    result = $"Добавление комплекта";
                }
                else
                {
                    result = $"Изменение комплекта #{IdSet}";
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

            }
            Commander.Init(this);
            OnLoad = () =>
            {
                FormInit();
                SetButtons();
            };

        }

        #region "Переменные"
        public FormHelper Form { get; set; }
        public int IdSet { get; set; }
        public string PathSet { get; set; }
        public int CustId { get; set; }


        public int IdBox { get; set; }
        public int IdPartition1 { get; set; }
        public int IdPartition2 { get; set; }
        public int IdGasket { get; set; }
        public int IdSheet { get; set; }
        public int IdLiner { get; set; }

        public int IdBoxOld { get; set; }
        public int IdPartition1Old { get; set; }
        public int IdPartition2Old { get; set; }
        public int IdGasketOld { get; set; }
        public int IdSheetOld { get; set; }
        public int IdLinerOld { get; set; }

        public string Tag { get; set; }
        public int IsCreate { get; set; }
        public bool IsUpdateCount { get; set; }

        public string CustomerShort { get; set; }

        public string Msg { get; set; }
        public string ReciverName { get; set; }
        #endregion

        public void FormInit()
        {
            Form = new FormHelper();

            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="BOX_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=BoxName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PARTITION_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=PartitionName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PARTITION2_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Partition2Name,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="GASKET_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=GasketName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="LINER_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=LinerName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SHEET_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=SheetName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QTY_BOX",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QtyBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QTY_PARTITION1",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QtyPartition1,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QTY_PARTITION2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QtyPartition2,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QTY_GASKET",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QtyGasket,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QTY_LINER",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QtyLiner,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QTY_SHEET",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QtySheet,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };
            Form.ToolbarControl = null;
            Form.SetFields(fields);
        }
        private void HelpButtonClick(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp-new/products_materials/typeschema");
        }



        public void Open(int id_set)
        {
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_SET", id_set.ToString());
            }
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "TechnologicalMapSets");
            q.Request.SetParam("Action", "GetSet");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        foreach (var item in ds.Items)
                        {
                            var tag = "";
                            if (item.CheckGet("ID_PCLASS").ToInt().ContainsIn(10, 11, 110, 111, 112, 121, 106, 218, 220, 219, 215, 17, 105, 2, 3, 4, 120, 109, 107, 108, 116, 114, 115, 113,122))
                            {
                                IdBox = item.CheckGet("ID_TK").ToInt();
                                IdBoxOld = IdBox;
                                BoxName.Text = item.CheckGet("NAME");
                                QtyBox.Text = item.CheckGet("QTY");
                                CustId = item.CheckGet("CUST_ID").ToInt();
                            }
                            if (item.CheckGet("ID_PCLASS").ToInt().ContainsIn(12, 225))
                            {
                                IdPartition1 = item.CheckGet("ID_TK").ToInt();
                                IdPartition1Old = IdPartition1;
                                PartitionName.Text = item.CheckGet("NAME");
                                QtyPartition1.Text = item.CheckGet("QTY");
                            }
                            if (item.CheckGet("ID_PCLASS").ToInt().ContainsIn(100, 229))
                            {
                                if (item.CheckGet("MAIN_GRATE").ToInt() == 1)
                                {
                                    IdPartition1 = item.CheckGet("ID_TK").ToInt();
                                    IdPartition1Old = IdPartition1;
                                    PartitionName.Text = item.CheckGet("NAME");
                                    QtyPartition1.Text = item.CheckGet("QTY");
                                }
                                else
                                {
                                    IdPartition2 = item.CheckGet("ID_TK").ToInt();
                                    IdPartition2Old = IdPartition2;
                                    Partition2Name.Text = item.CheckGet("NAME");
                                    QtyPartition2.Text = item.CheckGet("QTY");
                                }
                            }
                            if (item.CheckGet("ID_PCLASS").ToInt().ContainsIn(8, 9))
                            {
                                IdPartition1 = item.CheckGet("ID_TK").ToInt();
                                IdPartition1Old = IdPartition1;
                                PartitionName.Text = item.CheckGet("NAME");
                                QtyPartition1.Text = item.CheckGet("QTY");
                            }
                            if (item.CheckGet("ID_PCLASS").ToInt().ContainsIn(14, 15, 226))
                            {
                                IdGasket = item.CheckGet("ID_TK").ToInt();
                                IdGasketOld = IdGasket;
                                GasketName.Text = item.CheckGet("NAME");
                                QtyGasket.Text = item.CheckGet("QTY");
                            }
                            if (item.CheckGet("ID_PCLASS").ToInt().ContainsIn(7, 228))
                            {
                                IdLiner = item.CheckGet("ID_TK").ToInt();
                                IdLinerOld = IdLiner;
                                LinerName.Text = item.CheckGet("NAME");
                                QtyLiner.Text = item.CheckGet("QTY");
                            }
                            if (item.CheckGet("ID_PCLASS").ToInt().ContainsIn(1, 216))
                            {
                                IdSheet = item.CheckGet("ID_TK").ToInt();
                                IdSheetOld = IdSheet;
                                SheetName.Text = item.CheckGet("NAME");
                                QtySheet.Text = item.CheckGet("QTY");
                            }
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
            SetButtons();
        }

        /// <summary>
        /// Сохранение/обновление/удаление комплекта
        /// </summary>
        public void Save()
        {
            // Главное издение
            // Может только добавляться и удаляться(в конце)
            if (IdBoxOld > 0)
            {
                if (IdBox > 0)
                {
                    if (IdBox != IdBoxOld)
                    {
                        SeveranceTk(IdBoxOld, "Ящик");
                        UnionTk(IdBox, QtyBox.Text.ToInt(), "Ящик", 1);
                    }
                    else
                    {
                        UpdateQty(IdBox, QtyBox.Text.ToInt());
                    }
                }
            }
            else
            {
                CreateTkSetRecord();
                if (IdBox > 0)
                {
                    UnionTk(IdBox, QtyBox.Text.ToInt(), "Ящик", 1);
                }
            }

            // Решетка
            if (IdPartition1Old > 0)
            {
                if (IdPartition1 > 0)
                {
                    if (IdPartition1 != IdPartition1Old)
                    {
                        SeveranceTk(IdPartition1Old, "Комплект_решеток");
                        UnionTk(IdPartition1, QtyPartition1.Text.ToInt(), "Комплект_решеток", 0);
                    }
                    else
                    {
                        UpdateQty(IdPartition1, QtyPartition1.Text.ToInt());
                        if (IdPartition2 > 0)
                        {
                            UpdateQty(IdPartition2, QtyPartition2.Text.ToInt());
                        }
                    }
                }
                else
                {
                    SeveranceTk(IdPartition1Old, "Комплект_решеток");
                }
            }
            else
            {
                if (IdPartition1 > 0)
                {
                    UnionTk(IdPartition1, QtyPartition1.Text.ToInt(), "Комплект_решеток", 0);
                }
            }

            // Прокладка
            if (IdGasketOld > 0)
            {
                if (IdGasket > 0)
                {
                    if (IdGasket != IdGasketOld)
                    {
                        SeveranceTk(IdGasketOld, "Прокладка");
                        UnionTk(IdGasket, QtyGasket.Text.ToInt(), "Прокладка", 0);
                    }
                    else
                    {
                        UpdateQty(IdGasket, QtyGasket.Text.ToInt());
                    }
                }
                else
                {
                    SeveranceTk(IdGasketOld, "Прокладка");
                }
            }
            else
            {
                if (IdGasket > 0)
                {
                    UnionTk(IdGasket, QtyGasket.Text.ToInt(), "Прокладка", 0);
                }
            }

            // Вкладыш
            if (IdLinerOld > 0)
            {
                if (IdLiner > 0)
                {
                    if (IdLiner != IdLinerOld)
                    {
                        SeveranceTk(IdLinerOld, "Вкладыш");
                        UnionTk(IdLiner, QtyLiner.Text.ToInt(), "Вкладыш", 0);
                    }
                    else
                    {
                        UpdateQty(IdLiner, QtyLiner.Text.ToInt());
                    }
                }
                else
                {
                    SeveranceTk(IdLinerOld, "Вкладыш");
                }
            }
            else
            {
                if (IdLiner > 0)
                {
                    UnionTk(IdLiner, QtyLiner.Text.ToInt(), "Вкладыш", 0);
                }
            }

            // Лист
            if (IdSheetOld > 0)
            {
                if (IdSheet > 0)
                {
                    if (IdSheet != IdSheetOld)
                    {
                        SeveranceTk(IdSheetOld, "Лист");
                        UnionTk(IdSheet, QtySheet.ToInt(), "Лист", 0);
                    }
                    else
                    {
                        UpdateQty(IdSheet, QtySheet.ToInt());
                    }
                }
                else
                {
                    SeveranceTk(IdSheetOld, "Лист");
                }
            }
            else
            {
                if (IdSheet > 0)
                {
                    UnionTk(IdSheet, QtySheet.Text.ToInt(), "Лист", 0);
                }
            }

            var msg = "";
            if (IdBoxOld > 0 && IdBox == 0)
            {
                var result = DeleteSetDetails(IdBoxOld);

                if (result)
                {
                    var parametrs = GetTkParams(IdBoxOld);
                    var excel = new TechnologicalMapExcel(parametrs);

                    var name = parametrs.CheckGet("PATHTK");
                    name = name.Replace(" в компл.xls", ".xls");

                    excel.FileNameOld = parametrs.CheckGet("PATHTK");
                    excel.FileNameNew = name;

                    excel.RenameExcelFile(parametrs.CheckGet("PATHTK"), name);
                    
                    UpdatePathTk(IdBoxOld, name);
                }
                msg = "Комплект успешно удален";
            }
            else
            {
                msg = "Комплект успешно сохранен";
            }

            if (Msg == null || Msg == "")
            {
                var dw = new DialogWindow(msg, "Изменение комплекта", "", DialogWindowButtons.OKCancel);
                dw.ShowDialog();
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverName = ReciverName,
                    SenderName = ControlName,
                    Action = "SetCreated",
                    Message = IdSet.ToString(),
                });
                Close();
            }
            else
            {
                var dw = new DialogWindow(Msg, "Изменение комплекта", "", DialogWindowButtons.OKCancel);
                dw.ShowDialog();
            }
        }

        /// <summary>
        /// Создание записи в tk_set
        /// PathSet переименовывается с постфиксом "в компл."
        /// </summary>
        private void CreateTkSetRecord()
        {
            if (IdBox == 0)
            {
                Msg = "Ошибка создания комплекта. Не выбрано главное изделие.";
            }
            else
            {
                var parametrs = GetTkParams(IdBox);
                var excel = new TechnologicalMapExcel(parametrs);
                var name = excel.GetExcelName(IdBox);
                var set_name = name;
                set_name = set_name.Replace(".xls", "");
                set_name = set_name.Replace("_", "/");
                set_name = CustomerShort + " " + set_name;

                name = name.Replace(".xls", " в компл.xls");

                var p = new Dictionary<string, string>()
                {
                    {"PATHTK", name},
                    {"FILE_NAME", set_name}
                };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "TechnologicalMapSets");
                q.Request.SetParam("Action", "AddSet");
                q.Request.SetParams(p);

                q.DoQuery();


                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");

                            if (ds != null && ds.Items.Count != 0)
                            {
                                IdSet = ds.Items[0].CheckGet("ID_SET").ToInt();
                                PathSet = name;
                            }
                        }
                    }
                }

            }
        }

        private void UnionTk(int id_tk, int qty, string sheetName, int main)
        {
            bool resume = true;

            var parametrs = GetTkParams(id_tk);
            var excel = new TechnologicalMapExcel(parametrs);

            if (parametrs.CheckGet("ID_PCLASS").ToInt().ContainsIn(8, 9))
            {
                sheetName = "Решетка";
            }

            if (main == 1)
            {
                excel.FileNameOld = parametrs.CheckGet("PATHTK");
                excel.FileNameNew = PathSet;
                excel.RenameExcelFile(parametrs.CheckGet("PATHTK"), PathSet);
            }
            else
            {
                if (parametrs.CheckGet("PATHTK") != PathSet)
                {
                    excel.CopyExcelSheet(sheetName, PathSet);
                }
            }

            if (excel.Msg != "")
            {
                resume = false;
                Msg = excel.Msg;
            }

            if (resume)
            {
                resume = CreateSetDetails(id_tk, qty, main);
                if (resume && parametrs.CheckGet("ID_PCLASS").ToInt().ContainsIn(100, 229))
                {
                    resume = CreateSetDetails(IdPartition2, QtyPartition2.Text.ToInt(), 0);
                }
            }
            if (resume)
            {
                resume = UpdatePathTk(id_tk, PathSet);
                if (resume && parametrs.CheckGet("ID_PCLASS").ToInt().ContainsIn(100, 229))
                {
                    resume = UpdatePathTk(IdPartition2, PathSet);
                }
            }
        }

        public void SeveranceTk(int id_tk, string sheetName)
        {
            bool resume = true;

            var parametrs = GetTkParams(id_tk);
            if (parametrs.CheckGet("ID_PCLASS").ToInt().ContainsIn(8, 9))
            {
                sheetName = "Решетка";
            }
            var excel = new TechnologicalMapExcel(parametrs);

            excel.SeveranceExcelSheet(sheetName, PathSet);

            if (excel.Msg != "")
            {
                resume = false;
                Msg = excel.Msg;
            }

            if (resume)
            {
                resume = UpdatePathTk(id_tk, excel.FileNameNew);
                if (resume && parametrs.CheckGet("ID_PCLASS").ToInt().ContainsIn(100, 229))
                {
                    resume = UpdatePathTk(IdPartition2Old, excel.FileNameNew);
                }
            }

            if (resume)
            {
                resume = DeleteSetDetails(id_tk);
                if (resume && parametrs.CheckGet("ID_PCLASS").ToInt().ContainsIn(100, 229))
                {
                    resume = DeleteSetDetails(IdPartition2Old);
                }
            }
        }

        private async void UpdateQty(int id_tk, int qty)
        {
            var p = new Dictionary<string, string>(){
                {"ID_SET", IdSet.ToString()},
                {"ID_TK", id_tk.ToString()},
                {"QTY", qty.ToString()}
            };

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "TechnologicalMapSets");
            q.Request.SetParam("Action", "UpdateQty");
            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            bool ans = false;
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");

                    if (ds != null && ds.Items.Count != 0)
                    {
                        if (ds.Items[0].CheckGet("SUCCESS").ToInt() == 1)
                        {
                            ans = true;
                        }
                        else
                        {
                            Msg = $"Ошибка обновления количества комплектующего id_tk = {id_tk} в комплекте id_set = {IdSet}.";
                        }
                    }
                    else
                    {
                        Msg = "Ошибка десериализации при обновлении количества комплектующего.";
                    }
                }
                else
                {
                    Msg = $"Результат запроса на обновление количества комплектующего id_tk = {id_tk} в комплекте id_set = {IdSet} равен null.";
                }
            }
            else
            {
                q.ProcessError();
            }
            return;
        }


        private Dictionary<string, string> GetTkParams(int id_tk)
        {
            var parametrs = new Dictionary<string, string>();
            var p = new Dictionary<string, string>(){
                {"ID_TK", id_tk.ToString()},
            };

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "TechnologicalMapSets");
            q.Request.SetParam("Action", "GetData");
            q.Request.SetParams(p);


            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");

                        if (ds != null && ds.Items.Count != 0)
                        {
                            parametrs = ds.Items[0];
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
            return parametrs;
        }

        private bool CreateSetDetails(int id_tk, int qty, int main)
        {
            var p = new Dictionary<string, string>(){
                {"ID_SET", IdSet.ToString()},
                {"ID_TK", id_tk.ToString()},
                {"QTY", qty.ToString()},
                {"MAIN", main.ToString()},
            };

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "TechnologicalMapSets");
            q.Request.SetParam("Action", "AddSetDetails");
            q.Request.SetParams(p);


            q.DoQuery();

            bool ans = false;
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");

                    if (ds != null && ds.Items.Count != 0)
                    {
                        if (ds.Items[0].CheckGet("ID_TK_SET").ToInt() > 0)
                        {
                            ans = true;
                        }
                        else
                        {
                            Msg = $"Ид созданной записи в таблице tk_set_details {ds.Items[0].CheckGet("ID_TK_SET")}";
                        }
                    }
                    else
                    {
                        Msg = "Ошибка десериализации при создании записи в таблице tk_set_details.";
                    }
                }
                else
                {
                    Msg = "Результат запроса создания записи в таблице tk_set_details равен null.";
                }
            }
            else
            {
                q.ProcessError();
            }
            return ans;
        }

        private bool DeleteSetDetails(int id_tk)
        {
            var p = new Dictionary<string, string>(){
                {"ID_SET", IdSet.ToString()},
                {"ID_TK", id_tk.ToString()}
            };

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "TechnologicalMapSets");
            q.Request.SetParam("Action", "DeleteSetDetail");
            q.Request.SetParams(p);


            q.DoQuery();

            bool ans = false;
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");

                    if (ds != null && ds.Items.Count != 0)
                    {
                        if (ds.Items[0].CheckGet("SUCCESS").ToInt() == 1)
                        {
                            ans = true;
                        }
                        else
                        {
                            Msg = $"Ошибка удаления комплектующего id_tk = {id_tk}.";
                        }
                    }
                    else
                    {
                        Msg = "Ошибка десериализации при удалении комплектующего.";
                    }
                }
                else
                {
                    Msg = "Результат запроса удаления записи из таблицы tk_set_details равен null.";
                }
            }
            else
            {
                q.ProcessError();
            }
            return ans;
        }

        private bool UpdatePathTk(int id_tk, string path)
        {
            var p = new Dictionary<string, string>(){
                {"ID_TK", id_tk.ToString()},
                {"FILE_NAME", path}
            };

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "TechnologicalMapSets");
            q.Request.SetParam("Action", "UpdatePathTK");
            q.Request.SetParams(p);


            q.DoQuery();

            bool ans = false;
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEM");

                    if (ds != null && ds.Items.Count != 0)
                    {
                        if (ds.Items[0].CheckGet("SUCCESS").ToInt() == 1)
                        {
                            ans = true;
                        }
                        else
                        {
                            Msg = $"Ошибка обновления пути для id_tk = {id_tk}. Имя файла {path}.";
                        }
                    }
                    else
                    {
                        Msg = $"Ошибка десериализации при обновлении пути для id_tk = {id_tk}.";
                    }
                }
                else
                {
                    Msg = $"Результат запроса обновления пути для id_tk = {id_tk} равен null.";
                }
            }
            else
            {
                q.ProcessError();
            }
            return ans;
        }

        private async void SetSecondPartition(int id_tk)
        {
            var p = new Dictionary<string, string>(){
                {"ID_TK", id_tk.ToString()},
            };

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "TechnologicalMapSets");
            q.Request.SetParam("Action", "GetSecondPartition");
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
                    var ds = ListDataSet.Create(result, "ITEMS");

                    if (ds != null && ds.Items.Count != 0)
                    {
                        IdPartition1 = ds.Items[0].CheckGet("ID_TK1").ToInt();
                        PartitionName.Text = ds.Items[0].CheckGet("NAME_PARTITION1");
                        QtyPartition1.Text = ds.Items[0].CheckGet("QTY1");
                        QtyPartition2.Text = ds.Items[0].CheckGet("QTY2");
                        IdPartition2 = ds.Items[0].CheckGet("ID_TK2").ToInt();
                        Partition2Name.Text = ds.Items[0].CheckGet("NAME_PARTITION2");
                    }
                }
            }
        }

        public void DeleteSet(int id_set)
        {
            IdBox = 0;
            IdGasket = 0;
            IdLiner = 0;
            IdSheet = 0;
            IdPartition1 = 0;
            IdPartition2 = 0;
            Save();
            var p = new Dictionary<string, string>(){
                {"ID_SET", IdSet.ToString()},
            };

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "TechnologicalMapSets");
            q.Request.SetParam("Action", "DeleteSet");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");

                    if (ds != null && ds.Items.Count != 0)
                    {
                    }
                }
            }
        }

        private void SetButtons()
        {
            if (IsUpdateCount)
            {
                BoxSelectButton.IsEnabled = false;
                PartitionSelectButton.IsEnabled = false;
                GasketSelectButton.IsEnabled = false;
                LinerSelectButton.IsEnabled = false;
                SheetSelectButton.IsEnabled = false;
                BoxDeleteButton.IsEnabled = false;
                PartitionDeleteButton.IsEnabled = false;
                GasketDeleteButton.IsEnabled = false;
                SheetDeleteButton.IsEnabled = false;
                LinerDeleteButton.IsEnabled = false;
            }
            else
            {
                if (CustId > 0)
                {
                    BoxSelectButton.IsEnabled = false;
                    PartitionSelectButton.IsEnabled = true;
                    GasketSelectButton.IsEnabled = true;
                    LinerSelectButton.IsEnabled = true;
                    SheetSelectButton.IsEnabled = true;
                }
                else
                {
                    BoxSelectButton.IsEnabled = true;
                    PartitionSelectButton.IsEnabled = false;
                    GasketSelectButton.IsEnabled = false;
                    LinerSelectButton.IsEnabled = false;
                    SheetSelectButton.IsEnabled = false;
                }
                if (IdSet > 0 || IdSheet > 0 || IdLiner > 0 || IdPartition1 > 0 || IdPartition2 > 0 || IdGasket > 0)
                {
                    BoxDeleteButton.IsEnabled = false;
                }
                else
                {
                    BoxDeleteButton.IsEnabled = true;
                }

                if (PartitionName.Text.IsNullOrEmpty())
                {
                    PartitionDeleteButton.IsEnabled = false;
                }
                else
                {
                    PartitionDeleteButton.IsEnabled = true;
                }
                if (GasketName.Text.IsNullOrEmpty())
                {
                    GasketDeleteButton.IsEnabled = false;
                }
                else
                {
                    GasketDeleteButton.IsEnabled = true;
                }
                if (SheetName.Text.IsNullOrEmpty())
                {
                    SheetDeleteButton.IsEnabled = false;
                }
                else
                {
                    SheetDeleteButton.IsEnabled = true;
                }
                if (LinerName.Text.IsNullOrEmpty())
                {
                    LinerDeleteButton.IsEnabled = false;
                }
                else
                {
                    LinerDeleteButton.IsEnabled = true;
                }
            }
        }

        private void SelectOnClick(object sender, RoutedEventArgs e)
        {
            var b = (System.Windows.Controls.Button)sender;
            if (b != null)
            {
                var t = b.Tag.ToString();

                var i = new TkForTkSetForm();
                string classes = "";
                switch (t)
                {
                    case "Box":
                        classes = "10,11,110,111,112,121,106,218,220,219,215,17,105,2,3,4,120,109,107,108,116,114,115,113,122";
                        break;
                    case "Partition":
                        classes = "12,100,225,229,8,9";
                        break;
                    case "Gasket":
                        classes = "14,15,226";
                        break;
                    case "Liner":
                        classes = "7,228";
                        break;
                    case "Sheet":
                        classes = "1,216";
                        break;
                }
                Tag = t;
                i.ReciverName = ControlName;
                i.IdPclass = classes;
                i.CustId = CustId;
                i.Show();
                i.Focus();
            }
        }
        private void DeleteOnClick(object sender, RoutedEventArgs e)
        {
            var b = (System.Windows.Controls.Button)sender;
            if (b != null)
            {
                var t = b.Tag.ToString();
                switch (t)
                {
                    case "Box":
                        IdBox = 0;
                        CustId = 0;
                        BoxName.Text = "";
                        QtyBox.Text = "";
                        break;
                    case "Partition":
                        IdPartition1 = 0;
                        PartitionName.Text = "";
                        QtyPartition1.Text = "";

                        IdPartition2 = 0;
                        Partition2Name.Text = "";
                        QtyPartition2.Text = "";
                        break;
                    case "Gasket":
                        IdGasket = 0;
                        GasketName.Text = "";
                        QtyGasket.Text = "";
                        break;
                    case "Liner":
                        IdLiner = 0;
                        LinerName.Text = "";
                        QtyLiner.Text = "";
                        break;
                    case "Sheet":
                        IdSheet = 0;
                        SheetName.Text = "";
                        QtySheet.Text = "";
                        break;

                }
                SetButtons();
            }
        }


    }
}