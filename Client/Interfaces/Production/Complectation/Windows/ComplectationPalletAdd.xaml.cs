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

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Логика взаимодействия для ComplectationPalletAdd.xaml
    /// </summary>
    public partial class ComplectationPalletAdd : UserControl
    {
        public ComplectationPalletAdd(int defaultQuantityOnPallet)
        {
            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitializeComponent();
            //регистрация обработчика клавиатуры
            PreviewKeyDown += ProcessKeyboard;

            FrameName = "ComplectationPalletAdd";
            DefaultQuantityOnPallet = defaultQuantityOnPallet;

            InitForm();
            SetDefaults();
        }

        public ComplectationPalletAdd(int defaultQuantityOnPallet, int quantityOnPallet)
        {
            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitializeComponent();
            //регистрация обработчика клавиатуры
            PreviewKeyDown += ProcessKeyboard;

            FrameName = "ComplectationPalletAdd";
            DefaultQuantityOnPallet = defaultQuantityOnPallet;
            QuantityOnPallet = quantityOnPallet;

            InitForm();
            SetDefaults();
        }

        /// <summary>
        /// Техническое имя фрейма
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// Процессор форм
        /// </summary>
        public FormHelper FormHelper { get; set; }

        /// <summary>
        /// Количество продукции на поддоне по умолчанию
        /// </summary>
        public int DefaultQuantityOnPallet { get; set; }

        /// <summary>
        /// Количество продукции на поддоне после комплектации
        /// </summary>
        public int QuantityOnPallet { get; set; }

        /// <summary>
        /// Количество стоп на поддоне
        /// </summary>
        public int StackQuantity { get; set; }

        /// <summary>
        /// Толщина картона
        /// </summary>
        public double Thikness { get; set; }

        /// <summary>
        /// Флаг того, что работа с интерфейсом успешно завершена. Результат работы можно забрать из QuantityOnPallet
        /// </summary>
        public bool OkFlag { get; set; }

        /// <summary>
        /// Ид производсвтенного задания (proiz_zad.id_pz)
        /// </summary>
        public int ProductionTaskId { get; set; }

        /// <summary>
        /// Ид продукции (tovar.id2)
        /// </summary>
        public int ProductId { get; set; }

        public void InitForm()
        {
            FormHelper = new FormHelper();

            var fields = new List<FormHelperField>
                {
                    new FormHelperField
                    {
                        Path = "DEFAULT_QUANTITY_ON_PALLET",
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = DefaultQuantityOnPalletTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField
                    {
                        Path = "QUANTITY_ON_PALLET",
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = QuantityOnPalletTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.MinValue, 0},
                        },
                    },
                    new FormHelperField
                    {
                        Path = "STACK_QUANTITY",
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = StackQuantityTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField
                    {
                        Path = "THIKNESS",
                        FieldType = FormHelperField.FieldTypeRef.Double,
                        Control = ThiknessTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                };

            FormHelper.SetFields(fields);
        }

        public void SetDefaults()
        {
            FormHelper.SetValueByPath("DEFAULT_QUANTITY_ON_PALLET", $"{DefaultQuantityOnPallet}");

            if (QuantityOnPallet > 0)
            {
                FormHelper.SetValueByPath("QUANTITY_ON_PALLET", $"{QuantityOnPallet}");
            }
            else
            {
                FormHelper.SetValueByPath("QUANTITY_ON_PALLET", "0");
            }

            QuantityOnPalletTextBox.Focus();
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
            Central.WM.FrameMode = 2;
            ThiknessTextBoxBorder.Visibility = Visibility.Collapsed;
            ThiknessLabelBorder.Visibility = Visibility.Collapsed;
            StackQuantityTextBoxBorder.Visibility = Visibility.Collapsed;
            StackQuantityLabelBorder.Visibility = Visibility.Collapsed;
            GetQuantityByScanerButton.Visibility = Visibility.Collapsed;

            Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
            windowParametrs.Add("no_resize", "1");
            windowParametrs.Add("center_screen", "1");
            Central.WM.Show(FrameName, "Добавление поддона", true, "add", this, "top", windowParametrs);
        }


        /// <summary>
        /// отображение фрейма для комплектации ГА
        /// </summary>
        public void Show(bool complectationCorrugatingMachineFlag, double thikness, int stackQuantity, int productionTaskId, int productId)
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 2;
            ThiknessTextBoxBorder.Visibility = Visibility.Visible;
            ThiknessLabelBorder.Visibility = Visibility.Visible;
            StackQuantityTextBoxBorder.Visibility = Visibility.Visible;
            StackQuantityLabelBorder.Visibility = Visibility.Visible;
            GetQuantityByScanerButton.Visibility = Visibility.Visible;
            this.Height = 280;

            ProductionTaskId = productionTaskId;
            ProductId = productId;
            Thikness = thikness;
            StackQuantity = stackQuantity;
            FormHelper.SetValueByPath("STACK_QUANTITY", $"{StackQuantity}");
            FormHelper.SetValueByPath("THIKNESS", $"{Thikness}");

            Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
            windowParametrs.Add("no_resize", "1");
            windowParametrs.Add("center_screen", "1");
            Central.WM.Show(FrameName, "Добавление поддона", true, "add", this, "top", windowParametrs);
        }

        public void Save()
        {
            if (FormHelper.Validate())
            {
                if ((!string.IsNullOrEmpty(QuantityOnPalletTextBox.Text)) && QuantityOnPalletTextBox.Text.ToInt() >= 0)
                {
                    OkFlag = true;
                    QuantityOnPallet = QuantityOnPalletTextBox.Text.ToInt();
                    Close();
                }
            }
        }

        /// <summary>
        /// Получить количество на поддоне по данным со сканера
        /// </summary>
        public async void GetQuantityByScaner()
        {
            if (StackQuantityTextBox.Text.ToInt() > 0 && ThiknessTextBox.Text.ToDouble() > 0)
            {
                var p = new Dictionary<string, string>();

                p.Add("KOL_PACK", StackQuantityTextBox.Text);
                p.Add("THIKNES", ThiknessTextBox.Text);
                p.Add("ID_PZ", ProductionTaskId.ToString());
                p.Add("ID2", ProductId.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Complectation");
                q.Request.SetParam("Object", "Pallet");
                q.Request.SetParam("Action", "PalletGetQty");

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
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds.Items.Count > 0)
                        {
                            QuantityOnPalletTextBox.Text = ds.Items.First().CheckGet("CARDBOARD_QTY");
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
            else
            {
                var msg = "Пожалуйста укажите количество стоп на поддоне и толщину картона";
                var d = new DialogWindow($"{msg}", "Комплектация ГА", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// обработчик нажатий клавиатуры
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessKeyboard(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    Save();
                    e.Handled = true;
                    break;

                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;
            }
        }

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Complectation",
                ReceiverName = "",
                SenderName = "ComplectationPalletAdd",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
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

        /// <summary>
        /// Вызов справки
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/production/");
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m">Сообщение</param>
        private void ProcessMessages(ItemMessage m)
        {

        }

        private void GetQuantityByScanerButton_Click(object sender, RoutedEventArgs e)
        {
            GetQuantityByScaner();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }
    }
}
