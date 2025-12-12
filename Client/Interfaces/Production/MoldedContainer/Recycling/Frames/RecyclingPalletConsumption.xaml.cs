using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Production.MoldedContainer
{
 
    /// <summary>
    /// списание поддона с заготовками ЛТ для станка "Упаковщик"
    /// </summary>
    /// <author>greshnyh_ni</author>
    /// <version>1</version>
    /// <released>2024-07-17</released>
    /// <changed>2024-10-21</changed>
    public partial class RecyclingPalletConsumption : ControlBase
    {
        public RecyclingPalletConsumption()
        {           
            InitializeComponent();
            
            ControlSection = "recycling_control";
            RoleName = "[erp]developer";
            ControlTitle = "Списание паллеты с заготовками";
            DocumentationUrl = "/doc/l-pack-erp/production/molded_container/machine_control";

            FrameName = ControlName;
            PrimaryKeyValue = "0";

            OnMessage = (ItemMessage m) =>
            {
                if(m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (KeyEventArgs e) =>
            {
           
            };

            OnLoad = () =>
            {
                
            };

            OnUnload = () =>
            {
            };

            OnFocusGot = () =>
            {
              
            };

            OnFocusLost = () =>
            {
            };

            OnNavigate = () =>
            {
            };

            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "help",
                        Enabled = true,
                        Title = "Справка",
                        Description = "Показать справочную информацию",
                        ButtonUse = true,
                        ButtonName = "HelpButton",
                        HotKey = "F1",
                        Action = () =>
                        {
                            Central.ShowHelp(DocumentationUrl);
                        },
                    });
                }

                Commander.SetCurrentGroup("custom");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "ok",
                        Enabled = true,
                        Title = "ОК",
                        ButtonUse = true,
                        ButtonName = "SaveButton",
                        HotKey="Enter",
                        Action = () =>
                        {
                           Save();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "cancel",
                        Enabled = true,
                        Title = "Отмена",
                        ButtonUse = true,
                        ButtonName = "CancelButton",
                        HotKey = "Escape",
                        Action = () =>
                        {
                            Close();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "key_pressed",
                        Enabled = true,
                        ActionMessage = (ItemMessage message) =>
                        {
                            var m = message.Message;
                            Form.ProcessExtInput(m);

                        },
                    });
                }

                Commander.Init(this);
            }

            Values = new Dictionary<string, string>();

            FormInit();
        }

        private FormHelper Form { get; set; }
        public Dictionary<string,string> Values { get; set; }
        private int IdPoddon { get; set; }


        /// <summary>
        /// Имя вкладки, окуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;

        private void FormInit()
        {
            Form = new FormHelper();

            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="TASK_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="TASK_ID2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="GOODS_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="GOODS_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="TextBox",
                    Control=GoodsName,
                },                
                new FormHelperField()
                {
                    Path="MACHINE_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="ORDER_POSITION_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="QUANTITY",
                    First=true,
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="TextBox",
                    Control=Quantity,
                },
                new FormHelperField()
                {
                    Path="TASK_NUMBER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="void",
                },
            };
            Form.SetFields(fields);
            Form.StatusControl = FormStatus;            
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            Form.SetDefaults();
            FormStatus.Visibility = Visibility.Hidden; 
        }

        public void Create()
        {
            SetDefaults();
            Form.SetValues(Values);
            Show();
        }

        public void Edit()
        {
            FrameTitle = $"Паллет с заготовками. Задание {Values.CheckGet("TASK_ID").ToInt().ToString()}";
            IdPoddon = Values.CheckGet("ID_PODDON").ToInt();
            DataGet();
        }

        private void Save()
        {
            
            bool resume = true;
            string error = "";

            if(resume)
            {
                var validationResult = Form.Validate();
                if(!validationResult)
                {
                    resume = false;
                }
            }

            var v = Form.GetValues();

            if(resume)
            {
                if(v.CheckGet("TASK_ID2").ToInt() == 0)
                {
                    resume = false;
                    error = "нет данных TASK_ID2";
                }

                if(v.CheckGet("GOODS_ID").ToInt() == 0)
                {
                    resume = false;
                    error = "нет данных GOODS_ID";
                }

                if(v.CheckGet("QUANTITY").ToInt() == 0)
                {
                    resume = false;
                    error = "нет данных QUANTITY";
                }

                if(v.CheckGet("MACHINE_ID").ToInt() == 0)
                {
                    resume = false;
                    error = "нет данных MACHINE_ID";
                }
                
                if (v.CheckGet("ORDER_POSITION_ID").ToInt() == 0)
                {
               //     resume = false;
               //     error = "нет данных ORDER_POSITION_ID";
                }
            }

            if(resume)
            {

                // списываем заготовки с паллеты

                var p = new Dictionary<string, string>();
                {
                    p.Add("PALLET_ID", IdPoddon.ToString());
                    p.Add("ID_PZ", v.CheckGet("TASK_ID2").ToInt().ToString());
                    p.Add("ID_ST", v.CheckGet("MACHINE_ID").ToInt().ToString());
                    p.Add("I_QTY", v.CheckGet("QUANTITY").ToInt().ToString());
                    p.Add("PALLET_NUMBER_CUSTOM", v.CheckGet("TASK_NUMBER").ToString());
                }
               
                var res =  СonsumptionPallet(p);
                if (res)
                {
                    // создаем паллету с готовой продукцией
                    DataSave();
                }
            }
            else
            {
                LogMsg($"Ошибка при проверке формы [{ControlName}] {error}");
                Form.SetStatus(error, 1);
            }
            
        }

        /// <summary>
        /// получаем данные ПЗ по его prot_id (TASK_ID)
        /// </summary>
        private async void DataGet()
        {
            var complete = false;
            string error = "";
            var row = new Dictionary<string, string>();

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("TASK_ID", Values.CheckGet("TASK_ID"));
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "GetInfoPz");
            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if(q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if(result != null)
                {
                    
                    var ds = ListDataSet.Create(result, "ITEMS");
                    row = ds.GetFirstItem();
                    if (row != null)
                    {
                        if(
                            row.CheckGet("TASK_ID").ToInt() > 0
                            && row.CheckGet("TASK_ID2").ToInt() > 0
                        )
                        {
                            complete = true;
                        }
                    }
                }
            }
            else
            {
                //q.ProcessError();
                error = q.GetError();
            }

            if(complete)
            {
                row.CheckAdd("QUANTITY", Values.CheckGet("QUANTITY"));
                Form.SetValues(row);
                Show();
            }
            else
            {
                LogMsg($"Ошибка при получении данных по заданию {error}");
            }
        }

        /// <summary>
        /// создаем паллет с готовой продукцией
        /// </summary>
        /// <param name="p"></param>
        public async void DataSave()
        {
            var complete = false;
            string error = "";
            var row = new Dictionary<string, string>();
            var v = Form.GetValues();

            Form.DisableControls();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "CreatePallet");

            var p = new Dictionary<string, string>();
            {
                p.Add("PRODUCTION_TASK_ID", v.CheckGet("TASK_ID2").ToString());
                p.Add("PRODUCT_ID", v.CheckGet("GOODS_ID").ToString());
                p.Add("MACHINE_ID", v.CheckGet("MACHINE_ID").ToString());
                p.Add("ORDER_POSITION_ID", v.CheckGet("ORDER_POSITION_ID").ToString());
                p.Add("QUANTITY_ON_PALLET", v.CheckGet("QUANTITY").ToString());
                p.Add("SKLAD", "ЛТУ");
                p.Add("NUM_PLACE", "1");
            }

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
                    var ds = ListDataSet.Create(result, "ITEMS");

                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        var first = ds.GetFirstItem();
                        var id = first.CheckGet("PALLET_ID").ToInt();

                        if (id > 0)
                        {
                           row = first;

                            // в отладочном режиме отображение на экране
                            // в рабочем режиме сразу печать на принтере
                            if (!Central.DebugMode)
                            {
                                Stock.LabelReport2 report = new Stock.LabelReport2(true);
                                report.PrintLabel(id.ToString());
                                report.PrintLabel(id.ToString());
                            }

                            // оприходование паллеты с продукцией для текущего ПЗ
                            var p2 = new Dictionary<string, string>();
                            {
                                p2.Add("PALLET_ID", id.ToString());
                                p2.Add("PALLET_NUMBER_CUSTOM", v.CheckGet("GOODS_NAME").ToString());
                            }
                            ArrivialPallet(p2);

                            // Отправляем сообщение гриду c паллетами с готовой продукцией на обновление
                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverGroup = ControlSection,
                                ReceiverName = "",
                                SenderName = ControlName,
                                Action = "pallet_refresh",
                                Message = $"{row.CheckGet("PALLET_ID")}",
                                ContextObject = row,
                            });

                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverName = ReceiverName,
                                SenderName = ControlName,
                                Action = "refresh",
                            });

                            Close();
                        }
                    }
                }
            }
            else
            {
                //q.ProcessError();
                error = q.GetError();
                LogMsg($"Ошибка при создании паллеты {error}");
            }

            Form.EnableControls();
        }


        /// <summary>
        /// списание паллеты с заготовками
        /// </summary>
        private bool СonsumptionPallet(Dictionary<string, string> p)
        {
            var res = false;
            var palleta = p.CheckGet("PALLET_ID").ToString();
            var palletaName = p.CheckGet("PALLET_NUMBER_CUSTOM").ToString();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "PalletСonsumption");
            q.Request.SetParams(p);

            q.Request.Timeout = 5000;
            q.Request.Attempts = 1;

            //await Task.Run(() =>
            //{
            q.DoQuery();
            //});

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var dataSet = ListDataSet.Create(result, "ITEMS");
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        if (dataSet.Items.First().CheckGet("ID").ToInt() == 0)
                        {
                            // паллета списана успешно
                            string msg = $"Паллета [{palletaName}] списана успешно!{Environment.NewLine}.";
                            int status = 2;
                            var d = new StackerScanedLableInfo($"{msg}", status);
                            d.WindowMaxSizeFlag = true;
                            d.ShowAndAutoClose(2);
                            res = true;
                        }
                    }
                }
            }
            else  // if (q.Answer.Status == 145)
            {
                string msg = q.Answer.Error.Message;
                int status = 1;
                var d = new StackerScanedLableInfo(msg, status);
                d.WindowMaxSizeFlag = true;
                d.ShowAndAutoClose(2);
            }
            return res;
        }


        /// <summary>
        /// оприходование паллеты с продукцией для текущего ПЗ
        /// </summary>
        private void ArrivialPallet(Dictionary<string, string> p)
        {
            var palleta = p.CheckGet("PALLET_ID").ToString();
            var palletaName = p.CheckGet("PALLET_NUMBER_CUSTOM").ToString();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "PalletArrivial");
            q.Request.SetParams(p);

            q.Request.Timeout = 5000;
            q.Request.Attempts = 1;

            //await Task.Run(() =>
            //{
            q.DoQuery();
            //});

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var dataSet = ListDataSet.Create(result, "ITEMS");
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        if (dataSet.Items.First().CheckGet("ID").ToInt() == 0)
                        {
                            // паллет оприходован успешно
                            string msg = $"Паллета [{palletaName}] оприходована успешно!{Environment.NewLine}.";
                            int status = 2;
                            var d = new StackerScanedLableInfo($"{msg}", status);
                            d.WindowMaxSizeFlag = true;
                            d.ShowAndAutoClose(2);
                        }
                    }
                }
            }
            else if (q.Answer.Status == 145)
            {
                string msg = q.Answer.Error.Message;
                int status = 1;
                var d = new StackerScanedLableInfo(msg, status);
                d.WindowMaxSizeFlag = true;
                d.ShowAndAutoClose(2);
            }
            else if (q.Answer.Status == 7)
            {
                q.ProcessError();
                var error = q.GetError();
                LogMsg($"Ошибка при оприходовании паллеты. {error}");
            }
        }


    }
}
