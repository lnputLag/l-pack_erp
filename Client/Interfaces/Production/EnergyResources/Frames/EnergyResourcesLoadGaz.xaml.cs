using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using DevExpress.XtraPrinting.Native;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OracleClient;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Client.Interfaces.Production.EnergyResources
{
    /// <summary>
    /// загрузка данных по газу из базы CNT за указанный период и по выбранному расходомеру
    /// </summary>
    /// <author>greshnyh_ni</author>
    public partial class EnergyResourcesLoadGaz : ControlBase
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

        // дата начала загрузки
        public string StartDt { get; set; }
        // дата окончания загрузки
        public string EndDt { get; set; }

        /// <summary>
        /// динамический массив  данных для данных из базы CNT по газу: 
        /// </summary>
        private double[] DataArrayTemp = new double[] { };


        public EnergyResourcesLoadGaz()
        {
            InitializeComponent();

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
                        Title = "Загрузить",
                        ButtonUse = true,
                        ButtonName = "LoadButton",
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

            // получение прав пользователя
            ProcessPermissions();

            Values = new Dictionary<string, string>();

        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            StartDttm.IsEnabled = true;
            EndDttm.IsEnabled = true;
        }

        private void FormInit()
        {
            Form = new FormHelper();

            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="START_DTTM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control= StartDttm,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }, //не обязательно для заполнения
                },
                new FormHelperField()
                {
                    Path="END_DTTM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control= EndDttm,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }, //не обязательно для заполнения
                },
                new FormHelperField()
                {
                    Path="ID_ST",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="SelectBox",
                    Control=Machines,
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
            var list = new Dictionary<string, string>();
            list.Add("3", "Общее потребление газа всем заводом");
            list.Add("2", "Общее потребеление газа первой площадкой");
            list.Add("4", "Потребление газа котлом 1 на 2-ой площадке");
            list.Add("6", "Потребление газа котлом 3 на 2-ой площадке");
            list.Add("1", "Потребеление газа миникательными первой площадки");
            list.Add("7", "Потребление газа котлом 2 на 2-ой площадке");
            list.Add("8", "Потребеление газа 3-им котлом 1-ой площадки");

            Machines.Items = list;

            Form.SetDefaults();
            FormStatus.Visibility = Visibility.Hidden;
        }

        public void Create()
        {
            Show();
        }

        public void Edit()
        {
            FrameTitle = $"Загрузка данных.";
            Machines.IsReadOnly = false;

            SetDefaults();
            Form.SetValues(Values);
            Show();
        }


        /// <summary>
        /// Проверки перед загрузкой данных из БД CNT
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
                    if (Machines.SelectedItem.Key.ToInt() == 0)
                    {
                        errorMsg = "Выберите расходомер";
                        resume = false;
                    }
                }

                if (!resume)
                {
                    Form.SetStatus(errorMsg, 1);
                }
                else
                {
                    var f = StartDttm.Text.ToDateTime();
                    var t = EndDttm.Text.ToDateTime();

                    if (resume)
                    {
                        if (DateTime.Compare(f, t) > 0)
                        {
                            var msg = "Дата начала должна быть меньше даты окончания.";
                            var d = new DialogWindow($"{msg}", "Проверка данных");
                            d.ShowDialog();
                            resume = false;
                        }
                    }

                    if (resume)
                    {
                        var s = $"Загружаем данные по {Machines.SelectedItem.Value} с {f} по {t}";
                        Log.Text = Log.Text.Append(s, true);
                        Log.ScrollToEnd();

                        GridLoadItems();

                    }
                }
            }
        }

        /// <summary>
        /// получение данных по газу
        /// </summary>
        public async void GridLoadItems()
        {

            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("FROM_DATE", StartDttm.Text);
                    p.Add("TO_DATE", EndDttm.Text);
                    p.Add("N_UUG", Machines.SelectedItem.Key.ToInt().ToString());
//                    p.Add("NAME", Machines.SelectedItem.Value);
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "EnergyResource");
                q.Request.SetParam("Action", "LoadGazFromCnt");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

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
                            var first = ds.Items.FirstOrDefault();
                            var cnt_load = first.CheckGet("LOAD_CNT").ToString();
                            var cnt_save = first.CheckGet("SAVE_CNT").ToString();
                            var s = $"\nЗагружено {cnt_load} записей. Обработано {cnt_save} записей.";
                            Log.Text = Log.Text.Append(s, true);
                            Log.ScrollToEnd();
                        }
                    }
                }
                else
                {
                    var s = q.Answer.Error.Description;
                    Log.Text = Log.Text.Append(s, true);
                    Log.ScrollToEnd();
                }
            }
        }
    }
}

