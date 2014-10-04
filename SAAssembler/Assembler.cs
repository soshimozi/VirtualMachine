using System;
using System.IO;

namespace MacroAssembler
{

    public class Assembler : IAssembler
    {
        const bool Nocodelisted = false;
        const bool Codelisted = true;

        private enum Directives
        {
            AsErr = 61, // erroneous opcode
            AsNul = 62, // blank opcode
            AsBeg = 63, // introduce program
            AsEnd = 64, // end of source
            AsMac = 65, // introduce macro
            AsDs = 66, // define storage
            AsEqu = 67, // equate
            AsOrg = 68, // set location counter
            AsIf = 69, // conditional
            AsDC = 70 // define constant byte
        }


        private struct Optableval
        {
            public string Spelling;
            public byte Byt;
        }

        private struct Objlines
        {
            public byte Location, Opcode, Address;
        }

        private static readonly string[] ErrorMsg =
        {
            " - unknown opcode",
            " - address field not resolved",
            " - invalid address field",
            " - label missing",
            " - spurious address field",
            " - address field missing",
            " - address field too long",
            " - wrong number of parameters",
            " - invalid formal parameters",
            " - invalid label",
            " - unknown character",
            " - mismatched quotes",
            " - number too large"
        };

        private SourceHandler _srce;
        private SyntaxAnalyzer _parser;
        private SymbolTable _table;
        private readonly IVirtualMachine _machine;
        private MacroAnalyzer _macro;

        private readonly Optableval[] _optable = new Optableval[256];
        private int _opcodes; // number of opcodes actually defined
        private Objlines _objline; // current line as assembled
        private byte _location; // location counter
        private bool _assembling; // monitor progress of assembly
        private bool _include; // handle conditional assembly

        // Instantiates version of the assembler to process sourcename, creating
        // listings in listname, and generating code for associated machine M
        public Assembler(IVirtualMachine m)
        {
            _machine = m;

            // enter opcodes and mnemonics in ALPHABETIC order
            // done this way for ease of modification later
            _opcodes = 0; // bogus one for erroneous data
            Enter("Error   ",  (int)Directives.AsErr); // for lines with no opcode
            Enter("   ", (int)Directives.AsNul); Enter("ACI", (int)McOpcodes.McAci); Enter("ACX", (int)McOpcodes.McAcx);
            Enter("ADC", (int)McOpcodes.McAdc); Enter("ADD", (int)McOpcodes.McAdd); Enter("ADI", (int)McOpcodes.McAdi);
            Enter("ADX", (int)McOpcodes.McAdx); Enter("ANA", (int)McOpcodes.McAna); Enter("ANI", (int)McOpcodes.McAni);
            Enter("ANX", (int)McOpcodes.McAnx); Enter("BCC", (int)McOpcodes.McBcc); Enter("BCS", (int)McOpcodes.McBcs);
            Enter("BEG", (int)Directives.AsBeg); Enter("BNG", (int)McOpcodes.McBng); Enter("BNZ", (int)McOpcodes.McBnz);
            Enter("BPZ", (int)McOpcodes.McBpz); Enter("BRN", (int)McOpcodes.McBrn); Enter("BZE", (int)McOpcodes.McBze);
            Enter("CLA", (int)McOpcodes.McCla); Enter("CLC", (int)McOpcodes.McClc); Enter("CLX", (int)McOpcodes.McClx);
            Enter("CMC", (int)McOpcodes.McCmc); Enter("CMP", (int)McOpcodes.McCmp); Enter("CPI", (int)McOpcodes.McCpi);
            Enter("CPX", (int)McOpcodes.McCpx); Enter("DC", (int)Directives.AsDC); Enter("DEC", (int)McOpcodes.McDec);
            Enter("DEX", (int)McOpcodes.McDex); Enter("DS", (int)Directives.AsDs); Enter("END", (int)Directives.AsEnd);
            Enter("EQU", (int)Directives.AsEqu); Enter("HLT", (int)McOpcodes.McHlt); Enter("IF", (int)Directives.AsIf);
            Enter("INA", (int)McOpcodes.McIna); Enter("INB", (int)McOpcodes.McInb); Enter("INC", (int)McOpcodes.McInc);
            Enter("INH", (int)McOpcodes.McInh); Enter("INI", (int)McOpcodes.McIni); Enter("INX", (int)McOpcodes.McInx);
            Enter("JSR", (int)McOpcodes.McJsr); Enter("LDA", (int)McOpcodes.McLda); Enter("LDI", (int)McOpcodes.McLdi);
            Enter("LDX", (int)McOpcodes.McLdx); Enter("LSI", (int)McOpcodes.McLsi); Enter("LSP", (int)McOpcodes.McLsp);
            Enter("MAC", (int)Directives.AsMac); Enter("NOP", (int)McOpcodes.McNop); Enter("ORA", (int)McOpcodes.McOra);
            Enter("ORG", (int)Directives.AsOrg); Enter("ORI", (int)McOpcodes.McOri); Enter("ORX", (int)McOpcodes.McOrx);
            Enter("OTA", (int)McOpcodes.McOta); Enter("OTB", (int)McOpcodes.McOtb); Enter("OTC", (int)McOpcodes.McOtc);
            Enter("OTH", (int)McOpcodes.McOth); Enter("OTI", (int)McOpcodes.McOti); Enter("POP", (int)McOpcodes.McPop);
            Enter("PSH", (int)McOpcodes.McPsh); Enter("RET", (int)McOpcodes.McRet); Enter("SBC", (int)McOpcodes.McSbc);
            Enter("SBI", (int)McOpcodes.McSbi); Enter("SBX", (int)McOpcodes.McSbx); Enter("SCI", (int)McOpcodes.McSci);
            Enter("SCX", (int)McOpcodes.McScx); Enter("SHL", (int)McOpcodes.McShl); Enter("SHR", (int)McOpcodes.McShr);
            Enter("STA", (int)McOpcodes.McSta); Enter("STX", (int)McOpcodes.McStx); Enter("SUB", (int)McOpcodes.McSub);
            Enter("TAX", (int)McOpcodes.McTax);
        }

        // Assemble single srcline
        public void AssembleLine(SaUnpackedlines srcline, out bool failure)
        {
            failure = false;

            if (!_include)
            {
                _include = true; // conditional assembly
                return;
            }

            bool found, undefined;
            MhMacro macro;
            int formal;
            _macro.Checkmacro(srcline.Mnemonic, out macro, out found, out formal);
            if (found) // expand macro and exit
            {
                if (srcline.Labelled) _table.Enter(srcline.Labfield, _location);
                if (formal != srcline.Address.Length) // number of params okay?
                    srcline.Errors.Incl((int)AsmErrors.AsmMismatched);
                Listsourceline(srcline, Nocodelisted, out failure);
                if (srcline.Errors.Isempty()) // okay to expand?
                    _macro.Expand(macro, srcline.Address, this, ref failure);
                return;
            }

            var badaddress = false;
            _objline.Location = _location;
            _objline.Address = 0;
            _objline.Opcode = Bytevalue(srcline.Mnemonic);

            if (_objline.Opcode == (int)Directives.AsErr) // check various constraints
                srcline.Errors.Incl((int)AsmErrors.AsmInvalidcode);
            else if (_objline.Opcode > (int)Directives.AsMac ||
                     _objline.Opcode > (int)McOpcodes.McHlt && _objline.Opcode < (int)Directives.AsErr)
            {
                if (srcline.Address.Length == 0) srcline.Errors.Incl((int)AsmErrors.AsmNoaddress);
            }
            else if (_objline.Opcode != (int)Directives.AsMac && srcline.Address.Length != 0)
                srcline.Errors.Incl((int)AsmErrors.AsmHasaddress);

            if (_objline.Opcode >= (int)Directives.AsErr && _objline.Opcode <= (int)Directives.AsDC)
            {
                switch (_objline.Opcode) // directives
                {
                    case (int)Directives.AsBeg:
                        _location = 0;
                        break;
                    case (int)Directives.AsOrg:
                        Evaluate(srcline.Address, out _location, out undefined, out badaddress);
                        if (undefined) srcline.Errors.Incl((int)AsmErrors.AsmUndefinedlabel);
                        _objline.Location = _location;
                        break;
                    case (int)Directives.AsDs:
                        if (srcline.Labelled) _table.Enter(srcline.Labfield, _location);
                        Evaluate(srcline.Address, out _objline.Address, out undefined, out badaddress);
                        if (undefined) srcline.Errors.Incl((int)AsmErrors.AsmUndefinedlabel);
                        _location = (byte)((_location + _objline.Address) % 256);
                        break;
                    case (int)Directives.AsNul:
                    case (int)Directives.AsErr:
                        if (srcline.Labelled) _table.Enter(srcline.Labfield, _location);
                        break;
                    case (int)Directives.AsEqu:
                        Evaluate(srcline.Address, out _objline.Address, out undefined, out badaddress);
                        if (srcline.Labelled)
                            _table.Enter(srcline.Labfield, _objline.Address);
                        else
                            srcline.Errors.Incl((int)AsmErrors.AsmUnlabelled);
                        if (undefined) srcline.Errors.Incl((int)AsmErrors.AsmUndefinedlabel);
                        break;
                    case (int)Directives.AsDC:
                        if (srcline.Labelled) _table.Enter(srcline.Labfield, _location);
                        Evaluate(srcline.Address, out _objline.Address, out undefined, out badaddress);
                        _machine.Mem[_location] = _objline.Address;
                        _location = (byte)((_location + 1) % 256);
                        break;
                    case (int)Directives.AsIf:
                        Evaluate(srcline.Address, out _objline.Address, out undefined, out badaddress);
                        if (undefined) srcline.Errors.Incl((int)AsmErrors.AsmUndefinedlabel);
                        _include = (_objline.Address != 0);
                        break;
                    case (int)Directives.AsMac:
                        Definemacro(srcline, out failure);
                        break;
                    case (int)Directives.AsEnd:
                        _assembling = false;
                        break;
                }
            }
            else // machine codes
            {
                if (srcline.Labelled) _table.Enter(srcline.Labfield, _location);
                
                _machine.Mem[_location] = _objline.Opcode;

                if (_objline.Opcode > (int)McOpcodes.McHlt) // two byte op codes
                {
                    _location = (byte)((_location + 1) % 256);
                    Evaluate(srcline.Address, out _objline.Address, out undefined, out badaddress);
                    _machine.Mem[_location] = _objline.Address;
                }

                _location = (byte)((_location + 1) % 256);
            }

            if (badaddress) srcline.Errors.Incl((int)AsmErrors.AsmInvalidaddress);
            if (_objline.Opcode != (int)Directives.AsMac) Listsourceline(srcline, Codelisted, out failure);
        }

        // Assembles and lists program.
        // Assembled code is dumped to file for later interpretation, and left
        // in pseudo-machine memory for immediate interpretation if desired.
        // Returns errors = true if assembly fails
        public void Assemble(string sourcefile, string listfile, string version, ref bool errors)
        {
            _srce = new SourceHandler(sourcefile, listfile, version);
            var lex = new LexicalAnalyzer(_srce);
            _parser = new SyntaxAnalyzer(lex);
            _table = new SymbolTable(_srce);
            _macro = new MacroAnalyzer();

            _srce.Lst.Write("(One Pass Macro Assembler)\n\n");
            Firstpass(ref errors);
            _machine.Listcode(_srce.Lst);

            _srce.Dispose();
            _srce = null;
        }

        private void Firstpass(ref bool errors)
        {
            _location = 0;
            _assembling = true;
            _include = true;

            while (_assembling)
            {
                SaUnpackedlines srcline;
                _parser.Parse(out srcline);
                AssembleLine(srcline, out errors);
            }

            _table.Printsymboltable(ref errors);
            if (!errors) _table.Outstandingreferences(_machine.Mem, Backpatch);

        }

        private void Enter(string mnemonic, byte thiscode)
        {
            _optable[_opcodes].Spelling = mnemonic;
            _optable[_opcodes].Byt = thiscode;
            _opcodes++;
        }

        private byte Bytevalue(string mnemonic)
        {
            int look, l = 1, r = _opcodes;
            do   // binary search
            {
                look = (l + r)/2;
                if (String.Compare(mnemonic, _optable[look].Spelling, StringComparison.InvariantCulture) <= 0) r = look - 1;
                if (String.Compare(mnemonic, _optable[look].Spelling, StringComparison.InvariantCulture) >= 0) l = look + 1;
            } while (l <= r);

            return l > r + 1 ? _optable[look].Byt : _optable[0].Byt;
        }

        // determine value of a single term, recording outstanding action
        // if undefined so far, and recording badaddress if malformed
        private byte Termvalue(SaTerms term, StActions action, out bool undefined, ref bool badaddress)
        {
            byte value;

            undefined = false;
            switch (term.Kind)
            {
                case SaTermkinds.SaAbsent:
                case SaTermkinds.SaNumeric:
                    value = (byte)(term.Number%256);
                    break;
                case SaTermkinds.SaStar:
                    value = _location;
                    break;
                case SaTermkinds.SaAlphameric:
                    _table.Valueofsymbol(term.Name, _location, out value, action, out undefined);
                    break;
                default:
                    badaddress = true;
                    value = 0;
                    break;
            }

            return value;
        }

        private void Evaluate(SaAddresses address, out byte value, out bool undefined, out bool malformed)
        {
            malformed = false;

            value = Termvalue(address.Term[0], StActions.StAdd, out undefined, ref malformed);
            var i = 1;
            while (i < address.Length)
            {
                StActions nextaction;
                switch (address.Term[i].Kind)
                {
                    case SaTermkinds.SaPlus: nextaction = StActions.StAdd;
                        break;
                    case SaTermkinds.SaMinus: nextaction = StActions.StSubtract;
                        break;

                    default:
                        nextaction = StActions.StAdd;
                        malformed = true;
                        break;
                }
                i++;
                bool unknown;
                var nextvalue = Termvalue(address.Term[i], nextaction, out unknown, ref malformed);
                switch (nextaction)
                {
                    case StActions.StAdd:
                        value = (byte) ((value + nextvalue)%256);
                        break;

                    case StActions.StSubtract:
                        value = (byte) ((value - nextvalue + 256)%256);
                        break;
                }
                undefined = (undefined || unknown);
                i++;
            }
        }

        private void Listerrors(Set allerrors, out bool failure)
        {
            failure = false;

            if (allerrors.Isempty()) return;
            failure = true;

            _srce.Lst.Write("Next line has errors");
            for (var error = (int) AsmErrors.AsmInvalidcode; error <= (int) AsmErrors.AsmOverflow; error++)
            {
                if(allerrors.Memb(error)) _srce.Lst.WriteLine("{0}", ErrorMsg[error]);
            }
        }

        // List generated code bytes on source listing
        private void Listcode()
        {
            _srce.Writehex(_objline.Location, 4);

            if(_objline.Opcode >= (int)Directives.AsErr && _objline.Opcode <= (int)Directives.AsIf)
                _srce.Lst.Write("       ");
            else if(_objline.Opcode <= (int)McOpcodes.McHlt)
                _srce.Writehex(_objline.Opcode, 7);
            else if (_objline.Opcode == (int)Directives.AsDs)
                _srce.Writehex(_objline.Address, 7);
            else
            {
                _srce.Writehex(_objline.Opcode, 3);
                _srce.Writehex(_objline.Address, 4);
            }
        }

        // List srcline, with option of listing generated code
        private void Listsourceline(SaUnpackedlines srcline, bool coderequired, out bool failure)
        {
            Listerrors(srcline.Errors, out failure);

            if (coderequired) 
                Listcode();
            else 
                _srce.Lst.Write("           ");

            _srce.Writetext(srcline.Labfield, 9);
            _srce.Writetext(srcline.Mnemonic, 9);

            int width = srcline.Address.Term[0].Name.Length;
            _srce.Lst.Write(srcline.Address.Term[0].Name);

            for (int i = 1; i < srcline.Address.Length; i++)
            {
                width += srcline.Address.Term[i].Name.Length + 1;
                _srce.Lst.Write(' ');
                _srce.Lst.Write(srcline.Address.Term[i].Name);
            }

            if (width < 30) _srce.Writetext(" ", 30 - width);
            
            _srce.Lst.WriteLine("{0}", srcline.Comment);
        }

        // Handle introduction of a macro (possibly nested)
        private void Definemacro(SaUnpackedlines srcline, out bool failure)
        {
            byte opcode;
            var macro = new MhMacro();

            var declared = false;
            var i = 0;
            if (srcline.Labelled)                           // name must be present
                declared = true;
            else
                srcline.Errors.Incl((int)AsmErrors.AsmUnlabelled);

            if ((srcline.Address.Length & 1) == 0)          // must be an odd number of terms
                srcline.Errors.Incl((int)AsmErrors.AsmInvalidaddress);

            while (i < srcline.Address.Length)              // check that formals are names
            {
                if(srcline.Address.Term[i].Kind != SaTermkinds.SaAlphameric)
                    srcline.Errors.Incl((int)AsmErrors.AsmNonalpha);
                i += 2;
            }

            Listsourceline(srcline, Nocodelisted, out failure);
            if (declared) macro = _macro.Newmacro(srcline);

            do
            {
                _parser.Parse(out srcline);
                opcode = Bytevalue(srcline.Mnemonic);

                if (opcode == (int)Directives.AsMac) // nested macro?
                    Definemacro(srcline, out failure); // recursion handles it
                else
                {
                    Listsourceline(srcline, Nocodelisted, out failure);
                    if (declared && opcode != (int)Directives.AsEnd && srcline.Errors.Isempty())
                        _macro.Storeline(macro, srcline); // add to macro text
                }

            } while (opcode != (int)Directives.AsEnd);
        }

        private static void Backpatch(byte[] m, byte loc, byte value, byte how)
        {
            switch (how)
            {
                case (int)StActions.StAdd:
                    m[loc] = (byte)((m[loc] + value) % 256); break;
                case (int)StActions.StSubtract:
                    m[loc] = (byte)((m[loc] - value + 256) % 256); break;
            }
            
        }


    }
}
