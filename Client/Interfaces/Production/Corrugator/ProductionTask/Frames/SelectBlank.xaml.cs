using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using SixLabors.ImageSharp.Metadata.Profiles.Icc;
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
    /// выбор заготовки для стекера
    /// интерфейс ручного раскроя
    /// </summary>
    /// <author>balchugov_dv</author>    
    public partial class SelectBlank:UserControl
    {
        public SelectBlank()
        {
            InitializeComponent();

            SelectedItemId=0;
            BackTabName="manual_cutting";
            GoodsRefreshButtonBlue=false;
            ApplicationRefreshButtonBlue=false;
            ApplicationFormSessionKey="Client.Interfaces.Production.SelectBlank.ApplicationForm";
            ApplicationFormSessionEnabled=false;
            RefSourcesLoaded=false;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this,ProcessMessages);

            //регистрация обработчика клавиатуры
            //PreviewKeyDown+=ProcessKeyboard;

            InitApplicationForm();
            InitGoodsForm();
            LoadRef();            
        }

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string,string> SelectedItem { get; set; }

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        public int SelectedItemId { get; set; }
        /// <summary>
        /// id стекера, которому предназначается заготовка
        /// он вернется назад при положительном исходе
        /// </summary>
        public int StackerId { get; set; }
        /// <summary>
        /// Идентификатор производственной площадки, на которой выполняется ПЗГА
        /// </summary>
        public int FactoryId;

        public ListDataSet ApplicationCardboardDS { get; set; }
        public ListDataSet ApplicationProfileDS { get; set; }

        public ListDataSet GoodsCardboardDS { get; set; }
        public ListDataSet GoodsProfileDS { get; set; }

        public FormHelper ApplicationForm { get; set; }
        public FormHelper GoodsForm { get; set; }

        /// <summary>
        /// ключ для хранения данных формы фильтра в сессии
        /// </summary>
        private string ApplicationFormSessionKey { get;set;}
        /// <summary>
        /// флаг активности механизма работы с сессией
        /// </summary>
        private bool ApplicationFormSessionEnabled { get;set;}
        /// <summary>
        /// флаг готовности справочников
        /// </summary>
        private bool RefSourcesLoaded { get;set;}

        /// <summary>
        /// флаг акцента внимания на кнопку Показать
        /// при изменениях в форме поднимается, по клику на кнопке, опускается
        /// при поднятом флаге кнопка окрашивается в синий цвет
        /// </summary>
        public bool GoodsRefreshButtonBlue { get; set; }

        /// <summary>
        /// флаг акцента внимания на кнопку Показать
        /// при изменениях в форме поднимается, по клику на кнопке, опускается
        /// при поднятом флаге кнопка окрашивается в синий цвет
        /// </summary>
        public bool ApplicationRefreshButtonBlue { get; set; }

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
        public void ProcessKeyboard2()
        {
            var e=Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;
                
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// отображение статьи в справочной системе
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/production/creating_tasks/cutting_manual/select_blank");
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
                SenderName = "SelectBlank",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            ApplicationGrid.Destruct();

            //возврат к предыдущему интерфейсу (если есть цепь навигации)
            GoBack();
        }

        /// <summary>
        /// Инициализация формы "фильтр грида" заявки
        /// </summary>
        public void InitApplicationForm()
        {
            ApplicationForm=new FormHelper();
            //список колонок формы
            var fields=new List<FormHelperField>()
            {              
                new FormHelperField()
                {
                    Path="FROM_DATE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=FromDate,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TO_DATE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ToDate,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SEARCH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ApplicationSearchText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOT_CUTTED",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ApplicationFilter1,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="Z_CARDBOARD",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ApplicationFilterZ,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="FIRST_TIME_PRODUCTION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ApplicationFilterFirst,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                { 
                    Path="PROFILE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ApplicationProfile,
                    Default="0",
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                    },
                    
                },   
                new FormHelperField()
                { 
                    Path="CARDBOARD",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Default="0",
                    Control=ApplicationCardboard,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                    },
                },   
            };

            ApplicationForm.SetFields(fields);
            ApplicationForm.OnValidate=(bool valid, string message) =>
            {
                if(valid)
                {
                    //SaveButton.IsEnabled=true;
                    FormStatus.Text="";
                }
                else
                {
                    //SaveButton.IsEnabled=false;
                    FormStatus.Text="Не все поля заполнены верно";
                }
            };


            { 
                ApplicationProfile.OnSelectItem=(Dictionary<string,string> selectedItem) => 
                {
                    bool result = true;
                    if (selectedItem.Count > 0)
                    {
                        int id=selectedItem.CheckGet("ID").ToInt();
                        {
                            //отфильтруем только те картоны, которые относятся к выбранному профилю
                            if(ApplicationCardboardDS.Items.Count>0)
                            {
                                var list = new Dictionary<string,string>();
                                list.Add("0","");
                                foreach(Dictionary<string,string> row in ApplicationCardboardDS.Items)
                                {
                                    var include=false;
                                    if(row.CheckGet("PROFILE_ID").ToInt()==id)
                                    {
                                        include=true;
                                    }

                                    if(id==0)
                                    {
                                        include=true;
                                    }

                                    if(include)                                        
                                    {
                                        list.Add(row.CheckGet("ID"),row.CheckGet("NAME"));
                                    }
                                }
                                ApplicationCardboard.Items = list;
                                ApplicationCardboard.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
                            }

                        }
                        ApplicationGrid.LoadItems();
                    }
                    return result;
                };
            }

            { 
                ApplicationCardboard.OnSelectItem=(Dictionary<string,string> selectedItem) => 
                {
                    bool result = true;
                    if (selectedItem.Count > 0)
                    {
                        int id=selectedItem.CheckGet("ID").ToInt();
                        ApplicationGrid.UpdateItems();
                        
                    }
                    Central.Dbg("ApplicationCardboard changed");
                    return result;
                };
            }

        }

        /// <summary>
        /// Инициализация формы "фильтр грида" изделия
        /// </summary>
        public void InitGoodsForm()
        {
            GoodsForm=new FormHelper();
            //список колонок формы
            var fields=new List<FormHelperField>()
            {              
                new FormHelperField()
                {
                    Path="SEARCH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=GoodsSearchText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                { 
                    Path="PROFILE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=GoodsProfile,
                    Default="0",
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                    },
                    
                },   
                new FormHelperField()
                { 
                    Path="CARDBOARD",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Default="0",
                    Control=GoodsCardboard,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                    },
                },   
            };

            GoodsForm.SetFields(fields);
            GoodsForm.OnValidate=(bool valid, string message) =>
            {
                if(valid)
                {
                    //SaveButton.IsEnabled=true;
                    FormStatus.Text="";
                }
                else
                {
                    //SaveButton.IsEnabled=false;
                    FormStatus.Text="Не все поля заполнены верно";
                }
            };


            { 
                GoodsProfile.OnSelectItem=(Dictionary<string,string> selectedItem) => 
                {
                    bool result = true;
                    if (selectedItem.Count > 0)
                    {
                        int id=selectedItem.CheckGet("ID").ToInt();
                        {
                            //отфильтруем только те картоны, которые относятся к выбранному профилю
                            if(GoodsCardboardDS.Items.Count>0)
                            {
                                var list = new Dictionary<string,string>();
                                list.Add("0","");
                                foreach(Dictionary<string,string> row in GoodsCardboardDS.Items)
                                {
                                    var include=false;
                                    if(row.CheckGet("PROFILE_ID").ToInt()==id)
                                    {
                                        include=true;
                                    }

                                    if(id==0)
                                    {
                                        include=true;
                                    }

                                    if(include)                                        
                                    {
                                        list.Add(row.CheckGet("ID"),row.CheckGet("NAME"));
                                    }
                                }
                                GoodsCardboard.Items = list;
                                GoodsCardboard.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
                            }

                        }
                        GoodsGrid.UpdateItems();
                    }
                    return result;
                };
            }

            { 
                GoodsCardboard.OnSelectItem=(Dictionary<string,string> selectedItem) => 
                {
                    bool result = true;
                    if (selectedItem.Count > 0)
                    {
                        int id=selectedItem.CheckGet("ID").ToInt();
                        GoodsGrid.UpdateItems();                        
                    }
                    return result;
                };
            }

        }

        public void InitData()
        {
            SetDefaults();
            ApplicationGridInit();
            GoodsGridInit();
        }

        public void SetDefaults(bool resetSession=false)
        {
            {
                ApplicationForm.SetDefaults();
                {
                    var v=new Dictionary<string,string>()
                    { 
                        {"FROM_DATE",DateTime.Now.AddDays(0).ToString("dd.MM.yyyy")},
                        {"TO_DATE",DateTime.Now.AddDays(9).ToString("dd.MM.yyyy")},
                        {"SEARCH",""},
                        {"NOT_CUTTED","1"},
                        {"Z_CARDBOARD","0"},
                        {"PROFILE","0"},
                        {"CARDBOARD","0" },
                    };

                    //если в сессии есть сохраненные значения, загрузим их                    
                    {
                        if(!string.IsNullOrEmpty(ApplicationFormSessionKey))
                        {
                            if(Central.SessionValues.ContainsKey(ApplicationFormSessionKey))
                            {
                                if(resetSession)
                                {
                                    Central.SessionValues[ApplicationFormSessionKey]=new Dictionary<string, string>();
                                }
                                else
                                {
                                    v=Central.SessionValues[ApplicationFormSessionKey];
                                }                               
                            }
                        }
                    }                    
                     
                    ApplicationForm.SetValues(v);             
                    
                    //защита от перезаписи: разрешаем перезапись, только если мы все уже считали, установили изначально
                    ApplicationFormSessionEnabled=true;
                }
            }
            
            {
                GoodsForm.SetDefaults();
                { 
                    var v=new Dictionary<string,string>()
                    { 
                        {"SEARCH",""},
                        {"PROFILE","0"},
                        {"CARDBOARD","0" },
                    };   
                    GoodsForm.SetValues(v);
                }
            }

            // Фильтр недокроенных позиций
            var uncuttedPartList = new Dictionary<string, string>()
            {
                { "0", "0%" },
                { "5", "5%" },
                { "10", "10%" },
                { "15", "15%" },
                { "20", "20%" },
            };
            HideUncutted.Items = uncuttedPartList;
            HideUncutted.SetSelectedItemByKey("0");

            GoodsCheckForm();
            ApplicationRefreshButtonCheck(true);
        }

        public void ApplicationGridInit()
        {
            //список колонок грида
            var columns = new List<DataGridHelperColumn>()
            {
                new DataGridHelperColumn()
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    Doc="Номер строки по порядку",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=50,
                },
                
                new DataGridHelperColumn
                {
                    Header="Картон",
                    Path="CARDBOARDNAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=100,                        
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
                new DataGridHelperColumn()
                {
                    Header="Первое производство",
                    Path="FIRST_TIME_PRODUCTION",
                    Doc="Первое производство (данное изделие ни разу не отгружалось)",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=45,
                    MaxWidth=45,
                },

                new DataGridHelperColumn()
                {
                    Header="Изделие",
                    Path="GOODSNAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=120,
                },
                new DataGridHelperColumn()
                {
                    Header="Артикул изделия",
                    Path="GOODSCODE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=80,
                },
                new DataGridHelperColumn()
                {
                    Header="Заготовка",
                    Path="BLANKNAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=120,
                },
                new DataGridHelperColumn()
                {
                    Header="Артикул заготовки",
                    Path="BLANKCODE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=80,
                },

                new DataGridHelperColumn()
                {
                    Header="Рилевка",
                    Path="CREASETYPE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="Рилевка",
                    Path="CREASE_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=35,
                    MaxWidth=35,
                },

                new DataGridHelperColumn()
                {
                    Header="Оснастка не готова",
                    Path="_RIG_IS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    FormatterRaw=(row) =>
                    {
                        var result=false;

                        /*
                            статус шнанцформы
                            0 = не заказана, 
                            1 = заказана, 
                            2 = получена, 
                            ...

                            статус клише:
                            0 = В работе, 
                            1 = В архиве, 
                            2 = Готово к передаче, 
                            ...

                            RIG_IS = Math.Min( stampingFormStatusId, clicheStatusId );                            
                         */
                        if( row.CheckGet("RIG_IS").ToInt() < 2 )
                        {
                            result = true;
                        }
                        return result;
                    },
                },
                
                new DataGridHelperColumn()
                {
                    Header="Z-картон",
                    Path="Z_CARDBOARD",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                },
                
                new DataGridHelperColumn
                {
                    Header="Длина",
                    Path="LENGTH",
                    Doc="Длина заготовки, м.",
                    ColumnType=ColumnTypeRef.Integer,
                    Width=40,
                },
                new DataGridHelperColumn
                {
                    Header="Ширина",
                    Path="WIDTH",
                    Doc="Ширина заготовки, м.",
                    ColumnType=ColumnTypeRef.Integer,
                    Width=40,
                },

                new DataGridHelperColumn
                {
                    Header="=",
                    Path="QTY_LIMIT",
                    Doc="Допуск по количеству заготовок",
                    ColumnType=ColumnTypeRef.String,
                    Width=40,
                },

                new DataGridHelperColumn
                {
                    Header="В заявке",
                    Group="Изделий",
                    Path="ODQTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width=60,
                },
                new DataGridHelperColumn
                {
                    Header="Склад",
                    Group="Изделий",
                    Path="CURQTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width=50,
                },
                new DataGridHelperColumn
                {
                    Header="Склад ПЗ",
                    Group="Изделий",
                    Path="CURPZQTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width=50,
                },
                new DataGridHelperColumn
                {
                    Header="Отгрузка",
                    Group="Изделий",
                    Path="RQTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width=50,
                },
                new DataGridHelperColumn
                {
                    Header="Изделий из заготовки",
                    Group="Изделий",
                    Path="TLSQTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width=50,
                },


                new DataGridHelperColumn
                {
                    Header="Всего в ПЗ",
                    Group="Заготовок",
                    Path="PZTOTALQTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width=50,
                },
                new DataGridHelperColumn
                {
                    Header="Не сделано",
                    Group="Заготовок",
                    Path="PZQTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width=50,
                },
                new DataGridHelperColumn
                {
                    Header="В наличии",
                    Group="Заготовок",
                    Path="PZZAGQTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width=50,
                },
                new DataGridHelperColumn
                {
                    Header="Для раскроя",
                    Group="Заготовок",
                    Path="QTY",
                    Doc="Требуемое количество заготовок для раскроя, шт.",
                    ColumnType=ColumnTypeRef.Integer,
                    Width=50,
                },
                new DataGridHelperColumn
                {
                    Header="Припуск",
                    Group="Заготовок",
                    Path="QTY_ADD",
                    Doc="Увеличение количества заготовок для раскроя с учетом брака, шт",
                    ColumnType=ColumnTypeRef.String,
                    Width=50,
                    Style="DataGridColumnDigit",
                    FormatterRaw=(v) =>
                    {
                        var result="";
                        if(v.CheckGet("QTY").ToInt()>0 && v.CheckGet("QTY_ADD").ToInt()>0)
                        {
                            result=$"{v.CheckGet("QTY_ADD").ToInt().ToString()}";
                        }
                        return result;
                    },
                },

                new DataGridHelperColumn
                {
                    Header="Станок",
                    Path="MACHINE_ID",
                    Options="zeroempty",
                    ColumnType=ColumnTypeRef.Integer,
                },

                new DataGridHelperColumn
                {
                    Header="Изд/под",
                    Path="PRODUCTS_IN_PALLET",
                    Doc="Изделий на поддоне, шт.",
                    ColumnType=ColumnTypeRef.Integer,
                    Width=50,
                },
                new DataGridHelperColumn()
                {
                    Header="ИД позиции",
                    Path="IDORDERDATES",
                    Doc="ИД позиции заявки",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=50,
                },
                new DataGridHelperColumn()
                {
                    Header="Конфигурация рилевок",
                    Path="CREASE_LIST",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },

            };
            ApplicationGrid.SetColumns(columns);
            ApplicationGrid.RowStylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                {
                    {
                        StylerTypeRef.BackgroundColor,
                        (Dictionary<string, string> row) =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            // изделие со сложной схемой (больше 1 передела)
                            if (row.CheckGet("PRODUCTION_SCHEME_STEPS").ToInt() > 2)
                            {
                                color = HColor.Yellow;
                            }

                            // оснастка не готова
                            if( row.CheckGet("RIG_IS").ToInt() < 2 )
                            {
                                color = HColor.Orange;
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };


            ApplicationGrid.SetSorting("SHIPMENTDATETIME",ListSortDirection.Ascending);
            ApplicationGrid.SearchText=ApplicationSearchText;
            ApplicationGrid.Init();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            ApplicationGrid.OnSelectItem=(Dictionary<string,string> selectedItem) =>
            {
                if(selectedItem.Count > 0)
                {
                    ApplicationGridUpdateActions(selectedItem);
                }
            };

            //двойной клик на строке осуществит выбор данной позиции (заявки/изделия)
            ApplicationGrid.OnDblClick=(Dictionary<string,string> selectedItem) =>
            {
                Save();
            };

            //данные грида
            ApplicationGrid.OnLoadItems=ApplicationGridLoadItems;
            ApplicationGrid.OnFilterItems=ApplicationGridFilterItems;
            ApplicationGrid.AutoUpdateInterval=0;
            //ApplicationGrid.Run();

            //фокус ввода           
            ApplicationGrid.Focus();

            
        }

        /// <summary>
        /// обновление записей
        /// </summary>
        public async void ApplicationGridLoadItems()
        {
            ApplicationGridToolbar.IsEnabled=false;
            ApplicationGrid.ShowSplash();

            ApplicationRefreshButtonCheck(false);

            bool resume=true;

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
                int profileId = ApplicationProfile.SelectedItem.Key.ToInt();
                if (profileId == 0)
                {
                    var msg = "Выберите профиль";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();
                    resume = false;
                }
            }

            if (resume)
            {                
                var p = new Dictionary<string,string>();
                {
                    p.Add("FROMDATE",   FromDate.Text);
                    p.Add("TODATE",     ToDate.Text);
                    p.Add("USE_CACHING",     "1");
                    p.Add("FACTORY_ID", FactoryId.ToString());
                    p.Add("PROFILE_ID", ApplicationProfile.SelectedItem.Key);
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module","Production");
                q.Request.SetParam("Object","Position");
                q.Request.SetParam("Action","ListUncutted");
                q.Request.Timeout = 150000; //Central.Parameters.RequestTimeoutMax;
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
                        if(result.ContainsKey("POSITIONS"))
                        {
                            var ds = ListDataSet.Create(result, "POSITIONS");
                            var processedDS = ProcessItems(ds);
                            ApplicationGrid.UpdateItems(processedDS);
                        }
                    }
                }
            }

            ApplicationGridToolbar.IsEnabled=true;
            ApplicationGrid.HideSplash(); 
        }

        /// <summary>
        /// Обработка строк перед отображением в таблице
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        private ListDataSet ProcessItems(ListDataSet ds)
        {
            var _ds = ds;
            if (ds.Items != null)
            {
                if (ds.Items.Count > 0)
                {
                    foreach (var item in _ds.Items)
                    {
                        

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
                    }
                }
            }

            return _ds;
        }

        /// <summary>
        /// фильтрация записей
        /// </summary>
        public async void ApplicationGridFilterItems()
        {
            if(ApplicationGrid.GridItems!=null)
            {
                if(ApplicationGrid.GridItems.Count>0)
                {
                    ApplicationGrid.ShowSplash();
                    ApplicationGridToolbar.IsEnabled = false;
                    Mouse.OverrideCursor = Cursors.Wait;

                    var doFilteringByProfile=false;
                    var doFilteringByCardboard=false;
                    var doFilteringByComplete=false;
                    var doFilteringByZ=false;
                    var doFilteringByFirst=false;
                    var doFilteringByUncutted = false;
                    
                    var v=ApplicationForm.GetValues();

                    /*
                        по профилю
                        0 Все                                                
                     */                    
                    int profileId=v.CheckGet("PROFILE").ToInt();
                    if(profileId>0)
                    {
                        doFilteringByProfile=true;
                    }

                    /*
                        по марке картона    
                        0 Все                                                
                     */                    
                    int cardboardId=v.CheckGet("CARDBOARD").ToInt();
                    if(cardboardId>0)
                    {
                        doFilteringByCardboard=true;
                        //если выбрана конкретная марка картона, фильтрация по профилю уже не имеет смысла
                        doFilteringByProfile=false;
                    }
                    
                    //по признаку "нераскроено"
                    if((bool)ApplicationFilter1.IsChecked)
                    {
                        doFilteringByComplete=true;
                    }

                    //по признаку "z-картон"
                    if((bool)ApplicationFilterZ.IsChecked)
                    {
                        doFilteringByZ=true;
                    }

                    //по признаку "первое производство"
                    if((bool)ApplicationFilterFirst.IsChecked)
                    {
                        doFilteringByFirst=true;
                    }

                    //по доле недораскроенных позиций
                    if (HideUncutted.SelectedItem.Key.ToInt() > 0)
                    {
                        doFilteringByUncutted = true;
                    }

                    var items = new List<Dictionary<string,string>>();
                    foreach(Dictionary<string,string> row in ApplicationGrid.GridItems)
                    {
                        bool includeByProfile = true;
                        bool includeByCardboard = true;
                        bool includeByComplete = true;
                        bool includeByZ = true;
                        bool includeByFirst = true;
                        bool includeByUncutted = true;
                        bool includeByForbidden = true;

                        //Убираем позиции с запретом раскроя
                        if (row.CheckGet("TYPE_CUT").ToInt() == 2)
                        {
                            includeByForbidden = false;
                        }

                        if(doFilteringByProfile)
                        {
                            includeByProfile = false;
                            if (row.CheckGet("PROFILEID").ToInt()==profileId)
                            {
                                includeByProfile = true;
                            }
                        }

                        if(doFilteringByCardboard)
                        {
                            includeByCardboard = false;
                            if (row.CheckGet("CARDBOARD_ID").ToInt()==cardboardId)
                            {
                                includeByCardboard = true;
                            }
                        }

                        if(doFilteringByComplete)
                        {
                            includeByComplete = false;
                            if (row.CheckGet("QTY").ToInt()>0)
                            {
                                includeByComplete = true;
                            }
                        }

                        if(doFilteringByZ)
                        {
                            includeByZ = false;
                            if (row.CheckGet("Z_CARDBOARD").ToBool())
                            {
                                includeByZ = true;
                            }
                        }

                        if(doFilteringByFirst)
                        {
                            includeByFirst=false;
                            if (row.CheckGet("FIRST_TIME_PRODUCTION").ToBool())
                            {
                                includeByFirst = true;
                            }
                        }

                        if (doFilteringByUncutted)
                        {
                            includeByUncutted = false;
                            double ratio = HideUncutted.SelectedItem.Key.ToDouble() / 100;
                            int orderQty = row.CheckGet("ODQTY").ToInt();
                            int qty = row.CheckGet("QTY").ToInt();
                            if (qty > (int)(orderQty * ratio))
                            {
                                includeByUncutted = true;
                            }
                        }
                            

                        if(
                            includeByProfile
                            && includeByCardboard
                            && includeByComplete
                            && includeByZ
                            && includeByFirst
                            && includeByUncutted
                            && includeByForbidden
                        )
                        {
                            items.Add(row);
                        }
                    }
                    ApplicationGrid.GridItems=items;                        
                        

                    ApplicationGrid.HideSplash();
                    ApplicationGridToolbar.IsEnabled = true;
                    Mouse.OverrideCursor = null;
                }
            }

            //сохраним данные формы в сессии
            {
                if(ApplicationFormSessionEnabled)
                {
                    if(!string.IsNullOrEmpty(ApplicationFormSessionKey))
                    {
                        if(!Central.SessionValues.ContainsKey(ApplicationFormSessionKey))
                        {
                            Central.SessionValues.Add(ApplicationFormSessionKey,new Dictionary<string, string>());
                        }
                        var v=ApplicationForm.GetValues();
                        Central.SessionValues[ApplicationFormSessionKey]=v;
                    }
                }
            }
        }

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem"></param>
        public void ApplicationGridUpdateActions(Dictionary<string,string> selectedItem)
        {
            SelectedItem = selectedItem;

            SaveButton.IsEnabled=false;

            if(SelectedItem.Count>0)
            {
                SaveButton.IsEnabled=true;
            }
        }

        public void GoodsGridInit()
        {
            //список колонок грида
            var columns = new List<DataGridHelperColumn>()
            {
                new DataGridHelperColumn()
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    Doc="Номер строки по порядку",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=50,
                },
                new DataGridHelperColumn()
                {
                    Header="ИД",
                    Path="GOODS_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                },
                new DataGridHelperColumn()
                {
                    Header="Изделие",
                    Path="GOODS_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=100,
                },
                new DataGridHelperColumn()
                {
                    Header="Артикул изделия",
                    Path="GOODS_CODE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=100,
                },
                new DataGridHelperColumn()
                {
                    Header="Заготовка",
                    Path="BLANK_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=100,
                },
                new DataGridHelperColumn()
                {
                    Header="Артикул заготовки",
                    Path="BLANK_CODE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Длина",
                    Path="LENGTH",
                    Doc="Длина заготовки, м.",
                    ColumnType=ColumnTypeRef.Integer,
                    Width=40,
                },
                new DataGridHelperColumn
                {
                    Header="Ширина",
                    Path="WIDTH",
                    Doc="Ширина заготовки, м.",
                    ColumnType=ColumnTypeRef.Integer,
                    Width=40,
                },
                new DataGridHelperColumn
                {
                    Header="Тип изделия",
                    Path="PRODUCT_TYPE",
                    ColumnType=ColumnTypeRef.String,
                    Width=50,                        
                },
                new DataGridHelperColumn
                {
                    Header="Картон",
                    Path="CARDBOARD_NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=100,                        
                },
                
                new DataGridHelperColumn
                {
                    Header="Z-картон",
                    Path="Z_CARDBOARD",
                    ColumnType=ColumnTypeRef.Boolean,
                },
                
                
                
            };
            GoodsGrid.SetColumns(columns);


            GoodsGrid.SetSorting("_ROWNUMBER",ListSortDirection.Ascending);
            GoodsGrid.Init();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            GoodsGrid.OnSelectItem=(Dictionary<string,string> selectedItem) =>
            {
                if(selectedItem.Count > 0)
                {
                    GoodsGridUpdateActions(selectedItem);
                }
            };

            //двойной клик на строке осуществит выбор данной позиции (заявки/изделия)
            GoodsGrid.OnDblClick=(Dictionary<string,string> selectedItem) =>
            {
                Save();
            };

            //данные грида
            GoodsGrid.OnLoadItems=GoodsGridLoadItems;
            GoodsGrid.OnFilterItems=GoodsGridFilterItems;
            GoodsGrid.AutoUpdateInterval=0;
            //GoodsGrid.Run();

            //фокус ввода           
            GoodsGrid.Focus();
        }

        /// <summary>
        /// обновление записей
        /// </summary>
        public async void GoodsGridLoadItems()
        {
            GoodsGridToolbar.IsEnabled=false;
            GoodsGrid.ShowSplash();

            GoodsRefreshButtonCheck(false);

            bool resume=true;

            if (resume)
            {
                var t=GoodsSearchText.Text;
                if(string.IsNullOrEmpty(t))
                {
                    resume=false;
                }
            }

            if (resume)
            {                
                var p = new Dictionary<string,string>();
                p.Add("TEXT",           GoodsSearchText.Text);
                //p.Add("CARDBOARD_ID",   GoodsCardboard.SelectedItem.Key);

                var q = new LPackClientQuery();
                q.Request.SetParam("Module","Production");
                q.Request.SetParam("Object","Goods");
                q.Request.SetParam("Action","List");
                q.Request.Timeout = Central.Parameters.RequestTimeoutMax;
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
                        if(result.ContainsKey("GOODS"))
                        {
                            var ds=(ListDataSet)result["GOODS"];
                            ds.Init();
                            GoodsGrid.UpdateItems(ds);                   
                        }
                    }
                }
            }

            GoodsGridToolbar.IsEnabled=true;
            GoodsGrid.HideSplash(); 
        }

        /// <summary>
        /// фильтрация записей
        /// </summary>
        public async void GoodsGridFilterItems()
        {
            if(GoodsGrid.GridItems!=null)
            {
                if(GoodsGrid.GridItems.Count>0)
                {
                    GoodsGrid.ShowSplash();
                    GoodsGridToolbar.IsEnabled = false;
                    Mouse.OverrideCursor = Cursors.Wait;

                    var doFilteringByProfile=false;
                    var doFilteringByCardboard=false;
                    
                    var v=GoodsForm.GetValues();

                    /*
                        по профилю
                        0 Все                                                
                     */                    
                    int profileId=v.CheckGet("PROFILE").ToInt();
                    if(profileId>0)
                    {
                        doFilteringByProfile=true;
                    }

                    /*
                        по марке картона    
                        0 Все                                                
                     */                    
                    int cardboardId=v.CheckGet("CARDBOARD").ToInt();
                    if(cardboardId>0)
                    {
                        doFilteringByCardboard=true;
                        //если выбрана конкретная марка картона, фильтрация по профилю уже не имеет смысла
                        doFilteringByProfile=false;
                    }
                  

                    if(
                        doFilteringByProfile
                        || doFilteringByCardboard
                    )
                    {
                        var items = new List<Dictionary<string,string>>();
                        foreach(Dictionary<string,string> row in ApplicationGrid.GridItems)
                        {
                            bool includeByProfile = true;
                            bool includeByCardboard = true;

                             if(doFilteringByProfile)
                            {
                                includeByProfile = false;
                                if (row.CheckGet("PROFILEID").ToInt()==profileId)
                                {
                                    includeByProfile = true;
                                }
                            }

                            if(doFilteringByCardboard)
                            {
                                includeByCardboard = false;
                                if (row.CheckGet("CARDBOARD_ID").ToInt()==cardboardId)
                                {
                                    includeByCardboard = true;
                                }
                            }

                            if(
                                includeByProfile
                                && includeByCardboard
                            )
                            {
                                items.Add(row);
                            }
                        }
                        GoodsGrid.GridItems=items;
                        
                    }

                    GoodsGrid.HideSplash();
                    GoodsGridToolbar.IsEnabled = true;
                    Mouse.OverrideCursor = null;
                }
            }
        }

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem"></param>
        public void GoodsGridUpdateActions(Dictionary<string,string> selectedItem)
        {
            SelectedItem = selectedItem;

            SaveButton.IsEnabled=false;

            if(SelectedItem.Count>0)
            {
                SaveButton.IsEnabled=true;
            }
        }

        /// <summary>
        /// загрузка вспомогательных данных для построения интерфейса
        /// список картонов и бумаги для слоев
        /// </summary>
        public async void LoadRef()
        {
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
                            ApplicationProfileDS = ListDataSet.Create(result,"PROFILES");
                            var list = new Dictionary<string,string>();
                            list.Add("0","");
                            list.AddRange<string,string>(ApplicationProfileDS.GetItemsList("ID","NAME"));

                            ApplicationProfile.Items = list;
                            ApplicationProfile.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
                        }

                        {
                            ApplicationCardboardDS = ListDataSet.Create(result,"CARDBOARD");
                            var list = new Dictionary<string,string>();
                            list.Add("0","");
                            list.AddRange<string,string>(ApplicationCardboardDS.GetItemsList("ID","NAME"));

                            ApplicationCardboard.Items = list;
                            ApplicationCardboard.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
                        }


                        {
                            GoodsProfileDS = ListDataSet.Create(result,"PROFILES");
                            var list = new Dictionary<string,string>();
                            list.Add("0","");
                            list.AddRange<string,string>(GoodsProfileDS.GetItemsList("ID_PROF","NAME"));

                            GoodsProfile.Items = list;
                            GoodsProfile.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
                        }

                        {
                            GoodsCardboardDS = ListDataSet.Create(result,"CARDBOARD");
                            var list = new Dictionary<string,string>();
                            list.Add("0","");
                            list.AddRange<string,string>(GoodsCardboardDS.GetItemsList("ID","NAME"));

                            GoodsCardboard.Items = list;
                            GoodsCardboard.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
                        }

                        RefSourcesLoaded=true;
                        InitData();
                    }
                }
            }
        }


        /// <summary>
        /// выбор заготовки
        /// </summary>
        public void Save()
        {
            if(SelectedItem!=null)
            {
                if(SelectedItem.Count>0)
                {
                    SelectedItem.CheckAdd("STACKER_ID",StackerId.ToString());

                    //отправляем сообщение о выборе заготовки
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup="ProductionTask",
                        ReceiverName = "CuttingManualView",
                        SenderName = "SelectBlankView",
                        Action = "SelectedBlank",
                        ContextObject=SelectedItem,
                    });

                    Close();
                }
            }
        }

        public void GoodsCheckForm()
        {
            if(!string.IsNullOrEmpty(GoodsSearchText.Text))
            {
                GoodsRefreshButton.IsEnabled=true;
                GoodsRefreshButtonCheck(true);
            }
            else
            {
                GoodsRefreshButton.IsEnabled=false;
                GoodsRefreshButtonCheck(false);
            }
        }

        public void ApplicationRefreshButtonCheck(bool b)
        {
            ApplicationRefreshButtonBlue=b;

            var style="Button";
            if(ApplicationRefreshButtonBlue)
            {
                style="FButtonPrimary";
            }
            ApplicationRefreshButton.Style=(Style)ApplicationRefreshButton.TryFindResource(style);
        }

        public void GoodsRefreshButtonCheck(bool b)
        {
            GoodsRefreshButtonBlue=b;

            var style="Button";
            if(GoodsRefreshButtonBlue)
            {
                style="FButtonPrimary";
            }
            GoodsRefreshButton.Style=(Style)GoodsRefreshButton.TryFindResource(style);
        }
        
        public Window Window { get; set; }
        public void Show()
        {
            var rr=BackTabName;
            var r2=StackerId;
            string title=$"Выбор заготовки";            
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

        private void ApplicationRefreshButton_Click(object sender,RoutedEventArgs e)
        {
            ApplicationGrid.LoadItems();
            ApplicationRefreshButtonCheck(false);
        }

        private void CancelButton_Click(object sender,RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender,RoutedEventArgs e)
        {
            Save();
        }

        private void ApplicationCardboard_SelectedItemChanged(DependencyObject d,DependencyPropertyChangedEventArgs e)
        {
            ApplicationGrid.UpdateItems();
        }

        private void ApplicationType_SelectedItemChanged(DependencyObject d,DependencyPropertyChangedEventArgs e)
        {
            ApplicationGrid.UpdateItems();
        }

        private void ApplicationFilter1_Click(object sender,RoutedEventArgs e)
        {
            ApplicationGrid.UpdateItems();
        }

        private void GoodsRefreshButton_Click(object sender,RoutedEventArgs e)
        {
            GoodsGrid.LoadItems();
            GoodsRefreshButtonCheck(false);
        }

        private void GoodsSearchText_TextChanged(object sender,TextChangedEventArgs e)
        {
            GoodsCheckForm();            
        }

        private void GoodsSearchText_KeyUp(object sender,KeyEventArgs e)
        {
            if(e.Key==System.Windows.Input.Key.Enter)
            {
                GoodsGrid.LoadItems();
            }
        }

        private void HelpButton_Click(object sender,RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void ApplicationFilterZ_Click(object sender,RoutedEventArgs e)
        {
            ApplicationGrid.UpdateItems();
        }

        private void FromDate_TextChanged(object sender,TextChangedEventArgs e)
        {
            ApplicationRefreshButtonCheck(true);
        }

        private void ToDate_TextChanged(object sender,TextChangedEventArgs e)
        {
            ApplicationRefreshButtonCheck(true);
        }

        private void Test1_Click(object sender,RoutedEventArgs e)
        {
            if(!string.IsNullOrEmpty(ApplicationFormSessionKey))
            {
                var v=ApplicationForm.GetValues();
                Central.SessionValues[ApplicationFormSessionKey]=v;
            }    
        }

        private void Test2_Click(object sender,RoutedEventArgs e)
        {
            ApplicationGrid.UpdateItems();
        }

        private void ApplicationResetButton_Click(object sender,RoutedEventArgs e)
        {
            SetDefaults(true);
            //ApplicationGrid.LoadItems();
        }

        private void ApplicationFilterFirst_Click(object sender,RoutedEventArgs e)
        {
            ApplicationGrid.UpdateItems();
        }

        private void HideUncutted_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ApplicationGrid.UpdateItems();
        }
    }
}
