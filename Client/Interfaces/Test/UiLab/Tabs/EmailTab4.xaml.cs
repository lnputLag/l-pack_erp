using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Accounts
{
    /// <summary>
    /// сообщения электронной почты
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-09-29</released>
    /// <changed>2023-09-29</changed>
    public partial class EmailTab4:ControlBase
    {
        public EmailTab4()
        {
            InitializeComponent();
            ControlTitle="E-Mail";

            OnMessage=(ItemMessage m)=>
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
                    }
                }

                if(!e.Handled)
                {
                    Grid.ProcessKeyboard(e);      
                }
            };

            OnLoad=()=>
            {
                GridInit();
                SetDefaults();
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
        }

        public void GridInit()
        {
            {
                var columns = new List<DataGridHelperColumn>()
                {
                    new DataGridHelperColumn()
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="ИД",
                        Path="ID",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Создано",
                        Path="DISPATCH",
                        Group="Дата",
                        Doc="Дата добавления в очередь",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2=16,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Отправлено",
                        Path="SENT_DATE",
                        Group="Дата",
                        Doc="Дата отправки",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2=16,
                        //Stylers2=new List<StylerProcessor>()
                        //{
                        //    {
                        //        new StylerProcessor(
                        //            StylerTypeRef.ForegroundColor,
                        //            (Dictionary<string, string> row,int mode) =>
                        //            {
                        //                var result=DependencyProperty.UnsetValue;
                        //                var description=new Dictionary<string, string>();
                        //                var color = "";

                        //                color=HColor.RedFG;
                        //                description.CheckAdd(color,"утвержденные заявки");

                        //                color=HColor.GreenFG;
                        //                description.CheckAdd(color,"заявки в работе\nвсе, заявки, которые уже добавлены в очередь ГА\n автоматически или вручную");


                        //                if (!string.IsNullOrEmpty(color))
                        //                {
                        //                    result=color.ToBrush();
                        //                }

                        //                if(mode==1)
                        //                {
                        //                    return description;
                        //                }
                        //                else
                        //                {
                        //                    return result;
                        //                }                                        
                        //            }
                        //        )
                        //    }
                        //},
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Задержка",
                        Path="DELAY",
                        Doc="Интервал между постановкой в очередь и отправкой",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2=8,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Тема",
                        Path="SUBJECT",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=16,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Отправитель",
                        Path="SENDER",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=16,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Отправлено",
                        Path="SENT",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        Width2=10,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Ошибка",
                        Path="ERROR",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        Width2=10,
                    },
                };
                Grid.SetColumns(columns);
                Grid.SetPrimaryKey("ID");
                Grid.SetSorting("DISPATCH",ListSortDirection.Descending);
                Grid.SearchText=SearchText;
                Grid.ColumnWidthMode= GridBox.ColumnWidthModeRef.Full;

                Grid.OnLoadItems=LoadItems;
                Grid.OnSelectItem=(row) =>
                {
                };
                Grid.OnDblClick=(row) =>
                {
                    ProcessCommand("view");
                };
                Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
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
                        "view",
                        new DataGridContextMenuItem()
                        {
                            Header="Открыть",                            
                            Action=()=>
                            {
                                ProcessCommand("view");
                            },
                        }
                    },
                };

                Grid.AutoUpdateInterval=0;
                Grid.Descriription="Список сообщений электронной почты для рассылки";
                Grid.DebugName="email";
                Grid.Init();
                //Grid.DebugShowColumnsInfo();
                //Grid.ShowDescription();
            }
        }

        public void SetDefaults()
        {
            //FromDate.Text="20.10.2023 08:00:00";
            //ToDate.Text="20.10.2023 09:00:00";

            FromDate.Text=DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy 00:00:00");
            ToDate.Text=DateTime.Now.AddDays(1).ToString("dd.MM.yyyy 00:00:00");
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
                        //var email=new EmailView();
                        //email.Create();
                    }
                        break;

                    case "view":
                    {
                        var id=Grid.SelectedItem.CheckGet("ID").ToInt();    
                        if(id != 0)
                        {
                            //var email=new EmailView();
                            //email.Edit(id);
                        }
                    }
                        break;

                    case "refresh":
                    {
                        Grid.LoadItems();
                    }
                        break;

                    case "help":
                    {
                        Central.ShowHelp("/doc/l-pack-erp/production/production_tasks/complete");
                    }
                        break;
                }
            }
        }

        public async void LoadItems()
        {
            GridToolbar.IsEnabled=false;
            Grid.ShowSplash();
            bool resume = true;

            if(resume)
            {
                var f = FromDate.Text.ToDateTime();
                var t = ToDate.Text.ToDateTime();
                if(DateTime.Compare(f,t) > 0)
                {
                    //var msg="Дата начала должна быть меньше даты окончания."; 
                    //var d = new DialogWindow($"{msg}", "Проверка данных", "", DialogWindowButtons.OK);
                    //d.ShowDialog();
                    resume=false;
                }
            }

            if(resume)
            {
                
                var p = new Dictionary<string,string>();
                {
                    p.Add("FROM_DATE",FromDate.Text);
                    p.Add("TO_DATE",ToDate.Text);
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Messages");
                q.Request.SetParam("Object", "Email");
                q.Request.SetParam("Action", "List");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;
                
                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {

                        {
                            var ds=ListDataSet.Create(result,"ITEMS");
                            Grid.UpdateItems(ds);
                        }
                    }
                }      
            }

            GridToolbar.IsEnabled=true;
            Grid.HideSplash();
        }

        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            var b=(Button)sender;
            if(b != null)
            {
                var t=b.Tag.ToString();
                ProcessCommand(t);
            }
        }
    }


}
