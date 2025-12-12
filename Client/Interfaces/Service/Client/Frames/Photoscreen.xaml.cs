using Client.Common;
using Client.Interfaces.Main;
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

namespace Client.Interfaces.Service
{
    /// <summary>
    /// просмотр скриншотов
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-02-20</released>
    /// <changed>2023-02-20</changed>
    public partial class PhotoScreen : UserControl
    {
        public PhotoScreen()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);
            Loaded += OnLoad;

            ScreenShotGridInit();
            SetDefaults();

            HostUserId="";
            Title="";
            ScreenshotScaleFactor=0.0;
            ScreenshotScaleFactorInf=0.0;
            ScreenshotScaleFactorMin=0.5;
            ScreenshotScaleFactorMax=2.0;
            ScreenshotScaleFactorStep=0.1;
            ScreenshotImageWidth=0.0;
            ScreenshotImageHeight=0.0;
            LoadingData=false;
            ImageLoaded=false;
            ScreenShotFormChanged=false;
        }
        
        /// <summary>
        /// форма с полями для фильтрации данных
        /// </summary>
        public FormHelper ScreenShotForm { get; set; }

        /// <summary>
        /// данные формы изменились
        /// </summary>
        private bool ScreenShotFormChanged { get;set;}

        /// <summary>
        /// имя хоста
        /// </summary>
        public string HostUserId{get;set;}
        public string Title{get;set;}

        /// <summary>
        /// флаг поднимается на время ожидания данных от сервера
        /// </summary>
        private bool LoadingData {get;set;}

        /// <summary>
        /// изображение загружено
        /// </summary>
        private bool ImageLoaded {get;set;}

        private MemoryStream ImageStream {get;set;}

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void ScreenShotGridInit()
        {
            //инициализация формы
            {
                ScreenShotForm = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="FROM_DATE",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=FromDate,
                        ControlType="TextBox",
                        Default=DateTime.Now.AddHours(-1).ToString("dd.MM.yyyy HH:mm:ss"),
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="TO_DATE",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=ToDate,
                        ControlType="TextBox",
                        Default=DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                };

                ScreenShotForm.SetFields(fields);

                //после установки значений
                ScreenShotForm.AfterSet = (Dictionary<string, string> v) =>
                {
                    //фокус на кнопку обновления
                    RefreshButton.Focus();
                };
            }

            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Дата",
                        Path="DATE",
                        Doc="",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="HH:mm:ss",
                        MinWidth=30,
                        MaxWidth=250,
                    },                    
                };
                ScreenShotGrid.SetColumns(columns);

                ScreenShotGrid.PrimaryKey="DATE";
                ScreenShotGrid.SetSorting("DATE", ListSortDirection.Ascending);
                ScreenShotGrid.SearchText = SearchText;
                ScreenShotGrid.AutoUpdateInterval=0;
                ScreenShotGrid.UseRowHeader = false;
                ScreenShotGrid.AutoUpdateInterval=0;
                ScreenShotGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                ScreenShotGrid.OnSelectItem = selectedItem =>
                {
                    SelectImage();
                };

                //данные грида
                ScreenShotGrid.OnLoadItems = ScreenShotLoadItems;

                //фокус ввода           
                ScreenShotGrid.Focus();
            }
        }

        

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Service",
                ReceiverName = "",
                SenderName = "ClientList",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            ScreenShotGrid.Destruct();
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            ScreenShotForm.SetDefaults();
            CheckFormChanged(true);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("Client") > -1)
            {
                switch (m.Action)
                {
                    case "Refresh":
                        ScreenShotGrid.LoadItems();

                        // выделение на новую строку
                        var id = m.Message.ToInt();
                        ScreenShotGrid.SetSelectedItemId(id);
                        break;
                }
            }
        }

        /// <summary>
        /// получение записей
        /// </summary>
        public async void ScreenShotLoadItems()
        {
            bool resume = true;
            var p = new Dictionary<string, string>();

            if (resume)
            {
                if(LoadingData)
                {
                    resume=false;
                }
            }

            DisableControls();

            if (resume)
            {
                var validateResult=ScreenShotForm.Validate();
                if(validateResult)
                {
                    p=ScreenShotForm.GetValues();
                }
                else
                {
                    resume  =false;
                }
            }

            if (resume)
            {

                {
                    p.CheckAdd("HOST_USER_ID",HostUserId);
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Service");
                q.Request.SetParam("Object", "PhotoScreen");
                q.Request.SetParam("Action", "ListImage");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

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
                            ScreenShotGrid.UpdateItems(ds);
                        }
                    }
                }
            }

            EnableControls();
            CheckFormChanged();
        }

        /// <summary>
        /// получение изображения
        /// </summary>
        public async void LoadImage(string filePath)
        {
            bool resume = true;
            bool complete=false;
            ImageStream=new MemoryStream();
            var p = new Dictionary<string, string>();

            if (resume)
            {
                if(LoadingData)
                {
                    resume=false;
                }
            }

            DisableControls();

            if (resume)
            {
                {
                    p.CheckAdd("FILE_PATH",filePath);
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "FileStorage");
                q.Request.SetParam("Object", "File");
                q.Request.SetParam("Action", "DownloadByName");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;
                q.Request.RequiredAnswerType=LPackClientAnswer.AnswerTypeRef.Stream;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if(q.Answer.Status == 0)                
                {
                    if(q.Answer.DataStream!=null)
                    {
                        complete=true;
                        ImageStream=q.Answer.DataStream;
                                              
                    }
                }
            }

            {
                ScreenshotImage.Source=null;
                if(complete)
                {
                    try
                    {
                        BitmapImage image = new BitmapImage();
                        image.BeginInit();
                        image.StreamSource = ImageStream;
                        image.EndInit();
                        ScreenshotImage.Source=image;

                        ScreenshotImageWidth=image.Width;
                        ScreenshotImageHeight=image.Height;

                        ImageLoaded=true;
                    }
                    catch(Exception e)
                    {
                    }  
                }
                ScreenshotImageScale(0);                
            }

            EnableControls();
            ScreenshotImageUpdateActions();
        }

        public void ExportToFile()
        {
            if(ImageLoaded)
            {
                if(ImageStream.Length > 0)
                {
                    try
                    {
                        var filePath = Central.GetTempFilePathWithExtension("jpg");        
                        var fileStream = new FileStream(filePath, FileMode.Create, System.IO.FileAccess.Write);
                        ImageStream.WriteTo(fileStream);
                        fileStream.Close();
                        Central.OpenFile(filePath);
                    }
                    catch(Exception e)
                    {
                    }
                }
            }
        }

        /// <summary>
        /// фильтрация записей (аккаунты)
        /// </summary>
        public void FilterItems()
        {
            UpdateActions(null);
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            ScreenShotGrid.ShowSplash();
            LoadingData=true;
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            ScreenShotGrid.HideSplash();
            LoadingData=false;
        }


        public void Edit()
        {
            ScreenShotGrid.LoadItems();
            Show();
        }

        /// <summary>
        /// Отображение вкладки с формой редактирования данных водителя
        /// </summary>
        public void Show()
        {
            string tabTitle=$"{Title}";
            var tabName=GetFrameName();    
            Central.WM.AddTab(tabName,tabTitle,true,"add",this);

        }

        /// <summary>
        /// Закрытие фрейма
        /// </summary>
        public void Close()
        {
            var tabName=GetFrameName();    
            Central.WM.RemoveTab(tabName);
            Destroy();
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            var tabName=GetFrameName(); 
            Central.WM.SetActive(tabName);
        }

        private void CheckFormChanged(bool changed=false)
        {
            ScreenShotFormChanged=changed;

            if(ScreenShotFormChanged)
            {
                RefreshButton.Style=(Style)RefreshButton.TryFindResource("FButtonPrimary");
            }
            else
            {
                RefreshButton.Style=(Style)RefreshButton.TryFindResource("FButtonCancel");
            }

            //if(Grid.Items != null)
            //{
            //    if(Grid.Items.Count > 0)
            //    {
            //        ArrowLeftButton.Style=(Style)ArrowLeftButton.TryFindResource("FButtonPrimary");
            //        ArrowRightButton.Style=(Style)ArrowRightButton.TryFindResource("FButtonPrimary");
            //    }
            //    else
            //    {
            //        ArrowLeftButton.Style=(Style)ArrowLeftButton.TryFindResource("FButtonCancel");
            //        ArrowRightButton.Style=(Style)ArrowRightButton.TryFindResource("FButtonCancel");
            //    }
            //}
        }

        public string GetFrameName()
        {
            var result="";
            result=$"photoscreen_{HostUserId}";
            result=result.MakeSafeName();
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
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.F5:
                    ScreenShotGrid.LoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Home:
                    ScreenShotGrid.SetSelectToFirstRow();
                    e.Handled = true;
                    break;

                case Key.End:
                    ScreenShotGrid.SetSelectToLastRow();
                    e.Handled = true;
                    break;

                case Key.Right:
                    ScreenshotImageSelect(1);
                    e.Handled = true;
                    break;

                case Key.Left:
                    ScreenshotImageSelect(-1);
                    e.Handled = true;
                    break;

                case Key.Add:
                    ScreenshotImageScale(1);
                    e.Handled = true;
                    break;

                case Key.Subtract:
                    ScreenshotImageScale(-1);
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/service/agent/photoscreen");
        }           

        public void SelectImage()
        {
            if(ScreenShotGrid.SelectedItem != null)
            {
                var filePath=ScreenShotGrid.SelectedItem.CheckGet("FILE_PATH");
                LoadImage(filePath);
            }
        }

        public void ScreenshotImageUpdateActions()
        {
            if(ImageLoaded)
            {
                ScreenshotImageToolbar.IsEnabled=true; 
                ArrowRightButton.IsEnabled=true; 
                ArrowLeftButton.IsEnabled=true; 
            }
            else
            {
                ScreenshotImageToolbar.IsEnabled=false; 
                ArrowRightButton.IsEnabled=false; 
                ArrowLeftButton.IsEnabled=false; 
            }
        }

        public void ScreenshotImageSelect(int direction)
        {
            if(!LoadingData)
            {
                if(direction > 0)
                {
                    ScreenShotGrid.SetSelectToNextRow();
                }
                else
                {
                    ScreenShotGrid.SetSelectToPrevRow();
                }
            }
        }

        private double ScreenshotScaleFactor {get;set;}
        private double ScreenshotScaleFactorInf {get;set;}
        private double ScreenshotScaleFactorMin {get;set;}
        private double ScreenshotScaleFactorMax {get;set;}
        private double ScreenshotScaleFactorStep {get;set;}
        private double ScreenshotImageWidth {get;set;}
        private double ScreenshotImageHeight {get;set;}

        private void ScreenshotImageScaleInit()
        {
            var scrollBarSize=24;
            scrollBarSize=5;
            if(ScreenshotScaleFactor == 0.0)
            {
                //ScreenshotImageWidth=ScreenshotImageWidth;
                //ScreenshotImageHeight=ScreenshotImageHeight;
                double containerFactor=(double)ScreenshotImageContainer.ActualWidth/(double)ScreenshotImageContainer.ActualHeight;
                double imageFactor=(double)ScreenshotImageWidth/(double)ScreenshotImageHeight;

                if(imageFactor > containerFactor)
                {
                    ScreenshotScaleFactor=(ScreenshotImageContainer.ActualWidth-scrollBarSize)/ScreenshotImageWidth;
                }
                else
                {
                    ScreenshotScaleFactor=(ScreenshotImageContainer.ActualHeight-scrollBarSize)/ScreenshotImageHeight;
                }

                
                ScreenshotScaleFactorInf=ScreenshotScaleFactor;
            }
        }

        private void ScreenshotImageScaleInf()
        { 
            ScreenshotScaleFactor=ScreenshotScaleFactorInf;
            ScreenshotImageScale(0);
        }

        private void ScreenshotImageScale(int direction)
        {
            if(!LoadingData)
            {
                ScreenshotImageScaleInit();

                if(direction != 0)
                {
                    if(direction > 0)
                    {
                        ScreenshotScaleFactor=ScreenshotScaleFactor+ScreenshotScaleFactorStep;
                    }

                    if(direction < 0)
                    {
                        ScreenshotScaleFactor=ScreenshotScaleFactor-ScreenshotScaleFactorStep;
                    }

                    if(ScreenshotScaleFactor > ScreenshotScaleFactorMax)
                    {
                        ScreenshotScaleFactor = ScreenshotScaleFactorMax;
                    }

                    if(ScreenshotScaleFactor < ScreenshotScaleFactorMin)
                    {
                        ScreenshotScaleFactor = ScreenshotScaleFactorMin;
                    }
                }

                {
                    ScreenshotImage.Width=ScreenshotImageWidth*ScreenshotScaleFactor;
                    ScreenshotImage.Height=ScreenshotImageHeight*ScreenshotScaleFactor;
                }
            }
        }




        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            //Export();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            ScreenShotGrid.LoadItems();
        }

        private void ArrowLeftButton_Click(object sender, RoutedEventArgs e)
        {
            ScreenshotImageSelect(-1);
        }

        private void ArrowRightButton_Click(object sender, RoutedEventArgs e)
        {
            ScreenshotImageSelect(1);
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            ScreenshotImageScale(1);
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            ScreenshotImageScale(-1);
        }

        private void ZoomInfButton_Click(object sender, RoutedEventArgs e)
        {
            ScreenshotImageScaleInf();
        }

        private void FromDate_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckFormChanged(true);
        }

        private void ToDate_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckFormChanged(true);
        }

        private void ExportButton_Click_1(object sender, RoutedEventArgs e)
        {
            ExportToFile();
        }
    }


}
