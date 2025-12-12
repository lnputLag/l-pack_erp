using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;
using Client.Interfaces.Main;
using System.Windows.Media;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Выбор поставщика
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    public partial class Vendor : WizardFrame
    {
        public Vendor()
        {
            InitializeComponent();
            
            if(Central.InDesignMode()){
                return;
            }
            
            InitForm();
            InitGrid();
            SetDefaults();
            Central.Msg.Register(ProcessMessage);
        }

        /// <summary>
        /// Инициализация формы
        /// </summary>
        public void InitForm()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {           
                new FormHelperField()
                {
                    Path="_SEARCH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=SearchText,                    
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },          
                    First=true,
                },
                new FormHelperField()
                {
                    Path="VENDOR_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },          
                    First=true,
                },
                new FormHelperField()
                {
                    Path="VENDOR_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },          
                    First=true,
                },
            };

            Form.SetFields(fields);
        }

        public void InitGrid()
        {
             //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Название",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width=700,
                        MaxWidth=1000,
                        MinWidth =100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden = true,
                    },
                };
                Grid.SetColumns(columns);
            };

            Grid.UseSorting = false;
            Grid.AutoUpdateInterval = 0;
            Grid.UseRowHeader = false;
            Grid.SelectItemMode = 0;
            Grid.SetMode(1);
            Grid.Init();


            Grid.DisableControls=()=>
            {
                GridToolbar.IsEnabled = false;
                Grid.ShowSplash();
            };
                
            Grid.EnableControls=()=>
            {
                GridToolbar.IsEnabled = true;
                Grid.HideSplash();
            };

            Grid.OnLoadItems = async ()=>
            {
                Grid.DisableControls();

                var today=DateTime.Now;
                bool resume = true;

                if (resume)
                {
                    var p = new Dictionary<string, string>();
                    {
                    }

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "PaperProduction");
                    q.Request.SetParam("Object", "TransportDriver");
                    q.Request.SetParam("Action", "ListVendor");
                    q.Request.SetParams(p);

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if(q.Answer.Status == 0)                
                    {
                        var items=new List<Dictionary<string, string>>();
                        items.Add(new Dictionary<string, string>()
                        {
                            {"NAME","..."},
                            {"ID","0"}
                        });
                        Grid.UpdateItemsAnswerPrepend(q.Answer,"ITEMS",items);
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }

                Grid.EnableControls();
            };

            Grid.OnSelectItem = (row) =>
            {
                if(row!=null)
                {
                    Form.SetValueByPath("VENDOR_ID",row.CheckGet("ID"));
                    Form.SetValueByPath("VENDOR_NAME",row.CheckGet("NAME"));
                    Validate();
                }
            };

            Grid.Run();

            {
                ScaleTransform scale = new ScaleTransform(1.2, 1.2);
                Grid.LayoutTransform = scale;
            }

           
            
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            //установка значений по умолчанию
            Form.SetDefaults();
            NextButtonSet(false);
        }

        /// <summary>
        /// обработка сообщений
        /// </summary>
        /// <param name="message"></param>
        public void ProcessMessage(ItemMessage message)
        {
            if(message!=null)
            {
                if(message.ReceiverName==ControlName)
                {
                    switch (message.Action)
                    {
                        //фрейм загружен 
                        case "Showed":
                            Grid.SelectedItem=new Dictionary<string, string>();
                            Grid.UpdateItems();
                            Check();
                            SetDefaults();
                            LoadValues();

                            Grid.Run();
                            Form.SetValueByPath("_SEARCH", "");
                            Grid.SearchItems("");
                            Grid.SelectRowByKey("0", "ID");

                            break;

                        //ввод с экранной клавиатуры
                        case "KeyPressed":
                            ChangeValue(message.Message);                        
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// ввод в поле из виртуальной клавиатуры
        /// </summary>
        /// <param name="symbol"></param>
        private void ChangeValue(string symbol)
        {
            if (IsActive() && !string.IsNullOrEmpty(symbol))
            {
                var s = Form.GetValueByPath("_SEARCH");
                switch (symbol)
                {
                    case "BACK_SPACE":
                        if (s.Length > 0)
                        {
                            s = s.Substring(0, (s.Length - 1));
                        }
                        break;

                    default:
                        if (s.Length < 10)
                        {
                            s = s + symbol;
                        }
                        break;
                }
                Form.SetValueByPath("_SEARCH", s);
                Grid.SearchItems(s);
            }

            
            //Validate();
        }

        /// <summary>
        /// проверка, можно ли активировать кнопку "далее"
        /// </summary>
        private void Validate()
        {
            if(Form.GetValueByPath("VENDOR_ID").ToInt() != 0)
            {
                NextButtonSet(true);
                SaveValues();
            }
            else
            {
                NextButtonSet(false);
            }
        }

        private void Check()
        {
            //если пэс,химия и ТМЦ пропускаем шаг
            var v=Wizard.Values;
            switch(v.CheckGet("CARGO_TYPE").ToInt())
            {
                //typeDescription="Я привез макулатуру";
                case 1:
                {
                }
                break;

                //typeDescription="Я приехал за полиэтиленовой смесью";
                case 2:
                {
                    Wizard.Navigate(1);
                }
                break;

                //typeDescription="Я привез химию";
                case 3:
                    {
                        Wizard.Navigate(1);
                    }
                    break;
                //typeDescription="Я привез ТМЦ";
                case 4:
                    {
                        Wizard.Navigate(1);
                    }
                    break;
                //typeDescription="Я привез рулоны";
                case 5:
                    {
                        Wizard.Navigate(1);
                    }
                    break;

            }
        }

        /// <summary>
        /// активация/деактивация кнопки "далее"
        /// </summary>
        /// <param name="mode"></param>
        private void NextButtonSet(bool mode=true)
        {
            if(NextButton!=null)
            {
                if(mode)
                {
                    NextButton.IsEnabled=true;
                    NextButton.Opacity=1.0;
                    NextButton.Style=(Style)NextButton.TryFindResource("TouchFormButtonPrimaryBig");
                }
                else
                {
                    NextButton.IsEnabled=false;
                    NextButton.Opacity=0.5;
                    NextButton.Style=(Style)NextButton.TryFindResource("TouchFormButtonBig");
                }
            }
        }

        /// <summary>
        /// нажали кнопку "Домой"
        /// </summary>
        private void HomeButtonClick(object sender, RoutedEventArgs e)
        {
            Wizard.Navigate(0);
        }

        /// <summary>
        /// нажали кнопку "Предыдущий"
        /// </summary>
        private void PriorButtonClick(object sender, RoutedEventArgs e)
        {
            Wizard.Navigate(-1);
        }

        /// <summary>
        /// нажали кнопку "Следующий"
        /// </summary>
        private void NextButtonClick(object sender, RoutedEventArgs e)
        {
            Wizard.Navigate(1);
        }
    }
}
