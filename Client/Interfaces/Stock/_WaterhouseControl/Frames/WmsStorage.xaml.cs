using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Production;
using Client.Interfaces.Service.Printing;
using Client.Interfaces.Stock._WaterhouseControl;
using Client.Interfaces.Stock.ForkliftDrivers.Windows;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using NPOI.POIFS.Properties;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Printing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Interaction logic for WmsStorage.xaml
    /// Интерфейс редактирования и печат и ярлыка для хранилища
    /// <author>eletskikh_ya</author>
    /// </summary>
    public partial class WmsStorage : UserControl
    {
        private int Id { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        public WmsStorage()
        {
            InitializeComponent();

            Init();
        }


        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {
            Form = new FormHelper();

            FormHelper.ComboBoxInitHelper(CellType, "Warehouse", "StorageType", "List", "WMSY_ID", "STORAGE_TYPE");

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="NUM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TextName,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null  },
                    },
                },
                new FormHelperField()
                {
                    Path="WMSY_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CellType,
                    ControlType="SelectBox",
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = Status;

            //после установки значений
            Form.AfterSet = (Dictionary<string, string> v) =>
            {
                //фокус на поле ввода логина
                TextName.Focus();
            };

        }

        public void Edit(int id)
        {
            Id = id;

            if(Id!=0)
            {
                PrintButton.IsEnabled = true;
            }

            GetData();
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
        /// Генерация картинки для этикетки
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Bitmap GenerateLabel(string Id, string name)
        {
            // отсткп слева
            int DX = 15;
            // отступ с двух сторон
            int DDX = DX * 2;

            // высота выходной картинки будет HeightScale * высота баркода
            int HeightScale = 4;

            int fontSizeScale = 5;

            int barWeight = fontSizeScale * 2;

            var barcode = Code128Rendering.MakeBarcodeImage(Id, barWeight, true);
            Bitmap label = new Bitmap(barcode.Width + DDX, barcode.Height * HeightScale);

            using (Graphics g = Graphics.FromImage(label))
            {
                //g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                g.FillRectangle(
                    Brushes.White, 0, 0, label.Width, label.Height * 2);

                // Create string formatting options (used for alignment)
                StringFormat format = new StringFormat()
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                RectangleF rectf = new RectangleF(0, 0, barcode.Width, barcode.Height * 2);

                g.DrawString(name, new Font("Tahoma", 22 * fontSizeScale), Brushes.Black, rectf, format);


                float barcodeYpos = barcode.Height * 2;

                g.DrawImage(barcode, DX, barcodeYpos);

                double textW = barcode.Width / 4.3;

                Rectangle r = new Rectangle((int)(label.Width / 2 - textW / 2), (int)(barcodeYpos + (barcode.Height * 0.8)), (int)textW, barcode.Height);
                g.FillRectangle(Brushes.White, r);

                g.DrawString(Id, new Font("Tahoma", 8 * fontSizeScale), Brushes.Black, r, format);


                g.Flush();
            }

            label.SetResolution(600, 600);

            return label;
        }
       
        /// <summary>
        /// получение данных с сервера
        /// </summary>
        public async void GetData()
        {
            DisableControls();

            bool resume = true;// Id != 0;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("WMST_ID", Id.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Storage");
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
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            Form.SetValues(ds);

                            PrepareData();
                        }

                        Show();
                    }
                }
                else
                {
                    q.ProcessError();
                }

            }
            else
            {
                Show();
            }

            EnableControls();
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            var frameName = GetFrameName();
            Central.WM.Close(frameName);

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
                SenderName = GetFrameName(),
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// Возвращает название режима работы с тмц
        /// </summary>
        /// <returns></returns>
        private string GetActionName()
        {
            string result = "Редактирование ячейки";

            return result;
        }

        /// <summary>
        /// формирует уникальный идентификатор фрейма
        /// </summary>
        /// <returns></returns>
        public string GetFrameName()
        {
            string result = "";

            result = $"{FrameName}_{Id}";

            return result;
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

            var frameName = GetFrameName();
            if (Id == 0)
            {
                Central.WM.Show(frameName, "Новое хранилище", true, "add", this);
            }
            else
            {
                Central.WM.Show(frameName, GetActionName() + $" {TextName.Text}", true, "add", this);
            }

            if (Id != 0)
            {
                TextName.IsReadOnly = true;
            }
        }

        /// <summary>
        /// Сохранение изменений
        /// </summary>
        public void Save()
        {
            bool resume = true;
            string error = "";

            //стандартная валидация данных средствами формы
            if (resume)
            {
                var validationResult = Form.Validate();
                if (!validationResult)
                {
                    resume = false;
                }
            }


            {
                var v = Form.GetValues();

                //отправка данных
                if (resume)
                {
                    SaveData(v);
                }
                else
                {
                    Form.SetStatus(error, 1);
                }
            }
        }

        /// <summary>
        /// отпаравка данных на сервер
        /// </summary>
        public void SaveData(Dictionary<string, string> p)
        {
            DisableControls();

            bool resume = true;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Storage");
            q.Request.SetParam("Action", "UpdateType");

            p.Add("WMST_ID", Id.ToString());
            
            //p.Add("WMST_ID", "0");

            if (resume)
            {
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
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
                                    SenderName = "WMSStorage",
                                    Action = "Refresh",
                                    Message = $"{id}",
                                });

                                Close();
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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void PrepareData()
        {
            if (Document != null)
            {
                BarcodeGenerator generator = new BarcodeGenerator();
                generator.AddStorage(TextName.Text, Id.ToString());

                ScrollViewer.Document = generator.GenerateDocument();
            }
        }


      
        private static string _previewWindowXaml =
    @"<Window
        xmlns                 ='http://schemas.microsoft.com/netfx/2007/xaml/presentation'
        xmlns:x               ='http://schemas.microsoft.com/winfx/2006/xaml'
        Title                 ='Print Preview - @@TITLE'
        Height                ='400'
        Width                 ='300'
        WindowStartupLocation ='CenterOwner'>
        <DocumentViewer Name='dv1'/>
     </Window>";

        public void DoPreview(string title)
        {
            string fileName = System.IO.Path.GetRandomFileName();
            FlowDocumentScrollViewer visual = ScrollViewer;
            try
            {
                // write the XPS document
                using (XpsDocument doc = new XpsDocument(fileName, FileAccess.ReadWrite))
                {
                    XpsDocumentWriter writer = XpsDocument.CreateXpsDocumentWriter(doc);
                    writer.Write(visual);
                }

                // Read the XPS document into a dynamically generated
                // preview Window 
                using (XpsDocument doc = new XpsDocument(fileName, FileAccess.Read))
                {
                    FixedDocumentSequence fds = doc.GetFixedDocumentSequence();

                    string s = _previewWindowXaml;
                    s = s.Replace("@@TITLE", title.Replace("'", "&apos;"));

                    using (var reader = new System.Xml.XmlTextReader(new StringReader(s)))
                    {
                        Window preview = System.Windows.Markup.XamlReader.Load(reader) as Window;

                        DocumentViewer dv1 = LogicalTreeHelper.FindLogicalNode(preview, "dv1") as DocumentViewer;
                        dv1.Document = fds as IDocumentPaginatorSource;


                        preview.ShowDialog();
                    }
                }
            }
            finally
            {
                if (File.Exists(fileName))
                {
                    try
                    {
                        File.Delete(fileName);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            BarcodeGenerator generator = new BarcodeGenerator();
            generator.AddStorage(TextName.Text, Id.ToString());
            var doc = generator.GenerateDocument();
            var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;

            PrintDocument(paginator);
        }

        public void PrintDocument(DocumentPaginator documentPaginator)
        {
            var printHelper = new PrintHelper();
            printHelper.PrintingProfile = PrintingSettings.RawLabelPrinter.ProfileName;
            printHelper.PrintingDuplex = System.Drawing.Printing.Duplex.Simplex;
            printHelper.PrintingLandscape = true;
            printHelper.Init();
            var printingResult = printHelper.StartPrinting(documentPaginator);
            printHelper.Dispose();
        }

        public void SetPrintSettings()
        {
            var i = new PrintingInterface();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
