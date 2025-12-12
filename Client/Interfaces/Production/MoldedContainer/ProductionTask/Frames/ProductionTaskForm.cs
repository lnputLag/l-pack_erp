using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.MoldedContainer
{
    /// <summary>
    /// производственное задание на литую тару
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2024-07-10</released>
    /// <changed>2024-07-10</changed>
    class ProductionTaskForm :FormDialog
    {
        public ProductionTaskForm()
        {

            CreateMode = 0;
            OrderId = 0;
            GoodsId = 0;
            IdorderDates = 0;
            TaskQuantity = 0;
            OrderReceiptData = "";

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if(!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };
        }

        /// <summary>
        /// 0=default,1=create_from_order, 2=автовыбор с возможностью менять
        /// </summary>
        public int CreateMode { get; set; }
        /// <summary>
        /// id заявки
        /// </summary>
        public int OrderId { get; set; }
        /// <summary>
        /// id2 Tovar
        /// </summary>
        public int GoodsId { get; set; }
        /// <summary>
        /// IdorderDates
        /// </summary>
        public int IdorderDates { get; set; }
        /// <summary>
        /// TASK_QUANTITY
        /// </summary>
        public int TaskQuantity { get; set; }
        /// <summary>
        /// Примечание из техкарты
        /// </summary>
        public string ProductionTaskNote { get; set; }
        
        /// <summary>
        /// Статус производственного задания. Нужно для блокировки интерфейса если статус = 4 (В работе); 5(Выполнено);
        /// </summary>
        public int StatusTask { get; set; }
        
        /// <summary>
        /// Дата доставки этикетки
        /// </summary>
        public string OrderReceiptData { get; set; }
        
        public void Init(string mode="create", string id="")
        {
            RoleName = "[erp]molded_contnr_productn_task";
            FrameName = "ProductionTask";
            Title = $"ПЗЛТ";
            Fields = new List<FormHelperField>()
            {
                // ИЗДЕЛИЕ
                new FormHelperField()
                {
                    Path="GOODS_ID",
                    Description="Изделие",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="SelectBox",
                    Width=450, 
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    OnCreate = (FormHelperField field) =>
                    {
                        var c=(SelectBox)field.Control;

                        if(Mode=="edit")
                        {
                            c.IsReadOnly = true;
                        }
                        if(CreateMode==1)
                        {
                            c.IsReadOnly = true;
                        }

                        var columns = new List<DataGridHelperColumn>()
                        {
                            new DataGridHelperColumn()
                            {
                                Header="Изделие",
                                Path="GOODS_NAME",
                                ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                                Width2=46,
                            },
                             new DataGridHelperColumn()
                            {
                                Header="Артикул",
                                Path="GOODS_CODE",
                                ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                                Width2=10,
                            },
                            new DataGridHelperColumn()
                            {
                                Header="ИД",
                                Path="GOODS_ID",
                                ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                                Width2=8,
                            }
                        };
                        c.GridColumns=columns;
                        c.SelectedItemValue="GOODS_NAME GOODS_CODE";

                        c.ListBoxMinWidth = 550;//424;
                        c.ListBoxMinHeight = 300;
                        //AccountGrid.SetSorting("LOGIN", ListSortDirection.Ascending);
                        
                        c.Style = FindResource("CustomFormField");
                        c.DataType = SelectBox.DataTypeRef.Grid;
                        c.GridPrimaryKey=field.Path;
                    },
                    OnChange=(FormHelperField field, string value)=>{
                        ChainUpdate("ORDER_POSITION_ID");
                        ChainUpdate("PRODUCTION_SCHEME_ID");
                        ChainUpdate("PRODUCTION_STAGE_ID");
                    },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "MoldedContainer",
                        Object = "Goods",
                        Action = "List",
                        AnswerSectionKey="ITEMS",
                        OnComplete = (FormHelperField field, ListDataSet ds) =>
                        {
                            var c=(SelectBox)field.Control;
                            c.GridDataSet = ds;
                            FieldSetValueActual("GOODS_ID");
                        },
                    },
                },
                // ЗАЯВКА
                new FormHelperField()
                {
                    Path="ORDER_POSITION_ID",
                    AutoloadItems=false,
                    Description="Заявка",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="SelectBox",
                    Width=450, 
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnCreate = (FormHelperField field) =>
                    {
                        var c=(SelectBox)field.Control;

                        if(CreateMode==1)
                        {
                            c.IsReadOnly = true;
                        }

                        var columns = new List<DataGridHelperColumn>()
                        {
                            new DataGridHelperColumn
                            {
                                Header="Заявка",
                                Path="ORDER_NUM",
                                Description="",
                                ColumnType=ColumnTypeRef.String,
                                Width2=30,
                            },
                            new DataGridHelperColumn
                            {
                                Header="Количество, шт",
                                Path="ORDER_QUANTITY",
                                Description="",
                                ColumnType=ColumnTypeRef.Integer,
                                Width2=8,
                            },
                             new DataGridHelperColumn
                            {
                                Header="ИД позиции",
                                Path="ORDER_POSITION_ID",
                                Description="",
                                ColumnType=ColumnTypeRef.Integer,
                                Width2=8,
                            }                      
                        };
                        c.GridColumns=columns;
                        c.SelectedItemValue="ORDER_NUM Количество: ORDER_QUANTITY";

                        c.ListBoxMinWidth = 450;
                        c.ListBoxMinHeight = 200;
                        c.Style = FindResource("CustomFormField");
                        c.DataType = SelectBox.DataTypeRef.Grid;
                        c.GridPrimaryKey=field.Path;
                    },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "MoldedContainer",
                        Object = "Order",
                        Action = "Get",
                        AnswerSectionKey="ITEMS",
                        BeforeRequest = (RequestData rd) =>
                        {
                            var id=FieldGetValueActual("GOODS_ID").ToInt();
                            var mode = "2";
                              if (IdorderDates == 0)
                                mode = "1";

                            if(id != 0)
                            {
                                rd.Params = new Dictionary<string, string>()
                                {
                                    { "MODE", mode },
                                    { "ORDER_ID", OrderId.ToString() },
                                    { "GOODS_ID", id.ToString() },
                                    { "ORDER_POSITION_ID",  CreateMode != 2 ?  IdorderDates.ToString() : "0" },
                                    
                                };
                            }
                        },
                        OnComplete = (FormHelperField field, ListDataSet ds) =>
                        {
                            var c=(SelectBox)field.Control;
                            c.GridDataSet = ds;
                            FieldSetValueActual(field.Path);
                        },
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCTION_SCHEME_ID",
                    AutoloadItems=false,
                    Description="Схема производства",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="SelectBox",
                    Width=450, 
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnCreate = (FormHelperField field) =>
                    {
                        var c=(SelectBox)field.Control;
                        
                        if (StatusTask == 4 || StatusTask == 5)
                        {
                            c.IsReadOnly = true;
                        }
                        
                        var columns = new List<DataGridHelperColumn>()
                        {
                            new DataGridHelperColumn
                            {
                                Header="Этап производства",
                                Path="PRODUCTION_STAGE_ID",
                                Description="",
                                ColumnType=ColumnTypeRef.Integer,
                                Width2=16,
                            },
                            new DataGridHelperColumn
                            {
                                Header="Схема производства",
                                Path="PRODUCTION_SCHEME_NAME",
                                Description="",
                                ColumnType=ColumnTypeRef.String,
                                Width2=24,
                            },
                            new DataGridHelperColumn
                            {
                                Header="Основная схема",
                                Path="PRODUCTION_SCHEME_PRIMARY_FLAG",
                                Description="",
                                ColumnType=ColumnTypeRef.Boolean,
                                Width2=6,
                            },
                            new DataGridHelperColumn
                            {
                                Header="ИД",
                                Path="PRODUCTION_SCHEME_ID",
                                Description="",
                                ColumnType=ColumnTypeRef.Integer,
                                Width2=8,
                            },
                        };
                        c.GridColumns=columns;
                        c.SelectedItemValue="PRODUCTION_SCHEME_NAME";

                        c.ListBoxMinWidth = 472;
                        c.ListBoxMinHeight = 200;
                        c.Style = FindResource("CustomFormField");
                        c.DataType = SelectBox.DataTypeRef.Grid;
                        c.GridPrimaryKey=field.Path;
                    },
                    OnChange=(FormHelperField field, string value)=>{
                        var c=(SelectBox)field.Control;

                        var productionStageId=c.SelectedRow.CheckGet("PRODUCTION_STAGE_ID");
                        Values.CheckAdd("PRODUCTION_STAGE_ID",productionStageId);
                        FieldSetValueActual("PRODUCTION_STAGE_ID");

                        ChainUpdate("PRODUCTION_MACHINE_ID");
                        ChainUpdate("PRODUCTION_STAGE_ID");
                    },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "MoldedContainer",
                        Object = "ProductionScheme",
                        Action = "ListForProductionTask",
                        AnswerSectionKey="ITEMS",
                        BeforeRequest = (RequestData rd) =>
                        {
                            var id=FieldGetValueActual("GOODS_ID").ToInt();
                            if(id != 0)
                            {
                                rd.Params = new Dictionary<string, string>()
                                {
                                    { "GOODS_ID", id.ToString() },
                                };
                            }
                        },
                        OnComplete = (FormHelperField field, ListDataSet ds) =>
                        {
                            var c=(SelectBox)field.Control;
                            
                            if (StatusTask == 4 || StatusTask == 5)
                            {
                                c.IsReadOnly = true;
                            }
                            
                            c.GridDataSet = ds;
                            
                            //если есть строка с флагом, она будет выбрана по умолчанию
                            {
                                foreach(Dictionary<string,string> row in ds.Items)
                                {
                                    if(row.CheckGet("PRODUCTION_SCHEME_PRIMARY_FLAG").ToBool())
                                    {
                                        var productionStageId=row.CheckGet("PRODUCTION_STAGE_ID").ToInt();
                                        Values.CheckAdd("PRODUCTION_STAGE_ID",productionStageId.ToString());
                                        FieldSetValueActual("PRODUCTION_STAGE_ID");

                                        var productionSchemeId=row.CheckGet("PRODUCTION_SCHEME_ID").ToInt();
                                        Values.CheckAdd("PRODUCTION_SCHEME_ID",productionSchemeId.ToString());
                                        FieldSetValueActual("PRODUCTION_SCHEME_ID");

                                        break;
                                    }
                                }
                            }
                            field.OnChange.Invoke(field,field.ActualValue);
                        },
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCTION_STAGE_ID",
                    Description="(Этап)",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                    Width=120,                    
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCTION_MACHINE_ID",
                    AutoloadItems=false,
                    Description="Станок",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="SelectBox",
                    Width= 450,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    OnCreate = (FormHelperField field) =>
                    {
                        var c=(SelectBox)field.Control;
                        
                        if (StatusTask == 4 || StatusTask == 5)
                        {
                            c.IsReadOnly = true;
                        }
                        
                        var columns = new List<DataGridHelperColumn>()
                        {
                            new DataGridHelperColumn
                            {
                                Header="Станок",
                                Path="PRODUCTION_MACHINE_NAME",
                                Description="",
                                ColumnType=ColumnTypeRef.String,
                                Width2=16,
                            },
                            new DataGridHelperColumn
                            {
                                Header="Основная схема",
                                Path="PRODUCTION_STAGE_PRIMARY_FLAG",
                                Description="",
                                ColumnType=ColumnTypeRef.Boolean,
                                Width2=6,
                            },
                            new DataGridHelperColumn
                            {
                                Header="ИД",
                                Path="PRODUCTION_MACHINE_ID",
                                Description="",
                                ColumnType=ColumnTypeRef.Integer,
                                Width2=8,
                            },
                        };
                        c.GridColumns=columns;
                        c.SelectedItemValue="PRODUCTION_MACHINE_NAME";

                        c.ListBoxMinWidth = 350;
                        c.ListBoxMinHeight = 200;
                        c.Style = FindResource("CustomFormField");
                        c.DataType = SelectBox.DataTypeRef.Grid;
                        c.GridPrimaryKey=field.Path;
                    },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "MoldedContainer",
                        Object = "ProductionStage",
                        Action = "ListForProductionTask",
                        AnswerSectionKey="ITEMS",
                        BeforeRequest = (RequestData rd) =>
                        {
                            var productionSchemeId=FieldGetValueActual("PRODUCTION_SCHEME_ID").ToInt();
                            var productionStageId=FieldGetValueActual("PRODUCTION_STAGE_ID").ToInt();
                            {
                                rd.Params = new Dictionary<string, string>()
                                {
                                    { "PRODUCTION_SCHEME_ID", productionSchemeId.ToString() },
                                    { "PRODUCTION_STAGE_ID", productionStageId.ToString() },
                                };
                            }
                        },
                        OnComplete = (FormHelperField field, ListDataSet ds) =>
                        {
                            var c=(SelectBox)field.Control;
                            c.GridDataSet = ds;

                            //если есть строка с флагом, она будет выбрана по умолчанию
                            {
                                foreach(Dictionary<string,string> row in ds.Items)
                                {
                                    if(row.CheckGet("PRODUCTION_STAGE_PRIMARY_FLAG").ToBool())
                                    {
                                        var productionMachineId=row.CheckGet("PRODUCTION_MACHINE_ID").ToInt();
                                        Values.CheckAdd("PRODUCTION_MACHINE_ID",productionMachineId.ToString());
                                        FieldSetValueActual("PRODUCTION_MACHINE_ID");

                                        break;
                                    }
                                }
                            }
                        },
                    },
                },
                new FormHelperField()
                {
                    Path="TASK_QUANTITY",
                    Description="Количество",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Width=60,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    Params=new Dictionary<string, string>
                    {
                        {"MaxLength","7" },
                    },
                    OnCreate = (FormHelperField field) =>
                    {
                        var c = (TextBox)field.Control;
                        c.IsReadOnly = StatusTask == 5;
                    }
                },
                new FormHelperField()
                {
                    Path="NOTE",
                    Description="Примечание",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Width=450,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Params=new Dictionary<string, string>
                    {
                        {"MaxLength","256" },
                    },
                    OnCreate = (FormHelperField field) =>
                    {
                        var c = (TextBox)field.Control;
                        if (StatusTask == 4 || StatusTask == 5)
                        {
                            c.IsReadOnly = true;
                        }
                    }
                },
            };

            if (!string.IsNullOrEmpty(OrderReceiptData))
            {
                ProductionTaskNote += $"Этикетка приедет {OrderReceiptData}";
            }

            if(CreateMode==1 || CreateMode==2)
            {
                OnGet += (FormDialog fd) =>
                {
                    var result = CreateFromOrder();
                    return result;
                };
            }

            QueryGet = new RequestData()
            {
                Module = "MoldedContainer",
                Object = "ProductionTask",
                Action = "Get",
            };

            AfterGet += (FormDialog fd) =>
            {
                var s = fd.Values.CheckGet("TASK_ID").ToInt().ToString();
                switch(fd.Mode)
                {
                    case "create":
                        fd.FrameTitle = $"Новое ПЗЛТ";
                        
                        if (!string.IsNullOrEmpty(ProductionTaskNote))
                        {
                            fd.Values["NOTE"] = ProductionTaskNote;
                        }
                        break;

                    case "edit":
                        fd.FrameTitle = $"ПЗЛТ <{s}>";
                        var existingNote = fd.Values.CheckGet("NOTE");
                        if (!string.IsNullOrEmpty(ProductionTaskNote))
                        {
                            if (string.IsNullOrEmpty(existingNote))
                            {
                                fd.Values["NOTE"] = ProductionTaskNote;
                            }
                            else
                            {
                                fd.Values["NOTE"] = $"{existingNote}\n{ProductionTaskNote}";
                            }
                        }
                        break;

                    default:
                        fd.FrameTitle = $"ПЗЛТ <{s}>";
                        break;
                }
                fd.Open();
            };

            QuerySave =new RequestData()
            {
                Module="MoldedContainer",
                Object="ProductionTask",
                Action="Save",
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                    {
                        { "ID", PrimaryKeyValue },
                    };
                }
            };

            AfterUpdate+=(FormDialog fd)=>
            {
                fd.Hide();
                
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "",
                    ReceiverName = "ProductionTaskTab",
                    SenderName = ControlName,
                    Action = "order_refresh",
                    Message = $"{fd.InsertId}",
                });

                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "",
                    ReceiverName = "ProductionTaskTab",
                    SenderName = ControlName,
                    Action = "production_task_refresh",
                    Message = $"{fd.InsertId}",
                });
            };

            BeforeSave += ProductionTaskForm_BeforeSave;

            PrimaryKey= "TASK_ID";
            PrimaryKeyValue=id;
            Commander.Init(this);
            Run(mode);
        }

        private bool ProductionTaskForm_BeforeSave(Dictionary<string, string> parameters)
        {
           // получаем значение всех полей
            var a = GetValues();
           // var id = a.CheckGet("TASK_QUANTITY").ToInt();
      
            foreach (var item in Values)
            {
                parameters.CheckAdd(item.Key, item.Value);
            }
            
            parameters.CheckAdd("STATUS_TASK", StatusTask.ToString());
            
            return true;
        }

        public bool CreateFromOrder()
        {
            bool result = false;
            bool resume = true;
            var row = new Dictionary<string, string>();

            if(resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ORDER_ID", OrderId.ToString());

                    ///
                    p.CheckAdd("GOODS_ID", GoodsId.ToString());
                   if (IdorderDates !=0)
                       p.CheckAdd("MODE", "2");
                   else
                       p.CheckAdd("MODE", "1");
                    p.CheckAdd("ORDER_POSITION_ID", IdorderDates.ToString());
                    
                    ///

                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "MoldedContainer");
                q.Request.SetParam("Object", "Order");
                q.Request.SetParam("Action", "Get");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestTimeoutMin;
                q.Request.Attempts = 1;

                q.DoQuery();

                if(q.Answer.Status == 0)
                {
                    var answerData = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if(answerData != null)
                    {
                        var ds = ListDataSet.Create(answerData, "ITEMS");
                        row = ds.GetItemByKeyValue("ORDER_POSITION_ID", $"{IdorderDates}.0");
                    }
                }
                else
                {
                    resume = false;
                }
            }

            if(resume)
            {
                var v=new Dictionary<string, string>();
                if (CreateMode != 2)
                {
                    v.CheckAdd("ORDER_POSITION_ID", row.CheckGet("ORDER_POSITION_ID"));
                }
                // v.CheckAdd("ORDER_POSITION_ID", IdorderDates.ToString());
                v.CheckAdd("GOODS_ID", row.CheckGet("GOODS_ID"));
                //v.CheckAdd("TASK_QUANTITY", row.CheckGet("ORDER_QUANTITY"));
                v.CheckAdd("TASK_QUANTITY", TaskQuantity.ToInt().ToString());

                SetValues(v);
                result = true;
            }

            return result;
        }
    }
}
