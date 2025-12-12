using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
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
    /// Список активных производственных заданий для выбранного образца с линии для ручной привязки к заданию
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleProductionListManual : ControlBase
    {
        public SampleProductionListManual()
        {
            InitializeComponent();

            ProductId = 0;
            SelectedTaskId = 0;
            SampleId = 0;

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

        public int ProductId;
        /// <summary>
        /// ИД задания на переработку, если оно заполнено у выбранного образца
        /// </summary>
        public int SelectedTaskId;

        public int SampleId;
        /// <summary>
        /// Имя вкладки, окуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;

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
        /// Инициализацмя таблицы
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
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Номер",
                    Path="TASK_NUMBER",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Линия",
                    Path="LINE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="TASK_NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Плановое время начала работы станка",
                    Path="PLAN_START_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=20,
                    Format="dd.MM.yyyy HH:mm",
                },
            };
            Grid.SetColumns(columns);
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.OnLoadItems = LoadItems;
            Grid.Toolbar = FormToolbar;
            Grid.SetPrimaryKey("ID");
            Grid.AutoUpdateInterval = 0;

            Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                {
                    StylerTypeRef.BackgroundColor,
                    (Dictionary<string, string> row) =>
                    {
                        var result = DependencyProperty.UnsetValue;
                        var color = "";

                        if (row.CheckGet("ID").ToInt() == SelectedTaskId)
                        {
                            color = HColor.Blue;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result = color.ToBrush();
                        }

                        return result;
                    }
                }
            };

            Grid.OnSelectItem = (selectItem) =>
            {
                FormStatus.Text = "";
            };
            Grid.OnDblClick = (selectItem) =>
            {
                Save();
            };

            Grid.Init();
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        private async void LoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "ListTaskManual");
            q.Request.SetParam("PRODUCT_ID", ProductId.ToString());

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
                    var ds = ListDataSet.Create(result, "TASKS");
                    Grid.UpdateItems(ds);

                    if (ds.Items == null)
                    {
                        FormStatus.Text = "Нет заданий на переработку";
                        SaveButton.IsEnabled = false;
                    }
                    else if (ds.Items.Count == 0)
                    {
                        FormStatus.Text = "Нет заданий на переработку";
                        SaveButton.IsEnabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// Отображение вкладки
        /// </summary>
        public void Show()
        {
            string title = "Выбор задания для образца";
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

        /// <summary>
        /// Сохранение связи 
        /// </summary>
        public async void Save()
        {
            int taskId = Grid.SelectedItem.CheckGet("ID").ToInt();
            if (taskId > 0)
            {
                if (taskId != SelectedTaskId)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "Samples");
                    q.Request.SetParam("Action", "BindConverting");
                    q.Request.SetParam("SAMPLE_ID", SampleId.ToString());
                    q.Request.SetParam("TASK_ID", taskId.ToString());

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
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "PreproductionSample",
                                    ReceiverName = ReceiverName,
                                    SenderName = ControlName,
                                    Action = "Refresh",
                                });
                                Central.Msg.SendMessage(new ItemMessage()
                                {
                                    ReceiverGroup = "PreproductionSample",
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
                else
                {
                    Close();
                }
            }
            else
            {
                FormStatus.Text = "Ничего не выбрано";
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
    }
}
