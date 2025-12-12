using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Управление отгрузками, вкладка "Список ожидаемых водителей"
    /// </summary>
    /// <author>balchugov_dv</author>    
    public partial class DriverListExpected:UserControl
    {
        /// <summary>
        /// Инициализация
        /// </summary>
        public DriverListExpected()
        {
            InitializeComponent();

            TransportId=0;
            SetDateTime=false;
            ParentFrameType = ParentFrameTypeDefault.ShipmentsList;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this,ProcessMessages);

            //регистрация обработчика клавиатуры
            PreviewKeyDown+=ProcessKeyboard;

            InitGrid();
            SetDefaults();
        }

        /// <summary>
        /// датасет, содержащий данные
        /// </summary>        
        public ListDataSet ItemsDS { get; set; }

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string,string> SelectedItem { get; set; }

        /// <summary>
        /// Идентификатор отгрузки
        /// </summary>
        public int TransportId { get;set;}

        /// <summary>
        /// Флаг необходимости установки даты и времени отгрузки
        /// если поднят будет показано окно выбора даты и времени отгрузки
        /// </summary>
        public bool SetDateTime { get; set; }

        /// <summary>
        /// Тип отгрузки
        /// </summary>
        public int ShipmentType { get; set; }

        public enum ParentFrameTypeDefault 
        {
            ShipmentsList,
            MoldedContainerShipmentList,
            ShipmentKshList
        }

        public ParentFrameTypeDefault ParentFrameType { get; set; }

        /// <summary>
        /// Деструктор, завершает все вспомогательные процессы
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="ShipmentControl",
                ReceiverName = "",
                SenderName = "DriverListExpected",
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
            SearchText.Text=""; 
            
            //значения полей по умолчанию
            {
                var list = new Dictionary<string,string>();
                list.Add("-1","Все типы");
                list.Add("-2", "СГП");
                list.Add("0","Изделия");
                list.Add("2","Бумага");
                list.Add("5", "СОХ");
                list.Add("7", "Литая тара");
                Types.Items=list;
                Types.SelectedItem=list.FirstOrDefault((x)=>x.Key=="-1");                                
            }
        }

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        public void InitGrid()
        {
            //список колонок грида
            var columns = new List<DataGridHelperColumn>()
            {
                new DataGridHelperColumn()
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width=45,
                },
                new DataGridHelperColumn()
                {
                    Header="ИД отгрузки",
                    Path="SHIPMENTID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width=50,
                },
                new DataGridHelperColumn()
                {
                    Header="Дт/вр отгрузки",
                    Path="SHIPMENTDATETIME",
                    ColumnType=ColumnTypeRef.DateTime,
                    Format="dd.MM HH:mm",
                    Width=70,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            (Dictionary<string, string> row) =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                //перенесена на другой день
                                if (row.ContainsKey("UNSHIPPED"))
                                {
                                    if( row["UNSHIPPED"].ToInt() == 1 )
                                    {
                                        color = HColor.Orange;
                                    }
                                }

                                //опоздавшая
                                if (row.ContainsKey("LATE"))
                                {
                                    if( row["LATE"].ToInt() == 1 )
                                    {
                                        color = HColor.Yellow;
                                    }
                                }


                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        },
                    },
                },
                new DataGridHelperColumn()
                {
                    Header="Водитель",
                    Path="DRIVERNAME",
                    ColumnType=ColumnTypeRef.String,
                    Width=220,
                },
                new DataGridHelperColumn()
                {
                    Header="Автомобиль",
                    Path="CAR",
                    ColumnType=ColumnTypeRef.String,
                    Width=200,
                },
                new DataGridHelperColumn()
                {
                    Header="Телефон",
                    Path="NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Width=90,                       
                },
                new DataGridHelperColumn()
                {
                    Header="Покупатель",
                    Path="BUYER",
                    ColumnType=ColumnTypeRef.String,
                    Width=180,
                },
                new DataGridHelperColumn()
                {
                    Header="Тип продукции",
                    Path="PRODUCTIONTYPE",
                    ColumnType=ColumnTypeRef.String,
                    Width=100,
                },
                new DataGridHelperColumn()
                {
                    Header="ИД водителя",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width=100,
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


                        //отгруженные, на терминале
                        if (row.ContainsKey("DLDTTMENTRY"))
                        {
                            if(!string.IsNullOrEmpty(row["DLDTTMENTRY"]))
                            {
                                color = HColor.Green;
                            }
                        }

                        //отгрузка запрещена
                        if (row.ContainsKey("FINISHED"))
                        {
                            if(row["FINISHED"].ToInt() == 0)
                            {
                                color = HColor.Blue;
                            }
                        }


                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },

                {
                    StylerTypeRef.ForegroundColor,
                    (Dictionary<string, string> row) =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";


                        //на терминале
                        if (row.ContainsKey("ATTERMINAL"))
                        {
                            if(row["ATTERMINAL"].ToInt() == 1)
                            {
                                color = HColor.BlueFG;
                            }
                        }

                        //не отгружено, установлена цена
                        if(row.ContainsKey("FINISHED") && row.ContainsKey("PRICEIS"))
                        {
                            if(row["FINISHED"].ToInt() == 0)
                            {
                                if(row["PRICEIS"].ToInt() == 1)
                                {
                                    color=HColor.GreenFG;
                                }
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


            Grid.SetSorting("_ROWNUMBER",ListSortDirection.Ascending);
            Grid.SearchText=SearchText;
            Grid.Init();

            Grid.Menu=new Dictionary<string,DataGridContextMenuItem>()
            {
                { "Select", new DataGridContextMenuItem(){
                    Header="Отметить",
                    Action=()=>
                    {
                        MarkDriverArrived();
                    }
                }},
                { "s1", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                { "EditDriver", new DataGridContextMenuItem(){
                    Header="Изменить",
                    Action=()=>
                    {
                        EditDriver();
                    }
                }},
            };


            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem=(Dictionary<string,string> selectedItem) =>
            {
                if(selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };

            
            //двойной клик на строке откроет форму просмотра
            Grid.OnDblClick=(Dictionary<string,string> selectedItem) =>
            {
                MarkDriverArrived();
            };
            

            //данные грида
            Grid.OnLoadItems=LoadItems;
            Grid.OnFilterItems=FilterItems;
            Grid.Run();

            //фокус ввода           
            Grid.Focus();
        }

        /// <summary>
        /// обновление записей
        /// </summary>
        public void LoadItems()
        {
            GridToolbar.IsEnabled=false;
            Grid.ShowSplash();

            var q = new LPackClientQuery();

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

            var p = new Dictionary<string,string>();

            int productionTypeId = Types.SelectedItem.Key.ToInt();
            p.Add("ProductionTypeId", $"{productionTypeId}");

            if (ParentFrameType == ParentFrameTypeDefault.ShipmentKshList)
            {
                p.Add("FACTORY_ID", "2");
            }
            else
            {
                p.Add("FACTORY_ID", "1");
            }

            //FIXME: remove using LPackClientDataProvider
            //await Task.Run(() =>
            //{
            //    q = _LPackClientDataProvider.DoQueryGetResult("Shipments", "TransportDriver", "ListExpected", "", p);
            //});
            q = _LPackClientDataProvider.DoQueryGetResult("Shipments", "TransportDriver", "ListExpected", "", p);

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    ItemsDS = ListDataSet.Create(result, "Drivers");
                    Grid.UpdateItems(ItemsDS);
                }
            }
            
            GridToolbar.IsEnabled=true;
            Grid.HideSplash();            
        }

        /// <summary>
        /// фильтрация записей
        /// </summary>
        public void FilterItems()
        {
            if(Grid.GridItems!=null)
            {
                if(Grid.GridItems.Count>0)
                {
                    //обработка строк
                    foreach(Dictionary<string,string> row in Grid.GridItems)
                    {
                        //телефон
                        {
                            if(row.ContainsKey("NOTE"))
                            {
                                row["NOTE"]=DataFormatter.CellPhone(row["NOTE"]);
                            }
                        }
                    }

                    //фильтрация строк

                    //тип
                    /*
                        list.Add("-1","Все типы");
                        list.Add("0","Изделия");
                        list.Add("1","Бумага");

                        SQL: ProductionTypeId=
                            2 бумага
                            * гофра
                     */
                    bool doFilteringByType = false;

                    int type = -1;
                    if (Types.SelectedItem.Key != null)
                    {
                        doFilteringByType = true;
                        type = Types.SelectedItem.Key.ToInt();
                    }

                    if (doFilteringByType)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (var row in Grid.GridItems)
                        {
                            bool includeByType = false;
                            if (doFilteringByType)
                            {
                                includeByType = false;
                                switch (type)
                                {
                                    //-1 Все
                                    default:
                                        includeByType = true;
                                        break;

                                    case -2:
                                        if (row.ContainsKey("PRODUCTIONTYPEID"))
                                        {
                                            if (row["PRODUCTIONTYPEID"].ToInt() == 0 || row["PRODUCTIONTYPEID"].ToInt() == 1 
                                                || row["PRODUCTIONTYPEID"].ToInt() == 5
                                                || row["PRODUCTIONTYPEID"].ToInt() == 2)
                                            {
                                                includeByType = true;
                                            }
                                        }
                                        break;

                                    //Изделия 
                                    case 0:
                                        if (row.ContainsKey("PRODUCTIONTYPEID"))
                                        {
                                            if (row["PRODUCTIONTYPEID"].ToInt() == 0 || row["PRODUCTIONTYPEID"].ToInt() == 1)
                                            {
                                                includeByType = true;
                                            }
                                        }
                                        break;

                                    //Бумага
                                    case 2:
                                        if (row.ContainsKey("PRODUCTIONTYPEID"))
                                        {
                                            if (row["PRODUCTIONTYPEID"].ToInt() == 2)
                                            {
                                                includeByType = true;
                                            }
                                        }
                                        break;

                                    //Литая тара
                                    case 7:
                                        if (row.ContainsKey("PRODUCTIONTYPEID"))
                                        {
                                            if (row["PRODUCTIONTYPEID"].ToInt() == 7)
                                            {
                                                includeByType = true;
                                            }
                                        }
                                        break;

                                    //СОХ
                                    case 5:
                                        if (row.ContainsKey("PRODUCTIONTYPEID"))
                                        {
                                            if (row["PRODUCTIONTYPEID"].ToInt() == 5)
                                            {
                                                includeByType = true;
                                            }
                                        }
                                        break;

                                }
                            }

                            if (includeByType)
                            {
                                items.Add(row);
                            }

                        }
                        Grid.GridItems = items;
                    }
                }
            }
        }

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem"></param>
        public void UpdateActions(Dictionary<string,string> selectedItem)
        {
            SelectedItem=selectedItem;

            if(SelectedItem != null)
            {
                if(SelectedItem.ContainsKey("ID"))
                {
                    var selectedId = SelectedItem["ID"].ToInt();
                    if (selectedId != 0)
                    {
                        TransportId = selectedId;
                    }
                }
            }
        }

        private void Types_SelectedItemChanged(DependencyObject d,DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
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
        /// <param name="m">Сообщение</param>
        private void ProcessMessages(ItemMessage m)
        {
            //Group ShipmentControl
            if (m.ReceiverGroup.IndexOf("ShipmentControl") > -1)
            {
                if(m.ReceiverName.IndexOf("DriverListExpected")>-1)
                {
                    switch(m.Action)
                    {
                        case "Refresh":
                            Grid.LoadItems();
                            break;
                    }
                }
                
                /*
                if(m.ReceiverName=="DriverListExpected")
                {
                    switch (m.Action)
                    {
                        case "Save":
                            TransportId=m.Message.ToInt();
                            SetMarkArrived();
                            break;
                    }
                }
                */
                
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

        /// <summary>
        /// Получение ID выбранной звписи
        /// </summary>
        /// <returns></returns>
        public int GetSelectedItemId()
        {
            var result=0;

            if(SelectedItem!=null)
            {
                if(SelectedItem.ContainsKey("ID"))
                {
                    var id=SelectedItem["ID"].ToInt();

                    if(id!=0)
                    {
                        result=id;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        public void MarkDriverArrived()
        {
            bool resume=true;
            int id=0;

            if(resume)
            {
                if(SelectedItem!=null)
                {
                    id=SelectedItem.CheckGet("ID").ToInt();
                }
                if(id==0)
                {
                    resume=false;
                }
            }

            if(resume)
            {
                switch (ParentFrameType)
                {
                     case ParentFrameTypeDefault.MoldedContainerShipmentList:

                        Messenger.Default.Send(new ItemMessage()
                        {
                            ReceiverGroup = "MoldedContainerShipment",
                            ReceiverName = "MoldedContainerShipmentList",
                            SenderName = "DriverListExpected",
                            Action = "SelectItem",
                            Message = id.ToString(),
                            ContextObject = SelectedItem,
                        });
                        Central.WM.SetActive("MoldedContainerShipmentList", true);
                        Hide();

                        break;

                    case ParentFrameTypeDefault.ShipmentKshList:

                        Messenger.Default.Send(new ItemMessage()
                        {
                            ReceiverGroup = "ShipmentKsh",
                            ReceiverName = "ShipmentKshList",
                            SenderName = "DriverListExpected",
                            Action = "SelectItem",
                            Message = id.ToString(),
                            ContextObject = SelectedItem,
                        });
                        Central.WM.SetActive("ShipmentKshList", true);
                        Hide();

                        break;

                    case ParentFrameTypeDefault.ShipmentsList:
                    default:

                        Messenger.Default.Send(new ItemMessage()
                        {
                            ReceiverGroup = "ShipmentControl",
                            ReceiverName = "DriverList",
                            SenderName = "DriverListAll",
                            Action = "SelectItem",
                            Message = id.ToString(),
                            ContextObject = SelectedItem,
                        });

                        break;
                }
            }
        }

        /// <summary>
        /// Вызов формы редактирования водителя
        /// </summary>
        public void EditDriver()
        {
            bool resume = true;
            int id = 0;

            if (resume)
            {
                if (SelectedItem != null)
                {
                    id = SelectedItem.CheckGet("ID").ToInt();
                }
                if (id == 0)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var h = new Driver();
                h.ReturnTabName = "AddDriver";
                h.Edit(id);
            }
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        public void Hide()
        {
            Central.WM.RemoveTab("AddDriver");
            Central.WM.SetLayer("main");
            Destroy();
        }
      
        /// <summary>
        /// Обработчик нажатия на кнопку отметки водителя
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectButton_Click(object sender,RoutedEventArgs e)
        {
            MarkDriverArrived();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку обновления
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshButton_Click(object sender,RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            EditDriver();
        }
    }
}
