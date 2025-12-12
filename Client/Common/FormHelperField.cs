using Client.Interfaces.Main;
using Client.Interfaces.Production;
using Newtonsoft.Json;
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
    public class FormHelperField
    {
        public FormHelperField()
        {
            Enabled = true;
            Index = 0;            
            Name = "";
            Path = "";
            Style = "";
            FieldType = FieldTypeRef.String;
            ControlType = "TextBox";
            Format = "";
            Options = "";
            Mask = "";
            Control = null;
            Filters = new Dictionary<FieldFilterRef, object>();
            Valid = true;
            Description = "";
            Doc = "";
            Default = "";
            ActualValue = "";
            ValidateMessage = "";
            ValidateResult = true;
            ValidateProcessed = false;
            First = false;
            CaretIndex = 0;
            Fillers = new List<FormHelperFiller>();
            Comments = new List<FormHelperComment>();
            Form = null;
            Params = new Dictionary<string, string>();
            AutoloadItems = true;
            GroupName = "";
        }

        

        /// <summary>
        /// Ширина контрола
        /// </summary>
        public enum ControlWidthDegree
        {
            Unknow, Small, Medium, Large
        }
        public bool Enabled { get; set; }
        public int Index { get; set; }        
        public string Name { get; set; }
        public string Path { get; set; }
        public string Style { get; set; }
        public enum FieldTypeRef
        {
            String = 1,
            Integer = 2,
            Double = 3,
            DateTime = 4,
            Boolean = 5,
            Date = 6,
            Time = 7,
        }
        public FieldTypeRef FieldType { get; set; }
        /// <summary>
        /// TextBox, SelectBox, CheckBox, void
        /// </summary>
        public string ControlType { get; set; }
        /// <summary>
        /// Ширина контрола, если -1 то будет выбрана автоматически
        /// </summary>
        public int Width { get; set; } = -1;
        public int MinWidth { get; set; } = -1;
        public int MaxWidth { get; set; } = -1;
        
        public ControlWidthDegree ControlWidth { get; set; } = ControlWidthDegree.Unknow;

        /// <summary>
        /// формат 
        /// для чисел: N0, 
        /// для дат: dd.MM.yyyy, dd.MM.yyyy HH:mm:ss
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// дополнительные параметры отображения
        /// zeronoempty -- в числовых полях отображается 0, а не пустое поле
        /// </summary>
        public string Options { get; set; } = "";
        public Dictionary<string, string> Params { get; set; }
        /// <summary>
        /// выполнять LoadItems в конструкторе поля
        /// </summary>
        public bool AutoloadItems { get; set; }
        public string Mask { get; set; }
        public object Control { get; set; }

        /// <summary>
        /// фильтры ввода
        /// http://192.168.3.237/developer/l-pack-erp/client/development/elements/formhelper
        /// </summary>
        public enum FieldFilterRef
        {
            /// <summary>
            /// только цифры,
            /// пропускает на выход только цифры,
            /// первичный фильтр
            /// </summary>
            DigitOnly = 1,
            /// <summary>
            /// только цифры и зяпятая,
            /// пропускает на выход только зяпятую,
            /// первичный фильтр
            /// </summary>
            DigitCommaOnly = 2,
            /// <summary>
            /// только алфавитные символы,
            /// пропускает на выход только алфавитные символы,
            /// первичный фильтр
            /// </summary>
            AlphaOnly = 3,
            /// <summary>
            /// ограничение длины,
            /// подрезает строку ввода до указанной длины
            /// вторичный фильтр
            /// </summary>
            MaxLen = 4,
            /// <summary>
            /// обязательное поле,
            /// вызовет ошибку валидации, если поле пусто
            /// валидатор
            /// </summary>
            Required = 8,
            /// <summary>
            /// не ноль,
            /// вызовет ошибку валидации, если в поле "0"
            /// валидатор
            /// </summary>
            IsNotZero = 11,
            /// <summary>
            /// только латиница,
            /// пропускает на выход только латинские алфавитные символы,
            /// первичный фильтр
            /// </summary>
            LatinOnly = 12,
            /// <summary>
            /// только цифры и алфавитные символы,
            /// пропускает на выход только только цифры и алфавитные символы,
            /// первичный фильтр
            /// </summary>
            AlphaDigitOnly = 13,
            /// <summary>
            /// в верхний регистр,
            /// преобразует строку ввода в верхний регистр
            /// фильтр-преобразователь
            /// </summary>
            ToUpperCase = 14,
            /// <summary>
            /// без латиницы
            /// вырезает из ввода латинские алфавитные символы,
            /// вторичный фильтр
            /// </summary>
            NoLatin = 15,
            /// <summary>
            /// без кириллицы
            /// вырезает из ввода кириллические алфавитные символы,
            /// вторичный фильтр
            /// </summary>
            NoCyrillic = 16,
            /// <summary>
            /// в нижний регистр,
            /// преобразует строку ввода в нижний регистр
            /// фильтр-преобразователь
            /// </summary>
            ToLowerCase = 17,
            /// <summary>
            /// фильтр для проверки номера автомобиля
            /// первичный фильтр
            /// </summary>
            CarNumber = 18,
            /// <summary>
            /// запрещенные символы
            /// </summary>
            DeniedCharacters = 19,

            MinLen = 5,
            MaxValue = 6,
            MinValue = 7,
        }
        public Dictionary<FieldFilterRef, object> Filters { get; set; }
        public bool Valid { get; set; }
        /// <summary>
        /// примечание к полю ввода, появится слева от поля
        /// </summary>
        public string Description { get; set; } = "";
        public string Doc { get; set; } = "";
        public string Default { get; set; } = "";
        public string ActualValue { get; set; } = "";
        public string ValidateMessage { get; set; } = "";
        /// <summary>
        /// результат проверки поля, true - проверка пройдена, false проверка не пройдена
        /// </summary>
        public bool ValidateResult { get; set; } = true;
        /// <summary>
        /// флаг обозначающий учитывать ли результат кастомной проверки поля
        /// </summary>
        public bool ValidateProcessed { get; set; } = false;
        /// <summary>
        /// если true, фокус по умолчанию будет установлен сюда
        /// </summary>
        public bool First { get; set; } = false;
        public int CaretIndex { get; set; } = 0;

        /// <summary>
        /// Для RadioButton 
        /// </summary>
        public string GroupName { get; set; } = "";

        public void Create()
        {
            //FIXME: OnAfterCreate OR OnCreate?

            if(Control is FrameworkElement)
            {
                OnAfterCreate?.Invoke((FrameworkElement)Control);
            }

            if(OnCreate != null)
            {
                OnCreate.Invoke(this);
            }

            if(ControlType == "SelectBox")
            {
                if(AutoloadItems)
                {
                    UpdateItems();
                }
                
                var c = (SelectBox)Control;
                if(c != null)
                {
                    c.OnSelectItem = (Dictionary<string, string> row) =>
                    {
                        if(OnChange != null)
                        {
                            var v = "";
                            if(!c.GridPrimaryKey.IsNullOrEmpty())
                            {
                                v = row.CheckGet(c.GridPrimaryKey);
                            }
                            ActualValue= v;
                            OnChange.Invoke(this, v);
                        }
                        return true;
                    };
                }
            }
        }

        public void UpdateItems()
        {
            if(ControlType == "SelectBox")
            {
                var complete=false;

                if(!complete)
                {
                    if(QueryLoadItems != null)
                    {
                        LoadItems();
                    }
                }

                if(!complete)
                {
                    if(OnUpdateItems != null)
                    {
                        var list=OnUpdateItems.Invoke(this);
                        if(list != null)
                        {
                            var c=(SelectBox)Control;
                            if(c != null)
                            {
                                c.Items=list;
                            }                            
                        }
                        
                    }
                }
               
            }
        }

        public void ClearItems()
        {
            ActualValue = "";
            if(ControlType == "SelectBox")
            {
                var c = (SelectBox)Control;
                if(c != null)
                {
                    c.GridDataSet.Items.Clear();
                }
            }
        }

        public void ClearValue()
        {
            if(ControlType == "SelectBox")
            {
                var c = (SelectBox)Control;
                if(c != null)
                {
                    c.ValueTextBox.Text = "";
                    c.SelectedItem = new KeyValuePair<string, string>(Path, "");
                }
            }
        }

        public delegate void BeforeGetDelegate(FormHelperField f);
        public BeforeGetDelegate BeforeGet;
        public virtual void BeforeGetAction(FormHelperField f)
        {

        }

        public delegate void AfterSetDelegate(FormHelperField f, string v);
        /// <summary>
        /// после установки значения
        /// </summary>
        public AfterSetDelegate AfterSet;
        public virtual void AfterSetAction(FormHelperField f, string v)
        {
        }

        public delegate void BeforeSetDelegate(FormHelperField f, string v);
        /// <summary>
        /// до установки значения
        /// </summary>
        public BeforeSetDelegate BeforeSet;
        public virtual void BeforeSetAction(FormHelperField f, string v)
        {
        }

        public delegate void AfterRenderDelegate(FormHelperField f);
        /// <summary>
        /// после рендера на экране
        /// </summary>
        public AfterRenderDelegate AfterRender;
        public virtual void AfterRenderAction(FormHelperField f)
        {
        }

        public delegate void OnChangeDelegate(FormHelperField f, string v);
        /// <summary>
        /// после изменения значения поля
        /// </summary>
        public OnChangeDelegate OnChange;
        public virtual void OnChangeAction(FormHelperField f, string v)
        {

        }

        public delegate void OnTextChangeDelegate(FormHelperField f, string v);
        /// <summary>
        /// После изменения текста в поле
        /// </summary>
        public OnTextChangeDelegate OnTextChange;
        public virtual void OnTextChangeAction(FormHelperField f, string v)
        {

        }

        public delegate void OnCreateDelegate(FormHelperField f);
        /// <summary>
        /// после создания поля
        /// (после работы конструктора)
        /// </summary>
        public OnCreateDelegate OnCreate;
        public virtual void OnCreateAction(FormHelperField f)
        {

        }

        public delegate void ValidateDelegate(FormHelperField f, string v);
        public ValidateDelegate Validate;
        public virtual void ValidateAction(FormHelperField f, string v)
        {
            ValidateMessage = "";
            ValidateResult = true;
            ValidateProcessed = true;
        }

        public delegate void AfterCreated(FrameworkElement control);
        public event AfterCreated OnAfterCreate;

        public bool IsFilter(FieldFilterRef filter)
        {
            bool result = false;
            if(Filters.Count > 0)
            {
                var list = new Dictionary<FieldFilterRef, object>(Filters);
                foreach(KeyValuePair<FieldFilterRef, object> item in list)
                {
                    if(item.Key == filter)
                    {
                        result = true;
                    }
                }
            }
            return result;
        }

        public List<FormHelperFiller> Fillers { get; set; }

        public List<FormHelperComment> Comments { get; set; }

        public FormHelper Form { get; set; }

        /// <summary>
        /// запрос для обновления значений вариантов выбора
        /// (для полей с возможностью выбора из множества значений)
        /// </summary>
        public RequestData QueryLoadItems { get; set; }
        

        public delegate Dictionary<string,string> OnUpdateItemsDelegate(FormHelperField f);
        /// <summary>
        /// при обновлении данных
        /// </summary>
        public OnUpdateItemsDelegate OnUpdateItems;
        public virtual Dictionary<string,string> OnUpdateItemsAction(FormHelperField f)
        {
            var ds=new Dictionary<string,string>();
            return ds;
        }

        public void LoadItems()
        {
            var qp = QueryLoadItems;
            if(qp != null)
            {
                var q = new LPackClientQuery();

                if(qp.BeforeRequest != null)
                {
                    qp.BeforeRequest.Invoke(qp);
                }

                q.Request.SetParam("Module", qp.Module);
                q.Request.SetParam("Object", qp.Object);
                q.Request.SetParam("Action", qp.Action);
                q.Request.SetParams(qp.Params);

                q.Request.Timeout = qp.Timeout;
                q.Request.Attempts = qp.Attempts;

                q.DoQuery();

                if(q.Answer.Status == 0)
                {
                    var answerData = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if(answerData != null)
                    {
                        if(qp.OnComplete != null)
                        {
                            if(!String.IsNullOrEmpty(qp.AnswerSectionKey))
                            {
                                var ds = ListDataSet.Create(answerData, qp.AnswerSectionKey);
                                qp.OnComplete.Invoke(this, ds);
                            }
                        }
                    }
                }
            }
        }
    }

    public class FormHelperFiller
    {
        public FormHelperFiller()
        {
            Name = "";
            Description = "";
            Caption = "";
            Style = "";
            IconStyle = "";
            Action = null;
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public string Caption { get; set; }
        /// <summary>
        /// ButtonGlyph
        /// </summary>
        public string Style { get; set; }
        public string IconStyle { get; set; }
        public delegate string FormHelperFillerActionDelegate(FormHelper form);
        public FormHelperFillerActionDelegate Action;
    }
}
