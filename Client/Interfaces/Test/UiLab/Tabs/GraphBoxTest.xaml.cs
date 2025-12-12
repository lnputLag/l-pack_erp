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
    /// тестовый интерфейс для отладки блока "график"
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2022-10-31</released>
    /// <changed>2022-10-31</changed>
    public partial class GraphBoxTest:UserControl
    {
        public GraphBoxTest()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);
            
            Init();
            SetDefaults();
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
        public FormHelper Form { get; set; }

        /// <summary>
        // инициализация компонентов
        /// </summary>
        public void Init()
        {
            //инициализация грида
            {
                //колонки грида
 
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Время",
                        Path="TIME",
                        ColumnType=ColumnTypeRef.Integer,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Скорость",
                        Path="SPEED",
                        ColumnType=ColumnTypeRef.Integer,
                        Stylers=new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                        {
                            {
                                DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    /*
                                        //red
                                        #FFEC5F67
                                        //orange
                                        #FFF99157
                                        //yellow
                                        #FFFAC863
                                        //green
                                        #FF99C794
                                        //breeze
                                        #FF5FB3B3
                                        //blue
                                        #FF6699CC
                                        //violet
                                        #FFC594C5
                                        //brown
                                        #FFAB7967
                                        //gray
                                        #FFA7ADBA
                                     */
                                    color = "#FF99C794";
                                    if(row.CheckGet("VALUE").ToInt() > 150)
                                    {
                                        color = "#FFEC5F67";
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        }
                    },
                    new DataGridHelperColumn
                    {
                        Header="Скорость2",
                        Path="SPEED2",
                        ColumnType=ColumnTypeRef.Integer,
                    },
                };
                Graph.SetColumns(columns);
                Graph.PrimaryKey="TIME";
                Graph.Init();

                ////данные грида
                Graph.OnLoadItems = LoadItems;
                Graph.Run();
            }

            //инициализация формы
            {
                Form = new FormHelper();

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
                Form.SetFields(fields);
            }

            //фокус ввода           
            Graph.Focus();
        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Accounts",
                ReceiverName = "",
                SenderName = "UserList",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            //Grid.Destruct();
        }


        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            ////Group ProductionTask
            //if (m.ReceiverGroup.IndexOf("User") > -1)
            //{
            //    switch (m.Action)
            //    {
            //        case "Refresh":
            //            Grid.LoadItems();

            //            // выделение на новую строку
            //            var id = m.Message.ToInt();
            //            Grid.SetSelectedItemId(id);

            //            break;
            //    }
            //}

            //if (m.ReceiverGroup.IndexOf("Group") > -1)
            //{
            //    switch (m.Action)
            //    {
            //        case "Refresh":
            //            Grid.LoadItems();
            //            break;
            //    }
            //}
        }

        /// <summary>
        /// получение записей
        /// </summary>
        public async void LoadItems()
        {
            DisableControls();

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

                        //это тестовый блок
                        //вместо него нужно подставить реальные данные
                        {
                            var ds= new ListDataSet();
                            ds.Init();
                            ds.Items=_GenerateTestData();
                            Graph.UpdateItems(ds);
                        }
                        

                        //{
                        //    var ds=ListDataSet.Create(result,"ITEMS");
                        //    Grid.UpdateItems(ds);
                        //}

                        //{
                        //    var ds = ListDataSet.Create(result, "DEPARTMENTS");

                        //    {
                        //        var list = new Dictionary<string, string>();
                        //        list.Add("-1", "Все");

                        //        foreach (var item in ds.Items)
                        //        {
                        //            list.Add(item["ID"], item["NAME"]);
                        //        }

                        //        Department.Items = list;

                        //        if (i.Key == null)
                        //            Department.SelectedItem = list.FirstOrDefault((x) => x.Key == "-1");
                        //        else
                        //            Department.SelectedItem = i;

                        //    }
                        //}
                    }
                }                
            }

            EnableControls();
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            Graph.ShowSplash();
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            Graph.HideSplash();
        }


        public List<Dictionary<string,string>> _GenerateTestData()
        {
            var data=new List<Dictionary<string,string>>();

            var startDateTime="08.11.2022 09:00:00";
            var step=5;

            {
                
                var random = new Random();
                var dt=startDateTime.ToDateTime();
                for(int i=1; i<100; i++)
                //for(int i=1; i<5; i++)
                {
                    var t=dt.ToString("HH:mm");
                    var v=random.Next(50, 250);
                    var v2=random.Next(220, 420);

                    {
                        var row=new Dictionary<string,string>();
                        row.Add("TIME",t.ToString());
                        row.Add("SPEED",v.ToString());
                        row.Add("SPEED2",v2.ToString());
                        data.Add(row);
                    }

                    dt=dt.AddMinutes(step);
                }
            }

            return data;
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
                    Graph.LoadItems();
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
            Central.ShowHelp("/doc/l-pack-erp/service/accounts/users");
        }

       


        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

      

        private void RefreshButton_Click(object sender,RoutedEventArgs e)
        {
            Graph.LoadItems();
        }

      
    }


}
