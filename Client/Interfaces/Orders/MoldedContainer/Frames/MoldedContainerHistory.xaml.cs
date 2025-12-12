using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Orders.MoldedContainer.Frames
{
    public partial class MoldedContainerHistory : ControlBase
    {
        public MoldedContainerHistory()
        {
            ModeMenu = null;
            OrderId = null;
            PositionId = null;
            ShipmentOrderId = null;
            ShipmentPositionId = null;
            
            InitializeComponent();

            FrameMode = 0;
            
            OnGetFrameTitle = () =>
            {
                var result = "";
                var mode = ModeMenu;

                if (mode == "order")
                {
                    result = $"История заказа {OrderId}";
                }

                if (mode == "position")
                {
                    result = $"История позиции заказа {PositionId}";
                }

                if (mode == "shipment")
                {
                    result = "История отгрузки ..";
                }

                if (mode == "shipment_order")
                {
                    result = $"История заказа в отгрузке {ShipmentOrderId}";
                }

                if (mode == "shipment_position")
                {
                    result = $"История позиции заказа в отгрузке {ShipmentPositionId}";
                }

                return result;
            };

            OnLoad = () =>
            {
                if (ModeMenu == "order")
                    OrderGridInit();
                    
                if (ModeMenu == "position")
                    PositionGridInit();
                    
                if (ModeMenu == "shipment")
                    ShipmentGridInit();
                    
                if (ModeMenu == "shipment_order")
                    ShipmentOrderGridInit();
                    
                if (ModeMenu == "shipment_position")
                    ShipmentPositionGridInit();
            };

            OnUnload = () =>
            {
                HistoryGrid.Destruct();
            };

            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "close_history",
                        Group = "main",
                        Enabled = true,
                        Title = "Закрыть",
                        Description = "Вернуться к списку заказов",
                        ButtonUse = true,
                        ButtonName = "CloseButton",
                        Action = Close,
                    });
                }
                
                Commander.Init(this);
            }
        }
        
        private string ModeMenu { get; set; }
        private string OrderId { get; set; }
        private string PositionId { get; set; }
        private string ShipmentOrderId { get; set; }
        private string ShipmentPositionId { get; set; }
        

        public void OrderGridInit()
        {
             var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "#",
                    Path = "_ROWNUMBER",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 8,
                },
                new DataGridHelperColumn
                {
                    Header = "Дата изменения",
                    Path = "AUDIT_DTTM",
                    ColumnType = ColumnTypeRef.DateTime,
                    Width2 = 10,
                    Format = "dd.MM.yyyy HH:mm",
                },
                new DataGridHelperColumn
                {
                    Header = "Пользователь",
                    Path = "NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 20,
                },
                new DataGridHelperColumn
                {
                    Header = "Дата отгрузки",
                    Path = "SHIPMENT_DATE",
                    ColumnType = ColumnTypeRef.DateTime,
                    Width2 = 10,
                    Format = "dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header = "Дата доставки",
                    Path = "DELIVERY_DATE",
                    ColumnType = ColumnTypeRef.DateTime,
                    Width2 = 10,
                    Format = "dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header = "Грузополучатель",
                    Path = "CONSIGNEE",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 30,
                },
                new DataGridHelperColumn
                {
                    Header = "Самовывоз",
                    Path = "SELF_DELIVERY",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 5,
                },
                new DataGridHelperColumn
                {
                    Header = "Уточнение даты",
                    Path = "DATE_CONFIRMATION",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 5,
                },
                new DataGridHelperColumn
                {
                    Header = "Ожидание оплаты",
                    Path = "PREPAY_CONFIRMATION",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 5,
                },
                new DataGridHelperColumn
                {
                    Header = "Примечание грузчику",
                    Path = "NOTE",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header = "Примечание кладовщику",
                    Path = "NOTE_GENERAL",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header = "Примечание логисту",
                    Path = "NOTE_LOGISTIC",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header = "ИД отгрузки",
                    Path = "SHIPMENT_ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 10,
                },
            };
            HistoryGrid.SetColumns(columns);
            HistoryGrid.SetPrimaryKey("ID");
            HistoryGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            HistoryGrid.Commands = Commander;
            HistoryGrid.QueryLoadItems = new RequestData()
            {
                Module = "Orders",
                Object = "MoldedContainer",
                Action = "ListHistoryOrders",
                AnswerSectionKey = "ITEMS",
                BeforeRequest = rd =>
                {
                    rd.Params = new Dictionary<string, string>()
                    {
                        { "ID", OrderId },
                    };
                },
            };
            
            HistoryGrid.Init();
        }
        
        public void PositionGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "#",
                    Path = "_ROWNUMBER",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Дата изменения",
                    Path = "AUDIT_DTTM",
                    ColumnType = ColumnTypeRef.DateTime,
                    Width2 = 10,
                    Format = "dd.MM.yyyy HH:mm",
                },
                new DataGridHelperColumn
                {
                  Header  = "Пользователь",
                  Path  = "NAME",
                  ColumnType = ColumnTypeRef.String,
                  Width2 = 20,
                },
                new DataGridHelperColumn
                {
                    Header = "Позиция отгрузки",
                    Path = "SHIP_ORDER",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Артикул",
                    Path = "SKU_CODE",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 15,
                },
                new DataGridHelperColumn
                {
                    Header = "Изделие",
                    Path = "PRODUCT_NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 30,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество",
                    Path = "QTY",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Цена без НДС",
                    Path = "PRICE_VAT_EXCLUDED",
                    ColumnType = ColumnTypeRef.Double,
                    Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header = "Цена с НДС",
                    Path = "PRICE",
                    ColumnType = ColumnTypeRef.Double,
                    Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header = "Фиксированная цена",
                    Path = "FIX_PRICE",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 5,
                },
                new DataGridHelperColumn
                {
                    Header = "Адрес доставки",
                    Path = "ADDRESS",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 30,
                },
                new DataGridHelperColumn
                {
                    Header = "Примечание кладовщику",
                    Path = "NOTE_GENERAL",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 20,
                },
                new DataGridHelperColumn
                {
                    Header = "ИД",
                    Path = "ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 5,
                },
            };
            HistoryGrid.SetColumns(columns);
            HistoryGrid.SetPrimaryKey("ID");
            HistoryGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            HistoryGrid.Commands = Commander;
            HistoryGrid.AutoUpdateInterval = 0;
            HistoryGrid.QueryLoadItems = new RequestData()
            {
                Module = "Orders",
                Object = "MoldedContainer",
                Action = "ListPositionHistory",
                AnswerSectionKey = "ITEMS",
                BeforeRequest = rd =>
                {
                    rd.Params = new Dictionary<string, string>()
                    {
                        { "ID", PositionId },
                    };
                },
            };
            
            
            HistoryGrid.Init();
        }

        public void ShipmentGridInit()
        {
            
        }

        public void ShipmentOrderGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "#",
                    Path = "_ROWNUMBER",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 8,
                },
                new DataGridHelperColumn
                {
                    Header = "Дата изменения",
                    Path = "AUDIT_DTTM",
                    ColumnType = ColumnTypeRef.DateTime,
                    Width2 = 10,
                    Format = "dd.MM.yyyy HH:mm",
                },
                new DataGridHelperColumn
                {
                    Header = "Дата доставки",
                    Path = "DELIVERY_DATE",
                    ColumnType = ColumnTypeRef.DateTime,
                    Width2 = 10,
                    Format = "dd.MM.yyyy",
                },
                new DataGridHelperColumn()
                {
                  Header = "Пользователь",
                  Path = "NAME",
                  ColumnType = ColumnTypeRef.String,
                  Width2 = 20,
                },
                new DataGridHelperColumn
                {
                    Header = "Покупатель",
                    Path = "CUSTOMER_NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 20,
                },
                new DataGridHelperColumn
                {
                    Header = "Грузополучатель",
                    Path = "CONSIGNEE",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 20,
                },
                new DataGridHelperColumn
                {
                    Header = "Самовывоз",
                    Path = "SELF_DELIVERY",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 5,
                },
                new DataGridHelperColumn
                {
                    Header = "Номер",
                    Path = "NUMBER_ORDER",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "ИД",
                    Path = "ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 5,
                },
            };

            HistoryGrid.SetColumns(columns);
            HistoryGrid.SetPrimaryKey("ID");
            HistoryGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            HistoryGrid.AutoUpdateInterval = 0;
            HistoryGrid.QueryLoadItems = new RequestData()
            {
                Module = "Orders",
                Object = "MoldedContainer",
                Action = "ListHistoryOrders",
                AnswerSectionKey = "ITEMS",
                BeforeRequest = rd =>
                {
                    rd.Params = new Dictionary<string, string>()
                    {
                        { "ID", ShipmentOrderId },
                    };
                },
            };


            HistoryGrid.Init();
        }

        public void ShipmentPositionGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Позиция отгрузки",
                    Path = "SHIP_ORDER",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                  Header = "Дата изменения",
                  Path = "AUDIT_DTTM",
                  ColumnType = ColumnTypeRef.DateTime,
                  Width2 = 10,
                  Format = "dd.MM.yyyy HH:mm",
                },
                new DataGridHelperColumn
                {
                  Header = "Пользователь",
                  Path = "NAME",
                  ColumnType = ColumnTypeRef.String,
                  Width2 = 20,
                },
                new DataGridHelperColumn
                {
                    Header = "Артикул",
                    Path = "SKU_CODE",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 15,
                },
                new DataGridHelperColumn
                {
                    Header = "Изделие",
                    Path = "PRODUCT_NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 30,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество",
                    Path = "QTY",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество под отгрузку",
                    Path = "PRODUCT_QTY",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество на складе",
                    Path = "TOTAL_PRODUCT_QTY",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Отгружено",
                    Path = "SHIPPED_QTY",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Цена без НДС",
                    Path = "PRICE_VAT_EXCLUDED",
                    ColumnType = ColumnTypeRef.Double,
                    Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header = "Цена с НДС",
                    Path = "PRICE",
                    ColumnType = ColumnTypeRef.Double,
                    Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header = "ПЗ",
                    Path = "TASK_EXISTS",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 5,
                },
                new DataGridHelperColumn
                {
                    Header = "Адрес доставки",
                    Path = "ADDRESS",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 30,
                },
                new DataGridHelperColumn
                {
                    Header = "Примечание кладовщику",
                    Path = "NOTE_GENERAL",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 20,
                },
                new DataGridHelperColumn
                {
                    Header = "ИД",
                    Path = "ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 5,
                },
            };
            HistoryGrid.SetColumns(columns);
            HistoryGrid.SetPrimaryKey("ID");
            HistoryGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            HistoryGrid.QueryLoadItems = new RequestData()
            {
                Module = "Orders",
                Object = "MoldedContainer",
                Action = "ListPositionHistory",
                AnswerSectionKey = "ITEMS",
                BeforeRequest = rd =>
                {
                    rd.Params = new Dictionary<string, string>()
                    {
                        { "ID", ShipmentPositionId },
                    };
                },
            };

            HistoryGrid.AutoUpdateInterval = 0;
            
            HistoryGrid.Init();
        }

        public void ShowHistory(string mode, string orderId = null, string positionId = null, string shipmentOrderId = null, string shipmentPositionId = null)
        {
            ModeMenu = mode;
            OrderId = orderId;
            PositionId = positionId;
            ShipmentOrderId = shipmentOrderId;
            ShipmentPositionId = shipmentPositionId;
            Show();
        }
        
        
    }
}