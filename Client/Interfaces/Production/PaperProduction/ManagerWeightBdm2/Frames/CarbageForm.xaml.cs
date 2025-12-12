using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// карточка истории взвешивания машины с мусором
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    /// <released>2024-05-20</released>
    /// <changed>2025-04-17</changed>
    public partial class CarbageForm : UserControl
    {
        public CarbageForm()
        {
            FrameName = "Carbage";
            ReceiverName = "CarbageList";
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();
            SetDefaults();
        }

        /// <summary>
        /// Папка для хранения отчетов работы агента
        /// </summary>
        static string ReportFilePath = "manager_weight_report";

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Имя вкладки, которая становится активной после закрытия фрейма
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// идентификатор записи, с которой работает форма
        /// </summary>
        public int ChgaId { get; set; }

        /// <summary>
        /// полученные значение от запросов
        /// </summary>
        public List<Dictionary<string, string>> DataList { get; set; }

        /// <summary>
        /// Старое значение веса нетто
        /// </summary>
        private int WeightEmptyOld = 0;

        /// <summary>
        /// Старое значение веса брутто
        /// </summary>
        private int WeightFullOld = 0;

        /// <summary>
        /// Старое значение веса факт
        /// </summary>
        private int WeightFactOld = 0;

        /// <summary>
        /// Старое значение флага измене6ния веса машины контролером
        /// </summary>
        private int AutoInputOld = 0;


        /// <summary>
        // инициализация компонентов
        /// </summary>
        public void Init()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="CHGA_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOMER_CAR",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control= NomerCar,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }, //не обязательно для заполнения
                },
                new FormHelperField()
                {
                    Path="CREATED_DTTM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control= CreatedDttm,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }, //не обязательно для заполнения
                },
                new FormHelperField()
                {
                    Path="STATUS_CAR",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control= StatusCar,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }, //не обязательно для заполнения
                },
                new FormHelperField()
                {
                    Path="DESCRIPTION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control= Description,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }, //не обязательно для заполнения
                },
                new FormHelperField()
                {
                    Path="WEIGHT_EMPTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control= WeightEmpty,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
//                    { FormHelperField.FieldFilterRef.Required, null },
                    { FormHelperField.FieldFilterRef.DigitOnly, null },
                  //  { FormHelperField.FieldFilterRef.IsNotZero, null },
                    }, // обязательно к заполнению
                },
                new FormHelperField()
                {
                    Path="WEIGHT_FULL",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control= WeightFull,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
  //                  { FormHelperField.FieldFilterRef.Required, null },
                    { FormHelperField.FieldFilterRef.DigitOnly, null },
                  //  { FormHelperField.FieldFilterRef.IsNotZero, null },
                    }, // обязательно к заполнению
                },
                new FormHelperField()
                {
                    Path="WEIGHT_FACT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control= WeightFact,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    { FormHelperField.FieldFilterRef.DigitOnly, null },
//                    { FormHelperField.FieldFilterRef.IsNotZero, null },
    //                { FormHelperField.FieldFilterRef.Required, null },
                    }, // обязательно к заполнению
                },
                new FormHelperField()
                {
                    Path="REGION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Region,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange=(FormHelperField f, string v)=>
                    {
                    },
                },
                new FormHelperField()
                {
                    Path="LANDFILL",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Landfill,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange=(FormHelperField f, string v)=>
                    {
                    },
                },
                new FormHelperField()
                {
                    Path="CONTAINER_NUM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="SelectBox",
                    Control= ContainerNum,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }, //не обязательно для заполнения
                },
                new FormHelperField()
                {
                    Path="CARBAGE_EMPTY_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CarbageEmptyCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control= Note,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }, //не обязательно для заполнения
                },

            };

            Form.SetFields(fields);
            Form.OnValidate = (bool valid, string message) =>
            {
                if (valid)
                {
                    Status.Text = "";
                }
                else
                {
                    Status.Text = "Не все поля заполнены верно";
                }
            };
            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = Status;

            //после установки значений
            Form.AfterSet = (Dictionary<string, string> v) =>
              {
                  //фокус на поле ввода названия датчика
                  WeightEmpty.Focus();
              };
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();

            //значения полей по умолчанию
            {
                {
                    var list = new Dictionary<string, string>();
                    list.Add("1", "БДМ1");
                    list.Add("2", "БДМ2");
                    list.Add("3", "Территория БДМ1");
                    list.Add("4", "Территория БДМ2");
                    list.Add("5", "Территория ЦЛТ");
                    list.Add("6", "ЦЛТ");
                    list.Add("7", "ПЭС от \"ламинации\"");
                    Region.Items = list;
                    //Region.SetSelectedItemByKey("1"); //SelectedItem = list.FirstOrDefault((x) => x.Key == "1");
                }

                {
                    var list = new Dictionary<string, string>();
                    list.Add("1", "Полигон Эко-Сити");
                    list.Add("2", "ДСК");
                    list.Add("3", "Стебаево");
                    list.Add("4", "ЦПП");
                    list.Add("5", "Пробная утилизация");
                    Landfill.Items = list;
                    //   Landfill.SetSelectedItemByKey("1"); //SelectedItem = list.FirstOrDefault((x) => x.Key == "1");
                }

                {
                    var list = new Dictionary<string, string>();
                    list.Add("1", "1");
                    list.Add("2", "2");
                    list.Add("3", "3");
                    list.Add("4", "4");
                    list.Add("5", "5");
                    ContainerNum.Items = list;
                    // ContainerNum.SelectedItem = list.FirstOrDefault((x) => x.Key == "1");
                }

                {
                    var list = new Dictionary<string, string>();
                    // старый алгоритм запуска машин с отходами
                    list.Add("1", "Приехала пустая машина");
                    list.Add("2", "Взвешена пустая машина");
                    list.Add("3", "Начата загрузка машины");
                    list.Add("4", "Взвешена полная машина");
                    list.Add("5", "Уехала полная машина");
                    // новый алгоритм запуска машин с отходами
                    // list.Add("11", "Выезжает ПОЛНАЯ машина");
                    // list.Add("21", "Взвешена ПОЛНАЯ машина");
                    list.Add("31", "Уехала ПОЛНАЯ машина");
                    //list.Add("32", "Приехала ПУСТАЯ машина");
                    //list.Add("41", "Взвешена ПУСТАЯ машина");
                    list.Add("51", "Рейс завершен");

                    StatusCar.Items = list;
                    //  StatusCar.SetSelectedItemByKey("1");
                }

                //{
                //    var list = new Dictionary<string, string>();
                //    list.Add("1", "Рейс внутри территории завода");
                //    Note.Items = list;
                //}

            }
        }

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Carbage",
                ReceiverName = "CarbageList",
                SenderName = "Carbage",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            /*
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;
            }
            */
        }

        /// <summary>
        /// редактирование записи
        /// </summary>
        /// <param name="p"></param>
        public void Edit(Dictionary<string, string> p)
        {
            ChgaId = p.CheckGet("CHGA_ID").ToInt();
            GetData();
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
            Central.WM.FrameMode = 0;

            var frameName = GetFrameName();
            Central.WM.Show(frameName, $"Машина {NomerCar.Text}", true, "add", this);
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            var frameName = GetFrameName();
            Central.WM.Close(frameName);

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
        }

        /// <summary>
        /// формирует уникальный идентификатор фрейма
        /// </summary>
        /// <returns></returns>
        public string GetFrameName()
        {
            string result = "";
            result = $"{FrameName}_{ChgaId}";
            return result;
        }

        /// <summary>
        /// получение данных с сервера
        /// </summary>
        public async void GetData()
        {
            DisableControls();

            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID", ChgaId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "PaperProduction");
                q.Request.SetParam("Object", "ManagerWeightBdm2");
                q.Request.SetParam("Action", "CarbageGet");
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
                        Form.SetValues(ds);
                        // сохраняем старое значение всех весов
                        WeightEmptyOld = ds.GetFirstItemValueByKey("WEIGHT_EMPTY").ToInt();
                        WeightFullOld = ds.GetFirstItemValueByKey("WEIGHT_FULL").ToInt();
                        WeightFactOld = ds.GetFirstItemValueByKey("WEIGHT_FACT").ToInt();
                        AutoInputOld = ds.GetFirstItemValueByKey("AUTO_INPUT").ToInt();
                        Show();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            EnableControls();
        }

        /// <summary>
        /// подготовка данных
        /// </summary>
        public void Save()
        {
            bool resume = true;
            string error = "";
            int auto_input = 0;

            if ((WeightEmptyOld != WeightEmpty.Text.ToInt())
            || (WeightFullOld != WeightFull.Text.ToInt()))
            {
                var dw = new DialogWindow($"Вы действительно хотите изменить вес машины [{NomerCar.Text}]?", "Сохранение данных", "Подтверждение изменения веса машины", DialogWindowButtons.NoYes);
                if (dw.ShowDialog() != true)
                {
                    return;
                }
            }

            //стандартная валидация данных средствами формы
            if (resume)
            {
                var validationResult = Form.Validate();
                if (!validationResult)
                {
                    resume = false;
                }
            }

            var v = Form.GetValues();
            v.CheckAdd("CHGA_ID", ChgaId.ToString());
            v.CheckAdd("ACCO_ID", Central.User.AccountId.ToString());

            if (WeightFullOld != WeightFull.Text.ToInt())
            {
                auto_input = 1;
            }

            if (WeightEmptyOld != WeightEmpty.Text.ToInt())
            {
                auto_input = 2;
            }

            if ((WeightEmptyOld != WeightEmpty.Text.ToInt())
            && (WeightFullOld != WeightFull.Text.ToInt()))
            {
                auto_input = 3;
            }

            if (auto_input != 0)
            {
             
                if (Note.Text.IsNullOrEmpty())
                {
                    resume = false;
                }
                
                
                // если предыдущие значения весов не совпадают с текущими, то сообщаем ОЭБ
                if (
                    (WeightEmptyOld != WeightEmpty.Text.ToInt())
                   || (WeightFullOld != WeightFull.Text.ToInt()))
                {
                    var fio = Central.User.Name;
                    var today = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                    var dt = v.CheckGet("CREATED_DTTM");
                    var note = v.CheckGet("NOTE");

                    var mes = $"Дата изменения {today}";
                    mes = mes.Append($"", true);
                    mes = mes.Append($"Пользователем {fio} были внесены изменения в веса для машины ИД=[{ChgaId}] [{NomerCar.Text}] от {dt}", true);
                    mes = mes.Append($"Пред. вес полной = {WeightFullOld} Тек. вес полной = {WeightFull.Text}", true);
                    mes = mes.Append($"Пред. вес пустой = {WeightEmptyOld} Тек. вес пустой = {WeightEmpty.Text}", true);
                    mes = mes.Append($"Пред. вес отходов = {WeightFactOld} Тек. вес отходов = {WeightFact.Text}", true);
                    mes = mes.Append($"", true);
                    mes = mes.Append($"Пояснение контролера", true);
                    mes = mes.Append($"{note}", true);

                    v.CheckAdd("MSG", mes);
                }
            }
            else
                v.CheckAdd("MSG", "");

            //отправка данных
            if (resume)
            {
                if (AutoInputOld == 0)
                {
                    v.CheckAdd("AUTO_INPUT", auto_input.ToString());
                }
                else
                    v.CheckAdd("AUTO_INPUT", AutoInputOld.ToString());

                SaveData(v);
            }
            else
            {
                Form.SetStatus(error, 1);
            }
        }

        /// <summary>
        /// отправка данных на сервер
        /// </summary>
        public async void SaveData(Dictionary<string, string> p)
        {

            DisableControls();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "PaperProduction");
            q.Request.SetParam("Object", "ManagerWeightBdm2");
            q.Request.SetParam("Action", "SaveCarbage");

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
                    // если предыдущие значения весов не совпадают с текущими, то протоколируем изменения
                    if (
                        (WeightEmptyOld != WeightEmpty.Text.ToInt())
                       || (WeightFullOld != WeightFull.Text.ToInt())
                       || (WeightFactOld != WeightFact.Text.ToInt()))
                    {
                        var fio = Central.User.Name;
                        var mes = $"Пользователем {fio} внесены изменения в веса для машины ИД=[{ChgaId}], [{NomerCar.Text}]";
                        mes = mes.Append($"Пред. вес пустой = {WeightEmptyOld} Тек. вес пустой = {WeightEmpty.Text}", true);
                        mes = mes.Append($"Пред. вес полной = {WeightFullOld} Тек. вес полной = {WeightFull.Text}", true);
                        mes = mes.Append($"Пред. вес отходов = {WeightFactOld} Тек. вес отходов = {WeightFact.Text}", true);
                        LogItemAdd(mes);
                    }

                    {
                        // Отправляем сообщение о необходимости обновить грид списка машин с мусором
                        Messenger.Default.Send(new ItemMessage()
                        {
                            ReceiverGroup = "Carbage",
                            ReceiverName = "CarbageList",
                            SenderName = "CarbageForm",
                            Action = "Refresh",
                            Message = "",
                        }
                        );
                        Close();
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
        }

        private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Save();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            WeightEmpty.Focus();
        }

        /// <summary>
        /// Запись лога при изменении веса
        /// </summary>
        public static bool LogItemAdd(string mes)
        {
            bool result = true;

            // папка с отчетами
            var tableDirectory = "carbage_log";

            var ReportWorking = new Dictionary<string, string>();

            ReportWorking.CheckAdd("MESSAGE", mes);
            ReportWorking.CheckAdd("ID_SCRAP", "");

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ITEMS", JsonConvert.SerializeObject(ReportWorking));
                p.CheckAdd("TABLE_NAME", ReportFilePath);
                p.CheckAdd("TABLE_DIRECTORY", tableDirectory);
                // 1=global,2=local,3=net
                p.CheckAdd("STORAGE_TYPE", "3");
                p.CheckAdd("PRIMARY_KEY", "ID");
                p.CheckAdd("PRIMARY_KEY_VALUE", DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-ffffff"));

            }


            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Service");
            q.Request.SetParam("Object", "LiteBase");
            q.Request.SetParam("Action", "SaveData");
            q.Request.SetParams(p);

            q.Request.Timeout = 1000;
            q.Request.Attempts = 1;
            q.DoQuery();

            if (q.Answer.Status != 0)
            {
                result = false;
            }

            return result;
        }


        /// <summary>
        /// вызывается при изменении веса нетто
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WeightEmpty_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateFactWith();
        }

        /// <summary>
        /// вызывается при изменении веса брутто 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WeightFull_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateFactWith();
        }

        /// <summary>
        ///  расчитываем фактический вес отходов (брутто -нетто)
        /// </summary>
        private void CalculateFactWith()
        {
            if ((!WeightEmpty.Text.IsNullOrEmpty())
                && (!WeightFull.Text.IsNullOrEmpty()))
            {
                var fact = WeightFull.Text.ToInt() - WeightEmpty.Text.ToInt();
                WeightFact.Text = fact.ToString();
            }
            else
            {
                WeightFact.Text = "";
            }

        }

        private void DefaultButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Note.Text = "Рейс внутри территории завода";
        }
    }
}
