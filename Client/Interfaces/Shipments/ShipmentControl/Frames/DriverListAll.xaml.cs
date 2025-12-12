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

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Управление отгрузками, вкладка "Список всех водителей"
    /// </summary>
    /// <author>balchugov_dv</author>    
    public partial class DriverListAll:UserControl
    {
        public DriverListAll()
        {
            InitializeComponent();

            ReturnTabName="";
            TabName="transportdriverlist";
            ParentFrameType = ParentFrameTypeDefault.ShipmentsList;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this,ProcessMessages);

            //регистрация обработчика клавиатуры
            PreviewKeyDown+=ProcessKeyboard;

            InitGrid();
            SetDefaults();
        }

        public DriverListAll(bool usedForAnyFrameFlag, string receiverGroup, string receiverName, string parentFrame)
        {
            InitializeComponent();

            ReturnTabName = "";
            TabName = "transportdriverlist";

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            //регистрация обработчика клавиатуры
            PreviewKeyDown += ProcessKeyboard;

            InitGrid();
            SetDefaults();

            UsedForAnyFrameFlag = usedForAnyFrameFlag;
            _ReceiverGroup = receiverGroup;
            _ReceiverName = receiverName;
            _ParentFrame = parentFrame;
        }

        /// <summary>
        /// датасет, содержащий данные
        /// </summary>        
        public ListDataSet DriversAllDS { get; set; }

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string,string> SelectedItem { get; set; }

        /// <summary>
        /// Флаг того, что фрейм используется для выбора водителя в другом фрейме
        /// </summary>
        private bool UsedForAnyFrameFlag { get; set; }

        /// <summary>
        /// Группа получателей для отправки выбранного водителя по шине сообщений
        /// </summary>
        private string _ReceiverGroup { get; set; }

        /// <summary>
        /// Имя объекта получателя для отправки выбранного водителя по шине сообщений
        /// </summary>
        private string _ReceiverName { get; set; }

        /// <summary>
        /// Наименование таба, который вызвал этот таб
        /// </summary>
        private string _ParentFrame { get; set; }

        public enum ParentFrameTypeDefault
        {
            ShipmentsList,
            MoldedContainerShipmentList,
            ShipmentKshList
        }

        public ParentFrameTypeDefault ParentFrameType { get; set; }

        /// <summary>
        /// Таб для возврата
        /// Если определен, фокус будет возвращен этому табу
        /// </summary>
        public string ReturnTabName { get; set; }

        /// <summary>
        /// Имя фрейма
        /// Техническое имя для идентификации в системе WM
        /// </summary>
        public string TabName { get; set; }

        /// <summary>
        /// Окно с формой редактирования привязки
        /// </summary>
        public Window Window { get; set; }

        /// <summary>
        /// Деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="ShipmentControl",
                ReceiverName = "",
                SenderName = "DriverListAll",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            Grid.Destruct();

            if(!string.IsNullOrEmpty(ReturnTabName))
            {
                Central.WM.SetLayer(ReturnTabName);
                ReturnTabName="";
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            SearchText.Text=""; 
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
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=50,
                },

                new DataGridHelperColumn()
                {
                    Header="Водитель",
                    Path="DRIVERNAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width=255,
                },
                new DataGridHelperColumn()
                {
                    Header="Автомобиль",
                    Path="CAR",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width=215,
                },
                new DataGridHelperColumn()
                {
                    Header="Телефон",
                    Path="NOTE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width=90,                    
                },              
            };
            Grid.SetColumns(columns);

            // раскраска строк и текста
            Grid.RowStylers=new Dictionary<StylerTypeRef,StylerDelegate>()
            {
                // фон строки
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
                // шрифт строки
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

            Grid.SetSorting("SHIPMENTDATETIME",ListSortDirection.Ascending);
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

        public void Init()
        {
            Grid.Run();
        }

        /// <summary>
        /// обновление записей
        /// </summary>
        public async void LoadItems()
        {
            GridToolbar.IsEnabled=false;
            Grid.ShowSplash();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "TransportDriver");
            q.Request.SetParam("Action", "ListAll");

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    DriversAllDS = ListDataSet.Create(result, "Drivers");
                    Grid.UpdateItems(DriversAllDS);
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

                    //фильтрация строк

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

                }
            }
        }

        /// <summary>
        /// Вызов формы создания водителя
        /// </summary>
        public void Create()
        {
            var h=new Driver();
            //string tabName=GetTabName();
            //h.ReturnTabName=tabName;
            h.ReturnTabName="AddDriver";
            h.Create();
        }

        /// <summary>
        /// Вызов формы редактирования водителя
        /// </summary>
        public void Edit()
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
                var h=new Driver();
                h.ReturnTabName="AddDriver";
                h.Edit(id);
            }
        }        

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem"></param>
        public void UpdateActions(Dictionary<string,string> selectedItem)
        {
            EditButton.IsEnabled=false;
            SelectedItem = selectedItem;
            
            if(selectedItem.Count > 0)
            {
                int id = 0;
                if(selectedItem.ContainsKey("ID"))
                {
                    id=selectedItem["ID"].ToInt();
                }

                if(id!=0)
                {
                    EditButton.IsEnabled=true;
                }
            }
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
            if(m.ReceiverGroup.IndexOf("ShipmentControl") > -1)
            {
                if(m.ReceiverName.IndexOf("DriverListAll") > -1)
                {
                    switch(m.Action)
                    {
                        case "Refresh":

                            var itemId=m.Message.ToInt();
                            if(itemId!=0)
                            {
                                Grid.SelectedItem=new Dictionary<string,string>()
                                {
                                    { "ID", itemId.ToString() }
                                };
                            }

                            Grid.LoadItems();
                            break;
                    }
                }

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
        /// 
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

        
        public async void MarkDriverArrived()
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
                if (UsedForAnyFrameFlag)
                {
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = _ReceiverGroup,
                        ReceiverName = _ReceiverName,
                        SenderName = "DriverListAll",
                        Action = "SelectItem",
                        Message = id.ToString(),
                        ContextObject = SelectedItem,
                    });

                    Central.WM.SetActive(_ParentFrame, true);
                    Close();
                }
                else
                {
                    switch (ParentFrameType)
                    {
                        case ParentFrameTypeDefault.MoldedContainerShipmentList:

                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "MoldedContainerShipment",
                                ReceiverName = "MoldedContainerShipmentList",
                                SenderName = "DriverListAll",
                                Action = "SelectItem",
                                Message = id.ToString(),
                                ContextObject = SelectedItem,
                            });
                            Central.WM.RemoveTab("AddDriver");
                            Central.WM.SetLayer("main");
                            Destroy();

                            break;

                        case ParentFrameTypeDefault.ShipmentKshList:

                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "ShipmentKsh",
                                ReceiverName = "ShipmentKshList",
                                SenderName = "DriverListAll",
                                Action = "SelectItem",
                                Message = id.ToString(),
                                ContextObject = SelectedItem,
                            });
                            Central.WM.RemoveTab("AddDriver");
                            Central.WM.SetLayer("main");
                            Destroy();

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
        }

        public string GetTabName()
        {
            var result="";
            result=$"{TabName}";
            return result;
        }

        /// <summary>
        /// Отображение окна с формой редактирования
        /// </summary>
        public void Show()
        {
            string title = $"Водители";
            string tabName=GetTabName();
            Central.WM.AddTab(tabName,title,true,"add",this);
        }

        /// <summary>
        /// Дополнительный обработчик закрытия окна
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender,System.EventArgs e)
        {
            Destroy();
        }

        /// <summary>
        /// Сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.RemoveTab($"{TabName}");            

            if(Window!=null)
            {
                Window.Close();
            }

            Destroy();
        }
      
        /// <summary>
        /// Обработчик нажатия на кнопку отметки прибытия
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MarkArrivalButton_Click(object sender,RoutedEventArgs e)
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

        /// <summary>
        /// Обработчик нажатия на кнопку создания водителя
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateButton_Click(object sender,RoutedEventArgs e)
        {
            Create();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку редактирования
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditButton_Click(object sender,RoutedEventArgs e)
        {
            Edit();
        }
    }
}
