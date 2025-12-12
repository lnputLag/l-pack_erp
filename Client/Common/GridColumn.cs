namespace Client.Common
{
    /// <summary>
    /// вспомогательная структура для хранения данных колонки грида
    /// (используется дляпрозрачного экспорта данных грида в XLS)
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    public class GridColumn
    {
        /// <summary>
        /// символьное имя, должно быть уникальным (технологическое поле)
        /// оно же является ключом для извлечения данных из датасета
        /// </summary>
        public string Name { get; set;}
        /// <summary>
        /// текст заголовка колонки
        /// </summary>
        public string Header { get; set;}
        /// <summary>
        /// ширина колонки
        /// (в пикселях грида)
        /// </summary>
        public int Width { get; set;}
        /// <summary>
        /// тип данных: s,str,string, i,int,integer, d,digit, double, b,bool,boolean
        /// </summary>
        public string Type { get; set; }

        public GridColumn()
        {
            Name="";
            Header="";
            Width=50;
            Type="auto";
        }

        public GridColumn(string n="",string h="",int w=50,string t="s")
        {
            Name=n;
            Header=h;
            Width=w;
            Type=t;
        }
    }
}
