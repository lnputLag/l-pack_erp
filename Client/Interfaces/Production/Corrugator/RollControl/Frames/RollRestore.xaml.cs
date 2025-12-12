using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Список рулонов, списанных в расход за последние 8 часов
    /// позволяет выбрать рулон и сделать возврат
    /// </summary>
    /// <author>Рясной П.В.</author>
    public partial class RollRestore : UserControl
    {
        public RollRestore()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            SetDefaults();
            GridInit();
        }

        /// <summary>
        /// номер раската
        /// </summary>
        public int ReelNum { get; set; }

        /// <summary>
        /// ид ГА
        /// </summary>
        public int MachineId { get; set; }

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        private void ProcessMessages(ItemMessage obj)
        {
            //Group 
            if (obj.ReceiverGroup.IndexOf("Production") > -1)
            {
                if ((obj.ReceiverName.IndexOf("RollControl") > -1)
                        || (obj.ReceiverName.IndexOf("ReelControlKsh") > -1))
                {
                    switch (obj.Action)
                    {
                        case "Refresh":
                            GetData();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Production",
                ReceiverName = "",
                SenderName = "RollRestore",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            Grid.Destruct();
        }

        private void SetDefaults()
        {
            ReelNum = 0;
            MachineId = 0;
        }

        private void GridInit()
        {
            var columns = new List<DataGridHelperColumn>()
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=100,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Рулон",
                    Path="RAW_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=200,
                    MaxWidth=250,
                },
                new DataGridHelperColumn
                {
                    Header="Номер",
                    Path="NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=120,
                    MaxWidth=250,
                },
                new DataGridHelperColumn
                {
                    Header="Вес, кг",
                    Path="QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=90,
                },
                new DataGridHelperColumn
                {
                    Header="Длина, м",
                    Path="LNGTH",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=70,
                    MaxWidth=110,
                },
                new DataGridHelperColumn
                {
                    Header="Списан",
                    Path="DELETE_DTTM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format="dd.MM HH:mm",
                    MinWidth=90,
                    MaxWidth=110,
                },
            };
            Grid.SetColumns(columns);
            Grid.SetSorting("_ROWNNMBER", ListSortDirection.Ascending);
            Grid.AutoUpdateInterval = 0;
            Grid.SetMode(1);
            Grid.Init();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = (Dictionary<string, string> selectedItem) =>
            {
                ActionsUpdate(selectedItem);
            };
        }

        private async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Roll");
            q.Request.SetParam("Action", "ListDeletedRolls");
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

            q.Request.SetParam("MACHINE_ID", MachineId.ToString());
            q.Request.SetParam("REEL_NUM", ReelNum.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var RollsDS = ListDataSet.Create(result, "DeletedRolls");
                    Grid.UpdateItems(RollsDS);
                    Show();
                }
            }
            
        }

        private void ActionsUpdate(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;
        }

        public void Edit()
        {
            if ((MachineId > 0) && (ReelNum > 0))
            {
                GetData();
            }
        }

         /// <summary>
        /// Сохранение данных: подготовка данных
        /// </summary>
        public async void Save()
        {
            bool resume = true;

            if(resume)
            {
                if (SelectedItem==null)
                {
                    resume=false;
                }
            }

            /*  Иногда происходит автоматическое списание до того, как будут получены достоверные данные.
             *  У них выставлена длина 20, хотя фактически рулон почти не уменьшился.
             *  Такие рулоны тоже надо возвращать
            if(resume)
            {
                if (SelectedItem["QTY"].ToInt() <= 20)
                {
                    resume=false;                    
                    { 
                        var t="Нельзя вернуть рулон";
                        var m="Слишком маленький остаток";
                
                        var i=new ErrorTouch();
                        i.Show(t,m);
                    }
                }
            }
            */
            var v=new Dictionary<string,string>();
            if(resume)
            {
                v.CheckAdd("MACHINE_ID", MachineId.ToString());
                v.CheckAdd("REEL_NUM", ReelNum.ToString());
                v.CheckAdd("ID", SelectedItem["ID"]);            
            }

            //все данные собраны, отправляем
            if(resume)
            {
                SaveData(v);                
            }
        }
        
        /// <summary>
        /// Сохранение данных: отправка данных
        /// </summary>
        private async void SaveData(Dictionary<string,string> p)
        {
            DisableControls();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Roll");
            q.Request.SetParam("Action", "Restore");

            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var itemDS = ListDataSet.Create(result, "Items");
                    if (itemDS.Items.Count > 0)
                    {
                        Close();
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();
        }

        private void DisableControls()
        {
            Grid.ShowSplash();
            Toolbar.IsEnabled=false;
        }

        private void EnableControls()
        {
            Grid.HideSplash();
            Toolbar.IsEnabled=true;
        }

        public void Show()
        {
            Central.WM.Show($"RollRestore", "Восстановление рулона", true, "add", this);
        }

        public void Close()
        {
            Central.WM.Close($"RollRestore");
            Destroy();
        }

        private void SaveButton_Click(object sender,RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender,RoutedEventArgs e)
        {
            Close();
        }
    }
}
