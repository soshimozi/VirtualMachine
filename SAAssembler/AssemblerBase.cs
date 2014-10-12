using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroAssembler
{
    public enum AssemblerDirectives
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

    public abstract class AssemblerBase : IAssembler
    {
        protected SourceHandler Srce;
        protected SyntaxAnalyzer Parser;

        protected AssemblerBase(StreamReader input, StreamWriter output, string version)
        {
            Srce = new SourceHandler(input, output, version);
            Parser = new SyntaxAnalyzer(new LexicalAnalyzer(Srce));
        }

        public abstract void AssembleLine(SaUnpackedlines srceLine, out bool errors);
        public abstract void Assemble(ref bool errors);
    }
}

