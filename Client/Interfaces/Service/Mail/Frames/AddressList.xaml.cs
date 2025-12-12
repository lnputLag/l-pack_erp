using Client.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using static Client.Interfaces.Main.DataGridHelperColumn;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using UserControl = System.Windows.Controls.UserControl;

namespace Client.Interfaces.Service.Mail
{
    /// <summary>
    /// ярлык с адресом для почтовых конвертов, список ярлыков
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-06-22</released>
    /// <changed>2023-06-22</changed>
    public partial class AddressList : UserControl
    {
        public AddressList()
        {
            InitializeComponent();
            
            if(Central.InDesignMode()){
                return;
            }
            
            Id = 0;
            FrameName = "address_search";
            
            InitForm();
            InitGrid();
            SetDefaults();
            Central.Msg.Register(ProcessMessage);
        }
        
        public FormHelper Form { get; set; }

        public void InitForm()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {           
                new FormHelperField()
                {
                    Path="KEYWORD",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=KeywordText,                    
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },          
                    First=true,
                },
            };

            Form.SetFields(fields);
        }

        public void InitGrid()
        {
             //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=35,
                        MaxWidth=35,
                    },
                   
                    new DataGridHelperColumn
                    {
                        Header="ИНН",
                        Path="INN",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        MaxWidth=100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Получатель",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=200,
                        MaxWidth=450,
                    },
                   
                    new DataGridHelperColumn
                    {
                        Header="Тип",
                        Path="ADDRESS_TYPE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        MaxWidth=100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Адрес",
                        Path="ADDRESS_DATA",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=300,
                        MaxWidth=800,
                    },
                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=45,
                        MaxWidth=1500,
                    },
                    
                };
                Grid.SetColumns(columns);
            };

            Grid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            Grid.PrimaryKey = "_ROWNUMBER";
            

            Grid.DisableControls=()=>
            {
                GridToolbar.IsEnabled = false;
                Grid.ShowSplash();
            };
                
            Grid.EnableControls=()=>
            {
                GridToolbar.IsEnabled = true;
                Grid.HideSplash();
            };

            Grid.OnLoadItems = async ()=>
            {
                Grid.DisableControls();

                var today=DateTime.Now;
                bool resume = true;
                
                var v = Form.GetValues();
                if (resume)
                {
                    if (v.CheckGet("KEYWORD").IsNullOrEmpty())
                    {
                        resume = false;
                    }
                }

                if (resume)
                {
                    var p = new Dictionary<string, string>();
                    {
                    }
                    p = v;

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Mail");
                    q.Request.SetParam("Object", "Label");
                    q.Request.SetParam("Action", "ListSearch");
                    q.Request.SetParams(p);

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if(q.Answer.Status == 0)                
                    {
                        Grid.UpdateItemsAnswer(q.Answer,"ITEMS");
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }

                Grid.EnableControls();
            };


            Grid.OnSelectItem = (row) =>
            {
                ProcessCommand("actions_update");
            };
            
            Grid.OnDblClick= (row) =>
            {
                ProcessCommand("Select");
            };
            
            Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                {
                    "Select",
                    new DataGridContextMenuItem()
                    {
                        Header="Выбрать",
                        Action=() =>
                        {
                            ProcessCommand("Select");
                        }
                    }
                },
            };

            Grid.Init();
            Grid.Run();
        }

        private void SetDefaults()
        {
            Form.SetDefaults();
            ProcessCommand("actions_update");
        }
        
        private void ProcessCommand(string command)
        {
            command = command.ClearCommand();
            switch (command)
            {
                case "showed":
                    Grid.Run();
                    break;
                
                case "refresh":
                    Grid.LoadItems();
                    break;
                
                case "filter":
                    Grid.UpdateItems();
                    break;
                
                case "select":
                {
                    var row = Grid.SelectedItem;
                    if (row.Count > 0)
                    {
                        if (!row.CheckGet("ADDRESS_DATA").IsNullOrEmpty())
                        {
                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverGroup = "Mail",
                                SenderName = "AddressList",
                                Action = "Select",
                                Message = "",
                                ContextObject=row
                            });

                            Close();
                        }    
                    }
                                        
                }
                    break;

                case "actions_update":
                {

                    SelectButton.IsEnabled = false;
                    Grid.Menu["Select"].Enabled = false;
                    
                    var row = Grid.SelectedItem;

                    if (row.Count > 0)
                    {
                        if (!row.CheckGet("ADDRESS_DATA").IsNullOrEmpty())
                        {
                            SelectButton.IsEnabled = true;
                            Grid.Menu["Select"].Enabled = true;
                        }
                    }
                 
                }
                    break;
            }
        }

        public void ProcessMessage(ItemMessage message)
        {
            if(message!=null)
            {
                if (message.ReceiverGroup == "Mail")
                {
                    if(message.SenderName!="AddressList")
                    {
                        ProcessCommand(message.Action);
                    }
                }
            }
        }


        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (sender!=null)
            {
                var c = (System.Windows.Controls.Button) sender;
                var tag = c.Tag.ToString();
                ProcessCommand(tag);    
            }
        }
        
        
        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// идентификатор записи, с которой работает форма
        /// (primary key записи таблицы)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 1;

            var frameName = GetFrameName();

            Central.WM.Show(frameName, "Поиск в 1С", true, "add", this);
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            var frameName = GetFrameName();
            Central.WM.Close(frameName);
        }

        /// <summary>
        /// формирует уникальный идентификатор фрейма
        /// </summary>
        /// <returns></returns>
        public string GetFrameName()
        {
            string result = "";
            result = $"{FrameName}_{Id}";
            return result;
        }

    }
}
