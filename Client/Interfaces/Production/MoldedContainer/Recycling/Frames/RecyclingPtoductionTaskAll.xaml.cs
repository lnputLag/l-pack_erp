using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Xpf.Core;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.MoldedContainer
{
    /// <summary>
    /// возращение ранее выполненного задания в работу
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    /// <released>2024-05-14</released>
    /// <changed>2024-05-14</changed>
    public partial class RecyclingPtoductionTaskAll : UserControl
    {
        public RecyclingPtoductionTaskAll()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);
            Loaded += OnLoad;
            SetDefaults();
            RecyclingPtoductionTaskInit();


            IdSt = "";
            Title = "Список выполненных заданий";
        }

        public List<DataGridHelperColumn> Columns { get; private set; }

        /// <summary>
        /// форма с полями для фильтрации данных
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// имя хоста
        /// </summary>
        public string IdSt { get; set; }
        public string Title { get; set; }

        /// <summary>
        /// флаг поднимается на время ожидания данных от сервера
        /// </summary>
        private bool LoadingData { get; set; }
                
        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void RecyclingPtoductionTaskInit()
        {
            ////инициализация формы
            {
                Form = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                {

                };

                Form.SetFields(fields);

                //после установки значений
                Form.AfterSet = (Dictionary<string, string> v) =>
                {
                    //фокус на кнопку обновления
                    RefreshButton.Focus();
                };
            }

            //инициализация грида
            //колонки грида
            Columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="TASK_ID",
                    Description="(prot_id)",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Номер",
                    Path="TASK_NUMBER",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Станок",
                    Path="MACHINE_ID",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                    Visible = false,
                },
                new DataGridHelperColumn
                {
                    Header="Изделие",
                    Path="GOODS_NAME",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=50,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="GOODS_CODE",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Схема производства",
                    Path="PRODUCTION_SCHEME_NAME",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="TASK_STATUS_TITLE",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="TASK_STATUS_ID",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=2,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header="Количество, шт",
                    Path="TASK_QUANTITY",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Произведено, шт",
                    Path="LABEL_QTY",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Оприходовано, шт",
                    Path="PRIHOD_QTY",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Списано, шт",
                    Path="RASHOD_QTY",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Заявка",
                    Path="ORDER_TITLE",
                    Description="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=40,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="ORDER_NOTE_GENERAL",
                    Description="примечание ОПП и складу",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Прим. приостановки ПЗ",
                    Path="SUSPEND_NOTE",
                    Description="Прим. приостановки ПЗ",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                 new DataGridHelperColumn
                {
                    Header="ИДПЗ",
                    Path="TASK_ID2",
                    Description="(proiz_zad)",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="ИД позиции заявки",
                    Path="ORDER_POSITION_ID",
                    Description="(idorderdates)",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="ИД изделия",
                    Path="GOODS_ID",
                    Description="(id2)",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Количество на паллете",
                    Path="PER_PALLET_QTY",
                    Description="(tc.per_pallet_qty)",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                    Hidden=true,
                },
            };

            RecyclingPtoductionTaskAllGrid.SetColumns(Columns);
            RecyclingPtoductionTaskAllGrid.SetPrimaryKey("TASK_ID");
            // RecyclingPtoductionTaskAllGrid.SetSorting("ON_DATE", ListSortDirection.Descending);
            RecyclingPtoductionTaskAllGrid.SearchText = SearchText;
            RecyclingPtoductionTaskAllGrid.AutoUpdateInterval = 60;
            RecyclingPtoductionTaskAllGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            //данные грида
            RecyclingPtoductionTaskAllGrid.OnLoadItems = RecyclingPtoductionTaskAllLoadItems;

            RecyclingPtoductionTaskAllGrid.Init();
        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            RecyclingPtoductionTaskAllGrid.Destruct();
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
        }

        /// <summary>
        /// получение записей 
        /// </summary>
        public async void RecyclingPtoductionTaskAllLoadItems()
        {
            bool resume = true;

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "MoldedContainer");
                q.Request.SetParam("Object", "Recycling");
                q.Request.SetParam("Action", "AllList");
                q.Request.SetParam("MACHINE_ID", IdSt);

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
                        RecyclingPtoductionTaskAllGrid.UpdateItems(ds);
                    }
                }
            }
        }

        
        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            RecyclingPtoductionTaskAllGrid.ShowSplash();
            LoadingData = true;
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            RecyclingPtoductionTaskAllGrid.HideSplash();
            LoadingData = false;
        }


        public void Edit()
        {
            RecyclingPtoductionTaskAllGrid.LoadItems();
            Show();
        }

        /// <summary>
        /// Отображение вкладки с формой редактирования данных водителя
        /// </summary>
        public void Show()
        {
            string tabTitle = $"{Title}";
            var tabName = GetFrameName();
            Central.WM.AddTab(tabName, tabTitle, true, "add", this);

        }

        /// <summary>
        /// Закрытие фрейма
        /// </summary>
        public void Close()
        {
            var tabName = GetFrameName();
            Central.WM.RemoveTab(tabName);
            Destroy();
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            var tabName = GetFrameName();
            Central.WM.SetActive(tabName);
        }


        public string GetFrameName()
        {
            var result = "";
            result = $"production_task_all_{IdSt}";
            result = result.MakeSafeName();
            return result;
        }

        /// <summary>
        /// обновление методов работы с выбранной в гриде строкой
        /// </summary>
        /// <param name="selectedItem"></param>
        public void UpdateActions(Dictionary<string, string> selectedItem)
        {
        }

        /// <summary>
        /// обработка ввода с клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
        }

        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }


        /// <summary>
        /// отмена
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// нажали вернуть в работу
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReturnTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var task_name = RecyclingPtoductionTaskAllGrid.SelectedItem.CheckGet("TASK_NUMBER").ToString();

            var dw = new DialogWindow($"Вы действительно хотите вернуть производственное задание №{task_name}?", "Работа с ПЗ", "", DialogWindowButtons.NoYes);
            if (dw.ShowDialog() == true)
            {
                DataSave();
            }

        }

            /// <summary>
            /// возвращаем выбранное задание в очередь
            /// </summary>
            /// <param name="p"></param>
            public async void DataSave()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "TaskStatusSave");

            var p = new Dictionary<string, string>();
            {
                p.Add("TASK_ID", RecyclingPtoductionTaskAllGrid.SelectedItem.CheckGet("TASK_ID").ToInt().ToString());
                p.Add("PRTS_ID", "3");
                p.Add("SUSPEND_NOTE", RecyclingPtoductionTaskAllGrid.SelectedItem.CheckGet("SUSPEND_NOTE").ToString());
            }

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
                    //Central.Msg.SendMessage(new ItemMessage()
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "recycling_control",
                        ReceiverName = "",
                        SenderName = "",
                        Action = "refresh",
                        Message = "",
                    });

                    Close();
                }
            }
            else
            {
                q.ProcessError();
            }

        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RecyclingPtoductionTaskAllGrid.LoadItems();
        }
    }
}
