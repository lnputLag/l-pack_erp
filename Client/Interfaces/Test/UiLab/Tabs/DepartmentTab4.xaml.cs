using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using DevExpress.ClipboardSource.SpreadsheetML;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Accounts
{
    /// <summary>
    /// отделы
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-11-07</released>
    /// <changed>2023-11-07</changed>
    public partial class DepartmentTab4 : ControlBase
    {
        public DepartmentTab4()
        {
            DebugTimeout = 0;

            InitializeComponent();
            ControlTitle="Отделы";

            OnMessage=(ItemMessage m)=>
            {
                if(m.ReceiverName == ControlName)
                {
                    ProcessCommand(m.Action,m);
                }
            };           

            OnLoad=()=>
            {
                GridInit();
            };

            OnUnload=()=>
            {
                //Grid.Destruct();
            };

            OnFocusGot=()=>
            {
                //Grid.ItemsAutoUpdate=true;
                //Grid.Run();
            };

            OnFocusLost=()=>
            {
                //Grid.ItemsAutoUpdate=false;
            };
        }

        public FormHelper Form { get; set; }
        private int DebugTimeout { get; set; }


       

        public void GridInit()
        {
            
            {
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                        {
                            {
                                DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    if(row.CheckGet("ID").ToInt() ==1)
                                    {
                                        color = HColor.Orange;
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
                                    var color = "";

                                    if(row.CheckGet("ID").ToInt() ==7)
                                    {
                                        color = HColor.RedFG;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    /*
                    new DataGridHelperColumn
                    {
                        Header="S1",
                        Path="S1",
                        ColumnType=ColumnTypeRef.Boolean,
                    },
                    new DataGridHelperColumn
                    {
                        Header="S2",
                        Path="S2",
                        ColumnType=ColumnTypeRef.String,
                    },
                    */
                    new DataGridHelperColumn
                    {
                        Header="Название",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=30,
                        Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                        {
                            {
                                DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    if(row.CheckGet("NAME").ToString().IndexOf("БДМ") > -1)
                                    {
                                        color = HColor.BlueFG;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                        Labels=new List<DataGridHelperColumnLabel>()
                        {
                            new DataGridHelperColumnLabel()
                            {
                                Construct=()=>
                                {
                                    var block=DataGridHelperColumnLabel.MakeElement("CP");                                 
                                    return block;
                                },
                                Update = (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    {
                                        result=Visibility.Hidden;
                                        var r=Cryptor.MakeRandom();
                                        if(r > 555555)
                                        {
                                            result=Visibility.Visible;
                                        }
                                    }
                                    return result;
                                },
                            },
                            new DataGridHelperColumnLabel()
                            {
                                Construct=()=>
                                {
                                    var block=DataGridHelperColumnLabel.MakeElement("БДМ","#FFFFC182","#ff000000", 32, 16);
                                    return block;
                                },
                                Update = (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    {
                                        result=Visibility.Hidden;
                                        if(row.CheckGet("NAME").ToString().IndexOf("БДМ") > -1)
                                        {
                                            result=Visibility.Visible;
                                        }
                                    }
                                    return result;
                                },
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Сокращённое название",
                        Path="SHORT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=23,
                        Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                        {
                            {
                                DataGridHelperColumn.StylerTypeRef.FontWeight,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var fontWeight= new FontWeight();

                                    if(row.CheckGet("SHORT_NAME").ToString().IndexOf("БДМ") > -1)
                                    {
                                        fontWeight=FontWeights.Bold;
                                    }

                                    result=fontWeight;
                                    return result;
                                }
                            },
                            {
                                DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    if(row.CheckGet("SHORT_NAME").ToString().IndexOf("ПДС") > -1)
                                    {
                                        color = HColor.BlueFG;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },

                    },
                };
                Grid.SetColumns(columns);
                Grid.SetPrimaryKey("ID");
                //Grid.SetSorting("NAME", ListSortDirection.Ascending);
                Grid.ColumnWidthMode= GridBox.ColumnWidthModeRef.Full;
                Grid.SearchText = Search;
                Grid.Toolbar=GridToolbar;                
                Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "refresh",
                        new DataGridContextMenuItem()
                        {
                            Header="Обновить",
                            Action=()=>
                            {
                                DebugTimeout=0;
                                ProcessCommand("refresh");
                            },
                        }
                    },
                    { "s1", new DataGridContextMenuItem() {
                        Header="-",
                    }},
                    {
                        "create",
                        new DataGridContextMenuItem()
                        {
                            Header="Создать",
                            Action=()=>
                            {
                                ProcessCommand("create");
                            },
                        }
                    },
                    {
                        "edit",
                        new DataGridContextMenuItem()
                        {
                            Header="Изменить",
                            Action=()=>
                            {
                                ProcessCommand("edit");
                            },
                        }
                    },
                    { "s2", new DataGridContextMenuItem() {
                        Header="-",
                    }},
                    {
                        "export",
                        new DataGridContextMenuItem()
                        {
                            Header="Экспорт в Excel",
                            Action=()=>
                            {
                                ProcessCommand("export");
                            },
                        }
                    },
                    { "s3", new DataGridContextMenuItem() {
                        Header="-",
                    }},
                    {
                        "refresh2000",
                        new DataGridContextMenuItem()
                        {
                            Header="Обновить (2000 мс)",
                            Action=()=>
                            {
                                DebugTimeout=2000;
                                ProcessCommand("refresh");
                            },
                        }
                    },
                    {
                        "refresh7000",
                        new DataGridContextMenuItem()
                        {
                            Header="Обновить (7000 мс)",
                            Action=()=>
                            {
                                DebugTimeout=7000;
                                ProcessCommand("refresh");
                            },
                        }
                    },
                    {
                        "refresh10000",
                        new DataGridContextMenuItem()
                        {
                            Header="Обновить (10000 мс)",
                            Action=()=>
                            {
                                DebugTimeout=10000;
                                ProcessCommand("refresh");
                            },
                        }
                    },
                    {
                        "refresh20000",
                        new DataGridContextMenuItem()
                        {
                            Header="Обновить (20000 мс)",
                            Action=()=>
                            {
                                DebugTimeout=20000;
                                ProcessCommand("refresh");
                            },
                        }
                    },
                };
                Grid.OnSelectItem = selectedItem =>
                {
                    EditButton.IsEnabled = false;
                    if(Grid.SelectedItem.CheckGet("ID").ToInt() != 0)
                    {
                        EditButton.IsEnabled = true;
                    }
                };
                Grid.OnDblClick = selectedItem =>
                {
                    ProcessCommand("edit");
                };
                Grid.OnLoadItems = GridLoadItems;
                //Grid.QueryLoadItems=new FormDialog.RequestData()
                //{
                //    Module="Accounts",
                //    Object="Department",
                //    Action="List",
                //};

                Grid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                {
                    {
                        DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            if(row.CheckGet("ID").ToInt() < 10)
                            {
                                color = HColor.Red;
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
                            var color = "";

                            var id = row.CheckGet("ID").ToInt();
                            if (id == 42)
                            {
                                color = HColor.OliveFG;
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };
                //Grid.AutoUpdateInterval = 0;
                Grid.UseProgressSplashAuto = true;
                Grid.UseProgressBar = true;
                Grid.DebugName="department";
                Grid.Init();
            }
            

            {
                Form = new FormHelper();
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="SEARCH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=Search,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                };

                Form.SetFields(fields);
            }
        }

       

        public async void GridLoadItems()
        {
            bool resume = true;

            if(resume)
            {
                //Grid.SetBusy(true);

                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("_DELAY", DebugTimeout.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Accounts");
                q.Request.SetParam("Object", "Department");
                q.Request.SetParam("Action", "List");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                
                await Task.Run(() =>
                {
                    q.DoQuery();
                });
                

                //q.DoQuery();

                if(q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if(result != null)
                    {
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");

                            /*
                            foreach(var row in ds.Items)
                            {
                                row.CheckAdd("S1", "0");

                                if(
                                    row.CheckGet("ID").ToInt() == 1
                                    || row.CheckGet("ID").ToInt() == 15
                                )
                                {
                                    row.CheckAdd("S1", "1");
                                }

                                row.CheckAdd("S2", "#ffaaffaa");
                            }
                            */

                            Grid.UpdateItems(ds);
                        }
                    }
                }

                //Grid.SetBusy(false);
            }

        }

        public void SetDefaults()
        {
            Form.SetDefaults();
        }

        public void ProcessCommand(string command, ItemMessage m=null)
        {
            command=command.ClearCommand();
            if(!command.IsNullOrEmpty())
            {
                switch(command)
                {
                    case "create":
                    {
                        var h=new DepartmentForm();
                        h.Init("create");
                    }
                        break;

                    case "edit":
                    {
                        var id=Grid.SelectedItem.CheckGet("ID").ToString();
                        if(!id.IsNullOrEmpty())
                        {
                            var h=new DepartmentForm();
                            h.Init("edit", id);
                        }   
                    }
                        break;


                    case "clear":
                        {
                            Grid.ClearItems();
                        }
                        break;

                    case "refresh":
                    {
                        Grid.LoadItems();
                        if(m!=null)
                        {
                            var id=m.Message.ToString();
                            if(!id.IsNullOrEmpty())
                            {
                                //Grid.SelectRowByKey(id);
                            }
                        }
                    }
                        break;

                    case "export":
                    {
                        //Grid.ExportItemsExcel();
                    }
                        break;

                    case "help":
                    {
                        Central.ShowHelp("/doc/l-pack-erp/service/accounts/departments");
                    }
                        break;


                    case "on":
                        {
                            //Grid.ExportItemsExcel();
                            Grid.SetBusy(true);
                        }
                        break;


                    case "off":
                        {
                            //Grid.ExportItemsExcel();
                            Grid.SetBusy(false);
                        }
                        break;
                }
            }
        }

        private void ProcessItem(string mode="create", string id="")
        {
            var departmentForm = new FormDialog()
            {
                FrameName = "Department",
                Title = $"Отдел",
                Fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="ID",
                        Description="ИД",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        ControlType="void",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="NAME",
                        Description="Название",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        },                        
                    },
                    new FormHelperField()
                    {
                        Path="SHORT_NAME",
                        Description="Сокращенное название",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                },
            };
            //departmentForm.OnGet+=(FormDialog fd)=>
            //{
            //    var result=false;
            //    if(id.IsNullOrEmpty())
            //    {
            //        //создание
            //        fd.ClearValues();
            //        result=true;
            //    }
            //    else
            //    {
            //        //изменение
            //        var ds=new ListDataSet();
            //        {
            //            var p = new Dictionary<string, string>();
            //            {
            //                p.CheckAdd("ID", id.ToString());
            //            }

            //            var q = new LPackClientQuery();
            //            q.Request.SetParam("Module", "Accounts");
            //            q.Request.SetParam("Object", "Department");
            //            q.Request.SetParam("Action", "Get");
            //            q.Request.SetParams(p);

            //            q.DoQuery();

            //            if (q.Answer.Status == 0)
            //            {
            //                var answerData = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
            //                if (answerData != null)
            //                {
            //                    {
            //                        ds = ListDataSet.Create(answerData, "ITEMS");
            //                    }
            //                }
            //            }
            //            else
            //            {
            //                q.ProcessError();
            //            }
            //        }

            //        fd.SetValues(ds);

            //        result=true;
            //    }
            //    return result;
            //};
            departmentForm.QueryGet=new RequestData()
            {
                Module="Accounts",
                Object="Department",
                Action="Get",
            };
            departmentForm.AfterGet+=(FormDialog fd)=>
            {
                //var s=fd.Values.CheckGet("ID");
                var s=fd.Values.CheckGet("NAME");
                switch(fd.Mode)
                {
                    case "create":
                        fd.FrameTitle=$"Новый отдел";
                        break;

                    default:
                        fd.FrameTitle=$"Отдел <{s}>";
                        break;
                }
                fd.Open();
            };

            //departmentForm.OnSave+=(FormDialog fd)=>
            //{
            //    var result=false;
            //    var validationResult=fd.Validate();
            //    if(validationResult)
            //    {
            //        var p=fd.GetValues();

            //        var ds=new ListDataSet();
            //        {
            //            var q = new LPackClientQuery();
            //            q.Request.SetParam("Module", "Accounts");
            //            q.Request.SetParam("Object", "Department");
            //            q.Request.SetParam("Action", "Save");
            //            q.Request.SetParams(p);

            //            q.DoQuery();

            //            if (q.Answer.Status == 0)
            //            {
            //                var answerData = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
            //                if (answerData != null)
            //                {
            //                    {
            //                        ds = ListDataSet.Create(answerData, "ITEMS");
            //                    }
            //                }
            //            }
            //            else
            //            {
            //                q.ProcessError();
            //            }
            //        }

            //        fd.InsertId=ds.GetFirstItemValueByKey(fd.PrimaryKey);                    
            //        result=true;
            //    }
            //    return result;
            //};
            departmentForm.QuerySave=new RequestData()
            {
                Module="Accounts",
                Object="Department",
                Action="Save",
            };


            departmentForm.BeforeDelete+=(Dictionary<string, string> p)=>
            {
                bool resume=false;

                var msg = "";
                {
                    msg=msg.Append($"Удалить запись?");
                    msg=msg.Append($"{p.CheckGet("NAME")}",true);
                }

                var d = new DialogWindow($"{msg}", "Удаление", "", DialogWindowButtons.NoYes);
                if ((bool)d.ShowDialog())
                {
                    resume = true;
                }

                return resume;
            };
            departmentForm.OnDelete+=(FormDialog fd)=>
            {
                var result=false;
                {
                    result=true;
                }
                return result;
            };
            
            departmentForm.AfterUpdate+=(FormDialog fd)=>
            {
                fd.Hide();
                
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "",
                    ReceiverName = "DepartmentTab",
                    SenderName = ControlName,
                    Action = "Refresh",
                    Message = $"{fd.InsertId}",
                });
            };

            departmentForm.PrimaryKey="ID";
            departmentForm.PrimaryKeyValue=id;
            departmentForm.Run(mode);
        }

        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            var b=(System.Windows.Controls.Button)sender;
            if(b != null)
            {
                var t=b.Tag.ToString();
                ProcessCommand(t);
            }
        }
    }
   
}
