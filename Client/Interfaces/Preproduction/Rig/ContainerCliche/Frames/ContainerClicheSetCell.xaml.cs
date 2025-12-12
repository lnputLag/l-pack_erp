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

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Форма назначения ячейки клише литой тары
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class ContainerClicheSetCell : ControlBase
    {
        public ContainerClicheSetCell()
        {
            InitializeComponent();

            InitForm();
        }

        /// <summary>
        /// Идентификатор клише
        /// </summary>
        public string ClicheItemIds { get; set; }
        /// <summary>
        /// Имя вкладки, которая вызвала открытие фрейма, и в которую возвращается фокус после закрытия фрейма
        /// </summary>
        public string ReceiverName { get; set; }
        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }
        /// <summary>
        /// Идентификатор заполненной ячейки
        /// </summary>
        private int CellId;

        private void InitForm()
        {
            Form = new FormHelper();
            //список полей формы
            var fields = new List<FormHelperField>
            {
                new FormHelperField()
                {
                    Path="MACHINE_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control = Machine,
                    ControlType = "SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    OnChange = (FormHelperField f, string v) =>
                    {
                        LoadCellItems();
                    },
                },
                new FormHelperField()
                {
                    Path="CELL_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control = CellNum,
                    ControlType = "SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
            };
            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

        /// <summary>
        /// Обработка команд
        /// </summary>
        /// <param name="command"></param>
        private void ProcessCommand(string command)
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
                }
            }
        }


        /// <summary>
        /// Получение данных для заполнения формы
        /// </summary>
        private async void GetData()
        {
            var clicheItemId = ClicheItemIds.Split(',');
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "ContainerRig");
            q.Request.SetParam("Action", "GetClicheRack");
            q.Request.SetParam("ID", clicheItemId[0]);

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
                    var machineDS = ListDataSet.Create(result, "MACHINE_LIST");
                    Machine.Items = machineDS.GetItemsList("ID", "NAME");

                    var clicheDS = ListDataSet.Create(result, "ITEM");
                    Form.SetValues(clicheDS);
                    var cellId = clicheDS.Items[0].CheckGet("CELL_ID").ToInt();
                    if (cellId > 0)
                    {
                        CellId = cellId;
                    }

                    Show();
                }
            }
        }

        /// <summary>
        /// Загрузка списка доступных ячеек
        /// </summary>
        private async void LoadCellItems()
        {
            var clicheItemIds = ClicheItemIds.Split(',');
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "ContainerRig");
            q.Request.SetParam("Action", "ListCellForCliche");
            q.Request.SetParam("ID", clicheItemIds[0]);
            q.Request.SetParam("MACHINE_ID", Machine.SelectedItem.Key);

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
                    var cellDS = ListDataSet.Create(result, "CELL_LIST");
                    CellNum.Items = cellDS.GetItemsList("ID", "CELL_NUM");
                    if (CellId > 0)
                    {
                        CellNum.SetSelectedItemByKey(CellId.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Запуск редактирования клише
        /// </summary>
        /// <param name="values"></param>
        public void Edit(Dictionary<string, string> values)
        {
            ClicheItemIds = values.CheckGet("IDS");
            CellId = values.CheckGet("CELL_ID").ToInt();
            GetData();
        }

        /// <summary>
        /// Отображение вкладки
        /// </summary>
        public void Show()
        {
            var clicheItemIds = ClicheItemIds.Split(',');
            ControlName = $"ContainerClicheCell{clicheItemIds[0]}";
            ControlTitle = $"Выбор ячейки клише {clicheItemIds[0]}";

            Central.WM.Show(ControlName, ControlTitle, true, "add", this);
            Central.WM.SetActive(ControlName);
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        public void Close()
        {
            Central.WM.RemoveTab(ControlName);
            Central.WM.SetActive(ReceiverName);
        }

        /// <summary>
        /// Сохранение заданной ячейки для клише
        /// </summary>
        public async void Save()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "ContainerRig");
            q.Request.SetParam("Action", "SaveCell");
            q.Request.SetParam("IDS", ClicheItemIds);
            q.Request.SetParam("CELL_ID", CellNum.SelectedItem.Key);

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
                    if (result.ContainsKey("ITEM"))
                    {
                        // Отправляем сообщение гриду о необходимости обновить таблицу
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "RigContainer",
                            ReceiverName = ReceiverName,
                            SenderName = "SetCell",
                            Action = "RefreshCliche",
                        });
                        Close();
                    }
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                Form.SetStatus(q.Answer.Error.Message, 1);
            }    
        }

        /// <summary>
        /// Обработка нажатия на кнопку
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
