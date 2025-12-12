using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Client.Assets.HighLighters;
using System;
using System.Windows.Input;
using Newtonsoft.Json;
using System.Linq;
using static Client.Interfaces.Main.DataGridHelperColumn;
using Xceed.Wpf.Toolkit;
using Org.BouncyCastle.Asn1.Crmf;
using System.Security.Cryptography;

namespace Client.Interfaces.Production.MoldedContainer
{
    /// <summary>
    /// Список простоев для станков на Литой таре
    /// </summary>
    /// <author>greshnyh_ni</author>   
    public partial class RecyclingIdleList : ControlBase
    {
        public RecyclingIdleList()
        {
            InitializeComponent();

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverName == ControlName)
                {
                    ProcessMessage(msg);
                    //Commander.ProcessCommand(m.Action, m);
                }
            };


            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {

                SetDefaults();
                Init();
                IdleGridInit();

            };


            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                IdleGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                IdleGrid.ItemsAutoUpdate = true;
                IdleGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                IdleGrid.ItemsAutoUpdate = false;
            };

            Commander.SetCurrentGridName("IdleGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "edit_idles",
                    Title = "Изменить",
                    Description = "Изменить информацию по выбранному простою",
                    Group = "idle_grid_default",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = IdlesEditButton,
                    ButtonName = "IdlesEditButton",
                    Enabled = false,
                    Action = () =>
                    {
                        IdlesEdit();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;

                        if (IdleGrid != null && IdleGrid.SelectedItem != null && IdleGrid.SelectedItem.Count > 0)
                        {
                            if (IdleGrid.SelectedItem.CheckGet("IDIDLES").ToInt() != 0)
                            {
                                result = true;
                            }
                        }
                        return result;
                    },
                });
            }

            Commander.Init(this);
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
        }

        /// <summary>
        /// Инициализация грида
        /// </summary>
        public void Init()
        {
            //получаем id текущего времени работы бригад (IdTimes)
            GetCurTime();
            // трансформация формы
            // double nScale = 1.5;
            // IdleGrid.LayoutTransform = new ScaleTransform(nScale, nScale);
        }

        /// <summary>
        /// Id текущей производственной смены 
        /// </summary>
        public int WorkShiftId { get; set; }

        /// <summary>
        /// Id предыдущей производственной смены 
        /// </summary>
        public int WorkShiftPredId { get; set; }

        public Dictionary<string, string> SelectedIdleItem { get; set; }
        public int MachineId { get; internal set; }

        /// <summary>
        /// инициализация грида IdleGrid
        /// </summary>
        public void IdleGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Минут",
                        Path="TIME_IDLES",
                        Doc="минут простоя",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Начало",
                        Path="START_DTTM",
                        Doc="Начало простоя",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Окончание",
                        Path="END_DTTM_HH",
                        Doc="Окончание простоя",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Причина",
                        Path="NAME",
                        Doc="Причина простоя",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 32,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Описание",
                        Path="REASON",
                        Doc="Описание простоя",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ID",
                        Path="IDIDLES",
                        Doc="",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 10,
                        Visible=false,
                    },

                    new DataGridHelperColumn
                    {
                        Header="ID_ST",
                        Path="ID_ST",
                        Doc="",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 10,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="FROMDT",
                        Path="FROMDT",
                        Doc="",
                        ColumnType=ColumnTypeRef.String,
                        Width = 10,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="END_DTTM",
                        Path="END_DTTM",
                        Doc="",
                        ColumnType=ColumnTypeRef.String,
                        Width = 10,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="IDREASON",
                        Path="IDREASON",
                        Doc="",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 10,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ID_REASON_DETAIL",
                        Path="ID_REASON_DETAIL",
                        Doc="",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 10,
                        Visible=false,
                    },
                };

                IdleGrid.SetColumns(columns);
                IdleGrid.SetPrimaryKey("IDIDLES");
                //ProductionGrid.SetSorting("ORD", ListSortDirection.Ascending);
                IdleGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                IdleGrid.AutoUpdateInterval = 60;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                IdleGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {
                        SelectedIdleItem = selectedItem;
                    }
                };

                // двойной клик по строке
                //  IdleGrid.OnDblClick = IdleReasonEditShow;

                //данные грида
                IdleGrid.OnLoadItems = IdleGridLoadItems;
                IdleGrid.Commands = Commander;
                IdleGrid.Init();
            }
        }

        /// <summary>
        /// получение записей для IdleGrid
        /// </summary>
        public async void IdleGridLoadItems()
        {
            bool resume = true;
            int num = 0;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_ST", MachineId.ToString());
                    p.CheckAdd("ID_TIMES", WorkShiftId.ToString());
                }
                var q = new LPackClientQuery();

                q.Request.SetParam("Module", "MoldedContainer");
                q.Request.SetParam("Object", "Recycling");
                q.Request.SetParam("Action", "RecyclingList");
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
                        IdleGrid.UpdateItems(ds);

                        var ds2 = ListDataSet.Create(result, "ITEMS2");
                        var cnt = ds2.Items.FirstOrDefault().CheckGet("CNT").ToInt().ToString();
                        var tm = ds2.Items.FirstOrDefault().CheckGet("TIME_IDLES").ToInt().ToString();
                        IdlesInfo.Content = $"Простои длительностью менее 10 мин. Количество {cnt} и общее время {tm} минут";
                    }
                }
            }
        }

        public void LoadItems()
        {
            GetCurTime();
            IdleGrid.LoadItems();
        }

        /// <summary>
        /// возвращает id текущего времени работы бригад (IdTimes)
        /// </summary>
        private bool GetCurTime()
        {
            var result = true;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "Bdm");
            q.Request.SetParam("Action", "BdmGetCurTime");

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var res = JsonConvert.DeserializeObject<Dictionary<string, string>>(q.Answer.Data);
                if (res != null)
                {
                    WorkShiftId = res.CheckGet("ID").ToInt();
                }
            }
            else
            {
                var s = $"Error: GetCurTime. Code=[{q.Answer.Error.Code}] Message=[{q.Answer.Error.Message}] Description=[{q.Answer.Error.Description}]";
                LogMsg(s);
                result = false;
            }

            {
                var s = $"Запрос на сервер. GetCurTime Code=[{q.Answer.Error.Code}]. Получен IdTimes=[{WorkShiftId}].";
                LogMsg(s);
            }
            return result;
        }

        /// <summary>
        /// нажали "Пред. смена"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IdTimesPredCheckBox_Click(object sender, RoutedEventArgs e)
        {
            LoadItems();
        }

        /// <summary>
        /// Обработка общих сообщений
        /// </summary>
        /// <param name="action"></param>
        /// <param name="obj"></param>
        private void ProcessMessage(ItemMessage obj = null)
        {
            if (obj.ReceiverGroup.IndexOf("MoldedContainer") > -1)
            {
                switch (obj.Action)
                {
                    case "RefreshRecyclingIdles":
                        LoadItems();
                        break;
                }
            }
        }

        /// <summary>
        /// редактирование простоя
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IdlesEdit()
        {
            var idleRecord = new RecyclingIdleRecord(SelectedIdleItem);
            idleRecord.ReceiverName = ControlName;
            idleRecord.Edit();
        }




    }
}
