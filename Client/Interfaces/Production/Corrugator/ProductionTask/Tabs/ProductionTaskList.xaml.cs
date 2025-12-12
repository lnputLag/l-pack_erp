using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
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

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Производственные задания, общий список
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>2</version>
    /// <released>2021-11-16</released>
    public partial class ProductionTaskList : UserControl
    {
        public ProductionTaskList()
        {
            InitializeComponent();

            SelectBlankFromDate = DateTime.Now.AddDays(0);
            SelectBlankToDate = DateTime.Now.AddDays(9);

            SelectedItemId = 0;
            TlsIdHidden = true;
            DuplicatedTaskQuantity = 0;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);


            SetDefaults();
            LoadRef();
            TaskGridInit();
            PositionGridInit();
            SourceGridInit();

            if (Central.DebugMode)
            {
                TlsIdHidden = false;
            }

            ProcessPermissions();
        }

        /// <summary>
        /// датасет, содержащий данные
        /// </summary>
        public ListDataSet ProductionTasksDS { get; set; }
        public ListDataSet PositionsDS { get; set; }
        public ListDataSet CardboardDS { get; set; }
        public ListDataSet ProfileDS { get; set; }
        public ListDataSet FormatDS { get; set; }
        public ListDataSet CreatorDS { get; set; }
        public ListDataSet MachineDS { get; set; }

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        public int SelectedItemId { get; set; }
        Dictionary<string, string> SelectedItem { get; set; }

        public int SelectedItemId2 { get; set; }
        Dictionary<string, string> SelectedItem2 { get; set; }

        public DateTime SelectBlankFromDate { get; set; }
        public DateTime SelectBlankToDate { get; set; }

        private bool TlsIdHidden { get; set; }
        private int DuplicatedTaskQuantity { get; set; }

        public string RoleName = "[erp]production_task_cm";

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
                    break;
            }

            List<Button> buttons = UIUtil.GetVisualChilds<Button>(this.Content as DependencyObject);
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    var buttonTagList = UIUtil.GetTagList(button);
                    var accessMode = Acl.FindTagAccessMode(buttonTagList);
                    if (accessMode > userAccessMode)
                    {
                        button.IsEnabled = false;
                    }
                }
            }

            if (TaskGrid != null && TaskGrid.Menu != null && TaskGrid.Menu.Count > 0)
            {
                foreach (var manuItem in TaskGrid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }

            if (PositionGrid != null && PositionGrid.Menu != null && PositionGrid.Menu.Count > 0)
            {
                foreach (var manuItem in PositionGrid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            //Group ProductionTask
            if (m.ReceiverGroup.IndexOf("ProductionTask") > -1)
            {

                //позиции ПЗ
                if (m.ReceiverName.IndexOf("PositionList") > -1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            PositionGrid.LoadItems();
                            break;
                    }
                }


                //список ПЗ
                if (m.ReceiverName.IndexOf("TaskList") > -1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            var itemId = m.Message.ToInt();
                            if (itemId != 0)
                            {
                                TaskGrid.SelectedItem = new Dictionary<string, string>()
                                {
                                    { "PRODUCTIONTASKID", itemId.ToString() }
                                };
                            }
                            TaskGrid.LoadItems();
                            break;

                        case "SaveNote":
                            SaveTaskNote((Dictionary<string, string>)m.ContextObject);
                            break;
                        
                        case "Search":
                            if(!m.Message.IsNullOrEmpty())
                            {
                                //FIXME: use FormHelper
                                //var v=new Dictionary<string,string>();
                                //v.CheckAdd("SEARCH", m.Message);
                                //Form.SetValues(v);
                                
                                SearchText.Text = m.Message;
                                TaskGrid.UpdateItems();
                                Central.WM.SetActive("ProductionTask_productionTaskList");    
                            }
                            break;
                    }
                }
            }
        }


        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            //флаг активности текстового ввода
            //когда курсор стоит в поле ввода (например, поиск)
            //мы запрещаем обрабатывать такие клавиши, как Del, Ins etc
            bool inputActive = false;
            if (SearchText.IsFocused)
            {
                inputActive = true;
            }

            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.F5:
                    TaskGrid.LoadItems();
                    e.Handled = true;
                    break;

                case Key.Delete:
                    if (!inputActive)
                    {
                        Delete();
                    }
                    e.Handled = true;
                    break;

                case Key.Insert:
                    if (!inputActive)
                    {
                        Create();
                    }
                    e.Handled = true;
                    break;
            }
        }



        /// <summary>
        /// отображение статьи в справочной системе
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/production/production_tasks/list");
        }

        /// <summary>
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "ProductionTask",
                ReceiverName = "",
                SenderName = "ProductionTaskListView",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            TaskGrid.Destruct();
            PositionGrid.Destruct();
        }

        /// <summary>
        /// инициализация грида "производственные задания"
        /// </summary>
        public void TaskGridInit()
        {
            //инициализация грида
            {
                //список колонок грида
                var columns = new List<DataGridHelperColumn>()
                {
                    new DataGridHelperColumn()
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        Doc="Номер по порядку в списке",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        MinWidth=50,
                        MaxWidth=70,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Номер ПЗ",
                        Path="PRODUCTIONTASKNUMBER",
                        Doc="Номер ПЗ",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=50,
                        MaxWidth=80,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                (Dictionary<string, string> row) =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    bool noShipment=false;


                                    if( row.CheckGet("TASK1TASKID").ToInt() !=0 )
                                    {
                                        if( row.CheckGet("TASK1POSITIONID").ToInt() == 0 )
                                        {
                                            noShipment=true;
                                        }
                                    }

                                    if( row.CheckGet("TASK2TASKID").ToInt() !=0 )
                                    {
                                        if( row.CheckGet("TASK2POSITIONID").ToInt() == 0 )
                                        {
                                            noShipment=true;
                                        }
                                    }
                                                
                                    //нет заявки -> нет отгрузки
                                    if( noShipment )
                                    {
                                        color = HColor.Violet;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Дата создания",
                        Path="CREATED",
                        Doc="Дата создания ПЗ",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm",
                        MinWidth =95,
                        MaxWidth=95,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                (Dictionary<string, string> row) =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    //признак "ехать на горячую"
                                    if (
                                        row.CheckGet("TASK1RUNHOT").ToInt()==1
                                        || row.CheckGet("TASK2RUNHOT").ToInt()==1
                                    )
                                    {
                                        color = HColor.Red;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Завершение",
                        Path="TASKFINISH",
                        Doc="Дата завершения выполнения ПЗ",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                        Format="dd.MM HH:mm",
                        MinWidth=70,
                        MaxWidth=70,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Длина, м",
                        Path="LENGTH",
                        Doc="Длина ПЗ, метры",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        MinWidth=50,
                        MaxWidth=50,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                (Dictionary<string, string> row) =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";
                                    bool deviation = false;

                                    // Для выполненных заданий проверяем отсканированное количество
                                    // Если на каком-либо из стекеров отклонение превышает 5%, ячейку подсвечиваем красным
                                    if (row["POSTING"].ToInt() == 1)
                                    {
                                        var qty1 = row.CheckGet("TASK1QUANTITY").ToDouble();
                                        if (qty1 > 0)
                                        {
                                            var scanned1 = row.CheckGet("TASK1SCANNED").ToDouble();
                                            if ((scanned1 / qty1) < 0.95 )
                                            {
                                                deviation = true;
                                            }
                                        }

                                        var qty2 = row.CheckGet("TASK2QUANTITY").ToDouble();
                                        if (qty2 > 0)
                                        {
                                            var scanned2 = row.CheckGet("TASK2SCANNED").ToDouble();
                                            if ((scanned2 / qty2) < 0.95 )
                                            {
                                                deviation = true;
                                            }
                                        }
                                    }

                                    if (deviation)
                                    {
                                        color= HColor.Red;
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Профиль",
                        Path="PROFILENAME",
                        Doc="Название профиля картона, используемого в ПЗ",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=50,
                        MaxWidth=50,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="ИД профиля",
                        Path="PROFILEID",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Формат",
                        Path="FORMAT",
                        Doc="Формат, ширина полотна материала, мм.",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        MinWidth=50,
                        MaxWidth=50,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Обрезь, %",
                        Path="TRIMPERCENTAGE",
                        Doc="Процент обрези (от ширины полотна, формата)",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Double,
                        MinWidth=50,
                        MaxWidth=50,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                (Dictionary<string, string> row) =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Обрезь больше 10% - слишком большая
                                    if(row["TRIMPERCENTAGE"].ToDouble() >= 10.0)
                                    {
                                        color = HColor.Red;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Качество",
                        Path="SOURCECOMPOSITIONID",
                        Doc="Идентификатор качества сырья гофроагрегата",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=70,
                        MaxWidth=70,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="ГА",
                        Path="CORRUGATORNAME",
                        Doc="Гофроагрегат",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=50,
                        MaxWidth=50,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Станки",
                        Path="MACHINES",
                        Doc="Станки, участвующие в переработке заготовок",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=40,
                        MaxWidth=100,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                (Dictionary<string, string> row) =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    //фиолетовый -- z-картон
                                    if(row.ContainsKey("ZCARDBOARD"))
                                    {
                                        if(row["ZCARDBOARD"].ToBool() )
                                        {
                                            color = HColor.Violet;
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Примечание",
                        Path="NOTE",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=40,
                        MaxWidth=300,
                        FormatterRaw= (v) =>
                        {
                            var result = v.CheckGet("NOTE").ToString();
                            

                            var b=v.CheckGet("MACHINE_ERROR").ToBool();

                            if(b)
                            {
                                // FIXME: временно убираю ошибку "DU 0000 0112"
                                var code = v.CheckGet("MACHINE_ERROR_CODE");
                                if(code=="DU 0000 0112")
                                {
                                    return result;
                                }

                                result=$"Ошибка: {v.CheckGet("MACHINE_ERROR_CODE").ToString()}";
                            }

                            return result;
                        },
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Сырье",
                        Path="CARDBOARDNAME",
                        Doc="Марка картона, если картон обоих заданий одинаковый",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=100,
                        MaxWidth=200,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Тип рилевки",
                        Path="_CREASETYPE",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        FormatterRaw= (v) =>
                        {
                            /*
                                Тип рилёвки для заготовки,
                                1 = СТРОГО 3 точки(в простонародье папа-мама), 
                                2 = СТРОГО плоская рилёвка, 
                                3 = НЕВАЖНО, т.е. будем смотреть по ситуации по конкретному заданию, 
                                    Если отсутствует признак "симметричная рилевка" и сумма рилевок 0, то рилевок нет,
                                    ничего не отображаем вне зависимости от значения в поле TASK1CREASETYPE
                                4 - папа-папа (3 точки, смещение = 1)
                             */

                            var result = "";
                            var c1="";
                            {
                                switch(v.CheckGet("TASK1CREASETYPE").ToInt())
                                {
                                    case 1:
                                        c1="п/м";
                                        break;

                                    case 2:
                                        c1="пл";
                                        break;

                                    case 4:
                                        c1="п/п";
                                        break;
                                }
                                if ((v.CheckGet("TASK1CREASESYMMETRIC").ToInt() == 0) && (v.CheckGet("TASK1CREASESUMM").ToInt() == 0))
                                {
                                    c1 = "";
                                }
                                if(!string.IsNullOrEmpty(c1))
                                {
                                    result=$"{result}{c1} ";
                                }
                            }
                            var c2="";
                            {
                                switch(v.CheckGet("TASK2CREASETYPE").ToInt())
                                {
                                    case 1:
                                        c2="п/м";
                                        break;

                                    case 2:
                                        c2="пл";
                                        break;

                                    case 4:
                                        c2="п/п";
                                        break;
                                }
                                if ((v.CheckGet("TASK2CREASESYMMETRIC").ToInt() == 0) && (v.CheckGet("TASK2CREASESUMM").ToInt() == 0))
                                {
                                    c2 = "";
                                }
                                if(!string.IsNullOrEmpty(c2))
                                {
                                    result=$"{result}{c2}";
                                }
                            }

                            return result;
                        },
                        MinWidth=40,
                        MaxWidth=40,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.FontWeight,
                                (Dictionary<string, string> v) =>
                                {
                                    var fontWeight= new FontWeight();

                                    /*
                                        Тип рилёвки для заготовки,
                                        1 = СТРОГО 3 точки(в простонародье папа-мама), 
                                        2 = СТРОГО плоская рилёвка, 
                                        3 = НЕВАЖНО, т.е. будем смотреть по ситуации по конкретному заданию, 
                                        4 - папа-папа (3 точки, смещение = 1)
                                     */

                                    if(
                                        v.CheckGet("TASK1CREASETYPE").ToInt()==2
                                        || v.CheckGet("TASK2CREASETYPE").ToInt()==2
                                    )
                                    {
                                        fontWeight=FontWeights.Bold;
                                    }

                                    return fontWeight;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn()
                    {
                        Header="План ГА",
                        Path="INPLANNINGQUEUE",
                        Doc="находится в плане на гофроагрегат",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        MinWidth=30,
                        MaxWidth=50,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Блокировка",
                        Path="WORK",
                        Doc="",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        FormatterRaw= (v) =>
                        {
                            var result = false;
                            if(v.CheckGet("WORK").ToInt()==1)
                            {
                                result=false;
                            }
                            else
                            {
                                result=true;
                            }
                            return result;
                        },
                        MinWidth=30,
                        MaxWidth=50,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Очередь ГА",
                        Path="INSTACK",
                        Doc="находится в рабочей очереди гофроагрегата",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        MinWidth=30,
                        MaxWidth=50,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Дубль",
                        Path="_DUPLICATED",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        MinWidth=30,
                        MaxWidth=50,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Создатель",
                        Path="_CREATOR_NAME",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=100,
                        MaxWidth=200,
                        FormatterRaw= (row) =>
                        {
                            var v = row.CheckGet("USER_NAME");
                            if(string.IsNullOrEmpty(v))
                            {
                                v=row.CheckGet("CREATOR");
                            }
                            return v;
                        },
                    },
                    new DataGridHelperColumn()
                    {
                        Header="CREATOR",
                        Path="CREATOR",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Авто",
                        Path="AUTOCUTTING",
                        Doc="Признак, создано алгоритмом автораскроя",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        MinWidth=30,
                        MaxWidth=50,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Ошибка ГА",
                        Path="MACHINE_ERROR",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        MinWidth=30,
                        MaxWidth=50,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Тандем",
                        Path="TANDEM",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        MinWidth=30,
                        MaxWidth=50,
                    },

                    new DataGridHelperColumn()
                    {
                        Header="ИД ПЗ",
                        Path="PRODUCTIONTASKID",
                        Doc="ИД производственного задания",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        MinWidth=50,
                        MaxWidth=80,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Артикул на стекере 1",
                        Path="TASK1ART",
                        Doc="",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=10,
                        MaxWidth=10,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Изделие на стекере 1",
                        Path="TASK1NAME",
                        Doc="",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=10,
                        MaxWidth=10,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Артикул на стекере 2",
                        Path="TASK2ART",
                        Doc="",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=10,
                        MaxWidth=10,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Изделие на стекере 2",
                        Path="TASK2NAME",
                        Doc="",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=10,
                        MaxWidth=10,
                    },

                    new DataGridHelperColumn()
                    {
                        Header="Task1PositionId",
                        Path="TASK1POSITIONID",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Task2PositionId",
                        Path="TASK2POSITIONID",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Приход",
                        Path="PRIHOD_FLAG",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Выполнено",
                        Path="POSTING",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Сообщение об ошибке",
                        Path="MACHINE_ERROR_TEXT",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Перевыгон",
                        Path="REWORK_FLAG",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        Hidden=true,
                    },

                };
                TaskGrid.SetColumns(columns);
                TaskGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    {
                        StylerTypeRef.BackgroundColor,
                        (Dictionary<string, string> row) =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            //голубой -- задание в очереди на ГА
                            if (row.CheckGet("INSTACK").ToBool()==true)
                            {
                                color = HColor.Blue;
                            }

                            //зеленый -- задание выполнено
                            if (row.CheckGet("POSTING").ToInt() == 1)
                            {
                                color = HColor.Green;
                            }

                            //серый -- заблокировано
                            if(row.CheckGet("WORK").ToInt()!=1)
                            {
                                color = HColor.Gray;
                            }

                            //желтый -- проблемные задания:
                            //  не лист (тип изделия: idk1!=4 => листы)
                            //  нет ПЗ на переработку, 
                            var yellowFlag=false;
                            {
                                {
                                    bool noProcessingTask=false;

                                    if(row.CheckGet("TASK1CATEGORYID").ToInt()==4)
                                    {
                                        if (row.ContainsKey("TASK1STACKER"))
                                        {
                                            if( row["TASK1STACKER"].ToInt() == 1 )
                                            {
                                                if (row.ContainsKey("TASK1NEXTPOSITIONID"))
                                                {
                                                    if( row["TASK1NEXTPOSITIONID"].ToInt() == 0 )
                                                    {
                                                        noProcessingTask=true;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if(row.CheckGet("TASK2CATEGORYID").ToInt()==4)
                                    {
                                        if (row.ContainsKey("TASK2STACKER"))
                                        {
                                            if( row["TASK2STACKER"].ToInt() == 2 )
                                            {
                                                if (row.ContainsKey("TASK2NEXTPOSITIONID"))
                                                {
                                                    if( row["TASK2NEXTPOSITIONID"].ToInt() == 0 )
                                                    {
                                                        noProcessingTask=true;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if(noProcessingTask)
                                    {
                                        yellowFlag=true;
                                    }

                                }
                            }

                            if(yellowFlag)
                            {
                                color = HColor.Yellow;
                            }

                            //ошибки ГА
                            if(row.CheckGet("MACHINE_ERROR").ToBool())
                            {
                                var code = row.CheckGet("MACHINE_ERROR_CODE");
                                if(code=="DU 0000 0112")
                                {
                                    
                                }
                                else
                                    color = HColor.Red;
                            }

                            // оливковый - очищенные (удалены позиции)
                            if (row.CheckGet("DELETED").ToBool())
                            {
                                color = HColor.Olive;
                            }


                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };

                TaskGrid.PrimaryKey = "PRODUCTIONTASKID";
                TaskGrid.SetSorting("ROWNUMBER", ListSortDirection.Ascending);
                TaskGrid.SearchText = SearchText;
                //TaskGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                TaskGrid.Name="production_task_list2";


                TaskGrid.Init();

                TaskGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    { "edit", new DataGridContextMenuItem(){
                        Header="Изменить",
                        Tag = "access_mode_full_access",
                        Action=()=>
                        {
                            EditTask();
                        }
                    }},
                    { "copy", new DataGridContextMenuItem(){
                        Header="Копия",
                        Tag = "access_mode_full_access",
                        Action=()=>
                        {
                            Copy();
                        }
                    }},

                    { "finish", new DataGridContextMenuItem(){
                        Header="Завершить",
                        Tag = "access_mode_full_access",
                        Action=()=>
                        {
                            Finish();
                        }
                    }},
                    { "finish_cancel", new DataGridContextMenuItem(){
                        Header="Отменить завершение",
                        Tag = "access_mode_full_access",
                        Action=()=>
                        {
                            FinishCancel();
                        }
                    }},
                    { "delete", new DataGridContextMenuItem(){
                        Header="Удалить",
                        Tag = "access_mode_full_access",
                        Action=()=>
                        {
                            Delete();
                        }
                    }},
                    { "s1", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    { "showmap", new DataGridContextMenuItem(){
                        Header="Карта ПЗГА",
                        Tag = "access_mode_full_access",
                        Action=()=>
                        {
                            ShowProductionTaskMap();
                        }
                    }},
                    { "s2", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    { "block", new DataGridContextMenuItem(){
                        Header="Заблокировать",
                        Tag = "access_mode_full_access",
                        Action=()=>
                        {
                            Block();
                        }
                    }},
                    { "unblock", new DataGridContextMenuItem(){
                        Header="Разблокировать",
                        Tag = "access_mode_full_access",
                        Action=()=>
                        {
                            Unblock();
                        }
                    }},
                    { "edit_task_note", new DataGridContextMenuItem(){
                        Header="Изменить примечание",
                        Tag = "access_mode_full_access",
                        Action=()=>
                        {
                            EditTaskNote();
                        }
                    }},
                };

                //при выборе строки в гриде, обновляются актуальные действия для записи
                TaskGrid.OnSelectItem = (Dictionary<string, string> selectedItem) =>
                {
                    if (selectedItem.Count > 0)
                    {
                        TaskGridUpdateActions(selectedItem);
                    }
                };


                //двойной клик на строке откроет форму редактирования
                TaskGrid.OnDblClick = (Dictionary<string, string> selectedItem) =>
                {
                    EditTask();
                };


                //данные грида
                TaskGrid.OnLoadItems = TaskGridLoadItems;
                TaskGrid.OnFilterItems = TaskGridFilterItems;
                TaskGrid.Run();

                //фокус ввода           
                TaskGrid.Focus();
            }
        }

        /// <summary>
        /// инициализация грида "производственные задания"
        /// </summary>
        public void PositionGridInit()
        {
            //список колонок грида
            var columns = new List<DataGridHelperColumn>()
            {
                new DataGridHelperColumn()
                {
                    Header="Стекер",
                    Path="STACKER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=60,
                },
                new DataGridHelperColumn()
                {
                    Header="Наименование",
                    Path="NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=80,
                    MaxWidth=600,
                },
                new DataGridHelperColumn()
                {
                    Header="Артикул",
                    Path="CODE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=120,
                    MaxWidth=120,
                },
                new DataGridHelperColumn()
                {
                    Header="Количество",
                    Path="QUANTITY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=60,
                    MaxWidth=80,
                },
                new DataGridHelperColumn()
                {
                    Header=$"Сканер{Environment.NewLine}Количество произведённой и не отбракованной на ГА продукции",
                    Path="QUANTITYSCANNED",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=60,
                    MaxWidth=80,
                },
                new DataGridHelperColumn()
                {
                    Header="Укладка",
                    Path="GOODSINSTACK",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=80,
                },
                new DataGridHelperColumn()
                {
                    Header="На поддоне",
                    Path="STACKSINPALLET",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=80,
                },
                new DataGridHelperColumn()
                {
                    Header="Поддон",
                    Path="PALLETVARNAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=80,
                },
                new DataGridHelperColumn()
                {
                    Header="Станок",
                    Path="MACHINE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=60,
                },
                new DataGridHelperColumn()
                {
                    Header="Потоков",
                    Path="THREADS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=35,
                    MaxWidth=60,
                },
                new DataGridHelperColumn()
                {
                    Header="Дата окончания производства",
                    Path="PRODUCTIONCOMPLETE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format="dd.MM HH:mm",
                    MinWidth=100,
                    MaxWidth=150,
                },
                new DataGridHelperColumn()
                {
                    Header="Длина",
                    Path="LENGTH",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=60,
                },
                new DataGridHelperColumn()
                {
                    Header="Ширина",
                    Path="WIDTH",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=60,
                },
                new DataGridHelperColumn()
                {
                    Header="Мастер",
                    Path="MASTER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=35,
                    MaxWidth=35,
                },
                new DataGridHelperColumn()
                {
                    Header="Опытная партия",
                    Path="SAMPLE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=35,
                    MaxWidth=35,
                },
                new DataGridHelperColumn()
                {
                    Header="Техкарта",
                    Path="PATHTK",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    FormatterRaw=(v) =>
                    {
                        var result=0;
                        if(!string.IsNullOrEmpty(v.CheckGet("PATHTK")))
                        {
                            result = 1;
                        }
                        return result;
                    },
                    MinWidth=35,
                    MaxWidth=35,
                },
                new DataGridHelperColumn()
                {
                    Header="Стеллажный склад",
                    Path="PLACED_IN_RACK_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=35,
                    MaxWidth=35,
                },
                new DataGridHelperColumn()
                {
                    Header="Перевыгон",
                    Path="REWORK_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=35,
                    MaxWidth=35,
                },
                new DataGridHelperColumn()
                {
                    Header="Примечание переработка",
                    Path="PRODUCTIONTASK_PROCESSING_NOTE",
                    Doc="Примечание к ПЗПР",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=45,
                    MaxWidth=400,
                },
                new DataGridHelperColumn()
                {
                    Header="Заявка",
                    Path="APPLICATION",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=150,
                    MaxWidth=600,
                },
                new DataGridHelperColumn()
                {
                    Header="ИД заявки",
                    Path="IDORDERDATES",
                    Doc="ИД позиции заявки",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=55,
                    MaxWidth=55,
                },
                new DataGridHelperColumn()
                {
                    Header="ИД ПЗПР",
                    Path="PRODUCTIONTASKNEXTID",
                    Doc="ИД ПЗ на переработку",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=55,
                    MaxWidth=55,
                },

                new DataGridHelperColumn()
                {
                    Header="ИД изделия",
                    Path="GOODSID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=80,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="ИД категории",
                    Path="CATEGORYID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=40,
                    Hidden=true,
                },

                new DataGridHelperColumn()
                {
                    Header="Количество ПЗГА с этим ПЗПР",
                    Path="PRODUCTIONTASKNEXTCNT",
                    Doc="Количество ПЗ на ГА с этим же ПЗ на переработку",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=40,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="TLS_ID",
                    Path="TLS_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=80,
                    Hidden=true,
                },
            };
            PositionGrid.SetColumns(columns);

            PositionGrid.SetSorting("STACKER", ListSortDirection.Ascending);
            PositionGrid.Init();

            PositionGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                { "open", new DataGridContextMenuItem(){
                    Header="Перевыгон",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        SetPositionRework();
                    }
                }},
                { "positions", new DataGridContextMenuItem(){
                    Header="Заявки",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        EditPosition();
                    }
                }},
                { "note", new DataGridContextMenuItem(){
                    Header="Примечание для переработки",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        EditNote();
                    }
                }},
                { "placeRack", new DataGridContextMenuItem(){
                    Header="Разместить на стеллажном складе",
                    Tag = "access_mode_full_access",
                    Action=()=>
                    {
                        PlaceRack();
                    }
                }},

                /*
                { "bind", new DataGridContextMenuItem(){
                    Header="Привязать к заявке",
                    Action=()=>
                    {
                        BindApplication();
                    }
                }},

                { "unbind", new DataGridContextMenuItem(){
                    Header="Отвязать от заявки",
                    Action=()=>
                    {
                        UnbindApplication();
                    }
                }},
                */
                { "s2", new DataGridContextMenuItem(){
                    Header="-",
                }},

                { "showtk", new DataGridContextMenuItem(){
                    Header="Техкарта",
                    Action=()=>
                    {
                        ShowProductionMap();
                    }
                }},

            };

            //при выборе строки в гриде, обновляются актуальные действия для записи
            PositionGrid.OnSelectItem = (Dictionary<string, string> selectedItem) =>
            {
                if (selectedItem.Count > 0)
                {
                    PositionGridUpdateActions(selectedItem);
                }
            };

            //двойной клик на строке откроет форму редактирования
            PositionGrid.OnDblClick = (Dictionary<string, string> selectedItem) =>
            {
                EditPosition();
            };

            //данные грида
            PositionGrid.OnLoadItems = PositionGridLoadItems;
            PositionGrid.PrimaryKey = "STACKER";
            PositionGrid.Run();

        }

        /// <summary>
        /// инициализация таблицы со списком сырья для выбранного задания
        /// </summary>
        public void SourceGridInit()
        {
            var columns = new List<DataGridHelperColumn>()
            {
                new DataGridHelperColumn()
                {
                    Header="Слой",
                    Path="LAYER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=35,
                    MaxWidth=35,
                },
                new DataGridHelperColumn()
                {
                    Header="Группа",
                    Path="NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=120,
                    MaxWidth=180,
                },
                new DataGridHelperColumn()
                {
                    Header="ИД",
                    Path="ID_RAW_GROUP",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=35,
                    MaxWidth=35,
                },
                new DataGridHelperColumn()
                {
                    Header="Вес, кг",
                    Path="WEIGHT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=55,
                    MaxWidth=55,
                },
            };
            SourceGrid.SetColumns(columns);
            SourceGrid.SetSorting("LAYER", ListSortDirection.Ascending);
            // Запрет на изменение сортировки в таблице
            SourceGrid.UseSorting = false;
            SourceGrid.Init();


        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            ProductionTasksDS = new ListDataSet();
            PositionsDS = new ListDataSet();
            CardboardDS = new ListDataSet();
            ProfileDS = new ListDataSet();
            FormatDS = new ListDataSet();
            CreatorDS = new ListDataSet();
            MachineDS = new ListDataSet();

            //значения полей по умолчанию
            {
                Today.Text = DateTime.Now.ToString("dd.MM.yyyy");

                {
                    var list = new Dictionary<string, string>();
                    list.Add("-1", "Все");
                    list.Add("0", "Не выполнено");
                    list.Add("6", "Заблокировано");
                    list.Add("1", "Не в очереди");
                    list.Add("2", "В очереди");
                    list.Add("3", "Выполнено");
                    list.Add("4", "Не привязано к отгрузке");
                    list.Add("5", "Без задания на переработку");
                    list.Add("7", "Опытная партия");
                    list.Add("8", "Похожие задания");
                    list.Add("9", "Ошибки ГА");
                    list.Add("11", "Перевыгон");
                    list.Add("10", "Удаленные");


                    Statuses.Items = list;
                    Statuses.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
                }

                {
                    var list = new Dictionary<string, string>();
                    list.Add("-1", "Все");

                    Profile.Items = list;
                    Profile.SelectedItem = list.FirstOrDefault((x) => x.Key == "-1");
                }

            }

        }

        /// <summary>
        /// получение записей
        /// </summary>
        public async void TaskGridLoadItems()
        {
            TaskGridDisableControls();

            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>()
                {
                    { "TODAY", Today.Text },
                    { "FACTORY_ID", Factory.SelectedItem.Key },
                };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ProductionTask");
                q.Request.SetParam("Action", "List");
                q.Request.Timeout = Central.Parameters.RequestTimeoutMax;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

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
                        //Очищаем подчиненные таблицы
                        PositionGrid.ClearItems();
                        PositionGrid.SelectedItem.Clear();
                        SourceGrid.ClearItems();

                        var itemsDs = ListDataSet.Create(result, "ITEMS");
                        TaskGrid.UpdateItems(itemsDs);

                        var totalsDs = ListDataSet.Create(result, "TOTALS");

                        TotalLength.Text = "";
                        var l = totalsDs.GetFirstItemValueByKey("LENGTH").ToInt();
                        if (l > 0)
                        {
                            TotalLength.Text = l.ToString();
                        }

                        CreatorDS = ListDataSet.Create(result, "CREATORS");
                        if (CreatorDS.Items.Count > 0)
                        {
                            var list = new Dictionary<string, string>();
                            list.Add("", "");
                            foreach (Dictionary<string, string> row in CreatorDS.Items)
                            {
                                var k = row.CheckGet("NAME");
                                var v = row.CheckGet("FULL_NAME");
                                if (row.CheckGet("ID").ToInt() == 0)
                                {
                                    v = row.CheckGet("NAME");
                                }

                                list.Add(k, v);
                            }
                            Creator.Items = list;
                            Creator.SelectedItem = list.FirstOrDefault((x) => x.Key == "");
                        }
                    }
                    RefreshButton.Style = (Style)RefreshButton.TryFindResource("Button");
                }
            }

            TaskGridEnableControls();
        }



        /// <summary>
        /// получение записей
        /// </summary>
        public async void PositionGridLoadItems()
        {
            PositionGridDisableControls();

            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();

                {
                    p.Add("ID", SelectedItemId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Position");
                q.Request.SetParam("Action", "List");
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

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
                        if (result.ContainsKey("ITEMS"))
                        {
                            var ds = (ListDataSet)result["ITEMS"];
                            ds.Init();
                            PositionGrid.UpdateItems(ds, false);
                        }

                        if (result.ContainsKey("LAYERS"))
                        {
                            var lds = ListDataSet.Create(result, "LAYERS");
                            SourceGrid.UpdateItems(lds);
                        }
                    }
                }
            }

            PositionGridEnableControls();
        }

        public void TaskGridDisableControls()
        {
            TaskGridToolbar.IsEnabled = false;
            TaskFilterToolbar.IsEnabled = false;
            TaskGrid.ShowSplash();

            PositionGridDisableControls();
        }

        public void TaskGridEnableControls()
        {
            TaskGridToolbar.IsEnabled = true;
            TaskFilterToolbar.IsEnabled = true;
            TaskGrid.HideSplash();

            PositionGridEnableControls();
        }

        public void PositionGridDisableControls()
        {
            PositionGridToolbar.IsEnabled = false;
            PositionGrid.ShowSplash();
        }

        public void PositionGridEnableControls()
        {
            PositionGridToolbar.IsEnabled = true;
            PositionGrid.HideSplash();
        }

        /// <summary>
        /// фильтрация записей
        /// </summary>
        public async void TaskGridFilterItems()
        {
            DuplicatedTaskQuantity = 0;
            DuplicatedQuantityLabel.Visibility = Visibility.Collapsed;
            DuplicatedQuantity.Visibility = Visibility.Collapsed;


            if (TaskGrid.GridItems != null)
            {
                if (TaskGrid.GridItems.Count > 0)
                {

                    //обработка строк                    
                    foreach (Dictionary<string, string> row in TaskGrid.GridItems)
                    {

                        //не привязано к отгрузке
                        {
                            bool noShipment = false;

                            if (row.CheckGet("TASK1STACKER").ToInt() == 1)
                            {
                                if (row.CheckGet("TASK1POSITIONID").ToInt() == 0)
                                {
                                    noShipment = true;
                                }
                            }

                            if (row.CheckGet("TASK2STACKER").ToInt() == 2)
                            {
                                if (row.CheckGet("TASK2POSITIONID").ToInt() == 0)
                                {
                                    noShipment = true;
                                }
                            }

                            row.CheckAdd("NO_SHIPMENT", noShipment.ToInt().ToString());
                        }

                        //нет задания на переработку
                        {
                            bool noProcessingTask1 = false;
                            bool noProcessingTask2 = false;

                            if (row.CheckGet("TASK1NEXTPOSITIONID").ToInt() == 0)
                            {
                                noProcessingTask1 = true;
                            }

                            if (row.CheckGet("TASK2NEXTPOSITIONID").ToInt() == 0)
                            {
                                noProcessingTask2 = true;
                            }

                            var b = false;
                            if (noProcessingTask1 && noProcessingTask2)
                            {
                                b = true;
                            }
                            row.CheckAdd("NO_PROCESSING_TASK", b.ToInt().ToString());
                        }

                        //нет задания на переработку 2
                        {
                            //желтый флаг -- проблемные задания:
                            //  не лист (тип изделия: idk1!=4 => листы)
                            //  нет ПЗ на переработку, 
                            var yellowFlag = false;
                            {
                                bool noProcessingTask = false;

                                if (row.CheckGet("TASK1CATEGORYID").ToInt() == 4)
                                {
                                    if (row.ContainsKey("TASK1STACKER"))
                                    {
                                        if (row["TASK1STACKER"].ToInt() == 1)
                                        {
                                            if (row.ContainsKey("TASK1NEXTPOSITIONID"))
                                            {
                                                if (row["TASK1NEXTPOSITIONID"].ToInt() == 0)
                                                {
                                                    noProcessingTask = true;
                                                }
                                            }
                                        }
                                    }
                                }

                                if (row.CheckGet("TASK2CATEGORYID").ToInt() == 4)
                                {
                                    if (row.ContainsKey("TASK2STACKER"))
                                    {
                                        if (row["TASK2STACKER"].ToInt() == 2)
                                        {
                                            if (row.ContainsKey("TASK2NEXTPOSITIONID"))
                                            {
                                                if (row["TASK2NEXTPOSITIONID"].ToInt() == 0)
                                                {
                                                    noProcessingTask = true;
                                                }
                                            }
                                        }
                                    }
                                }

                                if (noProcessingTask)
                                {
                                    yellowFlag = true;
                                }

                            }

                            row.CheckAdd("YELLOW_FLAG", yellowFlag.ToInt().ToString());
                        }

                        //заливная печать
                        /*
                        {
                            if(!row.ContainsKey("FILLPRINTING"))
                            {
                                row.Add("FILLPRINTING","0");
                            }

                            var fp1 = 0;
                            if(row.ContainsKey("TASK1FILLPRINTING"))
                            {
                                fp1=row["TASK1FILLPRINTING"].ToInt();
                            }

                            var fp2 = 0;
                            if(row.ContainsKey("TASK2FILLPRINTING"))
                            {
                                fp2=row["TASK2FILLPRINTING"].ToInt();
                            }

                            if(fp1==1 || fp2==1)
                            {
                                row["FILLPRINTING"]="1";
                            }
                        }
                        */

                        //несимметричные рилевки
                        {
                            if (!row.ContainsKey("CREASENONSYMMETRIC"))
                            {
                                row.Add("CREASENONSYMMETRIC", "0");
                            }

                            var ns1 = 0;
                            if (row.ContainsKey("TASK1CREASESYMMETRIC") && row.ContainsKey("TASK1CREASESUMM"))
                            {
                                if (row["TASK1CREASESYMMETRIC"].ToInt() == 0 && row["TASK1CREASESUMM"].ToInt() > 0)
                                {
                                    ns1 = 1;
                                }
                            }

                            var ns2 = 0;
                            if (row.ContainsKey("TASK2CREASESYMMETRIC") && row.ContainsKey("TASK2CREASESUMM"))
                            {
                                if (row["TASK2CREASESYMMETRIC"].ToInt() == 0 && row["TASK2CREASESUMM"].ToInt() > 0)
                                {
                                    ns2 = 1;
                                }
                            }

                            if (ns1 == 1 || ns2 == 1)
                            {
                                row["CREASENONSYMMETRIC"] = "1";
                            }
                        }

                        //принято в работу
                        {
                            if (!row.ContainsKey("ACCEPTEDTOPRODUCTION"))
                            {
                                row.Add("ACCEPTEDTOPRODUCTION", "0");
                            }


                            bool acceptedToProduction = false;
                            bool actionDelete = false;

                            if (row.ContainsKey("POSTING"))
                            {
                                if (row["POSTING"].ToInt() == 0)
                                {
                                    acceptedToProduction = false;
                                    actionDelete = true;
                                }
                                else if (row["POSTING"].ToInt() == 1)
                                {
                                    acceptedToProduction = true;
                                    actionDelete = false;
                                }
                            }

                            row["ACCEPTEDTOPRODUCTION"] = acceptedToProduction.ToInt().ToString();

                        }

                        //нестандартный поддон
                        /*
                        {
                            if(!row.ContainsKey("PALLETNONSTANDART"))
                            {
                                row.Add("PALLETNONSTANDART","0");
                            }

                            if(row["PALLETSTANDART"].ToInt() != 1)
                            {
                                row["PALLETNONSTANDART"]="1";
                            }
                            else
                            {
                                row["PALLETNONSTANDART"]="0";
                            }
                        }
                        */

                        /*
                        //упаковка поддонов
                        {
                            if(!row.ContainsKey("PALLETEPACKING"))
                            {
                                row.Add("PALLETEPACKING","0");
                            }

                            if(!row.ContainsKey("PALLETENOPACKING"))
                            {
                                row.Add("PALLETENOPACKING","0");
                            }

                            var p1 = 0;
                            var np1 = 0;
                            if(row.ContainsKey("TASK1PACKING"))
                            {
                                if(row["TASK1PACKING"]=="с/уп")
                                {
                                    p1=1;
                                }
                                if(row["TASK1PACKING"]=="б/уп")
                                {
                                    np1=1;
                                }
                            }

                            var p2 = 0;
                            var np2 = 0;
                            if(row.ContainsKey("TASK2PACKING"))
                            {
                                if(row["TASK2PACKING"]=="с/уп")
                                {
                                    p2=1;
                                }
                                if(row["TASK2PACKING"]=="б/уп")
                                {
                                    np2=1;
                                }
                            }

                            if(p1==1 || p2==1)
                            {
                                row["PALLETEPACKING"]="1";
                            }
                            if(np1==1 || np2==1)
                            {
                                row["PALLETENOPACKING"]="1";
                            }
                        }
                        */

                        //z-картон
                        {
                            if (!row.ContainsKey("ZCARDBOARD"))
                            {
                                row.Add("ZCARDBOARD", "0");
                            }

                            var z1 = 0;
                            if (row.ContainsKey("TASK1ZCARDBOARD"))
                            {
                                z1 = row["TASK1ZCARDBOARD"].ToInt();
                            }

                            var z2 = 0;
                            if (row.ContainsKey("TASK2ZCARDBOARD"))
                            {
                                z2 = row["TASK2ZCARDBOARD"].ToInt();
                            }

                            if (z1 == 1 || z2 == 1)
                            {
                                row["ZCARDBOARD"] = "1";
                            }

                        }

                        // удаленное задание
                        {
                            row.CheckAdd("DELETED", "0");
                            if ((row.CheckGet("POSTING").ToInt() == 1) && string.IsNullOrEmpty(row.CheckGet("TASK1TASKID")) && string.IsNullOrEmpty(row.CheckGet("TASK2TASKID")))
                            {
                                row["DELETED"] = "1";
                            }
                        }

                        //хеш уникальности
                        {
                            var s = "";
                            s = $"{s}-{row.CheckGet("TASK1GOODSID")}-{row.CheckGet("TASK2GOODSID")}";
                            s = $"{s}-{row.CheckGet("TASK1POSITIONID")}-{row.CheckGet("TASK2POSITIONID")}";
                            s = $"{s}-{row.CheckGet("TASK1QUANTITY")}-{row.CheckGet("TASK2QUANTITY")}";
                            s = $"{s}-{row.CheckGet("LENGTH")}";

                            row.CheckAdd("_UK", s);
                        }


                    }

                    var keysList = new Dictionary<string, int>();

                    //проверка уникальности
                    foreach (Dictionary<string, string> row in TaskGrid.GridItems)
                    {
                        var k = row.CheckGet("_UK");

                        if (!keysList.ContainsKey(k))
                        {
                            keysList.Add(k, 0);
                        }

                        var i = keysList[k];
                        i = i + 1;
                        keysList[k] = i;
                    }

                    foreach (Dictionary<string, string> row in TaskGrid.GridItems)
                    {
                        var k = row.CheckGet("_UK");
                        row.CheckAdd("_DUPLICATED", "0");

                        if (keysList.ContainsKey(k))
                        {
                            var i = keysList[k].ToInt();
                            if (i > 1)
                            {
                                row.CheckAdd("_DUPLICATED", "1");
                                DuplicatedTaskQuantity++;
                            }
                        }
                    }



                    //статус
                    /*
                        -1 Все
                        0  Не выполнено
                        1  Не в очереди
                        2  В очереди
                        3  Выполнено
                        4  Не привязано к отгрузке
                        5  Без задания на переработку
                        6  Заблокировано
                        8  похожие
                        9  ошибки ГА
                        10 удаленные
                     */
                    bool doFilteringByStatus = false;
                    int status = -1;
                    if (Statuses.SelectedItem.Key != null)
                    {
                        doFilteringByStatus = true;
                        status = Statuses.SelectedItem.Key.ToInt();
                    }

                    bool doFilteringByProfile = false;
                    int profileId = Profile.SelectedItem.Key.ToInt();
                    if (profileId > 0)
                    {
                        doFilteringByProfile = true;
                    }

                    bool doFilteringByCardboard = false;
                    int cardboardId = Cardboard.SelectedItem.Key.ToInt();
                    if (cardboardId > 0)
                    {
                        doFilteringByCardboard = true;
                    }

                    bool doFilteringByMachine = false;
                    string machineName = Machine.SelectedItem.Key;
                    if (!string.IsNullOrEmpty(machineName))
                    {
                        doFilteringByMachine = true;
                    }

                    bool doFilteringByFormat = false;
                    int format = Format.SelectedItem.Value.ToInt();
                    if (format != 0)
                    {
                        doFilteringByFormat = true;
                    }

                    bool doFilteringByCreator = false;
                    string creator = Creator.SelectedItem.Key;
                    if (!string.IsNullOrEmpty(creator))
                    {
                        creator = creator.ToLower();
                        doFilteringByCreator = true;
                    }

                    // фильтр по признаку "Тандем"
                    bool doFilteringTandem = false;
                    if ((bool)TandemCheckBox.IsChecked)
                    {
                        doFilteringTandem = true;
                    }

                    if (
                        doFilteringByStatus
                        || doFilteringByProfile
                        || doFilteringByCardboard
                        || doFilteringByMachine
                        || doFilteringByFormat
                        || doFilteringByCreator
                        || doFilteringTandem
                    )
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in TaskGrid.GridItems)
                        {
                            bool includeByStatus = true;
                            bool includeByProfile = true;
                            bool includeByCardboard = true;
                            bool includeByMachine = true;
                            bool includeByFormat = true;
                            bool includeByCreator = true;
                            bool includeTandem = true;

                            if (doFilteringByStatus)
                            {
                                includeByStatus = false;
                                switch (status)
                                {
                                    //-1 Все
                                    default:
                                        includeByStatus = true;
                                        break;

                                    //Не выполнено    
                                    case 0:
                                        if (row.ContainsKey("POSTING"))
                                        {
                                            if (row["POSTING"].ToInt() != 1)
                                            {
                                                includeByStatus = true;
                                            }
                                        }
                                        break;

                                    //Не в очереди
                                    case 1:
                                        if (row.ContainsKey("INSTACK"))
                                        {
                                            if (row["INSTACK"].ToInt() != 1)
                                            {
                                                includeByStatus = true;
                                            }
                                        }
                                        break;

                                    //В очереди
                                    case 2:
                                        if (row.ContainsKey("INSTACK"))
                                        {
                                            if (row["INSTACK"].ToInt() == 1)
                                            {
                                                includeByStatus = true;
                                            }
                                        }
                                        break;

                                    //Выполнено    
                                    case 3:
                                        if (row.ContainsKey("POSTING"))
                                        {
                                            if ((row["POSTING"].ToInt() == 1) && (row.CheckGet("DELETED").ToInt() == 0))
                                            {
                                                includeByStatus = true;
                                            }
                                        }
                                        break;

                                    //Не привязано к отгрузке    
                                    case 4:
                                        if (row.CheckGet("NO_SHIPMENT").ToInt() == 1)
                                        {
                                            includeByStatus = true;
                                        }
                                        break;

                                    //Без задания на переработку
                                    case 5:
                                        if (row.CheckGet("YELLOW_FLAG").ToInt() == 1)
                                        {
                                            includeByStatus = true;
                                        }
                                        break;

                                    //Заблокировано
                                    case 6:
                                        if (
                                            (row.CheckGet("WORK").ToInt() == 0)
                                        //&& (row.CheckGet("INPLANNINGQUEUE").ToInt()==1)
                                        )
                                        {
                                            includeByStatus = true;
                                        }
                                        break;

                                    //опытная партия
                                    case 7:
                                        if (
                                            (row.CheckGet("TASK1SAMPLE").ToBool())
                                            || (row.CheckGet("TASK2SAMPLE").ToBool())
                                        )
                                        {
                                            includeByStatus = true;
                                        }
                                        break;

                                    //похожие
                                    case 8:
                                        if (row.CheckGet("_DUPLICATED").ToBool())
                                        {
                                            includeByStatus = true;
                                        }
                                        break;

                                    //ошибки ГА
                                    case 9:
                                        if (row.CheckGet("MACHINE_ERROR").ToBool())
                                        {
                                            // FIXME: "DU 0000 0112"
                                            var code = row.CheckGet("MACHINE_ERROR_CODE");
                                            if (code == "DU 0000 0112")
                                            {

                                            }
                                            else
                                                includeByStatus = true;
                                        }
                                        break;
                                    // удаленные
                                    case 10:
                                        if (row.CheckGet("DELETED").ToInt() == 1)
                                        {
                                            includeByStatus = true;
                                        }
                                        break;
                                    // перевыгон
                                    case 11:
                                        if (row.CheckGet("REWORK_FLAG").ToInt() == 1)
                                        {
                                            includeByStatus = true;
                                        }
                                        break;
                                }
                            }

                            if (doFilteringByProfile)
                            {
                                includeByProfile = false;
                                if (row.CheckGet("PROFILEID").ToInt() == profileId)
                                {
                                    includeByProfile = true;
                                }
                            }

                            if (doFilteringByCardboard)
                            {
                                includeByCardboard = false;
                                if (row.CheckGet("CARDBOARDID").ToInt() == cardboardId)
                                {
                                    includeByCardboard = true;
                                }
                            }

                            if (doFilteringByMachine)
                            {
                                //includeByMachine = false;
                                //if(row.CheckGet("MACHINES").IndexOf(machineName)>-1)
                                //{
                                //    includeByMachine = true;
                                //}
                                switch (machineName)
                                {
                                    default:
                                        includeByMachine = false;
                                        if (row.CheckGet("MACHINES").IndexOf(machineName) > -1)
                                        {
                                            includeByMachine = true;
                                        }
                                        break;
                                    case "0":
                                        includeByMachine = true;
                                        break;
                                }
                            }

                            if (doFilteringByFormat)
                            {
                                includeByFormat = false;
                                if (row.CheckGet("FORMAT").ToInt() == format)
                                {
                                    includeByFormat = true;
                                }
                            }

                            if (doFilteringByCreator)
                            {
                                includeByCreator = false;
                                if (row.CheckGet("CREATOR").ToLower() == creator)
                                {
                                    includeByCreator = true;
                                }
                            }

                            if (doFilteringTandem)
                            {
                                includeTandem = false;
                                if (row.CheckGet("TANDEM").ToBool())
                                {
                                    includeTandem = true;
                                }
                            }

                            if (
                                includeByStatus
                                && includeByProfile
                                && includeByCardboard
                                && includeByMachine
                                && includeByFormat
                                && includeByCreator
                                && includeTandem
                            )
                            {
                                items.Add(row);
                            }
                        }
                        TaskGrid.GridItems = items;




                    }

                    //итоги
                    {
                        //Длина заданий
                        //это длина заданий по тем строкам, которые сейчас на экране
                        int totalLength = 0;
                        foreach (Dictionary<string, string> row in TaskGrid.GridItems)
                        {
                            totalLength = totalLength + row.CheckGet("LENGTH").ToInt();
                        }
                        TotalLength2.Text = totalLength.ToString();
                    }

                    //похожие задания
                    {
                        if (DuplicatedTaskQuantity > 0)
                        {
                            DuplicatedQuantityLabel.Visibility = Visibility.Visible;
                            DuplicatedQuantity.Visibility = Visibility.Visible;
                            DuplicatedQuantity.Text = DuplicatedTaskQuantity.ToString();
                        }

                    }
                }


                if (TaskGrid.GridItems.Count == 0)
                {
                    PositionGrid.ClearItems();
                    SourceGrid.ClearItems();
                }
            }
        }

        /// <summary>
        /// загрузка вспомогательных данных для построения интерфейса
        /// </summary>
        public async void LoadRef()
        {
            Profile.IsEnabled = false;

            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Cutter");
                q.Request.SetParam("Action", "GetSources");
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

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
                            ProfileDS = ListDataSet.Create(result, "PROFILES");
                            var list = new Dictionary<string, string>();
                            list.Add("0", "");
                            list.AddRange<string, string>(ProfileDS.GetItemsList("ID", "NAME"));
                            Profile.Items = list;
                            Profile.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
                        }

                        {
                            CardboardDS = ListDataSet.Create(result, "CARDBOARD");
                            var list = new Dictionary<string, string>();
                            list.Add("0", "");
                            list.AddRange<string, string>(CardboardDS.GetItemsList("ID", "NAME"));
                            Cardboard.Items = list;
                            Cardboard.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
                        }
                        {
                            FormatDS = ListDataSet.Create(result, "FORMATS");
                            var list = new Dictionary<string, string>();
                            list.Add("0", "");
                            list.AddRange<string, string>(FormatDS.GetItemsList("PAWI_ID", "WIDTH"));
                            Format.Items = list;
                            Format.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
                        }

                        {
                            var factoryDS = ListDataSet.Create(result, "FACTORY");
                            Factory.Items = factoryDS.GetItemsList("ID", "NAME");
                            Factory.SetSelectedItemByKey("1");
                        }

                        {
                            MachineDS = ListDataSet.Create(result, "MACHINES");
                            SetMachineItems();
                        }

                    }
                }
            }

            Profile.IsEnabled = true;
        }

        /// <summary>
        /// показать карту пз
        /// (печатная форма для ГА)
        /// </summary>
        public async void ShowProductionTaskMap()
        {
            TaskGridToolbar.IsEnabled = false;
            TaskGrid.ShowSplash();

            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();

                {
                    p.Add("ID", SelectedItemId.ToString());
                    p.Add("TEMP_FILE", "1");
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ProductionTask");
                q.Request.SetParam("Action", "TaskGetMap");

                q.Request.SetParams(p);

                q.Request.Timeout = 10000;
                q.Request.Attempts= 1;

                q.Request.Timeout = Central.Parameters.RequestTimeoutMin;
                //q.Request.Attempts=;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    Central.OpenFile(q.Answer.DownloadFilePath);
                }
                else
                {
                    q.ProcessError();
                }
            }

            TaskGridToolbar.IsEnabled = true;
            TaskGrid.HideSplash();
        }

        /// <summary>
        /// показать техкарту
        /// </summary>
        public void ShowProductionMap()
        {
            if (SelectedItem2 != null)
            {
                var p = SelectedItem2.CheckGet("PATHTK").ToString();
                if (!string.IsNullOrEmpty(p))
                {
                    Central.OpenFile(p);
                }
            }
        }

        /// <summary>
        /// привязка к заявке
        /// </summary>
        public void BindApplication()
        {
            if (SelectedItem2 != null)
            {
                if (SelectedItem2.ContainsKey("GOODSID")
                    && SelectedItem2.ContainsKey("PRODUCTIONTASKID")
                )
                {
                    var applicationBind = new ApplicationBind();
                    applicationBind.FactoryId = Factory.SelectedItem.Key.ToInt();
                    applicationBind.ApplicationId = SelectedItem2["IDORDERDATES"].ToInt();
                    applicationBind.GoodsId = SelectedItem2["GOODSID"].ToInt();
                    applicationBind.ProductionTaskId = SelectedItem2["PRODUCTIONTASKID"].ToInt();
                    applicationBind.Grid.LoadItems();
                    applicationBind.BackTabName = "ProductionTask_productionTaskList";
                    applicationBind.Show();
                }
            }
        }

        /// <summary>
        /// отвязка от заявки
        /// </summary>
        public async void UnbindApplication()
        {
            bool resume = true;

            int productionTaskId = 0;
            int goodsId = 0;

            if (resume)
            {
                if (SelectedItem2 != null)
                {
                    productionTaskId = SelectedItem2.CheckGet("PRODUCTIONTASKID").ToInt();
                    goodsId = SelectedItem2.CheckGet("GOODSID").ToInt();
                }

                if (productionTaskId == 0 || goodsId == 0)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                string msg = "";
                msg += $"Отвязать от заявки?";
                msg += $"\nЗаявка: {SelectedItem2["APPLICATION"]}";
                var d = new DialogWindow($"{msg}", "Отвязка ПЗ", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() != true)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                TaskGridToolbar.IsEnabled = false;
                TaskGrid.ShowSplash();

                var p = new Dictionary<string, string>();

                {
                    p.Add("PRODUCTION_TASK_ID", productionTaskId.ToString());
                    p.Add("GOODS_ID", goodsId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ProductionTask");
                q.Request.SetParam("Action", "UnbindApplication");

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                var complete = false;
                int itemId = 0;

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        if (result.ContainsKey("ITEMS"))
                        {
                            var ds = (ListDataSet)result["ITEMS"];
                            ds.Init();

                            itemId = ds.GetFirstItemValueByKey("PRODUCTION_TASK_ID").ToInt();
                            if (itemId != 0)
                            {
                                complete = true;
                            }
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }

                if (complete)
                {
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "ProductionTask",
                        ReceiverName = "TaskList",
                        SenderName = "ProductionTaskListView",
                        Action = "Refresh",
                        Message = $"{itemId}",
                    });
                }
                else
                {
                    var msg = "Не удалось отвязать задание";
                    var d = new DialogWindow($"{msg}", "Привязка задания", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }

                Mouse.OverrideCursor = null;
                TaskGridToolbar.IsEnabled = true;
                TaskGrid.HideSplash();
            }
        }

        /// <summary>
        /// удаление записи
        /// </summary>
        public async void Delete()
        {
            bool resume = true;

            if (resume)
            {
                if (SelectedItem == null)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                string msg = "";
                msg += $"Удалить производственное задание?";
                msg += $"\n{SelectedItem["PRODUCTIONTASKNUMBER"]}";
                var d = new DialogWindow($"{msg}", "Удаление задания", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() != true)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                TaskGridToolbar.IsEnabled = false;
                TaskGrid.ShowSplash();

                var p = new Dictionary<string, string>();

                {
                    p.Add("ID", SelectedItemId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ProductionTask");
                q.Request.SetParam("Action", "Delete");

                q.Request.Timeout = 120000;

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status != 0)
                {
                    var message = "";

                    if (q.Answer.Error.Description.IndexOf("PRCQ_PROIZ_ZAD_GA_FK)") > -1)
                    {
                        message = $"{message}\n У данного ПЗГА есть ПЗ на переработку, которые находятся в очереди планирования.";
                        message = $"{message}\n Чтобы удалить ПЗГА нужно убрать ПЗ на переработку из плана.";
                    }

                    // ПЗГА уже в очереди
                    if (q.Answer.Error.Code == 147)
                    {
                        message = q.Answer.Error.Message;
                    }

                    if (!string.IsNullOrEmpty(message))
                    {
                        var error = new Error();
                        error.Code = 146;
                        error.Message = message;
                        //error.Description=q.Answer.Error.Description;
                        Central.ProcError(error, "", true, q);
                    }
                    else
                    {
                        q.ProcessError();
                    }

                }
                else
                {
                    TaskGrid.LoadItems();
                    PositionGrid.ClearItems();
                }

                Mouse.OverrideCursor = null;
                TaskGridToolbar.IsEnabled = true;
                TaskGrid.HideSplash();
            }
        }

        /// <summary>
        /// завершить
        /// </summary>
        public async void Finish()
        {
            bool resume = true;

            if (resume)
            {
                if (SelectedItem == null)
                {
                    resume = false;
                }
            }

            var id = SelectedItem.CheckGet("PRODUCTIONTASKID").ToInt();
            if (resume)
            {
                if (id == 0)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                string msg = "";
                msg += $"Завершить производственное задание?";
                msg += $"\n{SelectedItem["PRODUCTIONTASKNUMBER"]}";
                var d = new DialogWindow($"{msg}", "Завершение задания", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() != true)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                TaskGridToolbar.IsEnabled = false;
                TaskGrid.ShowSplash();

                var p = new Dictionary<string, string>();

                {
                    p.Add("ID", id.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ProductionTask");
                q.Request.SetParam("Action", "Finish");

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status != 0)
                {
                    q.ProcessError();
                }
                else
                {
                    TaskGrid.LoadItems();
                    PositionGrid.ClearItems();
                }

                Mouse.OverrideCursor = null;
                TaskGridToolbar.IsEnabled = true;
                TaskGrid.HideSplash();
            }
        }

        /// <summary>
        /// отменить завершение
        /// </summary>
        public async void FinishCancel()
        {
            bool resume = true;

            if (resume)
            {
                if (SelectedItem == null)
                {
                    resume = false;
                }
            }

            var id = SelectedItem.CheckGet("PRODUCTIONTASKID").ToInt();
            if (resume)
            {
                if (id == 0)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                string msg = "";
                msg += $"Отменить завершение производственного задания?";
                msg += $"\n{SelectedItem["PRODUCTIONTASKNUMBER"]}";
                var d = new DialogWindow($"{msg}", "Завершение задания", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() != true)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                TaskGridToolbar.IsEnabled = false;
                TaskGrid.ShowSplash();

                var p = new Dictionary<string, string>();

                {
                    p.Add("ID", id.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ProductionTask");
                q.Request.SetParam("Action", "FinishCancel");

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status != 0)
                {
                    q.ProcessError();
                }
                else
                {
                    TaskGrid.LoadItems();
                    PositionGrid.ClearItems();
                }

                Mouse.OverrideCursor = null;
                TaskGridToolbar.IsEnabled = true;
                TaskGrid.HideSplash();
            }
        }

        /// <summary>
        /// разблокировать задание
        /// </summary>
        public async void Unblock()
        {
            bool resume = true;

            if (resume)
            {
                if (SelectedItem == null)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                string msg = "";
                msg += $"Разблокировать производственное задание?";
                msg += $"\n{SelectedItem["PRODUCTIONTASKNUMBER"]}";
                var d = new DialogWindow($"{msg}", "Блокировка задания", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() != true)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                TaskGridToolbar.IsEnabled = false;
                TaskGrid.ShowSplash();

                var p = new Dictionary<string, string>();

                {
                    p.Add("ID", SelectedItemId.ToString());
                    p.Add("WORK", "1");
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ProductionTask");
                q.Request.SetParam("Action", "SetWork");

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status != 0)
                {
                    q.ProcessError();
                }
                else
                {
                    TaskGrid.LoadItems();
                    PositionGrid.ClearItems();
                }

                Mouse.OverrideCursor = null;
                TaskGridToolbar.IsEnabled = true;
                TaskGrid.HideSplash();
            }
        }

        /// <summary>
        /// Заблокировать
        /// </summary>
        public async void Block()
        {
            bool resume = true;

            if (resume)
            {
                if (SelectedItem == null)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                string msg = "";
                msg += $"Заблокировать производственное задание?";
                msg += $"\n{SelectedItem["PRODUCTIONTASKNUMBER"]}";
                var d = new DialogWindow($"{msg}", "Блокировка задания", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() != true)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                TaskGridToolbar.IsEnabled = false;
                TaskGrid.ShowSplash();

                var p = new Dictionary<string, string>();

                {
                    p.Add("ID", SelectedItemId.ToString());
                    p.Add("WORK", "0");
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ProductionTask");
                q.Request.SetParam("Action", "SetWork");

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status != 0)
                {
                    q.ProcessError();
                }
                else
                {
                    TaskGrid.LoadItems();
                    PositionGrid.ClearItems();
                }

                Mouse.OverrideCursor = null;
                TaskGridToolbar.IsEnabled = true;
                TaskGrid.HideSplash();
            }
        }

        /// <summary>
        /// создание записи
        /// </summary>
        public void Create()
        {
            var cuttingManual = new ProductionTask();
            cuttingManual.BackTabName = "ProductionTask_productionTaskList";
            cuttingManual.FactoryId = Factory.SelectedItem.Key.ToInt();
            cuttingManual.Create();
        }

        /// <summary>
        /// редактирование записи
        /// </summary>
        public void EditTask()
        {
            if (SelectedItemId != 0)
            {
                var cuttingManual = new ProductionTask();
                var id = SelectedItem.CheckGet("PRODUCTIONTASKID").ToInt();
                cuttingManual.BackTabName = "ProductionTask_productionTaskList";
                cuttingManual.FactoryId = Factory.SelectedItem.Key.ToInt();
                cuttingManual.Edit(id);
            }
        }

        /// <summary>
        /// клонирование записи
        /// </summary>
        public void Copy()
        {
            if (SelectedItemId != 0)
            {
                var cuttingManual = new ProductionTask();
                var id = SelectedItem.CheckGet("PRODUCTIONTASKID").ToInt();
                cuttingManual.BackTabName = "ProductionTask_productionTaskList";
                cuttingManual.FactoryId = Factory.SelectedItem.Key.ToInt();
                cuttingManual.Edit(id, true);
            }
        }

        /// <summary>
        /// карточка позиции задания
        /// </summary>
        public void EditPosition()
        {
            if (SelectedItem2 != null)
            {
                //id ПЗГА
                var productionTaskId = SelectedItem2.CheckGet("PRODUCTIONTASKID").ToInt();
                if (productionTaskId != 0)
                {
                    //id ПЗПР
                    var processingTaskId = SelectedItem2.CheckGet("PRODUCTIONTASKNEXTID").ToInt();
                    //id позиции заявки
                    var applicationPositionId = SelectedItem2.CheckGet("IDORDERDATES").ToInt();
                    //id заготовки
                    var goodsId = SelectedItem2.CheckGet("GOODSID").ToInt();
                    //id категории заготовки
                    var categoryId = SelectedItem2.CheckGet("CATEGORYID").ToInt();
                    // Признак, что задание - перевыгон
                    var reworkFlag = SelectedItem2.CheckGet("REWORK_FLAG").ToBool();

                    var position = new Position();
                    position.ReturnTabName = "ProductionTask_productionTaskList";
                    position.FactoryId = Factory.SelectedItem.Key.ToInt();
                    position.ReworkFlag = reworkFlag;
                    position.Edit(productionTaskId, applicationPositionId, goodsId, categoryId, processingTaskId);
                }
            }
        }

        /// <summary>
        /// изменение примечания для переработки
        /// </summary>
        public void EditNote()
        {
            if (SelectedItem2 != null)
            {
                //id ПЗПР
                var processingTaskId = SelectedItem2.CheckGet("PRODUCTIONTASKNEXTID").ToInt();
                if (processingTaskId != 0)
                {
                    var h = new ProcessingTaskNote();
                    h.Edit(processingTaskId);
                }
            }
        }

        /// <summary>
        /// Создание окна для добавления примечания к ПЗГА
        /// </summary>
        public void EditTaskNote()
        {
            if (SelectedItem != null)
            {
                var taskNoteWindow = new ProductionTaskNote();
                taskNoteWindow.ReceiverName = "TaskList";
                var p = new Dictionary<string, string>()
                {
                    { "ID", SelectedItem.CheckGet("PRODUCTIONTASKID") },
                    { "NOTE", SelectedItem.CheckGet("NOTE") },
                };
                taskNoteWindow.Edit(p);
            }
        }

        /// <summary>
        /// Сохраняет примечание для ПЗГА
        /// </summary>
        /// <param name="p">Параметры для сохранения</param>
        public async void SaveTaskNote(Dictionary<string, string> p)
        {
            int taskId = p.CheckGet("ID").ToInt();
            if (taskId > 0)
            {
                // сохраняем только если сделали изменения в примечании
                if (SelectedItem.CheckGet("NOTE") != p.CheckGet("NOTE"))
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Production");
                    q.Request.SetParam("Object", "ProductionTask");
                    q.Request.SetParam("Action", "SaveNote");
                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    q.Request.SetParams(p);

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if (q.Answer.Status == 0)
                    {
                        TaskGrid.LoadItems();
                    }
                }
            }
        }

        /// <summary>
        /// экспорт записей грида в Excel
        /// </summary>
        private async void ExportToExcel()
        {
            if (TaskGrid != null)
            {
                if (TaskGrid.Items.Count > 0)
                {
                    var eg = new ExcelGrid();
                    var cols = TaskGrid.Columns;
                    eg.SetColumnsFromGrid(cols);
                    eg.Items = TaskGrid.Items;
                    await Task.Run(() =>
                    {
                        eg.Make();
                    });
                }
            }
        }

        private async void ExportToHtml()
        {
            if (TaskGrid != null)
            {
                if (TaskGrid.Items.Count > 0)
                {
                    var eg = new ExcelGrid();
                    var cols = TaskGrid.Columns;
                    eg.SetColumnsFromGrid(cols);
                    eg.Items = TaskGrid.Items;
                    await Task.Run(() =>
                    {
                        eg.MakeHtml();
                    });
                }
            }
        }

        /// <summary>
        /// Сохранение изменения флага расмещения на стеллажном складе
        /// </summary>
        private async void PlaceRack()
        {
            var idOrder = PositionGrid.SelectedItem.CheckGet("IDORDERDATES").ToInt();
            int rackFlag = PositionGrid.SelectedItem.CheckGet("PLACED_IN_RACK_FLAG").ToInt();
            string newRackFlag = rackFlag == 1 ? "0" : "1";

            if (idOrder > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Application");
                q.Request.SetParam("Action", "SetPlaceRackFlag");
                q.Request.SetParam("IDORDERDATES", idOrder.ToString());
                q.Request.SetParam("PLACED_IN_RACK_FLAG", newRackFlag);
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
                        PositionGrid.LoadItems();
                    }
                }
            }
        }

        /// <summary>
        /// Заполняет данные по перевыгону
        /// </summary>
        private void SetPositionRework()
        {
            // Проверяем, содержится ли номер предыдущего ПЗ в текущем
            string currentNum = SelectedItem.CheckGet("PRODUCTIONTASKNUMBER");
            int prevNum = currentNum.Substring(5, 4).ToInt();
            // Должно быть число. Если нет, номер не соответствует формату
            if (prevNum > 0)
            {
                var p = new Dictionary<string, string>();
                p.CheckAdd("TASK_ID", SelectedItem.CheckGet("PRODUCTIONTASKID"));
                p.CheckAdd("POSITION_ID", SelectedItem2.CheckGet("GOODSID"));
                p.CheckAdd("NOTE", SelectedItem.CheckGet("NOTE"));
                p.CheckAdd("TASK_NUM", SelectedItem.CheckGet("PRODUCTIONTASKNUMBER"));
                p.CheckAdd("QUANTITY", SelectedItem2.CheckGet("QUANTITY"));
                p.CheckAdd("DTTM", SelectedItem.CheckGet("CREATED"));

                var reworkWindow = new ProductionTaskPositionReworkReason();
                reworkWindow.ReceiverName = "PositionList";
                reworkWindow.Edit(p);
            }
            else
            {
                var errDw = new DialogWindow("Неверный формат номер для задания с перевыгоном", "Отметка перевыгона");
                errDw.ShowDialog();
            }
        }

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem"></param>
        public void TaskGridUpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;
            SelectedItemId = 0;

            EditButton.IsEnabled = false;
            DeleteButton.IsEnabled = false;

            //Grid.Menu["copy"].Enabled=false;
            TaskGrid.Menu["edit"].Enabled = false;
            TaskGrid.Menu["delete"].Enabled = false;
            TaskGrid.Menu["showmap"].Enabled = false;
            TaskGrid.Menu["block"].Enabled = false;
            TaskGrid.Menu["unblock"].Enabled = false;
            TaskGrid.Menu["finish"].Enabled = false;
            TaskGrid.Menu["finish_cancel"].Enabled = false;
            TaskGrid.Menu["edit_task_note"].Enabled = false;

            EditButton.Content = "Изменить";

            if (selectedItem.Count > 0)
            {
                int id = selectedItem.CheckGet("PRODUCTIONTASKID").ToInt();

                /*
                   
                   кнопкой "изменить" можно открыть все, но готовые задания
                   будут в режиме "только для чтения"
                */
                if (id != 0)
                {
                    SelectedItemId = id;

                    EditButton.IsEnabled = true;
                    TaskGrid.Menu["edit"].Enabled = true;

                    //Grid.Menu["copy"].Enabled=true;
                    TaskGrid.Menu["showmap"].Enabled = true;
                }

                /*
                    INPLANNINGQUEUE =0|1 -- в плане на ГА  (BHS_QUEUE)
                    INPLANNING2QUEUE =0|1 -- в очереди на планирование (PROD_CONVRTNG_QUEUE)
                    POSTING =0|1 -- признак выполнения, 1 --выполнено
                    INSTACK =0|1 -- в очереди на ГА (PZ_LINE)
                    WORK =1|0 -- в очередь, если 1, уйдет в очередь ГА

                    если выполнено (POSTING=1), ничего нельзя (только смотреть)
                    если в очереди на ГА (INSTACK=1), ничего нельзя (только смотреть)
                    если не в плане  ГА (INPLANNINGQUEUE=0), можно редактировать (алгоритм2), можно удалить
                    если в плане ГА (INPLANNINGQUEUE=1), и если заблокировано (WORK=0), можно редактировать (алгоритм1)
              
                    алгоритм2 -- пересоздание ПЗГА
                    алгоритм1 -- изменение количества, сырья
                 */

                bool edit1 = false;
                bool edit2 = false;
                bool delete = false;
                bool view = true;

                //не выполнено
                if (selectedItem.CheckGet("POSTING").ToInt() == 0)
                {
                    //завершить
                    TaskGrid.Menu["finish"].Enabled = true;
                    // изменить примечание
                    TaskGrid.Menu["edit_task_note"].Enabled = true;

                    //не в очереди ГА
                    if (selectedItem.CheckGet("INSTACK").ToInt() == 0)
                    {
                        delete = true;

                        {
                            if (selectedItem.CheckGet("WORK").ToInt() == 0)
                            {
                                //заблокировано

                                //разблокировать
                                TaskGrid.Menu["unblock"].Enabled = true;
                            }
                            else
                            {
                                //разблокировано

                                //заблокировать
                                TaskGrid.Menu["block"].Enabled = true;
                            }
                        }

                        if (selectedItem.CheckGet("INPLANNINGQUEUE").ToInt() == 0)
                        {
                            //не в плане  ГА

                            edit2 = true;
                            delete = true;
                        }
                        else
                        {
                            //в плане ГА

                            if (selectedItem.CheckGet("WORK").ToInt() == 0)
                            {
                                //заблокировано
                                edit1 = true;
                                delete = true;

                            }
                            else
                            {
                                //разблокировано

                            }
                        }
                    }
                }
                else
                {
                    //отменить завершение можно только те задания,
                    //по кторым не было прихода
                    if (selectedItem.CheckGet("PRIHOD_FLAG").ToInt() == 0)
                    {
                        //отменить завершение
                        TaskGrid.Menu["finish_cancel"].Enabled = true;
                    }
                }

                if (view)
                {
                    EditButton.Content = "Открыть";
                    TaskGrid.Menu["edit"].Header = "Открыть";
                }

                if (edit1 || edit2)
                {
                    EditButton.Content = "Изменить";
                    TaskGrid.Menu["edit"].Header = "Изменить";
                }

                if (delete)
                {
                    DeleteButton.IsEnabled = true;
                    TaskGrid.Menu["delete"].Enabled = true;
                }

            }


            /////
            ///Grid.Menu["edit"].Enabled=true;

            if (SelectedItemId != 0)
            {
                PositionGrid.LoadItems();
            }

            ProcessPermissions();
        }

        private void SetMachineItems()
        {
            int factoryId = Factory.SelectedItem.Key.ToInt();
            if ((factoryId > 0) && (MachineDS.Items.Count > 0))
            {
                var list = new Dictionary<string, string>();
                list.Add("0", "Все");
                foreach (Dictionary<string, string> row in MachineDS.Items)
                {
                    if (row.CheckGet("FACTORY_ID").ToInt() == factoryId)
                    {
                        list.Add(row.CheckGet("NAME2"), $"{row.CheckGet("NAME2")} ({row.CheckGet("NAME")})");
                    }
                }
                Machine.Items = list;
                Machine.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");

            }
        }

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem"></param>
        public void PositionGridUpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem2 = selectedItem;
            //PositionGrid.Menu["bind"].Enabled=false;
            //PositionGrid.Menu["unbind"].Enabled=false;
            PositionGrid.Menu["positions"].Enabled = false;
            PositionGrid.Menu["showtk"].Enabled = false;
            PositionGrid.Menu["note"].Enabled = false;

            BindPosition.IsEnabled = false;

            if (SelectedItem2.Count > 0)
            {
                //PositionGrid.Menu["bind"].Enabled=true;
                //PositionGrid.Menu["unbind"].Enabled=true;
                PositionGrid.Menu["positions"].Enabled = true;
                BindPosition.IsEnabled = true;

                if (!SelectedItem2.CheckGet("PATHTK").ToString().IsNullOrEmpty())
                {
                    PositionGrid.Menu["showtk"].Enabled = true;
                }

                //если есть задание на переработку
                if (SelectedItem2.CheckGet("PRODUCTIONTASKNEXTID").ToInt() != 0)
                {
                    PositionGrid.Menu["note"].Enabled = true;
                }

                if (SelectedItem2.CheckGet("PLACED_IN_RACK_FLAG").ToInt() == 1)
                {
                    PositionGrid.Menu["placeRack"].Header = "Отменить размещение на стеллажном складе";
                }
                else
                {
                    PositionGrid.Menu["placeRack"].Header = "Разместить на стеллажном складе";
                }
            }
        }

        private void ShowButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            TaskGrid.LoadItems();
        }

        private void EditButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            EditTask();
        }

        private void CreateButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Create();
        }

        private void TestFilterButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            TaskGrid.UpdateItems();
        }

        private void HelpButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void ExportButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExportToExcel();
        }

        private void Export2Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExportToHtml();
        }

        private void Today_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshButton.Style = (Style)RefreshButton.TryFindResource("FButtonPrimary");
        }

        private void Statuses_SelectedItemChanged(System.Windows.DependencyObject d, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            TaskGrid.UpdateItems();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Delete();
        }

        private void ExportButton2_Click(object sender, RoutedEventArgs e)
        {
            var eg = new ExcelGrid();
            eg.SetColumnsFromGrid(TaskGrid.Columns);
            eg.Items = TaskGrid.GridItems;
            eg.Make();
        }

        private void EditButton_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void HelpButton_Click_1(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void Profile_SelectedItemChanged(System.Windows.DependencyObject d, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            var profileId = Profile.SelectedItem.Key.ToInt();

            if (CardboardDS.Items.Count > 0)
            {
                var list = new Dictionary<string, string>();
                list.Add("0", "");
                if (profileId > 0)
                {
                    foreach (Dictionary<string, string> row in CardboardDS.Items)
                    {
                        if (row.CheckGet("PROFILE_ID").ToInt() == profileId)
                        {
                            list.Add(row.CheckGet("ID"), row.CheckGet("NAME"));
                        }
                    }
                }
                else
                {
                    list.AddRange<string, string>(CardboardDS.GetItemsList("ID", "NAME"));
                }
                Cardboard.Items = list;
                Cardboard.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
            }

            TaskGrid.UpdateItems();
        }

        private void Cardboard_SelectedItemChanged(System.Windows.DependencyObject d, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            TaskGrid.UpdateItems();
        }

        private void Machine_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TaskGrid.UpdateItems();
        }

        private void Format_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TaskGrid.UpdateItems();
        }

        private void Creator_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TaskGrid.UpdateItems();
        }

        private void BindPosition_Click(object sender, RoutedEventArgs e)
        {
            EditPosition();
        }

        private void TandemCheckBox_Click(object sender, RoutedEventArgs e)
        {
            TaskGrid.UpdateItems();
        }

        private void Factory_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SetMachineItems();
            if (TaskGrid.Items != null)
            {
                TaskGrid.LoadItems();
            }
        }
    }
}
