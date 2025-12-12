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

namespace Client.Interfaces.Production.Corrugator.TaskPlannings
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : ControlBase
    {
        public Settings()
        {
            InitializeComponent();

            ControlTitle = "Настройки планирования";
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
                    
                }


            };

            LastDateTime.EditValue = TaskPlaningDataSet.LastDateTime;
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Минимальный размер блока 
        /// </summary>
        public static int MinimalBlockSize { get; set; } = 1000;

        /// <summary>
        /// Максимальная длина короткого блока
        /// </summary>
        public static int ShortBlockSize { get; set; } = 600;

        private void InitializeFrom()
        {
            Form = new FormHelper();
            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="MinimalBlockText",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=MinimalBlockText,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                new FormHelperField()
                {
                    Path="ShortBlockText",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ShortBlockText,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
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
            double hours = (TaskPlaningDataSet.LastDateTime - DateTime.Now).TotalHours + 0.1;
            LastDateTimeHours.Text = ((int)(hours)).ToString();
        }

        public void Edit()
        {
            MinimalBlockText.Text = MinimalBlockSize.ToString();
            ShortBlockText.Text= ShortBlockSize.ToString();

            Form.Validate();


            ControlName = $"{ControlName}";
            Central.WM.Show(ControlName, $"Настройки планирования", true, "add", this);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(MinimalBlockText.Text, out int minimalBlockSize))
            {
                MinimalBlockSize = minimalBlockSize;
            }

            if (int.TryParse(ShortBlockText.Text, out int shortBlockBlockSize))
            {
                ShortBlockSize = shortBlockBlockSize;
            }

            if (LastDateTime.EditValue is DateTime dt)
            {
                TaskPlaningDataSet.LastDateTime = dt;
            }

            Central.Msg.SendMessage(new ItemMessage()
            {
                ReceiverGroup = "TaskPlanning",
                ReceiverName = "TaskPlanning",
                SenderName = ControlName,
                Action = "Refresh",
            });


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

        private void LastDateTimeHours_TextChanged(object sender, TextChangedEventArgs e)
        {
            int hours;
            if(int.TryParse(LastDateTimeHours.Text, out hours))
            {
                if (LastDateTime != null)
                {
                    LastDateTime.EditValue = DateTime.Now.AddHours(hours);
                }
            }
        }
    }
}
