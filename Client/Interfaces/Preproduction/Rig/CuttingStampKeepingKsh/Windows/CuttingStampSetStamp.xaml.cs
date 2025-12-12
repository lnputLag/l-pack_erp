using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
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

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Выбор штанцформы
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class CuttingStampSetStamp : ControlBase
    {
        public CuttingStampSetStamp()
        {
            ControlTitle = "Выбор штанцформы";
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/cutting_stamp";
            RoleName = "[erp]rig_cutting_stamp_keep";
            InitializeComponent();

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

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                Commander.Init(this);

                MachineListInit();
                StampGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                StampGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
            };

            {
                Commander.Add(new CommandItem()
                {
                    Name = "refresh",
                    Group = "main",
                    Enabled = true,
                    Title = "Обновить",
                    Description = "Обновить данные",
                    ButtonUse = true,
                    ButtonName = "RefreshButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        StampGrid.LoadItems();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "help",
                    Group = "main",
                    Enabled = true,
                    Title = "Справка",
                    Description = "Показать справочную информацию",
                    ButtonUse = true,
                    ButtonName = "HelpButton",
                    HotKey = "F1",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "save",
                    Group = "main",
                    Enabled = true,
                    Title = "",
                    Description = "Сохранить",
                    ButtonUse = true,
                    ButtonControl = SaveButton,
                    ButtonName = "SaveButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        Save();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "cancel",
                    Group = "main",
                    Enabled = true,
                    Title = "",
                    Description = "Отмена",
                    ButtonUse = true,
                    ButtonControl = CancelButton,
                    ButtonName = "CancelButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Close();
                    },
                });
            }            
        }

        public int FactoryId { get; set; }

        public KeyValuePair<string, string> SelectedMachine { get; private set; } 

        public Dictionary<string, string> SelectedStamp { get; private set; }

        /// <summary>
        /// Структура окна
        /// </summary>
        private Window Window { get; set; }

        /// <summary>
        /// Показ окна
        /// </summary>
        public void Show()
        {
            int w = (int)Width;
            int h = (int)Height;
            string title = this.ControlTitle;

            Window = new Window
            {
                Title = title,
                Width = w + 17,
                Height = h + 40,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow,
            };
            Window.Content = new Frame
            {
                Content = this,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            if (Window != null)
            {
                Window.Topmost = true;
                Window.ShowDialog();
            }
        }

        private void MachineListInit()
        {
            FormHelper.ComboBoxInitHelper(MachineSelectBox, "Rig", "CuttingStamp", "ListMachine", "ID", "MACHINE_NAME", "MACHINE", new Dictionary<string, string>() { { "FACTORY_ID", $"{FactoryId}" } }, true);
            MachineSelectBox.SetSelectedItemFirst();
        }

        /// <summary>
        /// Инициализация таблицы штанцформ
        /// </summary>
        private void StampGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn()
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn()
                {
                    Header="Штанцформа",
                    Path="STAMP_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn()
                {
                    Header="Ручки/отверстия",
                    Path="HOLE_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=3,
                },
                new DataGridHelperColumn()
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn()
                {
                    Header="PD",
                    Path="PD",
                    ColumnType=ColumnTypeRef.Double,
                    Format="N0",
                    Width2=6,
                    Stylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("PD").ToInt() == 0)
                                {
                                    color = HColor.Yellow;
                                }

                                if (!color.IsNullOrEmpty())
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        },
                    },
                },
                new DataGridHelperColumn()
                {
                    Header="Место хранения",
                    Path="STORAGE_PLACE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=6,
                },
                new DataGridHelperColumn()
                {
                    Header="FEFCO",
                    Path="FEFCO",
                    ColumnType=ColumnTypeRef.String,
                    Width2=6,
                },
                new DataGridHelperColumn()
                {
                    Header="Ласточкин хвост",
                    Path="DOVETAIL_JOINT_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=3,
                },
                new DataGridHelperColumn()
                {
                    Header="Перфорация по рилевке",
                    Path="CREASE_PERFORATION_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=3,
                },
                new DataGridHelperColumn()
                {
                    Header="Размер заготовки",
                    Path="BLANK_SIZE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn()
                {
                    Header="Количество полумуфт",
                    Path="ITEMS_QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn()
                {
                    Header="Следующая отгрузка",
                    Path="NEXT_SHIPMENT",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=6,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn()
                {
                    Header="Последняя отгрузка",
                    Path="LAST_SHIPMENT",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=6,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn()
                {
                    Header="Примечание",
                    Path="NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
            };
            StampGrid.SetColumns(columns);
            StampGrid.SetPrimaryKey("ID");
            StampGrid.SetSorting("ID", ListSortDirection.Ascending);
            StampGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            StampGrid.AutoUpdateInterval = 0;
            StampGrid.ItemsAutoUpdate = false;
            StampGrid.Commands = Commander;
            StampGrid.Toolbar = StampToolbar;
            StampGrid.SearchText = GridSearch;
            StampGrid.OnLoadItems = LoadStampItems;
            // Раскраска строк
            StampGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                // Цвета фона строк
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";
                        var nextShipment = row.CheckGet("NEXT_SHIPMENT");

                        if (!nextShipment.IsNullOrEmpty())
                        {
                            color = HColor.Orange;
                        }

                        var lastShipment = row.CheckGet("LAST_SHIPMENT");
                        if (!lastShipment.IsNullOrEmpty())
                        {
                            var lastDate = lastShipment.ToDateTime("dd.MM.yyyy");
                            if (DateTime.Compare(lastDate.AddDays(360), DateTime.Today) < 0)
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
            StampGrid.Init();
            StampGrid.Run();
        }

        /// <summary>
        /// Загрузка списка штанцформ
        /// </summary>
        private async void LoadStampItems()
        {
            int machineId = MachineSelectBox.SelectedItem.Key.ToInt();
            if (machineId > 0)
            {
                StampGrid.Toolbar.IsEnabled = false;

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Rig");
                q.Request.SetParam("Object", "CuttingStamp");
                q.Request.SetParam("Action", "List");
                q.Request.SetParam("MACHINE_ID", $"{machineId}");

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
                        var ds = ListDataSet.Create(result, "STAMP_LIST");
                        StampGrid.UpdateItems(ds);
                    }
                }

                StampGrid.Toolbar.IsEnabled = true;
            }
        }

        /// <summary>
        /// Закрытие окна
        /// </summary>
        private void Close()
        {
            if (this.Window != null)
            {
                StampGrid.Destruct();
                this.Window.Close();
            }
        }

        private void Save()
        {
            if (CreateNewStampCheckBox.IsChecked == true)
            {
                if (!string.IsNullOrEmpty(MachineSelectBox.SelectedItem.Key))
                {
                    SelectedStamp = new Dictionary<string, string>();
                    SelectedMachine = MachineSelectBox.SelectedItem;
                    Close();
                }
                else
                {
                    //msg 
                }
            }
            else
            {
                if (StampGrid != null && StampGrid.SelectedItem != null && StampGrid.SelectedItem.Count > 0)
                {
                    if (!string.IsNullOrEmpty(MachineSelectBox.SelectedItem.Key))
                    {
                        SelectedStamp = StampGrid.SelectedItem;
                        SelectedMachine = MachineSelectBox.SelectedItem;
                        Close();
                    }
                    else
                    {
                        //msg 
                    }
                }
                else
                {
                    // msg
                }
            }
        }

        private void MachineSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            StampGrid.LoadItems();
        }
    }
}
