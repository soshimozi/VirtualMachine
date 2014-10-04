using System;
using System.IO;

namespace MacroAssembler
{
    public class SourceHandler : IDisposable
    {
        public const int LineMax = 129;  // limit on source line length

        public StreamWriter Lst;  // listing file
        public char Ch;  // latest character read

        private readonly StreamReader _src;
        private int _charpos;
        private int _linelength;
        private readonly char[] _line = new char[LineMax];
        private bool _disposed;


        public SourceHandler()
        {
            Ch = ' ';
        }

        public SourceHandler(string sourceFile, string listingFile, string version)
        {
            _src = File.OpenText(sourceFile);
            Lst = new StreamWriter(File.Open(listingFile, FileMode.Create));

            Lst.Write("{0}\n\n", version );
            Ch = ' ';
        }

        public void Nextch()
        {
            if (Ch == '\0') return; // input exhausted
            if (_charpos == _linelength) // new line needed
            {
                _linelength = 0;
                _charpos = 0;
                Ch = Getc(_src);
                while (Ch != '\n' && !_src.EndOfStream)
                {
                    if (_linelength < LineMax)
                    {
                        _line[_linelength] = Ch;
                        _linelength++;
                    }
                    Ch = Getc(_src);
                }
                if (_src.EndOfStream)
                    _line[_linelength] = '\0'; // mark end with an explicit nul
                else
                {
                    if (_src.Peek() == '\r')
                        Getc(_src);

                    _line[_linelength] = ' '; // mark end with an explicit space
                }

                _linelength++;
            }
            Ch = _line[_charpos];
            _charpos++; // pass back unique character            
        }


        public bool Endline()
        {
            return _charpos == _linelength;
        }

        public bool Startline()
        {
            return (_charpos == 1);
        }

        public void Writehex(int i, int n)
        {
            var str = string.Format("{0:X2}", i);
            Lst.Write(str.PadRight(n));
        }

        public void Writetext(string s, int n)
        {
            Lst.Write("{0}", s.PadRight(n));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    _src.Close();
                }
// ReSharper disable once EmptyGeneralCatchClause
                catch
                {
                }

                try
                {
                    Lst.Close();
                }
// ReSharper disable once EmptyGeneralCatchClause
                catch
                {
                }
            }

            _disposed = true;
        }

        private static char Getc(TextReader stream)
        {
            var buffer = new char[1];
            stream.Read(buffer, 0, 1);
            return buffer[0];
        }
    }
}
