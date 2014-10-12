using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroAssembler
{
    public class NumberFormatter
    {
        public static string NumberToHexString(int value)
        {
            const string str = "0123456789abcdef";
            var hi = ((value & 0xf0) >> 4);
            var lo = (value & 15);
            return str.Substring(hi, hi + 1) + str.Substring(lo, lo + 1);            
        }
    }
}
