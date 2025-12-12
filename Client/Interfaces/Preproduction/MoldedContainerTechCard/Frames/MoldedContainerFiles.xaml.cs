using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using DevExpress.DocumentServices.ServiceModel.DataContracts;
using DevExpress.Xpf.Core;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Common.LPackClientRequest;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Вкладка для файлов, приложенных к ТК ЛТ
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class MoldedContainerFiles : UserControl
    {
        public MoldedContainerFiles()
        {
            InitializeComponent();

            InitForm();
            InitGrid();

            SetDefaults();
            FromManager = 0;
        }

        /// <summary>
        /// ИД ТК, файлы которого отображаются на вкладке
        /// </summary>
        public int TkId;

        /// <summary>
        /// Форма редактирования
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Имя вкладки, которая становится активной после закрытия формы редактирования
        /// </summary>
        public string ReturnTabName { get; set; }

        /// <summary>
        /// Имя вкладки
        /// </summary>
        public string TabName { get; set; }

        /// <summary>
        /// Название техкарты
        /// </summary>
        public string TechCardName { get; set; }

        /// <summary>
        /// Флаг того, что файл привязывает менеджер
        /// </summary>
        public int FromManager { get; set; }

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
                    Path="OWNER_RB",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=OwnerRadioButton,
                    ControlType="RadioBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        AttachmentGrid.UpdateItems();
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
                    Width2 = 5,
                },
                new DataGridHelperColumn
                {
                    Header="Имя файла",
                    Path="FILE_NAME_ORIGINAL",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 47,
                },
                new DataGridHelperColumn
                {
                    Header="Загружен",
                    Path="UPLOAD_DTTM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Width2 = 14,
                    Format="dd.MM.yyyy HH:mm"
                },
                new DataGridHelperColumn
                {
                    Header="Владелец",
                    Path="NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 15,
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="DELETED_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 15,
                },
                new DataGridHelperColumn
                {
                    Header="Тип владельца",
                    Path="OWNER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="ИД владельца",
                    Path="OWNER_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Имя файла",
                    Path="FILE_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };

            AttachmentGrid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    if (selectedItem.CheckGet("OWNER_ID").ToInt() == 1)
                    {
                        DeleteButton.IsEnabled = true;
                    }
                    else
                    {
                        DeleteButton.IsEnabled = false;
                    }
                }
            };
            AttachmentGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=System.Windows.DependencyProperty.UnsetValue;
                        var color = "";

                        if (row.CheckGet("DELETED_FLAG") == "Удален")
                        {
                                color = HColor.Red;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };

            AttachmentGrid.OnLoadItems = LoadItems;
            AttachmentGrid.OnFilterItems = FilterItems;
            AttachmentGrid.SetPrimaryKey("ID");
            AttachmentGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            AttachmentGrid.SetColumns(columns);
            AttachmentGrid.Init();
        }

        /// <summary>
        /// Фильтрация записей таблицы 
        /// </summary>
        public void FilterItems()
        {
            if (AttachmentGrid?.Items != null)
            {
                if (AttachmentGrid.Items.Count > 0)
                {
                    var owner = Form.GetValueByPath("OWNER_RB");
                    var list = new List<Dictionary<string, string>>();
                    foreach (var item in AttachmentGrid.Items)
                    {
                        bool includeByVisible = true;
                        if (item.CheckGet("OWNER_ID").ToInt() != owner.ToInt())
                        {
                            includeByVisible = false;
                        }

                        if (includeByVisible)
                        {
                            list.Add(item);
                        }
                    }
                    AttachmentGrid.Items = list;
                    AttachmentGrid.SelectRowFirst();

                }
            }
        }

        private void SetDefaults()
        {
            TabName = "";
            ReturnTabName = "";

            Form.SetDefaults();
            Form.SetValueByPath("OWNER_RB", "2");
        }

        /// <summary>
        /// Загрузка данных на вкладку
        /// </summary>
        private async void LoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "AttachmentList");
            q.Request.SetParam("ID", TkId.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var filesDS = ListDataSet.Create(result, "ITEMS");
                    AttachmentGrid.UpdateItems(filesDS);
                }
            }
        }

        /// <summary>
        /// Отображение вкладки
        /// </summary>
        public void Show()
        {
            if (TkId > 0)
            {
                LoadItems();

                string title = $"Файлы техкарты ЛТ {TkId}";
                TkName.Text = "Файлы техкарты \""+TechCardName+"\""; 
                TabName = $"MoldedContainerFiles{TkId}";
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
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "AttachmentGet");
            q.Request.SetParam("ID", AttachmentGrid.SelectedItem.CheckGet("ID").ToInt().ToString());
            q.Request.SetParam("FILE_NAME", AttachmentGrid.SelectedItem.CheckGet("FILE_NAME_ORIGINAL"));

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items.Count > 0)
                    {

                        var fileName = ds.GetFirstItemValueByKey("FILE").ToString();
                        Central.OpenFile(fileName);

                    }
                }
            }
            else
            {
                Form.SetStatus("Файл не найден", 1);
            }
        }

        /// <summary>
        /// Добавление файла к приложенным файлам
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
                if (AttachmentGrid.Items != null)
                {
                    foreach (var item in AttachmentGrid.Items)
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
                q.Request.SetParam("Object", "MoldedContainer");
                q.Request.SetParam("Action", "AttachmentSave");
                q.Request.SetParam("ID", TkId.ToString()); 
                q.Request.SetParam("FLAG_MANAGER", FromManager.ToString());
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
                var dw = new DialogWindow("Вы действительно хотите удалить файл "+ AttachmentGrid.SelectedItem.CheckGet("FILE_NAME") +"?", "Удалить файл", "", DialogWindowButtons.NoYes);
                if ((bool)dw.ShowDialog())
                {

                    int fileId = AttachmentGrid.SelectedItem.CheckGet("ID").ToInt();
                    string fileName = AttachmentGrid.SelectedItem.CheckGet("FILE_NAME");

                    if (!fileName.IsNullOrEmpty() && (fileId > 0))
                    {
                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Preproduction");
                        q.Request.SetParam("Object", "MoldedContainer");
                        q.Request.SetParam("Action", "AttachmentDelete");
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
        }

        /// <summary>
        /// Скачивание файла из таблицы приложенных файлов
        /// </summary>
        private async void UploadFile()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "AttachmentGet");
            q.Request.SetParam("ID", AttachmentGrid.SelectedItem.CheckGet("ID").ToInt().ToString());
            q.Request.SetParam("FILE_NAME", AttachmentGrid.SelectedItem.CheckGet("FILE_NAME_ORIGINAL"));

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items.Count > 0)
                    {
                        var fileNameForSave = ds.GetFirstItemValueByKey("FILE").ToString();
                        Central.SaveFile(fileNameForSave.ToString());
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
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

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            UploadFile();
        }
    }
}
