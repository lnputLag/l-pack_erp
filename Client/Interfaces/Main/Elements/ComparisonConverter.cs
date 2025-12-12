using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Client.Interfaces.Main.Elements
{
    public class ComparisonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string paramString)
            {
                string[] parts = paramString.Split('|');
                if (parts.Length == 2)
                {
                    if (parts[0] == "ContainsIn")
                    {
                        var t = ContainsIn(value, parts[1]);
                        return ContainsIn(value, parts[1]);
                    }
                    else
                    {
                        if (value is IComparable comparable)
                        {
                            var compareValue = System.Convert.ChangeType(parts[1], value.GetType());
                            switch (parts[0])
                            {
                                case ">": return comparable.CompareTo(compareValue) > 0;
                                case ">=": return comparable.CompareTo(compareValue) >= 0;
                                case "<": return comparable.CompareTo(compareValue) < 0;
                                case "<=": return comparable.CompareTo(compareValue) <= 0;
                                case "==": return comparable.CompareTo(compareValue) == 0;
                                case "!=": return comparable.CompareTo(compareValue) != 0;
                            }
                        }
                    }

                }
                else if (parts.Length == 1)
                {
                    // Для унарных операций (не требующих второго значения)
                    switch (parts[0])
                    {
                        case "IsEmpty": return IsEmpty(value);
                        case "IsNotEmpty": return !IsEmpty(value);
                        case "IsNull": return value == null;
                        case "IsNotNull": return value != null;
                    }
                }
            }
            
            return false;
        }

        private bool IsEmpty(object value)
        {
            if (value == null) return true;
            if (value is string str) return string.IsNullOrEmpty(str);
            if (value is IEnumerable collection) return !collection.Cast<object>().Any();
            return false;
        }

        private bool ContainsIn(object value, string valuesString)
        {
            if (value == null) return false;

            // Разделяем строку значений по запятой
            string[] allowedValues = valuesString.Split(',');

            foreach (string allowedValue in allowedValues)
            {
                try
                {
                    // Пытаемся преобразовать к типу исходного значения
                    var convertedValue = System.Convert.ChangeType(allowedValue.Trim(), value.GetType());
                    if (value.Equals(convertedValue))
                        return true;
                }
                catch
                {
                    // Если преобразование не удалось, сравниваем как строки
                    if (value.ToString() == allowedValue.Trim())
                        return true;
                }
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
