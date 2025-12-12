using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Примечание к производственному заданию на переработку
    /// </summary>
    /// <author>balchugov_dv</author>       
    public partial class ProcessingTaskNote : UserControl
    {
        public ProcessingTaskNote()
        {
            Id=0;
            FrameName="ProcessingNote";
            FrameTitle="Примечание для переработки";


            InitializeComponent();
            InitForm();
            SetDefaults();
        }

        /// <summary>
        /// форма
        /// </summary>
        public FormHelper Form { get; set; }
        /// <summary>
        /// уникальный идентификатор фрейма
        /// </summary>
        public string FrameName { get;set;}
        /// <summary>
        /// Заголовок фрейма
        /// </summary>
        public string FrameTitle { get;set;}
        /// <summary>
        /// ID редактируемой записи
        /// </summary>
        public int Id { get;set;}

        /// <summary>
        /// инициализация формы
        /// </summary>
        private void InitForm()
        {
            Form = new FormHelper();
            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Note,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };
            Form.SetFields(fields);
            Form.StatusControl=FormStatus;
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            Form.SetDefaults();
        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="Production",
                ReceiverName = "",
                SenderName = "ProcessingNote",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch(e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// редактирование
        /// </summary>
        public void Edit(int id)
        {
            Id=id;
            GetData();
        }

        /// <summary>
        /// Получение данных для отображения в полях формы
        /// </summary>
        public async void GetData()
        {
            var p=new Dictionary<string,string>();
            {
                p.CheckAdd("ID",Id.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "ProductionTaskProcessing");
            q.Request.SetParam("Action", "Get");
            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });


            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds=ListDataSet.Create(result,"ITEMS");
                    Form.SetValues(ds);
                    Show();  
                }
            }
            else
            {
                q.ProcessError();
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
            Form.EnableControls();
            
            var q = new LPackClientQuery();
            q.Request.SetParam("Module","Production");
            q.Request.SetParam("Object","ProductionTaskProcessing");
            q.Request.SetParam("Action","Save");

            q.Request.SetParams(p);
            
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
                        var ds = ListDataSet.Create(result, "ITEMS");
                        var id=ds.GetFirstItemValueByKey("ID").ToInt();
                        
                        if(id!=0)
                        {
                            //отправляем сообщение гриду о необходимости обновить данные
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "ProductionTask",
                                ReceiverName = "PositionList",
                                SenderName = "ProcessingNote",
                                Action = "Refresh",
                            });
                            Close();
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            Form.EnableControls();
        }

        public void Show()
        {
            Central.WM.FrameMode=2;
            Central.WM.Show($"{FrameName}_{Id}",$"{FrameTitle}",true,"add",this);
        }

        public void Close()
        {
            Central.WM.Close($"{FrameName}_{Id}");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }
    }
}
