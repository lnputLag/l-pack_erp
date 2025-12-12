using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using DevExpress.DocumentServices.ServiceModel.DataContracts;
using DevExpress.Xpf.Core;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Common.LPackClientRequest;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Вкладка для файлов, приложенных к веб-техкартам
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class WebTechnologicalMapFiles : UserControl
    {
        public WebTechnologicalMapFiles()
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
                    Width2 = 40,
                },
                new DataGridHelperColumn
                {
                    Header="Загружен",
                    Path="DTTM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Width2 = 15,
                    Format="dd.MM.yyyy HH:mm"
                },
                new DataGridHelperColumn
                {
                    Header="Владелец",
                    Path="NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 40,
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
                    Header="Тип владельца",
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
                    Visible=false,
                },
            };
            
            AttachmentGrid.SetColumns(columns);
            AttachmentGrid.OnLoadItems = LoadItems;
            AttachmentGrid.OnFilterItems = FilterItems;
            AttachmentGrid.SetPrimaryKey("ID");
            AttachmentGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

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
            AttachmentGrid.OnSelectItem = (selectedItem) =>
            {
                selectedItem = AttachmentGrid.SelectedItem;
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
            q.Request.SetParam("Object", "WebTechnologicalMap");
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
                    var ds = ListDataSet.Create(result, "ITEMS");
                    AttachmentGrid.UpdateItems(ds);
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

                string title = $"Файлы веб-техкарты {TkId}";
                TkName.Text = "Файлы веб-техкарты \""+TechCardName+"\""; 
                TabName = $"WebTechnologicalMapFiles{TkId}";
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
        /// Получает и открывает приложенный к техкарте файл
        /// </summary>
        private async void OpenAttachment()
        {
            var folder = Central.GetStorageNetworkPathByCode("techcard_attachment");
            var file = Path.Combine(folder, AttachmentGrid.SelectedItem.CheckGet("FILE_NAME"));

            if (File.Exists(file))
            {
                Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });
            }
            else
            {
                var d = new DialogWindow($"Файл не найден.", "Ошибка открытия файла", "", DialogWindowButtons.OK);
                d.ShowDialog();
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
                            var d = new DialogWindow($"Такой файл уже есть в списке.", "Ошибка прикрепления файла", "", DialogWindowButtons.OK);
                            d.ShowDialog();
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
                q.Request.SetParam("Object", "WebTechnologicalMap");
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
                    q.ProcessError();
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
                var dw = new DialogWindow("Вы действительно хотите удалить файл "+ AttachmentGrid.SelectedItem.CheckGet("FILE_NAME_ORIGINAL") +"?", "Удалить файл", "", DialogWindowButtons.NoYes);
                if ((bool)dw.ShowDialog())
                {

                    int fileId = AttachmentGrid.SelectedItem.CheckGet("ID").ToInt();

                    if (fileId > 0)
                    {
                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Preproduction");
                        q.Request.SetParam("Object", "WebTechnologicalMap");
                        q.Request.SetParam("Action", "AttachmentDelete");
                        q.Request.SetParam("ID", fileId.ToString());

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
                                    var d = new DialogWindow($"Ошибка! Выберите файл снова.", "Ошибка удаления файла", "", DialogWindowButtons.OK);
                                    d.ShowDialog();
                                }
                            }
                        }
                        else
                        {
                            q.ProcessError();
                        }
                    }
                    else
                    {
                        var d = new DialogWindow($"Ошибка выбора файла для удаления. Выберите снова.", "Ошибка удаления файла", "", DialogWindowButtons.OK);
                    }
                }
            }
        }

        /// <summary>
        /// Скачивание файла из таблицы приложенных файлов
        /// </summary>
        private async void UploadFile()
        {
            var folder = Central.GetStorageNetworkPathByCode("techcard_attachment");
            var file = Path.Combine(folder, AttachmentGrid.SelectedItem.CheckGet("FILE_NAME"));

            if (File.Exists(file))
            {
                Central.SaveFile(file.ToString(), false, AttachmentGrid.SelectedItem.CheckGet("FILE_NAME_ORIGINAL"));
            }
            else
            {
                var d = new DialogWindow($"Файл не найден.", "Ошибка скачивания файла", "", DialogWindowButtons.OK);
                d.ShowDialog();
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
