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

namespace Client.Interfaces.Production
{
    /// <summary>
    /// выбор сырьевой группы из списка
    /// интерфейс ручного раскроя
    /// </summary>
    /// <author>balchugov_dv</author>    
    public partial class SelectPaper:UserControl
    {
        public SelectPaper()
        {
            InitializeComponent();

            SelectedItemId=0;
            LayerId=0;
            ProfileId=0;
            Format=0;
            PaperId=0;
            FactoryId = 1;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this,ProcessMessages);

            //регистрация обработчика клавиатуры
            PreviewKeyDown+=ProcessKeyboard;

            PaperGridInit();
            SetDefaults();
        }
     

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string,string> SelectedItem { get; set; }

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        public int SelectedItemId { get; set; }
        /// <summary>
        /// id слоя, которому предназначается бумага
        /// он вернется назад при положительном исходе
        /// </summary>
        public int LayerId { get; set; }
        public int ProfileId { get; set; }
        public int Format { get; set; }
        /// <summary>
        /// id сырьевой группы
        /// </summary>
        public int PaperId { get; set; }
        /// <summary>
        /// Идентификатор производственной площадки, на которой выполняется ПЗГА
        /// </summary>
        public int FactoryId;

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            //Group ProductionTask
            if(m.ReceiverGroup.IndexOf("ProductionTask") > -1)
            {
                if(m.ReceiverName.IndexOf("SelectPaper")>-1)
                {
                    switch(m.Action)
                    {
                        case "Refresh":
                        { 
                        }
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
            }
        }

        /// <summary>
        /// отображение статьи в справочной системе
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/production/creating_tasks/cutting_manual/select_paper");
        }

        /// <summary>
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="ProductionTask",
                ReceiverName = "",
                SenderName = "SelectPaper",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            PaperGrid.Destruct();
        }

        public void SetDefaults()
        {
            PaperSearchText.Text=""; 
        }

        public void PaperGridInit()
        {
            //список колонок грида
            var columns = new List<DataGridHelperColumn>()
            {
                new DataGridHelperColumn()
                {
                    Header="#",
                    Path="ID",
                    Doc="",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=35,
                },
                new DataGridHelperColumn()
                {
                    Header="Название",
                    Doc="",
                    Path="GROUP_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=130,
                },
                new DataGridHelperColumn()
                {
                    Header="Плотность",
                    Doc="плотность, г/кв .м.",
                    Path="DENSITY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=60,
                },
               
                new DataGridHelperColumn()
                {
                    Header="склад",
                    Doc="остаток на складе, кг",
                    Path="BALANCE_STOCK",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=50,
                },
                new DataGridHelperColumn()
                {
                    Header="склад Z",
                    Doc="остаток на складе Z, кг",
                    Path="BALANCE_STOCK_Z",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=50,
                },
                new DataGridHelperColumn()
                {
                    Header="БДМ",
                    Doc="количество в заданиях БДМ, кг",
                    Path="BALANCE_PM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=50,
                },
                new DataGridHelperColumn()
                {
                    Header="Код цвета",
                    Doc="1 - белая, 2 - бурая, 3 - крашенная",
                    Path="COLOR",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            PaperGrid.SetColumns(columns);


            PaperGrid.SetSorting("SHIPMENTDATETIME",ListSortDirection.Ascending);
            PaperGrid.SearchText=PaperSearchText;
            PaperGrid.Init();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            PaperGrid.OnSelectItem=(Dictionary<string,string> selectedItem) =>
            {
                if(selectedItem.Count > 0)
                {
                    PaperGridUpdateActions(selectedItem);
                }
            };

            //двойной клик на строке осуществит выбор данной позиции (заявки/изделия)
            PaperGrid.OnDblClick=(Dictionary<string,string> selectedItem) =>
            {
                Save();
            };

            //данные грида
            PaperGrid.OnLoadItems=PaperGridLoadItems;
            PaperGrid.OnFilterItems=PaperGridFilterItems;
            

            //фокус ввода           
            PaperGrid.Focus();

            
        }

        public void Init()
        {
            PaperGrid.Run();
            Show();
        }

        /// <summary>
        /// обновление записей
        /// </summary>
        public async void PaperGridLoadItems()
        {
            PaperGridToolbar.IsEnabled=false;
            PaperGrid.ShowSplash();

            bool resume=true;

            

            if (resume)
            {                
                var p = new Dictionary<string,string>();
                {
                    p.CheckAdd("FORMAT",Format.ToString());
                    p.CheckAdd("PROFILE_ID",ProfileId.ToString());
                    p.CheckAdd("FACTORY_ID", FactoryId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module","Production");
                q.Request.SetParam("Object","Cutter");
                q.Request.SetParam("Action","GetSources");
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if(q.Answer.Status == 0)                
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                    if(result!=null)
                    {
                        if(result.ContainsKey("RAW_GROUP"))
                        {
                            var ds = ListDataSet.Create(result, "RAW_GROUP");
                            PaperGrid.UpdateItems(ds);                   
                        }
                    }
                }
            }

            PaperGridToolbar.IsEnabled=true;
            PaperGrid.HideSplash(); 
        }

        /// <summary>
        /// фильтрация записей
        /// </summary>
        public async void PaperGridFilterItems()
        {
            if(PaperId!=0)
            {
                PaperGrid.SetSelectedItemId(PaperId);
            }
        }

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem"></param>
        public void PaperGridUpdateActions(Dictionary<string,string> selectedItem)
        {
            SelectedItem = selectedItem;

            SaveButton.IsEnabled=false;

            if(SelectedItem.Count>0)
            {
                SaveButton.IsEnabled=true;
            }
        }


        /// <summary>
        /// выбор заготовки
        /// </summary>
        public void Save()
        {
            if(SelectedItem!=null)
            {
                if(SelectedItem.Count>0)
                {
                    SelectedItem.CheckAdd("LAYER_ID",LayerId.ToString());
                    SelectedItem.CheckAdd("PROFILE_ID",ProfileId.ToString());
                    SelectedItem.CheckAdd("FORMAT",Format.ToString());

                    //отправляем сообщение о выборе заготовки
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup="ProductionTask",
                        ReceiverName = "CuttingManualView",
                        SenderName = "SelectPaper",
                        Action = "SelectedPaper",
                        ContextObject=SelectedItem,
                    });

                    Close();
                }
            }
        }

     
        
        public Window Window { get; set; }
        public void Show()
        {
            string title = $"Выбор сырьевой группы. Формат: {Format.ToString()}";

            int w = Width.ToInt();
            int h = Height.ToInt();

            Window = new Window
            {
                Title = title,
                Width = w + 17,
                Height = h + 40,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode=ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow,

            };

            Window.Content = new Frame
            {
                Content = this,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };


            if(Window != null)
            {
                Window.Topmost=true;
                Window.ShowDialog();
            }

            Window.Closed+=Window_Closed;
        }

        private void Window_Closed(object sender,System.EventArgs e)
        {
            Destroy();
        }

        public void Close()
        {            
            var window = this.Window;
            if(window != null)
            {
                window.Close();
            }
            Destroy();
        }
      
        public string BackTabName { get; set; }
        public void GoBack()
        {
            if(!string.IsNullOrEmpty(BackTabName))
            {
                Central.WM.SetActive(BackTabName,true);
                BackTabName="";
            }
        }

        private void ApplicationRefreshButton_Click(object sender,RoutedEventArgs e)
        {
            PaperGrid.LoadItems();
        }

        private void CancelButton_Click(object sender,RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender,RoutedEventArgs e)
        {
            Save();
        }

       

        

       

        private void HelpButton_Click(object sender,RoutedEventArgs e)
        {
            ShowHelp();
        }
    }
}
