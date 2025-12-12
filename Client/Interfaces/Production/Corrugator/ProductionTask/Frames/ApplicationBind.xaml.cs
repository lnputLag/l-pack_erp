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
    /// выбор заявки для функций привязки/отвязки позиций ПЗ,
    /// а также функция привязки пз 
    /// </summary>
    /// <author>balchugov_dv</author>  
    /// <version>1</version>
    /// <released>2021-11-18</released>
    public partial class ApplicationBind:UserControl
    {
        public ApplicationBind()
        {
            InitializeComponent();
            
            GoodsId=0;
            ProductionTaskId=0;

            BackTabName="ProductionTask_productionTaskList";

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this,ProcessMessages);

            //регистрация обработчика клавиатуры
            PreviewKeyDown+=ProcessKeyboard;

            SetDefaults();
            InitGrid();            
        }

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string,string> SelectedItem { get; set; }

        /// <summary>
        /// ИД изделия
        /// </summary>
        public int GoodsId { get; set; }
        /// <summary>
        /// ИД производственного задания
        /// </summary>
        public int ProductionTaskId { get; set; }
        /// <summary>
        /// ИД заявки
        /// </summary>
        public int ApplicationId { get;set;}
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
            if(m.ReceiverGroup.IndexOf("ShipmentControl") > -1)
            {
                if(m.ReceiverName.IndexOf("DriverAllList")>-1)
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
            Central.ShowHelp("/doc/l-pack-erp/production/production_tasks/list/join_application");
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
                SenderName = "SelectApplicationView",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            Grid.Destruct();

            //возврат к предыдущему интерфейсу (если есть цепь навигации)
            GoBack();
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
        }

        /// <summary>
        /// инициализация грида
        /// </summary>
        public void InitGrid()
        {
            //список колонок грида
            var columns = new List<DataGridHelperColumn>()
            {
                new DataGridHelperColumn
                {
                    Header="ИД заявки",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width=60,                        
                },
                new DataGridHelperColumn
                {
                    Header="Номер заявки",
                    Doc="Символьный номер заявки",
                    Path="NUM",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=100,                        
                },
                new DataGridHelperColumn
                {
                    Header="Изделий в заявке",
                    Doc="Количество изделий в заявке",
                    Path="IN_APPLICATION_QUANTITY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width=110,                        
                },
                new DataGridHelperColumn
                {
                    Header="Заготовок в ПЗ",
                    Doc="Количество заготовок в ПЗ, всего",
                    Path="BLANK_QUANTITY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width=100,               
                },
                new DataGridHelperColumn
                {
                    Header=" ",
                    Path="_",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=100, 
                    MaxWidth=900,
                },
                    
            };
            Grid.SetColumns(columns);


            Grid.SetSorting("ID",ListSortDirection.Ascending);
            Grid.Init();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem=(Dictionary<string,string> selectedItem) =>
            {
                if(selectedItem.Count > 0)
                {
                    GridUpdateActions(selectedItem);
                }
            };

            //двойной клик на строке осуществит выбор данной позиции (заявки/изделия)
            Grid.OnDblClick=(Dictionary<string,string> selectedItem) =>
            {
                Save();
            };

            //данные грида
            Grid.OnLoadItems=GridLoadItems;
            Grid.OnFilterItems=GridFilterItems;
            //Grid.Run();

            //фокус ввода           
            Grid.Focus();

            
        }

        /// <summary>
        /// фильтрация записей
        /// </summary>
        public async void GridFilterItems()
        {
            
        }

        /// <summary>
        /// обновление записей
        /// </summary>
        public async void GridLoadItems()
        {
            Grid.ShowSplash();

            bool resume=true;

            if (resume)
            {                
                var p = new Dictionary<string,string>();

                {
                    p.Add("ID",GoodsId.ToString());
                    p.Add("FACTORY_ID", FactoryId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module","Production");
                q.Request.SetParam("Object","Application");
                q.Request.SetParam("Action","List");
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
                        if(result.ContainsKey("ITEMS"))
                        {
                            var ds=(ListDataSet)result["ITEMS"];
                            ds.Init();
                            Grid.UpdateItems(ds,false);
                            if(ApplicationId!=0)
                            {
                                Grid.SelectRowByKey(ApplicationId);
                            }
                        }
                    }
                }
            }

            Grid.HideSplash(); 
        }

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem"></param>
        public void GridUpdateActions(Dictionary<string,string> selectedItem)
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
        public async void Save()
        {
            bool resume=true;
            
            int positionId=0;
            if(resume)
            {
                if(SelectedItem!=null)
                {
                    if(SelectedItem.ContainsKey("ID"))
                    {
                       positionId=SelectedItem["ID"].ToInt();
                    }
                }
            }

            if(resume)
            {
                if(positionId==0)
                {
                    resume=false;
                }
            }

            if(resume)
            {
                string msg = "";
                msg += $"Привязать производственное задание к заявке?";
                msg += $"\nЗаявка: {SelectedItem.CheckGet("NUM")}";

                var d = new DialogWindow($"{msg}","Привязка ПЗ","",DialogWindowButtons.NoYes);
                if(d.ShowDialog() != true)
                {
                    resume=false;
                } 
            }

            if (resume)
            {                
                var p = new Dictionary<string,string>();

                {
                    p.Add("PRODUCTION_TASK_ID", ProductionTaskId.ToString());
                    p.Add("POSITION_ID",        positionId.ToString());
                    p.Add("GOODS_ID",           GoodsId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module","Production");
                q.Request.SetParam("Object","ProductionTask");
                q.Request.SetParam("Action","BindApplication");

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                var complete=false;
                int itemId=0;

                if(q.Answer.Status == 0)                
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                    if(result!=null)
                    {
                        if(result.ContainsKey("ITEMS"))
                        {
                            var ds=(ListDataSet)result["ITEMS"];
                            ds.Init();
                            
                            itemId=ds.GetFirstItemValueByKey("PRODUCTION_TASK_ID").ToInt();
                            if(itemId!=0)
                            {
                                complete=true;
                            }
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }

                if(complete)
                {
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "ProductionTask",
                        ReceiverName = "TaskList",
                        SenderName = "ApplicationBindView",
                        Action = "Refresh",
                        Message= $"{itemId}",
                    });
                                
                    SetDefaults();

                    if(!string.IsNullOrEmpty(BackTabName))
                    {
                        Close();
                    }
                }
                else
                {
                    var msg="Не удалось привязать задание";
                    var d = new DialogWindow($"{msg}","Привязка ПЗ","",DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            
        }
        
        public Window Window { get; set; }
        public void Show()
        {
            string title=$"Выбор заявки";            
            Central.WM.AddTab($"select_blank",title,true,"add",this);
        }

        public void Close()
        {            
            Central.WM.RemoveTab($"select_blank");
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

        private void RefreshButton_Click(object sender,RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void CancelButton_Click(object sender,RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender,RoutedEventArgs e)
        {
            Save();
        }

        private void Cardboard_SelectedItemChanged(DependencyObject d,DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void Type_SelectedItemChanged(DependencyObject d,DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void Filter1_Click(object sender,RoutedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void HelpButton_Click(object sender,RoutedEventArgs e)
        {
            ShowHelp();
        }
    }
}
