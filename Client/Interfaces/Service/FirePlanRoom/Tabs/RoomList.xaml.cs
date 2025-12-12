using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static Client.Interfaces.Main.FormDialog;

namespace Client.Interfaces.Service
{
    /// <summary>
    /// Список датчиков в помещении на БДМ2 для отображения их на плане при пожаре
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <changed></changed> 
    public partial class RoomList : ControlBase //UserControl
    {
        public RoomList()
        {
            InitializeComponent();
            ControlTitle = "Пожарные датчики";
            TabName = "RoomList";
            RoleName = "[erp]accounts";

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            //OnMessage = (ItemMessage m) =>
            //{
            //  //  if (m.ReceiverGroup.IndexOf("Service") > -1)
            //    {
            //        if (m.ReceiverName.IndexOf("fire_plane_room") > -1)
            //        {
            //            switch (m.Action)
            //            {
            //                case "Refresh":
            //                    Grid.LoadItems();
            //                    break;
            //            }
            //        }
            //    }
            //};

            OnKeyPressed = (KeyEventArgs e) =>
            {
                /*  
                  if (!e.Handled)
                  {
                      Commander.ProcessKeyboard(e);
                  }

                  if (!e.Handled)
                  {
                      Grid.ProcessKeyboard(e);
                  }
                  */
            };

            OnLoad = () =>
            {
                FormInit();
                SetDefaults();
                GridInit();
            };

            OnUnload = () =>
            {
                Grid.Destruct();
            };

            OnFocusGot = () =>
            {
            };

            OnFocusLost = () =>
            {
            };

            ProcessPermissions();
        }

        #region Common

        ///
        /// имя пути с файлами картинок
        /// 
        private string PathResource = "pack://application:,,,/Assets/Images/FirePlanRoom/";
        private string PrevImageDir = "";

        /// <summary>
        /// Имя вкладки
        /// </summary>
        public string TabName;

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// Список групп, в которые входит пользователь
        /// </summary>
        public List<string> UserGroups { get; set; }

        /// <summary>
        /// право пользователя изменять запись
        /// </summary>
        bool UserChange;

        /// <summary>
        /// форма с полями для фильтрации данных
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// интервал неактивности клиента
        /// </summary>
        public int OnlineTimeout { get; set; }

        public double ImageHeight { get; private set; }
        public double ImageWidth { get; private set; }

        delegate void DrawIntoBmp(Graphics gx, Bitmap bmp);
        /// <summary>
        /// Текущий выбранный план
        /// </summary>
        private string ImageDir { get; set; }

        /// <summary>
        /// массив координат датчиков
        /// </summary>
        RectangleF[] rectsCoord = new RectangleF[300];

        /// <summary>
        /// массив названий датчиков
        /// </summary>
        string[] rectsStr = new string[300];

        private string currentId = string.Empty;
        #endregion

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m">сообщение</param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("Service") > -1)
            {
                if (m.ReceiverName.IndexOf("RoomList") > -1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            int fiasId = SelectedItem.CheckGet("FIAS_ID").ToInt();
                            int id = m.Message.ToInt();
                            if (id != fiasId)
                            {
                                currentId = id.ToString() + ".0";
                            }
                            Grid.LoadItems();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel(this.RoleName);
            var userAccessMode = mode;
            switch (mode)
            {
                case Role.AccessMode.Special:
                    break;

                case Role.AccessMode.FullAccess:
                    break;

                case Role.AccessMode.ReadOnly:
                default:
                    break;
            }

            //UIUtil.SetFrameworkElementEnabledByTagAccessMode(this.Content as DependencyObject, Acl.AccessMode.ReadOnly);

            List<Button> buttons = UIUtil.GetVisualChilds<Button>(this.Content as DependencyObject);
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    var buttonTagList = UIUtil.GetTagList(button);
                    var accessMode = Acl.FindTagAccessMode(buttonTagList);
                    if (accessMode > userAccessMode)
                    {
                        button.IsEnabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// инициализация формы
        /// </summary>
        public void FormInit()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="SEARCH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Search,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PLANROOM",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Default="0",
                    Control=PlanRoom,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange=(FormHelperField f, string v)=>
                    {
                        Grid.LoadItems();
                    },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "Service",
                        Object = "FireAlarm",
                        Action = "List",
                        AnswerSectionKey="ITEMS",
                        OnComplete = (FormHelperField f,ListDataSet ds) =>
                        {
                            var row = new Dictionary<string, string>();
                            ds.ItemsPrepend(row);
                            var list=ds.GetItemsList("ID","NAME");
                            var c=(SelectBox)f.Control;
                            if(c != null)
                            {
                                c.Items=list;
                                PlanRoom.SelectedItem = list.FirstOrDefault((x) => x.Key == "1");
                            }
                        },
                    },
                },

            };
            Form.SetFields(fields);
        }

        /// <summary>
        /// настройки по умолчанию
        /// </summary>
        public void SetDefaults()
        {

        }

        /// <summary>
        /// инициализация таблицы со списком датчиков
        /// </summary>
        public void GridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="FIAS_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Датчик",
                    Path="NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header="X",
                    Path="X_COORDINATE",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2 = 8,
                },
                new DataGridHelperColumn
                {
                    Header="Y",
                    Path="Y_COORDINATE",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2 = 8,
                },
                new DataGridHelperColumn
                {
                    Header="Активный",
                    Path="ACTIVE_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header="Надпись",
                    Path="NAME_PLANE",
                    ColumnType=ColumnTypeRef.String,
                    Width2 = 25,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Width2 = 170,
                },
            };
            Grid.SetColumns(columns);
            Grid.PrimaryKey = "FIAS_ID";
            Grid.SetSorting("NAME", ListSortDirection.Ascending);
            Grid.SearchText = Search;
            Grid.AutoUpdateInterval = 0;
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.SelectItemMode = 1;
            Grid.Init();

            //данные грида
            Grid.OnLoadItems = GridLoadItems;
            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = (Dictionary<string, string> selectedItem) =>
            {
                if (selectedItem.Count > 0)
                {
                    SelectedItem = selectedItem;
                }
            };

            //двойной клик на строке 
            Grid.OnDblClick = selectedItem =>
            {
                //рисуем квадрат, где находится датчик
                Array.Clear(rectsCoord, 0, 300);
                Array.Clear(rectsStr, 0, 300);

                rectsCoord[0] = new RectangleF(SelectedItem.CheckGet("X_COORDINATE").ToInt() - 7, SelectedItem.CheckGet("Y_COORDINATE").ToInt() + 23, 13, 13);
                rectsStr[0] = SelectedItem.CheckGet("NAME").ToString();
                ShowSensor(1);
            };

            Grid.Run();
            //фокус ввода           
            Grid.Focus();
        }

        /// <summary>
        /// загрузка данных грида
        /// </summary>
        private async void GridLoadItems()
        {
            var p = new Dictionary<string, string>();
            {
                p.Add("FIAL_ID", PlanRoom.SelectedItem.Key.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Service");
            q.Request.SetParam("Object", "FireAlarm");
            q.Request.SetParam("Action", "SensorList");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Grid.UpdateItemsAnswer(q.Answer, "ITEMS");

                if (currentId != string.Empty)
                {
                    Grid.SelectRowByKey(currentId, "FIAS_ID");
                    currentId = string.Empty;
                }
            }
            else
            {
                //  q.ProcessError();
            }
        }

        /// <summary>
        /// Обновить план
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            PrevImageDir = "";
            Array.Clear(rectsCoord, 0, 300);
            Array.Clear(rectsStr, 0, 300);
            rectsCoord[0] = new RectangleF(0, 0, 0, 0);
            rectsStr[0] = "";
            ShowSensor(1);
            Grid.LoadItems();
        }

        /// <summary>
        /// Получение списка групп, в которые входит пользователь
        /// </summary>
        private async void LoadUserGroup()
        {
            UserGroups = new List<string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Accounts");
            q.Request.SetParam("Object", "Group");
            q.Request.SetParam("Action", "GroupListByUser");
            q.Request.SetParam("ID", Central.User.EmployeeId.ToString());

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
                    var employeeGroups = ListDataSet.Create(result, "ITEMS");
                    if (employeeGroups.Items.Count > 0)
                    {
                        foreach (var item in employeeGroups.Items)
                        {
                            if (item.CheckGet("WOGR_ID").ToInt() != 1)
                            {
                                string groupCode = item.CheckGet("CODE");
                                if (!string.IsNullOrEmpty(groupCode))
                                {
                                    UserGroups.Add(groupCode);
                                }
                            }
                        }
                    }
                }

                // включение разрешений на действия
                UserChange = UserGroups.Contains("programmer");
                /*
                    CreateButton.IsEnabled = UserChange;
                    EditButton.IsEnabled = UserChange;
                    DeleteButton.IsEnabled = UserChange;
              */
                // для отладки  
                UserChange = true;
            }

        }

        // получаем координату мыши
        private void ImagePlan_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((ImageHeight > 0) && (e.LeftButton == MouseButtonState.Pressed))
            {
                System.Windows.Point pt = e.GetPosition(ImagePlan);
                var kh = ImagePlan.ActualHeight / ImageHeight;
                var kw = ImagePlan.ActualWidth / ImageWidth;

                Coordinata_X.Text = Math.Round((pt.X / kh)).ToString();
                Coordinata_Y.Text = Math.Round((pt.Y / kw - 30)).ToString();
            }
        }

        // загрузка картинки при смене локации
        private void PlanRoom_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LoadImage();
        }

        private void LoadImage()
        {
            switch (PlanRoom.SelectedItem.Key)
            {
                case "0":
                    {
                        return;
                    }
                    break;
                case "1":
                    {
                        ImageDir = "Abk0";
                    }
                    break;
                case "2":
                    {
                        ImageDir = "Proizv0";
                    }
                    break;
                case "3":
                    {
                        ImageDir = "Abk55";
                    }
                    break;
                case "4":
                    {
                        ImageDir = "AbkPotolok";
                    }
                    break;
                case "5":
                    {
                        ImageDir = "Proizv55";
                    }
                    break;
                case "6":
                    {
                        ImageDir = "Nasos";
                    }
                    break;
                case "7":
                    {
                        ImageDir = "Vakum";
                    }
                    break;
                case "8":
                    {
                        ImageDir = "Maslo";
                    }
                    break;
                case "9":
                    {
                        ImageDir = "Vry13";
                    }
                    break;
                case "10":
                    {
                        ImageDir = "Vry2";
                    }
                    break;
                case "11":
                    {
                        ImageDir = "Bdm2";
                    }
                    break;
                case "12":
                    {
                        ImageDir = "Transport";
                    }
                    break;
            }

            ImagePlan.Source = GetBitmap($"{PathResource}{ImageDir}.png");
        }

        private BitmapImage BmpImageFromBmp(Bitmap bmp)
        {
            using (var memory = new System.IO.MemoryStream())
            {
                bmp.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }

        private Bitmap BitmapImageToBitmap(BitmapImage bitmapImage)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }

        private BitmapImage GetBitmap(string imagePath, DrawIntoBmp func = null)
        {
            BitmapImage room = new BitmapImage();
            room.BeginInit();
            room.UriSource = new Uri(imagePath);
            room.EndInit();

            ImageHeight = room.PixelHeight;
            ImageWidth = room.PixelWidth;

            using (var bmp = BitmapImageToBitmap(room))
            {
                using (var gfx = Graphics.FromImage(bmp))
                {
                    func?.Invoke(gfx, bmp);
                }

                room = BmpImageFromBmp(bmp);
            }

            return room;
        }

        private BitmapImage GetBitmap(BitmapImage room, DrawIntoBmp func = null)
        {
            ImageHeight = room.PixelHeight;
            ImageWidth = room.PixelWidth;

            using (var bmp = BitmapImageToBitmap(room))
            {
                using (var gfx = Graphics.FromImage(bmp))
                {
                    func?.Invoke(gfx, bmp);
                }

                room = BmpImageFromBmp(bmp);
            }

            return room;
        }

        /// <summary>
        /// отображаем датчик
        /// vid = 1 квадрат
        /// vid = 2 синий круг
        /// vid = 3 красный круг 
        /// </summary>
        /// <param name="vid"></param>
        private void ShowSensor(int vid)
        {
            DrawIntoBmp drawFunc = (gx, bmp) =>
            {
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.Red, 3))
                {
                    switch (vid)
                    {
                        case 1:
                            {
                                //рисуем закрашенный прямоугольник
                                SolidBrush drawBrush = new SolidBrush(System.Drawing.Color.Red);
                                gx.FillRectangles(drawBrush, rectsCoord);
                                drawBrush.Dispose();
                                //выводим название датчика
                                Font drawFont = new Font("Tahoma", 8);
                                drawBrush = new SolidBrush(System.Drawing.Color.Black);

                                for (int i = 0; i < rectsStr.Length; i++)
                                {
                                    if (!rectsStr[i].IsNullOrEmpty())
                                    {
                                        gx.DrawString(rectsStr[i], drawFont, drawBrush, rectsCoord[i].X - 9, rectsCoord[i].Y + 11);
                                    }
                                    else
                                        break;
                                }
                                drawBrush.Dispose();
                            }
                            break;
                        case 2:
                            {
                                //рисуем синий круг и заливаем цветом
                                SolidBrush drawBrush = new SolidBrush(System.Drawing.Color.Blue);
                                //gx.FillEllipse(drawBrush, rectsCoord);
                                for (int i = 0; i < rectsStr.Length; i++)
                                {
                                    if (!rectsStr[i].IsNullOrEmpty())
                                    {
                                        gx.FillEllipse(drawBrush, rectsCoord[i]);
                                    }
                                    else
                                        break;
                                }
                                drawBrush.Dispose();
                            }
                            break;
                        case 3:
                            {
                                //рисуем красный круг и заливаем цветом 
                                SolidBrush drawBrush = new SolidBrush(System.Drawing.Color.Red);
                                //                                gx.FillEllipse(drawBrush, x - 18, y + 12, 35, 35);
                                for (int i = 0; i < rectsStr.Length; i++)
                                {
                                    if (!rectsStr[i].IsNullOrEmpty())
                                    {
                                        gx.FillEllipse(drawBrush, rectsCoord[i]);
                                    }
                                    else
                                        break;
                                }
                                drawBrush.Dispose();
                            }
                            break;
                    }
                }
            };

            if (PrevImageDir != ImageDir)
            {
                PrevImageDir = ImageDir;
                ImagePlan.Source = GetBitmap($"{PathResource}{ImageDir}.png", drawFunc);
            }
            else
            {
                ImagePlan.Source = GetBitmap(ImagePlan.Source as BitmapImage, drawFunc);
            }
        }

        // рисуем один выбранный датчик на плане в виде синего круга
        private void BlueCircleButton_Click(object sender, RoutedEventArgs e)
        {

            Array.Clear(rectsCoord, 0, 300);
            Array.Clear(rectsStr, 0, 300);
            rectsCoord[0] = new RectangleF(SelectedItem.CheckGet("X_COORDINATE").ToInt() - 18, SelectedItem.CheckGet("Y_COORDINATE").ToInt() + 12, 36, 36);
            rectsStr[0] = SelectedItem.CheckGet("NAME").ToString();
            ShowSensor(2);
        }

        // рисуем один выбранный датчик на плане в виде красного круга
        private void RedCircleButton_Click(object sender, RoutedEventArgs e)
        {
            Array.Clear(rectsCoord, 0, 300);
            Array.Clear(rectsStr, 0, 300);
            rectsCoord[0] = new RectangleF(SelectedItem.CheckGet("X_COORDINATE").ToInt() - 18, SelectedItem.CheckGet("Y_COORDINATE").ToInt() + 12, 35, 35);
            rectsStr[0] = SelectedItem.CheckGet("NAME").ToString();
            ShowSensor(3);
        }

        // показать все датчики
        private void AllShowSensorButton_Click(object sender, RoutedEventArgs e)
        {
            var i = 0;
            Array.Clear(rectsCoord, 0, 300);
            Array.Clear(rectsStr, 0, 300);
            foreach (var items in Grid.Items)
            {
                rectsCoord[i] = new RectangleF(items.CheckGet("X_COORDINATE").ToInt() - 7, items.CheckGet("Y_COORDINATE").ToInt() + 23, 13, 13);
                rectsStr[i] = items.CheckGet("NAME");
                i++;
            }
            ShowSensor(1);
        }

        // создать датчик на плане
        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            Edit(0);
        }

        // изменить датчик на плане
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null)
            {
                Edit(SelectedItem.CheckGet("FIAS_ID").ToInt());
            }
        }

        // удалить датчик на плане
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int fiasId = SelectedItem.CheckGet("FIAS_ID").ToInt();
            if (fiasId > 0)
            {
                var dw = new DialogWindow($"Вы действительно хотите удалить датчик [{SelectedItem.CheckGet("NAME")}]?", "Удаление датчика", "Подтверждение удаления датчика от клиента", DialogWindowButtons.NoYes);
                if (dw.ShowDialog() == true)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Service");
                    q.Request.SetParam("Object", "FireAlarm");
                    q.Request.SetParam("Action", "Delete");
                    q.Request.SetParam("FIAS_ID", fiasId.ToString());
                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            // вернулся не пустой ответ, обновим таблицу
                            Grid.LoadItems();
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }

        }

        /// <summary>
        /// Открывает фрейм изменения данных по датчику
        /// </summary>
        /// <param name="fiasId"></param>
        private void Edit(int fiasId)
        {

            var SensorForm = new SensorForm();
            SensorForm.ReceiverName = TabName;

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("FIAS_ID", fiasId.ToString());
                p.CheckAdd("FIAL_ID", PlanRoom.SelectedItem.Key.ToString());
                p.CheckAdd("X", Coordinata_X.Text.ToString());
                p.CheckAdd("Y", Coordinata_Y.Text.ToString());
            }

            SensorForm.Edit(p);
        }

    }
}
