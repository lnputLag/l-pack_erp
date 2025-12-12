using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
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
using static Client.Common.FormExtend;
using static Client.Common.FormHelperField;

namespace Client.Interfaces.Main
{
    
    /// <summary>
    /// диалог для работы с данными объекта
    /// (форма редактирования)
    /// </summary>
    /// <author>eletskikh_ya</author>
    /// <version>1</version>
    /// <released>2023-10-24</released>
    /// <changed>2023-10-24</changed>
    public partial class FormDialog:ControlBase
    {
        public FormDialog()
        {
            InitializeComponent();
            
            Loaded += OnRender;

            Form = new FormHelper();
            Values=new Dictionary<string, string>();
            DataSetList = new Dictionary<string, ListDataSet>();
            ControlName =this.GetType().Name;
            UseSave=true;
            PrimaryKey="";
            PrimaryKeyValue="";
            InsertId="";
            Mode="";
            FrameTitle="";
            FrameName="";

            {
                Commander.Add(new CommandItem()
                {
                    Name = "save",
                    Enabled = true,
                    Title = "Сохранить",
                    Description = "",
                    ButtonUse = true,
                    ButtonControl = SaveButton,
                    HotKey = "Ctrl+Return|F2",
                    AccessLevel = Common.Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        Save();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "cancel",
                    Enabled = true,
                    Title = "Отмена|F10",
                    Description = "",
                    ButtonUse = true,
                    ButtonControl = CancelButton,
                    HotKey = "Escape",
                    Action = () =>
                    {
                        Hide();
                    },
                });
                Commander.Init(this);
            }
        }

        public string ControlName {get;set;}
        public bool UseSave {get;set;}

        public delegate bool BeforeGetDelegate(Dictionary<string, string> parameters);
        public event BeforeGetDelegate BeforeGet;

        public delegate bool GetDataRequestDelegate(FormDialog fd);
        public event GetDataRequestDelegate OnGet;  

        public delegate void AfterGetDelegate(FormDialog fd);
        public event AfterGetDelegate AfterGet;


        public delegate bool BeforeSaveDelegate(Dictionary<string, string> parameters);
        public event BeforeSaveDelegate BeforeSave;

        public delegate bool SaveDataRequestDelegate(FormDialog fd);
        public event SaveDataRequestDelegate OnSave;  

        public delegate void AfterSaveDelegate(FormDialog fd);
        public event AfterSaveDelegate AfterSave;


        public delegate bool BeforeDeleteDelegate(Dictionary<string, string> parameters);
        public event BeforeDeleteDelegate BeforeDelete;

        public delegate bool DeleteDataRequestDelegate(FormDialog fd);
        public event DeleteDataRequestDelegate OnDelete;  

        public delegate void AfterDeleteDelegate(FormDialog fd);
        public event AfterDeleteDelegate AfterDelete;
        

        public delegate void AfterUpdateDelegate(FormDialog fd);
        public event AfterUpdateDelegate AfterUpdate;

        public delegate void AfterRenderDelegate(FormDialog fd);
        public event AfterRenderDelegate AfterRender;

        private FormHelper Form { get; set; }
        public Dictionary<string, string> Values { get; set; }
        public Dictionary<string, ListDataSet> DataSetList { get; set; }

        public string InsertId { get; set; }
        public string Title { get; set; }
        public string TitleCustom { get; set; }
        public bool Closable { get; set; } = true;

        public List<FormHelperField> Fields { get; set; }

        public Common.RequestData QueryGet { get; set; }
        public Common.RequestData QuerySave { get; set; }
        public Common.RequestData QueryDelete { get; set; }

        private bool IsInitFields = false;
        public string Mode {get;set;}

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

        public bool CheckInitialized()
        {
            return IsInitFields;
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

        public void Run(string mode)
        {
            Central.Dbg($"FormDialog Run");

            mode =mode.Trim();
            mode=mode.ToLower();
            Mode=mode;
            switch(mode)
            {
                case "create":
                case "edit":
                case "open":
                    Get();
                    break;

                case "delete":
                    Delete();
                    break;
            }
        }

        public bool Rendered { get; set; }

        /// <summary>
        /// после рендера графической части
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRender(object sender, RoutedEventArgs e)
        {
            Rendered = false;
            Central.Dbg($"FormDialog OnDialogLoad 0");

            Central.Dbg($"FormDialog OnDialogLoad 1 InitFields");
            InitFields();

            Central.Dbg($"FormDialog OnDialogLoad 2 SetValues");
            if(Values.Count > 0)
            {
                Form.SetValues(Values);
            }
            else
            {
                Form.SetDefaults();
            }

            if(UseSave)
            {
                SaveButton.Visibility=Visibility.Visible;
            }
            else
            {
                SaveButton.Visibility=Visibility.Collapsed;
            }

            Commander.RenderButtons();

            Form.RenderControls();
            if(AfterRender!=null)
            {
                AfterRender.Invoke(this);
            }
            Rendered = true;
            Central.Dbg($"FormDialog OnDialogLoad 9 finish");

        }

        public void SetValues(Dictionary<string, string> values)
        {
            Values=values;
            Form.SetValues(Values);
        }

        public void SetValues2(ListDataSet ds)
        {            
            Values = new Dictionary<string,string>();
            if(ds.Items.Count>0)
            {
                Values=ds.Items.First();
            }
        }

        public void ClearValues()
        {
            Values=new Dictionary<string, string>();
        }

        /// <summary>
        /// устанавливает актуальное на данный момент значение поля
        /// </summary>
        /// <param name="fieldName"></param>
        public void  FieldSetValueActual(string fieldName)
        {
            var v = Values;
            var v0 = GetValuesActual();
            var id = v.CheckGet(fieldName);
            if(!id.IsNullOrEmpty())
            {
                FieldSetValue(fieldName, id.ToString());
            }
        }

        /// <summary>
        /// возвращает актуальное на данный момент значение поля
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public string FieldGetValueActual(string fieldName)
        {
            var result = "";
            var v = GetValuesActual();
            result = v.CheckGet(fieldName);
            return result;
        }

        /// <summary>
        /// актуальные значения полей (в том числе измененные в процессе редактирования формы)
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetValuesActual()
        {
            var p=new Dictionary<string,string>();
            foreach(var item in Fields)
            {
                p.CheckAdd(item.Path, item.ActualValue);
            }
            return p;
        }


        public Dictionary<string, string> GetValues()
        {
            Values=Form.GetValues();
            return Values;
        }

        public bool Validate()
        {
            var result=false;
            result=Form.Validate();
            return result;
        }

        public void Get()
        {
            Central.Dbg($"FormDialog Get");

            var resume=true;
            var p = new Dictionary<string, string>();
            {
                if(!PrimaryKey.IsNullOrEmpty())
                {
                    p.CheckAdd(PrimaryKey.ToString(), PrimaryKeyValue.ToString());
                }
            }

            if(resume)
            {
                if(BeforeGet!=null)
                {
                    var beforeGetResult=(bool)BeforeGet.Invoke(p);
                    if(!beforeGetResult)
                    {
                        resume=false;
                    }
                }
            }

            if(resume)
            {
                FrameTitle=GetFrameTitle();
            }

            if(resume)
            {
                ClearValues();

                if (OnGet != null)
                {
                    var onGetResult = OnGet.Invoke(this);
                    if(onGetResult)
                    {
                        AfterGet?.Invoke(this);
                    }   
                }
                else
                {
                    if(Mode != "create")
                    {
                        var qp=QueryGet;
                        if (qp != null)
                        {
                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", qp.Module);
                            q.Request.SetParam("Object", qp.Object);
                            q.Request.SetParam("Action", qp.Action);
                            q.Request.SetParams(p);

                            q.Request.Timeout = qp.Timeout;
                            q.Request.Attempts = qp.Attempts;

                            q.DoQuery();

                            if (q.Answer.Status == 0)
                            {
                                var answerData = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                                if (answerData != null)
                                {
                                    DataSetList=answerData;
                                    if (!String.IsNullOrEmpty(qp.AnswerSectionKey))
                                    {
                                        var ds = ListDataSet.Create(answerData, qp.AnswerSectionKey);
                                        SetValues2(ds);
                                    }

                                    Central.Dbg($"FormDialog AfterGet");
                                    AfterGet?.Invoke(this);
                                }
                            }
                            else
                            {
                                q.ProcessError();
                            }
                        }
                    }
                    else
                    {
                        Central.Dbg($"FormDialog AfterGet");
                        AfterGet?.Invoke(this);
                    }
                } 
            }
        }

        public void Delete()
        {
            var resume=true;

            var p = new Dictionary<string, string>();
            if (!PrimaryKey.IsNullOrEmpty())
            {
                p.CheckAdd(PrimaryKey.ToString(), PrimaryKeyValue.ToString());
            }

            if (resume)
            {
                if (OnGet != null)
                {
                    var onGetResult = OnGet.Invoke(this);
                    if (onGetResult)
                    {
                        p = Values;
                    }
                }
                else
                {
                    var qp = QueryGet;
                    if (qp != null)
                    {
                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", qp.Module);
                        q.Request.SetParam("Object", qp.Object);
                        q.Request.SetParam("Action", qp.Action);
                        q.Request.SetParams(p);

                        q.Request.Timeout = qp.Timeout;
                        q.Request.Attempts = qp.Attempts;

                        q.DoQuery();

                        if (q.Answer.Status == 0)
                        {
                            var answerData = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                            if (answerData != null)
                            {
                                DataSetList = answerData;
                                if (!String.IsNullOrEmpty(qp.AnswerSectionKey))
                                {
                                    var ds = ListDataSet.Create(answerData, qp.AnswerSectionKey);
                                    SetValues2(ds);
                                }
                            }
                        }
                        else
                        {
                            q.ProcessError();
                        }
                    }

                    p = Values;
                }
            }

            if (!PrimaryKey.IsNullOrEmpty())
            {
                p.CheckAdd(PrimaryKey.ToString(), PrimaryKeyValue.ToString());
            }

            if (resume)
            {
                if (BeforeDelete != null)
                {
                    var beforeDeleteResult = (bool)BeforeDelete.Invoke(p);
                    if (!beforeDeleteResult)
                    {
                        resume = false;
                    }
                }
            }

            if(resume)
            {
                if (OnGet != null)
                {
                    var onDeleteResult = OnDelete.Invoke(this);
                    if(onDeleteResult)
                    {
                        AfterDelete?.Invoke(this);
                    }   
                    else
                    {
                        resume=false;
                    }
                }
                else
                {
                    if (QueryDelete != null)
                    {
                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", QueryDelete.Module);
                        q.Request.SetParam("Object", QueryDelete.Object);
                        q.Request.SetParam("Action", QueryDelete.Action);
                        q.Request.SetParams(p);

                        q.Request.Timeout = QueryDelete.Timeout;
                        q.Request.Attempts = QueryDelete.Attempts;

                        q.DoQuery();

                        if (q.Answer.Status == 0)
                        {
                            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                            if (result != null)
                            {
                                AfterDelete?.Invoke(this);
                            }
                        }
                        else
                        {
                            q.ProcessError();
                        }
                    }
                } 
            }

            if(resume)
            {
                if(AfterUpdate!=null)
                {
                    AfterUpdate.Invoke(this);
                }
            }
        }

        private void Save()
        {
            var resume=true;
            var p = Form.GetValues();
            {
                if(!PrimaryKey.IsNullOrEmpty())
                {
                    p.CheckAdd(PrimaryKey.ToString(), PrimaryKeyValue.ToString());
                }
            }

            if(resume)
            {
                if(BeforeSave!=null)
                {
                    var beforeSaveResult=(bool)BeforeSave.Invoke(p);
                    if(!beforeSaveResult)
                    {
                        resume=false;
                    }
                }
            }
          
            if(resume)
            {
                if (OnSave != null)
                {
                    var onSaveResult = OnSave.Invoke(this);
                    if(onSaveResult)
                    {
                        AfterSave?.Invoke(this);
                    }   
                    else
                    {
                        resume=false;
                    }
                }
                else
                {   
                    var validationResult=Validate();
                    if(validationResult)
                    {
                        var qp=QuerySave;
                        if (qp != null)
                        {
                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", qp.Module);
                            q.Request.SetParam("Object", qp.Object);
                            q.Request.SetParam("Action", qp.Action);

                            q.Request.Timeout = qp.Timeout;
                            q.Request.Attempts = qp.Attempts;

                            q.Request.SetParams(p);

                            q.DoQuery();

                            if (q.Answer.Status == 0)
                            {
                                var answerData = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                                if (answerData != null)
                                {
                                    {
                                        var ds = ListDataSet.Create(answerData, "ITEMS");
                                        InsertId=ds.GetFirstItemValueByKey(PrimaryKey);
                                    }

                                    AfterSave?.Invoke(this);
                                }
                            }
                            else
                            {
                                q.ProcessError();
                            }
                        }
                    }
                    else
                    {
                        Form.SetStatus("Не все обязательные поля заполнены верно");
                        resume=false;
                    }
                }
            }

            if(resume)
            {
                if(AfterUpdate!=null)
                {
                    AfterUpdate.Invoke(this);
                }
            }
        }


        private void Close()
        {
            var frameName = GetFrameName();
            Central.WM.Close(frameName);
        }

        public void Hide()
        {
            var frameName = GetFrameName();
            Central.WM.Close(frameName);
        }

        public Style FindResource(string name)
        {
            return (Style)SaveButton.FindResource(name);
        }

        /// <summary>
        /// цепная реакция обновления данных в зависимом поле
        /// </summary>
        /// <param name="fieldPath"></param>
        public void ChainUpdate(string fieldPath)
        {
            FieldClearItems(fieldPath);
            FieldUpdateItems(fieldPath);
            if(Rendered)
            {
                FieldClearValue(fieldPath);
            }            
        }

        public void FieldUpdateItems(string fieldPath)
        {
            if(IsInitFields)
            {
                int i = 0;
                int n = Fields.Count;
                bool focus = false;

                for(i = 0; i < n; i++)
                {
                    var field = Fields[i];
                    if(field.Path == fieldPath)
                    {
                        field.UpdateItems();
                    }
                }
            }
        }

        public void FieldClearItems(string fieldPath)
        {
            if(IsInitFields)
            {
                int i = 0;
                int n = Fields.Count;
                bool focus = false;

                for(i = 0; i < n; i++)
                {
                    var field = Fields[i];
                    if(field.Path == fieldPath)
                    {
                        field.ClearItems();
                    }
                }
            }
        }

        public void FieldClearValue(string fieldPath)
        {
            FormHelperField f = null;

            if(IsInitFields)
            {
                int i = 0;
                int n = Fields.Count;
                bool focus = false;

                for(i = 0; i < n; i++)
                {
                    var field = Fields[i];
                    if(field.Path == fieldPath)
                    {
                        f = field;
                        field.ClearValue();
                    }
                }
            }

            if(f!=null)
            {
                if(!f.ActualValue.IsNullOrEmpty())
                {
                    FieldSetValue(fieldPath, f.ActualValue);
                }
                else
                {
                    FieldSetValue(fieldPath, "");
                }
            }
        }

        public void FieldSetValue(string fieldPath, string value)
        {
            var p = new Dictionary<string, string>();
            p.CheckAdd(fieldPath, value);
            Form.SetValues(p);
            //SetValues(p);
        }

        private void InitFields()
        {
            Central.Dbg($"FormDialog InitFields");

            if(!IsInitFields)
            {
                IsInitFields = true;

                int i=0;
                int n = Fields.Count;
                bool focus = false;

                for(i=0; i<n; i++)
                {
                    var field = Fields[i];
                    field.Form=Form;
                    
                    var render=false;

                    if( field.ControlType.ToLower() != "void")
                    {
                        render=true;                        
                    }

                    if(render)
                    {
                        var rowDefinition = new RowDefinition();
                        rowDefinition.Height = new GridLength(0, GridUnitType.Auto);
                        BodyGrid.RowDefinitions.Add(rowDefinition);

                        var fdc = new FormDialogControl();
                        fdc.Field = field;
                        fdc.FindResource = FindResource;
                        fdc.Init();

                        // вставка в макетную сетку формы
                        if (fdc.BorderLabel != null)
                        {
                            System.Windows.Controls.Grid.SetRow(fdc.BorderLabel, i);
                            System.Windows.Controls.Grid.SetColumn(fdc.BorderLabel, 0);
                            BodyGrid.Children.Add(fdc.BorderLabel);
                        }

                        if(fdc.BorderControl != null)
                        {
                            System.Windows.Controls.Grid.SetRow(fdc.BorderControl, i);
                            System.Windows.Controls.Grid.SetColumn(fdc.BorderControl, 1);
                            BodyGrid.Children.Add(fdc.BorderControl);
                        }

                        // фокус на первое поле
                        if(!focus)
                        {
                            if(fdc.BorderControl is TextBox)
                            {
                                fdc.BorderControl.Focus();
                                focus = true;
                            }
                        }
                    }
                }

                Form.SetFields(Fields);
                Form.SetDefaults();

                Form.ToolbarControl = FormToolbar;
                Form.StatusControl = Status;
            }
        }

        private string GetFrameTitle()
        {
            var result="";
            var a=Title;
            var b="";
            if(!PrimaryKey.IsNullOrEmpty())
            {
                if(!PrimaryKeyValue.IsNullOrEmpty())
                {
                    b=PrimaryKeyValue;
                }                
            }

            switch(Mode)
            {
                case "create":
                    result=$"{a}";
                    break;

                default:
                    result=$"{a} #{b}";
                    break;
            }

            if(!TitleCustom.IsNullOrEmpty())
            {
                result=TitleCustom;
            }

            return result;
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
            Central.WM.Show(GetFrameName(), FrameTitle, Closable, "add", this);
            Central.Dbg($"FormDialog Show");
        }

        public void Open()
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 1;
            Central.WM.Show(GetFrameName(), FrameTitle, Closable, "add", this);
            Central.Dbg($"FormDialog Open");            
        }

    }
}

