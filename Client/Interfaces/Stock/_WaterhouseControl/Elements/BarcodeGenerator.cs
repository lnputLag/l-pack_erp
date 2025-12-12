using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Xps.Packaging;
using System.Windows.Xps;
using System.Windows.Xps.Serialization;
using CodeReason.Reports;
using System.Net.Mail;
using Client.Interfaces.Preproduction;
using System.Windows.Input;

namespace Client.Interfaces.Stock._WaterhouseControl
{
    /// <summary>
    /// Класс для генерации баркода хранилища
    /// <author>eletskikh_ya</author>
    /// </summary>
    public class BarcodeGenerator
    {

        /// <summary>
        /// Заготовки для генерации баркодв
        /// </summary>

        private const string startDoc = @"<FlowDocument Name=""Document"" xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                  xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                  xmlns:xrd=""clr-namespace:CodeReason.Reports.Document;assembly=CodeReason.Reports""
                  xmlns:xrbc=""clr-namespace:CodeReason.Reports.Document.Barcode;assembly=CodeReason.Reports""
                  PageHeight=""10cm"" PageWidth=""15cm"">";

        private const string body2 = @"<FlowDocument Name=""Document"" xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                  xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                  xmlns:xrd=""clr-namespace:CodeReason.Reports.Document;assembly=CodeReason.Reports""
                  xmlns:xrbc=""clr-namespace:CodeReason.Reports.Document.Barcode;assembly=CodeReason.Reports""
                  PageHeight=""10cm"" PageWidth=""15cm"">
                   <Section>
                       
                        <Paragraph TextAlignment=""Center"" >
                        <xrbc:InlineBarcodeC128 ShowText=""false"" PropertyName=""barcode"" Value=""%barcode%"" Width=""14cm"" Height=""2cm"" BarcodeSubType=""BC""/>
                        </Paragraph>
                        <Paragraph FontFamily=""Arial"" Margin=""0,0,0,0"" FontSize=""40"" TextAlignment=""Center"">%code%</Paragraph>
                        <Paragraph FontFamily=""Arial"" Margin=""0,0,0,0"" FontSize=""30"" TextAlignment=""Center"">%itemName%</Paragraph>
                        <Paragraph FontFamily=""Arial"" Margin=""0,0,0,0"" FontSize=""30"" TextAlignment=""Center"">%qty%</Paragraph>
                        <Paragraph FontFamily=""Arial"" Margin=""0,0,0,0"" FontSize=""30"" TextAlignment=""Center"">%date%</Paragraph>
                    </Section>
                </FlowDocument>";

         //<TextBlock FontSize = ""20"" FontWeight=""Bold"" FontFamily=""Arial"" Text=""%date%"" TextWrapping=""NoWrap""/>

        private const string bodySection = @"<Section>
                       
                        <Paragraph TextAlignment=""Left"" FontFamily=""Arial"">
                        <TextBlock Margin=""0,10"" FontSize=""127"" FontWeight=""Bold"" FontFamily=""Arial"" Text=""Storage123"" TextWrapping=""NoWrap""/>
                        </Paragraph>
                        <Paragraph TextAlignment=""Center"" >
                            <xrbc:InlineBarcodeC128 ShowText=""false"" Value=""1234"" Width=""14cm"" Height=""2cm"" BarcodeSubType=""BC""/>
                        </Paragraph>
                    </Section>";


        private const string endDoc = @" </FlowDocument>";

        private Dictionary<string,string> storages = new Dictionary<string, string>();

        /// <summary>
        /// размеры этикетки возможно потребуется изменять
        /// </summary>
        private int barcodeWidth = 8;
        private int barcodeHeight = 4;
        private int fontSize = 110;
        private int pageHeight = 10;
        private int pageWidth = 10;

        /// <summary>
        /// Добавление баркода хранилища
        /// </summary>
        /// <param name="name"></param>
        /// <param name="barcode"></param>
        public void AddStorage(string name, string barcode)
        {
            if(!storages.ContainsKey(name))
            {
                storages.Add(name, barcode);
            }
        }

        private string ApplySetting(string txt)
        {
            txt  =  txt.Replace("%10cm%", barcodeWidth.ToString() + "cm")
            .Replace("%4cm%", barcodeHeight.ToString() + "cm")
                    .Replace("%100%", fontSize.ToString())
                    .Replace("%PageHeight%", pageHeight.ToString())
                    .Replace("%PageWidth%", pageWidth.ToString());

            return txt;
        }

        /// <summary>
        /// подучение FloatDocument
        /// </summary>
        /// <returns></returns>
        public FlowDocument GenerateItemDocument(string barcode, string qty, string itemName, string date)
        {
            FlowDocument document = null;

            int space = date.IndexOf(' ');
            if(space>0) date = date.Substring(0, space);

            string txtDocument = ApplySetting(body2).Replace("%barcode%", barcode).Replace("%qty%", qty).Replace("%itemName%", itemName).Replace("%date%",date).Replace("%code%", barcode);

            try
            {
                document = (FlowDocument)XamlReader.Parse(txtDocument);
            }
            catch
            {
                document = null;
            }

            return document;
        }

        /// <summary>
        /// подучение FloatDocument
        /// </summary>
        /// <returns></returns>
        public FlowDocument GenerateDocument()
        {
            FlowDocument document = null;

            string txtDocument = ApplySetting(startDoc);

            foreach(string key in storages.Keys)
            {
                txtDocument += ApplySetting(bodySection).Replace("Storage123", key).Replace("1234", storages[key]);
            }
                
            txtDocument+= endDoc;

            try
            {
                document = (FlowDocument)XamlReader.Parse(txtDocument);
            }
            catch
            {
                document = null;
            }

            return document;
        }

        /// <summary>
        /// Получение ReportDocument
        /// </summary>
        /// <returns></returns>
        public ReportDocument GenerateReportDocument()
        {
            ReportDocument reportDocument = new ReportDocument();

            string txtDocument = startDoc;

            foreach (string key in storages.Keys)
            {
                txtDocument += bodySection.Replace("Storage123", key).Replace("1234", storages[key]);
            }

            txtDocument += endDoc;

            try
            {
                reportDocument.XamlData = txtDocument;
            }
            catch
            {
                reportDocument = null;
            }

            return reportDocument;
        }
    }
}
