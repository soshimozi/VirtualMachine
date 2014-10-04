using System;
using System.Text;

namespace MacroAssembler
{

    public enum LaSymtypes
    {
        LaUnknown, LaEofsym, LaEolsym, LaIdsym, LaNumsym, LaComsym,
        LaCommasym, LaPlussym, LaMinussym, LaStarsym, LaLeftParensym, LaRightParensym,
        LaDollarSignsym, LaPounsym
    }

    public class LaSymbols
    {
        public bool Islabel; // if in first column
        public LaSymtypes Sym; // class
        public StringBuilder Str = new StringBuilder(36, 36); // lexeme
        public int Num; // value if numeric
    }

    public class LexicalAnalyzer
    {
        private readonly SourceHandler _srce;

        private const int AsmSlength = 36;

        public LexicalAnalyzer(SourceHandler sh)
        {
            _srce = sh;
            _srce.Nextch();
        }

        public LaSymbols Getsym(Set errors)
        {
            var sym = new LaSymbols {Num = 0, Str = new StringBuilder()};

            // eat up whitespace
            while (Char.IsWhiteSpace(_srce.Ch) && !_srce.Endline()) _srce.Nextch();
            sym.Islabel = (_srce.Startline() && _srce.Ch != ' ' && _srce.Ch != ';' && _srce.Ch != '\0');
            if (sym.Islabel && !Char.IsLetter(_srce.Ch)) errors.Incl((int) AsmErrors.AsmBadlabel);
            if (_srce.Ch == '\0')
            {
                sym.Sym = LaSymtypes.LaEofsym;
                return sym;
            }

            if (_srce.Endline())
            {
                sym.Sym = LaSymtypes.LaEolsym;
                _srce.Nextch();
                return sym;
            }

            if (Char.IsLetter(_srce.Ch))
            {
                sym.Sym = LaSymtypes.LaIdsym;
                GetWord(sym);
            }
            else if (Char.IsDigit(_srce.Ch))
            {
                sym.Sym = LaSymtypes.LaNumsym;
                GetNumber(sym, errors);
            }
            else
                switch (_srce.Ch)
                {
                    case ')':
                        sym.Sym = LaSymtypes.LaRightParensym;
                        sym.Str.Append(')');
                        _srce.Nextch();
                        break;
                    case '(':
                        sym.Sym = LaSymtypes.LaLeftParensym;
                        sym.Str.Append('(');
                        _srce.Nextch();
                        break;
                    case '$':
                        sym.Sym = LaSymtypes.LaDollarSignsym;
                        sym.Str.Append('$');
                        _srce.Nextch();
                        break;
                    case '#':
                        sym.Sym = LaSymtypes.LaPounsym;
                        sym.Str.Append('#');
                        _srce.Nextch();
                        break;
                    case ';':
                        sym.Sym = LaSymtypes.LaComsym;
                        GetComment(sym);
                        break;
                    case ',':
                        sym.Sym = LaSymtypes.LaCommasym;
                        sym.Str.Append(',');
                        _srce.Nextch();
                        break;
                    case '+':
                        sym.Sym = LaSymtypes.LaPlussym;
                        sym.Str.Append("+");
                        _srce.Nextch();
                        break;
                    case '-':
                        sym.Sym = LaSymtypes.LaMinussym;
                        sym.Str.Append("-");
                        _srce.Nextch();
                        break;
                    case '*':
                        sym.Sym = LaSymtypes.LaStarsym;
                        sym.Str.Append("*");
                        _srce.Nextch();
                        break;
                    case '\'':
                    case '"':
                        sym.Sym = LaSymtypes.LaNumsym;
                        GetQuotedchar(sym, _srce.Ch, errors);
                        break;
                    default:
                        sym.Sym = LaSymtypes.LaUnknown;
                        GetComment(sym);
                        errors.Incl((int) AsmErrors.AsmInvalidchar);
                        break;
                }

            return sym;
        }

        private void GetWord(LaSymbols sym)
        // Assemble identifier or opcode, in UPPERCASE for consistency
        {
            var length = 0;
            while (Char.IsLetterOrDigit(_srce.Ch))
            {
                if (length < AsmSlength)
                {
                    sym.Str.Append(Char.ToUpper(_srce.Ch));
                    length++;
                }
                _srce.Nextch();
            }
        }

        private void GetNumber(LaSymbols sym, Set errors)
        // Assemble number and store its identifier in UPPERCASE for consistency
        {
            int length = 0;
            while (Char.IsDigit(_srce.Ch))
            {
                sym.Num = sym.Num*10 + _srce.Ch - '0';
                if (sym.Num > 255) errors.Incl((int) AsmErrors.AsmOverflow);
                sym.Num %= 256;
                if (length < AsmSlength)
                {
                    sym.Str.Append(Char.ToUpper(_srce.Ch));
                    length++;
                }
                _srce.Nextch();
            }
        }

        private void GetComment(LaSymbols sym)
        // Assemble comment
        {
            int length = 0;
            while (!_srce.Endline())
            {
                if (length < AsmSlength)
                {
                    sym.Str.Append(_srce.Ch);
                    length++;
                }
                _srce.Nextch();
            }
        }

        private void GetQuotedchar(LaSymbols sym, char quote, Set errors)
        // Assemble single character address token
        {
            sym.Str.Append(quote);
            _srce.Nextch();
            sym.Num = _srce.Ch;
            sym.Str.Append(_srce.Ch);
            if (!_srce.Endline()) _srce.Nextch();
            sym.Str.Append(_srce.Ch);
            if (_srce.Ch != quote) errors.Incl((int)AsmErrors.AsmInvalidquote);
            if (!_srce.Endline()) _srce.Nextch();
        }
    }
}
