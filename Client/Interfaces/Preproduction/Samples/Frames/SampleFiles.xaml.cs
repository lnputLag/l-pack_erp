using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Common.LPackClientRequest;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Вкладка для файлов, приложенных к образцу
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleFiles : UserControl
    {
        public SampleFiles()
        {
            InitializeComponent();

            InitForm();
            InitGrid();

            SetDefaults();
        }

        /// <summary>
        /// ИД образца, файлы которого отображаются на вкладке
        /// </summary>
        public int SampleId;

        /// <summary>
        /// Форма редактирования
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Признак, что образец создан в ЛК клиента. Файлы нельзя удалять
        /// </summary>
        private bool FromWeb;

        /// <summary>
        /// Имя вкладки, которая становится активной после закрытия формы редактирования
        /// </summary>
        public string ReturnTabName { get; set; }

        /// <summary>
        /// Имя вкладки
        /// </summary>
        public string TabName { get; set; }

        /// <summary>
        /// Инициализация формы
        /// </summary>
        private void InitForm()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="DESIGN_FILE_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=DesignFileName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DESIGN_FILE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=DesignFilePath,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DESIGN_FILE_OTHER_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=DesignOtherFileName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DESIGN_FILE_OTHER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=DesignOtherFilePath,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

        /// <summary>
        /// Инициализация таблицы приложенных файлов
        /// </summary>
        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=50,
                },
                new DataGridHelperColumn
                {
                    Header="Имя файла",
                    Path="FILE_NAME_ORIGINAL",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=250,
                },
                new DataGridHelperColumn
                {
                    Header="Загружен",
                    Path="UPLOAD_DTTM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth=130,
                    MaxWidth=130,
                    Format="dd.MM.yyyy HH:mm"
                },
                new DataGridHelperColumn
                {
                    Header="Имя файла",
                    Path="FILE_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            AttachmentGrid.SetColumns(columns);
            AttachmentGrid.Init();
        }

        private void SetDefaults()
        {
            TabName = "";
            ReturnTabName = "";

            Form.SetDefaults();
        }

        /// <summary>
        /// Загрузка данных на вкладку
        /// </summary>
        private async void LoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleAttachment");
            q.Request.SetParam("Action", "List");
            q.Request.SetParam("ID", SampleId.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var designDS = ListDataSet.Create(result, "DESIGN_FILES");
                    if (designDS.Items.Count > 0)
                    {
                        var rec = designDS.Items[0];
                        FromWeb = rec.CheckGet("WEB").ToBool();
                        var status = rec.CheckGet("STATUS").ToInt();

                        var designPath = rec.CheckGet("DESIGN_FILE");
                        var designOtherPath = rec.CheckGet("DESIGN_FILE_OTHER");

                        if (!designPath.IsNullOrEmpty())
                        {
                            rec.CheckAdd("DESIGN_FILE_NAME", Path.GetFileName(designPath));
                            DesignFileShowButton.IsEnabled = true;
                            DesignFileClearButton.IsEnabled = true;
                        }
                        else
                        {
                            DesignFileShowButton.IsEnabled = false;
                            DesignFileClearButton.IsEnabled = false;
                        }

                        if (!designOtherPath.IsNullOrEmpty())
                        {
                            rec.CheckAdd("DESIGN_FILE_OTHER_NAME", Path.GetFileName(designOtherPath));
                            DesignOtherFileShowButton.IsEnabled = true;
                            DesignOtherFileClearButton.IsEnabled = true;
                        }
                        else
                        {
                            DesignOtherFileShowButton.IsEnabled = false;
                            DesignOtherFileClearButton.IsEnabled = false;
                        }

                        // Для образцов уже не в работе чертежами нельзя управлять, только посмотреть
                        if (!status.ContainsIn(0, 1, 8))
                        {
                            DesignFileSelectButton.IsEnabled = false;
                            DesignFileClearButton.IsEnabled = false;
                            DesignOtherFileSelectButton.IsEnabled = false;
                            DesignOtherFileClearButton.IsEnabled = false;
                        }

                        Form.SetValues(rec);
                        // Кнопка удаления не отображается для образцов, созаднных в ЛК клиента.
                        // Если образец создавался из программы, кнопка удаления видна
                        if (FromWeb)
                        {
                            DeleteButton.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            DeleteButton.Visibility = Visibility.Visible;
                        }

                        // Для образцов уже не в работе блокируем кнопку добавления файла
                        if (status > 2)
                        {
                            AddButton.IsEnabled = false;
                            DeleteButton.IsEnabled = false;
                        }
                    }

                    var attachmentDS = ListDataSet.Create(result, "SAMPLE_FILES");
                    AttachmentGrid.UpdateItems(attachmentDS);
                }
            }
        }

        /// <summary>
        /// Отображение вкладки
        /// </summary>
        public void Show()
        {
            if (SampleId > 0)
            {
                LoadItems();

                string title = $"Файлы образца {SampleId}";
                TabName = $"SampleFiles{SampleId}";
                Central.WM.AddTab(TabName, title, true, "add", this);
            }

        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        public void Close()
        {
            Central.WM.RemoveTab(TabName);

            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "PreproductionSample",
                ReceiverName = "",
                SenderName = TabName,
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            if (!string.IsNullOrEmpty(ReturnTabName))
            {
                Central.WM.SetActive(ReturnTabName, true);
                ReturnTabName = "";
            }
        }

        /// <summary>
        /// Получает и открывает приложенный к образцу файл
        /// </summary>
        private async void OpenAttachment()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleAttachment");
            q.Request.SetParam("Action", "Get");
            q.Request.SetParam("ID", AttachmentGrid.SelectedItem.CheckGet("ID").ToInt().ToString());
            q.Request.SetParam("FILE_NAME", AttachmentGrid.SelectedItem.CheckGet("FILE_NAME_ORIGINAL"));

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Central.OpenFile(q.Answer.DownloadFilePath);
            }
            else
            {
                Form.SetStatus("Файл не найден", 1);
            }
        }

        /// <summary>
        /// Добавление файл к приложенным файлам
        /// </summary>
        private async void AddFile()
        {
            FormStatus.Text = "";
            bool resume = true;

            var fd = new OpenFileDialog();
            var fdResult = (bool)fd.ShowDialog();

            if (fdResult)
            {
                var fileName = Path.GetFileName(fd.FileName);
                // Исключаем дублирование файлов
                if (AttachmentGrid.GridItems != null)
                {
                    foreach (var item in AttachmentGrid.GridItems)
                    {
                        if (item["FILE_NAME_ORIGINAL"] == fileName)
                        {
                            Form.SetStatus("Такой файл уже есть в списке", 1);
                            resume = false;
                        }
                    }
                }
            }
            else
            {
                resume = false;
            }

            // Сохраняем файл
            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "SampleAttachment");
                q.Request.SetParam("Action", "Save");
                q.Request.SetParam("ID", SampleId.ToString());
                q.Request.Type = RequestTypeRef.MultipartForm;
                q.Request.UploadFilePath = fd.FileName;

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
                            LoadItems();
                        }
                    }
                }
                else
                {
                    if (q.Answer.Error.Code == 145)
                    {
                        Form.SetStatus(q.Answer.Error.Message, 1);
                    }
                }
            }
        }

        /// <summary>
        /// Удаление файла из таблицы приложенных файлов
        /// </summary>
        private async void DeleteFile()
        {
            FormStatus.Text = "";
            if (AttachmentGrid.SelectedItem != null)
            {
                int fileId = AttachmentGrid.SelectedItem.CheckGet("ID").ToInt();
                string fileName = AttachmentGrid.SelectedItem.CheckGet("FILE_NAME");

                if (!fileName.IsNullOrEmpty() && (fileId > 0))
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "SampleAttachment");
                    q.Request.SetParam("Action", "Delete");
                    q.Request.SetParam("ID", fileId.ToString());
                    q.Request.SetParam("FILE_NAME", fileName);

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
                                LoadItems();
                            }
                            else
                            {
                                Form.SetStatus("Ошибка! Выберите файл снова", 1);
                            }
                        }
                    }
                    else
                    {
                        if (q.Answer.Error.Code == 145)
                        {
                            Form.SetStatus(q.Answer.Error.Message, 1);
                        }
                    }
                }
                else
                {
                    Form.SetStatus("Ошибка выбора файла для удаления. Выберите снова", 1);
                }
            }
        }

        /// <summary>
        /// Сохранение файла из таблицы приложенных файлов
        /// </summary>
        private async void SaveAttachment()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleAttachment");
            q.Request.SetParam("Action", "Get");
            q.Request.SetParam("ID", AttachmentGrid.SelectedItem.CheckGet("ID").ToInt().ToString());
            q.Request.SetParam("FILE_NAME", AttachmentGrid.SelectedItem.CheckGet("FILE_NAME_ORIGINAL"));

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Central.SaveFile(q.Answer.DownloadFilePath);
            }
            else
            {
                Form.SetStatus("Файл не найден", 1);
            }

        }

        /// <summary>
        /// Сохранение для образца пути к файлу чертежа
        /// </summary>
        /// <param name="designType">Тип чертежа: 0 - основной, 1 - альтернативный</param>
        private async void LoadDesign(int designType, string fileName)
        {
            FormStatus.Text = "";
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "SaveDesignFilePath");
            q.Request.SetParam("ID", SampleId.ToString());
            q.Request.SetParam("DESIGN_TYPE", designType.ToString());
            q.Request.SetParam("FILE_PATH", fileName);

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
                        LoadItems();
                        // Отправляем сообщение гриду, который вызвал этот фрейм, о необходимости обновиться
                        Messenger.Default.Send(new ItemMessage()
                        {
                            ReceiverGroup = "PreproductionSample",
                            ReceiverName = ReturnTabName,
                            SenderName = "SampleFiles",
                            Action = "Refresh",
                        });
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "PreproductionSample",
                            ReceiverName = ReturnTabName,
                            SenderName = "SampleFiles",
                            Action = "Refresh",
                        });
                    }
                    else
                    {
                        Form.SetStatus("Ошибка! Выберите файл снова", 1);
                    }
                }
            }
            else
            {
                if (q.Answer.Error.Code == 145)
                {
                    Form.SetStatus(q.Answer.Error.Message, 1);
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DesignFileSelect_Click(object sender, RoutedEventArgs e)
        {
            var fd = new OpenFileDialog();

            // Если имя файла заполнено, то открываем папку с этим файлом или папку с альтернативным файлом
            if (!string.IsNullOrEmpty(DesignFilePath.Text))
            {
                fd.InitialDirectory = Path.GetDirectoryName(DesignFilePath.Text);
            }
            else if (!string.IsNullOrEmpty(DesignOtherFilePath.Text))
            {
                fd.InitialDirectory = Path.GetDirectoryName(DesignOtherFilePath.Text);
            }

            var fdResult = (bool)fd.ShowDialog();
            if (fdResult)
            {
                DesignFilePath.Text = fd.FileName;
                DesignFileName.Text = Path.GetFileName(fd.FileName);
                LoadDesign(0, fd.FileName);
            }
        }

        private void DesignFileClear_Click(object sender, RoutedEventArgs e)
        {
            DesignFileName.Text = "";
            DesignFilePath.Text = "";
            LoadDesign(0, "");
        }

        private void DesignOtherFileSelect_Click(object sender, RoutedEventArgs e)
        {
            var fd = new OpenFileDialog();

            // Если имя файла заполнено, то открываем папку с этим файлом или папку с альтернативным файлом
            if (!string.IsNullOrEmpty(DesignOtherFilePath.Text))
            {
                fd.InitialDirectory = Path.GetDirectoryName(DesignOtherFilePath.Text);
            }
            else if (!string.IsNullOrEmpty(DesignFilePath.Text))
            {
                fd.InitialDirectory = Path.GetDirectoryName(DesignFilePath.Text);
            }

            var fdResult = (bool)fd.ShowDialog();
            if (fdResult)
            {
                DesignOtherFilePath.Text = fd.FileName;
                DesignOtherFileName.Text = Path.GetFileName(fd.FileName);
                LoadDesign(1, fd.FileName);
            }
        }

        private void DesignOtherFileClear_Click(object sender, RoutedEventArgs e)
        {
            DesignOtherFileName.Text = "";
            DesignOtherFilePath.Text = "";
            LoadDesign(1, "");
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddFile();
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (AttachmentGrid.SelectedItem != null)
            {
                OpenAttachment();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteFile();
        }

        private void DesignFileShow_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(DesignFilePath.Text))
            {
                if (File.Exists(DesignFilePath.Text))
                {
                    Central.OpenFile(DesignFilePath.Text);
                }
                else
                {
                    Form.SetStatus("Файл не найден!", 1);
                }
            }
        }

        private void DesignOtherFileShow_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(DesignOtherFilePath.Text))
            {
                if (File.Exists(DesignOtherFilePath.Text))
                {
                    Central.OpenFile(DesignOtherFilePath.Text);
                }
                else
                {
                    Form.SetStatus("Файл не найден!", 1);
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (AttachmentGrid.SelectedItem != null)
            {
                SaveAttachment();
            }
        }
    }
}
