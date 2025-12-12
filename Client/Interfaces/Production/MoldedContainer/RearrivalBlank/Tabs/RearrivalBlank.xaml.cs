using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Stock;
using Gu.Wpf.DataGrid2D;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace Client.Interfaces.Production.MoldedContainer
{
    /// <summary>
    /// Переоприходование заготовок литой тары
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class RearrivalBlank : ControlBase
    {
        public RearrivalBlank()
        {
            ControlTitle = "Переоприходование заготовок";
            DocumentationUrl = "/doc/l-pack-erp/";
            RoleName = "[erp]rearrival_blank";
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
                FormInit();
                SetDefaults();
                ProductionTaskGridInit();
                PositionGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                ProductionTaskGrid.Destruct();
                PositionGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                ProductionTaskGrid.ItemsAutoUpdate = true;
                ProductionTaskGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                ProductionTaskGrid.ItemsAutoUpdate = false;
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
                    ButtonControl = HelpButton,
                    ButtonName = "HelpButton",
                    HotKey = "F1",
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
            }

            Commander.SetCurrentGridName("ArrivalInvoiceGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "rearrival",
                    Title = "Переоприходовать",
                    Description = "Переоприходовать выбранные поддоны с заготовками в другом цвете",
                    Group = "rearrival",
                    MenuUse = true,
                    Enabled = false,
                    ButtonUse = true,
                    ButtonControl = RearrivalButton,
                    ButtonName = "RearrivalButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        Rearrival();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (PositionGrid != null && PositionGrid.Items != null && PositionGrid.Items.Count > 0)
                        {
                            if (PositionGrid.SelectedItem != null && PositionGrid.SelectedItem.Count > 0)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
            }

            Commander.Init(this);
        }

        private FormHelper Form { get; set; }

        private ListDataSet ProductionTaskDataSet { get; set; }

        private ListDataSet PositionDataSet { get; set; }

        private LabelReport2 LabelReporter {  get; set; }

        public static Dictionary<int, int> ProductAlternativeDictionary = new Dictionary<int, int>()
        {
            {521436, 553489},
            {553489, 521436},
        };

        public static Dictionary<int, string> ProductIdNameDictionary = new Dictionary<int, string>()
        {
            {521436, "Л-Пак Полуфабрикат"},
            {553489, "Л-Пак Полуфабрикат белый"},
        };

        public void SetDefaults()
        {
            ProductionTaskDataSet = new ListDataSet();
            PositionDataSet = new ListDataSet();

            var date = DateTime.Now;
            if (date.Hour >= 20 || date.Hour < 8)
            {
                if (date.Hour >= 20)
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.AddDays(1).ToString("dd.MM.yyyy")} 08:00:00");
                }
                else
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00");
                }
            }
            else
            {
                Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00");
                Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00");
            }

           LabelReporter = new LabelReport2(true);
        }

        public void Refresh()
        {
            ProductionTaskGrid.LoadItems();
        }

        public void FormInit()
        {
            //инициализация формы
            {
                Form = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="FROM_DATE_TIME",
                        FieldType=FormHelperField.FieldTypeRef.DateTime,
                        Control=FromDateTime,
                        ControlType="dateedit",
                        Format="dd.MM.yyyy HH:mm:ss",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        }
                    },
                    new FormHelperField()
                    {
                        Path="TO_DATE_TIME",
                        FieldType=FormHelperField.FieldTypeRef.DateTime,
                        Control=ToDateTime,
                        ControlType="dateedit",
                        Format="dd.MM.yyyy HH:mm:ss",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        }
                    },
                };

                Form.SetFields(fields);
            }
        }

        public void ProductionTaskGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид задания",
                        Description = "Идентификатор производственного задания для литой тары",
                        Path="PROT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата создания",
                        Description = "Дата создания производственного задания для литой тары",
                        Path="CREATED_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер задания",
                        Description = "Номер производственного задания для литой тары",
                        Path="PRODUCTION_TASK_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Description = "Статус производственного задания для литой тары",
                        Path="PRODUCTION_TASK_STATUS",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Станок",
                        Description = "Станок производственного задания для литой тары",
                        Path="MACHINE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 17,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Description = "Артикул продукции по производственному заданию для литой тары",
                        Path="PRODUCT_CODE",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Продукция",
                        Description = "Наименование продукции по производственному заданию для литой тары",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 21,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид ПЗ",
                        Description = "Идентификатор производственного задания",
                        Path="PRODUCTION_TASK_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид продукции",
                        Description = "Идентификатор продукции по производственному заданию для литой тары",
                        Path="PRODUCT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 7,
                    },
                 };
                ProductionTaskGrid.SetColumns(columns);
                ProductionTaskGrid.SetPrimaryKey("PROT_ID");
                ProductionTaskGrid.SearchText = ProductionTaskSearchBox;
                //данные грида
                ProductionTaskGrid.OnLoadItems = ProductionTaskGridLoadItems;
                ProductionTaskGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                ProductionTaskGrid.AutoUpdateInterval = 60 * 5;
                ProductionTaskGrid.Toolbar = ProductionTaskGridToolbar;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                ProductionTaskGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem != null && selectedItem.Count > 0)
                    {
                        if (ProductionTaskGrid != null && ProductionTaskGrid.Items != null && ProductionTaskGrid.Items.Count > 0)
                        {
                            if (ProductionTaskGrid.Items.FirstOrDefault(x => x.CheckGet("PROT_ID").ToInt() == selectedItem.CheckGet("PROT_ID").ToInt()) == null)
                            {
                                ProductionTaskGrid.SelectRowFirst();
                            }
                        }

                        PositionGridLoadItems();
                    }
                };

                ProductionTaskGrid.OnFilterItems = ProductionTaskGridFilterItems;

                ProductionTaskGrid.Commands = Commander;

                ProductionTaskGrid.Init();
            }
        }

        public async void ProductionTaskGridLoadItems()
        {
            if (Form.Validate())
            {
                var f = Form.GetValueByPath("FROM_DATE_TIME").ToDateTime();
                var t = Form.GetValueByPath("TO_DATE_TIME").ToDateTime();

                if (DateTime.Compare(f, t) > 0)
                {
                    const string msg = "Дата начала должна быть меньше даты окончания.";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();
                }
                else
                {
                    var p = new Dictionary<string, string>();
                    p.Add("DTTM_FROM", Form.GetValueByPath("FROM_DATE_TIME"));
                    p.Add("DTTM_TO", Form.GetValueByPath("TO_DATE_TIME"));

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "MoldedContainer");
                    q.Request.SetParam("Object", "RearrivalBlank");
                    q.Request.SetParam("Action", "ListProductionTask");
                    q.Request.SetParams(p);

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    ProductionTaskDataSet = new ListDataSet();
                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            ProductionTaskDataSet = ListDataSet.Create(result, "ITEMS");
                        }
                    }
                    ProductionTaskGrid.UpdateItems(ProductionTaskDataSet);
                }
            }
        }

        public void ProductionTaskGridFilterItems()
        {
            PositionGrid.ClearItems();

            if (ProductionTaskGrid != null && ProductionTaskGrid.SelectedItem != null && ProductionTaskGrid.SelectedItem.Count > 0)
            {
                ProductionTaskGrid.Commands.Message = new ItemMessage() { Action = "refresh", Message = $"{ProductionTaskGrid.SelectedItem.CheckGet("PROT_ID")}" };
            }
        }

        public void PositionGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="*",
                        Path="_SELECTED",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2 = 5,
                        Editable = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ITEM_ID",
                        Description="Идентификатор складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата создания",
                        Path="CREATED_DTTM",
                        Description="Дата создания складской единицы",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Path="ITEM_QUANTITY",
                        Description="Количество складской единицы",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="ITEM_NAME",
                        Description="Наименование складской единицы",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 34,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД поддона",
                        Path="PALLET_ID",
                        Description="Идентификатор поддона складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД прихода",
                        Path="INCOMING_ID",
                        Description="Идентификатор прихода складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД накладной",
                        Path="INVOICE_ID",
                        Description="Идентификатор накладной прихода складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД ПЗ",
                        Path="PRODUCTION_TASK_ID",
                        Description="Идентификатор производственного задания складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД продукции",
                        Path="PRODUCT_ID",
                        Description="Идентификатор продукции складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД заявки",
                        Path="ORDER_ID",
                        Description="Идентификатор заявки складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 7,
                    },
                };
                PositionGrid.SetColumns(columns);
                PositionGrid.SetPrimaryKey("ITEM_ID");
                PositionGrid.SearchText = PositionSearchBox;
                //данные грида
                PositionGrid.OnLoadItems = PositionGridLoadItems;
                PositionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                PositionGrid.AutoUpdateInterval = 0;
                PositionGrid.ItemsAutoUpdate = false;
                PositionGrid.Toolbar = PositionGridToolbar;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                PositionGrid.OnSelectItem = selectedItem =>
                {
                    if (PositionGrid != null && PositionGrid.Items != null && PositionGrid.Items.Count > 0)
                    {
                        if (PositionGrid.Items.FirstOrDefault(x => x.CheckGet("ITEM_ID").ToInt() == selectedItem.CheckGet("ITEM_ID").ToInt()) == null)
                        {
                            PositionGrid.SelectRowFirst();
                        }
                    }
                };

                PositionGrid.Commands = Commander;

                PositionGrid.Init();
            }
        }

        public async void PositionGridLoadItems()
        {
            if (ProductionTaskGrid != null && ProductionTaskGrid.SelectedItem != null && ProductionTaskGrid.SelectedItem.Count > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("PRODUCTION_TASK_ID", ProductionTaskGrid.SelectedItem["PRODUCTION_TASK_ID"]);

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "MoldedContainer");
                q.Request.SetParam("Object", "RearrivalBlank");
                q.Request.SetParam("Action", "ListPositionByProductionTask");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                PositionDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        PositionDataSet = ListDataSet.Create(result, "ITEMS");
                    }
                }
                PositionGrid.UpdateItems(PositionDataSet);
            }
        }

        public async void Rearrival()
        {
            var selectedItemList = PositionGrid.GetItemsSelected();
            if (selectedItemList != null && selectedItemList.Count > 0)
            {
                bool resume = true;
                int alternativeProductId = GetAlternativeProductId(selectedItemList[0]["PRODUCT_ID"].ToInt());

                if (alternativeProductId > 0)
                {
                    var d = new DialogWindow($"" +
                    $"Переоприходовать {selectedItemList.Sum(x => x.CheckGet("ITEM_QUANTITY").ToInt())} штук {ProductIdNameDictionary[selectedItemList[0]["PRODUCT_ID"].ToInt()]} в {ProductIdNameDictionary[alternativeProductId]}?",
                    this.ControlTitle, "", DialogWindowButtons.YesNo);
                    if (d.ShowDialog() != true)
                    {
                        resume = false;
                    }
                }
                else
                {
                    DialogWindow.ShowDialog($"Не найдены заготовки, в которые можно переоприходовать эти заготовки.");
                    resume = false;
                }

                if (resume)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        EnableSplash();
                    });

                    foreach (var selectedItem in selectedItemList)
                    {
                        if (!await RearrivalOne(selectedItem, false, false))
                        {
                            resume = false;
                        }
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DisableSplash();
                    });

                    if (resume)
                    {
                        DialogWindow.ShowDialog($"Успешное выполнение переоприходования заготовок литой тары.");
                    }

                    Refresh();
                }
            }
            else if (PositionGrid.SelectedItem != null && PositionGrid.SelectedItem.Count > 0)
            {
                bool resume = true;
                int alternativeProductId = GetAlternativeProductId(PositionGrid.SelectedItem["PRODUCT_ID"].ToInt());

                if (alternativeProductId > 0)
                {
                    var d = new DialogWindow($"" +
                    $"Переоприходовать {PositionGrid.SelectedItem["ITEM_QUANTITY"].ToInt()} штук {PositionGrid.SelectedItem["ITEM_NAME"]} в {ProductIdNameDictionary[alternativeProductId]}?",
                    this.ControlTitle, "", DialogWindowButtons.YesNo);
                    if (d.ShowDialog() != true)
                    {
                        resume = false;
                    }
                }
                else
                {
                    DialogWindow.ShowDialog($"Не найдены заготовки, в которые можно переоприходовать эти заготовки.");
                    resume = false;
                }

                if (resume)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        EnableSplash();
                    });

                    await RearrivalOne(PositionGrid.SelectedItem);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DisableSplash();
                    });
                }
            }
            else 
            {
                DialogWindow.ShowDialog($"Не выбрана позиция для переоприходования заготовок литой тары.");
            }
        }

        private async Task<bool> RearrivalOne(Dictionary<string, string> selectedItem, bool showMessage = true, bool refresh = true)
        {
            bool succesfullFlag = false;

            if (selectedItem != null && selectedItem.Count > 0)
            {
                string palletId = selectedItem["PALLET_ID"];
                int alternativeProductId = GetAlternativeProductId(selectedItem["PRODUCT_ID"].ToInt());

                var p = new Dictionary<string, string>();
                p.Add("ITEM_ID", selectedItem["ITEM_ID"]);
                p.Add("NEW_PRODUCT_ID", $"{alternativeProductId}");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "MoldedContainer");
                q.Request.SetParam("Object", "RearrivalBlank");
                q.Request.SetParam("Action", "Rearrival");
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
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            if (ds.Items[0].CheckGet("ITEM_ID").ToInt() > 0)
                            {
                                succesfullFlag = true;
                            }
                        }
                    }

                    if (succesfullFlag)
                    {
                        if (showMessage)
                        {
                            DialogWindow.ShowDialog($"Успешное выполнение переоприходования заготовок литой тары.");
                        }

                        if (!string.IsNullOrEmpty(palletId))
                        {
                            LabelReporter.PrintLabel(palletId);
                        }

                        if (refresh)
                        {
                            Refresh();
                        }
                    }
                    else
                    {
                        DialogWindow.ShowDialog($"При выполнении переоприходования заготовок литой тары произошла ошибка. Пожалуйста, сообщите о проблеме.");
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
            else
            {
                DialogWindow.ShowDialog($"Не выбрана позиция для переоприходования заготовок литой тары.");
            }

            return succesfullFlag;
        }

        public int GetAlternativeProductId(int curentId)
        {
            int alternativeId = 0;

            try
            {
                alternativeId = ProductAlternativeDictionary[curentId];
            }
            catch (Exception ex)
            {

            }

            return alternativeId;
        }

        public void EnableSplash()
        {
            DisableControls();
            SplashControl.Message = $"Пожалуйста, подождите.";
            SplashControl.Visible = true;
        }

        public void DisableSplash()
        {
            EnableControls();
            SplashControl.Message = "";
            SplashControl.Visible = false;
        }

        public void DisableControls()
        {
            ProductionTaskGridToolbar.IsEnabled = false;
            PositionGridToolbar.IsEnabled = false;
        }

        public void EnableControls()
        {
            ProductionTaskGridToolbar.IsEnabled = true;
            PositionGridToolbar.IsEnabled = true;
        }

        /// <summary>
        /// Установка настроек для принтера
        /// </summary>
        public void SetPrintSettings()
        {
            LabelReport2.SetPrintingProfile();
        }

        private void OnCurrentShift(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now;
            if (date.Hour >= 20 || date.Hour < 8)
            {
                if (date.Hour >= 20)
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.AddDays(1).ToString("dd.MM.yyyy")} 08:00:00");
                }
                else
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00");
                }
            }
            else
            {
                Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00");
                Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00");
            }

            Refresh();
        }

        private void OnPrevShift(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now.AddHours(-12);
            if (date.Hour >= 20 || date.Hour < 8)
            {
                if (date.Hour >= 20)
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{date.AddDays(1).ToString("dd.MM.yyyy")} 08:00:00");
                }
                else
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{date.AddDays(-1).ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 08:00:00");
                }
            }
            else
            {
                Form.SetValueByPath("FROM_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 08:00:00");
                Form.SetValueByPath("TO_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 20:00:00");
            }

            Refresh();
        }

        private void OnCurrentDay(object sender, RoutedEventArgs e)
        {
            Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.AddDays(1).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnPrevDay(object sender, RoutedEventArgs e)
        {
            Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnCurrentWeek(object sender, RoutedEventArgs e)
        {
            DayOfWeek day = DateTime.Now.DayOfWeek;
            int days = day - DayOfWeek.Monday;
            DateTime date = DateTime.Now.AddDays(-days);

            Form.SetValueByPath("FROM_DATE_TIME", $"{date.Date.ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{date.Date.AddDays(7).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnPrevWeek(object sender, RoutedEventArgs e)
        {
            DayOfWeek day = DateTime.Now.DayOfWeek;
            int days = day - DayOfWeek.Monday;
            DateTime date = DateTime.Now.AddDays(-days).AddDays(-7);

            Form.SetValueByPath("FROM_DATE_TIME", $"{date.Date.ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{date.Date.AddDays(7).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnCurrentMonth(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now;

            Form.SetValueByPath("FROM_DATE_TIME", $"{new DateTime(date.Year, date.Month, 1).ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{new DateTime(date.Year, date.Month, 1).AddMonths(1).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnPrevMonth(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now.AddMonths(-1);

            Form.SetValueByPath("FROM_DATE_TIME", $"{new DateTime(date.Year, date.Month, 1).ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{new DateTime(date.Year, date.Month, 1).AddMonths(1).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }
    }
}
