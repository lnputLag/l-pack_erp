using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Service.Printing;
using CodeReason.Reports;
using CodeReason.Reports.Barcode;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Xps.Packaging;
using static Client.Common.LPackClientAnswer;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Управление отгрузками, генератор печатных форм
    /// </summary>
    /// <author>balchugov_dv</author>   
    class ShipmentReport
    {
        public ShipmentReport(int id)
        {
            ShipmentId=0;
            CurrentAssembly = Assembly.GetExecutingAssembly();
            TransportIdCode="";
            TransportIdCodeFull="";
            TransportIdBarcodeBitmap =null;

            Shipment =new Dictionary<string, string>();

            if(id!=0)
            {
                GetData(id);
            }
        }

        public int ShipmentId { get; set; }
        public Assembly CurrentAssembly { get; set;}
        public string TransportIdCode { get; set; }
        public string TransportIdCodeFull { get; set; }
        public System.Drawing.Bitmap TransportIdBarcodeBitmap { get; set; }

        public Dictionary<string,string> Shipment { get; set; }
        public Dictionary<string,string> Application { get; set; }
        public List<Dictionary<string,string>> Applications { get; set; }
        public List<Dictionary<string,string>> Positions { get; set; }
        public List<Dictionary<string,string>> Samples { get; set; }
        public List<Dictionary<string,string>> Сliche { get; set; }
        public List<Dictionary<string,string>> Shtantsforms { get; set; }
        public List<Dictionary<string,string>> Products { get; set; }
        public List<Dictionary<string,string>> RouteMaps { get; set; }
        public List<Dictionary<string,string>> Proxies { get; set; }

        public void GetData(int id)
        {
            if(id!=0)
            {
                //может быть, эта строка уже загружена
                //(когда мы последовательно вызываем генератор несколько раз подряд)
                if(id!=ShipmentId)
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    
                    var p = new Dictionary<string, string>();
                    {
                        p.Add("ID",id.ToString());
                    }
                    
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module","Shipments");
                    q.Request.SetParam("Object","Shipment");
                    q.Request.SetParam("Action","Get");
            
                    q.Request.SetParams(p);
            
                    q.DoQuery();

                    if(q.Answer.Status == 0)                
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                        if(result!=null)
                        {
                            {
                                var ds = ListDataSet.Create(result, "SHIPMENTS");
                                if(ds.Items.Count>0)
                                {
                                    Shipment=ds.Items.First();
                                    if (Shipment!=null)
                                    {
                                        ShipmentId = Shipment.CheckGet("ID").ToInt();
                                    }
                                }             
                            }

                            {
                                var ds = ListDataSet.Create(result, "APPLICATIONS");
                                if(ds.Items.Count>0)
                                {
                                    Applications=ds.Items;
                                    Application=ds.Items[0];
                                }             
                            }

                            {
                                var ds = ListDataSet.Create(result, "POSITIONS");
                                if(ds.Items.Count>0)
                                {
                                    Positions=ds.Items;
                                }             
                            }

                            {
                                var ds = ListDataSet.Create(result, "SAMPLES");   
                                if(ds.Items.Count>0)
                                {
                                    Samples=ds.Items;
                                }             
                            }

                            {
                                var ds = ListDataSet.Create(result, "СLICHE");   
                                if(ds.Items.Count>0)
                                {
                                    Сliche =ds.Items;
                                }             
                            }

                            {
                                var ds = ListDataSet.Create(result, "SHTANTSFORMS");      
                                if(ds.Items.Count>0)
                                {
                                    Shtantsforms =ds.Items;
                                }             
                            }

                            {
                                var ds = ListDataSet.Create(result, "PRODUCTS");         
                                if(ds.Items.Count>0)
                                {
                                    Products =ds.Items;
                                }             
                            }

                            {
                                var ds = ListDataSet.Create(result, "ROUTEMAPS");   
                                if(ds.Items.Count>0)
                                {
                                    RouteMaps =ds.Items;
                                }             
                            }

                            {
                                var ds = ListDataSet.Create(result, "PROXIES");      
                                if(ds.Items.Count>0)
                                {
                                    Proxies =ds.Items;
                                }             
                            }
                        }
                    } 
                    
                    Mouse.OverrideCursor = null;
                }
            }
        }

        /// <summary>
        /// Печать документов для перемещения на СОХ
        /// </summary>
        public void PrintMovementInvoiceDocuments()
        {
            PrintMovementInvoice();
            PrintMovementInvoice13();

            // Проверем статус отгрузки
            // Если статус отгрузки == 4 (отгрузка совершена),
            // значит все дополнительные действия (изменение статуса отгрузки, создание записей по поддонам, отправка заявки на СОХ) уже сделаны
            if (CheckShipmentStatus() != 4)
            {
                UpdateOrderStatus();
                CreateOnlineStorePallet();
                CreateFileForFTP();
            }
        }

        public int CheckShipmentStatus()
        {
            int status = -1;

            if (ShipmentId > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "Shipment");
                q.Request.SetParam("Action", "GetStatus");

                q.Request.SetParam("ID", ShipmentId.ToString());

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var dataSet = ListDataSet.Create(result, "ITEMS");
                        if (dataSet != null && dataSet.Items.Count > 0)
                        {
                            status = dataSet.Items.First().CheckGet("STATUS").ToInt();
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            return status;
        }

        /// <summary>
        /// Печать транспортной накладной
        /// </summary>
        public void PrintMovementInvoice()
        {
            if (ShipmentId > 0)
            {
                var firstDictionary = new Dictionary<string, string>();

                // Получаем данные для отчёта
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Shipments");
                    q.Request.SetParam("Object", "Shipment");
                    q.Request.SetParam("Action", "GetMovementInvoiceDocument");

                    q.Request.SetParam("ID", ShipmentId.ToString());
                    q.Request.SetParam("DOCUMENT_NUMBER", "1");

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var dataSet = ListDataSet.Create(result, "ITEMS");

                            if (dataSet != null && dataSet.Items.Count > 0)
                            {
                                firstDictionary = dataSet.Items.First();
                            }
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }

                // формируем отчёт (транспортная накладная)
                if (firstDictionary != null && firstDictionary.Count > 0)
                {
                    bool resume = true;

                    var culture = new System.Globalization.CultureInfo("");
                    var reportDocument = new ReportDocument();

                    //var reportTemplate = "C:\\Users\\sviridov_ae\\source\\repos\\l-pack_erp\\Client\\Reports\\Sales\\MovementInvoiceReport.xaml"; //Reports / Stock / StockLabel.xaml
                    //var stream = File.OpenRead(reportTemplate);

                    var reportTemplate = "Client.Reports.Sales.MovementInvoiceReport.xaml";
                    var stream = CurrentAssembly.GetManifestResourceStream(reportTemplate);

                    if (resume)
                    {
                        if (stream == null)
                        {
                            Central.Dbg($"Cant load report template [{reportTemplate}]");
                            resume = false;
                        }
                    }

                    if (resume)
                    {
                        var reader = new StreamReader(stream);

                        reportDocument.XamlData = reader.ReadToEnd();
                        reportDocument.XamlImagePath = Path.Combine(Environment.CurrentDirectory, @"Templates\");
                        reader.Close();

                        string tpl = reportDocument.XamlData;

                        ReportData data = new ReportData();

                        //общие данные
                        var systemName = $"{Central.Parameters.SystemName} {Central.Parameters.BaseLabel}";

                        data.ReportDocumentValues.Add("DT", firstDictionary.CheckGet("DT"));
                        data.ReportDocumentValues.Add("SENDER", firstDictionary.CheckGet("SENDER"));

                        string recipient = $"{firstDictionary.CheckGet("RECIPIENT_NAME")} {firstDictionary.CheckGet("RECIPIENT_ADDRESS")} ИНН:{firstDictionary.CheckGet("RECIPIENT_INN")}";
                        data.ReportDocumentValues.Add("RECIPIENT", recipient);

                        data.ReportDocumentValues.Add("DELIVERY_ADDRESS", firstDictionary.CheckGet("DELIVERY_ADDRESS"));
                        data.ReportDocumentValues.Add("PRODUCT_QUANTITY", firstDictionary.CheckGet("PRODUCT_QUANTITY").ToInt().ToString());
                        data.ReportDocumentValues.Add("PALLET_QUANTITY", firstDictionary.CheckGet("PALLET_QUANTITY").ToInt().ToString());
                        data.ReportDocumentValues.Add("SENDER_INN", firstDictionary.CheckGet("SENDER_INN"));
                        data.ReportDocumentValues.Add("TRUCK_MARKA", firstDictionary.CheckGet("TRUCK_MARKA"));
                        data.ReportDocumentValues.Add("TRUCK_NUM", firstDictionary.CheckGet("TRUCK_NUM"));
                        data.ReportDocumentValues.Add("TRANSPORTER_FULL", firstDictionary.CheckGet("TRANSPORTER_FULL"));
                        data.ReportDocumentValues.Add("DRIVER_FULL", firstDictionary.CheckGet("DRIVER_FULL"));

                        double massBrutto = Math.Round(firstDictionary.CheckGet("MASS_BRUTTO").ToDouble()/1000, 3);
                        double massNetto = Math.Round(firstDictionary.CheckGet("MASS_NETTO").ToDouble()/1000, 3);
                        data.ReportDocumentValues.Add("MASS_BRUTTO", massBrutto.ToString());
                        data.ReportDocumentValues.Add("MASS_NETTO", massNetto.ToString());

                        data.ReportDocumentValues.Add("SENDER_ADDRESS", firstDictionary.CheckGet("SENDER_ADDRESS"));
                        data.ReportDocumentValues.Add("SHIPMENT_PLAN_DTTM", firstDictionary.CheckGet("SHIPMENT_PLAN_DTTM"));

                        int massBruttoInKilograms = (firstDictionary.CheckGet("MASS_BRUTTO").ToDouble()).ToInt();
                        data.ReportDocumentValues.Add("MASS_BRUTTO_IN_KILO", massBruttoInKilograms.ToString());
                        data.ReportDocumentValues.Add("DRIVER_FIO", firstDictionary.CheckGet("DRIVER_FIO"));
                        data.ReportDocumentValues.Add("PAYER_FULL", firstDictionary.CheckGet("PAYER_FULL"));
                        data.ReportDocumentValues.Add("DRIVER_ARRIVAL_DTTM", firstDictionary.CheckGet("DRIVER_ARRIVAL_DTTM"));
                        data.ReportDocumentValues.Add("DRIVER_DEPARTURE_DTTM", firstDictionary.CheckGet("DRIVER_DEPARTURE_DTTM"));

                        string userFio = $"{Central.User.Surname} {Central.User.Name} {Central.User.MiddleName}";
                        data.ReportDocumentValues.Add("USER_FIO", userFio);

                        //data.ReportDocumentValues.Add("DOCUMENT_NUMBER", firstDictionary.CheckGet("DOCUMENT_NUMBER"));
                        data.ReportDocumentValues.Add("DOCUMENT_NUMBER", firstDictionary.CheckGet("MOVEMENT_INVOICE_13_NUMBER"));

                        data.ReportDocumentValues.Add("MOVEMENT_INVOICE_13_NUMBER", firstDictionary.CheckGet("MOVEMENT_INVOICE_13_NUMBER"));

                        //DOCUMENT_BARCODE

                        string driverOwn = "";
                        switch (firstDictionary.CheckGet("DRIVER_OWN").ToInt())
                        {
                            case 1:
                                driverOwn = "1";
                                break;

                            case 2:
                                driverOwn = "3";
                                break;

                            case 3:
                                driverOwn = "4";
                                break;

                            default:
                                driverOwn = " ";
                                break;
                        }
                        data.ReportDocumentValues.Add("DRIVER_OWN", driverOwn);

                        try
                        {
                            reportDocument.XamlData = tpl;

                            XpsDocument xps = reportDocument.CreateXpsDocumentKey(data, "MovementInvoiceReport");
                            ShowDocument(xps);
                        }
                        catch (Exception ex)
                        {
                            var msg = "";
                            msg = $"{msg}Не удалось создать транспортную накладную.{Environment.NewLine}";
                            msg = $"{msg}Пожалуйста, запустите создание документа снова.";
                            var d = new DialogWindow(msg, "Ошибка создания документа", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Печать накладной ТОРГ13
        /// </summary>
        public void PrintMovementInvoice13()
        {
            if (ShipmentId > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "Shipment");
                q.Request.SetParam("Action", "GetMovementInvoiceDocument");

                q.Request.SetParam("ID", ShipmentId.ToString());
                q.Request.SetParam("DOCUMENT_NUMBER", "2");

                q.Answer.Type = AnswerTypeRef.File;

                q.DoQuery();

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

        /// <summary>
        /// Обновление таблицы naklrashodz (status = 4)
        /// </summary>
        public void UpdateOrderStatus()
        {
            if (ShipmentId > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("ID", ShipmentId.ToString());
                p.Add("STATUS", "4");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "ResponsibleStock");
                q.Request.SetParam("Action", "UpdateOrderStatusByTransportId");
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds != null && ds.Items.Count > 0)
                        {
                            int shipmentId = ds.Items.First().CheckGet("ID").ToInt();

                            if (shipmentId > 0)
                            {

                            }
                            else
                            {
                                //exp
                            }
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Создаём записи в таблице ONLINE_STORE_PALLET (OSPS_ID = 0)
        /// </summary>
        public void CreateOnlineStorePallet()
        {
            if (ShipmentId > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("ID", ShipmentId.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "ResponsibleStock");
                q.Request.SetParam("Action", "CreateOnlineStorePallet");
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds != null && ds.Items.Count > 0)
                        {
                            int shipmentId = ds.Items.First().CheckGet("ID").ToInt();

                            if (shipmentId > 0)
                            {

                            }
                            else
                            {
                                //exp
                            }
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Создаём файл прихода для отправки по FTP
        /// </summary>
        public void CreateFileForFTP()
        {
            if (ShipmentId > 0)
            {
                bool succesfulFlag = false;
                int idTs = 0;
                int nsthet = 0;

                var p = new Dictionary<string, string>();
                p.Add("ID_TS", ShipmentId.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "ResponsibleStock");
                q.Request.SetParam("Action", "CreateFileForFTP");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();


                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var dataSet = ListDataSet.Create(result, "ITEMS");

                        if (dataSet != null && dataSet.Items.Count > 0)
                        {
                            idTs = dataSet.Items.First().CheckGet("ID_TS").ToInt();
                            nsthet = dataSet.Items.First().CheckGet("NSTHET").ToInt();
                            
                            if (idTs > 0 && nsthet > 0)
                            {
                                succesfulFlag = true;
                            }
                        }
                    }

                    if (!succesfulFlag)
                    {
                        string msg = $"Ошибка создания файла отгрузки №{idTs} для выгрузки на FTP сервер";
                        var d = new DialogWindow($"{msg}", "Отгрузка", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    else
                    {
                        string msg = $"Успешное создание файла отгрузки №{idTs} для выгрузки на FTP сервер";
                        var d = new DialogWindow($"{msg}", "Отгрузка", "", DialogWindowButtons.OK);
                        d.ShowDialog();

                        string fileName = $"{nsthet}.csv";
                        SendFileToFTP(fileName);
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Отправляем выбранный файл на FTP сервер
        /// </summary>
        public void SendFileToFTP(string fileName)
        {
            bool succesfulFlag = false;

            var p = new Dictionary<string, string>();
            p.Add("FILE_NAME", fileName);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "ResponsibleStock");
            q.Request.SetParam("Action", "SendByFTP");
            q.Request.SetParams(p);

            q.Request.Timeout = 300000;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();


            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var dataSet = ListDataSet.Create(result, "ITEMS");

                    if (dataSet != null && dataSet.Items.Count > 0)
                    {
                        fileName = dataSet.Items.First().CheckGet("FILE_NAME");

                        if (!string.IsNullOrEmpty(fileName))
                        {
                            succesfulFlag = true;
                        }
                    }
                }

                if (!succesfulFlag)
                {
                    string msg = $"Ошибка отправления файла {fileName} на FTP сервер";
                    var d = new DialogWindow($"{msg}", "Отгрузка", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    string msg = $"Успешное отправление файла {fileName} на FTP сервера";
                    var d = new DialogWindow($"{msg}", "Отгрузка", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// доверенность
        /// </summary>
        public void PrintProxy(bool printFlag = false)
        {
            var serverRootPath = Central.Parameters.StorageServerPath["Order.Proxy"];

            if(Shipment.CheckGet("TENDERDOC").ToInt() == 1)
            {
                //для этой отгрузки есть тендер
                //доверенность нужно сформировать, используя результат тендера
                GetProxy1(ShipmentId, printFlag);
                GetProxy2(ShipmentId, printFlag);
            }
            else
            {
                //иначе доверенность получим стандартным образом
                if(Proxies!=null)
                {
                    if(Proxies.Count > 0)
                    {
                        foreach(Dictionary<string,string> item in Proxies)
                        {
                            var filePath = "";
                            if(!string.IsNullOrEmpty(item["FILENAME"].ToString()))
                            {
                                filePath=$"{serverRootPath}{item["FILENAME"].ToString()}";

                            }

                            if(!string.IsNullOrEmpty(filePath))
                            {
                                if (printFlag)
                                {
                                    PrintDocument(filePath);
                                }
                                else
                                {
                                    Central.OpenFile(filePath);
                                }
                            }
                        }
                    }
                }
            }
            
        }

        /// <summary>
        /// загрузочная карта водителя
        /// </summary>
        public void PrintBootcard(bool printFlag = false)
        {
            var resume=true;            

            if(resume)
            {
                if(Shipment.Count==0)
                {
                    Central.Dbg($"No items");
                    resume=false;
                }
            }

            var reportDocument = new ReportDocument();
            var reportTemplate="Client.Reports.Shipments.Bootcard.xaml";
            var stream = CurrentAssembly.GetManifestResourceStream(reportTemplate);

            if(resume)
            {
                if(stream==null)
                {
                    Central.Dbg($"Cant load report template [{reportTemplate}]");
                    resume=false;
                }
            }

            if(resume)
            {
                var reader = new StreamReader(stream);

                reportDocument.XamlData = reader.ReadToEnd();
                reportDocument.XamlImagePath = Path.Combine(Environment.CurrentDirectory,@"Templates\");
                reportDocument.ImageProcessing += ReportDocumentImageProcessing;
                reportDocument.ImageError += ReportDocumentImageError;
                reader.Close();

                string tpl = reportDocument.XamlData;
                var data = new ReportData();

                var systemName = $"{Central.Parameters.SystemName} {Central.Parameters.BaseLabel}";
                data.ReportDocumentValues.Add("SystemName",systemName);
                data.ReportDocumentValues.Add("Id",Shipment["ID"]);

                var id = Shipment["ID"].ToInt();

                //barcode
                data.ReportDocumentValues.Add("IdCode",id.ToString("D13"));
                TransportIdCode=id.ToString("D12");
                var ean13 = new Ean13(TransportIdCode);
                TransportIdBarcodeBitmap = ean13.CreateBitmap();
                TransportIdCodeFull=$"{TransportIdCode}{ean13.ChecksumDigit}";
                data.ReportDocumentValues.Add("TransportIdCode",TransportIdCode);
                data.ReportDocumentValues.Add("TransportIdCodeFull",TransportIdCodeFull);

                if(Shipment["SELFSHIPMENT"].ToInt()==1)
                {
                    data.ReportDocumentValues.Add("ShipmentType","Самовывоз");
                }
                else
                {
                    data.ReportDocumentValues.Add("ShipmentType","Доставка");
                }
                data.ReportDocumentValues.Add("Today",DateTime.Now);

                var car = "";
                if(!string.IsNullOrEmpty(Shipment["CARMARK"]))
                {
                    car+=""+Shipment["CARMARK"];
                }

                if(!string.IsNullOrEmpty(Shipment["CARNUMBER"]))
                {
                    car+=" "+Shipment["CARNUMBER"];
                }

                if(!string.IsNullOrEmpty(Shipment["TRAILERNUMBER"]))
                {
                    car+=" "+Shipment["TRAILERNUMBER"];
                }

                data.ReportDocumentValues.Add("Car",car);
                data.ReportDocumentValues.Add("DriverName",Shipment["DRIVERNAME"]);
                data.ReportDocumentValues.Add("DriverPhone",Shipment["DRIVERPHONE"]);
                data.ReportDocumentValues.Add("BuyerName",Shipment["BUYERNAME"]);

                string shipperName = Shipment.CheckGet("SHIPPER_NAME");
                if (string.IsNullOrEmpty(shipperName))
                {
                    shipperName = "ООО \"Торговый Дом Л-ПАК\"";
                }
                data.ReportDocumentValues.Add("ShipperName", shipperName);
                string shipperAddress = Shipment.CheckGet("SHIPPER_ADRES");
                if (string.IsNullOrEmpty(shipperAddress))
                {
                    shipperAddress = "398007, Россия, г.Липецк, ул. Ковалева, 125А";
                }
                data.ReportDocumentValues.Add("ShipperAddress", shipperAddress);

                int Payment = Shipment["PAYMENT"].ToInt();
                if(Payment!=0)
                {
                    data.ReportDocumentValues.Add("Payment",Payment);
                }
                else
                {
                    tpl=tpl.Replace("Payment","_Payment");
                }

                if(!string.IsNullOrEmpty(Shipment["NOTE"]))
                {
                    data.ReportDocumentValues.Add("Note",Shipment["NOTE"]);
                }
                else
                {
                    tpl=tpl.Replace("Note","_Note");
                }

                try
                {
                    reportDocument.XamlData = tpl;
                    XpsDocument xps = reportDocument.CreateXpsDocumentKey(data, "MakeDriverBootCardReport");

                    if (printFlag)
                    {
                        PrintDocument(xps, System.Drawing.Printing.Duplex.Vertical);
                    }
                    else
                    {
                        ShowDocument(xps, System.Drawing.Printing.Duplex.Vertical);
                    }
                }
                catch(Exception e)
                {
                    //FIXME: 2021-09-01_B4
                    var msg=""; 
                    msg=$"{msg}Не удалось создать загрузочную карту.\n"; 
                    msg=$"{msg}Пожалуйста, запустите создание документа снова.\n"; 
                    var d = new DialogWindow(msg, "Ошибка создания документа", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
        }

        /// <summary>
        /// задание на отгрузку
        /// </summary>
        public void PrintShipmenttask(bool printFlag = false)
        {
            var resume=true;            

            if(resume)
            {
                if(Shipment.Count==0)
                {
                    Central.Dbg($"No items");
                    resume=false;
                }
            }

            var culture = new System.Globalization.CultureInfo("");
            var reportDocument = new ReportDocument();
            var reportTemplate="Client.Reports.Shipments.Shipmenttask.xaml";
            var stream = CurrentAssembly.GetManifestResourceStream(reportTemplate);

            if (resume)
            {
                if(stream==null)
                {
                    Central.Dbg($"Cant load report template [{reportTemplate}]");
                    resume=false;
                }
            }

            if(resume)
            {
                var reader = new StreamReader(stream);

                reportDocument.XamlData = reader.ReadToEnd();
                reportDocument.XamlImagePath = Path.Combine(Environment.CurrentDirectory,@"Templates\");
                reader.Close();

                string tpl = reportDocument.XamlData;

                ReportData data = new ReportData();

                //общие данные
                var systemName = $"{Central.Parameters.SystemName} {Central.Parameters.BaseLabel}";
                data.ReportDocumentValues.Add("SystemName",systemName);
                data.ReportDocumentValues.Add("Today",DateTime.Now);

                data.ReportDocumentValues.Add("Id",Shipment["ID"]);
                data.ReportDocumentValues.Add("DefaultTerminal",Shipment.CheckGet("DEFAULTTERMINAL").ToInt().ToString());
                data.ReportDocumentValues.Add("ShipmentDate",Shipment.CheckGet("SHIPMENTDATETIMEFULL").ToString());
                data.ReportDocumentValues.Add("Note",Shipment["NOTE"]);

                var car = "";
                if(!string.IsNullOrEmpty(Shipment["CARMARK"]))
                {
                    car+=""+Shipment["CARMARK"];
                }

                if(!string.IsNullOrEmpty(Shipment["CARNUMBER"]))
                {
                    car+=" "+Shipment["CARNUMBER"];
                }

                if(!string.IsNullOrEmpty(Shipment["TRAILERNUMBER"]))
                {
                    car+=" "+Shipment["TRAILERNUMBER"];
                }

                data.ReportDocumentValues.Add("Car",car);
                data.ReportDocumentValues.Add("DriverName",Shipment["DRIVERNAME"]);
                data.ReportDocumentValues.Add("DriverPhone",Shipment["DRIVERPHONE"]);

                if (Shipment.CheckGet("RESPONSIBLE_STOCK_FLAG").ToInt() == 0)
                {
                    tpl = tpl.Replace("ResponsibleStock", "_ResponsibleStock");
                }

                string sideLoad="";

                if(Applications!=null)
                {
                    if(Applications.Count > 0)
                    {
                        DataTable table = new DataTable("Applications");

                        table.Columns.Add("ApplicationId",typeof(string));
                        table.Columns.Add("OrderNumber",typeof(string));
                        table.Columns.Add("DeliveryDate",typeof(string));
                        table.Columns.Add("CustomerName",typeof(string));
                        table.Columns.Add("ChengerName",typeof(string));
                        table.Columns.Add("ManagerPhone",typeof(string));
                        table.Columns.Add("ManagerMobilePhone",typeof(string));
                        table.Columns.Add("StorekeeperNote",typeof(string));
                        table.Columns.Add("PorterNote",typeof(string));
                        table.Columns.Add("DocumentsPrintingNote",typeof(string));

                        foreach(Dictionary<string,string> application in Applications)
                        {
                            string applicationId="";
                            if(application["APPLICATIONID"].ToInt()!=0)
                            {
                                applicationId=application["APPLICATIONID"].ToInt().ToString();
                            }

                            var m=application.CheckGet("MANAGERNAMEFULL").ToString().SurnameInitials();
                            
                            if(application.CheckGet("SIDE_LOADING").ToBool())
                            {
                                sideLoad="Боковая загрузка";
                            }

                            table.Rows.Add(new object[]
                            {
                                applicationId,
                                application["ORDERNUMBER"],
                                application["DELIVERYDATE"],
                                application["CUSTOMERNAME"],
                                m,
                                application["MANAGERPHONE"],
                                application["MANAGERMOBILEPHONE"],
                                application["STOREKEEPERNOTE"],
                                application["PORTERNOTE"],
                                application["DOCUMENTSPRINTINGNOTE"],                                
                            });
                        }

                        data.DataTables.Add(table);
                    }
                }

                data.ReportDocumentValues.Add("CarSideLoad",sideLoad);

                //DocRegistry
                {

                    if(Applications!=null)
                    {
                        if(Applications.Count > 0)
                        {
                            DataTable table = new DataTable("DocRegistry");

                            table.Columns.Add("ApplicationId",typeof(string));
                            table.Columns.Add("CustomerName",typeof(string));
                            table.Columns.Add("UpdPrinting",typeof(string));
                            table.Columns.Add("OrderPrinting",typeof(string));
                            table.Columns.Add("Waybill",typeof(string));
                            table.Columns.Add("PTWaybillPrinting",typeof(string));
                            table.Columns.Add("InnerWaybill",typeof(string));
                            table.Columns.Add("ClientPTWaybillBlankPrinting",typeof(string));
                            table.Columns.Add("QualitySertificatePrinting",typeof(string));
                            table.Columns.Add("QualityPassportPrinting", typeof(string));
                            table.Columns.Add("CmrPrinting",typeof(string));
                            table.Columns.Add("GostSertificatesPrinting",typeof(string));
                            table.Columns.Add("TUSertificatesPrinting",typeof(string));
                            table.Columns.Add("PaperQSPrinting",typeof(string));
                            table.Columns.Add("PaperSpecificationPrinting",typeof(string));
                            table.Columns.Add("Torg13Printing", typeof(string));
                            table.Columns.Add("Torg12Printing",typeof(string));
                            table.Columns.Add("InnerTransportWaybillPrinting",typeof(string));
                            table.Columns.Add("InnerPTWaybillPrinting",typeof(string));
                            table.Columns.Add("InnerCmrPrinting",typeof(string));
                            table.Columns.Add("ProxyPrinting",typeof(string));
                            table.Columns.Add("PaperSpecificationPrinting2",typeof(string));
                            table.Columns.Add("Torg13InnerPrinting", typeof(string));

                            foreach (Dictionary<string,string> application in Applications)
                            {                                
                                var rowItems = new Dictionary<string,string>();

                                //задание на отгрузку, покупатель
                                rowItems.Add("ApplicationId",application["APPLICATIONID"].ToInt().ToString());
                                rowItems.Add("CustomerName",application["CUSTOMERNAME"].ToString());

                                //Документы клиенту
                                rowItems.Add("UpdPrinting",application["UPDPRINTING"].ToString());
                                rowItems.Add("OrderPrinting",application["ORDERPRINTING"].ToString());
                                rowItems.Add("Torg13Printing", application["TORG_13_PRINTING"].ToInt().ToString());

                                {
                                    var Waybill = "";
                                    int UpdPrinting = application["UPDPRINTING"].ToInt();
                                    int SelfShipment = application["SELFSHIPMENT"].ToInt();
                                    int TransportWaybillPrinting = application["TRANSPORTWAYBILLPRINTING"].ToInt();
                                    int ClientIdMax = application["CLIENTIDMAX"].ToInt();
                                    int DealerWaybillPrinting = application["DEALERWAYBILLPRINTING"].ToInt();
                                    int TransporterId = application["TRANSPORTERID"].ToInt();

                                    int c = 0;
                                    string t = "";

                                    if(UpdPrinting!=0 && SelfShipment==0 && TransporterId!=0)
                                    {
                                        //2022-04-27_F1
                                        //c=TransportWaybillPrinting+2;
                                        c=TransportWaybillPrinting;
                                    }
                                    else
                                    {
                                        c=TransportWaybillPrinting;
                                    }

                                    if(TransportWaybillPrinting>0 && UpdPrinting==0 && ClientIdMax==1 && DealerWaybillPrinting==1 && SelfShipment==1)
                                    {
                                        t=" Печ. кл.";
                                    }

                                    Waybill=$"{c}{t}";
                                    rowItems.Add("Waybill",Waybill);
                                }

                                rowItems.Add("PTWaybillPrinting",application["PTWAYBILLPRINTING"].ToInt().ToString());

                                {
                                    var InnerWaybill = "";                                    
                                    int InnerPTWaybillPrinting = application["INNERWAYBILLPRINTING"].ToInt();

                                    int DeliveryAddressQuantity = application["DELIVERYADDRESSQUANTITY"].ToInt();
                                    int ClientIdMax = Application["CLIENTIDMAX"].ToInt();
                                    int c = InnerPTWaybillPrinting*DeliveryAddressQuantity;
                                    var t = "";

                                    if(InnerPTWaybillPrinting > 0 && ClientIdMax == 1)
                                    {
                                        t=" Печ. кл.";
                                    }
                                    InnerWaybill=$"{c}{t}";
                                    rowItems.Add("InnerWaybill",InnerWaybill);
                                }


                                rowItems.Add("ClientPTWaybillBlankPrinting",application["CLIENTPTWAYBILLBLANKPRINTING"].ToString());
                                rowItems.Add("QualitySertificatePrinting",application["QUALITYSERTIFICATEPRINTING"].ToString());
                                rowItems.Add("QualityPassportPrinting", application["QUALITY_PASSPORT_PRINTING"].ToString());

                                {
                                    var CmrPrinting = "";
                                    int CmrPrinting2 = application["CMRPRINTING"].ToInt();
                                    int DeliveryAddressQuantity = application["DELIVERYADDRESSQUANTITY"].ToInt();
                                    int c = CmrPrinting2*DeliveryAddressQuantity;
                                    CmrPrinting=$"{c}";
                                    rowItems.Add("CmrPrinting",CmrPrinting);
                                }


                                rowItems.Add("GostSertificatesPrinting",application["GOSTSERTIFICATESPRINTING"].ToString());
                                rowItems.Add("TUSertificatesPrinting",application["TUSERTIFICATESPRINTING"].ToString());
                                rowItems.Add("PaperQSPrinting",application["PAPERQSPRINTING"].ToString());

                                {
                                    var PaperSpecificationPrinting = "";
                                    int PaperSpecification = application["PAPERSPECIFICATIONPRINTING"].ToInt();
                                    int c = 0;
                                    if(PaperSpecification > 1)
                                    {
                                        c=PaperSpecification-1;
                                    }
                                    else
                                    {
                                        c=PaperSpecification;
                                    }
                                    PaperSpecificationPrinting=$"{c}";
                                    rowItems.Add("PaperSpecificationPrinting",PaperSpecificationPrinting);
                                }



                                //докмунеты компании
                                rowItems.Add("Torg12Printing",application["TORG12PRINTING"].ToString());
                                rowItems.Add("Torg13InnerPrinting", application["TORG_13_INNER_PRINTING"].ToInt().ToString());

                                {
                                    var InnerTransportWaybillPrinting = "";
                                    int UpdPrinting = application["UPDPRINTING"].ToInt();
                                    int SelfShipment = application["SELFSHIPMENT"].ToInt();
                                    int InnerTransportWaybill = application["INNERTRANSPORTWAYBILLPRINTING"].ToInt();
                                    int TransporterId = application["TRANSPORTERID"].ToInt();
                                    int c = 0;
                                    
                                    //bugfix: 2023-01-24_F1
                                    
                                    //if(UpdPrinting==0 && SelfShipment==0 && TransporterId !=0)
                                    //{
                                    //    c=InnerTransportWaybill+2;
                                    //}
                                    //else
                                    //{
                                    //    c=InnerTransportWaybill;
                                    //}

                                    c=InnerTransportWaybill;

                                    InnerTransportWaybillPrinting=$"{c}";
                                    rowItems.Add("InnerTransportWaybillPrinting",InnerTransportWaybillPrinting);
                                }

                                rowItems.Add("InnerPTWaybillPrinting",application["INNERPTWAYBILLPRINTING"].ToInt().ToString());

                                {
                                    var InnerCmrPrinting = "";
                                    int _InnerCmrPrinting = application["INNERCMRPRINTING"].ToInt();
                                    int DeliveryAddressQuantity = application["DELIVERYADDRESSQUANTITY"].ToInt();
                                    int c = 0;
                                    c=_InnerCmrPrinting*DeliveryAddressQuantity;
                                    InnerCmrPrinting=$"{c}";
                                    rowItems.Add("InnerCmrPrinting",InnerCmrPrinting);
                                }

                                {
                                    var ProxyPrinting = "";
                                    int SelfShipment = application["SELFSHIPMENT"].ToInt();
                                    int Proxy = application["PROXY"].ToInt();
                                    int TransporterId = application["TRANSPORTERID"].ToInt();
                                    int c = 0;
                                    if(SelfShipment==1 || TransporterId!=0)
                                    {
                                        c=1;
                                    }
                                    else
                                    {
                                        c=Proxy;
                                    }
                                    ProxyPrinting=$"{c}";
                                    rowItems.Add("ProxyPrinting",ProxyPrinting);
                                }

                                {
                                    var PaperSpecificationPrinting2 = "";
                                    int _PaperSpecificationPrinting = application["PAPERSPECIFICATIONPRINTING"].ToInt();
                                    int c = 0;
                                    if(_PaperSpecificationPrinting>1)
                                    {
                                        c=1;
                                    }
                                    else
                                    {
                                        c=0;
                                    }
                                    PaperSpecificationPrinting2=$"{c}";
                                    rowItems.Add("PaperSpecificationPrinting2",PaperSpecificationPrinting2);
                                }


                                table.Rows.Add(new object[]
                                {
                                    rowItems["ApplicationId"],
                                    rowItems["CustomerName"],
                                    rowItems["UpdPrinting"],
                                    rowItems["OrderPrinting"],
                                    rowItems["Waybill"],
                                    rowItems["PTWaybillPrinting"],
                                    rowItems["InnerWaybill"],
                                    rowItems["ClientPTWaybillBlankPrinting"],
                                    rowItems["QualitySertificatePrinting"],
                                    rowItems["QualityPassportPrinting"],
                                    rowItems["CmrPrinting"],
                                    rowItems["GostSertificatesPrinting"],
                                    rowItems["TUSertificatesPrinting"],
                                    rowItems["PaperQSPrinting"],
                                    rowItems["PaperSpecificationPrinting"],
                                    rowItems["Torg13Printing"],
                                    rowItems["Torg12Printing"],
                                    rowItems["InnerTransportWaybillPrinting"],
                                    rowItems["InnerPTWaybillPrinting"],
                                    rowItems["InnerCmrPrinting"],
                                    rowItems["ProxyPrinting"],
                                    rowItems["PaperSpecificationPrinting2"],
                                    rowItems["Torg13InnerPrinting"],
                                });
                            }

                            data.DataTables.Add(table);
                        }
                    }
                }


                //образцы
                var hideSamples=true;
                if(Samples!=null)
                {
                    if(Samples.Count > 0)
                    {
                        DataTable table = new DataTable("Samples");

                        table.Columns.Add("CustomerName",typeof(string));
                        table.Columns.Add("SampleId", typeof(string));
                        table.Columns.Add("SampleName",typeof(string));
                        table.Columns.Add("Quantity",typeof(string));

                        foreach(Dictionary<string,string> sample in Samples)
                        {
                            table.Rows.Add(new object[]
                            {
                                sample["CUSTOMERNAME"],
                                sample["ID"],
                                sample["SAMPLENAME"],
                                sample["QUANTITY"],
                            });
                        }

                        data.DataTables.Add(table);
                        hideSamples=false;
                    }                    
                }

                if(hideSamples)
                {
                    tpl=tpl.Replace("Samples","_Samples");
                }


                //клише
                var hideСliche=true;
                if(Сliche!=null)
                {
                    if(Сliche.Count > 0)
                    {
                        DataTable table = new DataTable("Сliche");

                        table.Columns.Add("CustomerName",typeof(string));
                        table.Columns.Add("ProductName",typeof(string));
                        table.Columns.Add("ClicheName",typeof(string));
                        
                        foreach(Dictionary<string,string> item in Сliche)
                        {
                            table.Rows.Add(new object[]
                            {
                                item["CUSTOMERNAME"],
                                item["PRODUCTNAME"],
                                item["CLICHENAME"],
                            });
                        }

                        data.DataTables.Add(table);
                        hideСliche=false;
                    }                    
                }

                if(hideСliche)
                {
                    tpl=tpl.Replace("Cliche","_Сliche");
                }


                //штанцформы
                var hideShtantsforms=true;
                if(Shtantsforms!=null)
                {
                    if(Shtantsforms.Count > 0)
                    {
                        DataTable table = new DataTable("Shtantsforms");

                        table.Columns.Add("CustomerName",typeof(string));
                        table.Columns.Add("ProductName",typeof(string));
                        table.Columns.Add("ShtantsformName",typeof(string));

                        foreach(Dictionary<string,string> item in Shtantsforms)
                        {
                            table.Rows.Add(new object[]
                            {
                                item["CUSTOMERNAME"],
                                item["PRODUCTNAME"],
                                item["SHTANTSFORMNAME"],
                            });
                        }

                        data.DataTables.Add(table);
                        hideShtantsforms=false;
                    }                    
                }

                if(hideShtantsforms)
                {
                    tpl=tpl.Replace("Shtantsforms","_Shtantsforms");
                }


                //SpecNote
                var hideSpecNote=false;
                {
                    var SpecNote = "";
                    
                    //int UpdPrinting = Application["UPDPRINTING"].ToInt();
                    //int SelfShipment = Application["SELFSHIPMENT"].ToInt();
                    int UpdPrinting = 0;
                    int SelfShipment = 0;

                    if(Application!=null)
                    {
                        UpdPrinting = Application.CheckGet("UPDPRINTING").ToInt();
                        SelfShipment = Application.CheckGet("SELFSHIPMENT").ToInt();
                    }

                    if(UpdPrinting > 0 && SelfShipment == 0)
                    {
                        SpecNote=$"Скрепить 1 экз. УПД и 2 экз. ТН и поставить штамп на каждом экземпляре <Просьба поставить печать...>";
                    }
                    else if(UpdPrinting > 0 && SelfShipment == 1)
                    {
                        SpecNote=$"На 1 экз. УПД поставить штамп <Просьба поставить печать...>";
                    }

                    if(string.IsNullOrEmpty(SpecNote))
                    {
                        hideSpecNote=true;
                    }

                    data.ReportDocumentValues.Add("SpecNote",SpecNote);
                }

                if(hideSpecNote)
                {
                    tpl=tpl.Replace("SpecNote","_SpecNote");
                }


                //номенклатура
                if(Positions!=null)
                {
                    if(Positions.Count > 0)
                    {

                        DataTable table = new DataTable("Positions");

                        table.Columns.Add("LoadOrder",typeof(string));
                        table.Columns.Add("ApplicationId",typeof(string));
                        table.Columns.Add("VendorCode",typeof(string));
                        table.Columns.Add("VendorCodePrinting",typeof(string));
                        table.Columns.Add("Code",typeof(string));
                        table.Columns.Add("ProductName",typeof(string));
                        table.Columns.Add("Quantity",typeof(string));
                        table.Columns.Add("QuantityLimit",typeof(string));
                        table.Columns.Add("Status",typeof(string));
                        table.Columns.Add("InStockQuantity",typeof(string));
                        table.Columns.Add("ShippedQuantity",typeof(string));
                        table.Columns.Add("PriceActual",typeof(string));
                        table.Columns.Add("DeliveryAddress",typeof(string));
                        table.Columns.Add("StorekeeperNote",typeof(string));
                        table.Columns.Add("PorterNote",typeof(string));

                        foreach(Dictionary<string,string> position in Positions)
                        {
                            string price = position.CheckGet("PRICEACTUAL").ToDouble().ToString();


                            string loadOrder="";
                            if(!position["POSITIONNUM"].ToString().IsNullOrEmpty())
                            {
                                loadOrder=position["POSITIONNUM"].ToString();
                            }

                            string code="";
                            if(position["CODE"].ToInt()!=0)
                            {
                                code=position["CODE"].ToInt().ToString();
                            }


                            string quantity="";
                            if(position["QUANTITY"].ToInt()!=0)
                            {
                                quantity=position["QUANTITY"].ToInt().ToString();
                            }

                            string quantityLimit="";
                            if(!position["QUANTITYLIMIT"].ToString().IsNullOrEmpty())
                            {
                                quantityLimit=position["QUANTITYLIMIT"].ToString();
                            }

                            string inStockQuantity="";
                            if(position["INSTOCKQUANTITY"].ToInt()!=0)
                            {
                                inStockQuantity=position["INSTOCKQUANTITY"].ToInt().ToString();
                            }

                            string shippedQuantity="";
                            if(position["SHIPPEDQUANTITY"].ToInt()!=0)
                            {
                                shippedQuantity=position["SHIPPEDQUANTITY"].ToInt().ToString();
                            }


                            table.Rows.Add(new object[]
                            {
                                loadOrder,
                                position["APPLICATIONID"],
                                position["VENDORCODE"],
                                position["VENDORCODEPRINTING"],
                                code,
                                position["PRODUCTNAME"],
                                quantity,
                                quantityLimit,
                                position["STATUS"],
                                inStockQuantity,
                                shippedQuantity,
                                price,
                                position["DELIVERYADDRESS"],
                                position["STOREKEEPERNOTE"],
                                position["PORTERNOTE"],
                            });
                        }

                        data.DataTables.Add(table);

                    }
                }

                //номенклатура2
                if(Positions!=null)
                {
                    if(Positions.Count > 0)
                    {

                        DataTable table = new DataTable("Positions2");

                        table.Columns.Add("ProductName",typeof(string));
                        table.Columns.Add("Quantity",typeof(string));
                        table.Columns.Add("PalletQuantity",typeof(string));
                        table.Columns.Add("TransportPack",typeof(string));

                        foreach(Dictionary<string,string> position in Positions)
                        {
                            table.Rows.Add(new object[]
                            {
                                position["PRODUCTNAME"],
                                position["QUANTITY"].ToInt().ToString(),
                                position["PALLETQUANTITY"].ToDouble().ToString(),
                                position["TRANSPORTPACK"],
                            });
                        }

                        data.DataTables.Add(table);

                    }
                }

                try
                {
                    reportDocument.XamlData=tpl;
                    XpsDocument xps = reportDocument.CreateXpsDocumentKey(data,"MakeShipmentReport");

                    if (printFlag)
                    {
                        PrintDocument(xps);
                    }
                    else
                    {
                        ShowDocument(xps);
                    }
                }
                catch(Exception e)
                {
                    //FIXME: 2021-09-01_B4
                    var msg=""; 
                    msg=$"{msg}Не удалось создать загрузочную карту.\n"; 
                    msg=$"{msg}Пожалуйста, запустите создание документа снова.\n"; 
                    var d = new DialogWindow(msg, "Ошибка создания документа", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
        }

        /// <summary>
        /// карта склада
        /// </summary>
        public void PrintStockmap(bool printFlag = false)
        {
            var resume=true;            

            if(resume)
            {
                if(Shipment.Count==0)
                {
                    Central.Dbg($"No items");
                    resume=false;
                }
            }

            var reportDocument = new ReportDocument();
            var reportTemplate="Client.Reports.Shipments.Stockmap.xaml";
            var stream = CurrentAssembly.GetManifestResourceStream(reportTemplate);

            if(resume)
            {
                if(stream==null)
                {
                    Central.Dbg($"Cant load report template [{reportTemplate}]");
                    resume=false;
                }
            }

            if(resume)
            {
                var reader = new StreamReader(stream);

                reportDocument.XamlData = reader.ReadToEnd();
                reportDocument.XamlImagePath = Path.Combine(Environment.CurrentDirectory,@"Templates\");
                reportDocument.ImageProcessing += ReportDocumentImageProcessing;
                reportDocument.ImageError += ReportDocumentImageError;
                reader.Close();

                string tpl = reportDocument.XamlData;

                ReportData data = new ReportData();


                {
                    var systemName = $"{Central.Parameters.SystemName} {Central.Parameters.BaseLabel}";
                    data.ReportDocumentValues.Add("SystemName",systemName);
                    data.ReportDocumentValues.Add("Today",DateTime.Now);
                }

                {
                    data.ReportDocumentValues.Add("Id",Shipment["ID"]);
                    var id = Shipment["ID"].ToInt();
                }

                {
                    var car = "";
                    if(!string.IsNullOrEmpty(Shipment["CARMARK"]))
                    {
                        car+=""+Shipment["CARMARK"];
                    }

                    if(!string.IsNullOrEmpty(Shipment["CARNUMBER"]))
                    {
                        car+=" "+Shipment["CARNUMBER"];
                    }

                    if(!string.IsNullOrEmpty(Shipment["TRAILERNUMBER"]))
                    {
                        car+=" "+Shipment["TRAILERNUMBER"];
                    }

                    data.ReportDocumentValues.Add("Car",car);
                    data.ReportDocumentValues.Add("DriverName",Shipment["DRIVERNAME"]);
                    data.ReportDocumentValues.Add("DriverPhone",Shipment["DRIVERPHONE"]);
                }

                data.ReportDocumentValues.Add("BuyerName",Shipment["BUYERNAME"]);
                data.ReportDocumentValues.Add("ShipmentDateTimeFull",Shipment["SHIPMENTDATETIMEFULL"]);

                /*
                    формируем список продукции из Positions
                    для каждой строки находим соотв. ей записи в Products

                    */

                if (Positions != null)
                {
                    if (Positions.Count > 0)
                    {
                        var table = new DataTable("Positions");
                        var inlineSection = new InlineSection("Positions", "Block1");

                        table.Columns.Add("Number", typeof(int));
                        table.Columns.Add("ProductionName", typeof(string));
                        table.Columns.Add("NeedleQuantity", typeof(int));
                        table.Columns.Add("ShippedQuantity", typeof(int));
                        table.Columns.Add("InStockQuantityTotal", typeof(int));

                        foreach (Dictionary<string, string> position in Positions)
                        {
                            // данные для карты отгрузки
                            var store = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>>();
                            {
                                /*
                                    ProductId
                                        PlaceName
                                            ProductionTaskId-Quantity

                                    */

                                if (Products != null)
                                {
                                    if (Products.Count > 0)
                                    {
                                        int productId = position.CheckGet("PRODUCTID").ToInt();

                                        if (productId != 0)
                                        {


                                            foreach (Dictionary<string, string> product in Products)
                                            {
                                                var currentProductId = product["PRODUCTID"].ToString();
                                                if (currentProductId == productId.ToString())
                                                {

                                                    if (!store.ContainsKey(currentProductId))
                                                    {
                                                        store.Add(currentProductId, new Dictionary<string, Dictionary<string, Dictionary<string, string>>> ());
                                                    }

                                                    var currentPlaceName = product["PLACENAME"].ToString();
                                                    if (!string.IsNullOrEmpty(currentPlaceName))
                                                    {
                                                        if (!store[currentProductId].ContainsKey(currentPlaceName))
                                                        {
                                                            store[currentProductId].Add(currentPlaceName, new Dictionary<string, Dictionary<string, string>> ());
                                                        }

                                                        var currentTaskId = product["PRODUCTIONTASKID"].ToString();
                                                        var currentQuantity = product["QUANTITY"].ToInt().ToString();

                                                        var currentDataTime = product["RASHODDATATIME"].ToString();

                                                        if (string.IsNullOrEmpty(currentDataTime))
                                                        {
                                                            currentDataTime = "--";
                                                        }

                                                        if (!string.IsNullOrEmpty(currentTaskId))
                                                        {
                                                            if (!store[currentProductId][currentPlaceName].ContainsKey(currentTaskId))
                                                            {
                                                                store[currentProductId][currentPlaceName].Add(currentTaskId, new Dictionary<string, string>());

                                                                store[currentProductId][currentPlaceName][currentTaskId].Add(currentQuantity, currentDataTime);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }


                            {
                                // inline-секция строки таблицы
                                // содержит сложную таблицу с картой продукции
                                // секцию генерируем программно, в шаблон отдаем готовый рендер

                                int maxColumns = 4;
                                int fontSize = 14;
                                string fontFamily = "Verdana";


                                Table mapTable = new Table();
                                mapTable.Margin = new Thickness(0, 0, 0, 0);

                                for (int x = 1; x <= maxColumns; x++)
                                {
                                    mapTable.Columns.Add(new TableColumn());
                                }

                                mapTable.RowGroups.Add(new TableRowGroup());
                                //mapTable.RowGroups[0].Rows.Add(new TableRow());

                                TableRow r;
                                TableCell c;
                                int rowIndex = -1;

                                var bc = new BrushConverter();
                                var blackBrush = (Brush)bc.ConvertFrom("#ff000000");

                                foreach (KeyValuePair<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>> product in store)
                                {
                                    {
                                        mapTable.RowGroups[0].Rows.Add(new TableRow());
                                        rowIndex++;
                                        r = mapTable.RowGroups[0].Rows[rowIndex];

                                        c = new TableCell(new Paragraph(new Run($"№ Поддона")));
                                        c.FontFamily = new FontFamily(fontFamily);
                                        c.FontSize = fontSize;
                                        c.FontWeight = FontWeights.Normal;
                                        c.BorderThickness = new Thickness(0, 0, 0, 0);
                                        r.Cells.Add(c);

                                        c = new TableCell(new Paragraph(new Run($"Количество")));
                                        c.FontFamily = new FontFamily(fontFamily);
                                        c.FontSize = fontSize;
                                        c.FontWeight = FontWeights.Normal;
                                        c.BorderThickness = new Thickness(0, 0, 0, 0);
                                        r.Cells.Add(c);

                                        c = new TableCell(new Paragraph(new Run($"Статус")));
                                        c.FontFamily = new FontFamily(fontFamily);
                                        c.FontSize = fontSize;
                                        c.FontWeight = FontWeights.Normal;
                                        c.BorderThickness = new Thickness(0, 0, 0, 0);
                                        r.Cells.Add(c);

                                        c = new TableCell(new Paragraph(new Run($"Дата/Время отгрузки")));
                                        c.FontFamily = new FontFamily(fontFamily);
                                        c.FontSize = fontSize;
                                        c.FontWeight = FontWeights.Normal;
                                        c.BorderThickness = new Thickness(0, 0, 0, 0);
                                        r.Cells.Add(c);
                                    }

                                    foreach (KeyValuePair<string, Dictionary<string, Dictionary<string, string>>> place in product.Value)
                                    {
                                        foreach (KeyValuePair<string, Dictionary<string, string>> element in place.Value)
                                        {
                                            mapTable.RowGroups[0].Rows.Add(new TableRow());
                                            rowIndex++;
                                            r = mapTable.RowGroups[0].Rows[rowIndex];

                                            var v = element.Value.Keys.First().ToInt();

                                            var dttm = element.Value.Values.First().ToString();

                                            //production
                                            c = new TableCell(new Paragraph(new Run($"{element.Key}")));
                                            c.FontFamily = new FontFamily(fontFamily);
                                            c.FontSize = fontSize;
                                            c.FontWeight = FontWeights.Normal;
                                            c.BorderThickness = new Thickness(0, 0, 0, 0);
                                            r.Cells.Add(c);

                                            //quantity
                                            c = new TableCell(new Paragraph(new Run($"{v}")));
                                            c.FontFamily = new FontFamily(fontFamily);
                                            c.FontSize = fontSize;
                                            c.FontWeight = FontWeights.Normal;
                                            c.BorderThickness = new Thickness(0, 0, 0, 0);
                                            r.Cells.Add(c);

                                            //status
                                            c = new TableCell(new Paragraph(new Run($"{place.Key}")));
                                            c.FontFamily = new FontFamily(fontFamily);
                                            c.FontSize = fontSize;
                                            c.FontWeight = FontWeights.Normal;
                                            c.BorderThickness = new Thickness(0, 0, 0, 0);
                                            r.Cells.Add(c);

                                            //datetime
                                            c = new TableCell(new Paragraph(new Run($"{dttm}")));
                                            c.FontFamily = new FontFamily(fontFamily);
                                            c.FontSize = fontSize;
                                            c.FontWeight = FontWeights.Normal;
                                            c.BorderThickness = new Thickness(0, 0, 0, 0);
                                            r.Cells.Add(c);
                                        }
                                    }
                                }

                                inlineSection.Rows.Add(mapTable);
                            }

                            string productName = "";
                            productName = $"{position["PRODUCTNAME"]} {position["VENDORCODE"]}";
                            //productName = $"{position["VENDORCODE"]}";

                            // строка данных таблицы
                            table.Rows.Add(new object[]
                            {
                                position["LOADORDER"].ToInt(),
                                productName,
                                position["QUANTITY"].ToInt(),
                                position["SHIPPEDQUANTITY"].ToInt(),
                                position["INSTOCKQUANTITYTOTAL"].ToInt(),
                            });

                        }

                        data.DataTables.Add(table);
                        data.InlineSections.Add(inlineSection);

                    }
                }

                try
                {
                    data.ShowUnknownValues=false;
                    reportDocument.XamlData=tpl;

                    //XpsDocument xps = reportDocument.CreateXpsDocument(data);
                    XpsDocument xps = reportDocument.CreateXpsDocumentKey(data,"MakeShipmentMapReport");

                    if (printFlag)
                    {
                        PrintDocument(xps);
                    }
                    else
                    {
                        ShowDocument(xps);
                    }
                }
                catch(Exception e)
                {
                    //FIXME: 2021-09-01_B4
                    var msg=""; 
                    msg=$"{msg}Не удалось создать карту склада.\n"; 
                    msg=$"{msg}Пожалуйста, запустите создание документа снова.\n"; 
                    var d = new DialogWindow(msg, "Ошибка создания документа", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
        }
        
        /// <summary>
        /// карта проезда
        /// </summary>
        public void PrintRoutemap(bool printFlag = false)
        {
            var serverRootPath = Central.Parameters.StorageServerPath["DeliveryAddress"];

            if(RouteMaps!=null)
            {
                if(RouteMaps.Count > 0)
                {
                    foreach(Dictionary<string,string> item in RouteMaps)
                    {
                        var filePath = "";
                        if(!string.IsNullOrEmpty(item["FILENAME"].ToString()))
                        {
                            filePath=$"{serverRootPath}{item["FILENAME"].ToString()}";

                        }
                        else if(!string.IsNullOrEmpty(item["FILE2PATH"].ToString()))
                        {
                            filePath=$"{item["FILE2PATH"].ToString()}";
                        }

                        if(!string.IsNullOrEmpty(filePath))
                        {
                            if (printFlag)
                            {
                                PrintDocument(filePath);
                            }
                            else
                            {
                                Central.OpenFile(filePath);
                            }
                        }
                    }
                }
            }
            else
            {
                var e = new DialogWindow($"Нет карт проезда для данной отгрузки");
            }
        }

        public void PrintDocument(string filePath)
        {
            var printHelper = new PrintHelper();
            printHelper.PrintingProfile = PrintingSettings.DocumentPrinter.ProfileName;
            printHelper.PrintingDuplex = System.Drawing.Printing.Duplex.Simplex;
            printHelper.Init();
            var printingResult = printHelper.StartPrinting(filePath);
            printHelper.Dispose();
        }

        public void PrintDocument(XpsDocument xps)
        {
            var printHelper = new PrintHelper();
            printHelper.PrintingProfile = PrintingSettings.DocumentPrinter.ProfileName;
            printHelper.PrintingDuplex = System.Drawing.Printing.Duplex.Simplex;
            printHelper.Init();
            var printingResult = printHelper.StartPrinting(xps);
            printHelper.Dispose();
        }

        public void PrintDocument(XpsDocument xps, System.Drawing.Printing.Duplex duplex)
        {
            var printHelper = new PrintHelper();
            printHelper.PrintingProfile = PrintingSettings.DocumentPrinter.ProfileName;
            printHelper.PrintingDuplex = duplex;
            printHelper.Init();
            var printingResult = printHelper.StartPrinting(xps);
            printHelper.Dispose();
        }

        public void ShowDocument(XpsDocument xps)
        {
            var printHelper = new PrintHelper();
            printHelper.PrintingProfile = PrintingSettings.DocumentPrinter.ProfileName;
            printHelper.PrintingDuplex = System.Drawing.Printing.Duplex.Simplex;
            printHelper.Init();
            var printingResult = printHelper.ShowPreview(xps);
            printHelper.Dispose();
        }

        public void ShowDocument(XpsDocument xps, System.Drawing.Printing.Duplex duplex)
        {
            var printHelper = new PrintHelper();
            printHelper.PrintingProfile = PrintingSettings.DocumentPrinter.ProfileName;
            printHelper.PrintingDuplex = duplex;
            printHelper.Init();
            var printingResult = printHelper.ShowPreview(xps);
            printHelper.Dispose();
        }

        /// <summary>
        /// заготовка отчета
        /// </summary>
        private void _MakeReport()
        {
            var resume=true;            

            if(resume)
            {
                if(Shipment.Count==0)
                {
                    Central.Dbg($"No items");
                    resume=false;
                }
            }

            var reportDocument = new ReportDocument();
            var reportTemplate="Client.Reports.Shipments.ShipmentReport.xaml";
            var stream = CurrentAssembly.GetManifestResourceStream(reportTemplate);

            if(resume)
            {
                if(stream==null)
                {
                    Central.Dbg($"Cant load report template [{reportTemplate}]");
                    resume=false;
                }
            }

            if(resume)
            {
                var reader = new StreamReader(stream);

                
            }
        }
        
        private void GetProxy1(int shipmentId, bool printFlag = false)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Shipment");
            q.Request.SetParam("Action", "GetProxy");
                    
            q.Request.SetParam("ID",shipmentId.ToString());
            q.Request.SetParam("FORM","1");
            
            q.Request.Timeout=10000;
            q.Request.Attempts=3;
            q.Answer.Type=AnswerTypeRef.File;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                if (printFlag)
                {
                    PrintDocument(q.Answer.DownloadFilePath);
                }
                else
                {
                    Central.OpenFile(q.Answer.DownloadFilePath);
                }
            }
            else
            {
                q.ProcessError();
            }            
        }

        private void GetProxy2(int shipmentId, bool printFlag = false)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Shipment");
            q.Request.SetParam("Action", "GetProxy");
                    
            q.Request.SetParam("ID",shipmentId.ToString());
            q.Request.SetParam("FORM","2");
            
            q.Answer.Type=AnswerTypeRef.File;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                if (printFlag)
                {
                    PrintDocument(q.Answer.DownloadFilePath);
                }
                else
                {
                    Central.OpenFile(q.Answer.DownloadFilePath);
                }            
            }
            else
            {
                q.ProcessError();
            }            
        }
    
        /// <summary>
        /// генератор штрих-кода
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReportDocumentImageProcessing(object sender,ImageEventArgs e)
        {
            if(e.Image.Name == "ImageBarcode")
            {
                if(!string.IsNullOrEmpty(TransportIdCode))
                {
                    if(TransportIdBarcodeBitmap != null)
                    {
                        MemoryStream mem = new MemoryStream();
                        TransportIdBarcodeBitmap.Save(mem,System.Drawing.Imaging.ImageFormat.Bmp);
                        mem.Position = 0;

                        BitmapImage image = new BitmapImage();
                        image.BeginInit();
                        image.StreamSource = mem;
                        image.EndInit();
                        e.Image.Source = image;
                    }
                }
            }
        }

        private void ReportDocumentImageError(object sender,ImageErrorEventArgs e)
        {
            e.Handled = true;
        }
        
    }
}
