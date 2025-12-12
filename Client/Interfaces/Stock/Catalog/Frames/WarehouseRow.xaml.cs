using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using Client.Interfaces.Main;
using Org.BouncyCastle.Asn1.Cms;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// интерфейс для редактирования и создания рядов
    /// </summary>
    /// <author>eletskikh_ya</author>
    public partial class WarehouseRow : ControlBase
    {
        public WarehouseRow()
        {
            ControlTitle = "Редактирование ряда";
            DocumentationUrl = "/doc/l-pack-erp/";
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

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
                Init();
                SetDefaults();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
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
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// идентификатор ряда, с которой работает форма
        /// (primary key записи таблицы wms_row)
        /// </summary>
        public int RowId { get; set; }

        /// <summary>
        /// Идентификатор склада WMS
        /// </summary>
        public int WarehouseId { get; set; }

        public void SetDefaults()
        {
            Form.SetDefaults();
            GetWarehouseList();
            SetWarehouse();

            if (RowId > 0)
            {
                GetData();
                RowNumTextBox.IsReadOnly = true;
            }
            else
            {
                RowNumTextBox.Focus();
            }
        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="ROW_NUM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=RowNumTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.ToUpperCase, null },
                        { FormHelperField.FieldFilterRef.AlphaDigitOnly, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 8 },
                    },
                },
                new FormHelperField()
                {
                    Path="WMWA_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=WarehouseSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="ORDER_NUM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=OrderNumTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },

                },
                new FormHelperField()
                {
                    Path="PRIORITY",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=PriorityTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = Status;
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
        }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 1;

            this.FrameName = $"{FrameName}_{RowId}";
            if (RowId == 0)
            {
                Central.WM.Show(FrameName, "Новый ряд", true, "add", this);
            }
            else
            {
                Central.WM.Show(FrameName, $"Ряд {RowId}", true, "add", this);
            }
        }

        /// <summary>
        /// получение данных с сервера
        /// </summary>
        public async void GetData()
        {
            DisableControls();

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("WMRO_ID", RowId.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Row");
            q.Request.SetParam("Action", "Get");
            q.Request.SetParams(p);

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
                    Form.SetValues(ds);
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.Close(FrameName);

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "wms",
                ReceiverName = "",
                SenderName = "WarehouseRow",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
        }

        /// <summary>
        /// Получаем список складов для выпадающего списка
        /// </summary>
        public void GetWarehouseList()
        {
            FormHelper.ComboBoxInitHelper(WarehouseSelectBox, "Warehouse", "Warehouse", "List", "WMWA_ID", "WAREHOUSE", null, true, true);
        }

        /// <summary>
        /// Установка выбранного склада в выпадающем списке
        /// </summary>
        /// <param name="warehouseId"></param>
        public void SetWarehouse()
        {
            if (WarehouseId > 0)
            {
                Form.SetValueByPath("WMWA_ID", $"{WarehouseId}");
            }
        }

        /// <summary>
        /// Сохранение данных по ряду
        /// </summary>
        public void Save()
        {
            bool resume = true;
            string error = "";

            //стандартная валидация данных средствами формы
            if (Form.Validate())
            {
                var v = Form.GetValues();
                //отправка данных
                SaveData(v);
            }
            else
            {
                Form.SetStatus(error, 1);
            }
        }

        /// <summary>
        /// отпаравка данных на сервер
        /// </summary>
        public void SaveData(Dictionary<string, string> p)
        {
            DisableControls();

            p.Add("WMRO_ID", RowId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Row");
            q.Request.SetParam("Action", "Save");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    var id = ds.GetFirstItemValueByKey("ID").ToInt();
                    if (id != 0)
                    {
                        //отправляем сообщение гриду о необходимости обновить данные
                        Messenger.Default.Send(new ItemMessage()
                        {
                            ReceiverGroup = "WMS",
                            ReceiverName = "WMS_list",
                            SenderName = "WarehouseRow",
                            Action = "Refresh",
                            Message = $"{id}",
                        });

                        Close();
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
