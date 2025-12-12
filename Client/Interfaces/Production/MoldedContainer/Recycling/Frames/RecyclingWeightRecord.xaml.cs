using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Client.Interfaces.Production.MoldedContainer
{
    /// <summary>
    /// добавление записи замера веса сырой заготовки ВФМ Литой тары 
    /// </summary>
    /// <author>greshnyh_ni</author>
    public partial class RecyclingWeightRecord : ControlBase
    {
        private FormHelper Form { get; set; }

        /// <summary>
        /// Имя вкладки, откуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;
        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        public Dictionary<string, string> Values { get; set; }

        // примечание
        public string Note { get; set; }
        // номер станка
        public object MachineId { get; private set; }
        private bool ReadOnly { get; set; }

        /// <summary>
        /// Порт для считывания данных с весов
        /// </summary>
        SerialPort ComPortWeight { get; set; }

        /// <summary>
        /// Буфер выходного потока весов
        /// </summary>
        string WeightInputBuffer { get; set; }
        /// <summary>
        /// признак для получения веса заготовки
        /// </summary>
        bool WeightIs { get; set; }


        public RecyclingWeightRecord(Dictionary<string, string> p)
        {
            InitializeComponent();
            MachineId = p.CheckGet("ID_ST").ToInt(); ;

            DocumentationUrl = "/doc/l-pack-erp/production/molded_container/machine_control";

            FrameName = ControlName;


            FormInit();
            WeightIs = false;

//            ComPortInit();

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (KeyEventArgs e) =>
            {

            };

            OnLoad = () =>
            {

            };

            OnUnload = () =>
            {
                //if (ComPortWeight.IsOpen)
                //{
                //    ComPortWeight.Close();
                //}
            };

            OnFocusGot = () =>
            {

            };

            OnFocusLost = () =>
            {
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

                Commander.SetCurrentGroup("custom");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "ok",
                        Enabled = true,
                        Title = "ОК",
                        ButtonUse = true,
                        ButtonName = "SaveButton",
                        HotKey = "Enter",
                        Action = () =>
                        {
                            Save();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "cancel",
                        Enabled = true,
                        Title = "Отмена",
                        ButtonUse = true,
                        ButtonName = "CancelButton",
                        HotKey = "Escape",
                        Action = () =>
                        {
                            Close();
                        },
                    });
                }

                Commander.Init(this);
            }

            // получение прав пользователя
            ProcessPermissions();

            Values = new Dictionary<string, string>();

        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
        }

        private void FormInit()
        {
            Form = new FormHelper();

            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="ID_ST",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="SelectBox",
                    Control=Machines,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="WEIGHT",
                    Control=WeightTxt,
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
            };
            Form.SetFields(fields);

            Form.StatusControl = FormStatus;
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            var list = new Dictionary<string, string>();
            list.Add("306", "ВФМ BST[ЕС9600]-1");
            list.Add("305", "ВФМ BST[ЕС9600]-2");

            Machines.Items = list;

            Form.SetDefaults();
            DebugLog.Visibility = Visibility.Hidden;
        }

        public void Create()
        {
            Show();
        }

        public void Edit()
        {
            FrameTitle = $"Добавление веса заготовки.";
            Machines.IsReadOnly = false;

            SetDefaults();
            Form.SetValues(Values);
            Show();
        }

        /// <summary>
        /// Проверки перед записью данных в БД
        /// </summary>
        private void Save()
        {
            if (Form.Validate())
            {
                bool resume = true;
                var v = Form.GetValues();
                string errorMsg = "Не все поля заполнены верно";

                if (resume)
                {
                    if (Machines.SelectedItem.Key.ToInt() == 0)
                    {
                        resume = false;
                    }
                }
                if (resume)
                {
                    if (WeightTxt.Text.IsNullOrEmpty() || (WeightTxt.Text.ToInt() == 0))
                    {
                        resume = false;
                    }
                }

                if (resume)
                {
                    SaveData(v);
                }
                else
                {
                    Form.SetStatus(errorMsg, 1);
                }
            }
        }

        /// <summary>
        /// Добавляем данные по замеру
        /// </summary>
        /// <param name="p"></param>
        private async void SaveData(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();

            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Scales");
            q.Request.SetParam("Action", "WeightAdd");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                {
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "MoldedContainer",
                        ReceiverName = "RecyclingVacuumFormingMachineWeight",
                        SenderName = "VacuumFormingMachineWeightForm",
                        Action = "RefreshRecyclingWeightList",
                        Message = "",
                        ContextObject = p,
                    });

                    //Central.Msg.SendMessage(new ItemMessage()
                    //{
                    //    ReceiverGroup = "MoldedContainer",
                    //    ReceiverName = "RecyclingVacuumFormingMachineWeight",
                    //    SenderName = "VacuumFormingMachineWeightForm",
                    //    Action = "RefreshRecyclingWeightList",
                    //    Message = "",
                    //});
                }

                Close();
            }
            else
            {
                Form.SetStatus(q.Answer.Error.Message, 1);
            }
        }

        /// <summary>
        ///  показать лог файл получения веса
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Debug_Click(object sender, RoutedEventArgs e)
        {
            if (Debug.IsChecked == true)
                DebugLog.Visibility = Visibility.Visible;
            else
                DebugLog.Visibility = Visibility.Hidden;
        }


        /// <summary>
        /// Получить вес для отладки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WeightButton_Click(object sender, RoutedEventArgs e)
        {
            WeightButton.IsEnabled = false;
            WeightIs = true;
        //    ComPortInit();
            WeightButton.IsEnabled = true;
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        //public void ComPortInit()
        //{
        //    DebugLog.Text = "";
        //    FormStatus.Text = "";
        //    WeightTxt.Foreground = HColor.BlackFG.ToBrush();

        //    if (Central.Config.Ports != null
        //        && Central.Config.Ports.Count != 0)
        //    {
        //        var port = Central.Config.Ports[0];
        //        try
        //        {
        //            ComPortWeight = new SerialPort();
        //            ComPortWeight.DataReceived += ComPort_DataReceived;

        //            ComPortWeight.PortName = port.PortName;
        //            ComPortWeight.BaudRate = port.BaudRate.ToInt();
        //            ComPortWeight.DataBits = port.DataBits.ToInt();
        //            ComPortWeight.ReadTimeout = port.ReadTimeout.ToInt();

        //            if (port.Parity.ToUpper() == "NONE")
        //            {
        //                ComPortWeight.Parity = Parity.None;
        //            }
        //            else if (port.Parity.ToUpper() == "ODD")
        //            {
        //                ComPortWeight.Parity = Parity.Odd;
        //            }
        //            else if (port.Parity.ToUpper() == "EVEN")
        //            {
        //                ComPortWeight.Parity = Parity.Even;
        //            }
        //            else if (port.Parity.ToUpper() == "MARK")
        //            {
        //                ComPortWeight.Parity = Parity.Mark;
        //            }
        //            else if (port.Parity.ToUpper() == "SPACE")
        //            {
        //                ComPortWeight.Parity = Parity.Space;
        //            }

        //            if (port.StopBits == "0")
        //            {
        //                ComPortWeight.StopBits = StopBits.None;
        //            }
        //            else if (port.Parity == "1")
        //            {
        //                ComPortWeight.StopBits = StopBits.One;
        //            }
        //            else if (port.Parity == "1.5" || port.Parity == "1,5")
        //            {
        //                ComPortWeight.StopBits = StopBits.OnePointFive;
        //            }
        //            else if (port.Parity == "2")
        //            {
        //                ComPortWeight.StopBits = StopBits.Two;
        //            }


        //            if (ComPortWeight.IsOpen)
        //            {
        //                ComPortWeight.Close();
        //            }
        //            ComPortWeight.Open();
        //            WeightTxt.Foreground = HColor.GreenFG.ToBrush();
        //        }
        //        catch
        //        {
        //            WeightTxt.Foreground = HColor.RedFG.ToBrush();
        //            FormStatus.Text = "Нет связи с весами!";
        //        }
        //    }
        //    else
        //    {
        //        FormStatus.Text = "Не указан ComPort для связи с весами!";
        //    }
        //}

        /// <summary>
        /// Функция вызывается каждый раз, когда через порт поступают новые данные 
        /// <para>(На практике не совсем каждый раз. Здесь не критично, но нужно быть осторожным)</para>
        /// </summary>
        //private void ComPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        //{
        //    if (WeightIs == false)
        //        return;

        //    var port = (SerialPort)sender;
        //    System.Threading.Thread.Sleep(1000);

        //    try
        //    {
        //        WeightInputBuffer += ComPortWeight.ReadExisting();

        //        //this.Dispatcher.Invoke(() =>
        //        //{
        //        //    DebugLog.Text = DebugLog.Text.Append(WeightInputBuffer, true);
        //        //});


        //        // ждём, пока в буфере накопится как минимум одна полная строка вывода весов,
        //        // отделенная от других строк с обеих сторон символами S 
        //        if (WeightInputBuffer.Count(c => c == 'S') > 1)
        //        {
        //            string[] lines = WeightInputBuffer.Split('S');
        //            // гарантированно полная строка вывода весов
        //            string line = lines[1];

        //            line = line.Substring(0, 5);

        //            // оставляем только цифры и точку - десятичный разделитель
        //            string weightStr = new String(line.Where(c => Char.IsDigit(c)).ToArray());
                    
        //            double weightDouble;
        //            // убираем ведущие ноли
        //            // и проверяем еще раз, что это точно число
        //            if (double.TryParse(weightStr, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out weightDouble))
        //            {
        //                int weight = (int)Math.Truncate(weightDouble);

        //                //  int weight = 555;

        //                this.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate ()
        //                {
        //                    WeightTxt.Text = $"{weight}";
        //                    WeightIs = false;
        //                }));
        //            }
        //        }

        //        // на всякий случай, чтобы функция не перегрузила поток
        //        System.Threading.Thread.Sleep(10);

        //        // очищаем буфер
        //        WeightInputBuffer = "";

        //    }
        //    catch
        //    {
        //        if (ComPortWeight.IsOpen)
        //        {
        //            ComPortWeight.Close();
        //        }
        //    }
        //}


    }
}
