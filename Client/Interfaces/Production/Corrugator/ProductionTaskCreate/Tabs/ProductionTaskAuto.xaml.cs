using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Production.CreatingTasks;
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
    /// Автоматический раскрой.
    /// Создает производственные задания для ГА автоматически
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>2</version>
    /// <released>2021-11-15</released>
    public partial class ProductionTaskAuto : UserControl
    {
        public ProductionTaskAuto()
        {
            /*
                сервисный запрос, получаем форматы
                    отображаем toolbar
                рабочий запрос, получаем данные грида
                    отображаем данные гридов 1 и 2

                оба грида получают данные из одного запроса
             */

            AddDaysMax=15;
            
            
            CustomPositionListMode=false;
            PositionIdList="";
            PositionQuantityList="";

            /*
            CustomPositionListMode=true;
            PositionIdList="1304757,1305062";
            */

            /*
            CustomPositionListMode=true;
            PositionIdList="1322037,1328282,1326488";
            PositionQuantityList="1025,248,1500";
            */

            /*
            CustomPositionListMode=true;
            PositionIdList="1322037,1321793,1328282,1325313,1326488,1328271";
            PositionQuantityList="9285,2122,248,1400,1500,2463";
            */

            /*
            CustomPositionListMode=true;
            PositionIdList="1325477,1328087,1326928,1326583";
            PositionQuantityList="683,1008,834,1404";
            */


            InitializeComponent();

            Formats=new Dictionary<string, string>();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            //регистрация обработчика клавиатуры
            PreviewKeyDown += ProcessKeyboard;

            SetDefaults();            
            LoadRef();

            PositionGridInit();
            TaskGridInit();
            PaperGridInit();

            if(!Central.DebugMode)
            {
                ToggleSplash31Button.Visibility=Visibility.Collapsed;
                ToggleSplash30Button.Visibility=Visibility.Collapsed;
            }

            ProcessPermissions();
        }

        public string RoleName = "[erp]production_task_cm_create";

        /// <summary>
        /// максимальное количество дней докроя
        /// </summary>
        private int AddDaysMax { get;set; }

        /// <summary>
        /// форматы для раскроя
        /// массив вида:
        /// FORMAT2100 1
        /// при изменении значения чекбокса в блоке выбора форматов ставится значение в
        /// соотв. элемент массива, при отправке запроса на раскрой данные 
        /// по форматам берутся из этого массива
        /// </summary>
        private Dictionary<string,string> Formats { get;set; }

        /// <summary>
        /// режим формирования списка позиций на раскрой вручную
        /// для отладки
        /// если флаг установлен, укажите в PositionIdList список позиций через запятую
        /// например: "1304757,1305062"
        /// эти позиции будут отображены в списке на раскрой
        /// </summary>
        private bool CustomPositionListMode { get;set; }

        /// <summary>
        /// список позиций через запятую для режима CustomPositionListMode
        /// </summary>
        private string PositionIdList { get;set; }
        private string PositionQuantityList { get;set; }
        /// <summary>
        /// Позиция, выбранная для раскроя вручную со всеми параметрами
        /// </summary>
        private Dictionary<string, string> ManualCuttingPosisionSelected { get;set; }

        /// <summary>
        /// Деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о фрейма
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="ProductionTaskCreating",
                ReceiverName = "",
                SenderName = "CuttingAutoView",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            ///Grid.Destruct();
        }

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
            if (m.ReceiverGroup.IndexOf("ProductionTaskCutted") > -1)
            {
                if(m.ReceiverName.IndexOf("TaskList")>-1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            TaskGridSelectedItem=null; 
                            TaskGrid.LoadItems();
                            break;

                        case "Clear":
                            TaskGridSelectedItem=null; 
                            TaskGrid.ClearItems();
                            break;
                    }
                }
                
                if(m.ReceiverName.IndexOf("PositionList")>-1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            PositionGrid.LoadItems();                            
                            break;

                        case "GetPosition":
                            var selectedPosition = ManualCuttingPosisionSelected;
                            selectedPosition.CheckAdd("STACKER_ID", "1");

                            //отправляем сообщение о выборе заготовки
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "ProductionTask",
                                ReceiverName = "CuttingManualView",
                                SenderName = "SelectBlankView",
                                Action = "SelectedBlank",
                                ContextObject = selectedPosition,
                            });
                            break;
                    }
                }

                if (m.ReceiverName.IndexOf("PaperList") > -1)
                {
                    switch (m.Action)
                    {
                        case "Save":
                            var result = (Dictionary<string, string>)m.ContextObject;
                            if (PaperDS.Initialized)
                            {
                                var paperId = result.CheckGet("ID").ToInt();
                                if (paperId > 0)
                                {
                                    foreach (var row in PaperDS.Items)
                                    {
                                        if (row["ID"].ToInt() == paperId)
                                        {
                                            row.CheckAdd("WEIGHT_VIRTUAL", result["WEIGHT_VIRTUAL"]);
                                            // WeightResidual => Weight - WeightCorrugatingMachine - WeightCutting + WeightVirtual;
                                            var weightResidual = row.CheckGet("WEIGHT").ToDouble() + result["WEIGHT_VIRTUAL"].ToDouble() - row["WEIGHT_CORRUGATING_MACHINE"].ToDouble() - row["WEIGHT_CUTTING"].ToDouble();
                                            row["WEIGHT_RESIDUAL"] = weightResidual.ToString();
                                        }
                                    }
                                }
                                PaperGrid.UpdateItems(PaperDS);
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
        private void ProcessKeyboard(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F5:
                    //Grid.LoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Home:
                    //Grid.SetSelectToFirstRow();
                    e.Handled = true;
                    break;

                case Key.End:
                    //Grid.SetSelectToLastRow();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Отображение статьи в справочной системе
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/production/creating_tasks/cutting_auto");
        }


        /// <summary>
        /// инициализация полей формы исходных данных
        /// </summary>
        public void SetDefaults()
        {
            FromDate.Text=DateTime.Now.AddDays(1).ToString("dd.MM.yyyy");
            ToDate.Text=DateTime.Now.AddDays(1).ToString("dd.MM.yyyy");
            
            //несамокройные
            Filter5.IsChecked=false;

            {
                var list = new Dictionary<string,string>();
                list.Add("-1","Все");
                list.Add("1","Нераскроенные");
                list.Add("11","Нераскроенные+");
                list.Add("2","Недораскроенные");
                list.Add("3","Перераскроенные");
                list.Add("4","Неразрешенные");
                list.Add("6", "Сложная схема");
                list.Add("7", "Решетки");
                list.Add("8", "Только вручную");
                list.Add("9", "Комплекты");
                Type.Items=list;
                Type.SelectedItem=list.FirstOrDefault((x)=>x.Key=="-1");    

                if(CustomPositionListMode)
                {
                    Type.SelectedItem=list.FirstOrDefault((x)=>x.Key=="11");    
                }
            }

            CardboardDS = new ListDataSet();

            //дней докроя
            {
                if(AddDaysMax>0)
                {
                    var list = new Dictionary<string, string>();

                    for(int i=0; i<=AddDaysMax; i++)
                    {
                         list.Add($"{i}",$"{i}");
                    }
               
                    AddDays.Items = list;
                    AddDays.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
                }               
            }

            Trim10.IsChecked=false;
            Trim.Value=(decimal)4.5;
            TaskMinLength.Text="300";
            Deviation.Value=(decimal)5.0;
            
            CuttingWithPack.IsChecked=false;
            CuttingWithLong.IsChecked=false;

            TaskTab.IsSelected=true;
            TaskTotals.Content="";

            AvailableOnly.IsChecked=false;

            PositionSelectAll.IsChecked=true;
            Position2SelectAll.IsChecked=true;

            TrimMatrix.IsChecked=true;

            LoadWaste.IsEnabled = true;

            TotalToExcelButton.IsEnabled = false;
            ManualCuttingPosisionSelected = new Dictionary<string, string>();
        }


        /// <summary>
        /// датасет грид с позициями для раскроя
        /// </summary>
        public ListDataSet PositionGridDS { get; set; }
        public ListDataSet Position2GridDS { get; set; }
        public ListDataSet TaskGridDS { get; set; }
        public ListDataSet PaperDS { get; set; }
        public ListDataSet CardboardDS { get; set; }

        public Dictionary<string, string> PositionGridSelectedItem { get; set; }
        public Dictionary<string, string> Position2GridSelectedItem { get; set; }
        public Dictionary<string, string> TaskGridSelectedItem { get; set; }
        public Dictionary<string, string> PaperGridSelectedItem { get; set; }
        

        /// <summary>
        /// грид с позициями для раскроя
        /// </summary>
        public void PositionGridInit()
        {
            //список колонок грида
            //у двух гридов одинаковый набор колонок
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="*",
                    Path="_SELECTED",
                    ColumnType=ColumnTypeRef.Boolean,
                    Editable=true,                    
                    OnClickAction=(row,el) =>
                    {
                        var statusColor=row.CheckGet("STATUS_COLOR").ToString();
                        
                        var c=(CheckBox)el;
                        if(statusColor=="blue")
                        {

                        }
                        else
                        {
                            c.IsChecked=false;
                        }

                        return null;
                    },
                },
                new DataGridHelperColumn
                {
                    Header="ИД позиции",
                    Path="POSITIONID",
                    Doc="ИД позиции заявки (ID_ORDERDATES)",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=50,
                },
                
                new DataGridHelperColumn
                {
                    Header="Картон",
                    Path="CARDBOARDNAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=110,
                    MaxWidth=250,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.FontWeight,
                            row =>
                            {
                                var fontWeight= new FontWeight();
                                fontWeight=FontWeights.Normal;
            
                                if( row.CheckGet("FIXED_WEIGHT_FLAG").ToInt() == 1 )
                                {
                                    fontWeight=FontWeights.Bold;
                                }                                  
            
                                return fontWeight;
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Тип композиции",
                    Path="CARDBOARD_COMPOSITION_TYPE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=38,
                    MaxWidth=38,
                },
                new DataGridHelperColumn
                {
                    Header="Отгрузка",
                    Doc="Дата и время отгрузки",
                    Path="DTTMSHIP",
                    ColumnType=ColumnTypeRef.DateTime,
                    Format="dd.MM HH:mm",
                    MinWidth=70,
                    MaxWidth=70,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            (Dictionary<string, string> row) =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                //признак "ехать на горячую"
                                if ( row.CheckGet("RUN_HOT").ToInt()==1 )
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
                /*
                new DataGridHelperColumn
                {
                    Header="DT2",
                    Path="SHIPMENT_DATE_PRODUCTION",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=45,
                    MaxWidth=45,
                },
                */
                new DataGridHelperColumn()
                {
                    Header="Первое производство",
                    Path="FIRST_TIME_PRODUCTION",
                    Doc="Первое производство (данное изделие ни разу не отгружалось)",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=35,
                    MaxWidth=35,
                },


                new DataGridHelperColumn
                {
                    Header="Изделие",
                    Path="GOODSNAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=150,
                    MaxWidth=400,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                
                                int typeCut = row.CheckGet("TYPE_CUT").ToInt();
                                int pztotalqty = row.CheckGet("PZTOTALQTY").ToInt();
                                int zagqty = row.CheckGet("ZAGQTY").ToInt();
                                int resqty = row.CheckGet("RESQTY").ToInt();
                                int count_id2_id_ts = row.CheckGet("COUNT_ID2_ID_TS").ToInt();
                                

                                
                                var typeId = Type.SelectedItem.Key.ToInt();
                                /*
                                    Нераскроенные"    Name="Filter1"
                                    Недораскроенные"  Name="Filter2"
                                    Перераскроенные"  Name="Filter3"
                                    Неразрешенные"    Name="Filter4"
                                 */
                                
                                //Недораскроенные
                                if(typeId==2)
                                {
                                    if (typeCut == 0)
                                    {
                                        if (
                                            (pztotalqty < (zagqty * 0.95))
                                            ||
                                            (resqty > (zagqty * 0.05))
                                        )
                                        {
                                            if (count_id2_id_ts > 1)
                                            {
                                                color = HColor.Red;
                                            }
                                        }
                                    }
                                }

                                //Перераскроенные
                                if(typeId==3)
                                {
                                    if (typeCut == 3)
                                    {
                                        if (count_id2_id_ts > 1)
                                        {
                                            color = HColor.Red;
                                        }
                                    }
                                }
                                
                                
                               


                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        },
                        {
                            StylerTypeRef.FontWeight,
                            row =>
                            {
                                var fontWeight= new FontWeight();
                                fontWeight=FontWeights.Normal;
            
                                if( row.CheckGet("RIG_IS").ToInt() < 2 )
                                {
                                    fontWeight=FontWeights.Bold;
                                }                                  
            
                                return fontWeight;
                            }
                        },
                        
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Заготовка",
                    Path="BLANKNAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=150,
                    MaxWidth=400,
                },  
                new DataGridHelperColumn
                {
                    Header="Z-картон",
                    Path="ZCARDBOARD",
                    ColumnType=ColumnTypeRef.Boolean,
                    MinWidth=35,
                    MaxWidth=35,
                },  
                
                new DataGridHelperColumn
                {
                    Header="=",
                    Path="QTY_LIMIT",
                    Doc="Допуск по количеству заготовок",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=35,
                    MaxWidth=35,
                },
                new DataGridHelperColumn
                {
                    Header="Рилевка",
                    Path="CREASE_NAME",
                    Doc="Заданный тип рилевки",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=35,
                    MaxWidth=35,
                },

                new DataGridHelperColumn
                {
                    Header="Заявка",
                    Doc="Количество изделий в заявке",
                    Group="Изделий",
                    Path="PRODUCTS_IN_APPLICATION",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=65,
                },
                new DataGridHelperColumn
                {
                    Header="Припуск",
                    Doc="Количество изделий для припуска на брак",
                    Group="Изделий",
                    Path="PRODUCT_ADDITIVE",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=20,
                    MaxWidth=50,
                },


                new DataGridHelperColumn
                {
                    Header="Склад",
                    Doc="Количество изделий на складе",
                    Group="Изделий",
                    Path="CURQTY",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=65,
                },
                new DataGridHelperColumn
                {
                    Header="Склад ПЗ",
                    Doc="Количество изделий в заявке под данное ПЗ",
                    Group="Изделий",
                    Path="CURPZQTY",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=65,
                },                
                /*
                new DataGridHelperColumn
                {
                    Header="Склад всего",
                    Doc="Общее количество изделий на складе",
                    Group="Изделий",
                    Path="CURTOTALQTY",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=50,
                },
                */
                

                new DataGridHelperColumn
                {
                    Header="Отгрузка",
                    Group="Изделий",
                    Path="RQTY",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=65,
                },
                new DataGridHelperColumn
                {
                    Header="Изд/заг",
                    Doc="Количество изделий из одной заготовки",
                    Group="Изделий",
                    Path="TLSQTY",
                    ColumnType=ColumnTypeRef.Double,
                    MinWidth=40,
                    MaxWidth=40,
                    FormatterRaw = (v) =>{
                        var result = "";
                        var value = v.CheckGet("TLSQTY").ToDouble();
                        if (value >= 1)
                        {
                            result = value.ToInt().ToString();
                        }
                        else
                        {
                            result = value.ToString();
                        }
                        return result;
                    },
                },

                new DataGridHelperColumn
                {
                    Header="Всего в ПЗ",
                    Group="Заготовок",
                    Path="PZTOTALQTY",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=65,
                },
                new DataGridHelperColumn
                {
                    Header="В перевыгоне",
                    Group="Заготовок",
                    Path="REWORK_QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=65,
                },
                new DataGridHelperColumn
                {
                    Header="Не сделано",
                    Group="Заготовок",
                    Path="PZQTY",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=65,
                },
                new DataGridHelperColumn
                {
                    Header="Наличие",
                    Group="Заготовок",
                    Path="PZZAGQTY",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=65,
                },
                new DataGridHelperColumn
                {
                    Header="Для раскроя",
                    Group="Заготовок",
                    Path="BLANK_FOR_CUTTING",
                    Doc="Требуемое количество заготовок для раскроя, шт.",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=65,
                },

                new DataGridHelperColumn
                {
                    Header="В раскрое",
                    Group="Заготовок",
                    Path="TASKQTY",
                    Doc="Количество заготовок в результате раскроя",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=65,
                    Style="DataGridColumnDigit",
                    FormatterRaw=(v) =>
                    {
                        var result="";
                        if(v.CheckGet("TASKQTY").ToDouble()>0)
                        {
                            result=$"{v.CheckGet("TASKQTY").ToDouble().ToString()}";
                        }
                        return result;
                    },
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.FontWeight,
                            row =>
                            {
                                var fontWeight= new FontWeight();
                                fontWeight=FontWeights.Bold;
            
                                return fontWeight;
                            }
                        },
                    },
                },

                new DataGridHelperColumn
                {
                    Header="Всего",
                    Group="Отклонение, %",
                    Path="DEVIATION",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=65,
                    MaxWidth=65,
                    Style="DataGridColumnDigit",
                    FormatterRaw=(v) =>
                    {
                        var result="";
                        if(v.CheckGet("TASKQTY").ToDouble()>0)
                        {
                            result=$"{v.CheckGet("DEVIATION").ToDouble().ToString("N1")}";
                        }
                        return result;
                    },                    
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.ForegroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if(row.CheckGet("DEVIATION").ToInt()>=0)
                                {
                                    color = HColor.GreenFG;
                                }
                                else
                                {
                                    color = HColor.RedFG;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        },
                        {
                            StylerTypeRef.FontWeight,
                            row =>
                            {
                                var fontWeight= new FontWeight();
                                fontWeight=FontWeights.Bold;
            
                                return fontWeight;
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Раскроя",
                    Group="Отклонение, %",
                    Path="TASK_DEVIATION",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=65,
                    MaxWidth=65,
                    Style="DataGridColumnDigit",
                    FormatterRaw=(v) =>
                    {
                        var result="";
                        if(v.CheckGet("TASKQTY").ToDouble()>0)
                        {
                            result=$"{v.CheckGet("TASK_DEVIATION").ToDouble().ToString("N1")}";
                        }
                        return result;
                    },                     
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.ForegroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if(row.CheckGet("TASK_DEVIATION").ToInt()>=0)
                                {
                                    color = HColor.GreenFG;
                                }
                                else
                                {
                                    color = HColor.RedFG;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        },
                        {
                            StylerTypeRef.FontWeight,
                            row =>
                            {
                                var fontWeight= new FontWeight();
                                fontWeight=FontWeights.Bold;
            
                                return fontWeight;
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Самокройность примечание",
                    Path="SELFCUT_NOTE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=40,
                    MaxWidth=200,
                },
                new DataGridHelperColumn
                {
                    Header="Самокройное",
                    Path="SELFCUT",
                    ColumnType=ColumnTypeRef.Boolean,
                    MinWidth=35,
                    MaxWidth=35,
                },
                new DataGridHelperColumn
                {
                    Header="Отчет",
                    Path="CUTTING_REPORT",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=35,
                    MaxWidth=35,
                    FormatterRaw=(v) =>
                    {
                        var result="";
                        if(!v.CheckGet("CUTTING_REPORT").ToString().IsNullOrEmpty())
                        {
                            result="[!]";
                        }
                        return result;
                    },    
                },
                new DataGridHelperColumn
                {
                    Header="Схема производства",
                    Path="PRODUCTION_SCHEME_NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=35,
                    MaxWidth=35,
                },
                new DataGridHelperColumn
                {
                    Header="ПШ+Rt",
                    Path="FULL_STAMP_RT",
                    ColumnType=ColumnTypeRef.Boolean,
                    MinWidth=35,
                    MaxWidth=35,
                },
                new DataGridHelperColumn
                {
                    Header="Необрезной край",
                    Path="UNTRIMMED_EDGE",
                    ColumnType=ColumnTypeRef.Boolean,
                    MinWidth=35,
                    MaxWidth=35,
                },
                new DataGridHelperColumn
                {
                    Header="+1 поддон",
                    Path="ADDITIONAL_PALLET_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    MinWidth=35,
                    MaxWidth=35,
                },
                new DataGridHelperColumn
                {
                    Header="Сложная схема",
                    Path="_PRODUCTION_SCHEME_STEPS",
                    ColumnType=ColumnTypeRef.Boolean,
                    MinWidth=35,
                    MaxWidth=35,
                },
                new DataGridHelperColumn
                {
                    Header="ИД схемы производства",
                    Path="PRODUCTION_SCHEME_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },

            };


            //row stylers
            var rowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                {
                    StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        var c=row.CheckGet("STATUS_COLOR").ToString();
                        c=c.ToLower();
                        switch(c)
                        {
                            case "white":
                                //color = HColor.Green;
                                break;

                            case "yellow":
                                color = HColor.Yellow;
                                break;

                            case "green":
                                color = HColor.Green;
                                break;

                            case "blue":
                                color = HColor.Blue;
                                break;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
                {
                    StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        if( row.CheckGet("TYPE_CUT").ToInt() == 2 )
                        {
                            if( row.CheckGet("HOT_CONDITION").ToInt() == 1 )
                            {
                                color=HColor.BlueFG;
                            }
                        }
                        else if( row.CheckGet("TYPE_CUT").ToInt() == 3 )
                        {
                            color=HColor.BlueFG;
                        }                   

                        
                        if( row.CheckGet("RIG_IS").ToInt() ==1 )
                        {
                            color = HColor.BlackFG;
                        } 
                        else if( row.CheckGet("RIG_IS").ToInt() ==0 )
                        {
                            color = HColor.BlueFG;
                        } 
                                
                        
                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };

            //инициализация грида 1
            {
                PositionGrid.SetColumns(columns);
                PositionGrid.SetRowStylers(rowStylers);
                PositionGrid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);                
                // Запрет на изменение сортировки в таблице
                PositionGrid.UseSorting = false;
                PositionGrid.SearchText = PositionSearchText; 
                PositionGrid.AutoUpdateInterval=0;
                PositionGrid.Init();
                
                PositionGrid.Menu=new Dictionary<string,DataGridContextMenuItem>()
                {
                    { "showtk1", new DataGridContextMenuItem(){
                        Header="Показать техкарту",
                        Action=()=>
                        {
                            ShowProductionMap(1);
                        }
                    }},
                    { "showtk2", new DataGridContextMenuItem(){
                        Header="Показать техкарту",
                        Action=()=>
                        {
                            ShowProductionMap(2);
                        }
                    }},
                    { "showreport", new DataGridContextMenuItem(){
                        Header="Показать отчет раскроя",
                        Action=()=>
                        {
                            ShowCuttingReport();
                        }
                    }},
                    { "edit_losses", new DataGridContextMenuItem(){
                        Header="Настроить припуск",
                        Tag = "access_mode_full_access",
                        Action=()=>
                        {
                            EditLosses();
                        }
                    }},
                    { "cutting_manual", new DataGridContextMenuItem(){
                        Header="Раскроить вручную",
                        Tag = "access_mode_full_access",
                        Action=()=>
                        {
                            CuttingManual();
                        }
                    }},
                };

                //при выборе строки в гриде, обновляются актуальные действия для записи
                PositionGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {
                        PositionGridUpdateActions(selectedItem);
                    }
                };
                
                

                //данные грида
                PositionGrid.OnLoadItems     = PositionGridLoadItems;
                PositionGrid.OnFilterItems   = PositionGridFilterItems;
                PositionGrid.Run();

                //фокус ввода           
                PositionGrid.Focus();
            }

            //инициализация грида 2
            {
                Position2Grid.SetColumns(columns);
                Position2Grid.SetRowStylers(rowStylers);
                Position2Grid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);          
                //PositionGrid.SearchText = Position2SearchText;
                Position2Grid.AutoUpdateInterval=0;
                Position2Grid.Init();
                
                Position2Grid.Menu=new Dictionary<string,DataGridContextMenuItem>()
                {
                    { "showtk1", new DataGridContextMenuItem(){
                        Header="Показать техкарту",
                        Action=()=>
                        {
                            ShowProductionMap2(1);
                        }
                    }},
                    { "showtk2", new DataGridContextMenuItem(){
                        Header="Показать техкарту",
                        Action=()=>
                        {
                            ShowProductionMap2(2);
                        }
                    }},
                };

                //при выборе строки в гриде, обновляются актуальные действия для записи
                Position2Grid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {
                        PositionGrid2UpdateActions(selectedItem);
                    }
                };

                //данные грида
                Position2Grid.OnFilterItems   = Position2GridFilterItems;
                Position2Grid.Run();

                //фокус ввода           
                Position2Grid.Focus();
            }
        }

        /// <summary>
        /// показать техкарту
        /// </summary>
        public void ShowProductionMap(int id)
        {
            //id: 1=GOODS 2=BLANK
            
            if(PositionGridSelectedItem!=null)
            {
                var p = "";
                switch (id)
                {
                    case 1:
                        p = PositionGridSelectedItem.CheckGet("GOODS_PATH_TK").ToString();
                        break;
                    
                    case 2:
                        p = PositionGridSelectedItem.CheckGet("BLANK_PATH_TK").ToString();
                        break;
                }
                
                if (!string.IsNullOrEmpty(p))
                {
                    Central.OpenFile(p);
                }
            }
        }
        
        public void ShowProductionMap2(int id)
        {
            //id: 1=GOODS 2=BLANK
            if(Position2GridSelectedItem!=null)
            {
                var p = "";
                switch (id)
                {
                    case 1:
                        p = Position2GridSelectedItem.CheckGet("GOODS_PATH_TK").ToString();
                        break;
                    
                    case 2:
                        p = Position2GridSelectedItem.CheckGet("BLANK_PATH_TK").ToString();
                        break;
                }
                
                if (!string.IsNullOrEmpty(p))
                {
                    Central.OpenFile(p);
                }
            }
        }

        public void ShowCuttingReport()
        {
            if(PositionGridSelectedItem!=null)
            {
                var reportText=PositionGridSelectedItem.CheckGet("CUTTING_REPORT").ToString();
                if (!string.IsNullOrEmpty(reportText))
                {
                    var reportViewer=new ReportViewer();
                    reportViewer.Content=reportText;
                    reportViewer.Init();
                    reportViewer.Show();
                }
            }
        }
        
        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem"></param>
        public void PositionGridUpdateActions(Dictionary<string, string> selectedItem)
        {
            if (selectedItem != null)
            {
                PositionGridSelectedItem=selectedItem;
            }
            else
            {
                PositionGridSelectedItem=null;
            }

            PositionGrid.Menu["showtk1"].Enabled=true;
            PositionGrid.Menu["showtk2"].Enabled=true;
            PositionGrid.Menu["cutting_manual"].Enabled=true;
            
            PositionGrid.Menu["showtk1"].Visible=false;
            PositionGrid.Menu["showtk2"].Visible=false;
            PositionGrid.Menu["cutting_manual"].Visible=false;

            PositionGrid.Menu["showreport"].Visible=false;
            
            if (PositionGridSelectedItem != null)
            {
                {
                    var s = "";
                    s = $"{s}{PositionGridSelectedItem.CheckGet("GOODS_NAME")}";
                    s = $"{s}{PositionGridSelectedItem.CheckGet("GOODS_CODE")}";
                    s = $"Техкарта {s}";
                    PositionGrid.Menu["showtk1"].Header=s;
                    
                    if (!string.IsNullOrEmpty(PositionGridSelectedItem.CheckGet("GOODS_PATH_TK")))
                    {
                        PositionGrid.Menu["showtk1"].Visible=true;
                    }
                }

                {
                    var s = "";
                    s = $"{s}{PositionGridSelectedItem.CheckGet("BLANK_NAME")}";
                    s = $"{s}{PositionGridSelectedItem.CheckGet("BLANK_CODE")}";
                    s = $"Техкарта {s}";
                    PositionGrid.Menu["showtk2"].Header=s;
                    
                    if (!string.IsNullOrEmpty(PositionGridSelectedItem.CheckGet("BLANK_PATH_TK")))
                    {
                        PositionGrid.Menu["showtk2"].Visible=true;
                    }
                }

                { 
                    var reportText=PositionGridSelectedItem.CheckGet("CUTTING_REPORT").ToString();
                    if (!string.IsNullOrEmpty(reportText))
                    {
                        PositionGrid.Menu["showreport"].Visible=true;
                    }
                }

                {
                    PositionGrid.Menu["cutting_manual"].Visible=true;
                    double qty = PositionGridSelectedItem.CheckGet("PZTOTALQTY").ToDouble() * PositionGridSelectedItem.CheckGet("PRODUCTS_FROM_BLANK").ToInt();
                    PositionGrid.Menu["cutting_manual"].Enabled = (
                        (PositionGridSelectedItem.CheckGet("STATUS_COLOR") == "blue")
                        || (PositionGridSelectedItem.CheckGet("PRODUCTS_IN_APPLICATION").ToInt() > qty * 1.05));
                }
            }

            ProcessPermissions();
        }
        
        public void PositionGrid2UpdateActions(Dictionary<string, string> selectedItem)
        {
            if (selectedItem != null)
            {
                Position2GridSelectedItem=selectedItem;
            }
            else
            {
                Position2GridSelectedItem=null;
            }

            Position2Grid.Menu["showtk1"].Enabled=true;
            Position2Grid.Menu["showtk2"].Enabled=true;
            
            Position2Grid.Menu["showtk1"].Visible=false;
            Position2Grid.Menu["showtk2"].Visible=false;
            
            if (Position2GridSelectedItem != null)
            {
                {
                    var s = "";
                    s = $"{s}{Position2GridSelectedItem.CheckGet("GOODS_NAME")}";
                    s = $"{s}{Position2GridSelectedItem.CheckGet("GOODS_CODE")}";
                    s = $"Техкарта {s}";
                    Position2Grid.Menu["showtk1"].Header=s;
                    
                    if (!string.IsNullOrEmpty(Position2GridSelectedItem.CheckGet("GOODS_PATH_TK")))
                    {
                        Position2Grid.Menu["showtk1"].Visible=true;
                    }
                }
                {
                    var s = "";
                    s = $"{s}{Position2GridSelectedItem.CheckGet("BLANK_NAME")}";
                    s = $"{s}{Position2GridSelectedItem.CheckGet("BLANK_CODE")}";
                    s = $"Техкарта {s}";
                    Position2Grid.Menu["showtk2"].Header=s;
                    
                    if (!string.IsNullOrEmpty(Position2GridSelectedItem.CheckGet("BLANK_PATH_TK")))
                    {
                        Position2Grid.Menu["showtk2"].Visible=true;
                    }
                }
            }

            ProcessPermissions();
        }
        
        /// <summary>
        /// получение записей
        /// </summary>
        public async void PositionGridLoadItems()
        {

            PositionGridDisableControls();

            bool resume = true;
            int profileId = ProfileTypes.SelectedItem.Key.ToInt();

            if (resume)
            {
                var f = FromDate.Text.ToDateTime();
                var t = ToDate.Text.ToDateTime();
                if (DateTime.Compare(f, t) > 0)
                {
                    var msg = "Дата начала должна быть меньше даты окончания.";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();
                    resume = false;
                }
            }

            if (resume)
            {
                var p = ToolbarGetParams();

                // Виртуальное сырье
                var virtualDict = new Dictionary<string, string>();
                if (PaperGrid.Items != null)
                {
                    if (PaperGrid.Items.Count > 0)
                    {
                        foreach (var row in PaperDS.Items)
                        {
                            if (row["WEIGHT_VIRTUAL"].ToDouble() > 0)
                                virtualDict.Add(row["ID"], row["WEIGHT_VIRTUAL"]);
                        }
                    }
                }
                p.CheckAdd("PROFILE_ID", profileId.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Position");
                q.Request.SetParam("Action", "ListUncutted2");
                //q.Request.SetParam("PROFILE_ID", profileId.ToString());

                q.Request.Timeout = Central.Parameters.RequestTimeoutMax;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.Request.SetParams(p);
                q.Request.SetParam("VIRTUAL", JsonConvert.SerializeObject(virtualDict));

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        if (result.ContainsKey("POSITIONS"))
                        {
                            var ds = ListDataSet.Create(result, "POSITIONS");
                            PositionGridDS = PositionProcessItems(ds);
                            PositionGrid.UpdateItems(PositionGridDS);
                            DoPositionSelectAll();
                        }

                        if (result.ContainsKey("POSITIONS_ADD"))
                        {
                            var ds = ListDataSet.Create(result, "POSITIONS_ADD");
                            Position2GridDS = PositionProcessItems(ds);
                            Position2Grid.UpdateItems(Position2GridDS);
                            DoPosition2SelectAll();
                        }

                        if (result.ContainsKey("REPORT"))
                        {
                            var ds = (ListDataSet)result["REPORT"];
                            ds.Init();
                            if (ds.Items.Count > 0)
                            {
                                var row = ds.Items.First();
                                DebugLog.Text = DebugLog.Text + row.CheckGet("MESSAGE");
                            }
                        }

                        if (result.ContainsKey("PAPER"))
                        {
                            PaperDS = result["PAPER"];
                            PaperDS.Init();
                            PaperGrid.UpdateItems(PaperDS);
                        }

                    }
                }
            }

            PositionGridEnableControls();
            DoUnBlockCuttingButton();

            //FIXME: savebutton
            SaveTasksButton.IsEnabled = true;
        }

        /// <summary>
        /// Обработка данных перед загрузкой в таблицу
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        private ListDataSet PositionProcessItems(ListDataSet ds)
        {
            ListDataSet _ds = ds;

            if (ds.Items != null)
            {
                if (ds.Items.Count > 0)
                {
                    foreach (var item in _ds.Items)
                    {
                        var statusColor = "white";
                        if (item.CheckGet("TYPE_CUT").ToInt() == 0)
                        {
                            statusColor = "green";
                        }
                        else if (item.CheckGet("TYPE_CUT").ToInt() == 2)
                        {
                            statusColor = "yellow";
                        }
                        else if (item.CheckGet("TYPE_CUT").ToInt() == 3)
                        {
                            statusColor = "green";
                        }
                        else if (item.CheckGet("TYPE_CUT").ToInt() == 4)
                        {
                            statusColor = "yellow";
                        }
                        else
                        {
                            if (item.CheckGet("TASKQTY").ToInt() == 0)
                            {
                                statusColor = "blue";
                            }
                            else
                            {
                                int qty = item.CheckGet("QTY").ToInt();
                                int lower_limit = item.CheckGet("LOWER_LIMIT").ToInt();
                                int taskqty = item.CheckGet("TASKQTY").ToInt();

                                if (taskqty < (qty + lower_limit))
                                {
                                    statusColor = "blue";
                                }
                            }
                        }

                        if (CustomPositionListMode)
                        {
                            statusColor = "blue";
                        }

                        item.CheckAdd("STATUS_COLOR", statusColor);

                        // Название заданного типа рилевки
                        string creaseName = "";
                        var creseType = item.CheckGet("CREASETYPE").ToInt();
                        switch (creseType)
                        {
                            case 1:
                                creaseName = "п/м";
                                break;

                            case 2:
                                creaseName = "пл";
                                break;

                            case 4:
                                creaseName = "п/п";
                                break;
                        }
                        item.CheckAdd("CREASE_NAME", creaseName);

                        // Признак сложной схемы пр-ва, т.е. у которой 3 и более этапа в схеме производства
                        item.CheckAdd("_PRODUCTION_SCHEME_STEPS", "0");
                        if (item.CheckGet("PRODUCTION_SCHEME_STEPS").ToInt() > 2)
                        {
                            item["_PRODUCTION_SCHEME_STEPS"] = "1";
                        }
                    }
                }
            }

            return _ds;
        }

        /// <summary>
        /// фильтрация записей
        /// </summary>
        private void PositionGridFilterItems()
        {
            if (PositionGrid.GridItems != null)
            {
                if (PositionGrid.GridItems.Count > 0)
                {
                    /*
                    //обработка строк
                    foreach (var row in PositionGrid.GridItems)
                    {
                        var statusColor="white";
                        if (row.CheckGet("TYPE_CUT").ToInt() == 0)
                        {
                            statusColor="green";
                        }
                        else if (row.CheckGet("TYPE_CUT").ToInt() == 2)
                        {
                            statusColor="yellow";
                        }
                        else if (row.CheckGet("TYPE_CUT").ToInt() == 3)
                        {
                            statusColor="green";
                        }
                        else
                        {
                            if ( row.CheckGet("TASKQTY").ToInt()==0 )
                            {
                                statusColor="blue";
                            }
                            else
                            {
                                int qty = row.CheckGet("QTY").ToInt();
                                int lower_limit = row.CheckGet("LOWER_LIMIT").ToInt();
                                int taskqty = row.CheckGet("TASKQTY").ToInt();

                                if (taskqty < (qty + lower_limit))
                                {
                                    statusColor="blue";
                                }
                            }
                        }

                        if(CustomPositionListMode)
                        {
                            statusColor="blue";
                        }

                        row.CheckAdd("STATUS_COLOR",statusColor);
                    }
                    */
                    //фильтрация 
                    PositionGrid.GridItems = PositionFilterItems(PositionGrid.GridItems, 1);
                    
                }
            }

            PositionPrintButton.IsEnabled=false;
            if (PositionGrid.GridItems != null)
            {
                if (PositionGrid.GridItems.Count > 0)
                {
                    PositionPrintButton.IsEnabled=true;
                }
            }
        }

        /// <summary>
        /// фильтрация записей
        /// </summary>
        private void Position2GridFilterItems()
        {
            if (Position2Grid.GridItems != null)
            {
                if (Position2Grid.GridItems.Count > 0)
                {
                    /*
                    //обработка строк
                    foreach (var row in Position2Grid.GridItems)
                    {
                        var statusColor="white";
                        if (row.CheckGet("TYPE_CUT").ToInt() == 0)
                        {
                            statusColor="green";
                        }
                        else if (row.CheckGet("TYPE_CUT").ToInt() == 2)
                        {
                            statusColor="yellow";
                        }
                        else if (row.CheckGet("TYPE_CUT").ToInt() == 3)
                        {
                            statusColor="green";
                        }
                        else
                        {
                            if ( row.CheckGet("TASKQTY").ToInt()==0 )
                            {
                                statusColor="blue";
                            }
                            else
                            {
                                int qty = row.CheckGet("QTY").ToInt();
                                int lower_limit = row.CheckGet("LOWER_LIMIT").ToInt();
                                int taskqty = row.CheckGet("TASKQTY").ToInt();

                                if (taskqty < (qty + lower_limit))
                                {
                                    statusColor="blue";
                                }
                            }
                        }
                        row.CheckAdd("STATUS_COLOR",statusColor);
                    }
                    */
                    //фильтрация 
                    Position2Grid.GridItems = PositionFilterItems(Position2Grid.GridItems, 2);                    

                }
            }

            Position2PrintButton.IsEnabled=false;
            if (Position2Grid.GridItems != null)
            {
                if (Position2Grid.GridItems.Count > 0)
                {
                    Position2PrintButton.IsEnabled=true;
                }
            }
        }

        public List<Dictionary<string,string>> PositionFilterItems(List<Dictionary<string,string>> items0, int idx)
        {
            var items = new List<Dictionary<string, string>>(items0);

            {
                bool doFiltering5=(bool)Filter5.IsChecked;
                bool doFanfoldFilter = (bool)FanfoldFilter.IsChecked;

                int selectedCorrugators = CorrugatorSet.SelectedItem.Key.ToInt();
                bool doFilteringByCorrugator = selectedCorrugators != 1;


                var typeId = Type.SelectedItem.Key.ToInt();

                bool doFiltering1=false;
                bool doFiltering11=false;
                bool doFiltering2=false;
                bool doFiltering3=false;
                bool doFiltering4=false;
                bool doFilteringScheme = false;
                bool doFilterPartitional = false;
                bool doFilterManual = false;
                bool doFilterKit = false;

                switch (typeId)
                {
                    case 1:
                        doFiltering1=true;
                        break;

                    case 11:
                        doFiltering11=true;
                        break;
                    
                    case 2:
                        doFiltering2=true;
                        break;
                    
                    case 3:
                        doFiltering3=true;
                        break;
                    
                    case 4:
                        doFiltering4=true;
                        break;
                    // Сложная схема производства
                    case 6:
                        doFilteringScheme = true;
                        break;
                    // Решетки
                    case 7:
                        doFilterPartitional = true;
                        break;
                    // Вручную
                    case 8:
                        doFilterManual = true;
                        break;
                    // Комплекты
                    case 9:
                        doFilterKit = true;
                        break;

                    default:
                        doFiltering1=false;
                        doFiltering2=false;
                        doFiltering3=false;
                        doFiltering4=false;
                        doFiltering11=false;
                        doFilteringScheme = false;
                        doFilterPartitional = false;
                        doFilterManual = false;
                        doFilterKit = false;
                        break;
                }

                //первое производство
                bool doFilteringFirstTimeProduction=false;
                if((bool)ApplicationFilterFirst.IsChecked)
                {
                    doFilteringFirstTimeProduction=true;
                }
                
                
                bool doFilteringByProfile=false;
                var selectedProfileType=0;
                if(!string.IsNullOrEmpty(ProfileTypes.SelectedItem.Key))
                {
                    selectedProfileType=ProfileTypes.SelectedItem.Key.ToInt();
                }
                    
                if(selectedProfileType > 0)
                {
                    doFilteringByProfile=true;
                }

                // картон. для idx = 1 берём значение из верхнего фильтра, для idx = 2 - из нижнего
                bool doFilteringByCardboard = false;
                int cardboardId = Cardboard.SelectedItem.Key.ToInt();
                if (idx == 2)
                {
                    cardboardId = Cardboard2.SelectedItem.Key.ToInt();
                }
                if (cardboardId > 0)
                {
                    doFilteringByCardboard = true;
                }

                if(
                    doFiltering1
                    || doFiltering11
                    || doFiltering2
                    || doFiltering3
                    || doFiltering4
                    || doFiltering5
                    || doFilteringScheme
                    || doFilterPartitional
                    || doFilterManual
                    || doFilteringFirstTimeProduction
                    || doFilteringByProfile
                    || doFilteringByCardboard
                    || doFanfoldFilter
                    || doFilteringByCorrugator
                    || doFilterKit
                )
                {
                    items = new List<Dictionary<string, string>>();
                    foreach (var row in items0)
                    {
                        bool includeByFilter1=true;
                        bool includeByFilter11=true;
                        bool includeByFilter2=true;
                        bool includeByFilter3=true;
                        bool includeByFilter4=true;
                        bool includeByFilter5=true;
                        bool includeByScheme = true;
                        bool includeByPartitional = true;
                        bool includeByManual = true;
                        bool includeByProfile=true;
                        bool includeByCardboard = true;
                        bool includeFirstTimeProduction=true;
                        bool includeOnlyFanfold = true;
                        bool includeSelectedCorrugators = true;
                        bool includeByKit = true;

                        //нераскроенные
                        if(doFiltering1)
                        {
                            includeByFilter1=false;
                            if (
                                row.CheckGet("STATUS_COLOR").ToString()=="blue"
                            )
                            {
                                includeByFilter1=true;
                            }
                        }

                        //нераскроенные+раскроенные сейчас
                        if(doFiltering11)
                        {
                            includeByFilter11=false;
                            if (
                                row.CheckGet("STATUS_COLOR").ToString()=="blue"
                                || row.CheckGet("STATUS_COLOR").ToString()=="white"
                            )
                            {
                                includeByFilter11=true;
                            }
                        }

                        

                        //недораскроенные
                        if(doFiltering2)
                        {
                            includeByFilter2=false;
                            // В некоторых случаях для листов количество сделанных листов и количество на складе складывается,
                            // хотя это одни и те же изделия. И задание отображается как перераскроенное, но на самом деле
                            // задание недокроенное. Включим такие задания в фильтр, пусть пользователь сам разбирается,
                            // хватает ли сделанных изделий для исполнения заявки
                            if(row.CheckGet("TYPE_CUT").ToInt() == 0 || row.CheckGet("TYPE_CUT").ToInt() == 3)
                            {
                                // Количество заготовок в ПЗ меньше количества в заявке
                                double qty = row.CheckGet("PZTOTALQTY").ToDouble() * row.CheckGet("PRODUCTS_FROM_BLANK").ToInt();
                                if (row.CheckGet("PRODUCTS_IN_APPLICATION").ToInt() > qty * 1.05)
                                {
                                    includeByFilter2=true;
                                }
                            }
                        }

                        //перераскроенные
                        if(doFiltering3)
                        {
                            includeByFilter3=false;
                            if(row.CheckGet("TYPE_CUT").ToInt()==3)
                            {
                                includeByFilter3=true;
                            }
                        }

                        //неразрешенные
                        if(doFiltering4)
                        {
                            includeByFilter4 = false;
                            if(row.CheckGet("STATUS_COLOR")=="yellow")
                            {
                                includeByFilter4=true;
                            }
                            
                        }

                        //несамокройные
                        if(doFiltering5)
                        {
                            includeByFilter5 = false;
                            if(row.CheckGet("SELFCUT").ToBool()!=true)
                            {
                                includeByFilter5 = true;
                            }
                        }

                        //первое производство
                        if(doFilteringFirstTimeProduction)
                        {
                            includeFirstTimeProduction = false;
                            if(row.CheckGet("FIRST_TIME_PRODUCTION").ToBool()==true)
                            {
                                includeFirstTimeProduction = true;
                            }
                        }

                        //по выбранному профилю
                        if(doFilteringByProfile)
                        {
                            includeByProfile=false;
                            if( row.CheckGet("ID_PROF").ToInt() == selectedProfileType)
                            {
                                includeByProfile=true;
                            }
                        }

                        // по картону
                        if (doFilteringByCardboard)
                        {
                            includeByCardboard = false;
                            if (row.CheckGet("CARDBOARDID").ToInt() == cardboardId)
                            {
                                includeByCardboard = true;
                            }
                        }

                        // Со сложной схемой производства
                        if (doFilteringScheme)
                        {
                            includeByScheme = false;
                            if ((row.CheckGet("STATUS_COLOR").ToString() == "blue")
                                    && (row.CheckGet("PRODUCTION_SCHEME_STEPS").ToInt() > 2))
                            {
                                includeByScheme = true;
                            }
                        }

                        if (doFilterPartitional)
                        {
                            includeByPartitional = false;
                            int schemeId = row.CheckGet("PRODUCTION_SCHEME_ID").ToInt();
                            if (schemeId.ContainsIn(42, 81, 122, 1470, 1546, 1549, 1550, 1561)
                                && (row.CheckGet("STATUS_COLOR").ToString() == "yellow"))
                            {
                                includeByPartitional = true;
                            }
                        }

                        // Фильтр Z-картон
                        if (doFanfoldFilter)
                        {
                            includeOnlyFanfold = false;
                            if (row.CheckGet("ZCARDBOARD").ToBool())
                            {
                                includeOnlyFanfold = true;
                            }
                        }

                        if (doFilterManual)
                        {
                            includeByManual = false;
                            if (row.CheckGet("TYPE_CUT").ToInt() == 4)
                            {
                                includeByManual = true;
                            }
                        }

                        //Фильтр по заданныз гофроагрегатам
                        if (doFilteringByCorrugator)
                        {
                            includeSelectedCorrugators = false;
                            if (row.CheckGet("ALLOWED_MACHINE_SET").ToInt() == selectedCorrugators)
                            {
                                includeSelectedCorrugators = true;
                            }
                        }

                        if (doFilterKit)
                        {
                            includeByKit = false;
                            if (row.CheckGet("KIT_ID").ToInt() > 0)
                            {
                                int productionSchemeId = row.CheckGet("PRODUCTION_SCHEME_ID").ToInt();
                                //Исключаем решетки
                                if (!productionSchemeId.ContainsIn(42, 81, 122, 1470, 1546, 1549, 1550, 1561))
                                {
                                    // Исключаем обычные коробки
                                    if (row.CheckGet("PRODUCT_TYPE") != "02")
                                    {
                                        includeByKit = true;
                                    }
                                }
                            }
                        }

                        if (
                            includeByFilter1
                            && includeByFilter11
                            && includeByFilter2
                            && includeByFilter3
                            && includeByFilter4
                            && includeByFilter5
                            && includeByScheme
                            && includeByPartitional
                            && includeByManual
                            && includeByProfile
                            && includeByCardboard
                            && includeFirstTimeProduction
                            && includeOnlyFanfold
                            && includeSelectedCorrugators
                            && includeByKit
                        )
                        {
                            items.Add(row);
                        }
                        
                    }
                    
                }

            }

            return items;
        }


        /// <summary>
        /// отметка всех строк в гриде "позиции для раскроя"
        /// </summary>
        public void DoPositionSelectAll()
        {
            var selected=(bool)PositionSelectAll.IsChecked;
            
            if(PositionGrid.Items!=null)
            {
                if(PositionGrid.Items.Count>0)
                {
                    foreach(Dictionary<string,string> row in PositionGrid.Items)
                    {
                        if(selected)
                        {
                            var statusColor=row.CheckGet("STATUS_COLOR").ToString();
                            statusColor=statusColor.ToLower();
                            if(statusColor=="blue")
                            {
                                row.CheckAdd("_SELECTED","1");
                            }
                            else
                            {
                                row.CheckAdd("_SELECTED","0");
                            }                            
                        }
                        else
                        {
                            row.CheckAdd("_SELECTED","0");
                        }

                    }
                    PositionGrid.UpdateItems();
                }                  
            }
        }

        /// <summary>
        /// отметка всех строк в гриде "позиции для докроя"
        /// </summary>
        public void DoPosition2SelectAll()
        {
            var selected=(bool)Position2SelectAll.IsChecked;
            
            if(Position2Grid.Items!=null)
            {
                if(Position2Grid.Items.Count>0)
                {
                    foreach(Dictionary<string,string> row in Position2Grid.Items)
                    {
                        if(selected)
                        {
                            var statusColor=row.CheckGet("STATUS_COLOR").ToString();
                            statusColor=statusColor.ToLower();
                            if(statusColor=="blue")
                            {
                                row.CheckAdd("_SELECTED","1");
                            }
                            else
                            {
                                row.CheckAdd("_SELECTED","0");
                            }                            
                        }
                        else
                        {
                            row.CheckAdd("_SELECTED","0");
                        }

                    }
                    Position2Grid.UpdateItems();
                }                  
            }
        }

        /// <summary>
        /// блокировка кнопки "раскроить"
        /// </summary>
        public void DoBlockCuttingButton()
        {
            MakeCuttingButton.IsEnabled=false;
        }

        /// <summary>
        /// разблокировка кнопки "раскроить"
        /// </summary>
        public void DoUnBlockCuttingButton()
        {
            MakeCuttingButton.IsEnabled=true;
            
            TaskGrid.ClearItems();
            TaskGrid.UpdateItems();
        }

        /// <summary>
        /// грид с готовыми заданиями
        /// </summary>
        public void TaskGridInit()
        {
            //список колонок грида
            //у двух гридов одинаковый набор колонок
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="*",
                    Path="_SELECTED",
                    ColumnType=ColumnTypeRef.Boolean,
                    Editable=true,
                    Exportable=false,
                },
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=30,
                },

                new DataGridHelperColumn
                {
                    Header="Картон",
                    Path="CARDBOARD_NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=100,
                    MaxWidth=220,
                },
                new DataGridHelperColumn
                {
                    Header="Номер",
                    Path="NUM",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=30,
                    MaxWidth=40,
                },
                 new DataGridHelperColumn
                {
                    Header="Качество",
                    Path="QID",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header="Формат",
                    Path="FORMAT",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Обрезь, %",
                    Path="TRIM_PERCENT",
                    ColumnType=ColumnTypeRef.Double,
                    Format="N2",
                    MinWidth=40,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Обрезь, мм",
                    Path="TRIM",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Длина",
                    Path="LENGTH",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Фиксированный вес",
                    Path="FIXED_WEIGHT_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Самокройное",
                    Path="SELFCUT",
                    ColumnType=ColumnTypeRef.Boolean,
                },


                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="NAME1",
                    Group="Стекер 1",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=250,
                    MaxWidth=350,
                    FormatterRaw= (v) =>
                    {
                        var result = "";  
                        result=$"{v.CheckGet("NAME1")} ({v.CheckGet("ID_ORDERDATES1")})";
                        return result;
                    }
                },
                new DataGridHelperColumn
                {
                    Header="Потоков",
                    Path="THREAD1",
                    Group="Стекер 1",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="QTY1",
                    Group="Стекер 1",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header="Опытная партия",
                    Path="O1",
                    Group="Стекер 1",
                    ColumnType=ColumnTypeRef.Boolean,                    
                },

                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="NAME2",
                    Group="Стекер 2",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=250,
                    MaxWidth=350,
                    FormatterRaw= (v) =>
                    {
                        var result = "";  
                        result=$"{v.CheckGet("NAME2")} ({v.CheckGet("ID_ORDERDATES2")})";
                        return result;
                    }
                },
                new DataGridHelperColumn
                {
                    Header="Потоков",
                    Path="THREAD2",
                    Group="Стекер 2",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="QTY2",
                    Group="Стекер 2",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header="Опытная партия",
                    Path="O2",
                    Group="Стекер 2",
                    ColumnType=ColumnTypeRef.Boolean,                    
                },


                          

                new DataGridHelperColumn
                {
                    Header="1",
                    Path="LAYER_5_RAWGROUP",
                    Group="Слой",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=40,
                },   
                new DataGridHelperColumn
                {
                    Header="2",
                    Path="LAYER_4_RAWGROUP",
                    Group="Слой",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=40,
                },   
                new DataGridHelperColumn
                {
                    Header="3",
                    Path="LAYER_3_RAWGROUP",
                    Group="Слой",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=40,
                },   
                new DataGridHelperColumn
                {
                    Header="4",
                    Path="LAYER_2_RAWGROUP",
                    Group="Слой",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=40,
                },   
                new DataGridHelperColumn
                {
                    Header="5",
                    Path="LAYER_1_RAWGROUP",
                    Group="Слой",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=40,
                },   
                
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="DESCRIPTION",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=40,
                }, 
                new DataGridHelperColumn
                {
                    Header="Отчет",
                    Path="CUTTING_REPORT",
                    Exportable=false,
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=40,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Тандем",
                    Path="TANDEM_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                },
                new DataGridHelperColumn
                {
                    Header="ПШ",
                    Path="_FULL_STAMP",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=30,
                    MaxWidth=30,
                },
                new DataGridHelperColumn
                {
                    Header="ПШ+Rt",
                    Path="_FULL_STAMP_RT",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=30,
                    MaxWidth=30,
                },


                new DataGridHelperColumn
                {
                    Header="заявка 1",
                    Path="ID_ORDERDATES1",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    Visible=false,
                }, 
                new DataGridHelperColumn
                {
                    Header="заявка 2",
                    Path="ID_ORDERDATES2",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    Visible=false,
                },  
                new DataGridHelperColumn
                {
                    Header="длина",
                    Name="LENGTH2",
                    Path="LENGTH",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    Visible=false,
                }, 
                new DataGridHelperColumn
                {
                    Header="1_сырье",
                    Path="ID_RAW_GROUP5",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    Visible=false,
                },  
                new DataGridHelperColumn
                {
                    Header="1_вес",
                    Path="WEIGHT5",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    Visible=false,
                },  
                new DataGridHelperColumn
                {
                    Header="2_сырье",
                    Path="ID_RAW_GROUP4",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    Visible=false,
                },   
                new DataGridHelperColumn
                {
                    Header="2_вес",
                    Path="WEIGHT4",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="3_сырье",
                    Path="ID_RAW_GROUP3",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    Visible=false,
                },   
                new DataGridHelperColumn
                {
                    Header="3_вес",
                    Path="WEIGHT3",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="4_сырье",
                    Path="ID_RAW_GROUP2",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    Visible=false,
                },   
                new DataGridHelperColumn
                {
                    Header="4_вес",
                    Path="WEIGHT2",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="5_сырье",
                    Path="ID_RAW_GROUP1",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    Visible=false,
                },   
                new DataGridHelperColumn
                {
                    Header="5_вес",
                    Path="WEIGHT1",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    Visible=false,
                },
            };


            //row stylers
            var rowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                {
                    StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        if(row.CheckGet("LOW_TASK_FLAG").ToInt()==1)
                        {
                            //кроткое задание (< 400 м)
                            //и нет больше заданий (нераскроенных позиций)  с такими же условиями: 
                            //формат + марка картона
                            color = HColor.Orange;
                        }
                        else if (row.CheckGet("LOW_TASK_FLAG").ToInt() == 2)
                        {
                            //кроткое задание (< 400 м)
                            color = HColor.Yellow;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
                {
                    StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                                        

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };

            //инициализация грида 
            {
                TaskGrid.SetColumns(columns);
                TaskGrid.SetRowStylers(rowStylers);
                TaskGrid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);                
                TaskGrid.SearchText = TaskSearchText;
                TaskGrid.AutoUpdateInterval=0;
                TaskGrid.Init();
                

                //при выборе строки в гриде, обновляются актуальные действия для записи
                TaskGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {
                        TaskGridUpdateActions(selectedItem);
                    }
                };

                //данные грида
                TaskGrid.OnLoadItems     = TaskGridLoadItems;
                TaskGrid.OnFilterItems   = TaskGridFilterItems;
                TaskGrid.Run();
                
            }
        }

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem"></param>
        public void TaskGridUpdateActions(Dictionary<string, string> selectedItem)
        {
            if (selectedItem != null)
            {
                TaskGridSelectedItem=selectedItem;
            }
            else
            {
                TaskGridSelectedItem=null;
            }

            
            //удалить можно, если выбраны строки
            DeleteButton.IsEnabled=false;
            if(TaskGridSelectedItem!=null)
            {
                DeleteButton.IsEnabled=true;
            }

            //действия доступны, только если в гиде есть строки
            if(TaskGrid.Items.Count>0)
            {
                TaskGridToolbar.IsEnabled=true;
            }
            else
            {
                TaskGridToolbar.IsEnabled=false;
            }

            ProcessPermissions();
        }

      
        /// <summary>
        /// получение записей
        /// </summary>
        public async void TaskGridLoadItems()
        {
            TaskGridToolbar.IsEnabled = false;
            TaskGrid.ShowSplash();

            bool resume = true;

            if (resume)
            {   
                var p = ToolbarGetParams();

                var q = new LPackClientQuery();
                q.Request.SetParam("Module","Production");
                q.Request.SetParam("Object","ProductionTask");
                q.Request.SetParam("Action","ListAutoCutted");
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
                        TaskGridDS=ListDataSet.Create(result,"TASKS");
                        TaskGrid.UpdateItems(TaskGridDS);      
                    }
                }
            }

            TaskGridToolbar.IsEnabled = true;
            TaskGrid.HideSplash();
        }

        /// <summary>
        /// фильтрация записей
        /// </summary>
        private async void TaskGridFilterItems()
        {
            TaskTotals.Content="";

            if (TaskGrid.GridItems != null)
            {
                if (TaskGrid.GridItems.Count > 0)
                {
                    //статистика по заданиям
                    double lengthTotal=0;
                    double trimTotal=0;
                    int unselfCnt=0;
                    int selfCnt=0;

                    //обработка строк
                    foreach (var row in TaskGrid.GridItems)
                    {
                        var selfcut=0;
                        if(!string.IsNullOrEmpty(row.CheckGet("NAME1")) && !string.IsNullOrEmpty(row.CheckGet("NAME2")))
                        {
                            unselfCnt++;
                            selfcut=0;
                        }
                        else
                        {
                            selfCnt++;
                            selfcut=1;
                        }
                        row.CheckAdd("SELFCUT",selfcut.ToString());

                        lengthTotal  += row.CheckGet("LENGTH").ToDouble();
                        trimTotal    += row.CheckGet("LENGTH").ToDouble() * row.CheckGet("TRIM_PERCENT").ToDouble();
                    }

                    if (lengthTotal>0)
                    {
                        var n="";

                        var a=(double)(trimTotal) / (double)(lengthTotal);                        
                        a=Math.Round(a,2);
                        if(a>100)
                        {
                            a=0;
                            n="?";
                        }

                        var all=selfCnt + unselfCnt;

                        var b=(double)selfCnt / (double)(all);
                        b=b*100;
                        b=Math.Round(b,2);
                        if(b>100)
                        {
                            b=0;
                            n="?";
                        }

                        var c=(double)unselfCnt / (double)(all);
                        c=c*100;
                        c=Math.Round(c,2);
                        if(c>100)
                        {
                            c=0;
                            n="?";
                        }

                        TaskTotals.Content=$"Заданий: {all}. Обрезь: {a} %.  Самокройные / несамокройные: {selfCnt} ({b}%) / {unselfCnt} ({c}%) {n}";
                                
                    }

                    //фильтрация 
                    {
                       

                    }

                }
            }

            
            SaveTasksButton.IsEnabled=false;
            DeleteButton.IsEnabled=false;
            TaskExportButton.IsEnabled=false;

            if (TaskGrid.GridItems != null)
            {
                if (TaskGrid.GridItems.Count > 0)
                {                
                    SaveTasksButton.IsEnabled=true;
                    DeleteButton.IsEnabled=true;
                    TaskExportButton.IsEnabled=true;
                }                
            }

        }

       
        
        /// <summary>
        /// грид с сырьём в наличии
        /// </summary>
        public void PaperGridInit()
        {
            //список колонок грида
            var columns = new List<DataGridHelperColumn>
            {
               
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=35,
                    MaxWidth=35,
                },

                new DataGridHelperColumn
                {
                    Header="Бумага",
                    Doc="Наименование бумаги",
                    Path="NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=120,
                    MaxWidth=180,
                },
                new DataGridHelperColumn
                {
                    Header="Формат, мм",
                    Doc="Формат полотна, мм",
                    Path="FORMAT",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=80,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Приоритет",
                    Doc="Приоритет расхода данной бумаги",
                    Path="PRIORITY",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header="Наличие, кг",
                    Doc="Количество бумаги на складе в наличии, кг",
                    Path="WEIGHT",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=80,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="На кондиционировании, кг",
                    Doc="Количество бумаги на складе на кондиционировании (пока недоступно), кг",
                    Path="WEIGHT_CONDITIONING",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=80,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="ПЗ БДМ, кг",
                    Doc="Количество бумаги в ПЗ БДМ, кг",
                    Path="WEIGHT_PAPER_MACHINE",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=80,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Дата выхода из БДМ",
                    Doc="Ближайшая дата выхода бумаги из БДМ",
                    Path="PAPER_PRODUCTION_DATE",
                    ColumnType=ColumnTypeRef.DateTime,
                    Format="dd.MM.yyyy HH:mm",
                    MinWidth=110,
                    MaxWidth=110,
                },
                new DataGridHelperColumn
                {
                    Header="ПЗ ГА, кг",
                    Doc="Количество бумаги в ПЗ ГА, кг",
                    Path="WEIGHT_CORRUGATING_MACHINE",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=80,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Виртуальная, кг",
                    Doc="Виртуальный остаток, кг",
                    Path="WEIGHT_VIRTUAL",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=80,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="В раскрое кг",
                    Doc="Количество в данном раскрое, кг",
                    Path="WEIGHT_CUTTING",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=80,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Остаток, кг",
                    Path="WEIGHT_RESIDUAL",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=80,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header=" ",
                    Path="_",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=10,
                    MaxWidth=1500,
                },
            };


            //инициализация грида 
            {
                PaperGrid.SetColumns(columns);
                PaperGrid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);                
                PaperGrid.Init();
                

                //при выборе строки в гриде, обновляются актуальные действия для записи
                PaperGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {
                        PaperGridUpdateActions(selectedItem);
                    }
                };

                PaperGrid.OnDblClick = (Dictionary<string, string> selectedItem) =>
                {
                    VirtualSourceEdit();
                };

                
                TaskGrid.Menu=new Dictionary<string,DataGridContextMenuItem>()
                {
                    { "edit", new DataGridContextMenuItem(){
                        Header="Виртуальное сырье",
                        Tag = "access_mode_full_access",
                        Action=()=>
                        {
                            VirtualSourceEdit();
                        }
                    }},
                };


                //данные грида
                //PaperGrid.OnLoadItems     = PaperGridLoadItems;
                PaperGrid.OnFilterItems   = PaperGridFilterItems;
                PaperGrid.Run();
                
            }
        }

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem"></param>
        public void PaperGridUpdateActions(Dictionary<string, string> selectedItem)
        {
            if (selectedItem != null)
            {
                PaperGridSelectedItem=selectedItem;
            }
            else
            {
                PaperGridSelectedItem=null;
            }                                 
        }

      
        

        /// <summary>
        /// фильтрация записей
        /// </summary>
        private void PaperGridFilterItems()
        {
            if (PaperGrid.GridItems != null)
            {
                if (PaperGrid.GridItems.Count > 0)
                {
                    bool doAvailableOnly=(bool)AvailableOnly.IsChecked;
                    var format = PaperFormat.SelectedItem.Key.ToInt();
                    var density = PaperDensity.SelectedItem.Key.ToInt();

                    if(doAvailableOnly || (format > 0) || (density > 0))
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (var row in PaperGrid.GridItems)
                        {
                            bool includeAvailableOnly=true;
                            bool includeFormat = true;
                            bool includeDensity = true;

                            if(doAvailableOnly)
                            {
                                includeAvailableOnly = false;
                                if (row.CheckGet("WEIGHT").ToInt()>0)
                                {
                                    includeAvailableOnly=true;
                                }
                            }

                            if (format > 0)
                            {
                                includeFormat = false;
                                if (row["FORMAT"].ToInt() == format)
                                {
                                    includeFormat = true;
                                }
                            }

                            if (density > 0)
                            {
                                includeDensity = false;
                                if (row["DENSITY"].ToInt() == density)
                                {
                                    includeDensity = true;
                                }
                            }

                            if (
                                    includeAvailableOnly
                                    && includeFormat
                                    && includeDensity)
                            {
                                items.Add(row);
                            }

                        }
                        PaperGrid.GridItems = items;
                    }

                }
            }
        }


        /// <summary>
        /// Подготовка таблицы для вкладки итогов
        /// </summary>
        /// <param name="header">Словарь для формирования заголока таблицы</param>
        public void TotalGridInit(Dictionary<string,string> header)
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Картон",
                    Doc="Название марки картона",
                    Path="CARDBOARD",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=80,
                    MaxWidth=200,
                },
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Профиль",
                    Path="PROFILE",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            // колонки с форматами
            foreach (var c in header)
            {
                columns.Add(new DataGridHelperColumn
                {
                    Header = c.Key,
                    Path = c.Key,
                    ColumnType = ColumnTypeRef.String,
                    MinWidth = 80,
                    MaxWidth = 80,
                });
            }

            columns.Add(new DataGridHelperColumn
            {
                Header = "_",
                Path = "",
                ColumnType = ColumnTypeRef.String,
                MinWidth = 10,
                MaxWidth = 5000,
            });


            TotalGrid.SetColumns(columns);
            TotalGrid.PrimaryKey="_ROWNUMBER";
            TotalGrid.SetSorting("CARDBOARD", ListSortDirection.Ascending);
            TotalGrid.UseSorting = false;
            TotalGrid.AutoUpdateInterval = 0;

            TotalGrid.OnFilterItems = FilterTotalItems;
            TotalGrid.Init();
        }

        /// <summary>
        /// Получение данных для таблицы итогов
        /// </summary>
        public async void TotalLoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "ProductionTask");
            q.Request.SetParam("Action", "TotalFormatLength");
            q.Request.SetParam("FACTORY_ID", Factory.SelectedItem.Key);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var totalDS = new ListDataSet();
                totalDS.Init();
                var cardboards = new Dictionary<string, string>();
                var list = new List<Dictionary<string, string>>();

                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "TOTAL_FORMAT_LENGTH");
                    // временный словарь для удобного заполнения датасета данными
                    var l = new Dictionary<string, string>();
                    // Сформируем полный набор колонок
                    foreach (var item in ds.Items)
                    {
                        int format = item["WEB_WIDTH"].ToInt();
                        cardboards.CheckAdd(item["COMPOSITION"], item["CARDBOARD"]);
                        l.Add($"{item["COMPOSITION"]}-{format}", item["LENGTH"].ToInt().ToString());
                    }

                    // формируем датасет и заполняем его данными
                    int j = 1;

                    foreach (var c in cardboards)
                    {
                        var d = new Dictionary<string, string>()
                        {
                            { "_ROWNUMBER", j.ToString() },
                            { "CARDBOARD", c.Value },
                        };

                        // Добавим профиль, код профиля первый в композиции
                        var p = c.Key.Split('-');
                        d.Add("PROFILE", p[0]);

                        foreach (var format in Formats)
                        {
                            var k = $"{c.Key}-{format.Key}";
                            var s = "";
                            if (l.ContainsKey(k))
                            {
                                s = l[k];
                            }
                            d.Add(format.Key, s);
                        }
                        list.Add(d);
                        j++;
                    }

                    totalDS.Items = list;
                }
                TotalGridInit(Formats);
                TotalGrid.UpdateItems(totalDS);
                TotalToExcelButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Обработка и фильтрация строк таблицы итогов
        /// </summary>
        public void FilterTotalItems()
        {
            var profileId = ProfileTotals.SelectedItem.Key.ToInt();

            if (profileId > 0)
            {
                if (TotalGrid.GridItems != null)
                {
                    if (TotalGrid.GridItems.Count > 0)
                    {
                        var list = new List<Dictionary<string, string>>();

                        foreach (var item in TotalGrid.GridItems)
                        {
                            if (item.CheckGet("PROFILE").ToInt() == profileId)
                            {
                                list.Add(item);
                            }
                        }

                        TotalGrid.GridItems = list;
                    }
                }
            }
        }

        /// <summary>
        /// получение данных из формы параметров
        /// </summary>
        /// <param name="loadRows"></param>
        /// <returns></returns>
        public Dictionary<string, string> ToolbarGetParams(bool loadRows=false)
        {
            //FIXME: нужно переделать работу с этими параметрами через FormHelper
            
            var p=new Dictionary<string, string>();

            p.Add("FROMDATE",   FromDate.Text);
            p.Add("TODATE",     ToDate.Text);

            if(Formats.Count>0)
            {
                var formatsString=JsonConvert.SerializeObject(Formats);
                p.Add("FORMATS", formatsString);
            }

            p.Add("WITHPACK", CuttingWithPack.IsChecked.ToInt().ToString());
            p.Add("WITHLONG", CuttingWithLong.IsChecked.ToInt().ToString());            
            p.Add("TRIM", Trim.Text.ToString());
            p.Add("DEVIATION", Deviation.Text.ToString());
            p.Add("TASKMINLENGTH", TaskMinLength.Text.ToString());
            p.Add("TRIM10", Trim10.IsChecked.ToInt().ToString());
            p.Add("ADDDAYS", AddDays.SelectedItem.Key);
            p.Add("TRIM_MATRIX", TrimMatrix.IsChecked.ToInt().ToString());
            p.Add("FACTORY_ID", Factory.SelectedItem.Key);
          

            //выбранные строки
            if(loadRows)
            {
                //основные позиции
                var selectedPositions = new Dictionary<string, Dictionary<string, string>>();

                if (PositionGrid.GridItems != null)
                {
                    foreach (var item in PositionGrid.GridItems)
                    {
                        if (item.CheckGet("_SELECTED").ToBool() && (item.CheckGet("POSITIONID").ToInt() > 0))
                        {
                            var d = new Dictionary<string, string>()
                            {
                                { "PALLET_BALANCE" , item.CheckGet("CURQTY") },
                                { "PALLET_PT_BALANCE", item.CheckGet("CURPZQTY") },
                                { "PRODUCTION_SHIPPED", item.CheckGet("RQTY") },
                                { "BLANK_QUANTITY", item.CheckGet("PZTOTALQTY") },
                                { "BLANK_UNCOMPLETE_PT_QUANTITY", item.CheckGet("PZQTY") },
                                { "BLANK_COMPLETE_PT_QUANTITY", item.CheckGet("PZZAGQTY") },
                                { "FIRST_TIME_PRODUCTION", item.CheckGet("FIRST_TIME_PRODUCTION") },
                                { "RIG_IS", item.CheckGet("RIG_IS") },
                            };
                            selectedPositions.CheckAdd(item.CheckGet("POSITIONID"), d);
                        }
                    }
                }
                var rows = JsonConvert.SerializeObject(selectedPositions);
                p.Add("ROWS", rows.ToString());

                //позиции для докроя
                var selectedPositionsAfter = new Dictionary<string, Dictionary<string, string>>();
                if (Position2Grid.GridItems != null)
                {
                    if (Position2Grid.GridItems.Count > 0)
                    {
                        foreach (var item in Position2Grid.GridItems)
                        {
                            if (item.CheckGet("_SELECTED").ToBool() && (item.CheckGet("POSITIONID").ToInt() > 0))
                            {
                                var d = new Dictionary<string, string>()
                                {
                                    //{ "BLANK_FOR_CUTTING", item.CheckGet("BLANK_FOR_CUTTING") },
                                    { "PALLET_BALANCE" , item.CheckGet("PALLET_BALANCE") },
                                    { "PALLET_PT_BALANCE", item.CheckGet("PALLET_PT_BALANCE") },
                                    { "PRODUCTION_SHIPPED", item.CheckGet("PRODUCTION_SHIPPED") },
                                    { "BLANK_QUANTITY", item.CheckGet("BLANK_QUANTITY") },
                                    { "BLANK_UNCOMPLETE_PT_QUANTITY", item.CheckGet("BLANK_UNCOMPLETE_PT_QUANTITY") },
                                    { "BLANK_COMPLETE_PT_QUANTITY", item.CheckGet("BLANK_COMPLETE_PT_QUANTITY") },
                                    { "FIRST_TIME_PRODUCTION", item.CheckGet("FIRST_TIME_PRODUCTION") },
                                };
                                selectedPositionsAfter.CheckAdd(item.CheckGet("POSITIONID"), d);
                            }
                        }
                    }
                }
                var rows2 = JsonConvert.SerializeObject(selectedPositionsAfter);
                p.CheckAdd("ROWS2", rows2.ToString());
                
            }

            //debug
            if(CustomPositionListMode)
            {
                p.CheckAdd("CUSTOM_POSITION_LIST_MODE", CustomPositionListMode.ToInt().ToString());    
                p.CheckAdd("POSITION_ID", PositionIdList);
                p.CheckAdd("POSITION_QUANTITY", PositionQuantityList);
            }
            

            return p;
        }



        /// <summary>
        /// автораскрой выбранных позиций
        /// </summary>
        private async void MakeCutting()
        {
            PositionGridDisableControls();           

            bool resume = true;
            int profileId = ProfileTypes.SelectedItem.Key.ToInt();

            if (resume)
            {
                var f = FromDate.Text.ToDateTime();
                var t = ToDate.Text.ToDateTime();
                if (DateTime.Compare(f, t) > 0)
                {
                    var msg = "Дата начала должна быть меньше даты окончания.";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();
                    resume = false;
                }
            }

            if (resume)
            {
                var p = ToolbarGetParams(true);

                // Виртуальное сырье
                var virtualDict = new Dictionary<string, string>();
                if (PaperGrid.Items != null)
                {
                    if (PaperGrid.Items.Count > 0)
                    {
                        foreach (var row in PaperGrid.Items)
                        {
                            if (row["WEIGHT_VIRTUAL"].ToDouble() > 0)
                                virtualDict.Add(row["ID"], row["WEIGHT_VIRTUAL"]);
                        }
                    }
                }
                p.CheckAdd("PROFILE_ID", profileId.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module","Production");
                q.Request.SetParam("Object","Position");
                q.Request.SetParam("Action","MakeCutting");

                q.Request.Timeout=300000;

                q.Request.SetParams(p);
                q.Request.SetParam("VIRTUAL", JsonConvert.SerializeObject(virtualDict));

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if(q.Answer.Status == 0)                
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                    if(result!=null)
                    {
                        var dsp1 = ListDataSet.Create(result, "POSITIONS");
                        PositionGridDS = PositionProcessItems(dsp1);
                        PositionGrid.UpdateItems(PositionGridDS);

                        var dsp2 = ListDataSet.Create(result, "POSITIONS_ADD");
                        Position2GridDS = PositionProcessItems(dsp2);
                        Position2Grid.UpdateItems(Position2GridDS);

                        TaskGridDS = ListDataSet.Create(result, "TASKS");
                        TaskGrid.UpdateItems(TaskGridDS);


                        if (result.ContainsKey("REPORT"))
                        {
                            var ds=(ListDataSet)result["REPORT"];
                            ds.Init();
                            if(ds.Items.Count>0)
                            {
                                var row=ds.Items.First();
                                DebugLog.Text=DebugLog.Text+row.CheckGet("MESSAGE");
                            }
                        }

                        if(result.ContainsKey("PAPER"))
                        {
                            PaperDS = result["PAPER"];
                            PaperDS.Init();
                            PaperGrid.UpdateItems(PaperDS);
                        }

                        TotalLoadItems();
                        
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            PositionGridEnableControls();

            if(TaskGridDS.Items.Count>0)
            {
                DoBlockCuttingButton();
            }
            else
            {
                DoUnBlockCuttingButton();
            }
            
        }

        public void PositionGridDisableControls()
        {
            PositionGridToolbar.IsEnabled = false;
            PositionGrid.ShowSplash();

            Position2GridToolbar.IsEnabled = false;
            Position2Grid.ShowSplash();

            TaskGridToolbar.IsEnabled = false;
            TaskGrid.ShowSplash();

            PositionGridSplash.Visibility=Visibility.Visible;
        }

        public void PositionGridEnableControls()
        {
            PositionGridToolbar.IsEnabled = true;
            PositionGrid.HideSplash();

            Position2GridToolbar.IsEnabled = true;
            Position2Grid.HideSplash();

            TaskGridToolbar.IsEnabled = true;
            TaskGrid.HideSplash();

            PositionGridSplash.Visibility=Visibility.Collapsed;
        }

        /// <summary>
        /// загрузка вспомогательных данных для построения интерфейса
        /// </summary>
        public async void LoadRef()
        {
            PositionGridToolbar.IsEnabled = false;
            PositionGrid.ShowSplash();
            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();

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
                        {
                            var profileDS = ListDataSet.Create(result, "PROFILES");
                            var list = new Dictionary<string, string>();
                            list.Add("0", "");
                            list.AddRange<string, string>(profileDS.GetItemsList("ID", "NAME"));
                            ProfileTypes.Items = list;
                            ProfileTypes.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");

                            // Список профилей на вкладке итогов
                            ProfileTotals.Items = list;
                            ProfileTotals.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
                        }

                        {
                            CardboardDS = ListDataSet.Create(result, "CARDBOARD");
                            var list = new Dictionary<string, string>();
                            list.Add("0", "");
                            list.AddRange<string, string>(CardboardDS.GetItemsList("ID", "NAME"));
                            Cardboard.Items = list;
                            Cardboard.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");

                            Cardboard2.Items.AddRange<string, string>(list);
                            Cardboard2.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
                        }

                        {
                            var corrugatorSetDS = ListDataSet.Create(result, "CORRUGATOR_SET");
                            CorrugatorSet.Items = corrugatorSetDS.GetItemsList("ID", "NAME");
                            CorrugatorSet.SetSelectedItemByKey("1");
                        }

                        {
                            var densityDS = ListDataSet.Create(result, "DENSITY");
                            var densityList = new Dictionary<string, string>();
                            densityList.Add("0", " ");
                            foreach (var d in densityDS.Items)
                            {
                                densityList.CheckAdd(d["ID"], d["DENSITY"].ToInt().ToString());
                            }
                            PaperDensity.Items = densityList;
                            PaperDensity.SelectedItem = densityList.FirstOrDefault((x) => x.Key == "0");
                        }

                        {
                            var factoryDS = ListDataSet.Create(result, "FACTORY");
                            Factory.Items = factoryDS.GetItemsList("ID", "NAME");
                            Factory.SetSelectedItemByKey("1");
                        }

                        if(result.ContainsKey("FORMATS"))
                        {
                            var ds=(ListDataSet)result["FORMATS"];
                            ds?.Init();

                            var list=new Dictionary<string,string>();                            
                            list.AddRange<string,string>(ds.GetItemsList("PAWI_ID","WIDTH"));

                            var paperList = new Dictionary<string, string>() { { "-1", "Все" } };

                            if(list.Count>0)
                            {
                                Formats=new Dictionary<string, string>();
                                FormatContainer.Children.Clear();

                                foreach(KeyValuePair<string,string> i in list)
                                {
                                    var k=i.Value.ToString();
                                    var v="1";
                                    Formats.CheckAdd(k,v);
                                    paperList.CheckAdd(k, k);

                                    var checkBox=new CheckBox();
                                    {
                                        checkBox.Name=$"Format_{k}";
                                        checkBox.Content=$"{i.Value}";
                                        checkBox.AddHandler(
                                            CheckBox.ClickEvent,
                                            new RoutedEventHandler((o, e) =>
                                            {                                            
                                                if(o!=null)
                                                {
                                                    var el=(CheckBox)o;
                                                    if(el!=null)
                                                    {
                                                        var k=el.Name;
                                                        k=k.Replace("Format_","");
                                                        var v=el.IsChecked.ToInt().ToString();
                                                        Formats.CheckAdd(k,v);
                                                    }
                                                }                                           
                                            })
                                        );
                                        checkBox.Style=(Style)FormatContainer.TryFindResource("CheckBoxTopPanel");
                                        if(v.ToInt()==1)
                                        {
                                            checkBox.IsChecked=true;
                                        }
                                        else
                                        {
                                            checkBox.IsChecked=false;
                                        }
                                        FormatContainer.Children.Add(checkBox);
                                    }

                                }

                                PaperFormat.Items = paperList;
                                PaperFormat.SelectedItem = paperList.FirstOrDefault(x => x.Key == "-1");
                            }
                        }
                        
                    }
                }
            }
            

            PositionGridToolbar.IsEnabled = true;
            PositionGrid.HideSplash();
        }


        /// <summary>
        /// удаление выбранных заданий из предварительного списка
        /// </summary>
        public async void TaskDelete()
        {
            bool resume = true;

            var selectedItems=new List<int>();
            var selectedItemsNumbers=new List<string>();

            if(resume)
            {
                if(TaskGridSelectedItem!=null)
                {
                    if(TaskGridSelectedItem.CheckGet("TASKID").ToInt()!=0)
                    {
                        selectedItems.Add(TaskGridSelectedItem.CheckGet("TASKID").ToInt());
                        selectedItemsNumbers.Add(TaskGridSelectedItem.CheckGet("NUM").ToString());
                    }
                    else
                    {
                        resume=false;
                    }
                }
                else
                {
                    resume=false;
                }
            }

            if(resume)
            {
                if(TaskGrid.GridItems!=null)
                {
                    if(TaskGrid.GridItems.Count>0)
                    {
                        foreach(Dictionary<string, string> row in TaskGrid.GridItems)
                        {
                            if(row.CheckGet("_SELECTED").ToInt()==1)
                            {
                                int i=row.CheckGet("TASKID").ToInt();
                                if(!selectedItems.Contains(i))
                                {
                                    selectedItems.Add(i);
                                    selectedItemsNumbers.Add(row.CheckGet("NUM").ToString());
                                }                                
                            }
                        }
                    }
                }
            }
                
            if(resume)
            {
                var msg = "";
                msg = $"{msg}Удалить производственные задания из списка?\n";

                if(selectedItems.Count>0)
                {
                    var s="";
                    foreach(string itemNumber in selectedItemsNumbers)
                    {
                        s=$"{s} {itemNumber}";
                    }
                    msg = $"{msg}Номера заданий:\n";
                    msg = $"{msg}{s}\n";
                }

                msg = $"{msg}\n";
                msg = $"{msg}Задания будут удалены из списка заданий на сохранение.\n";

                var d = new DialogWindow($"{msg}", "Удаление производственных заданий", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() != true)
                {
                    resume = false;
                }

            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                var rows=JsonConvert.SerializeObject(selectedItems);
                p.Add("ITEMS",rows.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module","Production");
                q.Request.SetParam("Object","ProductionTask");
                q.Request.SetParam("Action","DeleteAutoCutted");

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if(q.Answer.Status == 0)                
                {
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "ProductionTaskCutted",
                        ReceiverName = "TaskList",
                        SenderName = "CuttingAutoView",
                        Action = "Refresh",
                    });
                }
            }
            
        }

        /// <summary>
        /// сохранение всех заданий из пердварительного списка
        /// </summary>
        public async void TaskSave()
        {
            TaskGridDisableControls();
            
            bool resume = true;

            if(resume)
            {
                if(TaskGrid.GridItems!=null)
                {
                    if(TaskGrid.GridItems.Count>0)
                    {
                    }
                    else
                    {
                        resume=false;
                    }
                }
                else
                {
                    resume=false;
                }
            }

            if (resume)
            {
                //FIXME: savebutton
                SaveTasksButton.IsEnabled=false;

                var p = new Dictionary<string, string>();

                var q = new LPackClientQuery();
                q.Request.SetParam("Module","Production");
                q.Request.SetParam("Object","ProductionTask");
                q.Request.SetParam("Action","SaveAutoCutted");

                q.Request.SetParams(p);
                q.Request.Timeout=300000;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if(q.Answer.Status == 0)                
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                    if(result!=null)
                    {
                        if(result.ContainsKey("REPORT"))
                        {
                            var ds=(ListDataSet)result["REPORT"];
                            ds.Init();
                            if(ds.Items.Count>0)
                            {
                                var row=ds.Items.First();
                                DebugLog.Text=DebugLog.Text+row.CheckGet("MESSAGE");
                            }
                        }
                    }

                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "ProductionTaskCutted",
                        ReceiverName = "PositionList",
                        SenderName = "CuttingAuto",
                        Action = "Refresh",
                    });

                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "ProductionTaskCutted",
                        ReceiverName = "TaskList",
                        SenderName = "CuttingAuto",
                        Action = "Clear",
                    });

                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "ProductionTask",
                        ReceiverName = "TaskList",
                        SenderName = "CuttingAuto",
                        Action = "Refresh",
                    });
                }
                else
                {
                    q.ProcessError();
                }
            }
            else
            {
                const string msg = "Нет заданий для сохранения";
                var d = new DialogWindow($"{msg}", "Сохранение заданий");
                d.ShowDialog();
            }

            TaskGridEnableControls();
        }

        private void TaskGridDisableControls()
        {
            PositionGridToolbar.IsEnabled = false;
            PositionGrid.ShowSplash();
            
            Position2GridToolbar.IsEnabled = false;
            Position2Grid.ShowSplash();
            
            TaskGridToolbar.IsEnabled = false;
            TaskGrid.ShowSplash();
            TaskGridSplash.Visibility=Visibility.Visible;
        }

        private void TaskGridEnableControls()
        {
            PositionGridToolbar.IsEnabled = true;
            PositionGrid.HideSplash();
            
            Position2GridToolbar.IsEnabled = true;
            Position2Grid.HideSplash();
            
            TaskGridToolbar.IsEnabled = true;
            TaskGrid.HideSplash();
            TaskGridSplash.Visibility=Visibility.Collapsed;
        }

        /// <summary>
        /// генерация печатной формы со списком позиций
        /// </summary>
        /// <param name="mode"></param>
        private void PositionPrint(int mode=1)
        {
            /*
                mode=1|2
                    1 -- основные позиции (раскрой)
                    2 -- дополнительные позиции (докрой)
             */

            var p=ToolbarGetParams();

            if(mode==1){

                if (PositionGrid.Items.Count>0)
                {
                    var reporter = new PositionReporter
                    {
                        Items = PositionGrid.Items,
                        FromDate = p.CheckGet("FROMDATE"),
                        ToDate = p.CheckGet("TODATE"),
                        Title = "Автораскрой"

                    };
                    reporter.MakeReportCuttingList();
                }
            }

            if(mode==2){
                
                if (Position2Grid.Items.Count>0)
                {
                    var toDate = p.CheckGet("TODATE").ToDateTime();
                    toDate=toDate.AddDays(p.CheckGet("ADDDAYS").ToInt());
                    var toDateString=toDate.ToString("dd.MM.yyyy");

                    var reporter = new PositionReporter
                    {
                        Items = Position2Grid.Items,
                        FromDate = p.CheckGet("FROMDATE"),
                        ToDate = toDateString,
                        Title = "Автораскрой (докрой)"

                    };
                    reporter.MakeReportCuttingList();
                }
                
                /*
                if(Position2GridDS.Initialized)
                {
                    if(Position2GridDS.Items.Count>0)
                    {
                        var toDate = p.CheckGet("TODATE").ToDateTime();
                        toDate=toDate.AddDays(p.CheckGet("ADDDAYS").ToInt());
                        var toDateString=toDate.ToString("dd.MM.yyyy");

                        var reporter = new PositionReporter
                        {
                            Items = Position2GridDS.Items,
                            FromDate = p.CheckGet("FROMDATE"),
                            ToDate = toDateString,
                            Title = "Автораскрой (докрой)"

                        };
                        reporter.MakeReportCuttingList();
                    }
                }
                */
            }
        }

        private void VirtualSourceEdit()
        {
            var item=PaperGrid.SelectedItem;
            if(item.Count > 0)
            {
                var editForm = new ProductionTaskVirtualPaperEdit();
                editForm.Id = item.CheckGet("ID").ToInt();
                editForm.Weight = item.CheckGet("WEIGHT_VIRTUAL").ToDouble();
                editForm.Edit();
            }
        }

        /// <summary>
        /// экспорт списка заданий в Excel
        /// </summary>
        private async void TaskExport()
        {
            if(TaskGrid!=null)
            {
                if(TaskGrid.Items.Count>0)
                {
                    var eg = new ExcelGrid();
                    var cols=TaskGrid.Columns;
                    eg.SetColumnsFromGrid(cols);
                    eg.Items = TaskGrid.Items;
                    await Task.Run(() =>
                    {
                        eg.Make();
                    });
                }
            }
        }

        /// <summary>
        /// Экспорт таблицы наличия сырья
        /// </summary>
        private async void PaperGridExport()
        {
            if (PaperGrid != null)
            {
                if (PaperGrid.Items.Count > 0)
                {
                    var eg = new ExcelGrid();
                    var cols = PaperGrid.Columns;
                    // Убираем последнюю колонку
                    int cnt = cols.Count;
                    cols.RemoveAt(cnt - 1);
                    eg.SetColumnsFromGrid(cols);
                    eg.Items = PaperGrid.Items;
                    await Task.Run(() =>
                    {
                        eg.Make();
                    });
                }
            }
        }

        /// <summary>
        /// Экспорт таблицы итогов раскроя
        /// </summary>
        private async void TotalExport()
        {
            if (TotalGrid != null)
            {
                if (TotalGrid.Items.Count > 0)
                {
                    var eg = new ExcelGrid();
                    var cols = TotalGrid.Columns;
                    eg.SetColumnsFromGrid(cols);
                    eg.Items = TotalGrid.Items;
                    await Task.Run(() =>
                    {
                        eg.Make();
                    });
                }
            }
        }

        /// <summary>
        /// Открывает окно ручного раскроя
        /// </summary>
        private void CuttingManual()
        {
            if (PositionGridSelectedItem != null)
            {
                ManualCuttingPosisionSelected.Clear();
                ManualCuttingPosisionSelected.AddRange(PositionGridSelectedItem);
                
                var productionTaskForm = new ProductionTask();
                productionTaskForm.BackTabName = "CreatingTasks_cuttingAuto";
                productionTaskForm.FactoryId = Factory.SelectedItem.Key.ToInt();
                productionTaskForm.Create();
            }
        }
        /// <summary>
        /// Подсчитывает суммарные длины заданий по форматам картона и выводит в окно
        /// </summary>
        private void SummaryLengthTask()
        {
            var totals = new Dictionary<string, Dictionary<string, int>>();
            List<string> formats = new List<string>();
            foreach (var item in Formats)
            {
                formats.Add(item.Key);
            }
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "_ROWNUMBER", "#" },
                { "FORMAT", "Формат" }
            };

            foreach (var row in TaskGrid.Items)
            {
                var f = row.CheckGet("FORMAT");
                if (!totals.ContainsKey(f))
                {
                    totals.Add(f, new Dictionary<string, int>());
                }

                var q = row.CheckGet("IDC");
                var v = row.CheckGet("LENGTH").ToInt();
                if (!totals[f].ContainsKey(q))
                {
                    totals[f].Add(q, v);
                    headers.CheckAdd(row.CheckGet("IDC"), row.CheckGet("CARDBOARD_NAME"));
                }
                else
                {
                    totals[f][q] += v;
                }
            }

            // Делаем ListDataSet. Строки - форматы, колонки - QID
            var list = new List<Dictionary<string, string>>();
            int j = 1;
            foreach (var format in formats)
            {
                var d = new Dictionary<string, string>()
                {
                    { "_ROWNUMBER", j.ToString() },
                    { "FORMAT", format }
                };

                foreach (var q in headers.Keys)
                {
                    if (q != "_ROWNUMBER" && q != "FORMAT")
                    {
                        d.Add(q, "");
                        if (totals.ContainsKey(format))
                        {
                            if (totals[format].ContainsKey(q))
                            {
                                d[q] = totals[format][q].ToString();
                            }
                        }
                    }
                }
                list.Add(d);
                j++;
            }

            var totalsDS = new ListDataSet();
            totalsDS.Init();
            totalsDS.Items = list;

            var totalWindow = new ProductionTaskTotalFormatLength();
            totalWindow.InitGrid(totalsDS, headers);
        }

        /// <summary>
        /// Настройка припуска выбранного изделия
        /// </summary>
        private void EditLosses()
        {
            int productId = PositionGrid.SelectedItem.CheckGet("GOODS_ID").ToInt();
            var productLossesFrame = new ProductLosses();
            productLossesFrame.ReceiverName = "CreatingTasks_cuttingAuto";
            productLossesFrame.ProductId = productId;
            productLossesFrame.QtyInApplication = PositionGrid.SelectedItem.CheckGet("PRODUCTS_IN_APPLICATION").ToInt();
            productLossesFrame.ProductSku = PositionGrid.SelectedItem.CheckGet("GOODS_CODE").Substring(0, 7);
            productLossesFrame.ProductsFromBlankQty = PositionGrid.SelectedItem.CheckGet("TLSQTY").ToDouble();
            productLossesFrame.ProductsOnPalletQty = PositionGrid.SelectedItem.CheckGet("PRODUCTS_IN_PALLET").ToInt();
            productLossesFrame.ShowTab();

        }

        private void HelpButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void ShowButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            DebugLog.Text = "";
            PositionGrid.LoadItems();
        }

        private void Filter1_Click(object sender,RoutedEventArgs e)
        {
            //Filter1.IsChecked = false; 
            //Filter2.IsChecked = false;
            //Filter3.IsChecked = false;
            //Filter4.IsChecked = false;
            
            PositionGrid.UpdateItems();
        }

        private void Filter2_Click(object sender,RoutedEventArgs e)
        {
            //Filter1.IsChecked = false; 
            //Filter2.IsChecked = false;
            //Filter3.IsChecked = false;
            //Filter4.IsChecked = false;
            
            PositionGrid.UpdateItems();
        }

        private void Filter3_Click(object sender,RoutedEventArgs e)
        {
            //Filter1.IsChecked = false; 
            //Filter2.IsChecked = false;
            //Filter3.IsChecked = false;
            //Filter4.IsChecked = false;
            
            PositionGrid.UpdateItems();
        }

        private void Filter4_Click(object sender,RoutedEventArgs e)
        {
            //Filter1.IsChecked = false; 
            //Filter2.IsChecked = false;
            //Filter3.IsChecked = false;
            //Filter4.IsChecked = false;
            
            PositionGrid.UpdateItems();
        }

        private void Filter5_Click(object sender,RoutedEventArgs e)
        {
            PositionGrid.UpdateItems();
        }

        private void ProfileTypes_SelectedItemChanged(DependencyObject d,DependencyPropertyChangedEventArgs e)
        {
            var profileId = ProfileTypes.SelectedItem.Key.ToInt();

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

                Cardboard2.Items.Clear();
                Cardboard2.Items.AddRange<string, string>(list);
                Cardboard2.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
            }

            PositionGrid.LoadItems();
            //Position2Grid.UpdateItems();
        }

        private void MakeCuttingButton_Click(object sender,RoutedEventArgs e)
        {
            DebugLog.Text = "";
            MakeCutting();
        }

        private void PositionSelectAll_Click(object sender,RoutedEventArgs e)
        {
            DoPositionSelectAll();
        }

        private void Position2SelectAll_Click(object sender,RoutedEventArgs e)
        {
            DoPosition2SelectAll();
        }

        private void RefreshButton_Click(object sender,RoutedEventArgs e)
        {
            TaskGrid.LoadItems();
        }

        private void DeleteButton_Click(object sender,RoutedEventArgs e)
        {
            TaskDelete();
        }

        private void SaveTasksButton_Click(object sender,RoutedEventArgs e)
        {
            TaskSave();
        }

        private void PositionPrintButton_Click(object sender,RoutedEventArgs e)
        {
            PositionPrint(1);
        }

        private void Position2PrintButton_Click(object sender,RoutedEventArgs e)
        {
            PositionPrint(2);
        }

        private void TaskExportButton_Click(object sender,RoutedEventArgs e)
        {
            TaskExport();
        }

        private void PaperGridRefreshButton_Click(object sender,RoutedEventArgs e)
        {
            PaperGrid.LoadItems();
        }

        private void AvailableOnly_Click(object sender,RoutedEventArgs e)
        {
            PaperGrid.UpdateItems();
        }

        private void Type_OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PositionGrid.UpdateItems();
        }

        private void ToggleSplash31Button_Click(object sender,RoutedEventArgs e)
        {
        }

        private void ToggleSplash30Button_Click(object sender,RoutedEventArgs e)
        {
        }

        private void ApplicationFilterFirst_Click(object sender,RoutedEventArgs e)
        {
            PositionGrid.UpdateItems();
        }

        private void LoadWaste_Click(object sender, RoutedEventArgs e)
        {
            var serviceWaste = new ServiceWaste();
            serviceWaste.Show();
        }

        private void Cardboard_SelectedItemChanged(System.Windows.DependencyObject d, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            PositionGrid.UpdateItems();

            //для подкроя выставляем тот же картон
            int cardboardId = Cardboard.SelectedItem.Key.ToInt();
            if (cardboardId > 0)
            {
                foreach (var item in Cardboard2.Items)
                {
                    if (item.Key.ToInt() == cardboardId)
                    {
                        Cardboard2.SelectedItem = item;
                    }
                }
                Position2Grid.UpdateItems();
            }
        }

        private void PaperFormat_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PaperGrid.UpdateItems();
        }

        private void FormatLengthButton_Click(object sender, RoutedEventArgs e)
        {
            SummaryLengthTask();
        }

        private void VirtualSourceButton_Click(object sender, RoutedEventArgs e)
        {
            VirtualSourceEdit();
        }

        private void TotalGridRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            TotalLoadItems();
        }

        private void ProfileTotals_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TotalGrid.UpdateItems();
        }

        private void Cardboard2_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Position2Grid.UpdateItems();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = new ProductionTaskSettings();
            settings.Edit();
        }

        private void TotalToExcel_Click(object sender, RoutedEventArgs e)
        {
            TotalExport();
        }

        private void SourceToExcel_Click(object sender, RoutedEventArgs e)
        {
            PaperGridExport();
        }

        private void FanfoldFilter_Click(object sender, RoutedEventArgs e)
        {
            PositionGrid.UpdateItems();
        }

        private void PaperDensity_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PaperGrid.UpdateItems();
        }

        private void CorrugatorSet_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PositionGrid.UpdateItems();
            Position2Grid.UpdateItems();
        }

        private void Factory_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PositionGrid.LoadItems();
        }
    }
}


