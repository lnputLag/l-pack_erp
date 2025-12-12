using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Окно выбора техкарты для связывания с этикеткой
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class MoldedContainerStickerTechcardSelect : ControlBase
    {
        public MoldedContainerStickerTechcardSelect()
        {
            InitializeComponent();

            InitGrid();

            OnUnload = () =>
            {
                Grid.Destruct();
            };

            OnFocusGot = () =>
            {
                Grid.ItemsAutoUpdate = true;
                Grid.Run();
            };

            OnFocusLost = () =>
            {
                Grid.ItemsAutoUpdate = false;
            };
        }

        /// <summary>
        /// ID этикетки
        /// </summary>
        public int StickerId;
        /// <summary>
        /// Имя вкладки, окуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;
        /// <summary>
        /// Флаг переименовывания этикетки
        /// </summary>
        public int RenameSticker;

        /// <summary>
        /// Обработка комманд
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
        /// Инициализация грида
        /// </summary>
        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Техкарта",
                    Path="TECHCARD_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Потребитель",
                    Path="CUSTOMER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
            };
            Grid.SetColumns(columns);
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            Grid.OnLoadItems = LoadItems;
            Grid.Toolbar = FormToolbar;
            Grid.SetPrimaryKey("ID");
            Grid.SetSorting("ID", ListSortDirection.Descending);
            Grid.AutoUpdateInterval = 0;
            Grid.OnDblClick = (selectItem) =>
            {
                Save();
            };

            Grid.Init();
        }

        private async void LoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Sticker");
            q.Request.SetParam("Action", "ListTechcardSelect");

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Grid.ClearItems();
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "TECHCARDS");
                    Grid.UpdateItems(ds);

                    if (ds.Items == null)
                    {
                        FormStatus.Text = "Нет техкарт с непривязанной этикеткой";
                        SaveButton.IsEnabled = false;
                    }
                    else if (ds.Items.Count == 0)
                    {
                        FormStatus.Text = "Нет техкарт с непривязанной этикеткой";
                        SaveButton.IsEnabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// Сохранение выбора техкарты
        /// </summary>
        public async void Save()
        {
            int techcardId = Grid.SelectedItem.CheckGet("ID").ToInt();
            if (techcardId != 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "Sticker");
                q.Request.SetParam("Action", "BindTechcard");
                q.Request.SetParam("STICKER_ID", StickerId.ToString());
                q.Request.SetParam("LAST_STICKER_ID", StickerId.ToString());
                q.Request.SetParam("TECHCARD_ID", techcardId.ToString());
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
                            //отправляем сообщение о закрытии окна
                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverGroup = "PreproductionContainer",
                                ReceiverName = ReceiverName,
                                SenderName = ControlName,
                                Action = "Refresh",
                            });
                            Close();
                        }
                    }
                }
                else if (q.Answer.Error.Code == 145)
                {
                    FormStatus.Text = q.Answer.Error.Message;
                }
            }
        }

        /// <summary>
        /// Показ формы
        /// </summary>
        public void Show()
        {
            string title = "Выбор техкарты ЛТ";
            Central.WM.Show(ControlName, title, true, "add", this);
        }

        /// <summary>
        /// Закрытие формы
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
        /// Обработка нажатия кнопки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
    }
}
