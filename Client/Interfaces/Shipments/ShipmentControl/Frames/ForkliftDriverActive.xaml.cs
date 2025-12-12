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

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// список "активные водители погрузчиков"
    /// (назначение погрузчиков в рабочую смену)
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>2</version>
    /// <released>2021-12-08</released>     
    public partial class ForkliftDriverActive : UserControl
    {
        public ForkliftDriverActive()
        {
            WindowTitle="Активные водители погрузчиков";

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            //регистрация обработчика клавиатуры
            PreviewKeyDown+=ProcessKeyboard;

            GridInit();
            SetDefaults();
        }

        /// <summary>
        /// форма с полями для фильтрации данных
        /// </summary>
        public FormHelper Form { get; set; }

        private string WindowTitle { get; set; }

        /// <summary>
        /// Идентификатор площадки
        /// </summary>
        public int FactoryId = 1;

        #region Common

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="ShipmentControl",
                ReceiverName = "",
                SenderName = "ForkliftDriverActive",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
            
            //утилизация ресурсов грида
            Grid.Destruct();
        }
        
        /// <summary>
        /// обработчик сообщений
        /// </summary>
        private void ProcessMessages(ItemMessage m)
        {
            //group 
            if(m.ReceiverGroup.IndexOf("ShipmentControl") > -1)
            {
                //object
                if(m.ReceiverName.IndexOf("ForkliftDriverActive") > -1)
                {
                    switch(m.Action)
                    {
                        case "Refresh":
                            Grid.LoadItems();
                            break;
                    }    
                }                
            }
        }
        
        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        private void ProcessKeyboard(object sender,System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled=true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled=true;
                    break;                
            }
        }    

        /// <summary>
        /// обработчик системы навигации по URL
        /// </summary>
        public void ProcessNavigation()
        {
        }
        
        /// <summary>
        /// Вызов справки
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/shipments/control/common/active_forkliftdrivers");
        }

        #endregion

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            {
                var list = new Dictionary<string, string>();
                list.Add("0", "Все");
                list.Add("1", "Рулоны");
                list.Add("2", "СГП");
                list.Add("3", "Макулатура");
                Department.Items = list;
            }

            Form.SetDefaults();
        }

        /// <summary>
        /// инициализация грида
        /// </summary>
        public void GridInit()
        {
            //grid
            {
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,                        
                        MinWidth=50,
                        MaxWidth=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="В смене",
                        Path="ACTIVE_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Editable=true,
                        MinWidth=60,
                        MaxWidth=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ФИО",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отдел",
                        Path="STOCK_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Телефон",
                        Path="PHONE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД водителя",
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Path="STOCK_ROLL",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Path="STOCK_PRODUCT",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Path="STOCK_WASTEPAPER",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                };

                Grid.SetColumns(columns);
                Grid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
                Grid.AutoUpdateInterval = 0;
                Grid.SearchText=SearchText;
                Grid.Init();
                Grid.OnLoadItems = GridLoadItems;
                Grid.OnFilterItems = GridFilterItems;

            }
            
            //grid form
            {
                Form = new FormHelper();

                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="SEARCH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=SearchText,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="SHOW_ALL",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=ActiveOnly,
                        ControlType="CheckBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="DEPARTMENT_ID",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Default="0",
                        Control=Department,
                        ControlType="SelectBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },                   
                };

                Form.SetFields(fields);

                //после установки значений
                Form.AfterSet = (Dictionary<string, string> v) =>
                {
                    //фокус на кнопку обновления
                    //RefreshButton.Focus();
                };

                {
                    Department.OnSelectItem = (Dictionary<string, string> selectedItem) =>
                    {
                        bool result = true;
                        if (selectedItem.Count > 0)
                        {
                            Grid.UpdateItems();
                        }
                        return result;
                    };
                }
            }

            //фокус ввода
            Grid.Focus();
        }
               

        /// <summary>
        /// получение записей
        /// </summary>
        public async void GridLoadItems()
        {
            GridDisableControls();

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("SHOW_ALL","1");
                p.Add("FACTORY_ID", $"{FactoryId}");
            }        

            var q = new LPackClientQuery();
            q.Request.SetParam("Module","Shipments");
            q.Request.SetParam("Object","ForkliftDriver");
            q.Request.SetParam("Action","List");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if(q.Answer.Status == 0)                
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                if(result!=null)
                {
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        Grid.UpdateItems(ds);                       
                    }
                }
            }

            GridEnableControls();
        }

        /// <summary>
        /// фильтрация записей
        /// </summary>
        public void GridFilterItems()
        {
            if (Grid.GridItems != null)
            {
                if (Grid.GridItems.Count > 0)
                {
                    //фильтрация строк
                    {
                        var v = Form.GetValues();    
                    
                        var showAll = false;
                        if (v.CheckGet("SHOW_ALL").ToBool())
                        {
                            // покаывать все
                            showAll = true;
                        }

                        var departmentId=v.CheckGet("DEPARTMENT_ID").ToInt();

                        var items = new List<Dictionary<string, string>>();
                        
                        foreach (Dictionary<string, string> row in Grid.GridItems)
                        {
                            var includeRowByActive = false;
                            if (showAll)
                            {
                                includeRowByActive = true;
                            }
                            else
                            {
                                if(row.CheckGet("LOCKED").ToInt() == 0)
                                {
                                    includeRowByActive=true;
                                }
                            }

                            var includeRowByDepartment = false;
                            if (departmentId != 0)
                            {
                                switch(departmentId)
                                {
                                    case 1:
                                    {
                                        if(row.CheckGet("STOCK_ROLL").ToInt() == 1)
                                        {
                                            includeRowByDepartment=true;
                                        }
                                    }
                                    break;

                                    case 2:
                                    {
                                        if(row.CheckGet("STOCK_PRODUCT").ToInt() == 1)
                                        {
                                            includeRowByDepartment=true;
                                        }
                                    }
                                    break;

                                    case 3:
                                    {
                                        if(row.CheckGet("STOCK_WASTEPAPER").ToInt() == 1)
                                        {
                                            includeRowByDepartment=true;
                                        }
                                    }
                                    break;
                                }
                            }
                            else
                            {
                                includeRowByDepartment = true;
                            }

                            if ((includeRowByActive) && (includeRowByDepartment))
                            {
                                items.Add(row);
                            }
                        }
                        Grid.GridItems = items;
                    }
                }
            }
        }

        public void GridDisableControls()
        {
            Grid.ShowSplash();
            FormToolbar.IsEnabled=false;
        }

        public void GridEnableControls()
        {
            Grid.HideSplash();
            FormToolbar.IsEnabled=true;
        }

        public void Edit()
        {
            Grid.Run();
            Grid.LoadItems();
            Show();
        }

        /// <summary>
        /// Сохранение выбора активных водителей погрузчиков
        /// </summary>
        public async void Save()
        {
            bool resume = true;

            var selectedDrivers = new List<int>();
            
            if (resume)
            {
                if (Grid.Items.Count>0)
                {
                    foreach (Dictionary<string,string> row in Grid.Items)
                    {
                        if (row.CheckGet("ACTIVE_FLAG").ToInt() == 1)
                        {
                            var id = row["ID"].ToInt();
                            if (!selectedDrivers.Contains(id))
                            {
                                selectedDrivers.Add(id);
                            }
                        }
                    }
                }
            }
            
            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("ID_LIST",  String.Join(",", selectedDrivers));
                    p.Add("STATUS_ID",  "1");
                    p.Add("FACTORY_ID", $"{FactoryId}");
                }
                
                var q = new LPackClientQuery();
                q.Request.SetParam("Module","Shipments");
                q.Request.SetParam("Object","ForkliftDriver");
                q.Request.SetParam("Action","SetActive");
                
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
                            var id=ds.GetFirstItemValueByKey("ID").ToInt();
                        
                            if(id!=0)
                            {
                                //отправляем сообщение гриду о необходимости обновить данные
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup="ShipmentControl",
                                    ReceiverName = "DriverList",
                                    SenderName = "ActiveForkliftdrivers",
                                    Action = "Refresh",
                                });
                                
                            }
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        #region Window

        /// <summary>
        /// Окно с формой редактирования
        /// </summary>
        public Window Window { get;set;}
        
        /// <summary>
        /// Отображение окна с формой редактирования
        /// </summary>
        public void Show()
        {
            string title = WindowTitle;
            
            int w=(int)Width;
            int h=(int)Height;
            
            Window = new Window
            {
                Title = title,
                Width = w + 17,
                Height = h + 40,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow,
            };
            
            Window.Content = new Frame
            {
                Content = this,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            
            if (Window != null)
            {
                Window.Topmost = true;
                Window.ShowDialog();
            }

            Window.Closed+=Window_Closed;

        }

        /// <summary>
        /// Дополнительный обработчик закрытия окна
        /// </summary>
        private void Window_Closed(object sender,EventArgs e)
        {
            Destroy();
        }

        public void Close()
        {
            var window = this.Window;
            if (window != null)
            {
                window.Close();
            }

            Destroy();
        }

        #endregion


        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        
        private void HelpButton_OnClick(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void ActiveOnly_Click(object sender, RoutedEventArgs e)
        {
            Grid.UpdateItems();
        }
    }
}
