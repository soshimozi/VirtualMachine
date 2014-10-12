using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MacroAssembler
{
    public class UndefinedSymbolException : ApplicationException
    {
        
    }

    public class BadAddressException : ApplicationException
    {
        
    }

    public class SymbolTree
    {
        private class ForwardReference
        {
            public int Location;
            public StActions Action;
        }

        private class SymbolTreeEntry
        {
            public string Name;
            public int Value;
            public bool Defined;
            public readonly List<ForwardReference> ForwardReferences;

            public SymbolTreeEntry()
            {
                ForwardReferences = new List<ForwardReference>();
            }
        }

        private readonly Dictionary<string, SymbolTreeEntry> _symbols;

        public SymbolTree()
        {
            _symbols = new Dictionary<string, SymbolTreeEntry>();
        }

        public bool TryGetSymbolValue(string symbol, int location, StActions action, out int value)
        {
            SymbolTreeEntry entry;
            FindEntry(symbol, out entry);

            value = entry.Value;
            if (entry.Defined) return false;

            var reference = new ForwardReference
            {
                Location = location,
                Action = action,
            };

            entry.ForwardReferences.Add(reference);
            return true;
        }

        private void FindEntry(string symbolName, out SymbolTreeEntry entry)
        {
            entry = null;
            if (_symbols.ContainsKey(symbolName))
            {
                entry = _symbols[symbolName];
            }

            if (entry != null) return;

            entry = new SymbolTreeEntry {Defined = false, Value = 0, Name = symbolName};
            _symbols.Add(symbolName, entry);
        }

        public void Enter(string name, int value)
        {
            SymbolTreeEntry symentry;

            FindEntry(name, out symentry);
            
            symentry.Value = value;
            symentry.Defined = true;
        }
    }

    public class Assembler6502 : AssemblerBase
    {
        private enum AssemblerDirective
        {
            Beg, 
            End, 
            If, 
            Mac, 
            Equ, 
            Org, 
            Ds, 
            Dc,
            Nul,
            Err = 0xff
        }

        private enum AddressMode
        {
            Imm, /* Immediate */
            Zp,  /* Zero Page */
            Zpx, /* Zero Page X Indexed */
            Zpy, /* Zero Page Y Indexed */
            Abs, /* Absolute */
            Absx, /* Absolute X Indexed */
            Absy, /* Absolute Y Indexed */
            Ind,  /* for jumps */
            Indx, /* Zero Page Indexed Indirect */
            Indy, /* Zero Page Indexed With Y */
            Sngl, /* Single Byte Instruction (implied) */
            Bra   /* Branch Instruction */
        }

        private struct ObjectLine
        {
            public int Location;
            public int Address;
            public byte Opcode;
            public SaUnpackedlines SourceLine;
        }

        private class OpCodeEntry
        {
            public string Name;
            public bool IsDirective;
            public byte[] Codes = new byte[(int) AddressMode.Bra];
        }

        private readonly int[] _instructionLengths = { 2, 2, 2, 2, 3, 3, 3, 3, 2, 2, 1, 2 };
        private readonly int[] _argSizes = { 1, 1, 1, 1, 2, 2, 2, 2, 1, 1, 0, 1 };

        private readonly Dictionary<string, OpCodeEntry> _opcodes;
        private readonly SymbolTree _tree;
        private readonly Memory _programMemory = new Memory();

        private bool _assembling = false;
        private ObjectLine _currentObjectLine;
        private int _currentPC;

        public Assembler6502(StreamReader input, StreamWriter output, string version) 
            : base(input, output, version)
        {
            _opcodes = new Dictionary<string, OpCodeEntry>();

            AddOpCode("   ", true, new byte[] { (int)AssemblerDirective.Nul, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }); // for lines with no opcode
            AddOpCode("EQU", true, new byte[] { (byte)AssemblerDirective.Equ, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }); // for lines with no opcode
            AddOpCode("DS", true, new byte[] { (byte)AssemblerDirective.Ds, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }); // for lines with no opcode

            AddOpCode("LDA", false, new byte[] { 0xa9, 0xa5, 0xb5, 0xff, 0xad, 0xbd, 0xb9, 0xff, 0xa1, 0xb1, 0xff, 0xff });

            _tree = new SymbolTree();
        }

        public int CurrentPC
        {
            get { return _currentPC;  }
        }

        public override void AssembleLine(SaUnpackedlines srceLine, out bool errors)
        {
            errors = false;

            _currentObjectLine.Location = _currentPC;
            _currentObjectLine.Address = 0;


            // if it's a directive process
            // if it's a macro definition then define the macro
            // if it's not a directive then process as opcode
            if (IsDirective(srceLine.Mnemonic))
            {
                int directive = GetOpCode(srceLine.Mnemonic, 0);

                int value;
                switch (directive)
                {
                    case (int)AssemblerDirective.Equ:
                        errors = ProcessEquDirective(srceLine);
                        break;

                    case (int)AssemblerDirective.Beg:
                        _currentPC = 0;
                        break;

                    case (int)AssemblerDirectives.AsOrg:
                        errors = ProcessOrgDirective(srceLine);
                        break;

                    case (int)AssemblerDirective.Ds:
                        break;

                    case (int)AssemblerDirective.Nul:
                        if (srceLine.Labelled) _tree.Enter(srceLine.Labfield, _currentPC);
                        break;
                }

            }
            else
            {
                // it's an opcode
                var addressMode = GetAddressMode(srceLine, ref errors);

                _currentObjectLine.Opcode = GetOpCode(srceLine.Mnemonic, addressMode);

                if (srceLine.Labelled) _tree.Enter(srceLine.Labfield, _currentPC);

                if (_currentObjectLine.Opcode == (int) AssemblerDirective.Err) return;

                PushByte(_currentObjectLine.Opcode);

                int address;
                try
                {
                    address = GetAddressValue(srceLine.Address, addressMode);
                }
                catch (BadAddressException bae)
                {
                    srceLine.Errors.Incl((int)AsmErrors.AsmInvalidaddress);
                    return;
                }

                switch (_argSizes[(int) addressMode])
                {
                    case 1:
                        PushByte((byte) address);
                        break;
                    case 2:
                        PushWord(address);
                        break;
                }
            }
        }

        private void PushWord(int value)
        {
            PushByte((byte)(value & 0xff));
            PushByte((byte)((value >> 8) & 0xff));
        }

        private void PushByte(byte value)
        {
            _programMemory[_currentPC] = value;
            _currentPC++;
        }

        private bool ProcessOrgDirective(SaUnpackedlines srceLine)
        {
            try
            {
                // throws BadAddress and LabelUndefined
                _currentObjectLine.Address = GetAddressValue(srceLine.Address, AddressMode.Abs);
            }
            catch (UndefinedSymbolException usex)
            {
                srceLine.Errors.Incl((int)AsmErrors.AsmUndefinedlabel);
                return false;
            }

            _currentObjectLine.Location = _currentPC;
            return true;
        }

        private bool ProcessEquDirective(SaUnpackedlines srceLine)
        {
            try
            {
                // throws BadAddress and LabelUndefined
                _currentObjectLine.Address = GetAddressValue(srceLine.Address, AddressMode.Abs);
            }
            catch (UndefinedSymbolException usex)
            {
                srceLine.Errors.Incl((int) AsmErrors.AsmUndefinedlabel);
            }

            if (!srceLine.Errors.Isempty()) return true;

            if (srceLine.Labelled)
                _tree.Enter(srceLine.Labfield, _currentObjectLine.Address);
            else
                srceLine.Errors.Incl((int) AsmErrors.AsmUndefinedlabel);

            return !srceLine.Errors.Isempty();
        }

        private AddressMode GetAddressMode(SaUnpackedlines srceLine, ref bool errors)
        {
            // check if it's a single byte mnemonic
            if (srceLine.Address.Length == 0)
            {
                if (IsSingleByteMnemonic(srceLine.Mnemonic))
                {
                    return AddressMode.Sngl;
                }
            }

            if (srceLine.Address.Length == 1)
            {
                // it could be a branch label
                // in which case it will be branch address type
                if(IsBranch(srceLine.Mnemonic))
                    return AddressMode.Bra;

                // only one, so it's either immediate or absolute
                if (srceLine.Address.Term[0].Kind == SaTermkinds.SaAlphameric)
                    return AddressMode.Abs;

                if (srceLine.Address.Term[0].Kind != SaTermkinds.SaNumeric)
                {
                    errors = true;
                    srceLine.Errors.Incl((int) AsmErrors.AsmInvalidaddress);
                    return AddressMode.Sngl;
                }

                if (srceLine.Address.Term[0].IsImmediate)
                    return AddressMode.Imm;

                return srceLine.Address.Term[0].Size == 1 ? AddressMode.Zp : AddressMode.Abs;
            }

            if (srceLine.Address.Length == 3)
            {
                if (srceLine.Mnemonic.ToUpper() == "JMP")
                {
                    if (srceLine.Address.Term[0].Kind == SaTermkinds.SaLParen &&
                        srceLine.Address.Term[2].Kind == SaTermkinds.SaRParen)
                    {
                        return AddressMode.Ind;
                    }
                }
                else
                {
                    // direct indexing
                    // LDA $02,X
                    // LDA $D332,Y
                    // LDA $D335,X
                    if (srceLine.Address.Term[1].Kind == SaTermkinds.SaComma)
                    {
                        if (CheckXIndex(srceLine.Address.Term[2]))
                        {
                            // check for zero page
                            if (srceLine.Address.Term[0].Kind == SaTermkinds.SaAlphameric)
                                return AddressMode.Absx;
                            
                            if(srceLine.Address.Term[0].Kind == SaTermkinds.SaNumeric)
                                return srceLine.Address.Term[0].Size == 1 ? AddressMode.Zpx : AddressMode.Absx;
                        }

                        if (CheckYIndex(srceLine.Address.Term[2]))
                        {
                            if (srceLine.Address.Term[0].Kind == SaTermkinds.SaAlphameric)
                                return AddressMode.Absy;

                            if (srceLine.Address.Term[0].Kind == SaTermkinds.SaNumeric)
                                return srceLine.Address.Term[0].Size == 1 ? AddressMode.Zpy : AddressMode.Absy;
                        }
                    }
                }

            }

            if (srceLine.Address.Length == 5)
            {
                // indirect indexing
                // LDA ($03,X)
                // LDA ($3D),Y

                if (srceLine.Address.Term[0].Kind == SaTermkinds.SaLParen)
                {
                    // determine which kind of index
                    if (srceLine.Address.Term[2].Kind == SaTermkinds.SaComma)
                    {
                        // indexed indirect
                        if (CheckXIndex(srceLine.Address.Term[3]))
                        {
                            // check for zero page
                            if (srceLine.Address.Term[1].Kind == SaTermkinds.SaNumeric)
                                return AddressMode.Indx;
                        }

                    } else if (srceLine.Address.Term[2].Kind == SaTermkinds.SaRParen)
                    {
                        // indirect indexed
                        if (CheckYIndex(srceLine.Address.Term[4]))
                        {
                            if (srceLine.Address.Term[1].Kind == SaTermkinds.SaNumeric)
                                return AddressMode.Indy;
                        }
                    }
                }
            }

            srceLine.Errors.Incl((int)AsmErrors.AsmInvalidaddress);
            errors = !srceLine.Errors.Isempty();

            return AddressMode.Sngl;
        }

        private bool CheckYIndex(SaTerms saTerms)
        {
            return saTerms.Name.ToUpper() == "Y";
        }

        private bool CheckXIndex(SaTerms saTerms)
        {
            return saTerms.Name.ToUpper() == "X";
        }

        private int GetAddressValue(SaAddresses address, AddressMode addressMode)
        {
            int value = 0;

            switch (addressMode)
            {
                case AddressMode.Absx:
                case AddressMode.Absy:
                case AddressMode.Abs:
                    // absolute, so just get the value as an integer
                    value = GetValueOfTerm(address.Term[0], StActions.StAdd);
                    break;

                case AddressMode.Zp:
                case AddressMode.Zpx:
                case AddressMode.Zpy:
                    value = ((byte)(GetValueOfTerm(address.Term[0], StActions.StAdd) % 256));
                    break;

                case AddressMode.Indx:
                case AddressMode.Indy:
                    value = ((byte)(GetValueOfTerm(address.Term[1], StActions.StAdd) % 256));
                    break;

                case AddressMode.Ind:
                    value = GetValueOfTerm(address.Term[1], StActions.StAdd);
                    break;
            }

            //malformed = false;

            //value = Termvalue(address.Term[0], StActions.StAdd, out undefined, ref malformed);
            //var i = 1;
            //while (i < address.Length)
            //{
            //    StActions nextaction;
            //    switch (address.Term[i].Kind)
            //    {
            //        case SaTermkinds.SaPlus: nextaction = StActions.StAdd;
            //            break;
            //        case SaTermkinds.SaMinus: nextaction = StActions.StSubtract;
            //            break;

            //        default:
            //            nextaction = StActions.StAdd;
            //            malformed = true;
            //            break;
            //    }
            //    i++;
            //    bool unknown;
            //    var nextvalue = Termvalue(address.Term[i], nextaction, out unknown, ref malformed);
            //    switch (nextaction)
            //    {
            //        case StActions.StAdd:
            //            value = (byte)((value + nextvalue) % 256);
            //            break;

            //        case StActions.StSubtract:
            //            value = (byte)((value - nextvalue + 256) % 256);
            //            break;
            //    }
            //    undefined = (undefined || unknown);
            //    i++;
            //}

            return value;
        }

        private int GetValueOfTerm(SaTerms term, StActions action)
        {
            int value;

            switch (term.Kind)
            {
                case SaTermkinds.SaAbsent:
                case SaTermkinds.SaNumeric:
                    value = term.Value;
                    break;
                case SaTermkinds.SaStar:
                    value = _currentPC;
                    break;
                case SaTermkinds.SaAlphameric:
                    if (!_tree.TryGetSymbolValue(term.Name, _currentPC, action, out value))
                        throw new UndefinedSymbolException();
                    break;
                default:
                    throw new BadAddressException();
            }

            return value;
        }
        private bool IsBranch(string mnemonic)
        {
            var mnemonics = new[]
            {
                "BCC", "BCS", "BEQ", "BMI", "BNE", "BVC", "BVS"
            };

            return mnemonics.Contains(mnemonic.ToUpper());
        }

        private bool IsSingleByteMnemonic(string mnemonic)
        {
            var mnemonics = new[]
            {
                "INX", "INY", "DEX", "DEY", "TAX", "TAY", "TXA", "TYA", "TSX", "TXS", "PHA", "PLA", "PHP", "PLP", "RTS",
                "SEC", "SED", "CLC", "CLD", "CLV", "NOP"
            };

            return mnemonics.Contains(mnemonic.ToUpper());
        }

        public override void Assemble(ref bool errors)
        {
            _assembling = true;

            while (_assembling)
            {
                SaUnpackedlines srcline;
                Parser.Parse(out srcline);
                AssembleLine(srcline, out errors);
            }

            // back patch symbol tree references


        }

        private List<ObjectLine> FirstPass(ref bool errors)
        {
            var list = new List<ObjectLine>();

            int location = 0;
            while (_assembling)
            {

                SaUnpackedlines srcline;
                Parser.Parse(out srcline);

                list.Add(new ObjectLine { Location = location, SourceLine = srcline });

                var mode = GetAddressMode(srcline, ref errors);
                location += _instructionLengths[(int) mode];
            }

            return list;
        }

        private void AddOpCode(string mnemonic, bool isDirective, byte[] opcodes)
        {
            _opcodes.Add(mnemonic, new OpCodeEntry { Name =  mnemonic, IsDirective =  isDirective, Codes = opcodes});
        }

        private bool IsDirective(string mnemonic)
        {
            return _opcodes.ContainsKey(mnemonic) && _opcodes[mnemonic].IsDirective;
        }

        private byte GetOpCode(string mnemonic, AddressMode mode)
        {
            return _opcodes.ContainsKey(mnemonic) ? _opcodes[mnemonic].Codes[(int) mode] : (byte)0xff;
        }
    }
}
