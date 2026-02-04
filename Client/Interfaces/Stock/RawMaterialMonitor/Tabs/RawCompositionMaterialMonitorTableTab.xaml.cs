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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Остаток по сырьевым композициям на складе
    /// в табличном виде
    /// </summary>
    /// <author>kurasov_dp</author>
    public partial class RawCompositionMaterialMonitorTableTab : ControlBase
    {
        public RawCompositionMaterialMonitorTableTab()
        {
            InitializeComponent();

            RoleName = "[erp]raw_material_monitor";
            ControlTitle = "Монитор остатков сырья";
            DocumentationUrl = "/doc/l-pack-erp";

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
                SetDefaults();
                CompositionTableGridInit();
            };

            OnUnload = () =>
            {
                CompositionTableGrid.Destruct();
            };

            OnFocusGot = () =>
            {
                CompositionTableGrid.ItemsAutoUpdate = true;
                CompositionTableGrid.Run();
            };

            OnFocusLost = () =>
            {
                CompositionTableGrid.ItemsAutoUpdate = false;
            };

            ///<summary>
            /// Система команд (Commander)
            ///</summary>
            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "help",
                        Enabled = true,
                        Title = "Справка",
                        Description = "Показать справочную информацию",
                        MenuUse = true,
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
                        Name = "export_to_excel",
                        Group = "main",
                        Enabled = true,
                        Title = "В Excel",
                        Description = "Выгрузить данные в Excel файл",
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "ExcelButton",
                        AccessLevel = Common.Role.AccessMode.ReadOnly,
                        Action = () =>
                        {
                            CompositionTableGrid.ItemsExportExcel();
                        },
                    });
                }
            }
            Commander.Init(this);
        }

        /// <summary>
        /// Настраивает Grid для отображения.
        /// Отображает колонки, оформление строк(цвет)
        /// </summary>
        private void CompositionTableGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Hidden= true,
                    Header="",
                    Path="IDC",
                    Description="ИД Картона",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Картон",
                    Path="CARTON_NAME",
                    Description="Имя картона/бумаги",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Слои",
                    Path="LAYER_NUMBER",
                    Description="Слои",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                 new DataGridHelperColumn
                {
                    Header="Сырьевая группа",
                    Path="RAW_GROUP",
                    Description="Сырьевая группа",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                 new DataGridHelperColumn
                {
                    Header="Формат",
                    Path="WIDTH",
                    Description="Формат сырья",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Остаток",
                    Path="STOCK_KG",
                    Description="Остаток на складе по сырьевой композиции",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=14,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                       if (row["STOCK_KG"].ToInt() == 0)
                                        {
                                            color = HColor.Red;
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
            };

            ///<summary>
            /// Привязка колонок и базовые настройки сетки
            ///</summary> 
            CompositionTableGrid.SetColumns(columns);
            CompositionTableGrid.SetPrimaryKey("_ROWNUMBER");
            CompositionTableGrid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            CompositionTableGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            CompositionTableGrid.Toolbar = CompositionTableGridToolbar;

            ///<summary>
            /// Как загружать данные (запрос)
            ///</summary>
            CompositionTableGrid.QueryLoadItems = new RequestData()
            {
                Module = "Stock",
                Object = "RawMaterialResidueMonitor",
                Action = "RawCompositionList",
                AnswerSectionKey = "ITEMS", // имя ключа для ответа от сервера
                Timeout = 60000,
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                            {
                                { "FACTORY_ID", PlatformSelectBox.SelectedItem.Key},

                            };
                },

                AfterRequest = (RequestData rd, ListDataSet ds) =>
                {
                    var formatList = new Dictionary<string, string>(); //заполнение уникальными форматами
                    formatList.Add("0", "Все форматы");
                    foreach (var item in ds.Items) //проходим по всему запросу и выполняем поиск по уникальным форматам
                    {
                        if (!formatList.ContainsKey(item.CheckGet("WIDTH")))
                        {
                            formatList.Add(item.CheckGet("WIDTH"), item.CheckGet("WIDTH"));
                        }
                    }
                    FormatSelectBox.SetItems(formatList);
                    FormatSelectBox.SetSelectedItemByKey("0");
                    return ds;

                }
            };


            ///<summary>
            /// Фильтрация элементов
            ///</summary>
            CompositionTableGrid.OnFilterItems = () =>
            {
                if (CompositionTableGrid.Items.Count > 0)
                {

                    var format = FormatSelectBox.SelectedItem.Key;

                    var items = new List<Dictionary<string, string>>();
                    if (format == "0")
                    {
                        items = CompositionTableGrid.Items;
                    }
                    else
                    {
                        foreach (Dictionary<string, string> row in CompositionTableGrid.Items)
                        {
                            bool include = false;

                            if (row.CheckGet("WIDTH") == format)
                            {
                                include = true;
                            }

                            if (include)
                            {
                                items.Add(row);
                            }

                        }
                    }
                    CompositionTableGrid.Items = items;
                }
            };

            ///<summary>
            /// Команды и инициализация
            ///</summary>
            CompositionTableGrid.Commands = Commander;
            CompositionTableGrid.Init();
        }

        public void SetDefaults()
        {
            PlatformSelectBox.SetItems(new Dictionary<string, string>()
            {
                {"1",  "Липецк"},
                {"2",  "Кашира"},
            });
            PlatformSelectBox.SelectedItem = PlatformSelectBox.Items.First();
            FormatSelectBox.SetItems(new Dictionary<string, string>()
            {
                { "0", "Все форматы"}

            });
            FormatSelectBox.SetSelectedItemByKey("0"); //Выпадающий список выбрал строку в выпадаюзем списке ID=0 - "Все фомраты"
        }

        private void PlatformSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CompositionTableGrid.LoadItems();
        }

        private void FormatSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CompositionTableGrid.UpdateItems();
        }
    }
}
