using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Назначение штанцформе ячейки хранения
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class CuttingStampSetCell : ControlBase
    {
        public CuttingStampSetCell()
        {
            InitializeComponent();
            InitForm();
        }

        /// <summary>
        /// ID образца
        /// </summary>
        int CuttingStampId { get; set; }
        /// <summary>
        /// ID ячейки, в которой находится элемент штанцформы
        /// </summary>
        int CellId { get; set; }

        /// <summary>
        /// Форма редактирования поддонов
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Имя вкладки, окуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// Структура окна
        /// </summary>
        private Window Window { get; set; }

        private ListDataSet CellDataSet = new ListDataSet();

        /// <summary>
        /// Инициализация формы редактирования
        /// </summary>
        private void InitForm()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="RACK_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=RackNum,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CELL_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CellNum,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
            };
            Form.SetFields(fields);
        }

        /// <summary>
        /// Получение данных из БД для формы редактирования
        /// </summary>
        private void GetData(Dictionary<string, string> values)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "CuttingStamp");
            q.Request.SetParam("Action", "ListAvailableCells");
            q.Request.SetParam("ID", CuttingStampId.ToString());

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var rackDS = ListDataSet.Create(result, "RACK_LIST");
                    RackNum.Items = rackDS.GetItemsList("RACK_ID", "RACK_NUM");
                    if (string.IsNullOrEmpty(values.CheckGet("RACK_ID")))
                    {
                        values.CheckAdd("RACK_ID", "1");
                    }

                    CellDataSet = ListDataSet.Create(result, "CELL_LIST");

                    Form.SetValues(values);
                    Show();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        private void SetCellListBySelectedRack()
        {
            int rackId = RackNum.SelectedItem.Key.ToInt();
            var list = new Dictionary<string, string>();
            foreach (var row in CellDataSet.Items)
            {
                if (row.CheckGet("RACK_NUM").ToInt() == rackId)
                {
                    list.Add(row.CheckGet("ID"), row.CheckGet("PLACE_NUM"));
                }
            }
            CellNum.Items = list;
        }

        /// <summary>
        /// Запуск редактирования номера ячейки
        /// </summary>
        public void Edit(Dictionary<string, string> values)
        {
            CuttingStampId = values.CheckGet("ID").ToInt();
            CellId = values.CheckGet("CELL_ID").ToInt();

            if (CuttingStampId > 0)
            {
                GetData(values);
            }
            else
            {
                var dw = new DialogWindow("Не определена полумуфта штанцформы", "Изменение ячейки");
                dw.SetIcon("alert");
                dw.ShowDialog();
            }
        }

        /// <summary>
        /// Показ окна
        /// </summary>
        public void Show()
        {
            int w = (int)Width;
            int h = (int)Height;
            string title = $"Изменение ячейки";

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
        }

        /// <summary>
        /// Закрытие окна
        /// </summary>
        public void Close()
        {
            var window = this.Window;
            if (window != null)
            {
                window.Close();
            }
        }

        /// <summary>
        /// Сохранение
        /// </summary>
        private void Save()
        {
            if (Form.Validate())
            {
                //отправляем сообщение с данными полей окна
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "Preproduction/Rig",
                    ReceiverName = ReceiverName,
                    SenderName = "SetCell",
                    Action = "SaveCell",
                    ContextObject = Form.GetValues(),
                });
                Close();
            }
            /*
            else
            {
                Form.SetStatus(1, "Не все поля заполнены верно");
            }
            */
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void RackNum_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SetCellListBySelectedRack();
        }
    }
}
