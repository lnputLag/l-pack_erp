using Client.Common;
using GalaSoft.MvvmLight.Messaging;
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

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Списание ТМЦ на гофроагрегате
    /// </summary>
    /// <author>Рясной П.В.</author>
    public partial class RollMaterialWriteOff : UserControl
    {
        public RollMaterialWriteOff()
        {
            InitializeComponent();

            Barcode="";
            MachineId=0;
            MaterialWriteOffInstanceKey="Client.Interfaces.Production.RollControl.MaterialWroteOff.InstanceId";
            InstanceId="";

            InitForm();
            SetDefaults();
        }

        public string Barcode { get; set; }
        public int MachineId { get; set; }
        private FormHelper Form { get; set; }
        private string MaterialWriteOffInstanceKey {get;set;}
        public string InstanceId {get;set;}
        
        public int Qty;
        private int MaterialId;
        
        private Dictionary<string,string> Material;

         /// <summary>
        /// Инициализация формы
        /// </summary>
        public void InitForm()
        {
            Form=new FormHelper();
            var fields=new List<FormHelperField>()
            {      
                new FormHelperField()
                { 
                    Path="ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",                    
                },
                new FormHelperField()
                { 
                    Path="BARCODE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",                    
                },
                new FormHelperField()
                { 
                    Path="MACHINE_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",                    
                },
                new FormHelperField()
                { 
                    Path="MULTIPLIER",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",                    
                },
                new FormHelperField()
                { 
                    Path="NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="TextBox",
                    Control=NameText,
                    Default="",
                },
                new FormHelperField()
                { 
                    Path="QUANTITY",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="TextBox",
                    Control=QuantityText,
                    Default="",
                },
                new FormHelperField()
                { 
                    Path="UNIT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="TextBox",
                    Control=UnitText,
                    Default="",
                },
            };

            Form.SetFields(fields);
            Form.StatusControl=FormStatus;            
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            Form.SetDefaults();
        }

        /// <summary>
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="Production",
                ReceiverName = "",
                SenderName = "RollMaterialWriteOff",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

       

        /// <summary>
        /// Начало редактироания данных для списания расходных материалов
        /// </summary>
        /// <param name="barcode"></param>
        public void Edit()
        {
            Form.SetValueByPath("MACHINE_ID",MachineId.ToString());
            Form.SetValueByPath("BARCODE",Barcode.ToString());
            GetData();
        }

        /// <summary>
        /// Получение данных из БД
        /// </summary>
        private async void GetData()
        {
            DisableControls();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Roll");
            q.Request.SetParam("Action", "GetMaterial");
            q.Request.SetParam("BARCODE", Barcode);

            q.Request.Timeout = 10000;
            q.Request.Attempts= 3;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    var complete = false;

                    if (ds.GetFirstItemValueByKey("ID").ToInt() != 0)
                    {
                        complete = true;
                    }

                       

                    if (complete)
                    {
                        // если пришел ответ для заказанного id
                        if(Central.SessionValues.ContainsKey(MaterialWriteOffInstanceKey))
                        {
                            var v=Central.SessionValues[MaterialWriteOffInstanceKey];
                            var k=v.CheckGet("INSTANCE_ID");
                            if(k == InstanceId)
                            {
                                
                                Form.SetValues(ds);
                                Show();

                            }
                        }
                    }
                    else
                    {
                        var h=new ErrorTouch();
                        var t = "";
                        h.Show(t,"Не удалось распознать штрих-код, проведите сканирование повторно", 5);
                    }
                    
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();
        }

        public void DisableControls()
        {
            Toolbar.IsEnabled=false;
        }

        public void EnableControls()
        {
            Toolbar.IsEnabled=true;
        }

        private void ChangeQuantity(int direction=0)
        {
            var v=Form.GetValues();
            var m=v.CheckGet("MULTIPLIER").ToInt();
            var q=v.CheckGet("QUANTITY").ToInt();
            if(m>0)
            {
                q=q+m*direction;
            }

            if(q<0)
            {
                q=0;
            }

            if(q>100)
            {
                q=100;
            }

            v.CheckAdd("QUANTITY",q.ToString());
            Form.SetValues(v);
        }

        public void Show()
        {
            Central.WM.Show($"MaterialWriteOff", "Списание ТМЦ", true, "add", this);
        }

        public void Close()
        {
            Central.WM.Close($"MaterialWriteOff");

        }

        private void Save()
        {
            bool resume = true;

            var v=Form.GetValues();
            var q=v.CheckGet("QUANTITY").ToInt();

            if(resume)
            {
                if(q==0)
                {
                    resume=false;
                    FormStatus.Text = "Задайте количество";
                }
            }

            if (resume)
            {
                SaveData(v);
            }
        }

        /// <summary>
        /// Передача данных в БД
        /// </summary>
        /// <param name="p"></param>
        private async void SaveData(Dictionary<string,string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Roll");
            q.Request.SetParam("Action", "WriteOffMaterial");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");    
                    var id=ds.GetFirstItemValueByKey("ID").ToInt();

                    var t="Списание расходных материалов";
                    if(id>0)
                    {
                        var h=new ErrorTouch();
                        h.OnClose=()=>
                        {
                            Close();
                        };
                        h.Show(t,"Списание произведено успешно", 2);
                    }
                    else
                    {
                        var h=new ErrorTouch();
                        h.OnClose=()=>
                        {
                            Close();
                        };
                        h.Show(t,"Списание не прошло", 5);

                        Central.SendReport("Списание расходных материалов не прошло",p,false,false);
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void KeyboardButtonInc_Click(object sender, RoutedEventArgs e)
        {
            var b=(Button)sender;
            var tag=b.Tag.ToString();
            tag=tag.MakeSafeName();
            switch(tag)
            {
                case "inc":
                    ChangeQuantity(1);
                    break;

                case "dec":
                    ChangeQuantity(-1);
                    break;
            }
        }






        //private void PlusButton_Click(object sender, RoutedEventArgs e)
        //{
        //    FormStatus.Text = "";
        //    Qty += Material["MULTIPLIER"].ToInt();
        //    QtyWMeasure.Text = Qty.ToString() + " " + Material["MEASURE"];
        //}

        //private void MinusButton_Click(object sender, RoutedEventArgs e)
        //{
        //    if (Qty > 0)
        //    {
        //        Qty -= Material["MULTIPLIER"].ToInt();
        //        QtyWMeasure.Text = Qty.ToString() + " " + Material["MEASURE"];
        //    }
        //    else
        //    {
        //        FormStatus.Text = "Задайте количество для списания";
        //    }
        //}
    }
}
