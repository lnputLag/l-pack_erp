using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Service
{
    /// <summary>
    /// просмотр логов работы агента
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    /// <released>2024-05-14</released>
    /// <changed>2024-05-14</changed>
    public partial class FireAlarmLog : UserControl
    {
        public FireAlarmLog()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);
            Loaded += OnLoad;

            FireAlarmLogGridInit();
            SetDefaults();

            HostUserId = "";
            Title = "Протокол работы агента";
            LogTableName = "fire_alarm_report";
        }

        public List<DataGridHelperColumn> Columns { get; private set; }

        /// <summary>
        /// Имя папки верхнего уровня, в которой хранится лог файл по работе агента
        /// </summary>
        public string LogTableName { get; set; }

        /// <summary>
        /// форма с полями для фильтрации данных
        /// </summary>
        public FormHelper FireAlarmLogForm { get; set; }

        /// <summary>
        /// данные формы изменились
        /// </summary>
        private bool ScreenShotFormChanged { get; set; }

        /// <summary>
        /// имя хоста
        /// </summary>
        public string HostUserId { get; set; }
        public string Title { get; set; }

        /// <summary>
        /// флаг поднимается на время ожидания данных от сервера
        /// </summary>
        private bool LoadingData { get; set; }

        /// <summary>
        /// изображение загружено
        /// </summary>
        private bool ImageLoaded { get; set; }

        private MemoryStream ImageStream { get; set; }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void FireAlarmLogGridInit()
        {
            //инициализация формы
            {
                FireAlarmLogForm = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="FROM_DATE",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=FromDate,
                        ControlType="TextBox",
                        Default=DateTime.Now.ToString("dd.MM.yyyy"),
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="TO_DATE",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=ToDate,
                        ControlType="TextBox",
                        Default=DateTime.Now.AddDays(1).ToString("dd.MM.yyyy"),
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                };

                FireAlarmLogForm.SetFields(fields);

                //после установки значений
                FireAlarmLogForm.AfterSet = (Dictionary<string, string> v) =>
                {
                    //фокус на кнопку обновления
                    RefreshButton.Focus();
                };
            }

            //инициализация грида
            //колонки грида
            Columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Дата",
                        Path="ON_DATE",
                        Doc="Дата записи",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2 = 50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Описание",
                        Path="MESSAGE",
                        Doc="Описание",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 500,
                    },
                };

            FireAlarmLogGrid.SetColumns(Columns);
            FireAlarmLogGrid.SetPrimaryKey("ID");
            FireAlarmLogGrid.SetSorting("ON_DATE", ListSortDirection.Descending);
            FireAlarmLogGrid.SearchText = SearchText;
            FireAlarmLogGrid.AutoUpdateInterval = 0;
            FireAlarmLogGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;

            FireAlarmLogGrid.Run();
            //данные грида
            FireAlarmLogGrid.OnLoadItems = FireAlarmLogLoadItems;
            FireAlarmLogGrid.Init();
        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Service",
                ReceiverName = "",
                SenderName = "FireAlarmLogList",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            FireAlarmLogGrid.Destruct();
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            FireAlarmLogForm.SetDefaults();
            CheckFormChanged(true);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("Client") > -1)
            {
                switch (m.Action)
                {
                    case "Refresh":
                        FireAlarmLogGrid.LoadItems();
                        break;
                }
            }
        }

        /// <summary>
        /// получение записей SYSTEM_HOSTNAME=GRESHNYH-NI
        /// </summary>
        public async void FireAlarmLogLoadItems()
        {
            bool resume = true;

            var f = FromDate.Text.ToDateTime();
            var t = ToDate.Text.ToDateTime();

            if (resume)
            {
                if (DateTime.Compare(f, t) > 0)
                {
                    var msg = "Дата начала должна быть меньше даты окончания.";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();
                    resume = false;
                }
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("TABLE_NAME", LogTableName);
                    p.Add("TABLE_DIRECTORY", HostUserId);
                    p.Add("DATE_FROM", FromDate.Text + " 00:00:00");
                    p.Add("DATE_TO", ToDate.Text + " 00:00:00");
                    // 1=global,2=local,3=net
                    p.Add("STORAGE_TYPE", "3");                    
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Service");
                q.Request.SetParam("Object", "LiteBase");
                q.Request.SetParam("Action", "List2");
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
                        {
                            var ds = ListDataSet.Create(result, LogTableName);
                            FireAlarmLogGrid.UpdateItems(ds);
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
        /// фильтрация записей (аккаунты)
        /// </summary>
        public void FilterItems()
        {
            UpdateActions(null);
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            FireAlarmLogGrid.ShowSplash();
            LoadingData = true;
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            FireAlarmLogGrid.HideSplash();
            LoadingData = false;
        }


        public void Edit()
        {
            FireAlarmLogGrid.LoadItems();
            Show();
        }

        /// <summary>
        /// Отображение вкладки с формой редактирования данных водителя
        /// </summary>
        public void Show()
        {
            string tabTitle = $"{Title}";
            var tabName = GetFrameName();
            Central.WM.AddTab(tabName, tabTitle, true, "add", this);

        }

        /// <summary>
        /// Закрытие фрейма
        /// </summary>
        public void Close()
        {
            var tabName = GetFrameName();
            Central.WM.RemoveTab(tabName);
            Destroy();
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            var tabName = GetFrameName();
            Central.WM.SetActive(tabName);
        }

        private void CheckFormChanged(bool changed = false)
        {
            ScreenShotFormChanged = changed;

            if (ScreenShotFormChanged)
            {
                RefreshButton.Style = (Style)RefreshButton.TryFindResource("FButtonPrimary");
            }
            else
            {
                RefreshButton.Style = (Style)RefreshButton.TryFindResource("FButtonCancel");
            }
        }

        public string GetFrameName()
        {
            var result = "";
            result = $"fire_alarm_log_{HostUserId}";
            result = result.MakeSafeName();
            return result;
        }

        /// <summary>
        /// обновление методов работы с выбранной в гриде строкой
        /// </summary>
        /// <param name="selectedItem"></param>
        public void UpdateActions(Dictionary<string, string> selectedItem)
        {
        }

        /// <summary>
        /// обработка ввода с клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
        }

        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            FireAlarmLogGrid.LoadItems();
        }

        private void FromDate_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckFormChanged(true);
        }

        private void ToDate_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckFormChanged(true);
        }

        private void ExportButton_Click_1(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }

        private async void ExportToExcel()
        {
            var list = FireAlarmLogGrid.Items;
            var eg = new ExcelGrid();
            eg.SetColumnsFromGrid(Columns);
            eg.Items = list;
            await Task.Run(() =>
            {
                eg.Make();
            });
        }
    }
}
