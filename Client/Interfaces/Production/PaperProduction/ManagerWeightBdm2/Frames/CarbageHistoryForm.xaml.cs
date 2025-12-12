using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// история взвешивания машины с мусором
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    /// <released>2025-03-05</released>
    /// <changed>2025-03-05</changed>
    public partial class CarbageHistoryForm : UserControl
    {
        public CarbageHistoryForm()
        {
            FrameName = "CarbageHistory";
            ReceiverName = "CarbageList";
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();
            SetDefaults();
            CarbageHistoryGridInit();
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Имя вкладки, которая становится активной после закрытия фрейма
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// идентификатор записи, с которой работает форма
        /// </summary>
        public int ChgaId { get; set; }

        /// <summary>
        /// полученные значение от запросов
        /// </summary>
        public List<Dictionary<string, string>> DataList { get; set; }

        /// <summary>
        // инициализация компонентов
        /// </summary>
        public void Init()
        {

        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
        }

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Carbage",
                ReceiverName = "CarbageList",
                SenderName = "CarbageHistory",
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
        /// редактирование записи
        /// </summary>
        /// <param name="p"></param>
        public void Edit(Dictionary<string, string> p)
        {
            ChgaId = p.CheckGet("ID").ToInt();
            GetData();
        }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 0;

            var frameName = GetFrameName();
            Central.WM.Show(frameName, $"Машина ИД [{ChgaId}]", true, "add", this);
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            var frameName = GetFrameName();
            Central.WM.Close(frameName);
        }

        /// <summary>
        /// формирует уникальный идентификатор фрейма
        /// </summary>
        /// <returns></returns>
        public string GetFrameName()
        {
            string result = "";
            result = $"{FrameName}_{ChgaId}";
            return result;
        }

        /// <summary>
        /// получение данных с сервера
        /// </summary>
        public async void GetData()
        {
            DisableControls();
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID", ChgaId.ToString());
            }
            try
            {
                var q = await LPackClientQuery.DoQueryAsync("PaperProduction", "ManagerWeightBdm2", "CarbageAudGet", "ITEMS", p);

                if (q.Answer.Status == 0)
                {
                    if (q.Answer.QueryResult != null)
                    {
                        var ds = q.Answer.QueryResult;
                        GarbageHistoryGrid.UpdateItems(ds);
                        Show();
                    }
                }
            }
            catch (Exception ex)
            {

            }

            EnableControls();
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
        }

        private void CloseButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// инициализация грида информации по взвешенной машине с отходами
        /// </summary>
        private void CarbageHistoryGridInit()
        {
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="№",
                        Path="RN",
                        Doc="№",
                        ColumnType=ColumnTypeRef.Integer,
                         Width2 = 2,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Пользователь",
                        Path="AUDIT_USER",
                        Doc="Пользователь",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата аудита",
                        Path="AUD_DTTM",
                        Doc="Дата аудита",
                        Group="Дата",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                         Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="CHGA_ID",
                        Doc="ИД",
                        ColumnType=ColumnTypeRef.Integer,
                         Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Машина",
                        Path="NOMER_CAR",
                        Doc="Машина",
                        ColumnType=ColumnTypeRef.String,
                         Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="регистрации",
                        Path="CREATED_DTTM",
                        Doc="Дата регистрации",
                        Group="Дата",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                         Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="полная",
                        Path="FULL_DTTM",
                        Doc="Дата полная",
                        Group="Дата",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="пустая",
                        Path="EMPTY_DTTM",
                        Doc="Дата пустая",
                        Group="Дата",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                         Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="полная",
                        Path="WEIGHT_FULL",
                        Doc="Вес полная",
                        Group="Вес",
                        ColumnType=ColumnTypeRef.Integer,
                         Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="пустая",
                        Path="WEIGHT_EMPTY",
                        Doc="Вес пустая",
                        Group="Вес",
                        ColumnType=ColumnTypeRef.Integer,
                         Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="отходы",
                        Path="WEIGHT_FACT",
                        Doc="Вес фактический",
                        Group="Вес",
                        ColumnType=ColumnTypeRef.Integer,
                         Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Без отходов",
                        Path="CARBAGE_EMPTY_FLAG",
                        Doc="Пустой рейс",
                        Group="Вес",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Path="STATUS",
                        Doc="Статус",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 22,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Контролер",
                        Path="NAME",
                        Doc="Контролер",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 14,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Описание",
                        Path="DESCRIPTION",
                        Doc="Описание",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 20,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Участок",
                        Path="REGION",
                        Doc="Участок",
                        ColumnType=ColumnTypeRef.String,
                         Width2 = 12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Полигон",
                        Path="LANDFILL",
                        Doc="Полигон",
                        ColumnType=ColumnTypeRef.String,
                         Width2 = 16,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ контейнера",
                        Path="CONTAINER_NUM",
                        Doc="№ контейнера",
                        ColumnType=ColumnTypeRef.String,
                         Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Бригада",
                        Path="BRIGADE",
                        Doc="Бригада",
                        ColumnType=ColumnTypeRef.String,
                         Width2 = 8,
                    },
                };

                GarbageHistoryGrid.SetColumns(columns);
                GarbageHistoryGrid.SetPrimaryKey("_ROWNUMBER");
                GarbageHistoryGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                GarbageHistoryGrid.AutoUpdateInterval = 0;
                GarbageHistoryGrid.Init();

                //данные грида
                GarbageHistoryGrid.OnLoadItems = GetData;
                GarbageHistoryGrid.Run();
            }
        }





    }
}
