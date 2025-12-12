using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Documents;
using Client.Common;
using Newtonsoft.Json;
using static DevExpress.Data.Filtering.Helpers.SubExprHelper.ThreadHoppingFiltering;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Отчеты по складу
    /// </summary>
    /// <author>Михеев И.С.</author>
    public class StockReporter
    {
        /// <summary>
        /// Отчёт по заготовкам в буфере
        /// </summary>
        /// <param name="factoryId"></param>
        public static async Task ReportBlankInBuffer(int factoryId = 1)
        {
            Dictionary<string, string> p = new Dictionary<string, string>();
            p.Add("FACTORY_ID", $"{factoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Report");
            q.Request.SetParam("Action", "BlankInBuffer");

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
                    var blanksInBufferDataSet = result["List"];
                    blanksInBufferDataSet.Init();

                    var eg = new ExcelGrid
                    {
                        Columns = new List<ExcelGridColumn>
                        {
                            new ExcelGridColumn("_ROWNUMBER", "№ п/п", 10, ExcelGridColumn.ColumnTypeRef.Integer),
                            new ExcelGridColumn("PALLET", "Поддон", 20),
                            new ExcelGridColumn("PRODUCT_CODE", "Артикул", 100),
                            new ExcelGridColumn("PLACE", "Место", 20),
                            new ExcelGridColumn("QUANTITY", "Количество", 20, ExcelGridColumn.ColumnTypeRef.Integer),
                            new ExcelGridColumn("SQUARE", "Площадь", 20, ExcelGridColumn.ColumnTypeRef.Integer),
                            new ExcelGridColumn("PRODUCT_NAME", "Название", 240),
                            new ExcelGridColumn("SHIPMENT_PRODUCT_NAME", "2е название", 110),
                            new ExcelGridColumn("DAYS_FROM_PRODUCTION", "Срок, дней", 30, ExcelGridColumn.ColumnTypeRef.Integer),
                        },
                        Items = blanksInBufferDataSet.Items,
                        GridTitle = "Отчет по заготовкам от " + DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss"),
                        CellMinWidth = 55

                    };

                    eg.Make();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Отчёт по продукции на кондиционировании в буфере
        /// </summary>
        /// <param name="factoryId"></param>
        public static async Task ReportProductConditioningInBuffer(int factoryId = 1)
        {
            Dictionary<string, string> p = new Dictionary<string, string>();
            p.Add("FACTORY_ID", $"{factoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Report");
            q.Request.SetParam("Action", "ProductConditioningInBuffer");

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
                    var conditioningProductsInBufferDataSet = result["List"];
                    conditioningProductsInBufferDataSet.Init();

                    var eg = new ExcelGrid
                    {
                        Columns = new List<ExcelGridColumn>
                        {
                            new ExcelGridColumn("_ROWNUMBER", "№ п/п", 10, ExcelGridColumn.ColumnTypeRef.Integer),
                            new ExcelGridColumn("PALLET", "Поддон", 20),
                            new ExcelGridColumn("PLACE","Место", 20),
                            new ExcelGridColumn("OLD", "Стоит в буфере, мин", 20, ExcelGridColumn.ColumnTypeRef.Integer),
                            new ExcelGridColumn("SQUARE", "Площадь", 20, ExcelGridColumn.ColumnTypeRef.Integer),
                            new ExcelGridColumn("ARTIKUL", "Артикул", 100),
                            new ExcelGridColumn("NAME", "Название", 300),
                            new ExcelGridColumn("NAME2", "2е название", 110),
                            new ExcelGridColumn("REASON", "Время кондиционирования", 220),
                        },
                        Items = conditioningProductsInBufferDataSet.Items,
                        GridTitle = "Отчет по продукции на кондиционировании в буфере от " + DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss"),
                        CellMinWidth = 70

                    };

                    eg.Make();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Отчёт паллеты на кондиционировании
        /// </summary>
        /// <param name="factoryId"></param>
        public static async Task ReportPalletConditioning(int factoryId = 1)
        {
            Dictionary<string, string> p = new Dictionary<string, string>();
            p.Add("FACTORY_ID", $"{factoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Report");
            q.Request.SetParam("Action", "PalletConditioning");

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
                    var conditioningProductsDataSet = result["List"];
                    conditioningProductsDataSet.Init();

                    var eg = new ExcelGrid
                    {
                        Columns = new List<ExcelGridColumn>
                        {
                            new ExcelGridColumn("_ROWNUMBER", "№ п/п", 10, ExcelGridColumn.ColumnTypeRef.Integer),
                            new ExcelGridColumn("PALLET", "Поддон", 20, ExcelGridColumn.ColumnTypeRef.Integer),
                            new ExcelGridColumn("KOL", "Количество", 20, ExcelGridColumn.ColumnTypeRef.Integer),
                            new ExcelGridColumn("CONDTIME", "Времени прошло", 20, ExcelGridColumn.ColumnTypeRef.Integer),
                            new ExcelGridColumn("PLACE","Ячейка", 20),
                            new ExcelGridColumn("NAME", "Название", 240),
                            new ExcelGridColumn("OBVAZ", "Обвязка", 30),
                            new ExcelGridColumn("SHIPMENTDATE", "Отгрузка", 20, ExcelGridColumn.ColumnTypeRef.DateTime),
                            new ExcelGridColumn("FULLCONDTIME", "Времени надо", 20, ExcelGridColumn.ColumnTypeRef.Integer),
                        },
                        Items = conditioningProductsDataSet.Items,
                        GridTitle = "Поддоны на кондиционировании от " + DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss"),
                        CellMinWidth = 50
                    };

                    eg.Make();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Отчёт по срокам хранения поддонов
        /// </summary>
        public static async Task ReportPalletDayFromProduction(int factoryId = 1)
        {
            Dictionary<string, string> p = new Dictionary<string, string>();
            p.Add("FACTORY_ID", $"{factoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Report");
            q.Request.SetParam("Action", "PalletDayFromProduction");

            q.Request.SetParams(p);

            q.Request.Timeout = 60000;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var oldDataSet = result["List"];
                    oldDataSet.Init();

                    var eg = new ExcelGrid
                    {
                        Columns = new List<ExcelGridColumn>
                        {
                            new ExcelGridColumn("_ROWNUMBER", "№ п/п", 10, ExcelGridColumn.ColumnTypeRef.Integer),
                            new ExcelGridColumn("NAME", "Название", 240),
                            new ExcelGridColumn("ARTIKUL", "Артикул", 240),
                            new ExcelGridColumn("KOL", "Количество", 20, ExcelGridColumn.ColumnTypeRef.Integer),
                            new ExcelGridColumn("SQUARE", "Площадь", 240, ExcelGridColumn.ColumnTypeRef.Integer),
                            new ExcelGridColumn("PLACE","Ячейка", 20),
                            new ExcelGridColumn("OLD", "Срок хранения, дней", 20, ExcelGridColumn.ColumnTypeRef.Integer),
                            new ExcelGridColumn("OLD_OTGR", "Дней с предыдущей отгрузки", 20, ExcelGridColumn.ColumnTypeRef.Integer),
                            new ExcelGridColumn("NEW_SHIP_DATA", "Дата ближайшей отгрузки", 20, ExcelGridColumn.ColumnTypeRef.DateTime),

                        },
                        Items = oldDataSet.Items,
                        GridTitle = "Сроки хранения на СГП от " + DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss")
                    };

                    eg.Make();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Отчёт по товарам на складе
        /// </summary>
        /// <param name="factoryId"></param>
        public static async Task ReportProductInStock(int factoryId = 1)
        {
            Dictionary<string, string> p = new Dictionary<string, string>();
            p.Add("FACTORY_ID", $"{factoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Product");
            q.Request.SetParam("Action", "List");

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
                    var productGridDataSet = result["List"];
                    productGridDataSet.Init();

                    var eg = new ExcelGrid
                    {
                        Columns = new List<ExcelGridColumn>
                        {
                            new ExcelGridColumn("_ROWNUMBER", "№ п/п", 10, ExcelGridColumn.ColumnTypeRef.Integer),
                            new ExcelGridColumn("PRODUCT_CODE", "Артикул", 110),
                            new ExcelGridColumn("PRODUCT_NAME", "Наименование", 280),
                            new ExcelGridColumn("PALLET_COUNT", "Поддоны, шт", 20, ExcelGridColumn.ColumnTypeRef.Integer),
                            new ExcelGridColumn("PRODUCT_QUANTITY", "Изделия, шт", 20, ExcelGridColumn.ColumnTypeRef.Integer),
                            new ExcelGridColumn("SQUARE", "Площадь, м2", 20),
                            new ExcelGridColumn("DAYS_FROM_PRODUCTION", "Срок хранения, дней", 20, ExcelGridColumn.ColumnTypeRef.Integer),

                        },
                        Items = productGridDataSet.Items,
                        GridTitle = "Товары на СГП от " + DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss")
                    };

                    eg.Make();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Отчёт по неликвидным товарам
        /// </summary>
        public static async Task ReportProductIlliquid(int factoryId = 1)
        {
            Dictionary<string, string> p = new Dictionary<string, string>();
            p.Add("FACTORY_ID", $"{factoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Report");
            q.Request.SetParam("Action", "ProductIlliquid");

            q.Request.SetParams(p);

            q.Answer.Type = LPackClientAnswer.AnswerTypeRef.File;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Central.OpenFile(q.Answer.DownloadFilePath);
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Отчёт по поддонам с выбранной продукцией
        /// </summary>
        public static async Task ReportPalletByProduct(int productId, int factoryId = 1)
        {
            Dictionary<string, string> p = new Dictionary<string, string>();
            p.Add("FACTORY_ID", $"{factoryId}");
            p.Add("PRODUCT_ID", $"{productId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Report");
            q.Request.SetParam("Action", "PalletByProduct");

            q.Request.SetParams(p);

            q.Answer.Type = LPackClientAnswer.AnswerTypeRef.File;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Central.OpenFile(q.Answer.DownloadFilePath);
            }
            else
            {
                q.ProcessError();
            }
        }
    }
}
