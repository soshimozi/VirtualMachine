using System;

namespace MacroAssembler
{
    public class Set : ICloneable
    {
        private readonly int _maxSize = 512;
        private readonly byte [] _bits;
        private int _length;

        public Set()
        {
            _bits = new byte[(_maxSize + 8) / 8];
            Clear();
        }

        public Set(byte e1) : this()
        {
            Incl(e1);
        }

        public Set(byte e1, byte e2) : this()
        {
            Incl(e1);
            Incl(e2);
        }

        public Set(byte e1, byte e2, byte e3)  : this()
        {
            Incl(e1);
            Incl(e2);
            Incl(e3);
        }

        public Set(int n, byte[] e1) : this()
        {
            for (var i = 0; i < n; i++) Incl(e1[i]);
        }


        public void Incl(int e)
        {
            if (e >= 0 && e <= _maxSize) _bits[Wrd(e)] |= Bitmask(e);
        }


        public static Set operator +(Set sthis, Set s)
        {
            if (sthis._maxSize != s._maxSize)
                throw new ArgumentException("MaxSize must be equal");

            var r = new Set();
            for (var i = 0; i < sthis._length; i++) r._bits[i] = (byte)(sthis._bits[i] | s._bits[i]);
            return r;            
        }

        public static Set operator *(Set sthis, Set s)
        {
            if (sthis._maxSize != s._maxSize)
                throw new ArgumentException("MaxSize must be equal");

            var r = new Set();
            for (var i = 0; i < sthis._length; i++) r._bits[i] = (byte)(sthis._bits[i] & s._bits[i]);
            return r;
        }

        public static Set operator -(Set sthis, Set s)
        {
            if (sthis._maxSize != s._maxSize)
                throw new ArgumentException("MaxSize must be equal");

            var r = new Set();
            for (var i = 0; i < sthis._length; i++) r._bits[i] = (byte)(sthis._bits[i] & ~s._bits[i]);
            return r;
        }

        public static Set operator /(Set sthis, Set s) // Symmetric difference with s
        {
            if (sthis._maxSize != s._maxSize)
                throw new ArgumentException("MaxSize must be equal");

            var r = new Set();
            for (var i = 0; i < sthis._length; i++) r._bits[i] = (byte)(sthis._bits[i] ^ s._bits[i]);
            return r;
        }


        private void Clear()
        {
            _length = (_maxSize + 8) / 8;
            for (int i = 0; i < _length; i++) _bits[i] = 0;
        }

        public void Excl(int e) // Exclude e
        {
            if (e >= 0 && e <= _maxSize) _bits[Wrd(e)] &= (byte)~Bitmask(e);
        }


        public bool Memb(int e) // Test membership for e
        {
            if (e >= 0 && e <= _maxSize) return ((_bits[Wrd(e)] & Bitmask(e)) != 0);
            return false;
        }

        public bool Isempty() // Test for empty set
        {
            for (int i = 0; i < _length; i++) if (_bits[i] != 0) return false;
            return true;
        }

        protected static byte Wrd(int i) { return (byte)(i / 8); }
        protected static byte Bitmask(int i) { return (byte)(1 << (i % 8)); }

        public object Clone()
        {
            var newSet = new Set();
            for (var i = 0; i < _length; i++)
            {
                newSet._bits[i] = _bits[i];
            }
            newSet._length = _length;

            return newSet;
        }
    }
}
