using Client.Common;
using DevExpress.Xpf.Grid.Printing;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Service.Printing
{
    /// <summary>
    /// предпросмотр печатного документа
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-09-25</released>
    /// <changed>2023-09-25</changed>
    public partial class PrintingPreview : Window
    {
        public PrintingPreview()
        {
            InitializeComponent();

            Id = "";
            FrameName = "printer";
            Title="Просмотр перед печатью";
            ProfileName="";
            PrintHelper=null;
            PrintingDocumentFile="";
            ChainUpdate=true;
            PrinterNotSelected=false;

            PrintPreviewCanvas0.Visibility=Visibility.Collapsed;

            Closing += OnClose;

            Init();
            SetDefaults();
        }

        private FormHelper Form { get; set; }
        public PrintHelper PrintHelper {get;set;}
        public string ProfileName { get; set; }
        public string PrintingDocumentFile {get;set;}
        private bool ChainUpdate {get;set;}
        public bool PrinterNotSelected {get;set;}

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// идентификатор записи, с которой работает форма
        /// (primary key записи таблицы)
        /// </summary>
        public string Id { get; set; }


        /// <summary>
        // инициализация компонентов
        /// </summary>
        public void Init()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="PRINTING_PROFILE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProfileSelect,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Name,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="DESCRIPTION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Description,
                    Default="",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRINTER_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=PrinterName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="COPIES",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Copies,
                    Default="1",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="WIDTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Width,
                    Default="210",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="HEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Height,
                    Default="297",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },                
            };

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = Status;

            Form.AfterSet = (Dictionary<string, string> v) =>
            {
                //Name.Focus();
                //Name.SelectAll();
            };

            {
               LoadItemsProfileSelect();
            }

            {
                ProfileSelect.OnSelectItem = (Dictionary<string, string> selectedItem) =>
                {
                    bool result = true;
                    if(ChainUpdate)
                    {
                        {
                            var id=selectedItem.CheckGet("ID");
                            GetData(id);
                        }
                    }
                    return result;
                };
            }
        }

        public void LoadItemsProfileSelect()
        {
            var list=Central.AppSettings.SectionGet("PRINTING_SETTINGS");
            var ds=ListDataSet.Create(list);
            var first=new Dictionary<string,string>();
            first.CheckAdd("NAME","");
            ds.ItemsPrepend(first);
            ProfileSelect.SetItems(ds, "NAME", "NAME");
        }

        public void SetPrintingSettings(Dictionary<string, string> v)
        {            
            ChainUpdate=false;
            ProfileName=v.CheckGet("NAME");
            v.CheckAdd("PRINTING_PROFILE",ProfileName);
            Form.SetValues(v);
            ChainUpdate=true;     
            
            UpdateBorder();
        }

        //public void SetPrintingProfile(string id)
        //{
        //    GetData(id);   
        //    var v=new Dictionary<string,string>();
        //    v.CheckAdd("PRINTING_PROFILE",id);
        //    Form.SetValues(v);
        //}

        /// <summary>
        /// получение данных
        /// </summary>
        public void GetData(string id)
        {
            var v=Central.AppSettings.SectionFindRow("PRINTING_SETTINGS","NAME", id);

            if(v.Count == 0)
            {
                v.CheckAdd("NAME", "");
                v.CheckAdd("DESCRIPTION", "");
                v.CheckAdd("PRINTER_NAME", "");
                v.CheckAdd("COPIES", "1");
                v.CheckAdd("WIDTH", "210");
                v.CheckAdd("HEIGHT", "297");
                v.CheckAdd("MARGIN_LEFT", "0");
                v.CheckAdd("MARGIN_TOP", "0");
                v.CheckAdd("MARGIN_RIGHT", "0");
                v.CheckAdd("MARGIN_BOTTOM", "0");
            }

            SetPrintingSettings(v);
            //Form.SetValues(p);
        }

      

        public void ProcessCommand(string command)
        {
            command=command.ClearCommand();
            if(!command.IsNullOrEmpty())
            {
                switch(command)
                {
                    case "printer_select":
                        {
                            var printHelper=new PrintHelper();
                            printHelper.Init();
                            var p=printHelper.GetDictionaryFromPrintingSettings(printHelper.GetPrintingSettingsFromSystem());
                            Form.SetValues(p);
                        }
                        break;

                    case "save":
                        {
                            Save();
                        }
                        break;

                    case "print":
                        {
                            StartPrinting();
                        }
                        break;

                    case "cancel":
                        {
                            Hide();
                        }
                        break;

                    case "border":
                        {
                           UpdateBorder();
                        }
                        break;

                    case "save_document":
                        SaveDocument();
                        break;

                }
            }
        }

        public void SetDefaults()
        {
            Form.SetDefaults();
        }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Open()
        {
            if(PrinterNotSelected)
            {
                Form.SetStatus("Принтер не настроен");
            }
            Show();
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Hide()
        {
           Close();
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
        /// получение данных
        /// </summary>
        public async void GetData()
        {
            DisableControls();
            var p=Central.AppSettings.SectionFindRow("PRINTING_SETTINGS","NAME", Id);
            Form.SetValues(p);
            Open();
            EnableControls();
        }

        public void Save()
        {
            //bool resume = true;
            //string error = "";

            ////стандартная валидация данных средствами формы
            //if (resume)
            //{
            //    var validationResult = Form.Validate();
            //    if (!validationResult)
            //    {
            //        resume = false;
            //        error="Не все обязательные поля заполнены верно";
            //    }
            //}

            //var v = Form.GetValues();

            ////отправка данных
            //if (resume)
            //{
            //    if(v.ContainsKey("PRINTING_PROFILE"))
            //    {
            //        v.Remove("PRINTING_PROFILE");
            //    }
            //    SaveData(v);
            //}
            //else
            //{
            //    Form.SetStatus(error, 1);
            //}
        }

        /// <summary>
        /// сохранение данных
        /// </summary>
        public async void SaveData(Dictionary<string, string> p)
        {
            DisableControls();
            Central.AppSettings.SectionAddRow("PRINTING_SETTINGS",p);
            Central.AppSettings.Store();

            Central.Msg.SendMessage(new ItemMessage()
            {
                ReceiverGroup="Printing",
                ReceiverName = "PrintingSettingsList",
                SenderName = "PrintingPreview",
                Action = "Refresh",
            });

            {
                LoadItemsProfileSelect();
                var v=new Dictionary<string, string>();
                v.CheckAdd("PRINTING_PROFILE",p.CheckGet("NAME"));
                Form.SetValues(v);
            }
            EnableControls();
        }
        
        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
        }

        public bool RenderPreviewPdf(string filePath)
        {
            var resume = true;
            var result = false;

            List<MemoryStream> imageStreamList = new List<MemoryStream>();

            if (resume)
            {
                if (!System.IO.File.Exists(filePath))
                {
                    resume = false;
                }
            }

            if (resume)
            {
                PrintingDocumentFile = filePath;

                try
                {
                    var pdfDocument = PdfiumViewer.PdfDocument.Load(filePath);
                    for (int i = 0; i < pdfDocument.PageCount; i++)
                    {
                        var image = pdfDocument.Render(i, 96, 96, false);
                        var imageStream = new MemoryStream();
                        image.Save(imageStream, ImageFormat.Bmp);
                        imageStreamList.Add(imageStream);
                    }
                }
                catch (Exception e)
                {
                }
            }

            if (resume)
            {
                foreach (var imageStream in imageStreamList)
                {
                    if (imageStream.Length == 0)
                    {
                        resume = false;
                    }
                }
            }

            if (resume)
            {
                try
                {
                    var border = new Border();
                    border.BorderBrush = ((string)("#ffcccccc")).ToBrush();
                    border.BorderThickness = new Thickness(1, 1, 1, 1);
                    border.VerticalAlignment = VerticalAlignment.Top;
                    border.HorizontalAlignment = HorizontalAlignment.Center;

                    StackPanel stackPanel = new StackPanel();
                    stackPanel.Orientation = Orientation.Vertical;

                    border.Child = stackPanel;

                    foreach (var imageStream in imageStreamList)
                    {
                        var imageSource = new System.Windows.Media.Imaging.BitmapImage { };
                        imageSource.BeginInit();
                        imageSource.StreamSource = imageStream;
                        imageSource.EndInit();

                        var image = new Image();
                        image.Width = imageSource.Width;
                        image.Height = imageSource.Height;
                        image.Source = imageSource;

                        stackPanel.Children.Add(image);
                    }

                    PrintPreviewBody.Child = border;

                    result = true;
                }
                catch (Exception e)
                {
                }
            }

            return result;
        }

        public bool _RenderPreviewPdf(string filePath)
        {
            var resume=true;
            var result=false;

            var imageStream=new MemoryStream();

            if(resume)
            {
                if(!System.IO.File.Exists(filePath))
                {
                    resume=false;
                }
            }

            if(resume)
            {
                PrintingDocumentFile=filePath;

                try{
                    var pdfDocument = PdfiumViewer.PdfDocument.Load(filePath);
                    var image = pdfDocument.Render(0, 96, 96, false);
                    image.Save(imageStream, ImageFormat.Bmp);
                }
                catch(Exception e)
                {
                }
            }

            if(resume)
            {
                if(imageStream.Length==0)
                {
                    resume=false;
                }
            }

            if(resume)
            {               
                try
                {
                    var imageSource = new System.Windows.Media.Imaging.BitmapImage { };
                    imageSource.BeginInit();
                    imageSource.StreamSource = imageStream;
                    imageSource.EndInit();

                    var image = new Image();
                    image.Width=imageSource.Width;
                    image.Height=imageSource.Height;
                    image.Source = imageSource;
                    
                    var border=new Border();
                    border.Child=image;
                    border.BorderBrush=((string)("#ffcccccc")).ToBrush();
                    border.BorderThickness=new Thickness(1,1,1,1);
                    border.VerticalAlignment=VerticalAlignment.Top;
                    border.HorizontalAlignment=HorizontalAlignment.Center;

                    PrintPreviewBody.Child = border;                  

                    result=true;
                }
                catch (Exception e)
                {
                }
            }

            return result;
        }

        public void UpdateBorder()
        {
            var timeout=new Common.Timeout(
                1,
                ()=>{
                    UpdateBorder2();
                },
                true,
                false
            );
            timeout.SetIntervalMs(1000);
            timeout.Run();            
        }

        public void UpdateBorder2()
        {
            /*
            var v=Form.GetValues();
            var w0=v.CheckGet("WIDTH").ToInt();
            var h0=v.CheckGet("HEIGHT").ToInt();

            var w=PrintPreviewBody.ActualWidth;
            var h=PrintPreviewBody.ActualHeight;
            */
            
            PrintPreviewBody0.Width=PrintPreviewBody.ActualWidth;
            PrintPreviewBody0.Height=PrintPreviewBody.ActualHeight;
            PrintPreviewCanvas0.Visibility=Visibility.Visible;
        }

        public void StartPrinting()
        {
            var v=Form.GetValues();
            var printingProfile=v.CheckGet("PRINTING_PROFILE");
            var fileName=PrintingDocumentFile;

            PrintHelper = new PrintHelper();
            PrintHelper.PrintingProfile = printingProfile;
            PrintHelper.PrintingCopies = v.CheckGet("COPIES").ToInt();
            PrintHelper.Init();
            PrintHelper.StartPrinting(fileName);
            PrintHelper.Dispose();
        }

        public void SaveDocument()
        {
            PrintHelper = new PrintHelper();
            PrintHelper.Init();

            if (PrintHelper != null)
            {
                PrintHelper.SaveDocument(this.PrintingDocumentFile);
            }
        }

        private void PrinterNameOnClick(object sender, RoutedEventArgs e)
        {
            ProcessCommand("printer_select");
        }

        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            var b=(Button)sender;
            if(b != null)
            {
                var t=b.Tag.ToString();
                ProcessCommand(t);
            }
        }

        private void OnClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try{
                if(System.IO.File.Exists(PrintingDocumentFile))
                {
                    System.IO.File.Delete(PrintingDocumentFile);
                }
            }
            catch(Exception ex)
            {
            }
        }
    }
}
