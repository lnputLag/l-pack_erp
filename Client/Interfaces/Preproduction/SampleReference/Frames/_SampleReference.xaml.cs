using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using static Client.Interfaces.Main.DataGridHelperColumn;
using Client.Interfaces.Production;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Окно редактирования данных эталонного образца
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleReference : ControlBase
    {
        /// <summary>
        /// Инициализация формы редактирования
        /// </summary>
        public SampleReference()
        {
            InitializeComponent();
            InitForm();

            OnLoad = () =>
            {
                Form.SetDefaults();
                GetData();
            };
        }

        /// <summary>
        /// Форма редактирования данных эталонного образца
        /// </summary>
        public FormHelper Form { get; set; }
        /// <summary>
        /// ID эталонного образца
        /// </summary>
        public int RefSampleId { get; set; }
        /// <summary>
        /// Часть артикула для имени вкладки
        /// </summary>
        public string RefSampleCode;
        /// <summary>
        /// ID техкарты, для которой сохраняют эталонные образец
        /// </summary>
        public int TechcardId { get; set; }

        /// <summary>
        /// Имя компонента, откуда вызвана форма
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// Обработка и выполнение команд
        /// </summary>
        /// <param name="command"></param>
        public void ProcessCommand(string command)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "save":
                        Save();
                        break;
                    case "close":
                        Close();
                        break;
                }
            }
        }

        /// <summary>
        /// Инициализация формы
        /// </summary>
        private void InitForm()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="CREATED_DT",
                    FieldType=FormHelperField.FieldTypeRef.Date,
                    Control=CreatedDate,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="RACK_NUM",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=RackNum,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CELL_NUM",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CellNum,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="MACHINE_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Machine,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="COLOR1_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Color1,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="COLOR2_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Color2,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="COLOR3_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Color3,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="COLOR4_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Color4,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="COLOR5_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Color5,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="COLOR_IN1_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ColorIn1,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="COLOR_IN2_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ColorIn2,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DUCTOR1",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Ductor1,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DUCTOR2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Ductor2,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DUCTOR3",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Ductor3,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DUCTOR4",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Ductor4,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DUCTOR5",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Ductor5,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };
            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

        /// <summary>
        /// Получение данных для полей формы
        /// </summary>
        private async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "ReferenceSample");
            q.Request.SetParam("Action", "Get");
            q.Request.SetParam("SAMPLE_ID", RefSampleId.ToString());
            q.Request.SetParam("ID", TechcardId.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var res = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (res != null)
                {
                    //Список цветов техкарты
                    var colorsDS = ListDataSet.Create(res, "COLORS");
                    var colorList = new Dictionary<string, string>()
                    {
                        { "0", " " },
                    };
                    foreach (var color in colorsDS.Items)
                    {
                        int colorId = color.CheckGet("ID_CLR").ToInt();
                        colorList.Add(colorId.ToString(), color.CheckGet("NAME"));
                    }


                    Color1.Items = colorList;
                    Color2.Items = colorList;
                    Color3.Items = colorList;
                    Color4.Items = colorList;
                    Color5.Items = colorList;
                    ColorIn1.Items = colorList;
                    ColorIn2.Items = colorList;

                    var machineDS = ListDataSet.Create(res, "MACHINES");
                    var machineList = new Dictionary<string, string>()
                    {
                        { "0", " " },
                    };
                    foreach (var machine in machineDS.Items)
                    {
                        int machineId = machine.CheckGet("ID").ToInt();
                        machineList.Add(machineId.ToString(), machine.CheckGet("NAME"));
                    }

                    Machine.Items = machineList;

                    if (RefSampleId > 0)
                    {
                        var refSampleDS = ListDataSet.Create(res, "REFERENCE_SAMPLE");
                        var rec = refSampleDS.Items[0];

                        rec.CheckAdd("DUCTOR1", "0");
                        rec.CheckAdd("DUCTOR2", "0");
                        rec.CheckAdd("DUCTOR3", "0");
                        rec.CheckAdd("DUCTOR4", "0");
                        rec.CheckAdd("DUCTOR5", "0");

                        int machineId = rec.CheckGet("MACHINE_ID").ToInt();
                        // У Jb и Mr все секции дукторные
                        if (machineId.ContainsIn(8, 10))
                        {
                            rec.CheckAdd("DUCTOR1", "1");
                            rec.CheckAdd("DUCTOR2", "1");
                        }
                        else
                        {
                            int ductorPlace = rec.CheckGet("DUCTOR_PLACE").ToInt();
                            if (ductorPlace > 0)
                            {
                                rec.CheckAdd($"DUCTOR{ductorPlace}", "1");
                            }
                        }

                        Form.SetValues(refSampleDS);
                        SetFielsAvailable();
                    }
                }
            }
        }

        /// <summary>
        /// Отображение окна с формой
        /// </summary>
        public void Show()
        {
            string code = RefSampleId.ToString();
            if (!RefSampleCode.IsNullOrEmpty())
            {
                code = RefSampleCode;
            }

            ControlTitle = $"Эталонный образец {code}";
            Central.WM.AddTab($"ReferenceSample{RefSampleId}", ControlTitle, true, "add", this);
        }

        /// <summary>
        /// Обработчик нажатий клавиш
        /// </summary>
        private void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;

                case Key.Enter:
                    Save();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Сохранение значений полей формы в БД
        /// </summary>
        private void Save()
        {
            if (Form.Validate())
            {
                bool resume = true;
                var p = Form.GetValues();

                p.CheckAdd("TECHCARD_ID", TechcardId.ToString());
                p.CheckAdd("SAMPLE_ID", RefSampleId.ToString());

                //Проверяем количество заполненных цветов
                int colorCnt = Color1.Items.Count;
                int colorFilledQty = 0;
                for (int q = 1; q < 8; q++)
                {
                    int colorId = p.CheckGet($"COLOR{q}_ID").ToInt();
                    if (colorId == 0)
                    {
                        //Проверяем внутренние цвета
                        colorId = p.CheckGet($"COLOR_IN{q}_ID").ToInt();
                        if (colorId> 0)
                        {
                            colorFilledQty++;
                        }
                    }
                    else
                    {
                        colorFilledQty++;
                    }
                }
                if (colorFilledQty > colorCnt)
                {
                    resume = false;
                    Form.SetStatus("Задано красок больше, чем есть в техкарте", 1);
                }

                if (resume)
                {
                    //Расположение дуктора
                    int ductorPlace = 0;
                    int MachineId = Machine.SelectedItem.ToInt();
                    if (MachineId != 8 && MachineId != 10)
                    {
                        for (int i = 1; i < 6; i++)
                        {
                            if (p.CheckGet($"DUCTOR{i}").ToInt() == 1)
                            {
                                ductorPlace = i;
                            }
                        }
                    }
                    p.CheckAdd("DUCTOR_PLACE", ductorPlace.ToString());
                }

                if (resume)
                {
                    SaveData(p);
                }
            }

        }

        /// <summary>
        /// Отправка данных в базу
        /// </summary>
        /// <param name="data"></param>
        private async void SaveData(Dictionary<string, string> data)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "ReferenceSample");
            q.Request.SetParam("Action", "Save");
            q.Request.SetParams(data);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var resultData = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (resultData.Count > 0)
                {
                    //Если ответ не пустой, отправляем сообщение Гриду о необходимости обновить данные
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverGroup = "Preproduction",
                        ReceiverName = ReceiverName,
                        SenderName = ControlName,
                        Action = "Refresh",
                    });
                }

                Close();
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Настройка доступности полей
        /// </summary>
        private void SetFielsAvailable()
        {
            //Для Джамбы и Мартина дуктор блокируется
            int machineId = Machine.SelectedItem.Key.ToInt();
            bool isJambo = machineId == 8 || machineId == 10;

            Ductor1.IsEnabled = !isJambo;
            Ductor2.IsEnabled = !isJambo;
            Ductor3.IsEnabled = !isJambo;
            Ductor4.IsEnabled = !isJambo;
            Ductor5.IsEnabled = !isJambo;
        }

        /// <summary>
        /// Обновление отметок дуктора
        /// </summary>
        /// <param name="n"></param>
        private void UpdateDuctorPlace(int n)
        {
            var p = Form.GetValues();
            var v = new Dictionary<string, string>();
            for (int i = 1; i < 6; i++)
            {
                if ((i != n) && (p.CheckGet($"DUCTOR{i}").ToInt() == 1))
                {
                    v.CheckAdd($"DUCTOR{i}", "0");
                }
            }
            Form.SetValues(v);
        }

        /// <summary>
        /// Закрытие окна
        /// </summary>
        public void Close()
        {
            Central.WM.RemoveTab($"ReferenceSample{RefSampleId}");
            Central.WM.SetActive(ReceiverName);
        }

        /// <summary>
        /// Обработка нажатия на кнопку
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            var b = (Button)sender;
            if (b != null)
            {
                var t = b.Tag.ToString();
                ProcessCommand(t);
            }
        }

        private void UpdateAvailableFields(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            int machineId = Machine.SelectedItem.Key.ToInt();
            bool isJambo = machineId == 8 || machineId == 10;
            bool ductorBlocked = (bool)Ductor1.IsChecked && !Ductor1.IsEnabled;

            if (isJambo)
            {
                if (!ductorBlocked)
                {
                    Ductor1.IsChecked = true;
                    Ductor2.IsChecked = true;
                }
            }
            else
            {
                if (ductorBlocked)
                {
                    Ductor1.IsChecked = false;
                    Ductor2.IsChecked = false;
                }
            }

            SetFielsAvailable();
        }

        private void Ducktor_Click(object sender, RoutedEventArgs e)
        {
            var c = (CheckBox)sender;
            if (c != null)
            {
                if ((bool)c.IsChecked)
                {
                    int n = c.Name[c.Name.Length - 1].ToInt();
                    UpdateDuctorPlace(n);
                }
            }
        }
    }
}
