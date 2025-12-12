using Client.Common;
using GalaSoft.MvvmLight.Messaging;
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

namespace Client.Interfaces.Sales
{
    /// <summary>
    /// Окно выбора значения в заданном селектбоксе
    /// </summary>
    public partial class TechnologicalMapForSiteListApplicationSection : UserControl
    {
        public TechnologicalMapForSiteListApplicationSection(string labelText, Dictionary<string,string> selectBoxItems)
        {
            FrameName = "TechnologicalMapForSiteListApplicationSection";
            OkFlag = false;

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();
            SetDefaults();

            LabelText = labelText;
            SelectBoxDictionary = selectBoxItems;
            SetValues();
        }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Флаг того, что работа с интерфейсом успешно завершена
        /// </summary>
        public bool OkFlag { get; set; }

        /// <summary>
        /// Текст перед селектбоксом
        /// </summary>
        public string LabelText { get; set; }

        /// <summary>
        /// Наполнение селектбокса
        /// </summary>
        public Dictionary<string, string> SelectBoxDictionary { get; set; }

        /// <summary>
        /// Выбранная запись в селектбоксе
        /// </summary>
        public Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {
            //инициализация формы
            {
                Form = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                    {
                        new FormHelperField()
                        {
                            Path="SELECT_BOX",
                            FieldType=FormHelperField.FieldTypeRef.String,
                            Control=SelectBox,
                            ControlType="SelectBox",
                            Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            }
                        },
                    };

                Form.SetFields(fields);
            }
        }

        public void Save()
        {
            if (SelectedItem != null && SelectedItem.Count > 0 && !string.IsNullOrEmpty(SelectedItem.First().Value))
            {
                OkFlag = true;
                Close();
            }
            else
            {
                // msg
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            if (Form != null)
            {
                Form.SetDefaults();
            }

            LabelText = "";
            SelectBoxDictionary = new Dictionary<string, string>();
            SelectedItem = new Dictionary<string, string>();

            SetValues();
        }

        /// <summary>
        /// Передача значений в контролы
        /// </summary>
        public void SetValues()
        {
            Label.Content = LabelText;
            SelectBox.SetItems(SelectBoxDictionary);
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

            var dt = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");

            FrameName = $"{FrameName}_new_{dt}";

            Central.WM.Show(FrameName, "Выбор значения", true, "add", this);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {

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
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Sales",
                ReceiverName = "",
                SenderName = "TechnologicalMapForSiteListApplicationSection",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            //FIXME: Нужно сделать документацию
            Central.ShowHelp("/doc/l-pack-erp-new/application/online_shop/online_shop_tk");
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (SelectBox.SelectedItem.Value != null)
            {
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                dictionary.Add(SelectBox.SelectedItem.Key, SelectBox.SelectedItem.Value);

                SelectedItem = dictionary;

                SaveButton.IsEnabled = true;
            }
            else
            {
                SelectedItem = new Dictionary<string, string>();

                SaveButton.IsEnabled = false;
            }
        }
    }
}
