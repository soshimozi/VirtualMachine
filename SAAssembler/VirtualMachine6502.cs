using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroAssembler
{
    internal enum Mc6502Opcodes
    {
        Mc6502Nop,
        Mc6502LdaIndX = 0xa1,
        Mc6502LdaZero = 0xa5,
        Mc6502LdaImm = 0xa9,
        Mc6502Tax = 0xaa,
        Mc6502LdYAbs = 0xac,
        Mc6502LdaAbs = 0xad,
        Mc6502LdXAbx = 0xae,
        Mc6502LdaIndY = 0xb1,
        Mc6502LdaZeroX = 0xb5,
        Mc6502LdaAbsY = 0xb9,
        Mc6502LdaAbsX = 0xbd,

        McClc,
        McClx,
        McCmc,
        McInc,
        McDec,
        McInx,
        McDex,
        McTax,
        McIni,
        McInh,
        McInb,
        McIna,
        McOti,
        McOtc,
        McOth,
        McOtb,
        McOta,
        McPsh,
        McPop,
        McShl,
        McShr,
        McRet,
        McHlt,
        McLda,
        McLdx,
        McLdi,
        McLsp,
        McLsi,
        McSta,
        McStx,
        McAdd,
        McAdx,
        McAdi,
        McAdc,
        McAcx,
        McAci,
        McSub,
        McSbx,
        McSbi,
        McSbc,
        McScx,
        McSci,
        McCmp,
        McCpx,
        McCpi,
        McAna,
        McAnx,
        McAni,
        McOra,
        McOrx,
        McOri,
        McBrn,
        McBze,
        McBnz,
        McBpz,
        McBng,
        McBcc,
        McBcs,
        McJsr,
        McBad = 255
    }

    public class VirtualMachine6502 : IVirtualMachine
    {
        public byte[] Mem { get; private set; }

        public VirtualMachine6502()
        {
            Mem = new byte[65536];
        }

        public void Listcode(StreamWriter writer)
        {
        }

        public void emulator(byte initpc, StreamReader data, StreamWriter results, bool tracing)
        {
        }
    }
}
