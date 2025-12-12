using Client.Common;
using Client.Interfaces.Main;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Логика взаимодействия для MoldedContainerTechCardColor.xaml
    /// </summary>
    public partial class MoldedContainerTechCardColor : ControlBase
    {
        public MoldedContainerTechCardColor()
        {
            DocumentationUrl = "/doc/l-pack-erp/preproduction/tk_grid/molded_container";
            InitializeComponent();

            InitForm();
        }

        public Dictionary<string, string> SpotList { get; set; }

        public ListDataSet PrintingColorDS { get; set; }
        /// <summary>
        /// Форма редактирования задания
        /// </summary>
        FormHelper Form { get; set; }

        public string ReceiverName;

        /// <summary>
        /// Обработка и выполнение команд
        /// </summary>
        /// <param name="command"></param>
        public void ProcessCommand(string command, ItemMessage m = null)
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
                    case "help":
                        ShowHelp();
                        break;
                }
            }
        }

        /// <summary>
        /// Вызов справки
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/preproduction/sample_accounting");
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
                    Path="ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="SPOT_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=PrintingSpot,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="COLOR_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Color,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="ORDER_NUM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                },
            };
            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

        public void Edit(Dictionary<string, string> values)
        {
            ControlName = $"SpotColor_{values.CheckGet("TECHCARD_ID")}_{values.CheckGet("ID")}";
            var list = new Dictionary<string, string>();
            PrintingSpot.Items = SpotList;
            Color.Items = PrintingColorDS.GetItemsList("ID", "FULL_NAME");
            Form.SetValues(values);
            if (values.CheckGet("ORDER_NUM").ToInt() != 0)
            {
                PrintingSpot.IsReadOnly = true;
            }
            Show();
        }

        public void Show()
        {
            Central.WM.Show(ControlName, $"Печать для техкарты", true, "add", this);
        }

        public void Save()
        {
            if (Form.Validate())
            {
                var obj = Form.GetValues();
                int colorId = Color.SelectedItem.Key.ToInt();
                foreach (var item in PrintingColorDS.Items)
                {
                    if (item.CheckGet("ID").ToInt() == colorId)
                    {
                        obj.Add("COLOR_NAME", item.CheckGet("NAME"));
                        obj.Add("HEX", item.CheckGet("HEX"));
                        break;
                    }
                }

                Central.Msg.SendMessage(new ItemMessage() { 
                    ReceiverGroup = "PreproductionContainer",
                    ReceiverName= ReceiverName,
                    SenderName=ControlName,
                    Action="EditColor",
                    ContextObject=obj,
                });
                Close();
            }
        }

        public void Close()
        {
            Central.WM.Close(ControlName);
            Central.WM.SetActive(ReceiverName);
        }

        /// <summary>
        /// Обработчик нажатия на кнопку
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
    }
}
