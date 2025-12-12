using Client.Common;
using DevExpress.Utils;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Editors.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Main
{
    /// <summary>
    /// Контрол дял редактирования даты и времени
    /// </summary>
    public class DateTimePicker : DateEdit
    {
        public DateTimePicker()
        {
            Mask = "dd.MM.yyyy HH:mm:ss";
            this.StyleSettings = new DateEditNavigatorWithTimePickerStyleSettings();
            this.PopupOpened += DateTimePicker_PopupOpened;
            EditValueChanged += DateTimePicker_EditValueChanged;
        }

        private void DateTimePicker_EditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            TextChanged?.Invoke(this, null);
        }

        public event TextChangedEventHandler TextChanged;

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

        public string Text
        {
            get
            {
                string res = string.Empty;

                if (this.EditValue is DateTime dt)
                {
                    res = dt.ToString(Mask);
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
                    this.EditValue = value.ToDateTime();
                }
            }
        }

    }
}
