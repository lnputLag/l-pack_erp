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
using System.Windows.Input;
using System.Windows.Media.Imaging;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Service
{
    /// <summary>
    /// скриншоты, список доступных машин
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-02-27</released>
    /// <changed>2023-02-27</changed>
    public partial class MachineScreenShotList : ControlBase
    {
        public MachineScreenShotList()
        {
            InitializeComponent();

            ControlSection = "screenshot";
            RoleName = "[erp]screenshots";
            ControlTitle ="Скриншоты";
            DocumentationUrl = "/doc/l-pack-erp/service/screenshots";

            OnMessage = (ItemMessage m) =>
            {
                if(m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if(!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };

            OnLoad =()=>
            {
                MachineFormInit();
                ScreenShotFormInit();
                SetDefaults();
                MachineGridInit();
                ScreenShotGridInit();            
                
            };

            OnUnload=()=>
            {
                MachineGrid.Destruct();
            };

            OnFocusGot=()=>
            {
                MachineGrid.ItemsAutoUpdate=true;
            };

            OnFocusLost=()=>
            {
                MachineGrid.ItemsAutoUpdate=false;
            };

            OnNavigate = () =>
            {
            };

            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "help",
                        Enabled = true,
                        Title = "Справка",
                        Description = "Показать справочную информацию",
                        ButtonUse = true,
                        ButtonName = "HelpButton",
                        HotKey = "F1",
                        Action = () =>
                        {
                            Central.ShowHelp(DocumentationUrl);
                        },
                    });
                }

                Commander.SetCurrentGridName("MachineGrid");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "refresh_mashine",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "RefreshButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            MachineGrid.LoadItems();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "show_info",
                        Enabled = true,
                        Title = "Информация",
                        MenuUse = true,
                        Action = () =>
                        {
                            var s=MachineGrid.SelectedItem.GetDumpString();
                            var reportViewer = new ReportViewer();
                            reportViewer.Content = s;
                            reportViewer.Init();
                            reportViewer.Show();
                        },
                    });
                }

                Commander.SetCurrentGridName("ScreenShotGrid");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "refresh_screenshot",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "ScreenShotRefreshButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            ScreenShotGrid.LoadItems();
                        },
                    });
                }

                Commander.Init(this);
            }


            HostUserId="";
            HostName="";
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

        public FormHelper MachineGridForm { get; set; }
        public FormHelper ScreenShotForm { get; set; }
        private bool ScreenShotFormChanged { get;set;}
        public string HostUserId{get;set;}
        public string HostName{get;set;}
        public string Title{get;set;}
        private bool LoadingData {get;set;}
        private bool ImageLoaded {get;set;}
        private MemoryStream ImageStream {get;set;}
        private double ScreenshotScaleFactor {get;set;}
        private double ScreenshotScaleFactorInf {get;set;}
        private double ScreenshotScaleFactorMin {get;set;}
        private double ScreenshotScaleFactorMax {get;set;}
        private double ScreenshotScaleFactorStep {get;set;}
        private double ScreenshotImageWidth {get;set;}
        private double ScreenshotImageHeight {get;set;}

        public void MachineFormInit()
        {
            MachineGridForm = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="FACTORY_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Default="1",
                    Control=FactoryId,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnCreate = (FormHelperField f) =>
                    {
                        var list=new Dictionary<string,string>()
                        {
                            {"1", "Липецк" },
                            {"2", "Кашира" },
                        };
                        var c=(SelectBox)f.Control;
                        if(c != null)
                        {
                            c.Items=list;
                        }
                    },
                    OnChange=(FormHelperField f, string v)=>
                    {
                        MachineGrid.LoadItems();
                    },
                },
            };
            MachineGridForm.SetFields(fields);
        }

        public void ScreenShotFormInit()
        {
            ScreenShotForm = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="FROM_DATE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=FromDate,
                    ControlType="TextBox",
                    Default=DateTime.Now.ToString("dd.MM.yyyy"),
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
                new FormHelperField()
                {
                    Path="TO_DATE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ToDate,
                    ControlType="TextBox",
                    Default=DateTime.Now.ToString("dd.MM.yyyy"),
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
                new FormHelperField()
                {
                    Path="FROM_TIME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=FromTime,
                    ControlType="TextBox",
                    Default=DateTime.Now.AddHours(-1).ToString("HH:mm:ss"),
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
                new FormHelperField()
                {
                    Path="TO_TIME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ToTime,
                    ControlType="TextBox",
                    Default=DateTime.Now.ToString("HH:mm:ss"),
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
            };
            ScreenShotForm.SetFields(fields);
        }

        public void SetDefaults()
        {         
            MachineGridForm.SetDefaults();
            ScreenShotForm.SetDefaults();
            ScreenshotImageUpdateActions();
        }

        public void MachineGridInit()
        {
            {
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Рабочее место",
                        Path="CLIENT_TITLE",
                        Doc="",
                        ColumnType=ColumnTypeRef.String,
                        Width2=20,
                    },
                    new DataGridHelperColumn
                    {
                        Header="С",
                        Path="DATE_FIRST",
                        Doc="",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yy",
                        Width2=8,                        
                    },
                    new DataGridHelperColumn
                    {
                        Header="По",
                        Path="DATE_LAST",
                        Doc="",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yy",
                        Width2=8,
                    },


                    new DataGridHelperColumn
                    {
                        Header="Расположение",
                        Path="CLIENT_PLACEMENT",
                        Doc="",
                        ColumnType=ColumnTypeRef.String,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="HOST_USER_ID",
                        Path="HOST_USER_ID",
                        Doc="",
                        ColumnType=ColumnTypeRef.String,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Hostname",
                        Path="SYSTEM_HOSTNAME",
                        Doc="",
                        ColumnType=ColumnTypeRef.String,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Username",
                        Path="SYSTEM_USER_NAME2",
                        Doc="",
                        ColumnType=ColumnTypeRef.String,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="STORAGE_CODE",
                        Path="STORAGE_CODE",
                        Doc="",
                        ColumnType=ColumnTypeRef.String,
                        Visible=false,
                    },                    
                    new DataGridHelperColumn
                    {
                        Header="FACTORY_ID",
                        Path="FACTORY_ID",
                        Doc="",
                        ColumnType=ColumnTypeRef.String,
                        Visible=false,
                    },  
                };
                MachineGrid.SetColumns(columns);
                MachineGrid.SetPrimaryKey("HOST_USER_ID");
                MachineGrid.SetSorting("CLIENT_TITLE", ListSortDirection.Ascending);
                MachineGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                MachineGrid.Toolbar = MachineGridToolbar;
                MachineGrid.OnSelectItem = selectedItem =>
                {
                    if(MachineGrid.SelectedItem != null)
                    {
                        ScreenshotImage.Source=null;
                        HostUserId=MachineGrid.SelectedItem.CheckGet("HOST_USER_ID");
                        HostName=MachineGrid.SelectedItem.CheckGet("SYSTEM_HOSTNAME");
                        ScreenshotScaleFactor = 0.0;
                        ScreenShotGrid.LoadItems();
                    }
                };
                MachineGrid.OnLoadItems = async ()=>
                {
                    //MachineGrid.DisableControls();

                    var today=DateTime.Now;
                    bool resume = true;

                    if (resume)
                    {
                        var p = new Dictionary<string, string>();

                        {
                            var storageCode="agent_screen_shot";
                            var factoryId=1;
                            if(MachineGridForm!=null)
                            {
                                var v=MachineGridForm.GetValues();
                                factoryId=v.CheckGet("FACTORY_ID").ToInt();
                                if(factoryId==2)
                                {
                                    storageCode="agent_ks_screen_shot";
                                }
                            }       
                            p.CheckAdd("STORAGE_CODE", storageCode.ToString());
                            p.CheckAdd("FACTORY_ID", factoryId.ToString());
                        }

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Service");
                        q.Request.SetParam("Object", "Control");
                        q.Request.SetParam("Action", "ListPhotoScreen");
                        q.Request.SetParams(p);

                        q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                        q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

                        await Task.Run(() =>
                        {
                            q.DoQuery();
                        });

                        if(q.Answer.Status == 0)
                        {
                            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                            if(result != null)
                            {
                                var ds = ListDataSet.Create(result, "ITEMS");
                                MachineGrid.UpdateItems(ds);
                            }
                        }
                    }

                    //MachineGrid.EnableControls();
                };
                MachineGrid.Commands = Commander;
                MachineGrid.Init(); 
            }

            
        }

        public void ScreenShotGridInit()
        {
            {
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Дата",
                        Path="DATE",
                        Doc="",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="HH:mm:ss",
                        Width2=20,
                    },                    

                    new DataGridHelperColumn
                    {
                        Header="FILE_NAME",
                        Path="FILE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Visible=false,
                    },   
                    new DataGridHelperColumn
                    {
                        Header="FILE_PATH",
                        Path="FILE_PATH",
                        ColumnType=ColumnTypeRef.String,
                        Visible=false,
                    },   
                    new DataGridHelperColumn
                    {
                        Header="_ROWNUMBER",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Visible=false,
                    }, 
                };
                ScreenShotGrid.SetColumns(columns);
                ScreenShotGrid.SetPrimaryKey("_ROWNUMBER");
                ScreenShotGrid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
                ScreenShotGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                ScreenShotGrid.AutoUpdateInterval=0;
                ScreenShotGrid.OnSelectItem = selectedItem =>
                {
                    if(ScreenShotGrid.SelectedItem != null)
                    {
                        var filePath=ScreenShotGrid.SelectedItem.CheckGet("FILE_PATH");
                        ScreenshotLoadImage(filePath);
                    }
                };
                ScreenShotGrid.OnLoadItems = async ()=>
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
                        var f1 = p.CheckGet("FROM_DATE");
                        var f2 = p.CheckGet("FROM_TIME");
                        var f = $"{f1} {f2}".ToDateTime();

                        var t1 = p.CheckGet("TO_DATE");
                        var t2 = p.CheckGet("TO_TIME");
                        var t = $"{t1} {t2}".ToDateTime();

                        if (DateTime.Compare(f, t) > 0)
                        {
                            var msg = "Дата начала должна быть меньше даты окончания.";
                            var d = new DialogWindow($"{msg}", "Проверка данных");
                            d.ShowDialog();
                            resume = false;
                        }

                        var dt=(TimeSpan)(t-f);
                        if (dt.TotalHours > 24)
                        {
                            var msg = "Интервал между датами должен быть не более суток.";
                            var d = new DialogWindow($"{msg}", "Проверка данных");
                            d.ShowDialog();
                            resume = false;
                        }

                        p.CheckAdd("FROM_DATE",f.ToString("dd.MM.yyyy HH:mm:ss"));
                        p.CheckAdd("TO_DATE",t.ToString("dd.MM.yyyy HH:mm:ss"));

                        {
                            //var storageCode="agent_screen_shot";
                            //var factoryId=1;
                            //if(MachineGridForm!=null)
                            //{
                            //    var v=MachineGridForm.GetValues();
                            //    factoryId=v.CheckGet("FACTORY_ID").ToInt();
                            //    if(factoryId==2)
                            //    {
                            //        storageCode="agent_ks_screen_shot";
                            //    }
                            //}       
                            //p.CheckAdd("STORAGE_CODE", storageCode.ToString());
                            //p.CheckAdd("FACTORY_ID", factoryId.ToString());
                            

                            p.CheckAdd("STORAGE_CODE", MachineGrid.SelectedItem.CheckGet("STORAGE_CODE"));
                            p.CheckAdd("FACTORY_ID", MachineGrid.SelectedItem.CheckGet("FACTORY_ID"));
                        }
                    }

                    if (resume)
                    {
                        //ScreenShotGrid.DisableControls();

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

                        //ScreenShotGrid.EnableControls();
                    }                    
                };
                ScreenShotGrid.Commands = Commander;
                ScreenShotGrid.Init();   
            }            
        }

        private async void ScreenshotLoadImage(string filePath)
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

            if (resume)
            {
                //ScreenShotGrid.DisableControls();

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

                //ScreenShotGrid.EnableControls();
            }

            if (resume)
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
                    ScreenshotImageScale(0);                
                }

                ScreenshotImageUpdateActions();
            }
        }

        private void ScreenshotImageUpdateActions()
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

        private void ScreenshotImageScaleInit()
        {
            var scrollBarSize=24;
            scrollBarSize=5;
            if(ScreenshotScaleFactor == 0.0)
            {
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

        private void ScreenShotExportToFile()
        {
            if(ImageLoaded)
            {
                if(ImageStream.Length > 0)
                {
                    try
                    {
                        var today=DateTime.Now.ToString("dd.MM.yyyy_HH-mm-ss");
                        var machine=HostName;
                        var f=$"{machine}_{today}";
                        var filePath = Central.GetTempFilePathWithExtension("jpg",f);
                        var fileStream = new FileStream(filePath, FileMode.Create, System.IO.FileAccess.Write);
                        ImageStream.WriteTo(fileStream);
                        fileStream.Close();
                        //Central.OpenFile(filePath);
                        Central.SaveFile(filePath);
                    }
                    catch(Exception e)
                    {
                    }
                }
            }
        }

        private void ScreenShotPrint()
        {
            PrintDialog pd = new PrintDialog();
            pd.PrintTicket.PageOrientation = System.Printing.PageOrientation.Landscape;
            if(pd.ShowDialog() == true)
            {
                pd.PrintVisual(ScreenshotImage, "Print a Large Image");
            }
        }

        public void ScreenshotImageSelect(int direction)
        {
            if(!LoadingData)
            {
                if(direction > 0)
                {
                    ScreenShotGrid.SelectRowNext();
                }
                else
                {
                    ScreenShotGrid.SelectRowPrev();
                }
            }
        }
      
        /*
        public void _ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.F5:
                    ScreenShotGrid.LoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    //ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Home:
                    //ScreenShotGrid.SetSelectToFirstRow();
                    e.Handled = true;
                    break;

                case Key.End:
                    //ScreenShotGrid.SetSelectToLastRow();
                    e.Handled = true;
                    break;
                
                case Key.Down:
                    ScreenshotImageSelect(1);
                    e.Handled = true;
                    break;

                case Key.Up:
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
        */

        


       

     

        private void ToDate_TextChanged(object sender, TextChangedEventArgs e)
        {

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

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            ScreenShotExportToFile();
        }


        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            ScreenShotPrint();
        }
    }
}
