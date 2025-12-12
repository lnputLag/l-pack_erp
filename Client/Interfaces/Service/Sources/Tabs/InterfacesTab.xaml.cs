using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.X509.Qualified;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Service.Sources
{
    /// <summary>
    /// интерфейсы
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2025-02-12</released>
    /// <changed>2025-02-12</changed>
    public partial class InterfacesTab : ControlBase
    {
        public InterfacesTab()
        {           

            InitializeComponent();

            ControlSection = "interfaces";
            RoleName = "[erp]server";
            ControlTitle ="Интерфейсы";
            DocumentationUrl = "/doc/l-pack-erp-new/service_new/sources";

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
                InterfaceGridInit();
            };

            OnUnload=()=>
            {
                InterfaceGrid.Destruct();
            };

            OnFocusGot=()=>
            {
               
            };

            OnFocusLost=()=>
            {
                
            };

            OnNavigate = () =>
            {
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
                    Commander.Add(new CommandItem()
                    {
                        Name = "detach",
                        Enabled = true,
                        Title = "Открепить",
                        Description = "",
                        ButtonUse = true,
                        ButtonName = "DetachButton",
                        Action = () =>
                        {
                            var p = new Dictionary<string, string>();
                            p.CheckAdd("width", "1024");
                            p.CheckAdd("height", "800");
                            p.CheckAdd("no_modal", "1");
                            Central.WM.FrameMode = 2;
                            Central.WM.Show("tools", $"Инструменты", true, "main", this, "top", p);
                        },
                    });
                }


              

                Commander.SetCurrentGridName("InterfaceGrid");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "interface_refresh",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "InterfaceGridRefreshButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            InterfaceGrid.LoadItems();
                        },
                    });

                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "interface_send_command",
                            Title = "Анализ",
                            Description = "",
                            ButtonUse = true,
                            ButtonName = "InterfaceCommandButton",
                            MenuUse = true,
                            Action = () =>
                            {
                                ProcessChecks(InterfaceGrid.SelectedItem);                                
                            },
                            CheckEnabled = () =>
                            {                              
                                var result = true;
                                return result;
                            },
                            AccessLevel = Role.AccessMode.FullAccess,
                        });                       
                    }
                }

                Commander.Init(this);
            }
        }
       
        public FormHelper Form { get; set; }

        public void FormInit()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
            };
            Form.SetFields(fields);
        }

        public void SetDefaults()
        {
            Form.SetDefaults();
        }

        public void InterfaceGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="CODE",
                    Path="CODE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="PARENT",
                    Path="PARENT_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="NAME",
                    Path="NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=24,
                },
                new DataGridHelperColumn
                {
                    Header="TYPE",
                    Path="TYPE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=45,
                },
                new DataGridHelperColumn
                {
                    Header="CHECKED",
                    Path="CHECKED",
                    ColumnType=ColumnTypeRef.Boolean,
                },
                new DataGridHelperColumn
                {
                    Header="RESULT",
                    Path="RESULT",
                    ColumnType=ColumnTypeRef.Boolean,
                },
                new DataGridHelperColumn
                {
                    Header="REPORT",
                    Path="REPORT",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },

            };
            
            InterfaceGrid.SetColumns(columns);
            InterfaceGrid.SetPrimaryKey("CODE");
            InterfaceGrid.SetSorting("CODE", ListSortDirection.Ascending);
            InterfaceGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            InterfaceGrid.SearchText = InterfaceGridSearch;
            InterfaceGrid.Toolbar = InterfaceGridToolbar;
            InterfaceGrid.AutoUpdateInterval = 0;
            InterfaceGrid.UseProgressBar = true;
            InterfaceGrid.UseProgressSplashAuto = true;
            InterfaceGrid.OnLoadItems = () => {
                LoadReports();
            };
      
            InterfaceGrid.OnSelectItem = (item) => {
            };
            InterfaceGrid.Commands = Commander;
            InterfaceGrid.Init();            
        }

        private Dictionary<string, string> InterfaceReport {  get; set; }= new Dictionary<string, string>();
        private void ProcessChecks(Dictionary<string, string> row)
        {
            var n = row.CheckGet("NAME");
            if(!n.IsNullOrEmpty())
            {
                if(Central.WM.TabItems.ContainsKey(n))
                {
                    var h = Central.WM.TabItems[n];
                    var s = "";
                    var hc = h.Content;

                    bool result = true;
                    int typeId = 0;
                    var type = hc.GetType();

                    {
                        try
                        {
                            if(type.GetMethod("GetControlBaseVersion") != null)
                            {
                                typeId = 3;
                            }
                        }
                        catch(Exception e)
                        {

                        }
                    }

                    s = s.Append($"name=[{n}] type=[{type}] typeId=[{typeId}]", true);
                    InterfaceReport.CheckAdd("NAME", row.CheckGet("NAME"));
                    InterfaceReport.CheckAdd("CODE", row.CheckGet("CODE"));
                    InterfaceReport.CheckAdd("TYPE_ID", typeId.ToString());
                    InterfaceReport.CheckAdd("TYPE", type.ToString());
                    

                    if(typeId == 0)
                    {
                        try
                        {
                            var d = false;
                            if(type.GetMethod("Destroy") != null)
                            {
                                d = true;
                            }
                            s = s.Append($"Destroy=[{d}]", true, 1);

                            if(d==false)
                            {
                                result = false;
                            }
                        }
                        catch(Exception e)
                        {
                        }
                    }

                    if(typeId == 3)
                    {
                        try
                        {
                            var hb = (ControlBase)hc;
                            var d = false;
                            if(hb.OnUnload != null)
                            {
                                d = true;
                            }
                            s = s.Append($"OnUnload=[{d}]", true, 1);

                            if(d == false)
                            {
                                result = false;
                            }
                        }
                        catch(Exception e)
                        {
                        }
                    }

                    InterfaceReport.CheckAdd("RESULT", result.ToString().ToInt().ToString());
                    InterfaceReport.CheckAdd("REPORT", s.ToString());
                    InterfaceReport.CheckAdd("ON_UPDATE", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));

                    var rr = 0;
                    if(!s.IsNullOrEmpty())
                    {
                        var reportViewer = new ReportViewer();
                        reportViewer.Content = s;
                        reportViewer.Init();
                        reportViewer.Show();
                    }

                    SaveReport();
                }
            }
        }

        public void SaveReport()
        {
            var jsonString = JsonConvert.SerializeObject(InterfaceReport);

            var p = new Dictionary<string, string>();
            p.Add("ITEMS", jsonString);
            // 1=global,2=local,3=net
            p.Add("STORAGE_TYPE", "3");
            p.Add("TABLE_NAME", "client_interface_stat");
            p.Add("PRIMARY_KEY", "CODE");
            p.Add("PRIMARY_KEY_VALUE", $"{InterfaceReport.CheckGet("CODE")}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Service");
            q.Request.SetParam("Object", "LiteBase");
            q.Request.SetParam("Action", "SaveData");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if(q.Answer.Status == 0)
            {
            }
            else
            {
                q.ProcessError();
            }
        }

        private ListDataSet ReportDS {  get; set; }
        private void LoadReports()
        {
            var jsonString = JsonConvert.SerializeObject(InterfaceReport);

            var p = new Dictionary<string, string>();
            // 1=global,2=local,3=net
            p.Add("STORAGE_TYPE", "3");
            p.Add("TABLE_NAME", "client_interface_stat");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Service");
            q.Request.SetParam("Object", "LiteBase");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if(q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if(result != null)
                {
                    ReportDS = ListDataSet.Create(result, "client_interface_stat");
                    var reports = ReportDS.Items;

                    var list = new List<Dictionary<string, string>>();
                    foreach(var item in Central.WM.TabItems)
                    {
                        var row = new Dictionary<string, string>();
                        var k = item.Key.ToUpper();
                        row.CheckAdd("CODE", k);
                        var n = item.Value.Name;
                        row.CheckAdd("NAME", n);
                        var h = item.Value;
                        row.CheckAdd("TYPE", h.Content.GetType().ToString());
                        var r = Central.WM.GetParentTabName(n);
                        row.CheckAdd("PARENT_NAME", r);
                        row.CheckAdd("CHECKED", "0");
                        row.CheckAdd("RESULT", "0");
                        row.CheckAdd("REPORT", "");

                        foreach(var row2 in reports)
                        {
                            if(row2.CheckGet("CODE")==k)
                            {
                                row.CheckAdd("CHECKED", "1");
                                row.CheckAdd("RESULT", row2.CheckGet("RESULT"));
                                row.CheckAdd("REPORT", row2.CheckGet("REPORT"));
                            }
                        }

                        list.Add(row);
                    }

                    var ds = ListDataSet.Create(list);
                    InterfaceGrid.UpdateItems(ds);

                }
            }
            else
            {
                q.ProcessError();
            }
        }
    }
}
