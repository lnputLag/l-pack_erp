using Client.Common;
using Client.Interfaces.Main;
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

namespace Client.Interfaces.Production.Corrugator.TaskPlanningKashira
{
    /// <summary>
    /// Interaction logic for TaskItem.xaml
    /// </summary>
    public partial class TaskItem : ControlBase
    {
        public TaskItem()
        {
            InitializeComponent();

            ControlTitle = "Задание на ГА";
            InitializeFrom();

            OnLoad = () =>
            {
                SetDefaults();
                
            };

            OnUnload = () =>
            {
                
            };

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverGroup == "PreproductionSample")
                {
                    if (msg.ReceiverName == ControlName)
                    {
                        //ReceivedData.Clear();
                        //if (msg.ContextObject != null)
                        //{
                        //    ReceivedData = (Dictionary<string, string>)msg.ContextObject;
                        //}
                        //ProcessCommand(msg.Action);
                    }
                }
            };

            OnFocusGot = () =>
            {

            };

            OnFocusLost = () =>
            {

            };

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    //switch (e.Key)
                    //{
                    //    case Key.F1:
                    //        ProcessCommand("help");
                    //        e.Handled = true;
                    //        break;
                    //    case Key.F5:
                    //        SampleGrid.LoadItems();
                    //        e.Handled = true;
                    //        break;

                    //    case Key.Home:
                    //        SampleGrid.SetSelectToFirstRow();
                    //        e.Handled = true;
                    //        break;

                    //    case Key.End:
                    //        SampleGrid.SetSelectToLastRow();
                    //        e.Handled = true;
                    //        break;
                    //}
                }


            };
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        private void InitializeFrom()
        {
            Form = new FormHelper();
            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path=TaskPlaningDataSet.Dictionary.ProductionTaskId,
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=OrderText,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path= TaskPlaningDataSet.Dictionary.Format,
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=FormatText,
                    ControlType="TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 6 }
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

        private void SetDefaults()
        {
        }

        public void Edit(Dictionary<string, string> item)
        {
            OrderText.Text = item.CheckGet(TaskPlaningDataSet.Dictionary.ProductionTaskId).ToInt().ToString();
            OrderName.Text = item.CheckGet("PRODUCTION_TASK_NUMBER").ToString();
            FormatText.Text = item.CheckGet(TaskPlaningDataSet.Dictionary.Format).ToInt().ToString();


            Form.Validate();


            ControlName = $"{ControlName}_{item.CheckGet(TaskPlaningDataSet.Dictionary.ProductionTaskId).ToInt().ToString()}";
            Central.WM.Show(ControlName, $"Задание #{item.CheckGet(TaskPlaningDataSet.Dictionary.ProductionTaskId).ToInt().ToString()}", true, "add", this);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();

        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.Close(ControlName);
        }
    }
}
