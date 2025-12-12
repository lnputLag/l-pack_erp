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
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Sales
{
    public partial class SalesSecondaryReport:UserControl
    {
        public SalesSecondaryReport()
        {
            RefreshButtonBlue=false;

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            FormInit();
            GridInit();

            ProcessPermissions();
        }

        public string RoleName = "[erp]sales_report_secondary";

        /// <summary>
        /// форма фильтра
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// флаг акцента внимания на кнопку Показать
        /// при изменениях в форме поднимается, по клику на кнопке, опускается
        /// при поднятом флаге кнопка окрашивается в синий цвет
        /// </summary>
        public bool RefreshButtonBlue { get; set; }

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

            if (Grid != null && Grid.Menu != null && Grid.Menu.Count > 0)
            {
                foreach (var manuItem in Grid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        private void ProcessMessages(ItemMessage m)
        {
            //Group ProductionTask
            if (m.ReceiverGroup.IndexOf("Sales") > -1)
            {
                switch (m.Action)
                {
                    case "Refresh":
                        Grid.LoadItems();
                        break;
                }
            }
        }

        /// <summary>
        /// отображение статьи в справочной системе
        /// </summary>
        public void ShowHelp()
        {
            //Central.ShowHelp("/doc/l-pack-erp/production/production_tasks/planning");
        }

        /// <summary>
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="Sales",
                ReceiverName = "",
                SenderName = "SalesSecondaryReport",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            Grid.Destruct();
        }


        public void FormInit()
        {
            Form=new FormHelper();

            //список колонок формы
            var fields=new List<FormHelperField>()
            {              
                new FormHelperField()
                {
                    Path="FROM_DATE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=FromDate,
                    Default=DateTime.Now.AddMonths(-1).ToString("dd.MM.yyyy"),
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TO_DATE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ToDate,
                    Default=DateTime.Now.ToString("dd.MM.yyyy"),
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SEARCH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=SearchText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },                  
            };

            Form.SetFields(fields);            
            Form.SetDefaults();
            
            RefreshButtonCheck(true);
        }

        public void GridInit()
        {
            //инициализация грида
            {
                //список колонок грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,                        
                        MaxWidth=55, 
                    },                 
                    new DataGridHelperColumn
                    {
                        Header="Дата",
                        Path="ON_DATE",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy",
                        MinWidth=70,                        
                        MaxWidth=90,  
                    },  
                    new DataGridHelperColumn
                    {
                        Header="ИД позиции заявки",
                        Path="POSITION_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=70,                        
                        MaxWidth=90,  
                    }, 
                    new DataGridHelperColumn
                    {
                        Header="Партнер",
                        Path="PARTNER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,   
                        MaxWidth=200,  
                    },                    
                    new DataGridHelperColumn
                    {
                        Header="Покупатель",
                        Path="BUYER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,   
                        MaxWidth=200,  
                    },                    
                    new DataGridHelperColumn
                    {
                        Header="Конечный грузополучатель",
                        Path="CONSIGNEE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,  
                        MaxWidth=200,  
                    },                    
                    new DataGridHelperColumn
                    {
                        Header="ИНН грузополучателя",
                        Path="CONSIGNEE_INN",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,  
                        MaxWidth=70,  
                    },    
                    new DataGridHelperColumn
                    {
                        Header="Адрес доставки",
                        Path="DELIVERY_ADDRESS",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,  
                        MaxWidth=150,  
                    },    
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Path="PRODUCT_ARTIKUL",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=120,  
                        MaxWidth=120,  
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,                        
                        MaxWidth=500,  
                    },                    
                    new DataGridHelperColumn
                    {
                        Header="Печать",
                        Path="PRODUCT_PRINTING",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=35,                        
                        MaxWidth=35,  
                    },   
                    new DataGridHelperColumn
                    {
                        Header="Бренд",
                        Path="CONSUMER_BRAND",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,      
                        MaxWidth=150,  
                    },
                    new DataGridHelperColumn
                    {
                        Header="Менеджер по продажам",
                        Path="SALES_NAME_FULL",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,      
                        MaxWidth=150,  
                    },
                    new DataGridHelperColumn
                    {
                        Header="E-mail менеджера по продажам",
                        Path="SALES_EMAIL",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,      
                        MaxWidth=90,  
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отрасль грузополучателя",
                        Path="CONSIGNEE_INDUSTRY",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,      
                        MaxWidth=150,  
                    },
                    new DataGridHelperColumn
                    {
                        Header="Область покупателя",
                        Path="BUYER_DISTRICT",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,      
                        MaxWidth=150,  
                    },
                    new DataGridHelperColumn
                    {
                        Header="Округ покупателя",
                        Path="BUYER_REGION",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,      
                        MaxWidth=150,  
                    },
                    new DataGridHelperColumn
                    {
                        Header="Объем продаж, кв.м.",
                        Path="SQUARE",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=70,   
                        MaxWidth=70,  
                    },                    
                    new DataGridHelperColumn
                    {
                        Header="Объем продаж, шт.",
                        Path="QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=70,   
                        MaxWidth=70,  
                    },                    
                    new DataGridHelperColumn
                    {
                        Header="Сумма продаж, р.",
                        Path="SUMM",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=70,   
                        MaxWidth=70,  
                    },  
                    new DataGridHelperColumn
                    {
                        Header="Примечание",
                        Path="CONSUMER_NOTE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,   
                        MaxWidth=150,  
                    },
                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,   
                        MaxWidth=900,  
                    },                     
                };

                Grid.SetColumns(columns);
                Grid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
                Grid.SearchText = SearchText;
                Grid.Init();

                //данные грида
                Grid.OnLoadItems = GridItemsLoad;
                Grid.OnFilterItems = GridItemsFilter;
                //Grid.Run();

                //фокус ввода на грид        
                Grid.Focus();
            }
        }
         
        

        /// <summary>
        /// получение записей
        /// </summary>
        public async void GridItemsLoad()
        {
            DisableControls();

            RefreshButtonCheck(false);
            
            bool resume = true;
            
            var v=Form.GetValues();

            if (resume)
            {
                var f = v.CheckGet("FROM_DATE").ToDateTime();
                var t = v.CheckGet("TO_DATE").ToDateTime();
                if (DateTime.Compare(f, t) > 0)
                {
                    var msg = "Дата начала должна быть меньше даты окончания.";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();
                    resume = false;
                }
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module","Sales");
                q.Request.SetParam("Object","Reports");
                q.Request.SetParam("Action","MakeSecondarySales");
                q.Request.SetParams(v);

                q.Request.Timeout=60000;
                
                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if(q.Answer.Status == 0)                
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                    if(result!=null)
                    {
                        var ds=ListDataSet.Create(result,"ITEMS");
                        Grid.UpdateItems(ds);
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }


            EnableControls();
        }

        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            Grid.ShowSplash();
        }

        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            Grid.HideSplash();
        }

        /// <summary>
        /// фильтрация записей
        /// </summary>
        public async void GridItemsFilter()
        {
            if (Grid.GridItems != null)
            {
                if (Grid.GridItems.Count > 0)
                {
                    //обработка строк

                    //фильтрация 

                }
            }
        }


        public void RefreshButtonCheck(bool b)
        {
            RefreshButtonBlue=b;

            var style="Button";
            if(RefreshButtonBlue)
            {
                style="FButtonPrimary";
            }
            RefreshButton.Style=(Style)RefreshButton.TryFindResource(style);
        }

        /// <summary>
        /// экспорт записей грида в Excel
        /// </summary>
        private async void ExportToExcel()
        {
            if(Grid!=null)
            {
                if(Grid.Items.Count>0)
                {
                    var eg = new ExcelGrid();
                    var cols=Grid.Columns;
                    eg.SetColumnsFromGrid(cols);
                    eg.Items = Grid.Items;
                    await Task.Run(() =>
                    {
                        eg.Make();
                    });
                }
            }
        }

        private void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void RefreshButton_Click(object sender,RoutedEventArgs e)
        {            
            Grid.LoadItems();
            RefreshButtonCheck(false);
        }

        private void ExportButton_Click(object sender,RoutedEventArgs e)
        {
            ExportToExcel();
        }

        private void FromDate_TextChanged(object sender,TextChangedEventArgs e)
        {
            RefreshButtonCheck(true);
        }

        private void ToDate_TextChanged(object sender,TextChangedEventArgs e)
        {
            RefreshButtonCheck(true);
        }
    }

}
