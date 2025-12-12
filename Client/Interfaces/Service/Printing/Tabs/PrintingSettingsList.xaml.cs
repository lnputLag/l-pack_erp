using Client.Common;
using Client.Interfaces.Main;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Common.LPackClientAnswer;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Service.Printing
{
    /// <summary>
    /// интерфейс настройки принтеров, список принтеров
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-09-22</released>
    /// <changed>2023-09-22</changed>
    public partial class PrintingSettingsList:ControlBase
    {
        public PrintingSettingsList()
        {
            ControlTitle="Профили печати";
            RoleName = "[erp]printing";

            OnMessage =(ItemMessage m)=>
            {
                if(m.ReceiverName == ControlName)
                {
                    ProcessCommand(m.Action,m);
                }
            };      
            
            OnKeyPressed=(KeyEventArgs e)=>
            {
                if(!e.Handled)
                {
                    switch(e.Key)
                    {
                        case Key.F1:
                            ProcessCommand("help");
                            e.Handled=true;
                            break;

                        case Key.Enter:
                            ProcessCommand("view");
                            e.Handled=true;
                            break;

                        case Key.Insert:
                            ProcessCommand("create");
                            e.Handled=true;
                            break;

                        case Key.Delete:
                            ProcessCommand("delete");
                            e.Handled=true;
                            break;
                    }
                }

                if(!e.Handled)
                {
                    Grid.ProcessKeyboardEvents(e);      
                }
            };

            OnLoad=()=>
            {
                InitializeComponent();

                GridInit();
                SetDefaults();

                ProcessPermissions();
            };

            OnUnload=()=>
            {
                Grid.Destruct();
            };

            OnFocusGot=()=>
            {
                Grid.ItemsAutoUpdate=true;
                Grid.Run();
            };

            OnFocusLost=()=>
            {
                Grid.ItemsAutoUpdate=false;
            };

            InnerLog="";
            GetPdfTimeout=10000;
            GetPdfAttempts=3;
            ReportWorking=new Dictionary<string, string>();
        }

        public string InnerLog {get;set;}
        public int GetPdfTimeout {get;set;}
        public int GetPdfAttempts {get;set;}
        public Dictionary<string,string> ReportWorking {get;set;}

        public void GridInit()
        {
            {
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        Doc="",
                        ColumnType=ColumnTypeRef.Integer,                        
                    },
                    new DataGridHelperColumn
                    {
                        Header="Имя",
                        Path="NAME",
                        Doc="Имя профиля",
                        ColumnType=ColumnTypeRef.String,
                        Width2=25,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Описание",
                        Path="DESCRIPTION",
                        ColumnType=ColumnTypeRef.String,
                        Width2=25,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Принтер",
                        Path="PRINTER_NAME",
                        Doc="Имя принтера или сетевой путь",
                        ColumnType=ColumnTypeRef.String,
                        Width2=25,
                    },
                   
                };
                Grid.SetColumns(columns);
                Grid.SetPrimaryKey("NAME");
                Grid.SetSorting("NAME", ListSortDirection.Ascending);
                Grid.ColumnWidthMode= GridBox.ColumnWidthModeRef.Full;
                Grid.AutoUpdateInterval=0;
                Grid.OnLoadItems = ()=>
                {
                    bool resume = true;
                    if (resume)
                    {
                        var ds=LoadItems();
                        Grid.UpdateItems(ds);
                    }
                };
                Grid.OnSelectItem = selectedItem =>
                {
                    
                };
                Grid.Init();
                Grid.Run();
            }
        }

        public void SetDefaults()
        {
        }

        public void ProcessCommand(string command, ItemMessage m=null)
        {
            command=command.ClearCommand();
            if(!command.IsNullOrEmpty())
            {
                switch(command)
                {
                    case "refresh":
                        {
                            Grid.LoadItems();
                            if (m != null)
                            {
                                var id = m.Message.ToString();
                                if (!id.IsNullOrEmpty())
                                {
                                    Grid.SelectRowByKey(id);
                                }
                            }

                        }
                        break;

                    case "cancel":
                        {
                            Close();
                        }
                        break;

                    case "create":
                        {
                            ProcessItem("create");
                        }
                        break;

                    case "edit":
                        {
                            var id = Grid.SelectedItem.CheckGet("NAME").ToString();
                            if (!id.IsNullOrEmpty())
                            {
                                ProcessItem("edit", id);
                            }
                        }
                        break;

                    case "delete":
                        {
                            var id = Grid.SelectedItem.CheckGet("NAME").ToString();
                            if (!id.IsNullOrEmpty())
                            {
                                ProcessItem("delete", id);
                            }
                        }
                        break;

                    case "test":
                        {
                            var id = Grid.SelectedItem.CheckGet("NAME").ToString();
                            if (!id.IsNullOrEmpty())
                            {
                                ProcessTest("", id);
                            }
                        }
                        break;
                }
            }
        }

        private void Close()
        {
            Central.WM.Close(ControlName);
            Central.WM.RemoveTab("printing");
        }

        private ListDataSet LoadItems()
        {
            var result=new ListDataSet();
            var list=Central.AppSettings.SectionGet("PRINTING_SETTINGS");
            result=ListDataSet.Create(list);
            return result;
        }

        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            var b=(Button)sender;
            if(b != null)
            {
                var buttonTagList = UIUtil.GetTagList(b);
                foreach (var t in buttonTagList)
                {
                    ProcessCommand(t);
                }
            }
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel(this.RoleName);
            var userAccessMode = mode;
            switch (mode)
            {
                case Role.AccessMode.Special:
                    break;

                case Role.AccessMode.FullAccess:
                    break;

                case Role.AccessMode.ReadOnly:
                default:
                    break;
            }

            //UIUtil.SetFrameworkElementEnabledByTagAccessMode(this.Content as DependencyObject, Acl.AccessMode.ReadOnly);

            List<Button> buttons = UIUtil.GetVisualChilds<Button>(this.Content as DependencyObject);
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    var buttonTagList = UIUtil.GetTagList(button);
                    var accessMode = Acl.FindTagAccessMode(buttonTagList);
                    if (accessMode > userAccessMode)
                    {
                        button.IsEnabled = false;
                    }
                }
            }
        }

        public void ProcessItem(string mode="create", string id="")
        {
            var editPrintingProfile = new FormDialog()
            {
                RoleName = "[erp]printing",
                FrameName = "PrintingProfile",
                Title = $"Профиль: ",
                Fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="NAME",
                        Description = "Имя:",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                    new FormHelperField()
                    {
                        Path="DESCRIPTION",
                        Description = "Описание:",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="PRINTER_NAME",
                        Description = "Принтер:",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                        Fillers=new List<FormHelperFiller>{
                            { 
                                new FormHelperFiller(){
                                    Name="SelectPrinter",
                                    Description="Выбрать принтер",
                                    Caption="Выбрать",
                                    //IconStyle="SelectImage",
                                    Action=(FormHelper form)=>
                                    {
                                        var result="";
                                        {
                                            var printHelper=new PrintHelper();
                                            printHelper.Init();
                                            var p=printHelper.GetPrintingSettingsFromSystem();
                                            result=p.PrinterFullName;
                                        }
                                        return result;
                                    }
                                }
                            }
                        },
                    },
                    new FormHelperField()
                    {
                        Path="COPIES",
                        Description = "Число копий:",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Default="1",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                    new FormHelperField()
                    {
                        Path="WIDTH",
                        Description = "Ширина печати:",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Default="210",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                    new FormHelperField()
                    {
                        Path="HEIGHT",
                        Description = "Высота печати:",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Default="297",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                    new FormHelperField()
                    {
                        Path="DUPLEX",
                        Description = "Двусторонняя печать",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Default="3",                      
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                              { FormHelperField.FieldFilterRef.Required, null },
                        },
                        Comments = new List<FormHelperComment>
                        {
                            new FormHelperComment()
                            {
                                Name = "DUPLEX_LABEL",
                                Content = "1 -- Односторонняя печать, 2 -- Двусторонняя печать, 3 -- По умолчанию.",
                            }
                        },
                    },
                    new FormHelperField()
                    {
                        Path="LANDSCAPE",
                        Description = "Альбомная ориентация",
                        FieldType=FormHelperField.FieldTypeRef.Boolean,
                        ControlType = "CheckBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                },
            };
            editPrintingProfile.OnGet+=(FormDialog fd)=>
            {
                var result=false;
                if(id.IsNullOrEmpty())
                {
                    //создание
                    fd.ClearValues();
                    result=true;
                }
                else
                {
                    //изменение
                    var p=Central.AppSettings.SectionFindRow("PRINTING_SETTINGS","NAME", id);
                    fd.SetValues(p);
                    result=true;
                }
                return result;
            };
            editPrintingProfile.AfterGet+=(FormDialog fd)=>
            {
                fd.Open();
            };
            editPrintingProfile.OnSave+=(FormDialog fd)=>
            {
                var result=false;
                var validationResult=fd.Validate();
                if(validationResult)
                {
                    var p=fd.GetValues();
                    Central.AppSettings.SectionAddRow("PRINTING_SETTINGS",p);
                    Central.AppSettings.Store();
                    fd.InsertId=p.CheckGet(fd.PrimaryKey);
                    result=true;
                }
                return result;
            };
            editPrintingProfile.BeforeDelete+=(Dictionary<string, string> p)=>
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
            editPrintingProfile.OnDelete+=(FormDialog fd)=>
            {
                var result=false;
                {
                    Central.AppSettings.SectionDeleteRow("PRINTING_SETTINGS","NAME", id);
                    Central.AppSettings.Store();
                    result=true;
                }
                return result;
            };
            editPrintingProfile.AfterUpdate+=(FormDialog fd)=>
            {
                fd.Hide();
                
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "Printing",
                    ReceiverName = "PrintingSettingsList",
                    SenderName = ControlName,
                    Action = "Refresh",
                    Message = $"{fd.InsertId}",
                });
            };
            editPrintingProfile.PrimaryKey="NAME";
            editPrintingProfile.PrimaryKeyValue=id;
            editPrintingProfile.Commander.Init(editPrintingProfile);
            editPrintingProfile.Run(mode);
        }

        private void ProcessTest(string mode="", string id="")
        {
            var editPrintingProfile = new FormDialog()
            {
                FrameName = "PrintingTest",
                Title = $"Проверка: ",
                Fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="NAME",
                        Description = "Профиль печати:",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                     new FormHelperField()
                    {
                        Path="_NAME",
                        Description = "Ярлык готовой продукции",
                        ControlType="asdf",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="PALLET_ID",
                        Description = "ИД поддона:",
                        Default="12100919",
                        FieldType=FormHelperField.FieldTypeRef.String,                        
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                        Fillers=new List<FormHelperFiller>{
                            { 
                                new FormHelperFiller(){
                                    Name="CorrugatorLabelGoodsPreview",
                                    Caption="Просмотр",
                                    Action=(FormHelper form)=>
                                    {
                                        CorrugatorLabelGoodsTestStart(form, 2);

                                        var result=form.GetValueByPath("PALLET_ID");
                                        return result;
                                    }
                                }
                            },
                            { 
                                new FormHelperFiller(){
                                    Name="CorrugatorLabelGoodsPrint",
                                    Caption="Печать",
                                    Action=(FormHelper form)=>
                                    {
                                        CorrugatorLabelGoodsTestStart(form, 1);

                                        var result=form.GetValueByPath("PALLET_ID");
                                        return result;
                                    }
                                }
                            },
                        },
                    },
                },
            };
            editPrintingProfile.OnGet+=(FormDialog fd)=>
            {
                var result=false;
                {
                    //изменение
                    var p=Central.AppSettings.SectionFindRow("PRINTING_SETTINGS","NAME", id);
                    fd.SetValues(p);
                    result=true;
                }
                return result;
            };
            editPrintingProfile.AfterGet+=(FormDialog fd)=>
            {
                fd.Open();
            };
            editPrintingProfile.PrimaryKey="NAME";
            editPrintingProfile.PrimaryKeyValue=id;
            editPrintingProfile.UseSave=false;
            editPrintingProfile.Run("edit");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="form"></param>
        /// <param name="mode">1=print,2=preview</param>
        public void CorrugatorLabelGoodsTestStart(FormHelper form, int mode=1)
        {
            var v=form.GetValues();

            var p=new Dictionary<string,string>();          
            
            if(mode==2)
            {
                p.CheckAdd("preview","1");
            }
            
            p.CheckAdd("pallet_id",v.CheckGet("PALLET_ID"));
            p.CheckAdd("printing_profile_name",v.CheckGet("NAME"));
            p.CheckAdd("copy","1");
            p.CheckAdd("debug","1");

            CorrugatorLabelGoodsTest(p);
        }

        public bool CorrugatorLabelGoodsTest(Dictionary<string,string> v)
        {
            var result=false;
            var resume=true;
            var fileName="";
            //1=print,2=preview
            var mode=1;
            var error="";
            var debug=false;

            InnerLog="";
            DebugLogClear();

            {
                var m=v.CheckGet("preview").ToInt();
                if(m == 1)
                {
                    mode=2;
                }
            }

            {
                var d=v.CheckGet("debug").ToInt();
                if(d == 1)
                {
                    debug=true;
                }
            }

            DebugLog($"PrintLabel mode=[{mode}]", "debug", 2);
            DebugLog($"{v.GetDumpString()}", "debug", 3);

            if(resume)
            {
                var complete=false;

                var p = new Dictionary<string, string>();
                p.Add("PRODUCTION_TASK_ID", v.CheckGet("production_task_id"));
                p.Add("PALLET_NUMBER",  v.CheckGet("pallet_number"));
                p.Add("PALLET_ID", v.CheckGet("pallet_id"));
                p.Add("FORMAT", "pdf");
                p.Add("MODE", "1");
                if(debug)
                {
                    p.Add("DEBUG", "1");
                }
                
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "Label");
                q.Request.SetParam("Action", "Make");
                q.Request.SetParams(p);

                q.Request.Timeout=GetPdfTimeout;
                q.Request.Attempts=GetPdfAttempts;

                q.DoQuery();
                DebugLog(q.InnerLog, "debug", 3);

                if (q.Answer.Status == 0)
                {
                    if(q.Answer.Type == AnswerTypeRef.File)
                    {
                        fileName=q.Answer.DownloadFilePath;
                        complete=true;
                    }
                    if(q.Answer.Type == AnswerTypeRef.Data)
                    {
                        error=error.Append($"{q.Answer.Data.ToString()}");
                    }
                }
                else
                {
                    error=error.Append($"{q.Answer.Error.Message} {q.Answer.Error.Description}");
                }

                if(complete)
                {
                    DebugLog($"complete file=[{fileName}]", "debug", 2);
                }
                else
                {
                    resume=false;
                    DebugLog($"get pdf error [{error}]", "debug", 2);
                }
                ReportAppend("LABEL_FILE_REQUEST_COMPLETE",complete.ToString().ToInt().ToString());
                ReportAppend("LABEL_FILE_REQUEST_ERROR",error);
                ReportAppend("LABEL_FILE_REQUEST_LOG",q.InnerLog);
                ReportAppend("LABEL_FILE_REQUEST_OID",q.Answer.ObjectId);
            }

            if(resume)
            {
                //1=печать
                if(mode == 1)
                {
                    var printingProfile=v.CheckGet("printing_profile_name");
                    var printHelper=new PrintHelper();
                    printHelper.PrintingProfile=printingProfile;
                    printHelper.PrintingCopies=v.CheckGet("copy").ToInt();
                    printHelper.Init();
                    var printingResult = printHelper.StartPrinting(fileName);

                    {
                        DebugLog($"printer settings", "debug", 2);
                        var s = printHelper.DebugGetSettings(fileName).GetDumpString();
                        ReportAppend(printHelper.DebugGetSettings(fileName), "PRINTER");
                        DebugLog($"{s}", "debug", 3);
                    }

                    if (!printingResult)
                    {
                        resume = false;
                        DebugLog($"printing error {printHelper.ErrorLog}", "debug", 4);
                    }
                    ReportAppend("PRINTING_COMPLETE", printingResult.ToString().ToInt().ToString());
                    ReportAppend("PRINTING_ERROR", printHelper.ErrorLog);

                    printHelper.Dispose();

                    try
                    {
                        if (System.IO.File.Exists(fileName))
                        {
                            System.IO.File.Delete(fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }

                //2=просмотр
                if(mode == 2)
                {
                    var printingProfile=v.CheckGet("printing_profile_name");
                    var printHelper=new PrintHelper();
                    printHelper.PrintingProfile=printingProfile;
                    printHelper.PrintingCopies=v.CheckGet("copy").ToInt();
                    printHelper.Init();
                    var printingResult=printHelper.ShowPreview(fileName);

                    {
                        DebugLog($"printer settings", "debug", 2);
                        var s=printHelper.DebugGetSettings(fileName).GetDumpString();
                        ReportAppend(printHelper.DebugGetSettings(fileName),"PRINTER");
                        DebugLog($"{s}", "debug", 3);
                    }

                    if(!printingResult)
                    {
                        resume=false;
                        DebugLog($"preview error {printHelper.ErrorLog}", "debug", 4);
                    }          
                    ReportAppend("PRINTING_COMPLETE",printingResult.ToString().ToInt().ToString());
                    ReportAppend("PRINTING_ERROR",printHelper.ErrorLog);
                }
            }                    

            if(resume)
            {
                result=true;
            }
            else
            {
                result=false;
            }

            DebugLog($"result=[{result}]", "debug", 2);

            if(debug)
            {
                if(!result)
                {
                    var msg=InnerLog;
                    var d = new LogWindow($"{msg}", "Ошибка печати" );
                    d.ShowDialog();
                }
            }

            return result;
        }

        public void DebugLog(string message, string tag="debug", int offset=0)
        {
            var today=DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

            var s="";
            var placeholder="    ";
            if(offset>0)
            {
                for(int i=0; i<offset; i++)
                {
                    s=$"{s}{placeholder}";
                }
                message=$"{s}{message}";
            }

            InnerLog=InnerLog.Append($"{today} {message}", true);
        }

        public void DebugLogClear()
        {
            InnerLog="";
        }

        public void ReportAppend(Dictionary<string,string> row, string prefix="")
        {
            if(row.Count > 0)
            {
                if(!prefix.IsNullOrEmpty())
                {
                    prefix=$"{prefix}_";
                }

                foreach(KeyValuePair<string,string> item in row)
                {
                    var k=item.Key;
                    var v=item.Value;
                    
                    k=$"{prefix}{k}";
                    k=k.ToUpper();
                    ReportWorking.CheckAdd(k,v);
                }
            }
        }

        public void ReportAppend(string k, string v)
        {
            k=k.ToUpper();
            ReportWorking.CheckAdd(k,v);
        }
    }
}

