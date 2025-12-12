using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Controls;

using Client.Interfaces.Main;

namespace Client.Interfaces.Service.Mail
{
    /// <summary>
    /// форма редактирования ярлыка
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-06-22</released>
    /// <changed>2023-06-22</changed>
    public partial class Label : UserControl
    {
        public Label()
        {
            InitializeComponent();
            
            if(Central.InDesignMode()){
                return;
            }

            ChainUpdate=true;
            ResultVisible=false;
            Id = 0;
            FrameName = "Label";
            KeywordInputTimer=new Timeout(
                1,
                () =>
                {
                    DoSearch();
                }
            );
            KeywordInputTimer.SetIntervalMs(200);

            SearchHideTimer=new Timeout(
                1,
                () =>
                {
                    SearchHide();
                }
            );
            SearchHideTimer.SetIntervalMs(500);

            SearchStatusTimer=
            new Timeout(
                3,
                () =>
                {
                    SetSearchStatus("");
                }
            );

            InitForm();
            InitGrid();
            SetDefaults();
            Central.Msg.Register(ProcessMessage);
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        public Timeout KeywordInputTimer { get; set; }
        public Timeout SearchHideTimer { get; set; }
        public Timeout SearchStatusTimer { get; set; }

        /// <summary>
        // инициализация компонентов
        /// </summary>
        public void InitForm()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TYPE_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Default="1",
                    Control=TypeSelect,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="RECIPIENT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=RecipientText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="ZIP_CODE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ZipCodeText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="ADDRESS",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=AddressText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                 new FormHelperField()
                {
                    Path="NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=NoteText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                
                new FormHelperField()
                {
                    Path="KEYWORD",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=KeywordText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = FormStatus;

            Form.AfterSet = (Dictionary<string, string> v) =>
            {
                RecipientText.Focus();
                //Name.SelectAll();
            };

            Form.OnValidate = (bool valid, string message) =>
            {
                if (valid)
                {
                    SaveButton.IsEnabled=true;
                    FormStatus.Text = "";
                }
                else
                {
                    SaveButton.IsEnabled=false;
                    FormStatus.Text = "Не все поля заполнены верно";
                }
            };

        }

        public void SetDefaults()
        {
            SetSearchStatus("");
            Form.SetDefaults();
            ProcessCommand("actions_update");
        }

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Accounts",
                ReceiverName = "",
                SenderName = "Role",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        

        
        /// создание новой записи
        /// </summary>
        public void Create()
        {
            Id = 0;
            GetData();
        }

        /// <summary>
        /// редактирвоание записи
        /// </summary>
        /// <param name="id"></param>
        public void Edit(int id)
        {
            Id = id;
            GetData();
        }

        public void SetSearchStatus(string txt)
        {
            if(txt.IsNullOrEmpty())
            {
                SearchStatus.Text="";
            }
            else
            {
                SearchStatus.Text=txt;
                SearchHide();
                SearchStatusTimer.Run();
            }
        }

        private void ProcessCommand(string command,ItemMessage message=null)
        {
            command = command.ClearCommand();
            switch (command)
            {
                 case "select":
                    {
                        var d = Grid.SelectedItem;
                        //var dataString = o.CheckGet("_DATA");
                        //if (!dataString.IsNullOrEmpty())
                        {
                            //var d=JsonConvert.DeserializeObject<Dictionary<string, string>>(dataString);
                            
                            if (d.Count > 0)
                            {
                                var v = new Dictionary<string, string>();
                            
                                var addressString = d.CheckGet("ADDRESS_DATA");
                                var a=Tools.AddressSplit(addressString);

                                v.CheckAdd("RECIPIENT", d.CheckGet("NAME_FULL"));    
                                v.CheckAdd("ZIP_CODE", a.CheckGet("ZIP_CODE"));
                                v.CheckAdd("ADDRESS", a.CheckGet("ADDRESS"));
                            
                                Form.SetValues(v);
                                SearchHideTimer.Restart();
                            }
                        }
                        
                    }
                    break;

                case "actions_update":
                {

                    Grid.Menu["Select"].Enabled = false;
                    
                    var row = Grid.SelectedItem;

                    if (row.Count > 0)
                    {
                        if (!row.CheckGet("ADDRESS_DATA").IsNullOrEmpty())
                        {
                            Grid.Menu["Select"].Enabled = true;
                        }
                    }
                 
                }
                    break;

                case "_select":
                    //Grid.Run();
                    if (message!=null)
                    {
                        var o = (Dictionary<string, string>)message.ContextObject;
                        var dataString = o.CheckGet("_DATA");
                        if (!dataString.IsNullOrEmpty())
                        {
                            var d=JsonConvert.DeserializeObject<Dictionary<string, string>>(dataString);
                            
                            if (d.Count > 0)
                            {
                                var v = new Dictionary<string, string>();
                            
                                var addressString = d.CheckGet("ADDRESS_DATA");
                                var address = "";
                                var zipCode = "";

                                if (!addressString.IsNullOrEmpty())
                                {
                                    zipCode = addressString.Substring(0, 6);
                                    address = addressString.Substring(6+2, addressString.Length-6-2);
                                }


                                v.CheckAdd("RECIPIENT", d.CheckGet("NAME"));    
                                v.CheckAdd("ZIP_CODE", zipCode);
                                v.CheckAdd("ADDRESS", address);
                            
                                Form.SetValues(v);
                                
                                var frameName = GetFrameName();
                                Central.WM.SetActive(frameName);
                            }
                        }
                        
                    }
                    break;
                
                
            }
        }

        
        public void InitGrid()
        {
             //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    //new DataGridHelperColumn
                    //{
                    //    Header="#",
                    //    Path="_ROWNUMBER",
                    //    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    //    MinWidth=35,
                    //    MaxWidth=35,
                    //},
                    new DataGridHelperColumn
                    {
                        Header="Получатель",
                        Path="NAME_FULL",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=200,
                        MaxWidth=380,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Адрес",
                        Path="ADDRESS_DATA",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=400,
                        MaxWidth=800,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИНН",
                        Path="INN",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=90,
                        MaxWidth=90,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Тип",
                        Path="ADDRESS_TYPE",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=120,
                        MaxWidth=120,
                    },
                    
                };
                Grid.SetColumns(columns);
            };

            Grid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            Grid.PrimaryKey = "_ROWNUMBER";
            Grid.AutoUpdateInterval=0;
            Grid.UseRowHeader=false;

            Grid.DisableControls=()=>
            {
                //GridToolbar.IsEnabled = false;
                //Grid.ShowSplash();
            };
                
            Grid.EnableControls=()=>
            {
                //GridToolbar.IsEnabled = true;
                //Grid.HideSplash();
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

                SetSearchStatus("");

                if (resume)
                {
                    var p = new Dictionary<string, string>();
                    {
                    }
                    p.CheckAdd("KEYWORD",v.CheckGet("KEYWORD"));

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
                        //Grid.UpdateItemsAnswer(q.Answer,"ITEMS");

                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            {
                                var ds = ListDataSet.Create(result, "ITEMS");
                                Grid.UpdateItems(ds);

                                if(ds.Items.Count > 0)
                                {
                                    SearchContainer.Visibility = System.Windows.Visibility.Visible;
                                    ResultVisible=true;
                                }
                                else
                                {                                    
                                    //SearchHide();
                                }


                            }
                        }
                    }
                    else
                    {
                        //q.ProcessError();
                        SetSearchStatus(q.Answer.Error.Message);
                    }
                }

                Grid.EnableControls();
            };


            Grid.OnSelectItem = (row) =>
            {
                //ProcessCommand("actions_update");
                //ProcessCommand("Select");
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
        }

        public void DoSearch()
        {

            if(KeywordText.Text.Length >= 3 )
            {
                Grid.LoadItems();
            }
            else
            {
                SearchHide();
            }
        }

        private void SearchHide()
        {
            ChainUpdate=false;
            SearchContainer.Visibility = System.Windows.Visibility.Hidden;
            Grid.ClearItems();
            //KeywordText.Text="";
            ChainUpdate=true;
            ResultVisible=false;
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

            if (Id == 0)
            {
                Central.WM.Show(frameName, "Новый ярлык", true, "add", this);
            }
            else
            {
                Central.WM.Show(frameName, $"Ярлык #{Id}", true, "add", this);
            }
            ChainUpdate=true;
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

        
        /// <summary>
        /// получение данных с сервера
        /// </summary>
        public async void GetData()
        {
            DisableControls();

            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID", Id.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Mail");
                q.Request.SetParam("Object", "Label");
                q.Request.SetParam("Action", "Get");
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
                        {
                            var ds = ListDataSet.Create(result, "TYPES");
                            TypeSelect.SetItems(ds, "ID", "TITLE");
                        }

                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            if(ds.Items.Count > 0)
                            {
                                Form.SetValues(ds);
                            }
                            else
                            {
                                Form.SetDefaults();
                            }                            
                        }

                        Show();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            EnableControls();
        }

        /// <summary>
        /// подготовка данных
        /// </summary>
        public void Save()
        {
            bool resume = true;
            string error = "";

            //стандартная валидация данных средствами формы
            if (resume)
            {
                var validationResult = Form.Validate();
                if (!validationResult)
                {
                    resume = false;
                }
            }

            var v = Form.GetValues();

            //отправка данных
            if (resume)
            {
                SaveData(v);
            }
            else
            {
                Form.SetStatus(error, 1);
            }
        }

        public void Search()
        {
            var h=new AddressList();
            h.Show();
        }
        
        /// <summary>
        /// отпаравка данных на сервер
        /// </summary>
        public async void SaveData(Dictionary<string, string> p)
        {
            DisableControls();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Mail");
            q.Request.SetParam("Object", "Label");
            q.Request.SetParam("Action", "Save");

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
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        var id = ds.GetFirstItemValueByKey("ID").ToInt();

                        if (id != 0)
                        {
                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverGroup = "Mail",
                                SenderName = "Label",
                                Action = "Refresh",
                                Message = $"{id}",
                            });

                            Close();
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
        }
        
        public void ProcessMessage(ItemMessage message)
        {
            if(message!=null)
            {
                if(message.ReceiverGroup=="Mail")
                {
                    if (message.SenderName != "Label")
                    {
                        ProcessCommand(message.Action,message);
                    }
                }
            }
        }

        private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Save();
        }
        
        private void SearchButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Search();
        }

        private bool ChainUpdate {get;set;}
        private bool ResultVisible {get;set;}
        private void TextOnChange(object sender, TextChangedEventArgs e)
        {
            /*
            if (sender != null)
            {
                var t = (TextBox) KeywordText;
                var text = t.Text;
                if (!text.IsNullOrEmpty())
                {
                    KeywordInput.Restart();
                }
            }
            */
            if (KeywordInputTimer!=null && ChainUpdate)
            {
                //if(KeywordText.Text.Length >= 3 )
                {
                    KeywordInputTimer.Restart();    
                }
                //SearchHide();
            }
            
        }

        private void KeywordText_GotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            if ( !ResultVisible)
            {
                KeywordText.Text="";
            }
        }
    }
}
