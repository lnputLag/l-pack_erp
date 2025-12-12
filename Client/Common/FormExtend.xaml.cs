using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using NPOI.OpenXmlFormats.Spreadsheet;
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
using Xceed.Wpf.Toolkit;
using static Client.Common.FormExtend;


namespace Client.Common
{
    /// <summary>
    /// Interaction logic for FormExtend.xaml
    /// <author>eletskikh_ya</author>
    /// </summary>
    public partial class FormExtend : ControlBase
    {
        public class LineControlForm
        {
            private static Dictionary<FormHelperField.ControlWidthDegree, int> controlWidth = new Dictionary<FormHelperField.ControlWidthDegree, int>()
            {
                { FormHelperField.ControlWidthDegree.Small, 50 },
                { FormHelperField.ControlWidthDegree.Medium, 120 },
                { FormHelperField.ControlWidthDegree.Large, 220 },
            };

            public Border BorderLabel { get; set; }
            public Label Label { get; set; }
            public Border BorderControl { get; set; }
            public FrameworkElement Control { get; set; }

            public delegate Style FindResource(string name);

            public LineControlForm(FormHelperField field, FindResource findResource)
            {
                // есть описание, сделаем контрол для описания
                if(!string.IsNullOrEmpty(field.Description) && field.ControlType!="CheckBox")
                {
                    BorderLabel = new Border();
                    BorderLabel.Style = findResource("FormLabelContainer");

                    Label = new Label();
                    Label.Style = findResource("FormLabel");
                    Label.Content = field.Description;
                    BorderLabel.Child = Label;
                }

                BorderControl = new Border();
                BorderControl.Style = findResource("FormFieldContainer");

                switch(field.ControlType)
                {
                    case "TextBox":
                        Control = new TextBox();
                        Control.Style = findResource("FormField");
                        Control.HorizontalAlignment = HorizontalAlignment.Left;
                        break;
                    case "CheckBox":
                        Control = new CheckBox();
                        Control.Style = findResource("FormField");
                        (Control as CheckBox).Content = field.Description;
                        Control.HorizontalAlignment = HorizontalAlignment.Left;
                        (Control as CheckBox).VerticalContentAlignment = VerticalAlignment.Center;
                        break;
                    case "SelectBox":
                        Control = new SelectBox();
                        Control.Style = findResource("CustomFormField");
                        Control.HorizontalAlignment = HorizontalAlignment.Left;
                        break;
                    case "DateTime":
                        Control = new DateTimeUpDown();
                        Control.HorizontalAlignment = HorizontalAlignment.Left;
                        break;
                    default:
                        break;
                }

                #region Установка размера
                if (Control != null)
                {
                    if(field.MinWidth>0)
                    {
                        Control.MinWidth = field.MinWidth;
                    }

                    if(field.MaxWidth>0)
                    {
                        Control.MaxWidth = field.MaxWidth;
                    }

                    if (field.Width > 0)
                    {
                        Control.Width = field.Width;
                    }
                    else if(field.ControlWidth != FormHelperField.ControlWidthDegree.Unknow)
                    {
                        Control.Width = controlWidth[field.ControlWidth];
                    }
                    else
                    {
                        if (Control is CheckBox)
                        {
                            // чекбоес будет отображаться по размеру надписи

                        }
                        else
                        {
                            // необходимо посчитать, пока заглушка
                            Control.Width = 200;
                        }
                    }
                }
                #endregion

                BorderControl.Child = Control;
                field.Control = Control;

                field.Create();
            }
        }

        public class RequestData
        {
            public string Module { get; set; }
            public string Object { get; set; }
            public string Action { get; set; }

            public int Timeout { get; set; } = Central.Parameters.RequestTimeoutMin;
            public int Attempts { get; set; } = 1;

            public string Key { get; set; } = "ITEMS";
        }

        public FormExtend()
        {
            InitializeComponent();

            Form = new FormHelper();
        }



        public delegate Dictionary<string, ListDataSet> GetDataRequest(FormHelper form);
        public event GetDataRequest OnGetData;  

        public delegate void BeforeSave(Dictionary<string, string> parameters);
        public event BeforeSave OnBeforeSave;

        public delegate void AfterSave(int id, Dictionary<string, ListDataSet> result);
        public event AfterSave OnAfterSave;

        public delegate void BeforeGet(Dictionary<string, string> parameters);
        public event BeforeGet OnBeforeGet;

        public delegate void AfterGet(Dictionary<string, ListDataSet> result);
        public event AfterGet OnAfterGet;


        /// <summary>
        /// процессор форм
        /// </summary>
        private FormHelper Form { get; set; }

        public string ID { get; set; }
        public int Id { get; set; }

        public string FrameName { get; set; }

        public string Title { get; set; }

        public bool Closable { get; set; } = true;

        public List<FormHelperField> Fields { get; set; }

        public RequestData QueryGet { get; set; }
        public RequestData QuerySave { get; set; }

        private bool IsInitFields = false;

        private Dictionary<string, LineControlForm> FormLineControls
        {
            get;
            set;
        }

        public FormHelperField this[string index]
        {
            get
            {
                foreach (var item in Fields)
                {
                    if(item.Path == index)
                    {
                        return item;
                    }
                }

                return null;
            }
        }


        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
        }

        private void Save()
        {
            if(Form.Validate())
            {
                if (QuerySave != null)
                {
                    var p = Form.GetValues();

                    OnBeforeSave?.Invoke(p);

                    p.CheckAdd(ID, Id.ToString());

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", QuerySave.Module);
                    q.Request.SetParam("Object", QuerySave.Object);
                    q.Request.SetParam("Action", QuerySave.Action);

                    q.Request.Timeout = QuerySave.Timeout;
                    q.Request.Attempts = QuerySave.Attempts;

                    q.Request.SetParams(p);

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            //var ds = ListDataSet.Create(result, "ITEMS");
                            //var id = ds.GetFirstItemValueByKey("ID").ToInt();
                            OnAfterSave?.Invoke(Id, result);
                            Close();
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        private void Close()
        {
            var frameName = GetFrameName();
            Central.WM.Close(frameName);

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
        }

        private Style FindResource(string name)
        {
            return (Style)SaveButton.FindResource(name);
        }

        /// <summary>
        /// Возвращает строку с описанием и контролом
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public LineControlForm? GetControllLine(string name)
        {
            if(FormLineControls!=null)
            {
                if(FormLineControls.ContainsKey(name))
                {
                    return FormLineControls[name];
                }
            }

            return null;
        }

        private void InitFields()
        {
            if(!IsInitFields)
            {
                IsInitFields = true;

                FormLineControls = new Dictionary<string, LineControlForm>();

                int i, n = Fields.Count;
                bool focus = false;

                for(i=0;i<n;i++)
                {
                    var field = Fields[i];

                    var rowDefinition = new RowDefinition();
                    rowDefinition.Height = new GridLength(0, GridUnitType.Auto);

                    BodyGrid.RowDefinitions.Add(rowDefinition);

                    LineControlForm line = new LineControlForm(field, FindResource);

                    FormLineControls.Add(field.Path, line);

                    if (line.BorderLabel != null)
                    {
                        System.Windows.Controls.Grid.SetRow(line.BorderLabel, i);
                        System.Windows.Controls.Grid.SetColumn(line.BorderLabel, 0);
                        BodyGrid.Children.Add(line.BorderLabel);
                    }

                    System.Windows.Controls.Grid.SetRow(line.BorderControl, i);
                    System.Windows.Controls.Grid.SetColumn(line.BorderControl, 1);
                    BodyGrid.Children.Add(line.BorderControl);

                    if(!focus)
                    {
                        if(line.BorderControl is TextBox)
                        {
                            line.BorderControl.Focus();
                            focus = true;
                        }
                    }
                }

                Form.SetFields(Fields);
                Form.ToolbarControl = FormToolbar;
                Form.StatusControl = Status;

                Form.SetDefaults();
            }
        }

        private void OnFormLoaded(object sender, RoutedEventArgs e)
        {
            InitFields();

            if(Id!=0)
            {
                GetData();
            }
        }

        private async void GetData()
        {
            if (OnGetData != null)
            {
                var result = OnGetData.Invoke(Form);
                OnAfterGet?.Invoke(result);
            }
            else
            {
                if (QueryGet != null)
                {
                    var p = new Dictionary<string, string>();
                    {
                        p.CheckAdd(ID, Id.ToString());
                    }

                    OnBeforeGet?.Invoke(p);

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", QueryGet.Module);
                    q.Request.SetParam("Object", QueryGet.Object);
                    q.Request.SetParam("Action", QueryGet.Action);
                    q.Request.SetParams(p);

                    q.Request.Timeout = QueryGet.Timeout;
                    q.Request.Attempts = QueryGet.Attempts;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            if (!String.IsNullOrEmpty(QueryGet.Key))
                            {
                                var ds = ListDataSet.Create(result, QueryGet.Key);
                                Form.SetValues(ds);
                            }

                            OnAfterGet?.Invoke(result);
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }
        }

        private string GetFrameName()
        {
            return $"{FrameName}_{Id}";
        }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно

            Central.WM.FrameMode = 1;
            Central.WM.Show(GetFrameName(), Title, Closable, "add", this);

        }
    }
}
