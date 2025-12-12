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

namespace Client.Interfaces.Messages
{
    /// <summary>
    /// сообщения электронной почты
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-09-29</released>
    /// <changed>2023-09-29</changed>
    public partial class EmailTab:ControlBase
    {
        public EmailTab()
        {
            InitializeComponent();

            ControlSection = "messages";
            RoleName = "[erp]messages";
            ControlTitle = "E-Mail";
            DocumentationUrl = "/doc/l-pack-erp/service/messages/email";


            OnMessage=(ItemMessage m)=>
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

                Commander.SetCurrentGridName("Grid");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "email_refresh",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "EmailRefresh",
                        MenuUse = true,
                        Action = () =>
                        {
                            Grid.LoadItems();
                        },
                    });

                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "email_create",
                            Title = "Создать",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "EmailCreateButton",
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var h = new EmailForm();
                                h.Init("create", "0");
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = Grid.GetPrimaryKey();
                                var row = Grid.SelectedItem;
                                if(row.CheckGet(k).ToInt() != 0)
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });
                        
                        Commander.Add(new CommandItem()
                        {
                            Name = "email_edit",
                            Title = "Открыть",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "EmailEditButton",
                            HotKey = "Return|DoubleCLick",
                            AccessLevel = Common.Role.AccessMode.ReadOnly,
                            Action = () =>
                            {
                                var id = Grid.SelectedItem.CheckGet("ID").ToInt();
                                if(id != 0)
                                {
                                    var h = new EmailForm();
                                    h.Init("edit", id.ToString());
                                }
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = Grid.GetPrimaryKey();
                                var row = Grid.SelectedItem;
                                if(row.CheckGet(k).ToInt() != 0)
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
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="DATE_START",
                    FieldType=FormHelperField.FieldTypeRef.DateTime,
                    Control=DateStart,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Default=DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy 00:00:00")
                },
                new FormHelperField()
                {
                    Path="DATE_FINISH",
                    FieldType=FormHelperField.FieldTypeRef.DateTime,
                    Control=DateFinish,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Default=DateTime.Now.AddDays(1).ToString("dd.MM.yyyy 00:00:00")
                },
                 new FormHelperField()
                {
                    Path="TYPE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Default="0",
                    Control=Type,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange=(FormHelperField f, string v)=>
                    {
                        Grid.UpdateItems();
                    },
                    OnCreate = (FormHelperField f) =>
                    {
                        var list = new Dictionary<string, string>();
                        list.Add("0", "Все");
                        list.Add("1", "Неотправленные");
                        list.Add("2", "Отправленные");
                        list.Add("3", "С ошибкой");

                        var c=(SelectBox)f.Control;
                        if(c != null)
                        {
                            c.Items=list;
                            c.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
                        }
                    },
                },
                
            };
            Form.SetFields(fields);
        }

        public void GridInit()
        {
            var columns = new List<DataGridHelperColumn>()
            {
                new DataGridHelperColumn()
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn()
                {
                    Header="Зпланировано к отправке",
                    Path="DISPATCH",
                    Doc="Дата добавления в очередь",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format="dd.MM.yyyy HH:mm:ss",
                    Width2=15,
                },
                new DataGridHelperColumn()
                {
                    Header="Отправлено",
                    Path="SENT_DATE",
                    Doc="Фактическая дата отправки",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format="dd.MM.yyyy HH:mm:ss",
                    Width2=15,
                },
                new DataGridHelperColumn()
                {
                    Header="Задержка, мин",
                    Path="DELAY",
                    Doc="Интервал между постановкой в очередь и отправкой",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Format="dd.MM.yyyy HH:mm:ss",
                    Width2=9,
                },
                new DataGridHelperColumn()
                {
                    Header="Тема",
                    Path="SUBJECT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn()
                {
                    Header="Отправитель",
                    Path="SENDER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn()
                {
                    Header="Вложение",
                    Path="ATTACH",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    Width2=10,
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
                    Width2=8,
                },
            };
            Grid.SetColumns(columns);
            Grid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        {
                            if (!row.CheckGet("SENT").ToBool())
                            {
                                color = HColor.Blue;
                            }
                        }

                        {
                            if (row.CheckGet("ERROR").ToBool())
                            {
                                color = HColor.Red;
                            }
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };
            Grid.SetPrimaryKey("ID");
            Grid.SetSorting("DISPATCH",ListSortDirection.Descending);            
            Grid.ColumnWidthMode= GridBox.ColumnWidthModeRef.Full;
            Grid.SearchText = SearchText;
            Grid.Toolbar = GridToolbar;
            Grid.UseProgressSplashAuto = true;
            Grid.UseProgressBar = true;
            Grid.QueryLoadItems = new RequestData()
            {
                Module = "Messages",
                Object = "Email",
                Action = "List",
                Timeout= Central.Parameters.RequestTimeoutDefault,
                AnswerSectionKey = "ITEMS",
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = Form.GetValues();
                },
            };
            Grid.OnFilterItems = ()=>
            { 
                var v = Form.GetValues();
                var type = v.CheckGet("TYPE").ToInt();

                if(Grid.Items != null)
                {
                    if(type != 0)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach(Dictionary<string, string> row in Grid.Items)
                        {
                            var include = false;

                            switch(type)
                            {
                                //Неотправленные
                                case 1:
                                    {
                                        if(
                                            !row.CheckGet("SENT").ToBool()
                                            && !row.CheckGet("ERROR").ToBool()
                                        )
                                        {
                                            include = true;
                                        }
                                    }
                                    break;

                                //Отправленные
                                case 2:
                                    {
                                        if(
                                            row.CheckGet("SENT").ToBool()
                                            && !row.CheckGet("ERROR").ToBool()
                                        )
                                        {
                                            include = true;
                                        }
                                    }
                                    break;

                                //С ошибкой
                                case 3:
                                    {
                                        if(row.CheckGet("ERROR").ToBool())
                                        {
                                            include = true;
                                        }
                                    }
                                    break;
                            }

                            if(include)
                            {
                                items.Add(row);
                            }
                        }
                        Grid.Items = items;
                    }
                }
            }; 
                
            Grid.Commands = Commander;
            Grid.Init();
        }

        public void SetDefaults()
        {
            Form.SetDefaults();
        }
    }
}
