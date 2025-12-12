using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Данные по расходу картона (композиций) на заданный диапазон дат
    /// </summary>
    public partial class PreproductionConfirmOrderCompositionList : ControlBase
    {
        public PreproductionConfirmOrderCompositionList()
        {
            InitializeComponent();
            ControlTitle = "Отчёт по композициям";
            RoleName = "[erp]preproduction_confirm_order";

            OnMessage = (ItemMessage message) => {

                DebugLog($"message=[{message.Message}]");

                if (message.ReceiverName == ControlName)
                {
                    ProcessCommand(message.Action);
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
                ProcessPermissions();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                CardboardGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                Refresh();
                CardboardGrid.ItemsAutoUpdate = true;
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                CurrentOrderCardboard = new List<int>();
                CurrentOrderDt = "";
                CardboardGrid.ItemsAutoUpdate = false;
            };
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Основной датасет с данными по картону
        /// </summary>
        public ListDataSet CardboardGridDataSet { get; set; }

        /// <summary>
        /// Список идентификаторов картона, используемого для выбранной заявки
        /// </summary>
        public List<int> CurrentOrderCardboard { get; set; }

        /// <summary>
        /// Дата отгрузки для выбранной заявки
        /// </summary>
        public string CurrentOrderDt { get; set; }

        /// <summary>
        /// Количество дней в указанном диапазоне дат (включая первую и последнюю даты)
        /// </summary>
        public int DayCount { get; set; }

        public void FormInit()
        {
            //инициализация формы
            {
                Form = new FormHelper();

                //список колонок формы
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path = "DT_FROM",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = DtFrom,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "DT_TO",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = DtTo,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                };

                Form.SetFields(fields);
            }
        }

        public void SetDefaults()
        {
            CurrentOrderCardboard = new List<int>();
            CardboardGridDataSet = new ListDataSet();
            Form.SetValueByPath("DT_FROM", DateTime.Now.ToString("dd.MM.yyyy"));
            Form.SetValueByPath("DT_TO", DateTime.Now.AddDays(14).ToString("dd.MM.yyyy"));

            var factorySelectBoxItems = new Dictionary<string, string>();
            factorySelectBoxItems.Add("1", "Л-ПАК ЛИПЕЦК");
            factorySelectBoxItems.Add("2", "Л-ПАК КАШИРА");
            FactorySelectBox.SetItems(factorySelectBoxItems);
            FactorySelectBox.SetSelectedItemByKey("1");

            LoadProfileList();
            Refresh();
        }

        public void Refresh()
        {
            RefreshButton.Style = (Style)RefreshButton.TryFindResource("Button");
            DayCount = (Form.GetValueByPath("DT_TO").ToDateTime() - Form.GetValueByPath("DT_FROM").ToDateTime()).Days + 1;
            CardboardGridInit();
        }

        /// <summary>
        /// Инициализация грида
        /// </summary>
        public void CardboardGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид картона",
                        Path="CARDBOARD_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=40,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Композиция",
                        Path="CARDBOARD_DESCRIPTION",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=176,
                        MaxWidth=195,
                        Width2=20,
                        Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    if (row.CheckGet("COMPOSITION_TYPE").ToInt() == 1)
                                    {
                                        color = HColor.Green;
                                    }
                                    else if (row.CheckGet("COMPOSITION_TYPE").ToInt() == 2)
                                    {
                                        color = HColor.Yellow;
                                    }
                                    else if (row.CheckGet("COMPOSITION_TYPE").ToInt() == 3)
                                    {
                                        color = HColor.Orange;
                                    }
                                    else if (row.CheckGet("COMPOSITION_TYPE").ToInt() == 4 || row.CheckGet("COMPOSITION_TYPE").ToInt() == 5)
                                    {
                                        color = HColor.Blue;
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

                for (int dayIterator = 0; dayIterator < DayCount; dayIterator++)
                {
                    var column = new DataGridHelperColumn
                    {
                        Header = $"{Form.GetValueByPath("DT_FROM").ToDateTime().AddDays(dayIterator).ToString("dd.MM.yyyy")}",
                        Path = $"DAY_{dayIterator}",
                        ColumnType = ColumnTypeRef.Integer,
                        MinWidth = 48,
                        MaxWidth = 73,
                        Width2 = 10,
                    };

                    columns.Add(column);
                }

                foreach (var column in columns)
                {
                    if (column.Header == CurrentOrderDt)
                    {
                        column.Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = HColor.Pink;
                                    result=color.ToBrush();

                                    return result;
                                }
                            },
                        };
                    }
                }

                CardboardGrid.SetColumns(columns);
                CardboardGrid.OnLoadItems = CardboardGridLoadItems;
                CardboardGrid.PrimaryKey = "CARDBOARD_ID";
                CardboardGrid.UseSorting = false;
                CardboardGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                
                CardboardGrid.OnFilterItems = () =>
                {
                    if (CardboardGrid.GridItems != null && CardboardGrid.GridItems.Count > 0)
                    {
                        if (CurrentOrderCardboard != null && CurrentOrderCardboard.Count > 0)
                        {
                            var items = new List<Dictionary<string, string>>();
                            items.AddRange(CardboardGrid.GridItems.Where(x => CurrentOrderCardboard.Contains(x.CheckGet("CARDBOARD_ID").ToInt())));
                            CardboardGrid.GridItems = items;
                        }

                        if (ProfileSelectBox.SelectedItem.Key != null)
                        {
                            if (ProfileSelectBox.SelectedItem.Key != "-1")
                            {
                                CardboardGrid.GridItems = CardboardGrid.GridItems.Where(x => x.CheckGet("PROFILE_ID").ToInt() == ProfileSelectBox.SelectedItem.Key.ToInt()).ToList();
                            }
                        }
                    }
                };

                CardboardGrid.Init();
                CardboardGrid.Run();
                CardboardGrid.Focus();
            }
        }

        /// <summary>
        /// Получаем данные для заполнения грида
        /// </summary>
        public async void CardboardGridLoadItems()
        {
            DisableControls();

            var p = new Dictionary<string, string>();
            p.Add("DT_FROM", Form.GetValueByPath("DT_FROM"));
            p.Add("DT_TO", Form.GetValueByPath("DT_TO"));
            p.Add("FACTORY_ID", FactorySelectBox.SelectedItem.Key);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "ConfirmOrder");
            q.Request.SetParam("Action", "ListComposition");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            CardboardGridDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    CardboardGridDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            CardboardGrid.UpdateItems(CardboardGridDataSet);
            CardboardGrid.Focus();

            EnableControls();
        }

        /// <summary>
        /// Получаем список профилей для выпадающего списка
        /// </summary>
        public async void LoadProfileList()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "TechnologicalMapForSite");
            q.Request.SetParam("Action", "ListProfile");
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
                    List<Dictionary<string, string>> profileList = new List<Dictionary<string, string>>();
                    Dictionary<string, string> emptyProfile = new Dictionary<string, string>();
                    emptyProfile.Add("ID", "-1");
                    emptyProfile.Add("NAME", "Все профили");
                    profileList.Add(emptyProfile);
                    ListDataSet ds = ListDataSet.Create(result, "ITEMS");
                    if (ds.Items != null)
                    {
                        profileList.AddRange(ds.Items);
                    }
                    ds.Items = profileList;

                    ProfileSelectBox.SetItems(ds, "ID", "NAME");
                    ProfileSelectBox.SetSelectedItemByKey("-1");
                }
            }
        }

        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            CardboardGrid.ShowSplash();
        }

        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            CardboardGrid.HideSplash();
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel("[erp]preproduction_confirm_order");
            var userAccessMode = mode;
            switch (mode)
            {
                case Role.AccessMode.Special:
                    break;

                case Role.AccessMode.FullAccess:
                    break;

                case Role.AccessMode.ReadOnly:
                default:
                    break;
            }

            List<Button> buttons = UIUtil.GetVisualChilds<Button>(this.Content as DependencyObject);
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    var buttonTagList = UIUtil.GetTagList(button);
                    var accessMode = Acl.FindTagAccessMode(buttonTagList);
                    if (accessMode > userAccessMode)
                    {
                        button.IsEnabled = false;
                    }
                }
            }

            if (CardboardGrid != null && CardboardGrid.Menu != null && CardboardGrid.Menu.Count > 0)
            {
                foreach (var manuItem in CardboardGrid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }
        }

        public void ProcessCommand(string command)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "refresh":
                        {
                            Refresh();
                        }
                        break;

                    case "help":
                        {
                            Central.ShowHelp("/doc/l-pack-erp-new/planing/confirm_application/report_composition");
                        }
                        break;
                }
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessCommand("help");
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void ProfileSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (CardboardGrid != null && CardboardGrid.GridItems != null)
            {
                CardboardGrid.UpdateItems();
            }
        }

        private void FactorySelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RefreshButton.Style = (Style)RefreshButton.TryFindResource("FButtonPrimary");
        }
    }
}
