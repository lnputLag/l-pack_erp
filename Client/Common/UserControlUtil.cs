using System.Windows;

namespace Client.Common
{
    /// <summary>
    /// Вспомогательный класс для работы с кастомными свойствами полей грида
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>    
    public static class UserControlUtil
    {
        public static string GetMask(DependencyObject obj)
        {
            return (string)obj.GetValue(MaskProperty);
        }

        public static void SetMask(DependencyObject obj, string value)
        {
            obj.SetValue(MaskProperty, value);
            /*
            Central.Dbg($" SetMask ={value}");
            obj.SetValue(MaskProperty, value);
            var tb=obj as TextBox;
            tb.KeyUp+=Tb_KeyUp;
            */            
        }

        private static void Tb_KeyUp(object sender,System.Windows.Input.KeyEventArgs e)
        {
            /*
            if(sender != null)
            {
                var s=sender as System.Windows.DependencyObject;
                string m=UserControlUtil.GetMask(s);
                Central.Dbg($" m={m}");
            }
            */
        }

        public static readonly DependencyProperty MaskProperty =  DependencyProperty.RegisterAttached("Mask", typeof(string), typeof(UserControlUtil), new UIPropertyMetadata(""));



       

       
       

    }
}
