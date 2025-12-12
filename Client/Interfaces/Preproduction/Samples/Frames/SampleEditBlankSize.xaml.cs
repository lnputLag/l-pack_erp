using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Окно изменения размеров развертки образца
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleEditBlankSize : UserControl
    {
        public SampleEditBlankSize()
        {
            InitializeComponent();
            InitForm();
            SetDefaults();
            LoadRef();
        }

        /// <summary>
        /// форма
        /// </summary>
        public FormHelper Form { get; set; }
        /// <summary>
        /// Название окна получателя сообщения
        /// </summary>
        public string ReceiverName;
        public string TabName;

        private Dictionary<string,string> FieldValues { get; set; }

        /// <summary>
        /// инициализация формы
        /// </summary>
        private void InitForm()
        {
            Form = new FormHelper();
            //список полей формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="BLANK_LENGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=BlankLength,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.IsNotZero, null },
                    },
                },
                new FormHelperField()
                {
                    Path="BLANK_WIDTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=BlankWidth,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.IsNotZero, null },
                    },
                },
                new FormHelperField()
                {
                    Path="ID_FEFCO",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Fefco,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="GLUING",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=GluingSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };
            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            Form.SetDefaults();
            ReceiverName = "";
            FieldValues = new Dictionary<string, string>();

            var gluingItems = new Dictionary<string, string>() {
                { "0", " " },
                { "1", "склеить" },
                { "2", "не клеить" },
                { "3", "сшить" },
                { "4", "склеить и сшить" },
            };
            GluingSelectBox.Items = gluingItems;
            GluingSelectBox.SetSelectedItemByKey("0");
        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "PreproductionSample",
                ReceiverName = "",
                SenderName = "SampleEditBlankSize",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
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

        private async void LoadRef()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PartitionTechnologicalMap");
            q.Request.SetParam("Action", "ListFEFCO");
            q.Request.Timeout = Central.Parameters.RequestTimeoutMax;
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
                    var fefcoDS = ListDataSet.Create(result, "ITEMS");
                    Fefco.Items = fefcoDS.GetItemsList("ID", "NAME");

                    Form.SetValues(FieldValues);
                }
            }
        }

        /// <summary>
        /// редактирование
        /// </summary>
        public void Edit(Dictionary<string, string> values)
        {
            FieldValues = values;
            TabName = $"SampleEditBlankSize{values.CheckGet("ID")}";
            Show();
        }

        /// <summary>
        /// Показывает окно
        /// </summary>
        private void Show()
        {
            string title = "Размеры развертки";
            Central.WM.AddTab(TabName, title, true, "add", this);
        }

        public async void Save()
        {
            var p = Form.GetValues();

            if (Form.Validate())
            {
                int l = p.CheckGet("BLANK_LENGTH").ToInt();
                int b = p.CheckGet("BLANK_WIDTH").ToInt();

                if (l > 0 && b > 0)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "Samples");
                    q.Request.SetParam("Action", "SaveBlankSize");
                    q.Request.SetParams(p);

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if (q.Answer.Status == 0)
                    {
                        //отправляем сообщение о закрытии окна
                        Messenger.Default.Send(new ItemMessage()
                        {
                            ReceiverGroup = "PreproductionSample",
                            ReceiverName = ReceiverName,
                            SenderName = "SampleEditBlankSize",
                            Action = "Refresh",
                        });
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "PreproductionSample",
                            ReceiverName = ReceiverName,
                            SenderName = "SampleEditBlankSize",
                            Action = "Refresh",
                        });
                        Close();
                    }

                }
                else
                {
                    Form.SetStatus("Заполните размеры", 1);
                }
            }
            else
            {
                Form.SetStatus("Не все поля заполнены верно", 1);
            }
        }

        public void Close()
        {
            Central.WM.RemoveTab(TabName);
            Central.WM.SetActive(ReceiverName);
            /*
            var window = this.Window;
            if (window != null)
            {
                window.Close();
            }
            */
            Destroy();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }
    }
}
