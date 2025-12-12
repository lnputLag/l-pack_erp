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
    /// меняем для рулона даты
    /// </summary>
    public partial class RollsEdit : UserControl
    {
        public RollsEdit()
        {
            InitializeComponent();

            Idr = 0;
            ReturnTabName = "";
            InitForm();
            SetDefaults();
        }

        /// <summary>
        /// ID накладной расхода рулона
        /// </summary>
        private int Idr;

        /// <summary>
        /// Данные для списка рулонов в накладной
        /// </summary>
        ListDataSet ItemsDS { get; set; }

        /// <summary>
        /// Форма редактирования
        /// </summary>
        public FormHelper RollsForm { get; set; }

        /// <summary>
        /// Имя вкладки, которая становится активной после закрытия формы редактирования
        /// </summary>
        public string ReturnTabName { get; set; }


        /// <summary>
        /// Инициализация формы
        /// </summary>
        private void InitForm()
        {
            RollsForm = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="IDR",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Idr,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                         { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="CLAIM_DT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ClaimDt,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        //{ FormHelperField.FieldFilterRef.Required, null },
                    },
                    First = true,
                },
                new FormHelperField()
                {
                    Path="ADJUSTMENT_DT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=AdjustmentDt,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

            };
            RollsForm.SetFields(fields);
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            FormStatus.Text = "";
            RollsForm.SetDefaults();
        }

        /// <summary>
        /// Формирование окна редактирования
        /// </summary>
        /// <param name="plexId">id поддона</param>
        public void Edit(int palId = 0)
        {
            Idr = palId;
            GetData();
            Show();
        }

        private async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Rolls");
            q.Request.SetParam("Action", "GetRecord");
            q.Request.SetParam("IDR", Idr.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    ItemsDS = ListDataSet.Create(result, "ITEMS");
                    RollsForm.SetValues(ItemsDS);
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

            var v = RollsForm.GetValues();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Rolls");
            q.Request.SetParam("Action", "ClaimSave");
            q.Request.SetParam("IDR", Idr.ToString());
            q.Request.SetParam("CLAIM_DT", v.CheckGet("CLAIM_DT"));
            q.Request.SetParam("ADJUSTMENT_DT", v.CheckGet("ADJUSTMENT_DT"));

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    //отправляем сообщение Гриду о необходимости обновить данные
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "Stock",
                        ReceiverName = "RollsList",
                        SenderName = "RollsEdit",
                        Action = "Refresh",
                    });

                    Close();
                }
            }
            else
            {
                q.ProcessError();
            }

        }

        private void Show()
        {
            string title = $"Рулон #{Idr}";
            Central.WM.AddTab($"RollsEdit_{Idr}", title, true, "add", this);
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        private void Close()
        {
            Central.WM.RemoveTab($"RollsEdit_{Idr}");
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
                ReceiverGroup = "Stock",
                ReceiverName = "",
                SenderName = "RollsEdit",
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
