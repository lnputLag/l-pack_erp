using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Production.MoldedContainer
{
 
    /// <summary>
    /// выпуск поддона с готовой продукцией ЛТ
    /// </summary>
    /// <author>greshnyh_ni</author>
    /// <version>1</version>
    /// <released>2024-07-17</released>
    /// <changed>2024-09-10</changed>
    public partial class RecyclingPalletCreate : ControlBase
    {
        public RecyclingPalletCreate()
        {           
            InitializeComponent();
            
            ControlSection = "recycling_control";
            RoleName = "[erp]developer";
            ControlTitle = "Создание паллета с готовой продукцией";
            DocumentationUrl = "/doc/l-pack-erp/production/molded_container/machine_control";

            FrameName = ControlName;
            //FrameTitle = $"Новый паллет. Задание {Values.CheckGet("TASK_ID").ToString()}";
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
              //  Title.Text = FrameTitle;
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
            //ControlName = $"Новый паллет. Задание {Values.CheckGet("TASK_ID").ToString()}";
            FrameTitle = $"Новый паллет. Задание {Values.CheckGet("TASK_ID").ToInt().ToString()}";
            //ControlName = $"Новый паллет. Задание {Values.CheckGet("TASK_ID").ToInt().ToString()}";
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
                    //resume = false;
                    //error = "нет данных ORDER_POSITION_ID";
                }
            }

            if(resume)
            {
                SaveButton.IsEnabled = false;
                DataSave();
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
                // расчитываем количество шт на паллете, с учетом ранее созданных палет для текущего ПЗ

                // количество шт по заданию
                int qtyPz = row.CheckGet("TASK_QUANTITY").ToInt();
                // количество шт на паллете из ТК на изделие
                int qtyTk = row.CheckGet("PER_PALLET_QTY").ToInt();
                // количество всего созданных паллет по данному ПЗ
                int countPallet = row.CheckGet("CNT_LAST").ToInt();
                // остаток на последней паллете
                int kolLast = qtyTk;
                /*
                if (qtyTk > 0)
                {
                    // должно быть количество полных паллет (4635 шт. на паллете)
                    int countPalletFull = qtyPz / qtyTk; 

                    if ((countPallet + 1) > countPalletFull )
                    {
                        kolLast = qtyPz - ((countPalletFull * qtyTk));
                        
                        if (kolLast == 0)
                        {
                            FormStatus.Text = $"По текущему заданию [{qtyPz}] шт. созданы все паллеты [{countPallet}]";
                            FormStatus.Visibility = Visibility.Visible;
                        }
                    }
                }
                */               
                
                row.CheckAdd("QUANTITY", kolLast.ToString());
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


            var MachineId = Values.CheckGet("MACHINE_ID").ToInt();

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
            }

            // id_st  sklad num_place
            // 311 ЛТП 1
            // 321 ЛТЭ 1
            // 331 ЛТУ 1
            if (MachineId==311)
            {
                p.Add("SKLAD", "ЛТП");
                p.Add("NUM_PLACE", "1");
            }
            else if(MachineId==321)
            {
                p.Add("SKLAD", "ЛТЭ");
                p.Add("NUM_PLACE", "1");
            }
            else if(MachineId== 331)
            {
                p.Add("SKLAD", "ЛТУ");
                p.Add("NUM_PLACE", "1");
            }
            else if (MachineId == 312)
            {
                p.Add("SKLAD", "ЛТП");
                p.Add("NUM_PLACE", "2");
            }
            else if (MachineId == 322)
            {
                p.Add("SKLAD", "ЛТЭ");
                p.Add("NUM_PLACE", "2");
            }

            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            SaveButton.IsEnabled = true;

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



    }
}
