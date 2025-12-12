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

namespace Client.Interfaces.Main
{
    /// <summary>
    /// контрол для редактирования даты (бкз времени)
    /// <author>eletskikh_ya</author>
    /// </summary>
    public class DatePicker : DateEdit
    {
        public DatePicker()
        {
            this.StyleSettings = new DateEditNavigatorStyleSettings();
            EditValueChanged += DatePicker_EditValueChanged;
        }

        public static Nullable<DateTime> NullDate = null;

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

                if(this.EditValue is DateTime dt)
                {
                    res = dt.ToString("dd.MM.yyyy");
                }

                return res;
            }
            set
            {
                if (value == string.Empty)
                {
                    this.EditValue = NullDate;
                }
                else
                {
                    this.EditValue = value.ToDateTime();
                }
            }
        }
    }
}
