using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Messages
{
    /// <summary>
    /// </summary>
    public partial class NotificationView : UserControl
    {
        public NotificationView()
        {
            InitializeComponent();
            
            Id=0;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            //регистрация обработчика клавиатуры
            PreviewKeyDown+=OnKeyDown;

            
            Form=new FormHelper();
            //список колонок формы
            var fields=new List<FormHelperField>()
            {
                
                new FormHelperField()
                { 
                    Path="CODE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Code,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                    },
                },
                new FormHelperField()
                { 
                    Path="CLASS",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Class,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                    },
                },
                new FormHelperField()
                { 
                    Path="TYPE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Type,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="TITLE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Title,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                        { FormHelperField.FieldFilterRef.Required, null },                    
                    },
                },
                new FormHelperField()
                { 
                    Path="CONTENT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Content,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                    },
                },
                new FormHelperField()
                { 
                    Path="LINK",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Link,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                    },
                },
                new FormHelperField()
                { 
                    Path="ROLES",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Roles,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                    },
                },
                new FormHelperField()
                { 
                    Path="USERS",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Users,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                    },
                },

            };

            Form.SetFields(fields);
            Form.OnValidate=(bool valid,string message) =>
            {
                if(valid)
                {
                    SaveButton.IsEnabled=true;
                    FormStatus.Text="";
                }
                else
                {
                    SaveButton.IsEnabled=false;
                    FormStatus.Text="Не все поля заполнены верно";
                }
            };

            SetDefaults();

        }

        public void SetDefaults()
        {
            Code.Text="";
            Title.Text="";
            Content.Text="";
            Roles.Text="";
            Users.Text="";
            
            {
                var list = new Dictionary<string, string>();
                list.Add("1", "(1) Побудительное сообщение");
                list.Add("2", "(2) Информационное сообщение");                
                list.Add("9", "(9) Системное");                
                Type.Items = list;
                Type.SelectedItem = list.FirstOrDefault((x) => x.Key == "2");
            }

            /*
            TestButton.Visibility=System.Windows.Visibility.Collapsed;
            Test2Button.Visibility=System.Windows.Visibility.Collapsed;
            if(Central.DebugMode)
            {
                TestButton.Visibility=System.Windows.Visibility.Visible;
                Test2Button.Visibility=System.Windows.Visibility.Visible;                
            } 
            */
        }
        
        public FormHelper Form { get;set;}

        public void Destroy()
        {
            Messenger.Default.Unregister<ItemMessage>(this);
        }


        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
        }
        
        private void OnKeyDown(object sender,System.Windows.Input.KeyEventArgs e)
        {
            Central.Dbg($"TestUserView.OnKeyDown KEY:{e.Key.ToString()}");
            switch (e.Key)
            {
                case Key.Escape:
                    Hide();
                    e.Handled=true;
                    break;
                      
                case Key.Enter:
                    if(Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        Save();
                        e.Handled=true;
                    }
                    break;

                case Key.F1:
                    Central.ShowHelp("/doc/l-pack-erp/production/production_tasks/list");
                    e.Handled=true;
                    break;
                
            }
        }


        public void Create()
        {
            Show();
        }

        public void Edit( int id )
        {
           GetData(id);
        }

        public void Show()
        {
            string title=$"Сообщение {Id}";
            if(Id == 0)
            {
                title="Новое сообщение";
                SaveButton.Visibility=System.Windows.Visibility.Visible;
            }
            else
            {
                SaveButton.Visibility=System.Windows.Visibility.Collapsed;
            }
            
            var key=$"Messages_Email_{Id}";
            Central.WM.AddTab(key, title, true, "add", this);

            Title.Focus();
        }

        public void Hide()
        {
            var key=$"Messages_Email_{Id}";
            Central.WM.RemoveTab(key);
        }

        public RowDataSet UserData { get; set; }
        public int Id { get;set;}

        public async void GetData(int id)
        {
            var p=new Dictionary<string,string> 
            {
                { "ID",id.ToString()},
            };

            await Task.Run(()=>{ 
                UserData=_LPackClientDataProvider.DoQueryDeserialize<RowDataSet>("Messages","Email","Get","Items",p);                                
            });

            if(UserData != null)
            {
                UserData.Init();

                if(UserData.Values.Count>0)
                {
                    Id=UserData.getValue("ID").ToInt();
                    Form.SetValues(UserData);
                    Show();
                }
            }
            
        }

        public async void SaveData(Dictionary<string,string> p)
        {
            var result="";
            var resultData=new Dictionary<string,string>();
            
            await Task.Run(()=>{ 
                result=_LPackClientDataProvider.DoQueryRawResult("Messages","Notification","Save","Items",p);                                
            });

            if(!string.IsNullOrEmpty(result))
            {
                resultData=JsonConvert.DeserializeObject<Dictionary<string,string>>(result);
                if(resultData.Count > 0)
                {
                    if(resultData.ContainsKey("ID"))
                    {
                        if(resultData["ID"].ToInt() != 0)
                        {
                            
                            //отправляем сообщение о необходимости обновить данные
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "Messages",
                                SenderName = "NotificationView",
                                Action = "Refresh",
                            });

                            var msg=$"Уведомление отправлено успешно";
                            var d = new DialogWindow($"{msg}", "Отправка уведомления", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }

                }
            }
        }

        public void Save()
        {
            if(Form.Validate())
            {
                var resume=true;
                var p=Form.GetValues();

                if(resume)
                {
                    SaveButton.IsEnabled=true;
                    FormStatus.Text="";

                    if(
                        string.IsNullOrEmpty(p["USERS"]) 
                        && string.IsNullOrEmpty(p["ROLES"]) 
                    )
                    {
                        SaveButton.IsEnabled=false;
                        FormStatus.Text="Необходимо указать роли или пользователей-получателей.";
                        resume=false;
                    }
                }                

                if(resume)
                {
                    
                    SaveData(p);
                    

                    /*
                     
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "Main",
                        ReceiverName = "MainWindow",
                        SenderName = "Navigator",
                        Action = "ChangeServer",
                        Message = "",
                    });

                    SaveData(p);
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "Main",
                        ReceiverName = "MainWindow",
                        SenderName = "Navigator",
                        Action = "ChangeServer",
                        Message = "",
                    });

                    SaveData(p);
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "Main",
                        ReceiverName = "MainWindow",
                        SenderName = "Navigator",
                        Action = "ChangeServer",
                        Message = "",
                    });
                    */

                }                
            }
        }

        private void CancelButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Hide();
        }

        private void SaveButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Save();
        }

        private void TestButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Code.Text="";
            Title.Text="Test notification";
            Content.Text="Curabitur sed aliquam turpis. Sed facilisis viverra nisi, sed gravida mauris. Etiam iaculis molestie accumsan. Proin eu tempus est. Fusce";
            Link.Text="l-pack://l-pack_erp/shipments/edm";
            Roles.Text="[f]admin";
            Users.Text="193";

            {
                var list = new Dictionary<string, string>();
                list.Add("1", "(1) Побудительное сообщение");
                list.Add("2", "(2) Информационное сообщение");                
                //Type.Items = list;
                Type.SelectedItem = list.FirstOrDefault((x) => x.Key == "2");
            }
            
        }

        private void Test2Button_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Code.Text="";
            Title.Text="Сообщения по образцам";
            
            var t=$"";
            t=$"{t}Константинова И.Н. Оскар Асеев Фиолент";
            t=$"{t}\nКонстантинова И.Н. Оскар Олейников ава";
            t=$"{t}\nПоволоцкая Л.А. КОМПАНИЯ ХАРД ООО ДП";
            t=$"{t}\nПоволоцкая Л.А. КОМПАНИЯ ХАРД ООО ";
            t=$"{t}\nРубахина О.В. Брава ПО ";
            t=$"{t}\nРубахина О.В. ЦБК 2562";
            t=$"{t}\nРубахина О.В. Брава ПО ";
            t=$"{t}\nРубахина О.В. ЦБК 2564";
            t=$"{t}\nРубахина О.В. ЦБК 2561";
            t=$"{t}\nРубахина О.В. ЦБК 2565";
            t=$"{t}\nРубахина О.В. ЦБК 2566";
            t=$"{t}\nРубахина О.В. ЦБК 2559";
            t=$"{t}\nРубахина О.В. ЦБК 2560";
            t=$"{t}\nРубахина О.В. Брава ПО ";
            t=$"{t}\nРубахина О.В. ЦБК 2563";
            t=$"{t}\nЯкунина Т.Н. ФАКЕЛ-БК ООО ";
            t=$"{t}\nЯкунина Т.Н. КАРТА ООО ПКО ";
            t=$"{t}\nЯкунина Т.Н. ФАКЕЛ-БК ООО ";
            t=$"{t}\nЯкунина Т.Н. КАРТА ООО ПКО Екатерина";
            t=$"{t}\nЯкунина Т.Н. ФАКЕЛ-БК ООО ";
            Content.Text=t;
            
            Link.Text="l-pack://l-pack_erp/shipments/edm";
            Roles.Text="[f]admin";
            Users.Text="193";

            {
                var list = new Dictionary<string, string>();
                list.Add("1", "(1) Побудительное сообщение");
                list.Add("2", "(2) Информационное сообщение");                
                //Type.Items = list;
                Type.SelectedItem = list.FirstOrDefault((x) => x.Key == "2");
            }
        }

        private void Test3Button_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Code.Text="";
            Title.Text="Новые веб-образцы";
            
            var t=$"";
            t=$"{t}Аверьянова Е.С. ИП Стародубов Алексей Ген  07.06 ";
            t=$"{t}\nКонстантинова И.Н. Оскар  26.03 ";
            t=$"{t}\nКонстантинова И.Н. ГОФРОПАК язев ТАМБОВ 10.11 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Олейников ава 14.03 ";
            t=$"{t}\nКонстантинова И.Н. Оскар олейников самсун 12.11 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Постников Валд 03.03 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Лапшин Кубань 14.01 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Гочачко 09.03 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Некипелов КК 12.02 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Савченко Юг 550 07.12 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Олейников АВА 03.02 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Олейников моспак 06.11 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Савченко Юг 650 07.12 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Захаров БонМеб 23.12 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Гочачко 09.03 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Гочачко СОФ 09.03 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Савченко Юг 650+ 07.12 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Постников Валд 05.03 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Савченко Юг 450 07.12 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Савченко Юг 550+ 07.12 ";
            t=$"{t}\nКонстантинова И.Н. Оскар язев БРЯНСК МИР 05.03 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Сорокин Unicar2 30.12 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Якименко 18.01 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Лапшин МКИ 03.12 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Постников утконо 24.11 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Лапшин Смарт 14.03 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Олейников моспак 06.11 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Некипелов КК 01.03 ";
            t=$"{t}\nКонстантинова И.Н. Оскар язев УЛЬЯНОВСК 04.03 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Гочачко СОФ 09.03 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Маслов ИЗТТ 06.11 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Савченко Брел пр 09.12 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Репин ТЭРА 20.02 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Асеев ЭкоХлеб 14.04 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Лесная Декорика 24.05 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Лесная Декорика 24.05 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Лесная Декорика 24.05 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Гочачко ФГ 17.05 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Лесная Декорика 24.05 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Лесная Декорика 24.05 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Олейников ава 01.07 (-)";
            t=$"{t}\nКонстантинова И.Н. Оскар Некипелов КК 06.04 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Постников Копак 06.04 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Лесная Формат 07.04 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Колесников Варни 26.06 ";
            t=$"{t}\nКонстантинова И.Н. Оскар Маслов КЗГА 02.06 ";
            t=$"{t}\nКонстантинова И.Н. ГОФРОПАК язев ТАМБОВ 10.11 ";
            t=$"{t}\nКонстантинова И.Н. Оскар  26.03 ";
            t=$"{t}\nМещерякова Е.С. ФАБРИКА УПАКОВКИ Марияя 01.03 ";
            t=$"{t}\nМещерякова Е.С. ФАБРИКА УПАКОВКИ тхк 18.04 ";
            t=$"{t}\nМещерякова Е.С. ФАБРИКА УПАКОВКИ Скопин Фарм 11.03 ";
            t=$"{t}\nМещерякова Е.С. ФАБРИКА УПАКОВКИ сыродел 08.06 ";
            t=$"{t}\nМещерякова О.С. Объединение МАККАРТ  20.03 ";
            t=$"{t}\nМещерякова О.С. Объединение МАККАРТ  04.12 ";
            t=$"{t}\nМещерякова О.С. Объединение МАККАРТ  08.12 ";
            t=$"{t}\nПоваляева А.А. УПАКСНАБ ООО Октобин 04.02 ";
            t=$"{t}\nПоваляева А.А. АЙ ПАК НН ООО Андрей  10.04 ";
            t=$"{t}\nПоваляева А.А. АЙ ПАК НН ООО А3 14.04 ";
            t=$"{t}\nПоваляева А.А. АЙ ПАК НН ООО Ан1 12.04 ";
            t=$"{t}\nПоваляева А.А. АЙ ПАК НН ООО 820х790 Дмитрий 02.03 ";
            t=$"{t}\nПоваляева А.А. АЙ ПАК НН ООО 1200х480 Дмитрий 11.11 ";
            t=$"{t}\nПоваляева А.А. УПАКСНАБ ООО упаковка 21.03 ";
            t=$"{t}\nПоваляева А.А. ГОФРИКА ООО Динар под кабель 18.12 ";
            t=$"{t}\nПоваляева А.А. ИП Зельманович А.В.  17.05 ";
            t=$"{t}\nПоваляева А.А. ИП Зельманович А.В.  11.12 ";
            t=$"{t}\nПоваляева А.А. ИП Зельманович А.В.  21.12 ";
            t=$"{t}\nПоваляева А.А. ИП Зельманович А.В.  21.12 ";
            t=$"{t}\nПоваляева А.А. АЙ ПАК НН ООО СергейИнком2.2 01.03 ";
            t=$"{t}\nПоволоцкая Л.А. КНК ГРУПП Николаев 17.05 ";
            t=$"{t}\nПоволоцкая Л.А. КНК ГРУПП Николаев 1 17.05 ";
            t=$"{t}\nПоволоцкая Л.А. КНК ГРУПП Подольск Рудь 01.06 ";
            t=$"{t}\nПоволоцкая Л.А. КНК ГРУПП Павел 2 фейса  27.05 ";
            t=$"{t}\nПоволоцкая Л.А. КОМПАНИЯ ХАРД ООО ДП 14.11 ";
            t=$"{t}\nПоволоцкая Л.А. КОМПАНИЯ ХАРД ООО мм 31.01 ";
            t=$"{t}\nПоволоцкая Л.А. СЗ-ГОФРА ООО  22.03 ";
            t=$"{t}\nПоволоцкая Л.А. КМС ГРУПП ООО 2104_Кристалл 26.04 ";
            t=$"{t}\nПоволоцкая Л.А. ГОФРОЛИНА ООО Санкт-Петербург 08.06 ";
            t=$"{t}\nПоволоцкая Л.А. ЩЕРБИНКА ОТИС ЛИФТ ЗАО  30.06 ";
            t=$"{t}\nПоволоцкая Л.А. КНК ГРУПП Рудь 2 01.06 ";
            t=$"{t}\nПоволоцкая Л.А. КНК ГРУПП Павел 2 фейса  2 27.05 ";
            t=$"{t}\nПоволоцкая Л.А. КНК ГРУПП Талмит дно 13.02 ";
            t=$"{t}\nПоволоцкая Л.А. КНК ГРУПП Талмит дно 13.02 ";
            t=$"{t}\nПоволоцкая Л.А. КНК ГРУПП Империя дно 13.02 ";
            t=$"{t}\nПоволоцкая Л.А. КНК ГРУПП Салат 02.03 ";
            t=$"{t}\nПоволоцкая Л.А. КНК ГРУПП Пиво 20 банок 24.11 ";
            t=$"{t}\nПоволоцкая Л.А. Бэст упаковка 2-800 15.03 ";
            t=$"{t}\nПоволоцкая Л.А. Бэст упаковка 1.1 20.01 ";
            t=$"{t}\nПоволоцкая Л.А. Бэст упаковка 2-1000 15.03 ";
            t=$"{t}\nПоволоцкая Л.А. Бэст упаковка  20.01 ";
            t=$"{t}\nПоволоцкая Л.А. Бэст упаковка 2-500 15.03 ";
            t=$"{t}\nПоволоцкая Л.А. ПАРЛАМЕНТ ПРОДАКШН ООО  22.06 ";
            t=$"{t}\nПоволоцкая Л.А. Упак-Трейдинг  02.06 ";
            t=$"{t}\nПоволоцкий С.А. Зеленая территория  28.01 ";
            t=$"{t}\nПоволоцкий С.А. Зеленая территория  28.01 ";
            t=$"{t}\nПоволоцкий С.А. Зеленая территория СТ АВ 05.04 ";
            t=$"{t}\nПоволоцкий С.А. Зеленая территория НЛ ск ц 17.03 ";
            t=$"{t}\nПоволоцкий С.А. Зеленая территория НЛ ск с 17.03 ";
            t=$"{t}\nПоволоцкий С.А. Зеленая территория НЛ КнБр_Л 09.04 ";
            t=$"{t}\nПоволоцкий С.А. ИРИЙ ООО 52 19.11 ";
            t=$"{t}\nПоволоцкий С.А. ИМПЭКС ООО  06.03 ";
            t=$"{t}\nПоволоцкий С.А. ИМПЭКС ООО  06.03 ";
            t=$"{t}\nПоволоцкий С.А. ИМПЭКС ООО  06.03 ";
            t=$"{t}\nПоволоцкий С.А. Терра Принт  02.05 ";
            t=$"{t}\nПоволоцкий С.А. Верность качеству Конд.фа  06.06 ";
            t=$"{t}\nПоволоцкий С.А. РПС АО Медь 0,7х6 25.12 ";
            t=$"{t}\nПоволоцкий С.А. РПС АО Сова 0,7х6 25.12 ";
            t=$"{t}\nПоволоцкий С.А. ВИТРА САНТЕХНИКА ООО 312748 28.04 ";
            t=$"{t}\nПоволоцкий С.А. КАМАГОФРОПАК ООО Колосницын  08.02 ";
            t=$"{t}\nПоволоцкий С.А. КАМАГОФРОПАК ООО Колосницын  16.11 ";
            t=$"{t}\nПоволоцкий С.А. КАМАГОФРОПАК ООО Колосницын DS 30.11 ";
            t=$"{t}\nПоволоцкий С.А. КАМАГОФРОПАК ООО Румпа ИП 26.03 ";
            t=$"{t}\nПоволоцкий С.А. КАМАГОФРОПАК ООО Ершова 10.06 ";
            t=$"{t}\nПоволоцкий С.А. ПАКПРЕСТУС ООО  25.02 ";
            t=$"{t}\nПоволоцкий С.А. ТИМПАК ПРО ООО 207 18.02 ";
            t=$"{t}\nПоволоцкий С.А. ТИМПАК ПРО ООО 101 05.11 ";
            t=$"{t}\nПоволоцкий С.А. Меридиан Лоток на 6 банок 23.03 ";
            t=$"{t}\nПоволоцкий С.А. ЛБК МАРКЕТИНГ ПРО ООО 4204 Октабин  22.06 ";
            t=$"{t}\nПоволоцкий С.А. Зеленая территория  28.12 ";
            t=$"{t}\nПоволоцкий С.А. Зеленая территория АО М 17.02 ";
            t=$"{t}\nПоволоцкий С.А. Зеленая территория БТ С 09.03 ";
            t=$"{t}\nПоволоцкий С.А. Зеленая территория НЛ ск 10.03 ";
            t=$"{t}\nПоволоцкий С.А. Зеленая территория  25.01 ";
            t=$"{t}\nПоволоцкий С.А. Зеленая территория АО М кр 17.02 ";
            t=$"{t}\nПоволоцкий С.А. Зеленая территория БТ С 11.03 ";
            t=$"{t}\nПоволоцкий С.А. Зеленая территория  11.03 ";
            t=$"{t}\nПоволоцкий С.А. Зеленая территория  17.01 ";
            t=$"{t}\nПоволоцкий С.А. Зеленая территория  17.01 ";
            t=$"{t}\nПоволоцкий С.А. Зеленая территория  23.11 ";
            t=$"{t}\nПоволоцкий С.А. Зеленая территория БТ С 10.03 ";
            t=$"{t}\nПоволоцкий С.А. Зеленая территория  22.01 ";
            t=$"{t}\nРубахина О.В. ЦБК 2417 30.03 ";
            t=$"{t}\nРубахина О.В. Фабрика Упаковки ПТП  09.05 ";
            t=$"{t}\nРубахина О.В. ТАРА77 2295 24.01 ";
            t=$"{t}\nРубахина О.В. ТАРА77 2166 12.11 ";
            t=$"{t}\nРубахина О.В. ТАРА77 2167 12.11 ";
            t=$"{t}\nРубахина О.В. ГОФРОЕВРОПАК ООО  21.01 ";
            t=$"{t}\nРубахина О.В. Брава ПО  15.12 ";
            t=$"{t}\nРубахина О.В. Брава ПО  08.12 ";
            t=$"{t}\nРубахина О.В. Брава ПО  09.06 ";
            t=$"{t}\nРубахина О.В. Брава ПО  09.06 ";
            t=$"{t}\nРубахина О.В. ТарПром 2131 05.11 ";
            t=$"{t}\nРубахина О.В. ТарПром 2284 14.02 ";
            t=$"{t}\nРубахина О.В. ТарПром 2292 22.01 ";
            t=$"{t}\nРубахина О.В. ТарПром 2231 15.12 ";
            t=$"{t}\nРубахина О.В. ТарПром 2232 15.12 ";
            t=$"{t}\nРубахина О.В. ТарПром 2283 14.02 ";
            t=$"{t}\nРубахина О.В. ОТК Песочница 04.02 ";
            t=$"{t}\nРубахина О.В. ОТК 30032021 картина 03.04 ";
            t=$"{t}\nРубахина О.В. Смигора ТД 48 06.04 ";
            t=$"{t}\nЧижова А.В. БЕРРИ ООО 9150511052 08.12 ";
            t=$"{t}\nЯкунина Т.Н. Р-ПАК новый +7 24.12 ";
            t=$"{t}\nЯкунина Т.Н. Элекс+ АТ-2 13.01 ";
            t=$"{t}\nЯкунина Т.Н. Элекс+ АТ-1 13.01 ";
            t=$"{t}\nЯкунина Т.Н. Элекс+ АТ-3 13.01 ";
            t=$"{t}\nЯкунина Т.Н. Элекс+ АТ-4 13.01 ";
            t=$"{t}\nЯкунина Т.Н. ООО ГлобалТрейд Комп. 3Е дно 28.05 ";
            t=$"{t}\nЯкунина Т.Н. ООО ГлобалТрейд Комп. 2000 обеч1 28.05 ";
            t=$"{t}\nЯкунина Т.Н. СПЕЦОГНЕУПОР ООО  07.06 ";
            t=$"{t}\nЯкунина Т.Н. СПЕЦОГНЕУПОР ООО  07.06 ";
            t=$"{t}\nЯкунина Т.Н. СПЕЦОГНЕУПОР ООО  07.06 ";
            t=$"{t}\nЯкунина Т.Н. КЭС ГК  09.04 ";
            t=$"{t}\nЯкунина Т.Н. Элекс плюс эп-16-11 18.11 ";
            t=$"{t}\nЯкунина Т.Н. Элекс плюс юг1 07.11 ";
            t=$"{t}\nЯкунина Т.Н. Элекс плюс Персик 05.03 ";
            t=$"{t}\nЯкунина Т.Н. Элекс плюс эп-25-11 25.11 ";
            t=$"{t}\nЯкунина Т.Н. Элекс плюс Эден Джин 09.11 ";
            t=$"{t}\nЯкунина Т.Н. Элекс плюс эп-17-11 18.11 ";
            t=$"{t}\nЯкунина Т.Н. Элекс плюс Землянск 22.05 ";
            t=$"{t}\nЯкунина Т.Н. Элекс плюс Землянск 22.05 ";
            t=$"{t}\nЯкунина Т.Н. Элекс плюс уголок1 04.07 ";
            t=$"{t}\nЯкунина Т.Н. Элекс плюс КС 2203 27.03 ";
            t=$"{t}\nЯкунина Т.Н. Элекс плюс эп-15-03 18.03 ";
            t=$"{t}\nЯкунина Т.Н. Элекс плюс маска2 23.03 ";
            t=$"{t}\nЯкунина Т.Н. Элекс плюс Флоренция дно 20.05 ";
            t=$"{t}\nЯкунина Т.Н. Элекс плюс КС 1603 18.03 ";
            t=$"{t}\nЯкунина Т.Н. Элекс плюс зор лоток 24.05 ";
            t=$"{t}\nЯкунина Т.Н. Бумага И Упаковка ООО Перепечаева 21.12 ";
            t=$"{t}\nЯкунина Т.Н. Бумага И Упаковка ООО Перепечаева 03.02 ";
            t=$"{t}\nЯкунина Т.Н. Бумага И Упаковка ООО Перепечаева 23.02 ";
            t=$"{t}\nЯкунина Т.Н. Бумага И Упаковка ООО Перепечаева 03.02 ";
            t=$"{t}\nЯкунина Т.Н. Бумага И Упаковка ООО Лоток 04.06 ";
            t=$"{t}\nЯкунина Т.Н. Бумага И Упаковка ООО Мясной лоток 06.05 ";
            t=$"{t}\nЯкунина Т.Н. Бумага И Упаковка ООО Мясной лоток 06.05 ";
            t=$"{t}\nЯкунина Т.Н. ФАКЕЛ-БК ООО  11.02 ";
            t=$"{t}\nЯкунина Т.Н. ФАКЕЛ-БК ООО  17.03 ";
            t=$"{t}\nЯкунина Т.Н. ФАКЕЛ-БК ООО  26.05 ";
            t=$"{t}\nЯкунина Т.Н. ФАКЕЛ-БК ООО  26.05 ";
            t=$"{t}\nЯкунина Т.Н. ФАКЕЛ-БК ООО  17.03 ";
            t=$"{t}\nЯкунина Т.Н. ФАКЕЛ-БК ООО  17.03 ";
            t=$"{t}\nЯкунина Т.Н. ФАКЕЛ-БК ООО  17.03 ";
            t=$"{t}\nЯкунина Т.Н. ФАКЕЛ-БК ООО  17.03 ";
            t=$"{t}\nЯкунина Т.Н. ФАКЕЛ-БК ООО  22.05 ";
            t=$"{t}\nЯкунина Т.Н. Неомар  14.03 ";
            t=$"{t}\nЯкунина Т.Н. Неомар  03.12 ";
            t=$"{t}\nЯкунина Т.Н. Неомар  15.12 ";
            t=$"{t}\nЯкунина Т.Н. Неомар 29.01.2021 03.02 ";
            t=$"{t}\nЯкунина Т.Н. Неомар  14.03 ";
            t=$"{t}\nЯкунина Т.Н. Промкартон 931  11.06 ";
            t=$"{t}\nЯкунина Т.Н. МАСТЕР ПАК ООО СРОЧНЫЙ НП 12.12 ";
            t=$"{t}\nЯкунина Т.Н. МАСТЕР ПАК ООО СРОЧНЫЙ НП 12.12 ";
            t=$"{t}\nЯкунина Т.Н. МАСТЕР ПАК ООО 2НП бур  16.11 ";
            t=$"{t}\nЯкунина Т.Н. МАСТЕР ПАК ООО 1ПК СРОЧНО 28.06 ";
            t=$"{t}\nЯкунина Т.Н. Империя Упаковки ООО 152/1 24.12 ";
            t=$"{t}\nЯкунина Т.Н. Империя Упаковки ООО 135 04.12 ";
            t=$"{t}\nЯкунина Т.Н. Империя Упаковки ООО 66/1 24.05 ";
            t=$"{t}\nЯкунина Т.Н. КАРТА ООО ПКО  30.03 ";
            t=$"{t}\nЯкунина Т.Н. КАРТА ООО ПКО  30.03 ";
            t=$"{t}\nЯкунина Т.Н. КАРТА ООО ПКО Екатерина 29.06 (-)";
            t=$"{t}\nЯкунина Т.Н. КЭСПАК ООО  19.02 ";
            t=$"{t}\nЯчменева И.В. ГрандУпаК  10.04 ";
            t=$"{t}\nЯчменева И.В. ГрандУпаК  20.05 ";
            t=$"{t}\nЯчменева И.В. ИРБИС ООО 2. 19.04 ";
            t=$"{t}\nЯчменева И.В. ИРБИС ООО 1. 18.04 ";
            t=$"{t}\nЯчменева И.В. ИРБИС ООО 3. 17.04 ";
            t=$"{t}\nЯчменева И.В. ГрандУпаК  10.04 ";
            Content.Text=t;
            
            Link.Text="l-pack://l-pack_erp/shipments/edm";
            Roles.Text="[f]admin";
            Users.Text="193";

            {
                var list = new Dictionary<string, string>();
                list.Add("1", "(1) Побудительное сообщение");
                list.Add("2", "(2) Информационное сообщение");                
                //Type.Items = list;
                Type.SelectedItem = list.FirstOrDefault((x) => x.Key == "2");
            }
        }

        private void ShipmentsSamples_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Code.Text="";
            Title.Text="Отгрузите образцы";
            Class.Text="ship_samples";
            Content.Text="";
            Link.Text="l-pack://l-pack_erp/shipments/control/equipment/samples";
            Roles.Text="[f]admin";
            Users.Text="193";

            {
                var list = new Dictionary<string, string>();
                list.Add("1", "(1) Побудительное сообщение");
                list.Add("2", "(2) Информационное сообщение");                
                //Type.Items = list;
                Type.SelectedItem = list.FirstOrDefault((x) => x.Key == "1");
            }
        }

        private void ShipmentsClishe_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Code.Text="";
            Title.Text="Отгрузите клише";
            Class.Text="ship_cliche";
            Content.Text="";
            Link.Text="l-pack://l-pack_erp/shipments/control/equipment/clishe/for_loading";
            Roles.Text="[f]admin";
            Users.Text="193";

            {
                var list = new Dictionary<string, string>();
                list.Add("1", "(1) Побудительное сообщение");
                list.Add("2", "(2) Информационное сообщение");                
                //Type.Items = list;
                Type.SelectedItem = list.FirstOrDefault((x) => x.Key == "1");
            }
        }

        private void ShipmentsShtanzforms_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Code.Text="";
            Title.Text="Отгрузите штанцформы";
            Class.Text="ship_shtanzforms";
            Content.Text="";
            Link.Text="l-pack://l-pack_erp/shipments/control/equipment/shtanzforms/for_loading";
            Roles.Text="[f]admin";
            Users.Text="193";

            {
                var list = new Dictionary<string, string>();
                list.Add("1", "(1) Побудительное сообщение");
                list.Add("2", "(2) Информационное сообщение");                
                //Type.Items = list;
                Type.SelectedItem = list.FirstOrDefault((x) => x.Key == "1");
            }
        }

        private void UpdateButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            var ts=DateTime.Now.ToString("yyyy-MM-dd_HH:mm");
            Code.Text="";
            Title.Text="Щелкните здесь, чтобы обновить программу";
            Class.Text=$"upd_{ts}";
            Content.Text="Доступна новая версия программы";
            Link.Text="l-pack://l-pack_erp/documentation/update";
            Roles.Text="";
            Users.Text="193";

            {
                var list = new Dictionary<string, string>();
                list.Add("1", "(1) Побудительное сообщение");
                list.Add("2", "(2) Информационное сообщение");                
                //Type.Items = list;
                Type.SelectedItem = list.FirstOrDefault((x) => x.Key == "1");
            }
        }

        private void RestartButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            var ts=DateTime.Now.ToString("yyyy-MM-dd_HH:mm");
            Code.Text=$"res_{ts}";
            Class.Text=$"res_{ts}";
            Title.Text="restart";
            
            Content.Text="";
            Link.Text="";
            Roles.Text="";
            Users.Text="";

            {
                var list = new Dictionary<string, string>();
                list.Add("1", "(1) Побудительное сообщение");
                list.Add("2", "(2) Информационное сообщение");                
                list.Add("9", "(9) Системное");     
                //Type.Items = list;
                Type.SelectedItem = list.FirstOrDefault((x) => x.Key == "9");
            }
        }

        private void ReceiversVoidButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Users.Text="";
        }

        private void AddUsers(string u)
        {
            var t=Users.Text;
            if(!string.IsNullOrEmpty(t))
            {
                t=$"{t},";
            }
            t=$"{t}{u}";
            Users.Text=t;
        }

        private void ReceiversCMButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            AddUsers("9001,9002");
        }

        private void ReceiversCMReelsButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            AddUsers("9011,9012,9013,9014,9015,9021,9022,9023,9031,9032,9033,9034,9035");
        }

        private void ReceiversPpdButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            AddUsers("56,311,389,25");
        }

        private void StockUsers_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            AddUsers("211,369,212,209,368,213,210,370,301,124");
        }

        private void DoHopButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var ts=DateTime.Now.ToString("yyyy-MM-dd_HH:mm");
            Code.Text=$"res_{ts}";
            Class.Text=$"res_{ts}";
            Title.Text="do_hop";
            
            Content.Text="";
            Link.Text="";
            Roles.Text="";
            Users.Text="";

            {
                var list = new Dictionary<string, string>();
                list.Add("1", "(1) Побудительное сообщение");
                list.Add("2", "(2) Информационное сообщение");                
                list.Add("9", "(9) Системное");     
                //Type.Items = list;
                Type.SelectedItem = list.FirstOrDefault((x) => x.Key == "9");
            }
        }
    }
}
