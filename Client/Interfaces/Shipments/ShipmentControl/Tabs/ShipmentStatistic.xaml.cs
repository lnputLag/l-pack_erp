using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Упралвение отгрузками. Статистика СГП
    /// </summary>
    public partial class ShipmentStatistic : ControlBase
    {
        public ShipmentStatistic()
        {
            ControlTitle = "Статистика СГП";
            DocumentationUrl = "/doc/l-pack-erp/shipments/";
            RoleName = "[erp]shipment_control";
            InitializeComponent();

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                FormInit();
                SetDefaults();
                FormFieldRunAutoupdateTimer();

                ProcessPermissions();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                FormFieldAutoUpdateTimer?.Finish();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                FormFieldItemsAutoUpdate = true;
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                FormFieldItemsAutoUpdate = false;
            };
        }

        /// <summary>
        /// Идентификатор площадки
        /// </summary>
        private int FactoryId = 1;

        /// <summary>
        /// Таймер авообновления данных формы
        /// </summary>
        private Common.Timeout FormFieldAutoUpdateTimer { get; set; }

        /// <summary>
        /// Флаг работы автообновления данных формы. Аналог Grid.ItemsAutoUpdate
        /// </summary>
        private bool FormFieldItemsAutoUpdate { get; set; }

        /// <summary>
        /// секунды
        /// </summary>
        private int FormFieldAutoUpdateInterval { get; set; }

        private ListDataSet FormFieldDataSet { get; set; }

        private FormHelper Form { get; set; }

        private void FormInit()
        {
            //инициализация формы
            {
                Form = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="FROM_DATE_TIME",
                        FieldType=FormHelperField.FieldTypeRef.DateTime,
                        Control=FromDateTime,
                        ControlType="dateedit",
                        Format="dd.MM.yyyy HH:mm:ss",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        }
                    },
                    new FormHelperField()
                    {
                        Path="TO_DATE_TIME",
                        FieldType=FormHelperField.FieldTypeRef.DateTime,
                        Control=ToDateTime,
                        ControlType="dateedit",
                        Format="dd.MM.yyyy HH:mm:ss",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        }
                    },
                };

                Form.SetFields(fields);
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            FormFieldDataSet = new ListDataSet();
            FormFieldAutoUpdateInterval = 300;

            var date = DateTime.Now;
            if (date.Hour < 8)
            {
                Form.SetValueByPath("FROM_DATE_TIME", $"{date.AddDays(-1).ToString("dd.MM.yyyy")} 08:00:00");
                Form.SetValueByPath("TO_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 08:00:00");
            }
            else
            {
                Form.SetValueByPath("FROM_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 08:00:00");
                Form.SetValueByPath("TO_DATE_TIME", $"{date.AddDays(1).ToString("dd.MM.yyyy")} 08:00:00");
            }
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
        }

        private void FormFieldRunAutoupdateTimer()
        {
            if (FormFieldAutoUpdateInterval > 0)
            {
                FormFieldAutoUpdateTimer = new Common.Timeout(
                    FormFieldAutoUpdateInterval,
                    () =>
                    {
                        if (FormFieldItemsAutoUpdate)
                        {
                            FormFieldLoadItems();
                        }
                    },
                    true,
                    false
                );
                {
                    FormFieldAutoUpdateTimer.Restart();
                }
            }
        }

        private async void FormFieldLoadItems()
        {
            bool resume = true;
            var f = Form.GetValueByPath("FROM_DATE_TIME").ToDateTime();
            var t = Form.GetValueByPath("TO_DATE_TIME").ToDateTime();
            if (DateTime.Compare(f, t) > 0)
            {
                const string msg = "Дата начала должна быть меньше даты окончания.";
                var d = new DialogWindow($"{msg}", "Проверка данных");
                d.ShowDialog();
                resume = false;
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                p.Add("FACTORY_ID", $"{this.FactoryId}");
                p.Add("DTTM_FROM", Form.GetValueByPath("FROM_DATE_TIME"));
                p.Add("DTTM_TO", Form.GetValueByPath("TO_DATE_TIME"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "Statistics");
                q.Request.SetParam("Action", "GetShipmentPlanByDateTime");
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
                        FormFieldDataSet = ListDataSet.Create(result, "ITEMS");
                        FormFieldUpdateItems(FormFieldDataSet);
                    }
                }
                else
                {
                    q.SilentErrorProcess = true;
                    q.ProcessError();
                }
            }
        }

        public void FormFieldUpdateItems(ListDataSet dataSet)
        {
            if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
            {
                Dictionary<string, string> data = dataSet.Items.First();

                ShipmentPlanCount.Text = data.CheckGet("SHIPMENT_PLAN_CNT").ToDouble().ToString("###,###,##0");
                ShipmentPlanAllowedCount.Text = data.CheckGet("SHIPMENT_ALLOWED_PLAN_CNT").ToDouble().ToString("###,###,##0");
                ShipmentFactCount.Text = data.CheckGet("SHIPPED_CNT").ToDouble().ToString("###,###,##0");
                ShipmentPlanPalletQuantity.Text = data.CheckGet("SHIPMENT_PALLET_QTY").ToDouble().ToString("###,###,##0");
                ShipmentFactPalletQuantity.Text = data.CheckGet("SHIPPED_PALLET_QTY").ToDouble().ToString("###,###,##0");
                ShipmentPlanSquare.Text = data.CheckGet("SHIPMENT_SQUARE").ToDouble().ToString("###,###,##0");
                ShipmentFactSquare.Text = data.CheckGet("SHIPPED_SQUARE").ToDouble().ToString("###,###,##0");
                ShipmentFactPalletTimeAvg.Text = data.CheckGet("AVG_SHIPMENT_PALLET_TIME").ToDouble().ToString("###,###,##0.0");
                ShipmentFactTimeAvg.Text = data.CheckGet("AVG_SHIPMENT_TIME").ToDouble().ToString("###,###,##0.0");
            }
        }

        public void Refresh()
        {
            FormFieldLoadItems();
        }

        private void OnCurrentShift(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now;
            if (date.Hour >= 20 || date.Hour < 8)
            {
                if (date.Hour >= 20)
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.AddDays(1).ToString("dd.MM.yyyy")} 08:00:00");
                }
                else
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00");
                }
            }
            else
            {
                Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00");
                Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00");
            }

            Refresh();
        }

        private void OnPrevShift(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now.AddHours(-12);
            if (date.Hour >= 20 || date.Hour < 8)
            {
                if (date.Hour >= 20)
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{date.AddDays(1).ToString("dd.MM.yyyy")} 08:00:00");
                }
                else
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{date.AddDays(-1).ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 08:00:00");
                }
            }
            else
            {
                Form.SetValueByPath("FROM_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 08:00:00");
                Form.SetValueByPath("TO_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 20:00:00");
            }

            Refresh();
        }

        private void OnCurrentDay(object sender, RoutedEventArgs e)
        {
            Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.AddDays(1).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnPrevDay(object sender, RoutedEventArgs e)
        {
            Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnCurrentWeek(object sender, RoutedEventArgs e)
        {
            DayOfWeek day = DateTime.Now.DayOfWeek;
            int days = day - DayOfWeek.Monday;
            DateTime date = DateTime.Now.AddDays(-days);

            Form.SetValueByPath("FROM_DATE_TIME", $"{date.Date.ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{date.Date.AddDays(7).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnPrevWeek(object sender, RoutedEventArgs e)
        {
            DayOfWeek day = DateTime.Now.DayOfWeek;
            int days = day - DayOfWeek.Monday;
            DateTime date = DateTime.Now.AddDays(-days).AddDays(-7);

            Form.SetValueByPath("FROM_DATE_TIME", $"{date.Date.ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{date.Date.AddDays(7).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnCurrentMonth(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now;

            Form.SetValueByPath("FROM_DATE_TIME", $"{new DateTime(date.Year, date.Month, 1).ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{new DateTime(date.Year, date.Month, 1).AddMonths(1).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnPrevMonth(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now.AddMonths(-1);

            Form.SetValueByPath("FROM_DATE_TIME", $"{new DateTime(date.Year, date.Month, 1).ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{new DateTime(date.Year, date.Month, 1).AddMonths(1).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }
    }
}
