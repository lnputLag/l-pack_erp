using Client.Assets.HighLighters;
using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client.Interfaces.Main
{
    /// <summary>
    /// Контрол для работы со сканером штрихкодов
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class ScannerInputControl : UserControl, IDisposable
    {
        public ScannerInputControl()
        {
            InitializeComponent();
            SetScanningStatus(ScannerInputControl.ScannerInputStatusRef.Disabled);
        }

        /// <summary>
        /// Обработка введённых данных
        /// </summary>
        /// <param name="code"></param>
        public delegate void OnBarcodeProcessDelegate(string code);
        /// <summary>
        /// Обработка введённых данных
        /// </summary>
        public OnBarcodeProcessDelegate OnBarcodeProcess;

        /// <summary>
        /// Справочник статусов контролла
        /// </summary>
        public enum ScannerInputStatusRef
        {
            /// <summary>
            /// Заблокирован
            /// </summary>
            Disabled = 1,
            /// <summary>
            /// Готов к работе
            /// </summary>
            Enabled = 2,
            /// <summary>
            /// Обработка ввода
            /// </summary>
            ProcessInput = 3,
            /// <summary>
            /// Обработка введённых данных
            /// </summary>
            ProcessBarcode = 4,
        }

        /// <summary>
        /// Текущие введённые данные
        /// </summary>
        public string Barcode
        {
            get
            {
                return BarcodeTextBox.Text;
            }

            set
            {
                BarcodeTextBox.Text = value;
            }
        }

        /// <summary>
        /// Текущий статус контролла
        /// </summary>
        private ScannerInputStatusRef Status;

        /// <summary>
        /// Интерфейс владелец этого контралла
        /// </summary>
        private ControlBase TabOwner;

        /// <summary>
        /// Флаг того, что делегат обработки введённых данных запускается асинхронно
        /// </summary>
        private bool AsyncFlag;

        /// <summary>
        /// Флаг того, что нужно ставить фокус в поле ввода штрихкода автоматически.
        /// Используется как экстренный способ.
        /// </summary>
        private bool AutoFocus;

        /// <summary>
        /// Флаг того, что инициализация контролла выполнена
        /// </summary>
        private bool Initialized;

        /// <summary>
        /// Первичная настройка контролла
        /// </summary>
        /// <param name="controlBase"></param>
        /// <param name="barcodeTextBoxVisible"></param>
        /// <param name="asyncFlag"></param>
        public void Init(ControlBase controlBase, bool barcodeTextBoxVisible = true, bool asyncFlag = true)
        {
            if (!Initialized)
            {
                this.AsyncFlag = asyncFlag;
                this.AutoFocus = Central.Config.ScannerInputAutoFocus > 0;

                if (barcodeTextBoxVisible)
                {
                    BarcodeTextBoxBorder.Visibility = Visibility.Visible;
                }
                else
                {
                    BarcodeTextBoxBorder.Visibility = Visibility.Collapsed;
                }

                TabOwner = controlBase;

                Central.MainWindow.Deactivated += MainWindow_Deactivated;
                Central.MainWindow.Activated += MainWindow_Activated;

                SetScanningStatus(ScannerInputControl.ScannerInputStatusRef.Enabled);

                Initialized = true;
            }
        }

        /// <summary>
        /// Обработка ввода данных штрихкода
        /// </summary>
        /// <param name="input"></param>
        /// <param name="e"></param>
        public void InputProcess(InputController input, System.Windows.Input.KeyEventArgs e)
        {
            if (Initialized)
            {
                // Если контрол не заблокирован и сейчас не выполняется обработка введённых данных
                if (Status != ScannerInputStatusRef.Disabled
                    && Status != ScannerInputStatusRef.ProcessBarcode)
                {
                    // Если поднят флаг того, что идёт считывание данных от сканера
                    if (input.ScanningInProgress)
                    {
                        SetScanningStatus(ScannerInputStatusRef.ProcessInput);
                    }
                    else
                    {
                        bool resume = true;

                        // Если есть считанные данные от сканера
                        if (!string.IsNullOrEmpty(input.WordScanned) && input.WordScanned != "0")
                        {
                            resume = false;
                            e.Handled = true;
                            Barcode = input.WordScanned;
                            BarcodeProcess();
                        }
                        // Если пользователь ввёл данные в поле ввода вручную
                        else if (e.Key == Key.Enter)
                        {
                            if (!string.IsNullOrEmpty(Barcode))
                            {
                                resume = false;
                                e.Handled = true;
                                BarcodeProcess();
                            }
                        }

                        if (resume)
                        {
                            SetScanningStatus(ScannerInputStatusRef.Enabled);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Установка статуса контролла
        /// </summary>
        /// <param name="scannerInputStatus"></param>
        public void SetScanningStatus(ScannerInputStatusRef scannerInputStatus)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                switch (scannerInputStatus)
                {
                    case ScannerInputStatusRef.Disabled:
                        ScannerImageBorder.Background = HColor.Red.ToBrush();
                        BarcodeTextBox.IsEnabled = false;
                        Status = scannerInputStatus;
                        Barcode = "";
                        break;

                    case ScannerInputStatusRef.ProcessInput:
                        ScannerImageBorder.Background = HColor.Green.ToBrush();
                        Status = scannerInputStatus;
                        if (AutoFocus) 
                        { 
                            BarcodeTextBox.Focus(); 
                        }
                        break;

                    case ScannerInputStatusRef.ProcessBarcode:
                        ScannerImageBorder.Background = HColor.Blue.ToBrush();
                        BarcodeTextBox.IsEnabled = false;
                        Status = scannerInputStatus;
                        break;

                    case ScannerInputStatusRef.Enabled:
                        ScannerImageBorder.Background = new SolidColorBrush(Colors.Transparent);
                        BarcodeTextBox.IsEnabled = true;
                        Status = scannerInputStatus;
                        break;
                }
            });
        }

        /// <summary>
        /// Отписываемся от событий изменения активности окна приложения.
        /// Переключаем статус контролла в Заблокирован
        /// </summary>
        public void Dispose()
        {
            Central.MainWindow.Activated -= MainWindow_Activated;
            Central.MainWindow.Deactivated -= MainWindow_Deactivated;
            SetScanningStatus(ScannerInputControl.ScannerInputStatusRef.Disabled);
            OnBarcodeProcess = null;
        }

        /// <summary>
        /// Обработка введённых данных
        /// Проводим смену статусов
        /// Вызываем делегат обработки введённых данных
        /// Очишаем введённые данные после обработки
        /// </summary>
        private async void BarcodeProcess()
        {
            SetScanningStatus(ScannerInputStatusRef.ProcessBarcode);

            try
            {
                string code = Barcode;

                if (AsyncFlag)
                {
                    await Task.Run(() =>
                    {
                        OnBarcodeProcess?.Invoke(code);
                    });

                    SetScanningStatus(ScannerInputStatusRef.Enabled);
                }
                else
                {
                    OnBarcodeProcess?.Invoke(code);
                }

                Barcode = "";
            }
            catch (Exception ex)
            {
                LPackClient.SendErrorReport(ex.Message);
            }
        }

        /// <summary>
        /// Потеря активности окна приложения.
        /// Переключаем статус контролла в Заблокирован.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Deactivated(object sender, EventArgs e)
        {
            SetScanningStatus(ScannerInputControl.ScannerInputStatusRef.Disabled);
        }

        /// <summary>
        /// Установка активности окна приложения.
        /// Переключаем статус контролла в Готов к работе, если Таб, владеющий этим контроллом, является активным
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Activated(object sender, EventArgs e)
        {
            if (TabOwner.ControlName == Central.WM.TabSelected1
                || TabOwner.GetFrameName() == Central.WM.TabSelected1)
            {
                SetScanningStatus(ScannerInputControl.ScannerInputStatusRef.Enabled);
            }
        }
    }
}
