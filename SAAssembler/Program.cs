using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace MacroAssembler
{
    class Program
    {
        static void Main(string[] args)
        {

            //var vm = new VirtualMachine();
            //var asm = new Assembler(vm);

            //bool errors = false;
            //asm.Assemble("test.asm", "test.out", "2.33", ref errors);

            //var reader = new StreamReader(Console.OpenStandardInput());
            //var writer = new StreamWriter(Console.OpenStandardOutput());

            //vm.emulator(0, reader, writer, true);

            string source = @"  
                                LDA ($3D),Y
                                LDA ($0D,X)
                                LDA $02,X
                                LDA $D332,Y
                                LDA $D335,X
                                LDA    $23,X
                                LDA    $23
                                LDA    $0D32
                                LDA    #$32
                                STA    ($15,X)
                                LSR    ($2A),Y
            ";

            StreamReader reader = new StreamReader(source.GenerateStream());
            StreamWriter writer = new StreamWriter("test.out");

            //var sh = new SourceHandler(reader, writer, "2.33");
            //var la = new LexicalAnalyzer(sh);

            ////var errors = new Set();
            ////var sym = la.Getsym(errors);

            ////sym = la.Getsym(errors);
            ////sym = la.Getsym(errors);
            ////sym = la.Getsym(errors);
            ////sym = la.Getsym(errors);
            ////sym = la.Getsym(errors);
            ////sym = la.Getsym(errors);
            ////sym = la.Getsym(errors);

            //var sa = new SyntaxAnalyzer(la);

            //SaUnpackedlines lines;
            //sa.Parse(out lines);
            //sa.Parse(out lines);

            bool errors = false;

            var assembler = new Assembler6502(reader, writer, "1.0a");
            assembler.Assemble(ref errors);

        }
    }
}
