using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MacroAssembler
{
    public enum StActions { StAdd, StSubtract }


    public class SymbolTable
    {
        private const int Tablemax = 239; // symbol table size
        private const int Tablestep = 7; // a prime number

        public delegate void StPatch(byte[] mem, byte b, byte v, byte a);

        private class StForwardrefs
        {
            // forward references for undefined labels
            public byte Byt;
            public StActions Action; // taken when patching
            //public StForwardrefs Nlink; // to next reference
        }

        private class StEntries
        {
            public String Name; // name
            public byte Value; // value once defined
            public bool Used; // true when in use already
            public bool Defined; // true after defining occurrence encountered
            //public StForwardrefs Flink; // to forward references
            public List<StForwardrefs> Entries { get; set; }
        }

        public SymbolTable(SourceHandler sh)
        {
            _srce = sh;
            //for (short i = 0; i < Tablemax; i++) _hashtable[i].Used = false;
        }

        // Summarizes symbol table at end of assembly
        // returns true if any symbols have remained undefined
        public void Printsymboltable(ref bool errors)
        {
            _srce.Lst.Write("\nSymbol Table\n");
            _srce.Lst.Write("------------\n");
            foreach (var entry in _hashtable.Values)
            {
                if (entry.Used)
                {
                    _srce.Writetext(entry.Name, 10);
                    if (!entry.Defined)
                    {
                        _srce.Lst.Write(" -- undefined");
                        errors = true;
                    }
                    else
                    {
                        _srce.Writehex(entry.Value, 3);
                        _srce.Lst.Write("{0:X5}", entry.Value);
                    }

                    _srce.Lst.WriteLine("");

                }
            }

            //for (short i = 0; i < Tablemax; i++)
            //{
            //    if (_hashtable[i].Used)
            //    {
            //        _srce.Writetext(_hashtable[i].Name, 10);
            //        if (!_hashtable[i].Defined)
            //        {
            //            _srce.Lst.Write(" -- undefined");
            //            errors = true;
            //        }
            //        else
            //        {
            //            _srce.Writehex(_hashtable[i].Value, 3);
            //            _srce.Lst.Write("{0:X5}", _hashtable[i].Value);
            //        }

            //        _srce.Lst.WriteLine("");
            //    }
            //}
            _srce.Lst.WriteLine("");
        }

        // Adds name to table with known value
        public void Enter(string name, byte value)
        {
            StEntries symentry;
            
            Findentry(out symentry, name);
            symentry.Value = value;
            symentry.Defined = true;
            //_hashtable[symentry].Value = value;
            //_hashtable[symentry].Defined = true;
        }

        // Returns value of required name, and sets undefined if not found.
        // Records action to be applied later in fixing up forward references.
        // location is the current value of the instruction location counter
        public void Valueofsymbol(string name, byte location, out byte value,
            StActions action, out bool undefined)
        {
            StEntries symentry;
            var found = Findentry(out symentry, name);
            value = symentry.Value;

            //value = _hashtable[symentry].Value;

            undefined = !symentry.Defined;
            //undefined = !_hashtable[symentry].Defined;

            if (!undefined) return;

            var forwardentry = new StForwardrefs
            {
                Byt = location,
                Action = action,
                //Nlink = found ? _hashtable[symentry].Flink : null
                //Nlink = found ? symentry.Flink : null
            };

            symentry.Entries.Add(forwardentry);
            //symentry.Flink = forwardentry;
            //_hashtable[symentry].Flink = forwardentry;
        }

        public void Outstandingreferences(byte[] mem, StPatch fix)
        {
            foreach (var entry in _hashtable.Values)
            {
                if (entry.Used)
                {
                    foreach (var link in entry.Entries)
                    {
                        fix(mem, link.Byt, entry.Value, (byte) link.Action);
                    }
                }
            }
            //for (short i = 0; i < Tablemax; i++)
            //{
            //    if (_hashtable[i].Used)
            //    {
            //        var link = _hashtable[i].Flink;
            //        while (link != null)
            //        {
            //            fix(mem, link.Byt, _hashtable[i].Value, (byte)link.Action);
            //            link = link.Nlink;
            //        }
            //    }
            //}

        }

        private readonly SourceHandler _srce;
        private readonly Dictionary<string, StEntries> _hashtable = new Dictionary<string, StEntries>(); 

        private enum Findstate
        {
            Looking,
            Entered,
            Caninsert,
            Overflow
        }

        bool Findentry(out StEntries symentry, string name)
        {
            var state = Findstate.Looking;

            symentry = null;
            if (_hashtable.ContainsKey(name))
            {
                symentry = _hashtable[name];
            }

            state = Findstate.Caninsert;
            if (symentry == null)
            {
                //state = Findstate.Entered;
                symentry = new StEntries();

                _hashtable.Add(name, symentry);
                _hashtable[name].Name = name;
                _hashtable[name].Value = 0;
                _hashtable[name].Used = true;
                _hashtable[name].Entries = new List<StForwardrefs>();
                _hashtable[name].Defined = false;
                return false;
            }

            return true;
            
            //symentry = Hashkey(name);

            //var state = Findstate.Looking;
            //var start = symentry;
            //while (state == Findstate.Looking)
            //{
            //    if (!_hashtable[symentry].Used)
            //    {
            //        state = Findstate.Caninsert;
            //        break;
            //    }

            //    if (_hashtable[symentry].Name.Equals(name))
            //    {
            //        state = Findstate.Entered;
            //        break;
            //    }

            //    symentry = (short)((symentry + Tablestep)%Tablemax);
            //    if(symentry == start) state = Findstate.Overflow;
            //}

            //switch (state)
            //{
            //    case Findstate.Caninsert:
            //        symentry.Name = name;
            //        symentry.Value = 0;
            //        symentry.Used = true;
            //        symentry.Entries = new List<StForwardrefs>();
            //        symentry.Defined = false;
            //        //_hashtable[symentry].Name = name;
            //        //_hashtable[symentry].Value = 0;
            //        //_hashtable[symentry].Used = true;
            //        //_hashtable[symentry].Flink = null;
            //        //_hashtable[symentry].Defined = false;
            //        break;

            //    case Findstate.Overflow:
            //        throw new Exception("Symbol table overflow");

            //    case Findstate.Entered:
            //        break;
            //}

            //return (state == Findstate.Entered);
        }

        private static short Hashkey(string ident)
        {
            const int large = (int.MaxValue - 256); // large number in hashing function
            int sum = 0, l = ident.Length;
            for (int i = 0; i < l; i++) sum = (sum + ident[i]) % large;
            return (short)(sum % Tablemax);
        }
    }
}
