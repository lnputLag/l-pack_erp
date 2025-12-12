using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Логика взаимодействия для PalletLocation.xaml
    /// </summary>
    public partial class PalletLocation : UserControl
    {
        public PalletLocation()
        {
            InitializeComponent();

            Id = 0;
            ReturnTabName = "";
            InitForm();
            SetDefaults();
        }

        /// <summary>
        /// ID накладной расхода поддонов
        /// </summary>
        private int Id;

        /// <summary>
        /// Данные для списка поддонов в накладной
        /// </summary>
        ListDataSet ItemsDS { get; set; }

        /// <summary>
        /// Форма редактирования расположения, количества в ремонте и под собственные нужды
        /// </summary>
        public FormHelper PalletLocationForm { get; set; }

        /// <summary>
        /// Имя вкладки, которая становится активной после закрытия формы редактирования
        /// </summary>
        public string ReturnTabName { get; set; }

        

        /// <summary>
        /// Инициализация формы
        /// </summary>
        private void InitForm()
        {
            PalletLocationForm = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="LOCATION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Location,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                         { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="REPAIR_QTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=InRepairQty,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        //{ FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="OWN_NEEDS_QTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=OwnNeedQty,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

            };
            PalletLocationForm.SetFields(fields);
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            FormStatus.Text="";
            PalletLocationForm.SetDefaults();

            
        }

        /// <summary>
        /// Формирование окна редактирования
        /// </summary>
        /// <param name="plexId">id поддона</param>
        public void Edit(int palId = 0)
        {
            Id = palId;
            GetData();
            Show();
        }

        private async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "Get");
            q.Request.SetParam("ID", Id.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    ItemsDS = ListDataSet.Create(result, "Record");
                    PalletLocationForm.SetValues(ItemsDS);
                }
            }
            else
            {
                q.ProcessError();
            }

        }

        

        /// <summary>
        /// Сохранение данных формы редактирования
        /// </summary>
        private async void Save()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "SaveLocation");
            q.Request.SetParam("ID", Id.ToString());
            q.Request.SetParams(PalletLocationForm.GetValues());

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    if (result.ContainsKey("Items"))
                    {
                        //отправляем сообщение Гриду о необходимости обновить данные
                        Messenger.Default.Send(new ItemMessage()
                        {
                            ReceiverGroup = "Stock",
                            ReceiverName = "PalletList",
                            SenderName = "PalletLocation",
                            Action = "Refresh",
                        });

                        Close();
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

        }


        private void Show()
        {
            string title = $"Поддон #{Id}";
            Central.WM.AddTab($"PalletLocation_{Id}", title, true, "add", this);
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        private void Close()
        {
            Central.WM.RemoveTab($"PalletLocation_{Id}");
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
                SenderName = "PalletLocation",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

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
