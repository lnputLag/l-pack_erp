using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
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
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Test
{
    
    /// <summary>
    /// тестовый интерфейс для отладки функции диалогов
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2022-11-14</released>
    /// <changed>2022-11-14</changed>
    public partial class DialogTest:UserControl
    {
        public DialogTest()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);
            
            //Grid1Init();
            //Grid2Init();
            ToolbarFormInit();
            ToolbarFormSetDefaults();
        }


        /// <summary>
        /// форма с полями для фильтрации данных
        /// </summary>
        public FormHelper ToolbarForm { get; set; }

        #region Common

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
           
        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Test",
                ReceiverName = "",
                SenderName = "InitToolbarForm",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            //Grid.Destruct();
        }

          /// <summary>
        /// обработка ввода с клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e=Central.WM.KeyboardEventsArgs;
            switch(e.Key)
            {
                case Key.F5:
                    e.Handled=true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled=true;
                    break;
            }
        }
       
        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp");
        }

        #endregion

        #region ToolbarForm

        public void ToolbarFormInit()
        {
            //инициализация формы
            {
                ToolbarForm = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="SEARCH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=Search,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                };
                ToolbarForm.SetFields(fields);
            }
        }

        public void ToolbarFormSetDefaults()
        {
            ToolbarForm.SetDefaults();
        }

        #endregion


        private void SelectFolder()
        {
            var fileName="Сохранить в этой папке";
            var filePath="";
            var fd = new SaveFileDialog();
            fd.Filter = "Directory | directory";
            fd.FileName=fileName;
            var fdResult = fd.ShowDialog();
            fileName=fd.FileName;
            filePath= Path.GetDirectoryName(fd.FileName);

            LogMsg($"result=[{fdResult}] fileName=[{fileName}] filePath=[{filePath}]");
        }

        private void LogMsg(string text)
        {
            var today=DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            var t=Log.Text;
            t=$"{t}\n";
            t=$"{t}{today} {text}";
            Log.Text=t;
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void RefreshButton_Click(object sender,RoutedEventArgs e)
        {
        }

        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            SelectFolder();
        }
    }
}
