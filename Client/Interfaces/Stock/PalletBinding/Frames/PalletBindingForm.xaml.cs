using Client.Assets.HighLighters;
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
                        Save();
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
        }

        public int PalletId;

        private int FactoryId; // для передачи информации

        private int ProductId;

        private int IdPz;

        private int Num;

        private int IdOrderDates;

        /// <summary>
        /// Метод для заполнения
        /// </summary>
        public void SetParams(int factoryId, int productId, int id_pz, int num, int id_order_dates)
        {
            this.FactoryId = factoryId;
            this.ProductId = productId;
            this.IdPz = id_pz;
            this.Num = num;
            this.IdOrderDates = id_order_dates;
        }

        /// <summary>
        /// Сохранение перед отправкой на сервер
        /// </summary>
        public void Save()
        {
            if( ShipmentGrid.SelectedItem != null && ShipmentGrid.SelectedItem.Count > 0)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("ORDER_ID", ShipmentGrid.SelectedItem["IDORDERDATES"]);
                    p.Add("PRODUCT_ID", ProductId.ToString());
                    p.Add("PRODUCTION_TASK_ID", IdPz.ToString());
                    p.Add("IDORDERDATES", IdOrderDates.ToString());
                    p.Add("PALLET_NUMBER", Num.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "PalletBinding");
                q.Request.SetParam("Action", "Save");
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    Close();
                }
                else
                {
                    q.ProcessError();
                }
            }
            else 
            {
                var d = new DialogWindow("Не выбрана заявка для привязки", "Ошибка", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }     

        private void ShipmentGridInit()
        {
            ///<summary>
            /// Определение колонок
            ///</summary>
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

            ///<summary>
            /// покраска в нужный цвет ячеек
            ///</summary>
            ShipmentGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        if (row != null && row.Count > 0)
                        {
                            if (!row["IDORDERDATES"].IsNullOrEmpty() && (row["IDORDERDATES"] == IdOrderDates.ToString()))
                            {
                            color = HColor.Green;
                            }

                        
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };

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
                                { "FACT_ID", FactoryId.ToString()},  
                                { "PRODUCT_ID", ProductId.ToString()},
                                { "IDORDERDATES", IdOrderDates.ToString()}
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
