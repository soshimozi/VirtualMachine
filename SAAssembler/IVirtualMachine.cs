using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace MacroAssembler
{
    public interface IVirtualMachine
    {
        byte[] Mem { get; }
        void Listcode(StreamWriter writer);
        void emulator(byte initpc, StreamReader data, StreamWriter results, bool tracing);
    }
}
