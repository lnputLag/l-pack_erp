using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Test
{
    
    /// <summary>
    /// тестовый интерфейс для отладки функции Drag and Drop
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2022-11-14</released>
    /// <changed>2022-11-14</changed>
    public partial class DndTest:UserControl
    {
        public DndTest()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);
            
            Grid1Init();
            Grid2Init();
            ToolbarFormInit();
            ToolbarFormSetDefaults();
            Grid1.Focus();
        }

        /// <summary>
        /// данные из выбранной в гриде строки
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }
       
        /// <summary>
        /// ID выбранной группы ролей
        /// </summary>
        int DepartmentID { get; set; } = -1;

        /// <summary>
        /// форма с полями для фильтрации данных
        /// </summary>
        public FormHelper ToolbarForm { get; set; }

        #region Common

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
           
        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Test",
                ReceiverName = "",
                SenderName = "InitToolbarForm",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            //Grid.Destruct();
        }

          /// <summary>
        /// обработка ввода с клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e=Central.WM.KeyboardEventsArgs;
            switch(e.Key)
            {
                case Key.F5:
                    Grid1.LoadItems();
                    e.Handled=true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled=true;
                    break;
            }
        }
       
        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp");
        }

        #endregion

        #region ToolbarForm

        public void ToolbarFormInit()
        {
            //инициализация формы
            {
                ToolbarForm = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="SEARCH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=Search,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                };
                ToolbarForm.SetFields(fields);
            }
        }

        public void ToolbarFormSetDefaults()
        {
            ToolbarForm.SetDefaults();
        }

        #endregion

        #region Grid1

        /// <summary>
        // инициализация грида
        /// </summary>
        public void Grid1Init()
        {
            //инициализация грида
            {
                //колонки грида
 
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=50,
                        MaxWidth=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер",
                        Path="NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,
                        MaxWidth=70,
                    },                
                    new DataGridHelperColumn
                    {
                        Header="Дата",
                        Path="ONDATE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=90,
                        MaxWidth=90,
                    },                
                    new DataGridHelperColumn
                    {
                        Header="Заголовок",
                        Path="TITLE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=120,
                        MaxWidth=900,
                    },                
                };
                Grid1.SetColumns(columns);
                Grid1.PrimaryKey="ID";
                Grid1.SetSorting("ID",ListSortDirection.Ascending);
                Grid1.UseRowDragDrop=true;
                Grid1.Name="Grid1";
                Grid1.Init();
                Grid1.OnLoadItems = Grid1LoadItems;
                Grid1.Run();
            }
            
        }

        /// <summary>
        /// получение записей
        /// </summary>
        public async void Grid1LoadItems()
        {
            Grid1DisableControls();

            bool resume = true;

            if (resume)
            {
                
                var p=new Dictionary<string,string>();

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Accounts");
                q.Request.SetParam("Object", "User");
                q.Request.SetParam("Action", "List");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;
                
                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {

                        {
                            var ds= new ListDataSet();
                            ds.Init();
                            ds.Items=Grid1GenerateItems();
                            Grid1.UpdateItems(ds);
                        }
                    
                    }
                }                
            }

            Grid1EnableControls();
        }

        public List<Dictionary<string,string>> Grid1GenerateItems()
        {
            var data=new List<Dictionary<string,string>>();

            {
                var row=new Dictionary<string,string>();
                row.Add("ID",       "1");
                row.Add("NUMBER",   "2342");
                row.Add("ONDATE",   "14.11.2022");
                row.Add("TITLE",    "Laboris nisi ut aliquip");
                data.Add(row);
            }

            {
                var row=new Dictionary<string,string>();
                row.Add("ID",       "2");
                row.Add("NUMBER",   "2343");
                row.Add("ONDATE",   "14.11.2022");
                row.Add("TITLE",    "Magni dolores eos qui ratione");
                data.Add(row);
            }

            {
                var row=new Dictionary<string,string>();
                row.Add("ID",       "3");
                row.Add("NUMBER",   "2344");
                row.Add("ONDATE",   "13.11.2022");
                row.Add("TITLE",    "Neque porro quisquam");
                data.Add(row);
            }

            {
                var row=new Dictionary<string,string>();
                row.Add("ID",       "4");
                row.Add("NUMBER",   "5678");
                row.Add("ONDATE",   "13.11.2022");
                row.Add("TITLE",    "Unde omnis iste natus error sit ");
                data.Add(row);
            }

            {
                var row=new Dictionary<string,string>();
                row.Add("ID",       "5");
                row.Add("NUMBER",   "5679");
                row.Add("ONDATE",   "10.11.2022");
                row.Add("TITLE",    "Nemo enim ipsam voluptatem");
                data.Add(row);
            }

            return data;
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void Grid1DisableControls()
        {
            GridToolbar.IsEnabled = false;
            Grid1.ShowSplash();
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void Grid1EnableControls()
        {
            GridToolbar.IsEnabled = true;
            Grid1.HideSplash();
        }

        #endregion


        #region Grid2

        /// <summary>
        // инициализация грида
        /// </summary>
        public void Grid2Init()
        {
            //инициализация грида
            {
                //колонки грида
 
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=50,
                        MaxWidth=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер",
                        Path="NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,
                        MaxWidth=70,
                    },                
                    new DataGridHelperColumn
                    {
                        Header="Дата",
                        Path="ONDATE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=90,
                        MaxWidth=90,
                    },                
                    new DataGridHelperColumn
                    {
                        Header="Заголовок",
                        Path="TITLE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=120,
                        MaxWidth=900,
                    },                
                };
                Grid2.SetColumns(columns);
                Grid2.PrimaryKey="ID";
                Grid2.SetSorting("ID",ListSortDirection.Ascending);
                Grid2.UseRowDragDrop=true;
                Grid2.OnItemDrop=Grid2Drop;
                Grid2.Name="Grid2";
                Grid2.Init();
                Grid2.OnLoadItems = Grid2LoadItems;
                Grid2.Run();
            }            
        }

        /// <summary>
        /// получение записей
        /// </summary>
        public async void Grid2LoadItems()
        {
            Grid2DisableControls();

            bool resume = true;

            if (resume)
            {
                
                var p=new Dictionary<string,string>();

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Accounts");
                q.Request.SetParam("Object", "User");
                q.Request.SetParam("Action", "List");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;
                
                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        {
                            var ds= new ListDataSet();
                            ds.Init();
                            //ds.Items=Grid2GenerateItems();
                            Grid2.UpdateItems(ds);
                        }                    
                    }
                }                
            }

            Grid2EnableControls();
        }

        public void Grid2Drop(string sourceName, Dictionary<string,string> row)
        {
            if(sourceName=="Grid1")
            {
                if(row.Count>0)
                {
                    var i=row.CheckGet("ID");
                    var item=Grid1.DataSet.GetItemByKeyValue("ID",i);
                    
                    Grid2.DataSet.AddItem(item);
                    Grid2.UpdateItems();

                    Grid1.DataSet.RemoveItemByKeyValue("ID",i);
                    Grid1.UpdateItems();
                }
            }
        }

        public List<Dictionary<string,string>> Grid2GenerateItems()
        {
            var data=new List<Dictionary<string,string>>();

            {
                var row=new Dictionary<string,string>();
                row.Add("ID",       "10");
                row.Add("NUMBER",   "9922");
                row.Add("ONDATE",   "14.11.2022");
                row.Add("TITLE",    "Laboris nisi ut aliquip");
                data.Add(row);
            }

            {
                var row=new Dictionary<string,string>();
                row.Add("ID",       "20");
                row.Add("NUMBER",   "9923");
                row.Add("ONDATE",   "14.11.2022");
                row.Add("TITLE",    "Magni dolores eos qui ratione");
                data.Add(row);
            }

            {
                var row=new Dictionary<string,string>();
                row.Add("ID",       "30");
                row.Add("NUMBER",   "9925");
                row.Add("ONDATE",   "13.11.2022");
                row.Add("TITLE",    "Neque porro quisquam");
                data.Add(row);
            }

            {
                var row=new Dictionary<string,string>();
                row.Add("ID",       "40");
                row.Add("NUMBER",   "1556");
                row.Add("ONDATE",   "13.11.2022");
                row.Add("TITLE",    "Unde omnis iste natus error sit ");
                data.Add(row);
            }

            {
                var row=new Dictionary<string,string>();
                row.Add("ID",       "50");
                row.Add("NUMBER",   "1557");
                row.Add("ONDATE",   "10.11.2022");
                row.Add("TITLE",    "Nemo enim ipsam voluptatem");
                data.Add(row);
            }

            return data;
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void Grid2DisableControls()
        {
            GridToolbar.IsEnabled = false;
            Grid2.ShowSplash();
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void Grid2EnableControls()
        {
            GridToolbar.IsEnabled = true;
            Grid2.HideSplash();
        }

        #endregion
      

       


        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void RefreshButton_Click(object sender,RoutedEventArgs e)
        {
            Grid1.LoadItems();
        }

        
    }
}
