namespace MacroAssembler
{
    public class MhLines
    {
        public SaUnpackedlines Text; // a single line of macro text
        public MhLines Link; // link to the next line in the macro
    }

    public class MhMacro
    {
        public SaUnpackedlines Definition; // header line
        public MhMacro Mlink; // link to next macro in list
        public MhLines Firstline, Lastline; // links to the text of this macro
    }

    public class MacroAnalyzer
    {
        private MhMacro _lastmac;

        public MhMacro Newmacro(SaUnpackedlines header)
        {
            //var m = new MhMacro {Definition = (SaUnpackedlines)header.Clone(), Firstline = null, Mlink = _lastmac};
            var m = new MhMacro { Definition = header, Firstline = null, Mlink = _lastmac };
            _lastmac = m;

            return m;
        }

        public void Storeline(MhMacro m, SaUnpackedlines line)
        {
            var newline = new MhLines {Text = line, Link = null};
            if (m.Firstline == null)            // first line of macro?
                m.Firstline = newline;          // form head of new queue
            else
                m.Lastline.Link = newline;      // add to tail of existing queue

            m.Lastline = newline;
        }

        public void Checkmacro(string name, out MhMacro m, out bool ismacro, out int parameters)
        {
            m = _lastmac;
            ismacro = false;
            parameters = 0;

            while (m != null && !ismacro)
            {
                if (m.Definition.Labfield.Equals(name))
                {
                    ismacro = true;
                    parameters = m.Definition.Address.Length;
                }
                else
                    m = m.Mlink;
            }
        }

        public void Expand(MhMacro m, SaAddresses actualparams, IAssembler assembler, ref bool errors)
        {
            if (m == null) return;
            var current = m.Firstline;
            while (current != null)
            {
                var nextline = current.Text;
                SubsituteActualParameters(m, actualparams, nextline);
                assembler.AssembleLine(nextline, out errors);  // and assemble it
                current = current.Link;
            }
        }

        // Search formals for match to str, returns 0 if no match
        private static int Position(MhMacro m, string str)
        {
            var found = false;
            var i = m.Definition.Address.Length - 1;
            while (i >= 0 && !found)
            {
                if (m.Definition.Address.Term[i].Name.Equals(str))
                {
                    found = true;
                }
                else
                {
                    i--;
                }
            }

            return i;
        }

        // Subsittue lable, mnemonic or address components into
        // nextline where necessary
        private void SubsituteActualParameters(MhMacro m, SaAddresses actualparams, SaUnpackedlines nextline)
        {
            var i = Position(m, nextline.Labfield); // check label

            if (i >= 0) nextline.Labfield = actualparams.Term[i].Name;
            i = Position(m, nextline.Mnemonic);             // check mnemonic
            if (i >= 0) nextline.Mnemonic = actualparams.Term[i].Name;
            var j = 0;
            while (j < nextline.Address.Length)
            {
                i = Position(m, nextline.Address.Term[j].Name);
                if (i >= 0) nextline.Address.Term[j] = actualparams.Term[i];
                j += 2;                                     // bypass commas
            }
        }
    }

}
