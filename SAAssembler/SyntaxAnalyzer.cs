using System;
using System.Linq;
using System.Runtime.Remoting.Messaging;

namespace MacroAssembler
{

    public enum SaTermkinds
    {
        SaAbsent,
        SaNumeric,
        SaAlphameric,
        SaComma,
        SaPlus,
        SaMinus,
        SaStar
    }

    public struct SaTerms
    {
        public SaTermkinds Kind;
        public int Number; // value if known
        public String Name; // character representation
    }

    public class SaAddresses //: ICloneable
    {
        public byte Length; // number of fields
        public SaTerms[] Term = new SaTerms[15];
        //public object Clone()
        //{
        //    var newAddress = new SaAddresses();
        //    newAddress.Length = Length;
        //    for(var i=0; i < Length; i++)
        //    {
        //        newAddress.Term[i].Name = Term[i].Name;
        //        newAddress.Term[i].Kind = Term[i].Kind;
        //        newAddress.Term[i].Number = Term[i].Number;
        //    }

        //    return newAddress;
        //}
    }

    public class SaUnpackedlines //: ICloneable
    {
        // source text, unpacked into fields
        public bool Labelled;
        public String Labfield, Mnemonic;
        public SaAddresses Address = new SaAddresses();
        public String Comment;
        public Set Errors = new Set((int)AsmErrors.AsmOverflow);


        //public object Clone()
        //{
        //    var newHeader = new SaUnpackedlines
        //    {
        //        Labelled = Labelled,
        //        Labfield = Labfield,
        //        Mnemonic = Mnemonic,
        //        Address = (SaAddresses) Address.Clone(),
        //        Comment = Comment,
        //        Errors = (Set) Errors.Clone()
        //    };

        //    return newHeader;
        //}
    }

    public class SyntaxAnalyzer
    {
        private const int SaMaxterms = 16;

        private readonly LexicalAnalyzer _lex;
        private LaSymbols _sym;

        public SyntaxAnalyzer(LexicalAnalyzer lex)
        {
            _lex = lex;
        }

        public SaUnpackedlines Parse(out SaUnpackedlines srcline)
        {
            var startaddress = new Set((int) LaSymtypes.LaIdsym,(int) LaSymtypes.LaNumsym, (int) LaSymtypes.LaStarsym);

            srcline = new SaUnpackedlines
            {
                Labfield = "",
                Mnemonic = "   ",
                Comment = "",
                Errors = new Set()
            };

            srcline.Address.Term[0].Kind = SaTermkinds.SaAbsent;
            srcline.Address.Term[0].Number = 0;
            srcline.Address.Term[0].Name = "";
            srcline.Address.Length = 0;

            GetSym(srcline.Errors); // first on line - opcode or label ?
            if (_sym.Sym == LaSymtypes.LaEofsym)
            {
                srcline.Mnemonic = "END";
                return srcline;
            }
            srcline.Labelled = _sym.Islabel;

            if (srcline.Labelled) // must look for the opcode
            {
                srcline.Labelled = srcline.Errors.Isempty();

                srcline.Labfield = _sym.Str.ToString();
                GetSym(srcline.Errors); // probably an opcode
            }

            if (_sym.Sym == LaSymtypes.LaIdsym) // has a mnemonic
            {
                srcline.Mnemonic = _sym.Str.ToString();
                
                GetSym(srcline.Errors); // possibly an address
                if (startaddress.Memb((int) _sym.Sym)) Getaddress(srcline);
            }

            if (_sym.Sym == LaSymtypes.LaComsym || _sym.Sym == LaSymtypes.LaUnknown)
            {
                srcline.Comment = _sym.Str.ToString();
                GetSym(srcline.Errors);
            }
            if (_sym.Sym != LaSymtypes.LaEolsym) // spurious symbol
            {
                srcline.Comment = _sym.Str.ToString();
                srcline.Errors.Incl((int) AsmErrors.AsmExcessfields);
            }
            while (_sym.Sym != LaSymtypes.LaEolsym && _sym.Sym != LaSymtypes.LaEofsym)
                GetSym(srcline.Errors); // consume garbage
            return srcline;
        }

        private void GetSym(Set errors)
        {
            _sym = _lex.Getsym(errors);
        }

        // Unpack the addressfield of line into srcline
        private void Getaddress(SaUnpackedlines srcline)
        {
            var allowed = new Set((int) LaSymtypes.LaIdsym, (int) LaSymtypes.LaNumsym, (int) LaSymtypes.LaStarsym);

            var possible = allowed +
                           new Set((int) LaSymtypes.LaCommasym,(int) LaSymtypes.LaPlussym, (int) LaSymtypes.LaMinussym);

            srcline.Address.Length = 0;
            while (possible.Memb((int)_sym.Sym))
            {
                if (!allowed.Memb((int)_sym.Sym))
                    srcline.Errors.Incl((int)AsmErrors.AsmInvalidaddress);
                if (srcline.Address.Length < SaMaxterms - 1)
                    srcline.Address.Length++;
                else
                    srcline.Errors.Incl((int)AsmErrors.AsmExcessfields);

                srcline.Address.Term[srcline.Address.Length - 1].Name = _sym.Str.ToString();
                srcline.Address.Term[srcline.Address.Length - 1].Number = _sym.Num;

                switch (_sym.Sym)
                {
                    case LaSymtypes.LaNumsym:
                        srcline.Address.Term[srcline.Address.Length - 1].Kind = SaTermkinds.SaNumeric;
                        break;
                    case LaSymtypes.LaIdsym:
                        srcline.Address.Term[srcline.Address.Length - 1].Kind = SaTermkinds.SaAlphameric;
                        break;
                    case LaSymtypes.LaPlussym:
                        srcline.Address.Term[srcline.Address.Length - 1].Kind = SaTermkinds.SaPlus;
                        break;
                    case LaSymtypes.LaMinussym:
                        srcline.Address.Term[srcline.Address.Length - 1].Kind = SaTermkinds.SaMinus;
                        break;
                    case LaSymtypes.LaStarsym:
                        srcline.Address.Term[srcline.Address.Length - 1].Kind = SaTermkinds.SaStar;
                        break;
                    case LaSymtypes.LaCommasym:
                        srcline.Address.Term[srcline.Address.Length - 1].Kind = SaTermkinds.SaComma;
                        break;
                }
                allowed = possible - allowed;
                GetSym(srcline.Errors); // check trailing comment, parameters
            }

            if ((srcline.Address.Length & 1) == 0) srcline.Errors.Incl((int)AsmErrors.AsmInvalidaddress);
        }
    }
}
