using System;
using System.Collections.Generic;
using Client.Common;
using Client.Interfaces.Main;
using Microsoft.Office.Interop.Word;
using Newtonsoft.Json;
using Task = System.Threading.Tasks.Task;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperatorKsh
{
    public partial class ProfileSpeed : ControlBase
    {
        /// <summary>
        /// Грид с отображение скоростей про профилям которые проехали за смену
        /// </summary>
        /// <author>volkov_as</author>
        public ProfileSpeed()
        {
            InitializeComponent();

            OnLoad = GridInit;

            OnUnload = () =>
            {
                SpeedProfileGrid.Destruct();
            };
            
            OnFocusGot = () =>
            {
                SpeedProfileGrid.ItemsAutoUpdate = true;
            };
            
            OnFocusLost = () =>
            {
                SpeedProfileGrid.ItemsAutoUpdate = false;
            };
        }
        
        /// <summary>
        /// Инициализация грида
        /// </summary>
        public void Init()
        {
            GridInit();
            LoadItems();
        }

        /// <summary>
        /// По умолчанию выключен
        /// 0 - off ; 1 - on
        /// </summary>
        public static int StateButton = 0;
        
        
        /// <summary>
        /// Выбранная машанина на текущий момент 
        /// </summary>
        public static int CurrentMachineId = 0;

        /// <summary>
        /// Инициализация грида
        /// </summary>
        private void GridInit()
        {
            var columns = new List<DataGridHelperColumn>()
            {
                new DataGridHelperColumn
                {
                    Header="Профиль",
                    Path="PROFILE_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width = 70,
                },
                new DataGridHelperColumn
                {
                    Header="Средняя скорость",
                    Path="AVG_SPEED",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width = 110,
                },
                new DataGridHelperColumn
                {
                    Header="Проехали за смену",
                    Path="TRAVELED_METERS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Double,
                    Width = 110,
                },
            };
            SpeedProfileGrid.SetColumns(columns);
            SpeedProfileGrid.AutoUpdateInterval  = 0;
            SpeedProfileGrid.Init();
        }

        /// <summary>
        /// Получения данных
        /// </summary>
        public async void LoadItems()
        {
            DateTime currentTime = DateTime.Now;
            bool isDayShift = currentTime.Hour >= 8 && currentTime.Hour < 20;
            DateTime startDate;
            DateTime endDate;
            
            if (isDayShift)
            {
                startDate = currentTime.Date.AddHours(8);
                endDate = currentTime.Date.AddHours(20);
            }
            else
            {
                if (currentTime.Hour >= 20)
                {
                    startDate = currentTime.Date.AddHours(20);
                    endDate = currentTime.Date.AddDays(1).AddHours(8);
                }
                else
                {
                    startDate = currentTime.Date.AddDays(-1).AddHours(20);
                    endDate = currentTime.Date.AddHours(8);
                }
            }
            
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatorMachineOperator");
            q.Request.SetParam("Action", "GetSpeedAnyProfile");
            q.Request.SetParam("ID_ST", CurrentMachineId.ToString());
            q.Request.SetParam("START_DTTM", startDate.ToString("dd.MM.yyyy HH:mm:ss"));
            q.Request.SetParam("END_DTTM", endDate.ToString("dd.MM.yyyy HH:mm:ss"));

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
                    SpeedProfileGrid.UpdateItems(ds);
                }
            }
        }
    }
}