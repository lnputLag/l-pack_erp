using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Управление отгрузками, вкладка "Отчет"
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>2</version>
    /// <released>2021-12-07</released>     
    public partial class ShipmentsReport : UserControl
    {
        public ShipmentsReport()
        {
            InitializeComponent();

            AutoUpdateInterval=60*5;

            ProcessPermissions();
            SetDefaults();
            GridInit();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, _ProcessMessages);
            Central.Msg.Register(ProcessMessages);
        }

        public int AutoUpdateInterval { get; set; }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel("[erp]shipment_control");
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
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о фрейма
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="ShipmentControl",
                ReceiverName = "",
                SenderName = "ShipmentsReport",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
            Central.Msg.UnRegister(ProcessMessages);

            //останавливаем таймеры грида
            Grid.Destruct();
        }
        
        /// <summary>
        /// обработка сообщений
        /// </summary>
        /// <param name="message"></param>
        public void ProcessMessages(ItemMessage message)
        {
            if(message!=null)
            {
                if(
                    message.SenderName == "WindowManager"
                    && message.ReceiverName == "ShipmentsControl_Report"
                )
                {
                    switch (message.Action)
                    {
                        case "FocusGot":
                            Grid.ItemsAutoUpdate=true;
                            //Grid.LoadItems();
                            break;

                        case "FocusLost":
                            Grid.ItemsAutoUpdate=false;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        private void _ProcessMessages(ItemMessage m)
        {
            //Group ProductionTask
            if (m.ReceiverGroup.IndexOf("ShipmentControl") > -1)
            {
                if(m.ReceiverName.IndexOf("ShipmentList")>-1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            Grid.LoadItems();
                            break;
                    }
                }

                //окно установки причины опоздания
                //окно закрыто, активируем кнопку
                if(
                    m.SenderName== "LateReason"
                    && m.Action=="Closed"
                )
                {
                    SetReasonButton.IsEnabled=true;
                }
            }
        }
        
        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e=Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.F5:
                    Grid.LoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Home:
                    Grid.SetSelectToFirstRow();
                    e.Handled = true;
                    break;

                case Key.End:
                    Grid.SetSelectToLastRow();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// отображение статьи в справочной системе
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/shipments/control/report");
        }
        
        /// <summary>
        /// обработчик системы навигации по URL
        /// </summary>
        public void ProcessNavigation()
        {
        }

        /// <summary>
        /// инициализация грида
        /// </summary>
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
                        Path="ROWNNMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=35,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Покупатель",
                        Path="BUYER",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,
                        MaxWidth=150,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Водитель перевозчика",
                        Path="TRANSPORTERDRIVER",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,
                        MaxWidth=170,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Водитель погрузчика",
                        Path="FORKLIFTDRIVER",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,
                        MaxWidth=95,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ терминала",
                        Path="TERMINALNAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=40,
                        MaxWidth=40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Самовывоз",
                        Path="SELFSHIPMENT",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=40,
                        MaxWidth=40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Упаковка",
                        Path="PACKAGINGTITLE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=40,
                        MaxWidth=40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="c/уп",
                        Group="Поддонов",
                        Path="PALLETSWITHPACKAGING",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=35,
                        MaxWidth=35,
                    },
                    new DataGridHelperColumn
                    {
                        Header="б/уп",
                        Group="Поддонов",
                        Path="PALLETSWITHOUTPACKAGING",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=35,
                        MaxWidth=35,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дт/вр отгрузки",
                        Path="SHIPMENTDATE",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM HH:mm",
                        MinWidth=70,
                        MaxWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дт/вр первоначальной отгрузки",
                        Path="SHIPMENTFIRSTDATE",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM HH:mm",
                        MinWidth=70,
                        MaxWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дт/вр первоначальное плановое",
                        Path="PLANFIRSTDATE",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM HH:mm",
                        MinWidth=70,
                        MaxWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дт/вр планируемой готовности",
                        Path="COMPLETEDATE",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM HH:mm",
                        MinWidth=70,
                        MaxWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дт/вр ПЗ",
                        Path="PRODUCTIONTASKDATE",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM HH:mm",
                        MinWidth=70,
                        MaxWidth=70,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    if(!string.IsNullOrEmpty(row.CheckGet("PRODUCTIONTASKDATE")))
                                    {
                                        if(
                                            row.CheckGet("PRODUCTIONTASKDATEEXPIRED").ToInt()==1
                                        )
                                        {
                                            color = HColor.Red;
                                        }
                                        else
                                        {
                                            color = HColor.Green;
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
                    new DataGridHelperColumn
                    {
                        Header="Дт/вр водителя",
                        Path="DRIVERENTRANCEDATE",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM HH:mm",
                        MinWidth=70,
                        MaxWidth=70,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    if(!string.IsNullOrEmpty(row.CheckGet("DRIVERENTRANCEDATE")))
                                    {
                                        if(
                                            row.CheckGet("DRIVERENTRANCEDATEEXPIRED").ToInt()==1
                                        )
                                        {
                                            color = HColor.Red;
                                        }
                                        else
                                        {
                                            color = HColor.Green;
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
                        Header="Дт/вр терминал",
                        Path="SHIPMENTBINDINGDATE",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM HH:mm",
                        MinWidth=70,
                        MaxWidth=70,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    if(!string.IsNullOrEmpty(row.CheckGet("SHIPMENTBINDINGDATE")))
                                    {
                                        if(
                                            row.CheckGet("SHIPMENTBINDINGDATEEXPIRED").ToInt()==1
                                        )
                                        {
                                            color = HColor.Red;
                                        }
                                        else
                                        {
                                            color = HColor.Green;
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
                        Header="начало",
                        Group="Дт/вр. отгрузки",
                        Path="SHIPMENTSTARTDATE",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                        Format="dd.MM HH:mm",
                        MinWidth=70,
                        MaxWidth=70,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="окончание",
                        Group="Дт/вр. отгрузки",
                        Path="SHIPMENTFINISHDATE",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM HH:mm",
                        MinWidth=70,
                        MaxWidth=70,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="итог",
                        Group="Дт/вр. отгрузки",
                        Path="SHIPMENTTOTALDATE",
                        ColumnType=ColumnTypeRef.String,
                        
                        MinWidth=55,
                        MaxWidth=55,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Производство",
                        Path="REASONMASTER",
                        Group="Причина опоздания",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=80,
                        MaxWidth=150,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    if (
                                        row.ContainsKey("REASONMASTERCONDITION")
                                    )
                                    {
                                        if( row["REASONMASTERCONDITION"].ToInt() == 1 )
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
                        Header="СГП",
                        Path="REASONSTOCK",
                        Group="Причина опоздания",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=80,
                        MaxWidth=150,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    /*
                                        подкрашиваем желтым колонку примечание СГП
                                        только там, где есть только прямая вина СГП

                                    */    
                                    if(
                                        row.CheckGet("SHIPMENTBINDINGDATEEXPIRED").ToInt()==1
                                        && row.CheckGet("REASONSTOCKCONDITION").ToInt()==1
                                    )
                                    {
                                        color = HColor.Yellow;
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
                        Header="Самовывоз",
                        Path="FAULT_SELFSHIP_FLAG",
                        Group="Срыв",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=45,
                        MaxWidth=45,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Доставка",
                        Path="FAULT_DELIVERY_FLAG",
                        Group="Срыв",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=45,
                        MaxWidth=45,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Склад",
                        Path="FALUT_STOCK_FLAG",
                        Group="Срыв",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=45,
                        MaxWidth=45,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Производство",
                        Path="FAULT_PRODUCTION_FLAG",
                        Group="Срыв",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=45,
                        MaxWidth=45,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Превышение времени загрузки",
                        Path="REASON",                        
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=150,
                        MaxWidth=300,
                    },

                };
                Grid.SetColumns(columns);

                Grid.SearchText = SearchText;
                

                Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "PrintDriverBootCard",
                        new DataGridContextMenuItem()
                        {
                            Header="Загрузочная карта",
                            Action=()=>
                            {
                                PrintDriverBootCard();
                            }
                        }
                    },
                    {
                        "ShowWarehouseMap",
                        new DataGridContextMenuItem()
                        {
                            Header="Карта склада",
                            Action=()=>
                            {
                                ShowWarehouseMap();
                            }
                        }
                    },    
                    {
                        "SetReason",
                        new DataGridContextMenuItem()
                        {
                            Header="Причина опоздания",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                SetReason();
                            }
                        }
                    },
                    {
                        "ShipmentInfo",
                        new DataGridContextMenuItem()
                        {
                            Header="Информация об отгрузке",
                            Action=()=>
                            {
                                ShowShipmentInfo();
                            }
                        }
                    },
                    {
                        "LoadingOrder",
                        new DataGridContextMenuItem
                        {
                            Header="Порядок загрузки",
                            Action=()=>
                            {
                                LoadingOrderNew();
                            }
                        }
                    },

                };

                //при выборе строки в гриде, обновляются актуальные действия для записи
                Grid.OnSelectItem = selectedItem =>
                {
                      if (selectedItem.Count > 0)
                      {
                          UpdateActions(selectedItem);
                      }
                };


                //данные грида
                Grid.OnLoadItems = LoadItems;
                Grid.OnFilterItems = FilterItems;

                Grid.AutoUpdateInterval=AutoUpdateInterval;
                Grid.ItemsAutoUpdate=false;

                Grid.Init();
                Grid.Run();

                Grid.Focus();
            }
        }

        private void ShowShipmentInfo()
        {
            if (SelectedItem != null)
            {
                if (SelectedItem.ContainsKey("ID"))
                {
                    if (SelectedItem["ID"].ToInt() != 0)
                    {
                        var h = new ShipmentInformation();
                        h.Id = SelectedItem["ID"].ToInt();
                        h.Init();
                        h.Open();
                    }
                }
            }
        }

        /// <summary>
        /// Порядок загрузки
        /// </summary>
        private void LoadingOrderNew()
        {
            if(SelectedItem != null)
            {
                int id = SelectedItem.CheckGet("ID").ToInt();
                var shipment = new Shipment(id);
                shipment.ShowLoadingScheme(SelectedItem);
            }
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            FromDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
            ToDate.Text = DateTime.Now.ToString("dd.MM.yyyy");

            var list = new Dictionary<string, string>();
            list.Add("-1", "Все типы");
            list.Add("0", "Изделия");
            list.Add("1", "Бумага");
            Types.Items = list;
            Types.SelectedItem = list.FirstOrDefault((x) => x.Key == "-1");

            //SearchText.Text = "";
        }

        /// <summary>
        /// датасет, содержащий данные
        /// </summary>
        public ListDataSet ShipmentsDS { get; set; }

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        public Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// получение записей
        /// </summary>
        public async void LoadItems()
        {
            DisableControls();
            
            
            bool resume = true;

            if (resume)
            {
                var f = FromDate.Text.ToDateTime();
                var t = ToDate.Text.ToDateTime();
                if (DateTime.Compare(f, t) > 0)
                {
                    var msg = "Дата начала должна быть меньше или равна даты окончания.";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();
                    resume = false;
                }
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                var p = new Dictionary<string, string>();
                p.Add("FromDate", FromDate.Text);
                p.Add("ToDate", ToDate.Text);

                //FIXME: remove using LPackClientDataProvider
                await Task.Run(() =>
                {
                    q = _LPackClientDataProvider.DoQueryGetResult("Shipments", "Shipment", "ListReport", "Items", p);
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result!=null)
                    {
                        ShipmentsDS = ListDataSet.Create(result, "Items");
                        Grid.UpdateItems(ShipmentsDS);

                        if (Grid.Items != null)
                        {
                            foreach (var item in Grid.Items)
                            {
                                if (item.CheckGet("SHIPMENTTOTALDATE") != null && item.CheckGet("SHIPMENTTOTALDATE") != "")
                                {
                                    string Shours = "";
                                    double hours = ("0" + item.CheckGet("SHIPMENTTOTALDATE")).ToDouble() * 24;
                                    if (hours.ToInt() < 10)
                                    {
                                        Shours = "0" + hours.ToInt().ToString();
                                    }
                                    else
                                    {
                                        Shours = hours.ToInt().ToString();
                                    }
                                    hours = hours - hours.ToInt();

                                    string Sminutes = "";
                                    double minutes = hours * 60;
                                    if (minutes.ToInt() < 10)
                                    {
                                        Sminutes = "0" + minutes.ToInt().ToString();
                                    }
                                    else
                                    {
                                        Sminutes = minutes.ToInt().ToString();
                                    }
                                    minutes = minutes - minutes.ToInt();

                                    string Sseconds = "";
                                    double seconds = minutes * 60;
                                    if (seconds.ToInt() < 10)
                                    {
                                        Sseconds = "0" + seconds.ToInt().ToString();
                                    }
                                    else
                                    {
                                        Sseconds = seconds.ToInt().ToString();
                                    }

                                    string total = $"{Shours}:{Sminutes}:{Sseconds}";
                                    item.CheckAdd("SHIPMENTTOTALDATE", total);
                                }
                            }
                        }
                        Grid.UpdateItems();
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
        public void FilterItems()
        {
            if (Grid.GridItems != null)
            {
                if (Grid.GridItems.Count > 0)
                {
                    //обработка строк
                    foreach (var row in Grid.GridItems)
                    {
                        row.CheckAdd("PRODUCTIONTASKDATEEXPIRED", "0");
                        row.CheckAdd("DRIVERENTRANCEDATEEXPIRED", "0");
                        row.CheckAdd("SHIPMENTBINDINGDATEEXPIRED", "0");
                        row.CheckAdd("REASONMASTERCONDITION", "0");
                        row.CheckAdd("REASONSTOCKCONDITION", "0");
                        row.CheckAdd("BADFAILURECONDITION", "0");

                        // "плохой срыв": водитель приехал вовремя, а дт\вр пз просрочено.
                        bool badFailureDriver = false;
                        bool badFailureProduction = false;

                        if (row.ContainsKey("SHIPMENTDATE"))
                        {
                            if (!string.IsNullOrEmpty(row["SHIPMENTDATE"]))
                            {
                                /*
                                    SHIPMENTDATE		dttm
                                    PRODUCTIONTASKDATE	dttm_pz
                                    DRIVERENTRANCEDATE	dttm_driver
                                    SHIPMENTBINDINGDATE	dttm_terminal
                                 */
                                
                                var shipmentDate = row["SHIPMENTDATE"].ToDateTime("dd.MM HH:mm");

                                //время завершения ПЗ
                                if(!string.IsNullOrEmpty(row.CheckGet("PRODUCTIONTASKDATE")))
                                {
                                    if( 
                                        row.CheckGet("PRODUCTIONTASKDATE").ToDateTime() > row.CheckGet("SHIPMENTDATE").ToDateTime()
                                        && row.CheckGet("TRANSFERRED").ToInt()==0
                                    )
                                    {
                                        row["PRODUCTIONTASKDATEEXPIRED"] = "1";
                                            
                                        if (string.IsNullOrEmpty(row.CheckGet("REASONMASTER")))
                                        {
                                            row["REASONMASTERCONDITION"] = "1";
                                        }
                                    }
                                }


                                //время въезда водителя
                                if(!string.IsNullOrEmpty(row.CheckGet("DRIVERENTRANCEDATE")))
                                {
                                    if( 
                                        row.CheckGet("DRIVERENTRANCEDATE").ToDateTime() > row.CheckGet("SHIPMENTDATE").ToDateTime()
                                    )
                                    {
                                        row["DRIVERENTRANCEDATEEXPIRED"] = "1";
                                    }
                                }


                                //время постановки на терминал
                                //причина СГП
                                if(
                                    !string.IsNullOrEmpty(row.CheckGet("DRIVERENTRANCEDATE"))
                                    && !string.IsNullOrEmpty(row.CheckGet("SHIPMENTBINDINGDATE"))
                                    && !string.IsNullOrEmpty(row.CheckGet("PRODUCTIONTASKDATE"))
                                )
                                {
                                    var terminalDt=row.CheckGet("SHIPMENTBINDINGDATE").ToDateTime();
                                    var driverDt=row.CheckGet("DRIVERENTRANCEDATE").ToDateTime().AddMinutes(30);
                                    var taskDt=row.CheckGet("PRODUCTIONTASKDATE").ToDateTime().AddMinutes(30);                                        
                                    var shipmentDt=row.CheckGet("SHIPMENTDATE").ToDateTime();

                                    if(
                                        (terminalDt <= shipmentDt || row.CheckGet("TRANSFERRED").ToInt()==1)
                                        || terminalDt <= taskDt
                                        || terminalDt <= driverDt
                                    )
                                    {

                                    }
                                    else
                                    {
                                        row["SHIPMENTBINDINGDATEEXPIRED"] = "1";

                                        if (string.IsNullOrEmpty(row.CheckGet("REASONSTOCK")))
                                        {
                                            row["REASONSTOCKCONDITION"] = "1";
                                        }
                                    }
                                }    

                            }
                        }
                        /*
                            Дополнительное условие: красим колонки (Id и Buyer) в красный, если 
                            есть "плохие срывы": водитель приехал вовремя, а дт\вр пз просрочено.
                            */
                        if (badFailureProduction && badFailureDriver)
                        {
                            row["BADFAILURECONDITION"] = "1";
                        }

                    }

                    //фильтрация 

                    //тип
                    /*
                        list.Add("-1","Все типы");
                        list.Add("0","Изделия");
                        list.Add("1","Бумага");

                        SQL: ProductionTypeId=
                            2 бумага
                            * гофра
                     */
                    bool doFilteringByStatus = false;
                    int type = -1;
                    if (Types.SelectedItem.Key != null)
                    {
                        doFilteringByStatus = true;
                        type = Types.SelectedItem.Key.ToInt();
                    }


                    bool doFilteringExpired = false;
                    if (ExpiredOnlyCheckbox.IsChecked != null)
                    {
                        var b = (bool)ExpiredOnlyCheckbox.IsChecked;
                        if (b)
                        {
                            doFilteringExpired = true;
                        }
                    }

                    bool doFilteringFaultSelfship = false;
                    if ((bool)FaultSelfshipCheckbox.IsChecked)
                    {
                        doFilteringFaultSelfship = true;
                    }

                    bool doFilteringFaultDelivery = false;
                    if ((bool)FaultDeliveryCheckbox.IsChecked)
                    {
                        doFilteringFaultDelivery = true;
                    }

                    bool doFilteringFaultStock= false;
                    if ((bool)FaultStockCheckbox.IsChecked)
                    {
                        doFilteringFaultStock = true;
                    }

                    bool doFilteringFaultProduction= false;
                    if ((bool)FaultProductionCheckbox.IsChecked)
                    {
                        doFilteringFaultProduction = true;
                    }


                    if (
                        doFilteringByStatus
                        || doFilteringExpired
                        || doFilteringFaultSelfship
                        || doFilteringFaultDelivery
                        || doFilteringFaultStock
                        || doFilteringFaultProduction
                    )
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (var row in Grid.GridItems)
                        {
                            bool includeByStatus = false;
                            bool includeByExpiration = false;
                            bool includeByFaultSelfship = false;
                            bool includeByFaultDelivery = false;
                            bool includeByFaultStock = false;
                            bool includeByFaultProduction = false;

                            if (doFilteringByStatus)
                            {
                                switch (type)
                                {
                                    //-1 Все
                                    default:
                                        includeByStatus = true;
                                        break;

                                    //Изделия 
                                    case 0:
                                        if (row.ContainsKey("PRODUCTIONTYPEID"))
                                        {
                                            if (row["PRODUCTIONTYPEID"].ToInt() != 2)
                                            {
                                                includeByStatus = true;
                                            }
                                        }
                                        break;

                                    //Бумага
                                    case 1:
                                        if (row.ContainsKey("PRODUCTIONTYPEID"))
                                        {
                                            if (row["PRODUCTIONTYPEID"].ToInt() == 2)
                                            {
                                                includeByStatus = true;
                                            }
                                        }
                                        break;

                                }
                            }
                            else
                            {
                                includeByStatus = true;
                            }

                            if (doFilteringExpired)
                            {
                                if (row.ContainsKey("PRODUCTIONTASKDATEEXPIRED"))
                                {
                                    if (row["PRODUCTIONTASKDATEEXPIRED"].ToInt() == 1)
                                    {
                                        includeByExpiration = true;
                                    }
                                }
                                if (row.ContainsKey("DRIVERENTRANCEDATEEXPIRED"))
                                {
                                    if (row["DRIVERENTRANCEDATEEXPIRED"].ToInt() == 1)
                                    {
                                        includeByExpiration = true;
                                    }
                                }
                                if (row.ContainsKey("SHIPMENTBINDINGDATEEXPIRED"))
                                {
                                    if (row["SHIPMENTBINDINGDATEEXPIRED"].ToInt() == 1)
                                    {
                                        includeByExpiration = true;
                                    }
                                }
                            }
                            else
                            {
                                includeByExpiration = true;
                            }

                            if (doFilteringFaultSelfship)
                            {
                                if (row.CheckGet("FAULT_SELFSHIP_FLAG").ToInt() == 1)
                                {
                                    includeByFaultSelfship = true;
                                }
                            }
                            else
                            {
                                includeByFaultSelfship = true;
                            }

                            if (doFilteringFaultDelivery)
                            {
                                if (row.CheckGet("FAULT_DELIVERY_FLAG").ToInt() == 1)
                                {
                                    includeByFaultDelivery = true;
                                }
                            }
                            else
                            {
                                includeByFaultDelivery = true;
                            }

                            if (doFilteringFaultStock)
                            {
                                if (row.CheckGet("FALUT_STOCK_FLAG").ToInt() == 1)
                                {
                                    includeByFaultStock = true;
                                }
                            }
                            else
                            {
                                includeByFaultStock = true;
                            }

                            if (doFilteringFaultProduction)
                            {
                                if (row.CheckGet("FAULT_PRODUCTION_FLAG").ToInt() == 1)
                                {
                                    includeByFaultProduction = true;
                                }
                            }
                            else
                            {
                                includeByFaultProduction = true;
                            }

                            if (
                                includeByStatus
                                && includeByExpiration
                                && includeByFaultSelfship
                                && includeByFaultDelivery
                                && includeByFaultStock
                                && includeByFaultProduction
                            )
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
        public void UpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;
            SetReasonButton.IsEnabled=false;
            
            if (SelectedItem != null)
            {
                if (SelectedItem.ContainsKey("ID"))
                {
                    if (SelectedItem["ID"].ToInt() != 0)
                    {
                        SetReasonButton.IsEnabled=true;
                    }
                }
            }

            ProcessPermissions();
        }
        
        /// <summary>
        /// показать загрузочную карту (печатная форма)
        /// </summary>
        private void PrintDriverBootCard()
        {
            if(SelectedItem!=null)
            {
                var id=SelectedItem.CheckGet("ID").ToInt();
                var reporter = new ShipmentReport(id);
                reporter.PrintBootcard();
                reporter.PrintShipmenttask();
            }
        }

        /// <summary>
        /// показать карту склада (печатная форма)
        /// </summary>
        private void ShowWarehouseMap()
        {
            if(SelectedItem!=null)
            {
                var id=SelectedItem.CheckGet("ID").ToInt();
                var reporter = new ShipmentReport(id);
                reporter.PrintStockmap();    
            }
        }

        /// <summary>
        /// установка причины опоздания отгрузки
        /// </summary>
        public void SetReason()
        {
            if(SelectedItem!=null)
            {
                var id=SelectedItem.CheckGet("ID").ToInt();
                var h=new ShipmentReasonOfLateness();
                h.Edit(id);
                SetReasonButton.IsEnabled=false;
            }
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

        /// <summary>
        /// Обработчик нажатия на кнопку обновления
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        /// <summary>
        /// Обработчик нажатия на нкопку установки причин опоздания
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetReason_Click(object sender, RoutedEventArgs e)
        {
            SetReason();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку экспорта в Excel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }

        /// <summary>
        /// Обработчик выбора значения в выпадающем списке типов изделий
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void Types_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        /// <summary>
        /// Обработчик отметки чекбокса выбора только опоздавших
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExpiredOnlyCheckbox_Click(object sender, RoutedEventArgs e)
        {
            Grid.UpdateItems();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку справки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void FaultSelfshipCheckbox_Click(object sender, RoutedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void FaultDeliveryCheckbox_Click(object sender, RoutedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void FaultStockCheckbox_Click(object sender, RoutedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void FaultProductionCheckbox_Click(object sender, RoutedEventArgs e)
        {
            Grid.UpdateItems();
        }
    }

}
