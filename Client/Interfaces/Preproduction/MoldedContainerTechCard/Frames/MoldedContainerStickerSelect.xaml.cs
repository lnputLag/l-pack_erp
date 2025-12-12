using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Выбор этикетки для техкарты литой тары
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class MoldedContainerStickerSelect : ControlBase
    {
        /// <summary>
        /// Выбор этикетки для техкарты литой тары
        /// </summary>
        public MoldedContainerStickerSelect()
        {
            InitializeComponent();
            InitGrid();

            OnUnload = () =>
            {
                Grid.Destruct();
            };

        }
        /// <summary>
        /// Имя вкладки, окуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;
        /// <summary>
        /// ID техкарты, для которой выбирается этикетка
        /// </summary>
        public int TechCardId;

        /// <summary>
        /// Обработчик команд
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
                    case "showsticker":
                        LoadSticker();
                        break;
                }
            }
        }

        /// <summary>
        /// Инициализация грида
        /// </summary>
        public void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                },
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("ID");
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            Grid.SearchText = SearchText;
            Grid.Toolbar = Toolbar;
            Grid.Commands = Commander;

            Grid.AutoUpdateInterval = 0;
            // Раскраска строк
            Grid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                // Цвета фона строк
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=System.Windows.DependencyProperty.UnsetValue;
                        var color = "";

                        if (row.CheckGet("USED_QTY").ToInt() == 0)
                        {
                            color = HColor.Green;
                        }

                        if (!color.IsNullOrEmpty())
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };

            Grid.OnLoadItems = LoadItems;
            Grid.OnFilterItems = FilterItems;
            /*
            Grid.OnSelectItem = (selectItem) =>
            {
                UpdateActions(selectItem);
            };
            */
            Grid.OnDblClick = (selectItem) =>
            {
                Save();
            };
            Grid.Init();

        }

        /// <summary>
        /// Загрузка содержимого таблицы
        /// </summary>
        private async void LoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "StickerSelect");

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
                    var ds = ListDataSet.Create(result, "STICKERS");
                    Grid.UpdateItems(ds);

                    if (ds.Items == null)
                    {
                        FormStatus.Text = "Нет свободных этикеток";
                        SaveButton.IsEnabled = false;
                    }
                    else if (ds.Items.Count == 0)
                    {
                        FormStatus.Text = "Нет свободных этикеток";
                        SaveButton.IsEnabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// Фильтрация строк в таблице
        /// </summary>
        private void FilterItems()
        {
            bool unbindedOnly = (bool)UnbindedCheckBox.IsChecked;

            if (Grid.Items != null)
            {
                if (Grid.Items.Count > 0)
                {
                    var items = new List<Dictionary<string, string>>();
                    foreach (Dictionary<string, string> row in Grid.Items)
                    {
                        bool includeByUsed = true;
                        
                        if (unbindedOnly)
                        {
                            includeByUsed = false;
                            if (row.CheckGet("USED_QTY").ToInt() == 0)
                            {
                                includeByUsed = true;
                            }
                        }

                        if (includeByUsed)
                        {
                            items.Add(row);
                        }
                    }

                    Grid.Items = items;
                }
            }
        }

        /// <summary>
        /// Передача данных выбранной этикетки в техкарту
        /// </summary>
        public void Save()
        {
            if (Grid.Items != null)
            {
                if (Grid.SelectedItem != null)
                {
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverGroup = "Preproduction",
                        ReceiverName = ReceiverName,
                        SenderName = ControlName,
                        Action = "StickerSelected",
                        ContextObject = Grid.SelectedItem,
                    });
                    Close();
                }
                else
                {
                    FormStatus.Text = "Выберите этикетку в таблице";
                }
            }
            else
            {
                FormStatus.Text = "Нет этикеток для выбора";
            }
        }

        /// <summary>
        /// Загружает и открывает изображение этикетки
        /// </summary>
        private async void LoadSticker()
        {
            int stickerId = Grid.SelectedItem.CheckGet("ID").ToInt();
            if (stickerId > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "Sticker");
                q.Request.SetParam("Action", "GetDrawingFile");
                q.Request.SetParam("ID", stickerId.ToString());
                q.Request.SetParam("FILE_TYPE", "2");
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
                    FormStatus.Text = q.Answer.Error.Message;
                }
            }
        }

        private void UpdateActions(Dictionary<string, string> selectedItem)
        {
            bool imageLoaded = selectedItem.CheckGet("IMAGE_LOADED").ToBool();
            ShowStickerButton.IsEnabled = imageLoaded;
        }

        /// <summary>
        /// Отображение вкладки
        /// </summary>
        public void Show()
        {
            string title = "Выбор этикетки";
            Central.WM.Show(ControlName, title, true, "add", this);
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

        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            var b = (Button)sender;
            if (b != null)
            {
                var t = b.Tag.ToString();
                if (!t.IsNullOrEmpty())
                {
                    ProcessCommand(t);
                }
            }
        }

        private void UnbindedCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Grid.UpdateItems();
        }
    }
}
