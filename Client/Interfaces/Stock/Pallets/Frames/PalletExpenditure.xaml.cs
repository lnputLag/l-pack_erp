using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Форма редактирования расхода поддонов
    /// </summary>
    public partial class PalletExpenditure : UserControl
    {
        public PalletExpenditure()
        {
            InitializeComponent();

            PlexId = 0;
            TabName = "";
            ReturnTabName = "";
            DeletedIds = new List<int>();
            UsedPalletIds = new List<int>();

            ItemsDS = new ListDataSet();
            ItemsDS.Init();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            SetDefaults();
            InitForm();
            InitGrid();
        }

        /// <summary>
        /// ID накладной расхода поддонов
        /// </summary>
        private int PlexId;

        /// <summary>
        /// Список ID удалённых поддонов
        /// </summary>
        private List<int> DeletedIds;

        /// <summary>
        /// Данные для списка поддонов в накладной
        /// </summary>
        ListDataSet ItemsDS { get; set; }

        /// <summary>
        /// Данные для выпадающего списка поддонов в окне редактирования поддона
        /// </summary>
        public ListDataSet PalletRefDS { get; set; }

        /// <summary>
        /// Имя вкладки, которая становится активной после закрытия формы редактирования
        /// </summary>
        public string ReturnTabName { get; set; }

        /// <summary>
        /// Имя этой вкладки
        /// </summary>
        public string TabName { get; set; }

        /// <summary>
        /// Форма редактирования накладной со списком поддонов
        /// </summary>
        public FormHelper PalletExpForm { get; set; }

        /// <summary>
        /// Список ID поддонов, которые есть в накладной. ID должен быть уникальным.
        /// </summary>
        private List<int> UsedPalletIds { get; set; }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            Num.Text = "";
            InvoiceDt.Text = DateTime.Now.ToString("dd.MM.yyyy");
            Note.Text = "";
            FormStatus.Text = "";
            FactIdSelectBox.Items = new Dictionary<string, string>() {
                { "1", "Л-ПАК ЛИПЕЦК" },
                { "2", "Л-ПАК КАШИРА" },
            };
            FactIdSelectBox.SelectedItem = FactIdSelectBox.Items.First();
        }

        /// <summary>
        /// Инициализация формы
        /// </summary>
        private void InitForm()
        {
            PalletExpForm = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="NUM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Num,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=InvoiceDt,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="FACT_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=FactIdSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Note,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

            };
            PalletExpForm.SetFields(fields);
        }

        /// <summary>
        /// Инициализация таблицы со списком поддонов
        /// </summary>
        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Поддон",
                    Path="NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=120,
                },
                new DataGridHelperColumn
                {
                    Header="Количество, шт",
                    Path="QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=90,
                },
                new DataGridHelperColumn
                {
                    Header="ID поддона",
                    Path="ID_PAL",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ID расхода поддона",
                    Path="PLEI_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Проведен",
                    Path="RECORD_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            // раскраска строк
            PalletGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                {
                    DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        // поддоны выбранного типа проведены
                        if (row["RECORD_FLAG"].ToInt() == 1)
                        {
                            color = HColor.GrayFG;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }
                        return result;
                    }
                },

            };
            PalletGrid.SetColumns(columns);
            PalletGrid.AutoUpdateInterval = 0;
            PalletGrid.Init();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            PalletGrid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };
            PalletGrid.OnDblClick = selectedItem =>
            {

                EditPallet();
            };
        }

        /// <summary>
        /// Формирование окна редактирования
        /// </summary>
        /// <param name="plexId">id накладной расхода поддонов</param>
        /// <param name="shipped">признак, что накладная на отгруженные поддоны</param>
        public void Edit(int plexId = 0, bool shipped = false)
        {
            PlexId = plexId;
            GetData();
            if (shipped)
            {
                Num.IsEnabled = false;
                InvoiceDt.IsEnabled = false;
            }
            ShowTab();
        }

        /// <summary>
        /// Обновление операций с табицей поддонов
        /// </summary>
        /// <param name="selectedItem">выбранная строка</param>
        private void UpdateActions(Dictionary<string, string> selectedItem)
        {
            if (selectedItem != null)
            {
                var canChangeQty = PalletPermissions.HasPermission("change_qty");
                EditButton.IsEnabled = canChangeQty && (selectedItem["RECORD_FLAG"].ToInt() == 0);
                DeleteButton.IsEnabled = canChangeQty && (selectedItem["RECORD_FLAG"].ToInt() == 0);
            }
        }

        /// <summary>
        /// Получение данных
        /// </summary>
        private async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "GetExpenditure");
            q.Request.SetParam("ID", PlexId.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    // поля формы
                    var recordDS = ListDataSet.Create(result, "Record");
                    PalletExpForm.SetValues(recordDS);

                    // таблица с поддонами
                    ItemsDS = ListDataSet.Create(result, "Items");
                    // настроим доступность кнопок редактирования и удаления
                    EditButton.IsEnabled = (ItemsDS.Items.Count != 0);
                    DeleteButton.IsEnabled = (ItemsDS.Items.Count != 0);
                    FactIdSelectBox.IsEnabled = PlexId == 0;
                    PalletGrid.UpdateItems(ItemsDS);
                    foreach (var item in ItemsDS.Items)
                    {
                        UsedPalletIds.Add(item["ID_PAL"].ToInt());
                    }

                    // содержимое справочника поддонов для выпадающего списка
                    PalletRefDS = ListDataSet.Create(result, "PalletRef");
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="obj">сообщение</param>
        private void ProcessMessages(ItemMessage obj)
        {
            if (obj.ReceiverGroup.IndexOf("Stock") > -1)
            {
                if (obj.ReceiverName.IndexOf("PalletExpenditure") > -1)
                {
                    Dictionary<string, string> result = (Dictionary<string, string>)obj.ContextObject;
                    var itemsDStmp = new ListDataSet();
                    itemsDStmp.Items.AddRange(ItemsDS.Items);
                    switch (obj.Action)
                    {
                        case "Insert":
                            int newRowNum = 1;
                            if (ItemsDS.Items.Count > 0)
                            {
                                newRowNum = ItemsDS.Items.Last()["_ROWNUMBER"].ToInt() + 1;
                            }
                            result["_ROWNUMBER"] = newRowNum.ToString();
                            itemsDStmp.Items.Add(result);
                            PalletGrid.UpdateItems(itemsDStmp);
                            break;

                        case "Update":
                            if (PalletGrid.SelectedItem != null)
                            {
                                var selectedPlei = PalletGrid.SelectedItem["PLEI_ID"];
                                foreach (var item in itemsDStmp.Items)
                                {
                                    if (item["PLEI_ID"] == selectedPlei)
                                    {
                                        item["NAME"] = result["NAME"];
                                        item["ID_PAL"] = result["ID_PAL"];
                                        item["QTY"] = result["QTY"];
                                    }
                                }
                                PalletGrid.UpdateItems(itemsDStmp);
                            }
                            break;
                    }
                    ItemsDS.Items = itemsDStmp.Items;
                    UsedPalletIds.Clear();
                    foreach (var item in ItemsDS.Items)
                    {
                        UsedPalletIds.Add(item["ID_PAL"].ToInt());
                    }

                    EditButton.IsEnabled = (ItemsDS.Items.Count != 0);
                    DeleteButton.IsEnabled = (ItemsDS.Items.Count != 0);
                }
            }
        }

        /// <summary>
        /// Отображение вкладки с формой редактирования
        /// </summary>
        private void ShowTab()
        {
            string title = $"Накладная расхода поддонов {PlexId}";
            if (PlexId == 0)
            {
                title = "Новая накладная расхода поддонов";
            }
            TabName = $"PalletExp_{PlexId}";
            Central.WM.AddTab(TabName, title, true, "add", this);
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        private void Close()
        {
            Central.WM.RemoveTab(TabName);
            Destroy();
        }

        /// <summary>
        /// Деструктор. Завершает вспомогательные процессы
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Stock",
                ReceiverName = "",
                SenderName = "PalletExpenditure",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            GoBack();
        }

        /// <summary>
        /// Возврат на фрейм, откуда был вызван данный фрейм
        /// </summary>
        public void GoBack()
        {
            if (!string.IsNullOrEmpty(ReturnTabName))
            {
                Central.WM.SetActive(ReturnTabName, true);
                ReturnTabName = "";
            }
        }

        /// <summary>
        /// Сохранение данных формы редактирования
        /// </summary>
        private void Save()
        {
            bool resume = true;
            if (resume)
            {
                if (ItemsDS.Items.Count == 0)
                {
                    FormStatus.Text = "Пожалуйста, добавьте поддоны";
                    resume = false;
                }
            }

            if (resume)
            {
                var res = PalletExpForm.GetValues();
                res.Add("ID", PlexId.ToString());
                    
                // из таблицы передаём id поддона и количество
                var tbl = new List<List<string>>();
                foreach (var item in PalletGrid.Items)
                {
                    tbl.Add(new List<string>()
                    {
                        item["PLEI_ID"],
                        item["ID_PAL"],
                        item["QTY"],
                        item["PRICE"],
                        
                    });
                }
                res.Add("Pallets", JsonConvert.SerializeObject(tbl));
                // список id удалённых записей из таблицы
                if (DeletedIds.Count > 0)
                {
                    res.Add("Deleted", JsonConvert.SerializeObject(DeletedIds));
                }

                SaveData(res);
            }
        }

        /// <summary>
        /// Отправка данных для записис в БД
        /// </summary>
        /// <param name="p"></param>
        private async void SaveData(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "SaveExpenditure");
            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                //отправляем сообщение Гриду о необходимости обновить данные
                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = "Stock",
                    ReceiverName = "PalletExpenditureList",
                    SenderName = "PalletExpenditureEdit",
                    Action = "Refresh",
                });
                Close();
            }
            else
            {
                q.ProcessError();
            }
        }

        private void AddPallet()
        {
            var EditItemForm = new ExpenditureItemEdit();
            EditItemForm.PalletRefDS = PalletRefDS;
            EditItemForm.UsedPalletIds.AddRange(UsedPalletIds);
            EditItemForm.Edit();
        }

        private void EditPallet()
        {
            if (PalletGrid.SelectedItem != null && (PalletGrid.SelectedItem["RECORD_FLAG"].ToInt() == 0))
            {
                var EditItemForm = new ExpenditureItemEdit();
                EditItemForm.PalletRefDS = PalletRefDS;
                EditItemForm.UsedPalletIds.AddRange(UsedPalletIds);
                EditItemForm.Id = PalletGrid.SelectedItem["ID_PAL"].ToInt();
                EditItemForm.Quantity = PalletGrid.SelectedItem["QTY"].ToInt();
                EditItemForm.Edit();
            }
        }

        private void DeletePallet()
        {
            var itemsDStmp = new ListDataSet();
            itemsDStmp.Items.AddRange(ItemsDS.Items);
            int selectedPlei = PalletGrid.SelectedItem["PLEI_ID"].ToInt();
            foreach (var item in ItemsDS.Items)
            {
                if (item["PLEI_ID"].ToInt() == selectedPlei)
                {
                    itemsDStmp.Items.Remove(item);
                    DeletedIds.Add(selectedPlei);
                }
            }
            PalletGrid.UpdateItems(itemsDStmp);
            ItemsDS.Items = itemsDStmp.Items;
            UsedPalletIds.Clear();
            foreach (var item in ItemsDS.Items)
            {
                UsedPalletIds.Add(item["ID_PAL"].ToInt());
            }

            EditButton.IsEnabled = (ItemsDS.Items.Count != 0);
            DeleteButton.IsEnabled = (ItemsDS.Items.Count != 0);
        }

        /// <summary>
        /// Обработчик нажатия на кнопку сохранения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку отмены
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Обработчик нажатияна кнопку добавления поддона
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddPallet();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку изменения поддона
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            EditPallet();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку удаления поддона
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (PalletGrid.SelectedItem.Count != 0)
            {
                var dw = new DialogWindow("Удалить поддон из списка?", "Удаление поддона", "", DialogWindowButtons.NoYes);
                if (dw.ShowDialog() == true)
                {
                    DeletePallet();
                }
            }
            else
            {
                var dw = new DialogWindow("Не выбран поддон", "Удаление поддона");
                dw.ShowDialog();
            }
        }
    }
}
