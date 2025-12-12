using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Client.Assets.HighLighters;
using Newtonsoft.Json;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Блок "Водитель погрузчика"
    /// (план отгрузок)
    /// </summary>
    /// <author>balchugov_dv</author>   
    public partial class DiagramForkliftdriver : UserControl
    {
        /// <summary>
        /// Блок "Водитель погрузчика"
        /// </summary>
        /// <param name="values"></param>
        public DiagramForkliftdriver(Dictionary<string, string> values)
        {
            InitializeComponent();
            Values = values;
            Init();
            ProcessPermissions();
        }

        public string RoleName = "[erp]shipment_control";

        /// <summary>
        /// данные блока
        /// </summary>
        public Dictionary<string, string> Values { get; set; }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel(this.RoleName);
            var userAccessMode = mode;
            switch (mode)
            {
                case Role.AccessMode.Special:
                    break;

                case Role.AccessMode.FullAccess:
                    break;

                case Role.AccessMode.ReadOnly:
                default:
                    SendMenuItem.IsEnabled = false;
                    BackMenuItem.IsEnabled = false;
                    break;
            }
        }

        /// <summary>
        /// инициализация блока
        /// </summary>
        public void Init()
        {
            var bgColor = "";
            
            DriverName.Text = Values.CheckGet("FORKLIFT_DRIVER_NAME");
            DriverName.Text = Values.CheckGet("FORKLIFT_DRIVER_NAME").SurnameInitials();
            DriverPhone.Text = Values.CheckGet("PHONE").CellPhone();

            DriverNote.Text = "";
            DriverNote.ToolTip = "";
            
            /*
               Статус водителя: 
               0 - работает, 
               1 - на обеде, 
               2 - выполняет общецеховые задачи
            */
            switch(Values.CheckGet("ABSENT_FLAG").ToInt())
            {
                //обед
                case 1:

                    var absentStart="";
                    if(!string.IsNullOrEmpty(Values.CheckGet("ABSENT_START")))
                    {
                        var absentStartDate=Values.CheckGet("ABSENT_START").ToDateTime();
                        absentStart=$"c {absentStartDate.ToString("HH:mm")}";
                    }

                    DriverNote.Text      = $"обед {absentStart}";
                    DriverNote.ToolTip   = $"на обеде {absentStart}";
                    bgColor = HColor.Orange;
                    break;

                //цех
                case 2:
                    DriverNote.Text = "цех";
                    DriverNote.ToolTip = "общецеховые задачи";
                    bgColor = HColor.Orange;

                    SendMenuItem.IsEnabled = false;
                    BackMenuItem.IsEnabled = true;
                    break;

                default:
                    DriverNote.Text = "";
                    DriverNote.ToolTip = "";
                    break;
            }
            
            //предыдущая смена
            if(!Values.CheckGet("ACTIVE_FLAG").ToBool())
            {
                DriverNote.Text = "смена";
                DriverNote.ToolTip = "предыдущая смена";
                bgColor = HColor.OrangeOrange;
            }

            //уже был на обеде

            //черный
            var driverNameColor=HColor.BlackFG;
            if(Values.CheckGet("DINNER_CNT").ToInt() > 0)
            {
                driverNameColor=HColor.BlueFg;
                DriverName.ToolTip="Уже был на обеде";
            }
            {
                var bc2 = new BrushConverter();
                DriverName.Foreground = (Brush)bc2.ConvertFrom(driverNameColor);
            }

            //достижения
            {
                DriverStatCnt.Text = "";
                DriverStatSquare.Text = "";
                {
                    var x = Values.CheckGet("LOADED_CNT").ToInt();
                    if(x > 0)
                    {
                        DriverStatCnt.Text = x.ToString();
                    }
                }
                {
                    var x = Values.CheckGet("LOADED_SQUARE").ToInt();
                    if(x > 0)
                    {
                        DriverStatSquare.Text = x.ToString();
                    }
                }
            }
            
            //цвет блока
            if(!string.IsNullOrEmpty(bgColor))
            {
                var bc = new BrushConverter();
                DriverNoteBlock.Background = (Brush)bc.ConvertFrom(bgColor);
            }
            
            UpdateActions();
        }

        public void UpdateActions()
        {
            // пункты контекстного меню: отправить на общецеховые задачи и вернуть
            SendMenuItem.IsEnabled = true;
            BackMenuItem.IsEnabled = false;
            
            /*
                Статус водителя: 
                0 - работает, 
                1 - на обеде, 
                2 - выполняет общецеховые задачи
             */
            switch(Values.CheckGet("ABSENT_FLAG").ToInt())
            {
                case 2:
                    SendMenuItem.IsEnabled = false;
                    BackMenuItem.IsEnabled = true;
                    break;
            }

            ProcessPermissions();
        }
        
        /// <summary>
        /// обновление статуса водителя
        /// newValue=
        ///     0-вернуть с общецеховых задач
        ///     2-отправить на общецеховые задачи
        /// </summary>
        private async void UpdateAbsentFlag(int newValue)
        {
            Mouse.OverrideCursor=Cursors.Wait;

            bool resume = true;

            int forkliftdriverId = Values.CheckGet("FORKLIFT_ID").ToInt();
            if (resume)
            {
                if (forkliftdriverId==0)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("FORKLIFT_ID", forkliftdriverId.ToString());
                    p.CheckAdd("STATUS_ID", newValue.ToString());
                }
            
                var q = new LPackClientQuery();
                q.Request.SetParam("Module","Shipments");
                q.Request.SetParam("Object","ForkliftDriver");
                q.Request.SetParam("Action","SetStatus");

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
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            var id=ds.GetFirstItemValueByKey("ID").ToInt();
                        
                            if(id!=0)
                            {
                                //отправляем сообщение гриду о необходимости обновить данные
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "ShipmentControl",
                                    SenderName = "",
                                    ReceiverName = "DriverList",
                                    Action = "Refresh",
                                });
                            }
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
            
            Mouse.OverrideCursor=null;
        }
        
        private void ToGeneralTask(object sender, System.Windows.RoutedEventArgs e)
        {
            UpdateAbsentFlag(2);
        }

        private void BackFromTask(object sender, System.Windows.RoutedEventArgs e)
        {
            UpdateAbsentFlag(0);
        }
    }
}
