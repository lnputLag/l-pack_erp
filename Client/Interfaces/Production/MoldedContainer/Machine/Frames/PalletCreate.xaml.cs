using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Xpf.Bars;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Client.Interfaces.Production.MoldedContainer
{
    /*
            главные механизмы работы:
            ->  Create          
                    Show
            ->  Edit
                    DataGet
                        Show
            --  Save
                    DataSave
                        Close

            --  ReceiptPrint    
     
     */



    /// <summary>
    /// создание поддона
    /// (выпуск поддона с продукцией)
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2024-07-17</released>
    /// <changed>2024-07-17</changed>
    public partial class PalletCreate : ControlBase
    {
        public PalletCreate()
        {           
            InitializeComponent();
            InitFlag = false;

            Keyboard.PreventSendKeyToCurrentControll = true;

            ControlSection = "machine_control";
            RoleName = "[erp]developer";
            ControlTitle = "Создание паллета";
            DocumentationUrl = "/doc/l-pack-erp/production/molded_container/machine_control";

            FrameName = ControlName;
            FrameTitle = "Новый паллет";
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
                if(!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };

            OnLoad = () =>
            {
            };

            OnUnload = () =>
            {
            };

            OnFocusGot = () =>
            {
                Title.Text = FrameTitle;
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
                            SaveButton.IsEnabled = false;
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
            InitFlag = true;
        }

        private FormHelper Form { get; set; }
        public Dictionary<string,string> Values { get; set; }
        private int Id2Cur { get; set; }
        
        private bool InitFlag { get; set; }

        private void FormInit()
        {
            Form = new FormHelper();

            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="MACHINE_NAME_SHORT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="TextBox",
                    Control=MachineName,
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
                    Path="PRODUCTION_TASK_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="PRODUCTION_TASK2_ID",
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
                    Path="MACHINE_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                },

            };
            Form.SetFields(fields);
            Form.StatusControl = FormStatus;            
        }

        private void SetDefaults()
        {
            SaveButton.IsEnabled = true;
            Form.SetDefaults();
            
        }

        public void Create()
        {
            SetDefaults();
            Form.SetValues(Values);
            Show();
        }

        private void Color1RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            Id2Cur = 521436;
            DataGet();
        }

        private void Color2RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            Id2Cur = 553489;
            DataGet();
        }

        public void Edit()
        {
            Id2Cur = Values.CheckGet("ID2").ToInt();
            DataGet();
        }

        private async void DataGet()
        {
            if (InitFlag == false)
                return;

            var complete = false;
            string error = "";
            var row = new Dictionary<string, string>();

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("MACHINE_ID", Values.CheckGet("MACHINE_ID"));
                p.CheckAdd("ID2", Id2Cur.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Machine");
            q.Request.SetParam("Action", "GetForPallet");
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
                    /*
                            prot.PROT_ID AS PRODUCTION_TASK_ID
                          , prot.id_pz AS PRODUCTION_TASK2_ID
                          , prot.id2 AS GOODS_ID
                          , prma.ID_ST AS MACHINE_ID
                          , prma.NAME AS MACHINE_NAME
                          , prma.SHORT_NAME AS MACHINE_NAME_SHORT
                     */

                    var ds = ListDataSet.Create(result, "ITEMS");
                    row = ds.GetFirstItem();

                    if (row != null)
                    {
                        if (
                            row.CheckGet("MACHINE_ID").ToInt() > 0
                            && row.CheckGet("PRODUCTION_TASK_ID").ToInt() > 0
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

            if (complete)
            {
                row.CheckAdd("QUANTITY", row.CheckGet("QUANTITY").ToString());
                Form.SetValues(row);

                // бурая заготовка
                if (Id2Cur == 521436)
                {
                    Color1RadioButton.IsChecked = true;
                }
                else // белая заготовка
                {
                    Color2RadioButton.IsChecked = true;
                }

                Show();
            }
            else
            {
                LogMsg($"Ошибка при получении данных станка {error}");
            }
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
                if (v.CheckGet("PRODUCTION_TASK_ID").ToInt() == 0)
                {
                    resume = false;
                    error = "нет данных PRODUCTION_TASK_ID";
                }

                if (v.CheckGet("PRODUCTION_TASK2_ID").ToInt() == 0)
                {
                    resume = false;
                    error = "нет данных PRODUCTION_TASK2_ID";
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

                if (v.CheckGet("MACHINE_ID").ToInt() == 0)
                {
                    resume = false;
                    error = "нет данных MACHINE_ID";
                }
            }

            if (resume)
            {
                SaveButton.IsEnabled = false;
                DataSave(v);
            }
            else
            {
                LogMsg($"Ошибка при проверке формы [{ControlName}] {error}");
                Form.SetStatus(error, 1);
            }
        }

        public async void DataSave(Dictionary<string, string> p)
        {
            var complete = false;
            string error = "";
            var row = new Dictionary<string, string>();
            var q = new LPackClientQuery();

            Form.DisableControls();

            {
                // устанавливаем текущее prod_id  
                q.Request.SetParam("Module", "MoldedContainer");
                q.Request.SetParam("Object", "Recycling");
                q.Request.SetParam("Action", "Save");
                q.Request.SetParam("TASK_ID", p.CheckGet("PRODUCTION_TASK_ID").ToString());
                q.Request.SetParam("PRODUCTION_MACHINE_ID", p.CheckGet("MACHINE_ID").ToString());

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    {
                        q = new LPackClientQuery();
                        q.Request.SetParam("Module", "MoldedContainer");
                        q.Request.SetParam("Object", "Recycling");
                        q.Request.SetParam("Action", "TaskStatusSave");

                        var p1 = new Dictionary<string, string>();
                        {
                            p1.Add("TASK_ID", p.CheckGet("PRODUCTION_TASK_ID").ToString());
                            p1.Add("PRTS_ID", "4");
                            p1.Add("SUSPEND_NOTE", "");
                        }
                        q.Request.SetParams(p1);

                        await Task.Run(() =>
                        {
                            q.DoQuery();
                        });

                        if (q.Answer.Status != 0)
                        {
                            q.ProcessError();
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            if (Color1RadioButton.IsChecked == true)
                p.CheckAdd("GOODS_ID", "521436");
            else
                p.CheckAdd("GOODS_ID", "553489");

            q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "Create");

            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            SaveButton.IsEnabled = true;

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if(result != null)
                {
                    /*
                        PALLET_NUMBER
                        PALLET_ID
                        PRODUCTION_TASK2_ID
                        MACHINE_ID
                        GOODS_SUB_FLAG
                        PRODUCTION_TASK_NUMBER  
                     */

                    var ds = ListDataSet.Create(result, "ITEMS");
                    var first = ds.GetFirstItem(); 
                    var id = first.CheckGet("PALLET_ID").ToInt();
                    complete = first.CheckGet("RESULT").ToBool();
                    if(complete)
                    {
                        row = first;
                    }
                }
            }
            else
            {
                // q.ProcessError();
                error = q.GetError();
                var i = new ErrorTouch();
                i.Show("Информация", error, 5);
            }

            if (complete)
            {              

                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = ControlSection,
                    ReceiverName = "",
                    SenderName = ControlName,
                    Action = "pallet_created",
                    Message = $"{row.CheckGet("PALLET_ID")}",
                    ContextObject=row,
                });

                Close();
            }
            else
            {
                LogMsg($"Ошибка при создании поддона {error}");
            }

            Form.EnableControls();
        }


        private bool ReceiptPrint(int mode = 0, int rollId = 0)
        {
            bool result = false;

            if(rollId == 0)
            {
            }

            if(rollId != 0)
            {
                var receiptViewer = new RollReceiptViewer();
                receiptViewer.RollId = rollId;
                result = receiptViewer.Init();

                switch(mode)
                {
                    //просмотр
                    default:
                    case 1:
                    receiptViewer.Show();
                    break;

                    //печать
                    case 2:
                    receiptViewer.Print(true);
                    break;
                }
            }

            return result;
        }

        private void ChangeValue(string symbol)
        {
            if(!string.IsNullOrEmpty(symbol))
            {
                ///var s = Form.GetValueByPath("RESIDUE");

                //if (!Quantity.IsFocused)
                //{
                //    Quantity.Focus();
                //}

                var s = Form.GetValueByPath("QUANTITY");
                
                switch (symbol)
                {
                    //case "<":
                    case "BACK_SPACE":

                    if (!string.IsNullOrEmpty(s))
                    {
                        s = s.Substring(0, (s.Length - 1));
                    }
                    break;

                    default:
                    s = s + symbol;
                    break;
                }

                Form.SetValueByPath("QUANTITY", s);
            }
        }

        private void KeyboardButton1_Click(object sender, RoutedEventArgs e)
        {
            ChangeValue("1");
        }

        private void KeyboardButton2_Click(object sender, RoutedEventArgs e)
        {
            ChangeValue("2");
        }

        private void KeyboardButton3_Click(object sender, RoutedEventArgs e)
        {
            ChangeValue("3");
        }

        private void KeyboardButton4_Click(object sender, RoutedEventArgs e)
        {
            ChangeValue("4");
        }

        private void KeyboardButton5_Click(object sender, RoutedEventArgs e)
        {
            ChangeValue("5");
        }

        private void KeyboardButton6_Click(object sender, RoutedEventArgs e)
        {
            ChangeValue("6");
        }

        private void KeyboardButton7_Click(object sender, RoutedEventArgs e)
        {
            ChangeValue("7");
        }

        private void KeyboardButton8_Click(object sender, RoutedEventArgs e)
        {
            ChangeValue("8");
        }

        private void KeyboardButton9_Click(object sender, RoutedEventArgs e)
        {
            ChangeValue("9");
        }

        private void KeyboardButtonBackspace_Click(object sender, RoutedEventArgs e)
        {
            ChangeValue("<");
        }

        private void KeyboardButton0_Click(object sender, RoutedEventArgs e)
        {
            ChangeValue("0");
        }

        private void KeyboardButtonNext_Click(object sender, RoutedEventArgs e)
        {
        }

        private void TouchKeyboardNumber_OnPressKey(string key)
        {
            ChangeValue(key);
        }

 
    }
}
