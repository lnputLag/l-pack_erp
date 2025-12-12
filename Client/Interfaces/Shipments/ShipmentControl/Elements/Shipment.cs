using Client.Common;
using Client.Interfaces.Main;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// библиотека функций типовых операций с отгрузкой
    /// </summary>
    /// <author>balchugov_dv</author>
    public class Shipment
    {
        public Shipment(int id=0)
        {
            Id=id;
        }

        public int Id { get; set; }
        private const string LoadingSchemeFolder = @"\\l-pack\net\Отделы\13 - СКЛАДЫ\Склад готовой продукции\Схемы загрузки\";

        /// <summary>
        /// показать схему погрузки ТС
        /// </summary>
        /// <param name="row"></param>
        public async void ShowLoadingScheme(Dictionary<string,string> row)
        {
            /*
                ID
                LOADINGSCHEMESTATUS
                LOADINGSCHEMEFILE
                PACKAGINGTYPETEXT
                PRODUCTION_TYPE_ID
                NOORDER

                PRODUCTION_TYPE_ID - тип продукции 
                    1-изделия с упаковкой
                    2-изделия без упаковки
                    3-бумага
                                       
                LOADINGSCHEMESTATUS - Статус схемы загрузки
                    0-разрешена
                    1-запрещена
                    2-грузить по схеме в файле
             */

            bool resume = false;

            var shipmentId = 0;
            var shemeStatusId=0;
            if (row != null)
            {
                shipmentId = row.CheckGet("ID").ToInt();
                if (shipmentId!=0)
                {
                    resume = true;       
                }

                /*
                    LOADINGSCHEMESTATUS - Статус схемы загрузки
                        0-разрешена
                        1-запрещена
                        2-грузить по схеме в файле
                 */
                shemeStatusId=row.CheckGet("LOADINGSCHEMESTATUS").ToInt();
            }

            if(resume)
            {
                var noOrder = row.CheckGet("NOORDER").ToInt();
                if(noOrder==1)
                {
                    var msg="";
                    msg=$"{msg}Отгрузка запрещена менеджером.";

                    var d = new DialogWindow($"{msg}", "Схема погрузки ТС", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                    resume = false;
                }
            }

            if(resume)
            {
                var typeId = row.CheckGet("PRODUCTION_TYPE_ID").ToInt();
                if(typeId==3)
                {
                    var msg="";
                    msg=$"{msg}Для данной отгрузки нет схемы, т.к. отгрузка содержит бумагу в рулонах.";

                    var d = new DialogWindow($"{msg}", "Схема погрузки ТС", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                    resume = false;
                }
            }

            if(resume)
            {
                if(shemeStatusId==1)
                {
                    var msg="";
                    msg=$"{msg}Автоматическая схема погрузки для данного транспортного средства запрещена менеджером.";

                    var d = new DialogWindow($"{msg}", "Схема погрузки ТС", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                    resume = false;
                }
            }

            

            if (resume)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                
                var p=new Dictionary<string, string>();
                {
                    p.CheckAdd("ID",shipmentId.ToString());
                    p.CheckAdd("DEMO", "0");
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module","Shipments");
                q.Request.SetParam("Object", "Loading");
                q.Request.SetParam("Action", "GetMap");
                //q.Request.SetParam("Object", "LoadingTwo");
                //q.Request.SetParam("Action", "GetOldMapTwo");

                q.Request.Timeout = 15000;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;
                //q.Request.Timeout = 300000;
                // q.Request.Attempts = 1;

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if(q.Answer.Status == 0)                
                {
                    Central.OpenFile(q.Answer.DownloadFilePath);
                }
                else
                {
                    q.ProcessError();
                }
                
                Mouse.OverrideCursor = null;
            }
        }
    }    
}
