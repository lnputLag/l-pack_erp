using Client.Common;
using Client.Interfaces.Main;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Service.Printing
{
    /// <summary>
    /// интерфейс настройки принтеров, карточка принтера
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-09-22</released>
    /// <changed>2023-09-22</changed>
    public partial class _PrintingSettings : Window
    {
        public _PrintingSettings()
        {
            InitializeComponent();

            Id = "";
            FrameName = "printer";
            Title="Профиль принтера";

            Init();
            SetDefaults();
        }

        public FormHelper Form { get; set; }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// идентификатор записи, с которой работает форма
        /// (primary key записи таблицы)
        /// </summary>
        public string Id { get; set; }

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
                    Path="NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Name,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="DESCRIPTION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Description,
                    Default="",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRINTER_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=PrinterName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="COPIES",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Copies,
                    Default="1",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="WIDTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Width,
                    Default="210",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="HEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Height,
                    Default="297",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                
            };

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = Status;

            Form.AfterSet = (Dictionary<string, string> v) =>
            {
                //Name.Focus();
                //Name.SelectAll();
            };

        }

        public void ProcessCommand(string command)
        {
            command=command.ClearCommand();
            if(!command.IsNullOrEmpty())
            {
                switch(command)
                {
                    case "printer_select":
                    {
                        var printHelper=new PrintHelper();
                        printHelper.Init();
                        var p=printHelper.GetDictionaryFromPrintingSettings(printHelper.GetPrintingSettingsFromSystem());
                        Form.SetValues(p);
                    }
                        break;

                    case "save":
                    {
                        Save();
                    }
                        break;

                    case "cancel":
                    {
                        Hide();
                    }
                        break;

                    case "test_print":
                    {
                        TestPrint(0);
                    }
                        break;

                    case "test_preview":
                    {
                        TestPrint(1);
                    }
                        break;

                    case "test_save":
                    {
                        TestSave();
                    }
                        break;

                        
                }
            }
        }

        public void SetDefaults()
        {
            Form.SetDefaults();
            PalletId.Text="12139927";
        }

        /// <summary>
        /// создание новой записи
        /// </summary>
        public void Create()
        {
            Id = "";
            //GetData();
            SetDefaults();
            Open();
        }

        /// <summary>
        /// редактирвоание записи
        /// </summary>
        /// <param name="id"></param>
        public void Edit(string id)
        {
            Id = id;
            GetData();
        }

        /// <summary>
        /// удаление записи
        /// </summary>
        /// <param name="hostname"></param>
        public void Delete(string id)
        {
            Id = id;

            var t="Удаление записи";
            var m = "";
            m=m.Append("Удалить запись?",true);
            m=m.Append($"Имя=[{id}]",true);
            var d = "";
                
            var dialog = new DialogWindow(m, t, d, DialogWindowButtons.OKCancel);
            var dialogResult=(bool)dialog.ShowDialog();
            if(dialogResult)
            {
                DeleteData();
            }
        }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Open()
        {
            Show();
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Hide()
        {
           Close();
        }

        /// <summary>
        /// формирует уникальный идентификатор фрейма
        /// </summary>
        /// <returns></returns>
        public string GetFrameName()
        {
            string result = "";
            result = $"{FrameName}_{Id}";
            return result;
        }

     

        /// <summary>
        /// подготовка данных
        /// </summary>
        public void Save()
        {
            bool resume = true;
            string error = "";

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

            //отправка данных
            if (resume)
            {
                SaveData(v);
            }
            else
            {
                Form.SetStatus(error, 1);
            }
        }

        /// <summary>
        /// получение данных
        /// </summary>
        public async void GetData()
        {
            DisableControls();
            var p=Central.AppSettings.SectionFindRow("PRINTING_SETTINGS","NAME", Id);
            Form.SetValues(p);
            Open();
            EnableControls();
        }

        /// <summary>
        /// сохранение данных
        /// </summary>
        public async void SaveData(Dictionary<string, string> p)
        {
            DisableControls();
            Central.AppSettings.SectionAddRow("PRINTING_SETTINGS",p);
            Central.AppSettings.Store();

            Central.Msg.SendMessage(new ItemMessage()
            {
                ReceiverGroup = "Printing",
                ReceiverName = "PrintingSettingsList",
                SenderName = "PrintingSettings",
                Action = "Refresh",
                Message = "",
            });

            Hide();
            EnableControls();
        }

        /// <summary>
        /// удаление данных
        /// </summary>
        public async void DeleteData()
        {
            DisableControls();
            Central.AppSettings.SectionDeleteRow("PRINTING_SETTINGS","NAME", Id);
            Central.AppSettings.Store();

            Central.Msg.SendMessage(new ItemMessage()
            {
                ReceiverGroup = "Printing",
                ReceiverName = "PrintingSettingsList",
                SenderName = "PrintingSettings",
                Action = "Refresh",
                Message = "",
            });

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

        private void PrinterNameOnClick(object sender, RoutedEventArgs e)
        {
            ProcessCommand("printer_select");
        }

        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            var b=(Button)sender;
            if(b != null)
            {
                var t=b.Tag.ToString();
                ProcessCommand(t);
            }
        }


        private void TestPrint(int preview=0)
        {
            // 1=print,2=preview


            var palletId=PalletId.Text.ToInt();
            var printingProfileName=Name.Text;

            var v=new Dictionary<string,string>();            
            v.CheckAdd("preview",preview.ToString());
            v.CheckAdd("pallet_id",palletId.ToString());
            v.CheckAdd("printing_profile_name",printingProfileName);
            v.CheckAdd("copy","1");
            v.CheckAdd("debug","1");
        }

        private void TestSave(int preview=0)
        {
            // 1=print,2=preview


            var palletId=PalletId.Text.ToInt();
            var printingProfileName=Name.Text;

            var v=new Dictionary<string,string>();            
            v.CheckAdd("preview",preview.ToString());
            v.CheckAdd("pallet_id",palletId.ToString());
            v.CheckAdd("printing_profile_name",printingProfileName);
            v.CheckAdd("copy","1");
        }
        
    }
}
