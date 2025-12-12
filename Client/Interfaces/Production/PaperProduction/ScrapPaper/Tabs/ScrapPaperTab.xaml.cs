using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Stock;
using Microsoft.Office.Interop.Excel;
using Newtonsoft.Json;
using NPOI.OpenXmlFormats.Spreadsheet;
using NPOI.SS.Formula.Eval;
using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Crypto;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.PaperProduction
{
    /// <summary>
    /// Прием машин с макулатурой на БДМ
    /// <author>Greshnyh_ni</author>
    /// <version>1</version>
    /// <released>2025-09-18</released>
    /// <changed>2025-11-11</changed>
    /// </summary>
    public partial class ScrapPaperTab : ControlBase
    {

        /// <summary>
        ///  ИД площадки 1 - БДМ1(716), 2 - БДМ2(1716) , 3 - ЛТ(2716)  
        /// </summary>
        public int IdSt { get; set; }

        /// <summary>
        /// Место работы программы
        /// </summary>
        private string CurrentPlaceName;

        public FormHelper Form { get; set; }

        /// <summary>
        ///  список машин на разгрузку
        /// </summary>
        private ListDataSet ScrapTransportWeightDataSet { get; set; }

        /// <summary>
        ///  список терминалов
        /// </summary>
        private ListDataSet ScrpTerminalDataSet { get; set; }

        /// <summary>
        ///  список в работе
        /// </summary>
        private ListDataSet ScrapCurrentDataSet { get; set; }

        /// <summary>
        ///  список задания на макулатуру
        /// </summary>
        private ListDataSet ScrapPzGridDataSet { get; set; }

        /// <summary>
        ///  История композиций
        /// </summary>
        private ListDataSet ScrapPzLogGridDataSet { get; set; }

        /// <summary>
        /// данные из выбранной в гриде  строки
        /// </summary>
        Dictionary<string, string> ScrapTransportWeightSelectedItem { get; set; }
        Dictionary<string, string> ScrpTerminalSelectedItem { get; set; }
        Dictionary<string, string> ScrapCurrentSelectedItem { get; set; }
        Dictionary<string, string> ScrapPzSelectedItem { get; set; }
        Dictionary<string, string> ScrapPzLogSelectedItem { get; set; }

        private int ScrapPzSelected = 0;

        /// Таймеры
        /// 
        private Timeout ScrapTransportRefreshButtonTimeout { get; set; }
        private Timeout ScrapPzRefreshButtonTimeout { get; set; }


        /// <summary>
        ///  время обновления гридов (сек)
        /// </summary>
        private int ScrapTransportRefreshTime { get; set; }
        private int ScrapPzRefreshTime { get; set; }

        /// <summary>
        ////количество секунд до обновления информации
        /// </summary>
        private int ScrapTransportCurSecund { get; set; }
        private int ScrapPzCurSecund { get; set; }

        /// Таймер периодического обновления каждую  секунду
        /// </summary>
        private DispatcherTimer FastTimer { get; set; }

        private bool RunFirst = true;
        public bool ReadOnlyFlag { get; set; }

        /// <summary>
        /// Количество кип с ПЭС
        /// </summary>
        private int KolBalePolyal { get; set; }

        /// <summary>
        /// контроль поставляемой категории макулатуры поставщиком в соответствии с договором.
        /// </summary>
        private bool ControlPostavshicFlag { get; set; }
        /// <summary>
        /// Номер Com порта на автом. весовой БДМ1
        /// </summary>
        private string Bdm1ComPort { get; set; }
        /// <summary>
        /// Номер Com порта на автом. весовой БДМ2
        /// </summary>
        private string Bdm2ComPort { get; set; }
        /// <summary>
        /// Ip адрес Laurent платы на весовой БДМ1
        /// </summary>
        private string Bdm1LaurentIp { get; set; }
        /// <summary>
        /// Ip адреса Laurent платы на весовой БДМ2
        /// </summary>
        private string Bdm2LaurentIp { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public ScrapPaperTab()
        {
            ControlTitle = "Прием макулатуры";
            DocumentationUrl = "/doc/l-pack-erp-new/lt/scrap_paper_bdm";

            InitializeComponent();

            Form = null;

            //регистрация обработчика сообщений
            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    ProcessMessage(m);
                    //  Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (KeyEventArgs e) =>
            {

                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                if (IdSt == 716)
                {
                    CurrentPlaceName = "Локация БДМ1";
                    CurrentMacine.Text = "БДМ1";
                    RoleName = "[erp]scrap_paper_bdm1";
                }
                else if (IdSt == 1716)
                {
                    CurrentPlaceName = "Локация БДМ2";
                    CurrentMacine.Text = "БДМ2";
                    RoleName = "[erp]scrap_paper_bdm2";
                }
                else if (IdSt == 2716)
                {
                    CurrentPlaceName = "Локация ЛТ";
                    CurrentMacine.Text = "ЛТ";
                    WastePaperCheckBox.IsChecked = false;
                    RoleName = "[erp]scrap_paper_molded_contner";
                }

                FormInit();
                SetDefaults();
                ProcessPermissions();

                // описание кнопок и контекстного меню
                ButtonEnable();

                // 1. машины на весовой
                ScrapTransportWeightGridInit();
                // 2. список машин на терминалах
                ScrpTerminalGridInit();
                // 3. В работе
                ScrapCurrentGridInit();

                if (IdSt != 2716)
                {
                    // 4. Задания на макаулатуру
                    ScrapPzGridInit();
                    // 5. История изменения композиции
                    ScrapPzLogGridInit();
                }

                // 6. список удаленных машины на весовой
                ScrapTransportDeleteWeightGridInit();

                // 7. Настройки программы
                GetSetupData();

                SetFastTimer(1);
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                FastTimer?.Stop();

                ScrapTransportWeightGrid.Destruct();
                ScrpTerminalGrid.Destruct();
                ScrapCurrentGrid.Destruct();
                ScrapPzGrid.Destruct();
                ScrapPzLogGrid.Destruct();
                ScrapTransportDeleteWeightGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                ScrapTransportWeightGrid.ItemsAutoUpdate = true;
                ScrapTransportWeightGrid.Run();

                ScrpTerminalGrid.ItemsAutoUpdate = true;
                ScrpTerminalGrid.Run();

                ScrapCurrentGrid.ItemsAutoUpdate = true;
                ScrapCurrentGrid.Run();

                ScrapPzGrid.ItemsAutoUpdate = true;
                ScrapPzGrid.Run();

                ScrapPzLogGrid.ItemsAutoUpdate = true;
                ScrapPzLogGrid.Run();

            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                ScrapTransportWeightGrid.ItemsAutoUpdate = false;
                ScrpTerminalGrid.ItemsAutoUpdate = false;
                ScrapCurrentGrid.ItemsAutoUpdate = false;
                ScrapPzGrid.ItemsAutoUpdate = false;
                ScrapPzLogGrid.ItemsAutoUpdate = false;
                ScrapTransportDeleteWeightGrid.ItemsAutoUpdate = false;
            };

        }

        /// <summary>
        /// доступность кнопок и контекстное меню
        /// </summary>
        private void ButtonEnable()
        {
            Commander.SetCurrentGridName("ScrapTransportWeightGrid");
            {
                // кнопки
                Commander.Add(new CommandItem()
                {
                    Name = "scrap_transport_atrr_edit",
                    Title = "Описание машины",
                    Description = "Описание машины",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = false,
                    ButtonUse = true,
                    ButtonControl = ScrapTransportAtrrEditButton,
                    ButtonName = "ScrapTransportAtrrEditButton",
                    //  AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = false,
                    Action = () =>
                    {
                        ProcessCommand("scrap_transport_atrr_edit");
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                if (ScrapTransportWeightGrid != null
                                && ScrapTransportWeightGrid.SelectedItem != null
                                && ScrapTransportWeightGrid.SelectedItem.Count > 0)
                                {
                                    // показать удаленные машины
                                    if (ScrapTransportAudDeleteCheckBox.IsChecked == true)
                                    {
                                        result = false;
                                    }
                                    else
                                    // ПЭС
                                    if ((ScrapTransportWeightGrid.SelectedItem.CheckGet("ID_STATUS").ToInt() >= 11)
                                    && (ScrapTransportWeightGrid.SelectedItem.CheckGet("ID_STATUS").ToInt() <= 25))
                                    {
                                        result = false;
                                    }
                                    else
                                    // Химия
                                    if ((ScrapTransportWeightGrid.SelectedItem.CheckGet("ID_STATUS").ToInt() >= 41)
                                    && (ScrapTransportWeightGrid.SelectedItem.CheckGet("ID_STATUS").ToInt() <= 45))
                                    {
                                        result = false;
                                    }
                                    else
                                    // Машина не приехала
                                    if (ScrapTransportWeightGrid.SelectedItem.CheckGet("ID_STATUS").ToInt() == 66)
                                    {
                                        result = false;
                                    }
                                    else
                                        result = true;
                                }
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "add_terminal",
                    Title = "Поставить на терминал",
                    Description = "Прикрепить машину к терминалу",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = false,
                    ButtonUse = true,
                    ButtonControl = AddTerminalButton,
                    ButtonName = "AddTerminalButton",
                    //  AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = false,
                    Action = () =>
                    {
                        //  AddTerminal();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                if (ScrapTransportWeightGrid != null
                                && ScrapTransportWeightGrid.SelectedItem != null
                                && ScrapTransportWeightGrid.SelectedItem.Count > 0)
                                {
                                    var status = ScrapTransportWeightGrid.SelectedItem.CheckGet("ID_STATUS").ToInt();

                                    // показать удаленные машины
                                    if (ScrapTransportAudDeleteCheckBox.IsChecked == true)
                                    {
                                        result = false;
                                    }
                                    else
                                    {
                                        switch (status)
                                        {
                                            case 1:
                                                result = true;
                                                break;
                                            case 2:
                                                result = true;
                                                break;
                                            case 11:
                                                result = true;
                                                break;
                                            case 13:
                                                result = true;
                                                break;
                                            case 41:
                                                result = true;
                                                break;
                                            case 42:
                                                result = true;
                                                break;
                                            default:
                                                result = false;
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "scrap_move_to_bdm1",
                    Title = "Отправить на БДМ1",
                    Description = "Отправить машину на БДМ1",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = false,
                    ButtonUse = IdSt == 1716 ? true : false,
                    ButtonControl = ScrapMoveToBdm1Button,
                    ButtonName = "ScrapMoveToBdm1Button",
                    //  AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = false,
                    Action = () =>
                    {

                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                if (ScrapTransportWeightGrid != null
                                && ScrapTransportWeightGrid.SelectedItem != null
                                && ScrapTransportWeightGrid.SelectedItem.Count > 0)
                                {
                                    var status = ScrapTransportWeightGrid.SelectedItem.CheckGet("ID_STATUS").ToInt();

                                    // показать удаленные машины
                                    if (ScrapTransportAudDeleteCheckBox.IsChecked == true)
                                    {
                                        result = false;
                                    }
                                    else
                                    {
                                        switch (status)
                                        {
                                            case 1:
                                                result = true;
                                                break;
                                            case 2:
                                                result = true;
                                                break;
                                            default:
                                                result = false;
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "move_to_cast_container",
                    Title = "Отправить на ЦЛТ",
                    Description = "Отправить машину на ЛТ",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = false,
                    ButtonUse = IdSt == 1716 ? true : false,
                    ButtonControl = MoveToCastContainerButton,
                    ButtonName = "MoveToCastContainerButton",
                    //  AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = false,
                    Action = () =>
                    {

                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                if (ScrapTransportWeightGrid != null
                                && ScrapTransportWeightGrid.SelectedItem != null
                                && ScrapTransportWeightGrid.SelectedItem.Count > 0)
                                {
                                    var status = ScrapTransportWeightGrid.SelectedItem.CheckGet("ID_STATUS").ToInt();

                                    // показать удаленные машины
                                    if (ScrapTransportAudDeleteCheckBox.IsChecked == true)
                                    {
                                        result = false;
                                    }
                                    else
                                    {
                                        switch (status)
                                        {
                                            case 1:
                                                result = true;
                                                break;
                                            case 2:
                                                result = true;
                                                break;
                                            default:
                                                result = false;
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "scrap_transport_scan_barcode",
                    Title = "Сканировать ШК",
                    Description = "Сканировать ШК на БДМ1",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = false,
                    ButtonUse = IdSt == 716 ? true : false,
                    ButtonControl = ScrapTransportScanBarcodeButton,
                    ButtonName = "ScrapTransportScanBarcodeButton",
                    //  AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = false,
                    Action = () =>
                    {

                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                if (ScrapTransportWeightGrid != null
                                && ScrapTransportWeightGrid.SelectedItem != null
                                && ScrapTransportWeightGrid.SelectedItem.Count > 0)
                                {
                                    var status = ScrapTransportWeightGrid.SelectedItem.CheckGet("ID_STATUS").ToInt();

                                    // показать удаленные машины
                                    if (ScrapTransportAudDeleteCheckBox.IsChecked == true)
                                    {
                                        result = false;
                                    }
                                    else
                                    {
                                        switch (status)
                                        {
                                            case 1:
                                                result = true;
                                                break;
                                            case 2:
                                                result = true;
                                                break;
                                            case 3:
                                                result = true;
                                                break;
                                            case 4:
                                                result = true;
                                                break;
                                            default:
                                                result = false;
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "scrap_transport_popup_menu",
                    Title = "Добавить",
                    Description = "Открыть вкладку добавления машины",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = false,
                    ButtonUse = IdSt == 2716 ? false : true,
                    ButtonControl = ScrapTransportPopupMenuButton,
                    ButtonName = "ScrapTransportPopupMenuButton",
                    //  AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = false,
                    Action = () =>
                    {
                        ProcessCommand("scrap_transport_popup_menu");
                    },
                    CheckEnabled = () =>
                    {
                        bool result = true;
                        {
                            if (ReadOnlyFlag == true)
                                result = false;
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "scrap_transport_edit",
                    Title = "Изменить",
                    Description = "Открыть вкладку редактирования машины",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = false,
                    ButtonUse = true,
                    ButtonControl = ScrapTransportEditButton,
                    ButtonName = "ScrapTransportEditButton",
                    //  AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = false,
                    Action = () =>
                    {
                        ProcessCommand("scrap_transport_edit");
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                if (ScrapTransportWeightGrid != null
                                && ScrapTransportWeightGrid.SelectedItem != null
                                && ScrapTransportWeightGrid.SelectedItem.Count > 0)
                                {
                                    var status = ScrapTransportWeightGrid.SelectedItem.CheckGet("ID_STATUS").ToInt();

                                    // показать удаленные машины
                                    if (ScrapTransportAudDeleteCheckBox.IsChecked == true)
                                    {
                                        result = false;
                                    }
                                    else
                                    // Химия
                                    if ((ScrapTransportWeightGrid.SelectedItem.CheckGet("ID_STATUS").ToInt() >= 41)
                                    && (ScrapTransportWeightGrid.SelectedItem.CheckGet("ID_STATUS").ToInt() <= 45))
                                    {
                                        result = false;
                                    }
                                    else
                                    // Машина не приехала
                                    if (ScrapTransportWeightGrid.SelectedItem.CheckGet("ID_STATUS").ToInt() == 66)
                                    {
                                        result = false;
                                    }
                                    else
                                        result = true;
                                }
                                else
                                    result = false;
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "scrap_transport_file",
                    Title = "Файлы",
                    Description = "Открыть вкладку просмотра списка файлов",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = false,
                    ButtonUse = true,
                    ButtonControl = ScrapTransportFileButton,
                    ButtonName = "ScrapTransportFileButton",
                    //  AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = false,
                    Action = () =>
                    {

                    },
                    CheckEnabled = () =>
                    {
                        bool result = true;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                if (ScrapTransportWeightGrid != null
                                && ScrapTransportWeightGrid.SelectedItem != null
                                && ScrapTransportWeightGrid.SelectedItem.Count > 0)
                                {
                                    var status = ScrapTransportWeightGrid.SelectedItem.CheckGet("ID_STATUS").ToInt();

                                    // показать удаленные машины
                                    if (ScrapTransportAudDeleteCheckBox.IsChecked == true)
                                    {
                                        result = false;
                                    }
                                    else
                                    // Химия
                                    if ((ScrapTransportWeightGrid.SelectedItem.CheckGet("ID_STATUS").ToInt() >= 41)
                                    && (ScrapTransportWeightGrid.SelectedItem.CheckGet("ID_STATUS").ToInt() <= 45))
                                    {
                                        result = false;
                                    }
                                    else
                                    // Машина не приехала
                                    if (ScrapTransportWeightGrid.SelectedItem.CheckGet("ID_STATUS").ToInt() == 66)
                                    {
                                        result = false;
                                    }
                                }
                                else
                                    result = false;
                            }
                            else
                                result = false;
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "scrap_bale_map_bdm1",
                    Title = "Карта склада БДМ1",
                    Description = "Показать карту склада для БДМ1",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = false,
                    ButtonUse = IdSt == 716 ? true : false,
                    ButtonControl = ScrapBaleMapBdm1Button,
                    ButtonName = "ScrapBaleMapBdm1Button",
                    //  AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = true,
                    Action = () =>
                    {

                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "scrap_bale_map_bdm2",
                    Title = "Карта склада БДМ2",
                    Description = "Показать карту склада для БДМ2",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = false,
                    ButtonUse = IdSt == 1716 ? true : false,
                    ButtonControl = ButtonScrapBaleMapButton,
                    ButtonName = "ButtonScrapBaleMapButton",
                    //  AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = true,
                    Action = () =>
                    {

                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "cast_container_map",
                    Title = "Карта склада ЛТ",
                    Description = "Показать карту склада литой тары",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = false,
                    ButtonUse = IdSt == 2716 ? true : false,
                    ButtonControl = CastContainerMapButton,
                    ButtonName = "CastContainerMapButton",
                    //  AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = true,
                    Action = () =>
                    {

                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "scrap_bale_info",
                    Title = "Информация по ячейкам",
                    Description = "Показать информацию по заполнению рядов",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = false,
                    ButtonUse = true,
                    ButtonControl = ScrapBaleInfoButton,
                    ButtonName = "ScrapBaleInfoButton",
                    //  AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = true,
                    Action = () =>
                    {

                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "Scrap_Bale_all_info",
                    Title = "Заполнение складов",
                    Description = "Показать информацию по заполнению складов на БДМ1 и БДМ2",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = false,
                    ButtonUse = IdSt == 2716 ? false : true,
                    ButtonControl = ScrapBaleBdm2Button,
                    ButtonName = "ScrapBaleBdm2Button",
                    //  AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = true,
                    Action = () =>
                    {

                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "setup",
                    Title = "Настройка",
                    Description = "Настройка параметров программы",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = false,
                    ButtonUse = IdSt == 1716 ? true : false,
                    ButtonControl = SetupButton,
                    ButtonName = "SetupButton",
                    //  AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = true,
                    Action = () =>
                    {
                        ProcessCommand("setup");
                    },
                });

                // пункты меню
                Commander.Add(new CommandItem()
                {
                    Name = "info_scrap",
                    MenuTitle = "Информация",
                    Description = "Открыть окно с информацией по машине",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = true,
                    Enabled = false,
                    Action = () =>
                    {
                        ProcessCommand("info_scrap");
                    },
                    CheckEnabled = () =>
                    {
                        bool result = true;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                if (ScrapTransportWeightGrid != null
                                && ScrapTransportWeightGrid.SelectedItem != null
                                && ScrapTransportWeightGrid.SelectedItem.Count > 0)
                                {
                                    // показать удаленные машины
                                    if (ScrapTransportAudDeleteCheckBox.IsChecked == true)
                                    {
                                        result = false;
                                    }
                                }
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "separator1",
                    MenuTitle = "-",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = true,
                    Enabled = false,
                });
                Commander.Add(new CommandItem()
                {
                    Name = "scrap_transport_delete",
                    MenuTitle = "Удалить машину (не оприходованную/не загруженную)",
                    Description = "Машина будет удалена из списка зарегистрированных",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = true,
                    Enabled = false,
                    Action = () =>
                    {
                        ProcessCommand("scrap_transport_delete");
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                if (ScrapTransportWeightGrid != null
                                && ScrapTransportWeightGrid.SelectedItem != null
                                && ScrapTransportWeightGrid.SelectedItem.Count > 0)
                                {
                                    var status = ScrapTransportWeightGrid.SelectedItem.CheckGet("ID_STATUS").ToInt();

                                    // показать удаленные машины
                                    if (ScrapTransportAudDeleteCheckBox.IsChecked == true)
                                    {
                                        result = false;
                                    }
                                    else
                                    {
                                        //   if ((Central.User.Login == "fedyanina_ev") || (Central.User.Login == "greshnyh_ni"))
                                        {
                                            switch (status)
                                            {
                                                case 1:
                                                    result = true;
                                                    break;
                                                case 2:
                                                    result = true;
                                                    break;
                                                case 3:
                                                    result = true;
                                                    break;
                                                case 4:
                                                    result = true;
                                                    break;
                                                case 6:
                                                    result = true;
                                                    break;
                                                case 11:
                                                    result = true;
                                                    break;
                                                case 13:
                                                    result = true;
                                                    break;
                                                case 41:
                                                    result = true;
                                                    break;
                                                case 42:
                                                    result = true;
                                                    break;
                                                default:
                                                    result = false;
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "scrap_transport_prihod_clear",
                    MenuTitle = "Удалить приход машины (заново провести разгрузку)",
                    Description = "Будет проведен откат машины до первоначального состояния \"Взвешена полная машина\"",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = true,
                    Enabled = false,
                    Action = () =>
                    {
                        ProcessCommand("scrap_transport_prihod_clear");
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                if (ScrapTransportWeightGrid != null
                                && ScrapTransportWeightGrid.SelectedItem != null
                                && ScrapTransportWeightGrid.SelectedItem.Count > 0)
                                {
                                    var status = ScrapTransportWeightGrid.SelectedItem.CheckGet("ID_STATUS").ToInt();

                                    // показать удаленные машины
                                    if (ScrapTransportAudDeleteCheckBox.IsChecked == true)
                                    {
                                        result = false;
                                    }
                                    else
                                    {
                                        if ((Central.User.Login == "fedyanina_ev") || (Central.User.Login == "greshnyh_ni"))
                                        {
                                            switch (status)
                                            {
                                                case 5:
                                                    result = true;
                                                    break;
                                                case 29:
                                                    result = true;
                                                    break;
                                                case 30:
                                                    result = true;
                                                    break;
                                                default:
                                                    result = false;
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "scrap_transport_prihod_provedeno",
                    MenuTitle = "Проведение прихода машины",
                    Description = "Будет проведен заново приход машины с пересчетом веса кип",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = true,
                    Enabled = false,
                    Action = () =>
                    {
                        ProcessCommand("scrap_transport_prihod_provedeno");
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                if (ScrapTransportWeightGrid != null
                                && ScrapTransportWeightGrid.SelectedItem != null
                                && ScrapTransportWeightGrid.SelectedItem.Count > 0)
                                {
                                    var status = ScrapTransportWeightGrid.SelectedItem.CheckGet("ID_STATUS").ToInt();

                                    // показать удаленные машины
                                    if (ScrapTransportAudDeleteCheckBox.IsChecked == true)
                                    {
                                        result = false;
                                    }
                                    else
                                    {
                                        if ((Central.User.Login == "fedyanina_ev") || (Central.User.Login == "greshnyh_ni"))
                                        {
                                            switch (status)
                                            {
                                                case 5:
                                                    result = true;
                                                    break;
                                                case 29:
                                                    result = true;
                                                    break;
                                                case 30:
                                                    result = true;
                                                    break;
                                                default:
                                                    result = false;
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "scrap_transport_akt_change",
                    MenuTitle = "Исправить номер Акта",
                    Description = "Будет изменен № Акта при изменении поставщика",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = true,
                    Enabled = false,
                    Action = () =>
                    {
                        ProcessCommand("scrap_transport_akt_change");
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                if (ScrapTransportWeightGrid != null
                                && ScrapTransportWeightGrid.SelectedItem != null
                                && ScrapTransportWeightGrid.SelectedItem.Count > 0)
                                {
                                    var status = ScrapTransportWeightGrid.SelectedItem.CheckGet("ID_STATUS").ToInt();

                                    // показать удаленные машины
                                    if (ScrapTransportAudDeleteCheckBox.IsChecked == true)
                                    {
                                        result = false;
                                    }
                                    else
                                    {
                                        if ((Central.User.Login == "fedyanina_ev") || (Central.User.Login == "greshnyh_ni"))
                                        {
                                            switch (status)
                                            {
                                                case 4:
                                                    result = true;
                                                    break;
                                                case 5:
                                                    result = true;
                                                    break;
                                                case 29:
                                                    result = true;
                                                    break;
                                                case 30:
                                                    result = true;
                                                    break;
                                                default:
                                                    result = false;
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "separator2",
                    MenuTitle = "-",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = true,
                    Enabled = false,
                });
                Commander.Add(new CommandItem()
                {
                    Name = "scrap_transport_histoty",
                    MenuTitle = "История изменений по машине",
                    Description = "Показ всех операций по машине и разгруженным кипам",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = true,
                    Enabled = false,
                    Action = () =>
                    {
                        ProcessCommand("scrap_transport_histoty");
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                if (ScrapTransportWeightGrid != null
                                && ScrapTransportWeightGrid.SelectedItem != null
                                && ScrapTransportWeightGrid.SelectedItem.Count > 0)
                                {
                                    var status = ScrapTransportWeightGrid.SelectedItem.CheckGet("ID_STATUS").ToInt();

                                    // показать удаленные машины
                                    if (ScrapTransportAudDeleteCheckBox.IsChecked == true)
                                    {
                                        result = false;
                                    }
                                }
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "separator3",
                    MenuTitle = "-",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = true,
                    Enabled = false,
                });
                Commander.Add(new CommandItem()
                {
                    Name = "scrap_transport_set_status_43",
                    MenuTitle = "Химия. Разгрузка начата",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = true,
                    Enabled = false,
                    Action = () =>
                    {
                        ProcessCommand("scrap_transport_set_status_43");
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                if (ScrapTransportWeightGrid != null
                                && ScrapTransportWeightGrid.SelectedItem != null
                                && ScrapTransportWeightGrid.SelectedItem.Count > 0)
                                {
                                    var status = ScrapTransportWeightGrid.SelectedItem.CheckGet("ID_STATUS").ToInt();

                                    // показать удаленные машины
                                    if (ScrapTransportAudDeleteCheckBox.IsChecked == true)
                                    {
                                        result = false;
                                    }
                                    else
                                    {
                                        if (status >= 42 && status < 45)
                                        {
                                            switch (status)
                                            {
                                                case 42:
                                                    result = true;
                                                    break;
                                                default:
                                                    result = false;
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "scrap_transport_set_status_44",
                    MenuTitle = "Химия. Разгрузка закончена",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = true,
                    Enabled = false,
                    Action = () =>
                    {
                        ProcessCommand("scrap_transport_set_status_44");
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                if (ScrapTransportWeightGrid != null
                                && ScrapTransportWeightGrid.SelectedItem != null
                                && ScrapTransportWeightGrid.SelectedItem.Count > 0)
                                {
                                    var status = ScrapTransportWeightGrid.SelectedItem.CheckGet("ID_STATUS").ToInt();

                                    // показать удаленные машины
                                    if (ScrapTransportAudDeleteCheckBox.IsChecked == true)
                                    {
                                        result = false;
                                    }
                                    else
                                    {
                                        if (status >= 42 && status < 45)
                                        {
                                            switch (status)
                                            {
                                                case 43:
                                                    result = true;
                                                    break;
                                                default:
                                                    result = false;
                                                    break;
                                            }

                                        }
                                    }
                                }
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "separator4",
                    MenuTitle = "-",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = true,
                    Enabled = false,
                });
                Commander.Add(new CommandItem()
                {
                    Name = "chckpnt_car_add",
                    MenuTitle = "Направить на осевое взвешивание БДМ1",
                    Description = "Машина с ПЭС будет направлена на весы БДМ1",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = true,
                    Enabled = false,
                    Action = () =>
                    {
                        ProcessCommand("chckpnt_car_add");
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                if (ScrapTransportWeightGrid != null
                                && ScrapTransportWeightGrid.SelectedItem != null
                                && ScrapTransportWeightGrid.SelectedItem.Count > 0)
                                {
                                    var status = ScrapTransportWeightGrid.SelectedItem.CheckGet("ID_STATUS").ToInt();

                                    // показать удаленные машины
                                    if (ScrapTransportAudDeleteCheckBox.IsChecked == true)
                                    {
                                        result = false;
                                    }
                                    else
                                    {
                                        if (IdSt == 1716 && status == 22)
                                        {
                                            result = true;
                                        }
                                    }
                                }
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "scrap_transport_didnt_arrive",
                    MenuTitle = "Установить статус \"Машина не приехала\"",
                    Description = "Будет убрана машина из списка зарегистрированных, без удаления",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = true,
                    Enabled = false,
                    Action = () =>
                    {
                        ProcessCommand("scrap_transport_didnt_arrive");
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                if (ScrapTransportWeightGrid != null
                                && ScrapTransportWeightGrid.SelectedItem != null
                                && ScrapTransportWeightGrid.SelectedItem.Count > 0)
                                {
                                    var status = ScrapTransportWeightGrid.SelectedItem.CheckGet("ID_STATUS").ToInt();

                                    // показать удаленные машины
                                    if (ScrapTransportAudDeleteCheckBox.IsChecked == true)
                                    {
                                        result = false;
                                    }
                                    else
                                    {
                                        if (status == 1)
                                        {
                                            result = true;
                                        }
                                    }
                                }
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "separator5",
                    MenuTitle = "-",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = true,
                    Enabled = false,
                });
                Commander.Add(new CommandItem()
                {
                    Name = "grid_export",
                    MenuTitle = "Выгрузка списка машин в Excel",
                    Description = "Выгрузка текущего списка машин в файл Excel",
                    Group = "scrap_transport_weight_grid_default",
                    MenuUse = true,
                    Enabled = false,
                    Action = () =>
                    {
                        ProcessCommand("grid_export");
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            if (ScrapTransportWeightGrid != null
                            && ScrapTransportWeightGrid.SelectedItem != null
                            && ScrapTransportWeightGrid.SelectedItem.Count > 0)
                            {
                                result = true;
                            }
                        }
                        return result;
                    },
                });
            }

            Commander.SetCurrentGridName("ScrpTerminalGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "set_place",
                    Title = "Назначить ячейку",
                    Description = "Выбрать ячейку для разгрузки/погрузки машины",
                    Group = "scrp_terminal_grid_default",
                    MenuUse = false,
                    ButtonUse = true,
                    ButtonControl = SetPlaceButton,
                    ButtonName = "SetPlaceButton",
                    //  AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = false,
                    Action = () =>
                    {

                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                if (ScrpTerminalGrid != null
                                && ScrpTerminalGrid.SelectedItem != null
                                && ScrpTerminalGrid.SelectedItem.Count > 0)
                                {
                                    var status = ScrpTerminalGrid.SelectedItem.CheckGet("ID_STATUS").ToInt();

                                    switch (status)
                                    {
                                        case 1:
                                            result = true;
                                            break;
                                        case 2:
                                            result = true;
                                            break;
                                        case 11:
                                            result = true;
                                            break;
                                        case 13:
                                            result = true;
                                            break;
                                        case 16:
                                            result = true;
                                            break;
                                        default:
                                            result = false;
                                            break;
                                    }
                                }
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "ClearTerminalButton",
                    Title = "Открепить машину",
                    Description = "Открепить машину от терминала",
                    Group = "scrp_terminal_grid_default",
                    MenuUse = false,
                    ButtonUse = true,
                    ButtonControl = ClearTerminalButton,
                    ButtonName = "ClearTerminalButton",
                    //  AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = false,
                    Action = () =>
                    {

                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                if (ScrpTerminalGrid != null
                                && ScrpTerminalGrid.SelectedItem != null
                                && ScrpTerminalGrid.SelectedItem.Count > 0)
                                {
                                    var status = ScrpTerminalGrid.SelectedItem.CheckGet("ID_STATUS").ToInt();

                                    switch (status)
                                    {
                                        case 1:
                                            result = true;
                                            break;
                                        case 2:
                                            result = true;
                                            break;
                                        case 11:
                                            result = true;
                                            break;
                                        case 13:
                                            result = true;
                                            break;
                                        case 16:
                                            result = true;
                                            break;
                                        default:
                                            result = false;
                                            break;
                                    }
                                }
                            }
                        }
                        return result;
                    },
                });
            }

            Commander.SetCurrentGridName("ScrapCurrentGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "scrap_bale_move",
                    Title = "Переместить кипы",
                    Description = "Переместить кипы внутри одного склада",
                    Group = "scrap_current_grid_default",
                    MenuUse = false,
                    ButtonUse = IdSt == 2716 ? false : true,
                    ButtonControl = ScrapBaleMoveButton,
                    ButtonName = "ScrapBaleMoveButton",
                    //  AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = false,
                    Action = () =>
                    {

                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                if (ScrapCurrentGrid != null
                                && ScrapCurrentGrid.SelectedItem != null
                                && ScrapCurrentGrid.SelectedItem.Count > 0)
                                {
                                    result = true;
                                }
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "roll_move",
                    Title = "Переместить рулоны",
                    Description = "Переместить нелеквидные рулоны с БДМ1 на БДМ2",
                    Group = "scrap_current_grid_default",
                    MenuUse = false,
                    ButtonUse = IdSt == 1716 ? true : false,
                    ButtonControl = RollMoveButton,
                    ButtonName = "RollMoveButton",
                    //  AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = false,
                    Action = () =>
                    {

                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                if (ScrapCurrentGrid != null
                                && ScrapCurrentGrid.SelectedItem != null
                                && ScrapCurrentGrid.SelectedItem.Count > 0)
                                {
                                    result = true;
                                }
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "scrap_bale_into_production",
                    Title = "Списать",
                    Description = "Списать кипы в производство",
                    Group = "scrap_current_grid_default",
                    MenuUse = false,
                    ButtonUse = IdSt == 2716 ? false : true,
                    ButtonControl = ScrapBaleIntoProductionButton,
                    ButtonName = "ScrapBaleIntoProductionButton",
                    //  AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = false,
                    Action = () =>
                    {

                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                if (ScrapCurrentGrid != null
                                && ScrapCurrentGrid.SelectedItem != null
                                && ScrapCurrentGrid.SelectedItem.Count > 0)
                                {
                                    result = true;
                                }
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "scrap_current_delete",
                    Title = "Удалить",
                    Description = "Удалить задание из работы",
                    Group = "scrap_current_grid_default",
                    MenuUse = false,
                    ButtonUse = IdSt == 2716 ? false : true,
                    ButtonControl = ScrapCurrentDeleteButton,
                    ButtonName = "ScrapCurrentDeleteButton",
                    //  AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = false,
                    Action = () =>
                    {

                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                if (ScrapCurrentGrid != null
                                && ScrapCurrentGrid.SelectedItem != null
                                && ScrapCurrentGrid.SelectedItem.Count > 0)
                                {
                                    result = true;
                                }
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "prihod_polyal",
                    Title = "Приход ПЭС",
                    Description = "Оприходовать кипу ПЭС из цеха",
                    Group = "scrap_current_grid_default",
                    MenuUse = false,
                    ButtonUse = IdSt == 1716 ? true : false,
                    ButtonControl = PrihodPolyAlButton,
                    ButtonName = "PrihodPolyAlButton",
                    //  AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = false,
                    Action = () =>
                    {

                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                result = true;
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "RachodPolyAlButton",
                    Title = "Расход ПЭС",
                    Description = "Загрузка ПЭС в машину",
                    Group = "scrap_current_grid_default",
                    MenuUse = false,
                    ButtonUse = IdSt == 1716 ? true : false,
                    ButtonControl = RachodPolyAlButton,
                    ButtonName = "RachodPolyAlButton",
                    //  AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = false,
                    Action = () =>
                    {

                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                if (ScrapCurrentGrid != null
                                && ScrapCurrentGrid.SelectedItem != null
                                && ScrapCurrentGrid.SelectedItem.Count > 0)
                                {
                                    var operation = ScrapCurrentGrid.SelectedItem.CheckGet("ID_OPERATION").ToString();

                                    if (operation == "E")
                                    {
                                        result = true;
                                    }
                                    else
                                    {
                                        result = false;
                                    }
                                }
                            }
                        }
                        return result;
                    },
                });
            }

            Commander.SetCurrentGridName("ScrapPzGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "scrap_pz_add",
                    Title = "Добавить",
                    Description = "Добавить задание для списания кип в производство",
                    Group = "scrap_pz_grid_default",
                    MenuUse = false,
                    ButtonUse = true,
                    ButtonControl = ScrapPzAddButton,
                    ButtonName = "ScrapPzAddButton",
                    //  AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = false,
                    Action = () =>
                    {

                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                result = true;
                            }
                        }
                        return result;
                    },
                });

                Commander.Add(new CommandItem()
                {
                    Name = "scrap_pz_edit",
                    Title = "Изменить",
                    Description = "Изменить задание для списания кип в производство",
                    Group = "scrap_pz_grid_default",
                    MenuUse = false,
                    ButtonUse = true,
                    ButtonControl = ScrapPzEditButton,
                    ButtonName = "ScrapPzEditButton",
                    //  AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = false,
                    Action = () =>
                    {

                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            // 06-12-2021 Федянина просила сделать не активной.
                            //if (ReadOnlyFlag == false)
                            //{
                            //    if (ScrapPzGrid != null
                            //    && ScrapPzGrid.SelectedItem != null
                            //    && ScrapPzGrid.SelectedItem.Count > 0)
                            //    {
                            //        result = true;
                            //    }
                            //}
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "scrap_pz_delete",
                    Title = "Удалить",
                    Description = "Удалить задание для списания кип в производство",
                    Group = "scrap_pz_grid_default",
                    MenuUse = false,
                    ButtonUse = true,
                    ButtonControl = ScrapPzDeleteButton,
                    ButtonName = "ScrapPzDeleteButton",
                    //  AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = false,
                    Action = () =>
                    {

                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                if (ScrapPzGrid != null
                                && ScrapPzGrid.SelectedItem != null
                                && ScrapPzGrid.SelectedItem.Count > 0)
                                {
                                    result = true;
                                }
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "scrap_pz_move",
                    Title = "Перенос заданий",
                    Description = "Перенос задание предыдущей смены в текущую смену",
                    Group = "scrap_pz_grid_default",
                    MenuUse = false,
                    ButtonUse = true,
                    ButtonControl = ScrapPzMoveButton,
                    ButtonName = "ScrapPzMoveButton",
                    //  AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = false,
                    Action = () =>
                    {

                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {
                            if (ReadOnlyFlag == false)
                            {
                                if (ScrapPzGrid != null
                                && ScrapPzGrid.SelectedItem != null
                                && ScrapPzGrid.SelectedItem.Count > 0)
                                {
                                    result = true;
                                }
                            }
                        }
                        return result;
                    },
                });
            }

            Commander.SetCurrentGridName("ScrapPzLogGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "scrap_pz_note_edit",
                    Title = "Композиция",
                    Description = "Изменить композицию",
                    Group = "scrap_pz_log_grid_default",
                    MenuUse = false,
                    ButtonUse = true,
                    ButtonControl = ScrapPzNoteEditButton,
                    ButtonName = "ScrapPzNoteEditButton",
                    //  AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = false,
                    Action = () =>
                    {
                        ProcessCommand("scrap_pz_note_edit");
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        {

                            if (ReadOnlyFlag == false)
                            {
                                if (ScrapPzGrid != null
                                && ScrapPzGrid.SelectedItem != null
                                && ScrapPzGrid.SelectedItem.Count > 0)
                                {
                                    result = true;
                                }
                            }
                        }
                        return result;
                    },
                });
            }

            Commander.Init(this);
        }

        /// <summary>
        /// Обработка общих сообщений
        /// </summary>
        /// <param name="action"></param>
        /// <param name="obj"></param>
        private void ProcessMessage(ItemMessage obj = null)
        {
            string action = obj.Action;
            switch (action)
            {
                case "RefreshScrapPzLog":
                    ScrapPzGridLoadItems();
                    break;

                case "RefreshScrapTransportWeightGrid":
                    ScrapTransportWeightGridLoadItems();
                    break;

                case "RefreshSetup":
                    GetSetupData();
                    break;

            }
        }


        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel(RoleName);

            switch (mode)
            {
                // Если уровень доступа -- "Спецправа",
                case Role.AccessMode.Special:
                    ReadOnlyFlag = true;
                    break;

                case Role.AccessMode.FullAccess:
                    ReadOnlyFlag = false;
                    break;

                // Если уровень доступа -- "Только чтение",
                case Role.AccessMode.ReadOnly:
                    ReadOnlyFlag = true;
                    break;

                default:
                    ReadOnlyFlag = true;
                    break;
            }
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            ScrapTransportWeightDataSet = new ListDataSet();
            ScrpTerminalDataSet = new ListDataSet();
            ScrapCurrentDataSet = new ListDataSet();
            ScrapPzGridDataSet = new ListDataSet();
            ScrapPzLogGridDataSet = new ListDataSet();

            FireStatus.Visibility = Visibility.Hidden;

            ScrapTransportReturningCheckBox.IsEnabled = false;
            ScrapTransportPolyAlCheckBox.IsEnabled = false;
            ScrapTransportChemistryCheckBox.IsEnabled = false;
            ScrapTransportDidntArriveCheckBox.IsEnabled = false;

            switch (IdSt)
            {
                case 716:
                    s1.Visibility = Visibility.Collapsed;
                    ScrapMoveToBdm1Button.Visibility = Visibility.Collapsed;

                    s2.Visibility = Visibility.Collapsed;
                    MoveToCastContainerButton.Visibility = Visibility.Collapsed;

                    ScrapTransportPolyAlCheckBox.Visibility = Visibility.Collapsed;
                    ScrapTransportChemistryCheckBox.Visibility = Visibility.Collapsed;
                    ScrapTransportDidntArriveCheckBox.Visibility = Visibility.Collapsed;

                    s5.Visibility = Visibility.Collapsed;
                    ScrapTransportFileButton.Visibility = Visibility.Collapsed;

                    s7.Visibility = Visibility.Collapsed;
                    ButtonScrapBaleMapButton.Visibility = Visibility.Collapsed;

                    s8.Visibility = Visibility.Collapsed;
                    CastContainerMapButton.Visibility = Visibility.Collapsed;

                    s12.Visibility = Visibility.Collapsed;
                    RollMoveButton.Visibility = Visibility.Collapsed;

                    PrihodPolyAlButton.Visibility = Visibility.Collapsed;
                    RachodPolyAlButton.Visibility = Visibility.Collapsed;
                    TetraPakBox.Visibility = Visibility.Collapsed;
                    WastePaperCheckBox.Visibility = Visibility.Collapsed;

                    WastePaperCheckBox.IsChecked = true;
                    TetraPakBox.IsChecked = false;

                    GetShiftList();
                    break;

                case 1716:
                    s7.Visibility = Visibility.Collapsed;
                    s13.Visibility = Visibility.Collapsed;
                    ScrapBaleMapBdm1Button.Visibility = Visibility.Collapsed;
                    CastContainerMapButton.Visibility = Visibility.Collapsed;
                    SetupButton.Visibility = Visibility.Visible;
                    WastePaperCheckBox.IsChecked = true;
                    TetraPakBox.IsChecked = true;

                    GetShiftList();
                    break;
                case 2716:
                    ScrapMoveToBdm1Button.Visibility = Visibility.Collapsed;
                    MoveToCastContainerButton.Visibility = Visibility.Collapsed;
                    ScrapTransportPolyAlCheckBox.Visibility = Visibility.Collapsed;
                    ScrapTransportChemistryCheckBox.Visibility = Visibility.Collapsed;
                    ScrapTransportDidntArriveCheckBox.Visibility = Visibility.Collapsed;
                    ScrapTransportFileButton.Visibility = Visibility.Collapsed;
                    ButtonScrapBaleMapButton.Visibility = Visibility.Collapsed;
                    ButtonScrapBaleMapButton.Visibility = Visibility.Collapsed;
                    ScrapBaleBdm2Button.Visibility = Visibility.Collapsed;
                    ScrpTerminalWorkingFlagCheckBox.Visibility = Visibility.Collapsed;
                    ScrapBaleMoveButton.Visibility = Visibility.Collapsed;
                    RollMoveButton.Visibility = Visibility.Collapsed;
                    ScrapBaleIntoProductionButton.Visibility = Visibility.Collapsed;
                    ScrapCurrentDeleteButton.Visibility = Visibility.Collapsed;
                    PrihodPolyAlButton.Visibility = Visibility.Collapsed;
                    RachodPolyAlButton.Visibility = Visibility.Collapsed;
                    Zadanie.Visibility = Visibility.Collapsed;
                    ScrapTransportScanBarcodeButton.Visibility = Visibility.Collapsed;
                    ScrapBaleMapBdm1Button.Visibility = Visibility.Collapsed;
                    ScrapTransportReturningCheckBox.Visibility = Visibility.Collapsed;
                    ScrapTransportAudDeleteCheckBox.Visibility = Visibility.Collapsed;
                    ScrapTransportPopupMenuButton.Visibility = Visibility.Collapsed;
                    ScrapTransportEditButton.Visibility = Visibility.Collapsed;
                    s1.Visibility = Visibility.Collapsed;
                    s2.Visibility = Visibility.Collapsed;
                    s3.Visibility = Visibility.Collapsed;
                    s5.Visibility = Visibility.Collapsed;
                    s6.Visibility = Visibility.Collapsed;
                    s7.Visibility = Visibility.Collapsed;
                    s10.Visibility = Visibility.Collapsed;
                    s11.Visibility = Visibility.Collapsed;
                    WastePaperCheckBox.IsChecked = false;
                    TetraPakBox.IsChecked = false;
                    break;
            }

            // время (сек) автообновления весовая
            ScrapTransportRefreshTime = 60;
            // время (сек) автообновления задания на макулатуру
            ScrapPzRefreshTime = 300;

            ScrapTransportCurSecund = ScrapPzCurSecund = 0;
        }

        /// <summary>
        // инициализация компонентов формы
        /// </summary>
        public void FormInit()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="SEARCH_STR",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=EditScrapTransportFilterSearchBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SCRAP_TRANSPORT_ALL",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ScrapTransportWeightAllCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
                new FormHelperField()
                {
                    Path="SCRAP_TRANSPORT_RETURNING",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ScrapTransportReturningCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
                new FormHelperField()
                {
                    Path="SCRAP_TRANSPORT_DELETE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ScrapTransportAudDeleteCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
                new FormHelperField()
                {
                    Path="SCRAP_TRANSPORT_POLYAL",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ScrapTransportPolyAlCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
                new FormHelperField()
                {
                    Path="SCRAP_TRANSPORT_CHEMISTRY",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ScrapTransportChemistryCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
                new FormHelperField()
                {
                    Path="SCRAP_TRANSPORT_DIDNT_ARRIVE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ScrapTransportDidntArriveCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
                new FormHelperField()
                {
                    Path="WASTE_PAPER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=WastePaperCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
                new FormHelperField()
                {
                    Path="TETRA_PAK",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TetraPakBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },


            };

            Form.SetFields(fields);
            Form.SetDefaults();

            //фокус на кнопку обновления
            ScrapTransportRefreshButton.Focus();
        }

        /// <summary>
        /// 1. Весовая
        /// </summary>
        private void ScrapTransportWeightGridInit()
        {
            var roleLevel = Central.Navigator.GetRoleLevel(RoleName);
            var fullAccess = roleLevel == Role.AccessMode.FullAccess;

            //инициализация грида
            var columns = new List<DataGridHelperColumn>
            {
                 new DataGridHelperColumn
                 {
                     Header="#",
                     Path="NUM",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Описание машины",
                     Path="IS_SCRAP_ATTR",
                     ColumnType=ColumnTypeRef.Boolean,
                     Width2=3,
                     Visible=true,
                     Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                     {
                         {
                             StylerTypeRef.BackgroundColor, // фон
                             row => GetColorRolls("IS_SCRAP_ATTR", row)
                         },
                     },
                 },
                 new DataGridHelperColumn
                 {
                     Header="Самовыоз",
                     Path="IS_FCA",
                     ColumnType=ColumnTypeRef.Boolean,
                     Width2=3,
                     Visible=false,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Цех",
                     Path="PRODCTN",
                     ColumnType=ColumnTypeRef.String,
                     Width2=3,
                     Visible=false,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Файл",
                     Path="TRANSPORT_FILE_COUNT",
                     ColumnType=ColumnTypeRef.Boolean,
                     Width2=3,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="ИД",
                     Path="ID_SCRAP",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Машина",
                     Path="NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=18,
                     Visible=true,
                     Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                     {
                         {
                             StylerTypeRef.BackgroundColor, // фон
                             row => GetColorRolls("NAME", row)
                         },
                         {
                                DataGridHelperColumn.StylerTypeRef.ForegroundColor, // цвет шрифта
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    if ((row.CheckGet("ID_STATUS").ToInt() >= 11)     // ПЭС
                                    && (row.CheckGet("ID_STATUS").ToInt() <= 25))
                                    {
                                        color = $"#808080"; // серый
                                    }
                                    else
                                    if ((row.CheckGet("ID_STATUS").ToInt() >= 41)     // химия
                                    && (row.CheckGet("ID_STATUS").ToInt() <= 45))
                                    {
                                        color = $"#0070C0"; // пурпупный
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                            {
                                DataGridHelperColumn.StylerTypeRef.FontWeight, // размер шрифта
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var fontWeight= new FontWeight();
                                    fontWeight=FontWeights.Bold;
                                    result=fontWeight;
                                    return result;
                                }
                            },
                     },
                     Labels=new List<DataGridHelperColumnLabel>()
                     {
                            new DataGridHelperColumnLabel()
                            {
                                Construct=()=>
                                {
                                    var block=DataGridHelperColumnLabel.MakeElement("Ц");
                                    return block;
                                },
                                Update = (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    {
                                        result=Visibility.Hidden;
                                        if(row.CheckGet("PRODCTN").ToString() == "ЦЛТ")
                                        {
                                            result=Visibility.Visible;
                                        }
                                    }
                                    return result;
                                },
                            },
                            new DataGridHelperColumnLabel()
                            {
                                Construct=()=>
                                {
                                    var block=DataGridHelperColumnLabel.MakeElement("СВ","#FFFFC182","#ff000000", 32, 16);
                                    return block;
                                },
                                Update = (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    {
                                        result=Visibility.Hidden;
                                        if(row.CheckGet("IS_FCA").ToInt() == 1)
                                        {
                                            result=Visibility.Visible;
                                        }
                                    }
                                    return result;
                                },
                            },
                     },
                 },
                 new DataGridHelperColumn
                 {
                     Header="Статус",
                     Path="STATUS_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=20,
                     Visible=true,
                     Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                     {
                         {
                             StylerTypeRef.BackgroundColor, // фон
                             row => GetColorRolls("STATUS_NAME", row)
                         },
                         {
                                DataGridHelperColumn.StylerTypeRef.FontWeight, // размер шрифта
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var fontWeight= new FontWeight();

                                    if (((row.CheckGet("ID_STATUS").ToInt() >= 27)  // возврат/хранение
                                    && (row.CheckGet("ID_STATUS").ToInt() <= 30))
                                    && (row.CheckGet("DT_RETURN").ToString().IsNullOrEmpty()))
                                    {
                                    fontWeight=FontWeights.Bold;
                                    }
                                    result=fontWeight;
                                    return result;
                                }
                            },
                     },
                 },
                 new DataGridHelperColumn
                 {
                     Header="Телефон",
                     Path="PHONE_NUMBER",
                     ColumnType=ColumnTypeRef.String,
                     Width2=10,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Поставщик/Покупатель",
                     Path="POST_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=30,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Приемки",
                     Path="NUM_AKT_STR",
                     Group="Акты",
                     ColumnType=ColumnTypeRef.String,
                     Width2=11,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Возврата",
                     Path="ACT_RETURN_NUM",
                     Group="Акты",
                     ColumnType=ColumnTypeRef.String,
                     Width2=7,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата регистрации",
                     Path="CREATED_DTTM",
                     Group="Дата",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 15,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Слот",
                     Path="DTTM_SLOT",
                     Group="Дата",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 15,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Полная",
                     Path="DT_FULL",
                     Group="Дата",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 15,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Пустая",
                     Path="DT_EMPTY",
                     Group="Дата",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 15,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="с грузом",
                     Path="WEIGHT_FULL",
                     Doc="Вес полная",
                     Group="Вес",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=7,
                     Visible=true,
                     Format="N0",
                     Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                     {
                         //{
                         //    StylerTypeRef.BackgroundColor, // фон
                         //    row => GetColorRolls("WEIGHT_FULL", row)
                         //},
                         {
                                DataGridHelperColumn.StylerTypeRef.ForegroundColor, // цвет шрифта
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    if ((row.CheckGet("AUTO_INPUT").ToInt() == 1) || (row.CheckGet("AUTO_INPUT").ToInt() == 3))
                                    {
                                        color = $"#FF0000"; // красный
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                            {
                                DataGridHelperColumn.StylerTypeRef.FontWeight, // размер шрифта
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var fontWeight= new FontWeight();
                                    if ((row.CheckGet("AUTO_INPUT").ToInt() == 1) || (row.CheckGet("AUTO_INPUT").ToInt() == 3))
                                    {
                                        fontWeight=FontWeights.Bold;
                                    }

                                    result=fontWeight;
                                    return result;
                                }
                            },
                     },
                 },
                 new DataGridHelperColumn
                 {
                     Header="пустой",
                     Path="WEIGHT_EMPTY",
                     Doc="Вес пустая",
                     Group="Вес",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=7,
                     Visible=true,
                     Format="N0",
                     Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                     {
                         //{
                         //    StylerTypeRef.BackgroundColor, // фон
                         //    row => GetColorRolls("WEIGHT_FULL", row)
                         //},
                         {
                                DataGridHelperColumn.StylerTypeRef.ForegroundColor, // цвет шрифта
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    if ((row.CheckGet("AUTO_INPUT").ToInt() == 2) || (row.CheckGet("AUTO_INPUT").ToInt() == 3))
                                    {
                                        color = $"#FF0000"; // красный
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                            {
                                DataGridHelperColumn.StylerTypeRef.FontWeight, // размер шрифта
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var fontWeight= new FontWeight();
                                    if ((row.CheckGet("AUTO_INPUT").ToInt() == 2) || (row.CheckGet("AUTO_INPUT").ToInt() == 3))
                                    {
                                        fontWeight=FontWeights.Bold;
                                    }

                                    result=fontWeight;
                                    return result;
                                }
                            },
                     },
                 },
                 new DataGridHelperColumn
                 {
                     Header="по документам",
                     Path="WEIGHT_DOK",
                     Doc="Вес по документам",
                     Group="Вес",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=7,
                     Visible=true,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="фактический",
                     Path="WEIGHT_FACT",
                     Doc="Вес фактический",
                     Group="Вес",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=7,
                     Visible=true,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="возврата/хранения",
                     Path="WEIGHT_RETURNING",
                     Doc="Вес возврата/хранения",
                     Group="Вес",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=7,
                     Visible=true,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="по документам",
                     Path="QUANTITY_BAL_DOC",
                     Doc="Кип по документам",
                     Group="Кип",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=7,
                     Visible=true,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="принято",
                     Path="QTY",
                     Doc="Кип принято",
                     Group="Кип",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=7,
                     Visible=true,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="возвращено/хранение",
                     Path="QTY_RETURNING",
                     Doc="Кип возвращено/хранение",
                     Group="Кип",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=6,
                     Visible=true,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ячейка",
                     Path="SKLAD",
                     ColumnType=ColumnTypeRef.String,
                     Width2=7,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Сотрудник",
                     Path="NAME_CS",
                     ColumnType=ColumnTypeRef.String,
                     Width2=16,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Водитель погрузчика",
                     Path="STAFF_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=16,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="ШК",
                     Path="BARCODE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=11,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="NNAKL",
                     Path="NNAKL",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=8,
                     Visible=Central.DebugMode,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="ID_POST",
                     Path="ID_POST",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=8,
                     Visible=Central.DebugMode,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="NSTHET",
                     Path="NSTHET",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=8,
                     Visible=Central.DebugMode,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="ID_STATUS",
                     Path="ID_STATUS",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=8,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="EMPL_ID",
                     Path="EMPL_ID",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=8,
                     Visible=Central.DebugMode,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="ID_CATEGORY",
                     Path="ID_CATEGORY",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=8,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="AUTO_INPUT",
                     Path="AUTO_INPUT",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=8,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="IS_ALARMED",
                     Path="IS_ALARMED",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=8,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="PAPER_TETRAPAK_FLAG",
                     Path="PAPER_TETRAPAK_FLAG",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=8,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="INPUT_CONTROL_FLAG",
                     Path="INPUT_CONTROL_FLAG",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=8,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="ID2",
                     Path="ID2",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=8,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="IS_RET",
                     Path="IS_RET",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=8,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="ORD",
                     Path="ORD",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="STATUS_RETURN",
                     Path="STATUS_RETURN",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=8,
                     Visible=Central.DebugMode,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата возврата",
                     Path="DT_RETURN",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 15,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="CRC",
                     Path="CRC",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=Central.DebugMode,
                 },

            };

            // раскраска всей строки
            ScrapTransportWeightGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                //{
                //    StylerTypeRef.BackgroundColor,
                //    row =>
                //    {
                //        var result=DependencyProperty.UnsetValue;
                //        var color = "";

                //        var days = row.CheckGet("DAYS").ToInt();

                //        if (days > 31) // прошло более 31 дня с момента разгрузки
                //        {
                //            color = "#FBC2C6"; // cветло-розовый  Background := RGB(251, 194, 198);
                //        }

                //        if (!string.IsNullOrEmpty(color))
                //        {
                //            result=color.ToBrush();
                //        }

                //        return result;
                //    }
                //},
                {
                       DataGridHelperColumn.StylerTypeRef.FontWeight, // размер шрифта
                       row =>
                       {
                           var result=DependencyProperty.UnsetValue;
                           var fontWeight= new FontWeight();
                           // машина не приехала
                           if (row.CheckGet("ID_STATUS").ToInt() == 66)
                           {
                               fontWeight=FontWeights.Bold;
                           }

                            result=fontWeight;
                            return result;
                       }
                },
                // определение цветов шрифта строк
                {
                    StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                            // разгружается
                            //if (row.ContainsKey("ORD"))
                            //{
                            //    if(row["ORD"].ToInt() == 1 || row["ORD"].ToInt() == 2)
                            //    {
                            //        color = HColor.BlackFG;
                            //    }
                            //}

                            //if (row.ContainsKey("ID_STATUS"))
                            //{
                            //    if ((row["ID_STATUS"].ToInt() >= 11)
                            //    && (row["ID_STATUS"].ToInt() <= 25)) // это Полиэтилен.
                            //    {
                            //       color = $"#0070C0"; // пурпупный фон;// HColor.Gray;
                            //    }
                            //}

                            if (row.ContainsKey("ID_STATUS"))
                            {
                                if (row["ID_STATUS"].ToInt() == 66) // машина не приехала
                                {
                                   color = $"#800000"; // коричнево-малиновый;// HColor.Gray;
                                }

                                if ((row["ID_STATUS"].ToInt() >= 27)
                                 && (row["ID_STATUS"].ToInt() <= 30)) // возврат/хранение
                                {
                                   color = $"#FF0000"; // светло-красный;// HColor.Gray;
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

            ScrapTransportWeightGrid.SetColumns(columns);
            ScrapTransportWeightGrid.SetPrimaryKey("NUM");

            ScrapTransportWeightGrid.SearchText = EditScrapTransportFilterSearchBox;
            //данные грида
            ScrapTransportWeightGrid.OnLoadItems = ScrapTransportWeightGridLoadItems;
            ScrapTransportWeightGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ScrapTransportWeightGrid.AutoUpdateInterval = 0;
            ScrapTransportWeightGrid.EnableSortingGrid = false;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            ScrapTransportWeightGrid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    ScrapTransportWeightSelectedItem = selectedItem;
                }
            };

            // двойной клик по строке
            // Report1Grid.OnDblClick = EditShow;

            // фильтрация по гриду
            ScrapTransportWeightGrid.OnFilterItems = () =>
            {
                if (ScrapTransportWeightGrid.Items.Count > 0)
                {
                    var v = Form.GetValues();

                    var showAll = false;

                    // все записи
                    var items_all = v.CheckGet("SCRAP_TRANSPORT_ALL").ToBool();

                    // возврат/хранение
                    var returning = v.CheckGet("SCRAP_TRANSPORT_RETURNING").ToBool();
                    // удаленные
                    var deleting = v.CheckGet("SCRAP_TRANSPORT_DELETE").ToBool();
                    // ПЭС
                    var polyal = v.CheckGet("SCRAP_TRANSPORT_POLYAL").ToBool();
                    // химия
                    var chemistry = v.CheckGet("SCRAP_TRANSPORT_CHEMISTRY").ToBool();
                    // машина не приехала
                    var dint_arrive = v.CheckGet("SCRAP_TRANSPORT_DIDNT_ARRIVE").ToBool();

                    var items = new List<Dictionary<string, string>>();
                    foreach (Dictionary<string, string> row in ScrapTransportWeightGrid.Items)
                    {
                        if ((items_all)
                        && (returning || polyal || chemistry || dint_arrive))
                        {
                            if (returning)
                            {
                                if ((row.CheckGet("ID_STATUS").ToInt() >= 27) && (row.CheckGet("ID_STATUS").ToInt() <= 30))
                                {
                                    items.Add(row);
                                }
                            }

                            if (polyal)
                            {
                                if ((row.CheckGet("ID_STATUS").ToInt() >= 11) && (row.CheckGet("ID_STATUS").ToInt() <= 25))
                                {
                                    items.Add(row);
                                }
                            }

                            if (chemistry)
                            {
                                if ((row.CheckGet("ID_STATUS").ToInt() >= 41) && (row.CheckGet("ID_STATUS").ToInt() <= 45))
                                {
                                    items.Add(row);
                                }
                            }

                            if (dint_arrive)
                            {
                                if (row.CheckGet("ID_STATUS").ToInt() == 66)
                                {
                                    items.Add(row);
                                }
                            }
                        }
                        else
                        if ((items_all)
                        && (!returning || !polyal || !chemistry || !dint_arrive))
                        {
                            items.Add(row);
                        }
                        else if (!items_all)
                        {
                            items.Add(row);
                        }
                    }

                    ScrapTransportWeightGrid.Items = items;
                }
            };

            ScrapTransportWeightGrid.Commands = Commander;
            ScrapTransportWeightGrid.Init();
        }

        /// <summary>
        /// загрузка данных для отчета 
        /// </summary>
        /// <param name="num"></param>
        public async void ScrapTransportWeightGridLoadItems()
        {
            bool resume = true;


            if (resume)
            {
                var q = new LPackClientQuery();

                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_ST", IdSt.ToString());
                    p.CheckAdd("NUMBEROFWEEK", EditNumberOfWeek.Text);

                    if (ScrapTransportWeightAllCheckBox.IsChecked == true)
                    {
                        p.CheckAdd("ID_STAT", "");
                        p.CheckAdd("RET", "1");
                    }
                    else
                    {
                        p.CheckAdd("ID_STAT", "200");
                        p.CheckAdd("RET", "0");
                    }
                }

                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "ScrapPaper");
                q.Request.SetParam("Action", "ScrapTransportSelectWeight");
                q.Request.SetParams(p);

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
                        var ds = ListDataSet.Create(result, "ITEMS");
                        ScrapTransportWeightGrid.UpdateItems(ds);
                        SetSplash(false);
                    }
                }
            }
        }

        /// <summary>
        /// 2. терминалы
        /// </summary>
        private void ScrpTerminalGridInit()
        {
            var roleLevel = Central.Navigator.GetRoleLevel(RoleName);
            var editableCheck = roleLevel == Role.AccessMode.FullAccess;
            //инициализация грида
            var columns = new List<DataGridHelperColumn>
            {
                 new DataGridHelperColumn
                 {
                     Header="#",
                     Path="_ROWNUMBER",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=false,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Рабочий",
                     Path="WORKING_FLAG", //_SELECTED
                     ColumnType=ColumnTypeRef.Boolean,
                     Width2=8,
                     Editable=editableCheck,
                     Visible=true,
                     OnClickAction = (row, el) =>
                     {
                       EditTerminal(row);
                       return true;

                        if (el != null)
                        {
                            return true;
                        }

                     },
                 },
                 new DataGridHelperColumn
                 {
                     Header="Терминал",
                     Path="NUM_TERMINAL",
                     ColumnType=ColumnTypeRef.String,
                     Width2=9,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Машина",
                     Path="NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=23,
                     Visible=true,
                     Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                     {
                         {
                             StylerTypeRef.BackgroundColor, // фон
                             row => GetColorRolls("NAME2", row)
                         },
                         {
    DataGridHelperColumn.StylerTypeRef.ForegroundColor, // цвет шрифта
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    var color = "";

                                    if ((row.CheckGet("ID_STATUS").ToInt() >= 11)     // ПЭС
                                    && (row.CheckGet("ID_STATUS").ToInt() <= 25))
                                    {
                                        color = $"#808080"; // серый
                                    }
                                    else
                                    if ((row.CheckGet("ID_STATUS").ToInt() >= 41)     // химия
                                    && (row.CheckGet("ID_STATUS").ToInt() <= 45))
                                    {
                                        color = $"#0070C0"; // пурпупный
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result = color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                            {
    DataGridHelperColumn.StylerTypeRef.FontWeight, // размер шрифта
                                row =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    var fontWeight = new FontWeight();
                                    fontWeight = FontWeights.Bold;
                                    result = fontWeight;
                                    return result;
                                }
                            },
                     },

                 },
                 new DataGridHelperColumn
                 {
                     Header = "Ряд",
                     Path = "SKLAD",
                     ColumnType = ColumnTypeRef.String,
                     Width2 = 7,
                     Visible = true,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Ячейка",
                     Path = "PLACE",
                     ColumnType = ColumnTypeRef.String,
                     Width2 = 8,
                     Visible = true,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Дата привязки",
                     Path = "DT",
                     ColumnType = ColumnTypeRef.DateTime,
                     Format = "dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                     Visible = true,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "ИД",
                     Path = "ID_SCRAP",
                     ColumnType = ColumnTypeRef.Double,
                     Width2 = 8,
                     Visible = true,
                     Format = "N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header = "ID_STATUS",
                     Path = "ID_STATUS",
                     ColumnType = ColumnTypeRef.Double,
                     Width2 = 9,
                     Visible = Central.DebugMode,
                     Format = "N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header = "SCTE_ID",
                     Path = "SCTE_ID",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 = 9,
                     Visible = Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "ID_ST",
                     Path = "ID_ST",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 = 9,
                     Visible = Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "NUMBER_TERMINAL",
                     Path = "NUMBER_TERMINAL",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 = 9,
                     Visible = Central.DebugMode,
                 },
            };

            ScrpTerminalGrid.SetColumns(columns);
            ScrpTerminalGrid.SetPrimaryKey("_ROWNUMBER");

            //данные грида
            ScrpTerminalGrid.OnLoadItems = ScrpTerminalGridLoadItems;
            ScrpTerminalGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ScrpTerminalGrid.AutoUpdateInterval = 0;
            ScrpTerminalGrid.EnableSortingGrid = false;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            ScrpTerminalGrid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    ScrpTerminalSelectedItem = selectedItem;
                }
            };

            ScrpTerminalGrid.Commands = Commander;
            ScrpTerminalGrid.Init();
        }

        /// <summary>
        /// загрузка данных для отчета 
        /// </summary>
        /// <param name="num"></param>
        public async void ScrpTerminalGridLoadItems()
        {
            bool resume = true;


            if (resume)
            {
                var q = new LPackClientQuery();

                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_ST", IdSt.ToString());
                }

                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "ScrapPaper");
                q.Request.SetParam("Action", "ScrpTerminalSelect");
                q.Request.SetParams(p);

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
                        var ds = ListDataSet.Create(result, "ITEMS");
                        ScrpTerminalGrid.UpdateItems(ds);

                    }
                }
            }
        }

        /// <summary>
        /// 3. В работе
        /// </summary>
        private void ScrapCurrentGridInit()
        {
            //инициализация грида
            var columns = new List<DataGridHelperColumn>
            {
                 new DataGridHelperColumn
                 {
                     Header="#",
                     Path="_ROWNUMBER",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Операция",
                     Path="OPERATION",
                     ColumnType=ColumnTypeRef.String,
                     Width2=8,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Товар",
                     Path="TOVAR_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=18,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ячейка",
                     Path="S",
                     ColumnType=ColumnTypeRef.String,
                     Width2=8,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Машина",
                     Path="NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=22,
                     Visible=true,
                     Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                     {
                         {
                             StylerTypeRef.BackgroundColor, // фон
                             row => GetColorRolls("NAME3", row)
                         },
                         {
                                DataGridHelperColumn.StylerTypeRef.ForegroundColor, // цвет шрифта
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    if ((row.CheckGet("ID_STATUS").ToInt() >= 11)     // ПЭС
                                    && (row.CheckGet("ID_STATUS").ToInt() <= 25))
                                    {
                                        color = $"#808080"; // серый
                                    }
                                    else
                                    if ((row.CheckGet("ID_STATUS").ToInt() >= 41)     // химия
                                    && (row.CheckGet("ID_STATUS").ToInt() <= 45))
                                    {
                                        color = $"#0070C0"; // пурпупный
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                            {
                                DataGridHelperColumn.StylerTypeRef.FontWeight, // размер шрифта
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var fontWeight= new FontWeight();
                                    fontWeight=FontWeights.Bold;
                                    result=fontWeight;
                                    return result;
                                }
                            },
                     },

                 },
                 new DataGridHelperColumn
                 {
                     Header="Поставщик",
                     Path="NAME_POST",
                     ColumnType=ColumnTypeRef.String,
                     Width2=24,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата",
                     Path="DT",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Вес в машине",
                     Path="WEIGHT_FACT",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=9,
                     Visible=true,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="Статус",
                     Path="STATUS_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=16,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Кип на складе",
                     Path="BALE_ON_SKLAD",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=9,
                     Visible=true,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="Кип списано/загружено",
                     Path="BALE_ON_PRODUCTION",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=9,
                     Visible=true,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="Водитель погрузчика",
                     Path="STAFF_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=15,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="ИД",
                     Path="ID_SCRAP",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=9,
                     Visible=Central.DebugMode,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="ID_OPERATION",
                     Path="ID_OPERATION",
                     ColumnType=ColumnTypeRef.String,
                     Width2=9,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="ID_STATUS",
                     Path="ID_STATUS",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=9,
                     Visible=Central.DebugMode,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="PAPER_TETRAPAK_FLAG",
                     Path="PAPER_TETRAPAK_FLAG",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=9,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="ID2",
                     Path="ID2",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=9,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="INPUT_CONTROL_FLAG",
                     Path="INPUT_CONTROL_FLAG",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=9,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="ID_CATEGORY",
                     Path="ID_CATEGORY",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=9,
                     Visible=Central.DebugMode,
                 },

            };

            ScrapCurrentGrid.SetColumns(columns);
            ScrapCurrentGrid.SetPrimaryKey("_ROWNUMBER");

            //данные грида
            ScrapCurrentGrid.OnLoadItems = ScrapCurrentGridLoadItems;
            ScrapCurrentGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ScrapCurrentGrid.AutoUpdateInterval = 0;
            ScrapCurrentGrid.EnableSortingGrid = false;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            ScrapCurrentGrid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    ScrapCurrentSelectedItem = selectedItem;
                }
            };

            ScrapCurrentGrid.Commands = Commander;
            ScrapCurrentGrid.Init();
        }

        /// <summary>
        /// загрузка данных для отчета 3
        /// </summary>
        /// <param name="num"></param>
        public async void ScrapCurrentGridLoadItems()
        {
            bool resume = true;


            if (resume)
            {
                var q = new LPackClientQuery();

                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_ST", IdSt.ToString());
                }

                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "ScrapPaper");
                q.Request.SetParam("Action", "ScrapCurrentSelect");
                q.Request.SetParams(p);

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
                        var ds = ListDataSet.Create(result, "ITEMS");
                        ScrapCurrentGrid.UpdateItems(ds);

                    }
                }
            }
        }

        /// <summary>
        /// 4. Задания на макулатуру
        /// </summary>
        private void ScrapPzGridInit()
        {
            //инициализация грида
            var columns = new List<DataGridHelperColumn>
            {
                 new DataGridHelperColumn
                 {
                     Header="#",
                     Path="_ROWNUMBER",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Поставщик",
                     Path="NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=40,
                     Visible=true,
                     Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                     {
                         {
                             StylerTypeRef.BackgroundColor, // фон
                             row => GetColorRolls("NAME3", row)
                         },
                         {
                             DataGridHelperColumn.StylerTypeRef.FontWeight, // размер шрифта
                             row =>
                             {
                                 var result=DependencyProperty.UnsetValue;
                                 var fontWeight= new FontWeight();
                                 fontWeight=FontWeights.Bold;
                                 result=fontWeight;
                                 return result;
                             }
                         },
                     },

                 },
                 new DataGridHelperColumn
                 {
                     Header="Товар",
                     Path="TOVAR_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=22,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Место",
                     Path="SKLAD_PLACE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=7,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата прихода",
                     Path="DATA",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy",
                     Width2 = 11,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Вес (приход)",
                     Path="WEIGTH",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=9,
                     Visible=true,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="Кип (осталось)",
                     Path="CNT",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=8,
                     Visible=true,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="Остаток (кг.)",
                     Path="KOL",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=10,
                     Visible=true,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="Сотрудник",
                     Path="NAME_CS",
                     ColumnType=ColumnTypeRef.String,
                     Width2=16,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата добавления",
                     Path="DT_TM",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Водитель погрузчика",
                     Path="STAFF_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=15,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="ID_SCRAP_PZ",
                     Path="ID_SCRAP_PZ",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=9,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="SCPZ_ID",
                     Path="SCPZ_ID",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=9,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="NNAKL",
                     Path="NNAKL",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=9,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="ID_TIMES",
                     Path="ID_TIMES",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=9,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="ID_CATEGORY",
                     Path="ID_CATEGORY",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=9,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="ID2",
                     Path="ID2",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=9,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="ID_ST",
                     Path="ID_ST",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=9,
                     Visible=Central.DebugMode,
                 },

            };

            ScrapPzGrid.SetColumns(columns);
            ScrapPzGrid.SetPrimaryKey("_ROWNUMBER");

            //данные грида
            ScrapPzGrid.OnLoadItems = ScrapPzGridLoadItems;
            ScrapPzGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ScrapPzGrid.AutoUpdateInterval = 0;
            ScrapPzGrid.EnableSortingGrid = false;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            ScrapPzGrid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    ScrapPzSelectedItem = selectedItem;
                }
            };

            // фильтрация по гриду
            ScrapPzGrid.OnFilterItems = () =>
            {
                if (ScrapPzGrid.Items.Count > 0)
                {
                    var v = Form.GetValues();

                    // Макулатура
                    var wastePaper = v.CheckGet("WASTE_PAPER").ToBool();
                    // ТетраПак
                    var tetraPak = v.CheckGet("TETRA_PAK").ToBool();

                    var items = new List<Dictionary<string, string>>();
                    foreach (Dictionary<string, string> row in ScrapPzGrid.Items)
                    {
                        if (tetraPak)
                        {
                            if ((row.CheckGet("ID2").ToInt() == 35007)
                            || (row.CheckGet("ID2").ToInt() == 261785)
                            || (row.CheckGet("ID2").ToInt() == 261786))
                            {
                                items.Add(row);
                            }
                        }

                        if (wastePaper)
                        {
                            if ((row.CheckGet("ID2").ToInt() != 35007)
                            && (row.CheckGet("ID2").ToInt() != 261785)
                            && (row.CheckGet("ID2").ToInt() != 261786))
                            {
                                items.Add(row);
                            }
                        }
                    }

                    ScrapPzGrid.Items = items;
                }
            };

            ScrapPzGrid.Commands = Commander;
            ScrapPzGrid.Init();
        }

        /// <summary>
        /// загрузка данных для отчета 4
        /// </summary>
        /// <param name="num"></param>
        public async void ScrapPzGridLoadItems()
        {
            bool resume = true;

            ScrapPzSelected = 0;

            var idTimes = SmenaSelectBox.SelectedItem.Key.ToInt();

            if (resume)
            {
                var q = new LPackClientQuery();

                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_ST", IdSt.ToString());
                    p.CheckAdd("ID_TIMES", idTimes.ToString());
                }

                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "ScrapPaper");
                q.Request.SetParam("Action", "ScrapPzSelect");
                q.Request.SetParams(p);

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
                        var ds = ListDataSet.Create(result, "ITEMS");
                        ScrapPzGrid.UpdateItems(ds);

                        if (ds.Items.Count > 0)
                        {
                            ScrapPzGrid.SelectRowFirst();
                            ScrapPzSelected = ds.Items[0].CheckGet("SCPZ_ID").ToInt();
                            ScrapPzLogGridLoadItems();
                            if (RunFirst)
                                RunFirst = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 5. История изменения композиции
        /// </summary>
        private void ScrapPzLogGridInit()
        {
            //инициализация грида
            var columns = new List<DataGridHelperColumn>
            {
                 new DataGridHelperColumn
                 {
                     Header="#",
                     Path="NUM",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=false,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Композиция",
                     Path="NOTE",
                     ColumnType=ColumnTypeRef.String,
                     //Width2=47,
                     Width=400,
                     MinWidth=360,
                     MaxWidth=400,
                     Visible=true,
                     Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                     {
                         {
                             StylerTypeRef.BackgroundColor, // фон
                             row => GetColorRolls("NOTE", row)
                         },
                     },

                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата добавления",
                     Path="DTTM",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     //Width2 = 14,
                     Width=114,
                     MinWidth=114,
                     MaxWidth=114,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Сотрудник",
                     Path="NAME_CONTROLLER",
                     ColumnType=ColumnTypeRef.String,
                    // Width2=16,
                     Width=155,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header=" ",
                     Path="_",
                     ColumnType=ColumnTypeRef.String,
                     MinWidth=2,
                     MaxWidth=2000,
                 },
            };

            ScrapPzLogGrid.SetColumns(columns);

            // Grid
            ScrapPzLogGrid.PrimaryKey = "NUM";
            ScrapPzLogGrid.UseSorting = false;
            ScrapPzLogGrid.UseRowHeader = false;
            ScrapPzLogGrid.SelectItemMode = 1;

            //данные грида
            ScrapPzLogGrid.OnLoadItems = ScrapPzLogGridLoadItems;

            ScrapPzLogGrid.AutoUpdateInterval = 0;

            // Grid4
            // ScrapPzLogGrid.SetPrimaryKey("NUM");
            // ScrapPzLogGrid.EnableSortingGrid = false;
            // ScrapPzLogGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact; //Full;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            ScrapPzLogGrid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    ;
                }
            };

            //  ScrapPzLogGrid.Commands = Commander;
            ScrapPzLogGrid.Init();
        }

        /// <summary>
        /// загрузка данных для отчета 5
        /// </summary>
        /// <param name="num"></param>
        public async void ScrapPzLogGridLoadItems()
        {
            bool resume = true;

            if (ScrapPzSelected == 0)
                return;

            if (resume)
            {
                var q = new LPackClientQuery();

                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("SCPZ_ID", ScrapPzSelected.ToString());
                }

                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "ScrapPaper");
                q.Request.SetParam("Action", "ScrapPzLogSelect");
                q.Request.SetParams(p);

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
                        var ds = ListDataSet.Create(result, "ITEMS");
                        ScrapPzLogGrid.UpdateItems(ds);

                    }
                }
            }
        }

        /////////////////////////

        /// <summary>
        /// 6. Удаленные с весовой машины
        /// </summary>
        private void ScrapTransportDeleteWeightGridInit()
        {
            //инициализация грида
            var columns = new List<DataGridHelperColumn>
            {
                 new DataGridHelperColumn
                 {
                     Header="#",
                     Path="NUM",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Описание машины",
                     Path="IS_SCRAP_ATTR",
                     ColumnType=ColumnTypeRef.Boolean,
                     Width2=3,
                     Visible=true,
                     Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                     {
                         {
                             StylerTypeRef.BackgroundColor, // фон
                             row => GetColorRolls("IS_SCRAP_ATTR", row)
                         },
                     },
                 },
                 new DataGridHelperColumn
                 {
                     Header="Самовыоз",
                     Path="IS_FCA",
                     ColumnType=ColumnTypeRef.Boolean,
                     Width2=3,
                     Visible=false,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Файл",
                     Path="TRANSPORT_FILE_COUNT",
                     ColumnType=ColumnTypeRef.Boolean,
                     Width2=3,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="ИД",
                     Path="ID_SCRAP",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Машина",
                     Path="NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=18,
                     Visible=true,
                     Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                     {
                         {
                             StylerTypeRef.BackgroundColor, // фон
                             row => GetColorRolls("NAME", row)
                         },
                         {
                                DataGridHelperColumn.StylerTypeRef.ForegroundColor, // цвет шрифта
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    if ((row.CheckGet("ID_STATUS").ToInt() >= 11)     // ПЭС
                                    && (row.CheckGet("ID_STATUS").ToInt() <= 25))
                                    {
                                        color = $"#808080"; // серый
                                    }
                                    else
                                    if ((row.CheckGet("ID_STATUS").ToInt() >= 41)     // химия
                                    && (row.CheckGet("ID_STATUS").ToInt() <= 45))
                                    {
                                        color = $"#0070C0"; // пурпупный
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                            {
                                DataGridHelperColumn.StylerTypeRef.FontWeight, // размер шрифта
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var fontWeight= new FontWeight();
                                    fontWeight=FontWeights.Bold;
                                    result=fontWeight;
                                    return result;
                                }
                            },
                     },
                     Labels=new List<DataGridHelperColumnLabel>()
                     {
                            new DataGridHelperColumnLabel()
                            {
                                Construct=()=>
                                {
                                    var block=DataGridHelperColumnLabel.MakeElement("СВ","#FFFFC182","#ff000000", 32, 16);
                                    return block;
                                },
                                Update = (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    {
                                        result=Visibility.Hidden;
                                        if(row.CheckGet("IS_FCA").ToInt() == 1)
                                        {
                                            result=Visibility.Visible;
                                        }
                                    }
                                    return result;
                                },
                            },
                     },
                 },
                 new DataGridHelperColumn
                 {
                     Header="Статус",
                     Path="STATUS_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=20,
                     Visible=true,
                     Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                     {
                         {
                             StylerTypeRef.BackgroundColor, // фон
                             row => GetColorRolls("STATUS_NAME", row)
                         },
                         {
                                DataGridHelperColumn.StylerTypeRef.FontWeight, // размер шрифта
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var fontWeight= new FontWeight();

                                    if (((row.CheckGet("ID_STATUS").ToInt() >= 27)  // возврат/хранение
                                    && (row.CheckGet("ID_STATUS").ToInt() <= 30))
                                    && (row.CheckGet("DT_RETURN").ToString().IsNullOrEmpty()))
                                    {
                                    fontWeight=FontWeights.Bold;
                                    }
                                    result=fontWeight;
                                    return result;
                                }
                            },
                     },
                 },
                 new DataGridHelperColumn
                 {
                     Header="Телефон",
                     Path="PHONE_NUMBER",
                     ColumnType=ColumnTypeRef.String,
                     Width2=10,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Поставщик/Покупатель",
                     Path="POST_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=30,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Приемки",
                     Path="NUM_AKT_STR",
                     Group="Акты",
                     ColumnType=ColumnTypeRef.String,
                     Width2=11,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Возврата",
                     Path="ACT_RETURN_NUM",
                     Group="Акты",
                     ColumnType=ColumnTypeRef.String,
                     Width2=7,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата регистрации",
                     Path="CREATED_DTTM",
                     Group="Дата",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 15,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Слот",
                     Path="DTTM_SLOT",
                     Group="Дата",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 15,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Полная",
                     Path="DT_FULL",
                     Group="Дата",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 15,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Пустая",
                     Path="DT_EMPTY",
                     Group="Дата",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 15,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="с грузом",
                     Path="WEIGHT_FULL",
                     Doc="Вес полная",
                     Group="Вес",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=7,
                     Visible=true,
                     Format="N0",
                     Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                     {
                         //{
                         //    StylerTypeRef.BackgroundColor, // фон
                         //    row => GetColorRolls("WEIGHT_FULL", row)
                         //},
                         {
                                DataGridHelperColumn.StylerTypeRef.ForegroundColor, // цвет шрифта
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    if ((row.CheckGet("AUTO_INPUT").ToInt() == 1) || (row.CheckGet("AUTO_INPUT").ToInt() == 3))
                                    {
                                        color = $"#FF0000"; // красный
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                            {
                                DataGridHelperColumn.StylerTypeRef.FontWeight, // размер шрифта
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var fontWeight= new FontWeight();
                                    if ((row.CheckGet("AUTO_INPUT").ToInt() == 1) || (row.CheckGet("AUTO_INPUT").ToInt() == 3))
                                    {
                                        fontWeight=FontWeights.Bold;
                                    }

                                    result=fontWeight;
                                    return result;
                                }
                            },
                     },
                 },
                 new DataGridHelperColumn
                 {
                     Header="пустой",
                     Path="WEIGHT_EMPTY",
                     Doc="Вес пустая",
                     Group="Вес",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=7,
                     Visible=true,
                     Format="N0",
                     Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                     {
                         //{
                         //    StylerTypeRef.BackgroundColor, // фон
                         //    row => GetColorRolls("WEIGHT_FULL", row)
                         //},
                         {
                                DataGridHelperColumn.StylerTypeRef.ForegroundColor, // цвет шрифта
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    if ((row.CheckGet("AUTO_INPUT").ToInt() == 2) || (row.CheckGet("AUTO_INPUT").ToInt() == 3))
                                    {
                                        color = $"#FF0000"; // красный
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                            {
                                DataGridHelperColumn.StylerTypeRef.FontWeight, // размер шрифта
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var fontWeight= new FontWeight();
                                    if ((row.CheckGet("AUTO_INPUT").ToInt() == 2) || (row.CheckGet("AUTO_INPUT").ToInt() == 3))
                                    {
                                        fontWeight=FontWeights.Bold;
                                    }

                                    result=fontWeight;
                                    return result;
                                }
                            },
                     },
                 },
                 new DataGridHelperColumn
                 {
                     Header="по документам",
                     Path="WEIGHT_DOK",
                     Doc="Вес по документам",
                     Group="Вес",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=7,
                     Visible=true,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="фактический",
                     Path="WEIGHT_FACT",
                     Doc="Вес фактический",
                     Group="Вес",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=7,
                     Visible=true,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="возврата/хранения",
                     Path="WEIGHT_RETURNING",
                     Doc="Вес возврата/хранения",
                     Group="Вес",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=7,
                     Visible=true,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="по документам",
                     Path="QUANTITY_BAL_DOC",
                     Doc="Кип по документам",
                     Group="Кип",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=7,
                     Visible=true,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="принято",
                     Path="QTY",
                     Doc="Кип принято",
                     Group="Кип",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=7,
                     Visible=true,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="возвращено/хранение",
                     Path="QTY_RETURNING",
                     Doc="Кип возвращено/хранение",
                     Group="Кип",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=6,
                     Visible=true,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ячейка",
                     Path="SKLAD",
                     ColumnType=ColumnTypeRef.String,
                     Width2=7,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Сотрудник",
                     Path="NAME_CS",
                     ColumnType=ColumnTypeRef.String,
                     Width2=16,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Водитель погрузчика",
                     Path="STAFF_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=16,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="ШК",
                     Path="BARCODE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=11,
                     Visible=true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="NNAKL",
                     Path="NNAKL",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=8,
                     Visible=Central.DebugMode,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="ID_POST",
                     Path="ID_POST",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=8,
                     Visible=Central.DebugMode,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="NSTHET",
                     Path="NSTHET",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=8,
                     Visible=Central.DebugMode,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="ID_STATUS",
                     Path="ID_STATUS",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=8,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="EMPL_ID",
                     Path="EMPL_ID",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=8,
                     Visible=Central.DebugMode,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="ID_CATEGORY",
                     Path="ID_CATEGORY",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=8,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="AUTO_INPUT",
                     Path="AUTO_INPUT",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=8,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="IS_ALARMED",
                     Path="IS_ALARMED",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=8,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="PAPER_TETRAPAK_FLAG",
                     Path="PAPER_TETRAPAK_FLAG",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=8,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="INPUT_CONTROL_FLAG",
                     Path="INPUT_CONTROL_FLAG",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=8,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="ID2",
                     Path="ID2",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=8,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="IS_RET",
                     Path="IS_RET",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=8,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="ORD",
                     Path="ORD",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="STATUS_RETURN",
                     Path="STATUS_RETURN",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=8,
                     Visible=Central.DebugMode,
                     Format="N0",
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата возврата",
                     Path="DT_RETURN",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 15,
                     Visible=Central.DebugMode,
                 },
                 new DataGridHelperColumn
                 {
                     Header="CRC",
                     Path="CRC",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=Central.DebugMode,
                 },

            };

            // раскраска всей строки
            ScrapTransportDeleteWeightGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                //{
                //    StylerTypeRef.BackgroundColor,
                //    row =>
                //    {
                //        var result=DependencyProperty.UnsetValue;
                //        var color = "";

                //        var days = row.CheckGet("DAYS").ToInt();

                //        if (days > 31) // прошло более 31 дня с момента разгрузки
                //        {
                //            color = "#FBC2C6"; // cветло-розовый  Background := RGB(251, 194, 198);
                //        }

                //        if (!string.IsNullOrEmpty(color))
                //        {
                //            result=color.ToBrush();
                //        }

                //        return result;
                //    }
                //},
                {
                       DataGridHelperColumn.StylerTypeRef.FontWeight, // размер шрифта
                       row =>
                       {
                           var result=DependencyProperty.UnsetValue;
                           var fontWeight= new FontWeight();
                           // машина не приехала
                           if (row.CheckGet("ID_STATUS").ToInt() == 66)
                           {
                               fontWeight=FontWeights.Bold;
                           }

                            result=fontWeight;
                            return result;
                       }
                },
                // определение цветов шрифта строк
                {
                    StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                            // разгружается
                            //if (row.ContainsKey("ORD"))
                            //{
                            //    if(row["ORD"].ToInt() == 1 || row["ORD"].ToInt() == 2)
                            //    {
                            //        color = HColor.BlackFG;
                            //    }
                            //}

                            //if (row.ContainsKey("ID_STATUS"))
                            //{
                            //    if ((row["ID_STATUS"].ToInt() >= 11)
                            //    && (row["ID_STATUS"].ToInt() <= 25)) // это Полиэтилен.
                            //    {
                            //       color = $"#0070C0"; // пурпупный фон;// HColor.Gray;
                            //    }
                            //}

                            if (row.ContainsKey("ID_STATUS"))
                            {
                                if (row["ID_STATUS"].ToInt() == 66) // машина не приехала
                                {
                                   color = $"#800000"; // коричнево-малиновый;// HColor.Gray;
                                }

                                if ((row["ID_STATUS"].ToInt() >= 27)
                                 && (row["ID_STATUS"].ToInt() <= 30)) // возврат/хранение
                                {
                                   color = $"#FF0000"; // светло-красный;// HColor.Gray;
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

            ScrapTransportDeleteWeightGrid.SetColumns(columns);
            ScrapTransportDeleteWeightGrid.SetPrimaryKey("NUM");

            ScrapTransportDeleteWeightGrid.SearchText = EditScrapTransportFilterSearchBox;
            //данные грида
            ScrapTransportDeleteWeightGrid.OnLoadItems = ScrapTransportDeleteWeightGridLoadItems;
            ScrapTransportDeleteWeightGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ScrapTransportDeleteWeightGrid.AutoUpdateInterval = 0;
            ScrapTransportDeleteWeightGrid.EnableSortingGrid = false;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            ScrapTransportDeleteWeightGrid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                }
            };

            // двойной клик по строке
            // Report1Grid.OnDblClick = EditShow;

            // фильтрация по гриду
            ScrapTransportDeleteWeightGrid.OnFilterItems = () =>
            {
                if (ScrapTransportDeleteWeightGrid.Items.Count > 0)
                {
                }
            };

            ScrapTransportDeleteWeightGrid.Commands = Commander;
            ScrapTransportDeleteWeightGrid.Init();
        }

        /// <summary>
        /// загрузка данных для удаленных машин 
        /// </summary>
        /// <param name="num"></param>
        public async void ScrapTransportDeleteWeightGridLoadItems()
        {
            bool resume = true;

            if (ScrapTransportAudDeleteCheckBox.IsChecked == false)
                return;

            if (resume)
            {
                var q = new LPackClientQuery();

                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_ST", IdSt.ToString());
                    p.CheckAdd("NUMBEROFWEEK", EditNumberOfWeek.Text);
                }

                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "ScrapPaper");
                q.Request.SetParam("Action", "ScrapTransportAudDelete");
                q.Request.SetParams(p);

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
                        var ds = ListDataSet.Create(result, "ITEMS");
                        ScrapTransportDeleteWeightGrid.UpdateItems(ds);
                        SetSplash(false);
                    }
                }
            }
        }


        /////////////////////////

        /// <summary>
        /// получаем список смен
        /// </summary>
        public void GetShiftList()
        {
            var p = new Dictionary<string, string>();
            p.Add("ID_ST", IdSt.ToString());

            var q = new LPackClientQuery();

            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "ScrapPaper");
            q.Request.SetParam("Action", "SmenaSelect");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    SmenaSelectBox.SetItems(ds, "SHIFT_ID", "SHIFT_NAME");

                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        string today = "";
                        if (DateTime.Now.Hour < 8)
                        {
                            today = $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")} 20:00:00";
                        }
                        else
                        {
                            if (DateTime.Now.Hour < 20)
                            {
                                today = $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00";
                            }
                            else
                            {
                                today = $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00";
                            }
                        }

                        var todayItem = ds.Items.FirstOrDefault(x => x.CheckGet("SHIFT_DTTM") == today);
                        if (todayItem != null)
                        {
                            SmenaSelectBox.SetSelectedItemByKey(todayItem.CheckGet("SHIFT_ID"));
                        }
                    }
                }
            }
        }

        /// <summary>
        ///  Разобрать потом
        /// </summary>

        #region Разобрать 
        private void ShiftSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }

        /// <summary>
        ///  Чек-бокс Все снят
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrapTransportWeightAllCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            //ScrapTransportReturningCheckBox.IsChecked = false;
            //ScrapTransportPolyAlCheckBox.IsChecked = false;
            //ScrapTransportChemistryCheckBox.IsChecked = false;
            //ScrapTransportDidntArriveCheckBox.IsChecked = false;

            ScrapTransportReturningCheckBox.IsEnabled = false;
            ScrapTransportPolyAlCheckBox.IsEnabled = false;
            ScrapTransportChemistryCheckBox.IsEnabled = false;
            ScrapTransportDidntArriveCheckBox.IsEnabled = false;

            SetSplash(true, "Загрузка данных");
            ScrapTransportCurSecund = 0;
            ScrapTransportWeightGridLoadItems();
            FastTimer.Start();
        }

        /// <summary>
        ///  Чек-бокс Все отмечен
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrapTransportWeightAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {

            ScrapTransportReturningCheckBox.IsEnabled = true;
            ScrapTransportPolyAlCheckBox.IsEnabled = true;
            ScrapTransportChemistryCheckBox.IsEnabled = true;
            ScrapTransportDidntArriveCheckBox.IsEnabled = true;

            SetSplash(true, "Загрузка данных");
            ScrapTransportCurSecund = 0;
            ScrapTransportWeightGridLoadItems();
            FastTimer.Start();
        }

        private void ScrapTransportPolyAlCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ScrapTransportWeightGrid.UpdateItems();
        }

        private void ScrapTransportPolyAlCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ScrapTransportWeightGrid.UpdateItems();
        }

        private void ScrapTransportReturningCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ScrapTransportWeightGrid.UpdateItems();
        }

        private void ScrapTransportReturningCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ScrapTransportWeightGrid.UpdateItems();
        }

        private void ScrapTransportChemistryCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ScrapTransportWeightGrid.UpdateItems();
        }

        private void ScrapTransportChemistryCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ScrapTransportWeightGrid.UpdateItems();
        }

        private void ScrapTransportDidntArriveCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ScrapTransportWeightGrid.UpdateItems();
        }

        private void ScrapTransportDidntArriveCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ScrapTransportWeightGrid.UpdateItems();
        }

        /// <summary>
        /// показать список удаленных контролером машин 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrapTransportAudDeleteCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (ScrapTransportAudDeleteCheckBox.IsChecked == true)
            {
                ScrapTransportWeightGrid.Visibility = Visibility.Hidden;

                ScrapTransportWeightAllCheckBox.IsEnabled = false;
                ScrapTransportReturningCheckBox.IsEnabled = false;
                ScrapTransportPolyAlCheckBox.IsEnabled = false;
                ScrapTransportChemistryCheckBox.IsEnabled = false;
                ScrapTransportDidntArriveCheckBox.IsEnabled = false;

                // доступность кнопок
                ScrapTransportAtrrEditButton.IsEnabled = false;
                AddTerminalButton.IsEnabled = false;
                ScrapMoveToBdm1Button.IsEnabled = false;
                MoveToCastContainerButton.IsEnabled = false;
                ScrapTransportScanBarcodeButton.IsEnabled = false;
                ScrapTransportPopupMenuButton.IsEnabled = false;
                ScrapTransportEditButton.IsEnabled = false;
                ScrapTransportFileButton.IsEnabled = false;

                ScrapTransportDeleteWeightGrid.Visibility = Visibility.Visible;
                SetSplash(true, "Загрузка данных");
                ScrapTransportDeleteWeightGridLoadItems();
                //фокус на текущий грид
                ScrapTransportDeleteWeightGrid.Focus();
            }
        }

        private void ScrapTransportAudDeleteCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ScrapTransportAudDeleteCheckBox.IsChecked == false)
            {
                ScrapTransportDeleteWeightGrid.Visibility = Visibility.Hidden;

                ScrapTransportWeightAllCheckBox.IsEnabled = true;
                ScrapTransportReturningCheckBox.IsEnabled = true;
                ScrapTransportPolyAlCheckBox.IsEnabled = true;
                ScrapTransportChemistryCheckBox.IsEnabled = true;
                ScrapTransportDidntArriveCheckBox.IsEnabled = true;

                // доступность кнопок
                ScrapTransportAtrrEditButton.IsEnabled = true;
                AddTerminalButton.IsEnabled = true;
                ScrapMoveToBdm1Button.IsEnabled = true;
                MoveToCastContainerButton.IsEnabled = true;
                ScrapTransportScanBarcodeButton.IsEnabled = true;
                ScrapTransportPopupMenuButton.IsEnabled = true;
                ScrapTransportEditButton.IsEnabled = true;
                ScrapTransportFileButton.IsEnabled = true;

                ScrapTransportWeightGrid.Visibility = Visibility.Visible;
                SetSplash(true, "Загрузка данных");
                ScrapTransportWeightGridLoadItems();
                //фокус на текущий грид
                ScrapTransportWeightGrid.Focus();
            }
        }

        private void ControlPostavshicCategoryCheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ControlPostavshicCategoryCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void WastePaperCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!RunFirst)
                ScrapPzGrid.UpdateItems();
        }

        private void WastePaperCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ScrapPzGrid.UpdateItems();
        }

        private void TetraPakBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!RunFirst)
                ScrapPzGrid.UpdateItems();
        }

        private void TetraPakBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ScrapPzGrid.UpdateItems();
        }

        private void SmenaSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScrapPzGrid.LoadItems();
        }

        #endregion

        /// <summary>
        /// Возвращает цвет  ячейки
        /// </summary>
        public static object GetColorRolls(string fieldName, Dictionary<string, string> row)
        {
            var result = DependencyProperty.UnsetValue;
            var color = "";

            // раскраска грида машин на весовой
            if (fieldName == "NAME")
            {
                //  разгружается
                if ((row.CheckGet("ORD").ToInt() == 1)
                || (row.CheckGet("ORD").ToInt() == 2))
                {
                    color = $"#99FF99"; // зеленый
                }
                else
                //  направляется к разгрузке
                if (row.CheckGet("ORD").ToInt() == 3)
                {
                    color = $"#FF9428"; // оранжевый фон
                }
                else
                //  ожидает, не привязан к терминалу
                if (row.CheckGet("ORD").ToInt() == 4)
                {
                    color = $"#FFFF99"; // желтый фон
                }

                // разгрузка закончена /Полиэтилен. Загрузка окончена
                if ((row.CheckGet("ID_STATUS").ToInt() == 4)
                || (row.CheckGet("ID_STATUS").ToInt() == 6)
                || (row.CheckGet("ID_STATUS").ToInt() == 19)
                || (row.CheckGet("ID_STATUS").ToInt() == 44))
                {
                    color = $"#FFCCFF"; // розовый фон
                }

                // разгрузка начата
                if ((row.CheckGet("ID_STATUS").ToInt() == 3)
                || (row.CheckGet("ID_STATUS").ToInt() == 16)
                || (row.CheckGet("ID_STATUS").ToInt() == 43))
                {
                    color = $"#99FF99"; // лайм фон
                }

                // Полиэтилен. Взвесилась полная
                if (row.CheckGet("ID_STATUS").ToInt() == 22)
                {
                    color = $"#ADD8E6"; // светло-голубой 
                }

                // загрузка закончена (отложена машина)
                if (row.CheckGet("ID_STATUS").ToInt() == 6)
                {
                    color = $"#8080C0"; // сине-фиолетовый 
                }
            }

            // раскраска грида терминалов
            if (fieldName == "NAME2")
            {
                // разгружается/загружается/химия
                if ((row.CheckGet("ID_STATUS").ToInt() == 2)
                //    || (row.CheckGet("ID_STATUS").ToInt() == 3)
                //    || (row.CheckGet("ID_STATUS").ToInt() == 4)
                || (row.CheckGet("ID_STATUS").ToInt() == 13)
                //    || (row.CheckGet("ID_STATUS").ToInt() == 16)
                //    || (row.CheckGet("ID_STATUS").ToInt() == 19)
                || (row.CheckGet("ID_STATUS").ToInt() == 22)
                || (row.CheckGet("ID_STATUS").ToInt() == 42)
                //  || (row.CheckGet("ID_STATUS").ToInt() == 43)
                //  || (row.CheckGet("ID_STATUS").ToInt() == 44)
                )
                {
                    color = $"#99FF99"; // зеленый фон
                }
                else
                // привязан к терминалу и направляется к разгрузке/Полиэтилен. Взвесилась пустая
                if ((row.CheckGet("ID_STATUS").ToInt() == 1)
                || (row.CheckGet("ID_STATUS").ToInt() == 11)
                || (row.CheckGet("ID_STATUS").ToInt() == 41)
                && (row.CheckGet("SCTE_ID").ToInt() > 0))
                {
                    color = $"#FF9428"; // оранжевый фон                    
                }
                else
                // ожидает, не привязан к терминалу
                if ((row.CheckGet("ID_STATUS").ToInt() == 1)
                || (row.CheckGet("ID_STATUS").ToInt() == 11)
                || (row.CheckGet("ID_STATUS").ToInt() == 41)
                && (row.CheckGet("SCTE_ID").ToInt() == 0))
                {
                    color = $"#00FF00"; // салатовый фон
                }

                // разгрузка закончена /Полиэтилен. Загрузка окончена
                if ((row.CheckGet("ID_STATUS").ToInt() == 4)
                || (row.CheckGet("ID_STATUS").ToInt() == 19)
                || (row.CheckGet("ID_STATUS").ToInt() == 44))
                {
                    color = $"#FFCCFF"; // светло-фиолетовый
                }

                // разгрузка начата
                if ((row.CheckGet("ID_STATUS").ToInt() == 3)
                || (row.CheckGet("ID_STATUS").ToInt() == 16)
                || (row.CheckGet("ID_STATUS").ToInt() == 43))
                {
                    color = $"#00FF00"; // салатовый
                }

                // Полиэтилен. Взвесилась полная
                if (row.CheckGet("ID_STATUS").ToInt() == 22)
                {
                    color = $"#FFCC99"; // светло-синий
                }
            }

            // раскраска грида в работе и задания на макулатуру
            if (fieldName == "NAME3")
            {
                switch (row.CheckGet("ID_CATEGORY").ToInt())
                {
                    case 1:
                        color = $"#008000"; // зеленый
                        break;
                    case 2:
                        color = $"#00FFFF"; // аква
                        break;
                    case 3:
                        color = $"#C4374B"; // красный
                        break;
                    case 21:
                        color = $"#800080"; // фиолетовый
                        break;
                    case 22:
                        color = $"#0000FF"; // синий
                        break;
                    case 23:
                        color = $"#FF00FF"; // фуксия
                        break;
                    case 31:
                        color = $"#00FF00"; // ярко-зеленый
                        break;
                    case 32:
                        color = $"#66FF99"; // моркской зеленый
                        break;
                    case 33:
                        color = $"#00FFFF"; // аква
                        break;
                    default:
                        break;
                }
            }

            // раскраска грида истории изменения композиции
            if (fieldName == "NOTE")
            {
                if (row.CheckGet("NUM").ToInt() == 1)
                {
                    color = $"#99FF99"; // зеленый фон
                }
            }

            if (fieldName == "IS_SCRAP_ATTR")
            {
                // машина с тетрапак
                if (row.CheckGet("PAPER_TETRAPAK_FLAG").ToInt() == 1)
                {
                    // не полное описание машины
                    if ((row.CheckGet("CRC").ToInt() > 0) && (row.CheckGet("CRC").ToInt() < 54))
                    {
                        color = $"#FF0000"; // красный фон                    
                    }
                }
                else
                // машина с макулатурой
                if ((row.CheckGet("PAPER_TETRAPAK_FLAG").ToInt() == 0) || (row.CheckGet("PAPER_TETRAPAK_FLAG").ToInt() == 24))
                {
                    // не полное описание машины
                    if ((row.CheckGet("CRC").ToInt() > 0) && (row.CheckGet("CRC").ToInt() < 24))
                    {
                        color = $"#FF0000"; // красный фон                    
                    }
                }
            }

            if (fieldName == "STATUS_NAME")
            {
                if (((row.CheckGet("ID_STATUS").ToInt() >= 27)  // возврат/хранение
                && (row.CheckGet("ID_STATUS").ToInt() <= 30))
                && (row.CheckGet("DT_RETURN").ToString().IsNullOrEmpty()))
                {
                    color = $"#FFFF99"; // желтый фон
                }
            }

            if (!color.IsNullOrEmpty())
            {
                result = color.ToBrush();
            }

            return result;
        }

        /// <summary>
        ///  отображение окна долгих операций
        /// </summary>
        /// <param name="inProgressFlag"></param>
        /// <param name="msg"></param>
        private void SetSplash(bool inProgressFlag, string msg = "")
        {
            SplashControl.Visible = inProgressFlag;
            SplashControl.Message = msg;
        }

        /// <summary>
        ///  Нажали кнопку "Обновить" весовая
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrapTransportRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Refresh();

        }

        /// <summary>
        /// обновить все данные (весовая, терминалы, в работе)
        /// </summary>
        private void Refresh()
        {
            SetSplash(true, "Загрузка данных");
            ScrapTransportCurSecund = 0;
            if (ScrapTransportAudDeleteCheckBox.IsChecked == false)
            {
                ScrapTransportWeightGridLoadItems();
            }
            else
            {
                ScrapTransportDeleteWeightGridLoadItems();
            }

            ScrpTerminalGridLoadItems();
            ScrapCurrentGridLoadItems();

            FastTimer.Start();
        }

        /// <summary>
        /// Нажали кнопку "Обновить" задание на макулатуру
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrapPzlRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshScrapPz();
        }

        /// <summary>
        ///  Обновляем задания на макулатуру и историю композиций
        /// </summary>
        private void RefreshScrapPz()
        {
            if (IdSt != 2716)
            {
                ScrapPzCurSecund = 0;
                ScrapPzGridLoadItems();
                FastTimer.Start();
            }
        }

        /// <summary>
        /// Таймер частого обновления (1 секунда)
        /// </summary>
        private void SetFastTimer(int autoUpdateInterval)
        {
            FastTimer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, autoUpdateInterval)
            };

            FastTimer.Tick += (s, e) =>
            {
                {
                    RefreshButtonUpdate();
                }
            };

            FastTimer.Start();
        }

        /// <summary>
        /// обновляем время на кнопках до обновления информации
        /// </summary>
        private void RefreshButtonUpdate()
        {
            if (ScrapTransportCurSecund >= ScrapTransportRefreshTime)
            {
                ScrapTransportCurSecund = 0;
                ScrapTransportWeightGridLoadItems();
                ScrpTerminalGridLoadItems();
                ScrapCurrentGridLoadItems();
            }

            if (ScrapPzCurSecund >= ScrapPzRefreshTime)
            {
                ScrapPzCurSecund = 0;
                ScrapPzGridLoadItems();
            }

            ScrapTransportCurSecund++;
            ScrapPzCurSecund++;
            int secondsBeforeFirstUpdate = ScrapTransportRefreshTime - ScrapTransportCurSecund;
            int secondsBeforeFirstUpdate2 = ScrapPzRefreshTime - ScrapPzCurSecund;
            ScrapTransportRefreshButton.Content = $"Обновить {secondsBeforeFirstUpdate}";
            ScrapPzlRefreshButton.Content = $"Обновить {secondsBeforeFirstUpdate2}";
        }

        /// <summary>
        /// открываем/закрываем терминал 
        /// </summary>
        /// <param name="row"></param>
        private async void EditTerminal(Dictionary<string, string> row)
        {
            int checking = row.CheckGet("WORKING_FLAG").ToInt();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "ScrapPaper");
            q.Request.SetParam("Action", "ScrpTerminalUpdateIdWorkingFlag");
            q.Request.SetParam("SCTE_ID", row.CheckGet("SCTE_ID").ToInt().ToString());
            q.Request.SetParam("WORKING_FLAG", checking == 0 ? "1" : "0");

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
                        ScrpTerminalGrid.LoadItems();
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        ///  обработчик кнопок и контекстного меню грида машин на весовой
        /// </summary>
        /// <param name="command"></param>
        /// <param name="m"></param>
        public void ProcessCommand(string command, ItemMessage m = null)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    // информация по машине
                    case "info_scrap":
                        {
                            var r = ScrapTransportWeightGrid.GetItemsSelected();
                            var s = Tools.DumpListSimple(r);
                            var d = new LogWindow($"{s}", "Информация о машине");
                            d.ShowDialog();
                        }
                        break;
                    // удалить машину
                    case "scrap_transport_delete":
                        {
                            //  AccountGrid.SelectAllRows(false);
                        }
                        break;
                    // удалить приход машины
                    case "scrap_transport_prihod_clear":
                        {
                            //  AccountCreate();
                        }
                        break;
                    // 1. Откат прихода машины
                    case "scrap_transport_prihod_otkat":
                        {
                            // AccountEdit();
                        }
                        break;
                    // Проведение прихода машины
                    case "scrap_transport_prihod_provedeno":
                        {
                            //AccountLock(1);
                        }
                        break;
                    // Исправить номер Акта
                    case "scrap_transport_akt_change":
                        {
                            //AccountLock(1);
                        }
                        break;
                    // История изменений
                    case "scrap_transport_histoty":
                        {

                        }
                        break;
                    // Химия. Разгрузка начата
                    case "scrap_transport_set_status_43":
                        {

                        }
                        break;
                    // Химия. Разгрузка закончена
                    case "scrap_transport_set_status_44":
                        {

                        }
                        break;
                    // Направить на осевое взвешивание (БДМ1)
                    case "chckpnt_car_add":
                        {

                        }
                        break;
                    // Машина не приехала
                    case "scrap_transport_didnt_arrive":
                        {

                        }
                        break;
                    // экспорт в Excel
                    case "grid_export":
                        {
                            ScrapTransportWeightGrid.ItemsExportExcel();
                        }
                        break;

                    // выбор типа машины для ручного добавления (привез макулатуру или приехал за ПЭС )
                    case "scrap_transport_popup_menu":
                        {
                            ScrapTransportAdd();
                        }
                        break;
                    // карточка машины
                    case "scrap_transport_edit":
                        {
                            ScrapTransportEdit();
                        }
                        break;
                    // описание машины
                    case "scrap_transport_atrr_edit":
                        {
                            ScrapTransportAttrEdit();
                        }
                        break;

                    // описание композиции
                    case "scrap_pz_note_edit":
                        {
                            ScrapPzNoteEdit();
                        }
                        break;
                    // настройки программы
                    case "setup":
                        {
                            Setup();
                        }
                        break;






                }
            }
        }

        /// <summary>
        /// получение настроек программы
        /// </summary>
        public async void GetSetupData()
        {
            bool resume = true;
            KolBalePolyal = 0;
            ControlPostavshicFlag = false;
            Bdm1ComPort = "";
            Bdm2ComPort = "";
            Bdm1LaurentIp = "";
            Bdm2LaurentIp = "";

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "ScrapPaper");
                q.Request.SetParam("Action", "ConfigurationOptionsGet");

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        foreach (Dictionary<string, string> row in ds.Items)
                        {
                            var val = row.CheckGet("PARAM_VALUE").ToString();
                            var id = row.CheckGet("COOP_ID").ToInt();

                            switch (id)
                            {
                                case 108:
                                    KolBalePolyal = val.ToInt();
                                    break;
                                case 118:
                                    Bdm1ComPort = val;
                                    break;
                                case 119:
                                    Bdm2ComPort = val;
                                    break;
                                case 120:
                                    Bdm1LaurentIp = val;
                                    break;
                                case 121:
                                    Bdm2LaurentIp = val;
                                    break;
                                case 242:
                                    ControlPostavshicFlag = val.ToBool();
                                    break;
                                default:
                                    break;
                            }
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
        /// изменяем настройки программы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Setup()
        {
            var scrapRecord = new ScrapPaperConfiguration();
            scrapRecord.ReceiverName = ControlName;
            scrapRecord.Edit();
        }

        /// <summary>
        ///  Редактируем композицию
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrapPzNoteEdit()
        {
            var scrapPzNoteRecord = new ScrapPzNote(ScrapPzSelectedItem as Dictionary<string, string>);
            scrapPzNoteRecord.ReceiverName = ControlName;
            scrapPzNoteRecord.Edit();
        }

        /// <summary>
        /// редактирование описания качества кип из машины 
        /// </summary>
        private void ScrapTransportAttrEdit()
        {
            // 0 - обычное описание качества кип, 1 - возвратные кипы
            var scrapTransportAttrNewRecord = new ScrapTransportAttrNew(ScrapTransportWeightSelectedItem as Dictionary<string, string>, 0);
            scrapTransportAttrNewRecord.ReceiverName = ControlName;
            scrapTransportAttrNewRecord.Edit();
        }

        /// <summary>
        /// редактирование данных машины (макулатура)
        /// </summary>
        private void ScrapTransportEdit()
        {

            var scrapTransportRecord = new ScrapTransport(ScrapTransportWeightSelectedItem as Dictionary<string, string>);
            scrapTransportRecord.ReceiverName = ControlName;
            scrapTransportRecord.ControlPostavshicFlag = ControlPostavshicFlag;
            scrapTransportRecord.Bdm1ComPort = Bdm1ComPort;
            scrapTransportRecord.Bdm2ComPort = Bdm2ComPort;
            scrapTransportRecord.Bdm1LaurentIp = Bdm1LaurentIp;
            scrapTransportRecord.Bdm2LaurentIp = Bdm2LaurentIp;
            scrapTransportRecord.WeightOpenFlag = false;
            scrapTransportRecord.ClearTabloBdm1Flag = false;
            scrapTransportRecord.IdSt = IdSt;

            scrapTransportRecord.Edit();
        }


        /// <summary>
        /// выбор типа машины для ручного добавления (привез макулатуру или приехал за ПЭС )
        /// </summary>
        private void ScrapTransportAdd()
        {
            if (IdSt == 716)
                ScrapTransportPolyalAddButton.IsEnabled = false;
            else
                ScrapTransportPolyalAddButton.IsEnabled = true;

            BurgerMenu.IsOpen = true;
        }

        /// <summary>
        /// добавляем вручную машину с макулатурой
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrapTransportAddButton_Click(object sender, RoutedEventArgs e)
        {
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("", "");
            }

            var scrapTransportRecord = new ScrapTransport(p);
            scrapTransportRecord.ReceiverName = ControlName;
            scrapTransportRecord.ControlPostavshicFlag = ControlPostavshicFlag;
            scrapTransportRecord.Bdm1ComPort = Bdm1ComPort;
            scrapTransportRecord.Bdm2ComPort = Bdm2ComPort;
            scrapTransportRecord.Bdm1LaurentIp = Bdm1LaurentIp;
            scrapTransportRecord.Bdm2LaurentIp = Bdm2LaurentIp;
            scrapTransportRecord.WeightOpenFlag = false;
            scrapTransportRecord.ClearTabloBdm1Flag = false;
            scrapTransportRecord.IdSt = IdSt;

            scrapTransportRecord.Edit();

        }

        /// <summary>
        /// добавляем вручную машину за ПЭС
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrapTransportPolyalAddButton_Click(object sender, RoutedEventArgs e)
        {

        }












        /////////////////////////
    }
}
