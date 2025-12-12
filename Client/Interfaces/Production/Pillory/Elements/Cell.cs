using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Production.Pillory
{
    public class Cell
    {
        public Cell(string cellName, int cellNumber)
        {
            CellName = cellName;
            CellNumber = cellNumber;
            Name = $"{CellName}{CellNumber}";
        }

        public string Name { get; set; }

        public string CellName { get; set; }

        public int CellNumber { get; set; }
    }
}
