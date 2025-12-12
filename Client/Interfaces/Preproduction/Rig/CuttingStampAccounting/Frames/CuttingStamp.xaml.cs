using Client.Common;
using Client.Interfaces.Main;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Редактирование штанцформы
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class CuttingStamp : ControlBase
    {
        public CuttingStamp()
        {
            InitializeComponent();
            DocumentationUrl = "/doc/l-pack-erp/preproduction/tk_grid/molded_container";

            StampItemDS = new ListDataSet();
            StampItemDS.Init();

            InitForm();

            OnLoad = () =>
            {
            };

            OnUnload = () =>
            {
            };

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverName == ControlName)
                {
                    ProcessMessage(msg);
                }
            };

            Commander.SetCurrentGroup("main");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "save",
                    Enabled = true,
                    Title = "Сохранить",
                    Description = "Сохранить/пересохранить и закрыть",
                    ButtonUse = true,
                    ButtonName = "SaveButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Save();
                    },
                    CheckEnabled = () =>
                    {
                        var result = true;

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "close",
                    Enabled = true,
                    Title = "Отмена",
                    Description = "Отмена",
                    ButtonUse = true,
                    ButtonName = "CancelButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Close();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "selectdrawingfile",
                    Enabled = true,
                    Title = "",
                    Description = "Выбрать файл чертежа",
                    ButtonUse = true,
                    ButtonName = "DrawingFileButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        SelectDrawingFile();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "viewdrawingfile",
                    Enabled = true,
                    Title = "",
                    Description = "Посмотреть файл чертежа",
                    ButtonUse = true,
                    ButtonName = "DrawingFileViewButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        ViewDrawingFile();
                    },
                    CheckEnabled = () =>
                    {
                        var result = true;

                        return result;
                    },
                });
            }
            Commander.Init(this);
        }

        /// <summary>
        /// Имя вкладки, окуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;
        /// <summary>
        /// Форма редактирования техкарты
        /// </summary>
        FormHelper Form { get; set; }
        /// <summary>
        /// Идентификатор редактируемой штанцформы
        /// </summary>
        public int CuttingStampId { get; set; }
        /// <summary>
        /// Статус штанцформы
        /// </summary>
        private int StampStatusId { get; set; }
        /// <summary>
        /// Данные для таблицы со списком элемонтов штанцформы
        /// </summary>
        ListDataSet StampItemDS { get; set; }

        /// <summary>
        /// Обработка сообщений из шины сообщений
        /// </summary>
        /// <param name="msg"></param>
        public void ProcessMessage(ItemMessage msg)
        {
            string action = msg.Action;
            action = action.ClearCommand();
            if (!action.IsNullOrEmpty())
            {

            }
        }

        private void InitForm()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>
            {
                new FormHelperField()
                {
                    Path="NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Name,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="DRAWING_FILE_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=DrawingFileName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DRAWING_FILE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=DrawingFilePath,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="FEFCO_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Fefco,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="HOLE_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=HoleCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="VERSATILE_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Versatile,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PD",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PdEdit,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CUTTING_LENGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CuttingLength,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CUTTING_WIDTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CuttingWidth,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="WASTE_SQUARE",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=WasteSquare,
                    Format="N6",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CREASE_PERFORATION_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=CreasePerforationCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DOVETAIL_JOINT_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=DovetailJointCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Note,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };
            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

        /// <summary>
        /// Получение данных о штанцформе
        /// </summary>
        private async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "CuttingStamp");
            q.Request.SetParam("Action", "Get");
            q.Request.SetParam("ID", CuttingStampId.ToString());

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
                    // FEFCO
                    if (result.ContainsKey("FEFCO"))
                    {
                        var fefcoDS = ListDataSet.Create(result, "FEFCO");
                        Fefco.Items = fefcoDS.GetItemsList("ID", "NAME");
                    }

                    //Универсальная
                    if (result.ContainsKey("VERSATILE"))
                    {
                        var vesatileDS = ListDataSet.Create(result, "VERSATILE");
                        var versatileList = new Dictionary<string, string>
                        {
                            { "0", " " },
                        };
                        foreach (var ve in vesatileDS.Items)
                        {
                            int veId = ve.CheckGet("ID").ToInt();
                            versatileList.CheckAdd(veId.ToString(), ve.CheckGet("NAME"));
                            Versatile.Items = versatileList;
                        }
                    }

                    if (result.ContainsKey("CUTTING_STAMP"))
                    {
                        var formDS = ListDataSet.Create(result, "CUTTING_STAMP");
                        var item = formDS.Items[0];
                        string drawingFile = item.CheckGet("DRAWING_FILE");
                        if (!drawingFile.IsNullOrEmpty())
                        {
                            string drawingFileName = Path.GetFileName(drawingFile);
                            item.CheckAdd("DRAWING_FILE_NAME", drawingFileName);
                        }

                        // Для ручек и отверстий PD заполняется для каждого изделия индивидуально
                        bool holeFlag = item.CheckGet("HOLE_FLAG").ToBool();
                        if (holeFlag)
                        {
                            PdEdit.IsEnabled = false;
                        }

                        Form.SetValues(formDS);
                    }

                    Show();
                }
            }
        }

        /// <summary>
        /// Запуск редактирования штанцформы
        /// </summary>
        /// <param name="id"></param>
        public void Edit(int id)
        {
            CuttingStampId = id;
            ControlName = $"CuttingStamp_{id}";
            GetData();
        }

        /// <summary>
        /// Отображение формы редактирования
        /// </summary>
        public void Show()
        {
            Central.WM.Show(ControlName, $"Штанцформа {CuttingStampId}", true, "add", this);
        }

        /// <summary>
        /// Закрытие формы
        /// </summary>
        public void Close()
        {
            Central.WM.Close(ControlName);
            Central.WM.SetActive(ReceiverName);
        }

        /// <summary>
        /// Сохранение штанцформы
        /// </summary>
        public void Save()
        {
            if (Form.Validate())
            {
                var v = Form.GetValues();
                bool resume = true;

                //Должен быть приложен чертеж. Не обязателен для ШФ с флагом Ручки/отверстия
                if (!v.CheckGet("HOLE_FLAG").ToBool())
                {
                    var drawingFile = v.CheckGet("DRAWING_FILE");
                    if (drawingFile.IsNullOrEmpty())
                    {
                        Form.SetStatus("Не задан файл чертежа", 1);
                        resume = false;
                    }

                    var versatileId = v.CheckGet("VERSATILE_ID").ToInt();
                    if (versatileId == 0)
                    {
                        if (v.CheckGet("PD").ToInt() == 0)
                        {
                            Form.SetStatus("Не задано значение PD");
                            resume = false;
                        }
                    }
                }

                if (resume)
                {
                    v.CheckAdd("ID", CuttingStampId.ToString());
                    v.CheckAdd("STATUS_ID", StampStatusId.ToString());

                    SaveData(v);
                }
            }
        }

        /// <summary>
        /// Сохранение данных штанцформы в БД
        /// </summary>
        /// <param name="data"></param>
        private async void SaveData(Dictionary<string, string> data)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "CuttingStamp");
            q.Request.SetParam("Action", "Save");
            q.Request.SetParams(data);

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
                        //Если ответ не пустой, отправляем сообщение Гриду о необходимости обновить данные
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "Preproduction",
                            ReceiverName = ReceiverName,
                            SenderName = ControlName,
                            Action = "Refresh",
                        });

                        Close();
                    }
                }
            }
            else if (q.Answer.Error.Code != 7)
            {
                Form.SetStatus(q.Answer.Error.Message);
            }
        }

        /// <summary>
        /// Выбор нового файла чертежа для штанцформы
        /// </summary>
        private void SelectDrawingFile()
        {
            bool resume = false;
            //Получим подтверждение замены чертежа
            var dw = new DialogWindow("Вы действительно хотите изменить чертеж штанцформы?", "Выборь файла чертежа", "", DialogWindowButtons.YesNo);
            if ((bool)dw.ShowDialog())
            {
                if (dw.ResultButton == DialogResultButton.Yes)
                {
                    resume = true;
                }
            }

            if (resume)
            {
                var fd = new OpenFileDialog();
                var fdResult = (bool)fd.ShowDialog();
                if (fdResult)
                {
                    DrawingFilePath.Text = fd.FileName;
                    DrawingFileName.Text = Path.GetFileName(fd.FileName);
                }
            }
        }

        /// <summary>
        /// Открывает файл чертежа для просмотра
        /// </summary>
        private void ViewDrawingFile()
        {
            Central.OpenFile(DrawingFilePath.Text);
        }
    }
}
