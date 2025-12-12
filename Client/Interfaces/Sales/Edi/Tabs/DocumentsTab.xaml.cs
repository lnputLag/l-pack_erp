using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Sales.Edi
{
    /// <summary>
    /// документы
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2024-09-30</released>
    /// <changed>2024-09-30</changed>
    public partial class DocumentsTab : ControlBase
    {
        public DocumentsTab()
        {
            InitializeComponent();

            ControlSection = "documents";
            RoleName = "[erp]edi";
            ControlTitle ="Документы";
            DocumentationUrl = "/doc/l-pack-erp";

            OnMessage = (ItemMessage m) =>
            {
                if(m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if(!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };

            OnLoad =()=>
            {
                FormInit();
                SetDefaults();
                DocumentGridInit();
            };

            OnUnload=()=>
            {
                DocumentGrid.Destruct();
            };

            OnFocusGot=()=>
            {
                DocumentGrid.ItemsAutoUpdate=true;
                DocumentGrid.Run();
            };

            OnFocusLost=()=>
            {
                DocumentGrid.ItemsAutoUpdate=false;
            };

            OnNavigate = () =>
            {
                //var departmentId = Parameters.CheckGet("department_id");
                //if(!departmentId.IsNullOrEmpty())
                //{
                //    OrderGridSearch.Text = departmentId;
                //    OrderGrid.UpdateItems();
                //}
            };

            {
                Commander.SetCurrentGroup("main");
                {                   
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

                Commander.SetCurrentGridName("DocumentGrid");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "document_refresh",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "DocumentGridRefreshButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            DocumentGrid.LoadItems();
                        },
                    });

                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "document_edit",
                            Title = "Открыть",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "DocumentGridEditButton",
                            HotKey = "Return|DoubleCLick",
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var k = DocumentGrid.GetPrimaryKey();
                                var row = DocumentGrid.SelectedItem;
                                var uid = row.CheckGet(k).ToString();
                                if(!uid.IsNullOrEmpty())
                                {
                                    DocumentView(uid);
                                }
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = DocumentGrid.GetPrimaryKey();
                                var row = DocumentGrid.SelectedItem;
                                if(!row.CheckGet(k).ToString().IsNullOrEmpty())
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });
                    }
                }

                Commander.Init(this);
            }

        }

        public FormHelper Form { get; set; }

        public void FormInit()
        {
            {
                Form = new FormHelper();
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="SEARCH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=DocumentGridSearch,                        
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path = "DATE_FROM",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = DateFrom,
                        ControlType = "TextBox",
                        Default=DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy"),
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "DATE_TO",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = DateTo,
                        ControlType = "TextBox",
                        Default=DateTime.Now.ToString("dd.MM.yyyy"),
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                };

                Form.SetFields(fields);
            }
        }

        public void SetDefaults()
        {
            Form.SetDefaults();
        }

        public void DocumentGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="ID",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="DOCUMENT_UID",
                    Path="DOCUMENT_UID",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="ON_DATE",
                    Path="ON_DATE",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="BUYER_ID",
                    Path="BUYER_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="SELLER_ID",
                    Path="SELLER_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="DESCRIPTION",
                    Path="DESCRIPTION",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="DOCUMENT_TYPE",
                    Path="DOCUMENT_TYPE",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="FILE_NAME",
                    Path="FILE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="MODE",
                    Path="MODE",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
            };

            DocumentGrid.SetColumns(columns);
            DocumentGrid.SetPrimaryKey("DOCUMENT_UID");
            DocumentGrid.SetSorting("ON_DATE", ListSortDirection.Ascending);
            DocumentGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            DocumentGrid.AutoUpdateInterval = 0;
            DocumentGrid.SearchText = DocumentGridSearch;
            DocumentGrid.Toolbar = DocumentGridToolbar;
            DocumentGrid.QueryLoadItems = new RequestData()
            {
                Module = "Sales",
                Object = "EdiDocument",
                Action = "List",
                AnswerSectionKey = "ITEMS",
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                    {
                        //{ "ORDER_ID", Id.ToString() },
                    };
                },
            };
            DocumentGrid.Commands = Commander;
            DocumentGrid.Init();
        }


        private async void DocumentView(string uid)
        {
            var resume = true;
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("DOCUMENT_UID", uid.ToString());
            }

            if(resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "EdiDocument");
                q.Request.SetParam("Action", "Download");

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if(q.Answer.Status == 0)
                {
                    if(q.Answer.Type == LPackClientAnswer.AnswerTypeRef.File)
                    {
                        Central.OpenFile(q.Answer.DownloadFilePath);
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

        }
    }
}
