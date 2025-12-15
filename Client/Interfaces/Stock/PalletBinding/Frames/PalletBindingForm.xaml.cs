using Client.Common;
using Client.Interfaces.Main;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Stock.PalletBinding.Frames
{
    /// <summary>
    /// Логика взаимодействия для PalletBindingForm.xaml
    /// </summary>
    public partial class PalletBindingForm : ControlBase
    {
        public PalletBindingForm()
        {
            PalletId = 0;

            InitializeComponent();

            FrameMode = 0;
            OnGetFrameTitle = () =>
            {
                var result = "";

                var id = PalletId;

                 result = $"Привязка поддона №{id}"; // отрисовка внизу
               
                return result;
            };

            // обработка сообщений между компонентами
            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            // Обработка нажатий клавиш
            OnKeyPressed = (System.Windows.Input.KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };

            // Инициализация при загрузке
            OnLoad = () =>
            {
                ShipmentGridInit();     //инициализация таблицы аккаунтов
                SetDefaults();         //установка значений по умолчанию
            };

            // Очистка ресурсов при выгрузке
            OnUnload = () =>
            {
                ShipmentGrid.Destruct();
            };

            // Управление автообновлением таблицы при фокусе
            // Активная вкладка 
            OnFocusGot = () =>
            {
                ShipmentGrid.ItemsAutoUpdate = true;
                ShipmentGrid.Run();
            };

            // Управление автообновлением таблицы при фокусе
            // Переход на другую вкладку
            OnFocusLost = () =>
            {
                ShipmentGrid.ItemsAutoUpdate = false;
            };

            {
                Commander.Add(new CommandItem()
                {
                    Name = "save",
                    Enabled = true,
                    Title = "Сохранить",
                    Description = "",
                    ButtonUse = true,
                    ButtonName = "SaveButton",
                    HotKey = "Ctrl+Return",
                    Action = () =>
                    {
                        //Save();
                    },
                }); ;
                Commander.Add(new CommandItem()
                {
                    Name = "cancel",
                    Enabled = true,
                    Title = "Отмена",
                    Description = "",
                    ButtonUse = true,
                    ButtonName = "CancelButton",
                    HotKey = "Escape",
                    Action = () =>
                    {
                        Hide();
                    },
                });
                Commander.Init(this);
            }

            Commander.Init(this);
            SetDefaults();
        }

        public int PalletId;

        public void SetDefaults()
        {
            
        }

        private void ShipmentGridInit()
        {
            ///<summary>
            /// 
            ///</summary>
            // Определение колонок
            // Каждый объект описывает одну колонку таблицы
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Дата отгрузки",
                    Path="DTTM",
                    Description = "Дата отгрузки",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Покупатель",
                    Path="NAME", 
                    Description="Покупатель",
                    ColumnType=ColumnTypeRef.String,
                    Width2=14,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="QTY",
                    Description="Количество",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="На складе",
                    Path="KOL",
                    Description="",
                    ColumnType=ColumnTypeRef.Double,
                    Format = "N0",
                    Width2=16,
                },
                 new DataGridHelperColumn
                {
                    Header="ИД заявки",
                    Path="IDORDERDATES",
                    Description="ИД заявки",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
            };

            // Ещё не менял
            ///<summary>
            /// Стилизаия строк по правилам
            /// RowStylers — это словарь, где ключ описывает тип стилизации (например, BackgroundColor), 
            /// а значение — делегат (функция), которая для каждой строки возвращает значение стиля (например, цвет фона).
            ///</summary>
            //ShipmentGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            //{
            //    {
            //        DataGridHelperColumn.StylerTypeRef.BackgroundColor,
            //        row => 
            //        {
            //            var result=DependencyProperty.UnsetValue;
            //            var color = "";

            //            var currentStatus = row.CheckGet("idts").ToBool(); //  Выше проверит к чему относится idts
            //            if (currentStatus == true)
            //            {
            //                color = HColor.Red;
            //            }

            //            var isEmployee = row.CheckGet("IS_EMPLOYEE").ToBool();
            //            if (isEmployee == false)
            //            {
            //                //это общий аккаунт - что это значит?
            //                color = HColor.Blue;
            //            }

            //            if (!string.IsNullOrEmpty(color))
            //            {
            //                result=color.ToBrush();
            //            }

            //            return result;
            //        }
            //    },
            //};
            ///<summary>
            /// Привязка колонок и базовые настройки сетки
            ///</summary> 
            ShipmentGrid.SetColumns(columns); //сообщает таблице какие колонки показывать
            ShipmentGrid.SetPrimaryKey("PALLET_ID"); //указывает, что поле ID является уникальным ключом для каждой строки
            ShipmentGrid.SetSorting("PALLET_ID", ListSortDirection.Ascending); //начальная сортировка по данной колонке по-умолчанию по возрастанию
            ShipmentGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact; //режим отображения ширины колонок
            //ShipmentGrid.SearchText = ShipmentGridSearch;  (оно не нужно же?)
            ShipmentGrid.Toolbar = FormToolbar; // привязка панели инструментов отвечающую за кнопки/действия таблицы

            ///<summary>
            /// Как загружать данные (запрос)
            ///</summary>
            ShipmentGrid.QueryLoadItems = new RequestData() //описывает как сетка должна запрашивать данные с сервера, структура с параметрами:
            {
                Module = "Stock",
                Object = "PalletBinding",
                Action = "ListAttachin",
                AnswerSectionKey = "ATTACHIN_PALLETS",
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                            {
                                //{ "FACT_ID", StatusSelectBox.SelectedItem.Key},  //Поискать реализацию SelectBox
                                { "PRODUCT_ID", Central.User.AccountId.ToString() }
                            };
                },
            };

            ///<summary>
            /// Команды и инициализация
            ///</summary>
            ShipmentGrid.Commands = Commander; //Привязка набора команд(кнопок/действий), которые будут доступны в таблице(CRUD)
            ShipmentGrid.Init();   //финальная инициализация: таблица применит все настройки, возможно выполнит первый загрузочный запрос QueryLoadItems и отрисуется
        }

    }
}
