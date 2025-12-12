using AutoUpdaterDotNET;
using Client.Annotations;
using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using DevExpress.ClipboardSource.SpreadsheetML;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using DevExpress.Xpf.Core.DragDrop.Native;
using DevExpress.Xpf.Core.Native;
using DevExpress.Xpf.Grid;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Office.Interop.Excel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Client.Interfaces.Production.PaperProduction
{
    /// <summary>
    /// План по приему машин с макулатурой на БДМ (бронь)
    /// <author>Greshnyh_ni</author>
    /// <version>1</version>
    /// <released>2025-09-01</released>
    /// <changed>2025-09-04</changed>
    /// </summary>
    public partial class ScrapPaperTerminalSlotTab : ControlBase
    {
        public FormHelper Form { get; set; }
        /// <summary>
        /// Имя вкладки
        /// </summary>
        public string TabName;

        private string RoleName;

        public string DocumentationUrl { get; set; }
        public bool Initialized { get; set; }

        private Timeout RefreshButtonTimeout { get; set; }

        /// <summary>
        ///  время обновления гридов (сек)
        /// </summary>
        private int RefreshTime { get; set; }

        /// <summary>
        ////количество секунд до обновления информации
        /// </summary>
        private int CurSecund { get; set; }

        /// Таймер периодического обновления каждую  секунду
        /// </summary>
        private DispatcherTimer FastTimer { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ScrapPaperTerminalSlotTab()
        {
            ControlTitle = "План разгрузок машин";
            //TabName = "ScrapPaperTerminalSlotTab";
            RoleName = "[erp]scrap_paper_bdm";
            DocumentationUrl = "/doc/l-pack-erp-new/lt/scrap_paper_bdm";

            InitializeComponent();

            //регистрация обработчика сообщений
            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    //ProcessMessage(msg);
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (KeyEventArgs e) =>
            {

                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
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
                // получение прав пользователя
                ProcessPermissions();
                TerminalSlotGridInit();
                SetFastTimer(1);
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                // Остановка таймеров
                FastTimer?.Stop();
                TerminalSlotGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
           //     TerminalSlotGrid.ItemsAutoUpdate = true;
           //     TerminalSlotGrid.Run();
                CurSecund = 0;
                FastTimer.Start();
               
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                TerminalSlotGrid.ItemsAutoUpdate = false;
              //  FastTimer?.Stop();
            };

          //  Initialized = true;
            
        }
              
        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            string role = "";
            // Проверяем уровень доступа
            role = RoleName;
            var mode = Central.Navigator.GetRoleLevel(role);
            var userAccessMode = mode;
           
            switch (mode)
            {
                case Role.AccessMode.Special:
                    {

                    }

                    break;

                case Role.AccessMode.FullAccess:
                    {
                      
                    }
                    break;

                case Role.AccessMode.ReadOnly:
                    {
                    }
                    break;
            }
        }

        /// <summary>
        /// обновляем все гриды
        /// </summary>
        public void Refresh()
        {
            CurSecund = 0;
            TerminalSlotGrid.LoadItems();
            FastTimer.Start();
        }

        /// <summary>
        /// обновляем время на кнопке до обновления информации
        /// </summary>
        private void RefreshButtonUpdate()
        {
            if (CurSecund >= RefreshTime)
            {
                CurSecund = 0;
                TerminalSlotGrid.LoadItems();

            }
            CurSecund = CurSecund + 1;
            int secondsBeforeFirstUpdate = RefreshTime - CurSecund;
            RefreshButton.Content = $"Обновить {secondsBeforeFirstUpdate}";
        }

        /// <summary>
        /// Таймер частого обновления (1 секунда)
        /// </summary>
        public void SetFastTimer(int autoUpdateInterval)
        {
            FastTimer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, autoUpdateInterval)
            };

            FastTimer.Tick += (s, e) =>
            {
                {
                    RefreshButtonUpdate();
                }
            };
                            
            FastTimer.Start();
        }


        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            RefreshTime = 300;
            CurSecund = 0;
        }

        /// <summary>
        /// Инициализация грида
        /// </summary>
        public void Init()
        {
            //фокус на кнопку обновления
            RefreshButton.Focus();
        }

        /// <summary>
        /// инициализация грида TerminalSlotGrid
        /// </summary>
        public void TerminalSlotGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Дт/вр",
                        Path="DTTM",
                        Doc="Дата и время слота",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM HH:mm",
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дт/вр регистрации",
                        Path="CREATED_DTTM",
                        Doc="Дата и время регистрации",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2 = 16,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дт/вр начала разгрузки",
                        Path="DT_FULL",
                        Doc="Дата и время начала разгрузки",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2 = 16,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номенклатура",
                        Path="TOVAR_NAME",
                        Doc="Номенклатура",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Поставщик",
                        Path="POST_NAME",
                        Doc="Поставщик",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Машина",
                        Path="CAR",
                        Doc="Машина",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 20,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Водитель",
                        Path="DRIVER",
                        Doc="Водитель",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 35,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД слота",
                        Path="WMTS_ID",
                        Doc="ИД слота",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание",
                        Path="NOTE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="FREE_FLAG",
                        Path="FREE_FLAG",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 2,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="SCTE_ID",
                        Path="SCTE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 2,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ID_STATUS",
                        Path="ID_STATUS",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 2,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ID_SCRAP",
                        Path="ID_SCRAP",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 2,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="IDTS",
                        Path="IDTS",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 2,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="RMBU_ID",
                        Path="RMBU_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 2,
                        Visible=false,
                    },

                };

                TerminalSlotGrid.SetColumns(columns);
                TerminalSlotGrid.SetPrimaryKey("WMTS_ID");
                TerminalSlotGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                TerminalSlotGrid.AutoUpdateInterval = 0;
                TerminalSlotGrid.EnableSortingGrid = false;
                TerminalSlotGrid.ItemsAutoUpdate = false;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                TerminalSlotGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {
                        // SelectedIdleItem = selectedItem;
                    }
                };

                // двойной клик по строке
                // TerminalSlotGrid.OnDblClick = IdleReasonEditShow;

                // раскраска всей строки
                TerminalSlotGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            var freeFlag = row.CheckGet("FREE_FLAG").ToInt();
                            var scteId = row.CheckGet("SCTE_ID").ToInt();
                            var idStatus = row.CheckGet("ID_STATUS").ToInt();
                            var idScrap = row.CheckGet("ID_SCRAP").ToInt();
                            var idTs = row.CheckGet("IDTS").ToInt();
                            var rmbuId = row.CheckGet("RMBU_ID").ToInt();

                            if ( freeFlag == 1) // слот свободный
                            {
                                 color = "#99CCFF"; // светло-голубой
                            } else
                            if (idScrap == 0 && (idTs != 0 || rmbuId != 0)) // слот занят под машину, которая не приехала
                            {
                                color = "#FFFF99";  // свело-желтый
                            } else
                            if (scteId != 0) // машина приехала и закреплена за терминалом
                            {
                                color = "#CCFFCC"; // cветло-зеленый
                            } else
                            if (scteId == 0 && (idStatus != 1 && idStatus != 11 && idStatus != 41)) // машина приехала и разрузилась
                            {
                                color = "#CCFFCC"; // cветло-зеленый
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },

                    // определение цветов шрифта строк
                    {
                        StylerTypeRef.ForegroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                                //на терминале: синий
                                if (row.ContainsKey("SCTE_ID"))
                                {
                                    if(row["SCTE_ID"].ToInt() != 0)
                                    {
                                        color = HColor.BlueFG;
                                    }
                                }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },

                };

                //данные грида
                TerminalSlotGrid.OnLoadItems = TerminalSlotGridLoadItems;
                TerminalSlotGrid.Init();

            }
        }


        /// <summary>
        /// получение записей для TerminalSlotGrid
        /// </summary>
        public async void TerminalSlotGridLoadItems()
        {
            bool resume = true;

            if (resume)
            {
                var q = new LPackClientQuery();

                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "ScrapPaper");
                q.Request.SetParam("Action", "WmsTerminalSlotList");

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
                        TerminalSlotGrid.UpdateItems(ds);
                    }
                }
            }
        }

        /// <summary>
        ////нажали кнопку обновить 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }
    }
}
