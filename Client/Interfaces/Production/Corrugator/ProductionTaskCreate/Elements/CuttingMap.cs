using Client.Assets.HighLighters;
using Client.Common.Lib.Reporter;
using Client.Common.Reporter;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;

namespace Client.Interfaces.Production.CreatingTasks
{
    /// <summary>
    /// генератор изображения схемы автораскроя, работает в составе стека Reporter
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>        
    class CuttingMap : Gizmo
    {
        protected IPen BorderPen { get; set; }

        /// <summary>
        /// высота бокса по умолчанию
        /// </summary>
        public int BlockHeight { get; set; }

        /// <summary>
        /// ручей1
        /// </summary>
        public Crease Crease1 { get; set; }
        /// <summary>
        /// ручей2
        /// </summary>
        public Crease Crease2 { get; set; }

        /// <summary>
        /// ширина границы бокса
        /// </summary>
        public float BoxBorderWidth { get; set; }

        /// <summary>
        /// перо границы бокса
        /// </summary>
        protected IPen BoxBorderPen { get; set; }
        protected Rgba32 BoxBorderColor { get; set; }

        public float CreaseBorderWidth { get; set; }
        protected IPen CreaseBorderPen { get; set; }
        protected Rgba32 CreaseBorderColor { get; set; }

        /// <summary>
        /// ширина выносной линии
        /// </summary>
        public float LineWidth { get; set; }
        public float AxisLineWidth { get; set; }

        /// <summary>
        /// перо выносной линии
        /// </summary>
        protected IPen LinePen { get; set; }
        protected IPen AxisLinePen { get; set; }
        protected Rgba32 LineColor { get; set; }

        /// <summary>
        /// Стиль текста
        /// </summary>
        public FontStyle FontStyle { get; set; }
        /// <summary>
        /// Размер текста
        /// </summary>
        public int FontSize { get; set; }
        /// <summary>
        /// Цвет текста
        /// </summary>
        public Color TextColor { get; set; }

        /// <summary>
        /// длина выносной линии
        /// </summary>
        protected int ServiceLineLength { get; set; }

        protected DocumentOptions Options { get; set; }

        public bool Debug { get; set; }

        /// <summary>
        /// максимальная ширина полотна
        /// </summary>
        public int MaxFormatWidth { get; set;}



        /// <summary>
        /// Конструктор
        /// </summary>
        public CuttingMap()
        {
            Width = 100;
            Height = 100;
            Border = true;
            BorderWidth = 1;

            Crease1 = new Crease();
            Crease2 = new Crease();

            BlockHeight = 18;

            //бордер заготовки
            //BoxBorderWidth = 1.2f;
            BoxBorderWidth = 1.5f;
            BoxBorderColor = new Rgba32(0, 0, 0);

            //бордер рилевки
            //CreaseBorderWidth = 0.5f;
            CreaseBorderWidth = 0.5f;
            CreaseBorderColor = new Rgba32(0, 0, 0);

            Debug = false;

            MaxFormatWidth=2550;
        }

        public override void Init(DocumentOptions options)
        {
            Options = options;
            var o = Options;
        }

        /// <summary>
        /// Отрисовка
        /// </summary>
        /// <param name="options"></param>
        public override void Render(DocumentOptions options)
        {

            // ГОСТ 2.307-2011

            Options = options;
            var o = Options;


            //граница
            if (Border && BorderWidth > 0)
            {
                BorderPen = Pens.Solid(Color.Red, BorderWidth);
            }
            else
            {
                BorderPen = Pens.Solid(Color.Red, 0);
            }

            //перья для отрисовки
            BoxBorderPen = Pens.Solid(BoxBorderColor, BoxBorderWidth);
            CreaseBorderPen = Pens.Solid(CreaseBorderColor, CreaseBorderWidth);

            //некоторые параметрические константы

            //положение заготовок: отступ сверху
            //int boxTopOffset = (int)(BlockHeight*2.0f);
            double boxTopOffset = 0;


            //рендер эскиза
            {

                //текущие экранные координаты
                double px = PositionX;
                double py = PositionY;

                //число ручьев
                int threads = 0;
                //общее число блоков 
                int blocksQuantity = 0;

                if (Crease1.Id != 0)
                {
                    threads = 1;
                    blocksQuantity = blocksQuantity + Crease1.Threads;

                    if (Crease2.Id != 0)
                    {
                        threads = 2;
                        blocksQuantity = blocksQuantity + Crease1.Threads;
                    }
                }


                /*
                
                //для расчета берем данные 1 стекера 
                //(Format и Trim одинаковые для обоих)
                int workWidth=Crease1.Format-Crease1.Trim;
                
                //коэффициент перехода от реальных размеров к экранным
                //размер_блока=рабочий_размер*ar
                double  ar=Math.Round( (double)(  (double)Width / (double)workWidth), 3 );

                */

                //double  ar=Math.Round( (double)(  (double)Width / (double)2550), 3 );
                double ar = (double)Width / (double)MaxFormatWidth;
                double workWidth = Crease1.Format - Crease1.Trim;

                //размеры заготовки
                double blockWidth;
                double blockHeight;
                double blockY = 0;


                double trim = 0;
                int blockCounter = 0;

                // проходы [1,2] 1 -- заливка, 2 -- контур
                for (int k = 1; k <= 2; k++)
                { 

                    px = PositionX;
                    py = PositionY;
                    blockCounter = 0;

                    // стекеры [1,2]
                    for (int i = 1; i <= 2; i++)
                    {
                        bool resume = false;

                        var c = Crease1;
                        if (i == 1)
                        {
                            c = Crease1;
                        }
                        else if (i == 2)
                        {
                            c = Crease2;
                        }


                        if (c.Trim > 0)
                        {
                            trim = c.Trim;
                        }

                        if (c.Threads > 0)
                        {
                            resume = true;
                        }

                        if (resume)
                        {

                            //экранная ширина одного блока
                            blockWidth = (c.Width * ar);
                            blockHeight = BlockHeight;

                            blockY = py + boxTopOffset;

                            for (int j = 1; j <= c.Threads; j++)
                            {
                                //заготовка
                                {
                                    //заливка
                                    if( k==1 ){
                                        o.Image.Mutate(x => x.Fill(
                                            Color.ParseHex(HColor.ToHexRGB(c.BackgroundColor)),
                                            //new Rectangle(px + 1, blockY + 1, blockWidth - 2, blockHeight - 2)
                                            new Rectangle( (int)px , (int)blockY, (int)blockWidth, (int)blockHeight )
                                        ));
                                    }

                                    //контур заготовки
                                    if( k==2 ){
                                        /*
                                        o.Image.Mutate( x=> x.Draw(
                                            BoxBorderPen, 
                                            new Rectangle( px, blockY, blockWidth, blockHeight )
                                        ));
                                        */

                                        if (blockCounter == 0)
                                        {
                                            //слева 
                                            o.Image.Mutate(context =>
                                            {
                                                PointF[] points ={
                                                    new PointF( (int)px, (int)(blockY) ),
                                                    new PointF( (int)px, (int)(blockY+blockHeight) ),
                                                };
                                                context.DrawLines(BoxBorderColor, BoxBorderWidth, points);
                                            });
                                        }


                                        //сверху 
                                        o.Image.Mutate(context =>
                                        {
                                            PointF[] points ={
                                                new PointF( (int)px,            (int)(blockY+1) ),
                                                new PointF( (int)(px+blockWidth), (int)(blockY+1) ),
                                            };
                                            context.DrawLines(BoxBorderColor, BoxBorderWidth, points);
                                        });

                                        //справа 
                                        o.Image.Mutate(context =>
                                        {
                                            PointF[] points ={
                                                new PointF( (int)(px+blockWidth), (int)(blockY) ),
                                                new PointF( (int)(px+blockWidth), (int)(blockY+blockHeight) ),
                                            };
                                            context.DrawLines(BoxBorderColor, BoxBorderWidth, points);
                                        });

                                        //снизу
                                        o.Image.Mutate(context =>
                                        {
                                            PointF[] points ={
                                                new PointF( (int)px,              (int)(blockY+blockHeight-1) ),
                                                new PointF( (int)(px+blockWidth), (int)(blockY+blockHeight-1) ),
                                            };
                                            context.DrawLines(BoxBorderColor, BoxBorderWidth, points);
                                        });

                                    }

                                }


                                //рилевки
                                if( k==2 ){
                                    /*
                                        алгоритм отображения рилевок:
                                        1 если заданы c.CreaseSumm, тогда рисуем 2 симметричных рилевки
                                        2 иначе смотрим c.CreaseList, рисуем столько, сколько там указано
                                    */

                                    if (c.CreaseSym != 0)
                                    {

                                        double leftCrease = c.CreaseSym;
                                        double leftCreaseWidth = (leftCrease * ar);

                                        double centerCrease = (c.Width - (c.CreaseSym * 2));
                                        double centerCreaseWidth = (centerCrease * ar);

                                        double px1 = px;

                                        px1 = px1 + leftCreaseWidth;

                                        //левая
                                        {
                                            o.Image.Mutate(context =>
                                            {
                                                PointF[] points ={
                                                    new PointF( (int)px1, (int)blockY ),
                                                    new PointF( (int)px1, (int)(blockY+blockHeight) ),
                                                };
                                                context.DrawLines(CreaseBorderColor, CreaseBorderWidth, points);
                                            });
                                        }


                                        px1 = px1 + centerCreaseWidth;

                                        //средняя
                                        {
                                            o.Image.Mutate(context =>
                                            {
                                                PointF[] points ={
                                                    new PointF( (int)px1, (int)blockY ),
                                                    new PointF( (int)px1, (int)(blockY+blockHeight) ),
                                                };
                                                context.DrawLines(CreaseBorderColor, CreaseBorderWidth, points);
                                            });
                                        }


                                    }
                                    else if (c.CreaseList.Count > 0)
                                    {

                                        double px1 = px;

                                        foreach (int crease in c.CreaseList)
                                        {
                                            double creaseWidth = (crease * ar);

                                            //аккумулятор
                                            px1 = px1 + creaseWidth;

                                            {
                                                o.Image.Mutate(context =>
                                                {
                                                    PointF[] points ={
                                                        new PointF( (int)px1, (int)blockY ),
                                                        new PointF( (int)px1, (int)(blockY+blockHeight) ),
                                                    };
                                                    context.DrawLines(CreaseBorderColor, CreaseBorderWidth, points);
                                                });
                                            }

                                        }

                                    }

                                }


                                //отступаем ширину блока (для рисования следующего)                            
                                px = (px + blockWidth);                           
                                blockCounter++;
                            }

                        }

                    }
                }


                //обрезь
                if (trim > 0)
                {

                    blockWidth = (trim * ar);
                    blockHeight = BlockHeight;

                    if( (int)blockWidth > 0 && (int)blockHeight > 0)
                    {
                         //заливка блока обрези
                        {
                            try { 
                            
                                o.Image.Mutate(x => x.Fill(
                                    Color.ParseHex(HColor.ToHexRGB(HColor.Red)),
                                    //new Rectangle(px + 1, blockY + 1, blockWidth - 2, blockHeight - 2)
                                    new Rectangle( (int)px, (int)blockY, (int)blockWidth, (int)blockHeight )
                                ));

                            }
                            catch(Exception e)
                            {

                            }
                        }

                        //контур блока обрези
                        {

                            //слева 
                            o.Image.Mutate(context =>
                            {
                                PointF[] points ={
                                    new PointF( (int)(px), (int)blockY ),
                                    new PointF( (int)(px), (int)(blockY+blockHeight) ),
                                };
                                context.DrawLines(BoxBorderColor, BoxBorderWidth, points);
                            });


                            //сверху 
                            o.Image.Mutate(context =>
                            {
                                PointF[] points ={
                                    new PointF( (int)px,              (int)(blockY+1) ),
                                    new PointF( (int)(px+blockWidth), (int)(blockY+1) ),
                                };
                                context.DrawLines(BoxBorderColor, BoxBorderWidth, points);
                            });

                            //справа 
                            o.Image.Mutate(context =>
                            {
                                PointF[] points ={
                                    new PointF( (int)(px+blockWidth), (int)blockY ),
                                    new PointF( (int)(px+blockWidth), (int)(blockY+blockHeight) ),
                                };
                                context.DrawLines(BoxBorderColor, BoxBorderWidth, points);
                            });

                            //снизу
                            o.Image.Mutate(context =>
                            {
                                PointF[] points ={
                                    new PointF( (int)px,              (int)(blockY+blockHeight-1) ),
                                    new PointF( (int)(px+blockWidth), (int)(blockY+blockHeight-1) ),
                                };
                                context.DrawLines(BoxBorderColor, BoxBorderWidth, points);
                            });

                        }
                    }
                    
                   

                    //отступаем ширину блока (для рисования следующего)
                    px = px + blockWidth;
                }

                /*
                o.Image.Mutate(context =>
                {
                    PointF[] points={ 
                        new PointF( PositionX+(int)(workWidth*ar), blockY ),
                        new PointF( PositionX+(int)(workWidth*ar), blockY+blockHeight ),
                    };                   
                    context.DrawLines(new Rgba32(250, 0, 0), BoxBorderWidth, points);    
                });
                */

            }


            //рендер границы
            {
                int px = PositionX;
                int py = PositionY;

                if (Debug)
                {
                    o.Image.Mutate(x => x.Draw(
                       BorderPen,
                       new Rectangle(px, py, Width, Height)
                   ));
                }

                PositionY = PositionY + Height;
            }

        }


    }
}
