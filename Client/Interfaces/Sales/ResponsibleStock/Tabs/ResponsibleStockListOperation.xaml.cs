using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
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

namespace Client.Interfaces.Sales
{
    /// <summary>
    /// Список операций с поддонами СОХ
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class ResponsibleStockListOperation : UserControl
    {
        public ResponsibleStockListOperation()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            SetDefaults();
            OperationGridInit();

            ProcessPermissions();
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Основной датасет с данными
        /// </summary>
        public ListDataSet OperationGridDataSet { get; set; }

        /// <summary>
        /// Выбранная запись в гриде
        /// </summary>
        public Dictionary<string, string> OperationGridSelectedItem { get; set; }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void OperationGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата операции",
                        Path="OPERATION_DTTM",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=62,
                        MaxWidth=112,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование операции",
                        Path="OPERATION_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=158,
                        MaxWidth=158,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Поддон",
                        Path="PALLET_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=60,
                        MaxWidth=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Кол-во на поддоне",
                        Path="QUANTITY_ON_PALLET",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Продукция",
                        Path="PRODUCTION_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=280,
                        MaxWidth=400,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид продукции",
                        Path="PRODUCTION_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=63,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Создатель операции",
                        Path="OPERATION_CREATOR_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=75,
                        MaxWidth=75,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Пользователь",
                        Path="ACCOUNT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=75,
                        MaxWidth=95,
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
                OperationGrid.SetColumns(columns);

                OperationGrid.SearchText = SearchText;
                OperationGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                OperationGrid.OnSelectItem = selectedItem =>
                {
                    OperationGridSelectedItem = selectedItem;
                    ProcessPermissions();
                };

                OperationGrid.SetSorting("ID", System.ComponentModel.ListSortDirection.Descending);

                //данные грида
                OperationGrid.OnLoadItems = OperationGridLoadItems;

                OperationGrid.OnFilterItems = () =>
                {
                    if (OperationGrid.GridItems != null)
                    {
                        if (OperationGrid.GridItems.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(OperationSelectBox.SelectedItem.Key))
                            {
                                var operationId = OperationSelectBox.SelectedItem.Key;
                                var items = new List<Dictionary<string, string>>();

                                if (operationId.ToInt() > -1)
                                {
                                    items.AddRange(OperationGrid.GridItems.Where(row => row.CheckGet("OPERATION_TYPE_ID").ToInt() == operationId.ToInt()));
                                }
                                else
                                {
                                    items = OperationGrid.GridItems;
                                }
                                
                                OperationGrid.GridItems = items;
                            }

                            if (!string.IsNullOrEmpty(CreatorSelectBox.SelectedItem.Key))
                            {
                                var creatorId = CreatorSelectBox.SelectedItem.Key;
                                var items = new List<Dictionary<string, string>>();

                                if (creatorId.ToInt() > -1)
                                {
                                    items.AddRange(OperationGrid.GridItems.Where(row => row.CheckGet("OPERATION_CREATOR").ToInt() == creatorId.ToInt()));
                                }
                                else
                                {
                                    items = OperationGrid.GridItems;
                                }

                                OperationGrid.GridItems = items;
                            }
                        }
                    }
                };

                OperationGrid.Run();

                //инициализация формы
                {
                    Form = new FormHelper();

                    //колонки формы
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
                        new FormHelperField()
                        {
                            Path="PALLET_ID_SELECT_BOX",
                            FieldType=FormHelperField.FieldTypeRef.String,
                            Control=PalletIdSelectBox,
                            ControlType="SelectBox",
                            Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            }
                        },
                        new FormHelperField()
                        {
                            Path="OPERATION_ID_SELECT_BOX",
                            FieldType=FormHelperField.FieldTypeRef.String,
                            Control=OperationIdSelectBox,
                            ControlType="SelectBox",
                            Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            }
                        },
                        new FormHelperField()
                        {
                            Path="OPERATION_SELECT_BOX",
                            FieldType=FormHelperField.FieldTypeRef.String,
                            Control=OperationSelectBox,
                            ControlType="SelectBox",
                            Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            }
                        },
                        new FormHelperField()
                        {
                            Path="CREATOR_SELECT_BOX",
                            FieldType=FormHelperField.FieldTypeRef.String,
                            Control=CreatorSelectBox,
                            ControlType="SelectBox",
                            Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            }
                        },
                    };

                    Form.SetFields(fields);
                }
            }
        }

        public async void OperationGridLoadItems()
        {
            DisableControls();

            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "ResponsibleStock");
            q.Request.SetParam("Action", "ListOperation");
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
                    var dataSet = ListDataSet.Create(result, "ITEMS");
                    OperationGridDataSet = dataSet;

                    if (OperationGridDataSet!= null && OperationGridDataSet.Items != null && OperationGridDataSet.Items.Count > 0)
                    {
                        foreach (var item in OperationGridDataSet.Items)
                        {
                            if (item.CheckGet("OPERATION_CREATOR").ToInt() == 1)
                            {
                                item.CheckAdd("OPERATION_CREATOR_NAME", "Л-ПАК");
                            }
                            else if (item.CheckGet("OPERATION_CREATOR").ToInt() == 2)
                            {
                                item.CheckAdd("OPERATION_CREATOR_NAME", "СОХ");
                            }
                        }
                    }

                    OperationGrid.UpdateItems(OperationGridDataSet);

                    var palletDataSet = ListDataSet.Create(result, "PALLETS");
                    PalletIdSelectBox.SetItems(palletDataSet, "ID", "PALLET_ID");

                    var operationDataSet = ListDataSet.Create(result, "OPERATIONS");
                    OperationIdSelectBox.SetItems(operationDataSet, "ID", "NAME");

                    OperationSelectBox.SetItems(operationDataSet, "ID", "NAME");
                    OperationSelectBox.Items.Add("-1", "Все типы операций");
                    OperationSelectBox.SetSelectedItemByKey("-1");
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();
        }

        public void SetDefaults()
        {

            OperationGridSelectedItem = new Dictionary<string, string>();
            OperationGridDataSet = new ListDataSet();

            if (Form != null)
            {
                Form.SetDefaults();
            }

            {
                var dictionary = new Dictionary<string, string>();
                dictionary.Add("-1", "Все");
                dictionary.Add("1", "Л-ПАК");
                dictionary.Add("2", "СОХ");
                CreatorSelectBox.Items = dictionary;
                CreatorSelectBox.SetSelectedItemByKey("-1");
            }
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel("[erp]responsible_stock");
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

            if (OperationGrid != null && OperationGrid.Menu != null && OperationGrid.Menu.Count > 0)
            {
                foreach (var manuItem in OperationGrid.Menu)
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

        /// <summary>
        /// Ручное создание новой операции с выбранным поддоном
        /// </summary>
        public async void CreateOperation()
        {
            DisableControls();

            // Если выпадающие списки содержат данные и в каждом из них выбрана запись, то выполняем запрос
            if (
                PalletIdSelectBox != null
                && PalletIdSelectBox.Items != null
                && PalletIdSelectBox.SelectedItem.Key != null
                && OperationIdSelectBox != null
                && OperationIdSelectBox.Items != null
                && OperationIdSelectBox.SelectedItem.Key != null
                )
            {
                var p = new Dictionary<string, string>();
                p.Add("PALLET_RECORD_ID", PalletIdSelectBox.SelectedItem.Key);
                p.Add("OPERATION_ID", OperationIdSelectBox.SelectedItem.Key);
                p.Add("CREATOR", "1");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "ResponsibleStock");
                q.Request.SetParam("Action", "SaveOperation");
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
                        var dataSet = ListDataSet.Create(result, "ITEMS");

                        if (dataSet != null && dataSet.Items.Count > 0)
                        {
                            int palletRecordId = dataSet.Items.First().CheckGet("PALLET_RECORD_ID").ToInt();

                            if (palletRecordId > 0)
                            {
                                Refresh();

                                var palletId = PalletIdSelectBox.Items.FirstOrDefault(x => x.Key == palletRecordId.ToString()).Value;

                                string msg = $"Успешное создание новой записи операции с поддоном {palletId}.";
                                var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                                d.ShowDialog();
                            }
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            EnableControls();
        }

        /// <summary>
        /// При выборе поддона в выпадающем списке фильтруем основной грид операций, оставляя только операции с выбранным поддоном
        /// </summary>
        public void PalletIdSelect()
        {
            var dictionary = new Dictionary<string, string>();
            dictionary.CheckAdd("SEARCH", PalletIdSelectBox.SelectedItem.Value.ToInt().ToString());
            Form.SetValues(dictionary);
            OperationGrid.UpdateItems();



            //if (PalletIdSelectBox != null && PalletIdSelectBox.Items != null)
            //{
            //    int palletId = PalletIdSelectBox.SelectedItem.Value.ToInt();

            //    if (palletId > 0 && OperationGrid != null && OperationGrid.Items != null && OperationGrid.Items.Count > 0)
            //    {

            //    }
            //}
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {

        }

        public void Refresh()
        {
            SetDefaults();
            OperationGridLoadItems();
        }

        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
        }

        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
        }

        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            //FIXME: Нужно сделать документацию
            Central.ShowHelp("/doc/l-pack-erp/");
        }

        private void ResreshButton_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void CreateOperationButton_Click(object sender, RoutedEventArgs e)
        {
            CreateOperation();
        }

        private void PalletIdSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PalletIdSelect();
        }

        private void OperationSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OperationGrid.UpdateItems();
        }

        private void CreatorSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OperationGrid.UpdateItems();
        }
    }
}
