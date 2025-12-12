using Client.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client.Interfaces.Production
{

    //FIXME: std, std methods
    //  нужно реализовать стандартыне функции
    //  http://192.168.3.237/developer/std/cheklist-21-11 
    //  g 34

    /// <summary>
    /// Окно настроек создания производственных заданий на ГА
    /// </summary>
    public partial class ProductionTaskSettings : UserControl
    {
        public ProductionTaskSettings()
        {
            InitializeComponent();

            InitForm();
            SetDefaults();
        }

        /// <summary>
        /// Форма
        /// </summary>
        public FormHelper Form { get; set; }
        /// <summary>
        /// Основное окно настроек
        /// </summary>
        public Window Window { get; set; }

        /// <summary>
        /// Инициализация формы
        /// </summary>
        private void InitForm()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="PAPER_CONDITIONING",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ConditioningHours,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
            };
            Form.SetFields(fields);

            Form.OnValidate = (bool valid, string message) =>
            {
                if (valid)
                {
                    FormStatus.Text = "";
                }
                else
                {
                    FormStatus.Text = "Не все поля заполнены верно";
                }
            };
        }

        public void SetDefaults()
        {
            Form.SetDefaults();
        }

        public async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "ProductionTask");
            q.Request.SetParam("Action", "SettingsGet");

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
                    var p = new Dictionary<string, string>();

                    if (ds.Items.Count > 0)
                    {
                        if (ds.Items[0].ContainsKey("PARAM_VALUE"))
                        {
                            p.Add("PAPER_CONDITIONING", ds.Items[0]["PARAM_VALUE"]);
                        }
                    }
                    Form.SetValues(p);
                }
            }
        }

        public void Save()
        {
            if (Form.Validate())
            {
                var p = Form.GetValues();
                SaveData(p);
            }
        }

        private async void SaveData(Dictionary<string, string> p)
        {

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "ProductionTask");
            q.Request.SetParam("Action", "SettingsSave");

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
                        Close();
                    }
                }
            }
        }

        public void Edit()
        {
            GetData();
            Show();
        }

        public void Show()
        {
            //FIXME: здесь нужно использовать windowmanager

            string title = $"Настройка";

            int w = Width.ToInt();
            int h = Height.ToInt();

            Window = new Window
            {
                Title = title,
                Width = w + 17,
                Height = h + 40,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow,

            };

            Window.Content = new Frame
            {
                Content = this,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };


            if (Window != null)
            {
                Window.Topmost = true;
                Window.ShowDialog();
            }

        }

        public void Close()
        {
            var window = this.Window;
            if (window != null)
            {
                window.Close();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }
    }
}
