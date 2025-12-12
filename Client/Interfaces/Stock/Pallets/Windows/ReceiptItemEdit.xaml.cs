using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Окно ввода количества поддонов выбранного типа в накладной прихода
    /// </summary>
    /// <author>Рясной П.В.</author>
    public partial class ReceiptItemEdit : UserControl
    {
        public ReceiptItemEdit()
        {
            InitializeComponent();

            ReturnTabName = "";
            TabName = "PalletReceiptItem";

            PalletRefDS = new ListDataSet();
            Quantity = -1;
            DocQuantity = -1;
            Id = 0;
            UsedPalletIds = new List<int>();

            InitForm();
            SetDefaults();
        }

        /// <summary>
        /// Данные для выпадающего списка поддонов. Получаются извне
        /// </summary>
        public ListDataSet PalletRefDS { get; set; }

        /// <summary>
        /// Количество поддонов в акте
        /// </summary>
        public int DocQuantity;

        /// <summary>
        /// Количество принятых поддонов
        /// </summary>
        public int Quantity;

        /// <summary>
        /// Идентификатор поддона из справочника поддонов
        /// </summary>
        public int Id;

        public List<int> UsedPalletIds { get; set; }

        /// <summary>
        /// Форма редактирования поддонов
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Таб для возврата
        /// Если определен, фокус будет возвращен этому табу
        /// </summary>
        public string ReturnTabName { get; set; }

        /// <summary>
        /// Имя фрейма
        /// Техническое имя для идентификации в системе WM
        /// </summary>
        public string TabName { get; set; }

        /// <summary>
        /// Структура окна
        /// </summary>
        private Window Window { get; set; }

        /// <summary>
        /// Деструктор компонентов. Завершает вспомогательные процессы
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Pallets",
                ReceiverName = "",
                SenderName = "ExpenditureItemEdit",
                Action = "Closed",
            });

            GoBack();
        }

        /// <summary>
        /// Возврат на фрейм, откуда был вызван данный фрейм
        /// </summary>
        public void GoBack()
        {
            if (!string.IsNullOrEmpty(ReturnTabName))
            {
                Central.WM.SetActive(ReturnTabName, true);
                ReturnTabName = "";
            }
        }

        /// <summary>
        /// Инициалтзация формы редактирования
        /// </summary>
        private void InitForm()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="ID_PAL",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Pallet,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="DOC_QTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=DocQty,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Qty,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.IsNotZero, null },
                    },
                },
            };
            Form.SetFields(fields);

            //после окончания стандартной валидации
            Form.OnValidate = (bool valid, string message) =>
            {
                if (valid)
                {
                    //SaveButton.IsEnabled=true;
                    FormStatus.Text = "";
                }
                else
                {
                    //SaveButton.IsEnabled=false;
                    FormStatus.Text = "Не все поля заполнены верно";
                }
            };
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            FormStatus.Text = "";
            DocQty.Text = "";
            Qty.Text = "";
        }

        /// <summary>
        /// Запуск редактирования поддонов и их количества
        /// </summary>
        public void Edit(int idPal = 0)
        {
            Id = idPal;
            var palletRefItems = PalletRefDS.GetItemsList("ID", "NAME");
            Pallet.Items = palletRefItems;
            if (Id > 0)
            {
                foreach (var item in palletRefItems)
                {
                    if (item.Key.ToInt() == Id)
                    {
                        Pallet.SetSelectedItem(item);
                    }
                }
                // исключим себя из проверки на уникальность
                UsedPalletIds.Remove(Id);
            }
            if (Quantity > -1)
            {
                Qty.Text = Quantity.ToString();
            }
            if (DocQuantity > -1)
            {
                DocQty.Text = DocQuantity.ToString();
            }
            Show();
        }

        /// <summary>
        /// Отображение фрейма
        /// </summary>
        private void Show()
        {
            int w = (int)Width;
            int h = (int)Height;
            string title = "Поддоны";

            Window = new Window
            {
                Title = title,
                Width = w + 17,
                Height = h + 40,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow,
            };

            Window.Content = new Frame
            {
                Content = this,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };


            if (Window != null)
            {
                Window.Topmost = true;
                Window.ShowDialog();
            }

            Window.Closed += Window_Closed;
        }

        /// <summary>
        /// Закрытие фрейма
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, System.EventArgs e)
        {
            Destroy();
        }

        /// <summary>
        /// Сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.RemoveTab($"{TabName}_{Id}");

            if (Window != null)
            {
                Window.Close();
            }

            Destroy();
        }

        /// <summary>
        /// Валидация и дополнительные проверки перед сохранением
        /// </summary>
        private void Save()
        {
            if (Form.Validate())
            {
                var resume = true;
                var p = Form.GetValues();

                if (resume)
                {
                    if (UsedPalletIds.Contains(Pallet.SelectedItem.Key.ToInt()))
                    {
                        FormStatus.Text = "Такой поддон уже есть в накладной";
                        resume = false;
                    }
                }

                if (resume && !string.IsNullOrEmpty(DocQty.Text))
                {
                    var docQty = p.CheckGet("DOC_QTY").ToInt();
                    var qty = p.CheckGet("QTY").ToInt();
                    if (qty > docQty)
                    {
                        FormStatus.Text = "Принято больше, чем по документу";
                        resume = false;
                    }
                }

                if (resume)
                {
                    SaveData();
                }
            }

        }

        /// <summary>
        /// отправка данных
        /// </summary>
        public void SaveData()
        {
            var p = Form.GetValues();
            string qtyDefect = "";
            // если задано количество по документу, вычисляем количество брака
            if (!string.IsNullOrEmpty(DocQty.Text))
            {
                int docQty = p.CheckGet("DOC_QTY").ToInt();
                int qty = p.CheckGet("QTY").ToInt();
                qtyDefect = (docQty - qty).ToString();
            }

            // структура ответа совпадает со структурой данных поддонов в расходных накладных
            var result = new Dictionary<string, string>()
            {
                { "_ROWNUMBER", "0" },
                { "PLRI_ID", "0" },
                { "ID_PAL", Pallet.SelectedItem.Key },
                { "DOC_QTY", p.CheckGet("DOC_QTY") },
                { "QTY", p.CheckGet("QTY") },
                { "PRICE", "0" },
                { "RECORD_FLAG", "0" },
                { "NAME", Pallet.SelectedItem.Value },
                { "TOTAL", "0" },
                { "QTY_DEFECT", qtyDefect }
            };

            var action = "";
            if (Id == 0)
            {
                action = "Insert";
            }
            else
            {
                action = "Update";
            }

            //отправляем сообщение Гриду о добавлении строки
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Stock",
                ReceiverName = "PalletReceipt",
                SenderName = "ReceiptItemEdit",
                Action = action,
                ContextObject = result,

            });

            Close();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку сохранения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку отмены
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
