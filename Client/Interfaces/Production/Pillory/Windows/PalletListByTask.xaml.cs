using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
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
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.Pillory
{
    /// <summary>
    /// Интерфейс Список поддонов по ПЗ переработки
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class PalletListByTask : ControlBase
    {
        public PalletListByTask()
        {
            ControlTitle = "Список поддонов по ПЗ";
            RoleName = "[erp]pillory";
            DocumentationUrl = "/doc/l-pack-erp/";
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

                FormInit();
                SetDefaults();
                PalletGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                PalletGrid?.Destruct();
            };

            OnFocusGot = () =>
            {
            };

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
                    ButtonControl = RefreshButton,
                    ButtonName = "RefreshButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Refresh();
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
                    AccessLevel = Role.AccessMode.ReadOnly,
                    HotKey = "F1",
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
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

        /// <summary>
        /// Наименование таба, который вызвал этот таб
        /// </summary>
        public string ParentFrame { get; set; }

        /// <summary>
        /// Ид станка переработки
        /// stanok.id_st
        /// </summary>
        public int MachineId { get; set; }

        /// <summary>
        /// Ид заготовки
        /// tovar.id2
        /// </summary>
        public int BlankProductId { get; set; }

        /// <summary>
        /// Ид ПЗ ГА
        /// proiz_zad.id_pz
        /// </summary>
        public int BlankProductionTaskId { get; set; }

        /// <summary>
        /// Ид позиции заявки
        /// orderdates.idorderdates
        /// </summary>
        public int OrderPositionId { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        private FormHelper Form { get; set; }

        private ListDataSet PalletGridDataSet { get; set; }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 2;

            FrameName = $"{FrameName}";
            Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
            windowParametrs.Add("no_resize", "1");
            windowParametrs.Add("center_screen", "1");
            this.MinHeight = 650;
            this.MinWidth = 800;
            Central.WM.Show(FrameName, this.ControlTitle, true, "main", this, "top", windowParametrs);
        }

        /// <summary>
        /// инициализация компонентов формы
        /// </summary>
        private void FormInit()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="SEARCH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=SearchText,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
            };
            Form.SetFields(fields);
        }

        private void SetDefaults()
        {
            bool resume = true;
            string msg = "";

            if (resume)
            {
                if (!(MachineId > 0 && BlankProductId > 0))
                {
                    resume = false;
                    msg = "Нет данных по производственному заданию. Пожалуйста, сообщите о проблеме.";
                }
            }

            if (resume)
            {
                PalletGridDataSet = new ListDataSet();
            }
            else
            {
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();

                Close();
            }
        }

        private void PalletGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид поддона",
                        Description = "Идентификатор поддона",
                        Path="PALLET_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер поддона",
                        Description = "Полный номер поддона",
                        Path="PALLET_FULL_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество, шт.",
                        Description = "Количество продукции на поддоне",
                        Path="PALLET_QUANTITY",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=6,
                        Format="N0",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ячейка",
                        Description = "Местонахождение поддона",
                        Path="PALLET_PLACE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=15,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Продукция",
                        Description = "Наименование продукции",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Description = "Артикул продукции",
                        Path="PRODUCT_CODE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=15,
                    },
                    
                    new DataGridHelperColumn
                    {
                        Header="№ поддона",
                        Description = "Порядковый номер поддона",
                        Path="PALLET_NUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид заявки",
                        Description = "Идентификатор позиции заявки",
                        Path="ORDER_POSITION_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                };
                PalletGrid.SetColumns(columns);
                PalletGrid.SetPrimaryKey("PALLET_ID");
                PalletGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                PalletGrid.SearchText = SearchText;
                PalletGrid.AutoUpdateInterval = 0;
                PalletGrid.OnLoadItems = PalletGridLoadItems;
                PalletGrid.UseProgressSplashAuto = false;
                PalletGrid.Commands = this.Commander;
                PalletGrid.Init();
            }
        }

        private async void PalletGridLoadItems()
        {
            EnableSplash();

            var p = new Dictionary<string, string>();
            p.Add("MACHINE_ID", $"{MachineId}");
            p.Add("BLANK_PRODUCT_ID", $"{BlankProductId}");
            p.Add("BLANK_PRODUCTION_TASK_ID", $"{BlankProductionTaskId}");
            p.Add("ORDER_POSITION_ID", $"{OrderPositionId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production/Pillory");
            q.Request.SetParam("Object", "ProductionTask");
            q.Request.SetParam("Action", "ListPallet");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            PalletGridDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    PalletGridDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            else
            {
                q.ProcessError();
            }
            PalletGrid.UpdateItems(PalletGridDataSet);

            DisableSplash();
        }

        private void Refresh()
        {
            PalletGrid.LoadItems();
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        private void Close()
        {
            if (!string.IsNullOrEmpty(ParentFrame))
            {
                Central.WM.SetActive(ParentFrame, true);
            }

            Central.WM.Close(FrameName);

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
        }

        private void EnableSplash()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SplashControl.Visible = true;
            });
        }

        private void DisableSplash()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SplashControl.Visible = false;
            });
        }
    }
}
