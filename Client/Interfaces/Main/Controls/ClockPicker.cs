using Client.Common;
using DevExpress.Xpf.Editors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using DevExpress.Xpf.Editors.Helpers;

namespace Client.Interfaces.Main
{
    /// <summary>
    /// контрол для редактирования времени
    /// <author>eletskikh_ya</author>
    /// </summary>
    public class ClockPicker : DateEdit
    {
        public ClockPicker()
        {
            this.StyleSettings = new DateEditTimePickerStyleSettings();
            Mask = "HH:mm:ss";
            MaskUseAsDisplayFormat = true;
            EditValueChanged += DatePicker_EditValueChanged;
            this.PopupOpened += DateTimePicker_PopupOpened;
        }

        private void DateTimePicker_PopupOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                var cancelButton = this.GetCancelButton();
                if (cancelButton != null)
                {
                    cancelButton.Content = "Отмена";
                }
            }
            catch (Exception)
            {
            }
        }

        private void DatePicker_EditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            TextChanged?.Invoke(this, null);
        }

        public event TextChangedEventHandler TextChanged;

        public string Text
        {
            get
            {
                string res = string.Empty;

                if (this.EditValue is DateTime dt)
                {
                    res = dt.ToString(Mask);
                }
                else
                {
                    if(this.EditValue!=null)
                    {
                        res = this.EditValue.ToString();
                    }
                }

                return res;
            }

            set
            {

                if (value == string.Empty)
                {
                    this.EditValue = DatePicker.NullDate;
                }
                else
                {
                    string val = DateTime.Now.ToString("dd.MM.yyyy ") + value;
                    if(this.EditValue!=null)
                    {
                        this.EditValue = val.ToDateTime();
                    }                    
                }
            }
        }
    }
}
