using Client.Common;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Client.Interfaces.Production.CreatingTasks
{
    /// <summary>
    /// вспомогательная структура для хранения данных рилевок (одного ручья)
    /// используется генератором эскиза раскроя
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>    
    public class Crease
    {
        /// <summary>
        /// id подзадания
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// формат задания
        /// </summary>
        public int Format { get; set; }
        /// <summary>
        /// обрезь задания
        /// </summary>
        public int Trim { get; set; }
        /// <summary>
        /// обрезь задания, процент от формата
        /// </summary>
        public double TrimPercent { get; set; }
        /// <summary>
        /// ширина заготовки
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// количество потоков в ручье
        /// </summary>
        public int Threads { get; set; }
        /// <summary>
        /// симметричная рилевка
        /// </summary>
        public int CreaseSym { get; set; }
        /// <summary>
        /// список рилевок (25 записей: [0-24])
        /// </summary>
        public List<int> CreaseList { get; set; }
        /// <summary>
        /// цвет заливки ручья
        /// </summary>
        public string BackgroundColor { get; set; }
        /// <summary>
        /// ID позиции продукции (id_orderdates)
        /// </summary>
        public int PositionId { get; set; }

        /// <summary>
        /// конструктор
        /// </summary>
        public Crease()
        {
            Id=0;
            Width=0;
            Threads=0;
            CreaseSym=0;
            CreaseList=new List<int>();
            BackgroundColor="#ffffffff";
            PositionId=0;
        }

        /// <summary>
        /// инициализватор, получает на входе данные и разбирает их, записывая в свои внутренние структуры
        /// </summary>
        /// <param name="task"></param>
        /// <param name="crease"></param>
        public void Init( JObject task, int thread=1,  JObject crease=null )
        {
            if( thread == 1 )
            {
                Id           = task["Task1Id"].ToInt();            
                Width        = task["Task1Width"].ToInt();
                Threads      = task["Task1Threads"].ToInt();
                CreaseSym   = task["Task1Crease"].ToInt();

            }else if( thread == 2 )
            {
                Id           = task["Task2Id"].ToInt();            
                Width        = task["Task2Width"].ToInt();
                Threads      = task["Task2Threads"].ToInt();
                CreaseSym   = task["Task2Crease"].ToInt();

            }

            Format       = task["Format"].ToInt();            
            Trim         = task["Trim"].ToInt();      
            

            if( crease != null )
            {
                foreach( JProperty prop in crease.Children() )
                {
                    var k=prop.Name.ToString();
                    var v=prop.Value.ToString();

                    if( k.IndexOf("Сrease") > -1 )
                    {
                        if( !string.IsNullOrEmpty(v) )
                        {
                            var vx=int.Parse( v );
                             CreaseList.Add( vx );
                        }
                    }                    
                }                
            }
        }

    }
}
