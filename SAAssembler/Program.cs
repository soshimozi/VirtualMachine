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

            var vm = new VirtualMachine();
            var asm = new Assembler(vm);

            bool errors = false;
            asm.Assemble("test.asm", "test.out", "2.33", ref errors);

            var reader = new StreamReader(Console.OpenStandardInput());
            var writer = new StreamWriter(Console.OpenStandardOutput());

            vm.emulator(0, reader, writer, true);
        }
    }
}
