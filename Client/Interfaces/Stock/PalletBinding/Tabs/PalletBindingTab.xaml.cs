using Client.Common;
using Client.Assets.HighLighters;
using Client.Interfaces.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using Client.Interfaces.Accounts;
using Client.Interfaces.Preproduction.Rig;
using static Client.Interfaces.Main.DataGridHelperColumn;
using System.ComponentModel;
using Newtonsoft.Json;
using Client.Interfaces.Stock.PalletBinding.Frames;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Изделия на складе
    /// <author>kurasovdp</author>
    /// </summary>
    public partial class PalletBindingTab : ControlBase
    {
        public PalletBindingTab()
        {
            InitializeComponent();

            //ControlSection = "palletbinding";
            RoleName = "[erp]pallet_binding";  // создать новую роль, прописать в навигаторе (создали)
            ControlTitle = "Изделия на складе";
            DocumentationUrl = "/doc/l-pack-erp/warehouse";

            // обработка сообщений между компонентами
            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            // Обработка нажатий клавиш
            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };

            // Инициализация при загрузке
            OnLoad = () =>
            {
                FormInit();            //инициализация формы
                PalletGridInit();     //инициализация таблицы аккаунтов
                SetDefaults();         //установка значений по умолчанию
            };

            // Очистка ресурсов при выгрузке
            OnUnload = () =>
            {
                PalletGrid.Destruct();
            };

            // Управление автообновлением таблицы при фокусе
            // Активная вкладка 
            OnFocusGot = () =>
            {
                PalletGrid.ItemsAutoUpdate = true;
                PalletGrid.Run();
            };

            // Управление автообновлением таблицы при фокусе
            // Переход на другую вкладку
            OnFocusLost = () =>
            {
                PalletGrid.ItemsAutoUpdate = false;
            };

            ///<summary>
            /// Система команд (Commander)
            /// Код реализует систему команд с группировкой и контекстной активацией.
            ///</summary>
            {

                // Группа "main"
                Commander.SetCurrentGroup("main");    
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "refresh", 
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "RefreshButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            PalletGrid.LoadItems();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "help",
                        Enabled = true,
                        Title = "Справка",
                        Description = "Показать справочную информацию",
                        ButtonUse = true,
                        ButtonName = "HelpButton",
                        HotKey = "F1",
                        Action = () =>
                        {
                            Central.ShowHelp(DocumentationUrl);
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "print",
                        Title = "Печать",
                        AccessLevel = Common.Role.AccessMode.ReadOnly,
                        Group = "print",
                        MenuUse = false,
                        ButtonUse = true,
                        ButtonName = "PrintButton",
                        Action = () =>
                        {
                           // дописать
                        },
                        CheckEnabled = () =>
                        {
                            var result = true;
                            return result;
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "export_to_excel",
                        Group = "main",
                        Enabled = true,
                        Title = "В Excel",
                        Description = "Выгрузить данные в Excel файл",
                        ButtonUse = true,
                        ButtonName = "ExcelButton",
                        //ButtonControl = ExcelButton,  - проверка на этапе компиляции
                        AccessLevel = Common.Role.AccessMode.ReadOnly,
                        Action = () =>
                        {
                            PalletGrid.ItemsExportExcel();
                        },
                    });
                }

                // Команды для изделия на складе (PalletBindingGrid)
                Commander.SetCurrentGridName("PalletBindingGrid");
                {
                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "tie",
                            Enabled = true,
                            Title = "Привязать",
                            Description = "Привязать поддон",
                            ButtonUse = true,
                            ButtonName = "TieButton",
                            Action = () =>
                            {
                                var i = new PalletBindingForm();
                                i.Show();
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = PalletGrid.GetPrimaryKey();
                                var row = PalletGrid.SelectedItem;
                                if (row.CheckGet(k).ToInt() != 0)
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "Untie",
                            Enabled = true,
                            Title = "Отвязать",
                            Description = "Отвязать поддон",
                            ButtonUse = true,
                            ButtonName = "UntieButton",
                            Action = () =>
                            {
                                Central.ShowHelp(DocumentationUrl); // стандартное диалоговое окно (поискать вызов функции ShowDialog)
                            },
                        });
                    }
                }
            }
            Commander.Init(this);
        }

        public FormHelper Form {  get; set; }

        public void SetDefaults()
        {
            Form.SetDefaults();
        }

        /// <summary>
        /// Настраивает Grid для отображения списка изделий на складе.
        /// Отображает колонки, оформление строк(цвет)
        /// </summary>
        private void PalletGridInit()
        {
            // Определение колонок
            // Каждый объект описывает одну колонку таблицы
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#", //Видит пользователь
                    Path="CHECKING",   // SQL - запрос
                    ColumnType=ColumnTypeRef.Boolean, //тип данных колонки
                    Width2=4, //ширина символов (число)
                },
                new DataGridHelperColumn
                {
                    Header="ИД Поддона",
                    Path="PALLET_ID", 
                    Description = "ИД поддона",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="PRODUCT_CODE",  // SQL - запрос
                    Description="Артикул Поддона",
                    ColumnType=ColumnTypeRef.String,
                    Width2=14,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="NAMES",
                    Description="Наименование",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="KOL",
                    Description="",
                    ColumnType=ColumnTypeRef.Double,
                    Format = "N0",
                    Width2=16,
                },
                 new DataGridHelperColumn
                {
                    Header="Поддон",
                    Path="PZ_NUM",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                 new DataGridHelperColumn
                {
                    Header="Место",
                    Path="PLACE",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                 //Заявка
                 new DataGridHelperColumn
                {
                    Header="Отгрузка",
                    Path="ORDER_DATA",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                    Group = "Заявка"
                },
                 new DataGridHelperColumn
                {
                    Header="ИД позиции заявки",
                    Path="IDORDERDATES",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                    Group = "Заявка"
                },
                 new DataGridHelperColumn
                {
                    Header="ИД отгрузки",
                    Path="IDTS",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                    Group = "Заявка"
                },
                 // ПЗ
                 new DataGridHelperColumn
                {
                    Header="Отгрузка",
                    Path="ORDER_DATA_PZ",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                    Group = "ПЗ"
                },
                 new DataGridHelperColumn
                {
                    Header="ИД позиции заявки",
                    Path="IDORDERDATES_PZ",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                    Group = "ПЗ"
                },
                 new DataGridHelperColumn
                {
                    Header="OD_C",
                    Path="OD_C",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                 new DataGridHelperColumn
                {
                    Header="SHIPPED",
                    Path="SHIPPED",
                    Description="Отправленный поддон",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                 new DataGridHelperColumn
                {
                    Header="ID_PZ",
                    Path="ID_PZ",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                 new DataGridHelperColumn
                {
                    Header="NUM",
                    Path="NUM",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                 new DataGridHelperColumn
                {
                    Header="ИД товара",
                    Path="ID_TOVAR",
                    Description="ИД товара",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                 new DataGridHelperColumn
                {
                    Header="FACT_ID",
                    Path="FACT_ID",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                 new DataGridHelperColumn
                {
                    Header="DTTM",
                    Path="DTTM",
                    Description="Указание даты и времени",
                    ColumnType=ColumnTypeRef.DateTime,
                    Format = "dd.MM.yyyy HH:mm:ss",
                    Width2=6,
                },
            };

            // Ещё не менял
            ///<summary>
            /// Стилизаия строк по правилам
            /// RowStylers — это словарь, где ключ описывает тип стилизации (например, BackgroundColor), 
            /// а значение — делегат (функция), которая для каждой строки возвращает значение стиля (например, цвет фона).
            ///</summary>
            //PalletGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
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
            PalletGrid.SetColumns(columns); //сообщает таблице какие колонки показывать
            PalletGrid.SetPrimaryKey("PALLET_ID"); //указывает, что поле ID является уникальным ключом для каждой строки
            PalletGrid.SetSorting("PALLET_ID", ListSortDirection.Ascending); //начальная сортировка по данной колонке по-умолчанию по возрастанию
            PalletGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact; //режим отображения ширины колонок
            PalletGrid.SearchText = PalletGridSearch;  //поле строки поиска
            PalletGrid.Toolbar = PalletGridToolbar; // привязка панели инструментов отвечающую за кнопки/действия таблицы

            ///<summary>
            /// Как загружать данные (запрос)
            ///</summary>
            PalletGrid.QueryLoadItems = new RequestData() //описывает как сетка должна запрашивать данные с сервера, структура с параметрами:
            {
                Module = "Stock", 
                Object = "PalletBinding",
                Action = "List",
                AnswerSectionKey = "PALLETS",
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                            {
                                //{ "FACTORY_ID", StatusSelectBox.SelectedItem.Key},
                            };
                },
            };

            ///<summary>
            /// Фильтрация элементов — OnFilterItems
            ///</summary>
            // Нужна ли эта часть?, сложно понять что она делает
            PalletGrid.OnFilterItems = () =>
            {
                if (PalletGrid.Items.Count > 0)
                {
                    var v = Form.GetValues();
                    var accountType = v.CheckGet("ACCOUNT_TYPE").ToInt();

                    var items = new List<Dictionary<string, string>>();
                    foreach (Dictionary<string, string> row in PalletGrid.Items)
                    {
                        bool include = false;

                        switch (accountType)
                        {
                            //Общие аккаунты
                            case 1:
                                {
                                    if (row.CheckGet("IS_EMPLOYEE").ToInt() == 0) // проверяем что выбрано
                                    {
                                        include = true;
                                    }
                                }
                                break;

                            //Аккаунты пользователей
                            case 2:
                                {
                                    if (row.CheckGet("IS_EMPLOYEE").ToInt() == 1)
                                    {
                                        include = true;
                                    }
                                }
                                break;

                            //Уволенные пользователи
                            case 3:
                                {
                                    if (row.CheckGet("LOCKED_FLAG").ToInt() == 1)
                                    {
                                        include = true;
                                    }
                                }
                                break;

                            //Все
                            case 0:
                            default:
                                {
                                    include = true;
                                }
                                break;
                        }

                        if (include)
                        {
                            items.Add(row);
                        }

                    }
                    PalletGrid.Items = items;
                }
            };

            ///<summary>
            /// Команды и инициализация
            ///</summary>
            PalletGrid.Commands = Commander; //Привязка набора команд(кнопок/действий), которые будут доступны в таблице(CRUD)
            PalletGrid.Init();   //финальная инициализация: таблица применит все настройки, возможно выполнит первый загрузочный запрос QueryLoadItems и отрисуется
        }

        /// <summary>
        /// Создание и настройка формы(объект FormHelper), в которой задаются поля
        /// </summary>
        private void FormInit()
        {
            Form = new FormHelper(); // контейнер, который будет управлять значениями полей, их логикой и связью с интерфейсом.
            var fields = new List<FormHelperField>()
            {
                new FormHelperField() // Каждый FormHelperField — это одно поле формы.
                {
                    Path="PALLET_GROUPS", //Имя поля, форма будет хранить знаечение под ключом "можно придумать свой"
                    FieldType=FormHelperField.FieldTypeRef.Integer, //Значение (целое число)
                    Default="0", //Если пользователь ничего не выбрал — используется "0"
                    Control=PalletGroup, //Это SelectBox (выпадающий список), размещённый в UI.
                    ControlType="SelectBox",                                                      //Сдлеать для площадки
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange=(FormHelperField f, string v)=>
                    {
                        PalletGrid.UpdateItems();  
                    },

                    //QueryLoadItems = new RequestData() // Загрузка вариантов выбора с сервера
                    //{
                    //    Module = "Stock", 
                    //    Object = "PalletBinding",
                    //    Action = "List",
                    //    AnswerSectionKey = "PALLETS", 
                    //    OnComplete = (FormHelperField f,ListDataSet ds) => // Что делать, когда данные пришли
                    //    {
                    //        var row = new Dictionary<string, string>() // Добавляем пункт “Все”, т е перед реальными группами всегда есть пункт «Все».
                    //        {
                    //            {"ID", "0" },
                    //            {"NAME", "Все" },
                    //        };
                    //        ds.ItemsPrepend(row);
                    //        var list=ds.GetItemsList("ID","NAME"); // Преобразование ListDataSet в словарь для SelectBox
                    //        var c=(SelectBox)f.Control; // передача данных в UI-контрол
                    //        if(c != null)
                    //        {
                    //            c.Items=list;
                    //        }
                    //    },
                    //},
                },
          
            };
            // Регистрация полей
            Form.SetFields(fields);
        }
    }
}
