using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroAssembler
{
    // machine instructions - order important
    internal enum McOpcodes
    {
        McNop,
        McCla,
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

    public class VirtualMachine : IVirtualMachine
    {
        private enum statusEnum
        {
            running,
            finished,
            nodata,
            baddata,
            badop
        };

        private struct processor
        {
            public byte a; // Accululator
            public byte sp; // Stack pointer
            public byte x; // Index register
            public byte ir; // instruction register
            public byte pc; // program counter
            public bool z, p, c; // condition flags
        }

        private processor _cpu;
        private statusEnum ps;
        private string[] mnemonics = new string[256];


        public VirtualMachine()
        {
            Mem = new byte[256];

            for (var i = 0; i <= 255; i++) Mem[i] = (byte) McOpcodes.McBad;

            // Initialize mnemonic table
            for (var i = 0; i <= 255; i++) mnemonics[i] = "???";
            mnemonics[(int) McOpcodes.McAci] = "ACI";
            mnemonics[(int) McOpcodes.McAcx] = "ACX";
            mnemonics[(int) McOpcodes.McAdc] = "ADC";
            mnemonics[(int) McOpcodes.McAdd] = "ADD";
            mnemonics[(int) McOpcodes.McAdi] = "ADI";
            mnemonics[(int) McOpcodes.McAdx] = "ADX";
            mnemonics[(int) McOpcodes.McAna] = "ANA";
            mnemonics[(int) McOpcodes.McAni] = "ANI";
            mnemonics[(int) McOpcodes.McAnx] = "ANX";
            mnemonics[(int) McOpcodes.McBcc] = "BCC";
            mnemonics[(int) McOpcodes.McBcs] = "BCS";
            mnemonics[(int) McOpcodes.McBng] = "BNG";
            mnemonics[(int) McOpcodes.McBnz] = "BNZ";
            mnemonics[(int) McOpcodes.McBpz] = "BPZ";
            mnemonics[(int) McOpcodes.McBrn] = "BRN";
            mnemonics[(int) McOpcodes.McBze] = "BZE";
            mnemonics[(int) McOpcodes.McCla] = "CLA";
            mnemonics[(int) McOpcodes.McClc] = "CLC";
            mnemonics[(int) McOpcodes.McClx] = "CLX";
            mnemonics[(int) McOpcodes.McCmc] = "CMC";
            mnemonics[(int) McOpcodes.McCmp] = "CMP";
            mnemonics[(int) McOpcodes.McCpi] = "CPI";
            mnemonics[(int) McOpcodes.McCpx] = "CPX";
            mnemonics[(int) McOpcodes.McDec] = "DEC";
            mnemonics[(int) McOpcodes.McDex] = "DEX";
            mnemonics[(int) McOpcodes.McHlt] = "HLT";
            mnemonics[(int) McOpcodes.McIna] = "INA";
            mnemonics[(int) McOpcodes.McInb] = "INB";
            mnemonics[(int) McOpcodes.McInc] = "INC";
            mnemonics[(int) McOpcodes.McInh] = "INH";
            mnemonics[(int) McOpcodes.McIni] = "INI";
            mnemonics[(int) McOpcodes.McInx] = "INX";
            mnemonics[(int) McOpcodes.McJsr] = "JSR";
            mnemonics[(int) McOpcodes.McLda] = "LDA";
            mnemonics[(int) McOpcodes.McLdi] = "LDI";
            mnemonics[(int) McOpcodes.McLdx] = "LDX";
            mnemonics[(int) McOpcodes.McLsi] = "LSI";
            mnemonics[(int) McOpcodes.McLsp] = "LSP";
            mnemonics[(int) McOpcodes.McNop] = "NOP";
            mnemonics[(int) McOpcodes.McOra] = "ORA";
            mnemonics[(int) McOpcodes.McOri] = "ORI";
            mnemonics[(int) McOpcodes.McOrx] = "ORX";
            mnemonics[(int) McOpcodes.McOta] = "OTA";
            mnemonics[(int) McOpcodes.McOtb] = "OTB";
            mnemonics[(int) McOpcodes.McOtc] = "OTC";
            mnemonics[(int) McOpcodes.McOth] = "OTH";
            mnemonics[(int) McOpcodes.McOti] = "OTI";
            mnemonics[(int) McOpcodes.McPop] = "POP";
            mnemonics[(int) McOpcodes.McPsh] = "PSH";
            mnemonics[(int) McOpcodes.McRet] = "RET";
            mnemonics[(int) McOpcodes.McSbc] = "SBC";
            mnemonics[(int) McOpcodes.McSbi] = "SBI";
            mnemonics[(int) McOpcodes.McSbx] = "SBX";
            mnemonics[(int) McOpcodes.McSci] = "SCI";
            mnemonics[(int) McOpcodes.McScx] = "SCX";
            mnemonics[(int) McOpcodes.McShl] = "SHL";
            mnemonics[(int) McOpcodes.McShr] = "SHR";
            mnemonics[(int) McOpcodes.McSta] = "STA";
            mnemonics[(int) McOpcodes.McStx] = "STX";
            mnemonics[(int) McOpcodes.McSub] = "SUB";
            mnemonics[(int) McOpcodes.McTax] = "TAX";
        }


        public void Listcode(StreamWriter writer)
// Simply print all 256 bytes in 16 rows
        {
            byte nextbyte = 0;

            writer.Write("\n");
            for (int i = 1; i <= 16; i++)
            {
                for (int j = 1; j <= 16; j++)
                {
                    writer.Write("{0:x4}", Mem[nextbyte]);
                    increment(ref nextbyte);
                }
                writer.Write("\n");
            }
            writer.Close();
        }


        public byte[] Mem { get; private set; }

        // Emulates action of the instructions stored in mem, with program counter
        // initialized ot initpc.  data and results are used for I/O
        public void emulator(byte initpc, StreamReader data, StreamWriter results, bool tracing)
        {
            byte pcnow; // Old program count
            bool carry; // Value of carry bit
            _cpu.z = false;
            _cpu.p = false;
            _cpu.c = false; // initialize flags
            _cpu.a = 0;
            _cpu.x = 0;
            _cpu.sp = 0; // initialize registers
            _cpu.pc = initpc; // initialize program counter
            ps = statusEnum.running;
            do
            {
                _cpu.ir = Mem[_cpu.pc]; // fetch
                pcnow = _cpu.pc; // record for use in tracing/postmortem
                increment(ref _cpu.pc); // and bump in anticipation
                if (tracing) trace(results, pcnow);
                switch (_cpu.ir) // execute
                {
                    case (byte)McOpcodes.McNop:
                        break;
                    case (byte)McOpcodes.McCla:
                        _cpu.a = 0;
                        break;
                    case (byte)McOpcodes.McClc:
                        _cpu.c = false;
                        break;
                    case (byte)McOpcodes.McClx:
                        _cpu.x = 0;
                        break;
                    case (byte)McOpcodes.McCmc:
                        _cpu.c = !_cpu.c;
                        break;
                    case (byte)McOpcodes.McInc:
                        increment(ref _cpu.a);
                        setflags(_cpu.a);
                        break;
                    case (byte)McOpcodes.McDec:
                        decrement(ref _cpu.a);
                        setflags(_cpu.a);
                        break;
                    case (byte)McOpcodes.McInx:
                        increment(ref _cpu.x);
                        setflags(_cpu.x);
                        break;
                    case (byte)McOpcodes.McDex:
                        decrement(ref _cpu.x);
                        setflags(_cpu.x);
                        break;
                    case (byte)McOpcodes.McTax:
                        _cpu.x = _cpu.a;
                        break;
                    case (byte)McOpcodes.McIni:
                        _cpu.a = getnumber(data, 10, ref ps);
                        setflags(_cpu.a);
                        break;
                    case (byte)McOpcodes.McInb:
                        _cpu.a = getnumber(data, 2, ref ps);
                        setflags(_cpu.a);
                        break;
                    case (byte)McOpcodes.McInh:
                        _cpu.a = getnumber(data, 16, ref ps);
                        setflags(_cpu.a);
                        break;
                    case (byte)McOpcodes.McIna:
                        char ascii = '\0';
                        readchar(data, ref ascii, ref ps);
                        if (data.EndOfStream) ps = statusEnum.nodata;
                        else
                        {
                            _cpu.a = (byte)ascii;
                            setflags(_cpu.a);
                        }
                        break;
                    case (byte)McOpcodes.McOti:
                        if (_cpu.a < 128)
                            results.Write(_cpu.a);
                        else
                            results.Write(_cpu.a - 256);
                        if (tracing) results.WriteLine();

                        break;
                    case (byte)McOpcodes.McOth:
                        results.Write("{0:x2}", _cpu.a);
                        if (tracing) results.WriteLine();
                        break;
                    case (byte)McOpcodes.McOtc:
                        results.Write("{0} ", _cpu.a);
                        if (tracing) results.WriteLine();
                        break;
                    case (byte)McOpcodes.McOta:
                        results.Write(_cpu.a);
                        if (tracing) results.WriteLine();
                        break;
                    case (byte)McOpcodes.McOtb:
                        int [] bits = new int[8];
                        byte number = _cpu.a;
                        for (int loop = 0; loop <= 7; loop++)
                        {
                            bits[loop] = number%2;
                            number /= 2;
                        }
                        for (int loop = 7; loop >= 0; loop--)
                            results.Write(bits[loop]);
                        results.Write(" ");
                        if (tracing) results.WriteLine();
                        break;
                    case (byte)McOpcodes.McPsh:
                        decrement(ref _cpu.sp);
                        Mem[_cpu.sp] = _cpu.a;
                        break;
                    case (byte)McOpcodes.McPop:
                        _cpu.a = Mem[_cpu.sp];
                        increment(ref _cpu.sp);
                        setflags(_cpu.a);
                        break;
                    case (byte)McOpcodes.McShl:
                        _cpu.c = (_cpu.a * 2 > 255);
                        _cpu.a = (byte)(_cpu.a * 2 % 256);
                        setflags(_cpu.a);
                        break;
                    case (byte)McOpcodes.McShr:
                        _cpu.c = (_cpu.a & 1) != 0;
                        _cpu.a /= 2;
                        setflags(_cpu.a);
                        break;
                    case (byte)McOpcodes.McRet:
                        _cpu.pc = Mem[_cpu.sp];
                        increment(ref _cpu.sp);
                        break;
                    case (byte)McOpcodes.McHlt:
                        ps = statusEnum.finished;
                        break;
                    case (byte)McOpcodes.McLda:
                        _cpu.a = Mem[Mem[_cpu.pc]];
                        increment(ref _cpu.pc);
                        setflags(_cpu.a);
                        break;
                    case (byte)McOpcodes.McLdx:
                        _cpu.a = Mem[index()];
                        increment(ref _cpu.pc);
                        setflags(_cpu.a);
                        break;
                    case (byte)McOpcodes.McLdi:
                        _cpu.a = Mem[_cpu.pc];
                        increment(ref _cpu.pc);
                        setflags(_cpu.a);
                        break;
                    case (byte)McOpcodes.McLsp:
                        _cpu.sp = Mem[Mem[_cpu.pc]];
                        increment(ref _cpu.pc);
                        break;
                    case (byte)McOpcodes.McLsi:
                        _cpu.sp = Mem[_cpu.pc];
                        increment(ref _cpu.pc);
                        break;
                    case (byte)McOpcodes.McSta:
                        Mem[Mem[_cpu.pc]] = _cpu.a;
                        increment(ref _cpu.pc);
                        break;
                    case (byte)McOpcodes.McStx:
                        Mem[index()] = _cpu.a;
                        increment(ref _cpu.pc);
                        break;
                    case (byte)McOpcodes.McAdd:
                        _cpu.c = (_cpu.a + Mem[Mem[_cpu.pc]] > 255);
                        _cpu.a = (byte)((_cpu.a + Mem[Mem[_cpu.pc]]) % 256);
                        increment(ref _cpu.pc);
                        setflags(_cpu.a);
                        break;
                    case (byte)McOpcodes.McAdx:
                        _cpu.c = (_cpu.a + Mem[index()] > 255);
                        _cpu.a = (byte)((_cpu.a + Mem[index()]) % 256);
                        increment(ref _cpu.pc);
                        setflags(_cpu.a);
                        break;
                    case (byte)McOpcodes.McAdi:
                        _cpu.c = (_cpu.a + Mem[_cpu.pc] > 255);
                        _cpu.a = (byte)((_cpu.a + Mem[_cpu.pc]) % 256);
                        increment(ref _cpu.pc);
                        setflags(_cpu.a);
                        break;
                    case (byte)McOpcodes.McAdc:
                        carry = _cpu.c;
                        _cpu.c = (_cpu.a + Mem[Mem[_cpu.pc]] + (carry ? 1 : 0) > 255);
                        _cpu.a = (byte)((_cpu.a + Mem[Mem[_cpu.pc]] + (carry ? 1 : 0)) % 256);
                        increment(ref _cpu.pc);
                        setflags(_cpu.a);
                        break;
                    case (byte)McOpcodes.McAcx:
                        carry = _cpu.c;
                        _cpu.c = (_cpu.a + Mem[index()] + (carry ? 1 : 0) > 255);
                        _cpu.a = (byte)((_cpu.a + Mem[index()] + (carry ? 1 : 0)) % 256);
                        increment(ref _cpu.pc);
                        setflags(_cpu.a);
                        break;
                    case (byte)McOpcodes.McAci:
                        carry = _cpu.c;
                        _cpu.c = (_cpu.a + Mem[_cpu.pc] + (carry ? 1 : 0) > 255);
                        _cpu.a = (byte)((_cpu.a + Mem[_cpu.pc] + (carry ? 1 : 0)) % 256);
                        increment(ref _cpu.pc);
                        setflags(_cpu.a);
                        break;
                    case (byte)McOpcodes.McSub:
                        _cpu.c = (_cpu.a < Mem[Mem[_cpu.pc]]);
                        _cpu.a = (byte)((_cpu.a - Mem[Mem[_cpu.pc]] + 256) % 256);
                        increment(ref _cpu.pc);
                        setflags(_cpu.a);
                        break;
                    case (byte)McOpcodes.McSbx:
                        _cpu.c = (_cpu.a < Mem[index()]);
                        _cpu.a = (byte)((_cpu.a - Mem[index()] + 256) % 256);
                        increment(ref _cpu.pc);
                        setflags(_cpu.a);
                        break;
                    case (byte)McOpcodes.McSbi:
                        _cpu.c = (_cpu.a < Mem[_cpu.pc]);
                        _cpu.a = (byte)((_cpu.a - Mem[_cpu.pc] + 256) % 256);
                        increment(ref _cpu.pc);
                        setflags(_cpu.a);
                        break;
                    case (byte)McOpcodes.McSbc:
                        carry = _cpu.c;
                        _cpu.c = (_cpu.a < Mem[Mem[_cpu.pc]] + (carry ? 1 : 0));
                        _cpu.a = (byte)((_cpu.a - Mem[Mem[_cpu.pc]] - (carry ? 1 : 0) + 256) % 256);
                        increment(ref _cpu.pc);
                        setflags(_cpu.a);
                        break;
                    case (byte)McOpcodes.McScx:
                        carry = _cpu.c;
                        _cpu.c = (_cpu.a < Mem[index()] + (carry ? 1 : 0));
                        _cpu.a = (byte)((_cpu.a - Mem[index()] - (carry ? 1 : 0) + 256) % 256);
                        increment(ref _cpu.pc);
                        setflags(_cpu.a);
                        break;
                    case (byte)McOpcodes.McSci:
                        carry = _cpu.c;
                        _cpu.c = (_cpu.a < Mem[_cpu.pc] + (carry ? 1 : 0));
                        _cpu.a = (byte)((_cpu.a - Mem[_cpu.pc] - (carry ? 1 : 0) + 256) % 256);
                        increment(ref _cpu.pc);
                        setflags(_cpu.a);
                        break;
                    case (byte)McOpcodes.McCmp:
                        _cpu.c = (_cpu.a < Mem[Mem[_cpu.pc]]);
                        setflags((byte)((_cpu.a - Mem[Mem[_cpu.pc]] + 256) % 256));
                        increment(ref _cpu.pc);
                        break;
                    case (byte)McOpcodes.McCpx:
                        _cpu.c = (_cpu.a < Mem[index()]);
                        setflags((byte)((_cpu.a - Mem[index()] + 256) % 256));
                        increment(ref _cpu.pc);
                        break;
                    case (byte)McOpcodes.McCpi:
                        _cpu.c = (_cpu.a < Mem[_cpu.pc]);
                        setflags((byte)((_cpu.a - Mem[_cpu.pc] + 256) % 256));
                        increment(ref _cpu.pc);
                        break;
                    case (byte)McOpcodes.McAna:
                        _cpu.a &= Mem[Mem[_cpu.pc]];
                        increment(ref _cpu.pc);
                        setflags(_cpu.a);
                        _cpu.c = false;
                        break;
                    case (byte)McOpcodes.McAnx:
                        _cpu.a &= Mem[index()];
                        increment(ref _cpu.pc);
                        setflags(_cpu.a);
                        _cpu.c = false;
                        break;
                    case (byte)McOpcodes.McAni:
                        _cpu.a &= Mem[_cpu.pc];
                        increment(ref _cpu.pc);
                        setflags(_cpu.a);
                        _cpu.c = false;
                        break;
                    case (byte)McOpcodes.McOra:
                        _cpu.a |= Mem[Mem[_cpu.pc]];
                        increment(ref _cpu.pc);
                        setflags(_cpu.a);
                        _cpu.c = false;
                        break;
                    case (byte)McOpcodes.McOrx:
                        _cpu.a |= Mem[index()];
                        increment(ref _cpu.pc);
                        setflags(_cpu.a);
                        _cpu.c = false;
                        break;
                    case (byte)McOpcodes.McOri:
                        _cpu.a |= Mem[_cpu.pc];
                        increment(ref _cpu.pc);
                        setflags(_cpu.a);
                        _cpu.c = false;
                        break;
                    case (byte)McOpcodes.McBrn:
                        _cpu.pc = Mem[_cpu.pc];
                        break;
                    case (byte)McOpcodes.McBze:
                        if (_cpu.z) _cpu.pc = Mem[_cpu.pc];
                        else increment(ref _cpu.pc);
                        break;
                    case (byte)McOpcodes.McBnz:
                        if (!_cpu.z) _cpu.pc = Mem[_cpu.pc];
                        else increment(ref _cpu.pc);
                        break;
                    case (byte)McOpcodes.McBpz:
                        if (_cpu.p) _cpu.pc = Mem[_cpu.pc];
                        else increment(ref _cpu.pc);
                        break;
                    case (byte)McOpcodes.McBng:
                        if (!_cpu.p) _cpu.pc = Mem[_cpu.pc];
                        else increment(ref _cpu.pc);
                        break;
                    case (byte)McOpcodes.McBcs:
                        if (_cpu.c) _cpu.pc = Mem[_cpu.pc];
                        else increment(ref _cpu.pc);
                        break;
                    case (byte)McOpcodes.McBcc:
                        if (!_cpu.c) _cpu.pc = Mem[_cpu.pc];
                        else increment(ref _cpu.pc);
                        break;
                    case (byte)McOpcodes.McJsr:
                        decrement(ref _cpu.sp);
                        Mem[_cpu.sp] = (byte)((_cpu.pc + 1)%256); // push return address
                        _cpu.pc = Mem[_cpu.pc];
                        break;
                    default:
                        ps = statusEnum.badop;
                        break;
                }
            } while (ps == statusEnum.running);
            if (ps != statusEnum.finished) posmortem(results, pcnow);
        }

        // Maps str to opcode, or ot McBad (0x0ffh) if no match can be found
        private byte opcode(string str)
// Simple linear search suffices for illustration
        {
            str = str.ToUpper();
            byte l = (byte) McOpcodes.McNop;
            while (l <= (byte) McOpcodes.McJsr && str != mnemonics[l]) l++;
            if (l <= (byte) McOpcodes.McJsr) return l;
            else return (byte) McOpcodes.McBad;
        }


        private void trace(StreamWriter results, byte pcnow)
        {
            // Simple trace facility for run time debugging
            results.Write(" PC = {0:x2} A = {1:x2} ", pcnow, _cpu.a);
            results.Write(" X = {0:x2} SP = {1:x2} ", _cpu.x, _cpu.sp);
            results.Write(" Z = {0} P = {1} C = {2}", _cpu.z, _cpu.p, _cpu.c);
            results.Write(" OPCODE = {0:x2} ({1})\n", _cpu.ir, mnemonics[_cpu.ir]);
        }

        private void posmortem(StreamWriter results, byte pcnow)
        {

        }

        private void setflags(byte MC_register)
        {
            // Set P and Z flags according to contents of register
            _cpu.z = (MC_register == 0);
            _cpu.p = (MC_register <= 127);
        }

        // todo: create x(), y() and add addressing modes
        private byte index()
        {
            // Get indexed address with folding at 256
            {
                return (byte) ((Mem[_cpu.pc] + _cpu.x)%256);
            }
        }

        private static void increment(ref byte x)
            // Increment with folding at 256
        {
            x = (byte) ((x + 257)%256);
        }

        private static void decrement(ref byte x)
            // Decrement with folding at 256
        {
            x = (byte) ((x + 255)%256);
        }

        private static void readchar(StreamReader data, ref char ch, ref statusEnum ps)
// Read ch and check for break-in and other awkward values
        {
            if (data.EndOfStream)
            {
                ps = statusEnum.nodata;
                ch = ' ';
                return;
            }

            char[] buffer = new char[1];
            data.Read(buffer, 0, 1);
            ch = buffer[0];

            if (ch == '\x1b') ps = statusEnum.finished;
            if (ch < ' ' || data.EndOfStream) ch = ' ';
        }

        private int hexdigit(char ch)
// Convert CH to equivalent value
        {
            if (ch >= 'a' && ch <= 'e') return (ch + 10 - 'a');
            if (ch >= 'A' && ch <= 'E') return (ch + 10 - 'A');
            if (Char.IsDigit(ch)) return (ch - '0');
            else return (0);
        }

        private byte getnumber(StreamReader data, int radix, ref statusEnum ps)
// Read number in required base
        {
            bool negative = false;
            char ch = '\0';
            int num = 0;
            do
            {
                readchar(data, ref ch, ref ps);
            } while (!(ch > ' ' || data.EndOfStream || ps != statusEnum.running));
            if (ps == statusEnum.running)
            {
                if (data.EndOfStream)
                    ps = statusEnum.nodata;
                else
                {
                    if (ch == '-')
                    {
                        negative = true;
                        readchar(data, ref ch, ref ps);
                    }
                    else if (ch == '+') readchar(data, ref ch, ref ps);
                    if (!ch.IsXDigit())
                    ps = statusEnum.baddata;
                    else
                    {
                        while (ch.IsXDigit() && ps == statusEnum.running)
                        {
                            if (hexdigit(ch) < radix && num <= (int.MaxValue - hexdigit(ch))/radix)
                                num = radix*num + hexdigit(ch);
                            else
                                ps = statusEnum.baddata;
                            readchar(data, ref ch, ref ps);
                        }
                    }
                }
                if (negative) num = -num;
                if (num > 0)
                    return (byte)(num%256);
                else
                    return (byte)((256 - Math.Abs(num)%256)%256);
            }
            return 0;
        }
    }
}
