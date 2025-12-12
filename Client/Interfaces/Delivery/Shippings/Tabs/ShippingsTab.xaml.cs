using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.DeliveryAddresses;
using Client.Interfaces.Main;
using CodeReason.Reports;
using DevExpress.Data.Helpers;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using NLog.LayoutRenderers;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using System.Windows.Xps.Serialization;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static iTextSharp.text.pdf.qrcode.Version;

namespace Client.Interfaces.Delivery.Shippings
{
    /// <summary>
    /// Загрузки
    /// </summary>
    /// <author>motenko_ek</author>   
    public partial class ShippingsTab : ControlBase
    {
        private List<DataGridHelperColumn> Columns;

        public ShippingsTab()
        {
            InitializeComponent();

            RoleName = "[erp]delivery_shippings";
            ControlTitle = "Загрузки";
            DocumentationUrl = "/doc/l-pack-erp/delivery/shippings_tab";

            StatusSelectBox.SelectedItemChanged += (DependencyObject d, DependencyPropertyChangedEventArgs e) => {
                ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
            };

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };
            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };
            OnLoad = () =>
            {
                GridInit();
            };
            OnUnload = () =>
            {
                Grid.Destruct();
            };

            SetDefaults();

            Commander.SetCurrentGridName("Grid");
            Commander.Add(new CommandItem()
            {
                Name = "refresh",
                Group = "grid_base",
                Enabled = true,
                Title = "Обновить",
                Description = "Обновить",
                ButtonUse = true,
                ButtonName = "ShowButton",
                MenuUse = true,
                ActionMessage = (ItemMessage message) =>
                {
                    Grid.LoadItems();
                    Grid.SelectRowByKey(message.Message);
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "add",
                Group = "grid_base",
                Enabled = true,
                Title = "Создать",
                MenuUse = true,
                HotKey = "Insert",
                ButtonUse = true,
                ButtonName = "CreateButton",
                AccessLevel = Client.Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    new ShippingForm(new ItemMessage() { ReceiverName = ControlName, Action = "refresh" });
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "excel",
                Group = "grid_base",
                Enabled = true,
                Title = "В Excel",
                Description = "В Excel",
                MenuUse = true,
                ButtonUse = true,
                ButtonName = "ExcelButton",
                AccessLevel = Client.Common.Role.AccessMode.ReadOnly,
                Action = Grid.ItemsExportExcel
            });
            Commander.SetCurrentGroup("item");
            Commander.Add(new CommandItem()
            {
                Name = "edit",
                Title = "Изменить",
                MenuUse = true,
                HotKey = "Return|DoubleCLick",
                ButtonUse = true,
                ButtonName = "EditButton",
                AccessLevel = Client.Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    var k = Grid.GetPrimaryKey();
                    var id = Grid.SelectedItem.CheckGet(k).ToInt();
                    if (id != 0)
                    {
                        new ShippingForm(new ItemMessage() { ReceiverName = ControlName, Action = "refresh" }, id);
                    }
                },
                CheckEnabled = () =>
                {
                    return Grid.SelectedItem != null
                        && Grid.SelectedItem.Count > 0;
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "delete",
                Title = "Удалить",
                MenuUse = true,
                ButtonUse = true,
                ButtonName = "DeleteButton",
                AccessLevel = Client.Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    Delete(Grid.SelectedItem);
                },
                CheckEnabled = () =>
                {
                    return Grid.SelectedItem != null
                        && Grid.SelectedItem.Count > 0;
                },
            });
            Commander.Init(this);
        }

        /// <summary>
        /// Инициализация грида
        /// </summary>
        public void GridInit()
        {
            Columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="*",
                        Path="CHECKING",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=4,
                        Editable=true,
                        Visible = false,
                        //OnAfterClickAction = (Dictionary<string, string> value, FrameworkElement element) =>
                        //{
                        //    if(value["CHECKING"]=="True") CheckBoxCount++;
                        //    else CheckBoxCount--;

                        //    ShippingAddressCopyButton.IsEnabled = CheckBoxCount > 0;

                        //    return true;
                        //},
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ID_SHIP",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Тип доставки",
                        Path="SHIPPING_TYPE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Завод",
                        Path="FACT_ID",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Адрес площадки",
                        Path="FACTORY_ID_ADRES",
                        ColumnType=ColumnTypeRef.String,
                        Width2=30,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Контрагент",
                        Path="IDMO",
                        ColumnType=ColumnTypeRef.String,
                        Width2=16,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Адрес контрагента",
                        Path="ID_ADRES",
                        ColumnType=ColumnTypeRef.String,
                        Width2=30,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Груз",
                        Path="CARGO",
                        ColumnType=ColumnTypeRef.String,
                        Width2=20,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес",
                        Path="WEIGHT",
                        ColumnType=ColumnTypeRef.String,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Длина",
                        Path="CARGO_LENGTH",
                        ColumnType=ColumnTypeRef.String,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ширина",
                        Path="CARGO_WIDTH",
                        ColumnType=ColumnTypeRef.String,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Высота",
                        Path="CARGO_HEIGHT",
                        ColumnType=ColumnTypeRef.String,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Начало",
                        Path = "BEGIN_DT",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Окончание",
                        Path = "END_DT",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 10,
                    },
                    //new DataGridHelperColumn
                    //{
                    //    Header = "Место выгрузки",
                    //    Path = "FACT_ID",
                    //    ColumnType = ColumnTypeRef.String,
                    //    Width2 = 10,
                    //},
                    //new DataGridHelperColumn
                    //{
                    //    Header="Способ выгрузки",
                    //    Path="FACTORY_ID_ADRES",
                    //    ColumnType=ColumnTypeRef.String,
                    //    Width2=10,
                    //},
                    new DataGridHelperColumn
                    {
                        Header="Комментарий",
                        Path="NOTE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=20,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Плановая отгрузка",
                        Path="DTTM",
                        ColumnType=ColumnTypeRef.String,
                        Width2=16,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Плательщик",
                        Path="ID_PROD",
                        ColumnType=ColumnTypeRef.String,
                        Width2=16,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ответственный",
                        Path="ACCO_ID",
                        ColumnType=ColumnTypeRef.String,
                        Width2=16,
                    },
                    //new DataGridHelperColumn
                    //{
                    //    Path="ID_TS",
                    //    Visible=false,
                    //},
                };
            Grid.SetColumns(Columns);
            Grid.SetPrimaryKey("ID_SHIP");
            Grid.SearchText = SearchText;
            Grid.Toolbar = GridToolbar;
            Grid.Commands = Commander;
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.AutoUpdateInterval = 0;
            Grid.ItemsAutoUpdate = false;
            Grid.EnableFiltering = true;
            Grid.QueryLoadItems = new RequestData()
            {
                Module = "Delivery",
                Object = "Shippings",
                Action = "GetList",
                AnswerSectionKey = "ITEMS",
                Timeout = 10000,
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                            {
                                { "STATUS", StatusSelectBox.SelectedItem.Key},
                                { "ACCO_ID", Central.User.AccountId.ToString() }
                            };
                },
                AfterUpdate = (RequestData rd, ListDataSet ds) =>
                {
                    ShowButton.Style = (Style)ShowButton.TryFindResource("Button");
                },
            };
            Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                {
                    StylerTypeRef.BackgroundColor,
                    (Dictionary<string, string> row) =>
                    {
                        return row.CheckGet("ID_TS").ToInt() switch
                        {
                            0 => HColor.Blue.ToBrush(),
                            _ => DependencyProperty.UnsetValue,
                        };
                    }
                },
            };
            Grid.Init();
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            StatusSelectBox.SetItems(new Dictionary<string, string>()
            {
                {"15",  "Все"},
                {"1",  "Создана"},
                {"2",  "Обработана"},
                //{"4",  "В пути"},
                //{"8",  "Выполнена"},
            });
            StatusSelectBox.SelectedItem = StatusSelectBox.Items.First();
        }

        public async void Delete(Dictionary<string, string> row)
        {
            if (DialogWindow.ShowDialog($"Вы действительно хотите удалить {row.CheckGet("CARGO")}?", "Удаление", "", DialogWindowButtons.NoYes) != true) return;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Delivery");
            q.Request.SetParam("Object", "Shippings");
            q.Request.SetParam("Action", "Delete");
            q.Request.SetParam("ID", row.CheckGet(Grid.GetPrimaryKey()));

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Grid.SelectRowPrev();
                Grid.LoadItems();
            }
            else
            {
                q.ProcessError();
            }
        }
    }
}
