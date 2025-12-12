using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Список операций (история) по выбранному образцу
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleHistory : UserControl
    {
        public SampleHistory()
        {
            InitializeComponent();

            InitGrid();
            SetDefaults();
        }

        /// <summary>
        /// Идентификатор образца
        /// </summary>
        public int SampleId;

        /// <summary>
        /// Имя вкладки
        /// </summary>
        public string TabName;

        /// <summary>
        /// Имя вызвавшей вкладки
        /// </summary>
        public string ReceiverName;

        private void SetDefaults()
        {
            SampleId = 0;
            TabName = "";
            ReceiverName = "";
        }

        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="№",
                    Path="_ROWNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Дата операции",
                    Path="AUDIT_DTTM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Пользователь",
                    Path="AUDIT_USER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Технолог",
                    Path="TECHNOLOG_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Номер образца",
                    Path="SAMPLE_NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Плановая дата изготовления",
                    Path="PLANNED_DT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth=60,
                    MaxWidth=80,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="Дата изготовления",
                    Path="DT_COMPLETED",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth=60,
                    MaxWidth=80,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="Тип изделия",
                    Path="NAME_PCLASS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Размеры образца",
                    Path="SAMPLE_SIZE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=60,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Сырье",
                    Path="RAW_MISSING",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Чертеж",
                    Path="DRAWING",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Доработка",
                    Path="REVISION",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Доставка",
                    Path="DELIVERY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Комментарий",
                    Path="NAME_NOTE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание сотрудника",
                    Path="EMPLOYEE_NOTE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Картон",
                    Path="CARDBOARD_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Заготовка",
                    Path="PREFORM_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=200,
                },
                new DataGridHelperColumn
                {
                    Header="Дата отгрузки",
                    Path="SHIPMENT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Код доставки",
                    Path="DELIVERY_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Код статуса",
                    Path="STATUS_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };

            Grid.SetColumns(columns);
            Grid.AutoUpdateInterval = 0;
            Grid.UseSorting = false;
            Grid.Init();

            //Grid.OnLoadItems = LoadItems;
            Grid.Run();
        }

        private async void LoadItems()
        {
            if (SampleId > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "Samples");
                q.Request.SetParam("Action", "ListHistory");
                q.Request.SetParam("ID", SampleId.ToString());

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                }
                );

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var sampleDS = ListDataSet.Create(result, "SAMPLES");
                        var proseccedDS = ProcessItems(sampleDS);
                        Grid.UpdateItems(proseccedDS);
                    }
                }
            }
        }

        private ListDataSet ProcessItems(ListDataSet ds)
        {
            var _ds = ds;
            if (ds.Items != null)
            {
                if (ds.Items.Count > 0)
                {
                    foreach (var row in _ds.Items)
                    {
                        if (row.CheckGet("AUDIT_USER") == "WEB")
                        {
                            row.CheckAdd("AUDIT_USER", "ЛК клиента");
                        }

                        if (row.CheckGet("STATUS_ID").ToInt() == 7)
                        {
                            if (row.ContainsKey("DELIVERY_ID"))
                            {
                                if (!string.IsNullOrEmpty(row["DELIVERY_ID"]))
                                {
                                    int deliveryId = row["DELIVERY_ID"].ToInt();
                                    string place = "";

                                    switch (deliveryId)
                                    {
                                        case 0:
                                            place = " на СГП";
                                            break;
                                        case 1:
                                            place = " в Липецк. офис";
                                            break;
                                        case 2:
                                            place = " в Моск. офис";
                                            break;
                                        case 3:
                                            place = " в трансп. компанию";
                                            break;
                                        case 4:
                                            place = " рег. представителю";
                                            break;
                                    }
                                    row["STATUS"] = $"{row["STATUS"]}{place}";
                                }
                            }
                        }
                    }
                }
            }

            return _ds;
        }

        /// <summary>
        /// Отображение вкладки
        /// </summary>
        public void Show()
        {
            string title = $"История {SampleId}";
            TabName = $"SampleHistory{SampleId}";
            Central.WM.AddTab(TabName, title, true, "add", this);

            LoadItems();
        }

        public void Close()
        {
            Central.WM.RemoveTab(TabName);
            if (!string.IsNullOrEmpty(ReceiverName))
            {
                Central.WM.SetActive(ReceiverName, true);
                ReceiverName = "";
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
