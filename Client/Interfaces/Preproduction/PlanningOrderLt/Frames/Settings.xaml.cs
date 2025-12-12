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
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;

namespace Client.Interfaces.Preproduction.PlanningOrderLt.Frames
{
    /// <summary>
    /// Настройка планирования ЛТ
    /// </summary>
    /// <author>volkov_as</author>
    public partial class Settings : ControlBase
    {
        public Settings()
        {
            InitializeComponent();

            FrameTitle = "Настройка планирования ЛТ";

            OnLoad = () =>
            {
                InitializeForm();
                LoadInfo();
            };

            OnKeyPressed = (e) =>
            {
                if (!e.Handled)
                {
                    if (e.Key == Key.Escape)
                    {
                        Close();
                    }
                }
            };
        }

        public FormHelper Form;

        public event Action OnSettingsClosed;

        private void InitializeForm()
        {
            Form = new FormHelper();

            var fields = new List<FormHelperField>()
            {
                new FormHelperField // Скорость
                {
                    Path = "SPEED_PRINTER_AAEI",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = SpeedPrinterAAEI,
                    ControlType = "TextBox"
                },
                new FormHelperField
                {
                    Path = "SPEED_PRINTER_BST",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = SpeedPrinterBST,
                    ControlType = "TextBox"
                },
                new FormHelperField
                {
                    Path = "SPEED_LABELER_AAEI",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = SpeedLabelerAAEI,
                    ControlType = "TextBox"
                },
                new FormHelperField
                {
                    Path = "SPEED_LABELER_BST",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = SpeedLabelerBST,
                    ControlType = "TextBox"
                },
                new FormHelperField // КПД
                {
                    Path = "EFFICIENCY_PRINTER_AAEI",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = EfficiencyPrinterAAEI,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.IsNotZero, null }
                    }
                },
                new FormHelperField
                {
                    Path = "EFFICIENCY_PRINTER_BST",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = EfficiencyPrinterBST,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.IsNotZero, null }
                    }
                },
                new FormHelperField
                {
                    Path = "EFFICIENCY_LABELER_AAEI",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = EfficiencyLabelerAAEI,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.IsNotZero, null }
                    }
                },
                new FormHelperField
                {
                    Path = "EFFICIENCY_LABELER_BST",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = EfficiencyLabelerBST,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.IsNotZero, null }
                    }
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = Status;

            Form.OnValidate = (bool valid, string message) =>
            {
                if (valid)
                {
                    Status.Text = "";
                }
                else
                {
                    Status.Text = "Не все поля заполнены верно";
                }
            };
        }

        /// <summary>
        /// Загрузка информации
        /// </summary>
        /// <returns></returns>
        private async Task LoadInfo()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PlanningOrderLt");
            q.Request.SetParam("Action", "GetSpeedAndEfficiency");

            await Task.Run(() => q.DoQuery());

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(q.Answer.Data);

                if (result != null)
                {
                    var printAaei = result.First(x => x.CheckGet("ID_ST").ToInt() == 312);
                    SpeedPrinterAAEI.Text = printAaei.CheckGet("SPEED_MAX");
                    EfficiencyPrinterAAEI.Text = printAaei.CheckGet("EFFICIENCY_PCT");

                    var printBst = result.First(x => x.CheckGet("ID_ST").ToInt() == 311);
                    SpeedPrinterBST.Text = printBst.CheckGet("SPEED_MAX");
                    EfficiencyPrinterBST.Text = printBst.CheckGet("EFFICIENCY_PCT");

                    var labelerAaei = result.First(x => x.CheckGet("ID_ST").ToInt() == 322);
                    SpeedLabelerAAEI.Text = labelerAaei.CheckGet("SPEED_MAX");
                    EfficiencyLabelerAAEI.Text = labelerAaei.CheckGet("EFFICIENCY_PCT");

                    var labelerBst = result.First(x => x.CheckGet("ID_ST").ToInt() == 321);
                    SpeedLabelerBST.Text = labelerBst.CheckGet("SPEED_MAX");
                    EfficiencyLabelerBST.Text = labelerBst.CheckGet("EFFICIENCY_PCT");
                }
            }
        }

        /// <summary>
        /// Сохранение новых значений
        /// </summary>
        /// <returns></returns>
        private async Task SaveNewEfficiency()
        {
            if (!Form.Validate())
            {
                return;
            }

            var p = new Dictionary<string, string>
            {
                { "311", $"{EfficiencyPrinterBST.Text}" },
                { "312", $"{EfficiencyPrinterAAEI.Text}" },
                { "321", $"{EfficiencyLabelerBST.Text}" },
                { "322", $"{EfficiencyLabelerAAEI.Text}" },
            };

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PlanningOrderLt");
            q.Request.SetParam("Action", "SaveNewEfficiency");
            q.Request.SetParam("ITEMS", JsonConvert.SerializeObject(p));

            await Task.Run(() => q.DoQuery());

            if (q.Answer.Status == 0)
            {
                OnSettingsClosed?.Invoke();
                Close();
            }
            else
            {
                var dialog = new DialogWindow("Ошибка при сохранении новых значений", "Настройка планирования ЛТ");
                dialog.ShowDialog();
            }
        }

        /// <summary>
        /// Открытие настроек
        /// </summary>
        public void Open()
        {
            Show();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveNewEfficiency();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            OnSettingsClosed?.Invoke();
            Close();
        }
    }
}
