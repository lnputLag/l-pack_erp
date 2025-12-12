using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Добавление в приложенные к образцу файлы техкарты, выбранной по артикулу
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleAttachTechnologicalMap : UserControl
    {
        public SampleAttachTechnologicalMap()
        {
            InitializeComponent();

            InitForm();
            InitGrid();
            SetDefaults();
        }
        /// <summary>
        /// ИД образца, к которому прикрепляем техкарту
        /// </summary>
        public int SampleId;
        /// <summary>
        /// Форма
        /// </summary>
        public FormHelper Form { get; set; }
        /// <summary>
        /// Имя вкладки
        /// </summary>
        public string TabName;
        /// <summary>
        /// Имя объекта интерфейса для возврата фокуса
        /// </summary>
        public string ReceiverName;

        private void InitForm()
        {
            Form = new FormHelper();

            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="ARTICLE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Article,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SIZE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductSize,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();
            SampleId = 0;
        }

        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header="Покупатель",
                    Path="CUSTOMER",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=120,
                    MaxWidth=220,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="ARTICLE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=120,
                    MaxWidth=140,
                },
                new DataGridHelperColumn
                {
                    Header="Размеры",
                    Path="PRODUCT_SIZE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Файл техкарты",
                    Path="MAP_FILE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=120,
                    MaxWidth=300,
                },
                new DataGridHelperColumn
                {
                    Header="Файл техкарты",
                    Path="MAP_FILE_PATH",
                    ColumnType=ColumnTypeRef.String,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Архивная ТК",
                    Path="TK_ARCHIVE",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            TechnologicalMapGrid.SetColumns(columns);
            // Раскраска строк
            TechnologicalMapGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                // цвета шрифта строк
                {
                    DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        // ТК в архиве
                        if (row["TK_ARCHIVE"].ToInt() == 1)
                        {
                            color = HColor.OliveFG;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;

                    }
                }
            };
            TechnologicalMapGrid.Init();

            TechnologicalMapGrid.OnDblClick = (Dictionary<string, string> selectedItem) =>
            {
                OpenMapFile(selectedItem);
            };

        }

        /// <summary>
        /// Открытие вкладки
        /// </summary>
        public void Show()
        {
            if (SampleId > 0)
            {
                string title = $"Техкарта для {SampleId}";
                TabName = $"AttachMap{SampleId}";
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


            if (!string.IsNullOrEmpty(ReceiverName))
            {
                Central.WM.SetActive(ReceiverName, true);
                ReceiverName = "";
            }
        }

        /// <summary>
        /// Получение имени и пути файла техкарты
        /// </summary>
        /// <param name="art"></param>
        private async void GetMapFile(Dictionary<string,string> p)
        {
            GridToolbar.IsEnabled = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleAttachment");
            q.Request.SetParam("Action", "GetMap");
            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "MAP_FILE");
                    TechnologicalMapGrid.UpdateItems(ds);

                    if (ds.Items.Count == 0)
                    {
                        Form.SetStatus("Ничего не найдено", 1);
                    }
                }
            }

            GridToolbar.IsEnabled = true;
        }

        /// <summary>
        /// Проверки перед запросом файла техкарты
        /// </summary>
        private void CheckArticle()
        {
            var v = Form.GetValues();
            bool resume = true;
            
            string article = v.CheckGet("ARTICLE");
            string size = v.CheckGet("SIZE");

            if ((string.IsNullOrEmpty(article)) && string.IsNullOrEmpty(size))
            {
                Form.SetStatus("Нет данных для поиска", 1);
                resume = false;
            }

            if (resume)
            {
                if ((size.Length == 0) && (article.Length < 7))
                {
                    Form.SetStatus("Недостаточно данных для поиска", 1);
                    resume = false;
                }
            }

            if (resume)
            {
                if (article.Length > 6)
                {
                    if (article.Substring(3, 1) != ".")
                    {
                        Form.SetStatus("Неверный формат артикула для поиска", 1);
                        resume = false;
                    }
                }
            }

            if (resume)
            {
                if (article.Length > 7)
                {
                    v.CheckAdd("ARTICLE", article.Substring(0, 7));
                }
            }

            if (resume)
            {
                GetMapFile(v);
            }
        }

        /// <summary>
        /// Копирование файла техкарты в прикрепленные к образцу файлы
        /// </summary>
        private async void CopyMapToAttach(Dictionary<string, string> row)
        {
            string technologicalMapPath = TechnologicalMapGrid.SelectedItem.CheckGet("MAP_FILE_PATH");
            if (!string.IsNullOrEmpty(technologicalMapPath))
            {
                if (File.Exists(technologicalMapPath))
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "SampleAttachment");
                    q.Request.SetParam("Action", "CopyFile");
                    q.Request.SetParam("ID", SampleId.ToString());
                    q.Request.SetParam("FILE_PATH", technologicalMapPath);

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
                                //отправляем сообщение гриду об обновлении
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "PreproductionSample",
                                    ReceiverName = ReceiverName,
                                    SenderName = TabName,
                                    Action = "Refresh",
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
                else
                {
                    Form.SetStatus("Файл не найден", 1);
                }

            }
            else
            {
                Form.SetStatus("Нет пути к техкарте", 1);
            }
        }

        /// <summary>
        /// Открывает файл техкарты в программе Excel
        /// </summary>
        /// <param name="row"></param>
        private void OpenMapFile(Dictionary<string, string> row)
        {
            string technologicalMapPath = row.CheckGet("MAP_FILE_PATH");
            if (!string.IsNullOrEmpty(technologicalMapPath))
            {
                if (File.Exists(technologicalMapPath))
                {
                    Central.OpenFile(technologicalMapPath);
                }
                else
                {
                    Form.SetStatus("Файл не найден", 1);
                }
            }
            else
            {
                Form.SetStatus("Нет пути к техкарте", 1);
            }
        }

        /// <summary>
        /// Открывает диалог сохранения файла техкарты
        /// </summary>
        /// <param name="row"></param>
        private void SaveMapFile(Dictionary<string, string> row)
        {
            string technologicalMapPath = row.CheckGet("MAP_FILE_PATH");
            if (!string.IsNullOrEmpty(technologicalMapPath))
            {
                if (File.Exists(technologicalMapPath))
                {
                    Central.SaveFile(technologicalMapPath);
                }
                else
                {
                    Form.SetStatus("Файл не найден", 1);
                }
            }
            else
            {
                Form.SetStatus("Нет пути к техкарте", 1);
            }
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            CheckArticle();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (TechnologicalMapGrid.SelectedItem != null)
            {
                CopyMapToAttach(TechnologicalMapGrid.SelectedItem);
            }
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (TechnologicalMapGrid.SelectedItem != null)
            {
                OpenMapFile(TechnologicalMapGrid.SelectedItem);
            }
        }

        private void SaveMapFile_Click(object sender, RoutedEventArgs e)
        {
            if (TechnologicalMapGrid.SelectedItem != null)
            {
                SaveMapFile(TechnologicalMapGrid.SelectedItem);
            }
        }
    }
}
