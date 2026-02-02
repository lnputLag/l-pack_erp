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
    /// Остаток по сырьевым группам на складе
    /// в табличном виде
    /// </summary>
    /// <author>kurasovdp</author>
    public partial class RawGroupMaterialMonitorTableTab : ControlBase
    {
        public RawGroupMaterialMonitorTableTab()
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
                RawGroupTableGridInit();
                SetDefaults();
            };

            OnUnload = () =>
            {
                RawGroupTableGrid.Destruct();
            };

            OnFocusGot = () =>
            {
                RawGroupTableGrid.ItemsAutoUpdate = true;
                RawGroupTableGrid.Run();
            };

            OnFocusLost = () =>
            {
                RawGroupTableGrid.ItemsAutoUpdate = false;
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
                            RawGroupTableGrid.ItemsExportExcel();
                        },
                    });
                }
            }
            Commander.Init(this);
        }

        /// <summary>
        /// Настраивает Grid для отображения списка изделий на складе.
        /// Отображает колонки, оформление строк(цвет)
        /// </summary>
        private void RawGroupTableGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Hidden= true,
                    Header="",
                    Path="ID_RAW_GROUP",
                    Description="ИД сырьевой группы",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Сырьевая группа",
                    Path="NAME",
                    Description="Наименование сырьевой группы",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Формат",
                    Path="FORMAT",
                    Description="Формат бумаги/картона",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Остаток",
                    Path="QTY_STOCK_ONLY",
                    Description="Остаток на складе по сырьевой группе",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                       if (row["QTY_STOCK_ONLY"].ToInt() == 0)
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
            RawGroupTableGrid.SetColumns(columns);
            RawGroupTableGrid.SetPrimaryKey("_ROWNUMBER");
            RawGroupTableGrid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            RawGroupTableGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            RawGroupTableGrid.Toolbar = RawGroupTableGridToolbar;

            ///<summary>
            /// Как загружать данные (запрос)
            ///</summary>
            RawGroupTableGrid.QueryLoadItems = new RequestData()
            {
                Module = "Stock",
                Object = "RawMaterialResidueMonitor",
                Action = "RawGroupList",
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
                    var a = rd.AnswerData["FORMATS"];
                    FormatSelectBox.SetItems(a, "ID", "NAME");
                    FormatSelectBox.SetSelectedItemFirst();
                    return ds;
                }
            };

            ///<summary>
            /// Команды и инициализация
            ///</summary>
            RawGroupTableGrid.Commands = Commander;
            RawGroupTableGrid.Init();
        }

        public void SetDefaults()
        {
            PlatformSelectBox.SetItems(new Dictionary<string, string>()
            {
                {"1",  "Липецк"},
                {"2",  "Кашира"},
            });
            PlatformSelectBox.SelectedItem = PlatformSelectBox.Items.First();
        }

        private void PlatformSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RawGroupTableGrid.LoadItems();
        }

        private void FormatSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RawGroupTableGrid.UpdateItems();
        }
    }
}
