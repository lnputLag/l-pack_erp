using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// интерфейс "Регистрация водителей на проходных БДМ1 и БДМ2"
    /// новый алгоритм регистрации
    /// </summary>
    /// <author>Грешных Н.</author>
    /// <changed>2023-09-27</changed>
    /// <changed>2025-05-22</changed>/// 
    public class DriverRegistrationInterface
    {
        public DriverRegistrationInterface(int machineNumber=0)
        {
            var w=new Wizard();
            w.ReturnHomeTimeout = 10000;

            //массив с данными для регистрации
            {
                // 716=БДМ1, 1716=БДМ2
                w.Values.CheckAdd("MACHINE_ID", "1716");
                w.Values.CheckAdd("MACHINE_NUMBER", "2");
            }

            ResetValues(w);

            {
                switch(machineNumber)
                {
                    case 1:
                        w.Values.CheckAdd("MACHINE_ID", "716");
                        w.Values.CheckAdd("MACHINE_NUMBER", "1");
                        break;

                    case 2:
                        w.Values.CheckAdd("MACHINE_ID", "1716");
                        w.Values.CheckAdd("MACHINE_NUMBER", "2");
                        break;
                }
            }

            //фреймы
            {
                var frame=new DriverList();
                frame.Wizard=w;
                w.AddFrame(frame);
            }

            {
                var frame=new CargoType();
                frame.Wizard=w;
                w.AddFrame(frame);
            }

            {
                var frame=new PhoneNumber();
                frame.Wizard=w;
                w.AddFrame(frame);
            }

            {
                var frame=new Vendor();
                frame.Wizard=w;
                w.AddFrame(frame);
            }

            {
                var frame=new CarModel();
                frame.Wizard=w;
                w.AddFrame(frame);
            }

            {
                var frame=new CarNumber();
                frame.Wizard=w;
                w.AddFrame(frame);
            }

            {
                var frame=new CheckInfo();
                frame.Wizard=w;
                w.AddFrame(frame);
            }

            {
                var frame=new ConfirmSms();
                frame.Wizard=w;
                w.AddFrame(frame);
            }

            {
                var frame=new Info();
                frame.Wizard=w;
                w.AddFrame(frame);
            }

            {
                var frame=new Complete();
                frame.Wizard=w;
                w.AddFrame(frame);
            }

            {
                var frame = new ConfirmCall();
                frame.Wizard = w;
                w.AddFrame(frame);
            }

            // Ввод ФИО водителя
            {
                var frame = new FioEdit();
                frame.Wizard = w;
                w.AddFrame(frame);
            }

            //  Ввод номера машины (полностью) и прицепа
            {
                var frame = new TruckNumber();
                frame.Wizard = w;
                w.AddFrame(frame);
            }

            // проверка данных (ФИО, номер машины и прицепа, телефона)
            {
                var frame = new CheckInfo2();
                frame.Wizard = w;
                w.AddFrame(frame);
            }

            {
                var frame = new ConfirmSms2();
                frame.Wizard = w;
                w.AddFrame(frame);
            }
            
            // ввод водителем кода бронирования тайм-слота
            {
                var frame = new BookingCode();
                frame.Wizard = w;
                w.AddFrame(frame);
            }

            // вывод сообщения об ошибке проверки кода бронирования
            {
                var frame = new ErrorMessageOut();
                frame.Wizard = w;
                w.AddFrame(frame);
            }

            // вывод данных для проверки перед получением СМС
            {
                var frame = new InformationOutput();
                frame.Wizard = w;
                w.AddFrame(frame);
            }

            {
                var frame = new ConfirmSms3();
                frame.Wizard = w;
                w.AddFrame(frame);
            }

            // выбор машины по коду брони (если rmbu_id > 0)
            {
                var frame = new CarModel4();
                frame.Wizard = w;
                w.AddFrame(frame);
            }

            // ввод телефона по коду брони (если rmbu_id > 0)
            {
                var frame = new PhoneNumber4();
                frame.Wizard = w;
                w.AddFrame(frame);
            }
            
            // ввод смс телефона по коду брони (если rmbu_id > 0)
            {
                var frame = new ConfirmSms4();
                frame.Wizard = w;
                w.AddFrame(frame);
            }
            // вывод сообщения время слота просрочено
            {
                var frame = new ErrorMessageOut2();
                frame.Wizard = w;
                w.AddFrame(frame);
            }


            w.Run();
        }
      
        /// <summary>
        /// сброс значений
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="itemId"></param>
        public static void ResetValues(Wizard w)
        {
            w.Values.CheckAdd("CAR_NUMBER", "");    
            w.Values.CheckAdd("CAR_MODEL_ID", "");                    
            w.Values.CheckAdd("CAR_MODEL_DESCRIPTION", "");                    
            w.Values.CheckAdd("PHONE_NUMBER", "8");

            w.Values.CheckAdd("CARGO_TYPE", "0");
            w.Values.CheckAdd("CARGO_TYPE_DESCRIPTION", "");
            w.Values.CheckAdd("VENDOR_ID", "0");
            w.Values.CheckAdd("VENDOR_NAME", "0");

            w.Values.CheckAdd("ITEM_ID", "0");
            
            w.Values.CheckAdd("_CODE_SMS", "");
            w.Values.CheckAdd("_CODE_CALL", "");

            // Фамилия
            w.Values.CheckAdd("SURNAME", "");
            // Имя
            w.Values.CheckAdd("NAME", "");
            // Отчество
            w.Values.CheckAdd("MIDDLE_NAME", "");
            // номер сотового +7
            w.Values.CheckAdd("PHONE", "+7");
            // номер машины полный
            w.Values.CheckAdd("TRUCK_NUMBER", "");
            // номер прицепа полный
            w.Values.CheckAdd("TRAILER_NUMBER", "");
            
            // код брони тайм слота
            w.Values.CheckAdd("BOOKING_CODE", "");
            // текст сообщения об ошибке поиска кода брони
            w.Values.CheckAdd("ERROR_INFO", "");
            // Фамилия
            w.Values.CheckAdd("FIO", "");
            // номер сотового 
            w.Values.CheckAdd("PHONE3", "");
            // марка машины
            w.Values.CheckAdd("MARKA_CAR", "");
            // номер машины
            w.Values.CheckAdd("NUMBER_CAR", "");
            // дата бронирования слота
            w.Values.CheckAdd("DTTM_SLOT", "");
            // wmts_id слота
            w.Values.CheckAdd("WMTS_ID", "");
            // id_ts машины
            w.Values.CheckAdd("ID_TS", "");
            // rmbu_id машины
            w.Values.CheckAdd("RMBU_ID", "");
            // Id_a машины
            w.Values.CheckAdd("ID_A", "");
            // Id_d водителя
            w.Values.CheckAdd("ID_D", "");
            // Признак перерегистрации водителя на новое время выгрузки
            w.Values.CheckAdd("REGISTRATION", "");


        }

        /// <summary>
        /// диалог просмотра и печати ярлыка
        /// 1=просмотр, 2=печать
        /// </summary>
        public static bool ProcessLabel(int mode=0,int itemId=0)
        {
            bool resume = true;
            bool result = false;

            var labelViewer = new LabelViewer();

            var label = new WastePaperLabel();
            label.IdScrap=itemId;
            labelViewer.LogMsg($"itemId=[{itemId}]");

            var makeResult = label.Make();
            if (makeResult)
            {
                labelViewer.ReceiptDocument = label.Document;
                result = labelViewer.Init();
                result = true;
            }
            else
            {
                labelViewer.LogMsg("Ошибка. Ярлык не сформирован.");
                
                resume = false;
            }

            switch (mode)
            {
                //просмотр
                default:
                case 1:
                    labelViewer.Show();
                    break;

                //печать
                case 2:
                    labelViewer.Print(true);
                    break;
            }

            return result;
        }
    }
}


