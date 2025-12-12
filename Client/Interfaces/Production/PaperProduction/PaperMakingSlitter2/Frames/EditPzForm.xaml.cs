using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Production.Corrugator;
using DevExpress.XtraPrinting.Native;
using Microsoft.Office.Interop.Excel;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// изменить производственное задание для выбранного рулона
    /// </summary>
    /// <author>greshnyh_ni</author>
    public partial class EditPzForm : ControlBase
    {
        private FormHelper Form { get; set; }

        /// <summary>
        /// Имя вкладки, откуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;
        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        public Dictionary<string, string> Values { get; set; }

        /// <summary>
        ///  idp рулона
        /// </summary>
        private int IdpRoll { get; set; }
        /// <summary>
        ///  номер рулона
        /// </summary>
        private string NumRoll { get; set; }
        /// <summary>
        ///  номер ПЗ
        /// </summary>
        private string Num { get; set; }
        /// <summary>
        ///  название изделия
        /// </summary>
        private string Name { get; set; }

        public EditPzForm(Dictionary<string, string> record = null)
        {
            InitializeComponent();
     
            if (record != null)
            {
                IdpRoll = record.CheckGet("IDP_ROLL").ToInt();
                NumRoll = record.CheckGet("ROLL_NUM").ToString();
                Num = record.CheckGet("NUM").ToString();
                Name = record.CheckGet("NAME").ToString();
            }

            ControlSection = "paper_machine_control";
            //  RoleName = "[erp]developer";
            DocumentationUrl = "/doc/l-pack-erp/production/molded_container/machine_control";

            FrameName = ControlName;

            FormInit();

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
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
                        HotKey = "Enter",
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
                }

                Commander.Init(this);
            }

            Values = new Dictionary<string, string>();

        }

        private void FormInit()
        {
            Form = new FormHelper();

            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="IDP_ROLL",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                },

                  new FormHelperField()
                {
                    Path="ID_PZ",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ListPz,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
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
            FrameTitle = $"Изменения ПЗ для рулона №{NumRoll}  {Name} старое ПЗ №{Num}";
            DataGet();
        }

        /// <summary>
        /// получаем данные по простоям IdIdles
        /// </summary>
        private  void DataGet()
        {
            var complete = false;
            string error = "";

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("IDP_ROLL", IdpRoll.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "PmSlitter");
            q.Request.SetParam("Action", "PzNumSlitterList");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    // список ПЗ
                    var ds = ListDataSet.Create(result, "ITEMS");
                    ListPz.Items = ds.GetItemsList("ID_PZ", "NUM_PZ");
                    complete = true;

                }
            }
            else
            {
                //q.ProcessError();
                error = q.GetError();
            }

            if (complete)
            {

                Show();
            }
            else
            {
                LogMsg($"Ошибка при получении данных по рулону {error}");
            }
        }

        /// <summary>
        /// Проверки перед записью данных в БД
        /// </summary>
        private void Save()
        {
            if (Form.Validate())
            {
                bool resume = true;
                var v = Form.GetValues();
                string errorMsg = "";

                if (resume)
                {
                    if (ListPz.SelectedItem.Key.ToInt() <=0)
                        
                    {
                        errorMsg = "Не все поля заполнены верно";
                        resume = false;
                    }
                }

                if (resume)
                {
                    SaveData(v);
                }
                else
                {
                    Form.SetStatus(errorMsg, 1);
                }
            }
        }

        /// <summary>
        /// Запись данных в БД
        /// </summary>
        /// <param name="p"></param>
        private async void SaveData(Dictionary<string, string> p)
        {
            p.Add("IDP", IdpRoll.ToString());

            var q = new LPackClientQuery();

            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "PmSlitter");
            q.Request.SetParam("Action", "SaveIdPz");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "Production",
                    ReceiverName = ReceiverName,
                    SenderName = ControlName,
                    Action = "RefreshRolls",
                });
                Close();
            }
            else if (q.Answer.Error.Code == 145)
            {
                Form.SetStatus(q.Answer.Error.Message, 1);
            }
        }

        // выбрали запись из списка ПЗ
        private void ListPz_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (ListPz.SelectedItem.Key.ToInt() > 0)
            {
                string errorMsg = "";
                errorMsg = "";
                Form.SetStatus(errorMsg, 0);
            }

        }
    }
}
