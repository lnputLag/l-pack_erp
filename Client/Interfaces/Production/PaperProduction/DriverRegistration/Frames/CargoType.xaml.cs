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
using System.Linq;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Выбор вида груза
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    /// <changed>2024-03-14</changed>///  
    public partial class CargoType : WizardFrame
    {
        public CargoType()
        {

            DataStateList = new List<Dictionary<string, string>>();
            InitializeComponent();

            if (Central.InDesignMode())
            {
                return;
            }

            InitForm();
            SetDefaults();
            Central.Msg.Register(ProcessMessage);
        }

        /// <summary>
        /// Признак доступности кнопки "Я привез химию"
        /// </summary>
        private int ChemistryIs { get; set; }

        /// <summary>
        /// Признак доступности кнопки "Я привез рулоны"
        /// </summary>
        private int RollIs { get; set; }

        /// <summary>
        /// Признак доступности кнопки "Я привез ТМЦ"
        /// </summary>
        private int InventoryIs { get; set; }

        private List<Dictionary<string, string>> DataStateList { get; set; }

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
                    Path="CARGO_TYPE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    First=true,
                },
                new FormHelperField()
                {
                    Path="CARGO_TYPE_DESCRIPTION",
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
            if (message != null)
            {
                if (message.ReceiverName == ControlName)
                {
                    switch (message.Action)
                    {
                        //фрейм загружен 
                        case "Showed":

                            // больше кнопка не нужна
                            WastePaperButton.IsEnabled = false;
                            WastePaperButton.Visibility = Visibility.Hidden;

                            // ПЭС
                            PolyethyleneMixButton.Visibility = Visibility.Visible;
                            // Химия
                            ChemistryButton.Visibility = Visibility.Visible;
                            // Код брони
                            BookingButton.Visibility = Visibility.Visible;

                            var v = Wizard.Values;
                            //если это БДМ1 то кнопки "Я приехал за ПЭС", "Я знаю код брони" и "Я привез химию " должны быть не активны
                            if (v.CheckGet("MACHINE_NUMBER").ToInt() == 1)
                            {
                                {
                                    // до ремонта
                                 //   WastePaperButton.Visibility = Visibility.Visible;
                                    PolyethyleneMixButton.Visibility = Visibility.Hidden;
                                    ChemistryButton.Visibility = Visibility.Hidden;
                                    BookingButton.Visibility = Visibility.Hidden;
                                    RollButton.Visibility = Visibility.Visible;
                                }
                            }

                            //если это БДМ2, то кнопка "Я привез химию" должно быть активна
                            if (v.CheckGet("MACHINE_NUMBER").ToInt() == 2)
                            {
                                ChemistryButton.Visibility = Visibility.Visible;
                                ChemistryButton.IsEnabled = true;
                                BookingButton.Visibility = Visibility.Visible;
                                BookingButton.IsEnabled = true;

                                // кнопка "Я привез ТМЦ"
                                InventoryButton.IsEnabled = false;
                                InventoryButton.Visibility = Visibility.Hidden;
                                // я привез рулоны
                                RollButton.Visibility = Visibility.Hidden;
                                RollButton.IsEnabled = false;
                            }

                            //если это БДМ2 и в CONFIGURATION_OPTIONS.BDM2_CHEMISTRY = 0, то кнопка "Я привез макулатуру" должно быть не активна
                            if (v.CheckGet("MACHINE_NUMBER").ToInt() == 2)
                            {
                                var dataList = new List<Dictionary<string, string>>();
                                var row2 = new Dictionary<string, string>();
                                row2.CheckAdd("PARAM_NAME", "BDM2_CHEMISTRY");
                                dataList.Add(row2);
                                GetData(dataList);

                                if (ChemistryIs == 0)
                                {
                                    // кнопка "Я привез макулатуру"
                                    WastePaperButton.Visibility = Visibility.Hidden;
                                }
                            }

                            //если это БДМ1 и в CONFIGURATION_OPTIONS.BDM1_REG = 0, то кнопка "Я знаю код брони" должно быть не активна
                            if (v.CheckGet("MACHINE_NUMBER").ToInt() == 1)
                            {
                                var dataList = new List<Dictionary<string, string>>();
                                var row2 = new Dictionary<string, string>();
                                row2.CheckAdd("PARAM_NAME", "BDM1_REG");
                                dataList.Add(row2);
                                GetData(dataList);

                                if (ChemistryIs == 1)
                                {
                                    // кнопка "Я знаю код брони"
                                    BookingButton.Visibility = Visibility.Visible;
                                    BookingButton.IsEnabled = true;
                                }
                            }

                            SetDefaults();
                            LoadValues();
                            break;
                    }
                }
            }
        }

        private void SetCargoType(object sender)
        {
            if (sender != null)
            {
                var button = (Button)sender;
                var tag = button.Tag;
                var type = tag.ToInt();
                var typeDescription = "";

                switch (type)
                {
                    case 1:
                        typeDescription = "Я привез макулатуру";
                        break;
                    case 2:
                        typeDescription = "Я приехал за полиэтиленовой смесью";
                        break;
                    case 3:
                        typeDescription = "Я привез химию";
                        break;
                    case 4:
                        typeDescription = "Я привез ТМЦ";
                        break;
                    case 5:
                        typeDescription = "Я привез рулоны";
                        break;
                    case 6:
                        typeDescription = "Я знаю код брони";
                        break;

                }

                Form.SetValueByPath("CARGO_TYPE", type.ToString());
                Form.SetValueByPath("CARGO_TYPE_DESCRIPTION", typeDescription.ToString());
                Validate();
            }
        }

        /// <summary>
        /// проверка, можно ли активировать кнопку "далее"
        /// </summary>
        private void Validate()
        {
            var s = Form.GetValueByPath("CARGO_TYPE");

            SaveValues();
            if (s.ToInt() == 4 || s.ToInt() == 5)
            {
                Wizard.Navigate("FioEdit");
            }
            else if (s.ToInt() == 6)
            {
                Wizard.Navigate("BookingCode");
            }
            else if (s.ToInt() > 0)
            {
                Wizard.Navigate(1);
            }
        }

        /// <summary>
        /// активация/деактивация кнопки "далее"
        /// </summary>
        /// <param name="mode"></param>
        private void NextButtonSet(bool mode = true)
        {
            if (NextButton != null)
            {
                if (mode)
                {
                    NextButton.IsEnabled = true;
                    NextButton.Opacity = 1.0;
                    NextButton.Style = (Style)NextButton.TryFindResource("TouchFormButtonPrimaryBig");
                }
                else
                {
                    NextButton.IsEnabled = false;
                    NextButton.Opacity = 0.5;
                    NextButton.Style = (Style)NextButton.TryFindResource("TouchFormButtonBig");
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

        private void SetCargoTypeButtonClick(object sender, RoutedEventArgs e)
        {
            SetCargoType(sender);
        }

        /// <summary>
        /// запрос на получение данных из CONFIGURATION_OPTIONS
        /// </summary>
        private void GetData(List<Dictionary<string, string>> list)
        {
            ChemistryIs = 0;

            var listString = JsonConvert.SerializeObject(list);

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("DATA_LIST", listString);
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "PMFire");
            q.Request.SetParam("Action", "GetData");
            q.Request.SetParams(p);

            q.Request.Timeout = 10000;
            q.Request.Attempts = 1;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds.Items.Count > 0)
                    {
                        DataStateList = ds.Items;
                        if (DataStateList.Count > 0)
                        {
                            var first = DataStateList.First();
                            if (first != null)
                            {
                                ChemistryIs = first.CheckGet("PARAM_VALUE").ToInt();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Привезли рулоны на терминал №26 или №27
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RollButton_Click(object sender, RoutedEventArgs e)
        {
            SetCargoType(sender);
        }

        /// <summary>
        /// я знаю код брони
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BookingButton_Click(object sender, RoutedEventArgs e)
        {
            SetCargoType(sender);
        }
        
        /// <summary>
        /// я привез химию
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChemistryButton_Click(object sender, RoutedEventArgs e)
        {
            SetCargoType(sender);
        }

    }
}
