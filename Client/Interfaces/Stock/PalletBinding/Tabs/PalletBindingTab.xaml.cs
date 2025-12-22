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
using System.Windows.Forms;
using NPOI.SS.Formula.Functions;

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

            RoleName = "[erp]pallet_binding";
            ControlTitle = "Изделия на складе";
            DocumentationUrl = "/doc/l-pack-erp/warehouse";

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (System.Windows.Input.KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };

            OnLoad = () =>
            {
                PalletGridInit();     
                SetDefaults();        
            };

            OnUnload = () =>
            {
                PalletGrid.Destruct();
            };
 
            OnFocusGot = () =>
            {
                PalletGrid.ItemsAutoUpdate = true;
                PalletGrid.Run();
            };

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
                                i.SetParams(PalletGrid.SelectedItem["FACT_ID"].ToInt(), 
                                    PalletGrid.SelectedItem["ID_TOVAR"].ToInt(), 
                                    PalletGrid.SelectedItem["ID_PZ"].ToInt(), 
                                    PalletGrid.SelectedItem["NUM"].ToInt(),
                                    PalletGrid.SelectedItem["IDORDERDATES"].ToInt());

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
                                Untie();
                            },
                        });
                    }
                }
            }
            Commander.Init(this);
        }

        public FormHelper Form {  get; set; }

        /// <summary>
        /// Настраивает Grid для отображения списка изделий на складе.
        /// Отображает колонки, оформление строк(цвет)
        /// </summary>
        private void PalletGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="CHECKING",   
                    ColumnType=ColumnTypeRef.Boolean, 
                    Editable=true, 
                    Width2=4, 
                },
                new DataGridHelperColumn
                {
                    Header="ИД Поддона",
                    Path="PALLET_ID", 
                    Description = "ИД поддона",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="PRODUCT_CODE",  
                    Description="Артикул продукции на поддоне",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="PRODUCT_NAME",
                    Description="Наименование продукции на поддоне",
                    ColumnType=ColumnTypeRef.String,
                    Width2=50,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="KOL",
                    Description="кол-во продукции на поддоне",
                    ColumnType=ColumnTypeRef.Double,
                    Format = "N0",
                    Width2=12,
                },
                 new DataGridHelperColumn
                {
                    Header="Поддон",
                    Path="PZ_NUM",
                    Description="номер поддона(№ пз + № поддона",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                 new DataGridHelperColumn
                {
                    Header="Место",
                    Path="PLACE",
                    Description="место хранения поддона",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                 //Заявка
                 new DataGridHelperColumn
                {
                    Header="Отгрузка",
                    Path="ORDER_DATA",
                    Description="информация по отгрузке",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                    Group = "Заявка",
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                       if (!row["IDORDERDATES_PZ"].IsNullOrEmpty() && 
                                            !row["IDORDERDATES"].IsNullOrEmpty() && 
                                            row["IDORDERDATES_PZ"] != row["IDORDERDATES"])
                                        {

                                            color = HColor.Yellow;
                                        }
                                 
                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                },
                 new DataGridHelperColumn
                {
                    Header="ИД позиции заявки",
                    Path="IDORDERDATES",
                    Description="ИД позиции заявки",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=12,
                    Group = "Заявка"
                },
                 new DataGridHelperColumn
                {
                    Header="ИД отгрузки",
                    Path="IDTS",
                    Description="№ отгрузки",
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
                    ColumnType=ColumnTypeRef.String,
                    Width2=40,
                    Group = "ПЗ"
                },
                 new DataGridHelperColumn
                {
                    Header="ИД позиции заявки",
                    Path="IDORDERDATES_PZ",
                    Description="Ид заявки по производственному заданию",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=12,
                    Group = "ПЗ"
                },
                 new DataGridHelperColumn
                {
                    Header="OD_C",
                    Path="OD_C",
                    Description="кол-во подходящих заявок для поддона",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                 new DataGridHelperColumn
                {
                    Header="SHIPPED",
                    Path="SHIPPED",
                    Description="Отгруженный поддон",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                 new DataGridHelperColumn
                {
                    Header="ID_PZ",
                    Path="ID_PZ",
                    Description="ИД производственного задания",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                 new DataGridHelperColumn
                {
                    Header="NUM",
                    Path="NUM",
                    Description="номер поддона",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=4,
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
                    Description="Площадка",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                 new DataGridHelperColumn
                {
                    Header="DTTM",
                    Path="DTTM",
                    Description="Указание даты и времени",
                    ColumnType=ColumnTypeRef.DateTime,
                    Format = "dd.MM.yyyy HH:mm:ss",
                    Width2=17,
                },
            };


            ///<summary>
            /// Покраска в нужный цвет     
            ///</summary>
            PalletGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        if (row != null && row.Count > 0)
                        {
                            if (!row["DTTM"].IsNullOrEmpty() && 
                                (row["SHIPPED"].ToInt() == 0 || 
                                 row["OD_C"].ToInt() == 0) && 
                                 row["DTTM"].ToDateTime("dd.MM.yyyy HH:mm:ss") < DateTime.Now)
                        {
                            color = HColor.Yellow;
                        }

                        if (!row["DTTM"].IsNullOrEmpty() && 
                             row["SHIPPED"].ToInt() == 0 && 
                             row["DTTM"].ToDateTime("dd.MM.yyyy HH:mm:ss") >= DateTime.Now)
                        {
                            color = HColor.Green;
                        }

                        if (row["DTTM"].IsNullOrEmpty() && 
                            row["SHIPPED"].ToInt() == 1 && 
                            row["OD_C"].ToInt() == 0)
                        {
                            color = HColor.Blue;
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
            PalletGrid.SetColumns(columns); 
            PalletGrid.SetPrimaryKey("PALLET_ID"); 
            PalletGrid.SetSorting("PALLET_ID", ListSortDirection.Ascending); 
            PalletGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact; 
            PalletGrid.SearchText = PalletGridSearch; 
            PalletGrid.Toolbar = PalletGridToolbar; 

            ///<summary>
            /// Как загружать данные (запрос)
            ///</summary>
            PalletGrid.QueryLoadItems = new RequestData()
            {
                Module = "Stock", 
                Object = "PalletBinding",
                Action = "List",
                AnswerSectionKey = "PALLETS",
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                            {
                                { "FACTORY_ID", PlatformSelectBox.SelectedItem.Key},

                            };
                },
            };

            ///<summary>
            /// Фильтрация элементов
            ///</summary>
            PalletGrid.OnFilterItems = () =>
            {
                if (PalletGrid.Items.Count > 0)
                {
                    
                    var palletType = PalletSelectBox.SelectedItem.Key.ToInt();

                    var items = new List<Dictionary<string, string>>();
                    foreach (Dictionary<string, string> row in PalletGrid.Items)
                    {
                        bool include = false;

                        switch (palletType)
                        {
                            //Привязанные поддоны
                            case 1:
                                {
                                    if (!row["DTTM"].IsNullOrEmpty() && 
                                         row["SHIPPED"].ToInt() == 0 &&
                                         row["DTTM"].ToDateTime("dd.MM.yyyy HH:mm:ss") >= DateTime.Now)
                                    {
                                        include = true;
                                    }
                                }
                                break;

                            //Непривязанные
                            case 2:
                                {
                                    if ((row["DTTM"].IsNullOrEmpty() ||
                                        row["SHIPPED"].ToInt() == 1) &&
                                        row["OD_C"].ToInt() == 0)
                                    {
                                        include = true;
                                    }
                                }
                                break;

                            //Для привязывания
                            case 3:
                                {
                                    if ((row["DTTM"].IsNullOrEmpty() ||
                                        row["SHIPPED"].ToInt() == 1) &&
                                        row["OD_C"].ToInt() > 0)
                                    {
                                        include = true;
                                    }
                                }
                                break;

                            //Для отвязывания
                            case 4:
                                {
                                    if (!row["DTTM"].IsNullOrEmpty() && 
                                       (row["OD_C"].ToInt() == 0 || row["SHIPPED"].ToInt() == 0) && 
                                        row["DTTM"].ToDateTime("dd.MM.yyyy HH:mm:ss") > DateTime.Now)
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
            PalletGrid.Commands = Commander; 
            PalletGrid.Init();
        }

        public void SetDefaults()
        {
            PlatformSelectBox.SetItems(new Dictionary<string, string>()
            {
                {"1",  "Липецк"},
                {"2",  "Кашира"},
            });
            PlatformSelectBox.SelectedItem = PlatformSelectBox.Items.First();

            PalletSelectBox.SetItems(new Dictionary<string, string>()
            {
                {"0", "Все"},
                {"1", "Привязанные"},
                {"2", "Непривязанные"},
                {"3", "Для привязывания"},
                {"4", "Для отвязывания"},
            });
            PalletSelectBox.SelectedItem = PalletSelectBox.Items.First();
        }

        /// <summary>
        /// Отвязка поддона
        /// </summary>
        public void Untie()
        {
            if (PalletGrid.SelectedItem != null && PalletGrid.SelectedItem.Count > 0)
            {
                var d = new DialogWindow("Вы действительно хотите отвязать отгрузку от поддона?", "Отвязка поддона", "", DialogWindowButtons.YesNo);

                if (d.ShowDialog() == true)
                {
                    var p = new Dictionary<string, string>();
                    {
                        p.Add("ORDER_ID", "0");
                        p.Add("PRODUCT_ID", PalletGrid.SelectedItem["ID_TOVAR"]);
                        p.Add("PRODUCTION_TASK_ID", PalletGrid.SelectedItem["ID_PZ"]);
                        p.Add("PALLET_NUMBER", PalletGrid.SelectedItem["NUM"]);
                    }

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Stock");
                    q.Request.SetParam("Object", "PalletBinding");
                    q.Request.SetParam("Action", "Save");
                    q.Request.SetParams(p);

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        PalletGrid.LoadItems();
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }
            else
            {
                var d = new DialogWindow("Не выбрана поддон для отвязки", "Ошибка", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private void PlatformSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PalletGrid.LoadItems();
        }

        private void PalletSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PalletGrid.UpdateItems();
        }
    }
}
