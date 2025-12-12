using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Messages;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Service.Printing;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xceed.Wpf.AvalonDock.Converters;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Интерфейс для отображения информации об отгрузках
    /// <author>eletskikh_ya</author>
    /// </summary>
    public partial class ShipmentInformation : ControlBase
    {
        public ShipmentInformation()
        {
            InitializeComponent();
            Id = 0;

            ControlTitle = "Отгрузка";

            OnLoad = () =>
            {
                ShipmentGridInit();
                ShipmentGridInfoInit();
                SetDefaults();
            };

            OnUnload = () =>
            {
                ShipmentGrid.Destruct();
                ShipmentGridInfo.Destruct();
            };

            OnFocusGot = () =>
            {
                ShipmentGrid.ItemsAutoUpdate=true;               
                ShipmentGrid.Run();
            };

            OnFocusLost = () => 
            {
                ShipmentGrid.ItemsAutoUpdate = false;
            };
        }

        public int Id { get; set; }

        public void Init()
        {
            ControlName=$"{ControlName}_{Id}";
        }

        public void Open()
        {
            Central.WM.AddTab(ControlName, $"{ControlTitle} #{Id}", true, "add", this);
        }

        public void Close()
        {
            Central.WM.Close(ControlName);
        }

        private void ShipmentGridInit()
        {
            {
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Номер",
                        Path="POSITIONNUM",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 8
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Path="VENDORCODE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=17,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="PRODUCTNAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Path="STATUS",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Path="QTY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="На складе",
                        Path="QTY_STOCK",
                        Doc="Количество на складе, шт.",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отгружено",
                        Path="QTY_SHIPPED",
                        Doc="Количество отгружено, шт.",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Станок",
                        Path="NAME_ST",
                        Doc="Станок",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус ПЗ",
                        Path="PZ_STATUS",
                        Doc="Статус ПЗ",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата начала ПЗ",
                        Path="DT_START",
                        Doc="Дата начала ПЗ",
                        ColumnType=ColumnTypeRef.String,
                        Width2=14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата окончания ПЗ",
                        Path="DT_END",
                        Doc="Дата окончания ПЗ",
                        ColumnType=ColumnTypeRef.String,
                        Width2=16,
                    },
                    new DataGridHelperColumn
                    {
                        Header="",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                };

                ShipmentGrid.SetColumns(columns);
                ShipmentGrid.SetPrimaryKey("POSITIONNUM");
                ShipmentGrid.SetSorting("_ROWNUMBER");
                ShipmentGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
                ShipmentGrid.OnSelectItem = selectedItem =>
                {
                    ShipmentGridInfoLoad();
                };            
                ShipmentGrid.OnLoadItems = ShipmentGridLoad;
                ShipmentGrid.Init();
            }
        }

        private void ShipmentGridInfoInit()
        {
            {
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Номер поддона",
                        Path="NUM",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Path="QTY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Path="PLACE",
                        Doc="Статус",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Станок",
                        Path="STANOK",
                        Doc="Статус",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата создания",
                        Path="DTTM_CREATED",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата оприходования",
                        Path="DTTM_PRIHOD",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 14,
                    },                  
                    new DataGridHelperColumn
                    {
                        Header="Дата сканирования на сигноде",
                        Path="DTTM_SD",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата списания в отгрузку",
                        Path="DTTM_RASHOD",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Оприходован",
                        Path="RECEIVED_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                };
                ShipmentGridInfo.SetColumns(columns);
                ShipmentGridInfo.SetPrimaryKey("NUM");
                ShipmentGridInfo.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
                ShipmentGridInfo.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
                ShipmentGridInfo.ItemsAutoUpdate = false;
                ShipmentGridInfo.OnLoadItems = ShipmentGridInfoLoad;
                ShipmentGridInfo.OnFilterItems = () =>
                {
                    if (ShipmentGridInfo.GridItems != null && ShipmentGridInfo.GridItems.Count > 0)
                    {
                        var items = new List<Dictionary<string, string>>();
                        if (ShowUnreceivedCheckbox.IsChecked == true)
                        {
                            items = ShipmentGridInfo.GridItems;
                        }
                        else
                        {
                            items = ShipmentGridInfo.GridItems.Where(x => x.CheckGet("RECEIVED_FLAG").ToInt() > 0).ToList();
                        }
                        ShipmentGridInfo.GridItems = items;
                    }
                };
                ShipmentGridInfo.Init();
            }
        }

        private void SetDefaults()
        {
            
        }

        public void ProcessCommand(string command, ItemMessage m=null)
        {
            command=command.ClearCommand();
            if(!command.IsNullOrEmpty())
            {
                switch(command)
                {
                    case "refresh":
                    {
                        ShipmentGrid.LoadItems();
                        if(m!=null)
                        {
                            var id=m.Message.ToString();
                            if(!id.IsNullOrEmpty())
                            {
                                ShipmentGrid.SelectRowByKey(id);
                            }
                        }
                    }
                        break;

                    case "close":
                    {
                        Close();
                    }
                        break;
                }
            }
        }

        private void ShipmentGridInfoLoad()
        {
            var resume=true;

            var goodsId=ShipmentGrid.SelectedItem.CheckGet("ID2").ToInt();
            var applicationId=ShipmentGrid.SelectedItem.CheckGet("IDORDERDATES").ToInt();
            if(resume)
            {
                if(goodsId == 0 || applicationId == 0)
                {
                    resume=false;
                }
            }

            if(resume)
            {
                {
                    var p = new Dictionary<string, string>();
                    p.Add("ID2", goodsId.ToString());
                    p.Add("IDORDERDATES", applicationId.ToString());

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Shipments");
                    q.Request.SetParam("Object", "Position");
                    q.Request.SetParam("Action", "ListPalletsByOrderDate");
                    q.Request.SetParams(p);

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            ShipmentGridInfo.UpdateItems(ds);
                        }
                    }
                }
            }
        }

        private async void ShipmentGridLoad()
        {
            ShipmentGrid.ShowSplash();

            var p = new Dictionary<string, string>();

            p.Add("SHIPMENT_ID", Id.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Position");
            q.Request.SetParam("Action", "ListSimple");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    ShipmentGrid.UpdateItems(ds);
                }
            }

            ShipmentGrid.HideSplash();
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

        private async void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            var p = new Dictionary<string, string>();

            p.Add("SHIPMENT_ID", Id.ToString());
            p.Add("FORMAT", "pdf");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Position");
            q.Request.SetParam("Action", "GetShipmentReport");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                if (q.Answer.Type == LPackClientAnswer.AnswerTypeRef.File)
                {
                    var printHelper = new PrintHelper();
                    printHelper.PrintingProfile = PrintingSettings.DocumentPrinter.ProfileName;
                    printHelper.PrintingCopies = 1;
                    printHelper.PrintingLandscape = true;
                    printHelper.Init();
                    var printingResult = printHelper.StartPrinting(q.Answer.DownloadFilePath);
                    printHelper.Dispose();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        private void ShowUnreceivedCheckbox_Click(object sender, RoutedEventArgs e)
        {
            ShipmentGridInfo.UpdateItems();
        }
    }
}
