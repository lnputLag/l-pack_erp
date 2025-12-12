using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Service.Printing
{
    public class PrintingSettings
    {
        public PrintingSettings()
        {
            Initialized = false;
            Copies = 1;
            Duplex = Duplex.Default;
            Landscape = false;
        }

        public bool Initialized { get; set; }

        public string ProfileName { get; set; }

        public string Description {  get; set; }

        public int Copies { get; set; }

        public string PrinterFullName { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int MarginLeft { get; set; }

        public int MarginRight { get; set; }

        public int MarginTop { get; set; }

        public int MarginBottom { get; set; }

        public Duplex Duplex { get; set; }

        public bool? Landscape { get; set; }

        public string PrinterQueueName { get; set; }

        public string PrinterServerName { get; set; }

        public void ParsePrinterFullName()
        {
            if (!string.IsNullOrEmpty(this.PrinterFullName))
            {
                if (PrinterFullName.Contains(@"\"))
                {
                    PrinterServerName = PrinterFullName.Substring(0, PrinterFullName.LastIndexOf(@"\"));
                    PrinterQueueName = PrinterFullName.Substring(PrinterFullName.LastIndexOf(@"\"));
                    PrinterQueueName = PrinterQueueName.Replace(@"\", "");
                }
                else
                {
                    PrinterServerName = "";
                    PrinterQueueName = PrinterFullName;
                }
            }
        }

        public static PrintingSettings Default = new PrintingSettings()
        {
            ProfileName = "default",
            Copies = 1,
            Width = 210,
            Height = 297,
        };

        public static PrintingSettings DocumentPrinter = new PrintingSettings()
        {
            ProfileName = "document_printer",
            Description = "Печать документов А4",
            Width = 210,
            Height = 297,
            Duplex = Duplex.Default,
            Copies = 1
        };

        public static PrintingSettings LabelPrinter = new PrintingSettings()
        {
            ProfileName = "label_printer",
            Description = "Печать ярлыков",
            Width = 72,
            Height = 850,
            Duplex = Duplex.Simplex,
            Copies = 1
        };

        public static PrintingSettings LabelPrinter2 = new PrintingSettings()
        {
            ProfileName = "label_printer2",
            Description = "Печать ярлыков на 2 принтере",
            Width = 72,
            Height = 850,
            Duplex = Duplex.Simplex,
            Copies = 1
        };

        public static PrintingSettings RawLabelPrinter = new PrintingSettings()
        {
            ProfileName = "raw_label_printer",
            Description = "Печать ярлыков сырья",
            Width = 100,
            Height = 140,
            Duplex = Duplex.Simplex,
            Copies = 1,
            Landscape = true
        };

        public static PrintingSettings OuterRawLabelPrinter = new PrintingSettings()
        {
            ProfileName = "outer_raw_label_printer",
            Description = "Печать внешних ярлыков сырья",
            Width = 210,
            Height = 297,
            //Width = 297,
            //Height = 210,
            Duplex = Duplex.Simplex,
            Copies = 1,
            Landscape = true
        };

        public static Dictionary<string, PrintingSettings> PrintingSettingsDictionary = new Dictionary<string, PrintingSettings>()
        {
            { Default.ProfileName, Default },
            { DocumentPrinter.ProfileName, DocumentPrinter},
            { LabelPrinter.ProfileName, LabelPrinter },
            { LabelPrinter2.ProfileName, LabelPrinter2 },
            { RawLabelPrinter.ProfileName, RawLabelPrinter },
            { OuterRawLabelPrinter.ProfileName, OuterRawLabelPrinter }
        };

        public static PrintingSettings GetPrintingSettingsByProfileName(string profileName)
        {
            PrintingSettings printingSettings = null;
            printingSettings = PrintingSettingsDictionary.FirstOrDefault(x => x.Key == profileName).Value;

            return printingSettings;
        }
    }
}
