using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using NLog.LayoutRenderers;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Автосоздание отгрузки
    /// </summary>
    /// <author>Михеев И.С.</author>
    public partial class ShipmentAuto : UserControl
    {
        public ShipmentAuto()
        {
            InitializeComponent();

            SetButtonActive=false;
            CheckQuantityTimeout=new Common.Timeout(
                1,
                ()=>{
                    CheckQuantity();
                }
            );
            CheckQuantityTimeout.SetIntervalMs(300);

            InitGrid();
            InitGrid2();

            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);
            PreviewKeyDown += ProcessKeyboard;
        }

        private Dictionary<string, string> SelectedItem2;

        private Window Window { get; set; }

        private string _idts;
        public string IdTs
        {
            get => _idts;
            set
            {
                _idts = value;
                //DeleteButton.IsEnabled = CanDeletedPallet();
                Grid.Run();

            }
        }
       

        #region default functions

        /// <summary>
        /// Деструктор. Остановка вспомогательных процессов при закрытии вкладки
        /// </summary>
        public void Destroy()
        {

            //отправляем сообщение о закрытии фрейма
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Shipment",
                ReceiverName = "",
                SenderName = "Auto",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры гридов
            Grid.Destruct();
        }


        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
        }

        /// <summary>
        /// обработчик системы навигации по URL
        /// </summary>
        public void ProcessNavigation()
        {
        }

        private void ProcessKeyboard(object sender, KeyEventArgs e)
        {
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

        private void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/shipments/control/listing/avtosozdanie-otgruzki");
        }


        #endregion

        #region init grids

        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Задание на отгрузку",
                    Path = "NSTHET",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 85,
                    MaxWidth = 85,
                },
                new DataGridHelperColumn
                {
                    Header = "Покупатель",
                    Path = "NAMEPOK",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 200,
                    MaxWidth = 500,
                },
                new DataGridHelperColumn
                {
                    Header = "Грузополучатель",
                    Path = "NAMEGP",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 200,
                    MaxWidth = 500,
                },
                new DataGridHelperColumn
                {
                    Header = "№ заявки",
                    Path = "NUMBERORDER",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 70,
                    MaxWidth = 70,
                },
            };

            Grid.SetColumns(columns);
            Grid.SetSorting("NSTHET");
            Grid.Menu = new Dictionary<string, DataGridContextMenuItem>();

            Grid.OnLoadItems = LoadItems;
            Grid.OnSelectItem = selectedItem => { };

            Grid.Init();
            Grid.Run();
            Grid.Focus();
        }


        private void InitGrid2()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "№ загр.",
                    Path = "LOADORDER",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 45,
                    MaxWidth = 45,
                },
                new DataGridHelperColumn
                {
                    Header = "Артикул",
                    Path = "ARTIKUL",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 110,
                    MaxWidth = 110,
                },
                new DataGridHelperColumn
                {
                    Header = "Наименование",
                    Path = "NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 180,
                    MaxWidth = 180,
                },
                new DataGridHelperColumn
                {
                    Header = "Ограничение количества, шт",
                    Path = "QTYLIMIT",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 50,
                    MaxWidth = 50,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество в заявке",
                    Path = "QUANTITY",
                    Doc="Количество в исходной заявке, шт.",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 60,
                    MaxWidth = 60,
                },               
                new DataGridHelperColumn
                {
                    Header = "На складе всего",
                    Path = "INSTOCKQUANTITYTOTAL",
                    Doc="Общее количество на складе, шт.",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 75,
                    MaxWidth = 75,
                },
                new DataGridHelperColumn
                {
                    Header="Отгружено",
                    Path="SHIPPEDQUANTITY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 80,
                    MaxWidth = 80,
                },
                new DataGridHelperColumn
                {
                    Header = "В автоотгрузке",
                    Path = "QUANTITY2",
                    Doc="Количество, которое пойдет в новую отгрузку, шт.",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 80,
                    MaxWidth = 80,
                },
                new DataGridHelperColumn
                {
                    Header = "Задание на отгрузку",
                    Path = "NSTHET",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 70,
                    MaxWidth = 70,
                },

            };

            Grid2.SetColumns(columns);
            Grid2.SetSorting("NAME");
            Grid2.Menu = new Dictionary<string, DataGridContextMenuItem>();

            Grid2.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>{
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    // предупредим что есть пустые позиции
                    row => row.ContainsKey("QTY") && row["QTY"].ToInt() == 0 ? HColor.Yellow : DependencyProperty.UnsetValue
                }
            };

            //Grid2.OnLoadItems = LoadItems2;
            Grid2.OnSelectItem = selectedItem =>
            {
                SelectedItem2 = selectedItem;
                //AmountTextBox.Text = SelectedItem2["QTY"].ToInt().ToString();
                SetQuantity();
            };

            Grid2.Init();
            Grid2.Run();
            Grid2.Focus();
        }


        #endregion

        #region load data

        private int GetQuantity(Dictionary<string,string> row)
        {
            var result = 0;

            var stock = row.CheckGet("INSTOCKQUANTITYTOTAL").ToInt();
            var application = row.CheckGet("QUANTITY").ToInt();
            var shipped = row.CheckGet("SHIPPEDQUANTITY").ToInt();

            if(application - shipped > 0)
            {
                result = application - shipped;

                if(stock > 0)
                {
                    if(result > stock)
                    {
                        result = stock;
                    }
                }
            }

            /*
            if(application > 0)
            {
                result = application;
            }

            if(stock > 0)
            {
                if(result > stock)
                {
                    result = stock;
                }
            }
            */

            return result;
        }

        private int SetQuantity()
        {
            var result = 0;
            result=GetQuantity(Grid2.SelectedItem);
            AmountTextBox.Text = result.ToString();
            return result;
        }

        private async void LoadItems()
        {
            if (IdTs != null)
            {
                Grid.ShowSplash();
                Grid.ClearItems();

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "Shipment");
                q.Request.SetParam("Action", "GetAuto");

                q.Request.SetParam("ID_TS", IdTs);

                q.Request.Attempts = 3;

                await Task.Run(() => { q.DoQuery(); });


                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        if (result.ContainsKey("Operations"))
                        {
                            var ds = result["Operations"];
                            ds.Init();
                            Grid.UpdateItems(ds);
                        }

                        if (result.ContainsKey("Positions"))
                        {
                            var ds = result["Positions"];
                            ds.Init();
                            foreach(var row in ds.Items)
                            {
                                var v = GetQuantity(row);
                                row.CheckAdd("QUANTITY2",v.ToString());
                            }
                            Grid2.UpdateItems(ds);
                        }
                    }
                }
                else
                {
                    if (q.Answer.Error.Code != 7)
                    {
                        q.ProcessError();
                    }
                }

                Grid.HideSplash();
                // GridToolbar.IsEnabled = true;
            }
        }


        #endregion
        
        private bool SetButtonActive {get;set;}
        private void UpdateQuantity()
        {
            var newValue = AmountTextBox.Text.ToInt();

            if(Grid2.SelectedItem.Count > 0)
            {
                Grid2.SelectedItem.CheckAdd("QUANTITY2", newValue.ToString());
                Grid2.UpdateGrid();
            }
            SetButtonActive=false;
            SetButtonUpdate();
        }

        private void SetButtonUpdate()
        {
            if(SetButtonActive)
            {
                SetButton.Style=(Style)SetButton.TryFindResource("FButtonPrimary");
            }
            else
            {
                SetButton.Style=(Style)SetButton.TryFindResource("Button");
            }
        }

        private void Close_OnClick(object sender, RoutedEventArgs e)
        {
            Window?.Close();
        }

        private void Set_OnClick(object sender, RoutedEventArgs e)
        {
            UpdateQuantity();
        }

        private Common.Timeout CheckQuantityTimeout {get;set;}
        private void CheckQuantity()
        {
            var validationResult=true;

            //var newValue = AmountTextBox.Text.ToInt();
            //var oldValue=Grid2.SelectedItem.CheckGet("_QTY").ToInt();
            //var stockValue=Grid2.SelectedItem.CheckGet("INSTOCKQUANTITYTOTAL").ToInt();

            //if(newValue > 0)
            //{
            //    if(oldValue>0)
            //    {
            //        if(newValue > oldValue)
            //        {
            //            validationResult=false;
            //        }
            //    }

            //    {
            //        if(newValue > stockValue)
            //        {
            //            validationResult=false;
            //        }
            //    }               
            //}
            //else
            //{
            //    validationResult=false;
            //}

            var quantity = AmountTextBox.Text.ToInt();
            var result = 0;
            var stock = Grid2.SelectedItem.CheckGet("INSTOCKQUANTITYTOTAL").ToInt();
            var application = Grid2.SelectedItem.CheckGet("QUANTITY").ToInt();

            if(application > 0)
            {
                result = application;
            }

            if(stock > 0)
            {
                if(result > stock)
                {
                    result = stock;
                }
            }

            if(quantity> result)
            {
                validationResult = false;
            }

            if(validationResult)
            {
                SetButton.IsEnabled=true;
                SetButtonActive=true;
                SetButtonUpdate();                   
            }
            else
            {
                SetButton.IsEnabled=false;
                SetButtonActive=false;
                SetButtonUpdate();   
            }
        }
        

        /// <summary>
        /// Создание автоотгрузки
        /// </summary>
        /// <returns></returns>
        private bool Do()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Shipment");
            q.Request.SetParam("Action", "CreateAuto");

            q.Request.SetParam("Idts", IdTs);

            q.Request.SetParam("Operations", JsonConvert.SerializeObject(Grid.DataSet.Items));
            q.Request.SetParam("Positions", JsonConvert.SerializeObject(Grid2.DataSet.Items));


            //await Task.Run(() => { q.DoQuery(); });
            q.DoQuery();


            if (q.Answer.Status == 0)
            {
                DialogWindow.ShowDialog("Автоотгрузка создалась", "Автоотгрузка");

                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                if (result != null)
                {
                    var dataSet = ListDataSet.Create(result, "List");
                    var id = dataSet.GetFirstItemValueByKey().ToInt();
                    if (id != 0)
                    {
                        //отправляем сообщение гриду о необходимости обновить данные
                        Messenger.Default.Send(new ItemMessage
                        {
                            ReceiverGroup = "ShipmentControl",
                            ReceiverName = "ShipmentsList",
                            SenderName = "ShipmentAuto",
                            Action = "SelectById",
                            Message = $"{id}",
                        });

                       
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            return true;
        }


        private void Do_OnClick(object sender, RoutedEventArgs e)
        {
            if (Do())
            {
                //обновляем корневой грид    
                Messenger.Default.Send(new ItemMessage
                {
                    ReceiverGroup = "ShipmentControl",
                    ReceiverName = "TerminalList,DriverList,ShipmentList",
                    SenderName = "AutoShipmentView",
                    Action = "Refresh",
                });
            }

            Window?.Close();
        }



        public void Edit()
        {
            const string title = "Автосоздание отгрузки";

            var w = 800;
            var h = 440;

            Window = new Window
            {
                Title = title,
                Width = w + 17,
                Height = h + 40,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow,
                Content = new Frame
                {
                    Content = this,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                },
                Topmost = true,
            };

            Window.ShowDialog();
        }


        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
           ShowHelp();
        }

        private void AmountTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            //CheckQuantity();
            CheckQuantityTimeout.Restart();
        }
    }
}
