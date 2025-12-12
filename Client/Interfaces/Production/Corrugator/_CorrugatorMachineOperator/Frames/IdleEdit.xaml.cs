using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;
using System.Windows.Media;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperator
{
    /// <summary>
    /// редактирование простоя
    /// </summary>
    /// <author>zelenskiy_sv</author>
    public partial class IdleEdit : UserControl
    {
        public IdleEdit()
        {
            Id = 0;
            FrameName = "IdleEdit";

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);
            
            Init();

            IdleReasonGridInit();
            IdleReasonDetailGridInit();
            StanokUnitGridInit();
            DowntimeDefectTypeGridInit();

            IdleReasonGrid.LayoutTransform = new ScaleTransform(1.4, 1.4);
            IdleReasonDetailGrid.LayoutTransform = new ScaleTransform(1.4, 1.4);
            StanokUnitGrid.LayoutTransform = new ScaleTransform(1.4, 1.4);
            DowntimeDefectTypeGrid.LayoutTransform = new ScaleTransform(1.4, 1.4);

            SetDefaults();
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// идентификатор записи, с которой работает форма
        /// (primary key записи таблицы)
        /// </summary>
        public int Id { get; set; }

        public int IdMachine { get; set; }

        /// <summary>
        /// идентификатор записи причины простоя
        /// </summary>
        public int SelectedReasonID { get; set; }

        /// <summary>
        /// идентификатор записи описания причины простоя
        /// </summary>
        public int SelectedReasonDetailID { get; set; }

        /// <summary>
        /// Выбранная запись в IdleGrid
        /// </summary>
        public Dictionary<string, string> SelectedIdleItem { get; set; }

        public delegate void OnCloseDelegate();
        public OnCloseDelegate OnClose;

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="REASON",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=IdleReasonText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        //{ FormHelperField.FieldFilterRef.Required, null },
                    },
                },
            };

            Form.SetFields(fields);

            //после установки значений
            Form.AfterSet = (Dictionary<string, string> v) =>
            {
                //фокус на ввод причины простоя
                IdleReasonText.Focus();
            };
        }

        /// <summary>
        /// инициализация грида (причины простоев)
        /// </summary>
        public void IdleReasonGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                     new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=30,
                    },
                     new DataGridHelperColumn
                    {
                        Header="Причина",
                        Path="NAME",
                        Doc="Причина простоя",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        MaxWidth=5000,
                    },
                    new DataGridHelperColumn
                    {
                        Header="_",
                        Path="ID_REASON",
                        Doc="ИД причины простоя",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                };
                IdleReasonGrid.SetColumns(columns);

                IdleReasonGrid.UseRowHeader = false;
                IdleReasonGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                IdleReasonGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {
                        SelectedReasonID = selectedItem.CheckGet("ID").ToInt();
                        IdleReasonDetailGrid.LoadItems();
                    }
                };

                //данные грида
                IdleReasonGrid.OnLoadItems = IdleReasonGridLoadItems;

                IdleReasonGrid.Run();
            }
        }

        /// <summary>
        /// инициализация грида (описание причин простоев)
        /// </summary>
        public void IdleReasonDetailGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                     new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=30,
                    },
                     new DataGridHelperColumn
                    {
                        Header="Описание",
                        Path="DESCRIPTION",
                        Doc="Описание",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        MaxWidth=5000,
                    },
                    new DataGridHelperColumn
                    {
                        Header="_",
                        Path="ID_REASON_DETAIL",
                        Doc="ИД описания причины простоя",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                };
                IdleReasonDetailGrid.SetColumns(columns);

                IdleReasonDetailGrid.UseRowHeader = false;
                IdleReasonDetailGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                IdleReasonDetailGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {
                        SelectedReasonDetailID = selectedItem.CheckGet("ID_REASON_DETAIL").ToInt();
                    }
                };

                //данные грида
                IdleReasonDetailGrid.OnLoadItems = IdleReasonDetailGridLoadItems;

                IdleReasonDetailGrid.Run();
            }
        }

        /// <summary>
        /// получение причин простоев
        /// </summary>
        public async void IdleReasonGridLoadItems()
        {
            bool resume = true;

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Idle");
                q.Request.SetParam("Action", "ReasonList");

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;
                
                IdleReasonGrid.IsEnabled = false;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                IdleReasonGrid.IsEnabled = true;

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            IdleReasonGrid.UpdateItems(ds);

                            // Установка курсора на требуемую запись
                            if (ds.Items.Count > 0)
                            {
                                foreach (var item in IdleReasonGrid.Items)
                                {
                                    if (item.CheckGet("ID").ToInt() == SelectedIdleItem.CheckGet("IDREASON").ToInt())
                                    {
                                        IdleReasonGrid.SelectRowByKey(item.CheckGet("_ROWNUMBER").ToInt(), "_ROWNUMBER", true);
                                    }
                                }
                            }

                            IdleReasonDetailGrid.LoadItems();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// получение описаний причин простоев
        /// </summary>
        public async void IdleReasonDetailGridLoadItems()
        {
            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_REASON_DETAIL", SelectedReasonID.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Idle");
                q.Request.SetParam("Action", "ReasonDetailList");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                IdleReasonDetailGrid.IsEnabled = false;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                IdleReasonDetailGrid.IsEnabled = true;

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            IdleReasonDetailGrid.UpdateItems(ds);

                            // Установка курсора на требуемую запись
                            if (ds.Items.Count > 0)
                            {
                                foreach (var item in IdleReasonDetailGrid.Items)
                                {
                                    if (item.CheckGet("ID_REASON_DETAIL").ToInt() == SelectedIdleItem.CheckGet("ID_REASON_DETAIL").ToInt())
                                    {
                                        IdleReasonDetailGrid.SelectRowByKey(item.CheckGet("_ROWNUMBER").ToInt(), "_ROWNUMBER", true);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        #region Для АСУТП
        private void StanokUnitGridInit()
        {
            {
                var columns = new List<DataGridHelperColumn>
                {
                     new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=30,
                    },
                     new DataGridHelperColumn
                    {
                        Header="Узел",
                        Path="NAME_UNIT",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        MaxWidth=5000,
                    },
                    new DataGridHelperColumn
                    {
                        Header="_",
                        Path="ID_UNIT",
                        Doc="ИД",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                };
                StanokUnitGrid.SetColumns(columns);
                StanokUnitGrid.PrimaryKey = "ID_UNIT";
                StanokUnitGrid.UseRowHeader = false;
                StanokUnitGrid.Init();

                StanokUnitGrid.Run();
            }
        }

        private void DowntimeDefectTypeGridInit()
        {
            {
                var columns = new List<DataGridHelperColumn>
                {
                     new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=30,
                    },
                     new DataGridHelperColumn
                    {
                        Header="Тип дефекта",
                        Path="DEFECT_TYPE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        MaxWidth=5000,
                    },
                    new DataGridHelperColumn
                    {
                        Header="_",
                        Path="DODT_ID",
                        Doc="ИД",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                };
                DowntimeDefectTypeGrid.SetColumns(columns);
                DowntimeDefectTypeGrid.PrimaryKey = "DODT_ID";
                DowntimeDefectTypeGrid.UseRowHeader = false;
                DowntimeDefectTypeGrid.Init();
                DowntimeDefectTypeGrid.Run();
            }
        }

        private async void GetDataForTechnicalDescriptionGrid()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Idle");
            q.Request.SetParam("Action", "GetTechnicalData");
            q.Request.SetParam("ID_ST", $"{IdMachine}");
            q.Request.SetParam("IDIDLES", Id.ToString());

            await Task.Run(() => q.DoQuery());

            if (q.Answer.Status == 0)
            {
                var data = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                if (data != null && data.Count > 0)
                {
                    var items1 = ListDataSet.Create(data, "ITEMS_1");
                    items1.Items.Insert(0, new Dictionary<string, string>
                        {
                            { "_ROWNUMBER", "0" },
                            { "NAME_UNIT", "Не выбрано" },
                            { "ID_UNIT", "-1" }
                        });

                    var items2 = ListDataSet.Create(data, "ITEMS_2");
                    items2.Items.Insert(0, new Dictionary<string, string>
                        {
                            { "_ROWNUMBER", "0" },
                            { "DEFECT_TYPE", "Не выбрано" },
                            { "DODT_ID", "-1" }
                        });

                    // Заполнение таблиц
                    DowntimeDefectTypeGrid.UpdateItems(items2);
                    StanokUnitGrid.UpdateItems(items1);

                    var idleinfo = ListDataSet.Create(data, "ITEMS_3").GetFirstItem();

                    MeasuresTakenText.Text = idleinfo.CheckGet("MEASURES_TAKEN");

                    DowntimeDefectTypeGrid.SelectRowByKey(idleinfo.CheckGet("DODT_ID").ToInt() == 0 ? -1 
                        : idleinfo.CheckGet("DODT_ID").ToInt(), "DODT_ID");

                    StanokUnitGrid.SelectRowByKey(idleinfo.CheckGet("ID_UNIT").ToInt() == 0 ? -1 
                        : idleinfo.CheckGet("ID_UNIT").ToInt(), "ID_UNIT");
                }
            } 
            else
            {
                var dialog = new DialogWindow("Не удалось получить данные для технической службы", "Ошибка");
                dialog.ShowDialog();
            }
        }
        #endregion

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();
            
        }

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "CorrugatorMachineOperator",
                ReceiverName = "",
                SenderName = "IdleEdit",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;
            }
        }
        
        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            GetDataForTechnicalDescriptionGrid();
            Central.WM.FrameMode = 1;
            var frameName = GetFrameName();
            Central.WM.Show(frameName, "Описание причины простоя", true, "add", this);
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            OnClose();
            var frameName = GetFrameName();
            Central.WM.Close(frameName);

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
        }

        /// <summary>
        /// формирует уникальный идентификатор фрейма
        /// </summary>
        /// <returns></returns>
        public string GetFrameName()
        {
            string result = "";
            result = $"{FrameName}_{Id}";
            return result;
        }

        /// <summary>
        /// подготовка и отправка данных
        /// </summary>
        public void Save()
        {
            bool resume = true;
            string error = "";

            if (resume)
            {
                var validationResult = Form.Validate();
                if (!validationResult)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                SaveData();
            }
            else
            {
                Form.SetStatus(error, 1);
            }
        }

        /// <summary>
        /// отпаравка данных на сервер
        /// </summary>
        public async void SaveData()
        {
            DisableControls();

            var idReason = IdleReasonGrid.SelectedItem.CheckGet("ID").ToInt();
            var idReasonDetail = IdleReasonDetailGrid.SelectedItem.CheckGet("ID_REASON_DETAIL").ToInt();
            var idDowntime = DowntimeDefectTypeGrid.SelectedItem.CheckGet("DODT_ID").ToInt();
            var idUnit = StanokUnitGrid.SelectedItem.CheckGet("ID_UNIT").ToInt();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Idle");
            q.Request.SetParam("Action", "DetailSave");
            q.Request.SetParam("IDIDLES", Id.ToString());
            q.Request.SetParam("IDREASON", idReason.ToString());
            q.Request.SetParam("ID_REASON_DETAIL", idReasonDetail.ToString());
            q.Request.SetParam("REASON", IdleReasonText.Text);
            q.Request.SetParam("MEASURES_TAKEN", MeasuresTakenText.Text);
            q.Request.SetParam("DODT_ID", idDowntime == -1 ? string.Empty : idDowntime.ToString());
            q.Request.SetParam("ID_UNIT", idUnit == -1 ? string.Empty : idUnit.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                //отправляем сообщение гриду о необходимости обновить данные
                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = "CorrugatorMachineOperator",
                    ReceiverName = "Idles",
                    SenderName = "IdleEdit",
                    Action = "Refresh",
                    Message = $"{Id}",
                });

                Close();
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
            IdleReasonGrid.IsEnabled = false;
            IdleReasonText.IsEnabled = false;
            IdleReasonDetailGrid.IsEnabled = false;
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
            IdleReasonGrid.IsEnabled = true;
            IdleReasonText.IsEnabled = true;
            IdleReasonDetailGrid.IsEnabled = true;
        }

        private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }
        
        private void SaveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Save();
        }
        private void CopyButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Clipboard.SetText(IdleReasonText.Text);
        }
        private void PasteButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            IdleReasonText.Text += Clipboard.GetText();
        }

        /// <summary>
        /// При нажатии на enter 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IdleReasonText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                Save();
            }
        }
    }
}
