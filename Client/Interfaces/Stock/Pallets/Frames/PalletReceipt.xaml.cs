using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Common.LPackClientRequest;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Логика окна редактирования прихода поддонов
    /// </summary>
    /// <author>Рясной П.В.</author>
    public partial class PalletReceipt : UserControl
    {
        public PalletReceipt()
        {
            InitializeComponent();

            PlreId = 0;
            ReturnTabName = "";
            TabName = "";
            DeletedIds = new List<int>();
            UsedPalletIds = new List<int>();
            DeletedFileIds = new Dictionary<int, string>();

            ItemsDS = new ListDataSet();
            ItemsDS.Init();
            FilesDS = new ListDataSet();
            FilesDS.Init();
            PalletRefDS = new ListDataSet();
            PalletRefDS.Init();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitForm();
            InitGrid();
            InitFileGrid();
            SetDefaults();
        }

        /// <summary>
        /// ID накладной прихода поддонов
        /// </summary>
        private int PlreId;

        /// <summary>
        /// Список ID удалённых поддонов
        /// </summary>
        private List<int> DeletedIds;

        /// <summary>
        /// Список ID удалённых поддонов
        /// </summary>
        private Dictionary<int, string> DeletedFileIds;

        /// <summary>
        /// Данные для списка поддонов в накладной
        /// </summary>
        ListDataSet ItemsDS { get; set; }

        /// <summary>
        /// Данные для списка приложенных файлов
        /// </summary>
        ListDataSet FilesDS { get; set; }

        /// <summary>
        /// Данные для выпадающего списка поддонов в окне редактирования поддона
        /// </summary>
        public ListDataSet PalletRefDS { get; set; }

        /// <summary>
        /// Форма редактирования накладной прихода поддонов со списком поддонов
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Имя вкладки, которая становится активной после закрытия формы редактирования
        /// </summary>
        public string ReturnTabName { get; set; }

        /// <summary>
        /// Имя этой вкладки
        /// </summary>
        public string TabName { get; set; }

        /// <summary>
        /// Список ID поддонов, которые есть в накладной. ID должен быть уникальным.
        /// </summary>
        private List<int> UsedPalletIds { get; set; }



        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            FormStatus.Text = "";
            Form.SetDefaults();
            SourceType.Items = PalletSourceTypes.Items;
            SourceType.SelectedItem = PalletSourceTypes.Items.First();
            Status.Items = PalletReceiptStatus.Items;
            Status.SelectedItem = PalletReceiptStatus.Items.First();
            FactIdSelectBox.Items = new Dictionary<string, string>() {
                { "1", "Л-ПАК ЛИПЕЦК" },
                { "2", "Л-ПАК КАШИРА" },
            };
            FactIdSelectBox.SelectedItem = FactIdSelectBox.Items.First();
        }

        /// <summary>
        /// Инициализация формы редактирования наклкдной прихода поддонов
        /// </summary>
        private void InitForm()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="SOURCE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=SourceType,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="ID_POK",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Buyer,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ID_POST",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Supplier,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
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
                    Control=ReceiptDt,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STATUS",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Status,
                    ControlType="SelectBox",
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
            Form.SetFields(fields);
            Form.OnValidate=(bool valid, string message) =>
            {
                if(valid)
                {
                    //SaveButton.IsEnabled=true;
                    FormStatus.Text="";
                }
                else
                {
                    //SaveButton.IsEnabled=false;
                    FormStatus.Text="Не все поля заполнены верно";
                }
            };
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
                    MinWidth=100,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Количество по документу",
                    Path="DOC_QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Брак",
                    Path="QTY_DEFECT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=150,
                    Stylers=new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row["QTY_DEFECT"].ToInt() > 0)
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
                new DataGridHelperColumn
                {
                    Header="ID поддона",
                    Path="ID_PAL",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ID прихода поддона",
                    Path="PLRI_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            PalletGrid.SetColumns(columns);
            PalletGrid.Init();

            PalletGrid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };

            PalletGrid.OnDblClick = selectedItem =>
            {
                bool result = false;

                if (Central.User.Roles.Count > 0)
                {
                    foreach (var item in Central.User.Roles)
                    {
                        if (item.Value.Code.Contains("[f]admin"))
                        {
                            result = true;
                        }
                    }
                }

                if (result)
                {
                    EditPallet();
                }
            };
        }

        private void InitFileGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Название",
                    Path="ORIGINAL_FILE_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=100,
                    MaxWidth=600,
                },
                new DataGridHelperColumn
                {
                    Header="ID файла",
                    Path="PLRF_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Имя файла",
                    Path="FILE_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Hidden=true,
                },
            };
            FileGrid.SetColumns(columns);
            FileGrid.Init();
        }

        private void UpdateActions(Dictionary<string, string> selectedItem)
        {
            if (selectedItem != null)
            {
                if (selectedItem.CheckGet("RECORD_FLAG") == "0")
                {
                    EditButton.IsEnabled = true;
                }
                else
                {
                    EditButton.IsEnabled = false;
                }
            }
        }


        /// <summary>
        /// Формирование окна редактирования
        /// </summary>
        /// <param name="plreId">id накладной расхода поддонов</param>
        public void Edit(int plreId = 0)
        {
            PlreId = plreId;
            GetData();
            Show();
        }

        /// <summary>
        /// Получение данных
        /// </summary>
        private async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "GetReceipt");
            q.Request.SetParam("ID", PlreId.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    // список покупателей
                    var BuyerDS = new ListDataSet();
                    BuyerDS.Init();
                    BuyerDS = ListDataSet.Create(result, "BuyerRef");
                    Buyer.Items = BuyerDS.GetItemsList("ID", "NAME");

                    // список поставщиков
                    var SupplierDS = new ListDataSet();
                    SupplierDS.Init();
                    SupplierDS = ListDataSet.Create(result, "SupplierRef");
                    Supplier.Items = SupplierDS.GetItemsList("ID", "NAME");

                    // содержимое справочника поддонов для выпадающего списка
                    PalletRefDS = ListDataSet.Create(result, "PalletRef");

                    // при редактировании заполним поля
                    var rec = new Dictionary<string, string>();
                    if (PlreId > 0)
                    {
                        var recordDS = new ListDataSet();
                        recordDS.Init();
                        recordDS = ListDataSet.Create(result, "Record");
                        Form.SetValues(recordDS);
                        rec = recordDS.Items.First();
                    }
                    ItemsDS = ListDataSet.Create(result, "Items");
                    FilesDS = ListDataSet.Create(result, "ListFiles");

                    // настроим доступность полей и кнопок
                    // тип можно выбирать только для новых накладных прихода
                    SourceType.IsEnabled = (PlreId == 0);
                    // Кнопки в панели списка поддонов доступны только для статуса на приемке
                    bool isIncoming = rec.CheckGet("STATUS").ToInt() == PalletReceiptStatus.Incoming;
                    AddButton.IsEnabled = isIncoming;
                    EditButton.IsEnabled = isIncoming && (ItemsDS.Items.Count != 0);
                    DeleteButton.IsEnabled = isIncoming && (ItemsDS.Items.Count != 0);
                    FactIdSelectBox.IsEnabled = isIncoming;

                    PalletGrid.UpdateItems(ItemsDS);

                    foreach(var item in ItemsDS.Items)
                    {
                        UsedPalletIds.Add(item["ID_PAL"].ToInt());
                    }

                    OpenFileButton.IsEnabled = (FilesDS.Items.Count != 0);
                    DeleteFileButton.IsEnabled = (FilesDS.Items.Count != 0);
                    FileGrid.UpdateItems(FilesDS);
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
                if (obj.ReceiverName.IndexOf("PalletReceipt") > -1)
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
                            PalletGrid.SelectRowByKey(newRowNum, "_ROWNUMBER");
                            break;

                        case "Update":
                            if (PalletGrid.SelectedItem != null)
                            {
                                var selectedRow = PalletGrid.SelectedItem["_ROWNUMBER"];
                                foreach (var item in itemsDStmp.Items)
                                {
                                    if (item["_ROWNUMBER"] == selectedRow)
                                    {
                                        item["NAME"] = result["NAME"];
                                        item["ID_PAL"] = result["ID_PAL"];
                                        item["QTY"] = result["QTY"];
                                        item["DOC_QTY"] = result["DOC_QTY"];
                                        item["QTY_DEFECT"] = result["QTY_DEFECT"];
                                    }
                                }
                                PalletGrid.UpdateItems(itemsDStmp);
                                PalletGrid.SelectRowByKey(selectedRow.ToInt(), "_ROWNUMBER");
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

       


        public void AddPallet()
        {
            var EditItemForm = new ReceiptItemEdit();
            EditItemForm.ReturnTabName = TabName;
            EditItemForm.PalletRefDS = PalletRefDS;
            EditItemForm.UsedPalletIds.AddRange(UsedPalletIds);
            EditItemForm.Edit();
        }

        public void EditPallet()
        {
            if (PalletGrid.SelectedItem != null)
            {
                var EditItemForm = new ReceiptItemEdit();
                EditItemForm.ReturnTabName = TabName;
                EditItemForm.PalletRefDS = PalletRefDS;
                EditItemForm.UsedPalletIds.AddRange(UsedPalletIds);
                if (!string.IsNullOrEmpty(PalletGrid.SelectedItem["DOC_QTY"]))
                {
                    EditItemForm.DocQuantity = PalletGrid.SelectedItem["DOC_QTY"].ToInt();
                }
                EditItemForm.Quantity = PalletGrid.SelectedItem["QTY"].ToInt();
                EditItemForm.Edit(PalletGrid.SelectedItem["ID_PAL"].ToInt());
            }
        }

        /// <summary>
        /// Валидация и проверки перед сохранением
        /// </summary>
        private void Save()
        {
            if (Form.Validate())
            {
                bool resume = true;
                var v = Form.GetValues();

                // дополнительные проверки

                if (resume)
                {
                    if ((SourceType.SelectedItem.Key.ToInt() == PalletSourceTypes.Purchase)
                        || (SourceType.SelectedItem.Key.ToInt() == PalletSourceTypes.Return))
                    {
                        if (resume)
                        {
                            if (string.IsNullOrEmpty(v.CheckGet("NUM")))
                            {
                                FormStatus.Text = "Укажите номер документа";
                                resume = false;
                            }
                        }
                        
                        if (resume)
                        {
                            if (string.IsNullOrEmpty(v.CheckGet("DT")))
                            {
                                FormStatus.Text = "Выберите дату";
                                resume = false;
                            }
                        }
                       
                    }
                }

                if (resume && (SourceType.SelectedItem.Key.ToInt() == PalletSourceTypes.Purchase))
                {
                    if (string.IsNullOrEmpty(Supplier.SelectedItem.Key))
                    {
                        FormStatus.Text = "Укажите поставщика";
                        resume = false;
                    }
                }

                if (resume && (SourceType.SelectedItem.Key.ToInt() == PalletSourceTypes.Return))
                {
                    if (string.IsNullOrEmpty(Buyer.SelectedItem.Key))
                    {
                        FormStatus.Text = "Укажите покупателя";
                        resume = false;
                    }
                }

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
                    if (SourceType.SelectedItem.Key.ToInt() == PalletSourceTypes.Purchase)
                    {
                        foreach (var row in ItemsDS.Items)
                        {
                            if (string.IsNullOrEmpty(row.CheckGet("DOC_QTY")))
                            {
                                FormStatus.Text = "Пожалуйста, заполните количество по документу для всех поддонов";
                                resume = false;
                                break;
                            }
                        }
                    }
                }

                if (resume)
                {
                    SaveData();
                }
            }
        }

        /// <summary>
        /// Сохранение данных
        /// </summary>
        private async void SaveData()
        {
            var res = Form.GetValues();
            res.Add("ID", PlreId.ToString());

            string recordFlag = "0";
            // Если поставили статус накладной Принят, то проводим поддоны
            if (Status.SelectedItem.Key.ToInt() == 1)
                recordFlag = "1";

            // из таблицы передаём id поддона и количество
            var tbl = new List<List<string>>();
            foreach (var item in PalletGrid.Items)
            {
                tbl.Add(new List<string>()
                    {
                        item["PLRI_ID"],
                        item["ID_PAL"],
                        item["DOC_QTY"],
                        item["QTY"],
                        recordFlag,
                    });
            }
            res.Add("Pallets", JsonConvert.SerializeObject(tbl));
            // список id удалённых записей из таблицы
            if (DeletedIds.Count > 0)
            {
                res.Add("Deleted", JsonConvert.SerializeObject(DeletedIds));
            }

            if (DeletedFileIds.Count > 0)
            {
                res.Add("DeletedFiles", JsonConvert.SerializeObject(DeletedFileIds));
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "SaveReceipt");
            q.Request.SetParams(res);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    string PlreIdStr = PlreId.ToString();
                    // для новой накладной получим из ответа ID созданной накладной
                    if (PlreId == 0)
                    {
                        var resultDS = ListDataSet.Create(result, "Items");
                        if (resultDS.Items.Count > 0)
                        {
                            var item = resultDS.Items.First();
                            PlreIdStr = item.CheckGet("PlreId");
                        }
                    }

                    // отправляем на сервер новые файлы
                    foreach (var item in FilesDS.Items)
                    {
                        if (item["PLRF_ID"].ToInt() == 0)
                        {
                            if (File.Exists(item["FILE_NAME"]))
                            {
                                SaveReceiptFile(PlreIdStr, item);
                            }
                        }
                    }

                    //отправляем сообщение Гриду о необходимости обновить данные
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "Stock",
                        ReceiverName = "PalletReceiptList",
                        SenderName = "PalletReceiptEdit",
                        Action = "Refresh",
                    });

                    Close();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        private async void SaveReceiptFile(string plreId, Dictionary<string,string> item)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "SaveReceiptFile");
            q.Request.SetParam("ID", plreId);
            q.Request.Type = RequestTypeRef.MultipartForm;
            q.Request.UploadFilePath = item["FILE_NAME"];

            await Task.Run(() =>
            {
                q.DoQuery();
            });
        }

        private void DeletePallet()
        {
            if (PalletGrid.SelectedItem.Count != 0)
            {
                var dw = new DialogWindow($"Удалить поддон \"{PalletGrid.SelectedItem["NAME"]}\" из списка?", "Удаление поддона", "", DialogWindowButtons.NoYes);
                if (dw.ShowDialog() == true)
                {
                    var itemsDStmp = new ListDataSet();
                    itemsDStmp.Items.AddRange(ItemsDS.Items);
                    int selectedPlri = PalletGrid.SelectedItem["PLRI_ID"].ToInt();
                    if (selectedPlri > 0)
                    {
                        DeletedIds.Add(selectedPlri);
                    }
                    itemsDStmp.Items.Remove(PalletGrid.SelectedItem);
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
            }
            else
            {
                var dw = new DialogWindow("Не выбран поддон", "Удаление поддона");
                dw.ShowDialog();
            }
        }

        /// <summary>
        /// Отображение фрейма
        /// </summary>
        private void Show()
        {
            string title = $"Накладная #{PlreId}";
            if (PlreId == 0)
                title = "Новая накладная";
            TabName = $"PalletReceipt_{PlreId}";
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
                SenderName = "PalletReceipt",
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
        /// Обработчик нажатия на кнопку отмены
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
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
        /// Обработчик нажатия на кнопку добавления поддона
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddPallet();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку изменения данных по поддону
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
            DeletePallet();
        }

        private void SourceType_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            int source = SourceType.SelectedItem.Key.ToInt();
            var emptyItem = new KeyValuePair<string, string>("", "");
            bool isPurchase = source == PalletSourceTypes.Purchase;
            bool isReturn = source == PalletSourceTypes.Return;


            NumLabel.Content = "Номер документа:";
            DateLabel.Content = "Дата:";
            // поле покупатель доступно только для возврата
            Buyer.IsEnabled = isReturn;
            BuyerLabel.IsEnabled = isReturn;
            // поле поставщик доступно только для поставки
            Supplier.IsEnabled = isPurchase;
            SupplierLabel.IsEnabled = isPurchase;

            // поставка
            if (source == PalletSourceTypes.Purchase)
            {
                Buyer.SetSelectedItem(emptyItem);
                NumLabel.Content += " *";
                DateLabel.Content += " *";
            }

            // возврат
            if (source == PalletSourceTypes.Return)
            {
                Supplier.SetSelectedItem(emptyItem);
                NumLabel.Content += " *";
                DateLabel.Content += " *";
            }


            // данные документа доступны только для поставки, в остальных случаях не имеют смысл
            bool fieldEnabled = isPurchase || isReturn || (source == PalletSourceTypes.Giving);
            NumLabel.IsEnabled = fieldEnabled;
            Num.IsEnabled = fieldEnabled;
            DateLabel.IsEnabled = fieldEnabled;
            DateBlock.IsEnabled = fieldEnabled;

            // для новых накладных для поставки статус по умолчанию На приемке, в остальных случаях - статус Приняты
            if (PlreId == 0)
            {
                if (isPurchase)
                {
                    Status.SetSelectedItem(PalletReceiptStatus.IncomingItem);
                }
                else
                {
                    Status.SetSelectedItem(PalletReceiptStatus.AcceptedItem);
                }
            }
        }

        private void AddFile()
        {
            var fd = new OpenFileDialog();
            var fdResult = (bool)fd.ShowDialog();
            if (fdResult)
            {
                var fileName = Path.GetFileName(fd.FileName);
                bool resume = true;

                foreach (var fn in FilesDS.Items)
                {
                    if (fileName == fn["ORIGINAL_FILE_NAME"])
                    {
                        resume = false;
                        var dw = new DialogWindow("Такой файл уже есть в списке", "Добавление файла");
                        dw.ShowDialog();
                    }
                }

                if (resume)
                {
                    var filesDStmp = new ListDataSet();
                    filesDStmp.Items.AddRange(FilesDS.Items);
                    int newRowNum = 1;
                    if (FilesDS.Items.Count > 0)
                    {
                        newRowNum = FilesDS.Items.Last()["_ROWNUMBER"].ToInt() + 1;
                    }
                    filesDStmp.Items.Add(new Dictionary<string, string>()
                    {
                        { "_ROWNUMBER", newRowNum.ToString() },
                        { "ORIGINAL_FILE_NAME", fileName },
                        { "PLRF_ID", "0" },
                        { "FILE_NAME", fd.FileName },
                    });
                    FileGrid.UpdateItems(filesDStmp);
                    FileGrid.SelectRowByKey(newRowNum, "_ROWNUMBER");
                    FilesDS.Items = filesDStmp.Items;

                    OpenFileButton.IsEnabled = (FilesDS.Items.Count != 0);
                    DeleteFileButton.IsEnabled = (FilesDS.Items.Count != 0);
                }
            }
        }

        private void DeleteFile()
        {
            if (FileGrid.SelectedItem != null)
            {
                var dw = new DialogWindow($"Удалить файл \"{FileGrid.SelectedItem["ORIGINAL_FILE_NAME"]}\" из списка?", "Удаление файла", "", DialogWindowButtons.NoYes);
                if (dw.ShowDialog() == true)
                {
                    var filesDStmp = new ListDataSet();
                    filesDStmp.Items.AddRange(FilesDS.Items);
                    int selectedPlrf = FileGrid.SelectedItem["PLRF_ID"].ToInt();
                    if (selectedPlrf > 0)
                    {
                        DeletedFileIds.Add(selectedPlrf, FileGrid.SelectedItem["FILE_NAME"]);
                    }
                    filesDStmp.Items.Remove(FileGrid.SelectedItem);

                    FileGrid.UpdateItems(filesDStmp);
                    FilesDS.Items = filesDStmp.Items;

                    OpenFileButton.IsEnabled = (FilesDS.Items.Count != 0);
                    DeleteFileButton.IsEnabled = (FilesDS.Items.Count != 0);
                }
            }
            else
            {
                var dw = new DialogWindow("Не выбран файл", "Удаление файла");
                dw.ShowDialog();
            }
        }

        private async void OpenReceiptFile()
        {
            if (FileGrid.SelectedItem != null)
            {
                if (FileGrid.SelectedItem.CheckGet("PLRF_ID").ToInt() > 0)
                {
                    // загрузка сохранённого файла
                    var fileName = FileGrid.SelectedItem["FILE_NAME"];
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Stock");
                        q.Request.SetParam("Object", "Pallet");
                        q.Request.SetParam("Action", "OpenReceiptFile");
                        q.Request.SetParam("ID", PlreId.ToString());
                        q.Request.SetParam("FILE_NAME", fileName);

                        await Task.Run(() =>
                        {
                            q.DoQuery();
                        });


                        if(q.Answer.Status == 0)
                        {
                            Central.OpenFile(q.Answer.DownloadFilePath);
                        }
                        else
                        {
                            q.ProcessError();
                        }

                        /*
                        if (q.Answer.Status == 0)
                        {
                            var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(q.Answer.Data);
                            if (result != null)
                            {
                                if (result.Count > 0)
                                {
                                    if (result.ContainsKey("documentFile"))
                                    {
                                        Central.OpenFile(result["documentFile"]);
                                    }
                                }
                            }
                        }
                        else
                        {
                            q.ProcessError();
                        }
                        */
                    }
                }
                else
                {
                    // загрузка несохранённого файла
                    Central.OpenFile(FileGrid.SelectedItem["FILE_NAME"]);
                }
            }
            else
            {
                var dw = new DialogWindow("Не выбран файл", "Открытие файла");
                dw.ShowDialog();
            }

        }

        private void AddFileButton_Click(object sender, RoutedEventArgs e)
        {
            AddFile();
        }

        private void DeleteFileButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteFile();
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenReceiptFile();
        }
    }
}
