using Client.Common;
using Client.Interfaces.Main;
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
using Microsoft.Win32;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Форма редактирования данных по потребителю.
    /// Позволяет менять папки для хранения excel файлов тех карт.
    /// </summary>
    public partial class CustomerData : UserControl
    {
        public CustomerData()
        {
            InitializeComponent();

            FrameName = "CustomerData";

            ProcessPermissions();
            Init();
            SetDefaults();

            InitialDirectory = Central.GetStorageNetworkPathByCode("techcards");
            if (string.IsNullOrEmpty(InitialDirectory))
            {
                InitialDirectory = "\\\\192.168.3.243\\техкарты\\";
            }
        }

        public string RoleName = "[erp]partition_technological_map";

        /// <summary>
        /// Имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// ИД потребителя
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Датасет с данными по потребителю
        /// </summary>
        public ListDataSet CustomerDataSet { get; set; }

        /// <summary>
        /// Датасет с данными по групам потребителей
        /// </summary>
        public ListDataSet GroupDataSet { get; set; }

        /// <summary>
        /// Датасет с данными по еденицам измерений
        /// </summary>
        public ListDataSet IzmDataSet { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Начальная папка для выбора места сохранения тех карт
        /// </summary>
        public string InitialDirectory { get; set; }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel(this.RoleName);
            var userAccessMode = mode;
            switch (mode)
            {
                case Role.AccessMode.Special:
                    break;

                case Role.AccessMode.FullAccess:
                    break;

                case Role.AccessMode.ReadOnly:
                default:
                    break;
            }

            List<Button> buttons = UIUtil.GetVisualChilds<Button>(this.Content as DependencyObject);
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    var buttonTagList = UIUtil.GetTagList(button);
                    var accessMode = Acl.FindTagAccessMode(buttonTagList);
                    if (accessMode > userAccessMode)
                    {
                        button.IsEnabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="CUSTOMER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=NameTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ART",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ArticulTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CUSTOMER_SHORT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ShortNameTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CUSTGROUP",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=GroupSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PATHTK_NEW",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=NewDirectoryTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PATHTK",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=MainDirectoryTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PATHTK_ARCHIVE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ArchiveDirectoryTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="IDIZM",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=UnitsSelectBoxFirst,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="IDIZM2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=UnitsSelectBoxSecond,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            Form.SetFields(fields);
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();

            GetDataForSelectBoxes();
        }

        /// <summary>
        /// Получаем данные для заполнения селектбоксов
        /// </summary>
        public void GetDataForSelectBoxes()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PartitionTechnologicalMap");
            q.Request.SetParam("Action", "GetDataByCustomerSelectBoxes");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    GroupDataSet = ListDataSet.Create(result, "GROUPS");
                    IzmDataSet = CustomerDataSet = ListDataSet.Create(result, "IZM");

                    if (GroupDataSet.Items.Count > 0)
                    {
                        GroupSelectBox.SetItems(GroupDataSet, "ID", "NAME");
                    }

                    if (IzmDataSet.Items.Count > 0)
                    {
                        UnitsSelectBoxFirst.SetItems(IzmDataSet, "ID", "NAME");
                        UnitsSelectBoxSecond.SetItems(IzmDataSet, "ID", "NAME");
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show(int customerId = 0)
        {
            if (customerId > 0)
            {
                CustomerId = customerId;

                GetDataByCustomer();
            }

            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 2;

            Central.WM.Show(FrameName, "Потребитель", true, "add", this, null);
        }

        public void GetDataByCustomer()
        {
            var p = new Dictionary<string, string>();
            p.Add("CUST_ID", CustomerId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PartitionTechnologicalMap");
            q.Request.SetParam("Action", "GetCustomerData");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    CustomerDataSet = ListDataSet.Create(result, "ITEMS");

                    if (CustomerDataSet.Items.Count > 0)
                    {
                        FormSetData(CustomerDataSet);
                    }
                }
                else
                {

                }
            }
            else
            {
                q.ProcessError();
            }
        }

        public void FormSetData(ListDataSet customerDs)
        {
            var ds = customerDs.Items.First();

            NameTextBox.Text = ds.CheckGet("CUSTOMER");
            ShortNameTextBox.Text = ds.CheckGet("CUSTOMER_SHORT");
            ArticulTextBox.Text = ds.CheckGet("ART");
            NewDirectoryTextBox.Text = ds.CheckGet("PATHTK_NEW");
            MainDirectoryTextBox.Text = ds.CheckGet("PATHTK");
            ArchiveDirectoryTextBox.Text = ds.CheckGet("PATHTK_ARCHIVE");

            if (!string.IsNullOrEmpty(ds.CheckGet("CUSTGROUP")))
            {
                GroupSelectBox.SetSelectedItemByKey(GroupDataSet.Items.FirstOrDefault(x => x.CheckGet("NAME") == ds.CheckGet("CUSTGROUP")).CheckGet("ID"));
            }

            if (!string.IsNullOrEmpty(ds.CheckGet("IDIZM")))
            {
                UnitsSelectBoxFirst.SetSelectedItemByKey(ds.CheckGet("IDIZM"));
            }

            if (!string.IsNullOrEmpty(ds.CheckGet("IDIZM2")))
            {
                UnitsSelectBoxSecond.SetSelectedItemByKey(ds.CheckGet("IDIZM2"));
            }
        }

        /// <summary>
        /// Получаем данные для сохранения информации по потребителю
        /// </summary>
        public Dictionary<string, string> GetDataForSave()
        {
            Dictionary<string, string> formValues = new Dictionary<string, string>();

            if (Form != null)
            {
                if (Form.Validate())
                {
                    formValues = Form.GetValues();
                    if (!string.IsNullOrEmpty(GroupSelectBox.SelectedItem.Value))
                    {
                        formValues.Add("CUSTGROUP_NAME", GroupSelectBox.SelectedItem.Value);
                        formValues.Add("CUST_ID", CustomerId.ToString());
                    }
                }
            }

            return formValues;
        }
        
        /// <summary>
        /// Сохраняем данные по потребителю
        /// </summary>
        public void SaveData(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PartitionTechnologicalMap");
            q.Request.SetParam("Action", "UpdateCustomerData");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");

                    if (!string.IsNullOrEmpty(ds.Items.First().CheckGet("CUSTOMER")))
                    {
                        //var custId = ds.Items.First().CheckGet("CUST_ID").ToInt();

                        var msg = "Данные потребителя успешно сохранены";
                        var d = new DialogWindow($"{msg}", "ТК решётки в сборе", "", DialogWindowButtons.OK);
                        d.ShowDialog();

                        // Отправляем сообщение вкладке с тех картой о том, что необходимо обновить данные по потребителям
                        {
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "Preproduction",
                                ReceiverName = "TechnologicalMap",
                                SenderName = "NotchData",
                                Action = "UpdateCustomerList",
                                Message = "",
                            }
                        );
                        }
                    }
                    else
                    {
                        var msg = "Ошибка сохранения данных потребителя";
                        var d = new DialogWindow($"{msg}", "ТК решётки в сборе", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {

                }
            }
            else
            {
                q.ProcessError();
            }
        }

        public void SelectDirectory(int number = 0)
        {
            var fileName = "Сохранить в этой папке";
            var filePath = "";
            var fd = new OpenFileDialog();
            fd.Filter = "Directory | directory";
            fd.FileName = fileName;
            fd.ValidateNames = false;
            fd.CheckFileExists = false;
            fd.CheckPathExists = true;
            fd.InitialDirectory = InitialDirectory;
            var fdResult = fd.ShowDialog();
            fileName = fd.FileName;
            filePath = System.IO.Path.GetDirectoryName(fd.FileName);
            filePath = $"{filePath}\\";


            switch (number)
            {
                case 1:
                    NewDirectoryTextBox.Text = filePath;
                    break;

                case 2:
                    MainDirectoryTextBox.Text = filePath;
                    break;

                case 3:
                    ArchiveDirectoryTextBox.Text = filePath;
                    break;

                default:
                    break;
            }

           


            {
                //var fd = new SaveFileDialog();
                //bool fdResult = (bool)fd.ShowDialog();
                //if (fdResult)
                //{
                //    var path = fd.FileName;
                //}
            }

        }


        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.Close(FrameName);

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
        }

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Preproduction",
                ReceiverName = "",
                SenderName = "CustomerData",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var values = GetDataForSave();
            if (values.Count > 0)
            {
                SaveData(values);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void NewDirectorySelectButton_Click(object sender, RoutedEventArgs e)
        {
            int number = 1;
            SelectDirectory(number);
        }

        private void ArchiveDirectorySelectButton_Click(object sender, RoutedEventArgs e)
        {
            int number = 3;
            SelectDirectory(number);
        }

        private void MainDirectorySelectButton_Click(object sender, RoutedEventArgs e)
        {
            int number = 2;
            SelectDirectory(number);
        }
    }
}
