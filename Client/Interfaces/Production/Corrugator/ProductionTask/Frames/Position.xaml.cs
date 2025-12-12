using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// подробности по позиции пз
    /// (ярлык позиции)
    /// </summary>
    /// <author>balchugov_dv</author>    
    public partial class Position : UserControl
    {
        /*
         
                Grid1                   Grid2
                схемы производства      Заявки
                MAIN_SCHEME             APPLICATION
                SCHEME

                Grid3
                ПЗПР
                POSITION


                LoadData:
                    Module="Production"
                    Object="Position"
                    Action="GetData"


                

             
         */

        public Position()
        {
            InitializeComponent();

            ProductionTaskId = 0;
            ProcessingTaskId = 0;
            ApplicationPositionId = 0;
            TlsId = 0;
            GoodsId = 0;
            CategoryId = 0;
            ReworkFlag = false;
            ReturnTabName = "";
            Grid2SelectedItem = new Dictionary<string, string>();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            //регистрация обработчика клавиатуры
            PreviewKeyDown += ProcessKeyboard;

            //Схемы производства
            GridInit();
            //Заявки
            Grid2Init();
            //Производственные задания на переработку
            Grid3Init();

            SetDefaults();

        }

        /// <summary>
        /// id ПЗГА
        /// </summary>
        public int ProductionTaskId { get; set; }
        /// <summary>
        /// id ПЗПР
        /// </summary>
        public int ProcessingTaskId { get; set; }
        /// <summary>
        /// id позиции заявки
        /// </summary>
        public int ApplicationPositionId { get; set; }
        /// <summary>
        /// id схемы производства
        /// </summary>
        private int TlsId { get; set; }
        /// <summary>
        /// id изделия
        /// </summary>
        public int GoodsId { get; set; }
        /// <summary>
        /// id категории изделия
        /// </summary>
        public int CategoryId { get; set; }
        /// <summary>
        /// Признак, что задание - перевыгон
        /// </summary>
        public bool ReworkFlag { get; set; }

        /// <summary>
        /// Имя вкладки, откуда вызвана эта вкладка и куда возвращается фокус 
        /// </summary>
        public string ReturnTabName{ get; set; }
        /// <summary>
        /// Имя вкладки
        /// </summary>
        public string TabName;
        /// <summary>
        /// Идентификатор производственной площадки, на которой выполняется ПЗГА
        /// </summary>
        public int FactoryId;

        private Dictionary<string, ListDataSet> Data{ get; set; }

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        public Dictionary<string,string> Grid2SelectedItem { get; set; }

        /// <summary>
        /// Деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="ProductionTask",
                ReceiverName = "",
                SenderName = "Position",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            Grid.Destruct();
        }





        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            
        }

        

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        public void GridInit()
        {
            //список колонок грида
            var columns = new List<DataGridHelperColumn>()
            {
                new DataGridHelperColumn()
                {
                    Header="ИД схемы производства",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=60,
                },
                new DataGridHelperColumn()
                {
                    Header="Скорость, шт/мин",
                    Path="SPEED",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=60,
                },
                new DataGridHelperColumn()
                {
                    Header="Изд/заг",
                    Doc="Количество изделий из одной заготовки",
                    Path="QUANTUTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=60,
                },
                new DataGridHelperColumn()
                {
                    Header="Коэффициент",
                    Path="QUANTUTY_NORM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=80,
                },

                new DataGridHelperColumn()
                {
                    Header="Станок",
                    Path="MACHINE_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=40,
                    MaxWidth=70,
                },
                 new DataGridHelperColumn()
                {
                    Header="Станок 2",
                    Path="MACHINE2_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=55,
                    MaxWidth=70,
                },
                new DataGridHelperColumn()
                {
                    Header="Изделие",
                    Path="PRODUCT_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=120,
                    MaxWidth=800,
                },
            };
            Grid.SetColumns(columns);
            Grid.RowStylers=new Dictionary<StylerTypeRef,StylerDelegate>()
            {
                {
                    StylerTypeRef.BackgroundColor,
                    (Dictionary<string, string> row) =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        if (row.CheckGet("ID").ToInt() == TlsId)
                        {
                            // главная схема производства
                            color = HColor.Blue;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };
            Grid.AutoUpdateInterval=0;

            Grid.SetSorting("_ROWNUMBER",ListSortDirection.Ascending);            
            //Grid.UseHit=false;
            Grid.Init();

            //загрузка данных в грид
            Grid.OnLoadItems=()=>
            {
                Grid.ShowSplash();
                if (Data!=null)
                {
                    
                    /*
                        если установлена главная схема производства,
                        выберем ее в списке схем
                     */
                    {

                        var ds=ListDataSet.Create(Data, "MAIN_SCHEME");
                        TlsId=ds.GetFirstItemValueByKey("TLS_ID").ToInt();
                    }

                    {
                        var ds=ListDataSet.Create(Data, "SCHEME");
                        Grid.UpdateItems(ds,false);
                        Grid.SelectRowByKey(TlsId,"ID",true);
                    }

                }
                Grid.HideSplash();   
            };

            //фильтрация данных грида
            Grid.OnFilterItems=()=>
            {

            };

            //выбор строки грида
            Grid.OnSelectItem=(Dictionary<string,string> selectedItem) =>
            {
                if(selectedItem.Count > 0)
                {
                    
                }
            };
        }

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        public void Grid2Init()
        {
            //список колонок грида
            var columns = new List<DataGridHelperColumn>()
            {
                new DataGridHelperColumn
                {
                    Header="ИД заявки",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Options="zeroempty",
                    MinWidth=60,
                    MaxWidth=60,                        
                },
                new DataGridHelperColumn
                {
                    Header="В заявке изделий, шт",
                    Doc="Количество изделий в заявке",
                    Path="IN_APPLICATION_QUANTITY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Options="zeroempty",
                    MinWidth=30,   
                    MaxWidth=70, 
                },
                new DataGridHelperColumn
                {
                    Header="В ПЗ заготовок, шт",
                    Doc="Количество заготовок в ПЗ",
                    Path="BLANK_QUANTITY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Options="zeroempty",
                    MinWidth=30,   
                    MaxWidth=70, 
                },      
                new DataGridHelperColumn
                {
                    Header="Готовность ПЗ",
                    Doc="Дата готовности ПЗ",
                    Path="PRODUCTION_DATE",
                    Format="dd.MM HH:mm",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth=70,
                    MaxWidth=70,
                },
                new DataGridHelperColumn
                {
                    Header="Отгрузка",
                    Doc="Назначенная дата отгрузки",
                    Path="SHIPMENT_DATE",
                    Format="dd.MM HH:mm",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth=70,
                    MaxWidth=70,
                },
                new DataGridHelperColumn
                {
                    Header="Изделие",
                    Doc="Наименование изделия",
                    Path="GOODS_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=100,
                    MaxWidth=800,                   
                },
                new DataGridHelperColumn
                {
                    Header="ИД изделия",
                    Path="GOODS_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=100,
                    MaxWidth=100,                   
                    Hidden=true,
                },
                 new DataGridHelperColumn
                {
                    Header="ИД отгрузки",
                    Path="SHIPMENT_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=70,
                    MaxWidth=70,
                },
            };
            Grid2.SetColumns(columns);
            Grid2.RowStylers=new Dictionary<StylerTypeRef,StylerDelegate>()
            {
                {
                    StylerTypeRef.BackgroundColor,
                    (Dictionary<string, string> row) =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        if (row.CheckGet("ID").ToInt() == ApplicationPositionId)
                        {
                            // привязанная позиция
                            color = HColor.Blue;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };
            Grid2.SetSorting("_ROWNUMBER",ListSortDirection.Ascending);
            Grid2.AutoUpdateInterval=0;
            Grid2.Init();

            //загрузка данных в грид
            Grid2.OnLoadItems=()=>
            {
                Grid2.ShowSplash();
                if (Data!=null)
                {
                    {
                        var ds=ListDataSet.Create(Data, "APPLICATION");
                        ds.ItemsPrepend(new Dictionary<string, string>(){
                            { "ID", "0" },
                            { "NUM", "" },
                            { "IN_APPLICATION_QUANTITY", "0" },
                            { "BLANK_QUANTITY", "0" },
                            { "GOODS_ID", "" },
                            { "GOODS_CATEGORY_ID", "" },
                            { "GOODS_NAME", "нет" },
                            { "SHIPMENT_DATE", "" },
                            { "PRODUCTION_DATE", "" },
                            { "SHIPMENT_ID", "" },
                        });
                        Grid2.UpdateItems(ds,false);
                    }

                    /*
                        если для данной позиции есть строка в списке,
                        выделим ее
                        PositionId=od.idorderdates
                     */
                    if(ApplicationPositionId!=0)
                    {
                        Grid2.SelectRowByKey(ApplicationPositionId,"ID",true);
                    }
                }
                Grid2.HideSplash();   
            };

            //фильтрация данных грида
            Grid2.OnFilterItems=()=>
            {

            };

            //выбор строки грида
            Grid2.OnSelectItem=(Dictionary<string,string> selectedItem) =>
            {
                if(selectedItem.Count > 0)
                {
                    Grid2SelectedItem=selectedItem;
                }
            };

            //двойной клик на строке откроет форму редактирования
            Grid2.OnDblClick=(Dictionary<string,string> selectedItem) =>
            {
                Save();
            };
        }


         /// <summary>
        /// Инициализация таблицы
        /// </summary>
        public void Grid3Init()
        {
            //список колонок грида
            var columns = new List<DataGridHelperColumn>()
            {
               
                new DataGridHelperColumn
                {
                    Header="ПЗПР",
                    Path="TASK_NUMBER",
                    Doc="символьный номер ПЗ",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width=80,                        
                }, 
                new DataGridHelperColumn
                {
                    Header="Изделие",
                    Path="GOODS_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=200,                        
                },
                  new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="GOODS_CODE",
                    Doc="Артикул изделия",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=150,                        
                },
                new DataGridHelperColumn
                {
                    Header="В заявке изделий, шт",
                    Path="QUANTITY",                    
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=60,                        
                },
                new DataGridHelperColumn
                {
                    Header="ГА",
                    Path="QUANTITY_CM",
                    Doc="количество заготовок на ГА, шт",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=60,                        
                },
                new DataGridHelperColumn
                {
                    Header="Клише",
                    Path="KLISHE_NUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width=80,                        
                },
                new DataGridHelperColumn
                {
                    Header="Штанцформа",
                    Path="SHTANZFORM_NUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width=80,                        
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="TASK_NOTE",
                    Doc="примечание ПЗ",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=80,                        
                },

                new DataGridHelperColumn
                {
                    Header="ИД отгрузки",
                    Path="TRANSPORT_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=60,                        
                },
                new DataGridHelperColumn
                {
                    Header="ИД ПЗПР",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width=80,                        
                }, 
               
            };
            Grid3.SetColumns(columns);
            Grid3.RowStylers=new Dictionary<StylerTypeRef,StylerDelegate>()
            {
                {
                    StylerTypeRef.BackgroundColor,
                    (Dictionary<string, string> row) =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        if (row.CheckGet("ID").ToInt() == ProcessingTaskId)
                        {
                            // привязанная позиция
                            color = HColor.Blue;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };
            Grid3.SetSorting("_ROWNUMBER",ListSortDirection.Ascending);
            //Grid3.UseSelecting=false;
            //Grid3.UseHit=false;
            Grid3.AutoUpdateInterval=0;
            Grid3.Init();

            //загрузка данных в грид
            Grid3.OnLoadItems=()=>
            {
                Grid3.ShowSplash();
                if (Data!=null)
                {
                    {
                        var ds=ListDataSet.Create(Data, "POSITION");
                        Grid3.UpdateItems(ds,false);
                    }

                    if(ApplicationPositionId!=0)
                    {
                        Grid3.SelectRowByKey(ProcessingTaskId,"ID",true);
                    }

                }
                Grid3.HideSplash();   
            };

            //фильтрация данных грида
            Grid3.OnFilterItems=()=>
            {

            };

            //выбор строки грида
            Grid3.OnSelectItem=(Dictionary<string,string> selectedItem) =>
            {
                if(selectedItem.Count > 0)
                {
                    
                }
            };
        }
     

        /// <summary>
        /// загрузка данных с сервера
        /// </summary>
        public async void LoadData()
        {
            DisableToolbar();
            Grid.ShowSplash();

            var p=new Dictionary<string,string>();
            {
                p.CheckAdd("GOODS_ID",      GoodsId.ToString());
                p.CheckAdd("CATEGORY_ID",   CategoryId.ToString());
                p.CheckAdd("POSITION_ID",   ApplicationPositionId.ToString());
                p.CheckAdd("TASK_ID",       ProductionTaskId.ToString());
                p.CheckAdd("FACTORY_ID",    FactoryId.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Position");
            q.Request.SetParam("Action", "GetData");
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

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
                    Data=result;  
                    Grid.LoadItems();
                    Grid2.LoadItems();
                    Grid3.LoadItems();
                }
            }
            

            EnableToolbar();
            Grid.HideSplash();  
        }

      
        public void Save()
        {
            bool resume=true;

            var p=new Dictionary<string,string>();
            

            if(resume)
            {
                if(ProductionTaskId!=0)
                {
                    p.CheckAdd("PRODUCTION_TASK_ID",ProductionTaskId.ToString());
                }
                else
                {
                    resume=false;
                }
            }

            if(resume)
            {
                if(GoodsId!=0)
                {
                    p.CheckAdd("GOODS_ID",GoodsId.ToString());
                }
                else
                {
                    resume=false;
                }
            }

            if(resume)
            {
                if(Grid2SelectedItem.Count>0)
                {
                    var positionId=Grid2SelectedItem.CheckGet("ID");
                    if (ReworkFlag)
                    {
                        if (positionId.ToInt() != ApplicationPositionId)
                        {
                            resume = false;
                            var dw = new DialogWindow("Задание на перевыгон нельзя привязать к другой заявке.\nСоздайте новое задание для новой заявки", "Изменение заявки");
                            dw.ShowDialog();
                        }
                        else
                        {
                            p.CheckAdd("POSITION_ID", positionId);
                        }
                    }
                    else
                    {
                        p.CheckAdd("POSITION_ID", positionId);
                    }
                }
                else
                {
                    resume=false;
                }
            }

            if(resume)
            {
                SaveData(p);
            }
        }

        public async void SaveData(Dictionary<string,string> p)
        {
            DisableToolbar();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "ProductionTask");
            q.Request.SetParam("Action", "BindApplication");
            q.Request.SetParams(p);

            await Task.Run(() => {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    var id=ds.GetFirstItemValueByKey("PRODUCTION_TASK_ID").ToInt();

                    if(id!=0)
                    {
                        Messenger.Default.Send(new ItemMessage()
                        {
                            ReceiverGroup = "ProductionTask",
                            ReceiverName = "PositionList",
                            SenderName = "Position",
                            Action = "Refresh",
                            Message = id.ToString(),
                        });        
                            
                        Close();
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableToolbar();
            
        }
       

        /// <summary>
        /// Вызов справки
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/shipments/control/listing");
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            //Group ProductionTask
            if(m.ReceiverGroup.IndexOf("ShipmentControl") > -1)
            {
               
            }
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessKeyboard(object sender,System.Windows.Input.KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.F5:
                    Grid.LoadItems();
                    e.Handled=true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled=true;
                    break;

                case Key.Home:
                    Grid.SetSelectToFirstRow();
                    e.Handled=true;
                    break;

                case Key.End:
                    Grid.SetSelectToLastRow();
                    e.Handled=true;
                    break;
            }
        }

        public void ProcessKeyboard2()
        {
            var e=Central.WM.KeyboardEventsArgs;
            switch(e.Key)
            {
                case Key.F5:
                    ///Grid.LoadItems();
                    e.Handled=true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled=true;
                    break;

                case Key.Home:
                    ///Grid.SetSelectToFirstRow();
                    e.Handled=true;
                    break;

                case Key.End:
                    ///Grid.SetSelectToLastRow();
                    e.Handled=true;
                    break;

                case Key.Escape:
                    ///Grid.SetSelectToLastRow();
                    e.Handled=true;
                    Close();
                    break;
            }
        }
      
        /// <summary>
        /// загрузка данных и отображение
        /// </summary>
        /// <param name="productionTaskId">id ПЗГА</param>
        /// <param name="applicationPositionId">id позиции заявки</param>
        /// <param name="goodsId">id изделия</param>
        /// <param name="categoryId">id категории изделия</param>
        /// <param name="processingTaskId">id ПЗПР</param>
        public void Edit(int productionTaskId,int applicationPositionId,int goodsId,int categoryId, int processingTaskId)
        {
            ProductionTaskId=productionTaskId;
            ProcessingTaskId=processingTaskId;
            ApplicationPositionId=applicationPositionId;
            GoodsId=goodsId;
            CategoryId=categoryId;
            LoadData();            
            Show();
        }

        public void EnableToolbar()
        {
            Toolbar.IsEnabled=true;
        }

        public void DisableToolbar()
        {
            Toolbar.IsEnabled=false;
        }

        /// <summary>
        /// Отображение вкладки с формой редактирования данных водителя
        /// </summary>
        public void Show()
        {
            string title=$"Позиция #{ApplicationPositionId}";
            TabName = $"shipmentposition_{ApplicationPositionId}";
            if(ApplicationPositionId == 0)
            {
                title="Позиция";
            }
            Central.WM.Show(TabName, title, true, "add", this);

        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        public void Close()
        {
            Central.WM.Close(TabName);
            if (!ReturnTabName.IsNullOrEmpty())
            {
                Central.WM.SetActive(ReturnTabName);
                ReturnTabName = "";
            }

            Destroy();
        }

        private void CloseButton_Click(object sender,RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender,RoutedEventArgs e)
        {
            Save();
        }

        private void HelpButton_Click(object sender,RoutedEventArgs e)
        {

        }
    }


}
