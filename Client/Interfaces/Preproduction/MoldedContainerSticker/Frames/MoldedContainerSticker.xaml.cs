using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using static Client.Common.LPackClientRequest;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Редактирование информации по этикетке для литой тары
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class MoldedContainerSticker : ControlBase
    {
        /// <summary>
        /// Редактирование информации по этикетке для литой тары
        /// </summary>
        public MoldedContainerSticker()
        {
            RoleName = "[erp]molded_contnr_sticker";
            InitializeComponent();
            InitForm();
            ProcessPermitions();
            SetDefaults();
            GridInit();

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverGroup == "PreproductionContainer")
                {
                    if (msg.ReceiverName == ControlName)
                    {
                        ProcessCommand(msg.Action);
                    }
                }
            };

            Commander.SetCurrentGridName("TechCardsGrid");
            Commander.SetCurrentGroup("grid");
            {

                Commander.Add(new CommandItem()
                {
                    Name = "addTk",
                    Title = "Добавить",
                    ButtonUse = true,
                    ButtonName = "AddButton",
                    Description = "Привязать техкарту к этикетке",
                    Action = () =>
                    {
                        BindTechcard();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        if (StickerId > 0)
                        {
                            result = true;
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "deleteTk",
                    Title = "Удалить",
                    ButtonUse = true,
                    ButtonName = "DeleteButton",
                    Description = "Отвязать техкарту от этикетки",
                    Action = () =>
                    {
                        UnbindTechcard();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row= TechCardsGrid.SelectedItem;
                        if(row.CheckGet("ID").ToInt() != 0 && row.CheckGet("SKU").IsNullOrEmpty())
                        {
                            result = true;
                        }
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
        /// Идентификатор этикетки
        /// </summary>
        public int StickerId { get; set; }
        /// <summary>
        /// Форма редактирования техкарты
        /// </summary>
        FormHelper Form { get; set; }
        /// <summary>
        /// Код права на редактирование
        /// </summary>
        private int Permition;
        /// <summary>
        /// Стартовая папка при загрузке файлов этикеток
        /// </summary>
        private string InitialDirectory;

        private bool ClearTechcardAllow;

        /// <summary>
        /// Управление доступом
        /// </summary>
        private void ProcessPermitions()
        {
            var mode = Central.Navigator.GetRoleLevel("[erp]molded_contnr_sticker");

            Permition = (int)mode;
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
                        Save(1);
                        break;
                    case "apply":
                        Save(0);
                        break;
                    case "close":
                        Close();
                        break;
                    case "degignselect":
                        SaveDesignFile();
                        break;
                    case "designshow":
                        ShowFile(1);
                        break;
                    case "designclear":
                        ClearFile(1);
                        break;
                    case "imageselect":
                        SaveImageFile();
                        break;
                    case "imageshow":
                        ShowFile(2);
                        break;
                    case "imageclear":
                        ClearFile(2);
                        break;
                    case "techcardselect":
                        var techcardSelectFrame = new MoldedContainerStickerTechcardSelect();
                        techcardSelectFrame.ReceiverName = ControlName;
                        techcardSelectFrame.StickerId = StickerId;
                        techcardSelectFrame.Show();
                        break;
                    case "techcardclear":
                        UnbindTechcard();
                        break;
                    case "refresh":
                        //отправляем сообщение о необходимости обновить данные
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "PreproductionContainer",
                            ReceiverName = ReceiverName,
                            SenderName = ControlName,
                            Action = "Refresh",
                        });
                        GetData();
                        break;
                }
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            InitialDirectory = Central.GetStorageNetworkPathByCode("rig_container_order");
            if (string.IsNullOrEmpty(InitialDirectory))
            {
                InitialDirectory = "\\\\file-server-4\\Техкарты\\_Дизайн\\";
            }
            InitialDirectory = $"{InitialDirectory}Рисунки\\_Литая тара (яичные лотки)";
        }

        /// <summary>
        /// Инициализация формы
        /// </summary>
        private void InitForm()
        {
            Form = new FormHelper();

            var fields = new List<FormHelperField>
            {
                new FormHelperField()
                {
                    Path="NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=StickerName,
                    Enabled =false,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="ARCHIVED_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=ArchivedCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="GUID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Guid,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DRAWING_EXISTS",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="IMAGE_EXISTS",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="TECHCARD_ACTIVE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="TECHCARD_WORKED",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                },
            };
            Form.ToolbarControl = FormToolbar;
            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

        /// <summary>
        /// Инициализация таблицы привязанных техкарт
        /// </summary>
        private void GridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="SKU",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Название",
                    Path="TECHCARD_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=34,
                },
                new DataGridHelperColumn
                {
                    Header="Покупатель",
                    Path="CUSTOMER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=25,
                },
                new DataGridHelperColumn
                {
                    Header="Дизайнер",
                    Path="DESIGNER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=14,
                },
                new DataGridHelperColumn
                {
                    Header="ИД дизайнера",
                    Path="DESIGNER_EMPL_ID",
                    Width2=12,
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            TechCardsGrid.SetColumns(columns);

            TechCardsGrid.SetPrimaryKey("ID");
            TechCardsGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            TechCardsGrid.Toolbar = GridToolbar;
            TechCardsGrid.Commands = Commander;
            TechCardsGrid.Init();
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        public async void TechCardsGridLoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Sticker");
            q.Request.SetParam("Action", "ListTechcardByStickerId");
            q.Request.SetParam("STICKER_ID", StickerId.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "TECHCARD");
                    TechCardsGrid.UpdateItems(ds);
                    if (ds.Items.Count == 0)
                    {
                        StickerName.IsEnabled = true;
                    }
                    else
                    {
                        StickerName.IsEnabled = false;
                    }
                }
            }
            //Проверяем значение в поле TECHCARD_SKU. Если заполнено, отвязывать техкарту нельзя
            if (TechCardsGrid.Items.Count > 0)
            {
                var flag = true;
                foreach (var i in TechCardsGrid.Items)
                {
                    if (!i.CheckGet("SKU").IsNullOrEmpty())
                    {
                        flag = false;
                    }
                }
                ClearTechcardAllow = flag;
            }
            EnableControls();
        }

        /// <summary>
        /// Получение данных
        /// </summary>
        private async void GetData()
        {
            DisableControls();
            ClearTechcardAllow = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Sticker");
            q.Request.SetParam("Action", "Get");
            q.Request.SetParam("ID", StickerId.ToString());

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
                    var ds = ListDataSet.Create(result, "STICKER");
                    
                    Form.SetValues(ds);
                }
            }
            TechCardsGridLoadItems();
        }

        public void Edit(int stickerId=0)
        {
            StickerId = stickerId;
            ControlName = $"MoldedContainerSticker_{StickerId}";
            if (StickerId > 0)
            {
                GetData();
            }
            else
            {
                SetButtonsAvailable();
            }
            Show();
        }

        /// <summary>
        /// Отображение вкладки редактирования
        /// </summary>
        public void Show()
        {
            Central.WM.Show(ControlName, $"Этикетка {StickerId}", true, "add", this);
        }

        /// <summary>
        /// Настройка доступности кнопок работы с файлами
        /// </summary>
        private void SetButtonsAvailable()
        {
            var v = Form.GetValues();

            // Если этикетка привязана к техкарте с ассортиментом, можно только посмотреть файлы
            bool techcardExists = v.CheckGet("TECHCARD_WORKED").ToBool();
            DesignFileSelectButton.IsEnabled = !techcardExists && (Permition > 1);
            ImageFileSelectButton.IsEnabled = !techcardExists && (Permition > 1);

            bool drawingExists = v.CheckGet("DRAWING_EXISTS").ToBool();
            DesignFileShowButton.IsEnabled = drawingExists;
            DesignFileClearButton.IsEnabled = drawingExists && !techcardExists && (Permition > 1);

            bool imageExists = v.CheckGet("IMAGE_EXISTS").ToBool();
            ImageFileShowButton.IsEnabled = imageExists;
            ImageFileClearButton.IsEnabled = imageExists && !techcardExists && (Permition > 1);

            bool techcardActive = v.CheckGet("TECHCARD_ACTIVE").ToBool();
            ArchivedLabel.IsEnabled = !techcardActive && (Permition > 1) && (StickerId > 0);
            ArchivedCheckBox.IsEnabled = !techcardActive && (Permition > 1) && (StickerId > 0);

            
        }

        /// <summary>
        /// Сохранение данных
        /// </summary>
        /// <param name="closeAfterSave"></param>
        public async void Save(int closeAfterSave=0)
        {
            if (Form.Validate())
            {
                var v = Form.GetValues();
                bool resume = true;

                if (!v.CheckGet("DRAWING_EXISTS").ToBool())
                {
                    Form.SetStatus("Выберите файл дизайна", 1);
                    resume = false;
                }

                if (!v.CheckGet("IMAGE_EXISTS").ToBool())
                {
                    Form.SetStatus("Выберите файл изображения", 1);
                    resume = false;
                }

                if (resume)
                {
                    // Создаем запись в БД
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "Sticker");
                    q.Request.SetParam("Action", "Save");
                    q.Request.SetParam("ID", StickerId.ToString());
                    q.Request.SetParams(v);

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
                            if (StickerId == 0)
                            {
                                var ds = ListDataSet.Create(result, "ITEMS");
                                if (ds.Items.Count > 0)
                                {
                                    StickerId = ds.Items[0].CheckGet("ID").ToInt();
                                }
                                else
                                {
                                    Form.SetStatus("Ошибка получения данных", 1);
                                    resume = false;
                                }
                            }
                        }
                    }
                    else if (q.Answer.Error.Code == 145)
                    {
                        Form.SetStatus(q.Answer.Error.Message, 1);
                        resume = false;
                    }
                    else
                    {
                        resume = false;
                    }
                }

                if (resume && !DesignFileName.Text.IsNullOrEmpty())
                {
                    // Если заполнен путь к файлу дизайна, отправляем его на сервер
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "Sticker");
                    q.Request.SetParam("Action", "SaveDrawignFile");
                    q.Request.SetParam("ID", StickerId.ToString());
                    q.Request.SetParam("FILE_TYPE", "1");
                    q.Request.Type = RequestTypeRef.MultipartForm;
                    q.Request.UploadFilePath = DesignFileName.Text;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            if (!result.ContainsKey("ITEMS"))
                            {
                                Form.SetStatus("Не удалось сохранить файл дизайна", 1);
                                resume = false;
                            }
                        }
                    }
                    else
                    {
                        Form.SetStatus("Не удалось сохранить файл дизайна", 1);
                        resume = false;
                    }
                }

                if (resume && !ImageFileName.Text.IsNullOrEmpty())
                {
                    // Если заполнен путь к файлу изображения, отправляем его на сервер
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "Sticker");
                    q.Request.SetParam("Action", "SaveDrawignFile");
                    q.Request.SetParam("ID", StickerId.ToString());
                    q.Request.SetParam("FILE_TYPE", "2");
                    q.Request.Type = RequestTypeRef.MultipartForm;
                    q.Request.UploadFilePath = ImageFileName.Text;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            if (!result.ContainsKey("ITEMS"))
                            {
                                Form.SetStatus("Не удалось сохранить файл изображения", 1);
                                resume = false;
                            }
                        }
                    }
                    else
                    {
                        Form.SetStatus("Не удалось сохранить файл изображения", 1);
                        resume = false;
                    }
                }

                if (resume)
                {
                    // Отправляем гриду сообщение
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverGroup = "PreproductionContainer",
                        ReceiverName = ReceiverName,
                        SenderName = ControlName,
                        Action = "Refresh",
                    });

                    if (closeAfterSave == 1)
                    {
                        Close();
                    }
                    else
                    {
                        SetButtonsAvailable();
                    }
                }
            }
        }

        /// <summary>
        /// Сохранение файла дизайна
        /// </summary>
        public void SaveDesignFile()
        {
            var fd = new OpenFileDialog();
            fd.Filter = "Файлы дизайна (*.*)|*.cdr;*.ai";
            fd.FilterIndex = 0;
            fd.InitialDirectory = InitialDirectory;

            var fdResult = (bool)fd.ShowDialog();
            if (fdResult)
            {
                var d = new Dictionary<string, string>();
                DesignFileName.Text = fd.FileName;
                d.Add("DRAWING_EXISTS", "1");

                Form.SetValues(d);

                SetButtonsAvailable();
            }
        }
        
        /// <summary>
        /// Сохранение файла изображения
        /// </summary>
        public void SaveImageFile()
        {
            var fd = new OpenFileDialog();
            fd.Filter = "PDF (*.pdf)|*.pdf|JPEG (*.jpg,*.jpeg)|*.jpg;*.jpeg";
            fd.FilterIndex = 0;
            fd.InitialDirectory = InitialDirectory;

            var fdResult = (bool)fd.ShowDialog();
            if (fdResult)
            {
                var d = new Dictionary<string, string>();
                ImageFileName.Text = fd.FileName;
                d.Add("IMAGE_EXISTS", "1");

                Form.SetValues(d);

                SetButtonsAvailable();
            }
        }

        /// <summary>
        /// Открывает содержимое файла
        /// </summary>
        /// <param name="fileType">1 - дизайн, 2 - растровое изображение</param>
        public async void ShowFile(int fileType)
        {
            bool resume = true;
            if (fileType == 1)
            {
                if (!DesignFileName.Text.IsNullOrEmpty())
                {
                    resume = false;
                    Central.OpenFile(DesignFileName.Text);
                }
            }
            else if (fileType == 2)
            {
                if (!ImageFileName.Text.IsNullOrEmpty())
                {
                    resume = false;
                    Central.OpenFile(ImageFileName.Text);

                }
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "Sticker");
                q.Request.SetParam("Action", "GetDrawingFile");
                q.Request.SetParam("ID", StickerId.ToString());
                q.Request.SetParam("FILE_TYPE", fileType.ToString());
                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    Central.OpenFile(q.Answer.DownloadFilePath);
                }
                else if (q.Answer.Error.Code == 145)
                {
                    Form.SetStatus(q.Answer.Error.Message, 1);
                }
            }

        }

        /// <summary>
        /// Очистка файла
        /// </summary>
        /// <param name="fileType">1 - дизайн, 2 - растровое изображение</param>
        public async void ClearFile(int fileType)
        {
            DisableControls();
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Sticker");
            q.Request.SetParam("Action", "ClearDrawingFile");
            q.Request.SetParam("ID", StickerId.ToString());
            q.Request.SetParam("FILE_TYPE", fileType.ToString());
            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    if (result.ContainsKey("ITEMS"))
                    {
                        string k = "DRAWING_EXISTS";
                        if (fileType == 2)
                        {
                            k = "IMAGE_EXISTS";
                        }
                        var d = new Dictionary<string, string> { { k, "0" } };
                        Form.SetValues(d);

                    }
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                Form.SetStatus(q.Answer.Error.Message, 1);
            }
            EnableControls();
        }

        /// <summary>
        /// Отвязываем активную техкарту от редактируемой этикетки
        /// </summary>
        private async void UnbindTechcard()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Sticker");
            q.Request.SetParam("Action", "BindTechcard");
            q.Request.SetParam("STICKER_ID", "0");
            q.Request.SetParam("LAST_STICKER_ID", StickerId.ToString());
            q.Request.SetParam("TECHCARD_ID", TechCardsGrid.SelectedItem.CheckGet("ID"));
            
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
                        //отправляем сообщение о необходимости обновить данные
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "PreproductionContainer",
                            ReceiverName = ReceiverName,
                            SenderName = ControlName,
                            Action = "Refresh",
                        });
                        GetData();
                    }
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                FormStatus.Text = q.Answer.Error.Message;
            }
        }

        /// <summary>
        /// Привязываем техкарту к редактируемой этикетке
        /// </summary>
        private async void BindTechcard()
        {
            var techcardSelectFrame = new MoldedContainerStickerTechcardSelect();
            techcardSelectFrame.ReceiverName = ControlName;
            if (TechCardsGrid.Items.Count == 0 && Guid.Text.IsNullOrEmpty())
            {
                techcardSelectFrame.RenameSticker = 1;
            }
            else
            {
                techcardSelectFrame.RenameSticker = 0;
            }
            techcardSelectFrame.StickerId = StickerId;
            techcardSelectFrame.Show();
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        public void Close()
        {
            Central.WM.Close(ControlName);
            if (!ReceiverName.IsNullOrEmpty())
            {
                Central.WM.SetActive(ReceiverName);
                ReceiverName = "";
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку
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

        public void DisableControls()
        {
            TechCardsGrid.ShowSplash();
        }

        public void EnableControls()
        {
            TechCardsGrid.HideSplash();
            SetButtonsAvailable();
        }

    }
}
