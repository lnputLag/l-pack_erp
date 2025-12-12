using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using static Client.Common.LPackClientRequest;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// отходы, импорт данных по отходам
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2022-05-19</released>   
    /// <modified>2022-05-19</modified>   
    public partial class ServiceWaste:UserControl
    {
        public ServiceWaste()
        {
            FrameName = "service_waste";

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this,ProcessMessages);

            InitForm();
        }
       
        public FormHelper Form { get; set; }

        /// <summary>
        /// имя фрейма,
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        private void ProcessMessages(ItemMessage m)
        {
        }

        public void Show()
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 2;

            var frameName = FrameName;
            Central.WM.Show(frameName, "Отходы", true, "add", this);
        }

        /// <summary>
        /// отображение статьи в справочной системе
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/production/service/waste");
        }

        /// <summary>
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="ProductionTask",
                ReceiverName = "",
                SenderName = "ServiceWaste",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

           
        }

        public void InitForm()
        {
            Form=new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="FILE_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=File,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        //{ FormHelperField.FieldFilterRef.Required, null },
                    },
                },            
                new FormHelperField()
                {
                    Path="FILE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",                   
                },  
            };

            Form.SetFields(fields);
            Form.StatusControl=FormStatus;
            Form.ToolbarControl=Toolbar;
            Form.SetDefaults();

            Progress.Visibility=System.Windows.Visibility.Collapsed;
        }

        public void SelectFile()
        {
            var fd = new OpenFileDialog();
            var fdResult = (bool)fd.ShowDialog();
            if (fdResult)
            {
                var fileName = Path.GetFileName(fd.FileName);
                var filePath = fd.FileName;

                var v=new Dictionary<string,string>();
                v.CheckAdd("FILE_NAME",fileName.ToString());
                v.CheckAdd("FILE",  filePath.ToString());
                Form.SetValues(v);
            }
        }

        /// <summary>
        /// Сохранение данных: подготовка данных
        /// </summary>
        public async void Save()
        {
            bool resume = true;
            string error="";

            //стандартная валидация данных средствами формы
            if(resume)
            {
                var validationResult=Form.Validate();
                if(!validationResult)
                {
                    resume = false;
                    error="Не все обязательные поля заполнены верно";
                }
            }

            var v=Form.GetValues();   
            
            if(resume)
            {
                if(string.IsNullOrEmpty(v.CheckGet("FILE")))
                {
                    resume = false;
                    error="Пожалуйста, выберите файл";
                }
            }

            if(resume)
            {
                var f=v.CheckGet("FILE");
                if(!string.IsNullOrEmpty(f))
                {
                    var fileName = Path.GetFileName(f);
                    var fileExt = Path.GetExtension(f);
                    fileExt=fileExt.ToLower();
                    if(fileExt!=".xlsx")
                    {
                        resume = false;
                        error="Неподходящий тип файла";
                    }
                }
            }
           
            //все данные собраны, отправляем
            if(resume)
            {
                SaveData(v);
            }
            else
            {
                Form.SetStatus(error,1);
            }
        }

        /// <summary>
        /// Сохранение данных: отпарвка данных
        /// </summary>
        public async void SaveData(Dictionary<string,string> p)
        {
            Form.DisableControls();
            ClearResult();
            Progress.Visibility=System.Windows.Visibility.Visible;
            
            var q = new LPackClientQuery();
            q.Request.SetParam("Module","Production");
            q.Request.SetParam("Object","Cutter");
            q.Request.SetParam("Action","ImportWaste");
            q.Request.SetParams(p);

            q.Request.Type = RequestTypeRef.MultipartForm;
            q.Request.UploadFilePath = p.CheckGet("FILE");

            q.Request.Timeout = 300000;
            q.Request.Attempts = 1;

            AddResult($"Отправка файла.");

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    {
                        var ds = ListDataSet.Create(result, "METRICS");
                        ProcessResult(ds);
                    }
                }
            }
            else
            {
                //q.ProcessError();
                var t="";
                t=$"{t}\n {q.Answer.Error.Message}";
                ResultMetrics.Text=t;
            }

            Form.EnableControls();
            Progress.Visibility=System.Windows.Visibility.Collapsed;
        }

        public void ProcessResult(ListDataSet ds)
        {
            AddResult($"Результат:");
            if(ds.Items.Count>0)
            {
                foreach(Dictionary<string,string> row in ds.Items)
                {
                    var k=row.CheckGet("TITLE");
                    var v=row.CheckGet("VALUE");
                    AddResult($"    {k}:{v}");
                }
            }
        }

        public void AddResult(string txt)
        {
            var t=ResultMetrics.Text;
            t=$"{t}\n{txt}";
            ResultMetrics.Text=t;
        }

        public void ClearResult()
        {
            ResultMetrics.Text="";
        }
       

        private void ShowButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            
        }        

        private void HelpButton_Click_1(object sender,System.Windows.RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void FileSelectButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            SelectFile();
        }

        private void SaveButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Save();
        }
    }


}



