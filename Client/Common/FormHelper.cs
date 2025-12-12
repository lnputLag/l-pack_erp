using Client.Interfaces.Main;
using Client.Interfaces.Production;
using DevExpress.Xpf.Core.Native;
using DevExpress.Xpf.Editors;
using Newtonsoft.Json;
using NPOI.POIFS.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xceed.Wpf.Toolkit;
using static Client.Common.FormExtend;
using static Client.Common.FormHelperField;
using static Client.Interfaces.Main.FormDialog;

namespace Client.Common
{
    /// <summary>
    /// процессор форм
    /// Автоматизирует работу с формами: чтение/запись значений полей, валидация
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    public class FormHelper
    {
        public FormHelper()
        {
            Fields=new List<FormHelperField>();
            FieldFocused = null;
            FieldFocusedIndex = 0;
            Valid =true;
            UseErrorHighLighting=true;
            OnValidate=OnValidateAction;
            Values=new Dictionary<string, string>();
        }

        public List<FormHelperField> Fields { get; set; }
        /// <summary>
        /// активное поле,
        /// на котором стоит фокус ввода,
        /// по умолчанию первое поле в списке
        /// </summary>
        public FormHelperField FieldFocused { get; set; }
        /// <summary>
        /// индекс активного поля в массиве полей
        /// исчисление начинается с 1
        /// </summary>
        public int FieldFocusedIndex { get; set; }
        public bool Valid { get; set; }
        /// <summary>
        /// Список невалидных полей на форме
        /// </summary>
        public string NotValidFileds { get; set; }
        public bool UseErrorHighLighting { get; set; }

        public delegate void OnValidateDelegate(bool valid, string message="");
        public OnValidateDelegate OnValidate;
        public virtual void OnValidateAction(bool valid, string message="")
        {

        }

        public delegate void BeforeGetDelegate(FormHelperField f);
        public BeforeGetDelegate BeforeGet;
        public virtual void BeforeGetAction(FormHelperField f)
        {

        }

        public delegate void BeforeSetDelegate(Dictionary<string,string> v);
        public BeforeSetDelegate BeforeSet;
        public virtual void BeforeSetAction(Dictionary<string,string> v)
        {

        }

        public delegate void AfterSetDelegate(Dictionary<string,string> v);
        public AfterSetDelegate AfterSet;
        public virtual void AfterSetAction(Dictionary<string,string> v)
        {

        }

        public TextBlock StatusControl { get;set;}
        public StackPanel ToolbarControl { get;set;} 

        public void SetFields(List<FormHelperField> fields)
        {
            if(fields.Count > 0)
            {
                Fields=fields;
                InitFields();
            }
        }

        private Dictionary<string,string> Values { get;set;}

        protected void InitFields()
        {
            if(Fields.Count>0)
            {
                //первый проход инициализирующий
                //развесим фильтры
                {
                    int j = 0;
                    foreach(FormHelperField f in Fields)
                    {
                        //только если рабочий контрол определен
                        if(f.Control!=null)
                        {
                            if(!string.IsNullOrEmpty(f.ControlType))
                            {
                                switch(f.ControlType)
                                {
                                    case "TextBox":
                                    {
                                        if (f.Control is TextBox tb)
                                        {
                                            f.Name = tb.Name;
                                        }
                                        else if(f.Control is ClockPicker tp)
                                        {
                                            f.Name = tp.Name;
                                        }
                                        else if (f.Control is Client.Interfaces.Main.DatePicker td)
                                        {
                                            f.Name = td.Name;
                                        }
                                    }
                                    break;

                                    case "CheckBox":
                                    {
                                        var tb = f.Control as CheckBox;
                                        f.Name=tb.Name;
                                    }
                                    break;

                                    case "SelectBox":
                                    {
                                        var tb = f.Control as SelectBox;
                                        f.Name=DataGridUtil.GetName(tb);                                            
                                    }
                                    break;

                                    case "RadioBox":
                                        {
                                            var tb = f.Control as StackPanel;
                                            f.Name = tb.Name;
                                        }
                                        break;
                                }
                            }

                            f.Enabled=true;
                            f.Index=j;

                            switch(f.FieldType)
                            {
                                case FormHelperField.FieldTypeRef.String:
                                {
                                }
                                break;

                                case FormHelperField.FieldTypeRef.Integer:
                                {
                                    AddFilter(f,FormHelperField.FieldFilterRef.DigitOnly);
                                }
                                break;

                                case FormHelperField.FieldTypeRef.Double:
                                {
                                    AddFilter(f,FormHelperField.FieldFilterRef.DigitCommaOnly);
                                }
                                break;
                            }

                            f.Create();
                        }
                        j++;
                    }
                }

                //второй проход 
                //развесим эвенты по набору фильтров
                {
                    int j = 0;
                    foreach(FormHelperField f in Fields)
                    {
                        if(f.Enabled && f.Control!=null)
                        {
                            if(!string.IsNullOrEmpty(f.ControlType))
                            {
                                switch(f.ControlType)
                                {
                                    case "TextBox":
                                    {
                                        if (f.Control is TextBox tb)
                                        {
                                            tb.KeyUp += Tb_KeyUp;
                                            //это временно заблокировано, до разборок с эвентами 2022-08-25_F1
                                            //tb.PreviewKeyDown += Tb_KeyUp;
                                            tb.TextChanged += Tb_TextChanged;
                                            tb.GotFocus += Tb_GotFocus;
                                        }
                                        else if (f.Control is ClockPicker tp)
                                        {

                                        }
                                        else if (f.Control is Client.Interfaces.Main.DatePicker td)
                                        {
                                                td.EditValueChanged += Td_EditValueChanged;

                                        }
                                        else if (f.Control is Client.Interfaces.Main.DateTimePicker ttd)
                                        {

                                        }
                                    }
                                    break;

                                    case "CheckBox":
                                    {
                                        var tb = f.Control as CheckBox;
                                        tb.Click+=Tb_Click;
                                    }
                                    break;

                                    case "SelectBox":
                                    {
                                        var tb = f.Control as SelectBox;
                                        f.Name=tb.Name;
                                        tb.FieldControl=f;
                                        tb.OnSelectItemComplete += Tb_OnSelectItem;
                                    }
                                    break;

                                    case "RadioBox":
                                        {
                                            var tb = f.Control as StackPanel;
                                            if (tb.Children.Count > 0)
                                            {
                                                foreach (var rb in tb.Children)
                                                {
                                                    if (rb is RadioButton radioButton)
                                                    {
                                                        radioButton.Checked += Tb_RadioButton_Click;
                                                    }
                                                }
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }

                {
                    var focusComplete = false;
                    if(!focusComplete)
                    {
                        int j = 0;
                        foreach(FormHelperField f in Fields)
                        {
                            j++;
                            {
                                if(f.Control != null && f.ControlType != "void")
                                {
                                    if(f.First)
                                    {
                                        focusComplete = true;
                                        FieldFocused = f;
                                        FieldFocusedIndex = j;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if(!focusComplete)
                    {
                        int j = 0;
                        foreach(FormHelperField f in Fields)
                        {
                            j++;
                            {
                                if(f.Control != null && f.ControlType != "void")
                                {
                                    focusComplete = true;
                                    FieldFocused = f;
                                    FieldFocusedIndex = j;
                                    break;
                                }
                            }
                        }
                    }

                    if(focusComplete)
                    {
                        if(FieldFocused!=null)
                        {
                            FieldSetFocus(FieldFocused);
                        }
                    }
                                       
                }
            }
        }

        private void Td_EditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            var cb = sender as Client.Interfaces.Main.DatePicker;
            string n = cb.Name;
            if (!string.IsNullOrEmpty(n))
            {
                var f = GetFieldByName(n);
                if (f != null)
                {
                    var v = GetFieldValue(f);
                    f.OnChange?.Invoke(f, v);
                }

                SetStatus("", 0);

            }

        }

        private void Tb_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            string n = tb.Name;
            if(!string.IsNullOrEmpty(n))
            {
                var f = GetFieldByName(n);
                if(f != null)
                {
                    FieldFocused = f;
                }
            }
        }

        /// <summary>
        /// добавляет один фильтр к полю
        /// (предаврительно проверяет фильтр на существование)
        /// </summary>
        /// <param name="n"></param>
        /// <param name="f"></param>
        /// <param name="v"></param>
        public void AddFilter(FormHelperField field,FormHelperField.FieldFilterRef filter,object value = null)
        {
            if(field.Filters == null)
            {
                field.Filters=new Dictionary<FormHelperField.FieldFilterRef,object>();
            }

            if(!field.Filters.ContainsKey(filter))
            {
                field.Filters.Add(filter,null);
            }

            field.Filters[filter]=value;
        }

        private void Tb_KeyUp(object sender,System.Windows.Input.KeyEventArgs e)
        {
            var tb = sender as TextBox;
            string n = tb.Name;
            if(!string.IsNullOrEmpty(n))
            {
                var f = GetFieldByName(n);
                if(f != null)
                {
                    ValidateField(f,true);
                }

                SetStatus("",0);
            }
        }

        private void Tb_TextChanged(object sender,TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            string n = tb.Name;
            if(!string.IsNullOrEmpty(n))
            {
                var f = GetFieldByName(n);
                if (f != null)
                {
                    ValidateField(f, true);
                    var v = GetFieldValue(f);
                    f.OnTextChange?.Invoke(f, v);
                }

                SetStatus("",0);
            }
        }

        private void Tb_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            string n = cb.Name;
            if(!string.IsNullOrEmpty(n))
            {
                var f = GetFieldByName(n);
                if(f != null)
                {
                    var v=GetFieldValue(f);
                    f.OnChange?.Invoke(f,v);
                }

                SetStatus("",0);
                
            }
        }

        private void Tb_RadioButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var cb = sender as RadioButton;
            string n = cb.GroupName;
            if (!string.IsNullOrEmpty(n))
            {
                var clickedRadio = sender as RadioButton;
                //if (clickedRadio == null) return;

                string groupName = clickedRadio.GroupName;
                var parent = VisualTreeHelper.GetParent(clickedRadio);
                while (parent != null && !(parent is StackPanel))
                {
                    parent = VisualTreeHelper.GetParent(parent);
                }

                if (parent is StackPanel panel)
                {
                    foreach (var child in panel.Children)
                    {
                        if (child is RadioButton rb && rb != clickedRadio && rb.GroupName == groupName)
                        {
                            rb.IsChecked = false;
                        }
                    }
                }

                var f = GetFieldByName(groupName);
                if (f != null)
                {
                    var v = GetFieldValue(f);
                    f.OnChange?.Invoke(f, v);
                }

                SetStatus("", 0);

            }
        }

        private bool Tb_OnSelectItem(FormHelperField f, Dictionary<string,string> selectedItem)
        {
            var v = GetFieldValue(f);
            f.OnChange?.Invoke(f, v);
            ValidateField(f, true, true);
            return true;
        }

        public void SetValues(RowDataSet ds)
        {
            CultureInfo culture;

            if(ds!=null)
            {
                if(ds.Values.Count > 0)
                {
                    if(Fields.Count>0)
                    {
                        foreach(FormHelperField f in Fields)
                        {
                            if(f.Enabled)
                            {
                                string p = f.Path;

                                if(!string.IsNullOrEmpty(p))
                                {
                                    if(ds.Values.ContainsKey(p))
                                    {
                                        if(ds.Values[p] != null)
                                        {
                                            SetFieldValue(f,ds.Values[p]);
                                            if(f.AfterSet!=null)
                                            {
                                                f.AfterSet.Invoke(f,ds.Values[p]);
                                            }                                            
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void SetValues(Dictionary<string,string> values)
        {
            Central.Dbg($"FormHelper SetValues");
            CultureInfo culture;

            if(values!=null)
            {
                var s = "";
                
                if(values.Count > 0)
                {
                    if(Fields.Count>0)
                    {
                        foreach(FormHelperField f in Fields)
                        {
                            if(f.Enabled)
                            {
                                string p = f.Path;

                                if(!string.IsNullOrEmpty(p))
                                {
                                    if(values.ContainsKey(p))
                                    {
                                        if(values[p] != null)
                                        {
                                            if(f.BeforeSet != null)
                                            {
                                                f.BeforeSet.Invoke(f, values[p]);
                                            }
                                            SetFieldValue(f,values[p]);
                                            s = $"{s} [{p}]=[{values[p]}]";
                                            if(f.AfterSet!=null)
                                            {
                                                f.AfterSet.Invoke(f,values[p]);
                                            }   
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                //Central.Logger.Trace($"Form.SetValues {s}");
            }
        }

        public void SetValues(ListDataSet ds)
        {
            CultureInfo culture;

            if(ds!=null)
            {
                var values = new Dictionary<string,string>();

                if(ds.Items.Count>0)
                {
                    values=ds.Items.First();
                }

                BeforeSet?.Invoke(values);

                if(values.Count > 0)
                {
                    if(Fields.Count>0)
                    {
                        foreach(FormHelperField f in Fields)
                        {
                            if(f.Enabled)
                            {
                                string p = f.Path;

                                if(!string.IsNullOrEmpty(p))
                                {
                                    if(values.ContainsKey(p))
                                    {
                                        if(values[p] != null)
                                        {
                                            var v = values[p];

                                            if(f.FieldType==FormHelperField.FieldTypeRef.Integer)
                                            {
                                                v=v.ToInt().ToString();
                                            }

                                            SetFieldValue(f,v);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                AfterSet?.Invoke(values);
            }
        }

        public void RenderControls()
        {
            if(Fields.Count > 0)
            {
                foreach(FormHelperField f in Fields)
                {
                    if(f.Enabled)
                    {
                        string p = f.Path;

                        if(f.AfterRender != null)
                        {
                            f.AfterRender.Invoke(f);
                        }
                    }
                }
            }
        }

        public void SetDefaults()
        {
            if(Fields.Count>0)
            {
                foreach(FormHelperField f in Fields)
                {
                    if(f.Enabled)
                    {
                        string p = f.Path;

                        string v="";
                        string m = "0";
                        if(!string.IsNullOrEmpty(f.Default))
                        {
                            v=f.Default;
                            m = "1";
                        }

                        SetFieldValue(f,v);
                        FieldHighlight(f);

                        if(FieldFocused != null)
                        {
                            if(f.Path == FieldFocused.Path)
                            {
                                FieldSetFocus(f);
                            }
                        }
                    }
                }
            }

            if(StatusControl!=null)
            {
                SetStatus("",0);
            }
        }
        
        public Dictionary<string,string> GetDefaults()
        {
            var result = new Dictionary<string,string>();
            
            if(Fields.Count>0)
            {
                foreach(FormHelperField f in Fields)
                {
                    if(f.Enabled)
                    {
                        string p = f.Path;

                        string v="";
                        if(!string.IsNullOrEmpty(f.Default))
                        {
                            v=f.Default;
                        }

                        result.CheckAdd(p,v);
                    }
                }
            }

            return result;
        }

        public Dictionary<string,string> GetValues()
        {
            var result = new Dictionary<string,string>();

            if(Fields.Count>0)
            {
                foreach(FormHelperField f in Fields)
                {
                    if(f.Enabled)
                    {
                        string p = f.Path;

                        if(!string.IsNullOrEmpty(p))
                        {
                            if(!result.ContainsKey(p))
                            {
                                result.Add(p,"");
                            }
                            result[p]=GetFieldValue(f);
                        }

                    }
                }
            }

            return result;
        }

        private void SetValue(string controlName,string value)
        {
            var f = GetFieldByName(controlName);
            if(f != null)
            {
                SetFieldValue(f,value);
            }
        }

        private string GetValue(string controlName)
        {
            string result = "";
            var f = GetFieldByName(controlName);
            if(f != null)
            {
                result = GetFieldValue(f);
            }
            return result;
        }


        // 2024-07-12 balchugov_dv
        // SetValueByPath, GetValueByPath и т.д.
        // концептуально не стоит использовать
        // получить даныне из формы можно только всем скопом, после окончания валидации
        // получение значения одного поля не имеет смысла.

        // FIXME: избавиться от этих функций

        [Obsolete]
        public void SetValueByPath(string path,string value)
        {
            var f = GetFieldByPath(path);
            if(f != null)
            {
                SetFieldValue(f,value);
            }
        }

        [Obsolete]
        public string GetValueByPath(string path)
        {
            string result = "";
            var f = GetFieldByPath(path);
            if(f != null)
            {
                result=GetFieldValue(f);
            }
            return result;
        }

        public bool Validate()
        {
            bool result = true;

            if(Fields.Count>0)
            {
                foreach(FormHelperField f in Fields)
                {
                    if(f.Enabled)
                    {
                        ValidateField(f);
                    }
                }
            }

            CheckValid();
            result=Valid;
            return result;
        }
        
        public FormHelperField GetFieldByName(string n)
        {
            var result = new FormHelperField();

            if(Fields.Count>0)
            {
                foreach(FormHelperField f in Fields)
                {
                    if(f.Name == n)
                    {
                        result=f;
                    }
                }
            }
            return result;
        }

        public FormHelperField GetFieldByPath(string n)
        {
            var result = new FormHelperField();

            if(Fields.Count>0)
            {
                foreach(FormHelperField f in Fields)
                {
                    if(f.Path == n)
                    {
                        result=f;
                    }
                }
            }
            return result;
        }

        //FIXME: эту функцию нужно перенести в Extensions

        /// <summary>
        /// функцмя удаляет из строки разделители тысяч, 
        /// но оставляет последнюю запятую которая является разделителме дробной части
        /// </summary>
        /// <param name="value">строка число с разделителями дробной части и возможными разделителями тысяч, миллионов и т.д.</param>
        /// <returns>возвращает строку без запятхы разделителей тысяч</returns>
        /// <author>eletskikh_ya</author>
        [Obsolete]
        public string RemoveThousandSeparators(string value)
        {
            // получаем индекс последней запятой
            int sepIndex = value.LastIndexOf(',');

            if (sepIndex > 0) // если она есть
            {
                sepIndex = value.Length - sepIndex - 1;
                // выделяем дробную часть
                string mantissa = value.Substring(value.Length - sepIndex);
                // выделяем целую часть
                string number = value.Substring(0, value.Length - (sepIndex + 1));
                // формируем результат путем удаление из целой части запятых, и добавлением дробной части после запятой
                value = number.Replace(",", "") + "," + mantissa;
            }

            return value;
        }

        public void ProcessExtInput(string v)
        {
            if(FieldFocused != null)
            {
                switch(v)
                {
                    case "BACK_SPACE":
                        {
                            var v0 = GetFieldValue(FieldFocused);
                            if(v0.Length > 0)
                            {
                                v0 = v0.Substring(0, (v0.Length - 1));
                            }
                            SetFieldValue(FieldFocused, v0);
                            MoveCaret(FieldFocused, 9);
                        }
                        break;

                    default:
                        {
                            var v0 = GetFieldValue(FieldFocused);
                            v0 = $"{v0}{v}";
                            SetFieldValue(FieldFocused, v0);
                            MoveCaret(FieldFocused,9);
                        }
                        break;
                }                
            }
        }

        private void MoveCaret(FormHelperField f, int d=0)
        {
            switch(f.ControlType.ToLower())
            {
                case "textbox":
                    {
                        var tb = f.Control as TextBox;
                        if(tb != null)
                        {
                            var i = tb.CaretIndex;
                            var len=tb.Text.Length;

                            switch(d)
                            {
                                case -1:
                                    i--;
                                    break;

                                case 1:
                                    i++;
                                    break;

                                case 9:
                                    i=len;
                                    break;
                            }

                            if(i < 0)
                            {
                                i = 0;
                            }

                            if(i > len )
                            {
                                i = len-1;
                            }

                            tb.CaretIndex=i;
                        }
                    }
                    break;
            }
        }

        private void SetFieldValue(FormHelperField f,string v)
        {
            
            switch(f.ControlType.ToLower())
            {
                case "textbox":
                {
                        if (f.Control is TextBox tb)
                        {
                            if (tb != null)
                            {
                                switch (f.FieldType)
                                {
                                    case FormHelperField.FieldTypeRef.String:
                                        {
                                            tb.Text = (v.ToString());
                                        }
                                        break;

                                    case FormHelperField.FieldTypeRef.Integer:
                                        {
                                            var emptyMode = true;
                                            if (f.Options.IndexOf("zeronoempty") > -1)
                                            {
                                                emptyMode = false;
                                            }

                                            if (emptyMode)
                                            {
                                                if (v.ToInt() == 0)
                                                {
                                                    tb.Text = "";
                                                }
                                                else
                                                {
                                                    tb.Text = (v.ToInt()).ToString();
                                                }
                                            }
                                            else
                                            {
                                                tb.Text = (v.ToInt()).ToString();
                                            }
                                        }
                                        break;

                                    case FormHelperField.FieldTypeRef.Double:
                                        {
                                            var emptyMode = true;

                                            var subFormat = "N";
                                            if (!string.IsNullOrEmpty(f.Format))
                                            {
                                                subFormat = f.Format;
                                            }
                                            var format = "{0:" + subFormat + "}";
                                            var dv = v.ToDouble();

                                            // сделал как и в integer при 0 что бы было пустое значение eletskikh_ya
                                            if (dv == 0.0 && emptyMode)
                                            {
                                                tb.Text = "";
                                            }
                                            else
                                            {
                                                var s = string.Format(CultureInfo.InvariantCulture, format, dv);
                                                s = s.Replace(".", ",");
                                                s = RemoveThousandSeparators(s);
                                                tb.Text = s;
                                            }
                                        }
                                        break;

                                    case FormHelperField.FieldTypeRef.DateTime:
                                        {
                                            string frm = "dd.MM.yyyy";
                                            if (!f.Format.IsNullOrEmpty())
                                            {
                                                frm = f.Format;
                                            }

                                            var dv = v.ToDateTime(frm);
                                            if (DateTime.Compare(dv, DateTime.MinValue) > 0)
                                            {
                                                tb.Text = dv.ToString(frm);
                                            }
                                            else
                                            {
                                                tb.Text = "";
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                        else if (f.Control is Client.Interfaces.Main.DatePicker tp)
                        {
                            tp.Text = v;
                        }
                        else if (f.Control is Client.Interfaces.Main.ClockPicker tc)
                        {
                            tc.Text = v;
                        }
                        else if (f.Control is Client.Interfaces.Main.DateTimePicker dtc)
                        {
                            dtc.Text = v;
                        }
                        else if(f.Control is DevExpress.Xpf.Editors.TextEdit te)
                        {
                            te.Text = v;
                        }
                }
                break;

                case "checkbox":
                {
                    var tb = f.Control as CheckBox;
                    if (tb != null)
                    {
                        var c = false;
                        c = v.ToBool();
                        tb.IsChecked = c;

                        f.OnChange?.Invoke(f, v);
                    }
                }
                break;

                case "datetime":
                {
                    if(f.Control.GetType() == typeof(Interfaces.Main.DateTimePicker))
                    {
                        var tb = f.Control as Interfaces.Main.DateTimePicker;
                        if(tb != null)
                        {
                            if(!v.IsNullOrEmpty())
                            {
                                var dt = v.ToDateTime();
                                var s = dt.ToString("dd.MM.yyyy HH:mm:ss");
                                tb.Text = s;
                            }
                            f.OnChange?.Invoke(f, v);
                        }
                    }
                    else
                    {
                        var tb = f.Control as DateTimeUpDown;
                        if(tb != null)
                        {
                            var c = false;

                            DateTime t = new DateTime();
                            if(DateTime.TryParse(v, out t))
                            {
                                tb.Value = t;
                            }
                            f.OnChange?.Invoke(f, v);
                        }
                    }
                }
                break;

                case "radiobox":
                {
                    var container = f.Control as StackPanel;
                    if(container != null)
                    {
                        if(container.Children.Count>0)
                         {
                            foreach(var el in container.Children)
                            {
                                if(el.GetType()==typeof(RadioButton))
                                {
                                    var rb=(RadioButton)el;

                                    if (!v.IsNullOrEmpty())
                                    {
                                            if (rb.Tag != null && !string.IsNullOrEmpty(rb.Tag.ToString()))
                                            {
                                                if (rb.Tag.ToString() == v.ToString())
                                                {
                                                    rb.IsChecked = true;
                                                }
                                                else
                                                {
                                                    rb.IsChecked = false;
                                                }
                                            }
                                            else
                                            {
                                                if (rb.Content.ToString() == v.ToString())
                                                {
                                                    rb.IsChecked = true;
                                                }
                                                else
                                                {
                                                    rb.IsChecked = false;
                                                }
                                            }
                                        }
                                }
                                else if(el.GetType()==typeof(StackPanel))
                                {
                                    var sp=(StackPanel)el;

                                    foreach(var el2 in sp.Children)
                                    {
                                        if(el2.GetType()==typeof(RadioButton))
                                        {
                                            var rb=(RadioButton)el2;

                                            if(!string.IsNullOrEmpty(rb.Tag.ToString()))
                                            {
                                                if(rb.Tag.ToString()==v.ToString())
                                                {
                                                    rb.IsChecked=true;
                                                }
                                                else
                                                {
                                                    rb.IsChecked=false;
                                                }
                                            }
                                            else
                                            {
                                                if(rb.Content.ToString()==v.ToString())
                                                {
                                                    rb.IsChecked=true;
                                                }
                                                else
                                                {
                                                    rb.IsChecked=false;
                                                }
                                            } 
                                        }

                                    }
                                }
                                
                            }
                        }
                    }
                }
                break;

                case "selectbox":
                {
                    var sb = f.Control as SelectBox;

                    switch(f.FieldType)
                    {
                        case FormHelperField.FieldTypeRef.Integer:
                        {
                            v=v.ToInt().ToString();
                        }
                        break;
                        
                        default:
                        {
                        }
                        break;
                    }

                    if(sb.Items.Count>0)
                    {
                        foreach(KeyValuePair<string,string> item in sb.Items)
                        {
                            if(item.Key==v)
                            {
                                sb.SetSelectedItem(item);
                            }
                        }
                    }
                    else if(sb.GridDataSet!=null)
                    {
                        if(sb.GridDataSet.Items!=null)
                        {
                            if(sb.GridDataSet.Items.Count>0)
                            {
                                foreach(Dictionary<string,string> item in sb.GridDataSet.Items)
                                {
                                    if(!sb.GridPrimaryKey.IsNullOrEmpty())
                                    {
                                        var v1 = item.CheckGet(sb.GridPrimaryKey);
                                        if(v1.ToInt() == v.ToInt())
                                        {
                                            sb.SetSelectedItem(item);
                                        }
                                    }
                                    else
                                    {
                                        if(item.CheckGet("ID") == v)
                                        {
                                            sb.SetSelectedItem(item);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    
                }
                break;

                case "dateedit":
                    {
                        var tb = f.Control as DateEdit;
                        if (tb != null)
                        {
                            var c = false;

                            DateTime t = new DateTime();
                            if (DateTime.TryParse(v, out t))
                            {
                                tb.EditValue = t;
                            }


                            f.OnChange?.Invoke(f, v);
                        }
                    }

                    break;

                case "textedit":
                    {
                        if (f.Control is TextEdit te)
                        {
                            if (te != null)
                            {
                                switch (f.FieldType)
                                {
                                    case FormHelperField.FieldTypeRef.String:
                                        {
                                            te.Text = (v.ToString());
                                        }
                                        break;
                                }
                            }
                        }
                    }
                    break;

                case "void":
                default:
                {
                    Values.CheckAdd(f.Path,v);
                }
                break;
            }

            f.ActualValue = v; 
            f.AfterSet?.Invoke(f,v);

        }

        private string GetFieldValue(FormHelperField f)
        {
            string result = "";

            f.BeforeGet?.Invoke(f);

            switch(f.ControlType.ToLower())
            {
                case "textbox":
                {
                        if (f.Control is TextBox tb)
                        {

                            if (tb != null)
                            {
                                switch (f.FieldType)
                                {
                                    case FormHelperField.FieldTypeRef.String:
                                        {
                                            result = tb.Text;
                                        }
                                        break;

                                    case FormHelperField.FieldTypeRef.Integer:
                                        {
                                            result = tb.Text;
                                        }
                                        break;

                                    case FormHelperField.FieldTypeRef.Double:
                                        {
                                            var sv = tb.Text;

                                            var subFormat = "N";
                                            if (!string.IsNullOrEmpty(f.Format))
                                            {
                                                subFormat = f.Format;
                                            }
                                            var format = "{0:" + subFormat + "}";
                                            var dv = sv.ToDouble();
                                            var s = string.Format(CultureInfo.InvariantCulture, format, dv);
                                            s = s.Replace(".", ",");
                                            s = RemoveThousandSeparators(s);
                                            result = s;
                                        }
                                        break;

                                    case FormHelperField.FieldTypeRef.DateTime:
                                        {
                                            result = tb.Text;
                                            var dateString = tb.Text;
                                            if (!string.IsNullOrEmpty(dateString))
                                            {
                                                var date = dateString.ToDateTime();
                                                var format = "dd.MM.yyyy";
                                                if (!string.IsNullOrEmpty(f.Format))
                                                {
                                                    format = f.Format;
                                                }
                                                result = date.ToString(format);
                                            }

                                        }
                                        break;

                                }
                            }
                        }
                        else if(f.Control is ClockPicker tp)
                        {
                            result = tp.Text;
                        }
                        else if(f.Control is Client.Interfaces.Main.DatePicker dp)
                        {
                            result = dp.Text;
                        }
                        else if (f.Control is Client.Interfaces.Main.DateTimePicker dtp)
                        {
                            result = dtp.Text;
                        }

                }
                break;

                case "checkbox":
                {
                    var tb = f.Control as CheckBox;

                    if(tb!=null)
                    {
                        var c=false;
                        if((bool)tb.IsChecked)
                        {
                            result="1";
                        }
                        else
                        {
                            result="0";
                        }
                    }
                        
                }
                break;

                case "datetime":
                {
                    var tb = f.Control as DateTimeUpDown;
                    if (tb != null)
                    {
                        if (tb.Value != null)
                        {
                            if (!string.IsNullOrEmpty(f.Format))
                            {
                                DateTime t = (DateTime)tb.Value;
                                result = t.ToString(f.Format);
                            }
                            else
                            {
                                result = tb.Value.ToString();
                            }
                        }
                    }
                }
                break;

                case "dateedit":
                {
                    switch (f.FieldType)
                    {
                        case FieldTypeRef.Date:
                            var td = f.Control as Client.Interfaces.Main.DatePicker;
                            if (td != null)
                            {
                                result = td.Text;
                            }
                            break;
                        case FieldTypeRef.Time:
                            var tp = f.Control as Client.Interfaces.Main.ClockPicker;
                            if (tp != null)
                            {
                                result = tp.Text;
                            }
                            break;
                        case FieldTypeRef.DateTime:
                            var tm = f.Control as Client.Interfaces.Main.DateTimePicker;
                            if (tm != null)
                            {
                                result = tm.Text;
                            }
                            break;
                    }

                    /*
                    Старый вариант

                    var tb = f.Control as DateEdit;
                    if (tb != null)
                    {
                        if (tb.EditValue != null)
                        {
                            if (!string.IsNullOrEmpty(f.Format))
                            {
                                DateTime t = (DateTime)tb.EditValue;
                                result = t.ToString(f.Format);
                            }
                            else
                            {
                                result = tb.EditValue.ToString();
                            }
                        }
                    }
                    */
                }
                break;

                case "radiobox":
                {
                    var container = f.Control as StackPanel;
                    if(container != null)
                    {
                        if(container.Children.Count>0)
                        {
                            foreach(var el in container.Children)
                            {
                                if(el.GetType()==typeof(RadioButton))
                                {
                                    var rb=(RadioButton)el;

                                    if((bool)rb.IsChecked)
                                    {
                                        if(rb.Tag!=null && !string.IsNullOrEmpty(rb.Tag.ToString()))
                                        {
                                            result=rb.Tag.ToString();
                                        }
                                        else
                                        {
                                            result=rb.Content.ToString();
                                        }                                        
                                    }
                                }
                                else if(el.GetType()==typeof(StackPanel))
                                {
                                    var sp=(StackPanel)el;

                                    foreach(var el2 in sp.Children)
                                    {
                                        if(el2.GetType()==typeof(RadioButton))
                                        {
                                            var rb=(RadioButton)el2;

                                            if((bool)rb.IsChecked)
                                            {
                                                if(rb.Tag!=null && !string.IsNullOrEmpty(rb.Tag.ToString()))
                                                {
                                                    result=rb.Tag.ToString();
                                                }
                                                else
                                                {
                                                    result=rb.Content.ToString();
                                                }     
                                            }
                                        }

                                    }
                                }

                            }
                        }
                    }
                }
                break;

                case "selectbox":
                {
                    var tb = f.Control as SelectBox;

                    if(tb!=null)
                    {
                        switch(f.FieldType)
                        {
                            default:
                                {
                                    result=tb.SelectedItem.Key;
                                }
                                break;
                        }
                    }

                      
                }
                break;

                case "void":
                default:
                {
                    result=Values.CheckGet(f.Path);
                }
                break;

            }

            return result;
        }

        public bool ValidateField(FormHelperField f, bool checkValid = false, bool checkFilters = true)
        {
            bool result = true;

            if(f.Enabled && f.Control!=null)
            {
                var valid = true;
                var errorMessage = "";

                // Запуск проверки по фильтрам для Field
                if (checkFilters)
                {
                    //просмотрим все фильтры
                    if (f.Filters.Count > 0)
                    {
                        foreach (KeyValuePair<FormHelperField.FieldFilterRef, object> filter in f.Filters)
                        {
                            bool bindKeyUp = true;

                            switch (filter.Key)
                            {

                                case FormHelperField.FieldFilterRef.DigitOnly:
                                    {
                                        /*
                                            только цифры
                                            Получаем контент из контрола.
                                            Перебираем все символы в контенте, пропускаем только разрешенные символы.
                                            Отфильтрованный набор возвращаем назад.
                                         */
                                        var tb = f.Control as TextBox;

                                        if (tb != null)
                                        {
                                            string text = (string)tb.Text;
                                            int caretIndex = tb.CaretIndex;

                                            string text2 = "";
                                            bool replacement = false;
                                            int commaCounter = 0;
                                            foreach (Char c in text.ToCharArray())
                                            {
                                                if (Char.IsDigit(c))
                                                {
                                                    text2 = $"{text2}{c}";
                                                }
                                                else
                                                {
                                                    replacement = true;
                                                }

                                                if (c == '-')
                                                {
                                                    commaCounter++;
                                                    if (commaCounter == 1)
                                                    {
                                                        text2 = $"{text2}{c}";

                                                        caretIndex++;
                                                    }
                                                }
                                            }

                                            text = text2;
                                            if (replacement)
                                            {
                                                caretIndex--;
                                            }

                                            tb.Text = text;
                                            if (caretIndex >= 0)
                                            {
                                                tb.CaretIndex = caretIndex;
                                            }
                                        }
                                    }
                                    break;


                                case FormHelperField.FieldFilterRef.LatinOnly:
                                    {
                                        /*
                                            только латиница
                                            Получаем контент из контрола.
                                            Перебираем все символы в контенте, пропускаем только разрешенные символы.
                                            Отфильтрованный набор возвращаем назад.
                                         */
                                        var tb = f.Control as TextBox;

                                        if (tb != null)
                                        {
                                            string text = (string)tb.Text;
                                            int caretIndex = tb.CaretIndex;

                                            string text2 = "";
                                            bool replacement = false;
                                            foreach (Char c in text.ToCharArray())
                                            {
                                                if (!c.IsCyrillic())
                                                {
                                                    text2 = $"{text2}{c}";
                                                }
                                                else
                                                {
                                                    replacement = true;
                                                }
                                            }
                                            text = text2;
                                            if (replacement)
                                            {
                                                caretIndex--;
                                            }

                                            tb.Text = text;
                                            if (caretIndex >= 0)
                                            {
                                                tb.CaretIndex = caretIndex;
                                            }
                                        }
                                    }
                                    break;

                                case FormHelperField.FieldFilterRef.DigitCommaOnly:
                                    {
                                        /*
                                            только цифры и символ ","
                                            Получаем контент из контрола.
                                            Перебираем все символы в контенте, пропускаем только разрешенные символы.
                                            Отфильтрованный набор возвращаем назад.
                                         */
                                        var tb = f.Control as TextBox;
                                        if (tb != null)
                                        {
                                            string text = (string)tb.Text;
                                            int caretIndex = tb.CaretIndex;

                                            string text2 = "";
                                            bool replacement = false;
                                            int commaCounter = 0;
                                            int commaCounter2 = 0;
                                            foreach (Char c in text.ToCharArray())
                                            {
                                                if (Char.IsDigit(c))
                                                {
                                                    text2 = $"{text2}{c}";
                                                }
                                                else
                                                {
                                                    replacement = true;
                                                }
                                                if (c == ',')
                                                {
                                                    commaCounter++;
                                                    if (commaCounter == 1)
                                                    {
                                                        text2 = $"{text2}{c}";

                                                        caretIndex++;
                                                    }
                                                }
                                                else if (c == '-')
                                                {
                                                    commaCounter2++;
                                                    if (commaCounter2 == 1)
                                                    {
                                                        text2 = $"{text2}{c}";

                                                        caretIndex++;
                                                    }
                                                }
                                            }

                                            text = text2;
                                            if (replacement)
                                            {
                                                caretIndex--;
                                            }

                                            tb.Text = text;
                                            if (caretIndex >= 0)
                                            {
                                                tb.CaretIndex = caretIndex;
                                            }

                                        }
                                    }
                                    break;

                                case FormHelperField.FieldFilterRef.AlphaOnly:
                                    {
                                        /*
                                            Получаем контент из контрола.
                                            Перебираем все символы в контенте, пропускаем только разрешенные символы.
                                            Отфильтрованный набор возвращаем назад.
                                         */
                                        var tb = f.Control as TextBox;
                                        if (tb != null)
                                        {
                                            string text = (string)tb.Text;
                                            int caretIndex = tb.CaretIndex;

                                            string text2 = "";
                                            bool replacement = false;
                                            foreach (Char c in text.ToCharArray())
                                            {
                                                if (!Char.IsDigit(c))
                                                {
                                                    text2 = $"{text2}{c}";
                                                }
                                                else
                                                {
                                                    replacement = true;
                                                }
                                            }
                                            text = text2;
                                            if (replacement)
                                            {
                                                caretIndex--;
                                            }

                                            tb.Text = text;
                                            if (caretIndex >= 0)
                                            {
                                                tb.CaretIndex = caretIndex;
                                            }
                                        }
                                    }
                                    break;

                                case FormHelperField.FieldFilterRef.MaxLen:
                                    {
                                        /*
                                           Получаем контент из контрола.
                                           Если длина строки более допустимой, подрезаем ее и возвращаем назад.
                                        */
                                        var tb = f.Control as TextBox;
                                        if (tb != null)
                                        {
                                            string text = (string)tb.Text;
                                            int caretIndex = tb.CaretIndex;

                                            if (filter.Value != null)
                                            {
                                                var vf = filter.Value.ToString().ToInt();
                                                if (text.Length > vf)
                                                {
                                                    text = text.Substring(0, vf);
                                                }
                                            }

                                            tb.Text = text;
                                            if (caretIndex >= 0)
                                            {
                                                tb.CaretIndex = caretIndex;
                                            }
                                        }
                                    }
                                    break;

                                case FormHelperField.FieldFilterRef.MinLen:
                                    {
                                        /*
                                           Получаем контент из контрола.
                                           Если длина строки менее необходимой сообщим об ошибке.
                                        */
                                        var tb = f.Control as TextBox;
                                        if (tb != null)
                                        {
                                            string text = (string)tb.Text;
                                            int caretIndex = tb.CaretIndex;

                                            if (filter.Value != null)
                                            {
                                                var vf = filter.Value.ToString().ToInt();
                                                if (text.Length < vf)
                                                {
                                                    valid = false;
                                                    errorMessage = $"Не менее {vf} символов";
                                                }
                                            }
                                        }

                                    }
                                    break;

                                case FormHelperField.FieldFilterRef.MaxValue:
                                    if (filter.Value != null)
                                    {
                                        var v = GetFieldValue(f);
                                        var vx = v.ToDouble();
                                        var vf = filter.Value.ToString().ToDouble();
                                        if (vx > vf)
                                        {
                                            valid = false;
                                            errorMessage = $"Не более {vf}";
                                        }
                                    }
                                    break;

                                case FormHelperField.FieldFilterRef.MinValue:
                                    if (filter.Value != null)
                                    {
                                        var v = GetFieldValue(f);
                                        var vx = v.ToDouble();
                                        var vf = filter.Value.ToString().ToDouble();
                                        if (vx < vf)
                                        {
                                            valid = false;
                                            errorMessage = $"Не менее {vf}";
                                        }
                                    }
                                    break;

                                case FormHelperField.FieldFilterRef.Required:
                                    {
                                        /*
                                           Получаем контент из контрола.
                                           Если контент пустой, то валидация не проходит
                                        */
                                        var v = GetFieldValue(f);

                                        if (f.FieldType == FormHelperField.FieldTypeRef.Integer)
                                        {
                                            if (v.ToInt() == 0)
                                            {
                                                valid = false;
                                                errorMessage = $"Должно быть более нуля";
                                            }
                                        }
                                        else
                                        {
                                            if (string.IsNullOrEmpty(v))
                                            {
                                                valid = false;
                                                errorMessage = $"Это обязательное поле";
                                            }
                                        }

                                    }
                                    break;

                                case FormHelperField.FieldFilterRef.IsNotZero:
                                    {
                                        /*
                                           Получаем контент из контрола.
                                           Если контент =0, то валидация не проходит
                                        */
                                        var v = GetFieldValue(f);
                                        if (v.ToInt() == 0)
                                        {
                                            valid = false;
                                            errorMessage = $"Должно быть более нуля";
                                        }
                                    }
                                    break;

                                case FormHelperField.FieldFilterRef.AlphaDigitOnly:
                                    {
                                        /*
                                            только цифры и буквы 
                                         */
                                        var tb = f.Control as TextBox;
                                        if (tb != null)
                                        {
                                            string text = (string)tb.Text;
                                            int caretIndex = tb.CaretIndex;

                                            string text2 = "";
                                            bool replacement = false;
                                            foreach (Char c in text.ToCharArray())
                                            {
                                                if (Char.IsLetterOrDigit(c))
                                                {
                                                    if (Char.IsDigit(c))
                                                    {
                                                        text2 = $"{text2}{c}";
                                                    }
                                                    else if (Char.IsLetter(c))
                                                    {
                                                        text2 = $"{text2}{c}";
                                                    }
                                                    else
                                                    {
                                                        replacement = true;
                                                    }
                                                }
                                                else
                                                {
                                                    replacement = true;
                                                }
                                            }
                                            text = text2;
                                            if (replacement)
                                            {
                                                caretIndex--;
                                            }

                                            tb.Text = text;
                                            if (caretIndex >= 0)
                                            {
                                                tb.CaretIndex = caretIndex;
                                            }
                                        }
                                    }
                                    break;

                                case FormHelperField.FieldFilterRef.NoLatin:
                                    {
                                        /*
                                            отсекает латиницу
                                         */
                                        var tb = f.Control as TextBox;
                                        if (tb != null)
                                        {
                                            string text = (string)tb.Text;
                                            int caretIndex = tb.CaretIndex;

                                            string text2 = "";
                                            bool replacement = false;
                                            text = text.ToLower();
                                            foreach (Char c in text.ToCharArray())
                                            {
                                                if (c.IsLatin())
                                                {
                                                    replacement = true;
                                                }
                                                else
                                                {
                                                    text2 = $"{text2}{c}";
                                                }
                                            }
                                            text = text2;
                                            if (replacement)
                                            {
                                                caretIndex--;
                                            }

                                            tb.Text = text;
                                            if (caretIndex >= 0)
                                            {
                                                tb.CaretIndex = caretIndex;
                                            }
                                        }
                                    }
                                    break;

                                case FormHelperField.FieldFilterRef.NoCyrillic:
                                    {
                                        /*
                                            отсекает кириллицу
                                         */
                                        var tb = f.Control as TextBox;
                                        if (tb != null)
                                        {
                                            string text = (string)tb.Text;
                                            int caretIndex = tb.CaretIndex;

                                            string text2 = "";
                                            bool replacement = false;
                                            text = text.ToLower();
                                            foreach (Char c in text.ToCharArray())
                                            {
                                                if (c.IsLatin())
                                                {
                                                    replacement = true;
                                                }
                                                else
                                                {
                                                    text2 = $"{text2}{c}";
                                                }
                                            }
                                            text = text2;
                                            if (replacement)
                                            {
                                                caretIndex--;
                                            }

                                            tb.Text = text;
                                            if (caretIndex >= 0)
                                            {
                                                tb.CaretIndex = caretIndex;
                                            }
                                        }
                                    }
                                    break;

                                case FormHelperField.FieldFilterRef.ToUpperCase:
                                    {
                                        /*
                                            буквы в верхний регистр
                                         */
                                        var tb = f.Control as TextBox;
                                        if (tb != null)
                                        {
                                            string text = (string)tb.Text;
                                            int caretIndex = tb.CaretIndex;

                                            string text2 = "";
                                            foreach (Char c in text.ToCharArray())
                                            {
                                                if (Char.IsLetter(c))
                                                {
                                                    text2 = text2 + System.Char.ToUpper(c);
                                                }
                                                else
                                                {
                                                    text2 = $"{text2}{c}";
                                                }
                                            }
                                            text = text2;

                                            tb.Text = text;
                                            if (caretIndex >= 0)
                                            {
                                                tb.CaretIndex = caretIndex;
                                            }
                                        }
                                    }
                                    break;

                                case FormHelperField.FieldFilterRef.ToLowerCase:
                                    {
                                        /*
                                            буквы в верхний регистр
                                         */
                                        var tb = f.Control as TextBox;
                                        if (tb != null)
                                        {
                                            string text = (string)tb.Text;
                                            int caretIndex = tb.CaretIndex;

                                            string text2 = "";
                                            foreach (Char c in text.ToCharArray())
                                            {
                                                if (Char.IsLetter(c))
                                                {
                                                    text2 = text2 + System.Char.ToLower(c);
                                                }
                                                else
                                                {
                                                    text2 = $"{text2}{c}";
                                                }
                                            }
                                            text = text2;

                                            tb.Text = text;
                                            if (caretIndex >= 0)
                                            {
                                                tb.CaretIndex = caretIndex;
                                            }
                                        }
                                    }
                                    break;

                                case FormHelperField.FieldFilterRef.CarNumber:
                                    {
                                        /*
                                            только цифры, буквы и минус
                                         */
                                        var tb = f.Control as TextBox;
                                        if (tb != null)
                                        {
                                            string text = (string)tb.Text;
                                            int caretIndex = tb.CaretIndex;

                                            string text2 = "";
                                            bool replacement = false;
                                            foreach (Char c in text.ToCharArray())
                                            {
                                                //if (Char.IsLetterOrDigit(c))
                                                //{
                                                if (Char.IsDigit(c))
                                                {
                                                    text2 = $"{text2}{c}";
                                                }
                                                else if (Char.IsLetter(c))
                                                {
                                                    text2 = $"{text2}{c}";
                                                }
                                                else if (c == '-')
                                                {
                                                    text2 = $"{text2}{c}";
                                                }
                                                else
                                                {
                                                    replacement = true;
                                                }
                                                //}
                                                //else
                                                //{
                                                //    replacement = true;
                                                //}
                                            }
                                            text = text2;
                                            if (replacement)
                                            {
                                                caretIndex--;
                                            }

                                            tb.Text = text;
                                            if (caretIndex >= 0)
                                            {
                                                tb.CaretIndex = caretIndex;
                                            }
                                        }
                                    }
                                    break;

                                case FormHelperField.FieldFilterRef.DeniedCharacters:
                                {
                                    var tb = f.Control as TextBox;
                                    if (tb != null)
                                    {
                                        string text = (string)tb.Text;
                                        int caretIndex = tb.CaretIndex;

                                        if (filter.Value != null)
                                        {
                                            var vf = filter.Value.ToString();
                                            if (text != "" && vf.Contains(text.Last().ToString()))
                                            {
                                                text = text.Substring(0, text.Length - 1);
                                            }
                                        }

                                        tb.Text = text;
                                        if (caretIndex >= 0)
                                        {
                                            tb.CaretIndex = caretIndex;
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }

                // Запуск валидации для Field
                {
                    f.ValidateMessage="";
                    f.ValidateResult=true;
                    f.ValidateProcessed=false;
                    var v = GetFieldValue(f);
                    f.Validate?.Invoke(f, v);                    
                    
                    if(f.ValidateProcessed)
                    {
                        if(f.ValidateResult == false)
                        {
                            valid=false;
                            if(!f.ValidateMessage.IsNullOrEmpty())
                            {
                                errorMessage=f.ValidateMessage;
                            }                            
                        }
                    }                              
                }


                //подсветка невалидных полей
                if(UseErrorHighLighting)
                {
                    FieldHighlight(f, valid, errorMessage);
                }


                f.Valid=valid;

                // Запуск валидации для FormHelper 
                if (checkValid)
                {
                    CheckValid();
                }

                Central.Dbg($"Validate field:[{f.Name}] =>[{valid}]");
            }

            return result;
        }

        private void FieldSetFocus(FormHelperField f)
        {
            switch(f.ControlType)
            {
                case "TextBox":
                {
                    var tb = f.Control as TextBox;
                    if(tb!=null)
                    {
                        //установка курсора в конец строки                    
                        if(f.CaretIndex > 0)
                        {
                            //tb.CaretIndex=f.CaretIndex;
                        }

                        //фокус ввода на первое поле
                        //if(f.First)
                        {
                            tb.Focus();
                        }
                    }
                }
                break;

                case "CheckBox":
                {
                    var tb = f.Control as CheckBox;
                    if(tb != null)
                    {
                    }
                }
                break;

                case "SelectBox":
                {
                    var tb = f.Control as SelectBox;
                    if(tb != null)
                    {
                    }
                }
                break;
            }
        }

        private void FieldHighlight(FormHelperField f, bool valid=true, string errorMessage="")
        {
            switch (f.ControlType)
            {
                case "TextBox":
                    {
                        if( f.Control is System.Windows.Controls.Control c)
                        {
                            var color = "#ffcccccc";
                            if(!valid)
                            {
                                color = "#ffee0000";
                            }

                            var bc = new BrushConverter();
                            var brush = (Brush)bc.ConvertFrom(color);
                            c.BorderBrush = brush;

                            c.ToolTip = errorMessage;
                        }
                    }
                    break;

                case "CheckBox":
                    {
                        var tb = f.Control as CheckBox;
                        if(tb!=null)
                        {
                            var color = "#ffcccccc";
                            if(!valid)
                            {
                                color = "#ffee0000";
                            }

                            var bc = new BrushConverter();
                            var brush = (Brush)bc.ConvertFrom(color);
                            tb.BorderBrush = brush;

                            tb.ToolTip = errorMessage;
                        }
                    }
                    break;

                case "SelectBox":
                    {
                        var tb = f.Control as SelectBox;
                        if(tb != null)
                        {
                            var color = "#ffcccccc";
                            if(!valid)
                            {
                                color = "#ffee0000";
                            }

                            var bc = new BrushConverter();
                            var brush = (Brush)bc.ConvertFrom(color);
                            tb.BorderBrush = brush;

                            tb.ToolTip = errorMessage;
                        }
                    }
                    break;

                case "DateTime":
                    {
                        var tb = f.Control as DateTimeUpDown;
                        if(tb != null)
                        {
                            var color = "#ffcccccc";
                            if(!valid)
                            {
                                color = "#ffee0000";
                            }

                            var bc = new BrushConverter();
                            var brush = (Brush)bc.ConvertFrom(color);
                            tb.BorderBrush = brush;

                            tb.ToolTip = errorMessage;
                        }
                        
                    }
                    break;
            }
        }

        private void CheckValid()
        {
            NotValidFileds = "";
            Valid =true;

            if(Fields.Count>0)
            {
                foreach(FormHelperField f in Fields)
                {
                    if(f.Enabled)
                    {
                        Valid=Valid && f.Valid;
                        if(f.Valid == false && !NotValidFileds.Contains(f.Name))
                        {
                            NotValidFileds = NotValidFileds + f.Name + ", ";
                        }
                    }
                }
            }

            OnValidate?.Invoke(Valid);
        }

        public void SetStatus(bool valid, string message)
        {
            OnValidate?.Invoke(valid,message);
        }

        public void RemoveFilter(string path, FieldFilterRef filter)
        {
            foreach(FormHelperField f in Fields)
            {
                if(f.Path==path)
                {
                    if(f.Filters.Count > 0)
                    {                    
                        var list= new Dictionary<FieldFilterRef,object>(f.Filters);
                        foreach(KeyValuePair<FieldFilterRef,object> item in list)
                        {
                            if(item.Key == filter)
                            {
                                f.Filters.Remove(item.Key);
                            }
                        }
                    }
                }
            }
        }

        public void SetStatus(string message, int error=0)
        {
            /*
                error=0-нет ошибок
                    etc -- Ошибки валидации
                
                если контрол для отображения статуса установлен,
                выводим текст в него
                иначе запускаем старый механизм
             */
            bool valid=false;
            if(error==0)
            {
                valid=true;
            }
            
            if(StatusControl!=null)
            {
                StatusControl.Text=message;
            }
            else
            {
                OnValidate?.Invoke(valid,message);
            }
        }

        public void DisableControls()
        {
            if(ToolbarControl!=null)
            {
                ToolbarControl.IsEnabled=false;
            }

        }

        public void EnableControls()
        {
            if(ToolbarControl!=null)
            {
                ToolbarControl.IsEnabled=true;
            }
        }

        /// <summary>
        /// Заполняет selectbox данными из БД по параметрам
        /// 
        /// Возможно несколько раз вызывать данную функцию для одного комбобокса для заполнения
        /// из разных источников, при условии что имена ключей и значений будут одинаковыми
        /// есть проверка на повторное добавление с тем же ключем
        /// </summary>
        /// <autor>eletskikh_ya</autor>
        /// <param name="box">контрол для заполнения</param>
        /// <param name="Module">Имя контролера</param>
        /// <param name="Object">объект контролера</param>
        /// <param name="Action">действие контролера</param>
        /// <param name="Key">Имя ключа</param>
        /// <param name="Value">Имя значения</param>
        /// <param name="param">параметры для получения данных, если параметры не нужны то null</param>
        /// <param name="Int2Num">Если данный параметр true то данные в ключ selectbox будут преборазоованны Key.ToInt().ToString() что бы убрать .0 </param>
        /// <param name="clear">удалить данные еперед загрузкой</param>
        public static void ComboBoxInitHelper(SelectBox box, string Module, string Object, string Action, string Key, string Value, Dictionary<string, string> param = null, bool Int2Num = false, bool clear = false)
        {
            var q = new LPackClientQuery();

            q.Request.SetParam("Module", Module);
            q.Request.SetParam("Object", Object);
            q.Request.SetParam("Action", Action);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            if (param != null)
            {
                q.Request.SetParams(param);
            }

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    if (clear)
                    {
                        box.Items = new Dictionary<string, string>();
                    }

                    // преобразование int в string для избавления от .0
                    if (Int2Num)
                    {
                        var items = ListDataSet.Create(result, "ITEMS").GetItemsList(Key, Value);

                        foreach (var item in items)
                        {
                            var key = item.Key.ToInt().ToString();

                            if (!box.Items.ContainsKey(key))
                            {
                                box.Items.Add(key, item.Value);
                            }
                        }

                        // без этой строки не появляется список, подсмотрел в свойстве selectbox::Item set
                        box.UpdateListItems(box.Items);
                    }
                    else
                    {
                        var items = ListDataSet.Create(result, "ITEMS").GetItemsList(Key, Value);

                        foreach (var item in items)
                        {
                            // предотвращение повторного добавления данных с тем же ключем
                            if (!box.Items.ContainsKey(item.Key))
                            {
                                box.Items.Add(item.Key, item.Value);
                            }
                        }

                        // без этой строки не появляется список, подсмотрел в свойстве selectbox::Item set
                        box.UpdateListItems(box.Items);
                    }
                }
            }
        }

        /// <summary>
        /// Заполняет selectbox данными из БД по параметрам
        /// 
        /// Возможно несколько раз вызывать данную функцию для одного комбобокса для заполнения
        /// из разных источников, при условии что имена ключей и значений будут одинаковыми
        /// есть проверка на повторное добавление с тем же ключем
        /// </summary>
        /// <autor>eletskikh_ya</autor>
        /// <param name="box">контрол для заполнения</param>
        /// <param name="Module">Имя контролера</param>
        /// <param name="Object">объект контролера</param>
        /// <param name="Action">действие контролера</param>
        /// <param name="Key">Имя ключа</param>
        /// <param name="Value">Имя значения</param>
        /// <param name="param">параметры для получения данных, если параметры не нужны то null</param>
        /// <param name="Int2Num">Если данный параметр true то данные в ключ selectbox будут преборазоованны Key.ToInt().ToString() что бы убрать .0 </param>
        /// <param name="clear">удалить данные еперед загрузкой</param>
        public static void ComboBoxInitHelper(SelectBox box, string Module, string Object, string Action, string Key, string Value, string ListDataSetName, Dictionary<string, string> param = null, bool Int2Num = false, bool clear = false)
        {
            var q = new LPackClientQuery();

            q.Request.SetParam("Module", Module);
            q.Request.SetParam("Object", Object);
            q.Request.SetParam("Action", Action);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            if (param != null)
            {
                q.Request.SetParams(param);
            }

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    if (clear)
                    {
                        box.Items = new Dictionary<string, string>();
                    }

                    // преобразование int в string для избавления от .0
                    if (Int2Num)
                    {
                        var items = ListDataSet.Create(result, ListDataSetName).GetItemsList(Key, Value);

                        foreach (var item in items)
                        {
                            var key = item.Key.ToInt().ToString();

                            if (!box.Items.ContainsKey(key))
                            {
                                box.Items.Add(key, item.Value);
                            }
                        }

                        // без этой строки не появляется список, подсмотрел в свойстве selectbox::Item set
                        box.UpdateListItems(box.Items);
                    }
                    else
                    {
                        var items = ListDataSet.Create(result, ListDataSetName).GetItemsList(Key, Value);

                        foreach (var item in items)
                        {
                            // предотвращение повторного добавления данных с тем же ключем
                            if (!box.Items.ContainsKey(item.Key))
                            {
                                box.Items.Add(item.Key, item.Value);
                            }
                        }

                        // без этой строки не появляется список, подсмотрел в свойстве selectbox::Item set
                        box.UpdateListItems(box.Items);
                    }
                }
            }
        }

        /// <summary>
        /// Функция возвращает все контролы типа T принадлежащие form
        /// взято отсюда:
        /// https://stackoverflow.com/questions/974598/find-all-controls-in-wpf-window-by-type
        /// </summary>
        /// <typeparam name="T">тип получаемых контролов</typeparam>
        /// <param name="form">контрол в котором осуществляется поиск</param>
        /// <returns></returns>        
        public static IEnumerable<T> FindLogicalChildren<T>(DependencyObject form) where T : DependencyObject
        {
            if (form != null)
            {
                foreach (object rawChild in LogicalTreeHelper.GetChildren(form))
                {
                    if (rawChild is DependencyObject)
                    {
                        DependencyObject child = (DependencyObject)rawChild;
                        if (child is T)
                        {
                            yield return (T)child;
                        }

                        foreach (T childOfChild in FindLogicalChildren<T>(child))
                        {
                            yield return childOfChild;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// установить режим "грид занят одижанием данных"
        /// блокируется блок с инструментами (тулбар), блок данных затеняется
        /// </summary>
        public void HideSplash()
        {
            if(ToolbarControl!=null)
            {
                ToolbarControl.IsEnabled = true;
            }
        }

        /// <summary>
        /// установить режим "грид занят одижанием данных"
        /// блокируется блок с инструментами (тулбар), блок данных затеняется
        /// </summary>
        public void ShowSplash(string note = "")
        {
            if(ToolbarControl != null)
            {
                ToolbarControl.IsEnabled = false;
            }
        }

        /// <summary>
        /// установить режим "грид занят одижанием данных"
        /// блокируется блок с инструментами (тулбар), блок данных затеняется
        /// </summary>
        /// <param name="busy">true=занят, false=свободен</param>
        public void SetBusy(bool busy = true, string note = "")
        {
            if(busy)
            {
                ShowSplash(note);
            }
            else
            {
                HideSplash();
            }            
        }

       
    }
    
}
