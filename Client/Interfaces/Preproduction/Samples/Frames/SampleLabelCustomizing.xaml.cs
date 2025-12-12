using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
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

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Фома настройки содержимого ярлыка для образца
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleLabelCustomizing : ControlBase
    {
        public SampleLabelCustomizing()
        {
            DocumentationUrl = "/doc/l-pack-erp/preproduction/sample_accounting#label_customizing";

            OnLoad = () =>
            {
                InitializeComponent();

                InitForm();
                GetData();
            };
        }

        /// <summary>
        /// Список ID образцов, у которых настриивают содержиое ярлыка
        /// </summary>
        public List<int> SampleIdList { get; set; }

        private Dictionary<string, string> SampleValues { get; set; }

        FormHelper Form { get; set; }

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
                        Central.ShowHelp(DocumentationUrl);
                        break;
                }
            }
        }


        private void InitForm()
        {
            Form = new FormHelper();

            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="CUSTOMER_CHECKING",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=CustomerCheckbox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CUSTOMER_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CustomerName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SAMPLE_ID_COMPLETED_DT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=SampleCompletedDt,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SAMPLE_SIZE_CHECKING",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=SizeCheckbox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SAMPLE_SIZE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=SampleSize,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SAMPLE_TYPE_CHECKING",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=TypeCheckbox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SAMPLE_CLASS",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=SampleType,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CARDBOARD_CHECKING",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=CardboardCheckbox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CARDBOARD_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=SampleRaw,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QTY",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=SampleQty,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRINTING_INFO",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=PrintingInfo,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DELIVERY",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Delivery,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

        public async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "GetTaskReport");
            var l = string.Join(",", SampleIdList);
            q.Request.SetParam("ID_LIST", l);

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "SampleReport");
                    SampleValues = ds.Items[0];
                    // Если есть флаг скрывать марку, то показываем сокращенное название картона
                    string visibleCardboardName = SampleValues.CheckGet("CARDBOARD_NAME");
                    // Если картон для образца еще не определен, показываем картон, запрошенный клиентом
                    if (visibleCardboardName.IsNullOrEmpty())
                    {
                        visibleCardboardName = SampleValues.CheckGet("ORDER_CARDBOARD");
                    }

                    if (SampleValues.CheckGet("HIDE_MARK").ToBool())
                    {
                        visibleCardboardName = SampleValues.CheckGet("PROFILE_NAME");
                    }
                    SampleValues.CheckAdd("_CARDBOARD_NAME", visibleCardboardName);

                    var d = new Dictionary<string, string>();
                    d.Add("SAMPLE_ID_COMPLETED_DT", $"{SampleValues.CheckGet("DT_COMPLITED")} / {SampleValues.CheckGet("ID")}");
                    d.CheckAdd("CUSTOMER_CHECKING", "0");
                    CustomerName.IsReadOnly = true;
                    d.CheckAdd("CUSTOMER_NAME", SampleValues.CheckGet("CUSTOMER_NAME"));
                    d.CheckAdd("SAMPLE_SIZE_CHECKING", "0");
                    SampleSize.IsReadOnly = true;
                    d.CheckAdd("SAMPLE_SIZE", SampleValues.CheckGet("SAMPLE_SIZE"));
                    d.CheckAdd("SAMPLE_TYPE_CHECKING", "0");
                    SampleType.IsReadOnly = true;
                    d.CheckAdd("SAMPLE_CLASS", SampleValues.CheckGet("SAMPLE_CLASS"));
                    d.CheckAdd("CARDBOARD_CHECKING", "0");
                    SampleRaw.IsReadOnly = true;
                    d.CheckAdd("CARDBOARD_NAME", SampleValues.CheckGet("_CARDBOARD_NAME"));

                    var labelValues = SampleValues.CheckGet("LABEL_TEXT");
                    if (!labelValues.IsNullOrEmpty())
                    {
                        var v = labelValues.Split(';');
                        // покупатель
                        if (v[0].ToBool())
                        {
                            d.CheckAdd("CUSTOMER_CHECKING", "1");
                            CustomerName.IsReadOnly = false;
                            d.CheckAdd("CUSTOMER_NAME", v[1]);
                        }

                        // размер
                        if (v[2].ToBool())
                        {
                            d.CheckAdd("SAMPLE_SIZE_CHECKING", "1");
                            SampleSize.IsReadOnly = false;
                            d.CheckAdd("SAMPLE_SIZE", v[3]);
                        }

                        // класс
                        if (v[4].ToBool())
                        {
                            d.CheckAdd("SAMPLE_TYPE_CHECKING", "1");
                            SampleType.IsReadOnly = false;
                            d.CheckAdd("SAMPLE_CLASS", v[5]);
                        }

                        // картон
                        if (v[6].ToBool())
                        {
                            d.CheckAdd("CARDBOARD_CHECKING", "1");
                            SampleRaw.IsReadOnly = false;
                            d.CheckAdd("CARDBOARD_NAME", v[7]);
                        }

                        // Информация для ярлыка
                        if (v.Length > 8)
                        {
                            string printingInfo = v[8];
                            if (!printingInfo.IsNullOrEmpty())
                            {
                                d.CheckAdd("PRINTING_INFO", printingInfo);
                            }
                        }
                    }
                    Form.SetValues(d);
                }
            }
        }

        /// <summary>
        /// Формирует строку с данными для печати на ярлыке
        /// </summary>
        /// <returns></returns>
        private string MakeLabelText()
        {
            string result = "";
            // Проверяем, отмечен ли хотя бы один чекбокс, или есть ли текст в поле информации. Если да, формируем строку
            bool filled = (bool)CustomerCheckbox.IsChecked
                || (bool)SizeCheckbox.IsChecked
                || (bool)TypeCheckbox.IsChecked
                || (bool)CardboardCheckbox.IsChecked
                || !PrintingInfo.Text.IsNullOrEmpty();

            if (filled)
            {
                if ((bool)CustomerCheckbox.IsChecked)
                {
                    result = $"1;{CustomerName.Text}";
                }
                else
                {
                    result = "0;";
                }

                if ((bool)SizeCheckbox.IsChecked)
                {
                    result = $"{result};1;{SampleSize.Text}";
                }
                else
                {
                    result = $"{result};0;";
                }

                if ((bool)TypeCheckbox.IsChecked)
                {
                    result = $"{result};1;{SampleType.Text}";
                }
                else
                {
                    result = $"{result};0;";
                }

                if ((bool)CardboardCheckbox.IsChecked)
                {
                    result = $"{result};1;{SampleRaw.Text}";
                }
                else
                {
                    result = $"{result};0;";
                }

                result = $"{result};{PrintingInfo.Text}";
            }

            return result;
        }

        public void Show()
        {
            int id = SampleValues.CheckGet("ID").ToInt();
            string title = $"Настройка ярлыков {id}";
            ControlName = $"SampleLabelCustomizing{id}";
            Central.WM.AddTab(ControlName, title, true, "add", this);
            Central.WM.SetActive(ControlName);
        }

        public void Close()
        {
            Central.WM.RemoveTab(ControlName);
        }

        private async void Save()
        {
            var p = Form.GetValues();
            string labelText = MakeLabelText();

            // В БД можно сохранить только 256 символов. Если получилось больше - выведем предупреждение
            if (labelText.Length <= 256)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "Samples");
                q.Request.SetParam("Action", "SaveLabelText");
                var l = string.Join(",", SampleIdList);
                q.Request.SetParam("ID_LIST", l);
                q.Request.SetParam("LABEL_TEXT", labelText);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        Close();
                    }
                }
            }
            else
            {
                int c = 256 - 12 - CustomerName.Text.Length - SampleSize.Text.Length - SampleType.Text.Length - SampleRaw.Text.Length;
                var dw = new DialogWindow($"Сократите информационное сообщение. Можно записать только {c} символов, сейчас {PrintingInfo.Text.Length}", "Настройка ярлыка");
                dw.ShowDialog();
            }
        }

        private void CustomerCheckbox_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)CustomerCheckbox.IsChecked)
            {
                CustomerName.IsReadOnly = false;
            }
            else
            {
                CustomerName.IsReadOnly = true;
                CustomerName.Text = SampleValues.CheckGet("CUSTOMER_NAME");
            }
        }

        private void SizeCheckbox_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)SizeCheckbox.IsChecked)
            {
                SampleSize.IsReadOnly = false;
            }
            else
            {
                SampleSize.IsReadOnly = true;
                SampleSize.Text = SampleValues.CheckGet("SAMPLE_SIZE");
            }
        }

        private void TypeCheckbox_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)TypeCheckbox.IsChecked)
            {
                SampleType.IsReadOnly = false;
            }
            else
            {
                SampleType.IsReadOnly = true;
                SampleType.Text = SampleValues.CheckGet("SAMPLE_CLASS");
            }
        }

        private void CardboardCheckbox_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)CardboardCheckbox.IsChecked)
            {
                SampleRaw.IsReadOnly = false;
            }
            else
            {
                SampleRaw.IsReadOnly = true;
                SampleRaw.Text = SampleValues.CheckGet("_CARDBOARD_NAME");
            }
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
