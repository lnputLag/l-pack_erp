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

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс изменения приоритета выбранной сырьевой группы
    /// </summary>
    public partial class RawMaterialGroupEditPriority : UserControl
    {
        public RawMaterialGroupEditPriority(string idRawGroup, string paperWidth)
        {
            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitializeComponent();

            FrameName = "RawMaterialGroupEditPriority";
            IdRawGroup = idRawGroup;
            PaperWidth = paperWidth;

            Init();
            SetDefaults();
        }

        /// <summary>
        /// Имя фрейма
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Ид сырьевой группы
        /// </summary>
        public string IdRawGroup { get; set; }
        
        /// <summary>
        /// Формат сырьевой группы
        /// </summary>
        public string PaperWidth { get; set; }

        /// <summary>
        /// Инициализация компонентов
        /// </summary>
        public void Init()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="PRIORITY",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProiritySelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            Form.SetFields(fields);
        }

        public void SetDefaults()
        {
            RawMaterialGroupNameTextBox.Content = "";
            ProiritySelectBox.Items = new Dictionary<string, string>();
            Dictionary<string, string> listOfPriority = new Dictionary<string, string>();
            listOfPriority.Add("0", "Не использовать");
            listOfPriority.Add("1", "Низкий");
            listOfPriority.Add("2", "Средний");
            listOfPriority.Add("3", "Высокий");
            ProiritySelectBox.SetItems(listOfPriority);
        }

        /// <summary>
        /// Сохраняем данные по выбранному приоритету для выбранной сырьевой группы
        /// </summary>
        public void Save()
        {
            if (ProiritySelectBox.Items != null && ProiritySelectBox.Items.Count > 0 && ProiritySelectBox.SelectedItem.Key != null)
            {
                var p = new Dictionary<string, string>();
                p.Add("PRIORITY", ProiritySelectBox.SelectedItem.Key);
                p.Add("ID_RAW_GROUP", IdRawGroup);
                p.Add("PAPER_WIDTH", PaperWidth);

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "RawMaterialGroup");
                q.Request.SetParam("Action", "UpdatePriority");

                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    int idRawGroup = 0;

                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            idRawGroup = ds.Items.First().CheckGet("ID").ToInt();
                        }
                    }

                    if (idRawGroup == 0)
                    {
                        var msg = "Ошибка изменения приоритета";
                        var d = new DialogWindow($"{msg}", "Изменение приоритета сырьевой группы", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    else
                    {
                        // Отправляем сообщение на обновление данных по приоритету в выбранной сырьевой группе
                        {
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "Preproduction",
                                ReceiverName = "RawMaterialGroupList",
                                SenderName = "RawMaterialGroupEditPriority",
                                Action = "LoadData",
                                Message = "",
                                ContextObject = p,
                            }
                            );
                        }

                        Close();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m">Сообщение</param>
        private void ProcessMessages(ItemMessage m)
        {

        }

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Preproduction",
                ReceiverName = "",
                SenderName = "RawMaterialGroupEditPriority",
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
        /// отображение фрейма
        /// </summary>
        /// <param name="labelText">Текст, отображаемый в шапке формы</param>
        /// </param>
        public void Show(string labelText)
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 2;
            RawMaterialGroupNameTextBox.Content = labelText;

            Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
            windowParametrs.Add("no_resize", "1");
            windowParametrs.Add("center_screen", "1");
            Central.WM.Show(FrameName, labelText, true, "add", this, "top", windowParametrs);
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
