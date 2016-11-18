using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossCardReader.Abstractions
{
    public class Card
    {
        public Int64 Number { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
    }
}
