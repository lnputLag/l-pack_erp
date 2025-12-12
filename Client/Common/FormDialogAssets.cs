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
    public class FormDialogControl
    {
        public FormDialogControl()
        {
            Required = false;
        }

        /// <summary>
        /// структура данных поля
        /// </summary>
        public FormHelperField Field { get; set; }
        public delegate Style FindResourceDelegate(string name);
        /// <summary>
        /// что-то она там ищет
        /// </summary>
        public FindResourceDelegate FindResource;

        public Border BorderLabel { get; set; }
        public Label Label { get; set; }
        public Border BorderControl { get; set; }
        public FrameworkElement Control { get; set; }

        /// <summary>
        /// обязательное поле
        /// флаг поднимается автоматически, когда есть фильтр типа: FormHelperField.FieldFilterRef.Required
        /// </summary>
        public bool Required { get; set; }

        public void Init()
        {
            if(Field.Filters.Count > 0)
            {
                foreach(KeyValuePair<FieldFilterRef, object> item in Field.Filters)
                {
                    if(item.Key == FormHelperField.FieldFilterRef.Required)
                    {
                        Required = true;
                    }

                    if(item.Key == FormHelperField.FieldFilterRef.MaxLen)
                    {
                        Field.Params.CheckAdd("MaxLength", item.Value.ToString());
                    }
                }
            }

            var p = new Dictionary<string, string>();
            if(!Field.Options.IsNullOrEmpty())
            {
                p = DictionaryExtension.CreateFromLine(Field.Options);
            }

            if(
                !string.IsNullOrEmpty(Field.Description) 
                && Field.ControlType != "CheckBox"
            )
            {
                BorderLabel = new Border();
                BorderLabel.Style = FindResource("FormLabelContainer");

                Label = new Label();
                Label.Style = FindResource("FormLabel");

                //астериска нет, будет добавлен автоматически
                if(Field.Description.IndexOf("*") == -1)
                {
                    var s = "";
                    if(Required)
                    {
                        s = "*";
                    }
                    else
                    {
                        s = " ";
                    }
                    Field.Description = $"{Field.Description}: {s}";
                }
                Label.Content = Field.Description;

                BorderLabel.Child = Label;
            }

            if(Field.Control != null)
            {
                Control = (FrameworkElement)Field.Control;
            }
            else
            {
                if(Field.ControlType != "void")
                {
                    switch(Field.FieldType)
                    {
                        case FieldTypeRef.Date:
                            {
                                Field.ControlType = "Date";
                            }
                            break;

                        case FieldTypeRef.DateTime:
                            {
                                Field.ControlType = "DateTime";
                            }
                            break;
                    }
                }

                switch(Field.ControlType)
                {
                    case "TextBox":
                        {
                            Control = new TextBox();
                            Control.Name = Field.Path;
                            Control.Style = FindResource("FormField");
                            Control.HorizontalAlignment = HorizontalAlignment.Left;
                            {
                                var v = Field.Params.CheckGet("ControlHeight").ToInt();
                                if(v > 0)
                                {
                                    Control.Height = v;
                                }
                            }
                            {
                                var v = Field.Params.CheckGet("ControlWidth").ToInt();
                                if(v > 0)
                                {
                                    Control.Width = v;
                                    Control.MinWidth = v;
                                }
                            }
                            {
                                var v = Field.Params.CheckGet("MaxLength").ToInt();
                                if(v > 0)
                                {
                                    Control.SetValue(TextBox.MaxLengthProperty, v);
                                }
                            }
                        }
                        break;

                    case "CheckBox":
                        {
                            Control = new CheckBox();
                            Control.Name = Field.Path;
                            Control.Style = FindResource("FormField");
                            (Control as CheckBox).Content = Field.Description;
                            Control.HorizontalAlignment = HorizontalAlignment.Left;
                            (Control as CheckBox).VerticalContentAlignment = VerticalAlignment.Center;
                        }
                        break;

                    case "SelectBox":
                        {
                            Control = new SelectBox();
                            Control.Name = Field.Path;
                            Control.Style = FindResource("CustomFormField");
                            Control.HorizontalAlignment = HorizontalAlignment.Left;
                        }
                        break;

                    case "Date":
                        {
                            Control = new Interfaces.Main.DatePicker();
                            Control.Name = Field.Path;
                            Control.Style = FindResource("DateEditStyle");
                            Control.HorizontalAlignment = HorizontalAlignment.Left;
                        }
                        break;

                    case "DateTime":
                        {
                            Control = new Interfaces.Main.DateTimePicker();
                            Control.Name = Field.Path;
                            Control.Style = FindResource("DateTimeEditStyle");
                            Control.HorizontalAlignment = HorizontalAlignment.Left;
                            Control.Width = 130;
                        }
                        break;

                    case "void":
                    default:
                        {
                            Control = new TextBox();
                            Control.Name = Field.Path;
                            Control.Style = FindResource("FormField");
                            Control.HorizontalAlignment = HorizontalAlignment.Left;
                            Control.Visibility = Visibility.Collapsed;
                        }
                        break;
                }

                Field.Control= Control;
            }


            int widthDefault = 0;
            switch(Field.FieldType)
            {
                case FieldTypeRef.String:
                case FieldTypeRef.Boolean:
                    widthDefault = 320;
                    break;

                case FieldTypeRef.Integer:
                case FieldTypeRef.Double:
                    widthDefault = 50;
                    break;

                case FieldTypeRef.DateTime:
                    {
                        if(Field.Control.GetType() == typeof(Interfaces.Main.DateTimePicker))
                        {
                            widthDefault = 130;
                        }
                        else
                        {
                            widthDefault = 80;
                        }
                    }
                    break;
            }

            /*
                1 значение по умолчанию в зависимости от типа данных
                2 предустановленые стили ControlWidthClassRef
                3 установленные вручную значения:
                    3.1 статическая ширина
                    3.2 динамическая ширина
             */

            if(Control != null)
            {
                var widthStatic = true;
                var width = widthDefault;

                {
                    if(Field.Width > 0)
                    {
                        width = Field.Width;
                    }
                }

                {
                    if(
                        Field.MinWidth > 0
                        && Field.MaxWidth > 0
                    )
                    {
                        widthStatic = false;
                    }
                }


                if(widthStatic)
                {
                    //статический размер
                    Control.Width = width;
                }
                else
                {
                    //динамический размер
                    Control.MinWidth = Field.MinWidth;
                    Control.MaxWidth = Field.MaxWidth;
                }
            }
            

            var controlContainer = new StackPanel();
            controlContainer.Orientation = Orientation.Horizontal;
            controlContainer.Children.Add(Control);

            if(Field.Fillers.Count > 0)
            {
                foreach(FormHelperFiller filler in Field.Fillers)
                {
                    var button = new Button();
                    button.Style = FindResource("ButtonGlyph");
                    button.Margin = new Thickness(-1, 0, 0, 0);
                    button.Tag = $"path={Field.Path}";
                    button.Click += (object sender, RoutedEventArgs e) =>
                    {
                        var path = "";
                        var b = (Button)sender;
                        if(b != null)
                        {
                            var t = b.Tag.ToString();
                            if(!t.IsNullOrEmpty())
                            {
                                path = t.CropAfter2("path=");
                            }
                        }

                        if(!path.IsNullOrEmpty())
                        {
                            if(filler.Action != null)
                            {

                                if(Field.Form != null)
                                {
                                    var value = filler.Action.Invoke(Field.Form);
                                    Field.Form.SetValueByPath(path, value);
                                }
                            }
                        }
                    };

                    if(!filler.Description.IsNullOrEmpty())
                    {
                        button.ToolTip = filler.Description;
                    }

                    if(!filler.Caption.IsNullOrEmpty())
                    {
                        button.Content = filler.Caption;
                        if(!filler.Style.IsNullOrEmpty())
                        {
                            button.Style = FindResource(filler.Style);
                        }
                        else
                        {
                            button.Style = FindResource("Button");
                        }
                    }
                    else
                    {
                        if(!filler.IconStyle.IsNullOrEmpty())
                        {
                            var image = new Image();
                            image.Style = FindResource(filler.IconStyle);
                            button.Content = image;
                            button.Style = FindResource("ButtonGlyph");
                        }
                    }

                    controlContainer.Children.Add(button);
                }
            }

            if(Field.Comments.Count > 0)
            {
                foreach(FormHelperComment comment in Field.Comments)
                {
                    var label = new Label();
                    label.Style = FindResource("FormLabelNote");
                    label.Tag = $"path={Field.Path}";
                    label.Content = comment.Content;

                    if(!string.IsNullOrEmpty(comment.Name))
                    {
                        label.Name = comment.Name;
                    }

                    controlContainer.Children.Add(label);
                }
            }

            BorderControl = new Border();
            BorderControl.Style = FindResource("FormFieldContainer");
            BorderControl.Child = controlContainer;
        }
    }

    public class RequestData
    {
        public string Module { get; set; }
        public string Object { get; set; }
        public string Action { get; set; }

        /// <summary>
        /// таймаут ожидаения ответа, мс
        /// </summary>
        public int Timeout { get; set; } = Central.Parameters.RequestTimeoutDefault;
        public int Attempts { get; set; } = 1;

        /// <summary>
        /// ключ с секцией данных для грида
        /// </summary>
        public string AnswerSectionKey { get; set; } = "ITEMS";
        public Dictionary<string, string> Params { get; set; }
        public Dictionary<string, ListDataSet> AnswerData { get; set; }

        public delegate void OnCompleteDelegate(FormHelperField f, ListDataSet ds);
        /// <summary>
        /// после успешного завершения запроса
        /// </summary>
        public OnCompleteDelegate OnComplete;

        public delegate void OnCompleteDelegateGrid(ListDataSet ds);
        /// <summary>
        /// После успешного завершения запроса (в гриде)
        /// </summary>
        public OnCompleteDelegateGrid OnCompleteGrid;

        public delegate void BeforeRequestDelegate(RequestData rd);
        /// <summary>
        /// коллбэк перед отправкой запроса
        /// </summary>
        public BeforeRequestDelegate BeforeRequest;

        public delegate ListDataSet AfterRequestDelegate(RequestData rd, ListDataSet ds);
        /// <summary>
        /// коллбэк после получения ответа
        /// </summary>
        public AfterRequestDelegate AfterRequest;

        public delegate void AfterUpdateDelegate(RequestData rd, ListDataSet ds);
        public AfterUpdateDelegate AfterUpdate;
    }
}
