using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Stock
{
    public partial class SetBalance : UserControl
    {
        public SetBalance()
        {
            InitializeComponent();

            Id = 0;
            ReturnTabName = "";

            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="i_qty",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Qty,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },

            };
            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = StatusBar;
            Form.SetDefaults();
        }

        /// <summary>
        /// ID накладной расхода поддонов
        /// </summary>
        private int Id;
        private int FactId;

        /// <summary>
        /// Форма редактирования расположения, количества в ремонте и под собственные нужды
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Имя вкладки, которая становится активной после закрытия формы редактирования
        /// </summary>
        public string ReturnTabName { get; set; }

        /// <summary>
        /// Формирование окна редактирования
        /// </summary>
        /// <param name="plexId">id поддона</param>
        public void Set(int palId, int qty, int factId)
        {
            Id = palId;
            FactId = factId;
            Qty.Text = qty.ToString();

            string title = $"Поддон #{Id}";
            Central.WM.AddTab($"SetBalance_{Id}", title, true, "add", this);
        }

        /// <summary>
        /// Сохранение данных формы редактирования
        /// </summary>
        private async void Save()
        {
            if (!Form.Validate())
            {
                Form.SetStatus("Не все обязательные поля заполнены верно", 1);
                return;
            }

            Form.SetBusy(true);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "SetBalance");
            q.Request.SetParam("i_id_pal", Id.ToString());
            q.Request.SetParam("i_fact_id", Id.ToString());
            q.Request.SetParams(Form.GetValues());

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = "Stock",
                    ReceiverName = "",
                    SenderName = "SetBalance",
                    Action = "PalletListRefresh",
                });
                Close();
            }
            else
            {
                q.ProcessError();
            }

            Form.SetBusy(false);
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        private void Close()
        {
            Central.WM.RemoveTab($"SetBalance_{Id}");
            Destroy();
        }

         /// <summary>
        /// Деструктор. Завершает вспомогательные процессы
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="Stock",
                ReceiverName = "",
                SenderName = "SetBalance",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //возвращаемся
            if (!string.IsNullOrEmpty(ReturnTabName))
            {
                Central.WM.SetActive(ReturnTabName, true);
                ReturnTabName = "";
            }
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
